using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Equipment = Beep.SimEngine.Tests.EquipmentConditionTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string EquipmentCase = "Networked equipment: authored wear, physical cancelled service and original ten-hour processing";
        private static EntityView EquipmentEntity(SimGame game, int seat = 0) => Colony.Mine(game, seat, "equipment.session-machine").Single();
        private static ConditionSnapshot EquipmentCondition(SimGame game) => Equipment.Read(game, EquipmentEntity(game).Id);
        private static ConditionWearSnapshot EquipmentWear(SimGame game) => ConditionWearTests.Wear(game, EquipmentEntity(game).Id);
        private static TransformationSnapshot EquipmentProcess(SimGame game) => Equipment.State(game, EquipmentEntity(game).Id);
        private static ResourceId EquipmentSteel(SimGame game)
        {
            if (!game.Seat(0).View.TryFindResource("steel", out var steel)) throw new InvalidOperationException("authored equipment material missing");
            return steel;
        }
        private static LogisticsOperationalSnapshot EquipmentStock(SimGame game, int seat = 0)
        {
            Code refusal = game.Seat(seat).View.Feature<LogisticsView>().ReadDiagnostics(EquipmentEntity(game, seat).Id, EquipmentSteel(game), out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("owned equipment stock/" + refusal); return value;
        }
        private static string EquipmentDiagnostics(SimGame game) => "at=" + game.Clock.StepIndex + "/condition=" + EquipmentCondition(game).Value +
            "/exposure=" + EquipmentWear(game).Exposure.ElapsedTicks + "/wear=" + EquipmentWear(game).Decision.Blocker + "/stock=" + EquipmentStock(game).Stock +
            "/process=" + EquipmentProcess(game).Phase + "/progress=" + EquipmentProcess(game).Progress + "/blocker=" + EquipmentProcess(game).Blocker;
        private static Pair EquipmentPair(SimGame a, SimGame b)
        {
            var observed = new Dictionary<SimGame, long>();
            void Observe(SimGame game)
            {
                if (observed.TryGetValue(game, out long prior) && prior == game.Clock.StepIndex) return;
                var process = EquipmentProcess(game); BigInteger stock = EquipmentStock(game).Stock.Raw + process.Wip.Stock.AmountOf(EquipmentSteel(game)).Raw;
                BigInteger produced = 0, consumed = 0;
                foreach (var row in game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates) {
                    if (row.Kind == ResourceFlowKind.AuthoredCreation || row.Kind == ResourceFlowKind.StockPurchase)
                        throw new InvalidOperationException("equipment workflow fabricated material");
                    if (row.Resource != EquipmentSteel(game)) continue;
                    if (row.Kind == ResourceFlowKind.Produced) produced += row.AmountRaw;
                    if (row.Kind == ResourceFlowKind.Consumed) consumed += row.AmountRaw;
                }
                if (new BigInteger(Fix.FromInt(30).Raw) + produced != stock + consumed)
                    throw new InvalidOperationException("equipment steel/WIP/tool disposition lost or duplicated/" + EquipmentDiagnostics(game));
                var foreign = game.Seat(0).View.Feature<EquipmentConditionView>(); var other = EquipmentEntity(game, 1).Id;
                if (foreign.Read(other, Equipment.Source, out var hidden) != Codes.NotFound || hidden.Unit.IsValid ||
                    foreign.ReadWear(other, Equipment.Source, out var hiddenWear) != Codes.NotFound || hiddenWear.Rule != null || hiddenWear.Unit.IsValid ||
                    game.Seat(1).View.Feature<EquipmentConditionView>().Read(other, Equipment.Source, out var own) != Code.None || own.Value != Fix.FromInt(10) ||
                    EquipmentStock(game, 1).Stock != Fix.FromInt(30)) throw new InvalidOperationException("private or isolated foreign equipment changed");
                observed[game] = game.Clock.StepIndex;
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, EquipmentDiagnostics, 0);
        }
        private static Pair EquipmentRestore(Check c, GameFactory factory, Pair source)
        {
            var pair = EquipmentPair(factory.Load(source.A.Save()), factory.Load(source.B.Save()));
            c.Equal(source.A.Hash(), pair.A.Hash(), "actual condition/exposure/WIP/work/material codecs restore together");
            c.Equal(source.B.Hash(), pair.B.Hash(), "actual peer restores the same committed state");
            c.Equal(EquipmentWear(source.A).Exposure.ElapsedTicks, EquipmentWear(pair.A).Exposure.ElapsedTicks, "saved eligible wear age is not a second clock");
            c.Equal(EquipmentCondition(source.A).ChangedTick, EquipmentCondition(pair.A).ChangedTick, "saved native condition change chronology");
            return pair;
        }
        private static ConfigureOperationCommand EquipmentWearControl(SimGame game, bool enabled) => new ConfigureOperationCommand {
            Entity = EquipmentEntity(game).Id, Operation = new Code("sensor"), Enabled = enabled, Target = Fix.One };
        private static ActivityNodeView EquipmentNode(SimGame game, int order)
        {
            var nodes = new List<ActivityNodeView>();
            if (!game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes)) throw new InvalidOperationException("actual equipment service order missing/" + order);
            return nodes.Single(node => node.Id == "repair");
        }
        private static bool EquipmentFree(SimGame game)
        {
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
            return jobs.All(job => job.Worker != Colony.Mine(game, 0, "crane").Single().Id);
        }
        private static bool EquipmentPartial(SimGame game, int sequence)
        {
            var answer = Result(game, 0, sequence);
            if (answer?.Accepted != true) return false;
            var node = EquipmentNode(game, checked((int)answer.Value.Arg0));
            return node.Progress.WholeTicks > 0 && node.Progress.WholeTicks < node.RequiredTicks && node.State != NodeState.Done;
        }
        private static void EquipmentRun(Check c)
        {
            var factory = Equipment.Factory(session: true);
            var pair = EquipmentPair(Equipment.New(factory, "equipment-session"), Equipment.New(factory, "equipment-session"));
            Console.WriteLine("MODULES equipment=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(!pair.A.Modules.Contains(TycoonModule.ModuleId) && pair.A.Modules.Contains(EquipmentConditionModule.ModuleId), "authored survival equipment selects condition/physical work, not business");
            c.That(EquipmentCondition(pair.A).Value == Fix.FromInt(10) && EquipmentWear(pair.A).Exposure.ElapsedTicks == 0 &&
                EquipmentWear(pair.A).Decision.Blocker == new Code("equipment.wear-disabled"), "authored initial operating policy prevents unrequested wear");
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            int queue = Submit(c, pair, new EnqueueTransformationCommand { Transformer = EquipmentEntity(pair.A).Id, Capability = Equipment.Process, Transformation = "equipment.batch" });
            int foreign = Submit(c, pair, new SetWorkPermitCommand { Target = EquipmentEntity(pair.B).Id, Enabled = false }, Codes.NotOwned, seat: 1);
            Reach(pair, game => Result(game, 0, queue)?.Accepted == true && Result(game, 1, foreign)?.Refusal == Codes.NotOwned &&
                EquipmentProcess(game).Progress.WholeTicks > 0, "original ten-hour processing earns positive work with real WIP");
            var working = EquipmentProcess(pair.A); long execution = working.Wip.Execution;
            c.Equal(600L, working.Required, "unchanged ten-hour processing follows the sole minute clock");
            c.Equal(Fix.FromInt(2), working.Wip.Stock.AmountOf(EquipmentSteel(pair.A)), "two actual inputs reside in original WIP");
            Submit(c, pair, EquipmentWearControl(pair.A, true));
            Reach(pair, game => EquipmentCondition(game).Value == Fix.FromInt(9) && EquipmentWear(game).Exposure.ElapsedTicks == 1, "first actual eligible wear minute");
            var early = EquipmentCondition(pair.A); var earlyWear = EquipmentWear(pair.A);
            var restored = EquipmentRestore(c, factory, pair);
            var wearLeft = restored.A.RecordReplay(limits); var wearRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => EquipmentCondition(game).Value == Fix.FromInt(4), "six real eligible minutes degrade equipment without direct damage injection");
            foreach (var current in new[] { pair, restored }) Submit(c, current, EquipmentWearControl(current.A, false));
            Both(pair, restored, game => EquipmentWear(game).Decision.Blocker == new Code("equipment.wear-disabled") &&
                EquipmentProcess(game).Blocker == new Code("equipment.unsafe") && EquipmentEntity(game).State == new Code("state.stopped"), "normal wear interruption and next entity-update safety state");
            var damaged = EquipmentCondition(pair.A); var blocked = EquipmentProcess(pair.A);
            c.Equal(6L, EquipmentWear(pair.A).Exposure.ElapsedTicks, "disabled wear retains exact earned eligibility, not total world age");
            c.Equal(execution, blocked.Wip.Execution, "unsafe processing retains its original execution");
            pair.A.Net.RequestPause(); Reach(pair, game => game.Clock.Paused, "native network pause on the damaged process");
            long paused = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, game => game.Clock.StepIndex >= paused + 8, "other actual saved world advances disabled-wear/unsafe-work wait");
            pair.Round(); c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen, "paused original borrows no wear, condition, work or material from its advancing twin");
            c.That(EquipmentCondition(restored.A).Value == Fix.FromInt(4) && EquipmentWear(restored.A).Exposure.ElapsedTicks == 6 &&
                EquipmentProcess(restored.A).Progress.Equals(blocked.Progress), "elapsed time cannot bypass explicit wear or process safety gates");
            pair.A.Net.RequestResume(); Both(pair, restored, game => EquipmentProcess(game).Blocker == new Code("equipment.unsafe"), "native resumption converges at the same actual epoch");
            int service = 0;
            foreach (var current in new[] { pair, restored }) {
                Submit(c, current, new SetWorkPermitCommand { Target = EquipmentEntity(current.A).Id, Enabled = false });
                int ticket = Submit(c, current, new PostOrderCommand { Target = EquipmentEntity(current.A).Id, Plan = "equipment.service" });
                if (service == 0) service = ticket; else c.Equal(service, ticket, "saved blocked service keeps actual player sequence");
            }
            Both(pair, restored, game => Result(game, 0, service)?.Accepted == true && EquipmentNode(game, checked((int)Result(game, 0, service).Value.Arg0)).Blocker ==
                new Code("equipment.service-disabled"), "authored work-permit requirement blocks real queued service");
            int order = checked((int)Result(pair.A, 0, service).Value.Arg0); long waiting = pair.A.Clock.StepIndex;
            Both(pair, restored, game => game.Clock.StepIndex >= waiting + 10, "blocked service earns neither work nor free repair over actual time");
            c.That(EquipmentNode(pair.A, order).Progress.Equals(WorkProgress.Zero) && EquipmentStock(pair.A).Stock == Fix.FromInt(28) &&
                EquipmentStock(pair.A).OutboundPromised.IsZero, "blocked service neither spends spares nor acquires tools");
            ReplayPair(c, factory, wearLeft, wearRight, restored, limits, 3, "early-wear to unsafe and permit-blocked physical service replay");
            restored = EquipmentRestore(c, factory, pair);
            var permitLeft = restored.A.RecordReplay(limits); var permitRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new SetWorkPermitCommand { Target = EquipmentEntity(current.A).Id, Enabled = true });
            Both(pair, restored, game => EquipmentNode(game, order).Progress.WholeTicks > 0, "actual machine travel and positive service work after normal permit command");
            c.Equal(30L, EquipmentNode(pair.A, order).RequiredTicks, "original half-hour service is not shortened");
            c.That(EquipmentCondition(pair.A).Value == Fix.FromInt(4) && EquipmentStock(pair.A).Stock == Fix.FromInt(25) &&
                EquipmentStock(pair.A).OutboundPromised == Fix.FromInt(2), "partial service pays three real spares, holds two real tools and cannot repair early");
            ReplayPair(c, factory, permitLeft, permitRight, restored, limits, 1, "blocked-service to physically productive paid service replay");
            restored = EquipmentRestore(c, factory, pair);
            var serviceLeft = restored.A.RecordReplay(limits); var serviceRight = restored.B.RecordReplay(limits);
            c.That(EquipmentNode(restored.A, order).Progress.WholeTicks > 0, "load preserves actual earned work under the same order ID");
            int cancel = 0;
            foreach (var current in new[] { pair, restored }) {
                int ticket = Submit(c, current, new CancelOrderCommand { Order = order });
                if (cancel == 0) cancel = ticket; else c.Equal(cancel, ticket, "saved service cancellation preserves player identity");
            }
            Both(pair, restored, game => Result(game, 0, cancel)?.Accepted == true && EquipmentStock(game).OutboundPromised.IsZero && EquipmentFree(game), "cancelled service releases actual tools and machine occupancy");
            c.That(EquipmentCondition(pair.A).Value == Fix.FromInt(4) && EquipmentStock(pair.A).Stock == Fix.FromInt(25), "cancel cannot repair or refund consumed spares");
            int replacement = 0;
            foreach (var current in new[] { pair, restored }) {
                int ticket = Submit(c, current, new PostOrderCommand { Target = EquipmentEntity(current.A).Id, Plan = "equipment.service" });
                if (replacement == 0) replacement = ticket; else c.Equal(replacement, ticket, "new service is an ordinary fresh intent in both branches");
            }
            Both(pair, restored, game => EquipmentPartial(game, replacement), "replacement service physically earns fresh paid progress");
            c.That(Result(pair.A, 0, replacement).Value.Arg0 != order && EquipmentStock(pair.A).Stock == Fix.FromInt(22) &&
                EquipmentCondition(pair.A).Value == Fix.FromInt(4), "fresh execution pays its own spares without inheriting cancelled completion");
            Both(pair, restored, game => EquipmentCondition(game).Value == Fix.FromInt(10) && EquipmentStock(game).OutboundPromised.IsZero && EquipmentFree(game) &&
                EquipmentProcess(game).Progress.WholeTicks > blocked.Progress.WholeTicks, "earned physical repair restores the same process and releases its tools/machine");
            var repaired = EquipmentCondition(pair.A);
            c.That(EquipmentWear(pair.A).Exposure.ElapsedTicks == 6 && EquipmentProcess(pair.A).Wip.Execution == execution,
                "repair preserves real wear history and original in-flight processing execution");
            ReplayPair(c, factory, serviceLeft, serviceRight, restored, limits, 2, "partial-service through cancellation and actual replacement repair replay");
            restored = EquipmentRestore(c, factory, pair);
            var processLeft = restored.A.RecordReplay(limits); var processRight = restored.B.RecordReplay(limits);
            for (int eighth = 1; eighth <= 4; eighth++) {
                long target = working.Required * eighth / 8;
                Both(pair, restored, game => EquipmentProcess(game).Progress.WholeTicks >= target, "real original processing milestone " + eighth + "/8");
            }
            c.That(EquipmentProcess(pair.A).Wip.Execution == execution && EquipmentCondition(pair.A).ChangedTick == repaired.ChangedTick,
                "hours of resumed work neither reset execution nor repeat completed repair");
            ReplayPair(c, factory, processLeft, processRight, restored, limits, 0, "physically repaired to half-complete original processing replay");
            restored = EquipmentRestore(c, factory, pair);
            var finalLeft = restored.A.RecordReplay(limits); var finalRight = restored.B.RecordReplay(limits);
            for (int eighth = 5; eighth <= 7; eighth++) {
                long target = working.Required * eighth / 8;
                Both(pair, restored, game => EquipmentProcess(game).Progress.WholeTicks >= target, "real saved processing milestone " + eighth + "/8");
            }
            Both(pair, restored, game => EquipmentProcess(game).QueueCount == 0, "original ten-hour batch finishes once through all earned game time");
            c.That(EquipmentStock(pair.A).Stock == Fix.FromInt(23) && EquipmentProcess(pair.A).Wip.Stock.IsEmpty && EquipmentProcess(pair.A).Tools.Held.IsEmpty,
                "thirty minus two feed minus six consumed service spares plus one earned output leaves exactly twenty-three");
            foreach (var current in new[] { pair, restored }) Submit(c, current, EquipmentWearControl(current.A, true));
            Both(pair, restored, game => EquipmentCondition(game).Value == Fix.FromInt(9) && EquipmentWear(game).Exposure.ElapsedTicks == 7, "repaired equipment earns fresh damage on a later real eligible minute");
            Both(pair, restored, game => EquipmentCondition(game).Value == Fix.FromInt(4), "second physical wear interval retains cumulative provenance");
            foreach (var current in new[] { pair, restored }) Submit(c, current, EquipmentWearControl(current.A, false));
            Both(pair, restored, game => EquipmentWear(game).Decision.Blocker == new Code("equipment.wear-disabled"), "normal final wear shutdown");
            c.That(EquipmentWear(pair.A).Exposure.ElapsedTicks == 12 && EquipmentCondition(pair.A).Value == Fix.FromInt(4) &&
                EquipmentStock(pair.A).Stock == Fix.FromInt(23) && EquipmentStock(pair.A).OutboundPromised.IsZero,
                "repeat wear neither resets age, fabricates a second output nor repeats a free repair");
            c.That(early.Value == Fix.FromInt(9) && earlyWear.Exposure.ElapsedTicks == 1 && damaged.Value == Fix.FromInt(4) && working.Wip.Stock.AmountOf(EquipmentSteel(pair.A)) == Fix.FromInt(2),
                "retained condition, exposure and WIP facts remain detached through real hours of play");
            c.That(pair.Stalls > 0 && restored.Stalls > 0 && pair.ReversedBatches > 0 && restored.ReversedBatches > 0, "actual native original/restored transports stall and reorder bytes");
            ReplayPair(c, factory, finalLeft, finalRight, restored, limits, 2, "saved productive hours through batch completion and fresh wear replay");

            // Continue the SAME authored workflow to actual failure. No damage/spawn/clock
            // injection: original wear, condition, queue, work and inventory owners decide it.
            restored = EquipmentRestore(c, factory, pair);
            var failureLeft = restored.A.RecordReplay(limits); var failureRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, EquipmentWearControl(current.A, true));
            Both(pair, restored, game => EquipmentCondition(game).Value.IsZero, "four later eligible minutes reach real zero-condition failure");
            foreach (var current in new[] { pair, restored }) Submit(c, current, EquipmentWearControl(current.A, false));
            Both(pair, restored, game => game.Seat(0).View.Feature<OperationsView>().TryGetControl(EquipmentEntity(game).Id, new Code("sensor"), out var wearPolicy) &&
                !wearPolicy.Enabled && EquipmentWear(game).Decision.Blocker == ConditionWearCodes.Exhausted &&
                EquipmentEntity(game).State == new Code("state.stopped"), "failed equipment stays exhausted and stopped after actual wear shutdown command");
            var failed = EquipmentCondition(pair.A);
            c.That(failed.Value.IsZero && EquipmentWear(pair.A).Exposure.ElapsedTicks == 16 && EquipmentStock(pair.A).Stock == Fix.FromInt(23),
                "failure is earned by cumulative16 wear minutes with no stock or output mutation");
            ReplayPair(c, factory, failureLeft, failureRight, restored, limits, 2, "later eligible wear through zero-condition failure replay");
            restored = EquipmentRestore(c, factory, pair);
            c.That(EquipmentCondition(restored.A).Value.IsZero && EquipmentWear(restored.A).Exposure.ElapsedTicks == 16,
                "saved failed machine does not regain condition or forget wear history");
            var failedLeft = restored.A.RecordReplay(limits); var failedRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new EnqueueTransformationCommand {
                Transformer = EquipmentEntity(current.A).Id, Capability = Equipment.Process, Transformation = "equipment.batch" });
            Both(pair, restored, game => EquipmentProcess(game).QueueCount == 1 && EquipmentProcess(game).Blocker == new Code("equipment.unsafe"),
                "failed process accepts intent but cannot begin unsafe production");
            c.That(EquipmentProcess(pair.A).Progress.Equals(WorkProgress.Zero) && EquipmentProcess(pair.A).Wip.Stock.IsEmpty && EquipmentStock(pair.A).Stock == Fix.FromInt(23),
                "zero-condition production gate precedes feed consumption and productive progress");
            int failedService = 0;
            foreach (var current in new[] { pair, restored }) {
                int ticket = Submit(c, current, new PostOrderCommand { Target = EquipmentEntity(current.A).Id, Plan = "equipment.service" });
                if (failedService == 0) failedService = ticket; else c.Equal(failedService, ticket, "failed-machine service retains actual saved player identity");
            }
            Both(pair, restored, game => EquipmentPartial(game, failedService), "real machine travels and earns partial repair on failed equipment");
            int failedOrder = checked((int)Result(pair.A, 0, failedService).Value.Arg0);
            c.That(EquipmentNode(pair.A, failedOrder).RequiredTicks == 30 && EquipmentCondition(pair.A).Value.IsZero &&
                EquipmentStock(pair.A).Stock == Fix.FromInt(20) && EquipmentStock(pair.A).OutboundPromised == Fix.FromInt(2) &&
                EquipmentProcess(pair.A).Wip.Stock.IsEmpty && EquipmentProcess(pair.A).Progress.Equals(WorkProgress.Zero),
                "partial failed repair pays original three spares and holds two tools without early condition/feed/work");
            ReplayPair(c, factory, failedLeft, failedRight, restored, limits, 2, "failed queue admission through physically paid partial repair replay");
            restored = EquipmentRestore(c, factory, pair);
            c.That(EquipmentNode(restored.A, failedOrder).Progress.WholeTicks > 0 && EquipmentCondition(restored.A).Value.IsZero &&
                EquipmentStock(restored.A).Stock == Fix.FromInt(20) && EquipmentStock(restored.A).OutboundPromised == Fix.FromInt(2),
                "failed partial-repair save retains same earned execution and real tool claims");
            var restartLeft = restored.A.RecordReplay(limits); var restartRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => EquipmentCondition(game).Value == Fix.FromInt(6) && EquipmentStock(game).OutboundPromised.IsZero && EquipmentFree(game) &&
                EquipmentProcess(game).Progress.WholeTicks > 0 && EquipmentProcess(game).Blocker.IsNone,
                "original six-unit physical repair reaches authored safe threshold and restarts queued work");
            c.That(EquipmentWear(pair.A).Exposure.ElapsedTicks == 16 && EquipmentStock(pair.A).Stock == Fix.FromInt(18) &&
                EquipmentProcess(pair.A).Wip.Stock.AmountOf(EquipmentSteel(pair.A)) == Fix.FromInt(2) &&
                EquipmentProcess(pair.A).Wip.Execution != execution && EquipmentProcess(pair.A).Required == 600,
                "safe restart is fresh original ten-hour work: no free full repair, repeated spares or second output");
            c.That(failed.Value.IsZero && damaged.Value == Fix.FromInt(4) && repaired.Value == Fix.FromInt(10),
                "retained failed, damaged and repaired condition facts remain immutable");
            ReplayPair(c, factory, restartLeft, restartRight, restored, limits, 0, "saved failed partial repair through safe productive restart replay");
            ReplayPair(c, factory, left, right, pair, limits, 15, "full authored equipment/service/original-processing/failure/restart replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "all four genuinely advanced equipment sessions converge");
            EquipmentRestore(c, factory, pair);
        }
    }
}
