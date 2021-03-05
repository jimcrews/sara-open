using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Cron
{
    /// <summary>
    /// Represents a repeat syntax, like 1/5, meaning
    /// repeat every 5 units, starting at 1.
    /// </summary>
    public class CronRepeat : ICronNode
    {
        public int Start;
        public int Repeat;
        public CronRepeat(int start, int repeat)
        {
            this.Start = start;
            this.Repeat = repeat;
        }
    }
}