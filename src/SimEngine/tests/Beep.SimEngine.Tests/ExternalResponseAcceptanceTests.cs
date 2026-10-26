namespace Beep.SimEngine.Tests
{
    // Same thin public driver, new externally owned content/providers. No historical suite invocation.
    internal static class ExternalResponseAcceptanceTests
    {
        internal const string Case = "External response acceptance: engine-owned episode workflow and environmental providers";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Run(Check c)
        {
            foreach (bool halfMinute in new[] { false, true })
                EnvironmentalResponseAcceptanceTests.Scenario(c, halfMinute, external: true);
        }
    }
}
