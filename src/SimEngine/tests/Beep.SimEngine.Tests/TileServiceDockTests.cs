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
    // Authored content, ordinary player commands, bounded Update and observations; no native dock rules.
    internal static class TileServiceDockTests
    {
        internal static void Register()
        {
            TestRunner.Add("Tile service docks: authored geometry, placement and shared admission", Admission);
            TestRunner.Add("Tile service docks: native physical queues and saved continuation", Physical);
        }
        private static JsonValue Parts(JsonValue root, int index = 0) => root.Get("entities")[index].Get("components");
        private static JsonValue Dock(JsonValue root) => Parts(root).Get("tile_work_dock");
        private static string Text(JsonValue root) { var text = new StringBuilder(); root.WriteCanonical(text); return text.ToString(); }
        private static GameFactory Factory(Action<JsonValue> edit = null, Action<JsonValue> baseEdit = null)
        {
            string extra = FrameworkProofContent.Read("tile-service-docks.json");
            if (edit != null) { var root = Content.Json.Parse(extra); edit(root); extra = Text(root); }
            string native = Colony.Defs;
            if (baseEdit != null) { var root = Content.Json.Parse(native); baseEdit(root); native = Text(root); }
            return Colony.Factory(native, additionalDefinitions: extra);
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static Entity Find(SimGame game, string id)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException(id); }
        private static Entity[] Setup(SimGame game)
        {
            Spawn(game, "facility.source", 20, 20); Spawn(game, "facility.destination", 28, 20);
            var actors = new[] { Spawn(game, "facility.worker", 18, 20), Spawn(game, "facility.worker", 18, 22), Spawn(game, "facility.worker", 18, 24), Spawn(game, "facility.worker", 15, 28) };
            Tick(game); return actors; // Finish the actual initial-stock events before any checkpoint/query hash.
        }
        private static IHaulExecution Hauling(SimGame game) => game.World.Services.Get<IHaulExecution>();
        private static ResourceId Steel(SimGame game) => game.World.Content.ResourceId("steel");
        private static Fix Stock(SimGame game, string id) => Find(game, id).Get<IInletPort>().Amount(Steel(game));
        private static HaulPreview Preview(SimGame game) => Hauling(game).PreviewHaul(new PlayerId(0), Find(game, "facility.source").Id, Find(game, "facility.destination").Id, Steel(game), Fix.FromInt(8));
        private static Job Haul(SimGame game)
        {
            int id = Hauling(game).AcquireHaul(new PlayerId(0), Find(game, "facility.source").Id, Find(game, "facility.destination").Id, Steel(game), Fix.FromInt(8), 100);
            return Board(game).Job(id) ?? throw new InvalidOperationException("native dock acquisition refused: " + Preview(game).Blocker);
        }
        private static Order Post(SimGame game)
        {
            var result = TestKit.Order(game, 0, new PostOrderCommand { Plan = "facility.inspect", Target = Find(game, "facility.source").Id });
            if (!result.Accepted || game.Fault != null) throw new InvalidOperationException("native service order refused/faulted: " + result.Refusal + "/" + game.Fault);
            return Board(game).Order(checked((int)result.Arg0));
        }
        private static Entity Actor(SimGame game, Job job) => game.World.Entities.Get(job.Worker);
        private static WorkJobView Read(SimGame game, int id)
        {
            var values = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(values);
            foreach (var value in values) if (value.Id == id) return value; throw new InvalidOperationException("missing native service fact");
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 500 && !done(); i++)
            {
                Tick(game); if (twin != null) Tick(twin);
                foreach (var bay in new[] { new TileCoord(19, 20), new TileCoord(27, 20) })
                {
                    int count = 0;
                    foreach (Entity entity in game.World.Entities.All)
                        if (entity.Alive && (entity.Has<IWorker>() || entity.Has<ITransporter>()) && entity.Get<ITilePresence>()?.Tile == bay) count++;
                    if (count > 1) throw new InvalidOperationException(stage + ": actual tile service overlap " + bay);
                }
            }
            if (done()) return;
            var diagnostics = new List<string>();
            foreach (Job job in Board(game).Jobs) diagnostics.Add(job.Id + "/" + job.State + "/toil=" + job.ToilIndex + "/slots=" + job.SourceDockSlot + "," + job.DestinationDockSlot + "/" + job.Blocker + "/tile=" + Actor(game, job)?.Get<ITilePresence>()?.Tile);
            throw new InvalidOperationException(stage + " timed out at " + game.Tick + ": " + string.Join(";", diagnostics));
        }
        private static void Policy(Check c, SimGame game, EntityId worker, bool enabled, SimGame twin)
        {
            foreach (SimGame current in new[] { game, twin }) c.That(TestKit.Order(current, 0, new ConfigureWorkPolicyCommand { Worker = worker,
                Settings = new WorkPolicySettings(enabled, 100, new[] { new WorkerRoleId("crane"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted,
                "ordinary player policy pauses/resumes actual native service actor");
        }
        private static void Admission(Check c)
        {
            var factory = Factory(); var game = New(factory); var actors = Setup(game); var source = Find(game, "facility.source");
            var definition = source.Def.Component<TileWorkDockDef>(); var dock = source.Get<IWorkDock>();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current native facility semantics");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no extra queue or coordinate payload");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "same saved job claim owner");
            c.That(dock is TileWorkDock && ReferenceEquals(dock.Slots, definition.Slots), "native runtime aliases registered immutable geometry");
            c.That(source.Components[0] is TileWorkDock, "authored dock before presence exercises staged cross-component checkpoint validation");
            c.That(source.Has<IInletPort>() && source.Has<IOutletPort>(), "native resource service retains both port contracts");
            c.Throws<NotSupportedException>(() => ((IList<WorkDockSlot>)definition.Slots).Clear(), "native slot declarations are read-only");
            c.Throws<NotSupportedException>(() => ((IList<WorkerRoleId>)definition.WorkRoles).Clear(), "native service role declarations are read-only");
            c.Throws<InvalidOperationException>(() => definition.Read(new JsonReader(Content.Json.Parse("{}"), "late native dock")), "native geometry cannot be reauthored");
            c.Throws<InvalidOperationException>(() => definition.Resolve(null), "native geometry cannot be rebound");
            var map = game.World.Services.Get<TileMap>(); ulong hash = game.Hash(); EntityId next = game.World.Entities.NextIdentity;
            c.Equal(Codes.InvalidArgument, definition.CheckTilePlacement(game.World, null, null, null), "public native constraint rejects absent placement identity explicitly");
            c.Equal(Codes.OutOfBounds, map.CheckPlacement(game.World, source.Def, new TilePlacement(map.Id, new TileCoord(0, 20), FixVec2.Centre(new TileCoord(0, 20))), null), "negative outlying slot participates in canonical placement bounds");
            c.Throws<InvalidOperationException>(() => Spawn(game, "facility.source", 0, 20), "native spawning shares exact facility placement refusal");
            c.Equal(next, game.World.Entities.NextIdentity, "refused full geometry allocates no entity");
            c.Equal(hash, game.Hash(), "refused full geometry initializes no stock or map state");
            c.Equal(Codes.OutOfBounds, map.CheckPlacement(game.World, source.Def, new TilePlacement(map.Id, new TileCoord(16, map.Height - 1), FixVec2.Centre(new TileCoord(16, map.Height - 1))), null), "departure extent beyond building footprint participates in placement");
            Entity obstacle = Spawn(game, "module_port", 14, 20); Tick(game); hash = game.Hash(); next = game.World.Entities.NextIdentity;
            var blocked = new TilePlacement(map.Id, new TileCoord(15, 20), FixVec2.Centre(new TileCoord(15, 20)));
            c.Equal(Codes.Blocked, map.CheckPlacement(game.World, source.Def, blocked, null), "actual static obstacle in service geometry refuses placement");
            c.Throws<InvalidOperationException>(() => Spawn(game, "facility.source", 15, 20), "static facility-slot refusal precedes actual creation");
            c.Equal(next, game.World.Entities.NextIdentity, "blocked slot allocates no ID"); c.Equal(hash, game.Hash(), "blocked facility-slot preview is pure");
            c.Equal(Code.None, map.CheckPlacement(game.World, source.Def, map.CapturePlacement(source), source), "replacement ignores actual source geometry only");
            c.Equal(Code.None, dock.CheckRequest(game.World, actors[0], WorkDockRequest.Work(new WorkerRoleId("crane"))), "actual native worker accepted by explicit authored role");
            c.Equal(WorkDockCodes.Incompatible, dock.CheckRequest(game.World, actors[0], WorkDockRequest.Work(new WorkerRoleId("haul"))), "resource hauler role is not substituted for ordinary facility work");
            c.Equal(WorkDockCodes.Incompatible, dock.CheckRequest(game.World, actors[0], default), "default request is not a native service");
            var other = New(factory); c.Equal(WorkDockCodes.Incompatible, dock.CheckRequest(other.World, actors[0], WorkDockRequest.Work(new WorkerRoleId("crane"))), "cross-session native entity handles refuse");
            c.Equal(SpatialInteractionStatus.Reachable, dock.Check(game.World, actors[0], new Code("bay")), "native reach uses actual mobility route");
            c.That(dock.TryEstimateCost(game.World, actors[0], new Code("bay"), null, out long cost), "native slot cost is available"); c.Equal(1L, cost, "cost is for bay, not building center");
            Job haul = Haul(game); Order order = Post(game); Job a = Board(game).Job(order.Node("a").JobId), b = Board(game).Job(order.Node("b").JobId);
            c.Equal(actors[0].Id, haul.Worker, "actual eligible native carrier selected");
            c.Equal(new Code("bay"), haul.SourceDockSlot, "haul owns actual native service claim");
            c.Equal(new Code("wait.a"), a.DestinationDockSlot, "ordinary work uses same first native waiting claim");
            c.Equal(new Code("wait.b"), b.DestinationDockSlot, "parallel activity uses next stable actual tile claim");
            c.Equal(WorkDockCodes.Full, Preview(game).Blocker, "mixed activity claims exhaust shared native capacity");
            hash = game.Hash(); int jobId = Board(game).NextJobId;
            c.Equal(0, Hauling(game).AcquireHaul(new PlayerId(0), source.Id, Find(game, "facility.destination").Id, Steel(game), Fix.FromInt(8), 100), "full native facility cannot admit another carrier");
            c.Equal(jobId, Board(game).NextJobId, "full native refusal allocates no job"); c.Equal(hash, game.Hash(), "full native refusal creates no receipt");
            c.That(Read(game, a.Id).DockWaiting, "existing private facts report native queued work");
            var hidden = new List<WorkJobView>(); game.Seat(1).View.Feature<WorkView>().CopyJobs(hidden); c.Equal(0, hidden.Count, "foreign private view leaks no native claims");
            c.Equal(ResourceTransactions.ActivePromises, game.World.CheckDespawn(source), "same removal owner protects mixed native jobs");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "native dock-first JSON saves and restores after complete presence decode");
            Invalid(c); WorkOnly(c); Nonblocking(c); WideGeometry(c); LifecycleGuards(c);
        }
        private static void Invalid(Check c)
        {
            foreach (string key in new[] { "work_roles", "resources", "slots" })
            {
                c.Throws<ContentException>(() => New(Factory(root => Dock(root).Remove(key))), "required native facility field " + key);
                foreach (var invalid in new[] { JsonValue.Null(), JsonValue.Of("wrong"), JsonValue.Of(false) })
                    c.Throws<ContentException>(() => New(Factory(root => Dock(root).Set(key, invalid))), "typed native facility field " + key + "/" + invalid);
            }
            foreach (string field in new[] { "x", "y" })
            {
                c.Throws<ContentException>(() => New(Factory(root => Dock(root).Get("slots")[0].Remove(field))), "explicit relative coordinate required " + field);
                foreach (var invalid in new[] { JsonValue.Number("1.5"), JsonValue.Number("2147483648"), JsonValue.Of("1") })
                    c.Throws<ContentException>(() => New(Factory(root => Dock(root).Get("slots")[0].Set(field, invalid))), "relative coordinate is an exact tile integer " + field);
            }
            c.Throws<ContentException>(() => New(Factory(root => Dock(root).Set("slots", Content.Json.Parse("[]")))), "empty native slot set refuses");
            c.Throws<ContentException>(() => New(Factory(root => Dock(root).Get("slots")[1].Set("id", JsonValue.Of("bay")))), "duplicate native slot ID refuses");
            c.Throws<ContentException>(() => New(Factory(root => Dock(root).Get("slots")[1].Set("y", JsonValue.Number("0")))), "duplicate native slot tile refuses");
            c.Throws<ContentException>(() => New(Factory(root => Dock(root).Get("slots")[0].Set("kind", JsonValue.Of("unknown")))), "unknown native slot kind refuses");
            c.Throws<ContentException>(() => New(Factory(root => Dock(root).Get("slots")[0].Set("x", JsonValue.Number("0")))), "service cannot occupy own blocking footprint");
            c.Throws<ContentException>(() => New(Factory(root => Dock(root).Get("slots")[0].Set("x", JsonValue.Number("-2147483648")))), "offset larger than authored world refuses with wide arithmetic");
            c.Throws<ContentException>(() => New(Factory(root => Parts(root).Get("tile_presence").Set("movable", JsonValue.Of(true)))), "moving facilities are not invented");
            c.Throws<ContentException>(() => New(Factory(root => Parts(root).Remove("tile_presence"))), "native facility requires tile presence");
            c.Throws<ContentException>(() => New(Factory(root => Parts(root).Remove("storage"))), "native resource service requires real inlet");
            foreach (string field in new[] { "work_roles", "resources" })
                c.Throws<ContentException>(() => New(Factory(root => Dock(root).Set(field, Content.Json.Parse("[\"missing\"]")))), "native catalog reference refuses " + field);
            c.Throws<ContentException>(() => New(Factory(root => Dock(root).Set("work_roles", Content.Json.Parse("[\"crane\",\"crane\"]")))), "duplicate native work roles refuse");
            c.Throws<ContentException>(() => New(Factory(root => Dock(root).Set("resources", Content.Json.Parse("[\"steel\",\"steel\"]")))), "duplicate native resources refuse");
            c.Throws<ContentException>(() => New(Factory(root => { Dock(root).Set("resources", Content.Json.Parse("[]")); Dock(root).Set("work_roles", Content.Json.Parse("[]")); })), "empty combined native declaration refuses");
            var game = New(Factory()); Setup(game); var source = Find(game, "facility.source"); var presence = source.Get<TilePresence>(); var original = presence.Tile;
            typeof(TilePresence).GetProperty("Tile").SetValue(presence, new TileCoord(0, 20)); // Checkpoint fault instrumentation only.
            c.Throws<CorruptStateException>(() => game.Save(), "checkpoint refuses actual origin inconsistent with outlying slot bounds");
            typeof(TilePresence).GetProperty("Tile").SetValue(presence, new TileCoord(game.World.Services.Get<TileMap>().Width, 20));
            c.Throws<CorruptStateException>(() => game.Hash(), "checkpoint refuses outlying building footprint even when every negative slot fits");
            typeof(TilePresence).GetProperty("Tile").SetValue(presence, original); c.That(game.Save().Length > 0, "restored origin instrumentation returns to legal native geometry");
        }
        private static void WorkOnly(Check c)
        {
            var factory = Factory(root => { Dock(root).Set("resources", Content.Json.Parse("[]")); Parts(root).Remove("storage"); });
            var game = New(factory); Setup(game); var source = Find(game, "facility.source"); Order order = Post(game);
            c.That(source.Has<IWorkDock>() && !source.Has<IInletPort>() && !source.Has<IOutletPort>(), "native work-only facility has no invented inventory endpoint");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "native work-only facility checkpoints through existing owners");
            Reach(game, () => !order.IsActive, "native work-only service"); c.Equal(OrderState.Done, order.State, "native work-only timed activity completes");
        }
        private static void Nonblocking(Check c)
        {
            var factory = Factory(root =>
            {
                var worker = root.Get("entities")[2].Clone(); worker.Set("id", JsonValue.Of("facility.nonblocking"));
                worker.Get("components").Get("tile_presence").Set("blocks", JsonValue.Of(false));
                worker.Get("components").Get("worker").Set("roles", Content.Json.Parse("[\"crane\"]")); root.Get("entities").Add(worker);
            });
            var game = New(factory); var actors = Setup(game); var occupant = Spawn(game, "facility.nonblocking", 19, 20);
            c.That(game.World.Services.Get<TileMap>().OccupantAt(new TileCoord(19, 20)) == null, "nonblocking actual worker does not occupy static map ledger");
            c.Equal(WorkDockCodes.Occupied, Find(game, "facility.source").Get<IWorkDock>().CheckVacancy(game.World, actors[0], new Code("bay")), "physical native vacancy still sees nonblocking worker presence");
            c.Equal(new Code("wait.a"), Preview(game).SourceDockSlot, "native hauling cannot hide another real nonblocking occupant");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "nonblocking real geometry saves without duplicate occupancy state");
        }
        private static void WideGeometry(Check c)
        {
            var factory = Factory(root =>
            {
                var wide = root.Get("entities")[0].Clone(); wide.Set("id", JsonValue.Of("facility.wide"));
                wide.Get("components").Get("tile_presence").Set("space", JsonValue.Of("facility.wide-space"));
                wide.Get("components").Get("tile_work_dock").Get("slots")[4].Set("x", JsonValue.Number("1")); root.Get("entities").Add(wide);
            }, root => root.Get("tile_spaces").Add(Content.Json.Parse("{\"id\":\"facility.wide-space\",\"width\":2147483647,\"height\":40}")));
            var game = New(factory); var definition = game.World.Content.Get<EntityDef>("facility.wide").Component<TileWorkDockDef>();
            TileSpaceGeometry geometry = SpatialSpaceContracts.ReadGeometry(definition.SpaceDefinition, game.World.Settings.Scenario);
            c.Equal(2147483647, geometry.Width, "native facility accepts independent very-wide authored space without a platform cap");
            c.Equal(Code.None, definition.CheckOrigin(geometry, new TileCoord(2147483645, 20)), "wide offset geometry uses actual authored extent");
            c.Equal(Codes.OutOfBounds, definition.CheckOrigin(geometry, new TileCoord(2147483646, 20)), "wide positive offset refuses boundary instead of integer wrapping");
        }
        private static void Physical(Check c)
        {
            var factory = Factory(); var game = New(factory); Setup(game); var untouched = New(factory); ulong untouchedHash = untouched.Hash();
            Job haul = Haul(game); Order order = Post(game); Job a = Board(game).Job(order.Node("a").JobId), b = Board(game).Job(order.Node("b").JobId);
            Entity hauler = Actor(game, haul), first = Actor(game, a), second = Actor(game, b);
            c.Equal(WorkProgress.Zero, order.Node("a").Progress, "command/assignment grants no instant native service");
            Reach(game, () => haul.HandlingProgress.WholeTicks >= 2 && first.Get<ITilePresence>().Tile == new TileCoord(19, 22) && second.Get<ITilePresence>().Tile == new TileCoord(19, 24), "actual native mixed waiting");
            c.Equal(new TileCoord(19, 20), hauler.Get<ITilePresence>().Tile, "hauler works in authored bay outside blocking facility");
            c.Equal(WorkProgress.Zero, order.Node("a").Progress, "native waiter earns no remote work");
            c.Equal(WorkProgress.Zero, order.Node("b").Progress, "younger native waiter earns no remote work");
            c.Equal(WorkDockCodes.Waiting, order.Node("a").Blocker, "native queue blocker comes from existing activity graph");
            c.Equal(new Code("state.stopped"), first.State, "Entity.Update settles native waiting state");
            c.Equal(untouchedHash, untouched.Hash(), "other session has no shared facility queue state");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "native physical queue survives staged restore");
            Reach(game, () => order.Node("a").Progress.WholeTicks >= 2, "native oldest activity promotion after real haul clearance", twin);
            c.Equal(new TileCoord(19, 20), first.Get<ITilePresence>().Tile, "promoted native actor is physically in service");
            c.That(hauler.Get<ITilePresence>().Tile != new TileCoord(19, 20), "promotion waits for actual pickup bay vacancy");
            c.Equal(WorkProgress.Zero, order.Node("b").Progress, "native younger waiter cannot overtake service actor");
            WorkProgress progress = order.Node("a").Progress; Policy(c, game, first.Id, false, twin);
            for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); }
            c.Equal(progress, order.Node("a").Progress, "paused occupied native bay earns no work");
            c.Equal(new Code("bay"), a.DestinationDockSlot, "paused native activity retains actual exclusive service claim");
            c.Equal(WorkProgress.Zero, order.Node("b").Progress, "younger native actor cannot preempt paused bay");
            c.Equal(game.Hash(), twin.Hash(), "native paused physical queue continues identically");
            twin = factory.Load(game.Save()); Policy(c, game, first.Id, true, twin);
            Reach(game, () => a.ToilIndex == 2, "native earned work awaiting actual exit", twin);
            c.That(a.IsLive && first.Get<IWorker>().Job == a.Id && order.Node("a").State != NodeState.Done, "native actor/node retained until real departure");
            c.Equal(game.World.Time.Ticks(order.Plan.Graph.Find("a").Duration), order.Node("a").Progress.WholeTicks, "native service consumes exact authored game time");
            twin = factory.Load(game.Save()); Reach(game, () => !order.IsActive && haul.State == JobState.Done, "native mixed service and timed delivery", twin);
            c.Equal(OrderState.Done, order.State, "native activities finish only after service and departure");
            c.Equal(Fix.FromInt(24), Stock(game, "facility.source"), "native mixed work never fabricates extra pickup");
            c.Equal(Fix.FromInt(8), Stock(game, "facility.destination"), "native timed haul delivers exactly one batch");
            c.Equal(Fix.Zero, hauler.Get<ITransporter>().Cargo.Amount(Steel(game)), "native cargo conservation leaves no duplicate stock");
            c.That(a.DestinationDockSlot.IsNone && b.DestinationDockSlot.IsNone && haul.SourceDockSlot.IsNone && haul.DestinationDockSlot.IsNone, "same owner releases every terminal native commitment");
            c.Equal(game.Hash(), twin.Hash(), "interleaved saved native full continuation agrees");
            BlockedExit(c);
        }
        private static void LifecycleGuards(Check c)
        {
            var factory = Factory(root =>
            {
                Parts(root).Set("proof_checkpoint", Content.Json.Parse("{\"fault\":\"none\"}"));
                Parts(root).Set("proof_tile_constraint", Content.Json.Parse("{\"fault\":\"none\"}"));
            });
            var game = New(factory); Setup(game); var source = Find(game, "facility.source"); var map = game.World.Services.Get<TileMap>(); ulong hash = game.Hash();
            c.Equal(Code.None, map.CheckPlacement(game.World, source.Def, map.CapturePlacement(source), source), "multiple definition constraints compose in existing placement owner");
            c.Equal(hash, game.Hash(), "public/native constraint and checkpoint checks remain pure");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "public checkpoint probe before presence runs only after complete decode");
            var impure = New(Factory(root => Parts(root).Set("proof_checkpoint", Content.Json.Parse("{\"fault\":\"touch\"}"))));
            Setup(impure); c.Throws<CorruptStateException>(() => impure.Save(), "public checkpoint mutation cannot return a usable save");
            var corrupt = New(Factory(root => Parts(root).Set("proof_tile_constraint", Content.Json.Parse("{\"fault\":\"touch-replacing\"}"))));
            Setup(corrupt); var current = Find(corrupt, "facility.source"); var space = corrupt.World.Services.Get<TileMap>();
            c.Throws<InvalidOperationException>(() => space.CheckPlacement(corrupt.World, current.Def, space.CapturePlacement(current), current), "public placement constraint mutation refuses before replacement admission");
        }
        private static void BlockedExit(Check c)
        {
            var factory = Factory(); var game = New(factory); Setup(game);
            Entity blockerA = Spawn(game, "truck", 19, 26), blockerB = Spawn(game, "truck", 19, 28);
            Order order = Post(game); Job a = Board(game).Job(order.Node("a").JobId);
            Reach(game, () => a.Blocker == WorkDockCodes.DepartureBlocked, "actual native occupied exits");
            c.That(a.IsLive && order.Node("a").State != NodeState.Done, "occupied native exits cannot teleport/complete work");
            c.Equal(new TileCoord(19, 20), Actor(game, a).Get<ITilePresence>().Tile, "blocked native actor remains physically in bay");
            c.Equal(WorkDockCodes.DepartureBlocked, order.Node("a").Blocker, "native blocked exit is visible without host inference");
            var twin = factory.Load(game.Save()); WorkProgress progress = order.Node("a").Progress;
            for (int i = 0; i < 3; i++) { Tick(game); Tick(twin); }
            c.Equal(progress, order.Node("a").Progress, "completed blocked native service earns no extra work");
            c.Equal(game.Hash(), twin.Hash(), "occupied native exit saves/continues through actual presence");
            foreach (SimGame current in new[] { game, twin }) foreach (EntityId id in new[] { blockerA.Id, blockerB.Id })
                c.That(TestKit.Order(current, 0, new SetHomeCommand { Worker = id, Home = Find(current, "facility.destination").Id }).Accepted, "ordinary engine home movement clears native exit occupants");
            Reach(game, () => !order.IsActive, "native real exit clearance", twin);
            c.Equal(OrderState.Done, order.State, "cleared native exits allow exactly-once activity completion"); c.Equal(game.Hash(), twin.Hash(), "native exit clearance saves/continues identically");
        }
    }
}
