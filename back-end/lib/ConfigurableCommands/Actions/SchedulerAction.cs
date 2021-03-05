using Sara.Lib.Cron;
using Sara.Lib.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.ConfigurableCommands.Actions
{
    /// <summary>
    /// The configurable command scheduler.
    /// </summary>
    public class SchedulerAction : AbstractAction
    {
        public override void Execute()
        {
            var configurableCommands = MetadataRepository.GetConfigurableCommands();
            var schedules = MetadataRepository.GetConfigurableCommandSchedules();
            var preconditions = MetadataRepository.GetConfigurableCommandPreconditions();
            var scheduler = new Scheduler(configurableCommands, schedules, preconditions);

            foreach (var item in scheduler.NextDt.Keys)
            {
                var hasRescheduled = MetadataRepository.OnScheduleConfigurableCommand(scheduler, item);
                if (hasRescheduled)
                    Logger.Log(LogType.INFORMATION, $"Set next date for item: {item} to {scheduler.NextDt[item]}.");
            }

            // Finally, delete one-time schedules
            foreach (var item in scheduler.OneTimeSchedules)
            {
                Logger.Log(LogType.INFORMATION, $"Removing one-time schedule: {item}");
                MetadataRepository.OnRemoveOneTimeSchedule(item);
            }
        }
    }
}
