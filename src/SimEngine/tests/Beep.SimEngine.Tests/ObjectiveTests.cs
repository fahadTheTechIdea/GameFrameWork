using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
    public static class ObjectiveTests
    {
        public static void Register()
        {
            TestRunner.Add("Objectives: authored graphs, bindings and private contracts", Contracts);
            TestRunner.Add("Objectives: physical chains, deadlines and saved continuation", Physical);
        }
        private static GameFactory Factory(string json = null, Action<JsonValue> scenarioEdit = null)
            => ConstructionPlanTests.Points(additionalModule: new ObjectiveProofModule(),
                moduleDeclaration: "{\"id\":\"sim.objectives\",\"version\":\"1.0.0\",\"documents\":[\"objectives.json\"]}",
                moduleDocument: json ?? FrameworkProofContent.Read("objectives.json"), scenarioEdit: scenario =>
                {
                    scenario.Get("modules").Add(JsonValue.Of(ObjectiveModule.ModuleId));
                    scenario.Get("settings").Set(ObjectiveModule.ModuleId, Content.Json.Parse("{\"companies\":[{\"company\":0,\"objectives\":[\"field.win\",\"field.ready\",\"field.build\"]},{\"company\":1,\"objectives\":[\"field.ready\",\"field.build\",\"field.win\"]}]}"));
                    scenarioEdit?.Invoke(scenario);
                });
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static List<ObjectiveSnapshot> Facts(SimGame game, int seat = 0)
        { var rows = new List<ObjectiveSnapshot>(); game.Seat(seat).View.Feature<ObjectiveView>().CopyObjectives(rows); return rows; }
        private static ObjectiveSnapshot Fact(SimGame game, string id) => Facts(game).Find(f => f.Objective == id);
        private static ObjectiveSystem Owner(SimGame game) => game.World.System<ObjectiveSystem>();
        private static int EffectNotices(SimGame game)
        {
            long cursor = 0; var notices = new List<Notification>();
            return game.World.Notifications.CopySince(ref cursor, notices, n => n.Kind == new Code("proof.objective-effect"));
        }
        private static int Towers(SimGame game)
        { int count = 0; foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == "point.tower") count++; return count; }
        private static long Build(Check c, SimGame game, int at)
        {
            JsonValue placement = Content.Json.Parse("{\"space\":\"field.points\",\"position\":" + at + "}");
            JsonValue arguments = JsonValue.NewObject(); arguments.Set("placement", placement);
            var result = TestKit.Order(game, 0, new SubmitPlanCommand { Intent = new PlanIntent("build:point.tower", arguments, "objective.build", 70, false) });
            c.That(result.Accepted, "player submits ordinary physically supplied build"); return result.Arg0;
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1200 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " tick=" + game.Tick + " outcome=" + game.Outcome + " objectives=" +
                string.Join(";", Facts(game).ConvertAll(f => f.Objective + "/" + f.State + "/" + f.Blocker + "/" + f.DeadlineTick)));
        }
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); IObjectives owner = game.World.Services.Get<IObjectives>();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "native objective behavior has current identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no old schema/reader");
            c.That(ReferenceEquals(owner, Owner(game)), "contract aliases actual system, not a persistent service clone");
            c.Equal(1, Owner(game).StateSchemaVersion, "sole objective state layout");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "native physical work codec unchanged");
            c.That(game.World.Content.Get<ObjectiveDef>("field.win").AuthoringPath.Contains("objectives.json"), "master document is authoritative");
            c.That(!game.World.Services.Has<ITechnologyKnowledge>(), "objectives require no compulsory research state");
            ulong hash = game.Hash(); var retained = Facts(game);
            c.That(retained.Count == 3 && retained.TrueForAll(f => f.Company == new PlayerId(0) && f.State == ObjectiveState.Locked && !f.ActivatedTick.HasValue), "explicit company enrollment starts only intent");
            c.That(Facts(game, 1).TrueForAll(f => f.Company == new PlayerId(1)), "private snapshots never expose another company");
            c.That(game.Seat(0).View.Feature<ObjectiveView>().Observe("field.ready").Satisfied, "ordinary declared provider observes actual physical input stock");
            c.That(!game.Seat(1).View.Feature<ObjectiveView>().Observe("field.ready").Satisfied, "another company cannot borrow owner's stock");
            c.Equal(Codes.NotFound, owner.Observe(new PlayerId(0), "missing").Blocker, "unknown enrollment has typed refusal");
            c.Equal(Codes.NotSeated, owner.Observe(new PlayerId(9), "field.ready").Blocker, "unknown company refuses");
            c.Equal(Fix.FromRaw(Fix.One.Raw / 2), game.Seat(0).View.Feature<ObjectiveView>().Observe("field.build").Progress, "all progress combines current owned facts without fake construction");
            c.Throws<NotSupportedException>(() => ((IList<string>)retained.Find(f => f.Objective == "field.win").Prerequisites).Add("fake"), "snapshot prerequisite list is detached readonly");
            c.Equal(hash, game.Hash(), "objective preview/private facts allocate no authoritative state");
            Tick(game);
            c.Equal(ObjectiveState.Completed, Fact(game, "field.ready").State, "native Update alone commits actual root completion");
            c.Equal(ObjectiveState.Locked, Fact(game, "field.build").State, "dependency cannot activate in completion pass");
            Tick(game);
            c.Equal(ObjectiveState.Active, Fact(game, "field.build").State, "next Update activates native prerequisite chain");
            c.That(Fact(game, "field.build").ActivatedTick > Fact(game, "field.ready").SettledTick, "activation uses next pass rather than recursive feedback");
            c.That(retained.TrueForAll(f => f.State == ObjectiveState.Locked), "retained snapshots do not change after Update");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "active chain and observed progress checkpoint identically");
            foreach (string mode in new[] { "missing", "cycle", "duplicate", "logical-cycle", "empty", "missing-predicate", "minimum", "deadline", "missing-deadline", "effect", "missing-effect-field" })
            {
                JsonValue root = Content.Json.Parse(FrameworkProofContent.Read("objectives.json"));
                JsonValue win = root.Get("objectives")[0], build = root.Get("objectives")[1], ready = root.Get("objectives")[2];
                if (mode == "missing") win.Set("prerequisites", Content.Json.Parse("[\"missing\"]"));
                if (mode == "cycle") ready.Set("prerequisites", Content.Json.Parse("[\"field.win\"]"));
                if (mode == "duplicate") win.Set("prerequisites", Content.Json.Parse("[\"field.build\",\"field.build\"]"));
                if (mode == "logical-cycle") root.Get("logical_objective_predicates")[0].Set("predicates", Content.Json.Parse("[\"objective.ready\"]"));
                if (mode == "empty") root.Get("logical_objective_predicates")[0].Set("predicates", JsonValue.NewArray());
                if (mode == "missing-predicate") build.Set("predicate", JsonValue.Of("missing"));
                if (mode == "minimum") root.Get("entity_objective_predicates")[0].Set("minimum", JsonValue.Number("0"));
                if (mode == "deadline") build.Get("deadline").Set("numerator", JsonValue.Number("0"));
                if (mode == "missing-deadline") build.Remove("deadline");
                if (mode == "effect") build.Set("on_complete", JsonValue.Of("missing"));
                if (mode == "missing-effect-field") build.Remove("on_failure");
                c.Throws<ContentException>(() => New(Factory(root.ToString())), "invalid authored " + mode + " refuses composition");
            }
            foreach (string settings in new[] { "{\"companies\":[{\"company\":0,\"objectives\":[\"field.win\"]}]}",
                "{\"companies\":[{\"company\":0,\"objectives\":[\"field.ready\",\"field.ready\"]}]}",
                "{\"companies\":[{\"company\":9,\"objectives\":[]}]}",
                "{\"companies\":[{\"company\":0,\"objectives\":[\"missing\"]}]}",
                "{\"companies\":[{\"company\":0}]}" })
                c.Throws<ContentException>(() => New(Factory(scenarioEdit: scenario => scenario.Get("settings").Set(ObjectiveModule.ModuleId, Content.Json.Parse(settings)))), "invalid company/prerequisite enrollment refuses");
            c.Throws<ContentException>(() => New(Factory(scenarioEdit: scenario => scenario.Get("settings").Remove(ObjectiveModule.ModuleId))), "missing settings cannot imply all objectives");
            SimGame empty = New(Factory(scenarioEdit: scenario => scenario.Get("settings").Set(ObjectiveModule.ModuleId, Content.Json.Parse("{\"companies\":[]}"))));
            c.Equal(0, Facts(empty).Count, "explicit empty enrollment enrolls nobody");
            SimGame absent = ConstructionPlanTests.NewPoints(ConstructionPlanTests.Points());
            c.That(!absent.World.Services.Has<IObjectives>(), "objective-free game has no hidden victory ledger");
            c.Equal(Fix.FromInt(20), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "objectives do not consume/copy/grant physical stock");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Configure(c, game);
            long plan = Build(c, game, 12); SimGame twin = factory.Load(game.Save());
            Until(game, () => Towers(game) == 1, "first actual construction", twin);
            c.Equal(ObjectiveState.Completing, Fact(game, "field.build").State, "earned physical condition waits for actual effect gate");
            c.Equal(ObjectiveCodes.Condition, Fact(game, "field.build").Blocker, "effect wait is explicit rather than silent completion");
            c.Equal(ObjectiveState.Locked, Fact(game, "field.win").State, "effect wait cannot satisfy dependent victory");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "first real tower consumes eight actual hauled inputs");
            c.Equal(game.Hash(), twin.Hash(), "physical completion and effect wait twins match");
            twin = factory.Load(game.Save()); var waiting = Fact(game, "field.build");
            for (int i = 0; i < 7; i++) { Tick(game); Tick(twin); }
            c.Equal(ObjectiveState.Completing, Fact(game, "field.build").State, "effect waits preserve earned completion across reload");
            c.Equal(waiting.ActivatedTick, Fact(game, "field.build").ActivatedTick, "effect wait does not restart deadline/progress");
            c.Equal(game.Hash(), twin.Hash(), "saved pending-effect continuation is isolated");
            Build(c, game, 20); Build(c, twin, 20);
            Until(game, () => Fact(game, "field.build").State == ObjectiveState.Completed, "second real tower admits external effect", twin);
            c.Equal(2, Towers(game), "two actual structures exist, never objective ghosts");
            c.Equal(ObjectiveState.Locked, Fact(game, "field.win").State, "whole-pass capture blocks same-tick effect/dependency feedback");
            c.That(game.Outcome.IsNone, "victory not inferred by player driver");
            c.Equal(1, EffectNotices(game), "external effect commits exactly once after actual gate readiness");
            SimGame completedEffect = factory.Load(game.Save());
            Tick(completedEffect);
            c.Equal(0, EffectNotices(completedEffect), "saved completion never reapplies its irreversible effect");
            Tick(game); Tick(twin);
            c.Equal(new Code("proof.colony-established"), game.Outcome, "native objective effect sets sole engine outcome");
            c.Equal(ObjectiveState.Completed, Fact(game, "field.win").State, "final chain commits once");
            c.Equal(Fix.FromInt(4), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "two constructions consume sixteen real inputs");
            c.That(Facts(game, 1).TrueForAll(f => f.State != ObjectiveState.Completed), "private beneficiary never borrows another company's victory conditions");
            c.Equal(game.Hash(), twin.Hash(), "entire physical chain and outcome twin hashes match");
            SimGame loaded = factory.Load(game.Save()); long endedTick = loaded.Tick;
            loaded.Update(); c.Equal(endedTick, loaded.Tick, "engine outcome halts native loop without host rules");
            c.Equal(game.Hash(), loaded.Hash(), "terminal outcome and once-only effects checkpoint");
            Deadlines(c);
            SimGame corrupt = factory.Load(game.Save());
            var entries = (IList)typeof(ObjectiveSystem).GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Owner(corrupt));
            entries[0].GetType().GetField("Activated", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(entries[0], corrupt.Tick + 1);
            c.Throws<CorruptStateException>(() => corrupt.Save(), "future objective activation cannot save");
            c.Throws<CorruptStateException>(() => corrupt.Hash(), "future objective activation cannot hash as settled");
        }
        private static void Deadlines(Check c)
        {
            JsonValue root = Content.Json.Parse(FrameworkProofContent.Read("objectives.json"));
            root.Get("objectives")[1].Set("deadline", Content.Json.Parse("{\"numerator\":3,\"denominator\":1,\"unit\":\"minute\"}"));
            root.Get("objectives")[0].Set("on_complete", JsonValue.Null());
            GameFactory factory = Factory(root.ToString()); SimGame game = New(factory); Tick(game); Tick(game);
            var active = Fact(game, "field.build"); c.Equal(ObjectiveState.Active, active.State, "deadline scenario starts ordinary unsatisfied objective");
            c.Equal(active.ActivatedTick + game.World.Time.Ticks(GameDuration.Minutes(3)), active.DeadlineTick, "deadline uses actual session quantum");
            SimGame twin = factory.Load(game.Save());
            Until(game, () => Fact(game, "field.win").State == ObjectiveState.Failed, "declared deadline and failed dependency propagation", twin);
            c.Equal(ObjectiveCodes.Deadline, Fact(game, "field.build").Failure, "missing actual work fails by engine game-time deadline");
            c.Equal(ObjectiveCodes.DependencyFailed, Fact(game, "field.win").Failure, "failed prerequisite cannot be skipped");
            c.That(!Fact(game, "field.win").ActivatedTick.HasValue, "failed dependent never pretends to activate");
            c.Equal(active.DeadlineTick, Fact(game, "field.build").SettledTick, "expiry commits at exact deadline tick");
            c.Equal(game.Hash(), twin.Hash(), "deadline and dependency failure survive saved continuation");
            c.That(game.Outcome.IsNone, "failure with explicit null effect invents no defeat/economy action");
            root.Get("objectives")[1].Set("on_failure", JsonValue.Of("objective.wait-two"));
            factory = Factory(root.ToString()); game = New(factory); Configure(c, game); Tick(game);
            Until(game, () => Fact(game, "field.build").State == ObjectiveState.Failing, "failure effect admission");
            c.Equal(ObjectiveCodes.Condition, Fact(game, "field.build").Blocker, "failure effect has its actual gate blocker");
            c.Equal(ObjectiveCodes.Deadline, Fact(game, "field.build").Failure, "failure effect retains original deadline cause");
            c.Equal(ObjectiveState.Locked, Fact(game, "field.win").State, "dependent waits for committed failure, not an uncommitted effect");
            twin = factory.Load(game.Save()); long? activated = Fact(game, "field.build").ActivatedTick;
            Build(c, game, 12); Build(c, twin, 12);
            Until(game, () => Towers(game) == 1, "failure response physical first construction", twin);
            c.Equal(ObjectiveState.Failing, Fact(game, "field.build").State, "one structure does not bypass authored failure gate");
            Build(c, game, 20); Build(c, twin, 20);
            Until(game, () => Fact(game, "field.win").State == ObjectiveState.Failed, "failure response effect and propagation", twin);
            c.Equal(activated, Fact(game, "field.build").ActivatedTick, "delayed failure preserves original activation");
            c.Equal(1, EffectNotices(game), "failure effect commits once after real gate");
            c.Equal(Fix.FromInt(4), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "failure response uses same real work/materials");
            c.Equal(game.Hash(), twin.Hash(), "saved Failing phase and late effect are twin-identical");
            SimGame failed = factory.Load(game.Save()); Tick(failed);
            c.Equal(0, EffectNotices(failed), "saved settled failure never repeats its effect");
        }
    }
}
