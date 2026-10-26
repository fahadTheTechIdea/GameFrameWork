using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine;
using Beep.SimEngine.App;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Tests
{
    // ---------------------------------------------------------------- the suites

    public static class FrameworkTests
    {
        public static void Register()
        {
            TestRunner.Add("Framework: math", Math);
            TestRunner.Add("Framework: movement vectors preserve exact represented distance budgets", MovementBudgets);
            TestRunner.Add("Framework: a scenario's horizon ends the game, and its frame size sets the pace", Horizon);
            TestRunner.Add("Framework: content is strict", ContentStrict);
            TestRunner.Add("Framework: a new game", NewGame);
            TestRunner.Add("Framework: orders are checked, applied and answered", Orders);
            TestRunner.Add("Framework: spread schedules and events", Scheduling);
            TestRunner.Add("Framework: determinism and pace independence", Determinism);
            TestRunner.Add("Framework: save, load, describe", SaveLoad);
            TestRunner.Add("Framework: views, snapshot and feeds", Views);
            TestRunner.Add("Framework: the app lifecycle", AppLifecycle);
        }

        private static void Math(Check c)
        {
            c.Equal(Fix.FromInt(3), Fix.FromInt(6) / Fix.FromInt(2), "division");
            c.Equal(Fix.Parse("2.5"), Fix.FromRatio(5, 2), "ratio equals parse");
            c.Equal(Fix.FromInt(3), Fix.Sqrt(Fix.FromInt(9)), "sqrt");
            Fix two = Fix.Sqrt(Fix.FromInt(2));
            c.That(two * two <= Fix.FromInt(2) && (two + Fix.FromRaw(1)) * (two + Fix.FromRaw(1)) >= Fix.FromInt(2), "sqrt is exact to the last fractional bit (" + two + ")");
            c.Equal(Fix.Zero, Fix.Sqrt(Fix.Zero), "sqrt of zero");
            c.That(!Fix.TryParse("12x", out _) && !Fix.TryParse("", out _) && !Fix.TryParse(".", out _), "malformed numbers are rejected");
            c.Equal(1000, Fix.Parse("1e3").ToIntRound(), "exponent form");
            var a = new DetRandom(5); var b = new DetRandom(5);
            bool same = true;
            for (int i = 0; i < 100; i++) same &= a.NextULong() == b.NextULong();
            c.That(same, "the RNG repeats from a seed");
            c.Equal(14, TileCoord.Octile10(new TileCoord(0, 0), new TileCoord(1, 1)), "diagonal step costs 14");
            c.Equal(24, TileCoord.Octile10(new TileCoord(0, 0), new TileCoord(2, 1)), "octile distance");
        }

        private static void MovementBudgets(Check c)
        {
            BigInteger Squared(FixVec2 value) => new BigInteger(value.X.Raw) * value.X.Raw + new BigInteger(value.Y.Raw) * value.Y.Raw;
            for (int i = 1; i <= 32; i++)
            {
                var from = new FixVec2(Fix.FromRatio(i % 5 - 2, 7), Fix.FromRatio(i % 7 - 3, 11));
                var target = from + new FixVec2(Fix.FromRatio(i % 11 - 5, 3), Fix.FromRatio(i % 13 - 6, 7));
                Fix step = Fix.FromRatio(i % 3 + 1, 6);
                BigInteger squared = Squared(target - from), ceiling = (target - from).LengthCeiling.Raw;
                c.That(ceiling * ceiling >= squared && (ceiling == 0 || (ceiling - 1) * (ceiling - 1) < squared),
                    "ceil distance is the least integer raw length enclosing the exact represented vector");
                c.Equal(from, FixVec2.MoveTowards(from, target, Fix.Zero), "zero budget never moves a represented coordinate");
                for (int n = 0; n < 8 && from != target; n++)
                {
                    FixVec2 next = FixVec2.MoveTowards(from, target, step);
                    BigInteger moved = Squared(next - from), limit = new BigInteger(step.Raw) * step.Raw;
                    c.That(moved <= limit, "exact raw distance never overspends/" + i + "/" + n + "/excess=" + (moved - limit));
                    c.That(Squared(target - next) < Squared(target - from), "positive movement really approaches its target/" + i + "/" + n);
                    from = next;
                }
            }
            c.Throws<ArgumentOutOfRangeException>(() => FixVec2.MoveTowards(FixVec2.Zero, new FixVec2(Fix.One, Fix.One), -Fix.One), "negative movement budget is invalid");
            var endpoint = new FixVec2(Fix.FromInt(3), Fix.FromInt(4));
            c.Equal(endpoint, FixVec2.MoveTowards(FixVec2.Zero, endpoint, Fix.FromInt(5)), "exact whole budget lands on target without an extra quantum");
        }
        private static void Horizon(Check c)
        {
            SimGame g = TestGame.Factory().NewGame(new NewGameRequest { Scenario = "flat", Seed = 3, Horizon = GameDuration.Days(2) });
            c.Equal(GameState.Running, g.State, "a game with a horizon runs");
            g.RunTicks(checked((int)TestGame.Time.Days(2)) - 1);
            c.Equal(GameState.Running, g.State, "until the last day is over");
            g.RunTicks(1);
            c.Equal(GameState.Ended, g.State, "then the engine ends it");
            c.Equal(Beep.SimEngine.Kernel.World.HorizonOutcome, g.Outcome, "with the horizon as its outcome");
            ulong ended = g.Hash();
            c.Equal(0, g.RunTicks(20), "a finished game accepts no more fixed updates");
            c.Equal(0, g.Update(), "a finished game's Update publishes facts without advancing time");
            c.Equal(0, g.Update(1000000), "a finished game's platform-timed Update advances no time");
            c.Equal(ended, g.Hash(), "drawing and batch calls cannot advance a finished game");
            c.That(!g.World.Step() && g.Hash() == ended, "the kernel also refuses a tick after an outcome");
            SimGame batched = TestGame.Factory().NewGame(new NewGameRequest { Scenario = "flat", Seed = 3, Horizon = GameDuration.Days(2) });
            batched.Clock.TicksPerFrame = checked((int)TestGame.Time.Days(1));
            batched.Clock.CatchUpCap = checked((int)TestGame.Time.Days(2));
            batched.Clock.Speed = 4;
            c.Equal(checked((int)TestGame.Time.Days(2)), batched.Update(), "a fast update stops at the horizon inside its tick batch");
            c.Equal(checked((int)TestGame.Time.Days(2)), batched.Tick, "the horizon is an exact simulation boundary");
            SimGame open = TestGame.Factory().NewGame(new NewGameRequest { Scenario = "flat", Seed = 3 });
            open.RunTicks(checked((int)TestGame.Time.Days(3)));
            c.Equal(GameState.Running, open.State, "no horizon: only the rules end it");
        }

        private static void ContentStrict(Check c)
        {
            ResolvedContent content = TestGame.Content().Resolve(new[] { "test" });
            c.Equal(2, content.ResourceCount, "resources load");
            c.That(content.ResourceId("rock").IsValid && content.Resource(content.ResourceId("water")).Phase == new Code("liquid"), "resources intern and keep their authored phases");
            EntityDef bin = content.Get<EntityDef>("bin");
            c.Equal(2, bin.Component<global::Beep.SimEngine.Maps.ITilePresenceDefinition>().Width, "entity footprint");
            c.Equal(Fix.Half, bin.Component<CounterDef>().PerTick, "component config is read once into the def");

            ResolvedContent extended = TestGame.Content().Resolve(new[] { "test", "extra" });
            c.Equal(Fix.FromInt(2), extended.Get<EntityDef>("extra.bin").Component<CounterDef>().PerTick, "a later module adds its own authored definition");
            c.Equal(Fix.Half, extended.Get<EntityDef>("bin").Component<CounterDef>().PerTick, "an extension does not rewrite the base definition");
            c.That(content.Fingerprint != extended.Fingerprint, "different module sets fingerprint differently");

            c.Throws<ContentException>(() => TestGame.Content(TestGame.Defs.Replace("\"width\": 2,", "\"widht\": 2,")).Resolve(new[] { "test" }), "a misspelt key fails the load");
            c.Throws<ContentException>(() => TestGame.Content(TestGame.Defs.Replace("\"counter\"", "\"countr\"")).Resolve(new[] { "test" }), "an unknown component fails the load");
            c.Throws<ContentException>(() => TestGame.Content(TestGame.Defs.Replace("\"per_tick\": 0.5", "\"per_tick\": \"fast\"")).Resolve(new[] { "test" }), "a wrongly typed value fails the load");
            c.Throws<ContentException>(() => TestGame.Content(TestGame.Defs.Replace("{ \"id\": \"water\", \"fluid\": true }", "{ \"id\": \"rock\" }")).Resolve(new[] { "test" }), "a duplicate id fails the load");
            c.Throws<ContentException>(() => TestGame.Content().Resolve(new[] { "extra" }), "a module without its requirement fails");
        }

        private static void NewGame(Check c)
        {
            SimGame g = TestGame.New();
            c.Equal(2, g.SeatCount, "the scenario's default seats");
            c.Equal(Controller.Human, g.Seat(0).Controller, "seat 0 is human by default");
            c.Equal(Controller.Autopilot, g.Seat(1).Controller, "other default seats are autopilot");
            c.Equal(32, g.Observer.Feature<global::Beep.SimEngine.Maps.TileMapView>().Width, "map size from the scenario");
            var ents = new List<EntityView>();
            g.Observer.CopyEntities(ents);
            c.Equal(2, ents.Count, "world generation placed a bin per seat");
            c.Equal(new TileCoord(11, 2), ((global::Beep.SimEngine.Maps.TileSpatialSnapshot)ents[1].Spatial).Tile, "world generation read the scenario's module settings");
            c.Equal(0, g.Tick, "a new game starts at tick 0");
            var seats3 = TestGame.Factory().NewGame(new NewGameRequest { Scenario = "flat", Seed = 1, Seats = { Controller.Human, Controller.Human, Controller.Autopilot } });
            c.Equal(3, seats3.SeatCount, "a request may choose the seats");
            c.Throws<ArgumentException>(() => TestGame.Factory().NewGame(new NewGameRequest { Scenario = "flat", Seats = { Controller.Human, Controller.Human, Controller.Human, Controller.Human } }), "more seats than the scenario allows is refused");
            c.Throws<ArgumentException>(() => TestGame.Factory().NewGame(new NewGameRequest { Scenario = "nope" }), "an unknown scenario is refused");
            c.Equal(1, TestGame.Factory().Scenarios().Count, "the factory lists scenarios");
        }

        private static void Orders(Check c)
        {
            SimGame g = TestGame.New();
            PlayerSeat me = g.Seat(0);
            var place = new PlaceBin { At = new TileCoord(20, 10) };
            c.That(me.Preview(place).IsNone, "preview accepts a free spot");
            c.Equal(PlaceBinHandler.Occupied, me.Preview(new PlaceBin { At = new TileCoord(3, 3) }), "preview refuses occupied ground with the handler's code");
            int seq = me.Submit(place);
            int seqBad = me.Submit(new PlaceBin { At = new TileCoord(31, 23) });
            var views = new List<EntityView>();
            g.Observer.CopyEntities(views);
            c.Equal(2, views.Count, "submitting changes nothing until a tick runs");
            g.RunTicks(1);
            long cursor = 0;
            var results = new List<CommandResult>();
            me.ReadResults(ref cursor, results);
            c.Equal(2, results.Count, "every order is answered");
            c.That(results[0].Sequence == seq && results[0].Accepted && results[0].Entity.IsValid && results[0].Command == "test.place-bin", "the accepted order names the entity it made");
            c.That(results[1].Sequence == seqBad && !results[1].Accepted && results[1].Refusal == Codes.OutOfBounds, "a refused order carries its code");
            var theirs = new List<CommandResult>();
            long theirCursor = 0;
            g.Seat(1).ReadResults(ref theirCursor, theirs);
            c.Equal(0, theirs.Count, "a seat reads only its own answers");
            me.Submit(new RemoveEntity { Target = results[0].Entity });
            g.Seat(1).Submit(new RemoveEntity { Target = views[0].Id });
            g.RunTicks(1);
            results.Clear();
            me.ReadResults(ref cursor, results);
            c.That(results.Count == 1 && results[0].Accepted, "removing my own entity is accepted");
            theirs.Clear();
            g.Seat(1).ReadResults(ref theirCursor, theirs);
            c.That(theirs.Count == 1 && theirs[0].Refusal == Codes.NotOwned, "another seat cannot remove my entity");
            c.That(!g.Observer.TryGetEntity(results[0].Entity, out _), "the removed entity is gone");
        }

        private static void Scheduling(Check c)
        {
            SimGame g = TestGame.New();
            g.RunTicks(100);
            var facets = new List<Facet>();
            var ents = new List<EntityView>();
            g.Seat(0).View.CopyEntities(new PlayerId(0), ents);
            g.Seat(0).View.Inspect(ents[0].Id, facets);
            c.Equal(Fix.FromInt(50), facets[0].Value, "a spread system covers every tick exactly (100 ticks × 0.5)");
            var theirs = new List<Facet>();
            g.Seat(1).View.Inspect(ents[0].Id, theirs);
            c.Equal(0, theirs.Count, "a rival's inspect panel hides private state");
            CounterSystem sys = g.World.System<CounterSystem>();
            c.Equal(20, sys.Visits, "each entity is visited once per period, not every tick");
            g.Seat(0).Submit(new PlaceBin { At = new TileCoord(20, 10) });
            g.Seat(0).Submit(new PlaceBin { At = new TileCoord(24, 10) });
            g.RunTicks(1);
            c.Equal(2, g.World.System<EventLog>().Seen.Count, "events reach subscribers in the same tick, in order");
        }

        private static ulong Script(SimGame g, int ticks)
        {
            for (int t = 0; t < ticks; t++)
            {
                if (t % 7 == 0) g.Seat(t % 2).Submit(new PlaceBin { At = new TileCoord(t % 29, 5 + t % 17) });
                g.RunTicks(1);
            }
            return g.Hash();
        }

        private static void Determinism(Check c)
        {
            c.Equal(Script(TestGame.New(3), 200), Script(TestGame.New(3), 200), "the same seed and orders give the same state");
            c.That(Script(TestGame.New(3), 200) != Script(TestGame.New(4), 200), "a different seed gives a different state");

            SimGame fast = TestGame.New(9), slow = TestGame.New(9);
            fast.Clock.TicksPerSecond = 1000;
            slow.Clock.TicksPerSecond = 7;
            int fastTicks = 0, slowTicks = 0;
            while (fastTicks < 300) fastTicks += fast.Update(16667);
            while (slowTicks < fastTicks) slowTicks += slow.Update(1000000);
            while (fastTicks < slowTicks) fastTicks += fast.Update(1000);
            c.Equal(fastTicks, slowTicks, "both paces reached the same tick");
            c.Equal(fast.Hash(), slow.Hash(), "pace changes how fast ticks come, never what they do");

            SimGame paused = TestGame.New(9);
            paused.Clock.Pause();
            c.Equal(0, paused.Update(1000000), "a paused clock runs no ticks");
            c.That(paused.State == GameState.Paused, "and says it is paused");
        }

        private static void SaveLoad(Check c)
        {
            SingleClockTests.Persistence(c);
            GameFactory f = TestGame.Factory();
            SimGame a = f.NewGame(new NewGameRequest { Scenario = "flat", Seed = 11 });
            Script(a, 90);
            a.Seat(1).Submit(new PlaceBin { At = new TileCoord(15, 15) });
            byte[] save = a.Save();
            SimGame b = f.Load(save);
            c.Equal(a.Hash(), b.Hash(), "a loaded game has the saved state");
            c.Equal(a.Tick, b.Tick, "and the saved tick");
            Script(a, 60);
            Script(b, 60);
            c.Equal(a.Hash(), b.Hash(), "and continues identically, including the order queued at save time");
            SaveInfo info = f.Describe(save);
            c.That(info.Compatible && info.Scenario == "flat" && info.Seed == 11 && info.Seats == 2 && info.Tick == 90, "a save describes itself without loading");
            byte[] damaged = (byte[])save.Clone();
            damaged[damaged.Length / 2] ^= 0x5A;
            c.Throws<CorruptStateException>(() => f.Load(damaged), "a damaged save is refused");
            c.Throws<CorruptStateException>(() => f.Load(new byte[] { 1, 2, 3 }), "garbage is refused");
            IReadOnlyList<KeyValuePair<string, ulong>> parts = a.HashBreakdown();
            c.That(parts.Count >= 4, "the hash breaks down by part for desync reports");
        }

        private static void Views(Check c)
        {
            SimGame g = TestGame.New();
            GameSnapshot first = g.Snapshot;
            c.Equal(2, first.Entities.Count, "the snapshot lists what to draw");
            c.Equal(2, first.Spawned.Count, "the first snapshot reports every entity as spawned");
            g.Seat(0).Submit(new PlaceBin { At = new TileCoord(20, 10) });
            g.RunTicks(1);
            c.That(g.Snapshot.Spawned.Count == 1 && g.Snapshot.Removed.Count == 0, "the next snapshot reports only what appeared");
            EntityId made = g.Snapshot.Spawned[0];
            g.Seat(0).Submit(new RemoveEntity { Target = made });
            g.RunTicks(1);
            c.That(g.Snapshot.Removed.Count == 1 && g.Snapshot.Removed[0] == made, "and what disappeared");

            long c1 = 0, c2 = 0;
            var n1 = new List<Notification>();
            var n2 = new List<Notification>();
            g.Seat(0).ReadNotifications(ref c1, n1);
            g.Seat(0).ReadNotifications(ref c2, n2);
            c.That(n1.Count == 1 && n2.Count == 1, "two readers each get the news (nobody consumes another's feed)");
            var other = new List<Notification>();
            long c3 = 0;
            g.Seat(1).ReadNotifications(ref c3, other);
            c.Equal(0, other.Count, "news about one company is not told to another");

            var layer = new List<int>();
            c.Equal(SpatialLayerReadStatus.Success, g.Observer.Feature<global::Beep.SimEngine.Maps.TileMapView>().ReadLayer("ground", new TileRect(0, 0, 4, 4), layer), "authorized regional query succeeds");
            c.Equal(16, layer.Count, "a map layer reads row by row");
            var mapView = g.Observer.Feature<global::Beep.SimEngine.Maps.TileMapView>();
            var delta = mapView.ReadChanges();
            c.That(delta.Cursor.Version == mapView.Version, "changed chunks return the version to resume from");
            var catalogue = new List<CatalogueEntry>();
            g.Observer.CopyCatalogue(catalogue);
            c.Equal(2, catalogue.Count, "the catalogue lists every def");
            c.That(System.Linq.Enumerable.Contains(g.Observer.CommandIds, "test.place-bin"), "the view lists the commands");
        }

        private sealed class BufferPresenter : IPresenter
        {
            public readonly List<string> Frames = new List<string>();
            public void Present(SimApp app, AppFrame frame)
                => Frames.Add(frame.Number + ":" + (app.Game?.Tick ?? -1) + ":" + (app.Game?.Clock.Paused ?? false) + ":" + (app.Game?.Snapshot.Entities.Count ?? 0));
        }

        private static void AppLifecycle(Check c)
        {
            var presenter = new BufferPresenter();
            var input = new ScriptedInput().PressOnFrame(5, "space").PressOnFrame(8, "space").PressOnFrame(12, "plus");
            var app = new SimApp(TestGame.Factory(), input, presenter);
            app.Add(new ClockControls());
            var scene = new PlayScene(new NewGameRequest { Scenario = "flat", Seed = 2, Horizon = GameDuration.Days(1) }, 0, 1);
            int answered = 0;
            scene.OnStart(game => game.Seat(0).Attach(new ScriptedController(() => Build(() => answered++))));
            app.Start(scene);
            app.RunFrame();
            c.That(app.Game != null && app.LocalSeat != null && app.LocalSeat.Id.Value == 0, "the play scene starts a game and binds the local seat");
            app.Run(6);
            c.That(app.Game.Clock.Paused, "a mapped key paused the clock through the input map");
            app.Run(3);
            c.That(!app.Game.Clock.Paused, "and resumed it");
            app.Run(5);
            c.Equal(2, app.Game.Clock.Speed, "plus doubles the speed");
            long frames = app.Run(5000);
            c.That(app.ExitRequested, "the play scene exits at its day limit");
            c.Equal(1, answered, "a scripted controller submitted, waited and got its answer without blocking a frame");
            c.That(presenter.Frames.Count == app.FrameNumber, "the presenter is called once per frame");

            var p1 = new BufferPresenter(); var p2 = new BufferPresenter();
            RunApp(p1); RunApp(p2);
            c.That(string.Join("|", p1.Frames) == string.Join("|", p2.Frames), "the same input gives the same frames");
        }

        private static IEnumerable<Wait> Build(Action answered)
        {
            yield return Wait.Hours(1);
            int seq = 0;
            yield return Wait.Do(seat => seq = seat.Submit(new PlaceBin { At = new TileCoord(20, 12) }));
            yield return Wait.ForAnswer(seq, r => { if (r.Accepted) answered(); });
            yield return Wait.Until(s => s.Date["hour"].Value >= 3);
        }

        private static void RunApp(BufferPresenter p)
        {
            var app = new SimApp(TestGame.Factory(), new ScriptedInput().PressOnFrame(3, "space").PressOnFrame(6, "space"), p);
            app.Add(new ClockControls());
            app.Start(new PlayScene(new NewGameRequest { Scenario = "flat", Seed = 5, Horizon = GameDuration.Days(1) }, 0, 2));
            app.Run(200);
        }
    }
}
