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
    // Authored input and public player actions/views only; learned/material/progress owners stay in the engine.
    internal static class TechnologyContentContractTests
    {
        internal const string Case = "Technology content contracts: independent research and unlocks use physical work, private objectives and saved replay";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(InitializeProbe init, out TechnologyContractModule proof,
            Action<JsonValue, JsonValue> edit = null)
        {
            var native = Json.Parse(FrameworkProofContent.Read("technology.json"));
            var external = Json.Parse(FrameworkProofContent.Read("technology-contracts.json"));
            native.Get("technologies")[1].Set("prerequisites", Json.Parse("[\"research.external\"]"));
            native.Get("technologies")[1].Get("nodes")[1].Set("requirement", JsonValue.Of("external.after"));
            external.Set("work_requirement_groups", Json.Parse("[{\"id\":\"external.after\",\"mode\":\"all\",\"entries\":[{\"id\":\"known\",\"requirement\":\"knowledge.external\"},{\"id\":\"inputs\",\"requirement\":\"preempt.ready\"}]}]"));
            edit?.Invoke(native, external);
            proof = new TechnologyContractModule(init);
            return ConstructionPlanTests.Points(additionalModule: new TechnologyModule(),
                moduleDeclaration: "{\"id\":\"sim.technology\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"technology.json\"]}",
                moduleDocument: native.ToString(), extraModules: new GameModule[] { new ObjectiveModule(), proof },
                extraDeclarations: "[{\"id\":\"sim.objectives\",\"version\":\"1.0.0\",\"documents\":[]},{\"id\":\"proof.technology-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.technology\",\"version\":\"1.0.0\"},{\"id\":\"sim.objectives\",\"version\":\"1.0.0\"}],\"documents\":[\"technology-contracts.json\"]}]",
                extraDocuments: new Dictionary<string, string> { ["technology-contracts.json"] = external.ToString() },
                scenarioEdit: scenario => {
                    foreach (string module in new[] { TechnologyModule.ModuleId, ObjectiveModule.ModuleId, TechnologyContractModule.ModuleId })
                        scenario.Get("modules").Add(JsonValue.Of(module));
                    scenario.Get("settings").Set(ObjectiveModule.ModuleId, Json.Parse("{\"companies\":[{\"company\":0,\"objectives\":[\"external.earned\"]},{\"company\":1,\"objectives\":[\"external.earned\"]}]}"));
                }, worldEdit: world => {
                    StartupContentAuthoring.Position(world, "machine", JsonValue.Number("4"));
                    world.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number("40"));
                });
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "points", Seed = 853, Seats = { Controller.Human, Controller.Human } });
        private static EntityView Entity(SimGame game, string def) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == def);
        private static List<TechnologySnapshot> Facts(SimGame game, int seat = 0)
        { var values = new List<TechnologySnapshot>(); game.Seat(seat).View.Feature<TechnologyView>().CopyTechnologies(values); return values; }
        private static bool Known(SimGame game, string id) => Facts(game).Single(t => t.Technology == id).Known;
        private static OrderView Current(SimGame game, int id)
        { var values = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(values); return values.Single(o => o.Id == id); }
        private static Fix Stock(SimGame game)
        { var values = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, "source").Id, values); return values.Single(f => f.Key == Storage.FacetStock && f.Subject == "supply").Value; }
        private static void Step(SimGame game, SimGame twin)
        {
            game.Update(); twin.Update();
            if (game.Fault != null || twin.Fault != null || game.Hash() != twin.Hash())
                throw new InvalidOperationException("technology contract Update " + game.Clock.StepIndex + ": " + game.Fault + "/" + twin.Fault);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> done)
        {
            int steps = 0; while (!done() && steps++ < 240) Step(game, twin);
            if (!done()) throw new InvalidOperationException("technology contract stalled at " + game.Clock.StepIndex + ": " +
                string.Join(";", Facts(game).Select(f => f.Technology + "/" + f.Known + "/" + f.ActiveOrder + "/" + f.Blocker)));
        }
        private static void Run(Check c)
        {
            var init = new InitializeProbe(); var factory = Factory(init, out var proof); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "contract technology semantics have current identity");
            c.Equal(154, EngineInfo.SaveFormat, "no learned/work/save layout changes");
            c.That(proof.IndependentTechnology && proof.IndependentUnlock && proof.NativeLinks, "actual independent contracts alias native prerequisites, requirements and objective predicate");
            c.Equal(1, init.Initializations, "valid contract composition initializes once");
            c.Equal(3, Facts(game).Count, "private technology view includes independent and native definitions");
            c.That(Facts(game).All(t => !t.Known), "definitions never pre-grant knowledge");
            c.Equal(Fix.FromInt(40), Stock(game), "explicit authored opening stock, not runtime grants");
            var view = game.Seat(0).View.Feature<TechnologyView>(); ulong hash = game.Hash();
            c.Equal(TechnologyCodes.MissingPrerequisite, view.PreviewResearch("research.foundation", Entity(game, "source").Id, 70), "native technology respects independent prerequisite");
            c.Equal(TechnologyCodes.Locked, view.PreviewResearch("point.tower", Entity(game, "source").Id, 70), "independent unlock gates exact actual activity");
            c.Equal(TechnologyCodes.Locked, game.Seat(0).View.Feature<ObjectiveView>().Observe("external.earned").Blocker, "native objective observes independent unlearned knowledge");
            c.Equal(hash, game.Hash(), "contract private previews/observations are pure");
            var recording = game.RecordReplay(new ReplayLimits(1024 * 1024, 1200));
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine").Id,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") },
                    null, true, true, WorkAssignmentMode.All) }).Accepted, "player enables ordinary machine work");
            var intent = new PlanIntent("activity:research.external", Json.Parse("{\"target\":" + Entity(game, "source").Id.Value + "}"), "research.contracts", 70, false);
            var posted = TestKit.Order(game, 0, new SubmitPlanCommand { Intent = intent });
            c.That(posted.Accepted, "independent technology uses existing persistent plan command"); long plan = posted.Arg0;
            var twin = factory.Load(game.Save());
            Until(game, twin, () => Facts(game).Single(t => t.Technology == "research.external").ActiveOrder > 0);
            int order = Facts(game).Single(t => t.Technology == "research.external").ActiveOrder;
            Until(game, twin, () => Current(game, order).CurrentProgress.WholeTicks > 0);
            c.That(((PointSnapshot)Entity(game, "machine").Spatial).Position != Fix.FromInt(4), "research machine travels physically from authored position");
            c.Equal(Fix.FromInt(32), Stock(game), "actual research consumes eight inputs through native requirements");
            c.That(!Known(game, "research.external"), "partial elapsed work never learns early");
            c.Equal(TechnologyCodes.Active, view.PreviewResearch("research.external", Entity(game, "destination").Id, 70), "independent active research remains company-exclusive");
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = plan, Group = "research.contracts", Priority = 70, Paused = true }).Accepted, "player pauses native plan");
            twin = factory.Load(game.Save()); WorkProgress progress = Current(game, order).CurrentProgress;
            for (int i = 0; i < 5; i++) Step(game, twin);
            c.Equal(progress, Current(game, order).CurrentProgress, "saved paused work earns no progress");
            c.That(!Known(game, "research.external"), "paused saved research stays unlearned");
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { plan }) }).Accepted, "player cancels actual acquired research");
            c.Equal(Fix.FromInt(32), Stock(game), "spent input is not refunded on cancellation");
            c.That(!Known(game, "research.external"), "cancelled research gives no unlock");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "cancelled graph/knowledge saves identically");
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "research.external", Target = Entity(game, "source").Id, Priority = 70 }).Accepted, "player retries independent graph through ordinary order");
            twin = factory.Load(game.Save()); var retained = Facts(game);
            Until(game, twin, () => Known(game, "research.external"));
            c.Equal(Fix.FromInt(24), Stock(game), "retried earned research consumes its own actual inputs once");
            c.That(retained.All(f => !f.Known), "retained private facts stay detached");
            c.That(Facts(game, 1).All(t => !t.Known), "learned independent knowledge never grants to another company");
            c.Equal(TechnologyCodes.Known, view.PreviewResearch("research.external", Entity(game, "source").Id, 70), "known technology cannot grant twice");
            c.That(game.Seat(0).View.Feature<ObjectiveView>().Observe("external.earned").Satisfied, "native objective recognizes actual earned independent technology");
            c.That(!game.Seat(1).View.Feature<ObjectiveView>().Observe("external.earned").Satisfied, "foreign objective remains unearned");
            c.Equal(Code.None, view.PreviewResearch("point.tower", Entity(game, "source").Id, 70), "independent actual unlock opens ordinary activity only after learning");
            foreach (string native in new[] { "research.foundation", "research.advanced" })
            {
                c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = native, Target = Entity(game, "source").Id, Priority = 70 }).Accepted, "mixed native/external prerequisite research admits " + native);
                twin = factory.Load(game.Save()); Until(game, twin, () => Known(game, native));
            }
            c.Equal(Fix.FromInt(8), Stock(game), "cancel/retry plus mixed chain conserves exact earned inputs");
            c.That(Facts(game).All(t => t.Known && t.ActiveOrder == 0 && t.CompletedTick.HasValue), "one learned ledger outlives completed work");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "learned contract chronology validates on restore");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), new ReplayLimits(1024 * 1024, 1200)).Success, "physical research/pause/cancel/unlock/native chain replay identically");
            c.That(Facts(New(factory)).All(t => !t.Known), "new interleaved session retains no learned globals");

            foreach (string invalid in new[] { "missing-prerequisite", "wrong-prerequisite", "cycle", "duplicate-prerequisite", "missing-unlock",
                "wrong-unlock", "duplicate-unlock", "missing-target", "no-final", "zero-time", "missing-array", "null-native-array", "missing-objective", "wrong-final", "replacement-node" })
            {
                var refused = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(refused, out _, (native, external) => {
                    var definition = external.Get("independent_technologies")[0];
                    if (invalid == "missing-prerequisite") definition.Set("prerequisites", Json.Parse("[\"missing\"]"));
                    if (invalid == "wrong-prerequisite") definition.Set("prerequisites", Json.Parse("[\"inspect\"]"));
                    if (invalid == "cycle") definition.Set("prerequisites", Json.Parse("[\"research.foundation\"]"));
                    if (invalid == "duplicate-prerequisite") definition.Set("prerequisites", Json.Parse("[\"research.foundation\",\"research.foundation\"]"));
                    if (invalid == "missing-unlock") definition.Set("unlocks", Json.Parse("[\"missing\"]"));
                    if (invalid == "wrong-unlock") definition.Set("unlocks", Json.Parse("[\"research.complete\"]"));
                    if (invalid == "duplicate-unlock") definition.Set("unlocks", Json.Parse("[\"unlock.external\",\"unlock.external\"]"));
                    if (invalid == "missing-target") external.Get("independent_activity_unlocks")[0].Set("activity", JsonValue.Of("missing"));
                    if (invalid == "no-final") definition.Remove("on_complete");
                    if (invalid == "wrong-final" || invalid == "replacement-node") {
                        external.Set("entity_replacement_effects", Json.Parse("[{\"id\":\"contract.replace\",\"source\":\"source\",\"target\":\"destination\"}]"));
                        if (invalid == "wrong-final") definition.Set("on_complete", JsonValue.Of("contract.replace"));
                        else definition.Get("nodes")[0].Set("effect", JsonValue.Of("contract.replace"));
                    }
                    if (invalid == "zero-time") definition.Get("nodes")[0].Get("duration").Set("numerator", JsonValue.Number("0"));
                    if (invalid == "missing-array") definition.Remove("unlocks");
                    if (invalid == "null-native-array") native.Get("technologies")[1].Set("prerequisites", JsonValue.Null());
                    if (invalid == "missing-objective") external.Get("technology_objective_predicates")[0].Set("technology", JsonValue.Of("missing"));
                })), "contract technology refuses " + invalid);
                c.Equal(0, refused.Initializations, invalid + " refuses before Initialize");
            }
        }
    }
}

