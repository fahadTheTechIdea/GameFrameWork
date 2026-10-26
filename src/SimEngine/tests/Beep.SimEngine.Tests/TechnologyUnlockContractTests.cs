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
    // Authored intent, normal player commands/clock and immutable progress/stock/knowledge only.
    internal static class TechnologyUnlockContractTests
    {
        internal const string Case = "Technology unlock contracts: research earns physical manufacturing";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static JsonValue Read(string name) => Json.Parse(FrameworkProofContent.Read(name));
        private static GameFactory Create(TechnologyUnlockContractModule module, bool independent = true,
            Action<JsonValue, JsonValue> edit = null)
        {
            var technology = Read("technology.json"); var foundation = technology.Get("technologies")[1].Clone();
            foundation.Set("unlocks", Json.Parse("[\"unlock.entity\",\"unlock.activity\",\"unlock.recipe\"]"));
            technology.Set("technologies", JsonValue.NewArray()); technology.Get("technologies").Add(foundation);
            var authored = JsonValue.NewObject();
            var entity = Json.Parse("[{\"id\":\"unlock.entity\",\"definition\":\"point.tower\"}]");
            var activity = Json.Parse("[{\"id\":\"unlock.activity\",\"definition\":\"inspect\"}]");
            var recipe = Json.Parse("[{\"id\":\"unlock.recipe\",\"definition\":\"contract.make\"}]");
            technology.Set("technology_entity_unlocks", independent ? JsonValue.NewArray() : entity);
            technology.Set("technology_activity_unlocks", independent ? JsonValue.NewArray() : activity);
            technology.Set("technology_transformation_unlocks", independent ? JsonValue.NewArray() : recipe);
            authored.Set("independent_entity_unlocks", independent ? entity : JsonValue.NewArray());
            authored.Set("context_activity_unlocks", independent ? activity : JsonValue.NewArray());
            authored.Set("independent_recipe_unlocks", independent ? recipe : JsonValue.NewArray());
            edit?.Invoke(technology, authored);
            var setup = Read("capacity-contracts.json"); setup.Set("transformations", JsonValue.NewArray());
            var capacity = new CapacityContractModule(new InitializeProbe());
            return ConstructionPlanTests.Points(additionalModule: new TechnologyModule(),
                moduleDeclaration: "{\"id\":\"sim.technology\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"technology.json\"]}",
                moduleDocument: technology.ToString(),
                extraModules: new GameModule[] { capacity, new TransformationContractModule(), module },
                extraDeclarations: "[{\"id\":\"proof.capacity-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"capacity-contracts.json\"]}," +
                    "{\"id\":\"proof.transformation-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"}],\"documents\":[\"transformations-contracts.json\"]}," +
                    "{\"id\":\"proof.unlock-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.technology\",\"version\":\"1.0.0\"},{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"}],\"documents\":[\"unlock-contracts.json\"]}]",
                extraDocuments: new Dictionary<string, string> {
                    ["capacity-contracts.json"] = setup.ToString(), ["transformations-contracts.json"] = FrameworkProofContent.Read("transformations-contracts.json"),
                    ["unlock-contracts.json"] = authored.ToString() },
                startupProfiles: new[] { "contract.capacity-start" },
                scenarioEdit: scenario => {
                    foreach (string id in new[] { TechnologyModule.ModuleId, capacity.Id, "proof.transformation-contracts", module.Id })
                        scenario.Get("modules").Add(JsonValue.Of(id));
                }, worldEdit: world => {
                    world.Get("entities")[2].Get("components").Get("storage").Set("capacity_policy", JsonValue.Of("contract.carrier"));
                    StartupContentAuthoring.Position(world, "machine", JsonValue.Number("4"));
                });
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static EnqueueTransformationCommand Queue(SimGame game) => new EnqueueTransformationCommand {
            Transformer = Entity(game, "contract.factory"), Capability = new Code("contract.process"), Transformation = "contract.make" };
        private static ConstructCommand Build() => new ConstructCommand {
            Def = "point.tower", Priority = 50, Placement = Json.Parse("{\"space\":\"field.points\",\"position\":12}") };
        private static bool Known(SimGame game, int seat = 0)
        {
            var facts = new List<TechnologySnapshot>(); game.Seat(seat).View.Feature<TechnologyView>().CopyTechnologies(facts);
            return facts.Single(f => f.Technology == "research.foundation").Known;
        }
        private static ActivityNodeView Research(SimGame game, int order)
        {
            var nodes = new List<ActivityNodeView>(); game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes);
            return nodes.Single(n => n.Id == "research");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual unlock contract build identity");
            c.Equal(154, EngineInfo.SaveFormat, "same work/learned/production state owners");
            foreach (bool independent in new[] { false, true }) Physical(c, independent);
            Invalid(c); AuthoredChange(c); Faults(c);
        }
        private static void Physical(Check c, bool independent)
        {
            var module = new TechnologyUnlockContractModule(); var factory = Create(module, independent);
            var game = New(factory); var idle = New(factory); ulong idleHash = idle.Hash();
            c.That(module.ActualAliases && module.DirectDefinitions && module.ExactTargets && module.Independent == independent,
                "native/independent exact typed definition aliases");
            c.That(module.Initialize.Initializations >= 1, "valid unique initialization probe actually ran");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "physical research/production does not require business");
            var view = game.Seat(0).View.Feature<TechnologyView>(); ulong before = game.Hash();
            c.Equal(TechnologyCodes.Locked, game.Seat(0).Preview(Build()), "exact entity unlock gates construction");
            c.Equal(TechnologyCodes.Locked, view.PreviewResearch("inspect", Entity(game, "source"), 20), "exact activity unlock gates work");
            c.Equal(Code.None, view.PreviewResearch("point.tower", Entity(game, "source"), 20), "entity homonym does not gate another activity kind");
            c.Equal(TechnologyCodes.Locked, game.Seat(0).Preview(Queue(game)), "exact recipe unlock gates real processor queue");
            c.Equal(TechnologyCodes.Locked, game.Seat(0).View.Feature<TransformationManagementView>().PreviewEntityOutput(
                Entity(game, "contract.factory"), new Code("contract.process"), "contract.make").Refusal, "output preview shares actual unlock gate");
            c.Equal(before, game.Hash(), "all typed admission reads are pure");
            c.Equal(TechnologyCodes.Locked, TestKit.Order(game, 0, Queue(game)).Refusal, "locked recipe command matches preview");
            c.That(Process(game).QueueCount == 0 && game.Seat(0).Snapshot.Entities.All(e => e.Def != "contract.result"),
                "locked manufacturing allocates no queue/product");
            Enable(c, game);
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recording = game.RecordReplay(limits))
            {
                var result = TestKit.Order(game, 0, new PostOrderCommand { Plan = "research.foundation", Target = Entity(game, "source"), Priority = 70 });
                c.That(result.Accepted, "player orders original native research graph");
                int id = checked((int)result.Arg0);
                Until(game, null, () => Research(game, id).Progress.WholeTicks > 0 && !Known(game), "partial physical research");
                var research = Research(game, id);
                c.That(research.Worker != EntityId.None && game.Seat(0).Snapshot.Entities.Any(e => e.Def == "machine" &&
                    ((PointSnapshot)e.Spatial).Position != Fix.FromInt(4)), "real research machine traveled and earned work");
                c.Equal(Fix.FromInt(12), Stock(game, "source"), "physical research spent exact8 of20 opening materials");
                c.Equal(TechnologyCodes.Locked, game.Seat(0).Preview(Queue(game)), "partial work never unlocks manufacturing");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial research uses original saved owners");
                long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.Equal(at, game.Clock.StepIndex, "sole clock pauses actual research");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, twin, () => Known(game), "actual earned research unlock");
                c.That(!Known(game, 1), "other company never gains private learned unlock");
                c.Equal(Code.None, game.Seat(0).Preview(Build()), "completed research opens exact entity");
                c.Equal(Code.None, view.PreviewResearch("inspect", Entity(game, "source"), 20), "completed research opens exact activity");
                c.Equal(Code.None, game.Seat(0).Preview(Queue(game)), "earned knowledge opens actual independent recipe");
                Order(c, game, Queue(game));
                c.That(game.Seat(0).Snapshot.Entities.All(e => e.Def != "contract.result"), "unlock/queue never yields instantly");
                Until(game, null, () => Process(game).Phase == TransformationPhase.Working, "physical material hauling and recipe WIP");
                var retained = Process(game); twin = factory.Load(game.Save());
                c.That(retained.Required == 30 && retained.QueueCount == 1, "original authored30-minute recipe, not research-owned production");
                c.That(!game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(
                    Entity(game, "contract.factory"), new Code("contract.process"), out _), "actual acquired WIP stays private");
                at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.Equal(at, game.Clock.StepIndex, "same sole clock pauses recipe");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, twin, () => Process(game).QueueCount == 0 && game.Seat(0).Snapshot.Entities.Any(e => e.Def == "contract.result"),
                    "earned real manufacturing output");
                c.That(game.Clock.StepIndex >= at + 29 && game.Seat(0).Snapshot.Entities.Count(e => e.Def == "contract.result") == 1,
                    "physical recipe earns exactly one output over time");
                c.That(Stock(game, "source") == Fix.FromInt(8) && Stock(game, "contract.factory") == Fix.One && Stock(game, "machine") == Fix.Zero,
                    "20 opening minus8 research minus3 production conserves9 stock and releases reusable tool");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "research/recipe completion saves original ledgers");
                c.That(retained.QueueCount == 1 && retained.Required == 30, "retained production facts remain detached");
                c.Equal(idleHash, idle.Hash(), "interleaved session retains separate locked knowledge/materials/queue");
                c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "same normal research/manufacturing stream replays");
                c.Equal(TechnologyCodes.Known, view.PreviewResearch("research.foundation", Entity(game, "source"), 70), "no repeated grant");
            }
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "unknown-entity", "wrong-entity-kind", "unknown-activity", "wrong-recipe-kind", "unknown-recipe",
                "missing-target", "unused-unknown-target", "duplicate-contract", "missing-unlock", "stateful-used", "stateful-unused", "unknown-key" })
            {
                var module = new TechnologyUnlockContractModule();
                c.Throws<ContentException>(() => New(Create(module, edit: (technology, authored) => {
                    var entity = authored.Get("independent_entity_unlocks")[0]; var recipe = authored.Get("independent_recipe_unlocks")[0];
                    if (mode == "unknown-entity") entity.Set("definition", JsonValue.Of("missing"));
                    if (mode == "wrong-entity-kind") entity.Set("definition", JsonValue.Of("inspect"));
                    if (mode == "unknown-activity") authored.Get("context_activity_unlocks")[0].Set("definition", JsonValue.Of("missing"));
                    if (mode == "wrong-recipe-kind") recipe.Set("definition", JsonValue.Of("inspect"));
                    if (mode == "unknown-recipe") recipe.Set("definition", JsonValue.Of("missing"));
                    if (mode == "missing-target") recipe.Remove("definition");
                    if (mode == "unused-unknown-target") authored.Get("independent_recipe_unlocks").Add(Json.Parse("{\"id\":\"unused.unlock\",\"definition\":\"missing\"}"));
                    if (mode == "duplicate-contract") entity.Set("id", JsonValue.Of("unlock.recipe"));
                    if (mode == "missing-unlock") technology.Get("technologies")[0].Set("unlocks", Json.Parse("[\"missing\"]"));
                    if (mode == "stateful-used" || mode == "stateful-unused")
                    {
                        string id = mode == "stateful-used" ? "unlock.entity" : "unused.stateful";
                        authored.Set("stateful_unlocks", Json.Parse("[{\"id\":\"" + id + "\"}]"));
                        if (mode == "stateful-used") authored.Set("independent_entity_unlocks", JsonValue.NewArray());
                    }
                    if (mode == "unknown-key") recipe.Set("legacy_target", JsonValue.Of("contract.make"));
                })), "invalid actual unlock refuses before Initialize: " + mode);
                c.Equal(0, module.Initialize?.Initializations ?? 0, "no Initialize for " + mode);
            }
        }
        private static void AuthoredChange(Check c)
        {
            var original = New(Create(new TechnologyUnlockContractModule()));
            var changed = New(Create(new TechnologyUnlockContractModule(), edit: (technology, authored) =>
                authored.Get("independent_recipe_unlocks")[0].Set("definition", JsonValue.Of("contract.unused-recipe"))));
            c.Equal(Code.None, changed.Seat(0).Preview(Queue(changed)), "new typed JSON target removes recipe from that research gate");
            c.Equal(TechnologyCodes.Locked, original.Seat(0).Preview(Queue(original)), "previous frozen exact target remains locked");
            c.That(!Known(changed), "changing authored target does not fabricate knowledge");
        }
        private static void Faults(Check c)
        {
            foreach (UnlockProbeFault fault in Enum.GetValues(typeof(UnlockProbeFault)))
            {
                var module = new TechnologyUnlockContractModule(); var game = New(Create(module));
                module.Arm(fault); long at = game.Clock.StepIndex; Code? answer = null;
                c.Throws<InvalidOperationException>(() => answer = game.Seat(0).Preview(Queue(game)), "actual matcher callback refuses " + fault);
                c.That(!answer.HasValue && module.Reads == 1 && game.Fault?.Phase == "observation.provider", "first actual matcher fault withholds result");
                c.That(module.MutationCompleted == (fault != UnlockProbeFault.Exception), "actual matcher mutation completed before guard");
                c.That(game.Update() == 0 && game.Clock.StepIndex == at, "faulted matcher cannot advance clock");
                c.Throws<InvalidOperationException>(() => game.Hash(), "no healthy matcher hash"); c.Throws<InvalidOperationException>(() => game.Save(), "no faulted matcher save");
            }
        }
    }
}
