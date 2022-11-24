using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.MapMatching.IO.GeoJson;
using Itinero.MapMatching.Model;
using Itinero.MapMatching.Solver;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routes.Paths;
using Itinero.Routing;
using Itinero.Snapping;

[assembly: InternalsVisibleTo("Itinero.MapMatching.Test")]
[assembly: InternalsVisibleTo("Itinero.MapMatching.Test.Functional")]

namespace Itinero.MapMatching;

public class MapMatcher
{
    private readonly RoutingNetwork _routingNetwork;
    private readonly Profile _profile;
    private readonly MapMatcherSettings _settings;
    private readonly ModelBuilder _modelBuilder;
    private readonly ModelSolver _modelSolver;

    public MapMatcher(RoutingNetwork routingNetwork, MapMatcherSettings settings)
    {
        _settings = settings;
        _routingNetwork = routingNetwork;
        if (_settings.Profile == null) throw new Exception("No profile set");
        _profile = settings.Profile;

        _modelBuilder = new ModelBuilder(routingNetwork);
        _modelSolver = new ModelSolver();
    }

    public MapMatcherSettings Settings => _settings;

    /// <summary>
    /// Gets the routing network.
    /// </summary>
    public RoutingNetwork RoutingNetwork => _routingNetwork;

    public async Task<IEnumerable<MapMatch>> MatchAsync(Track track)
    {
        var matches = new List<MapMatch>();
        
        // build track model.
        var trackModels = await _modelBuilder.BuildModels(track, _profile);

        foreach (var trackModel in trackModels)
        {
            var trackModelGeoJson = trackModel.ToGeoJson(this.RoutingNetwork);

            // run solver.
            var bestMatch = _modelSolver.Solve(trackModel).ToList();

            // calculate the paths between each matched point pair.
            var rawPaths = new List<Path>();
            var router = _routingNetwork.Route(_profile);
            for (var l = 2; l < bestMatch.Count - 1; l++)
            {
                var sourceNode = trackModel.GetNode(bestMatch[l - 1]);
                var source = sourceNode.SnapPoint;
                var targetNode = trackModel.GetNode(bestMatch[l]);
                var target = targetNode.SnapPoint;

                if (source == null) throw new Exception("Track point should have a snap point");
                if (target == null) throw new Exception("Track point should have a snap point");

                var path = await router.From(source.Value).To(target.Value).Path(CancellationToken.None);
                if (path.IsError)
                    throw new Exception(
                        $"Raw path calculation failed, it shouldn't fail at this point because it succeeded on the same path before: {path.ErrorMessage}");
                rawPaths.Add(path);
                if (rawPaths.Count == 218) Debug.WriteLine("break");
            }

            matches.Add(new MapMatch(track, _profile, rawPaths));
        }

        return matches;
    }
}
