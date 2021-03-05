using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sara.Lib.Models.Dataset
{
    /// <summary>
    /// Metadata for a published dataset.
    /// </summary>
    public class DatasetInfo
    {
        /// <summary>
        /// Primary / internal key of dataset
        /// </summary>
        public int? DatasetId { get; set; }

        /// <summary>
        /// Category of the dataset. Defines security access.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Name of the dataset.
        /// </summary>
        public string Dataset { get; set; }

        /// <summary>
        /// Description of the dataset.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Server that dataset resides in.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Location / source name of dataset.
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// Display group within the category.
        /// </summary>
        public string DisplayGroup { get; set; }

        /// <summary>
        /// For writeable datasets, Writeable is set to true.
        /// </summary>
        public bool Writeable { get; set; }

        /// <summary>
        /// Set to true if dataset is 'live'. If false, dataset no longer accessible.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Set to true for internal objects. These are only visible to administrators.
        /// Internal objects cannot be used by end users. Internal objects are useful
        /// for adding metadata like documentation which flows through to parent
        /// objects that are public.
        /// </summary>
        public bool Internal { get; set; }

        /// <summary>
        /// Set to id of dataset that replaces this dataset. If set, dataset will
        /// be displayed as 'deprecated'.
        /// </summary>
        public int? ForwardDatasetId { get; set; }

        /// <summary>
        /// The owner of the dataset.
        /// </summary>
        public string Owner { get; set; }
    }
}
