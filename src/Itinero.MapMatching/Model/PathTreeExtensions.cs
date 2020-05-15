using System.Collections.Generic;

namespace Itinero.MapMatching.Model
{
    internal static class PathTreeExtensions
    {
        public static uint AddVisit(this PathTree tree, uint vertex, uint previousPointer)
        {
            var data0 = vertex;
            var data1 = previousPointer;

            return tree.Add(data0, data1);
        }

        public static (uint vertex, uint previousPointer) GetVisit(this PathTree tree, uint pointer)
        {
            tree.Get(pointer, out var data0, out var data1);
            return (data0, data1);
        }

        public static IEnumerable<uint> GetVisits(this PathTree tree, uint pointer)
        {
            while (pointer != uint.MaxValue)
            {
                var (vertex, previousPointer) = tree.GetVisit(pointer);
                yield return vertex;
                pointer = previousPointer;
            }
        }
    }
}