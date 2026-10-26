using System;
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
    // Shared thin player helpers; selected JSON and original engine own tools, time and actual books.
    public static class TransformationToolPortTests
    {
        public const string Case = "P8 tool ports: whole startup, original reusable holds and saved batch/flow release";
        public static void Register() => TestRunner.Add(Case, Run);
        private static readonly ResourceEndpointId OriginalTool = new ResourceEndpointId("proof.pools", 7);
        private static readonly ResourceEndpointId Raw = new ResourceEndpointId("proof.pools", 4);
        private static readonly ResourceEndpointId Water = new ResourceEndpointId("proof.pools", 5);
        private static void SelectTool(Check c, SimGame game, long pool) => Order(c, game,
            new SelectResourcePortCommand { Entity = Find(game, "ports.factory"), Port = new Code("tool"), Endpoint = new ResourceEndpointId("proof.pools", pool) });
        private static void SupplyTool(Check c, SimGame game, SimGame twin = null)
            => Transfer(c, game, ResourceEndpointId.ForEntity(Find(game, "ports.source")), OriginalTool, "ports.jig", Fix.One, twin);
        private static void RefuseToolTake(Check c, SimGame game, ResourceEndpointId source, string resource, Fix amount)
        {
            var command = new TransferProbeCommand { Move = true, Movements = new[] {
                new ResourceMovementIntent(new ReservationOwner("proof.tool-steal", 901), new PlayerId(0), source,
                    ResourceEndpointId.ForEntity(Find(game, "ports.sink-entity")),
                    new Bill(new[] { new ResourceAmount(Resource(game, resource), amount) }), Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain) } };
            ulong hash = game.Hash();
            c.Equal(Codes.Blocked, game.Seat(0).Preview(command), "unrelated player transfer cannot spend protected tools");
            c.Equal(hash, game.Hash(), "refused preview changes no actual promise, stock or history");
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory);
            var limits = new ReplayLimits(2 * 1024 * 1024, 500); var recorder = game.RecordReplay(limits);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build, "current tool-route engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat, "original tool bindings join sole execution codec");
            var view = game.Seat(0).View.Feature<TransformationManagementView>();
            ulong initialHash = game.Hash();
            c.Equal(Code.None, view.ReadToolPorts(Find(game, "ports.factory"), new Code("process"), "ports.tool-batch", out var selected), "authored reusable ports are engine-visible");
            c.Equal(2, selected.Count, "both tool resources have exact native routes");
            c.Equal(initialHash, game.Hash(), "tool selection view is pure");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<TransformationManagementView>().ReadToolPorts(Find(game, "ports.factory"),
                new Code("process"), "ports.tool-batch", out var hidden), "foreign reusable port selection refused");
            c.Equal(0, hidden.Count, "foreign view reveals no endpoints");
            Enqueue(c, game, "ports.tool-batch");
            c.Equal(TransformerManager.BlockerTools, State(game).Blocker, "remote tool absence blocks despite spare local tools");
            Prime(c, game, Fix.FromInt(3), Fix.One);
            SupplyTool(c, game);
            c.Equal(WorkSystem.BlockerMaterials, State(game).Blocker, "feed/tool same-resource sum is admitted as one actual bill");
            c.That(State(game).Wip.Stock.IsEmpty && State(game).Tools.Receipts.Count == 0, "failed whole startup acquires no prefix");
            var twin = factory.Load(game.Save());
            Transfer(c, game, ResourceEndpointId.ForEntity(Find(game, "ports.source")), Raw, "ports.ore", Fix.One, twin);
            var active = State(game);
            c.Equal(1L, active.Progress.WholeTicks, "successful startup earns one authored second, not instant completion");
            c.Equal(Fix.FromInt(2), active.Wip.Stock.AmountOf(Resource(game, "ports.ore")), "only raw feed moves into existing protected WIP");
            c.Equal(Fix.FromInt(2), active.Tools.Held.AmountOf(Resource(game, "ports.ore")), "same-source native receipt retains exact reusable remainder");
            c.Equal(Fix.One, active.Tools.Held.AmountOf(Resource(game, "ports.jig")), "remote jig is held at its original endpoint");
            c.Equal(2, active.Tools.Receipts.Count, "actual books own exactly two reusable receipts");
            c.That(active.Tools.Sources.Any(source => source.Endpoint == OriginalTool), "saved execution references original remote tool");
            c.Equal(game.Hash(), twin.Hash(), "saved supplying startup converges through actual mixed feed/tool acquisition");
            Control(c, game, false); Control(c, twin, false);
            SelectTool(c, game, 8); SelectTool(c, twin, 8);
            c.Equal(OriginalTool, State(game).Tools.Sources.Single(source => source.Resource == Resource(game, "ports.jig")).Endpoint,
                "changing selected port never substitutes an active reusable commitment");
            RefuseToolTake(c, game, OriginalTool, "ports.jig", Fix.One);
            RefuseToolTake(c, game, Raw, "ports.ore", Fix.FromInt(3));
            Prime(c, game, Fix.FromInt(2), Fix.One, twin);
            Cancel(c, game); Cancel(c, twin);
            c.Equal(TransformerManager.BlockerReturnFull, State(game).Blocker, "full original feed destinations delay whole cancellation");
            c.Equal(2, State(game).Tools.Receipts.Count, "return wait retains all reusable holds");
            twin = factory.Load(game.Save());
            var sink = ResourceEndpointId.ForEntity(Find(game, "ports.sink-entity"));
            Transfer(c, game, Raw, sink, "ports.ore", Fix.FromInt(2), twin);
            c.Equal(TransformerManager.BlockerReturnFull, State(game).Blocker, "one freed destination cannot release tools or return half WIP");
            Transfer(c, game, Water, sink, "ports.water", Fix.One, twin);
            c.That(State(game).QueueCount == 0 && State(game).Tools.Sources.Count == 0 && State(game).Tools.Receipts.Count == 0,
                "complete physical return releases original tools once and clears only execution references");
            c.Equal(Fix.FromInt(4), PortStock(game, "ports.ore", true), "same-source tools plus returned feed remain real stock");
            c.Equal(Fix.One, active.Tools.Held.AmountOf(Resource(game, "ports.jig")), "old immutable reusable facts remain detached");
            c.Equal(game.Hash(), twin.Hash(), "saved returning continuation converges despite changed current tool selection");
            SelectTool(c, game, 7); SelectTool(c, twin, 7);
            Control(c, game, true); Control(c, twin, true);
            Enqueue(c, game, "ports.tool-batch"); Enqueue(c, twin, "ports.tool-batch");
            twin = factory.Load(game.Save()); Step(game, twin); Step(game, twin);
            c.That(State(game).QueueCount == 0 && State(game).Tools.Held.IsEmpty, "timed complete conversion releases both actual tool endpoints");
            c.Equal(Fix.FromInt(2), PortStock(game, "ports.alloy", false), "complete three-second batch produces authored product");
            c.Equal(Fix.FromInt(2), PortStock(game, "ports.ore", true), "reusable raw tools are not consumed as feed");
            c.Equal(Fix.FromInt(20), Stock(game, "ports.ore"), "selected feed/tool ports never use spare local stock");
            c.Equal(Fix.One, Stock(game, "ports.jig"), "unused local jig remains untouched");
            c.Equal(BigInteger.Zero, game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Totals(Resource(game, "ports.jig")).ConsumedRaw,
                "tool hold/release produces no fictitious consumption");
            c.Equal(game.Hash(), twin.Hash(), "saved batch completion matches actual receipt cleanup");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success, "ordinary tool player replay verifies: " + replay.Detail);
            c.Equal(game.Hash(), replay.ActualHash, "replay reproduces original tools and committed flow");

            var flow = New(factory); Prime(c, flow, Fix.FromInt(3), Fix.One); SupplyTool(c, flow); Enqueue(c, flow, "ports.tool-flow");
            c.Equal(1L, State(flow).Progress.WholeTicks, "continuous first draw respects real second");
            c.Equal(Fix.One, State(flow).Tools.Held.AmountOf(Resource(flow, "ports.ore")), "continuous feed leaves reusable raw receipt protected");
            var flowTwin = factory.Load(flow.Save());
            Control(c, flow, false); Control(c, flowTwin, false);
            SelectTool(c, flow, 8); SelectTool(c, flowTwin, 8);
            Control(c, flow, true); Control(c, flowTwin, true); Step(flow, flowTwin);
            c.Equal(2L, State(flow).Progress.WholeTicks, "continuous backpressure cannot spend reusable holds or earn excess work");
            c.Equal(TransformerManager.BlockerOutput, State(flow).Blocker, "full actual outlets stop stream");
            c.Equal(2, State(flow).Tools.Receipts.Count, "stream retains original remote and same-feed reusable tools");
            flowTwin = factory.Load(flow.Save()); Cancel(c, flow); Cancel(c, flowTwin);
            c.That(State(flow).QueueCount == 0 && State(flow).Tools.Receipts.Count == 0, "stop releases originals rather than the new empty selected port");
            c.Equal(Fix.One, PortStock(flow, "ports.ore", true), "burned continuous feed is not refunded; reusable raw remains");
            c.Equal(Fix.FromInt(2), PortStock(flow, "ports.alloy", false), "stop creates no product or refund");
            c.Equal(flow.Hash(), flowTwin.Hash(), "saved changed-port stream stop converges");
            foreach (string invalid in new[] { "missing", "extra", "unknown", "incompatible", "empty", "too-large", "joint-capacity" })
                c.Throws<ContentException>(() => New(Factory(json => {
                    var recipe = json.Get("transformations")[2]; var routes = recipe.Get("tool_ports");
                    if (invalid == "missing") recipe.Remove("tool_ports");
                    if (invalid == "extra") routes.Set("ports.water", JsonValue.Of("feed-water"));
                    if (invalid == "unknown") routes.Set("ports.jig", JsonValue.Of("unknown-tool"));
                    if (invalid == "incompatible") routes.Set("ports.jig", JsonValue.Of("raw"));
                    if (invalid == "empty") routes.Remove("ports.jig");
                    if (invalid == "too-large") recipe.Get("tools").Set("ports.ore", JsonValue.Number("5"));
                    if (invalid == "joint-capacity")
                    {
                        routes.Set("ports.ore", JsonValue.Of("tool.input")); routes.Set("ports.jig", JsonValue.Of("tool.input"));
                        recipe.Get("tools").Set("ports.ore", JsonValue.Number("39")); recipe.Get("tools").Set("ports.jig", JsonValue.Number("2"));
                    }
                })), "invalid native tool map refuses before Update: " + invalid);
        }
    }
}
