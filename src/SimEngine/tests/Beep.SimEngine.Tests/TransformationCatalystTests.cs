using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationInputPortTests;

namespace Beep.SimEngine.Tests
{
    // One public player driver. Content/native processors and actual endpoint books own every rule.
    public static class TransformationCatalystTests
    {
        public const string Case = "P8 catalyst disposition: exact partial receipts, timed batch wear and saved output/cancel waits";
        private const string External = "ports.external-factory", Recipe = "ports.catalyst-batch";
        private static readonly Code Process = new Code("process");
        private static readonly ResourceEndpointId Raw = new ResourceEndpointId("proof.pools", 4);
        private static readonly ResourceEndpointId Tool = new ResourceEndpointId("proof.pools", 7);
        public static void Register() => TestRunner.Add(Case, Run);
        private static TransformationSnapshot Current(SimGame game) => State(game, External);
        private static void Queue(Check c, SimGame game) => Order(c, game, new EnqueueTransformationCommand {
            Transformer = Find(game, External), Capability = Process, Transformation = Recipe, Count = 1 });
        private static void Enable(Check c, SimGame game, bool enabled) => Order(c, game, new ConfigureOperationCommand {
            Entity = Find(game, External), Operation = Process, Enabled = enabled, Target = Fix.One });
        private static void Stop(Check c, SimGame game) => Order(c, game, new CancelTransformationCommand {
            Transformer = Find(game, External), Capability = Process, Index = 0 });
        private static void Route(Check c, SimGame game, long pool) => Order(c, game, new SelectResourcePortCommand {
            Entity = Find(game, External), Port = new Code("raw"), Endpoint = new ResourceEndpointId("proof.pools", pool) });
        private static ResourceFlowPage History(SimGame game) => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0);
        private static void Consumed(Check c, SimGame game, string resource, Fix amount) => c.Equal(new BigInteger(amount.Raw),
            History(game).Totals(Resource(game, resource)).ConsumedRaw, "actual consumption accounting for " + resource);
        private static IReadOnlyList<ResourceReservation> SourceClaims(SimGame game)
        {
            if (!game.Seat(0).View.Feature<InventoryView>().TryGetReservations(Find(game, "ports.source"), Code.None, out var records))
                throw new InvalidOperationException("missing private actual source receipts");
            return records;
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory); var half = Fix.Parse("0.5");
            var limits = new ReplayLimits(2 * 1024 * 1024, 500); var recorder = game.RecordReplay(limits);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build, "breaking completion-disposition engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat, "pending conversion command has the new explicit disposition codec");
            c.That(!game.Modules.Contains("sim.tycoon") && game.Seat(0).Snapshot.Space == null, "catalyst processing requires no business/platform map");
            c.That(!game.Seat(0).View.Feature<InventoryView>().TryGetCapacity(Find(game, External), Code.None, out _), "external processor has no local stock fallback");
            var view = game.Seat(0).View.Feature<TransformationManagementView>(); ulong hash = game.Hash();
            c.Equal(Code.None, view.ReadToolConsumption(Find(game, External), Process, Recipe, out var wear), "authored wear is an engine-owned management preview");
            c.Equal(half, wear.AmountOf(Resource(game, "ports.ore")), "authored fractional completion wear uses native mass units");
            c.Equal(Fix.Zero, wear.AmountOf(Resource(game, "ports.jig")), "fully reusable fixture explicitly has no consumption");
            c.Equal(hash, game.Hash(), "wear preview changes no stock, promises, progress or RNG");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<TransformationManagementView>().ReadToolConsumption(Find(game, External), Process, Recipe, out var hidden),
                "foreign wear preview refused");
            c.That(hidden.IsEmpty, "foreign preview reveals no policy bill");
            Prime(c, game, Fix.FromInt(3), half);
            Transfer(c, game, ResourceEndpointId.ForEntity(Find(game, "ports.source")), Tool, "ports.jig", Fix.One);
            Queue(c, game);
            var active = Current(game).Tools;
            c.Equal(1L, Current(game).Progress.WholeTicks, "startup earns one actual work interval, not completion wear");
            c.Equal(Fix.FromInt(2), active.Held.AmountOf(Resource(game, "ports.ore")), "original actual reusable catalyst receipt holds full required charge");
            c.Equal(Fix.FromInt(2), PortStock(game, "ports.ore", true), "active catalyst still exists at original stock endpoint");
            Consumed(c, game, "ports.ore", Fix.Zero);
            var twin = factory.Load(game.Save()); Step(game, twin); Step(game, twin);
            c.Equal(0, Current(game).QueueCount, "three earned intervals finish ordinary native batch");
            c.Equal(Fix.Parse("1.5"), PortStock(game, "ports.ore", true), "completion consumes only authored catalyst half and releases actual remainder");
            c.Equal(Fix.FromInt(2), PortStock(game, "ports.alloy", false), "same conversion commits complete solid result");
            c.Equal(Fix.One, PortStock(game, "ports.water", false), "same conversion commits complete liquid result");
            c.That(Current(game).Tools.Held.IsEmpty, "completion retires original receipts rather than fabricating a returned tool");
            Consumed(c, game, "ports.ore", Fix.Parse("1.5")); Consumed(c, game, "ports.jig", Fix.Zero);
            c.Equal(game.Hash(), twin.Hash(), "saved working completion has identical stock/receipt/history state");
            c.Equal(Fix.FromInt(2), active.Held.AmountOf(Resource(game, "ports.ore")), "earlier actual hold facts remain immutable after wear/release");

            Prime(c, game, Fix.Parse("1.5"), half, twin); Queue(c, game); Queue(c, twin);
            Enable(c, game, false); Enable(c, twin, false); Route(c, game, 6); Route(c, twin, 6);
            c.Equal(Raw, Current(game).Tools.Sources.Single(source => source.Resource == Resource(game, "ports.ore")).Endpoint,
                "current port selection cannot relocate active catalyst authority");
            Prime(c, game, Fix.FromInt(2), Fix.Zero, twin);
            Stop(c, game); Stop(c, twin);
            c.Equal(TransformerManager.BlockerReturnFull, Current(game).Blocker, "full original feed return delays complete cancellation");
            c.Equal(Fix.FromInt(2), Current(game).Tools.Held.AmountOf(Resource(game, "ports.ore")), "return wait retains entire original catalyst hold without wear");
            Consumed(c, game, "ports.ore", Fix.Parse("1.5"));
            twin = factory.Load(game.Save()); var sink = ResourceEndpointId.ForEntity(Find(game, "ports.sink-entity"));
            Transfer(c, game, Raw, sink, "ports.ore", Fix.FromInt(2), twin);
            c.That(Current(game).QueueCount == 0 && Current(game).Tools.Held.IsEmpty, "whole original return then releases full catalyst/tools once");
            c.Equal(Fix.FromInt(3), PortStock(game, "ports.ore", true), "cancel returns only original raw feed and unconsumed charge");
            Consumed(c, game, "ports.ore", Fix.Parse("1.5"));
            c.Equal(game.Hash(), twin.Hash(), "saved changed-port full return is identical and applies no completion wear");
            Route(c, game, 4); Route(c, twin, 4); Enable(c, game, true); Enable(c, twin, true);
            Queue(c, game); Queue(c, twin); Step(game, twin); Step(game, twin);
            c.Equal(TransformationPhase.Blocked, Current(game).Phase, "full output retains completed earned batch");
            c.Equal(3L, Current(game).Progress.WholeTicks, "output wait preserves complete earned duration");
            c.Equal(Fix.FromInt(2), Current(game).Tools.Held.AmountOf(Resource(game, "ports.ore")), "output wait does not pre-consume catalyst");
            Consumed(c, game, "ports.ore", Fix.Parse("1.5"));
            twin = factory.Load(game.Save());
            Transfer(c, game, new ResourceEndpointId("proof.pools", 1), sink, "ports.alloy", Fix.FromInt(2), twin);
            c.Equal(Fix.FromInt(2), Current(game).Tools.Held.AmountOf(Resource(game, "ports.ore")), "one ready output cannot authorize a catalyst/raw-input prefix");
            c.Equal(Fix.One, Current(game).Wip.Stock.AmountOf(Resource(game, "ports.ore")), "partial output capacity preserves protected raw WIP");
            Transfer(c, game, new ResourceEndpointId("proof.pools", 2), sink, "ports.water", Fix.One, twin);
            c.Equal(0, Current(game).QueueCount, "complete output admission commits one actual wear/result conversion");
            c.Equal(Fix.Parse("1.5"), PortStock(game, "ports.ore", true), "saved output completion releases exact original remaining catalyst");
            Consumed(c, game, "ports.ore", Fix.FromInt(3)); Consumed(c, game, "ports.water", Fix.One); Consumed(c, game, "ports.jig", Fix.Zero);
            c.Equal(game.Hash(), twin.Hash(), "saved full-output wait continues with identical once-only completion wear");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success, "native catalyst player replay verifies: " + replay.Detail);
            c.Equal(game.Hash(), replay.ActualHash, "replay includes exact original resource/tool/catalyst disposition");
            PartialReceipts(c, factory);
            foreach (string invalid in new[] { "missing", "overheld", "unheld", "negative", "discrete", "continuous" })
                c.Throws<ContentException>(() => New(Factory(json => {
                    var recipe = json.Get("transformations")[6]; var consumption = recipe.Get("tool_consumption");
                    if (invalid == "missing") recipe.Remove("tool_consumption");
                    if (invalid == "overheld") consumption.Set("ports.ore", JsonValue.Number("3"));
                    if (invalid == "unheld") consumption.Set("ports.water", JsonValue.Number("0.25"));
                    if (invalid == "negative") consumption.Set("ports.ore", JsonValue.Number("-0.5"));
                    if (invalid == "discrete") consumption.Set("ports.jig", JsonValue.Number("0.5"));
                    if (invalid == "continuous") json.Get("transformations")[5].Set("tool_consumption", Json.Parse("{\"ports.ore\":0.5}"));
                })), "invalid completion disposition refuses before Update: " + invalid);
        }
        private static void PartialReceipts(Check c, GameFactory factory)
        {
            var game = New(factory); var owner = new ReservationOwner("proof.catalyst-disposition", 100); var company = new PlayerId(0);
            var source = ResourceEndpointId.ForEntity(Find(game, "ports.source")); var half = Fix.Parse("0.5"); var quarter = Fix.Parse("0.25");
            var limits = new ReplayLimits(2 * 1024 * 1024, 200); var recorder = game.RecordReplay(limits);
            Order(c, game, new ClaimProbeCommand { Intents = new[] { new ResourceClaimIntent(ReservationDirection.Outbound, owner, source,
                new Bill(new[] { new ResourceAmount(Resource(game, "ports.ore"), Fix.FromInt(2)),
                    new ResourceAmount(Resource(game, "ports.water"), Fix.One), new ResourceAmount(Resource(game, "ports.jig"), Fix.One) })) } });
            var original = SourceClaims(game); var ore = original.Single(record => record.Resource == Resource(game, "ports.ore"));
            var water = original.Single(record => record.Resource == Resource(game, "ports.water")); var jig = original.Single(record => record.Resource == Resource(game, "ports.jig"));
            var cause = new Code("proof.catalyst-conversion");
            foreach (string invalid in new[] { "negative", "overheld", "duplicate", "stale", "owner", "noop", "discrete" })
            {
                var rows = new[] { new ResourceConversionInput(ResourceFlowKind.Consumed, ore, half, true) }; var selectedOwner = owner;
                if (invalid == "negative") rows[0] = new ResourceConversionInput(ResourceFlowKind.Consumed, ore, -half, true);
                if (invalid == "overheld") rows[0] = new ResourceConversionInput(ResourceFlowKind.Consumed, ore, Fix.FromInt(3), true);
                if (invalid == "duplicate") rows = new[] { rows[0], rows[0] };
                if (invalid == "stale") rows[0] = new ResourceConversionInput(ResourceFlowKind.Consumed, new ResourceReservation(ore.Token, ore.Owner, ore.Resource, Fix.One, ore.Direction), half, true);
                if (invalid == "owner") selectedOwner = new ReservationOwner("proof.catalyst-disposition", 101);
                if (invalid == "noop") rows[0] = new ResourceConversionInput(ResourceFlowKind.Consumed, ore, Fix.Zero, false);
                if (invalid == "discrete") rows[0] = new ResourceConversionInput(ResourceFlowKind.Consumed, jig, quarter, true);
                var command = new ConversionProbeCommand { Intent = new ResourceConversionIntent(selectedOwner, company, rows,
                    Array.Empty<ResourceProductionIntent>(), cause) };
                ulong hash = game.Hash();
                c.Equal(Codes.Blocked, game.Seat(0).Preview(command), "invalid actual disposition preview refuses: " + invalid);
                c.Equal(hash, game.Hash(), "refused disposition preview is pure: " + invalid);
                c.That(!TestKit.Order(game, 0, command).Accepted && game.Fault == null, "ordinary command refuses malformed disposition without a mutation fault: " + invalid);
                c.Equal(3, SourceClaims(game).Count, "refused disposition preserves all actual original receipts");
            }
            var inputs = new[] { new ResourceConversionInput(ResourceFlowKind.Consumed, ore, half, false), new ResourceConversionInput(ResourceFlowKind.Consumed, water, quarter, true),
                new ResourceConversionInput(ResourceFlowKind.Consumed, jig, Fix.Zero, true) };
            var conversion = new ResourceConversionIntent(owner, company, inputs, new[] { new ResourceProductionIntent(new ResourceEndpointId("proof.pools", 1),
                new Bill(new[] { new ResourceAmount(Resource(game, "ports.alloy"), Fix.One) })) }, cause);
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(new ConversionProbeCommand { Intent = conversion }), "whole partial consumption/release/output preview accepts");
            c.Equal(before, game.Hash(), "successful disposition preview leaves actual records untouched");
            game.Seat(0).Submit(new ConversionProbeCommand { Intent = conversion }); var twin = factory.Load(game.Save()); Step(game, twin);
            c.Equal(game.Hash(), twin.Hash(), "pending command saves original receipt, exact wear and remainder disposition");
            var remaining = SourceClaims(game).Single();
            c.That(remaining.Token.Equals(ore.Token) && remaining.Owner.Equals(ore.Owner), "partial preserved remainder stays in the same actual original receipt");
            c.Equal(Fix.Parse("1.5"), remaining.Amount, "actual endpoint book reduces protected remainder exactly");
            c.Equal(Fix.Parse("23.5"), Stock(game, "ports.ore", "ports.source"), "partial actual take consumes only half from physical source");
            c.Equal(Fix.Parse("5.75"), Stock(game, "ports.water", "ports.source"), "water remainder release never refunds its consumed quarter");
            c.Equal(Fix.FromInt(4), Stock(game, "ports.jig", "ports.source"), "release-only fixture changes no physical stock");
            c.Equal(3, original.Count, "old copied actual receipt facts remain unchanged after disposition");
            twin = factory.Load(game.Save());
            ResourceConversionIntent Finish() => new ResourceConversionIntent(owner, company,
                new[] { new ResourceConversionInput(ResourceFlowKind.Consumed, remaining, half, true) }, Array.Empty<ResourceProductionIntent>(), cause);
            Order(c, game, new ConversionProbeCommand { Intent = Finish() }); Order(c, twin, new ConversionProbeCommand { Intent = Finish() });
            c.Equal(0, SourceClaims(game).Count, "later exact actual remainder consumption and release retires the original claim");
            Consumed(c, game, "ports.ore", Fix.One); Consumed(c, game, "ports.water", quarter); Consumed(c, game, "ports.jig", Fix.Zero);
            c.Equal(game.Hash(), twin.Hash(), "saved partially held remainder completes identically without a copied ledger");
            c.Equal(Codes.Blocked, game.Seat(0).Preview(new ConversionProbeCommand { Intent = conversion }), "spent original receipt cannot be reused");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success, "explicit partial disposition player replay verifies: " + replay.Detail);
            c.Equal(game.Hash(), replay.ActualHash, "replay preserves actual partial promises and history");
        }
    }
}
