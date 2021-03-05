using Sara.Lib.Extensions;
using Sara.Lib.Logging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Sara.Lib.ConfigurableCommands.Loaders
{
    /// <summary>
    /// A loader that does nothing.
    /// </summary>
    public class NullLoader : AbstractLoader
    {
        /// <summary>
        /// Executes a query to probe the structure of the results.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<DataColumn> Probe()
        {
            throw new Exception("Not Supported");
        }

        public override IEnumerable<IDictionary<string, object>> Read(int? maxDataRows = null)
        {
            throw new Exception("Not Supported");
        }
    }
}
