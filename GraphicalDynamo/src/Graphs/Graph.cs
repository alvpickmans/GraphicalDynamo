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
using Graphical.Core;
using System.Drawing;
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
        internal DSCore.Color edgeDefaultColour = DSCore.Color.ByARGB(150, 200, 255, 255);
        internal DSCore.Color vertexDefaultColour = DSCore.Color.ByARGB(255, 0, 0, 255);

        internal Dictionary<double, DSCore.Color> colorRange { get; set; }

        internal List<double> Factors { get; set; }
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


        /// <summary>
        /// Connectivity factors represent the number of connections an edge has 
        /// on a range from 0 to 1.
        /// </summary>
        /// <param name="visGraph">Visibility Graph</param>
        /// <returns name="visGraph">Visibility Graph</returns>
        /// <returns name="factors">Connectivity factors by edge on graph</returns>
        [NodeCategory("Query")]
        [MultiReturn(new[] { "visGraph", "factors" })]
        public static Dictionary<string, object> ConnectivityFactors(Graph visGraph, 
            [DefaultArgument("null")]List<DSCore.Color> colors, 
            [DefaultArgument("null")]List<double> indices)
        {
            if (!visGraph.IsVisibilityGraph) { throw new ArgumentException("Needs to be visibility graph","visGraph"); }
            VisibilityGraph vGraph = visGraph.graph as VisibilityGraph;

            Graph graph = new Graph()
            {
                graph = vGraph,
                Factors = vGraph.ConnectivityFactor()
            };

            if(colors != null && indices != null && colors.Count == indices.Count)
            {
                graph.colorRange = new Dictionary<double, DSCore.Color>();
                // Create KeyValuePairs and sort them by index in case unordered.
                var pairs = indices.Zip(colors, (i, c) => new KeyValuePair<double, DSCore.Color>(i, c)).OrderBy(kv => kv.Key);

                // Adding values to colorRange dictionary
                foreach(KeyValuePair<double, DSCore.Color> kv in pairs)
                {
                    graph.colorRange.Add(kv.Key, kv.Value);
                }
            }

            return new Dictionary<string, object>()
            {
                {"visGraph", graph },
                {"factors", graph.Factors }
            };
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
        /// Creates a Graph by a set of boundary and internal polygons.
        /// </summary>
        /// <param name="boundaries">Boundary polygons</param>
        /// <param name="internals">Internal polygons</param>
        /// <returns name="graph">Base graph</returns>
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

            // There are factorsto represent
            if (this.Factors != null && colorRange != null)
            {
                package.RequiresPerVertexColoration = true;
                var rangeColors = colorRange.Values.ToList();
                for (var i = 0; i < graph.edges.Count; i++)
                {
                    var e = graph.edges[i];
                    var factor = Factors[i];
                    DSCore.Color color;

                    if (factor <= colorRange.First().Key)
                    {
                       color = colorRange.First().Value;
                    }
                    else if(factor >= colorRange.Last().Key)
                    {
                       color = colorRange.Last().Value;
                    }
                    else
                    {
                        int index = List.BisectIndex(colorRange.Keys.ToList(), factor);
                        
                        color = DSCore.Color.Lerp(rangeColors[index-1], rangeColors[index], Factors[i]);
                    }
                    

                    AddColouredEdge(package, e, color);
                }
            }
            else
            {

                foreach (gEdge e in graph.edges)
                {
                    AddColouredEdge(package, e, edgeDefaultColour);
                }
            }

        } 

        internal static byte[] CreateColorByteArrayByColours(List<DSCore.Color> colors)
        {
            byte[] arr = new byte[colors.Count * 4];
            for(var i = 0; i < colors.Count; i++)
            {
                arr[i * 4] = colors[i].Red;
                arr[i * 4 + 1] = colors[i].Green;
                arr[i * 4 + 2] = colors[i].Blue;
                arr[i * 4 + 2] = colors[i].Alpha; 
            }
            return arr;
        }

        internal static void AddColouredEdge(IRenderPackage package, gEdge edge, DSCore.Color color)
        {
            package.AddLineStripVertex(edge.StartVertex.X, edge.StartVertex.Y, edge.StartVertex.Z);
            package.AddLineStripVertex(edge.EndVertex.X, edge.EndVertex.Y, edge.EndVertex.Z);

            package.AddLineStripVertexColor(color.Red, color.Green, color.Blue, color.Alpha);
            package.AddLineStripVertexColor(color.Red, color.Green, color.Blue, color.Alpha);

            package.AddLineStripVertexCount(2);
        }
        #endregion
    }
}
