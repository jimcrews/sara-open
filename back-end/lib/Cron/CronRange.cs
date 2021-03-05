using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Cron
{
    /// <summary>
    /// Represents a range of numbers.
    /// </summary>
    public class CronRange : ICronNode
    {
        public int Start;
        public int End;

        public CronRange(int start, int end)
        {
            this.Start = start;
            this.End = end;
        }
    }
}