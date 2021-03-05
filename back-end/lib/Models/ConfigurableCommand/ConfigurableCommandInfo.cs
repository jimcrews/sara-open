using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Models.ConfigurableCommand
{
    public class ConfigurableCommandInfo
    {
        public int? ConfigurableCommandId { get; set; }
        public string Description { get; set; }
        public string Comment { get; set; }
        public string ClassName { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDt { get; set; }
        public bool EnabledFlag { get; set; }
    }
}