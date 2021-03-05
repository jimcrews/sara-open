

namespace Sara.Lib.ConfigurableCommands.Loaders
{
    public class DataColumn
    {
        public string ColumnName { get; set; }
        public int Order { get; set; }
        public bool PrimaryKey { get; set; }
        public DataType DataType { get; set; }
        public int DataLength { get; set; }

        // For Decimal type only
        public int? Precision { get; set; }
        public int? Scale { get; set; }

    }
}