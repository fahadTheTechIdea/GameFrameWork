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
    // Explicit JSON inputs, public players/views and bounded Update only.
    internal static class NativePointStartupTests
    {
        internal const string Case = "Native point startup: explicit selection, physical work and saved delivery without provider spawning";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(InitializeProbe probe, bool include = false, bool enabled = true, Action<JsonValue> edit = null)
        {
            JsonValue root = Json.Parse(PointGame.Definitions), scenario = root.Get("scenarios")[0];
            scenario.Set("seats", JsonValue.Number("2"));
            scenario.Get("modules").Add(JsonValue.Of("proof.native-startup"));
            StartupContentAuthoring.Position(root, "source", JsonValue.Number("2"));
            StartupContentAuthoring.Position(root, "destination", JsonValue.Number("18"));
            StartupContentAuthoring.Position(root, "machine", JsonValue.Number("4"));
            if (!enabled)
            {
                var selected = JsonValue.NewArray();
                for (int i = 0; i < scenario.Get("modules").Count; i++)
                    if (scenario.Get("modules")[i].Text != WorldSetupModule.ModuleId) selected.Add(scenario.Get("modules")[i].Clone());
                scenario.Set("modules", selected); scenario.Get("settings").Remove(WorldSetupModule.ModuleId);
                root.Remove("entity_setups"); root.Remove("world_setup_profiles");
            }
            edit?.Invoke(root);
            return PointGame.Factory(root.ToString(),
                extraModules: new GameModule[] { new ProbeModule("proof.native-startup", new ModuleVersion(1, 0, 0), compose: c => c.AddSystem(probe, 300)) },
                extraDeclarations: "[{\"id\":\"proof.native-startup\",\"version\":\"1.0.0\",\"documents\":[]}]",
                additionalDocuments: enabled ? new Dictionary<string, string> { ["native-point-startup-extra.json"] = FrameworkProofContent.Read("native-point-startup-extra.json") } : null,
                startupProfiles: include ? new[] { "point.extra", "point.extra.shared" } : null);
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "points", Seed = 821, Seats = { Controller.Human, Controller.Human } });
        private static EntityView Entity(SimGame game, string prototype, int company = 0)
            => game.Seat(company).Snapshot.Entities.Single(e => e.Def == prototype && e.Owner == new PlayerId(company));
        private static Fix Position(SimGame game, string prototype, int company = 0) => ((PointSnapshot)Entity(game, prototype, company).Spatial).Position;
        private static Fix Stock(SimGame game, string prototype, int company = 0)
        {
            var facts = new List<Facet>();
            if (!game.Seat(company).View.Inspect(Entity(game, prototype, company).Id, facts)) throw new InvalidOperationException("missing native startup inspection");
            return facts.Single(f => f.Key == Storage.FacetStock && f.Subject == "supply").Value;
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("native startup Update " + game.Clock.StepIndex + ": " + game.Fault + "/" + twin?.Fault);
        }
        private static void Run(Check c)
        {
            var probe = new InitializeProbe(); var factory = Factory(probe); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "uses current native engine identity; no new runtime owner");
            c.Equal(154, EngineInfo.SaveFormat, "saved layout unchanged");
            c.Equal(1, probe.Initializations, "normal startup initializes once");
            c.Equal(3, game.Seat(0).Snapshot.Entities.Count, "loaded but unselected included content spawns nothing");
            c.Equal(Fix.FromInt(2), Position(game, "source"), "actual startup placement is not prototype position0");
            c.Equal(Fix.FromInt(18), Position(game, "destination"), "actual destination is not prototype position10");
            c.Equal(Fix.FromInt(4), Position(game, "machine"), "actual worker native placement overrides prototype position0");
            c.Equal(Fix.FromInt(20), Stock(game, "source"), "actual initial inventory granted once");
            c.Equal(Fix.Zero, Stock(game, "destination"), "actual destination opens empty");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId) && !game.Modules.Contains("sim.tile-navigation"), "physical point startup needs neither business nor tile navigation");

            var selectedFactory = Factory(new InitializeProbe(), include: true); var selected = New(selectedFactory);
            c.Equal(5, selected.Seat(0).Snapshot.Entities.Count, "explicit native includes execute shared aliases once");
            c.Equal(Fix.FromInt(40), Position(selected, "source", 1), "foreign source has its explicitly authored actual placement");
            c.Equal(Fix.FromInt(35), Position(selected, "machine", 1), "foreign machine has its explicit placement");
            c.Equal(Fix.FromInt(20), Stock(selected, "source", 1), "shared profile alias does not duplicate opening stock");
            c.Equal(Code.None, selected.Seat(1).View.Feature<WorkView>().ReadHome(Entity(selected, "machine", 1).Id, out var home), "native private home observation succeeds");
            c.Equal(Entity(selected, "source", 1).Id, home.Home, "actual cross-entry dependency binds same-company home");
            var selectedTwin = selectedFactory.Load(selected.Save());
            c.Equal(selected.Hash(), selectedTwin.Hash(), "composed startup saves without executing aliases again");
            c.Equal(5, selectedTwin.Seat(0).Snapshot.Entities.Count, "load never creates repeated startup entities");
            c.Equal(Fix.FromInt(20), Stock(selectedTwin, "source", 1), "load never repeats initial stock");
            var disabled = New(Factory(new InitializeProbe(), enabled: false));
            c.Equal(0, disabled.Seat(0).Snapshot.Entities.Count, "point provider has no startup executor when native startup is not selected");

            var recording = game.RecordReplay(new ReplayLimits(1024 * 1024, 600));
            var command = new PostOrderCommand { Plan = "inspect", Target = Entity(game, "destination").Id };
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary engine work preview admits");
            c.Equal(hash, game.Hash(), "preview does not create work or change resource state");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary player posts physical timed work");
            c.Equal(1, game.Seat(0).View.Feature<WorkView>().ActiveOrders(Entity(game, "destination").Id), "accepted work is not immediately completed");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "travelling native work saves");
            bool moved = false, arrived = false; int updates = 0;
            while (game.Seat(0).View.Feature<WorkView>().ActiveOrders(Entity(game, "destination").Id) != 0 && updates++ < 160)
            {
                Step(game, twin); moved |= Position(game, "machine") != Fix.FromInt(4);
                arrived |= Position(game, "machine") == Fix.FromInt(18);
            }
            c.That(updates < 160 && moved && arrived, "engine moves to actual target and earns authored work over bounded game time");
            c.That(game.Seat(0).View.TryFindResource("supply", out var resource), "stable resource resolves publicly");
            var haul = new HaulResourcesCommand { Source = Entity(game, "source").Id, Destination = Entity(game, "destination").Id,
                Worker = Entity(game, "machine").Id, Resource = resource, Ceiling = Fix.FromInt(4), Priority = 100 };
            c.That(TestKit.Order(game, 0, haul).Accepted, "ordinary native hauling uses actual setup entities");
            c.Equal(Fix.Zero, Stock(game, "destination"), "accepted intent teleports no cargo");
            twin = factory.Load(game.Save()); updates = 0;
            while (Stock(game, "destination") != Fix.FromInt(4) && updates++ < 160) Step(game, twin);
            c.That(updates < 160, "physical pickup, movement and handling deliver within bound");
            c.Equal(Fix.FromInt(16), Stock(game, "source"), "only actual delivered cargo leaves source");
            c.Equal(Fix.FromInt(4), Stock(game, "destination"), "physical delivery commits exactly once");
            c.Equal(3, game.Seat(0).Snapshot.Entities.Count, "updates/load never repeat native startup");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), new ReplayLimits(1024 * 1024, 600)).Success, "physical native startup/work/hauling replay identically");

            foreach (string invalid in new[] { "obsolete-section", "missing-placement", "spatial-null", "missing-entry", "entry-cycle" })
            {
                var refused = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(refused, edit: root => {
                    if (invalid == "obsolete-section") root.Set("point_" + "starts", JsonValue.NewArray());
                    if (invalid == "missing-placement") root.Get("entity_setups")[0].Remove("placement");
                    if (invalid == "spatial-null") root.Get("entity_setups")[0].Set("placement", JsonValue.Null());
                    if (invalid == "missing-entry") root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("missing"));
                    if (invalid == "entry-cycle") root.Get("entity_setups")[0].Get("after").Add(JsonValue.Of("source"));
                })), "native grammar rejects " + invalid);
                c.Equal(0, refused.Initializations, invalid + " rejects before system Initialize");
            }
        }
    }
}
