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
    internal static class StartupLifecycleCutoffTests
    {
        internal const string Case = "Startup lifecycle: first faults stop initialization generation new-game callbacks and preserve real saved work";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(StartupLifecycleProofModule module, string phase = null, string failure = "none")
        {
            var root = Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module.Id));
            var document = Json.Parse(FrameworkProofContent.Read("startup-lifecycle.json"));
            var profile = document.Get("startup_lifecycle_profiles")[0];
            profile.Set("phase", phase == null ? JsonValue.Null() : JsonValue.Of(phase)); profile.Set("failure", JsonValue.Of(failure));
            return PointGame.Factory(root.ToString(), extraModules: new[] { module },
                extraDeclarations: "[{\"id\":\"proof.startup-lifecycle\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.world-setup\",\"version\":\"1.0.0\"}],\"documents\":[\"startup-lifecycle.json\"]}]",
                extraDocuments: new Dictionary<string, string> { ["startup-lifecycle.json"] = document.ToString() });
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 911, Seats = { Controller.Human } });
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current startup core identity");
            Negative(c, "initialize", "caught-create");
            foreach (string phase in new[] { "initialize", "generate", "new-game" })
                foreach (string failure in new[] { "direct", "caught-create" })
                    if (phase != "initialize" || failure != "caught-create") Negative(c, phase, failure);
            Positive(c);
        }
        private static void Negative(Check c, string phase, string failure)
        {
            var module = new StartupLifecycleProofModule(); var factory = Factory(module, phase, failure); Exception caught = null;
            try { New(factory); } catch (InvalidOperationException error) { caught = error; }
            string label = phase + "/" + failure; var session = module.Last;
            c.That(caught != null, label + " failed startup cannot return a playable session");
            if (session == null) throw new InvalidOperationException("startup never reached its intended callback: " + label);
            int later = phase == "initialize" ? session.LaterInitialize + session.FirstGeneration + session.LaterGeneration + session.FirstNewGame + session.LaterNewGame
                : phase == "generate" ? session.LaterGeneration + session.FirstNewGame + session.LaterNewGame : session.LaterNewGame;
            c.Equal(0, later, label + " no later startup callback after first fault");
            if (later != 0) throw new InvalidOperationException("later startup callback escaped first fault: " + label + ";count=" + later);
            c.That(session.World.Fault != null, label + " original world owns the terminal startup fault");
            if (failure == "caught-create")
            {
                c.Equal(1, session.Caught, label + " actual failed entity creation was caught by provider");
                c.Equal("entity.create", session.World.Fault?.Phase, label + " earlier creation cause is never replaced");
                c.That(session.World.Fault?.Message.Contains("proof result OnCreate failure") == true, label + " original creation exception retained");
            }
            else
            {
                c.That(caught?.Message == "proof.startup-original-error" && session.World.Fault?.Message.Contains("proof.startup-original-error") == true,
                    label + " uncaught provider error is not replaced");
                c.Equal(phase == "generate" ? "world.generate" : phase + ".proof.startup-first", session.World.Fault?.Phase,
                    label + " direct failure names its original startup phase");
            }
            c.That(!session.World.Generating, label + " generation permission is released on failure");
            c.Throws<InvalidOperationException>(() => session.World.RequireHealthyUpdate(), label + " terminal world cannot be reused");
            c.Throws<InvalidOperationException>(() => SaveGame.Write(session.World, "point-proof"), label + " failed startup cannot be saved as healthy");
        }
        private static void Positive(Check c)
        {
            var module = new StartupLifecycleProofModule(); var factory = Factory(module); var game = New(factory); var first = module.Last;
            c.That(first.FirstInitialize == 1 && first.LaterInitialize == 1 && first.FirstGeneration == 1 && first.LaterGeneration == 1 && first.FirstNewGame == 1 && first.LaterNewGame == 1,
                "healthy original lifecycle invokes every startup phase exactly once");
            c.That(game.Fault == null && !game.Modules.Contains(TycoonModule.ModuleId), "resource-only engine startup healthy");
            var entities = game.Seat(0).Snapshot.Entities; var source = entities.Single(e => e.Def == "source").Id;
            var destination = entities.Single(e => e.Def == "destination").Id; var worker = entities.Single(e => e.Def == "machine").Id;
            c.That(game.Seat(0).View.TryFindResource("supply", out var supply), "actual authored material");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000); using var recorder = game.RecordReplay(limits);
            var command = new HaulResourcesCommand { Source = source, Destination = destination, Worker = worker, Resource = supply, Ceiling = Fix.FromInt(4), Priority = 100 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary physical work preview after startup");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary physical work starts through engine");
            var twin = factory.Load(game.Save()); var restored = module.Last;
            c.Equal(game.Hash(), twin.Hash(), "real in-flight work restores");
            c.That(restored.FirstInitialize == 1 && restored.LaterInitialize == 1 && restored.FirstGeneration == 0 && restored.LaterGeneration == 0 && restored.FirstNewGame == 0 && restored.LaterNewGame == 0,
                "load initializes once without regenerating or replaying new-game callbacks");
            Fix Stock(SimGame session, EntityId entity)
            {
                var refusal = session.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity, supply, out var inventory);
                if (!refusal.IsNone) throw new InvalidOperationException("actual inventory unavailable: " + refusal);
                return inventory.Stock;
            }
            for (int i = 0; i < 300 && Stock(game, destination) != Fix.FromInt(4); i++)
            {
                game.Update(); twin.Update();
                if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("healthy startup physical continuation faulted");
            }
            c.Equal(Fix.FromInt(4), Stock(game, destination), "actual physical delivery completes over original game time");
            c.Equal(Fix.FromInt(16), Stock(game, source), "actual finite source material consumed once");
            c.Equal(game.Hash(), twin.Hash(), "saved physical continuation remains deterministic");
            c.That(first.FirstInitialize == 1 && first.FirstGeneration == 1 && first.FirstNewGame == 1, "running Update never repeats startup phases");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "healthy startup and actual hauling replay through original engine");
        }
    }
}
