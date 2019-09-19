using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DS = Autodesk.DesignScript.Geometry;
using Graphical.Geometry;
using GraphicalDynamo.Geometry;

namespace GraphicalDynamo.Graphs
{
    public class ConvexGraph : BaseGraph
    {
        private ConvexGraph(Graphical.Graphs.ConvexGraph convexGraph)
        {
            this.graph = convexGraph;
        }

        public static ConvexGraph ByGraphStartPointAndEndPoint(BaseGraph graph, DS.Point start, DS.Point end)
        {
            Vertex origin = start.ToVertex();
            Vertex destination = end.ToVertex();
            Graphical.Graphs.Graph basegraph = graph.graph;

            x`x`Graphical.Graphs.ConvexGraph convexGraph = Graphical.Graphs.ConvexGraph.ByGraphOriginAndDestination(basegraph, origin, destination);

            return new ConvexGraph(convexGraph);
        }
    }
}
