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
using static Beep.SimEngine.Tests.TransformationInputPortTests;

namespace Beep.SimEngine.Tests
{
    // Ordinary player requests and immutable observations; selected engine content owns all execution.
    public static class TransformationOperatingPortTests
    {
        public const string Case = "P8 operating ports: mixed timed draw, shared startup and inventory-free saved batch/flow";
        private const string External = "ports.external-factory";
        private static readonly Code Process = new Code("process");
        private static readonly ResourceEndpointId UtilityWater = new ResourceEndpointId("proof.pools", 9);
        private static readonly ResourceEndpointId Tool = new ResourceEndpointId("proof.pools", 7);
        public static void Register() => TestRunner.Add(Case, Run);
        private static TransformationSnapshot Current(SimGame game) => State(game, External);
        private static void Queue(Check c, SimGame game, string recipe) => Order(c, game,
            new EnqueueTransformationCommand { Transformer = Find(game, External), Capability = Process, Transformation = recipe, Count = 1 });
        private static void Enable(Check c, SimGame game, bool enabled) => Order(c, game,
            new ConfigureOperationCommand { Entity = Find(game, External), Operation = Process, Enabled = enabled, Target = Fix.One });
        private static void Stop(Check c, SimGame game) => Order(c, game,
            new CancelTransformationCommand { Transformer = Find(game, External), Capability = Process, Index = 0 });
        private static void Water(Check c, SimGame game, Fix amount, SimGame twin = null)
            => Transfer(c, game, ResourceEndpointId.ForEntity(Find(game, "ports.source")), UtilityWater, "ports.water", amount, twin);
        private static void Jig(Check c, SimGame game)
            => Transfer(c, game, ResourceEndpointId.ForEntity(Find(game, "ports.source")), Tool, "ports.jig", Fix.One);
        private static UtilityDemandSnapshot Demand(SimGame game)
        {
            if (!game.Seat(0).View.Feature<OperationsView>().TryGetUtilityDemand(Find(game, External), Process, out var facts))
                throw new InvalidOperationException("missing engine-owned utility facts");
            return facts;
        }
        private static Fix UtilityStock(SimGame game) => Demand(game).Ports.Single(port => port.Resource == Resource(game, "ports.water"))
            .Stock.AmountOf(Resource(game, "ports.water"));
        private static void Burned(Check c, SimGame game, string resource, Fix expected)
        {
            var rows = game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Entries;
            BigInteger actual = rows.Where(row => row.Item.Resource == Resource(game, resource) && row.Item.Cause == TransformerManager.CauseOperatingInputs)
                .Aggregate(BigInteger.Zero, (sum, row) => sum + row.Item.Amount.Raw);
            c.Equal(new BigInteger(expected.Raw), actual, "actual operating ledger retains exact timed burn for " + resource);
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory); var quarter = Fix.Parse("0.25"); var half = Fix.Parse("0.5");
            var limits = new ReplayLimits(2 * 1024 * 1024, 500); var recorder = game.RecordReplay(limits);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build, "current operating-port engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat, "existing execution/save layout is unchanged");
            c.That(!game.Modules.Contains("sim.tycoon") && game.Seat(0).Snapshot.Space == null, "manufacturing needs no money, market or platform map");
            c.That(!game.Seat(0).View.Feature<InventoryView>().TryGetCapacity(Find(game, External), Code.None, out _), "processor genuinely has no entity inventory/inlet fallback");
            ulong hash = game.Hash(); var view = game.Seat(0).View.Feature<TransformationManagementView>();
            c.Equal(Code.None, view.ReadOperatingPorts(Find(game, External), Process, "ports.operating-batch", out var ports), "authored operating ports are public engine facts");
            c.Equal(2, ports.Count, "independent solid/liquid utility ports");
            c.That(ports.Single(port => port.Resource == Resource(game, "ports.water")).Endpoint == UtilityWater,
                "operating water is separate from feed water");
            c.Equal(hash, game.Hash(), "operating selection facts are pure");
            c.Throws<NotSupportedException>(() => ((IList<ResourcePortSnapshot>)ports).Clear(), "port facts cannot mutate authored selection");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<TransformationManagementView>().ReadOperatingPorts(Find(game, External), Process,
                "ports.operating-batch", out var hidden), "foreign operating port facts refused");
            c.Equal(0, hidden.Count, "foreign port refusal reveals no actual stock");
            c.That(!game.Seat(1).View.Feature<OperationsView>().TryGetUtilityDemand(Find(game, External), Process, out _), "generic utility facts are company-private");

            Queue(c, game, "ports.operating-batch");
            Prime(c, game, Fix.FromInt(2), half); Jig(c, game); Water(c, game, quarter);
            c.Equal(WorkSystem.BlockerMaterials, Current(game).Blocker, "feed/tool/utility same-source aggregate blocks despite each individual bill fitting");
            c.That(Current(game).Wip.Stock.IsEmpty && Current(game).Tools.Receipts.Count == 0 && Current(game).Operating.Consumed.IsEmpty,
                "whole failed startup acquires, burns and progresses no prefix");
            c.Equal(Fix.FromInt(2), PortStock(game, "ports.ore", true), "combined startup retains all original ore");
            c.Equal(quarter, UtilityStock(game), "combined startup retains ready independent water");
            var twin = factory.Load(game.Save()); Prime(c, game, Fix.One, Fix.Zero, twin);
            c.Equal(1L, Current(game).Progress.WholeTicks, "whole startup earns only its actual first second");
            c.Equal(Fix.One, Current(game).Wip.Stock.AmountOf(Resource(game, "ports.ore")), "raw feed enters protected native WIP");
            c.Equal(Fix.One, Current(game).Tools.Held.AmountOf(Resource(game, "ports.ore")), "same actual source protects reusable ore remainder");
            c.Equal(Fix.One, Current(game).Tools.Held.AmountOf(Resource(game, "ports.jig")), "independent reusable jig is protected");
            c.Equal(Fix.One, PortStock(game, "ports.ore", true), "first utility burn leaves only held reusable ore");
            c.Equal(Fix.Zero, UtilityStock(game), "selected independent utility source burns its quarter litre");
            hash = game.Hash(); var active = Demand(game);
            c.Equal(2, active.Ports.Count, "generic utility contract exposes both actual endpoints");
            c.Equal(1L, active.ElapsedTicks, "utility exposure follows the sole native running counter");
            c.Equal(Fix.One, active.Consumed.AmountOf(Resource(game, "ports.ore")), "solid utility cumulative rate follows native discrete quantum");
            c.Equal(quarter, active.Consumed.AmountOf(Resource(game, "ports.water")), "fluid utility cumulative rate follows its selected quantum");
            c.Equal(hash, game.Hash(), "generic actual endpoint/version/stock validation is pure");
            c.Equal(game.Hash(), twin.Hash(), "saved supplying twin starts identical whole execution");
            Step(game, twin);
            c.Equal(TransformerManager.BlockerOperatingInputs, Current(game).Blocker, "missing timed draw is engine-reported");
            c.Equal(1L, Current(game).Progress.WholeTicks, "utility shortage earns no fabricated progress");
            Prime(c, game, Fix.One, Fix.Zero, twin);
            c.Equal(Fix.FromInt(2), PortStock(game, "ports.ore", true), "ready solid prefix cannot burn without water");
            c.Equal(1L, Current(game).Operating.ElapsedTicks, "partial supply cannot change earned exposure");
            Water(c, game, quarter, twin);
            c.Equal(2L, Current(game).Progress.WholeTicks, "second whole mixed draw earns second work interval");
            c.Equal(Fix.FromInt(2), Demand(game).Consumed.AmountOf(Resource(game, "ports.ore")), "cumulative burn contains exactly two earned draws");
            Enable(c, game, false); Enable(c, twin, false); var paused = Demand(game);
            c.That(!paused.Drawing, "disabled operation advertises no active draw");
            Prime(c, game, Fix.One, Fix.Zero, twin); Water(c, game, quarter, twin); Step(game, twin);
            c.Equal(2L, Current(game).Progress.WholeTicks, "ready disabled sources do not advance work");
            c.Equal(quarter, UtilityStock(game), "pause preserves utility stock");
            c.Equal(Fix.Zero, paused.Ports.Single(port => port.Resource == Resource(game, "ports.water")).Stock.AmountOf(Resource(game, "ports.water")),
                "old utility snapshot stays detached after later supply");
            twin = factory.Load(game.Save()); Enable(c, game, true); Enable(c, twin, true);
            c.Equal(0, Current(game).QueueCount, "third earned second completes native batch");
            c.Equal(Fix.FromInt(2), PortStock(game, "ports.alloy", false), "complete batch converts into actual solid product");
            c.Equal(Fix.One, PortStock(game, "ports.water", false), "complete batch converts into actual separate liquid product");
            c.Equal(Fix.One, PortStock(game, "ports.ore", true), "completion releases reusable source ore, not burned utility");
            c.That(Current(game).Tools.Held.IsEmpty && Demand(game).Ports.Count == 0, "finished head releases holds and active utility routes");
            Burned(c, game, "ports.ore", Fix.FromInt(3)); Burned(c, game, "ports.water", Fix.Parse("0.75"));
            c.Equal(Fix.FromInt(2), paused.Consumed.AmountOf(Resource(game, "ports.ore")), "old cumulative facts remain detached after completion");
            c.That(ports.All(port => port.Stock.IsEmpty), "original empty port facts remain detached");
            c.Equal(game.Hash(), twin.Hash(), "saved disabled twin finishes identical actual stock/receipt/history state");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success, "native operating-port public-player replay verifies: " + replay.Detail);
            c.Equal(game.Hash(), replay.ActualHash, "replay reproduces whole utility burns and release");

            var cancelled = New(factory); Prime(c, cancelled, Fix.FromInt(3), half); Jig(c, cancelled); Water(c, cancelled, quarter);
            Queue(c, cancelled, "ports.operating-batch"); var cancelledTwin = factory.Load(cancelled.Save());
            c.Equal(1L, Current(cancelled).Progress.WholeTicks, "cancellation fixture performs actual timed burn before stop");
            Stop(c, cancelled); Stop(c, cancelledTwin);
            c.That(Current(cancelled).QueueCount == 0 && Current(cancelled).Tools.Held.IsEmpty, "cancel returns complete WIP then releases original tools");
            c.Equal(Fix.FromInt(2), PortStock(cancelled, "ports.ore", true), "cancel returns feed and tool only, never consumed utility ore");
            c.Equal(half, PortStock(cancelled, "ports.water", true), "cancel returns feed water to its original separate endpoint");
            c.Equal(Fix.Zero, PortStock(cancelled, "ports.alloy", false), "cancel never creates unearned product");
            Burned(c, cancelled, "ports.ore", Fix.One); Burned(c, cancelled, "ports.water", quarter);
            c.Equal(cancelled.Hash(), cancelledTwin.Hash(), "saved working cancellation preserves actual burned accounting");

            var flow = New(factory); Prime(c, flow, Fix.FromInt(3), Fix.One); Jig(c, flow); Water(c, flow, half);
            Queue(c, flow, "ports.operating-flow");
            c.Equal(1L, Current(flow).Progress.WholeTicks, "continuous feed/utility/output share one earned first interval");
            c.Equal(Fix.One, Current(flow).Flow.Consumed.AmountOf(Resource(flow, "ports.ore")), "continuous feed accounting is distinct from operating consumption");
            c.Equal(Fix.One, Current(flow).Operating.Consumed.AmountOf(Resource(flow, "ports.ore")), "continuous operating accounting follows same interval");
            var flowTwin = factory.Load(flow.Save()); Step(flow, flowTwin);
            c.Equal(TransformerManager.BlockerFlowInputs, Current(flow).Blocker, "protected tool cannot supply continuous feed/utility draw");
            Prime(c, flow, Fix.One, Fix.Zero, flowTwin);
            c.Equal(1L, Current(flow).Progress.WholeTicks, "one ready ore cannot satisfy combined two-ore draw");
            c.Equal(Fix.FromInt(2), PortStock(flow, "ports.ore", true), "mixed conversion shortage consumes no solid prefix");
            c.Equal(quarter, UtilityStock(flow), "mixed conversion shortage consumes no liquid utility prefix");
            Prime(c, flow, Fix.One, Fix.Zero, flowTwin);
            c.Equal(2L, Current(flow).Progress.WholeTicks, "whole second feed/utility draw earns complete second output");
            c.Equal(Fix.FromInt(2), PortStock(flow, "ports.alloy", false), "continuous native output reaches actual capacity");
            c.Equal(Fix.One, PortStock(flow, "ports.water", false), "continuous liquid output reaches its separate actual capacity");
            flowTwin = factory.Load(flow.Save()); Step(flow, flowTwin);
            Prime(c, flow, Fix.FromInt(2), half, flowTwin); Water(c, flow, quarter, flowTwin); Step(flow, flowTwin);
            c.Equal(2L, Current(flow).Progress.WholeTicks, "full outputs do not earn progress or burn ready feed/utility");
            c.Equal(Fix.FromInt(3), PortStock(flow, "ports.ore", true), "backpressure retains held tool plus ready feed/utility ore");
            c.Equal(quarter, UtilityStock(flow), "backpressure retains ready operating water");
            c.Equal(half, PortStock(flow, "ports.water", true), "backpressure retains ready feed water");
            Stop(c, flow); Stop(c, flowTwin);
            c.That(Current(flow).QueueCount == 0 && Current(flow).Tools.Held.IsEmpty, "continuous stop releases reusable tools without refunding either burn");
            c.Equal(Fix.FromInt(3), PortStock(flow, "ports.ore", true), "continuous stop changes no remaining stock");
            var flowHistory = flow.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0);
            c.Equal(new BigInteger(Fix.FromInt(4).Raw), flowHistory.Totals(Resource(flow, "ports.ore")).ConsumedRaw,
                "original whole conversion ledger contains feed plus operating ore, never reusable tools");
            c.Equal(new BigInteger(Fix.Parse("1.5").Raw), flowHistory.Totals(Resource(flow, "ports.water")).ConsumedRaw,
                "original whole conversion ledger contains feed plus operating water");
            c.Equal(flow.Hash(), flowTwin.Hash(), "saved continuous shortage/backpressure/stop twins remain identical");

            foreach (string invalid in new[] { "missing", "extra", "unknown", "incompatible", "empty", "oversize" })
                c.Throws<ContentException>(() => New(Factory(json => {
                    var recipe = json.Get("transformations")[4]; var routes = recipe.Get("operating_ports");
                    if (invalid == "missing") recipe.Remove("operating_ports");
                    if (invalid == "extra") routes.Set("ports.jig", JsonValue.Of("tool"));
                    if (invalid == "unknown") routes.Set("ports.ore", JsonValue.Of("unknown-utility"));
                    if (invalid == "incompatible") routes.Set("ports.water", JsonValue.Of("raw"));
                    if (invalid == "empty") routes.Remove("ports.water");
                    if (invalid == "oversize") recipe.Get("operating_inputs")[0].Set("quantity", JsonValue.Number("5"));
                })), "invalid authored operating routes/capacity refuse before Update: " + invalid);
        }
    }
}
