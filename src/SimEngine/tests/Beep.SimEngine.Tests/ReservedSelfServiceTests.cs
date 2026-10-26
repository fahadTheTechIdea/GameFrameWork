using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.FacilitySelectionTests;

namespace Beep.SimEngine.Tests
{
    internal static class ReservedSelfServiceTests
    {
        private static readonly Code Service = new Code("fleet.service");
        private static readonly WorkerRoleId Inspect = new WorkerRoleId("inspect");
        internal static void Register()
        {
            TestRunner.Add("Reserved self-service: authored provenance, private controls and safe admission", Contracts);
            TestRunner.Add("Reserved self-service: physical maintenance, reserve floors and saved continuation", Physical);
        }
        internal static GameFactory Fixture(bool publicPolicy = false, bool publicWorker = false, bool publicSource = false,
            Action<JsonValue> edit = null)
            => Factory(publicWorker, edit: root =>
            {
                var intent = Json.Parse(FrameworkProofContent.Read("reserved-self-service.json"));
                root.Get("work_policies")[0].Set("assignment_mode", intent.Get("assignment_mode"));
                var parts = Parts(root, publicWorker); var source = parts.Get("recurring_activity");
                source.Set("reserve_use", intent.Get("reserve_use"));
                if (publicSource)
                {
                    source.Remove("conditions");
                    source.Set("fault", JsonValue.Number("0")); source.Set("idle_only", JsonValue.Of(false));
                    parts.Remove("recurring_activity"); parts.Set("proof_service_recurring", source);
                }
                if (publicPolicy)
                {
                    parts.Remove("work_policy"); parts.Set("proof_service_policy", Json.Parse("{\"profile\":\"docks.worker\",\"fault\":0}"));
                }
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(CompanyAlertModule.ModuleId));
                root.Get("scenarios")[0].Get("settings").Set(CompanyAlertModule.ModuleId,
                    Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"alerts.neutral\",\"level\":\"normal\",\"worker_reserves\":[{\"role\":\"inspect\",\"minimum_ready\":4}]},{\"company\":1,\"profile\":\"alerts.neutral\",\"level\":\"normal\",\"worker_reserves\":[]}]}"));
                edit?.Invoke(root);
            }, alertProfiles: FrameworkProofContent.Read("company-alerts.json"));
        private static JsonValue Source(JsonValue root, bool worker = false, bool source = false)
            => Parts(root, worker).Get(source ? "proof_service_recurring" : "recurring_activity");
        private static Entity Machine(SimGame game, bool worker = false) => Find(game, Actor(worker));
        private static void Policy(Check c, SimGame game, bool worker, WorkAssignmentMode mode, bool enabled = true)
        {
            Entity actor = Machine(game, worker); var current = WorkPolicyRules.Read(actor);
            var settings = new WorkPolicySettings(enabled, current.Priority, current.AllowedRoles, current.Area,
                current.Interruptible, current.EmergencyEligible, mode);
            c.Equal(Code.None, View(game).PreviewPolicy(actor.Id, settings), "existing private policy preview accepts current mode");
            c.That(Command(game, new ConfigureWorkPolicyCommand { Worker = actor.Id, Settings = settings }).Accepted, "existing command commits exact mode");
        }
        private static void Enabled(Check c, SimGame game, bool worker, bool enabled)
            => c.That(Command(game, new ConfigureRecurringPlanCommand { Entity = Machine(game, worker).Id, Source = Service,
                Settings = new RecurringPlanSettings(enabled, 60, GameDuration.Minutes(1), new Code("service.nearest")) }).Accepted, "player controls the same recurring source");
        private static CompanyWorkerReserveSnapshot Reserve(SimGame game)
        {
            Code refusal = game.Seat(0).View.Feature<CompanyAlertView>().TryReadReserve(new PlayerId(0), Inspect, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("self-service reserve refused: " + refusal); return value;
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) Step(game, twin);
            if (!done())
            {
                var facts = new List<string>(); foreach (var job in Board(game).Jobs)
                    facts.Add(job.Id + "/" + job.State + "/" + job.Blocker + "/" + job.Worker + "/" + job.ToilIndex);
                throw new InvalidOperationException(stage + " timeout at game time " + game.Clock.TotalGameTime + ": " + string.Join(";", facts));
            }
        }
        private static PlanIntent Manual(SimGame game, bool worker)
        {
            Entity machine = Machine(game, worker);
            var definition = (IEntityTargetPlanDefinition)RecurringPlanContracts.ResolvePlan(game.World, machine.Def.Component<IRecurringPlanDefinition>(Service));
            return definition.CreateTargetIntent(Find(game, "source").Id,
                new ActivityWorkerSelection(new[] { new ActivityWorkerBinding("service", machine.Id) }), "manual.service", int.MaxValue);
        }
        private static void Contracts(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current self-service semantics identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "clean new accepted policy protocol");
            c.Throws<ArgumentException>(() => new WorkPolicySettings(true, 0, Array.Empty<WorkerRoleId>(), null, true, true, (WorkAssignmentMode)3), "unknown work mode rejects at construction");
            using (var invalid = new MemoryStream())
            {
                var writer = new StateWriter(invalid); bool enabled = true; int priority = 0, mode = 258; string area = null;
                writer.Bool(ref enabled); writer.Int(ref priority); writer.String(ref area); writer.Bool(ref enabled); writer.Bool(ref enabled); writer.Int(ref mode);
                byte[] bad = invalid.ToArray();
                c.Throws<CorruptStateException>(() => WorkPolicySettings.Sync(new StateReader(bad, 0, bad.Length), null), "wide mode cannot wrap to a valid byte enum");
            }
            c.Throws<ArgumentException>(() => new RecurringPlanBinding(RecurringPlanTarget.Self, Array.Empty<string>(), RecurringReserveUse.SelfService, null), "self-service cannot be a timer without a selected machine");
            c.Throws<ArgumentException>(() => new RecurringPlanBinding(RecurringPlanTarget.WorkerHome, new[] { "service" }, (RecurringReserveUse)8, null), "unknown reserve purpose rejects");
            foreach (bool provider in new[] { false, true }) foreach (Action<JsonValue> change in new Action<JsonValue>[] {
                root => Source(root, source: provider).Remove("reserve_use"),
                root => Source(root, source: provider).Set("reserve_use", JsonValue.Of("emergency")),
                root => Source(root, source: provider).Set("reserve_use", JsonValue.Of(true)),
                root => Source(root, source: provider).Set("reserve_use", JsonValue.Null()),
                root => Source(root, source: provider).Set("worker_nodes", Json.Parse("[\"settle\"]")),
                root => Source(root, source: provider).Set("worker_nodes", JsonValue.NewArray())
            }) c.Throws<ContentException>(() => New(Fixture(publicSource: provider, edit: change)), "native/public invalid authored purpose fails before play");
            foreach (bool publicPolicy in new[] { false, true }) foreach (bool publicWorker in new[] { false, true }) foreach (bool publicSource in new[] { false, true })
            {
                var factory = Fixture(publicPolicy, publicWorker, publicSource); var game = New(factory);
                Entity machine = Machine(game, publicWorker); ulong hash = game.Hash(); var read = Read(game, publicWorker);
                c.That(read.Target.IsValid, "genuine prospective source passes individual standby and company floor without acquiring");
                c.Equal(4, Reserve(game).ReadyWorkers, "only actual idle workers count ready");
                c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "sole work system declares self-service admission semantics");
                c.Equal(WorkPolicyCodes.Standby, game.World.Services.Get<IWorkerDispatch>().CheckAssignment(game.World, machine, machine.Owner, Inspect,
                    new[] { Find(game, "source") }, false), "unbound ordinary assignment gains no service provenance");
                c.Equal(0, machine.Get<IWorker>().Job, "preview allocates no worker");
                c.Equal(hash, game.Hash(), "source/facility/ready observations do not mutate");
                var definition = machine.Def.Component<IRecurringPlanDefinition>(Service);
                var plan = RecurringPlanContracts.ResolvePlan(game.World, definition);
                var offers = new List<WorkWorkerCandidate>();
                game.World.Services.Get<IWorkerDispatch>().CopyCandidates(game.World, machine.Owner, plan.Graph.Find("service").Roles,
                    Find(game, "source"), plan.Graph.Find("service"), 0, null, null, WorkCandidateMode.IdleOnly, offers,
                    machine.Get<IRecurringPlanSource>(Service));
                int allowed = 0; foreach (var offer in offers) if (offer.Result.Status == WorkRequirementStatus.Satisfied) { allowed++; c.Equal(machine.Id, offer.Worker, "prospective exemption never selects another worker"); }
                c.Equal(1, allowed, "only source worker can borrow prospective self-service access");
                c.Equal(Codes.NotOwned, View(game, 1).PreviewPolicy(machine.Id, WorkPolicyRules.Read(machine)), "foreign policy control is private");
                c.Equal(Codes.NotFound, View(game, 1).ReadRecurringFacilities(machine.Id, Service, out _), "foreign facility facts do not leak");
                var current = WorkPolicyRules.Read(machine); using var stream = new MemoryStream();
                WorkPolicySettings.Sync(new StateWriter(stream), current); stream.Position = 0;
                byte[] bytes = stream.ToArray();
                c.Equal(WorkAssignmentMode.EmergencyAndSelfService, WorkPolicySettings.Sync(new StateReader(bytes, 0, bytes.Length), null).AssignmentMode, "shared codec round-trips the exact new mode");
                int pending = game.Seat(0).Submit(new ConfigureWorkPolicyCommand { Worker = machine.Id, Settings = current });
                var pendingTwin = factory.Load(game.Save()); Step(game, pendingTwin);
                c.Equal(game.Hash(), pendingTwin.Hash(), "pending new mode controls persist through normal command stream");
                Policy(c, game, publicWorker, WorkAssignmentMode.EmergencyOnly);
                c.That(!Read(game, publicWorker).Target.IsValid, "pure emergency-only mode still refuses ordinary self-service");
                Policy(c, game, publicWorker, WorkAssignmentMode.EmergencyAndSelfService);
                c.That(Read(game, publicWorker).Target.IsValid, "explicit opt-in restores real prospective service");
                Policy(c, game, publicWorker, WorkAssignmentMode.EmergencyAndSelfService, false);
                c.That(!Read(game, publicWorker).Target.IsValid, "self-service permission cannot override disabled worker policy");
                Policy(c, game, publicWorker, WorkAssignmentMode.EmergencyAndSelfService);
                var manual = Manual(game, publicWorker);
                c.That(Command(game, new SubmitPlanCommand { Intent = manual }).Accepted, "manual intent may wait without fake provenance");
                for (int i = 0; i < 3; i++) Step(game);
                c.Equal(0, machine.Get<IWorker>().Job, "even exact worker/node and maximum manual priority cannot borrow service exemption");
                c.Equal(Fix.FromInt(4), Condition(game, Actor(publicWorker)), "manual blocked service cannot repair");
                c.Equal(Fix.FromInt(32), Stock(game, "source"), "manual refusal spends no spares");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "manual blocked provenance saves identically");
                var denied = New(Fixture(publicPolicy, publicWorker, publicSource, root => Source(root, publicWorker, publicSource).Set("reserve_use", JsonValue.Of("none"))));
                c.That(!Read(denied, publicWorker).Target.IsValid, "ordinary recurring origin alone cannot borrow reserve");
                c.Equal(4, Reserve(denied).ReadyWorkers, "refused service does not consume readiness");
            }
        }
        private static void Physical(Check c)
        {
            foreach (bool publicPolicy in new[] { false, true }) foreach (bool publicWorker in new[] { false, true }) foreach (bool publicSource in new[] { false, true })
            {
                var factory = Fixture(publicPolicy, publicWorker, publicSource); var game = New(factory); var untouched = New(factory);
                ulong untouchedHash = untouched.Hash(); var clock = game.Clock; Entity actor = Machine(game, publicWorker);
                Enabled(c, game, publicWorker, true);
                Until(game, () => SourceView(game, publicWorker).Schedule.LastPlan > 0, "actual recurring admission");
                long plan = SourceView(game, publicWorker).Schedule.LastPlan;
                c.Equal(actor.Id, Plan(game, plan).Origin.Entity, "engine admits an actual worker origin");
                c.Equal(Service, Plan(game, plan).Origin.Source, "origin names the actual selected source");
                Enabled(c, game, publicWorker, false); // Disable future posts, not this admitted physical project.
                Until(game, () => actor.Get<IWorker>().Job != 0, "reserved self-service machine assignment");
                Job job = Board(game).Job(actor.Get<IWorker>().Job);
                c.That(!Board(game).IsEmergency(job), "maintenance never fabricates emergency identity");
                c.Equal(3, Reserve(game).ReadyWorkers, "busy maintenance worker is not reported ready despite exemption");
                c.Equal(Fix.FromInt(4), Condition(game, Actor(publicWorker)), "assignment does not instantly repair");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "active service and original provenance restore exactly");
                game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash();
                game.Update(); twin.Update(); c.Equal(paused, game.Hash(), "the single paused clock holds service and travel");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, () => Plan(game, plan).State == PlanState.Done, "physical service/departure completion", twin);
                c.Equal(Fix.FromInt(10), Condition(game, Actor(publicWorker)), "real machine completes repair through worker condition effect");
                c.Equal(Fix.FromInt(29), Stock(game, "source"), "physical maintenance consumes exactly three spares, releases held tools");
                c.Equal(Fix.FromInt(32), Stock(game, "destination"), "unselected facility stock is untouched");
                c.Equal(4, Reserve(game).ReadyWorkers, "completed unheld worker returns to real ready count");
                c.Equal(game.Hash(), twin.Hash(), "saved native/public service twins remain identical");
                c.That(ReferenceEquals(clock, game.Clock), "the same game clock owns the complete lifecycle");
                c.Equal(untouchedHash, untouched.Hash(), "interleaved independent session receives no maintenance state");

                var cancel = New(factory); Enabled(c, cancel, publicWorker, true);
                Until(cancel, () => SourceView(cancel, publicWorker).Schedule.LastPlan > 0 && Machine(cancel, publicWorker).Get<IWorker>().Job != 0, "cancellable real service admission");
                long firstPlan = SourceView(cancel, publicWorker).Schedule.LastPlan;
                Entity cancelActor = Machine(cancel, publicWorker); var cancelPolicy = WorkPolicyRules.Read(cancelActor);
                cancel.Seat(0).Submit(new ConfigureWorkPolicyCommand { Worker = cancelActor.Id,
                    Settings = new WorkPolicySettings(cancelPolicy.Enabled, cancelPolicy.Priority, cancelPolicy.AllowedRoles,
                        cancelPolicy.Area, cancelPolicy.Interruptible, cancelPolicy.EmergencyEligible, WorkAssignmentMode.EmergencyOnly) });
                c.That(Command(cancel, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { firstPlan }) }).Accepted, "player cancels the actual maintenance plan");
                c.Equal(WorkAssignmentMode.EmergencyOnly, WorkPolicyRules.Read(cancelActor).AssignmentMode, "same update applies standby before cancel/reposting decisions");
                c.Equal(PlanState.Cancelled, Plan(cancel, firstPlan).State, "cancellation ends the original plan, not a copied service state");
                c.Equal(Fix.FromInt(4), Condition(cancel, Actor(publicWorker)), "early cancellation grants no repair");
                c.Equal(Fix.FromInt(32), Stock(cancel, "source"), "unspent service claims release without consuming spares");
                for (int i = 0; i < 3; i++) Step(cancel);
                c.Equal(firstPlan, SourceView(cancel, publicWorker).Schedule.LastPlan, "cancelled service cannot reacquire under pure emergency-only mode");
                Policy(c, cancel, publicWorker, WorkAssignmentMode.EmergencyAndSelfService);
                Until(cancel, () => SourceView(cancel, publicWorker).Schedule.LastPlan > firstPlan && Machine(cancel, publicWorker).Get<IWorker>().Job != 0, "fresh authorized maintenance repost");
                long secondPlan = SourceView(cancel, publicWorker).Schedule.LastPlan;
                Enabled(c, cancel, publicWorker, false);
                Policy(c, cancel, publicWorker, WorkAssignmentMode.EmergencyOnly);
                var cancelTwin = factory.Load(cancel.Save());
                Until(cancel, () => Plan(cancel, secondPlan).State == PlanState.Done, "real incumbent may finish after service mode is removed", cancelTwin);
                c.Equal(Fix.FromInt(10), Condition(cancel, Actor(publicWorker)), "reposted actual worker completes exactly one repair");
                c.Equal(Fix.FromInt(29), Stock(cancel, "source"), "cancel/repost and mode edits spend service inputs exactly once");
                c.Equal(cancel.Hash(), cancelTwin.Hash(), "interrupted policy/provenance continuation saves identically");
            }
            var noStock = New(Fixture(edit: root => {
                root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number("2"));
                root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number("2")); }));
            c.That(!Read(noStock).Target.IsValid, "maintenance exemption cannot override missing spares/tools");
            Enabled(c, noStock, false, true); Step(noStock);
            c.Equal(0L, SourceView(noStock).Schedule.LastPlan, "unsafe service earns no posting or free material");
        }
    }
}
