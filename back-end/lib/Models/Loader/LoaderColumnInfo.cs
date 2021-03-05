using Sara.Lib.ConfigurableCommands.Loaders;

namespace Sara.Lib.Models.Loader
{
    /// <summary>
    /// Represents a loader column for a loader command.
    /// </summary>
    public class LoaderColumnInfo : DataColumn
    {
        public int ConfigurableCommandId { get; set; }
        public bool Selected { get; set; }
    }
}