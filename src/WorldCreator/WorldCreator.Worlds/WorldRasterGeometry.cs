using WorldCreator.Core;
using WorldCreator.Core.Geometry;

namespace WorldCreator.Worlds;

internal static class WorldRasterGeometry
{
    public static GridPoint Project(WorldPoint p, double scale) => new((int)Math.Floor(p.X/scale), (int)Math.Floor(p.Y/scale));

    public static IEnumerable<GridPoint> CoveredCells(WorldPolygon polygon, GridBounds bounds, double scale,
        CancellationToken token = default)
    {
        int left = Math.Max(0, (int)Math.Floor(polygon.Vertices.Min(p => p.X)/scale));
        int top = Math.Max(0, (int)Math.Floor(polygon.Vertices.Min(p => p.Y)/scale));
        int right = Math.Min(bounds.Width-1, (int)Math.Ceiling(polygon.Vertices.Max(p => p.X)/scale)-1);
        int bottom = Math.Min(bounds.Height-1, (int)Math.Ceiling(polygon.Vertices.Max(p => p.Y)/scale)-1);
        for (int y = top; y <= bottom; y++)
        for (int x = left; x <= right; x++)
        {
            token.ThrowIfCancellationRequested();
            var cell = new WorldPolygon([new(x*scale,y*scale), new((x+1)*scale,y*scale),
                new((x+1)*scale,(y+1)*scale), new(x*scale,(y+1)*scale)]);
            if (TerrainCoverageTopology.IntersectionArea(polygon, cell) > 1e-8)
                yield return new(x,y);
        }
    }
}
