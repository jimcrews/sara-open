using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sara.Lib.Models.Dataset
{
    /// <summary>
    /// Represents an audit entry for a dataset.
    /// </summary>
    public class DatasetAuditInfo
    {
        /// <summary>
        /// The unique audit id of the entry.
        /// </summary>
        public int AuditId { get; set; }
        /// <summary>
        /// The date/time of the audit entry.
        /// </summary>
        public DateTime AuditDt { get; set; }
        /// <summary>
        /// The user causing the audit entry.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// The controller class accessed.
        /// </summary>
        public string Controller { get; set; }
        /// <summary>
        /// The action method executed.
        /// </summary>
        public string Action { get; set; }
        /// <summary>
        /// The duration of the event.
        /// </summary>
        public TimeSpan Duration { get; set; }
        /// <summary>
        /// The duration of the event in seconds.
        /// </summary>
        public int DurationSecs { get; set; }
        /// <summary>
        /// The category involved in the event (if applicable).
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// The dataset involved in the event (if applicable).
        /// </summary>
        public string Dataset { get; set; }
    }
}