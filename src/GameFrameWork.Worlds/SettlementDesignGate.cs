using GameFrameWork.Core;
using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;

namespace GameFrameWork.Worlds;

/// <summary>Validates a settlement design as a whole before allowing compilation.</summary>
public sealed class SettlementDesignGate
{
    public IReadOnlyList<string> Validate(SettlementDesignResult result, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        var errors = new List<string>(result.Diagnostics);
        errors.AddRange(new WorldDesignValidator().Validate(result.Design,token).Issues
            .Select(x => $"{x.Code}:{x.ObjectId}"));
        var roadIds = result.Streets.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        var plannedIds = result.Design.Objects.Select(o => o.Id).ToHashSet(StringComparer.Ordinal);
        if (roadIds.Count != result.Streets.Count)
            errors.Add("DUPLICATE_STREET_ID");
        foreach (var street in result.Streets)
        {
            try { street.Centerline.Validate(); street.Profile.Validate(); }
            catch (ArgumentException) { errors.Add($"INVALID_STREET:{street.Id}"); }
        }
        foreach (var building in result.Settlement.Buildings)
        {
            if (!plannedIds.Contains(building.Id))
                errors.Add($"MISSING_PLANNED_BUILDING:{building.Id}");
            var access = building.AccessRoadCell;
            if (!result.Settlement.Roads.Any(r => r.Cells().Contains(access)))
                errors.Add($"INVALID_BUILDING_ACCESS:{building.Id}");
        }
        var features = new HashSet<string>(StringComparer.Ordinal);
        foreach (var feature in result.PublicRealm.Features)
        {
            if (!features.Add(feature.Id))
                errors.Add($"DUPLICATE_PUBLIC_FEATURE:{feature.Id}");
            if (!roadIds.Contains(feature.ParentId))
                errors.Add($"ORPHAN_PUBLIC_FEATURE:{feature.Id}");
            if (string.IsNullOrWhiteSpace(feature.Purpose) || string.IsNullOrWhiteSpace(feature.RuleId))
                errors.Add($"UNEXPLAINED_PUBLIC_FEATURE:{feature.Id}");
            if (!double.IsFinite(feature.Position.X) || !double.IsFinite(feature.Position.Y))
                errors.Add($"INVALID_FEATURE_POSITION:{feature.Id}");
        }
        return errors.Distinct(StringComparer.Ordinal).ToArray();
    }

    public IReadOnlyList<WorldObject> Compile(SettlementDesignResult result, CancellationToken token = default)
    {
        var errors = Validate(result,token);
        if (errors.Count != 0)
            throw new InvalidOperationException("Settlement design rejected: " + string.Join("; ", errors));
        return new WorldPlanCompiler().Compile(result.Design,token);
    }
}
