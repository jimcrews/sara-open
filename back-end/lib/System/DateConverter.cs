using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace System
{
    /// <summary>
    /// Converter class to convert from strings / DateTimes to Date object
    /// Note that the DateTime / String must have no time component. If
    /// There is a time component, the conversion fails.
    /// </summary>
    public class DateConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return
                sourceType == typeof(string) ||
                sourceType == typeof(DateTime) ||
                base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var casted = value as string;
            return casted != null
                ? Date.Parse(casted)
                : base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            var casted = value as Date?;
            return destinationType == typeof(string) && casted != null
                ? casted.ToString()
                : base.ConvertTo(context, culture, value, destinationType);
        }
    }
}