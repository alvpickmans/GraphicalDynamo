using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DS = Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;
using Graphical.Geometry;

namespace GraphicalDynamo.Geometry
{
    public static class Polygons
    {
        #region Public Methods

        /// <summary>
        /// Method to check if a polygon contains a point.
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool ContainsPoint(DS.Polygon polygon, DS.Point point)
        {
            Polygon gPol = Polygon.ByVertices(polygon.Points.Select(p => Vertex.ByCoordinates(p.X, p.Y, p.Z)).ToList());
            Vertex vertex = Vertex.ByCoordinates(point.X, point.Y, point.Z);

            return gPol.ContainsVertex(vertex);
        }

        public static bool IsConvex(DS.Polygon polygon)
        {
            Polygon gPol = Polygon.ByVertices(polygon.Points.Select(p => Vertex.ByCoordinates(p.X, p.Y, p.Z)).ToList());

            return gPol.IsConvex();
        }

        public static List<DS.Geometry> Intersection(DS.Polygon polygon, DS.Line line)
        {
            Polygon gPol = Polygon.ByVertices(polygon.Points.Select(p => Vertex.ByCoordinates(p.X, p.Y, p.Z)).ToList());
            Edge edge = Edge.ByStartVertexEndVertex(line.StartPoint.ToVertex(), line.EndPoint.ToVertex());

            List<Graphical.Geometry.Geometry> geometries = gPol.Intersection(edge);

            List<DS.Geometry> intersections = new List<DS.Geometry>();

            foreach (var intersection in geometries)
            {
                if(intersection is Edge interEdge) { intersections.Add(interEdge.ToLine()); }
                else if(intersection is Vertex interVertex) { intersections.Add(interVertex.ToPoint()); }
            }

            return intersections;
        }
        #endregion
    }
}
