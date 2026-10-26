using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class SignalWorkRequirementTests
    {
        public const string Case = "Signal work guards: authored worker alternatives, physical production wait and saved continuation";
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly Code Permit = new Code("signal.machine-permit"), Production = new Code("dispatch.production");
        public static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(Action<JsonValue> work = null) => ManufacturingDispatchTests.Factory(work: work,
            workDocument: "signal-work.json", operationsDocument: "signal-work-operations.json", startupDocument: "signal-work-startup.json");
        private static SimGame New(GameFactory factory) => ManufacturingDispatchTests.New(factory);
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static WorkView Work(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static Entity Site(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "dispatch.factory");
        private static Entity[] Machines(SimGame game) => game.World.Entities.All.Where(e => e.Def.Id == "signal.machine").OrderBy(e => e.Id).ToArray();
        private static TransformationSnapshot Queue(SimGame game)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Site(game).Id, Production, out var value))
                throw new InvalidOperationException("missing actual production");
            return value;
        }
        private static List<PlanView> Plans(SimGame game)
        { var result = new List<PlanView>(); Work(game).CopyPlans(result); return result; }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("signal workflow time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " production=" + Queue(game).Phase + "/" + Queue(game).QueueCount +
                " nodes=" + string.Join(";", Board(game).Orders.SelectMany(o => o.Nodes.Select(n => n.Id + "/" + n.State + "/" + n.Progress + "/" + n.Blocker))));
        }
        private static void Control(Check c, SimGame game, Entity entity, Code capability, bool enabled, int target = 1)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = entity.Id, Operation = capability,
                Enabled = enabled, Target = Fix.FromInt(target) }).Accepted, "ordinary control changes actual signal owner");
        private static long Begin(Check c, SimGame game)
        {
            var definition = game.World.Content.Get<EntityActivityDef>("signal.workflow");
            var intent = definition.CreateTargetIntent(Site(game).Id, ActivityWorkerSelection.Unbound, "signal.pipeline", 50);
            var command = new SubmitPlanCommand { Intent = intent }; ulong hash = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary plan preview admits declarative workflow");
            c.Equal(hash, game.Hash(), "plan intent does not reserve signals/work/stock");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary plan creates actual graph execution");
            return result.Arg0;
        }
        private static Fix Stock(SimGame game)
        {
            var resource = game.World.Content.ResourceId("steel"); Fix total = Queue(game).Wip.Stock.AmountOf(resource);
            foreach (Entity entity in game.World.Entities.All) if (entity.Get<IInventory>() is IInventory inventory) total += inventory.Amount(resource);
            return total;
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "strict shared signal requirements identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "same actual graph/provider/queue save layout");
            var factory = Factory(); var game = New(factory);
            c.That(!game.World.Services.Has<IEconomy>(), "signal-conditioned work requires no business module");
            var requirement = game.World.Content.Get<TargetSignalRequirementDef>("signal.queue-idle");
            c.That(requirement is ISignalWorkRequirement && ReferenceEquals(requirement.Conditions[0].Signal,
                game.World.Content.Get<AutomationSignalDef>("signal.production-backlog")), "requirement aliases existing selected signal");
            var workers = Machines(game); var offers = new List<WorkWorkerCandidate>(); ulong hash = game.Hash();
            c.Equal(Code.None, Work(game).PreviewWorkerCandidates("signal.workflow", "prepare", Site(game).Id, offers), "same candidate preview evaluates signal requirements");
            c.That(offers.Count == 2 && offers.All(o => o.Result.Status != WorkRequirementStatus.Satisfied), "both current machine settings block preparation");
            c.That(offers.Any(o => o.Result.Blocker == new Code("signal.wait-machine") || o.Result.Blocker == new Code("signal.wait-setting")), "authored blocker survives all/any evaluation");
            var criteria = new List<WorkRequirementView>();
            c.Equal(Code.None, Work(game).PreviewOrderRequirements("signal.absent", Site(game).Id, criteria), "missing runtime named source remains readable diagnostic");
            c.That(criteria.Any(v => v.Result.Blocker == OperationCodes.SignalUnavailable), "no source substitution or fabricated ready signal");
            c.Equal(hash, game.Hash(), "candidate/missing-source previews pure");
            c.Equal(Codes.NotOwned, Work(game, 1).PreviewOrderRequirements("signal.workflow", Site(game).Id, criteria), "foreign requirements stay private");
            c.Equal(0, criteria.Count, "foreign preview discloses no signal/work detail");

            // An explicitly authored numeric alternative may independently admit a compatible machine.
            var alternative = New(factory); var other = Machines(alternative)[1];
            Control(c, alternative, other, Permit, false, 2);
            offers.Clear(); Work(alternative).PreviewWorkerCandidates("signal.workflow", "prepare", Site(alternative).Id, offers);
            c.That(offers.Any(o => o.Worker == other.Id && o.Result.Status == WorkRequirementStatus.Satisfied),
                "authored any branch admits numeric alternative without inventing a provider");
            c.That(offers.Any(o => o.Worker != other.Id && o.Result.Status != WorkRequirementStatus.Satisfied), "unconfigured alternative worker remains blocked");

            long plan = Begin(c, game);
            Reach(game, () => Plans(game).Single(p => p.Id == plan).State == PlanState.Active, "real signal workflow activation");
            var order = Board(game).Order(Plans(game).Single(p => p.Id == plan).Order);
            for (int i = 0; i < 6; i++) Update(game);
            c.Equal(WorkProgress.Zero, order.Node("prepare").Progress, "blocked signal cannot earn preparation");
            c.Equal(0, Queue(game).QueueCount, "blocked workflow dispatches no manufacturing");
            c.Equal(Fix.FromInt(60), Stock(game), "readiness checks consume no real materials");
            Control(c, game, workers[0], Permit, true);
            Reach(game, () => order.Node("prepare").Progress.WholeTicks > 0 && !order.Node("prepare").Progress.Complete(6), "real signal-approved preparation");
            var prepare = order.Node("prepare"); var progress = prepare.Progress; int job = prepare.JobId;
            c.Equal(workers[0].Id, Board(game).Job(job).Worker, "actual selector assigns only compatible enabled worker");
            c.That(prepare.Resources.Count > 0, "actual selected job retains physical reusable tool commitment");
            Control(c, game, workers[0], Permit, false);
            for (int i = 0; i < 3; i++) Update(game);
            c.Equal(progress, prepare.Progress, "current signal recheck pauses rather than erases or earns work");
            c.That(prepare.State == NodeState.Paused && !prepare.Blocker.IsNone, "actual node reports paused signal blocker");
            c.Equal(job, prepare.JobId, "signal wait retains actual job, not a duplicate execution");
            c.That(prepare.Resources.Count > 0, "paused work keeps actual held tools");
            c.That(Work(game).TryReadProgress(order.Id, out var paused), "live progress reads real paused order");
            c.That(!Work(game, 1).TryReadProgress(order.Id, out _), "foreign progress remains private");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "signal pause saves actual graph/job/tool/provider state");
            for (int i = 0; i < 3; i++) Update(game, twin);
            c.Equal(game.Hash(), twin.Hash(), "paused saved twins stay identical without second signal clock");
            Control(c, game, workers[0], Permit, true); Control(c, twin, Machines(twin)[0], Permit, true);
            Reach(game, () => Queue(game).QueueCount == 2, "earned node dispatches actual two batches", twin);
            c.That(order.Node("finish").State == NodeState.Done && order.Node("wait").State != NodeState.Done, "dependencies settle but production guard waits");
            c.That(order.Node("wait").Blocker == new Code("signal.wait-production"), "actual backlog propagates authored wait blocker");
            c.Equal(Fix.FromInt(58), Stock(game), "only earned physical preparation consumes two units");
            c.That(Work(game).TryReadProgress(order.Id, out var waiting) && waiting.EarnedUnsettledNodes > 0,
                "zero nominal work does not mean signal-dependent settlement complete");
            c.Equal(PlanState.Active, Plans(game).Single(p => p.Id == plan).State, "real plan remains active while production pending");
            c.Equal(game.Hash(), twin.Hash(), "saved physical dispatch matches");
            twin = factory.Load(game.Save());
            for (int i = 0; i < 5; i++) Update(game, twin);
            c.That(Queue(game).QueueCount == 2 && order.Node("verify").Progress.Equals(WorkProgress.Zero), "disabled production cannot satisfy final work or dispatch duplicate batches");
            c.Equal(game.Hash(), twin.Hash(), "saved signal wait continues identically");
            Control(c, game, Site(game), Production, true); Control(c, twin, Site(twin), Production, true);
            c.That(Queue(game).Progress.WholeTicks > 0 && Queue(game).Progress.WholeTicks < Queue(game).Required &&
                !Queue(game).Wip.Stock.IsEmpty && !Queue(game).Tools.Held.IsEmpty, "manufacturing uses real time/WIP/tools while activity waits");
            Reach(game, () => Queue(game).QueueCount == 1, "first actual batch", twin);
            c.Equal(Fix.FromInt(56), Stock(game), "first real three-input one-output production accounting");
            c.That(order.Node("wait").State != NodeState.Done, "one unfinished batch keeps guard blocked");
            Reach(game, () => Queue(game).QueueCount == 0, "second actual batch", twin);
            c.Equal(Fix.FromInt(54), Stock(game), "second real batch exact material accounting");
            Reach(game, () => Plans(game).Single(p => p.Id == plan).State == PlanState.Done, "final real physical verification", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved entire signal-conditioned pipeline deterministic");
            c.That(order.Node("verify").Progress.Complete(2), "last worker work earns its own authored duration");
            c.That(Board(game).TryReadCompletion(Company, order.PlanKey, Site(game).Id, out _), "only completed workflow records actual activity completion");
            c.That(!Work(game).TryReadProgress(order.Id, out _), "completed retired order does not expose invented live progress");
            c.That(paused.Work.RemainingRaw > 0 && paused.State != OrderState.Done, "detached earlier paused snapshot unchanged");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed signal workflow saves with existing codecs");

            game = New(factory); plan = Begin(c, game);
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { plan }) }).Accepted, "normal cancellation of signal-blocked workflow");
            for (int i = 0; i < 8; i++) Update(game);
            c.That(Queue(game).QueueCount == 0 && Stock(game) == Fix.FromInt(60), "cancelled readiness causes no dispatch/refund/consumption");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled signal workflow checkpoints");
            foreach (string invalid in new[] { "unknown-signal", "unknown-source", "empty", "duplicate", "wrong-unit", "inverted-range", "boolean-as-number", "worker-on-timer" })
                c.Throws<ContentException>(() => New(Factory(json => {
                    JsonValue row = json.Get("target_signal_requirements")[0], conditions = row.Get("conditions"), condition = conditions[0];
                    switch (invalid) {
                        case "unknown-signal": condition.Set("signal", JsonValue.Of("missing")); break;
                        case "unknown-source": condition.Set("source", JsonValue.Of("missing")); break;
                        case "empty": row.Set("conditions", JsonValue.NewArray()); break;
                        case "duplicate": conditions.Add(condition.Clone()); break;
                        case "wrong-unit": condition.Set("unit", JsonValue.Of("unit.wrong")); break;
                        case "inverted-range": condition.Set("minimum", JsonValue.Number("1")); break;
                        case "boolean-as-number": condition.Set("signal", JsonValue.Of("signal.machine-enabled")); break;
                        case "worker-on-timer": json.Get("entity_activities")[0].Get("nodes")[1].Set("requirement", JsonValue.Of("signal.worker-enabled")); break;
                    }
                })), "strict authored guard refuses " + invalid);
        }
    }
}
