using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Authored selected sight module and public physical player inputs; no state writers.
    internal static class OperationalVisibilityTests
    {
        internal const string Case = "Operational visibility: hidden operation motion and logistics facts share original sight";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half, DirectResourceContentModule resources, string target)
            => ResourceContentContractTests.Factory(half, resources, worldEdit: root =>
            {
                var scenario = root.Get("scenarios")[0]; scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                if (scenario.Get("settings") == null) scenario.Set("settings", JsonValue.NewObject());
                var settings = Json.Parse("{\"target_kind\":\"entity\",\"target\":\"destination\",\"initial_hidden\":false}");
                settings.Set("target", JsonValue.Of(target)); scenario.Get("settings").Set(InventorySightModule.ModuleId, settings);
            }, extraModules: new GameModule[] { new InventorySightModule() },
                extraDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
        private static void Hidden(Check c, SimGame g, EntityId id, ResourceId resource, bool machine)
        {
            ulong before = g.Hash(); long time = g.Clock.StepIndex;
            var view = g.Seat(0).View;
            c.That(!view.TryGetEntity(id, out _), "actual selected sight policy hides current entity");
            var operations = view.Feature<OperationsView>(); var codes = new List<Code> { new Code("seed") };
            c.That(!operations.CopyOperationIds(id, codes) && codes.Count == 0, "hidden operation identity list clears caller seed");
            codes.Add(new Code("seed")); c.That(!operations.CopySignalSourceIds(id, codes) && codes.Count == 0, "hidden signal identities publish no prefix");
            codes.Add(new Code("seed")); c.That(!operations.CopyUtilityConsumerIds(id, codes) && codes.Count == 0, "hidden utility identities publish no prefix");
            c.That(!operations.TryGetOperation(id, new Code("process"), out _) && !operations.TryGetControl(id, new Code("process"), out _), "hidden operation and control facts refuse");
            c.That(!operations.TryGetOperationSchema(id, new Code("process"), out var schema) && schema == null, "hidden operation schema cannot expose provider capability");
            c.That(!operations.TryGetSignalSchema(id, new Code("process"), out var signals) && signals == null, "hidden signal schema remains absent");
            c.That(!operations.TryGetUtilityDemand(id, new Code("process"), out _), "hidden utility demand returns no live facts");
            var logistics = view.Feature<LogisticsView>();
            c.Equal(Codes.NotFound, logistics.ReadDiagnostics(id, resource, out var diagnostic), "cached hidden ID cannot read live logistics");
            c.That(diagnostic == null, "hidden logistics outputs no object");
            c.Equal(Codes.NotFound, logistics.ReadIncomingThroughput(id, resource, 0, g.Clock.StepIndex, out var throughput), "hidden throughput query does not disclose activity");
            c.That(throughput == null, "hidden throughput outputs no object");
            var motion = view.Feature<MotionView>();
            c.Equal(Codes.NotFound, motion.ReadNavigation(id, out var navigation), "hidden navigation facts refuse before unsupported/provider lookup");
            c.Equal(Codes.NotFound, motion.ReadExecution(id, out var execution), "hidden movement execution refuses");
            c.Equal(Codes.NotFound, motion.ReadArrival(id, out var arrival), "hidden arrival refuses");
            c.Equal(Codes.NotFound, motion.ReadOperatingInputs(id, 1, out var operating), "hidden movement consumption refuses");
            c.Equal(Codes.NotFound, motion.ReadRoute(id, null, null, out var route), "hidden route refuses before geometry evaluation");
            c.That(navigation == null && execution == null && arrival == null && operating == null && route == null, "all hidden movement out facts are null");
            c.Equal(Code.None, g.Observer.Feature<LogisticsView>().ReadDiagnostics(id, resource, out var actual), "explicit observer remains authorized to actual logistics");
            c.That(actual != null, "observer gets actual independent physical provider facts");
            if (machine) c.Equal(Code.None, g.Observer.Feature<MotionView>().ReadExecution(id, out _), "observer reads actual worker motion");
            else c.That(g.Observer.Feature<OperationsView>().TryGetOperation(id, new Code("process"), out _), "observer reads actual running processor");
            c.Equal(before, g.Hash(), "operation/motion/logistics visibility is pure");
            c.Equal(time, g.Clock.StepIndex, "views never drive the one clock");
        }
        private static void Checkpoint(Check c, GameFactory f, SimGame g, EntityId processor, ResourceId resource, bool machine)
        {
            EntityId id = machine ? g.Seat(0).Snapshot.Entities.Single(x => x.Def == "machine").Id : processor;
            var retained = g.Seat(0).Snapshot; var ops = g.Seat(0).View.Feature<OperationsView>(); var motion = g.Seat(0).View.Feature<MotionView>();
            if (machine) c.Equal(Code.None, motion.ReadExecution(id, out _), "actual visible worker motion is observable");
            else c.That(ops.TryGetOperation(id, new Code("process"), out _), "actual visible processor operation is observable");
            c.Equal(Code.None, g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(id, resource, out var facts), "actual visible logistics is observable");
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = true });
            c.That(g.Seat(0).Snapshot.Removed.Contains(id), "same snapshot owner publishes loss-of-sight delta");
            g.Clock.Pause(); Hidden(c, g, id, resource, machine);
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "persisted sight and original live providers restore together");
            twin.Clock.Pause(); Hidden(c, twin, id, resource, machine);
            g.Update(); twin.Update(); c.Equal(g.Hash(), twin.Hash(), "equal paused pacing preserves hidden saved twin state");
            c.That(retained.Entities.Any(x => x.Id == id) && facts != null, "retained prior authorized facts stay detached");
            g.Clock.Resume(); PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = false });
            c.That(g.Seat(0).Snapshot.Spawned.Contains(id), "reveal reports same surviving visible identity");
            if (machine) c.Equal(Code.None, motion.ReadExecution(id, out _), "cached motion handle follows revealed visibility");
            else c.That(ops.TryGetOperation(id, new Code("process"), out _), "cached operation handle follows revealed visibility");
            c.Equal(Code.None, g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(id, resource, out _), "revealed logistics restores actual observation");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current shared operational visibility build");
            foreach (bool half in new[] { false, true }) foreach (bool cancel in new[] { false, true }) foreach (bool machine in new[] { false, true })
            {
                var resources = new DirectResourceContentModule(); var f = Factory(half, resources, machine ? "machine" : "destination");
                ResourceContentContractTests.Healthy(c, half, cancel, f, resources,
                    partialCheckpoint: (check, factory, game, target, resource) => Checkpoint(check, factory, game, target, resource, machine));
            }
        }
    }
}
