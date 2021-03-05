using Sara.Lib.ConfigurableCommands;
using Sara.Lib.ConfigurableCommands.Actions;
using Sara.Lib.Data;
using Sara.Lib.Logging;
using Sara.Lib.Metadata;
using Sara.Lib.Models.ConfigurableCommand;
using MailKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Sara.Lib.Extensions;
using System.Dynamic;
using Newtonsoft.Json.Converters;
using Sara.Lib.ConfigurableCommands.Loaders;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;

namespace Sara.Lib.ConfigurableCommands
{
    /// <summary>
    /// Fully self-contained command launcher.
    /// </summary>
    public class ConfigurableCommandLauncher
    {
        System.Timers.Timer Timer;
        DateTime StartTime;

        IServiceLogger ServiceLogger;
        IMetadataRepository MetadataRepository;
        string Environment;
        int? ConfigurableCommandId;
        string ConfigurableCommandClass;
        string ConfigurableCommandProperties;
        IEnumerable<ConfigurableCommandPreconditionInfo> Preconditions;
        IEnumerable<ConfigurableCommandLastLogInfo> LastLogs;
        string UserContext;
        int? TimeoutMins;
        Action AbortHandler;

        public ConfigurableCommandLauncher(
            IServiceLogger serviceLogger,
            IMetadataRepository metadataRepository,
            string environment,
            int? configurableCommandId,
            string configurableCommandClass,
            string configurableCommandProperties,
            string userContext,
            int? timeoutMins,
            Action abortHandler
            )
        {
            this.ServiceLogger = serviceLogger;
            this.MetadataRepository = metadataRepository;
            this.Environment = environment;
            this.ConfigurableCommandId = configurableCommandId;
            this.ConfigurableCommandClass = configurableCommandClass;
            this.ConfigurableCommandProperties = configurableCommandProperties;
            this.Preconditions = metadataRepository.GetConfigurableCommandPreconditions();
            this.LastLogs = metadataRepository.ConfigurableCommandLastLogs();
            this.UserContext = userContext;
            this.TimeoutMins = timeoutMins;
            this.AbortHandler = abortHandler;
        }

        public void Launch()
        {
            string logExecutable = this.GetType().Assembly.GetName().Name;
            string logClass = this.GetType().Name;

            // Do the actual work in try/catch
            try
            {
                // Get the key - either the configurable command id or the configurable command class
                var key = (ConfigurableCommandId.HasValue ? ConfigurableCommandId.ToString() : null) ?? ConfigurableCommandClass;
                ServiceLogger.LogHeader(logExecutable, logClass, key);

                // Check preconditions before running
                CheckPreconditionSuccessStatus();

                // Set up cancellation check
                if (TimeoutMins.HasValue)
                    SetupCancellationTimer();

                Execute(Environment, ConfigurableCommandId, ConfigurableCommandClass, ConfigurableCommandProperties, ServiceLogger);

                // Log completion
                ServiceLogger.Log(LogType.SUCCESS, "Execution completed successfully.");
                ServiceLogger.LogHeaderSuccess(true);
            }
            catch (Exception ex)
            {
                ServiceLogger.Log(ex);
                ServiceLogger.LogHeaderSuccess(false);
                throw;
            }
        }

        /// <summary>
        /// Sets up the cancellation check timer.
        /// Check the database audit table - If the cancel flag
        /// has been set, then abort this process. This is mechanism
        /// for users / administrators to abort running loaders + actions.
        /// </summary>
        private void SetupCancellationTimer()
        {
            StartTime = DateTime.Now;
            Timer = new System.Timers.Timer();
            Timer.AutoReset = false;
            Timer.Interval = 2 * 60 * 1000; // 2 minutes
            Timer.Elapsed += Timer_Elapsed;
            Timer.Start();
        }

        /// <summary>
        /// Cancellation timer handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Command cannot run > 60 minutes
            TimeSpan ts = DateTime.Now - StartTime;
            if (ts.TotalMinutes >= TimeoutMins)
            {
                AbortHandler();
            }

            // Check for manual cancellation
            var cancelled = MetadataRepository.GetCancelledStatus(ServiceLogger.LogId);
            if (cancelled)
            {
                AbortHandler();
            }
            Timer.Start();
        }

        private void Execute(
            string environment,
            int? commandId,
            string commandClass,
            string commandProperties,
            ILogger logger
            )
        {
            // Get the configurable command class
            ConfigurableCommandClassInfo configurableCommandClass = null;
            if (!string.IsNullOrEmpty(commandClass))
            {
                configurableCommandClass = MetadataRepository.GetClasses().First(c => c.ClassName.Equals(commandClass, StringComparison.OrdinalIgnoreCase));
            }

            // Get the configurable command
            ConfigurableCommandInfo configurableCommand = null;
            IEnumerable<ConfigurableCommandProperty> configurableCommandProperties = null;
            if (commandId != null)
            {
                configurableCommand = MetadataRepository.GetConfigurableCommand(commandId.Value);
                configurableCommandClass = MetadataRepository.GetClasses().First(c => c.ClassName.Equals(configurableCommand.ClassName, StringComparison.OrdinalIgnoreCase));
                configurableCommandProperties = MetadataRepository.GetConfigurableCommandProperties(configurableCommand.ConfigurableCommandId.Value).ToList();
            }

            // Merge any run-time properties provided with those relating to the command id
            if (!string.IsNullOrEmpty(commandProperties))
            {
                Dictionary<string, object> runTimeProps = new Dictionary<string, object>();
                // Get the passed in run-time properties (optional)
                List<ConfigurableCommandProperty> cp = new List<ConfigurableCommandProperty>();
                var obj = (JObject)JsonConvert.DeserializeObject(commandProperties);

                foreach (var token in obj.Children())
                {
                    if (token is JProperty)
                    {
                        var prop = token as JProperty;
                        runTimeProps.Add(prop.Name, prop.Value);
                    }
                }

                var configuredProps = configurableCommandProperties!=null ? configurableCommandProperties.ToDictionary(ccp => ccp.PropertyName, ccp => ccp.PropertyValue) : null;

                var mergedProps = configuredProps.Extend(runTimeProps);
                configurableCommandProperties = mergedProps.Select(kv => new ConfigurableCommandProperty
                {
                    PropertyName = kv.Key,
                    PropertyValue = kv.Value.ToString()
                });
            }

            // Should have a class here
            if (configurableCommandClass == null)
            {
                throw new Exception("Configurable command class not set!");
            }

            // Log command details
            var user = UserContext;

            logger.Log(LogType.INFORMATION, $"Environment: {environment}");
            logger.Log(LogType.INFORMATION, $"Current User: {user}");
            if (configurableCommand != null)
            {
                logger.Log(LogType.INFORMATION, $"Configurable Command Name: {configurableCommand.Description}");
            }
            logger.Log(LogType.INFORMATION, $"Configurable Command Class: {configurableCommandClass.ClassName}");
            logger.Log(LogType.INFORMATION, $"Configurable Command Class Type: {configurableCommandClass.ClassType}");

            // Create exe, and set up logging
            var executable = AbstractConfigurableCommand.Create(
                configurableCommandClass, configurableCommandProperties, MetadataRepository);

            executable.Logger = logger;

            // Launch
            if (configurableCommandClass.ClassType == ConfigurableCommandType.Action)
            {
                ((AbstractAction)executable).Execute();
            }
            else if (configurableCommandClass.ClassType == ConfigurableCommandType.Loader)
            {
                var loader = MetadataRepository.GetLoader(configurableCommand.ConfigurableCommandId.Value);

                // If loader TargetBehaviour = 'DROP', we refresh the column metadata at this point
                if (loader.TargetBehaviour == Loaders.TargetBehaviour.DROP)
                {
                    var newColumns = ((AbstractLoader)executable).Probe();
                    MetadataRepository.UpdateLoaderColumns(configurableCommand.ConfigurableCommandId.Value, newColumns.Select(c => new Models.Loader.LoaderColumnInfo
                    {
                        ConfigurableCommandId = configurableCommand.ConfigurableCommandId.Value,
                        ColumnName = c.ColumnName,
                        DataType = c.DataType,
                        DataLength = c.DataLength,
                        Order = c.Order,
                        PrimaryKey = c.PrimaryKey,
                        Selected = true
                    }));
                }

                var columns = MetadataRepository.GetLoaderColumns(configurableCommand.ConfigurableCommandId.Value).ToList();
                var server = MetadataRepository.GetServers().First(s => s.ServerName.Equals(loader.ServerName, StringComparison.OrdinalIgnoreCase));
                var dataLake = AbstractDataLake.Create(server, logger);
                dataLake.Load(executable, loader, columns);
            }
        }

        /// <summary>
        /// Checks preconditions prior to executing command. Command only runs if precondition
        /// commands have appropriate completion status.
        /// </summary>
        /// <param name="configurableCommandId"></param>
        /// <param name="preconditions"></param>
        /// <param name="lastLogs"></param>
        private void CheckPreconditionSuccessStatus()
        {
            foreach (var precondition in Preconditions.Where(p => p.ConfigurableCommandId == ConfigurableCommandId))
            {
                var lastLogStatus = LastLogs.FirstOrDefault(l => l.ConfigurableCommandId == ConfigurableCommandId);
                if (lastLogStatus == null)
                {
                    throw new Exception($"Precondition status check failed for precondition: {precondition.PreconditionId}. No precondition status found in logs.");
                }
                else if (
                  (lastLogStatus.LogStatus == Sara.Lib.Logging.LogType.FAILURE && precondition.SuccessFlag == true) ||
                  (lastLogStatus.LogStatus == Sara.Lib.Logging.LogType.SUCCESS && precondition.SuccessFlag == false))
                {
                    throw new Exception($"Precondition status check failed for precondition: {precondition.PreconditionId}. Last log status: {lastLogStatus.LogStatus} of Log Id: {lastLogStatus.LogId} is incompatible with precondition SUCCESS_FLAG status: {precondition.SuccessFlag}.");
                }
            }
        }
    }
}
