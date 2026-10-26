using WorldCreator.Core.Planning;

namespace WorldCreator.Catalogs.Modern;

/// <summary>Functional policy attached to the existing catalog, not a second catalog.
/// Numeric demands use editable game-service units. Research IDs refer to the repository source register.</summary>
public static class ModernSitingProfiles
{
    public static BuildingDefinition Attach(BuildingDefinition definition)
    {
        var ruleIds=Rules(definition);
        var districts=new HashSet<string>();
        var tags=new HashSet<string>(StringComparer.Ordinal);
        var requirements=new Dictionary<string,StructureResourceRequirement>(StringComparer.Ordinal);
        var technologies=new HashSet<string> { FacilityTechnology.Standard };
        var sources=new HashSet<string>(StringComparer.Ordinal) { "R22" };
        bool building=definition.PlacementKind==BuildingPlacementKind.FrontageSite;
        if (building)
        {
            Need("power",SitingResourceKind.Electricity);
            Need("potable-water",SitingResourceKind.PotableWater,qualities:["potable","reliable"]);
            Need("wastewater",SitingResourceKind.Wastewater);
        }
        foreach (var id in ruleIds)
        {
            switch (id)
            {
                case "S02":
                    Districts(DistrictUse.HeavyIndustry); tags.Add("heavy-impact"); sources.Add("R05"); Freight(8); break;
                case "S03":
                    Districts(DistrictUse.LightIndustry,DistrictUse.HeavyIndustry,DistrictUse.MixedUse); sources.Add("R04"); Freight(3); break;
                case "S04":
                    Districts(DistrictUse.Logistics,DistrictUse.HeavyIndustry,DistrictUse.LightIndustry,DistrictUse.Port,DistrictUse.Agriculture);
                    sources.Add("R04"); tags.Add("freight-intensive"); Freight(6); break;
                case "S05":
                    Districts(DistrictUse.LightIndustry,DistrictUse.HeavyIndustry,DistrictUse.Agriculture);
                    sources.UnionWith(["R05","R10"]); Freight(6); Need("process-water",SitingResourceKind.ProcessWater,qualities:["food-compatible"]);
                    Need("waste-handling",SitingResourceKind.WasteHandling); break;
                case "S06":
                    Districts(DistrictUse.HeavyIndustry); sources.Add("R05"); tags.Add("heavy-impact"); Freight(8);
                    Need("deposit",SitingResourceKind.MineralDeposit,contains:true); break;
                case "S07":
                    Districts(DistrictUse.Utility,DistrictUse.HeavyIndustry); sources.UnionWith(["R03","R24"]); tags.Add("heavy-impact");
                    Need("grid-export",SitingResourceKind.GridExport); Need("fuel",SitingResourceKind.Fuel);
                    Technologies(FacilityTechnology.ThermalOnceThrough,FacilityTechnology.ThermalRecirculating,FacilityTechnology.ThermalAirCooled,
                        FacilityTechnology.NuclearOnceThrough,FacilityTechnology.NuclearRecirculating,FacilityTechnology.NuclearAtmospheric);
                    Need("cooling-intake",SitingResourceKind.CoolingWater,qualities:["reliable","cooling-compatible"],origin:SitingResourceOrigin.SurfaceWater,
                        when:[FacilityTechnology.ThermalOnceThrough,FacilityTechnology.NuclearOnceThrough]);
                    Need("cooling-makeup",SitingResourceKind.CoolingWater,qualities:["reliable","cooling-compatible"],
                        when:[FacilityTechnology.ThermalRecirculating,FacilityTechnology.NuclearRecirculating]);
                    Need("water-outfall",SitingResourceKind.WaterOutfall,origin:SitingResourceOrigin.SurfaceWater,
                        qualities:["thermal-compatible"],when:[FacilityTechnology.ThermalOnceThrough,FacilityTechnology.NuclearOnceThrough]);
                    Need("normal-heat",SitingResourceKind.HeatRejection,qualities:["normal-operation"],
                        origin:null);
                    Need("shutdown-heat",SitingResourceKind.HeatRejection,qualities:["shutdown-compatible","reliable"],
                        when:[FacilityTechnology.NuclearOnceThrough,FacilityTechnology.NuclearRecirculating,FacilityTechnology.NuclearAtmospheric]);
                    Need("security",SitingResourceKind.Security,SitingNetworkMode.Road,when:[FacilityTechnology.NuclearOnceThrough,
                        FacilityTechnology.NuclearRecirculating,FacilityTechnology.NuclearAtmospheric]);
                    Need("emergency",SitingResourceKind.EmergencyAccess,SitingNetworkMode.Road);
                    break;
                case "S09":
                    Districts(DistrictUse.Utility,DistrictUse.Agriculture); sources.Add("R15");
                    Technologies(FacilityTechnology.Photovoltaic,FacilityTechnology.SolarThermal);
                    Need("solar-resource",SitingResourceKind.Solar,contains:true); Need("grid-export",SitingResourceKind.GridExport);
                    Need("thermal-cooling",SitingResourceKind.CoolingWater,when:[FacilityTechnology.SolarThermal]); break;
                case "S10":
                    Districts(DistrictUse.Utility,DistrictUse.Agriculture); sources.Add("R14"); tags.Add("aviation-obstacle");
                    Need("wind-resource",SitingResourceKind.Wind,contains:true); Need("grid-export",SitingResourceKind.GridExport); break;
                case "S11":
                    Districts(DistrictUse.Utility,DistrictUse.LightIndustry,DistrictUse.HeavyIndustry,DistrictUse.Residential,DistrictUse.MixedUse);
                    sources.Add("R24"); Need("grid-export",SitingResourceKind.GridExport); break;
                case "S12":
                    Districts(DistrictUse.Utility,DistrictUse.Research,DistrictUse.LightIndustry); sources.Add("R20");
                    Technologies(FacilityTechnology.DataCenterAirCooled,FacilityTechnology.DataCenterEvaporative);
                    Need("communications",SitingResourceKind.Communications); Need("normal-heat",SitingResourceKind.HeatRejection,qualities:["normal-operation"]);
                    Need("cooling-makeup",SitingResourceKind.CoolingWater,qualities:["reliable"],when:[FacilityTechnology.DataCenterEvaporative]); break;
                case "S13":
                    Districts(DistrictUse.Utility,DistrictUse.MixedUse,DistrictUse.CommercialCenter,DistrictUse.Research);
                    sources.Add("R25"); tags.Add("aviation-obstacle"); Need("backhaul",SitingResourceKind.Communications); break;
                case "S14":
                    Districts(DistrictUse.Utility); sources.Add("R10"); tags.Add("water-supply");
                    if (definition.Id=="water-treatment-plant") Need("source-water",SitingResourceKind.ProcessWater,qualities:["fresh","protected","reliable"]);
                    else Need("source-water",SitingResourceKind.PotableWater,qualities:["potable","reliable"]); break;
                case "S15":
                    Districts(DistrictUse.Utility,DistrictUse.HeavyIndustry); sources.Add("R11"); tags.Add("heavy-impact"); tags.Add("water-discharge");
                    Need("sewer-catchment",SitingResourceKind.Wastewater); Need("water-outfall",SitingResourceKind.WaterOutfall); break;
                case "S16":
                    Districts(DistrictUse.Utility,DistrictUse.HeavyIndustry); sources.UnionWith(["R10","R11"]); tags.Add("water-discharge");
                    Technologies(FacilityTechnology.SeawaterDesalination,FacilityTechnology.BrackishDesalination);
                    Need("seawater",SitingResourceKind.ProcessWater,qualities:["marine","reliable"],origin:SitingResourceOrigin.SurfaceWater,when:[FacilityTechnology.SeawaterDesalination]);
                    Need("brackish-water",SitingResourceKind.ProcessWater,qualities:["brackish","reliable"],when:[FacilityTechnology.BrackishDesalination]);
                    Need("brine-outfall",SitingResourceKind.WaterOutfall,qualities:["brine-compatible"]); break;
                case "S17":
                    Districts(DistrictUse.Utility,DistrictUse.HeavyIndustry,DistrictUse.Logistics); sources.UnionWith(["R05","R12"]);
                    tags.UnionWith(["heavy-impact","wildlife-attractant","water-contamination"]); Freight(6);
                    Need("waste-stream",SitingResourceKind.WasteHandling); break;
                case "S18":
                    Districts(DistrictUse.Education,DistrictUse.Residential,DistrictUse.MixedUse); sources.UnionWith(["R06","R07"]);
                    tags.Add("sensitive"); Need("pupil-access",SitingResourceKind.PedestrianCatchment,SitingNetworkMode.Pedestrian); break;
                case "S19":
                    Districts(DistrictUse.Education,DistrictUse.Research,DistrictUse.MixedUse); sources.Add("R25");
                    Need("campus-access",SitingResourceKind.PedestrianCatchment,SitingNetworkMode.Pedestrian); break;
                case "S20":
                    Districts(DistrictUse.Research,DistrictUse.Conservation,DistrictUse.Education); sources.Add("R21");
                    Technologies(FacilityTechnology.OpticalResearch,FacilityTechnology.TeachingObservatory);
                    Need("dark-sky",SitingResourceKind.DarkSky,contains:true,when:[FacilityTechnology.OpticalResearch]); break;
                case "S21":
                    Districts(DistrictUse.Healthcare,DistrictUse.Residential,DistrictUse.MixedUse,DistrictUse.CommercialCenter); sources.UnionWith(["R09","R25"]);
                    tags.Add("sensitive"); Need("patient-access",SitingResourceKind.PedestrianCatchment,SitingNetworkMode.Pedestrian);
                    Need("emergency",SitingResourceKind.EmergencyAccess,SitingNetworkMode.Road); Need("clinical-waste",SitingResourceKind.WasteHandling); break;
                case "S22":
                    Districts(DistrictUse.Civic,DistrictUse.Residential,DistrictUse.MixedUse,DistrictUse.Transport); sources.UnionWith(["R09","R23"]);
                    tags.Add("critical-service"); Need("response-network",SitingResourceKind.EmergencyAccess,SitingNetworkMode.Road); break;
                case "S23":
                    Districts(DistrictUse.Civic,DistrictUse.MixedUse,DistrictUse.CommercialCenter); Need("public-access",SitingResourceKind.PedestrianCatchment,SitingNetworkMode.Pedestrian); break;
                case "S24":
                    Districts(DistrictUse.Recreation,DistrictUse.CommercialCenter,DistrictUse.MixedUse); sources.Add("R18"); tags.Add("crowd");
                    Need("spectators",SitingResourceKind.PedestrianCatchment,SitingNetworkMode.Pedestrian);
                    Need("transit",SitingResourceKind.Transit,SitingNetworkMode.Road); Need("emergency",SitingResourceKind.EmergencyAccess,SitingNetworkMode.Road); break;
                case "S25":
                    Districts(DistrictUse.Recreation,DistrictUse.Residential,DistrictUse.MixedUse); sources.UnionWith(["R17","R26"]);
                    Need("recreation-access",SitingResourceKind.PedestrianCatchment,SitingNetworkMode.Pedestrian); break;
                case "S26":
                    Districts(DistrictUse.NeighborhoodCommercial,DistrictUse.MixedUse,DistrictUse.CommercialCenter); sources.Add("R04");
                    Need("customers",SitingResourceKind.PedestrianCatchment,SitingNetworkMode.Pedestrian); break;
                case "S27":
                    Districts(DistrictUse.CommercialCenter,DistrictUse.Logistics); sources.Add("R04"); Freight(6);
                    Need("customers",SitingResourceKind.PedestrianCatchment,SitingNetworkMode.Pedestrian); break;
                case "S28":
                    Districts(DistrictUse.Tourism,DistrictUse.MixedUse,DistrictUse.CommercialCenter,DistrictUse.Recreation); sources.Add("R25");
                    Need("guests",SitingResourceKind.PedestrianCatchment,SitingNetworkMode.Pedestrian); break;
                case "S29":
                    Districts(DistrictUse.Agriculture); sources.Add("R16"); Need("farmland",SitingResourceKind.Farmland,contains:true); Freight(3);
                    if (definition.Id=="greenhouse") Need("irrigation",SitingResourceKind.Irrigation,qualities:["crop-compatible"]); break;
                case "S30":
                    Districts(DistrictUse.Agriculture); sources.UnionWith(["R05","R10"]); tags.UnionWith(["heavy-impact","water-contamination"]);
                    Need("feed",SitingResourceKind.Feedstock); Need("manure-handling",SitingResourceKind.WasteHandling); Freight(3); break;
                case "S31":
                    Districts(DistrictUse.Agriculture,DistrictUse.Port,DistrictUse.LightIndustry); sources.Add("R16");
                    Technologies(FacilityTechnology.AquacultureMarine,FacilityTechnology.AquacultureFreshwater,FacilityTechnology.AquacultureRecirculating);
                    Need("marine-water",SitingResourceKind.ProcessWater,qualities:["marine","species-compatible"],origin:SitingResourceOrigin.SurfaceWater,when:[FacilityTechnology.AquacultureMarine]);
                    Need("fresh-water",SitingResourceKind.ProcessWater,qualities:["fresh","species-compatible"],when:[FacilityTechnology.AquacultureFreshwater]);
                    Need("recirculation-makeup",SitingResourceKind.ProcessWater,qualities:["species-compatible"],when:[FacilityTechnology.AquacultureRecirculating]);
                    Need("water-outfall",SitingResourceKind.WaterOutfall); break;
                case "S32":
                    Districts(DistrictUse.Airport); sources.Add("R08"); tags.Add("airport");
                    Need("airside",SitingResourceKind.AirportAirside,contains:true); Need("landside",SitingResourceKind.Transit,SitingNetworkMode.Road); break;
                case "S33":
                    Districts(DistrictUse.Transport,DistrictUse.MixedUse,DistrictUse.Residential,DistrictUse.CommercialCenter); sources.Add("R25");
                    Need("passengers",SitingResourceKind.PedestrianCatchment,SitingNetworkMode.Pedestrian);
                    Need("route",definition.Id=="railway-station"?SitingResourceKind.Railway:SitingResourceKind.Transit,
                        definition.Id=="railway-station"?SitingNetworkMode.Railway:SitingNetworkMode.Road); break;
                case "S34":
                    Districts(DistrictUse.Transport,DistrictUse.Logistics,DistrictUse.HeavyIndustry,DistrictUse.Port,DistrictUse.Airport);
                    sources.Add("R04"); tags.Add("freight-intensive");
                    if (definition.Id=="bus-depot") Need("fleet-route",SitingResourceKind.Transit,SitingNetworkMode.Road,width:6);
                    else Freight(6); break;
                case "S35":
                    Districts(DistrictUse.Port); sources.Add("R13");
                    Need("navigation",SitingResourceKind.NavigableWater,SitingNetworkMode.SiteInterface,origin:SitingResourceOrigin.SurfaceWater,qualities:["navigable"]);
                    Need("landside",SitingResourceKind.Freight,SitingNetworkMode.Road,width:6); break;
                case "S36":
                    Districts(DistrictUse.Transport,DistrictUse.MixedUse,DistrictUse.Logistics,DistrictUse.Port,DistrictUse.Recreation);
                    sources.Add("R25");
                    Need("carried-route",definition.Id=="railway-bridge"?SitingResourceKind.Railway:definition.Id=="pedestrian-bridge"?
                        SitingResourceKind.PedestrianCatchment:SitingResourceKind.Freight,
                        definition.Id=="railway-bridge"?SitingNetworkMode.Railway:definition.Id=="pedestrian-bridge"?SitingNetworkMode.Pedestrian:SitingNetworkMode.Road); break;
                case "S37":
                    Districts(DistrictUse.Port,DistrictUse.Utility,DistrictUse.Transport); sources.Add("R13");
                    Need("water-system",definition.Id=="dam"?SitingResourceKind.ProcessWater:SitingResourceKind.NavigableWater,
                        definition.Id=="canal-lock"?SitingNetworkMode.Waterway:SitingNetworkMode.SiteInterface,
                        origin:SitingResourceOrigin.SurfaceWater,qualities:definition.Id=="dam"?["hydraulic-compatible"]:["navigable"]);
                    Need("maintenance",SitingResourceKind.EmergencyAccess,SitingNetworkMode.Road); break;
                case "S38":
                    Districts(DistrictUse.Recreation,DistrictUse.Residential,DistrictUse.MixedUse,DistrictUse.Civic,DistrictUse.Education,DistrictUse.Tourism,DistrictUse.Conservation);
                    sources.UnionWith(["R17","R26"]); Need("public-access",SitingResourceKind.PedestrianCatchment,SitingNetworkMode.Pedestrian); break;
                case "S39":
                    Districts(DistrictUse.Civic,DistrictUse.Recreation,DistrictUse.Residential); sources.Add("R19");
                    if (definition.Id=="cemetery") { tags.Add("water-contamination"); Need("burial-land",SitingResourceKind.Farmland,qualities:["burial-compatible"],contains:true); }
                    break;
                case "S40":
                    Districts(DistrictUse.Civic,DistrictUse.MixedUse,DistrictUse.Recreation,DistrictUse.CommercialCenter); tags.Add("crowd");
                    Need("community-access",SitingResourceKind.PedestrianCatchment,SitingNetworkMode.Pedestrian); break;
                case "S41":
                    Districts(DistrictUse.Residential,DistrictUse.MixedUse); tags.Add("sensitive");
                    Need("neighborhood-access",SitingResourceKind.PedestrianCatchment,SitingNetworkMode.Pedestrian); break;
                case "S42":
                    Districts(DistrictUse.CommercialCenter,DistrictUse.MixedUse,DistrictUse.Research); Need("customers",SitingResourceKind.PedestrianCatchment,SitingNetworkMode.Pedestrian); break;
                case "S43":
                    Districts(DistrictUse.Transport,DistrictUse.CommercialCenter,DistrictUse.MixedUse,DistrictUse.Logistics);
                    sources.Add("R25"); Need("vehicle-access",SitingResourceKind.Transit,SitingNetworkMode.Road);
                    if (definition.Id=="fuel-station") { tags.Add("water-contamination"); Need("fuel",SitingResourceKind.Fuel); } break;
                case "S44":
                    Districts(DistrictUse.Values.ToArray()); Need("served-asset",SitingResourceKind.ParentFacility); break;
                case "S45":
                    Districts(DistrictUse.SecureInstitution); Need("custody-services",SitingResourceKind.Security);
                    Need("visiting-access",SitingResourceKind.Transit,SitingNetworkMode.Road); break;
            }
        }
        if (definition.Id=="waste-to-energy-plant")
        {
            Technologies(FacilityTechnology.ThermalOnceThrough,FacilityTechnology.ThermalRecirculating,FacilityTechnology.ThermalAirCooled);
            foreach (var key in requirements.Keys.ToArray())
            {
                var r=requirements[key];
                if (r.WhenTechnologies is not { } conditions) continue;
                var allowed=conditions.Intersect(technologies).ToHashSet();
                if (allowed.Count==0) requirements.Remove(key); else requirements[key]=r with { WhenTechnologies=allowed };
            }
        }
        if (definition.Id=="factory") Technologies(FacilityTechnology.CleanLight,FacilityTechnology.HeavyProcess);
        if (definition.Id=="warehouse") Technologies(FacilityTechnology.GeneralStorage,FacilityTechnology.AgriculturalStorage,FacilityTechnology.HazardousStorage);
        if (definition.Id=="workshop") Technologies(FacilityTechnology.CleanLight,FacilityTechnology.HeavyProcess);
        if (definition.Id=="research-laboratory") Technologies(FacilityTechnology.GeneralLaboratory,FacilityTechnology.HazardousLaboratory);
        if (definition.Id=="pumping-station") Technologies(FacilityTechnology.DrinkingWaterPump,FacilityTechnology.WastewaterPump);
        return definition with { SitingProfile=new("siting:"+definition.Id,1,["S01",..ruleIds],sources.Order(StringComparer.Ordinal).ToArray(),
            districts,technologies,tags,requirements.Values.OrderBy(r=>r.Id,StringComparer.Ordinal).ToArray(),
            (definition.SiteRules ?? []).Where(r=>r.Required).Select(r=>r.Kind)
                .Concat(building ? [SiteFeatureKind.AccessWalkway] : Array.Empty<SiteFeatureKind>()).ToHashSet(),ProvidedResources(definition)) };

        void Districts(params string[] uses) => districts.UnionWith(uses);
        void Technologies(params string[] values) { technologies.Clear(); technologies.UnionWith(values); }
        void Freight(double width) => Need("freight",SitingResourceKind.Freight,SitingNetworkMode.Road,width:width);
        void Need(string id,string kind,SitingNetworkMode mode=SitingNetworkMode.Utility,
            string[]? qualities=null,SitingResourceOrigin? origin=null,string[]? when=null,bool contains=false,double width=0) =>
            requirements[id]=new(id,kind,mode,RequiredQualities:qualities?.ToHashSet(StringComparer.Ordinal),RequiredOrigin:origin ?? (contains?SitingResourceOrigin.ResourceArea:null),
                WhenTechnologies:when?.ToHashSet(),SourceContainsSite:contains,MinimumRouteWidthMeters:width);
    }

    public static BuildingDefinition Resolve(BuildingDefinition definition,string technology)
    {
        var profile=definition.SitingProfile ?? throw new ArgumentException("A functional siting profile is required.");
        if (profile.Id!="siting:"+definition.Id) return definition;
        var districts=profile.AllowedDistricts.ToHashSet(); var tags=profile.Tags.ToHashSet(StringComparer.Ordinal);
        var requirements=profile.Requirements.ToList(); var ruleIds=profile.ResearchRuleIds.ToList(); var sources=profile.SourceIds.ToHashSet(StringComparer.Ordinal);
        var site=(definition.SiteRules ?? []).ToList();
        if (definition.Id is "factory" or "workshop" && technology==FacilityTechnology.CleanLight)
        { districts=[DistrictUse.LightIndustry,DistrictUse.MixedUse]; tags.Remove("heavy-impact"); requirements= requirements.Select(r=>r.Id=="freight"?r with { MinimumRouteWidthMeters=3 }:r).ToList(); }
        if (definition.Id is "factory" or "workshop" && technology==FacilityTechnology.HeavyProcess)
        { districts=[DistrictUse.HeavyIndustry]; tags.Add("heavy-impact"); }
        if (definition.Id=="warehouse" && technology==FacilityTechnology.GeneralStorage)
            districts=[DistrictUse.Logistics,DistrictUse.LightIndustry,DistrictUse.HeavyIndustry,DistrictUse.Port];
        if (technology==FacilityTechnology.AgriculturalStorage) districts=[DistrictUse.Agriculture];
        if (technology==FacilityTechnology.HazardousStorage) { districts=[DistrictUse.HeavyIndustry]; tags.Add("heavy-impact"); }
        if (technology==FacilityTechnology.HazardousLaboratory) { districts=[DistrictUse.HeavyIndustry,DistrictUse.Research]; tags.Add("heavy-impact"); }
        if (technology==FacilityTechnology.WastewaterPump)
        {
            districts=[DistrictUse.Utility,DistrictUse.HeavyIndustry]; tags.Remove("water-supply"); tags.Add("water-discharge");
            requirements=requirements.Where(r=>r.Id!="source-water").Append(new("sewer-catchment",SitingResourceKind.Wastewater,SitingNetworkMode.Utility)).ToList();
        }
        if (technology is FacilityTechnology.NuclearOnceThrough or FacilityTechnology.NuclearRecirculating or FacilityTechnology.NuclearAtmospheric)
        { ruleIds.Add("S08"); sources.UnionWith(["R01","R02"]); tags.Add("nuclear"); Extra("security-reservation",SiteFeatureKind.SecurityCompound,SiteBand.Right,5); }
        if (profile.ResearchRuleIds.Contains("S07") || profile.ResearchRuleIds.Contains("S12"))
            Extra("cooling-plant",SiteFeatureKind.CoolingPlant,SiteBand.Left,4);
        if (profile.ResearchRuleIds.Contains("S16")) Extra("treatment-services",SiteFeatureKind.UtilityPlant,SiteBand.Left,4);
        if (profile.ResearchRuleIds.Contains("S31")) Extra("biosecurity-services",SiteFeatureKind.BiosecurityApron,SiteBand.Left,3);
        if (technology is FacilityTechnology.ThermalAirCooled or FacilityTechnology.NuclearAtmospheric or FacilityTechnology.DataCenterAirCooled)
            requirements=requirements.Select(r=>r.Id=="normal-heat"?r with { RequiredOrigin=SitingResourceOrigin.Atmosphere }:r).ToList();
        if (technology==FacilityTechnology.NuclearAtmospheric)
            requirements=requirements.Select(r=>r.Id=="shutdown-heat"?r with { RequiredOrigin=SitingResourceOrigin.Atmosphere }:r).ToList();
        if (technology is FacilityTechnology.ThermalOnceThrough or FacilityTechnology.NuclearOnceThrough)
            requirements=requirements.Select(r=>r.Id=="normal-heat"?r with { RequiredOrigin=SitingResourceOrigin.SurfaceWater }:r).ToList();
        if (technology==FacilityTechnology.OpticalResearch) tags.Add("dark-sky-sensitive");
        if (profile.ResearchRuleIds.Contains("S24")) tags.Add("night-lighting");
        return definition with { SiteRules=site,SitingProfile=profile with { AllowedDistricts=districts,Tags=tags,Requirements=requirements,
            ResearchRuleIds=ruleIds,SourceIds=sources.Order(StringComparer.Ordinal).ToArray(),
            RequiredSurroundings=site.Where(s=>s.Required).Select(s=>s.Kind)
                .Concat(definition.PlacementKind==BuildingPlacementKind.FrontageSite ? [SiteFeatureKind.AccessWalkway] : Array.Empty<SiteFeatureKind>()).ToHashSet() } };
        void Extra(string id,SiteFeatureKind kind,SiteBand band,double depth)
        { site.RemoveAll(s=>s.Band==band); site.Add(new(id,kind,band,depth,MaximumDepthMeters:depth*4)); }
    }

    private static string[] Rules(BuildingDefinition d)
    {
        if (Refinements.TryGetValue(d.Id,out var refined)) return refined;
        return [d.Category switch { BuildingCategory.Residential=>"S41",BuildingCategory.Retail=>"S26",BuildingCategory.FoodHospitality=>"S28",
            BuildingCategory.OfficeFinance=>"S42",BuildingCategory.EducationResearch=>"S19",BuildingCategory.Healthcare=>"S21",
            BuildingCategory.CivicSafety=>"S23",BuildingCategory.CultureWorship=>"S40",BuildingCategory.SportRecreation=>"S25",
            BuildingCategory.TransportLogistics=>"S34",BuildingCategory.IndustryExtraction=>"S02",BuildingCategory.Agriculture=>"S29",
            BuildingCategory.UtilitiesCommunications=>"S11",BuildingCategory.PublicSpace=>"S38",BuildingCategory.LinearWaterInfrastructure=>"S37",
            _=>throw new ArgumentException("Custom catalog families need an explicit siting profile.") }];
    }

    private static IReadOnlySet<string> ProvidedResources(BuildingDefinition d)
    {
        var provided=new HashSet<string> { SitingResourceKind.ParentFacility };
        switch (d.Id)
        {
            case "power-station": case "solar-farm": case "wind-farm": case "waste-to-energy-plant": case "electrical-substation":
                provided.Add(SitingResourceKind.Electricity); break;
            case "water-treatment-plant": case "desalination-plant": case "water-tower": provided.Add(SitingResourceKind.PotableWater); break;
            case "wastewater-treatment-plant": provided.UnionWith([SitingResourceKind.Wastewater,SitingResourceKind.ProcessWater]); break;
            case "telecom-exchange": case "cell-tower": provided.Add(SitingResourceKind.Communications); break;
            case "warehouse": case "distribution-center": case "logistics-center": case "freight-terminal": provided.Add(SitingResourceKind.Freight); break;
            case "fire-station": case "ambulance-depot": case "police-station": provided.Add(SitingResourceKind.EmergencyAccess); break;
            case "waste-transfer-station": case "recycling-center": provided.Add(SitingResourceKind.WasteHandling); break;
        }
        return provided;
    }

    private static readonly IReadOnlyDictionary<string,string[]> Refinements = CreateRefinements();
    private static Dictionary<string,string[]> CreateRefinements()
    {
        var map=new Dictionary<string,string[]>(StringComparer.Ordinal);
        Add(["S03"],"workshop","light-factory","assembly-plant","vehicle-maintenance-workshop");
        Add(["S04"],"warehouse","cold-storage-warehouse","grain-warehouse","container-depot");
        Add(["S04","S34"],"distribution-center","logistics-center","freight-terminal");
        Add(["S05"],"food-processing-plant","catering-kitchen","packing-house"); Add(["S06"],"mine-shaft-building","quarry-facility","oil-well-site");
        Add(["S07"],"power-station"); Add(["S07","S17"],"waste-to-energy-plant"); Add(["S09"],"solar-farm"); Add(["S10"],"wind-farm");
        Add(["S12"],"data-center"); Add(["S13"],"telecom-exchange","cell-tower"); Add(["S14"],"water-treatment-plant","water-tower","pumping-station");
        Add(["S15"],"wastewater-treatment-plant"); Add(["S16"],"desalination-plant"); Add(["S17"],"recycling-center","waste-transfer-station");
        Add(["S18"],"school","kindergarten","primary-school","secondary-school","vocational-school","training-center"); Add(["S20"],"observatory");
        Add(["S22"],"fire-station","ambulance-depot","police-station","civil-defense-center","emergency-shelter");
        Add(["S24"],"football-stadium","multipurpose-stadium","indoor-arena","convention-center","exhibition-center","concert-hall");
        Add(["S26"],"cafe","restaurant","fast-food-restaurant","food-court"); Add(["S26","S05"],"bakery"); Add(["S26","S21"],"pharmacy");
        Add(["S27"],"shopping-mall","hypermarket","supermarket","wholesale-market","car-dealership","furniture-store","department-store","garden-center","hardware-store");
        Add(["S30"],"livestock-shed","poultry-house","dairy-farm","feed-mill","stable"); Add(["S31"],"aquaculture-facility");
        Add(["S32"],"airport-terminal","aircraft-hangar"); Add(["S33"],"bus-stop","bus-station","railway-station"); Add(["S35"],"port-terminal","marina-building");
        Add(["S36"],"road-bridge","railway-bridge","pedestrian-bridge","road-tunnel"); Add(["S38"],"library"); Add(["S23","S38"],"community-center");
        Add(["S39"],"cemetery","funeral-home"); Add(["S43"],"parking-lot","multistorey-car-park","fuel-station","ev-charging-hub");
        Add(["S44"],"guardhouse","watchtower"); Add(["S45"],"prison");
        return map;
        void Add(string[] rules,params string[] ids) { foreach (var id in ids) map.Add(id,rules); }
    }
}

