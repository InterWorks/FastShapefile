using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FastShapefile.Shape
{
    public static class BigEndianExtensions
    {
        public static int ReadInt32BE(this BinaryReader reader)
        {
            byte[] byteArray = new byte[4];
            int iBytesRead = reader.Read(byteArray, 0, 4);

            Array.Reverse(byteArray);
            return BitConverter.ToInt32(byteArray, 0);
        }

        public static double ReadDoubleBE(this BinaryReader reader)
        {
            byte[] byteArray = new byte[8];
            int iBytesRead = reader.Read(byteArray, 0, 8);

            Array.Reverse(byteArray);
            return BitConverter.ToDouble(byteArray, 0);
        }

        public static void WriteBE(this BinaryWriter writer, int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);

            Array.Reverse(bytes, 0, 4);
            writer.Write(bytes);
        }

        public static void WriteBE(this BinaryWriter writer, double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);

            Array.Reverse(bytes, 0, 8);
            writer.Write(bytes);
        }
    }
}
