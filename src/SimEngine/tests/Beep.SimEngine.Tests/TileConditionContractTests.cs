using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Only JSON intentions, ordinary player commands/Update and immutable facts.
    internal static class TileConditionContractTests
    {
        internal static void Register()
        {
            TestRunner.Add("Tile condition contracts: native physical construction stage", c => PhysicalStage(c, "native"));
            TestRunner.Add("Tile condition contracts: independent band physical construction stage", c => PhysicalStage(c, "band"));
            TestRunner.Add("Tile condition contracts: independent columns physical construction stage", c => PhysicalStage(c, "columns"));
            TestRunner.Add("Tile condition contracts: invalid authored metadata stage", Invalid);
            TestRunner.Add("Tile condition contracts: callback fault stage", Faults);
        }
        private static JsonValue Read(string name) => Json.Parse(FrameworkProofContent.Read(name));
        internal static GameFactory Create(TileConditionContractModule module, string kind = "band",
            Action<JsonValue, JsonValue, JsonValue, JsonValue, JsonValue> edit = null)
        {
            var world = Read("colony.json"); var bands = Read("colony-conditions.json");
            var placement = Read("colony-placement.json"); var navigation = Read("colony-navigation.json");
            var columns = Read("tile-condition-proof.json"); var authored = JsonValue.NewObject();
            var independent = JsonValue.NewArray(); authored.Set("independent_tile_bands", independent);
            if (kind != "native")
            {
                var original = bands.Get("tile_layer_band_conditions");
                independent.Add(original[0].Clone()); var retained = JsonValue.NewArray();
                for (int i = 1; i < original.Count; i++) retained.Add(original[i].Clone());
                bands.Set("tile_layer_band_conditions", retained);
            }
            string predicate = kind == "columns" ? "proof.closed-columns" : "colony.condition.wet";
            placement.Get("tile_construction_rules")[0].Get("rules")[0].Set("conditions", Json.Parse("[\"" + predicate + "\"]"));
            navigation.Get("layered_tile_navigation")[0].Get("rules")[0].Set("conditions", Json.Parse("[\"" + predicate + "\"]"));
            world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module.Id));
            world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(TileConditionProofModule.ModuleId));
            edit?.Invoke(world, authored, placement, navigation, columns);
            return Colony.Factory(definitions: world.ToString(),
                additionalModules: new GameModule[] { module, new TileConditionProofModule() },
                moduleDeclarations: new[] {
                    "{\"id\":\"proof.tile-contracts\",\"version\":\"1.0.0\",\"documents\":[\"tile-contracts.json\"]}",
                    "{\"id\":\"proof.tile-conditions\",\"version\":\"1.0.0\",\"documents\":[\"tile-condition-proof.json\"]}" },
                moduleDocuments: new Dictionary<string, string> {
                    ["colony-conditions.json"] = bands.ToString(), ["colony-placement.json"] = placement.ToString(),
                    ["colony-navigation.json"] = navigation.ToString(), ["tile-contracts.json"] = authored.ToString(),
                    ["tile-condition-proof.json"] = columns.ToString() });
        }
        internal static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 877 });
        internal static EntityView[] Mine(SimGame game, string prototype = null)
            => game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0) && (prototype == null || e.Def == prototype)).ToArray();
        internal static ConstructCommand Build(int x, int y) => new ConstructCommand {
            Def = "tower", Priority = 50, Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(x, y)) };
        internal static MoveVehicleCommand Move(SimGame game, int x, int y)
            => new MoveVehicleCommand { Vehicle = Mine(game, "truck")[0].Id, To = new TileCoord(x, y) };
        internal static WorkJobView[] Jobs(SimGame game)
        { var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs); return jobs.ToArray(); }
        internal static Fix Steel(SimGame game)
        {
            Fix amount = Fix.Zero;
            foreach (var entity in Mine(game))
            {
                var facets = new List<Facet>(); game.Seat(0).View.Inspect(entity.Id, facets);
                foreach (var fact in facets) if (fact.Key == Storage.FacetStock && fact.Subject == "steel") amount += fact.Value;
            }
            return amount;
        }
        internal static void Step(SimGame game, SimGame twin = null)
        {
            game.Update();
            if (game.Fault != null) throw new InvalidOperationException("tile predicate work fault: " + game.Fault);
            if (twin == null) return;
            twin.Update();
            if (twin.Fault != null || game.Hash() != twin.Hash()) throw new InvalidOperationException("saved predicate twins diverged: " + game.Clock.StepIndex);
        }
        internal static void Reach(SimGame game, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !ready(); i++) {
                Step(game, twin);
                if (i % 100 == 0) Console.WriteLine("   stage: " + stage + " update=" + i + "/clock=" + game.Clock.StepIndex + "/jobs=" +
                    string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.Blocker)));
            }
            if (!ready()) throw new InvalidOperationException(stage + "/step=" + game.Clock.StepIndex + "/jobs=" +
                string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.Blocker)));
        }
        private static void PhysicalStage(Check c, string kind)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current interface predicate identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no new runtime codec or map owner");
            Physical(c, kind);
        }
        private static void Physical(Check c, string kind)
        {
            var module = new TileConditionContractModule(); var factory = Create(module, kind);
            var game = New(factory); var idle = New(factory); ulong idleHash = idle.Hash();
            c.That(module.SharedAlias && module.DirectDefinitions && module.Independent == (kind != "native"),
                "actual catalog aliases use direct native/independent contracts: " + kind);
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "work does not activate business");
            c.Equal(0L, game.Clock.StepIndex, "composition does not run a second clock");
            ulong before = game.Hash();
            c.Equal(new Code("colony.on-water"), game.Seat(0).Preview(Build(35, 5)), "shared predicate rejects footprint");
            c.Equal(new Code("core.unreachable"), game.Seat(0).Preview(Move(game, 35, 5)), "same predicate rejects routing");
            c.Equal(Code.None, game.Seat(0).Preview(Build(10, 16)), "shared predicate admits actual dry work");
            c.Equal(before, game.Hash(), "accepted/refused previews are pure");
            var rejected = TestKit.Order(game, 0, Build(35, 5));
            c.That(!rejected.Accepted && rejected.Refusal == new Code("colony.on-water") && !Mine(game, "site:tower").Any(),
                "normal refusal matches preview and does not create project");
            if (kind == "columns")
            {
                c.Equal(new Code("colony.on-water"), game.Seat(0).Preview(Build(35, 16)), "independent column predicate changes dry construction");
                c.Equal(new Code("core.unreachable"), game.Seat(0).Preview(Move(game, 35, 16)), "independent column predicate changes dry routing");
            }
            var limits = new ReplayLimits(4 * 1024 * 1024, 5000);
            using (var recorder = game.RecordReplay(limits))
            {
                var result = TestKit.Order(game, 0, Build(10, 16));
                c.That(result.Accepted && Mine(game, "site:tower").Any(e => e.Id == result.Entity) && !Mine(game, "tower").Any(),
                    "ordinary accepted command creates physical project, not completed structure");
                Reach(game, () => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero) && !Mine(game, "tower").Any(), "partial loaded hauling");
                c.That(game.Clock.StepIndex > 1 && Mine(game).Any(e => e.State == new Code("state.moving")),
                    "actual loaded machine moves over game time");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "interrupted hauling uses original save owners");
                long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.Equal(at, game.Clock.StepIndex, "single paused clock stops physical work");
                game.Clock.Resume(); twin.Clock.Resume();
                Reach(game, () => Mine(game, "tower").Length == 1, "physical construction completion", twin);
                c.Equal(Fix.FromInt(20), Steel(game), "exact physical 50 opening minus30 materials, no duplication");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed original work/stock checkpoint");
                c.Equal(idleHash, idle.Hash(), "interleaved sessions are isolated");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "same public command stream reproduces physical work");
            }
            if (kind == "band")
            {
                var changed = New(Create(new TileConditionContractModule(), edit: (world, authored, placement, navigation, columns) =>
                    authored.Get("independent_tile_bands")[0].Set("minimum", JsonValue.Number("2"))));
                c.Equal(Code.None, changed.Seat(0).Preview(Build(35, 5)), "independent JSON edit changes placement");
                c.Equal(Code.None, changed.Seat(0).Preview(Move(changed, 35, 5)), "same independent JSON edit changes routing");
                c.Equal(new Code("colony.on-water"), idle.Seat(0).Preview(Build(35, 5)), "old frozen metadata unaffected");
            }
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing-space", "unknown-space", "null-space", "foreign-alias", "unused-null-space",
                "duplicate-id", "missing-layer", "missing-bound", "empty-band", "fractional-byte", "private-layer",
                "missing-reference", "unknown-reference", "duplicate-reference", "empty-references", "foreign-consumer", "unknown-key" })
            {
                var module = new TileConditionContractModule();
                c.Throws<ContentException>(() => New(Create(module, edit: (world, authored, placement, navigation, columns) => {
                    var rows = authored.Get("independent_tile_bands"); var band = rows[0];
                    if (mode == "unused-null-space") { band = band.Clone(); band.Set("id", JsonValue.Of("unused.condition")); rows.Add(band); }
                    if (mode == "missing-space") band.Remove("space");
                    if (mode == "unknown-space") band.Set("space", JsonValue.Of("missing.space"));
                    if (mode == "null-space" || mode == "unused-null-space") band.Set("proof_fault", JsonValue.Of("null-space"));
                    if (mode == "foreign-alias") band.Set("proof_fault", JsonValue.Of("foreign-alias"));
                    if (mode == "duplicate-id") columns.Get("proof_tile_columns")[0].Set("id", JsonValue.Of("colony.condition.wet"));
                    if (mode == "missing-layer") band.Set("layer", JsonValue.Of("missing.layer"));
                    if (mode == "missing-bound") band.Remove("maximum");
                    if (mode == "empty-band") band.Set("maximum", JsonValue.Number("1"));
                    if (mode == "fractional-byte") band.Set("minimum", JsonValue.Number("0.5"));
                    if (mode == "private-layer") world.Get("spatial_layers")[0].Set("visibility", JsonValue.Of("company_private"));
                    var rule = placement.Get("tile_construction_rules")[0].Get("rules")[0];
                    if (mode == "missing-reference") rule.Remove("conditions");
                    if (mode == "unknown-reference") rule.Set("conditions", Json.Parse("[\"missing.condition\"]"));
                    if (mode == "duplicate-reference") rule.Get("conditions").Add(rule.Get("conditions")[0].Clone());
                    if (mode == "empty-references") rule.Set("conditions", JsonValue.NewArray());
                    if (mode == "foreign-consumer")
                    {
                        world.Get("tile_spaces").Add(Json.Parse("{\"id\":\"other.tiles\",\"width\":60,\"height\":40}"));
                        columns.Get("proof_tile_columns")[0].Set("space", JsonValue.Of("other.tiles"));
                        rule.Set("conditions", Json.Parse("[\"proof.closed-columns\"]"));
                    }
                    if (mode == "unknown-key") band.Set("host_rule", JsonValue.Of(true));
                })), "loaded independent predicate refuses before Initialize: " + mode);
                c.That(module.Initialize == null || module.Initialize.Initializations == 0, "invalid metadata never initializes: " + mode);
            }
        }
        private static void Faults(Check c)
        {
            foreach (TilePredicateFault fault in Enum.GetValues(typeof(TilePredicateFault)))
                foreach (bool route in new[] { false, true })
                {
                    var module = new TileConditionContractModule(); var game = New(Create(module));
                    var command = route ? (Command)Move(game, 10, 16) : Build(10, 16);
                    module.Arm(fault); long at = game.Clock.StepIndex; Code? result = null;
                    c.Throws<InvalidOperationException>(() => result = game.Seat(0).Preview(command), "scoped actual predicate fault " + fault + "/" + route);
                    c.That(!result.HasValue && module.Reads == 1 && game.Fault?.Phase == "observation.provider", "first fault withholds predicate result");
                    c.That(module.MutationCompleted == (fault != TilePredicateFault.Exception), "actual mutation completed before guard refusal: " + fault + "/" + route);
                    c.That(game.Update() == 0 && game.Clock.StepIndex == at, "faulted predicate cannot advance single clock");
                    c.Throws<InvalidOperationException>(() => game.Hash(), "no healthy faulted hash");
                    c.Throws<InvalidOperationException>(() => game.Save(), "no faulted checkpoint");
                }
        }
    }
}
