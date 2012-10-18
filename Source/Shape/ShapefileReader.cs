using System;
using System.Collections.Generic;
using System.IO;

using FastShapefile.Transform;

using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;

namespace FastShapefile.Shape
{
    public class ShapefileReader : IDisposable
    {
        #region - Fields -

        private Func<IGeometry> _shapeFunc;
        private IGeometry _currentShape;
        private IGeometryFactory _gf;
        private GeometryTransform _transform;

        protected BinaryReader _reader;

        #endregion

        #region - Ctor -

        public ShapefileReader(string path, IGeometryFactory geometryFactory = null, GeometryTransform transform = null)
        {
            _gf = geometryFactory ?? new GeometryFactory();
            _reader = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read));
            ShapeHeader = ShapefileHeader.Read(_reader);
            ShapeEnvelope = new Envelope();

            switch (ShapeHeader.ShapeType)
            {
                case ShapefileGeometryType.Point:
                case ShapefileGeometryType.PointZ:
                    _shapeFunc = ReadPoint;
                    break;
                case ShapefileGeometryType.PolyLine:
                case ShapefileGeometryType.PolyLineZ:
                    _shapeFunc = ReadPolyLine;
                    break;
                case ShapefileGeometryType.Polygon:
                case ShapefileGeometryType.PolygonZ:
                    _shapeFunc = ReadPolygon;
                    break;
                case ShapefileGeometryType.MultiPoint:
                case ShapefileGeometryType.MultiPointZ:
                    _shapeFunc = ReadMultiPoint;
                    break;
                default:
                    throw new Exception("Shape type is not supported");
            }

            if (transform != null)
            {
                _transform = transform;

                Func<IGeometry> origFun = _shapeFunc;
                _shapeFunc = () =>
                {
                    return _transform.Apply(origFun());
                };
            }
        }

        #endregion

        #region - Properties -

        public string Projection { get; private set; }
        public ShapefileHeader ShapeHeader { get; private set; }
        public IGeometry Geometry { get { return _currentShape; } }
        public Envelope ShapeEnvelope { get; private set; }

        #endregion

        #region - Methods -

        #region - Main Reading -

        public virtual bool Read()
        {
            return Read(null);
        }

        protected virtual bool Read(int? recordIdx)
        {
            if (_reader.BaseStream.Position == _reader.BaseStream.Length)
                return false;

            int recordNumber = _reader.ReadInt32BE();
            int contentLength = _reader.ReadInt32BE();

            _currentShape = _shapeFunc();

            return true;
        }

        #endregion

        #region - Shape Reading -

        #region - ReadPoint -

        private IGeometry ReadPoint()
        {
            ShapefileGeometryType type = (ShapefileGeometryType)_reader.ReadInt32();
            if (type == ShapefileGeometryType.NullShape)
                return _gf.CreatePoint((Coordinate)null);

            if (type == ShapefileGeometryType.Point)
                return _gf.CreatePoint(new Coordinate(
                    _reader.ReadDouble(),
                    _reader.ReadDouble()));
            else if (type == ShapefileGeometryType.PointZ)
                return _gf.CreatePoint(new Coordinate(
                    _reader.ReadDouble(),
                    _reader.ReadDouble(),
                    _reader.ReadDouble(),
                    _reader.ReadDouble()));
            else
                throw new Exception("Attempting to load a non-point as point.");	
        }

        #endregion

        #region - ReadPolyLine -

        private IGeometry ReadPolyLine()
        {
            ShapefileGeometryType type = (ShapefileGeometryType)_reader.ReadInt32();
            if (type == ShapefileGeometryType.NullShape)
                return _gf.CreateLineString((Coordinate[])null);

            if (type != ShapefileGeometryType.PolyLine && type != ShapefileGeometryType.PolyLineZ)
                throw new Exception("Attempting to load a non-point as point.");

            // Read the box
            double xMin = _reader.ReadDouble(), yMin = _reader.ReadDouble();
            ShapeEnvelope.Init(xMin, _reader.ReadDouble(),
                yMin, _reader.ReadDouble());

            // Read poly header
            int numParts = _reader.ReadInt32();
            int numPoints = _reader.ReadInt32();

            // Read parts array
            int[] partOffsets = new int[numParts];
            for (int i = 0; i < numParts; i++)
                partOffsets[i] = _reader.ReadInt32();

            // Read the parts and their points
            ILineString[] lines = new ILineString[numParts];
            for (int part = 0, last = numParts - 1; part < numParts; part++)
            {
                int start = partOffsets[part], stop = (part == last ? numPoints : partOffsets[part + 1]);
                Coordinate[] coords = new Coordinate[stop - start];

                for (int i = 0; i < coords.Length; i++)
                    coords[i] = new Coordinate(
                        _reader.ReadDouble(),
                        _reader.ReadDouble());

                lines[part] = _gf.CreateLineString(coords);
            }

            if (type == ShapefileGeometryType.PolyLineZ)
            {
                // Don't use the min/max z values. Just read & throw away.
                _reader.ReadDouble();
                _reader.ReadDouble();
                for (int i = 0; i < lines.Length; i++)
                {
                    IList<Coordinate> coords = lines[i].Coordinates;
                    for(int x = 0; x < coords.Count; x++)
                        coords[i].Z = _reader.ReadDouble();
                }

                // Don't use the min/max m values. Just read & throw away.
                _reader.ReadDouble();
                _reader.ReadDouble();
                for (int i = 0; i < lines.Length; i++)
                {
                    IList<Coordinate> coords = lines[i].Coordinates;
                    for (int x = 0; x < coords.Count; x++)
                        coords[i].M = _reader.ReadDouble();
                }
            }

            if (lines.Length == 1)
                return lines[0];
            else
                return _gf.CreateMultiLineString(lines);
        }

        #endregion

        #region - ReadPolygon -

        private IGeometry ReadPolygon()
        {
            ShapefileGeometryType type = (ShapefileGeometryType)_reader.ReadInt32();
            if (type == ShapefileGeometryType.NullShape)
                return _gf.CreatePolygon(null, null);

            if (type != ShapefileGeometryType.Polygon && type != ShapefileGeometryType.PolygonZ)
                throw new Exception("Attempting to load a non-polygon as polygon.");

            // Read the box
            double xMin = _reader.ReadDouble(), yMin = _reader.ReadDouble();
            ShapeEnvelope.Init(xMin, _reader.ReadDouble(),
                yMin, _reader.ReadDouble());

            // Read poly header
            int numParts = _reader.ReadInt32();
            int numPoints = _reader.ReadInt32();

            // Read parts array
            int[] partOffsets = new int[numParts];
            for (int i = 0; i < numParts; i++)
                partOffsets[i] = _reader.ReadInt32();

            // Read the parts and their points
            List<ILinearRing> shells = new List<ILinearRing>();
            List<ILinearRing> holes = new List<ILinearRing>();
            Coordinate[] allCoords = new Coordinate[numPoints];
            for (int part = 0, last = numParts - 1, x = 0; part < numParts; part++)
            {
                int start = partOffsets[part], stop = (part == last ? numPoints : partOffsets[part + 1]);
                Coordinate[] coords = new Coordinate[stop - start];

                for (int i = 0; i < coords.Length; i++)
                    coords[i] = allCoords[x++] = new Coordinate(
                        _reader.ReadDouble(),
                        _reader.ReadDouble());

                ILinearRing ring = _gf.CreateLinearRing(coords);

                // Check for hole.
                if (CgAlgorithms.IsCounterClockwise(coords))
                    holes.Add(ring);
                else 
                    shells.Add(ring);
            }

            if (type == ShapefileGeometryType.PolygonZ)
            {
                // z min/max
                _reader.ReadDouble();
                _reader.ReadDouble();

                for (int i = 0; i < allCoords.Length; i++)
                    allCoords[i].Z = _reader.ReadDouble();

                // m min/max
                _reader.ReadDouble();
                _reader.ReadDouble();
                for (int i = 0; i < allCoords.Length; i++)
                    allCoords[i].M = _reader.ReadDouble();
            }

            // Create the polygon
            if (shells.Count == 1)
                return _gf.CreatePolygon(shells[0], holes.ToArray());
            else // Create a multipolygon
            {
                List<IPolygon> polys = new List<IPolygon>(shells.Count);
                foreach (ILinearRing shell in shells)
                {
                    IEnvelope shellEnv = shell.EnvelopeInternal;
                    List<ILinearRing> shellHoles = new List<ILinearRing>();
                    for(int i = holes.Count - 1; i >=0; i--)
                    {
                        ILinearRing hole = holes[i];
                        if (shellEnv.Contains(hole.EnvelopeInternal))
                        {
                            shellHoles.Add(hole);
                            holes.RemoveAt(i);
                        }
                    }

                    polys.Add(_gf.CreatePolygon(shell, shellHoles.ToArray()));
                }

                // Add the holes that weren't contains as shells
                foreach (ILinearRing hole in holes)
                {
                    LinearRing ring = new LinearRing(hole.Reverse());
                    polys.Add(_gf.CreatePolygon(ring, null));
                }

                return _gf.CreateMultiPolygon(polys.ToArray());
            }
        }

        #endregion

        #region - ReadMultiPoint -

        private IGeometry ReadMultiPoint()
        {
            ShapefileGeometryType type = (ShapefileGeometryType)_reader.ReadInt32();
            if (type == ShapefileGeometryType.NullShape)
                return _gf.CreateMultiPoint((Coordinate[])null);

            if (type != ShapefileGeometryType.MultiPoint && type != ShapefileGeometryType.MultiPointZ)
                throw new Exception("Attempting to load a non-multipoint as multipoint.");

            // Read the box
            double xMin = _reader.ReadDouble(), yMin = _reader.ReadDouble();
            ShapeEnvelope.Init(xMin, _reader.ReadDouble(),
                yMin, _reader.ReadDouble());

            Coordinate[] coords = new Coordinate[_reader.ReadInt32()];
            for (int i = 0; i < coords.Length; i++)
                coords[i] = new Coordinate(
                    _reader.ReadDouble(),
                    _reader.ReadDouble());

            if (type == ShapefileGeometryType.MultiPointZ)
            {
                // Don't use the min/max z values. Just read & throw away.
                _reader.ReadDouble();
                _reader.ReadDouble();
                for (int i = 0; i < coords.Length; i++)
                    coords[i].Z = _reader.ReadDouble();

                // Don't use the min/max m values. Just read & throw away.
                _reader.ReadDouble();
                _reader.ReadDouble();
                for (int i = 0; i < coords.Length; i++)
                    coords[i].M = _reader.ReadDouble();
            }

            return _gf.CreateMultiPoint(coords);
        }

        #endregion

        #endregion

        #region - Reset -

        public void Reset()
        {
            _reader.BaseStream.Seek(100L, SeekOrigin.Begin);
        }

        #endregion

        #region - IDisposable -

        public virtual void Dispose()
        {
            _reader.Dispose();
        }

        #endregion

        #endregion
    }
}
