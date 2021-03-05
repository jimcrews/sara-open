using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Models.ConfigurableCommand
{
    public class ConfigurableCommandScheduleInfo
    {
        public int? ConfigurableCommandScheduleId { get; set; }
        public int ConfigurableCommandId { get; set; }
        public string Cron { get; set; }
        public bool OneTime { get; set; }
        public bool EnabledFlag { get; set; }
        public DateTime? Next(DateTime? startDate = null)
        {
            Cron.Cron c = Sara.Lib.Cron.Cron.Create(this.Cron);
            DateTime? next = c.Next(startDate);
            return next;
        }
    }
}