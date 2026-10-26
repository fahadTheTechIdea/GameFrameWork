using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static partial class ResourceLotTests
    {
        internal const string Case = "Resource lots: JSON quality selection same-buffer split merge and saved contracts";
        internal const string PhysicalCase = "Resource lots: ordinary physical hauling preserves cargo quality saved twins and replay";
        internal const string ReservationCase = "Resource lots: quality-specific whole-set reservations protect partitions and saved pickup";
        internal const string WorkCase = "Resource lots: JSON activity quality requirements share native claims saved work and replay";
        internal const string DispatchCase = "Resource lots: quality-aware player hauling and JSON stock dispatch preserve physical selected cargo";
        internal const string SupplyCase = "Resource lots: automatic activity supply keeps distinct grade demands and saved physical dispatch";
        internal const string ProductionCase = "Resource lots: JSON recipe output and weighted blending preserve actual consumed composition";
        internal const string BlendCase = "Resource lots: blend refusal rounding and continuous production use original conversion admission";
        private const string Declaration = "{\"id\":\"proof.resource-lots\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"resource-lots.json\"]}";
        private static readonly PlayerId Company = new PlayerId(0);
        internal static void Register() { TestRunner.Add(Case, Contracts); TestRunner.Add(PhysicalCase, Physical); TestRunner.Add(ReservationCase, Reservations); TestRunner.Add(WorkCase, QualityWork); TestRunner.Add(DispatchCase, QualityDispatch); TestRunner.Add(SupplyCase, QualitySupply); TestRunner.Add(ProductionCase, QualityProduction); TestRunner.Add(BlendCase, BlendAdmission); TestRunner.Add(FlowCase, QualityFlows); }
        private static JsonValue Content(bool independent)
            => Json.Parse("{\"units\":[{\"id\":\"unit.grade\",\"dimension\":\"grade\",\"scale\":{\"numerator\":1,\"denominator\":1},\"discrete\":false}],\""
                + (independent ? "proof_resource_qualities" : "resource_qualities") + "\":[{\"id\":\"supply.purity\",\"resource\":\"supply\",\"unit\":\"unit.grade\",\"minimum\":0,\"maximum\":1}],\""
                + (independent ? "proof_resource_lots" : "resource_lots") + "\":[{\"id\":\"lot.high\",\"resource\":\"supply\",\"qualities\":{\"supply.purity\":0.75}},{\"id\":\"lot.low\",\"resource\":\"supply\",\"qualities\":{\"supply.purity\":0.25}}]}");
        private static JsonValue Storage(bool initial)
            => Json.Parse("{\"capacity_policy\":\"fixture.count.40\",\"initial_stock\":{},\"initial_lots\":" + (initial
                ? "[{\"lot\":\"lot.high\",\"amount\":6},{\"lot\":\"lot.low\",\"amount\":4}]" : "[]")
                + ",\"accepts\":[\"supply\"],\"phases\":[],\"public\":false,\"evacuate\":false}");
        private static GameFactory Factory(bool independent, Action<JsonValue> edit = null, Action<JsonValue> processEdit = null)
        {
            var content = Content(independent);
            content.Set(independent ? "proof_resource_selections" : "resource_selections", Selections()); edit?.Invoke(content);
            var extra = Json.Parse(FrameworkProofContent.Read("transformer-proof.json"));
            extra.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("lot.receiver.start"));
            extra.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("plain.endpoint.start"));
            return TransformerTests.Factory(root => {
                processEdit?.Invoke(root);
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.resource-lots"));
                var parts = root.Get("entities")[0].Get("components"); parts.Remove("process_stock"); parts.Set("storage", Storage(true));
                root.Get("entities").Add(Json.Parse("{\"id\":\"lot.receiver\",\"kind\":\"entity\",\"components\":{\"storage\":" + Storage(false) + "}}"));
                root.Get("entity_setups").Add(Json.Parse("{\"id\":\"lot.receiver.start\",\"after\":[],\"entity\":\"lot.receiver\",\"company\":0,\"placement\":null,\"home\":null}"));
                root.Get("entities").Add(Json.Parse("{\"id\":\"plain.endpoint\",\"kind\":\"entity\",\"components\":{\"process_stock\":{\"resource\":\"supply\",\"capacity_policy\":\"fixture.count.40\",\"initial\":0}}}"));
                root.Get("entity_setups").Add(Json.Parse("{\"id\":\"plain.endpoint.start\",\"after\":[],\"entity\":\"plain.endpoint\",\"company\":0,\"placement\":null,\"home\":null}"));
            }, new ResourceLotProofModule(), Declaration, content.ToString(), transformerDocument: extra.ToString());
        }
        private static EntityId Find(SimGame game, string definition) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == definition).Id;
        private static JsonValue Selections() => Json.Parse("[{\"id\":\"supply.high\",\"resource\":\"supply\",\"lot\":null,\"qualities\":{\"supply.purity\":{\"minimum\":0.5,\"maximum\":1}}},{\"id\":\"supply.low\",\"resource\":\"supply\",\"lot\":\"lot.low\",\"qualities\":{}}]");
        private static IResourceLotEndpoint Store(SimGame game, EntityId entity) => (IResourceLotEndpoint)game.World.Entities.Get(entity).Get<IOutletPort>();
        private static Bill Quantity(ResourceId resource, int amount) => new Bill(new[] { new ResourceAmount(resource, Fix.FromInt(amount)) });
        private static GameFactory ProductionFactory(bool independent, string rule, bool continuous = false, Action<JsonValue> contentEdit = null, Action<JsonValue> processEdit = null)
            => Factory(independent, edit: contentEdit, processEdit: root => {
                var recipe = root.Get("transformations")[0];
                recipe.Set("inputs", Json.Parse(continuous ? "[{\"resource\":\"supply\",\"quantity\":2,\"period\":{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"}}]" : "{\"supply\":8}"));
                recipe.Set("outputs", Json.Parse(continuous ? "[{\"resource\":\"supply\",\"quantity\":1,\"period\":{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"}}]" : "{\"supply\":4}"));
                recipe.Set("entity_output", JsonValue.Null()); recipe.Set("output_home", JsonValue.Null());
                recipe.Set("output_qualities", Json.Parse(rule));
                if (continuous) { recipe.Set("mode", JsonValue.Of("continuous")); recipe.Set("cancel_policy", JsonValue.Of("stop")); recipe.Set("duration", JsonValue.Null()); recipe.Set("tool_consumption", Json.Parse("[]")); }
                if (independent) { root.Set("proof_lot_recipes", root.Get("transformations")); root.Remove("transformations"); }
                processEdit?.Invoke(root);
            });
        private static void QualityProduction(Check c)
        {
            const string blend = "{\"supply.purity\":{\"source\":\"supply.purity\",\"rounding\":\"exact\"}}";
            foreach (bool independent in new[] { false, true }) foreach (bool fixedGrade in new[] { false, true })
            {
                var factory = ProductionFactory(independent, fixedGrade ? "{\"supply.purity\":{\"constant\":0.5}}" : blend);
                var game = TransformerTests.New(factory); var source = Find(game, "assembler"); var receiver = Find(game, "lot.receiver"); var resource = game.World.Content.ResourceId("supply");
                var limits = new ReplayLimits(4 * 1024 * 1024, 500); using var recorder = game.RecordReplay(limits);
                c.That(TransformerTests.Queue(game, "assembler", "build").Accepted, "player queues the existing timed recipe");
                c.That(Lots(game, source).All(l => l.GeneratedQualities == null), "queue does not instantly manufacture graded output");
                UpdateLots(game); var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "reserved graded WIP survives saved timed production");
                for (int i = 0; i < 30 && !Lots(game, source).Any(l => l.GeneratedQualities != null); i++) UpdateLots(game, twin);
                var output = Lots(game, source).Single(l => l.GeneratedQualities != null);
                Fix expected = fixedGrade ? Fix.Half : Fix.FromRatio(5, 8);
                c.Equal(expected, output.GeneratedQualities["supply.purity"], "output uses fixed grade or weighted six high plus two low actual consumed units");
                c.Equal(Fix.FromInt(4), output.Amount, "output quantity remains authored conversion quantity, not quality-weighted stock");
                c.Equal(Fix.FromInt(6), Store(game, source).Amount(resource), "eight inputs become four outputs beside untouched two low units");
                c.That(output.ValidMetadata(game.World.Content), "generated immutable composition uses actual resource axes and unit bounds");
                c.That(game.World.Content.Find<IResourceLotDefinition>(output.Lot.Id) == null, "runtime blend is not inserted into a second content catalog");
                c.That(!new ResourceLotAmount(resource, output.Lot, Fix.One).ValidMetadata(game.World.Content), "generated identity cannot stand in for lost composition metadata");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits); c.That(replay.Success, "timed JSON quality recipe replays"); c.Equal(game.Hash(), replay.ActualHash, "quality recipe replay reaches live hash");
                var selected = game.World.Content.Get<IResourceLotSelectionDefinition>("supply.high");
                var owner = new ReservationOwner("proof.blend.pickup", 1); var tx = game.World.Services.Get<IResourceTransaction>();
                var acquired = tx.AcquireClaims(Company, new[] { new ResourceClaimIntent(ReservationDirection.Outbound, owner, ResourceEndpointId.ForEntity(source), Quantity(resource, 4), new[] { selected }) }, Array.Empty<ResourceReservation>());
                c.That(acquired.Committed, "existing quality reservation selects generated output without host logic");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "generated held allocation saves and hashes with original book");
                foreach (var current in new[] { game, twin })
                {
                    var receipt = Store(current, source).Reservations.Single();
                    var move = new ResourceMovementIntent(owner, Company, ResourceEndpointId.ForEntity(source), ResourceEndpointId.ForEntity(receiver), Quantity(resource, 2), new[] { receipt }, Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
                    c.That(current.World.Services.Get<IResourceTransaction>().CommitMovement(move).Committed, "original transaction splits generated output without losing composition");
                    c.Equal(expected, Lots(current, receiver).Single().GeneratedQualities["supply.purity"], "delivered generated composition remains exact");
                }
                c.Equal(game.Hash(), twin.Hash(), "saved generated partial pickup continues identically");
                c.Equal(Fix.FromInt(2), Store(game, source).Reservations.Single().Lots.Single().Amount, "remaining held composition shrinks in original receipt");
            }
            foreach (string bad in new[] { "{\"missing\":{\"constant\":0.5}}", "{\"supply.purity\":{\"constant\":2}}", "{\"supply.purity\":{\"source\":\"missing\",\"rounding\":\"exact\"}}", "{\"supply.purity\":{\"source\":\"supply.purity\",\"rounding\":\"default\"}}", "{\"supply.purity\":{\"constant\":0.5,\"source\":\"supply.purity\",\"rounding\":\"exact\"}}" })
                c.Throws<ContentException>(() => TransformerTests.New(ProductionFactory(false, bad)), "invalid output rule refuses before initialization");
            var directFactory = ProductionFactory(false, blend); var direct = TransformerTests.New(directFactory); var input = Find(direct, "assembler"); var supply = direct.World.Content.ResourceId("supply");
            var transaction = direct.World.Services.Get<IResourceTransaction>(); var ruleSet = direct.World.Content.Get<ITransformationDefinition>("build").OutputQualities;
            var endpoint = ResourceEndpointId.ForEntity(input); var conversionOwner = new ReservationOwner("proof.blend.coupled", 1);
            var claims = new[] { new ResourceClaimIntent(ReservationDirection.Outbound, conversionOwner, endpoint, Quantity(supply, 8)) };
            var production = new[] { new ResourceProductionIntent(endpoint, Quantity(supply, 4), ruleSet) };
            ulong before = direct.Hash(); c.That(transaction.EvaluateConversionClaims(Company, conversionOwner, claims, production, new Code("build")).Ready, "same-buffer preview uses exact proposed selected lots"); c.Equal(before, direct.Hash(), "quality conversion preflight is pure");
            var receipts = transaction.AcquireClaims(Company, claims, Array.Empty<ResourceReservation>());
            var conversion = new ResourceConversionIntent(conversionOwner, Company, new[] { new ResourceConversionInput(ResourceFlowKind.Consumed, receipts.Receipts.Single(), Fix.FromInt(8), true) }, production, new Code("build"));
            c.That(transaction.CommitConversion(conversion, null).Committed, "original atomic same-buffer conversion produces composition");
            c.Equal(Fix.FromRatio(5, 8), Lots(direct, input).Single(l => l.GeneratedQualities != null).GeneratedQualities["supply.purity"], "coupled blend uses reserved stock, not post-consumption balance");
            c.Equal(direct.Hash(), directFactory.Load(direct.Save()).Hash(), "same-buffer generated output uses canonical save path");
        }
        private static void BlendAdmission(Check c)
        {
            foreach (string rounding in new[] { "exact", "floor", "ceiling" })
            {
                var factory = ProductionFactory(false, "{\"supply.purity\":{\"source\":\"supply.purity\",\"rounding\":\"" + rounding + "\"}}");
                var game = TransformerTests.New(factory); var source = Find(game, "assembler"); var destination = Find(game, "lot.receiver");
                var resource = game.World.Content.ResourceId("supply"); var tx = game.World.Services.Get<IResourceTransaction>(); var owner = new ReservationOwner("proof.blend.rounding", 1);
                var claims = new[] { new ResourceClaimIntent(ReservationDirection.Outbound, owner, ResourceEndpointId.ForEntity(source), Quantity(resource, 7)) };
                var outputs = new[] { new ResourceProductionIntent(ResourceEndpointId.ForEntity(destination), Quantity(resource, 3), game.World.Content.Get<ITransformationDefinition>("build").OutputQualities) };
                ulong before = game.Hash(); var preview = tx.EvaluateConversionClaims(Company, owner, claims, outputs, new Code("build"));
                c.Equal(rounding == "exact" ? ResourceTransferStatus.Incompatible : ResourceTransferStatus.Ready, preview.Status, "19/28 weighted grade follows explicit one-time rounding"); c.Equal(before, game.Hash(), "rounding admission never mutates stock or claims");
                var held = tx.AcquireClaims(Company, claims, Array.Empty<ResourceReservation>());
                var intent = new ResourceConversionIntent(owner, Company, new[] { new ResourceConversionInput(ResourceFlowKind.Consumed, held.Receipts.Single(), Fix.FromInt(7), true) }, outputs, new Code("build"));
                before = game.Hash(); var result = tx.CommitConversion(intent, null);
                if (rounding == "exact") { c.Equal(ResourceTransferStatus.Incompatible, result.Status, "unrepresentable exact grade refuses"); c.Equal(before, game.Hash(), "refused conversion retains complete original receipts and stock"); }
                else
                {
                    c.That(result.Committed, "explicit rounded blend commits through original transaction");
                    long floor = 2914442093L; // floor((19/28) * 2^32), one final raw-unit rounding.
                    c.Equal(Fix.FromRaw(rounding == "floor" ? floor : floor + 1), Lots(game, destination).Single().GeneratedQualities["supply.purity"], "no intermediate fixed-point multiplication or mean truncation");
                    c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "rounded composition saves exactly");
                }
            }
            foreach (string reason in new[] { "untracked", "unsupported", "destroyed", "empty-feed" })
            {
                var factory = ProductionFactory(false, "{\"supply.purity\":{\"source\":\"supply.purity\",\"rounding\":\"exact\"}}"); var game = TransformerTests.New(factory);
                var source = Find(game, "assembler"); var destination = Find(game, reason == "unsupported" ? "plain.endpoint" : "lot.receiver");
                var resource = game.World.Content.ResourceId("supply"); var tx = game.World.Services.Get<IResourceTransaction>(); var owner = new ReservationOwner("proof.blend.refusal", 1);
                if (reason == "untracked")
                {
                    c.That(tx.CommitInventory(new ResourceInventoryIntent(owner, Company, source, Quantity(resource, 1), ResourceFlowKind.Produced, new Code("proof.untracked"))).Committed, "resource-only production is honestly untracked");
                }
                var claims = new[] { new ResourceClaimIntent(ReservationDirection.Outbound, owner, ResourceEndpointId.ForEntity(source), Quantity(resource, 1)) };
                var held = tx.AcquireClaims(Company, claims, Array.Empty<ResourceReservation>());
                var inputs = reason == "empty-feed" ? Array.Empty<ResourceConversionInput>() : new[] { new ResourceConversionInput(reason == "destroyed" ? ResourceFlowKind.Destroyed : ResourceFlowKind.Consumed, held.Receipts.Single(), Fix.One, false) };
                var outputs = new[] { new ResourceProductionIntent(ResourceEndpointId.ForEntity(destination), Quantity(resource, 1), game.World.Content.Get<ITransformationDefinition>("build").OutputQualities) };
                var intent = new ResourceConversionIntent(owner, Company, inputs, outputs, new Code("build")); ulong before = game.Hash();
                c.Equal(ResourceTransferStatus.Incompatible, tx.EvaluateConversion(intent, null).Status, "missing source-quality/capability refuses: " + reason);
                c.Equal(ResourceTransferStatus.Incompatible, tx.CommitConversion(intent, null).Status, "commit shares quality refusal: " + reason);
                c.Equal(before, game.Hash(), "refusal publishes no stock/claim/version prefix: " + reason);
            }
            foreach (bool independent in new[] { false, true })
            {
                var factory = ProductionFactory(independent, "{\"supply.purity\":{\"source\":\"supply.purity\",\"rounding\":\"floor\"}}", continuous: true);
                var game = TransformerTests.New(factory); var source = Find(game, "assembler"); var limits = new ReplayLimits(4 * 1024 * 1024, 500); using var recorder = game.RecordReplay(limits);
                c.That(TransformerTests.Queue(game, "assembler", "build").Accepted, "same queue starts authored continuous quality recipe");
                for (int i = 0; i < 15 && !Lots(game, source).Any(l => l.GeneratedQualities != null); i++) UpdateLots(game);
                c.Equal(Fix.FromRatio(3, 4), Lots(game, source).Single(l => l.GeneratedQualities != null).GeneratedQualities["supply.purity"], "first continuous interval consumes two actual high-grade units");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "continuous generated output saves mid-operation");
                for (int i = 0; i < 5; i++) UpdateLots(game, twin);
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits); c.That(replay.Success, "continuous quality intervals replay without separate blend scheduler"); c.Equal(game.Hash(), replay.ActualHash, "continuous replay reaches actual live hash");
            }
            var haulFactory = LogisticsFactory(true, "manual", document => document.Set("proof_lot_recipes", Json.Parse("[{\"id\":\"blend.cargo\",\"mode\":\"batch\",\"tools\":{},\"tool_consumption\":{},\"operating_inputs\":[],\"input_ports\":{\"supply\":\"feed\"},\"output_ports\":{\"supply\":\"output\"},\"tool_ports\":{},\"operating_ports\":{},\"inputs\":{\"supply\":6},\"outputs\":{\"supply\":6},\"output_qualities\":{\"supply.purity\":{\"source\":\"supply.purity\",\"rounding\":\"exact\"}},\"entity_output\":null,\"output_home\":null,\"duration\":{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"},\"cancel_policy\":\"return_inputs\"}]")));
            var haul = ConstructionPlanTests.NewPoints(haulFactory, ResourceFlowsModule.ModuleId); var far = Find(haul, "grade.source"); var target = Find(haul, "destination"); var worker = Find(haul, "machine.a"); var supply = haul.World.Content.ResourceId("supply");
            var transactions = haul.World.Services.Get<IResourceTransaction>(); var batchOwner = new ReservationOwner("proof.generated.cargo", 1); var endpoint = ResourceEndpointId.ForEntity(far);
            var claimed = transactions.AcquireClaims(Company, new[] { new ResourceClaimIntent(ReservationDirection.Outbound, batchOwner, endpoint, Quantity(supply, 6)) }, Array.Empty<ResourceReservation>());
            c.That(transactions.CommitConversion(new ResourceConversionIntent(batchOwner, Company, new[] { new ResourceConversionInput(ResourceFlowKind.Consumed, claimed.Receipts.Single(), Fix.FromInt(6), true) }, new[] { new ResourceProductionIntent(endpoint, Quantity(supply, 6), haul.World.Content.Get<ITransformationDefinition>("blend.cargo").OutputQualities) }, new Code("blend.cargo")), null).Committed, "engine conversion creates actual generated cargo before player hauling");
            UpdateLots(haul); // Commit-generated events use the existing Update/event boundary.
            var haulLimits = new ReplayLimits(4 * 1024 * 1024, 2000); using var haulingRecorder = haul.RecordReplay(haulLimits);
            c.That(TestKit.Order(haul, 0, new HaulResourcesCommand { Source = far, Destination = target, Worker = worker, Resource = supply, Ceiling = Fix.FromInt(6), Priority = 50, Selection = new Code("supply.high") }).Accepted, "normal player command accepts generated grade");
            for (int i = 0; i < 200 && Lots(haul, worker).Count == 0; i++) UpdateLots(haul);
            c.That(Lots(haul, worker).Any(l => l.GeneratedQualities != null), "generated composition loads physically, not instant delivery");
            var pickup = haul.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0).Entries.Last().Item;
            c.That(pickup.Destination.Endpoint == ResourceEndpointId.ForEntity(worker) && pickup.Selection == new Code("supply.high") && pickup.Lots.Single().GeneratedQualities != null,
                "physical pickup flow identifies selected generated cargo at actual transporter");
            var cargoTwin = haulFactory.Load(haul.Save()); c.Equal(haul.Hash(), cargoTwin.Hash(), "generated cargo and its outstanding receipts survive partial loading save");
            for (int i = 0; i < 240 && Store(haul, target).Amount(supply) != Fix.FromInt(10); i++) UpdateLots(haul, cargoTwin);
            c.Equal(Fix.FromInt(6), Lots(haul, target).Single(l => l.GeneratedQualities != null).Amount, "travel and unloading preserve entire generated batch beside authored low grade");
            var delivery = haul.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0).Entries.Last().Item;
            c.That(delivery.Source.Endpoint == ResourceEndpointId.ForEntity(worker) && delivery.Destination.Endpoint == ResourceEndpointId.ForEntity(target) &&
                delivery.Lots.Single().Lot == Lots(haul, target).Single(l => l.GeneratedQualities != null).Lot && delivery.Lots.Single().Amount == delivery.Amount,
                "physical unloading flow preserves actual generated identity and handled quantity");
            var verified = new ReplayVerifier().Verify(haulFactory, haulingRecorder.Finish(), haulLimits); c.That(verified.Success, "generated cargo player hauling replays"); c.Equal(haul.Hash(), verified.ActualHash, "generated cargo replay reaches live hash");
        }
        private static void Release(SimGame game, IReadOnlyList<ResourceReservation> receipts)
        {
            var result = game.World.Services.Get<IResourceTransaction>().ReleaseClaims(new ResourceClaimReleaseIntent(Company, receipts,
                Array.Empty<ResourceReservation>(), Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact));
            if (!result.Committed) throw new InvalidOperationException("actual lot release refused: " + result.Status);
        }
        private static void Reservations(Check c)
        {
            foreach (bool independent in new[] { false, true })
            {
                var factory = Factory(independent); var game = TransformerTests.New(factory); var twin = TransformerTests.New(factory);
                var source = Find(game, "assembler"); var receiver = Find(game, "lot.receiver"); var resource = game.World.Content.ResourceId("supply");
                var endpoint = ResourceEndpointId.ForEntity(source); var tx = game.World.Services.Get<IResourceTransaction>();
                var high = game.World.Content.Find<IResourceLotSelectionDefinition>("supply.high");
                var low = game.World.Content.Find<IResourceLotSelectionDefinition>("supply.low");
                var highOwner = new ReservationOwner("proof.lots.z-quality", 1); var ordinaryOwner = new ReservationOwner("proof.lots.a-ordinary", 1);
                ResourceClaimIntent Claim(ReservationOwner owner, int amount, IResourceLotSelectionDefinition selection = null)
                    => new ResourceClaimIntent(ReservationDirection.Outbound, owner, endpoint, Quantity(resource, amount), selection == null ? null : new[] { selection });
                var impossible = new[] { Claim(highOwner, 4, high), Claim(ordinaryOwner, 3, high) }; ulong before = game.Hash();
                c.Equal(ResourceTransferStatus.NoStock, tx.EvaluateClaims(Company, impossible, Array.Empty<ResourceReservation>()).Status, "enough total stock cannot satisfy seven high-grade units");
                c.That(!tx.AcquireClaims(Company, impossible, Array.Empty<ResourceReservation>()).Committed, "whole-set grade overcommit refuses before first claim");
                c.Equal(before, game.Hash(), "overlapping grade refusal has no stock, token or version prefix");
                var unsupported = new ResourceClaimIntent(ReservationDirection.Outbound, highOwner, ResourceEndpointId.ForEntity(Find(game, "plain.endpoint")), Quantity(resource, 1), new[] { high });
                c.Equal(ResourceTransferStatus.Incompatible, tx.EvaluateClaims(Company, new[] { unsupported }, Array.Empty<ResourceReservation>()).Status, "non-lot endpoint refuses typed quality selection, not silent resource-only fallback");
                c.Equal(before, game.Hash(), "unsupported selection has no prefix");
                var requests = new[] { Claim(ordinaryOwner, 4), Claim(highOwner, 6, high) };
                var check = tx.EvaluateClaims(Company, requests, Array.Empty<ResourceReservation>());
                c.That(check.Ready, "quality-bound and ordinary claims share one feasible whole-set admission"); c.Equal(before, game.Hash(), "claim preview is pure");
                var acquired = tx.AcquireClaims(Company, requests, Array.Empty<ResourceReservation>()); c.That(acquired.Committed, "whole set acquired through original transaction");
                var twinHigh = twin.World.Content.Find<IResourceLotSelectionDefinition>(high.Id);
                var reverse = new[] { new ResourceClaimIntent(ReservationDirection.Outbound, highOwner, endpoint, Quantity(resource, 6), new[] { twinHigh }), Claim(ordinaryOwner, 4) };
                c.That(twin.World.Services.Get<IResourceTransaction>().AcquireClaims(Company, reverse, Array.Empty<ResourceReservation>()).Committed, "reversed caller order admits same set");
                c.Equal(game.Hash(), twin.Hash(), "canonical allocation ignores caller ordering");
                var heldHigh = acquired.Receipts.Single(r => r.Owner.Equals(highOwner)); var heldOrdinary = acquired.Receipts.Single(r => r.Owner.Equals(ordinaryOwner));
                c.Equal(new Code(high.Id), heldHigh.Selection, "receipt retains actual authored quality selection");
                c.Equal(new Code("lot.high"), heldHigh.Lots.Single().Lot, "quality receipt holds six exact high-grade units");
                c.Equal(new Code("lot.low"), heldOrdinary.Lots.Single().Lot, "ordinary request cannot steal qualified partitions");
                c.Equal(Fix.Zero, Store(game, source).Available(resource), "aggregate availability derives from the same claims");
                c.That(!tx.EvaluateClaims(new PlayerId(1), requests, Array.Empty<ResourceReservation>()).Ready, "foreign company cannot acquire private lots");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual selected and ordinary lot reservations survive save");
                foreach (var current in new[] { game, twin })
                {
                    var currentTx = current.World.Services.Get<IResourceTransaction>();
                    c.That(currentTx.ConsumeClaims(Company, new[] { heldOrdinary }, new Code("proof.lots.consume"), Array.Empty<ResourceReservation>()).Committed, "ordinary consumption spends only its low-grade partition");
                    var move = new ResourceMovementIntent(highOwner, Company, endpoint, ResourceEndpointId.ForEntity(receiver), Quantity(resource, 2),
                        new[] { heldHigh }, Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
                    c.That(currentTx.CommitMovement(move).Committed, "partial selected pickup uses original owned transfer");
                    c.Equal(Fix.FromInt(4), Store(current, source).Reservations.Single().Lots.Single().Amount, "partial pickup reduces only the held partition");
                    c.Equal(new Code("lot.high"), Lots(current, receiver).Single().Lot, "partial delivery retains selected high-grade identity");
                    var currentReceipt = Store(current, source).Reservations.Single(); ulong unchanged = current.Hash();
                    c.That(!currentTx.AcquireClaims(Company, new[] { new ResourceClaimIntent(currentReceipt, Quantity(resource, 1)) }, Array.Empty<ResourceReservation>()).Committed, "selected receipt growth cannot invent more high-grade stock");
                    c.Equal(unchanged, current.Hash(), "failed selected growth preserves actual receipt");
                }
                c.Equal(game.Hash(), twin.Hash(), "saved partial pickup continues identically");
                twin = factory.Load(game.Save());
                foreach (var current in new[] { game, twin })
                {
                    Release(current, Store(current, source).Reservations);
                    c.Equal(Fix.FromInt(4), Store(current, source).Available(resource), "cancellation releases exact remaining high-grade partitions");
                    var selected = current.World.Content.Find<IResourceLotSelectionDefinition>(high.Id);
                    var request = new ResourceClaimIntent(ReservationDirection.Outbound, new ReservationOwner("proof.lots.grow", 2), endpoint, Quantity(resource, 2), new[] { selected });
                    var first = current.World.Services.Get<IResourceTransaction>().AcquireClaims(Company, new[] { request }, Array.Empty<ResourceReservation>());
                    var grow = current.World.Services.Get<IResourceTransaction>().AcquireClaims(Company, new[] { new ResourceClaimIntent(first.Receipts.Single(), Quantity(resource, 2)) }, Array.Empty<ResourceReservation>());
                    c.That(grow.Committed && grow.Receipts.Single().Amount == Fix.FromInt(4), "existing receipt grows with actual unclaimed matching stock");
                    c.Equal(new Code(selected.Id), grow.Receipts.Single().Selection, "growth preserves original quality intent");
                    c.Equal(1, grow.Receipts.Single().Lots.Count, "growth merges same held lot identity, not a new ledger");
                    Release(current, grow.Receipts);
                }
                c.Equal(game.Hash(), twin.Hash(), "saved cancellation and growth are deterministic");
                var shortage = tx.EvaluateClaims(Company, new[] { Claim(new ReservationOwner("proof.lots.low", 4), 1, low) }, Array.Empty<ResourceReservation>());
                c.Equal(ResourceTransferStatus.NoStock, shortage.Status, "exact low-lot request refuses after low stock was physically consumed");
                c.Equal(Fix.Zero, shortage.Availability.Single().Available, "quality diagnostics report eligible unclaimed stock, not aggregate balance");
            }
            foreach (string invalid in new[] { "axis", "range", "lot" })
                c.Throws<ContentException>(() => TransformerTests.New(Factory(false, root => {
                    var selection = root.Get("resource_selections")[0];
                    if (invalid == "axis") selection.Set("qualities", Json.Parse("{\"unknown.axis\":{\"minimum\":0,\"maximum\":1}}"));
                    else if (invalid == "range") selection.Get("qualities").Get("supply.purity").Set("minimum", JsonValue.Number("2"));
                    else selection.Set("lot", JsonValue.Of("unknown.lot"));
                })), "malformed selected reference/range refuses before initialization: " + invalid);
        }
        private static void QualityWork(Check c)
        {
            foreach (bool independent in new[] { false, true })
            {
                var content = Content(independent); content.Set(independent ? "proof_resource_selections" : "resource_selections", Selections());
                var factory = ServiceDockTests.Factory(root => {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.resource-lots"));
                    var parts = root.Get("entities")[0].Get("components"); parts.Remove("point_stock"); parts.Set("storage", Storage(true));
                    root.Set("target_resource_requirements", Json.Parse("[{\"id\":\"lots.hold-high\",\"port\":null,\"use\":\"hold\",\"quantities\":{\"supply\":6},\"lot_selections\":[\"supply.high\"]},{\"id\":\"lots.consume-low\",\"port\":null,\"use\":\"consume\",\"quantities\":{\"supply\":4},\"lot_selections\":[\"supply.low\"]}]"));
                    root.Get("activities")[0].Set("nodes", Json.Parse("[{\"id\":\"hold\",\"executor\":\"work.timed\",\"duration\":{\"numerator\":1,\"denominator\":1,\"unit\":\"hour\"},\"requirement\":\"lots.hold-high\"},{\"id\":\"consume\",\"executor\":\"work.timed\",\"duration\":{\"numerator\":1,\"denominator\":5,\"unit\":\"hour\"},\"requirement\":\"lots.consume-low\"}]"));
                }, extraModules: new[] { new ResourceLotProofModule() }, extraDeclarations: "[" + Declaration + "]", extraDocuments: new Dictionary<string, string> { ["resource-lots.json"] = content.ToString() });
                var game = ConstructionPlanTests.NewPoints(factory); var source = Find(game, "source"); var limits = new ReplayLimits(4 * 1024 * 1024, 2000);
                using var recorder = game.RecordReplay(limits);
                var command = new PostOrderCommand { Plan = "inspect", Target = source }; ulong before = game.Hash();
                c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary order preview accepts authored quality requirements"); c.Equal(before, game.Hash(), "quality work preview is pure");
                var order = TestKit.Order(game, 0, command); c.That(order.Accepted, "player posts normal activity, no host quality rules");
                for (int i = 0; i < 100 && Store(game, source).Reservations.Count == 0; i++) game.Update();
                c.That(Store(game, source).Reservations.Any(r => r.Selection == new Code("supply.high")), "actual activity holds selected high-grade partitions");
                for (int i = 0; i < 100 && Store(game, source).Amount(game.World.Content.ResourceId("supply")) != Fix.FromInt(6); i++)
                { game.Update(); if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString()); }
                c.Equal(Fix.FromInt(6), Store(game, source).Amount(game.World.Content.ResourceId("supply")), "low-grade input consumed without stealing retained high-grade tools");
                c.Equal(new Code("lot.high"), Lots(game, source).Single().Lot, "only actual held high-grade stock remains");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "saved activity reconstructs same JSON quality intent and live claims");
                int id = checked((int)order.Arg0);
                var executing = game.World.Services.Get<WorkBoard>().Order(id);
                for (int i = 0; i < 120 && executing.State != OrderState.Done; i++)
                { game.Update(); twin.Update(); if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("quality activity fault"); c.Equal(game.Hash(), twin.Hash(), "saved quality work progresses identically"); }
                c.Equal(OrderState.Done, executing.State, "original timed work finishes, not instant completion");
                c.Equal(0, Store(game, source).Reservations.Count, "successful completion releases original held partitions");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
                c.That(replay.Success, "ordinary selected work replays"); c.Equal(game.Hash(), replay.ActualHash, "quality work replay reaches actual live final hash");
            }
        }
        private static IReadOnlyList<ResourceLotAmount> Lots(SimGame game, EntityId entity, Code quality = default)
        {
            if (!game.Seat(0).View.Feature<InventoryView>().TryGetLots(entity, Code.None, out var lots, quality, Fix.FromRatio(1, 2), Fix.One))
                throw new InvalidOperationException("missing actual lot inventory");
            return lots;
        }
        private static ResourceMovementIntent Movement(SimGame game, EntityId from, EntityId to, int amount)
        {
            var resource = game.World.Content.ResourceId("supply");
            return new ResourceMovementIntent(new ReservationOwner("proof.lots", 1), Company, ResourceEndpointId.ForEntity(from), ResourceEndpointId.ForEntity(to),
                new Bill(new[] { new ResourceAmount(resource, Fix.FromInt(amount)) }), Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(),
                Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
        }
        private static void Contracts(Check c)
        {
            foreach (bool independent in new[] { false, true })
            {
                var factory = Factory(independent); var game = TransformerTests.New(factory);
                var source = Find(game, "assembler"); var receiver = Find(game, "lot.receiver"); var resource = game.World.Content.ResourceId("supply");
                var tx = game.World.Services.Get<IResourceTransaction>(); ulong hash = game.Hash();
                c.Equal(2, Lots(game, source).Count, "two actual partitions share one resource balance");
                c.Equal(Fix.FromInt(10), game.World.Entities.Get(source).Get<IInventory>().Amount(resource), "aggregate is the sum of the one lot buffer");
                c.Equal(Fix.FromInt(6), Lots(game, source, new Code("supply.purity")).Single().Amount, "engine quality selection excludes low grade");
                c.Equal(hash, game.Hash(), "lot and quality views are pure");
                c.That(!game.Seat(0).View.Feature<InventoryView>().TryGetLots(source, Code.None, out _, new Code("supply.purity"), Fix.One, Fix.Zero), "invalid player quality range refuses without faulting a session");
                c.Equal(hash, game.Hash(), "invalid quality query has no state or first-fault prefix");
                c.That(!game.Seat(1).View.Feature<InventoryView>().TryGetLots(source, Code.None, out var hidden) && hidden.Count == 0, "foreign lots expose no prefix");
                c.That(!tx.CommitMovement(Movement(game, source, Find(game, "plain.endpoint"), 1)).Committed, "endpoint without the lot contract cannot erase lot metadata");
                c.Equal(hash, game.Hash(), "incompatible lot destination refuses before claims and stock mutation");
                c.That(tx.CommitMovement(Movement(game, source, receiver, 4)).Committed, "existing transaction physically splits a lot between actual buffers");
                var retained = Lots(game, receiver); c.Equal(Fix.FromInt(4), retained.Single().Amount, "split retains original lot identity");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "split lots use the original save/hash path");
                foreach (var current in new[] { game, twin })
                {
                    var transactions = current.World.Services.Get<IResourceTransaction>();
                    c.That(transactions.CommitMovement(Movement(current, source, receiver, 2)).Committed, "second same-lot delivery merges in the same buffer");
                    c.That(transactions.CommitMovement(Movement(current, source, receiver, 2)).Committed, "different grade keeps a distinct partition");
                }
                c.Equal(game.Hash(), twin.Hash(), "saved twin performs identical partition changes");
                c.Equal(2, Lots(game, receiver).Count, "different lots do not silently blend");
                c.Equal(Fix.FromInt(6), Lots(game, receiver, new Code("supply.purity")).Single().Amount, "same-identity merge preserves authored purity");
                c.Equal(Fix.FromInt(4), retained.Single().Amount, "retained partition facts are detached");
                c.Equal(Fix.FromInt(2), Lots(game, source).Single().Amount, "only actual transferred material leaves source");
                hash = game.Hash(); c.That(!tx.CommitMovement(Movement(game, source, receiver, 20)).Committed, "short whole-set movement refuses");
                c.Equal(hash, game.Hash(), "refusal acquires no stock or metadata prefix");
                foreach (string invalid in new[] { "unknown-axis", "wrong-resource", "out-of-range" })
                    c.Throws<ContentException>(() => TransformerTests.New(Factory(independent, json => {
                        var lots = json.Get(independent ? "proof_resource_lots" : "resource_lots");
                        if (invalid == "unknown-axis") lots[0].Set("qualities", Json.Parse("{\"missing\":0.5}"));
                        else if (invalid == "wrong-resource") lots[0].Set("resource", JsonValue.Of("missing"));
                        else lots[0].Get("qualities").Set("supply.purity", JsonValue.Number("2"));
                    })), "invalid lot metadata refuses before game initialization: " + invalid);
            }
        }
        private static GameFactory LogisticsFactory(bool independent, string mode, Action<JsonValue> documentEdit = null)
        {
            var content = Content(independent); content.Set(independent ? "proof_resource_selections" : "resource_selections", Selections());
            if (mode == "stock") content.Set("logistics_policies", Json.Parse("[{\"id\":\"quality.destination\",\"stock\":{\"supply\":{\"enabled\":true,\"minimum\":6,\"desired\":6,\"maximum\":16,\"priority\":65,\"reserve_access\":\"protected\",\"selection\":\"supply.high\"}},\"outlet\":{\"enabled\":true,\"reserve\":{},\"reserve_access\":\"protected\",\"export_priority\":0,\"allowed_destinations\":null},\"states\":{\"disabled\":\"blocked\"}}]"));
            documentEdit?.Invoke(content);
            return ServiceDockTests.Factory(root => {
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.resource-lots"));
                var entries = JsonValue.NewArray(); for (int i = 0; i < 3; i++) entries.Add(root.Get("entity_setups")[i].Clone());
                StartupContentAuthoring.UseEntries(root, entries);
                for (int i = 0; i < 2; i++)
                {
                    var parts = root.Get("entities")[i].Get("components"); parts.Remove("point_stock"); var storage = Storage(false);
                    if (i == 0 || mode != "work") storage.Set("initial_lots", Json.Parse("[{\"lot\":\"lot.low\",\"amount\":4}]"));
                    if (i == 0) storage.Set("public", JsonValue.Of(true));
                    parts.Set("storage", storage);
                }
                var farther = root.Get("entities")[0].Clone(); farther.Set("id", JsonValue.Of("grade.source"));
                var high = Storage(false); high.Set("public", JsonValue.Of(true)); high.Set("initial_lots", Json.Parse("[{\"lot\":\"lot.high\",\"amount\":6}]"));
                farther.Get("components").Set("storage", high); farther.Get("components").Get("point_presence").Set("position", JsonValue.Number("20"));
                root.Get("entities").Add(farther);
                StartupContentAuthoring.Add(root, "grade.source.start", "grade.source", 0, Json.Parse("{\"space\":\"field.points\",\"position\":20}"));
                if (mode == "stock") root.Get("entities")[1].Get("components").Set("logistics_policy", Json.Parse("{\"profiles\":[{\"port\":null,\"profile\":\"quality.destination\"}]}"));
                if (mode == "work")
                {
                    root.Set("target_resource_requirements", Json.Parse("[{\"id\":\"dispatch.high\",\"port\":null,\"use\":\"consume\",\"quantities\":{\"supply\":6},\"lot_selections\":[\"supply.high\"]},{\"id\":\"dispatch.low\",\"port\":null,\"use\":\"hold\",\"quantities\":{\"supply\":4},\"lot_selections\":[\"supply.low\"]}]"));
                    root.Get("activities")[0].Set("automatic_supply", JsonValue.Of(true));
                    root.Get("activities")[0].Set("nodes", Json.Parse("[{\"id\":\"high\",\"executor\":\"work.timed\",\"duration\":{\"numerator\":1,\"denominator\":5,\"unit\":\"hour\"},\"requirement\":\"dispatch.high\"},{\"id\":\"low\",\"executor\":\"work.timed\",\"duration\":{\"numerator\":1,\"denominator\":1,\"unit\":\"hour\"},\"requirement\":\"dispatch.low\"}]"));
                }
            }, extraModules: new[] { new ResourceLotProofModule() }, extraDeclarations: "[" + Declaration + "]", extraDocuments: new Dictionary<string, string> { ["resource-lots.json"] = content.ToString() });
        }
        private static void UpdateLots(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("quality dispatch fault: " + game.Fault + "/" + twin?.Fault);
            if (twin != null && game.Hash() != twin.Hash()) throw new InvalidOperationException("quality dispatch twin divergence at " + game.Clock.StepIndex);
        }
        private static void QualityDispatch(Check c)
        {
            foreach (bool independent in new[] { false, true }) foreach (string mode in new[] { "manual", "stock", "route" })
            {
                var factory = LogisticsFactory(independent, mode); var game = ConstructionPlanTests.NewPoints(factory);
                var source = Find(game, "source"); var destination = Find(game, "destination"); var far = Find(game, "grade.source"); var worker = Find(game, "machine.a");
                var resource = game.World.Content.ResourceId("supply"); var selected = new Code("supply.high"); var view = game.Seat(0).View.Feature<LogisticsView>();
                var limits = new ReplayLimits(4 * 1024 * 1024, 3000); using var recorder = game.RecordReplay(limits); ulong initial = game.Hash();
                c.Equal(HaulStatus.NoSupply, view.PreviewHaul(source, destination, resource, Fix.FromInt(8), worker: worker, selection: selected).Status, "near low-grade source is not usable high-grade supply");
                var preview = view.PreviewHaul(far, destination, resource, Fix.FromInt(8), worker: worker, selection: selected);
                c.Equal(HaulStatus.Ready, preview.Status, "far compatible stock admits normal physical handling"); c.Equal(Fix.FromInt(6), preview.Amount, "quality preview caps load at actual eligible stock");
                c.Equal(selected, preview.Selection, "engine preview exposes exact selected intent"); c.Equal(initial, game.Hash(), "grade-specific source preview is pure");
                c.Equal(HaulStatus.NotFound, game.Seat(1).View.Feature<LogisticsView>().PreviewHaul(far, destination, resource, Fix.One, selection: selected).Status, "foreign player cannot inspect quality dispatch");
                if (mode == "manual")
                {
                    var command = new HaulResourcesCommand { Source = far, Destination = destination, Worker = worker, Resource = resource, Ceiling = Fix.FromInt(8), Priority = 65, Selection = selected };
                    c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary haul command agrees with quality preview"); c.That(TestKit.Order(game, 0, command).Accepted, "player uses existing haul command, not host filtering");
                }
                else if (mode == "route") c.That(TestKit.Order(game, 0, new AddRouteCommand { From = far, To = destination, Resource = resource, Settings = new LogisticsRouteSettings(true, Fix.FromInt(8), 65, selected) }).Accepted, "original route accepts selected intent");
                else
                {
                    var queue = new List<LogisticsDispatchOffer>(); view.CopyQueue(queue);
                    var offer = queue.Single(q => q.Destination == destination && q.Resource == resource);
                    c.Equal(Fix.FromInt(6), offer.Requested.Value, "wrong-grade destination stock cannot fill the selected JSON demand");
                    c.Equal(far, offer.Haul.Source, "automatic matching skips nearer wrong-grade stock"); c.Equal(selected, offer.Selection, "existing dispatch offer retains authored selection");
                    c.Equal(Code.None, view.ReadDiagnostics(destination, resource, out var diagnostic), "quality-aware diagnostics use original target query");
                    c.Equal(Fix.FromInt(6), diagnostic.UnmetDesired, "wrong-grade stock does not reduce on-hand quality shortage");
                }
                for (int i = 0; i < 200 && Lots(game, worker).Count == 0; i++) UpdateLots(game);
                c.That(Lots(game, worker).Count > 0, "truck physically begins incremental selected loading");
                c.That(Lots(game, worker).All(l => l.Lot == new Code("lot.high")), "cargo contains only the selected grade");
                c.Equal(Fix.FromInt(4), Store(game, destination).Amount(resource), "pickup does not teleport high-grade goods to destination");
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                c.That(jobs.Any(j => j.IsHaul && j.HaulSelection == selected && j.Source == far), "original job view preserves selected physical provenance");
                if (mode == "stock")
                {
                    var pending = new List<LogisticsDispatchOffer>(); view.CopyQueue(pending);
                    c.That(!pending.Any(q => q.Destination == destination && q.Selection == selected && q.Requested.GetValueOrDefault().IsPositive), "actual in-flight lot commitments prevent duplicate dispatch");
                    c.Equal(Code.None, view.ReadDiagnostics(destination, resource, out var diagnostic), "in-flight selected diagnostics remain available");
                    c.Equal(Fix.Zero, diagnostic.UnpromisedGap, "actual incoming grade covers dispatch gap without pretending it is already delivered");
                }
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial selected loading and JSON demand survive save");
                game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash(); UpdateLots(game, twin); c.Equal(paused, game.Hash(), "single paused game clock halts selected hauling"); game.Clock.Resume(); twin.Clock.Resume();
                for (int i = 0; i < 240 && Store(game, destination).Amount(resource) != Fix.FromInt(10); i++) UpdateLots(game, twin);
                c.Equal(Fix.FromInt(10), Store(game, destination).Amount(resource), "normal travel and unloading deliver six high-grade units beside four low-grade units");
                c.Equal(Fix.FromInt(6), Lots(game, destination, new Code("supply.purity")).Single().Amount, "delivered stock has the requested quality");
                c.Equal(Fix.FromInt(4), Store(game, source).Amount(resource), "near incompatible supply remains untouched"); c.Equal(Fix.Zero, Store(game, far).Amount(resource), "far compatible supply was physically depleted");
                var verified = new ReplayVerifier().Verify(factory, recorder.Finish(), limits); c.That(verified.Success, "selected command or automatic dispatch replays"); c.Equal(game.Hash(), verified.ActualHash, "selected dispatch replay reaches actual final state");
            }
            c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(LogisticsFactory(false, "stock", doc => doc.Get("logistics_policies")[0].Get("stock").Get("supply").Set("selection", JsonValue.Of("unknown.selection")))), "invalid authored stock quality reference refuses initialization");
        }
        private static void QualitySupply(Check c)
        {
            foreach (bool independent in new[] { false, true })
            {
                var factory = LogisticsFactory(independent, "work"); var game = ConstructionPlanTests.NewPoints(factory); var destination = Find(game, "destination"); var worker = Find(game, "machine.a"); var resource = game.World.Content.ResourceId("supply");
                var limits = new ReplayLimits(4 * 1024 * 1024, 3000); using var recorder = game.RecordReplay(limits);
                var posted = TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = destination }); c.That(posted.Accepted, "ordinary automatic activity selected");
                var order = game.World.Services.Get<WorkBoard>().Order(checked((int)posted.Arg0));
                for (int i = 0; i < 100 && game.World.Services.Get<LogisticsService>().Demands.Count == 0; i++) UpdateLots(game);
                var demands = new List<LogisticsDemandView>(); game.Seat(0).View.Feature<LogisticsView>().CopyDemands(demands);
                c.That(demands.Any(d => d.Settings.Selection == new Code("supply.high")) && demands.Any(d => d.Settings.Selection == new Code("supply.low")), "same workflow and resource retain distinct JSON grade requirements");
                for (int i = 0; i < 200 && Lots(game, worker).Count == 0; i++) UpdateLots(game);
                c.That(Lots(game, worker).Count > 0, "engine dispatches a real machine to supply selected work");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "grade-demand dispatch saves during actual loading");
                for (int i = 0; i < 440 && order.State != OrderState.Done; i++) UpdateLots(game, twin);
                c.Equal(OrderState.Done, order.State, "automatic multi-grade supply permits authored timed work to finish");
                c.Equal(Fix.FromInt(4), Store(game, destination).Amount(resource), "six qualified consumables spent while four low-grade held units survive");
                c.Equal(new Code("lot.low"), Lots(game, destination).Single().Lot, "held lot identity survives full supply and completion");
                c.Equal(Fix.Zero, Store(game, Find(game, "source")).Amount(resource), "low-grade tools were physically supplied"); c.Equal(Fix.Zero, Store(game, Find(game, "grade.source")).Amount(resource), "high-grade input was physically supplied");
                c.Equal(0, Store(game, destination).Reservations.Count, "completed work releases the original retained-grade claims");
                var verified = new ReplayVerifier().Verify(factory, recorder.Finish(), limits); c.That(verified.Success, "multi-grade automatic work replays"); c.Equal(game.Hash(), verified.ActualHash, "automatic selected work replay reaches final live hash");
            }
        }
        private static void Physical(Check c)
        {
            var content = Content(true);
            var factory = ServiceDockTests.Factory(root => {
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.resource-lots"));
                for (int i = 0; i < 2; i++)
                { var parts = root.Get("entities")[i].Get("components"); parts.Remove("point_stock"); parts.Set("storage", Storage(i == 0)); }
                var entries = root.Get("entity_setups");
                for (int i = 3; i < entries.Count; i++) entries[i].Get("placement").Set("position", JsonValue.Number((24 + i).ToString()));
            }, extraModules: new[] { new ResourceLotProofModule() }, extraDeclarations: "[" + Declaration + "]",
                extraDocuments: new Dictionary<string, string> { ["resource-lots.json"] = content.ToString() });
            var game = ConstructionPlanTests.NewPoints(factory); var source = Find(game, "source"); var destination = Find(game, "destination"); var worker = Find(game, "machine.a");
            var supply = game.World.Content.ResourceId("supply"); var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recorder = game.RecordReplay(limits);
            var command = new HaulResourcesCommand { Source = source, Destination = destination, Worker = worker, Resource = supply, Ceiling = Fix.FromInt(8), Priority = 50 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary haul preview admits real lot stock and capacity");
            game.Seat(0).Submit(command);
            for (int i = 0; i < 200 && Lots(game, worker).Count == 0; i++) { game.Update(); if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString()); }
            c.That(Lots(game, worker).Count > 0, "physical pickup begins a real lot partition over native handling time");
            c.That(Lots(game, destination).Count == 0, "pickup is not instant destination delivery");
            var twin = factory.Load(game.Save());
            for (int i = 0; i < 200 && Lots(game, destination).Sum(l => l.Amount.Raw) != Fix.FromInt(8).Raw; i++)
            { game.Update(); twin.Update(); if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("lot haul fault"); c.Equal(game.Hash(), twin.Hash(), "loaded saved twins retain physical lot identity"); }
            c.Equal(Fix.FromInt(8).Raw, Lots(game, destination).Sum(l => l.Amount.Raw), "ordinary loading travel and delivery move exactly eight units");
            c.Equal(2, Lots(game, destination).Count, "completed delivery preserves both original lot identities");
            c.Equal(Fix.FromInt(6), Lots(game, destination, new Code("supply.purity")).Single().Amount, "delivered high-grade partition preserves exact quality");
            c.Equal(Fix.FromInt(2), Lots(game, source).Single().Amount, "unhauled low-grade material stays at source");
            var verified = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(verified.Success, "normal player hauling replays through original jobs and transactions"); c.Equal(game.Hash(), verified.ActualHash, "lot replay reaches actual final live state");
        }
    }
}
