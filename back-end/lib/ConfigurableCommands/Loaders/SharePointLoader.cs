using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using Microsoft.CSharp;
using Sara.Lib.Models;
using Sara.Lib.ConfigurableCommands;
using Sara.Lib.Logging;
using Sara.Lib.ConfigurableCommands.Loaders;
using Sara.Lib.Extensions;

namespace Sara.Lib.ConfigurableCommands.Loaders
{
    public class SharePointLoader : AbstractLoader
    {
        [ConfigurableProperty(Seq = 1, Name = "SITE_URL", Mandatory = true, Help = @"e.g. http://example.com", Description = "The url to the SharePoint list site.")]
        public string SiteUrl { get; set; }

        [ConfigurableProperty(Seq = 2, Name = "LIST_NAME", Mandatory = true, Help = @"e.g. MyList", Description = "The name of the SharePoint list in the site.")]
        public string ListName { get; set; }

        public override IEnumerable<DataColumn> Probe()
        {
            var rows = Read();
            return rows.ProbeColumns();

            /*
            string url = Uri.EscapeUriString(string.Format(
                @"{0}/_api/web/lists/GetByTitle('{1}')/fields",
                SiteUrl,
                ListName));

            var results = GetSharePointJson(url);

            foreach (var row in results)
            {
                var r = row.ToObject<Hashtable>();
                if ((bool)r["Hidden"] == false)
                {
                    var t = r["TypeAsString"].ToString();
                    yield return new ProbeResult
                    {
                        Column = UseColumnInternalNames ? r["EntityPropertyName"].ToString() : r["DisplayName"].ToString(),
                        PrimaryKey = false,
                        DataType = DataType.STRING,
                        DataLength = 50
                    };
                }
            }
            */
        }

        public override IEnumerable<IDictionary<string, object>> Read(int? maxDataRows=null)
        {
            string url = Uri.EscapeUriString(string.Format(
                @"{0}/_api/web/lists/GetByTitle('{1}')/items?$top=99999",
                SiteUrl,
                ListName));

            var results = GetSharePointJson(url);

            foreach (var row in results)
            {
                Dictionary<string, object> result = new Dictionary<string, object>();
                Hashtable r = row.ToObject<Hashtable>();
                foreach (string key in r.Keys)
                {
                    result[key] = r[key];
                }
                yield return result;
            }

        }

        private dynamic GetSharePointJson(string url)
        {
            WebRequest request = WebRequest.Create(url);
            ((HttpWebRequest)request).Accept = "application/json;odata=verbose";
            request.Credentials = CredentialCache.DefaultCredentials;
            var response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            var text = reader.ReadToEnd();
            dynamic obj = JObject.Parse(text);
            return obj.d.results;
        }
    }
}
