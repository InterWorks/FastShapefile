using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

using FastShapefile.Dbf;
using FastShapefile.Shape;
using FastShapefile.Transform;

using DotSpatial.Topology;

namespace FastShapefile.Conversion
{
    public class Shape2Sql
    {
        public static void Convert(string shapefile, string connectionString, string tableName, int srid = 0, string targetProjectionWKT = null)
        {
            GeometryTransform transform = GeometryTransform.GetTransform(shapefile, targetProjectionWKT);
            GeometryFactory factory = new GeometryFactory(new PrecisionModel(), srid);

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlBulkCopy copy = new SqlBulkCopy(conn))
            using (ShapefileDataReader reader = new ShapefileDataReader(shapefile, factory, transform))
            {
                conn.Open();

                string createTableSql = GenerateCreateTableQuery(reader, tableName);
                using (SqlCommand createTableComm = new SqlCommand(createTableSql, conn))
                    createTableComm.ExecuteNonQuery();

                copy.SqlRowsCopied += (object sender, SqlRowsCopiedEventArgs e) =>
                    {
                        System.Console.Clear();
                        System.Console.WriteLine("Copied " + e.RowsCopied);
                    };
                copy.NotifyAfter = 257;
                copy.DestinationTableName = tableName;
                copy.WriteToServer(new ShapefileBulkSqlReader(reader, srid));
            }
        }

        private static string GenerateCreateTableQuery(ShapefileDataReader reader, string tableName)
        {
            StringBuilder bldr = new StringBuilder();
            bldr.Append("CREATE TABLE [");
            bldr.Append(tableName);
            bldr.Append("] ([Id_");
            bldr.Append(tableName);
            bldr.Append("] [int] IDENTITY(1,1) NOT NULL, [Geom] [geometry] NOT NULL, ");

            DbfHeader header = reader.DbfHeader;
            for (int i = 0; i < header.Count; i++)
            {
                DbfColumn col = header[i];

                bldr.Append('[');
                bldr.Append(col.Name);
                bldr.Append("] ");

                switch (col.Type)
                {
                    case DbfColumnType.Character:
                        bldr.Append("[varchar](");
                        bldr.Append(col.Length);
                        bldr.Append(")");
                        break;
                    case DbfColumnType.Float:
                    case DbfColumnType.Number:
                        bldr.Append("[float]");
                        break;
                    case DbfColumnType.Boolean:
                        bldr.Append("[bit]");
                        break;
                    case DbfColumnType.Date:
                        bldr.Append("[datetime]");
                        break;
                    default:
                        throw new Exception(String.Format("Column type '{0}' is not supported.", (char)col.Type));
                }

                bldr.Append(" NULL, ");
            }

            bldr.Append("CONSTRAINT [PK_");
            bldr.Append(tableName);
            bldr.Append("] PRIMARY KEY CLUSTERED ([Id_");
            bldr.Append(tableName);
            bldr.Append("] ASC) WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON))");

            return bldr.ToString();
        }

        #region - Private ShapefileBulkSqlReader -

        private class ShapefileBulkSqlReader : IDataReader
        {
            #region - Fields -

            private ShapefileDataReader _reader;
            private int _srid;

            #endregion

            #region - Ctor -

            public ShapefileBulkSqlReader(ShapefileDataReader reader, int srid)
            {
                _reader = reader;
                _srid = srid;
            }

            #endregion

            #region - Properties -

            public int Depth
            {
                get { return 1; }
            }

            public bool IsClosed
            {
                get { return false; }
            }

            public int RecordsAffected
            {
                get { return -1; }
            }

            public int FieldCount
            {
                get { return _reader.DbfHeader.Count + 2; }
            }

            #endregion

            #region - Methods -

            public bool NextResult()
            {
                return _reader.Read();
            }

            public bool Read()
            {
                return _reader.Read();
            }

            public object GetValue(int i)
            {
                if (i == 0)
                    return _reader.RecordId;
                else if (i == 1)
                    return ConvertToSqlGeometry.GeometryToSqlGeometry(_reader.Geometry, _srid);
                return _reader.Record.GetValue(i - 2);
            }

            public void Dispose() { }

            public void Close() { }

            #endregion

            #region - NotImplemented -

            public DataTable GetSchemaTable()
            {
                throw new NotImplementedException();
            }

            public bool GetBoolean(int i)
            {
                throw new NotImplementedException();
            }

            public byte GetByte(int i)
            {
                throw new NotImplementedException();
            }

            public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
            {
                throw new NotImplementedException();
            }

            public char GetChar(int i)
            {
                throw new NotImplementedException();
            }

            public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
            {
                throw new NotImplementedException();
            }

            public IDataReader GetData(int i)
            {
                throw new NotImplementedException();
            }

            public string GetDataTypeName(int i)
            {
                throw new NotImplementedException();
            }

            public DateTime GetDateTime(int i)
            {
                throw new NotImplementedException();
            }

            public decimal GetDecimal(int i)
            {
                throw new NotImplementedException();
            }

            public double GetDouble(int i)
            {
                throw new NotImplementedException();
            }

            public Type GetFieldType(int i)
            {
                throw new NotImplementedException();
            }

            public float GetFloat(int i)
            {
                throw new NotImplementedException();
            }

            public Guid GetGuid(int i)
            {
                throw new NotImplementedException();
            }

            public short GetInt16(int i)
            {
                throw new NotImplementedException();
            }

            public int GetInt32(int i)
            {
                throw new NotImplementedException();
            }

            public long GetInt64(int i)
            {
                throw new NotImplementedException();
            }

            public string GetName(int i)
            {
                throw new NotImplementedException();
            }

            public int GetOrdinal(string name)
            {
                throw new NotImplementedException();
            }

            public string GetString(int i)
            {
                throw new NotImplementedException();
            }

            public int GetValues(object[] values)
            {
                throw new NotImplementedException();
            }

            public bool IsDBNull(int i)
            {
                throw new NotImplementedException();
            }

            public object this[string name]
            {
                get { throw new NotImplementedException(); }
            }

            public object this[int i]
            {
                get { throw new NotImplementedException(); }
            }

            #endregion
        }

        #endregion
    }
}
