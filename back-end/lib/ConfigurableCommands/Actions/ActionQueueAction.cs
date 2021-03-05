using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Sara.Lib.ConfigurableCommands;
using Sara.Lib.ConfigurableCommands.Actions;
using Sara.Lib.Extensions;
using Sara.Lib.Logging;
using Sara.Lib.Models.Action;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Sara.Lib.ConfigurableCommands.Actions
{
    /// <summary>
    /// Action that reads from a standard SARA queue. Each queue entry
    /// contains configuration to execute another action. This is the standard
    /// mechanism for ad-hoc invokation of actions.
    /// </summary>
    public class ActionQueueAction : AbstractAction
    {
        public override void Execute()
        {
            // Get queue
            var queue = MetadataRepository.GetActionQueue();

            // Process each one sequentially, serially
            foreach (var item in queue)
            {
                Logger.Log(LogType.INFORMATION, $"Found queue item for action: {item.Action}, with parameters of {item.Parameters}.");

                // Run the action
                try
                {
                    MetadataRepository.SetActionQueueStatus(item.ActionQueueId, ActionQueueStatus.Running);
                    var action = item.Action;
                    var parameters = item.Parameters;

                    // Parse the parameters
                    List<ConfigurableCommandProperty> cp = new List<ConfigurableCommandProperty>();
                    var obj = (JObject)JsonConvert.DeserializeObject(parameters);

                    foreach (var token in obj.Children())
                    {
                        if (token is JProperty)
                        {
                            var prop = token as JProperty;
                            var newProp = new ConfigurableCommandProperty
                            {
                                PropertyName = prop.Name,
                                PropertyValue = prop.Value.ToString()
                            };
                            cp.Add(newProp);

                        }
                    }
                    var cls = MetadataRepository.GetClasses().First(c => c.ClassName.Equals(action, StringComparison.OrdinalIgnoreCase));
                    AbstractAction cmd = (AbstractAction)AbstractConfigurableCommand.Create(cls, cp, MetadataRepository);

                    // Run command in separate process
                    new Process().RunSaraCommandProcess(
                        Assembly.GetEntryAssembly().Location,
                        MetadataRepository.GetEnvironment().ToText(),
                        null,
                        item.Action,
                        item.Parameters);
                    MetadataRepository.SetActionQueueStatus(item.ActionQueueId, ActionQueueStatus.Success);
                }
                catch(Exception ex)
                {
                    MetadataRepository.SetActionQueueStatus(item.ActionQueueId, ActionQueueStatus.Failure, ex);
                }
            }
        }
    }
}
