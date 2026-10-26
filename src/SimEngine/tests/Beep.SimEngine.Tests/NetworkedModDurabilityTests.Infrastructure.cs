using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Physical = Beep.SimEngine.Tests.PermanentRemovalTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string OwnedTileNetworkCase = "Networked infrastructure: private tile pools preserve physical split cancellation and supplied reconnect";
        internal const string OwnedPointNetworkCase = "Networked infrastructure contracts: private half-minute point pools earn construction split and reconnect";
        internal const string WorldTileNetworkCase = "Networked world infrastructure: public half-minute tile pools retain physical redistribution and saved work";
        internal const string WorldPointNetworkCase = "Networked world infrastructure contracts: public point pools retain physical construction redistribution and reconnect";
        private static string InfrastructureNetwork(bool point) => point ? "point.network" : "binding.main";
        private static InfrastructureView InfrastructureFacts(SimGame game) => game.Seat(0).View.Feature<InfrastructureView>();
        private static List<NetworkPoolView> InfrastructurePools(SimGame game, bool point) => TopologyErasePostconditionTests.Pools(game, point);
        private static ResourceId InfrastructureResource(SimGame game, bool point)
        {
            if (!game.Seat(0).View.TryFindResource(point ? "supply" : "steel", out var id)) throw new InvalidOperationException("authored infrastructure resource missing");
            return id;
        }
        private static Fix InfrastructurePoolStock(SimGame game, bool point)
            => InfrastructurePools(game, point).SelectMany(p => p.Stock).Where(s => s.Resource == InfrastructureResource(game, point)).Aggregate(Fix.Zero, (a, b) => a + b.Amount);
        private static Fix InfrastructureStock(SimGame game, bool point)
        {
            Fix total = InfrastructurePoolStock(game, point);
            foreach (var entity in game.Seat(0).Snapshot.Entities) {
                var facts = new List<Facet>(); game.Seat(0).View.Inspect(entity.Id, facts);
                foreach (var fact in facts.Where(f => f.Key == Storage.FacetStock && f.Subject == (point ? "supply" : "steel"))) total += fact.Value;
            }
            return total;
        }
        private static Fix InfrastructureMiddle(SimGame game, bool point)
            => point ? SpatialCorridorExecutionTests.Segment(game, true, 1) : Fix.FromInt(PhysicalLineRemovalTests.Tile(game, 11));
        private static PostOrderCommand InfrastructureCut(SimGame game, bool point)
            => new PostOrderCommand { Plan = point ? "corridor.remove" : "removal.work", Target = FleetEntity(game, point ? "corridor.removal-site" : "removal.site"), Priority = 100 };
        private static bool InfrastructurePartial(SimGame game, int order)
            => LoadNodes(game, order).Any(n => n.Progress.WholeTicks > 0 && n.Progress.WholeTicks < n.RequiredTicks);
        private static string InfrastructureDiagnostics(SimGame game, bool point) => "at=" + game.Clock.StepIndex + "/middle=" + InfrastructureMiddle(game, point) +
            "/stock=" + InfrastructureStock(game, point) + "/pools=" + string.Join(";", InfrastructurePools(game, point).Select(p => p.Endpoint + "/" + p.MemberCount + "/" + p.Version)) +
            "/jobs=" + string.Join(";", Physical.Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.Blocker));
        private static Pair InfrastructurePair(SimGame a, SimGame b, bool point, bool world)
        {
            void Observe(SimGame game)
            {
                ulong before = game.Hash(); BigInteger consumed = 0;
                foreach (var owner in new[] { new PlayerId(0), PlayerId.None })
                    foreach (var row in game.Observer.Feature<ResourceFlowView>().ReadSummary(owner).Aggregates) {
                        if (row.Kind == ResourceFlowKind.AuthoredCreation || row.Kind == ResourceFlowKind.StockPurchase || row.Kind == ResourceFlowKind.Produced ||
                            row.Kind == ResourceFlowKind.Lost || row.Kind == ResourceFlowKind.Destroyed)
                            throw new InvalidOperationException("physical infrastructure granted or discarded original finite material");
                        if (row.Kind == ResourceFlowKind.Consumed && row.Resource == InfrastructureResource(game, point)) consumed += row.AmountRaw;
                    }
                if (new BigInteger(InfrastructureStock(game, point).Raw) + consumed != new BigInteger(Fix.FromInt(point ? 20 : 80).Raw))
                    throw new InvalidOperationException("infrastructure material balance failed/" + InfrastructureDiagnostics(game, point));
                if (InfrastructurePools(game, point).Any(p => p.Company != (world ? PlayerId.None : new PlayerId(0))))
                    throw new InvalidOperationException("actual topology owner silently substituted");
                var privateOrders = new List<OrderView>(); game.Seat(1).View.Feature<WorkView>().CopyOrders(privateOrders);
                if (privateOrders.Count != 0) throw new InvalidOperationException("foreign physical topology work leaked");
                var foreignPools = new List<NetworkPoolView>();
                if (!game.Seat(1).View.Feature<InfrastructureView>().CopyPools(InfrastructureNetwork(point), foreignPools).IsNone ||
                    foreignPools.Count != (world ? InfrastructurePools(game, point).Count : 0)) throw new InvalidOperationException("private/public pool visibility violated");
                var flows = new List<NetworkFlowSnapshot>();
                if (!InfrastructureFacts(game).CopyFlow(InfrastructureNetwork(point), flows).IsNone || flows.Any(f => f.LastTick > game.Clock.StepIndex))
                    throw new InvalidOperationException("flow reads borrowed unearned game time");
                if (game.Hash() != before) throw new InvalidOperationException("public topology and material observations changed authority");
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, g => InfrastructureDiagnostics(g, point), 0);
        }
        private static Pair InfrastructureRestore(Check c, GameFactory factory, Pair source, bool point, bool world)
        {
            var saved = InfrastructurePair(factory.Load(source.A.Save()), factory.Load(source.B.Save()), point, world);
            c.Equal(source.A.Hash(), saved.A.Hash(), "native geometry pools stock flow cursors and physical claims restore exactly");
            c.Equal(source.B.Hash(), saved.B.Hash(), "peer saved producer identity and actual work state remain exact"); return saved;
        }
        private static void InfrastructureRun(Check c, bool point, bool world, bool half)
        {
            var factory = world ? WorldNetworkStockTests.Factory(point, half) : TopologyErasePostconditionTests.Factory(point, half);
            SimGame New() {
                var request = new NewGameRequest { Scenario = point ? "points" : "colony", Seed = point ? 719UL : 1462UL, Seats = { Controller.Human, Controller.Human } };
                if (point && !world) request.Mods.Add(ResourceFlowsModule.ModuleId);
                return factory.NewGame(request);
            }
            var pair = InfrastructurePair(New(), New(), point, world);
            Console.WriteLine("MODULES infrastructure-" + (world ? "world-" : "owned-") + (point ? "point" : "tile") + "=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(pair.A.Modules.Contains(InfrastructureModule.ModuleId) && !pair.A.Modules.Contains(TycoonModule.ModuleId), "same shared physical topology owner requires no business");
            var limits = new ReplayLimits(12 * 1024 * 1024, 6000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            int issued = 0;
            if (point) {
                c.Equal(0, InfrastructurePools(pair.A, true).Count, "unbuilt point geometry creates no imaginary graph stock");
                int input = Submit(c, pair, new SubmitPlanCommand { Intent = SpatialCorridorExecutionTests.Intent(true, false) }); issued++;
                Reach(pair, g => Result(g, 0, input)?.Accepted == true && InfrastructurePartial(g, Physical.Plan(g, Result(g, 0, input).Value.Arg0).Order),
                    "actual original machine supplies and earns first four-minute segment prefix");
                long plan = Result(pair.A, 0, input).Value.Arg0;
                c.That(SpatialCorridorExecutionTests.Segment(pair.A, true, 0).IsZero && SpatialCorridorExecutionTests.Segment(pair.A, true, 2).IsZero,
                    "partial work cannot stamp present or future segment");
                var buildSaved = InfrastructureRestore(c, factory, pair, point, world);
                var buildLeft = buildSaved.A.RecordReplay(limits); var buildRight = buildSaved.B.RecordReplay(limits);
                Both(pair, buildSaved, g => Physical.Plan(g, plan).State == PlanState.Done && InfrastructurePoolStock(g, true) == Fix.FromInt(14),
                    "full three authored segments consume six supplies and actual elapsed flow fills original graph");
                ReplayPair(c, factory, buildLeft, buildRight, buildSaved, limits, 0, "saved physical point construction through connected finite graph replay");
                c.Equal(Fix.FromInt(14), InfrastructureStock(pair.A, true), "three real material bills consume exactly six opening supplies");
            } else Reach(pair, g => g.Clock.StepIndex >= (half ? 8 : 4), "authored startup geometry earns real elapsed native flow");
            var oldPool = InfrastructurePools(pair.A, point).Single(); Fix pooled = InfrastructurePoolStock(pair.A, point), total = InfrastructureStock(pair.A, point);
            c.That(oldPool.MemberCount == 3 && pooled.IsPositive, "actual connected three-vertex topology contains finite transferred stock");
            c.That(InfrastructureFacts(pair.A).ReadConnection(FleetEntity(pair.A, point ? "source" : "binding.source"), InfrastructureNetwork(point), out var from).IsNone &&
                InfrastructureFacts(pair.A).ReadConnection(FleetEntity(pair.A, point ? "destination" : "binding.watcher"), InfrastructureNetwork(point), out var to).IsNone && from.Endpoint == to.Endpoint,
                "native shared endpoint comes from actual spatial connectivity");
            int denied = Submit(c, pair, InfrastructureCut(pair.A, point), Codes.NotOwned, 1);
            int cut = Submit(c, pair, InfrastructureCut(pair.A, point)); issued++;
            Reach(pair, g => Result(g, 0, cut)?.Accepted == true && InfrastructurePartial(g, (int)Result(g, 0, cut).Value.Arg0), "original machine travels then earns partial topology removal");
            int cutOrder = (int)Result(pair.A, 0, cut).Value.Arg0;
            c.That(Result(pair.A, 1, denied)?.Refusal == Codes.NotOwned && InfrastructureMiddle(pair.A, point).IsPositive, "foreign refusal and own partial work leave physical bridge intact");
            var partial = LoadNodes(pair.A, cutOrder).Single();
            c.Equal(point ? (half ? 8L : 4L) : (half ? 12L : 6L), partial.RequiredTicks, "original four/six game minutes convert through sole actual quantum");
            var restored = InfrastructureRestore(c, factory, pair, point, world);
            var cancelLeft = restored.A.RecordReplay(limits); var cancelRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new CancelOrderCommand { Order = cutOrder }); issued++;
            long cancelAt = pair.A.Clock.StepIndex + 4;
            Both(pair, restored, g => g.Clock.StepIndex >= cancelAt, "saved earned removal cancellation settles without erasing geometry");
            c.That(InfrastructureMiddle(pair.A, point).IsPositive && InfrastructurePools(pair.A, point).Count == 1 && InfrastructureStock(pair.A, point) == total,
                "cancelled partial cut grants no salvage destroys no stock and cannot split graph");
            ReplayPair(c, factory, cancelLeft, cancelRight, restored, limits, 1, "saved physical cut prefix through ordinary cancellation replay");
            restored = InfrastructureRestore(c, factory, pair, point, world);
            var freshLeft = restored.A.RecordReplay(limits); var freshRight = restored.B.RecordReplay(limits);
            int fresh = 0;
            foreach (var current in new[] { pair, restored }) { int id = Submit(c, current, InfrastructureCut(current.A, point)); if (fresh == 0) fresh = id; else c.Equal(fresh, id, "saved next player input sequence exact"); } issued++;
            Both(pair, restored, g => Result(g, 0, fresh)?.Accepted == true && InfrastructurePartial(g, (int)Result(g, 0, fresh).Value.Arg0), "fresh cut earns original full work rather than cancelled credit");
            int freshOrder = (int)Result(pair.A, 0, fresh).Value.Arg0;
            c.That(freshOrder != cutOrder && InfrastructureMiddle(pair.A, point).IsPositive, "fresh execution identity retains unfinished actual bridge");
            ReplayPair(c, factory, freshLeft, freshRight, restored, limits, 1, "saved cancelled cut to fresh physical work replay");
            restored = InfrastructureRestore(c, factory, pair, point, world);
            var eraseLeft = restored.A.RecordReplay(limits); var eraseRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "native clock pause during fresh physical cut");
            long at = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, g => InfrastructureMiddle(g, point).IsZero, "separate saved pair earns actual erase and split"); pair.Round();
            c.That(pair.A.Clock.StepIndex == at && pair.A.Hash() == frozen && InfrastructureMiddle(pair.A, point).IsPositive,
                "paused native world borrows no earned cut topology change or redistribution");
            pair.A.Net.RequestResume(); Both(pair, restored, g => InfrastructureMiddle(g, point).IsZero, "native resumed physical erase commits complete split");
            c.That(InfrastructurePools(pair.A, point).Count == 2 && InfrastructureStock(pair.A, point) == total && InfrastructurePoolStock(pair.A, point) == pooled && oldPool.MemberCount == 3,
                "physical graph split preserves exact pooled material and detached prior graph facts");
            if (world) {
                var journal = pair.A.Observer.Feature<ResourceFlowView>().Read(PlayerId.None, 0);
                c.That(journal.Entries.Any(e => e.Item.Owner.Scope == "sim.infrastructure.topology" && e.Item.IsTransfer && e.Item.Company == PlayerId.None),
                    "actual world-owned redistribution emits original physical transfer history");
                c.Equal(ResourceFlowReadStatus.NotOwned, pair.A.Seat(0).View.Feature<ResourceFlowView>().Read(PlayerId.None, 0).Status, "public topology does not leak observer-only world history");
            }
            ReplayPair(c, factory, eraseLeft, eraseRight, restored, limits, 0, "saved earned cut through native clock isolation erase and redistribution replay");
            restored = InfrastructureRestore(c, factory, pair, point, world);
            var splitLeft = restored.A.RecordReplay(limits); var splitRight = restored.B.RecordReplay(limits);
            var args = point ? Json.Parse("{\"space\":\"field.points\",\"path\":[{\"space\":\"field.points\",\"position\":5}]}") :
                Json.Parse("{\"space\":\"recovery.tiles\",\"path\":[{\"space\":\"recovery.tiles\",\"x\":11,\"y\":28}]}");
            int rebuild = 0;
            foreach (var current in new[] { pair, restored }) { int id = Submit(c, current, new SubmitPlanCommand { Intent = new PlanIntent(point ? "line:corridor.point-line" : "line:network.pipe", args, "reconnect", 60, false) }); if (rebuild == 0) rebuild = id; } issued++;
            Both(pair, restored, g => Result(g, 0, rebuild)?.Accepted == true, "ordinary reconnect allocates physical cursor no instant bridge");
            long reconnect = Result(pair.A, 0, rebuild).Value.Arg0;
            c.That(InfrastructureMiddle(pair.A, point).IsZero && InfrastructurePools(pair.A, point).Count == 2, "rebuild intent does not prepublish topology");
            EntityId cursor = FleetEntity(pair.A, point ? "corridor.cursor" : "sim.line-work.network.pipe");
            HaulResourcesCommand Supply(SimGame game) => new HaulResourcesCommand {
                Source = FleetEntity(game, point ? "source" : "depot"), SourcePort = point ? new Code("graph") : Code.None,
                Destination = cursor, Worker = FleetEntity(game, point ? "machine" : "truck"),
                Resource = InfrastructureResource(game, point), Ceiling = Fix.FromInt(point ? 2 : 1), Priority = 100 };
            bool LoadedSupply(SimGame game) => Physical.Jobs(game).Any(j => j.IsHaul && j.Destination == cursor && j.Loaded.IsPositive) && InfrastructureMiddle(game, point).IsZero;
            Both(pair, restored, g => LoadedSupply(g) || g.Seat(0).Preview(Supply(g)).IsNone,
                "native automatic supply earns real cargo or original carrier becomes available for manual hauling");
            bool automatic = LoadedSupply(pair.A);
            if (automatic) c.That(point && Physical.Jobs(pair.A).Any(j => j.IsHaul && j.Destination == cursor && j.Loaded.IsPositive && !j.Purchase),
                "original automatic_supply graph physically loaded finite material without an extra player haul");
            else { foreach (var current in new[] { pair, restored }) Submit(c, current, Supply(current.A)); issued++; }
            Both(pair, restored, LoadedSupply,
                "real carrier loads reconnect supply from finite endpoint not a grant");
            ReplayPair(c, factory, splitLeft, splitRight, restored, limits, automatic ? 1 : 2, "saved split topology to ordinary reconnect intent and actual loaded cargo replay");
            restored = InfrastructureRestore(c, factory, pair, point, world);
            var cargoLeft = restored.A.RecordReplay(limits); var cargoRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => Physical.Plan(g, reconnect).State == PlanState.Done, "same original haul unload and full staffed segment reconnect actual topology");
            c.That(InfrastructurePools(pair.A, point).Count == 1 && InfrastructureMiddle(pair.A, point).IsPositive && InfrastructureStock(pair.A, point) == total - Fix.FromInt(point ? 2 : 1),
                "earned merge consumes only authored material bill and never duplicates pooled stock");
            ReplayPair(c, factory, cargoLeft, cargoRight, restored, limits, 0, "saved reconnect cargo to earned merged network replay");
            ReplayPair(c, factory, left, right, pair, limits, issued + 1, "full own and refused foreign physical topology input stream replay");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && pair.A.Hash() == restored.B.Hash(), "four advanced topology worlds converge through actual stalled shuffled transport");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "merged native geometry flow claims and finite stock checkpoint");
        }
    }
}
