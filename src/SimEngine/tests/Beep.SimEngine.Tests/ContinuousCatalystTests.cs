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
    // Thin player: ordinary commands, immutable engine facts and Update. No live stock/state writes.
    public static class ContinuousCatalystTests
    {
        public const string Case = "P8 continuous catalysts: authored earned-work wear, original-source replenishment and saved waits";
        private const string External = "ports.external-factory", Recipe = "ports.catalyst-flow";
        private static readonly Code Process = new Code("process");
        private static readonly ResourceEndpointId Raw = new ResourceEndpointId("proof.pools", 4), Alternate = new ResourceEndpointId("proof.pools", 6),
            Tool = new ResourceEndpointId("proof.pools", 7), Utility = new ResourceEndpointId("proof.pools", 9);
        public static void Register() => TestRunner.Add(Case, Run);
        private static TransformationSnapshot Current(SimGame game) => State(game, External);
        private static void Queue(Check c, SimGame game) => Order(c, game, new EnqueueTransformationCommand {
            Transformer = Find(game, External), Capability = Process, Transformation = Recipe, Count = 1 });
        private static ConfigureOperationCommand Control(SimGame game, bool enabled, Fix target) => new ConfigureOperationCommand {
            Entity = Find(game, External), Operation = Process, Enabled = enabled, Target = target };
        private static void Enable(Check c, SimGame game, bool enabled, Fix target, SimGame twin = null)
        { Order(c, game, Control(game, enabled, target)); if (twin != null) Order(c, twin, Control(twin, enabled, target)); }
        private static void Fill(Check c, SimGame game, ResourceEndpointId destination, string resource, Fix amount, SimGame twin = null)
            => Transfer(c, game, ResourceEndpointId.ForEntity(Find(game, "ports.source")), destination, resource, amount, twin);
        private static Fix UtilityStock(SimGame game) => Current(game).Operating.Ports.Single(row => row.Resource == Resource(game, "ports.water"))
            .Stock.AmountOf(Resource(game, "ports.water"));
        private static void Consumed(Check c, SimGame game, string resource, Fix amount) => c.Equal(new BigInteger(amount.Raw),
            game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Totals(Resource(game, resource)).ConsumedRaw,
            "actual native consumption of " + resource);
        private static ResourceReservation OreHold(SimGame game) => Current(game).Tools.Receipts.Single(row => row.Resource == Resource(game, "ports.ore"));
        private static void Charge(Check c, SimGame game, ReservationToken token, Fix wear)
        {
            c.Equal(token, OreHold(game).Token, "running draw retains original actual catalyst token");
            c.Equal(Fix.FromInt(2), OreHold(game).Amount, "running draw retains full original catalyst charge");
            c.Equal(wear, Current(game).Flow.ToolConsumed.AmountOf(Resource(game, "ports.ore")), "earned progress derives exact cumulative wear");
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory); var half = Fix.Parse("0.5"); var quarter = Fix.Parse("0.25");
            var limits = new ReplayLimits(2 * 1024 * 1024, 600); var recorder = game.RecordReplay(limits);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build, "continuous catalyst execution identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat, "native state layouts remain unchanged");
            c.That(!game.Modules.Contains("sim.tycoon") && game.Seat(0).Snapshot.Space == null, "wear requires neither business nor platform world");
            c.That(!game.Seat(0).View.Feature<InventoryView>().TryGetCapacity(Find(game, External), Code.None, out _), "processor has no hidden local inventory fallback");
            var view = game.Seat(0).View.Feature<TransformationManagementView>(); ulong hash = game.Hash();
            c.Equal(Code.None, view.ReadToolConsumptionRates(Find(game, External), Process, Recipe, out var rates), "private authored rate preview accepts");
            c.Equal(1, rates.Count, "one explicit continuous catalyst rate"); c.Equal(half, rates[0].Quantity, "native rate quantity");
            c.Equal(2L, rates[0].PeriodNumerator, "authored two-second wear period");
            c.Throws<NotSupportedException>(() => ((IList<ResourceRateInfo>)rates).Clear(), "authored rate facts are immutable");
            c.Equal(hash, game.Hash(), "authored preview changes no state or clock");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<TransformationManagementView>().ReadToolConsumptionRates(Find(game, External), Process, Recipe, out var hidden),
                "foreign authored wear preview refused"); c.Equal(0, hidden.Count, "foreign view reveals no rate rows");
            Queue(c, game); Prime(c, game, Fix.Parse("2.5"), half); Fill(c, game, Tool, "ports.jig", Fix.One);
            Fill(c, game, Utility, "ports.water", half);
            c.Equal(0L, Current(game).Wip.Execution, "full original charge plus feed/wear shortage creates no execution prefix");
            c.That(Current(game).Tools.Held.IsEmpty && Current(game).Progress.Equals(WorkProgress.Zero), "startup admits no partial charge/work");
            c.Equal(half, UtilityStock(game), "ready utility remains physically unconsumed during startup shortage");
            Consumed(c, game, "ports.ore", Fix.Zero);
            Prime(c, game, half, Fix.Zero);
            c.Equal(1L, Current(game).Progress.WholeTicks, "first complete authored startup earns actual work");
            var token = OreHold(game).Token; var initial = Current(game).Flow;
            Charge(c, game, token, quarter);
            c.Equal(Fix.Parse("2.25"), PortStock(game, "ports.ore", true), "same endpoint admits base charge plus feed and wear, then consumes only draw");
            c.Equal(half, Current(game).Flow.Consumed.AmountOf(Resource(game, "ports.ore")), "feed facts remain distinct from catalyst wear");
            Consumed(c, game, "ports.ore", Fix.Parse("0.75"));
            Step(game); c.Equal(TransformerManager.BlockerFlowInputs, Current(game).Blocker, "unpromised stock shortage blocks next whole draw");
            c.Equal(1L, Current(game).Progress.WholeTicks, "starved work earns no progress"); Charge(c, game, token, quarter);
            c.Equal(quarter, UtilityStock(game), "starvation consumes no ready utility prefix");
            var twin = factory.Load(game.Save()); Enable(c, game, false, half, twin); Prime(c, game, half, Fix.Zero, twin);
            c.Equal(game.Hash(), twin.Hash(), "saved shortage retains original charge while paused replenishment arrives");
            game.Seat(0).Submit(Control(game, true, half)); twin = factory.Load(game.Save()); Step(game, twin);
            Charge(c, game, token, Fix.Parse("0.375"));
            c.Equal(Fix.Parse("2.375"), PortStock(game, "ports.ore", true), "half work-rate draw consumes half feed/wear, not a full charge");
            c.Equal(Fix.Zero, UtilityStock(game), "elapsed utility draw remains one actual interval at half work target");
            c.Equal(game.Hash(), twin.Hash(), "saved control command preserves fractional work and replenished receipts");
            Enable(c, game, false, half, twin);
            void Route(SimGame session) => Order(c, session, new SelectResourcePortCommand { Entity = Find(session, External), Port = new Code("raw"), Endpoint = new ResourceEndpointId("proof.pools", 6) });
            Route(game); Route(twin);
            c.Equal(Alternate, Current(game).Flow.InputPorts.Single(row => row.Resource == Resource(game, "ports.ore")).Endpoint,
                "new feed follows current player selection");
            c.Equal(Raw, Current(game).Flow.ToolPorts.Single(row => row.Resource == Resource(game, "ports.ore")).Endpoint,
                "original catalyst wear and replenishment stay at original endpoint");
            Fill(c, game, Alternate, "ports.ore", half, twin); Prime(c, game, Fix.Zero, Fix.Parse("0.375"), twin);
            Fill(c, game, Utility, "ports.water", half, twin);
            c.Equal(Fix.Parse("0.375"), Current(game).Flow.ToolConsumed.AmountOf(Resource(game, "ports.ore")), "paused deliveries and changed selections cause no wear");
            Enable(c, game, true, half, twin); Step(game, twin);
            Charge(c, game, token, Fix.Parse("0.625"));
            c.Equal(Fix.Parse("2.125"), PortStock(game, "ports.ore", true), "original store loses only earned catalyst wear after feed selection changes");
            c.Equal(game.Hash(), twin.Hash(), "saved originals coexist with new live feed bindings");
            Step(game, twin); Charge(c, game, token, Fix.Parse("0.625"));
            c.Equal(TransformerManager.BlockerFlowInputs, Current(game).Blocker, "missing fresh feed/utility leaves original replenishment unchanged");
            Enable(c, game, false, Fix.FromInt(2), twin);
            Fill(c, game, Raw, "ports.ore", Fix.Parse("0.875"), twin); Fill(c, game, Alternate, "ports.ore", Fix.FromInt(2), twin);
            Prime(c, game, Fix.Zero, Fix.Parse("0.75"), twin); Fill(c, game, Utility, "ports.water", Fix.Parse("0.75"), twin);
            game.Seat(0).Submit(Control(game, true, Fix.FromInt(2))); twin = factory.Load(game.Save()); Step(game, twin);
            c.Equal(TransformerManager.BlockerOutput, Current(game).Blocker, "higher target blocked by aggregate actual output capacity");
            Charge(c, game, token, Fix.Parse("0.625")); c.Equal(Fix.FromInt(3), PortStock(game, "ports.ore", true), "output wait acquires/consumes no original growth prefix");
            c.Equal(Fix.Parse("0.75"), UtilityStock(game), "output wait consumes no utility prefix");
            var sink = ResourceEndpointId.ForEntity(Find(game, "ports.sink-entity"));
            Transfer(c, game, new ResourceEndpointId("proof.pools", 1), sink, "ports.alloy", half, twin);
            Charge(c, game, token, Fix.Parse("0.625"));
            c.Equal(TransformerManager.BlockerOutput, Current(game).Blocker, "ready solid outlet cannot bypass full liquid result");
            twin = factory.Load(game.Save()); Transfer(c, game, new ResourceEndpointId("proof.pools", 2), sink, "ports.water", quarter, twin);
            Charge(c, game, token, Fix.Parse("1.125")); c.Equal(4L, Current(game).Progress.WholeTicks, "whole mixed conversion earns two more work intervals");
            c.Equal(Fix.Parse("2.5"), PortStock(game, "ports.ore", true), "complete output consumes only added original catalyst wear");
            c.Equal(Fix.Parse("1.75"), PortStock(game, "ports.alloy", false), "complete native solid output");
            c.Equal(Fix.Parse("0.875"), PortStock(game, "ports.water", false), "complete native liquid output");
            c.Equal(game.Hash(), twin.Hash(), "saved output wait resumes same original claims and real progress");
            c.Equal(quarter, initial.ToolConsumed.AmountOf(Resource(game, "ports.ore")), "earlier copied wear facts remain unchanged");
            Enable(c, game, false, Fix.FromInt(2), twin);
            void Stop(SimGame session) => Order(c, session, new CancelTransformationCommand { Transformer = Find(session, External), Capability = Process, Index = 0 });
            Stop(game); Stop(twin);
            c.That(Current(game).QueueCount == 0 && Current(game).Tools.Held.IsEmpty, "ordinary stop releases original remaining charge without another conversion");
            c.Equal(Fix.Parse("2.5"), PortStock(game, "ports.ore", true), "stop never refunds consumed catalyst/feed");
            Consumed(c, game, "ports.ore", Fix.Parse("3.375")); Consumed(c, game, "ports.water", Fix.Parse("2.375")); Consumed(c, game, "ports.jig", Fix.Zero);
            c.Equal(game.Hash(), twin.Hash(), "saved continuous stop has no repeated wear/cost/refund");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success, "original-source wear player replay verifies: " + replay.Detail); c.Equal(game.Hash(), replay.ActualHash, "replay retains original catalyst authority and history");
            DiscreteWear(c);
            foreach (string bad in new[] { "missing", "old-shape", "unheld", "negative", "period", "duplicate", "discrete" })
                c.Throws<ContentException>(() => New(Factory(json => {
                    var recipe = json.Get("transformations")[7]; var row = recipe.Get("tool_consumption")[0];
                    if (bad == "missing") recipe.Remove("tool_consumption");
                    if (bad == "old-shape") recipe.Set("tool_consumption", Json.Parse("{}"));
                    if (bad == "unheld") row.Set("resource", JsonValue.Of("ports.water"));
                    if (bad == "negative") row.Set("quantity", JsonValue.Number("-0.5"));
                    if (bad == "period") row.Get("period").Set("denominator", JsonValue.Number("0"));
                    if (bad == "duplicate") recipe.Set("tool_consumption", Json.Parse("[" + row + "," + row + "]"));
                    if (bad == "discrete") row.Set("resource", JsonValue.Of("ports.jig"));
                })), "strict continuous wear authoring refuses before Update: " + bad);
        }
        private static void DiscreteWear(Check c)
        {
            var factory = Factory(json => json.Get("transformations")[7].Set("tool_consumption", Json.Parse(
                "[{\"resource\":\"ports.ore\",\"quantity\":0.5,\"period\":{\"numerator\":2,\"denominator\":1,\"unit\":\"second\"}}," +
                "{\"resource\":\"ports.jig\",\"quantity\":1,\"period\":{\"numerator\":3,\"denominator\":1,\"unit\":\"second\"}}]")));
            var game = New(factory); var half = Fix.Parse("0.5"); Enable(c, game, false, half);
            Prime(c, game, Fix.FromInt(4), Fix.One); Fill(c, game, Utility, "ports.water", Fix.One); Fill(c, game, Tool, "ports.jig", Fix.FromInt(3));
            Queue(c, game); Enable(c, game, true, half);
            var jig = Current(game).Tools.Receipts.Single(row => row.Resource == Resource(game, "ports.jig"));
            c.Equal(Fix.One, Current(game).Flow.ToolConsumed.AmountOf(jig.Resource), "discrete wear rounds cumulative earned fraction upward once");
            var twin = factory.Load(game.Save()); Step(game, twin);
            c.Equal(Fix.One, Current(game).Flow.ToolConsumed.AmountOf(jig.Resource), "second fractional interval does not repeat discrete rounding wear");
            c.Equal(Fix.One, Current(game).Tools.Receipts.Single(row => row.Token.Equals(jig.Token)).Amount, "discrete original charge retains same actual token");
            Consumed(c, game, "ports.jig", Fix.One); c.Equal(game.Hash(), twin.Hash(), "saved discrete catalyst rounding derives only from progress");
        }
    }
}
