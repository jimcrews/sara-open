namespace Sara.Lib.Models.Server
{
    public class ServerParameterInfo
    {
        public string ServerName { get; set; }
        public string ObjectName { get; set; }
        public int ParameterId { get; set; }
        public string ParameterName { get; set; }
        public string DataType { get; set; }
        public int MaximumLength { get; set; }
        public bool IsNullable { get; set; }
    }
}