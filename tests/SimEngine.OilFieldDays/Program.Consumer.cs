namespace Beep.SimEngine.Tests
{
    public static partial class Program
    {
        static partial void RegisterConsumerTests()
        {
            ConditionalOperatingCostTests.Register();
            OilActivityAuthoringTests.Register();
            OilAuthoredInfrastructureTests.Register();
            OilAuthoredNavigationTests.Register();
            OilCanonicalManufacturingTests.Register();
            OilConstructionAuthoringTests.Register();
            OilFinancialOwnershipTests.Register();
            OilManifestContentTests.Register();
            OilProductionAuthoringTests.Register();
            OilSessionTimeTests.Register();
            OilStorageAuthoringTests.Register();
            OilVehicleAuthoringTests.Register();
            TycoonOperatingCostTests.Register();
        }
    }
}
