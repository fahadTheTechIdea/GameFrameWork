using GameFrameWork.Catalogs.Modern;
using GameFrameWork.Core;
using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;

/// <summary>Authored synthetic layout for algorithm tests. This is not approved art
/// or a production recommendation for industrial site dimensions.</summary>
internal static class MockupGenerationFixture
{
    public static WorldRequest Create(int width = 48,int height = 48,int settlementCount = 2,int seed = 42)
    {
        const double scale = 4;
        var regions = new List<VisualRegion> { new("world",Rect(0,0,width*scale,height*scale),"synthetic-desert-composition") };
        var paths = new List<VisualDesignPath> { new("collector","world",
            new([new(2*scale,4*scale),new(2*scale,(height-4)*scale)]),"road") };
        var roadRules = new List<MockupRoadRule> { new("collector",RoadClass.Collector,new(3,2,0,0,0,0)) };
        var settlements = new List<SettlementRegionRule>();
        var props = new List<PathPattern>();
        for (int i = 0; i < settlementCount; i++)
        {
            double y = (8+i*(height-12.0)/settlementCount)*scale;
            string region = "industrial-"+i, path = "service-road-"+i;
            double depth = Math.Min(12*scale,(height-12)*scale/Math.Max(1,settlementCount)-2*scale);
            regions.Add(new(region,Rect(4*scale,y,(width-4)*scale,y+depth),"industrial-block"));
            paths.Add(new(path,"world",new([new(2*scale,y),new((width-3)*scale,y)]),"road"));
            roadRules.Add(new(path,RoadClass.Local,new(3,2,0,0,0,0)));
            settlements.Add(new("warehouse-row-"+i,region,"settlement-"+i,path,"storage serving the authored industrial block",
                (width-8)*scale/4,(width-8)*scale/4,
                new("warehouse",4,4,2,12,10),[new("gate","gate","control the designed loading entrance","closed","gate-open-close")],
                "small",10,new(SettlementClass.Town,SettlementRole.Industrial,2000)));
            props.Add(new("lamps-"+i,path,"Streetlight","follow the mockup's service-road decoration rhythm",
                "synthetic-industrial-lamps",12*scale,4*scale,4*scale,-3*scale,.5));
        }
        var warehouse = new ModernCatalog().Structures.Single(d => d.Id == "warehouse") with {
            SiteRules = [new("loading-yard",SiteFeatureKind.LoadingYard,SiteBand.Rear,4),
                new("green-buffer",SiteFeatureKind.GreenBuffer,SiteBand.Left,2,2,true,"synthetic-shrub",8,1)]
        };
        return TestCatalogs.Contextualize(new(width,height,seed,Biome.Desert,settlementCount,
            new(1,new(1,"synthetic-industrial-mockup","simulated-layout-r1","synthetic-pixel-profile",regions,[]),
                scale,paths,roadRules,settlements,[new("world",CellKind.Sand,0,2,"authored-sand-plane",.001,0)],props,[warehouse],
                VegetationClearances:[new("synthetic-shrub",.5,false)])));
    }

    public static WorldPolygon Rect(double x0,double y0,double x1,double y1) =>
        new([new(x0,y0),new(x1,y0),new(x1,y1),new(x0,y1)]);
}
