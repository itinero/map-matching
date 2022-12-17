using Itinero.Network.Enumerators.Edges;
using Itinero.Routes.Paths;

namespace Itinero.MapMatching;

internal static class PathExtensions
{
    /// <summary>
    /// Returns the length of a path in meters.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static double LengthInMeter(this Path path)
    {
        var edgeEnumerator = path.RoutingNetwork.GetEdgeEnumerator();

        var length = 0.0;
        foreach (var (edge, forward, offset1, offset2) in path)
        {
            edgeEnumerator.MoveTo(edge, forward);

            var edgeLength = 0.0;
            if (edgeEnumerator.Length.HasValue)
            {
                edgeLength += edgeEnumerator.Length.Value / 100.0;
            }
            else
            {
                edgeLength = edgeEnumerator.EdgeLength();
            }

            if (offset1 != 0)
            {
                edgeLength -= OffsetFactor(offset1) * edgeLength;
            }

            if (offset2 != ushort.MaxValue)
            {
                edgeLength -= (1.0 - OffsetFactor(offset2)) * edgeLength;
            }

            length += edgeLength;
        }

        return length;
    }
    
    private static double OffsetFactor(ushort offset)
    {
        return offset / (double)ushort.MaxValue;
    }
    
    /// <summary>
    /// The transition from path1 to path2 represents a u-turn.
    /// </summary>
    /// <param name="path1">The first path.</param>
    /// <param name="path2">The second path.</param>
    /// <returns>True if a u-turn, false otherwise.</returns>
    public static bool IsUTurn(this Path path1, Path path2)
    {
        if (path1.Last.edge.LocalId == path2.First.edge.LocalId &&
            path1.Last.edge.TileId == path2.First.edge.TileId &&
            path1.Last.direction != path2.First.direction)
        {
            return path1.Offset2 + path2.Offset1 == ushort.MaxValue;
        }

        return false;
    }

    /// <summary>
    /// Tries to merge the two paths by removing the last few edges if they represent a u-turn.
    /// </summary>
    /// <param name="path1">The first path.</param>
    /// <param name="path2">The second path.</param>
    /// <returns>A path without u-turns or null if merging is not possible.</returns>
    public static Path? TryMergeAsUTurn(this Path path1, Path path2)
    {
        if (path1.IsUTurn(path2))
        {
            path1 = path1.Clone();
            path2 = path2.Clone();
        }
        else
        {
            return null;
        }

        do
        {
            if (path2.Count == 1 &&
                path2.Offset2 < ushort.MaxValue)
            {
                path1.Offset2 = (ushort)(ushort.MaxValue - path2.Offset2);
                return path1;
            }
            path1.RemoveLast();
            path2.RemoveFirst();

            if (path1.Count == 0) return null;
            if (path2.Count == 0) return path1;
        } while (path1.IsUTurn(path2));

        if (path1.IsNext(path2))
        {
            return new[] { path1, path2 }.Merge();
        }

        return null;
    }

    public static Path? TryMergeOverlap(this Path path1, Path path2)
    {
        path2 = path2.Clone();
        while (path1.Last.edge.LocalId != path2.First.edge.LocalId ||
               path1.Last.edge.TileId != path2.First.edge.TileId)
        {
            path2.RemoveFirst();
        }

        if (path2.Count > 0)
        {
            if (path2.Count > 1)
            {
                path2.RemoveFirst();
                path1.Offset2 = ushort.MaxValue;
                return new[] { path1, path2 }.Merge();
            }
            else
            {
                path1 = path1.Clone();
                path1.Offset2 = path2.Offset2;
                return path1;
            }
        }

        return null;
    }
}
