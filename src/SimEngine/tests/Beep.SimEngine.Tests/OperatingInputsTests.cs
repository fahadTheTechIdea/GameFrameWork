using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class OperatingInputsTests
    {
        internal static void Register()
        {
            TestRunner.Add("Operating inputs: phases, public contracts and atomic resource admission", c => { Admission(c); HorizonAdmission(c); });
            TestRunner.Add("Operating inputs: physical work, standby resupply and saved continuation", c => { Physical(c); HorizonPhysical(c); });
        }
        private static GameFactory Factory(int stock = 2, bool courier = false, bool extension = false, int fault = 0, Action<JsonValue> edit = null, bool worker = false)
        {
            var root = Content.Json.Parse(PointGame.Definitions); var machine = root.Get("entities")[2]; var parts = machine.Get("components");
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(UtilitySignalsModule.ModuleId));
            if (courier)
            {
                var carrier = machine.Clone(); carrier.Set("id", JsonValue.Of("courier"));
                carrier.Get("components").Get("worker").Set("work_rate", JsonValue.Number("1")); root.Get("entities").Add(carrier);
                StartupContentAuthoring.Add(root, "courier", "courier", 0, carrier.Get("components").Get("point_presence"));
            }
            if (!worker) { parts.Remove("worker"); parts.Remove("transporter"); }
            parts.Get("storage").Set("initial_stock", stock == 0 ? JsonValue.NewObject() : Content.Json.Parse("{\"supply\":" + stock + "}"));
            parts.Set("motion_execution", Content.Json.Parse(FrameworkProofContent.Read("motion-execution.json")));
            parts.Set("operating_inputs", Content.Json.Parse(FrameworkProofContent.Read("motion-operating-inputs.json")));
            parts.Set("utility_signal", Content.Json.Parse("{\"id\":\"utility\",\"consumer\":\"propulsion\"}"));
            if (extension)
            {
                var proof = parts.Get("operating_inputs").Clone(); proof.Remove("states"); proof.Set("id", JsonValue.Of("auxiliary"));
                proof.Set("fault", JsonValue.Number(fault.ToString())); parts.Set("proof_operating_inputs", proof);
            }
            edit?.Invoke(root); var text = new StringBuilder(); root.WriteCanonical(text);
            return PointGame.Factory(text.ToString(), retryProfiles: FrameworkProofContent.Read("haul-retry.json"), flowDefinitions: FrameworkProofContent.Read("endpoints-flows.json"),
                extraModules: new GameModule[] { new UtilitySignalsModule() },
                extraDeclarations: "[{\"id\":\"sim.utility-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[\"motion-utility-signals.json\"]}]",
                extraDocuments: new Dictionary<string, string> { ["motion-utility-signals.json"] = FrameworkProofContent.Read("motion-utility-signals.json") });
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 641 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            request.Mods.Add(ResourceFlowsModule.ModuleId); return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string definition)
        { foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == definition) return entity; throw new InvalidOperationException(definition); }
        private static MotionExecutionView Read(SimGame game)
        { var result = game.Seat(0).View.Feature<MotionView>().ReadExecution(Find(game, "machine").Id, out var value); if (!result.IsNone) throw new InvalidOperationException(result.ToString()); return value; }
        private static UtilityDemandSnapshot Utility(SimGame game, string capability = "propulsion")
        {
            if (!game.Seat(0).View.Feature<OperationsView>().TryGetUtilityDemand(Find(game, "machine").Id, new Code(capability), out var facts))
                throw new InvalidOperationException("missing actual motion utility " + capability);
            return facts;
        }
        private static AutomationSignalSnapshot Signal(SimGame game, string metric)
        {
            Code refusal = game.Seat(0).View.Feature<OperationsView>().ReadSignal(Find(game, "machine").Id, new Code("utility"), "motion.utility." + metric, out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("motion utility signal refused " + refusal);
            return facts;
        }
        private static void Control(Check c, SimGame game, bool enabled, Fix rate)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = Find(game, "machine").Id, Operation = new Code("travel"), Enabled = enabled, Target = rate }).Accepted, "ordinary command controls actual motion");
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 200 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " timeout: tick=" + game.Tick + " status=" + Read(game).Status + " blocker=" + Read(game).Blocker);
        }
        private static byte[] ExposureBytes(long elapsed, long updated)
        {
            using (var stream = new MemoryStream()) { var writer = new StateWriter(stream); writer.Long(ref elapsed); writer.Long(ref updated); writer.Flush(); return stream.ToArray(); }
        }
        private static void Admission(Check c)
        {
            var factory = Factory(); var game = New(factory); var entity = Find(game, "machine"); var stock = entity.Get<IInventory>(); var supply = game.World.Content.ResourceId("supply");
            var original = Read(game); ulong before = game.Hash();
            c.Equal(1, original.Operating.Providers.Count, "native exposure is one actual named capability");
            c.Equal(new Code("propulsion"), original.Operating.Providers[0].Demand.Consumer, "authored identity is projected");
            c.Equal(0L, original.Operating.Providers[0].Exposure.ElapsedTicks, "idle has no exposure");
            c.Equal(-1L, original.Operating.Providers[0].Exposure.UpdatedTick, "no fictitious accepted time");
            c.Equal(Fix.One, original.Operating.RequiredInputs.AmountOf(supply), "cumulative ceiling prepays one native quantum");
            c.Equal(Code.None, original.Blocker, "actual stock admits next motion attempt");
            c.Equal(before, game.Hash(), "projection is pure");
            var consumer = entity.Get<IUtilityConsumer>(new Code("propulsion")); var ids = new List<Code>();
            c.That(ReferenceEquals(consumer, entity.Get<IOperatingInputs>(new Code("propulsion"))) && ReferenceEquals(consumer.ResolveUtilityPort(game.World, supply).ResolveStore(game.World).Store, stock),
                "native utility is the actual existing motion input provider and inventory");
            c.That(game.Seat(0).View.Feature<OperationsView>().CopyUtilityConsumerIds(entity.Id, ids) && ids.Count == 1 && ids[0] == new Code("propulsion"),
                "existing generic view discovers motion without a type case");
            var utility = Utility(game);
            c.That(utility.Drawing && utility.Satisfied && utility.ElapsedTicks == 0, "idle next-attempt readiness neither earns exposure nor claims current movement");
            c.Equal(original.Operating.Providers[0].Demand.NextInputs.AmountOf(supply), utility.NextInputs.AmountOf(supply), "generic and motion views share next draw");
            c.Equal(stock.Endpoint, utility.Ports[0].Endpoint, "utility identifies actual physical endpoint");
            c.Equal(stock.Version, utility.Ports[0].Version, "utility identifies actual stock version");
            c.Equal(Fix.One, Signal(game, "ready").Value.Value, "authored generic signal exposes prospective input readiness");
            c.Equal(Fix.One, Signal(game, "requested").Value.Value, "generic signal requests actual next quantum");
            c.Equal(Fix.One, Signal(game, "available").Value.Value, "generic signal reports only needed unpromised contribution");
            c.Equal(Fix.Zero, Signal(game, "consumed").Value.Value, "idle observation never fabricates consumption");
            c.Throws<NotSupportedException>(() => ((IList<ResourceId>)utility.Resources).Clear(), "utility resource identities are immutable");
            c.Equal(before, game.Hash(), "generic views and signals do not create reservations or move the sole clock");
            c.That(!game.Seat(1).View.Feature<OperationsView>().TryGetUtilityDemand(entity.Id, new Code("propulsion"), out _), "foreign generic utility facts are withheld");
            c.That(!game.Seat(0).View.Feature<OperationsView>().TryGetUtilityDemand(entity.Id, new Code("missing"), out _), "missing named consumer is never substituted");
            c.Throws<NotSupportedException>(() => ((IList<OperatingUtilitySnapshot>)original.Operating.Providers).Clear(), "provider facts are immutable");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<MotionView>().ReadExecution(entity.Id, out var hidden), "foreign company cannot read propulsion stock readiness");
            c.That(hidden == null, "foreign read returns no facts or capability");
            var reservationOwner = new ReservationOwner("proof.cargo", 1); var receipt = stock.ReserveOut(reservationOwner, supply, Fix.FromInt(2));
            c.Equal(OperatingInputCodes.Inputs, Read(game).Blocker, "cargo promises cannot be burned as propulsion input");
            c.That(Utility(game).Blocker == UtilityCodes.Shortage && Utility(game).Available.IsEmpty, "generic demand honors actual outbound cargo claims");
            c.Equal(Fix.Zero, Signal(game, "ready").Value.Value, "shortage is available false readiness, not an unavailable substitute");
            c.Equal(Fix.One, Signal(game, "requested").Value.Value, "shortage retains the physical next input request");
            entity.Get<PointMotion>().GoTo(Fix.FromInt(10)); Tick(game);
            c.Equal(Fix.Zero, entity.Get<PointPresence>().Position, "reserved stock blocks actual advancement");
            c.Equal(Fix.FromInt(2), stock.Amount(supply), "shortage causes no partial burn");
            c.Equal(0L, Read(game).Operating.Providers[0].Exposure.ElapsedTicks, "shortage grants no exposure credit");
            c.That(stock.CheckReservedOut(reservationOwner, receipt).Ready, "native cargo promise survives wait");
            stock.ReleaseOut(reservationOwner, receipt); Tick(game);
            c.Equal(Fix.One, stock.Amount(supply), "release permits actual whole draw");
            c.Equal(1L, Read(game).Operating.Providers[0].Exposure.ElapsedTicks, "successful attempt advances one exposure tick");
            c.Equal(0L, original.Operating.Providers[0].Exposure.ElapsedTicks, "old detached projection is unchanged");
            c.Equal(Fix.Zero, utility.Consumed.AmountOf(supply), "retained utility facts never follow actual later consumption");
            c.Equal(Fix.One, Utility(game).Consumed.AmountOf(supply), "generic consumer reports the physically paid quantum");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "actual input/exposure/position restore together");

            var combined = New(Factory(stock: 1, extension: true)); var budget = Read(combined).Operating;
            c.Equal(2, budget.Providers.Count, "independent public provider joins existing admission");
            c.Equal(new Code("auxiliary"), budget.Providers[0].Demand.Consumer, "provider order is stable identity order");
            c.Equal(Fix.FromInt(2), budget.RequiredInputs.AmountOf(combined.World.Content.ResourceId("supply")), "overlapping resource draws merge before admission");
            c.Equal(OperatingInputCodes.Inputs, budget.Blocker, "individually affordable draws refuse as a whole");
            c.That(Utility(combined).Satisfied && Utility(combined, "auxiliary").Satisfied, "individual readiness is not aggregate stock admission");
            c.That(Find(combined, "machine").Get<IUtilityConsumer>(new Code("auxiliary")) is ProofOperatingInputs,
                "public-only provider joins generic utility without an adapter or alternate fuel arithmetic");
            Find(combined, "machine").Get<PointMotion>().GoTo(Fix.FromInt(10)); Tick(combined);
            c.Equal(Fix.One, Find(combined, "machine").Get<IInventory>().Amount(combined.World.Content.ResourceId("supply")), "no provider partially consumes combined shortage");
            c.Equal(0L, Read(combined).Operating.Providers[0].Exposure.ElapsedTicks, "public provider earns no shortage exposure");
            var enoughFactory = Factory(stock: 4, extension: true); var enough = New(enoughFactory); Find(enough, "machine").Get<PointMotion>().GoTo(Fix.FromInt(10)); Tick(enough);
            c.Equal(Fix.FromInt(2), Find(enough, "machine").Get<IInventory>().Amount(enough.World.Content.ResourceId("supply")), "whole input vector consumes once");
            c.Equal(1L, Read(enough).Operating.Providers[0].Exposure.ElapsedTicks, "public provider owns exact accepted exposure");
            c.Equal(1L, Read(enough).Operating.Providers[1].Exposure.ElapsedTicks, "native provider owns its exact accepted exposure");
            c.Equal(enough.Hash(), enoughFactory.Load(enough.Save()).Hash(), "independent exposure owners restore normally");
            for (int fault = 1; fault <= 3; fault++)
            {
                c.Throws<InvalidOperationException>(() => Read(New(Factory(extension: true, fault: fault))), "mutating/malformed/future public observation is rejected " + fault);
                c.Throws<InvalidOperationException>(() => Utility(New(Factory(extension: true, fault: fault)), "auxiliary"), "generic view guards actual public motion exposure " + fault);
            }
            for (int fault = 6; fault <= 9; fault++)
                c.Throws<InvalidOperationException>(() => Read(New(Factory(stock: 8, extension: true, fault: fault))), "forged motion utility cannot change authored inputs/drawing/consumption/exposure " + fault);
            for (int fault = 4; fault <= 5; fault++)
            {
                var bad = New(Factory(stock: 8, extension: true, fault: fault)); Find(bad, "machine").Get<PointMotion>().GoTo(Fix.FromInt(10)); bad.Update();
                c.That(bad.Fault != null, "non-exact or stock-mutating exposure commit faults " + fault);
                c.Equal(Fix.Zero, Find(bad, "machine").Get<PointPresence>().Position, "invalid participant cannot advance actual motion " + fault);
            }
            Strict(c); Decode(c);
            WorkerAdmission(c);
            c.Equal(6, game.World.System<MovementSystem>().StateSchemaVersion, "movement semantics have explicit schema identity");
            c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "work update owns inactive phase expenditure");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current source has a distinct identity");
        }
        private static void Strict(Check c)
        {
            foreach (string field in new[] { "id", "phase", "inputs", "states" })
                c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[2].Get("components").Get("operating_inputs").Remove(field))), "explicit operating field is required: " + field);
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[2].Get("components").Remove("storage"))), "no fabricated fuel inventory");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[2].Get("components").Remove("point_motion"))), "inputs require actual motion");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[2].Get("components").Get("operating_inputs").Get("inputs")[0].Set("resource", JsonValue.Of("missing")))), "missing native resource refuses");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[2].Get("components").Get("operating_inputs").Get("inputs")[0].Set("quantity", JsonValue.Number("0.5")))), "discrete rate quantum refuses fractional content");
            c.Throws<ContentException>(() => New(Factory(edit: root => { var rows = root.Get("entities")[2].Get("components").Get("operating_inputs").Get("inputs"); rows.Add(rows[0].Clone()); })), "duplicate native resource rate refuses");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[2].Get("components").Get("operating_inputs").Get("inputs")[0].Get("period").Set("denominator", JsonValue.Number("0")))), "invalid rational period refuses");
            var empty = New(Factory(stock: 0, edit: root => root.Get("entities")[2].Get("components").Get("operating_inputs").Set("inputs", JsonValue.NewArray())));
            Find(empty, "machine").Get<PointMotion>().GoTo(Fix.FromInt(2)); Tick(empty);
            c.Equal(Fix.One, Find(empty, "machine").Get<PointPresence>().Position, "explicit empty rates still allow actual motion");
            c.That(Read(empty).Operating.RequiredInputs.IsEmpty, "empty rates never fabricate supply");
            c.That(!Utility(empty).Drawing && Utility(empty).Blocker == UtilityCodes.Inactive && Utility(empty).Resources.Count == 0,
                "explicit empty motion rates never invent an active utility");
            c.That(!Signal(empty, "ready").Available && Signal(empty, "ready").Blocker == UtilityCodes.Inactive,
                "empty-rate signal withholds active readiness");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[2].Get("components").Get("utility_signal").Set("consumer", JsonValue.Of("missing")))),
                "authored utility signal requires actual named motion consumer");
            c.Throws<ContentException>(() => New(Factory(stock: 0, edit: root => root.Get("entities")[2].Get("components").Get("storage").Set("phases", Content.Json.Parse("[\"liquid\"]")))), "actual inventory filter rejects incompatible motion input before tick one");
            var multipleFactory = Factory(stock: 4, edit: root =>
            {
                var parts = root.Get("entities")[2].Get("components"); var entries = JsonValue.NewArray(); var first = parts.Get("operating_inputs").Clone();
                first.Set("id", JsonValue.Of("auxiliary")); entries.Add(first); entries.Add(parts.Get("operating_inputs").Clone()); parts.Set("operating_inputs", entries);
            });
            var multiple = New(multipleFactory); Find(multiple, "machine").Get<PointMotion>().GoTo(Fix.FromInt(4)); Tick(multiple);
            c.Equal(2, Read(multiple).Operating.Providers.Count, "one registered component admits multiple actual named native owners");
            c.Equal(new Code("auxiliary"), Read(multiple).Operating.Providers[0].Demand.Consumer, "native named owners use the shared stable ordering");
            c.Equal(Fix.FromInt(2), Find(multiple, "machine").Get<IInventory>().Amount(multiple.World.Content.ResourceId("supply")), "multiple native owners share whole inventory admission without duplicate implementation");
            c.Equal(multiple.Hash(), multipleFactory.Load(multiple.Save()).Hash(), "named native owner array saves normally");
        }
        private static void Decode(Check c)
        {
            var factory = Factory(); var game = New(factory); var part = Find(game, "machine").Get<OperatingInputs>(); var before = part.ReadExposure();
            var invalid = ExposureBytes(1, -1);
            c.Throws<CorruptStateException>(() => part.Sync(new StateReader(invalid, 0, invalid.Length)), "malformed exposure rejects staged decode");
            c.Equal(before.ElapsedTicks, part.ReadExposure().ElapsedTicks, "failed decode retains elapsed owner state");
            c.Equal(before.UpdatedTick, part.ReadExposure().UpdatedTick, "failed decode retains accepted tick");
            var valid = ExposureBytes(1, 0);
            c.Throws<CorruptStateException>(() => part.Sync(new StateReader(valid, 0, 8)), "truncated exposure rejects before assignment");
            c.Equal(before.ElapsedTicks, part.ReadExposure().ElapsedTicks, "truncated decode leaves prior exposure");
            var future = ExposureBytes(1, 100); part.Sync(new StateReader(future, 0, future.Length));
            c.Throws<InvalidOperationException>(() => game.Hash(), "checkpoint rejects future accepted time against actual session");
            c.Throws<InvalidOperationException>(() => part.AdvanceExposure(default), "default context is not engine admission");
            part.Sync(new StateReader(ExposureBytes(0, -1), 0, 16));
            var overflow = New(Factory(edit: root => root.Get("entities")[2].Get("components").Get("operating_inputs").Get("inputs")[0].Set("quantity", JsonValue.Number("2000000000"))));
            Tick(overflow); Tick(overflow); var native = Find(overflow, "machine").Get<OperatingInputs>(); native.Sync(new StateReader(ExposureBytes(2, 1), 0, 16));
            c.Equal(OperatingInputCodes.Range, Read(overflow).Blocker, "unrepresentable next cumulative draw is a typed range blocker");
            c.Equal(OperatingInputCodes.Range, Utility(overflow).NextBlocker, "generic motion utility retains the same representational blocker");
            c.That(!Signal(overflow, "ready").Available && Signal(overflow, "ready").Blocker == OperatingInputCodes.Range,
                "unrepresentable draw cannot masquerade as shortage or ready fuel");
        }
        private static void Physical(Check c)
        { Timed(c); Resupply(c); Attempt(c); Mixed(c); WorkerPhysical(c); }
        private static void Timed(Check c)
        {
            var factory = Factory(); var game = New(factory); var entity = Find(game, "machine"); var store = entity.Get<IInventory>(); var supply = game.World.Content.ResourceId("supply");
            for (int i = 0; i < 3; i++) Tick(game);
            c.Equal(Fix.FromInt(2), store.Amount(supply), "idle ticks burn no stock");
            c.Equal(0L, Read(game).Operating.Providers[0].Exposure.ElapsedTicks, "idle grants no paid exposure");
            c.Equal(0L, Utility(game).ElapsedTicks, "generic local exposure is not an independent current clock");
            c.Equal(Fix.Zero, Signal(game, "consumed").Value.Value, "idle game updates do not count as utility consumption");
            entity.Get<PointMotion>().GoTo(Fix.FromInt(10)); Tick(game);
            c.Equal(Fix.One, entity.Get<PointPresence>().Position, "first funded attempt moves actual distance");
            c.Equal(Fix.One, store.Amount(supply), "first attempt prepays native quantum");
            c.That(Read(game).Operating.RequiredInputs.IsEmpty, "second interval uses already-paid quantum credit");
            c.That(Utility(game).Drawing && Utility(game).Satisfied && Utility(game).NextInputs.IsEmpty, "prepaid interval is ready without recharging a native quantum");
            c.Equal(Fix.One, Signal(game, "consumed").Value.Value, "signals report actual cumulative payment rather than next request");
            var twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.Equal(Fix.FromInt(2), entity.Get<PointPresence>().Position, "paid fractional interval earns distance");
            c.Equal(Fix.One, store.Amount(supply), "fractional interval is not charged again");
            c.Equal(game.Hash(), twin.Hash(), "saved prepaid fractional boundary continues identically");
            Control(c, game, false, Fix.One); Control(c, twin, false, Fix.One);
            for (int i = 0; i < 3; i++) { Tick(game); Tick(twin); }
            c.Equal(2L, Read(game).Operating.Providers[0].Exposure.ElapsedTicks, "disabled waits grant no exposure");
            c.Equal(Fix.One, store.Amount(supply), "disabled waits burn no fuel");
            Control(c, game, true, Fix.Zero); Control(c, twin, true, Fix.Zero); Tick(game); Tick(twin);
            c.Equal(2L, Read(game).Operating.Providers[0].Exposure.ElapsedTicks, "zero-rate waits grant no exposure");
            Control(c, game, true, Fix.One); Control(c, twin, true, Fix.One); Tick(game); Tick(twin);
            c.Equal(Fix.FromInt(4), entity.Get<PointPresence>().Position, "two final funded intervals move without teleport");
            c.Equal(Fix.Zero, store.Amount(supply), "real stock is exhausted");
            c.Equal(OperatingInputCodes.Inputs, Read(game).Blocker, "next unpaid interval has typed shortage");
            c.Equal(MoveStatus.Moving, entity.Get<PointMotion>().Status, "shortage retains actual motion goal");
            for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); }
            c.Equal(Fix.FromInt(4), entity.Get<PointPresence>().Position, "shortage waits grant no distance");
            c.Equal(4L, Read(game).Operating.Providers[0].Exposure.ElapsedTicks, "shortage waits grant no credit");
            c.That(Utility(game).Blocker == UtilityCodes.Shortage && Utility(game).ElapsedTicks == 4, "generic shortage agrees with stopped physical progress");
            c.Equal(Fix.FromInt(2), Signal(game, "consumed").Value.Value, "shortage waiting never fabricates further utility burn");
            c.Equal(new Code("point.blocked"), entity.State, "entity Update resolves authored shortage state");
            c.Equal(game.Hash(), twin.Hash(), "disabled/zero/shortage history resumes deterministically");
            long clock = game.Clock.StepIndex; ulong paused = game.Hash(); var pausedDemand = Utility(game);
            game.Clock.Pause(); c.Equal(0, game.Update(), "sole paused game clock runs no game update steps");
            c.Equal(clock, game.Clock.StepIndex, "utility observations have no independently advancing clock");
            c.Equal(paused, game.Hash(), "pause leaves actual stock/exposure/state untouched");
            c.Equal(pausedDemand.ElapsedTicks, Utility(game).ElapsedTicks, "pause earns no local execution duration");
            game.Clock.Resume();
            BigInteger consumed = 0; foreach (var item in game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates)
                if (item.Kind == ResourceFlowKind.Consumed && item.Cause == OperatingInputCodes.Cause) consumed += item.AmountRaw;
            c.Equal(new BigInteger(Fix.FromInt(2).Raw), consumed, "existing journal reports exactly the real motion consumption");
        }
        private static void Resupply(Check c)
        {
            var factory = Factory(courier: true); var game = New(factory); var isolated = New(factory); var entity = Find(game, "machine"); var supply = game.World.Content.ResourceId("supply");
            entity.Get<PointMotion>().GoTo(Fix.FromInt(10)); Reach(game, () => Read(game).Blocker == OperatingInputCodes.Inputs, "initial fuel depletion");
            c.Equal(Fix.FromInt(4), entity.Get<PointPresence>().Position, "consumer strands at actual reached location");
            Control(c, game, false, Fix.One);
            var board = game.World.Services.Get<WorkBoard>(); int id = game.World.Services.Get<IHaulExecution>().AcquireHaul(new PlayerId(0), Find(game, "source").Id, entity.Id, supply, Fix.FromInt(4), 100);
            c.That(id > 0, "normal engine hauling admits physical refuel to same inlet"); var job = board.Job(id);
            Reach(game, () => job.ToilIndex == 2 && Find(game, "courier").Get<PointPresence>().Position > Fix.Zero, "loaded refuel departure");
            c.Equal(Fix.Zero, entity.Get<IInventory>().Amount(supply), "loaded courier has not teleported fuel to consumer");
            c.Equal(Fix.FromInt(4), Find(game, "courier").Get<ITransporter>().Cargo.Amount(supply), "refuel material exists as ordinary carrier cargo");
            c.That(Utility(game).Blocker == UtilityCodes.Shortage && Utility(game).Available.IsEmpty, "inbound promises and traveling fuel are not current usable stock");
            var twin = factory.Load(game.Save()); Reach(game, () => !job.IsLive, "saved physical refuel", twin);
            c.Equal(Fix.FromInt(4), entity.Get<IInventory>().Amount(supply), "physical unloading resupplies actual native inventory");
            c.Equal(Fix.FromInt(4), entity.Get<PointPresence>().Position, "player shutdown keeps consumer still during unloading");
            c.Equal(4L, Read(game).Operating.Providers[0].Exposure.ElapsedTicks, "resupply does not reset paid exposure");
            c.That(Utility(game).Satisfied && Utility(twin).Satisfied, "physical receipt alone restores generic next-attempt input readiness");
            c.Equal(Fix.FromInt(2), Utility(game).Consumed.AmountOf(supply), "loading and delivery do not count as propulsion consumption");
            Control(c, game, true, Fix.One); Control(c, twin, true, Fix.One);
            Reach(game, () => entity.Get<PointMotion>().Status == MoveStatus.Arrived, "funded saved arrival", twin);
            c.Equal(Fix.FromInt(10), entity.Get<PointPresence>().Position, "consumer reaches original retained goal");
            c.Equal(10L, Read(game).Operating.Providers[0].Exposure.ElapsedTicks, "only ten actual funded attempts are earned");
            c.Equal(Fix.One, entity.Get<IInventory>().Amount(supply), "exact cumulative ceiling consumes five total units");
            c.Equal(Fix.FromInt(5), Utility(game).Consumed.AmountOf(supply), "generic cumulative consumption agrees with timed physical stock conservation");
            c.Equal(Utility(game).Consumed.AmountOf(supply), Utility(twin).Consumed.AmountOf(supply), "generic saved twin facts agree after loaded refuel and continued movement");
            c.Equal(Fix.FromInt(16), Find(game, "source").Get<IOutletPort>().Amount(supply), "refuel draws from actual source stock");
            c.Equal(Fix.Zero, Find(game, "courier").Get<ITransporter>().Cargo.Amount(supply), "refuel cargo is delivered once");
            c.Equal(game.Hash(), twin.Hash(), "loaded refuel/exposure/goal continuation hashes agree");
            Tick(isolated); c.Equal(Fix.FromInt(2), Find(isolated, "machine").Get<IInventory>().Amount(isolated.World.Content.ResourceId("supply")), "interleaved session receives no fuel consumption");
        }
        private static void Attempt(Check c)
        {
            var game = New(Factory()); var entity = Find(game, "machine"); entity.Get<PointMotion>().GoTo(Fix.FromInt(10));
            c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = false }).Accepted, "normal command changes route access");
            c.Equal(MoveStatus.Unreachable, entity.Get<PointMotion>().Status, "actual motion provider reports route failure");
            c.Equal(Fix.Zero, entity.Get<PointPresence>().Position, "failed route attempt earns no distance");
            c.Equal(1L, Read(game).Operating.Providers[0].Exposure.ElapsedTicks, "authored active-attempt semantics charge one attempted phase");
            c.Equal(Fix.One, entity.Get<IInventory>().Amount(game.World.Content.ResourceId("supply")), "route attempt consumes physical input, not inferred distance formula");
            Tick(game); c.Equal(1L, Read(game).Operating.Providers[0].Exposure.ElapsedTicks, "unreachable inactive provider is not repeatedly charged");
        }
        private static void Mixed(Check c)
        {
            var factory = Factory(edit: root =>
            {
                root.Get("units").Add(Content.Json.Parse("{\"id\":\"unit.fuel\",\"dimension\":\"volume\",\"scale\":{\"numerator\":1,\"denominator\":1},\"discrete\":false}"));
                root.Get("resources")[0].Get("capacity").Set("unit.capacity.mass", JsonValue.Number("1"));
                root.Get("resources").Add(Content.Json.Parse("{\"id\":\"fuel\",\"unit\":\"unit.fuel\",\"category\":\"general\",\"phase\":\"liquid\",\"traits\":[],\"capacity\":{\"unit.fuel\":1,\"unit.capacity.mass\":1}}"));
                root.Get("capacity_policies").Add(Content.Json.Parse("{\"id\":\"fixture.mixed.inputs\",\"limits\":{\"unit.capacity.mass\":8}}"));
                var parts = root.Get("entities")[2].Get("components"); parts.Get("storage").Set("capacity_policy", JsonValue.Of("fixture.mixed.inputs"));
                parts.Get("storage").Set("initial_stock", Content.Json.Parse("{\"supply\":2,\"fuel\":2}"));
                parts.Get("operating_inputs").Get("inputs").Add(Content.Json.Parse("{\"resource\":\"fuel\",\"quantity\":1,\"period\":{\"numerator\":3,\"denominator\":1,\"unit\":\"minute\"}}"));
            });
            var game = New(factory); var entity = Find(game, "machine"); var fuel = game.World.Content.ResourceId("fuel"); var supply = game.World.Content.ResourceId("supply");
            c.Equal(2, Read(game).Operating.RequiredInputs.Count, "distinct native dimensions remain a whole input vector");
            var firstDraw = Fix.FromRaw((Fix.One.Raw + 2) / 3);
            c.Equal(firstDraw, Read(game).Operating.RequiredInputs.AmountOf(fuel), "liquid input uses exact native raw-unit cumulative ceiling");
            entity.Get<PointMotion>().GoTo(Fix.FromInt(4)); Tick(game);
            c.Equal(Fix.FromInt(2) - firstDraw, entity.Get<IInventory>().Amount(fuel), "actual liquid stock pays exact first interval");
            c.Equal(Fix.One, entity.Get<IInventory>().Amount(supply), "discrete and liquid inputs commit together");
            var twin = factory.Load(game.Save()); Reach(game, () => entity.Get<PointMotion>().Status == MoveStatus.Arrived, "mixed-input saved travel", twin);
            c.Equal(Fix.FromInt(2) - Fix.FromRaw((Fix.One.Raw * 4 + 2) / 3), entity.Get<IInventory>().Amount(fuel), "four intervals equal cumulative ceiling rather than four rounded draws");
            c.Equal(Fix.Zero, entity.Get<IInventory>().Amount(supply), "whole-resource cumulative quantization stays independent of liquid dimension");
            c.Equal(game.Hash(), twin.Hash(), "mixed native-resource continuation is identical");
            c.Equal(Fix.FromRaw((Fix.One.Raw * 4 + 2) / 3), Utility(game).Consumed.AmountOf(fuel), "generic liquid consumption uses the same exact native rounding");
            c.Equal(Fix.FromInt(2), Utility(game).Consumed.AmountOf(supply), "generic discrete consumption keeps its separate native dimension");
        }
    }
}
