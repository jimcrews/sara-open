using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Models.Notebook
{
    /// <summary>
    /// Represents an interactive Sara notebook.
    /// </summary>
    public class NotebookInfo
    {
        public string Category { get; set; }
        public string Notebook { get; set; }
        public string Script { get; set; }
    }
}
