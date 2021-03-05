using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Models.ConfigurableCommand
{
    public class ConfigurableCommandQueueInfo
    {
        public int ConfigurableCommandId { get; set; }
        public DateTime NextDt { get; set; }
        public bool Running { get; set; }
    }
}