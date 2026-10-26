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
    internal static class ConditionInvocationTests
    {
        private const string Case = "Conditions: single invocation, recursive providers and durable physical play";
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        internal const string TerminalCase = "Conditions: first provider fault cuts off known null and missing view queries";
        internal static void Register()
        { TestRunner.Add(Case, Run); TestRunner.Add(TerminalCase, Terminal); }
        private static void Terminal(Check c)
        {
            Faults(c);
            var factory = Factory(); var game = New(factory); var healthy = New(factory);
            var view = View(game); var retainedFault = game.Fault;
            c.Equal(Codes.UnknownDef, view.CheckCondition(null), "healthy null remains an explicit missing definition");
            c.Equal(Codes.UnknownDef, view.CheckCondition("missing"), "healthy unknown remains an explicit missing definition");
            c.Throws<InvalidOperationException>(() => view.CheckCondition("probe.throw"), "actual original provider records first fault");
            retainedFault = game.Fault;
            c.That(retainedFault != null, "original fault owner retains provider failure");
            foreach (string id in new[] { "probe.repeat", null, "missing" })
                c.Throws<InvalidOperationException>(() => view.CheckCondition(id), "no healthy-shaped query after fault: " + (id ?? "null"));
            c.That(ReferenceEquals(retainedFault, game.Fault), "refused observations preserve original first cause");
            long at = game.Clock.StepIndex;
            c.Equal(0, game.Update(), "fault stops update");
            c.Equal(at, game.Clock.StepIndex, "one clock stays stopped");
            c.Throws<InvalidOperationException>(() => game.Save(), "fault cannot be checkpointed");
            c.Throws<InvalidOperationException>(() => game.Hash(), "fault cannot present a healthy hash");
            c.Equal(Codes.UnknownDef, View(healthy).CheckCondition("missing"), "other session retains healthy missing result");
            c.Equal("controller.risk-disabled", View(healthy).CheckCondition("probe.repeat").Id, "other session retains real condition evaluation");
        }
        private static GameFactory Factory()
        {
            var sites = Content.Json.Parse(FrameworkProofContent.Read("conditional-plan-sites.json"));
            var rows = sites.Get("plan_site_sets")[0].Get("sites");
            for (int i = 0; i < rows.Count; i++) rows[i].Set("conditions", Content.Json.Parse("[\"probe.repeat\"]"));
            return ConditionalPlanSiteTests.Factory(sites.ToString(), conditionsDocument: "condition-invocation.json", conditionModule: new ConditionCallProofModule());
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static GameConditionsView View(SimGame game) => game.Seat(0).View.Feature<GameConditionsView>();
        private static ConditionCallProbeDef Probe(SimGame game, string id) => game.World.Content.Get<ConditionCallProbeDef>(id);
        private static PlaybookStepSnapshot Claim(SimGame game)
        {
            var rows = new List<PlaybookStepSnapshot>();
            game.Seat(0).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(0), rows); return rows.Single();
        }
        private static List<PlanView> Plans(SimGame game)
        { var rows = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(rows); return rows; }
        private static Fix Stock(SimGame game)
        {
            Fix stock = Fix.Zero; var supply = game.World.Content.ResourceId("supply");
            foreach (var entity in game.World.Entities.All)
                if (entity.Alive && entity.Owner == new PlayerId(0) && entity.Get<IInletPort>() is IInletPort inlet) stock += inlet.Amount(supply);
            return stock;
        }
        private static void Order(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary current admission");
            c.Equal(hash, game.Hash(), "ordinary preview pure"); c.That(TestKit.Order(game, 0, command).Accepted, "normal command accepted");
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.RunTicks(1); twin?.RunTicks(1);
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("condition scope time=" + game.Tick + "/fault=" + game.Fault);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "one revised condition contract/call owner");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no saved invocation stack/readiness");
            Physical(c);
        }
        private static void Faults(Check c)
        {
            foreach (string id in new[] { "probe.self", "probe.alias-loop", "probe.pair.a", "probe.swallow", "probe.throw", "probe.bad-code" })
            {
                var game = New(Factory()); var conditions = View(game);
                if (id == "probe.bad-code")
                    c.Throws<ArgumentException>(() => conditions.CheckCondition(id), "invalid typed blocker refuses: " + id);
                else
                    c.Throws<InvalidOperationException>(() => conditions.CheckCondition(id), "bad provider rejects without unbounded recursive query: " + id);
                c.That(game.Fault != null, "actual first-cause engine fault retained: " + id);
                var captured = Probe(game, id).Last;
                c.That(captured != null, "actual failed provider invocation captured");
                c.Throws<InvalidOperationException>(() => { var unused = captured.View; }, "failed invocation expires");
                c.Throws<InvalidOperationException>(() => captured.Check(Probe(game, id)), "expired context cannot restart");
                c.Throws<InvalidOperationException>(() => game.Save(), "failed provider query cannot checkpoint");
                c.Throws<InvalidOperationException>(() => game.Hash(), "swallowed/invalid query cannot present healthy hash");
            }
        }
        private static void Physical(Check c)
        {
            var factory = Factory(); var game = New(factory); var other = New(factory); ulong hash = game.Hash();
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IBusinessAccounts>(), "condition scope needs no money/business");
            c.Equal("controller.risk-disabled", View(game).CheckCondition("probe.repeat").Id, "repeated child DAG path is not recursion");
            var held = Probe(game, "probe.repeat").Last;
            c.Throws<InvalidOperationException>(() => { var unused = held.View; }, "successful root context expires");
            c.Throws<InvalidOperationException>(() => held.Check(Probe(game, "probe.repeat")), "success cannot retain executable scope");
            c.Equal("controller.risk-disabled", View(game).CheckCondition("probe.alias").Id, "same-company view aliases inherit actual root context");
            c.Equal(hash, game.Hash(), "healthy repeated/alias observations pure");
            c.That(Probe(game, "probe.repeat").AuthoringPath.Contains("condition-invocation.json"), "independent provider has actual master file identity");
            for (int i = 0; i < 6; i++) Step(other);
            c.Equal(Code.None, View(other).CheckCondition("probe.repeat"), "interleaved second session uses its own actual clock");
            c.Equal("controller.risk-disabled", View(game).CheckCondition("probe.repeat").Id, "no context/time/result borrowed across sessions");
            var worker = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "machine" && e.Owner == new PlayerId(0)).Id;
            Order(c, game, new ConfigureWorkPolicyCommand { Worker = worker, Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
            using (var recorder = game.RecordReplay(Limits))
            {
                Order(c, game, new SetControllerCommand { Controller = Controller.Autopilot });
                c.That(Plans(game).Count == 0, "real private logical wait, no fake plan or construction");
                var twin = factory.Load(game.Save());
                for (int i = 0; i < 12 && Claim(game).PendingCommand == 0; i++) Step(game, twin);
                c.That(Claim(game).PendingCommand > 0 && Plans(game).Count == 0, "native clock leaf opens normal queued placement through shared scope");
                c.Equal(Code.None, View(game).CheckCondition("probe.alias"), "healthy aliases remain ready after actual elapsed condition");
                twin = factory.Load(game.Save()); Step(game, twin);
                c.That(Plans(game).Single().State == PlanState.Active && Stock(game) == Fix.FromInt(52), "actual supplied project, no instant work/resources");
                twin = factory.Load(game.Save());
                for (int i = 0; i < 2000 && Claim(game).Status != PlaybookStepStatus.Done; i++) Step(game, twin);
                if (Claim(game).Status != PlaybookStepStatus.Done) throw new InvalidOperationException("physical scope wait time=" + game.Tick + "/blocker=" + Claim(game).Blocker);
                c.Equal(Fix.FromInt(44), Stock(game), "physical machine hauling/build consumes8 of52");
                c.Equal(1, game.Seat(0).Snapshot.Entities.Count(e => e.Def == "point.tower"), "one real completed structure");
                var result = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(result.Success && result.Game != null && result.Game.Hash() == game.Hash(), "saved physical replay agrees after invocation redesign");
            }
        }
    }
}
