using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Csv
{
    /// <summary>
    /// Information about invalid file rows.
    /// </summary>
    public class InvalidRowEventArgs : EventArgs
    {
        public int RowNumber { get; set; }
        public string RawData { get; set; }
    }
}