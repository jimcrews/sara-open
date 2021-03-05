using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Data.SqlClient;
using Sara.Lib.Extensions;
using Dapper;
using Sara.Lib.Models;
using Sara.Lib.ConfigurableCommands;
using Sara.Lib.Logging;
using Sara.Lib.ConfigurableCommands.Loaders;

namespace Sara.Lib.ConfigurableCommands.Loaders
{
    public class MSSQLLoader : AbstractLoader
    {
        [ConfigurableProperty(Seq = 1, Name = "CONNECTION_STRING", Mandatory = true, Help = "e.g. Data Source=SERVE_NAME;Initial Catalog=DATABASE_NAME;Integrated Security=True;", Description = "The connection string for the SQL Server source.")]
        public string ConnectionString { get; set; }

        [ConfigurableProperty(Seq = 2, Name = "QUERY", Mandatory = true, Help = "SELECT * FROM TABLE", Description = "The SQL query to retrieve data from the SQL Server database.", AllowMultiLine = true)]
        public string Query { get; set; }

        [ConfigurableProperty(Seq = 3, Name = "COMMAND_TYPE", Default = CommandType.Text, Description = "The type of query / command used.")]
        public CommandType CommandType { get; set; }

        [ConfigurableProperty(Seq = 4, Name = "TIMEOUT", Default = 30, Description = "The command timeout in seconds.")]
        public int Timeout { get; set; }

        /// <summary>
        /// Executes a query to probe the structure of the results.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<Lib.ConfigurableCommands.Loaders.DataColumn> Probe()
        {
            // Get schema
            using (var db = new SqlConnection(ConnectionString))
            {
                db.Open();
                var results = db.GetSchemaForSql(Query, null, CommandType, Timeout);
                foreach (var r in results)
                {
                    // format results

                    var columnType = (Type)r["DataType"];
                    if (columnType == null) throw new Exception("The type is null");

                    Lib.ConfigurableCommands.Loaders.DataColumn p = new Lib.ConfigurableCommands.Loaders.DataColumn();
                    p.ColumnName = (string)r["ColumnName"];
                    p.PrimaryKey = false;
                    p.DataType = columnType.ToDataType();
                    p.DataLength = p.DataType == DataType.String || p.DataType==DataType.Binary ? (int)r["ColumnSize"] : 0;
                    yield return p;
                }
            }
        }

        public override IEnumerable<IDictionary<string, object>> Read(int? maxDataRows=null)
        {
            using (var db = new SqlConnection(ConnectionString))
            {
                foreach (var row in db.Query<dynamic>(this.Query, commandTimeout:3600, commandType: CommandType))
                {
                    var rowAsDict = row as IDictionary<string, object>;
                    if (maxDataRows.HasValue && RowsAffected >= maxDataRows) break;
                    yield return rowAsDict;
                    RowsAffected++;
                }
            }
        }
    }
}