using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Models.Server
{
    public class ServerDependencyInfo
    {
        public string ServerName { get; set; }
        public string ParentName { get; set; }
        public string ChildName { get; set; }
    }
}
