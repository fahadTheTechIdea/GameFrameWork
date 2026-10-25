using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core.Planning;

public sealed record BuildingSizeResult(WorldPolygon? Footprint,
    BuildingSizeVariant? Variant, int RequiredCapacity, IReadOnlyList<string> Issues)
{
    public bool Valid => Footprint is not null && Variant is not null && Issues.Count == 0;
}

/// <summary>Resolves scale from archetype capacity, settlement demand and an optional
/// authored override, then fits an unscaled size variant into an existing envelope.</summary>
public sealed class BuildingSizePlanner
{
    public BuildingSizeResult Plan(BuildableParcel parcel, BuildingEnvelopeRules envelopeRules,
        BuildingDefinition definition, string? variantId = null, int requiredCapacity = 0,
        SettlementDemand? settlement = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        definition.Validate();
        if (definition.PlacementKind != BuildingPlacementKind.FrontageSite)
            return new(null,null,0,["BUILDING_PLACEMENT_KIND_UNSUPPORTED"]);
        settlement?.Validate();
        if (requiredCapacity < 0) throw new ArgumentOutOfRangeException(nameof(requiredCapacity));
        BuildingSizeResult Reject(string code, int demand = 0) => new(null, null, demand, [code]);
        if (definition.Id != envelopeRules.BuildingType) return Reject("BUILDING_DEFINITION_MISMATCH");
        if (definition.SizeVariants is not { Count: > 0 }) return Reject("BUILDING_SIZE_VARIANTS_REQUIRED");
        if (settlement is not null && !definition.AllowedClasses.Contains(settlement.Class))
            return Reject("BUILDING_SETTLEMENT_CLASS_UNSUPPORTED");
        if (definition.AllowedRoles is not null && settlement is null)
            return Reject("BUILDING_SETTLEMENT_ROLE_REQUIRED");
        if (settlement is not null && definition.AllowedRoles is { } roles && !roles.Contains(settlement.Role))
            return Reject("BUILDING_SETTLEMENT_ROLE_UNSUPPORTED");
        if (requiredCapacity == 0 && definition.ScaleDemandPerResident > 0 && settlement is null && variantId is null)
            return Reject("BUILDING_SCALE_CONTEXT_REQUIRED");
        double calculated = requiredCapacity > 0 ? requiredCapacity :
            Math.Max(definition.ServiceCapacity, Math.Ceiling((settlement?.Population ?? 0) * definition.ScaleDemandPerResident));
        if (!double.IsFinite(calculated) || calculated > int.MaxValue)
            return Reject("BUILDING_CAPACITY_UNSUPPORTED");
        int demand = (int)calculated;
        var envelope = BuildingEnvelopePlanner.Plan(parcel, envelopeRules);
        if (!envelope.IsFeasible) return new(null, null, demand, envelope.Diagnostics);
        var candidates = definition.SizeVariants.Where(v => variantId is null || v.Id == variantId).ToArray();
        if (candidates.Length == 0) return Reject("BUILDING_SIZE_VARIANT_UNKNOWN", demand);
        var boundary = envelope.Footprint!.Vertices;
        double minX = boundary.Min(p => p.X), maxX = boundary.Max(p => p.X);
        double minY = boundary.Min(p => p.Y), maxY = boundary.Max(p => p.Y);
        bool frontNorth = Math.Abs(parcel.StreetAccess.Y - parcel.Boundary.Vertices.Min(p => p.Y)) < 1e-8;
        foreach (var variant in candidates.OrderBy(v => v.WidthMeters * v.DepthMeters)
                     .ThenBy(v => v.ServiceCapacity).ThenBy(v => v.Id, StringComparer.Ordinal))
        {
            if (variant.ServiceCapacity < demand ||
                settlement is not null && variant.AllowedClasses is { } classes && !classes.Contains(settlement.Class)) continue;
            double clearance = variant.SiteClearanceMeters;
            if (variant.WidthMeters + 2*clearance > maxX-minX + 1e-8 ||
                variant.DepthMeters + 2*clearance > maxY-minY + 1e-8) continue;
            double left = (minX+maxX-variant.WidthMeters)/2;
            double top = frontNorth ? minY+clearance : maxY-clearance-variant.DepthMeters;
            return new(new WorldPolygon([new(left,top), new(left+variant.WidthMeters,top),
                new(left+variant.WidthMeters,top+variant.DepthMeters), new(left,top+variant.DepthMeters)]),
                variant, demand, []);
        }
        return Reject("BUILDING_SIZE_OR_CAPACITY_DOES_NOT_FIT", demand);
    }
}
