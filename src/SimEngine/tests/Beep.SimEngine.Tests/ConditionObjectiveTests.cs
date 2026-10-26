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
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class ConditionObjectiveTests
    {
        private static readonly Code Leak = new Code("pressure.leak");
        public static void Register() => TestRunner.Add("Objectives: shared private conditions and material-consuming incident recovery", Run);
        private static GameFactory Factory(string conditions = null, string objectives = null)
            => ConstructionPlanTests.Points(extraModules: new GameModule[] { new GameConditionProofModule(), new ObjectiveProofModule(), new InventorySignalsModule() },
                extraDeclarations: "[{\"id\":\"sim.conditions\",\"version\":\"1.0.0\",\"documents\":[\"objective-conditions.json\"]},{\"id\":\"sim.objectives\",\"version\":\"1.0.0\",\"documents\":[\"condition-objectives.json\"]},{\"id\":\"sim.inventory-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]}]",
                extraDocuments: new Dictionary<string, string> {
                    ["objective-conditions.json"] = conditions ?? FrameworkProofContent.Read("objective-conditions.json"),
                    ["condition-objectives.json"] = objectives ?? FrameworkProofContent.Read("condition-objectives.json") },
                additionalDocuments: new Dictionary<string, string> { ["objective-response.json"] = FrameworkProofContent.Read("objective-response.json") },
                scenarioEdit: scenario => {
                    foreach (string module in new[] { GameConditionsModule.ModuleId, ObjectiveModule.ModuleId, InventorySignalsModule.ModuleId, IncidentResponseModule.ModuleId })
                        scenario.Get("modules").Add(JsonValue.Of(module));
                    scenario.Get("settings").Set(ObjectiveModule.ModuleId, Content.Json.Parse("{\"companies\":[{\"company\":0,\"objectives\":[\"field.win\",\"field.repair\",\"field.detect\"]},{\"company\":1,\"objectives\":[\"field.detect\",\"field.win\",\"field.repair\"]}]}"));
                }, startupProfiles: new[] { "objective-conditions.start" });
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static List<ObjectiveSnapshot> Facts(SimGame game, int seat = 0)
        { var facts = new List<ObjectiveSnapshot>(); game.Seat(seat).View.Feature<ObjectiveView>().CopyObjectives(facts); return facts; }
        private static ObjectiveSnapshot Fact(SimGame game, string id) => Facts(game).Single(f => f.Objective == id);
        private static ObjectiveObservation Observe(SimGame game, string id, int seat = 0) => game.Seat(seat).View.Feature<ObjectiveView>().Observe(id);
        private static bool Active(SimGame game, string id = "field.plant", int seat = 0)
        {
            if (!game.Seat(seat).View.Feature<IncidentsView>().TryGetIncident(Find(game, id).Id, Leak, out var snapshot)) throw new InvalidOperationException("missing private incident");
            return snapshot.Active;
        }
        private static Order Response(SimGame game) => Board(game).ActiveResponseOn(Find(game, "field.plant").Id, Leak);
        private static Fix Stock(SimGame game, string id = "field.plant") => Find(game, id).Get<IInventory>().Amount(Supply(game));
        private static RaiseIncidentCommand Raise(SimGame game, string id = "field.plant") => new RaiseIncidentCommand { Target = Find(game, id).Id, Source = Leak, Kind = Leak };
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 400 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " fault=" + game.Fault + " outcome=" + game.Outcome +
                " objectives=" + string.Join(";", Facts(game).Select(f => f.Objective + "/" + f.State + "/" + f.Blocker)) +
                " orders=" + string.Join(";", Board(game).Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current shared condition semantics");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "explicit breaking content/view identity");
            Physical(c); Unavailable(c); InvalidContent(c); ObservationFaults(c);
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            c.That(!game.World.Services.Has<IPlayerControllers>() && !game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IBusinessAccounts>(), "physical objective needs neither autopilot nor business");
            IGameCondition shared = game.World.Content.Get<GameConditionDef>("field.recovered");
            c.That(ReferenceEquals(shared, game.World.Content.Get<ConditionObjectivePredicateDef>("field.recovered-predicate").Condition), "predicate aliases exact catalog condition");
            c.That(((GameConditionDef)shared).AuthoringPath.Contains("objective-conditions.json") && game.World.Content.Get<ObjectiveDef>("field.win").AuthoringPath.Contains("condition-objectives.json"), "separate master documents own content once");
            c.That(game.World.Content.Get<GameConditionDef>("field.elapsed") is ElapsedGameConditionDef, "independent public condition needs no scheduler/objective edit");
            var retained = Facts(game); ulong hash = game.Hash();
            for (int i = 0; i < 8; i++) {
                c.Equal(new Code("field.incident-state"), Observe(game, "field.detect").Blocker, "no incident waits instead of inventing detection");
                c.Equal(new Code("field.reserve-accounting"), Observe(game, "field.repair").Blocker, "healthy stock20 is not repaired stock12");
                c.Equal(new Code("condition.no-target"), Observe(game, "field.detect", 1).Blocker, "private company never borrows visible foreign prototype");
            }
            c.Equal(hash, game.Hash(), "repeated readiness observations create no identity reservations or progress");
            c.Throws<NotSupportedException>(() => ((IList<string>)retained.Single(f => f.Objective == "field.win").Prerequisites).Clear(), "snapshot prerequisites immutable");
            IObjectives objectives = game.World.Services.Get<IObjectives>();
            foreach (int invalid in new[] { -2, 8 }) { var rows = Facts(game); objectives.CopyObjectives(new PlayerId(invalid), rows); c.Equal(0, rows.Count, "invalid viewer clears without observer access"); }
            var observer = new List<ObjectiveSnapshot>(); game.Observer.Feature<ObjectiveView>().CopyObjectives(observer);
            c.Equal(6, observer.Count, "exact observer explicitly reads enrolled state");
            c.Equal(Codes.NotSeated, objectives.Observe(new PlayerId(-2), "field.detect").Blocker, "malformed observer cannot be objective beneficiary");
            c.That(TestKit.Order(game, 1, Raise(game, "field.foreign-plant")).Accepted, "rival uses same native incident command");
            c.That(Active(game, "field.foreign-plant", 1) && !Active(game), "foreign incident cannot change own plant");
            c.That(!Observe(game, "field.detect").Satisfied && !Observe(game, "field.detect", 1).Satisfied, "own conditions never borrow rival incident or stock");
            Configure(c, game);
            c.Equal(Code.None, game.Seat(0).Preview(Raise(game)), "normal incident preview admits current physical target");
            game.Seat(0).Submit(Raise(game)); SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "pending ordinary command saves without synthetic incident");
            Tick(game); Tick(twin);
            c.That(Active(game) && Fact(game, "field.detect").State == ObjectiveState.Completed, "actual command and native Update complete detection");
            c.Equal(ObjectiveState.Locked, Fact(game, "field.repair").State, "whole-pass capture prohibits same-Update dependency feedback");
            c.Equal(Fix.FromInt(20), Stock(game), "incident does not instantly consume or repair");
            c.Equal(game.Hash(), twin.Hash(), "pending command twin agrees");
            Reach(game, () => Response(game)?.Node("repair").Progress.WholeTicks > 0, "worker travels and starts repair", twin);
            c.That(Find(game, "machine").Get<PointPresence>().Position == Fix.FromInt(20) && Active(game), "actual machine reaches site but timed response remains active");
            c.Equal(Fix.FromInt(12), Stock(game), "real work consumes eight actual materials");
            c.Equal(ObjectiveState.Active, Fact(game, "field.repair").State, "reserve band alone cannot fake incident completion");
            c.Equal(new Code("field.incident-state"), Observe(game, "field.repair").Blocker, "same evaluator waits for actual response state");
            c.That(game.Outcome.IsNone && game.Hash() == twin.Hash(), "partial timed repair remains nonterminal and deterministic");
            twin = factory.Load(game.Save()); Reach(game, () => Fact(game, "field.repair").State == ObjectiveState.Completed, "native material-consuming response completion", twin);
            c.That(!Active(game) && Stock(game) == Fix.FromInt(12), "sole incident/work owners finish response without granting stock");
            c.Equal(ObjectiveState.Locked, Fact(game, "field.win").State, "victory waits next native pass");
            c.That(Observe(game, "field.win").Satisfied, "public elapsed and native signals share actual clock/private condition graph");
            Tick(game); Tick(twin);
            c.Equal(new Code("field.recovery-complete"), game.Outcome, "native once-only effect owns final outcome");
            c.That(Facts(game).All(f => f.State == ObjectiveState.Completed) && Facts(game, 1).All(f => f.State != ObjectiveState.Completed), "only own earned chain completes");
            c.Equal(Fix.FromInt(20), Stock(game, "field.foreign-plant"), "foreign materials are neither borrowed nor duplicated");
            c.That(Active(game, "field.foreign-plant", 1), "foreign unresolved incident not silently repaired");
            c.That(retained.All(f => f.State == ObjectiveState.Locked), "retained initial state never follows updates");
            c.Equal(game.Hash(), twin.Hash(), "partial-save twins finish identically");
            SimGame terminal = factory.Load(game.Save()); long ended = terminal.Tick; terminal.Update();
            c.Equal(ended, terminal.Tick, "native outcome halts game loop");
            c.Equal(game.Hash(), terminal.Hash(), "terminal state/effects checkpoint once");
            SimGame isolated = New(factory); c.That(!Active(isolated) && Stock(isolated) == Fix.FromInt(20) && isolated.Outcome.IsNone, "interleaved session has no copied conditions or state");
        }
        private static void Unavailable(Check c)
        {
            JsonValue conditions = Content.Json.Parse(FrameworkProofContent.Read("objective-conditions.json"));
            JsonValue gate = conditions.Get("signal_conditions")[1].Get("conditions")[1];
            gate.Set("signal", JsonValue.Of("field.stock-ready")); gate.Remove("minimum"); gate.Remove("maximum"); gate.Remove("unit"); gate.Set("expected", JsonValue.Of(true));
            JsonValue definitions = Content.Json.Parse(FrameworkProofContent.Read("condition-objectives.json"));
            definitions.Get("objectives")[1].Set("prerequisites", JsonValue.NewArray());
            GameFactory factory = Factory(conditions.ToString(), definitions.ToString()); SimGame game = New(factory);
            c.Equal(InventorySignal.NoThreshold, Observe(game, "field.repair").Blocker, "genuinely missing threshold stays typed unavailable");
            Tick(game); SimGame twin = factory.Load(game.Save());
            for (int i = 0; i < 7; i++) { Tick(game); Tick(twin); }
            c.That(Fact(game, "field.repair").State == ObjectiveState.Active && Fact(game, "field.repair").Blocker == InventorySignal.NoThreshold, "unavailable input waits rather than fails or substitutes false/zero");
            c.That(Stock(game) == Fix.FromInt(20) && game.Outcome.IsNone && game.Hash() == twin.Hash(), "unavailable saved continuation cannot consume or award");
            foreach (string mode in new[] { "all", "any" }) {
                JsonValue absent = Content.Json.Parse(FrameworkProofContent.Read("objective-conditions.json"));
                StartupContentAuthoring.Clear(absent, "objective-conditions.start");
                StartupContentAuthoring.Add(absent, "foreign", "field.foreign-plant", 1, Content.Json.Parse("{\"space\":\"field.points\",\"position\":50}"), "objective-conditions.start");
                absent.Get("signal_conditions")[0].Set("mode", JsonValue.Of(mode));
                SimGame noTarget = New(Factory(absent.ToString()));
                c.Equal(new Code("condition.no-target"), Observe(noTarget, "field.detect").Blocker, "no target is not vacuous success: " + mode);
            }
        }
        private static void InvalidContent(Check c)
        {
            foreach (string fault in new[] { "missing-condition", "mode", "empty", "source", "signal", "kind", "unit", "cycle" })
                c.Throws<ContentException>(() => {
                    JsonValue conditions = Content.Json.Parse(FrameworkProofContent.Read("objective-conditions.json"));
                    JsonValue definitions = Content.Json.Parse(FrameworkProofContent.Read("condition-objectives.json"));
                    JsonValue condition = conditions.Get("signal_conditions")[1], gate = condition.Get("conditions")[1];
                    if (fault == "missing-condition") definitions.Get("condition_objective_predicates")[0].Set("condition", JsonValue.Of("missing"));
                    if (fault == "mode") condition.Set("mode", JsonValue.Of("default"));
                    if (fault == "empty") condition.Set("conditions", JsonValue.NewArray());
                    if (fault == "source") gate.Set("source", JsonValue.Of("missing"));
                    if (fault == "signal") gate.Set("signal", JsonValue.Of("missing"));
                    if (fault == "kind") { gate.Set("signal", JsonValue.Of("field.stock-ready")); }
                    if (fault == "unit") gate.Set("unit", JsonValue.Of("unit.boolean"));
                    if (fault == "cycle") definitions.Get("objectives")[2].Get("prerequisites").Add(JsonValue.Of("field.win"));
                    New(Factory(conditions.ToString(), definitions.ToString()));
                }, "invalid actual authored graph refuses before Update: " + fault);
        }
        private static void ObservationFaults(Check c)
        {
            foreach (bool effect in new[] { false, true }) {
                JsonValue definitions = Content.Json.Parse(FrameworkProofContent.Read("condition-objectives.json"));
                definitions.Set(effect ? "proof_mutating_objective_effects" : "proof_mutating_objective_predicates", Content.Json.Parse("[{\"id\":\"proof.mutates\"}]"));
                JsonValue objective = definitions.Get("objectives")[2];
                if (effect) { objective.Set("predicate", JsonValue.Of("field.elapsed-predicate")); objective.Set("on_complete", JsonValue.Of("proof.mutates")); }
                else objective.Set("predicate", JsonValue.Of("proof.mutates"));
                SimGame game = New(Factory(objectives: definitions.ToString()));
                for (int i = 0; i < 8 && game.Fault == null; i++) game.Update();
                c.That(game.Fault != null && game.Outcome.IsNone, "clock-mutating " + (effect ? "effect Check" : "predicate") + " faults before terminal success");
                c.Throws<InvalidOperationException>(() => game.Save(), "failed observation cannot checkpoint as success");
                c.Throws<InvalidOperationException>(() => game.Hash(), "failed observation cannot hash as success");
            }
        }
    }
}
