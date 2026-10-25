using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core.Planning;

/// <summary>Checks contextual design input before parcel generation or resource allocation.</summary>
public sealed class StructureSitingValidator(ISettlementCatalogRegistry catalogs)
{
    public IReadOnlyList<WorldDesignIssue> Validate(MockupWorldDesign design,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        var issues=new List<WorldDesignIssue>();
        void Fail(string code,string id,string message) => issues.Add(new(code,id,message));
        RegisteredSettlementCatalog pack;
        try { pack=catalogs.Resolve(design.Catalog!); }
        catch (ArgumentException ex) { Fail(ex.Message,"","Select a registered catalog and explicit capabilities."); return issues; }
        foreach (var capability in pack.Policy.RequiredCapabilities(design))
            if (!pack.Provenance.EnabledCapabilities.Contains(capability)) Fail("CATALOG_CAPABILITY_REQUIRED","",capability);
        foreach (var definition in design.BuildingCatalog)
        {
            try { SettlementCatalogRegistry.ValidateDefinition(pack.Descriptor,definition); }
            catch (ArgumentException ex) { Fail(ex.Message,definition.Id,"Structure vocabulary/profile is not registered by this pack."); }
        }
        if (issues.Count!=0) return issues;
        var plan=design.Siting;
        if (plan is null) { Fail("SITING_PLAN_REQUIRED","","A complete contextual siting plan is required."); return issues; }
        if (plan.SchemaVersion!=1 || plan.Districts is null || plan.Sources is null || plan.Links is null ||
            plan.Separations is null || plan.Exclusions is null)
        { Fail("SITING_PLAN_INVALID","","Version 1 and complete collections are required."); return issues; }
        if (plan.Districts.Count>10000 || plan.Sources.Count>10000 || plan.Links.Count>10000 ||
            plan.Exclusions.Count>10000 || plan.Separations.Count>10000)
        { Fail("SITING_BUDGET_EXCEEDED","","Split the contextual design into smaller regions."); return issues; }
        var regions=design.Blueprint.Regions.ToDictionary(r=>r.Id,StringComparer.Ordinal);
        var paths=design.Paths.ToDictionary(p=>p.Id,StringComparer.Ordinal);
        var catalog=design.BuildingCatalog.ToDictionary(d=>d.Id,StringComparer.Ordinal);
        var terrain=new MockupTerrainQuery(design);
        var districts=new Dictionary<string,SitingDistrict>(StringComparer.Ordinal);
        foreach (var district in plan.Districts)
        {
            token.ThrowIfCancellationRequested();
            if (district is null || string.IsNullOrWhiteSpace(district.Id) || !districts.TryAdd(district.Id,district) ||
                string.IsNullOrWhiteSpace(district.RegionId) || !regions.ContainsKey(district.RegionId) ||
                string.IsNullOrWhiteSpace(district.SettlementId) || !pack.Descriptor.Vocabulary.Districts.Contains(district.Use) || string.IsNullOrWhiteSpace(district.RuleId))
            { Fail("SITING_DISTRICT_INVALID",district?.Id ?? "","District identity, parent and provenance are required."); continue; }
            if (!ConvexParcelGeometry.IsConvex(regions[district.RegionId].Outline))
                Fail("SITING_DISTRICT_GEOMETRY_UNSUPPORTED",district.Id,"Current district containment requires convex polygons.");
        }
        if (issues.Count!=0) return issues;
        var districtArray=districts.Values.OrderBy(d=>d.Id,StringComparer.Ordinal).ToArray();
        for (int i=0;i<districtArray.Length;i++)
        for (int j=0;j<i;j++)
        {
            token.ThrowIfCancellationRequested();
            if (InfrastructureGeometry.Overlap(regions[districtArray[i].RegionId].Outline,regions[districtArray[j].RegionId].Outline))
                Fail("SITING_DISTRICT_OVERLAP",districtArray[i].Id,"District land ownership must be unambiguous.");
        }
        var sources=new Dictionary<string,SitingResourceSource>(StringComparer.Ordinal);
        foreach (var source in plan.Sources)
        {
            token.ThrowIfCancellationRequested();
            if (source is null || string.IsNullOrWhiteSpace(source.Id) || !sources.TryAdd(source.Id,source) ||
                !pack.Descriptor.Vocabulary.Resources.Contains(source.Kind) || !Enum.IsDefined(source.Origin) || string.IsNullOrWhiteSpace(source.RegionId) ||
                !regions.TryGetValue(source.RegionId,out var region) || !region.Outline.ContainsPoint(source.Port) ||
                !double.IsFinite(source.Capacity) || source.Capacity<=0 || string.IsNullOrWhiteSpace(source.Unit) ||
                source.Qualities is null || source.Qualities.Any(string.IsNullOrWhiteSpace) || string.IsNullOrWhiteSpace(source.RuleId) ||
                source.UpstreamSourceIds?.Any(string.IsNullOrWhiteSpace)==true ||
                source.Origin==SitingResourceOrigin.Facility && string.IsNullOrWhiteSpace(source.ProviderObjectId))
            { Fail("SITING_SOURCE_INVALID",source?.Id ?? "","Source identity, location, capacity, units and provenance must be complete."); continue; }
            if (source.Origin==SitingResourceOrigin.SurfaceWater &&
                design.TerrainRules.SingleOrDefault(t=>t.RegionId==source.RegionId)?.Kind!=CellKind.Water)
                Fail("SITING_SOURCE_WATER_REGION_INVALID",source.Id,"Surface-water sources require an authored water mask.");
            var owners=design.TerrainRules.Where(t=>regions[t.RegionId].Outline.ContainsPoint(source.Port)).OrderByDescending(t=>t.Priority).ToArray();
            if (source.Origin==SitingResourceOrigin.SurfaceWater && (owners.Length==0 || owners[0].Kind!=CellKind.Water))
                Fail("SITING_SOURCE_WATER_REGION_INVALID",source.Id,"The water source port is not on water after terrain overlays.");
        }
        foreach (var source in sources.Values)
            foreach (var upstream in source.UpstreamSourceIds ?? [])
                if (!sources.ContainsKey(upstream)) Fail("SITING_UPSTREAM_SOURCE_MISSING",source.Id,upstream);
        if (issues.Count==0)
        {
            var visiting=new HashSet<string>(StringComparer.Ordinal); var visited=new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in sources.Keys.Order(StringComparer.Ordinal)) CheckSourceCycle(id);
            void CheckSourceCycle(string id)
            {
                token.ThrowIfCancellationRequested();
                if (visited.Contains(id)) return;
                if (!visiting.Add(id)) { Fail("SITING_WATER_NETWORK_CYCLE",id,"Upstream relations must be acyclic."); return; }
                if (visiting.Count>256) { Fail("SITING_DEPENDENCY_DEPTH_EXCEEDED",id,"Split deeply nested supply graphs."); visiting.Remove(id); return; }
                foreach (var parent in sources[id].UpstreamSourceIds ?? []) CheckSourceCycle(parent);
                visiting.Remove(id); visited.Add(id);
            }
        }
        var links=new Dictionary<string,SitingNetworkLink>(StringComparer.Ordinal);
        foreach (var link in plan.Links)
        {
            token.ThrowIfCancellationRequested();
            if (link is null || string.IsNullOrWhiteSpace(link.Id) || !links.TryAdd(link.Id,link) ||
                string.IsNullOrWhiteSpace(link.PathId) || !paths.TryGetValue(link.PathId,out var path) || !Enum.IsDefined(link.Mode) ||
                !double.IsFinite(link.Capacity) || link.Capacity<=0 || string.IsNullOrWhiteSpace(link.Unit) || string.IsNullOrWhiteSpace(link.RuleId) ||
                link.Services is null || link.Services.Count==0 || link.Services.Any(s=>!pack.Descriptor.Vocabulary.Resources.Contains(s)))
            { Fail("SITING_LINK_INVALID",link?.Id ?? "","Links need an existing authored path, typed capacity and provenance."); continue; }
            if (!double.IsFinite(link.ReservationWidthMeters) || link.ReservationWidthMeters<=0)
                Fail("SITING_LINK_INVALID",link.Id,"Utility reservation width must be positive and finite.");
            string role=link.Mode switch { SitingNetworkMode.Utility=>"utility",SitingNetworkMode.Road=>"road",
                SitingNetworkMode.Railway=>"railway",SitingNetworkMode.Pedestrian=>"pedestrian",SitingNetworkMode.Waterway=>"waterway",_=>"site-interface" };
            // A declared sidewalk is compatible with pedestrian travel; not every road has one.
            bool sidewalk=link.Mode==SitingNetworkMode.Pedestrian && path.Role=="road" && design.RoadRules.Any(r=>
                r.PathId==path.Id && r.Profile.SidewalkLeftMeters+r.Profile.SidewalkRightMeters>0);
            bool sharedUtility=link.Mode==SitingNetworkMode.Utility && path.Role=="road" && path.AdditionalNetworkModes?.Contains("utility")==true;
            if (path.Role!=role && !sidewalk && !sharedUtility) Fail("SITING_LINK_MODE_MISMATCH",link.Id,"Path role does not carry the declared network mode.");
            if (link.Mode==SitingNetworkMode.Road && !design.RoadRules.Any(r=>r.PathId==path.Id))
                Fail("SITING_LINK_ROAD_PROFILE_MISSING",link.Id,"Road supply links need a usable cross section.");
            if (link.Mode==SitingNetworkMode.Waterway && !terrain.Matches(path.Centerline,p=>terrain.KindAt(p)==CellKind.Water,token))
                Fail("SITING_WATERWAY_LINK_TERRAIN_INVALID",link.Id,"Vessel links must remain on connected water; shore transfer uses SiteInterface.");
        }
        var separationIds=new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in plan.Separations)
            if (rule is null || string.IsNullOrWhiteSpace(rule.Id) || !separationIds.Add(rule.Id) ||
                string.IsNullOrWhiteSpace(rule.FirstTag) || string.IsNullOrWhiteSpace(rule.SecondTag) ||
                !double.IsFinite(rule.MinimumMeters) || rule.MinimumMeters<0 ||
                string.IsNullOrWhiteSpace(rule.ParameterOrigin) || string.IsNullOrWhiteSpace(rule.SourceId))
                Fail("SITING_SEPARATION_INVALID",rule?.Id ?? "","Separation parameters need units, rationale and source provenance.");
        var exclusionIds=new HashSet<string>(StringComparer.Ordinal);
        foreach (var zone in plan.Exclusions)
            if (zone is null || string.IsNullOrWhiteSpace(zone.Id) || !exclusionIds.Add(zone.Id) ||
                string.IsNullOrWhiteSpace(zone.RegionId) || !regions.ContainsKey(zone.RegionId) || zone.ExcludedTags is null ||
                zone.ExcludedTags.Count==0 || zone.ExcludedTags.Any(string.IsNullOrWhiteSpace) || string.IsNullOrWhiteSpace(zone.RuleId))
                Fail("SITING_EXCLUSION_INVALID",zone?.Id ?? "","Exclusions need a region, affected structure tags and provenance.");
        foreach (var rule in design.SettlementRules)
            CheckContext(rule.Id,rule.RegionId,rule.SettlementId,rule.Envelope.BuildingType,rule.Siting);
        foreach (var rule in design.InfrastructureRules ?? [])
            CheckContext(rule.Id,rule.RegionId,rule.SettlementId,rule.DefinitionId,rule.Siting);
        return issues;

        void CheckContext(string ruleId,string regionId,string settlementId,string definitionId,StructureSitingContext? context)
        {
            token.ThrowIfCancellationRequested();
            if (context is null || string.IsNullOrWhiteSpace(context.DistrictId) || !districts.TryGetValue(context.DistrictId,out var district) ||
                !pack.Descriptor.Vocabulary.Technologies.Contains(context.Technology) || context.Dependencies is null || context.Dependencies.Count>256)
            { Fail("SITING_CONTEXT_REQUIRED",ruleId,"Each structure grammar requires a district, technology and dependency bindings."); return; }
            if (district.SettlementId!=settlementId || regions[regionId].Outline.Vertices.Any(p=>!regions[district.RegionId].Outline.ContainsPoint(p)))
                Fail("SITING_DISTRICT_CONTAINMENT_INVALID",ruleId,"The complete design region must belong to its settlement district.");
            var definition=catalog[definitionId];
            if (definition.SitingProfile is not { } profile)
            { Fail("SITING_PROFILE_REQUIRED",ruleId,"Custom structures require a functional siting profile."); return; }
            if (!profile.AllowedTechnologies.Contains(context.Technology))
            { Fail("SITING_TECHNOLOGY_UNRESOLVED",ruleId,"Select a supported explicit facility technology."); return; }
            var resolved=pack.Policy.Resolve(definition,context.Technology);
            foreach (var capability in pack.Policy.RequiredCapabilities(definition,context.Technology))
                if (!pack.Provenance.EnabledCapabilities.Contains(capability)) Fail("CATALOG_CAPABILITY_REQUIRED",ruleId,capability);
            if (!resolved.SitingProfile!.AllowedDistricts.Contains(district.Use))
                Fail("SITING_DISTRICT_INCOMPATIBLE",ruleId,"Facility use is incompatible with district "+district.Use);
            var active=resolved.SitingProfile.Requirements.Where(r=>r.WhenTechnologies is null || r.WhenTechnologies.Contains(context.Technology))
                .Select(r=>r.Id).ToHashSet(StringComparer.Ordinal);
            var bindings=new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in context.Dependencies)
                if (binding is null || string.IsNullOrWhiteSpace(binding.RequirementId) || !bindings.Add(binding.RequirementId) ||
                    !active.Contains(binding.RequirementId) || string.IsNullOrWhiteSpace(binding.SourceId) || !sources.ContainsKey(binding.SourceId) ||
                    binding.LinkIds is null || binding.LinkIds.Any(id=>string.IsNullOrWhiteSpace(id) || !links.ContainsKey(id)) ||
                    binding.ServicePointMeters is { } point && (!double.IsFinite(point.X) || !double.IsFinite(point.Y)) ||
                    binding.LinkIds.Distinct(StringComparer.Ordinal).Count()!=binding.LinkIds.Count)
                    Fail("SITING_DEPENDENCY_BINDING_INVALID",ruleId,"Binding must reference one active requirement and valid source/link IDs.");
            foreach (var missing in active.Except(bindings,StringComparer.Ordinal))
                Fail("SITING_DEPENDENCY_UNRESOLVED",ruleId,"Required dependency "+missing+" is not bound.");
        }
    }
}
