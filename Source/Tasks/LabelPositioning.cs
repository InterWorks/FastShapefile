using System.Collections.Generic;

using DotSpatial.Topology;
using DotSpatial.Topology.Algorithm;
using DotSpatial.Topology.Operation.Buffer;

namespace FastShapefile.Tasks
{
    public static class LabelPositioning
    {
        #region - Static Methods -

        #region - Public -

        public static IPoint Calculate(IGeometry geom)
        {
            double bufferAmt = -0.001, inc = -.0002, stopper = 0;
            IGeometry prevBuffer = null, buffer = geom;

            BufferBuilder bldr = new BufferBuilder();
            bldr.EndCapStyle = BufferStyle.CapRound;

            while (!IsAllCW(buffer))
            {
                buffer = bldr.Buffer(geom, bufferAmt);

                if (stopper++ > 300)
                    break;

                prevBuffer = buffer;
                bufferAmt += inc;
            }

            IPoint labelPoint;
            if (buffer is IGeometryCollection) // It is empty.
                labelPoint = GetPoint(prevBuffer);
            else
                labelPoint = GetPoint(buffer);

            // Should never happen but just in case we need to have some point.
            if (labelPoint == null)
                return geom.Centroid;
            else
                return labelPoint;
        }

        #endregion

        #region - Private -

        private static IPoint GetPoint(IGeometry geom)
        {
            if (geom is IMultiPolygon)
            {
                IPolygon maxPoly = null;
                double maxArea = -1;
                foreach (IPolygon poly in (geom as IMultiPolygon).Geometries)
                {
                    double area = poly.Area;
                    if (area > maxArea)
                    {
                        maxArea = area;
                        maxPoly = poly;
                    }
                }

                return GetPoint(maxPoly);
            }
            else if (geom is IPolygon)
                return (geom as IPolygon).Centroid;

            return null;
        }

        private static bool IsAllCW(IGeometry geom)
        {
            if (geom is IPolygon)
                return IsAllCW(geom as IPolygon);
            else if (geom is IMultiPolygon)
            {
                foreach (IPolygon poly in (geom as IMultiPolygon).Geometries)
                {
                    if (!IsAllCW(poly))
                        return false;
                }
            }

            return true;
        }

        private static bool IsAllCW(IPolygon poly)
        {
            IList<Coordinate> shell = poly.Shell.Coordinates;
            for (int i = 0, c = shell.Count - 3; i < c; i++)
            {
                if (CgAlgorithms.ComputeOrientation(shell[i], shell[i + 1], shell[i + 2]) == 1)
                    return false;
            }

            return true;
        }

        #endregion

        #endregion
    }
}