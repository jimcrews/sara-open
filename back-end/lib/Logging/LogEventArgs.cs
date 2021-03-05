using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sara.Lib.Logging
{
    public class LogEventArgs : EventArgs
    {
        public MethodBase Caller { get; set; }
        public LogType LogType { get; set; }
        public string Message { get; set; }
        public IDictionary<string, object> State { get; set; }
        public Exception Exception { get; set; }

    }
}