using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class OperationPolicyTests
    {
        private static readonly Code Safety = new Code("safety"), Integrity = new Code("integrity"), Process = new Code("process"), Maintenance = new Code("maintenance");
        public static void Register()
        {
            TestRunner.Add("Operation policies: authored thresholds, controls and checkpoint ownership", Contracts);
            TestRunner.Add("Operation policies: shutdown, physical maintenance and saved restart", Physical);
        }
        private static JsonValue Parts(JsonValue value) => value.Get("entities")[0].Get("components");
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static GameFactory Factory(Action<JsonValue> edit = null)
        {
            var value = Json.Parse(FrameworkProofContent.Read("operation-policies.json")); edit?.Invoke(value);
            return Colony.Factory(additionalDocuments: new Dictionary<string, string> {
                { "equipment-condition.json", FrameworkProofContent.Read("equipment-condition.json") }, { "operation-policies.json", Text(value) } });
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "colony", Seed = 995 }; request.Mods.Add(EquipmentConditionModule.ModuleId);
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Spawn(SimGame game, int seat = 0)
        {
            var entity = ((TileMap)game.World.Space).SpawnAt(game.World, game.World.Content.Get<EntityDef>("policy-machine"), new PlayerId(seat), new TileCoord(12 + seat * 8, 28));
            game.World.Entities.Flush(); return entity;
        }
        private static OperationPolicyView Read(SimGame game, EntityId id)
        {
            Code refusal = game.Seat(0).View.Feature<EquipmentConditionView>().ReadPolicy(id, Safety, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("policy view=" + refusal); return value;
        }
        private static OperationPolicySettings Settings(int criticality = 20, int maintenance = 4, int shutdown = 4, int restart = 8, OperationRestartPolicy policy = OperationRestartPolicy.Automatic)
            => new OperationPolicySettings(criticality, Fix.FromInt(maintenance), Fix.FromInt(shutdown), Fix.FromInt(restart), policy);
        private static CommandResult Configure(SimGame game, EntityId id, OperationPolicySettings settings)
            => TestKit.Order(game, 0, new ConfigureOperationPolicyCommand { Entity = id, Policy = Safety, Settings = settings });
        private static CommandResult Control(SimGame game, EntityId id, bool enabled, int target = 1)
            => TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = id, Operation = Process, Enabled = enabled, Target = Fix.FromInt(target) });
        private static void Advance(SimGame game, int count = 1, SimGame twin = null)
        {
            for (int i = 0; i < count; i++)
            {
                if (game.RunTicks(1) != 1) throw new InvalidOperationException("policy tick=" + game.Tick + "; fault=" + game.Fault);
                if (twin != null && twin.RunTicks(1) != 1) throw new InvalidOperationException("policy twin tick=" + twin.Tick + "; fault=" + twin.Fault);
            }
        }
        private static void Reach(SimGame game, EntityId id, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 700 && !done(); i++) Advance(game, 1, twin);
            if (!done()) { var value = Read(game, id); throw new InvalidOperationException(stage + " timeout tick=" + game.Tick + "; condition=" + value.Equipment.Value + "; enabled=" + value.Control.Enabled + "; latch=" + value.State.ShutdownLatched + "; blocker=" + value.Blocker); }
        }
        private static void Change(SimGame game, Entity entity, int amount, ConditionChangeKind kind = ConditionChangeKind.Damage)
        {
            Code refusal = ConditionContracts.Commit(game.World, entity.Get<IConditionProvider>(Integrity),
                new ConditionChange(kind, new UnitQuantity(new UnitId("unit.condition"), Fix.FromInt(amount))));
            if (!refusal.IsNone) throw new InvalidOperationException("test condition change=" + refusal);
        }
        private static void Invalid(Check c, Action<JsonValue> edit, string reason) => c.Throws<ContentException>(() => New(Factory(edit)), reason);
        private static void Contracts(Check c)
        {
            var factory = Factory(); var game = New(factory); var target = Spawn(game); var foreign = Spawn(game, 1);
            var view = game.Seat(0).View.Feature<EquipmentConditionView>(); var captured = Read(game, target.Id); var policy = target.Get<IOperationPolicy>(Safety);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current engine policy identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "new policy codec does not duplicate existing owner payloads");
            c.That(policy is OperationPolicy && target.Def.Component<IOperationPolicyDefinition>(Safety) is OperationPolicyDef, "policy metadata/runtime use named public contracts");
            c.That(captured.Control.Enabled && !captured.State.ShutdownLatched && captured.State.ChangedTick == -1 && !captured.MaintenanceDue && captured.RestartReady,
                "initial policy state derives explicit thresholds and actual operation/condition");
            c.That(captured.State.Settings.Criticality == 20 && captured.Operation == Process && captured.Condition == Integrity, "JSON owns exact identities and criticality");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, game.World.Services.Get<WorkBoard>().StateSchemaVersion, "no policy job/progress ledger");
            c.That(ReferenceEquals(target.Def.Component<IOperationPolicyDefinition>(Safety).Unit, game.World.Content.Get<UnitDef>("unit.condition")), "policy unit aliases actual catalog");
            var ids = new List<Code>(); c.That(view.CopyPolicyIds(target.Id, ids) && ids.Count == 1 && ids[0] == Safety, "IDs enumerate actual named policies");
            c.That(!view.CopyPolicyIds(foreign.Id, ids) && ids.Count == 0, "private enumeration clears output");
            c.Equal(Codes.NotFound, view.ReadPolicy(foreign.Id, Safety, out var withheld), "foreign policy facts withheld");
            c.That(withheld.State.Settings == null && withheld.Policy.IsNone, "foreign metadata/control/condition do not leak");
            c.Equal(Code.None, game.Observer.Feature<EquipmentConditionView>().ReadPolicy(foreign.Id, Safety, out _), "explicit observer diagnoses other company");
            c.Equal(Codes.NotFound, view.PreviewPolicy(foreign.Id, Safety, Settings()), "foreign policy command preview stays private");
            c.Equal(OperationPolicyCodes.NotPolicy, view.ReadPolicy(target.Id, new Code("absent"), out _), "missing policy never substitutes another part");
            c.Equal(Codes.InvalidArgument, view.ReadPolicy(target.Id, Code.None, out _), "explicit policy identity required");
            c.Throws<ArgumentException>(() => OperationPolicyContracts.Read(New(factory).World, policy), "cross-session provider refuses");
            ulong before = game.Hash();
            foreach (var invalid in new[] { Settings(-1), Settings(20, -1), Settings(20, 3, 4), Settings(20, 4, -1), Settings(20, 4, 4, 4), Settings(20, 4, 4, 11), Settings(policy: (OperationRestartPolicy)7) })
                c.Equal(Codes.InvalidArgument, view.PreviewPolicy(target.Id, Safety, invalid), "invalid bands/criticality/enum refuse without mutation");
            c.Equal(Codes.InvalidArgument, view.PreviewPolicy(target.Id, Safety, null), "no fabricated settings for missing request");
            c.Equal(before, game.Hash(), "observations/previews allocate no work or state");
            c.That(Configure(game, target.Id, Settings(30)).Accepted && Read(game, target.Id).State.Settings.Criticality == 30, "ordinary command changes actual policy settings");
            c.Equal(20, captured.State.Settings.Criticality, "previous settings remain immutable/detached");
            c.That(!Configure(game, target.Id, Settings(-1)).Accepted && Read(game, target.Id).State.Settings.Criticality == 30, "command and preview share refusal semantics");
            c.That(!Configure(game, foreign.Id, Settings()).Accepted, "foreign command cannot change policy");
            c.Equal(100, RecurringPlanContracts.Read(game.World, target.Get<IRecurringPlanSource>(Maintenance)).Intent.Priority, "linked effective priority derives current base plus criticality");
            c.Equal(OperationPolicyCodes.PriorityRange, view.PreviewPolicy(target.Id, Safety, Settings(int.MaxValue)), "criticality overflow refuses explicitly, not arbitrary cap");
            c.Equal(OperationPolicyCodes.PriorityRange, game.Seat(0).View.Feature<WorkView>().PreviewRecurringPlan(target.Id, Maintenance,
                new RecurringPlanSettings(true, int.MaxValue, RecurringPlanContracts.ReadSchedule(game.World, target.Get<IRecurringPlanSource>(Maintenance)).Interval, Code.None)), "linked source configuration checks same actual representation");
            c.That(Configure(game, target.Id, Settings(int.MaxValue - 70)).Accepted &&
                RecurringPlanContracts.Read(game.World, target.Get<IRecurringPlanSource>(Maintenance)).Intent.Priority == int.MaxValue,
                "full representable effective priority is allowed without a genre ceiling");
            c.That(Configure(game, target.Id, Settings(30)).Accepted, "policy returns to explicit player settings");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "configured policy survives normal save/hash");
            game.Seat(0).Submit(new ConfigureOperationPolicyCommand { Entity = target.Id, Policy = Safety, Settings = Settings(35) });
            var pending = factory.Load(game.Save()); Advance(game, 1, pending);
            c.That(Read(game, target.Id).State.Settings.Criticality == 35 && game.Hash() == pending.Hash(), "saved pending command uses the same settings serializer and ordinary replay admission");
            foreach (string field in new[] { "id", "operation", "condition", "unit", "criticality", "maintenance_threshold", "shutdown_threshold", "restart_threshold", "restart_policy" })
                Invalid(c, j => Parts(j).Get("operation_policy").Remove(field), "required authored policy field " + field);
            Invalid(c, j => Parts(j).Get("operation_policy").Set("operation", JsonValue.Of("absent")), "missing operation refuses before tick one");
            Invalid(c, j => Parts(j).Get("operation_policy").Set("condition", JsonValue.Of("absent")), "missing condition refuses before tick one");
            Invalid(c, j => Parts(j).Get("operation_policy").Set("unit", JsonValue.Of("unit.steel")), "policy cannot reinterpret condition unit");
            Invalid(c, j => Parts(j).Get("operation_policy").Set("restart_policy", JsonValue.Of("default")), "no inferred restart behavior");
            Invalid(c, j => Parts(j).Get("operation_policy").Set("criticality", JsonValue.Number("-1")), "negative criticality refuses");
            Invalid(c, j => Parts(j).Get("operation_policy").Set("restart_threshold", JsonValue.Number("11")), "restart above actual maximum refuses");
            Invalid(c, j => Parts(j).Get("operation_policy").Set("maintenance_threshold", JsonValue.Number("3")), "shutdown cannot exceed maintenance band");
            Invalid(c, j => Parts(j).Get("operation_policy").Set("restart_threshold", JsonValue.Number("4")), "restart hysteresis must be positive");
            Invalid(c, j => Parts(j).Get("recurring_activity").Remove("operation_policy"), "recurring link explicit even when null");
            Invalid(c, j => Parts(j).Get("recurring_activity").Set("operation_policy", JsonValue.Of("absent")), "missing linked policy refuses");
            Invalid(c, j => Parts(j).Get("recurring_activity").Set("priority", JsonValue.Number(int.MaxValue.ToString())), "initial effective priority overflow refuses");
            Invalid(c, j => { var first = Parts(j).Get("operation_policy"); var second = first.Clone(); second.Set("id", JsonValue.Of("other")); var rows = JsonValue.NewArray(); rows.Add(first); rows.Add(second); Parts(j).Set("operation_policy", rows); }, "two policies cannot own one operation");
            Checkpoint(c); Independent(c); Overrides(c); Hysteresis(c);
        }
        private static byte[] Payload(OperationPolicySettings settings, bool latch, long tick)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); int priority = settings.Criticality; Fix maintenance = settings.MaintenanceThreshold, shutdown = settings.ShutdownThreshold, restart = settings.RestartThreshold; byte policy = (byte)settings.Restart;
                writer.Int(ref priority); writer.Fix(ref maintenance); writer.Fix(ref shutdown); writer.Fix(ref restart); writer.Byte(ref policy); writer.Bool(ref latch); writer.Long(ref tick); return stream.ToArray();
            }
        }
        private static void Decode(OperationPolicy policy, byte[] data) { policy.Sync(new StateReader(data, 0, data.Length)); }
        private static void Checkpoint(Check c)
        {
            var factory = Factory(); var game = New(factory); var target = Spawn(game); var policy = (OperationPolicy)target.Get<IOperationPolicy>(Safety);
            foreach (var bad in new[] { Payload(Settings(-1), false, -1), Payload(Settings(), true, -1), Payload(Settings(), false, -2), Payload(Settings(policy: (OperationRestartPolicy)9), false, -1), Payload(Settings(restart: 11), false, -1) })
            {
                c.Throws<CorruptStateException>(() => Decode(policy, bad), "native policy decode rejects whole malformed state");
                c.That(Read(game, target.Id).State.Settings.Criticality == 20 && Read(game, target.Id).State.ChangedTick == -1, "failed native decode retains previous settings/latch");
            }
            Decode(policy, Payload(Settings(), false, game.Tick + 1));
            c.Throws<CorruptStateException>(() => game.Hash(), "future chronology cannot checkpoint");
            Decode(policy, Payload(Settings(), true, game.Tick));
            c.Throws<CorruptStateException>(() => game.Hash(), "latched enabled-operation contradiction refuses checkpoint");
            Decode(policy, Payload(Settings(), false, -1)); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "restored valid owner still checkpoints normally");
        }
        private static void Independent(Check c)
        {
            Action<JsonValue> proof = j => { var part = Parts(j).Get("operation_policy"); part.Set("fault", JsonValue.Number("0")); Parts(j).Remove("operation_policy"); Parts(j).Set("proof_operation_policy", part); };
            var factory = Factory(proof); var game = New(factory); var target = Spawn(game);
            c.That(target.Get<IOperationPolicy>(Safety) is Beep.SimEngine.ExtensionProof.ProofOperationPolicy, "public-only independent policy owner uses same engine rules");
            Change(game, target, 6); Advance(game);
            c.That(Read(game, target.Id).State.ShutdownLatched && !Read(game, target.Id).Control.Enabled, "same coordinator stops independent policy's actual operation");
            Change(game, target, 6, ConditionChangeKind.Repair); Advance(game);
            c.That(Read(game, target.Id).Control.Enabled && !Read(game, target.Id).State.ShutdownLatched, "same generic protocol restarts independent owner");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "independent provider codec saves through normal composition");
            for (int fault = 1; fault <= 5; fault++)
            {
                var bad = New(Factory(j => { proof(j); Parts(j).Get("proof_operation_policy").Set("fault", JsonValue.Number(fault.ToString())); })); var entity = Spawn(bad);
                if (fault == 2 || fault == 5) bad.Seat(0).Submit(new ConfigureOperationPolicyCommand { Entity = entity.Id, Policy = Safety, Settings = Settings(30) });
                else if (fault > 2) Change(bad, entity, 6);
                bad.RunTicks(1); c.That(bad.Fault != null, "dishonest policy faults normal Update mode=" + fault);
                c.Throws<InvalidOperationException>(() => bad.Save(), "faulted policy cannot checkpoint mode=" + fault);
            }
            var dialFactory = Factory(j => { var operation = Parts(j).Get("transformer").Get("operation").Clone(); Parts(j).Remove("transformer"); var dial = JsonValue.NewObject(); dial.Set("operation", operation); Parts(j).Set("dial_operation", dial); });
            var dialGame = New(dialFactory); var dialEntity = Spawn(dialGame); Change(dialGame, dialEntity, 6); Advance(dialGame);
            c.That(!Read(dialGame, dialEntity.Id).Control.Enabled && Read(dialGame, dialEntity.Id).State.ShutdownLatched, "policy binds unrelated public operation, not a transformer lookup");
            Change(dialGame, dialEntity, 6, ConditionChangeKind.Repair); Advance(dialGame);
            c.That(Read(dialGame, dialEntity.Id).Control.Enabled, "unrelated operation restarts without scheduler changes");
            Action<JsonValue> alternateUnit = j => {
                Parts(j).Remove("equipment_condition"); Parts(j).Set("proof_condition", Json.Parse("{\"id\":\"integrity\",\"unit\":\"unit.steel\",\"maximum\":10,\"initial\":10,\"fault\":0}"));
                Parts(j).Get("operation_policy").Set("unit", JsonValue.Of("unit.steel"));
            };
            var unitFactory = Factory(alternateUnit); var unitGame = New(unitFactory); var unitEntity = Spawn(unitGame);
            c.That(Read(unitGame, unitEntity.Id).Equipment.Unit == new UnitId("unit.steel"), "contract supports a game's explicit alternative condition unit");
            var unitDamage = new ConditionChange(ConditionChangeKind.Damage, new UnitQuantity(new UnitId("unit.steel"), Fix.FromInt(6)));
            c.Equal(Code.None, ConditionContracts.Commit(unitGame.World, unitEntity.Get<IConditionProvider>(Integrity), unitDamage), "actual alternate unit damage uses shared bounded condition protocol");
            Advance(unitGame); c.That(Read(unitGame, unitEntity.Id).State.ShutdownLatched, "alternate condition provider/unit uses unchanged policy coordinator");
            c.Equal(unitGame.Hash(), unitFactory.Load(unitGame.Save()).Hash(), "alternate typed policy facts restore normally");
            Invalid(c, j => { alternateUnit(j); Parts(j).Get("operation_policy").Set("maintenance_threshold", JsonValue.Number("4.5")); }, "threshold violating actual discrete unit quantum refuses");
        }
        private static void Overrides(Check c)
        {
            var factory = Factory(); var game = New(factory); var target = Spawn(game); Change(game, target, 3);
            c.That(Control(game, target.Id, true, 2).Accepted, "enabled safe operation can change target below restart threshold");
            Change(game, target, 3); Advance(game); var stopped = Read(game, target.Id);
            c.That(stopped.State.ShutdownLatched && !stopped.Control.Enabled && stopped.Control.Target == Fix.FromInt(2), "shutdown preserves actual configured rate");
            var operations = game.Seat(0).View.Feature<OperationsView>();
            c.Equal(OperationPolicyCodes.Unsafe, operations.PreviewConfigure(target.Id, Process, true, Fix.One), "unsafe restart preview refuses");
            c.That(!Control(game, target.Id, true).Accepted && Read(game, target.Id).State.ShutdownLatched, "ordinary command cannot bypass unsafe threshold");
            c.That(Control(game, target.Id, false, 2).Accepted && !Read(game, target.Id).State.ShutdownLatched, "explicit disable releases automatic restart ownership");
            Change(game, target, 6, ConditionChangeKind.Repair); Advance(game, 4);
            c.That(!Read(game, target.Id).Control.Enabled, "healthy manually-disabled operation never auto-restarts");
            c.That(Control(game, target.Id, true, 2).Accepted, "healthy player restart admitted normally");
            c.That(Configure(game, target.Id, Settings(policy: OperationRestartPolicy.Manual)).Accepted, "manual restart selected by normal policy command");
            Change(game, target, 6); Advance(game); Change(game, target, 6, ConditionChangeKind.Repair); Advance(game, 4);
            c.That(Read(game, target.Id).RestartReady && Read(game, target.Id).State.ShutdownLatched && !Read(game, target.Id).Control.Enabled, "manual policy retains shutdown until explicit command");
            c.That(Control(game, target.Id, true, 2).Accepted && !Read(game, target.Id).State.ShutdownLatched, "safe manual restart acknowledges latch");
            c.That(Configure(game, target.Id, Settings(maintenance: 4, shutdown: 0, restart: 8)).Accepted, "thresholds are editable, not fixed genre constants");
            Change(game, target, 6); Advance(game);
            c.That(Read(game, target.Id).MaintenanceDue && Read(game, target.Id).Control.Enabled, "maintenance threshold alone does not shut down operation");
            Change(game, target, 4); Advance(game);
            c.That(Read(game, target.Id).State.ShutdownLatched && !Read(game, target.Id).Control.Enabled, "explicit zero shutdown threshold still stops failed equipment");
        }
        private static void Hysteresis(Check c)
        {
            var factory = Factory(); var game = New(factory); var target = Spawn(game); Change(game, target, 6); Advance(game);
            Change(game, target, 3, ConditionChangeKind.Repair); Advance(game, 3);
            c.That(Read(game, target.Id).State.ShutdownLatched && !Read(game, target.Id).Control.Enabled && !Read(game, target.Id).RestartReady,
                "recovery above shutdown but below restart retains the safety stop");
            c.Equal(OperationPolicyCodes.Unsafe, game.Seat(0).View.Feature<OperationsView>().PreviewConfigure(target.Id, Process, true, Fix.One), "partial recovery does not admit manual/automation enabling");
            Change(game, target, 1, ConditionChangeKind.Repair); Advance(game);
            c.That(Read(game, target.Id).Equipment.Value == Fix.FromInt(8) && Read(game, target.Id).Control.Enabled && !Read(game, target.Id).State.ShutdownLatched,
                "exact restart boundary resumes without a free full repair");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "hysteresis transition saves exact bounded condition and controls");
        }
        private static TransformationSnapshot ProcessState(SimGame game, EntityId id)
        { if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(id, Process, out var value)) throw new InvalidOperationException("process missing"); return value; }
        private static PlanView Plan(SimGame game, long id)
        { var plans = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(plans); return plans.Find(p => p.Id == id); }
        private static long LastPlan(SimGame game, EntityId id)
        { game.Seat(0).View.Feature<WorkView>().ReadRecurringPlan(id, Maintenance, out var value); return value.Schedule.LastPlan; }
        private static void Physical(Check c)
        {
            var factory = Factory(); var game = New(factory); var target = Spawn(game); var foreign = Spawn(game, 1);
            WideTimeTests.SetTick(game, 1L << 40); var steel = game.World.Content.ResourceId("steel"); var inventory = target.Get<IInventory>();
            Entity crane = game.World.Entities.Get(Colony.Mine(game, 0, "crane")[0].Id); var origin = crane.Get<ITilePresence>().Position;
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = target.Id, Capability = Process, Transformation = "equipment.batch", Count = 1 }).Accepted, "normal command starts actual production WIP");
            c.That(TestKit.Order(game, 0, new ConfigureRecurringPlanCommand { Entity = target.Id, Source = Maintenance,
                Settings = new RecurringPlanSettings(true, 70, RecurringPlanContracts.ReadSchedule(game.World, target.Get<IRecurringPlanSource>(Maintenance)).Interval, Code.None) }).Accepted && LastPlan(game, target.Id) == 0,
                "healthy machine enrollment does not post unnecessary service");
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Target = target.Id, Plan = "equipment.damage-work" }).Accepted, "ordinary physical stress activity admitted");
            c.Equal(Fix.FromInt(10), Read(game, target.Id).Equipment.Value, "posting is intent, never instant damage");
            Reach(game, target.Id, () => Read(game, target.Id).State.ShutdownLatched && LastPlan(game, target.Id) != 0, "physical damage and policy stop");
            c.That(crane.Get<ITilePresence>().Position != origin && Read(game, target.Id).Equipment.Value == Fix.FromInt(4), "worker travels and performs real timed damage");
            c.That(!Read(game, target.Id).Control.Enabled && Read(game, target.Id).State.ChangedTick >= (1L << 40), "safety controls and timestamps stay engine-owned/full-width");
            long planId = LastPlan(game, target.Id); var plan = Plan(game, planId); var order = game.World.Services.Get<WorkBoard>().Order(plan.Order);
            c.That(plan.State == PlanState.Active && plan.Priority == 90 && plan.Target == target.Id, "criticality affects authoritative native maintenance priority");
            var pausedProcess = ProcessState(game, target.Id); var twin = factory.Load(game.Save()); Advance(game, 3, twin);
            c.Equal(pausedProcess.Progress, ProcessState(game, target.Id).Progress, "shutdown earns no production work and preserves WIP");
            c.Equal(game.Hash(), twin.Hash(), "stopped operation, source and native work restore as one session");
            Reach(game, target.Id, () => !order.Node("repair").Progress.Equals(WorkProgress.Zero), "physical maintenance startup", twin);
            c.That(Read(game, target.Id).Equipment.Value == Fix.FromInt(4) && inventory.Amount(steel) == Fix.FromInt(25) && inventory.ReservedOut(steel) == Fix.FromInt(2),
                "actual startup consumes three spares after two WIP inputs, holds tools and cannot repair early");
            c.That(Configure(game, target.Id, Settings(30)).Accepted && Configure(twin, target.Id, Settings(30)).Accepted && Plan(game, planId).Priority == 90,
                "policy edits do not rewrite already-admitted plan priority");
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = planId, Group = "service", Priority = 89, Paused = true }).Accepted &&
                TestKit.Order(twin, 0, new ConfigurePlanCommand { Plan = planId, Group = "service", Priority = 89, Paused = true }).Accepted, "ordinary player plan control pauses repair");
            var progress = order.Node("repair").Progress; Advance(game, 7, twin);
            c.That(order.Node("repair").Progress.Equals(progress) && Read(game, target.Id).State.ShutdownLatched && Read(game, target.Id).Equipment.Value == Fix.FromInt(4), "paused repair never earns free recovery or restart");
            c.Equal(game.Hash(), twin.Hash(), "saved settings edits and paused physical work remain deterministic");
            twin = factory.Load(game.Save());
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = planId, Group = "service", Priority = 89, Paused = false }).Accepted &&
                TestKit.Order(twin, 0, new ConfigurePlanCommand { Plan = planId, Group = "service", Priority = 89, Paused = false }).Accepted, "same native plan resumes earned progress");
            Reach(game, target.Id, () => Read(game, target.Id).Equipment.Value == Fix.FromInt(10), "physical repair completion", twin);
            c.That(Read(game, target.Id).State.ShutdownLatched && !Read(game, target.Id).Control.Enabled, "physical repair at work400 cannot retroactively restart process350");
            Advance(game, 2, twin);
            c.That(Read(game, target.Id).Control.Enabled && !Read(game, target.Id).State.ShutdownLatched && Plan(game, planId).State == PlanState.Done, "next policy observation restarts only recovered safety-owned operation");
            c.That(ProcessState(game, target.Id).Progress.WholeTicks > pausedProcess.Progress.WholeTicks && inventory.Amount(steel) == Fix.FromInt(25) && inventory.ReservedOut(steel) == Fix.Zero,
                "real production resumes preserved work; spares spent once, held tools released normally");
            c.That(LastPlan(game, target.Id) == planId && !Read(game, target.Id).MaintenanceDue, "healthy policy prevents duplicate maintenance posting");
            c.Equal(game.Hash(), twin.Hash(), "wide saved physical continuation remains identical");
            c.That(game.Observer.Feature<EquipmentConditionView>().ReadPolicy(foreign.Id, Safety, out var other).IsNone && other.Control.Enabled && !other.State.ShutdownLatched &&
                foreign.Get<IInventory>().Amount(steel) == Fix.FromInt(30), "foreign company controls and stock untouched");
        }
    }
}
