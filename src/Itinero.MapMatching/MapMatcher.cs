using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.MapMatching.Model;
using Itinero.MapMatching.Solver;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routes.Paths;
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

    public async Task<Result<MapMatch>> MatchAsync(Track track)
    {
        // build track model.
        var trackModel = await _modelBuilder.BuildModel(track, _profile);

        // run solver.
        var bestMatch = _modelSolver.Solve(trackModel);

        // calculate the paths between each matched point pair.
        var rawPaths = new List<Path>();
        // router = _routerDb.Route(_profile);
        // for (var l = 1; l < bestMatch.Count; l++)
        // {
        //     var source = bestMatch[l - 1];
        //     var sourceRp = locations[source.track].points[source.point].rp;
        //     var target = bestMatch[l - 0];
        //     var targetRp = locations[target.track].points[target.point].rp;
        //
        //     var path = router.From(sourceRp).To(targetRp).Path();
        //     if (path.IsError) throw new Exception("Raw path calculation failed, it shouldn't fail at this point because it succeeded on the same path before.");
        //     rawPaths.Add(path);
        // }

        return new Result<MapMatch>(
            new MapMatch(track, _profile, rawPaths));
    }
}
