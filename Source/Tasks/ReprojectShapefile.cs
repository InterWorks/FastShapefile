using System;
using System.IO;
using System.Threading.Tasks;

using FastShapefile.Dbf;
using FastShapefile.Shape;
using FastShapefile.Transform;

using DotSpatial.Projections;
using DotSpatial.Topology;

namespace FastShapefile.Tasks
{
    public class ReprojectShapefile
    {
        public string Source { get; set; }
        public string DestinationName { get; set; }
        public string Label { get; set; }

        public static void Reproject(string destinationFolder, string projection, params ReprojectShapefile[] shapes)
        {
            ProjectionInfo targetProjection = ProjectionInfo.FromEsriString(projection);

            foreach (ReprojectShapefile shape in shapes)
            {
                string shapePath = Path.Combine(destinationFolder, shape.DestinationName);

                ShapefileHeader shapeHeader = ShapefileHeader.CreateEmpty(ShapefileGeometryType.Polygon);
                DbfHeader dbfHeader = new DbfHeader();
                dbfHeader.AddCharacter("Label", 80);
                GeometryFactory gf = new GeometryFactory();

                using (ShapefileDataWriter writer = ShapefileDataWriter.Create(shapePath + ".shp", dbfHeader, shapeHeader))
                {
                    GeometryTransform transform = null;
                    if (File.Exists(Path.ChangeExtension(shape.Source, ".prj")))
                    {
                        transform = GeometryTransform.GetTransform(shape.Source, projection);

                        if (transform != null)
                            File.WriteAllText(shapePath + ".prj", projection);
                        else
                            File.Copy(Path.ChangeExtension(shape.Source, ".prj"), shapePath + ".prj");
                    }

                    using (ShapefileIndexReader index = new ShapefileIndexReader(Path.ChangeExtension(shape.Source, ".shx")))
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
                                using (ShapefileBlockReader reader = new ShapefileBlockReader(shape.Source, index, gf, transform))
                                {
                                    while (reader.Read())
                                        writer.Write(reader.Geometry, reader.Record.GetString(shape.Label));
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
