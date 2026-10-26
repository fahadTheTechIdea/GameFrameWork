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
    internal static class ActivityPrerequisiteTests
    {
        internal const string Case = "Activity prerequisites: cross-plan alternatives, cycle refusal and physical saved bootstrap";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "native-survey", Seed = 733 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static WorkView View(SimGame game, int company = 0) => game.Seat(company).View.Feature<WorkView>();
        private static Entity Site(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "prior.site" && e.Get<ITilePresence>().Tile.X == 16);
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("prerequisite clock=" + game.Clock.StepIndex + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " clock=" + game.Clock.StepIndex + " orders=" + string.Join(";",
                Board(game).Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)) + " positions=" + string.Join(";",
                game.World.Entities.All.Select(e => e.Id + "/" + e.Get<ITilePresence>()?.Position)));
        }
        private static Order Start(Check c, SimGame game, string plan, EntityId target)
        {
            var command = new PostOrderCommand { Plan = plan, Target = target, Priority = 60 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary plan admission");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary plan command accepted");
            return Board(game).Order((int)result.Arg0);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "cross-activity validation identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "composition-only check changes no persistent payload");
            var proof = new PriorActivityProofModule(); var factory = PriorActivityTests.Factory(proof: proof); var game = New(factory);
            var site = Site(game); var steel = game.World.Content.ResourceId("steel");
            c.Equal(1, proof.Initializations, "viable feedback composed before ordinary initialization");
            c.That(!game.World.Services.Has<IEconomy>(), "physical prior activities require no business");
            c.That(ReferenceEquals(Board(game), game.World.Services.Get<IActivityCatalog>()), "sole existing captured plan owner");
            c.That(ReferenceEquals(Board(game), game.World.Services.Get<IActivityCompletionHistory>()), "sole earned history owner");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "existing work state unchanged");
            c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.Systems.OfType<WorkSystem>().Single().StateSchemaVersion, "existing scheduler state unchanged");
            c.That(game.World.Content.Get<WorkRequirementDef>("proof.bootstrap-ready") is IPriorActivityWorkRequirement &&
                !(game.World.Content.Get<WorkRequirementDef>("proof.bootstrap-ready") is IWorkRequirementBindingValidator), "independent public leaf requires no custom binding validator");
            c.That(game.World.Content.Get<WorkRequirementDef>("proof.bootstrap-ready").AuthoringPath.Contains("prior-activity-proof.json"), "mod intent comes from actual selected master document");
            var criteria = new List<WorkRequirementView>(); ulong initial = game.Hash();
            c.That(!View(game).TryReadActivityCompletion("activity:dependency.bootstrap", site.Id, out _), "structural possibility grants no earned history");
            c.Equal(Code.None, View(game).PreviewOrderRequirements("dependency.dependent", site.Id, criteria), "valid blocked intent preview");
            c.That(criteria.All(r => r.Result.Status == WorkRequirementStatus.Pending), "native and public leaf await actual completion");
            c.Equal(initial, game.Hash(), "structural history reads and previews are pure");
            var dependent = Start(c, game, "dependency.dependent", site.Id);
            c.Equal(0, dependent.Node("first").JobId, "blocked work reserves no worker");
            var bootstrap = Start(c, game, "dependency.bootstrap", site.Id);
            Reach(game, () => bootstrap.Node("prepare").Progress.WholeTicks > 0, "material alternative and physical travel");
            c.Equal(Fix.FromInt(24), site.Get<IInventory>().Amount(steel), "real alternative consumes eight stock");
            c.That(!View(game).TryReadActivityCompletion("activity:dependency.bootstrap", site.Id, out _), "partial positive work is not full completion");
            c.Equal(0L, dependent.Node("first").Progress.WholeTicks, "dependent never fabricates an earlier completion");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial bootstrap and pending dependent save");
            game.Clock.Pause(); twin.Clock.Pause(); long paused = game.Clock.StepIndex; Update(game, twin);
            c.Equal(paused, game.Clock.StepIndex, "one clock pause stops real prerequisite work");
            c.Equal(game.Hash(), twin.Hash(), "equal explicit local pacing on saved twins"); game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, () => bootstrap.State == OrderState.Done, "full physical bootstrap", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved bootstrap completes identically");
            c.That(Board(game).Order(bootstrap.Id) == null, "ordinary terminal pruning remains authoritative");
            c.That(View(game).TryReadActivityCompletion("activity:dependency.bootstrap", site.Id, out var fact) && fact.Order == bootstrap.Id, "earned compact fact survives pruning");
            c.That(!View(game, 1).TryReadActivityCompletion("activity:dependency.bootstrap", site.Id, out _), "foreign company learns no private prerequisite fact");
            Reach(game, () => dependent.Node("first").Progress.WholeTicks > 0, "actual public/native prerequisite admission", twin);
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial dependent save");
            Reach(game, () => dependent.State == OrderState.Done, "reordered authored sequential nodes", twin);
            c.Equal(game.Hash(), twin.Hash(), "dependent saved sequence completes identically");
            c.That(dependent.Node("first").CompletedBy.IsValid && dependent.Node("finish").CompletedBy.IsValid &&
                dependent.Node("first").Progress.WholeTicks > 0 && dependent.Node("finish").Progress.WholeTicks > 0, "both stages execute actual machine work");
            c.Equal(Fix.FromInt(24), site.Get<IInventory>().Amount(steel), "completion predicates neither copy nor refund materials");
            var optional = Start(c, game, "dependency.optional", site.Id);
            c.Equal(0, optional.Node("inspect").JobId, "viable optional self branch still requires real independent starter");
            var cancelled = Start(c, game, "prior.prepare", site.Id);
            Reach(game, () => cancelled.Node("prepare").Progress.WholeTicks > 0, "partial independent starter");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = cancelled.Id }).Accepted, "ordinary prerequisite cancellation");
            c.That(!View(game).TryReadActivityCompletion("activity:prior.prepare", site.Id, out _), "cancelled partial work earns no history");
            c.Equal(0L, optional.Node("inspect").Progress.WholeTicks, "cancelled starter does not open optional branch");
            c.Equal(Fix.FromInt(16), site.Get<IInventory>().Amount(steel), "cancellation retains actual spent inputs");
            var prepare = Start(c, game, "prior.prepare", site.Id); twin = factory.Load(game.Save());
            Reach(game, () => prepare.State == OrderState.Done, "real independent starter completion", twin);
            Reach(game, () => optional.State == OrderState.Done, "viable optional self feedback executes physically", twin);
            c.Equal(game.Hash(), twin.Hash(), "starter/optional chain survives saved continuation");
            c.That(optional.Node("inspect").CompletedBy.IsValid && optional.Node("inspect").Progress.WholeTicks > 0, "optional branch is not instant work");
            c.Equal(Fix.FromInt(8), site.Get<IInventory>().Amount(steel), "exact three material attempts consume once each");
            c.Equal(bootstrap.Id, fact.Order, "held immutable completion fact does not change");
            // Feedback is now genuinely earned. Repeating bootstrap uses completion, not another eight steel.
            var repeat = Start(c, game, "dependency.bootstrap", site.Id); twin = factory.Load(game.Save());
            Reach(game, () => repeat.State == OrderState.Done, "earned feedback repeat", twin);
            c.Equal(Fix.FromInt(8), site.Get<IInventory>().Amount(steel), "real feedback alternative uses earned history without repeated material consumption");
            c.Equal(game.Hash(), twin.Hash(), "earned feedback saved identity");
            c.That(repeat.Node("prepare").Progress.WholeTicks > 0 && repeat.Node("prepare").CompletedBy.IsValid, "earned alternative still takes physical work time");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "final compact facts save without another graph store");
            var isolated = New(factory);
            c.That(!View(isolated).TryReadActivityCompletion("activity:dependency.bootstrap", EntityId.None, out _), "interleaved session gets no static/foreign earned facts");
            c.Equal(Fix.FromInt(32), Site(isolated).Get<IInventory>().Amount(steel), "independent authored stock");
            Invalid(c);
        }
        private static string Refusal(Check c, JsonValue dependencies, JsonValue external, string label)
        {
            var probe = new PriorActivityProofModule(); string message = null;
            try { New(PriorActivityTests.Factory(proof: probe, dependencies: dependencies.ToString(), proofRequirements: external.ToString())); }
            catch (ContentException error) { message = error.Message; }
            c.That(message != null, label + " refused as content");
            c.Equal(0, probe.Initializations, label + " rejected before system initialization"); return message;
        }
        private static JsonValue Dependencies() => Content.Json.Parse(FrameworkProofContent.Read("native-prior-dependencies.json"));
        private static JsonValue External() => Content.Json.Parse(FrameworkProofContent.Read("prior-activity-proof.json"));
        private static JsonValue Entries(params string[] values)
        {
            var result = JsonValue.NewArray();
            for (int i = 0; i < values.Length; i++) result.Add(Content.Json.Parse("{\"id\":\"edge" + i + "\",\"requirement\":\"" + values[i] + "\"}"));
            return result;
        }
        private static void Invalid(Check c)
        {
            foreach (var mode in new[] { "missing", "empty", "unselected" })
            {
                var external = External(); external.Get("proof_prior_activity_requirements")[0].Set("plan", JsonValue.Of(mode == "empty" ? "" : mode == "missing" ? "activity:missing" : "build:missing-provider"));
                c.That(Refusal(c, Dependencies(), external, "public " + mode).Contains("explicit selected plan"), "generic engine validates independent public leaf");
            }
            var self = Dependencies(); self.Get("prior_activity_requirements")[0].Set("plan", JsonValue.Of("activity:dependency.bootstrap"));
            self.Get("work_requirement_groups")[0].Set("entries", Entries("dependency.dependent-ready"));
            c.That(Refusal(c, self, External(), "mandatory self").Contains("depends on itself"), "same compiler reports unavoidable self dependency");
            var pair = Dependencies(); pair.Get("work_requirement_groups")[0].Set("entries", Entries("dependency.dependent-ready"));
            string pairMessage = Refusal(c, pair, External(), "mandatory pair");
            c.That(pairMessage.Contains("dependency cycle") && pairMessage.Contains("activity:dependency.bootstrap") && pairMessage.Contains("activity:dependency.dependent"), "same compiler reports actual unresolved pair");
            var reversed = Dependencies(); reversed.Get("work_requirement_groups")[0].Set("entries", Entries("dependency.dependent-ready"));
            var reversedActivities = JsonValue.NewArray(); for (int i = 2; i >= 0; i--) reversedActivities.Add(reversed.Get("activities")[i]); reversed.Set("activities", reversedActivities);
            c.Equal(pairMessage, Refusal(c, reversed, External(), "reordered pair"), "diagnostic stable across authored declaration order");
            var all = Dependencies(); all.Get("work_requirement_groups")[0].Set("mode", JsonValue.Of("all"));
            c.That(Refusal(c, all, External(), "all with material branch").Contains("dependency cycle"), "all cannot use one ready sibling to bootstrap");
            var fork = Dependencies();
            fork.Get("prior_activity_requirements")[2].Set("plan", JsonValue.Of("activity:dependency.optional"));
            fork.Get("work_requirement_groups")[0].Set("entries", Entries("dependency.dependent-ready", "dependency.optional-self-ready"));
            fork.Get("work_requirement_groups")[2].Set("mode", JsonValue.Of("all"));
            fork.Get("work_requirement_groups")[2].Set("entries", Entries("dependency.bootstrap-ready", "prior.target-ready"));
            c.That(Refusal(c, fork, External(), "any without independent path").Contains("dependency cycle"), "alternative cyclic prerequisites need a real starter, not intersected mandatory edges");
            // An already-ready ANY is excluded from diagnostic even when another node makes this plan deadlock.
            var mixed = Dependencies(); var second = Content.Json.Parse("{\"executor\":\"work.staffed\",\"id\":\"blocked\",\"roles\":[\"survey\"],\"duration\":{\"numerator\":1,\"denominator\":10,\"unit\":\"hour\"},\"requirement\":\"dependency.dependent-ready\"}");
            mixed.Get("activities")[0].Get("nodes").Add(second);
            mixed.Get("work_requirement_groups")[0].Set("entries", Entries("dependency.bootstrap-ready", "survey.inputs"));
            string mixedMessage = Refusal(c, mixed, External(), "ready optional self with blocked second node");
            c.That(mixedMessage.Contains("dependency cycle") && !mixedMessage.Contains("depends on itself"), "irrelevant ready-any self edge does not replace real cycle diagnostic");
            var chain = Dependencies();
            for (int i = 0; i < 128; i++)
            {
                if (i > 0) chain.Get("prior_activity_requirements").Add(Content.Json.Parse("{\"id\":\"chain.ready" + i + "\",\"plan\":\"activity:chain." + (i - 1) + "\",\"scope\":\"target\",\"blocker\":\"chain.pending\"}"));
                chain.Get("activities").Add(Content.Json.Parse("{\"id\":\"chain." + i + "\",\"priority\":60,\"player_orderable\":true,\"automatic_supply\":false,\"nodes\":[{\"executor\":\"work.staffed\",\"id\":\"work\",\"roles\":[\"survey\"],\"duration\":{\"numerator\":1,\"denominator\":10,\"unit\":\"hour\"}" + (i > 0 ? ",\"requirement\":\"chain.ready" + i + "\"" : "") + "}]}"));
            }
            var valid = new PriorActivityProofModule(); var composed = New(PriorActivityTests.Factory(proof: valid, dependencies: chain.ToString()));
            c.Equal(1, valid.Initializations, "iterative long authored chain composes");
            c.That(Board(composed).PlanKeys.Contains("activity:chain.127"), "last actual declared chain plan is captured");
            c.That(!View(composed).TryReadActivityCompletion("activity:chain.127", EntityId.None, out _), "long static bootstrap never fabricates actual history");
        }
    }
}
