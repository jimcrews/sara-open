using Sara.Lib.ConfigurableCommands;
using Sara.Lib.ConfigurableCommands.Actions;
using Sara.Lib.ConfigurableCommands.Loaders;
using Sara.Lib.Extensions;
using Sara.Lib.Logging;
using Sara.Lib.Metadata;
using Sara.Lib.Models.Loader;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Sara.Lib.ConfigurableCommands.Actions
{
    /// <summary>
    /// Runs any user (admin) triggered probes on loaders. This queries the loaders'
    /// data sources to automatically generate the column lists. Multiple loaders
    /// can be probed in the single execution. The list is maintained in a table
    /// called LOADER_PROBE_QUEUE that is updated via the API.
    /// </summary>
    public class ProbeLoadersAction : AbstractAction
    {
        public override void Execute()
        {
            Logger.Log(LogType.INFORMATION, "Checking for loaders to probe...");

            var probesDue = MetadataRepository.GetProbeLoaderQueue();
            var probeCount = 0;

            foreach (var configurableCommandId in probesDue)
            {
                probeCount++;
                try
                {
                    Logger.Log(LogType.INFORMATION, $"Starting probe for configurable command id: {configurableCommandId}.");

                    // Do probe
                    var loader = (AbstractLoader)MetadataRepository.CreateExecutable(configurableCommandId);
                    var cols = loader.Probe();
                    MetadataRepository.UpdateLoaderColumns(configurableCommandId, cols.Select(c => new Models.Loader.LoaderColumnInfo()
                    {
                        ConfigurableCommandId = configurableCommandId,
                        ColumnName = c.ColumnName,
                        DataLength = c.DataLength,
                        DataType = c.DataType,
                        Order = c.Order,
                        PrimaryKey = c.PrimaryKey,
                        Selected = true
                    }));

                }
                catch(Exception ex)
                {
                    Logger.Log(LogType.ERROR, $"Error occured probing configurable command id: {configurableCommandId}: {ex.Message}");
                }

                // remove queue
                MetadataRepository.DeleteProbeLoaderQueue(configurableCommandId);
            }

            Logger.Log(LogType.INFORMATION, $"Complete. {probeCount} loader(s) have been probed.", new { ROWS = probeCount }.ToDictionary());

        }
    }
}
