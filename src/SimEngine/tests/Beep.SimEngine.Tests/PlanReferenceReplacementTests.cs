using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class PlanReferenceReplacementTests
    {
        internal const string Case = "Plan replacement references: provider schemas targets selected workers and saved physical continuation";
        internal const string SuppliedCase = "Plan replacement references: supplied DAG keeps pending intent active guards loaded delivery and terminal history";
        internal static void Register() { TestRunner.Add(Case, Run); TestRunner.Add(SuppliedCase, Supplied); }
        private static GameFactory Factory(bool worker, string mode = "normal", string incompatible = null, bool supplied = false)
            => EntityReferenceReplacementTests.Factory(moduleDocument:
                "{\"reference_plans\":[{\"id\":\"inspect\",\"activity\":\"inspect\",\"mode\":\"" + mode + "\"}]}",
                edit: root =>
                {
                    if (worker) root.Get("entities")[2].Get("components").Set("point_dock", Json.Parse("""
                        {"work_roles":["inspect"],"resources":[],"slots":[
                          {"id":"service","kind":"service","offset":1},
                          {"id":"wait","kind":"waiting","offset":2},
                          {"id":"exit","kind":"departure","offset":3}]}
                        """));
                    var next = root.Get("entities")[3]; var parts = root.Get("entities")[worker ? 2 : 0].Get("components").Clone();
                    next.Set("components", parts); next.Set("id", JsonValue.Of("replacement"));
                    root.Get("entity_replacement_effects")[0].Set("source", JsonValue.Of(worker ? "machine" : "source"));
                    root.Get("entity_replacement_effects")[0].Set("target", JsonValue.Of("replacement"));
                    StartupContentAuthoring.Position(root, "crew", JsonValue.Number(worker ? "8" : "23"));
                    var parking = root.Get("entities")[1].Clone(); parking.Set("id", JsonValue.Of("parking")); root.Get("entities").Add(parking);
                    StartupContentAuthoring.Add(root, "parking", "parking", 0, Json.Parse("{\"space\":\"field.points\",\"position\":70}"));
                    if (incompatible == "worker") parts.Get("worker").Set("roles", Json.Parse("[\"haul\"]"));
                    if (incompatible == "target")
                    {
                        root.Get("entities")[0].Set("tags", Json.Parse("[\"inspect.target\"]"));
                        next.Set("tags", Json.Parse("[]")); root.Get("activities")[0].Set("target_tags", Json.Parse("[\"inspect.target\"]"));
                    }
                    if (supplied)
                    {
                        var original = root.Get("entities")[0]; var donor = original.Clone(); donor.Set("id", JsonValue.Of("donor")); root.Get("entities").Add(donor);
                        StartupContentAuthoring.Add(root, "donor", "donor", 0, Json.Parse("{\"space\":\"field.points\",\"position\":0}"));
                        original.Get("components").Get("point_stock").Set("initial", JsonValue.Number("0")); original.Get("components").Get("point_stock").Set("public", JsonValue.Of(false));
                        parts.Get("point_stock").Set("initial", JsonValue.Number("0")); parts.Get("point_stock").Set("public", JsonValue.Of(false));
                        root.Get("entities")[2].Get("components").Get("worker").Set("roles", Json.Parse("[\"haul\"]"));
                        root.Set("target_resource_requirements", Json.Parse("""[{"id":"reference.materials","port":null,"use":"consume","quantities":{"supply":2}}]"""));
                        var activity = root.Get("activities")[0]; activity.Set("automatic_supply", JsonValue.Of(true));
                        activity.Set("nodes", Json.Parse("""
                        [
                          {"id":"finish","after":["check","calibrate"],"executor":"work.timed","duration":{"numerator":1,"denominator":1,"unit":"minute"}},
                          {"id":"check","after":["prepare"],"executor":"work.staffed","roles":["inspect"],"duration":{"numerator":4,"denominator":1,"unit":"minute"},"requirement":"reference.materials"},
                          {"id":"calibrate","after":["prepare"],"executor":"work.timed","duration":{"numerator":3,"denominator":1,"unit":"minute"}},
                          {"id":"prepare","after":[],"executor":"work.timed","duration":{"numerator":2,"denominator":1,"unit":"minute"}}
                        ]
                        """));
                        var later = next.Clone(); later.Set("id", JsonValue.Of("replacement.next")); root.Get("entities").Add(later);
                        root.Get("entity_replacement_effects").Add(Json.Parse("""{"id":"reference.upgrade.next","source":"replacement","target":"replacement.next"}"""));
                        root.Get("activities").Add(Json.Parse("""{"id":"upgrade.next","player_orderable":true,"automatic_supply":false,"priority":1,"target_tags":[],"on_complete":"reference.upgrade.next","nodes":[{"id":"upgrade","executor":"work.staffed","roles":["inspect"],"duration":{"numerator":3,"denominator":1,"unit":"minute"}}]}"""));
                    }
                });
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest
        { Scenario = "points", Seed = 31, Seats = new List<Controller> { Controller.Human, Controller.Human }, Mods = { ResourceFlowsModule.ModuleId } });
        private static EntityId Find(SimGame game, string definition) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == definition).Id;
        private static List<PlanView> Plans(SimGame game, int seat = 0)
        { var plans = new List<PlanView>(); game.Seat(seat).View.Feature<WorkView>().CopyPlans(plans); return plans; }
        private static void Send(Check c, SimGame game, Command command)
        {
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary command admission");
            int sequence = game.Seat(0).Submit(command); game.Update(); long cursor = 0;
            var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
            c.That(answers.Any(a => a.Sequence == sequence && a.Accepted), "intent commits through the sole engine inbox");
        }
        private static PlanIntent Intent(SimGame game, bool worker, bool custom, long prerequisite = 0)
        {
            var old = Find(game, worker ? "machine" : "source"); var site = worker ? Find(game, "destination") : old;
            JsonValue arguments;
            if (custom)
                arguments = Json.Parse("{\"site\":" + site.Value + ",\"operator\":" + (worker ? old.Value : 0) + ",\"marker\":" + old.Value + "}");
            else
                arguments = ActivityWorkerSelection.CreateArguments(site, worker
                    ? new ActivityWorkerSelection(new[] { new ActivityWorkerBinding("check", old) }) : ActivityWorkerSelection.Unbound);
            return new PlanIntent(custom ? "reference-plan:inspect" : "activity:inspect", arguments, "field", 71, true,
                prerequisite == 0 ? null : new[] { prerequisite });
        }
        private static int Number(JsonValue arguments, string key) => int.Parse(arguments.Get(key).Text, CultureInfo.InvariantCulture);
        private static void Run(Check c)
        {
            foreach (bool worker in new[] { false, true }) foreach (bool custom in new[] { false, true })
            {
                var factory = Factory(worker); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
                Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true });
                Send(c, game, new SubmitPlanCommand { Intent = new PlanIntent("activity:inspect",
                    ActivityWorkerSelection.CreateArguments(Find(game, "parking"), ActivityWorkerSelection.Unbound), "field", 33, true) });
                long prerequisite = Plans(game).Single().Id;
                Send(c, game, new SubmitPlanCommand { Intent = Intent(game, worker, custom, prerequisite) });
                var original = Plans(game).Single(p => p.Id != prerequisite); var old = Find(game, worker ? "machine" : "source");
                c.That(original.State == PlanState.Pending && original.Paused && original.Order == 0, "paused plan records intent without earned work");
                c.That(Plans(game, 1).Count == 0, "pending JSON intent remains company-private");
                var upgrade = new PostOrderCommand { Plan = "upgrade", Target = old }; ulong before = game.Hash();
                c.Equal(Code.None, game.Seat(0).Preview(upgrade), "compatible pending references admit physical upgrade"); c.Equal(before, game.Hash(), "projected reference admission is pure");
                Send(c, game, upgrade); var twin = factory.Load(game.Save()); int steps = 0;
                while (!game.Seat(0).Snapshot.Entities.Any(e => e.Def == "replacement") && steps++ < 30)
                { game.Update(); twin.Update(); if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("plan replacement fault: " + game.Fault + "/" + twin.Fault); }
                if (!game.Seat(0).Snapshot.Entities.Any(e => e.Def == "replacement"))
                {
                    var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
                    var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                    throw new InvalidOperationException("replacement stalled worker=" + worker + ",custom=" + custom + ";orders=" +
                        string.Join(";", orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker + "/" + o.NodesDone)) + ";jobs=" +
                        string.Join(";", jobs.Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/" + j.ToilIndex)));
                }
                var made = Find(game, "replacement"); var rebound = Plans(game).Single(p => p.Id == original.Id);
                c.That(made != old && !game.Seat(0).Snapshot.Entities.Any(e => e.Id == old), "real staffed upgrade retires the original entity");
                c.That(rebound.Id == original.Id && rebound.CreatedTick == original.CreatedTick && rebound.Group == original.Group && rebound.Priority == original.Priority &&
                    rebound.Paused && rebound.State == PlanState.Pending && rebound.Order == 0 && rebound.Dependencies.SequenceEqual(original.Dependencies),
                    "same plan retains age group priority pause dependency and unearned execution state");
                if (custom)
                {
                    c.Equal(worker ? Find(game, "destination").Value : made.Value, Number(rebound.Arguments, "site"), "mod owns target schema mapping");
                    c.Equal(worker ? made.Value : 0, Number(rebound.Arguments, "operator"), "mod owns selected-worker schema mapping");
                    c.Equal(old.Value, Number(rebound.Arguments, "marker"), "unrelated numeric data equal to retired ID is never rewritten");
                    c.Equal(old.Value, Number(original.Arguments, "marker"), "retained detached JSON remains factual");
                }
                else
                {
                    c.Equal(worker ? Find(game, "destination").Value : made.Value, Number(rebound.Arguments, "target"), "native target mapping");
                    if (worker) c.Equal(made.Value, Number(rebound.Arguments.Get("workers")[0], "worker"), "selected worker identity is retained explicitly, not unbound");
                    c.Equal(worker ? Find(game, "destination").Value : old.Value, Number(original.Arguments, "target"), "old detached intent is not mutated");
                }
                c.That(rebound.Arguments.Frozen, "rebinding persists frozen detached provider arguments");
                c.Equal(game.Hash(), twin.Hash(), "saved pending twins rebind identically");
                game = factory.Load(game.Save()); twin = factory.Load(twin.Save()); c.Equal(game.Hash(), twin.Hash(), "mapped plan persists through original codec");
                foreach (var session in new[] { game, twin })
                {
                    Send(c, session, new SetHomeCommand { Worker = Find(session, "crew"), Home = Find(session, "parking") });
                    if (worker) Send(c, session, new SetWorkerHeldCommand { Worker = Find(session, "replacement"), Held = false });
                    Send(c, session, new ConfigurePlanCommand { Plan = prerequisite, Group = "field", Priority = 33, Paused = false });
                    Send(c, session, new ConfigurePlanCommand { Plan = original.Id, Group = "field", Priority = 71, Paused = false });
                }
                steps = 0;
                while (Plans(game).Single(p => p.Id == original.Id).State != PlanState.Done && steps++ < 160)
                { game.Update(); twin.Update(); if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("rebound plan execution fault: " + game.Fault + "/" + twin.Fault); }
                var done = Plans(game).Single(p => p.Id == original.Id);
                c.Equal(PlanState.Done, done.State, "rebound intent completes through ordinary timed work, blocker=" + done.Blocker);
                c.That(Plans(game).Single(p => p.Id == prerequisite).State == PlanState.Done && done.EndedTick > rebound.CreatedTick, "prior dependency and physical duration are earned");
                c.Equal(game.Hash(), twin.Hash(), "productive saved continuation remains deterministic");
                c.Equal(isolatedHash, isolated.Hash(), "interleaved unused session stays unchanged");
            }
            foreach (string refusal in new[] { "target", "worker", "deny" })
            {
                bool worker = refusal == "worker"; var factory = Factory(worker, refusal == "deny" ? "deny" : "normal", refusal);
                var game = New(factory); Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true });
                Send(c, game, new SubmitPlanCommand { Intent = Intent(game, worker, refusal == "deny") });
                var old = Find(game, worker ? "machine" : "source"); var command = new PostOrderCommand { Plan = "upgrade", Target = old };
                Code expected = refusal == "target" ? StandardCodes.WrongTarget : refusal == "worker" ? WorkSystem.BlockerNoWorker : new Code("proof.plan-replacement-denied");
                ulong before = game.Hash(); c.Equal(expected, game.Seat(0).Preview(command), "pending plan's typed " + refusal + " refusal"); c.Equal(before, game.Hash(), "refused preview changes no plans");
                int sequence = game.Seat(0).Submit(command); game.Update(); long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
                c.That(answers.Any(a => a.Sequence == sequence && a.Refusal == expected), "inbox and preview refuse identically");
                c.That(game.Fault == null && !game.Seat(0).Snapshot.Entities.Any(e => e.Def == "replacement"), "refusal grants no replacement");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused pending intent remains a coherent checkpoint");
            }
            {
                var factory = Factory(false, "omit-rebind"); var game = New(factory);
                Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true });
                Send(c, game, new SubmitPlanCommand { Intent = Intent(game, false, true) });
                c.Throws<InvalidOperationException>(() => game.Seat(0).Preview(new PostOrderCommand { Plan = "upgrade", Target = Find(game, "source") }),
                    "provider that omits required rewrite is rejected before physical mutation");
                c.That(game.Fault != null, "incorrect plan schema contract faults the authoritative observation gateway");
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted schema has no healthy checkpoint");
            }
        }
        private static void Supplied(Check c)
        {
            Supplied(c, false, false); Supplied(c, true, false); Supplied(c, false, true);
        }
        private static void Supplied(Check c, bool custom, bool cancel)
        {
            string context = (custom ? "mod" : "native") + (cancel ? "/cancel" : "/complete");
            var factory = Factory(false, supplied: true); var game = New(factory); SimGame twin = null;
            var isolated = New(factory); ulong untouched = isolated.Hash(); var old = Find(game, "source"); var carrier = Find(game, "machine");
            if (!game.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("DAG supply missing");
            List<LogisticsDemandView> Demands(SimGame session)
            { var rows = new List<LogisticsDemandView>(); session.Seat(0).View.Feature<LogisticsView>().CopyDemands(rows); return rows; }
            List<WorkJobView> Jobs(SimGame session)
            { var rows = new List<WorkJobView>(); session.Seat(0).View.Feature<WorkView>().CopyJobs(rows); return rows; }
            ResourceFlowSummary Summary(SimGame session) => session.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            BigInteger Consumed(SimGame session) => Summary(session).Aggregates.Where(row => row.Resource == resource && row.Kind == ResourceFlowKind.Consumed)
                .Aggregate(BigInteger.Zero, (amount, row) => amount + row.AmountRaw);
            void Observe()
            {
                Fix actual = Fix.Zero;
                foreach (var entity in game.Seat(0).Snapshot.Entities)
                    if (game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, resource, out var balance).IsNone) actual += balance.Stock;
                var summary = Summary(game); c.Equal(ResourceFlowReadStatus.Ready, summary.Status, "actual cumulative history remains available despite bounded-window eviction " + context);
                c.Equal(new BigInteger(Fix.FromInt(20).Raw), new BigInteger(actual.Raw) + Consumed(game),
                    "every update accounts for actual stock/cargo and committed consumption, not invented WIP " + context);
                if (twin != null) c.Equal(game.Hash(), twin.Hash(), "saved DAG/supply sessions remain identical each update " + context);
            }
            void StepBoth() { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("supplied plan fault " + context + ": " + game.Fault + "/" + twin?.Fault); Observe(); }
            void Reach(Func<bool> ready, int maximum, string stage)
            {
                for (int i = 0; i < maximum && !ready(); i++) StepBoth();
                if (!ready()) throw new InvalidOperationException(stage + "/" + context + " exceeded " + maximum + " updates at " + game.Tick + "; jobs=" +
                    string.Join(";", Jobs(game).Select(job => job.Id + "/" + job.State + "/" + job.ToilIndex + "/" + job.Loaded + "/" + job.Blocker)));
            }
            void SendBoth(Command command) { Send(c, game, command); if (twin != null) Send(c, twin, command); Observe(); }
            void Refuse(Command command, Code expected)
            {
                ulong before = game.Hash(); c.Equal(expected, game.Seat(0).Preview(command), "physical workflow guard has original typed refusal " + context);
                c.Equal(before, game.Hash(), "refusal preview cannot mutate progress, materials or demand " + context);
                int sequence = game.Seat(0).Submit(command); twin?.Seat(0).Submit(command); StepBoth(); long cursor = 0; var answers = new List<CommandResult>();
                game.Seat(0).ReadResults(ref cursor, answers);
                c.That(answers.Any(answer => answer.Sequence == sequence && answer.Refusal == expected), "inbox and preview agree without replacing active work " + context);
                c.That(!game.Seat(0).Snapshot.Entities.Any(entity => entity.Def == "replacement.next"), "refusal publishes no completed replacement " + context);
            }
            SendBoth(new SetWorkerHeldCommand { Worker = carrier, Held = true });
            SendBoth(new SubmitPlanCommand { Intent = Intent(game, false, custom) });
            var pending = Plans(game).Single();
            c.That(pending.Paused && pending.State == PlanState.Pending && pending.Order == 0 && Demands(game).Count == 0,
                "pending DAG is pure intent, not an active supply owner or free construction " + context);
            SendBoth(new PostOrderCommand { Target = old, Plan = "upgrade" }); twin = factory.Load(game.Save());
            Reach(() => game.Seat(0).Snapshot.Entities.Any(entity => entity.Def == "replacement"), 30, "staffed pending-site replacement");
            var made = Find(game, "replacement"); var rebound = Plans(game).Single();
            c.That(rebound.Id == pending.Id && rebound.CreatedTick == pending.CreatedTick && rebound.Group == pending.Group && rebound.Priority == pending.Priority && rebound.Paused && rebound.Order == 0 &&
                Number(rebound.Arguments, custom ? "site" : "target") == made.Value, "same unearned plan retains identity/age/intent and actual rebound site " + context);
            if (custom) c.Equal(old.Value, Number(rebound.Arguments, "marker"), "unrelated mod numeric data is not a reference " + context);
            game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
            SendBoth(new SetHomeCommand { Worker = Find(game, "crew"), Home = Find(game, "parking") });
            SendBoth(new SetHomeCommand { Worker = carrier, Home = Find(game, "donor") });
            Reach(() => game.Seat(0).View.Feature<WorkView>().ReadHome(Find(game, "crew"), out var home).IsNone && home.State == WorkerHomeState.AtHome, 180, "upgrade crew clears material approach");
            SendBoth(new ConfigurePlanCommand { Plan = pending.Id, Group = pending.Group, Priority = pending.Priority, Paused = false });
            Reach(() => Demands(game).Any(demand => demand.Source.Scope == WorkModule.ModuleId), 30, "active DAG creates its own physical supply request");
            var active = Plans(game).Single(); int order = active.Order; var demand = Demands(game).Single();
            List<ActivityNodeView> Nodes(SimGame session)
            { var nodes = new List<ActivityNodeView>(); if (!session.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes)) throw new InvalidOperationException("active DAG nodes missing"); return nodes; }
            c.That(order > 0 && active.Id == pending.Id && demand.Source.Identity == order && demand.Source.Entry.IsNone && demand.Destination == made &&
                demand.Endpoint == ResourceEndpointId.ForEntity(made) && demand.Quantity == Fix.FromInt(2), "one aggregate demand belongs to the real new order/site, never retired IDs " + context);
            c.That(Nodes(game).Single(node => node.Id == "prepare").State == NodeState.Done && Nodes(game).Single(node => node.Id == "check").Progress.WholeTicks == 0,
                "DAG earns preparation but material-gated machine work earns nothing early " + context);
            game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
            Refuse(new PostOrderCommand { Target = made, Plan = "upgrade.next" }, WorkBoard.ReplacementActiveExecution);
            c.That(Demands(game).Single().Source.Equals(demand.Source) && Demands(game).Single().CreatedTick == demand.CreatedTick,
                "active-order refusal preserves existing supply identity and starvation age " + context);
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            if (cancel)
            {
                game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
                using var cancelledReplay = game.RecordReplay(limits);
                SendBoth(new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { pending.Id }) });
                c.That(Plans(game).Single().State == PlanState.Cancelled && Demands(game).Count == 0 && !Jobs(game).Any(), "normal cancellation withdraws only the real owned workflow requests " + context);
                SendBoth(new PostOrderCommand { Target = made, Plan = "upgrade.next" });
                Reach(() => game.Seat(0).Snapshot.Entities.Any(entity => entity.Def == "replacement.next"), 180, "cancelled workflow permits earned later replacement");
                c.Equal(BigInteger.Zero, Consumed(game), "unstarted cancelled material work consumes nothing " + context);
                c.Equal(2L, cancelledReplay.CommandCount, "replay records only real cancel and later upgrade intents " + context);
                var verified = new ReplayVerifier().Verify(factory, cancelledReplay.Finish(), limits);
                c.That(verified.Success && verified.ActualHash == game.Hash(), "actual cancellation and later physical replacement replay " + context);
            }
            else
            {
                SendBoth(new SetWorkerHeldCommand { Worker = carrier, Held = false });
                bool Loaded()
                {
                    var at = (PointSnapshot)game.Seat(0).Snapshot.Entities.Single(entity => entity.Id == carrier).Spatial;
                    return Jobs(game).Any(job => job.IsHaul && job.HaulDemand.Equals(demand.Source) && job.Loaded == Fix.FromInt(2) && job.ToilIndex == 2) && at.Position > Fix.One && at.Position < Fix.FromInt(19);
                }
                Reach(Loaded, 180, "order supply actually loads and travels");
                var loaded = Jobs(game).Single(job => job.IsHaul); game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
                c.That(Loaded() && Jobs(game).Any(job => job.Id == loaded.Id && job.Destination == made && job.HaulDemand.Equals(demand.Source)), "loaded save preserves the same original job/order/site binding " + context);
                Refuse(new PostOrderCommand { Target = made, Plan = "upgrade.next" }, Upgrades.InventoryReserved);
                game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
                using (var loadedReplay = game.RecordReplay(limits))
                {
                    Reach(() => Nodes(game).Single(node => node.Id == "check").Progress.WholeTicks > 0 && Nodes(game).Single(node => node.Id == "check").Progress.WholeTicks < 4, 180, "same supplied material node earns partial staffed work");
                    c.Equal(0L, loadedReplay.CommandCount, "saved loaded demand needs no recreated order or haul " + context);
                    var verified = new ReplayVerifier().Verify(factory, loadedReplay.Finish(), limits);
                    c.That(verified.Success && verified.ActualHash == game.Hash(), "actual loaded workflow supply replay reaches partial work " + context);
                }
                var partial = Nodes(game).Single(node => node.Id == "check"); game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
                var restored = Nodes(game).Single(node => node.Id == "check");
                c.That(restored.Progress.Equals(partial.Progress) && restored.JobId == partial.JobId && restored.Resources.Count == 1 &&
                    restored.Resources[0].Choice.Endpoint == ResourceEndpointId.ForEntity(made) && restored.Resources[0].Choice.Quantities.AmountOf(resource) == Fix.FromInt(2),
                    "partial-work save retains exact earned progress/job/material selection " + context);
                using (var workReplay = game.RecordReplay(limits))
                {
                    Reach(() => Plans(game).Single().State == PlanState.Done, 180, "all authored sequential and parallel DAG branches settle");
                    c.Equal(new BigInteger(Fix.FromInt(2).Raw), Consumed(game), "DAG consumes the physically delivered two once " + context);
                    c.That(Demands(game).Count == 0, "completed work withdraws its original supply envelope " + context);
                    SendBoth(new PostOrderCommand { Target = made, Plan = "upgrade.next" });
                    Reach(() => game.Seat(0).Snapshot.Entities.Any(entity => entity.Def == "replacement.next"), 180, "completed workflow permits earned later replacement");
                    c.Equal(1L, workReplay.CommandCount, "only later real upgrade is recorded, never duplicate work " + context);
                    var verified = new ReplayVerifier().Verify(factory, workReplay.Finish(), limits);
                    c.That(verified.Success && verified.ActualHash == game.Hash(), "actual partial DAG completion and later replacement replay " + context);
                }
            }
            var terminal = Plans(game).Single();
            if (!cancel) c.That(Summary(game).EvictedThroughTick >= 0 && Consumed(game) == new BigInteger(Fix.FromInt(2).Raw),
                "real two-record-window eviction cannot erase cumulative once-only consumption " + context);
            c.That(terminal.Id == pending.Id && terminal.Order == order && terminal.State == (cancel ? PlanState.Cancelled : PlanState.Done) &&
                Number(terminal.Arguments, custom ? "site" : "target") == made.Value, "terminal history remains factual after its earned target is retired " + context);
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal graph/demand/reference state remains loadable " + context);
            c.That(Plans(game, 1).Count == 0, "supplied workflow history remains company-private " + context);
            c.Equal(untouched, isolated.Hash(), "supplied workflow changes no independent session " + context);
        }
    }
}
