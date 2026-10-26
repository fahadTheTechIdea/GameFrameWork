using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Beep.SimEngine.App;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class SessionTimeTests
    {
        public static void Register()
        {
            TestRunner.Add("Session time: authored quantum, exact schedules and optional calendars", Definitions);
            TestRunner.Add("Session time: physical consumers, partial updates and saved twins", Physical);
        }

        // These helpers author input JSON only. All conversion, scheduling and execution is runtime-owned.
        private static JsonValue Time(string id, long numerator, long denominator, int frame = 1, int rate = 2)
            => Json.Parse("{\"id\":\"" + id + "\",\"quantum\":{\"numerator\":" + numerator.ToString(CultureInfo.InvariantCulture)
                + ",\"denominator\":" + denominator.ToString(CultureInfo.InvariantCulture)
                + ",\"unit\":\"second\"},\"frame_ticks\":" + frame + ",\"ticks_per_second\":" + rate + ",\"calendar\":null,\"horizon\":null}");

        private static JsonValue AddTimes(string original)
        {
            JsonValue root = Json.Parse(original), times = root.Get("time_definitions"), scenarios = root.Get("scenarios");
            times.Add(Time("test.ten-seconds", 10, 1, 3));
            times.Add(Time("test.tenth", 1, 10));
            times.Add(Time("test.third", 1, 3));
            string first = scenarios[0].Get("id").Text;
            foreach (string suffix in new[] { "ten-seconds", "tenth", "third" })
            {
                JsonValue scenario = scenarios[0].Clone();
                scenario.Set("id", JsonValue.Of(first + "." + suffix)); scenario.Set("time", JsonValue.Of("test." + suffix));
                scenarios.Add(scenario);
            }
            return root;
        }

        private static GameFactory Basic(JsonValue root) => new GameFactory(TestGame.Definition(), TestGame.Content(root.ToString()));
        private static SimGame Start(GameFactory factory, string scenario, GameDuration? horizon = null)
            => factory.NewGame(new NewGameRequest { Scenario = scenario, Seed = 217, Horizon = horizon });

        private static void Definitions(Check c)
        {
            JsonValue root = AddTimes(TestGame.Defs); GameFactory factory = Basic(root);
            ISimulationTime minute = factory.ScenarioTime("flat"), tenth = factory.ScenarioTime("flat.tenth"), third = factory.ScenarioTime("flat.third");
            c.Equal(60L, minute.QuantumNumerator, "minute quantum is authored, not a static engine constant");
            c.Equal(10L, tenth.QuantumDenominator, "subsecond rational quantum is retained exactly");
            c.Equal(30L, tenth.ExactTicks(GameDuration.Seconds(3)), "subsecond exact duration conversion");
            c.Equal(3L, third.ExactTicks(GameDuration.Seconds(1)), "nondecimal rational quantum is exact");
            c.Equal(1L, third.ExactTicks(new GameDuration(1, 3, DurationUnit.Second)), "rational work need not approximate its quantum");
            c.Equal(1L, tenth.Ticks(GameDuration.Decimal("0.09999999999999999999", DurationUnit.Second)), "ceil once below a quantum");
            c.Equal(2L, tenth.Ticks(GameDuration.Decimal("0.10000000000000000001", DurationUnit.Second)), "ceil once above a quantum");
            c.Throws<ArgumentException>(() => third.ExactTicks(GameDuration.Decimal("0.33333333333333333333", DurationUnit.Second)), "approximate thirds cannot silently change periodic schedules");
            c.Equal(1L, tenth.Ticks(GameDuration.Decimal("1e-999999999999999999999", DurationUnit.Hour)), "positive infinitesimal work does not disappear");
            c.Equal(0L, tenth.Ticks(GameDuration.Decimal("-0e999999999999999999999", DurationUnit.Hour)), "signed zero remains zero");
            c.Equal(long.MaxValue, tenth.Ticks(GameDuration.Decimal("922337203685477580.7", DurationUnit.Second)), "exact final tick representation boundary");
            c.Throws<OverflowException>(() => tenth.Ticks(GameDuration.Decimal("922337203685477580.71", DurationUnit.Second)), "first unrepresentable tick refuses");
            c.Throws<OverflowException>(() => third.Ticks(GameDuration.Decimal("1e999999999999999999999", DurationUnit.Second)), "oversized exponent refuses without giant allocation");
            c.Throws<ArgumentOutOfRangeException>(() => GameDuration.Decimal("-1e-999999", DurationUnit.Second), "negative work cannot round into zero");
            c.Throws<ArgumentOutOfRangeException>(() => new GameDuration(1, 0, DurationUnit.Second), "zero duration denominator refuses");
            c.Throws<FormatException>(() => GameDuration.Decimal("+1", DurationUnit.Second), "authored decimals share JSON grammar");
            var random = new Random(217);
            for (int i = 0; i < 12; i++)
            {
                long n = random.NextInt64(1, 1L << 48); int exponent = random.Next(1, 10);
                BigInteger divisor = BigInteger.Pow(10, exponent);
                c.Equal((long)(((BigInteger)n * 10 + divisor - 1) / divisor),
                    tenth.Ticks(GameDuration.Decimal(n.ToString(CultureInfo.InvariantCulture) + "e-" + exponent, DurationUnit.Second)), "session duration matches independent rational oracle " + i);
            }
            Schedule schedule = Schedule.Every(tenth, GameDuration.Seconds(3), 31);
            c.Equal(30L, schedule.Period, "period resolves against session quantum");
            c.Equal(1L, schedule.Phase, "stable phase normalizes after conversion");
            c.That(schedule.IsDue(31) && !schedule.IsDue(30), "schedule remains engine-owned");
            c.Throws<ArgumentException>(() => Schedule.Every(minute, GameDuration.Seconds(10)), "unaligned schedule refuses instead of rounding");
            Schedule wide = Schedule.Every(tenth, GameDuration.Days(4000), 1L << 33);
            c.That(wide.Period > int.MaxValue && wide.IsDue(wide.Phase + wide.Period), "schedule period and phase are wide");
            var tick = new TickInfo(1L << 40, 1L << 33, 0, tenth);
            c.Equal(Fix.FromInt(2), tick.Over(Fix.FromRaw(1)), "wide span does not narrow through Fix.FromInt");
            c.That(tick.Date == null && tenth.Calendar(0) == null, "calendar absence has no fabricated oil date");
            c.Equal(1L, minute.Calendar(0)["day"].Value, "explicit minute calendar starts at authored origin");
            c.Equal(1L, minute.Calendar(minute.Hours(1))["hour"].Value, "explicit calendar is a pure projection");
            c.Throws<ArgumentOutOfRangeException>(() => tenth.Calendar(-1), "even absent calendar refuses negative time");

            JsonValue alien = root.Clone();
            alien.Get("calendars").Add(Json.Parse(@"{ ""id"":""alien"", ""fields"":[
                {""id"":""shift"",""duration"":{""numerator"":7,""denominator"":10,""unit"":""second""},""radix"":5,""origin"":2},
                {""id"":""cycle"",""duration"":{""numerator"":7,""denominator"":2,""unit"":""second""},""radix"":0,""origin"":0}]}"));
            alien.Get("time_definitions")[2].Set("calendar", JsonValue.Of("alien"));
            ISimulationTime alienTime = Basic(alien).ScenarioTime("flat.tenth"); CalendarReading retained = alienTime.Calendar(35);
            c.Equal(2L, retained["shift"].Value, "calendar cycles need no earth year/day/hour fields");
            c.Equal(1L, retained["cycle"].Value, "independent authored calendar units project exact ticks");
            c.Throws<ArgumentException>(() => { var absent = retained["day"]; }, "missing calendar field is explicit");
            c.Throws<NotSupportedException>(() => ((IList<CalendarField>)retained.Fields)[0] = default, "retained calendar facts are immutable");
            alienTime.Calendar(70); c.Equal(1L, retained["cycle"].Value, "later projection cannot mutate retained date");
            JsonValue misaligned = alien.Clone(); misaligned.Get("scenarios")[0].Set("time", JsonValue.Of("test.tenth"));
            misaligned.Get("time_definitions")[2].Get("quantum").Set("numerator", JsonValue.Number("2"));
            c.Throws<ContentException>(() => Start(Basic(misaligned), "flat"), "misaligned calendar refuses before initialization");

            foreach (string field in new[] { "time", "frame_ticks", "ticks_per_second" })
            {
                JsonValue missing = root.Clone();
                (field == "time" ? missing.Get("scenarios")[0] : missing.Get("time_definitions")[0]).Remove(field);
                c.Throws<ContentException>(() => Start(Basic(missing), "flat"), "required authored " + field + " has no implicit fallback");
            }
            foreach (string reference in new[] { "", "unknown" })
            {
                JsonValue invalid = root.Clone(); invalid.Get("time_definitions")[0].Set("calendar", JsonValue.Of(reference));
                c.Throws<ContentException>(() => Start(Basic(invalid), "flat"), "invalid calendar reference refuses: " + reference);
            }
            foreach (string field in new[] { "numerator", "denominator" })
            {
                JsonValue invalid = root.Clone(); invalid.Get("time_definitions")[0].Get("quantum").Set(field, JsonValue.Number("0"));
                c.Throws<ContentException>(() => Start(Basic(invalid), "flat"), "nonpositive quantum " + field + " refuses");
            }
            JsonValue legacy = root.Clone(); legacy.Get("scenarios")[0].Set("frame_minutes", JsonValue.Number("1"));
            c.Throws<ContentException>(() => Start(Basic(legacy), "flat"), "old minute field is rejected, not accepted by a compatibility path");
        }

        private static void Reach(SimGame game, Func<bool> done, int budget, string stage)
        {
            TestKit.RunUntil(game, done, budget, 1);
            if (!done()) throw new InvalidOperationException(stage + " stalled at " + game.Tick + "; state=" + game.State + "; fault=" + game.Fault);
        }

        private static void Physical(Check c)
        {
            JsonValue root = AddTimes(Colony.Defs); GameFactory factory = Colony.Factory(root.ToString());
            SimGame minute = Start(factory, "colony"), seconds = Start(factory, "colony.ten-seconds");
            c.Equal(1, minute.Clock.TicksPerFrame, "minute session uses authored frame count");
            c.Equal(3, seconds.Clock.TicksPerFrame, "other session has its own frame count");
            c.Equal(2, seconds.Clock.TicksPerSecond, "host elapsed rate is authored separately from game-time quantum");
            c.Equal(0, seconds.Update(250000), "partial elapsed frame earns no premature tick");
            c.Equal(Fix.Half, seconds.Time.Alpha, "partial update publishes bounded interpolation");
            c.Equal(1, seconds.Update(250000), "remaining elapsed frame completes one tick");
            c.Equal(0, seconds.Update(0), "empty elapsed update invents no work");
            c.Equal(0L, minute.Tick, "interleaved session clock is isolated");
            c.That(seconds.Date == null && seconds.Seat(0).View.Date == null, "optional calendar remains absent through player views");

            TransformationDef shared = minute.World.Content.Get<TransformationDef>("build_truck");
            c.That(ReferenceEquals(shared, seconds.World.Content.Get<TransformationDef>("build_truck")), "different clocks share one immutable content definition");
            c.Equal(180L, minute.SimulationTime.Ticks(shared.Duration), "same authored transformation takes 180 minute ticks");
            c.Equal(1080L, seconds.SimulationTime.Ticks(shared.Duration), "same transformation takes 1080 ten-second ticks without mutation");
            c.Equal(10L, minute.World.Services.Get<IWorkerDispatchSource>().GetDispatchSchedule(minute.SimulationTime).Period, "logistics wakes shared pass on minute clock");
            c.Equal(60L, seconds.World.Services.Get<IWorkerDispatchSource>().GetDispatchSchedule(seconds.SimulationTime).Period, "logistics wakes shared pass on ten-second clock");
            c.Equal(Fix.FromRatio(1, 12), seconds.SimulationTime.RateFor(Fix.FromInt(30), DurationUnit.Hour), "movement rates use selected quantum");
            c.Equal(Fix.FromInt(5), seconds.SimulationTime.RateFor(Fix.FromInt(30), DurationUnit.Hour, 60), "network interval scales rate over elapsed game duration");

            Entity crane = seconds.World.Entities.Get(Colony.Mine(seconds, 0, "crane")[0].Id); FixVec2 start = crane.Get<global::Beep.SimEngine.Maps.ITilePresence>().Position;
            CommandResult build = TestKit.Order(seconds, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(seconds.World.Space.Id, new TileCoord(10, 16)), Priority = 50 });
            c.That(build.Accepted, "public construction command accepts alternate time resolution");
            WorkBoard board = seconds.World.Services.Get<WorkBoard>();
            Order order = board.Order(seconds.World.Entities.Get(build.Entity).Get<Site>().Order);
            c.Equal(720L, seconds.SimulationTime.Ticks(order.Plan.Graph.Nodes[0].Duration), "construction goal uses the same selected clock");
            Reach(seconds, () => order.Nodes[0].Progress.WholeTicks >= 3, 400, "physical construction dispatch/arrival");
            c.That(crane.Get<global::Beep.SimEngine.Maps.ITilePresence>().Position != start && order.Nodes[0].State != NodeState.Done, "machine traveled physically and work is not instant");
            SimGame twin = factory.Load(seconds.Save());
            c.Equal(seconds.Hash(), twin.Hash(), "session quantum and partial physical work restore identically");
            c.Equal("test.ten-seconds", twin.SimulationTime.Id, "load restores the selected time identity");
            c.Equal(3, twin.Clock.TicksPerFrame, "load uses selected authored frame count");
            c.That(factory.Describe(seconds.Save()).Date == null, "save menu does not fabricate a calendar");
            int elapsed = TestKit.RunUntil(seconds, () => Colony.Mine(seconds, 0, "tower").Count == 1, 2500, 1); twin.RunTicks(elapsed);
            c.Equal(1, Colony.Mine(seconds, 0, "tower").Count, "hauling and both construction stages complete physically");
            c.That(elapsed >= 1700, "five authored work hours cannot complete instantly on ten-second clock");
            c.Equal(seconds.Hash(), twin.Hash(), "saved continuation retains timing, reservations, movement and material consumption");

            Entity producerEntity = seconds.World.Entities.Get(Colony.Mine(seconds, 0, "factory")[0].Id); Transformer transformer = producerEntity.Get<Transformer>();
            c.That(TestKit.Order(seconds, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = producerEntity.Id, Transformation = "build_truck" }).Accepted, "transformation uses ordinary player queue");
            Reach(seconds, () => transformer.Phase == TransformationPhase.Working && transformer.Progress.WholeTicks >= 2, 600, "transformation physical supply");
            seconds.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(producerEntity.Id, new Code("transformation"), out TransformationSnapshot facts);
            c.Equal(1080L, facts.Required, "management facts resolve the same transformation duration");
            c.Equal(transformer.Progress, facts.Progress, "management facts expose actual elapsed progress");
            twin = factory.Load(seconds.Save());
            elapsed = TestKit.RunUntil(seconds, () => transformer.Queue.Count == 0, 1200, 1); twin.RunTicks(elapsed);
            c.Equal(3, Colony.Mine(seconds, 0, "truck").Count, "supplied timed transformation rolls out one new vehicle");
            c.Equal(seconds.Hash(), twin.Hash(), "transformation save/load follows identical clock and stock decisions");
            c.Equal(180L, minute.SimulationTime.Ticks(shared.Duration), "running another session never mutates shared duration content");
            c.Equal(0L, minute.Tick, "thousands of physical ticks do not advance an idle second session");

            JsonValue bounded = AddTimes(TestGame.Defs);
            bounded.Get("time_definitions")[1].Set("horizon", Json.Parse("{\"numerator\":30,\"denominator\":1,\"unit\":\"second\"}"));
            GameFactory boundedFactory = Basic(bounded); SimGame ending = Start(boundedFactory, "flat.ten-seconds");
            c.Equal(3, ending.Update(10000000), "authored horizon cuts off an earned host backlog at exact game ticks");
            c.Equal(GameState.Ended, ending.State, "horizon is an explicit engine rule");
            c.Equal(0L, ending.Clock.PendingTicks, "terminal cutoff cancels remaining host time");
            SaveHeader header = SaveGame.Describe(ending.Save());
            c.Equal(10, header.Format, "new explicit session time layout replaces old save schema");
            c.Equal(3L, header.Settings.HorizonTicks, "save records the converted optional horizon");
            c.Equal(ending.Hash(), boundedFactory.Load(ending.Save()).Hash(), "ended session time restores identically");
            SimGame unbounded = Start(boundedFactory, "flat.ten-seconds", GameDuration.Zero); unbounded.RunTicks(4);
            c.Equal(GameState.Running, unbounded.State, "explicit zero disables authored horizon without a fallback");
            SimGame shorter = Start(boundedFactory, "flat.ten-seconds", GameDuration.Seconds(10));
            c.Equal(1L, shorter.World.Settings.HorizonTicks, "player-selected duration is converted in engine");
            c.That(shorter.Hash() != Start(boundedFactory, "flat.ten-seconds").Hash(), "horizon decision participates in authoritative hash");
            var decision = new PlayerDecisionContext(unbounded.World, new PlayerId(0), unbounded.Seat(0).View,
                unbounded.Seat(0).Submit, () => Array.Empty<CommandResult>(), null);
            Wait wait = Wait.Duration(GameDuration.Seconds(20)); c.That(!wait.Done(decision), "engine script wait anchors to current session time");
            unbounded.RunTicks(1); c.That(!wait.Done(decision), "duration wait cannot finish a tick early");
            unbounded.RunTicks(1); c.That(wait.Done(decision), "duration wait expires against selected quantum");
        }
    }
}
