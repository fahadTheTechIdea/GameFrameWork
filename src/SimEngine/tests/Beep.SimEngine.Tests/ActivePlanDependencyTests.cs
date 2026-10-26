using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static partial class PlanTemplateTests
    {
        private static Order ActiveDependencyOrder(SimGame game, long id)
            => game.World.Services.Get<WorkBoard>().Order(TemplatePlan(game, id).Order);
        private static void ActiveDependencyContracts(Check c)
        {
            GameFactory factory = Dependencies(); SimGame game = ConstructionPlanTests.NewPoints(factory);
            EmergencyPreemptionTests.Configure(c, game);
            long a = DependencySubmit(c, game, DependencyBuild(game, 12, "active.edit"));
            long b = DependencySubmit(c, game, DependencyBuild(game, 16, "waiting.edit", a));
            long foreign = DependencySubmit(c, game, DependencyBuild(game, 60, "foreign.active"), 1);
            DependencyEdit(c, game, "active.edit", false);
            EditingReach(game, () => ActiveDependencyOrder(game, a)?.Node("build").Progress.WholeTicks > 0, "active edit contract frontier");
            DependencyEdit(c, game, "active.edit", true);
            EmergencyPreemptionTests.Configure(c, game, interruptible: false);
            Order order = ActiveDependencyOrder(game, a); var node = order.Node("build");
            int job = node.JobId; WorkProgress progress = node.Progress; ulong hash = game.Hash(); long time = game.Clock.StepIndex;
            var reversal = Changes(Links(a, b), Links(b));
            c.Equal(WorkPolicyCodes.NotInterruptible, View(game).PreviewPlanDependencies(reversal).Blocker,
                "active dependency waits require normal actual machine interruption permission");
            c.Equal(Codes.NotOwned, View(game).PreviewPlanDependencies(Changes(Links(a, foreign))).Blocker, "active references remain company-private");
            c.Equal(hash, game.Hash(), "active previews stop no machine and spend no resource/progress/time");
            c.Equal(time, game.Clock.StepIndex, "all active-control previews read the sole clock");
            c.Equal(WorkPolicyCodes.NotInterruptible, ChangeLinks(game, reversal).Refusal, "protected actual job refuses atomic active command");
            c.That(TemplatePlan(game, b).Dependencies[0] == a && TemplatePlan(game, a).Dependencies.Count == 0 &&
                node.Progress.Equals(progress) && node.JobId == job, "late interruption refusal changes no pending prefix or native execution");
            EmergencyPreemptionTests.Configure(c, game, interruptible: true);
            c.Equal(PlanCodes.DependencyCycle, View(game).PreviewPlanDependencies(Changes(Links(a, b))).Blocker,
                "active proposed topology includes unchanged pending ancestry in the sole compiler");
            hash = game.Hash(); var proposal = View(game).PreviewPlanDependencies(reversal);
            c.That(proposal.Ready && proposal.Entries.Count == 2, "atomic live graph reversal has no invalid intermediate cycle");
            c.Equal(hash, game.Hash(), "accepted active proposal remains pure");
            PlanView retained = TemplatePlan(game, a);
            c.That(ChangeLinks(game, reversal).Accepted, "same existing command edits active and pending manual intent atomically");
            c.That(TemplatePlan(game, a).State == PlanState.Active && TemplatePlan(game, a).Order == order.Id &&
                TemplatePlan(game, a).Target == order.Target && node.Progress.Equals(progress) && node.JobId == job,
                "active edit retains exact native plan/order/target/job/progress, never creates another execution");
            c.Equal(0, retained.Dependencies.Count, "retained immutable active view keeps previous prerequisite facts");
            c.Equal(0, EmergencyPreemptionTests.Find(game, "machine").Get<IWorker>().Job, "dependency hold frees actual machine through native interruption");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "paused active graph and reopened job use existing native save owners");
            // Queued recovery restores actual prerequisites, not a compatibility model.
            game.Seat(0).Submit(new ConfigurePlanDependenciesCommand { Changes = Changes(Links(a), Links(b, a)) });
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(TemplatePlan(game, a).Dependencies.Count == 0 && TemplatePlan(game, b).Dependencies[0] == a, "saved atomic command restores live topology at normal Update");
            c.Equal(game.Hash(), twin.Hash(), "queued active topology command and native codecs continue identically");
            long ended = DependencySubmit(c, game, DependencyBuild(game, 24, "ended.active"));
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { ended }) }).Accepted, "player cancels actual pending record");
            c.Equal(PlanCodes.NotPending, View(game).PreviewPlanDependencies(Changes(Links(ended))).Blocker, "terminal intent is not rewritten as live work");
            ActiveSourceOwnedDependencies(c);
        }
        private static void ActiveSourceOwnedDependencies(Check c)
        {
            foreach (bool provider in new[] { false, true })
            {
                GameFactory factory = Repeating(root => root.Get("plan_templates")[0].Get("entries")[0].Set("paused", Beep.SimEngine.Content.JsonValue.Of(false)));
                SimGame game = ConstructionPlanTests.NewPoints(factory);
                c.That(ConfigureTemplate(game, provider, true).Accepted, "ordinary player enables actual recurring source");
                Tick(game); long id = TemplateSource(game, provider).Schedule.LastPlan;
                c.That(TemplatePlan(game, id).State == PlanState.Active, "native/public recurring source owns real active execution");
                ulong hash = game.Hash();
                c.Equal(PlanCodes.SourceOwnedDependencies, View(game).PreviewPlanDependencies(Changes(Links(id))).Blocker,
                    "active manual editing never rewrites frozen authored recurring provenance");
                c.Equal(hash, game.Hash(), "active source-owned refusal is pure");
                c.Equal(PlanCodes.SourceOwnedDependencies, ChangeLinks(game, Changes(Links(id))).Refusal, "active source-owned command shares refusal");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused active source graph remains durable");
            }
        }
        private static void ActiveDependencyPhysical(Check c)
        {
            GameFactory factory = Dependencies(); SimGame game = ConstructionPlanTests.NewPoints(factory);
            EmergencyPreemptionTests.Configure(c, game);
            long a = DependencySubmit(c, game, DependencyBuild(game, 12, "physical.active"));
            long b = DependencySubmit(c, game, DependencyBuild(game, 16, "physical.prerequisite"));
            DependencyEdit(c, game, "physical.active", false);
            EditingReach(game, () => ActiveDependencyOrder(game, a)?.Node("build").Progress.WholeTicks > 0, "earned active machine work");
            Order order = ActiveDependencyOrder(game, a); var node = order.Node("build");
            WorkProgress progress = node.Progress; int job = node.JobId; var target = order.Target;
            var supply = FindPoint(game, "source").Get<IInventory>(); var resource = game.World.Content.ResourceId("supply");
            Fix stock = supply.Amount(resource);
            c.That(ChangeLinks(game, Changes(Links(a, b))).Accepted, "player adds a real pending prerequisite to partially earned physical work");
            c.That(TemplatePlan(game, a).Blocker == PlanCodes.DependencyPending && !TemplatePlan(game, a).Paused,
                "dependency hold is an actual engine blocker, not a fabricated player pause");
            c.That(node.Progress.Equals(progress) && node.JobId == job && order.Target == target && stock == supply.Amount(resource),
                "active hold spends/refunds no material and keeps earned progress/job/target");
            var sourceCheck = game.World.Services.Get<WorkBoard>().CheckSource(
                new LogisticsDemandId(WorkModule.ModuleId, order.Id, Code.None), order.Owner, target);
            c.That(sourceCheck.Status == LogisticsDemandStatus.Expired && sourceCheck.Refusal == PlanCodes.DependencyPending,
                "same active dependency gate expires native automatic supply intent with the actual blocker");
            c.Equal(0, EmergencyPreemptionTests.Find(game, "machine").Get<IWorker>().Job, "held work releases the only machine to avoid a prerequisite deadlock");
            var offers = new List<WorkerDispatchOffer>(); View(game).CopyAssignments(offers);
            c.That(offers.TrueForAll(offer => offer.Identity != job || !offer.Ready), "held active job is not a ready assignment");
            SimGame twin = factory.Load(game.Save()); TemplateAdvance(game, 5, twin);
            c.That(node.Progress.Equals(progress) && TemplateCount(game, "point.tower") == 0 && stock == supply.Amount(resource),
                "normal updates complete no blocked effect or duplicate consumption while prerequisite remains paused");
            c.Equal(game.Hash(), twin.Hash(), "active dependency hold saves/continues identically");
            long time = game.Clock.StepIndex; ulong hash = game.Hash(); game.Clock.Pause(); twin.Clock.Pause(); game.Update(); twin.Update();
            c.That(game.Clock.StepIndex == time && game.Hash() == hash, "single-clock pause freezes active control and native work");
            game.Clock.Resume(); twin.Clock.Resume();
            DependencyEdit(c, game, "physical.prerequisite", false); DependencyEdit(c, twin, "physical.prerequisite", false);
            EditingReach(game, () => TemplatePlan(game, b).State == PlanState.Done, "new physical prerequisite uses released machine", twin);
            c.That(TemplateCount(game, "point.tower") == 1 && node.Progress.Equals(progress), "held successor earns nothing before actual prerequisite completion");
            EditingReach(game, () => TemplatePlan(game, a).State == PlanState.Done, "existing active job resumes earned construction", twin);
            c.That(TemplateCount(game, "point.tower") == 2 && TemplatePlan(game, a).Order == order.Id &&
                TemplatePlan(game, a).Target == target, "original active execution resumes and completes physically, not a replacement job/order");
            c.Equal(Fix.FromInt(12), supply.Amount(resource), "exactly two physical builds consume eight finite units; completed preparation is not replayed");
            c.Equal(game.Hash(), twin.Hash(), "active edit, prerequisite work and resumed completion agree on saved twin");
            c.Equal(PlanCodes.NotPending, View(game).PreviewPlanDependencies(Changes(Links(a))).Blocker, "completed earned history remains immutable");
            ActiveDependencyInterruptedPredecessor(c, false);
            ActiveDependencyInterruptedPredecessor(c, true);
            ActiveDependencyTravel(c);
            ActiveDependencyTimer(c);
        }
        private static void ActiveDependencyTravel(Check c)
        {
            GameFactory factory = Dependencies(); SimGame game = ConstructionPlanTests.NewPoints(factory);
            EmergencyPreemptionTests.Configure(c, game);
            long a = DependencySubmit(c, game, DependencyBuild(game, 24, "travel.active"));
            long b = DependencySubmit(c, game, DependencyBuild(game, 16, "travel.prerequisite"));
            DependencyEdit(c, game, "travel.active", false);
            Entity machine = EmergencyPreemptionTests.Find(game, "machine");
            Order order = ActiveDependencyOrder(game, a);
            EditingReach(game, () => machine.Get<IMotion>().Status == MoveStatus.Moving &&
                game.World.Services.Get<WorkBoard>().Job(machine.Get<IWorker>().Job)?.OrderId == order.Id,
                "active physical machine travels to actual project");
            Fix position = machine.Get<PointPresence>().Position;
            c.That(ChangeLinks(game, Changes(Links(a, b))).Accepted, "player adds a prerequisite while actual machine is moving");
            c.That(machine.Get<IMotion>().Status != MoveStatus.Moving && machine.Get<IWorker>().Job == 0 &&
                machine.Get<PointPresence>().Position == position, "dependency command stops and releases real travel without teleporting");
            var candidates = new List<WorkWorkerCandidate>(); ulong hash = game.Hash();
            c.That(View(game).TryCopyWorkerCandidates(order.Id, "build", candidates) && candidates.Count > 0 &&
                candidates.TrueForAll(candidate => candidate.Result.Blocker == PlanCodes.DependencyPending),
                "unposted active stage reports the same plan dependency blocker before any native job exists");
            c.Equal(hash, game.Hash(), "unposted active stage candidate diagnostics are pure");
            SimGame twin = factory.Load(game.Save()); TemplateAdvance(game, 4, twin);
            c.Equal(position, machine.Get<PointPresence>().Position, "blocked native trip does not advance on an independent timer");
            c.Equal(game.Hash(), twin.Hash(), "interrupted native travel and graph save identically");
            DependencyEdit(c, game, "travel.prerequisite", false); DependencyEdit(c, twin, "travel.prerequisite", false);
            EditingReach(game, () => TemplatePlan(game, a).State == PlanState.Done, "physical travel restarts after prerequisite", twin);
            c.That(TemplatePlan(game, b).State == PlanState.Done && TemplateCount(game, "point.tower") == 2,
                "released moving machine completes prerequisite then returns through normal routes/work");
            c.Equal(game.Hash(), twin.Hash(), "saved travel interruption and resumed construction agree");
        }
        private static void ActiveDependencyTimer(Check c)
        {
            GameFactory factory = Dependencies(constructionEdit: authored => authored
                .Get("activities")[0].Get("nodes")[1].Set("roles", JsonValue.NewArray()));
            SimGame game = ConstructionPlanTests.NewPoints(factory); EmergencyPreemptionTests.Configure(c, game);
            long a = DependencySubmit(c, game, DependencyBuild(game, 12, "timer.active"));
            long b = DependencySubmit(c, game, DependencyBuild(game, 16, "timer.prerequisite"));
            DependencyEdit(c, game, "timer.active", false);
            EditingReach(game, () => ActiveDependencyOrder(game, a)?.Node("build").Progress.WholeTicks > 0, "actual authored workerless stage");
            Order order = ActiveDependencyOrder(game, a); var node = order.Node("build"); WorkProgress progress = node.Progress;
            c.Equal(0, node.JobId, "authored workerless stage uses same node progress, not a fake worker job");
            c.That(ChangeLinks(game, Changes(Links(a, b))).Accepted, "same live command gates authored workerless work");
            SimGame twin = factory.Load(game.Save()); TemplateAdvance(game, 5, twin);
            c.That(node.Progress.Equals(progress) && TemplateCount(game, "point.tower") == 0,
                "workerless stage cannot bypass active dependencies through elapsed time");
            c.Equal(game.Hash(), twin.Hash(), "held authored workerless progress is durable");
            DependencyEdit(c, game, "timer.prerequisite", false); DependencyEdit(c, twin, "timer.prerequisite", false);
            EditingReach(game, () => TemplatePlan(game, a).State == PlanState.Done, "authored workerless progress resumes", twin);
            c.That(TemplatePlan(game, b).State == PlanState.Done && TemplateCount(game, "point.tower") == 2,
                "same native timer stages finish only after physical prerequisites");
            c.Equal(Fix.FromInt(12), FindPoint(game, "source").Get<IInventory>().Amount(game.World.Content.ResourceId("supply")),
                "workerless resume consumes no duplicate preparation/input");
            c.Equal(game.Hash(), twin.Hash(), "native machine and workerless active control share saved deterministic execution");
        }
        private static void ActiveDependencyInterruptedPredecessor(Check c, bool failed)
        {
            GameFactory factory = Dependencies(failed: failed); SimGame game = ConstructionPlanTests.NewPoints(factory);
            EmergencyPreemptionTests.Configure(c, game);
            long a = DependencySubmit(c, game, DependencyBuild(game, 12, "interrupted.active"));
            long b = DependencySubmit(c, game, DependencyBuild(game, 16, "interrupted.prerequisite"));
            DependencyEdit(c, game, "interrupted.active", false);
            EditingReach(game, () => ActiveDependencyOrder(game, a)?.Node("build").Progress.WholeTicks > 0, "retained interrupted work");
            Order order = ActiveDependencyOrder(game, a); WorkProgress progress = order.Node("build").Progress;
            c.That(ChangeLinks(game, Changes(Links(a, b))).Accepted, "actual active project waits for physical prerequisite");
            if (failed)
            {
                DependencyEdit(c, game, "interrupted.prerequisite", false);
                EditingReach(game, () => TemplatePlan(game, b).State == PlanState.Failed, "physical failed prerequisite");
            }
            else c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { b }) }).Accepted, "player explicitly cancels actual prerequisite");
            Code blocker = failed ? PlanCodes.DependencyFailed : PlanCodes.DependencyCancelled;
            c.That(TemplatePlan(game, a).State == PlanState.Active && TemplatePlan(game, a).Blocker == blocker &&
                order.Node("build").Progress.Equals(progress), "failed/cancelled prerequisite holds earned native work, not implicit success/cancellation");
            SimGame twin = factory.Load(game.Save()); TemplateAdvance(game, 3, twin);
            c.Equal(game.Hash(), twin.Hash(), "unresolved active dependency survives interrupted save");
            c.That(ChangeLinks(game, Changes(Links(a))).Accepted && ChangeLinks(twin, Changes(Links(a))).Accepted,
                "explicit ordinary edit removes failed/cancelled dependency from same active project");
            EditingReach(game, () => TemplatePlan(game, a).State != PlanState.Active, "explicit recovery continuation", twin);
            c.Equal(game.Hash(), twin.Hash(), "recovered native execution continues identically");
            if (!failed) c.That(TemplatePlan(game, a).State == PlanState.Done && TemplateCount(game, "point.tower") == 1,
                "removing a cancelled prerequisite completes only real resumed work");
        }
    }
}

