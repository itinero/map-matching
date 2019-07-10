// Based on Newson and Krumm, 2009
// https://www.microsoft.com/en-us/research/wp-content/uploads/2016/12/map-matching-ACM-GIS-camera-ready.pdf

fn map_match(gpx, road_network) {
    projection: mapping (uint gpx_point, uint road_segment) → Coords projected_point

    for gpx_point in gpx:
        for segment in road_network within 200m of gpx_point:
            projection[gpx_point, segment] = projection of gpx_point on segment

    emission_p = emission_probabilities(projection)
    start_p = start_probabilities(gpx[0], emission_p)
    transition_p = transition_probabilities(projection)

    selected_edges = viterbi(gpx, segments of road network,
        start_p,
        emission_p,
        transition_p)

    route = []
    for edge, next in each_with_next(selected_edges):
        route.append(route from edge to next along roads)
    return route
}

fn start_probabilities(gpx_point, emission_p) {
    start_p: mapping (uint road_segment) → float probability (default 0)
    for (_, segment) → prob in emission_p[gpx_point, *]:
        start_p[segment] = prob

    return start_p
}

fn emission_probabilities(projection) {
    emission_p: mapping (uint gpx_point, uint road_segment) → float probability (default 0)

    sigma = // TODO (left as "future work" in the paper)
    // They use sigma = 1.4826 * median( great circle distance between GPX point and projection on correct segment )

    for (gpx_p, seg) → proj in projection:
        // Zero-mean Gaussian distribution
        emission_p[proj, seg] = 1/sqrt(2*pi)/sigma * exp(-0.5 * great circle distance between gpx_p and proj / sigma)

    return emission_p
}

fn transition_probabilities(projection) {
    transition_p: mapping (uint gpx_point, uint road_segment, uint road_segment) → float probability (default 0)

    beta = // TODO (left as "future work" in the paper)
    // They use beta = 1/ln(2) * median( (great circle distance between correct segments) / great circle distance )

    for (gpx_point, seg1) → proj1 in projection:
        for (_, seg2) → proj2 in projection[gpx_point + 1, *]:
            // Exponential distribution
            transition_p[gpx_point, seg1, seg2] = 1/beta * exp( -distance_factor(gpx_point, gpx_point + 1, proj1, proj2) /beta)

    return transition_p
}

fn distance_factor(p1, p2, proj1, proj2) {
    dist_route = route distance from proj1 to proj2
    dist_gcirc = great circle distance between p1 and p2
    d = dist_route - dist_gcirc
    return d
}
