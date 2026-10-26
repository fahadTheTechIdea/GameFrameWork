using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using NetProof = Beep.SimEngine.Tests.NetworkedModDurabilityTests;

namespace Beep.SimEngine.Tests
{
    public static partial class PurchasingHaulTests
    {
        private const string CommerceCase = "Optional resource commerce: native purchase cancellation physical delivery scheduled sale and saved accounting";
        private const string NetworkCommerceCase = "Networked resource commerce: shuffled native purchases scheduled sales and saved physical authority";
        // Public player inputs/views only. Existing engine owners execute all supply, handling,
        // sale scheduling, resource disposition, accounts and persistence.
        private static void ResourceCommerce(Check c, bool networked)
        {
            var factory = Factory(commerce: true);
            var request = new NewGameRequest { Scenario = "purchase-haul", Seed = 59, Seats = { Controller.Human, Controller.Human } };
            var game = factory.NewGame(request); var isolated = factory.NewGame(request); ulong untouched = isolated.Hash(); SimGame twin = null;
            var peer = networked ? factory.NewGame(request) : null; SimGame savedPeer = null;
            NetProof.Pair pair = null, savedPair = null;
            EntityId Id(string prototype) => game.Seat(0).Snapshot.Entities.Single(row => row.Def == prototype).Id;
            EntityId source = Id("source"), destination = Id("destination"), carrier = Id("machine");
            if (!game.Seat(0).View.TryFindResource("supply", out var supply)) throw new InvalidOperationException("missing authored supply");
            Fix Material(SimGame session, EntityId entity) {
                var rows = new List<Facet>(); session.Seat(0).View.Inspect(entity, rows);
                return rows.Where(row => row.Key == Storage.FacetStock && row.Subject == "supply").Aggregate(Fix.Zero, (sum, row) => sum + row.Value);
            }
            Fix At(SimGame session) => ((PointSnapshot)session.Seat(0).Snapshot.Entities.Single(row => row.Id == carrier).Spatial).Position;
            ResourceFlowPage Flows(SimGame session) => session.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0);
            BusinessSalesSnapshot Sales(SimGame session) {
                Code result = session.Seat(0).View.Feature<BusinessView>().ReadSales(new PlayerId(0), out var value);
                if (!result.IsNone) throw new InvalidOperationException("actual sale projection: " + result); return value;
            }
            Fix Supplied(SimGame session) => Flows(session).Entries.Where(row => row.Item.Kind == ResourceFlowKind.AuthoredCreation)
                .Aggregate(Fix.Zero, (sum, row) => sum + row.Item.Amount);
            var policies = new List<ResourceSaleView>();
            c.Equal(Code.None, game.Seat(0).View.Feature<BusinessView>().CopySalesPolicies(destination, policies), "actual engine sale policy is visible on the authored endpoint");
            c.That(policies.Count == 1 && policies[0].Id == "purchase.delivered-sale" && policies[0].Prices.AmountOf(supply) == Fix.FromInt(5), "file-authored sale policy uses original five-unit valuation");
            int inputs = 0; var limits = new ReplayLimits(8 * 1024 * 1024, 2000);
            void AssertAccounting(SimGame session) {
                var page = Flows(session); c.Equal(ResourceFlowReadStatus.Ready, page.Status, "finite accounting does not use expired history");
                var totals = page.Totals(supply); Fix stock = Material(session, destination) + Material(session, carrier);
                c.Equal(totals.CreatedRaw, new BigInteger(stock.Raw) + totals.SoldRaw, "authored native supply equals actual destination/cargo plus sold material each update");
                c.That(totals.ConsumedRaw.IsZero && totals.DestroyedRaw.IsZero && totals.LostRaw.IsZero && totals.StockPurchasedRaw.IsZero, "native unlimited supply is explicitly creation, never fake finite stock purchase or destruction");
                c.Equal(new BigInteger(Fix.FromInt(100).Raw) - totals.CreatedRaw * 5 + totals.SoldRaw * 5,
                    new BigInteger(Account(session).Balance.Raw), "native account charges only physically loaded supply and credits only committed sold material");
                c.Equal(totals.SoldRaw * 5, new BigInteger(Sales(session).Revenue.Raw), "sale history and resource disposition describe the same committed quantities");
                c.Equal(0L, Account(session).GenerationSettlements, "sale does not also grant primary production income");
            }
            string Describe(SimGame session) => "step=" + session.Clock.StepIndex + ";position=" + At(session) + ";cargo=" + Material(session, carrier) + ";stock=" + Material(session, destination) + ";revenue=" + Sales(session).Revenue;
            void Check() {
                AssertAccounting(game);
                if (peer != null) { AssertAccounting(peer); c.Equal(game.Hash(), peer.Hash(), "shuffled peers retain exact native purchase and sale state at the same epoch"); }
                if (twin != null) { AssertAccounting(twin); c.Equal(game.Hash(), twin.Hash(), "saved original supplier book cargo account sale state and stream remain identical"); }
                if (savedPeer != null) { AssertAccounting(savedPeer); c.Equal(twin.Hash(), savedPeer.Hash(), "independently reattached saved peer retains the same actual authority"); }
            }
            void Advance(NetProof.Pair transport) {
                if (!transport.SameEpoch) throw new InvalidOperationException("commerce transport must start at a common committed epoch");
                long next = transport.A.Clock.StepIndex + 1;
                for (int attempt = 0; attempt < 16 && (!transport.SameEpoch || transport.A.Clock.StepIndex < next); attempt++) transport.Round(next);
                if (!transport.SameEpoch || transport.A.Clock.StepIndex != next) throw new InvalidOperationException("bounded commerce transport did not finish one actual native step/" + Describe(transport.A));
            }
            void Step() {
                if (networked) { Advance(pair); if (savedPair != null) Advance(savedPair); }
                else { c.Equal(1, game.Update(), "ordinary engine update"); if (game.Fault != null) throw new InvalidOperationException("commerce fault: " + game.Fault);
                    if (twin != null) c.Equal(1, twin.Update(), "saved twin engine update"); }
                Check();
            }
            void Input(Command command) {
                if (!networked) {
                    c.That(TestKit.Order(game, 0, command).Accepted, "ordinary player command accepted: " + command.GetType().Name);
                    if (twin != null) c.That(TestKit.Order(twin, 0, command).Accepted, "saved twin accepts same player command"); inputs++; Check(); return;
                }
                c.Equal(Code.None, game.Seat(0).Preview(command), "actual local owner admits the same commerce input before network delivery");
                var foreign = new SetWorkerHeldCommand { Worker = carrier, Held = true };
                c.Equal(Codes.NotFound, peer.Seat(1).Preview(foreign), "foreign worker intent has the same typed privacy refusal before transport");
                int sequence = game.Seat(0).Submit(command), rejected = peer.Seat(1).Submit(foreign);
                if (savedPair != null) { c.Equal(sequence, twin.Seat(0).Submit(command), "saved owner keeps the original input sequence");
                    c.Equal(rejected, savedPeer.Seat(1).Submit(new SetWorkerHeldCommand { Worker = carrier, Held = true }), "saved foreign peer keeps the original refusal sequence"); }
                inputs += 2; Step();
                foreach (var session in new[] { game, peer, twin, savedPeer }.Where(value => value != null)) {
                    c.That(NetProof.Result(session, 0, sequence)?.Accepted == true, "every active and saved peer applies the original accepted owner intent once");
                    c.Equal(Codes.NotFound, NetProof.Result(session, 1, rejected)?.Refusal ?? Code.None, "shuffled foreign intent refuses without changing worker or account authority");
                }
            }
            void Wait(Func<bool> done, string stage) {
                for (int i = 0; i < 240 && !done(); i++) Step();
                if (!done()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + ";position=" + At(game) + ";cargo=" + Material(game, carrier) + ";delivered=" + Material(game, destination) + ";sale=" + Sales(game).Revenue + ";job=" + Read(game)?.State + "/" + Read(game)?.Blocker);
            }
            void Save() {
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual completed checkpoint preserves original supplier receipts and accounts");
                if (networked) { savedPeer = factory.Load(peer.Save()); savedPair = new NetProof.Pair(twin, savedPeer, AssertAccounting, Describe, 0); }
                Check();
            }
            ConfigureStockPolicyCommand Demand(bool enabled) => new ConfigureStockPolicyCommand {
                Store = destination, Resource = supply, Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(enabled,
                    Fix.FromInt(8), Fix.FromInt(8), Fix.FromInt(8), 100, LogisticsReserveAccess.Protected)) };
            if (networked) pair = new NetProof.Pair(game, peer, AssertAccounting, Describe, 0);
            using var recording = game.RecordReplay(limits);
            using var peerRecording = networked ? peer.RecordReplay(limits) : null;
            Check(); Input(Demand(true));
            Wait(() => Read(game).HasValue && At(game) > Fix.Zero && At(game) < Fix.FromInt(10), "original promised native supply begins physical travel");
            c.Equal(Fix.Zero, Supplied(game), "reservation and travel create no actual goods or purchase charge"); Save();
            Input(new SetWorkerHeldCommand { Worker = carrier, Held = true }); Input(Demand(false));
            c.That(!Read(game).HasValue && Material(game, carrier).IsZero, "holding the unloaded worker cancels native supply and receiving promises without phantom cargo"); Save();
            Input(new SetWorkerHeldCommand { Worker = carrier, Held = false }); Input(Demand(true));
            Wait(() => Material(game, carrier) == Fix.FromInt(2), "actual partial native supply fulfillment");
            c.Equal(Fix.FromInt(90), Account(game).Balance, "two actually loaded units cost ten once"); Save();
            Wait(() => Material(game, destination) == Fix.FromInt(2), "actual progressive unloading");
            c.Equal(Fix.FromInt(6), Material(game, carrier), "partial delivered amount leaves the exact real cargo remainder"); Save();
            Wait(() => Material(game, destination) == Fix.FromInt(8) && !Read(game).HasValue, "whole paid supply physically reaches sale endpoint");
            Input(Demand(false)); c.Equal(Fix.FromInt(60), Account(game).Balance, "delivered eight cost forty and sale has not run early");
            c.Equal(Fix.Zero, Sales(game).Revenue, "physical arrival does not bypass the authored sale schedule"); Save();
            Wait(() => Sales(game).Revenue == Fix.FromInt(40), "authored native sale boundary");
            c.Equal(121L, game.Clock.StepIndex, "sale executes in the authored interval starting at minute120, not on delivery");
            c.Equal(Fix.Zero, Material(game, destination) + Material(game, carrier), "sold stock leaves actual physical inventory");
            c.Equal(Fix.FromInt(100), Account(game).Balance, "the sole account has exact committed purchase debit and sale credit");
            c.Equal(Fix.FromInt(40), Sales(game).PreviousPeriodRevenue, "same native sale/report boundary closes the actual earned revenue");
            c.Equal(Fix.Zero, Sales(game).CurrentPeriodRevenue, "closed reporting window is not double counted"); Save();
            for (int i = 0; i < 8; i++) Step();
            c.Equal(Fix.FromInt(8), Supplied(game), "cancelled/retried/restored claims create only eight actually loaded units");
            c.That(Flows(game).Entries.Where(row => row.Item.Kind == ResourceFlowKind.AuthoredCreation).All(row => row.Item.Source.Endpoint == ResourceEndpointId.ForEntity(source)), "creation facts retain the actual native supplier identity, never a fabricated source balance");
            c.Equal(Fix.FromInt(40), Sales(game).Revenue, "idle saved continuation never resells or refunds material");
            var expenses = new List<BusinessExpenseSnapshot>(); c.Equal(Code.None, game.Seat(0).View.Feature<BusinessView>().CopySpending(new PlayerId(0), expenses), "native purchase expense history is public to its company");
            c.Equal(new BigInteger(Fix.FromInt(40).Raw), expenses.Where(row => row.Reason == BusinessModeRules.LedgerPurchase).Aggregate(BigInteger.Zero, (sum, row) => sum + row.AmountRaw), "all actual progressive purchases debit exactly forty once");
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Status, "purchase and sale facts remain company-private");
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<BusinessView>().ReadSales(new PlayerId(0), out _), "sale revenue remains company-private");
            c.Equal(inputs, recording.CommandCount, "recorder includes every actual player demand hold release and cancellation input");
            c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "recorder reaches every committed Update through final sold continuation");
            byte[] replayBytes = recording.Finish();
            if (networked) {
                c.Equal(inputs, peerRecording.CommandCount, "other peer records every accepted and refused canonical input");
                c.Equal(peer.Clock.StepIndex - peerRecording.InitialStep, (long)peerRecording.CheckpointCount, "other peer records every committed physical and sale epoch");
                c.That(replayBytes.SequenceEqual(peerRecording.Finish()), "oppositely owned shuffled peers produce the same complete canonical replay bytes");
                c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && savedPair.Stalls > 0 && savedPair.ReversedBatches > 0,
                    "both original and loaded native sessions actually stall for delayed frames and receive reversed packet batches");
            }
            var replay = new ReplayVerifier().Verify(factory, replayBytes, limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "complete physical purchase and scheduled sale replay reaches final live identity");
            c.Equal(untouched, isolated.Hash(), "another session retains its original native supplier and account ownership");
            c.Equal(1, isolated.Update(), "isolated session independently advances its own original engine");
            c.Equal(Fix.Zero, Supplied(isolated), "independent advance does not acquire another session's supply");
            Console.WriteLine("native resource commerce: inputs=" + inputs + ";initial-step=" + recording.InitialStep + ";final-step=" + game.Clock.StepIndex + ";checkpoints=" + recording.CheckpointCount + ";expected-hash=" + game.Hash().ToString("X16") + ";actual-hash=" + replay.ActualHash.ToString("X16")+
                ";networked=" + networked + ";stalls=" + (pair?.Stalls ?? 0) + ";reversed=" + (pair?.ReversedBatches ?? 0));
            if (!networked) Survival(c);
        }
    }
}
