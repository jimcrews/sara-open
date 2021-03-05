using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Logging
{
    public interface IServiceLogger : ILogger
    {
        public int LogId { get; }

        /// <summary>
        /// Creates a header log record.
        /// </summary>
        /// <param name="referenceId"></param>
        /// <returns></returns>
        void LogHeader(string executableName, string className, string itemKey = null);

        /// <summary>
        /// Logs the status:
        /// true. Success
        /// false. Failure
        /// null: in progress
        /// </summary>
        /// <param name="logId"></param>
        /// <param name="successFlag"></param>
        void LogHeaderSuccess(bool? successFlag);
    }
}
