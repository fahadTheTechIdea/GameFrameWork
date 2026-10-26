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
    // Public commands/clock/Update/retained views only; probes deliberately inject faults, not gameplay.
    internal static class EntityStateObservationTests
    {
        private const string Case = "Entity state observation: authored physical transitions and codec-pure update sources";
        private static readonly Code A = new Code("a"), B = new Code("b"), Later = new Code("later"), Integrity = new Code("integrity");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static EntityId Factory(SimGame game) => Entity(game, "contract.factory");
        private static EntityId Next(SimGame game) => Entity(game, "state.later");
        private static EntityStateProbeDiagnostics Diagnostics(SimGame game) => game.Seat(0).View.Feature<EntityStateProbeDiagnostics>();
        private static EntityView Actor(SimGame game) => game.Seat(0).Snapshot.Entities.Single(e => e.Id == Factory(game));
        private static ConditionSnapshot Condition(SimGame game)
        {
            if (!game.Seat(0).View.Feature<EquipmentConditionView>().Read(Factory(game), Integrity, out var value).IsNone)
                throw new InvalidOperationException("missing actual condition");
            return value;
        }
        private static GameFactory Create(ConditionBindingModule conditionModule = null, bool active = false,
            Action<JsonValue> edit = null, Action<JsonValue> editEquipment = null)
            => ConditionBindingTests.Create(conditionModule ?? new ConditionBindingModule(),
                extraModule: new EntityStateObservationModule(),
                extraDeclaration: "{\"id\":\"proof.entity-state-observation\",\"version\":\"1.0.0\",\"requires\":[],\"documents\":[]}",
                editEquipment: equipment =>
                {
                    equipment.Get("entity_states").Add(Json.Parse("{\"id\":\"state.integrity-damaged\",\"state\":\"state.integrity-damaged\",\"priority\":250}"));
                    equipment.Get("entity_states").Add(Json.Parse("{\"id\":\"state.integrity-failed\",\"state\":\"state.integrity-failed\",\"priority\":280}"));
                    equipment.Get("entity_states").Add(Json.Parse("{\"id\":\"state.alpha\",\"state\":\"state.alpha\",\"priority\":250}"));
                    equipment.Get("entity_states").Add(Json.Parse("{\"id\":\"state.zeta\",\"state\":\"state.zeta\",\"priority\":250}"));
                    equipment.Get("entity_states").Add(Json.Parse("{\"id\":\"state.low\",\"state\":\"state.low\",\"priority\":-100}"));
                    editEquipment?.Invoke(equipment);
                }, edit: setup =>
                {
                    var parts = setup.Get("entities")[1].Get("components");
                    var condition = parts.Get("independent_condition"); condition.Set("wear", JsonValue.Null());
                    condition.Get("states").Set("damaged", JsonValue.Of("state.integrity-damaged"));
                    condition.Get("states").Set("failed", JsonValue.Of("state.integrity-failed"));
                    var probes = Json.Parse("[{\"id\":\"a\",\"mode\":\"healthy\",\"active\":false,\"state\":\"state.low\"},{\"id\":\"b\",\"mode\":\"healthy\",\"active\":false,\"state\":\"state.low\"}]");
                    for (int i = 0; i < probes.Count; i++) probes[i].Set("active", JsonValue.Of(active));
                    parts.Set("entity_probe", probes);
                    setup.Get("entities").Add(Json.Parse("{\"id\":\"state.later\",\"kind\":\"entity\",\"components\":{\"entity_probe\":{\"id\":\"later\",\"mode\":\"healthy\",\"active\":false,\"state\":\"state.low\"}}}"));
                    setup.Get("entity_setups").Add(Json.Parse("{\"id\":\"state.later\",\"after\":[\"contract.factory\"],\"entity\":\"state.later\",\"company\":0,\"placement\":null,\"home\":null}"));
                    setup.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("state.later"));
                    edit?.Invoke(setup);
                });
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current state observation build");
            c.Equal(154, EngineInfo.SaveFormat, "same entity state/chronology codec");
            c.Equal(0, typeof(EntityUpdateContext).GetConstructors().Length, "game issues contexts, no public constructor");
            Physical(c); Priorities(c); SourceFaults(c); NativeFaults(c); Contexts(c);
        }
        private static void Physical(Check c)
        {
            var factory = Create(); var game = ConstructionPlanTests.NewPoints(factory); var id = Factory(game); var diagnostic = Diagnostics(game);
            var retained = game.Seat(0).Snapshot; var initial = Actor(game); var untouched = ConstructionPlanTests.NewPoints(factory); ulong isolated = untouched.Hash();
            c.That(initial.State != Code.None && initial.StateSince == 0, "each object begins in actual authored default state");
            c.That(game.Seat(1).Snapshot.Entities.All(e => e.Id != id), "private entity state absent from another company snapshot");
            ulong hash = game.Hash(); for (int i = 0; i < 5; i++) { _ = game.Snapshot; _ = game.Seat(0).Snapshot; _ = Actor(game); }
            c.That(diagnostic.Reads(id, A) == 0 && game.Hash() == hash, "retained views never invoke Update/GetState");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                Enable(c, game);
                long started = game.Clock.StepIndex;
                Order(c, game, new PostOrderCommand { Target = id, Plan = "equipment.damage-work" });
                c.That(Condition(game).Value == Fix.FromInt(10), "posted work neither changes condition nor state instantly");
                Until(game, null, () => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "machine").State == new Code("point.travel"), "native machine travel state");
                var moving = game.Seat(0).Snapshot; var twin = factory.Load(game.Save()); var twinDiagnostic = Diagnostics(twin);
                c.Equal(0, twinDiagnostic.Reads(id, A), "read counters/context are transient, not saved chronology");
                c.That(twinDiagnostic.LastContext(id, A).Equals(default(EntityUpdateContext)), "load cannot fabricate old update permission");
                c.Equal(game.Hash(), twin.Hash(), "saved in-travel entity states retain exact native payload");
                long at = game.Clock.StepIndex; int reads = diagnostic.Reads(id, A); hash = game.Hash();
                game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.That(game.Clock.StepIndex == at && game.Hash() == hash && diagnostic.Reads(id, A) == reads, "sole clock pause prevents all entity source updates");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, twin, () => Condition(game).Value == Fix.FromInt(4), "travel and twelve-minute physical stress");
                var damaged = Actor(game);
                c.That(game.Clock.StepIndex >= started + 12 && damaged.State == new Code("state.integrity-damaged") &&
                    damaged.StateSince == Condition(game).ChangedTick, "same Update settles authored damage with actual game timestamp");
                c.That(game.Seat(0).Snapshot.Changed.Any(e => e.Id == id), "engine snapshot delta publishes settled state");
                c.That(diagnostic.Reads(id, A) == game.Clock.StepIndex && diagnostic.Reads(id, B) == game.Clock.StepIndex &&
                    diagnostic.Reads(Next(game), Later) == game.Clock.StepIndex, "every source visited exactly once per completed engine update");
                c.That(ReferenceEquals(diagnostic.LastContext(id, A).Clock, game.Clock), "entity observes the game's sole clock");
                long since = damaged.StateSince; for (int i = 0; i < 3; i++) Step(game, twin);
                c.Equal(since, Actor(game).StateSince, "unchanged state never resets state-since");
                Order(c, game, new PostOrderCommand { Target = id, Plan = "equipment.service" });
                c.Equal(Fix.FromInt(4), Condition(game).Value, "repair posting is not instantaneous");
                var repairTwin = factory.Load(game.Save()); long repairAt = game.Clock.StepIndex;
                Until(game, repairTwin, () => Condition(game).Value == Fix.FromInt(10), "physical spare hauling and thirty-minute repair");
                c.That(game.Clock.StepIndex >= repairAt + 30 && Actor(game).State != damaged.State, "positive physical repair clears damage in its own completed Update");
                Until(game, repairTwin, () => Actor(game).State == initial.State, "next-update restart releases remaining stopped capability");
                c.That(Stock(game, "source") + Stock(game, "contract.factory") + Stock(game, "machine") == Fix.FromInt(17),
                    "state settlement never duplicates physical spares or reusable tools");
                c.That(retained.Entities.Single(e => e.Id == id).State == initial.State &&
                    moving.Entities.Single(e => e.Def == "machine").State == new Code("point.travel"), "retained state snapshots remain detached");
                c.Equal(isolated, untouched.Hash(), "interleaved session does not share entity state/permission");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "public timed work/repair and state transitions replay");
            }
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled entity state save identity");
        }
        private static void Priorities(Check c)
        {
            foreach (bool reverse in new[] { false, true })
            {
                var game = ConstructionPlanTests.NewPoints(Create(edit: setup =>
                {
                    var parts = setup.Get("entities")[1].Get("components"); var original = parts.Get("independent_condition");
                    original.Set("initial", JsonValue.Number("9")); original.Get("states").Set("damaged", JsonValue.Of("state.zeta"));
                    var other = original.Clone(); other.Set("id", JsonValue.Of("backup.integrity")); other.Get("states").Set("damaged", JsonValue.Of("state.alpha"));
                    var rows = JsonValue.NewArray(); rows.Add(reverse ? other : original); rows.Add(reverse ? original : other); parts.Set("independent_condition", rows);
                }));
                Step(game); c.Equal(new Code("state.alpha"), Actor(game).State, "actual catalog-ID tie-break independent of callback/declaration ordering");
                long since = Actor(game).StateSince; Step(game);
                c.That(Actor(game).State == new Code("state.alpha") && Actor(game).StateSince == since, "same selected state preserves chronology");
            }
            var failed = ConstructionPlanTests.NewPoints(Create(edit: setup => setup.Get("entities")[1].Get("components").Get("independent_condition").Set("initial", JsonValue.Number("0"))));
            Step(failed); c.Equal(new Code("state.integrity-failed"), Actor(failed).State, "failed equipment selects authored high-priority state through normal Update");
        }
        private static void Cutoff(Check c, SimGame game, string phase, int expectedFirst = 1, int expectedSecond = 0)
        {
            var diagnostic = Diagnostics(game); var id = Factory(game); var initial = Actor(game); var retained = game.Seat(0).Snapshot;
            long at = game.Clock.StepIndex; game.Clock.TicksPerFrame = 8;
            c.Equal(0, game.Update(), "faulted source completes no update");
            c.That(game.Fault?.Phase == phase && game.Fault.Tick == at, "original source/recursive fault coordinate retained");
            c.That(game.Clock.StepIndex == at && game.Clock.PendingTicks == 0, "one clock not completed; host backlog cancelled");
            c.Equal(expectedFirst, diagnostic.Reads(id, A), "actual first probe follows original source order/cutoff");
            c.Equal(expectedSecond, diagnostic.Reads(id, B), "later same-entity source cutoff at offending provider");
            c.Equal(0, diagnostic.Reads(Next(game), Later), "later entity cannot update after source failure");
            c.That(game.State == GameState.Faulted && game.Seat(0).Snapshot.State == GameState.Faulted &&
                retained.State == GameState.Running, "faulted engine publication leaves retained snapshot detached");
            c.That(Actor(game).State == initial.State && Actor(game).StateSince == initial.StateSince, "no selected state commits after fault");
            c.Throws<InvalidOperationException>(() => game.Hash(), "no partial state hash");
            c.Throws<InvalidOperationException>(() => game.Save(), "no partial state checkpoint");
            var fault = game.Fault; c.Equal(0, game.Update(), "no automatic callback retry");
            c.That(ReferenceEquals(fault, game.Fault) && diagnostic.Reads(id, A) == expectedFirst, "first fault retained without further callbacks");
        }
        private static void SourceFaults(Check c)
        {
            foreach (EntityStateProbeFault mode in Enum.GetValues(typeof(EntityStateProbeFault)))
            {
                var factory = Create(active: true); var game = ConstructionPlanTests.NewPoints(factory); var id = Factory(game); var diagnostic = Diagnostics(game);
                var isolated = ConstructionPlanTests.NewPoints(factory); ulong hash = isolated.Hash(); diagnostic.Arm(id, A, mode);
                string phase = mode == EntityStateProbeFault.RecursiveEntity || mode == EntityStateProbeFault.SwallowEntity ? "entity.update:" + id.Value :
                    mode == EntityStateProbeFault.RecursiveGame || mode == EntityStateProbeFault.SwallowGame ? "game.update" : diagnostic.Phase(id, A);
                Cutoff(c, game, phase); c.Equal(hash, isolated.Hash(), "scoped state fault leaves another session healthy");
            }
            var foreign = ConstructionPlanTests.NewPoints(Create(active: true)); var target = ConstructionPlanTests.NewPoints(Create(active: true));
            Diagnostics(target).ArmForeign(Factory(target), A, Diagnostics(foreign));
            Cutoff(c, target, Diagnostics(target).Phase(Factory(target), A));
        }
        private static void NativeFaults(Check c)
        {
            foreach (OperationBindingFault mode in Enum.GetValues(typeof(OperationBindingFault)))
            {
                var module = new ConditionBindingModule(); var factory = Create(module, edit: setup =>
                {
                    var parts = setup.Get("entities")[1].Get("components");
                    parts.Get("independent_condition").Set("initial", JsonValue.Number("9"));
                    parts.Get("independent_condition").Set("wear", JsonValue.Of("wear.independent"));
                    parts.Get("observed_processor").Get("operation").Set("enabled", JsonValue.Of(false));
                });
                var game = ConstructionPlanTests.NewPoints(factory); module.Arm("condition.states", mode);
                Cutoff(c, game, Diagnostics(game).Phase(Factory(game), Integrity), expectedFirst: 0, expectedSecond: 0);
                c.Equal(1, module.Reads, "native authored state getter invoked only once");
            }
        }
        private static void Contexts(Check c)
        {
            var factory = Create(); var game = ConstructionPlanTests.NewPoints(factory); var diagnostic = Diagnostics(game); var id = Factory(game);
            Step(game); var context = diagnostic.LastContext(id, A); int reads = diagnostic.Reads(id, A); ulong hash = game.Hash();
            c.Throws<InvalidOperationException>(() => diagnostic.ProbeUpdate(id, default), "default context grants no permission");
            c.Throws<InvalidOperationException>(() => diagnostic.ProbeUpdate(id, context), "retained context grants no out-of-phase permission");
            var foreign = ConstructionPlanTests.NewPoints(factory);
            c.Throws<InvalidOperationException>(() => Diagnostics(foreign).ProbeUpdate(Factory(foreign), context), "same numeric entity ID in foreign session grants no permission");
            c.That(game.Hash() == hash && diagnostic.Reads(id, A) == reads && game.Fault == null && foreign.Fault == null,
                "invalid contexts execute no callbacks and invent no active-update fault");
        }
    }
}
