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
    public static partial class RecurringPlanTests
    {
        private static readonly Code Inspection = new Code("inspection"), Maintenance = new Code("maintenance"), Integrity = new Code("integrity");
        public static void Register()
        {
            TestRunner.Add("Authored recurrence: plan contracts, JSON and private admission", c => { Contracts(c); AuthoredContracts(c); });
            TestRunner.Add("Authored recurrence: physical construction, repeat and saved continuation", c => { Physical(c); AuthoredPhysical(c); });
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static JsonValue Parts(JsonValue value) => value.Get("entities")[0].Get("components");
        private static JsonValue Sources(JsonValue value) => Parts(value).Get("recurring_activity");
        internal static GameFactory Factory(Action<JsonValue> edit = null, bool halfMinute = false)
        {
            var content = Json.Parse(FrameworkProofContent.Read("recurring-maintenance.json")); edit?.Invoke(content);
            var documents = new Dictionary<string, string> {
                { "equipment-condition.json", FrameworkProofContent.Read("equipment-condition.json") }, { "recurring-maintenance.json", Text(content) } };
            string definitions = null;
            if (halfMinute)
            {
                var root = Json.Parse(Colony.Defs); root.Get("scenarios")[0].Set("time", JsonValue.Of("cadence.half-minute")); definitions = Text(root);
                documents.Add("cadence-time.json", "{\"time_definitions\":[{\"id\":\"cadence.half-minute\",\"quantum\":{\"numerator\":30,\"denominator\":1,\"unit\":\"second\"},\"frame_ticks\":1,\"ticks_per_second\":1,\"horizon\":null,\"calendar\":null}]}");
            }
            return Colony.Factory(definitions: definitions, additionalDocuments: documents);
        }
        internal static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "colony", Seed = 994 }; request.Mods.Add(EquipmentConditionModule.ModuleId);
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        internal static Entity Spawn(SimGame game, int seat = 0)
        {
            var entity = ((TileMap)game.World.Space).SpawnAt(game.World, game.World.Content.Get<EntityDef>("recurring-machine"),
                new PlayerId(seat), new TileCoord(12 + seat * 8, 28)); game.World.Entities.Flush(); return entity;
        }
        private static void Advance(SimGame game, int count = 1, SimGame twin = null)
        {
            for (int i = 0; i < count; i++)
            {
                if (game.RunTicks(1) != 1) throw new InvalidOperationException("recurring tick=" + game.Tick + "; fault=" + game.Fault);
                if (twin != null && twin.RunTicks(1) != 1) throw new InvalidOperationException("recurring twin tick=" + twin.Tick + "; fault=" + twin.Fault);
            }
        }
        private static RecurringPlanView Read(SimGame game, EntityId id, Code source)
        {
            var refusal = game.Seat(0).View.Feature<WorkView>().ReadRecurringPlan(id, source, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("recurring view=" + refusal); return value;
        }
        private static List<PlanView> Plans(SimGame game)
        { var plans = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(plans); return plans; }
        private static PlanView Plan(SimGame game, long id) => Plans(game).Find(p => p.Id == id);
        private static Fix Condition(SimGame game, EntityId id)
        {
            if (!game.Seat(0).View.Feature<EquipmentConditionView>().Read(id, Integrity, out var value).IsNone) throw new InvalidOperationException("condition unavailable");
            return value.Value;
        }
        private static CommandResult Configure(SimGame game, EntityId id, Code source, bool enabled, int priority = 10)
            => TestKit.Order(game, 0, new ConfigureRecurringPlanCommand { Entity = id, Source = source,
                Settings = new RecurringPlanSettings(enabled, priority, RecurringPlanContracts.ReadSchedule(game.World,
                    game.World.Entities.Get(id).Get<IRecurringPlanSource>(source)).Interval, Code.None) });
        private static void Reach(SimGame game, EntityId id, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 700 && !done(); i++) Advance(game, 1, twin);
            if (!done()) throw new InvalidOperationException(stage + " timeout; tick=" + game.Tick + "; condition=" + Condition(game, id) +
                "; maintenance=" + Read(game, id, Maintenance).Blocker + "; inspection=" + Read(game, id, Inspection).Blocker +
                "; plans=" + string.Join(";", Plans(game).ConvertAll(p => p.Id + "/" + p.State + "/" + p.Blocker)));
        }
        private static void Invalid(Check c, Action<JsonValue> edit, string why) => c.Throws<ContentException>(() => New(Factory(edit)), why);
        private static void Contracts(Check c)
        {
            var factory = Factory(); var game = New(factory); var target = Spawn(game); var foreign = Spawn(game, 1); var view = game.Seat(0).View.Feature<WorkView>();
            var board = game.World.Services.Get<WorkBoard>(); var captured = Read(game, target.Id, Inspection); ulong hash = game.Hash();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "recurring rules are framework gameplay");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "source settings codec is current; no old reader");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, board.StateSchemaVersion, "same existing plan/job codec, no posting ledger");
            c.That(ReferenceEquals(board, game.World.Services.Get<IPlanBoard>()), "intent contract aliases sole work board");
            c.That(captured.Source == Inspection && captured.Schedule.LastPlan == 0 && captured.Schedule.LastPostedTick == -1 && captured.DueTick == 0 &&
                captured.Blocker == RecurringPlanCodes.Disabled && captured.Intent.Definition == "activity:equipment.inspect" && captured.Intent.Arguments.Frozen,
                "explicit authored source owns settings/virgin posting history and detached actual intent");
            c.Equal(hash, game.Hash(), "pure source diagnostics never seed a schedule or submit a plan");
            c.Throws<ArgumentException>(() => target.Get<IRecurringPlanSource>(), "multiple named sources cannot select an arbitrary provider");
            c.Equal(Codes.NotFound, view.ReadRecurringPlan(foreign.Id, Inspection, out var hidden), "foreign recurring history is private");
            c.That(hidden.Intent == null && hidden.Source.IsNone, "foreign source facts cleared");
            c.Equal(Codes.NotOwned, view.PreviewRecurringPlan(foreign.Id, Inspection, new RecurringPlanSettings(true, 10, captured.Schedule.Interval, Code.None)), "foreign control preview uses authority");
            c.Equal(Codes.InvalidArgument, view.ReadRecurringPlan(target.Id, Code.None, out _), "source must be explicitly named");
            c.Equal(RecurringPlanCodes.NotSource, view.ReadRecurringPlan(target.Id, new Code("absent"), out _), "no fallback source");
            c.Throws<ArgumentException>(() => new RecurringPlanSettings(true, -1, captured.Schedule.Interval, Code.None), "negative priority settings cannot be constructed");
            c.Equal(Codes.InvalidArgument, view.PreviewRecurringPlan(target.Id, Inspection, null), "missing settings preview refuses without a fabricated interval");
            c.That(!TestKit.Order(game, 0, new ConfigureRecurringPlanCommand { Entity = target.Id, Source = Inspection, Settings = null }).Accepted && Read(game, target.Id, Inspection).Schedule.LastPlan == 0,
                "matching command refusal preserves source settings/history");
            c.That(!Configure(game, foreign.Id, Inspection, true).Accepted, "foreign source control is refused");
            c.That(Configure(game, target.Id, Maintenance, true, 70).Accepted && Read(game, target.Id, Maintenance).Blocker == new Code("equipment.service-not-due"),
                "enabled maintenance posts only when actual condition trigger is satisfied");
            c.Equal(0, Plans(game).Count, "healthy threshold cannot fabricate service work");
            c.That(Configure(game, target.Id, Inspection, true).Accepted, "ordinary command enables inspection source");
            var active = Read(game, target.Id, Inspection); var plan = Plan(game, active.Schedule.LastPlan);
            c.That(active.Schedule.LastPlan > 0 && plan.State == PlanState.Active && plan.CreatedTick == active.Schedule.LastPostedTick && plan.Target == target.Id && plan.Priority == 10,
                "first eligible update creates exactly one actual native plan/order");
            c.That(active.Blocker == RecurringPlanCodes.Cadence && active.DueTick == active.Schedule.LastPostedTick + 120 && Plans(game).Count == 1,
                "minimum posting interval begins at accepted intent, never a failed observation");
            c.That(captured.Schedule.LastPlan == 0 && !captured.Schedule.Enabled, "captured source view never mutates with current state");
            var twin = factory.Load(game.Save()); Advance(game, 3, twin); c.Equal(game.Hash(), twin.Hash(), "saved source reference and native plan continuation stay isolated");
            Invalid(c, j => Sources(j)[0].Remove("enabled"), "enabled initial value is explicit");
            Invalid(c, j => Sources(j)[0].Remove("priority"), "priority initial value is explicit");
            Invalid(c, j => Sources(j)[0].Set("priority", JsonValue.Number("-1")), "authored priority cannot be negative");
            Invalid(c, j => Sources(j)[0].Remove("group"), "group:null or explicit identity required");
            Invalid(c, j => Sources(j)[0].Set("group", JsonValue.Of("")), "empty group refuses");
            Invalid(c, j => Sources(j)[0].Remove("conditions"), "unconditional policies explicitly author []");
            Invalid(c, j => Sources(j)[0].Set("plan", JsonValue.Of("missing")), "missing activity fails actual catalog resolution");
            Invalid(c, j => Sources(j)[0].Set("id", JsonValue.Of("")), "stable named source identity required");
            Invalid(c, j => Sources(j)[1].Set("id", JsonValue.Of("inspection")), "duplicate named source cannot own the same policy");
            Invalid(c, j => Sources(j)[0].Get("interval").Set("numerator", JsonValue.Number("0")), "zero cadence refuses");
            Invalid(c, j => Sources(j)[0].Get("interval").Set("denominator", JsonValue.Number("0")), "invalid rational cadence refuses");
            Invalid(c, j => Sources(j)[0].Set("interval", Json.Parse("{\"numerator\":1,\"denominator\":1,\"unit\":\"second\"}")), "cadence must align with chosen session quantum");
            Invalid(c, j => Sources(j)[1].Get("conditions")[0].Set("source", JsonValue.Of("missing")), "condition source must actually exist on owner");
            Invalid(c, j => Sources(j)[1].Get("conditions")[0].Set("unit", JsonValue.Of("unit.steel")), "threshold uses declared signal units");
            Invalid(c, j => j.Get("activities")[0].Set("player_orderable", JsonValue.Of(false)), "automatic intent cannot bypass ordinary orderability");
            Invalid(c, j => j.Get("activities")[0].Set("target_tags", Json.Parse("[\"missing-target-tag\"]")), "automatic intent cannot bypass activity target constraints");
            Checkpoint(c); Duplicates(c); PublicSource(c); Overflow(c);
            EditableContracts(c, false); EditableContracts(c, true); DishonestEdits(c);
        }
        private static byte[] Payload(bool enabled, int priority, long tick, long plan)
        {
            using (var stream = new MemoryStream())
            { var writer = new StateWriter(stream); bool present = true; writer.Bool(ref present); writer.Bool(ref enabled); writer.Int(ref priority);
                GameDuration.Sync(writer, GameDuration.Minutes(120)); string policy = null; writer.String(ref policy); writer.Long(ref tick); writer.Long(ref plan); return stream.ToArray(); }
        }
        private static byte[] Encode(RecurringActivitySource source)
        { using (var stream = new MemoryStream()) { source.Sync(new StateWriter(stream)); return stream.ToArray(); } }
        private static void Decode(RecurringActivitySource source, byte[] bytes) => source.Sync(new StateReader(bytes, 0, bytes.Length));
        private static void Checkpoint(Check c)
        {
            var factory = Factory(); var game = New(factory); var target = Spawn(game); var source = target.Get<RecurringActivitySource>(Inspection); ulong before = game.Hash();
            foreach (var pair in new[] { (-1L, 1L), (0L, 0L), (-2L, 0L), (0L, -1L) })
            {
                c.Throws<CorruptStateException>(() => Decode(source, Payload(false, 10, pair.Item1, pair.Item2)), "staged native schedule rejects inconsistent history");
                c.Equal(before, game.Hash(), "failed native decode preserves exact source settings/history");
            }
            c.Throws<CorruptStateException>(() => Decode(source, Payload(false, -1, -1, 0)), "staged native schedule rejects negative priority");
            c.Equal(before, game.Hash(), "invalid settings decode cannot replace the source owner");
            Configure(game, target.Id, Inspection, true); var live = Read(game, target.Id, Inspection); byte[] original = Encode(source);
            Decode(source, Payload(true, 10, game.Tick + 1, live.Schedule.LastPlan));
            c.Throws<CorruptStateException>(() => game.Hash(), "future source chronology refuses actual checkpoint");
            c.Throws<CorruptStateException>(() => game.World.System<WorkSystem>().OnLoaded(game.World), "future source chronology refuses post-restore validation");
            Decode(source, original); ulong correct = game.Hash();
            Decode(source, Payload(true, 10, live.Schedule.LastPostedTick, long.MaxValue));
            c.Throws<CorruptStateException>(() => game.Hash(), "invented last plan cannot replace actual plan reference");
            Decode(source, original); c.Equal(correct, game.Hash(), "instrumentation restores exact prior sole source payload");
        }
        private static void Duplicates(Check c)
        {
            var factory = Factory(j => { Sources(j)[0].Set("enabled", JsonValue.Of(true)); Sources(j)[0].Set("priority", JsonValue.Number("10"));
                var other = Sources(j)[0].Clone(); other.Set("id", JsonValue.Of("higher")); other.Set("priority", JsonValue.Number("90")); Sources(j).Add(other); });
            var game = New(factory); var target = Spawn(game); Advance(game);
            var winner = Read(game, target.Id, new Code("higher")); var loser = Read(game, target.Id, Inspection);
            c.That(Plans(game).Count == 1 && winner.Schedule.LastPlan > 0 && loser.Schedule.LastPlan == 0 && Plan(game, winner.Schedule.LastPlan).Priority == 90,
                "competing identical intents prefer explicit priority and admit only one native plan");
            Advance(game, 5); c.That(Plans(game).Count == 1 && Read(game, target.Id, Inspection).Schedule.LastPostedTick == -1,
                "duplicate/refused source never earns a false posting clock");
            for (int order = 0; order < 2; order++)
            {
                bool reverse = order == 1;
                var equal = New(Factory(j => {
                    var original = Sources(j)[0]; original.Set("enabled", JsonValue.Of(true)); var other = original.Clone(); other.Set("id", JsonValue.Of("alpha"));
                    var rows = JsonValue.NewArray(); if (reverse) { rows.Add(other); rows.Add(original); } else { rows.Add(original); rows.Add(other); }
                    Parts(j).Set("recurring_activity", rows);
                }));
                var equalTarget = Spawn(equal); Advance(equal);
                c.That(Read(equal, equalTarget.Id, new Code("alpha")).Schedule.LastPlan > 0 && Read(equal, equalTarget.Id, Inspection).Schedule.LastPlan == 0,
                    "equal-priority sources use stable capability identity, independent of authored array order=" + order);
            }
            var manual = New(Factory()); var manualTarget = Spawn(manual);
            var result = TestKit.Order(manual, 0, new SubmitPlanCommand { Intent = new PlanIntent("activity:equipment.inspect",
                Json.Parse("{\"target\":" + manualTarget.Id.Value + "}"), "manual", 30, true) });
            c.That(result.Accepted && Configure(manual, manualTarget.Id, Inspection, true).Accepted, "player pending plan and source use same board admission");
            c.That(Read(manual, manualTarget.Id, Inspection).Blocker == PlanCodes.Duplicate && Read(manual, manualTarget.Id, Inspection).Schedule.LastPlan == 0 && Plans(manual).Count == 1,
                "paused pending player intent prevents an automatic duplicate");
            c.Equal(manual.Hash(), Factory().Load(manual.Save()).Hash(), "pending manual/source relationship restores without a second model");
        }
        private static void PublicSource(Check c)
        {
            Action<JsonValue> proof = j => {
                Parts(j).Set("proof_recurring_activity", Json.Parse("{\"id\":\"custom\",\"plan\":\"activity:equipment.inspect\",\"template\":null,\"target\":\"self\",\"arguments\":null,\"reserve_use\":\"none\",\"worker_nodes\":[],\"cadence\":\"submission\",\"trigger\":\"schedule\",\"dispatch_mode\":\"compete\",\"facility_policy\":null,\"group\":null,\"enabled\":true,\"priority\":80,\"interval\":{\"numerator\":30,\"denominator\":1,\"unit\":\"minute\"},\"operation_policy\":null,\"fault\":0,\"idle_only\":false}"));
                Parts(j).Get("work_permit").Set("enabled", JsonValue.Of(false));
            };
            var factory = Factory(proof); var game = New(factory); var target = Spawn(game); Advance(game);
            c.That(Read(game, target.Id, new Code("custom")).Blocker == new Code("proof.recurring-permit") && Plans(game).Count == 0,
                "independent provider selects actual permit readiness without native condition/scheduler edits");
            c.That(TestKit.Order(game, 0, new SetWorkPermitCommand { Target = target.Id, Enabled = true }).Accepted, "normal command unlocks independent trigger");
            c.That(Read(game, target.Id, new Code("custom")).Schedule.LastPlan > 0 && Plans(game).Count == 1, "public provider posts same native physical plan");
            var twin = factory.Load(game.Save()); Advance(game, 4, twin); c.Equal(game.Hash(), twin.Hash(), "independent source codec resumes ordinary work deterministically");
            var pass = New(Factory(j => {
                proof(j); Parts(j).Get("work_permit").Set("enabled", JsonValue.Of(true));
                var first = Parts(j).Get("proof_recurring_activity"); first.Set("idle_only", JsonValue.Of(true));
                var second = first.Clone(); second.Set("id", JsonValue.Of("second")); second.Set("plan", JsonValue.Of("activity:equipment.service")); second.Set("priority", JsonValue.Number("79"));
                var rows = JsonValue.NewArray(); rows.Add(first); rows.Add(second); Parts(j).Set("proof_recurring_activity", rows);
            }));
            var passTarget = Spawn(pass); Advance(pass);
            c.That(Plans(pass).Count == 2 && Read(pass, passTarget.Id, new Code("custom")).Schedule.LastPlan > 0 &&
                Read(pass, passTarget.Id, new Code("second")).Schedule.LastPlan > 0, "complete source pass captures both idle triggers before either admission changes plan state");
            for (int mode = 1; mode <= 5; mode++)
            {
                int fault = mode; var bad = New(Factory(j => { proof(j); Parts(j).Get("work_permit").Set("enabled", JsonValue.Of(true));
                    Parts(j).Get("proof_recurring_activity").Set("fault", JsonValue.Number(fault.ToString())); }));
                Spawn(bad); bad.RunTicks(1); c.That(bad.Fault != null, "impure/invalid/dishonest source faults actual Update mode=" + mode);
            }
        }
        private static void Overflow(Check c)
        {
            var game = New(Factory(j => { Sources(j)[0].Set("enabled", JsonValue.Of(true)); Parts(j).Get("work_permit").Set("enabled", JsonValue.Of(false)); }));
            var target = Spawn(game); WideTimeTests.SetTick(game, long.MaxValue - 80); Advance(game);
            var value = Read(game, target.Id, Inspection);
            c.That(value.Schedule.LastPlan > 0 && value.Schedule.LastPostedTick > int.MaxValue && value.DueTick == null && value.Blocker == RecurringPlanCodes.TimeRange,
                "actual due arithmetic overflow is explicit, never an int cap or wrapped deadline");
        }
        private static void Physical(Check c)
        {
            var factory = Factory(); var game = New(factory); var target = Spawn(game); var foreign = Spawn(game, 1);
            WideTimeTests.SetTick(game, 1L << 40); var steel = game.World.Content.ResourceId("steel"); var store = target.Get<IInventory>();
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Target = target.Id, Plan = "equipment.damage-work" }).Accepted,
                "player causes real damage through timed machine activity, not a host condition write");
            Reach(game, target.Id, () => Condition(game, target.Id) == Fix.FromInt(4), "physical damage");
            c.That(Configure(game, target.Id, Maintenance, true, 70).Accepted, "player enrolls actual damaged machine in automatic maintenance");
            var admitted = Read(game, target.Id, Maintenance); var first = Plan(game, admitted.Schedule.LastPlan);
            c.That(first.State == PlanState.Active && Condition(game, target.Id) == Fix.FromInt(4) && store.Amount(steel) == Fix.FromInt(30),
                "automatic posting is only intent/admission; no immediate repair or resource spending");
            var board = game.World.Services.Get<WorkBoard>(); var order = board.Order(first.Order);
            Reach(game, target.Id, () => !order.Node("repair").Progress.Equals(WorkProgress.Zero), "service travel and startup");
            c.That(Condition(game, target.Id) == Fix.FromInt(4) && store.Amount(steel) == Fix.FromInt(27) && store.ReservedOut(steel) == Fix.FromInt(2),
                "physical service consumes three real spares, protects two tools and never repairs early");
            long servicePosted = Read(game, target.Id, Maintenance).Schedule.LastPostedTick;
            c.That(TestKit.Order(game, 0, new ConfigureRecurringPlanCommand { Entity = target.Id, Source = Maintenance,
                Settings = new RecurringPlanSettings(false, 71, GameDuration.Minutes(2), Code.None) }).Accepted && Plan(game, first.Id).State == PlanState.Active && Plan(game, first.Id).Priority == 70,
                "source disable/priority/cadence controls future postings only, never rewrites current physical service");
            c.Equal(servicePosted, Read(game, target.Id, Maintenance).Schedule.LastPostedTick, "service interval edit preserves actual posting timestamp");
            c.Equal(servicePosted + 2, Read(game, target.Id, Maintenance).DueTick, "edited service deadline derives from original submission, not repair progress or another timer");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "save during automatic service keeps source reference, native work and actual receipts");
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = first.Id, Group = "service.zone", Priority = 69, Paused = true }).Accepted &&
                TestKit.Order(twin, 0, new ConfigurePlanCommand { Plan = first.Id, Group = "service.zone", Priority = 69, Paused = true }).Accepted,
                "ordinary plan command pauses/reprioritizes admitted source work");
            var progress = order.Node("repair").Progress; Advance(game, 8, twin);
            c.That(order.Node("repair").Progress.Equals(progress) && Condition(game, target.Id) == Fix.FromInt(4), "paused plan freezes real machine progress without repair");
            c.Equal(game.Hash(), twin.Hash(), "source link remains valid when native plan group/priority/pause change");
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { first.Id }) }).Accepted && TestKit.Order(twin, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { first.Id }) }).Accepted,
                "ordinary plan cancellation interrupts automatic service");
            c.That(Plan(game, first.Id).State == PlanState.Cancelled && store.Amount(steel) == Fix.FromInt(27) && store.ReservedOut(steel) == Fix.Zero && Condition(game, target.Id) == Fix.FromInt(4),
                "cancellation keeps spent spares, releases tools and never fabricates repair");
            Advance(game, 7, twin); c.That(Read(game, target.Id, Maintenance).Schedule.LastPlan == first.Id && Plans(game).Count == 1, "disabled source never reposts cancelled work");
            c.That(Configure(game, target.Id, Maintenance, true, 71).Accepted && Configure(twin, target.Id, Maintenance, true, 71).Accepted,
                "player re-enables trigger after actual cadence");
            var second = Plan(game, Read(game, target.Id, Maintenance).Schedule.LastPlan);
            c.That(second.Id != first.Id && second.Priority == 71 && second.State == PlanState.Active, "retry creates a fresh native plan using current source priority");
            Reach(game, target.Id, () => Condition(game, target.Id) == Fix.FromInt(10), "replacement physical service completion", twin);
            c.That(store.Amount(steel) == Fix.FromInt(24) && store.ReservedOut(steel) == Fix.Zero && Plan(game, second.Id).State == PlanState.Done,
                "once-only completion pays fresh three spares and releases real held tools");
            c.That(board.Order(second.Order) == null && Read(game, target.Id, Maintenance).Schedule.LastPlan == second.Id,
                "source references existing terminal plan history after native order pruning, never copied progress");
            Advance(game, 8, twin); c.That(Read(game, target.Id, Maintenance).Blocker == new Code("equipment.service-not-due") && Plans(game).Count == 2,
                "healthy threshold suppresses repeated repair despite elapsed cadence");
            c.That(Configure(game, target.Id, Inspection, true).Accepted && Configure(twin, target.Id, Inspection, true).Accepted, "player enrolls periodic physical inspection");
            var inspection = Plan(game, Read(game, target.Id, Inspection).Schedule.LastPlan);
            c.That(inspection.State == PlanState.Active && store.Amount(steel) == Fix.FromInt(24), "inspection is a timed machine DAG, not an instant host check");
            twin = factory.Load(game.Save()); Reach(game, target.Id, () => Plan(game, inspection.Id).State == PlanState.Done, "physical inspection completion", twin);
            c.Equal(Fix.FromInt(23), store.Amount(steel), "two real service startups plus one inspection consume exactly seven native spares");
            c.Equal(game.Hash(), twin.Hash(), "automatic service and inspection restore deterministically at wide game ages");
            Reach(game, target.Id, () => Read(game, target.Id, Inspection).Schedule.LastPlan != inspection.Id, "inspection cadence repost", twin);
            var repeat = Read(game, target.Id, Inspection);
            c.That(repeat.Schedule.LastPostedTick == inspection.CreatedTick + 120 && repeat.Schedule.LastPlan > inspection.Id,
                "repeat posts on exact authored cadence, not a wall clock or completion-time guess");
            c.That(Configure(game, target.Id, Inspection, false).Accepted && Configure(twin, target.Id, Inspection, false).Accepted, "player disables future inspections without hiding the live plan");
            c.That(Plan(game, repeat.Schedule.LastPlan).State == PlanState.Active && Read(game, target.Id, Inspection).Blocker == RecurringPlanCodes.Disabled,
                "disabling respects current native execution authority");
            c.That(game.Observer.Feature<WorkView>().ReadRecurringPlan(foreign.Id, Maintenance, out var other).IsNone && other.Schedule.LastPlan == 0 &&
                foreign.Get<IInventory>().Amount(steel) == Fix.FromInt(30), "other company's sources/stock remain untouched");
            c.Equal(game.Hash(), twin.Hash(), "interleaved sessions retain independent source/work/stock owners");
            EditablePhysical(c, false); EditablePhysical(c, true); EditablePhysical(c, false, true);
        }

        private static readonly Code Custom = new Code("custom");
        private static GameFactory EditableFactory(bool extension, int fault = 0, bool halfMinute = false)
            => Factory(j => {
                if (extension) Parts(j).Set("proof_recurring_activity", Json.Parse("{\"id\":\"custom\",\"plan\":\"activity:equipment.inspect\",\"template\":null,\"target\":\"self\",\"arguments\":null,\"reserve_use\":\"none\",\"worker_nodes\":[],\"cadence\":\"submission\",\"trigger\":\"schedule\",\"dispatch_mode\":\"compete\",\"facility_policy\":null,\"group\":null,\"enabled\":false,\"priority\":40,\"interval\":{\"numerator\":30,\"denominator\":1,\"unit\":\"minute\"},\"operation_policy\":null,\"fault\":" + fault + ",\"idle_only\":false}"));
            }, halfMinute);
        private static ConfigureRecurringPlanCommand Edit(EntityId entity, Code source, RecurringPlanSettings settings)
            => new ConfigureRecurringPlanCommand { Entity = entity, Source = source, Settings = settings };
        private static byte[] EncodeSource(IRecurringPlanSource source)
        { using var stream = new MemoryStream(); ((Component)source).Sync(new StateWriter(stream)); return stream.ToArray(); }
        private static byte[] SettingsPayload(bool present, int priority, string literal, long scale, long divisor)
        {
            using var stream = new MemoryStream(); var writer = new StateWriter(stream); writer.Bool(ref present);
            if (present) { bool enabled = false; writer.Bool(ref enabled); writer.Int(ref priority); writer.String(ref literal); writer.Long(ref scale); writer.Long(ref divisor); string policy = null; writer.String(ref policy); }
            long posted = -1, plan = 0; writer.Long(ref posted); writer.Long(ref plan); return stream.ToArray();
        }
        private static void EditableContracts(Check c, bool extension)
        {
            var factory = EditableFactory(extension); var game = New(factory); var target = Spawn(game); var foreign = Spawn(game, 1);
            Code id = extension ? Custom : Inspection; var source = target.Get<IRecurringPlanSource>(id);
            var view = game.Seat(0).View.Feature<WorkView>(); var initial = Read(game, target.Id, id);
            c.That(ReferenceEquals(initial.Schedule.Settings, target.Def.Component<IRecurringPlanDefinition>(id).Initial), "explicit JSON supplies the sole immutable initial settings value");
            c.Equal(extension ? 30L : 120L, game.Clock.Time.ExactTicks(initial.Schedule.Interval), "native/public initial cadence comes from authored content");
            c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "existing work owner declares editable-source admission semantics");
            var settings = new RecurringPlanSettings(false, 17, GameDuration.Decimal("0.05", DurationUnit.Hour), Code.None);
            ulong before = game.Hash();
            c.Equal(Code.None, view.PreviewRecurringPlan(target.Id, id, settings), "private management preview accepts an aligned fractional game duration");
            c.Equal(Code.None, game.Seat(0).Preview(Edit(target.Id, id, settings)), "ordinary command preview shares the same settings validation");
            c.Equal(Codes.NotOwned, view.PreviewRecurringPlan(foreign.Id, id, settings), "foreign source settings cannot be managed");
            c.Equal(before, game.Hash(), "settings previews cannot change source history, plans, stock or game clock");
            c.That(TestKit.Order(game, 0, Edit(target.Id, id, settings)).Accepted, "normal command commits full native/public settings");
            var edited = Read(game, target.Id, id);
            c.Equal(3L, game.Clock.Time.ExactTicks(edited.Schedule.Interval), "current cadence is not the immutable definition interval");
            c.That(edited.Schedule.Priority == 17 && !edited.Schedule.Enabled && edited.Schedule.LastPlan == 0 && edited.Schedule.LastPostedTick == -1,
                "editing virgin source settings cannot fabricate posting history");
            c.That(initial.Schedule.Priority != edited.Schedule.Priority && game.Clock.Time.ExactTicks(initial.Schedule.Interval) != 3,
                "retained initial settings remain immutable after runtime edit");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "exact edited duration and source settings save/hash through one codec");
            before = game.Hash();
            foreach (GameDuration invalid in new[] { GameDuration.Seconds(1), GameDuration.Decimal("1.5", DurationUnit.Minute) })
            {
                var unaligned = new RecurringPlanSettings(true, 19, invalid, Code.None);
                c.Equal(RecurringPlanCodes.Interval, view.PreviewRecurringPlan(target.Id, id, unaligned), "unaligned cadence refuses against the actual chosen session quantum");
                c.Equal(RecurringPlanCodes.Interval, game.Seat(0).Preview(Edit(target.Id, id, unaligned)), "unaligned ordinary command has identical refusal");
            }
            c.Equal(RecurringPlanCodes.TimeRange, view.PreviewRecurringPlan(target.Id, id, new RecurringPlanSettings(true, 19, GameDuration.Hours(long.MaxValue), Code.None)), "duration conversion overflow is explicit, not a genre time ceiling");
            c.Equal(Codes.InvalidArgument, view.PreviewRecurringPlan(target.Id, id, null), "no missing-settings defaults");
            c.Throws<ArgumentException>(() => new RecurringPlanSettings(true, -1, GameDuration.Minutes(3), Code.None), "negative priority fails settings construction");
            c.Throws<ArgumentException>(() => new RecurringPlanSettings(true, 1, GameDuration.Zero, Code.None), "zero cadence fails settings construction");
            c.Equal(before, game.Hash(), "all refused observations retain exact authoritative state");
            foreach (var property in typeof(RecurringPlanSettings).GetProperties()) c.That(!property.CanWrite, "settings exposed to hosts have no mutation surface");
            byte[] bytes = EncodeSource(source);
            foreach (var payload in new[] { SettingsPayload(false, 1, null, 0, 0), SettingsPayload(true, -1, "3", 60, 1),
                SettingsPayload(true, 1, "0", 60, 1), SettingsPayload(true, 1, "3", 0, 1),
                SettingsPayload(true, 1, "-3", 60, 1), SettingsPayload(true, 1, "3", 60, 0) })
            {
                c.Throws<CorruptStateException>(() => ((Component)source).Sync(new StateReader(payload, 0, payload.Length)), "shared settings/duration codec rejects corrupt native/public source input");
                c.Equal(before, game.Hash(), "staged source decode cannot partially replace live settings/history");
            }
            c.Throws<CorruptStateException>(() => ((Component)source).Sync(new StateReader(bytes, 0, bytes.Length - 1)), "truncated history cannot commit decoded settings");
            c.Equal(before, game.Hash(), "truncated source decode retains original settings");
            byte[] unalignedPayload = SettingsPayload(true, 17, "1", 1, 1);
            ((Component)source).Sync(new StateReader(unalignedPayload, 0, unalignedPayload.Length));
            c.Throws<CorruptStateException>(() => game.Hash(), "session alignment is checked at whole-session checkpoint, not guessed by a component reader");
            c.Throws<CorruptStateException>(() => game.World.System<WorkSystem>().OnLoaded(game.World), "post-restore source validation rejects an unaligned saved cadence");
            ((Component)source).Sync(new StateReader(bytes, 0, bytes.Length)); c.Equal(before, game.Hash(), "fault instrumentation restores exact settings through the same codec");
            var history = Read(game, target.Id, id);
            game.Seat(0).Submit(Edit(target.Id, id, new RecurringPlanSettings(false, 23, new GameDuration(3, 2, DurationUnit.Hour), Code.None)));
            var twin = factory.Load(game.Save()); game.Clock.Pause(); long step = game.Clock.StepIndex; ulong paused = game.Hash();
            c.Equal(0, game.Update(), "sole-clock pause cannot process a pending recurring policy edit");
            c.Equal(step, game.Clock.StepIndex, "source settings contain no independently advancing time"); c.Equal(paused, game.Hash(), "paused queued duration/settings/history remain exact");
            game.Clock.Resume(); Advance(game, 1, twin);
            c.Equal(90L, game.Clock.Time.ExactTicks(Read(game, target.Id, id).Schedule.Interval), "pending rational period preserves exact unit/divisor on restore");
            c.That(Read(game, target.Id, id).Schedule.LastPlan == history.Schedule.LastPlan && Read(game, target.Id, id).Schedule.LastPostedTick == history.Schedule.LastPostedTick,
                "pending cadence edit never rebases successful-post history");
            c.Equal(game.Hash(), twin.Hash(), "pending native/public settings continue identically after checkpoint");

            if (!extension)
            {
                var wide = New(Factory(j => { Sources(j)[0].Set("enabled", JsonValue.Of(true)); Parts(j).Get("work_permit").Set("enabled", JsonValue.Of(false)); }));
                var wideTarget = Spawn(wide); WideTimeTests.SetTick(wide, long.MaxValue - 80); Advance(wide);
                var due = Read(wide, wideTarget.Id, Inspection); long posted = due.Schedule.LastPostedTick;
                c.Equal(RecurringPlanCodes.TimeRange, due.Blocker, "existing unrepresentable deadline remains a typed wait");
                c.Equal(RecurringPlanCodes.TimeRange, wide.Seat(0).View.Feature<WorkView>().PreviewRecurringPlan(wideTarget.Id, Inspection,
                    new RecurringPlanSettings(true, 10, GameDuration.Minutes(120), Code.None)), "edits cannot wrap original-history deadline");
                c.That(TestKit.Order(wide, 0, Edit(wideTarget.Id, Inspection, new RecurringPlanSettings(false, 20, GameDuration.Minutes(1), Code.None))).Accepted,
                    "shorter representable cadence can recover overflow without replacing source history");
                c.Equal(posted, Read(wide, wideTarget.Id, Inspection).Schedule.LastPostedTick, "overflow recovery preserves successful history");
                c.Equal(posted + 1, Read(wide, wideTarget.Id, Inspection).DueTick, "recovered deadline comes from sole clock's original coordinate");
                c.Equal(posted + 1, wide.Clock.Time.ExactTicks(Read(wide, wideTarget.Id, Inspection).DueGameTime.Value), "wide derived due game duration does not narrow through fixed point");
            }
        }
        private static void DishonestEdits(Check c)
        {
            foreach (var failure in new[] { (6, GameDuration.Minutes(3)), (6, GameDuration.Seconds(1800)), (7, GameDuration.Minutes(3)), (8, GameDuration.Minutes(3)) })
            {
                int fault = failure.Item1;
                var game = New(EditableFactory(true, fault)); var target = Spawn(game); long step = game.Clock.StepIndex;
                var command = Edit(target.Id, Custom, new RecurringPlanSettings(fault == 8, 91, failure.Item2, Code.None));
                c.Equal(Code.None, game.Seat(0).Preview(command), "valid intent previews before dishonest provider commit");
                game.Seat(0).Submit(command); c.Equal(0, game.Update(), "dishonest settings/history commit cannot complete a game update");
                c.That(game.Fault != null, "existing terminal session boundary owns failed recurring callback");
                c.Equal(step, game.Clock.StepIndex, "dishonest source cannot advance the sole current-time owner");
                c.Throws<InvalidOperationException>(() => game.Save(), "partial dishonest commit has no resumable checkpoint");
            }
        }
        private static void EditablePhysical(Check c, bool extension, bool halfMinute = false)
        {
            var factory = EditableFactory(extension, halfMinute: halfMinute); var game = New(factory); var target = Spawn(game); var isolated = New(factory); var untouched = Spawn(isolated);
            long updatesPerMinute = halfMinute ? 2 : 1;
            Code source = extension ? Custom : Inspection; var steel = game.World.Content.ResourceId("steel"); var store = target.Get<IInventory>();
            c.That(TestKit.Order(game, 0, Edit(target.Id, source, new RecurringPlanSettings(true, 40, GameDuration.Minutes(1000), Code.None))).Accepted, "player enrolls native/public source with edited long cadence");
            var started = Read(game, target.Id, source); long posted = started.Schedule.LastPostedTick; var first = Plan(game, started.Schedule.LastPlan);
            var board = game.World.Services.Get<WorkBoard>(); var order = board.Order(first.Order);
            c.That(first.State == PlanState.Active && store.Amount(steel) == Fix.FromInt(30), "automatic posting still cannot instantly inspect or spend spares");
            Reach(game, target.Id, () => order.Node("inspect").Progress.WholeTicks > 0 || order.Node("inspect").Progress.Fraction > 0, "actual inspection travel/startup before cadence edit");
            c.Equal(Fix.FromInt(29), store.Amount(steel), "real machine startup consumes exactly one native spare");
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = first.Id, Group = "edited.inspection", Priority = 40, Paused = true }).Accepted, "ordinary plan pause preserves actual productive work");
            var progress = order.Node("inspect").Progress;
            c.That(TestKit.Order(game, 0, Edit(target.Id, source, new RecurringPlanSettings(true, 91, GameDuration.Minutes(1), Code.None))).Accepted, "shortened overdue cadence updates future policy only");
            var shortPeriod = Read(game, target.Id, source);
            c.Equal(posted + updatesPerMinute, shortPeriod.DueTick, "short cadence uses original successful submission, not edit time");
            c.Equal(RecurringPlanCodes.Outstanding, shortPeriod.Blocker, "actual live source-plan link prevents overdue cadence from overlapping a paused plan");
            c.That(shortPeriod.Schedule.LastPlan == first.Id && shortPeriod.Schedule.LastPostedTick == posted && Plans(game).Count == 1, "duplicate attempts cannot earn history or new native plans");
            c.Equal(progress, order.Node("inspect").Progress, "source edits do not reset earned machine work");
            c.Equal(40, Plan(game, first.Id).Priority, "source priority changes cannot rewrite the admitted plan");
            c.That(TestKit.Order(game, 0, Edit(target.Id, source, new RecurringPlanSettings(true, 91, GameDuration.Minutes(300), Code.None))).Accepted, "lengthened cadence postpones future posting without rebasing history");
            c.Equal(RecurringPlanCodes.Cadence, Read(game, target.Id, source).Blocker, "current interval replaces content interval in readiness");
            c.Equal(posted + 300 * updatesPerMinute, Read(game, target.Id, source).DueTick, "longer period derives exact next deadline");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "edited paused physical source/work/resources save consistently");
            foreach (var current in new[] { game, twin }) c.That(TestKit.Order(current, 0, Edit(target.Id, source,
                new RecurringPlanSettings(false, 92, GameDuration.Decimal("0.05", DurationUnit.Hour), Code.None))).Accepted, "disable and fractional-period edit retain existing plan in both saved sessions");
            Advance(game, 4, twin);
            c.Equal(progress, order.Node("inspect").Progress, "disabled source cannot change paused native progress");
            c.Equal(Fix.FromInt(29), store.Amount(steel), "cadence/disable updates neither grant nor duplicate materials");
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { first.Id }) }).Accepted && TestKit.Order(twin, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { first.Id }) }).Accepted,
                "ordinary cancellation remains the sole actual execution control");
            c.Equal(PlanState.Cancelled, Plan(game, first.Id).State, "source history can reference retained cancelled native plan");
            foreach (var current in new[] { game, twin }) c.That(TestKit.Order(current, 0, Edit(target.Id, source,
                new RecurringPlanSettings(true, 93, GameDuration.Minutes(150), Code.None))).Accepted, "reenabling with longer interval waits on original history instead of posting immediately");
            c.Equal(first.Id, Read(game, target.Id, source).Schedule.LastPlan, "cancel/reenable did not replace posting age");
            c.Equal(RecurringPlanCodes.Cadence, Read(game, target.Id, source).Blocker, "reenabled source obeys edited interval");
            Reach(game, target.Id, () => Read(game, target.Id, source).Schedule.LastPlan != first.Id, "exact edited cadence repost", twin);
            var next = Read(game, target.Id, source); var second = Plan(game, next.Schedule.LastPlan);
            c.Equal(posted + 150 * updatesPerMinute, next.Schedule.LastPostedTick, "next real accepted plan occurs at original plus edited period");
            c.That(second.Id != first.Id && second.Priority == 93 && second.State == PlanState.Active, "new accepted work uses current priority without changing old plan");
            foreach (var current in new[] { game, twin }) c.That(Configure(current, target.Id, source, false, 94).Accepted, "disable future repetition while letting accepted physical inspection finish");
            Reach(game, target.Id, () => Plan(game, second.Id).State == PlanState.Done, "second real edited-period inspection", twin);
            c.Equal(Fix.FromInt(28), store.Amount(steel), "cancelled startup and later completed startup consume exactly two real spares");
            c.Equal(Fix.FromInt(10), Condition(game, target.Id), "inspection policy edits never fabricate repair or damage");
            c.Equal(game.Hash(), twin.Hash(), "native/public edited cadence keeps deterministic physical continuation");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal plan reference and actual edited settings remain durable");
            var isolatedSource = Read(isolated, untouched.Id, source);
            c.Equal((extension ? 30L : 120L) * updatesPerMinute, isolated.Clock.Time.ExactTicks(isolatedSource.Schedule.Interval), "shared authored JSON never shares mutable per-session cadence");
            c.That(isolatedSource.Schedule.LastPlan == 0 && isolatedSource.Schedule.LastPostedTick == -1 && !isolatedSource.Schedule.Enabled, "interleaved session retains virgin source state");
            c.Equal(Fix.FromInt(30), untouched.Get<IInventory>().Amount(steel), "isolated session never receives another session's costs/work");
        }
    }
}
