using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Sara.Lib.Csv
{
    /// <summary>
    /// Parses a delimited / csv file.
    /// </summary>
    public class CsvParser
    {
        public Stream Stream { get; set; }
        public char FieldTerminator { get; set; }
        public char? TextTerminator { get; set; }
        public int FirstDataRow { get; set; }
        public bool FirstRowHeaderFlag { get; set; }
        public bool AllowRaggedFlag { get; set; }
        public bool NullsFlag { get; set; }
        public bool CaseInsensitiveHeaders { get; set; }

        public delegate void InvalidRowHandler(object source, InvalidRowEventArgs args);

        public event InvalidRowHandler InvalidRow;

        protected virtual void OnInvalidRow(InvalidRowEventArgs args)
        {
            if (InvalidRow != null)
                InvalidRow(this, args);
        }

        public CsvParser(Stream stream, char fieldTerminator = ',', char? textTerminator = '"', int firstDataRow = 2, bool firstRowHeaderFlag = true, bool nullsFlag = true, bool caseInsensitiveHeaders = true)
        {
            this.Stream = stream;
            this.FieldTerminator = fieldTerminator;
            this.TextTerminator = textTerminator;
            this.FirstDataRow = firstDataRow;
            this.FirstRowHeaderFlag = firstRowHeaderFlag;
            this.NullsFlag = nullsFlag;
            this.CaseInsensitiveHeaders = caseInsensitiveHeaders;
        }

        public IEnumerable<IDictionary<string, object>> Parse()
        {
            StreamReader sr = null;
            int i = 0;
            string[] headers = null;

            try
            {
                sr = new StreamReader(Stream);

                while (!sr.EndOfStream)
                {
                    var values = ReadRow(sr);
                    i++;

                    if (i == 1)
                    {
                        headers = GetHeaders(values);
                    }
                    if (i >= FirstDataRow)
                    {
                        if (values.Length == headers.Length || AllowRaggedFlag && values.Length < headers.Length)
                        {
                            // Create hash table with insensitive / sensitive headers / keys
                            var row = new Dictionary<string, object>(CaseInsensitiveHeaders ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture);

                            for (int r = 0; r < values.Length; r++)
                            {
                                var columnName = headers[r];
                                row[columnName] = values[r];
                                if (NullsFlag && ((string)row[columnName] == string.Empty))
                                    row[columnName] = null;
                            }
                            for (int r = values.Length + 1; r < headers.Length; r++)
                            {
                                // ragged row containing less colums than header
                                var columnName = headers[r];
                                row[columnName] = null;
                            }
                            yield return row;
                        }
                        else
                        {
                            string raw = string.Join("|", values);
                            OnInvalidRow(new InvalidRowEventArgs { RowNumber = i, RawData = raw });
                        }
                    }
                }
            }
            finally
            {
                sr.Close();
            }
        }

        #region Private Methods

        private string[] ReadRow(StreamReader sr, string previousLine)
        {
            var line = string.IsNullOrEmpty(previousLine) ? sr.ReadLine() : (previousLine + Environment.NewLine + sr.ReadLine());
            StringParser sp = new StringParser(line);
            List<string> row = new List<string>();
            bool inCell = false;
            string cell = string.Empty;
            bool closingTextTerminatorFound = false;    // used to determine whether to trim values.

            while (!sp.EndOfString)
            {
                if (inCell && sp.Match(TextTerminator))
                {
                    // match another text delimiter immediately after previous text delimiter, i.e. "" - treat as escaped text delimiter.
                    if (sp.Match(TextTerminator))
                    {
                        // another text delimiter means treat as text in value
                        cell += TextTerminator;
                    }
                    else
                    {
                        // closing text delimiter
                        inCell = false;
                        closingTextTerminatorFound = true;
                    }
                }
                else if (!inCell && sp.Match(TextTerminator))
                {
                    // text delimiter denoting start of value
                    inCell = true;
                }
                else if (!inCell && sp.Match(FieldTerminator))
                {
                    // field terminator - add to row. If the cell was not previously closed by text delimiter
                    // then trim it.
                    if (!closingTextTerminatorFound)
                        cell = cell.Trim();
                    closingTextTerminatorFound = false;
                    row.Add(cell);
                    cell = string.Empty;
                }
                else if (!inCell && !string.IsNullOrEmpty(cell) && sp.Match(TextTerminator))
                    // fail if try to include a text delimiter when already data in cell.
                    throw new Exception("Cannot delimit text as column already contains data.");
                else if (sp.IsWhiteSpaceChar() && string.IsNullOrEmpty(cell))
                    // ignore white space at start of cell.
                    sp.Read();
                else
                {
                    cell += sp.Read();
                }
            }
            // add the final cell
            if (!closingTextTerminatorFound)
                cell = cell.Trim();
            closingTextTerminatorFound = false;
            row.Add(cell);

            // At the end of the string, the inCell should be FALSE. However, if a field spans multiple lines, a " character should escape the text
            // so inCell will equal TRUE. in these cases, return the value array of the current row + the next row
            if (inCell)
                return ReadRow(sr, line).ToArray();
            else
                return row.ToArray();
        }

        private string[] ReadRow(StreamReader sr)
        {
            return ReadRow(sr, null);
        }

        /// <summary>
        /// Returns the header names from row 1 (if headers exists), otherwise returns generic 'Column1,Column2...' names
        /// </summary>
        /// <param name="data"></param>
        /// <param name="headerRows"></param>
        /// <returns></returns>
        private string[] GetHeaders(string[] values)
        {
            int col = 1;
            List<string> cols = new List<string>();
            if (!FirstRowHeaderFlag)
            {
                foreach (var c in values)
                    cols.Add(string.Format("Column{0}", col++));
            }
            else
            {
                foreach (var c in values)
                    cols.Add(c);
            }

            List<string> ret = new List<string>();
            foreach (var column in cols)
                ret.Add(column.Trim());

            return ret.ToArray();
        }

        #endregion
    }
}