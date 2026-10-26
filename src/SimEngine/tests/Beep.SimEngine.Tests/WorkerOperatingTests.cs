using System;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Same exact two phase-input suites; fixtures author content and simulate ordinary player actions.
    internal static partial class OperatingInputsTests
    {
        private static GameFactory WorkerFactory(Action<JsonValue> edit = null)
        {
            var document = Json.Parse(FrameworkProofContent.Read("worker-operating-inputs.json")); edit?.Invoke(document);
            var root = Json.Parse(PointGame.Definitions); root.Remove("entities"); StartupContentAuthoring.Clear(root);
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            string Text(JsonValue value) { var output = new StringBuilder(); value.WriteCanonical(output); return output.ToString(); }
            return PointGame.Factory(Text(root), additionalDocuments: new Dictionary<string, string> { ["worker-operating-inputs.json"] = Text(document) }, startupProfiles: new[] { "worker-operating-inputs.start" });
        }
        private static OperatingInputEvaluation WorkerInputs(SimGame game, string worker, OperatingPhase phase)
        {
            Code refusal = game.Seat(0).View.Feature<WorkView>().ReadOperatingInputs(Find(game, worker).Id, phase, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("worker input facts refused: " + refusal); return value;
        }
        private static long Exposure(SimGame game, string worker, OperatingPhase phase)
            => WorkerInputs(game, worker, phase).Providers[0].Exposure.ElapsedTicks;
        private static Fix WorkerStock(SimGame game, string worker)
            => Find(game, worker).Get<IInventory>().Amount(game.World.Content.ResourceId("supply"));
        private static void WorkerStep(SimGame game, SimGame twin = null)
        { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("worker inputs fault: " + game.Fault + "/" + twin?.Fault); }
        private static void WorkerUntil(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) WorkerStep(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " timeout: game time=" + game.Clock.TotalGameTime +
                "; a=" + WorkerStock(game, "machine.a") + "; b=" + WorkerStock(game, "machine.b") +
                "; c=" + WorkerStock(game, "machine.c") + "; jobs=" + game.World.Services.Get<WorkBoard>().Jobs.Count);
        }
        private static void WorkerPolicy(Check c, SimGame game, string worker, WorkAssignmentMode mode, bool enabled = true, bool haulOnly = false)
        {
            var entity = Find(game, worker); var current = WorkPolicyRules.Read(entity);
            var settings = new WorkPolicySettings(enabled, current.Priority,
                haulOnly ? new[] { new WorkerRoleId("haul") } : current.AllowedRoles, current.Area, current.Interruptible, current.EmergencyEligible, mode);
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = entity.Id, Settings = settings }).Accepted, "ordinary command edits actual work policy");
        }
        private static Fix Paid(SimGame game)
        {
            Fix total = Fix.Zero; ResourceId supply = game.World.Content.ResourceId("supply");
            foreach (var id in new[] { "machine.a", "machine.b", "machine.c" })
                foreach (var phase in new[] { OperatingPhase.Movement, OperatingPhase.Idle, OperatingPhase.Standby })
                    foreach (var fact in OperatingInputsContracts.Read(game.World, Find(game, id), phase).Providers)
                        total += fact.Demand.Consumed.AmountOf(supply);
            return total;
        }
        private static Fix PhysicalStock(SimGame game)
        {
            Fix total = Fix.Zero; ResourceId supply = game.World.Content.ResourceId("supply");
            foreach (var entity in game.World.Entities.All)
                if (entity.Alive && entity.Get<IInletPort>() != null) total += entity.Get<IInletPort>().Amount(supply);
            return total;
        }
        private static void WorkerAdmission(Check c)
        {
            var factory = WorkerFactory(); var game = New(factory); var a = Find(game, "machine.a"); var b = Find(game, "machine.b");
            c.That(a.Get<IOperatingInputs>(new Code("idle")) is OperatingInputs && b.Get<IOperatingInputs>(new Code("idle")) is ProofOperatingInputs,
                "native and unrelated public phase providers alias actual utilities");
            c.That(b.Get<IWorker>() is ContractWorker, "idle phases do not require the built-in worker");
            var retained = WorkerInputs(game, "machine.a", OperatingPhase.Idle); ulong original = game.Hash();
            c.Equal(1, retained.Providers.Count, "idle excludes movement and standby sources");
            c.Equal(0L, retained.Providers[0].Exposure.ElapsedTicks, "startup invents no accepted idle interval");
            c.Equal(Fix.One, retained.RequiredInputs.AmountOf(game.World.Content.ResourceId("supply")), "exact cumulative ceiling requests one native quantum");
            c.Equal(1, WorkerInputs(game, "machine.b", OperatingPhase.Standby).Providers.Count, "public standby phase is independently declared");
            c.Equal(OperatingInputCodes.Inputs, WorkerInputs(game, "machine.c", OperatingPhase.Idle).Blocker, "unfunded phase reports real shortage");
            c.Equal(original, game.Hash(), "all phase and private observations are pure");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<WorkView>().ReadOperatingInputs(a.Id, OperatingPhase.Idle, out var hidden), "foreign input facts are private");
            c.That(hidden == null, "foreign refusal returns no partial resource facts");
            c.Equal(Codes.NotFound, PlayerView.Create(game.World, new PlayerId(9)).Feature<WorkView>().ReadOperatingInputs(a.Id, OperatingPhase.Idle, out _), "unseated viewer cannot observe a company");
            c.Equal(Codes.InvalidArgument, game.Seat(0).View.Feature<WorkView>().ReadOperatingInputs(a.Id, OperatingPhase.Movement, out _), "work view refuses another phase surface");
            c.Equal(StandardCodes.NotWorker, game.Seat(0).View.Feature<WorkView>().ReadOperatingInputs(Find(game, "source").Id, OperatingPhase.Idle, out _), "endpoint is not a substitute worker");
            c.Throws<NotSupportedException>(() => ((IList<OperatingUtilitySnapshot>)retained.Providers).Clear(), "phase facts are detached immutable values");
            WorkerStep(game);
            c.Equal(1L, Exposure(game, "machine.a", OperatingPhase.Idle), "native idle phase earns one accepted interval");
            c.Equal(1L, Exposure(game, "machine.b", OperatingPhase.Idle), "public idle phase follows identical chronology");
            c.Equal(Fix.FromInt(7), WorkerStock(game, "machine.a"), "native idle pays physical fuel");
            c.Equal(Fix.FromInt(7), WorkerStock(game, "machine.b"), "public idle pays physical fuel once");
            c.Equal(0L, Exposure(game, "machine.c", OperatingPhase.Idle), "shortage never earns free exposure");
            c.Equal(0L, retained.Providers[0].Exposure.ElapsedTicks, "old observation does not follow execution");
            c.Equal(0L, Exposure(game, "machine.a", OperatingPhase.Standby), "inactive phase is not advanced");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "all named exposures restore through their sole providers");
            c.That(ReferenceEquals(game.World.Clock, game.Clock), "phase exposure uses the actual single game clock");

            var combinedFactory = WorkerFactory(doc =>
            {
                var parts = doc.Get("entities")[2].Get("components"); parts.Get("storage").Set("initial_stock", Json.Parse("{\"supply\":1}"));
                var extra = doc.Get("entities")[3].Get("components").Get("proof_operating_inputs")[0].Clone(); extra.Set("id", JsonValue.Of("auxiliary"));
                parts.Set("proof_operating_inputs", extra);
            });
            var combined = New(combinedFactory); var vector = WorkerInputs(combined, "machine.a", OperatingPhase.Idle);
            c.Equal(2, vector.Providers.Count, "native/public idle sources share one aggregate admission");
            c.Equal(OperatingInputCodes.Inputs, vector.Blocker, "two individually affordable draws cannot spend one physical unit twice");
            WorkerStep(combined);
            c.Equal(Fix.One, WorkerStock(combined, "machine.a"), "aggregate shortage causes no partial burn");
            foreach (var value in WorkerInputs(combined, "machine.a", OperatingPhase.Idle).Providers) c.Equal(0L, value.Exposure.ElapsedTicks, "no unfunded participant is advanced");
            foreach (string field in new[] { "id", "phase", "inputs", "states" })
                c.Throws<ContentException>(() => New(WorkerFactory(doc => doc.Get("entities")[2].Get("components").Get("operating_inputs")[0].Remove(field))), "required native worker field refuses: " + field);
            c.Throws<ContentException>(() => New(WorkerFactory(doc => doc.Get("entities")[2].Get("components").Get("operating_inputs")[0].Set("phase", JsonValue.Of("legacy")))), "unknown operating phase has no fallback");
            c.Throws<ContentException>(() => New(WorkerFactory(doc =>
            { var parts = doc.Get("entities")[2].Get("components"); parts.Remove("worker"); parts.Remove("work_policy"); })), "idle inputs require an actual worker contract");
            c.Throws<ContentException>(() => New(WorkerFactory(doc => doc.Get("entities")[2].Get("components").Remove("storage"))), "no implicit inventory for workers");
            for (int fault = 1; fault <= 3; fault++)
            {
                int mode = fault;
                c.Throws<InvalidOperationException>(() => WorkerInputs(New(WorkerFactory(doc =>
                    doc.Get("entities")[3].Get("components").Get("proof_operating_inputs")[0].Set("fault", JsonValue.Number(mode.ToString())))), "machine.b", OperatingPhase.Idle),
                    "public idle pure/malformed/future observation refuses " + fault);
            }
        }
        private static void WorkerPhysical(Check c)
        {
            var stationaryFactory = WorkerFactory(doc =>
            {
                var parts = doc.Get("entities")[2].Get("components");
                parts.Remove("point_motion"); parts.Get("storage").Set("initial_stock", JsonValue.NewObject());
                var phases = JsonValue.NewArray(); phases.Add(parts.Get("operating_inputs")[0]); phases.Add(parts.Get("operating_inputs")[1]);
                parts.Set("operating_inputs", phases); doc.Get("entities")[0].Get("components").Remove("point_dock");
            });
            var stationary = New(stationaryFactory);
            c.Equal(OperatingInputCodes.Inputs, WorkerInputs(stationary, "machine.a", OperatingPhase.Idle).Blocker, "authored idle input is genuinely unavailable");
            var dryWork = TestKit.Order(stationary, 0, new PostOrderCommand { Plan = "inspect", Target = Find(stationary, "source").Id });
            c.That(dryWork.Accepted && Find(stationary, "machine.a").Get<IWorker>().Job != 0, "unpaid idle phase cannot forbid valid stationary work");
            var dryOrder = stationary.World.Services.Get<WorkBoard>().Order(checked((int)dryWork.Arg0)); var dryTwin = stationaryFactory.Load(stationary.Save());
            WorkerUntil(stationary, () => dryOrder.State == OrderState.Done, "stationary work without invented worker needs", dryTwin);
            c.Equal(0L, Exposure(stationary, "machine.a", OperatingPhase.Idle), "productive intervals and idle shortage never grant paid exposure");
            c.Equal(Fix.Zero, WorkerStock(stationary, "machine.a"), "otherwise valid work does not fabricate idle fuel");
            c.Equal(stationary.Hash(), dryTwin.Hash(), "saved stationary work shares the same rules");
            foreach (var actor in new[] { "machine.a", "machine.b" })
            {
                var factory = WorkerFactory(); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
                WorkerStep(game); long idle = Exposure(game, actor, OperatingPhase.Idle);
                c.Equal(Fix.FromInt(7), WorkerStock(game, actor), "first native/public idle quantum is paid");
                WorkerPolicy(c, game, actor, WorkAssignmentMode.EmergencyOnly);
                c.Equal(idle, Exposure(game, actor, OperatingPhase.Idle), "standby switch preserves idle rounding history");
                c.Equal(1L, Exposure(game, actor, OperatingPhase.Standby), "standby has its own accepted exposure");
                c.Equal(Fix.FromInt(6), WorkerStock(game, actor), "standby pays its separately authored slower rate");
                var twin = factory.Load(game.Save()); ulong paused = game.Hash(); long update = game.Clock.StepIndex;
                game.Clock.Pause(); twin.Clock.Pause(); WorkerStep(game, twin);
                c.Equal(update, game.Clock.StepIndex, "single clock pause prevents all operating phases");
                c.Equal(paused, game.Hash(), "pause does not spend or grant exposure");
                game.Clock.Resume(); twin.Clock.Resume(); WorkerStep(game, twin);
                c.Equal(game.Hash(), twin.Hash(), "saved phase switch resumes identically");
                WorkerPolicy(c, game, actor, WorkAssignmentMode.All);
                c.Equal(idle + 1, Exposure(game, actor, OperatingPhase.Idle), "idle resumes its prepaid second interval rather than resetting rounding");
                foreach (var other in new[] { "machine.a", "machine.b", "machine.c" })
                    if (other != actor) WorkerPolicy(c, game, other, WorkAssignmentMode.All, false);
                WorkerPolicy(c, game, "machine.d", WorkAssignmentMode.All, true, true);
                idle = Exposure(game, actor, OperatingPhase.Idle);
                var result = TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = Find(game, "source").Id });
                c.That(result.Accepted, "ordinary player command creates physical inspection");
                var order = game.World.Services.Get<WorkBoard>().Order(checked((int)result.Arg0)); var machine = Find(game, actor);
                c.That(machine.Get<IWorker>().Job != 0, "actual selected native/public machine acquires work");
                c.Equal(idle, Exposure(game, actor, OperatingPhase.Idle), "newly dispatched travel cannot also burn idle fuel");
                c.That(OperatingInputsContracts.CurrentWorkerPhase(game.World, machine) == null, "busy work is not a parked phase");
                var busyTwin = factory.Load(game.Save()); WorkerStep(game, busyTwin);
                c.Equal(idle, Exposure(game, actor, OperatingPhase.Idle), "productive interval earns no idle exposure");
                c.Equal(game.Hash(), busyTwin.Hash(), "active work/phase resources continue identically");
                WorkerUntil(game, () => order.State == OrderState.Done && machine.Get<IWorker>().Job == 0, "physical inspection/departure", busyTwin);
                c.That(OperatingInputsContracts.Read(game.World, machine, OperatingPhase.Movement).Providers[0].Exposure.ElapsedTicks > 0, "ordinary work uses actual funded movement");
                c.Equal(idle, Exposure(game, actor, OperatingPhase.Idle), "finishing productive update is not billed as a whole idle interval");
                c.Equal(Fix.FromInt(48), PhysicalStock(game) + Paid(game), "all ordinary stock plus cumulative phase expenditure is conserved");
                c.Equal(isolatedHash, isolated.Hash(), "interleaved session receives no phase changes");

                var emptyFactory = WorkerFactory(doc => doc.Get("entities")[actor == "machine.a" ? 2 : 3].Get("components").Get("storage").Set("initial_stock", JsonValue.NewObject()));
                var empty = New(emptyFactory); WorkerPolicy(c, empty, actor, WorkAssignmentMode.EmergencyOnly);
                c.Equal(0L, Exposure(empty, actor, OperatingPhase.Standby), "unfunded standby earns no resource credit");
                var route = TestKit.Order(empty, 0, new AddRouteCommand { From = Find(empty, "source").Id, To = Find(empty, actor).Id,
                    Resource = empty.World.Content.ResourceId("supply"), Settings = new LogisticsRouteSettings(true, Fix.FromInt(4), 100) });
                c.That(route.Accepted, "ordinary route command physically resupplies a standby worker");
                Job haul = null;
                WorkerUntil(empty, () =>
                {
                    foreach (var job in empty.World.Services.Get<WorkBoard>().Jobs)
                        if (job.IsHaul && job.IsLive && job.To == Find(empty, actor).Id && job.Loaded > Fix.Zero) { haul = job; return true; }
                    return false;
                }, "physical loaded standby supply");
                c.Equal(Find(empty, "machine.d").Id, haul.Worker, "existing fleet dispatcher uses actual compatible courier");
                c.Equal(0L, Exposure(empty, actor, OperatingPhase.Standby), "traveling cargo and inbound receipts are not usable fuel");
                c.That(TestKit.Order(empty, 0, new RemoveRouteCommand { Route = checked((int)route.Arg0) }).Accepted, "player retires standing intent without revoking loaded delivery");
                var loadedTwin = emptyFactory.Load(empty.Save());
                WorkerUntil(empty, () => !haul.IsLive && Exposure(empty, actor, OperatingPhase.Standby) > 0, "saved physical resupply and standby restart", loadedTwin);
                c.Equal(empty.Hash(), loadedTwin.Hash(), "loaded receipts/phase consumption/partial continuation agree");
                c.Equal(Fix.FromInt(40), PhysicalStock(empty) + Paid(empty), "finite donor and real delivered fuel account for all expenditure");
                c.Equal(Fix.Zero, WorkerStock(empty, "machine.d"), "courier delivers actual cargo rather than fabricating reserve fuel");
                c.That(WorkerInputs(empty, actor, OperatingPhase.Standby).Blocker.IsNone, "real physical fuel restores standby readiness");
            }
        }
    }
}
