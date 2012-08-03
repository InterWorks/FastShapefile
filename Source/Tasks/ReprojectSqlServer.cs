using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DotSpatial.Projections;
using Microsoft.SqlServer.Types;
using System.Data.SqlClient;
using System.Data;

namespace FastShapefile.Tasks
{
    public class ReprojectSqlServer
    {
        private ProjectionInfo _proj_source;
        private ProjectionInfo _proj_target;
        private int _srid_source;
        private int _srid_target;

        public ReprojectSqlServer(ProjectionInfo source, int sourceSRID, ProjectionInfo target, int targetSRID)
        {
            _proj_source = source;
            _proj_target = target;
            _srid_source = sourceSRID;
            _srid_target = targetSRID;
        }

        public void Reproject(SqlConnectionStringBuilder connBldr, string table, string idColumnName, params string[] reprojectColumns)
        {
            int count = 0, geomIdx = -1;

            string selectSQL = CreateSELECT(table, idColumnName, reprojectColumns);
            string updateSQL = CreateUPDATE(table, idColumnName, reprojectColumns);

            using (SqlConnection readerConn = new SqlConnection(connBldr.ConnectionString))
            using (SqlConnection updateConn = new SqlConnection(connBldr.ConnectionString))
            using (SqlCommand readerComm = new SqlCommand(selectSQL, readerConn))
            {
                readerConn.Open();
                updateConn.Open();

                using (SqlDataReader reader = readerComm.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (CheckForSkip(reader, ref geomIdx))
                            continue;

                        using (SqlCommand updateComm = new SqlCommand(updateSQL, updateConn))
                        {
                            ReprojectObjects(reader, updateComm);

                            updateComm.Parameters.AddWithValue("idValue", reader[0]);
                            updateComm.ExecuteNonQuery();
                        }

                        if (++count % 7 == 0)
                        {
                            System.Console.Clear();
                            System.Console.WriteLine(connBldr.InitialCatalog + " - " + count);
                        }
                    }
                }
            }
        }

        private string CreateSELECT(string table, string idColumnName, params string[] reprojectColumns)
        {
            StringBuilder statement = new StringBuilder();
            statement.Append("SELECT [");
            statement.Append(idColumnName);
            statement.Append("]");

            // Iterate geoms
            for (int i = 0; i < reprojectColumns.Length; i++)
            {
                statement.Append(", [");
                statement.Append(reprojectColumns[i]);
                statement.Append(']');
            }
            
            statement.Append(" FROM [");
            statement.Append(table);
            statement.Append("] WITH(NOLOCK)");

            return statement.ToString();
        }

        private string CreateUPDATE(string table, string idColumnName, params string[] reprojectColumns)
        {
            StringBuilder statement = new StringBuilder();
            statement.Append("UPDATE [");
            statement.Append(table);
            statement.Append("] SET ");

            // Iterate geoms
            for(int i = 0; i < reprojectColumns.Length; i++)
            {
                statement.Append('[');
                statement.Append(reprojectColumns[i]);
                statement.Append("] = @geo");
                statement.Append(i + 1);

                if (i != reprojectColumns.Length - 1)
                    statement.Append(", ");
            }

            statement.Append(" WHERE [");
            statement.Append(idColumnName);
            statement.Append("] = @idValue");

            return statement.ToString();
        }

        private void ReprojectObjects(SqlDataReader reader, SqlCommand command)
        {
            for(int i = 1, c = reader.FieldCount; i < c; i++)
            {
                object value = reader[i];

                if(value is SqlGeometry)
                {
                    SqlGeometry geom = value as SqlGeometry;
                    SqlGeometryBuilder bldr = new SqlGeometryBuilder();
                    bldr.SetSrid(_srid_target);

                    if (geom.STIsValid())
                        ReprojectGeometry(geom, bldr);
                    else
                        ReprojectGeometry(geom.MakeValid(), bldr);

                    command.Parameters.Add(new SqlParameter { ParameterName = "geo" + i, UdtTypeName = "Geometry", SqlDbType = SqlDbType.Udt, Value = bldr.ConstructedGeometry });
                }
                else if(value is double) // a double must come in pairs
                {
                    int x = i, y = ++i;
                    double[] values = new double[] { (double)value, (double)reader[y] };

                    DotSpatial.Projections.Reproject.ReprojectPoints(values, null, _proj_source, _proj_target, 0, 1);

                    command.Parameters.AddWithValue("geo" + x, values[0]);
                    command.Parameters.AddWithValue("geo" + y, values[1]);
                }
            }
        }

        private bool CheckForSkip(SqlDataReader reader, ref int geomIdx)
        {
            SqlGeometry geom = null;
            if (geomIdx == -1)
            {
                for (int i = 0, c = reader.FieldCount; i < c; i++)
                {
                    geom = reader[i] as SqlGeometry;
                    if (geom != null)
                    {
                        geomIdx = i;
                        break;
                    }
                }
            }
            else
                geom = reader[geomIdx] as SqlGeometry;

            if (geom == null || (geom.STSrid != _srid_source && geom.STSrid != 0))
                return true;
            else
                return false;
        }

        private void ReprojectGeometry(SqlGeometry geom, SqlGeometryBuilder bldr)
        {
            if (geom.STIsEmpty())
            {
                bldr.BeginGeometry(OpenGisGeometryType.GeometryCollection);
                bldr.EndGeometry();
            }
            else
            {
                OpenGisGeometryType geometryType = (OpenGisGeometryType)Enum.Parse(typeof(OpenGisGeometryType), geom.STGeometryType().Value);
                switch (geometryType)
                {
                    case OpenGisGeometryType.Point:
                        ReprojectPoint(geom, bldr);
                        break;
                    case OpenGisGeometryType.LineString:
                        ReprojectLineString(geom, bldr);
                        break;
                    case OpenGisGeometryType.Polygon:
                        ReprojectPolygon(geom, bldr);
                        break;
                    case OpenGisGeometryType.MultiPoint:
                        ReprojectMultiPoint(geom, bldr);
                        break;
                    case OpenGisGeometryType.MultiLineString:
                        ReprojectMultiLineString(geom, bldr);
                        break;
                    case OpenGisGeometryType.MultiPolygon:
                        ReprojectMultiPolygon(geom, bldr);
                        break;
                    case OpenGisGeometryType.GeometryCollection:
                        ReprojectGeometryCollection(geom, bldr);
                        break;
                    default:
                        throw new ArgumentException(string.Format("Cannot reproject SqlServer Type '{0}'", geom.STGeometryType()), "geom");
                }
            }
        }

        private void ReprojectGeometryCollection(SqlGeometry geometry, SqlGeometryBuilder bldr)
        {
            bldr.BeginGeometry(OpenGisGeometryType.GeometryCollection);
            for (int i = 1, c = geometry.STNumGeometries().Value; i <= c; i++)
                ReprojectGeometry(geometry.STGeometryN(i), bldr);
            bldr.EndGeometry();
        }

        private void ReprojectMultiPolygon(SqlGeometry geometry, SqlGeometryBuilder bldr)
        {
            bldr.BeginGeometry(OpenGisGeometryType.MultiPolygon);
            for (int i = 1, c = geometry.STNumGeometries().Value; i <= c; i++)
                ReprojectPolygon(geometry.STGeometryN(i), bldr);
            bldr.EndGeometry();
        }

        private void ReprojectMultiLineString(SqlGeometry geometry, SqlGeometryBuilder bldr)
        {
            bldr.BeginGeometry(OpenGisGeometryType.MultiLineString);
            for (int i = 1, c = geometry.STNumGeometries().Value; i <= c; i++)
                ReprojectLineString(geometry.STGeometryN(i), bldr);
            bldr.EndGeometry();
        }

        private void ReprojectMultiPoint(SqlGeometry geometry, SqlGeometryBuilder bldr)
        {
            bldr.BeginGeometry(OpenGisGeometryType.MultiPoint);
            for (int i = 1, c = geometry.STNumGeometries().Value; i <= c; i++)
                ReprojectPoint(geometry.STGeometryN(i), bldr);
            bldr.EndGeometry();
        }

        private void ReprojectPoint(SqlGeometry geometry, SqlGeometryBuilder bldr)
        {
            double[] pts = new double[] { geometry.STX.Value, geometry.STY.Value };

            DotSpatial.Projections.Reproject.ReprojectPoints(pts, null, _proj_source, _proj_target, 0, 1);

            bldr.BeginGeometry(OpenGisGeometryType.Point);
            bldr.BeginFigure(pts[0], pts[1]);
            bldr.EndFigure();
            bldr.EndGeometry();
        }

        private void ReprojectLineString(SqlGeometry geometry, SqlGeometryBuilder bldr)
        {
            bldr.BeginGeometry(OpenGisGeometryType.LineString);
            ReprojectFigure(geometry, bldr);
            bldr.EndGeometry();
        }

        private void ReprojectPolygon(SqlGeometry geometry, SqlGeometryBuilder bldr)
        {
            bldr.BeginGeometry(OpenGisGeometryType.Polygon);

            ReprojectFigure(geometry.STExteriorRing(), bldr);

            if (geometry.STNumInteriorRing() > 0)
            {
                for (int i = 1, c = geometry.STNumInteriorRing().Value; i <= c; i++)
                    ReprojectFigure(geometry.STInteriorRingN(i), bldr);
            }

            bldr.EndGeometry();
        }

        private void ReprojectFigure(SqlGeometry geometry, SqlGeometryBuilder bldr)
        {
            SqlGeometry point;
            int numPoints = geometry.STNumPoints().Value;
            double[] pts = new double[numPoints * 2];

            for (int i = 0, idx = 0; i < numPoints; i++)
            {
                point = geometry.STPointN(i + 1);

                pts[idx++] = point.STX.Value;
                pts[idx++] = point.STY.Value;
            }

            if (numPoints > 0)
            {
                DotSpatial.Projections.Reproject.ReprojectPoints(pts, null, _proj_source, _proj_target, 0, numPoints);

                bldr.BeginFigure(pts[0], pts[1]);

                for (int i = 2; i < pts.Length; i += 2)
                    bldr.AddLine(pts[i], pts[i + 1]);

                bldr.EndFigure();
            }
        }
    }
}
