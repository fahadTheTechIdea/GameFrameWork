using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Plans = Beep.SimEngine.Tests.PlanTemplateTests;
using Policy = Beep.SimEngine.Tests.LogisticsPolicyContentTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string HistoryPhysicalCase = "Networked plan history: physical dependency construction, private retention and safe retirement";
        internal const string HistoryBudgetCase = "Networked plan history policy: public one-cohort budget, saved cursor and live-head fairness";
        private static string HistoryDiagnostics(SimGame game) => "at=" + game.Clock.StepIndex + "/cursor=" + HistoryPolicy(game).Cursor +
            "/plans=" + string.Join(";", Plans.TemplatePlans(game).Select(p => p.Id + "/" + p.State + "/" + p.Blocker)) +
            "/history=" + string.Join(";", Plans.HistoryRows(game).Select(h => h.Head + "/" + h.Blocker));
        private static PlanHistoryPolicyView HistoryPolicy(SimGame game)
            => game.Seat(0).View.Feature<WorkView>().ReadPlanHistoryPolicy() ?? throw new InvalidOperationException("explicit authored history policy absent");
        private static Pair HistoryPair(SimGame a, SimGame b)
        {
            var observed = new Dictionary<SimGame, (long Step, HashSet<long> Plans)>();
            void Observe(SimGame game)
            {
                if (observed.TryGetValue(game, out var old) && old.Step == game.Clock.StepIndex) return;
                var plans = Plans.TemplatePlans(game); var ids = plans.Select(p => p.Id).ToHashSet();
                if (old.Plans != null && old.Plans.Except(ids).Count() > HistoryPolicy(game).CohortsPerUpdate)
                    throw new InvalidOperationException("single-plan cohorts exceed authored retirement budget/" + HistoryDiagnostics(game));
                if (plans.Any(p => p.Dependencies.Any(id => !ids.Contains(id))))
                    throw new InvalidOperationException("retirement left an orphaned live/terminal plan dependency");
                BigInteger stock = 0, produced = 0, removed = 0;
                foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0))) {
                    var facets = new List<Facet>(); game.Seat(0).View.Inspect(entity.Id, facets);
                    foreach (var facet in facets.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply")) stock += facet.Value.Raw;
                }
                foreach (var row in game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates) {
                    if (row.Kind == ResourceFlowKind.AuthoredCreation || row.Kind == ResourceFlowKind.StockPurchase)
                        throw new InvalidOperationException("history workflow fabricated resources");
                    if (row.Resource != Policy.Supply(game)) continue;
                    if (row.Kind == ResourceFlowKind.Produced) produced += row.AmountRaw;
                    if (row.Kind == ResourceFlowKind.Consumed || row.Kind == ResourceFlowKind.Lost || row.Kind == ResourceFlowKind.Destroyed) removed += row.AmountRaw;
                }
                if (new BigInteger(Fix.FromInt(20).Raw) + produced != stock + removed)
                    throw new InvalidOperationException("construction/history resource conservation failed/" + HistoryDiagnostics(game));
                var foreign = new List<PlanView>(); game.Seat(1).View.Feature<WorkView>().CopyPlans(foreign);
                if (Plans.HistoryRows(game).Any(h => h.Plans.Any(id => foreign.Any(p => p.Id == id))) ||
                    Plans.HistoryRows(game, 1).Any(h => h.Plans.Any(ids.Contains)))
                    throw new InvalidOperationException("company-private plan history leaked");
                observed[game] = (game.Clock.StepIndex, ids);
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, HistoryDiagnostics, 0);
        }
        private static Pair HistoryRestore(Check c, GameFactory factory, Pair source)
        {
            var pair = HistoryPair(factory.Load(source.A.Save()), factory.Load(source.B.Save()));
            c.Equal(source.A.Hash(), pair.A.Hash(), "original plan/terminal chronology/cursor/resources restore together");
            c.Equal(source.B.Hash(), pair.B.Hash(), "peer restores exact native board state");
            c.Equal(HistoryPolicy(source.A).Cursor, HistoryPolicy(pair.A).Cursor, "saved fair-scan cursor remains an actual stable identity");
            return pair;
        }
        private static SubmitPlanCommand HistoryBuild(int position, string group, params long[] dependencies)
            => new SubmitPlanCommand { Intent = Plans.DependencyBuild(Json.Parse("{\"space\":\"field.points\",\"position\":" + position + "}"), group, dependencies) };
        private static RetirePlanHistoryCommand HistoryRetire(params long[] ids) => new RetirePlanHistoryCommand { Selection = new PlanHistorySelection(ids) };
        private static int HistoryTowers(SimGame game) => game.Seat(0).Snapshot.Entities.Count(e => e.Owner == new PlayerId(0) && e.Def == "point.tower");
        private static bool HistoryProductive(SimGame game, long id)
        {
            var plan = Plans.TemplatePlan(game, id); var nodes = new List<ActivityNodeView>();
            return plan.Order > 0 && game.Seat(0).View.Feature<WorkView>().TryCopyNodes(plan.Order, nodes) && nodes.Any(n => n.ProductiveSteps > 0 && n.State != NodeState.Done);
        }
        private static void HistoryPhysicalRun(Check c)
        {
            var factory = Plans.AutomaticHistoryFactory();
            var pair = HistoryPair(ConstructionPlanTests.NewPoints(factory, ResourceFlowsModule.ModuleId), ConstructionPlanTests.NewPoints(factory, ResourceFlowsModule.ModuleId));
            Console.WriteLine("MODULES plan-history=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(!pair.A.Modules.Contains(TycoonModule.ModuleId) && pair.A.Modules.Contains(PlanHistoryModule.ModuleId), "authored retention is optional engine history, not business");
            c.That(HistoryPolicy(pair.A).MinimumAge.Equals(GameDuration.Minutes(6)) && HistoryPolicy(pair.A).MinimumRetainedCohorts == 1 && HistoryPolicy(pair.A).CohortsPerUpdate == 2,
                "original six-minute/one-retained/two-scan JSON policy is unchanged");
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            Pair restored = null; int issued = 0;
            int Input(Func<SimGame, Command> make, Code expected = default, int seat = 0) {
                int ticket = Submit(c, pair, make(pair.A), expected, seat); issued++;
                if (restored != null) c.Equal(ticket, Submit(c, restored, make(restored.A), expected, seat), "saved branch preserves actual player sequence");
                bool Answered(SimGame game) => Result(game, seat, ticket)?.Refusal == expected;
                if (restored == null) Reach(pair, Answered, "ordinary history input admitted/answered"); else Both(pair, restored, Answered, "same actual history input on all peers");
                return ticket;
            }
            long Plan(int ticket, int seat = 0) => Result(pair.A, seat, ticket).Value.Arg0;
            long parent = Plan(Input(_ => HistoryBuild(12, "history.parent")));
            long child = Plan(Input(_ => HistoryBuild(16, "history.child", parent)));
            long foreign = Plan(Input(_ => HistoryBuild(80, "history.foreign"), seat: 1), 1);
            long live = Plan(Input(_ => HistoryBuild(40, "history.live")));
            c.That(HistoryTowers(pair.A) == 0 && Plans.TemplatePlans(pair.A).All(p => p.Paused && p.Order == 0), "paused dependency intent creates no completed structure or resources");
            Input(game => EmergencyPreemptionTests.ConfigureCommand(Policy.Id(game, "machine")));
            Input(_ => new ConfigurePlanGroupCommand { Group = "history.parent", Priority = 70, Paused = false });
            Input(_ => new ConfigurePlanGroupCommand { Group = "history.child", Priority = 70, Paused = false });
            Reach(pair, game => HistoryProductive(game, parent), "actual first twelve-minute preparation earns physical work");
            c.That(Plans.TemplatePlan(pair.A, child).Order == 0 && HistoryTowers(pair.A) == 0, "unearned prerequisite cannot activate the dependent or create a tower");
            restored = HistoryRestore(c, factory, pair); int partialStart = issued;
            var partialLeft = restored.A.RecordReplay(limits); var partialRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => Plans.TemplatePlan(game, parent).State == PlanState.Done && Plans.TemplatePlan(game, child).State == PlanState.Active,
                "first real construction completes before dependent work activates");
            c.Equal(1, HistoryTowers(pair.A), "first real earned tower, not two instant placements");
            Both(pair, restored, game => Plans.TemplatePlan(game, child).State == PlanState.Done, "second actual material-consuming construction completes");
            c.That(HistoryTowers(pair.A) == 2 && Policy.Stock(pair.A, "source") == Fix.FromInt(12), "two original four-unit builds consume exactly eight finite supplies");
            c.Equal(PlanCodes.HistoryDependency, Plans.HistoryRows(pair.A).Single(h => h.Head == parent).Blocker, "minimum-retained child keeps its actual prerequisite");
            c.Equal(PlanCodes.HistoryMinimum, Plans.HistoryRows(pair.A).Single(h => h.Head == child).Blocker, "newest completed cohort is retained by native policy");
            var completed = Plans.TemplatePlan(pair.A, child); var retained = Plans.HistoryRows(pair.A);
            ReplayPair(c, factory, partialLeft, partialRight, restored, limits, issued - partialStart, "saved productive construction through completed dependency history replay");
            restored = HistoryRestore(c, factory, pair); int terminalStart = issued;
            var terminalLeft = restored.A.RecordReplay(limits); var terminalRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, game => game.Clock.Paused, "native network pause on retained history");
            long paused = pair.A.Clock.StepIndex, cursor = HistoryPolicy(pair.A).Cursor; ulong frozen = pair.A.Hash();
            Reach(restored, game => game.Clock.StepIndex >= paused + 8, "other saved peers advance real age and cleanup scans");
            pair.Round(); c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen && HistoryPolicy(pair.A).Cursor == cursor, "paused original borrows no history age/cursor/resources");
            pair.A.Net.RequestResume(); Both(pair, restored, game => Plans.TemplatePlan(game, child).State == PlanState.Done, "native resume converges with independently advanced retained history");
            Input(_ => new CancelPlanGroupCommand { Group = "history.foreign" }, seat: 1);
            c.That(Plans.HistoryRows(pair.A, 1).Single().Head == foreign && Plans.HistoryRows(pair.A, 1).Single().Blocker == PlanCodes.HistoryMinimum,
                "minimum retained cohorts is per company, not a shared global count");
            Input(_ => HistoryRetire(parent), PlanCodes.HistoryDependency);
            long newest = Plan(Input(_ => HistoryBuild(24, "history.newest")));
            Input(_ => new CancelPlanGroupCommand { Group = "history.newest" });
            Both(pair, restored, game => Plans.TemplatePlan(game, parent).Id == 0 && Plans.TemplatePlan(game, child).Id == 0, "genuinely newer terminal cohort permits dependency-safe automatic retirement");
            c.That(HistoryTowers(pair.A) == 2 && Policy.Stock(pair.A, "source") == Fix.FromInt(12), "retiring plan history never destroys real towers or refunds/consumes resources");
            c.That(retained.Any(h => h.Head == child) && completed.Dependencies.Single() == parent, "retained old history/graph values remain detached after automatic deletion");
            Input(_ => new RepeatPlanCommand { Plan = parent }, Codes.NotFound);
            Input(_ => HistoryRetire(newest));
            c.That(Plans.TemplatePlan(pair.A, newest).Id == 0 && Plans.TemplatePlan(pair.A, live).State == PlanState.Pending, "explicit safe retirement may override policy minimum but not delete live work");
            ReplayPair(c, factory, terminalLeft, terminalRight, restored, limits, issued - terminalStart, "retained dependency through private cancellation and automatic/manual retirement replay");
            restored = HistoryRestore(c, factory, pair); int youngStart = issued;
            var youngLeft = restored.A.RecordReplay(limits); var youngRight = restored.B.RecordReplay(limits);
            long aging = Plan(Input(_ => HistoryBuild(24, "history.aging")));
            long latest = Plan(Input(_ => HistoryBuild(28, "history.latest")));
            long created = Plans.TemplatePlan(pair.A, aging).CreatedTick;
            Both(pair, restored, game => game.Clock.StepIndex >= created + 10, "old but still live plan is not disposable terminal history");
            c.Equal(PlanCodes.NotEnded, Plans.HistoryRows(pair.A).Single(h => h.Head == aging).Blocker, "retention age never starts at intent creation");
            // Same ordinary input batch gives both terminal cohorts the same real end time;
            // stable later ID is the retained minimum, leaving the older one subject to age.
            int firstCancel = Submit(c, pair, new CancelPlanGroupCommand { Group = "history.aging" }); issued++;
            int lastCancel = Submit(c, pair, new CancelPlanGroupCommand { Group = "history.latest" }); issued++;
            c.Equal(firstCancel, Submit(c, restored, new CancelPlanGroupCommand { Group = "history.aging" }), "saved batch first player identity");
            c.Equal(lastCancel, Submit(c, restored, new CancelPlanGroupCommand { Group = "history.latest" }), "saved batch last player identity");
            Both(pair, restored, game => Result(game, 0, lastCancel)?.Accepted == true, "ordinary terminal cancellation batch commits");
            c.That(Plans.TemplatePlan(pair.A, aging).EndedTick >= created + 10 && Plans.HistoryRows(pair.A).Single(h => h.Head == aging).Blocker == PlanCodes.HistoryYoung,
                "six-minute age starts only at actual termination, not old creation time");
            ReplayPair(c, factory, youngLeft, youngRight, restored, limits, issued - youngStart, "retired-history through old-live and fresh-terminal chronology replay");
            restored = HistoryRestore(c, factory, pair); int finalStart = issued;
            var finalLeft = restored.A.RecordReplay(limits); var finalRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => Plans.TemplatePlan(game, aging).Id == 0, "saved young history earns its full age before retirement");
            c.That(Plans.TemplatePlan(pair.A, latest).State == PlanState.Cancelled && Plans.HistoryRows(pair.A, 1).Single().Head == foreign,
                "each company's genuine newest terminal history remains protected");
            c.That(aging > newest && latest > aging, "automatic/manual retirement never recycles actual native plan identities");
            Input(_ => HistoryRetire(foreign), Codes.NotOwned);
            Input(_ => HistoryRetire(latest, aging), Codes.NotFound);
            c.Equal(latest, Plans.TemplatePlan(pair.A, latest).Id, "late missing member refuses atomic retirement without deleting a valid prefix");
            Input(_ => HistoryRetire(latest)); Input(_ => HistoryRetire(live), PlanCodes.NotEnded);
            c.That(Plans.TemplatePlans(pair.A).Count == 1 && Plans.TemplatePlan(pair.A, live).State == PlanState.Pending &&
                HistoryTowers(pair.A) == 2 && Policy.Stock(pair.A, "source") == Fix.FromInt(12), "terminal history cleanup retains real live intent and finished physical world");
            c.That(pair.Stalls > 0 && restored.Stalls > 0 && pair.ReversedBatches > 0 && restored.ReversedBatches > 0, "native original/restored history transports actually stall and reorder bytes");
            ReplayPair(c, factory, finalLeft, finalRight, restored, limits, issued - finalStart, "saved young cohort through private/missing/live refusal and explicit retirement replay");
            ReplayPair(c, factory, left, right, pair, limits, issued, "full physical private plan-history sequence replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "all four genuinely advanced physical/history worlds converge");
            HistoryRestore(c, factory, pair); Console.WriteLine("HISTORY inputs=" + issued);
        }
        private static void HistoryBudgetRun(Check c)
        {
            var factory = Plans.AutomaticHistoryFactory("history.public");
            var pair = HistoryPair(ConstructionPlanTests.NewPoints(factory, ResourceFlowsModule.ModuleId), ConstructionPlanTests.NewPoints(factory, ResourceFlowsModule.ModuleId));
            Console.WriteLine("MODULES plan-history-budget=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(HistoryPolicy(pair.A).Policy == "history.public" && HistoryPolicy(pair.A).MinimumAge.IsZero && HistoryPolicy(pair.A).MinimumRetainedCohorts == 0 && HistoryPolicy(pair.A).CohortsPerUpdate == 1,
                "existing public definition selects explicit immediate/zero-retained/one-scan policy");
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000); var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            var ids = new List<long>();
            for (int i = 0; i < 5; i++) {
                int x = i == 0 ? 12 : 16 + i * 4; string group = i == 0 ? "history.live-head" : "history.budget." + i;
                int ticket = Submit(c, pair, HistoryBuild(x, group)); Reach(pair, game => Result(game, 0, ticket)?.Accepted == true, "ordinary paused cohort intent");
                ids.Add(Result(pair.A, 0, ticket).Value.Arg0);
            }
            var restored = HistoryRestore(c, factory, pair); var batchLeft = restored.A.RecordReplay(limits); var batchRight = restored.B.RecordReplay(limits);
            int last = 0;
            for (int i = 1; i < 5; i++) {
                string group = "history.budget." + i; last = Submit(c, pair, new CancelPlanGroupCommand { Group = group });
                c.Equal(last, Submit(c, restored, new CancelPlanGroupCommand { Group = group }), "same actual batch identity on saved peers");
            }
            Both(pair, restored, game => Result(game, 0, last)?.Accepted == true && Plans.TemplatePlans(game).Count == 4, "one original update retires only one of four freshly terminal cohorts");
            var held = Plans.HistoryRows(pair.A); long cursor = HistoryPolicy(pair.A).Cursor;
            c.That(held.Count == 4 && Plans.TemplatePlan(pair.A, ids[0]).State == PlanState.Pending, "permanently live head survives one-budget scan");
            ReplayPair(c, factory, batchLeft, batchRight, restored, limits, 4, "saved live intents through partially budgeted cancellation/retirement replay");
            restored = HistoryRestore(c, factory, pair); var finalLeft = restored.A.RecordReplay(limits); var finalRight = restored.B.RecordReplay(limits);
            c.Equal(cursor, HistoryPolicy(restored.A).Cursor, "partial cleanup cursor survives source reattachment");
            Both(pair, restored, game => Plans.TemplatePlans(game).Count == 1, "saved fair scan skips live head and retires every eligible terminal cohort");
            c.That(Plans.TemplatePlan(pair.A, ids[0]).State == PlanState.Pending && held.Count == 4 && Plans.HistoryRows(pair.A).Single().Blocker == PlanCodes.NotEnded,
                "fair retirement preserves live work and detached old membership");
            int fresh = Submit(c, pair, HistoryBuild(40, "history.after-budget")); c.Equal(fresh, Submit(c, restored, HistoryBuild(40, "history.after-budget")), "new post-retirement input identity");
            Both(pair, restored, game => Result(game, 0, fresh)?.Accepted == true, "new actual plan after identity gaps");
            long newId = Result(pair.A, 0, fresh).Value.Arg0; c.That(newId > ids.Max(), "public policy cannot recycle deleted native identities");
            int cancel = Submit(c, pair, new CancelPlanGroupCommand { Group = "history.after-budget" });
            c.Equal(cancel, Submit(c, restored, new CancelPlanGroupCommand { Group = "history.after-budget" }), "new terminal command identity");
            Both(pair, restored, game => Plans.TemplatePlans(game).Count == 1 && Plans.TemplatePlan(game, newId).Id == 0, "same one-budget policy retires a genuinely new terminal cohort");
            c.That(HistoryTowers(pair.A) == 0 && Policy.Stock(pair.A, "source") == Fix.FromInt(20), "cancelled paused intent/history never constructs or grants material");
            ReplayPair(c, factory, finalLeft, finalRight, restored, limits, 2, "saved partial scan through fair completion and fresh terminal retirement replay");
            ReplayPair(c, factory, left, right, pair, limits, 11, "full public-policy fair-scan sequence replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "all four genuinely advanced public-policy worlds converge");
            HistoryRestore(c, factory, pair);
        }
    }
}
