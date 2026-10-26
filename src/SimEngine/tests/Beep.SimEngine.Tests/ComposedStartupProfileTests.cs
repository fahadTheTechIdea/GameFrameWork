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
    // JSON authoring, ordinary public player requests and detached observations only.
    internal static class ComposedStartupProfileTests
    {
        internal const string Case = "Composed startup: native profile diamond, point placement, physical work and saved hauling";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(InitializeProbe probe, Action<Dictionary<string, JsonValue>> edit = null)
        {
            var version = new ModuleVersion(1, 0, 0);
            var definition = new GameDefinition("composed-startup", new GameModule[] {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new WorkModule(),
                new OperationsModule(), new TransformationModule(), new LogisticsModule(), new WorldSetupModule(),
                new PointModule(), new ProbeModule("proof.startup", version, compose: c => c.AddSystem(probe, 300)) });
            var documents = new Dictionary<string, JsonValue>();
            foreach (string name in new[] { "composed-startup-master.json", "composed-startup-game.json", "composed-startup-root.json", "composed-startup-shared.json", "composed-startup-branches.json" })
                documents.Add(name, Json.Parse(FrameworkProofContent.Read(name)));
            edit?.Invoke(documents);
            var files = new PreloadedFileSource().Add("manifest.json", documents["composed-startup-master.json"].ToString())
                .Add("open-access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"));
            foreach (var document in documents) if (document.Key != "composed-startup-master.json") files.Add(document.Key, document.Value.ToString());
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "composed-startup", Seed = 811 });
        private static EntityView Entity(SimGame game, string name) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "startup." + name);
        private static Fix Position(SimGame game, string name) => ((PointSnapshot)Entity(game, name).Spatial).Position;
        private static Fix Stock(SimGame game, string name)
        {
            var facts = new List<Facet>();
            if (!game.Seat(0).View.Inspect(Entity(game, name).Id, facts)) throw new InvalidOperationException("missing startup inspection");
            return facts.Single(f => f.Key == Storage.FacetStock && f.Subject == "startup.kit").Value;
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("composed startup Update " + game.Tick + ": " + game.Fault + "/" + twin?.Fault);
        }
        private static string Projection(SimGame game)
            => string.Join(";", game.Seat(0).Snapshot.Entities.Select(e => e.Id + "/" + e.Def + "/" + ((PointSnapshot)e.Spatial).Position));
        private static void Reverse(JsonValue parent, string key)
        {
            var current = parent.Get(key); var reversed = JsonValue.NewArray();
            for (int i = current.Count - 1; i >= 0; i--) reversed.Add(current[i]); parent.Set(key, reversed);
        }
        private static void Run(Check c)
        {
            var probe = new InitializeProbe(); var factory = Factory(probe); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "new composed profile semantics have exact engine identity");
            c.Equal(154, EngineInfo.SaveFormat, "saved field layout unchanged");
            c.Equal(1, probe.Initializations, "valid composed profiles initialize once");
            c.Equal(3, game.Seat(0).Snapshot.Entities.Count, "diamond shared entry spawns once and unselected profile spawns nothing");
            c.Equal(Fix.FromInt(20), Stock(game, "source"), "shared initial stock is granted exactly once");
            c.Equal(Fix.Zero, Stock(game, "destination"), "destination opening inventory is actual authored zero");
            c.Equal(Fix.FromInt(12), Position(game, "source"), "explicit placement overrides prototype opening position");
            c.Equal(Fix.FromInt(4), Position(game, "machine"), "worker receives actual native point placement");
            c.Equal(Fix.FromInt(22), Position(game, "destination"), "sibling branch uses actual authored point coordinates");
            c.That(!game.Modules.Contains("sim.tile-navigation") && !game.Modules.Contains(TycoonModule.ModuleId), "native composition requires no raster or money");
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().ReadHome(Entity(game, "machine").Id, out var home), "public home observation succeeds");
            c.Equal(Entity(game, "source").Id, home.Home, "cross-file graph dependency binds actual same-company home");
            var reordered = New(Factory(new InitializeProbe(), docs => {
                foreach (string name in new[] { "composed-startup-root.json", "composed-startup-shared.json", "composed-startup-branches.json" })
                {
                    Reverse(docs[name], "world_setup_profiles");
                    if (docs[name].Get("entity_setups") != null) Reverse(docs[name], "entity_setups");
                    for (int i = 0; i < docs[name].Get("world_setup_profiles").Count; i++) Reverse(docs[name].Get("world_setup_profiles")[i], "includes");
                }
            }));
            c.Equal(Projection(game), Projection(reordered), "profile, include and entry authoring order cannot reorder native entity identities");
            var recording = game.RecordReplay(new ReplayLimits(1024 * 1024, 600));
            var post = new PostOrderCommand { Plan = "startup.inspect", Target = Entity(game, "destination").Id };
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(post), "native point work preview admits");
            c.Equal(hash, game.Hash(), "preview reserves and progresses nothing");
            c.That(TestKit.Order(game, 0, post).Accepted, "ordinary player command posts real staffed work");
            c.Equal(1, game.Seat(0).View.Feature<WorkView>().ActiveOrders(Entity(game, "destination").Id), "travel and work are not instantly completed");
            bool moved = false, reached = false; int updates = 0;
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "travelling native startup/work saves exactly");
            c.Equal(3, twin.Seat(0).Snapshot.Entities.Count, "restore does not execute the startup diamond again");
            c.Equal(Fix.FromInt(20), Stock(twin, "source"), "restore grants no duplicate opening stock");
            while (game.Seat(0).View.Feature<WorkView>().ActiveOrders(Entity(game, "destination").Id) != 0 && updates++ < 200)
            { Step(game, twin); moved |= Position(game, "machine") != Fix.FromInt(4); reached |= Position(game, "machine") == Fix.FromInt(22); }
            c.That(updates < 200 && moved, "engine physically moves and finishes authored timed work within its bound");
            c.That(reached, "real staffed work reaches the actual target before ordinary return-home motion");
            c.That(game.Seat(0).View.TryFindResource("startup.kit", out var material), "public stable resource lookup succeeds");
            var haul = new HaulResourcesCommand { Source = Entity(game, "source").Id, Destination = Entity(game, "destination").Id, Resource = material, Ceiling = Fix.FromInt(4), Worker = Entity(game, "machine").Id, Priority = 100 };
            c.That(TestKit.Order(game, 0, haul).Accepted, "ordinary native hauling uses composed startup identities");
            c.Equal(Fix.Zero, Stock(game, "destination"), "accepted haul does not teleport resources");
            twin = factory.Load(game.Save()); updates = 0;
            while (Stock(game, "destination") != Fix.FromInt(4) && updates++ < 200) Step(game, twin);
            c.That(updates < 200, "physical pickup, travel and handling deliver within bounded updates");
            c.Equal(Fix.FromInt(16), Stock(game, "source"), "real pickup consumes only delivered cargo");
            c.Equal(Fix.FromInt(4), Stock(game, "destination"), "actual completed delivery commits once");
            c.Equal(3, game.Seat(0).Snapshot.Entities.Count, "work and hauling create no duplicate startup entities");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), new ReplayLimits(1024 * 1024, 600)).Success, "native profile startup/work/haul replay identically");
            foreach (string invalid in new[] { "missing", "self", "cycle", "repeat-include", "repeat-entry", "required-field", "required-entries", "null-includes" })
            {
                var refused = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(refused, docs => {
                    var root = docs["composed-startup-root.json"].Get("world_setup_profiles")[0];
                    var shared = docs["composed-startup-shared.json"].Get("world_setup_profiles")[0];
                    if (invalid == "missing") root.Get("includes").Add(JsonValue.Of("absent"));
                    if (invalid == "self") root.Get("includes").Add(JsonValue.Of("startup.root"));
                    if (invalid == "cycle") shared.Get("includes").Add(JsonValue.Of("startup.root"));
                    if (invalid == "repeat-include") root.Get("includes").Add(JsonValue.Of("startup.alpha"));
                    if (invalid == "repeat-entry") shared.Get("entries").Add(JsonValue.Of("startup.source.start"));
                    if (invalid == "required-field") root.Remove("includes");
                    if (invalid == "required-entries") root.Remove("entries");
                    if (invalid == "null-includes") root.Set("includes", JsonValue.Null());
                })), "composed profile refuses " + invalid);
                c.Equal(0, refused.Initializations, invalid + " refuses before system initialization");
            }
        }
    }
}
