namespace Itinero.MapMatching
{
    /// <summary>
    /// Contains map matcher extensions.
    /// </summary>
    public static class MapMatcherExtensions
    {
        internal static MapMatcherResult Match(this MapMatcher matcher, Track track)
        {
            return matcher.TryMatch(track).Value;
        }
    }
}