using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    internal static class RouteOperatingAreaTests
    {
        internal static void Register()
        {
            TestRunner.Add("Route areas: provider contracts, admission and private diagnostics", Admission);
            TestRunner.Add("Route areas: physical constrained travel, cargo and saved continuation", Physical);
        }
        private static string Text(JsonValue root) { var text = new StringBuilder(); root.WriteCanonical(text); return text.ToString(); }
        internal static GameFactory Tiles() => Colony.Factory(additionalDefinitions: FrameworkProofContent.Read("tile-service-docks.json"),
            additionalDocuments: new Dictionary<string, string> { ["tile-route-areas.json"] = FrameworkProofContent.Read("tile-route-areas.json") });
        internal static GameFactory Points(string fault = null, bool fuel = false)
        {
            var root = Content.Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            var parts = root.Get("entities")[2].Get("components");
            parts.Set("work_policy", Content.Json.Parse("{\"profile\":\"route.policy\"}"));
            if (fault == "stationary") parts.Remove("point_motion");
            else if (fault == "absent")
            { parts.Remove("point_motion"); parts.Set("unestimated_motion", Content.Json.Parse("{\"ticks\":1000}")); }
            else if (fault != null) parts.Get("point_motion").Set("area_fault", JsonValue.Of(fault));
            if (fuel)
            {
                parts.Set("operating_inputs", Content.Json.Parse(FrameworkProofContent.Read("motion-operating-inputs.json")));
                parts.Get("storage").Set("capacity_policy", JsonValue.Of("fixture.count.40"));
                parts.Get("storage").Set("initial_stock", Content.Json.Parse("{\"supply\":20}"));
                // Propulsion stock is not cargo to return through automatic hauling.
                parts.Remove("transporter");
            }
            var policyDocument = Content.Json.Parse(FrameworkProofContent.Read("point-route-areas.json"));
            if (fault == "absent") policyDocument.Get("work_policies")[0].Set("area", JsonValue.Of("route.all"));
            return PointGame.Factory(Text(root), additionalDocuments: new Dictionary<string, string> { ["point-route-areas.json"] = Text(policyDocument) });
        }
        internal static SimGame NewPoint(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 317 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            return factory.NewGame(request);
        }
        internal static Entity Find(SimGame game, string name)
        { foreach (Entity e in game.World.Entities.All) if (e.Alive && e.Def.Id == name) return e; throw new InvalidOperationException(name); }
        internal static Entity Setup(Check c, SimGame game, int workerY = 20)
        {
            var initial = new List<Entity>(); foreach (Entity e in game.World.Entities.All) if (e.Has<IWorker>()) initial.Add(e);
            foreach (Entity e in initial) c.That(TestKit.Order(game, e.Owner.Value, new MoveVehicleCommand { Vehicle = e.Id, To = e.Get<ITilePresence>().Tile }).Accepted, "ordinary player commands hold unrelated machines");
            Spawn(game, "facility.source", 20, 20); Spawn(game, "facility.destination", 28, 20);
            Entity actor = Spawn(game, "facility.worker", 18, workerY); Tick(game); return actor;
        }
        internal static void Policy(Check c, SimGame game, EntityId id, string area, bool tile, SimGame twin = null)
        {
            foreach (SimGame current in twin == null ? new[] { game } : new[] { game, twin })
            {
                var settings = new WorkPolicySettings(true, 100, tile ? new[] { new WorkerRoleId("crane"), new WorkerRoleId("haul") }
                    : new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, area, true, true, WorkAssignmentMode.All);
                var command = new ConfigureWorkPolicyCommand { Worker = id, Settings = settings };
                c.Equal(Code.None, current.Seat(0).Preview(command), "normal policy preview validates authored identity");
                c.That(TestKit.Order(current, 0, command).Accepted && current.Fault == null, "ordinary policy command updates sole settings owner");
            }
        }
        internal static MotionExecutionView Fact(SimGame game, EntityId id)
        {
            var code = game.Seat(0).View.Feature<MotionView>().ReadExecution(id, out var fact);
            if (!code.IsNone) throw new InvalidOperationException(code.ToString()); return fact;
        }
        internal static void Reach(SimGame game, Func<bool> done, string stage, Action observe = null, SimGame twin = null)
        {
            for (int i = 0; i < 500 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); observe?.Invoke(); }
            if (!done())
            {
                var details = new List<string>(); foreach (Job job in game.World.Services.Get<WorkBoard>().Jobs) details.Add(job.Id + "/" + job.State + "/" + job.ToilIndex + "/" + job.Blocker);
                throw new InvalidOperationException(stage + " timeout at " + game.Tick + ": " + string.Join(";", details));
            }
        }
        internal static TileSpatialSnapshot At(string space, TileCoord tile) => new TileSpatialSnapshot(space, tile, new TileRect(tile, 1, 1), FixVec2.Centre(tile), FixVec2.Centre(tile), FixVec2.Zero, false, true);
        private static void Admission(Check c)
        {
            var factory = Tiles(); var game = New(factory); Entity actor = Setup(c, game);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "one current engine implementation identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "one operating-area persistence responsibility joins the manifest");
            c.Equal(6, game.World.System<MovementSystem>().StateSchemaVersion, "movement declares constrained execution semantics");
            c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "work declares runtime area references and retained waits");
            var all = game.World.Content.Get<ISpatialRegionDefinition>("route.all"); var detour = game.World.Content.Get<ISpatialRegionDefinition>("route.detour");
            var islands = game.World.Content.Get<ISpatialRegionDefinition>("route.islands");
            c.Equal(SpatialRegionStatus.Inside, islands.Check(At(all.Space, new TileCoord(21, 20))), "departure endpoint is individually allowed");
            c.Equal(SpatialRegionStatus.Inside, islands.Check(At(all.Space, new TileCoord(25, 20))), "arrival endpoint is individually allowed");
            c.Equal(SpatialRegionStatus.Unsupported, MotionAreaContracts.CheckTravel(islands, At(all.Space, new TileCoord(21, 20)), At(all.Space, new TileCoord(25, 20))), "native region never infers a long sweep from two endpoint cells");
            c.Equal(SpatialRegionStatus.Outside, MotionAreaContracts.CheckTravel(detour, At(all.Space, new TileCoord(21, 17)), At(all.Space, new TileCoord(22, 18))), "diagonal cannot cut an excluded painted corner");
            c.Equal(SpatialRegionStatus.Inside, MotionAreaContracts.CheckTravel(all, At(all.Space, new TileCoord(21, 17)), At(all.Space, new TileCoord(22, 18))), "rectangular full sweep permits ordinary diagonal");
            Policy(c, game, actor.Id, "route.islands", true);
            var mover = actor.Get<Mover>(); mover.GoTo(new TileRect(27, 20, 1, 1));
            ulong hash = game.Hash(); var fact = Fact(game, actor.Id);
            c.Equal("route.islands", fact.Area.Area, "private immutable view publishes actual area setting");
            c.Equal(MotionAreaCodes.Outside, fact.Blocker, "disconnected permitted endpoints refuse whole-route admission");
            c.Equal(hash, game.Hash(), "pure admission changes no route, clock, stock or claims");
            c.Equal(-1, mover.PathIndex, "preview does not publish a speculative path");
            c.Equal(0, mover.Path.Count, "A* scratch is not a second route owner");
            game.Seat(0).View.Feature<MotionView>().ReadArrival(actor.Id, out var arrival);
            c.That(!arrival.RemainingTicks.HasValue && arrival.Blocker == MotionAreaCodes.Outside, "blocked route never publishes fabricated ETA");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<MotionView>().ReadExecution(actor.Id, out var hidden), "foreign company cannot inspect route policy");
            c.That(hidden == null, "foreign query yields no detached private facts");
            c.Equal(hash, factory.Load(game.Save()).Hash(), "pending blocked intent is a valid checkpoint");
            mover.Stop(); Policy(c, game, actor.Id, "route.detour", true); mover.GoTo(new TileRect(27, 20, 1, 1));
            c.Equal(Code.None, Fact(game, actor.Id).Blocker, "available painted detour admits physical execution");
            c.Equal(MotionAreaCodes.Outside, fact.Blocker, "retained view remains detached after area edits");

            var points = Points(); var pointGame = NewPoint(points); var machine = Find(pointGame, "machine");
            Policy(c, pointGame, machine.Id, "route.stations", false); machine.Get<PointMotion>().GoTo(Fix.FromInt(10));
            var stations = pointGame.World.Content.Get<ISpatialRegionDefinition>("route.stations");
            c.Equal(SpatialRegionStatus.Inside, stations.Check(machine.Get<ISpatialPresence>().Capture()), "public start station allowed");
            c.Equal(SpatialRegionStatus.Inside, stations.Check(new PointSnapshot(stations.Space, Fix.FromInt(10), Fix.FromInt(10))), "public end station allowed");
            hash = pointGame.Hash();
            c.Equal(MotionAreaCodes.Unsupported, Fact(pointGame, machine.Id).Blocker, "endpoint-only mod shape cannot authorize continuous travel");
            c.Equal(hash, pointGame.Hash(), "unsupported provider shape is a pure explicit refusal");
            machine.Get<PointMotion>().Stop(); Policy(c, pointGame, machine.Id, "route.all", false); machine.Get<PointMotion>().GoTo(Fix.FromInt(10));
            c.Equal(Code.None, Fact(pointGame, machine.Id).Blocker, "public swept interval works without scheduler coordinate knowledge");
            c.That(ReferenceEquals(machine.Get<IMotion>(), machine.Get<IMotionAreaConstraint>()), "public capability aliases actual motion owner");
            var blind = NewPoint(Points("absent")); var blindMachine = Find(blind, "machine");
            c.That(blindMachine.Get<IMotion>() != null && blindMachine.Get<IMotionAreaConstraint>() == null, "independent actual provider may omit area support");
            c.Equal(MotionAreaCodes.Unsupported, Fact(blind, blindMachine.Id).Blocker, "missing motion constraint never means unrestricted automatic travel");
            c.Equal(blind.Hash(), Points("absent").Load(blind.Save()).Hash(), "unsupported actual motion remains a valid durable refusal");
            machine.Get<PointMotion>().Stop(); Policy(c, pointGame, machine.Id, "route.short", false); machine.Get<PointMotion>().GoTo(Fix.FromInt(10));
            c.Equal(MotionAreaCodes.Outside, Fact(pointGame, machine.Id).Blocker, "continuous interval refuses a sweep beyond its allowed extent");
            c.Throws<ArgumentException>(() => machine.Get<PointMotion>().CheckArea(NewPoint(points).World, stations), "public provider rejects another actual session");
            foreach (string fault in new[] { "touch", "status", "invalid" })
            {
                var broken = NewPoint(Points(fault)); var provider = Find(broken, "machine"); Policy(c, broken, provider.Id, "route.all", false);
                provider.Get<PointMotion>().GoTo(Fix.FromInt(10));
                c.Throws<InvalidOperationException>(() => Fact(broken, provider.Id), "invalid public callback refuses usable admission: " + fault);
            }
            EditableAdmission(c);
        }
        private static void Physical(Check c)
        {
            var factory = Tiles(); var game = New(factory); var actor = Setup(c, game); Policy(c, game, actor.Id, "route.all", true);
            var source = Find(game, "facility.source"); var destination = Find(game, "facility.destination"); var steel = game.World.Content.ResourceId("steel");
            var haul = game.World.Services.Get<IHaulExecution>();
            // Engine-side task submission uses the same authoritative carrier/job/receipt owner.
            int id = haul.AcquireHaul(new PlayerId(0), source.Id, destination.Id, steel, Fix.FromInt(8), 100);
            c.That(id > 0, "normal engine haul admission acquires real finite cargo claims");
            Job job = game.World.Services.Get<WorkBoard>().Job(id);
            Reach(game, () => job.ToilIndex >= 2 && actor.Get<Mover>().Status == MoveStatus.Moving && actor.Get<Mover>().PathIndex >= 0,
                "physical source loading and actual planned delivery");
            c.That(actor.Get<Mover>().Path.Count > 0, "area edit interrupts a real planned path, not only pending intent");
            c.Equal(Fix.FromInt(8), actor.Get<ITransporter>().Cargo.Amount(steel), "cargo exists only after physical loading");
            Policy(c, game, actor.Id, "route.islands", true);
            c.Equal(JobState.Paused, job.State, "live disconnected area pauses rather than cancels loaded work");
            c.Equal(MotionAreaCodes.Outside, job.Blocker, "same job owns current route refusal");
            c.Equal(MoveStatus.Moving, actor.Get<IMotion>().Status, "area pause retains actual intent for deterministic reopening");
            var frozen = actor.Get<ITilePresence>().Position; var age = actor.Get<Mover>().TicksMoving;
            var retainedPath = new List<TileCoord>(actor.Get<Mover>().Path);
            var claim = destination.Get<IInletPort>().ReservedIn(steel); var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "blocked loaded route restores all existing owners");
            for (int i = 0; i < 6; i++) { Tick(game); Tick(twin); }
            c.Equal(frozen, actor.Get<ITilePresence>().Position, "blocked vehicle does not move or teleport");
            c.Equal(age, actor.Get<Mover>().TicksMoving, "blocked trip earns no travel ticks");
            c.Equal(retainedPath.Count, actor.Get<Mover>().Path.Count, "blocked edit retains the one actual path");
            for (int i = 0; i < retainedPath.Count; i++) c.Equal(retainedPath[i], actor.Get<Mover>().Path[i], "blocked path steps are not silently replaced");
            c.Equal(Fix.FromInt(8), actor.Get<ITransporter>().Cargo.Amount(steel), "blocked cargo stays aboard");
            c.Equal(claim, destination.Get<IInletPort>().ReservedIn(steel), "blocked destination claim remains owned");
            c.Equal(game.Hash(), twin.Hash(), "blocked updates continue identically in isolated sessions");
            Policy(c, game, actor.Id, "route.detour", true, twin);
            var region = game.World.Content.Get<ISpatialRegionDefinition>("route.detour"); bool detoured = false;
            Reach(game, () => !job.IsLive, "constrained loaded delivery and exit", () =>
            {
                c.Equal(SpatialRegionStatus.Inside, region.Check(actor.Get<ISpatialPresence>().Capture()), "every observed physical position remains inside painted area");
                var motion = actor.Get<Mover>();
                for (int step = System.Math.Max(0, motion.PathIndex); step < motion.Path.Count; step++)
                    c.Equal(SpatialRegionStatus.Inside, region.Check(At(region.Space, motion.Path[step])), "reopened remaining path is constrained by the same region");
                detoured |= actor.Get<ITilePresence>().Tile.Y <= 17 || actor.Get<ITilePresence>().Tile.Y >= 27;
                c.Equal(game.Hash(), twin.Hash(), "constrained physical twins remain deterministic");
            }, twin);
            c.That(detoured, "loaded transporter physically travels around the forbidden middle, not through it");
            c.Equal(JobState.Done, job.State, "same job completes its actual delivery and departure");
            c.Equal(Fix.FromInt(24), source.Get<IOutletPort>().Amount(steel), "physical source debits exactly once");
            c.Equal(Fix.FromInt(8), destination.Get<IInletPort>().Amount(steel), "real destination receives exactly once");
            c.Equal(Fix.Zero, actor.Get<ITransporter>().Cargo.Amount(steel), "delivery leaves no duplicated cargo");
            c.Equal(Fix.Zero, destination.Get<IInletPort>().ReservedIn(steel), "completed receipt releases exactly");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed constrained trip remains durable");

            // A manual held goal is explicit control; releasing returns authority to automatic policy.
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = actor.Id, To = new TileCoord(23, 20) }).Accepted, "ordinary player direct order owns its held goal");
            c.That(Fact(game, actor.Id).Area.Area == null, "held movement is not silently treated as automatic dispatch");
            Reach(game, () => actor.Get<IMotion>().Status == MoveStatus.Arrived, "explicit manual travel");
            c.Equal(new TileCoord(23, 20), actor.Get<ITilePresence>().Tile, "manual intent reaches explicit outside-policy goal physically");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = actor.Id, Held = false }).Accepted, "normal release ends held intent");

            var localWork = New(factory); var localActor = Setup(c, localWork); Policy(c, localWork, localActor.Id, "route.islands", true);
            c.That(TestKit.Order(localWork, 0, new SetHomeCommand { Worker = localActor.Id, Home = Find(localWork, "facility.destination").Id }).Accepted,
                "ordinary home intent may wait for an unreachable permitted route");
            c.Equal(MotionAreaCodes.Outside, Fact(localWork, localActor.Id).Blocker, "idle home route is genuinely constrained");
            var posted = TestKit.Order(localWork, 0, new PostOrderCommand { Plan = "facility.inspect", Target = Find(localWork, "facility.source").Id });
            c.That(posted.Accepted, "player posts ordinary local activity while home route is blocked");
            var order = localWork.World.Services.Get<WorkBoard>().Order(checked((int)posted.Arg0));
            Reach(localWork, () => !order.IsActive, "permitted local activity replaces blocked idle home route", () =>
                c.Equal(SpatialRegionStatus.Inside, localWork.World.Content.Get<ISpatialRegionDefinition>("route.islands").Check(localActor.Get<ISpatialPresence>().Capture()),
                    "ordinary work, facility service and departure never leave their connected area"));
            c.Equal(OrderState.Done, order.State, "blocked unrelated home intent cannot starve valid local work");
            c.Equal(localWork.Hash(), factory.Load(localWork.Save()).Hash(), "constrained ordinary graph and blocked home intent remain durable");

            var pointFactory = Points(fuel: true); var points = NewPoint(pointFactory); var machine = Find(points, "machine");
            Policy(c, points, machine.Id, "route.all", false);
            c.That(TestKit.Order(points, 0, new SetHomeCommand { Worker = machine.Id, Home = Find(points, "destination").Id }).Accepted, "public provider uses ordinary automatic home intent");
            Reach(points, () => machine.Get<PointPresence>().Position >= Fix.FromInt(2), "positive-time point travel");
            // Both the destination and current point are inside this interval initially; after editing to a
            // disconnected shape the endpoint check still permits destination, but the sweep must refuse.
            Policy(c, points, machine.Id, "route.stations", false);
            var position = machine.Get<PointPresence>().Position; var stock = machine.Get<IInventory>().Amount(points.World.Content.ResourceId("supply"));
            var pointTwin = pointFactory.Load(points.Save());
            for (int i = 0; i < 5; i++) { Tick(points); Tick(pointTwin); }
            c.Equal(position, machine.Get<PointPresence>().Position, "unsupported public sweep halts physical movement");
            c.Equal(stock, machine.Get<IInventory>().Amount(points.World.Content.ResourceId("supply")), "area wait consumes no propulsion input");
            c.Equal(MotionAreaCodes.Unsupported, Fact(points, machine.Id).Blocker, "public diagnostics retain unsupported sweep reason");
            c.Equal(points.Hash(), pointTwin.Hash(), "public paused route and propulsion stock restore identically");
            Policy(c, points, machine.Id, "route.all", false, pointTwin);
            Reach(points, () => machine.Get<IMotion>().Status == MoveStatus.Arrived, "public resumed physical travel", twin: pointTwin);
            c.Equal(Fix.FromInt(10), machine.Get<PointPresence>().Position, "public provider alone settles continuous arrival");
            c.That(machine.Get<IInventory>().Amount(points.World.Content.ResourceId("supply")) < stock, "resumed physical updates consume actual propulsion stock");
            c.Equal(points.Hash(), pointTwin.Hash(), "independent saved providers settle identically");
            EditablePhysical(c);
        }

        private static WorkView Areas(SimGame game, int company = 0) => game.Seat(company).View.Feature<WorkView>();
        private static SetOperatingAreaCommand Edit(SimGame game, string name, string template, string shape = null)
            => new SetOperatingAreaCommand { Name = name, Template = template,
                ShapeJson = shape ?? game.World.Content.Get<ISpatialRegionDefinition>(template).ShapeJson,
                ExpectedRevision = Areas(game).ReadOperatingArea(name)?.Revision ?? -1 };
        private static void EditableAdmission(Check c)
        {
            var factory = Tiles(); var game = New(factory); Entity actor = Setup(c, game); var view = Areas(game);
            var seeded = view.ReadOperatingArea("route.all"); var initial = game.World.Content.Get<ISpatialRegionDefinition>("route.all");
            c.That(seeded != null && seeded.Revision == 0 && seeded.ShapeJson == initial.ShapeJson, "explicit JSON seeds private canonical initial areas");
            var areas = game.World.Services.Get<IOperatingAreas>();
            c.That(!ReferenceEquals(areas.Resolve(new PlayerId(0), "route.all"), initial), "runtime geometry is not a shared mutable content authority");
            c.That(!ReferenceEquals(areas.Resolve(new PlayerId(0), "route.all"), areas.Resolve(new PlayerId(1), "route.all")), "company geometries are separate immutable versions");
            c.That(TestKit.Order(game, 1, new SetOperatingAreaCommand { Name = "route.all", Template = "route.all", ExpectedRevision = 0,
                ShapeJson = "{\"x\":16,\"y\":16,\"width\":16,\"height\":1}" }).Accepted, "second company independently edits the same authored name");
            c.Equal(initial.ShapeJson, seeded.ShapeJson, "other company's edit does not mutate retained initial view");
            c.Equal(initial.ShapeJson, Areas(game).ReadOperatingArea("route.all").ShapeJson, "other company's actual geometry cannot alter this company's area");
            c.Equal(0L, Areas(game).ReadOperatingArea("route.all").Revision, "company edit sequences remain isolated");
            var list = new List<OperatingAreaSnapshot>(); view.CopyOperatingAreas(list);
            foreach (var item in list) c.Equal(new PlayerId(0), item.Company, "view listing cannot leak other company's intent");
            var command = Edit(game, "player.field", "route.all", "{\"height\":16,\"y\":16,\"width\":16,\"x\":16}");
            ulong hash = game.Hash();
            c.Equal(Code.None, view.PreviewOperatingArea(command.Name, command.Template, command.ShapeJson, command.ExpectedRevision), "management preview accepts explicit rectangular intent");
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary command preview shares the area authority");
            c.Equal(hash, game.Hash(), "shape decoding and previews allocate no authoritative identities, stock or time");
            c.That(TestKit.Order(game, 0, command).Accepted, "player creates named geometry through engine command");
            var created = view.ReadOperatingArea(command.Name);
            c.Equal(initial.ShapeJson, created.ShapeJson, "shape member order normalizes through the existing JSON serializer");
            c.That(created.Revision > 0, "revision is an edit identity, not an extra game clock");
            c.That(Areas(game, 1).ReadOperatingArea(command.Name) == null, "new private name is invisible to other company");
            Policy(c, game, actor.Id, command.Name, true);
            c.Equal(Code.None, WorkPolicyRules.CheckGeometry(game.World, actor, At(initial.Space, new TileCoord(18, 20))), "actual job rules resolve new name instead of content fallback");
            c.That(game.World.Content.Find<ISpatialRegionDefinition>(command.Name) == null, "runtime creation does not mutate shared content registry");
            hash = game.Hash();
            c.Equal(OperatingAreaCodes.InUse, view.PreviewRemoveOperatingArea(command.Name, created.Revision), "assigned work policy prevents unsafe area deletion");
            c.Equal(OperatingAreaCodes.Stale, game.Seat(0).Preview(command), "replayed stale create cannot overwrite current area");
            foreach (string shape in new[] { "null", "{", "{\"x\":-1,\"y\":0,\"width\":1,\"height\":1}",
                "{\"x\":0,\"y\":0,\"width\":2147483647,\"height\":1}",
                "{\"x\":16,\"y\":16,\"width\":16,\"height\":16,\"space\":\"other\"}" })
                c.Equal(OperatingAreaCodes.InvalidShape, game.Seat(0).Preview(Edit(game, command.Name, "route.all", shape)), "invalid explicit shape refuses without changing live geometry");
            c.Equal(OperatingAreaCodes.UnknownTemplate, view.PreviewOperatingArea("missing.new", "missing.template", "{}", -1), "no inferred prototype or fallback region");
            c.Equal(OperatingAreaCodes.Invalid, view.PreviewOperatingArea("Invalid Name", "route.all", "{}", -1), "area names use stable engine identity rules");
            c.Equal(hash, game.Hash(), "refused edits/removal preserve all existing owners");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "created geometry and non-content policy reference restore after services decode");
            var paint = game.World.Content.Get<PaintedTileRegionDef>("route.detour");
            var reversed = Content.Json.Parse(paint.ShapeJson); var values = reversed.Get("tiles"); var backwards = JsonValue.NewArray();
            for (int i = values.Count - 1; i >= 0; i--) backwards.Add(values[i].Clone()); reversed.Set("tiles", backwards);
            c.That(TestKit.Order(game, 0, Edit(game, "player.paint", "route.detour", Text(reversed))).Accepted, "player paints explicit tile-set geometry");
            c.Equal(paint.ShapeJson, view.ReadOperatingArea("player.paint").ShapeJson, "paint membership order canonicalizes without a second containment algorithm");
            c.Equal(OperatingAreaCodes.InvalidShape, view.PreviewOperatingArea("paint.bad", "route.detour", "{\"tiles\":[{\"x\":16,\"y\":16},{\"x\":16,\"y\":16}]}", -1), "duplicate painted cells refuse");
            Policy(c, game, actor.Id, "route.all", true);
            c.That(TestKit.Order(game, 0, new RemoveOperatingAreaCommand { Name = command.Name, ExpectedRevision = created.Revision }).Accepted, "unreferenced area removes through ordinary command");
            c.That(view.ReadOperatingArea(command.Name) == null, "removed area has no fallback definition");
            c.That(TestKit.Order(game, 0, Edit(game, command.Name, "route.all")).Accepted, "same name can be recreated with a new edit identity");
            c.That(view.ReadOperatingArea(command.Name).Revision > created.Revision, "delete/recreate never resurrects stale edit authority");
            c.Equal(OperatingAreaCodes.Stale, view.PreviewOperatingArea(command.Name, "route.all", initial.ShapeJson, created.Revision), "old version cannot edit a recreated name");
            c.Equal(initial.ShapeJson, seeded.ShapeJson, "retained immutable view stays detached through all edits");
            var isolated = New(factory); c.Equal(0L, Areas(isolated).ReadOperatingArea("route.all").Revision, "interleaved session retains initial geometry and revision");

            // Direct corrupt owner decode is fault instrumentation, never a host gameplay implementation.
            hash = game.Hash(); var persistence = (IPersistentService)areas;
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); writer.Count(game.World.Players.Count); long seq = 1; writer.Long(ref seq); writer.Count(1);
                string name = "bad.saved", template = "route.all", bad = "{}"; long rev = 1;
                writer.String(ref name); writer.String(ref template); writer.String(ref bad); writer.Long(ref rev);
                byte[] bytes = stream.ToArray();
                c.Throws<CorruptStateException>(() => persistence.Sync(new StateReader(bytes, 0, bytes.Length)), "invalid saved geometry is rejected by the same provider");
            }
            c.Equal(hash, game.Hash(), "failed area decode does not partially replace live geometry or edit sequence");
            foreach (var bad in new[] {
                ("bad.saved", "route.all", "{}", 1L, 1L),
                ("bad.saved", "missing.template", initial.ShapeJson, 1L, 1L),
                ("bad.saved", "route.all", initial.ShapeJson, 2L, 1L),
                ("bad.saved", "route.all", initial.ShapeJson, 0L, 0L),
                ("bad.saved", "route.all", "{\"x\":16,\"y\":16,\"width\":16,\"height\":16}", 1L, 1L),
                ("bad.saved", "route.all", initial.ShapeJson, -1L, 1L) })
            {
                using var stream = new MemoryStream(); var writer = new StateWriter(stream); writer.Count(game.World.Players.Count);
                long seq = bad.Item5; writer.Long(ref seq); writer.Count(1);
                string name = bad.Item1, template = bad.Item2, shape = bad.Item3; long rev = bad.Item4;
                writer.String(ref name); writer.String(ref template); writer.String(ref shape); writer.Long(ref rev);
                byte[] bytes = stream.ToArray();
                c.Throws<CorruptStateException>(() => persistence.Sync(new StateReader(bytes, 0, bytes.Length)), "saved area identity/template/revision/canonical shape rejects corruption");
                c.Equal(hash, game.Hash(), "each refused decode retains original area state");
            }
            var pointFactory = Points(); var points = NewPoint(pointFactory); var machine = Find(points, "machine");
            var pointEdit = Edit(points, "player.interval", "route.all", "{\"maximum\":12,\"minimum\":0}");
            points.Seat(0).Submit(pointEdit);
            points.Seat(0).Submit(new ConfigureWorkPolicyCommand { Worker = machine.Id,
                Settings = new WorkPolicySettings(true, 100, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, pointEdit.Name, true, true, WorkAssignmentMode.All) });
            var twin = pointFactory.Load(points.Save()); c.Equal(points.Hash(), twin.Hash(), "pending create and policy commands preserve original ordering");
            points.Clock.Pause(); long step = points.Clock.StepIndex; hash = points.Hash();
            c.Equal(0, points.Update(), "sole game-clock pause cannot commit an area edit or dependent policy");
            c.Equal(step, points.Clock.StepIndex, "area revision never advances time independently"); c.Equal(hash, points.Hash(), "paused command stream and world remain unchanged");
            points.Clock.Resume(); Tick(points); Tick(twin);
            c.That(Areas(points).ReadOperatingArea(pointEdit.Name) != null, "public contract creates continuous geometry without scheduler coordinate code");
            c.Equal(pointEdit.Name, Areas(points).TryReadPolicy(machine.Id, out var policy).IsNone ? policy.Settings.Area : null, "same ordered update applies newly created area to actual policy");
            c.Equal(points.Hash(), twin.Hash(), "pending public geometry commands continue identically after load");
            c.Equal(points.Hash(), pointFactory.Load(points.Save()).Hash(), "public region factory is also the saved geometry validator");
            foreach (var property in typeof(OperatingAreaSnapshot).GetProperties()) c.That(!property.CanWrite, "host area facts expose no mutable settings handle");

            var authoredFactory = Points("absent"); var authored = NewPoint(authoredFactory); var authoredMachine = Find(authored, "machine");
            Policy(c, authored, authoredMachine.Id, null, false);
            c.Equal(OperatingAreaCodes.InUse, Areas(authored).PreviewRemoveOperatingArea("route.all", 0), "future authored worker initialization also protects its starting area");
            var authoredOwner = (IPersistentService)authored.World.Services.Get<IOperatingAreas>(); ulong authoredHash = authored.Hash();
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); writer.Count(authored.World.Players.Count);
                for (int company = 0; company < authored.World.Players.Count; company++) { long seq = 0; writer.Long(ref seq); writer.Count(0); }
                byte[] bytes = stream.ToArray();
                c.Throws<CorruptStateException>(() => authoredOwner.Sync(new StateReader(bytes, 0, bytes.Length)), "saved state cannot erase an area required by future authored worker initialization");
            }
            c.Equal(authoredHash, authored.Hash(), "missing authored-area restore cannot partially replace the authority");
        }
        private static void EditablePhysical(Check c)
        {
            var factory = Tiles(); var game = New(factory); var actor = Setup(c, game); Policy(c, game, actor.Id, "route.all", true);
            var source = Find(game, "facility.source"); var destination = Find(game, "facility.destination"); var steel = game.World.Content.ResourceId("steel");
            var board = game.World.Services.Get<WorkBoard>();
            int id = game.World.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), source.Id, destination.Id, steel, Fix.FromInt(8), 100);
            Job job = board.Job(id); c.That(id > 0, "editable-area scenario acquires an ordinary physical haul");
            Reach(game, () => job.ToilIndex >= 2 && actor.Get<Mover>().Status == MoveStatus.Moving && actor.Get<Mover>().PathIndex >= 0, "loaded actual route before geometry edit");
            var oldGeometry = game.World.Services.Get<IOperatingAreas>().Resolve(new PlayerId(0), "route.all");
            var oldSnapshot = Areas(game).ReadOperatingArea("route.all");
            c.That(TestKit.Order(game, 0, Edit(game, "route.all", "route.islands")).Accepted, "edit existing name into disconnected painted geometry without changing worker policy");
            c.Equal(JobState.Paused, job.State, "live geometry edit pauses the same loaded job");
            c.Equal(MotionAreaCodes.Outside, job.Blocker, "route checks observe edited area through the sole authority");
            var position = actor.Get<ITilePresence>().Position; var claim = destination.Get<IInletPort>().ReservedIn(steel); var progress = job.HandlingProgress;
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "interrupted loaded geometry edit restores exact owners");
            for (int i = 0; i < 5; i++) { Tick(game); Tick(twin); }
            c.Equal(position, actor.Get<ITilePresence>().Position, "edited exclusion never moves or teleports the vehicle");
            c.Equal(Fix.FromInt(8), actor.Get<ITransporter>().Cargo.Amount(steel), "all real cargo remains aboard while blocked");
            c.Equal(claim, destination.Get<IInletPort>().ReservedIn(steel), "destination promise remains owned");
            c.Equal(progress, job.HandlingProgress, "completed physical handling is not reset by geometry policy");
            c.Equal(oldSnapshot.ShapeJson, ((ISpatialRegionGeometry)oldGeometry).ShapeJson, "old immutable geometry remains detached after replacement");
            c.Equal(game.Hash(), twin.Hash(), "blocked saved updates remain deterministic");
            foreach (SimGame current in new[] { game, twin })
                c.That(TestKit.Order(current, 0, Edit(current, "route.all", "route.detour")).Accepted, "painted reconnection reopens existing job/route and dock selectors");
            Reach(game, () => !job.IsLive, "edited connected physical delivery and departure", () =>
                c.Equal(SpatialRegionStatus.Inside, game.World.Services.Get<IOperatingAreas>().Resolve(new PlayerId(0), "route.all").Check(actor.Get<ISpatialPresence>().Capture()), "every actual update remains within current painted area"), twin);
            c.Equal(JobState.Done, job.State, "original acquired haul completes after geometry reconnection");
            c.Equal(Fix.FromInt(24), source.Get<IOutletPort>().Amount(steel), "physical source debits exactly once");
            c.Equal(Fix.FromInt(8), destination.Get<IInletPort>().Amount(steel), "physical destination receives exact retained load");
            c.Equal(Fix.Zero, actor.Get<ITransporter>().Cargo.Amount(steel), "geometry edits never duplicate or discard cargo");
            c.Equal(game.Hash(), twin.Hash(), "edited native routes and receipts continue identically through save/load");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed edited area remains durable");

            var pointFactory = Points(fuel: true); var points = NewPoint(pointFactory); var machine = Find(points, "machine");
            Policy(c, points, machine.Id, "route.all", false);
            c.That(TestKit.Order(points, 0, new SetHomeCommand { Worker = machine.Id, Home = Find(points, "destination").Id }).Accepted, "public machine starts ordinary timed home travel");
            Reach(points, () => machine.Get<PointPresence>().Position >= Fix.FromInt(2), "public travel before interval edit");
            c.That(TestKit.Order(points, 0, Edit(points, "route.all", "route.all", "{\"minimum\":0,\"maximum\":4}")).Accepted, "player narrows public interval through same engine command");
            var pointPosition = machine.Get<PointPresence>().Position; var stock = machine.Get<IInventory>().Amount(points.World.Content.ResourceId("supply"));
            var pointTwin = pointFactory.Load(points.Save());
            for (int i = 0; i < 5; i++) { Tick(points); Tick(pointTwin); }
            c.Equal(pointPosition, machine.Get<PointPresence>().Position, "public geometry exclusion stops actual travel");
            c.Equal(stock, machine.Get<IInventory>().Amount(points.World.Content.ResourceId("supply")), "blocked public travel consumes no propulsion stock");
            c.Equal(Code.None, Areas(points).ReadHome(machine.Id, out var home), "private home diagnostics remain readable");
            c.Equal(WorkPolicyCodes.OutsideArea, home.Blocker, "edited-out home endpoint is reported by its actual home-policy owner");
            c.Equal("route.all", Fact(points, machine.Id).Area.Area, "motion facts still publish current area identity when home policy stops motion");
            foreach (SimGame current in new[] { points, pointTwin }) c.That(TestKit.Order(current, 0, Edit(current, "route.all", "route.all")).Accepted, "public interval expansion reopens retained route");
            Reach(points, () => machine.Get<IMotion>().Status == MoveStatus.Arrived, "public edited interval physical arrival", twin: pointTwin);
            c.Equal(Fix.FromInt(10), machine.Get<PointPresence>().Position, "public provider settles physical arrival without engine coordinate assumptions");
            c.That(machine.Get<IInventory>().Amount(points.World.Content.ResourceId("supply")) < stock, "resumed game updates consume real propulsion supply");
            c.Equal(points.Hash(), pointTwin.Hash(), "public edited geometry and motion share saved deterministic continuation");
        }
    }
}
