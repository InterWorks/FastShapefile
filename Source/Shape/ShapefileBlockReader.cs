using FastShapefile.Transform;

using DotSpatial.Topology;

namespace FastShapefile.Shape
{
    public class ShapefileBlockReader : ShapefileDataReader
    {
        #region - Fields -

        private int[,] _block;
        private int _blockPos;
        private int _blockSize;
        private int _recordPos;
        private ShapefileIndexReader _index;

        #endregion

        #region - Ctor -

        public ShapefileBlockReader(string path, ShapefileIndexReader index, IGeometryFactory geometryFactory = null, GeometryTransform transform = null, int blockSize = 25)
            : base(path, geometryFactory, transform)
        {
            _block = new int[blockSize, 2];
            _blockPos = blockSize;
            _index = index;
        }

        #endregion

        #region - Methods -

        public override bool Read()
        {
            if (_blockPos >= _blockSize)
            {
                _blockSize = _index.ReadBlock(_block, 0, _block.Length / 2, out _recordPos);
                _blockPos = 0;
            }

            if (_blockSize > 0)
            {
                base._reader.BaseStream.Seek(_block[_blockPos, 0] * 2, System.IO.SeekOrigin.Begin);

                _blockPos++;

                return base.Read(_recordPos++);
            }

            return false;
        }

        #endregion
    }
}
