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
    // JSON, ordinary seat commands, Update and immutable views only; no host utility/gameplay rules.
    internal static class UtilityOwnerObservationTests
    {
        internal const string Case = "Utility owner observation: timed supply and sibling getter faults stay engine owned";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool independent, bool half, UtilityOwnerObservationModule negative = null,
            string phase = null, string mode = null)
        {
            var doc = Json.Parse(FrameworkProofContent.Read("utility-owner-observation.json"));
            if (negative != null) { doc.Get("utility_owner_failures")[0].Set("phase", JsonValue.Of(phase));
                doc.Get("utility_owner_failures")[0].Set("mode", JsonValue.Of(mode)); }
            return UtilityResearchContractTests.Factory(new UtilityResearchContractsModule(), independent,
                extraModule: negative,
                extraDeclaration: negative == null ? null : FrameworkProofContent.Read("utility-owner-observation-module.json"),
                extraDocuments: negative == null ? null : new Dictionary<string,string> { ["utility-owner-observation.json"] = doc.ToString() },
                configure: (tech, setup) => {
                    if (negative == null) return;
                    var parts = setup.Get("entities")[1].Get("components");
                    parts.Set("utility_owner_witness", JsonValue.NewObject());
                    parts.Set("utility_owner_probe", Json.Parse("{\"id\":\"utility.observer\",\"source\":\"contract.process\",\"failure\":\"utility.owner-failure\"}"));
                },
                worldEdit: root => { if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30")); });
        }
        private static SimGame New(GameFactory f) => ConstructionPlanTests.NewPoints(f);
        private static UtilityDemandSnapshot Demand(SimGame g, string source = "contract.process")
        {
            if (!g.Seat(0).View.Feature<OperationsView>().TryGetUtilityDemand(Entity(g, "contract.factory"), new Code(source), out var facts))
                throw new InvalidOperationException("missing actual named utility"); return facts;
        }
        private static ActivityNodeView Node(SimGame g, int order)
        { var nodes = new List<ActivityNodeView>(); g.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes); return nodes.Single(n => n.Id == "research"); }
        private static bool Known(SimGame g, int seat = 0)
        { var values = new List<TechnologySnapshot>(); g.Seat(seat).View.Feature<TechnologyView>().CopyTechnologies(values);
            return values.Single(t => t.Technology == "research.foundation").Known; }
        private static void Reach(SimGame g, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 600 && !done(); i++) Step(g, twin);
            if (!done()) throw new InvalidOperationException(stage + " at=" + g.Clock.StepIndex + " supply=" + Stock(g, "contract.factory")
                + " cargo=" + Stock(g, "lab.carrier") + " utility=" + Demand(g).Blocker + " processor=" + Process(g).Phase);
        }
        private static void Command(Check c, SimGame g, Command command) => PermanentRemovalTests.Submit(c, g, command);
        private static void Control(Check c, SimGame g, bool enabled) => Command(c, g, new ConfigureOperationCommand {
            Entity = Entity(g, "contract.factory"), Operation = new Code("contract.process"), Enabled = enabled, Target = Fix.One });
        private static void Physical(Check c, bool independent, bool half)
        {
            var f = Factory(independent, half); var g = New(f); var idle = New(f); ulong isolated = idle.Hash();
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "utilities and work require no business module");
            c.Equal(UtilityCodes.Inactive, Demand(g).Blocker, "idle consumer is not invented readiness");
            Command(c, g, new ConfigureWorkPolicyCommand { Worker = Entity(g, "machine"), Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, "lab.research-area", true, true, WorkAssignmentMode.All) });
            Command(c, g, new ConfigureWorkPolicyCommand { Worker = Entity(g, "lab.carrier"), Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("haul") }, "lab.carrier-area", true, true, WorkAssignmentMode.All) });
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = g.RecordReplay(limits))
            {
                Command(c, g, new EnqueueTransformationCommand { Transformer = Entity(g, "contract.factory"), Capability = new Code("contract.process"), Transformation = "contract.make" });
                int order = checked((int)PermanentRemovalTests.Submit(c, g, new PostOrderCommand {
                    Plan = "research.foundation", Target = Entity(g, "contract.factory"), Priority = 70 }).Arg0);
                Reach(g, null, () => Node(g, order).Progress.WholeTicks > 0 && !Known(g), "real staffed utility research");
                var retained = Demand(g); var old = Node(g, order); ulong before = g.Hash(); var direct = Demand(g, "lab.utility");
                c.That(direct.ElapsedTicks == retained.ElapsedTicks && direct.Consumed.Lines.SequenceEqual(retained.Consumed.Lines), "same sole timed execution owner");
                c.Equal(before, g.Hash(), "whole owner observations pure");
                var owned = g.Seat(0).View.Feature<OperationsView>(); EntityId entity = Entity(g, "contract.factory");
                c.That(owned.TryGetOperation(entity, new Code("contract.process"), out _) && owned.TryGetControl(entity, new Code("contract.process"), out _)
                    && owned.TryGetOperationSchema(entity, new Code("contract.process"), out _), "existing operation reads still valid");
                c.That(!g.Seat(1).View.Feature<OperationsView>().TryGetUtilityDemand(entity, new Code("lab.utility"), out _), "private utility facts withheld");
                Control(c, g, false); Reach(g, null, () => Node(g, order).State == NodeState.Paused, "real shutdown");
                var paused = Node(g, order); var stopped = Demand(g); var twin = f.Load(g.Save());
                for (int i = 0; i < 4; i++) Step(g, twin);
                c.That(Node(g, order).Progress.Equals(paused.Progress) && Demand(g).ElapsedTicks == stopped.ElapsedTicks, "shutdown earns and consumes nothing");
                long time = g.Clock.StepIndex; g.Clock.Pause(); twin.Clock.Pause(); Step(g, twin);
                c.Equal(time, g.Clock.StepIndex, "one clock pauses all execution"); g.Clock.Resume(); twin.Clock.Resume();
                Control(c, g, true); Reach(g, null, () => Node(g, order).Blocker == UtilityCodes.Shortage, "finite physical shortage");
                var shortage = Node(g, order); var exhausted = Demand(g); twin = f.Load(g.Save());
                for (int i = 0; i < 3; i++) Step(g, twin);
                c.That(shortage.Progress.WholeTicks > 0 && !Known(g) && Node(g, order).Progress.Equals(shortage.Progress)
                    && Demand(g).ElapsedTicks == exhausted.ElapsedTicks, "shortage preserves partial progress without fake inputs");
                g.Seat(0).View.TryFindResource("supply", out var supply);
                Command(c, g, new HaulResourcesCommand { Source = Entity(g, "source"), Destination = entity, Worker = Entity(g, "lab.carrier"),
                    DestinationPort = new Code("operating.input"), Resource = supply, Ceiling = Fix.FromInt(8), Priority = 90 });
                c.That(!Known(g), "submitted haul is not instant delivery or research"); twin = f.Load(g.Save());
                Reach(g, twin, () => Known(g), "real movement/refuel/research completion"); var final = Demand(g);
                c.That(!Known(g, 1) && final.ElapsedTicks > exhausted.ElapsedTicks && final.Consumed.AmountOf(supply) > exhausted.Consumed.AmountOf(supply), "actual supply resumes sole consumer");
                c.Equal(Fix.FromInt(36), Stock(g, "source") + Stock(g, "machine") + Stock(g, "lab.carrier") + Stock(g, "contract.factory")
                    + Fix.FromInt(8) + final.Consumed.AmountOf(supply), "all stock conserved across physical work and utility burn");
                c.That(old.Progress.WholeTicks > 0 && old.Progress.WholeTicks < old.RequiredTicks && retained.ElapsedTicks < final.ElapsedTicks, "retained facts detached");
                c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "completed original owners durable"); c.Equal(isolated, idle.Hash(), "interleaved session isolated");
                c.That(new ReplayVerifier().Verify(f, recorder.Finish(), limits).Success, "player commands and sole clock replay");
            }
        }
        private static void Failure(Check c, bool half, string phase, string mode)
        {
            var module = new UtilityOwnerObservationModule(); var f = Factory(true, half, module, phase, mode); var g = New(f);
            var independent = New(Factory(true, half)); ulong isolated = independent.Hash();
            var cached = g.Seat(0).View.Feature<OperationsView>(); var foreign = g.Seat(1).View.Feature<OperationsView>();
            EntityId id = Entity(g, "contract.factory"); var retained = Demand(g, "utility.observer");
            Command(c, g, new EnqueueTransformationCommand { Transformer = id, Capability = new Code("contract.process"), Transformation = "contract.make" });
            while (g.Clock.StepIndex < (half ? 6 : 3) && g.Fault == null) g.Update();
            UtilityDemandSnapshot failed = retained; Exception error = null;
            if (phase == "view") { try { cached.TryGetUtilityDemand(id, new Code("utility.observer"), out failed); } catch (Exception actual) { error = actual; } }
            c.That(g.Fault != null && !module.Returned, "actual " + phase + "/" + mode + " does not publish healthy core facts");
            if (g.Fault == null) throw new InvalidOperationException("missing utility fault " + phase + "/" + mode);
            if (phase == "view") { c.That(error is InvalidOperationException && failed.Consumer.IsNone && failed.Ports == null, "no partial snapshot");
                if (mode == "clock-throw") c.Equal("proof.utility-owner-first-error", error.Message, "callback first error not masked"); }
            if (mode == "clock-throw") c.That(g.Fault.ToString().Contains("proof.utility-owner-first-error"), "first error retained in terminal fault");
            var first = g.Fault;
            foreach (var view in new[] { cached, foreign }) foreach (Code code in new[] { Code.None, new Code("missing"), new Code("utility.observer") })
            {
                c.Throws<InvalidOperationException>(() => view.TryGetUtilityDemand(id, code, out _), "faulted private/missing utility read");
                c.Throws<InvalidOperationException>(() => view.TryGetOperation(id, code, out _), "faulted operation read");
                c.Throws<InvalidOperationException>(() => view.TryGetControl(id, code, out _), "faulted control read");
                c.Throws<InvalidOperationException>(() => view.TryGetOperationSchema(id, code, out _), "faulted schema read");
            }
            c.Throws<InvalidOperationException>(() => g.Save(), "faulted utility cannot save"); c.Throws<InvalidOperationException>(() => g.Hash(), "faulted utility cannot hash healthy state");
            c.Equal(0, g.Update(), "terminal Update refuses"); c.That(ReferenceEquals(first, g.Fault), "first fault authoritative");
            c.That(retained.Consumer == new Code("utility.observer") && retained.ElapsedTicks == 0, "retained pure facts unchanged");
            c.Equal(isolated, independent.Hash(), "fault isolated from another session"); Step(independent);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current engine identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current original payload");
            foreach (bool independent in new[] { false, true }) foreach (bool half in new[] { false, true }) Physical(c, independent, half);
            foreach (bool half in new[] { false, true }) foreach (string phase in new[] { "update", "view" })
                foreach (string mode in new[] { "getter", "demand", "port", "clock-throw" }) Failure(c, half, phase, mode);
        }
    }
}
