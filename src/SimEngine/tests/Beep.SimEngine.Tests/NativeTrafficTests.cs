using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static class NativeTrafficTests
    {
        internal const string Case = "Native traffic: opposing slow vehicles clear a shared approach without oscillation";
        internal const string ClearanceCase = "Native traffic: partial segment clearance is pure, saved and released by player stop";
        internal const string ContractCase = "Native traffic: independent clearance contracts reject mutation and malformed segments";
        internal const string ChokeCase = "Native traffic: temporary choke blockage waits for structural route to reopen";
        internal const string HomeTrafficCase = "Native traffic: returning home and outbound movement clear adjacent slow approaches";
        internal const string CrossingCase = "Native traffic: opposite crossings clear parallel approaches between parked obstacles";
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add(ClearanceCase, Clearance);
            TestRunner.Add(ContractCase, Contracts);
            TestRunner.Add(ChokeCase, Choke);
            TestRunner.Add(HomeTrafficCase, HomeTraffic);
            TestRunner.Add(CrossingCase, Crossing);
        }
        private static GameFactory Factory(bool half = false, bool probe = false, bool choke = false, bool homeTraffic = false, bool crossing = false, bool controlled = false)
        {
            var world = Json.Parse(FrameworkProofContent.Read("native-sensor-world.json"));
            var machine = world.Get("entities")[1];
            machine.Get("components").Remove("tile_sensor");
            machine.Get("components").Get("mover").Set("speed", JsonValue.Number("20"));
            if (controlled)
            {
                var execution = Json.Parse(FrameworkProofContent.Read("motion-execution.json"));
                execution.Get("states").Set("disabled", JsonValue.Of("survey.stopped"));
                machine.Get("components").Set("motion_execution", execution);
            }
            world.Get("tile_spaces")[0].Set("width", JsonValue.Number("24"));
            world.Get("tile_spaces")[0].Set("height", JsonValue.Number(homeTraffic ? "32" : "24"));
            if (half) world.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            var scenario = world.Get("scenarios")[0];
            scenario.Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of(crossing ? "traffic.crossing" : homeTraffic ? "traffic.home" : probe ? "traffic.probe.start" : choke ? "traffic.choke" : "traffic.start"));
            var master = Json.Parse(FrameworkProofContent.Read("native-sensor-master.json"));
            for (int i = 0; i < master.Get("modules").Count; i++)
            {
                var module = master.Get("modules")[i];
                if (module.Get("id").Text == WorldSetupModule.ModuleId)
                    module.Get("documents").Add(JsonValue.Of("native-movement-traffic.json"));
            }
            if (probe)
            {
                scenario.Get("modules").Add(JsonValue.Of("proof.traffic-clearance"));
                master.Get("modules").Add(Json.Parse("{\"id\":\"proof.traffic-clearance\",\"version\":\"1.0.0\",\"documents\":[\"native-traffic-clearance-probe.json\"]}"));
            }
            // Original declaration and cost owner; only the explicitly authored fixture cost changes.
            return LiveSensorTests.Factory(world: world.ToString(), master: master.ToString(),
                additionalDocuments: new Dictionary<string, string> {
                    ["native-movement-traffic.json"] = FrameworkProofContent.Read("native-movement-traffic.json"),
                    ["native-traffic-clearance-probe.json"] = FrameworkProofContent.Read("native-traffic-clearance-probe.json"),
                    ["native-survey-navigation.json"] = "{\"uniform_tile_navigation\":[{\"id\":\"survey.route\",\"space\":\"survey.tiles\",\"cost\":15}]}" },
                additionalModules: probe ? new GameModule[] { new TrafficClearanceProbeModule() } : null);
        }
        private static Entity[] Vehicles(SimGame game) => game.World.Entities.All
            .Where(e => e.Def.Id == "survey.machine").OrderBy(e => e.Id).ToArray();
        private static string Diagnostic(SimGame game) => string.Join(";", Vehicles(game).Select(e => {
            var mover = e.Get<Mover>(); return e.Id + "/" + e.Get<ITilePresence>().Position + "/" + mover.Status +
                "/goal=" + mover.Goal + "/index=" + mover.PathIndex + "/path=" + string.Join(",", mover.Path.Skip(Math.Max(0, mover.PathIndex)).Take(5)); }));
        private static void HomeTraffic(Check c)
        {
            var factory = Factory(homeTraffic: true); var game = LiveSensorTests.New(factory); var vehicles = Vehicles(game);
            var isolated = LiveSensorTests.New(factory); ulong isolatedHash = isolated.Hash();
            var limits = new ReplayLimits(2 * 1024 * 1024, 1000); using var recorder = game.RecordReplay(limits);
            for (int i = 0; i < 4; i++) game.Update();
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = vehicles[0].Id, To = new TileCoord(6, 11) }).Accepted,
                "outbound movement meets an already returning worker through normal player command");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual fractional home and outbound routes restore identically");
            TileRect retained = vehicles[1].Get<IMobility>().Goal; int retargets = 0; bool savedWaiting = false, savedRetarget = false;
            for (int i = 0; i < 240; i++)
            {
                var before = vehicles.Select(e => e.Get<ITilePresence>().Position).ToArray();
                game.Update(); twin.Update();
                if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("home traffic fault/" + game.Fault + "/" + twin.Fault);
                c.Equal(game.Hash(), twin.Hash(), "saved home intent and traffic replan match every update");
                c.That(vehicles[0].Get<ITilePresence>().Tile != vehicles[1].Get<ITilePresence>().Tile, "physical traffic never shares an occupied tile");
                for (int v = 0; v < vehicles.Length; v++)
                    c.That((vehicles[v].Get<ITilePresence>().Position - before[v]).Length <= Fix.FromRatio(20, 60), "movement cannot exceed authored speed budget");
                var returning = vehicles[1].Get<Mover>();
                if (returning.Status == MoveStatus.Moving && returning.Goal != retained)
                { retained = returning.Goal; retargets++; savedRetarget = true; twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "valid alternative parking uses only saved motion intent"); }
                if (!savedWaiting && vehicles.Any(e => e.Get<Mover>().Status == MoveStatus.Moving && e.Get<Mover>().PathIndex < 0))
                { twin = factory.Load(game.Save()); savedWaiting = true; c.Equal(game.Hash(), twin.Hash(), "transiently blocked original route restores without a parking ledger"); }
                ulong observed = game.Hash(); var home = game.World.Entities.All.Single(e => e.Def.Id == "traffic.home.base");
                game.World.Services.Get<WorkBoard>().ReadHomeParking(home, vehicles[1], out _, out _, out _);
                c.Equal(observed, game.Hash(), "retained parking query is pure while traffic moves");
                if (i % 15 == 0) Console.WriteLine("HOME_TRAFFIC " + game.Tick + "/" + Diagnostic(game));
                if (vehicles[0].Get<IMotion>().Status == MoveStatus.Arrived && WorkerHomeContracts.Read(game.World, vehicles[1]).State == WorkerHomeState.AtHome) break;
            }
            c.Equal(MoveStatus.Arrived, vehicles[0].Get<IMotion>().Status, "outbound movement physically arrives, not cyclic moving state");
            c.Equal(WorkerHomeState.AtHome, WorkerHomeContracts.Read(game.World, vehicles[1]).State, "returning worker physically parks");
            c.Equal(FixVec2.Centre(new TileCoord(6, 11)), vehicles[0].Get<ITilePresence>().Position, "outbound movement reaches actual destination centre");
            c.Equal(new TileCoord(10, 18), vehicles[1].Get<ITilePresence>().Tile, "actual occupied first parking permits one valid alternative");
            c.That(savedWaiting && savedRetarget, "actual temporary blockage and changed parking intent both persisted");
            c.Equal(1, retargets, "valid alternative is retained instead of cyclically chasing the outbound worker");
            c.Equal(isolatedHash, isolated.Hash(), "interleaved independent session retains its own clock and home state");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "ordinary player command replay preserves traffic and parking/" + replay.Failure);
        }
        private static void Run(Check c)
        {
            foreach (bool half in new[] { false, true }) foreach (bool reverse in new[] { false, true })
                Journey(c, half, reverse);
        }
        private static void Crossing(Check c)
        {
            foreach (bool half in new[] { false, true }) foreach (bool reverse in new[] { false, true })
                Journey(c, half, reverse, crossing: true);
            StalledCrossing(c);
        }
        private static void StalledCrossing(Check c)
        {
            var factory = Factory(half: true, crossing: true, controlled: true);
            var game = LiveSensorTests.New(factory); var vehicles = Vehicles(game);
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = vehicles[0].Id, To = new TileCoord(7, 9) }).Accepted, "crossing starts with normal movement intent");
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = vehicles[1].Id, To = new TileCoord(3, 1) }).Accepted, "other crossing vehicle starts a real segment");
            c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = vehicles[1].Id, Operation = new Code("travel"), Enabled = false, Target = Fix.One }).Accepted,
                "normal player control suspends actual movement without changing its route intent");
            FixVec2 stopped = vehicles[1].Get<ITilePresence>().Position;
            c.Equal(MoveStatus.Moving, vehicles[1].Get<IMotion>().Status, "suspended motion keeps actual intent, not a fake idle status");
            var twin = factory.Load(game.Save());
            for (int i = 0; i < 240 && vehicles[0].Get<IMotion>().Status == MoveStatus.Moving; i++)
            {
                game.Update(); twin.Update();
                if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("stalled crossing fault/" + game.Fault + "/" + twin.Fault);
                c.Equal(game.Hash(), twin.Hash(), "saved waiting qualification remains deterministic through a disabled peer");
                c.Equal(stopped, vehicles[1].Get<ITilePresence>().Position, "disabled vehicle never moves to rescue traffic");
                c.That(vehicles[0].Get<ITilePresence>().Tile != vehicles[1].Get<ITilePresence>().Tile, "passing a disabled peer preserves physical occupancy");
            }
            c.Equal(MoveStatus.Arrived, vehicles[0].Get<IMotion>().Status, "right of way never waits indefinitely for an execution-blocked peer/" + Diagnostic(game));
            c.Equal(FixVec2.Centre(new TileCoord(7, 9)), vehicles[0].Get<ITilePresence>().Position, "active vehicle really routes around disabled peer");
        }
        private static void Journey(Check c, bool half, bool reverse, bool crossing = false)
        {
            var factory = Factory(half, crossing: crossing); var game = LiveSensorTests.New(factory); var vehicles = Vehicles(game);
            var isolated = LiveSensorTests.New(factory); ulong isolatedHash = isolated.Hash();
            var limits = new ReplayLimits(2 * 1024 * 1024, 1000);
            using var recorder = game.RecordReplay(limits);
            c.Equal(2, vehicles.Length, "authored ordinary native movers");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "native traffic simulation identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "clearance derives from original route codec");
            c.That(!game.Modules.Contains("sim.tycoon"), "native movement has no business requirement");
            var goals = crossing ? new[] { new TileCoord(7, 9), new TileCoord(3, 1) } : new[] { new TileCoord(6, 4), new TileCoord(9, 11) };
            foreach (int index in reverse ? new[] { 1, 0 } : new[] { 0, 1 })
                c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = vehicles[index].Id, To = goals[index] }).Accepted,
                    "normal player movement admitted");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual partial native routes save identically");
            var tail = new Queue<string>();
            for (int i = 0; i < 240 && vehicles.Any(e => e.Get<IMotion>().Status == MoveStatus.Moving); i++)
            {
                game.Update(); if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString());
                twin.Update(); if (twin.Fault != null) throw new InvalidOperationException(twin.Fault.ToString());
                c.Equal(game.Hash(), twin.Hash(), "every saved native traffic update matches");
                if (i == 10) { twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "replanning/segment state saves again mid-journey"); }
                if (i % 20 == 0) { tail.Enqueue(game.Tick + "/" + Diagnostic(game)); if (tail.Count > 8) tail.Dequeue(); }
                c.That(vehicles[0].Get<ITilePresence>().Tile != vehicles[1].Get<ITilePresence>().Tile,
                    "physical blocking occupants never share a tile");
            }
            Console.WriteLine("TRAFFIC crossing=" + crossing + " half=" + half + " reverse=" + reverse + " " + string.Join(" | ", tail));
            c.That(vehicles.All(e => e.Get<IMotion>().Status == MoveStatus.Arrived),
                "both physically arrive within240Updates/" + Diagnostic(game));
            for (int i = 0; i < vehicles.Length; i++)
            {
                c.Equal(FixVec2.Centre(goals[i]), vehicles[i].Get<ITilePresence>().Position, "arrival is actual goal centre, not reported progress");
                c.That(vehicles[i].Get<ITileMotionClearance>().ReadMotionClearance() == null, "arrived route retains no motion clearance");
            }
            c.Equal(isolatedHash, isolated.Hash(), "interleaved session has no traffic or clock mutation");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success && replay.Game != null && replay.ActualHash == game.Hash(), "normal command stream replays physical traffic identically/" + replay.Failure);
        }
        private static void Clearance(Check c)
        {
            foreach (bool diagonal in new[] { false, true })
            {
                var factory = Factory(); var game = LiveSensorTests.New(factory); var vehicles = Vehicles(game);
                var first = vehicles[0]; var second = vehicles[1];
                c.That(first.Get<ITileMotionClearance>().ReadMotionClearance() == null, "idle motion reserves no future cells");
                var goal = diagonal ? new TileCoord(10, 10) : new TileCoord(12, 8);
                c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = first.Id, To = goal }).Accepted, "ordinary player starts actual partial segment");
                var motion = first.Get<Mover>(); var presence = first.Get<ITilePresence>();
                var next = motion.Path[motion.PathIndex]; var grid = game.World.Services.Get<NavService>().Grid(motion.MovementClass);
                ulong hash = game.Hash(); long time = game.Tick;
                TileRect? cells = motion.ReadMotionClearance();
                c.That(cells.HasValue && cells.Value.Contains(next) && cells.Value.Contains(presence.Tile), "in-flight clearance derives from actual start/next route tiles");
                c.Equal(diagonal ? 2 : 1, cells.Value.H, "diagonal sweep includes native corner cells");
                var freeCells = new List<TileCoord>();
                for (int y = cells.Value.Y; y < cells.Value.Y + cells.Value.H; y++)
                    for (int x = cells.Value.X; x < cells.Value.X + cells.Value.W; x++)
                    {
                        var tile = new TileCoord(x, y);
                        c.Equal(0, grid.CostFor(second, tile), "other mover cannot enter actual sweep, including corners");
                        c.Equal(15, grid.StructuralCostFor(second, tile), "prospective structural feasibility ignores temporary traffic");
                        c.Equal(15, grid.CostFor(first, tile), "mover never blocks its own partial segment");
                        if (tile != presence.Tile) freeCells.Add(tile);
                    }
                c.Equal(hash, game.Hash(), "clearance and guarded dynamic/structural queries are pure");
                c.Equal(time, game.Tick, "clearance owns no second clock");
                var twin = factory.Load(game.Save());
                c.That(cells.Equals(Vehicles(twin)[0].Get<ITileMotionClearance>().ReadMotionClearance()), "existing native save rederives exact segment clearance");
                game.Clock.Pause(); twin.Clock.Pause(); game.Update(); twin.Update();
                c.Equal(time, game.Tick, "paused one clock preserves actual segment");
                c.Equal(game.Hash(), twin.Hash(), "paused saved traffic remains identical");
                game.Clock.Resume(); twin.Clock.Resume();
                var stop = new MoveVehicleCommand { Vehicle = first.Id, To = presence.Tile };
                c.That(TestKit.Order(game, 0, stop).Accepted, "ordinary player stops at actual current tile");
                c.That(TestKit.Order(twin, 0, new MoveVehicleCommand { Vehicle = first.Id, To = stop.To }).Accepted, "saved twin receives same stop intent");
                c.That(motion.ReadMotionClearance() == null, "stop releases only derived motion clearance");
                foreach (var tile in freeCells) c.Equal(15, grid.CostFor(second, tile), "released unoccupied sweep is immediately usable");
                c.Equal(game.Hash(), twin.Hash(), "no hidden reservations survive stop/save continuation");
            }
        }
        private static void Contracts(Check c)
        {
            for (int fault = 1; fault <= 7; fault++)
            {
                var game = LiveSensorTests.New(Factory(probe: true));
                var first = Vehicles(game)[0];
                var entity = game.World.Entities.All.Single(e => e.Def.Id == "traffic.probe");
                entity.Get<TrafficClearanceProbe>().Arm(game.World, fault);
                var grid = game.World.Services.Get<NavService>().Grid("survey.route"); var tile = new TileCoord(9, 8);
                if (fault == 1)
                {
                    ulong hash = game.Hash();
                    c.Equal(0, grid.CostFor(first, tile), "non-native contract blocks an actual in-flight adjacent cell");
                    c.Equal(15, grid.StructuralCostFor(first, tile), "independent temporary claim never becomes structural obstruction");
                    c.Equal(hash, game.Hash(), "independent clearance observation is pure");
                }
                else
                {
                    Exception thrown = null;
                    try { grid.CostFor(first, tile); } catch (Exception error) { thrown = error; }
                    c.That(thrown != null, "clearance violation exposes no entry cost/fault=" + fault);
                    c.That(game.Fault != null, "guarded failure latches the actual session/fault=" + fault);
                    var original = game.Fault; long time = game.Tick; game.Update();
                    c.That(ReferenceEquals(original, game.Fault) && game.Tick == time, "faulted clearance cannot resume physical work/fault=" + fault);
                    if (fault == 2) c.That(thrown?.Message == "proof.traffic-clearance", "original provider error is retained");
                }
            }
        }
        private static void Choke(Check c)
        {
            var factory = Factory(choke: true); var game = LiveSensorTests.New(factory); var vehicles = Vehicles(game);
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = vehicles[0].Id, To = new TileCoord(8, 4) }).Accepted, "trailing vehicle receives normal currently reachable command");
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = vehicles[1].Id, To = new TileCoord(8, 3) }).Accepted, "leading vehicle receives normal shared-choke command");
            bool waiting = false; var twin = factory.Load(game.Save());
            for (int i = 0; i < 160 && vehicles.Any(e => e.Get<IMotion>().Status == MoveStatus.Moving); i++)
            {
                var before = vehicles.Select(e => e.Get<ITilePresence>().Position).ToArray();
                game.Update(); twin.Update();
                if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("traffic choke fault/" + game.Fault + "/" + twin.Fault);
                c.Equal(game.Hash(), twin.Hash(), "saved physical choke travel is deterministic");
                for (int v = 0; v < vehicles.Length; v++)
                {
                    var mover = vehicles[v].Get<Mover>();
                    if (mover.Status != MoveStatus.Moving) continue;
                    if (mover.PathIndex < 0 || vehicles[v].Get<ITilePresence>().Position == before[v] &&
                        mover.PathIndex < mover.Path.Count && game.World.Services.Get<NavService>().Grid(mover.MovementClass)
                            .CostFor(vehicles[v], mover.Path[mover.PathIndex]) <= 0) waiting = true;
                }
            }
            Console.WriteLine("TRAFFIC_CHOKE " + Diagnostic(game));
            c.That(waiting, "temporary physical obstruction actually exercised a pending route or retained blocked physical wait");
            c.That(vehicles.All(e => e.Get<IMotion>().Status == MoveStatus.Arrived), "temporary traffic never becomes permanent unreachable/" + Diagnostic(game));
            // Explicit framework topology fault injection, not a player grant or a host construction rule.
            // Close the actual sole passage AFTER an ordinary reachable command has been accepted.
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = vehicles[0].Id, To = new TileCoord(8, 9) }).Accepted, "same engine accepts actual open return route");
            var gap = new TileCoord(8, 6);
            game.World.Spawn(game.World.Content.Get<EntityDef>("traffic.plug"), new PlayerId(0),
                created => created.Get<ITilePresence>().Place(gap, FixVec2.Centre(gap)));
            for (int i = 0; i < 24 && vehicles[0].Get<IMotion>().Status == MoveStatus.Moving; i++)
            { game.Update(); if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString()); }
            c.Equal(MoveStatus.Unreachable, vehicles[0].Get<IMotion>().Status, "actual static disconnection is not an infinite traffic wait");
            c.That(vehicles[0].Get<ITileMotionClearance>().ReadMotionClearance() == null, "static failure leaves no obsolete clearance");
        }
    }
}
