using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.PlanetXProductionTests;

namespace Beep.SimEngine.Tests
{
    // Pure JSON authoring, negative registration probes, public player inputs/facts/time/saves only.
    internal static class CompositionAcceptanceTests
    {
        internal const string Case = "Composition acceptance: ownership dependency and order refusals precede Initialize and valid physical saved execution";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly ModuleVersion Version = new ModuleVersion(1, 0, 0);
        private static ModuleRequirement Require(string id) => new ModuleRequirement(id, Version);
        private static GameFactory Build(StartupCommandBindingsModule module, string mode = "valid", bool aliases = false,
            bool half = false, Action<JsonValue> master = null, Action<JsonValue> world = null, string recipes = null, string omit = null)
            => Factory(bindings: module, recipes: recipes, omit: omit, masterEdit: master,
                bindingEdit: value => { var row = value.Get("startup_command_bindings")[0]; row.Set("mode", JsonValue.Of(mode)); row.Set("aliases", JsonValue.Of(aliases)); },
                worldEdit: value => { if (half) value.Get("time_definitions")[0].Get("quantum").Set("denominator", JsonValue.Number("2")); world?.Invoke(value); });
        private static void NoPrefix(Check c, StartupCommandBindingsModule module, string reason)
            => c.That(module.Initialize.Initializations == 0 && module.Generated == 0 && module.CommandCalls == 0,
                "no Initialize, generation or gameplay prefix: " + reason);
        private static void Graphs(Check c)
        {
            foreach (string invalid in new[] { "missing", "version", "self", "cycle", "duplicate-owner", "duplicate-requirement" })
            {
                int declarations = 0, compositions = 0; var initialize = new InitializeProbe();
                var first = new ProbeModule("first", Version, content: _ => declarations++, compose: composer => { compositions++; composer.AddSystem(initialize, 1); });
                GameModule[] modules = invalid switch {
                    "missing" => new[] { first, new ProbeModule("second", Version, new[] { Require("absent") }) },
                    "version" => new[] { first, new ProbeModule("second", Version, new[] { new ModuleRequirement("first", new ModuleVersion(2, 0, 0)) }) },
                    "self" => new[] { first, new ProbeModule("second", Version, new[] { Require("second") }) },
                    "cycle" => new[] { first, new ProbeModule("second", Version, new[] { Require("third") }), new ProbeModule("third", Version, new[] { Require("second") }) },
                    "duplicate-owner" => new[] { first, new ProbeModule("first", Version) },
                    _ => new[] { first, new ProbeModule("second", Version, new[] { Require("first"), Require("first") }) }
                };
                c.Throws<InvalidOperationException>(() => new GameDefinition("refused", modules), "invalid complete code graph " + invalid);
                c.That(declarations == 0 && compositions == 0 && initialize.Initializations == 0, "graph refusal runs no earlier declaration/Compose/Initialize");
            }
            var declarationsToRefuse = new (Action<ContentTypes> First, Action<ContentTypes> Second)[] {
                (types => types.Section<StartupCommandBindingDef>("collision"), types => types.Section<PointSpaceDef>("collision")),
                (types => types.Section<StartupCommandBindingDef>("first"), types => types.Section<StartupCommandBindingDef>("second")),
                (types => types.Component<CodecPartDef>("collision", 1), types => types.Component<CounterDef>("collision", 1)),
                (types => types.Component<CodecPartDef>("first", 1), types => types.Component<CodecPartDef>("second", 1)),
                (types => { }, types => types.Section<StartupCommandBindingDef>("resources")),
                (types => { }, types => types.Component<Beep.SimEngine.Maps.TilePresenceDef>("another-presence", 1))
            };
            foreach (var declaration in declarationsToRefuse)
            {
                int composed = 0; var initialize = new InitializeProbe();
                c.Throws<InvalidOperationException>(() => new GameDefinition("refused", new[] {
                    new ProbeModule("first", Version, content: declaration.First, compose: composer => { composed++; composer.AddSystem(initialize, 1); }),
                    new ProbeModule("second", Version, new[] { Require("first") }, content: declaration.Second)
                }), "content kind/type/codec ownership cannot be replaced or aliased");
                c.That(composed == 0 && initialize.Initializations == 0, "content registration conflict cannot initialize");
            }
        }
        private static JsonValue Module(JsonValue master, string id)
        {
            var rows = master.Get("modules");
            return Enumerable.Range(0, rows.Count).Select(index => rows[index]).Single(row => row.Get("id").Text == id);
        }
        private static void Files(Check c)
        {
            var edits = new Action<JsonValue>[] {
                master => master.Get("modules").Add(Module(master, "points").Clone()),
                master => Module(master, "points").Get("documents").Add(JsonValue.Of("planet-x-world.json")),
                master => Module(master, "points").Get("documents").Add(JsonValue.Of("planet-x-resources.json")),
                master => Module(master, "points").Set("version", JsonValue.Of("2.0.0")),
                master => Module(master, "points").Get("requires")[0].Set("id", JsonValue.Of("missing")),
                master => Module(master, "points").Get("requires")[0].Set("version", JsonValue.Of("2.0.0")),
                master => Module(master, "proof.startup-command-bindings").Get("requires")[0].Set("id", JsonValue.Of("proof.startup-command-bindings")),
                master => Module(master, "points").Set("id", JsonValue.Of("unregistered")),
                master => Module(master, "points").Set("requires", JsonValue.NewArray())
            };
            foreach (var edit in edits)
            {
                var module = new StartupCommandBindingsModule();
                c.Throws<ContentException>(() => New(Build(module, master: edit)), "master cannot change declaration ownership/exact code dependency graph");
                NoPrefix(c, module, "invalid master");
                c.That(module.Prepared == 0 && module.Validated == 0, "invalid file graph cannot run frozen provider callbacks");
            }
            foreach (string path in new[] { "planet-x-world.json", "planet-x-recipes.json", "planet-x-resources.json" })
            {
                var module = new StartupCommandBindingsModule();
                c.Throws<ContentException>(() => New(Build(module, omit: path)), "listed file has no fallback: " + path); NoPrefix(c, module, path);
            }
            var duplicate = Json.Parse(FrameworkProofContent.Read("planet-x-recipes.json")); duplicate.Get("transformations").Add(duplicate.Get("transformations")[0].Clone());
            var duplicateModule = new StartupCommandBindingsModule();
            c.Throws<ContentException>(() => New(Build(duplicateModule, recipes: duplicate.ToString())), "duplicate authored transformation owner refuses"); NoPrefix(c, duplicateModule, "duplicate recipe");
            var entityModule = new StartupCommandBindingsModule();
            c.Throws<ContentException>(() => New(Build(entityModule, world: value => value.Get("entities").Add(value.Get("entities")[0].Clone()))), "duplicate authored entity refuses"); NoPrefix(c, entityModule, "duplicate entity");
        }
        private static void Registries(Check c)
        {
            foreach (string mode in new[] { "command-type", "command-id", "service-contract", "persistent-id", "service-provider", "service-native-owner",
                "system-order", "system-id", "system-native-id", "system-native-order", "generation-order", "generation-id", "event-type", "event-id",
                "event-provider", "event-schema", "event-handler", "event-subscriber-provider", "view-contract", "view-id", "view-provider", "view-schema", "view-native-contract" })
            {
                var module = new StartupCommandBindingsModule();
                c.Throws<InvalidOperationException>(() => New(Build(module, mode)), "existing registry refuses " + mode); NoPrefix(c, module, mode);
                c.That(module.Prepared == 0 && module.Validated == 0, "registration conflicts precede cross-provider preparation: " + mode);
            }
            foreach (string mode in new[] { "system-schedule", "system-codec-schema", "service-codec-schema" })
            {
                var module = new StartupCommandBindingsModule();
                c.Throws<ArgumentException>(() => New(Build(module, mode)), "invalid explicit schedule/schema refuses " + mode); NoPrefix(c, module, mode);
            }
            foreach (bool aliases in new[] { false, true })
                foreach (string mode in new[] { "missing-provider", "prepare-refuse", "validate-refuse", "late-command", "late-service", "late-event", "late-system" })
                {
                    var module = new StartupCommandBindingsModule();
                    if (mode.StartsWith("late-", StringComparison.Ordinal)) c.Throws<InvalidOperationException>(() => New(Build(module, mode, aliases)), "frozen registries refuse " + mode);
                    else c.Throws<ContentException>(() => New(Build(module, mode, aliases)), "required provider/validation refuses " + mode);
                    NoPrefix(c, module, mode + "/aliases=" + aliases);
                    c.That(mode == "prepare-refuse" ? module.Prepared == 1 && module.Validated == 0 : module.Prepared == 2 && module.Validated == 1,
                        "same aliased instance prepares/validates once; all prepares precede checks");
                }
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 512 && !ready(); i++) Step(game, twin);
            if (!ready()) { var facts = Process(game, "smelter"); throw new InvalidOperationException(stage + " exceeded512Updates at=" + game.Clock.StepIndex
                + " phase=" + facts.Phase + " blocker=" + facts.Blocker + " progress=" + facts.Progress.WholeTicks + " source=" + Stock(game, "source", "planetx.ore")); }
        }
        private static void Physical(Check c, bool aliases, bool half)
        {
            var module = new StartupCommandBindingsModule(); var factory = Build(module, aliases: aliases, half: half); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current engine identity, not historical stage build");
            c.That(module.Prepared == 2 && module.Validated == 2 && module.Initialize.Initializations == 1 && module.Generated == 1 && module.CommandCalls == 0,
                "valid independent command declarations initialize after complete admission");
            c.That(module.Trace.SequenceEqual(new[] { "prepare.first", "prepare.second", "validate.first", "validate.second" }), "canonical same-owner aliases prepare once before validation");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId) && !game.Modules.Contains(TileNavigationModule.ModuleId) && game.Observer.Space is PointWorldSnapshot,
                "actual selected non-tile provider; no Tycoon, navigation or geometry fallback");
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var replay = game.RecordReplay(limits);
            var command = Queue(game, "smelter", "space.smelt.planetx"); ulong before = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary physical production admission"); c.Equal(before, game.Hash(), "preview is pure");
            int sequence = game.Seat(0).Submit(command); var twin = factory.Load(game.Save());
            c.That(module.Prepared == 4 && module.Validated == 4 && module.Initialize.Initializations == 2 && module.Generated == 1,
                "restore revalidates new instances without duplicate world generation");
            c.That(Process(game, "smelter").QueueCount == 0 && Stock(game, "smelter", "alloy") == Fix.Zero, "pending input grants no instant result");
            Step(game, twin); long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            c.Equal(Code.None, results.Single(row => row.Sequence == sequence).Refusal, "actual command agrees with preview");
            Until(game, twin, () => Process(game, "smelter").Progress.WholeTicks > 0, "physical material/tool loading and travel");
            var retained = Process(game, "smelter");
            c.That(retained.Wip.Stock.AmountOf(Resource(game, "planetx.ore")) == Fix.FromInt(10) && retained.Tools.Held.AmountOf(Resource(game, "crucible")) == Fix.One,
                "actual original supply and reusable tool precede productive elapsed work");
            c.That(Stock(game, "source", "planetx.ore") == Fix.FromInt(20) && Stock(game, "smelter", "alloy") == Fix.Zero, "paid partial work is not completed output");
            c.That(!game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(Actor(game, "smelter"), new Code("space.operation"), out _), "foreign company has no private WIP");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial work restores original ownership/schedules/claims once");
            game.Clock.Pause(); twin.Clock.Pause(); long at = game.Clock.StepIndex; ulong paused = game.Hash(); Step(game, twin);
            c.That(game.Clock.StepIndex == at && game.Hash() == paused, "sole clock pauses actual physical work"); game.Clock.Resume(); twin.Clock.Resume();
            Until(game, twin, () => Process(game, "smelter").QueueCount == 0 && Stock(game, "smelter", "alloy") == Fix.FromInt(6), "whole earned outputs");
            c.That(Stock(game, "smelter", "slag") == Fix.FromInt(4) && Stock(game, "source", "planetx.ore") == Fix.FromInt(20)
                && Process(game, "smelter").Wip.Stock.IsEmpty && retained.Wip.Stock.AmountOf(Resource(game, "planetx.ore")) == Fix.FromInt(10), "conserved results and detached retained partial facts");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal saved state has no repeated input/output");
            c.That(new ReplayVerifier().Verify(factory, replay.Finish(), limits).Success, "valid frozen composition survives public command replay");
            var isolated = New(factory); Step(isolated); Step(game, twin);
            c.That(Stock(isolated, "source", "planetx.ore") == Fix.FromInt(30) && Stock(isolated, "smelter", "alloy") == Fix.Zero && module.CommandCalls == 0,
                "interleaved session state is isolated; registration probes never implement gameplay");
        }
        private static void Run(Check c)
        {
            Graphs(c); Files(c); Registries(c);
            foreach (bool aliases in new[] { false, true }) foreach (bool half in new[] { false, true }) Physical(c, aliases, half);
        }
    }
}
