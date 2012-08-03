using System;
using System.IO;
using System.Threading.Tasks;

using FastShapefile.Dbf;
using FastShapefile.Shape;
using FastShapefile.Transform;

using DotSpatial.Topology;

namespace FastShapefile.Tasks
{
    public class CombineShapefile
    {
        public string FilePath { get; set; }
        public string Label { get; set; }

        public static void Combine(string finalShape, string projection, params CombineShapefile[] combineShapes)
        {
            DbfHeader dbfHeader = new DbfHeader();
            dbfHeader.AddCharacter("Label", 80);

            ShapefileHeader shapeHeader = ShapefileHeader.CreateEmpty(ShapefileGeometryType.Polygon);
            GeometryFactory gf = new GeometryFactory();

            using (ShapefileDataWriter writer = ShapefileDataWriter.Create(finalShape, dbfHeader, shapeHeader))
            {
                // Write the projection file.
                File.WriteAllText(Path.ChangeExtension(finalShape, ".prj"), projection);

                foreach (CombineShapefile workerShp in combineShapes)
                {
                    GeometryTransform transform = GeometryTransform.GetTransform(workerShp.FilePath, projection);

                    using (ShapefileIndexReader index = new ShapefileIndexReader(Path.ChangeExtension(workerShp.FilePath, ".shx")))
                    {
                        if (transform != null)
                            writer.Header.Bounds.ExpandToInclude(transform.Apply(index.Header.Bounds));
                        else
                            writer.Header.Bounds.ExpandToInclude(index.Header.Bounds);

                        Task[] tasks = new Task[Environment.ProcessorCount];
                        for (int i = 0; i < tasks.Length; i++)
                        {
                            tasks[i] = Task.Factory.StartNew(() =>
                            {
                                using (ShapefileBlockReader reader = new ShapefileBlockReader(workerShp.FilePath, index, gf, transform))
                                {
                                    while (reader.Read())
                                        writer.Write(reader.Geometry, reader.Record.GetString(workerShp.Label));
                                }
                            });
                        }

                        Task.WaitAll(tasks);

                        writer.Flush();
                    }
                }
            }
        }
    }
}
