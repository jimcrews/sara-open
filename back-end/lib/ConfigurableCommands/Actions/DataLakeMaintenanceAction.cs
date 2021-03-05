using Sara.Lib.ConfigurableCommands;
using Sara.Lib.ConfigurableCommands.Actions;
using Sara.Lib.Extensions;
using Sara.Lib.Logging;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Sara.Lib.Data;
using DotLiquid.Util;

namespace Sara.Lib.ConfigurableCommands.Actions
{
    public class DataLakeMaintenanceAction : AbstractAction
    {
        public override void Execute()
        {
            var databaseBlacklist = MetadataRepository
                .GetConfiguration()
                .First(c => c.ConfigurationName.Equals("DATA_LAKE_MAINTENANCE_BLACKLIST", StringComparison.OrdinalIgnoreCase))
                .ConfigurationValue
                .Split(new string[] { "," }, StringSplitOptions.None)
                .Select(i => i.Trim())
                .ToArray();

            var servers = MetadataRepository.GetServers();
            foreach (var server in servers)
            {
                var dl = AbstractDataLake.Create(server, Logger);
                dl.Maintenance(databaseBlacklist);
            }
        }
    }
}