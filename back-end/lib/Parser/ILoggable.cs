using System;

namespace Sara.Lib.Parser
{
    public interface ILoggable
    {
        Action<object, LogArgs> LogHandler { get; set; }
    }
}