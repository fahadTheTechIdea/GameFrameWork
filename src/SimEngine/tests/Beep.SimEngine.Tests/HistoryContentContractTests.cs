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
    internal static class HistoryContentContractTests
    {
        internal const string Case = "History content contracts: independent JSON cohorts retain dependencies and physical work";
        private static readonly ReplayLimits Limits = new ReplayLimits(4 * 1024 * 1024, 3000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(InitializeProbe initialize, out HistoryContractModule proof,
            Action<JsonValue> edit = null, Action<JsonValue> rootEdit = null, bool selected = true)
        {
            proof = new HistoryContractModule(initialize); var root = Json.Parse(PointGame.Definitions);
            var scenario = root.Get("scenarios")[0]; scenario.Set("seats", JsonValue.Number("2"));
            if (selected) scenario.Get("modules").Add(JsonValue.Of(HistoryContractModule.ModuleId));
            scenario.Get("settings").Set(PlanHistoryModule.ModuleId, Json.Parse("{\"policy\":\"contract.history\"}"));
            StartupContentAuthoring.Position(root, "source", JsonValue.Number("20"));
            foreach (string id in new[] { "history.first", "history.second", "history.third", "history.live" })
            { var activity = root.Get("activities")[0].Clone(); activity.Set("id", JsonValue.Of(id)); root.Get("activities").Add(activity); }
            var foreign = root.Get("entities")[1].Clone(); foreign.Set("id", JsonValue.Of("foreign.target")); root.Get("entities").Add(foreign);
            StartupContentAuthoring.Add(root, "foreign.target", "foreign.target", 1, Json.Parse("{\"space\":\"field.points\",\"position\":40}"));
            var profiles = Json.Parse(FrameworkProofContent.Read("history-contracts.json")); edit?.Invoke(profiles); rootEdit?.Invoke(root);
            return PointGame.Factory(root.ToString(), extraModules: new GameModule[] { new PlanHistoryModule(), proof },
                extraDeclarations: "[{\"id\":\"sim.plan-history\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"proof.history-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.plan-history\",\"version\":\"1.0.0\"}],\"documents\":[\"history-contracts.json\"]}]",
                extraDocuments: new Dictionary<string, string> { { "history-contracts.json", profiles.ToString() } });
        }
        private static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "points", Seed = 813 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request); }
        private static WorkView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static EntityId Entity(SimGame game, string id) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == id).Id;
        private static List<PlanView> Plans(SimGame game, int seat = 0)
        { var result = new List<PlanView>(); View(game, seat).CopyPlans(result); return result; }
        private static List<PlanHistoryCohortView> History(SimGame game, int seat = 0)
        { var result = new List<PlanHistoryCohortView>(); View(game, seat).CopyPlanHistory(result); return result; }
        private static Fix Stock(SimGame game)
        { var values = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, "source"), values); return values.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (a, f) => a + f.Value); }
        private static void Step(SimGame game, SimGame twin = null)
        { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash()) throw new InvalidOperationException("history step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault); }
        private static void Until(SimGame game, SimGame twin, Func<bool> ready, string stage)
        { for (int i = 0; i < 700 && !ready(); i++) Step(game, twin); if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " plans=" + string.Join(";", Plans(game).Select(p => p.Id + "/" + p.State + "/" + p.Blocker))); }
        private static CommandResult Command(Check c, SimGame game, Command command, int seat = 0)
        { ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(seat).Preview(command), "normal preview"); c.Equal(hash, game.Hash(), "preview pure"); var result = TestKit.Order(game, seat, command); c.That(result.Accepted, "normal player command"); return result; }
        private static PlanIntent Intent(SimGame game, string activity, string target, string group, bool paused = true, IEnumerable<long> dependencies = null)
            => new PlanIntent("activity:" + activity, Json.Parse("{\"target\":" + Entity(game, target).Value + "}"), group, 20, paused, dependencies);
        private static long Submit(Check c, SimGame game, string activity, string group, IEnumerable<long> dependencies = null, int seat = 0)
            => Command(c, game, new SubmitPlanCommand { Intent = Intent(game, activity, seat == 0 ? "source" : "foreign.target", group, dependencies: dependencies) }, seat).Arg0;
        private static void Cancel(Check c, SimGame game, string group, int seat = 0) => Command(c, game, new CancelPlanGroupCommand { Group = group }, seat);
        private static void Run(Check c)
        {
            var initialize = new InitializeProbe(); var factory = Factory(initialize, out var proof); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual contract protocol"); c.Equal(154, EngineInfo.SaveFormat, "same board/clock codecs");
            c.That(proof.IndependentAlias, "actual owned selected policy without native inheritance"); c.Equal(1, initialize.Initializations, "valid Initialize once");
            var policy = View(game).ReadPlanHistoryPolicy().Value;
            c.That(policy.Policy == "contract.history" && policy.MinimumAge.Equals(GameDuration.Minutes(6)) && policy.MinimumRetainedCohorts == 1 && policy.CohortsPerUpdate == 1 && policy.Cursor == 0, "explicit JSON facts");
            var recorder = game.RecordReplay(Limits);
            long live = Submit(c, game, "history.live", "keep.live");
            long first = Submit(c, game, "history.first", "old.first");
            long second = Submit(c, game, "history.second", "old.second", new[] { first });
            long newest = Submit(c, game, "history.third", "keep.newest");
            Cancel(c, game, "old.first"); Cancel(c, game, "old.second"); Cancel(c, game, "keep.newest");
            c.Equal(PlanCodes.HistoryYoung, History(game).Single(h => h.Head == second).Blocker, "age begins at terminal time");
            c.Equal(PlanCodes.HistoryMinimum, History(game).Single(h => h.Head == newest).Blocker, "newest actual cohort retained");
            c.Equal(PlanCodes.NotEnded, History(game).Single(h => h.Head == live).Blocker, "live intent cannot be retired");
            var captured = History(game); ulong hash = game.Hash();
            c.Equal(0, History(game, 1).Count, "private history facts");
            c.Equal(hash, game.Hash(), "history queries pure");
            c.Throws<NotSupportedException>(() => ((IList<long>)captured[0].Plans).Clear(), "detached cohort membership immutable");
            long foreignOld = Submit(c, game, "history.first", "foreign.old", seat: 1); Cancel(c, game, "foreign.old", 1);
            long foreignNew = Submit(c, game, "history.second", "foreign.new", seat: 1); Cancel(c, game, "foreign.new", 1);
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "terminal chronology/cursor save");
            game.Clock.Pause(); twin.Clock.Pause(); long time = game.Clock.StepIndex; long cursor = View(game).ReadPlanHistoryPolicy().Value.Cursor; hash = game.Hash();
            for (int i = 0; i < 3; i++) Step(game, twin);
            c.That(game.Clock.StepIndex == time && game.Hash() == hash && View(game).ReadPlanHistoryPolicy().Value.Cursor == cursor, "sole clock pause freezes fair scan");
            game.Clock.Resume(); twin.Clock.Resume();
            for (int i = 0; i < 40; i++)
            {
                int before = Plans(game).Count + Plans(game, 1).Count; Step(game, twin);
                int after = Plans(game).Count + Plans(game, 1).Count;
                c.That(before - after >= 0 && before - after <= 1, "one authored cohort scan budget");
                c.That(!Plans(game).Any(p => p.Id == first) ? !Plans(game).Any(p => p.Id == second) : true, "dependent history cannot outlive retired prerequisite");
            }
            c.That(Plans(game).Select(p => p.Id).OrderBy(p => p).SequenceEqual(new[] { live, newest }), "fair scan retires safe aged dependency history but retains live/newest");
            c.That(Plans(game, 1).Single().Id == foreignNew && !Plans(game, 1).Any(p => p.Id == foreignOld), "minimum retention applies separately per company");
            c.That(captured.Any(h => h.Head == first) && policy.Cursor == 0, "retained snapshots do not mutate");
            var group = new PlanGroupIntent("physical.group", new[] { Intent(game, "inspect", "source", "physical.group"), Intent(game, "inspect", "destination", "physical.group") }, new[] { new PlanGroupDependency(1, 0) });
            var submitted = Command(c, game, new SubmitPlanGroupCommand { Intent = group });
            Command(c, game, new ConfigurePlanGroupCommand { Group = "physical.group", Priority = 20, Paused = false });
            var machine = Entity(game, "machine"); twin = factory.Load(game.Save());
            Until(game, twin, () => ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == machine).Spatial).Position > Fix.Zero, "physical approach");
            c.That(Plans(game).Single(p => p.Id == submitted.Arg0).State == PlanState.Active && Plans(game).Single(p => p.Id == submitted.Arg0 + 1).State == PlanState.Pending, "same native dependency board executes in order");
            twin = factory.Load(game.Save());
            Until(game, twin, () => Plans(game).Where(p => p.Group == "physical.group").All(p => p.State == PlanState.Done), "physical group completion");
            var finished = Plans(game).Where(p => p.Group == "physical.group").ToArray();
            c.That(finished.Length == 2 && finished[1].EndedTick >= finished[0].EndedTick && finished[0].EndedTick > finished[0].CreatedTick + 6, "actual travel and work precede completion");
            for (int i = 0; i < 15; i++) Step(game, twin);
            c.That(History(game).Single(h => h.Head == submitted.Arg0).Plans.Count == 2 && History(game).Single(h => h.Head == submitted.Arg0).Blocker == PlanCodes.HistoryMinimum, "group remains one actual retained cohort");
            c.That(!Plans(game).Any(p => p.Id == newest) && Plans(game).Any(p => p.Id == live), "newer physical cohort replaces old minimum without retiring live work");
            c.Equal(Fix.FromInt(20), Stock(game), "history cleanup cannot consume/refund original stock");
            c.That(!View(game).PreviewRetirePlanHistory(new PlanHistorySelection(new[] { submitted.Arg0 })).Ready, "partial cohort selection refuses");
            Command(c, game, new RetirePlanHistoryCommand { Selection = new PlanHistorySelection(finished.Select(p => p.Id)) });
            c.That(Plans(game).Single().Id == live && game.Seat(0).Snapshot.Entities.Any(e => e.Id == Entity(game, "source")), "safe retirement removes history, not world entities");
            long later = Submit(c, game, "inspect", "later"); c.That(later > submitted.Arg0 + 1, "retirement never reuses stable IDs");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "original policy and board continue");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), Limits).Success, "normal group and retirement commands replay");
            var fresh = New(factory); c.That(Plans(fresh).Count == 0 && View(fresh).ReadPlanHistoryPolicy().Value.Cursor == 0 && Stock(fresh) == Fix.FromInt(20), "fresh interleaved session isolated");
            Absent(c); Invalid(c);
        }
        private static void Absent(Check c)
        {
            var game = New(Factory(new InitializeProbe(), out _, selected: false));
            long id = Submit(c, game, "inspect", "unselected"); Cancel(c, game, "unselected");
            for (int i = 0; i < 20; i++) Step(game);
            c.That(!View(game).ReadPlanHistoryPolicy().HasValue && Plans(game).Single().Id == id && History(game).Single().Blocker == PlanCodes.HistoryUnselected, "unselected module never fabricates default pruning");
        }
        private static void Invalid(Check c)
        {
            foreach (string fault in new[] { "age", "retained", "budget", "negative-retained", "zero-budget", "negative-age", "denominator", "unit", "age-type", "budget-type", "overflow", "unused-overflow", "missing", "null", "wrong-kind" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(initialize, out _, document =>
                {
                    var policy = document.Get("independent_history_policies")[0];
                    if (fault == "age") policy.Remove("minimum_age"); if (fault == "retained") policy.Remove("minimum_retained_cohorts"); if (fault == "budget") policy.Remove("cohorts_per_update");
                    if (fault == "negative-retained") policy.Set("minimum_retained_cohorts", JsonValue.Number("-1")); if (fault == "zero-budget") policy.Set("cohorts_per_update", JsonValue.Number("0"));
                    if (fault == "negative-age") policy.Get("minimum_age").Set("numerator", JsonValue.Number("-1")); if (fault == "denominator") policy.Get("minimum_age").Set("denominator", JsonValue.Number("0"));
                    if (fault == "unit") policy.Get("minimum_age").Set("unit", JsonValue.Of("unknown")); if (fault == "age-type") policy.Set("minimum_age", JsonValue.Of("six"));
                    if (fault == "budget-type") policy.Set("cohorts_per_update", JsonValue.Of("one"));
                    if (fault == "overflow" || fault == "unused-overflow")
                        document.Get("independent_history_policies")[fault == "overflow" ? 0 : 1].Set("minimum_age", Json.Parse("{\"numerator\":9223372036854775807,\"denominator\":1,\"unit\":\"hour\"}"));
                }, root =>
                {
                    var settings = root.Get("scenarios")[0].Get("settings").Get(PlanHistoryModule.ModuleId);
                    if (fault == "missing") settings.Set("policy", JsonValue.Of("missing")); if (fault == "null") settings.Set("policy", JsonValue.Null());
                    if (fault == "wrong-kind") settings.Set("policy", JsonValue.Of("ready"));
                })), "early history refusal " + fault);
                c.Equal(0, initialize.Initializations, "before Initialize " + fault);
            }
        }
    }
}

