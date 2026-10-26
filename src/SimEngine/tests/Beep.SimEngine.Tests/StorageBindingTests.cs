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
    // JSON intent, ordinary player commands/Update, immutable views and saves. No inventory rules.
    internal static class StorageBindingTests
    {
        internal const string Case = "Storage bindings: independent metadata retains one physical inventory and pure views";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Swap(JsonValue parts, bool independent)
        {
            if (!independent) return;
            var metadata = parts.Get("storage"); parts.Remove("storage"); parts.Set("independent_storage", metadata);
        }
        private static GameFactory Create(StorageBindingModule module, bool independent = true, InitializeProbe initialize = null,
            Action<JsonValue> edit = null, Action<JsonValue> worldEdit = null)
            => RecipeObservationTests.Create(new RecipeObservationModule(), management: true, initialize: initialize,
                extraModules: new[] { module },
                extraDeclarations: "[{\"id\":\"proof.storage-bindings\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]",
                edit: (setup, recipes) => {
                    Swap(setup.Get("entities")[1].Get("components"), independent);
                    string key = independent ? "independent_storage" : "storage";
                    setup.Get("entities").Add(Json.Parse("{\"id\":\"storage.stocked\",\"kind\":\"entity\",\"components\":{\"" + key + "\":{\"capacity_policy\":\"contract.source\",\"initial_stock\":{\"supply\":5},\"accepts\":[\"supply\"],\"phases\":[\"solid\"],\"public\":false,\"evacuate\":true}}}"));
                    setup.Get("entity_setups").Add(Json.Parse("{\"id\":\"storage.stocked\",\"after\":[],\"entity\":\"storage.stocked\",\"company\":0,\"placement\":null,\"home\":null}"));
                    setup.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("storage.stocked"));
                    edit?.Invoke(setup);
                },
                worldEdit: root => {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module.Id));
                    Swap(root.Get("entities")[2].Get("components"), independent); worldEdit?.Invoke(root);
                });
        private static InventoryView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<InventoryView>();
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current storage contract build");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "same native stock/promise codec");
            c.That(typeof(Storage).BaseType == typeof(Component) && typeof(IStorage).IsAssignableFrom(typeof(Storage)) &&
                typeof(IInletPort).IsAssignableFrom(typeof(IOutletPort)), "native inventory consumes paired contract and every outlet is inlet");
            foreach (bool independent in new[] { false, true }) Physical(c, independent);
            Admission(c); Faults(c);
        }
        private static void Physical(Check c, bool independent)
        {
            var module = new StorageBindingModule(); var initialize = new InitializeProbe(); var factory = Create(module, independent, initialize);
            var game = ConstructionPlanTests.NewPoints(factory);
            c.Equal(independent, module.NativeBinding, "actual independent cargo/factory metadata binds original Storage");
            c.Equal(1, initialize.Initializations, "complete bindings before one Initialize");
            c.Equal(Fix.FromInt(5), Stock(game, "storage.stocked"), "authored stock initialized exactly once through original transaction");
            c.That(game.Seat(0).View.TryFindResource("supply", out var supply), "actual native resource identity");
            ulong hash = game.Hash(); var id = Entity(game, "storage.stocked");
            c.That(View(game).TryGetCapacity(id, Code.None, out var retained) && retained.Usage.Single().Stock == Fix.FromInt(5), "actual detached capacity facts");
            c.That(View(game).StoreAccepts("storage.stocked", supply, Code.None), "same contract filter serves prototype preview");
            c.Throws<NotSupportedException>(() => ((IList<CapacityUsage>)retained.Usage).Clear(), "capacity usage immutable");
            c.That(!View(game, 1).TryGetCapacity(id, Code.None, out _) && !View(game, 1).TryGetReservations(id, Code.None, out var hidden) &&
                hidden.Count == 0, "private stock/capacity/receipts never leaked");
            c.Equal(hash, game.Hash(), "healthy inventory projections pure");
            var untouched = ConstructionPlanTests.NewPoints(factory); ulong isolated = untouched.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                Enable(c, game); Order(c, game, new EnqueueTransformationCommand { Transformer = Entity(game, "contract.factory"),
                    Capability = new Code("contract.process"), Transformation = "contract.make" });
                Until(game, null, () => Stock(game, "machine").IsPositive, "actual independent cargo load");
                c.That(View(game).TryGetReservations(Entity(game, "machine"), Code.None, out var reservations) && reservations.Count > 0,
                    "actual carrier receipt book projected, not duplicated");
                var twin = factory.Load(game.Save()); long at = game.Clock.StepIndex; hash = game.Hash();
                game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.That(game.Clock.StepIndex == at && game.Hash() == hash, "one clock freezes cargo and stock promises");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, twin, () => Process(game).QueueCount == 0 && game.Seat(0).Snapshot.Entities.Any(e => e.Def == "contract.result"),
                    "physical input/tool delivery and manufacturing using original stock owner");
                c.That(Stock(game, "source") == Fix.FromInt(16) && Stock(game, "machine") == Fix.Zero &&
                    Stock(game, "contract.factory") == Fix.One && Stock(game, "storage.stocked") == Fix.FromInt(5), "same physical input3/tool1 and untouched authored initial bill");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "save/load does not grant initial stock again");
                c.Equal(Fix.FromInt(5), retained.Usage.Single().Stock, "retained capacity observation frozen");
                c.Equal(isolated, untouched.Hash(), "interleaved sessions share no inventory/promise state");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary inventory-backed manufacturing replays");
            }
        }
        private static void Admission(Check c)
        {
            foreach (bool independent in new[] { false, true })
                foreach (string bad in new[] { "policy", "wrong-kind", "phase-empty", "phase-duplicate", "accepts-empty", "category-empty",
                    "accepts-duplicate", "unknown-resource", "initial-incompatible", "initial-overflow", "unused" })
                {
                    var initialize = new InitializeProbe();
                    c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Create(new StorageBindingModule(), independent, initialize, edit: setup => {
                        var entity = setup.Get("entities")[2];
                        if (bad == "unused") { entity = entity.Clone(); entity.Set("id", JsonValue.Of("storage.unused")); setup.Get("entities").Add(entity); }
                        var metadata = entity.Get("components").Get(independent ? "independent_storage" : "storage");
                        if (bad == "policy") metadata.Remove("capacity_policy");
                        if (bad == "wrong-kind") metadata.Set("capacity_policy", JsonValue.Of("supply"));
                        if (bad == "phase-empty") metadata.Set("phases", Json.Parse("[\"\"]"));
                        if (bad == "phase-duplicate") metadata.Set("phases", Json.Parse("[\"solid\",\"solid\"]"));
                        if (bad == "accepts-empty") metadata.Set("accepts", Json.Parse("[\" \"]"));
                        if (bad == "category-empty") metadata.Set("accepts", Json.Parse("[\"category:\"]"));
                        if (bad == "accepts-duplicate") metadata.Set("accepts", Json.Parse("[\"supply\",\"supply\"]"));
                        if (bad == "unknown-resource") metadata.Set("accepts", Json.Parse("[\"missing\"]"));
                        if (bad == "initial-incompatible") metadata.Set("phases", Json.Parse("[\"liquid\"]"));
                        if (bad == "initial-overflow" || bad == "unused") metadata.Set("initial_stock", Json.Parse("{\"supply\":41}"));
                    })), "storage authored declaration refuses " + independent + "/" + bad);
                    c.Equal(0, initialize.Initializations, "bad loaded/unused storage before Initialize");
                }
            foreach (string bad in new[] { "context", "capacity", "initial", "undeclared", "non-storage" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Create(new StorageBindingModule(), initialize: initialize, edit: setup => {
                    var parts = setup.Get("entities")[2].Get("components"); var metadata = parts.Get("independent_storage");
                    if (bad == "undeclared" || bad == "non-storage")
                    { parts.Remove("independent_storage"); parts.Set(bad == "undeclared" ? "undeclared_storage" : "non_storage", metadata); }
                    else metadata.Set("proof_fault", JsonValue.Of(bad));
                })), "actual configured inventory metadata required " + bad);
                c.Equal(0, initialize.Initializations, "invalid contract cannot initialize stock");
            }
        }
        private static void Faults(Check c)
        {
            foreach (StorageBindingFault mode in Enum.GetValues(typeof(StorageBindingFault)))
            {
                var module = new StorageBindingModule(); var game = ConstructionPlanTests.NewPoints(Create(module));
                long at = game.Clock.StepIndex; module.Arm(mode); CapacityCheck published = default;
                c.Throws<InvalidOperationException>(() => View(game).TryGetCapacity(Entity(game, "storage.stocked"), Code.None, out published), "actual stock codec guarded during metadata observation " + mode);
                c.That(published.Status == CapacityStatus.InvalidRequest && module.Reads == 1, "impure capacity publishes no facts");
                c.That(game.Fault?.Phase == "observation.provider" && game.Clock.StepIndex == at, "first stock observation fault retained");
                c.Equal(0, game.Update(), "faulted stock session cannot continue"); c.Throws<InvalidOperationException>(() => game.Hash(), "faulted stock has no healthy hash");
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted stock cannot checkpoint");
            }
        }
    }
}
