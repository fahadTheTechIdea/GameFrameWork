using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string WeatherCase = "Networked weather incidents: reordered peers retain exposures, supplied repairs and isolated saved clocks";
        private static readonly Code WeatherLeak = new Code("leak");
        private static WeatherSnapshot WeatherFacts(SimGame game)
        {
            Code refusal = game.Seat(0).View.Feature<WeatherView>().Read("weather.local", out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("networked weather facts/" + refusal);
            return value;
        }
        private static IncidentLifecycleView WeatherIncident(SimGame game, string name)
            => NetworkIncidentPropagationTests.Incident(game, name, WeatherLeak);
        private static IncidentPropagationSnapshot WeatherExposure(SimGame game)
            => NetworkIncidentPropagationTests.Exposure(game);
        private static ResourceId WeatherWater(SimGame game)
        {
            if (!game.Seat(0).View.TryFindResource("water", out var water)) throw new InvalidOperationException("authored response water missing");
            return water;
        }
        private static Fix WeatherStock(SimGame game, string name)
        {
            Code refusal = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(Entity(game, name), WeatherWater(game), out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("networked water stock/" + name + "/" + refusal);
            return value.Stock;
        }
        private static BigInteger WeatherConsumed(SimGame game)
            => game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates
                .Where(a => a.Resource == WeatherWater(game) && a.Kind == ResourceFlowKind.Consumed)
                .Aggregate(BigInteger.Zero, (sum, fact) => sum + fact.AmountRaw);
        private static void WeatherObserve(SimGame game)
        {
            if (WeatherFacts(game).ObservedUpdate != game.Clock.StepIndex) throw new InvalidOperationException("weather used another session clock");
            BigInteger stock = BigInteger.Zero;
            foreach (string name in new[] { "depot", "truck", "binding.source", "binding.watcher" }) stock += WeatherStock(game, name).Raw;
            if (stock + WeatherConsumed(game) != new BigInteger(Fix.FromInt(10).Raw))
                throw new InvalidOperationException("networked physical response water lost or duplicated/" + WeatherDiagnostics(game));
        }
        private static string WeatherDiagnostics(SimGame game)
            => "at=" + game.Clock.StepIndex + "/weather=" + WeatherFacts(game).Observation.Condition +
                "/pending=" + (WeatherIncident(game, "binding.source").Snapshot.Active ? WeatherExposure(game).Pending.Count.ToString() : "inactive") +
                "/source=" + WeatherIncident(game, "binding.source").Blocker +
                "/target=" + WeatherIncident(game, "binding.watcher").Snapshot.Active;
        private static Pair WeatherPair(SimGame a, SimGame b) => new Pair(a, b, WeatherObserve, WeatherDiagnostics, 2);
        private static HaulResourcesCommand WeatherSupply(SimGame game, string target)
            => new HaulResourcesCommand { Source = Entity(game, "depot"), Destination = Entity(game, target),
                Worker = Entity(game, "truck"), Resource = WeatherWater(game), Ceiling = Fix.One, Priority = 100 };
        private static bool WeatherNoHaul(SimGame game)
        {
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
            return !jobs.Any(j => j.IsHaul && (j.State == JobState.Open || j.State == JobState.Assigned || j.State == JobState.Paused));
        }
        private static bool WeatherLoaded(SimGame game)
        {
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
            return jobs.Any(j => j.IsHaul && j.Loaded.IsPositive);
        }
        private static void WeatherRun(Check c)
        {
            var factory = WeatherIncidentPropagationTests.Factory(new InitializeProbe(), out _, direct: false, tile: false);
            var pair = WeatherPair(NetworkIncidentPropagationTests.New(factory), NetworkIncidentPropagationTests.New(factory));
            Console.WriteLine("MODULES weather=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(new[] { WeatherModule.ModuleId, IncidentPropagationModule.ModuleId, InfrastructureModule.ModuleId, ResourceFlowsModule.ModuleId }
                .All(id => pair.A.Modules.Contains(id)), "all four added native module boundaries are actually selected");
            c.That(!pair.A.Modules.Contains(TycoonModule.ModuleId), "physical weather response needs no business module");
            var limits = new ReplayLimits(8 * 1024 * 1024, 2000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            int owner = Submit(c, pair, new RaiseIncidentCommand { Target = Entity(pair.A, "binding.source"), Source = WeatherLeak, Kind = new Code("network.fire") });
            int foreign = Submit(c, pair, new RaiseIncidentCommand { Target = Entity(pair.B, "binding.source"), Source = WeatherLeak, Kind = new Code("network.fire") }, Codes.NotOwned, seat: 1);
            Reach(pair, g => WeatherIncident(g, "binding.source").Snapshot.Active && WeatherExposure(g).Pending.Count == 1, "first genuine dry-weather exposure");
            c.That(Result(pair.A, 0, owner)?.Accepted == true && Result(pair.B, 0, owner)?.Accepted == true, "both peers admit owner incident once");
            c.That(Result(pair.A, 1, foreign)?.Refusal == Codes.NotOwned && Result(pair.B, 1, foreign)?.Refusal == Codes.NotOwned,
                "same-frame foreign incident receives the same actual refusal on both peers");
            var original = WeatherExposure(pair.A).Pending.Single(); var dry = WeatherFacts(pair.A);
            c.Equal(8L, original.DueTick - original.ScheduledTick, "original engine receipt owns authored eight-minute exposure delay");
            var restored = WeatherPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var middleLeft = restored.A.RecordReplay(limits); var middleRight = restored.B.RecordReplay(limits);
            c.Equal(pair.A.Hash(), restored.A.Hash(), "pending propagation and original topology reattach exactly");
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "network pause of original pending exposure");
            long paused = pair.A.Clock.StepIndex, otherBefore = restored.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            long rainEpoch = original.DueTick + 1; var isolated = Stopwatch.StartNew();
            for (int i = 0; i < 1000 && isolated.Elapsed.TotalSeconds < 25 &&
                (!restored.SameEpoch || restored.A.Clock.StepIndex != rainEpoch); i++) {
                pair.Round(); restored.Round(rainEpoch);
            }
            if (!restored.SameEpoch || restored.A.Clock.StepIndex != rainEpoch)
                throw new InvalidOperationException("saved rain epoch not reached/" + WeatherDiagnostics(restored.A));
            Console.WriteLine("WEATHER isolated original=" + WeatherDiagnostics(pair.A) + "/saved=" + WeatherDiagnostics(restored.A));
            c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen, "original networked weather and pending exposure freeze with sole clock");
            c.That(restored.A.Clock.StepIndex > otherBefore && restored.A.Hash() != frozen, "separate restored session really advances while original remains paused");
            Reach(restored, g => g.Clock.StepIndex > original.DueTick, "saved due exposure reaches actual rain");
            c.Equal(new Code("weather.wet"), WeatherFacts(restored.A).Observation.Condition, "due exposure is observed in actual authored rain phase");
            c.That(!WeatherIncident(restored.A, "binding.watcher").Snapshot.Active && WeatherExposure(restored.A).Pending.Count == 0,
                "actual rain cancels due propagation rather than delivering stale damage");
            c.Equal(new Code("weather.dry"), dry.Observation.Condition, "retained dry publication remains detached");
            c.Equal(1, WeatherExposure(pair.A).Pending.Count, "paused independent source still retains its own pending receipt");
            long target = restored.A.Clock.StepIndex; pair.A.Net.RequestResume(); var timer = Stopwatch.StartNew();
            for (int i = 0; i < 1000 && timer.Elapsed.TotalSeconds < 25 && (!pair.SameEpoch || pair.A.Clock.StepIndex != target); i++) pair.Round(target);
            Console.WriteLine("WEATHER aligned original=" + WeatherDiagnostics(pair.A) + "/saved=" + WeatherDiagnostics(restored.A) +
                "/hash=" + pair.A.Hash().ToString("X16") + "/" + restored.A.Hash().ToString("X16"));
            c.That(pair.SameEpoch && pair.A.Clock.StepIndex == target && pair.A.Hash() == restored.A.Hash(), "resumed original catches up to advanced saved session at actual committed epoch");
            Both(pair, restored, g => WeatherExposure(g).Pending.Count == 1 && WeatherFacts(g).Observation.Condition == new Code("weather.dry"), "later dry phase earns a fresh exposure on both sessions");
            var fresh = WeatherExposure(pair.A).Pending.Single();
            c.That(fresh.ScheduledTick > original.DueTick && fresh.DueTick - fresh.ScheduledTick == 8, "fresh full delay preserves chronology, not an old deadline");
            Both(pair, restored, g => WeatherIncident(g, "binding.watcher").Snapshot.Active, "same actual delayed target incident");
            c.Equal(1, WeatherIncident(pair.A, "binding.watcher").Snapshot.Episode, "weather changes do not duplicate target episodes");
            c.That(!WeatherIncident(pair.A, "binding.source").Blocker.IsNone, "source response still waits for finite physical water");
            foreach (var current in new[] { pair, restored }) Submit(c, current, WeatherSupply(current.A, "binding.source"));
            Both(pair, restored, WeatherLoaded, "real source-response haul earns loaded cargo before delivery");
            c.That(WeatherStock(pair.A, "truck").IsPositive && WeatherStock(pair.A, "binding.source").IsZero, "water is physically aboard, not remotely supplied");
            ReplayPair(c, factory, middleLeft, middleRight, restored, limits, 1, "pending-weather to loaded-source replay");
            restored = WeatherPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var lateLeft = restored.A.RecordReplay(limits); var lateRight = restored.B.RecordReplay(limits);
            c.Equal(pair.A.Hash(), restored.A.Hash(), "loaded water and resource-flow history reattach exactly");
            Both(pair, restored, g => !WeatherIncident(g, "binding.source").Snapshot.Active && WeatherNoHaul(g), "physical source resupply repair and departure complete");
            c.Equal(new BigInteger(Fix.One.Raw), WeatherConsumed(pair.A), "source repair consumes one actual unit once");
            foreach (var current in new[] { pair, restored }) Submit(c, current, WeatherSupply(current.A, "binding.watcher"));
            Both(pair, restored, g => !WeatherIncident(g, "binding.watcher").Snapshot.Active && WeatherNoHaul(g), "physical target resupply and repair complete");
            c.Equal(Fix.FromInt(8), WeatherStock(pair.A, "depot"), "finite depot pays exactly two actual responses");
            c.Equal(new BigInteger(Fix.FromInt(2).Raw), WeatherConsumed(pair.A), "original journal accounts exactly two consumed response units");
            c.That(WeatherStock(pair.A, "truck").IsZero && WeatherStock(pair.A, "binding.source").IsZero && WeatherStock(pair.A, "binding.watcher").IsZero,
                "no stranded or duplicated repair cargo");
            c.That(!WeatherIncident(pair.A, "binding.remote").Snapshot.Active, "unconnected target was never substituted for eligible endpoint");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && restored.Stalls > 0 && restored.ReversedBatches > 0, "both real transports encountered stalls and reordered byte batches");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "settled weather network response restores exactly");
            ReplayPair(c, factory, lateLeft, lateRight, restored, limits, 1, "loaded-source to final-target replay");
            ReplayPair(c, factory, left, right, pair, limits, 4, "full weather incident network replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "all four genuinely advanced worlds converge");
        }
    }
}
