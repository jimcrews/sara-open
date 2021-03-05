using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sara.Lib.Models.Server
{
    /// <summary>
    /// Represents a single object dependency relationship.
    /// </summary>
    public class DatasetDependencyInfo
    {
        /// <summary>
        /// Unique identifier for the relationship.
        /// </summary>
        public string SurrogateKey { get; set; }
        /// <summary>
        /// Related object's surrogate key.
        /// </summary>
        public string RelationSurrogateKey { get; set; }
        /// <summary>
        /// Object name.
        /// </summary>
        public string ObjectName { get; set; }
        /// <summary>
        /// Object type.
        /// </summary>
        public string ObjectType { get; set; }
        /// <summary>
        /// Node type.
        /// </summary>
        public string NodeType { get; set; }
        /// <summary>
        /// Date/time that object was last updated.
        /// </summary>
        public DateTime? LastUpdatedDt { get; set; }
        /// <summary>
        /// Number of rows in object.
        /// </summary>
        public int? RowCount { get; set; }
    }
}
