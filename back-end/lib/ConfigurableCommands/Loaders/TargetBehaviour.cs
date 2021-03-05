using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sara.Lib.ConfigurableCommands.Loaders
{
    public enum TargetBehaviour
    {
        // Table is dropped if exists and recreated
        DROP,

        // Data is truncated if table exists
        TRUNCATE,

        // Table is create if not exists
        CREATE,

        // No action - table must be created already.
        NONE
    }
}