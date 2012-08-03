using System;
using System.IO;

using FastShapefile.Shape;

namespace FastShapefile.Tasks
{
    public class ValidateShapefile
    {
        #region - Static Methods -

        public static void ValidateIndex(string shapefilePath)
        {
            string indexPath = Path.ChangeExtension(shapefilePath, ".shx");

            int posShouldBe = 50, recordNum = 1;
            using (BinaryReader shape = new BinaryReader(new FileStream(shapefilePath, FileMode.Open)))
            using (BinaryReader index = new BinaryReader(new FileStream(indexPath, FileMode.Open)))
            {
                ShapefileHeader shapeHeader = ShapefileHeader.Read(shape);
                ShapefileHeader indexHeader = ShapefileHeader.Read(index);

                if (shapeHeader.FileLength * 2 != new FileInfo(shapefilePath).Length)
                    Console.WriteLine("length");

                if (indexHeader.FileLength * 2 != new FileInfo(indexPath).Length)
                    Console.WriteLine("length");

                while (index.BaseStream.Position < index.BaseStream.Length)
                {
                    int filePos = index.ReadInt32BE();

                    if (filePos != posShouldBe)
                        Console.WriteLine("Ding");

                    int indexContentLength = index.ReadInt32BE();

                    shape.BaseStream.Seek(filePos * 2, SeekOrigin.Begin);

                    int shapeRecordNumber = shape.ReadInt32BE();
                    int shapeContentLength = shape.ReadInt32BE();

                    if (shapeRecordNumber != recordNum++ || shapeContentLength != indexContentLength)
                        Console.WriteLine("Ding");

                    posShouldBe += indexContentLength + 4;
                }
            }
        }

        #endregion
    }
}
