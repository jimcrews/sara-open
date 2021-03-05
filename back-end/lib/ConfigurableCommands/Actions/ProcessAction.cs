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

namespace Sara.Lib.ConfigurableCommands.Actions
{
    /// <summary>
    /// Runs an executable process to perform an action.
    /// </summary>
    public class ProcessAction : AbstractAction
    {
        [ConfigurableProperty(Seq = 1, Name = "FILE_NAME", Mandatory = true, Help = "e.g. C:\\exe\\myprocess.exe", Description = "The file path of the process to execute. The process must return data in standard output.")]
        public string FileName { get; set; }

        [ConfigurableProperty(Seq = 2, Name = "ARGUMENTS", Help = "e.g. arg1 arg2 arg3", Description = "Optional arguments to provide to the process.")]
        public string ProcessArguments { get; set; }

        public override void Execute()
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
            // Read the error output
            string output = proc.StandardOutput.ReadToEnd();
            string err = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            Logger.Log(LogType.INFORMATION, output);

            if (!string.IsNullOrEmpty(err))
                throw new Exception(err);

        }
    }
}
