using System;
using System.Numerics;
using System.Text;
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
    internal static class MotionArrivalTests
    {
        internal static void Register()
        {
            TestRunner.Add("Motion arrival: contract estimates, private admission and provider guards", Admission);
            TestRunner.Add("Motion arrival: physical route timing, gates and saved continuation", Physical);
        }
        private static GameFactory Factory(string fault = null, string rate = null, bool powered = false, bool unestimated = false)
        {
            var root = Content.Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            var parts = root.Get("entities")[2].Get("components");
            parts.Get("worker").Set("work_rate", JsonValue.Number("1"));
            parts.Set("worker_execution", Content.Json.Parse(FrameworkProofContent.Read("worker-execution.json")));
            parts.Set("motion_execution", Content.Json.Parse(FrameworkProofContent.Read("motion-execution.json")));
            if (fault != null) parts.Get("point_motion").Set("estimate_fault", JsonValue.Of(fault));
            if (rate != null) parts.Get("point_motion").Set("rate", JsonValue.Number(rate));
            if (powered)
            {
                parts.Set("operating_inputs", Content.Json.Parse(FrameworkProofContent.Read("motion-operating-inputs.json")));
                parts.Get("storage").Set("initial_stock", Content.Json.Parse("{\"supply\":2}"));
            }
            if (unestimated)
            {
                parts.Remove("worker"); parts.Remove("worker_execution"); parts.Remove("point_motion");
                parts.Set("unestimated_motion", Content.Json.Parse("{\"ticks\":7}"));
            }
            var text = new StringBuilder(); root.WriteCanonical(text); return PointGame.Factory(text.ToString());
        }
        private static SimGame New(GameFactory factory, string scenario = "points")
        {
            var request = new NewGameRequest { Scenario = scenario, Seed = 751 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string prototype)
        { foreach (Entity e in game.World.Entities.All) if (e.Alive && e.Owner == new PlayerId(0) && e.Def.Id == prototype) return e; throw new InvalidOperationException(prototype); }
        private static MotionArrivalView Read(SimGame game, string prototype = "machine")
        {
            Code refusal = game.Seat(0).View.Feature<MotionView>().ReadArrival(Find(game, prototype).Id, out var result);
            if (!refusal.IsNone) throw new InvalidOperationException(refusal.ToString()); return result;
        }
        private static void Start(Check c, SimGame game)
            => c.That(TestKit.Order(game, 0, new SetHomeCommand { Worker = Find(game, "machine").Id, Home = Find(game, "destination").Id }).Accepted,
                "normal engine home command starts actual point travel");
        private static void Control(Check c, SimGame game, bool enabled, Fix target, string operation = "travel")
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = Find(game, "machine").Id, Operation = new Code(operation), Enabled = enabled, Target = target }).Accepted,
                "normal operation command changes actual travel/readiness control");
        private static void Reach(SimGame game, string prototype, SimGame twin = null)
        {
            for (int i = 0; i < 150 && Read(game, prototype).Status != MoveStatus.Arrived; i++) { Tick(game); if (twin != null) Tick(twin); }
            var value = Read(game, prototype);
            if (value.Status != MoveStatus.Arrived) throw new InvalidOperationException("arrival timeout tick=" + game.Tick + " status=" + value.Status + " blocker=" + value.Blocker + " eta=" + value.ArrivalTick);
        }
        private static void Admission(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current forecast identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no extra ETA payload");
            var factory = Factory(); var game = New(factory); var machine = Find(game, "machine"); ulong hash = game.Hash(); var idle = Read(game);
            c.That(idle.Status == MoveStatus.Idle && idle.RemainingTicks == null && idle.ArrivalTick == null && idle.Blocker == MotionArrivalCodes.NotMoving,
                "idle motion never invents a goal or zero arrival");
            c.Equal(hash, game.Hash(), "idle diagnostics are pure");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<MotionView>().ReadArrival(machine.Id, out var hidden), "foreign arrival intent is private");
            c.That(hidden == null, "private refusal leaks no estimate");
            c.Equal(Codes.NotFound, game.Seat(0).View.Feature<MotionView>().ReadArrival(new EntityId(99999), out _), "unknown motion refuses");
            c.Equal(Codes.InvalidArgument, game.Seat(0).View.Feature<MotionView>().ReadArrival(Find(game, "source").Id, out _), "non-motion endpoint gets no fabricated forecast");
            c.Equal(Code.None, game.Observer.Feature<MotionView>().ReadArrival(machine.Id, out _), "explicit observer can inspect owned motion");
            Start(c, game); var moving = Read(game); hash = game.Hash();
            c.That(moving.RemainingTicks == 9 && moving.ArrivalTick == game.Tick + 9 && moving.Blocker.IsNone, "actual point budget produces engine arrival facts");
            c.Equal(machine.Version, moving.EntityVersion, "fact identifies actual entity version"); c.Equal(game.Tick, moving.ObservedTick, "fact identifies observed simulation tick");
            c.Equal(hash, game.Hash(), "actual route forecast changes no stock/goal/age/control/hash");
            Tick(game); c.Equal(9L, moving.RemainingTicks.Value, "retained facts never follow later movement"); c.Equal(8L, Read(game).RemainingTicks.Value, "fresh facts use current actual position");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "forecast reads add no codec authority");
            var unsupported = New(Factory(unestimated: true)); var absent = Read(unsupported);
            c.That(absent.Status == MoveStatus.Moving && absent.ArrivalTick == null && absent.Blocker == MotionArrivalCodes.Unsupported,
                "independent actual IMotion without estimator has explicit unknown duration, never a coordinate fallback");
            c.Equal(1L, MotionArrivalContracts.FromBudget(0, Fix.Zero).RemainingTicks.Value, "already reached moving goal settles in its next actual phase");
            c.Equal(3L, MotionArrivalContracts.FromBudget(5, Fix.FromRaw(2)).RemainingTicks.Value, "raw division ceilings once");
            c.Equal(1L << 40, MotionArrivalContracts.FromBudget(BigInteger.One << 40, Fix.FromRaw(1)).RemainingTicks.Value, "duration has no int-sized limit");
            c.Equal(MotionArrivalCodes.Overflow, MotionArrivalContracts.FromBudget(BigInteger.One << 80, Fix.FromRaw(1)).Blocker, "unrepresentable duration refuses, never saturates");
            c.Equal(MotionArrivalCodes.ZeroBudget, MotionArrivalContracts.FromBudget(1, Fix.Zero).Blocker, "subquantum movement has explicit unknown duration");
            c.Throws<ArgumentOutOfRangeException>(() => MotionArrivalContracts.FromBudget(-1, Fix.One), "negative raw geometry refuses");
            c.Throws<ArgumentOutOfRangeException>(() => MotionArrivalContracts.FromBudget(1, -Fix.One), "negative actual tick budget refuses");
            foreach (string fault in new[] { "touch", "status", "empty", "negative", "zero", "both" })
            {
                var faulty = New(Factory(fault)); Start(c, faulty);
                c.Throws<InvalidOperationException>(() => Read(faulty), "actual public estimate violation refuses usable facts: " + fault);
            }
            var overflow = New(Factory("overflow")); Start(c, overflow);
            c.Equal(MotionArrivalCodes.Overflow, Read(overflow).Blocker, "positive duration with overflowing arrival produces a typed unknown");
            WideTimeTests.SetTick(game, long.MaxValue - 4); hash = game.Hash(); var nearEnd = Read(game);
            c.That(nearEnd.RemainingTicks == null && nearEnd.ArrivalTick == null && nearEnd.Blocker == MotionArrivalCodes.Overflow, "arrival addition cannot wrap near session exhaustion");
            c.Equal(hash, game.Hash(), "overflow forecast changes no actual session state");
            var quantized = New(Factory(rate: "0.00000001")); Start(c, quantized);
            c.Equal(MotionArrivalCodes.ZeroBudget, Read(quantized).Blocker, "positive authored base rate may have zero representable physical tick budget");
            var wideFactory = Factory(rate: "0.0000001"); var wide = New(wideFactory); Start(c, wide); var wideEstimate = Read(wide);
            c.That(wideEstimate.RemainingTicks > int.MaxValue && wideEstimate.ArrivalTick > int.MaxValue && wideEstimate.Blocker.IsNone,
                "actual authored point motion also estimates beyond int-sized session durations");
            var wideTwin = wideFactory.Load(wide.Save());
            c.Equal(wideEstimate.RemainingTicks, Read(wideTwin).RemainingTicks, "actual wide route estimate reconstructs from sole provider codec");
            Tick(wide); Tick(wideTwin); c.Equal(wide.Hash(), wideTwin.Hash(), "wide forecast never narrows actual saved motion time");
        }
        private static void Physical(Check c)
        {
            var factory = Factory(); var game = New(factory); Start(c, game); var first = Read(game); long expected = first.ArrivalTick.Value;
            var twin = factory.Load(game.Save()); Reach(game, "machine", twin);
            c.Equal(expected, game.Tick, "actual point arrival matches its current pure budget estimate"); c.Equal(game.Hash(), twin.Hash(), "saved actual point route continues identically");
            c.That(Read(game).RemainingTicks == 0 && Read(game).ArrivalTick == game.Tick && Read(game).Blocker.IsNone, "actual Arrived alone reports zero remaining ticks");
            c.Equal(Fix.FromInt(10), Find(game, "machine").Get<PointPresence>().Position, "estimate never bypasses physical point travel");
            var changing = New(factory); Start(c, changing); var prior = Read(changing);
            Control(c, changing, true, Fix.Half); var slower = Read(changing);
            c.That(slower.ArrivalTick > prior.ArrivalTick, "actual speed change recalculates remaining duration, not a copied deadline");
            Control(c, changing, false, Fix.One); Fix held = Find(changing, "machine").Get<PointPresence>().Position;
            c.That(Read(changing).ArrivalTick == null && Read(changing).Blocker == OperationCodes.Disabled, "disabled travel retains original gate reason, no invented ETA");
            for (int i = 0; i < 3; i++) Tick(changing); c.Equal(held, Find(changing, "machine").Get<PointPresence>().Position, "disabled estimate earns no physical progress");
            Control(c, changing, true, Fix.Zero); c.Equal(MotionExecutionCodes.ZeroRate, Read(changing).Blocker, "zero multiplier cannot forecast progress");
            Control(c, changing, true, Fix.One); Control(c, changing, false, Fix.One, "availability");
            c.Equal(OperationCodes.Disabled, Read(changing).Blocker, "actual worker readiness also blocks arrival");
            Control(c, changing, true, Fix.One, "availability"); var restored = Read(changing); var changedTwin = factory.Load(changing.Save());
            Reach(changing, "machine", changedTwin); c.Equal(restored.ArrivalTick.Value, changing.Tick, "re-enabled arrival uses current geometry/time"); c.Equal(changing.Hash(), changedTwin.Hash(), "changed controls and current route save once");
            var retarget = New(factory); Start(c, retarget); var before = Read(retarget);
            c.That(TestKit.Order(retarget, 0, new SetHomeCommand { Worker = Find(retarget, "machine").Id, Home = Find(retarget, "source").Id }).Accepted, "ordinary home edit changes actual point goal");
            c.That(Read(retarget).RemainingTicks < before.RemainingTicks, "retarget forecast follows actual provider, not old requested arrival");
            Reach(retarget, "machine"); c.Equal(Fix.Zero, Find(retarget, "machine").Get<PointPresence>().Position, "retarget still executes physical return");
            var closed = New(factory); Start(c, closed); ((PointSpace)closed.World.Space).SetAccess(false); ulong hash = closed.Hash();
            c.That(Read(closed).Status == MoveStatus.Moving && Read(closed).Blocker == MotionArrivalCodes.Unreachable && Read(closed).ArrivalTick == null, "current point access loss returns provider unknown without changing actual motion status");
            c.Equal(hash, closed.Hash(), "unreachable forecast is pure"); ((PointSpace)closed.World.Space).SetAccess(true); Reach(closed, "machine");
            var unreachable = New(factory); Start(c, unreachable); Find(unreachable, "machine").Get<IWorker>().Held = true;
            c.That(TestKit.Order(unreachable, 0, new SetPointAccessCommand { Open = false }).Accepted, "normal spatial command blocks an actual held point route");
            c.That(Read(unreachable).Status == MoveStatus.Unreachable && Read(unreachable).Blocker == MotionArrivalCodes.Unreachable && Read(unreachable).ArrivalTick == null,
                "actual unreachable status never borrows a last valid forecast");
            var independent = New(factory); c.Equal(MotionArrivalCodes.NotMoving, Read(independent).Blocker, "shared JSON never shares current motion or forecast state");
            Powered(c); Tile(c);
        }
        private static void Powered(Check c)
        {
            var factory = Factory(powered: true); var game = New(factory); var machine = Find(game, "machine"); var supply = game.World.Content.ResourceId("supply");
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = machine.Id, Resource = supply,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.Zero, Fix.FromInt(2), Fix.FromInt(8), 1, LogisticsReserveAccess.Protected)) }).Accepted, "normal stock policy retains actual travel fuel");
            Start(c, game); c.That(Read(game).ArrivalTick.HasValue, "currently funded motion has a conditional current-route estimate, not a future fuel guarantee");
            for (int i = 0; i < 30 && Read(game).Blocker.IsNone; i++) Tick(game);
            var empty = Read(game); c.That(!empty.Blocker.IsNone && empty.ArrivalTick == null && empty.RemainingTicks == null, "actual fuel shortage replaces ETA with existing operating blocker");
            var execution = MotionExecutionContracts.Read(game.World, machine); c.Equal(execution.Blocker, empty.Blocker, "forecast does not translate away real fuel reason");
            Fix position = machine.Get<PointPresence>().Position; ulong hash = game.Hash(); Read(game); c.Equal(hash, game.Hash(), "fuel-blocked read burns nothing");
            var twin = factory.Load(game.Save()); Tick(game); Tick(twin); c.Equal(position, machine.Get<PointPresence>().Position, "fuel-blocked estimate grants no distance");
            c.Equal(game.Hash(), twin.Hash(), "saved actual fuel/route ownership produces identical unknown estimates");
            c.Equal(Read(game).Blocker, Read(twin).Blocker, "current fuel blocker also reconstructs through actual loaded owners");
        }
        private static void Tile(Check c)
        {
            var factory = Colony.Factory(); var game = New(factory, "colony"); var truck = Find(game, "truck"); var mover = truck.Get<Mover>();
            mover.GoTo(new TileRect(14, 6, 1, 1)); ulong hash = game.Hash();
            c.Equal(MotionArrivalCodes.RoutePending, Read(game, "truck").Blocker, "native unplanned route stays unknown, read never runs path planning");
            c.Equal(-1, mover.PathIndex, "pending read leaves actual plan state unchanged"); c.Equal(hash, game.Hash(), "pending native forecast is pure");
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = truck.Id, To = new TileCoord(14, 6) }).Accepted, "normal engine tile command owns planning and actual movement");
            var estimate = Read(game, "truck"); c.That(estimate.ArrivalTick.HasValue && estimate.RemainingTicks > 0, "native planned remaining path yields an engine estimate");
            hash = game.Hash(); long moving = mover.TicksMoving; Read(game, "truck"); c.Equal(hash, game.Hash(), "current native route read preserves coordinates/age/path/hash");
            c.Equal(moving, mover.TicksMoving, "forecasts earn no telemetry exposure");
            var water = game.World.Services.Get<TileMap>().Layer<ByteLayer>("water"); TileCoord blocked = mover.Path[mover.PathIndex]; water.Set(blocked, 1);
            hash = game.Hash(); c.Equal(MotionArrivalCodes.RouteChanged, Read(game, "truck").Blocker, "current blocked native segment has typed unknown duration, not old distance");
            c.Equal(hash, game.Hash(), "changed-route diagnostic cannot replan or move"); water.Set(blocked, 0);
            var current = Read(game, "truck"); var twin = factory.Load(game.Save());
            c.Equal(current.RemainingTicks, Read(twin, "truck").RemainingTicks, "native estimate derives exactly from restored physical route");
            c.Equal(current.ArrivalTick, Read(twin, "truck").ArrivalTick, "restored native route reconstructs observation/arrival time without ETA state");
            Reach(game, "truck", twin);
            c.Equal(current.ArrivalTick.Value, game.Tick, "actual straight native tile arrival matches remaining weighted route budget"); c.Equal(game.Hash(), twin.Hash(), "saved native path and manual control continue identically");
            c.Equal(0L, Read(game, "truck").RemainingTicks.Value, "actual native arrival has zero remaining duration");
            var diagonal = New(factory, "colony"); var diagonalTruck = Find(diagonal, "truck");
            c.That(TestKit.Order(diagonal, 0, new MoveVehicleCommand { Vehicle = diagonalTruck.Id, To = new TileCoord(14, 12) }).Accepted,
                "normal native diagonal route has actual continuous segment geometry");
            var diagonalEstimate = Read(diagonal, "truck"); var diagonalTwin = factory.Load(diagonal.Save()); Reach(diagonal, "truck", diagonalTwin);
            c.That(System.Math.Abs(diagonal.Tick - diagonalEstimate.ArrivalTick.Value) <= 1,
                "native weighted continuous-route estimate follows physical diagonal timing within fixed-point rounding, not a guaranteed deadline");
            c.Equal(diagonal.Hash(), diagonalTwin.Hash(), "saved diagonal actual movement remains deterministic independent of estimates");
        }
    }
}
