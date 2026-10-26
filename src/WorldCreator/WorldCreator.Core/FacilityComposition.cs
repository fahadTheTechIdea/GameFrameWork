using WorldCreator.Core.Geometry;

namespace WorldCreator.Core.Planning;

public sealed record FacilityRoad(string Id, WorldPolyline Centerline, RoadClass Class, RoadCrossSection Profile);
public sealed record FacilityStructureSlot(string Id, string Purpose, WorldPolygon Site,
    string FrontagePathId, BuildingEnvelopeRules Envelope, string SizeVariantId,
    IReadOnlyList<BuildingComponentRule> Components);
public sealed record FacilityChild(string Id, string DefinitionId, WorldPoint OffsetMeters, string Purpose);
public sealed record FacilityDefinition(string Id, string Name, double WidthMeters, double DepthMeters,
    IReadOnlyList<FacilityRoad> Roads, IReadOnlyList<FacilityStructureSlot> Structures,
    IReadOnlyList<FacilityChild> Children);
public sealed record FacilityPlacement(string Id, string DefinitionId, string RegionId, string AnchorId,
    string SettlementId, string Purpose, SettlementDemand Settlement,
    IReadOnlyDictionary<string, StructureSitingContext> MemberContexts);
public sealed record FacilityMembership(string FacilityId, string TemplateId, string MemberId);
public sealed record PlannedFacility(string Id, string TemplateId, string? ParentFacilityId,
    string Purpose, WorldPolygon Boundary, string SourceRegionId, string SourceAnchorId,
    string MockupId, string MockupRevision, string ArtStyleId, IReadOnlyList<string> MemberRuleIds);

/// <summary>Expands bounded pack-owned composition grammars at mockup anchors. A group is
/// ownership, not a blocking building or a render attachment. Existing planners own every child.</summary>
public sealed class FacilityCompositionPlanner(ISettlementCatalogRegistry catalogs)
{
    public MockupWorldDesign Expand(MockupWorldDesign design, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(design);
        token.ThrowIfCancellationRequested();
        design = CatalogContentAuthoring.BindReferences(design, catalogs, token);
        if (design.FacilityPlacements is not { Count: > 0 })
        {
            ValidateCompiled(design, token);
            return design;
        }
        if (design.SchemaVersion!=1 || design.Blueprint?.Regions is null || design.Blueprint.Anchors is null || design.Paths is null || design.RoadRules is null || design.SettlementRules is null || design.BuildingCatalog is null) throw new ArgumentException("FACILITY_DESIGN_INVALID");
        if (design.PlannedFacilities is { Count: > 0 }) throw new ArgumentException("FACILITY_ALREADY_COMPILED");
        if (design.FacilityPlacements.Count > 256) throw new ArgumentException("FACILITY_PLACEMENT_BUDGET");
        var pack = catalogs.Resolve(design.Catalog!);
        var templates = pack.Facilities.ToDictionary(f => f.Id, StringComparer.Ordinal);
        var regions = design.Blueprint.Regions.ToList();
        var paths = design.Paths.ToList(); var roads = design.RoadRules.ToList();
        var rules = design.SettlementRules.ToList(); var plans = new List<PlannedFacility>();
        var definitions = design.BuildingCatalog.ToDictionary(d => d.Id, StringComparer.Ordinal);
        var ids = rules.Select(r => r.Id).Concat(regions.Select(r => r.Id)).Concat(paths.Select(p => p.Id)).ToHashSet(StringComparer.Ordinal);
        foreach (var placement in design.FacilityPlacements.OrderBy(p => p?.Id, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            if (placement is null || !SettlementCatalogRegistry.ValidId(placement.Id) ||
                string.IsNullOrWhiteSpace(placement.Purpose) || string.IsNullOrWhiteSpace(placement.SettlementId) ||
                placement.Settlement is null || placement.MemberContexts is null || placement.MemberContexts.Count>10000)
                throw new ArgumentException("FACILITY_PLACEMENT_INVALID");
            placement.Settlement.Validate();
            var anchor = design.Blueprint.Anchors.SingleOrDefault(a => a.Id == placement.AnchorId);
            var parentRegion = design.Blueprint.Regions.SingleOrDefault(r => r.Id == placement.RegionId);
            if (anchor is null || parentRegion is null || anchor.RegionId != parentRegion.Id ||
                !ConvexParcelGeometry.IsConvex(parentRegion.Outline)) throw new ArgumentException("FACILITY_ANCHOR_INVALID:" + placement.Id);
            var used = new HashSet<string>(StringComparer.Ordinal);
            Visit(placement.DefinitionId, placement.Id, null, "", anchor.PositionMeters, parentRegion.Outline, placement.Purpose, 0);
            if (!used.SetEquals(placement.MemberContexts.Keys)) throw new ArgumentException("FACILITY_MEMBER_CONTEXT_MISMATCH:" + placement.Id);

            void Visit(string templateId, string instanceId, string? parentId, string relative,
                WorldPoint origin, WorldPolygon container, string purpose, int depth)
            {
                token.ThrowIfCancellationRequested();
                if (depth > 16 || plans.Count >= 10000 || rules.Count >= 10000 || paths.Count >= 10000)
                    throw new ArgumentException("FACILITY_EXPANSION_BUDGET");
                if (!templates.TryGetValue(templateId, out var template)) throw new ArgumentException("FACILITY_TEMPLATE_UNKNOWN:" + templateId);
                AddId(instanceId);
                var boundary = Rectangle(origin.X, origin.Y, template.WidthMeters, template.DepthMeters);
                if (boundary.Vertices.Any(p => !container.ContainsPoint(p))) throw new ArgumentException("FACILITY_OUTSIDE_PARENT:" + instanceId);
                regions.Add(new(instanceId, boundary, "facility:" + templateId));
                var memberRules = new List<string>();
                var planned = new PlannedFacility(instanceId, templateId, parentId, purpose, boundary,
                    placement.RegionId, placement.AnchorId, design.Blueprint.MockupId, design.Blueprint.ApprovedRevision,
                    design.Blueprint.ArtStyleId, memberRules);
                plans.Add(planned);
                foreach (var road in template.Roads.OrderBy(r => r.Id, StringComparer.Ordinal))
                {
                    string id = instanceId + ":" + road.Id; AddId(id);
                    paths.Add(new(id, instanceId, new(road.Centerline.Points.Select(Shift).ToArray()), "road"));
                    roads.Add(new(id, road.Class, road.Profile));
                }
                foreach (var slot in template.Structures.OrderBy(s => s.Id, StringComparer.Ordinal))
                {
                    token.ThrowIfCancellationRequested();
                    string id = instanceId + ":" + slot.Id, key = relative + slot.Id; AddId(id);
                    if (!placement.MemberContexts.TryGetValue(key, out var context) || context is null || !used.Add(key))
                        throw new ArgumentException("FACILITY_MEMBER_CONTEXT_REQUIRED:" + key);
                    var definition = pack.Structures.Single(d => d.Id == slot.Envelope.BuildingType);
                    if (definitions.TryGetValue(definition.Id, out var authored)) definition = authored;
                    else definitions.Add(definition.Id, definition);
                    var site = new WorldPolygon(slot.Site.Vertices.Select(Shift).ToArray());
                    regions.Add(new(id, site, "facility-member:" + slot.Id));
                    double frontage = site.Vertices.Max(p => p.X) - site.Vertices.Min(p => p.X);
                    rules.Add(new(id, id, placement.SettlementId, instanceId + ":" + slot.FrontagePathId,
                        slot.Purpose, frontage, frontage, slot.Envelope, slot.Components, slot.SizeVariantId,
                        Settlement: placement.Settlement, Siting: context,
                        Facility: new(instanceId, templateId, slot.Id)));
                    memberRules.Add(id);
                }
                foreach (var child in template.Children.OrderBy(c => c.Id, StringComparer.Ordinal))
                    Visit(child.DefinitionId, instanceId + ":" + child.Id, instanceId, relative + child.Id + ":",
                        Shift(child.OffsetMeters), boundary, child.Purpose, depth + 1);
                plans[plans.IndexOf(planned)] = planned with { MemberRuleIds = Array.AsReadOnly(memberRules.ToArray()) };
                WorldPoint Shift(WorldPoint p) => new(p.X + origin.X, p.Y + origin.Y);
            }
        }
        var roots=plans.Where(p=>p.ParentFacilityId is null).ToArray();
        for (int i=0;i<roots.Length;i++) for (int j=0;j<i;j++)
            if (TerrainCoverageTopology.IntersectionArea(roots[i].Boundary,roots[j].Boundary)>1e-8) throw new ArgumentException("FACILITY_PLACEMENT_CONFLICT:"+roots[i].Id);
        if (regions.Count>10000 || rules.Count>10000 || paths.Count>10000) throw new ArgumentException("FACILITY_EXPANSION_BUDGET");
        var result = design with { Blueprint = design.Blueprint with { Regions = regions }, Paths = paths, RoadRules = roads,
            SettlementRules = rules, BuildingCatalog = definitions.Values.OrderBy(d => d.Id, StringComparer.Ordinal).ToArray(),
            FacilityPlacements = null, PlannedFacilities = plans };
        ValidateCompiled(result, token);
        return result;
        void AddId(string id) { if (!ids.Add(id)) throw new ArgumentException("FACILITY_ID_CONFLICT:" + id); }
    }

    private void ValidateCompiled(MockupWorldDesign design, CancellationToken token)
    {
        if (design.PlannedFacilities is not { Count: > 0 })
        {
            if (design.SettlementRules?.Any(r => r?.Facility is not null) == true)
                throw new ArgumentException("FACILITY_OWNER_MISSING");
            return;
        }
        if (design.PlannedFacilities.Count > 10000 || design.Blueprint?.Regions is null || design.Blueprint.Anchors is null ||
            design.SettlementRules is null || design.Paths is null || design.RoadRules is null)
            throw new ArgumentException("FACILITY_COMPILED_PLAN_INVALID");
        var templates = catalogs.Resolve(design.Catalog!).Facilities.ToDictionary(f => f.Id, StringComparer.Ordinal);
        var groups = new Dictionary<string, PlannedFacility>(StringComparer.Ordinal);
        foreach (var group in design.PlannedFacilities)
            if (group is null || !SettlementCatalogRegistry.ValidId(group.Id) || !groups.TryAdd(group.Id, group))
                throw new ArgumentException("FACILITY_COMPILED_PLAN_INVALID");
        foreach (var group in groups.Values)
        {
            token.ThrowIfCancellationRequested();
            if (!templates.TryGetValue(group.TemplateId, out var template) || group.Boundary is null || group.MemberRuleIds is null ||
                string.IsNullOrWhiteSpace(group.Purpose) || group.MockupId != design.Blueprint.MockupId ||
                group.MockupRevision != design.Blueprint.ApprovedRevision || group.ArtStyleId != design.Blueprint.ArtStyleId)
                throw new ArgumentException("FACILITY_COMPILED_PROVENANCE_INVALID:" + group.Id);
            group.Boundary.Validate();
            var anchor = design.Blueprint.Anchors.SingleOrDefault(a => a.Id == group.SourceAnchorId);
            var sourceRegion = design.Blueprint.Regions.SingleOrDefault(r => r.Id == group.SourceRegionId);
            var origin = group.Boundary.Vertices[0];
            if (anchor is null || sourceRegion is null || anchor.RegionId != sourceRegion.Id ||
                group.Boundary.Vertices.Any(p => !sourceRegion.Outline.ContainsPoint(p)) ||
                !group.Boundary.Vertices.SequenceEqual(Rectangle(origin.X, origin.Y, template.WidthMeters, template.DepthMeters).Vertices))
                throw new ArgumentException("FACILITY_COMPILED_BOUNDS_INVALID:" + group.Id);
            var ownRegion = design.Blueprint.Regions.SingleOrDefault(r => r.Id == group.Id);
            if (ownRegion is null || !ownRegion.Outline.Vertices.SequenceEqual(group.Boundary.Vertices))
                throw new ArgumentException("FACILITY_COMPILED_REGION_CHANGED:" + group.Id);
            foreach (var road in template.Roads)
            {
                var path = design.Paths.SingleOrDefault(p => p.Id == group.Id + ":" + road.Id);
                var rule = design.RoadRules.SingleOrDefault(r => r.PathId == path?.Id);
                if (path is null || rule is null || path.RegionId != group.Id || rule.Class != road.Class || rule.Profile != road.Profile ||
                    !path.Centerline.Points.SequenceEqual(road.Centerline.Points.Select(p => new WorldPoint(origin.X + p.X, origin.Y + p.Y))))
                    throw new ArgumentException("FACILITY_COMPILED_ROAD_CHANGED:" + group.Id);
            }
            if (group.ParentFacilityId is null)
            {
                if (origin != anchor.PositionMeters) throw new ArgumentException("FACILITY_COMPILED_ANCHOR_CHANGED:" + group.Id);
            }
            else
            {
                if (!groups.TryGetValue(group.ParentFacilityId, out var parent) || !templates.TryGetValue(parent.TemplateId, out var parentTemplate))
                    throw new ArgumentException("FACILITY_PARENT_MISSING:" + group.Id);
                var child = parentTemplate.Children.SingleOrDefault(c => parent.Id + ":" + c.Id == group.Id);
                if (child is null || child.DefinitionId != group.TemplateId ||
                    origin != new WorldPoint(parent.Boundary.Vertices[0].X + child.OffsetMeters.X, parent.Boundary.Vertices[0].Y + child.OffsetMeters.Y))
                    throw new ArgumentException("FACILITY_COMPILED_PARENT_INVALID:" + group.Id);
            }
            var members = design.SettlementRules.Where(r => r.Facility?.FacilityId == group.Id).ToArray();
            if (!members.Select(r => r.Id).ToHashSet(StringComparer.Ordinal).SetEquals(group.MemberRuleIds) || members.Length != template.Structures.Count)
                throw new ArgumentException("FACILITY_COMPILED_MEMBERS_INVALID:" + group.Id);
            foreach (var slot in template.Structures)
            {
                var member = members.SingleOrDefault(m => m.Id == group.Id + ":" + slot.Id);
                var region = design.Blueprint.Regions.SingleOrDefault(r => r.Id == member?.RegionId);
                if (member is null || member.Facility != new FacilityMembership(group.Id, template.Id, slot.Id) ||
                    member.Envelope != slot.Envelope || member.SizeVariantId != slot.SizeVariantId ||
                    member.Purpose != slot.Purpose || !member.Components.SequenceEqual(slot.Components) ||
                    member.FrontagePathId != group.Id + ":" + slot.FrontagePathId || region is null ||
                    !region.Outline.Vertices.SequenceEqual(slot.Site.Vertices.Select(p => new WorldPoint(origin.X + p.X, origin.Y + p.Y))))
                    throw new ArgumentException("FACILITY_COMPILED_MEMBER_CHANGED:" + group.Id);
            }
            foreach (var child in template.Children)
                if (!groups.TryGetValue(group.Id + ":" + child.Id, out var nested) || nested.ParentFacilityId != group.Id)
                    throw new ArgumentException("FACILITY_COMPILED_CHILD_MISSING:" + group.Id);
        }
        if (design.SettlementRules.Any(r => r.Facility is { } m && !groups.ContainsKey(m.FacilityId)))
            throw new ArgumentException("FACILITY_OWNER_MISSING");
    }

    internal static IReadOnlyList<FacilityDefinition> Snapshot(IReadOnlyList<FacilityDefinition> facilities,
        IReadOnlyList<BuildingDefinition> structures, CancellationToken token)
    {
        if (facilities is null || facilities.Count > 1000) throw new ArgumentException("FACILITY_CATALOG_INVALID");
        var byId = new Dictionary<string, FacilityDefinition>(StringComparer.Ordinal);
        foreach (var f in facilities)
        {
            token.ThrowIfCancellationRequested();
            if (f is null || !SettlementCatalogRegistry.ValidId(f.Id) || string.IsNullOrWhiteSpace(f.Name) ||
                !double.IsFinite(f.WidthMeters) || !double.IsFinite(f.DepthMeters) || f.WidthMeters <= 0 || f.DepthMeters <= 0 ||
                f.Roads is null || f.Structures is null || f.Children is null || f.Structures.Count + f.Children.Count is 0 or > 1000 ||
                f.Roads.Count > 1000 || byId.ContainsKey(f.Id)) throw new ArgumentException("FACILITY_CATALOG_INVALID");
            var boundary = Rectangle(0, 0, f.WidthMeters, f.DepthMeters);
            var local = new HashSet<string>(StringComparer.Ordinal);
            void Name(string id) { if (!SettlementCatalogRegistry.ValidId(id) || id.Contains(':') || !local.Add(id)) throw new ArgumentException("FACILITY_MEMBER_ID_INVALID"); }
            foreach (var r in f.Roads)
            {
                if (r is null || r.Centerline is null || r.Profile is null || !Enum.IsDefined(r.Class)) throw new ArgumentException("FACILITY_ROAD_INVALID");
                Name(r.Id); r.Centerline.Validate(); r.Profile.Validate();
                if (StreetSurfaceBuilder.Build(new(r.Id, r.Centerline, r.Profile)).Any(s => s.Polygon.Any(p => !boundary.ContainsPoint(p))))
                    throw new ArgumentException("FACILITY_ROAD_OUTSIDE_PARENT");
            }
            foreach (var s in f.Structures)
            {
                if (s is null || s.Site is null || s.Envelope is null || s.Components is null || string.IsNullOrWhiteSpace(s.Purpose)) throw new ArgumentException("FACILITY_STRUCTURE_INVALID");
                if (s.Components.Count > 256 || s.Components.Any(c => c is null || string.IsNullOrWhiteSpace(c.Id) || string.IsNullOrWhiteSpace(c.Kind) || string.IsNullOrWhiteSpace(c.Purpose)) ||
                    s.Components.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != s.Components.Count)
                    throw new ArgumentException("FACILITY_COMPONENT_INVALID");
                Name(s.Id); s.Site.Validate(); s.Envelope.Validate();
                var d = structures.SingleOrDefault(d => d.Id == s.Envelope.BuildingType);
                var road = f.Roads.SingleOrDefault(r => r.Id == s.FrontagePathId);
                if (d is null || d.PlacementKind != BuildingPlacementKind.FrontageSite || d.SizeVariants?.Any(v => v.Id == s.SizeVariantId) != true)
                    throw new ArgumentException("FACILITY_STRUCTURE_UNKNOWN:" + s.Id);
                if (s.Site.Vertices.Count != 4 || s.Site.Vertices.Select(p => p.X).Distinct().Count() != 2 || s.Site.Vertices.Select(p => p.Y).Distinct().Count() != 2 ||
                    s.Site.Vertices.Any(p => !boundary.ContainsPoint(p)) || road is null || road.Centerline.Points.Count != 2 ||
                    road.Centerline.Points[0].Y != road.Centerline.Points[1].Y ||
                    !new[] { s.Site.Vertices.Min(p => p.Y), s.Site.Vertices.Max(p => p.Y) }.Contains(road.Centerline.Points[0].Y) ||
                    road.Centerline.Points.Min(p => p.X) > s.Site.Vertices.Min(p => p.X) || road.Centerline.Points.Max(p => p.X) < s.Site.Vertices.Max(p => p.X))
                    throw new ArgumentException("FACILITY_SITE_FRONTAGE_INVALID:" + s.Id);
            }
            foreach (var c in f.Children)
            {
                if (c is null || !SettlementCatalogRegistry.ValidId(c.DefinitionId) || string.IsNullOrWhiteSpace(c.Purpose) ||
                    !double.IsFinite(c.OffsetMeters.X) || !double.IsFinite(c.OffsetMeters.Y)) throw new ArgumentException("FACILITY_CHILD_INVALID");
                Name(c.Id);
            }
            byId.Add(f.Id, f with { Roads = Array.AsReadOnly(f.Roads.Select(r => r with { Centerline = new(Array.AsReadOnly(r.Centerline.Points.ToArray())) }).ToArray()),
                Structures = Array.AsReadOnly(f.Structures.Select(s => s with { Site = new(Array.AsReadOnly(s.Site.Vertices.ToArray())), Components = Array.AsReadOnly(s.Components.ToArray()) }).ToArray()),
                Children = Array.AsReadOnly(f.Children.ToArray()) });
        }
        var active = new HashSet<string>(StringComparer.Ordinal); var done = new HashSet<string>(StringComparer.Ordinal);
        foreach (var f in byId.Values.OrderBy(f => f.Id, StringComparer.Ordinal)) Visit(f.Id);
        void Visit(string id)
        {
            token.ThrowIfCancellationRequested();
            if (done.Contains(id)) return;
            if (!byId.TryGetValue(id, out var f)) throw new ArgumentException("FACILITY_CHILD_UNKNOWN:" + id);
            if (!active.Add(id)) throw new ArgumentException("FACILITY_CYCLE:" + id);
            if (active.Count > 16) throw new ArgumentException("FACILITY_DEPTH_EXCEEDED");
            var areas = f.Structures.Select(s => s.Site).ToList();
            foreach (var child in f.Children)
            {
                Visit(child.DefinitionId); var nested = byId[child.DefinitionId];
                var box = Rectangle(child.OffsetMeters.X, child.OffsetMeters.Y, nested.WidthMeters, nested.DepthMeters);
                if (box.Vertices.Any(p => !Rectangle(0, 0, f.WidthMeters, f.DepthMeters).ContainsPoint(p))) throw new ArgumentException("FACILITY_CHILD_OUTSIDE_PARENT");
                areas.Add(box);
            }
            for (int i = 0; i < areas.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                for (int j = 0; j < i; j++)
                    if (TerrainCoverageTopology.IntersectionArea(areas[i], areas[j]) > 1e-8) throw new ArgumentException("FACILITY_SITE_CONFLICT:" + id);
            }
            active.Remove(id); done.Add(id);
        }
        return Array.AsReadOnly(byId.Values.OrderBy(f => f.Id, StringComparer.Ordinal).ToArray());
    }
    private static WorldPolygon Rectangle(double x, double y, double width, double depth) => new([
        new(x, y), new(x + width, y), new(x + width, y + depth), new(x, y + depth)]);
}
