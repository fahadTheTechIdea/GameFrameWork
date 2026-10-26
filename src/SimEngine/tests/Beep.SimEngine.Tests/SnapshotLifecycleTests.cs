using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class SnapshotLifecycleTests
    {
        internal const string Case = "Snapshot lifecycle: initialization guards, actual batches and private fault epochs";
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static ViewObservationVisibility Visibility(SimGame game) => (ViewObservationVisibility)game.World.Services.Get<IVisibility>();
        private static void Counts(Check c, SimGame game, int completed, long start, string label)
        {
            c.That(game.Time.StartTick == start && game.Time.Tick == start + completed && game.Tick == game.Time.Tick,
                label + " reports actual sole-clock coordinates");
            c.Equal(completed, game.Time.TicksRun, label + " event timing reports completed work");
            c.Equal(completed, game.Clock.TicksThisFrame, label + " clock reports this update, not the preceding paced frame");
        }
        private static SimGame BeforeInitialize()
        {
            // Engine-side construction instrumentation only: the real definition builds one world,
            // then its sole session is observed immediately before Initialize, never a duplicate loop.
            var definition = ViewObservationProof.Definition(); var data = ViewObservationProof.Content(definition);
            var selected = data.ResolveScenario("views", null); var scenario = selected.Get<ScenarioDef>("views");
            var settings = new WorldSettings { Scenario = scenario.Id, Seed = 289, Modules = selected.Modules.ToArray(),
                Seats = new[] { Controller.Human, Controller.Human }, Space = scenario.Space.Id,
                Time = scenario.Time.Id, GameMode = scenario.DeclaredGameMode, HorizonTicks = 0 };
            var world = definition.Build(data, settings, true, out var features);
            return new SimGame(world, definition.Name, features);
        }
        private static void InitializationGuard(Check c)
        {
            var game = BeforeInitialize(); bool exercised = false; int updated = 0;
            c.That(!game.Initialized && game.Tick == 0, "composed engine-side fixture has not run a lifecycle phase");
            game.Updated += _ => updated++;
            Visibility(game).OnEntity = (world, viewer, entity) =>
            {
                if (exercised) return; exercised = true;
                c.Throws<InvalidOperationException>(() => game.Update(), "initial snapshot callback cannot run nested Update");
                c.Throws<InvalidOperationException>(() => game.RunTicks(3), "initial snapshot callback cannot run nested batch");
                c.Throws<InvalidOperationException>(() => game.Initialize(), "initial snapshot callback cannot invoke idempotent Initialize in another phase");
            };
            game.Initialize(); c.That(exercised && game.Initialized && game.Fault == null, "caught nested-phase refusals do not corrupt initialization");
            Counts(c, game, 0, 0, "Initialize"); c.Equal(0, updated, "Initialize emits no Update event");
            c.That(game.Snapshot.Entities.Count == 2 && game.Seat(0).Snapshot.Entities.Count == 1 && game.Seat(1).Snapshot.Entities.Count == 1,
                "normal initialization publishes private snapshots only after all capture succeeds");
            long revision = game.Snapshot.Revision; ulong hash = game.Hash(); game.Initialize();
            c.That(game.Snapshot.Revision == revision && game.Hash() == hash, "outside-phase repeated Initialize is truly idempotent");
            game.Updated += _ =>
            {
                c.Throws<InvalidOperationException>(() => game.Initialize(), "Updated cannot reenter Initialize");
                c.Throws<InvalidOperationException>(() => game.Update(), "Updated cannot reenter Update");
                c.Throws<InvalidOperationException>(() => game.RunTicks(1), "Updated cannot reenter diagnostic batch");
            };
            c.Equal(3, game.RunTicks(3), "batch uses normal fixed loop after initialization guard releases"); Counts(c, game, 3, 0, "first batch");
        }
        private static void HealthyPacing(Check c)
        {
            var factory = ViewObservationProof.Factory(); var game = ViewObservationProof.New(factory);
            game.Clock.TicksPerSecond = 10; game.Clock.CatchUpCap = 1;
            using (var recorder = game.RecordReplay(Limits))
            {
                c.Equal(1, game.Update(350000), "paced update completes one capped step"); Counts(c, game, 1, 0, "paced update");
                long pending = game.Clock.PendingTicks; Fix alpha = game.Clock.Alpha;
                c.That(pending == 2 && alpha > Fix.Zero && game.Clock.IsRunningSlowly, "earned backlog and fractional time really exist");
                var retained = game.Seat(0).Snapshot;
                c.Equal(3, game.RunTicks(3), "healthy batch runs three actual steps"); Counts(c, game, 3, 1, "healthy batch");
                c.That(game.Clock.PendingTicks == pending && game.Clock.Alpha == alpha, "healthy batch does not destroy earned platform time");
                c.That(retained.Tick == 1 && game.Seat(0).Snapshot.Tick == 4 && retained.Entities[0].Version < game.Seat(0).Snapshot.Entities[0].Version,
                    "batch publishes settled entity facts without mutating retained projections");
                c.Equal(0, game.RunTicks(0), "zero batch does not update entities"); Counts(c, game, 0, 4, "zero batch");
                c.Equal(pending, game.Clock.PendingTicks, "zero batch preserves earned backlog");
                c.Equal(1, game.Update(0), "next paced update spends previously earned time"); Counts(c, game, 1, 4, "retained pacing");
                c.Equal(1L, game.Clock.PendingTicks, "only one actual earned step was spent");
                game.Clock.Pause(); c.Equal(2, game.RunTicks(2), "explicit tool batch bypasses pause through the same fixed loop");
                Counts(c, game, 2, 5, "paused explicit batch"); c.Equal(1L, game.Clock.PendingTicks, "paused tool batch preserves earned backlog");
                game.Clock.Resume();
                var twin = factory.Load(game.Save()); long loadedRevision = twin.Snapshot.Revision; twin.Initialize();
                c.That(twin.Snapshot.Revision == loadedRevision && twin.Time.TicksRun == 0, "restored Initialize is idempotent and earns no work");
                for (int i = 0; i < 6; i++)
                {
                    long start = game.Tick; c.Equal(2, game.RunTicks(2), "mixed-path source batch completes"); c.Equal(2, twin.RunTicks(2), "saved twin batch completes");
                    Counts(c, game, 2, start, "source saved batch"); Counts(c, twin, 2, start, "twin saved batch");
                    c.Equal(game.Hash(), twin.Hash(), "pacing diagnostics are not another saved time owner");
                }
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "mixed paced/batch/pause updates replay canonically: " + replay.Failure + "/" + replay.Detail);
            }
            Action<GameUpdate> observerFailure = _ => throw new InvalidOperationException("proof.updated.failure");
            game.Updated += observerFailure; long before = game.Tick;
            c.Throws<InvalidOperationException>(() => game.RunTicks(3), "host Updated exception is reported after settled facts");
            Counts(c, game, 3, before, "observer-failed batch");
            c.That(game.Fault == null && game.Snapshot.Tick == game.Tick, "host observer failure does not turn completed simulation into a provider fault");
            game.Updated -= observerFailure; c.Equal(1, game.RunTicks(1), "observer exception releases original lifecycle guard");
        }
        private static void EmptyFault(Check c, SimGame game, long actualTick, string label)
        {
            c.That(game.Initialized && game.Fault != null && game.State == GameState.Faulted, label + " returns an initialized terminal session");
            Counts(c, game, 0, actualTick, label);
            foreach (var snapshot in new[] { game.Snapshot, game.Seat(0).Snapshot, game.Seat(1).Snapshot })
                c.That(snapshot.State == GameState.Faulted && snapshot.Revision == 1 && snapshot.PreviousRevision == 0 && snapshot.Entities.Count == 0 &&
                    snapshot.Spawned.Count == 0 && snapshot.Changed.Count == 0 && snapshot.Removed.Count == 0 && snapshot.Space == null,
                    label + " discards every partially captured initial projection");
            c.That(game.Seat(0).Snapshot.Viewer == new PlayerId(0) && game.Seat(1).Snapshot.Viewer == new PlayerId(1) && game.Snapshot.Viewer == PlayerId.None,
                label + " never substitutes observer data for private seats");
            var fault = game.Fault; long revision = game.Snapshot.Revision; game.Initialize();
            c.That(ReferenceEquals(fault, game.Fault) && game.Snapshot.Revision == revision, label + " repeat Initialize neither retries nor replaces the cause");
            c.Equal(0, game.RunTicks(9), label + " fault cannot resume a batch"); Counts(c, game, 0, actualTick, label + " stopped batch");
            c.That(ReferenceEquals(fault, game.Fault) && game.Clock.PendingTicks == 0, label + " first cause and cleared pacing persist");
            c.Throws<InvalidOperationException>(() => game.Save(), label + " cannot checkpoint poisoned state");
            c.Throws<InvalidOperationException>(() => game.Hash(), label + " cannot claim a settled hash");
        }
        private static void InitialFaults(Check c)
        {
            for (int kind = 0; kind < 3; kind++)
            {
                int calls = 0;
                var module = new ViewObservationModule { ConfigureVisibility = visibility => visibility.OnEntity = (world, viewer, entity) =>
                {
                    if (viewer != new PlayerId(1)) return; calls++;
                    if (kind == 0) throw new InvalidOperationException("proof.initial.capture");
                    if (kind == 1) visibility.Value++;
                    else ((GameClock)world.Clock).Pause();
                } };
                var game = ViewObservationProof.New(ViewObservationProof.Factory(module));
                EmptyFault(c, game, 0, "new initial fault " + kind); c.Equal(1, calls, "initial fault is not retried after partial earlier viewers");
            }
            var mutableModule = new ViewObservationModule(); var factory = ViewObservationProof.Factory(mutableModule);
            var source = ViewObservationProof.New(factory); var other = ViewObservationProof.New(factory); byte[] save, artifact;
            using (var recorder = source.RecordReplay(Limits)) { source.RunTicks(5); save = source.Save(); artifact = recorder.Finish(); }
            ulong sourceHash = source.Hash(), otherHash = other.Hash(); int reads = 0;
            mutableModule.ConfigureVisibility = visibility => visibility.OnEntity = (world, viewer, entity) =>
            { if (viewer == new PlayerId(1)) { reads++; throw new InvalidOperationException("proof.loaded.capture"); } };
            var loaded = factory.Load(save); EmptyFault(c, loaded, 5, "restored initial fault");
            c.Equal(1, reads, "loaded initial failure is not retried");
            var refusal = new ReplayVerifier().Verify(factory, artifact, Limits);
            c.That(!refusal.Success && refusal.Game == null && refusal.Failure == new Code("replay.provider") && refusal.Detail.Contains("initial.hash"),
                "replay refuses an initially poisoned restored session without exposing a verified game");
            c.That(source.Hash() == sourceHash && other.Hash() == otherHash, "failed load/replay touches no source or interleaved world");
        }
        private static void LateFaults(Check c)
        {
            for (int mode = 0; mode < 3; mode++)
            {
                var game = ViewObservationProof.New(); game.Clock.TicksPerSecond = 10; game.Clock.CatchUpCap = 1;
                game.Update(350000); var before = new[] { game.Snapshot, game.Seat(0).Snapshot, game.Seat(1).Snapshot };
                int calls = 0, events = 0; var visibility = Visibility(game);
                visibility.OnEntity = (world, viewer, entity) =>
                { if (viewer == new PlayerId(1)) { calls++; throw new InvalidOperationException("proof.late.lifecycle"); } };
                game.Updated += _ => { events++; c.Equal(GameState.Faulted, game.Seat(0).Snapshot.State, "Updated sees atomic fault metadata"); };
                long start = game.Tick; int expected = mode == 0 ? 4 : mode == 1 ? 1 : 0;
                if (mode == 2) game.Clock.Pause();
                int actual = mode == 0 ? game.RunTicks(4) : mode == 1 ? game.Update(0) : game.RunTicks(0);
                c.Equal(expected, actual, "late fault reports the current batch/paced/zero result"); Counts(c, game, expected, start, "late mode " + mode);
                var after = new[] { game.Snapshot, game.Seat(0).Snapshot, game.Seat(1).Snapshot };
                for (int i = 0; i < after.Length; i++)
                    c.That(after[i].Tick == before[i].Tick && after[i].Viewer == before[i].Viewer && after[i].Revision == before[i].Revision + 1 &&
                        after[i].Entities.Select(e => e.Version).SequenceEqual(before[i].Entities.Select(e => e.Version)) &&
                        after[i].Changed.Count == 0 && after[i].Spawned.Count == 0 && after[i].Removed.Count == 0,
                        "late lifecycle fault preserves this viewer's retained facts epoch");
                c.That(game.Clock.PendingTicks == 0 && game.Clock.Alpha == Fix.Zero && !game.Clock.IsRunningSlowly && events == 1,
                    "late lifecycle fault clears pacing without misreporting completed steps");
                var fault = game.Fault; long stopped = game.Tick;
                c.Equal(0, game.Update(), "faulted later Update earns no time"); Counts(c, game, 0, stopped, "terminal next update");
                c.That(ReferenceEquals(fault, game.Fault) && calls == 1, "terminal update retries no failed snapshot callback");
            }
        }
        private static List<WorkJobView> Jobs(SimGame game)
        { var rows = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(rows); return rows; }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !done(); i++)
            {
                int amount = i % 3 + 1; game.RunTicks(amount); twin?.RunTicks(amount);
                if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                    throw new InvalidOperationException(stage + "/time=" + game.Tick + "/fault=" + game.Fault + "/twin=" + twin?.Fault);
            }
            if (!done()) throw new InvalidOperationException(stage + "/time=" + game.Tick + "/jobs=" + string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker)));
        }
        private static void Physical(Check c)
        {
            var factory = Colony.Factory(); var game = Colony.New(289);
            using (var recorder = game.RecordReplay(Limits))
            {
                var command = new ConstructCommand { Def = "tower", Priority = 50, Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)) };
                ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal engine preview owns real construction");
                c.Equal(hash, game.Hash(), "physical preview earns no time or resources");
                var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "player command posts original physical project");
                Counts(c, game, 1, 0, "player-command batch");
                Until(game, () => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero), "physical pickup through mixed batches");
                c.That(!game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "loaded material is not an instant structure");
                var twin = factory.Load(game.Save()); var retained = game.Seat(0).Snapshot;
                Until(game, () => game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "physical delivery and construction through saved batches", twin);
                c.That(retained.Tick < game.Tick && !retained.Entities.Any(e => e.Def == "tower"), "retained loaded-stage facts do not mutate into completed work");
                Fix steel = Fix.Zero;
                foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0)))
                {
                    var rows = new List<Facet>(); game.Seat(0).View.Inspect(entity.Id, rows);
                    steel += rows.Where(f => f.Key == Storage.FacetStock && f.Subject == "steel").Aggregate(Fix.Zero, (sum, row) => sum + row.Value);
                }
                c.Equal(Fix.FromInt(20), steel, "batch lifecycle preserves fifty-minus-thirty steel accounting");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "physical mixed-batch continuation replays: " + replay.Failure + "/" + replay.Detail);
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "snapshot lifecycle identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "one unchanged persistent game clock");
            InitializationGuard(c); HealthyPacing(c); InitialFaults(c); LateFaults(c); Physical(c);
        }
    }
}
