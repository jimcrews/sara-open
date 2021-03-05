using Microsoft.CodeAnalysis;
using Sara.Lib.ConfigurableCommands;
using Sara.Lib.Cron;
using Sara.Lib.Logging;
using Sara.Lib.Metadata;
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Sara.Lib.Metadata
{
    /// <summary>
    /// Mocks all data required for a mock server implementation.
    /// </summary>
    public class MockRepository
    {
        public IList<ServerInfo> Servers { get; set; }
        public IList<ConfigurationInfo> Configuration { get; set; }
        public IList<ConfigurableCommandClassInfo> Classes { get; set; }
        public IList<ConfigurableCommandInfo> Commands { get; set; }
        public IList<LoaderInfo> Loaders { get; set; }
        public IList<LoaderColumnInfo> LoaderColumns { get; set; }
        public IList<ConfigurableCommandProperty> Properties {get; set;}
        public IList<DatasetInfo> Datasets { get; set; }
        public IList<ServerObjectInfo> ServerObjects { get; set; }
        public IList<ServerColumnInfo> ServerColumns { get; set; }
        public IList<ServerParameterInfo> ServerParameters { get; set; }
        public IList<ServerDependencyInfo> ServerDependencies { get; set; }
        public IList<ConfigurableCommandQueueInfo> Queue { get; set; }
        public IList<ConfigurableCommandScheduleInfo> Schedules { get; set; }
        public IList<ConfigurableCommandPreconditionInfo> Preconditions { get; set; }
        public IList<ActionQueueInfo> ActionQueue { get; set; }
        public IList<NotebookInfo> Notebooks { get; set; }
    }

    /// <summary>
    /// Mock metadata repository. Provides all objects via a static in-memory array
    /// </summary>
    public class MockMetadataRepository : AbstractMetadataRepository
    {
        // Store data
        MockRepository Repository = new MockRepository();

        #region Initialise

        public override void Initialise()
        {
            Repository.Servers = new List<ServerInfo>();
            Repository.Configuration = new List<ConfigurationInfo>();
            Repository.Classes = new List<ConfigurableCommandClassInfo>();
            Repository.Commands = new List<ConfigurableCommandInfo>();
            Repository.Loaders = new List<LoaderInfo>();
            Repository.LoaderColumns = new List<LoaderColumnInfo>();
            Repository.Datasets = new List<DatasetInfo>();
            Repository.Preconditions = new List<ConfigurableCommandPreconditionInfo>();
            Repository.Properties = new List<ConfigurableCommandProperty>();
            Repository.Queue = new List<ConfigurableCommandQueueInfo>();
            Repository.Schedules = new List<ConfigurableCommandScheduleInfo>();

            Repository.ServerObjects = new List<ServerObjectInfo>();
            Repository.ServerColumns = new List<ServerColumnInfo>();
            Repository.ServerParameters = new List<ServerParameterInfo>();
            Repository.ServerDependencies = new List<ServerDependencyInfo>();

            Repository.ActionQueue = new List<ActionQueueInfo>();

            Repository.Notebooks = new List<NotebookInfo>();
        }

        #endregion

        #region Environment

        public override SaraEnvironment GetEnvironment()
        {
            return SaraEnvironment.Development;
        }

        #endregion

        #region Servers

        public override IEnumerable<ServerInfo> GetServers()
        {
            return new List<ServerInfo>()
            {
                new ServerInfo()
                {
                    ServerName="Demo",
                    TypeName="Sara.Lib.Data.Mock.MockDataLake,Sara.Lib",
                    ConnectionString=null
                }
            };
        }

        public override void AddServer(ServerInfo server)
        {
            Repository.Servers.Add(server);
        }

        public override void DeleteServer(string serverName)
        {
            ServerInfo server = Repository.Servers.FirstOrDefault(s => s.ServerName.Equals(serverName, StringComparison.OrdinalIgnoreCase));

            if (server != null)
            {
                Repository.Servers.Remove(server);
            }
        }

        #endregion

        #region Configuration

        public override IEnumerable<ConfigurationInfo> GetConfiguration()
        {
            return Repository.Configuration;
        }

        public override void SetConfiguration(ConfigurationInfo item)
        {
            var existing = GetConfiguration().FirstOrDefault(c => c.ConfigurationName.Equals(item.ConfigurationName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                Repository.Configuration.Remove(existing);
            Repository.Configuration.Add(item);
        }

        public override void UnsetConfiguration(string configurationName)
        {
            var existing = GetConfiguration().FirstOrDefault(c => c.ConfigurationName.Equals(configurationName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                Repository.Configuration.Remove(existing);
        }

        #endregion

        #region Configurable Commands

        public override IEnumerable<ConfigurableCommandInfo> GetConfigurableCommands()
        {
            return Repository.Commands;
        }

        public override ConfigurableCommandInfo GetConfigurableCommand(int configurableCommandId)
        {
            if (!Repository.Commands.Any(c => c.ConfigurableCommandId == configurableCommandId))
            {
                throw new FileNotFoundException("Command not found.");
            }
            return Repository.Commands.First(c => c.ConfigurableCommandId == configurableCommandId);
        }

        public override ConfigurableCommandInfo AddConfigurableCommand(ConfigurableCommandInfo configurableCommand)
        {
            int? nextId = Repository.Commands.Max(c => c.ConfigurableCommandId) + 1 ?? 1;
            configurableCommand.ConfigurableCommandId = nextId;
            Repository.Commands.Add(configurableCommand);
            return configurableCommand;
        }

        public override void UpdateConfigurableCommand(int configurableCommandId, ConfigurableCommandInfo configurableCommand)
        {
            var oldCommand = Repository.Commands.FirstOrDefault(c => c.ConfigurableCommandId == configurableCommandId);

            if (oldCommand == null)
            {
                throw new FileNotFoundException("Command not found.");
            }

            // remove old object and replace with new one
            Repository.Commands.Remove(oldCommand);
            Repository.Commands.Add(configurableCommand);
        }

        public override void DeleteConfigurableCommand(int configurableCommandId)
        {
            var oldCommand = Repository.Commands.FirstOrDefault(c => c.ConfigurableCommandId == configurableCommandId);

            if (oldCommand == null)
            {
                throw new FileNotFoundException("Command not found.");
            }

            // remove old object and replace with new one
            Repository.Commands.Remove(oldCommand);
        }

        #endregion

        #region Configurable Commands - Actions

        public override IEnumerable<ActionQueueInfo> GetActionQueue(string action = null, bool includeHistory = false)
        {
            return Repository.ActionQueue.Where(
                aq =>
                (action != null && aq.Action.Equals(action, StringComparison.OrdinalIgnoreCase) || action == null) &&
                (includeHistory = true || aq.Status == ActionQueueStatus.Ready));
        }

        public override ActionQueueInfo AddActionQueue(ActionQueueInfo item)
        {
            var id = Repository.ActionQueue.Select(aq => aq.ActionQueueId).Max() + 1;
            item.ActionQueueId = id;
            Repository.ActionQueue.Add(item);
            return item;
        }

        public override void SetActionQueueStatus(int actionQueueId, ActionQueueStatus actionQueueStatus, Exception ex = null)
        {
            var aq = Repository.ActionQueue.FirstOrDefault(aq => aq.ActionQueueId == actionQueueId);
            if (aq == null)
                throw new FileNotFoundException("Action queue not found.");

            aq.Status = actionQueueStatus;
            if (ex != null)
                aq.Exception = ex.Message;
        }

        public override void DeleteActionQueue(int actionQueueId)
        {
            var queue = Repository.ActionQueue.First(aq => aq.ActionQueueId==actionQueueId);
            Repository.ActionQueue.Remove(queue);
        }

        #endregion

        #region Configurable Commands - Loaders

        public override LoaderInfo GetLoader(int configurableCommandId)
        {
            var loader = Repository.Loaders.FirstOrDefault(l => l.ConfigurableCommandId == configurableCommandId);
            if (loader == null)
                throw new FileNotFoundException("Loader not found.");

            return loader;
        }

        public override void AddLoader(LoaderInfo loader)
        {
            Repository.Loaders.Add(loader);
        }

        public override void UpdateLoader(int configurableCommandId, LoaderInfo loader)
        {
            if (loader.ConfigurableCommandId != configurableCommandId)
            {
                throw new FileNotFoundException("Invalid loader id");
            }
            var l = Repository.Loaders.First(l => l.ConfigurableCommandId == configurableCommandId);
            if (l == null)
            {
                throw new FileNotFoundException("Invalid loader id");
            }
            Repository.Loaders.Remove(l);
            Repository.Loaders.Add(loader);
        }

        public override IList<LoaderColumnInfo> GetLoaderColumns(int configurableCommandId)
        {
            return Repository
                .LoaderColumns
                .Where(lc => lc.ConfigurableCommandId == configurableCommandId)
                .OrderBy(l => l.Order)
                .ToList();
        }

        public override void UpdateLoaderColumns(int configurableCommandId, IEnumerable<LoaderColumnInfo> columns)
        {
            if (columns.Any(c => c.ConfigurableCommandId != configurableCommandId))
            {
                throw new FileNotFoundException("Columns have invalid configurable command id.");
            }

            var repo = Repository.LoaderColumns.Where(lc => lc.ConfigurableCommandId == configurableCommandId);
            foreach (var item in repo)
                Repository.LoaderColumns.Remove(item);

            foreach (var item in columns)
                Repository.LoaderColumns.Add(item);
        }

        public override IEnumerable<int> GetProbeLoaderQueue()
        {
            throw new NotImplementedException();
        }

        public override void AddProbeLoaderQueue(int configurableCommandId)
        {
            throw new NotImplementedException();
        }

        public override void DeleteProbeLoaderQueue(int configurableCommandId)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Configurable Commands - Classes

        public override IEnumerable<ConfigurableCommandClassInfo> GetClasses()
        {
            return Repository.Classes;
        }

        public override void AddClass(ConfigurableCommandClassInfo configurableCommandClass)
        {
            Repository.Classes.Add(configurableCommandClass);
        }

        public override void DeleteClass(string configurableCommandClassName)
        {
            ConfigurableCommandClassInfo configurableClass = Repository.Classes.FirstOrDefault(c => c.ClassName.Equals(configurableCommandClassName, StringComparison.OrdinalIgnoreCase));

            if (configurableClass != null)
            {
                Repository.Classes.Remove(configurableClass);
            }
        }

        #endregion

        #region Configurable Commands - Properties

        public override IEnumerable<ConfigurableCommandProperty> GetConfigurableCommandProperties(int configurableCommandId)
        {
            return Repository.Properties.Where(ccp => ccp.ConfigurableCommandId == configurableCommandId);
        }

        public override void SetConfigurableCommandProperty(int configurableCommandId, string propertyName, string propertyValue)
        {
            var cc = GetConfigurableCommand(configurableCommandId);

            var prop = Repository.Properties.FirstOrDefault(p => p.ConfigurableCommandId == configurableCommandId && p.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
            if (prop != null)
            {
                Repository.Properties.Remove(prop);
            }

            Repository.Properties.Add(new ConfigurableCommandProperty()
            {
                ConfigurableCommandId = configurableCommandId,
                PropertyName = propertyName,
                PropertyValue = propertyValue
            });
        }

        public override void UnsetConfigurableCommandProperty(int configurableCommandId, string propertyName)
        {
            var prop = Repository.Properties.FirstOrDefault(p => p.ConfigurableCommandId == configurableCommandId && p.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
            if (prop != null)
            {
                Repository.Properties.Remove(prop);
            }
        }

        #endregion

        #region Configurable Commands - Schedules

        public override IEnumerable<ConfigurableCommandScheduleInfo> GetConfigurableCommandSchedules()
        {
            return Repository.Schedules;
        }

        public override IEnumerable<ConfigurableCommandScheduleInfo> GetConfigurableCommandSchedules(int configurableCommandId)
        {
            var cc = GetConfigurableCommand(configurableCommandId);
            return GetConfigurableCommandSchedules().Where(s => s.ConfigurableCommandId == cc.ConfigurableCommandId);
        }

        public override ConfigurableCommandScheduleInfo GetConfigurableCommandSchedule(int configurableCommandScheduleId)
        {
            var schedule = Repository.Schedules.FirstOrDefault(s => s.ConfigurableCommandScheduleId == configurableCommandScheduleId);
            if (schedule == null)
                throw new FileNotFoundException("Schedule not found.");

            return schedule;
        }

        public override ConfigurableCommandScheduleInfo AddConfigurableCommandSchedule(ConfigurableCommandScheduleInfo schedule)
        {
            schedule.ConfigurableCommandScheduleId = Repository.Schedules.Max(s => s.ConfigurableCommandScheduleId) + 1 ?? 1;
            Repository.Schedules.Add(schedule);
            return schedule;
        }

        public override void UpdateConfigurableCommandSchedule(int configurableCommandScheduleId, ConfigurableCommandScheduleInfo schedule)
        {
            var cc = GetConfigurableCommand(configurableCommandScheduleId);
            if (schedule.ConfigurableCommandId != cc.ConfigurableCommandId)
                throw new FileNotFoundException("Configurable command not found.");

            var s = Repository.Schedules.FirstOrDefault(s => s.ConfigurableCommandScheduleId == schedule.ConfigurableCommandScheduleId);
            if (s == null)
                throw new FileNotFoundException("Schedule not found.");

            Repository.Schedules.Remove(s);
            Repository.Schedules.Add(schedule);
        }

        public override void DeleteConfigurableCommandSchedule(int configurableCommandScheduleId)
        {
            var s = Repository.Schedules.FirstOrDefault(s => s.ConfigurableCommandScheduleId == configurableCommandScheduleId);
            if (s == null)
                throw new FileNotFoundException("Schedule not found.");

            Repository.Schedules.Remove(s);
        }

        #endregion

        #region Configurable Commands - Preconditions

        public override IEnumerable<ConfigurableCommandPreconditionInfo> GetConfigurableCommandPreconditions()
        {
            return Repository.Preconditions;
        }

        public override IEnumerable<ConfigurableCommandPreconditionInfo> GetConfigurableCommandPreconditions(int configurableCommandId)
        {
            throw new NotImplementedException();
        }

        public override void SetConfigurableCommandPrecondition(ConfigurableCommandPreconditionInfo precondition)
        {
            throw new NotImplementedException();
        }

        public override void UnsetConfigurableCommandPrecondition(int configurableCommandId, int preconditionId)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Configurable Commands - Scheduling & Execution

        public override void ScheduleConfigurableCommand(int configurableCommandId)
        {
            throw new NotImplementedException();
        }

        public override void AbortConfigurableCommand(int logId)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<ConfigurableCommandQueueInfo> GetConfigurableCommandQueue()
        {
            return Repository.Queue;
        }

        public override IEnumerable<ConfigurableCommandQueueInfo> LockDueQueueForExecution()
        {
            throw new NotImplementedException();
        }

        public override void RemoveQueue(int configurableCommandId)
        {
            throw new NotImplementedException();
        }

        public override bool OnScheduleConfigurableCommand(Scheduler scheduler, int configurableCommandId)
        {
            var nextDt = scheduler.NextDt[configurableCommandId];
            var existing = Repository.Queue.FirstOrDefault(c => c.ConfigurableCommandId == configurableCommandId);
            if (existing != null)
                Repository.Queue.Remove(existing);

            Repository.Queue.Add(new ConfigurableCommandQueueInfo
            {
                ConfigurableCommandId = configurableCommandId,
                NextDt = nextDt,
                Running = false
            });
            return true;
        }

        public override void OnRemoveOneTimeSchedule(int configurableCommandScheduleId)
        {
            var schedule = Repository.Schedules.FirstOrDefault(s => s.ConfigurableCommandScheduleId == configurableCommandScheduleId);
            if (schedule != null)
                Repository.Schedules.Remove(schedule);
        }

        public override void ResetQueues(int? maximumCommandExecutionMins = 60)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Datasets

        public override IEnumerable<DatasetInfo> GetDatasets(bool includeInactive = false)
        {
            return Repository.Datasets.Where(d => d.Active || includeInactive);
        }

        public override DatasetSummaryInfo GetDataset(string category, string dataset)
        {
            var ds = Repository.Datasets.FirstOrDefault(ds => ds.Category.Equals(category, StringComparison.OrdinalIgnoreCase) && ds.Dataset.Equals(dataset, StringComparison.OrdinalIgnoreCase));

            if (ds == null)
                throw new FileNotFoundException("Dataset not found.");
            return new DatasetSummaryInfo()
            {
                DatasetId = ds.DatasetId,
                Description = ds.Description,
                DisplayGroup = ds.DisplayGroup,
                Category = ds.Category,
                ServerName = ds.ServerName,
                SourceName = ds.SourceName,
                Active = ds.Active,
                Dataset = ds.Dataset,
                Columns = Repository.ServerColumns.Where(
                    c =>
                    c.ObjectName.Equals(ds.SourceName, StringComparison.OrdinalIgnoreCase) &&
                    c.ServerName.Equals(ds.ServerName, StringComparison.OrdinalIgnoreCase))
            };
        }

        public override DatasetInfo GetDataset(int datasetId)
        {
            if (!Repository.Datasets.Any(d => d.DatasetId == datasetId))
                throw new FileNotFoundException("Dataset not found");

            return Repository.Datasets.First(d => d.DatasetId == datasetId);
        }

        public override DatasetInfo AddDataset(DatasetInfo dataset)
        {
            var id = Repository.Datasets.Max(ds => ds.DatasetId) + 1 ?? 1;
            dataset.DatasetId = id;
            Repository.Datasets.Add(dataset);
            return dataset;
        }

        public override void UpdateDataset(int datasetId, DatasetInfo dataset)
        {
            var ds = Repository.Datasets.FirstOrDefault(ds => ds.DatasetId == datasetId);
            if (ds == null)
                throw new FileNotFoundException("Dataset not found.");

            if (dataset.DatasetId!=datasetId)
                throw new FileNotFoundException("Dataset not found.");

            Repository.Datasets.Remove(ds);
            Repository.Datasets.Add(dataset);
        }

        public override void DeleteDataset(int datasetId)
        {
            var dataset = Repository.Datasets.FirstOrDefault(ds => ds.DatasetId == datasetId);
            if (dataset == null)
                throw new FileNotFoundException("Dataset not found.");

            Repository.Datasets.Remove(dataset);
        }


        public override IEnumerable<DatasetAuditInfo> GetDatasetAudit(string category, string dataset, string action)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<DatasetDependencyInfo> GetDatasetDependencies(string objectName, string direction)
        {
            throw new NotImplementedException();
        }

        public override string GetObjectDefinition(string objectName)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Logging

        public override int LogHeader(string executableName, string className, string itemKey = null)
        {
            throw new NotImplementedException();
        }

        public override void Log(LogEventArgs e)
        {
            throw new NotImplementedException();
        }

        public override void LogHeaderStatus(int logId, bool? successFlag)
        {
            throw new NotImplementedException();
        }

        public override bool GetCancelledStatus(int logId)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<LogInfo> LogsGet(int daysHistory = 7, string logClass = null, string itemKey = null)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<LogItemInfo> LogItemsGet(int logId)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<ConfigurableCommandLastLogInfo> ConfigurableCommandLastLogs()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Server Cache

        public override void SaveServerObjects(IEnumerable<ServerObjectInfo> serverObjects)
        {
            Repository.ServerObjects = serverObjects.ToList();
        }

        public override void SaveServerColumns(IEnumerable<ServerColumnInfo> serverColumns)
        {
            Repository.ServerColumns = serverColumns.ToList();
        }

        public override void SaveServerParameters(IEnumerable<ServerParameterInfo> serverParameters)
        {
            Repository.ServerParameters = serverParameters.ToList();
        }

        public override void SaveServerDependencies(IEnumerable<ServerDependencyInfo> serverDependencies)
        {
            Repository.ServerDependencies = serverDependencies.ToList();
        }

        #endregion

        #region Auditing

        public override int AuditBegin(string user, DateTime auditDt, string controller, string action, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException();
        }

        public override void AuditEnd(int auditId, TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Notebooks

        public override IEnumerable<NotebookInfo> GetNotebooks(string category)
        {
            return Repository.Notebooks.Where(n => n.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        public override NotebookInfo GetNotebook(string category, string notebook)
        {
            var nb = Repository.Notebooks.FirstOrDefault(n => n.Category.Equals(category, StringComparison.OrdinalIgnoreCase) && n.Notebook.Equals(notebook, StringComparison.OrdinalIgnoreCase));
            if (nb == null)
            {
                throw new FileNotFoundException("Notebook not found.");
            }
            return nb;
        }

        public override void SaveNotebook(string category, string notebook, string script, bool overwrite = false)
        {
            var nb = Repository.Notebooks.FirstOrDefault(n => n.Category.Equals(category, StringComparison.OrdinalIgnoreCase) && n.Notebook.Equals(notebook, StringComparison.OrdinalIgnoreCase));
            if (overwrite && nb != null) {
                Repository.Notebooks.Remove(nb);
                nb = null;
            }

            if (nb == null)
            {
                Repository.Notebooks.Add(new NotebookInfo
                {
                    Category = category,
                    Notebook = notebook,
                    Script = script
                });
            } else
            {
                throw new Exception("Notebook already exists. If you wish to overwrite existing notebook, set overwrite flag to true.");
            }
        }

        public override void DeleteNotebook(string category, string notebook)
        {
            var nb = Repository.Notebooks.FirstOrDefault(n => n.Category.Equals(category, StringComparison.OrdinalIgnoreCase) && n.Notebook.Equals(notebook, StringComparison.OrdinalIgnoreCase));
            if (nb == null)
            {
                throw new FileNotFoundException("Notebook not found.");
            }
        }

        #endregion

        #region Maintenance

        public override void Maintenance()
        {
            // Nothing to do here.
        }

        #endregion
    }
}
