using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class RemoteSignalLifecycleTests
    {
        internal const string Case = "Remote signal lifecycle: physical provider replacement and removal preserve private saved references";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static EntityId Find(SimGame game, string definition)
        { var entities = new List<EntityView>(); game.Seat(0).View.CopyEntities(entities); return entities.Single(e => e.Def == definition).Id; }
        private static void Step(SimGame game, SimGame twin)
        { game.Update(); twin.Update(); if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("remote lifecycle fault: " + game.Fault + "/" + twin.Fault); }
        internal static RemoteSignalBindingSnapshot Binding(SimGame game, EntityId sensor)
        {
            Code refusal = game.Seat(0).View.Feature<RemoteSignalsView>().ReadBinding(sensor, new Code("sensor.remote"), out var fact);
            if (!refusal.IsNone) throw new InvalidOperationException("binding read: " + refusal); return fact;
        }
        private static int Post(Check c, SimGame game, string activity, EntityId target)
        {
            var command = new PostOrderCommand { Plan = activity, Target = target, Priority = 80 };
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary physical lifecycle activity admitted");
            c.Equal(before, game.Hash(), "physical preview pure"); return game.Seat(0).Submit(command);
        }
        private static int Order(SimGame game, int sequence)
        { long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers); var result = answers.Single(r => r.Sequence == sequence); if (!result.Accepted) throw new InvalidOperationException("lifecycle order refused: " + result.Refusal); return (int)result.Arg0; }
        private static void Earn(Check c, SimGame game, SimGame twin, int order, EntityId target)
        {
            int steps = 0; var work = game.Seat(0).View.Feature<WorkView>(); var nodes = new List<ActivityNodeView>();
            while (steps++ < 128)
            {
                if (work.TryCopyNodes(order, nodes) && nodes.Any(n => n.Progress.WholeTicks > 0))
                { c.That(game.Seat(0).View.TryGetEntity(target, out _), "positive physical work does not replace/retire early"); return; }
                Step(game, twin);
            }
            throw new InvalidOperationException("no positive lifecycle work at" + game.Tick + "/order" + order);
        }
        private static void Run(Check c)
        {
            foreach (bool independent in new[] { false, true })
            {
                var factory = RemoteSensorTests.Factory(independent: independent, lifecycle: true);
                var game = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 734, Seats = { Controller.Human, Controller.Human } });
                var sensor = Find(game, "sensor.machine"); var original = Find(game, "utility.remote-alternative"); var untouched = Find(game, "utility.remote-provider");
                var limits = new ReplayLimits(4 * 1024 * 1024, 2000); var recording = game.RecordReplay(limits);
                var link = new BindRemoteSignalCommand { Entity = sensor, SignalSource = new Code("sensor.remote"), Provider = original };
                c.Equal(Code.None, game.Seat(0).Preview(link), "normal link admission"); game.Seat(0).Submit(link);
                var twin = factory.Load(game.Save()); Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "saved bind command identical");
                int submitted = Post(c, game, "remote.upgrade", original); twin = factory.Load(game.Save()); Step(game, twin);
                int upgrade = Order(game, submitted); Earn(c, game, twin, upgrade, original); twin = factory.Load(game.Save());
                c.Equal(game.Hash(), twin.Hash(), "earned partial replacement checkpoint");
                int steps = 0; while (Binding(game, sensor).Provider == original && steps++ < 128) Step(game, twin);
                var replaced = Binding(game, sensor); c.That(replaced.Provider.IsValid && replaced.Provider != original && replaced.Provider != untouched, "sole reference retargeted to actual replacement, not another compatible provider");
                c.That(!game.Seat(0).View.TryGetEntity(original, out _), "original physically retired after complete work");
                c.Equal(Code.None, replaced.Blocker, "actual compatible replacement remains bound");
                c.Equal(game.Hash(), twin.Hash(), "replacement saved continuation identical");
                c.That(game.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:remote.upgrade", EntityId.None, out var completed), "engine publishes company-scoped completion history after replacement");
                c.Equal(upgrade, completed.Order, "stable original activity order identity retained");
                submitted = Post(c, game, "remote.remove", replaced.Provider); twin = factory.Load(game.Save()); Step(game, twin);
                int removal = Order(game, submitted); Earn(c, game, twin, removal, replaced.Provider); twin = factory.Load(game.Save());
                steps = 0; while (game.Seat(0).View.TryGetEntity(replaced.Provider, out _) && steps++ < 128) Step(game, twin);
                c.That(!game.Seat(0).View.TryGetEntity(replaced.Provider, out _), "provider physically removed after elapsed staffed work");
                var lost = Binding(game, sensor); c.Equal(replaced.Provider, lost.Provider, "lost reference preserves selected historical identity");
                c.Equal(Codes.NotFound, lost.Blocker, "removed provider blocks instead of falling back");
                c.Equal(Code.None, game.Seat(0).View.Feature<OperationsView>().ReadSignal(sensor, new Code("sensor.remote"), "sensor.utility.ready", out var value), "read publishes typed unavailability");
                c.That(!value.Available && value.Blocker == Codes.NotFound, "removed endpoint exposes no stale utility readiness");
                c.That(game.Seat(0).View.TryGetEntity(untouched, out _), "another compatible provider remains alive but is not substituted");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "dangling historical reference saves/restores exactly");
                c.Equal(Codes.NotFound, Binding(twin, sensor).Blocker, "saved removed target remains blocked");
                c.Equal(Codes.NotFound, game.Seat(1).View.Feature<RemoteSignalsView>().ReadBinding(sensor, new Code("sensor.remote"), out _), "replacement/removal never publishes foreign private reference");
                c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "physical replacement/removal replay identical");
            }
        }
    }
}
