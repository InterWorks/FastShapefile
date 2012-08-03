using System;

using DotSpatial.Topology;
using Microsoft.SqlServer.Types;
using System.Collections.Generic;

namespace FastShapefile.Conversion
{
    public class ConvertToSqlGeometry
    {
        #region - Geometry To SqlGeometry -

        #region - GeometryToSqlGeometry -

        public static SqlGeometry GeometryToSqlGeometry(IGeometry geom, int srid)
        {
            SqlGeometryBuilder bldr = new SqlGeometryBuilder();
            bldr.SetSrid(srid);

            GeometryToSqlGeometry(geom, bldr);

            return bldr.ConstructedGeometry;
        }

        #endregion

        #region - IGeometry -

        private static void GeometryToSqlGeometry(IGeometry geom, SqlGeometryBuilder bldr)
        {
            if (geom is IPoint)
                GeometryToSqlGeometry(geom as IPoint, bldr);
            else if (geom is IMultiPoint)
                GeometryToSqlGeometry(geom as IMultiPoint, bldr);
            else if (geom is ILineString)
                GeometryToSqlGeometry(geom as ILineString, bldr);
            else if (geom is IMultiLineString)
                GeometryToSqlGeometry(geom as IMultiLineString, bldr);
            else if (geom is IPolygon)
                GeometryToSqlGeometry(geom as IPolygon, bldr);
            else if (geom is IMultiPolygon)
                GeometryToSqlGeometry(geom as IMultiPolygon, bldr);
            else if (geom is IGeometryCollection)
                GeometryToSqlGeometry(geom as IGeometryCollection, bldr);
            else
                throw new Exception(String.Format("Unable to convert geometry of type '{0}'", geom.GetType().Name));
        }

        #endregion

        #region - IPoint -

        private static void GeometryToSqlGeometry(IPoint geom, SqlGeometryBuilder bldr)
        {
            bldr.BeginGeometry(OpenGisGeometryType.MultiPoint);

            bldr.BeginFigure(geom.X, geom.Y);
            bldr.EndFigure();

            bldr.EndGeometry();
        }

        #endregion

        #region - IMultiPoint -

        private static void GeometryToSqlGeometry(IMultiPoint geom, SqlGeometryBuilder bldr)
        {
            bldr.BeginGeometry(OpenGisGeometryType.MultiPoint);

            for (int i = 0, c = geom.NumGeometries; i < c; i++)
                GeometryToSqlGeometry(geom.Geometries[i] as IPoint, bldr);

            bldr.EndGeometry();
        }

        #endregion

        #region - ILineString -

        private static void GeometryToSqlGeometry(ILineString geom, SqlGeometryBuilder bldr)
        {
            bldr.BeginGeometry(OpenGisGeometryType.LineString);

            AddFigure(geom, bldr);

            bldr.EndGeometry();
        }

        #endregion

        #region - IMultiLineString -

        private static void GeometryToSqlGeometry(IMultiLineString geom, SqlGeometryBuilder bldr)
        {
            bldr.BeginGeometry(OpenGisGeometryType.MultiLineString);

            for (int i = 0, c = geom.NumGeometries; i < c; i++)
                GeometryToSqlGeometry(geom.Geometries[i] as ILineString, bldr);

            bldr.EndGeometry();
        }

        #endregion

        #region - IPolygon -

        private static void GeometryToSqlGeometry(IPolygon geom, SqlGeometryBuilder bldr)
        {
            bldr.BeginGeometry(OpenGisGeometryType.Polygon);

            AddFigure(geom.Shell, bldr);

            for (int i = 0, c = geom.NumHoles; i < c; i++)
                AddFigure(geom.Holes[i], bldr);

            bldr.EndGeometry();
        }

        #endregion

        #region - IMultiPolygon -

        private static void GeometryToSqlGeometry(IMultiPolygon geom, SqlGeometryBuilder bldr)
        {
            bldr.BeginGeometry(OpenGisGeometryType.MultiPolygon);

            for (int i = 0, c = geom.NumGeometries; i < c; i++)
                GeometryToSqlGeometry(geom.Geometries[i] as IPolygon, bldr);

            bldr.EndGeometry();
        }

        #endregion

        #region - IGeometryCollection -

        private static void GeometryToSqlGeometry(IGeometryCollection geom, SqlGeometryBuilder bldr)
        {
            bldr.BeginGeometry(OpenGisGeometryType.GeometryCollection);

            for (int i = 0, c = geom.NumGeometries; i < c; i++)
                GeometryToSqlGeometry(geom.Geometries[i], bldr);

            bldr.EndGeometry();
        }

        #endregion

        #region - AddFigure -

        private static void AddFigure(ILineString line, SqlGeometryBuilder bldr)
        {
            IList<Coordinate> coords = line.Coordinates;

            bldr.BeginFigure(coords[0].X, coords[0].Y);
            for (int i = 0; i < coords.Count; i++)
                bldr.AddLine(coords[i].X, coords[i].Y);
            bldr.EndFigure();
        }

        #endregion

        #endregion
    }
}
