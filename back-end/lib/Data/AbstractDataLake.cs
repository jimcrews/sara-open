using Sara.Lib.ConfigurableCommands;
using Sara.Lib.ConfigurableCommands.Loaders;
using Sara.Lib.Data;
using Sara.Lib.Extensions;
using Sara.Lib.Logging;
using Sara.Lib.Models.Dataset;
using Sara.Lib.Models.Loader;
using Sara.Lib.Models.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sara.Lib.Data
{
    public abstract class AbstractDataLake : IDataLake
    {
        protected ILogger Logger { get; set; }

        /// <summary>
        /// Cache copy of each IDataLake in a singleton.
        /// </summary>
        private static Dictionary<ServerInfo, IDataLake> Instances = new Dictionary<ServerInfo, IDataLake>();

        protected string ConnectionString { get; set; }

        public AbstractDataLake(ILogger logger, string connectionString)
        {
            this.Logger = logger;
            this.ConnectionString = connectionString;
        }
       
        /// <summary>
        /// Factory method to create a data lake instance based on a server config.
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public static IDataLake Create(ServerInfo server, ILogger logger=null)
        {
            if (logger == null)
                logger = new NullLogger();

            if (!Instances.ContainsKey(server))
            {
                Type t = Type.GetType(server.TypeName);
                var obj = Activator.CreateInstance(t, logger, server.ConnectionString);
                Instances[server] = (IDataLake)obj;
            }

            return Instances[server];
        }

        /// <summary>
        /// Returns the column structure of a Sara dataset. If no SELECT string is provided then
        /// this is equivalent to the dataset's base column definition. However, the API allows
        /// a SELECT parameter which can be used to call various functions / aggregation functions
        /// and also aliasing of columns. This method will determine the resulting column structure
        /// </summary>
        /// <param name="category"></param>
        /// <param name="dataset"></param>
        /// <param name="select"></param>
        /// <returns></returns>
        public IEnumerable<DataColumn> Schema(DatasetSummaryInfo dataset, string select)
        {
            if (string.IsNullOrEmpty(select))
            {
                // No SELECT specified. The columns are the same as defined on the dataset.
                return dataset.Columns.Select(c => (DataColumn)c);
            }
            else
            {
                List<DataColumn> result = new List<DataColumn>();

                // Need to parse the SELECT parameter to work out columns + types etc.
                select = select.ToUpper();

                var selectState = new DefaultSelectParser().Parse(select, dataset.Columns.Select(c => c.ColumnName));
                if (((List<ColumnNode>)selectState.Result).Any())
                {
                    var parsedColumns = (List<ColumnNode>)selectState.Result;

                    int order = 0;
                    (parsedColumns).ForEach(c => {
                        // Default data type
                        var dataType = dataset.Columns.First(col => col.ColumnName.Equals(c.ColumnName, StringComparison.OrdinalIgnoreCase)).DataType;
                        var dataLength = dataset.Columns.First(col => col.ColumnName.Equals(c.ColumnName, StringComparison.OrdinalIgnoreCase)).DataLength;

                        // Only functions that can change type are:
                        // COUNT - return Int32
                        // All other functions on a column value return the same type as the base column type, i.e:
                        // SUM,AVG,MIN,MAX,BIN
                        switch (c.Function)
                        {
                            case "COUNT":
                                dataType = DataType.Integer32;
                                dataLength = -1;
                                break;
                        }

                        result.Add(new DataColumn()
                        {
                            ColumnName = c.Alias,
                            DataType = dataType,
                            DataLength = dataLength,
                            PrimaryKey = false,
                            Order = order++,
                        });
                    });
                }
                return result;
            }
        }

        public abstract IEnumerable<IDictionary<string, object>> Query(DatasetSummaryInfo dataset, int? top = 10000, int? sample = null, string filter = null, string param = null, string select = null, int? timeout = null);

        public abstract void Load(AbstractConfigurableCommand configurableCommand, LoaderInfo loader, IList<LoaderColumnInfo> columns);

        #region Writeback

        public abstract IDictionary<string, object> GetRow(DatasetSummaryInfo dataset, int id);

        public abstract IDictionary<string, object> UpdateRow(DatasetSummaryInfo dataset, int id, IDictionary<string, object> data);

        public abstract IDictionary<string, object> AddRow(DatasetSummaryInfo dataset, IDictionary<string, object> data);

        public abstract void DeleteRow(DatasetSummaryInfo dataset, int id);

        #endregion

        #region Server Cache

        public abstract IEnumerable<ServerObjectInfo> GetObjects(string serverName, IEnumerable<string> databases);

        public abstract IEnumerable<ServerColumnInfo> GetColumns(string serverName, IEnumerable<string> databases);

        public abstract IEnumerable<ServerParameterInfo> GetParameters(string serverName, IEnumerable<string> databases);

        public abstract IEnumerable<ServerDependencyInfo> GetDependencies(string serverName, IEnumerable<string> databases);

        #endregion

        #region Caching

        public abstract void CacheObject(string sourceName, string targetName, bool refreshSchema);

        #endregion

        #region Maintenance

        public abstract void Maintenance(string[] databaseBlacklist);

        #endregion
    }
}
