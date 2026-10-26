using WorldCreator.Core.Cities;
using WorldCreator.Core.Geometry;

namespace WorldCreator.Worlds;

/// <summary>Recursively divides neighborhoods with connected T-junction streets; no free-point scatter.</summary>
public sealed class SubdivisionCityLayout : ICityStreetLayout
{
    public string Id => "recursive-neighborhoods";
    public CityLayoutResult Generate(CityLayoutContext context,CancellationToken token=default)
    {
        var q=context.Request;var blocks=new List<CityRegion>();var roads=new List<StreetCorridor>();int serial=0;
        Split(context.Region,0);return new(blocks,roads);
        void Split(CityRegion region,int depth)
        {
            token.ThrowIfCancellationRequested();
            if(depth>24 || blocks.Count+roads.Count>2*context.BlockBudget)throw new ArgumentException("CITY_SUBDIVISION_BUDGET");
            var b=region.Bounds;
            bool x=b.Width/q.TargetBlockWidthMeters>=b.Height/q.TargetBlockDepthMeters;
            bool canX=b.Width>=2*q.MinimumBlockSpanMeters/0.84,canY=b.Height>=2*q.MinimumBlockSpanMeters/0.84;
            if((b.Width<=q.TargetBlockWidthMeters*1.35 && b.Height<=q.TargetBlockDepthMeters*1.35) || !canX && !canY)
            {
                if(blocks.Count>=context.BlockBudget || b.Width<q.MinimumBlockSpanMeters || b.Height<q.MinimumBlockSpanMeters)throw new ArgumentException("CITY_BLOCK_BUDGET_OR_SPAN");
                blocks.Add(region with{Id=context.Region.Id+"-leaf-"+blocks.Count});return;
            }
            if(x&&!canX)x=false;else if(!x&&!canY)x=true;
            double ratio=0.42+0.04*((q.Variation+depth)%5);string id=context.Region.Id+"-branch-"+serial++;
            if(x)
            {
                double cut=b.X+b.Width*ratio;
                roads.Add(new(id,new([new(cut,b.Y),new(cut,b.Bottom)]),q.StreetProfile));
                Split(region with{Bounds=new(b.X,b.Y,cut-b.X,b.Height),EastRoadId=id},depth+1);
                Split(region with{Bounds=new(cut,b.Y,b.Right-cut,b.Height),WestRoadId=id},depth+1);
            }
            else
            {
                double cut=b.Y+b.Height*ratio;
                roads.Add(new(id,new([new(b.X,cut),new(b.Right,cut)]),q.StreetProfile));
                Split(region with{Bounds=new(b.X,b.Y,b.Width,cut-b.Y),SouthRoadId=id},depth+1);
                Split(region with{Bounds=new(b.X,cut,b.Width,b.Bottom-cut),NorthRoadId=id},depth+1);
            }
        }
    }
}
