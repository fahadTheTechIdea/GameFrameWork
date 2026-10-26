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
    internal static partial class PoweredPointSensorTests
    {
        internal const string ExplorationCase = "Powered remote exploration: interruption, physical resupply and private shared rediscovery";
        internal static readonly Code Explorer = new Code("powered.exploration");
        private static void RegisterExploration() => TestRunner.Add(ExplorationCase, Explore);
        internal static GameFactory ExplorationFactory()
        {
            var sensor = Json.Parse(FrameworkProofContent.Read("point-sensor-utility.json"));
            sensor.Get("entities")[0].Get("components").Set("authored_exploration", Json.Parse(
                "{\"id\":\"powered.exploration\",\"sensor\":\"sensor.sight\",\"profile\":\"powered.remote-profile\"}"));
            return Factory(sensor.ToString(), new GameModule[] { new ExplorationModule() },
                new[] { Json.Parse("{\"id\":\"sim.exploration\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"point-sensor-exploration.json\"]}") },
                new Dictionary<string, string> { ["point-sensor-exploration.json"] = FrameworkProofContent.Read("point-sensor-exploration.json") });
        }
        internal static ExplorationSnapshot Scan(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<ExplorationView>().Read(Find(game, "sensor.machine"), Explorer, out var value);
            if (!code.IsNone) throw new InvalidOperationException("powered exploration read: " + code);
            return value;
        }
        internal static ExplorationEntrySnapshot Entry(SimGame game, string id) => Scan(game).Entries.Single(value => value.Id == id);
        internal static bool Known(SimGame game, string branch, int seat = 0) => game.Seat(seat).View.Feature<KnowledgeView>()
            .TryRead(new PlayerId(seat), "sim.knowledge.facts", "powered." + branch + "-known", out _);
        internal static ConfigureOperationCommand SightCommand(SimGame game, Fix radius) => new ConfigureOperationCommand {
            Entity = Find(game, "sensor.machine"), Operation = Sight, Enabled = true, Target = radius };
        private static void SightControl(Check c, SimGame game, Fix radius) => Order(c, game, SightCommand(game, radius));
        private static void Explore(Check c)
        {
            var factory = ExplorationFactory(); var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000); using var replay = game.RecordReplay(limits);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "remote powered composition uses existing runtime");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "earned observation needs neither business nor money");
            var initial = Scan(game); c.That(initial.Entries.All(row => row.Blocker == ExplorationCodes.NoSensor && !row.Known), "empty unpowered remote region remains undiscovered");
            var foreign = game.Seat(1).View.Feature<ExplorationView>().Read(Find(game, "sensor.machine"), Explorer, out var hidden);
            c.That(foreign == Codes.NotFound && hidden == null, "foreign partial exploration is withheld");
            int route = SupplyRoute(c, game); var twin = factory.Load(game.Save());
            Reach(game, () => Stock(game) == Fix.FromInt(8), "physical remote exploration fuel", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved real supply is deterministic");
            Order(c, game, new RemoveRouteCommand { Route = route });
            Order(c, game, new EnqueueTransformationCommand { Transformer = Find(game, "sensor.machine"), Capability = Power, Transformation = "sensor.power-cycle" });
            long execution = Process(game).Queue.Single().Execution;
            SightControl(c, game, Fix.Parse("1.5")); Order(c, game, Control(game, true));
            c.That(Scan(game).Entries.All(row => row.Progress.Equals(WorkProgress.Zero) && row.Blocker == ExplorationCodes.NoSensor), "center-only powered sight cannot earn complete-region discovery");
            c.Equal(Fix.FromInt(5), ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(row => row.Id == Find(game, "sensor.machine")).Spatial).Position,
                "remote source remains outside target interval six-to-seven");
            SightControl(c, game, Fix.Parse("2.5"));
            c.That(Scan(game).Entries.All(row => row.Progress.WholeTicks == 1 && !row.Known), "full powered coverage earns one actual interval in both entries");
            var partial = Scan(game); twin = factory.Load(game.Save());
            Order(c, game, Control(game, false));
            c.That(Entry(game, "a.reset").Progress.Equals(WorkProgress.Zero) && Entry(game, "b.retain").Progress.WholeTicks == 1,
                "power interruption obeys authored reset and retain policies");
            twin = factory.Load(game.Save()); Step(game, twin);
            c.Equal(game.Hash(), twin.Hash(), "saved power-off dwell stays deterministic");
            long time = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.Equal(time, game.Clock.StepIndex, "paused sole clock earns no remote exposure"); game.Clock.Resume(); twin.Clock.Resume();
            game.Seat(0).Submit(Control(game, true)); twin = factory.Load(game.Save()); Step(game, twin);
            c.Equal(game.Hash(), twin.Hash(), "pending power restart preserves original exploration progress");
            Reach(game, () => Stock(game).IsZero && !Process(game).Blocker.IsNone, "remote observation fuel exhaustion", twin);
            c.That(!Known(game, "reset") && !Known(game, "retain"), "unpaid or incomplete final intervals cannot reveal the region");
            c.That(Entry(game, "a.reset").Progress.Equals(WorkProgress.Zero) && Entry(game, "b.retain").Progress.WholeTicks == 2,
                "real shortage resets or retains only original earned exposure");
            c.Equal(game.Hash(), twin.Hash(), "saved twins reach same actual shortage");
            Order(c, game, Control(game, false)); route = SupplyRoute(c, game);
            twin = factory.Load(game.Save()); Reach(game, () => Stock(game) == Fix.FromInt(8), "physical remote observation replenishment", twin);
            c.Equal(game.Hash(), twin.Hash(), "supply restores no knowledge before powered observation");
            Order(c, game, new RemoveRouteCommand { Route = route }); Order(c, game, Control(game, true));
            c.That(Known(game, "retain") && !Known(game, "reset"), "retained branch finishes without gifting reset branch time");
            twin = factory.Load(game.Save()); Reach(game, () => Known(game, "reset"), "complete reset branch after physical fuel", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved partial-to-known continuation agrees");
            c.That(Scan(game).Entries.All(row => row.Known && row.Progress.Equals(WorkProgress.Zero)), "original ledger alone owns completed discoveries");
            c.Equal(execution, Process(game).Queue.Single().Execution, "power recovery does not repeat original intent");
            Order(c, game, Control(game, false));
            c.That(!Known(game, "reset", 1), "earned remote discovery stays company-private");
            Order(c, game, new ShareKnowledgeCommand { Recipient = new PlayerId(1), Policy = "powered.share" });
            c.That(Known(game, "reset", 1) && Known(game, "retain", 1), "explicit permitted sharing exports actual earned facts");
            Order(c, game, new ForgetKnowledgeCommand { Policy = "powered.forget" });
            c.That(!Known(game, "reset") && !Known(game, "retain") && Known(game, "reset", 1), "own forgetting cannot revoke recipient's independent copy");
            Step(game); c.That(Scan(game).Entries.All(row => !row.Known && row.Progress.Equals(WorkProgress.Zero)), "forgotten knowledge cannot return while power is disabled");
            route = SupplyRoute(c, game, 4); twin = factory.Load(game.Save());
            Reach(game, () => Stock(game) == Fix.FromInt(6), "last finite source supply for rediscovery", twin);
            c.Equal(game.Hash(), twin.Hash(), "third physical delivery remains deterministic");
            Order(c, game, new RemoveRouteCommand { Route = route });
            var lowPower = Control(game, true); lowPower.Target = Fix.Half; Order(c, game, lowPower);
            twin = factory.Load(game.Save()); Reach(game, () => Known(game, "reset") && Known(game, "retain"), "earned low-rate rediscovery", twin);
            c.Equal(game.Hash(), twin.Hash(), "rediscovery uses same saved clock/ledger and actual utilities");
            c.That(Known(game, "reset", 1) && Known(game, "retain", 1), "rediscovery never rewrites recipient observations");
            c.That(partial.Entries.All(row => !row.Known && row.Progress.WholeTicks == 1), "held partial facts stay detached after sharing/forgetting/rediscovery");
            c.Equal(isolated, idle.Hash(), "interleaved world borrows no power, discovery or sharing state");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed discovery saves with actual resource exposure");
            c.That(new ReplayVerifier().Verify(factory, replay.Finish(), limits).Success, "power supply regional dwell sharing and rediscovery replay exactly");
        }
    }
}
