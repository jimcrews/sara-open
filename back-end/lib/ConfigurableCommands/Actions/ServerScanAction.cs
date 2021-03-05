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
using Sara.Lib.Models.Server;

namespace Sara.Lib.ConfigurableCommands.Actions
{
	public class ServerScanAction : AbstractAction
    {
        public override void Execute()
        {
			var databases = MetadataRepository
				.GetConfiguration()
				.First(c => c.ConfigurationName.Equals("DATABASES", StringComparison.OrdinalIgnoreCase))
				.ConfigurationValue
				.Split(',');

			// Server Objects
			List<ServerObjectInfo> serverObjects = new List<ServerObjectInfo>();
			foreach (var server in MetadataRepository.GetServers())
			{
				var dl = AbstractDataLake.Create(server, Logger);
				var obj = dl.GetObjects(server.ServerName, databases);
				serverObjects.AddRange(obj);
			}
			MetadataRepository.SaveServerObjects(serverObjects);

			// Server Columns
			List<ServerColumnInfo> serverColumns = new List<ServerColumnInfo>();
			foreach (var server in MetadataRepository.GetServers())
			{
				var dl = AbstractDataLake.Create(server, Logger);
				var obj = dl.GetColumns(server.ServerName, databases);
				serverColumns.AddRange(obj);
			}
			MetadataRepository.SaveServerColumns(serverColumns);

			// Server Parameters
			List<ServerParameterInfo> serverParameters = new List<ServerParameterInfo>();
			foreach (var server in MetadataRepository.GetServers())
			{
				var dl = AbstractDataLake.Create(server, Logger);
				var obj = dl.GetParameters(server.ServerName, databases);
				serverParameters.AddRange(obj);
			}
			MetadataRepository.SaveServerParameters(serverParameters);

			// Server Dependencies
			List<ServerDependencyInfo> serverDependencies = new List<ServerDependencyInfo>();
			foreach (var server in MetadataRepository.GetServers())
			{
				var dl = AbstractDataLake.Create(server, Logger);
				var obj = dl.GetDependencies(server.ServerName, databases);
				serverDependencies.AddRange(obj);
			}
			MetadataRepository.SaveServerDependencies(serverDependencies);
        }
    }
}