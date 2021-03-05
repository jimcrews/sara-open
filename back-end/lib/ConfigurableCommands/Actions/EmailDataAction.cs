using Sara.Lib.ConfigurableCommands.Loaders;
using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sara.Lib.Extensions;
using System.Text.RegularExpressions;
using Sara.Lib.Logging;
using Sara.Lib.ConfigurableCommands.Actions;
using Sara.Lib.ConfigurableCommands;

namespace Sara.Lib.ConfigurableCommands.Actions
{
    public enum DataFormatType
    {
        CSV,
        JSON
    }

    /// <summary>
    /// Emails a SARA dataset. The action supports moustache style {...} values in most of the parameters
    /// to indicate a dynamic value from the dataset.
    /// </summary>
    public class EmailDataAction : AbstractAction
    {
        [ConfigurableProperty(Seq = 1, Name = "SARA_URL", Mandatory = true, Help = @"", Description = "The URL to the SARA dataset.")]
        public string SaraUrl { get; set; }

        [ConfigurableProperty(Seq = 2, Name = "SPLIT_COLUMN_NAME", Mandatory = false, Help = @"", Description = "Optional column name in the dataset, to split the data into groups. If groups are specified, a separate email will be constructed for each group with a subset of data.")]
        public string SplitColumnName { get; set; }

        [ConfigurableProperty(Seq = 3, Name = "RECIPIENTS", Mandatory = true, Help = @"", Description = "The recipient name(s). Can reference data on first row using {{row.<fieldname>}}.")]
        public string Recipients { get; set; }

        [ConfigurableProperty(Seq = 4, Name = "SUBJECT", Mandatory = true, Help = @"", Description = "The subject text for the email. Can reference data on first row using {{row.<fieldname>}}.")]
        public string Subject { get; set; }

        [ConfigurableProperty(Seq = 5, AllowMultiLine = true, Name = "BODY", Mandatory = true, Help = @"", Description = "The body text for the email. Can reference data on first row using {{row.<fieldname>}}.")]
        public string Body { get; set; }

        [ConfigurableProperty(Seq = 6, Name = "DATA_FORMAT", Mandatory = true, Help = @"", Description = "The format of the data file.", Default = DataFormatType.CSV)]
        public DataFormatType DataFormat { get; set; }

        [ConfigurableProperty(Seq = 7, Name = "FILE_NAME", Mandatory = true, Help = @"", Description = "The name of the file attachment containing the data. Can reference data on first row using {{row.<fieldname>}}.")]
        public string Filename { get; set; }

        public override void Execute()
        {
            Logger.Log(LogType.INFORMATION, $"Start of EmailData action.");

            var data = GetData();

            if (data.Any())
            {
                // Validations
                if (!string.IsNullOrEmpty(SplitColumnName) && !data.First().ContainsKey(SplitColumnName))
                    throw new Exception("Invalid SplitColumnName.");

                // Split data up into groups
                var groups = SplitData(data);

                int c = 0;
                foreach (var group in groups)
                {
                    c++;
                    Logger.Log(LogType.INFORMATION, $"Preparing email for group {c}.");

                    // Email each group separately.
                    MimeMessage message = new MimeMessage();
                    var builder = new BodyBuilder();
                    message.Subject = InterpolateString(Subject, group);

                    var body = InterpolateString(Body, group);
                    builder.TextBody = $@"{body}

--- Powered by SARA ---";

                    InterpolateString(this.Recipients, group).Split(',').ToList().ForEach(r =>
                    {
                        message.To.Add(new MailboxAddress(r, r));
                    });

                    Logger.Log(LogType.INFORMATION, $"Recipient(s): {message.To.ToString()}.");

                    message.From.Add(new MailboxAddress("Data Lake", "admin@datalake.com"));

                    // Data
                    Logger.Log(LogType.INFORMATION, $"Data rows in group: {group.Count()}.");
                    var groupSafe = RemoveRecipientValues(group);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8))
                        {
                            groupSafe.ToCsv(sw);
                        }
                        builder.Attachments.Add(InterpolateString(Filename, group), ms.ToArray(), new ContentType("text", "csv"));
                    }

                    message.Body = builder.ToMessageBody();

                    Logger.Log(LogType.INFORMATION, $"Sending email via SMTP.");

                    using (var client = new SmtpClient())
                    {
                        client.Connect("smtp.datalake.com.au");
                        client.Send(message);
                        client.Disconnect(true);
                    }
                }
            }
            Logger.Log(LogType.INFORMATION, $"Finished EmailData action.");
        }

        private IEnumerable<IDictionary<string, object>> GetData()
        {
            Logger.Log(LogType.INFORMATION, $"Getting data from SARA.");
            Logger.Log(LogType.INFORMATION, $"URL: {SaraUrl}.");

            // Get data
            SaraLoader loader = new SaraLoader();
            loader.Url = SaraUrl;
            var data = loader.Read();

            Logger.Log(LogType.INFORMATION, $"Data rows: {data.Count()}.");

            return data;
        }

        /// <summary>
        /// If the recipients property contains column name references, then
        /// these columns contain email addresses, and should be removed from
        /// the data actually emailed.
        /// </summary>
        /// <param name="data"></param>
        private IEnumerable<IDictionary<string, object>> RemoveRecipientValues(IEnumerable<IDictionary<string, object>> data)
        {
            var recipientColumnNames = GetColumnNameReferences(this.Recipients);
            if (recipientColumnNames.Any())
            {
                foreach (var row in data)
                {
                    foreach (var name in recipientColumnNames)
                    {
                        row.Remove(name);
                    }
                }
            }
            return data;
        }

        /// <summary>
        /// Parses a string input, and returns a list of column name references within the string.
        /// These are any labels wrapped within moustaches {...}.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string[] GetColumnNameReferences(string input)
        {
            List<string> recipientColumnNames = new List<string>();

            // Find {...} and replace with the contents of the first row of data, matching the column name within {...}.
            var moustacheReplace = new System.Text.RegularExpressions.Regex(@"\{(\w+)\}", System.Text.RegularExpressions.RegexOptions.Compiled);

            var matches = moustacheReplace.Matches(input);

            foreach (Match match in matches)
            {
                var name = match.Groups[1].Value;
                if (!recipientColumnNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                    recipientColumnNames.Add(name);
            }
            return recipientColumnNames.ToArray();
        }

        /// <summary>
        /// Interpolates a string, replacing variables denoted by {...}
        /// With the value from the first row of the dataset.
        /// </summary>
        /// <param name="str"></param>
        private string InterpolateString(string input, IEnumerable<IDictionary<string, object>> data)
        {
            var row = data.First();

            // Find {...} and replace with the contents of the first row of data, matching the column name within {...}.
            var moustacheReplace = new System.Text.RegularExpressions.Regex(@"\{(\w+)\}", System.Text.RegularExpressions.RegexOptions.Compiled);

            return moustacheReplace.Replace(input, match =>
            {
                var columnName = match.Groups[1].Value;
                return (string)row[columnName] ?? string.Empty;
            });
        }

        /// <summary>
        /// Splits a single dataset into groups based on the split column name.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="splitColumnName"></param>
        /// <returns></returns>
        private IEnumerable<IEnumerable<IDictionary<string, object>>> SplitData(IEnumerable<IDictionary<string, object>> data)
        {
            if (string.IsNullOrEmpty(SplitColumnName))
            {
                // No grouping
                Logger.Log(LogType.INFORMATION, $"Data not split into groups.");
                yield return data;
            }
            else
            {
                // Grouping
                var groups = data.GroupBy(
                    row => row[SplitColumnName],
                    row => row,
                    (key, rows) => new
                    {
                        Key = key,
                        Data = rows
                    });

                Logger.Log(LogType.INFORMATION, $"Data being split into {groups.Count()} groups.");

                foreach (var group in groups)
                {
                    yield return group.Data;
                }
            }
        }
    }
}
