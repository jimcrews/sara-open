using Sara.Lib.ConfigurableCommands;
using Sara.Lib.Models;
using Sara.Lib.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sara.Lib.Logging;
using Sara.Lib.ConfigurableCommands.Loaders;
using Sara.Lib.Csv;

namespace Sara.Lib.ConfigurableCommands.Loaders
{
    public class FileLoader : AbstractLoader
    {
        [ConfigurableProperty(Seq = 1, Name = "FILE_PATH", Mandatory = true, Help = @"e.g. C:\MyData\MyFile.csv", Description = "The full path and filename to the csv file.")]
        public string FilePath { get; set; }

        [ConfigurableProperty(Seq = 2, Name ="FIELD_DELIMITER", Default = ',', Mandatory = false, Validation = "^[,|/||;]$", Description ="The delimiter used to separate fields in the file. Normally a comma.")]
        public char FieldDelimiter { get; set; }

        [ConfigurableProperty(Seq = 3, Name = "TEXT_DELIMITER", Default = '"', Mandatory = false, Validation = "^[\"|\']$", Description = "The character used to delimit text values. Normally double quotes.")]
        public char TextDelimiter { get; set; }

        [ConfigurableProperty(Seq = 4, Name = "FIRST_ROW_HEADER_FLAG", Default = true, Mandatory = false, Validation = "^(true|false)$", Description = "Set to true if the file contains a header row on row 1.")]
        public bool FirstRowHeaderFlag { get; set; }

        [ConfigurableProperty(Seq = 5, Name = "CONVERT_EMPTY_TO_NULL_FLAG", Default = false, Mandatory = false, Validation = "^(true|false)$", Description = "Set to true if you want empty values to be converted to null values.")]
        public bool ConvertEmptyToNullFlag { get; set; }

        [ConfigurableProperty(Seq = 6, Name = "FIRST_DATA_ROW", Default = 2, Mandatory = false, Validation = "^[0-9]{1,10}", Help = "e.g. 2", Description = "The first data row. For normal csv files with a header row, enter '2' here.")]
        public int FirstDataRow { get; set; }

        [ConfigurableProperty(Seq = 7, Name = "ALLOW_RAGGED", Default = false, Validation = "^(true|false)$", Description = "Set to true if the file contains rows with variable numbers of columns in each row.")]
        public bool AllowRagged { get; set; }

        [ConfigurableProperty(Seq = 8, Name = "CASE_INSENSITIVE_HEADERS", Default = true, Validation = "^(true|false)$", Description = "Set to true for case insensitive headers.")]
        public bool CaseInsensitiveHeaders { get; set; }

        public override IEnumerable<DataColumn> Probe()
        {
            int MAX_ROWS = 99999999;
            var rows = this.Read(MAX_ROWS);

            var firstRow = rows.FirstOrDefault();
            if (firstRow == null) throw new Exception("File source is empty cannot determine column metadata");

            Dictionary<object, ColumnType> fields = new Dictionary<object, ColumnType>();
            Dictionary<object, int> lengths = new Dictionary<object, int>();

            return rows.ProbeColumns();

        }

        public override IEnumerable<IDictionary<string, object>> Read(int? maxDataRows=null)
        {
            var filePath = this.FilePath;

            using (var stream = File.Open(filePath, FileMode.Open))
            {
                var parser = new CsvParser(
                    stream,
                    FieldDelimiter,
                    TextDelimiter,
                    FirstDataRow,
                    FirstRowHeaderFlag,
                    ConvertEmptyToNullFlag,
                    CaseInsensitiveHeaders);

                parser.AllowRaggedFlag = AllowRagged;
                parser.InvalidRow += new CsvParser.InvalidRowHandler(parser_InvalidRow);

                foreach (var row in parser.Parse())
                {
                    if (maxDataRows.HasValue && RowsAffected >= maxDataRows) break;
                    yield return row;
                    RowsAffected++;
                }
            }
        }

        void parser_InvalidRow(object source, InvalidRowEventArgs args)
        {
            throw new Exception(string.Format("File has invalid row. Check number of columns matches header columns: [{0}]", args.RawData));
        }
    }
}
