namespace Sara.Lib.Logging
{
    public class NullLogger : Logger
    {
        public override void Log(LogEventArgs args)
        {
            // do nothing
        }
    }
}