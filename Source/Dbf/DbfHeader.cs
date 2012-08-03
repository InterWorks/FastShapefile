using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FastShapefile.Dbf
{
    public class DbfHeader
    {
        #region - Const -

        private const byte FIELD_DESCRIPTOR_TERMINATOR = 0x0D;
        private const ushort HEADER_START_LENGTH = 33;
        private const ushort RECORD_LENGTH = 32;

        #endregion

        #region - Fields -

        private List<DbfColumn> _columns = new List<DbfColumn>();
        private Dictionary<string, int> _map = new Dictionary<string, int>();

        private ushort _recordLength;
        private ushort _headerLength;

        private byte[] _emptyRecord;

        #endregion

        #region - Properties -

        internal ushort RecordLength { get { return _recordLength; } }

        public DateTime LastUpdate { get; set; }
        public uint RecordCount { get; internal set; }

        public DbfColumn this[int index]
        {
            get { return _columns[index]; }
        }

        public int Count
        {
            get { return _columns.Count; }
        }

        public byte[] EmptyRecordData
        {
            get
            {
                if (_emptyRecord == null)
                {
                    _emptyRecord = new byte[_recordLength];
                    for (int i = 0; i < _recordLength; i++)
                        _emptyRecord[i] = 0x20;
                }

                return _emptyRecord.Clone() as byte[];
            }
        }

        #endregion

        #region - Ctor -

        public DbfHeader()
        {
            // Includes delete flag
            _recordLength = 1;

            // Length of non field descriptor bytes
            _headerLength = HEADER_START_LENGTH;
        }

        #endregion

        #region - Methods -

        public int GetOrdinal(string name)
        {
            return _map[name];
        }

        public void Read(BinaryReader reader)
        {
            // Read version
            if (reader.ReadByte() != 3)
                throw new Exception("Only dbase version 3 is supported");

            // Last update
            int year = (int)reader.ReadByte(), month = (int)reader.ReadByte(), day = (int)reader.ReadByte();
            LastUpdate = new DateTime(year + 1900, month, day);

            RecordCount = reader.ReadUInt32();
            
            // Ignore the header length and record length since they will be calced.
            reader.ReadUInt16();
            reader.ReadUInt16();

            // skip the header reserved bytes
            reader.ReadBytes(20);

            while(reader.PeekChar() != FIELD_DESCRIPTOR_TERMINATOR)
            {
                string name = new String(reader.ReadChars(11)).TrimEnd((char)0);
                DbfColumnType type = (DbfColumnType)reader.ReadByte();

                // Ignore data address
                reader.ReadBytes(4);

                byte length = reader.ReadByte();
                byte decimalCount = reader.ReadByte();

                // Skip the reserved bytes
                reader.ReadBytes(14);

                AddColumn(name, type, length, decimalCount);
            }

            reader.ReadByte();
        }

        public void Write(BinaryWriter writer)
        {
            DateTime now = DateTime.Today;
            RecordCount = 0; //Reset since the dispose will write the finish amount

            // Version number
            writer.Write((byte)0x03);

            // Last update
            writer.Write((byte)(now.Year - 1900));
            writer.Write((byte)now.Month);
            writer.Write((byte)now.Day);

            writer.Write(RecordCount);
            writer.Write(_headerLength);
            writer.Write(_recordLength);

            // write the header reserved empty bytes
            writer.Write(new byte[20]);

            byte[] colReserved = new byte[14];
            Encoding encode = Encoding.ASCII;
            foreach (DbfColumn col in _columns)
            {
                writer.Write(encode.GetBytes(col.Name.PadRight(11, (char)0)));
                writer.Write((byte)col.Type);

                writer.Write((uint)col.DataAddress);

                writer.Write(col.Length);
                writer.Write(col.DecimalCount);

                writer.Write(colReserved);
            }

            writer.Write(FIELD_DESCRIPTOR_TERMINATOR);
            writer.Flush();
        }

        public void AddBoolean(string name)
        {
            AddColumn(name, DbfColumnType.Boolean, 1, 0);
        }

        public void AddCharacter(string name, byte length)
        {
            AddColumn(name, DbfColumnType.Character, length, 0);
        }

        public void AddDate(string name)
        {
            AddColumn(name, DbfColumnType.Date, 8, 0);
        }

        public void AddNumber(string name, byte length, byte decimalCount)
        {
            length = Math.Min(length, (byte)18);
            decimalCount = Math.Min(decimalCount, (byte)6);

            AddColumn(name, DbfColumnType.Number, length, decimalCount);
        }

        private void AddColumn(string name, DbfColumnType type, byte length, byte decimalCount)
        {
            if (name.Length > 10)
                name = name.Substring(0, 10);

            if (length == 0)
                throw new Exception("Length cannot be zero");

            _map.Add(name, _columns.Count);

            _columns.Add(new DbfColumn
            {
                DataAddress = _recordLength,
                Name = name,
                Type = type,
                Length = length,
                DecimalCount = decimalCount
            });

            _recordLength += length;
            _headerLength += RECORD_LENGTH;

            _emptyRecord = null;
        }

        internal long GetSeekPosition(int recordIndex)
        {
            return _headerLength + (recordIndex * _recordLength);
        }

        public DbfHeader Clone()
        {
            DbfHeader header = new DbfHeader();
            header.LastUpdate = this.LastUpdate;

            foreach (DbfColumn col in _columns)
                header.AddColumn(col.Name, col.Type, col.Length, col.DecimalCount);

            return header;
        }

        #endregion
    }
}
