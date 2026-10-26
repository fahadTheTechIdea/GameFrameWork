using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string ConditionGraphCase = "Networked conditions: private authored DAG waits physical construction and saved clock isolation";
        private static Code GraphCheck(SimGame game, string id, int company = 0)
            => game.Seat(company).View.Feature<GameConditionsView>().CheckCondition(id);
        private static PlaybookStepSnapshot GraphClaim(SimGame game)
        {
            var rows = new List<PlaybookStepSnapshot>();
            game.Seat(0).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(0), rows); return rows.Single();
        }
        private static List<PlanView> GraphPlans(SimGame game)
        { var rows = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(rows); return rows; }
        private static Fix GraphStock(SimGame game, int company)
        {
            Fix total = Fix.Zero;
            foreach (var entity in game.Seat(company).Snapshot.Entities.Where(e => e.Owner == new PlayerId(company))) {
                var facets = new List<Facet>(); game.Seat(company).View.Inspect(entity.Id, facets);
                foreach (var fact in facets.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply")) total += fact.Value;
            }
            return total;
        }
        private static EntityId GraphEntity(SimGame game, string def, int company = 0)
            => game.Seat(company).Snapshot.Entities.Single(e => e.Def == def && e.Owner == new PlayerId(company)).Id;
        private static string GraphDiagnostics(SimGame game) => "at=" + game.Clock.StepIndex + "/all=" + GraphCheck(game, "policy.a.all") +
            "/strict=" + GraphCheck(game, "policy.z.strict") + "/stock=" + GraphStock(game, 0) + "/claim=" + GraphClaim(game).Status +
            "/pending=" + GraphClaim(game).PendingCommand + "/plans=" + string.Join(";", GraphPlans(game).Select(p => p.Id + "/" + p.State + "/" + p.Order + "/" + p.Blocker)) +
            "/machine=" + PermanentRemovalTests.At(game, GraphEntity(game, "machine")) + "/jobs=" + string.Join(";", PermanentRemovalTests.Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.ToilIndex + "/" + j.Blocker));
        private static Pair GraphPair(SimGame a, SimGame b)
        {
            void Observe(SimGame game)
            {
                ulong before = game.Hash();
                if (GraphCheck(game, "missing") != Codes.UnknownDef || GraphCheck(game, null) != Codes.UnknownDef)
                    throw new InvalidOperationException("healthy unknown graph silently substituted");
                if (GraphStock(game, 1) != Fix.FromInt(52) || GraphStock(game, 0) < Fix.FromInt(44) || GraphStock(game, 0) > Fix.FromInt(52))
                    throw new InvalidOperationException("private physical graph stock escaped original authored bounds");
                var denied = new List<PlaybookStepSnapshot>();
                if (game.Seat(1).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(0), denied) || denied.Count != 0)
                    throw new InvalidOperationException("foreign private strategy leaked");
                if (before != game.Hash()) throw new InvalidOperationException("graph observations mutated authoritative state");
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, GraphDiagnostics, 0);
        }
        private static Pair GraphRestore(Check c, GameFactory factory, Pair source)
        {
            var saved = GraphPair(factory.Load(source.A.Save()), factory.Load(source.B.Save()));
            c.Equal(source.A.Hash(), saved.A.Hash(), "native graph leaves and physical state restore exactly");
            c.Equal(source.B.Hash(), saved.B.Hash(), "peer save retains original command counters"); return saved;
        }
        private static void ConditionGraphRun(Check c)
        {
            var factory = CompositeGameConditionTests.Factory(editSites: sites => {
                // The idle rival cannot occupy the sole source approach of this
                // one-dimensional proof map. Author its parking before game creation;
                // do not remove physical occupancy checks or move a live actor.
                var setups = sites.Get("entity_setups");
                for (int i = 0; i < setups.Count; i++)
                    if (setups[i].Get("id").Text == "choice.rival.machine")
                        setups[i].Get("placement").Set("position", JsonValue.Number("80"));
            });
            SimGame New() => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 719, Seats = { Controller.Human, Controller.Human } });
            var pair = GraphPair(New(), New());
            Console.WriteLine("MODULES condition-graph=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(pair.A.Modules.Contains(GameConditionsModule.ModuleId) && !pair.A.Modules.Contains(TycoonModule.ModuleId), "original pure conditions need no business");
            c.Equal("controller.risk-disabled", GraphCheck(pair.A, "policy.a.all").Id, "native all retains actual first child refusal");
            var retained = GraphClaim(pair.A);
            var limits = new ReplayLimits(16 * 1024 * 1024, 12000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            Submit(c, pair, new ConfigureWorkPolicyCommand { Worker = GraphEntity(pair.A, "machine"), Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
            int foreign = Submit(c, pair, new ConfigureOperationCommand { Entity = GraphEntity(pair.B, "strategy.beacon", 1), Operation = new Code("risk-dial"), Enabled = true, Target = Fix.Zero }, seat: 1);
            Reach(pair, g => Result(g, 1, foreign)?.Accepted == true, "foreign risk policy admitted through ordinary own seat");
            c.That(GraphCheck(pair.A, "policy.b.any", 1).IsNone && GraphCheck(pair.A, "policy.b.any").Id == "controller.risk-disabled",
                "foreign true signal does not satisfy own any graph before original five-minute clock branch");
            int auto = Submit(c, pair, new SetControllerCommand { Controller = Controller.Autopilot });
            Reach(pair, g => Result(g, 0, auto)?.Accepted == true, "native controller enters actual blocked strategy");
            c.That(GraphPlans(pair.A).Count == 0 && GraphClaim(pair.A).PendingCommand == 0, "real logical wait creates neither project nor autonomous input");
            var restored = GraphRestore(c, factory, pair);
            var waitLeft = restored.A.RecordReplay(limits); var waitRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "native sole clock pauses before elapsed readiness");
            long at = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, g => g.Clock.StepIndex >= 7 && GraphPlans(g).Any(), "saved clock independently satisfies real grace and admits native project"); pair.Round();
            c.That(pair.A.Clock.StepIndex == at && pair.A.Hash() == frozen && GraphCheck(pair.A, "policy.a.all").Id == "controller.risk-disabled" &&
                GraphCheck(restored.A, "policy.a.all").IsNone, "no elapsed/readiness/intent borrowed across advancing sessions");
            pair.A.Net.RequestResume();
            Both(pair, restored, g => GraphPlans(g).Any() && LoadNodes(g, GraphPlans(g).Single().Order).Any(n => n.Progress.WholeTicks > 0 && n.Progress.WholeTicks < n.RequiredTicks),
                "original selected machine travels supplies and earns real construction prefix");
            var plan = GraphPlans(pair.A).Single();
            c.That(plan.State == PlanState.Active && plan.Arguments.Get("placement").Get("position").Text == "30" &&
                GraphCheck(pair.A, "policy.z.strict").Id == "controller.risk-disabled" && GraphCheck(pair.A, "policy.a.all").IsNone,
                "elapsed any is not strict all and stable priority selects original primary site");
            c.That(retained.SelectedSite == null && retained.Sites.All(s => !s.Ready), "retained private graph evidence stays detached");
            ReplayPair(c, factory, waitLeft, waitRight, restored, limits, 0, "saved logical wait native producer and physical prefix replay");
            restored = GraphRestore(c, factory, pair);
            var workLeft = restored.A.RecordReplay(limits); var workRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new ConfigureOperationCommand {
                Entity = GraphEntity(current.A, "strategy.beacon"), Operation = new Code("risk-dial"), Enabled = true, Target = Fix.Zero });
            Both(pair, restored, g => GraphCheck(g, "policy.z.strict").IsNone, "own normal policy now satisfies both actual strict leaves");
            Both(pair, restored, g => GraphClaim(g).Status == PlaybookStepStatus.Done, "full original hauling prep and build durations actually complete");
            c.That(GraphStock(pair.A, 0) == Fix.FromInt(44) && GraphStock(pair.A, 1) == Fix.FromInt(52) &&
                pair.A.Seat(0).Snapshot.Entities.Count(e => e.Owner == new PlayerId(0) && e.Def == "point.tower") == 1, "one physical build consumes eight own units and no rival stock");
            ReplayPair(c, factory, workLeft, workRight, restored, limits, 1, "saved actual work to private strict policy and completed construction replay");
            ReplayPair(c, factory, left, right, pair, limits, 4, "full private condition graph physical player stream replay");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && pair.A.Hash() == restored.B.Hash(), "four actually advanced worlds retain real shuffled lockstep convergence");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "completed sole-authority graph checkpoint");
        }
    }
}
