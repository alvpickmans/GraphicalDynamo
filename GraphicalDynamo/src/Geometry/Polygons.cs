using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.DesignScript.Geometry;
using Graphical.Geometry;

namespace GraphicalDynamo.Geometry
{
    internal static class Polygons
    {

        public static List<Polygon> Union(Polygon subject, Polygon clip)
        {
            var result = subject.ToGraphicalPolygon().Union(clip.ToGraphicalPolygon());
            return result.Select(gPol => gPol.ToDynamoPolygon()).ToList();
        }

        public static List<Polygon> Difference(Polygon subject, Polygon clip)
        {
            var result = subject.ToGraphicalPolygon().Difference(clip.ToGraphicalPolygon());
            return result.Select(gPol => gPol.ToDynamoPolygon()).ToList();
        }

        public static List<Polygon> Intersection(Polygon subject, Polygon clip)
        {
            var result = subject.ToGraphicalPolygon().Intersection(clip.ToGraphicalPolygon());
            return result.Select(gPol => gPol.ToDynamoPolygon()).ToList();
        }

        public static List<Polygon> MultiUnion(List<Polygon> subjects, List<Polygon> clips)
        {
            var result = Graphical.Geometry.gPolygon.Union(
                    subjects.Select(s => s.ToGraphicalPolygon()).ToList(),
                    clips.Select(c => c.ToGraphicalPolygon()).ToList()
                    );
            return result.Select(gPol => gPol.ToDynamoPolygon()).ToList();
        }

        public static List<Polygon> MultiDifference(List<Polygon> subjects, List<Polygon> clips)
        {
            var result = Graphical.Geometry.gPolygon.Difference(
                    subjects.Select(s => s.ToGraphicalPolygon()).ToList(),
                    clips.Select(c => c.ToGraphicalPolygon()).ToList()
                    );
            return result.Select(gPol => gPol.ToDynamoPolygon()).ToList();
        }

        public static List<Polygon> MultiIntersection(List<Polygon> subjects, List<Polygon> clips)
        {
            var result = Graphical.Geometry.gPolygon.Union(
                    subjects.Select(s => s.ToGraphicalPolygon()).ToList(),
                    clips.Select(c => c.ToGraphicalPolygon()).ToList()
                    );
            return result.Select(gPol => gPol.ToDynamoPolygon()).ToList();
        }


        internal static gPolygon ToGraphicalPolygon (this Polygon polygon)
        {
            return gPolygon.ByVertices(polygon.Points.Select(p => p.ToVertex()).ToList());
        }

        internal static Polygon ToDynamoPolygon(this gPolygon polygon)
        {
            return Polygon.ByPoints(polygon.Vertices.Select(v => v.ToPoint()));
        }

    }
}
