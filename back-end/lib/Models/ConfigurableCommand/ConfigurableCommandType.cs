using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Models.ConfigurableCommand
{
    /// <summary>
    /// The type of configurable command.
    /// </summary>
    public enum ConfigurableCommandType
    {
        /// <summary>
        /// Type of configurable command that loads data into SARA.
        /// </summary>
        Loader,

        /// <summary>
        /// Type of configurable command that performs some kind of action in SARA.
        /// </summary>
        Action
    }
}