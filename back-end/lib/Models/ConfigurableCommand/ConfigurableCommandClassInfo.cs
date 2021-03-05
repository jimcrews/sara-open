using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Models.ConfigurableCommand
{
    /// <summary>
    /// Used to return list of configurable commands.
    /// </summary>
    public class ConfigurableCommandClassInfo
    {
        /// <summary>
        /// Display name of the configurable command.
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// Type of the configurableCommand.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ConfigurableCommandType ClassType { get; set; }

        /// <summary>
        /// Description for the configurable command.
        /// </summary>
        public string ClassDesc { get; set; }

        /// <summary>
        /// The .NET type name for the configurable command.
        /// </summary>
        public string TypeName { get; set; }
    }
}