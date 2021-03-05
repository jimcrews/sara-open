using Sara.Lib.ConfigurableCommands.Actions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Models.Action
{
    /// <summary>
    /// An queue entry for processing by an action.
    /// </summary>
    public class ActionQueueInfo
    {
        public int ActionQueueId { get; set; }
        public DateTime DueDt { get; set; }
        public DateTime? BeginDt { get; set; }
        public DateTime? EndDt { get; set; }
        public string Action { get; set; }
        /// <summary>
        /// Parameters encoded in a json string.
        /// </summary>
        public string Parameters { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ActionQueueStatus Status { get; set; }
        public string Exception { get; set; }
    }
}
