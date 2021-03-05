using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sara.Lib.ConfigurableCommands.Loaders
{
    /// <summary>
    /// Denotes how rows are loaded into the target table.
    /// </summary>
    public enum RowProcessingBehaviour
    {
        /// <summary>
        /// No processing done on target table.
        /// </summary>
        NONE,

        /// <summary>
        /// Rows inserted into target table
        /// </summary>
        INSERT,

        /// <summary>
        /// Like INSERT, except an additional _SYS_UPDATED_DT column
        /// added. Used for sources where the entire dataset is
        /// captured at regular intervals.
        /// </summary>
        SNAPSHOT,

        /// <summary>
        /// Inserts and updates applied to the target table. An
        /// additional _SYS_UPDATED_DT column is added.
        /// </summary>
        UPSERT,

        /// <summary>
        /// Like UPSERT, however in addition, deletes are performed
        /// too. An additional _SYS_UPDATED_DT column is added.
        /// </summary>
        MERGE,

        /// <summary>
        /// Similar to merge, but includes temporal transaction valid dates.
        /// _SYS_EFFECTIVE_DT and _SYS_EXPIRY_DT columns added. Enables
        /// full historised view of data.
        /// </summary>
        TEMPORAL
    }
}