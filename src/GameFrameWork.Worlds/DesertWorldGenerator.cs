using GameFrameWork.Core;
namespace GameFrameWork.Worlds;
/// <summary>Deterministic grid projection of the first purpose-driven settlement plan.
/// Height/rock are spatially correlated analytical terrain prototypes, not mature hydrology.
/// </summary>
public sealed class DesertWorldGenerator : IWorldGenerator
{
    public Task<WorldDocument> GenerateAsync(WorldRequest request, CancellationToken cancellationToken = default)
    {
        request.Validate();
        if (request.Biome != Biome.Desert) throw new NotSupportedException("Desert only in the current terrain generator.");
        var plan = new SettlementPlanner().Plan(request, cancellationToken);
        if (plan.Diagnostics.Count != 0)
            throw new InvalidOperationException(string.Join("; ", plan.Diagnostics.Select(d => d.Message)));
        var count = checked(request.Width * request.Height);
        var kinds = new CellKind[count];
        var heights = new float[count];
        // Smooth spatially correlated geological variation; seed changes field phases.
        // Landform positions depend on analytical geography rather than random points.
        double phaseX = (request.Seed % 1009) * .017;
        double phaseY = (request.Seed % 997) * .019;
        for (int y = 0; y < request.Height; y++)
        for (int x = 0; x < request.Width; x++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int index = y * request.Width + x;
            double ridge = Math.Sin(x * .075 + phaseX) * Math.Cos(y * .061 + phaseY);
            double dunes = Math.Sin(x * .24 + y * .045 + phaseY) * .12;
            heights[index] = (float)Math.Round(2.0 + ridge * .65 + dunes, 3);
            kinds[index] = ridge > .72 ? CellKind.Rock : CellKind.Sand;
        }
        foreach (var road in plan.Roads)
            foreach (var position in road.Cells())
                kinds[position.Y * request.Width + position.X] = CellKind.Road;
        var objects = new List<WorldObject>();
        foreach (var building in plan.Buildings)
        {
            kinds[building.Site.Y * request.Width + building.Site.X] = CellKind.Structure;
            objects.Add(new(building.Id, building.Kind, building.Site.X, building.Site.Y, "idle"));
            // Gate is mounted on the building frontage, not independently scattered on a road.
            objects.Add(new(building.Id + "-gate", "gate", building.Site.X, building.Site.Y,
                "closed", "gate-open-close"));
        }
        var cells = new WorldCell[count];
        for (int y = 0; y < request.Height; y++)
        for (int x = 0; x < request.Width; x++)
        {
            int index = y * request.Width + x;
            var kind = kinds[index];
            bool blocked = kind is CellKind.Rock or CellKind.Structure or CellKind.Water or CellKind.Tree;
            cells[index] = new(x, y, kind, heights[index], blocked,
                blocked ? int.MaxValue : kind == CellKind.Road ? 1 : 3,
                kind == CellKind.Structure ? 2 : 0);
        }
        return Task.FromResult(new WorldDocument(1, request, cells, objects));
    }
}
