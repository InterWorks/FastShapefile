using System;
using System.Collections.Generic;
using System.IO;

using DotSpatial.Projections;
using DotSpatial.Topology;

namespace FastShapefile.Transform
{
    public class GeometryTransform
    {
        #region - Static Methods -

        public static GeometryTransform GetTransform(string shapefile, string targetWKT)
        {
            string prjPath = Path.ChangeExtension(shapefile, ".prj");

            if (targetWKT != null && File.Exists(prjPath))
            {
                string sourceWKT = File.ReadAllText(prjPath);

                ProjectionInfo source = ProjectionInfo.FromEsriString(sourceWKT);
                ProjectionInfo target = ProjectionInfo.FromEsriString(targetWKT);

                if (source != null && !source.Matches(target))
                    return new GeometryTransform(source, target);
            }

            return null;
        }

        #endregion

        #region - Fields -

        private ProjectionInfo _source;
        private ProjectionInfo _target;

        #endregion

        #region - Ctor -

        public GeometryTransform(ProjectionInfo source, ProjectionInfo target)
        {
            _source = source;
            _target = target;
        }

        #endregion

        #region - Methods -

        public IEnvelope Apply(IEnvelope env)
        {
            double[] points = new double[]
            {
                env.Minimum.X, env.Minimum.Y, env.Maximum.X, env.Maximum.Y
            };

            Reproject.ReprojectPoints(points, null, _source, _target, 0, 2);

            return new Envelope(points[0], points[1], points[2], points[3]);
        }

        public IGeometry Apply(IGeometry geom)
        {
            if (geom == null)
                return null;
            if (geom is IPoint)
                return Apply(geom as IPoint);
            if (geom is ILineString)
                return Apply(geom as ILineString);
            if (geom is IPolygon)
                return Apply(geom as IPolygon);
            if (geom is IMultiPoint)
                return Apply(geom as IMultiPoint);
            if (geom is IMultiLineString)
                return Apply(geom as IMultiLineString);
            if (geom is IMultiPolygon)
                return Apply(geom as IMultiPolygon);

            throw new ArgumentException(string.Format("Could not transform geometry type '{0}'", geom.GetType()));
        }

        private void Apply(IList<Coordinate> coords)
        {
            double[] pts = new double[coords.Count * 2];

            for (int i = 0, p = 0; i < coords.Count; i++)
            {
                pts[p++] = coords[i].X;
                pts[p++] = coords[i].Y;
            }

            Reproject.ReprojectPoints(pts, null, _source, _target, 0, coords.Count);

            for (int i = 0, p = 0; i < coords.Count; i++)
            {
                coords[i].X = pts[p++];
                coords[i].Y = pts[p++];
            }
        }

        public IPoint Apply(IPoint point)
        {
            double[] pts = new double[] { point.X, point.Y };

            Reproject.ReprojectPoints(pts, null, _source, _target, 0, 1);

            point.X = pts[0];
            point.Y = pts[1];

            return point;
        }

        public ILineString Apply(ILineString line)
        {
            Apply(line.Coordinates);

            return line;
        }

        public IPolygon Apply(IPolygon polygon)
        {
            Apply(polygon.Shell);

            for (int i = 0, c = polygon.NumHoles; i < c; i++)
                Apply(polygon.Holes[i]);

            return polygon;
        }

        public IMultiPoint Apply(IMultiPoint multipoint)
        {
            Apply(multipoint.Coordinates);

            return multipoint;
        }

        public IMultiLineString Apply(IMultiLineString multilinestring)
        {
            for (int i = 0, c = multilinestring.NumGeometries; i < c; i++)
                Apply(multilinestring.Geometries[i]);

            return multilinestring;
        }

        public IMultiPolygon Apply(IMultiPolygon multipolygon)
        {
            for (int i = 0, c = multipolygon.NumGeometries; i < c; i++)
                Apply(multipolygon.Geometries[i]);

            return multipolygon;
        }

        #endregion
    }
}
