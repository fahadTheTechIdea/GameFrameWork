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

namespace Beep.SimEngine.Tests
{
    // JSON input and ordinary commands/clock/Update/views/save/replay only; no processor logic here.
    internal static class TransformationContentContractTests
    {
        internal const string Case = "Transformation content contracts: authored independent recipe and physical manufacturing";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(InitializeProbe initialize, out TransformationContractModule recipes, Action<JsonValue> edit = null,
            Action<JsonValue> setupEdit = null, Action<JsonValue> worldEdit = null, GameModule extraModule = null)
        {
            recipes = new TransformationContractModule(); var capacity = new CapacityContractModule(initialize);
            var recipeDocument = Json.Parse(FrameworkProofContent.Read("transformations-contracts.json")); edit?.Invoke(recipeDocument);
            var setup = Json.Parse(FrameworkProofContent.Read("capacity-contracts.json"));
            setup.Set("transformations", JsonValue.NewArray()); // One author per selected recipe ID, not a runtime replacement.
            setupEdit?.Invoke(setup);
            var modules = new List<GameModule> { capacity, recipes }; if (extraModule != null) modules.Add(extraModule);
            var declarations = Json.Parse("[{\"id\":\"proof.capacity-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"capacity-contracts.json\"]},{\"id\":\"proof.transformation-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"}],\"documents\":[\"transformations-contracts.json\"]}]");
            if (extraModule != null) declarations.Add(Json.Parse("{\"id\":\"" + extraModule.Id + "\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
            return ConstructionPlanTests.Points(extraModules: modules,
                extraDeclarations: declarations.ToString(),
                extraDocuments: new Dictionary<string, string> { { "capacity-contracts.json", setup.ToString() }, { "transformations-contracts.json", recipeDocument.ToString() } },
                startupProfiles: new[] { "contract.capacity-start" },
                scenarioEdit: scenario => { scenario.Get("modules").Add(JsonValue.Of(capacity.Id)); scenario.Get("modules").Add(JsonValue.Of("proof.transformation-contracts")); if (extraModule != null) scenario.Get("modules").Add(JsonValue.Of(extraModule.Id)); },
                worldEdit: root => { root.Get("entities")[2].Get("components").Get("storage").Set("capacity_policy", JsonValue.Of("contract.carrier")); worldEdit?.Invoke(root); });
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        internal static EntityId Entity(SimGame game, string id) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == id).Id;
        internal static Fix Stock(SimGame game, string id)
        {
            var rows = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, id), rows);
            return rows.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        internal static TransformationSnapshot Process(SimGame game)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Entity(game, "contract.factory"), new Code("contract.process"), out var value))
                throw new InvalidOperationException("missing actual processor facts");
            return value;
        }
        internal static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("recipe step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        internal static void Until(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 480 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " phase=" + Process(game).Phase
                + " blocker=" + Process(game).Blocker + " source=" + Stock(game, "source") + " cargo=" + Stock(game, "machine"));
        }
        internal static void Order(Check c, SimGame game, Command command)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal player preview");
            c.Equal(before, game.Hash(), "preview pure"); c.That(TestKit.Order(game, 0, command).Accepted, "normal player command");
        }
        internal static void Enable(Check c, SimGame game) => Order(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine"),
            Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual recipe contract build"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current physical state schema authority");
            var initialize = new InitializeProbe(); var factory = Factory(initialize, out var recipes); var game = New(factory);
            c.That(recipes.ActualAliases && recipes.OriginalNativeBinding, "independent actual owned recipe resolves once and binds original native processor");
            c.Equal(1, initialize.Initializations, "complete metadata before one Initialize");
            Enable(c, game); var limits = new ReplayLimits(4 * 1024 * 1024, 3000); var recorder = game.RecordReplay(limits);
            Order(c, game, new EnqueueTransformationCommand { Transformer = Entity(game, "contract.factory"), Capability = new Code("contract.process"), Transformation = "contract.make" });
            c.That(game.Seat(0).Snapshot.Entities.All(e => e.Def != "contract.result"), "no instant entity output");
            Until(game, null, () => Process(game).Phase == TransformationPhase.Working, "physically supplied independent recipe WIP");
            var retained = Process(game); var twin = factory.Load(game.Save());
            c.That(retained.Transformation == "contract.make" && retained.Required == 30 && retained.QueueCount == 1, "queue views use actual authored ID and duration");
            c.That(!game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(Entity(game, "contract.factory"), new Code("contract.process"), out _), "same actual WIP private to company");
            long at = game.Clock.StepIndex; ulong hash = game.Hash(); game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(game.Clock.StepIndex == at && game.Hash() == hash, "sole clock pauses independent recipe progress");
            game.Clock.Resume(); twin.Clock.Resume();
            Until(game, twin, () => Process(game).QueueCount == 0 && game.Seat(0).Snapshot.Entities.Any(e => e.Def == "contract.result"), "native timed independent-recipe completion");
            c.That(Stock(game, "source") == Fix.FromInt(16) && Stock(game, "contract.factory") == Fix.One && Stock(game, "machine") == Fix.Zero, "physical supply4 consumes3 and returns1 reusable tool through original owners");
            c.That(game.Clock.StepIndex >= at + 29 && game.Seat(0).Snapshot.Entities.Count(e => e.Def == "contract.result") == 1, "time earns one and only one entity result");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed independent recipe save/load");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "normal player queue and clock input replay");
            c.That(retained.QueueCount == 1 && retained.Required == 30, "retained process facts unchanged");
            var fresh = New(factory); c.That(Stock(fresh, "source") == Fix.FromInt(20) && Process(fresh).QueueCount == 0, "interleaved session has independent stock/queue");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "mode", "tools", "duration", "zero", "cancel", "negative-cash", "tool-consumption", "ports", "unknown-resource", "missing-entity", "category", "extra", "unused", "invalid-provider", "duplicate" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(initialize, out _, root =>
                {
                    var row = root.Get("independent_transformations")[mode == "unused" ? 1 : 0];
                    if (mode == "mode" || mode == "tools" || mode == "duration") row.Remove(mode);
                    if (mode == "zero" || mode == "unused") row.Get("duration").Set("numerator", JsonValue.Number("0"));
                    if (mode == "cancel") row.Set("cancel_policy", JsonValue.Of("instant"));
                    if (mode == "negative-cash") row.Set("cash", JsonValue.Number("-1"));
                    if (mode == "tool-consumption") row.Set("tool_consumption", Json.Parse("{\"supply\":2}"));
                    if (mode == "ports") row.Set("input_ports", JsonValue.NewObject());
                    if (mode == "unknown-resource") row.Get("inputs").Set("missing", JsonValue.Number("1"));
                    if (mode == "missing-entity") row.Set("entity_output", JsonValue.Of("missing"));
                    if (mode == "category") row.Set("category", JsonValue.Of(" "));
                    if (mode == "extra") row.Set("host_duration", JsonValue.Number("1"));
                    if (mode == "invalid-provider") { var invalid = root.Get("independent_transformations")[1].Clone(); invalid.Set("id", JsonValue.Of("contract.invalid-unused")); root.Get("invalid_transformations").Add(invalid); }
                    if (mode == "duplicate") { var duplicate = row.Clone(); root.Set("transformations", JsonValue.NewArray()); root.Get("transformations").Add(duplicate); }
                })), "authored independent recipe refusal: " + mode);
                c.Equal(0, initialize.Initializations, "recipe refuses before Initialize: " + mode);
            }
        }
    }
}
