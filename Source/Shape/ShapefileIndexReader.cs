using System;
using System.IO;

using FastShapefile.Transform;

using DotSpatial.Topology;

namespace FastShapefile.Shape
{
    public class ShapefileIndexReader : IDisposable
    {
        #region - Fields -

        private string _path;
        private BinaryReader _reader;
        private int _recordPos = 0;

        public ShapefileHeader Header { get; private set; }
        public int FilePosition { get; private set; }
        public int ContentLength { get; private set; }
        public int RecordCount
        {
            get
            {
                return (int)(_reader.BaseStream.Length - 100) / 8;
            }
        }

        #endregion

        #region - Ctor -

        public ShapefileIndexReader(string path)
        {
            _path = path;
            _reader = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read));

            Header = ShapefileHeader.Read(_reader);
        }

        #endregion

        #region - Methods -

        public bool Read()
        {
            lock (_reader)
            {
                if (_reader.BaseStream.Position < _reader.BaseStream.Length)
                {
                    FilePosition = _reader.ReadInt32BE();
                    ContentLength = _reader.ReadInt32BE();

                    return true;
                }

                return false;
            }
        }

        public int ReadBlock(int[,] block, int start, int count, out int recordPosition)
        {
            lock (_reader)
            {
                int idx = start;
                recordPosition = _recordPos;
                while (_reader.BaseStream.Position < _reader.BaseStream.Length && idx < count)
                {
                    block[idx, 0] = _reader.ReadInt32BE();
                    block[idx, 1] = _reader.ReadInt32BE();

                    idx++;
                }

                _recordPos += idx - start;
                return idx - start;
            }
        }

        public ShapefileBlockReader CreateBlockReader(IGeometryFactory geometryFactory = null, GeometryTransform transform = null)
        {
            return new ShapefileBlockReader(Path.ChangeExtension(_path, ".shp"), this, geometryFactory, transform);
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        #endregion
    }
}
