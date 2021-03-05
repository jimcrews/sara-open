using MimeKit.Cryptography;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Sara.Lib.ConfigurableCommands;
using Sara.Lib.Data;
using Sara.Lib.Data.Parsers;
using Sara.Lib.Models.Dataset;
using Sara.Lib.Models.Loader;
using Sara.Lib.Models.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Sara.Lib.Extensions;
using Sara.Lib.ConfigurableCommands.Loaders;
using System.IO;
using Org.BouncyCastle.Asn1.X509;
using Sara.Lib.Logging;

namespace Sara.Lib.Data.Mock
{
    public class MockDataLake : AbstractDataLake
    {
        /// <summary>
        /// Mock database is a dictionary. Each element is a function which generates a dataset
        /// on the fly.
        /// </summary>
        private IDictionary<string, IEnumerable<IDictionary<string, object>>> Data { get; set; }

        public MockDataLake(ILogger logger, string connectionString) : base(logger, connectionString)
        {
            Data = new Dictionary<string, IEnumerable<IDictionary<string, object>>>();
        }

        public override void Load(AbstractConfigurableCommand executable, LoaderInfo loader, IList<LoaderColumnInfo> columns)
        {
            if (loader.TargetBehaviour!=ConfigurableCommands.Loaders.TargetBehaviour.TRUNCATE && loader.TargetBehaviour != ConfigurableCommands.Loaders.TargetBehaviour.DROP)
            {
                throw new Exception("Target behaviour must be set to DROP or TRUNCATE.");
            }
            if (loader.RowProcessingBehaviour != ConfigurableCommands.Loaders.RowProcessingBehaviour.INSERT)
            {
                throw new Exception("Row processing behaviour must be set to INSERT.");
            }

            var name = loader.TargetName;
            var data = ((AbstractLoader)executable).ReadFormatted(columns);
            Data[name] = data;
        }

        public override IEnumerable<IDictionary<string, object>> Query(DatasetSummaryInfo dataset, int? top = 10000, int? sample = null, string filter = null, string param = null, string select = null, int? timeout = null)
        {
            var source = dataset.SourceName;
            var data = Data[source];
            dynamic state = null;
            var columns = dataset.Columns.Where(c => c.Public).Select(c => c.ColumnName);

            if (top.HasValue)
            {
                if (top < 0)
                {
                    throw new Exception("top cann be less than zero.");
                } else
                {
                    data = data.Take(top.Value);
                }
            }

            // optional filter
            Func<IDictionary<string, object>, bool> filterFunction;
            if (!string.IsNullOrEmpty(filter))
            {
                state = new DictionaryFilterParser().Parse(ParamsType.FILTER, filter, columns, state);
                filterFunction = ((IEnumerable<Func<IDictionary<string, object>, bool>>)state.FilterFunctions).First();
                data = data.Where(r => filterFunction(r));
            }

            // optional select parser
            if (!string.IsNullOrEmpty(select))
            {
                state = new DefaultSelectParser().Parse(select, columns);

                if (!state.IsGrouped())
                {
                    data = data.Select(r =>
                    {
                        IDictionary<string, object> newRow = new Dictionary<string, object>();
                        foreach (ColumnNode item in ((List<ColumnNode>)state.Result))
                        {
                            newRow[item.Alias] = r[item.ColumnName];
                        }
                        return newRow;
                    });
                } else
                {
                    throw new Exception("Mock data lake does not currently support grouping.");
                }
            }
            return sample.HasValue ? data.Sample(sample.Value) : data;
        }

        #region Writebacks

        public override IDictionary<string, object> GetRow(DatasetSummaryInfo dataset, int id)
        {
            var rows = Data[dataset.SourceName];

            var keyColumn = dataset.GetKeyColumn();
            var row = rows.FirstOrDefault(r => id.Equals(Convert.ToInt32(r[keyColumn.ColumnName])));

            if (row == null)
                throw new FileNotFoundException("Row not found.");

            return row;
        }

        public override IDictionary<string, object> UpdateRow(DatasetSummaryInfo dataset, int id, IDictionary<string, object> data)
        {
            // get id
            var rows = Data[dataset.SourceName];
            var keyColumn = dataset.GetKeyColumn();

            if (id != Convert.ToInt32(data[keyColumn.ColumnName])) {
                throw new FileNotFoundException("Invalid id.");
            }

            Data[dataset.SourceName] = Data[dataset.SourceName].Where(r=>id!=Convert.ToInt32(r[keyColumn.ColumnName])).Union(new[] { data });
            return data;
        }

        public override IDictionary<string, object> AddRow(DatasetSummaryInfo dataset, IDictionary<string, object> data)
        {
            // get id
            var rows = Data[dataset.SourceName];
            var keyColumn = dataset.GetKeyColumn();
            data[keyColumn.ColumnName] = rows.Max(r => Convert.ToInt32(r[keyColumn.ColumnName]))+1;
            Data[dataset.SourceName] = Data[dataset.SourceName].Union(new[] { data });
            return data;
        }

        public override void DeleteRow(DatasetSummaryInfo dataset, int id)
        {
            // get id
            var keyColumn = dataset.GetKeyColumn();
            Data[dataset.SourceName] = Data[dataset.SourceName].Where(r => id != Convert.ToInt32(r[keyColumn.ColumnName]));
        }


        #endregion

        #region Cache Server

        public override IEnumerable<ServerObjectInfo> GetObjects(string serverName, IEnumerable<string> databases)
        {
            var keys = Data.Keys;
            foreach (var key in keys)
            {
                yield return new ServerObjectInfo()
                {
                    ServerName = serverName,
                    ObjectName = key,
                    CreatedDt = DateTime.Now,
                    ModifiedDt = DateTime.Now,
                    UpdatedDt = DateTime.Now,
                    RowCount = Data[key].Count(),
                    Definition = "",
                    ObjectType = "TABLE"
                };
            }
        }

        public override IEnumerable<ServerColumnInfo> GetColumns(string serverName, IEnumerable<string> databases)
        {
            var keys = Data.Keys;
            foreach (var key in keys)
            {
                // Get the columns for the dataset
                var columns = Data[key].ProbeColumns().Select(c => new ServerColumnInfo
                {
                    ServerName = serverName,
                    ObjectName = key,
                    ColumnName = c.ColumnName,
                    DataLength = c.DataLength,
                    DataType = c.DataType,
                    Order = c.Order,
                    PrimaryKey = false,
                    Public = true,
                    ReadOnly = false,
                    Precision = null,
                    Scale = null
                });

                foreach (var item in columns)
                    yield return item;
            }
        }

        public override IEnumerable<ServerParameterInfo> GetParameters(string serverName, IEnumerable<string> databases)
        {
            return new List<ServerParameterInfo>();
        }

        public override IEnumerable<ServerDependencyInfo> GetDependencies(string serverName, IEnumerable<string> databases)
        {
            return new List<ServerDependencyInfo>();
        }

        #endregion

        #region Caching

        public override void CacheObject(string sourceName, string targetName, bool refreshSchema)
        {
            Data[targetName] = Data[sourceName];
        }

        #endregion

        #region Maintenance

        public override void Maintenance(string[] databaseBlacklist)
        {
            // Nothing to do here.
        }

        #endregion
    }
}
