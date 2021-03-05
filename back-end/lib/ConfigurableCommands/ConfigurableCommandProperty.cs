using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sara.Lib.Extensions;
using Sara.Lib.Models.ConfigurableCommand;

namespace Sara.Lib.ConfigurableCommands
{
    /// <summary>
    /// Represents a property for a configurable command as configured in the database. Can be applied to loaders and actions.
    /// </summary>
    public class ConfigurableCommandProperty
    {
        public int ConfigurableCommandId { get; set; }
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }
        public ConfigurablePropertyAttribute Attribute { get; set; }

        /// <summary>
        /// Calculates initial property values given any existing state of properties.
        /// </summary>
        /// <param name="configurableCommandClass"></param>
        /// <param name="existingProperties"></param>
        /// <returns></returns>
        public static IEnumerable<ConfigurableCommandProperty> GetDefaults(
            ConfigurableCommandClassInfo configurableCommandClass,
            IEnumerable<ConfigurableCommandProperty> existingValues)
        {
            var properties = AbstractConfigurableCommand.GetConfigurableProperties(configurableCommandClass);

            foreach (var prop in properties)
            {
                var deflt = prop.PropertyAttribute.Default;
                if (prop.Property.PropertyType.IsEnum && deflt != null)
                {
                    deflt = ((Enum)deflt).ToText();
                }
                if (prop.Property.PropertyType == typeof(bool) && deflt != null)
                {
                    deflt = bool.Parse(deflt.ToString()) ? bool.TrueString : bool.FalseString;
                }
                if (!existingValues.Select(v => v.PropertyName).Contains(prop.PropertyAttribute.Name, StringComparer.OrdinalIgnoreCase))
                {
                    yield return new ConfigurableCommandProperty()
                    {
                        PropertyName = prop.PropertyAttribute.Name,
                        PropertyValue = (deflt!=null?deflt.ToString():""),
                        Attribute = prop.PropertyAttribute
                    };
                }
            }
        }

        /// <summary>
        /// Converts an IEnumerable of ConfigurableCommand objects (from a database) to an IEnumerable of ConfigurableCommandPropertyUI.
        /// These objects contain additional metadata useful for drawing UIs.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="classProperties"></param>
        /// <returns></returns>
        public static IEnumerable<ConfigurableCommandPropertyUI> GetPropertyUI(IEnumerable<ConfigurableCommandProperty> properties, IEnumerable<ConfigurableProperty> classProperties)
        {
            var displayMapping = new Dictionary<Type, ConfigurablePropertyDisplayType>
            {
                [typeof(string)] = ConfigurablePropertyDisplayType.Text,
                [typeof(bool)] = ConfigurablePropertyDisplayType.Boolean,
                [typeof(int)] = ConfigurablePropertyDisplayType.Number,
                [typeof(char)] = ConfigurablePropertyDisplayType.Text
            };

            foreach (var loaderProperty in properties)
            {
                var classProperty = classProperties.Single(a => a.PropertyAttribute.Name == loaderProperty.PropertyName);

                ConfigurableCommandPropertyUI ret = new ConfigurableCommandPropertyUI();
                ret.ConfigurableCommandId = loaderProperty.ConfigurableCommandId;
                ret.PropertyName = loaderProperty.PropertyName;
                ret.PropertyValue = loaderProperty.PropertyValue;

                ret.Seq = classProperty.PropertyAttribute.Seq;
                ret.Description = classProperty.PropertyAttribute.Description;
                ret.Mandatory = classProperty.PropertyAttribute.Mandatory;
                if (classProperty.Property.PropertyType.IsEnum)
                    ret.Default = ((Enum)classProperty.PropertyAttribute.Default).ToText();
                else if (classProperty.Property.PropertyType == typeof(bool))
                    ret.Default = ((bool?)classProperty.PropertyAttribute.Default ?? false).ToString();
                else
                    ret.Default = classProperty.PropertyAttribute.Default;

                ret.Help = classProperty.PropertyAttribute.Help;
                ret.Validation = classProperty.PropertyAttribute.Validation;

                if (displayMapping.ContainsKey(classProperty.Property.PropertyType))
                {
                    ret.DisplayType = displayMapping[classProperty.Property.PropertyType];
                }

                if (classProperty.Property.PropertyType.IsEnum)
                {
                    ret.DisplayType = ConfigurablePropertyDisplayType.List;
                    var enumList = new List<string>();
                    var enumValues = Enum.GetValues(classProperty.Property.PropertyType);
                    foreach (var item in enumValues)
                        enumList.Add(item.ToString());
                    ret.ListValues = enumList;
                }
                if (ret.DisplayType == ConfigurablePropertyDisplayType.Text && classProperty.PropertyAttribute.AllowMultiLine)
                    ret.DisplayType = ConfigurablePropertyDisplayType.MultilineText;
                yield return ret;
            }
        }
    }
}
