using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class FacilitySelectionTests
    {
        private static readonly Code Service = new Code("fleet.service"), Integrity = new Code("integrity");
        internal static void Register()
        {
            TestRunner.Add("Service facilities: authored policies, private candidates and deterministic selection", Contracts);
            TestRunner.Add("Service facilities: physical repair, replanning and saved continuation", Physical);
        }
        internal static JsonValue Parts(JsonValue root, bool extension = false) => root.Get("entities")[extension ? 3 : 2].Get("components");
        private static JsonValue Source(JsonValue root, bool extension = false) => Parts(root, extension).Get("recurring_activity");
        internal static GameFactory Factory(bool extension = false, string policy = "service.nearest", Action<JsonValue> edit = null, string alertProfiles = null, string facilityDocument = null, string facilityDefinitions = null,
            IReadOnlyList<GameModule> additionalModules = null, JsonValue additionalDeclarations = null, IReadOnlyDictionary<string,string> additionalDocuments = null)
            => WorkerServiceTests.Factory(extension, root =>
            {
                root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number("32"));
                Parts(root, extension).Set("recurring_activity", Json.Parse("{\"id\":\"fleet.service\",\"plan\":\"activity:facility.service\",\"template\":null,\"target\":\"service_facility\",\"arguments\":null,\"cadence\":\"submission\",\"trigger\":\"schedule\",\"dispatch_mode\":\"compete\",\"facility_policy\":\"" + policy + "\",\"reserve_use\":\"none\",\"worker_nodes\":[\"service\"],\"enabled\":false,\"priority\":60,\"group\":\"fleet.service\",\"operation_policy\":null,\"interval\":{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"},\"conditions\":[{\"id\":\"damaged\",\"source\":\"integrity\",\"signal\":\"facility.integrity\",\"unit\":\"unit.condition\",\"minimum\":0,\"maximum\":4,\"blocker\":\"facility.not-due\"}]}"));
                edit?.Invoke(root);
            }, activityDocument: facilityDocument ?? "service-facilities.json", alertProfiles: alertProfiles, activityDefinitions: facilityDefinitions, additionalModules:additionalModules, additionalDeclarations:additionalDeclarations, additionalDocuments:additionalDocuments);
        internal static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 1007 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        internal static string Actor(bool extension) => extension ? "machine.b" : "machine.a";
        internal static Entity Find(SimGame game, string id)
        {
            foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity;
            throw new InvalidOperationException("service facility entity missing: " + id);
        }
        internal static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        internal static WorkView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        internal static ActivityFacilitySelectionView Read(SimGame game, bool extension = false)
        {
            Code refusal = View(game).ReadRecurringFacilities(Find(game, Actor(extension)).Id, Service, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("private facility selection refused: " + refusal); return value;
        }
        internal static RecurringPlanView SourceView(SimGame game, bool extension = false)
        {
            Code refusal = View(game).ReadRecurringPlan(Find(game, Actor(extension)).Id, Service, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("private facility source refused: " + refusal); return value;
        }
        internal static PlanView Plan(SimGame game, long id)
        {
            var plans = new List<PlanView>(); View(game).CopyPlans(plans);
            foreach (var plan in plans) if (plan.Id == id) return plan; throw new InvalidOperationException("service facility plan missing: " + id);
        }
        internal static Fix Condition(SimGame game, string id) => ConditionContracts.Read(game.World, Find(game, id).Get<IConditionProvider>(Integrity)).Value;
        internal static Fix Stock(SimGame game, string id) => Find(game, id).Get<IInletPort>().Amount(game.World.Content.ResourceId("supply"));
        internal static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("service facility game update failed: " + game.Fault + "/" + twin?.Fault);
        }
        internal static CommandResult Command(SimGame game, Command command)
        {
            int sequence = game.Seat(0).Submit(command); Step(game);
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            foreach (var result in results) if (result.Sequence == sequence) return result; throw new InvalidOperationException("facility command answer missing");
        }
        private static void Enable(Check c, SimGame game, bool enabled, bool extension = false)
            => c.That(Command(game, new ConfigureRecurringPlanCommand { Entity = Find(game, Actor(extension)).Id, Source = Service,
                Settings = new RecurringPlanSettings(enabled, 60, GameDuration.Minutes(1), SourceView(game, extension).Schedule.Settings.FacilityPolicy) }).Accepted, "normal recurring command drives engine-owned facility intent");
        private static void Home(Check c, SimGame game, string home, bool extension = false)
            => c.That(Command(game, new SetHomeCommand { Worker = Find(game, Actor(extension)).Id, Home = Find(game, home).Id }).Accepted,
                "normal home command changes preference input only");
        internal static void Reach(SimGame game, Func<bool> complete, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !complete(); i++) Step(game, twin);
            if (!complete())
            {
                var facts = new List<string>(); foreach (var order in Board(game).Orders) foreach (var node in order.Nodes)
                    facts.Add(order.Id + "/" + order.Target + "/" + node.Id + "/" + node.State + "/" + node.Blocker + "/" + node.Progress);
                throw new InvalidOperationException(stage + " timeout at game time " + game.Clock.TotalGameTime + ": " + string.Join(";", facts));
            }
        }
        private static void Contracts(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current facility query identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "clean content/protocol replacement");
            foreach (bool extension in new[] { false, true })
            {
                var factory = Factory(extension); var game = New(factory); ulong hash = game.Hash(); var read = Read(game, extension);
                c.That(ReferenceEquals(Board(game), game.World.Services.Get<IActivityFacilitySelection>()), "facility contract aliases existing board");
                c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "plan/node/job payload unchanged");
                c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "existing work owner declares facility request semantics");
                c.Equal(Find(game, "source").Id, read.Target, "nearest actually ready facility selected with no assigned home");
                c.Equal(2, read.Candidates.Count, "only actual owned docks are candidates, not arbitrary machines");
                c.That(read.Candidates[0].Ready && read.Candidates[1].Ready && read.Candidates[0].TravelCost < read.Candidates[1].TravelCost,
                    "cost comes from actual shared worker/dock query");
                c.Equal(Find(game, Actor(extension)).Id, read.Worker, "exact subject retained through detached selection");
                c.Equal(hash, game.Hash(), "selection preview acquires nothing and remains pure");
                c.Equal(0, Find(game, Actor(extension)).Get<IWorker>().Job, "readiness is not a worker reservation");
                c.Equal(Fix.FromInt(32), Stock(game, "source"), "preview cannot consume spares");
                c.Throws<NotSupportedException>(() => ((IList<ActivityFacilityCandidate>)read.Candidates).Clear(), "candidate facts are immutable");
                c.Throws<InvalidOperationException>(() => read.Request.Intent.Arguments.Set("target", JsonValue.Number("99")), "selected intent is frozen");
                c.Equal(Codes.NotFound, View(game, 1).ReadRecurringFacilities(Find(game, Actor(extension)).Id, Service, out var hidden), "foreign facilities never leak");
                c.That(hidden == null, "private refusal clears candidate facts");
                c.Equal(Codes.InvalidArgument, View(game).ReadRecurringFacilities(Find(game, Actor(extension)).Id, Code.None, out _), "explicit source identity required");
                c.Equal(RecurringPlanCodes.NotSource, View(game).ReadRecurringFacilities(Find(game, Actor(extension)).Id, new Code("missing"), out _), "unknown source has no substitution");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "pure selected request restores through existing owner codecs");
                var other = New(factory);
                c.Throws<ArgumentException>(() => Board(other).ReadFacilities(Find(game, Actor(extension)).Get<IRecurringPlanSource>(Service)), "wrong-session source cannot query same numeric identities");
                Entity foreign = Find(game, "destination"); foreign.Owner = game.Seat(1).Id;
                c.Equal(1, Read(game, extension).Candidates.Count, "foreign endpoint details excluded before providers run"); foreign.Owner = game.Seat(0).Id;
                Enable(c, game, true, extension); long id = SourceView(game, extension).Schedule.LastPlan;
                c.That(id > 0, "ordinary recurring admission creates actual plan");
                c.Equal(Find(game, "source").Id, Plan(game, id).Target, "chosen target becomes sole actual order target");
                c.Equal(read.Request.Intent.EncodedArguments(), Plan(game, id).Intent.EncodedArguments(), "preview/engine posting parity");
                c.Equal(Find(game, Actor(extension)).Id, Plan(game, id).Origin.Entity, "selected plan retains actual source origin");
                c.Equal(Find(game, "source").Id, read.Target, "retained facts cannot change after posting");
            }
            Invalid(c); Preferences(c); Refusals(c); PublicPolicy(c);
        }
        private static void Invalid(Check c)
        {
            foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                root => Source(root).Remove("facility_policy"), root => Source(root).Set("facility_policy", JsonValue.Null()),
                root => Source(root).Set("facility_policy", JsonValue.Of("missing")), root => Source(root).Set("facility_policy", JsonValue.Number("1")),
                root => Source(root).Set("target", JsonValue.Of("worker_home")), root => Source(root).Set("worker_nodes", JsonValue.NewArray()),
                root => { Source(root).Set("facility_policy", JsonValue.Of("bad")); root.Set("activity_facility_policies", Json.Parse("[{\"id\":\"bad\",\"node\":\"missing\",\"prefer_home\":true}]")); }
            }) c.Throws<ContentException>(() => New(Factory(edit: edit)), "malformed facility binding refuses before play");
            foreach (string body in new[] { "{\"id\":\"bad\",\"prefer_home\":false}", "{\"id\":\"bad\",\"node\":\"service\"}",
                "{\"id\":\"bad\",\"node\":\"settle\",\"prefer_home\":false}", "{\"id\":\"bad\",\"node\":\"missing\",\"prefer_home\":true}",
                "{\"id\":\"bad\",\"node\":\"service\",\"prefer_home\":false,\"fallback\":true}" })
                c.Throws<ContentException>(() => New(Factory(policy: "bad", edit: root => root.Set("activity_facility_policies", Json.Parse("[" + body + "]")))), "strict authored probe/ranking policy refuses");
            var homeFactory = HomeServiceTests.Factory(); var home = New(homeFactory);
            c.Equal(ActivityFacilityCodes.NotSelected, View(home).ReadRecurringFacilities(Find(home, "machine.a").Id, new Code("home.service"), out _), "home policy does not acquire an implicit search fallback");
        }
        private static void Preferences(Check c)
        {
            var tie = New(Factory(edit: root => StartupContentAuthoring.Position(root, "machine.a", JsonValue.Number("6"))));
            var offers = Read(tie); c.Equal(offers.Candidates[0].TravelCost, offers.Candidates[1].TravelCost, "fixture earns an actual route tie");
            c.Equal(Find(tie, "source").Id, offers.Target, "equal ready costs use stable entity identity");
            var preferred = New(Factory(policy: "service.home-first")); Home(c, preferred, "destination");
            var result = Read(preferred); c.Equal(Find(preferred, "destination").Id, result.Target, "ready home preference precedes travel cost");
            c.That(result.Candidates[0].TravelCost < result.Candidates[1].TravelCost, "preference is not accidental nearest selection");
            var poor = New(Factory(policy: "service.home-first", edit: root => root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number("2"))));
            Home(c, poor, "destination"); var actual = Read(poor);
            c.Equal(Find(poor, "source").Id, actual.Target, "home preference cannot override missing tools/spares");
            c.That(!actual.Candidates[1].Ready && !actual.Candidates[1].Readiness.Blocker.IsNone, "blocked preferred facility has real resource diagnostics");
            c.That(!actual.Candidates[1].Preference.HasValue, "unsafe candidate never reaches ranking provider");
        }
        private static void Refusals(Check c)
        {
            var factory = Factory(edit: root => {
                root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number("2"));
                root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number("2")); });
            var game = New(factory); var read = Read(game);
            c.Equal(ActivityFacilityCodes.NoFacility, read.Request.Blocker, "no stocked safe facility yields typed refusal"); c.That(read.Request.Intent == null, "no facility means no invented target");
            Enable(c, game, true); Step(game); c.Equal(0L, SourceView(game).Schedule.LastPlan, "refused search earns no posting history");
            c.Equal(0, Board(game).Orders.Count, "refused search creates no execution"); c.Equal(Fix.FromInt(2), Stock(game, "source"), "refusal cannot grant/consume resources");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused search saves no shadow plan");
            var incompatible = New(Factory(edit: root => root.Get("entities")[0].Get("components").Get("point_dock").Set("work_roles", JsonValue.NewArray())));
            var incompatibleRead = Read(incompatible); c.Equal(Find(incompatible, "destination").Id, incompatibleRead.Target, "actual incompatible dock does not win by distance");
            c.Equal(WorkDockCodes.Incompatible, incompatibleRead.Candidates[0].Readiness.Blocker, "same bay contract supplies diagnostic");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "waiting resource diagnostics are pure");
            var areas = New(Factory(edit: root => {
                root.Set("point_interval_regions", Json.Parse("[{\"id\":\"service.area\",\"space\":\"field.points\",\"minimum\":0,\"maximum\":9}]"));
                root.Get("work_policies")[0].Set("area", JsonValue.Of("service.area")); }));
            var areaRead = Read(areas); c.Equal(Find(areas, "source").Id, areaRead.Target, "permitted whole bay journey remains eligible");
            c.Equal(WorkPolicyCodes.OutsideArea, areaRead.Candidates[1].Readiness.Blocker, "same operating-area owner refuses outside facility");
            var closed = New(Factory()); ((PointSpace)closed.World.Space).SetAccess(false);
            ulong closedHash = closed.Hash(); var closedRead = Read(closed);
            c.Equal(ActivityFacilityCodes.NoFacility, closedRead.Request.Blocker, "closed routes cannot admit service by distance alone");
            c.That(!closedRead.Candidates[0].Ready && !closedRead.Candidates[0].Readiness.Blocker.IsNone, "actual route provider supplies failure facts");
            c.Equal(closedHash, closed.Hash(), "route refusal is pure and cannot reopen access");
            ((PointSpace)closed.World.Space).SetAccess(true); c.That(Read(closed).Target.IsValid, "same query replans after actual route becomes available");
            var busy = New(Factory(edit: root => {
                StartupContentAuthoring.Position(root, "machine.b", JsonValue.Number("1"));
                StartupContentAuthoring.Position(root, "machine.c", JsonValue.Number("3"));
                StartupContentAuthoring.Position(root, "machine.d", JsonValue.Number("4")); }));
            var busyRead = Read(busy); c.Equal(Find(busy, "destination").Id, busyRead.Target, "physically occupied bay and waits force another facility");
            c.That(!busyRead.Candidates[0].Ready && !busyRead.Candidates[0].Readiness.Blocker.IsNone, "occupied slots are not treated as empty queue capacity");
            var disabled = New(Factory()); var actor = Find(disabled, "machine.a"); var current = WorkPolicyRules.Read(actor);
            c.That(Command(disabled, new ConfigureWorkPolicyCommand { Worker = actor.Id,
                Settings = new WorkPolicySettings(false, current.Priority, current.AllowedRoles, current.Area, current.Interruptible, current.EmergencyEligible, WorkAssignmentMode.All) }).Accepted,
                "normal work policy controls automatic servicing eligibility");
            c.Equal(WorkPolicyCodes.Disabled, Read(disabled).Candidates[0].Readiness.Blocker, "facility search respects actual disabled worker");
            c.Equal(ActivityFacilityCodes.NoFacility, Read(disabled).Request.Blocker, "disabled source does not substitute another machine");
            c.Throws<InvalidOperationException>(() => Read(New(Factory(edit: root => root.Get("entities")[0].Get("components").Get("point_dock").Set("fault", JsonValue.Of("request-touch"))))),
                "impure actual dock observation faults instead of admitting service");
        }
        private static GameFactory ProofFactory(int fault = 0, Action<JsonValue> edit = null) => Factory(policy: "proof.later", edit: root =>
        {
            root.Set("proof_facility_policies", Json.Parse("[{\"id\":\"proof.later\",\"node\":\"service\",\"prefer_later\":true,\"fault\":" + fault + "}]"));
            edit?.Invoke(root);
        });
        private static void PublicPolicy(Check c)
        {
            var factory = ProofFactory(); var game = New(factory); var read = Read(game);
            c.Equal(Find(game, "destination").Id, read.Target, "unrelated public policy ranks safe candidates without engine edits");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "public content policy has no private mirrored state");
            var excluded = New(ProofFactory(2)); c.Equal(ActivityFacilityCodes.NoFacility, Read(excluded).Request.Blocker, "explicit exclusion cannot invent a preferred default");
            c.Equal(ActivityFacilityCodes.Excluded, Read(excluded).Candidates[0].Readiness.Blocker, "excluded safe offer retains typed reason");
            c.Throws<InvalidOperationException>(() => Read(New(ProofFactory(1))), "negative public rank is a contract fault, not a ranking shortcut");
        }
        private static void Physical(Check c)
        { Repair(c, false); Repair(c, true); CancelReplan(c); }
        private static void Repair(Check c, bool extension)
        {
            var factory = Factory(extension, edit: root => root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number("2")));
            var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash(); string actor = Actor(extension);
            Enable(c, game, true, extension); long id = SourceView(game, extension).Schedule.LastPlan; Order order = Board(game).Order(Plan(game, id).Order);
            c.Equal(Find(game, "destination").Id, order.Target, "farther supplied facility owns actual job destination");
            c.Equal(Fix.FromInt(4), Condition(game, actor), "admission is not instant repair");
            Reach(game, () => order.Node("service").Progress.WholeTicks >= 3, "farther physical facility work begins");
            c.Equal(Fix.FromInt(11), Find(game, actor).Get<PointPresence>().Position, "machine physically reaches selected facility bay");
            c.Equal(Find(game, actor).Id, Board(game).Job(order.Node("service").JobId).Worker, "source worker alone claims selected-node job");
            c.Equal(Fix.FromInt(29), Stock(game, "destination"), "actual selected facility consumes spares");
            c.Equal(Fix.FromInt(2), Stock(game, "source"), "rejected nearer facility stock is untouched");
            c.Equal(Fix.FromInt(4), Condition(game, actor), "partial work cannot repair");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "selected facility and partial physical work save exactly");
            long coordinate = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash(); game.Update(1000000); twin.Update(1000000);
            c.Equal(coordinate, game.Clock.StepIndex, "sole game clock pause stops repair progress"); c.Equal(paused, game.Hash(), "paused update earns no selection/posting/work time");
            game.Clock.Resume(); twin.Clock.Resume(); Home(c, game, "source", extension); Home(c, twin, "source", extension);
            c.Equal(Find(game, "destination").Id, order.Target, "new home never retargets active selected facility");
            c.Equal(id, SourceView(game, extension).Schedule.LastPlan, "busy selected source cannot create overlapping new execution");
            Reach(game, () => Board(game).Job(order.Node("service").JobId)?.ToilIndex == 2, "work earns departure stage", twin);
            c.Equal(Fix.FromInt(4), Condition(game, actor), "earned work awaits actual departure");
            twin = factory.Load(game.Save()); Reach(game, () => order.Node("service").State == NodeState.Done, "actual selected bay departure repairs worker", twin);
            c.Equal(Fix.FromInt(10), Condition(game, actor), "native/public condition owner repaired through same physical effect");
            c.Equal(Find(game, actor).Id, order.Node("service").CompletedBy, "repair receipt identifies selected source worker");
            c.Equal(Fix.FromInt(10), Condition(game, "destination"), "facility condition was not used as repair subject");
            Reach(game, () => Plan(game, id).State == PlanState.Done, "normal DAG settling completes", twin);
            for (int i = 0; i < 8; i++) Step(game, twin);
            c.Equal(id, SourceView(game, extension).Schedule.LastPlan, "healthy source cannot consume more recurring spares");
            c.Equal(Fix.FromInt(29), Stock(game, "destination"), "tools released; no duplicate resource charge");
            c.Equal(game.Hash(), twin.Hash(), "physical facility continuation twins are exact"); c.Equal(untouched, isolated.Hash(), "selection state is session isolated");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "ended origin survives new dynamic selection refusal");
        }
        private static void CancelReplan(Check c)
        {
            var factory = ProofFactory(edit: root => root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number("5")));
            var game = New(factory); Enable(c, game, true);
            long first = SourceView(game).Schedule.LastPlan; Order order = Board(game).Order(Plan(game, first).Order);
            Reach(game, () => order.Node("service").Progress.WholeTicks >= 3, "public preferred facility work begins");
            Enable(c, game, false); c.That(Command(game, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { first }) }).Accepted, "normal cancellation stops partial facility service");
            c.Equal(Fix.FromInt(4), Condition(game, "machine.a"), "cancel cannot apply repair");
            c.Equal(Fix.FromInt(2), Stock(game, "destination"), "cancel cannot refund consumed physical spares");
            c.Throws<InvalidOperationException>(() => game.World.Despawn(Find(game, "destination")), "real stock forbids silent endpoint deletion");
            int hauling = game.World.Services.Get<IHaulExecution>().AcquireHaul(game.Seat(0).Id, Find(game, "destination").Id,
                Find(game, "source").Id, game.World.Content.ResourceId("supply"), Fix.FromInt(2), 80);
            c.That(hauling > 0, "explicit player supply request uses actual engine hauling owner");
            Reach(game, () => Stock(game, "destination").IsZero && Stock(game, "source") == Fix.FromInt(34), "remaining tools physically delivered before removal");
            Reach(game, () => Board(game).Job(hauling)?.State == JobState.Done || Board(game).Job(hauling) == null, "actual supply job releases endpoint");
            // Explicit endpoint-removal diagnostic instrumentation after actual physical stock disposition.
            game.World.Despawn(Find(game, "destination")); game.World.Entities.Flush();
            c.Equal(Find(game, "source").Id, Read(game).Target, "same query replans to the remaining real facility");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "removed old endpoint/ended origin checkpoint honestly");
            Enable(c, game, true); Enable(c, twin, true); long second = SourceView(game).Schedule.LastPlan;
            c.That(second > first, "future eligible source posts ordinary new intent after cancellation/removal");
            c.Equal(Find(game, "source").Id, Plan(game, second).Target, "remaining facility becomes new actual order destination");
            Reach(game, () => Plan(game, second).State == PlanState.Done, "replanned physical facility service completes", twin);
            c.Equal(Fix.FromInt(10), Condition(game, "machine.a"), "new actual work alone completes cancelled repair");
            c.Equal(Fix.FromInt(31), Stock(game, "source"), "new facility supplies actual spares after receiving the returned tools");
            c.Equal(PlanState.Cancelled, Plan(game, first).State, "original plan retains cancellation and old target history");
            c.Equal(game.Hash(), twin.Hash(), "cancel/remove/replan continuation is deterministic");
        }
    }
}
