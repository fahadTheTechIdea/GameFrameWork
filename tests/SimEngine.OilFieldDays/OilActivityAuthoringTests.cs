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
    public static class OilActivityAuthoringTests
    {
        public const string CaseName = "Oil activities: authored drilling DAG material stages and optional survey branches";
        public static void Register() => TestRunner.Add(CaseName, Run);
        private static void Sources(PreloadedFileSource files, JsonValue master)
        {
            var authored = Json.Parse(OilFieldDaysProofContent.Read("oil-work.json"));
            var selected = JsonValue.NewArray();
            for (int i = 0; i < authored.Get("activities").Count; i++)
                if (authored.Get("activities")[i].Get("id").Text == "drill_well")
                { var activity = authored.Get("activities")[i]; activity.Set("on_complete", Json.Parse("null")); selected.Add(activity); }
            authored.Set("activities", selected); authored.Remove("entity_activities"); authored.Remove("entity_replacement_effects"); authored.Remove("surveys");
            var requirements = authored.Get("target_resource_requirements"); var picked = JsonValue.NewArray();
            for (int i = 0; i < requirements.Count; i++) if (requirements[i].Get("id").Text.StartsWith("oil.activity.drill_well.")) picked.Add(requirements[i]);
            authored.Set("target_resource_requirements", picked);
            var money = Json.Parse(files.ReadText("oil-business-costs.json"));
            var bindings = money.Get("activity_cost_bindings"); var current = JsonValue.NewArray();
            for (int i = 0; i < bindings.Count; i++)
                if (bindings[i].Get("activity").Text.StartsWith("oil.line.") || bindings[i].Get("activity").Text == "drill_well") current.Add(bindings[i]);
            money.Set("activity_cost_bindings", current);
            files.Add("oil-business-costs.json", money.ToString()).Add("oil-work.json", authored.ToString());
            for (int i = 0; i < master.Get("modules").Count; i++) if (master.Get("modules")[i].Get("id").Text == "proof.oil-infrastructure")
                master.Get("modules")[i].Get("documents").Add(JsonValue.Of("oil-work.json"));
        }
        private static void Fixture(JsonValue root)
        {
            for (int i = 0; i < root.Get("entities").Count; i++)
            {
                var entity = root.Get("entities")[i];
                if (entity.Get("id").Text == "proof.infrastructure.source")
                { entity.Set("tags", Json.Parse("[\"pad\"]")); entity.Get("components").Get("storage").Set("initial_stock", Json.Parse("{\"casing_string\":8,\"drilling_mud\":20,\"cementing_supplies\":12}")); }
                if (entity.Get("id").Text == "proof.infrastructure.builder") entity.Get("components").Get("worker").Set("roles", Json.Parse("[\"drill\"]"));
            }
        }
        private static void Run(Check check)
        {
            var all = Json.Parse(OilFieldDaysProofContent.Read("oil-work.json"));
            check.Equal(10, all.Get("activities").Count + all.Get("entity_activities").Count, "all original activity identities migrated");
            foreach (bool money in new[] { false, true })
            {
                var factory = OilAuthoredInfrastructureTests.Factory(authoredScenario: money ? "frontier" : "sandbox", editFixture: Fixture, editSources: Sources);
                var game = OilAuthoredInfrastructureTests.New(factory); var target = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "proof.infrastructure.source").Id;
                var command = new PostOrderCommand { Plan = "drill_well", Target = target };
                check.Equal(Code.None, game.Seat(0).Preview(command), "legal pad starts without survey/logging knowledge");
                game.Seat(0).Submit(command); game.Update(); var twin = factory.Load(game.Save());
                var work = game.Seat(0).View.Feature<WorkView>(); var orders = new List<OrderView>(); work.CopyOrders(orders);
                int orderId = orders.Single(o => o.Plan == "activity:drill_well").Id;
                var savedStages = new HashSet<string>(); bool finished = false;
                for (int frame = 0; frame < 720; frame++)
                {
                    game.Update(); twin.Update(); check.Equal(game.Hash(), twin.Hash(), "authored DAG saved twin identity");
                    var nodes = new List<ActivityNodeView>();
                    if (!work.TryCopyNodes(orderId, nodes))
                    {
                        finished = work.TryReadActivityCompletion("activity:drill_well", target, out var completion)
                            && completion.Order == orderId;
                        check.That(finished, "pruned order has actual earned completion, not inferred disappearance");
                        break;
                    }
                    check.Equal(4, nodes.Count, "rig/drill/casing-cement/completion graph");
                    foreach (var node in nodes)
                    {
                        if (node.State == NodeState.Working && savedStages.Add(node.Id)) twin = factory.Load(game.Save());
                        if (node.State == NodeState.Working || node.State == NodeState.Done)
                            foreach (string dependency in node.Dependencies) check.That(nodes.Single(n => n.Id == dependency).State == NodeState.Done, "dependencies settle before downstream work");
                    }
                    if (nodes.All(n => n.State == NodeState.Done)) { finished = true; break; }
                }
                check.That(finished, "bounded authored drilling stages finish");
                check.That(game.Clock.StepIndex >= 6480, "original108hours plus physical travel/departure earned");
                check.That(savedStages.Contains("rig_up") && savedStages.Contains("drill") && savedStages.Contains("casing_cementing"), "save/load covers each machine-executed stage");
                if (money)
                {
                    var business = game.Seat(0).View.Feature<BusinessView>();
                    check.Equal(Code.None, business.ReadAccount(new PlayerId(0), out var account), "native optional account");
                    check.Equal(Fix.FromInt(5960000), account.Balance, "original40000 charged once at casing/completion work");
                }
                else check.That(!game.Modules.Contains(TycoonModule.ModuleId), "physical drilling stages require no Tycoon module");
                check.That(game.Fault == null && twin.Fault == null, "healthy native DAG sessions");
            }
        }
    }
}
