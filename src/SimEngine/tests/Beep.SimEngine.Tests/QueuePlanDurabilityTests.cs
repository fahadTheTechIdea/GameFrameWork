using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.LogisticsPolicyContentTests;

namespace Beep.SimEngine.Tests
{
    // Public player inputs, immutable facts and bounded Updates only. No gameplay execution here.
    internal static class QueuePlanDurabilityTests
    {
        internal const string Case = "Physical plan durability: saved dock queues partial handling recovery and paused plans";
        private const string Receiver = "durability.receiver";
        private static readonly ReplayLimits Limits = new ReplayLimits(8 * 1024 * 1024, 5000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static ResourceId Resource(SimGame game, string name)
        { if (!game.Seat(0).View.TryFindResource(name, out var id)) throw new InvalidOperationException("missing authored durability resource " + name); return id; }
        private static LogisticsOperationalSnapshot Facts(SimGame game, string entity, string resource = "steel")
        {
            var result = View(game).ReadDiagnostics(Id(game, entity), Resource(game, resource), out var value);
            if (!result.IsNone) throw new InvalidOperationException("actual durability stock facts " + entity + "/" + result); return value;
        }
        private static TileCoord Position(SimGame game, string entity) =>
            ((TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == Id(game, entity)).Spatial).Tile;
        private static List<PlanView> Plans(SimGame game, int seat = 0)
        { var rows = new List<PlanView>(); game.Seat(seat).View.Feature<WorkView>().CopyPlans(rows); return rows; }
        private static PlanView Plan(SimGame game, long id) => Plans(game).Single(p => p.Id == id);
        private static ActivityNodeView Node(SimGame game, int order)
        {
            var rows = new List<ActivityNodeView>();
            if (!game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, rows)) throw new InvalidOperationException("missing actual plan node " + order);
            return rows.Single();
        }
        private static WorkJobView Haul(SimGame game, int id) => Jobs(game).Single(j => j.Id == id);
        private static int Road(SimGame game)
        {
            var cells = new List<int>();
            if (game.Seat(0).View.Feature<TileMapView>().ReadLayer("road", new TileRect(18, 12, 1, 1), cells) != SpatialLayerReadStatus.Success)
                throw new InvalidOperationException("missing actual road cell"); return cells.Single();
        }
        private static void Integrity(Check c, SimGame game)
        {
            Fix steel = Fix.Zero, fuel = Fix.Zero;
            foreach (string entity in new[] { "recovery.source", Receiver, "recovery.carrier", "recovery.service" }) {
                var stock = Facts(game, entity); steel += stock.Stock; fuel += Facts(game, entity, "recovery.fuel").Stock;
                c.That(stock.OutboundPromised <= stock.Stock && stock.InboundPromised >= Fix.Zero, "native cargo/export claims remain physically backed");
            }
            c.Equal(Fix.FromInt(20), steel, "finite steel conserved during queues plan pauses and route recovery");
            var flow = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            var consumed = flow.Aggregates.Where(a => a.Resource == Resource(game, "recovery.fuel") && a.Kind == ResourceFlowKind.Consumed).Aggregate(BigInteger.Zero, (sum, a) => sum + a.AmountRaw);
            c.Equal(new BigInteger(Fix.FromInt(72).Raw), new BigInteger(fuel.Raw) + consumed, "movement fuel stock plus real exposure consumption conserved");
            c.That(!flow.Aggregates.Any(a => a.Kind == ResourceFlowKind.Produced || a.Kind == ResourceFlowKind.StockPurchase || a.Kind == ResourceFlowKind.AuthoredCreation), "no manufactured stock grants or business rescue");
            var hauls = Jobs(game).Where(j => j.IsHaul && j.Worker.IsValid).ToList();
            c.Equal(hauls.Count, hauls.Select(j => j.Worker).Distinct().Count(), "no double carrier assignment");
            foreach (var slots in new[] { hauls.Where(j => j.ToilIndex < 2).Select(j => j.SourceDockSlot), hauls.Select(j => j.DestinationDockSlot) }) {
                var claims = slots.Where(s => !s.IsNone).ToList(); c.Equal(claims.Count, claims.Distinct().Count(), "shared dock slots have unique actual claims");
            }
            c.Equal(3, new[] { "recovery.carrier", "recovery.service", "crane" }.Select(e => Position(game, e)).Distinct().Count(), "physical machines never overlap");
        }
        private static void Reach(Check c, SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 512 && !ready(); i++) { Step(c, game, twin); Integrity(c, game); }
            if (!ready()) throw new InvalidOperationException(stage + " exceeded512Updates at=" + game.Clock.StepIndex + "/road=" + Road(game) + "/destination=" + Facts(game, Receiver).Stock +
                "/jobs=" + string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/" + j.Loaded + "/" + j.DockWaiting + "/" + j.SourceDockSlot + "/" + j.DestinationDockSlot)));
        }
        private static ConfigurePlanCommand Edit(long id, bool paused) => new ConfigurePlanCommand { Plan = id, Group = "durability.field", Priority = 100, Paused = paused };
        private static HaulResourcesCommand Dispatch(SimGame game, string worker, int priority) => new HaulResourcesCommand {
            Source = Id(game, "recovery.source"), Destination = Id(game, Receiver), Worker = Id(game, worker), Resource = Resource(game, "steel"), Ceiling = Fix.FromInt(8), Priority = priority };
        private static void Run(Check c)
        { foreach (bool halfMinute in new[] { false, true }) Scenario(c, halfMinute); }
        private static void Scenario(Check c, bool halfMinute)
        {
            var factory = PhysicalRecoveryScenario.Create(false, durability: true, halfMinute: halfMinute);
            SimGame NewSession() => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 920, Mods = { ResourceFlowsModule.ModuleId }, Seats = { Controller.Human, Controller.Human } });
            var game = NewSession(); var isolated = NewSession(); ulong untouched = isolated.Hash();
            using var recording = game.RecordReplay(Limits);
            c.That(!game.Modules.Contains("sim.tycoon"), "queue and persistent plan execution do not require business");
            var intent = new PlanIntent("activity:recovery.cut-road", Json.Parse("{\"target\":" + Id(game, "recovery.cut-site").Value + "}"), "durability.field", 100, true);
            var twin = Input(c, factory, game, new SubmitPlanCommand { Intent = intent });
            long cut = Plans(game).Single(p => p.Definition == intent.Definition).Id;
            c.Equal(PlanState.Pending, Plan(game, cut).State, "paused native plan records intent only"); c.Equal(0, Plan(game, cut).Order, "paused pending plan creates no physical execution");
            twin = Partial(c, factory, game);
            twin = Input(c, factory, game, Dispatch(game, "recovery.carrier", 100), Dispatch(game, "recovery.service", 50));
            int primary = Jobs(game).Single(j => j.IsHaul && j.Worker == Id(game, "recovery.carrier")).Id;
            int secondary = Jobs(game).Single(j => j.IsHaul && j.Worker == Id(game, "recovery.service")).Id;
            Reach(c, game, twin, () => Haul(game, secondary).DockWaiting && Haul(game, primary).Loaded > Fix.Zero && Haul(game, primary).Loaded < Fix.FromInt(8), "actual source service queue and partial first pickup");
            c.Equal(Fix.Zero, Haul(game, secondary).Loaded, "waiting carrier cannot load through occupied service bay");
            twin = Partial(c, factory, game); Integrity(c, game);
            c.That(Plan(game, cut).Paused && Plan(game, cut).Order == 0 && Road(game) == 1, "saved pending pause creates no hidden road removal");
            var privatePlans = Plans(game, 1); c.Equal(0, privatePlans.Count, "foreign viewer sees no private plans");
            ulong hash = game.Hash(); c.Equal(Codes.NotOwned, game.Seat(1).Preview(Edit(cut, false)), "foreign plan edits refused"); c.Equal(hash, game.Hash(), "plan refusal is pure");
            Reach(c, game, twin, () => Haul(game, primary).Loaded == Fix.FromInt(8) && Haul(game, primary).ToilIndex == 2, "first carrier physically finishes eight-unit pickup");
            twin = Partial(c, factory, game);
            twin = Input(c, factory, game, Edit(cut, false));
            Reach(c, game, twin, () => Plan(game, cut).State == PlanState.Active && Node(game, Plan(game, cut).Order).ProductiveSteps > 0, "native crane begins actual persistent road-removal plan");
            int cutOrder = Plan(game, cut).Order; var earned = Node(game, cutOrder);
            c.Equal(halfMinute ? 12L : 6L, earned.RequiredTicks, "same six game minutes resolve through selected clock quantum");
            twin = Partial(c, factory, game);
            twin = Input(c, factory, game, Edit(cut, true));
            var paused = Node(game, cutOrder); c.That(Plan(game, cut).Paused, "actual active plan is paused through engine command");
            twin = Partial(c, factory, game);
            for (int i = 0; i < (halfMinute ? 4 : 2); i++) {
                Step(c, game, twin); Integrity(c, game); c.Equal(paused.Progress, Node(game, cutOrder).Progress, "live plan pause preserves earned work");
                c.Equal(paused.ProductiveSteps, Node(game, cutOrder).ProductiveSteps, "live plan pause grants no productive exposure"); c.Equal(1, Road(game), "paused active removal leaves actual road unchanged");
            }
            twin = Input(c, factory, game, Edit(cut, false)); c.Equal(cutOrder, Plan(game, cut).Order, "resume preserves same native order identity");
            Reach(c, game, twin, () => Road(game) == 0, "same crane completes timed physical road removal");
            c.Equal(PlanState.Done, Plan(game, cut).State, "persistent removal plan settles with actual effect");
            Reach(c, game, twin, () => Haul(game, primary).State == JobState.Paused && Haul(game, primary).Blocker == WorkSystem.BlockerUnreachable, "loaded original route really becomes unavailable");
            c.Equal(Fix.FromInt(8), Haul(game, primary).Loaded, "road recovery retains original loaded cargo");
            var originalEndpoint = Haul(game, primary).DestinationEndpoint;
            twin = Partial(c, factory, game);
            Reach(c, game, twin, () => Haul(game, secondary).DockWaiting && Haul(game, secondary).Loaded == Fix.FromInt(8), "second loaded carrier physically waits for shared receiver bay during first recovery");
            twin = Partial(c, factory, game); Integrity(c, game);
            c.Equal(originalEndpoint, Haul(game, primary).DestinationEndpoint, "saved queue and route recovery preserve original inlet binding");
            twin = Input(c, factory, game, new LayLineCommand { Line = "road", Tiles = { new TileCoord(18, 12) } });
            c.Equal(0, Road(game), "repair command does not instantaneously restore road");
            Reach(c, game, twin, () => Plans(game).Any(p => p.Definition == "line:road" && p.State == PlanState.Active && Node(game, p.Order).ProductiveSteps > 0), "real road project earns physical construction work");
            twin = Partial(c, factory, game);
            Reach(c, game, twin, () => Road(game) == 1, "native project completes six game minutes of road repair");
            twin = Input(c, factory, game, new MoveVehicleCommand { Vehicle = Id(game, "crane"), To = new TileCoord(18, 15) });
            Reach(c, game, twin, () => Facts(game, Receiver).Stock > Fix.Zero && Facts(game, Receiver).Stock < Fix.FromInt(8) && Haul(game, primary).Loaded > Fix.Zero, "actual partial unloading after saved recovery");
            twin = Partial(c, factory, game); Integrity(c, game);
            Reach(c, game, twin, () => Facts(game, Receiver).Stock == Fix.FromInt(16) && !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), "both original queued hauls physically deliver once and depart");
            c.Equal(Fix.FromInt(4), Facts(game, "recovery.source").Stock, "two eight-unit pickups consume finite source stock once");
            foreach (string entity in new[] { "recovery.source", Receiver, "recovery.carrier", "recovery.service" }) {
                c.Equal(Fix.Zero, Facts(game, entity).InboundPromised, "no orphan queued inlet claim"); c.Equal(Fix.Zero, Facts(game, entity).OutboundPromised, "no orphan source or cargo claim");
            }
            twin = Partial(c, factory, game);
            for (int i = 0; i < 12; i++) { Step(c, game, twin); Integrity(c, game); }
            c.Equal(Fix.FromInt(16), Facts(game, Receiver).Stock, "terminal Updates repeat neither delivery");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal plans queues route and promises restore exactly"); c.Equal(untouched, isolated.Hash(), "interleaved session stays untouched");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), Limits);
            c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "physical queue and paused-plan replay " + replay.Failure + "/" + replay.Detail);
        }
    }
}
