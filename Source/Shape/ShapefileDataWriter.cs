using System.IO;

using FastShapefile.Dbf;

using DotSpatial.Topology;

namespace FastShapefile.Shape
{
    public class ShapefileDataWriter : ShapefileWriter
    {
        #region - Fields -

        private DbfFile _dbf;
        private DbfRecord _currentRecord;

        #endregion

        #region - Properties -

        public DbfHeader DbfHeader { get { return _dbf.Header; } }
        public DbfRecord Record { get { return _currentRecord; } }

        #endregion

        #region - Ctor -

        protected ShapefileDataWriter(string path, ShapefileHeader shapeHeader, FileMode fileMode, FileAccess fileAccess)
            : base(path, shapeHeader, fileMode, fileAccess)
        {
        }

        #endregion

        #region - Static Constructors -

        public static ShapefileDataWriter Create(string path, DbfHeader dbfHeader, ShapefileHeader shapeHeader)
        {
            ShapefileDataWriter writer = new ShapefileDataWriter(path, shapeHeader, FileMode.CreateNew, FileAccess.Write);

            writer._writerShape.BaseStream.Seek(100L, SeekOrigin.Begin);
            writer._writerIndex.BaseStream.Seek(100L, SeekOrigin.Begin);

            writer._dbf = DbfFile.Create(Path.ChangeExtension(path, ".dbf"), dbfHeader);
            writer._currentRecord = new DbfRecord(dbfHeader);

            writer._recordNumber = 1;
            writer._filePos = 50;

            return writer;
        }

        public new static ShapefileDataWriter Open(string path)
        {
            ShapefileHeader header;
            int recordNumber;
            using (BinaryReader reader = new BinaryReader(new FileStream(Path.ChangeExtension(path, ".shx"), FileMode.Open)))
            {
                header = ShapefileHeader.Read(reader);
                recordNumber = ((int)(reader.BaseStream.Length - 100) / 8) + 1;
            }

            ShapefileDataWriter writer = new ShapefileDataWriter(path, header, FileMode.Append, FileAccess.Write);

            writer._writerShape.BaseStream.Seek(0, SeekOrigin.End);
            writer._writerIndex.BaseStream.Seek(0, SeekOrigin.End);

            writer._recordNumber = recordNumber;
            writer._filePos = (int)writer._writerShape.BaseStream.Length / 2;

            // Need to push dbf reader to end of file.
            writer._dbf = DbfFile.Open(Path.ChangeExtension(path, ".dbf"));
            writer._currentRecord = new DbfRecord(writer._dbf.Header);

            return writer;
        }

        #endregion

        #region - Methods -

        #region - Write -

        public void Write(IGeometry geom, byte[] rawRecord)
        {
            base.Write(geom);

            // Write the dbf
            if (_dbf != null)
            {
                _currentRecord.SetRawRecord(rawRecord);

                _dbf.Write(_currentRecord);
            }
        }

        public void Write(IGeometry geom, params object[] values)
        {
            base.Write(geom);

            // Write the dbf
            if (_dbf != null)
            {
                for (int i = 0; i < values.Length; i++)
                    _currentRecord.SetRaw(i, values[i]);

                _dbf.Write(_currentRecord);
            }
        }

        #endregion

        #region - Flush -

        public new void Flush()
        {
            base.Flush();

            if (_dbf != null)
                _dbf.Flush();
        }

        #endregion

        #region - Dispose -

        public override void Dispose()
        {
            if (_dbf != null)
            {
                _dbf.Dispose();
                _dbf = null;
            }

            base.Dispose();
        }

        #endregion

        #endregion
    }
}
