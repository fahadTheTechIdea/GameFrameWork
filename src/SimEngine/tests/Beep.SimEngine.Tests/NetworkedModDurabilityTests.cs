using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ComposedModProofTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string Case = "Networked colony mod: shuffled peers reattach partial saves and preserve active-session replay";
        static partial void RegisterConsumerCases();
        internal static void Register()
        {
            RegisterConsumerCases();
            TestRunner.Add(Case, Run); TestRunner.Add(WeatherCase, WeatherRun); TestRunner.Add(AutomationCase, AutomationRun);
            TestRunner.Add(CargoRequestCase, CargoRequestsRun);
            TestRunner.Add(ControllerCase, ControllersRun); TestRunner.Add(ProgressionCase, ProgressionRun); TestRunner.Add(ExplorationCase, ExplorationRun);
            TestRunner.Add(PressureCase, c => PressureRun(c, true)); TestRunner.Add(IndependentPressureCase, c => PressureRun(c, false));
            TestRunner.Add(EnvironmentCase, c => EnvironmentRun(c, false)); TestRunner.Add(ExternalEnvironmentCase, c => EnvironmentRun(c, true));
            TestRunner.Add(EquipmentCase, EquipmentRun);
            TestRunner.Add(PolicyRouteCase, c => PolicyRecoveryRun(c, PolicyDispatchKind.Route, false));
            TestRunner.Add(PolicyEvacuationCase, c => PolicyRecoveryRun(c, PolicyDispatchKind.Evacuation, false));
            TestRunner.Add(PolicyCargoCase, c => PolicyRecoveryRun(c, PolicyDispatchKind.CargoReturn, false));
            TestRunner.Add(PolicyContractCargoCase, c => PolicyRecoveryRun(c, PolicyDispatchKind.CargoReturn, true));
            TestRunner.Add(HistoryPhysicalCase, HistoryPhysicalRun); TestRunner.Add(HistoryBudgetCase, HistoryBudgetRun);
            TestRunner.Add(EventPolicyCase, c => EventPolicyRun(c, false)); TestRunner.Add(IndependentEventPolicyCase, c => EventPolicyRun(c, true));
            TestRunner.Add(FleetBatchCase, FleetBatchRun);
            TestRunner.Add(FleetTripCase, FleetTripRun);
            TestRunner.Add(FleetServiceCase, FleetServiceRun);
            TestRunner.Add(FleetHomeCase, FleetHomeRun);
            TestRunner.Add(ScenarioPressureCase, ScenarioPressureRun); TestRunner.Add(ScenarioPolicyCase, ScenarioPolicyRun);
            TestRunner.Add(MultiCompanyPressureCase, MultiCompanyPressureRun);
            TestRunner.Add(MultiCompanyPressureIntegrityCase, c => {
                MultiCompanyPressureRun(c); PressureContentContractTests.MultiCompanySaveIntegrity(c, MultiCompanyPressureFactory());
            });
            TestRunner.Add(InventoryControlCase, c => InventoryControlRun(c, false));
            TestRunner.Add(IndependentInventoryControlCase, c => InventoryControlRun(c, true));
            TestRunner.Add(RemoteLifecycleCase, c => RemoteLifecycleRun(c, false));
            TestRunner.Add(IndependentRemoteLifecycleCase, c => RemoteLifecycleRun(c, true));
            TestRunner.Add(WorkBacklogCase, WorkBacklogRun); TestRunner.Add(WorkQueueCase, WorkQueueRun);
            TestRunner.Add(SmallLockstepCase, c => AuthoredPolicyRun(c, false, "net.small", 4, 4));
            TestRunner.Add(IndependentLockstepCase, c => AuthoredPolicyRun(c, true, "net.independent", 2048, 400));
            TestRunner.Add(UnlimitedLockstepCase, c => AuthoredPolicyRun(c, false, "net.unlimited", null, null));
            TestRunner.Add(ConditionGraphCase, ConditionGraphRun);
            TestRunner.Add(NativeTileConditionCase, c => TilePredicateRun(c, "native"));
            TestRunner.Add(IndependentTileConditionCase, c => TilePredicateRun(c, "band"));
            TestRunner.Add(OwnedTileNetworkCase, c => InfrastructureRun(c, false, false, false));
            TestRunner.Add(OwnedPointNetworkCase, c => InfrastructureRun(c, true, false, true));
            TestRunner.Add(WorldTileNetworkCase, c => InfrastructureRun(c, false, true, true));
            TestRunner.Add(WorldPointNetworkCase, c => InfrastructureRun(c, true, true, false));
            TestRunner.Add(ContinuousSupplyCase, c => ContinuousSupplyRun(c, false));
            TestRunner.Add(HalfContinuousSupplyCase, c => ContinuousSupplyRun(c, true));
            TestRunner.Add(AtomicCargoCase, c => AtomicCargoRun(c, false));
            TestRunner.Add(HalfAtomicCargoCase, c => AtomicCargoRun(c, true));
            TestRunner.Add(NativeBatchReturnCase, c => BatchReturnRun(c, false, false));
            TestRunner.Add(HalfNativeBatchReturnCase, c => BatchReturnRun(c, false, true));
            TestRunner.Add(IndependentBatchReturnCase, c => BatchReturnRun(c, true, false));
            TestRunner.Add(HalfIndependentBatchReturnCase, c => BatchReturnRun(c, true, true));
        }
        // Transport and observation only. No packet decoding, command application or game rules.
        internal sealed class Pair
        {
            internal readonly SimGame A, B;
            private readonly List<byte[]> _left = new List<byte[]>(), _right = new List<byte[]>();
            private int _round;
            private readonly Action<SimGame> _observe;
            private readonly Func<SimGame, string> _describe;
            internal int Stalls, ReversedBatches;
            internal bool SameEpoch => A.Clock.StepIndex == B.Clock.StepIndex;
            internal Pair(SimGame a, SimGame b, Action<SimGame> observe, Func<SimGame, string> describe, int inputDelay)
            {
                _observe = observe ?? throw new ArgumentNullException(nameof(observe));
                _describe = describe ?? throw new ArgumentNullException(nameof(describe));
                A = a; B = b;
                // Observe each committed step, including transient pending decisions.
                // Stalls retain earned time; this public host budget limits one
                // Update, never discards that backlog or bypasses lockstep/pause.
                A.Clock.CatchUpCap = B.Clock.CatchUpCap = 1;
                A.StartLockstep(new LockstepOptions { InputDelay = inputDelay, ChecksumEvery = 1, LocalSeats = { 0 } });
                B.StartLockstep(new LockstepOptions { InputDelay = inputDelay, ChecksumEvery = 1, LocalSeats = { 1 } });
                if (!A.Net.CheckHello(B.Net.Hello()).IsNone || !B.Net.CheckHello(A.Net.Hello()).IsNone)
                    throw new InvalidOperationException("actual peer identity/committed state differs");
            }
            internal string Describe(SimGame game) => _describe(game);
            private static void Drain(SimGame source, List<byte[]> pending)
            { var rows = new List<byte[]>(); source.Net.DrainOutgoing(rows); pending.AddRange(rows); }
            private void Send(List<byte[]> pending, SimGame destination)
            {
                if (pending.Count > 1) ReversedBatches++;
                pending.Reverse(); foreach (byte[] packet in pending) destination.Net.Receive(packet); pending.Clear();
            }
            internal void Round(long? ceiling = null)
            {
                long beforeA = A.Clock.StepIndex, beforeB = B.Clock.StepIndex;
                // Pacing only: don't let the faster host run a new epoch before its
                // peer catches up. Every attempted Update still uses native lockstep.
                long limit = ceiling ?? Math.Min(beforeA, beforeB) + 1;
                if (beforeA < limit) A.Update(); if (beforeB < limit) B.Update();
                if (A.Clock.StepIndex - beforeA > 1 || B.Clock.StepIndex - beforeB > 1)
                    throw new InvalidOperationException("explicit per-Update observation budget exceeded");
                Drain(A, _left); Drain(B, _right); _round++;
                if (_round % 2 == 0) Send(_left, B);
                if (_round % 3 == 0) Send(_right, A);
                if (A.Fault != null || B.Fault != null || A.Net.State == NetState.Broken || B.Net.State == NetState.Broken ||
                    A.Net.State == NetState.Desynced || B.Net.State == NetState.Desynced)
                    throw new InvalidOperationException("native peer failure " + A.Fault + "/" + B.Fault + "/" + A.Net.State + "/" + B.Net.State);
                if (beforeA == A.Clock.StepIndex && beforeB == B.Clock.StepIndex &&
                    (A.Net.State == NetState.Stalled || B.Net.State == NetState.Stalled)) Stalls++;
                _observe(A); _observe(B);
                if (SameEpoch && A.Hash() != B.Hash()) throw new InvalidOperationException("same-epoch peer divergence/" + Describe(A));
            }
        }
        private static void Reach(Pair pair, Func<SimGame, bool> ready, string stage)
        {
            var timer = Stopwatch.StartNew();
            for (int i = 0; i < 1000 && timer.Elapsed.TotalSeconds < 25 &&
                (!pair.SameEpoch || !ready(pair.A) || !ready(pair.B)); i++) pair.Round();
            if (!pair.SameEpoch || !ready(pair.A) || !ready(pair.B))
                throw new InvalidOperationException(stage + "/A=" + pair.Describe(pair.A) + "/B=" + pair.Describe(pair.B));
        }
        private static void Both(Pair original, Pair restored, Func<SimGame, bool> ready, string stage)
        {
            var timer = Stopwatch.StartNew();
            bool Ready(Pair p) => p.SameEpoch && ready(p.A) && ready(p.B);
            for (int i = 0; i < 1200 && timer.Elapsed.TotalSeconds < 25 && (!Ready(original) || !Ready(restored)); i++) {
                if (!Ready(original)) original.Round(); if (!Ready(restored)) restored.Round();
            }
            if (!Ready(original) || !Ready(restored)) throw new InvalidOperationException(stage + "/" + original.Describe(original.A) + "/" + restored.Describe(restored.A));
            // Each independent transport may observe readiness on a different
            // publication. Compare only at one actual committed game-clock epoch.
            long target = Math.Max(original.A.Clock.StepIndex, restored.A.Clock.StepIndex);
            for (int i = 0; i < 1000 && timer.Elapsed.TotalSeconds < 25 &&
                (!original.SameEpoch || !restored.SameEpoch || original.A.Clock.StepIndex != target || restored.A.Clock.StepIndex != target); i++) {
                if (!original.SameEpoch || original.A.Clock.StepIndex < target) original.Round(target);
                if (!restored.SameEpoch || restored.A.Clock.StepIndex < target) restored.Round(target);
            }
            if (original.A.Clock.StepIndex != restored.A.Clock.StepIndex || original.A.Hash() != restored.A.Hash())
                throw new InvalidOperationException("restored active session differs at " + stage + "/" + original.Describe(original.A) + "/" + restored.Describe(restored.A));
            if (!Ready(original) || !Ready(restored)) throw new InvalidOperationException("milestone not retained at common epoch/" + stage);
        }
        internal static CommandResult? Result(SimGame game, int seat, int sequence)
        {
            long cursor = 0; var rows = new List<CommandResult>(); game.Seat(seat).ReadResults(ref cursor, rows);
            foreach (var row in rows) if (row.Stream == CommandStreams.Player && row.Sequence == sequence) return row; return null;
        }
        private static int Submit(Check c, Pair pair, Command command, Code expected = default, int seat = 0)
        {
            var local = seat == 0 ? pair.A : pair.B; ulong hash = local.Hash();
            c.Equal(expected, local.Seat(seat).Preview(command), "actual local seat preview");
            c.Equal(hash, local.Hash(), "network preview mutates no authoritative state");
            return local.Seat(seat).Submit(command);
        }
        private static ConfigureStockPolicyCommand Reserve(SimGame game, ResourceId steel, bool enabled) => new ConfigureStockPolicyCommand {
            Store = Entity(game, "sensor.depot").Id, Resource = steel, Change = LogisticsDemandChange.Replace(
                new LogisticsDemandSettings(enabled, Fix.FromInt(4), Fix.FromInt(4), Fix.FromInt(4), 10, LogisticsReserveAccess.Protected)) };
        private static ActivityNodeView BuildNode(SimGame game, int sequence)
        {
            var result = Result(game, 0, sequence);
            if (!result.HasValue || !result.Value.Accepted || !game.Seat(0).View.Feature<ConstructionView>().TryReadSite(result.Value.Entity, out var site))
                throw new InvalidOperationException("missing actual networked project result/site");
            return Nodes(game, site.Order).Single(n => n.Id == "build");
        }
        private static bool Built(SimGame game) => game.Seat(0).Snapshot.Entities.Count(e => e.Def == "mod.facility") == 1 && Home(game);
        private static void ReplayPair(Check c, GameFactory factory, IReplayRecorder left, IReplayRecorder right,
            Pair pair, ReplayLimits limits, long commands, string phase)
        {
            c.Equal(commands, left.CommandCount, phase + " actual canonical command count");
            c.Equal(commands, right.CommandCount, phase + " peer admits each command once");
            byte[] a = left.Finish(), b = right.Finish(); c.That(a.SequenceEqual(b), phase + " peers record identical canonical replay bytes");
            var verification = new ReplayVerifier().Verify(factory, a, limits);
            c.That(verification.Success, phase + " replay/" + verification.Failure + "/" + verification.Detail);
            c.Equal(pair.A.Hash(), verification.ActualHash, phase + " replay reaches exact current authoritative hash");
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var pair = new Pair(New(factory), New(factory), Observe, Diagnostics, 2);
            Console.WriteLine("MODULES base=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            var limits = new ReplayLimits(4 * 1024 * 1024, 1500);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            c.That(pair.A.Seat(0).View.TryFindResource("steel", out var steel), "public resource identity");
            c.Equal(1, Submit(c, pair, Reserve(pair.A, steel, true)), "first local proposal sequence");
            c.Equal(2, Submit(c, pair, new HaulResourcesCommand { Source = Entity(pair.A, "sensor.depot").Id,
                Destination = Entity(pair.A, "mod.endpoint").Id, Worker = Entity(pair.A, "mod.machine").Id,
                Resource = steel, Ceiling = Fix.FromInt(8), Priority = 100 }), "second local proposal sequence");
            int foreign = Submit(c, pair, Reserve(pair.B, steel, false), Codes.NotFound, seat: 1);
            c.Equal(0L, left.CommandCount, "unadmitted transport proposals aren't replayed as game commands");
            bool PartialLoad(SimGame game) => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero && j.Loaded < Fix.FromInt(8));
            Reach(pair, PartialLoad, "actual partial loading and committed command checkpoint");
            c.That(Result(pair.A, 0, 1)?.Accepted == true && Result(pair.B, 0, 2)?.Accepted == true,
                "both peers admit owner commands through normal engine checks");
            c.That(Result(pair.A, 1, foreign)?.Refusal == Codes.NotFound && Result(pair.B, 1, foreign)?.Refusal == Codes.NotFound,
                "simultaneous foreign proposal has identical private refusal on both peers");
            c.Equal(3L, left.CommandCount, "all proposals now admitted/answered before saving");
            var restored = new Pair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()), Observe, Diagnostics, 2);
            var middleLeft = restored.A.RecordReplay(limits); var middleRight = restored.B.RecordReplay(limits);
            c.Equal(pair.A.Hash(), restored.A.Hash(), "partial load reattaches identical committed simulation");
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "networked sole-clock pause");
            long paused = pair.A.Clock.StepIndex, otherBefore = restored.A.Clock.StepIndex; ulong held = pair.A.Hash();
            for (int i = 0; i < 24; i++) { pair.Round(); restored.Round(); }
            c.Equal(paused, pair.A.Clock.StepIndex, "paused original session advances no simulation age");
            c.Equal(held, pair.A.Hash(), "paused original material/worker state remains fixed");
            c.That(restored.A.Clock.StepIndex > otherBefore && restored.A.Hash() != held, "other actual saved session advances its own physical workflow while original is paused");
            Reach(restored, g => g.Clock.StepIndex >= paused + 4, "active isolated peer alignment");
            long target = restored.A.Clock.StepIndex; pair.A.Net.RequestResume();
            var catchup = Stopwatch.StartNew();
            for (int i = 0; i < 1000 && catchup.Elapsed.TotalSeconds < 25 &&
                (!pair.SameEpoch || pair.A.Clock.StepIndex != target); i++) pair.Round(target);
            c.That(pair.SameEpoch && pair.A.Clock.StepIndex == target && pair.A.Hash() == restored.A.Hash(),
                "network-resumed original catches up to independently advanced restored state");
            bool PartialDelivery(SimGame game) {
                Fix received = Stock(game, Entity(game, "mod.endpoint").Id);
                return received > Fix.Zero && received < Fix.FromInt(8) &&
                    Jobs(game).Any(j => j.IsHaul && j.ToilIndex == 3 && j.Loaded > Fix.Zero);
            }
            Both(pair, restored, PartialDelivery, "actual partially delivered endpoint and residual cargo");
            var delivery = new Pair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()), Observe, Diagnostics, 2);
            var deliveryLeft = delivery.A.RecordReplay(limits); var deliveryRight = delivery.B.RecordReplay(limits);
            c.Equal(pair.A.Hash(), delivery.A.Hash(), "partial delivery preserves actual received stock, residual cargo and commitments");
            c.That(PartialDelivery(delivery.A) && PartialDelivery(delivery.B), "load does not reset unload or pretend full delivery");
            bool Supplied(SimGame game) => Stock(game, Entity(game, "mod.endpoint").Id) == Fix.FromInt(8) && Home(game);
            Both(pair, restored, Supplied, "native supply and home completion");
            Reach(delivery, Supplied, "restored partial delivery completes without another hauling command");
            Both(pair, delivery, Supplied, "partial-delivery continuation reaches original committed state");
            Both(pair, restored, Supplied, "all active branches share the same supply epoch");
            c.Equal(pair.A.Hash(), delivery.A.Hash(), "partial-delivery saved branch has identical final physical result");
            ReplayPair(c, factory, deliveryLeft, deliveryRight, delivery, limits, 0, "partial-delivery zero-input replay segment");
            ConstructCommand Build() => new ConstructCommand { Def = "mod.facility", Priority = 80,
                Placement = Json.Parse("{\"space\":\"sensor.tiles\",\"x\":18,\"y\":18}") };
            int project = Submit(c, pair, Build()); c.Equal(project, Submit(c, restored, Build()), "reattachment preserves next native command sequence");
            bool Productive(SimGame game) {
                if (!Result(game, 0, project).HasValue) return false;
                var node = BuildNode(game, project); return node.Progress.WholeTicks > 0 && node.Progress.WholeTicks < node.RequiredTicks;
            }
            Both(pair, restored, Productive, "actual independently authored project work");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "networked productive project saves original work and reservations");
            Both(pair, restored, Built, "physical project completion");
            foreach (var current in new[] { pair, restored }) {
                Submit(c, current, new SetWorkerHeldCommand { Worker = Entity(current.A, "mod.machine").Id, Held = true });
                Submit(c, current, new RaiseIncidentCommand { Target = Entity(current.A, "mod.endpoint").Id,
                    Source = new Code("failure"), Kind = new Code("unsupported.kind") }, Codes.InvalidArgument);
                Submit(c, current, new RaiseIncidentCommand { Target = Entity(current.A, "mod.endpoint").Id,
                    Source = new Code("failure"), Kind = new Code("mod.equipment-failure") });
            }
            bool Blocked(SimGame game) => Incident(game).Snapshot.Active &&
                Nodes(game, Incident(game).OrderId).Single(n => n.Id == "repair").Blocker == WorkResourceCodes.Stock;
            Both(pair, restored, Blocked, "real blocked incident on every active peer");
            ReplayPair(c, factory, middleLeft, middleRight, restored, limits, 4, "mid-load replay segment");
            restored = new Pair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()), Observe, Diagnostics, 2);
            var lateLeft = restored.A.RecordReplay(limits); var lateRight = restored.B.RecordReplay(limits);
            c.Equal(pair.A.Hash(), restored.A.Hash(), "active blocked episode reattaches through original codecs");
            foreach (var current in new[] { pair, restored }) {
                c.Equal(7, Submit(c, current, Reserve(current.A, steel, false)), "late reattachment preserves actual prior command sequence");
                c.Equal(8, Submit(c, current, new SetWorkerHeldCommand { Worker = Entity(current.A, "mod.machine").Id, Held = false }),
                    "late reattachment neither restarts nor duplicates command sequence");
            }
            bool Repairing(SimGame game) {
                var node = Nodes(game, Incident(game).OrderId).Single(n => n.Id == "repair");
                return node.Progress.WholeTicks > 0 && node.Progress.WholeTicks < node.RequiredTicks;
            }
            Both(pair, restored, Repairing, "physical networked response resupply and work");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "partially earned networked response saves");
            Both(pair, restored, g => !Incident(g).Snapshot.Active && Home(g), "native repaired episode and worker return");
            c.Equal(Fix.FromInt(2), Stock(pair.A), "same actual twelve minus build eight and repair two");
            c.Equal(Fix.Zero, Signal(restored.A).Value.Value, "restored typed signal reads actual consumed endpoint stock");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && restored.Stalls > 0 && restored.ReversedBatches > 0,
                "real native transport stalls and reordered batches occurred, not fabricated frame application");
            ReplayPair(c, factory, lateLeft, lateRight, restored, limits, 2, "blocked-incident replay segment");
            ReplayPair(c, factory, left, right, pair, limits, 9, "full initial-session replay");
            c.Equal(pair.A.Hash(), pair.B.Hash(), "original peers end identically");
            c.Equal(pair.A.Hash(), restored.A.Hash(), "genuinely advanced restored session ends identically");
            c.Equal(restored.A.Hash(), restored.B.Hash(), "restored peers end identically");
        }
    }
}
