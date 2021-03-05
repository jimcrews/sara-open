using Sara.Lib.ConfigurableCommands;
using Sara.Lib.ConfigurableCommands.Loaders;
using Sara.Lib.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Sara.Lib.ConfigurableCommands.Loaders
{
    public enum MockDataGenerationMode
    {
        Fixed,
        Random,
        Incremental
    }

    public enum MockColumnType
    {
        String,
        Number,
        Decimal,
        Boolean,
        Date,
        Time,
        DateTime
    }

    public class MockColumn
    {
        public string Name { get; set; }
        public MockColumnType ColumnType { get; set; }
        public int Length { get; set; }
        public MockDataGenerationMode GenerationMode { get; set; }
        public float Start { get; set; }
        public float Increment { get; set; }
        /// <summary>
        /// Percentage of rows with null values.
        /// </summary>
        public float SparseFactor { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class MockLoader : AbstractLoader
    {
        [ConfigurableProperty(Seq = 1, Name = "SCHEMA", Mandatory = true, Help = @"NAME,STRING,50,RANDOM,0,AGE,NUMBER,0,INCREMENTAL,0.1", Description = "Enter the schema in format [column name],[data type],[length],[null percentage]. Valid types are STRING,NUMBER,DECIMAL,BOOLEAN,DATE,TIME,DATETIME. Null percentage must be between 0 and 1.")]
        public string Schema { get; set; }

        [ConfigurableProperty(Seq = 2, Name = "ROWS", Mandatory = true, Help = @"1000", Description = "Enter the number of rows to generate.")]
        public int Rows { get; set; }

        public override IEnumerable<DataColumn> Probe()
        {
            int MAX_ROWS = 99999999;
            var rows = this.Read(MAX_ROWS);
            return rows.ProbeColumns();
        }

        public override IEnumerable<IDictionary<string, object>> Read(int? maxDataRows = null)
        {
            return GenerateData();
        }

        /// <summary>
        /// Generates a single value.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public object GenerateValue(MockColumn column, int row)
        {
            var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            // Fixed Data
            Dictionary<MockColumnType, object> fixedData = new Dictionary<MockColumnType, object>() {
                {MockColumnType.String, "X"},
                {MockColumnType.Boolean, true },
                {MockColumnType.Date, DateTime.Today.Date },
                {MockColumnType.DateTime, DateTime.Now},
                {MockColumnType.Time, DateTime.Now.TimeOfDay},
                {MockColumnType.Decimal, 1.2345 },
                {MockColumnType.Number, 12345 }
            };

            // Random Data
            Dictionary<MockColumnType, Func<object>> randomData = new Dictionary<MockColumnType, Func<object>>() {
                {MockColumnType.String, () => string.Join("", new string(' ', column.Length).Select(c=>alphabet[(int)new Random().Next(26)])) },
                {MockColumnType.Boolean, () => new Random().Next(1000) < 500 ? true : false },
                {MockColumnType.Date, () => DateTime.Today.Date.AddDays((int)new Random().Next(365)*-1) },
                {MockColumnType.DateTime, () => DateTime.Now.AddDays((int)new Random().Next(365)*-1)},
                {MockColumnType.Time, () => DateTime.Now.AddSeconds((int)new Random().Next(86400)*-1).TimeOfDay},
                {MockColumnType.Decimal, () => new Random().Next(1000000) },
                {MockColumnType.Number, () => (int)new Random().Next(1000000) }
            };

            // Incremental Data
            Dictionary<MockColumnType, Func<float, float, int, object>> incrementalData = new Dictionary<MockColumnType, Func<float, float, int, object>>() {
                {MockColumnType.String, (start, increment, row) => IntToString((int)(start + (row*increment)), alphabet.ToCharArray()) },
                {MockColumnType.Boolean, (start, increment, row) => (((int)(start + (row*increment)) % 2) == 0) },
                {MockColumnType.Date, (start, increment, row) => new DateTime((long)start).AddMilliseconds(increment * row).Date },
                {MockColumnType.DateTime, (start, increment, row) => new DateTime((long)start).AddMilliseconds(increment * row) },
                {MockColumnType.Time, (start, increment, row) => new DateTime((long)start).AddMilliseconds(increment * row).TimeOfDay },
                {MockColumnType.Decimal, (start, increment, row) => (int)(start + (row*increment)) },
                {MockColumnType.Number, (start, increment, row) => (int)(start + (row*increment)) }
            };

            if (new Random().Next(100000) < (column.SparseFactor * 100000))
            {
                // emit null value
                return null;
            }
            else if (column.GenerationMode == MockDataGenerationMode.Random)
            {
                // random value
                return randomData[column.ColumnType]();
            }
            else if (column.GenerationMode == MockDataGenerationMode.Fixed)
            {
                // fixed value
                return randomData[column.ColumnType];
            }
            else if (column.GenerationMode == MockDataGenerationMode.Incremental)
            {
                // fixed value
                return incrementalData[column.ColumnType](column.Start, column.Increment, row);
            }
            else
            {
                throw new Exception("Invalid configuration.");
            }
        }

        private IEnumerable<IDictionary<string, object>> GenerateData()
        {
            var schemaArray = Schema.Split(new string[] { "," }, StringSplitOptions.None);
            var columnCount = schemaArray.Count() / 7;
            var columns = new List<MockColumn>();

            if (!schemaArray.Any() || schemaArray.Count() == 0 || schemaArray.Count() % 7 != 0)
            {
                throw new Exception("Invalid schema provided.");
            }

            if (Rows < 0 || Rows >= 1000000)
            {
                throw new Exception("Rows must be between 1 and 999,999");
            }

            // Prepare columns
            for (int i = 0; i < columnCount; i++)
            {
                var mockColumn = new MockColumn();
                mockColumn.Name = schemaArray[i * 7];
                mockColumn.ColumnType = (MockColumnType)Enum.Parse(typeof(MockColumnType), schemaArray[(i * 7) + 1]);
                mockColumn.Length = int.Parse(schemaArray[(i * 7) + 2]);
                mockColumn.GenerationMode = (MockDataGenerationMode)Enum.Parse(typeof(MockDataGenerationMode), schemaArray[(i * 7) + 3]);
                mockColumn.Start = float.Parse(schemaArray[(i * 7) + 4]);
                mockColumn.Increment = float.Parse(schemaArray[(i * 7) + 5]);
                mockColumn.SparseFactor = float.Parse(schemaArray[(i * 7) + 6]);
                columns.Add(mockColumn);
            }

            // Generate data
            for (int i = 0; i < Rows; i++)
            {
                Dictionary<string, object> row = new Dictionary<string, object>();
                for (int j = 0; j < columns.Count(); j++)
                {
                    var column = columns[j];
                    row[column.Name] = GenerateValue(column, i);
                }
                yield return row;
            }
        }

        public string IntToString(int value, char[] baseChars)
        {
            string result = string.Empty;
            int targetBase = baseChars.Length;

            do
            {
                result = baseChars[(value % targetBase)] + result;
                value = value / targetBase;
            }
            while (value > 0);

            return result;
        }
    }
}
