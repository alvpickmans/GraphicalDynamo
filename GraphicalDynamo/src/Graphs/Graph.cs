#region namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSPoint = Autodesk.DesignScript.Geometry.Point;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Interfaces;
using Autodesk.DesignScript.Runtime;
using System.Globalization;
using Graphical.Geometry;
using Graphical.Graphs;
using Dynamo.Graph.Nodes;
using GraphicalDynamo.Geometry;
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
        /// <summary>
        /// Checks if the input is a Visibility or Base graph.
        /// </summary>
        public bool IsVisibilityGraph
        {
            get
            {
                return graph.GetType() == typeof(Graphical.Graphs.VisibilityGraph);
            }
        }

        /// <summary>
        /// Returns the input graph as a list of lines
        /// </summary>
        /// <returns name="lines">List of lines representing the graph.</returns>
        [NodeCategory("Query")]
        public List<Line> Lines()
        {
            List<Line> lines = new List<Line>();
            foreach(gEdge edge in graph.edges)
            {
                var start = Points.ToPoint(edge.StartVertex);
                var end = Points.ToPoint(edge.EndVertex);
                lines.Add(Line.ByStartPointEndPoint(start, end));
            }
            return lines;
        }

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
        /// <returns name="graph">Base graph</returns>
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
        /// Creates a Graph by a set of boundaries and internals polygons.
        /// </summary>
        /// <param name="boundaries">Boundary polygons</param>
        /// <param name="internals">Internal polygons</param>
        /// <returns name="graph">Base graph</returns>
        public static Graph ByBoundariesAndInternalaPolygons(List<Polygon> boundaries, [DefaultArgument("[]")]List<Polygon> internals)
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
        /// <returns name="graph">Base Graph</returns>
        public static Graph ByLines(List<Line> lines)
        {
            if(lines == null) { throw new NullReferenceException("lines"); }
            Graph g = new Graph()
            {
                graph = new Graphical.Graphs.Graph()
            };

            foreach(Line line in lines)
            {
                gVertex start = Geometry.Points.ToVertex(line.StartPoint);
                gVertex end = Geometry.Points.ToVertex(line.EndPoint);
                g.graph.AddEdge(gEdge.ByStartVertexEndVertex(start, end));
            }
            return g;
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Computes the Visibility Graph from a base graph using Lee's algorithm.
        /// </summary>
        /// <param name="graph">Base graph</param>
        /// <param name="reducedGraph">Reduced graph returns edges where its vertices belong to different 
        /// polygons and at least one is not convex/concave to its polygon.</param>
        /// <returns name="visGraph">Visibility graph</returns>
        [NodeCategory("Actions")]
        public static Graph VisibilityGraph(Graph graph, bool reducedGraph = true)
        {
            if(graph == null) { throw new ArgumentNullException("graph"); }
            var visGraph = new VisibilityGraph(graph.graph, reducedGraph, true);

            var baseGraph = new Graph()
            {
                graph = visGraph
            };

            return baseGraph;

        }

        /// <summary>
        /// Returns a graph representing the shortest path 
        /// between two points on a given Visibility Graph.
        /// </summary>
        /// <param name="visGraph">Visibility Graph</param>
        /// <param name="origin">Origin point</param>
        /// <param name="destination">Destination point</param>
        /// <returns name="graph">Graph representing the shortest path</returns>
        /// <returns name="length">Length of path</returns>
        [MultiReturn(new[] { "graph", "length" })]
        public static Dictionary<string, object> ShortestPath(Graph visGraph, DSPoint origin, DSPoint destination)
        {
            if (visGraph == null) { throw new ArgumentNullException("visGraph"); }
            if (origin == null) { throw new ArgumentNullException("origin"); }
            if (destination == null) { throw new ArgumentNullException("destination"); }

            //Graphical.Graphs.Graph internalGraph = visibilityGraph.graph;
            gVertex gOrigin = gVertex.ByCoordinates(origin.X, origin.Y, origin.Z);
            gVertex gDestination = gVertex.ByCoordinates(destination.X, destination.Y, destination.Z);

            if (!visGraph.IsVisibilityGraph) { throw new ArgumentException("Input should be a Visibility Graph", "visibilityGraph"); }
            
            VisibilityGraph vGraph = visGraph.graph as Graphical.Graphs.VisibilityGraph;
            var shortest = Graphical.Graphs.VisibilityGraph.ShortestPath(vGraph, gOrigin, gDestination);
            Graph resultGraph = new Graph();
            resultGraph.graph = shortest;
            return new Dictionary<string, object>()
            {
                {"graph", resultGraph },
                {"length", shortest.edges.Select(e => e.Length).Sum() }
            };
        }


        /// <summary>
        /// Returns a Graph representing all the vertices on 
        /// a Graph that are visibles from a given point.
        /// </summary>
        /// <param name="graph">Base Graph</param>
        /// <param name="point">Origin point</param>
        /// <returns name="graph">Graph representing the vertex visibility</returns>
        [NodeCategory("Actions")]
        public static Graph VertexVisibility(Graph graph, DSPoint point)
        {
            if (graph == null) { throw new ArgumentNullException("graph"); }
            if (graph.IsVisibilityGraph) { throw new ArgumentException("Input cannot be a Visibility Graph"); }
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
