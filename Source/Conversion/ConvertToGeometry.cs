using System;

using DotSpatial.Topology;
using Microsoft.SqlServer.Types;

namespace FastShapefile.Conversion
{
    public static class ConvertToGeometry
    {
        #region - SqlGeometry to Geometry -

        public static IGeometry SqlGeometryToGeometry(SqlGeometry geom)
        {
            return SqlGeometryToGeometry(geom, new GeometryFactory());
        }

        public static IGeometry SqlGeometryToGeometry(SqlGeometry geom, GeometryFactory factory)
        {
            if (geom.STIsEmpty())
                return factory.CreateGeometryCollection(null);

            OpenGisGeometryType geometryType = (OpenGisGeometryType)Enum.Parse(typeof(OpenGisGeometryType), geom.STGeometryType().Value);
            switch (geometryType)
            {
                case OpenGisGeometryType.Point:
                    return SqlGeometryToGeometryPoint(geom, factory);
                case OpenGisGeometryType.LineString:
                    return SqlGeometryToGeometryLineString(geom, factory);
                case OpenGisGeometryType.Polygon:
                    return SqlGeometryToGeometryPolygon(geom, factory);
                case OpenGisGeometryType.MultiPoint:
                    return SqlGeometryToGeometryMultiPoint(geom, factory);
                case OpenGisGeometryType.MultiLineString:
                    return SqlGeometryToGeometryMultiLineString(geom, factory);
                case OpenGisGeometryType.MultiPolygon:
                    return SqlGeometryToGeometryMultiPolygon(geom, factory);
                case OpenGisGeometryType.GeometryCollection:
                    return SqlGeometryToGeometryGeometryCollection(geom, factory);
            }

            throw new ArgumentException(string.Format("Cannot convert SqlServer '{0}' to Geometry", geom.STGeometryType()), "geom");
        }

        private static IGeometryCollection SqlGeometryToGeometryGeometryCollection(SqlGeometry geometry, GeometryFactory factory)
        {
            IGeometry[] geoms = new IGeometry[geometry.STNumGeometries().Value];
            for (int i = 1; i <= geoms.Length; i++)
                geoms[i - 1] = SqlGeometryToGeometry(geometry.STGeometryN(i), factory);

            return factory.CreateGeometryCollection(geoms);
        }

        private static IMultiPolygon SqlGeometryToGeometryMultiPolygon(SqlGeometry geometry, GeometryFactory factory)
        {
            IPolygon[] polygons = new IPolygon[geometry.STNumGeometries().Value];
            for (var i = 1; i <= polygons.Length; i++)
                polygons[i - 1] = SqlGeometryToGeometryPolygon(geometry.STGeometryN(i), factory);

            return factory.CreateMultiPolygon(polygons);
        }

        private static IMultiLineString SqlGeometryToGeometryMultiLineString(SqlGeometry geometry, GeometryFactory factory)
        {
            ILineString[] lineStrings = new ILineString[geometry.STNumGeometries().Value];
            for (int i = 1; i <= lineStrings.Length; i++)
                lineStrings[i - 1] = SqlGeometryToGeometryLineString(geometry.STGeometryN(i), factory);

            return factory.CreateMultiLineString(lineStrings);
        }

        private static IGeometry SqlGeometryToGeometryMultiPoint(SqlGeometry geometry, GeometryFactory factory)
        {
            IPoint[] points = new IPoint[geometry.STNumGeometries().Value];
            for (int i = 1; i <= points.Length; i++)
                points[i - 1] = SqlGeometryToGeometryPoint(geometry.STGeometryN(i), factory);
            return factory.CreateMultiPoint(points);
        }

        private static IPoint SqlGeometryToGeometryPoint(SqlGeometry geometry, GeometryFactory factory)
        {
            return factory.CreatePoint(new Coordinate(geometry.STX.Value, geometry.STY.Value));
        }

        private static Coordinate[] GetPoints(SqlGeometry geometry)
        {
            Coordinate[] pts = new Coordinate[geometry.STNumPoints().Value];
            for (int i = 1; i <= pts.Length; i++)
            {
                SqlGeometry ptGeometry = geometry.STPointN(i);
                pts[i - 1] = new Coordinate(ptGeometry.STX.Value, ptGeometry.STY.Value);
            }
            return pts;
        }

        private static ILineString SqlGeometryToGeometryLineString(SqlGeometry geometry, GeometryFactory factory)
        {
            return factory.CreateLineString(GetPoints(geometry));
        }

        private static IPolygon SqlGeometryToGeometryPolygon(SqlGeometry geometry, GeometryFactory factory)
        {
            ILinearRing exterior = factory.CreateLinearRing(GetPoints(geometry.STExteriorRing()));

            ILinearRing[] interior = null;
            if (geometry.STNumInteriorRing() > 0)
            {
                interior = new ILinearRing[geometry.STNumInteriorRing().Value];
                for (int i = 1; i <= interior.Length; i++)
                    interior[i - 1] = factory.CreateLinearRing(GetPoints(geometry.STInteriorRingN(i)));
            }

            return factory.CreatePolygon(exterior, interior);
        }

        #endregion
    }
}
