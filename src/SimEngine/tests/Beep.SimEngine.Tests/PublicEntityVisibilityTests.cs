using System;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    public static class PublicEntityVisibilityTests
    {
        public const string CaseName = "Public visibility: exact authored prototypes without unrelated disclosure";
        public static void Register() => TestRunner.Add(CaseName, Run);
        private static GameFactory Factory(string world = null)
        {
            var definition = new GameDefinition("public-visibility", new GameModule[]
            { new WorldSetupModule(), new CompanyKnowledgeModule(), new KnowledgeVisibilityModule() });
            var files = new PreloadedFileSource()
                .Add("manifest.json", FrameworkProofContent.Read("public-visibility-master.json"))
                .Add("public-visibility-world.json", world ?? FrameworkProofContent.Read("public-visibility-world.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest
        { Scenario = "public-test", Seed = 17, Seats = new System.Collections.Generic.List<Controller> { Controller.Human, Controller.Human } });
        private static void AssertViews(Check check, SimGame game)
        {
            var town = game.Snapshot.Entities.Single(e => e.Def == "public-test.town");
            var hidden = game.Snapshot.Entities.Single(e => e.Def == "public-test.hidden");
            check.That(game.Seat(0).View.TryGetEntity(town.Id, out _), "authored public town is visible beyond any sensor");
            check.That(!game.Seat(0).View.TryGetEntity(hidden.Id, out _), "another prototype remains undisclosed");
            check.That(game.Seat(1).View.TryGetEntity(hidden.Id, out _), "actual owner's entity remains observable");
            check.That(game.Seat(0).Snapshot.Entities.Any(e => e.Id == town.Id), "public town appears in scoped snapshot");
            check.That(!game.Seat(0).Snapshot.Entities.Any(e => e.Id == hidden.Id), "unrelated prototype absent from scoped snapshot");
            check.That(game.Fault == null, "visibility reads do not fault the session");
        }
        private static void Run(Check check)
        {
            GameFactory factory = Factory(); SimGame game = New(factory);
            AssertViews(check, game);
            SimGame loaded = factory.Load(game.Save()); AssertViews(check, loaded);
            for (int i = 0; i < 3; i++) { game.Update(); loaded.Update(); AssertViews(check, game); AssertViews(check, loaded); }
            check.Equal(game.Hash(), loaded.Hash(), "same native saved continuation");
            var malformed = Json.Parse(FrameworkProofContent.Read("public-visibility-world.json"));
            malformed.Get("entity_public_visibility")[0].Set("definition", JsonValue.Of("absent.prototype"));
            bool refused = false;
            try { New(Factory(malformed.ToString())); } catch (ContentException) { refused = true; }
            check.That(refused, "missing public target refuses; never substitutes another prototype");
        }
    }
}
