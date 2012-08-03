using System;
using System.Data;
using System.Data.SqlClient;

using FastShapefile.Dbf;
using FastShapefile.Shape;

using DotSpatial.Topology;
using Microsoft.SqlServer.Types;

namespace FastShapefile.Conversion
{
    public static class Sql2Shape
    {
        public static void Convert(ShapefileGeometryType type, SqlDataReader reader, string shapefile)
        {
            if (!reader.Read())
                throw new Exception("No Results found");

            int geomOrdinal, colCount;
            ShapefileHeader shapeHeader = ShapefileHeader.CreateEmpty(type);
            DbfHeader dbfHeader = BuildHeader(reader, out geomOrdinal, out colCount);
            GeometryFactory factory = new GeometryFactory();
            Envelope env = shapeHeader.Bounds;

            using (ShapefileDataWriter writer = ShapefileDataWriter.Create(shapefile, dbfHeader, shapeHeader))
            {
                do
                {
                    SqlGeometry geom = reader[geomOrdinal] as SqlGeometry;
                    if (!geom.STIsValid())
                        geom = geom.MakeValid();

                    for (int i = 0, offset = 0; i < colCount; i++)
                    {
                        if (i == geomOrdinal)
                            offset++;

                        writer.Record.SetRaw(i, reader[i + offset]);
                    }

                    ExpandEnv(env, geom.STBoundary());
                    writer.Write(ConvertToGeometry.SqlGeometryToGeometry(geom, factory));
                }
                while (reader.Read());
            }
        }

        private static DbfHeader BuildHeader(SqlDataReader reader, out int geomOrdinal, out int colCount)
        {
            DbfHeader dbfHeader = new DbfHeader();
            DataTable schema = reader.GetSchemaTable();
            geomOrdinal = -1;
            colCount = schema.Rows.Count - 1;

            foreach (DataRow row in schema.Rows)
            {
                int oridinal = (int)row["ColumnOrdinal"];
                int size = (int)row["ColumnSize"];
                string name = row["ColumnName"] as string;

                switch ((row["DataType"] as Type).Name)
                {
                    case "String":
                        dbfHeader.AddCharacter(name, (byte)size);
                        break;
                    case "SqlGeometry":
                        geomOrdinal = oridinal;
                        break;
                    default:
                        throw new Exception(String.Format("'{0}' is not a recognized data type", (row["DataType"] as Type).Name));
                }
            }

            if (geomOrdinal == -1)
                throw new Exception("Geometry column was not found");

            return dbfHeader;
        }

        private static void ExpandEnv(Envelope env, SqlGeometry geom)
        {
            for (int i = 0, c = geom.STNumPoints().Value; i < c; i++)
            {
                SqlGeometry geomPoint = geom.STPointN(i + 1);

                env.ExpandToInclude(geomPoint.STX.Value, geomPoint.STY.Value);
            }
        }
    }
}
