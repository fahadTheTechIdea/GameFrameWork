using System;
using System.Diagnostics;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class CapacityLogisticsStressTests
    {
        // Pure JSON setup and player inputs. The engine owns routing, reservations,
        // handling, physical travel, completion, observation, saves and replay.
        private static void ByteLayerSupply(Check c)
        {
            var profile = new Profile();
            var factory = Factory(profile, 2, true, false, configure: root => {
                string unit = root.Get("units")[0].Get("id").Text;
                root.Set("spatial_layers", Json.Parse("[{\"id\":\"stress.binary\",\"space\":\"stress.tiles\",\"type\":\"terrain.binary\",\"value_type\":\"byte\",\"visibility\":\"public\",\"unit\":\"" + unit + "\"}]"));
                var setups = JsonValue.NewArray(); var entries = root.Get("world_setup_profiles")[0].Get("entries");
                for (int x = 1; x < 72; x += 2)
                {
                    string id = "stress.binary." + x;
                    setups.Add(Json.Parse("{\"id\":\"" + id + "\",\"after\":[],\"layer\":\"stress.binary\",\"x\":" + x + ",\"y\":0,\"width\":1,\"height\":20,\"value\":3,\"owner\":-1}"));
                    entries.Add(JsonValue.Of(id));
                }
                root.Set("tile_layer_setups", setups);
            });
            var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
            c.That(!game.World.Services.Has<IEconomy>(), "guarded physical supply does not require Tycoon");
            var source = Entity(game, "source"); var target = Entity(game, "stress.destination.0");
            if (!game.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("authored supply unavailable");
            Fix opening = Fix.FromInt(2 * profile.Rounds * profile.Quantity);
            var limits = new ReplayLimits(4 * 1024 * 1024, profile.MaxUpdates);
            using var recording = game.RecordReplay(limits);
            long elapsed = 0, allocated = 0; int updates = 0;
            for (int i = 0; i < 2; i++)
            {
                var command = new HaulResourcesCommand { Source = source, Destination = target,
                    Worker = Entity(game, "stress.carrier." + i), Resource = resource, Ceiling = Fix.FromInt(profile.Quantity), Priority = 10 };
                c.Equal(Code.None, game.Seat(0).Preview(command), "actual guarded routes admit authored supply");
                game.Seat(0).Submit(command);
            }
            Step();
            c.Equal(2, Jobs(game).Count, "two ordinary commands create two actual reserved haul jobs");
            c.That(Jobs(game).All(j => j.State == JobState.Assigned), "both actual workers are assigned");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "active guarded supply saves through original codecs");
            bool carried = false, moved = false;
            for (int i = 0; i < profile.MaxUpdates && Jobs(game).Any(j => j.State != JobState.Done); i++)
            {
                Step(); twin.Update();
                if (twin.Fault != null) throw new InvalidOperationException(twin.Fault.ToString());
                c.Equal(game.Hash(), twin.Hash(), "supplied physical movement retains saved twin identity");
                carried |= Jobs(game).Any(j => j.Loaded > Fix.Zero);
                moved |= Enumerable.Range(0, 2).Any(n => ((TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == Entity(game, "stress.carrier." + n)).Spatial).Footprint.X > 2);
                Accounting(c, game, opening, resource);
            }
            c.That(updates < profile.MaxUpdates && Jobs(game).All(j => j.State == JobState.Done), "supplied actual hauls finish within original authored update bound");
            c.That(carried && moved, "real cargo and real travel occur, never injected delivery");
            c.Equal(Fix.FromInt(2 * profile.Quantity), PermanentRemovalTests.Facts(game, target).Stock, "both whole loads physically reach original destination");
            c.Equal(isolated, idle.Hash(), "guarded busy-world observation does not alter another session");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "normal player commands replay actual physical supply");
            Console.WriteLine(FormattableString.Invariant($"guarded byte supply;build={EngineInfo.Build};updates={updates};update-ms={(double)elapsed * 1000 / Stopwatch.Frequency:F3};update-bytes={allocated};clock={game.Clock.StepIndex};hash={game.Hash():X16}"));

            void Step()
            {
                long before = GC.GetAllocatedBytesForCurrentThread(), started = Stopwatch.GetTimestamp();
                game.Update(); elapsed += Stopwatch.GetTimestamp() - started;
                allocated += GC.GetAllocatedBytesForCurrentThread() - before; updates++;
                if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString());
            }
        }
    }
}
