using System.Collections.Frozen;
using WorldCreator.Core.Planning;

namespace WorldCreator.Catalogs.Modern;

/// <summary>Explicit review of every current family. Roles describe authored game programs,
/// not mandatory real-world components, generated parts or universal facility inventories.</summary>
internal static class ModernCompositionProfiles
{
    private static readonly FrozenDictionary<string, StructureCompositionAssessment> Reviews = Create();
    public static BuildingDefinition Attach(BuildingDefinition definition) => definition with {
        Composition = Reviews.TryGetValue(definition.Id, out var review) ? review
            : throw new ArgumentException("MODERN_COMPOSITION_REVIEW_MISSING:" + definition.Id)
    };

    private static FrozenDictionary<string, StructureCompositionAssessment> Create()
    {
        var result = new Dictionary<string, StructureCompositionAssessment>(StringComparer.Ordinal);
        const string basis = "Authored game inventory review; requested scope/parts determine the reusable program. No universal component count or completed artwork is implied.";
        Add(StructureRepresentation.SingleStructure, "one dwelling shell; detached structures require an authored program",
            "village-house townhouse bungalow courtyard-house detached-house duplex terraced-house villa",
            Role("living-interior", CatalogPartKind.Interior), Role("outbuilding", CatalogPartKind.Structure, "agricultural-equipment-shed"), Role("garden", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.ContextDependentSite, "one residential block or an authored group of blocks",
            "apartment residential-tower dormitory student-residence senior-housing worker-housing",
            Role("housing-block", CatalogPartKind.Structure, "apartment", "dormitory"), Role("shared-services", CatalogPartKind.Interior), Role("shared-ground", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.CompoundSite, "aggregate residential site profile; individual blocks are not yet separated",
            "apartment-complex", Role("housing-block", CatalogPartKind.Structure, "apartment"), Role("community-building", CatalogPartKind.Structure, "community-center"), Role("circulation", CatalogPartKind.Infrastructure));
        Add(StructureRepresentation.SingleStructure, "one retail unit shell; shop departments are interiors",
            "convenience-store grocery-store supermarket department-store hardware-store furniture-store retail-kiosk city-retail",
            Role("sales-interior", CatalogPartKind.Interior), Role("stockroom", CatalogPartKind.Interior), Role("delivery-space", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.ContextDependentSite, "retail building with optional separate grounds/support structures",
            "hypermarket car-dealership garden-center", Role("retail-unit", CatalogPartKind.Structure, "city-retail"), Role("display-ground", CatalogPartKind.GroundArea), Role("service-building", CatalogPartKind.Structure, "workshop"));
        Add(StructureRepresentation.CompoundSite, "aggregate multi-unit retail profile; units and circulation need a program",
            "local-market wholesale-market shopping-arcade shopping-mall", Role("retail-units", CatalogPartKind.Structure, "retail-kiosk", "city-retail"), Role("shared-ground", CatalogPartKind.GroundArea), Role("circulation", CatalogPartKind.Infrastructure));
        Add(StructureRepresentation.SingleStructure, "one hospitality/food building shell; functional rooms remain interiors",
            "bakery cafe catering-kitchen fast-food-restaurant guesthouse hostel restaurant",
            Role("service-interior", CatalogPartKind.Interior), Role("kitchen-or-support", CatalogPartKind.Interior), Role("service-ground", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.ContextDependentSite, "one accommodation block or an authored accommodation/support site",
            "hotel motel conference-hotel", Role("accommodation", CatalogPartKind.Structure, "hotel", "guesthouse"), Role("food-service", CatalogPartKind.Structure, "restaurant"), Role("arrival-ground", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.CompoundSite, "aggregate leisure/accommodation site; separate blocks depend on game scope",
            "resort", Role("accommodation", CatalogPartKind.Structure, "hotel", "guesthouse"), Role("food-service", CatalogPartKind.Structure, "restaurant"), Role("recreation-ground", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.ContextDependentSite, "one building with multiple internal operators or an authored group",
            "food-court", Role("food-units", CatalogPartKind.Interior), Role("shared-seating", CatalogPartKind.Interior), Role("support", CatalogPartKind.Interior));
        Add(StructureRepresentation.SingleStructure, "one workplace/service building shell",
            "bank co-working-center insurance-office office-building office-tower post-office stock-exchange civic-office government-office town-hall",
            Role("workplace-interior", CatalogPartKind.Interior), Role("public-or-staff-services", CatalogPartKind.Interior), Role("arrival-ground", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.CompoundSite, "aggregate workplace precinct; blocks/support spaces need a program",
            "corporate-campus", Role("office-blocks", CatalogPartKind.Structure, "office-building"), Role("staff-services", CatalogPartKind.Structure, "community-center"), Role("circulation", CatalogPartKind.Infrastructure));
        Add(StructureRepresentation.ContextDependentSite, "one public/workplace block or a multi-building program",
            "trade-center courthouse embassy civil-defense-center community-center", Role("primary-building", CatalogPartKind.Structure, "office-building", "community-center"), Role("support-interior", CatalogPartKind.Interior), Role("public-ground", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.ContextDependentSite, "single emergency building or separated operations/support site",
            "ambulance-depot fire-station police-station", Role("operations", CatalogPartKind.Interior), Role("vehicle-service", CatalogPartKind.Structure, "vehicle-maintenance-workshop"), Role("vehicle-ground", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.CompoundSite, "aggregate secure site; separate accommodation/support and boundaries need a program",
            "prison", Role("accommodation-blocks", CatalogPartKind.Structure, "dormitory"), Role("administration", CatalogPartKind.Structure, "office-building"), Role("security-boundary", CatalogPartKind.Infrastructure));
        Add(StructureRepresentation.ContextDependentSite, "one teaching building or a school site with distinct grounds/blocks",
            "school kindergarten primary-school secondary-school vocational-school training-center college",
            Role("teaching-blocks", CatalogPartKind.Structure, "school", "university-building"), Role("specialist-rooms", CatalogPartKind.Interior), Role("activity-ground", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.CompoundSite, "aggregate education/research campus; constituent blocks need a program",
            "science-park university-campus", Role("teaching-or-research-blocks", CatalogPartKind.Structure, "university-building", "research-laboratory"), Role("library", CatalogPartKind.Structure, "library"), Role("student-services", CatalogPartKind.Structure, "student-residence"));
        Add(StructureRepresentation.ContextDependentSite, "one specialist building or a separated specialist/support site",
            "library observatory research-laboratory university-building", Role("specialist-interior", CatalogPartKind.Interior), Role("specialist-equipment", CatalogPartKind.Equipment), Role("support-building", CatalogPartKind.Structure, "office-building"));
        Add(StructureRepresentation.SingleStructure, "one outpatient/dispensing building shell",
            "clinic dental-clinic pharmacy veterinary-clinic", Role("care-interior", CatalogPartKind.Interior), Role("support-interior", CatalogPartKind.Interior), Role("arrival-ground", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.ContextDependentSite, "one care block or an authored multi-block care/support site",
            "emergency-department health-center maternity-hospital medical-center nursing-home rehabilitation-center",
            Role("care-block", CatalogPartKind.Structure, "clinic", "nursing-home"), Role("specialist-care", CatalogPartKind.Interior), Role("support-building", CatalogPartKind.Structure, "office-building"));
        Add(StructureRepresentation.CompoundSite, "aggregate hospital profile; wings, clinical/service blocks need a program",
            "hospital", Role("clinical-blocks", CatalogPartKind.Structure, "emergency-department", "clinic"), Role("diagnostics", CatalogPartKind.Interior), Role("support-building", CatalogPartKind.Structure, "office-building"));
        Add(StructureRepresentation.ContextDependentSite, "one cultural/worship building or a group with ancillary structures",
            "art-gallery cathedral church cinema concert-hall convention-center cultural-center exhibition-center mosque museum synagogue temple theatre",
            Role("primary-interior", CatalogPartKind.Interior), Role("ancillary-building", CatalogPartKind.Structure, "visitor-center"), Role("public-ground", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.SingleStructure, "one authored monument object; surrounding site is separately scoped",
            "memorial", Role("monument", CatalogPartKind.Equipment), Role("public-ground", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.CompoundSite, "aggregate sport site profile; playing surface, stands and support need a program",
            "football-stadium multipurpose-stadium football-training-ground swimming-center tennis-center",
            Role("playing-or-training-ground", CatalogPartKind.GroundArea), Role("spectator-and-support", CatalogPartKind.Structure, "sports-hall", "gym"), Role("changing-and-services", CatalogPartKind.Interior));
        Add(StructureRepresentation.ContextDependentSite, "one indoor sport building or an authored recreation site",
            "gym indoor-arena sports-hall", Role("activity-interior", CatalogPartKind.Interior), Role("changing-and-services", CatalogPartKind.Interior), Role("arrival-ground", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.ContextDependentSite, "one sport/ground profile; accessories are distinct authored parts",
            "athletics-track basketball-court skatepark", Role("activity-ground", CatalogPartKind.GroundArea), Role("activity-equipment", CatalogPartKind.Equipment), Role("support-building", CatalogPartKind.Structure, "public-toilet"));
        Add(StructureRepresentation.CompoundSite, "aggregate transport/logistics site; operational parts depend on its program",
            "airport-terminal bus-depot distribution-center freight-terminal logistics-center port-terminal truck-depot",
            Role("operations-building", CatalogPartKind.Structure, "warehouse", "office-building"), Role("handling-ground", CatalogPartKind.GroundArea), Role("transport-connections", CatalogPartKind.Infrastructure));
        Add(StructureRepresentation.ContextDependentSite, "one transport building or a connected multi-part stop/service site",
            "aircraft-hangar bus-station bus-stop ev-charging-hub fuel-station marina-building multistorey-car-park parking-lot railway-station",
            Role("service-building", CatalogPartKind.Structure, "guardhouse", "public-toilet"), Role("operational-ground", CatalogPartKind.GroundArea), Role("transport-equipment", CatalogPartKind.Equipment));
        Add(StructureRepresentation.SingleStructure, "one storage/maintenance building; yards/support may be separately authored",
            "cold-storage-warehouse grain-warehouse warehouse workshop vehicle-maintenance-workshop",
            Role("storage-or-work-interior", CatalogPartKind.Interior), Role("handling-ground", CatalogPartKind.GroundArea), Role("support-building", CatalogPartKind.Structure, "office-building"));
        Add(StructureRepresentation.CompoundSite, "aggregate production site or primary hall profile; process/storage/support need a program",
            "assembly-plant cement-plant chemical-plant concrete-batching-plant factory food-processing-plant heavy-factory light-factory oil-gas-processing-plant refinery sawmill steelworks textile-mill",
            Role("production-building", CatalogPartKind.Structure, "factory", "workshop"), Role("storage", CatalogPartKind.Structure, "warehouse"), Role("process-equipment", CatalogPartKind.Equipment), Role("administration", CatalogPartKind.Structure, "office-building"));
        Add(StructureRepresentation.CompoundSite, "aggregate extraction/handling site; operational ground/equipment are not a single building",
            "container-depot mine-shaft-building oil-well-site quarry-facility", Role("operations-building", CatalogPartKind.Structure, "workshop", "warehouse"), Role("working-ground", CatalogPartKind.GroundArea), Role("process-equipment", CatalogPartKind.Equipment));
        Add(StructureRepresentation.CompoundSite, "aggregate farm/processing site; production, husbandry and support are separately scoped",
            "aquaculture-facility dairy-farm feed-mill", Role("production-building", CatalogPartKind.Structure, "barn", "livestock-shed", "packing-house"), Role("production-ground", CatalogPartKind.GroundArea), Role("process-equipment", CatalogPartKind.Equipment));
        Add(StructureRepresentation.ContextDependentSite, "one agricultural building or an authored group with working ground",
            "agricultural-equipment-shed barn farmhouse grain-silo greenhouse livestock-shed packing-house poultry-house stable",
            Role("agricultural-building", CatalogPartKind.Structure, "agricultural-equipment-shed"), Role("working-interior", CatalogPartKind.Interior), Role("working-ground", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.CompoundSite, "aggregate utility/processing site; plant units, support and service areas need a program",
            "data-center desalination-plant power-station recycling-center solar-farm waste-to-energy-plant waste-transfer-station wastewater-treatment-plant water-treatment-plant wind-farm",
            Role("service-building", CatalogPartKind.Structure, "office-building", "workshop"), Role("plant-equipment", CatalogPartKind.Equipment), Role("service-infrastructure", CatalogPartKind.Infrastructure));
        Add(StructureRepresentation.ContextDependentSite, "one utility object/building or a separated service installation",
            "cell-tower electrical-substation emergency-shelter pumping-station telecom-exchange water-tower",
            Role("service-building", CatalogPartKind.Structure, "guardhouse"), Role("utility-equipment", CatalogPartKind.Equipment), Role("service-infrastructure", CatalogPartKind.Infrastructure));
        Add(StructureRepresentation.CompoundSite, "aggregate public landscape/site; paths, grounds and structures need a program",
            "botanical-garden cemetery playground public-square", Role("public-ground", CatalogPartKind.GroundArea), Role("visitor-or-service-building", CatalogPartKind.Structure, "visitor-center", "public-toilet"), Role("paths-and-boundaries", CatalogPartKind.Infrastructure));
        Add(StructureRepresentation.SingleStructure, "one public-service building/object; surrounding site is separately authored",
            "funeral-home guardhouse park-pavilion public-toilet visitor-center watchtower", Role("service-interior", CatalogPartKind.Interior), Role("public-ground", CatalogPartKind.GroundArea));
        Add(StructureRepresentation.InfrastructureAssembly, "alignment/span/shoreline aggregate; structural components need the infrastructure/art compiler",
            "breakwater canal-lock dam ferry-landing jetty pedestrian-bridge quay railway-bridge road-bridge road-tunnel",
            Role("structural-parts", CatalogPartKind.Infrastructure), Role("approaches-and-interfaces", CatalogPartKind.Infrastructure), Role("moving-or-service-equipment", CatalogPartKind.Equipment));
        return result.ToFrozenDictionary(StringComparer.Ordinal);

        void Add(StructureRepresentation representation, string scope, string ids, params CatalogPartRole[] roles)
        {
            var assessment = new StructureCompositionAssessment(representation, scope, basis, Array.AsReadOnly(roles));
            foreach (var id in ids.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (!result.TryAdd(id, assessment)) throw new InvalidOperationException("MODERN_COMPOSITION_REVIEW_DUPLICATE:" + id);
        }
        static CatalogPartRole Role(string role, CatalogPartKind kind, params string[] candidates) => new(role, kind, Array.AsReadOnly(candidates));
    }
}
