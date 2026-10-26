using System;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.PlanetXProductionTests;

namespace Beep.SimEngine.Tests
{
    // Authored JSON, registration fault probes, ordinary commands/views/clock/saves and assertions only.
    internal static class CommandCompositionTests
    {
        internal const string Case = "Command composition: all registered handlers prepare and validate before Initialize with aliases and physical saved production";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Build(StartupCommandBindingsModule module, string mode = "valid", bool aliases = false, string resource = "space.planetx.ore")
            => Factory(bindings: module, bindingEdit: document => {
                var declaration = document.Get("startup_command_bindings")[0];
                declaration.Set("mode", JsonValue.Of(mode)); declaration.Set("aliases", JsonValue.Of(aliases)); declaration.Set("resource", JsonValue.Of(resource));
            });
        private static void Until(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 256 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " exceeded256Updates at=" + game.Clock.StepIndex
                + " phase=" + Process(game, "smelter").Phase + " blocker=" + Process(game, "smelter").Blocker
                + " progress=" + Process(game, "smelter").Progress.WholeTicks + " source=" + Stock(game, "source", "planetx.ore"));
        }
        private static void Run(Check c)
        {
            foreach (bool aliases in new[] { false, true })
            {
                Positive(c, aliases);
                foreach (string mode in new[] { "missing-provider", "prepare-refuse", "validate-refuse", "late-command", "late-service", "late-event", "late-system", "missing-resource" })
                {
                    var module = new StartupCommandBindingsModule();
                    Action startup = () => New(Build(module, mode == "missing-resource" ? "valid" : mode, aliases,
                        mode == "missing-resource" ? "space.missing" : "space.planetx.ore"));
                    if (mode.StartsWith("late-", StringComparison.Ordinal)) c.Throws<InvalidOperationException>(startup, "frozen command participant refuses " + mode + "/alias=" + aliases);
                    else c.Throws<ContentException>(startup, "command-only required declaration refuses " + mode + "/alias=" + aliases);
                    c.That(module.Initialize.Initializations == 0 && module.Generated == 0 && module.CommandCalls == 0,
                        "no initialization/generation/gameplay callback prefix for " + mode + "/alias=" + aliases);
                    if (mode == "prepare-refuse") c.That(module.Prepared == 1 && module.Validated == 0, "failed preparation validates no participant");
                    else c.That(module.Prepared == 2 && module.Validated == 1, "all preparations precede first failing handler validation");
                }
            }
            foreach (string mode in new[] { "command-type", "command-id", "service-contract", "persistent-id", "system-order", "system-id",
                "generation-order", "generation-id", "event-type", "event-id", "event-provider", "event-schema", "view-contract", "view-id", "view-provider", "view-schema" })
            {
                var module = new StartupCommandBindingsModule();
                c.Throws<InvalidOperationException>(() => New(Build(module, mode)), "existing registration owner refuses " + mode);
                c.That(module.Prepared == 0 && module.Validated == 0 && module.Initialize.Initializations == 0 && module.Generated == 0,
                    "registry conflict refuses before handler preparation or Initialize: " + mode);
            }
            foreach (string invalid in new[] { "missing", "wrong-version", "cycle", "self", "duplicate-owner", "duplicate-requirement" })
            {
                int declared = 0; var version = new ModuleVersion(1, 0, 0);
                var first = new ProbeModule("first", version, content: _ => declared++);
                GameModule[] modules;
                if (invalid == "missing") modules = new[] { first, new ProbeModule("second", version, new[] { new ModuleRequirement("absent", version) }) };
                else if (invalid == "wrong-version") modules = new[] { first, new ProbeModule("second", version, new[] { new ModuleRequirement("first", new ModuleVersion(2, 0, 0)) }) };
                else if (invalid == "self") modules = new[] { first, new ProbeModule("second", version, new[] { new ModuleRequirement("second", version) }) };
                else if (invalid == "duplicate-owner") modules = new[] { first, new ProbeModule("first", version) };
                else if (invalid == "duplicate-requirement") modules = new[] { first, new ProbeModule("second", version, new[] { new ModuleRequirement("first", version), new ModuleRequirement("first", version) }) };
                else modules = new[] { first, new ProbeModule("second", version, new[] { new ModuleRequirement("third", version) }), new ProbeModule("third", version, new[] { new ModuleRequirement("second", version) }) };
                c.Throws<InvalidOperationException>(() => new GameDefinition("refused", modules), "whole original dependency graph refuses " + invalid);
                c.Equal(0, declared, "invalid complete graph runs no earlier content declaration callback");
            }
        }
        private static void Positive(Check c, bool aliases)
        {
            var module = new StartupCommandBindingsModule(); var factory = Build(module, aliases: aliases); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build, "actual changed startup behavior identity");
            c.That(module.Prepared == 2 && module.Validated == 2 && module.Initialize.Initializations == 1 && module.Generated == 1 && module.CommandCalls == 0,
                "both unused command-only/aliased participants admit before one Initialize/generation");
            c.That(module.Trace.SequenceEqual(new[] { "prepare.first", "prepare.second", "validate.first", "validate.second" }),
                "all prepares precede validation, original reference identity aliases participate exactly once");
            c.That(game.Modules.Count == 10 && !game.Modules.Contains(TycoonModule.ModuleId), "authored ordinary physical modules have no money/business/admin fallback");
            c.Equal(Fix.FromInt(30), Stock(game, "source", "planetx.ore"), "original scenario grants actual stock once");
            var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "valid handler composition restores without payload/schema change");
            c.That(module.Prepared == 4 && module.Validated == 4 && module.Initialize.Initializations == 2 && module.Generated == 1,
                "restore validates each new handler before Initialize but never generates again");
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var recording = game.RecordReplay(limits);
            ulong before = game.Hash(); var command = Queue(game, "smelter", "space.smelt.planetx");
            c.Equal(Code.None, game.Seat(0).Preview(command), "original physical production preview admits");
            c.Equal(before, game.Hash(), "pure command preview performs no production or preparation");
            game.Seat(0).Submit(command); twin = factory.Load(game.Save());
            c.Equal(Fix.Zero, Stock(game, "smelter", "alloy"), "pending command cannot instantly finish production");
            Until(game, twin, () => Process(game, "smelter").Progress.WholeTicks > 0
                && Process(game, "smelter").Wip.Stock.AmountOf(Resource(game, "planetx.ore")) == Fix.FromInt(10), "physical loading/travel/timed work");
            c.That(game.Clock.StepIndex > 2 && Stock(game, "source", "planetx.ore") == Fix.FromInt(20), "real supplies travel before actual productive work");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial original WIP and handler admission restore");
            Until(game, twin, () => Stock(game, "smelter", "alloy") == Fix.FromInt(6) && Stock(game, "smelter", "slag") == Fix.FromInt(4), "whole actual outputs");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed actual production restores without repeated cost/output");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "new handler startup admission preserves original normal player replay");
            c.That(module.CommandCalls == 0 && module.Generated == 1, "registration-only probes implement no gameplay or repeated startup stock grant");
            var separate = New(factory); Step(separate); Step(game, twin);
            c.That(Stock(separate, "source", "planetx.ore") == Fix.FromInt(30) && Stock(separate, "smelter", "alloy") == Fix.Zero,
                "interleaved session preserves original gameplay state isolation");
            c.Equal(0, module.CommandCalls, "all positive gameplay remains original engine behavior, not the startup test commands");
        }
    }
}
