using Sara.Lib.Extensions;
using Sara.Lib.Models.ConfigurableCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sara.Lib.Cron
{
    /// <summary>
    /// Schedules commands based on their schedules
    /// </summary>
    public class Scheduler
    {
        IEnumerable<ConfigurableCommandScheduleInfo> Schedules;
        IEnumerable<ConfigurableCommandPreconditionInfo> Preconditions;
        IEnumerable<ConfigurableCommandInfo> ConfigurableCommands;

        private Dictionary<int, DateTime> nextScheduleDt = new Dictionary<int, DateTime>();
        List<int> oneTimeSchedulesToDelete = new List<int>();

        public Scheduler(
            IEnumerable<ConfigurableCommandInfo> configurableCommands,
            IEnumerable<ConfigurableCommandScheduleInfo> schedules,
            IEnumerable<ConfigurableCommandPreconditionInfo> preconditions)
        {
            this.Schedules = schedules;
            this.Preconditions = preconditions;
            this.ConfigurableCommands = configurableCommands;

            Init();
        }

        public Dictionary<int, DateTime> NextDt => this.nextScheduleDt;

        public List<int> OneTimeSchedules => this.oneTimeSchedulesToDelete;

        /// <summary>
        /// The start date / time used for the scheduler algorithm.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Initialises the schedule. Works out the next date for ALL commands regardless
        /// of whether to actually change the schedule.
        /// </summary>
        private void Init()
        {
            // We schedule from the NEXT minute rounded down.
            // This is to prevent the scheduler scheduling twice in same minute
            // as extractors may have just started same loader and removed
            // entry from LOADER_QUEUE.
            StartDate = DateTime.Now.AddMinutes(1).TruncateToMinute();

            // Enabled commands
            var enabledCommands = ConfigurableCommands.Where(c => c.EnabledFlag).Select(c => c.ConfigurableCommandId.Value);

            // Enabled schedules
            var schedules = Schedules
                .Where(s => s.EnabledFlag)
                .Where(s => enabledCommands.Contains(s.ConfigurableCommandId));

            // Get preconditions with auto_schedule_flag = true. For these, we will also generate a schedule.
            var preconditions = Preconditions
                .Where(p => p.AutoScheduleFlag)
                .Where(p => enabledCommands.Contains(p.ConfigurableCommandId));

            // Calculate the next extract due for each schedule from 'now'.
            // Ignore schedule if there is existing queue entry (as we must let this
            // process to completion first).
            Dictionary<int, DateTime?> nextRunByConfigurableCommandId = new Dictionary<int, DateTime?>();

            // There can be several schedules for the same configurable command. Get the earliest next_dt for each one.
            foreach (var schedule in schedules)
            {
                DateTime? next = schedule.Next(StartDate);
                if (!nextRunByConfigurableCommandId.ContainsKey(schedule.ConfigurableCommandId))
                    nextRunByConfigurableCommandId[schedule.ConfigurableCommandId] = next;
                else if (schedule.Next(StartDate) < nextRunByConfigurableCommandId[schedule.ConfigurableCommandId])
                    nextRunByConfigurableCommandId[schedule.ConfigurableCommandId] = next;
            }

            // Do Preconditions - any command that has a precondition set to auto-schedule=true can have its next
            // schedule time created / altered
            var candidates = preconditions.Select(p => p.ConfigurableCommandId).Distinct();
            foreach (var candidate in candidates)
            {
                GetPreconditionNextDt(candidate, candidate, preconditions, nextRunByConfigurableCommandId);
            }

            // check if any of the next run dates are from one-time schedules. If so these should be deleted at end
            foreach (var schedule in schedules)
            {
                if (nextRunByConfigurableCommandId.ContainsKey(schedule.ConfigurableCommandId) && schedule.OneTime && schedule.Next(StartDate) <= nextRunByConfigurableCommandId[schedule.ConfigurableCommandId])
                {
                    oneTimeSchedulesToDelete.Add(schedule.ConfigurableCommandScheduleId.Value);
                }
            }

            // Now, go through each schedule by configurable command ID adding queue entries
            foreach (var item in nextRunByConfigurableCommandId)
            {
                var next = item.Value.Value.TruncateToMinute();
                nextScheduleDt[item.Key] = next;
            }
        }


        /// <summary>
        /// Recursively traverse backwards through the preconditions, getting the earliest next date/time.
        /// </summary>
        /// <param name="rootId">The top-level command. Used to check for circular references.</param>
        /// <param name="configurableCommandId">Current configurable command being checked for preconditions.</param>
        /// <param name="preconditions">Full list of preconditions that have auto-refresh set to true.</param>
        /// <param name="nextRunByConfigurableCommandId">List of next run dates. Gets updated recursively.</param>
        private void GetPreconditionNextDt(int rootId, int configurableCommandId, IEnumerable<ConfigurableCommandPreconditionInfo> preconditions, Dictionary<int, DateTime?> nextRunByConfigurableCommandId)
        {
            var items = preconditions.Where(p => p.ConfigurableCommandId == configurableCommandId);
            var nextDt = nextRunByConfigurableCommandId.ContainsKey(configurableCommandId) ? nextRunByConfigurableCommandId[configurableCommandId] : null;

            if (items.Any(p => p.PreconditionId == rootId))
            {
                throw new Exception($"Circular dependency found in preconditions for configurable command id: {rootId}");
            }
            if (items.Any())
            {
                foreach (var item in items)
                {
                    GetPreconditionNextDt(rootId, item.PreconditionId, preconditions, nextRunByConfigurableCommandId);
                    if (nextRunByConfigurableCommandId.ContainsKey(item.PreconditionId))
                    {
                        var preconditionNextDt = nextRunByConfigurableCommandId[item.PreconditionId];

                        // Update / insert next schedule for current command with precondition next dt
                        if (!nextRunByConfigurableCommandId.ContainsKey(item.ConfigurableCommandId))
                            nextRunByConfigurableCommandId[item.ConfigurableCommandId] = preconditionNextDt;
                        else if (preconditionNextDt < nextRunByConfigurableCommandId[item.ConfigurableCommandId])
                            nextRunByConfigurableCommandId[item.ConfigurableCommandId] = preconditionNextDt;
                    }
                }
            }
            return;
        }
    }
}