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
    // Authored intent, ordinary player commands/Update and immutable views, not consumption logic.
    internal static class OperatingInputsBindingTests
    {
        internal const string Case = "Operating inputs bindings: authored exposure contracts and physical phase consumption";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Create(OperatingInputsBindingModule module, bool independent = true, int stock = 20,
            InitializeProbe initialize = null, Action<JsonValue> edit = null)
            => RecipeObservationTests.Create(new RecipeObservationModule(), management: true, initialize: initialize,
                extraModules: new[] { module },
                extraDeclarations: "[{\"id\":\"proof.operating-inputs-bindings\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]",
                worldEdit: root => {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module.Id));
                    var parts = root.Get("entities")[2].Get("components"); parts.Remove("transporter");
                    parts.Get("storage").Set("capacity_policy", JsonValue.Of("fixture.count.40"));
                    parts.Get("storage").Set("initial_stock", Json.Parse(stock == 0 ? "{}" : "{\"supply\":" + stock + "}"));
                    var entries = JsonValue.NewArray();
                    foreach (var phase in new[] { "movement", "idle", "standby" })
                    {
                        var entry = Json.Parse(FrameworkProofContent.Read("motion-operating-inputs.json"));
                        entry.Set("phase", JsonValue.Of(phase)); entry.Set("id", JsonValue.Of(phase == "movement" ? "propulsion" : phase)); entries.Add(entry);
                    }
                    parts.Set(independent ? "independent_operating_inputs" : "operating_inputs", entries);
                    edit?.Invoke(root);
                });
        private static OperatingInputEvaluation Read(SimGame game, OperatingPhase phase = OperatingPhase.Movement)
        {
            OperatingInputEvaluation value; var id = Entity(game, "machine");
            Code code = phase == OperatingPhase.Movement
                ? game.Seat(0).View.Feature<MotionView>().ReadOperatingInputs(id, 1, out value)
                : game.Seat(0).View.Feature<WorkView>().ReadOperatingInputs(id, phase, out value);
            if (!code.IsNone) throw new InvalidOperationException("operating facts refused " + code);
            return value;
        }
        private static void Policy(Check c, SimGame game, bool enabled) => Order(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine"),
            Settings = new WorkPolicySettings(enabled, 0, new[] { new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 480 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " stock=" + Stock(game, "machine") +
                " state=" + game.Seat(0).Snapshot.Entities.Single(e => e.Def == "machine").State + " blocker=" + Read(game).Blocker);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current exposure binding build");
            c.Equal(154, EngineInfo.SaveFormat, "original exposure codec and stock formats");
            c.That(typeof(OperatingInputs).BaseType == typeof(Component) && typeof(IStatefulOperatingInputs).IsAssignableFrom(typeof(OperatingInputs)),
                "reusable exposure component consumes actual contract");
            foreach (bool independent in new[] { false, true }) Physical(c, independent);
            Shortage(c); Admission(c); Faults(c);
        }
        private static void Physical(Check c, bool independent)
        {
            var module = new OperatingInputsBindingModule(); var probe = new InitializeProbe(); var factory = Create(module, independent, initialize: probe);
            var game = ConstructionPlanTests.NewPoints(factory);
            c.That(module.NativeBinding == independent && probe.Initializations == 1, "actual metadata binds original exposure before Initialize");
            var retained = Read(game); ulong before = game.Hash();
            c.That(retained.Providers.Count == 1 && retained.Providers[0].Exposure.ElapsedTicks == 0 && retained.Providers[0].Exposure.UpdatedTick == -1,
                "one actual named phase owner, no fictitious elapsed time");
            c.Throws<NotSupportedException>(() => ((IList<OperatingUtilitySnapshot>)retained.Providers).Clear(), "detached provider list immutable");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<MotionView>().ReadOperatingInputs(Entity(game, "machine"), 1, out var hidden), "operating fuel facts company-private");
            c.That(hidden == null && before == game.Hash(), "healthy/refused observations change no stock/exposure/clock");
            var isolated = ConstructionPlanTests.NewPoints(factory); ulong isolatedHash = isolated.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                Policy(c, game, true); Order(c, game, new PostOrderCommand { Target = Entity(game, "destination"), Plan = "inspect" });
                Reach(game, null, () => Read(game).Providers[0].Exposure.ElapsedTicks >= 2, "physical fuel-paid travel");
                c.That(game.Seat(0).Snapshot.Entities.Single(e => e.Def == "machine").State == new Code("point.travel"), "Update publishes actual moving state");
                var moving = Read(game); var twin = factory.Load(game.Save()); before = game.Hash(); long at = game.Clock.StepIndex;
                game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.That(game.Clock.StepIndex == at && game.Hash() == before, "sole clock freezes paid travel and exposure");
                game.Clock.Resume(); twin.Clock.Resume();
                Reach(game, twin, () => game.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:inspect", Entity(game, "destination"), out _), "real approach and six-minute inspection");
                c.That(Read(game).Providers[0].Exposure.ElapsedTicks >= 10 && game.Clock.StepIndex >= at + 6, "distance and work take actual game time");
                Reach(game, twin, () => Read(game, OperatingPhase.Idle).Providers[0].Exposure.ElapsedTicks >= 2, "enabled idle exposure");
                long idle = Read(game, OperatingPhase.Idle).Providers[0].Exposure.ElapsedTicks;
                Policy(c, game, false); Policy(c, twin, false);
                Reach(game, twin, () => Read(game, OperatingPhase.Standby).Providers[0].Exposure.ElapsedTicks >= 2, "disabled-policy standby exposure");
                c.Equal(idle, Read(game, OperatingPhase.Idle).Providers[0].Exposure.ElapsedTicks, "standby does not advance another phase owner");
                game.Seat(0).View.TryFindResource("supply", out var supply);
                Fix consumed = Fix.Zero;
                foreach (OperatingPhase phase in Enum.GetValues(typeof(OperatingPhase))) consumed += Read(game, phase).Providers[0].Demand.Consumed.AmountOf(supply);
                c.Equal(Fix.FromInt(20) - consumed, Stock(game, "machine"), "original transactions exactly own aggregate phase consumption");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "three original exposure codecs restore with physical stock");
                c.That(retained.Providers[0].Exposure.ElapsedTicks == 0 && moving.Providers[0].Exposure.ElapsedTicks >= 2,
                    "retained initial and moving facts do not follow later Update");
                c.Equal(isolatedHash, isolated.Hash(), "interleaved sessions share no stock or exposure");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary phase policy and physical work replay");
            }
        }
        private static void Shortage(Check c)
        {
            var factory = Create(new OperatingInputsBindingModule(), stock: 0); var game = ConstructionPlanTests.NewPoints(factory);
            Policy(c, game, true); Order(c, game, new PostOrderCommand { Target = Entity(game, "destination"), Plan = "inspect" });
            for (int i = 0; i < 8; i++) Step(game);
            var value = Read(game);
            c.That(value.Blocker == OperatingInputCodes.Inputs && value.Providers[0].Exposure.ElapsedTicks == 0 && Stock(game, "machine") == Fix.Zero,
                "no fuel blocks actual travel without partial stock or exposure credit");
            c.That(!game.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:inspect", Entity(game, "destination"), out _), "fuel shortage never completes distant work");
            c.That(game.Seat(0).Snapshot.Entities.Single(e => e.Def == "machine").State == new Code("point.blocked"), "original entity Update resolves authored input-disabled state");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "physical fuel wait survives checkpoint");
        }
        private static void Admission(Check c)
        {
            foreach (bool independent in new[] { false, true })
                foreach (string bad in new[] { "id", "phase", "inputs", "states", "blank-id", "unknown-phase", "unknown-resource", "quantum", "period", "duplicate-rate", "unknown-state", "extra-state", "unused", "missing-motion", "missing-inventory" })
                {
                    var probe = new InitializeProbe();
                    c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Create(new OperatingInputsBindingModule(), independent, initialize: probe, edit: root => {
                        var entity = root.Get("entities")[2];
                        if (bad == "unused") { entity = entity.Clone(); entity.Set("id", JsonValue.Of("operating.unused")); root.Get("entities").Add(entity); }
                        var parts = entity.Get("components"); var row = parts.Get(independent ? "independent_operating_inputs" : "operating_inputs")[0];
                        if (bad == "id" || bad == "phase" || bad == "inputs" || bad == "states") row.Remove(bad);
                        if (bad == "blank-id") row.Set("id", JsonValue.Of(" "));
                        if (bad == "unknown-phase") row.Set("phase", JsonValue.Of("work"));
                        if (bad == "unknown-resource") row.Get("inputs")[0].Set("resource", JsonValue.Of("missing"));
                        if (bad == "quantum") row.Get("inputs")[0].Set("quantity", JsonValue.Number("0.5"));
                        if (bad == "period" || bad == "unused") row.Get("inputs")[0].Get("period").Set("denominator", JsonValue.Number("0"));
                        if (bad == "duplicate-rate") row.Get("inputs").Add(row.Get("inputs")[0].Clone());
                        if (bad == "unknown-state") row.Get("states").Set("disabled", JsonValue.Of("missing"));
                        if (bad == "extra-state") row.Get("states").Set("extra", JsonValue.Of("ready"));
                        if (bad == "missing-motion") parts.Remove("point_motion");
                        if (bad == "missing-inventory") parts.Remove("storage");
                    })), "operating declaration refuses " + independent + "/" + bad);
                    c.Equal(0, probe.Initializations, "invalid loaded declaration before Initialize");
                }
            foreach (string bad in new[] { "phase", "inputs", "states", "undeclared" })
            {
                var probe = new InitializeProbe();
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Create(new OperatingInputsBindingModule(), initialize: probe, edit: root => {
                    var parts = root.Get("entities")[2].Get("components"); var entries = parts.Get("independent_operating_inputs");
                    if (bad == "undeclared") { parts.Remove("independent_operating_inputs"); parts.Set("undeclared_operating_inputs", entries); }
                    else entries[0].Set("proof_fault", JsonValue.Of(bad));
                })), "actual reusable operating contract required " + bad);
                c.Equal(0, probe.Initializations, "invalid actual metadata cannot initialize exposure");
            }
        }
        private static void Faults(Check c)
        {
            foreach (string point in new[] { "inputs", "phase" })
                foreach (OperatingInputsBindingFault fault in Enum.GetValues(typeof(OperatingInputsBindingFault)))
                    foreach (bool utility in new[] { false, true })
                {
                    var module = new OperatingInputsBindingModule(); var game = ConstructionPlanTests.NewPoints(Create(module));
                    long at = game.Clock.StepIndex; module.Arm(point, fault); OperatingInputEvaluation published = null;
                    UtilityDemandSnapshot demand = default;
                    c.Throws<InvalidOperationException>(() => {
                        if (utility) game.Seat(0).View.Feature<OperationsView>().TryGetUtilityDemand(Entity(game, "machine"), new Code("propulsion"), out demand);
                        else game.Seat(0).View.Feature<MotionView>().ReadOperatingInputs(Entity(game, "machine"), 1, out published);
                    }, "actual exposure codec guarded " + point + "/" + fault + "/" + utility);
                    c.That(published == null && demand.Ports == null && module.Reads == 1 && game.Fault?.Phase == "observation.provider", "no facts and first actual provider fault retained");
                    c.That(game.Clock.StepIndex == at && game.Update() == 0, "faulted exposure cannot advance sole clock");
                    c.Throws<InvalidOperationException>(() => game.Hash(), "no healthy exposure hash after observation failure");
                    c.Throws<InvalidOperationException>(() => game.Save(), "faulted exposure cannot checkpoint");
                }
        }
    }
}
