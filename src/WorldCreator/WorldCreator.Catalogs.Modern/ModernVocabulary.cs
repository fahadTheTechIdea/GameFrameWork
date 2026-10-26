using System.Collections.Frozen;

namespace WorldCreator.Catalogs.Modern;

public static class BuildingFunction
{
    public const string Housing = "Housing";
    public const string FoodRetail = "FoodRetail";
    public const string Education = "Education";
    public const string Health = "Health";
    public const string Administration = "Administration";
    public const string Employment = "Employment";
    public const string Storage = "Storage";
    public const string Sports = "Sports";
    public const string Hospitality = "Hospitality";
    public const string Worship = "Worship";
    public const string Transport = "Transport";
    public const string Utilities = "Utilities";
    public const string Retail = "Retail";
    public const string FoodService = "FoodService";
    public const string Finance = "Finance";
    public const string PublicSafety = "PublicSafety";
    public const string Culture = "Culture";
    public const string Recreation = "Recreation";
    public const string Agriculture = "Agriculture";
    public const string Research = "Research";
    public const string Communications = "Communications";
    public const string Sanitation = "Sanitation";
    public const string Infrastructure = "Infrastructure";
    public const string ResourceExtraction = "ResourceExtraction";
    public static IReadOnlySet<string> Values { get; } = new HashSet<string>(StringComparer.Ordinal) { Housing,FoodRetail,Education,Health,Administration,Employment,Storage,Sports,Hospitality,Worship,Transport,Utilities,Retail,FoodService,Finance,PublicSafety,Culture,Recreation,Agriculture,Research,Communications,Sanitation,Infrastructure,ResourceExtraction }.ToFrozenSet(StringComparer.Ordinal);
}

public static class BuildingCategory
{
    public const string Unspecified = "Unspecified";
    public const string Residential = "Residential";
    public const string Retail = "Retail";
    public const string FoodHospitality = "FoodHospitality";
    public const string OfficeFinance = "OfficeFinance";
    public const string EducationResearch = "EducationResearch";
    public const string Healthcare = "Healthcare";
    public const string CivicSafety = "CivicSafety";
    public const string CultureWorship = "CultureWorship";
    public const string SportRecreation = "SportRecreation";
    public const string TransportLogistics = "TransportLogistics";
    public const string IndustryExtraction = "IndustryExtraction";
    public const string Agriculture = "Agriculture";
    public const string UtilitiesCommunications = "UtilitiesCommunications";
    public const string PublicSpace = "PublicSpace";
    public const string LinearWaterInfrastructure = "LinearWaterInfrastructure";
    public static IReadOnlySet<string> Values { get; } = new HashSet<string>(StringComparer.Ordinal) { Unspecified,Residential,Retail,FoodHospitality,OfficeFinance,EducationResearch,Healthcare,CivicSafety,CultureWorship,SportRecreation,TransportLogistics,IndustryExtraction,Agriculture,UtilitiesCommunications,PublicSpace,LinearWaterInfrastructure }.ToFrozenSet(StringComparer.Ordinal);
}

public static class DistrictUse
{
    public const string Residential = "Residential";
    public const string MixedUse = "MixedUse";
    public const string NeighborhoodCommercial = "NeighborhoodCommercial";
    public const string CommercialCenter = "CommercialCenter";
    public const string LightIndustry = "LightIndustry";
    public const string HeavyIndustry = "HeavyIndustry";
    public const string Logistics = "Logistics";
    public const string Utility = "Utility";
    public const string Civic = "Civic";
    public const string Education = "Education";
    public const string Healthcare = "Healthcare";
    public const string Recreation = "Recreation";
    public const string Tourism = "Tourism";
    public const string Agriculture = "Agriculture";
    public const string Conservation = "Conservation";
    public const string Airport = "Airport";
    public const string Port = "Port";
    public const string Transport = "Transport";
    public const string Research = "Research";
    public const string SecureInstitution = "SecureInstitution";
    public static IReadOnlySet<string> Values { get; } = new HashSet<string>(StringComparer.Ordinal) { Residential,MixedUse,NeighborhoodCommercial,CommercialCenter,LightIndustry,HeavyIndustry,Logistics,Utility,Civic,Education,Healthcare,Recreation,Tourism,Agriculture,Conservation,Airport,Port,Transport,Research,SecureInstitution }.ToFrozenSet(StringComparer.Ordinal);
}

public static class FacilityTechnology
{
    public const string Standard = "Standard";
    public const string CleanLight = "CleanLight";
    public const string HeavyProcess = "HeavyProcess";
    public const string GeneralStorage = "GeneralStorage";
    public const string AgriculturalStorage = "AgriculturalStorage";
    public const string HazardousStorage = "HazardousStorage";
    public const string ThermalOnceThrough = "ThermalOnceThrough";
    public const string ThermalRecirculating = "ThermalRecirculating";
    public const string ThermalAirCooled = "ThermalAirCooled";
    public const string NuclearOnceThrough = "NuclearOnceThrough";
    public const string NuclearRecirculating = "NuclearRecirculating";
    public const string NuclearAtmospheric = "NuclearAtmospheric";
    public const string DataCenterAirCooled = "DataCenterAirCooled";
    public const string DataCenterEvaporative = "DataCenterEvaporative";
    public const string AquacultureMarine = "AquacultureMarine";
    public const string AquacultureFreshwater = "AquacultureFreshwater";
    public const string AquacultureRecirculating = "AquacultureRecirculating";
    public const string DrinkingWaterPump = "DrinkingWaterPump";
    public const string WastewaterPump = "WastewaterPump";
    public const string SeawaterDesalination = "SeawaterDesalination";
    public const string BrackishDesalination = "BrackishDesalination";
    public const string GeneralLaboratory = "GeneralLaboratory";
    public const string HazardousLaboratory = "HazardousLaboratory";
    public const string Photovoltaic = "Photovoltaic";
    public const string SolarThermal = "SolarThermal";
    public const string OpticalResearch = "OpticalResearch";
    public const string TeachingObservatory = "TeachingObservatory";
    public static IReadOnlySet<string> Values { get; } = new HashSet<string>(StringComparer.Ordinal) { Standard,CleanLight,HeavyProcess,GeneralStorage,AgriculturalStorage,HazardousStorage,ThermalOnceThrough,ThermalRecirculating,ThermalAirCooled,NuclearOnceThrough,NuclearRecirculating,NuclearAtmospheric,DataCenterAirCooled,DataCenterEvaporative,AquacultureMarine,AquacultureFreshwater,AquacultureRecirculating,DrinkingWaterPump,WastewaterPump,SeawaterDesalination,BrackishDesalination,GeneralLaboratory,HazardousLaboratory,Photovoltaic,SolarThermal,OpticalResearch,TeachingObservatory }.ToFrozenSet(StringComparer.Ordinal);
}

public static class SitingResourceKind
{
    public const string Electricity = "Electricity";
    public const string PotableWater = "PotableWater";
    public const string ProcessWater = "ProcessWater";
    public const string CoolingWater = "CoolingWater";
    public const string Wastewater = "Wastewater";
    public const string HeatRejection = "HeatRejection";
    public const string GridExport = "GridExport";
    public const string Fuel = "Fuel";
    public const string Freight = "Freight";
    public const string PedestrianCatchment = "PedestrianCatchment";
    public const string Transit = "Transit";
    public const string Railway = "Railway";
    public const string NavigableWater = "NavigableWater";
    public const string Communications = "Communications";
    public const string Farmland = "Farmland";
    public const string Feedstock = "Feedstock";
    public const string MineralDeposit = "MineralDeposit";
    public const string EmergencyAccess = "EmergencyAccess";
    public const string DarkSky = "DarkSky";
    public const string Wind = "Wind";
    public const string Solar = "Solar";
    public const string WasteHandling = "WasteHandling";
    public const string Security = "Security";
    public const string AirportAirside = "AirportAirside";
    public const string ParentFacility = "ParentFacility";
    public const string Irrigation = "Irrigation";
    public const string WaterOutfall = "WaterOutfall";
    public static IReadOnlySet<string> Values { get; } = new HashSet<string>(StringComparer.Ordinal) { Electricity,PotableWater,ProcessWater,CoolingWater,Wastewater,HeatRejection,GridExport,Fuel,Freight,PedestrianCatchment,Transit,Railway,NavigableWater,Communications,Farmland,Feedstock,MineralDeposit,EmergencyAccess,DarkSky,Wind,Solar,WasteHandling,Security,AirportAirside,ParentFacility,Irrigation,WaterOutfall }.ToFrozenSet(StringComparer.Ordinal);
}

