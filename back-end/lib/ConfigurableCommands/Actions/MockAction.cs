using Sara.Lib.ConfigurableCommands.Actions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;

namespace Sara.Lib.ConfigurableCommands.Actions
{
    public enum MockActionStatus
    {
        SUCCESS,
        FAILURE
    }

    public class MockAction : AbstractAction
    {
        [ConfigurableProperty(Seq = 1, Name = "DELAY", Mandatory = true, Help = @"Mock the delay in seconds", Description = "Enter the mock delay in seconds.")]
        public int DelaySecs { get; set; }

        [ConfigurableProperty(Seq = 2, Name = "STATUS", Mandatory = false, Help = @"", Description = "The required status of the action.")]
        public MockActionStatus Status { get; set; }


        public override void Execute()
        {
            Logger.Log(Logging.LogType.INFORMATION, $"Sleeping for {DelaySecs} seconds...");

            Thread.Sleep(DelaySecs * 1000);

            if (Status==MockActionStatus.FAILURE)
            {
                throw new Exception("The action has failed.");
            }
        }
    }
}
