using Sara.Lib.Models.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Sara.Lib.Extensions;

namespace Sara.Lib.ConfigurableCommands.Loaders
{
    /// <summary>
    /// Loads data from a Sara dataset.
    /// </summary>
    public class SaraLoader : AbstractLoader
    {
        [ConfigurableProperty(Seq = 1, Name = "URL", Mandatory = true, Help = @"", Description = "")]
        public string Url { get; set; }

        public override IEnumerable<IDictionary<string, object>> Read(int? maxDataRows = null)
        {
            // Get data from SARA Url
            WebRequest request = WebRequest.Create(Url);
            request.Timeout = 1000 * 60 * 60;   // 1 hour
            request.UseDefaultCredentials = true;
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            var json = reader.ReadToEnd();
            var data = JsonConvert.DeserializeObject<IEnumerable<IDictionary<string, object>>>(json);
            return data;
        }

        private IEnumerable<ServerColumnInfo> GetSchemaColumns()
        {
            // We use the SARA URL to get the Schema URL
            var schemaUrl = Url.Replace("", "");

            // Get data from SARA Url
            WebRequest request = WebRequest.Create(schemaUrl);
            request.Timeout = 1000 * 60 * 60;   // 1 hour
            request.UseDefaultCredentials = true;
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            var json = reader.ReadToEnd();
            var data = JsonConvert.DeserializeObject<IEnumerable<ServerColumnInfo>>(json);
            return data;
        }

        public override IEnumerable<DataColumn> Probe()
        {
            int MAX_ROWS = 1;
            var rows = this.Read(MAX_ROWS);

            if (!rows.Any())
            {
                throw new Exception("File source is empty cannot determine column metadata");
            }
            else
            {
                var schemaMode = true;

                if (schemaMode)
                {
                    // Fast version - use API Schema
                    return GetSchemaColumns().Select(c => (DataColumn)c);
                }
                else
                {
                    // Slow / buggy option - brute force - treats fully null columns as strings TO DO
                    return rows.ProbeColumns();
                }
            }
        }
    }
}
