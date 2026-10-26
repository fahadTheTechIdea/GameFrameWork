using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static partial class ProducerHomeBerthTests
    {
        internal const string LoadedParkingCase = "Producer home: occupied parking retains loaded replenishment through saved physical clearance";
        internal const string LoadedWideParkingCase = "Producer home: wide machines retain sixty-unit cargo through occupied berth recovery";
        private static EntityId DeliveryEntity(SimGame game, string prototype)
            => game.Seat(0).Snapshot.Entities.Single(e => e.Def == prototype).Id;
        private static TileSpatialSnapshot DeliveryPosition(SimGame game, string prototype)
            => (TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Def == prototype).Spatial;
        private static List<WorkJobView> DeliveryJobs(SimGame game)
        { var result = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(result); return result; }
        private static bool DeliveryLive(WorkJobView job)
            => job.State == JobState.Open || job.State == JobState.Assigned || job.State == JobState.Paused;
        private static LogisticsOperationalSnapshot DeliveryStock(SimGame game, string prototype)
        {
            if (!game.Seat(0).View.TryFindResource("steel", out var steel)) throw new InvalidOperationException("actual steel metadata missing");
            Code refusal = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(DeliveryEntity(game, prototype), steel, out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("actual delivery stock unavailable/" + prototype + "/" + refusal);
            return facts;
        }
        private static TransformationSnapshot DeliveryProcess(SimGame game)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(
                DeliveryEntity(game, "berth.producer"), new Code("production"), out var value))
                throw new InvalidOperationException("actual producer unavailable");
            return value;
        }
        private static WorkerHomeView DeliveryHome(SimGame game)
        {
            Code refusal = game.Seat(0).View.Feature<WorkView>().ReadHome(DeliveryEntity(game, "berth.observer"), out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual output home unavailable/" + refusal);
            return value;
        }
        private static void DeliveryStep(Check c, SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("loaded berth fault/" + game.Fault + "/" + twin?.Fault);
            if (twin != null) c.Equal(game.Hash(), twin.Hash(), "saved loaded delivery and physical home clearance advance identically");
            var occupied = game.Seat(0).Snapshot.Entities.Select(e => e.Spatial).OfType<TileSpatialSnapshot>().Where(p => p.Movable).ToArray();
            for (int i = 0; i < occupied.Length; i++) for (int j = i + 1; j < occupied.Length; j++)
                c.That(!occupied[i].Footprint.Overlaps(occupied[j].Footprint), "complete parking output, blocker and loaded carrier footprints never overlap");
        }
        private static void DeliveryUntil(Check c, SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 128; i++)
            {
                if (ready()) return;
                if (i < 32 && i % 8 == 0) Console.WriteLine("LOADED_BERTH " + game.Clock.StepIndex + "/" + stage + "/" +
                    string.Join(";", game.Seat(0).Snapshot.Entities.Where(e => e.Spatial is TileSpatialSnapshot p && p.Movable)
                        .Select(e => e.Def + "/" + ((TileSpatialSnapshot)e.Spatial).Position)) +
                    "/home=" + (game.Seat(0).Snapshot.Entities.Any(e => e.Def == "berth.observer") ? DeliveryHome(game).State + "/" + DeliveryHome(game).Blocker : "not-produced") +
                    "/jobs=" + string.Join(";", DeliveryJobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/" + j.Loaded)));
                DeliveryStep(c, game, twin);
            }
            throw new InvalidOperationException(stage + "/at=" + game.Clock.StepIndex + "/queue=" + DeliveryProcess(game).QueueCount +
                "/cargo=" + DeliveryStock(game, "berth.carrier").Stock + "/jobs=" +
                string.Join(";", DeliveryJobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/" + j.Loaded + "/" + j.ToilIndex)));
        }
        private static void LoadedParking(Check c)
        {
            var factory = Factory(root => {
                root.Get("entities")[3].Get("components").Get("mover").Set("speed", JsonValue.Number("6"));
                var blocker = root.Get("entities")[2].Clone(); blocker.Set("id", JsonValue.Of("berth.parking-blocker"));
                root.Get("entities").Add(blocker);
                root.Get("tile_layer_setups").Add(Json.Parse("{\"id\":\"berth.clearance.start\",\"after\":[],\"layer\":\"berth.channel\",\"x\":6,\"y\":6,\"width\":1,\"height\":1,\"value\":1,\"owner\":-1}"));
                root.Get("tile_layer_setups").Add(Json.Parse("{\"id\":\"berth.bypass.start\",\"after\":[],\"layer\":\"berth.channel\",\"x\":7,\"y\":5,\"width\":1,\"height\":1,\"value\":1,\"owner\":-1}"));
                root.Get("entity_setups").Add(Json.Parse("{\"id\":\"berth.blocker.start\",\"after\":[\"berth.producer.start\",\"berth.clearance.start\"],\"entity\":\"berth.parking-blocker\",\"company\":0,\"placement\":{\"space\":\"survey.tiles\",\"x\":6,\"y\":6},\"home\":null}"));
                var entries = root.Get("world_setup_profiles")[0].Get("entries");
                entries.Add(JsonValue.Of("berth.clearance.start")); entries.Add(JsonValue.Of("berth.blocker.start"));
                entries.Add(JsonValue.Of("berth.bypass.start"));
            });
            RunLoadedParking(c, factory, new TileCoord(8, 5), new TileCoord(7, 6), new TileCoord(7, 6), new TileCoord(6, 6), Fix.FromInt(4), Fix.FromInt(20), 128);
        }
        private static void LoadedWideParking(Check c)
        {
            var factory = Factory(document: "producer-home-wide-berth.json");
            var probe = LiveSensorTests.New(factory); ulong before = probe.Hash();
            c.That(DeliveryPosition(probe, "berth.carrier").Footprint.W == 2 && DeliveryPosition(probe, "berth.carrier").Footprint.H == 2,
                "actual authored carrier occupies all four cells, not a one-tile proxy");
            var samples = new List<int>();
            c.Equal(SpatialLayerReadStatus.Success, probe.Seat(0).View.Feature<TileMapView>().ReadLayer("berth.channel", new TileRect(23, 10, 2, 1), samples),
                "actual edge terrain facts are public");
            c.That(samples.SequenceEqual(new[] { 1, 0 }), "origin terrain is passable while another occupied cell is not");
            c.Equal(new Code("core.unreachable"), probe.Seat(0).Preview(new MoveVehicleCommand { Vehicle = DeliveryEntity(probe, "berth.carrier"), To = new TileCoord(23, 10) }),
                "whole footprint refuses a blocked non-origin terrain cell");
            c.Equal(before, probe.Hash(), "whole-footprint terrain refusal is pure");
            var boundary = LiveSensorTests.New(Factory(root => root.Get("tile_layer_setups")[0].Set("width", JsonValue.Number("36")), document: "producer-home-wide-berth.json"));
            before = boundary.Hash(); samples.Clear();
            c.Equal(SpatialLayerReadStatus.Success, boundary.Seat(0).View.Feature<TileMapView>().ReadLayer("berth.channel", new TileRect(47, 10, 1, 1), samples), "actual map edge terrain");
            c.Equal(1, samples.Single(), "map edge origin is really passable");
            c.Equal(new Code("core.unreachable"), boundary.Seat(0).Preview(new MoveVehicleCommand { Vehicle = DeliveryEntity(boundary, "berth.carrier"), To = new TileCoord(47, 10) }),
                "whole footprint cannot extend past authored world bounds");
            c.Equal(before, boundary.Hash(), "whole-footprint bounds refusal is pure");
            RunLoadedParking(c, factory, new TileCoord(20, 7), new TileCoord(16, 11), new TileCoord(17, 11),
                new TileCoord(12, 11), Fix.FromInt(60), Fix.FromInt(100), 512);
        }
        private static void RunLoadedParking(Check c, GameFactory factory, TileCoord service, TileCoord parking, TileCoord obstruction, TileCoord clearance,
            Fix amount, Fix initialStock, int recoveryUpdates)
        {
            var game = LiveSensorTests.New(factory); var isolated = LiveSensorTests.New(factory); ulong isolatedHash = isolated.Hash();
            var limits = new ReplayLimits(2 * 1024 * 1024, 1000); using var recorder = game.RecordReplay(limits);
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = DeliveryEntity(game, "berth.producer"),
                Capability = new Code("production"), Transformation = "berth.worker", Count = 1 }).Accepted, "ordinary manufacturing earns output before obstruction");
            DeliveryUntil(c, game, null, () => game.Seat(0).Snapshot.Entities.Any(e => e.Def == "berth.observer") &&
                DeliveryHome(game).State == WorkerHomeState.AtHome, "manufactured machine physically parks before player redirection");
            c.That(game.Seat(0).View.TryFindResource("steel", out var steel), "actual supply metadata");
            var supply = new HaulResourcesCommand { Source = DeliveryEntity(game, "berth.source"),
                Destination = DeliveryEntity(game, "berth.producer"), SourcePort = new Code("stock"), DestinationPort = new Code("stock"),
                Worker = DeliveryEntity(game, "berth.carrier"), Resource = steel, Ceiling = amount, Priority = 100 };
            c.Equal(Code.None, game.Seat(0).Preview(supply), "actual full supply journey preview");
            c.That(TestKit.Order(game, 0, supply).Accepted,
                "player dispatches finite supplies while actual service approach is still clear");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = DeliveryEntity(game, "berth.producer"),
                Capability = new Code("production"), Transformation = "berth.product", Count = 1 }).Accepted, "later production uses original physical supply commitment");
            DeliveryUntil(c, game, null, () => DeliveryStock(game, "berth.carrier").Stock == amount,
                "physical full finite loading before later player obstruction");
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = DeliveryEntity(game, "berth.observer"), To = service }).Accepted,
                "player redirects real manufactured machine into previously clear service access");
            DeliveryUntil(c, game, null, () => DeliveryPosition(game, "berth.observer").Position == FixVec2.Centre(service),
                "player-controlled output fully clears parking and physically reaches service");
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = DeliveryEntity(game, "berth.parking-blocker"), To = obstruction }).Accepted,
                "another real machine physically occupies vacated parking");
            DeliveryUntil(c, game, null, () => DeliveryPosition(game, "berth.observer").Position == FixVec2.Centre(service) &&
                DeliveryPosition(game, "berth.parking-blocker").Position == FixVec2.Centre(obstruction), "both player moves actually arrive");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = DeliveryEntity(game, "berth.observer"), Held = false }).Accepted,
                "ordinary player release returns original producer home intent to engine");
            DeliveryUntil(c, game, null, () => DeliveryStock(game, "berth.carrier").Stock == amount && DeliveryHome(game).State == WorkerHomeState.Blocked,
                "loaded replenishment behind occupied home parking");
            var retained = DeliveryJobs(game).Single(j => j.IsHaul && DeliveryLive(j)); int jobId = retained.Id;
            c.Equal(amount, retained.Loaded, "actual stock is aboard original carrier before clearance");
            c.Equal(DeliveryEntity(game, "berth.producer"), retained.Destination, "loaded job retains actual producer");
            c.Equal(WorkerHomeState.Blocked, DeliveryHome(game).State, "output cannot invent a parking position");
            c.Equal(WorkDockCodes.Occupied, DeliveryHome(game).Blocker, "parking blockage is explicit engine fact");
            c.Equal(Fix.Zero, DeliveryStock(game, "berth.producer").Stock, "loaded cargo has not arrived remotely");
            c.Equal(amount, DeliveryStock(game, "berth.producer").InboundPromised, "original full inbound receipt remains protected");
            var twin = factory.Load(game.Save());
            var output = DeliveryPosition(game, "berth.observer").Position;
            for (int i = 0; i < 12; i++)
            {
                DeliveryStep(c, game, twin);
                c.Equal(amount, DeliveryJobs(game).Single(j => j.Id == jobId).Loaded, "blocked original job retains full physical cargo");
                c.Equal(amount, DeliveryStock(game, "berth.producer").InboundPromised, "blocked original inbound receipt remains exact");
                c.Equal(initialStock, DeliveryStock(game, "berth.carrier").Stock + DeliveryStock(game, "berth.source").Stock +
                    DeliveryStock(game, "berth.producer").Stock, "blocked physical stock never disappears or duplicates");
            }
            c.Equal(output, DeliveryPosition(game, "berth.observer").Position, "occupied parking never teleports output or switches to service fallback");
            c.Equal(amount, DeliveryStock(game, "berth.carrier").Stock, "waiting loaded delivery retains every finite unit");
            c.Equal(retained.DestinationEndpoint, DeliveryJobs(game).Single(j => j.Id == jobId).DestinationEndpoint, "blocked saved job retains original endpoint identity");
            var command = new MoveVehicleCommand { Vehicle = DeliveryEntity(game, "berth.parking-blocker"), To = clearance };
            game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash();
            c.That(game.Update(1000) == 0 && twin.Update(1000) == 0 && game.Hash() == paused && twin.Hash() == paused,
                "sole paused clock freezes loaded promise, obstructed home and physical cargo");
            game.Clock.Resume(); twin.Clock.Resume();
            ulong before = game.Hash(); c.Equal(Codes.NotOwned, game.Seat(1).Preview(command), "another company cannot clear visible owned parking");
            c.Equal(before, game.Hash(), "foreign refusal is pure");
            c.That(TestKit.Order(game, 0, command).Accepted, "player orders physical clearance, never instant relocation");
            c.That(DeliveryPosition(game, "berth.parking-blocker").Position != FixVec2.Centre(clearance), "clearance consumes real movement time");
            twin = factory.Load(game.Save());
            bool partialUnloadSaved = false, parkingMotionSaved = false, complete = false;
            for (int i = 0; i < recoveryUpdates; i++)
            {
                DeliveryStep(c, game, twin);
                var currentJobs = DeliveryJobs(game).Where(j => j.IsHaul && DeliveryLive(j)).ToArray();
                c.That(currentJobs.All(j => j.Id == jobId && j.Worker == retained.Worker && j.Amount == retained.Amount &&
                    j.SourceEndpoint == retained.SourceEndpoint && j.DestinationEndpoint == retained.DestinationEndpoint),
                    "recovery preserves original loaded commitment, not a cancellation or substitute job");
                var process = DeliveryProcess(game);
                Fix stock = DeliveryStock(game, "berth.source").Stock + DeliveryStock(game, "berth.carrier").Stock +
                    DeliveryStock(game, "berth.producer").Stock + process.Wip.Stock.AmountOf(steel);
                c.Equal(process.QueueCount == 0 ? initialStock - amount + Fix.One : initialStock, stock, "physical raw stock plus protected WIP reconcile actual once-only conversion");
                if (!partialUnloadSaved && currentJobs.Any(j => j.Loaded.IsPositive && j.Loaded < amount))
                { twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "original residual receipt and delivered prefix restore during partial unloading"); partialUnloadSaved = true; }
                if (!parkingMotionSaved && DeliveryHome(game).State == WorkerHomeState.Returning)
                { twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual output home movement restores during clearance"); parkingMotionSaved = true; }
                if (process.QueueCount == 0 && !DeliveryJobs(game).Any(DeliveryLive) && DeliveryHome(game).State == WorkerHomeState.AtHome)
                { complete = true; break; }
            }
            c.That(complete, "bounded actual loaded delivery, departure, home and subsequent production complete");
            c.That(partialUnloadSaved && parkingMotionSaved, "partial physical unloading and output return are both observed and saved");
            c.Equal(Fix.Zero, DeliveryStock(game, "berth.carrier").Stock, "original finite cargo delivered once");
            c.Equal(Fix.Zero, DeliveryStock(game, "berth.producer").InboundPromised, "completed delivery releases original receipt");
            c.Equal(Fix.Zero, DeliveryStock(game, "berth.source").OutboundPromised, "source has no orphan promise");
            c.Equal(Fix.Zero, DeliveryStock(game, "berth.carrier").OutboundPromised, "carrier has no orphan cargo promise");
            c.That(game.Seat(0).View.Feature<InventoryView>().TryGetReservations(DeliveryEntity(game, "berth.producer"), new Code("stock"), out var claims) &&
                claims.Count == 0, "original producer endpoint has no remaining claim records");
            c.Equal(initialStock - amount + Fix.One, DeliveryStock(game, "berth.producer").Stock + DeliveryStock(game, "berth.source").Stock,
                "authored finite steel minus consumed input plus1 produced, no grants or duplicate result");
            c.Equal(parking, DeliveryPosition(game, "berth.observer").Tile, "actual output eventually occupies authored home parking");
            for (int i = 0; i < 12; i++) DeliveryStep(c, game, twin);
            c.Equal(Fix.One, DeliveryStock(game, "berth.producer").Stock, "terminal updates cannot repeat production");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "fully settled combined workflow restores");
            c.Equal(isolatedHash, isolated.Hash(), "physical parking recovery does not mutate independent session");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "ordinary player manufacturing and clearance replay identically/" + replay.Failure);
        }
    }
}
