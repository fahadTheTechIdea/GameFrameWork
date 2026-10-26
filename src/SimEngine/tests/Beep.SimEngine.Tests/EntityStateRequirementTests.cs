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
    public static class EntityStateRequirementTests
    {
        public const string Case = "Entity state requirements: authored guards, physical pauses and saved work";
        public static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(string content = null, string world = null)
        {
            var authored = Content.Json.Parse(world ?? FrameworkProofContent.Read("native-sensor-world.json"));
            authored.Get("scenarios")[0].Get("settings").Get("sim.world-setup").Set("profile", JsonValue.Of("state.start"));
            var master = Content.Json.Parse(FrameworkProofContent.Read("native-sensor-master.json"));
            for (int i = 0; i < master.Get("modules").Count; i++)
            {
                var module = master.Get("modules")[i];
                if (module.Get("id").Text == "sim.work") module.Get("documents").Add(JsonValue.Of("native-state-work.json"));
                if (module.Get("id").Text == "sim.world-setup") module.Get("documents").Add(JsonValue.Of("native-state-startup.json"));
            }
            return LiveSensorTests.Factory(world: authored.ToString(), master: master.ToString(), additionalDocuments: new Dictionary<string, string> {
                ["native-state-work.json"] = content ?? FrameworkProofContent.Read("native-state-work.json"),
                ["native-state-startup.json"] = FrameworkProofContent.Read("native-state-startup.json") });
        }
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Def.Id == id);
        private static WorkView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static Order Order(SimGame game, int id) => game.World.Services.Get<WorkBoard>().Order(id);
        private static bool Known(SimGame game) => game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "survey.reading", out _);
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("state requirement time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " site=" + Find(game, "state.site").State +
                " worker=" + Find(game, "survey.machine").State + " orders=" + string.Join(";", game.World.Services.Get<WorkBoard>().Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static void Control(Check c, SimGame game, EntityId entity, string operation, bool enabled, int target, SimGame twin = null)
        {
            var command = new ConfigureOperationCommand { Entity = entity, Operation = new Code(operation), Enabled = enabled, Target = Fix.FromInt(target) };
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal state-driving operation preview");
            c.That(TestKit.Order(game, 0, command).Accepted, "normal capability control settles entity state in Update");
            if (twin != null) c.That(TestKit.Order(twin, 0, command).Accepted, "same operation command in saved continuation");
        }
        private static Order Start(Check c, SimGame game)
        {
            var command = new PostOrderCommand { Plan = "state.sample", Target = Find(game, "state.site").Id, Priority = 60 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "state mismatch is readiness, not invalid player intent");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "normal state-guarded physical order");
            return Order(game, (int)result.Arg0);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "state guard build identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no duplicate state or requirement save payload");
            var factory = Factory(); var game = LiveSensorTests.New(factory);
            var site = Find(game, "state.site"); var machine = Find(game, "survey.machine");
            ResourceId steel = game.World.Content.ResourceId("steel");
            c.Equal(2, game.World.Entities.All.Count, "actual native JSON startup only");
            c.That(!game.World.Services.Has<IEconomy>(), "site state and physical work require no business");
            var definition = game.World.Content.Get<TargetStateRequirementDef>("state.site-ready");
            c.That(definition.AuthoringPath.Contains("native-state-work.json"), "actual master-listed state requirement");
            c.That(ReferenceEquals(definition, game.World.Content.Get<WorkRequirementDef>(definition.Id)), "single requirement content authority");
            c.Throws<NotSupportedException>(() => ((IList<IEntityStateDefinition>)definition.States).Clear(), "resolved state references are immutable");
            c.Equal("survey.idle", definition.States.Single().Id, "state reference resolves actual authored stable ID");
            Control(c, game, machine.Id, "survey.sensor", false, 2);
            c.Equal(new Code("stopped"), machine.State, "actual worker entity state settled by normal Update");
            c.Equal(new Code("stopped"), site.State, "actual authored inactive site settled by normal Update");
            var criteria = new List<WorkRequirementView>(); var candidates = new List<WorkWorkerCandidate>();
            ulong before = game.Hash();
            c.Equal(Code.None, View(game).PreviewWorkerCandidates("state.worker-inspect", "inspect", site.Id, candidates), "worker state guard offers through existing dispatch");
            c.That(candidates.Single().Result.Blocker == new Code("state.machine-not-ready"), "stopped actual candidate is not substituted");
            c.Equal(Code.None, View(game).PreviewOrderRequirements("state.sample", site.Id, criteria), "all-expression state diagnostics");
            c.That(criteria.Any(r => r.Requirement == definition.Id && r.Result.Blocker == definition.Blocker), "actual stopped site blocks readiness");
            c.Equal(Code.None, View(game).PreviewOrderRequirements("state.inspect", site.Id, criteria), "any-expression state diagnostics");
            c.That(criteria.Any(r => r.Requirement == "state.either" && r.Result.Status == WorkRequirementStatus.Satisfied && r.Result.SelectedEntry == "b.shutdown"), "any branch chooses actual committed shutdown state");
            c.Equal(before, game.Hash(), "state guards/readiness/worker offers never update or reserve");
            Control(c, game, machine.Id, "survey.sensor", true, 2);
            c.Equal(Code.None, View(game).PreviewWorkerCandidates("state.worker-inspect", "inspect", site.Id, candidates), "same guard re-evaluates current candidate");
            c.That(candidates.Single().Result.Status == WorkRequirementStatus.Satisfied, "actual idle/moving worker state permits authored alternatives");
            Order work = Start(c, game);
            c.Equal(0, work.Node("sample").JobId, "unready site acquires no machine job");
            c.Equal(Fix.FromInt(20), site.Get<IInventory>().Amount(steel), "unready site spends no input");
            c.Equal(definition.Blocker, work.Node("sample").Blocker, "execution shares state blocker");
            c.That(!View(game, 1).TryCopyOrderRequirements(work.Id, criteria) && criteria.Count == 0, "foreign company receives no private site criteria");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "state-blocked order saved through existing owners");
            Control(c, game, site.Id, "state.site-control", true, 1, twin);
            c.Equal(new Code("idle"), site.State, "normal operation clears actual site state");
            Reach(game, () => work.Node("sample").JobId > 0, "state-gated machine acquisition", twin);
            c.That(work.Node("sample").Progress.WholeTicks == 0 && !Known(game), "ready state alone earns neither work nor observation");
            Reach(game, () => work.Node("sample").Progress.WholeTicks > 0, "physical travel and actual work", twin);
            c.Equal(game.Hash(), twin.Hash(), "blocked save resumes physical workflow identically");
            c.Equal(Fix.FromInt(12), site.Get<IInventory>().Amount(steel), "real work consumes eight once");
            c.That(!Known(game), "partial state-authorized work cannot earn completion effect");
            Control(c, game, site.Id, "state.site-control", false, 1, twin);
            Update(game, twin); long progress = work.Node("sample").Progress.WholeTicks;
            int job = work.Node("sample").JobId; long since = site.StateSince;
            for (int i = 0; i < 3; i++) Update(game, twin);
            c.Equal(NodeState.Paused, work.Node("sample").State, "actual changed target state pauses existing node");
            c.Equal(progress, work.Node("sample").Progress.WholeTicks, "blocked state earns no further work");
            c.Equal(job, work.Node("sample").JobId, "pause retains same authoritative job");
            c.Equal(since, site.StateSince, "pure guard reads do not retrigger state transitions");
            c.Equal(Fix.FromInt(12), site.Get<IInventory>().Amount(steel), "pause recreates or spends no stock");
            c.Equal(game.Hash(), twin.Hash(), "state pause remains deterministic");
            c.That(View(game).TryCopyOrderRequirements(work.Id, criteria) && criteria.Any(r => r.Result.Blocker == definition.Blocker), "paused diagnostics read same actual state");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "paused actual state and spent node save exactly");
            Control(c, game, site.Id, "state.site-control", true, 1, twin);
            Reach(game, () => work.State == OrderState.Done, "real resumed completion", twin);
            c.Equal(game.Hash(), twin.Hash(), "paused saved continuation completes identically");
            c.Equal(Fix.FromInt(12), site.Get<IInventory>().Amount(steel), "resumption never consumes startup stock twice");
            c.That(Known(game) && work.Node("sample").CompletedBy == machine.Id, "actual physical machine earns sole completion effect");
            Order cancelled = Start(c, game);
            Reach(game, () => cancelled.Node("sample").Progress.WholeTicks > 0, "second real state-authorized work");
            c.Equal(Fix.FromInt(4), site.Get<IInventory>().Amount(steel), "second order spends its actual own eight");
            Control(c, game, site.Id, "state.site-control", false, 1); Update(game);
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = cancelled.Id }).Accepted, "normal paused-work cancellation");
            c.Equal(OrderState.Cancelled, cancelled.State, "cancelled state guard retains terminal order");
            c.Equal(Fix.FromInt(4), site.Get<IInventory>().Amount(steel), "cancellation does not refund spent materials");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled state/work saves without second state ledger");
            var isolated = LiveSensorTests.New(factory); Update(isolated);
            c.Equal(Fix.FromInt(20), Find(isolated, "state.site").Get<IInventory>().Amount(steel), "interleaved actual site has independent materials");
            c.That(!Known(isolated), "interleaved session borrows no completed observation");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            string source = FrameworkProofContent.Read("native-state-work.json");
            foreach (string mode in new[] { "missing", "empty", "duplicate", "absent", "blocker", "extra" })
            {
                JsonValue root = Content.Json.Parse(source); JsonValue leaf = root.Get("target_state_requirements")[0];
                if (mode == "missing") leaf.Set("states", Content.Json.Parse("[\"missing\"]"));
                if (mode == "empty") leaf.Set("states", Content.Json.Parse("[]"));
                if (mode == "duplicate") leaf.Set("states", Content.Json.Parse("[\"survey.idle\",\"survey.idle\"]"));
                if (mode == "absent") leaf.Remove("states");
                if (mode == "blocker") leaf.Set("blocker", JsonValue.Of(" "));
                if (mode == "extra") leaf.Set("fallback", JsonValue.Of("idle"));
                c.Throws<ContentException>(() => LiveSensorTests.New(Factory(root.ToString())), "selected state guard refuses " + mode);
            }
            JsonValue noRoles = Content.Json.Parse(source); noRoles.Get("activities")[2].Get("nodes")[0].Set("roles", Content.Json.Parse("[]"));
            c.Throws<ContentException>(() => LiveSensorTests.New(Factory(noRoles.ToString())), "worker state guard requires actual node roles");
            JsonValue world = Content.Json.Parse(FrameworkProofContent.Read("native-sensor-world.json"));
            var alias = world.Get("entity_states")[0].Clone(); alias.Set("id", JsonValue.Of("state.alias")); alias.Set("default", JsonValue.Of(false)); world.Get("entity_states").Add(alias);
            JsonValue duplicate = Content.Json.Parse(source); duplicate.Get("target_state_requirements")[0].Set("states", Content.Json.Parse("[\"survey.idle\",\"state.alias\"]"));
            c.Throws<ContentException>(() => LiveSensorTests.New(Factory(duplicate.ToString(), world.ToString())), "same published state under different IDs cannot duplicate guard choices");
        }
    }
}
