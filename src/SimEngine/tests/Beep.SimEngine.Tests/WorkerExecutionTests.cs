using System;
using System.Collections.Generic;
using System.IO;
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
    // Authored fixtures, player inputs and observations only; no dispatch/progress/transfer decisions.
    internal static partial class WorkerExecutionTests
    {
        internal static void Register()
        {
            TestRunner.Add("Execution gates: owned immutable publication and bounded allocation costs", Publication);
            TestRunner.Add("Worker execution: authored contracts, controls and immutable admission", Admission);
            TestRunner.Add("Worker execution: physical work, cargo waits and saved continuation", Physical);
        }
        private static GameFactory Factory(Action<JsonValue> edit = null, bool extension = false, int fault = 0, bool condition = false)
        {
            var root = Content.Json.Parse(PointGame.Definitions);
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            var parts = root.Get("entities")[2].Get("components");
            parts.Get("worker").Set("work_rate", JsonValue.Number("1"));
            parts.Get("point_motion").Set("rate", JsonValue.Number("60"));
            parts.Set("worker_execution", Content.Json.Parse(FrameworkProofContent.Read("worker-execution.json")));
            if (extension) parts.Set("proof_worker_execution", Content.Json.Parse("{\"id\":\"job-gate\",\"operation\":\"availability\",\"job_required_at\":2,\"multiplier\":0.5,\"fault\":" + fault + "}"));
            if (condition)
            {
                parts.Set("equipment_condition", Content.Json.Parse("{\"id\":\"integrity\",\"unit\":\"unit.condition\",\"maximum\":10,\"initial\":10,\"wear\":null,\"states\":{\"damaged\":\"blocked\",\"failed\":\"blocked\"}}"));
                parts.Get("worker_execution").Set("conditions", Content.Json.Parse("[{\"id\":\"safe\",\"source\":\"integrity\",\"signal\":\"execution.integrity\",\"unit\":\"unit.condition\",\"minimum\":6,\"maximum\":10,\"blocker\":\"execution.unsafe\"}]"));
            }
            root.Get("activities")[0].Get("nodes")[0].Set("duration", Content.Json.Parse("{\"numerator\":2,\"denominator\":5,\"unit\":\"hour\"}"));
            edit?.Invoke(root); var text = new StringBuilder(); root.WriteCanonical(text);
            var retry = Content.Json.Parse(FrameworkProofContent.Read("work-retry.json"));
            retry.Set("work_retry_profiles", Content.Json.Parse(FrameworkProofContent.Read("haul-retry.json")).Get("work_retry_profiles"));
            // The prepared input is the explicit points.json document in PointGame's master manifest.
            return PointGame.Factory(text.ToString(), retryProfiles: retry.ToString(),
                flowDefinitions: FrameworkProofContent.Read("endpoints-flows.json"),
                extraModules: condition ? new GameModule[] { new EquipmentConditionModule() } : null,
                extraDeclarations: condition ? "[{\"id\":\"sim.equipment-condition\",\"version\":\"1.0.0\",\"documents\":[\"execution-condition.json\"]}]" : null,
                extraDocuments: condition ? new Dictionary<string, string> { ["execution-condition.json"] = FrameworkProofContent.Read("worker-execution-condition.json") } : null);
        }
        private static SimGame New(GameFactory factory, bool condition = false)
        { var request = new NewGameRequest { Scenario = "points", Seed = 613 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); request.Mods.Add(ResourceFlowsModule.ModuleId); if (condition) request.Mods.Add(EquipmentConditionModule.ModuleId); return factory.NewGame(request); }
        private static Entity Find(SimGame game, string id)
        { foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException("missing execution entity " + id); }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static WorkerExecutionView Read(SimGame game)
        { var code = game.Seat(0).View.Feature<WorkView>().ReadWorkerExecution(Find(game, "machine").Id, out var result); if (!code.IsNone) throw new InvalidOperationException(code.ToString()); return result; }
        private static void Control(Check c, SimGame game, bool enabled, Fix multiplier)
            => c.That(WorkerExecutionTestContent.Control(game, Find(game, "machine").Id, enabled, multiplier).Accepted, "ordinary engine operation command changes the sole execution control");
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 160 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (done()) return;
            var facts = new List<string>(); foreach (var job in Board(game).Jobs) facts.Add(job.Id + "/" + job.State + "/toil=" + job.ToilIndex + "/loaded=" + job.Loaded + "/" + job.Blocker);
            throw new InvalidOperationException(stage + " timeout at tick " + game.Tick + ": " + string.Join(";", facts));
        }
        private static void Admission(Check c)
        {
            var factory = Factory(); var game = New(factory); var worker = Find(game, "machine");
            ulong before = game.Hash(); var original = Read(game);
            c.Equal(Fix.One, original.EffectiveRate, "actual authored base rate uses explicit native multiplier");
            c.Equal(1, original.Providers.Count, "one execution capability owns one control");
            c.Equal("availability", original.Providers[0].Gate.Capability.Id, "facts carry actual named metadata");
            c.Equal(Code.None, original.Gates.Blocker, "explicit empty condition set is ready");
            c.Equal(before, game.Hash(), "execution diagnostics are pure");
            c.Throws<NotSupportedException>(() => ((IList<ExecutionRateSnapshot>)original.Providers).Clear(), "provider facts cannot be modified by a host");
            c.Throws<NotSupportedException>(() => ((IList<ExecutionGateSnapshot>)original.Gates.Gates).Clear(), "aggregate gate facts are immutable");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<WorkView>().ReadWorkerExecution(worker.Id, out var hidden), "foreign company cannot read worker controls or blockers");
            c.That(hidden == null, "foreign projection leaks no provider handles");
            c.Equal(Codes.InvalidArgument, game.Seat(0).View.Feature<WorkView>().ReadWorkerExecution(Find(game, "source").Id, out _), "non-worker refuses rather than inventing a base rate");
            var view = game.Seat(0).View.Feature<OperationsView>();
            c.That(!view.PreviewConfigure(worker.Id, new Code("availability"), true, Fix.FromInt(3)).IsNone, "out-of-band multiplier refuses in engine preview");
            c.Equal(before, game.Hash(), "refused preview changes no control or claims");
            Control(c, game, true, Fix.FromInt(2));
            c.Equal(Fix.FromInt(2), Read(game).EffectiveRate, "ordinary operation target changes effective rate");
            c.Equal(Fix.One, original.EffectiveRate, "retained earlier rate is detached");
            c.Equal(factory.Load(game.Save()).Hash(), game.Hash(), "native control and current system identities restore exactly");
            Control(c, game, false, Fix.One);
            c.Equal(OperationCodes.Disabled, Read(game).Gates.Blocker, "disabled control has typed original blocker");
            c.Equal(new Code("point.blocked"), worker.State, "entity Update commits authored disabled state");
            var offers = new List<WorkWorkerCandidate>();
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("inspect", "check", Find(game, "source").Id, offers), "same native selector serves policy preview");
            c.That(offers.Count == 1 && offers[0].Result.Blocker == WorkSystem.BlockerWorkerUnavailable, "dispatch uses canonical unavailable classification");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "disabled capability saves without re-enabling OnCreate");
            var extended = New(Factory(extension: true)); var extensionFacts = Read(extended);
            c.Equal(2, extensionFacts.Providers.Count, "public independent provider participates without scheduler changes");
            c.Equal(Fix.FromRatio(1, 2), extensionFacts.EffectiveRate, "multiple providers compose with sole base rate");
            c.Equal("availability", extensionFacts.Providers[0].Gate.Capability.Id, "provider ordering is stable identity order");
            c.Equal("job-gate", extensionFacts.Providers[1].Gate.Capability.Id, "extension provider follows stable identity");
            c.Equal(extended.Hash(), Factory(extension: true).Load(extended.Save()).Hash(), "public stateless provider survives current manifest restore");
            for (int fault = 1; fault <= 5; fault++)
            {
                var hostile = New(Factory(extension: true, fault: fault));
                c.Throws<InvalidOperationException>(() => Read(hostile), "pure read rejects hostile execution provider " + fault);
                c.Throws<InvalidOperationException>(() => hostile.Save(), "checkpoint rejects hostile execution provider " + fault);
            }
            foreach (string field in new[] { "operation", "conditions", "states" })
                c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[2].Get("components").Get("worker_execution").Remove(field))), "required authoring refuses absent " + field);
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[2].Get("components").Get("worker_execution").Get("operation").Set("minimum", JsonValue.Number("-1")))), "negative authored execution band refuses");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[2].Get("components").Get("worker_execution").Get("operation").Set("unit", JsonValue.Of("unit.other")))), "undeclared multiplier convention refuses");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[2].Get("components").Get("worker_execution").Get("states").Set("disabled", JsonValue.Of("missing")))), "missing authored entity state refuses");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("entities")[0].Get("components").Set("worker_execution", Content.Json.Parse(FrameworkProofContent.Read("worker-execution.json"))))), "execution metadata cannot bind a non-worker");
            var noProvider = New(Factory(edit: root => root.Get("entities")[2].Get("components").Remove("worker_execution")));
            c.Equal(Fix.One, Read(noProvider).EffectiveRate, "no provider means mathematical identity, not another base owner");
            c.Equal(0, Read(noProvider).Providers.Count, "no fictitious provider is created");
            var overflowing = New(Factory(edit: root =>
            {
                var parts = root.Get("entities")[2].Get("components"); parts.Get("worker").Set("work_rate", JsonValue.Number("2147483647"));
                parts.Get("worker_execution").Get("operation").Set("target", JsonValue.Number("2"));
            }));
            c.Throws<OverflowException>(() => Read(overflowing), "unrepresentable effective rate faults explicitly rather than clamping");
            var firstFault = overflowing.Fault;
            c.That(firstFault?.Message.StartsWith("OverflowException:", StringComparison.Ordinal) == true, "original rate overflow is retained by the first-fault owner");
            c.Throws<InvalidOperationException>(() => overflowing.Save(), "checkpoint refuses an already faulted rate observation");
            c.That(ReferenceEquals(firstFault, overflowing.Fault), "checkpoint refusal cannot replace the original overflow fault");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current build identifies removed worker callback paths");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "existing save envelope remains current with exact codec identities");
            c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "work coordinator identity describes current readiness and cargo semantics");
            Codec(c); Conditions(c);
        }
        private static void Codec(Check c)
        {
            var factory = Factory(); var game = New(factory); var machine = Find(game, "machine"); var part = machine.Get<WorkerExecution>();
            var control = part.ReadControl(); byte[] invalid;
            using (var stream = new MemoryStream())
            { var writer = new StateWriter(stream); bool enabled = false; Fix target = Fix.FromInt(3); writer.Bool(ref enabled); writer.Fix(ref target); writer.Flush(); invalid = stream.ToArray(); }
            c.Throws<CorruptStateException>(() => part.Sync(new StateReader(invalid, 0, invalid.Length)), "out-of-band saved multiplier is rejected before assignment");
            c.Equal(control.Enabled, part.ReadControl().Enabled, "failed staged decode leaves enabled owner untouched");
            c.Equal(control.Target, part.ReadControl().Target, "failed staged decode leaves multiplier owner untouched");
            c.Throws<CorruptStateException>(() => part.Sync(new StateReader(invalid, 0, 1)), "truncated payload is rejected before assignment");
            c.Equal(control.Enabled, part.ReadControl().Enabled, "truncated decode cannot partially disable the capability");
            game.Seat(0).Submit(new ConfigureOperationCommand { Entity = machine.Id, Operation = new Code("availability"), Enabled = true, Target = Fix.FromRatio(3, 2) });
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending ordinary control command saves identically"); Tick(game); Tick(twin);
            c.Equal(Fix.FromRatio(3, 2), Read(game).EffectiveRate, "saved pending command applies exact native fixed-point multiplier");
            c.Equal(game.Hash(), twin.Hash(), "pending command continuation is deterministic");
        }
        private static void Conditions(Check c)
        {
            var factory = Factory(condition: true); var game = New(factory, true); var machine = Find(game, "machine");
            var intact = Read(game); c.Equal(1, intact.Gates.Gates[0].Conditions.Count, "typed condition facts come from actual named signal provider");
            c.Equal(Fix.FromInt(10), intact.Gates.Gates[0].Conditions[0].Signal.Value, "ready facts report real authored equipment integrity");
            var provider = machine.Get<IConditionProvider>(new Code("integrity")); var unit = ConditionContracts.Read(game.World, provider).Unit;
            c.Equal(Code.None, ConditionContracts.Commit(game.World, provider, new ConditionChange(ConditionChangeKind.Damage, new UnitQuantity(unit, Fix.FromInt(6)))), "existing engine condition contract alone owns damage mutation");
            var damaged = Read(game); c.Equal(new Code("execution.unsafe"), damaged.Gates.Blocker, "unsafe integrity blocks readiness with original typed cause");
            c.Equal(Fix.FromInt(4), damaged.Gates.Gates[0].Conditions[0].Signal.Value, "readiness does not mirror condition values");
            Tick(game); c.Equal(new Code("point.blocked"), machine.State, "normal entity Update resolves condition-driven state");
            ulong before = game.Hash(); var hauling = game.World.Services.Get<IHaulExecution>(); var resource = game.World.Content.ResourceId("supply");
            c.Equal(0, hauling.AcquireHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, resource, Fix.One, 100), "unsafe carrier cannot reserve a new physical job");
            c.Equal(before, game.Hash(), "refused condition-driven acquisition allocates no receipts or jobs");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "unsafe signal-bound readiness restores from actual owner state");
            c.Equal(Code.None, ConditionContracts.Commit(game.World, provider, new ConditionChange(ConditionChangeKind.Repair, new UnitQuantity(unit, Fix.FromInt(6)))), "existing condition provider alone owns repair mutation");
            var twinProvider = Find(twin, "machine").Get<IConditionProvider>(new Code("integrity"));
            ConditionContracts.Commit(twin.World, twinProvider, new ConditionChange(ConditionChangeKind.Repair, new UnitQuantity(unit, Fix.FromInt(6))));
            Tick(game); Tick(twin); c.Equal(Code.None, Read(game).Gates.Blocker, "repaired real condition reopens readiness without cached gate state");
            c.Equal(game.Hash(), twin.Hash(), "condition-driven readiness continues identically after save/load");
            c.Equal(Fix.FromInt(10), intact.Gates.Gates[0].Conditions[0].Signal.Value, "previous signal observation stays detached");
            c.Equal(Fix.FromInt(4), damaged.Gates.Gates[0].Conditions[0].Signal.Value, "retained damaged facts stay detached after repair");
            c.Throws<ContentException>(() => New(Factory(condition: true, edit: root => root.Get("entities")[2].Get("components").Get("worker_execution").Get("conditions")[0].Set("source", JsonValue.Of("missing"))), true), "missing local signal binding fails before tick one");
            c.Throws<ContentException>(() => New(Factory(condition: true, edit: root => root.Get("entities")[2].Get("components").Get("worker_execution").Get("conditions")[0].Set("signal", JsonValue.Of("missing"))), true), "unknown signal has no substitute");
        }
        private static void Physical(Check c)
        { TimedWork(c); LoadedCargo(c); FreeMovement(c); }
        private static void TimedWork(Check c)
        {
            var factory = Factory(); var game = New(factory);
            var result = TestKit.Order(game, 0, new PostOrderCommand { Target = Find(game, "source").Id, Plan = "inspect" });
            c.That(result.Accepted, "player posts a native authored activity"); var order = Board(game).Order(checked((int)result.Arg0)); var node = order.Node("check");
            Reach(game, () => node.Progress.WholeTicks > 0, "physical work start");
            var progress = node.Progress; Control(c, game, true, Fix.Zero);
            for (int i = 0; i < 4; i++) Tick(game);
            c.Equal(progress, node.Progress, "zero multiplier earns no progress over game time");
            var twin = factory.Load(game.Save()); Control(c, game, true, Fix.FromInt(2)); Control(c, twin, true, Fix.FromInt(2));
            c.Equal(game.Hash(), twin.Hash(), "saved zero-rate state resumes through the same player commands");
            // Zero-rate work reopens through the original authored retry delay; enabling
            // cannot skip reacquisition/travel and instantly resume productive time.
            Reach(game, () => node.Progress.WholeTicks > progress.WholeTicks, "physical work retry after re-enable", twin);
            progress = node.Progress; Tick(game); Tick(twin);
            c.Equal(checked(progress.WholeTicks + 2), node.Progress.WholeTicks, "positive multiplier earns exactly two work ticks, not instant completion");
            Control(c, game, false, Fix.One); Control(c, twin, false, Fix.One); progress = node.Progress;
            for (int i = 0; i < 3; i++) { Tick(game); Tick(twin); }
            c.Equal(progress, node.Progress, "unavailable machine retains earned ordinary-work progress");
            c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "ordinary work uses existing reopen semantics, not a second dispatcher");
            c.That(order.IsActive, "disabled worker never fabricates activity completion");
            c.Equal(game.Hash(), twin.Hash(), "interleaved saved work waits remain identical");
            twin = factory.Load(game.Save()); Control(c, game, true, Fix.One); Control(c, twin, true, Fix.One);
            Reach(game, () => !order.IsActive, "recovered timed activity", twin);
            c.Equal(OrderState.Done, order.State, "same work order finishes through physical execution");
            c.Equal(game.Hash(), twin.Hash(), "save/load/continue retains real work decisions and hashes");
        }
        private static void LoadedCargo(Check c)
        {
            var factory = Factory(); var game = New(factory); var isolated = New(factory);
            var hauling = game.World.Services.Get<IHaulExecution>(); var resource = game.World.Content.ResourceId("supply");
            var source = Find(game, "source"); var destination = Find(game, "destination"); var machine = Find(game, "machine");
            int id = hauling.AcquireHaul(new PlayerId(0), source.Id, destination.Id, resource, Fix.FromInt(8), 100);
            c.That(id > 0, "public engine haul execution reserves actual native endpoints and carrier"); var job = Board(game).Job(id);
            Tick(game); Tick(game); var partial = job.Loaded; var progress = job.HandlingProgress;
            c.Equal(Fix.FromInt(2), partial, "two elapsed handling updates load two actual units");
            Control(c, game, false, Fix.One);
            c.Equal(JobState.Paused, job.State, "unavailable loaded carrier waits in the existing job");
            c.Equal(WorkSystem.BlockerWorkerUnavailable, job.Blocker, "same retry profile receives canonical unavailability");
            c.Equal(partial, job.Loaded, "disable preserves partially loaded cargo");
            c.Equal(progress, job.HandlingProgress, "unavailable worker earns no loading progress");
            var cargoHold = job.CargoOutReservation; var sourceHold = job.OutReservation; var destinationHold = job.InReservation; var carrier = job.Worker; long age = job.CreatedTick;
            var twin = factory.Load(game.Save()); for (int i = 0; i < 5; i++) { Tick(game); Tick(twin); Tick(isolated); }
            c.Equal(game.Hash(), twin.Hash(), "partial cargo, holds and backoff save identically");
            c.Equal(partial, machine.Get<ITransporter>().Cargo.Amount(resource), "physical wait leaves native cargo intact");
            c.Equal(sourceHold, job.OutReservation, "partial pickup wait retains the remaining source receipt");
            Control(c, game, true, Fix.One); Control(c, twin, true, Fix.One);
            Reach(game, () => job.ToilIndex == 2 && machine.Get<PointPresence>().Position > Fix.Zero, "loaded physical departure", twin);
            c.Equal(Fix.FromInt(8), job.Loaded, "all loading happens before delivery travel");
            var position = machine.Get<PointPresence>().Position; Control(c, game, false, Fix.One); Control(c, twin, false, Fix.One);
            c.Equal(position, machine.Get<PointPresence>().Position, "unavailable carrier stops at its actual position");
            for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); }
            c.Equal(position, machine.Get<PointPresence>().Position, "retry waiting never teleports the loaded carrier");
            c.Equal(cargoHold, job.CargoOutReservation, "partial and full loaded waits retain the same cargo receipt");
            c.That(!job.OutReservation.IsValid, "fully earned pickup releases the exhausted source receipt");
            c.Equal(destinationHold, job.InReservation, "destination promise stays attached to the same job");
            c.Equal(carrier, job.Worker, "no replacement transporter is fabricated");
            c.Equal(age, job.CreatedTick, "retry never resets original age");
            c.Equal(Fix.Zero, destination.Get<IInletPort>().Amount(resource), "no stock arrives while the carrier waits remotely");
            twin = factory.Load(game.Save()); Control(c, game, true, Fix.One); Control(c, twin, true, Fix.One);
            Reach(game, () => job.ToilIndex == 3 && job.HandlingProgress.WholeTicks >= 2, "timed unloading", twin);
            partial = job.Loaded; progress = job.HandlingProgress; Control(c, game, true, Fix.Zero); Control(c, twin, true, Fix.Zero);
            c.Equal(partial, job.Loaded, "zero handling rate cannot unload unearned quantities");
            c.Equal(progress, job.HandlingProgress, "zero rate cannot grant unload work");
            Control(c, game, true, Fix.One); Control(c, twin, true, Fix.One); Reach(game, () => !job.IsLive, "saved cargo delivery", twin);
            c.Equal(Fix.FromInt(8), destination.Get<IInletPort>().Amount(resource), "retained cargo delivers exactly once");
            c.Equal(Fix.FromInt(20), source.Get<IOutletPort>().Amount(resource) + machine.Get<ITransporter>().Cargo.Amount(resource) + destination.Get<IInletPort>().Amount(resource), "all interruptions conserve actual native stock");
            c.Equal(game.Hash(), twin.Hash(), "physical delivery and saved continuation remain deterministic");
            c.Equal(Fix.Zero, Find(isolated, "machine").Get<ITransporter>().Cargo.Amount(resource), "interleaved independent session receives no cargo or worker controls");
        }
        private static void FreeMovement(Check c)
        {
            var factory = Factory(); var game = New(factory); var machine = Find(game, "machine"); var motion = machine.Get<IMotion>();
            var destination = Find(game, "destination");
            c.That(TestKit.Order(game, 0, new SetHomeCommand { Worker = machine.Id, Home = destination.Id }).Accepted,
                "player home intent owns idle physical travel, not an unassigned test-injected route");
            var position = machine.Get<PointPresence>().Position; c.That(position > Fix.Zero, "actual motion owner advances through normal engine Update");
            Control(c, game, false, Fix.One); for (int i = 0; i < 3; i++) Tick(game);
            c.Equal(position, machine.Get<PointPresence>().Position, "readiness gates unassigned movement as well as work jobs");
            c.Equal(MoveStatus.Moving, motion.Status, "unassigned readiness wait retains existing goal, not a second route");
            var twin = factory.Load(game.Save()); Control(c, game, true, Fix.One); Control(c, twin, true, Fix.One);
            bool AtHome()
            {
                Code result = game.Seat(0).View.Feature<WorkView>().ReadHome(machine.Id, out var home);
                if (!result.IsNone) throw new InvalidOperationException("worker home observation refused: " + result);
                return home.State == WorkerHomeState.AtHome;
            }
            Reach(game, AtHome, "saved player-owned home travel", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved motion readiness resumes through the actual provider");
        }
    }
}
