using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FastShapefile.Shape
{
    /// <summary>
    /// Feature type enumeration
    /// </summary>
    public enum ShapefileGeometryType : int
    {
        /// <summary>
        /// Null Shape
        /// </summary>
        NullShape = 0,

        /// <summary>
        /// Point
        /// </summary>
        Point = 1,

        /// <summary>
        /// PolyLine
        /// </summary>
        PolyLine = 3,

        /// <summary>
        /// Polygon
        /// </summary>
        Polygon = 5,

        /// <summary>
        /// MultiPoint
        /// </summary>
        MultiPoint = 8
    }
}
