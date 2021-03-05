using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.ConfigurableCommands
{
    public abstract class AbstractSchedule
    {
        public string Cron { get; set; }
        public bool OneTime { get; set; }

        public DateTime? Next(DateTime? startDate = null)
        {
            Cron.Cron c = Sara.Lib.Cron.Cron.Create(this.Cron);
            DateTime? next = c.Next(startDate);
            return next;
        }
    }
}
