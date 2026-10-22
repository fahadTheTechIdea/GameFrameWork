using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core;

/// <summary>Explicit traversable road span; endpoints and road width are in world meters.
/// This models a straight segment of a road, not arbitrary curved curb geometry.</summary>
public sealed record PedestrianCrossingSpan(string CrossingId, WorldPoint FirstCurb,
    WorldPoint SecondCurb, double RoadWidthMeters, bool GradeSeparated);

public sealed class PedestrianCrossingSpanValidator
{
    private const double Tolerance = 1e-5;
    public IReadOnlyList<string> Validate(DummyVillageWorldDocument world,
        IReadOnlyList<PedestrianCrossingSpan> spans)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(spans);
        var issues = new List<string>();
        var crossings = (world.RoadCrossings ?? []).ToDictionary(c=>c.Id,StringComparer.Ordinal);
        var roads = world.SemanticWorld.Paths.Where(p=>p.Kind=="road")
            .ToDictionary(p=>p.Id,StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var span in spans)
        {
            if (!seen.Add(span.CrossingId) || !crossings.TryGetValue(span.CrossingId,out var crossing) ||
                !roads.TryGetValue(crossing.RoadPathId,out var road) ||
                !double.IsFinite(span.RoadWidthMeters) || span.RoadWidthMeters<=0 ||
                !double.IsFinite(span.FirstCurb.X) || !double.IsFinite(span.FirstCurb.Y) ||
                !double.IsFinite(span.SecondCurb.X) || !double.IsFinite(span.SecondCurb.Y))
            {
                issues.Add("CROSSING_SPAN_INVALID:"+span.CrossingId);
                continue;
            }
            if (span.GradeSeparated)
            {
                issues.Add("CROSSING_GRADE_SEPARATED:"+span.CrossingId);
                continue;
            }
            double length=span.FirstCurb.DistanceTo(span.SecondCurb);
            if (Math.Abs(length-span.RoadWidthMeters)>Tolerance ||
                crossing.PositionMeters.DistanceTo(new WorldPoint(
                    (span.FirstCurb.X+span.SecondCurb.X)/2,
                    (span.FirstCurb.Y+span.SecondCurb.Y)/2))>Tolerance)
            {
                issues.Add("CROSSING_CURB_SPAN_MISMATCH:"+span.CrossingId);
                continue;
            }
            var points=road.Geometry.Points;
            bool perpendicular=false;
            for(int i=1;i<points.Count;i++)
            {
                var a=points[i-1];var b=points[i];
                double dx=b.X-a.X,dy=b.Y-a.Y;
                double sx=span.SecondCurb.X-span.FirstCurb.X;
                double sy=span.SecondCurb.Y-span.FirstCurb.Y;
                double roadLength=Math.Sqrt(dx*dx+dy*dy);
                if(roadLength<Tolerance)continue;
                double offset=Math.Abs((crossing.PositionMeters.X-a.X)*dy-
                    (crossing.PositionMeters.Y-a.Y)*dx)/roadLength;
                double projection=((crossing.PositionMeters.X-a.X)*dx+
                    (crossing.PositionMeters.Y-a.Y)*dy)/(roadLength*roadLength);
                if(offset<=Tolerance && projection>=-Tolerance && projection<=1+Tolerance &&
                    Math.Abs(dx*sx+dy*sy)<=Tolerance*roadLength*length)
                { perpendicular=true;break; }
            }
            if(!perpendicular)issues.Add("CROSSING_NOT_PERPENDICULAR_TO_ROAD:"+span.CrossingId);
        }
        foreach(var crossing in crossings.Values)
            if(!seen.Contains(crossing.Id))issues.Add("CROSSING_SPAN_MISSING:"+crossing.Id);
        return issues;
    }
}
