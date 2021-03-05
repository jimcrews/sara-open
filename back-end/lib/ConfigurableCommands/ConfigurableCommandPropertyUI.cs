using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.ConfigurableCommands
{
    /// <summary>
    /// Alternate version of the ConfigurableCommandProperty. Used to provide richer information via the API to clients.
    /// </summary>
    public class ConfigurableCommandPropertyUI
    {
        public int ConfigurableCommandId { get; set; }
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }

        // Attributes
        public int Seq { get; set; }
        public string Description { get; set; }
        public bool Mandatory { get; set; }
        public object Default { get; set; }
        public string Help { get; set; }
        public string Validation { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ConfigurablePropertyDisplayType DisplayType { get; set; }
        public IEnumerable<string> ListValues { get; set; }
    }
}
