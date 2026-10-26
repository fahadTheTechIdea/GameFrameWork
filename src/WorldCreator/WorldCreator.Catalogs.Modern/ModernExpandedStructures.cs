using WorldCreator.Core.Planning;

namespace WorldCreator.Catalogs.Modern;

/// <summary>Broad game-design profiles sharing the existing catalog/placement contracts.
/// Dimensions and capacities are editable examples, not regulations or measured art.
/// Specialized families are explicitly selected by a world composition/program.</summary>
internal static partial class ModernStructures
{
    private enum SiteProfile { Domestic, Retail, Hospitality, Office, Campus, Medical,
        Emergency, Civic, Worship, Sport, Logistics, Industry, Agriculture, Utility, PublicSpace, Infrastructure }
    private sealed record Family(string Id,string Unit,
        (double W,double D,double H,int Capacity) Small,
        (double W,double D,double H,int Capacity) Medium,
        (double W,double D,double H,int Capacity) Large,
        BuildingPlacementKind Placement = BuildingPlacementKind.FrontageSite);

    private static IReadOnlyList<BuildingDefinition> ExpandedDefinitions()
    {
        var result = new List<BuildingDefinition>();
        Add(BuildingCategory.Residential,BuildingFunction.Housing,SiteProfile.Domestic,
            new("detached-house","residents",(8,10,5,4),(12,16,8,7),(18,24,10,12)),
            new("villa","residents",(14,18,8,6),(24,30,12,12),(40,50,15,24)),
            new("duplex","residents",(12,14,8,8),(18,22,10,14),(26,30,12,22)),
            new("terraced-house","residents",(6,12,8,4),(8,16,11,6),(12,20,14,10)),
            new("bungalow","residents",(10,12,4,4),(16,20,5,6),(24,30,6,10)),
            new("courtyard-house","residents",(14,16,6,6),(22,26,9,12),(36,42,12,24)),
            new("residential-tower","residents",(24,30,45,160),(36,44,90,500),(52,64,180,1400)),
            new("dormitory","beds",(18,24,10,40),(32,45,18,180),(55,80,30,600)),
            new("student-residence","beds",(20,28,12,60),(40,55,24,300),(70,100,45,1200)),
            new("senior-housing","residents",(20,24,8,24),(38,50,14,100),(65,90,20,300)),
            new("worker-housing","beds",(20,30,8,60),(45,65,14,250),(90,140,20,1000)),
            new("apartment-complex","residents",(40,55,18,200),(85,110,30,800),(160,220,50,3000)));

        Add(BuildingCategory.Retail,BuildingFunction.Retail,SiteProfile.Retail,
            new("convenience-store","customers",(6,8,4,15),(10,14,5,35),(16,22,6,70)),
            new("grocery-store","customers",(10,14,5,30),(18,25,6,80),(30,42,7,180)),
            new("supermarket","customers",(24,32,6,100),(45,60,8,300),(75,100,10,700)),
            new("hypermarket","customers",(65,80,9,500),(110,140,12,1400),(170,220,15,3000)),
            new("shopping-mall","customers",(70,90,15,1000),(150,190,28,6000),(280,360,45,18000)),
            new("department-store","customers",(30,40,12,200),(55,75,22,700),(100,130,35,2000)),
            new("hardware-store","customers",(14,20,6,30),(30,45,9,100),(60,85,12,300)),
            new("furniture-store","customers",(24,35,7,60),(50,70,10,180),(100,140,15,500)),
            new("car-dealership","display-vehicles",(25,35,8,12),(50,75,10,40),(90,140,14,120)),
            new("garden-center","customers",(25,35,6,50),(60,80,8,160),(110,160,10,400)),
            new("wholesale-market","stalls",(30,45,8,20),(75,100,12,100),(150,220,16,350)),
            new("retail-kiosk","customers",(3,4,3,4),(5,7,4,10),(9,12,5,20)),
            new("shopping-arcade","shops",(18,40,8,8),(35,90,14,30),(65,180,22,90)));

        Add(BuildingCategory.FoodHospitality,BuildingFunction.Hospitality,SiteProfile.Hospitality,
            new("cafe","seats",(6,9,4,15),(12,18,5,50),(22,30,7,120)),
            new("restaurant","seats",(10,14,5,30),(20,30,7,100),(40,55,10,300)),
            new("fast-food-restaurant","seats",(10,14,5,25),(20,28,6,70),(32,45,8,150)),
            new("food-court","seats",(25,35,6,100),(55,75,10,400),(100,140,15,1000)),
            new("bakery","customers",(8,12,4,15),(16,24,6,45),(30,45,8,100)),
            new("hotel","guest-rooms",(20,28,15,25),(45,65,35,140),(90,130,90,600)),
            new("motel","guest-rooms",(20,35,6,15),(40,70,9,45),(70,130,12,120)),
            new("resort","guest-rooms",(55,80,12,60),(130,190,20,250),(280,420,35,900)),
            new("hostel","beds",(12,18,8,25),(25,35,14,90),(45,65,22,250)),
            new("guesthouse","guest-rooms",(10,14,6,6),(18,26,10,18),(30,45,15,45)),
            new("conference-hotel","guest-rooms",(40,55,20,80),(85,120,50,300),(160,240,100,1000)),
            new("catering-kitchen","meals-per-service",(12,18,6,200),(30,45,9,1200),(65,90,12,5000)));

        Add(BuildingCategory.OfficeFinance,BuildingFunction.Employment,SiteProfile.Office,
            new("office-building","workers",(15,20,12,60),(30,45,25,250),(60,80,45,900)),
            new("office-tower","workers",(25,30,60,500),(40,50,120,1800),(65,80,240,6000)),
            new("corporate-campus","workers",(55,75,15,250),(130,180,30,1500),(300,400,50,7000)),
            new("bank","customers",(12,16,7,20),(24,35,15,70),(50,70,35,220)),
            new("post-office","customers",(12,18,6,20),(28,40,10,80),(60,90,16,250)),
            new("co-working-center","workers",(12,18,8,30),(28,40,15,120),(55,75,25,400)),
            new("insurance-office","workers",(10,15,6,20),(22,30,12,80),(40,60,25,250)),
            new("trade-center","workers",(40,55,20,250),(90,130,60,1200),(170,240,150,4500)),
            new("stock-exchange","workers",(35,45,18,200),(70,90,35,700),(120,170,70,2200)));

        Add(BuildingCategory.EducationResearch,BuildingFunction.Education,SiteProfile.Campus,
            new("kindergarten","pupils",(18,22,5,30),(30,40,7,100),(50,65,10,250)),
            new("primary-school","pupils",(30,40,8,120),(55,75,12,400),(90,130,18,1000)),
            new("secondary-school","pupils",(40,55,10,200),(75,100,16,700),(130,180,24,1800)),
            new("vocational-school","students",(35,50,10,150),(75,110,16,600),(140,210,24,1600)),
            new("college","students",(50,70,15,300),(110,160,25,1500),(220,320,40,5000)),
            new("university-building","students",(35,50,18,250),(70,100,30,1000),(120,180,50,3000)),
            new("university-campus","students",(120,160,20,1000),(300,420,35,6000),(700,1000,50,25000)),
            new("training-center","students",(20,28,8,60),(45,65,15,250),(90,130,24,800)),
            new("research-laboratory","researchers",(25,35,10,40),(60,85,18,200),(130,190,30,700)),
            new("science-park","workers",(90,120,15,300),(220,320,30,1800),(500,750,45,7000)),
            new("library","readers",(20,25,8,50),(45,60,16,250),(90,130,30,900)),
            new("observatory","visitors",(12,15,12,15),(25,35,25,50),(55,75,45,150)));

        Add(BuildingCategory.Healthcare,BuildingFunction.Health,SiteProfile.Medical,
            new("pharmacy","customers",(6,10,4,10),(12,18,5,25),(22,30,7,60)),
            new("dental-clinic","treatment-chairs",(10,14,5,2),(20,28,8,8),(35,50,14,20)),
            new("health-center","consultation-rooms",(20,28,8,5),(45,60,14,20),(85,120,22,60)),
            new("hospital","beds",(45,60,15,50),(100,140,35,250),(220,320,70,1000)),
            new("medical-center","consultation-rooms",(25,35,12,10),(60,85,25,50),(130,180,45,150)),
            new("emergency-department","treatment-bays",(25,35,8,8),(55,75,14,30),(100,140,22,100)),
            new("rehabilitation-center","patients",(25,35,8,25),(55,80,15,100),(110,160,25,300)),
            new("maternity-hospital","beds",(30,40,12,30),(65,90,22,100),(120,170,35,300)),
            new("nursing-home","residents",(25,35,8,30),(55,75,15,120),(100,145,22,300)),
            new("veterinary-clinic","treatment-rooms",(12,18,5,3),(25,35,8,10),(45,65,12,25)));

        Add(BuildingCategory.CivicSafety,BuildingFunction.Administration,SiteProfile.Civic,
            new("town-hall","staff",(20,28,10,25),(45,60,20,120),(90,125,35,400)),
            new("courthouse","courtrooms",(25,35,12,2),(55,80,22,8),(110,160,40,24)),
            new("government-office","staff",(22,30,12,50),(50,70,25,250),(100,150,50,900)),
            new("embassy","staff",(25,35,10,30),(55,75,18,100),(100,140,30,300)),
            new("community-center","visitors",(18,25,7,60),(40,55,12,220),(75,110,20,700)),
            new("prison","places",(60,80,12,100),(150,220,20,600),(320,480,30,2000)));
        Add(BuildingCategory.CivicSafety,BuildingFunction.PublicSafety,SiteProfile.Emergency,
            new("police-station","staff",(18,25,8,20),(40,55,15,80),(80,115,25,250)),
            new("fire-station","response-vehicles",(20,30,8,2),(40,60,12,6),(75,110,18,15)),
            new("ambulance-depot","response-vehicles",(18,25,7,2),(35,50,10,6),(65,90,14,16)),
            new("civil-defense-center","staff",(30,40,10,30),(65,90,18,120),(130,180,28,400)));

        Add(BuildingCategory.CultureWorship,BuildingFunction.Worship,SiteProfile.Worship,
            new("mosque","worshippers",(15,20,10,100),(35,45,20,600),(75,100,40,2500)),
            new("church","worshippers",(15,25,12,100),(30,50,25,500),(60,100,45,2000)),
            new("temple","worshippers",(15,20,10,80),(35,50,20,450),(80,110,35,1800)),
            new("synagogue","worshippers",(14,20,9,80),(28,40,16,350),(55,80,28,1200)),
            new("cathedral","worshippers",(40,65,30,600),(65,110,55,1800),(100,170,90,5000)));
        Add(BuildingCategory.CultureWorship,BuildingFunction.Culture,SiteProfile.Civic,
            new("museum","visitors",(25,35,10,100),(65,90,22,500),(140,210,40,2000)),
            new("art-gallery","visitors",(15,22,7,40),(35,50,14,180),(75,110,25,700)),
            new("theatre","seats",(20,30,12,150),(45,65,22,600),(85,120,35,1800)),
            new("cinema","seats",(20,30,9,120),(50,70,16,600),(100,145,24,2000)),
            new("concert-hall","seats",(25,35,14,250),(55,80,28,1000),(110,160,45,3500)),
            new("cultural-center","visitors",(25,35,10,100),(60,85,20,450),(120,180,35,1600)),
            new("exhibition-center","visitors",(50,70,12,500),(130,180,20,3000),(280,400,30,12000)),
            new("convention-center","delegates",(45,65,15,400),(110,160,28,2000),(230,340,45,8000)),
            new("memorial","visitors",(6,8,6,10),(18,25,18,60),(45,65,45,250)));

        Add(BuildingCategory.SportRecreation,BuildingFunction.Sports,SiteProfile.Sport,
            new("multipurpose-stadium","seats",(120,100,15,2000),(200,160,35,20000),(300,240,65,80000)),
            new("indoor-arena","seats",(50,65,15,1000),(90,120,28,6000),(150,200,45,20000)),
            new("sports-hall","participants",(25,35,10,60),(45,65,15,250),(80,115,22,800)),
            new("swimming-center","visitors",(25,40,8,60),(50,75,14,250),(90,135,20,800)),
            new("gym","members-present",(12,18,6,20),(25,40,9,80),(50,75,14,250)),
            new("tennis-center","courts",(20,40,5,1),(60,85,8,4),(130,180,12,12)),
            new("basketball-court","players-and-spectators",(18,32,1,30),(30,45,6,150),(50,70,12,500)),
            new("football-training-ground","participants",(70,50,5,40),(130,100,8,150),(250,200,12,500)),
            new("athletics-track","participants",(110,75,3,80),(160,110,8,300),(230,165,15,1200)),
            new("skatepark","participants",(20,30,3,20),(45,65,5,70),(85,125,8,200)));

        Add(BuildingCategory.TransportLogistics,BuildingFunction.Transport,SiteProfile.Logistics,
            new("bus-stop","waiting-passengers",(3,6,3,8),(6,12,4,25),(10,20,5,60)),
            new("bus-station","bus-bays",(25,45,6,3),(60,100,10,12),(130,210,16,35)),
            new("bus-depot","buses",(35,55,8,10),(90,140,12,50),(190,300,18,180)),
            new("railway-station","passengers",(25,60,8,100),(75,160,20,800),(180,350,45,4000)),
            new("freight-terminal","loading-units",(50,80,10,20),(150,240,16,120),(350,550,25,500)),
            new("truck-depot","trucks",(35,50,8,8),(90,130,12,40),(200,300,18,150)),
            new("logistics-center","storage-units",(45,65,12,200),(120,180,18,1500),(280,420,28,7000)),
            new("distribution-center","storage-units",(40,60,10,150),(100,160,16,1000),(240,360,25,5000)),
            new("multistorey-car-park","vehicles",(25,35,10,60),(55,80,20,350),(110,160,35,1200)),
            new("parking-lot","vehicles",(18,25,1,12),(45,65,2,80),(100,150,3,350)),
            new("fuel-station","fueling-bays",(15,22,6,2),(30,45,8,6),(65,95,12,16)),
            new("ev-charging-hub","charging-bays",(12,18,4,4),(30,45,6,16),(65,95,9,48)),
            new("airport-terminal","passengers",(60,90,15,500),(180,280,30,5000),(450,700,50,25000)),
            new("aircraft-hangar","aircraft",(30,40,12,1),(70,100,25,4),(150,220,45,12)),
            new("port-terminal","cargo-units",(45,70,12,100),(130,210,20,700),(300,500,30,3000)),
            new("marina-building","visitors",(15,25,6,30),(35,55,10,100),(75,110,16,350)));

        Add(BuildingCategory.IndustryExtraction,BuildingFunction.Employment,SiteProfile.Industry,
            new("light-factory","workers",(20,30,8,40),(50,75,12,180),(110,165,18,650)),
            new("heavy-factory","workers",(50,75,15,150),(130,190,25,700),(300,450,40,2500)),
            new("assembly-plant","workers",(40,60,10,100),(110,170,18,500),(250,380,28,2000)),
            new("food-processing-plant","workers",(25,40,9,40),(65,100,14,200),(150,230,22,750)),
            new("textile-mill","workers",(30,45,10,60),(80,120,16,300),(180,270,24,1000)),
            new("chemical-plant","process-units",(50,70,18,10),(140,210,30,60),(330,500,45,220)),
            new("refinery","process-units",(100,150,25,20),(300,450,45,150),(700,1100,70,700)),
            new("steelworks","production-units",(80,120,25,20),(220,340,40,120),(500,800,65,500)),
            new("cement-plant","production-units",(70,100,25,15),(180,280,45,90),(420,650,70,350)),
            new("concrete-batching-plant","production-units",(30,45,15,8),(65,100,24,35),(140,210,35,120)),
            new("oil-gas-processing-plant","process-units",(70,110,20,15),(200,300,35,90),(450,700,55,350)),
            new("oil-well-site","well-units",(25,35,18,1),(55,80,30,4),(120,180,45,12)),
            new("mine-shaft-building","workers",(25,35,25,30),(60,90,45,150),(140,210,70,500)),
            new("quarry-facility","production-units",(40,60,12,10),(100,150,20,50),(230,350,30,180)),
            new("sawmill","workers",(25,40,9,25),(65,100,14,120),(150,230,20,400)),
            new("vehicle-maintenance-workshop","service-bays",(18,25,7,2),(40,60,10,8),(85,125,16,24)),
            new("cold-storage-warehouse","storage-units",(20,30,9,80),(55,85,14,450),(130,200,22,2000)),
            new("grain-warehouse","storage-units",(20,35,9,100),(60,95,15,600),(140,220,24,2500)),
            new("container-depot","containers",(40,60,8,30),(120,180,14,250),(280,430,20,1200)));

        Add(BuildingCategory.Agriculture,BuildingFunction.Agriculture,SiteProfile.Agriculture,
            new("farmhouse","residents",(10,14,6,5),(18,24,8,9),(30,40,10,16)),
            new("barn","storage-units",(12,20,7,30),(25,45,10,150),(55,90,15,600)),
            new("livestock-shed","animal-places",(12,25,6,15),(30,60,9,80),(70,140,13,350)),
            new("poultry-house","animal-places",(10,30,4,100),(20,65,6,700),(40,140,8,3500)),
            new("stable","animal-places",(12,20,5,6),(25,45,7,24),(55,90,10,80)),
            new("greenhouse","growing-units",(8,20,4,20),(25,60,6,150),(70,160,9,900)),
            new("grain-silo","storage-units",(6,8,15,100),(15,22,30,800),(35,50,55,4000)),
            new("feed-mill","production-units",(18,25,12,15),(45,70,22,80),(100,150,35,300)),
            new("dairy-farm","animal-places",(25,45,7,30),(70,120,12,180),(160,280,18,700)),
            new("aquaculture-facility","production-units",(30,45,5,10),(90,140,8,70),(220,340,12,300)),
            new("packing-house","workers",(20,30,7,20),(50,75,10,100),(110,170,15,350)),
            new("agricultural-equipment-shed","equipment-places",(12,18,6,3),(25,40,8,12),(55,85,12,40)));

        Add(BuildingCategory.UtilitiesCommunications,BuildingFunction.Utilities,SiteProfile.Utility,
            new("electrical-substation","power-units",(15,20,8,10),(40,60,15,80),(100,150,25,400)),
            new("power-station","power-units",(50,75,20,30),(150,230,40,250),(360,550,65,1200)),
            new("solar-farm","power-units",(60,90,3,10),(200,300,5,100),(600,900,8,700)),
            new("wind-farm","power-units",(100,150,60,10),(350,500,100,80),(900,1300,160,400)),
            new("water-treatment-plant","water-units",(25,40,8,20),(80,120,15,150),(200,310,25,700)),
            new("wastewater-treatment-plant","water-units",(35,50,8,20),(100,160,15,150),(250,400,25,700)),
            new("pumping-station","water-units",(8,12,5,5),(20,30,9,30),(45,70,15,120)),
            new("water-tower","water-units",(8,10,15,20),(18,22,30,100),(35,45,55,400)),
            new("desalination-plant","water-units",(40,60,10,20),(120,180,20,150),(280,430,30,650)),
            new("recycling-center","processing-units",(25,40,8,10),(75,110,14,70),(170,260,22,300)),
            new("waste-transfer-station","processing-units",(25,35,8,10),(65,100,12,60),(140,220,18,220)),
            new("waste-to-energy-plant","power-units",(50,70,25,20),(130,200,45,120),(300,450,70,450)),
            new("data-center","server-units",(25,35,8,100),(75,110,15,1000),(180,280,25,6000)),
            new("telecom-exchange","connection-units",(12,18,6,100),(30,45,12,1000),(65,100,20,5000)),
            new("cell-tower","connection-units",(5,7,20,100),(10,14,40,700),(20,28,70,2500)),
            new("emergency-shelter","people",(20,30,7,50),(50,75,12,250),(110,170,20,900)));

        Add(BuildingCategory.PublicSpace,BuildingFunction.Recreation,SiteProfile.PublicSpace,
            new("park-pavilion","visitors",(6,8,4,12),(12,18,6,40),(25,35,9,120)),
            new("public-toilet","users",(4,6,3,4),(8,12,4,12),(15,22,5,30)),
            new("playground","children",(12,18,3,15),(30,45,5,60),(65,95,8,180)),
            new("botanical-garden","visitors",(40,60,8,100),(130,190,15,700),(350,500,25,3000)),
            new("public-square","visitors",(18,25,1,80),(50,70,3,400),(120,170,6,1800)),
            new("cemetery","plots",(35,50,3,100),(100,150,5,700),(250,380,8,3000)),
            new("funeral-home","visitors",(15,22,6,30),(30,45,10,100),(60,90,16,300)),
            new("visitor-center","visitors",(12,18,5,25),(28,40,8,100),(60,85,14,350)),
            new("watchtower","occupants",(4,5,10,2),(8,10,20,6),(15,20,35,15)),
            new("guardhouse","occupants",(3,4,3,2),(6,8,4,5),(12,16,6,12)));

        Add(BuildingCategory.LinearWaterInfrastructure,BuildingFunction.Transport,SiteProfile.Infrastructure,
            new("road-bridge","vehicle-flow-units",(8,20,6,10),(18,100,20,80),(35,600,70,400),BuildingPlacementKind.LinearInfrastructure),
            new("railway-bridge","train-flow-units",(6,25,8,5),(12,140,25,30),(20,800,80,120),BuildingPlacementKind.LinearInfrastructure),
            new("pedestrian-bridge","pedestrian-flow-units",(3,15,5,20),(6,50,12,100),(12,180,30,400),BuildingPlacementKind.LinearInfrastructure),
            new("road-tunnel","vehicle-flow-units",(8,30,5,10),(18,250,8,80),(35,1500,12,400),BuildingPlacementKind.LinearInfrastructure),
            new("dam","water-units",(12,40,8,20),(35,180,35,150),(90,800,120,900),BuildingPlacementKind.WaterInfrastructure),
            new("canal-lock","vessel-units",(12,40,6,5),(30,120,15,30),(65,350,30,100),BuildingPlacementKind.WaterInfrastructure),
            new("breakwater","coastal-protection-units",(10,40,4,10),(25,250,8,70),(50,1200,15,300),BuildingPlacementKind.WaterInfrastructure),
            new("quay","cargo-units",(15,40,4,20),(35,180,8,120),(70,650,15,500),BuildingPlacementKind.WaterInfrastructure),
            new("ferry-landing","passengers",(10,25,5,30),(25,70,10,200),(50,180,18,800),BuildingPlacementKind.WaterInfrastructure),
            new("jetty","vessel-units",(4,20,3,3),(10,80,6,15),(22,250,10,60),BuildingPlacementKind.WaterInfrastructure));
        return result;

        void Add(string category,string function,SiteProfile profile,params Family[] families)
        {
            foreach (var family in families)
            {
                var siteProfile = family.Id == "farmhouse" ? SiteProfile.Domestic : profile;
                double road = profile switch { SiteProfile.Industry => 8,SiteProfile.Logistics => 6,
                    SiteProfile.Utility or SiteProfile.Emergency => 6,SiteProfile.Sport => 4,_ => 3 };
                if (family.Id == "pedestrian-bridge") road = 1.5;
                var roles = profile switch {
                    SiteProfile.Industry => new HashSet<SettlementRole> { SettlementRole.Industrial,SettlementRole.Mixed },
                    SiteProfile.Agriculture => new HashSet<SettlementRole> { SettlementRole.Agricultural,SettlementRole.Mixed },
                    _ => null
                };
                var variants = new[] { MakeSize("small",family.Small),MakeSize("medium",family.Medium),MakeSize("large",family.Large) };
                var actualFunction = family.Id switch {
                    "farmhouse" => BuildingFunction.Housing,
                    "convenience-store" or "grocery-store" or "supermarket" or "hypermarket" or "wholesale-market" => BuildingFunction.FoodRetail,
                    "cafe" or "restaurant" or "fast-food-restaurant" or "food-court" or "bakery" or "catering-kitchen" => BuildingFunction.FoodService,
                    "bank" or "insurance-office" or "stock-exchange" => BuildingFunction.Finance,
                    "research-laboratory" or "science-park" or "observatory" => BuildingFunction.Research,
                    "data-center" or "telecom-exchange" or "cell-tower" => BuildingFunction.Communications,
                    "wastewater-treatment-plant" or "recycling-center" or "waste-transfer-station" or "public-toilet" => BuildingFunction.Sanitation,
                    "guardhouse" or "watchtower" or "emergency-shelter" => BuildingFunction.PublicSafety,
                    "oil-well-site" or "mine-shaft-building" or "quarry-facility" => BuildingFunction.ResourceExtraction,
                    _ => function
                };
                result.Add(new(family.Id,actualFunction,family.Small.Capacity,
                    new HashSet<SettlementClass> { SettlementClass.Village,SettlementClass.Town,SettlementClass.City },
                    roles ?? new HashSet<SettlementRole>(),variants,ScaleDemand(family.Id),FamilySurroundings(family.Id,siteProfile),
                    profile is SiteProfile.Industry or SiteProfile.Logistics or SiteProfile.Utility ? 4 : 1.5,
                    road,roles,category,family.Unit,false,family.Placement));
                BuildingSizeVariant MakeSize(string id,(double W,double D,double H,int Capacity) size) =>
                    new(id,size.W,size.D,size.H,size.Capacity,
                        profile is SiteProfile.Industry or SiteProfile.Utility ? 5 : profile is SiteProfile.Sport or SiteProfile.Campus ? 3 : 1,
                        MinimumRoadWidthMeters:family.Id is "road-bridge" or "road-tunnel"
                            ? size.W < 12 ? 6 : size.W < 25 ? 12 : 24
                            : family.Placement == BuildingPlacementKind.WaterInfrastructure
                                ? size.Capacity == family.Small.Capacity ? 3 : size.Capacity == family.Medium.Capacity ? 6 : 10
                                : road);
            }
        }
    }

    private static double ScaleDemand(string id) => id switch {
        "multipurpose-stadium" => .12,"indoor-arena" => .03,"hospital" => .002,
        "shopping-mall" => .01,"university-campus" => .01,"airport-terminal" => .005,
        "mosque" or "church" or "temple" or "synagogue" => .02,
        "road-bridge" or "road-tunnel" or "pedestrian-bridge" => .005,
        "railway-bridge" => .001,"dam" => .003,"canal-lock" or "jetty" => .0005,
        "breakwater" => .001,"quay" => .002,"ferry-landing" => .005,_ => 0
    };

    private static IReadOnlyList<BuildingSiteRule> FamilySurroundings(string id,SiteProfile profile) => id switch
    {
        "retail-kiosk" or "guardhouse" => [new("front-access",SiteFeatureKind.Forecourt,SiteBand.Front,1.5,MaximumDepthMeters:5)],
        "bus-stop" => [new("waiting-area",SiteFeatureKind.Forecourt,SiteBand.Front,2,MaximumDepthMeters:6)],
        "parking-lot" or "multistorey-car-park" => [new("vehicle-entrance",SiteFeatureKind.Forecourt,SiteBand.Front,4,MaximumDepthMeters:18),
            new("landscape-edge",SiteFeatureKind.GreenBuffer,SiteBand.Rear,2,Required:false,MaximumDepthMeters:8)],
        "fuel-station" => [new("fueling-apron",SiteFeatureKind.Forecourt,SiteBand.Front,8,MaximumDepthMeters:35),
            new("service-yard",SiteFeatureKind.LoadingYard,SiteBand.Rear,4,MaximumDepthMeters:20)],
        "ev-charging-hub" => [new("charging-access",SiteFeatureKind.Forecourt,SiteBand.Front,5,MaximumDepthMeters:25),
            new("rest-area",SiteFeatureKind.Garden,SiteBand.Rear,2,Required:false,MaximumDepthMeters:12)],
        "pharmacy" => [new("customer-entry",SiteFeatureKind.Forecourt,SiteBand.Front,2,MaximumDepthMeters:8),
            new("delivery-space",SiteFeatureKind.LoadingYard,SiteBand.Rear,2,MaximumDepthMeters:10)],
        "cell-tower" or "water-tower" or "pumping-station" => [new("maintenance-access",SiteFeatureKind.Forecourt,SiteBand.Front,2,MaximumDepthMeters:12)],
        "watchtower" => [new("inspection-access",SiteFeatureKind.Forecourt,SiteBand.Front,2,MaximumDepthMeters:8)],
        "public-toilet" => [new("pedestrian-entry",SiteFeatureKind.Forecourt,SiteBand.Front,2,MaximumDepthMeters:8)],
        _ => Surroundings(profile)
    };

    private static IReadOnlyList<BuildingSiteRule> Surroundings(SiteProfile profile) => profile switch
    {
        SiteProfile.Domestic => [new("garden",SiteFeatureKind.Garden,SiteBand.Rear,2,MaximumDepthMeters:20)],
        SiteProfile.Retail => [new("customer-court",SiteFeatureKind.Forecourt,SiteBand.Front,3,MaximumDepthMeters:18),
            new("delivery-yard",SiteFeatureKind.LoadingYard,SiteBand.Rear,4,MaximumDepthMeters:30)],
        SiteProfile.Hospitality => [new("arrival-court",SiteFeatureKind.Forecourt,SiteBand.Front,3,MaximumDepthMeters:25),
            new("guest-garden",SiteFeatureKind.Garden,SiteBand.Rear,3,MaximumDepthMeters:30)],
        SiteProfile.Office => [new("entrance-court",SiteFeatureKind.Forecourt,SiteBand.Front,3,MaximumDepthMeters:20),
            new("staff-green",SiteFeatureKind.Garden,SiteBand.Rear,3,MaximumDepthMeters:20)],
        SiteProfile.Campus => [new("arrival-court",SiteFeatureKind.Forecourt,SiteBand.Front,4,MaximumDepthMeters:30),
            new("campus-green",SiteFeatureKind.Lawn,SiteBand.Rear,5,MaximumDepthMeters:60)],
        SiteProfile.Medical => [new("patient-dropoff",SiteFeatureKind.Forecourt,SiteBand.Front,4,MaximumDepthMeters:25),
            new("recovery-garden",SiteFeatureKind.Garden,SiteBand.Rear,4,MaximumDepthMeters:30)],
        SiteProfile.Emergency => [new("response-apron",SiteFeatureKind.Forecourt,SiteBand.Front,8,MaximumDepthMeters:35),
            new("vehicle-yard",SiteFeatureKind.LoadingYard,SiteBand.Rear,6,MaximumDepthMeters:40)],
        SiteProfile.Civic => [new("public-court",SiteFeatureKind.Forecourt,SiteBand.Front,4,MaximumDepthMeters:35),
            new("public-green",SiteFeatureKind.GreenBuffer,SiteBand.Rear,3,MaximumDepthMeters:20)],
        SiteProfile.Worship => [new("gathering-court",SiteFeatureKind.Forecourt,SiteBand.Front,5,MaximumDepthMeters:35),
            new("quiet-garden",SiteFeatureKind.Garden,SiteBand.Rear,4,MaximumDepthMeters:30)],
        SiteProfile.Sport => [new("spectator-court",SiteFeatureKind.Forecourt,SiteBand.Front,6,MaximumDepthMeters:50),
            new("recreation-green",SiteFeatureKind.GreenBuffer,SiteBand.Rear,4,MaximumDepthMeters:35)],
        SiteProfile.Logistics => [new("vehicle-apron",SiteFeatureKind.Forecourt,SiteBand.Front,6,MaximumDepthMeters:45),
            new("loading-yard",SiteFeatureKind.LoadingYard,SiteBand.Rear,8,MaximumDepthMeters:70)],
        SiteProfile.Industry => [new("service-apron",SiteFeatureKind.Forecourt,SiteBand.Front,6,MaximumDepthMeters:40),
            new("service-yard",SiteFeatureKind.LoadingYard,SiteBand.Rear,10,MaximumDepthMeters:90),
            new("landscape-buffer",SiteFeatureKind.GreenBuffer,SiteBand.Left,3,MaximumDepthMeters:12)],
        SiteProfile.Agriculture => [new("equipment-yard",SiteFeatureKind.LoadingYard,SiteBand.Rear,6,MaximumDepthMeters:60),
            new("field-edge-buffer",SiteFeatureKind.GreenBuffer,SiteBand.Left,3,MaximumDepthMeters:12)],
        SiteProfile.Utility => [new("maintenance-court",SiteFeatureKind.Forecourt,SiteBand.Front,6,MaximumDepthMeters:30),
            new("service-yard",SiteFeatureKind.LoadingYard,SiteBand.Rear,6,MaximumDepthMeters:60)],
        SiteProfile.PublicSpace => [new("arrival-court",SiteFeatureKind.Forecourt,SiteBand.Front,2,MaximumDepthMeters:15),
            new("landscape-edge",SiteFeatureKind.GreenBuffer,SiteBand.Rear,2,MaximumDepthMeters:20)],
        _ => []
    };
}
