// using Itinero.Network;
//
// namespace Itinero.MapMatching;
//
// /// <summary>
// /// The map matcher.
// /// </summary>
// public interface IMapMatcher
// {
//     /// <summary>
//     /// Gets the settings.
//     /// </summary>
//     public IMapMatcherSettings Settings { get; }
//         
//     /// <summary>
//     /// Gets the routing network.
//     /// </summary>
//     public RoutingNetwork RoutingNetwork { get; }
//         
//     /// <summary>
//     /// Matches the given track.
//     /// </summary>
//     /// <param name="track">The track.</param>
//     /// <returns>Returns a map map match.</returns>
//     public Result<MapMatch> Match(Track track);
// }