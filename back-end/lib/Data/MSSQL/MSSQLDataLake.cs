using Sara.Lib.ConfigurableCommands;
using Sara.Lib.ConfigurableCommands.Loaders;
using Sara.Lib.Data.Parsers;
using Sara.Lib.Extensions;
using Sara.Lib.Logging;
using Sara.Lib.Metadata;
using Sara.Lib.Models.Dataset;
using Sara.Lib.Models.Loader;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Sara.Lib.Data.Mock;
using System.IO;
using Newtonsoft.Json;
using Sara.Lib.Models.Server;


namespace Sara.Lib.Data.MSSQL
{
    /// <summary>
    /// SQL Server Data Lake
    /// </summary>
    public class MSSQLDataLake : AbstractDataLake
    {
        public MSSQLDataLake(ILogger logger, string connectionString) : base(logger, connectionString)
        {
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        public override IEnumerable<IDictionary<string, object>> Query(DatasetSummaryInfo dataset, int? top = 10000, int? sample = null, string filter = null, string param = null, string select = null, int? timeout = null)
        {
            var columns = dataset.Columns.Where(c => c.Public);
            var columnNames = columns.Select(c => c.ColumnName);
            var parameters = dataset.Parameters.Select(p => p.ParameterName);

            // Default select = all columns
            var selectedColumns = dataset.Columns.Where(c => c.Public).Select(c => new ColumnNode()
            {
                ColumnName = c.ColumnName,
                Alias = c.ColumnName
            }).ToList();

            if (top < 0)
                throw new Exception("The 'top' parameter cannot be negative.");

            if (top == null)
                top = 99999999;

            if (timeout == null || timeout <= 0)
            {
                timeout = 300;
            }

            // Selecting columns:
            // if select parameter passed in, filter the list of columns
            // the column list can include aggregate function, e.g.:
            // SELECT=ABC,DEF,GHI,SUM(JKL)
            // In above case, first 3 columns are GROUPED, the 4th column is aggregated
            // we DON'T allow column to be grouped that is not in select. Any column in select
            // is EITHER aggregated or else grouped by definition.
            bool isGrouped = false;
            if (!string.IsNullOrEmpty(select))
            {
                select = select.ToUpper();

                var selectState = new DefaultSelectParser().Parse(select, columnNames);
                if (((List<ColumnNode>)selectState.Result).Any())
                {
                    selectedColumns = ((List<ColumnNode>)selectState.Result);
                }
                isGrouped = selectState.IsGrouped();
            }

            if (!selectedColumns.Any())
                throw new Exception("No columns selected.");

            // SPECIAL CASE FOR CONCURRENCY FIELDS (ROWVERSION)
            // MUST CONVERT TO VARCHAR
            var concurrencyColumn = dataset.Columns.FirstOrDefault(c => c.DataType == DataType.RowVersion);

            if (concurrencyColumn != null)
            {
                var col = selectedColumns.FirstOrDefault(c => c.ColumnName.Equals(concurrencyColumn.ColumnName, StringComparison.OrdinalIgnoreCase));
                if (col != null)
                {
                    col.FormattedName = $"CONVERT(NVARCHAR(MAX), CONVERT(BINARY(8), [{concurrencyColumn.ColumnName}]), 1) [{concurrencyColumn.ColumnName}]";
                }
            }

            selectedColumns.ForEach(c =>
            {
                var dataType = columns.First(col => col.ColumnName.Equals(c.ColumnName, StringComparison.OrdinalIgnoreCase)).DataType;

                if (!string.IsNullOrEmpty(c.Function))
                {
                    if (c.IsAggregateFunction())
                    {
                        // Aggregation functions
                        c.FormattedName = $"{c.Function}([{c.ColumnName}])";
                    }
                    else if (c.Function.Equals("BIN", StringComparison.OrdinalIgnoreCase))
                    {
                        // Bin function
                        // This bins following data types:
                        // DateTime - bin size is seconds
                        // Date - bin size is days
                        // Number - bin size is integer number
                        // Float - bin size is float/decimal number - note we must temporarily convert to DECIMAL as modulo not permitted on floats.

                        var binSize = float.Parse(c.Parameter);
                        switch (dataType)
                        {
                            case DataType.DateTime:
                                c.FormattedName = $"CAST(CAST([{c.ColumnName}] AS DATE) AS DATETIME) + CAST(DATEADD(SECOND, (DATEDIFF(SECOND, '00:00:00', CAST([{c.ColumnName}] AS TIME)) / {binSize}) * {binSize}, '00:00:00') AS DATETIME)";
                                break;
                            case DataType.Integer8:
                                c.FormattedName = $"[{c.ColumnName}] - ([{c.ColumnName}] % {binSize})";
                                break;
                            case DataType.Integer16:
                                c.FormattedName = $"[{c.ColumnName}] - ([{c.ColumnName}] % {binSize})";
                                break;
                            case DataType.Integer32:
                                c.FormattedName = $"[{c.ColumnName}] - ([{c.ColumnName}] % {binSize})";
                                break;
                            case DataType.Integer64:
                                c.FormattedName = $"[{c.ColumnName}] - ([{c.ColumnName}] % {binSize})";
                                break;
                            case DataType.Float32:
                                c.FormattedName = $"CAST(CAST([{c.ColumnName}] AS DECIMAL(18,4)) - (CAST([{c.ColumnName}] AS DECIMAL(18,4)) % {binSize}) AS FLOAT)";
                                break;
                            case DataType.Float64:
                                c.FormattedName = $"CAST(CAST([{c.ColumnName}] AS DECIMAL(18,4)) - (CAST([{c.ColumnName}] AS DECIMAL(18,4)) % {binSize}) AS FLOAT)";
                                break;
                            case DataType.Decimal:
                                c.FormattedName = $"[{c.ColumnName}] - ([{c.ColumnName}] % {binSize})";
                                break;
                        }
                    }
                    else
                    {
                        // unspecified function?
                        //c.FormattedName = $"{c.Function}([{c.ColumnName}], {c.Parameter})";
                    }
                }
                else
                {
                    c.FormattedName = $"[{c.ColumnName}]";
                }
            });

            // Param + Filter query parameters.
            dynamic state = null;
            string filterSQL = "";
            string paramSQL = "";

            // function parameters
            if (!string.IsNullOrEmpty(param))
            {
                state = new MSSQLFilterParser().Parse(ParamsType.PARAMS, param, parameters, state);
                paramSQL = state.Sql;
            }
            // optional filter
            if (!string.IsNullOrEmpty(filter))
            {
                state = new MSSQLFilterParser().Parse(ParamsType.FILTER, filter, columnNames, state);
                filterSQL = state.Sql;
            }
            object args = null;
            if (state != null)
                args = (object)state.Parameters;

            var sql = "";
            if (dataset.ObjectType == "PROCEDURE")
            {
                // For procedures, we:
                // a) create a temp table
                // b) insert into temp table from sproc
                // c) treat the temp table like the base table below

                Dictionary<DataType, string> dataTypeMapping = new Dictionary<DataType, string>()
                {
                    { DataType.Boolean, "BIT" },
                    { DataType.Time, "TIME" },
                    { DataType.Date, "DATE" },
                    { DataType.DateTime, "DATETIME2" },
                    { DataType.Float32, "FLOAT(24)" },
                    { DataType.Float64, "FLOAT(53)" },
                    { DataType.Guid, "UNIQUEIDENTIFIER" },
                    { DataType.Integer16, "SMALLINT" },
                    { DataType.Integer32, "INT" },
                    { DataType.Integer64, "BIGINT" },
                    { DataType.String, "VARCHAR" },
                    { DataType.Json, "NVARCHAR(MAX)" },      // No native Json type. Must use NVARCHAR(MAX)
                    { DataType.Binary, "VARBINARY" },
                    { DataType.Decimal, "DECIMAL" }
                };

                var tempTable = string.Format("#{0}_{1}", dataset.Category, dataset.Dataset);
                var tempTableColumns = dataset.Columns.Select(
                    c => string.Format(
                        "{3}{0} {1}{2} NULL",
                        c.ColumnName,
                        dataTypeMapping[c.DataType],
                        c.DataType == DataType.String ? string.Format("({0})", c.DataLength) :
                        c.DataType == DataType.Decimal ? string.Format("({0}, {1})", c.Precision, c.Scale) :
                        "",
                        Environment.NewLine));
                sql = string.Format(@"
DROP TABLE IF EXISTS {0};
CREATE TABLE {0} (
    {1}
);
INSERT INTO {0} EXEC {2} {3}
SELECT {6} FROM (SELECT TOP {5} * FROM {0} {4}) A {7}",
                    tempTable,
                    string.Join(",", tempTableColumns),
                    dataset.SourceName,
                    paramSQL,
                    filterSQL,
                    top,
                    string.Join(",", selectedColumns.Select(c => $"{c.FormattedName} [{c.Alias}]")),
                    isGrouped ? " GROUP BY " + string.Join(",", selectedColumns.Where(c => !c.IsAggregateFunction()).Select(c => c.FormattedName)) : ""
                    );
            }

            if (dataset.ObjectType == "BASE TABLE" || dataset.ObjectType == "MATERIALISED VIEW" || dataset.ObjectType == "VIEW" || dataset.ObjectType == "SYNONYM")
            {
                sql = string.Format(
                    "SELECT {3} FROM (SELECT TOP {2} {4} FROM {0} {1} {5}) A",
                    dataset.SourceName,
                    filterSQL,
                    top,
                    string.Join(",", selectedColumns.Select(c => $"[{c.Alias}]")),
                    string.Join(",", selectedColumns.Select(c => $"{c.FormattedName} [{c.Alias}]")),
                    isGrouped ? " GROUP BY " + string.Join(",", selectedColumns.Where(c => !c.IsAggregateFunction()).Select(c => c.FormattedName)) : ""
                    );
            }
            else if (dataset.ObjectType == "FUNCTION")
            {
                sql = string.Format(
                    "SELECT TOP {3} {4} FROM {0} ({1}) {2} {5}",
                    dataset.SourceName,
                    paramSQL,
                    filterSQL,
                    top,
                    string.Join(",", selectedColumns.Select(c => $"{c.FormattedName} [{c.Alias}]")),
                    isGrouped ? " GROUP BY " + string.Join(",", selectedColumns.Where(c => !c.IsAggregateFunction()).Select(c => c.FormattedName)) : ""
                    );
            }

            using (var db = new SqlConnection(ConnectionString))
            {
                var data = db.Query(sql, args, commandTimeout: timeout).Select(r => (IDictionary<string, object>)r);
                if (sample.HasValue)
                    return data.Sample(sample.Value);
                else
                    return data;
            }
        }

        public override void Load(AbstractConfigurableCommand executable, LoaderInfo loader, IList<LoaderColumnInfo> columns)
        {
            Logger.Log(LogType.BEGIN, "Beginning of extraction.");
            StageData((AbstractLoader)executable, loader, columns);
            LoadData(loader, columns);
            Logger.Log(LogType.SUCCESS, "Extraction completed successfully.");
        }

        private void StageData(AbstractLoader exe, LoaderInfo loader, IList<LoaderColumnInfo> columns)
        {
            try
            {
                using (var db = new SqlConnection(ConnectionString))
                {
                    db.Open();
                    db.ChangeDatabase(loader.TargetName.Split('.')[0]);

                    Logger.Log(LogType.PROGRESS, "Beginning staging.");

                    // Create stage table
                    db.Execute(string.Format($"DROP TABLE IF EXISTS {StageTable(loader)}"));
                    Logger.Log(LogType.PROGRESS, "Dropped stage table if exists.");
                    var sql = GetDDL(loader, columns, TableType.STAGE);
                    db.Execute(sql);
                    Logger.Log(LogType.PROGRESS, "Created stage table.");

                    Dictionary<string, Type> schema = new Dictionary<string, Type>();
                    foreach (var column in columns)
                        schema[column.ColumnName] = ColumnType.Create().First(ct => ct.DataType == column.DataType).DotNetType;

                    // Bulk Copy Extract
                    //var data = State.GetExecutable().Read(MAX_ROWS);
                    Logger.Log(LogType.PROGRESS, "Querying source for data.");
                    var data = exe.ReadFormatted(columns, 99999999);
                    db.BulkCopy(data, schema, StageTable(loader), (logType, message) => { Logger.Log(logType, message); });
                    var rows = data.Count();
                    //var rows = exe.RowsAffected;
                    Logger.Log(LogType.PROGRESS, "Bulk copied from source to stage table.", new
                    {
                        ROWS = rows
                    }.ToDictionary());
                    Logger.Log(LogType.PROGRESS, "Staging complete.");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Loads the data from the staging area in the the persistent
        /// table. Applies any insert/merge rules as specified
        /// in the loader rule.
        /// </summary>
        /// <returns></returns>
        private void LoadData(LoaderInfo loader, IList<LoaderColumnInfo> columns)
        {
            int rows = 0;

            using (var db = new SqlConnection(ConnectionString))
            {
                db.Open();
                db.ChangeDatabase(loader.TargetName.Split('.')[0]);

                Logger.Log(LogType.PROGRESS, "Beginning data load.");

                // Target behaviour
                Logger.Log(LogType.PROGRESS, string.Format("Using target behaviour: [{0}].", loader.TargetBehaviour));

                if (loader.TargetBehaviour == TargetBehaviour.DROP)
                {
                    db.Execute(string.Format($"DROP TABLE IF EXISTS {TargetTable(loader)}"), commandTimeout: 600);
                    Logger.Log(LogType.PROGRESS, "Dropped target database if exists.");
                }
                else if (loader.TargetBehaviour == TargetBehaviour.TRUNCATE)
                {
                    db.Execute(string.Format($"IF OBJECT_ID('{TargetTable(loader)}', 'U') IS NOT NULL TRUNCATE TABLE {TargetTable(loader)}"), commandTimeout: 600);
                    Logger.Log(LogType.PROGRESS, "Truncated target table if exists.");
                }

                if (loader.TargetBehaviour == TargetBehaviour.DROP || ((loader.TargetBehaviour == TargetBehaviour.CREATE || loader.TargetBehaviour == TargetBehaviour.TRUNCATE)))
                {
                    db.Execute(GetDDL(loader, columns, TableType.LOAD), commandTimeout: 600);
                    Logger.Log(LogType.PROGRESS, "Created target database if not exists.");
                }

                // BeforeRowProcessingSql
                if (!string.IsNullOrEmpty(loader.BeforeRowProcessingSql))
                {
                    rows = db.Execute(loader.BeforeRowProcessingSql);
                    Logger.Log(LogType.PROGRESS, "Executed BeforeRowProcessingSql.", new
                    {
                        ROWS = rows
                    }.ToDictionary());
                }

                Logger.Log(LogType.PROGRESS, string.Format("Using RowProcessing behaviour: [{0}].", loader.RowProcessingBehaviour));

                // Check primary keys exist for row processing types that need PK.
                if (
                    loader.RowProcessingBehaviour == RowProcessingBehaviour.MERGE ||
                    loader.RowProcessingBehaviour == RowProcessingBehaviour.TEMPORAL ||
                    loader.RowProcessingBehaviour == RowProcessingBehaviour.UPSERT)
                {
                    if (!columns.Any(c => c.PrimaryKey))
                        throw new Exception(string.Format("Cannot load data using row processing behaviour of {0}, as no primary keys specified.", loader.RowProcessingBehaviour.ToText()));
                }

                var sql = GetLoadSQL(loader, columns);
                rows = db.Execute(sql, commandTimeout: 600);
                Logger.Log(LogType.PROGRESS, "Updated target table.", new
                {
                    ROWS = rows
                }.ToDictionary());

                // AfterRowProcessingSql
                if (!string.IsNullOrEmpty(loader.AfterRowProcessingSql))
                {
                    rows = db.Execute(loader.AfterRowProcessingSql);
                    Logger.Log(LogType.PROGRESS, "Executed AfterRowProcessingSql.", new
                    {
                        ROWS = rows
                    }.ToDictionary());
                }

                // Tidy up
                db.Execute(string.Format($"DROP TABLE IF EXISTS {StageTable(loader)}"));
                Logger.Log(LogType.PROGRESS, "Cleaned up temporary tables.");
                Logger.Log(LogType.PROGRESS, "Data load complete.");
            }
        }

        #region Private SQL Methods

        // Useful strings used in SQL templating

        private string StageTable(LoaderInfo loader) => $"_s_{loader.ConfigurableCommandId}";
        private string TargetTable(LoaderInfo loader) => $"{loader.TargetName}";

        public string LoaderColumns(IList<LoaderColumnInfo> columns) => string.Join(", ", columns.Select(c => string.Format("[{0}]", c.ColumnName)));
        public string SourceColumns(IList<LoaderColumnInfo> columns) => string.Join(", ", columns.Select(c => string.Format("SOURCE.[{0}]", c.ColumnName)));
        public string KeyJoin(IList<LoaderColumnInfo> columns) => string.Join(" AND ", columns.Where(c => c.PrimaryKey).Select(c => string.Format("TARGET.[{0}] = SOURCE.[{0}]", c.ColumnName)));
        public string FirstKey(IList<LoaderColumnInfo> columns) => columns.First(col => col.PrimaryKey).ColumnName;
        public string ValueCompare(IList<LoaderColumnInfo> columns) => string.Join(" OR ", columns.Where(c => !c.PrimaryKey).Select(c => string.Format("TARGET.[{0}] <> SOURCE.[{0}]", c.ColumnName)));
        public string KeysNotNull(IList<LoaderColumnInfo> columns) => string.Join(" AND ", columns.Where(c => c.PrimaryKey).Select(c => string.Format("[{0}] IS NOT NULL", c.ColumnName)));
        public string Now => DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt");
        public string UpdateNonKeyColumns(IList<LoaderColumnInfo> columns) => string.Join(",", columns.Where(c => !c.PrimaryKey).Select(c => string.Format("TARGET.[{0}] = SOURCE.[{0}]", c.ColumnName)));

        /// <summary>
        /// Returns the DDL script to create a table.
        /// </summary>
        /// <param name="tableType"></param>
        /// <returns></returns>
        public string GetDDL(LoaderInfo loader, IList<LoaderColumnInfo> columns, TableType tableType)
        {
            var cols = columns.OrderBy(dc => dc.Order);

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
                {DataType.Json, "NVARCHAR(MAX)" },      // No native Json type. Must use NVARCHAR(MAX)
                {DataType.Binary, "VARBINARY({0})" }
            };

            List<string> columnArray = new List<string>();
            foreach (var column in cols)
            {
                var dataType = string.Format(dataTypeMapping[column.DataType], (column.DataLength > 8000 ? "MAX" : column.DataLength.ToString()));
                var nullable = column.PrimaryKey ? "NOT NULL" : "NULL";
                columnArray.Add(string.Format("[{0}] {1} {2}", column.ColumnName, dataType, nullable));
            }

            if (loader.RowProcessingBehaviour == RowProcessingBehaviour.TEMPORAL && tableType == TableType.LOAD)
            {
                columnArray.Add("_SYS_EFFECTIVE_DT DATETIME2 NOT NULL");
                columnArray.Add("_SYS_EXPIRY_DT DATETIME2 NOT NULL DEFAULT '31-DEC-9999'");
            }

            if (loader.RowProcessingBehaviour == RowProcessingBehaviour.SNAPSHOT && tableType == TableType.LOAD)
                columnArray.Add("_SYS_UPDATED_DT DATETIME2 NOT NULL");

            if (loader.RowProcessingBehaviour == RowProcessingBehaviour.MERGE && tableType == TableType.LOAD)
                columnArray.Add("_SYS_UPDATED_DT DATETIME2 NOT NULL");

            if (loader.RowProcessingBehaviour == RowProcessingBehaviour.UPSERT && tableType == TableType.LOAD)
                columnArray.Add("_SYS_UPDATED_DT DATETIME2 NOT NULL");

            string sql = @"
IF OBJECT_ID('{0}', 'U') IS NULL CREATE TABLE {0} (
  {1}
);
";
            string columnDefinitions = string.Join(@"
  ,", columnArray);

            return string.Format(
                sql
                , (tableType == TableType.STAGE) ? StageTable(loader) : TargetTable(loader)
                , columnDefinitions
                );
        }

        /// <summary>
        /// Uses templates to get the loading SQL statement from the stage table.
        /// </summary>
        public string GetLoadSQL(LoaderInfo loader, IList<LoaderColumnInfo> columns)
        {
            var sql = @"
--------------------------------------
-- SQL SCRIPT TO LOAD DATA FROM
-- STAGE TABLE
--
-- THIS SCRIPT HAS BEEN AUTO-GENERATED
--------------------------------------

";
            switch (loader.RowProcessingBehaviour)
            {
                case RowProcessingBehaviour.INSERT:
                    sql = sql + $@"
INSERT INTO
    {TargetTable(loader)} ({LoaderColumns(columns)})
SELECT
    {LoaderColumns(columns)}
FROM
    {StageTable(loader)};
SELECT @@ROWCOUNT;".RenderTemplate(this);
                    break;
                case RowProcessingBehaviour.SNAPSHOT:
                    sql = sql + $@"
INSERT INTO
    {TargetTable(loader)} ({LoaderColumns(columns)}, _SYS_UPDATED_DT)
SELECT
    {LoaderColumns(columns)},
    '{Now}' _SYS_UPDATED_DT
FROM
    {StageTable(loader)}
SELECT @@ROWCOUNT;".RenderTemplate(this);
                    break;
                case RowProcessingBehaviour.UPSERT:
                    sql = sql + $@"
MERGE
    {TargetTable(loader)} [TARGET]
USING
    {StageTable(loader)} [SOURCE]
ON
    ({KeyJoin(columns)})
WHEN
    NOT MATCHED BY TARGET THEN
INSERT
    ({LoaderColumns(columns)}, _SYS_UPDATED_DT)
VALUES
    ({SourceColumns(columns)}, '{Now}')
WHEN
    MATCHED AND {ValueCompare(columns)}
THEN UPDATE SET
    {UpdateNonKeyColumns(columns)},
    TARGET._SYS_UPDATED_DT = '{Now}';
SELECT @@ROWCOUNT".RenderTemplate(this);
                    break;
                case RowProcessingBehaviour.MERGE:
                    sql = sql + $@"
MERGE
    {TargetTable(loader)} [TARGET]
USING
    {StageTable(loader)} [SOURCE]
ON
    ({KeyJoin(columns)})
WHEN
    NOT MATCHED BY TARGET THEN
INSERT
    ({LoaderColumns(columns)}, _SYS_UPDATED_DT)
VALUES
    ({SourceColumns(columns)}, '{Now}')
WHEN
    MATCHED AND {ValueCompare(columns)}
THEN UPDATE SET
    {UpdateNonKeyColumns(columns)},
    TARGET._SYS_UPDATED_DT = '{Now}'
WHEN
    NOT MATCHED BY SOURCE
THEN
    DELETE;
SELECT @@ROWCOUNT;".RenderTemplate(this);
                    break;
                case RowProcessingBehaviour.TEMPORAL:
                    sql = sql + $@"
DECLARE @ROWS INT;
DECLARE @END_DT DATETIME2;
SET @END_DT = '31-DEC-9999';

BEGIN TRANSACTION

-- NOT EXIST, INSERT
INSERT INTO {TargetTable(loader)} ({LoaderColumns(columns)}, _SYS_EFFECTIVE_DT, _SYS_EXPIRY_DT)
SELECT
	SOURCE.*,
	'{Now}',
	@END_DT
FROM
	{StageTable(loader)} SOURCE
LEFT JOIN
	{TargetTable(loader)} TARGET
ON
	{KeyJoin(columns)}
WHERE
    TARGET.{FirstKey(columns)} IS NULL AND
    TARGET._SYS_EXPIRY_DT = @END_DT;

SELECT @ROWS = @@ROWCOUNT;

-- EXPIRE OLD VERSION OF MODIFIED RECORDS
UPDATE
	{TargetTable(loader)}
SET
	_SYS_EXPIRY_DT = '{Now}'
FROM
	{TargetTable(loader)} TARGET
INNER JOIN
	{StageTable(loader)} SOURCE
ON
	{KeyJoin(columns)} AND
	TARGET._SYS_EXPIRY_DT = @END_DT
WHERE
	{ValueCompare(columns)};

SELECT @ROWS = @ROWS + @@ROWCOUNT;

-- CREATE NEW VERSION FOR MODIFIED RECORDS
INSERT INTO
	{TargetTable(loader)} ({LoaderColumns(columns)}, _SYS_EFFECTIVE_DT, _SYS_EXPIRY_DT)
SELECT
	SOURCE.*,
	'{Now}',
	@END_DT
FROM
	{TargetTable(loader)} TARGET
INNER JOIN
	{StageTable(loader)} SOURCE
ON
	{KeyJoin(columns)} AND
	TARGET._SYS_EXPIRY_DT = '{Now}'
WHERE
	{ValueCompare(columns)};

SELECT @ROWS = @ROWS + @@ROWCOUNT;

-- DELETE EXPIRED RECORDS
UPDATE
	{TargetTable(loader)}
SET
	_SYS_EXPIRY_DT = '{Now}'
FROM
	{TargetTable(loader)} TARGET
LEFT OUTER JOIN
	{StageTable(loader)} SOURCE
ON
	{KeyJoin(columns)}
WHERE
	SOURCE.{FirstKey(columns)} IS NULL AND
    TARGET._SYS_EXPIRY_DT = @END_DT;

SELECT @ROWS = @ROWS + @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT @ROWS;".RenderTemplate(this);
                    break;
                default:
                    return null;
            }
            return sql;
        }

        #endregion

        #region Writeback

        public override IDictionary<string, object> GetRow(DatasetSummaryInfo dataset, int id)
        {
            var updateableColumns = dataset.Columns.Where(c => !c.ReadOnly).Select(c => c.ColumnName);    // remove the key + concurrency
            var keyColumn = dataset.GetKeyColumn();
            var concurrencyColumn = dataset.GetRowVersionColumn();

            List<string> selectColumns = updateableColumns.Union(new string[] { keyColumn.ColumnName }).ToList();

            // Need to convert ROWVERSION to VARCHAR
            if (concurrencyColumn != null)
                selectColumns.Add($"CONVERT(NVARCHAR(MAX), CONVERT(BINARY(8), {concurrencyColumn.ColumnName}), 1) {concurrencyColumn.ColumnName}");

            var sqlSelect = string.Join(",", selectColumns);

            var sql = $"SELECT {sqlSelect} FROM {dataset.SourceName} WHERE {keyColumn.ColumnName} = @ID";

            using (var db = new SqlConnection(ConnectionString))
            {
                var data = db.Query(sql, new { ID = id }, commandTimeout: 60 * 5);   // 5 minute timeout
                var row = data.FirstOrDefault();
                if (row == null)
                {
                    throw new FileNotFoundException("Data not found!");
                }
                return (IDictionary<string, object>)row; ;
            }
        }

        public override IDictionary<string, object> UpdateRow(DatasetSummaryInfo dataset, int id, IDictionary<string, object> data)
        {
            using (var db = new SqlConnection(ConnectionString))
            {
                // Only Non-Key and Non-Concurrency fields can be updated
                var updateableColumns = dataset.Columns.Where(c => !c.ReadOnly).Select(c => c.ColumnName);    // remove the key + concurrency
                var keyColumn = dataset.GetKeyColumn();
                var concurrencyColumn = dataset.GetRowVersionColumn();

                // Check id not changed
                if (id != (Int64)data[keyColumn.ColumnName])
                {
                    throw new Exception("Cannot modify id value.");
                }

                var sqlSet = string.Join(",", updateableColumns.Select(uc => $"{uc} = @{uc}"));
                var sqlKey = $"{keyColumn.ColumnName} = @{keyColumn.ColumnName}";
                var sqlConcurrency = concurrencyColumn != null ? $" AND CONVERT(NVARCHAR(MAX), CONVERT(BINARY(8), {concurrencyColumn.ColumnName}), 1) = @{concurrencyColumn.ColumnName} " : "";
                var sql = $"UPDATE {dataset.SourceName} SET {sqlSet} WHERE {sqlKey} {sqlConcurrency}";

                var result = db.Execute(sql, data);
                // should update 1 row. If not, throw error
                if (result != 1)
                {
                    throw new Exception("Data not saved. The key may no longer exist in the database, or the row may have been modified externally. Please refresh data and try again.");
                }
                return GetRow(dataset, id);
            }
        }

        public override IDictionary<string, object> AddRow(DatasetSummaryInfo dataset, IDictionary<string, object> data)
        {
            using (var db = new SqlConnection(ConnectionString))
            {
                int id = 0;
                var insertColumns = dataset.Columns.Where(c => !c.ReadOnly).Select(c => c.ColumnName);

                // Get the key column - if the key is not calculated on the server
                // (e.g. SQL Server identity), we manually calculate a next id
                // based on the max id currently in the system + 1.
                var keyColumn = dataset.GetKeyColumn();
                if (!keyColumn.ReadOnly)
                {
                    var rows = this.Query(dataset);
                    if (rows.Any())
                    {
                        id = this.Query(dataset).Max(r => Convert.ToInt32(r[keyColumn.ColumnName])) + 1;
                    }
                    data[keyColumn.ColumnName] = id;
                }

                var sqlColumns = string.Join(",", insertColumns.Select(c => $"{c}"));
                var sqlValues = string.Join(",", insertColumns.Select(c => $"@{c}"));

                var sql = $"INSERT INTO {dataset.SourceName} ({sqlColumns}) VALUES ({sqlValues})";
                if (keyColumn.ReadOnly)
                {
                    sql = $"INSERT INTO {dataset.SourceName} ({sqlColumns}) VALUES ({sqlValues}); SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    id = db.Query<int>(sql, data).Single();
                }
                else
                {
                    db.Execute(sql, data);
                }

                return GetRow(dataset, id);
            }
        }

        public override void DeleteRow(DatasetSummaryInfo dataset, int id)
        {
            using (var db = new SqlConnection(ConnectionString))
            {
                // Find the PK column
                var keyColumn = dataset.GetKeyColumn();
                var sql = $"DELETE FROM {dataset.SourceName} WHERE {keyColumn.ColumnName} = @ID";
                var result = db.Execute(sql, new
                {
                    ID = id
                });

                // should update 1 row. If not, throw error
                if (result != 1)
                {
                    throw new FileNotFoundException("Delete failed. The key may no longer be valid. Please refresh data and try again.");
                }
            }
        }

        public override IEnumerable<ServerObjectInfo> GetObjects(string serverName, IEnumerable<string> databases)
        {
            using (var db = new SqlConnection(ConnectionString))
            {
                // Non system databases.
                var dbase = db.Query<string>("SELECT NAME FROM SYS.DATABASES WHERE DATABASE_ID > 4 AND NAME IN @DATABASES", new
                {
                    DATABASES = databases
                });

                foreach (var database in dbase)
                {
                    Logger.Log(LogType.INFORMATION, string.Format("Starting scan for database: {0}.", database));

                    // SERVER OBJECTS
                    Logger.Log(LogType.INFORMATION, string.Format("Getting server objects for database: {0}.", database));

                    var sql = string.Format(@"
					DECLARE @MV TABLE (
						SOURCE_VIEW_NAME VARCHAR(250),
						TARGET_TABLE_NAME VARCHAR(250)
					)
					INSERT INTO @MV SELECT * FROM DBO.MATERIALISED_VIEWS

                    USE [{0}];
                    SELECT
                        @SERVER_NAME SERVER_NAME,
			            DB_NAME() + '.' + SCHEMA_NAME(SCHEMA_ID) + '.' + O.NAME OBJECT_NAME,
			            CASE
				            -- MATERIALISED VIEW IF THE OBJECT EXISTS IN THE API.DBO.MATERIALISED_VIEWS VIEW
				            WHEN O.[TYPE] = 'U' AND EXISTS (
					            SELECT NULL FROM @MV MV WHERE DB_NAME() + '.' + SCHEMA_NAME(SCHEMA_ID) + '.' + O.NAME = MV.TARGET_TABLE_NAME
				            ) THEN 'MATERIALISED VIEW'
				            WHEN O.[TYPE] = 'U' THEN 'BASE TABLE'
				            WHEN O.[TYPE] = 'IF' THEN 'FUNCTION'
                            WHEN O.[TYPE] = 'TF' THEN 'FUNCTION'
				            WHEN O.[TYPE] = 'SN' THEN 'SYNONYM'
				            WHEN O.[TYPE] = 'P' THEN 'PROCEDURE'
				            WHEN O.[TYPE] = 'V' THEN 'VIEW'
				            ELSE 'UNKNOWN'
			            END OBJECT_TYPE,
			            (SELECT SUM(ROW_COUNT) FROM SYS.DM_DB_PARTITION_STATS PS WITH (NOLOCK) WHERE PS.OBJECT_ID = O.OBJECT_ID AND (INDEX_ID=0 or INDEX_ID=1)) ROW_COUNT,
			            O.CREATE_DATE CREATED_DT,
			            O.MODIFY_DATE MODIFIED_DT,
			            STAT.LAST_USER_UPDATE UPDATED_DT,
			            OBJECT_DEFINITION(O.OBJECT_ID) DEFINITION
		            FROM
			            SYS.OBJECTS O WITH (NOLOCK) 
		            LEFT OUTER JOIN (
			            SELECT
				            DATABASE_ID,
				            OBJECT_ID,
				            MAX(LAST_USER_UPDATE) LAST_USER_UPDATE
			            FROM
				            SYS.DM_DB_INDEX_USAGE_STATS WITH (NOLOCK) 
			            GROUP BY 
				            DATABASE_ID,
				            OBJECT_ID
		            ) STAT
		            ON
			            STAT.DATABASE_ID = DB_ID() AND
			            STAT.OBJECT_ID = O.OBJECT_ID
                    WHERE
                        SCHEMA_NAME(SCHEMA_ID) NOT IN ('SYS')", database);

                    foreach (var item in db.Query<ServerObjectInfo>(sql, new { SERVER_NAME = serverName }))
                    {
                        yield return item;
                    }
                }
            }
        }

        public override IEnumerable<ServerColumnInfo> GetColumns(string serverName, IEnumerable<string> databases)
        {
            using (var db = new SqlConnection(ConnectionString))
            {
                db.Open();

                // Non system databases.
                var dbase = db.Query<string>("SELECT NAME FROM SYS.DATABASES WHERE DATABASE_ID > 4 AND NAME IN @DATABASES", new
                {
                    DATABASES = databases
                });

                foreach (var database in dbase)
                {
                    // SERVER COLUMNS
                    var sql = $@"
    USE [{database}];
 	WITH ctePK
	AS
	(
		SELECT
			SCHEMA_NAME(TAB.SCHEMA_ID) as SCHEMA_NAME, 
			PK.[NAME] as PK_NAME,
			IC.INDEX_COLUMN_ID as PK_COLUMN_ID,
			COL.[NAME] as COLUMN_NAME, 
			TAB.[NAME] as TABLE_NAME
		FROM
			SYS.TABLES TAB
		INNER JOIN
			SYS.INDEXES PK
		ON
			TAB.OBJECT_ID = PK.OBJECT_ID AND
			PK.IS_PRIMARY_KEY = 1
		INNER JOIN
			SYS.INDEX_COLUMNS IC
		ON
			IC.OBJECT_ID = PK.OBJECT_ID AND
			IC.INDEX_ID = PK.INDEX_ID
		INNER JOIN
			SYS.COLUMNS COL
		ON
			PK.OBJECT_ID = COL.OBJECT_ID AND
			COL.COLUMN_ID = IC.COLUMN_ID
	)
	, cteCols
	AS
	(
		SELECT
			'{serverName}' SERVER_NAME,
			DB_NAME() DATABASE_NAME,
			SCHEMA_NAME(O.SCHEMA_ID) SCHEMA_NAME,
			OBJECT_NAME(C.OBJECT_ID) OBJECT_NAME,
			C.COLUMN_ID,
			C.NAME COLUMN_NAME,
			T.NAME DATA_TYPE,
			C.MAX_LENGTH MAXIMUM_LENGTH,
			C.PRECISION PRECISION,
			C.SCALE SCALE,
			CASE WHEN C.IS_IDENTITY = 1 OR T.NAME = 'TIMESTAMP' OR T.NAME = 'ROWVERSION' OR C.IS_COMPUTED = 1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END [READ_ONLY],
			CASE WHEN LEFT(C.NAME,1) = '_' THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END [PUBLIC]
		FROM
			SYS.COLUMNS C
		INNER JOIN
			SYS.TYPES T
		ON
			C.USER_TYPE_ID = T.USER_TYPE_ID
		INNER JOIN
			SYS.OBJECTS O
		ON
			C.OBJECT_ID = O.OBJECT_ID
	)

    , cteFinal
    AS
    (
	    SELECT
		    C.SERVER_NAME,
		    C.DATABASE_NAME,
		    C.SCHEMA_NAME,
		    C.OBJECT_NAME,
		    C.COLUMN_ID,
		    C.COLUMN_NAME,
		    C.DATA_TYPE,
		    C.MAXIMUM_LENGTH,
		    C.PRECISION,
		    C.SCALE,
		    C.[READ_ONLY],
		    C.[PUBLIC],
		    CASE WHEN PK.PK_COLUMN_ID IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END PRIMARY_KEY
	    FROM
		    cteCols C
	    LEFT OUTER JOIN
		    ctePK PK
	    ON
		    C.SCHEMA_NAME = PK.SCHEMA_NAME AND
		    C.OBJECT_NAME = PK.TABLE_NAME AND
		    C.COLUMN_NAME = PK.COLUMN_NAME
        WHERE
   		    C.SCHEMA_NAME NOT IN ('SYS')

	    UNION ALL

	    -- COLUMNS FOR PROCEDURES (NOTE THAT THE PROCEDURE MUST RETURN THE SAME SHAPE RESULTS. WE DO NOT ALLOW
	    -- PROCEDURES TO RETURN DIFFERENT SHAPES BASED ON THE PARAMETER(S) PASSED IN).
	    SELECT
            '{serverName}' SERVER_NAME,		
            DB_NAME() DATABASE_NAME,
		    SCHEMA_NAME(O.SCHEMA_ID) SCHEMA_NAME,
		    OBJECT_NAME(O.OBJECT_ID) OBJECT_NAME,
		    COLS.COLUMN_ORDINAL COLUMN_ID,
		    COLS.NAME COLUMN_NAME,
		    T.NAME DATA_TYPE,
		    COLS.MAX_LENGTH MAXIMUM_LENGTH,
            COLS.PRECISION PRECISION,
            COLS.SCALE SCALE,
		    CASE WHEN COLS.IS_IDENTITY_COLUMN = 1 OR COLS.IS_COMPUTED_COLUMN = 1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END [READ_ONLY],
		    CASE WHEN LEFT(COLS.NAME,1) = '_' OR LEFT(OBJECT_NAME(O.OBJECT_ID),1) = '_' OR LEFT(DB_NAME(), 1) = '_' OR SCHEMA_NAME(O.SCHEMA_ID) NOT IN ('DBO','MATERIALISED') THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END [PUBLIC],
		    CAST(0 AS BIT) PRIMARY_KEY
	    FROM
		    SYS.OBJECTS O
	    CROSS APPLY
		    SYS.DM_EXEC_DESCRIBE_FIRST_RESULT_SET_FOR_OBJECT(O.OBJECT_ID,NULL) COLS
	    INNER JOIN
		    SYS.TYPES T
	    ON
		    COLS.SYSTEM_TYPE_ID = T.USER_TYPE_ID
	    WHERE
		    O.TYPE = 'P' AND
            SCHEMA_NAME(O.SCHEMA_ID) NOT IN ('SYS')
    )

    SELECT
        SERVER_NAME,
        DATABASE_NAME + '.' + SCHEMA_NAME + '.' + OBJECT_NAME OBJECT_NAME,
        COLUMN_NAME,
        COLUMN_ID [ORDER],
        PRIMARY_KEY,
        CASE DATA_TYPE
            WHEN 'varchar' THEN 'String'
            WHEN 'nvarchar' THEN 'String'
            WHEN 'char' THEN 'String'
            WHEN 'nchar' THEN 'String'
            WHEN 'int' THEN 'Integer64'
            WHEN 'smallint' THEN 'Integer16'
            WHEN 'tinyint' THEN 'Integer8'
            WHEN 'bigint' THEN 'Integer64'
            WHEN 'float' THEN 'Float32'
            WHEN 'real' THEN 'Float32'
            WHEN 'decimal' THEN 'Decimal'
            WHEN 'numeric' THEN 'Decimal'
            WHEN 'datetime' THEN 'DateTime'
            WHEN 'datetime2' THEN 'DateTime'
            WHEN 'date' THEN 'Date'
            WHEN 'time' THEN 'Time'
            WHEN 'bit' THEN 'Boolean'
            WHEN 'uniqueidentifier' THEN 'Guid'
            ELSE 'String'
        END DATA_TYPE,
        MAXIMUM_LENGTH DATA_LENGTH,
        PRECISION,
        SCALE,
        [READ_ONLY],
        [PUBLIC]
    FROM
        cteFinal";

                    var data = db.Query<ServerColumnInfo>(sql);
                    foreach (var item in data)
                        yield return item;
                }
            }
        }

        public override IEnumerable<ServerParameterInfo> GetParameters(string serverName, IEnumerable<string> databases)
        {
            using (var db = new SqlConnection(ConnectionString))
            {
                db.Open();

                // Non system databases.
                var dbase = db.Query<string>("SELECT NAME FROM SYS.DATABASES WHERE DATABASE_ID > 4 AND NAME IN @DATABASES", new
                {
                    DATABASES = databases
                });

                foreach (var database in dbase)
                {
                    // SERVER COLUMNS
                    var sql = $@"
                    USE [{database}];
                    SELECT
                        '{serverName}' SERVER_NAME,		
                        DB_NAME() + '.' + SCHEMA_NAME(O.SCHEMA_ID) + '.' + OBJECT_NAME(P.OBJECT_ID) OBJECT_NAME,
		                P.PARAMETER_ID,
		                SUBSTRING(P.NAME, 2, 128) PARAMETER_NAME,
		                T.NAME DATA_TYPE,
                        P.MAX_LENGTH MAXIMUM_LENGTH,
                        P.IS_NULLABLE
                    FROM
                        SYS.PARAMETERS P
                    INNER JOIN
                        SYS.TYPES T
                    ON
                        P.USER_TYPE_ID = T.USER_TYPE_ID
                    INNER JOIN
                        SYS.OBJECTS O
                    ON
                        P.OBJECT_ID = O.OBJECT_ID";

                    var data = db.Query<ServerParameterInfo>(sql);
                    foreach (var item in data)
                        yield return item;
                }
            }
        }


        public override IEnumerable<ServerDependencyInfo> GetDependencies(string serverName, IEnumerable<string> databases)
        {
            using (var db = new SqlConnection(ConnectionString))
            {
                db.Open();

                // First, return all the materialised views
                var mvSql = $@"
                SELECT
                    '{serverName}' SERVER_NAME,
	                TARGET_TABLE_NAME PARENT_NAME,
                    SOURCE_VIEW_NAME CHILD_NAME
                FROM
                    MATERIALISED_VIEWS MV";

                var mv = db.Query<ServerDependencyInfo>(mvSql);

                foreach (var item in mv)
                    yield return item;

                // Then go through each database, getting the dependency information
                // Non system databases.
                var dbase = db.Query<string>("SELECT NAME FROM SYS.DATABASES WHERE DATABASE_ID > 4 AND NAME IN @DATABASES", new
                {
                    DATABASES = databases
                });

                foreach (var database in dbase)
                {
                    // SERVER COLUMNS
                    var sql = $@"
                    USE [{database}];
                    WITH cte
                    AS
                    (
                        SELECT DISTINCT
                            '{serverName}' SERVER_NAME,
                            DB_NAME() + '.' + OBJECT_SCHEMA_NAME(REFERENCING_ID) + '.' + OBJECT_NAME(REFERENCING_ID) PARENT_NAME,
		                    DEP.REFERENCING_ID PARENT_ID,
		                    COALESCE(DEP.REFERENCED_DATABASE_NAME, DB_NAME()) + '.' + COALESCE(DEP.REFERENCED_SCHEMA_NAME, OBJECT_SCHEMA_NAME(DEP.REFERENCED_ID)) + '.' + DEP.REFERENCED_ENTITY_NAME CHILD_NAME,
		                    DEP.REFERENCED_ID CHILD_ID
	                    FROM
		                    sys.sql_expression_dependencies DEP
	                    --WHERE
	                    --	DEP.referenced_id IS NOT NULL
		
	                    UNION ALL
		
	                    -- ADD SYNONYMS
	                    -- SYNONYMS SHOULD BE USED TO HIGHLIGHT THAT THE ORIGINAL OBJECT IS BEING USED
	                    -- WITHOUT CHANGE. SYNONYMS NEEDS TO BE DONE IN SPECIAL WAY, AS STANDARD
	                    -- DEPENDANCY VIEWS IN SQL DON''T CATER FOR SYNONYMS.
	                    -- SCRIPT USES STRING_SPLIT TO SPLIT SYNONYM''S BASE OBJECT FROM
	                    -- [DATABASE].[DBO].[OBJECT] FORMAT INTO 3 COLUMNS.

	                    SELECT
                            '{serverName}' SERVER_NAME,
		                    DB_NAME() + '.' + SCHEMA_NAME + '.' + OBJECT_NAME,
		                    NULL,
		                    [3] + '.' + [2] + '.' + [1],
		                    NULL
	                    FROM
	                    (
		                    SELECT
			                    ROW_NUMBER() OVER (PARTITION BY SCHEMA_NAME, OBJECT_NAME ORDER BY SEQ DESC) SEQ,
			                    OBJECT_NAME,
			                    SCHEMA_NAME,
			                    VALUE
		                    FROM
		                    (
			                    SELECT
				                    ROW_NUMBER() OVER (PARTITION BY OBJECT_ID ORDER BY OBJECT_ID ASC) SEQ,
				                    SYN.NAME OBJECT_NAME,
				                    SCH.NAME SCHEMA_NAME,
				                    VALUE
			                    FROM
				                    SYS.SYNONYMS SYN
			                      INNER JOIN
				                    SYS.SCHEMAS SCH
			                    ON
				                    SYN.SCHEMA_ID = SCH.SCHEMA_ID
			                    CROSS APPLY
				                    STRING_SPLIT (REPLACE(REPLACE(BASE_OBJECT_NAME,'[',''),']',''), '.')
		                    ) A
	                    ) A
	                    pivot
	                    (
		                       MIN(VALUE)
		                       FOR SEQ IN ([1],[2],[3])
	                    ) AS PVT
                    )
                    SELECT SERVER_NAME, PARENT_NAME, CHILD_NAME FROM cte WHERE CHILD_NAME IS NOT NULL
";

                    var data = db.Query<ServerDependencyInfo>(sql);
                    foreach (var item in data)
                        yield return item;
                }
            }
        }

        #endregion

        #region Caching

        public override void CacheObject(string sourceName, string targetName, bool refreshSchema)
        {
            Logger.Log(LogType.INFORMATION, string.Format("Starting materialising view: {0}.", sourceName));
            using (var db = new SqlConnection(ConnectionString))
            {
                Logger.Log(LogType.INFORMATION, $"RefreshSchema: {refreshSchema}.");

                if (refreshSchema)
                {
                    // This will refresh / create a new materialised view table
                    // Runs slower and locks database more.

                    // The temp table is always the same database as the target table
                    var tempName = string.Format("{0}_TMP", targetName);
                    var targetDatabase = targetName.Split('.')[0];

                    // Drop temp table if exists
                    var sql = string.Format("DROP TABLE IF EXISTS {0}", tempName);
                    Logger.Log(LogType.INFORMATION, string.Format("Deleting temp table if exists: {0}.", tempName));
                    db.Execute(sql);

                    // Create empty table
                    sql = string.Format(@"
BEGIN TRANSACTION
    SELECT
        *
    INTO
        {0}
    FROM
        {1}
    WHERE
        0 = 1;
COMMIT TRANSACTION
", tempName, sourceName);
                    Logger.Log(LogType.INFORMATION, string.Format("Creating new temp table:  {0}.", tempName));
                    db.Execute(sql, null, null, 3600);

                    // Copy data into temp table
                    sql = string.Format(@"
BEGIN TRANSACTION
    -- THIS MAY TAKE SOME TIME, BUT WON'T LOCK
    INSERT INTO 
        {0}
    SELECT
        *
    FROM
        {1}

    -- CREATE CLUSTERED COLUMNSTORE INDEX
    CREATE CLUSTERED COLUMNSTORE INDEX CCI_{2} ON {0};
COMMIT TRANSACTION", tempName, sourceName, sourceName.Replace(".", "_"));
                    Logger.Log(LogType.INFORMATION, string.Format("Copying data from {0} to {1}.", sourceName, tempName));
                    db.Execute(sql, null, null, 3600);

                    // Switch temp table to become new live table
                    // Note that sp_rename only works on current database so we must set current db first.
                    // The rename only uses schema.object notation.
                    // The renamed object just requires the name (no schema)
                    var tempSchemaObject = string.Join(".", tempName.Split('.').ToList().Reverse<string>().Take(2).Reverse<string>());
                    var targetSchemaObject = string.Join(".", targetName.Split('.').ToList().Reverse<string>().Take(2).Reverse<string>());
                    var targetObject = string.Join(".", targetName.Split('.').ToList().Reverse<string>().Take(1).Reverse<string>());

                    db.Open();
                    db.ChangeDatabase(targetDatabase);
                    sql = string.Format(@"
BEGIN TRANSACTION
    DROP TABLE IF EXISTS {0};
    EXEC SP_RENAME '{1}', '{2}';

    -- FAKE DELETE TO ENSURE UPDATE DATE IS WRITTEN TO SYS.DM_DB_INDEX_USAGE_STATS
    DELETE FROM {0} WHERE 0=1;
COMMIT TRANSACTION;", targetSchemaObject, tempSchemaObject, targetObject);
                    Logger.Log(LogType.INFORMATION, string.Format("Renaming table from {0} to {1}.", tempName, targetName));
                    db.Execute(sql);
                }
                else
                {
                    // This will not create any materialised view table. Assumes already exists.
                    // Runs faster, minimal locking.

                    var sql = string.Format(@"
BEGIN TRANSACTION
    TRUNCATE TABLE {0};
    INSERT INTO {0}
    SELECT * FROM {1};
COMMIT TRANSACTION
", targetName, sourceName);
                    Logger.Log(LogType.INFORMATION, $"Materialising data from view {sourceName} into {targetName}.");
                    db.Execute(sql, null, null, 3600);
                }
            }
            Logger.Log(LogType.SUCCESS, string.Format("Completed materialising view: {0}.", sourceName));
        }

        #endregion

        #region Maintenance

        public override void Maintenance(string[] databaseBlacklist)
        {
            using (var db = new SqlConnection(ConnectionString))
            {
                Logger.Log(LogType.INFORMATION, string.Format("NOTE! Maintenance action will NOT touch any tables > 1,000,000 data pages. These will need to be maintained manually."));

                // Non system databases
                var databases = db.Query<string>("SELECT NAME FROM SYS.DATABASES WHERE DATABASE_ID > 4 AND NAME NOT IN @DATABASE_BLACKLIST", new
                {
                    DATABASE_BLACKLIST = databaseBlacklist
                });
                foreach (var database in databases)
                {
                    Logger.Log(LogType.INFORMATION, string.Format("Starting maintenance for database: {0}.", database));

                    Logger.Log(LogType.INFORMATION, string.Format("Updating statistics for database: {0}.", database));

                    // Update statistics
                    var sql = string.Format("USE [{0}]; EXEC SP_UPDATESTATS;", database);
                    db.Execute(sql, null, null, 3600);

                    // Refresh Indexes
                    Logger.Log(LogType.INFORMATION, string.Format("Refreshing indexes for database: {0}.", database));
                    sql = GetRefreshIndexesSql(database);
                    db.Execute(sql, null, null, 3600);

                    // Recompile Objects
                    RecompileObjectsForDatabase(db, database);

                    Logger.Log(LogType.INFORMATION, string.Format("Completed maintenance for database: {0}.", database));
                }

                // Check for unused objects
                CheckUnusedObject(db);
            }
        }

        private void CheckUnusedObject(SqlConnection db)
        {
            Logger.Log(LogType.INFORMATION, $"Starting check for Unused MSSQL objects.");

            int c = 0;
            var sql = @"
;WITH cteObjects
AS
(
	SELECT
		OBJECT_NAME,
		OBJECT_TYPE
	FROM
		SERVER_OBJECT OBJ
)

SELECT
	*
FROM
	cteObjects OBJ
WHERE
	NOT EXISTS (
		SELECT
			NULL
		FROM
			SERVER_DEPENDENCY DEP
		WHERE
			DEP.CHILD_NAME = OBJ.OBJECT_NAME
	) AND
	NOT EXISTS (
		SELECT
			NULL
		FROM
			DATASET D
		WHERE
			D.SOURCE_NAME = OBJ.OBJECT_NAME
	) AND
	OBJECT_TYPE <> 'UNKNOWN'
ORDER BY
    OBJECT_NAME
";

            var data = db.Query(sql);
            foreach (var item in data)
            {
                c++;
                // log information:
                Logger.Log(LogType.INFORMATION, $"Object: [{item.OBJECT_NAME}], Type: [{item.OBJECT_TYPE}] appears to be unused.");
            }
            Logger.Log(LogType.INFORMATION, $"{c} unused MSSQL objects detected. Delete any of these objects WITH CAUTION.");

            Logger.Log(LogType.INFORMATION, $"Completed check for unused MSSQL objects.");
        }

        private void RecompileObjectsForDatabase(SqlConnection db, string database)
        {
            // reset any settings
            db.Execute($"SET SHOWPLAN_ALL OFF;");
            db.Execute($"SET NOEXEC OFF;");

            int c = 0;
            Logger.Log(LogType.INFORMATION, $"Starting recompilation of objects for database: {database}.");

            // Functions (sp_refreshsqlmodule)
            var functions = db.Query($"USE [{database}]; SELECT SCHEMA_NAME(schema_id) OBJECT_SCHEMA, [name] OBJECT_NAME FROM sys.objects WHERE [type] IN ('FN', 'IF', 'TF');");
            foreach (var function in functions)
            {
                try
                {
                    c++;
                    db.Execute($"USE [{database}]; EXEC SP_REFRESHSQLMODULE '[{function.OBJECT_SCHEMA}].[{function.OBJECT_NAME}]';");
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.ERROR, $"Object: {database}.{function.OBJECT_SCHEMA}.{function.OBJECT_NAME} failed module refresh: {ex.Message}.");
                }
            }

            // Sprocs
            var sprocs = db.Query($"USE [{database}]; SELECT SCHEMA_NAME(schema_id) OBJECT_SCHEMA, [name] OBJECT_NAME FROM sys.objects WHERE [type] IN ('P');");

            foreach (var sproc in sprocs)
            {
                try
                {
                    c++;

                    // https://stackoverflow.com/questions/1177659/syntax-check-all-stored-procedures
                    // Warning: ninja code ahead.
                    // sp_recompile only removes plan from cache. Does not check / recompile sproc.
                    // We need to 'force' SQL to recompile / check.
                    // Must put SET SHOWPLAN_TEXT ON in own batch.
                    db.Execute($"SET SHOWPLAN_ALL ON;");
                    db.Execute($"USE [{database}]; EXEC SP_RECOMPILE '[{sproc.OBJECT_SCHEMA}].[{sproc.OBJECT_NAME}]'; SET NOEXEC ON; EXEC [{sproc.OBJECT_SCHEMA}].[{sproc.OBJECT_NAME}];");
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.ERROR, $"Object: {database}.{sproc.OBJECT_SCHEMA}.{sproc.OBJECT_NAME} failed recompilation: {ex.Message}.");
                }
            }

            // Views (sp_refreshview)
            db.Execute($"SET SHOWPLAN_ALL OFF;");
            var views = db.Query($"USE [{database}]; SELECT SCHEMA_NAME(schema_id) OBJECT_SCHEMA, [name] OBJECT_NAME FROM sys.objects WHERE [type] IN ('V');");

            foreach (var view in views)
            {
                try
                {
                    c++;
                    db.Execute($@"USE [{database}]; EXEC sp_refreshview @NAME;", new
                    {
                        NAME = $"[{view.OBJECT_SCHEMA}].[{view.OBJECT_NAME}]"
                    });
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.ERROR, $"Object: {database}.{view.OBJECT_SCHEMA}.{view.OBJECT_NAME} failed refresh: {ex.Message}.");
                }
            }

            Logger.Log(LogType.INFORMATION, $"Completed recompilation of {c} objects for database: {database}.");
        }

        private string GetRefreshIndexesSql(string databaseName)
        {
            return string.Format(@"
USE [{0}];
DECLARE @schema VARCHAR(50)
DECLARE @table VARCHAR(256)
DECLARE @index VARCHAR(256)
DECLARE @frag FLOAT
DECLARE @pageCount INT

DECLARE curIndexes CURSOR FOR
SELECT
    dbschemas.[name] [Schema],
    dbtables.[name] [Table],
    dbindexes.[name] [Index],
    indexstats.avg_fragmentation_in_percent [FragPct],
    indexstats.page_count [PageCount]
FROM
    sys.dm_db_index_physical_stats (DB_ID(), NULL, NULL, NULL, NULL) AS indexstats
INNER JOIN
    sys.tables dbtables
ON
    dbtables.[object_id] = indexstats.[object_id]
INNER JOIN
    sys.schemas dbschemas
ON
    dbtables.[schema_id] = dbschemas.[schema_id]
INNER JOIN
    sys.indexes AS dbindexes
ON
    dbindexes.[object_id] = indexstats.[object_id] AND
    indexstats.index_id = dbindexes.index_id
WHERE
    indexstats.database_id = DB_ID() AND
	indexstats.page_count <= 1000000
ORDER BY
    indexstats.avg_fragmentation_in_percent desc

OPEN curIndexes
FETCH NEXT FROM curIndexes INTO @schema, @table, @index, @frag, @pageCount

-- NOTE IF THE @INDEX IS NULL, THIS REFERS TO A HEAP TABLE WITHOUT INDEXES
-- THESE ARE TYPICALLY HIGHLY FRAGMENTED AND CANNOT BE REBUILT. NEED TO
-- ADD PRIMARY KEYS TO THESE TABLES.
PRINT('Starting index refresh for database ' + DB_NAME())
WHILE @@FETCH_STATUS = 0  
BEGIN  
    DECLARE @sql VARCHAR(1000)
    IF @frag >= 30 AND @SCHEMA IS NOT NULL AND @TABLE IS NOT NULL AND @INDEX IS NOT NULL
    BEGIN
        SET @sql = 'ALTER INDEX [' + @index + '] ON [' + @schema + '].[' + @table + '] REBUILD'
        PRINT('Rebuilding index: [' + @index + '] ON [' + @schema + '].[' + @table + ']...')
        EXEC(@sql)
    END
    ELSE IF @frag >= 5 AND @SCHEMA IS NOT NULL AND @TABLE IS NOT NULL AND @INDEX IS NOT NULL
    BEGIN
        SET @sql = 'ALTER INDEX [' + @index + '] ON [' + @schema + '].[' + @table + '] REORGANIZE'
        PRINT('Reorganising index: [' + @index + '] ON [' + @schema + '].[' + @table + ']...')
        EXEC(@sql)
    END
    FETCH NEXT FROM curIndexes INTO @schema, @table, @index, @frag, @pageCount
END 
PRINT('Completed index refresh for database ' + DB_NAME())

CLOSE curIndexes
DEALLOCATE curIndexes", databaseName);
        }

        #endregion
    }
}
