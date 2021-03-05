using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Models.Server
{
    public class ServerObjectInfo
    {
        public string ServerName { get; set; }
        public string ObjectName { get; set; }
        public string ObjectType { get; set; }
        public int RowCount { get; set; }
        public DateTime CreatedDt { get; set; }
        public DateTime ModifiedDt { get; set; }
        public DateTime UpdatedDt { get; set; }
        public string Definition { get; set; }
    }
}