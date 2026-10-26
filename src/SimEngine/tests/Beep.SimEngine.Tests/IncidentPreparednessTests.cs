using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;

namespace Beep.SimEngine.Tests
{
    internal static class IncidentPreparednessTests
    {
        private static readonly Code Source = new Code("damage"), Kind = new Code("proof.failure");
        private static readonly WorkerRoleId Haul = new WorkerRoleId("haul"), Inspect = new WorkerRoleId("inspect");
        internal static void Register()
        {
            TestRunner.Add("Incident preparedness: authored response, private DAG facts and distinct frontier", Contracts);
            TestRunner.Add("Incident preparedness: physical response, live blockers and saved continuation", Physical);
        }
        internal static GameFactory Fixture(bool nativeIncident = false, bool publicPolicy = false, bool publicWorker = false,
            int[] fleet = null, Action<JsonValue> document = null, Action<JsonValue> edit = null,
            string documentName = "incident-preparedness.json")
        {
            fleet = fleet ?? (publicWorker ? new[] { 2, 1 } : new[] { 3, 1 });
            var author = Json.Parse(FrameworkProofContent.Read(documentName)); document?.Invoke(author);
            return WorkerStandbyTests.Fixture(publicPolicy, publicWorker, edit: root =>
            {
                root.Get("scenarios")[0].Get("settings").Set(CompanyAlertModule.ModuleId, Json.Parse(FrameworkProofContent.Read("distinct-worker-reserves.json")));
                var target = root.Get("entities")[1].Get("components"); target.Get("point_stock").Set("initial", JsonValue.Number("8"));
                if (nativeIncident)
                { target.Remove("contract_incident"); target.Set("incident", Json.Parse("{\"id\":\"damage\",\"workflow\":\"prepared.workflow\",\"states\":{\"active\":\"stopped\"}}")); }
                else target.Get("contract_incident").Set("response", JsonValue.Of("prepared.response"));
                var original = root.Get("entities")[2]; SetRoles(original, fleet[0], publicWorker);
                for (int i = 1; i < fleet.Length; i++)
                {
                    var actor = original.Clone(); actor.Set("id", JsonValue.Of("prepared.worker." + i)); SetRoles(actor, fleet[i], publicWorker);
                    root.Get("entities").Add(actor); StartupContentAuthoring.Add(root, "prepared.worker." + i, "prepared.worker." + i, 0, actor.Get("components").Get("point_presence"));
                }
                edit?.Invoke(root);
            }, workAuthor: root =>
            {
                foreach (string section in new[] { "activities", "target_resource_requirements", "work_requirement_groups" })
                    if (author.Has(section))
                        for (int i = 0; i < author.Get(section).Count; i++) root.Get(section).Add(author.Get(section)[i]);
                root.Set("incident_workflows", author.Get("incident_workflows"));
            });
        }
        private static void SetRoles(JsonValue actor, int roles, bool extension)
        {
            if (extension)
            {
                if (roles != 1 && roles != 2) throw new ArgumentException("public fixture worker declares one explicit role");
                actor.Get("components").Get("contract_worker").Set("role", JsonValue.Of(roles == 1 ? "haul" : "inspect"));
            }
            else
            {
                var authored = Json.Parse("[]"); if ((roles & 1) != 0) authored.Add(JsonValue.Of("haul")); if ((roles & 2) != 0) authored.Add(JsonValue.Of("inspect"));
                actor.Get("components").Get("worker").Set("roles", authored);
            }
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 607 }; request.Mods.Add("proof.shipments");
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static IncidentPreparednessSnapshot Read(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<IncidentsView>().ReadPreparedness(Find(game, "destination").Id, Source, out var result);
            if (!code.IsNone) throw new InvalidOperationException("preparedness refused: " + code); return result;
        }
        private static IncidentPreparednessNode Node(IncidentPreparednessSnapshot facts, string id)
        { foreach (var node in facts.Nodes) if (node.Id == id) return node; throw new InvalidOperationException("preparedness node missing: " + id); }
        private static void Step(SimGame game, SimGame twin = null)
        { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("preparedness game update fault: " + game.Fault + "/" + twin?.Fault); }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) Step(game, twin);
            if (!done())
            {
                var facts = new List<string>(); foreach (var job in Board(game).Jobs) facts.Add(job.Id + "/" + job.State + "/" + job.Blocker + "/" + job.Worker + "/" + job.ToilIndex);
                throw new InvalidOperationException(stage + " timeout at game time " + game.Clock.TotalGameTime + ": " + string.Join(";", facts));
            }
        }
        private static void Policy(Check c, SimGame game, string area = null, bool eligible = true, IReadOnlyList<WorkerRoleId> roles = null)
        {
            var actor = Find(game, "machine"); var prior = WorkPolicyRules.Read(actor);
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = actor.Id, Settings = new WorkPolicySettings(prior.Enabled,
                prior.Priority, roles ?? prior.AllowedRoles, area, prior.Interruptible, eligible, prior.AssignmentMode) }).Accepted, "player changes actual preparedness policy");
        }
        private static void Contracts(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual preparedness semantics identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "clean current save identity");
            foreach (bool native in new[] { false, true }) foreach (bool policy in new[] { false, true }) foreach (bool worker in new[] { false, true })
            {
                var factory = Fixture(native, policy, worker); var game = New(factory); var target = Find(game, "destination"); var incident = target.Get<IIncidentResponse>(Source);
                c.That(!ResponseWork.Read(incident).Active && incident.ResponsePlan != null, "native/public inactive provider exposes authored response without inventing an incident");
                ulong hash = game.Hash(); long time = game.Clock.StepIndex; var facts = Read(game);
                c.That(facts.Prospective && facts.OrderId == 0 && !facts.OrderState.HasValue, "never-raised preparedness has no fake work record");
                c.Equal(3, facts.Nodes.Count, "actual authored DAG supplies all node IDs");
                c.Equal(2, facts.FrontierNodes, "only dependency-free nodes are current frontier");
                c.Equal(2, facts.FrontierWorkersRequired, "parallel roles need two distinct current machines");
                c.Equal(2, facts.FrontierWorkersCovered, "maximum coverage reassigns flexible first machine");
                c.That(facts.ReadyForFrontier, "existing route/requirements/roles pass prospective frontier checks");
                c.Equal(Find(game, "prepared.worker.1").Id, Node(facts, "a.flex").CoveredWorker, "haul-only machine frees the compatible inspector");
                c.Equal(Find(game, "machine").Id, Node(facts, "b.inspect").CoveredWorker, "sole inspector covers actual inspection node");
                c.That(!Node(facts, "restart").Frontier && Node(facts, "restart").Dependencies.Count == 2, "future dependent stage is not prematurely current work");
                c.That(ReferenceEquals(Board(game), game.World.Services.Get<IIncidentPreparedness>()), "preparedness aliases existing board, not a second planner");
                c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "no new DAG or readiness state codec");
                c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "single current candidate selector semantics");
                c.Equal(time, facts.ObservedUpdate, "read observations derive from sole game clock");
                c.Equal(hash, game.Hash(), "preparedness does not raise/post/preempt/reserve/consume"); c.Equal(time, game.Clock.StepIndex, "no alternate preparedness time advances");
                c.Throws<NotSupportedException>(() => ((IList<IncidentPreparednessNode>)facts.Nodes).Clear(), "snapshot nodes are immutable");
                c.Throws<NotSupportedException>(() => ((IList<string>)Node(facts, "restart").Dependencies).Clear(), "dependency copies are immutable");
                c.Throws<NotSupportedException>(() => ((IList<WorkWorkerCandidate>)Node(facts, "a.flex").Candidates).Clear(), "candidate copies are immutable");
                c.Equal(Codes.NotFound, game.Seat(1).View.Feature<IncidentsView>().ReadPreparedness(target.Id, Source, out var hidden), "foreign incident facts remain private");
                c.That(hidden == null, "private refusal leaks no partial DAG");
                c.Equal(IncidentCodes.NotResponse, game.Seat(0).View.Feature<IncidentsView>().ReadPreparedness(target.Id, new Code("missing"), out _), "unknown named source refuses");
                c.Equal(Codes.InvalidArgument, game.Seat(0).View.Feature<IncidentsView>().ReadPreparedness(target.Id, Code.None, out _), "source identity must be explicit");
                Policy(c, game, area: "reserve.home"); c.That(!Read(game).ReadyForFrontier, "target outside actual worker area blocks inspection preparedness");
                c.Equal(WorkPolicyCodes.OutsideArea, Node(Read(game), "b.inspect").Criteria.Blocker, "shared policy exposes typed target area blocker");
                c.That(facts.ReadyForFrontier && Node(facts, "b.inspect").Criteria.Blocker.IsNone, "retained prospective facts do not change with live policy");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "prospective policy saves without retained preparedness state");
            }
            var single = New(Fixture(fleet: new[] { 3 })); var shortage = Read(single);
            c.Equal(1, shortage.FrontierWorkersCovered, "one multi-role actor cannot cover both parallel nodes"); c.Equal(1, shortage.FrontierWorkerShortfall, "real parallel worker shortage");
            c.That(!shortage.ReadyForFrontier, "individually ready candidates are not two distinct workers");
            var sequential = New(Fixture(fleet: new[] { 3 }, document: root => root.Get("activities")[0].Get("nodes")[1].Set("after", Json.Parse("[\"a.flex\"]"))));
            c.That(Read(sequential).ReadyForFrontier && Read(sequential).FrontierWorkersRequired == 1, "sequential nodes can reuse a real actor later without demanding a second now");
            var timed = New(Fixture(document: root => {
                root.Get("activities")[0].Get("nodes")[0].Set("roles", Json.Parse("[]"));
                root.Get("activities")[0].Get("nodes")[1].Set("after", Json.Parse("[\"a.flex\"]")); }));
            c.That(Read(timed).ReadyForFrontier && Read(timed).FrontierWorkersRequired == 0, "authored timed frontier needs no fabricated machine while future machine nodes stay dependent");
            var dry = New(Fixture(edit: root => root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number("0"))));
            c.That(!Read(dry).ReadyForFrontier && Node(Read(dry), "b.inspect").Criteria.Status != WorkRequirementStatus.Satisfied, "missing target input is a real requirement blocker");
            var material = Node(Read(dry), "b.inspect").Criteria.ResourceCandidates;
            c.That(material.Count > 0 && material[0].Availability.Count > 0, "typed per-source shortage quantities are retained through existing resource facts");
            c.Equal(Fix.Zero, material[0].Availability[0].Available, "actual target source has no material");
            c.Equal(Fix.FromInt(4), material[0].Availability[0].Required, "actual authored physical input bill is four");
            var route = New(Fixture()); c.That(TestKit.Order(route, 0, new SetPointAccessCommand { Open = false }).Accepted, "player closes actual route");
            c.Equal(0, Read(route).FrontierWorkersCovered, "unreachable target is not ready despite company role capacity");
            c.That(!Node(Read(route), "a.flex").Criteria.Blocker.IsNone, "shared route/dock evaluator returns typed blocker");
            var disabled = New(Fixture()); Policy(c, disabled, eligible: false); c.That(!Read(disabled).ReadyForFrontier, "non-emergency-eligible inspector excluded");
            var zero = New(Fixture()); c.That(WorkerExecutionTestContent.Control(zero, Find(zero, "machine").Id, true, Fix.Zero).Accepted, "player zeroes real work execution");
            c.That(!Read(zero).ReadyForFrontier, "zero-rate actor cannot prepare a physical response");
            c.Equal(WorkSystem.BlockerWorkerUnavailable, Node(Read(zero), "b.inspect").Criteria.Blocker, "positive execution admission is shared, not preparedness-specific filtering");
            var actor = Find(zero, "machine"); var settings = WorkPolicyRules.Read(actor);
            c.That(TestKit.Order(zero, 0, new ConfigureWorkPolicyCommand { Worker = actor.Id, Settings = new WorkPolicySettings(settings.Enabled,
                settings.Priority, settings.AllowedRoles, settings.Area, settings.Interruptible, settings.EmergencyEligible, WorkAssignmentMode.All) }).Accepted, "enable ordinary dispatch explicitly");
            c.That(TestKit.Order(zero, 0, new ConfigureCompanyAlertCommand { Company = actor.Owner, Settings = new CompanyAlertSettings("alerts.neutral",
                CompanyAlertLevel.Normal, Array.Empty<WorkerReserveSetting>()) }).Accepted, "remove authored floor through current command");
            var refused = Post(c, zero);
            c.Equal(0, actor.Get<IWorker>().Job, "normal dispatch cannot acquire a zero-rate actor even without reserve policy");
            c.Equal(WorkSystem.BlockerWorkerUnavailable, Board(zero).Job(refused.Node("work").JobId).Blocker, "ordinary candidate selector uses same zero-rate admission blocker");
            c.Equal(Fix.FromInt(20), Find(zero, "source").Get<IInletPort>().Amount(Supply(zero)), "zero-rate refusal does not consume physical work bill");
            var ordinary = New(Fixture()); Post(c, ordinary);
            c.Equal(0, Find(ordinary, "machine").Get<IWorker>().Job, "prospective response view does not authorize ordinary reserve use");
            c.That(Read(ordinary).ReadyForFrontier, "authored prospective response can inspect reserved actors without taking them");
            SharedMatching(c);
            c.Throws<ContentException>(() => New(Fixture(document: root => root.Get("activities")[0].Get("nodes")[0].Set("after", Json.Parse("[\"restart\"]")))), "cycles fail in sole authored DAG compiler");
            c.Throws<InvalidOperationException>(() => Read(New(Fixture(publicPolicy: true, edit: root => root.Get("entities")[2].Get("components").Get("proof_work_policy").Set("fault", JsonValue.Number("3"))))), "impure provider fails existing scoped read guard");
        }
        private static void SharedMatching(Check c)
        {
            var a = new EntityId(1); var b = new EntityId(2); var d = new EntityId(3);
            var edges = new IReadOnlyList<EntityId>[] { new[] { a, b }, new[] { b, d }, new[] { a } };
            var matches = DistinctWorkerMatching.Solve(new[] { 1, 1, 1 }, edges);
            c.Equal(3, matches.Count, "shared maximum matching supports three-demand augmenting chains");
            c.Equal(2, matches[0].Demand, "stable first worker serves uniquely compatible third demand");
            c.Equal(0, matches[1].Demand, "second worker is reassigned through first demand");
            c.Equal(1, matches[2].Demand, "third worker completes forced three-demand augmenting chain");
            c.Equal(3, DistinctWorkerMatching.Solve(new[] { int.MaxValue, int.MaxValue, int.MaxValue }, edges).Count, "large capacities never allocate a requested-slot graph");
            c.Throws<ArgumentException>(() => DistinctWorkerMatching.Solve(new[] { -1 }, new IReadOnlyList<EntityId>[] { new[] { a } }), "negative capacity refuses");
            c.Throws<ArgumentException>(() => DistinctWorkerMatching.Solve(new[] { 1 }, new IReadOnlyList<EntityId>[] { new[] { a, a } }), "duplicate compatible identity refuses");
            c.Throws<ArgumentException>(() => DistinctWorkerMatching.Solve(new[] { 1 }, new IReadOnlyList<EntityId>[] { new[] { EntityId.None } }), "invalid compatible identity refuses");
            var overlap = New(Fixture(fleet: new[] { 3, 3 })); var reserve = overlap.World.Services.Get<ICompanyWorkerReservePolicy>(); reserve.ReadCoverage(new PlayerId(0), out var coverage);
            c.Equal(2, coverage.CoveredWorkers, "D12 company floor coverage reuses shared algorithm");
            foreach (var entity in overlap.World.Entities.All) if (entity.Has<IWorker>())
                c.Equal(CompanyWorkerReserveCodes.MinimumReady, reserve.CheckOrdinaryAssignment(entity), "D12 protected overlap admission remains enforced");
        }
        private static void Physical(Check c)
        {
            foreach (bool native in new[] { false, true }) foreach (bool policy in new[] { false, true }) foreach (bool worker in new[] { false, true })
                Response(c, native, policy, worker);
            LiveRecheck(c); Cancel(c);
        }
        private static void Response(Check c, bool native, bool policy, bool worker)
        {
            var factory = Fixture(native, policy, worker); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash(); var clock = game.Clock;
            var before = Read(game); Order order = Raise(c, game); var active = Read(game);
            c.That(!active.Prospective && active.OrderId == order.Id && active.Incident.Active, "real incident points to same authoritative native order");
            c.That(order.State != OrderState.Done && Node(active, "restart").State != NodeState.Done, "raising incident does not instantly repair");
            c.That(!Node(active, "restart").Frontier, "uncompleted predecessors block future restart frontier");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "active response save retains only real episode/work state");
            game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash(); long time = game.Clock.StepIndex;
            Step(game, twin); Read(game); c.Equal(paused, game.Hash(), "one paused clock freezes episode, material and work");
            c.Equal(time, game.Clock.StepIndex, "preparedness read has no timer"); game.Clock.Resume(); twin.Clock.Resume();
            Until(game, () => Node(Read(game), "restart").Frontier, "real completed parallel predecessors expose restart frontier", twin);
            var final = Read(game); c.Equal(1, final.FrontierNodes, "remaining current frontier derives from actual node completion");
            c.Equal(1, final.FrontierWorkersRequired, "later sequential stage may reuse released machine");
            c.Equal(NodeState.Done, Node(final, "a.flex").State.Value, "first physical stage completed in sole graph");
            c.Equal(NodeState.Done, Node(final, "b.inspect").State.Value, "second physical stage completed in sole graph");
            c.Equal(Fix.FromInt(4), Find(game, "destination").Get<IInletPort>().Amount(Supply(game)), "repair consumed four physical inputs once");
            Until(game, () => order.State == OrderState.Done, "actual response work and completion effect", twin);
            c.That(!Find(game, "destination").Get<IIncidentResponse>(Source).ReadIncident().Active, "real response effect resolves current episode");
            c.That(Read(game).Prospective && Read(game).OrderId == 0, "next prospective episode is not stale completed execution");
            c.That(before.Prospective && before.OrderId == 0, "retained pre-incident facts never become live orders");
            c.Equal(game.Hash(), twin.Hash(), "saved physical response/frontier continuation is exact");
            c.Equal(untouched, isolated.Hash(), "another game receives no response or prepared state");
            c.That(ReferenceEquals(clock, game.Clock), "one unchanged clock owns entire lifecycle");
        }
        private static void LiveRecheck(Check c)
        {
            var factory = Fixture(fleet: new[] { 3 }); var game = New(factory); var prospective = Read(game); Policy(c, game, area: "reserve.home");
            Order order = Raise(c, game); c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "actual dispatch rechecks changed area instead of trusting retained preview");
            c.That(!Read(game).ReadyForFrontier, "current active report reflects changed target eligibility"); Policy(c, game);
            Until(game, () => Find(game, "machine").Get<IWorker>().Job > 0, "real worker dispatch after policy repair");
            var actor = Find(game, "machine"); c.That(WorkerExecutionTestContent.Control(game, actor.Id, true, Fix.Zero).Accepted, "pause actual acquired work capacity");
            c.That(!Read(game).ReadyForFrontier, "active incumbent's execution blocker is not hidden by another offer");
            var twin = factory.Load(game.Save()); c.That(WorkerExecutionTestContent.Control(game, actor.Id, true, Fix.One).Accepted, "restore actual work rate");
            c.That(WorkerExecutionTestContent.Control(twin, actor.Id, true, Fix.One).Accepted, "same player rate restoration in saved twin");
            Until(game, () => order.State == OrderState.Done, "one machine physically sequences parallel intent without fake capacity", twin);
            c.Equal(1, prospective.FrontierWorkerShortfall, "prospective distinct shortage does not promise or prohibit eventual sequential completion");
            c.Equal(game.Hash(), twin.Hash(), "execution pause/restore/continued response deterministic");
        }
        private static void Cancel(Check c)
        {
            var factory = Fixture(); var game = New(factory); Order order = Raise(c, game); var target = Find(game, "destination");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "player cancellation uses existing response command");
            // An active incident legitimately resumes its existing order in the same update, never another response.
            c.Equal(order.Id, Read(game).OrderId, "response repost/resume retains exact episode order identity");
            var provider = target.Get<ContractIncident>(Source); provider.Renew(game.World); // Explicit stale-episode fault injection, not gameplay implementation.
            ulong before = game.Hash(); var facts = Read(game);
            c.Equal(0, facts.OrderId, "new actual episode never presents older response as its execution");
            c.Equal(before, game.Hash(), "new-episode query does not cancel or post work itself");
            var twin = factory.Load(game.Save()); Step(game, twin);
            c.That(Read(game).OrderId != order.Id && Read(game).Incident.Episode == facts.Incident.Episode, "next actual update posts correctly bound new response");
            c.Equal(game.Hash(), twin.Hash(), "renewed episode and old-order cleanup restore exactly");
        }
    }
}
