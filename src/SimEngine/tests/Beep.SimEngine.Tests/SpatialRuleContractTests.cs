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
using static Beep.SimEngine.Tests.TileConditionContractTests;

namespace Beep.SimEngine.Tests
{
    internal static class SpatialRuleContractTests
    {
        internal const string Case = "Spatial rule contracts: independent JSON profiles drive timed colony work";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static JsonValue Read(string name) => Json.Parse(FrameworkProofContent.Read(name));
        private static GameFactory Create(SpatialRuleContractModule module, string kind = "independent",
            Action<JsonValue, JsonValue, JsonValue, JsonValue> edit = null)
        {
            var world = Read("colony.json"); var navigation = Read("colony-navigation.json");
            var placement = Read("colony-placement.json"); var authored = JsonValue.NewObject();
            authored.Set("independent_uniform_navigation", Json.Parse("[{\"id\":\"uniform.proof\",\"space\":\"colony.tiles\",\"cost\":10}]"));
            authored.Set("independent_layered_navigation", JsonValue.NewArray());
            authored.Set("independent_placement_rules", JsonValue.NewArray());
            authored.Set("gradient_navigation", JsonValue.NewArray());
            if (kind == "native")
            {
                world.Set("uniform_tile_navigation", authored.Get("independent_uniform_navigation").Clone());
                authored.Set("independent_uniform_navigation", JsonValue.NewArray());
            }
            else
            {
                authored.Set("independent_layered_navigation", navigation.Get("layered_tile_navigation").Clone());
                navigation.Set("layered_tile_navigation", JsonValue.NewArray());
                authored.Set("independent_placement_rules", placement.Get("tile_construction_rules").Clone());
                placement.Set("tile_construction_rules", JsonValue.NewArray());
                if (kind == "gradient")
                {
                    var rules = authored.Get("independent_layered_navigation"); var remaining = JsonValue.NewArray();
                    for (int i = 1; i < rules.Count; i++) remaining.Add(rules[i].Clone());
                    authored.Set("independent_layered_navigation", remaining);
                    authored.Set("gradient_navigation", Json.Parse("[{\"id\":\"land\",\"space\":\"colony.tiles\",\"blocked_condition\":\"colony.condition.wet\",\"minimum_cost\":10,\"preferred_y\":10}]"));
                }
            }
            var entities = world.Get("entities");
            for (int i = 0; i < entities.Count; i++)
            {
                var mover = entities[i].Get("components")?.Get("mover");
                if (mover != null) mover.Get("classes").Add(JsonValue.Of("uniform.proof"));
            }
            world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module.Id));
            edit?.Invoke(world, authored, navigation, placement);
            return Colony.Factory(definitions: world.ToString(), additionalModules: new GameModule[] { module },
                moduleDeclarations: new[] { "{\"id\":\"proof.spatial-rule-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.tile-navigation\",\"version\":\"1.0.0\"},{\"id\":\"sim.tile-construction-rules\",\"version\":\"1.0.0\"}],\"documents\":[\"spatial-rules.json\"]}" },
                moduleDocuments: new Dictionary<string, string> {
                    ["colony-navigation.json"] = navigation.ToString(), ["colony-placement.json"] = placement.ToString(),
                    ["spatial-rules.json"] = authored.ToString() });
        }
        private static void Order(Check c, SimGame game, Command command)
        {
            ulong before = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary profile/player command preview");
            c.Equal(before, game.Hash(), "profile preview remains pure");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary profile/player command accepted");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current contract profile identity");
            c.Equal(154, EngineInfo.SaveFormat, "same routing/map/work/stock persistence");
            foreach (string kind in new[] { "native", "independent", "gradient" }) Physical(c, kind);
            Invalid(c); Faults(c);
        }
        private static void Physical(Check c, string kind)
        {
            var module = new SpatialRuleContractModule(); var factory = Create(module, kind);
            var game = New(factory); var idle = New(factory); ulong idleHash = idle.Hash();
            c.That(module.Aliases && module.DirectDefinitions && module.IndependentNavigation == (kind != "native") &&
                module.IndependentPlacement == (kind != "native"), "original services alias actual direct contract profiles: " + kind);
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "physical profiles do not activate optional business");
            ulong before = game.Hash();
            c.Equal(new Code("colony.on-water"), game.Seat(0).Preview(Build(35, 5)), "authored footprint exclusion");
            c.Equal(new Code("core.unreachable"), game.Seat(0).Preview(Move(game, 35, 5)), "authored blocked terrain");
            c.Equal(before, game.Hash(), "profile refusal previews pure");
            var rejected = TestKit.Order(game, 0, Build(35, 5));
            c.That(!rejected.Accepted && rejected.Refusal == new Code("colony.on-water") && !Mine(game, "site:tower").Any(),
                "actual placement command shares rule and creates no refused site");
            var motion = game.Seat(0).View.Feature<MotionView>(); var truck = Mine(game, "truck")[0].Id;
            Order(c, game, new ConfigureMotionNavigationCommand { Entity = truck, Profile = "uniform.proof" });
            c.Equal(Code.None, motion.ReadNavigation(truck, out var uniform), "actual independent/native uniform selection");
            c.That(uniform.Profile == "uniform.proof" && uniform.Allowed.Contains("land"), "same authored allowed profiles");
            c.Throws<NotSupportedException>(() => ((IList<string>)uniform.Allowed).Clear(), "retained profile choices immutable");
            c.Equal(Code.None, game.Seat(0).Preview(Move(game, 35, 5)), "uniform native/independent profile changes route feasibility");
            Order(c, game, new ConfigureMotionNavigationCommand { Entity = truck, Profile = "land" });
            c.Equal(new Code("core.unreachable"), game.Seat(0).Preview(Move(game, 35, 5)), "ordinary profile selection restores original blockage");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<MotionView>().ReadNavigation(truck, out var hidden), "foreign physical profile facts private");
            c.That(hidden == null, "no private selection leak");
            var limits = new ReplayLimits(4 * 1024 * 1024, 5000);
            using (var recorder = game.RecordReplay(limits))
            {
                Order(c, game, Build(10, 16));
                c.That(Mine(game, "site:tower").Any() && !Mine(game, "tower").Any(), "real site not instant placement");
                Reach(game, () => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero) && !Mine(game, "tower").Any(), "profile-governed loaded travel");
                c.That(game.Clock.StepIndex > 1 && Mine(game).Any(e => e.State == new Code("state.moving")), "actual machines move over game time");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial profile-governed hauling save");
                long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.Equal(at, game.Clock.StepIndex, "one paused clock stops physical work");
                game.Clock.Resume(); twin.Clock.Resume();
                Reach(game, () => Mine(game, "tower").Length == 1, "profile-governed construction", twin);
                c.Equal(Fix.FromInt(20), Steel(game), "existing stock/transaction consumes actual30 materials");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed original profile-driven work roundtrip");
                c.Equal(idleHash, idle.Hash(), "interleaved sessions isolated");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary build and original physical profile rules replay");
            }
            if (kind == "independent")
            {
                var changed = New(Create(new SpatialRuleContractModule(), edit: (world, authored, navigation, placement) => {
                    authored.Get("independent_layered_navigation")[0].Get("rules")[0].Set("cost", JsonValue.Number("10"));
                    authored.Get("independent_placement_rules")[0].Set("rules", JsonValue.NewArray());
                }));
                c.Equal(Code.None, changed.Seat(0).Preview(Move(changed, 35, 5)), "independent authored cost edit governs route");
                c.Equal(Code.None, changed.Seat(0).Preview(Build(35, 5)), "independent authored rule edit governs placement");
                c.Equal(new Code("colony.on-water"), idle.Seat(0).Preview(Build(35, 5)), "earlier frozen profile unaffected");
            }
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "uniform-zero", "uniform-space", "uniform-metadata-space", "uniform-metadata-minimum",
                "navigation-space", "navigation-minimum", "unused-minimum", "duplicate-navigation", "missing-navigation-rules",
                "negative-base", "duplicate-rule", "unknown-condition", "placement-space", "unused-placement-space",
                "duplicate-placement", "missing-placement-rules", "placement-condition", "selected-other-space", "unknown-key" })
            {
                var module = new SpatialRuleContractModule();
                c.Throws<ContentException>(() => New(Create(module, edit: (world, authored, navigation, placement) => {
                    var uniform = authored.Get("independent_uniform_navigation")[0];
                    var navs = authored.Get("independent_layered_navigation"); var nav = navs[0];
                    var placements = authored.Get("independent_placement_rules"); var place = placements[0];
                    if (mode == "uniform-zero") uniform.Set("cost", JsonValue.Number("0"));
                    if (mode == "uniform-space") uniform.Set("space", JsonValue.Of("missing.space"));
                    if (mode == "uniform-metadata-space") uniform.Set("proof_fault", JsonValue.Of("space"));
                    if (mode == "uniform-metadata-minimum") uniform.Set("proof_fault", JsonValue.Of("minimum"));
                    if (mode == "navigation-space") nav.Set("proof_fault", JsonValue.Of("space"));
                    if (mode == "navigation-minimum") nav.Set("proof_fault", JsonValue.Of("minimum"));
                    if (mode == "unused-minimum") { nav = nav.Clone(); nav.Set("id", JsonValue.Of("unused.navigation")); nav.Set("proof_fault", JsonValue.Of("minimum")); navs.Add(nav); }
                    if (mode == "duplicate-navigation") uniform.Set("id", JsonValue.Of("land"));
                    if (mode == "missing-navigation-rules") nav.Remove("rules");
                    if (mode == "negative-base") nav.Set("base_cost", JsonValue.Number("-1"));
                    if (mode == "duplicate-rule") nav.Get("rules").Add(nav.Get("rules")[0].Clone());
                    if (mode == "unknown-condition") nav.Get("rules")[0].Set("conditions", Json.Parse("[\"missing.condition\"]"));
                    if (mode == "placement-space") place.Set("proof_fault", JsonValue.Of("space"));
                    if (mode == "unused-placement-space") { place = place.Clone(); place.Set("id", JsonValue.Of("unused.placement")); place.Set("proof_fault", JsonValue.Of("space")); placements.Add(place); }
                    if (mode == "duplicate-placement") { place = place.Clone(); placements.Add(place); }
                    if (mode == "missing-placement-rules") place.Remove("rules");
                    if (mode == "placement-condition") place.Get("rules")[0].Set("conditions", Json.Parse("[\"missing.condition\"]"));
                    if (mode == "selected-other-space") {
                        world.Get("tile_spaces").Add(Json.Parse("{\"id\":\"other.tiles\",\"width\":48,\"height\":32}"));
                        place.Set("space", JsonValue.Of("other.tiles")); place.Set("rules", JsonValue.NewArray());
                    }
                    if (mode == "unknown-key") place.Set("host_rule", JsonValue.Of(true));
                })), "actual loaded profile refuses before Initialize: " + mode);
                c.That(module.Initialize == null || module.Initialize.Initializations == 0, "malformed selected/unused profile does not initialize");
            }
        }
        private static void Faults(Check c)
        {
            foreach (SpatialRuleTarget target in Enum.GetValues(typeof(SpatialRuleTarget)))
                foreach (SpatialRuleFault fault in Enum.GetValues(typeof(SpatialRuleFault)))
                {
                    if (target == SpatialRuleTarget.Placement && (fault == SpatialRuleFault.NegativeCost || fault == SpatialRuleFault.BelowMinimum)) continue;
                    var module = new SpatialRuleContractModule(); var game = New(Create(module));
                    Command command = target == SpatialRuleTarget.Navigation ? (Command)Move(game, 10, 16) : Build(10, 16);
                    module.Arm(target, fault); long at = game.Clock.StepIndex; Code? result = null;
                    c.Throws<InvalidOperationException>(() => result = game.Seat(0).Preview(command), "actual profile observation/cost fault " + target + "/" + fault);
                    c.That(!result.HasValue && module.Reads == 1 && game.Fault?.Phase == "observation.provider", "first actual profile fault withholds result");
                    c.That(module.MutationCompleted == (fault != SpatialRuleFault.Exception), "actual callback completes before guard refuses");
                    c.That(game.Update() == 0 && game.Clock.StepIndex == at, "faulted profile cannot advance clock");
                    c.Throws<InvalidOperationException>(() => game.Hash(), "no healthy faulted profile hash");
                    c.Throws<InvalidOperationException>(() => game.Save(), "no faulted profile checkpoint");
                }
        }
    }
}
