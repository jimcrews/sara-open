using Sara.Lib.ConfigurableCommands;
using Sara.Lib.Cron;
using Sara.Lib.Data;
using Sara.Lib.Extensions;
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
    public abstract class AbstractMetadataRepository : IMetadataRepository
    {
        public abstract void AbortConfigurableCommand(int logId);

        public abstract ActionQueueInfo AddActionQueue(ActionQueueInfo item);

        public abstract void AddClass(ConfigurableCommandClassInfo configurableCommandClass);

        public abstract ConfigurableCommandInfo AddConfigurableCommand(ConfigurableCommandInfo configurableCommand);

        public abstract ConfigurableCommandScheduleInfo AddConfigurableCommandSchedule(ConfigurableCommandScheduleInfo schedule);

        public abstract DatasetInfo AddDataset(DatasetInfo dataset);

        public abstract void AddLoader(LoaderInfo loader);

        public abstract void AddProbeLoaderQueue(int configurableCommandId);

        public abstract void AddServer(ServerInfo server);

        public abstract int AuditBegin(string user, DateTime auditDt, string controller, string action, IDictionary<string, object> arguments);

        public abstract void AuditEnd(int auditId, TimeSpan duration);

        public abstract void DeleteActionQueue(int actionQueueId);

        public abstract IEnumerable<ConfigurableCommandLastLogInfo> ConfigurableCommandLastLogs();

        public abstract void DeleteClass(string configurableCommandClassName);

        public abstract void DeleteConfigurableCommand(int configurableCommandId);

        public abstract void DeleteConfigurableCommandSchedule(int configurableCommandScheduleId);

        public abstract void DeleteDataset(int datasetId);

        public abstract void DeleteNotebook(string category, string notebook);

        public abstract void DeleteProbeLoaderQueue(int configurableCommandId);

        public abstract void DeleteServer(string serverName);

        public abstract IEnumerable<ActionQueueInfo> GetActionQueue(string action = null, bool includeHistory = false);

        public abstract bool GetCancelledStatus(int logId);

        public abstract IEnumerable<ConfigurableCommandClassInfo> GetClasses();

        public abstract ConfigurableCommandInfo GetConfigurableCommand(int configurableCommandId);

        public abstract IEnumerable<ConfigurableCommandPreconditionInfo> GetConfigurableCommandPreconditions();

        public abstract IEnumerable<ConfigurableCommandPreconditionInfo> GetConfigurableCommandPreconditions(int configurableCommandId);

        public abstract IEnumerable<ConfigurableCommandProperty> GetConfigurableCommandProperties(int configurableCommandId);

        public abstract IEnumerable<ConfigurableCommandQueueInfo> GetConfigurableCommandQueue();

        public abstract IEnumerable<ConfigurableCommandInfo> GetConfigurableCommands();

        public abstract ConfigurableCommandScheduleInfo GetConfigurableCommandSchedule(int configurableCommandScheduleId);

        public abstract IEnumerable<ConfigurableCommandScheduleInfo> GetConfigurableCommandSchedules();

        public abstract IEnumerable<ConfigurableCommandScheduleInfo> GetConfigurableCommandSchedules(int configurableCommandId);

        public abstract IEnumerable<ConfigurationInfo> GetConfiguration();

        public abstract DatasetSummaryInfo GetDataset(string category, string dataset);

        public abstract DatasetInfo GetDataset(int datasetId);

        public abstract IEnumerable<DatasetAuditInfo> GetDatasetAudit(string category, string dataset, string action);

        public abstract IEnumerable<DatasetDependencyInfo> GetDatasetDependencies(string objectName, string direction);

        public abstract IEnumerable<DatasetInfo> GetDatasets(bool includeInactive = false);

        public abstract SaraEnvironment GetEnvironment();

        public abstract LoaderInfo GetLoader(int configurableCommandId);

        public abstract IList<LoaderColumnInfo> GetLoaderColumns(int configurableCommandId);

        public abstract NotebookInfo GetNotebook(string category, string notebook);

        public abstract IEnumerable<NotebookInfo> GetNotebooks(string category);

        public abstract string GetObjectDefinition(string objectName);

        public abstract IEnumerable<int> GetProbeLoaderQueue();

        public abstract IEnumerable<ServerInfo> GetServers();

        public abstract void Initialise();

        public abstract IEnumerable<ConfigurableCommandQueueInfo> LockDueQueueForExecution();

        public abstract void Log(LogEventArgs e);

        public abstract int LogHeader(string executableName, string className, string itemKey = null);

        public abstract void LogHeaderStatus(int logId, bool? successFlag);

        public abstract IEnumerable<LogItemInfo> LogItemsGet(int logId);

        public abstract IEnumerable<LogInfo> LogsGet(int daysHistory = 7, string logClass = null, string itemKey = null);

        public abstract void Maintenance();

        public abstract void OnRemoveOneTimeSchedule(int configurableCommandScheduleId);

        public abstract bool OnScheduleConfigurableCommand(Scheduler scheduler, int configurableCommandId);

        public abstract void RemoveQueue(int configurableCommandId);

        public abstract void ResetQueues(int? maximumCommandExecutionMins = 60);

        public abstract void SaveNotebook(string category, string notebook, string script, bool overwrite = false);

        public abstract void SaveServerColumns(IEnumerable<ServerColumnInfo> serverColumns);

        public abstract void SaveServerDependencies(IEnumerable<ServerDependencyInfo> serverDependencies);

        public abstract void SaveServerObjects(IEnumerable<ServerObjectInfo> serverObjects);

        public abstract void SaveServerParameters(IEnumerable<ServerParameterInfo> serverParameters);

        public abstract void ScheduleConfigurableCommand(int configurableCommandId);

        public abstract void SetActionQueueStatus(int actionQueueId, ActionQueueStatus actionQueueStatus, Exception ex = null);

        public abstract void SetConfigurableCommandPrecondition(ConfigurableCommandPreconditionInfo precondition);

        public abstract void SetConfigurableCommandProperty(int configurableCommandId, string propertyName, string propertyValue);

        public abstract void SetConfiguration(ConfigurationInfo item);

        public abstract void UnsetConfigurableCommandPrecondition(int configurableCommandId, int preconditionId);

        public abstract void UnsetConfigurableCommandProperty(int configurableCommandId, string propertyName);

        public abstract void UnsetConfiguration(string configurationName);

        public abstract void UpdateConfigurableCommand(int configurableCommandId, ConfigurableCommandInfo configurableCommand);

        public abstract void UpdateConfigurableCommandSchedule(int configurableCommandScheduleId, ConfigurableCommandScheduleInfo schedule);

        public abstract void UpdateDataset(int datasetId, DatasetInfo dataset);

        public abstract void UpdateLoader(int configurableCommandId, LoaderInfo loader);

        public abstract void UpdateLoaderColumns(int configurableCommandId, IEnumerable<LoaderColumnInfo> columns);
    }
}
