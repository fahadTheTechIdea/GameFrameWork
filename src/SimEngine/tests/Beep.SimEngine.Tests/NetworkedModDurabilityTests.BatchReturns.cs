using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Physical = Beep.SimEngine.Tests.PermanentRemovalTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string NativeBatchReturnCase = "Networked finite batch: native WIP blocked return real truck capacity release and surviving queued work";
        internal const string HalfNativeBatchReturnCase = "Networked finite batch quantum: native half-minute WIP blocked return real capacity release and queued work";
        internal const string IndependentBatchReturnCase = "Networked finite batch contracts: independent recipe WIP blocked return real capacity release and queued work";
        internal const string HalfIndependentBatchReturnCase = "Networked finite batch contract quantum: independent half-minute WIP return physical capacity and queued work";
        private static readonly Code BatchCapability = new Code("contract.process");
        private static TransformationSnapshot BatchFacts(SimGame game)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(FleetEntity(game, "contract.factory"), BatchCapability, out var facts))
                throw new InvalidOperationException("actual finite batch unavailable"); return facts;
        }
        private static Fix BatchStock(SimGame game, string prototype)
        {
            Code code = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(FleetEntity(game, prototype), ContinuousResource(game, "supply"), out var facts);
            if (!code.IsNone) throw new InvalidOperationException("actual finite endpoint stock unavailable/" + code); return facts.Stock;
        }
        private static int BatchOutputs(SimGame game) => game.Seat(0).Snapshot.Entities.Count(e => e.Owner == new PlayerId(0) && e.Def == "contract.result");
        private static string BatchDiagnostics(SimGame game) {
            var facts = BatchFacts(game);
            return "at=" + game.Clock.StepIndex + "/phase=" + facts.Phase + "/queue=" + facts.QueueCount + "/blocker=" + facts.Blocker +
                "/progress=" + facts.Progress + "/wip=" + facts.Wip.Stock.AmountOf(ContinuousResource(game, "supply")) + "/store=" + BatchStock(game, "contract.factory") +
                "/jobs=" + string.Join(";", Physical.Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.Blocker));
        }
        private static Pair BatchPair(SimGame a, SimGame b)
        {
            var observed = new Dictionary<SimGame, long>();
            void Observe(SimGame game)
            {
                if (observed.TryGetValue(game, out long previous) && previous == game.Clock.StepIndex) return;
                ulong before = game.Hash(); var resource = ContinuousResource(game, "supply"); var facts = BatchFacts(game);
                BigInteger consumed = 0;
                foreach (var row in game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates.Where(r => r.Resource == resource)) {
                    if (row.Kind == ResourceFlowKind.Produced || row.Kind == ResourceFlowKind.StockPurchase || row.Kind == ResourceFlowKind.AuthoredCreation ||
                        row.Kind == ResourceFlowKind.Lost || row.Kind == ResourceFlowKind.Destroyed)
                        throw new InvalidOperationException("finite batch fabricated a refund or discarded actual material");
                    if (row.Kind == ResourceFlowKind.Consumed) consumed += row.AmountRaw;
                }
                if ((BigInteger)ContinuousStock(game, "supply").Raw + facts.Wip.Stock.AmountOf(resource).Raw + consumed != Fix.FromInt(20).Raw ||
                    consumed != new BigInteger(Fix.FromInt(BatchOutputs(game) * 3).Raw))
                    throw new InvalidOperationException("original batch stock WIP consumption result accounting disagrees/" + BatchDiagnostics(game));
                if (game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(FleetEntity(game, "contract.factory"), BatchCapability, out _) ||
                    game.Seat(1).View.Feature<InventoryView>().TryGetReservations(FleetEntity(game, "contract.factory"), Code.None, out _))
                    throw new InvalidOperationException("private WIP or retained tool receipt leaked");
                if (before != game.Hash()) throw new InvalidOperationException("finite batch diagnostics changed authority");
                observed[game] = game.Clock.StepIndex;
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, BatchDiagnostics, 0);
        }
        private static Pair BatchRestore(Check c, GameFactory factory, Pair source)
        {
            var saved = BatchPair(factory.Load(source.A.Save()), factory.Load(source.B.Save()));
            c.Equal(source.A.Hash(), saved.A.Hash(), "original finite WIP receipt queue cargo and history restore exactly");
            c.Equal(source.B.Hash(), saved.B.Hash(), "peer actual material and player sequence remain exact"); return saved;
        }
        private static ConfigureOperationCommand BatchControl(SimGame game, bool enabled)
            => new ConfigureOperationCommand { Entity = FleetEntity(game, "contract.factory"), Operation = BatchCapability, Enabled = enabled, Target = Fix.One };
        private static CancelTransformationCommand BatchCancel(SimGame game, int index)
            => new CancelTransformationCommand { Transformer = FleetEntity(game, "contract.factory"), Capability = BatchCapability, Index = index };
        private static bool BatchNoHaul(SimGame game) => !Physical.Jobs(game).Any(j => j.IsHaul && (j.State == JobState.Open || j.State == JobState.Assigned || j.State == JobState.Paused));
        private static void BatchReturnRun(Check c, bool independent, bool half)
        {
            var factory = RecipeDisclosureTests.Factory(independent, "result", half,
                setupEdit: root => {
                    // A normal authored five-unit receiving store admits complete input3/tool1,
                    // then real truck deliveries can exhaust return room. No runtime stock writes.
                    var profiles = root.Get("independent_capacity_policies");
                    for (int i = 0; i < profiles.Count; i++) if (profiles[i].Get("id").Text == "contract.source")
                        profiles[i].Get("limits").Set("unit.capacity.count", JsonValue.Number("5"));
                }, knowledgeEdit: root => root.Get("knowledge_visibility_policies")[0].Set("rules", JsonValue.NewArray()),
                scenarioEdit: scenario => scenario.Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId)));
            SimGame New() => ConstructionPlanTests.NewPoints(factory);
            var pair = BatchPair(New(), New());
            Console.WriteLine("MODULES batch-return-" + (independent ? "independent-" : "native-") + (half ? "half" : "minute") + "=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(!pair.A.Modules.Contains(TycoonModule.ModuleId), "actual batch material and tool return requires no money");
            var limits = new ReplayLimits(16 * 1024 * 1024, 6000); var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            Submit(c, pair, new ConfigureWorkPolicyCommand { Worker = FleetEntity(pair.A, "machine"), Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
            Submit(c, pair, new EnqueueTransformationCommand { Transformer = FleetEntity(pair.A, "contract.factory"), Capability = BatchCapability, Transformation = "contract.make", Count = 4 });
            Reach(pair, g => BatchFacts(g).Phase == TransformationPhase.Working && BatchFacts(g).Progress.WholeTicks > 0, "original finite inputs physically arrive and batch earns real work");
            Submit(c, pair, BatchControl(pair.A, false));
            Reach(pair, g => Result(g, 0, 3)?.Accepted == true, "normal disable preserves partially earned finite head");
            var held = BatchFacts(pair.A); var resource = ContinuousResource(pair.A, "supply");
            c.That(held.QueueCount == 4 && held.Wip.Stock.AmountOf(resource) == Fix.FromInt(3) && held.Tools.Held.AmountOf(resource) == Fix.One && held.Required == (half ? 8 : 4) && BatchOutputs(pair.A) == 0,
                "actual recipe retains original four-minute work, physical input3 and reusable tool1 without completion");
            var restored = BatchRestore(c, factory, pair); var heldLeft = restored.A.RecordReplay(limits); var heldRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, BatchCancel(current.A, 1), Codes.NotFound, 1);
            Both(pair, restored, g => Result(g, 1, 1)?.Refusal == Codes.NotFound && BatchFacts(g).QueueCount == 4,
                "foreign player preview and canonical command refuse without revealing or altering owned queue");
            foreach (var current in new[] { pair, restored }) Submit(c, current, BatchCancel(current.A, 1));
            Both(pair, restored, g => Result(g, 0, 4)?.Accepted == true && BatchFacts(g).QueueCount == 3, "normal tail cancellation removes one actual request not active WIP or progress");
            c.That(BatchFacts(pair.A).Wip.Execution == held.Wip.Execution && BatchFacts(pair.A).Progress.Equals(held.Progress) && BatchFacts(pair.A).Tools.Held.AmountOf(resource) == Fix.One,
                "cancelled pending tail does not release or restart live head commitments");
            c.That(BatchFacts(pair.A).Queue[0].Execution == held.Queue[0].Execution && BatchFacts(pair.A).Queue[2].Execution == held.Queue[3].Execution,
                "early cancellation preserves exact head and final surviving request identities");
            foreach (var current in new[] { pair, restored }) Submit(c, current, BatchCancel(current.A, 3), Codes.InvalidArgument);
            Both(pair, restored, g => Result(g, 0, 5)?.Refusal == Codes.InvalidArgument, "now-missing queue index refuses canonically without deleting a prefix");
            HaulResourcesCommand Fill(SimGame game) => new HaulResourcesCommand { Source = FleetEntity(game, "source"), Destination = FleetEntity(game, "contract.factory"),
                Worker = FleetEntity(game, "machine"), Resource = ContinuousResource(game, "supply"), Ceiling = Fix.FromInt(4), Priority = 80 };
            Both(pair, restored, g => g.Seat(0).Preview(Fill(g)).IsNone, "actual original carrier is available for normal capacity-pressure haul");
            foreach (var current in new[] { pair, restored }) Submit(c, current, Fill(current.A));
            Both(pair, restored, g => Result(g, 0, 6)?.Accepted == true && Physical.Jobs(g).Any(j => j.IsHaul && j.Loaded.IsPositive), "real finite four-unit truck refill leaves source for disabled factory");
            ReplayPair(c, factory, heldLeft, heldRight, restored, limits, 4, "saved finite WIP through foreign refusal exact tail refusal and physically loaded pressure haul replay");
            restored = BatchRestore(c, factory, pair); var fillLeft = restored.A.RecordReplay(limits); var fillRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => BatchStock(g, "contract.factory") == Fix.FromInt(5) && BatchNoHaul(g), "native travel unloading and departure actually fill five-unit store");
            foreach (var current in new[] { pair, restored }) Submit(c, current, BatchCancel(current.A, 0));
            Both(pair, restored, g => Result(g, 0, 7)?.Accepted == true && BatchFacts(g).Phase == TransformationPhase.Returning, "head cancellation waits for physical return space instead of refunding or discarding inputs");
            var returning = BatchFacts(pair.A);
            c.That(returning.Blocker == TransformerManager.BlockerReturnFull && returning.QueueCount == 3 && returning.Wip.Stock.AmountOf(resource) == Fix.FromInt(3) &&
                returning.Wip.Execution == held.Wip.Execution && returning.Progress.Equals(held.Progress) && returning.Tools.Held.AmountOf(resource) == Fix.One && BatchStock(pair.A, "source") == Fix.FromInt(12),
                "real full inlet retains original returning head WIP receipt tool and earned work");
            foreach (var current in new[] { pair, restored }) Submit(c, current, BatchCancel(current.A, 1));
            Both(pair, restored, g => Result(g, 0, 8)?.Accepted == true && BatchFacts(g).QueueCount == 2,
                "ordinary tail cancellation remains legal while actual head waits for physical return room");
            var afterTail = BatchFacts(pair.A);
            c.That(afterTail.Phase == returning.Phase && afterTail.Blocker == returning.Blocker && afterTail.Progress.Equals(returning.Progress) &&
                afterTail.Wip.Execution == returning.Wip.Execution && afterTail.Wip.Stock.AmountOf(resource) == Fix.FromInt(3) &&
                afterTail.Tools.Held.AmountOf(resource) == Fix.One && afterTail.Queue[1].Execution == held.Queue[3].Execution && returning.QueueCount == 3,
                "returning-tail edit preserves exact head material tool work and final request, with detached pre-edit queue");
            foreach (var current in new[] { pair, restored }) Submit(c, current, BatchCancel(current.A, 0), TransformerManager.BlockerReturning);
            Both(pair, restored, g => Result(g, 0, 9)?.Refusal == TransformerManager.BlockerReturning, "duplicate stop cannot release or duplicate returning head inputs");
            ReplayPair(c, factory, fillLeft, fillRight, restored, limits, 3, "saved pressure cargo through full-store head and returning-tail cancellation and duplicate-return refusal replay");
            restored = BatchRestore(c, factory, pair); var returnLeft = restored.A.RecordReplay(limits); var returnRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "native sole clock pauses real blocked material return");
            long at = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, g => g.Clock.StepIndex >= at + 4, "saved blocked return independently advances without room or productive work"); pair.Round();
            c.That(pair.A.Clock.StepIndex == at && pair.A.Hash() == frozen && BatchFacts(restored.A).Progress.Equals(returning.Progress) && BatchOutputs(restored.A) == 0,
                "paused and space-blocked worlds borrow neither return room nor productive credit");
            pair.A.Net.RequestResume(); Both(pair, restored, g => g.Clock.StepIndex >= at + 4, "native blocked return catches up without losing original inputs");
            HaulResourcesCommand Drain(SimGame game) => new HaulResourcesCommand { Source = FleetEntity(game, "contract.factory"), Destination = FleetEntity(game, "destination"),
                Worker = FleetEntity(game, "machine"), Resource = ContinuousResource(game, "supply"), Ceiling = Fix.FromInt(3), Priority = 80 };
            foreach (var current in new[] { pair, restored }) Submit(c, current, Drain(current.A));
            Both(pair, restored, g => Result(g, 0, 10)?.Accepted == true && Physical.Jobs(g).Any(j => j.IsHaul && j.Loaded.IsPositive && j.Loaded < Fix.FromInt(3)),
                "ordinary truck removes positive partial actual stock while native WIP still awaits sufficient room");
            c.That(BatchFacts(pair.A).Wip.Stock.AmountOf(resource) == Fix.FromInt(3) && BatchFacts(pair.A).Phase == TransformationPhase.Returning,
                "partial physical capacity release cannot create an incomplete material return");
            ReplayPair(c, factory, returnLeft, returnRight, restored, limits, 1, "saved full blocked return through sole-clock isolation and physically partial drain replay");
            restored = BatchRestore(c, factory, pair); var drainLeft = restored.A.RecordReplay(limits); var drainRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => BatchFacts(g).QueueCount == 1 && BatchFacts(g).Wip.Execution == 0 && Physical.Jobs(g).Any(j => j.IsHaul && j.Loaded == Fix.FromInt(3)),
                "full real capacity release lets original owner return exact WIP3 while drain cargo is still physical");
            c.That(BatchStock(pair.A, "contract.factory") == Fix.FromInt(5) && BatchFacts(pair.A).Tools.Sources.Count == 0 && BatchFacts(pair.A).Wip.Stock.IsEmpty &&
                returning.Wip.Stock.AmountOf(resource) == Fix.FromInt(3) && BatchOutputs(pair.A) == 0, "actual returned inputs fill same store once, original detached WIP remains unchanged and cancelled head produces no result");
            ReplayPair(c, factory, drainLeft, drainRight, restored, limits, 0, "saved partial drain to exact native WIP return and tool-release replay");
            restored = BatchRestore(c, factory, pair); var returnedLeft = restored.A.RecordReplay(limits); var returnedRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => BatchNoHaul(g), "actual three-unit drain completes travel unloading and departure");
            c.That(BatchStock(pair.A, "destination") == Fix.FromInt(3) && BatchStock(pair.A, "contract.factory") == Fix.FromInt(5) && ContinuousStock(pair.A, "supply") == Fix.FromInt(20),
                "real return and drain conserve twenty original material with no produced refund or lost WIP");
            ReplayPair(c, factory, returnedLeft, returnedRight, restored, limits, 0, "saved actual returned WIP and cargo through complete physical delivery replay");
            restored = BatchRestore(c, factory, pair); var nextLeft = restored.A.RecordReplay(limits); var nextRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, BatchControl(current.A, true));
            Both(pair, restored, g => Result(g, 0, 11)?.Accepted == true && BatchFacts(g).Wip.Execution > 0 && BatchFacts(g).Progress.WholeTicks > 0,
                "normal enable starts only surviving queued batch with fresh real WIP and original duration");
            c.That(BatchFacts(pair.A).Wip.Execution != returning.Wip.Execution && BatchFacts(pair.A).Wip.Stock.AmountOf(resource) == Fix.FromInt(3) && BatchOutputs(pair.A) == 0,
                "surviving request neither reuses cancelled work nor emits a cancelled result");
            ReplayPair(c, factory, nextLeft, nextRight, restored, limits, 1, "saved returned resources through fresh surviving batch startup replay");
            restored = BatchRestore(c, factory, pair); var finalLeft = restored.A.RecordReplay(limits); var finalRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => BatchFacts(g).QueueCount == 0 && BatchOutputs(g) == 1, "full original finite work consumes real inputs and emits surviving result once");
            long quiet = pair.A.Clock.StepIndex + 4; Both(pair, restored, g => g.Clock.StepIndex >= quiet, "settled original result stays once-only across later updates");
            c.That(BatchStock(pair.A, "source") == Fix.FromInt(12) && BatchStock(pair.A, "contract.factory") == Fix.FromInt(2) && BatchStock(pair.A, "destination") == Fix.FromInt(3) &&
                BatchFacts(pair.A).Wip.Stock.IsEmpty && BatchFacts(pair.A).Tools.Sources.Count == 0 && BatchOutputs(pair.A) == 1,
                "only fresh completed surviving batch consumes three units; cancelled head and tail create none");
            ReplayPair(c, factory, finalLeft, finalRight, restored, limits, 0, "saved fresh WIP through one earned result and settled material history replay");
            ReplayPair(c, factory, left, right, pair, limits, 12, "full exact foreign refusal two tail edits head return refusal physical drain and fresh work stream replay");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && pair.A.Hash() == restored.B.Hash(), "four actually advancing finite WIP worlds converge through native shuffled transport");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "settled actual WIP return and surviving result checkpoint");
        }
    }
}
