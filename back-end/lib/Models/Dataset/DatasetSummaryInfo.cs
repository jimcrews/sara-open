using Sara.Lib.Models.Dataset;
using Sara.Lib.Models.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sara.Lib.Models.Dataset
{
    /// <summary>
    /// Summary information for a data. Used for the summary page presented
    /// to users. Includes core metadata used when publishing a dataset
    /// together with some additional server information and metrics.
    /// </summary>
    public class DatasetSummaryInfo : DatasetInfo
    {
        #region Server Properties

        public string ObjectType { get; set; }
        public long RowCount { get; set; }
        public DateTime CreatedDt { get; set; }
        public DateTime ModifiedDt { get; set; }
        public DateTime UpdatedDt { get; set; }
        public string Definition { get; set; }

        #endregion

        #region Derived Properties

        public IEnumerable<ServerColumnInfo> Columns { get; set; }
        public IEnumerable<ServerParameterInfo> Parameters { get; set; }
        public int? Ranking { get; set; }
        public string[] Top3Users { get; set; }
        public int? DownloadSecs { get; set; }

        #endregion
    }
}
