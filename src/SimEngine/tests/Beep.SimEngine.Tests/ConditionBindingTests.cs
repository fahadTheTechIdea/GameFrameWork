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
    // Master-listed JSON and normal public player controls; no timing, repair or wear logic here.
    internal static class ConditionBindingTests
    {
        private const string Case = "Condition bindings: independent metadata and wear govern physical service";
        private static readonly Code Integrity = new Code("integrity"), Safety = new Code("safety"), Processor = new Code("contract.process");
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Create(ConditionBindingModule module, InitializeProbe initialize = null,
            Action<JsonValue> edit = null, Action<JsonValue> editEquipment = null, GameModule extraModule = null, string extraDeclaration = null)
            => PolicyBindingTests.Create(new PolicyBindingModule(), initialize: initialize,
                additionalModules: extraModule == null ? new GameModule[] { module } : new GameModule[] { module, extraModule },
                extraDeclaration: "{\"id\":\"proof.condition-bindings\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.equipment-condition\",\"version\":\"1.0.0\"}],\"documents\":[]}" + (extraDeclaration == null ? "" : "," + extraDeclaration),
                editEquipment: equipment =>
                {
                    equipment.Get("units").Add(Json.Parse("{\"id\":\"unit.boolean\",\"dimension\":\"boolean\",\"scale\":{\"numerator\":1,\"denominator\":1},\"discrete\":true}"));
                    equipment.Set("signals", Json.Parse("[{\"id\":\"wear.enabled\",\"kind\":\"boolean\",\"metric\":\"operation.enabled\",\"unit\":\"unit.boolean\",\"providers\":[{\"component\":\"observed_processor\",\"schema\":1}]}]"));
                    equipment.Set("independent_wear", Json.Parse("[{\"id\":\"wear.independent\",\"unit\":\"unit.condition\",\"quantity\":1,\"period\":{\"numerator\":3,\"denominator\":1,\"unit\":\"minute\"},\"conditions\":[{\"id\":\"operation\",\"source\":\"contract.process\",\"signal\":\"wear.enabled\",\"expected\":true,\"blocker\":\"wear.disabled\"}]}]"));
                    editEquipment?.Invoke(equipment);
                }, edit: setup =>
                {
                    var parts = setup.Get("entities")[1].Get("components"); var authored = parts.Get("equipment_condition");
                    authored.Set("wear", JsonValue.Of("wear.independent")); parts.Remove("equipment_condition"); parts.Set("independent_condition", authored);
                    edit?.Invoke(setup);
                });
        private static EntityId Factory(SimGame game) => Entity(game, "contract.factory");
        private static EquipmentConditionView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<EquipmentConditionView>();
        private static ConditionSnapshot Condition(SimGame game)
        { if (!View(game).Read(Factory(game), Integrity, out var value).IsNone) throw new InvalidOperationException("missing actual condition"); return value; }
        private static ConditionWearSnapshot Wear(SimGame game)
        { if (!View(game).ReadWear(Factory(game), Integrity, out var value).IsNone) throw new InvalidOperationException("missing actual wear"); return value; }
        private static OperationPolicyView Policy(SimGame game)
        { if (!View(game).ReadPolicy(Factory(game), Safety, out var value).IsNone) throw new InvalidOperationException("missing safety"); return value; }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current condition contract build"); c.Equal(154, EngineInfo.SaveFormat, "same four-field condition codec");
            c.Throws<ArgumentNullException>(() => ConditionWearContracts.Read(null, null, out _), "missing wear session");
            c.Throws<ArgumentNullException>(() => ConditionWearContracts.ReadExposure(null, null), "missing exposure session");
            c.Throws<ArgumentNullException>(() => ConditionContracts.Check(null, null, default), "missing admission session");
            Healthy(c); Bindings(c); Faults(c); AdmissionFaults(c); Invalid(c);
        }
        private static void Healthy(Check c)
        {
            var module = new ConditionBindingModule(); var factory = Create(module); var game = ConstructionPlanTests.NewPoints(factory); var id = Factory(game);
            c.That(module.NativeBinding, "independent JSON condition metadata and direct wear contract bind ORIGINAL equipment runtime");
            var retained = Wear(game); ulong hash = game.Hash(); var ids = new List<Code>();
            c.That(View(game).CopyIds(id, ids) && ids.SequenceEqual(new[] { Integrity }), "complete detached actual condition IDs");
            c.That(retained.Rule == "wear.independent" && retained.Exposure.ElapsedTicks == 0 && retained.Decision.Advance
                && Condition(game).Value == Fix.FromInt(10), "reading future eligibility never applies wear");
            c.That(!View(game, 1).CopyIds(id, ids) && ids.Count == 0 &&
                View(game, 1).ReadWear(id, Integrity, out var hidden) == Codes.NotFound && hidden.Rule == null, "company-private empty wear facts");
            c.Equal(ConditionWearCodes.NotWear, View(game).ReadWear(id, new Code("absent"), out _), "missing provider never substituted");
            c.Equal(hash, game.Hash(), "all condition/decision/ID reads pure");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                for (int i = 0; i < 3; i++) Step(game);
                c.That(Condition(game).Value == Fix.FromInt(9) && Wear(game).Exposure.ElapsedTicks == 3, "three actual one-minute updates earn exactly one wear unit");
                var fractional = factory.Load(game.Save()); Step(game, fractional);
                c.That(Condition(game).Value < Fix.FromInt(9) && Wear(game).Exposure.ElapsedTicks == 4, "fractional native wear survives save/continue");
                long at = game.Clock.StepIndex; hash = game.Hash(); game.Clock.Pause(); fractional.Clock.Pause(); Step(game, fractional);
                c.That(at == game.Clock.StepIndex && hash == game.Hash(), "sole clock pause prevents wear"); game.Clock.Resume(); fractional.Clock.Resume();
                Until(game, fractional, () => Policy(game).State.ShutdownLatched, "earned wear reaches ordinary shutdown");
                c.That(Condition(game).Value == Fix.FromInt(4) && !Policy(game).Control.Enabled && Wear(game).Exposure.ElapsedTicks == 18,
                    "existing policy stops physical degradation after eighteen earned minutes");
                c.That(game.Seat(0).Snapshot.Entities.Single(e => e.Id == id).State != Code.None, "ordinary entity Update settles authored condition state");
                var stopped = Wear(game); for (int i = 0; i < 3; i++) Step(game, fractional);
                c.That(Wear(game).Exposure.ElapsedTicks == stopped.Exposure.ElapsedTicks && Wear(game).Decision.Blocker == new Code("wear.disabled"),
                    "gated shutdown does not fabricate eligible exposure");
                Enable(c, game); // Normal player machine policy; no test-side assignment or stock writes.
                long repairAt = game.Clock.StepIndex;
                Order(c, game, new PostOrderCommand { Target = id, Plan = "equipment.service" });
                c.Equal(Fix.FromInt(4), Condition(game).Value, "posted service is not instant repair");
                var repairTwin = factory.Load(game.Save());
                Until(game, repairTwin, () => Condition(game).Value == Fix.FromInt(10), "automatic spare hauling and physical thirty-minute service");
                c.That(game.Clock.StepIndex >= repairAt + 30 && Wear(game).Exposure.ElapsedTicks == 18 && !Policy(game).Control.Enabled,
                    "physical service consumes time without resetting exposure or restarting the earlier policy pass");
                c.That(Stock(game, "source") + Stock(game, "contract.factory") + Stock(game, "machine") == Fix.FromInt(17),
                    "three actual spares consumed; two reusable tools conserved");
                Order(c, game, new ConfigureOperationCommand { Entity = id, Operation = Processor, Enabled = false, Target = Fix.One });
                c.That(!Policy(game).State.ShutdownLatched && !Policy(game).Control.Enabled, "explicit player disable acknowledges latch without restart");
                var held = Wear(game); var saved = factory.Load(game.Save()); for (int i = 0; i < 5; i++) Step(game, saved);
                c.That(Condition(game).Value == Fix.FromInt(10) && Wear(game).Exposure.ElapsedTicks == held.Exposure.ElapsedTicks,
                    "saved disabled operation preserves repair and cumulative wear");
                Order(c, game, new ConfigureOperationCommand { Entity = id, Operation = Processor, Enabled = true, Target = Fix.One });
                c.That(Wear(game).Exposure.ElapsedTicks == 19 && Condition(game).Value < Fix.FromInt(10), "reenabling earns wear from original cumulative exposure");
                c.That(retained.Exposure.ElapsedTicks == 0 && retained.Decision.Advance, "retained wear facts detached");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary player controls, timed wear and physical service replay");
            }
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled condition/wear save identity");
        }
        private static void Bindings(Check c)
        {
            foreach (bool nativeCondition in new[] { false, true }) foreach (bool nativeRule in new[] { false, true })
            {
                var factory = Create(new ConditionBindingModule(), edit: setup =>
                {
                    if (!nativeCondition) return;
                    var parts = setup.Get("entities")[1].Get("components"); var condition = parts.Get("independent_condition");
                    parts.Remove("independent_condition"); parts.Set("equipment_condition", condition);
                }, editEquipment: equipment =>
                {
                    equipment.Get("units")[0].Set("discrete", JsonValue.Of(true));
                    if (!nativeRule) return;
                    var rule = equipment.Get("independent_wear"); equipment.Remove("independent_wear"); equipment.Set("condition_wear_profiles", rule);
                });
                var game = ConstructionPlanTests.NewPoints(factory);
                Step(game); Step(game);
                c.That(Condition(game).Value == Fix.FromInt(10) && Wear(game).Exposure.ElapsedTicks == 2,
                    "native/independent bindings earn exposure without inventing subquantum damage");
                var twin = factory.Load(game.Save()); Step(game, twin);
                c.That(Condition(game).Value == Fix.FromInt(9) && Wear(game).Exposure.ElapsedTicks == 3,
                    "native/independent JSON combinations share original accumulated rate and saved native quantum");
            }
            var disabled = ConstructionPlanTests.NewPoints(Create(new ConditionBindingModule(), edit: setup =>
                setup.Get("entities")[1].Get("components").Get("independent_condition").Set("wear", JsonValue.Null())));
            for (int i = 0; i < 4; i++) Step(disabled);
            c.That(Condition(disabled).Value == Fix.FromInt(10) && Wear(disabled).Exposure.ElapsedTicks == 0 &&
                Wear(disabled).Rule == null && Wear(disabled).Decision.Blocker == ConditionWearCodes.None,
                "explicit null wear remains disabled, no implicit profile/default");
        }
        private static void AdmissionFaults(Check c)
        {
            var change = new ConditionChange(ConditionChangeKind.Damage, new UnitQuantity(new UnitId("unit.condition"), Fix.One));
            foreach (OperationBindingFault mode in Enum.GetValues(typeof(OperationBindingFault)))
            {
                var module = new PolicyBindingModule(); var game = ConstructionPlanTests.NewPoints(PolicyBindingTests.Create(module, probes: true));
                c.Equal(Code.None, module.ProbeConditionAdmission(change), "actual independent mutable condition admits before fault");
                module.Arm("condition.check", mode);
                c.Throws<InvalidOperationException>(() => module.ProbeConditionAdmission(change), "pure condition change admission " + mode);
                c.That(game.Fault?.Phase == "observation.provider" && module.Reads == 1, "actual admission provider/codec/clock fault");
                c.Throws<InvalidOperationException>(() => game.Save(), "admission fault cannot save");
                c.Throws<InvalidOperationException>(() => game.Hash(), "admission fault cannot hash");
                c.Equal(0, game.Update(), "admission fault cannot resume");
            }
        }
        private static void Faults(Check c)
        {
            foreach (string reader in new[] { "wear", "context", "condition", "ids" })
                foreach (string point in new[] { "condition.identity", "condition.unit", "condition.maximum", "condition.wear", "wear.unit", "wear.observe" })
                    foreach (OperationBindingFault mode in Enum.GetValues(typeof(OperationBindingFault)))
                    {
                        if (reader != "wear" && reader != "context" && point.StartsWith("wear.", StringComparison.Ordinal)) continue;
                        var module = new ConditionBindingModule(); var game = ConstructionPlanTests.NewPoints(Create(module)); var id = Factory(game);
                        ConditionWearContext context = default; ConditionWearSnapshot wear = default; ConditionSnapshot condition = default; var ids = new List<Code>();
                        Action read = () => { if (reader == "context") { wear = default; wear = module.ProbeDecision(out context); } else if (reader == "wear") View(game).ReadWear(id, Integrity, out wear);
                            else if (reader == "condition") View(game).Read(id, Integrity, out condition); else View(game).CopyIds(id, ids); };
                        read(); module.Arm(point, mode);
                        c.Throws<InvalidOperationException>(read, "pure actual condition/wear " + reader + "/" + point + "/" + mode);
                        c.That((reader == "wear" || reader == "context") ? wear.Rule == null && (reader != "context" || context.Equals(default(ConditionWearContext))) : reader == "condition" ? !condition.Unit.IsValid : ids.Count == 0, "no partial read facts");
                        c.That(game.Fault?.Phase == "observation.provider" && module.Reads == 1, "original actual codec guard latches first fault");
                        var fault = game.Fault; c.Equal(0, game.Update(), "faulted update stops");
                        c.Throws<InvalidOperationException>(() => game.Hash(), "no faulted hash");
                        c.Throws<InvalidOperationException>(() => game.Save(), "no faulted save");
                        c.Throws<InvalidOperationException>(read, "no faulted callback retry");
                        c.That(module.Reads == 1 && ReferenceEquals(fault, game.Fault), "first fault retained");
                    }
        }
        private static void Invalid(Check c)
        {
            foreach (string fault in new[] { "wear", "unit", "state", "initial", "maximum", "unknown", "metadata-unit", "metadata-maximum", "metadata-initial", "metadata-states", "unused" })
            {
                var initialize = new InitializeProbe();
                var factory = Create(new ConditionBindingModule(), initialize, setup =>
                {
                    var part = setup.Get("entities")[1].Get("components").Get("independent_condition");
                    if (fault == "wear") part.Set("wear", JsonValue.Of("absent"));
                    if (fault == "unit") part.Set("unit", JsonValue.Of("unit.supply"));
                    if (fault == "state") part.Get("states").Set("failed", JsonValue.Of("absent"));
                    if (fault == "initial") part.Set("initial", JsonValue.Number("11"));
                    if (fault == "maximum") part.Set("maximum", JsonValue.Number("0"));
                    if (fault == "unknown") part.Set("legacy", JsonValue.Of(true));
                    if (fault.StartsWith("metadata-", StringComparison.Ordinal)) part.Set("proof_fault", JsonValue.Of(fault.Substring(9)));
                    if (fault == "unused") { var unused = setup.Get("entities")[1].Clone(); unused.Set("id", JsonValue.Of("unused.condition")); unused.Get("components").Get("independent_condition").Set("proof_fault", JsonValue.Of("initial")); setup.Get("entities").Add(unused); }
                });
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(factory), "invalid actual condition " + fault);
                c.Equal(0, initialize.Initializations, "no partial initialization " + fault);
            }
            foreach (string fault in new[] { "unit", "quantity", "period", "source", "unknown" })
            {
                var initialize = new InitializeProbe(); var factory = Create(new ConditionBindingModule(), initialize, editEquipment: equipment =>
                {
                    var rule = equipment.Get("independent_wear")[0];
                    if (fault == "unit") rule.Set("unit", JsonValue.Of("unit.supply"));
                    if (fault == "quantity") rule.Set("quantity", JsonValue.Number("0"));
                    if (fault == "period") rule.Get("period").Set("denominator", JsonValue.Number("0"));
                    if (fault == "source") rule.Get("conditions")[0].Set("source", JsonValue.Of("absent"));
                    if (fault == "unknown") rule.Set("legacy", JsonValue.Of(true));
                });
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(factory), "invalid actual rule " + fault);
                c.Equal(0, initialize.Initializations, "no partial initialization " + fault);
            }
        }
    }
}
