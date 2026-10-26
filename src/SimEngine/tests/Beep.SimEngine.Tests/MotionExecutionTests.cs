using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    internal static class MotionExecutionTests
    {
        internal static void Register()
        {
            TestRunner.Add("Motion execution: authored contracts, controls and private readiness", Admission);
            TestRunner.Add("Motion execution: timed travel, retained cargo and saved continuation", Physical);
        }
        private static GameFactory Factory(Action<JsonValue> edit = null, bool extension = false, int fault = 0, bool condition = false)
        {
            var root = Content.Json.Parse(PointGame.Definitions); var parts = root.Get("entities")[2].Get("components");
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            parts.Get("worker").Set("work_rate", JsonValue.Number("1"));
            parts.Set("worker_execution", Content.Json.Parse(FrameworkProofContent.Read("worker-execution.json")));
            parts.Set("motion_execution", Content.Json.Parse(FrameworkProofContent.Read("motion-execution.json")));
            if (extension) parts.Set("proof_motion_execution", Content.Json.Parse("{\"id\":\"ambient\",\"multiplier\":0.5,\"fault\":" + fault + "}"));
            if (condition)
            {
                parts.Set("equipment_condition", Content.Json.Parse("{\"id\":\"integrity\",\"unit\":\"unit.condition\",\"maximum\":10,\"initial\":10,\"wear\":null,\"states\":{\"damaged\":\"blocked\",\"failed\":\"blocked\"}}"));
                parts.Get("motion_execution").Set("conditions", Content.Json.Parse("[{\"id\":\"safe\",\"source\":\"integrity\",\"signal\":\"execution.integrity\",\"unit\":\"unit.condition\",\"minimum\":6,\"maximum\":10,\"blocker\":\"motion.unsafe\"}]"));
            }
            root.Get("activities")[0].Get("nodes")[0].Set("duration", Content.Json.Parse("{\"numerator\":2,\"denominator\":5,\"unit\":\"hour\"}"));
            edit?.Invoke(root); var text = new StringBuilder(); root.WriteCanonical(text);
            var retry = Content.Json.Parse(FrameworkProofContent.Read("work-retry.json"));
            retry.Set("work_retry_profiles", Content.Json.Parse(FrameworkProofContent.Read("haul-retry.json")).Get("work_retry_profiles"));
            return PointGame.Factory(text.ToString(), retryProfiles: retry.ToString(),
                flowDefinitions: FrameworkProofContent.Read("endpoints-flows.json"),
                extraModules: condition ? new GameModule[] { new EquipmentConditionModule() } : null,
                extraDeclarations: condition ? "[{\"id\":\"sim.equipment-condition\",\"version\":\"1.0.0\",\"documents\":[\"condition.json\"]}]" : null,
                extraDocuments: condition ? new Dictionary<string, string> { ["condition.json"] = FrameworkProofContent.Read("worker-execution-condition.json") } : null);
        }
        private static SimGame New(GameFactory factory, bool condition = false)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 617 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            request.Mods.Add(ResourceFlowsModule.ModuleId); if (condition) request.Mods.Add(EquipmentConditionModule.ModuleId); return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id)
        { foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException(id); }
        private static MotionExecutionView Read(SimGame game)
        { var code = game.Seat(0).View.Feature<MotionView>().ReadExecution(Find(game, "machine").Id, out var value); if (!code.IsNone) throw new InvalidOperationException(code.ToString()); return value; }
        private static void Control(Check c, SimGame game, bool enabled, Fix multiplier, string operation = "travel")
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = Find(game, "machine").Id, Operation = new Code(operation), Enabled = enabled, Target = multiplier }).Accepted, "player changes actual ordinary execution control");
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 200 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " timeout at " + game.Tick + ": " + Read(game).Status + "/" + Read(game).Blocker);
        }
        private static void Admission(Check c)
        {
            var factory = Factory(); var game = New(factory); var machine = Find(game, "machine"); ulong before = game.Hash(); var original = Read(game);
            c.Equal(Fix.One, original.Rates.Rate, "explicit native travel multiplier is independent of work rate");
            c.Equal(1, original.Rates.Providers.Count, "one named motion provider owns one control");
            c.Equal(Code.None, original.Blocker, "explicit empty condition set opens movement");
            c.Equal(before, game.Hash(), "motion diagnostics are pure");
            c.Throws<NotSupportedException>(() => ((IList<ExecutionRateSnapshot>)original.Rates.Providers).Clear(), "motion provider facts are immutable");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<MotionView>().ReadExecution(machine.Id, out var hidden), "foreign company cannot inspect motion control facts");
            c.That(hidden == null, "foreign projection returns no handle");
            c.Equal(Codes.InvalidArgument, game.Seat(0).View.Feature<MotionView>().ReadExecution(Find(game, "source").Id, out _), "non-mover refuses without fabricated geometry");
            c.That(!game.Seat(0).View.Feature<OperationsView>().PreviewConfigure(machine.Id, new Code("travel"), true, Fix.FromInt(3)).IsNone, "out-of-band control is refused by engine preview");
            c.Equal(before, game.Hash(), "preview refusal changes no path/control/job");
            Control(c, game, true, Fix.FromRatio(1, 2)); c.Equal(Fix.FromRatio(1, 2), Read(game).Rates.Rate, "ordinary native control changes multiplier");
            c.Equal(Fix.One, original.Rates.Rate, "retained projection is detached");
            Control(c, game, true, Fix.FromInt(2), "availability");
            c.Equal(Fix.FromRatio(1, 2), Read(game).Rates.Rate, "work-rate control never changes speed multiplier");
            c.Equal(Fix.FromInt(2), WorkerExecutionContracts.Read(game.World, machine).EffectiveRate, "shared refactor preserves independent actual work base/rate");
            Control(c, game, false, Fix.One); c.Equal(OperationCodes.Disabled, Read(game).Blocker, "disabled motion has original typed cause");
            c.Equal(new Code("point.blocked"), machine.State, "entity Update resolves authored disabled state");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "disabled motion control restores without OnCreate replay");
            Control(c, game, true, Fix.Zero); c.Equal(MotionExecutionCodes.ZeroRate, Read(game).Blocker, "zero multiplier reports distinct current cause");
            Control(c, game, false, Fix.One, "availability");
            c.Equal(OperationCodes.Disabled, Read(game).WorkerGates.Blocker, "motion facts include actual worker readiness without a second owner");
            var extended = New(Factory(extension: true)); var facts = Read(extended);
            c.Equal(2, facts.Rates.Providers.Count, "public independent provider composes without movement-system branches");
            c.Equal(Fix.FromRatio(1, 2), facts.Rates.Rate, "public ambient multiplier composes in the common rate reader");
            c.Equal("ambient", facts.Rates.Providers[0].Gate.Capability.Id, "named providers use stable identity order");
            c.Equal("travel", facts.Rates.Providers[1].Gate.Capability.Id, "native provider follows stable identity");
            for (int fault = 1; fault <= 5; fault++)
            {
                var hostile = New(Factory(extension: true, fault: fault));
                c.Throws<InvalidOperationException>(() => Read(hostile), "reader rejects hostile motion provider " + fault);
                c.Throws<InvalidOperationException>(() => hostile.Save(), "checkpoint rejects hostile motion provider " + fault);
            }
            foreach (string field in new[] { "operation", "conditions", "states" })
                c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[2].Get("components").Get("motion_execution").Remove(field))), "explicit authoring requires " + field);
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[2].Get("components").Get("motion_execution").Get("operation").Set("minimum", JsonValue.Number("-1")))), "negative speed band refuses before tick one");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[2].Get("components").Get("motion_execution").Get("operation").Set("unit", JsonValue.Of("unit.work-rate")))), "speed and work conventions are not silently interchangeable");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[0].Get("components").Set("motion_execution", Content.Json.Parse(FrameworkProofContent.Read("motion-execution.json"))))), "motion metadata requires an actual motion capability");
            var identity = New(Factory(edit: root => root.Get("entities")[2].Get("components").Remove("motion_execution")));
            c.Equal(Fix.One, Read(identity).Rates.Rate, "no motion provider contributes mathematical identity");
            c.Equal(0, Read(identity).Rates.Providers.Count, "no fake control/provider is created");
            var noWorker = New(Factory(edit: root => { var parts = root.Get("entities")[2].Get("components"); parts.Remove("worker"); parts.Remove("worker_execution"); parts.Remove("transporter"); }));
            c.Equal(0, Read(noWorker).WorkerGates.Gates.Count, "motion capability does not require a worker profile");
            Find(noWorker, "machine").Get<PointMotion>().GoTo(Fix.FromInt(10)); Tick(noWorker);
            c.Equal(Fix.One, Find(noWorker, "machine").Get<PointPresence>().Position, "actual non-worker motion advances through the same coordinator");
            var overflow = New(Factory(extension: true, edit: root =>
            {
                var parts = root.Get("entities")[2].Get("components"); parts.Get("proof_motion_execution").Set("multiplier", JsonValue.Number("2147483647"));
                parts.Get("motion_execution").Get("operation").Set("target", JsonValue.Number("2"));
            }));
            c.Throws<OverflowException>(() => Read(overflow), "unrepresentable composed speed faults instead of clamping");
            c.Throws<OverflowException>(() => overflow.Save(), "checkpoint rejects unrepresentable composition");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current identity captures direct callback removal and shared protocol");
            c.Equal(6, game.World.System<MovementSystem>().StateSchemaVersion, "movement system owns updated execution semantics");
            c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "work identity captures current travel and cargo admission");
            Codec(c); Condition(c);
        }
        private static void Codec(Check c)
        {
            var factory = Factory(); var game = New(factory); var machine = Find(game, "machine"); var part = machine.Get<MotionExecution>(); var before = part.ReadControl(); byte[] bytes;
            using (var stream = new MemoryStream()) { var writer = new StateWriter(stream); bool enabled = false; Fix target = Fix.FromInt(3); writer.Bool(ref enabled); writer.Fix(ref target); writer.Flush(); bytes = stream.ToArray(); }
            c.Throws<CorruptStateException>(() => part.Sync(new StateReader(bytes, 0, bytes.Length)), "shared native decode rejects out-of-band speed before assignment");
            c.Equal(before.Enabled, part.ReadControl().Enabled, "failed decode cannot partially disable motion");
            c.Equal(before.Target, part.ReadControl().Target, "failed decode cannot alter multiplier");
            c.Throws<CorruptStateException>(() => part.Sync(new StateReader(bytes, 0, 1)), "truncated decode stages all fields");
            c.Equal(before.Enabled, part.ReadControl().Enabled, "truncation preserves prior control");
            game.Seat(0).Submit(new ConfigureOperationCommand { Entity = machine.Id, Operation = new Code("travel"), Enabled = true, Target = Fix.FromRatio(3, 2) });
            var twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.Equal(Fix.FromRatio(3, 2), Read(game).Rates.Rate, "saved pending normal control applies exact fixed-point speed");
            c.Equal(game.Hash(), twin.Hash(), "pending speed command continues deterministically");
        }
        private static void Condition(Check c)
        {
            var factory = Factory(condition: true); var game = New(factory, true); var machine = Find(game, "machine"); var provider = machine.Get<IConditionProvider>(new Code("integrity"));
            var unit = ConditionContracts.Read(game.World, provider).Unit; var intact = Read(game);
            c.Equal(Fix.FromInt(10), intact.Rates.Gates.Gates[0].Conditions[0].Signal.Value, "motion gate reads real catalog-declared condition signal");
            ConditionContracts.Commit(game.World, provider, new ConditionChange(ConditionChangeKind.Damage, new UnitQuantity(unit, Fix.FromInt(6))));
            c.Equal(new Code("motion.unsafe"), Read(game).Blocker, "actual damage gates motion without mirroring condition");
            c.Equal(Code.None, WorkerExecutionContracts.Read(game.World, machine).Gates.Blocker, "motion condition never silently adds a work prerequisite");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "condition-bound motion readiness restores from actual owner state");
            ConditionContracts.Commit(game.World, provider, new ConditionChange(ConditionChangeKind.Repair, new UnitQuantity(unit, Fix.FromInt(6))));
            c.Equal(Code.None, Read(game).Blocker, "actual recovery opens movement without a cached gate ledger");
            c.Equal(Fix.FromInt(10), intact.Rates.Gates.Gates[0].Conditions[0].Signal.Value, "retained condition observation stays detached");
            c.Throws<ContentException>(() => New(Factory(condition: true, edit: root => root.Get("entities")[2].Get("components").Get("motion_execution").Get("conditions")[0].Set("source", JsonValue.Of("missing"))), true), "missing local signal binding refuses composition");
        }
        private static void Physical(Check c)
        { PointTravel(c); Cargo(c); Stationary(c); TileTravel(c); }
        private static void PointTravel(Check c)
        {
            var factory = Factory(extension: true); var game = New(factory); var machine = Find(game, "machine"); var motion = machine.Get<PointMotion>();
            Control(c, game, true, Fix.One);
            c.That(TestKit.Order(game, 0, new SetHomeCommand { Worker = machine.Id, Home = Find(game, "destination").Id }).Accepted,
                "ordinary home command owns actual point travel instead of unassigned direct motion");
            c.Equal(Fix.FromRatio(1, 2), machine.Get<PointPresence>().Position, "public ambient provider scales real point distance in normal movement phase");
            c.Equal(1L, motion.TicksMoving, "one engine movement phase advances provider once");
            Control(c, game, true, Fix.FromInt(2)); c.Equal(Fix.FromRatio(3, 2), machine.Get<PointPresence>().Position, "changed multiplier scales actual next distance, not a teleport");
            var position = machine.Get<PointPresence>().Position; long age = motion.TicksMoving;
            Control(c, game, true, Fix.Zero); for (int i = 0; i < 3; i++) Tick(game);
            c.Equal(position, machine.Get<PointPresence>().Position, "zero-speed wait grants no distance");
            c.Equal(age, motion.TicksMoving, "zero-speed wait grants no active movement update");
            c.Equal(MoveStatus.Moving, motion.Status, "home-bound wait retains real motion goal");
            var twin = factory.Load(game.Save()); Control(c, game, true, Fix.One); Control(c, twin, true, Fix.One);
            Control(c, game, false, Fix.One, "availability"); Control(c, twin, false, Fix.One, "availability"); position = machine.Get<PointPresence>().Position;
            for (int i = 0; i < 3; i++) { Tick(game); Tick(twin); }
            c.Equal(position, machine.Get<PointPresence>().Position, "actual worker execution blocks motion independently of speed control");
            Control(c, game, true, Fix.FromInt(2), "availability"); Control(c, twin, true, Fix.FromInt(2), "availability");
            Reach(game, () => machine.Get<PointPresence>().Position == Fix.FromInt(9), "saved point home travel", twin);
            c.Equal(Fix.FromInt(9), machine.Get<PointPresence>().Position, "actual provider reaches the engine-selected home interaction approach");
            c.Equal(game.Hash(), twin.Hash(), "combined public/native/worker movement resumes identically");
        }
        private static void Cargo(Check c)
        {
            var factory = Factory(); var game = New(factory); var isolated = New(factory); var machine = Find(game, "machine"); var board = game.World.Services.Get<WorkBoard>();
            var resource = game.World.Content.ResourceId("supply"); var haul = game.World.Services.Get<IHaulExecution>();
            Control(c, game, true, Fix.Zero); ulong before = game.Hash();
            c.Equal(0, haul.AcquireHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, resource, Fix.FromInt(8), 100), "travel-required acquisition excludes zero-speed carrier");
            c.Equal(before, game.Hash(), "blocked acquisition grants no claims or job identity");
            Control(c, game, true, Fix.FromRatio(1, 2)); int id = haul.AcquireHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, resource, Fix.FromInt(8), 100);
            c.That(id > 0, "restored motion control permits ordinary native haul commitment"); var job = board.Job(id);
            Reach(game, () => job.ToilIndex == 2 && machine.Get<PointPresence>().Position > Fix.Zero, "loaded slow departure");
            c.Equal(Fix.FromInt(8), job.Loaded, "motion multiplier never changes actual loading work rate");
            var position = machine.Get<PointPresence>().Position; var cargo = job.CargoOutReservation; var inlet = job.InReservation; var worker = job.Worker; long age = job.CreatedTick;
            Control(c, game, false, Fix.One); c.Equal(JobState.Paused, job.State, "motion unavailability uses existing retained haul wait");
            c.Equal(WorkSystem.BlockerWorkerUnavailable, job.Blocker, "travel readiness has canonical work retry classification");
            c.Equal(position, machine.Get<PointPresence>().Position, "disabled loaded vehicle waits at actual location");
            var twin = factory.Load(game.Save()); for (int i = 0; i < 5; i++) { Tick(game); Tick(twin); Tick(isolated); }
            c.Equal(cargo, job.CargoOutReservation, "saved motion waits retain cargo receipt"); c.Equal(inlet, job.InReservation, "destination capacity stays protected");
            c.Equal(worker, job.Worker, "same real vehicle remains assigned"); c.Equal(age, job.CreatedTick, "wait never resets original job age");
            c.Equal(Fix.FromInt(8), machine.Get<ITransporter>().Cargo.Amount(resource), "wait does not discard or teleport cargo");
            c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(resource), "remote wait delivers nothing instantly");
            Control(c, game, true, Fix.FromInt(2)); Control(c, twin, true, Fix.FromInt(2)); Reach(game, () => !job.IsLive, "saved real delivery", twin);
            c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IInletPort>().Amount(resource), "same cargo travels and unloads exactly once");
            c.Equal(Fix.FromInt(20), Find(game, "source").Get<IOutletPort>().Amount(resource) + machine.Get<ITransporter>().Cargo.Amount(resource) + Find(game, "destination").Get<IInletPort>().Amount(resource), "native quantities are conserved across speed changes");
            c.Equal(game.Hash(), twin.Hash(), "loaded motion/control/retry continuation hashes match");
            c.Equal(Fix.Zero, Find(isolated, "machine").Get<ITransporter>().Cargo.Amount(resource), "interleaved session receives no cargo");
        }
        private static void Stationary(Check c)
        {
            var factory = Factory(); var game = New(factory); Control(c, game, false, Fix.One);
            var result = TestKit.Order(game, 0, new PostOrderCommand { Target = Find(game, "source").Id, Plan = "inspect" }); c.That(result.Accepted, "player posts ordinary stationary work");
            var order = game.World.Services.Get<WorkBoard>().Order(checked((int)result.Arg0)); var twin = factory.Load(game.Save());
            Reach(game, () => !order.IsActive, "stationary work with disabled locomotion", twin);
            c.Equal(OrderState.Done, order.State, "no-travel work completes despite independent disabled motion control");
            c.Equal(Fix.Zero, Find(game, "machine").Get<PointPresence>().Position, "stationary work earns no movement");
            c.Equal(game.Hash(), twin.Hash(), "stationary work and independent motion policy restore identically");
        }
        private static void TileTravel(Check c)
        {
            var root = Content.Json.Parse(Colony.Defs); JsonValue prototype = null;
            for (int i = 0; i < root.Get("entities").Count; i++) if (root.Get("entities")[i].Get("id").Text == "truck") prototype = root.Get("entities")[i].Clone();
            if (prototype == null) throw new InvalidOperationException("missing tile truck prototype"); prototype.Set("id", JsonValue.Of("motion.tile"));
            var execution = Content.Json.Parse(FrameworkProofContent.Read("motion-execution.json")); execution.Get("states").Set("disabled", JsonValue.Of("stopped"));
            prototype.Get("components").Set("motion_execution", execution); var extra = JsonValue.NewObject(); var entities = JsonValue.NewArray(); entities.Add(prototype); extra.Set("entities", entities);
            var text = new StringBuilder(); extra.WriteCanonical(text); var factory = Colony.Factory(additionalDocuments: new Dictionary<string, string> { ["motion-tile.json"] = text.ToString() });
            var game = ResourceDispositionTests.New(factory); var machine = Spawn(game, "motion.tile", 8, 24); var mover = machine.Get<Mover>();
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = machine.Id, To = new TileCoord(16, 24) }).Accepted,
                "ordinary manual movement command owns the native tile route");
            var position = machine.Get<ITilePresence>().Position;
            c.That(position != FixVec2.Centre(new TileCoord(8, 24)), "native tile mover advances actual route through the same coordinate-neutral coordinator");
            c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = machine.Id, Operation = new Code("travel"), Enabled = false, Target = Fix.One }).Accepted, "ordinary control disables tile movement");
            for (int i = 0; i < 3; i++) Tick(game); c.Equal(position, machine.Get<ITilePresence>().Position, "disabled tile movement retains actual position");
            var twin = factory.Load(game.Save()); var savedMachine = twin.World.Entities.Get(machine.Id);
            foreach (var current in new[] { game, twin }) c.That(TestKit.Order(current, 0, new ConfigureOperationCommand { Entity = machine.Id, Operation = new Code("travel"), Enabled = true, Target = Fix.FromRatio(1, 2) }).Accepted, "saved tile motion reopens through ordinary controls");
            for (int i = 0; i < 200 && mover.Status != MoveStatus.Arrived; i++) { Tick(game); Tick(twin); }
            c.Equal(MoveStatus.Arrived, mover.Status, "actual tile route completes under authored multiplier");
            c.Equal(machine.Get<ITilePresence>().Position, savedMachine.Get<ITilePresence>().Position, "saved tile provider reaches the same coordinate");
            c.Equal(game.Hash(), twin.Hash(), "native tile route/path/controls continue identically");
        }
    }
}
