using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FastShapefile.Dbf
{
    public class DbfRecord
    {
        #region - Fields -

        private static readonly Encoding _encod = Encoding.ASCII;
        private byte[] _contents;
        private DbfHeader _header;

        #endregion

        #region - Ctor -

        public DbfRecord(DbfHeader header)
        {
            _contents = new byte[header.RecordLength];
            _contents[0] = (byte)' ';

            _header = header;
        }

        #endregion

        #region - Properties -

        public byte[] RawContents
        {
            get { return _contents; }
        }

        #endregion

        #region - Methods -

        #region - Get Methods -

        public string[] GetAll()
        {
            string[] vals = new string[_header.Count];

            for (int i = 0; i < vals.Length; i++)
                vals[i] = GetString(i);

            return vals;
        }

        public object GetValue(int index)
        {
            DbfColumn col = _header[index];

            switch (col.Type)
            {
                case DbfColumnType.Boolean:
                    return GetBoolean(index);
                case DbfColumnType.Character:
                    return GetString(index);
                case DbfColumnType.Date:
                    return GetDate(index);
                case DbfColumnType.Float:
                case DbfColumnType.Number:
                    return GetNumber(index);
            }

            throw new Exception(String.Format("Error getting value for column type '{0}'", (char)col.Type));
        }

        public string GetString(string name)
        {
            return GetString(_header.GetOrdinal(name));
        }

        public string GetString(int index)
        {
            DbfColumn col = _header[index];

            return _encod.GetString(_contents, col.DataAddress, col.Length);
        }

        public DateTime GetDate(int index)
        {
            DbfColumn col = _header[index];

            if (col.Type == DbfColumnType.Date)
            {
                string dateStr = _encod.GetString(_contents, col.DataAddress, col.Length);

                int year = int.Parse(dateStr.Substring(0, 4));
                int month = int.Parse(dateStr.Substring(4, 2));
                int day = int.Parse(dateStr.Substring(6, 2));

                return new DateTime(year, month, day);
            }
            else
                throw new Exception(col.Name + " column is not a date column");
        }

        public bool? GetBoolean(int index)
        {
            DbfColumn col = _header[index];

            if (col.Type == DbfColumnType.Boolean)
            {
                if ((new char[] { 'Y', 'y', 'T', 't' }).Contains((char)_contents[col.DataAddress]))
                    return true;
                if ((new char[] { 'N', 'n', 'F', 'f' }).Contains((char)_contents[col.DataAddress]))
                    return false;
                return null;
            }
            else
                throw new Exception(col.Name + " column is not a boolean column");
        }

        public double GetNumber(string name)
        {
            return GetNumber(_header.GetOrdinal(name));
        }

        public double GetNumber(int index)
        {
            DbfColumn col = _header[index];

            if (col.Type == DbfColumnType.Number || col.Type == DbfColumnType.Float)
                return double.Parse(_encod.GetString(_contents, col.DataAddress, col.Length));
            else
                throw new Exception(col.Name + " column is not a number column");
        }

        #endregion

        #region - Set Methods -

        public void SetRawRecord(byte[] rawContents)
        {
            if (rawContents.Length != _contents.Length)
                throw new Exception("Record length does not match");

            Buffer.BlockCopy(rawContents, 0, _contents, 0, _contents.Length);
        }

        public void SetRaw(int index, string value)
        {
            DbfColumn col = _header[index];

            _encod.GetBytes((value ?? "").PadRight(col.Length, ' '), 0, col.Length, _contents, col.DataAddress);
        }

        public void SetRaw(int index, byte[] value)
        {
            DbfColumn col = _header[index];

            if (value == null)
                return;

            if (value.Length < col.Length)
                Buffer.BlockCopy(value, 0, _contents, col.DataAddress, value.Length);
            else
                Buffer.BlockCopy(value, 0, _contents, col.DataAddress, col.Length);
        }

        public void SetRaw(int index, object value)
        {
            DbfColumn col = _header[index];

            switch (col.Type)
            {
                case DbfColumnType.Character:
                    Set(index, value == null ? "" : Convert.ToString(value));
                    break;
                case DbfColumnType.Number:
                    Set(index, value == null ? 0d : Convert.ToDouble(value));
                    break;
                case DbfColumnType.Boolean:
                    Set(index, value == null ? (bool?)null : Convert.ToBoolean(value));
                    break;
                case DbfColumnType.Date:
                    Set(index, value == null ? new DateTime(0, 0, 0) : Convert.ToDateTime(value));
                    break;
            }
        }

        public void Set(int index, string value)
        {
            DbfColumn col = _header[index];

            if (col.Type == DbfColumnType.Character)
                _encod.GetBytes((value ?? "").PadRight(col.Length, ' '), 0, col.Length, _contents, col.DataAddress);
            else
                throw new Exception(col.Name + " column is not a character column");
        }

        public void Set(int index, double value)
        {
            DbfColumn col = _header[index];

            if (col.Type == DbfColumnType.Number)
            {
                string strVal = value.ToString(col.DecimalFormat).PadLeft(col.Length, '0');

                _encod.GetBytes(strVal, 0, strVal.Length, _contents, col.DataAddress);
            }
            else
                throw new Exception(col.Name + " column is not a number column");
        }

        public void Set(int index, bool? value)
        {
            DbfColumn col = _header[index];

            if (col.Type == DbfColumnType.Boolean)
            {
                if (value.HasValue)
                {
                    if (value.Value)
                        _contents[col.DataAddress] = (byte)'T';
                    else
                        _contents[col.DataAddress] = (byte)'F';
                }
                else
                    _contents[col.DataAddress] = (byte)'?';
            }
            else
                throw new Exception(col.Name + " column is not a boolean column");
        }

        public void Set(int index, DateTime value)
        {
            DbfColumn col = _header[index];

            if (col.Type == DbfColumnType.Date)
                _encod.GetBytes(value.ToString("yyyyMMdd"), 0, 8, _contents, col.DataAddress);
            else
                throw new Exception(col.Name + " column is not a date column");
        }

        #endregion

        #region - Write -

        public bool Read(BinaryReader reader)
        {
            return reader.Read(_contents, 0, _contents.Length) == _contents.Length;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(_contents);
        }

        #endregion

        #endregion
    }
}
