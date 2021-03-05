using Sara.Lib.ConfigurableCommands;
using Sara.Lib.ConfigurableCommands.Loaders;
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
    /// <summary>
    /// Describes the methods applicable to a data lake.
    /// </summary>
    public interface IDataLake
    {

        /// <summary>
        /// Returns the column metadata based on a Sara API query. This is useful for downstream
        /// BI tools like Power BI that need to know the column structure for a specific
        /// query from Sara.
        /// This is different to the base column structure that is returned from the
        /// IMetadataRepository.GetDataset(category, dataset).
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="select"></param>
        /// <returns></returns>
        IEnumerable<DataColumn> Schema(DatasetSummaryInfo dataset, string select);

        /// <summary>
        /// Query data from a data lake.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IDictionary<string, object>> Query(
            DatasetSummaryInfo dataset,
            int? top = null,
            int? sample = null,
            string filter = null,
            string param = null,
            string select = null,
            int? timeout = null);

        /// <summary>
        /// Saves data to data lake by executing a loader
        /// </summary>
        /// <param name="data"></param>
        void Load(AbstractConfigurableCommand executable, LoaderInfo loader, IList<LoaderColumnInfo> columns);

        #region Writebacks

        IDictionary<string, object> GetRow(DatasetSummaryInfo dataset, int id);

        IDictionary<string, object> UpdateRow(DatasetSummaryInfo dataset, int id, IDictionary<string, object> data);

        IDictionary<string, object> AddRow(DatasetSummaryInfo dataset, IDictionary<string, object> data);

        void DeleteRow(DatasetSummaryInfo dataset, int id);

        #endregion

        #region Server Cache

        IEnumerable<ServerObjectInfo> GetObjects(string serverName, IEnumerable<string> databases);

        IEnumerable<ServerColumnInfo> GetColumns(string serverName, IEnumerable<string> databases);

        IEnumerable<ServerParameterInfo> GetParameters(string serverName, IEnumerable<string> databases);

        IEnumerable<ServerDependencyInfo> GetDependencies(string serverName, IEnumerable<string> databases);

        #endregion

        #region Caching

        /// <summary>
        /// Server-specific code to cache/materialise data. For MSSQL provider, this would be materialising a view.
        /// </summary>
        /// <param name="sourceName"></param>
        /// <param name="targetName"></param>
        /// <param name="refreshSchema"></param>
        void CacheObject(string sourceName, string targetName, bool refreshSchema);

        #endregion

        #region Maintenance

        /// <summary>
        /// Performs any provider-specific data lake maintenance.
        /// </summary>
        /// <param name="databaseBlacklist">List of databases that maintenance should not be performed on.</param>
        void Maintenance(string[] databaseBlacklist);

        #endregion
    }
}
