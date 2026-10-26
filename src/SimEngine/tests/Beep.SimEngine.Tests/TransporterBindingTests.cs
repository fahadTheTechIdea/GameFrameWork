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
using static Beep.SimEngine.Tests.HandlingContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Thin players author existing master-listed JSON and drive commands/Update/views/save only.
    internal static class TransporterBindingTests
    {
        internal const string Case = "Transporter bindings: authored carrier contracts and physical handling without native metadata";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Create(TransporterBindingModule module, bool independent = true,
            InitializeProbe initialize = null, Action<JsonValue> edit = null, int quantum = 60)
            => Factory(initialize ?? new InitializeProbe(), out _, rootEdit: root => {
                root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number(quantum.ToString()));
                var parts = root.Get("entities")[2].Get("components");
                if (independent) { var metadata = parts.Get("transporter"); parts.Remove("transporter"); parts.Set("independent_transporter", metadata); }
                edit?.Invoke(root);
            }, extraModule: module);
        private static HaulResourcesCommand Haul(SimGame game)
        {
            if (!game.Seat(0).View.TryFindResource("supply", out var supply)) throw new InvalidOperationException("missing supply");
            return new HaulResourcesCommand { Source = Entity(game, "source"), Destination = Entity(game, "destination"),
                Worker = Entity(game, "machine"), Resource = supply, Ceiling = Fix.FromInt(8), Priority = 20 };
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current transporter contract revision");
            c.Equal(154, EngineInfo.SaveFormat, "no carrier state or duplicate stock");
            c.That(typeof(Transporter).BaseType == typeof(Component), "native transporter consumes actual metadata contract");
            foreach (bool independent in new[] { false, true }) Physical(c, independent, 60);
            Physical(c, true, 30); Admission(c); Faults(c); Foreign(c);
        }
        private static void Physical(Check c, bool independent, int quantum)
        {
            var module = new TransporterBindingModule(); var initialize = new InitializeProbe();
            var factory = Create(module, independent, initialize, quantum: quantum); var game = ConstructionPlanTests.NewPoints(factory);
            c.Equal(independent, module.NativeBinding, "actual independent metadata binds original transporter");
            c.Equal(1, initialize.Initializations, "one Initialize after complete metadata validation");
            var isolated = ConstructionPlanTests.NewPoints(factory); ulong isolatedHash = isolated.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                Policy(c, game, true); int id = checked((int)Order(c, game, Haul(game)).Arg0);
                c.Equal(Fix.Zero, Stock(game, "machine"), "intent never creates cargo");
                Until(c, game, null, () => Stock(game, "machine").IsPositive, "independent carrier physical approach and first loading prefix");
                var loading = Job(game, id);
                c.Equal(12L * 60 / quantum, loading.HandlingRequiredTicks.Value, "same authored profile/selected sole time");
                c.That(Stock(game, "machine") < Fix.FromInt(8) && loading.Loaded == Stock(game, "machine"), "actual partial loading, no duplicate quantity");
                c.Equal(Fix.FromInt(20), Position(game), "real source arrival before loading");
                c.Equal(0, Jobs(game, 1).Count, "carrier handling facts company-private");
                var twin = factory.Load(game.Save()); long at = game.Clock.StepIndex; ulong hash = game.Hash();
                game.Clock.Pause(); twin.Clock.Pause(); Step(c, game, twin);
                c.That(game.Clock.StepIndex == at && game.Hash() == hash, "single game clock pause retains receipts/cargo/progress");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(c, game, twin, () => Stock(game, "machine") == Fix.FromInt(8), "actual loading completion");
                c.That(Stock(game, "source") == Fix.FromInt(12) && Stock(game, "destination") == Fix.Zero, "loaded cargo not teleported to destination");
                Until(c, game, twin, () => Stock(game, "destination").IsPositive, "physical travel and unloading prefix");
                var unloading = Job(game, id);
                c.Equal(8L * 60 / quantum, unloading.HandlingRequiredTicks.Value, "actual authored unloading duration");
                c.Equal(Fix.FromInt(10), Position(game), "delivery requires real destination arrival");
                var unloadingTwin = factory.Load(game.Save());
                Until(c, game, unloadingTwin, () => Stock(game, "destination") == Fix.FromInt(8) && Stock(game, "machine") == Fix.Zero, "complete timed delivery");
                c.That(Stock(game, "source") == Fix.FromInt(12) && loading.Loaded < Fix.FromInt(8), "conservation and retained loading facts");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "same original carrier/cargo/job codec continuation");
                c.Equal(isolatedHash, isolated.Hash(), "interleaved transporter state/inventory isolation");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary physical haul replays");
            }
        }
        private static void Admission(Check c)
        {
            foreach (bool independent in new[] { false, true })
                foreach (string bad in new[] { "missing", "null", "empty", "unknown", "wrong-kind", "inventory", "presence", "unused" })
                {
                    var initialize = new InitializeProbe();
                    c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Create(new TransporterBindingModule(), independent, initialize, root => {
                        var entity = root.Get("entities")[2];
                        if (bad == "unused") { entity = entity.Clone(); entity.Set("id", JsonValue.Of("carrier.unused")); root.Get("entities").Add(entity); }
                        var parts = entity.Get("components"); var metadata = parts.Get(independent ? "independent_transporter" : "transporter");
                        if (bad == "missing") metadata.Remove("handling");
                        if (bad == "null") metadata.Set("handling", JsonValue.Null());
                        if (bad == "empty") metadata.Set("handling", JsonValue.Of(""));
                        if (bad == "unknown" || bad == "unused") metadata.Set("handling", JsonValue.Of("missing"));
                        if (bad == "wrong-kind") metadata.Set("handling", JsonValue.Of("supply"));
                        if (bad == "inventory") parts.Remove("storage");
                        if (bad == "presence") parts.Remove("point_presence");
                    })), "invalid transporter content " + independent + "/" + bad);
                    c.Equal(0, initialize.Initializations, "refusal before Initialize");
                }
            foreach (string bad in new[] { "metadata-null", "undeclared", "non-transporter", "unused-null" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Create(new TransporterBindingModule(), initialize: initialize, edit: root => {
                    var entity = root.Get("entities")[2];
                    if (bad == "unused-null") { entity = entity.Clone(); entity.Set("id", JsonValue.Of("carrier.unused")); root.Get("entities").Add(entity); }
                    var parts = entity.Get("components");
                    if (bad == "metadata-null" || bad == "unused-null") parts.Get("independent_transporter").Set("proof_null", JsonValue.Of(true));
                    else { parts.Remove("independent_transporter"); parts.Set(bad == "undeclared" ? "undeclared_transporter" : "non_transporter",
                        bad == "undeclared" ? Json.Parse("{}") : Json.Parse("{\"handling\":\"contract.handling\"}")); }
                })), "invalid actual runtime/metadata pairing " + bad);
                c.Equal(0, initialize.Initializations, "unused and lying transporter metadata cannot initialize");
            }
            var valid = ConstructionPlanTests.NewPoints(Create(new TransporterBindingModule(), edit: root => {
                var entity = root.Get("entities")[2].Clone(); entity.Set("id", JsonValue.Of("carrier.stationary"));
                entity.Get("components").Remove("worker"); entity.Get("components").Remove("work_policy"); entity.Get("components").Remove("point_motion");
                root.Get("entities").Add(entity);
            }));
            c.That(valid.Fault == null, "transporter contract itself does not invent mandatory worker/motion roles");
        }
        private static void Faults(Check c)
        {
            foreach (TransporterBindingFault mode in Enum.GetValues(typeof(TransporterBindingFault)))
                foreach (bool update in new[] { false, true })
                {
                    var module = new TransporterBindingModule(); var factory = Create(module); var game = ConstructionPlanTests.NewPoints(factory);
                    Policy(c, game, true); Order(c, game, Haul(game));
                    Until(c, game, null, () => Stock(game, "machine").IsPositive, "fault probe actual loading stage");
                    long at = game.Clock.StepIndex; module.Arm(mode);
                    var published = new List<WorkJobView>();
                    if (update) c.Equal(0, game.Update(), "invalid handling metadata stops normal Update " + mode);
                    else c.Throws<InvalidOperationException>(() => game.Seat(0).View.Feature<WorkView>().CopyJobs(published), "actual metadata projection guarded " + mode);
                    c.That(published.Count == 0 && module.Reads == 1, "fault exposes no partial carrier job facts");
                    c.That(game.Fault?.Phase == "observation.provider" && game.Clock.StepIndex == at, "actual first fault stops sole clock completion");
                    c.Equal(0, game.Update(), "faulted carrier cannot resume"); c.Throws<InvalidOperationException>(() => game.Save(), "no faulted carrier checkpoint");
                    c.Throws<InvalidOperationException>(() => game.Hash(), "no healthy carrier hash after fault");
                }
        }
        private static void Foreign(Check c)
        {
            var foreignModule = new TransporterBindingModule(); ConstructionPlanTests.NewPoints(Create(foreignModule));
            var module = new TransporterBindingModule(); var game = ConstructionPlanTests.NewPoints(Create(module));
            Policy(c, game, true); var command = Haul(game); module.ArmForeign(foreignModule);
            c.Throws<ContentException>(() => game.Seat(0).Preview(command), "same named foreign-session handling definition is not a catalog alias");
            c.That(game.Fault?.Phase == "observation.provider", "foreign metadata cannot authorize receipts");
            c.Throws<InvalidOperationException>(() => game.Save(), "foreign handling fault cannot checkpoint");
        }
    }
}
