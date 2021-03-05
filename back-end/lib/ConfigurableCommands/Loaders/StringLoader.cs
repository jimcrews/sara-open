using Sara.Lib.Csv;
using Sara.Lib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sara.Lib.ConfigurableCommands.Loaders
{
    /// <summary>
    /// Loader that returns data provided in a static CSV string. Useful for mocking data.
    /// </summary>
    public class StringLoader : AbstractLoader
    {
        [ConfigurableProperty(Seq = 1, Name = "DATA", Mandatory = true, Help = @"", Description = "Enter the data in CSV format (including header row).")]
        public string Data { get; set; }

        public override IEnumerable<DataColumn> Probe()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IDictionary<string, object>> Read(int? maxDataRows = null)
        {
            using (var str = Data.ToStream())
            {
                CsvParser parser = new CsvParser(str);
                var obj = parser.Parse();
                return obj.ToList();
            }
        }
    }
}
