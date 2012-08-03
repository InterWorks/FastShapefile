using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DotSpatial.Topology;
using Microsoft.SqlServer.Types;

namespace FastShapefile.Test
{
    public abstract class GeometryTest
    {
        #region - Fields -

        private GeometryFactory _factory;

        #endregion

        #region - Properties -

        public Coordinate[] Shell1 { get { return new Coordinate[] { new Coordinate(93.885114484466612, 31.812138317385688), new Coordinate(93.884972195141017, 31.812155772466213), new Coordinate(93.884392276406288, 31.812241964973509), new Coordinate(93.884444979485124, 31.812898738542572), new Coordinate(93.884472389705479, 31.812942813150585), new Coordinate(93.8846825840883, 31.812888431828469), new Coordinate(93.88493181951344, 31.812823949381709), new Coordinate(93.88501817593351, 31.812801606953144), new Coordinate(93.8857997385785, 31.812796169891953), new Coordinate(93.885812157765031, 31.812561694765463), new Coordinate(93.8852611919865, 31.81265026750043), new Coordinate(93.885114484466612, 31.812138317385688) }; } }
        public Coordinate[] Shell2 { get { return new Coordinate[] { new Coordinate(94.389225459657609, 31.654675849713385), new Coordinate(94.389609588310122, 31.65660687815398), new Coordinate(94.389765060506761, 31.656652066856623), new Coordinate(94.389976977370679, 31.656641487963498), new Coordinate(94.3901298455894, 31.656582319177687), new Coordinate(94.3903768165037, 31.656410687603056), new Coordinate(94.39074569940567, 31.656188192777336), new Coordinate(94.391203574836254, 31.655944447033107), new Coordinate(94.391517229378223, 31.655866280198097), new Coordinate(94.391591422259808, 31.655739731155336), new Coordinate(94.391874678432941, 31.655362978577614), new Coordinate(94.389225459657609, 31.654675849713385) }; } }

        public Coordinate[] Hole1 { get { return new Coordinate[] { new Coordinate(94.391250583343208, 31.65536673553288), new Coordinate(94.391314106062055, 31.65545338857919), new Coordinate(94.391487306915224, 31.655692549422383), new Coordinate(94.391419006511569, 31.655728756450117), new Coordinate(94.391350706107914, 31.655764964409173), new Coordinate(94.391113765537739, 31.655437715351582), new Coordinate(94.391140352003276, 31.655423603951931), new Coordinate(94.391182026825845, 31.655401514843106), new Coordinate(94.391250583343208, 31.65536673553288) }; } }
        public Coordinate[] Hole2 { get { return new Coordinate[] { new Coordinate(94.391045465134084, 31.655473922379315), new Coordinate(94.391282437369227, 31.655801171436906), new Coordinate(94.391214136965573, 31.655837378464639), new Coordinate(94.39097719732672, 31.655510129407048), new Coordinate(94.391045465134084, 31.655473922379315) }; } }
        public Coordinate[] Hole3 { get { return new Coordinate[] { new Coordinate(94.390908895991743, 31.655546309426427), new Coordinate(94.391145868226886, 31.655873557552695), new Coordinate(94.391077567823231, 31.655909764580429), new Coordinate(94.391009266488254, 31.655945972539485), new Coordinate(94.3907723268494, 31.655618722550571), new Coordinate(94.390840595588088, 31.65558251645416), new Coordinate(94.390908895991743, 31.655546309426427) }; } }
        public Coordinate[] Hole4 { get { return new Coordinate[] { new Coordinate(94.390704026445746, 31.655654929578304), new Coordinate(94.390940997749567, 31.655982178635895), new Coordinate(94.39087269641459, 31.656018358655274), new Coordinate(94.3908044276759, 31.656054565683007), new Coordinate(94.390736126340926, 31.656090772710741), new Coordinate(94.390667825937271, 31.656126979738474), new Coordinate(94.390430886298418, 31.65579972974956), new Coordinate(94.390499155968428, 31.655763522721827), new Coordinate(94.390567456372082, 31.655727315694094), new Coordinate(94.390635725110769, 31.655691109597683), new Coordinate(94.390704026445746, 31.655654929578304) }; } }

        #endregion

        #region - Ctor -

        public GeometryTest(int srid = 0)
        {
            _factory = new GeometryFactory(new PrecisionModel(), srid);
        }

        #endregion

        private SqlGeometryBuilder CreateBuilder()
        {
            SqlGeometryBuilder bldr = new SqlGeometryBuilder();
            bldr.SetSrid(_factory.Srid);

            return bldr;
        }

        private void AddFigure(SqlGeometryBuilder bldr, params Coordinate[] coords)
        {
            bldr.BeginFigure(coords[0].X, coords[0].Y);
            for (int i = 1; i < coords.Length; i++)
                bldr.AddLine(coords[i].X, coords[i].Y);
            bldr.EndFigure();
        }

        public IPoint CreatePoint(double x, double y)
        {
            return _factory.CreatePoint(new Coordinate { X = x, Y = y });
        }

        public SqlGeometry CreateSqlPoint(double x, double y)
        {
            SqlGeometryBuilder bldr = CreateBuilder();

            bldr.BeginGeometry(OpenGisGeometryType.Point);
            bldr.BeginFigure(x, y);
            bldr.EndFigure();
            bldr.EndGeometry();

            return bldr.ConstructedGeometry;
        }

        public IPolygon CreatePolygon(IList<Coordinate> shell, IList<Coordinate>[] holes = null)
        {
            ILinearRing[] geomHoles = new ILinearRing[holes == null ? 0 : holes.Length];

            for(int i = 0; i < geomHoles.Length; i++)
                geomHoles[i] = _factory.CreateLinearRing(holes[i]);

            return _factory.CreatePolygon(_factory.CreateLinearRing(shell), geomHoles);
        }

        public SqlGeometry CreateSqlLineString(Coordinate[] coords)
        {
            SqlGeometryBuilder bldr = CreateBuilder();

            bldr.BeginGeometry(OpenGisGeometryType.LineString);

            AddFigure(bldr, coords);

            bldr.EndGeometry();

            return bldr.ConstructedGeometry;
        }

        public SqlGeometry CreatePolygon(Coordinate[] shell, params Coordinate[][] holes)
        {
            SqlGeometryBuilder bldr = CreateBuilder();

            bldr.BeginGeometry(OpenGisGeometryType.Polygon);

            AddFigure(bldr, shell);

            foreach (Coordinate[] hole in holes)
                AddFigure(bldr, hole);

            bldr.EndGeometry();

            return bldr.ConstructedGeometry;
        }

        public SqlGeometry CreateMultiPoint(params Coordinate[] coords)
        {
            SqlGeometryBuilder bldr = CreateBuilder();

            bldr.BeginGeometry(OpenGisGeometryType.MultiPoint);

            foreach (Coordinate coord in coords)
            {
                bldr.BeginGeometry(OpenGisGeometryType.Point);
                AddFigure(bldr, coord);
                bldr.EndGeometry();
            }

            bldr.EndGeometry();

            return bldr.ConstructedGeometry;
        }
    }
}
