using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace FastShapefile.Dbf
{
    public class DbfFile : IDisposable, IEnumerable<DbfRecord>
    {
        #region - Fields -

        protected Stream _stream;
        protected bool _headerNeedsWrite = false;
        private BinaryWriter _writer;
        private BinaryReader _reader;

        #endregion

        #region - Properties -

        public DbfHeader Header { get; private set; }

        #endregion

        #region - Ctor -

        private DbfFile(Stream stream, bool isWriting, DbfHeader header = null)
        {
            if (header == null)
                Header = new DbfHeader();
            else
                Header = header;

            _stream = stream;
            _reader = new BinaryReader(stream);

            if (isWriting)
            {
                _headerNeedsWrite = true;
                _writer = new BinaryWriter(stream);
            }
            else
                Header.Read(_reader);
        }

        #endregion

        #region - Static Creators -

        public static DbfFile Create(string path, DbfHeader header = null)
        {
            return new DbfFile(
                File.Create(path), true, header);
        }

        public static DbfFile Open(string path)
        {
            DbfFile file = new DbfFile(
                File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read), false);

            return file;
        }

        public static DbfFile Append(string path)
        {
            DbfFile file = new DbfFile(
                File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read), false);

            // Seek to end of file
            file._stream.Seek(file._stream.Length - 1, SeekOrigin.Begin);

            return file;
        }

        #endregion

        #region - Methods -

        public bool Read(int index, DbfRecord record)
        {
            _stream.Seek(Header.GetSeekPosition(index), SeekOrigin.Begin);

            return record.Read(_reader);
        }

        public bool Read(DbfRecord record)
        {
            return record.Read(_reader);
        }

        public void Write(DbfRecord record)
        {
            lock (_writer)
            {
                if (_headerNeedsWrite)
                {
                    Header.Write(_writer);
                    _headerNeedsWrite = false;
                }

                record.Write(_writer);

                Header.RecordCount++;
            }
        }

        public void Flush()
        {
            if(_writer != null)
                _writer.Flush();
        }

        public void Dispose()
        {
            // Write the record count;
            if (_writer != null)
            {
                if (_headerNeedsWrite)
                    Header.Write(_writer);

                // Write the closing byte.
                _writer.Write((byte)26);

                // Write the record count.
                _stream.Seek(4, SeekOrigin.Begin);
                _writer.Write(Header.RecordCount);
            }

            _stream.Flush();
            _stream.Dispose();
        }

        public IEnumerator<DbfRecord> GetEnumerator()
        {
            return new DbfRecordEnumerator(_reader, Header);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region - Record Enumerator -

        private class DbfRecordEnumerator : IEnumerator<DbfRecord>
        {
            private DbfRecord _record;
            private BinaryReader _reader;

            public DbfRecordEnumerator(BinaryReader reader, DbfHeader header)
            {
                _reader = reader;
                _record = new DbfRecord(header);
            }

            public DbfRecord Current
            {
                get { return _record; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                return _record.Read(_reader);
            }

            public void Reset()
            { }

            public void Dispose()
            { }
        }

        #endregion
    }
}
