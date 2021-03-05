using Sara.Lib.ConfigurableCommands;
using Sara.Lib.Models;
using Sara.Lib.Models.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sara.Lib.ConfigurableCommands
{
    /// <summary>
    /// Represents a property on a loader or action that can be configured.
    /// </summary>
    public class ConfigurableProperty
    {
        public PropertyInfo Property {get; set;}
        public ConfigurablePropertyAttribute PropertyAttribute { get; set; }
    }
}
