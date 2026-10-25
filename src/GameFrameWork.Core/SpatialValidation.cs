namespace GameFrameWork.Core;

/// <summary>Reports structural faults in a rasterized road network and its land parcels.</summary>
public static class SpatialValidation
{
    private static readonly GridPoint[] Neighbors = { new(1,0), new(-1,0), new(0,1), new(0,-1) };

    public static IReadOnlyList<PlacementDiagnostic> Validate(
        GridBounds bounds, IReadOnlyList<RoadSegment> roads,
        IReadOnlyList<PlacementDecision> buildings)
    {
        var errors = new List<PlacementDiagnostic>();
        var network = new HashSet<GridPoint>();
        var segmentIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var road in roads)
        {
            if (!segmentIds.Add(road.Id))
                errors.Add(new("DUPLICATE_ROAD_ID", road.Id));
            if (!bounds.Contains(road.Start) || !bounds.Contains(road.End))
            {
                errors.Add(new("ROAD_OUT_OF_BOUNDS", road.Id));
                continue;
            }
            foreach (var point in road.Cells()) network.Add(point);
        }
        if (network.Count > 0)
        {
            var visited = new HashSet<GridPoint>();
            var queue = new Queue<GridPoint>();
            var start = network.OrderBy(p => p.Y).ThenBy(p => p.X).First();
            visited.Add(start);
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var delta in Neighbors)
                {
                    var next = new GridPoint(current.X + delta.X, current.Y + delta.Y);
                    if (network.Contains(next) && visited.Add(next)) queue.Enqueue(next);
                }
            }
            if (visited.Count != network.Count)
                errors.Add(new("DISCONNECTED_ROAD_NETWORK", $"{network.Count - visited.Count} road cells are unreachable."));
        }
        var usedSites = new HashSet<GridPoint>();
        var buildingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var building in buildings)
        {
            if (!buildingIds.Add(building.Id))
                errors.Add(new("DUPLICATE_BUILDING_ID", building.Id));
            if (!bounds.Contains(building.Site) || !bounds.Contains(building.AccessRoadCell))
                errors.Add(new("BUILDING_OUT_OF_BOUNDS", building.Id));
            if (!usedSites.Add(building.Site) || network.Contains(building.Site))
                errors.Add(new("BUILDING_OVERLAP", building.Id));
            if (!network.Contains(building.AccessRoadCell))
                errors.Add(new("FRONTAGE_NOT_ON_ROAD", building.Id));
            int distance = Math.Abs(building.Site.X - building.AccessRoadCell.X) +
                           Math.Abs(building.Site.Y - building.AccessRoadCell.Y);
            // Continuous envelopes can have a designed front setback. Their exact
            // footprint/road clearance is checked by the mockup settlement planner.
            if (building.VisualSource?.Footprint is null && distance != 1)
                errors.Add(new("FRONTAGE_NOT_ADJACENT", building.Id));
            if (string.IsNullOrWhiteSpace(building.Purpose) ||
                string.IsNullOrWhiteSpace(building.ParentId) ||
                string.IsNullOrWhiteSpace(building.ConstraintId))
                errors.Add(new("PLACEMENT_PROVENANCE_MISSING", building.Id));
        }
        return errors;
    }
}
