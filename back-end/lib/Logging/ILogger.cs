using System;
using System.Collections.Generic;

namespace Sara.Lib.Logging
{
    public interface ILogger
    {
        void Log(LogEventArgs args);
        void Log(LogType logType, string message, IDictionary<string, object> state = null);
        void Log(Exception exception, IDictionary<string, object> state = null);
    }
}