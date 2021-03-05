using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sara.Lib.ConfigurableCommands
{
    /// <summary>
    /// An attribute that can be added to a property / field
    /// on a ConfigurableCommand.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Field, AllowMultiple=false)]
    public class ConfigurablePropertyAttribute : Attribute
    {
        public int Seq { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Mandatory { get; set; }
        public object Default { get; set; }
        public string Help { get; set; }
        public string Validation { get; set; }
        public bool AllowMultiLine { get; set; }
    }
}
