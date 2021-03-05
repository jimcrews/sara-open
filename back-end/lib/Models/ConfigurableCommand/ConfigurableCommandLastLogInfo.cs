using Sara.Lib.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Models.ConfigurableCommand
{
    /// <summary>
    /// Last run information relating to configurable command's last execution. Used in the configurable command list.
    /// </summary>
    public class ConfigurableCommandLastLogInfo
    {
        public int ConfigurableCommandId { get; set; }
        public int LogId { get; set; }
        public DateTime LastRunDt { get; set; }
        public TimeSpan Duration { get; set; }
        public LogType LogStatus { get; set; }
        public int RowsAffected { get; set; }
    }
}