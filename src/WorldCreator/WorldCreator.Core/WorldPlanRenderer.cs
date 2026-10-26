using System.Globalization;
using System.Xml.Linq;
using WorldCreator.Core.Geometry;

namespace WorldCreator.Core;

/// <summary>Diagnostic map of actual compiled geometry. This supplies no production artwork.</summary>
public interface IWorldPlanRenderer { string Render(WorldDocument world,CancellationToken token=default); }
public interface IWorldPlanPalette { string StructureColor(string definitionId); string SurfaceColor(string materialId); }
public sealed class NeutralWorldPlanPalette : IWorldPlanPalette
{ public string StructureColor(string definitionId)=>"#cbab83"; public string SurfaceColor(string materialId)=>"#c8c8bc"; }
public sealed class WorldPlanSvgRenderer(IWorldPlanPalette? palette=null) : IWorldPlanRenderer
{
    public string Render(WorldDocument world,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(world); token.ThrowIfCancellationRequested();
        var input=world.Request.Design ?? throw new ArgumentException("MOCKUP_DESIGN_REQUIRED");
        XNamespace ns="http://www.w3.org/2000/svg";
        string N(double n)=>n.ToString("0.###",CultureInfo.InvariantCulture);
        double w=world.Width*input.MetersPerCell,h=world.Height*input.MetersPerCell;
        var root=new XElement(ns+"svg",new XAttribute("viewBox",$"0 0 {N(w)} {N(h)}"),new XAttribute("width",1536),new XAttribute("height",(int)Math.Ceiling(1536*h/w)),
            new XElement(ns+"title",$"{input.Blueprint.MockupId}: generated engine geometry, not production artwork"),
            new XElement(ns+"rect",new XAttribute("width",N(w)),new XAttribute("height",N(h)),new XAttribute("fill","#eddfbb")));
        var regions=input.Blueprint.Regions.ToDictionary(r=>r.Id,StringComparer.Ordinal);
        foreach(var group in input.TerrainRules.GroupBy(r=>r.Priority).OrderBy(g=>g.Key))
        {
            var groupIds=group.Select(r=>r.RegionId).ToHashSet(StringComparer.Ordinal);
            foreach(var gap in (input.SurfaceClearances??[]).Where(r=>r.GapMaterialId is not null&&r.MinimumGapMeters>0&&groupIds.Contains(r.RegionId)))
                root.Add(new XElement(ns+"polygon",new XAttribute("points",string.Join(" ",regions[gap.RegionId].Outline.Vertices.Select(p=>$"{N(p.X)},{N(p.Y)}"))),
                    new XAttribute("fill","none"),new XAttribute("stroke",(palette??new NeutralWorldPlanPalette()).SurfaceColor(gap.GapMaterialId!)),
                    new XAttribute("stroke-width",N(gap.MinimumGapMeters*2)),new XAttribute("stroke-linejoin","round"),new XElement(ns+"title","spacing-band:"+gap.Id)));
        foreach(var terrain in group.OrderBy(r=>r.RegionId,StringComparer.Ordinal))
            Polygon(regions[terrain.RegionId].Outline,terrain.SurfaceMaterialId is { } material ? (palette??new NeutralWorldPlanPalette()).SurfaceColor(material)
                : terrain.Kind switch { CellKind.Grass=>"#b5c99a",CellKind.Water=>"#85b6c2",CellKind.Rock=>"#aaa599",_=>"#eddfbb" },terrain.RegionId);
        }
        foreach(var zone in (world.Landscape ?? []).Where(z=>z.Role==LandscapeZoneRole.GroundCover)) Polygon(zone.Boundary,"#9bbd83",zone.Id);
        var paths=input.Paths.ToDictionary(p=>p.Id,StringComparer.Ordinal);
        foreach(var surface in input.RoadRules.SelectMany(road=>StreetSurfaceBuilder.Build(new(road.PathId,paths[road.PathId].Centerline,road.Profile)))
            .OrderBy(s=>s.Kind==StreetSurfaceKind.Carriageway?1:0))
        { token.ThrowIfCancellationRequested(); Polygon(new(surface.Polygon),surface.Kind==StreetSurfaceKind.Carriageway?"#53636b":"#bfc4bb",surface.RoadId); }
        foreach(var feature in world.SiteFeatures ?? [])
            Polygon(feature.Boundary,feature.Kind is Planning.SiteFeatureKind.Garden or Planning.SiteFeatureKind.GreenBuffer or Planning.SiteFeatureKind.Lawn?"#9bbd83":"#c8c0ad",feature.Id);
        foreach(var item in world.Objects.Where(o=>o.VisualSource?.Footprint is not null))
        {
            token.ThrowIfCancellationRequested();
            var source=item.VisualSource!;
            string color=source.BuildingDefinitionId is { } definition ? (palette ?? new NeutralWorldPlanPalette()).StructureColor(definition)
                : item.Kind.StartsWith("vegetation:",StringComparison.Ordinal)?"#4c8555":"#cbab83";
            Polygon(source.Footprint!,color,item.Id);
            if(source.BuildingDefinitionId is not null)
            {
                double cx=source.PositionMeters.X,cy=source.PositionMeters.Y;
                root.Add(new XElement(ns+"text",new XAttribute("x",N(cx)),new XAttribute("y",N(cy)),new XAttribute("text-anchor","middle"),
                    new XAttribute("font-family","sans-serif"),new XAttribute("font-size",Math.Min(7,Math.Max(3,Math.Sqrt(Math.Abs(source.Footprint!.SignedArea()))/8))),
                    new XAttribute("fill","#182b37"),source.BuildingDefinitionId));
            }
        }
        return new XDocument(root).ToString();
        void Polygon(WorldPolygon shape,string color,string id)=>root.Add(new XElement(ns+"polygon",new XAttribute("points",string.Join(" ",shape.Vertices.Select(p=>$"{N(p.X)},{N(p.Y)}"))),
            new XAttribute("fill",color),new XAttribute("stroke","#42595f"),new XAttribute("stroke-width","0.3"),new XElement(ns+"title",id)));
    }
}
