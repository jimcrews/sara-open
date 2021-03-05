using Sara.Lib.ConfigurableCommands;
using Sara.Lib.ConfigurableCommands.Actions;
using Sara.Lib.Cron;
using Sara.Lib.Logging;
using Sara.Lib.Models.Action;
using Sara.Lib.Models.ConfigurableCommand;
using Sara.Lib.Models.Configuration;
using Sara.Lib.Models.Dataset;
using Sara.Lib.Models.Loader;
using Sara.Lib.Models.Log;
using Sara.Lib.Models.Notebook;
using Sara.Lib.Models.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sara.Lib.Metadata
{
    /// <summary>
    /// The Sara Environment Type.
    /// </summary>
    public enum SaraEnvironment
    {
        Development,
        Testing,
        Production
    }

    /// <summary>
    /// Interface that describes the metadata repository / provider.
    /// </summary>
    public interface IMetadataRepository
    {
        #region Initialise

        /// <summary>
        /// Perform any start-up logic (e.g. creating database structure) here.
        /// </summary>
        void Initialise();

        #endregion

        #region Environment

        public SaraEnvironment GetEnvironment();

        #endregion

        #region Servers

        IEnumerable<ServerInfo> GetServers();
        void AddServer(ServerInfo server);
        void DeleteServer(string serverName);

        #endregion

        #region Configuration

        public IEnumerable<ConfigurationInfo> GetConfiguration();
        public void SetConfiguration(ConfigurationInfo item);
        public void UnsetConfiguration(string configurationName);

        #endregion

        #region Configurable Commands

        IEnumerable<ConfigurableCommandInfo> GetConfigurableCommands();    // Get all commands (for scheduler)
        ConfigurableCommandInfo GetConfigurableCommand(int configurableCommandId);
        ConfigurableCommandInfo AddConfigurableCommand(ConfigurableCommandInfo configurableCommand);
        void UpdateConfigurableCommand(int configurableCommandId, ConfigurableCommandInfo configurableCommand);
        void DeleteConfigurableCommand(int configurableCommandId);

        #endregion

        #region Configurable Commands - Actions

        IEnumerable<ActionQueueInfo> GetActionQueue(string action = null, bool includeHistory = false);
        ActionQueueInfo AddActionQueue(ActionQueueInfo item);
        void SetActionQueueStatus(int actionQueueId, ActionQueueStatus actionQueueStatus, Exception ex = null);
        void DeleteActionQueue(int actionQueueId);

        #endregion

        #region Configurable Commands - Loaders

        LoaderInfo GetLoader(int configurableCommandId);
        void AddLoader(LoaderInfo loader);
        void UpdateLoader(int configurableCommandId, LoaderInfo loader);
        IList<LoaderColumnInfo> GetLoaderColumns(int configurableCommandId);
        void UpdateLoaderColumns(int configurableCommandId, IEnumerable<LoaderColumnInfo> columns);
        IEnumerable<int> GetProbeLoaderQueue();
        void AddProbeLoaderQueue(int configurableCommandId);
        void DeleteProbeLoaderQueue(int configurableCommandId);

        #endregion

        #region Configurable Commands - Classes

        IEnumerable<ConfigurableCommandClassInfo> GetClasses();

        void AddClass(ConfigurableCommandClassInfo configurableCommandClass);

        void DeleteClass(string configurableCommandClassName);

        #endregion

        #region Configurable Commands - Properties

        /// <summary>
        /// Gets a list of raw configurable command properties to create a runnable instance.
        /// </summary>
        /// <param name="configurableCommandId"></param>
        /// <returns></returns>
        IEnumerable<ConfigurableCommandProperty> GetConfigurableCommandProperties(int configurableCommandId);
        void SetConfigurableCommandProperty(int configurableCommandId, string propertyName, string propertyValue);
        void UnsetConfigurableCommandProperty(int configurableCommandId, string propertyName);

        #endregion

        #region Configurable Commands - Schedules

        IEnumerable<ConfigurableCommandScheduleInfo> GetConfigurableCommandSchedules();
        IEnumerable<ConfigurableCommandScheduleInfo> GetConfigurableCommandSchedules(int configurableCommandId);
        ConfigurableCommandScheduleInfo GetConfigurableCommandSchedule(int configurableCommandScheduleId);
        ConfigurableCommandScheduleInfo AddConfigurableCommandSchedule(ConfigurableCommandScheduleInfo schedule);
        void UpdateConfigurableCommandSchedule(int configurableCommandScheduleId, ConfigurableCommandScheduleInfo schedule);
        void DeleteConfigurableCommandSchedule(int configurableCommandScheduleId);

        #endregion

        #region Configurable Commands - Preconditions

        IEnumerable<ConfigurableCommandPreconditionInfo> GetConfigurableCommandPreconditions();
        IEnumerable<ConfigurableCommandPreconditionInfo> GetConfigurableCommandPreconditions(int configurableCommandId);
        void SetConfigurableCommandPrecondition(ConfigurableCommandPreconditionInfo precondition);
        void UnsetConfigurableCommandPrecondition(int configurableCommandId, int preconditionId);

        #endregion

        #region Configurable Commands - Scheduling & Execution

        void ScheduleConfigurableCommand(int configurableCommandId);
        void AbortConfigurableCommand(int logId);
        IEnumerable<ConfigurableCommandQueueInfo> GetConfigurableCommandQueue();
        IEnumerable<ConfigurableCommandQueueInfo> LockDueQueueForExecution();
        void RemoveQueue(int configurableCommandId);
        bool OnScheduleConfigurableCommand(Scheduler scheduler, int configurableCommandId);
        void OnRemoveOneTimeSchedule(int configurableCommandScheduleId);
        void ResetQueues(int? maximumCommandExecutionMins = 60);

        #endregion

        #region Datasets

        /// <summary>
        /// Gets a list of dataset objects providing an optional list of authorisation claims.
        /// </summary>
        /// <returns></returns>
        IEnumerable<DatasetInfo> GetDatasets(bool includeInactive = false);

        /// <summary>
        /// Gets a single dataset. Includes additional information.
        /// Typically used for end-user scenarios.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="dataset"></param>
        /// <returns></returns>
        DatasetSummaryInfo GetDataset(string category, string dataset);

        DatasetInfo GetDataset(int datasetId);

        DatasetInfo AddDataset(DatasetInfo dataset);

        void UpdateDataset(int datasetId, DatasetInfo dataset);

        void DeleteDataset(int datasetId);

        /// <summary>
        /// Gets the audit entries for a specific action for a dataset.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="dataset"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        IEnumerable<DatasetAuditInfo> GetDatasetAudit(string category, string dataset, string action);

        /// <summary>
        /// Gets the dependency information for a dataset.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="dataset"></param>
        /// <param name="direction">either UP or DOWN</param>
        /// <returns></returns>
        IEnumerable<DatasetDependencyInfo> GetDatasetDependencies(string objectName, string direction);

        /// <summary>
        /// Gets the object definition (e.g. create SQL text).
        /// </summary>
        /// <param name="objectName"></param>
        /// <returns></returns>
        string GetObjectDefinition(string objectName);

        #endregion

        #region Logging

        /// <summary>
        /// Logs a new header record and returns new log id value.
        /// </summary>
        /// <param name="executableName"></param>
        /// <param name="className"></param>
        /// <param name="itemKey"></param>
        /// <returns></returns>
        int LogHeader(string executableName, string className, string itemKey = null);
        void Log(LogEventArgs e);
        /// <summary>
        /// Logs success/failure of a log.
        /// </summary>
        /// <param name="logId"></param>
        /// <param name="successFlag"></param>
        void LogHeaderStatus(int logId, bool? successFlag);
        bool GetCancelledStatus(int logId);
        IEnumerable<LogInfo> LogsGet(int daysHistory = 7, string logClass = null, string itemKey = null);
        IEnumerable<LogItemInfo> LogItemsGet(int logId);
        IEnumerable<ConfigurableCommandLastLogInfo> ConfigurableCommandLastLogs();

        #endregion

        #region Server Cache

        void SaveServerObjects(IEnumerable<ServerObjectInfo> serverObjects);
        void SaveServerColumns(IEnumerable<ServerColumnInfo> serverColumns);
        void SaveServerParameters(IEnumerable<ServerParameterInfo> serverParameters);
        void SaveServerDependencies(IEnumerable<ServerDependencyInfo> serverDependencies);

        #endregion

        #region Auditing

        int AuditBegin(string user, DateTime auditDt, string controller, string action, IDictionary<string, object> arguments);

        void AuditEnd(int auditId, TimeSpan duration);

        #endregion

        #region Notebooks

        /// <summary>
        /// Gets a list of notebooks for a category.
        /// </summary>
        /// <param name="category">The category name</param>
        /// <returns></returns>
        IEnumerable<NotebookInfo> GetNotebooks(string category);

        /// <summary>
        /// Gets a single notebook.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="notebook"></param>
        /// <returns></returns>
        NotebookInfo GetNotebook(string category, string notebook);

        /// <summary>
        /// Saves a notebook
        /// </summary>
        /// <param name="category"></param>
        /// <param name="notebook"></param>
        /// <param name="script"></param>
        void SaveNotebook(string category, string notebook, string script, bool overwrite = false);

        /// <summary>
        /// Deletes a notebook.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="notebook"></param>
        void DeleteNotebook(string category, string notebook);

        #endregion

        #region Maintenance

        void Maintenance();

        #endregion
    }
}
