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
        internal const string ContinuousSupplyCase = "Networked continuous production: finite hauled supply fractional work tools and saved stop disposition";
        internal const string HalfContinuousSupplyCase = "Networked continuous production quantum: half-minute finite supply preserves rates tools and saved stop disposition";
        internal const string AtomicCargoCase = "Networked cargo contracts: shared full inventory process tools protected stock and saved stop partition";
        internal const string HalfAtomicCargoCase = "Networked cargo contract quantum: half-minute shared conversion preserves exact stock and saved tool release";
        private static readonly Code ContinuousCapability = new Code("process");
        private static TransformationSnapshot ContinuousFacts(SimGame game, string prototype)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(FleetEntity(game, prototype), ContinuousCapability, out var facts))
                throw new InvalidOperationException("actual named continuous processor unavailable"); return facts;
        }
        private static ResourceId ContinuousResource(SimGame game, string name)
        {
            if (!game.Seat(0).View.TryFindResource(name, out var resource)) throw new InvalidOperationException("actual continuous resource missing/" + name); return resource;
        }
        private static Fix ContinuousStock(SimGame game, string name)
        {
            Fix total = Fix.Zero;
            foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0))) {
                if (entity.Def == "atomic.factory") {
                    var resource = ContinuousResource(game, name);
                    Code result = game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(entity.Id, new Code("shared"), resource, out var port);
                    if (!result.IsNone) throw new InvalidOperationException("explicit independent inventory port unavailable/" + result);
                    total += port.Stock.AmountOf(resource); continue;
                }
                var rows = new List<Facet>(); game.Seat(0).View.Inspect(entity.Id, rows);
                foreach (var row in rows.Where(f => f.Key == Storage.FacetStock && f.Subject == name)) total += row.Value;
            }
            return total;
        }
        private static string ContinuousDiagnostics(SimGame game, string prototype) {
            var row = ContinuousFacts(game, prototype);
            return "at=" + game.Clock.StepIndex + "/phase=" + row.Phase + "/blocker=" + row.Blocker + "/progress=" + row.Progress +
                "/execution=" + row.Wip.Execution + "/fuel-time=" + row.Operating.ElapsedTicks + "/jobs=" +
                string.Join(";", Physical.Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.Blocker));
        }
        private static CargoDispositionSnapshot AtomicCargo(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<LogisticsView>().ReadCargoDisposition(FleetEntity(game, "atomic.factory"), out var facts);
            if (!code.IsNone) throw new InvalidOperationException("actual atomic cargo refused/" + code); return facts;
        }
        private static Pair ContinuousPair(SimGame a, SimGame b, bool atomic)
        {
            string prototype = atomic ? "atomic.factory" : "flow.factory";
            var observed = new Dictionary<SimGame, long>();
            void Observe(SimGame game)
            {
                // Network stalls publish no new simulation resource state. Check
                // every committed step, not the same whole-world codecs per byte batch.
                if (observed.TryGetValue(game, out long previous) && previous == game.Clock.StepIndex) return;
                ulong before = game.Hash(); var current = ContinuousFacts(game, prototype);
                foreach (var name in atomic ? new[] { "supply" } : new[] { "supply", "flow.product", "flow.residue" }) {
                    var resource = ContinuousResource(game, name); BigInteger consumed = 0, produced = 0;
                    foreach (var row in game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates.Where(r => r.Resource == resource)) {
                        if (row.Kind == ResourceFlowKind.AuthoredCreation || row.Kind == ResourceFlowKind.StockPurchase || row.Kind == ResourceFlowKind.Lost || row.Kind == ResourceFlowKind.Destroyed)
                            throw new InvalidOperationException("continuous process fabricated or discarded finite material");
                        if (row.Kind == ResourceFlowKind.Consumed) consumed += row.AmountRaw;
                        if (row.Kind == ResourceFlowKind.Produced) produced += row.AmountRaw;
                    }
                    BigInteger initial = name == "supply" ? Fix.FromInt(atomic ? 28 : 20).Raw : 0;
                    if (initial + produced != (BigInteger)ContinuousStock(game, name).Raw + current.Wip.Stock.AmountOf(resource).Raw + consumed)
                        throw new InvalidOperationException("per-resource continuous WIP/stock/journal balance failed/" + name + "/" + ContinuousDiagnostics(game, prototype));
                }
                if (game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(FleetEntity(game, prototype), ContinuousCapability, out _) ||
                    game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Status != ResourceFlowReadStatus.NotOwned)
                    throw new InvalidOperationException("private continuous execution or material history leaked");
                if (atomic) {
                    var cargo = AtomicCargo(game); var resource = ContinuousResource(game, "supply");
                    Code code = game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(FleetEntity(game, prototype), new Code("shared"), resource, out var stock);
                    if (!code.IsNone || cargo.Retained.AmountOf(resource) + cargo.Committed.AmountOf(resource) + cargo.Unassigned.AmountOf(resource) != stock.Stock.AmountOf(resource))
                        throw new InvalidOperationException("same original physical stock was omitted or counted twice in cargo partition");
                    if (game.Seat(1).View.Feature<LogisticsView>().ReadCargoDisposition(FleetEntity(game, prototype), out var hidden) != Codes.NotFound || hidden != null)
                        throw new InvalidOperationException("foreign cargo partition leaked");
                }
                if (before != game.Hash()) throw new InvalidOperationException("continuous/cargo read changed native execution");
                observed[game] = game.Clock.StepIndex;
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, g => ContinuousDiagnostics(g, prototype), 0);
        }
        private static Pair ContinuousRestore(Check c, GameFactory factory, Pair source, bool atomic)
        {
            var saved = ContinuousPair(factory.Load(source.A.Save()), factory.Load(source.B.Save()), atomic);
            c.Equal(source.A.Hash(), saved.A.Hash(), "original continuous progress rates tools physical cargo and journal restore exactly");
            c.Equal(source.B.Hash(), saved.B.Hash(), "peer original execution and player counters restore exactly"); return saved;
        }
        private static ConfigureOperationCommand ContinuousControl(SimGame game, string prototype, bool enabled, Fix target)
            => new ConfigureOperationCommand { Entity = FleetEntity(game, prototype), Operation = ContinuousCapability, Enabled = enabled, Target = target };
        private static void ContinuousSupplyRun(Check c, bool half)
        {
            var factory = ContinuousTransformationTests.Factory(edit: data => {
                var parts = data.Get("entities")[0].Get("components"); parts.Get("storage").Set("initial_stock", JsonValue.NewObject());
                parts.Get("transformer").Set("automatic_supply", JsonValue.Of(true));
            }, basisEdit: root => { if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30")); });
            SimGame New() => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 814, Seats = { Controller.Human, Controller.Human }, Mods = { "proof.transformation-supply" } });
            var pair = ContinuousPair(New(), New(), false);
            Console.WriteLine("MODULES continuous-supply-" + (half ? "half" : "minute") + "=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(!pair.A.Modules.Contains(TycoonModule.ModuleId), "original finite continuous production requires no business module");
            var limits = new ReplayLimits(12 * 1024 * 1024, 6000); var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            Submit(c, pair, new EnqueueTransformationCommand { Transformer = FleetEntity(pair.A, "flow.factory"), Capability = ContinuousCapability, Transformation = "flow.refine" });
            Reach(pair, g => Physical.Jobs(g).Any(j => j.IsHaul && j.Loaded.IsPositive), "real carrier loads original finite supply for empty processor");
            c.That(ContinuousFacts(pair.A, "flow.factory").Progress.Equals(WorkProgress.Zero) && ContinuousFacts(pair.A, "flow.factory").Flow.Produced.IsEmpty,
                "cargo in transit is neither process input nor earned output");
            var restored = ContinuousRestore(c, factory, pair, false); var cargoLeft = restored.A.RecordReplay(limits); var cargoRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "native sole clock pause during actual continuous supply trip");
            long at = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, g => g.Clock.StepIndex >= at + 4, "other saved peers independently advance actual cargo handling and travel"); pair.Round();
            c.That(pair.A.Clock.StepIndex == at && pair.A.Hash() == frozen, "paused processor receives no borrowed material time or journal updates");
            pair.A.Net.RequestResume();
            Both(pair, restored, g => ContinuousFacts(g, "flow.factory").Progress.WholeTicks > 0,
                "physical unloading permits real quantized feed fuel tool acquisition and first productive prefix");
            ReplayPair(c, factory, cargoLeft, cargoRight, restored, limits, 0, "saved real supply cargo through physical arrival and continuous startup replay");
            restored = ContinuousRestore(c, factory, pair, false); var prefixLeft = restored.A.RecordReplay(limits); var prefixRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => Physical.Jobs(g).Any(j => j.IsHaul && j.Loaded.IsPositive),
                "original automatic refill carrier physically returns to finite source and loads again");
            ReplayPair(c, factory, prefixLeft, prefixRight, restored, limits, 0, "saved first quantized work through actual loaded automatic refill replay");
            restored = ContinuousRestore(c, factory, pair, false); var refillLeft = restored.A.RecordReplay(limits); var refillRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => ContinuousFacts(g, "flow.factory").Flow.Produced.AmountOf(ContinuousResource(g, "flow.product")).IsPositive,
                "actual cumulative work and physical refill earn a whole product without rounding partial work up");
            var active = ContinuousFacts(pair.A, "flow.factory");
            c.That(active.Mode == TransformationMode.Continuous && active.Required == 0 && active.Wip.Execution > 0 && active.Tools.Held.AmountOf(ContinuousResource(pair.A, "supply")) == Fix.One,
                "native indefinite execution retains original tool without inventing a batch or duplicate WIP store");
            ReplayPair(c, factory, refillLeft, refillRight, restored, limits, 0, "saved actual automatic refill cargo to first earned whole output replay");
            restored = ContinuousRestore(c, factory, pair, false); var runLeft = restored.A.RecordReplay(limits); var runRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, ContinuousControl(current.A, "flow.factory", false, Fix.Half));
            Both(pair, restored, g => Result(g, 0, 2)?.Accepted == true, "normal continuous disable gate admitted");
            var held = ContinuousFacts(pair.A, "flow.factory");
            ReplayPair(c, factory, runLeft, runRight, restored, limits, 1, "saved active continuous work to actual disabled process replay");
            restored = ContinuousRestore(c, factory, pair, false); var heldLeft = restored.A.RecordReplay(limits); var heldRight = restored.B.RecordReplay(limits);
            long until = pair.A.Clock.StepIndex + 5; Both(pair, restored, g => g.Clock.StepIndex >= until, "saved disabled processor earns no throughput or elapsed fuel");
            c.That(ContinuousFacts(pair.A, "flow.factory").Progress.Equals(held.Progress) && ContinuousFacts(pair.A, "flow.factory").Operating.ElapsedTicks == held.Operating.ElapsedTicks &&
                ContinuousFacts(pair.A, "flow.factory").Wip.Execution == held.Wip.Execution, "disabled interval preserves actual execution and tool commitment without consuming for waiting");
            foreach (var current in new[] { pair, restored }) Submit(c, current, ContinuousControl(current.A, "flow.factory", true, Fix.FromInt(2)));
            Both(pair, restored, g => Result(g, 0, 3)?.Accepted == true && !ContinuousFacts(g, "flow.factory").Blocker.IsNone,
                "real finite feed and fuel shortages block resumed fast flow rather than manufacturing resources");
            c.Equal(held.Wip.Execution, ContinuousFacts(pair.A, "flow.factory").Wip.Execution, "same cumulative stream survives real shortage and control changes");
            ReplayPair(c, factory, heldLeft, heldRight, restored, limits, 1, "saved disabled continuous stream through native fast resume and actual finite wait replay");
            restored = ContinuousRestore(c, factory, pair, false); var waitLeft = restored.A.RecordReplay(limits); var waitRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, ContinuousControl(current.A, "flow.factory", false, Fix.FromInt(2)));
            Both(pair, restored, g => Result(g, 0, 4)?.Accepted == true, "actual disabled process gate admitted without assuming a new automatic demand");
            HaulResourcesCommand Refill(SimGame game) => new HaulResourcesCommand {
                Source = FleetEntity(game, "source"), Destination = FleetEntity(game, "flow.factory"), Worker = FleetEntity(game, "machine"),
                Resource = ContinuousResource(game, "supply"), Ceiling = Fix.FromInt(2), Priority = 70 };
            bool LoadedRefill(SimGame game) => Physical.Jobs(game).Any(j => j.IsHaul && j.Destination == FleetEntity(game, "flow.factory") && j.Loaded.IsPositive);
            Both(pair, restored, g => LoadedRefill(g) || g.Seat(0).Preview(Refill(g)).IsNone,
                "original acquired refill cargo loads or carrier becomes available for ordinary manual refill");
            bool manual = !LoadedRefill(pair.A);
            if (manual) {
                foreach (var current in new[] { pair, restored }) Submit(c, current, Refill(current.A));
                Both(pair, restored, g => Result(g, 0, 5)?.Accepted == true, "actual manual refill is canonically admitted before any save or replay cut");
            }
            Both(pair, restored, g => Physical.Jobs(g).Any(j => j.IsHaul && j.Loaded.IsPositive),
                "ordinary player refill physically loads finite source while process stays disabled");
            var beforeStop = ContinuousFacts(pair.A, "flow.factory");
            ReplayPair(c, factory, waitLeft, waitRight, restored, limits, manual ? 2 : 1, "saved shortage through disabled gate and actual loaded refill replay");
            restored = ContinuousRestore(c, factory, pair, false); var stopLeft = restored.A.RecordReplay(limits); var stopRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new CancelTransformationCommand { Transformer = FleetEntity(current.A, "flow.factory"), Capability = ContinuousCapability, Index = 0 });
            Both(pair, restored, g => ContinuousFacts(g, "flow.factory").QueueCount == 0 && !Physical.Jobs(g).Any(j => j.IsHaul && (j.State == JobState.Open || j.State == JobState.Assigned || j.State == JobState.Paused)),
                "normal stop releases tools and native acquired deliveries finish without refunding consumed stream input");
            var stopped = ContinuousFacts(pair.A, "flow.factory");
            c.That(stopped.Tools.Sources.Count == 0 && stopped.Wip.Execution == 0 && stopped.Flow.Produced.IsEmpty && active.Wip.Execution == held.Wip.Execution &&
                ContinuousStock(pair.A, "flow.product") == beforeStop.Flow.Produced.AmountOf(ContinuousResource(pair.A, "flow.product")),
                "stopped execution retires while actual earned products and detached earlier facts remain");
            ReplayPair(c, factory, stopLeft, stopRight, restored, limits, 1, "saved real refill cargo through continuous stop tool release and completed physical deliveries replay");
            ReplayPair(c, factory, left, right, pair, limits, manual ? 6 : 5, "full finite continuous supply control shortage and disposition player stream replay");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && pair.A.Hash() == restored.B.Hash(), "four actually advanced stream/cargo worlds converge");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "settled actual stopped continuous material checkpoint");
        }
        private static void AtomicCargoRun(Check c, bool half)
        {
            var factory = CargoDispositionObservationTests.Factory("none", half);
            SimGame New() => ConstructionPlanTests.NewPoints(factory);
            var pair = ContinuousPair(New(), New(), true);
            Console.WriteLine("MODULES atomic-cargo-" + (half ? "half" : "minute") + "=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(!pair.A.Modules.Contains(TycoonModule.ModuleId), "same-store production and cargo contracts require no money");
            var limits = new ReplayLimits(8 * 1024 * 1024, 6000); var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            var resource = ContinuousResource(pair.A, "supply");
            int policy = Submit(c, pair, new ConfigureStockPolicyCommand { Store = FleetEntity(pair.A, "atomic.factory"), Resource = resource,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(2), Fix.FromInt(2), Fix.FromInt(12), 30, LogisticsReserveAccess.Protected)) });
            Reach(pair, g => Result(g, 0, policy)?.Accepted == true, "original protected stock band admitted");
            var initial = AtomicCargo(pair.A);
            c.That(initial.Retained.AmountOf(resource) == Fix.FromInt(2) && initial.Committed.IsEmpty && initial.Unassigned.AmountOf(resource) == Fix.FromInt(10), "initial exact twelve-unit stock partition has no invented promise");
            Submit(c, pair, new EnqueueTransformationCommand { Transformer = FleetEntity(pair.A, "atomic.factory"), Capability = ContinuousCapability, Transformation = "atomic.equal" });
            Submit(c, pair, ContinuousControl(pair.A, "atomic.factory", true, Fix.One));
            Reach(pair, g => ContinuousFacts(g, "atomic.factory").Flow.Produced.AmountOf(resource) >= Fix.FromInt(4), "actual same-endpoint input/output conversion earns real timed output at full capacity");
            var active = AtomicCargo(pair.A); var execution = ContinuousFacts(pair.A, "atomic.factory");
            c.That(active.Retained.AmountOf(resource) == Fix.FromInt(2) && active.Committed.AmountOf(resource) == Fix.One && active.Unassigned.AmountOf(resource) == Fix.FromInt(9) &&
                execution.Tools.Held.AmountOf(resource) == Fix.One && execution.Wip.Stock.IsEmpty,
                "same actual stock is partitioned into floor tool claim and free stock rather than copied into WIP");
            var restored = ContinuousRestore(c, factory, pair, true); var activeLeft = restored.A.RecordReplay(limits); var activeRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "native pause during actual same-store stream");
            long at = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash(); var paused = ContinuousFacts(pair.A, "atomic.factory");
            Reach(restored, g => g.Clock.StepIndex >= at + 4, "separate saved peers continue actual same-store conversion without borrowing state"); pair.Round();
            c.That(pair.A.Clock.StepIndex == at && pair.A.Hash() == frozen && ContinuousFacts(restored.A, "atomic.factory").Operating.ElapsedTicks > paused.Operating.ElapsedTicks,
                "paused cargo partition and stream receive no other session's earned work");
            pair.A.Net.RequestResume(); Both(pair, restored, g => g.Clock.StepIndex >= at + 4, "native same-store stream converges after real clock catch-up");
            ReplayPair(c, factory, activeLeft, activeRight, restored, limits, 0, "saved shared-stock stream through native pause isolated conversion and catch-up replay");
            restored = ContinuousRestore(c, factory, pair, true); var stopLeft = restored.A.RecordReplay(limits); var stopRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new CancelTransformationCommand { Transformer = FleetEntity(current.A, "atomic.factory"), Capability = ContinuousCapability, Index = 0 });
            Both(pair, restored, g => ContinuousFacts(g, "atomic.factory").QueueCount == 0, "normal stop retires original same-stock execution and releases actual tool receipt");
            var stopped = AtomicCargo(pair.A);
            c.That(stopped.Retained.AmountOf(resource) == Fix.FromInt(2) && stopped.Committed.IsEmpty && stopped.Unassigned.AmountOf(resource) >= Fix.FromInt(9) &&
                ContinuousFacts(pair.A, "atomic.factory").Tools.Sources.Count == 0 && active.Committed.AmountOf(resource) == Fix.One && initial.Committed.IsEmpty,
                "tool release changes only original commitment partition with no lost stock or mutated retained views");
            ReplayPair(c, factory, stopLeft, stopRight, restored, limits, 1, "saved shared full inventory through normal stop and private cargo tool release replay");
            ReplayPair(c, factory, left, right, pair, limits, 4, "full protected shared stock and same-endpoint conversion player stream replay");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && pair.A.Hash() == restored.B.Hash(), "four actually advanced same-stock worlds converge");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "settled original same-stock cargo partition checkpoint");
        }
    }
}
