using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
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
    public static class ConditionWearTests
    {
        private static readonly Code Integrity = new Code("integrity"), Sensor = new Code("sensor");
        public static void Register()
        {
            TestRunner.Add("Condition wear: authored rates, exposure contracts and checkpoint guards", Contracts);
            TestRunner.Add("Condition wear: signal-gated degradation, physical service and saved continuation", Physical);
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static JsonValue Parts(JsonValue value) => value.Get("entities")[0].Get("components");
        private static GameFactory Factory(string profile = "wear.rational", Action<JsonValue, JsonValue> edit = null)
        {
            var equipment = Json.Parse(FrameworkProofContent.Read("equipment-condition.json"));
            var wear = Json.Parse(FrameworkProofContent.Read("condition-wear.json"));
            Parts(equipment).Get("equipment_condition").Set("wear", profile == null ? JsonValue.Null() : JsonValue.Of(profile));
            Parts(equipment).Set("network_sensor", Json.Parse("{\"level\":1,\"operation\":{\"id\":\"sensor\",\"enabled\":true,\"target\":1,\"minimum\":0,\"maximum\":10,\"unit\":\"unit.level\"}}"));
            edit?.Invoke(equipment, wear);
            return Colony.Factory(additionalDocuments: new Dictionary<string, string> {
                { "equipment-condition.json", Text(equipment) }, { "condition-wear.json", Text(wear) } });
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "colony", Seed = 993 }; request.Mods.Add(EquipmentConditionModule.ModuleId);
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Spawn(SimGame game, int seat = 0)
        {
            var entity = ((TileMap)game.World.Space).SpawnAt(game.World, game.World.Content.Get<EntityDef>("condition-machine"),
                new PlayerId(seat), new TileCoord(12 + seat * 8, 28)); game.World.Entities.Flush(); return entity;
        }
        private static void Advance(SimGame game, int count = 1, SimGame twin = null)
        {
            for (int i = 0; i < count; i++)
            {
                if (game.RunTicks(1) != 1) throw new InvalidOperationException("wear update tick=" + game.Tick + "; fault=" + game.Fault);
                if (twin != null && twin.RunTicks(1) != 1) throw new InvalidOperationException("wear twin tick=" + twin.Tick + "; fault=" + twin.Fault);
            }
        }
        private static ConditionSnapshot Read(SimGame game, EntityId id, Code source = default)
        {
            var refusal = game.Seat(0).View.Feature<EquipmentConditionView>().Read(id, source.IsNone ? Integrity : source, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("condition read=" + refusal); return value;
        }
        internal static ConditionWearSnapshot Wear(SimGame game, EntityId id, Code source = default)
        {
            var refusal = game.Seat(0).View.Feature<EquipmentConditionView>().ReadWear(id, source.IsNone ? Integrity : source, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("wear read=" + refusal); return value;
        }
        private static void Invalid(Check c, Action<JsonValue, JsonValue> edit, string why)
            => c.Throws<ContentException>(() => New(Factory(edit: edit)), why);
        private static void Contracts(Check c)
        {
            var factory = Factory(); var game = New(factory); var target = Spawn(game); var foreign = Spawn(game, 1);
            var provider = target.Get<IConditionWearProvider>(Integrity); var definition = target.Def.Component<IConditionWearDefinition>(Integrity);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "wear belongs to the framework");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "direct condition/exposure codec identity");
            c.That(ReferenceEquals(definition.Wear, game.World.Content.Get<AuthoredConditionWearDef>("wear.rational")) &&
                ReferenceEquals(definition.Unit, definition.Wear.Unit), "actual authored catalog rule/unit, no parallel registry");
            var captured = Wear(game, target.Id); ulong hash = game.Hash();
            c.That(captured.Rule == "wear.rational" && captured.Unit == new UnitId("unit.condition") && captured.Exposure.ElapsedTicks == 0 &&
                captured.Exposure.UpdatedTick == -1 && captured.Decision.Advance, "detached preview exposes actual rule and virgin exposure");
            c.Equal(hash, game.Hash(), "wear preview performs no game mutation");
            var view = game.Seat(0).View.Feature<EquipmentConditionView>();
            c.Equal(Codes.NotFound, view.ReadWear(foreign.Id, Integrity, out var hidden), "company-private wear facts");
            c.That(hidden.Rule == null && !hidden.Unit.IsValid, "foreign view withholds rule/unit/exposure");
            c.Equal(Codes.InvalidArgument, view.ReadWear(target.Id, Code.None, out _), "named source required");
            c.Equal(ConditionWearCodes.NotWear, view.ReadWear(target.Id, new Code("missing"), out _), "no arbitrary fallback condition");
            c.Equal(Code.None, game.Observer.Feature<EquipmentConditionView>().ReadWear(foreign.Id, Integrity, out _), "explicit observer diagnosis");
            ConditionWearContracts.Read(game.World, provider, out var admission);
            Advance(game); c.Equal(Fix.FromInt(10) - Fix.FromRatio(1, 3), Read(game, target.Id).Value, "first fractional exposure uses exact cumulative floor");
            c.That(captured.Exposure.ElapsedTicks == 0 && Wear(game, target.Id).Exposure.ElapsedTicks == 1, "earlier detached snapshot is immutable");
            c.Throws<InvalidOperationException>(() => ConditionWearContracts.Commit(provider, admission, captured.Decision), "previous tick admission cannot damage twice");
            var twin = factory.Load(game.Save()); Advance(game, 2, twin);
            c.Equal(Fix.FromInt(9), Read(game, target.Id).Value, "three thirds lose exactly one, not per-tick rounded drift");
            c.Equal(game.Hash(), twin.Hash(), "fractional exposure checkpoint continuation matches exact native condition");
            Advance(game, 40); c.Equal(Fix.Zero, Read(game, target.Id).Value, "authored degradation saturates at actual remaining condition");
            var exhausted = Wear(game, target.Id); Advance(game, 5);
            c.That(exhausted.Decision.Blocker == ConditionWearCodes.Exhausted && Wear(game, target.Id).Exposure.ElapsedTicks == exhausted.Exposure.ElapsedTicks,
                "failed condition earns no exposure and no negative damage");
            c.Equal(new Code("state.stopped"), target.State, "ordinary entity Update settles authored failed state");
            Invalid(c, (j, w) => Parts(j).Get("equipment_condition").Remove("wear"), "no absent wear policy default");
            Invalid(c, (j, w) => Parts(j).Get("equipment_condition").Set("wear", JsonValue.Number("0")), "wear requires null or identity");
            Invalid(c, (j, w) => Parts(j).Get("equipment_condition").Set("wear", JsonValue.Of("")), "empty wear identity refuses");
            Invalid(c, (j, w) => Parts(j).Get("equipment_condition").Set("wear", JsonValue.Of("missing")), "missing wear reference refuses before tick one");
            Invalid(c, (j, w) => w.Get("condition_wear_profiles")[0].Remove("conditions"), "explicit conditions array required even for unconditional wear");
            Invalid(c, (j, w) => w.Get("condition_wear_profiles")[0].Set("quantity", JsonValue.Number("0")), "zero degradation rate refuses");
            Invalid(c, (j, w) => w.Get("condition_wear_profiles")[0].Set("quantity", JsonValue.Number("-1")), "negative degradation rate refuses");
            Invalid(c, (j, w) => w.Get("condition_wear_profiles")[0].Get("period").Set("numerator", JsonValue.Number("0")), "zero period refuses");
            Invalid(c, (j, w) => w.Get("condition_wear_profiles")[0].Get("period").Set("denominator", JsonValue.Number("0")), "zero denominator refuses");
            Invalid(c, (j, w) => w.Get("condition_wear_profiles")[0].Get("period").Set("unit", JsonValue.Of("tick")), "rate period uses declared game duration units");
            Invalid(c, (j, w) => w.Get("condition_wear_profiles")[0].Set("unit", JsonValue.Of("unit.steel")), "wear must alias condition native unit");
            Invalid(c, (j, w) => { Parts(j).Get("equipment_condition").Set("wear", JsonValue.Of("wear.operating")); Parts(j).Remove("network_sensor"); }, "missing source rejects actual owner binding");
            Invalid(c, (j, w) => { j.Get("units")[0].Set("discrete", JsonValue.Of(true)); w.Get("condition_wear_profiles")[0].Set("quantity", JsonValue.Number("0.5")); }, "native discrete wear cannot author fractional quantum");
            Discrete(c); Checkpoint(c); Custom(c); Mapless(c); SharedRates(c, game);
        }
        private static void Discrete(Check c)
        {
            var factory = Factory(edit: (j, w) => j.Get("units")[0].Set("discrete", JsonValue.Of(true)));
            var game = New(factory); var target = Spawn(game); Advance(game, 2);
            c.That(Read(game, target.Id).Value == Fix.FromInt(10) && Read(game, target.Id).ChangedTick == -1 && Wear(game, target.Id).Exposure.ElapsedTicks == 2,
                "eligible zero-quantum updates accrue real exposure, not fake damage");
            var twin = factory.Load(game.Save()); Advance(game, 1, twin);
            c.Equal(Fix.FromInt(9), Read(game, target.Id).Value, "third eligible update commits native discrete quantum");
            var part = target.Get<IConditionProvider>(Integrity);
            c.Equal(Code.None, ConditionContracts.Commit(game.World, part, new ConditionChange(ConditionChangeKind.Repair,
                new UnitQuantity(new UnitId("unit.condition"), Fix.One))), "same authoritative repair contract preserves exposure");
            c.Equal(3L, Wear(game, target.Id).Exposure.ElapsedTicks, "repair never resets rate history");
            Advance(game, 2); c.Equal(Fix.FromInt(10), Read(game, target.Id).Value, "repaired condition preserves remaining discrete interval");
            Advance(game); c.Equal(Fix.FromInt(9), Read(game, target.Id).Value, "next native quantum is earned at cumulative exposure six");
        }
        private static byte[] Payload(Fix value, long changed, long elapsed, long updated)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); writer.Fix(ref value); writer.Long(ref changed); writer.Long(ref elapsed); writer.Long(ref updated);
                return stream.ToArray();
            }
        }
        private static byte[] Encode(EquipmentCondition part)
        { using (var stream = new MemoryStream()) { part.Sync(new StateWriter(stream)); return stream.ToArray(); } }
        private static void Decode(EquipmentCondition part, byte[] bytes) => part.Sync(new StateReader(bytes, 0, bytes.Length));
        private static void Checkpoint(Check c)
        {
            var factory = Factory(); var game = New(factory); var target = Spawn(game); var part = target.Get<EquipmentCondition>(Integrity); ulong before = game.Hash();
            foreach (var pair in new[] { (-1L, -1L), (1L, -1L), (0L, 0L), (4L, 1L), (1L, -2L) })
            {
                c.Throws<CorruptStateException>(() => Decode(part, Payload(Fix.FromInt(10), -1, pair.Item1, pair.Item2)), "malformed staged exposure refuses");
                c.Equal(before, game.Hash(), "failed exposure decode preserves sole provider payload");
            }
            byte[] original = Encode(part); Decode(part, Payload(Fix.FromInt(10), -1, 1, 1));
            c.Throws<InvalidOperationException>(() => game.Hash(), "future exposure refuses checkpoint under actual session time");
            c.Throws<CorruptStateException>(() => game.World.System<EquipmentConditionSystem>().OnLoaded(game.World), "future exposure refuses post-restore semantics");
            Decode(part, original); c.Equal(before, game.Hash(), "instrumentation restores exact original owner facts");
            var disabled = New(Factory(null)); var disabledTarget = Spawn(disabled);
            c.Throws<CorruptStateException>(() => Decode(disabledTarget.Get<EquipmentCondition>(Integrity), Payload(Fix.FromInt(10), -1, 1, 0)),
                "no-rule provider cannot fabricate historical exposure");
            Advance(disabled, 4); var disabledWear = Wear(disabled, disabledTarget.Id);
            c.That(disabledWear.Rule == null && disabledWear.Exposure.ElapsedTicks == 0 && disabledWear.Decision.Blocker == ConditionWearCodes.None,
                "explicit null rule is stable, not a manufactured fallback rate");
            long age = 1L << 42; WideTimeTests.SetTick(game, age); Decode(part, Payload(Fix.FromInt(10), -1, age, age - 1));
            var preview = Wear(game, target.Id); Advance(game);
            c.Equal(Fix.FromInt(10) - preview.Decision.Damage, Read(game, target.Id).Value, "wide cumulative rate narrows only bounded delta, not accumulated total");
            c.Equal(age + 1, Wear(game, target.Id).Exposure.ElapsedTicks, "full-width exposure has no int-sized game limit");
            var twin = factory.Load(game.Save()); Advance(game, 2, twin);
            c.Equal(game.Hash(), twin.Hash(), "wide time/exposure/condition continuation retains exact hash");
            var current = Wear(game, target.Id); var live = target.Get<IConditionWearProvider>(Integrity);
            var snapshot = ConditionWearContracts.Read(game.World, live, out var context);
            ConditionWearContracts.Commit(live, context, snapshot.Decision);
            c.Throws<InvalidOperationException>(() => ConditionWearContracts.Commit(live, context, snapshot.Decision), "one provider cannot advance twice in one tick");
            c.Equal(ConditionWearCodes.Updated, Wear(game, target.Id).Decision.Blocker, "same tick diagnostics show already-admitted exposure");
            c.Equal(current.Exposure.ElapsedTicks + 1, Wear(game, target.Id).Exposure.ElapsedTicks, "same tick rejection cannot duplicate exposure");
        }
        private static void Custom(Check c)
        {
            var factory = Factory("wear.custom"); var game = New(factory); var target = Spawn(game); Advance(game, 2);
            c.Equal(Fix.FromInt(6), Read(game, target.Id).Value, "independent public contract rule changes behavior without scheduler edits");
            var twin = factory.Load(game.Save()); Advance(game, 2, twin); c.Equal(game.Hash(), twin.Hash(), "public rule resumes provider-owned exposure codec");
            for (int mode = 1; mode <= 3; mode++)
            {
                int fault = mode; var bad = New(Factory("wear.custom", (j, w) => w.Get("proof_condition_wear_profiles")[0].Set("fault", JsonValue.Number(fault.ToString()))));
                Spawn(bad); bad.RunTicks(1); c.That(bad.Fault != null, "impure/malformed/excessive public wear rule faults actual Update mode=" + mode);
            }
        }
        private static void Mapless(Check c)
        {
            var def = new GameDefinition("wear-only", new GameModule[] { new EquipmentConditionModule() });
            var baseContent = Json.Parse(Colony.Defs); var content = JsonValue.NewObject();
            foreach (string section in new[] { "entity_states", "time_definitions", "calendars", "scenarios" }) content.Set(section, baseContent.Get(section).Clone());
            content.Set("units", Json.Parse(FrameworkProofContent.Read("equipment-condition.json")).Get("units").Clone());
            content.Set("condition_wear_profiles", Json.Parse("[{\"id\":\"wear.bare\",\"unit\":\"unit.condition\",\"quantity\":1,\"period\":{\"numerator\":3,\"denominator\":1,\"unit\":\"minute\"},\"conditions\":[]}]"));
            content.Set("entities", Json.Parse("[{\"id\":\"bare\",\"kind\":\"structure\",\"components\":{\"equipment_condition\":{\"id\":\"integrity\",\"unit\":\"unit.condition\",\"maximum\":10,\"initial\":10,\"wear\":\"wear.bare\",\"states\":{\"damaged\":\"stopped\",\"failed\":\"stopped\"}}}}]"));
            var scenario = content.Get("scenarios")[0]; scenario.Set("space", JsonValue.Null()); scenario.Set("modules", Json.Parse("[\"sim.equipment-condition\"]")); scenario.Set("settings", JsonValue.NewObject());
            var files = new PreloadedFileSource().Add("manifest.json", "{\"schema\":1,\"modules\":[{\"id\":\"sim.equipment-condition\",\"version\":\"1.0.0\",\"documents\":[\"wear.json\"]}]}").Add("wear.json", Text(content));
            var factory = new GameFactory(def, ContentSet.Load(files, def.ContentTypes)); var game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 9 });
            var target = game.World.Spawn(game.World.Content.Get<EntityDef>("bare"), new PlayerId(0)); game.World.Entities.Flush();
            c.That(game.World.Space == null && !game.World.Services.Has<WorkBoard>() && !game.World.Services.Has<IResourceTransaction>(), "wear introduces no compulsory map/work/resource owner");
            Advance(game, 2); var twin = factory.Load(game.Save()); Advance(game, 4, twin);
            c.Equal(Fix.FromInt(8), Read(game, target.Id).Value, "mapless framework accumulates authored game time");
            c.Equal(game.Hash(), twin.Hash(), "mapless saved exposure is deterministic");
            content.Get("time_definitions")[0].Set("quantum", Json.Parse("{\"numerator\":1,\"denominator\":2,\"unit\":\"second\"}"));
            content.Get("condition_wear_profiles")[0].Set("period", Json.Parse("{\"numerator\":3,\"denominator\":2,\"unit\":\"second\"}"));
            var fineFiles = new PreloadedFileSource().Add("manifest.json", files.ReadText("manifest.json")).Add("wear.json", Text(content));
            var fineFactory = new GameFactory(def, ContentSet.Load(fineFiles, def.ContentTypes));
            var fine = fineFactory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 9 });
            var fineTarget = fine.World.Spawn(fine.World.Content.Get<EntityDef>("bare"), new PlayerId(0)); fine.World.Entities.Flush();
            Advance(fine, 2); var fineTwin = fineFactory.Load(fine.Save()); Advance(fine, 4, fineTwin);
            c.Equal(Fix.FromInt(8), Read(fine, fineTarget.Id).Value, "rational subsecond session and period produce same exposure loss");
            c.Equal(fine.Hash(), fineTwin.Hash(), "non-minute quantum retains saved exact rate continuation");
        }
        private static void SharedRates(Check c, SimGame game)
        {
            c.Equal(new BigInteger(Fix.One.Raw), QuantizedGameRates.AccumulatedRaw(Fix.One, 3, 1, DurationUnit.Minute, game.World.Time,
                new WorkProgress(3), 1, false), "shared exact calculator handles cumulative rational time");
            c.Equal(BigInteger.Zero, QuantizedGameRates.AccumulatedRaw(Fix.One, 3, 1, DurationUnit.Minute, game.World.Time,
                new WorkProgress(2), Fix.One.Raw, false), "shared discrete floor keeps unearned quantum");
            c.Equal(new BigInteger(Fix.One.Raw), QuantizedGameRates.AccumulatedRaw(Fix.One, 3, 1, DurationUnit.Minute, game.World.Time,
                new WorkProgress(1), Fix.One.Raw, true), "shared discrete ceiling supports resource input billing");
            var steel = game.World.Content.Get<ResourceDef>("steel"); var rate = new ResourceRate(steel, Fix.One, 3, 1, DurationUnit.Minute);
            var capacity = game.World.Content.Get<EntityDef>("condition-machine").Component<IInventoryDefinition>().CapacityPolicy;
            c.Equal(Fix.Zero, ResourceRates.Accumulated(new[] { rate }, game.World.Time, capacity, new WorkProgress(2), ResourceRateRounding.Floor).AmountOf(steel.Resource),
                "existing resource billing delegates same floor arithmetic");
            c.Equal(Fix.One, ResourceRates.Accumulated(new[] { rate }, game.World.Time, capacity, new WorkProgress(1), ResourceRateRounding.Ceiling).AmountOf(steel.Resource),
                "existing resource billing preserves ceiling semantics");
        }
        private static bool Enabled(SimGame game, EntityId id, bool enabled)
            => TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = id, Operation = Sensor, Enabled = enabled, Target = Fix.One }).Accepted;
        private static TransformationSnapshot Process(SimGame game, EntityId id)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(id, new Code("process"), out var value))
                throw new InvalidOperationException("wear process absent"); return value;
        }
        private static void Reach(SimGame game, EntityId id, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 700 && !done(); i++) Advance(game, 1, twin);
            if (!done()) throw new InvalidOperationException(stage + " timeout; tick=" + game.Tick + "; condition=" + Read(game, id).Value + "; wear=" + Wear(game, id).Decision.Blocker + "; process=" + Process(game, id).Blocker);
        }
        private static void Physical(Check c)
        {
            var factory = Factory("wear.operating"); var game = New(factory); var target = Spawn(game);
            WideTimeTests.SetTick(game, 1L << 40); var steel = game.World.Content.ResourceId("steel"); var store = target.Get<IInventory>();
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = target.Id, Capability = new Code("process"), Transformation = "equipment.batch" }).Accepted,
                "player queues actual process through engine command");
            Reach(game, target.Id, () => Read(game, target.Id).Value == Fix.FromInt(4), "operating degradation");
            c.That(Enabled(game, target.Id, false), "player disables declared wear eligibility through ordinary operation command");
            Advance(game); var frozen = Wear(game, target.Id); var paused = Process(game, target.Id);
            c.That(frozen.Decision.Blocker == new Code("equipment.wear-disabled") && frozen.Exposure.ElapsedTicks == 6 && Read(game, target.Id).Value == Fix.FromInt(4),
                "disabled wear gate earns no time or damage");
            c.That(paused.Phase == TransformationPhase.Working && paused.Blocker == new Code("equipment.unsafe") && paused.Wip.Stock.AmountOf(steel) == Fix.FromInt(2),
                "actual degraded condition gates production and retains real WIP");
            c.Equal(new Code("state.stopped"), target.State, "entity Update publishes authored damaged state");
            c.That(TestKit.Order(game, 0, new SetAutomationProbeCommand { Entity = target.Id, Level = Fix.One, Available = false }).Accepted, "player environment request makes actual source unavailable");
            c.Equal(new Code("probe.sensor-offline"), Wear(game, target.Id).Decision.Blocker, "source unavailability keeps original typed blocker");
            Advance(game, 3); c.Equal(frozen.Exposure.ElapsedTicks, Wear(game, target.Id).Exposure.ElapsedTicks, "offline source cannot silently accrue wear");
            c.Equal(paused.Progress, Process(game, target.Id).Progress, "unsafe paused process cannot advance its retained execution");
            c.That(TestKit.Order(game, 0, new SetAutomationProbeCommand { Entity = target.Id, Level = Fix.One, Available = true }).Accepted, "source restored through normal engine request");
            var posted = TestKit.Order(game, 0, new PostOrderCommand { Target = target.Id, Plan = "equipment.service" }); c.That(posted.Accepted, "ordinary physical service admits real machine/material requirements");
            var board = game.World.Services.Get<WorkBoard>(); var order = board.Order(checked((int)(long)posted.Arg0));
            Reach(game, target.Id, () => !order.Node("repair").Progress.Equals(WorkProgress.Zero), "physical service startup");
            c.That(Read(game, target.Id).Value == Fix.FromInt(4) && store.Amount(steel) == Fix.FromInt(25) && store.ReservedOut(steel) == Fix.FromInt(2),
                "partial service moves/works, consumes actual three spares, holds two tools and never repairs early");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "save during condition service retains gate, exposure, WIP and material claims");
            Advance(game, 2, twin); c.Equal(game.Hash(), twin.Hash(), "interrupted service twins advance deterministically");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted && TestKit.Order(twin, 0, new CancelOrderCommand { Order = order.Id }).Accepted,
                "player cancels physical service through same command authority");
            c.That(Read(game, target.Id).Value == Fix.FromInt(4) && store.Amount(steel) == Fix.FromInt(25) && store.ReservedOut(steel) == Fix.Zero,
                "cancellation cannot fake repair/refund; releases only held tools");
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Target = target.Id, Plan = "equipment.service" }).Accepted &&
                TestKit.Order(twin, 0, new PostOrderCommand { Target = target.Id, Plan = "equipment.service" }).Accepted, "replacement service is fresh real execution");
            Reach(game, target.Id, () => Read(game, target.Id).Value == Fix.FromInt(10), "physical repair completion", twin);
            c.That(store.Amount(steel) == Fix.FromInt(22) && store.ReservedOut(steel) == Fix.Zero && Wear(game, target.Id).Exposure.ElapsedTicks == 6,
                "once-only repair pays fresh spares, releases tools and preserves exposure history");
            Advance(game, 3, twin);
            c.That(Process(game, target.Id).Progress.WholeTicks > paused.Progress.WholeTicks && Process(game, target.Id).Blocker.IsNone,
                "real process resumes only after physical condition restoration");
            c.Equal(game.Hash(), twin.Hash(), "physical service, condition and paused/resumed WIP remain isolated and deterministic");
            c.That(Enabled(game, target.Id, true) && Enabled(twin, target.Id, true), "player re-enables authored degradation normally");
            c.Equal(Fix.FromInt(9), Read(game, target.Id).Value, "repaired equipment wears again on next real update, not an instant host mutation");
            c.Equal(7L, Wear(game, target.Id).Exposure.ElapsedTicks, "resumed eligibility continues cumulative exposure after repair");
            c.Equal(game.Hash(), twin.Hash(), "re-enabled policy and wide game ages keep saved twin identity");
            CapturePass(c); ImpureSignal(c);
        }
        private static void CapturePass(Check c)
        {
            var factory = Factory("wear.operating", (j, w) => {
                w.Get("condition_wear_profiles")[1].Set("conditions", JsonValue.NewArray());
                var first = Parts(j).Get("equipment_condition"); first.Set("initial", JsonValue.Number("1"));
                var second = first.Clone(); second.Set("id", JsonValue.Of("secondary")); second.Set("initial", JsonValue.Number("10")); second.Set("wear", JsonValue.Of("wear.dependent"));
                var owners = JsonValue.NewArray(); owners.Add(first); owners.Add(second); Parts(j).Set("equipment_condition", owners);
            });
            var game = New(factory); var target = Spawn(game); Advance(game);
            c.That(Read(game, target.Id).Value == Fix.Zero && Read(game, target.Id, new Code("secondary")).Value == Fix.FromInt(9),
                "complete pass captures sibling eligibility before primary damage commits");
            Advance(game); var secondary = Wear(game, target.Id, new Code("secondary"));
            c.That(secondary.Decision.Blocker == new Code("equipment.primary-exhausted") && secondary.Exposure.ElapsedTicks == 1 && Read(game, target.Id, new Code("secondary")).Value == Fix.FromInt(9),
                "dependent condition sees primary loss only on next update");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "multiple named provider exposure codecs persist without a mirrored clock");
        }
        private static void ImpureSignal(Check c)
        {
            var game = New(Factory("wear.operating")); var target = Spawn(game); target.Get<AutomationProbe>().FaultMode = 2;
            game.RunTicks(1); c.That(game.Fault != null, "signal mutation during wear eligibility faults actual game Update");
        }
    }
}
