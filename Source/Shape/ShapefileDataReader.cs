using System.Data;
using System.IO;

using FastShapefile.Dbf;
using FastShapefile.Transform;

using DotSpatial.Topology;

namespace FastShapefile.Shape
{
    public class ShapefileDataReader : ShapefileReader
    {
        #region - Fields -

        protected DbfFile _dbf;
        protected DbfRecord _currentRecord;

        #endregion

        #region - Properties -

        public int RecordId { get; protected set; }
        public DbfHeader DbfHeader { get { return _dbf.Header; } }
        public DbfRecord Record { get { return _currentRecord; } }

        #endregion

        #region - Ctor -

        public ShapefileDataReader(string path, IGeometryFactory geometryFactory = null, GeometryTransform transform = null)
            : base(path, geometryFactory, transform)
        {
            _dbf = DbfFile.Open(Path.ChangeExtension(path, ".dbf"));

            _currentRecord = new DbfRecord(_dbf.Header);
        }

        #endregion

        #region - Overrides -

        protected override bool Read(int? recordIdx)
        {
            bool shapeRead = base.Read(recordIdx);
            if (recordIdx.HasValue)
                shapeRead &= _dbf.Read(recordIdx.Value, _currentRecord);
            else
                shapeRead &= _dbf.Read(_currentRecord);

            if (recordIdx.HasValue)
                RecordId = recordIdx.Value;
            else
                RecordId++;

            return shapeRead;
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_dbf != null)
                _dbf.Dispose();
        }

        #endregion
    }
}
