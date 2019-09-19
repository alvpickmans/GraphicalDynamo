using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using DS = Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;
using Graphical.Geometry;
using Graphical.Extensions;
using GraphicalDynamo.Graphs;

namespace GraphicalDynamo.Geometry
{
    
    /// <summary>
    /// Static class extending Point functionality
    /// </summary>
    public static class Curves
    {
        #region Internal Methods

        internal static Edge ToEdge(this DS.Line line)
        {
            return Edge.ByStartVertexEndVertex(line.StartPoint.ToVertex(), line.EndPoint.ToVertex());
        }

        internal static DS.Line ToLine(this Edge edge){
            return DS.Line.ByStartPointEndPoint(edge.StartVertex.ToPoint(), edge.EndVertex.ToPoint());
        }

        internal static Dictionary<Vertex,List<DS.Curve>> CurvesDependency(List<DS.Curve> curves)
        {
            Dictionary<Vertex, List<DS.Curve>> graph = new Dictionary<Vertex, List<DS.Curve>>();

            foreach (DS.Curve curve in curves)
            {
                Vertex start = Points.ToVertex(curve.StartPoint);
                Vertex end = Points.ToVertex(curve.EndPoint);
                List<DS.Curve> startList = new List<DS.Curve>();
                List<DS.Curve> endList = new List<DS.Curve>();
                if (graph.TryGetValue(start, out startList))
                {
                    startList.Add(curve);
                }
                else
                {
                    graph.Add(start, new List<DS.Curve>() { curve });
                }

                if (graph.TryGetValue(end, out endList))
                {
                    endList.Add(curve);
                }
                else
                {
                    graph.Add(end, new List<DS.Curve>() { curve });
                }
            }
            return graph;
        }

        internal static bool PolygonContainsPoint(DS.Polygon polygon, DS.Point point)
        {
            Vertex vertex = Points.ToVertex(point);
            var vertices = polygon.Points.Select(p => Points.ToVertex(p)).ToList();
            Polygon Polygon = Polygon.ByVertices(vertices, false);

            return Polygon.ContainsVertex(vertex);
        }

        internal static bool DoesIntersect(DS.Line line1, DS.Line line2)
        {
            Edge edge1 = Edge.ByStartVertexEndVertex(Points.ToVertex(line1.StartPoint), Points.ToVertex(line1.EndPoint));
            Edge edge2 = Edge.ByStartVertexEndVertex(Points.ToVertex(line2.StartPoint), Points.ToVertex(line2.EndPoint));

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

        internal static bool DoesIntersect(DS.Line line, DS.Point point)
        {
            Edge edge = Edge.ByStartVertexEndVertex(Points.ToVertex(line.StartPoint), Points.ToVertex(line.EndPoint));
            Vertex vertex = Points.ToVertex(point);

            return vertex.OnEdge(edge);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates polygons from a list of lines. Lines are returned as ungrouped if not connected or
        /// not forming a closed polygon.
        /// </summary>
        /// <param name="lines">List of connected lines</param>
        /// <returns name="polygons">Polygons created from connected lines</returns>
        /// <returns name="ungrouped">Lines not forming a closed polygon</returns>
        [MultiReturn(new[] { "polygons", "ungrouped" })]
        public static Dictionary<string, object> BuildPolygons(List<DS.Line> lines)
        {
            if (lines == null) { throw new ArgumentNullException("lines"); }
            if (lines.Count < 2) { throw new ArgumentException("Needs 2 or more lines", "lines"); }

            BaseGraph g = BaseGraph.ByLines(lines);
            g.graph.BuildPolygons();

            var Polygons = g.graph.Polygons;
            List<DS.Polygon> dsPolygons = new List<DS.Polygon>();
            List<DS.Line> dsLines = new List<DS.Line>();
            List<Edge> polygonEdges = new List<Edge>();
            foreach (Polygon gP in Polygons)
            {
                var points = gP.Vertices.Select(v => DS.Point.ByCoordinates(v.X, v.Y, v.Z)).ToList();
                if (gP.IsClosed)
                {
                    dsPolygons.Add(DS.Polygon.ByPoints(points));
                }
                else if (gP.Edges.Count > 1)
                {
                    foreach(Edge edge in gP.Edges)
                    {
                        DS.Point start = Points.ToPoint(edge.StartVertex);
                        DS.Point end = Points.ToPoint(edge.EndVertex);
                        dsLines.Add(DS.Line.ByStartPointEndPoint(start, end));
                    }
                }
                else
                {
                    DS.Point start = Points.ToPoint(gP.Edges.First().StartVertex);
                    DS.Point end = Points.ToPoint(gP.Edges.First().EndVertex);
                    dsLines.Add(DS.Line.ByStartPointEndPoint(start, end));
                }
            }

            return new Dictionary<string, object>()
            {
                {"polygons", dsPolygons },
                {"ungrouped",  dsLines}
            };
        }

        /// <summary>
        /// Groups connected curves into polycurves. Curves are returned as ungrouped if not connected to any other curve.
        /// </summary>
        /// <param name="curves">Curves to group</param>
        /// <returns name="polycurves">Polycurves grouped</returns>
        /// <returns name="ungrouped">Lines not grouped</returns>
        [MultiReturn(new[] { "polycurves", "ungrouped" })]
        public static Dictionary<string, object> GroupCurves(List<DS.Curve> curves)
        {
            if(curves == null) { throw new ArgumentNullException("lines"); }
            if(curves.Count < 2) { throw new ArgumentException("Needs 2 or more lines", "lines"); }

            Dictionary<Vertex, List<DS.Curve>> graph = CurvesDependency(curves);
            Dictionary<int, List<DS.Curve>> grouped = new Dictionary<int, List<DS.Curve>>();
            Dictionary<Vertex, int> vertices = new Dictionary<Vertex, int>();

            foreach(Vertex v in graph.Keys)
            {

                // If already belongs to a polygon or is not a polygon vertex or already computed
                if (vertices.ContainsKey(v)|| graph[v].Count > 2) { continue; }

                // grouped.Count() translates to the number of different groups created
                vertices.Add(v, grouped.Count());
                grouped.Add(vertices[v], new List<DS.Curve>());

                foreach(DS.Curve curve in graph[v])
                {
                    var startVertex = Points.ToVertex(curve.StartPoint);
                    var endVertex = Points.ToVertex(curve.EndPoint);
                    DS.Curve nextCurve = curve;
                    Vertex nextVertex = (startVertex.Equals(v)) ? endVertex : startVertex;
                
                    while(!vertices.ContainsKey(nextVertex))
                    {
                        vertices.Add(nextVertex, vertices[v]);
                        grouped[vertices[v]].Add(nextCurve);

                        // Next vertex doesn't have any other curve connected.
                        if(graph[nextVertex].Count < 2) { break; }

                        nextCurve = graph[nextVertex].Where(c => !c.Equals(nextCurve)).First();
                        startVertex = Points.ToVertex(nextCurve.StartPoint);
                        endVertex = Points.ToVertex(nextCurve.EndPoint);
                        nextVertex = (startVertex.Equals(nextVertex)) ? endVertex : startVertex;

                    }
                    if (!grouped[vertices[v]].Last().Equals(nextCurve))
                    {
                        grouped[vertices[v]].Add(nextCurve);
                    }

                }

            }
            
            List<DS.PolyCurve> polyCurves = new List<DS.PolyCurve>();
            List<DS.Curve> ungrouped = new List<DS.Curve>();
            foreach(var group in grouped.Values)
            {
                if(group.Count > 1)
                {
                    polyCurves.Add(DS.PolyCurve.ByJoinedCurves(group));
                }
                else
                {
                    ungrouped.Add(group.First());
                }
            }

            return new Dictionary<string, object>()
            {
                {"polycurves", polyCurves },
                {"ungrouped",  ungrouped}
            };
        }

        /// <summary>
        /// Creates a simplified version of the curve by creating lines with a maximum length defined.
        /// </summary>
        /// <param name="curve">Curve to polygonize</param>
        /// <param name="maxLength">Maximum length of subdivisions</param>
        /// <param name="asPolycurve">If true returns a Polycurve or a list of lines otherwise.</param>
        /// <returns></returns>
        public static object Polygonize(DS.Curve curve, double maxLength, bool asPolycurve = false)
        {
            //TODO : Look into http://www.antigrain.com/research/adaptive_bezier/index.html
            if (curve == null) { throw new ArgumentNullException("curve"); }
            List<DS.Curve> lines = new List<DS.Curve>();
            bool isStraight = curve.Length.AlmostEqualTo(curve.StartPoint.DistanceTo(curve.EndPoint));
            if (isStraight)
            {
                lines.Add(curve);
            }
            else
            {
                int divisions = (int)Math.Ceiling(curve.Length / maxLength);
                if(divisions > 1)
                {
                    var points = curve.PointsAtEqualSegmentLength(divisions);
                    lines.Add(DS.Line.ByStartPointEndPoint(curve.StartPoint, points.First()));
                    for (var i = 0; i < points.Count() - 1; i++)
                    {
                        lines.Add(DS.Line.ByStartPointEndPoint(points[i], points[i + 1]));
                    }
                    lines.Add(DS.Line.ByStartPointEndPoint(points.Last(), curve.EndPoint));
                }
                else
                {
                    lines.Add(DS.Line.ByStartPointEndPoint(curve.StartPoint, curve.EndPoint));
                }
            }

            if (asPolycurve)
            {
                return DS.PolyCurve.ByJoinedCurves(lines);
            }
            else
            {
                return lines;
            }

        }

        #endregion
    }
}
