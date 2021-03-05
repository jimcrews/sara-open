using Sara.Lib.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Models.Log
{
    /// <summary>
    /// Individual log event during an extraction.
    /// </summary>
    public class LogItemInfo
    {
        /// <summary>
        /// Unique key for log item.
        /// </summary>
        public int LogItemId { get; set; }
        /// <summary>
        /// Parent log id reference.
        /// </summary>
        public int LogId { get; set; }
        /// <summary>
        /// Date/time of log event.
        /// </summary>
        public DateTime LogDt { get; set; }
        /// <summary>
        /// Name of executable generating the log entry.
        /// </summary>
        public string Executable { get; set; }
        /// <summary>
        /// Name of class generating the log entry.
        /// </summary>
        public string Class { get; set; }
        /// <summary>
        /// Name of method generating the log entry.
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// Log type of the log entry.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public LogType LogType { get; set; }
        /// <summary>
        /// Message generated from the log event.
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Rows affected by the event.
        /// </summary>
        public int? RowsAffected { get; set; }
    }
}
