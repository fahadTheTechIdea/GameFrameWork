namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        static partial void RegisterConsumerCases()
        {
            TestRunner.Add(NoMoneyRoadCase, c => OptionalMoneyRoadRun(c, 0));
            TestRunner.Add(DormantMoneyRoadCase, c => OptionalMoneyRoadRun(c, 1));
            TestRunner.Add(PaidRoadCase, c => OptionalMoneyRoadRun(c, 2));
            TestRunner.Add(UpkeepDurabilityCase, UpkeepDurabilityRun);
        }
    }
}
