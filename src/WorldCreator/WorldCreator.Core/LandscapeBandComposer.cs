using WorldCreator.Core.Geometry;

namespace WorldCreator.Core.Planning;

/// <summary>Game-art composition grammar: organized planting bands inside an approved landscape zone.</summary>
public sealed record LandscapeBandStyle(string Id, string MaterialId, double BorderMeters,
    double BandWidthMeters, double GapMeters)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(MaterialId) ||
            !double.IsFinite(BorderMeters) || BorderMeters < 0 ||
            !double.IsFinite(BandWidthMeters) || BandWidthMeters <= 0 ||
            !double.IsFinite(GapMeters) || GapMeters < 0)
            throw new ArgumentException("Invalid game-art landscape band style.");
    }
}
public sealed record PlannedLandscapeBand(string Id, string ParentZoneId,
    string MaterialId, WorldPolygon Shape, string RuleId);
public sealed record LandscapeCompositionResult(IReadOnlyList<PlannedLandscapeBand> Bands,
    IReadOnlyList<string> Diagnostics);

/// <summary>
/// Produces a coherent series of garden/ground-cover beds from a zone's visual grammar.
/// No randomly selected positions or finding vacant cells. Nonrectangular regions are
/// explicitly deferred rather than silently approximated.
/// </summary>
public sealed class LandscapeBandComposer
{
    public LandscapeCompositionResult Compose(LandscapeZone zone, LandscapeBandStyle style)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(style);
        style.Validate();
        zone.Boundary.Validate();
        var v = zone.Boundary.Vertices;
        var xs = v.Select(p => p.X).Distinct().Order().ToArray();
        var ys = v.Select(p => p.Y).Distinct().Order().ToArray();
        if (v.Count != 4 || xs.Length != 2 || ys.Length != 2)
            return new([], ["UNSUPPORTED_LANDSCAPE_GEOMETRY:" + zone.Id]);
        var left = xs[0] + style.BorderMeters;
        var right = xs[1] - style.BorderMeters;
        var top = ys[0] + style.BorderMeters;
        var bottom = ys[1] - style.BorderMeters;
        if (left >= right || top >= bottom)
            return new([], ["LANDSCAPE_BORDER_EXCEEDS_ZONE:" + zone.Id]);
        var bands = new List<PlannedLandscapeBand>();
        double pitch = style.BandWidthMeters + style.GapMeters;
        for (int index = 0; index < 10000; index++)
        {
            double y0 = top + index * pitch;
            if (y0 + style.BandWidthMeters > bottom + 1e-8) break;
            double y1 = y0 + style.BandWidthMeters;
            bands.Add(new($"{zone.Id}-{style.Id}-{index}", zone.Id, style.MaterialId,
                new WorldPolygon([
                    new WorldPoint(left, y0), new WorldPoint(right, y0),
                    new WorldPoint(right, y1), new WorldPoint(left, y1)
                ]), style.Id));
        }
        if (bands.Count == 0)
            return new([], ["NO_BAND_FITS_ZONE:" + zone.Id]);
        return new(bands, []);
    }
}
