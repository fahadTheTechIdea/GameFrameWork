using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Pressure = Beep.SimEngine.Tests.IncidentProcessTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string PressureCase = "Networked incident pressure: finite spill, physical phase isolation and saved environmental accounting";
        internal const string IndependentPressureCase = "Networked independent incident pressure: half-minute spill, physical isolation and saved environmental accounting";
        private static EnvironmentalObservation PressureBacklog(SimGame game, string metric = "sensor.spill-backlog")
        {
            var value = game.Seat(0).View.Feature<EnvironmentalMetricsView>().Read(new PlayerId(0), metric);
            if (!value.Available || !value.AmountRaw.HasValue) throw new InvalidOperationException("actual owned spill metric/" + value.Blocker);
            return value;
        }
        private static Fix PressureSpill(SimGame game) => Fix.FromRaw(checked((long)PressureBacklog(game).AmountRaw.Value));
        private static ResourceId PressureResource(SimGame game, string name)
        {
            if (!game.Seat(0).View.TryFindResource(name, out var id)) throw new InvalidOperationException("authored incident resource missing/" + name);
            return id;
        }
        private static void PressureObserve(SimGame game)
        {
            var metric = PressureBacklog(game); var external = PressureBacklog(game, "sensor.extension-backlog");
            if (metric.AmountRaw != external.AmountRaw || metric.Measure != EnvironmentalMeasure.Stock || metric.Rate.HasValue ||
                metric.RateBlocker != EnvironmentalCodes.StockHasNoRate || metric.FromTick != game.Clock.StepIndex || metric.ThroughTick != metric.FromTick)
                throw new InvalidOperationException("current environmental quantity/clock/provider disagreement");
            BigInteger oil = metric.AmountRaw.Value, steel = BigInteger.Zero;
            foreach (var entity in game.Seat(0).Snapshot.Entities) {
                oil += Pressure.Stock(game, entity.Id).Raw; steel += Pressure.Stock(game, entity.Id, "steel").Raw;
            }
            var history = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            foreach (var row in history.Aggregates) {
                if (row.Kind == ResourceFlowKind.AuthoredCreation || row.Kind == ResourceFlowKind.StockPurchase)
                    throw new InvalidOperationException("incident response fabricated material");
                if (row.Resource == PressureResource(game, "steel") && row.Kind == ResourceFlowKind.Consumed) steel += row.AmountRaw;
            }
            if (oil != new BigInteger(Fix.FromInt(5).Raw) || steel != new BigInteger(Fix.FromInt(12).Raw))
                throw new InvalidOperationException("finite incident material lost or duplicated/" + PressureDiagnostics(game));
            var foreign = game.Seat(1).View.Feature<EnvironmentalMetricsView>().Read(new PlayerId(1), "sensor.spill-backlog");
            if (foreign.Available || foreign.Unit.IsValid || foreign.FromTick != 0 || foreign.ThroughTick != 0 || foreign.AmountRaw.HasValue ||
                game.Seat(1).View.Feature<IncidentsView>().TryGetProcess(Pressure.Id(game, "sensor.leaking-site"), new Code("leak-flow"), out _))
                throw new InvalidOperationException("foreign incident/environmental state leaked");
        }
        private static string PressureDiagnostics(SimGame game) => "at=" + game.Clock.StepIndex + "/spill=" + PressureSpill(game) +
            "/tank=" + Pressure.Stock(game, Pressure.Id(game, "sensor.leaking-site")) + "/episode=" + Pressure.Process(game).Episode +
            "/interval=" + Pressure.Process(game).ElapsedTicks + "/" + Pressure.Process(game).RequiredTicks + "/blocker=" + Pressure.Process(game).Blocker +
            "/jobs=" + string.Join(";", Pressure.Jobs(game).Select(job => job.Id + "/" + job.State + "/" + job.ToilIndex));
        private static Pair PressurePair(SimGame a, SimGame b) => new Pair(a, b, PressureObserve, PressureDiagnostics, 0);
        private static bool PressureFree(SimGame game) => Pressure.Jobs(game).All(job => job.Worker != Pressure.Id(game, "sensor.cart"));
        private static RaiseIncidentCommand PressureRaise(SimGame game) => new RaiseIncidentCommand {
            Target = Pressure.Id(game, "sensor.leaking-site"), Source = new Code("leak"), Kind = new Code("sensor.fire") };
        private static HaulResourcesCommand PressureSteel(SimGame game) => new HaulResourcesCommand {
            Source = Pressure.Id(game, "sensor.depot"), Destination = Pressure.Id(game, "sensor.leaking-site"),
            Worker = Pressure.Id(game, "sensor.cart"), Resource = PressureResource(game, "steel"), Ceiling = Fix.One, Priority = 100 };
        private static bool PressurePartialRecovery(SimGame game, int order)
        {
            var nodes = new List<ActivityNodeView>();
            return game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes) &&
                nodes.Any(node => node.Progress.WholeTicks > 0 && node.Progress.WholeTicks < node.RequiredTicks && node.State != NodeState.Done);
        }
        private static void PressureRun(Check c, bool native)
        {
            var factory = Pressure.Factory(native, half: !native, environment: true);
            var pair = PressurePair(Pressure.New(factory), Pressure.New(factory));
            Console.WriteLine("MODULES pressure=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(!pair.A.Modules.Contains(TycoonModule.ModuleId), "periodic incidents and environmental quantities require no business module");
            c.That(Pressure.Process(pair.A).Blocker == IncidentProcessCodes.Inactive && PressureSpill(pair.A).IsZero, "no leak before an actual episode");
            var limits = new ReplayLimits(8 * 1024 * 1024, 3000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            int owner = Submit(c, pair, PressureRaise(pair.A));
            int foreign = Submit(c, pair, PressureRaise(pair.B), Codes.NotFound, seat: 1);
            Reach(pair, game => Result(game, 0, owner)?.Accepted == true && Result(game, 1, foreign)?.Refusal == Codes.NotFound &&
                Pressure.Process(game).ElapsedTicks == 1, "actual first positive partial interval and private foreign refusal");
            var first = Pressure.Process(pair.A); var initialMetric = PressureBacklog(pair.A);
            c.Equal(native ? 2L : 4L, first.RequiredTicks, "unchanged two-minute cadence uses the sole selected game quantum");
            c.That(!first.Due && PressureSpill(pair.A).IsZero, "first positive update cannot transfer a full interval's oil");
            var restored = PressurePair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var partialLeft = restored.A.RecordReplay(limits); var partialRight = restored.B.RecordReplay(limits);
            c.Equal(pair.A.Hash(), restored.A.Hash(), "partial process episode and environmental ownership restore exactly");
            bool Empty(SimGame game) => PressureSpill(game) == Fix.FromInt(5) && Pressure.Process(game).Due && Pressure.Process(game).Blocker == WorkEffectCodes.NoStock;
            Both(pair, restored, Empty, "actual finite tank exhausts and holds exactly one due pulse");
            c.That(Pressure.Stock(pair.A, Pressure.Id(pair.A, "sensor.leaking-site")).IsZero, "five physical pulses exhaust the original tank");
            ReplayPair(c, factory, partialLeft, partialRight, restored, limits, 0, "partial-interval to exhausted-source replay");
            restored = PressurePair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var blockedLeft = restored.A.RecordReplay(limits); var blockedRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, game => game.Clock.Paused, "native network pause at finite-source wait");
            long paused = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, game => game.Clock.StepIndex >= paused + 6 && Empty(game), "independent saved session advances actual bounded due wait");
            pair.Round(); c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen, "paused original borrows no episode age or material");
            c.Equal(first.RequiredTicks, Pressure.Process(restored.A).ElapsedTicks, "starvation cannot bank multiple missed pulses");
            pair.A.Net.RequestResume(); Both(pair, restored, Empty, "native resumption converges with advanced saved wait");
            int recover = 0;
            foreach (var current in new[] { pair, restored }) {
                int ticket = Submit(c, current, new PostOrderCommand { Plan = "sensor.recover-spill-work", Target = Pressure.Id(current.A, "sensor.leaking-site"), Priority = 100 });
                if (recover == 0) recover = ticket; else c.Equal(recover, ticket, "saved next sequence belongs to the original player stream");
            }
            Both(pair, restored, game => Result(game, 0, recover)?.Accepted == true &&
                PressurePartialRecovery(game, checked((int)Result(game, 0, recover).Value.Arg0)), "actual machine earns partial six-minute recovery work");
            int recoveryOrder = checked((int)Result(pair.A, 0, recover).Value.Arg0);
            c.Equal(Fix.FromInt(5), PressureSpill(pair.A), "partial work moves no oil early");
            ReplayPair(c, factory, blockedLeft, blockedRight, restored, limits, 1, "starved-source to partial physical recovery replay");
            restored = PressurePair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var workLeft = restored.A.RecordReplay(limits); var workRight = restored.B.RecordReplay(limits);
            c.That(PressurePartialRecovery(restored.A, recoveryOrder), "save keeps the actual order identity and earned recovery rather than requiring an old result-feed entry");
            Both(pair, restored, game => PressureSpill(game) == Fix.FromInt(2), "completed real recovery withdraws exactly three cell units");
            long recoveredAt = pair.A.Clock.StepIndex;
            Both(pair, restored, game => game.Clock.StepIndex >= recoveredAt + 1, "one due pulse resumes after real replenishment");
            c.Equal(Fix.FromInt(3), PressureSpill(pair.A), "due wait resumes once without a catch-up burst");
            Both(pair, restored, game => Empty(game) && PressureFree(game), "recovered tank earns remaining pulses and worker physically departs");
            foreach (var current in new[] { pair, restored }) Submit(c, current, PressureSteel(current.A));
            Both(pair, restored, game => Pressure.RepairComplete(game) && PressureFree(game), "physical steel supply and timed repair complete the stop phase");
            c.That(Pressure.Incident(pair.A).Snapshot.Active && Pressure.Process(pair.A).Blocker == IncidentProcessCodes.PhaseComplete,
                "phase isolation stops pressure before separately unsupplied cleanup");
            ReplayPair(c, factory, workLeft, workRight, restored, limits, 1, "partial-recovery to physically supplied phase isolation replay");
            restored = PressurePair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var isolatedLeft = restored.A.RecordReplay(limits); var isolatedRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new PostOrderCommand {
                Plan = "sensor.pressure-recover-one", Target = Pressure.Id(current.A, "sensor.leaking-site"), Priority = 100 });
            Both(pair, restored, game => Pressure.Stock(game, Pressure.Id(game, "sensor.leaking-site")) == Fix.One && PressureFree(game),
                "physically recovered oil remains in an isolated site's tank");
            var isolated = PressureBacklog(pair.A); long stoppedAt = pair.A.Clock.StepIndex;
            Both(pair, restored, game => game.Clock.StepIndex >= stoppedAt + first.RequiredTicks * 2 + 1, "actual isolated intervals pass despite available oil");
            c.That(PressureBacklog(pair.A).AmountRaw == isolated.AmountRaw && Pressure.Process(pair.A).ElapsedTicks == 0,
                "saved completed phase and authored reset prevent further leakage");
            foreach (var current in new[] { pair, restored }) Submit(c, current, PressureSteel(current.A));
            Both(pair, restored, game => !Pressure.Incident(game).Snapshot.Active && PressureFree(game), "second real steel supply and cleanup resolve the original episode");
            c.Equal(IncidentProcessCodes.Inactive, Pressure.Process(pair.A).Blocker, "resolved incident has no active pressure");
            ReplayPair(c, factory, isolatedLeft, isolatedRight, restored, limits, 2, "phase-isolated recovery and physical cleanup replay");
            restored = PressurePair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var freshLeft = restored.A.RecordReplay(limits); var freshRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, PressureRaise(current.A));
            Both(pair, restored, game => Pressure.Process(game).Episode == 2 && Pressure.Process(game).ElapsedTicks == 1, "fresh episode earns a new positive partial interval");
            c.That(PressureSpill(pair.A) == Fix.FromInt(4) && Pressure.Stock(pair.A, Pressure.Id(pair.A, "sensor.leaking-site")) == Fix.One,
                "new episode has no instant transfer or inherited due interval");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "new partial episode also round-trips original owners");
            Both(pair, restored, Empty, "new episode earns its last finite physical pulse and waits without invented oil");
            c.That(first.Episode == 1 && first.ElapsedTicks == 1 && initialMetric.AmountRaw == BigInteger.Zero &&
                isolated.AmountRaw == new BigInteger(Fix.FromInt(4).Raw), "old process and metric publications remain detached");
            c.That(pair.Stalls > 0 && restored.Stalls > 0 && pair.ReversedBatches > 0 && restored.ReversedBatches > 0,
                "actual native transport stalls and reorders original byte batches");
            ReplayPair(c, factory, freshLeft, freshRight, restored, limits, 1, "completed-cleanup to fresh finite episode replay");
            ReplayPair(c, factory, left, right, pair, limits, 7, "full two-episode physical incident and private environmental replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(),
                "all four genuinely advanced incident sessions converge");
        }
    }
}
