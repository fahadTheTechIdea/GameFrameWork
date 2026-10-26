using System;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;
using static Beep.SimEngine.Tests.PhysicalRecoveryTestContent;

namespace Beep.SimEngine.Tests
{
    public static class RequirementExpressionTests
    {
        private static string Definitions => FrameworkProofContent.Read("requirement-expressions.json");
        public static void Register()
        {
            TestRunner.Add("Requirement expressions: strict named composition, topology and pure typed outcomes", Admission);
            TestRunner.Add("Requirement expressions: physical guarded execution, saved alternatives and cancellation", Physical);
        }
        private static List<WorkRequirementView> Preview(Check c, SimGame game, Entity target, string plan = "expression.machine")
        {
            var facts = new List<WorkRequirementView>();
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewOrderRequirements(plan, target.Id, facts), "engine previews authored expression");
            return facts;
        }
        private static WorkRequirementResult Root(List<WorkRequirementView> facts) => facts[facts.Count - 1].Result;
        private static WorkRequirementView Fact(List<WorkRequirementView> facts, string requirement)
        {
            foreach (WorkRequirementView fact in facts) if (fact.Requirement == requirement) return fact;
            throw new InvalidOperationException("missing requirement fact " + requirement);
        }
        private static void Permit(Check c, SimGame game, EntityId target, bool enabled)
            => c.That(TestKit.Order(game, 0, new SetWorkPermitCommand { Target = target, Enabled = enabled }).Accepted, "player changes an engine-owned permit");
        private static string Refusal(string json)
        {
            try { New(Factory(json)); return "accepted"; }
            catch (ContentException error) { return error.Message; }
        }
        private static void Admission(Check c)
        {
            UtilityWorkTests.Admission(c);
            ActivityGraphTests.Admission(c); // Same moved topology implementation, not a broad family run.
            GameFactory factory = Factory(Definitions); SimGame game = New(factory);
            Entity target = Spawn(game, "expression.target", 20, 20);
            Tick(game); // Drain actual creation facts through Update before the pure-read checkpoint.
            WorkRequirementGroupDef group = game.World.Content.Get<WorkRequirementGroupDef>("expression.root");
            c.That(ReferenceEquals(group, game.World.Content.Get<WorkRequirementDef>("expression.root")), "group uses the existing base catalog alias");
            c.Equal("a.reserve", group.Entries[0].Id, "authored entries resolve in ordinal identity order");
            c.That(ReferenceEquals(game.World.Content.Get<WorkRequirementDef>("expression.any"), group.Entries[1].Requirement), "forward nested reference aliases one definition");
            c.Throws<NotSupportedException>(() => ((IList<WorkRequirementEntry>)group.Entries).Clear(), "group entries are immutable");
            c.Throws<ContentException>(() => group.Read(new JsonReader(Content.Json.Parse("{}"), "probe")), "resolved group cannot be reauthored");
            ulong before = game.Hash();
            List<WorkRequirementView> facts = Preview(c, game, target);
            c.Equal(8, facts.Count, "one traversal projects nested guards, canonical resource/cost leaves and outer group");
            c.Equal(WorkRequirementStatus.Pending, Root(facts).Status, "all expression preserves nested pending readiness");
            c.Equal(new Code("expression.disabled"), Root(facts).Blocker, "actual leaf blocker drives aggregate readiness");
            c.That(Root(facts).SelectedEntry == null && !Root(facts).Quantity.HasValue, "all group fabricates neither a provider selection nor combined quantity");
            c.Equal("proof.requirement-expressions.expression.machine.work.cost", facts[0].Requirement, "canonical first outer branch is the authored cost provider");
            c.Equal(Fix.FromInt(10000), Fact(facts, "expression.reserve").Result.Quantity.Value.Available, "leaf reads the actual economy");
            WorkRequirementView permit = Fact(facts, "expression.permit");
            c.Equal(4, permit.Path.Count, "nested view exposes stable outer-group and guard path segments");
            c.Equal("proof.requirement-expressions.expression.machine.work.requirements", permit.Path[0], "path identifies canonical root requirement");
            c.Equal("guard", permit.Path[1], "path identifies authored guard branch");
            c.Equal("b.availability", permit.Path[2], "path identifies nested group branch");
            c.Equal("a.site", permit.Path[3], "path identifies leaf branch without concatenation ambiguity");
            c.Throws<NotSupportedException>(() => ((IList<string>)permit.Path)[0] = "changed", "detached paths reject mutation");
            c.Equal(before, game.Hash(), "preview reserves/consumes no input, money or machine");
            WorkRequirementView retained = Fact(facts, "expression.any");
            Permit(c, game, target.Id, true); facts = Preview(c, game, target);
            c.Equal(WorkRequirementStatus.Satisfied, Root(facts).Status, "all permits satisfied nested any");
            c.Equal("a.site", Fact(facts, "expression.any").Result.SelectedEntry, "any names the real satisfied entry");
            c.Equal(WorkRequirementStatus.Pending, retained.Result.Status, "old detached outcome does not follow later state");
            game.World.Services.Get<IEconomy>().Credit(new PlayerId(0), Fix.FromInt(10000), new Code("test.setup"));
            facts = Preview(c, game, target);
            c.Equal("a.site", Fact(facts, "expression.any").Result.SelectedEntry, "two ready alternatives tie-break by stable entry identity");
            c.That(!Fact(facts, "expression.any").Result.Quantity.HasValue, "expression choice is not a sum or substitute for individual measured quantities");
            Permit(c, game, target.Id, false); facts = Preview(c, game, target);
            c.Equal("z.cash", Fact(facts, "expression.any").Result.SelectedEntry, "pure readiness may choose another actually ready guard before acquisition");
            c.Equal(WorkRequirementStatus.Satisfied, Root(facts).Status, "ready cash branch satisfies any while another entry is pending");
            game.World.Services.Get<IEconomy>().Charge(new PlayerId(0), Fix.FromInt(19999), new Code("test.setup"));
            facts = Preview(c, game, target);
            c.Equal(WorkRequirementStatus.Blocked, Root(facts).Status, "all blocked dominates a pending sibling");
            c.Equal(new Code("expression.reserve-low"), Root(facts).Blocker, "all blockage names the actual quantitative criterion");
            c.Equal(WorkRequirementStatus.Pending, Fact(facts, "expression.any").Result.Status, "any pending dominates blocked when no alternative is ready");
            var foreign = new List<WorkRequirementView> { retained };
            Entity ownedByOther = Spawn(game, "expression.target", 28, 20, 1);
            c.Equal(Codes.NotOwned, game.Seat(0).View.Feature<WorkView>().PreviewOrderRequirements("expression.machine", ownedByOther.Id, foreign),
                "expressions reveal no foreign operational information");
            c.Equal(0, foreign.Count, "private refusal clears previous expression observations");

            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"mode\":\"all\"", "\"mode\":\"xor\""))), "unknown Boolean mode refuses explicitly");
            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"b.availability\"", "\"a.reserve\""))), "duplicate branch identity refused");
            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"requirement\":\"expression.any\"", "\"requirement\":\"expression.reserve\""))), "duplicate referenced provider refused");
            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"requirement\":\"expression.any\"", "\"requirement\":\"missing\""))), "missing nested provider refused");
            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"requirement\":\"expression.any\"", "\"requirement\":\"expression.root\""))), "self dependency refused by the shared compiler");
            string cycle = Definitions.Replace("\"requirement\":\"expression.high-reserve\"", "\"requirement\":\"expression.root\"");
            c.That(Refusal(cycle).Contains("expression.any -> expression.root -> expression.any"), "shared cycle compiler names expression references");
            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"requirement\":\"expression.root\"", "\"requirements\":[\"expression.root\"]"))),
                "flat node requirements array is removed, not silently AND-wrapped");
            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"id\":\"b.availability\"", "\"id\":\"\""))), "empty branch identities refused");
            c.Throws<ContentException>(() => New(Factory("{\"work_requirement_groups\":[{\"id\":\"empty\",\"mode\":\"all\",\"entries\":[]}]}")), "empty all cannot fabricate a ready guard");
            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"mode\":\"all\",\"entries\":[", "\"mode\":\"all\",\"extra\":true,\"entries\":["))), "unknown expression fields refused");
            Deep(c);
            InvalidAlternatives(c);
        }
        private static void Deep(Check c)
        {
            var text = new StringBuilder("{\"work_requirement_groups\":[");
            for (int i = 511; i >= 0; i--)
            {
                if (i < 511) text.Append(',');
                text.Append("{\"id\":\"deep.").Append(i).Append("\",\"mode\":\"all\",\"entries\":[{\"id\":\"child\",\"requirement\":\"")
                    .Append(i == 0 ? "deep.leaf" : "deep." + (i - 1)).Append("\"}]}");
            }
            text.Append("],\"work_permits\":[{\"id\":\"deep.leaf\",\"blocker\":\"deep.pending\",\"missing\":\"deep.missing\"}],")
                .Append("\"activities\":[{\"id\":\"deep.work\",\"player_orderable\":true,\"automatic_supply\":false,\"nodes\":[{\"id\":\"work\",\"requirement\":\"deep.511\",\"hours\":0.1}]}],")
                .Append("\"entities\":[{\"id\":\"deep.target\",\"kind\":\"structure\",\"components\":{\"tile_presence\":{\"space\":\"recovery.tiles\",\"width\":1,\"height\":1,\"blocks\":true,\"movable\":false},\"work_permit\":{\"enabled\":true}}}]}");
            SimGame game = New(Factory(text.ToString())); Entity target = Spawn(game, "deep.target", 20, 20);
            var facts = new List<WorkRequirementView>();
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewOrderRequirements("deep.work", target.Id, facts), "deep expression evaluates iteratively");
            c.Equal(513, facts.Count, "every deep group/leaf uses one diagnostic traversal");
            c.Equal(WorkRequirementStatus.Satisfied, Root(facts).Status, "deep root reads real readiness without a depth fallback");
            c.Equal(513, facts[0].Path.Count, "deep leaf path retains every authored entry");
        }
        private static void InvalidAlternatives(Check c)
        {
            string absent = Definitions.Replace("\"work_permit\":{\"enabled\":false},", "")
                .Replace("\"minimum\":20000", "\"minimum\":100");
            SimGame game = New(Factory(absent)); Entity target = Spawn(game, "expression.target", 20, 20);
            var facts = Preview(c, game, target);
            c.Equal(WorkRequirementStatus.Invalid, Root(facts).Status, "invalid capability is not hidden by another satisfied alternative");
            c.Equal(new Code("expression.missing-permit"), Root(facts).Blocker, "invalid aggregate retains typed reason");
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "expression.machine", Target = target.Id }).Accepted, "player may queue intent but cannot bypass invalid execution");
            c.Equal(OrderState.Failed, game.World.Services.Get<WorkBoard>().Orders[0].State, "invalid expression terminates ordinary named execution");
            c.Equal(Fix.FromInt(10), target.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "invalid alternatives spend no materials");
            string malformed = Definitions.Replace("\"work_permits\": [", "\"fault_requirements\":[{\"id\":\"expression.fault\"}],\"work_permits\": [")
                .Replace("\"requirement\":\"expression.high-reserve\"", "\"requirement\":\"expression.fault\"");
            SimGame fault = New(Factory(malformed)); Entity bad = Spawn(fault, "expression.target", 20, 20); Permit(c, fault, bad.Id, true);
            fault.Seat(0).Submit(new PostOrderCommand { Plan = "expression.machine", Target = bad.Id });
            c.Equal(0, fault.RunTicks(1), "malformed unselected provider cannot claim a completed tick");
            c.That(fault.Fault != null, "all alternatives are checked, not silently short-circuited around contract violations");
            c.Equal(Fix.FromInt(10), bad.Get<IInventory>().Amount(fault.World.Content.ResourceId("steel")), "malformed unselected guard causes no material spending");
            c.Equal(Fix.FromInt(10000), fault.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "malformed unselected guard causes no cash spending");
        }
        private static Order Post(Check c, SimGame game, Entity target, string plan)
        {
            var result = TestKit.Order(game, 0, new PostOrderCommand { Plan = plan, Target = target.Id });
            c.That(result.Accepted, "ordinary player command posts guarded activity");
            return game.World.Services.Get<WorkBoard>().Order((int)result.Arg0);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, params SimGame[] twins)
        {
            for (int i = 0; !done() && i < 500; i++) { Tick(game); foreach (var twin in twins) Tick(twin); }
            if (!done())
            {
                var details = new List<string>();
                foreach (Order order in game.World.Services.Get<WorkBoard>().Orders)
                    foreach (ActivityNodeExecution node in order.Nodes)
                        details.Add(order.Id + ":" + node.Id + "=" + node.State + "/" + node.Progress + "/" + node.Blocker);
                throw new InvalidOperationException(stage + " stalled at tick " + game.Tick + ": " + string.Join("; ", details));
            }
        }
        private static void Physical(Check c)
        {
            UtilityWorkTests.Physical(c);
            ActivityGraphTests.Physical(c); // Directly affected named execution/codec route only.
            GameFactory factory = Factory(Definitions); SimGame game = New(factory);
            Entity target = Spawn(game, "expression.target", 20, 20);
            Order order = Post(c, game, target, "expression.machine"); var node = order.Node("work");
            ResourceId steel = game.World.Content.ResourceId("steel");
            c.Equal(new Code("expression.disabled"), node.Blocker, "nested pending guard is the actual node blocker");
            c.Equal(Fix.FromInt(10), target.Get<IInventory>().Amount(steel), "unready expression spends no native input");
            c.Equal(Fix.FromInt(10000), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "unready expression spends no cash");
            SimGame pending = factory.Load(game.Save());
            for (int i = 0; i < 3; i++) { Tick(game); Tick(pending); }
            c.Equal(game.Hash(), pending.Hash(), "pending expression and named state restore through the existing codec");
            Permit(c, game, target.Id, true); Permit(c, pending, target.Id, true);
            Reach(game, () => node.Progress.WholeTicks >= 3, "guarded physical work", pending);
            c.Equal(game.Hash(), pending.Hash(), "saved pending twin reaches identical physical work");
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Amount(steel), "satisfied expression consumes actual node bill once");
            c.Equal(Fix.FromInt(9995), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "satisfied expression pays once");
            Entity worker = game.World.Entities.Get(game.World.Services.Get<WorkBoard>().Job(node.JobId).Worker);
            FixVec2 position = worker.Get<ITilePresence>().Position;
            Permit(c, game, target.Id, false);
            WorkProgress progress = node.Progress;
            c.Equal(NodeState.Paused, node.State, "nested any/all guard pauses existing machine job");
            c.Equal(position, worker.Get<ITilePresence>().Position, "pause does not move or teleport the worker");
            SimGame paused = factory.Load(game.Save());
            for (int i = 0; i < 4; i++) { Tick(game); Tick(paused); }
            c.Equal(progress, node.Progress, "expression pause retains actual elapsed work");
            c.Equal(game.Hash(), paused.Hash(), "saved paused continuation is deterministic");
            var active = new List<WorkRequirementView>(); ulong before = game.Hash();
            c.That(game.Seat(0).View.Feature<WorkView>().TryCopyOrderRequirements(order.Id, active), "active per-entry facts use same evaluator");
            c.Equal(node.Blocker, Root(active).Blocker, "preview and execution agree on nested blocker");
            c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyOrderRequirements(order.Id, active) && active.Count == 0, "private active branch facts do not leak");
            c.Equal(before, game.Hash(), "active branch diagnostics are pure");
            Permit(c, game, target.Id, true); Permit(c, paused, target.Id, true);
            Reach(game, () => !order.IsActive, "resumed expression", paused);
            c.Equal(OrderState.Done, order.State, "same physical job resumes and finishes");
            c.Equal(game.Hash(), paused.Hash(), "resumed saved execution has identical hash");
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Amount(steel), "expression change never consumes materials twice");
            c.Equal(Fix.FromInt(9995), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "expression change never charges twice");

            SimGame timerGame = New(factory); Entity timerTarget = Spawn(timerGame, "expression.target", 20, 20);
            Order timer = Post(c, timerGame, timerTarget, "expression.timer"); Permit(c, timerGame, timerTarget.Id, true);
            Reach(timerGame, () => timer.Node("wait").Progress.WholeTicks >= 2, "guarded timer");
            Permit(c, timerGame, timerTarget.Id, false); WorkProgress elapsed = timer.Node("wait").Progress;
            SimGame timerTwin = factory.Load(timerGame.Save());
            for (int i = 0; i < 3; i++) { Tick(timerGame); Tick(timerTwin); }
            c.Equal(elapsed, timer.Node("wait").Progress, "timer uses same nested alternative readiness");
            c.Equal(timerGame.Hash(), timerTwin.Hash(), "guarded timer restores identically");
            c.That(TestKit.Order(timerGame, 0, new CancelOrderCommand { Order = timer.Id }).Accepted, "cancel uses existing node owner");
            c.Equal(NodeState.Cancelled, timer.Node("wait").State, "cancelled expression cannot resume silently");
            c.Equal(elapsed, timer.Node("wait").Progress, "cancelled expression retains attained time");
            c.Equal(timerGame.Hash(), factory.Load(timerGame.Save()).Hash(), "cancelled expression saves without a separate choice ledger");
        }
    }
}
