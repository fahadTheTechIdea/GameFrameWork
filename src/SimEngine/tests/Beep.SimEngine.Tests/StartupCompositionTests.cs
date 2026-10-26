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
    // Explicit JSON authoring, public commands/views and bounded Update. No startup/gameplay mutation.
    internal static class StartupCompositionTests
    {
        internal const string Case = "Startup composition: selected declarations refuse before Initialize and preserve physical saved work";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(InitializeProbe init, StartupStageProbe stage, Action<JsonValue> edit = null,
            bool extensionRefusal = false, bool decoderRefusal = false, bool contractRoot = false, Action<JsonValue> editExtra = null)
        {
            var root = Json.Parse(PointGame.Definitions);
            var scenario = root.Get("scenarios")[0]; scenario.Set("seats", JsonValue.Number("2"));
            scenario.Get("modules").Add(JsonValue.Of("proof.startup-admission"));
            StartupContentAuthoring.Position(root, "source", JsonValue.Number("2"));
            StartupContentAuthoring.Position(root, "destination", JsonValue.Number("18"));
            StartupContentAuthoring.Position(root, "machine", JsonValue.Number("4"));
            root.Get("entity_setups")[2].Set("after", Json.Parse("[\"source\"]"));
            root.Get("entity_setups")[2].Set("home", JsonValue.Of("source"));
            StartupContentAuthoring.Include(root, "admission.extra");
            if (contractRoot) scenario.Get("settings").Get(WorldSetupModule.ModuleId).Set("profile", JsonValue.Of("admission.contract-root"));
            edit?.Invoke(root);
            var extra = Json.Parse(FrameworkProofContent.Read("startup-composition-extra.json"));
            extra.Get("startup_admission_probes")[0].Set("refuse", JsonValue.Of(extensionRefusal));
            editExtra?.Invoke(extra);
            var module = new ProbeModule("proof.startup-admission", new ModuleVersion(1, 0, 0),
                requires: new[] { new ModuleRequirement(WorldSetupModule.ModuleId, new ModuleVersion(1, 0, 0)) },
                content: types => { types.Section<StartupAdmissionProbeDef>("startup_admission_probes");
                    types.Section<ContractStartupProfileDef>("contract_world_setup_profiles"); },
                compose: composer => stage.Compose(composer, decoderRefusal));
            return PointGame.Factory(root.ToString(), extraModules: new GameModule[] { module },
                extraDeclarations: "[{\"id\":\"proof.startup-admission\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.world-setup\",\"version\":\"1.0.0\"}],\"documents\":[\"startup-composition-extra.json\"]}]",
                extraDocuments: new Dictionary<string, string> { ["startup-composition-extra.json"] = extra.ToString() },
                startupProfiles: null);
        }
        private static SimGame New(GameFactory factory, int seats = 2)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 829 };
            for (int i = 0; i < seats; i++) request.Seats.Add(Controller.Human);
            return factory.NewGame(request);
        }
        private static EntityView Entity(SimGame game, string prototype) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == prototype);
        private static Fix Position(SimGame game) => ((PointSnapshot)Entity(game, "machine").Spatial).Position;
        private static Fix Stock(SimGame game, string prototype)
        {
            var facts = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, prototype).Id, facts);
            return facts.Single(f => f.Key == Storage.FacetStock && f.Subject == "supply").Value;
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("startup admission Update " + game.Clock.StepIndex + ": " + game.Fault + "/" + twin?.Fault);
        }
        private static void Run(Check c)
        {
            var init = new InitializeProbe(); var stage = new StartupStageProbe(init);
            var factory = Factory(init, stage); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "startup validation semantics have exact engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current saved field/layout identity");
            c.Equal(1, stage.Prepared, "aliased service/stage prepares once");
            c.Equal(1, stage.Validated, "aliased service/stage validates once before Initialize");
            c.Equal(1, init.Initializations, "valid composition initializes systems once");
            c.Equal(1, stage.Generated, "new game invokes original generation pipeline once");
            c.Equal(1, stage.EntryValidated, "external native node participates without native type switch");
            c.Equal(1, stage.EntryGenerated, "external selected native graph entry executes once");
            c.Equal(3, game.Seat(0).Snapshot.Entities.Count, "unselected invalid-company startup intent executes nothing");
            c.Equal(Fix.FromInt(20), Stock(game, "source"), "actual source stock granted once");
            c.Equal(Fix.FromInt(4), Position(game), "native decoded actual worker placement preserved");
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().ReadHome(Entity(game, "machine").Id, out var home), "public home read admits");
            c.Equal(Entity(game, "source").Id, home.Home, "composition declaration becomes actual same-company home only during generation");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId) && !game.Modules.Contains("sim.tile-navigation"), "startup has no economic/tile prerequisite");
            ulong opening = game.Hash();

            var twin = factory.Load(game.Save());
            c.Equal(opening, twin.Hash(), "restore preserves actual startup state");
            c.Equal(2, stage.Prepared, "restore prepares stage once");
            c.Equal(2, stage.Validated, "restore validates stage once");
            c.Equal(2, stage.EntryValidated, "restore validates selected extension declaration");
            c.Equal(1, stage.Generated, "restore never invokes stage generation");
            c.Equal(1, stage.EntryGenerated, "restore never repeats native extension execution");
            c.Equal(2, init.Initializations, "restore initializes its systems after composition checks");
            c.Equal(Fix.FromInt(20), Stock(twin, "source"), "restore does not grant opening inventory");

            var recording = game.RecordReplay(new ReplayLimits(1024 * 1024, 600));
            var order = new PostOrderCommand { Plan = "inspect", Target = Entity(game, "destination").Id };
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(order), "public work preview admits");
            c.Equal(hash, game.Hash(), "preview creates no work or startup state");
            c.That(TestKit.Order(game, 0, order).Accepted, "ordinary player posts real timed work");
            c.Equal(1, game.Seat(0).View.Feature<WorkView>().ActiveOrders(Entity(game, "destination").Id), "accepted work is not instant");
            twin = factory.Load(game.Save()); int updates = 0; bool moved = false, arrived = false;
            while (game.Seat(0).View.Feature<WorkView>().ActiveOrders(Entity(game, "destination").Id) != 0 && updates++ < 160)
            { Step(game, twin); moved |= Position(game) != Fix.FromInt(4); arrived |= Position(game) == Fix.FromInt(18); }
            c.That(updates < 160 && moved && arrived, "native machines travel and earn authored work over bounded game time");
            c.That(game.Seat(0).View.TryFindResource("supply", out var resource), "resource resolves through public stable identity");
            c.That(TestKit.Order(game, 0, new HaulResourcesCommand { Source = Entity(game, "source").Id, Destination = Entity(game, "destination").Id,
                Worker = Entity(game, "machine").Id, Resource = resource, Ceiling = Fix.FromInt(4), Priority = 100 }).Accepted,
                "ordinary hauling uses actual native startup entities");
            c.Equal(Fix.Zero, Stock(game, "destination"), "accepted haul teleports no cargo");
            twin = factory.Load(game.Save()); updates = 0;
            while (Stock(game, "destination") != Fix.FromInt(4) && updates++ < 160) Step(game, twin);
            c.That(updates < 160, "physical pickup/travel/handling reaches actual delivery");
            c.Equal(Fix.FromInt(16), Stock(game, "source"), "actual source debited only delivered cargo");
            c.Equal(Fix.FromInt(4), Stock(game, "destination"), "actual destination receives exactly once");
            c.Equal(1, stage.Generated, "work/hauling/saved twins do not run startup again");
            c.Equal(1, stage.EntryGenerated, "work/hauling do not execute extension startup again");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), new ReplayLimits(1024 * 1024, 600)).Success, "startup validation and physical commands replay identically");
            c.Equal(opening, New(factory).Hash(), "independent new session opens exactly the same native state");

            foreach (string invalid in new[] { "requested-seat", "foreign-home", "missing-after", "null-after", "nonarray-after",
                "wrong-space", "wrong-presence", "no-space", "missing-position", "extra-placement-field", "stage-refusal", "extension-refusal", "decoder-refusal" })
            {
                var refused = new InitializeProbe(); var probe = new StartupStageProbe(refused) { Refuse = invalid == "stage-refusal" };
                c.Throws<ContentException>(() => New(Factory(refused, probe, edit: root => {
                    if (invalid == "requested-seat" || invalid == "foreign-home") root.Get("entity_setups")[0].Set("company", JsonValue.Number("1"));
                    if (invalid == "missing-after") root.Get("entity_setups")[0].Remove("after");
                    if (invalid == "null-after") root.Get("entity_setups")[0].Set("after", JsonValue.Null());
                    if (invalid == "nonarray-after") root.Get("entity_setups")[0].Set("after", JsonValue.Of(false));
                    if (invalid == "wrong-space") root.Get("entity_setups")[0].Get("placement").Set("space", JsonValue.Of("different"));
                    if (invalid == "wrong-presence") {
                        var other = root.Get("point_spaces")[0].Clone(); other.Set("id", JsonValue.Of("other.points")); root.Get("point_spaces").Add(other);
                        root.Get("entities")[2].Get("components").Get("point_presence").Set("space", JsonValue.Of("other.points"));
                    }
                    if (invalid == "no-space") root.Get("scenarios")[0].Remove("space");
                    if (invalid == "missing-position") root.Get("entity_setups")[0].Get("placement").Remove("position");
                    if (invalid == "extra-placement-field") root.Get("entity_setups")[0].Get("placement").Set("invented", JsonValue.Number("0"));
                }, extensionRefusal: invalid == "extension-refusal", decoderRefusal: invalid == "decoder-refusal"), invalid == "requested-seat" ? 1 : 2),
                    "composition refuses " + invalid);
                c.Equal(0, refused.Initializations, invalid + " refuses before any system Initialize");
                c.Equal(0, probe.Generated, invalid + " never reaches stage generation");
                c.Equal(0, probe.EntryGenerated, invalid + " never reaches native entry generation");
            }
            // Decoding/declaration validation is not future geometry/occupancy prediction.
            var late = new InitializeProbe(); var lateStage = new StartupStageProbe(late);
            c.Throws<ContentException>(() => New(Factory(late, lateStage, edit: root =>
                StartupContentAuthoring.Position(root, "source", JsonValue.Number("101")))), "actual out-of-bounds generation still refuses");
            c.Equal(1, late.Initializations, "actual geometric admission remains during generation after Initialize, not a fabricated whole-world preflight");
            c.Equal(0, lateStage.Generated, "failed actual startup does not reach later generation stage");
        }
    }
}
