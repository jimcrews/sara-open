using Sara.Lib.ConfigurableCommands.Loaders;

namespace Sara.Lib.Models.Server
{
    public class ServerColumnInfo : DataColumn
    {
        public string ServerName { get; set; }
        public string ObjectName { get; set; }
        public bool ReadOnly { get; set; }
        public bool Public { get; set; }
    }
}