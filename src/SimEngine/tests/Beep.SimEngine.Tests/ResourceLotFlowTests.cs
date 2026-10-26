using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Inputs and engine contract assertions only. No production, attribution, filter or rate implementation here.
    internal static partial class ResourceLotTests
    {
        internal const string FlowCase = "Resource lots: JSON quality flow attribution preserves committed partitions private saved history and replay";
        private static GameFactory FlowFactory(bool independent, Action<JsonValue> edit = null)
            => ProductionFactory(independent, "{\"supply.purity\":{\"source\":\"supply.purity\",\"rounding\":\"exact\"}}", contentEdit: document => {
                document.Set(independent ? "proof_flow_queries" : "resource_flow_queries", Json.Parse("[{\"id\":\"flow.opening\",\"resource\":\"supply\",\"kinds\":[\"initialized\"],\"selection\":\"supply.high\",\"window\":{\"numerator\":1,\"denominator\":1,\"unit\":\"hour\"},\"per\":\"minute\"},{\"id\":\"flow.output\",\"resource\":\"supply\",\"kinds\":[\"produced\"],\"selection\":\"supply.high\",\"window\":{\"numerator\":1,\"denominator\":1,\"unit\":\"hour\"},\"per\":\"minute\"}]"));
                edit?.Invoke(document);
            }, processEdit: root => root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId)));
        private static void QualityFlows(Check c)
        {
            foreach (bool independent in new[] { false, true })
            {
                var factory = FlowFactory(independent); var game = TransformerTests.New(factory); var source = Find(game, "assembler"); var receiver = Find(game, "lot.receiver");
                var resource = game.World.Content.ResourceId("supply"); var view = game.Seat(0).View.Feature<ResourceFlowView>();
                var opening = view.Read(Company, 0).Entries.Single(e => e.Item.Kind == ResourceFlowKind.Initialized).Item;
                c.Equal(Fix.FromInt(10), opening.Amount, "initial stock has one original opening fact, not a second grant");
                c.Equal(2, opening.Lots.Count, "opening fact includes JSON tags before publication");
                c.Equal(Fix.FromInt(6), opening.Lots.Single(l => l.Lot == new Code("lot.high")).Amount, "opening fact identifies six actual high-grade units");
                c.Throws<NotSupportedException>(() => ((IList<ResourceLotAmount>)opening.Lots).Clear(), "published lot rows are detached and immutable");
                var foreign = game.Seat(1).View.Feature<ResourceFlowView>().Read(Company, 0);
                c.That(foreign.Status == ResourceFlowReadStatus.NotOwned && foreign.Entries.Count == 0 && foreign.Latest == 0, "foreign history reveals no attribution or watermark");
                c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<ResourceFlowView>().ReadAuthoredThroughput(Company, "flow.opening", out var denied), "foreign JSON query refuses before metadata"); c.That(denied == null, "foreign query exposes no prefix");
                UpdateLots(game); ulong before = game.Hash();
                c.Equal(Code.None, view.ReadAuthoredThroughput(Company, "flow.opening", out var initial), "engine evaluates actual JSON rolling-window profile");
                c.Equal(ResourceFlowReadStatus.Ready, initial.Status, "native/direct JSON query is available"); c.Equal(new BigInteger(Fix.FromInt(6).Raw), initial.AmountRaw.Value, "query counts selected six, not whole mixed opening ten"); c.Equal(1L, initial.Facts, "mixed fact counted once rather than once per lot"); c.Equal(before, game.Hash(), "quality history read is pure");
                var limits = new ReplayLimits(4 * 1024 * 1024, 500); using var recorder = game.RecordReplay(limits);
                c.That(TransformerTests.Queue(game, "assembler", "build").Accepted, "ordinary timed recipe produces the attributed output");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "saved graded WIP and attribution use original state owner");
                for (int i = 0; i < 30 && !Lots(game, source).Any(l => l.GeneratedQualities != null); i++) UpdateLots(game, twin);
                UpdateLots(game, twin); // Half-open windows only include completed prior steps.
                var page = view.Read(Company, 0); var produced = page.Entries.Single(e => e.Item.Kind == ResourceFlowKind.Produced).Item;
                c.Equal(Fix.FromRatio(5, 8), produced.Lots.Single().GeneratedQualities["supply.purity"], "output fact records actual produced quality");
                c.Equal(Lots(game, source).Single(l => l.GeneratedQualities != null).Lot, produced.Lots.Single().Lot, "flow identity equals physical output identity");
                var consumed = page.Entries.Single(e => e.Item.Kind == ResourceFlowKind.Consumed).Item;
                c.Equal(Fix.FromInt(6), consumed.Lots.Single(l => l.Lot == new Code("lot.high")).Amount, "conversion input fact retains actual high allocation");
                c.Equal(Fix.FromInt(2), consumed.Lots.Single(l => l.Lot == new Code("lot.low")).Amount, "conversion input fact retains actual low allocation");
                var transfer = page.Entries.Single(e => e.Item.IsTransfer).Item;
                c.That(transfer.Lots.Count == 2 && transfer.Amount == Fix.FromInt(8), "original WIP movement attributes both physical input lots");
                c.Equal(Code.None, view.ReadAuthoredThroughput(Company, "flow.output", out var output), "JSON profile observes generated output without host filtering");
                c.Equal(new BigInteger(Fix.FromInt(4).Raw), output.AmountRaw.Value, "quality-qualified output throughput is actual four-unit batch");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits); c.That(replay.Success, "timed attributed output replays"); c.Equal(game.Hash(), replay.ActualHash, "replay reaches actual live journal/hash");
                recorder.Dispose();
                twin = factory.Load(game.Save());
                foreach (var current in new[] { game, twin })
                {
                    var tx = current.World.Services.Get<IResourceTransaction>(); var currentSource = Find(current, "assembler");
                    var owner = new ReservationOwner("proof.flow.selected", 1); var high = current.World.Content.Get<IResourceLotSelectionDefinition>("supply.high");
                    var acquired = tx.AcquireClaims(Company, new[] { new ResourceClaimIntent(ReservationDirection.Outbound, owner, ResourceEndpointId.ForEntity(currentSource), Quantity(resource, 2), new[] { high }) }, Array.Empty<ResourceReservation>());
                    var receipt = acquired.Receipts.Single();
                    var movement = new ResourceMovementIntent(owner, Company, ResourceEndpointId.ForEntity(currentSource), ResourceEndpointId.ForEntity(receiver), Quantity(resource, 1), new[] { receipt }, Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
                    c.That(tx.CommitMovement(movement).Committed, "ordinary original transfer moves generated selected unit");
                    var held = Store(current, currentSource).Reservations.Single();
                    c.That(tx.ConsumeClaims(Company, new[] { held }, new Code("proof.selected.consume"), Array.Empty<ResourceReservation>()).Committed, "normal owned consumption retains original allocation");
                    UpdateLots(current);
                    var facts = current.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0).Entries;
                    var shipment = facts.Last(e => e.Item.IsTransfer).Item;
                    c.That(shipment.Selection == new Code(high.Id) && shipment.Lots.Single().Lot == produced.Lots.Single().Lot && shipment.Amount == Fix.One, "transfer fact retains actual selection and partial produced identity");
                    var spent = facts.Last(e => e.Item.Kind == ResourceFlowKind.Consumed).Item;
                    c.That(spent.Selection == new Code(high.Id) && spent.Lots.Single().Amount == Fix.One && spent.Lots.Single().Lot == produced.Lots.Single().Lot, "consume fact records exact protected remainder");
                    foreach (var kind in new[] { ResourceFlowKind.Destroyed, ResourceFlowKind.Lost })
                    {
                        c.That(tx.CommitInventory(new ResourceInventoryIntent(new ReservationOwner("proof.flow.disposition", (int)kind), Company, currentSource, Quantity(resource, 1), kind, new Code("proof.disposition"))).Committed, "original direct disposition commits: " + kind);
                        UpdateLots(current);
                        var fact = current.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0).Entries.Last().Item;
                        c.That(fact.Kind == kind && fact.Lots.Single().Lot == new Code("lot.low") && fact.Lots.Single().Amount == Fix.One, "disposition attributes exact unclaimed low partition: " + kind);
                    }
                }
                c.Equal(game.Hash(), twin.Hash(), "saved generated transfer/consumption/disposition histories continue identically");
                var saved = factory.Load(game.Save()); c.Equal(game.Hash(), saved.Hash(), "generated history saves independently of moved/consumed source stock");
                var txLive = game.World.Services.Get<IResourceTransaction>(); before = game.Hash(); long sequence = view.ReadSummary(Company).Latest;
                c.That(!txLive.CommitMovement(Movement(game, source, receiver, 100)).Committed, "refused movement has no committed flow");
                c.Equal(sequence, view.ReadSummary(Company).Latest, "refusal advances no flow sequence"); c.Equal(before, game.Hash(), "refusal preserves stock and history");
                var opaque = Find(game, "plain.endpoint"); var unknownOwner = new ReservationOwner("proof.flow.opaque", 1);
                c.That(txLive.CommitInventory(new ResourceInventoryIntent(unknownOwner, Company, opaque, Quantity(resource, 2), ResourceFlowKind.Produced, new Code("proof.flow.opaque"))).Committed, "independent resource-only provider remains supported without fabricated lot metadata");
                UpdateLots(game);
                var unknown = new ResourceFlowQuery(Company, resource, new[] { ResourceFlowKind.Produced }, 0, game.Clock.StepIndex, DurationUnit.Minute, cause: new Code("proof.flow.opaque"), selection: new Code("supply.high"));
                var measured = view.ReadThroughput(unknown); c.Equal(ResourceFlowReadStatus.Unattributed, measured.Status, "quality query refuses incomplete provider attribution instead of false zero"); c.That(!measured.AmountRaw.HasValue && !measured.Rate.HasValue, "unknown grade produces no invented measurement");
                var general = view.ReadThroughput(new ResourceFlowQuery(Company, resource, new[] { ResourceFlowKind.Produced }, 0, game.Clock.StepIndex, DurationUnit.Minute, cause: new Code("proof.flow.opaque")));
                c.Equal(new BigInteger(Fix.FromInt(2).Raw), general.AmountRaw.Value, "same opaque fact still supports honest resource-only throughput");
                var rawBefore = view.ReadSummary(Company).Aggregates.Where(a => a.Kind == ResourceFlowKind.Produced).Aggregate(BigInteger.Zero, (sum, a) => sum + a.AmountRaw);
                for (int i = 0; i < 35; i++)
                {
                    var owner = new ReservationOwner("proof.flow.eviction", i + 1);
                    foreach (var kind in new[] { ResourceFlowKind.Produced, ResourceFlowKind.Consumed })
                        if (!txLive.CommitInventory(new ResourceInventoryIntent(owner, Company, receiver, Quantity(resource, 1), kind, new Code("proof.flow.eviction"))).Committed) throw new InvalidOperationException("eviction protocol mutation refused");
                }
                UpdateLots(game);
                c.Equal(ResourceFlowReadStatus.Expired, view.ReadThroughput(new ResourceFlowQuery(Company, resource, new[] { ResourceFlowKind.Produced }, 0, game.Clock.StepIndex, DurationUnit.Minute, selection: new Code("supply.high"))).Status, "expired quality window refuses partial retained totals");
                var rawAfter = view.ReadSummary(Company).Aggregates.Where(a => a.Kind == ResourceFlowKind.Produced).Aggregate(BigInteger.Zero, (sum, a) => sum + a.AmountRaw);
                c.Equal(rawBefore + (BigInteger)35 * Fix.One.Raw, rawAfter, "original scalar cumulative summaries count quantities once, not once per lot");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "evicted attributed journal keeps original cumulative/retained invariants");
            }
            foreach (bool independent in new[] { false, true }) foreach (string invalid in new[] { "selection", "resource", "kinds", "window", "extra" })
                c.Throws<ContentException>(() => TransformerTests.New(FlowFactory(independent, document => {
                    var query = document.Get(independent ? "proof_flow_queries" : "resource_flow_queries")[0];
                    if (invalid == "selection" || invalid == "resource") query.Set(invalid, JsonValue.Of("absent"));
                    else if (invalid == "kinds") query.Set("kinds", Json.Parse("[\"produced\",\"produced\"]"));
                    else if (invalid == "window") query.Get("window").Set("numerator", JsonValue.Number("0"));
                    else query.Set("extra", JsonValue.Of("invalid"));
                })), "malformed native/direct JSON flow profile refuses before play: " + independent + "/" + invalid);
            foreach (string invalid in new[] { "quantity", "duplicate", "unknown-lot", "generated-range", "selection" })
            {
                var game = TransformerTests.New(FlowFactory(false));
                var original = game.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0).Entries.Single().Item;
                IReadOnlyList<ResourceLotAmount> partitions = original.Lots;
                if (invalid == "quantity") partitions = new[] { original.Lots[0].WithAmount(Fix.One) };
                else if (invalid == "duplicate") partitions = new[] { original.Lots[0].WithAmount(Fix.FromInt(5)), original.Lots[0].WithAmount(Fix.FromInt(5)) };
                else if (invalid == "unknown-lot") partitions = new[] { new ResourceLotAmount(original.Resource, new Code("absent.lot"), original.Amount) };
                else if (invalid == "generated-range") partitions = new[] { ResourceLotAmount.Produced(original.Resource, original.Amount, new Dictionary<string, Fix> { ["supply.purity"] = Fix.FromInt(2) }, "proof.invalid.batch") };
                var malformed = original.WithLots(partitions, invalid == "selection" ? new Code("supply.high") : Code.None);
                game.World.Events.Publish(malformed); game.Update();
                c.That(game.Fault != null, "malformed committed attribution trips original first fault: " + invalid);
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted attributed history cannot checkpoint: " + invalid);
            }
        }
    }
}
