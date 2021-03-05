using Sara.Lib.ConfigurableCommands.Actions;
using Sara.Lib.ConfigurableCommands.Loaders;
using Sara.Lib.Extensions;
using Sara.Lib.Logging;
using Sara.Lib.Metadata;
using Sara.Lib.Models.ConfigurableCommand;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Sara.Lib.ConfigurableCommands
{
    /// <summary>
    /// Abstract base class for AbstractAction and AbstractLoader.
    /// </summary>
    public abstract class AbstractConfigurableCommand
    {
        /// <summary>
        /// Provides access to the metadata. Gets set by the static factory method.
        /// </summary>
        protected IMetadataRepository MetadataRepository { get; set; }

        /// <summary>
        /// To log from within a configurable command.
        /// </summary>
        public ILogger Logger { get; set; }

        public static List<ConfigurableProperty> GetConfigurableProperties(ConfigurableCommandClassInfo configurableCommandClass)
        {
            List<ConfigurableProperty> result = new List<ConfigurableProperty>();

            // Get the properties for the ConfigurableCommand Class
            Type t = Type.GetType(configurableCommandClass.TypeName);
            var prop = t.GetPropertiesDecoratedBy<ConfigurablePropertyAttribute>().ToList();
            return prop.Select(p => new ConfigurableProperty
            {
                Property = p,
                PropertyAttribute = (ConfigurablePropertyAttribute)p.GetCustomAttribute(typeof(ConfigurablePropertyAttribute), true)
            }).ToList();
        }

        /// <summary>
        /// Factory method to create fully configured command.
        /// </summary>
        /// <param name="configurableCommandClass">The class to create.</param>
        /// <param name="commandProperties">Parameters to configure the command.</param>
        /// <param name="metadataRepository">Metadata repository useable by any command.</param>
        /// <returns></returns>
        public static AbstractConfigurableCommand Create(
            ConfigurableCommandClassInfo configurableCommandClass,
            IEnumerable<ConfigurableCommandProperty> commandProperties,
            IMetadataRepository metadataRepository
            )
        {
            if (commandProperties == null)
            {
                // empty list
                commandProperties = new List<ConfigurableCommandProperty>();
            }

            var configuredProperties = commandProperties
                .Select(p => new { Name = p.PropertyName, Value = p.PropertyValue })
                .ToDictionary(x => x.Name, x => x.Value);

            var obj = (AbstractConfigurableCommand)CreateConfigurableCommandInstance(configurableCommandClass);

            // Set the metadata repository property
            obj.MetadataRepository = metadataRepository;

            // Set the properties
            var availableProperties = AbstractConfigurableCommand.GetConfigurableProperties(configurableCommandClass);

            foreach (var availableProperty in availableProperties)
            {
                var value = (configuredProperties.ContainsKey(availableProperty.PropertyAttribute.Name) ? configuredProperties[availableProperty.PropertyAttribute.Name] : (availableProperty.PropertyAttribute.Default ?? "")).ToString();

                // Check if field can be interpolated using {...} expressions
                value = value.Interpolate();

                if (availableProperty.Property.PropertyType == typeof(string))
                {
                    availableProperty.Property.SetValue(obj, Convert.ChangeType(value, availableProperty.Property.PropertyType));
                }
                else if (availableProperty.Property.PropertyType.IsEnum)
                    availableProperty.Property.SetValue(obj, Enum.Parse(availableProperty.Property.PropertyType, value.ToString()));
                else if (availableProperty.Property.PropertyType == typeof(bool))
                {
                    // Handle booleans - can be string 'true' or 'false', or integer like 0/1
                    if (availableProperty.Property.PropertyType == typeof(bool))
                    {
                        int valueAsInt = 0;
                        if (int.TryParse(value.ToString(), out valueAsInt) == true)
                            availableProperty.Property.SetValue(obj, Convert.ChangeType(valueAsInt, typeof(int)));
                        else
                            availableProperty.Property.SetValue(obj, Convert.ChangeType(value, availableProperty.Property.PropertyType));
                    }
                }
                else
                    availableProperty.Property.SetValue(obj, Convert.ChangeType(value, availableProperty.Property.PropertyType));
            }
            return obj;
        }

        #region Private Methods

        private static AbstractConfigurableCommand CreateConfigurableCommandInstance(ConfigurableCommandClassInfo configurableCommandClass)
        {
            Type t = Type.GetType(configurableCommandClass.TypeName);
            var obj = (AbstractConfigurableCommand)Activator.CreateInstance(t);
            return obj;
        }

        #endregion
    }
}