using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotSpatial.Topology;
using Microsoft.SqlServer.Types;
using FastShapefile.Conversion;

namespace FastShapefile.Test.Conversion
{
    public class ConvertToGeometryTests
    {
        [TestClass]
        public class SqlGeometryToGeometry_Method : GeometryTest
        {
            private void Compare(ILineString g, Coordinate[] coords)
            {
                IList<Coordinate> line = (g as ILineString).Coordinates;
                for (int i = 0; i < coords.Length; i++)
                {
                    Assert.AreEqual(coords[i].X, line[i].X);
                    Assert.AreEqual(coords[i].Y, line[i].Y);
                }
            }

            [TestMethod]
            public void Point_Convert()
            {
                SqlGeometry sg = CreateSqlPoint(127, 127);
                IGeometry g = ConvertToGeometry.SqlGeometryToGeometry(sg);

                Assert.IsTrue(g is IPoint);
                Assert.AreEqual((g as IPoint).X, sg.STX.Value);
                Assert.AreEqual((g as IPoint).Y, sg.STY.Value);
            }
            
            [TestMethod]
            public void LineString_Convert()
            {
                Coordinate[] coords = Shell1;
                SqlGeometry sq = CreateSqlLineString(coords);
                IGeometry g = ConvertToGeometry.SqlGeometryToGeometry(sq);

                Assert.IsTrue(g is ILineString);
                Compare(g as ILineString, coords);
            }

            [TestMethod]
            public void Polygon_Convert()
            {
                Coordinate[] shell = Shell2, hole1 = Hole1, hole2 = Hole2;
                SqlGeometry sg = CreatePolygon(shell, hole1, hole2);
                IGeometry g = ConvertToGeometry.SqlGeometryToGeometry(sg);

                Assert.IsTrue(g is IPolygon);
                Compare((g as IPolygon).Shell, shell);
                Compare((g as IPolygon).Holes[0], hole1);
                Compare((g as IPolygon).Holes[1], hole2);
            }
            
            [TestMethod]
            public void MultiPoint_Convert()
            {
                Coordinate[] shell = Shell1;
                SqlGeometry sg = CreateMultiPoint(shell);
                IGeometry g = ConvertToGeometry.SqlGeometryToGeometry(sg);

                Assert.IsTrue(g is IMultiPoint);

                IMultiPoint mp = g as IMultiPoint;
                for(int i = 0; i < mp.Count; i++)
                {
                    Assert.AreEqual(mp[i].X, shell[i].X);
                    Assert.AreEqual(mp[i].Y, shell[i].Y);
                }
            }

            [TestMethod]
            public void MultiLineString_Convert()
            {
            }
            
            [TestMethod]
            public void MultiPolygon_Convert()
            {
            }

            [TestMethod]
            public void GeometryCollection_Convert()
            {
            }
        }
    }
}
