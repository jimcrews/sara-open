using Sara.Lib.ConfigurableCommands;
using Sara.Lib.ConfigurableCommands.Actions;
using Sara.Lib.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Sara.Lib.ConfigurableCommands.Actions
{
    /// <summary>
    /// Loads data into SARA through the Theobald tool.
    /// This is not a loader, but an action. The theobald tool
    /// manages all the loading into the SARA MSSQL database.
    /// </summary>
    public class TheobaldLoaderAction : AbstractAction
    {
        [ConfigurableProperty(Seq = 1, Name = "BASE_URL", Mandatory = true, Help = @"http://localhost:8065/", Default = "http://localhost:8065/", Description = "The base url for the HTTP endpoint.")]
        public string BaseUrl { get; set; }
        [ConfigurableProperty(Seq = 2, Name = "EXTRACT_RULES", Mandatory = true, Help = @"MARA,VBAK", Description = "A comma-separated list of extract rules. Each will be executed serially. Each rule is the name of the rule in Xtract Universal.")]
        public string ExtractRules { get; set; }

        public override void Execute()
        {
            var rules = ExtractRules.Split(',').Select(r => r.Trim());
            foreach (var rule in rules)
            {
                try
                {
                    var url = $"{BaseUrl}?name={rule}";
                    Logger.Log(LogType.INFORMATION, $"Starting Extract for: {rule}");

                    // Execute the Theobald rule via the HTTP endpoint
                    WebRequest request = WebRequest.Create(url);
                    request.Timeout = 1000 * 60 * 60;   // 1 hour
                    WebResponse response = request.GetResponse();
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        Logger.Log(LogType.INFORMATION, line);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.ERROR, ex.Message);
                } finally
                {
                    Logger.Log(LogType.INFORMATION, $"End of Extract for: {rule}");
                }
            }
        }
    }
}
