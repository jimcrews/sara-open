using Sara.Lib.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sara.Lib.Logging
{
    public abstract class Logger : MarshalByRefObject, ILogger
    {

        public void Log(Exception exception, IDictionary<string, object> state = null)
        {
            StackTrace trc = new StackTrace();

            LogEventArgs args = new LogEventArgs()
            {
                Caller = trc.GetFrame(1).GetMethod(),
                Message = exception.Message,
                Exception = exception,
                LogType = LogType.ERROR,
                State = state != null ? state.ToDictionary() : null
            };

            Log(args);
        }

        public void Log(LogType logType, string message, IDictionary<string, object> state = null)
        {
            StackTrace trc = new StackTrace();
            LogEventArgs args = new LogEventArgs()
            {
                Caller = trc.GetFrame(1).GetMethod(),
                LogType = logType,
                Message = message,
                State = state != null ? state.ToDictionary() : null
            };
            Log(args);

        }

        public abstract void Log(LogEventArgs args);
    }
}