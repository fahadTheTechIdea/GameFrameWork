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
    // Authored files, public player commands and detached views only.
    internal static class ObjectiveContentContractTests
    {
        internal const string Case = "Objective content contracts: independent graphs and effects use physical construction, private lifecycle and saved replay";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(InitializeProbe init, out ObjectiveContractModule proof,
            Action<JsonValue, JsonValue> edit = null, Action<JsonValue> settingsEdit = null)
        {
            var native = Json.Parse(FrameworkProofContent.Read("objectives.json"));
            var external = Json.Parse(FrameworkProofContent.Read("objective-contracts.json"));
            var objectives = JsonValue.NewArray();
            objectives.Add(native.Get("objectives")[0]); objectives.Add(native.Get("objectives")[2]);
            native.Set("objectives", objectives); native.Remove("proof_gated_objective_effects");
            native.Get("logical_objective_predicates")[1].Set("predicates", Json.Parse("[\"objective.has-source\",\"objective.forward-tower\"]"));
            edit?.Invoke(native, external);
            proof = new ObjectiveContractModule(init);
            return ConstructionPlanTests.Points(additionalModule: proof,
                moduleDeclaration: "{\"id\":\"sim.objectives\",\"version\":\"1.0.0\",\"documents\":[\"objectives.json\",\"objective-contracts.json\"]}",
                moduleDocument: native.ToString(),
                extraDocuments: new Dictionary<string, string> { ["objective-contracts.json"] = external.ToString() },
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(ObjectiveModule.ModuleId));
                    var settings = Json.Parse("{\"companies\":[{\"company\":0,\"objectives\":[\"field.ready\",\"field.build\",\"field.win\"]},{\"company\":1,\"objectives\":[\"field.ready\",\"field.build\",\"field.win\"]}]}");
                    settingsEdit?.Invoke(settings); scenario.Get("settings").Set(ObjectiveModule.ModuleId, settings);
                }, worldEdit: world => StartupContentAuthoring.Position(world, "machine", JsonValue.Number("4")));
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static List<ObjectiveSnapshot> Facts(SimGame game, int seat = 0)
        { var rows = new List<ObjectiveSnapshot>(); game.Seat(seat).View.Feature<ObjectiveView>().CopyObjectives(rows); return rows; }
        private static ObjectiveSnapshot Fact(SimGame game, string id) => Facts(game).Single(f => f.Objective == id);
        private static EntityView Entity(SimGame game, string def) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == def);
        private static int Towers(SimGame game) => game.Seat(0).Snapshot.Entities.Count(e => e.Def == "point.tower");
        private static Fix Stock(SimGame game)
        { var facets = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, "source").Id, facets); return facets.Single(f => f.Key == Storage.FacetStock && f.Subject == "supply").Value; }
        private static int Notices(SimGame game)
        { long cursor = 0; var rows = new List<Notification>(); game.Seat(0).ReadNotifications(ref cursor, rows); return rows.Count(n => n.Kind == new Code("proof.objective-contract-effect")); }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin != null && (twin.Fault != null || game.Hash() != twin.Hash()))
                throw new InvalidOperationException("objective contracts step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            int steps = 0; while (!done() && steps++ < 480) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + ": " +
                string.Join(";", Facts(game).Select(f => f.Objective + "/" + f.State + "/" + f.Blocker)));
        }
        private static void Build(Check c, SimGame game, int position)
        {
            var arguments = Json.Parse("{\"placement\":{\"space\":\"field.points\",\"position\":" + position + "}}");
            c.That(TestKit.Order(game, 0, new SubmitPlanCommand { Intent = new PlanIntent("build:point.tower", arguments, "objective.contracts", 70, false) }).Accepted,
                "player submits ordinary physical construction at " + position);
        }
        private static void Run(Check c)
        {
            var init = new InitializeProbe(); var factory = Factory(init, out var proof); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "contract objectives use current protocol");
            c.Equal(154, EngineInfo.SaveFormat, "objective/work saved layout unchanged");
            c.That(proof.IndependentAliases && proof.NativeLinks, "independent definition/predicate/effect aliases link native graphs");
            c.Equal(1, init.Initializations, "valid composition initializes once");
            ulong hash = game.Hash(); var retained = Facts(game);
            c.Equal(3, retained.Count, "independent enrollment is visible through original private view");
            c.That(retained.All(f => f.State == ObjectiveState.Locked && f.Company == new PlayerId(0)), "authored definitions never grant completion");
            c.That(Facts(game, 1).All(f => f.Company == new PlayerId(1)), "views preserve beneficiary isolation");
            c.Equal(Fix.FromRaw(Fix.One.Raw / 2), game.Seat(0).View.Feature<ObjectiveView>().Observe("field.build").Progress, "native logical predicate evaluates independent child through captured graph");
            c.Equal(hash, game.Hash(), "contract observations are pure");
            c.Throws<NotSupportedException>(() => ((IList<string>)retained.Single(f => f.Objective == "field.win").Prerequisites).Add("fake"), "view prerequisites stay immutable");
            var recording = game.RecordReplay(new ReplayLimits(1024 * 1024, 1800));
            Step(game);
            c.Equal(ObjectiveState.Completed, Fact(game, "field.ready").State, "native root settles through actual readiness");
            c.Equal(ObjectiveState.Locked, Fact(game, "field.build").State, "independent prerequisite waits until later pass");
            Step(game);
            c.Equal(ObjectiveState.Active, Fact(game, "field.build").State, "independent entry activates in existing system");
            c.That(Fact(game, "field.build").ActivatedTick > Fact(game, "field.ready").SettledTick, "no same-pass dependency feedback");
            c.That(retained.All(f => f.State == ObjectiveState.Locked), "retained facts stay detached");
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine").Id,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) }).Accepted,
                "player enables native machine policy");
            Build(c, game, 12); var twin = factory.Load(game.Save());
            Until(game, twin, () => Towers(game) == 1, "first physical construction");
            c.That(((PointSnapshot)Entity(game, "machine").Spatial).Position != Fix.FromInt(4), "machine travels from authored position");
            c.Equal(Fix.FromInt(12), Stock(game), "real tower consumes eight hauled inputs");
            c.Equal(ObjectiveState.Completing, Fact(game, "field.build").State, "independent effect gates earned physical completion");
            c.Equal(ObjectiveCodes.Condition, Fact(game, "field.build").Blocker, "pending effect exposes its actual blocker");
            c.Equal(ObjectiveState.Locked, Fact(game, "field.win").State, "native prerequisite cannot borrow pending effect");
            c.Equal(0, Notices(game), "unready effect never applies");
            twin = factory.Load(game.Save()); var waiting = Fact(game, "field.build");
            for (int i = 0; i < 7; i++) Step(game, twin);
            c.Equal(ObjectiveState.Completing, Fact(game, "field.build").State, "saved independent effect wait survives Update");
            c.Equal(waiting.ActivatedTick, Fact(game, "field.build").ActivatedTick, "wait does not restart deadline");
            Build(c, game, 20); twin = factory.Load(game.Save());
            Until(game, twin, () => Fact(game, "field.build").State == ObjectiveState.Completed, "second physical structure admits effect");
            c.Equal(2, Towers(game), "no objective ghosts or instant construction");
            c.Equal(Fix.FromInt(4), Stock(game), "two actual builds conserve sixteen spent inputs");
            c.Equal(1, Notices(game), "independent effect applies exactly once");
            c.Equal(ObjectiveState.Locked, Fact(game, "field.win").State, "native final objective waits next pass");
            c.That(game.Outcome.IsNone, "host infers no victory");
            var settled = factory.Load(game.Save()); Step(settled);
            c.Equal(0, Notices(settled), "loaded completion does not reapply independent effect");
            Step(game, twin);
            c.Equal(new Code("proof.colony-established"), game.Outcome, "native final effect owns outcome");
            c.Equal(ObjectiveState.Completed, Fact(game, "field.win").State, "mixed chain settles in sole lifecycle owner");
            c.That(Facts(game, 1).All(f => f.State != ObjectiveState.Completed), "foreign company never earns owner's physical facts");
            var loaded = factory.Load(game.Save()); long step = loaded.Clock.StepIndex; loaded.Update();
            c.Equal(step, loaded.Clock.StepIndex, "terminal engine outcome halts Update");
            c.Equal(game.Hash(), loaded.Hash(), "terminal chronology loads identically");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), new ReplayLimits(1024 * 1024, 1800)).Success, "mixed graph/physical construction/pending effects replay identically");
            c.That(Facts(New(factory)).All(f => f.State == ObjectiveState.Locked), "new interleaved session shares no lifecycle globals");

            var deadlineFactory = Factory(new InitializeProbe(), out _, (native, external) =>
                external.Get("independent_objectives")[0].Set("deadline", Json.Parse("{\"numerator\":1,\"denominator\":4,\"unit\":\"hour\"}")));
            var deadlineGame = New(deadlineFactory); var deadlineTwin = deadlineFactory.Load(deadlineGame.Save());
            Until(deadlineGame, deadlineTwin, () => Fact(deadlineGame, "field.win").State == ObjectiveState.Failed, "independent deadline and native dependent failure");
            c.Equal(ObjectiveCodes.Deadline, Fact(deadlineGame, "field.build").Failure, "independent deadline uses sole game time");
            c.Equal(ObjectiveCodes.DependencyFailed, Fact(deadlineGame, "field.win").Failure, "native dependency failure follows actual independent failure");
            c.Equal(Fix.FromInt(20), Stock(deadlineGame), "objectives never create/consume physical stock");
            c.Equal(deadlineGame.Hash(), deadlineFactory.Load(deadlineGame.Save()).Hash(), "failed independent chronology validates on restore");

            foreach (string invalid in new[] { "missing-prerequisite", "wrong-prerequisite", "cycle", "duplicate-prerequisite", "missing-predicate", "wrong-predicate", "predicate-cycle", "missing-child", "wrong-child", "missing-effect", "wrong-effect", "missing-gate", "zero-deadline", "missing-array", "missing-deadline", "missing-effect-field", "missing-enrollment", "duplicate-enrollment" })
            {
                var refused = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(refused, out _, (native, external) => {
                    var definition = external.Get("independent_objectives")[0];
                    if (invalid == "missing-prerequisite") definition.Set("prerequisites", Json.Parse("[\"missing\"]"));
                    if (invalid == "wrong-prerequisite") definition.Set("prerequisites", Json.Parse("[\"objective.has-source\"]"));
                    if (invalid == "cycle") definition.Set("prerequisites", Json.Parse("[\"field.win\"]"));
                    if (invalid == "duplicate-prerequisite") definition.Set("prerequisites", Json.Parse("[\"field.ready\",\"field.ready\"]"));
                    if (invalid == "missing-predicate" || invalid == "wrong-predicate") definition.Set("predicate", JsonValue.Of(invalid == "missing-predicate" ? "missing" : "field.ready"));
                    if (invalid == "predicate-cycle" || invalid == "missing-child" || invalid == "wrong-child") external.Get("forward_objective_predicates")[0].Set("child", JsonValue.Of(invalid == "predicate-cycle" ? "objective.established" : invalid == "missing-child" ? "missing" : "field.ready"));
                    if (invalid == "missing-effect" || invalid == "wrong-effect") definition.Set("on_complete", JsonValue.Of(invalid == "missing-effect" ? "missing" : "objective.has-source"));
                    if (invalid == "missing-gate") external.Get("independent_objective_effects")[0].Set("gate", JsonValue.Of("missing"));
                    if (invalid == "zero-deadline") definition.Set("deadline", Json.Parse("{\"numerator\":0,\"denominator\":1,\"unit\":\"hour\"}"));
                    if (invalid == "missing-array") definition.Remove("prerequisites");
                    if (invalid == "missing-deadline") definition.Remove("deadline");
                    if (invalid == "missing-effect-field") definition.Remove("on_failure");
                }, settings => {
                    if (invalid == "missing-enrollment") settings.Get("companies")[0].Set("objectives", Json.Parse("[\"field.build\",\"field.win\"]"));
                    if (invalid == "duplicate-enrollment") settings.Get("companies")[0].Set("objectives", Json.Parse("[\"field.ready\",\"field.build\",\"field.build\",\"field.win\"]"));
                })), "objective contracts refuse " + invalid);
                c.Equal(0, refused.Initializations, invalid + " refuses before Initialize");
            }
        }
    }
}
