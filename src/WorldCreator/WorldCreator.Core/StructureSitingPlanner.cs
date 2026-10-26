using WorldCreator.Core.Geometry;

namespace WorldCreator.Core.Planning;

/// <summary>Evaluates the exact computed structures against authored functional context.
/// No fallback sources, automatic exemptions, invented resource capacity or relocation.</summary>
public sealed class StructureSitingPlanner(ISettlementCatalogRegistry catalogs)
{
    private sealed record Consumer(PlannedWorldObject Object,BuildingDefinition Definition,
        StructureSitingContext Context,WorldPoint Terminal);

    public WorldSitingAssessment Evaluate(MockupWorldDesign design,IReadOnlyList<PlannedWorldObject> objects,
        IReadOnlyList<BuildingSiteFeature> surroundings,IReadOnlyList<LandscapeZone> landscape,
        CancellationToken token=default, IReadOnlyList<PublicFeature>? publicFeatures=null)
    {
        token.ThrowIfCancellationRequested();
        try { design=CatalogContentAuthoring.BindReferences(design,catalogs,token); }
        catch (ArgumentException ex) { return new(SitingEvaluationStatus.Failed,[new("catalog",ex.Message,SitingEvaluationStatus.Failed,"Resolve explicit source-catalog definitions before siting.",[])],[]); }
        var invalid=new StructureSitingValidator(catalogs).Validate(design,token);
        if (invalid.Count!=0) return new(SitingEvaluationStatus.Failed,invalid.Select(i=>
            new SitingRuleEvaluation(i.ObjectId,i.Code,SitingEvaluationStatus.Unresolved,i.Message,[])).ToArray(),[]);
        var pack=catalogs.Resolve(design.Catalog!);
        var plan=design.Siting!;
        var evaluations=new List<SitingRuleEvaluation>(); var allocations=new List<SitingResourceAllocation>();
        var regions=design.Blueprint.Regions.ToDictionary(r=>r.Id,StringComparer.Ordinal);
        var paths=design.Paths.ToDictionary(p=>p.Id,StringComparer.Ordinal);
        var catalog=design.BuildingCatalog.ToDictionary(d=>d.Id,StringComparer.Ordinal);
        var sources=plan.Sources.ToDictionary(s=>s.Id,StringComparer.Ordinal);
        var links=plan.Links.ToDictionary(l=>l.Id,StringComparer.Ordinal);
        var sourceUsed=plan.Sources.ToDictionary(s=>s.Id,_=>0d,StringComparer.Ordinal);
        var linkUsed=plan.Links.ToDictionary(l=>l.Id,_=>0d,StringComparer.Ordinal);
        var consumers=new List<Consumer>();
        foreach (var item in objects.Where(o=>o.ParentObjectId is null && o.VisualSource?.BuildingDefinitionId is not null).OrderBy(o=>o.Id,StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            var source=item.VisualSource!;
            var rule=design.SettlementRules.SingleOrDefault(r=>r.Id==source.FeatureId);
            var infrastructure=(design.InfrastructureRules ?? []).SingleOrDefault(r=>r.Id==source.FeatureId);
            var context=rule?.Siting ?? infrastructure!.Siting!;
            var definition=pack.Policy.Resolve(catalog[source.BuildingDefinitionId!],context.Technology);
            var terminal=rule is not null ? new WorldPoint(source.PositionMeters.X,paths[rule.FrontagePathId].Centerline.Points[0].Y)
                : paths[infrastructure!.AlignmentPathId].Centerline.Points[0];
            consumers.Add(new(item,definition,context,terminal));
        }
        var byId=consumers.ToDictionary(c=>c.Object.Id,StringComparer.Ordinal);
        var providerEdges=consumers.ToDictionary(c=>c.Object.Id,_=>new HashSet<string>(StringComparer.Ordinal),StringComparer.Ordinal);
        foreach (var group in plan.Sources.Where(s=>s.ProviderObjectId is not null).GroupBy(s=>(s.ProviderObjectId,s.Kind)))
        {
            token.ThrowIfCancellationRequested();
            var supply=group.First();
            if (!byId.TryGetValue(supply.ProviderObjectId!,out var provider) ||
                provider.Definition.SitingProfile!.Provides?.Contains(supply.Kind)!=true ||
                group.Any(s=>s.Unit!=provider.Definition.SitingProfile.OutputUnit || !provider.Object.VisualSource!.Footprint!.ContainsPoint(s.Port)) ||
                group.Sum(s=>s.Capacity)>provider.Object.VisualSource!.ServiceCapacity!.Value*provider.Definition.SitingProfile.OutputUnitsPerCapacity+1e-8)
                evaluations.Add(new(supply.Id,"SITING_PROVIDER_EXPORT_INVALID",SitingEvaluationStatus.Failed,
                    "Provider output type, location, units or declared aggregate capacity is invalid.",["R22"]));
        }
        foreach (var consumer in consumers)
        {
            token.ThrowIfCancellationRequested();
            var profile=consumer.Definition.SitingProfile!;
            var footprint=consumer.Object.VisualSource!.Footprint!;
            var district=plan.Districts.Single(d=>d.Id==consumer.Context.DistrictId);
            Record(consumer,"SITING_DISTRICT_ACCEPTED",SitingEvaluationStatus.Passed,"Compatible district "+district.Id+" / "+district.Use);
            if (footprint.Vertices.Any(p=>!regions[district.RegionId].Outline.ContainsPoint(p)))
                Record(consumer,"SITING_FOOTPRINT_OUTSIDE_DISTRICT",SitingEvaluationStatus.Failed,"Physical structure extends outside its district.");
            var ownSurroundings=surroundings.Where(s=>s.BuildingId==consumer.Object.Id).ToArray();
            foreach (var required in profile.RequiredSurroundings)
                Record(consumer,"SITING_SURROUNDINGS_"+required,ownSurroundings.Any(s=>s.Kind==required)?SitingEvaluationStatus.Passed:SitingEvaluationStatus.Failed,
                    "Required planned surrounding use: "+required);
            foreach (var site in ownSurroundings)
                if (site.Boundary.Vertices.Any(p=>!regions[district.RegionId].Outline.ContainsPoint(p)))
                    Record(consumer,"SITING_SURROUNDINGS_OUTSIDE_DISTRICT",SitingEvaluationStatus.Failed,site.Id);
            foreach (var requirement in profile.Requirements.Where(r=>r.WhenTechnologies is null || r.WhenTechnologies.Contains(consumer.Context.Technology))
                         .OrderBy(r=>r.Id,StringComparer.Ordinal))
            {
                token.ThrowIfCancellationRequested();
                var binding=consumer.Context.Dependencies.Single(b=>b.RequirementId==requirement.Id);
                var supply=sources[binding.SourceId];
                double quantity=requirement.BaseQuantity+consumer.Object.VisualSource.ServiceCapacity!.Value*requirement.QuantityPerCapacity;
                int failures=evaluations.Count(e=>e.ObjectId==consumer.Object.Id && e.RuleId=="SITING_DEPENDENCY_FAILED");
                void Reject(string reason) => Record(consumer,"SITING_DEPENDENCY_FAILED",SitingEvaluationStatus.Failed,
                    requirement.Id+": "+reason,binding.SourceId,quantity,requirement.Unit,binding.LinkIds);
                if (!double.IsFinite(quantity) || quantity<=0) { Reject("invalid calculated demand"); continue; }
                if (supply.Kind!=requirement.Kind || supply.Unit!=requirement.Unit) Reject("source type or units do not match");
                if (requirement.RequiredOrigin is { } origin && supply.Origin!=origin) Reject("source origin does not match the selected technology");
                if (requirement.RequiredQualities?.Any(q=>!supply.Qualities.Contains(q))==true) Reject("source quality/reliability is incompatible");
                foreach (var reason in pack.Policy.ValidateSupply(design,consumer.Definition,requirement,supply,consumer.Context,token)) Reject(reason);
                if (supply.ProviderObjectId is { } provider)
                {
                    if (!byId.TryGetValue(provider,out var parent)) Reject("provider is not a planned structure");
                    else if (provider==consumer.Object.Id) Reject("a structure cannot satisfy its own input dependency");
                    else if (parent.Definition.SitingProfile!.Provides?.Contains(requirement.Kind)!=true) Reject("provider does not produce the required resource/service");
                    else if (!parent.Object.VisualSource!.Footprint!.ContainsPoint(supply.Port)) Reject("provider port is outside its footprint");
                    else providerEdges[consumer.Object.Id].Add(provider);
                }
                var usedLinks=new List<string>();
                if (requirement.SourceContainsSite)
                {
                    if (footprint.Vertices.Any(p=>!regions[supply.RegionId].Outline.ContainsPoint(p))) Reject("resource area does not contain the full site");
                    if (binding.LinkIds.Count!=0) Reject("contained resources do not consume transport links");
                }
                else
                {
                    var terminal=binding.ServicePointMeters ?? consumer.Terminal;
                    if (binding.ServicePointMeters is not null && !footprint.ContainsPoint(terminal) &&
                        !ownSurroundings.Any(s=>s.Kind is SiteFeatureKind.AccessWalkway or SiteFeatureKind.Forecourt && s.Boundary.ContainsPoint(terminal)))
                        Reject("service point is not on this structure or its arrival/access space");
                    var selected=binding.LinkIds.Select(id=>links[id]).ToArray();
                    if (selected.Length==0 || selected.Any(l=>l.Mode!=requirement.Mode || l.Unit!=requirement.Unit || !l.Services!.Contains(requirement.Kind)))
                        Reject("a compatible typed connection is required");
                    else
                    {
                        foreach (var link in selected.Where(l=>l.Mode==SitingNetworkMode.Road))
                        {
                            var section=design.RoadRules.Single(r=>r.PathId==link.PathId).Profile;
                            if (section.LaneWidthMeters*section.LaneCount+1e-8<requirement.MinimumRouteWidthMeters)
                                Reject("a segment of the required route is too narrow");
                        }
                        var route=FindRoute(selected,supply.Port,terminal,token);
                        if (route is null) Reject("source and service entrance are disconnected");
                        else
                        {
                            usedLinks=route.Value.Links;
                            if (requirement.MaximumRouteLengthMeters is { } max && route.Value.Distance>max+1e-8)
                                Reject("service catchment route exceeds its configured distance");
                        }
                    }
                }
                if (sourceUsed[supply.Id]+quantity>supply.Capacity+1e-8) Reject("shared source capacity exceeded");
                foreach (var id in usedLinks)
                    if (linkUsed[id]+quantity>links[id].Capacity+1e-8) Reject("shared link capacity exceeded: "+id);
                if (evaluations.Count(e=>e.ObjectId==consumer.Object.Id && e.RuleId=="SITING_DEPENDENCY_FAILED")==failures)
                {
                    sourceUsed[supply.Id]+=quantity;
                    foreach (var id in usedLinks) linkUsed[id]+=quantity;
                    allocations.Add(new(consumer.Object.Id,requirement.Id,supply.Id,usedLinks,quantity,requirement.Unit));
                    Record(consumer,"SITING_DEPENDENCY_ACCEPTED",SitingEvaluationStatus.Passed,requirement.Id+" supplied by "+supply.Id,
                        supply.Id,quantity,requirement.Unit,usedLinks);
                }
            }
        }
        foreach (var zone in plan.Exclusions)
        {
            token.ThrowIfCancellationRequested();
            foreach (var consumer in consumers)
                if (consumer.Definition.SitingProfile!.Tags.Overlaps(zone.ExcludedTags) &&
                    Intersects(consumer.Object.VisualSource!.Footprint!,regions[zone.RegionId].Outline))
                    Record(consumer,"SITING_EXCLUSION_CONFLICT",SitingEvaluationStatus.Failed,zone.Id+" / "+zone.RuleId);
            if (zone.ExcludedTags.Contains("public-green"))
                foreach (var green in landscape)
                    if (Intersects(green.Boundary,regions[zone.RegionId].Outline))
                        evaluations.Add(new(green.Id,"SITING_GREEN_EXCLUSION_CONFLICT",SitingEvaluationStatus.Failed,zone.Id,["R17","R26"]));
            foreach (var feature in publicFeatures ?? [])
                if ((zone.ExcludedTags.Contains("public-prop") || feature.Kind==PublicFeatureKind.Streetlight && zone.ExcludedTags.Contains("night-lighting")) &&
                    regions[zone.RegionId].Outline.DistanceToPoint(feature.Position)<=feature.ClearanceRadiusMeters+1e-8)
                    evaluations.Add(new(feature.Id,"SITING_PUBLIC_REALM_EXCLUSION_CONFLICT",SitingEvaluationStatus.Failed,zone.Id,["R21","R26"]));
        }
        for (int i=0;i<consumers.Count;i++)
        for (int j=0;j<i;j++)
        {
            token.ThrowIfCancellationRequested();
            var a=consumers[i]; var b=consumers[j];
            var aTags=a.Definition.SitingProfile!.Tags; var bTags=b.Definition.SitingProfile!.Tags;
            var applicable=plan.Separations.Where(r=>aTags.Contains(r.FirstTag)&&bTags.Contains(r.SecondTag) ||
                bTags.Contains(r.FirstTag)&&aTags.Contains(r.SecondTag)).ToArray();
            bool needsPolicy=pack.Policy.RequiresNeighborPolicy(aTags,bTags);
            if (needsPolicy && applicable.Length==0)
                Record(a,"SITING_SEPARATION_UNRESOLVED",SitingEvaluationStatus.Unresolved,"Missing explicit neighbor policy for "+b.Object.Id);
            foreach (var rule in applicable)
            {
                double distance=Distance(a.Object.VisualSource!.Footprint!,b.Object.VisualSource!.Footprint!);
                evaluations.Add(new(a.Object.Id,"SITING_SEPARATION_"+rule.Id,distance+1e-8>=rule.MinimumMeters?
                    SitingEvaluationStatus.Passed:SitingEvaluationStatus.Failed,
                    $"Neighbor {b.Object.Id}: edge clearance {distance:R}m; minimum {rule.MinimumMeters:R}m; {rule.ParameterOrigin}",[rule.SourceId]));
            }
        }
        var activeStack=new HashSet<string>(StringComparer.Ordinal); var done=new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in providerEdges.Keys.Order(StringComparer.Ordinal)) Visit(id);
        var reservations=new List<SitingNetworkReservation>();
        foreach (var link in plan.Links.Where(l=>l.Mode is SitingNetworkMode.Utility or SitingNetworkMode.SiteInterface).OrderBy(l=>l.Id,StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            foreach (var surface in StreetSurfaceBuilder.Build(new(link.Id,paths[link.PathId].Centerline,new(link.ReservationWidthMeters,1,0,0,0,0))))
            {
                var polygon=new WorldPolygon(surface.Polygon);
                if (polygon.Vertices.Any(p=>!regions[paths[link.PathId].RegionId].Outline.ContainsPoint(p)))
                    evaluations.Add(new(link.Id,"SITING_UTILITY_CORRIDOR_OUTSIDE_REGION",SitingEvaluationStatus.Failed,link.RuleId,[]));
                foreach (var consumer in consumers)
                    if (Intersects(polygon,consumer.Object.VisualSource!.Footprint!) && !plan.Sources.Any(s=>s.ProviderObjectId==consumer.Object.Id &&
                        allocations.Any(a=>a.SourceId==s.Id && a.LinkIds.Contains(link.Id))) &&
                        !(link.Mode==SitingNetworkMode.SiteInterface && allocations.Any(a=>a.ObjectId==consumer.Object.Id && a.LinkIds.Contains(link.Id))))
                        Record(consumer,"SITING_UTILITY_CORRIDOR_BUILDING_CONFLICT",SitingEvaluationStatus.Failed,link.Id);
                reservations.Add(new(link.Id,polygon,link.RuleId));
            }
        }
        var status=evaluations.Any(e=>e.Status==SitingEvaluationStatus.Failed)?SitingEvaluationStatus.Failed:
            evaluations.Any(e=>e.Status==SitingEvaluationStatus.Unresolved)?SitingEvaluationStatus.Unresolved:SitingEvaluationStatus.Passed;
        return new(status,evaluations.OrderBy(e=>e.ObjectId,StringComparer.Ordinal).ThenBy(e=>e.RuleId,StringComparer.Ordinal)
            .ThenBy(e=>e.DependencyId,StringComparer.Ordinal).ToArray(),allocations,reservations);

        void Record(Consumer c,string rule,SitingEvaluationStatus status,string message,string? source=null,double? quantity=null,
            string? unit=null,IReadOnlyList<string>? used=null) => evaluations.Add(new(c.Object.Id,rule,status,message,
                c.Definition.SitingProfile!.SourceIds,source,quantity,unit,used));
        void Visit(string id)
        {
            token.ThrowIfCancellationRequested();
            if (done.Contains(id)) return;
            if (!activeStack.Add(id)) { Record(byId[id],"SITING_PROVIDER_DEPENDENCY_CYCLE",SitingEvaluationStatus.Failed,"Provider graph is cyclic."); return; }
            if (activeStack.Count>256) { Record(byId[id],"SITING_PROVIDER_DEPTH_EXCEEDED",SitingEvaluationStatus.Failed,"Provider graph exceeds depth budget."); activeStack.Remove(id); return; }
            foreach (var parent in providerEdges[id].Order(StringComparer.Ordinal)) Visit(parent);
            activeStack.Remove(id); done.Add(id);
        }
        (List<string> Links,double Distance)? FindRoute(SitingNetworkLink[] selected,WorldPoint from,WorldPoint to,CancellationToken cancellation)
        {
            var topology=StreetTopologyBuilder.Build(selected.Select(l=>new StreetCorridor(l.Id,paths[l.PathId].Centerline,new(1,1,0,0,0,0))).ToArray(),[from,to],cancellation);
            if (topology.Diagnostics.Any(d=>!d.StartsWith("DISCONNECTED_STREET_GRAPH",StringComparison.Ordinal))) return null;
            var start=topology.Nodes.SingleOrDefault(n=>n.Location.DistanceTo(from)<1e-8);
            var end=topology.Nodes.SingleOrDefault(n=>n.Location.DistanceTo(to)<1e-8);
            if (start is null || end is null) return null;
            var distances=topology.Nodes.ToDictionary(n=>n.Id,_=>double.PositiveInfinity);
            var previous=new Dictionary<string,(string Node,string Link)>();
            var pending=new PriorityQueue<string,(double,string)>(); distances[start.Id]=0; pending.Enqueue(start.Id,(0,start.Id));
            while (pending.TryDequeue(out var current,out var priority))
            {
                cancellation.ThrowIfCancellationRequested();
                if (priority.Item1>distances[current]) continue;
                if (current==end.Id) break;
                foreach (var edge in topology.Edges.Where(e=>e.FromId==current || e.ToId==current).OrderBy(e=>e.Id,StringComparer.Ordinal))
                {
                    string next=edge.FromId==current?edge.ToId:edge.FromId; double value=distances[current]+edge.LengthMeters;
                    if (value>=distances[next]-1e-8) continue;
                    distances[next]=value; previous[next]=(current,edge.CorridorId); pending.Enqueue(next,(value,next));
                }
            }
            if (!double.IsFinite(distances[end.Id])) return null;
            var used=new HashSet<string>(StringComparer.Ordinal); var cursor=end.Id;
            while (cursor!=start.Id) { var step=previous[cursor]; used.Add(step.Link); cursor=step.Node; }
            return (used.Order(StringComparer.Ordinal).ToList(),distances[end.Id]);
        }
    }

    private static bool Intersects(WorldPolygon a,WorldPolygon b) => LandscapePolygonTriangulator.Triangulate(a)
        .Any(triangle=>LandscapePolygonTriangulator.Triangulate(b).Any(other=>InfrastructureGeometry.Overlap(triangle,other)));
    private static double Distance(WorldPolygon a,WorldPolygon b) => Intersects(a,b)?0:
        Math.Min(a.Vertices.Min(b.DistanceToPoint),b.Vertices.Min(a.DistanceToPoint));
}
