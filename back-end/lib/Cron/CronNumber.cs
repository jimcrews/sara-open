using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Cron
{
    /// <summary>
    /// Represents a single number
    /// </summary>
    public class CronNumber : ICronNode
    {
        public int Number;
        public CronNumber(int number)
        {
            this.Number = number;
        }
    }
}