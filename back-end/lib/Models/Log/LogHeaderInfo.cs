using Sara.Lib.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Models.Log
{
     /// <summary>
    /// Represents log header record.
    /// </summary>
    public class LogHeaderInfo
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
        /// Flag indicating overall success of the task.
        /// </summary>
        public int? SuccessFlag { get; set; }

        /// <summary>
        /// Flag indicating whether task associated with this log id has been cancelled by the user.
        /// </summary>
        public int? CancelledFlag { get; set; }
    }
}
