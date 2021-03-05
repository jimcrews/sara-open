using Sara.Lib.Json;
using System;
using System.Collections.Generic;

namespace Sara.Lib.ConfigurableCommands.Loaders
{
    public class ColumnType
    {
        /// <summary>
        /// The DataLoader type
        /// </summary>
        public DataType DataType { get; set; }

        /// <summary>
        /// The .NET type
        /// </summary>
        public Type DotNetType { get; set; }

        /// <summary>
        /// The next DataLoader type (when attempting to probe the type of a set of data values).
        /// </summary>
        public DataType Next { get; set; }

        /// <summary>
        /// The width of the data type
        /// </summary>
        public int Width { get; set; }
        public ColumnType(DataType dataType, Type dotNetType, DataType next, int width)
        {
            DataType = dataType;
            DotNetType = dotNetType;
            Next = next;
            Width = width;
        }

        public static List<ColumnType> Create()
        {
            return new List<ColumnType>()
            {
                new ColumnType(DataType.Boolean, typeof(bool), DataType.Integer8, 1),
                new ColumnType(DataType.Integer8, typeof(bool), DataType.Integer16, 1),
                new ColumnType(DataType.Integer16, typeof(short), DataType.Integer32, 2),
                new ColumnType(DataType.Integer32, typeof(int), DataType.Integer64, 4),
                new ColumnType(DataType.Integer64, typeof(long), DataType.Float32, 4),
                new ColumnType(DataType.Float32, typeof(float), DataType.Float64, 8),
                new ColumnType(DataType.Float64, typeof(double), DataType.String, 1),
                new ColumnType(DataType.Time, typeof(TimeSpan), DataType.DateTime, 8),
                new ColumnType(DataType.Date, typeof(Date), DataType.DateTime, 8),
                new ColumnType(DataType.DateTime, typeof(DateTime), DataType.String, 8),
                new ColumnType(DataType.Guid, typeof(Guid), DataType.String, 16),
                new ColumnType(DataType.Json, typeof(JsonObject), DataType.String, 1),
                new ColumnType(DataType.String, typeof(string), DataType.Binary, 1),
                new ColumnType(DataType.Binary, typeof(byte[]), DataType.Binary, 1)
            };
        }
    }
}