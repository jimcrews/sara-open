using Sara.Lib.Models;
using Sara.Lib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Sara.Lib.Logging;

namespace Sara.Lib.ConfigurableCommands.Actions
{
    public abstract class AbstractAction : AbstractConfigurableCommand
    {
        public abstract void Execute();
    }
}
