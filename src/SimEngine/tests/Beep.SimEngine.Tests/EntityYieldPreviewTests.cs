using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Author JSON and simulate normal players. The engine owns all yield, time and disclosure rules.
    internal static class EntityYieldPreviewTests
    {
        internal const string Case = "Complete execution previews: entity yields physical estimates disclosure and atomic observations";
        private static readonly Code Capability = new Code("contract.process");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static TransformationManagementView View(SimGame g) => g.Seat(0).View.Feature<TransformationManagementView>();
        private static TransformationExecutionPreview Read(Check c, SimGame g)
        {
            ulong hash = g.Hash(); long at = g.Clock.StepIndex;
            c.Equal(Code.None, View(g).ReadExecutionPreview(Entity(g, "contract.factory"), Capability, "contract.make", out var facts), "same named engine preview");
            c.That(facts != null && g.Hash() == hash && g.Clock.StepIndex == at, "complete projection performs no state RNG or time mutation");
            return facts;
        }
        private static void Input(Check c, SimGame g, Command command)
        {
            ulong hash = g.Hash(); c.Equal(Code.None, g.Seat(0).Preview(command), "original command evaluator");
            c.Equal(hash, g.Hash(), "command preview pure"); int sequence = g.Seat(0).Submit(command); Step(g);
            long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(x => x.Sequence == sequence).Accepted, "player input applies only in Update");
        }
        private static void Physical(Check c, bool half)
        {
            var factory = Factory(new InitializeProbe(), out _,
                edit: root => root.Get("independent_transformations")[0].Get("duration").Set("numerator", JsonValue.Number("4")),
                setupEdit: root => root.Get("entities")[1].Get("components").Get("transformer").Get("operation").Set("maximum", JsonValue.Number("2")),
                worldEdit: root => { if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30")); });
            var g = ConstructionPlanTests.NewPoints(factory); var limits = new ReplayLimits(4 * 1024 * 1024, 3000); using var recording = g.RecordReplay(limits);
            var idle = Read(c, g); long required = half ? 8 : 4;
            c.That(idle.EntityOutput == "contract.result" && idle.Current.EntityOutput == null && idle.EstimateStatus == ProductiveEstimateStatus.NotHead,
                "authored entity yield exists before any queue head or actual result");
            c.That(idle.Outputs.IsEmpty && idle.Inputs.Lines.Single().Amount == Fix.FromInt(3) && idle.Tools.Lines.Single().Amount == Fix.One,
                "entity yield never replaces resource inputs or reusable tools with fabricated stock");
            c.Equal(required, g.Clock.Time.Ticks(idle.NominalCycle.Value), "actual authored nominal throughput period");
            c.Throws<NotSupportedException>(() => ((IList<TransformationQueueEntry>)idle.Current.Queue).Clear(), "queue projection immutable");
            c.Throws<NotSupportedException>(() => ((IList<ResourceRateInfo>)idle.OutputRates).Clear(), "rate projection immutable");
            Input(c, g, new ConfigureWorkPolicyCommand { Worker = Entity(g, "machine"), Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
            Input(c, g, new EnqueueTransformationCommand { Transformer = Entity(g, "contract.factory"), Capability = Capability, Transformation = "contract.make" });
            var supply = Read(c, g);
            c.That(supply.EstimateStatus == ProductiveEstimateStatus.Blocked && supply.Current.Phase == TransformationPhase.Supplying
                && !supply.RemainingProductiveTime.HasValue && supply.EntityOutput == "contract.result", "physical supply blocker keeps authored yield but invents no ETA");
            Until(g, null, () => Process(g).Phase == TransformationPhase.Working, "physically hauled preview inputs");
            var work = Read(c, g);
            c.That(work.EstimateStatus == ProductiveEstimateStatus.Conditional && work.ProductiveCompletionStep == g.Clock.StepIndex + required - 1,
                "productive estimate uses actual elapsed work, not instant entity yield");
            Input(c, g, new ConfigureOperationCommand { Entity = Entity(g, "contract.factory"), Operation = Capability, Enabled = false, Target = Fix.One });
            var disabled = Read(c, g);
            c.That(disabled.CurrentBlocker == OperationCodes.Disabled && disabled.EstimateStatus == ProductiveEstimateStatus.Blocked
                && disabled.EntityOutput == "contract.result" && disabled.Current.Progress.Equals(work.Current.Progress), "disabled work retains WIP and yield without productive ETA");
            Input(c, g, new ConfigureOperationCommand { Entity = Entity(g, "contract.factory"), Operation = Capability, Enabled = true, Target = Fix.FromRatio(3, 2) });
            var changed = Read(c, g);
            c.That(changed.EstimateStatus == ProductiveEstimateStatus.Conditional && changed.Current.Progress.Equals(new WorkProgress(2, 1u << 31)), "fractional target advances original work once");
            c.Equal(half ? 6L : 3L, g.Clock.Time.Ticks(changed.NominalCycle.Value), "entity yield throughput uses exact original ceiling");
            long predicted = changed.ProductiveCompletionStep.Value; var twin = factory.Load(g.Save());
            g.Clock.Pause(); twin.Clock.Pause(); ulong paused = g.Hash(); long at = g.Clock.StepIndex;
            var pause = Read(c, g); Step(g, twin);
            c.That(pause.EstimateStatus == ProductiveEstimateStatus.Paused && pause.EntityOutput == "contract.result"
                && !pause.ProductiveCompletionStep.HasValue && g.Hash() == paused && g.Clock.StepIndex == at, "sole paused clock preserves detached yield and removes schedule");
            g.Clock.Resume(); twin.Clock.Resume(); Until(g, twin, () => Process(g).QueueCount == 0, "physical entity yield completion");
            c.Equal(predicted, g.Clock.StepIndex, "productive estimate matches unchanged-rate physical completion");
            c.That(g.Seat(0).Snapshot.Entities.Count(x => x.Def == "contract.result") == 1 && Stock(g, "source") == Fix.FromInt(16)
                && Stock(g, "contract.factory") == Fix.One, "one earned actual entity, three consumed inputs and one retained tool");
            c.That(Read(c, g).EntityOutput == "contract.result" && idle.Current.QueueCount == 0 && idle.Current.EntityOutput == null
                && work.Current.Progress.WholeTicks == 1 && work.EntityOutput == "contract.result", "recipe yield and retained execution facts never mutate with completion");
            c.Equal(g.Hash(), factory.Load(g.Save()).Hash(), "completed yield checkpoint identical");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "public work policy queue control pause and entity yield replay");
        }
        private static void Disclosure(Check c, SimGame g, bool known)
        {
            ulong hash = g.Hash(); Code refusal = View(g).ReadExecutionPreview(Entity(g, "contract.factory"), Capability, "contract.make", out var facts);
            c.That(known ? refusal.IsNone && facts.EntityOutput == "contract.result" && facts.Current.EntityOutput == null
                : refusal == Codes.UnknownDef && facts == null, "authored entity yield is detached and disclosed only after physical discovery");
            c.Equal(hash, g.Hash(), "yield disclosure changes no random stream state or clock");
        }
        private static void Metadata(Check c)
        {
            foreach (bool batch in new[] { true, false })
            {
                var module = new RecipeObservationModule(); var factory = RecipeObservationTests.Create(module, batch: batch);
                var g = ConstructionPlanTests.NewPoints(factory); var facts = Read(c, g);
                c.That(facts.EntityOutput == null && facts.Current.EntityOutput == null && facts.Tools.Lines.Single().Amount == Fix.One,
                    "resource-only authored recipe creates no fictional entity yield");
                if (batch)
                {
                    c.That(facts.Inputs.Lines.Single().Amount == Fix.FromInt(2) && facts.Outputs.Lines.Single().Amount == Fix.One
                        && facts.ToolConsumption.Lines.Single().Amount == Fix.One && facts.InputRates.Count == 0 && facts.OutputRates.Count == 0,
                        "exact batch input2 yield1 tool-consumption1 are detached authored quantities");
                    c.Equal(1L, g.Clock.Time.Ticks(facts.NominalCycle.Value), "resource batch throughput per original one-minute cycle");
                    c.Throws<NotSupportedException>(() => ((IList<ResourceAmount>)facts.Outputs.Lines).Clear(), "yield bill immutable");
                }
                else
                {
                    c.That(facts.Inputs.IsEmpty && facts.Outputs.IsEmpty && facts.ToolConsumption.IsEmpty && facts.NominalCycle == null,
                        "continuous yields remain rates, not substituted finite bills/cycle");
                    c.Equal(Fix.FromInt(2), facts.InputRates.Single().Quantity, "exact authored continuous input throughput");
                    c.Equal(Fix.One, facts.OutputRates.Single().Quantity, "exact authored continuous output throughput");
                    c.Equal(Fix.One, facts.ToolConsumptionRates.Single().Quantity, "exact authored continuous tool wear");
                    foreach (var rates in new[] { facts.InputRates, facts.OutputRates, facts.ToolConsumptionRates })
                    {
                        c.That(rates.Single().PeriodNumerator == 1 && rates.Single().PeriodDenominator == 1
                            && rates.Single().PeriodUnit == DurationUnit.Minute && rates.Single().Resource.Id == "supply", "native exact resource and period identity");
                        c.Throws<NotSupportedException>(() => ((IList<ResourceRateInfo>)rates).Clear(), "throughput rates immutable");
                    }
                }
                c.That(facts.OperatingRates.Single().Quantity == Fix.One && facts.OperatingRates.Single().PeriodNumerator == 1
                    && facts.OperatingRates.Single().PeriodDenominator == 1 && facts.OperatingRates.Single().PeriodUnit == DurationUnit.Minute,
                    "actual one-unit operating consumption per minute, not productive target multiplication");
                c.Equal(g.Hash(), factory.Load(g.Save()).Hash(), "resource yield/rate facts add no checkpoint payload");
            }
        }
        private static void Faults(Check c)
        {
            foreach (string site in new[] { "output", "cost", "bill", "visibility", "support", "gate", "ports", "recipe", "rate", "resource", "unit", "period" })
                foreach (ResourceSnapshotFault mode in Enum.GetValues(typeof(ResourceSnapshotFault)))
                {
                    var module = new RecipeObservationModule(); bool continuous = site == "recipe" || site == "rate" || site == "resource" || site == "unit" || site == "period";
                    var factory = RecipeObservationTests.Create(module, management: true, edit: (setup, document) =>
                    {
                        if (!continuous) return;
                        var flow = Json.Parse(FrameworkProofContent.Read("transformation-metadata.json")).Get("metadata_transformations")[1].Clone();
                        flow.Remove("fault"); flow.Set("id", JsonValue.Of("contract.make"));
                        var entries = JsonValue.NewArray(); entries.Add(flow);
                        for (int i = 1; i < document.Get("observed_recipes").Count; i++) entries.Add(document.Get("observed_recipes")[i]);
                        document.Set("observed_recipes", entries);
                    });
                    var g = ConstructionPlanTests.NewPoints(factory); var retained = Read(c, g); long at = g.Clock.StepIndex;
                    c.That(continuous ? retained.EntityOutput == null && retained.OutputRates.Count == 1 : retained.EntityOutput == "contract.result", "actual healthy yield before fault");
                    TransformationExecutionPreview leaked = retained; module.Arm(site, mode);
                    if (site == "ports")
                    {
                        // Execution metadata does not inspect physical bindings. The port view does.
                        ulong pure = g.Hash();
                        c.Equal(Code.None, View(g).ReadExecutionPreview(Entity(g, "contract.factory"), Capability, "contract.make", out leaked), "unused port callback cannot poison recipe yield");
                        c.That(module.Reads == 0 && g.Hash() == pure && g.Fault == null, "complete preview does not invoke unrelated binding callbacks");
                        IReadOnlyList<ResourcePortSnapshot> ports = null;
                        c.Throws<InvalidOperationException>(() => View(g).ReadInputPorts(Entity(g, "contract.factory"), Capability, "contract.make", out ports), "dedicated actual port projection guards callback");
                        c.That(ports.Count == 0 && g.Fault != null, "failed binding projection publishes no port prefix");
                    }
                    c.Throws<InvalidOperationException>(() => View(g).ReadExecutionPreview(Entity(g, "contract.factory"), Capability, "contract.make", out leaked), "complete projection refuses callback " + site + "/" + mode);
                    c.That(leaked == null && g.Fault != null && module.Reads == 1, "no output identity or metadata prefix escapes failed observation");
                    var first = g.Fault; g.Update(); c.That(ReferenceEquals(first, g.Fault) && g.Clock.StepIndex == at, "first observation fault stops actual gameplay/time");
                    c.Throws<InvalidOperationException>(() => g.Save(), "faulted metadata cannot become saved gameplay");
                    c.That(retained.Current.QueueCount == 0 && retained.Transformation == "contract.make", "prior detached facts preserved");
                }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current engine yield protocol");
            Metadata(c);
            Physical(c, false); Physical(c, true);
            foreach (bool direct in new[] { false, true }) foreach (string hidden in new[] { "resource", "quantity", "capacity", "result" })
                RecipeDisclosureTests.Physical(c, direct, hidden, hidden == "quantity" || hidden == "capacity", Disclosure);
            foreach (bool half in new[] { false, true }) ContinuousPreviewStateTests.Physical(c, half);
            foreach (bool point in new[] { false, true }) foreach (bool half in new[] { false, true }) ReturnExecutionPreviewTests.Physical(c, point, half);
            Faults(c);
            foreach (string fault in new[] { "rng", "rng-restore", "rng-same" }) RandomObservationPurityTests.PreviewFault(c, fault);
        }
    }
}
