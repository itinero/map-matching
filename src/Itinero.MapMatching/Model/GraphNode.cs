using Itinero.Snapping;

namespace Itinero.MapMatching.Model;

public class GraphNode
{
    public int? TrackPoint { get; set; }

    public SnapPoint? SnapPoint { get; set; }

    public double Cost { get; set; }
}
