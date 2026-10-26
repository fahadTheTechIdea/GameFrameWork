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

namespace Beep.SimEngine.Tests
{
    // Ordinary player commands/views only. Native processor owns supply, consumption, time and signals.
    internal static partial class PoweredPointSensorTests
    {
        internal const string Case = "Powered point sensors: physical utilities, private coverage and saved consumption";
        internal static readonly Code Power = new Code("sensor.power"), Sight = new Code("sensor.sight");
        internal static void Register() { TestRunner.Add(Case, Run); RegisterExploration(); }
        private static GameFactory Factory(string document = null, IReadOnlyList<GameModule> additionalModules = null,
            IReadOnlyList<JsonValue> additionalDeclarations = null, IReadOnlyDictionary<string, string> extraDocuments = null)
        {
            var modules = new List<GameModule> { new UtilitySignalsModule() };
            var declarations = new List<JsonValue> { Json.Parse("{\"id\":\"sim.utility-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]}") };
            if (additionalModules != null) modules.AddRange(additionalModules);
            if (additionalDeclarations != null) declarations.AddRange(additionalDeclarations);
            return PointSensorTests.PhysicalFactory("point-sensor-utility.json", document, modules, declarations, extraDocuments);
        }
        internal static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 734 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            request.Mods.Add(ResourceFlowsModule.ModuleId); return factory.NewGame(request);
        }
        internal static EntityId Find(SimGame game, string definition, int seat = 0)
        {
            var entities = new List<EntityView>(); game.Seat(seat).View.CopyEntities(entities);
            return entities.Single(entity => entity.Def == definition).Id;
        }
        internal static ResourceId Supply(SimGame game)
        {
            if (!game.Seat(0).View.TryFindResource("supply", out var id)) throw new InvalidOperationException("missing supply metadata");
            return id;
        }
        internal static Fix Stock(SimGame game)
        {
            var result = game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(Find(game, "sensor.machine"), new Code("sensor.utility"), Supply(game), out var value);
            if (!result.IsNone || !value.Ready) throw new InvalidOperationException("missing actual sensor utility port: " + result);
            return value.Stock.AmountOf(Supply(game));
        }
        internal static TransformationSnapshot Process(SimGame game)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Find(game, "sensor.machine"), Power, out var value))
                throw new InvalidOperationException("missing actual sensor process");
            return value;
        }
        internal static UtilityDemandSnapshot Utility(SimGame game)
        {
            if (!game.Seat(0).View.Feature<OperationsView>().TryGetUtilityDemand(Find(game, "sensor.machine"), Power, out var value))
                throw new InvalidOperationException("missing actual utility demand");
            return value;
        }
        internal static BigInteger Consumed(SimGame game)
        {
            var summary = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            if (summary.Status != ResourceFlowReadStatus.Ready) throw new InvalidOperationException("cumulative utility accounting unavailable");
            return summary.Aggregates.Where(value => value.Resource == Supply(game) && value.Kind == ResourceFlowKind.Consumed)
                .Aggregate(BigInteger.Zero, (total, value) => total + value.AmountRaw);
        }
        internal static bool Visible(SimGame game, EntityId contact) => game.Seat(0).View.TryGetEntity(contact, out _);
        private static CommandResult Order(Check c, SimGame game, Command command)
        {
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary player preview");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary player command accepted"); return result;
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("powered sensor update fault: " + game.Fault);
            if (twin != null) Step(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int update = 0; update < 160 && !done(); update++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + ";game-time=" + game.Clock.StepIndex + ";process=" + Process(game).Blocker);
        }
        internal static AddRouteCommand SupplyRouteCommand(SimGame game, int batch) => new AddRouteCommand {
            From = Find(game, "source"), To = Find(game, "sensor.machine"), Resource = Supply(game),
            Settings = new LogisticsRouteSettings(true, Fix.FromInt(batch), 70) };
        private static int SupplyRoute(Check c, SimGame game, int batch = 8) => (int)Order(c, game, SupplyRouteCommand(game, batch)).Arg0;
        internal static ConfigureOperationCommand Control(SimGame game, bool enabled) => new ConfigureOperationCommand {
            Entity = Find(game, "sensor.machine"), Operation = Power, Enabled = enabled, Target = Fix.One };
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory); var independent = New(factory);
            ulong isolated = independent.Hash(); var contact = Find(game, "sensor.contact", 1);
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000); using var replay = game.RecordReplay(limits);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "existing generic runtime identity retained");
            c.That(!game.Modules.Contains("sim.tycoon"), "sensor utilities require no business module");
            c.That(!Visible(game, contact) && Stock(game).IsZero, "unpowered empty sensor has no fabricated live sight");
            int route = SupplyRoute(c, game);
            c.Equal(Fix.Zero, Stock(game), "route admission does not teleport fuel");
            var twin = factory.Load(game.Save());
            Reach(game, () => Stock(game) == Fix.FromInt(8), "physical utility delivery", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved carrier delivery continues identically");
            Order(c, game, new RemoveRouteCommand { Route = route });
            c.Equal(BigInteger.Zero, Consumed(game), "disabled processor consumes no delivered fuel");
            Order(c, game, new EnqueueTransformationCommand { Transformer = Find(game, "sensor.machine"), Capability = Power, Transformation = "sensor.power-cycle" });
            c.Equal(1, Process(game).QueueCount, "one original continuous intent");
            c.That(!Visible(game, contact) && Process(game).Progress.Equals(WorkProgress.Zero), "disabled queue earns neither work nor coverage");
            Order(c, game, Control(game, true));
            c.That(Visible(game, contact) && Utility(game).Drawing && Utility(game).ElapsedTicks > 0, "actual powered processing opens non-tile sight");
            c.That(Consumed(game) > BigInteger.Zero && Stock(game) < Fix.FromInt(8), "real feed and operating utility are consumed");
            c.That(Process(game).Flow.Produced.IsEmpty, "consume-only sensor fabricates no resource product");
            var frozen = Utility(game); var frozenProgress = Process(game).Progress;
            var layer = game.Seat(0).View.Feature<PointLayerView>(); var region = new PointRegion(Fix.FromInt(5), Fix.One, 1);
            var samples = new List<Fix>(); var page = layer.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1), samples);
            c.Equal(SpatialLayerReadStatus.Success, page.Status, "powered private non-tile layer page");
            ulong hash = game.Hash(); long time = game.Clock.StepIndex;
            for (int i = 0; i < 3; i++) { Utility(game); Process(game); Visible(game, contact); }
            c.That(hash == game.Hash() && time == game.Clock.StepIndex, "all utility/sight observations are pure");
            c.That(!game.Seat(1).View.Feature<OperationsView>().TryGetUtilityDemand(Find(game, "sensor.machine"), Power, out _), "foreign utility facts withheld");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(Control(game, false)), "foreign power control withheld");
            Order(c, game, Control(game, false));
            BigInteger stopped = Consumed(game); Fix heldStock = Stock(game); var stoppedProgress = Process(game).Progress;
            twin = factory.Load(game.Save());
            for (int i = 0; i < 3; i++) Step(game, twin);
            c.That(!Visible(game, contact) && !Utility(game).Drawing, "disabled power revokes current coverage and utility demand");
            c.That(heldStock == Stock(game) && stopped == Consumed(game) && stoppedProgress.Equals(Process(game).Progress), "disabled intervals earn no consumption or work");
            c.Equal(game.Hash(), twin.Hash(), "disabled saved continuation deterministic");
            c.Equal(SpatialLayerReadStatus.Unavailable, layer.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1, page.Cursor), samples).Status, "power shutdown withholds private layer before cursor admission");
            game.Seat(0).Submit(Control(game, true)); twin = factory.Load(game.Save()); Step(game, twin);
            c.Equal(game.Hash(), twin.Hash(), "saved pending enable retains exact original consumption");
            c.Equal(SpatialLayerReadStatus.Invalidated, layer.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1, page.Cursor), samples).Status, "restored sight cannot revive a pre-shutdown cursor");
            Reach(game, () => Stock(game).IsZero && !Process(game).Blocker.IsNone, "actual fuel exhaustion", twin);
            c.That(!Visible(game, contact) && !Utility(game).Satisfied, "real shortage revokes powered coverage");
            c.Equal(game.Hash(), twin.Hash(), "saved running sensor reaches same shortage");
            c.Equal(new BigInteger(Fix.FromInt(8).Raw), Consumed(game), "all eight delivered units are consumed exactly once");
            c.Equal(ResourceFlowReadStatus.Expired, game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Status,
                "bounded two-record feed is explicitly expired, never substituted for cumulative consumption");
            c.Equal(Fix.FromInt(4), Utility(game).Consumed.AmountOf(Supply(game)), "four authored elapsed utility intervals");
            c.Equal(Fix.FromInt(4), Process(game).Flow.Consumed.AmountOf(Supply(game)), "four authored productive feed intervals");
            var starved = Process(game).Progress; stopped = Consumed(game); Step(game); Step(game);
            c.That(starved.Equals(Process(game).Progress) && stopped == Consumed(game), "starved sensor cannot earn work or consume a partial prefix");
            Order(c, game, Control(game, false)); route = SupplyRoute(c, game);
            twin = factory.Load(game.Save()); Reach(game, () => Stock(game) == Fix.FromInt(8), "physical exhausted-sensor replenishment", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved physical replenishment deterministic");
            Order(c, game, new RemoveRouteCommand { Route = route });
            Order(c, game, Control(game, true));
            c.That(Visible(game, contact) && Process(game).Progress.WholeTicks > starved.WholeTicks, "same queued operation resumes after real replenishment");
            c.Equal(1, Process(game).QueueCount, "recovery never duplicates original continuous intent");
            Order(c, game, new CancelTransformationCommand { Transformer = Find(game, "sensor.machine"), Capability = Power, Index = 0 });
            c.That(Process(game).QueueCount == 0 && !Visible(game, contact), "normal stop retires power intent and live coverage");
            c.Equal(new BigInteger(Fix.FromInt(10).Raw), Consumed(game), "normal stop never refunds or repeats earned consumption");
            c.Equal(Fix.FromInt(6), Stock(game), "real remaining replenished fuel stays in its original store");
            c.That(frozen.Drawing && frozen.ElapsedTicks > 0 && !frozenProgress.Equals(WorkProgress.Zero), "retained detached facts preserve original powered state");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal consumed-utility checkpoint reloads");
            c.Equal(isolated, independent.Hash(), "interleaved unrelated session borrows no fuel/exposure/coverage");
            c.That(new ReplayVerifier().Verify(factory, replay.Finish(), limits).Success, "all actual route/power/shortage/recovery/stop commands replay");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "consumer", "operation", "port", "signal" })
            {
                var document = Json.Parse(FrameworkProofContent.Read("point-sensor-utility.json")); var parts = document.Get("entities")[0].Get("components");
                if (mode == "consumer") parts.Get("utility_signal").Set("consumer", JsonValue.Of("missing.consumer"));
                if (mode == "operation") parts.Get("point_sensor").Get("conditions")[0].Set("source", JsonValue.Of("missing.operation"));
                if (mode == "port") document.Get("transformations")[0].Get("operating_ports").Set("supply", JsonValue.Of("missing.port"));
                if (mode == "signal") parts.Get("point_sensor").Get("conditions")[1].Set("signal", JsonValue.Of("missing.signal"));
                c.Throws<ContentException>(() => New(Factory(document.ToString())), "invalid powered sensor " + mode + " refuses composition");
            }
        }
    }
}
