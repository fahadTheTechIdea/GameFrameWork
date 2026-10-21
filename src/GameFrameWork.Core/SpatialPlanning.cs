namespace GameFrameWork.Core;
public readonly record struct GridPoint(int X, int Y);
public readonly record struct GridBounds(int Width, int Height)
{
    public bool Contains(GridPoint p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;
}
public enum RoadClass { Arterial, Collector, Local }
public sealed record RoadSegment(string Id, GridPoint Start, GridPoint End, RoadClass Class)
{
    public IEnumerable<GridPoint> Cells()
    {
        if (Start.X != End.X && Start.Y != End.Y)
            throw new InvalidOperationException("Grid rasterization requires orthogonal road segments.");
        int dx = Math.Sign(End.X - Start.X), dy = Math.Sign(End.Y - Start.Y);
        var p = Start;
        while (true)
        {
            yield return p;
            if (p == End) yield break;
            p = new GridPoint(p.X + dx, p.Y + dy);
        }
    }
}
public sealed record PlacementDecision(string Id, string Kind, GridPoint Site, string Purpose,
    string ParentId, string ConstraintId, GridPoint AccessRoadCell);
public sealed record PlacementDiagnostic(string Code, string Message);
public sealed record SettlementPlan(IReadOnlyList<RoadSegment> Roads,
    IReadOnlyList<PlacementDecision> Buildings, IReadOnlyList<PlacementDiagnostic> Diagnostics);
