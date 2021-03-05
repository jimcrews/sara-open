using Sara.Lib.ConfigurableCommands;
using Sara.Lib.ConfigurableCommands.Actions;
using Sara.Lib.Logging;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Sara.Lib.Data;

namespace Sara.Lib.ConfigurableCommands.Actions
{
    public class CacheObjectAction : AbstractAction
    {
        [ConfigurableProperty(Seq = 1, Name = "SERVER_NAME", Mandatory = true, Help = @"", Description = "The name of the server where the object resides.")]
        public string ServerName { get; set; }

        [ConfigurableProperty(Seq = 2, Name = "SOURCE_NAME", Mandatory = true, Help = @"DATABASE_NAME.SCHEMA_NAME.VIEW_NAME", Description = "The source name of the object to cache. For example for MSSQL, the full 3-part object name of the view to materialise.")]
        public string SourceName { get; set; }

        [ConfigurableProperty(Seq = 3, Name = "TARGET_NAME", Mandatory = true, Help = @"DATABASE_NAME.SCHEMA_NAME.TABLE_NAME", Description = "The target name of the cached object. For example for MSSQL, the full 3-part object name of the resulting table containing the view contents.")]
        public string TargetName { get; set; }

        [ConfigurableProperty(Seq = 4, Name = "REFRESH_SCHEMA", Mandatory = true, Help = @"True", Description = "If set to true, the target object will be refreshed prior to caching. For example for MSSQL, the materialised table will be dropped and recreated. Note that this may result in additional database locking, and longer processing times.")]
        public bool RefreshSchema { get; set; }

        public override void Execute()
        {
            var srv = MetadataRepository.GetServers().First(s => s.ServerName.Equals(ServerName, StringComparison.OrdinalIgnoreCase));
            var dl = AbstractDataLake.Create(srv, this.Logger);
            dl.CacheObject(SourceName, TargetName, RefreshSchema);
        }
    }
}
