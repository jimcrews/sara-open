using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;
using Sara.Lib.Extensions;
using Sara.Lib.Logging;

namespace Sara.Lib.Data
{
    /// <summary>
    /// A data reader over a collection of Dictionary objects.
    /// Note that the keys are case insensitive.
    /// </summary>
    public class DictionaryDataReader : IDataReader
    {
        IEnumerable<IDictionary<string, object>> data;
        IEnumerator enumerator;
        private bool isClosed = false;
        private Dictionary<string, Type> schema;
        Action<LogType, string> LogAction;
        long Row = 0;

        public DictionaryDataReader(IEnumerable<IDictionary<string, object>> data, Dictionary<string, Type> schema, Action<LogType, string> logAction)
        {
            this.schema = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in schema.Keys)
                this.schema.Add(key, schema[key]);

            this.data = data;
            enumerator = data.GetEnumerator();
            this.LogAction = logAction;
        }

        #region IDataReader Members

        public void Close()
        {
            IDisposable disposable = data as IDisposable;
            if (disposable != null)
                disposable.Dispose();

            isClosed = true;
        }

        public int Depth
        {
            get { return 0; }
        }

        public DataTable GetSchemaTable()
        {
            DataTable table = new DataTable("schema");
            table.Columns.Add("ColumnName", typeof(string));
            table.Columns.Add("ColumnOrdinal", typeof(int));
            table.Columns.Add("DataType", typeof(Type));

            // Get first row
            var keys = schema.Keys.ToList();
            for (int i = 0; i < keys.Count; i++)
            {
                table.Rows.Add(
                    keys[i],
                    i,
                    schema[keys[i]]);
            }
            return table;
        }

        public bool IsClosed
        {
            get { return isClosed; }
        }

        public bool NextResult()
        {
            // only support 1 result.
            return false;
        }

        public bool Read()
        {
            Row++;
            return enumerator.MoveNext();
        }

        public int RecordsAffected
        {
            get { return -1; }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion

        #region IDataRecord Members

        public int FieldCount
        {
            get { return schema.Keys.Count(); }
        }

        public bool GetBoolean(int i)
        {
            return (bool)GetValue(i);
        }

        public byte GetByte(int i)
        {
            return (byte)GetValue(i);
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            return (char)GetValue(i);
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i)
        {
            var key = schema.Keys.ToList()[i];
            return schema[key].Name;
        }

        public DateTime GetDateTime(int i)
        {
            return (DateTime)GetValue(i);
        }

        public decimal GetDecimal(int i)
        {
            return (Decimal)GetValue(i);
        }

        public double GetDouble(int i)
        {
            return (Double)GetValue(i);
        }

        public Type GetFieldType(int i)
        {
            var key = schema.Keys.ToList()[i];
            return schema[key];
        }

        public float GetFloat(int i)
        {
            return (float)GetValue(i);
        }

        public Guid GetGuid(int i)
        {
            return (Guid)GetValue(i);
        }

        public short GetInt16(int i)
        {
            return (Int16)GetValue(i);
        }

        public int GetInt32(int i)
        {
            return (Int32)GetValue(i);
        }

        public long GetInt64(int i)
        {
            return (Int64)GetValue(i);
        }

        public string GetName(int i)
        {
            return schema.Keys.ToList()[i];
        }

        public int GetOrdinal(string name)
        {
            return schema.Keys.ToList().IndexOf(name);
        }

        public string GetString(int i)
        {
            return (string)GetValue(i);
        }

        /// <summary>
        /// Main method to get the value from the Dictionary collection.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public object GetValue(int i)
        {
            if (Row%10000==0 && i==0 && LogAction!=null)
                LogAction(LogType.PROGRESS, string.Format("Bulk loading in progress: {0} rows.", Row));

            var key = schema.Keys.ToList()[i];
            var current = (IDictionary<string, object>)enumerator.Current;
            var currentInsensitive = new Dictionary<string, object>(current, StringComparer.OrdinalIgnoreCase);
            if (currentInsensitive.ContainsKey(key))
            {
                // Generally, the C# dot net type maps straight to the SQL type.
                // however, SQL does not know about our custom 'Date' type,
                // so we convert to DateTime.
                if (currentInsensitive[key]!=null && currentInsensitive[key].GetType()==typeof(Date))
                    return ((Date)currentInsensitive[key]).DateTime;
                else
                    return currentInsensitive[key];
            }
                
            else
                return DBNull.Value;
        }

        public int GetValues(object[] values)
        {
            int i = 0;
            foreach (var field in schema.Keys)
            {
                values[i++] = ((IDictionary<string, object>)enumerator.Current)[field];
            }
            return schema.Keys.Count();
        }

        public bool IsDBNull(int i)
        {
            return GetValue(i) == null || GetValue(i) == DBNull.Value;
        }

        public object this[string name]
        {
            get { return GetValue(GetOrdinal(name)); }
        }

        public object this[int i]
        {
            get { return GetValue(i); }
        }

        #endregion
    }
}