using Sara.Lib.ConfigurableCommands;
using Sara.Lib.ConfigurableCommands.Actions;
using Sara.Lib.Logging;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace Sara.Lib.ConfigurableCommands.Actions
{
    /// <summary>
    /// Runs the metadata repository maintenance method
    /// </summary>
    public class MetadataRepositoryMaintenanceAction : AbstractAction
    {
        public override void Execute()
        {
            MetadataRepository.Maintenance();
        }
    }
}
