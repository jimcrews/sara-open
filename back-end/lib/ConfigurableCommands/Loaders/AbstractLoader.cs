using Sara.Lib.ConfigurableCommands;
using Sara.Lib.Models;
using Sara.Lib.Models.Loader;
using Sara.Lib.Extensions;
using Sara.Lib.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Sara.Lib.Logging;

namespace Sara.Lib.ConfigurableCommands.Loaders
{
    /// <summary>
    /// A loader encapsulates an extraction from an external source.
    /// An concrete instance of a loader is created from configured properties.
    /// </summary>
    public abstract class AbstractLoader : AbstractConfigurableCommand
    {
        /// <summary>
        /// This method probes the loader to retrieve a list of columns.
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<DataColumn> Probe();

        /// <summary>
        /// This method retrieves the raw data from the source.
        /// </summary>
        /// <param name="maxDataRows"></param>
        /// <returns></returns>
        public abstract IEnumerable<IDictionary<string, object>> Read(int? maxDataRows = null);

        /// <summary>
        /// Reads the raw data from the source, but casts the values to .NET types matching
        /// the target schema provided.
        /// </summary>
        /// <param name="columnTypes"></param>
        /// <param name="maxDataRows"></param>
        /// <returns></returns>
        public IEnumerable<IDictionary<string, object>> ReadFormatted(
            IList<LoaderColumnInfo> columns,
            int? maxDataRows = null)
        {
            var columnTypes = columns.ToDictionary(c => c.ColumnName, c => c.DataType.ToDotNetType());

            Dictionary<string, Type> columnTypesInsensitive = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in columnTypes.Keys)
                columnTypesInsensitive.Add(key, columnTypes[key]);

            foreach (var row in Read(maxDataRows))
            {
                Dictionary<string, object> formattedRow = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var key in row.Keys)
                {
                    var value = row[key];
                    var targetType = columnTypesInsensitive[key];

                    if (value == null || value == DBNull.Value)
                    {
                        formattedRow[key] = DBNull.Value;
                    }
                    else if (value is JsonObject)
                    {
                        formattedRow[key] = value.ToString();   // Convert to Json representation.
                    }
                    else if (value.GetType() == targetType)
                    {
                        formattedRow[key] = value;
                    }
                    else if (value.IsNumeric())
                    {
                        // eg. value is Int64, targetType = Int16
                        formattedRow[key] = Convert.ChangeType(value, targetType);
                    }
                    else if (value.GetType() == typeof(string))
                    {
                        if (!string.IsNullOrEmpty((string)value))
                            formattedRow[key] = value.ConvertTo(targetType);
                        else
                        {
                            // empty string
                            if (targetType.IsNumericType() || targetType == typeof(DateTime) || targetType == typeof(Date) || targetType == typeof(TimeSpan) || targetType == typeof(Guid))
                                formattedRow[key] = DBNull.Value;
                            else
                                formattedRow[key] = value;
                        }
                    }
                    else if (targetType == typeof(string))
                    {
                        formattedRow[key] = value.ToString();
                    }
                    else
                    {
                        // Helpful debug msg
                        throw new Exception(string.Format("Column: {0} -  Unable to format value '{1}', value type '{2}', target type '{3}'", key, (value ?? "<null>"), (value == null ? "<null>" : value.GetType().ToString()), targetType));
                    }
                }
                yield return formattedRow;
            }
        }

        public long RowsAffected { get; protected set; }
    }
}