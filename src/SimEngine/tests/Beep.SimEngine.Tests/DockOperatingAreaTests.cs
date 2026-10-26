using System;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    // Explicit authored inputs, ordinary commands, bounded engine Update and observations only.
    internal static class DockOperatingAreaTests
    {
        internal static void Register()
        {
            TestRunner.Add("Dock operating areas: provider geometry, policy admission and checkpoint ownership", Admission);
            TestRunner.Add("Dock operating areas: physical slot restrictions and saved continuation", Physical);
        }
        private static string Text(JsonValue root) { var text = new StringBuilder(); root.WriteCanonical(text); return text.ToString(); }
        private static GameFactory Tiles(bool three = false)
        {
            var root = Content.Json.Parse(FrameworkProofContent.Read("tile-service-docks.json"));
            if (three) root.Get("activities")[0].Get("nodes").Add(Content.Json.Parse("{\"id\":\"c\",\"executor\":\"work.staffed\",\"roles\":[\"crane\"],\"duration\":{\"numerator\":1,\"denominator\":5,\"unit\":\"hour\"}}"));
            return Colony.Factory(additionalDefinitions: Text(root), additionalDocuments: new Dictionary<string, string> { ["tile-dock-areas.json"] = FrameworkProofContent.Read("tile-dock-areas.json") });
        }
        private static GameFactory Points(string fault = null)
        {
            var root = Content.Json.Parse(PointGame.Definitions); var docks = Content.Json.Parse(FrameworkProofContent.Read("point-docks.json"));
            root.Set("entities", docks.Get("entities")); StartupContentAuthoring.UseEntries(root, docks.Get("entity_setups")); root.Set("work_policies", docks.Get("work_policies"));
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            root.Get("entities")[0].Get("components").Get("point_dock").Set("work_roles", Content.Json.Parse("[\"inspect\"]"));
            if (fault != null) root.Get("entities")[0].Get("components").Get("point_dock").Set("fault", JsonValue.Of(fault));
            return PointGame.Factory(Text(root), additionalDocuments: new Dictionary<string, string> { ["point-dock-areas.json"] = FrameworkProofContent.Read("point-dock-areas.json") });
        }
        private static SimGame NewPoint(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 313 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string name)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == name) return entity; throw new InvalidOperationException(name); }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static Entity Actor(SimGame game, Job job) => game.World.Entities.Get(job.Worker);
        private static Entity[] Setup(Check c, SimGame game)
        {
            // Initial colony machines remain real entities, held through ordinary player commands.
            var initial = new List<Entity>(); foreach (Entity entity in game.World.Entities.All) if (entity.Has<IWorker>()) initial.Add(entity);
            foreach (Entity entity in initial) c.That(TestKit.Order(game, entity.Owner.Value, new MoveVehicleCommand { Vehicle = entity.Id, To = entity.Get<ITilePresence>().Tile }).Accepted, "player holds initial machines outside this facility exercise");
            Spawn(game, "facility.source", 20, 20); Spawn(game, "facility.destination", 28, 20);
            var actors = new[] { Spawn(game, "facility.worker", 18, 20), Spawn(game, "facility.worker", 18, 22), Spawn(game, "facility.worker", 18, 24) };
            Tick(game); return actors;
        }
        private static WorkPolicySettings Settings(bool tile, string area, bool enabled = true)
            => new WorkPolicySettings(enabled, 100, tile ? new[] { new WorkerRoleId("crane"), new WorkerRoleId("haul") } :
                new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, area, true, true, WorkAssignmentMode.All);
        private static void Policy(Check c, SimGame game, EntityId worker, bool tile, string area, SimGame twin = null, bool enabled = true)
        {
            foreach (SimGame current in twin == null ? new[] { game } : new[] { game, twin })
            {
                var command = new ConfigureWorkPolicyCommand { Worker = worker, Settings = Settings(tile, area, enabled) };
                c.Equal(Code.None, current.Seat(0).Preview(command), "engine previews exact authored area edit");
                c.That(TestKit.Order(current, 0, command).Accepted && current.Fault == null, "ordinary policy command settles actual job");
            }
        }
        private static void Reach(SimGame game, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 500 && !ready(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (ready()) return;
            var facts = new List<string>(); foreach (Job job in Board(game).Jobs) facts.Add(job.Id + "/" + job.State + "/" + job.ToilIndex + "/" + job.Blocker + "/" + job.SourceDockSlot + "," + job.DestinationDockSlot);
            throw new InvalidOperationException(stage + " timeout at tick " + game.Tick + ": " + string.Join(";", facts));
        }
        private static Order Post(Check c, SimGame game, bool tile)
        {
            var result = TestKit.Order(game, 0, new PostOrderCommand { Plan = tile ? "facility.inspect" : "inspect", Target = Find(game, tile ? "facility.source" : "source").Id });
            c.That(result.Accepted && game.Fault == null, "player posts authored ordinary facility activity");
            return Board(game).Order(checked((int)result.Arg0));
        }
        private static WorkJobView Fact(SimGame game, Job job)
        {
            var facts = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(facts);
            foreach (var fact in facts) if (fact.Id == job.Id) return fact; throw new InvalidOperationException("missing job fact");
        }
        private static void Admission(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "slot policy has current execution identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no second area or physical queue codec");
            var factory = Tiles(); var game = New(factory); var actors = Setup(c, game); var source = Find(game, "facility.source");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "existing job payload owns physical commitments");
            c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "existing update coordinator has slot policy semantics");
            var dock = source.Get<IWorkDock>(); var bay = (TileSpatialSnapshot)dock.CaptureSlot(game.World, new Code("bay"));
            c.Equal(new TileCoord(19, 20), bay.Tile, "slot capture resolves negative relative offset through actual presence");
            c.Equal(new TileRect(19, 20, 1, 1), bay.Footprint, "native physical slot is detached unit geometry");
            c.Equal(game.World.Space.Id, bay.Space, "geometry carries actual session space identity");
            c.Throws<ArgumentException>(() => dock.CaptureSlot(game.World, new Code("missing")), "provider refuses unauthored slot");
            c.Throws<InvalidOperationException>(() => dock.CaptureSlot(New(factory).World, new Code("bay")), "provider rejects another session");
            foreach (Entity actor in actors) Policy(c, game, actor.Id, true, "dock.endpoints");
            ulong hash = game.Hash(); var candidates = new List<WorkWorkerCandidate>();
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("facility.inspect", "a", source.Id, candidates), "private candidate preview remains normal engine query");
            foreach (Entity actor in actors)
            {
                WorkWorkerCandidate? candidate = null; foreach (var row in candidates) if (row.Worker == actor.Id) candidate = row;
                c.That(candidate.HasValue && candidate.Value.Result.Blocker == WorkPolicyCodes.OutsideArea, "permitted endpoint cannot authorize excluded service bay");
            }
            c.Equal(hash, game.Hash(), "slot policy preview allocates no jobs, receipts or coordinate state");
            foreach (Entity actor in actors) Policy(c, game, actor.Id, true, "dock.no-exit");
            hash = game.Hash(); var haul = game.World.Services.Get<IHaulExecution>(); ResourceId steel = game.World.Content.ResourceId("steel");
            var preview = haul.PreviewHaul(new PlayerId(0), source.Id, Find(game, "facility.destination").Id, steel, Fix.FromInt(8));
            c.Equal(WorkPolicyCodes.OutsideArea, preview.Blocker, "delivery must have permitted actual exit geometry");
            c.Equal(0, haul.AcquireHaul(new PlayerId(0), source.Id, Find(game, "facility.destination").Id, steel, Fix.FromInt(8), 100), "invalid physical itinerary acquires no carrier or resource claims");
            c.Equal(hash, game.Hash(), "refused native haul leaves all authorities unchanged");
            c.Equal(0, source.Get<IOutletPort>().Reservations.Count, "refused source owns no promise");
            foreach (Entity actor in actors) Policy(c, game, actor.Id, true, "dock.selected");
            c.Equal(HaulStatus.Ready, haul.PreviewHaul(new PlayerId(0), source.Id, Find(game, "facility.destination").Id, steel, Fix.FromInt(8)).Status, "painted allowed physical slots admit normal hauling");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "authored area policy restores through existing codec");
            c.Equal(new TileCoord(19, 20), bay.Tile, "retained geometry is an immutable detached fact");

            var points = Points(); var pointGame = NewPoint(points); Entity machine = Find(pointGame, "machine.a"), pointSource = Find(pointGame, "source");
            var pointDock = pointSource.Get<IWorkDock>(); var snapshot = (PointSnapshot)pointDock.CaptureSlot(pointGame.World, new Code("load.bay"));
            c.Equal(Fix.One, snapshot.Position, "public mod supplies its own geometry without native point lookups");
            Policy(c, pointGame, machine.Id, false, "dock.endpoints");
            c.Equal(Code.None, WorkPolicyRules.CheckDestination(pointGame.World, machine, pointSource), "public endpoint itself is allowed");
            c.Equal(WorkPolicyCodes.OutsideArea, WorkPolicyRules.CheckGeometry(pointGame.World, machine, snapshot), "same canonical policy refuses outlying public service position");
            Policy(c, pointGame, machine.Id, false, "dock.no-exit");
            candidates.Clear(); pointGame.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("inspect", "check", pointSource.Id, candidates);
            foreach (var candidate in candidates) if (candidate.Worker == machine.Id) c.Equal(WorkPolicyCodes.OutsideArea, candidate.Result.Blocker, "public activity requires allowed exit before allocation");
            Policy(c, pointGame, machine.Id, false, "dock.all");
            c.Equal(Code.None, WorkPolicyRules.CheckGeometry(pointGame.World, machine, snapshot), "same public shape now admits actual bay");
            c.Equal(WorkPolicyCodes.UnsupportedArea, WorkPolicyRules.CheckGeometry(pointGame.World, machine, bay), "wrong spatial representation is never approximated");
            c.Equal(pointGame.Hash(), points.Load(pointGame.Save()).Hash(), "public geometry and policies save deterministically");
            foreach (string fault in new[] { "geometry-touch", "geometry-null", "geometry-space" })
            {
                var broken = NewPoint(Points(fault)); var actor = Find(broken, "machine.a"); Policy(c, broken, actor.Id, false, "dock.all");
                c.Throws<InvalidOperationException>(() => broken.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("inspect", "check", Find(broken, "source").Id, new List<WorkWorkerCandidate>()), "invalid public geometry refuses usable admission: " + fault);
            }
        }
        private static void Physical(Check c)
        {
            var factory = Tiles(); var game = New(factory); var actors = Setup(c, game);
            foreach (Entity actor in actors) Policy(c, game, actor.Id, true, "dock.selected", enabled: actor != actors[2]);
            var order = Post(c, game, true);
            Reach(game, () => Board(game).Job(order.Node("a").JobId)?.ToilIndex == 1 && Board(game).Job(order.Node("b").JobId) is Job b &&
                Actor(game, b).Get<ITilePresence>().Tile == new TileCoord(19, 24), "painted waiting area");
            Job working = Board(game).Job(order.Node("a").JobId), waiting = Board(game).Job(order.Node("b").JobId);
            c.Equal(new Code("wait.b"), waiting.DestinationDockSlot, "excluded first waiting slot is never selected");
            c.Equal(new TileCoord(19, 20), Actor(game, working).Get<ITilePresence>().Tile, "actual bay arrival precedes machine work");
            Policy(c, game, working.Worker, true, "dock.no-bay"); Policy(c, game, waiting.Worker, true, "dock.no-wait");
            c.Equal(JobState.Paused, working.State, "live bay exclusion pauses in place");
            c.Equal(JobState.Paused, waiting.State, "live waiting exclusion pauses in place");
            c.Equal(WorkPolicyCodes.OutsideArea, Fact(game, working).Blocker, "private fact publishes exact area blocker");
            c.Equal(NodeState.Paused, order.Node("a").State, "same activity node reflects policy pause");
            var earned = order.Node("a").Progress; var frozen = Fact(game, waiting); var tile = Actor(game, waiting).Get<ITilePresence>().Tile;
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "paused physical slots restore existing commitments");
            for (int i = 0; i < 5; i++) { Tick(game); Tick(twin); }
            c.Equal(earned, order.Node("a").Progress, "area wait earns no work");
            c.Equal(tile, Actor(game, waiting).Get<ITilePresence>().Tile, "area wait never teleports or continues old goal");
            c.Equal(waiting.Worker, frozen.Worker, "detached retained fact keeps original assignment");
            c.Equal(game.Hash(), twin.Hash(), "saved paused updates remain identical");
            Policy(c, game, working.Worker, true, "dock.no-exit", twin);
            c.Equal(JobState.Paused, working.State, "missing permitted departure retains earned work and bay");
            Policy(c, game, working.Worker, true, "dock.selected", twin);
            EntityId completedActor = working.Worker;
            Reach(game, () => working.ToilIndex == 2, "earned work before physical exit", twin);
            var completedWork = order.Node("a").Progress; Code exit = working.DestinationDockSlot;
            Policy(c, game, completedActor, true, "dock.no-exit", twin);
            c.Equal(JobState.Paused, working.State, "excluding an already committed exit pauses real departure");
            c.Equal(2, working.ToilIndex, "departure pause never reopens completed work");
            c.Equal(exit, working.DestinationDockSlot, "departure pause retains its actual exit claim");
            c.Equal(completedWork, order.Node("a").Progress, "departure edit preserves exact earned work");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "earned-work departure pause is a valid durable checkpoint");
            Policy(c, game, completedActor, true, "dock.selected", twin);
            Reach(game, () => !working.IsLive, "restricted physical departure", twin);
            c.Equal(new TileCoord(19, 28), game.World.Entities.Get(completedActor).Get<ITilePresence>().Tile, "physical departure selects allowed second exit, not excluded first");
            c.Equal(JobState.Paused, waiting.State, "oldest restricted waiter cannot commandeer service");
            c.Equal(JobState.Paused, frozen.State, "retained fact remains detached across peer completion");
            foreach (SimGame current in new[] { game, twin })
                c.That(TestKit.Order(current, 0, new SetHomeCommand { Worker = completedActor, Home = Find(current, "facility.destination").Id }).Accepted, "ordinary home movement frees finite physical exit");
            Policy(c, game, waiting.Worker, true, "dock.selected", twin);
            Reach(game, () => !order.IsActive, "resumed physical facility order", twin);
            c.Equal(OrderState.Done, order.State, "resumed activity completes through ordinary graph owner");
            c.Equal(game.Hash(), twin.Hash(), "native area-controlled physical continuation remains identical");
            TileFifo(c); Loaded(c, true); Loaded(c, false);
        }
        private static void TileFifo(Check c)
        {
            var factory = Tiles(true); var game = New(factory); foreach (Entity actor in Setup(c, game)) Policy(c, game, actor.Id, true, "dock.all");
            Order order = Post(c, game, true);
            Reach(game, () => Board(game).Job(order.Node("a").JobId)?.ToilIndex == 1 &&
                Board(game).Job(order.Node("b").JobId) is Job b && Actor(game, b).Get<ITilePresence>().Tile == new TileCoord(19, 22) &&
                Board(game).Job(order.Node("c").JobId) is Job later && Actor(game, later).Get<ITilePresence>().Tile == new TileCoord(19, 24), "three physically arrived facility actors");
            Job first = Board(game).Job(order.Node("a").JobId), older = Board(game).Job(order.Node("b").JobId), younger = Board(game).Job(order.Node("c").JobId);
            EntityId firstActor = first.Worker, olderActor = older.Worker, youngerActor = younger.Worker;
            Policy(c, game, olderActor, true, "dock.no-bay");
            c.Equal(JobState.Paused, older.State, "older arrived actor remains restricted without releasing its waiting claim");
            var twin = factory.Load(game.Save());
            Reach(game, () => younger.ToilIndex == 1, "eligible younger waiter promotion", twin);
            c.Equal(JobState.Paused, older.State, "older restricted waiter cannot block younger eligible bay admission");
            c.Equal(new Code("wait.a"), older.DestinationDockSlot, "bypassed older worker still owns original actual waiting slot");
            c.Equal(new Code("bay"), younger.DestinationDockSlot, "younger uses normal sole service claim");
            c.Equal(new TileCoord(19, 20), game.World.Entities.Get(youngerActor).Get<ITilePresence>().Tile, "younger reaches service physically rather than skipping travel");
            Reach(game, () => !first.IsLive && !younger.IsLive, "eligible peer service and exits", twin);
            foreach (EntityId actor in new[] { firstActor, youngerActor })
                foreach (SimGame current in new[] { game, twin }) c.That(TestKit.Order(current, 0, new SetHomeCommand { Worker = actor, Home = Find(current, "facility.destination").Id }).Accepted, "player clears finite physical peer exits");
            Policy(c, game, olderActor, true, "dock.all", twin);
            Reach(game, () => !order.IsActive, "restored older physical worker", twin);
            c.Equal(OrderState.Done, order.State, "restored older waiter completes original node without duplicate job");
            c.Equal(game.Hash(), twin.Hash(), "FIFO policy bypass and later resumption remain saved deterministic");
        }
        private static void Loaded(Check c, bool tile)
        {
            var factory = tile ? Tiles() : Points(); var game = tile ? New(factory) : NewPoint(factory);
            if (tile) { foreach (Entity actor in Setup(c, game)) Policy(c, game, actor.Id, true, "dock.all"); }
            else foreach (string id in new[] { "machine.a", "machine.b", "machine.c", "machine.d" }) Policy(c, game, Find(game, id).Id, false, "dock.all");
            Entity source = Find(game, tile ? "facility.source" : "source"), destination = Find(game, tile ? "facility.destination" : "destination");
            ResourceId resource = game.World.Content.ResourceId(tile ? "steel" : "supply"); var haul = game.World.Services.Get<IHaulExecution>();
            Job job = Board(game).Job(haul.AcquireHaul(new PlayerId(0), source.Id, destination.Id, resource, Fix.FromInt(8), 100));
            c.That(job != null, "ordinary haul acquires actual permitted physical itinerary");
            Reach(game, () => job.ToilIndex == 2 && job.Loaded.IsPositive, "physical pickup before area edit");
            Entity carrier = Actor(game, job); Fix cargo = carrier.Get<IInletPort>().Amount(resource); int claims = destination.Get<IInletPort>().Reservations.Count;
            Policy(c, game, job.Worker, tile, tile ? "dock.no-bay" : "dock.no-delivery-bay");
            c.Equal(JobState.Paused, job.State, "loaded haul pauses rather than releases or reopens");
            c.Equal(WorkPolicyCodes.OutsideArea, job.Blocker, "loaded haul shares exact slot area reason");
            c.Equal(cargo, carrier.Get<IInletPort>().Amount(resource), "area edit retains actual cargo");
            c.Equal(claims, destination.Get<IInletPort>().Reservations.Count, "area edit retains destination receipt");
            EntityId actorId = job.Worker; Code slot = job.DestinationDockSlot; var twin = factory.Load(game.Save());
            for (int i = 0; i < 5; i++) { Tick(game); Tick(twin); }
            c.Equal(actorId, job.Worker, "policy wait retains selected transporter");
            c.Equal(slot, job.DestinationDockSlot, "policy wait retains selected physical slot");
            c.Equal(cargo, carrier.Get<IInletPort>().Amount(resource), "paused Updates never duplicate or discard load");
            c.Equal(game.Hash(), twin.Hash(), "loaded restriction survives deterministic saved updates");
            Policy(c, game, actorId, tile, tile ? "dock.destination" : "dock.all", twin);
            c.That(job.State != JobState.Paused || job.Blocker != WorkPolicyCodes.OutsideArea, "physically departed pickup geometry no longer restricts remaining itinerary");
            Reach(game, () => !job.IsLive, "physical delivery and exit after policy restoration", twin);
            c.Equal(JobState.Done, job.State, "restored remaining itinerary completes real haul");
            c.Equal(Fix.FromInt(8), destination.Get<IInletPort>().Amount(resource), "actual receiver obtains exactly original cargo");
            c.Equal(Fix.FromInt(24), source.Get<IOutletPort>().Amount(resource), "actual pickup stock is conserved");
            c.Equal(Fix.Zero, carrier.Get<IInletPort>().Amount(resource), "carrier unloads physically, without grant or compensation");
            c.Equal(0, destination.Get<IInletPort>().Reservations.Count, "completed delivery releases native receipt");
            c.Equal(game.Hash(), twin.Hash(), "native/public saved physical delivery remains identical");
        }
    }
}
