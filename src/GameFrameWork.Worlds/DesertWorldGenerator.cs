using GameFrameWork.Core;
namespace GameFrameWork.Worlds;
/// <summary>Deterministic first vertical slice. Does not yet implement advanced geography.</summary>
public sealed class DesertWorldGenerator : IWorldGenerator
{
    public Task<WorldDocument> GenerateAsync(WorldRequest request, CancellationToken cancellationToken = default)
    {
        request.Validate();
        if (request.Biome != Biome.Desert) throw new NotSupportedException("This generator supports desert worlds only.");
        var random = new Random(request.Seed);
        var kinds = new CellKind[request.Width * request.Height];
        var heights = new float[kinds.Length];
        for (var y = 0; y < request.Height; y++)
        for (var x = 0; x < request.Width; x++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var i = y * request.Width + x;
            heights[i] = (float)Math.Round(1.0 + random.NextDouble() * 0.6, 3);
            kinds[i] = random.NextDouble() < .07 ? CellKind.Rock : CellKind.Sand;
        }
        var objects = new List<WorldObject>();
        for (var s = 0; s < request.SettlementCount; s++)
        {
            var cx = 4 + random.Next(request.Width - 8);
            var cy = 4 + random.Next(request.Height - 8);
            // Connected cross-shaped service roads; structures are placed adjacent to the roads.
            for (var x = 1; x < request.Width - 1; x++) kinds[cy * request.Width + x] = CellKind.Road;
            for (var y = 1; y < request.Height - 1; y++) kinds[y * request.Width + cx] = CellKind.Road;
            for (var b = 0; b < 4; b++)
            {
                var x = cx + (b % 2 == 0 ? -2 : 2);
                var y = Math.Clamp(cy - 2 + b, 1, request.Height - 2);
                var index = y * request.Width + x;
                kinds[index] = CellKind.Structure;
                objects.Add(new WorldObject($"settlement-{s}-building-{b}", "warehouse", x, y, "idle"));
            }
            var gx = cx; var gy = cy;
            objects.Add(new WorldObject($"settlement-{s}-gate", "gate", gx, gy, "closed", "gate-open-close"));
        }
        var cells = new WorldCell[kinds.Length];
        for (var y = 0; y < request.Height; y++)
        for (var x = 0; x < request.Width; x++)
        {
            var i = y * request.Width + x;
            var kind = kinds[i];
            var blocked = kind is CellKind.Rock or CellKind.Structure or CellKind.Water or CellKind.Tree;
            cells[i] = new WorldCell(x, y, kind, heights[i], blocked, blocked ? int.MaxValue : kind == CellKind.Road ? 1 : 3, kind == CellKind.Structure ? 2 : 0);
        }
        return Task.FromResult(new WorldDocument(1, request, cells, objects));
    }
}