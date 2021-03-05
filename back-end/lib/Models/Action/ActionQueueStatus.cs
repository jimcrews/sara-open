using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Models.Action
{
    public enum ActionQueueStatus
    {
        Ready,
        Running,
        Success,
        Failure,
        Aborted
    }
}
