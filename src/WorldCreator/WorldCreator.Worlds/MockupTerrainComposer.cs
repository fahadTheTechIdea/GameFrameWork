using WorldCreator.Core;
using WorldCreator.Core.Geometry;
using WorldCreator.Core.Planning;

namespace WorldCreator.Worlds;

public sealed record TerrainRasterResult(IReadOnlyList<WorldCell> Cells, IReadOnlyList<string> Issues);

/// <summary>Rasterizes authored terrain masks and gradients. Seed never invents
/// geography. Same-priority ambiguity or uncovered cells prevents output.</summary>
public sealed class MockupTerrainComposer(ISettlementCatalogRegistry catalogs)
{
    public TerrainRasterResult Compose(WorldRequest request, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var invalid = new MockupDesignValidator(catalogs).Validate(request,token);
        if (invalid.Count != 0) return new([], invalid.Select(i => i.Code + ":" + i.ObjectId + ": " + i.Message).ToArray());
        var design = request.Design!;
        var regions = design.Blueprint.Regions.ToDictionary(r => r.Id, StringComparer.Ordinal);
        var rules = design.TerrainRules.OrderBy(r => r.Priority).ThenBy(r => r.RegionId, StringComparer.Ordinal).ToArray();
        var owners = new int[checked(request.Width*request.Height)];
        Array.Fill(owners, -1);
        var issues = new List<string>();
        for (int i = 0; i < rules.Length; i++)
        {
            var rule = rules[i]; var region = regions[rule.RegionId].Outline;
            int left = Math.Max(0, (int)Math.Floor(region.Vertices.Min(p => p.X)/design.MetersPerCell));
            int right = Math.Min(request.Width-1, (int)Math.Ceiling(region.Vertices.Max(p => p.X)/design.MetersPerCell)-1);
            int top = Math.Max(0, (int)Math.Floor(region.Vertices.Min(p => p.Y)/design.MetersPerCell));
            int bottom = Math.Min(request.Height-1, (int)Math.Ceiling(region.Vertices.Max(p => p.Y)/design.MetersPerCell)-1);
            bool ambiguous = false;
            for (int y = top; y <= bottom; y++)
            for (int x = left; x <= right; x++)
            {
                token.ThrowIfCancellationRequested();
                if (!region.ContainsPoint(new((x+.5)*design.MetersPerCell, (y+.5)*design.MetersPerCell))) continue;
                int index = y*request.Width+x;
                if (owners[index] >= 0 && rules[owners[index]].Priority == rule.Priority) ambiguous = true;
                owners[index] = i;
            }
            if (ambiguous) issues.Add("TERRAIN_PRIORITY_AMBIGUOUS:" + rule.RegionId);
        }
        if (owners.Any(o => o < 0)) issues.Add("TERRAIN_COVERAGE_GAP");
        if (issues.Count != 0) return new([], issues);
        var origins = rules.Select(r => new WorldPoint(regions[r.RegionId].Outline.Vertices.Min(p => p.X),
            regions[r.RegionId].Outline.Vertices.Min(p => p.Y))).ToArray();
        var cells = new WorldCell[owners.Length];
        for (int y = 0; y < request.Height; y++)
        for (int x = 0; x < request.Width; x++)
        {
            token.ThrowIfCancellationRequested();
            int index = y*request.Width+x, owner = owners[index];
            var rule = rules[owner];
            double height = rule.ElevationMeters + ((x+.5)*design.MetersPerCell-origins[owner].X)*rule.SlopeX +
                ((y+.5)*design.MetersPerCell-origins[owner].Y)*rule.SlopeY;
            if (!double.IsFinite(height) || Math.Abs(height) > float.MaxValue)
                return new([], ["TERRAIN_HEIGHT_OUT_OF_RANGE:" + rule.RegionId]);
            bool blocked = rule.Kind is CellKind.Rock or CellKind.Water;
            cells[index] = new(x,y,rule.Kind,(float)height,blocked,blocked ? int.MaxValue : 3,0,rule.RegionId,rule.RuleId,rule.SurfaceMaterialId);
        }
        // A spacing band is an authored ground material, below peer/higher surface masks.
        // Ground and sports polygons retain their exact source/design contours.
        foreach(var spacing in (design.SurfaceClearances??[]).Where(r=>r.GapMaterialId is not null&&r.MinimumGapMeters>0))
        {
            var sourceRule=rules.Single(r=>r.RegionId==spacing.RegionId);var polygon=regions[spacing.RegionId].Outline;double gap=spacing.MinimumGapMeters;
            int left=Math.Max(0,(int)Math.Floor((polygon.Vertices.Min(p=>p.X)-gap)/design.MetersPerCell));
            int right=Math.Min(request.Width-1,(int)Math.Ceiling((polygon.Vertices.Max(p=>p.X)+gap)/design.MetersPerCell));
            int top=Math.Max(0,(int)Math.Floor((polygon.Vertices.Min(p=>p.Y)-gap)/design.MetersPerCell));
            int bottom=Math.Min(request.Height-1,(int)Math.Ceiling((polygon.Vertices.Max(p=>p.Y)+gap)/design.MetersPerCell));
            for(int y=top;y<=bottom;y++)for(int x=left;x<=right;x++)
            {
                token.ThrowIfCancellationRequested();int index=y*request.Width+x;
                if(rules[owners[index]].Priority>=sourceRule.Priority||cells[index].Kind is CellKind.Rock or CellKind.Water)continue;
                var p=new WorldPoint((x+.5)*design.MetersPerCell,(y+.5)*design.MetersPerCell);
                if(!polygon.ContainsPoint(p)&&polygon.DistanceToPoint(p)<=gap+1e-8)
                    cells[index]=cells[index] with {Kind=CellKind.Sand,SurfaceMaterialId=spacing.GapMaterialId,SourceVisualRegionId=spacing.RegionId,DesignRuleId="spacing-band:"+spacing.Id};
            }
        }
        return new(cells, []);
    }
}
