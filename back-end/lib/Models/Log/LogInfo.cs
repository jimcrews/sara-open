using Sara.Lib.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Models.Log
{
     /// <summary>
    /// Represents log information for a unit of work.
    /// </summary>
    public class LogInfo
    {
        /// <summary>
        /// Unique key for log.
        /// </summary>
        public int LogId { get; set; }

        /// <summary>
        /// The executable source.
        /// </summary>
        public string Executable { get; set; }

        /// <summary>
        /// The class source.
        /// </summary>
        public string Class { get; set; }

        /// <summary>
        /// The optional key.
        /// </summary>
        public string ItemKey { get; set; }

        /// <summary>
        /// For audit header entries with an item key, this field
        /// returns the class-specific description.
        /// </summary>
        public string ItemDescription { get; set; }

        /// <summary>
        /// Start date/time of the extraction.
        /// </summary>
        public DateTime StartDt { get; set; }

        /// <summary>
        /// End date/time of the extaction.
        /// </summary>
        public DateTime EndDt { get; set; }
        
        /// <summary>
        /// Duration of the extraction.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Flag indicating overall success of the task.
        /// </summary>
        public int? SuccessFlag { get; set; }
        /// <summary>
        /// Log status of the extraction.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public LogType LogStatus { get; set; }
        
        /// <summary>
        /// Rows affected by the extraction.
        /// </summary>
        public int? RowsAffected { get; set; }
    }
}
