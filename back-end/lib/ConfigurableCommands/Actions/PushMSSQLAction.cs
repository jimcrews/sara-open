using Sara.Lib.ConfigurableCommands.Loaders;
using Sara.Lib.Logging;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Sara.Lib.Extensions;
using Sara.Lib.ConfigurableCommands.Actions;
using Sara.Lib.ConfigurableCommands;

namespace Sara.Lib.ConfigurableCommands.Actions
{
    /// <summary>
    /// This action pushes a published dataset to an external MSSQL table. This is useful
    /// for integration with other applications that are unable to pull using the API.
    /// </summary>
    public class PushMSSQLAction : AbstractAction
    {
        [ConfigurableProperty(Seq = 1, Name = "SARA_URL", Mandatory = true, Help = @"", Description = "The URL to the SARA dataset.")]
        public string SaraUrl { get; set; }

        [ConfigurableProperty(Seq = 2, Name = "CONNECTION_STRING", Mandatory = true, Help = @"", Description = "The connection string of the target MSSQL server to save the data to.")]
        public string ConnectionString { get; set; }

        [ConfigurableProperty(Seq = 3, Name = "DESTINATION_TABLE", Mandatory = true, Help = @"DATABASE_NAME.SCHEMA_NAME.TABLE_NAME", Description = "The name of the destination table (3-part name).")]
        public string DestinationTable { get; set; }

        [ConfigurableProperty(Seq = 4, Name = "TRUNCATE", Mandatory = false, Help = @"", Description = "If set to true, the target table will be truncated prior to loading with new data.")]
        public bool Truncate { get; set; }

        [ConfigurableProperty(Seq = 5, Name = "CREATE_TABLE", Mandatory = false, Help = @"", Description = "If set to true, the target table will be created if it doesn't exist. Note that performance will be impacted if this is set to true.")]
        public bool CreateTable { get; set; }

        public override void Execute()
        {
            Logger.Log(LogType.INFORMATION, $"Start of PushMSSQL action.");

            SaraLoader loader = new SaraLoader();
            loader.Url = SaraUrl;

            using(var db = new SqlConnection(ConnectionString))
            {
                if (CreateTable)
                {
                    Logger.Log(LogType.INFORMATION, $"Creating table if not exists.");
                    var probeResults = loader.Probe();
                    var createSql = GetTableDDL(probeResults);
                    db.Execute(createSql);
                }

                Logger.Log(LogType.INFORMATION, $"Reading data from {SaraUrl}.");
                var data = loader.Read();

                if (Truncate)
                {
                    Logger.Log(LogType.INFORMATION, $"Truncating table: {DestinationTable}.");
                    db.Execute($"TRUNCATE TABLE {DestinationTable}", null, null, 3600);
                }

                if (data.Any())
                {
                    Dictionary<string, Type> schema = new Dictionary<string, Type>();
                    foreach (var key in data.First().Keys)
                    {
                        schema.Add(key, typeof(object));
                    }
                    Logger.Log(LogType.INFORMATION, $"Loading data to table: {DestinationTable}.");
                    var dt = DictionaryToDataTable(data);
                    db.Open();
                    db.BulkCopy(dt, DestinationTable, 3600);
                    Logger.Log(LogType.INFORMATION, $"{dt.Rows.Count} rows copied.");
                }
                else
                {
                    throw new Exception("No data found to push to MSSQL!");
                }
            }
            Logger.Log(LogType.INFORMATION, $"Finished PushMSSQL action.");
        }

        private DataTable DictionaryToDataTable(IEnumerable<IDictionary<string, object>> data)
        {
            DataTable dt = new DataTable();
            List<string> columns = new List<string>();
            dt.Clear();
            var firstRow = data.First();
            foreach (var key in firstRow.Keys)
            {
                dt.Columns.Add(key);
                columns.Add(key);
            }

            foreach (var item in data)
            {
                var row = dt.NewRow();
                foreach (var column in columns)
                {
                    row[column] = item[column];
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

        private string GetTableDDL(IEnumerable<Lib.ConfigurableCommands.Loaders.DataColumn> probeColumns)
        {
            Dictionary<DataType, string> dataTypeMapping = new Dictionary<DataType, string>()
            {
                {DataType.Boolean, "BIT" },
                {DataType.Time, "TIME" },
                {DataType.Date, "DATE" },
                {DataType.DateTime, "DATETIME2" },
                {DataType.Float32, "FLOAT(24)" },
                {DataType.Float64, "FLOAT(53)" },
                {DataType.Guid, "UNIQUEIDENTIFIER" },
                {DataType.Integer16, "SMALLINT" },
                {DataType.Integer32, "INT" },
                {DataType.Integer64, "BIGINT" },
                {DataType.String, "VARCHAR({0})" },
                {DataType.Decimal, "DECIMAL({0},{1})" },
                {DataType.Json, "NVARCHAR(MAX)" },      // No native Json type. Must use NVARCHAR(MAX)
                {DataType.Binary, "VARBINARY({0})" }
            };

            List<string> columnArray = new List<string>();
            foreach (var column in probeColumns)
            {
                string dataType = "";
                if (column.DataType==DataType.Decimal)
                {
                    dataType = string.Format(dataTypeMapping[column.DataType], column.Precision, column.Scale);
                } else
                {
                    dataType = string.Format(dataTypeMapping[column.DataType], (column.DataLength > 8000 ? "MAX" : column.DataLength.ToString()));
                }
                var nullable = column.PrimaryKey ? "NOT NULL" : "NULL";
                columnArray.Add(string.Format("[{0}] {1} {2}", column.ColumnName, dataType, nullable));
            }

            return string.Format(
                @"
IF OBJECT_ID('{0}', 'U') IS NULL CREATE TABLE {0} (
  {1}
);",
                DestinationTable,
                string.Join(@"
 ,", columnArray));
        }
    }
}
