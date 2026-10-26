using WorldCreator.Core.Cities;
using WorldCreator.Core.Geometry;

namespace WorldCreator.Worlds;

/// <summary>Computes an orthogonal street grid and its blocks from dimensions and frontage rhythm.</summary>
public sealed class RectilinearCityLayout : ICityStreetLayout
{
    public string Id => "rectilinear-grid";
    public CityLayoutResult Generate(CityLayoutContext context,CancellationToken token=default)
    {
        var r=context.Region;var q=context.Request;
        int nx=Math.Max(1,(int)Math.Floor(r.Bounds.Width/q.TargetBlockWidthMeters));
        int ny=Math.Max(1,(int)Math.Floor(r.Bounds.Height/q.TargetBlockDepthMeters));
        if((long)nx*ny>context.BlockBudget)throw new ArgumentException("CITY_BLOCK_BUDGET");
        double[] Axis(double start,double span,int count)
        {
            double[] weights=Enumerable.Range(0,count).Select(i=>q.Variation==0?1d:1+0.12*((i+q.Variation)%3-1)).ToArray();
            double total=weights.Sum(),position=start;var values=new List<double>{start};
            foreach(double weight in weights){position+=span*weight/total;values.Add(position);}values[^1]=start+span;
            if(values.Zip(values.Skip(1),(a,b)=>b-a).Any(length=>length<q.MinimumBlockSpanMeters))throw new ArgumentException("CITY_GRID_BLOCK_TOO_SMALL");
            return values.ToArray();
        }
        var xs=Axis(r.Bounds.X,r.Bounds.Width,nx);var ys=Axis(r.Bounds.Y,r.Bounds.Height,ny);
        var roads=new List<StreetCorridor>();
        var vertical=new List<string>{r.WestRoadId};var horizontal=new List<string>{r.NorthRoadId};
        for(int i=1;i<nx;i++){token.ThrowIfCancellationRequested();string id=r.Id+"-grid-v-"+i;vertical.Add(id);roads.Add(new(id,new([new(xs[i],ys[0]),new(xs[i],ys[^1])]),q.StreetProfile));}
        for(int i=1;i<ny;i++){token.ThrowIfCancellationRequested();string id=r.Id+"-grid-h-"+i;horizontal.Add(id);roads.Add(new(id,new([new(xs[0],ys[i]),new(xs[^1],ys[i])]),q.StreetProfile));}
        vertical.Add(r.EastRoadId);horizontal.Add(r.SouthRoadId);
        var blocks=new List<CityRegion>();
        for(int y=0;y<ny;y++)for(int x=0;x<nx;x++)
        {
            token.ThrowIfCancellationRequested();
            blocks.Add(new(r.Id+"-block-"+x+"-"+y,new(xs[x],ys[y],xs[x+1]-xs[x],ys[y+1]-ys[y]),horizontal[y],vertical[x+1],horizontal[y+1],vertical[x]));
        }
        return new(blocks,roads);
    }
}
