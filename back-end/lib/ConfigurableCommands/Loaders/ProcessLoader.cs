using Sara.Lib.ConfigurableCommands;
using Sara.Lib.Models;
using Sara.Lib.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sara.Lib.Logging;
using Sara.Lib.ConfigurableCommands.Loaders;

namespace Sara.Lib.ConfigurableCommands.Loaders
{
    public enum ProcessOutputType
    {
        CSV,
        JSON
    }

    /// <summary>
    /// Runs an executable process which returns data via the standard output.
    /// </summary>
    public class ProcessLoader : AbstractLoader
    {
        [ConfigurableProperty(Seq = 1, Name = "FILE_NAME", Mandatory = true, Help = "e.g. C:\\exe\\myprocess.exe", Description = "The file path of the process to execute. The process must return data in standard output.")]
        public string FileName { get; set; }

        [ConfigurableProperty(Seq = 2, Name = "ARGUMENTS", Help = "e.g. arg1 arg2 arg3", Description = "Optional arguments to provide to the process.")]
        public string ProcessArguments { get; set; }

        [ConfigurableProperty(Seq = 3, Name = "OUTPUT_TYPE", Help = "e.g. JSON", Description = "The output type of data that the process emits. Can be JSON or CSV.", Default = ProcessOutputType.JSON )]
        public ProcessOutputType OutputType { get; set; }

        /// <summary>
        /// Executes a query to probe the structure of the results.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<DataColumn> Probe()
        {
            var rows = Read();
            return rows.ProbeColumns();
        }

        public override IEnumerable<IDictionary<string, object>> Read(int? maxDataRows = null)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = FileName,
                    Arguments = ProcessArguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            //* Read the output (or the error)
            string output = proc.StandardOutput.ReadToEnd();
            string err = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            var json = output;

            if (!string.IsNullOrEmpty(err))
                throw new Exception(err);

            // parse the output
            if (OutputType == ProcessOutputType.JSON)
            {
                foreach (var item in JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json))
                {
                    yield return item;
                    RowsAffected++;
                    if (maxDataRows.HasValue)
                        maxDataRows--;
                    if (maxDataRows.HasValue && maxDataRows <= 0)
                        break;
                }
            }
        }
    }
}
