using GameFrameWork.Core;
namespace GameFrameWork.Worlds;

/// <summary>A composable, inspectable feasibility rule for planned building parcels.</summary>
public interface IParcelConstraint
{
    string Id { get; }
    bool IsSatisfied(GridPoint site, GridPoint access, IReadOnlySet<GridPoint> roads,
        IReadOnlySet<GridPoint> occupied, GridBounds bounds);
}

public sealed class RoadFrontageConstraint : IParcelConstraint
{
    public string Id => "adjacent-road-frontage-v1";
    public bool IsSatisfied(GridPoint site, GridPoint access, IReadOnlySet<GridPoint> roads,
        IReadOnlySet<GridPoint> occupied, GridBounds bounds) =>
        bounds.Contains(site) && bounds.Contains(access) && !roads.Contains(site) &&
        !occupied.Contains(site) && roads.Contains(access) &&
        Math.Abs(site.X - access.X) + Math.Abs(site.Y - access.Y) == 1;
}

public sealed class ParcelConstraintSet(params IParcelConstraint[] constraints)
{
    public IReadOnlyList<string> Unsatisfied(GridPoint site, GridPoint access,
        IReadOnlySet<GridPoint> roads, IReadOnlySet<GridPoint> occupied, GridBounds bounds) =>
        constraints.Where(c => !c.IsSatisfied(site, access, roads, occupied, bounds))
                   .Select(c => c.Id).ToArray();
}
