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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    internal static class PolicyBindingTests
    {
        private const string Case = "Policy bindings: authored contracts govern physical shutdown repair and restart";
        private static readonly Code Safety = new Code("safety"), Integrity = new Code("integrity"), Processor = new Code("contract.process"), Maintenance = new Code("maintenance");
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Create(PolicyBindingModule module, bool probes = false, InitializeProbe initialize = null, Action<JsonValue> edit = null,
            IReadOnlyList<GameModule> additionalModules = null, string extraDeclaration = null, Action<JsonValue> editEquipment = null)
        {
            var equipment = Json.Parse(FrameworkProofContent.Read("equipment-condition.json"));
            equipment.Remove("entities"); equipment.Remove("signals"); equipment.Remove("transformations");
            equipment.Set("entity_states", Json.Parse("[{\"id\":\"stopped\",\"state\":\"point.service-stopped\",\"priority\":60}]"));
            foreach (var name in new[] { "equipment.spares", "equipment.tools" })
                foreach (var index in new[] { 0, 1 })
                {
                    var requirement = equipment.Get("target_resource_requirements")[index];
                    if (requirement.Get("id").Text != name) continue;
                    var quantities = requirement.Get("quantities"); var amount = quantities.Get("steel"); quantities.Remove("steel"); quantities.Set("supply", amount);
                }
            for (int i = 0; i < equipment.Get("activities").Count; i++)
            {
                var activity = equipment.Get("activities")[i]; var roles = JsonValue.NewArray(); roles.Add(JsonValue.Of("inspect"));
                activity.Get("nodes")[0].Set("roles", roles);
                if (activity.Get("id").Text == "equipment.service") activity.Set("automatic_supply", JsonValue.Of(true));
            }
            editEquipment?.Invoke(equipment);
            var authored = Json.Parse(FrameworkProofContent.Read("operation-policies.json")).Get("entities")[0].Get("components");
            var selectedModules = new List<GameModule> { new EquipmentConditionModule(), module };
            if (additionalModules != null) selectedModules.AddRange(additionalModules);
            return RecipeObservationTests.Create(new RecipeObservationModule(), management: true, initialize: initialize,
                extraModules: selectedModules,
                extraDeclarations: "[{\"id\":\"sim.equipment-condition\",\"version\":\"1.0.0\",\"requires\":[],\"documents\":[]},{\"id\":\"proof.policy-bindings\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.equipment-condition\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[\"policy-bindings.json\"]}" + (extraDeclaration == null ? "" : "," + extraDeclaration) + "]",
                extraDocuments: new Dictionary<string, string> { { "policy-bindings.json", equipment.ToString() } },
                edit: (setup, recipes) =>
                {
                    var parts = setup.Get("entities")[1].Get("components");
                    parts.Set("work_permit", authored.Get("work_permit").Clone());
                    var condition = authored.Get("equipment_condition").Clone();
                    condition.Get("states").Set("damaged", JsonValue.Of("blocked")); condition.Get("states").Set("failed", JsonValue.Of("blocked"));
                    if (probes) { condition.Remove("states"); condition.Remove("wear"); condition.Set("fault", JsonValue.Number("0")); }
                    parts.Set(probes ? "proof_condition" : "equipment_condition", condition);
                    var policy = authored.Get("operation_policy").Clone(); policy.Set("operation", JsonValue.Of("contract.process"));
                    if (probes) policy.Set("fault", JsonValue.Number("0"));
                    parts.Set(probes ? "proof_operation_policy" : "independent_policy", policy);
                    parts.Set("recurring_activity", authored.Get("recurring_activity").Clone());
                    recipes.Get("observed_recipes")[0].Get("duration").Set("numerator", JsonValue.Number("180"));
                    edit?.Invoke(setup);
                });
        }
        private static EntityId Factory(SimGame game) => Entity(game, "contract.factory");
        private static EquipmentConditionView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<EquipmentConditionView>();
        private static WorkView Work(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        private static OperationPolicyView Policy(SimGame game)
        {
            if (!View(game).ReadPolicy(Factory(game), Safety, out var value).IsNone) throw new InvalidOperationException("missing actual policy");
            return value;
        }
        private static RecurringPlanView Recurring(SimGame game)
        {
            if (!Work(game).ReadRecurringPlan(Factory(game), Maintenance, out var value).IsNone) throw new InvalidOperationException("missing actual recurring policy link");
            return value;
        }
        private static OperationPolicySettings Settings(int criticality = 30, OperationRestartPolicy restart = OperationRestartPolicy.Automatic)
            => new OperationPolicySettings(criticality, Fix.FromInt(4), Fix.FromInt(4), Fix.FromInt(8), restart);
        private static ConfigureOperationPolicyCommand Configure(SimGame game, OperationPolicySettings settings)
            => new ConfigureOperationPolicyCommand { Entity = Factory(game), Policy = Safety, Settings = settings };
        private static void Refuse(Check c, SimGame game, Command command, Code expected)
        {
            ulong hash = game.Hash(); c.Equal(expected, game.Seat(0).Preview(command), "ordinary policy preview refusal");
            c.Equal(hash, game.Hash(), "refused policy preview pure");
            c.Equal(expected, TestKit.Order(game, 0, command).Refusal, "same normal command refusal");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current policy contract build");
            c.Throws<ArgumentNullException>(() => OperationPolicyContracts.Read(null, null), "missing policy session fails before observation");
            c.Throws<ArgumentNullException>(() => ConditionContracts.Read(null, null), "missing condition session fails before observation");
            Healthy(c); Faults(c);
            foreach (string fault in new[] { "operation", "condition", "unit", "bands", "duplicate", "unknown", "unused" })
            {
                var init = new InitializeProbe(); var factory = Create(new PolicyBindingModule(), initialize: init, edit: setup =>
                {
                    var parts = setup.Get("entities")[1].Get("components"); var policy = parts.Get("independent_policy");
                    if (fault == "operation" || fault == "condition") policy.Set(fault, JsonValue.Of("absent"));
                    if (fault == "unit") policy.Set("unit", JsonValue.Of("unit.supply"));
                    if (fault == "bands") policy.Set("restart_threshold", JsonValue.Number("4"));
                    if (fault == "unknown") policy.Set("legacy", JsonValue.Of(true));
                    if (fault == "duplicate") { var other = policy.Clone(); other.Set("id", JsonValue.Of("other.safety")); var rows = JsonValue.NewArray(); rows.Add(policy); rows.Add(other); parts.Set("independent_policy", rows); }
                    if (fault == "unused") { var unused = setup.Get("entities")[1].Clone(); unused.Set("id", JsonValue.Of("unused.safety")); unused.Get("components").Get("independent_policy").Set("condition", JsonValue.Of("absent")); setup.Get("entities").Add(unused); }
                });
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(factory), "invalid actual policy metadata " + fault);
                c.Equal(0, init.Initializations, "no partial Initialize " + fault);
            }
        }
        private static void Healthy(Check c)
        {
            var module = new PolicyBindingModule(); var factory = Create(module); var game = ConstructionPlanTests.NewPoints(factory); var id = Factory(game);
            c.That(module.NativeBinding, "independent JSON metadata binds original native policy runtime and actual unit");
            var retained = Policy(game); var ids = new List<Code>(); ulong hash = game.Hash();
            c.That(View(game).CopyPolicyIds(id, ids) && ids.SequenceEqual(new[] { Safety }), "actual named policy enumeration");
            c.That(retained.Control.Enabled && retained.RestartReady && !retained.MaintenanceDue && retained.State.ChangedTick == -1, "explicit authored initial policy facts");
            c.That(!View(game, 1).CopyPolicyIds(id, ids) && ids.Count == 0 && View(game, 1).ReadPolicy(id, Safety, out var withheld) == Codes.NotFound
                && withheld.State.Settings == null, "private policy facts empty");
            c.Equal(OperationPolicyCodes.NotPolicy, View(game).ReadPolicy(id, new Code("absent"), out _), "missing policy not substituted");
            c.Equal(hash, game.Hash(), "policy and bound condition/control reads pure");
            Refuse(c, game, Configure(game, Settings(-1)), Codes.InvalidArgument);
            Refuse(c, game, Configure(game, Settings(int.MaxValue)), OperationPolicyCodes.PriorityRange);
            Refuse(c, game, Configure(game, null), Codes.InvalidArgument);
            Enable(c, game); var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                Order(c, game, Configure(game, Settings()));
                c.That(Policy(game).State.Settings.Criticality == 30 && retained.State.Settings.Criticality == 20, "new controls do not mutate retained settings");
                var schedule = Recurring(game).Schedule;
                Order(c, game, new ConfigureRecurringPlanCommand { Entity = id, Source = Maintenance, Settings = new RecurringPlanSettings(true, schedule.Priority, schedule.Interval, Code.None) });
                c.Equal(0L, Recurring(game).Schedule.LastPlan, "healthy enrollment creates no maintenance work");
                Order(c, game, new EnqueueTransformationCommand { Transformer = id, Capability = Processor, Transformation = "contract.make" });
                Until(game, null, () => Process(game).Phase == TransformationPhase.Working, "real policy processor physical supply");
                c.That(Stock(game, "source") == Fix.FromInt(16) && Process(game).Required == 180, "actual haul and authored three-hour process");
                long stressAt = game.Clock.StepIndex;
                Order(c, game, new PostOrderCommand { Target = id, Plan = "equipment.damage-work" });
                c.Equal(Fix.FromInt(10), Policy(game).Equipment.Value, "posting is not instant damage");
                Until(game, null, () => Policy(game).State.ShutdownLatched && Recurring(game).Schedule.LastPlan != 0, "machine travels stresses and original policy shuts down");
                c.That(game.Clock.StepIndex > stressAt && !Policy(game).Control.Enabled && Policy(game).Equipment.Value == Fix.FromInt(4), "physical damage owns unsafe shutdown");
                long maintenance = Recurring(game).Schedule.LastPlan; var plans = new List<PlanView>(); Work(game).CopyPlans(plans);
                c.That(plans.Single(p => p.Id == maintenance).Priority == 100, "native recurring priority is base70 plus policy criticality30");
                Refuse(c, game, new ConfigureOperationCommand { Entity = id, Operation = Processor, Enabled = true, Target = Fix.One }, OperationPolicyCodes.Unsafe);
                var branch = factory.Load(game.Save());
                Order(c, branch, new ConfigureOperationCommand { Entity = Factory(branch), Operation = Processor, Enabled = false, Target = Fix.One });
                c.That(!Policy(branch).State.ShutdownLatched && !Policy(branch).Control.Enabled && Policy(game).State.ShutdownLatched,
                    "explicit player disable acknowledges only its own session latch");
                Refuse(c, branch, new ConfigureOperationCommand { Entity = Factory(branch), Operation = Processor, Enabled = true, Target = Fix.One }, OperationPolicyCodes.Unsafe);
                var held = Process(game).Progress; var twin = factory.Load(game.Save()); for (int i = 0; i < 3; i++) Step(game, twin);
                c.That(Process(game).Progress.Equals(held), "saved shutdown earns no manufacturing work");
                long at = game.Clock.StepIndex; hash = game.Hash(); game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.That(at == game.Clock.StepIndex && hash == game.Hash(), "one clock pauses policy and service");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, twin, () => Policy(game).Equipment.Value == Fix.FromInt(10), "real spare hauling and thirty-minute repair");
                c.That(Policy(game).State.ShutdownLatched && !Policy(game).Control.Enabled, "repair cannot retroactively restart earlier policy pass");
                Until(game, twin, () => Policy(game).Control.Enabled && !Policy(game).State.ShutdownLatched, "original next-update safety restart");
                Until(game, twin, () => Process(game).QueueCount == 0, "physical three-hour process resumes to completion");
                c.That(game.Seat(0).Snapshot.Entities.Count(e => e.Def == "contract.result") == 1 && Stock(game, "source") + Stock(game, "contract.factory") + Stock(game, "machine") == Fix.FromInt(14),
                    "one timed result, three input plus three spares spent, tools conserved");
                c.That(Recurring(game).Schedule.LastPlan == maintenance && !Policy(game).MaintenanceDue, "no duplicate healthy repair plan");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "same public policy/physical workflow replays");
            }
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled safety save identity");
        }
        private static void Faults(Check c)
        {
            var points = new[] { "policy.identity", "policy.unit", "policy.operation", "policy.state", "condition.identity", "condition.unit", "condition.state" };
            foreach (string reader in new[] { "view", "preview", "ids" }) foreach (string point in points)
                foreach (OperationBindingFault mode in Enum.GetValues(typeof(OperationBindingFault)))
                {
                    if (reader == "ids" && point != "policy.identity" && point != "policy.unit") continue;
                    var module = new PolicyBindingModule(); var game = ConstructionPlanTests.NewPoints(Create(module, probes: true)); var view = View(game);
                    OperationPolicyView facts = default; var ids = new List<Code>(); Code refusal = Code.None;
                    Action read = () => {
                        if (reader == "view") refusal = view.ReadPolicy(Factory(game), Safety, out facts);
                        else if (reader == "preview") refusal = game.Seat(0).Preview(Configure(game, Settings()));
                        else view.CopyPolicyIds(Factory(game), ids);
                    };
                    read(); c.Equal(Code.None, refusal, "probe reaches healthy actual policy"); module.Arm(point, mode);
                    c.Throws<InvalidOperationException>(read, "actual policy/condition observation " + reader + "/" + point + "/" + mode);
                    c.That(reader == "view" ? facts.State.Settings == null : reader == "ids" ? ids.Count == 0 : true, "no partial facts");
                    c.That(game.Fault?.Phase == "observation.provider" && module.Reads == 1, "original provider codec fault");
                    var fault = game.Fault; c.Equal(0, game.Update(), "faulted Update stopped");
                    c.Throws<InvalidOperationException>(() => game.Hash(), "faulted hash stopped"); c.Throws<InvalidOperationException>(() => game.Save(), "faulted save stopped");
                    c.Throws<InvalidOperationException>(read, "faulted read stopped"); c.That(module.Reads == 1 && ReferenceEquals(fault, game.Fault), "first fault retained without callbacks");
                }
        }
    }
}
