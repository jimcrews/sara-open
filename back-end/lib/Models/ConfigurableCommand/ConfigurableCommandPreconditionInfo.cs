using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Models.ConfigurableCommand
{
    public class ConfigurableCommandPreconditionInfo
    {
        /// <summary>
        /// The current command.
        /// </summary>
        public int ConfigurableCommandId { get; set; }

        /// <summary>
        /// The dependant / precondition command
        /// </summary>
        public int PreconditionId { get; set; }

        /// <summary>
        /// Determines action to take on this command based on the exit status of the precondition command:
        /// Null: always run this command
        /// False: Only run this command if the precondition failed
        /// True: Only run this command if the precondition succeeded
        /// </summary>
        public bool? SuccessFlag { get; set; }

        /// <summary>
        /// When set to true, this command will be automatically schedules (one-time schedule) on the same
        /// frequency as any/all precondition schedules.
        /// </summary>
        public bool AutoScheduleFlag { get; set; }
    }
}