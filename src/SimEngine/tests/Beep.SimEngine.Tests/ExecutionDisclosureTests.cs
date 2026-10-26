using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Player inputs and detached observations only; discovery, queue and execution belong to the engine.
    internal static class ExecutionDisclosureTests
    {
        internal const string Case = "Execution disclosure: queued and active recipe facts follow earned knowledge without resetting physical work";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Capability = new Code("contract.process");
        private const string KnownRecipe = "contract.unused-recipe", HiddenRecipe = "contract.make";
        private static TransformationManagementView View(SimGame game, int seat = 0)
            => game.Seat(seat).View.Feature<TransformationManagementView>();
        private static bool Known(SimGame game)
            => game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "recipe.discovered", out _);
        private static void Command(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary player admission");
            c.Equal(hash, game.Hash(), "preview is pure");
            int sequence = game.Seat(0).Submit(command); Step(game);
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(x => x.Sequence == sequence).Accepted, "ordinary engine command accepted");
        }
        private static void Reach(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 512 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " exceeded512Updates step=" + game.Clock.StepIndex
                + " knowledge=" + Known(game) + " source=" + Stock(game, "source") + " cargo=" + Stock(game, "machine"));
        }
        private static void Withheld(Check c, SimGame game)
        {
            var view = View(game); var actor = Entity(game, "contract.factory"); ulong hash = game.Hash();
            c.That(!view.TryGetTransformation(actor, Capability, out var snapshot) && snapshot.Queue == null,
                "whole current state withheld, not fabricated idle state");
            var queue = new List<TransformationQueueEntry> { new TransformationQueueEntry(999, KnownRecipe, false, EntityId.None) };
            c.That(!view.CopyQueue(actor, Capability, queue) && queue.Count == 0, "mixed queue publishes no known prefix or stale caller rows");
            c.That(view.ReadExecutionPreview(actor, Capability, KnownRecipe, out var preview) == Codes.UnknownDef && preview == null,
                "visible recipe preview cannot disclose another hidden queued execution");
            c.That(view.ReadExecutionPreview(actor, Capability, HiddenRecipe, out preview) == Codes.UnknownDef && preview == null,
                "hidden head or later recipe preview withheld");
            c.That(view.TransformationsOf(actor, Capability).Contains(KnownRecipe)
                && !view.TransformationsOf(actor, Capability).Contains(HiddenRecipe), "unrelated visible recipe menu remains available");
            c.That(view.ReadInputPorts(actor, Capability, KnownRecipe, out var ports).IsNone && ports.Count == 0,
                "unrelated standalone metadata query is not blocked by hidden execution");
            c.That(View(game, 1).ReadExecutionPreview(actor, Capability, KnownRecipe, out preview) == Codes.NotFound && preview == null,
                "foreign processor remains private");
            c.Equal(hash, game.Hash(), "withheld and independent queries do not mutate physical state or clock");
        }
        private static void Physical(Check c, bool direct, bool half, bool hiddenHead)
        {
            var factory = RecipeDisclosureTests.Factory(direct, "result", half,
                setupEdit: root =>
                {
                    root.Get("entities")[1].Get("components").Get("transformer").Get("transformations").Add(JsonValue.Of(KnownRecipe));
                    var visibleResult = root.Get("entities")[0].Clone(); visibleResult.Set("id", JsonValue.Of("contract.visible-result"));
                    root.Get("entities").Add(visibleResult);
                },
                recipeEdit: root =>
                {
                    var recipes = root.Get("independent_transformations");
                    recipes[0].Get("duration").Set("numerator", JsonValue.Number("30"));
                    recipes[1].Set("entity_output", JsonValue.Of("contract.visible-result"));
                    recipes[1].Get("duration").Set("numerator", JsonValue.Number("30"));
                }, knowledgeExtraDocument: "execution-disclosure.json");
            var game = ConstructionPlanTests.NewPoints(factory); var idle = ConstructionPlanTests.NewPoints(factory); ulong idleHash = idle.Hash();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "no business module for physical manufacturing and knowledge");
            var limits = new ReplayLimits(4 * 1024 * 1024, 5000); var recording = game.RecordReplay(limits);
            Command(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine"),
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
            Command(c, game, new PostOrderCommand { Plan = "recipe.discover", Target = Entity(game, "source") });
            Reach(game, null, () => Known(game), "first physical discovery");
            var actor = Entity(game, "contract.factory");
            if (!hiddenHead) Command(c, game, new EnqueueTransformationCommand { Transformer = actor, Capability = Capability, Transformation = KnownRecipe });
            Command(c, game, new EnqueueTransformationCommand { Transformer = actor, Capability = Capability, Transformation = HiddenRecipe });
            Command(c, game, new EnqueueTransformationCommand { Transformer = actor, Capability = Capability, Transformation = KnownRecipe });
            Reach(game, null, () => Process(game).Progress.WholeTicks > 0, "real active work");
            var before = Process(game); var beforeQueue = before.Queue.Select(x => x.Execution).ToArray();
            c.That(before.QueueCount == (hiddenHead ? 2 : 3) && before.Transformation == (hiddenHead ? HiddenRecipe : KnownRecipe), "actual head and future ordering");
            c.That(!hiddenHead || before.Wip.Stock.Lines.Count > 0 && before.Tools.Held.Lines.Count > 0, "hidden active recipe has physically supplied WIP and held tool");
            c.That(View(game).ReadExecutionPreview(actor, Capability, KnownRecipe, out var retained).IsNone && retained.Current.QueueCount == before.QueueCount,
                "earned knowledge permits whole mixed execution preview");
            int forget = game.Seat(0).Submit(new ForgetKnowledgeCommand { Policy = "recipe.forget" });
            var twin = factory.Load(game.Save()); game.Clock.Pause(); twin.Clock.Pause(); long at = game.Clock.StepIndex; ulong paused = game.Hash(); Step(game, twin);
            c.That(game.Clock.StepIndex == at && game.Hash() == paused && Known(game), "saved pending forgetting waits on sole paused clock");
            game.Clock.Resume(); twin.Clock.Resume(); Step(game, twin);
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(x => x.Sequence == forget).Accepted && !Known(game), "ordinary authored forgetting changes knowledge only");
            Withheld(c, game); Withheld(c, twin);
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "hidden live queue saves without redacted authoritative data");
            for (int i = 0; i < 3; i++) Step(game, twin);
            Withheld(c, game);
            Command(c, game, new PostOrderCommand { Plan = "recipe.discover", Target = Entity(game, "source") });
            twin = factory.Load(game.Save()); Reach(game, twin, () => Known(game), "physical rediscovery while execution continues");
            var restored = Process(game);
            c.That(restored.Queue.Select(x => x.Execution).SequenceEqual(beforeQueue), "forgetting/relearning preserves every actual execution identity and order");
            c.That((hiddenHead ? restored.Progress.Equals(before.Progress) : restored.Progress.WholeTicks > before.Progress.WholeTicks)
                && restored.Progress.WholeTicks < restored.Required,
                "original hidden-head access gate retains work; a visible head keeps advancing despite hidden later entry");
            c.That(!hiddenHead || restored.Wip.Stock.Lines.SequenceEqual(before.Wip.Stock.Lines)
                && restored.Tools.Held.Lines.SequenceEqual(before.Tools.Held.Lines), "knowledge loss neither discards nor duplicates WIP/tools");
            c.That(retained.Current.Progress.WholeTicks == before.Progress.WholeTicks && retained.Current.QueueCount == before.QueueCount,
                "previously retained immutable facts are not rewritten by forgetting");
            var queue = new List<TransformationQueueEntry>();
            c.That(View(game).CopyQueue(actor, Capability, queue) && queue.Select(x => x.Execution).SequenceEqual(beforeQueue), "restored whole queue copies actual identities");
            Step(game, twin); var resumed = Process(game);
            c.That(resumed.Progress.WholeTicks > restored.Progress.WholeTicks && resumed.Blocker.IsNone,
                "earned rediscovery clears original access gate and resumes real productive time");
            Reach(game, twin, () => Process(game).QueueCount == 0, "all real sequential manufacturing");
            c.That(game.Seat(0).Snapshot.Entities.Count(x => x.Def == "contract.result") == 1, "exactly one earned entity result");
            c.That(game.Seat(0).Snapshot.Entities.Count(x => x.Def == "contract.visible-result") == (hiddenHead ? 1 : 2), "visible queue neighbors earn their actual authored results too");
            c.That(Stock(game, "source") == Fix.FromInt(16) && Stock(game, "contract.factory") == Fix.One && Stock(game, "machine") == Fix.Zero,
                "three consumed units and one reusable tool conserved through hidden work");
            c.That(before.QueueCount == beforeQueue.Length && before.Progress.WholeTicks < resumed.Progress.WholeTicks, "retained snapshots remain detached");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal original physical state saves");
            c.Equal(idleHash, idle.Hash(), "undiscovered interleaved session isolated");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "discovery queue forgetting hidden work and rediscovery replay identically");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current changed engine identity");
            foreach (bool direct in new[] { false, true }) foreach (bool half in new[] { false, true }) foreach (bool head in new[] { false, true }) Physical(c, direct, half, head);
        }
    }
}
