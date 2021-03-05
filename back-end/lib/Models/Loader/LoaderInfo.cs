using Sara.Lib.ConfigurableCommands.Loaders;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace Sara.Lib.Models.Loader
{
    /// <summary>
    /// Extended configurable command information specific for loaders.
    /// </summary>
    public class LoaderInfo
    {
        public int? ConfigurableCommandId { get; set; }
        public string ServerName { get; set; }
        public string TargetName { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public TargetBehaviour TargetBehaviour { get; set; }
        public string BeforeRowProcessingSql { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public RowProcessingBehaviour RowProcessingBehaviour { get; set; }
        public string AfterRowProcessingSql { get; set; }
    }
}