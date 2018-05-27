using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using DSVector = Autodesk.DesignScript.Geometry.Vector;
using DSPoint = Autodesk.DesignScript.Geometry.Point;
using DSLine = Autodesk.DesignScript.Geometry.Line;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;
using Graphical.Geometry;
using GraphicalDynamo.Graphs;

namespace GraphicalDynamo.Geometry
{
    
    /// <summary>
    /// Static class extending Point functionality
    /// </summary>
    public static class Curve
    {
        #region Public Methods
        [MultiReturn(new[] { "polygons", "polyCurves", "lines" })]
        public static Dictionary<string, object> BuildPolygons(List<Line> lines)
        {
            if(lines == null) { throw new ArgumentNullException("lines"); }
            if(lines.Count < 2) { throw new ArgumentException("Needs 2 or more lines", "lines"); }

            Graph g = Graph.ByLines(lines);
            g.graph.BuildPolygons();

            var gPolygons = g.graph.Polygons;
            List<Polygon> dsPolygons = new List<Polygon>();
            List<PolyCurve> polyCurves = new List<PolyCurve>();
            List<Line> dsLines = new List<DSLine>();
            List<gEdge> polygonEdges = new List<gEdge>();
            foreach(gPolygon gP in gPolygons)
            {
                var points = gP.Vertices.Select(v => DSPoint.ByCoordinates(v.X, v.Y, v.Z)).ToList();
                if (gP.IsClosed)
                {
                    dsPolygons.Add(Polygon.ByPoints(points));
                }
                else if(gP.Edges.Count > 1)
                {
                    polyCurves.Add(PolyCurve.ByPoints(points));
                }
                else
                {
                    DSPoint start = Point.ToPoint(gP.Edges.First().StartVertex);
                    DSPoint end = Point.ToPoint(gP.Edges.First().EndVertex);
                    dsLines.Add(DSLine.ByStartPointEndPoint(start, end));
                }
            }

            return new Dictionary<string, object>()
            {
                {"polygons", dsPolygons },
                {"polyCurves", polyCurves },
                {"lines",  dsLines}
            };
        }

        public static bool PolygonContainsPoint(Polygon polygon, DSPoint point)
        {
            gVertex vertex = Point.ToVertex(point);
            var vertices = polygon.Points.Select(p => Point.ToVertex(p)).ToList();
            gPolygon gPolygon = gPolygon.ByVertices(vertices, false);

            return gPolygon.ContainsVertex(vertex);
        }

        internal static bool DoesIntersect(Line line1, Line line2)
        {
            gEdge edge1 = gEdge.ByStartVertexEndVertex(Point.ToVertex(line1.StartPoint), Point.ToVertex(line1.EndPoint));
            gEdge edge2 = gEdge.ByStartVertexEndVertex(Point.ToVertex(line2.StartPoint), Point.ToVertex(line2.EndPoint));

            if (edge1.Intersects(edge2))
            {
                if (edge2.StartVertex.OnEdge(edge1)) { return false; }
                if (edge2.EndVertex.OnEdge(edge1)) { return false; }
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool DoesIntersect(Line line, DSPoint point)
        {
            gEdge edge = gEdge.ByStartVertexEndVertex(Point.ToVertex(line.StartPoint), Point.ToVertex(line.EndPoint));
            gVertex vertex = Point.ToVertex(point);

            return vertex.OnEdge(edge);
        }
        #endregion
    }
}
