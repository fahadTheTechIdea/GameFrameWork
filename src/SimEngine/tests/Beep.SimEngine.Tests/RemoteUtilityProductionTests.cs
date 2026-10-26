using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class RemoteUtilityProductionTests
    {
        internal const string Case = "Remote utilities: actual timed production consumption shortage shutdown and saved telemetry";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static EntityId Find(SimGame game, string definition)
        { var entities = new List<EntityView>(); game.Seat(0).View.CopyEntities(entities); return entities.Single(e => e.Def == definition).Id; }
        private static void Step(SimGame game, SimGame twin = null)
        { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("remote production fault: " + game.Fault + "/" + twin?.Fault); }
        private static UtilityDemandSnapshot Demand(SimGame game, EntityId provider)
        { if (!game.Seat(0).View.Feature<OperationsView>().TryGetUtilityDemand(provider, new Code("point.process"), out var value)) throw new InvalidOperationException("missing native consumer"); return value; }
        private static AutomationSignalSnapshot Signal(SimGame game, EntityId sensor, string id)
        { Code refusal = game.Seat(0).View.Feature<OperationsView>().ReadSignal(sensor, new Code("sensor.remote"), id, out var value); if (!refusal.IsNone) throw new InvalidOperationException("remote signal refused: " + refusal); return value; }
        private static int Queue(SimGame game, EntityId provider)
        { if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(provider, new Code("point.process"), out var value)) throw new InvalidOperationException("missing native production"); return value.QueueCount; }
        private static void Run(Check c)
        {
            foreach (bool independent in new[] { false, true })
            {
                var factory = RemoteSensorTests.Factory(independent: independent, production: true);
                var game = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 734, Seats = { Controller.Human, Controller.Human } });
                var sensor = Find(game, "sensor.machine"); var provider = Find(game, "remote.production-provider");
                game.Seat(0).View.TryFindResource("supply", out var supply);
                var limits = new ReplayLimits(4 * 1024 * 1024, 3000); var recorder = game.RecordReplay(limits);
                var bind = new BindRemoteSignalCommand { Entity = sensor, SignalSource = new Code("sensor.remote"), Provider = provider };
                c.Equal(Code.None, game.Seat(0).Preview(bind), "real native producer binding admitted"); game.Seat(0).Submit(bind); Step(game);
                c.Equal(UtilityCodes.Inactive, Signal(game, sensor, "sensor.utility.ready").Blocker, "idle producer is not invented readiness");
                var enqueue = new EnqueueTransformationCommand { Transformer = provider, Capability = new Code("point.process"), Transformation = "remote.utility-recipe", Count = 1 };
                c.Equal(Code.None, game.Seat(0).Preview(enqueue), "normal native production admission"); game.Seat(0).Submit(enqueue);
                var twin = factory.Load(game.Save()); Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "pending native execution saved exactly");
                int guard = 0; while (Demand(game, provider).Blocker != UtilityCodes.Shortage && guard++ < 16) Step(game, twin);
                c.Equal(UtilityCodes.Shortage, Demand(game, provider).Blocker, "empty actual stock blocks timed operation");
                c.That(Signal(game, sensor, "sensor.utility.ready").Available && Signal(game, sensor, "sensor.utility.ready").Value == Fix.Zero, "remote readiness reports real shortage");
                c.Equal(Fix.Zero, Demand(game, provider).Consumed.AmountOf(supply), "no input fabricated during shortage");
                var route = new AddRouteCommand { From = Find(game, "source"), To = provider, Resource = supply, Settings = new LogisticsRouteSettings(true, Fix.One, 70) };
                c.Equal(Code.None, game.Seat(0).Preview(route), "real hauling admitted for operating material"); game.Seat(0).Submit(route);
                twin = factory.Load(game.Save()); Step(game, twin); guard = 0;
                while (!Demand(game, provider).Consumed.AmountOf(supply).IsPositive && guard++ < 400) Step(game, twin);
                var partial = Demand(game, provider); c.That(partial.Consumed.AmountOf(supply).IsPositive && partial.ElapsedTicks > 0, "native time and physical delivery earn actual operating consumption");
                c.Equal(partial.Consumed.AmountOf(supply), Signal(game, sensor, "remote.consumed").Value.GetValueOrDefault(), "remote consumed quantity is the exact native fact");
                c.That(Queue(game, provider) == 1, "earned partial consumption is not immediate completion");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual partial consumption/remote binding saved");
                var shutdown = new ConfigureOperationCommand { Entity = provider, Operation = new Code("point.process"), Enabled = false, Target = Fix.One };
                c.Equal(Code.None, game.Seat(0).Preview(shutdown), "normal shutdown accepted"); game.Seat(0).Submit(shutdown); twin = factory.Load(game.Save()); Step(game, twin);
                var stopped = Demand(game, provider); for (int i = 0; i < 3; i++) Step(game, twin);
                c.Equal(stopped.ElapsedTicks, Demand(game, provider).ElapsedTicks, "shutdown earns no productive exposure");
                c.Equal(stopped.Consumed.AmountOf(supply), Demand(game, provider).Consumed.AmountOf(supply), "shutdown burns no operating material");
                c.Equal(UtilityCodes.Inactive, Signal(game, sensor, "sensor.utility.ready").Blocker, "remote shutdown fact unavailable, not stale ready");
                var restart = new ConfigureOperationCommand { Entity = provider, Operation = new Code("point.process"), Enabled = true, Target = Fix.One };
                game.Seat(0).Submit(restart); twin = factory.Load(game.Save()); Step(game, twin); guard = 0;
                while (Queue(game, provider) != 0 && guard++ < 500)
                {
                    var native = Demand(game, provider); var remote = Signal(game, sensor, "remote.consumed");
                    if (native.Drawing) c.Equal(native.Consumed.AmountOf(supply), remote.Value.GetValueOrDefault(), "each active remote consumption read uses sole producer state");
                    Step(game, twin);
                }
                c.Equal(0, Queue(game, provider), "actual timed operation completes after delivered fuel");
                c.Equal(UtilityCodes.Inactive, Signal(game, sensor, "remote.consumed").Blocker, "completion does not fabricate a cached lifetime consumer counter");
                var history = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
                c.Equal(ResourceFlowReadStatus.Ready, history.Status, "explicit original committed history available");
                var consumed = history.Aggregates.Where(a => a.Resource == supply && a.Kind == ResourceFlowKind.Consumed && a.Cause == TransformerManager.CauseOperatingInputs)
                    .Aggregate(System.Numerics.BigInteger.Zero, (sum, line) => sum + line.AmountRaw);
                var produced = history.Aggregates.Where(a => a.Resource == supply && a.Kind == ResourceFlowKind.Produced)
                    .Aggregate(System.Numerics.BigInteger.Zero, (sum, line) => sum + line.AmountRaw);
                c.Equal(Fix.FromInt(12).Raw, checked((long)consumed), "twenty-four productive minutes commit twelve real operating units");
                c.Equal(Fix.One.Raw, checked((long)produced), "normal completed result creates one authored unit");
                c.Equal(Fix.FromInt(20), PermanentRemovalTests.Total(game) + Fix.FromRaw(checked((long)consumed)) - Fix.FromRaw(checked((long)produced)), "actual stores/cargo plus native consumed/produced quantities conserved");
                c.Equal(game.Hash(), twin.Hash(), "whole physical continuation identical");
                c.That(!game.Seat(1).View.Feature<OperationsView>().TryGetUtilityDemand(provider, new Code("point.process"), out _), "remote fixture grants no foreign operation knowledge");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "physical production and remote telemetry replay");
            }
        }
    }
}
