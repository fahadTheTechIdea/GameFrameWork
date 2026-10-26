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
    public static class SignalObservationBoundaryTests
    {
        public const string Case = "Signal observation boundary: independent physical work, hidden mutation and shared fault cutoff";
        private static readonly Code SensorId = new Code("boundary.sensor");
        public static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory() => ManufacturingDispatchTests.Factory(workDocument: "signal-boundary-work.json",
            startupDocument: "signal-boundary-startup.json", extension: new SignalBoundaryProofModule(), extensionDocument: "signal-boundary.json");
        private static SimGame New(GameFactory factory) => ManufacturingDispatchTests.New(factory);
        private static Entity Site(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "boundary.factory");
        private static AutomationProbe Provider(SimGame game) => Site(game).Get<AutomationProbe>();
        private static OperationsView Operations(SimGame game, int company = 0) => game.Seat(company).View.Feature<OperationsView>();
        private static WorkView Work(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static AutomationSignalSnapshot Read(SimGame game, string signal = "boundary.level")
        {
            Code result = Operations(game).ReadSignal(Site(game).Id, SensorId, signal, out var snapshot);
            if (!result.IsNone) throw new InvalidOperationException("signal read refused: " + result);
            return snapshot;
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("boundary work time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 160 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " nodes=" +
                string.Join(";", Board(game).Orders.SelectMany(o => o.Nodes.Select(n => n.Id + "/" + n.State + "/" + n.Progress + "/" + n.Blocker))));
        }
        private static long Begin(Check c, SimGame game)
        {
            var definition = game.World.Content.Get<EntityActivityDef>("boundary.workflow");
            var result = TestKit.Order(game, 0, new SubmitPlanCommand { Intent = definition.CreateTargetIntent(Site(game).Id,
                ActivityWorkerSelection.Unbound, "boundary.work", 50) });
            c.That(result.Accepted, "ordinary player intent creates real work graph"); return result.Arg0;
        }
        private static PlanView Plan(SimGame game, long id)
        { var plans = new List<PlanView>(); Work(game).CopyPlans(plans); return plans.Single(p => p.Id == id); }
        private static void Level(Check c, SimGame game, int value, bool available = true)
            => c.That(TestKit.Order(game, 0, new SetAutomationProbeCommand { Entity = Site(game).Id,
                Level = Fix.FromInt(value), Available = available }).Accepted, "normal engine command changes actual provider facts");
        private static void Cutoff(Check c, SimGame game, string stage)
        {
            c.That(game.Fault != null, stage + " latches existing session fault"); var fault = game.Fault; long time = game.Tick;
            game.Update(); c.Equal(time, game.Tick, stage + " cannot continue sole clock");
            c.That(ReferenceEquals(fault, game.Fault), stage + " preserves first fault");
            c.Throws<InvalidOperationException>(() => game.Save(), stage + " cannot save partial provider state");
            c.Throws<InvalidOperationException>(() => game.Hash(), stage + " cannot checkpoint hash");
            c.Throws<InvalidOperationException>(() => Read(game), stage + " subsequent signal read cannot appear healthy");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "strict shared signal observation identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no new signal/work state layout");
            var factory = Factory(); var game = New(factory); ulong hash = game.Hash();
            c.That(Provider(game) is IAutomationSignalSource && Site(game).Get<Transformer>() == null, "independent actual provider needs no native transformer");
            c.That(!game.World.Services.Has<IEconomy>(), "physical signal-controlled work has no business requirement");
            var frozen = Read(game); c.Equal(Fix.FromInt(2), frozen.Value.Value, "explicit native numeric provider quantity");
            c.Equal(Fix.One, Read(game, "boundary.enabled").Value.Value, "explicit independent Boolean provider fact");
            var native = game.World.Entities.All.Single(e => e.Def.Id == "dispatch.factory");
            c.Equal(Code.None, Operations(game).ReadSignal(native.Id, new Code("dispatch.production"), "boundary.native-enabled", out var enabled), "native signal shares same reader");
            c.Equal(Fix.Zero, enabled.Value.Value, "actual native authored disabled state");
            c.Equal(Code.None, Operations(game).ReadSignal(native.Id, new Code("dispatch.production"), "boundary.native-backlog", out var backlog), "native numeric signal shares same reader");
            c.Equal(Fix.Zero, backlog.Value.Value, "actual native empty queue");
            c.Equal(Code.None, Operations(game).ReadSignal(Site(game).Id, new Code("missing"), "boundary.level", out var missing), "absent source is a typed fact");
            c.That(!missing.Available && missing.Blocker == OperationCodes.SignalUnavailable, "missing source never substitutes another capability");
            c.Equal(Code.None, Operations(game).ReadSignal(native.Id, new Code("dispatch.production"), "boundary.level", out var incompatible), "incompatible source remains typed");
            c.That(!incompatible.Available && incompatible.Blocker == OperationCodes.SignalUnavailable, "wrong actual schema never fabricates a value");
            c.Equal(hash, game.Hash(), "healthy native/independent/unavailable reads pure");

            Provider(game).FaultMode = 5;
            Code foreign = Operations(game, 1).ReadSignal(Site(game).Id, SensorId, "boundary.level", out _);
            c.That(foreign == Codes.NotOwned || foreign == Codes.NotFound, "foreign company refused before provider callback");
            c.That(game.Fault == null && game.Hash() == hash, "foreign read neither mutates hidden provider state nor faults");
            Provider(game).FaultMode = 0;
            var other = New(factory); Provider(other).FaultMode = 5;
            var signal = game.World.Content.Get<AutomationSignalDef>("boundary.level");
            c.Throws<ArgumentException>(() => signal.ReadFrom(game.World, Provider(other)), "foreign-session source rejected before callback");
            c.That(game.Fault == null && other.Fault == null, "caller alias refusal is not committed provider failure");
            Provider(other).FaultMode = 0; c.Equal(Fix.FromInt(2), Read(other).Value.Value, "foreign callback never ran");
            c.Equal(hash, game.Hash(), "session identity refusal leaves original unchanged");
            Level(c, game, 2, false); c.Equal(new Code("probe.sensor-offline"), Read(game).Blocker, "actual provider unavailable reason preserved");
            Level(c, game, 1); long plan = Begin(c, game);
            Reach(game, () => Plan(game, plan).State == PlanState.Active, "real signal-blocked activation");
            Order order = Board(game).Order(Plan(game, plan).Order);
            for (int i = 0; i < 3; i++) Update(game);
            c.That(order.Node("prepare").Progress.Equals(WorkProgress.Zero) && order.Node("prepare").Blocker == new Code("boundary.level-unsafe"),
                "actual unsafe provider cannot earn physical work");
            Level(c, game, 2);
            Reach(game, () => order.Node("prepare").Progress.WholeTicks > 0, "real worker arrival and earned progress");
            c.That(order.Node("prepare").Progress.WholeTicks < 6 && Board(game).Jobs.Any(j => j.Worker.IsValid), "independent signal controls real timed worker job");
            var earned = order.Node("prepare").Progress;
            Level(c, game, 1); for (int i = 0; i < 3; i++) Update(game);
            c.Equal(earned, order.Node("prepare").Progress, "current shared signal read pauses without discarding earned work");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual paused provider/jobs graph checkpoint");
            Level(c, game, 2); Level(c, twin, 2);
            Reach(game, () => Plan(game, plan).State == PlanState.Done, "saved physical work resumes", twin);
            c.Equal(game.Hash(), twin.Hash(), "interleaved independent provider/work continuation identical");
            c.That(order.Node("prepare").Progress.Complete(6) && order.Node("verify").Progress.Complete(2), "real sequential physical work earns authored durations");
            c.Equal(Fix.FromInt(2), frozen.Value.Value, "detached earlier signal is not mutable state");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed work uses existing provider/codecs");

            foreach (int mode in new[] { 1, 2, 5, 6, 7 })
            {
                game = New(factory); Provider(game).FaultMode = mode;
                c.Throws<InvalidOperationException>(() => Read(game), "shared view catches signal fault mode" + mode);
                Cutoff(c, game, "view fault" + mode);
            }
            game = New(factory); Provider(game).FaultMode = 5;
            c.Throws<InvalidOperationException>(() => game.World.Content.Get<AutomationSignalDef>("boundary.level").ReadFrom(game.World, Provider(game)),
                "direct shared reader catches hidden persisted mutation without Touch");
            Cutoff(c, game, "direct reader");
            game = New(factory); Provider(game).FaultMode = 5;
            c.Throws<InvalidOperationException>(() => Work(game).PreviewOrderRequirements("boundary.workflow", Site(game).Id, new List<WorkRequirementView>()),
                "work preview inherits same pure signal boundary without local wrapper");
            Cutoff(c, game, "work preview");
            game = New(factory); plan = Begin(c, game);
            Reach(game, () => Plan(game, plan).State == PlanState.Active, "faulted work setup");
            Provider(game).FaultMode = 5; game.Update(); Cutoff(c, game, "normal work Update");
            c.That(New(factory).Fault == null && twin.Fault == null, "faults never poison separate or saved sessions");
        }
    }
}
