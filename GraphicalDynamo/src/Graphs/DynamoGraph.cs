#region namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Interfaces;
using Autodesk.DesignScript.Runtime;
using System.Globalization;
using Graphical.Geometry;
using Graphical.Graphs;
using Dynamo.Graph.Nodes;
#endregion

namespace GraphicalDynamo.Graphs
{
    
    /// <summary>
    /// Representation of a Graph.
    /// Graph contains a Dictionary where
    /// </summary>
    public class Graph : IGraphicItem
    {
        #region Internal Properties
        internal Graphical.Graphs.Graph graph { get; private set; }
        #endregion

        #region Public Properties
        [NodeCategory("Query")]
        public bool IsVisibilityGraph() => graph.GetType() == typeof(Graphical.Graphs.VisibilityGraph);

        #endregion

        #region Internal Constructors
        internal Graph() { }

        internal Graph(List<gPolygon> gPolygons)
        {
            graph = new Graphical.Graphs.Graph(gPolygons);
        }
        #endregion

        #region Public Constructors
        /// <summary>
        /// Creates a graph by a set of closed polygons
        /// </summary>
        /// <param name="polygons">Polygons</param>
        /// <returns name="graph">New graph</returns>
        public static Graph ByPolygons(List<Polygon> polygons)
        {
            if (polygons == null) { throw new NullReferenceException("polygons"); }
            List<gPolygon> input = new List<gPolygon>();
            foreach (Polygon pol in polygons)
            {
                var vertices = pol.Points.Select(pt => gVertex.ByCoordinates(pt.X, pt.Y, pt.Z)).ToList();
                gPolygon gPol = gPolygon.ByVertices(vertices, false);
                input.Add(gPol);
            }

            return new Graph(input);
        }

        /// <summary>
        /// Creates a Graph by a set of boundary and internal polygons.
        /// </summary>
        /// <param name="boundaries">Boundary polygons</param>
        /// <param name="internals">Internal polygons</param>
        /// <returns></returns>
        public static Graph ByBoundaryAndInternalPolygons(List<Polygon> boundaries, [DefaultArgument("[]")]List<Polygon> internals)
        {
            if(boundaries == null) { throw new NullReferenceException("boundaryPolygons"); }
            if(internals == null) { throw new NullReferenceException("internalPolygons"); }
            List<gPolygon> input = new List<gPolygon>();
            foreach (Polygon pol in boundaries)
            {
                var vertices = pol.Points.Select(pt => gVertex.ByCoordinates(pt.X, pt.Y, pt.Z)).ToList();
                gPolygon gPol = gPolygon.ByVertices(vertices, true);
                input.Add(gPol);
            }

            foreach (Polygon pol in internals)
            {
                var vertices = pol.Points.Select(pt => gVertex.ByCoordinates(pt.X, pt.Y, pt.Z)).ToList();
                gPolygon gPol = gPolygon.ByVertices(vertices, false);
                input.Add(gPol);
            }

            return new Graph(input);
        }
        
        /// <summary>
        /// Creates a new Graph by a set of lines.
        /// </summary>
        /// <param name="lines">Lines</param>
        /// <returns name="graph">New Graph</returns>
        public static Graph ByLines(List<Line> lines)
        {
            if(lines == null) { throw new NullReferenceException("lines"); }
            Graph g = new Graph()
            {
                graph = new Graphical.Graphs.Graph()
            };

            foreach(Line line in lines)
            {
                gVertex start = gVertex.ByCoordinates(line.StartPoint.X, line.StartPoint.Y, line.StartPoint.Z);
                gVertex end = gVertex.ByCoordinates(line.EndPoint.X, line.EndPoint.Y, line.EndPoint.Z);
                g.graph.AddEdge(gEdge.ByStartVertexEndVertex(start, end));
            }
            return g;
        }

        #endregion

        #region Public Methods
        public Graph VisibilityGraph(bool reducedGraph = true)
        {
            var visGraph = new VisibilityGraph(this.graph, reducedGraph, true);

            var baseGraph = new Graph();
            baseGraph.graph = visGraph;

            return baseGraph;

        }

        [MultiReturn(new[] { "graph", "length" })]
        public static Dictionary<string, object> ShortestPath(Graph visibilityGraph, Point origin, Point destination)
        {
            if (visibilityGraph == null) { throw new ArgumentNullException("visibilityGraph"); }
            if (origin == null) { throw new ArgumentNullException("origin"); }
            if (destination == null) { throw new ArgumentNullException("destination"); }

            //Graphical.Graphs.Graph internalGraph = visibilityGraph.graph;
            gVertex gOrigin = gVertex.ByCoordinates(origin.X, origin.Y, origin.Z);
            gVertex gDestination = gVertex.ByCoordinates(destination.X, destination.Y, destination.Z);

            if (!visibilityGraph.IsVisibilityGraph()) { throw new ArgumentException("Input should be a Visibility Graph", "visibilityGraph"); }
            
            Graphical.Graphs.VisibilityGraph visGraph = visibilityGraph.graph as Graphical.Graphs.VisibilityGraph;
            var shortest = Graphical.Graphs.VisibilityGraph.ShortestPath(visGraph, gOrigin, gDestination);
            Graph resultGraph = new Graph();
            resultGraph.graph = shortest;
            return new Dictionary<string, object>()
            {
                {"graph", resultGraph },
                {"length", shortest.edges.Select(e => e.Length).Sum() }
            };
        }


        public static Graph VertexVisibility(Graph graph, Point point)
        {
            if(graph == null) { throw new ArgumentNullException("graph"); }
            if (point == null) { throw new ArgumentNullException("point"); }

            gVertex origin = gVertex.ByCoordinates(point.X, point.Y, point.Z);

            Graph g = new Graph()
            {
                graph = Graphical.Graphs.VisibilityGraph.VertexVisibility(origin, graph.graph, false, false)
            };

            return g;
        }

        #endregion

            #region Override Methods
            /// <summary>
            /// Override of ToString Method
            /// </summary>
            /// <returns></returns>
        public override string ToString()
        {
            return String.Format("Graph:(gVertices: {0}, gEdges: {1})", graph.vertices.Count.ToString(), graph.edges.Count.ToString());
        }

        /// <summary>
        /// Customizing the render of Graph
        /// </summary>
        /// <param name="package"></param>
        /// <param name="parameters"></param>
        [IsVisibleInDynamoLibrary(false)]
        public void Tessellate(IRenderPackage package, TessellationParameters parameters)
        {
            foreach (gVertex v in graph.vertices)
            {
                package.AddPointVertex(v.X, v.Y, v.Z);
                package.AddPointVertexColor(255, 0, 0, 255);
            }
            foreach (gEdge e in graph.edges)
            {
                package.AddLineStripVertex(e.StartVertex.X, e.StartVertex.Y, e.StartVertex.Z);
                package.AddLineStripVertex(e.EndVertex.X, e.EndVertex.Y, e.EndVertex.Z);
                /*Colour addition can be done iteratively with a for loop,
                 * but for just two elements might be better to save the overhead
                 * variable declaration and all.
                 */
                package.AddLineStripVertexColor(150, 200, 255, 255);
                package.AddLineStripVertexColor(150, 200, 255, 255);
            }
        } 
        #endregion
    }
}
