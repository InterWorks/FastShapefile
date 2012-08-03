using System;
using System.IO;

using DotSpatial.Topology;

namespace FastShapefile.Shape
{
    public class ShapefileHeader
    {
        #region - Static Methods -

        public static ShapefileHeader CreateEmpty(ShapefileGeometryType type)
        {
            return new ShapefileHeader
            {
                Bounds = new Envelope(),
                ShapeType = type,
                FileLength = 0
            };
        }

        public static ShapefileHeader Read(BinaryReader reader)
        {
            ShapefileHeader header = new ShapefileHeader();

            int fileType = reader.ReadInt32BE();
            if (fileType != 9994)
                throw new Exception("The first four bytes of this file indicate this is not a shape file.");

            // skip 5 unsed bytes
            reader.BaseStream.Seek(20L, SeekOrigin.Current);

            header.FileLength = reader.ReadInt32BE();

            int version = reader.ReadInt32();
            if (version != 1000)
                throw new Exception("Shapefile is not the proper version");

            header.ShapeType = (ShapefileGeometryType)reader.ReadInt32();

            double[] coords = new double[4];
            for (int i = 0; i < 4; i++)
                coords[i] = reader.ReadDouble();
            header.Bounds = new Envelope(coords[0], coords[2], coords[1], coords[3]);

            // Skip to end of header
            reader.BaseStream.Seek(100L, SeekOrigin.Begin);

            return header;
        }

        #endregion

        #region - Methods -

        public ShapefileHeader Clone()
        {
            ShapefileHeader header = ShapefileHeader.CreateEmpty(this.ShapeType);
            header.Bounds = this.Bounds.Copy();

            return header;
        }

        #endregion

        #region - Properties -

        public int FileLength { get; set; }
        public ShapefileGeometryType ShapeType { get; set; }
        public Envelope Bounds { get; set; }

        #endregion

        #region - Ctor -

        private ShapefileHeader()
        {
        }

        #endregion
    }
}
