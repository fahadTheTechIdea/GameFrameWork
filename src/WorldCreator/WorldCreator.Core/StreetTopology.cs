using WorldCreator.Core.Geometry;
namespace WorldCreator.Core.Geometry;
public sealed record StreetGraphNode(string Id, WorldPoint Location);
public sealed record StreetGraphEdge(string Id, string CorridorId, string FromId, string ToId, double LengthMeters);
public sealed record StreetTopology(IReadOnlyList<StreetGraphNode> Nodes,
    IReadOnlyList<StreetGraphEdge> Edges, IReadOnlyList<string> Diagnostics)
{
    public bool IsConnected()
    {
        if (Nodes.Count == 0) return true;
        var adjacent = Nodes.ToDictionary(n => n.Id, _ => new List<string>());
        foreach (var e in Edges)
        {
            adjacent[e.FromId].Add(e.ToId);
            adjacent[e.ToId].Add(e.FromId);
        }
        var reached = new HashSet<string> { Nodes[0].Id };
        var pending = new Queue<string>();
        pending.Enqueue(Nodes[0].Id);
        while (pending.TryDequeue(out var current))
            foreach (var next in adjacent[current])
                if (reached.Add(next)) pending.Enqueue(next);
        return reached.Count == Nodes.Count;
    }
}
public static class StreetTopologyBuilder
{
    private const double Precision = 1_000_000d;
    private static string Key(WorldPoint p) =>
        $"{Math.Round(p.X * Precision):F0}:{Math.Round(p.Y * Precision):F0}";
    public static StreetTopology Build(IReadOnlyList<StreetCorridor> corridors,
        IReadOnlyList<WorldPoint>? terminals = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(corridors);
        var diagnostics = new List<string>();
        var nodes = new Dictionary<string, StreetGraphNode>();
        var edges = new List<StreetGraphEdge>();
        var segments = new List<(string CorridorId, WorldPoint A, WorldPoint B)>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var corridor in corridors)
        {
            token.ThrowIfCancellationRequested();
            corridor.Centerline.Validate();
            corridor.Profile.Validate();
            if (!ids.Add(corridor.Id)) diagnostics.Add($"DUPLICATE_CORRIDOR_ID:{corridor.Id}");
            for (var i = 1; i < corridor.Centerline.Points.Count; i++)
            {
                segments.Add((corridor.Id, corridor.Centerline.Points[i - 1], corridor.Centerline.Points[i]));
                if(segments.Count>4096) return new([],[],["STREET_SEGMENT_BUDGET_EXCEEDED"]);
            }
        }
        var cuts = segments.Select(s => new List<WorldPoint> { s.A, s.B }).ToArray();
        for (int i = 0; i < segments.Count; i++)
        for (int j = i + 1; j < segments.Count; j++)
        {
            token.ThrowIfCancellationRequested();
            var a = segments[i];
            var b = segments[j];
            var hit = PlanarIntersections.Intersect(a.A, a.B, b.A, b.B);
            if (hit.Kind == IntersectionKind.CollinearOverlap)
                diagnostics.Add($"COLLINEAR_OVERLAP:{a.CorridorId}:{b.CorridorId}");
            if (hit.Point is { } point)
            {
                cuts[i].Add(point);
                cuts[j].Add(point);
            }
        }
        for (int i = 0; i < segments.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            var segment = segments[i];
            foreach (var point in terminals ?? [])
            {
                double dx=segment.B.X-segment.A.X,dy=segment.B.Y-segment.A.Y;
                double t=((point.X-segment.A.X)*dx+(point.Y-segment.A.Y)*dy)/(dx*dx+dy*dy);
                if (t>=0 && t<=1 && point.DistanceTo(new(segment.A.X+t*dx,segment.A.Y+t*dy))<1e-8)
                    cuts[i].Add(point);
            }
            var sorted = cuts[i].DistinctBy(Key)
                .OrderBy(p => (p.X - segment.A.X) * (segment.B.X - segment.A.X) +
                              (p.Y - segment.A.Y) * (segment.B.Y - segment.A.Y)).ToArray();
            for (int j = 1; j < sorted.Length; j++)
            {
                var a = sorted[j - 1];
                var b = sorted[j];
                if (a.DistanceTo(b) <= 1e-7) continue;
                var ka = Key(a); var kb = Key(b);
                if (!nodes.ContainsKey(ka)) nodes[ka] = new(ka, a);
                if (!nodes.ContainsKey(kb)) nodes[kb] = new(kb, b);
                edges.Add(new($"{segment.CorridorId}:{i}:{j}", segment.CorridorId, ka, kb, a.DistanceTo(b)));
            }
        }
        var ordered = nodes.Values.OrderBy(n => n.Id, StringComparer.Ordinal).ToArray();
        var result = new StreetTopology(ordered, edges, diagnostics);
        if (!result.IsConnected()) diagnostics.Add("DISCONNECTED_STREET_GRAPH");
        return result;
    }
}
