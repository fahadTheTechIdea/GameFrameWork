using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class LogisticsDispatchTests
    {
        public static void Register()
        {
            TestRunner.Add("Logistics dispatch: native route admission, private queue and deterministic intent ordering", Admission);
            TestRunner.Add("Logistics dispatch: contested physical carriers, refreshed offers and saved route policy", Physical);
        }
        private const string Declaration = "{\"id\":\"proof.demands\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"demands.json\"]}";
        private static GameFactory Factory(int carriers = 1, bool reverse = false)
        {
            JsonValue root = Content.Json.Parse(PointGame.Definitions);
            root.Get("entities")[2].Get("components").Set("work_policy", Content.Json.Parse("{\"profile\":\"dispatch.machine\"}"));
            foreach (var extra in new[] { ("near", 4), ("far", 25), ("blocked", 15) })
            {
                JsonValue entity = root.Get("entities")[1].Clone(); entity.Set("id", JsonValue.Of(extra.Item1));
                entity.Get("components").Get("point_presence").Set("position", JsonValue.Number(extra.Item2.ToString()));
                root.Get("entities").Add(entity);
            }
            // Authored creation order is independently variable; dispatch cannot depend on it.
            foreach (string id in reverse ? new[] { "far", "near", "blocked" } : new[] { "near", "far", "blocked" })
                StartupContentAuthoring.Add(root, id, id, 0, Content.Json.Parse("{\"space\":\"field.points\",\"position\":" + (id == "near" ? 4 : id == "far" ? 25 : 15) + "}"));
            for (int i = 1; i < carriers; i++)
            {
                string id = "machine" + i; JsonValue entity = root.Get("entities")[2].Clone(); entity.Set("id", JsonValue.Of(id));
                entity.Get("components").Get("point_presence").Set("position", JsonValue.Number((i * 20).ToString()));
                root.Get("entities").Add(entity);
                StartupContentAuthoring.Add(root, id, id, 0, entity.Get("components").Get("point_presence"));
            }
            var text = new StringBuilder(); root.WriteCanonical(text);
            return PointGame.Factory(text.ToString(), FrameworkProofContent.Read("logistics-dispatch.json"),
                new DemandProofModule(), Declaration, FrameworkProofContent.Read("logistics-demands.json"));
        }
        private static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "points", Seed = 283 }; request.Mods.Add("proof.demands"); return factory.NewGame(request); }
        private static Entity Find(SimGame game, string id)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException("missing dispatch entity " + id); }
        private static ResourceId Resource(SimGame game) => game.World.Content.ResourceId("supply");
        private static LogisticsService Log(SimGame game) => game.World.Services.Get<LogisticsService>();
        private static LogisticsRouteSettings Route(bool enabled, int batch, int priority) => new LogisticsRouteSettings(enabled, Fix.FromInt(batch), priority);
        private static List<LogisticsDispatchOffer> Queue(SimGame game)
        { var offers = new List<LogisticsDispatchOffer>(); game.Seat(0).View.Feature<LogisticsView>().CopyQueue(offers); return offers; }
        private static void Enable(SimGame game)
        {
            foreach (Entity entity in game.World.Entities.All)
            {
                IWorkPolicy policy = entity.Get<IWorkPolicy>(); if (policy == null) continue;
                WorkPolicySettings current = policy.ReadPolicy();
                policy.Configure(game.World, new WorkPolicySettings(true, current.Priority, current.AllowedRoles,
                    current.Area, current.Interruptible, current.EmergencyEligible, WorkAssignmentMode.All));
            }
        }
        private static void Stock(Check c, SimGame game, string target, int priority, int desired = 8, int minimum = 1, int maximum = 40)
            => c.Equal(Code.None, game.World.Services.Get<ILogisticsStockPolicies>().ConfigureStockPolicy(new PlayerId(0), Find(game, target).Id,
                Resource(game), LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(minimum), Fix.FromInt(desired), Fix.FromInt(maximum), priority, LogisticsReserveAccess.Protected))), "engine installs native stock intent");
        private static int Add(SimGame game, string target, int priority, bool enabled = true)
            => Log(game).AddRoute(new PlayerId(0), Find(game, "source").Id, Find(game, target).Id, Resource(game), Route(enabled, 8, priority));
        private static Job Live(SimGame game, EntityId destination)
        { foreach (Job job in game.World.Services.Get<WorkBoard>().Jobs) if (job.IsLive && job.IsHaul && job.To == destination) return job; return null; }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null, SimGame isolated = null)
        {
            for (int i = 0; i < 320 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); if (isolated != null) Tick(isolated); }
            if (!done())
            {
                var facts = new List<string>(); foreach (Job job in game.World.Services.Get<WorkBoard>().Jobs) facts.Add(job.Id + "/" + job.State + "/" + job.To + "/" + job.Blocker);
                throw new InvalidOperationException(stage + " timeout at " + game.Tick + ": " + string.Join(";", facts));
            }
        }
        private static void Admission(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity source = Find(game, "source"), near = Find(game, "near");
            ResourceId supply = Resource(game); LogisticsService log = Log(game);
            c.That(ReferenceEquals(log, game.World.Services.Get<ILogisticsDispatchQueue>()), "queue aliases one logistics matching authority");
            ulong before = game.Hash();
            c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(new AddRouteCommand { From = source.Id, To = near.Id, Resource = supply }), "route has no inferred settings");
            c.Equal(Codes.InvalidArgument, log.CheckRoute(source.Owner, source.Id, near.Id, supply, new LogisticsRouteSettings(true, Fix.Half, 5)), "route batch respects native count quantum");
            c.Equal(Codes.NotFound, log.CheckRoute(new PlayerId(1), source.Id, near.Id, supply, Route(true, 8, 5)), "route ownership is actual endpoint ownership");
            c.Equal(Codes.NotFound, log.CheckRoute(source.Owner, source.Id, source.Id, supply, Route(true, 8, 5)), "self route refuses");
            c.Equal(Codes.InvalidArgument, log.CheckRoute(source.Owner, source.Id, near.Id, new ResourceId(int.MaxValue), Route(true, 8, 5)), "unknown route resource refuses");
            c.Throws<ArgumentException>(() => Route(true, 0, 1), "zero batch refuses");
            c.Throws<ArgumentException>(() => Route(true, 8, -1), "negative priority refuses");
            c.Equal(before, game.Hash(), "all rejected route previews are pure");
            var result = TestKit.Order(game, 0, new AddRouteCommand { From = source.Id, To = near.Id, Resource = supply, Settings = Route(true, 8, 5) });
            c.That(result.Accepted, "ordinary player creates explicit route"); int route = checked((int)result.Arg0);
            c.Equal(1, route, "refusals did not allocate route identity");
            var facts = new List<RouteView>(); game.Seat(0).View.Feature<LogisticsView>().CopyRoutes(facts); RouteView retained = facts[0];
            c.Equal(5, retained.Settings.Priority, "private detached route settings reflect command");
            c.Equal(HaulStatus.PolicyBlocked, Queue(game)[0].Haul.Status, "disabled real machine yields wait, not fake carrier");
            c.Equal(WorkPolicyCodes.Disabled, Queue(game)[0].Haul.Blocker, "queue exposes shared typed worker policy refusal");
            c.That(TestKit.Order(game, 0, new ConfigureRouteCommand { Route = route, Settings = Route(false, 16, int.MaxValue) }).Accepted, "route settings edit through player command");
            c.Equal(retained.CreatedTick, log.FindRoute(route).Value.CreatedTick, "route edits retain original age");
            c.Equal(5, retained.Settings.Priority, "retained route settings cannot mutate");
            c.Equal(LogisticsService.RouteDisabled, Queue(game)[0].Haul.Blocker, "disabled route keeps truthful queue fact");
            c.Equal(Codes.NotFound, log.CheckRouteSettings(new PlayerId(1), route, Route(true, 8, 1)), "foreign route edit refuses");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(new ConfigureRouteCommand { Route = int.MaxValue, Settings = Route(true, 8, 1) }), "missing route has no substitute");
            c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(new ConfigureRouteCommand { Route = route }), "missing edit settings refuse");
            var hidden = new List<LogisticsDispatchOffer>(Queue(game)); before = game.Hash();
            PlayerView.Create(game.World, new PlayerId(1)).Feature<LogisticsView>().CopyQueue(hidden);
            c.Equal(0, hidden.Count, "foreign queue clears all operational facts");
            PlayerView.Create(game.World, new PlayerId(1)).Feature<LogisticsView>().CopyRoutes(facts);
            c.Equal(0, facts.Count, "foreign route settings and age stay private");
            c.Equal(before, game.Hash(), "private projections do not advance planner or refill state");
            c.Equal(32, SaveGame.Describe(game.Save()).Format, "route policy and age use new native layout");
            c.Equal(StateCodecTests.ExpectedLogisticsSchema, log.StateSchemaVersion, "same service codec owns route settings");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "disabled edited route survives same-owner save/load");
            CorruptRoutes(c, game);
            Ordering(c);
        }
        private static void Ordering(Check c)
        {
            foreach (bool reverse in new[] { false, true })
            {
                SimGame game = New(Factory(reverse: reverse)); Stock(c, game, "far", 1); Stock(c, game, "near", 100);
                int route = Add(game, "destination", 80); Add(game, "blocked", int.MaxValue);
                Stock(c, game, "blocked", 0, desired: 0, minimum: 0, maximum: 0); Enable(game);
                ulong before = game.Hash(); var queue = Queue(game);
                c.Equal(Find(game, "near").Id, queue[0].Destination, "priority wins irrespective of authored/insertion order");
                c.Equal(route, queue[1].Route, "route and stock demand compete in one ranked queue");
                c.Equal(Find(game, "far").Id, queue[2].Destination, "low priority demand cannot grab carrier first");
                c.Equal(HaulStatus.PolicyBlocked, queue[3].Haul.Status, "blocked maximum-priority route ranks after real ready intent");
                c.Equal(LogisticsPolicyCodes.Maximum, queue[3].Haul.Blocker, "receiving ceiling diagnostic uses common admission");
                c.Equal((long?)Fix.FromInt(4).Raw, queue[0].Haul.TravelCost, "pickup plus delivery cost comes from selected non-tile provider");
                c.Equal(before, game.Hash(), "derived queue allocates no execution or receipt");
                c.Equal(0, game.World.Services.Get<WorkBoard>().Jobs.Count, "ready offers are not jobs");
            }
            SimGame costs = New(Factory()); Stock(c, costs, "far", 50); Stock(c, costs, "near", 50); Enable(costs);
            c.Equal(Find(costs, "near").Id, Queue(costs)[0].Destination, "equal priority/age uses real complete travel cost before identity");
            Tick(costs); // the older near intent now owns a real carrier
            Stock(c, costs, "destination", 50); Enable(costs);
            c.Equal(0L, Queue(costs)[0].CreatedTick, "older still-pending same-priority intent retains age despite another acquisition");
            SimGame tie = New(Factory()); Stock(c, tie, "near", 50); int routeTie = Add(tie, "near", 50); Enable(tie);
            c.Equal(routeTie, Queue(tie)[0].Route, "exact same cost/age/target/resource tie uses stable intent scope/route identity");
            Tick(tie); c.Equal(routeTie, Live(tie, Find(tie, "near").Id).Route, "stable tie predicts actual winning route without duplicate target commitment");
            // Quiet maximum-priority stock is not a contributor to a mod's active delivery cycle.
            SimGame mod = New(Factory()); Stock(c, mod, "destination", int.MaxValue, 8, 0);
            DemandProofDef def = mod.World.Services.Get<DemandProofOwner>().Definition;
            c.Equal(Code.None, mod.World.Services.Get<ILogisticsDemandRequests>().ReplaceRequests(def.Source, def.Company, Find(mod, "destination").Id, new[] { LogisticsSupplyRequest.Entity(Find(mod, "destination").Id, def.Quantities) }, def.Priority), "JSON mod requests through declared owner contract");
            Enable(mod); var offer = Queue(mod)[0];
            c.Equal(def.Source, offer.Source, "actual contributing mod source controls aggregate offer");
            c.Equal(65, offer.Priority, "quiet stock does not promote unrelated work to its priority");
            c.Equal(Fix.FromInt(8), offer.Requested, "quiet band contributes no fictional delivery target");
            mod.World.Services.Get<IDemandProof>().Retire();
            c.Equal(0, Queue(mod).Count, "retired source disappears immediately without query pruning/mutation");
        }
        private static void CorruptRoutes(Check c, SimGame game)
        {
            byte[] state;
            using (var stream = new MemoryStream()) { var writer = new StateWriter(stream); Log(game).Sync(writer); writer.Flush(); state = stream.ToArray(); }
            // next ID, demand count, route count, route ID, company/from/to/resource, enabled/batch/priority, age.
            foreach (var corrupt in new[] { (12, BitConverter.GetBytes(0)), (45, BitConverter.GetBytes(-1L)),
                (45, BitConverter.GetBytes(checked(game.Tick + 1))), (0, BitConverter.GetBytes(1)), (41, BitConverter.GetBytes(-1)), (33, BitConverter.GetBytes(Fix.Half.Raw)) })
            {
                byte[] copy = (byte[])state.Clone(); Array.Copy(corrupt.Item2, 0, copy, corrupt.Item1, corrupt.Item2.Length);
                c.Throws<CorruptStateException>(() => new LogisticsService(game.World).Sync(new StateReader(copy, 0, copy.Length)), "malformed route identity/age/counter refuses sole codec");
            }
            byte[] duplicate = new byte[state.Length + state.Length - 12];
            Array.Copy(state, duplicate, state.Length); Array.Copy(state, 12, duplicate, state.Length, state.Length - 12);
            Array.Copy(BitConverter.GetBytes(2), 0, duplicate, 8, 4);
            c.Throws<CorruptStateException>(() => new LogisticsService(game.World).Sync(new StateReader(duplicate, 0, duplicate.Length)), "duplicate route identity cannot create parallel execution intent on restore");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Stock(c, game, "far", 1); Stock(c, game, "near", 100);
            Stock(c, game, "destination", 0, desired: 8, minimum: 0);
            int route = Add(game, "destination", 80); Enable(game); var first = Queue(game)[0]; Tick(game);
            Job selected = Live(game, Find(game, "near").Id);
            c.That(selected != null && selected.Worker == first.Haul.Carrier, "actual first acquisition agrees with private ranked carrier offer");
            c.Equal(100, selected.Priority, "haul uses explicit current intent priority");
            c.That(Live(game, Find(game, "far").Id) == null && Live(game, Find(game, "destination").Id) == null, "last carrier was not pre-acquired by earlier low-priority request or route");
            c.Equal(Fix.FromInt(8), Find(game, "near").Get<IInletPort>().ReservedIn(Resource(game)), "winning destination owns real inbound receipt");
            c.Equal(Fix.Zero, Find(game, "near").Get<IInletPort>().Amount(Resource(game)), "ready/assigned promise is not delivered stock");
            Reach(game, () => Find(game, "near").Get<IInletPort>().Amount(Resource(game)) == Fix.FromInt(8), "first physical demand");
            Reach(game, () => Live(game, Find(game, "destination").Id) != null, "higher-priority standing route");
            Job routeJob = Live(game, Find(game, "destination").Id); c.Equal(route, routeJob.Route, "route competes before lower-priority remaining demand");
            Reach(game, () => routeJob.Loaded.IsPositive, "route physical pickup");
            c.That(TestKit.Order(game, 0, new ConfigureRouteCommand { Route = route, Settings = Route(false, 16, 2) }).Accepted, "player disables live loaded route without cancelling actual haul");
            c.That(routeJob.IsLive && routeJob.CargoOutReservation.IsValid && routeJob.InReservation.IsValid, "route edit preserves acquired real receipts and cargo");
            SimGame twin = factory.Load(game.Save()), isolated = New(factory);
            c.Equal(game.Hash(), twin.Hash(), "partial loaded route and edited settings restore identically");
            Reach(game, () => Find(game, "destination").Get<IInletPort>().Amount(Resource(game)) == Fix.FromInt(8), "disabled loaded route still delivers", twin, isolated);
            c.Equal(game.Hash(), twin.Hash(), "saved twin advances identical loaded physical route");
            c.Equal(Fix.Zero, Find(isolated, "destination").Get<IInletPort>().Amount(Resource(isolated)), "interleaved session borrows no stock/intent/carrier");
            Reach(game, () => Find(game, "far").Get<IInletPort>().Amount(Resource(game)) == Fix.FromInt(4), "remaining partial native source stock", twin);
            c.Equal(game.Hash(), twin.Hash(), "lower-priority demand continues against real finite stock");
            c.Equal(Fix.Zero, Find(game, "source").Get<IOutletPort>().Amount(Resource(game)), "three deliveries conserve authored twenty units exactly");
            var queue = Queue(game); c.That(queue.Exists(x => x.Destination == Find(game, "far").Id && x.Haul.Status == HaulStatus.NoSupply), "unsatisfied residual request reports real supply wait");
            MultipleCarriers(c); AgeAndRoutePriority(c); WithdrawnMod(c); UpdatedReadiness(c);
        }
        private static void MultipleCarriers(Check c)
        {
            SimGame game = New(Factory(carriers: 2)); Stock(c, game, "far", 40); Stock(c, game, "near", 50); Enable(game);
            var queue = Queue(game); c.Equal(queue[0].Haul.Carrier, queue[1].Haul.Carrier, "independent pure offers can name same unclaimed machine");
            Tick(game); Job near = Live(game, Find(game, "near").Id), far = Live(game, Find(game, "far").Id);
            c.That(near != null && far != null && near.Worker != far.Worker, "each actual acquisition rebuilds remaining offers after carrier claim");
            c.Equal(Fix.FromInt(16), Find(game, "source").Get<IOutletPort>().ReservedOut(Resource(game)), "competing offers acquire exactly two native outbound promises");
            c.Equal(2, game.World.Services.Get<WorkBoard>().Jobs.Count, "no duplicate job created from stale offers");
            SimGame refill = New(Factory(carriers: 2)); Stock(c, refill, "near", 100, desired: 12); Stock(c, refill, "far", 1); Enable(refill); Tick(refill);
            c.Equal(Fix.FromInt(12), Find(refill, "near").Get<IInletPort>().ReservedIn(Resource(refill)), "priority refill recomputation shrinks second load to four, not another eight");
            c.Equal(Fix.Zero, Find(refill, "far").Get<IInletPort>().ReservedIn(Resource(refill)), "unified priority applies to each carrier acquisition");
        }
        private static void AgeAndRoutePriority(Check c)
        {
            SimGame aged = New(Factory()); Stock(c, aged, "far", 50); Tick(aged); Stock(c, aged, "near", 50); Enable(aged);
            c.Equal(Find(aged, "far").Id, Queue(aged)[0].Destination, "original age beats cheaper newer route within priority");
            Reach(aged, () => Live(aged, Find(aged, "far").Id) != null, "oldest physical request");
            c.That(Live(aged, Find(aged, "near").Id) == null, "younger intent does not steal oldest request's carrier");
            SimGame route = New(Factory()); Stock(c, route, "near", 1); int id = Add(route, "far", int.MaxValue); Enable(route);
            c.Equal(id, Queue(route)[0].Route, "explicit route priority competes ahead of demands without fixed tuning");
            Tick(route); c.Equal(id, Live(route, Find(route, "far").Id).Route, "actual route can win first carrier");
            ulong before = route.Hash();
            c.Equal(0, Log(route).AcquireHaul(new PlayerId(0), Find(route, "source").Id, Find(route, "near").Id, Resource(route), Fix.One, -1), "negative direct priority cannot bypass admission");
            c.Equal(before, route.Hash(), "rejected direct priority allocates nothing");
        }
        private static void WithdrawnMod(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Stock(c, game, "near", 1);
            DemandProofDef def = game.World.Services.Get<DemandProofOwner>().Definition;
            c.Equal(Code.None, game.World.Services.Get<ILogisticsDemandRequests>().ReplaceRequests(def.Source, def.Company, Find(game, "destination").Id, new[] { LogisticsSupplyRequest.Entity(Find(game, "destination").Id, def.Quantities) }, def.Priority), "mod uses normal declared request admission");
            Enable(game); Tick(game); Job job = Live(game, Find(game, "destination").Id);
            c.That(job != null, "JSON mod priority wins physical carrier over ordinary lower request");
            Reach(game, () => job.Loaded.IsPositive, "mod physical pickup");
            game.World.Services.Get<IDemandProof>().Retire(); game.World.Services.Get<ILogisticsDemandRequests>().WithdrawRequests(def.Source);
            c.That(job.IsLive && job.CargoOutReservation.IsValid, "retired mod intent does not revoke live haul ownership");
            SimGame twin = factory.Load(game.Save());
            Reach(game, () => Find(game, "destination").Get<IInletPort>().Amount(Resource(game)) == Fix.FromInt(8), "retired mod delivery", twin);
            c.Equal(game.Hash(), twin.Hash(), "mod retirement and physical commitments survive saved continuation");
        }
        private static void UpdatedReadiness(Check c)
        {
            SimGame game = New(Factory()); Stock(c, game, "near", 50); int route = Add(game, "far", 1); Enable(game);
            LogisticsDispatchOffer retained = Queue(game)[0]; c.Equal(Find(game, "near").Id, retained.Destination, "retained pure offer originally prefers stock intent");
            c.Equal(Code.None, Log(game).ConfigureRoute(new PlayerId(0), route, Route(true, 8, 100)), "engine route edit preflights current native endpoints");
            c.Equal(50, retained.Priority, "retained queue does not mutate when route priority changes");
            Tick(game); Job job = Live(game, Find(game, "far").Id);
            c.That(job != null && job.Route == route && job.Priority == 100, "actual dispatch rebuilds and observes current route priority, not old view");
            c.That(Live(game, Find(game, "near").Id) == null, "stale earlier offer cannot allocate same truck");
            // A route can disappear while its acquired physical shipment remains real.
            Reach(game, () => job.Loaded.IsPositive, "removed route physical pickup");
            c.That(TestKit.Order(game, 0, new RemoveRouteCommand { Route = route }).Accepted, "player removes future route intent");
            c.That(job.IsLive && job.InReservation.IsValid && job.CargoOutReservation.IsValid, "route removal does not cancel or erase loaded shipment");
            Reach(game, () => Find(game, "far").Get<IInletPort>().Amount(Resource(game)) == Fix.FromInt(8), "removed route delivered stock");
            c.That(!Queue(game).Exists(x => x.Route == route), "removed route is absent from future dispatch facts");
        }
    }
}
