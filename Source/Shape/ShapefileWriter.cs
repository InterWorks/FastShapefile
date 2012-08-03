using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using DotSpatial.Topology;

namespace FastShapefile.Shape
{
    public class ShapefileWriter : IDisposable
    {
        #region - Fields -

        protected IGeometryFactory _geometryFactory = null;

        protected BinaryWriter _writerShape;
        protected BinaryWriter _writerIndex;
        protected ShapefileHeader _header;

        protected int _recordNumber = 1;
        protected int _filePos = 50;

        #endregion

        #region - Properties -

        public ShapefileHeader Header { get { return _header; } }

        #endregion

        #region - Ctor -

        protected ShapefileWriter(string path, ShapefileHeader header, FileMode fileMode, FileAccess fileAccess)
        {
            _writerShape = new BinaryWriter(new FileStream(path, fileMode, fileAccess));
            _writerIndex = new BinaryWriter(new FileStream(Path.ChangeExtension(path, ".shx"), fileMode, fileAccess));

            _header = header;
        }

        #endregion

        #region - Static Constructors -

        public static ShapefileWriter Create(string path, ShapefileHeader header)
        {
            ShapefileWriter writer = new ShapefileWriter(path, header, FileMode.CreateNew, FileAccess.Write);

            writer._writerShape.BaseStream.Seek(100L, SeekOrigin.Begin);
            writer._writerIndex.BaseStream.Seek(100L, SeekOrigin.Begin);

            writer._recordNumber = 1;
            writer._filePos = 50;

            return writer;
        }

        public static ShapefileWriter Open(string path)
        {
            ShapefileHeader header;
            int recordNumber;
            using (BinaryReader reader = new BinaryReader(new FileStream(Path.ChangeExtension(path, ".shx"), FileMode.Open)))
            {
                header = ShapefileHeader.Read(reader);
                recordNumber = ((int)(reader.BaseStream.Length - 100) / 8) + 1;
            }

            ShapefileWriter writer = new ShapefileWriter(path, header, FileMode.Append, FileAccess.Write);

            writer._writerShape.BaseStream.Seek(0, SeekOrigin.End);
            writer._writerIndex.BaseStream.Seek(0, SeekOrigin.End);

            writer._recordNumber = recordNumber;
            writer._filePos = (int)writer._writerShape.BaseStream.Length / 2;

            return writer;
        }

        #endregion

        #region - Methods -

        #region - Shape Writing -

        #region - Write Header -

        private void Write(ShapefileHeader header, BinaryWriter stream, int fileLength)
        {
            stream.BaseStream.Seek(0, SeekOrigin.Begin);

            stream.WriteBE(9994);
            stream.WriteBE(0);
            stream.WriteBE(0);
            stream.WriteBE(0);
            stream.WriteBE(0);
            stream.WriteBE(0);
            stream.WriteBE(fileLength);
            stream.Write(1000);
            stream.Write((int)header.ShapeType);
            stream.Write(header.Bounds.Minimum.X);
            stream.Write(header.Bounds.Minimum.Y);
            stream.Write(header.Bounds.Maximum.X);
            stream.Write(header.Bounds.Maximum.Y);
        }

        #endregion

        #region - Flush -

        public void Flush()
        {
            if (_writerShape != null)
                _writerShape.Flush();

            if (_writerIndex != null)
                _writerIndex.Flush();
        }

        #endregion

        #region - IEnvelope -

        private void Write(IEnvelope env)
        {
            _writerShape.Write(env.Minimum.X);
            _writerShape.Write(env.Minimum.Y);
            _writerShape.Write(env.Maximum.X);
            _writerShape.Write(env.Maximum.Y);
        }

        #endregion

        #region - IList<Coordinate> -

        private void Write(IList<Coordinate> coords)
        {
            foreach (Coordinate coord in coords)
            {
                _writerShape.Write(coord.X);
                _writerShape.Write(coord.Y);
            }
        }

        #endregion

        #region - IGeometry -

        public void Write(IGeometry geom)
        {
            // TODO: Finish making writing for shape types.

            lock (_writerIndex)
            lock (_writerShape)
            {
                if (geom is IMultiPolygon)
                    Write(geom as IMultiPolygon);
                else if (geom is IPolygon)
                    Write(geom as IPolygon);
                else
                    throw new Exception("Unable to write shape");
            }
        }

        #endregion

        #region - IPolygon -

        private void Write(IPolygon poly)
        {
            EnsureGeometryFactory(poly);

            Write(_geometryFactory.CreateMultiPolygon(new IPolygon[] { poly }));
        }

        #endregion

        #region - IMultiPolygon -

        private void Write(IMultiPolygon multiPoly)
        {
            EnsureGeometryFactory(multiPoly);

            int contentLength = ContentLength(multiPoly);

            _writerIndex.WriteBE(_filePos);
            _writerIndex.WriteBE(contentLength);

            _writerShape.WriteBE(_recordNumber++);
            _writerShape.WriteBE(contentLength);
            _writerShape.Write((int)ShapefileGeometryType.Polygon);

            Write(multiPoly.EnvelopeInternal);

            int numParts = multiPoly.Geometries.Cast<IPolygon>().Sum(g => g.Holes.Length + 1);
            int numPoints = multiPoly.NumPoints;

            _writerShape.Write(numParts);
            _writerShape.Write(numPoints);

            // write the offsets to the points
            int offset = 0;
            foreach (IPolygon poly in multiPoly.Geometries)
            {
                // offset to the shell points
                _writerShape.Write(offset);
                offset = offset + poly.Shell.NumPoints;

                // offstes to the holes
                foreach (ILinearRing ring in poly.Holes)
                {
                    _writerShape.Write(offset);
                    offset = offset + ring.NumPoints;
                }
            }

            // write the points 
            foreach (IPolygon poly in multiPoly.Geometries)
            {
                Write(poly.Shell.Coordinates);
                foreach (ILinearRing ring in poly.Holes)
                    Write(ring.Coordinates);
            }

            _filePos += 4 + contentLength;
        }

        #endregion

        #endregion

        #region - Content Length Methods -

        public int ContentLength(IMultiPolygon poly)
        {
            int numParts = poly.Geometries.Cast<IPolygon>().Sum(g => g.Holes.Length + 1);
            return (22 + (2 * numParts) + (poly.NumPoints * 8)); // 22 => shapetype(2) + bbox(4*4) + numparts(2) + numpoints(2)
        }

        #endregion

        #region - IDisposable -

        public virtual void Dispose()
        {
            if (_writerShape != null)
            {
                Write(_header, _writerShape, (int)_filePos);

                _writerShape.Flush();
                _writerShape.Dispose();
                _writerShape = null;
            }

            if (_writerIndex != null)
            {
                Write(_header, _writerIndex, 50 + (4 * (_recordNumber - 1)));

                _writerIndex.Flush();
                _writerIndex.Dispose();
                _writerIndex = null;
            }
        }

        #endregion

        #region - Helpers -

        private void EnsureGeometryFactory(IGeometry geom)
        {
            if (_geometryFactory == null)
                _geometryFactory = (geom.Factory ?? new GeometryFactory(new PrecisionModel(geom.PrecisionModel)));
        }

        #endregion

        #endregion
    }
}
