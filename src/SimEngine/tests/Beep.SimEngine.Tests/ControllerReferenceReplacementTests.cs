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
    internal static class ControllerReferenceReplacementTests
    {
        internal const string Case = "Controller replacement references: dormant private intent cadence replies and ordinary physical continuation";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool direct = false, bool half = false, string mode = "normal")
        {
            var document = Json.Parse(FrameworkProofContent.Read("controller-replacement.json"));
            document.Get("replacement_controllers")[0].Set("mode", JsonValue.Of(mode));
            return EntityReferenceReplacementTests.Factory(external: direct,
                extraModules: new GameModule[] { new ControllerReferenceReplacementModule() },
                extraDeclarations: """[{"id":"sim.player-controllers","version":"1.0.0","documents":["controller-replacement.json"]}]""",
                extraDocuments: new Dictionary<string, string> { ["controller-replacement.json"] = document.ToString() },
                edit: root =>
                {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(PlayerControllersModule.ModuleId));
                    root.Get("scenarios")[0].Get("settings").Set(PlayerControllersModule.ModuleId,
                        Json.Parse("""{"companies":[{"company":0,"profile":"replacement.controller"},{"company":1,"profile":"replacement.foreign-controller"}]}"""));
                    if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
                });
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest
        { Scenario = "points", Seed = 41, Seats = new List<Controller> { Controller.Human, Controller.Human }, Mods = { ResourceFlowsModule.ModuleId } });
        private static EntityId Find(SimGame game, string definition) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == definition).Id;
        private static PlayerControllerFacts Facts(SimGame game, int seat = 0)
        {
            var rows = new List<PlayerControllerFacts>(); game.Seat(seat).View.Feature<PlayerControllersView>().CopyControllers(rows);
            return rows.Single();
        }
        private static List<OrderView> Orders(SimGame game)
        {
            var rows = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(rows); return rows;
        }
        private static bool Completed(SimGame game, EntityId target)
            => game.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:inspect", target, out _);
        private static void Send(Check c, SimGame game, Command command)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary pure player admission");
            c.Equal(before, game.Hash(), "preview preserves controller and clock state");
            int sequence = game.Seat(0).Submit(command); game.Update();
            if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString());
            long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
            c.That(answers.Any(a => a.Sequence == sequence && a.Accepted), "ordinary inbox accepts player input");
        }
        private static void StartDormant(Check c, SimGame game)
        {
            Send(c, game, new SetControllerCommand { Controller = Controller.Autopilot });
            c.That(Facts(game).Decisions == 1 && Facts(game).AwaitingCommands == 1, "strategy submits exactly one ordinary command at its first decision");
            Send(c, game, new SetControllerCommand { Controller = Controller.Human });
            c.That(Facts(game).PendingAnswers == 1 && Facts(game).AwaitingCommands == 0, "dormant strategy retains its actual unconsumed reply");
        }
        private static void Run(Check c)
        {
            foreach (bool direct in new[] { false, true }) foreach (bool half in new[] { false, true })
            {
                var factory = Factory(direct, half); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
                StartDormant(c, game); var old = Find(game, "source"); var schedule = Facts(game); var foreign = Facts(game, 1);
                Send(c, game, new PostOrderCommand { Target = old, Plan = "upgrade" }); var twin = factory.Load(game.Save());
                int steps = 0;
                while (!game.Seat(0).Snapshot.Entities.Any(e => e.Def == "source.next") && steps++ < 45)
                {
                    game.Update(); twin.Update();
                    if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("controller replacement fault: " + game.Fault + "/" + twin.Fault);
                    c.Equal(game.Hash(), twin.Hash(), "pending physical replacement preserves interleaved strategy state");
                }
                var made = Find(game, "source.next"); var retained = Facts(game);
                c.That(made != old && retained.Decisions == schedule.Decisions && retained.LastDecision == schedule.LastDecision &&
                    retained.NextDecision == schedule.NextDecision && retained.PendingAnswers == schedule.PendingAnswers && retained.AwaitingCommands == schedule.AwaitingCommands,
                    "reference handoff preserves dormant cadence and actual pending reply");
                c.That(Facts(game, 1).Decisions == foreign.Decisions && Facts(game, 1).PendingAnswers == foreign.PendingAnswers,
                    "other company receives no replacement callback or decision change");
                game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
                foreach (var session in new[] { game, twin }) Send(c, session, new SetControllerCommand { Controller = Controller.Autopilot });
                steps = 0; bool partial = false;
                while (!Completed(game, made) && steps++ < 65)
                {
                    game.Update(); twin.Update();
                    if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("controller resume fault: " + game.Fault + "/" + twin.Fault);
                    partial |= Orders(game).Any(o => o.Target == made && o.Plan == "activity:inspect" &&
                        o.State == OrderState.Active && o.CurrentProgress.WholeTicks > 0);
                    c.Equal(game.Hash(), twin.Hash(), "resumed ordinary strategy and real work share every update");
                }
                c.That(Completed(game, made) && partial,
                    "resumed strategy consumes original reply and orders physically completed work on actual replacement; " +
                    string.Join(";", Orders(game).Select(o => o.Target + "/" + o.Plan + "/" + o.State + "/" + o.Blocker)) +
                    "; controller=" + Facts(game).Blocker + "/decisions=" + Facts(game).Decisions + "/pending=" + Facts(game).PendingAnswers);
                c.That(!Orders(game).Any(o => o.Target == old && o.Plan == "activity:inspect"), "strategy never orders work against retired live intent");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed controller reference and history checkpoint through original codec");
                c.Equal(isolatedHash, isolated.Hash(), "nested strategies remain isolated between sessions");
            }
            {
                var factory = Factory(mode: "deny"); var game = New(factory); StartDormant(c, game);
                var command = new PostOrderCommand { Target = Find(game, "source"), Plan = "upgrade" }; ulong before = game.Hash();
                var refusal = new Code("proof.controller-replacement-denied");
                c.Equal(refusal, game.Seat(0).Preview(command), "nested controller participates in pure replacement admission");
                c.Equal(before, game.Hash(), "controller refusal preserves actual dormant intent");
                int sequence = game.Seat(0).Submit(command); game.Update(); long cursor = 0; var answers = new List<CommandResult>();
                game.Seat(0).ReadResults(ref cursor, answers);
                c.That(answers.Any(a => a.Sequence == sequence && a.Refusal == refusal), "controller preview and inbox refuse identically");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused controller reference stays checkpointable");
            }
            foreach (string mode in new[] { "mutate-check", "throw-commit" })
            {
                var factory = Factory(mode: mode); var game = New(factory); StartDormant(c, game);
                var command = new PostOrderCommand { Target = Find(game, "source"), Plan = "upgrade" };
                if (mode == "mutate-check") c.Throws<InvalidOperationException>(() => game.Seat(0).Preview(command), "mutating nested reference admission faults");
                else
                {
                    Send(c, game, command); int steps = 0;
                    while (game.Fault == null && steps++ < 45) { try { game.Update(); } catch (InvalidOperationException) { if (game.Fault == null) throw; } }
                }
                c.That(game.Fault != null, "nested controller fault latches on original world boundary");
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted strategy cannot emit a healthy checkpoint");
                c.Throws<InvalidOperationException>(() => game.Hash(), "faulted strategy cannot emit a healthy deterministic hash");
            }
        }
    }
}
