using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Fixture authoring, player commands and bounded observations only; engine owns all service decisions.
    internal static class HomeServiceTests
    {
        private static readonly Code Service = new Code("home.service"), Integrity = new Code("integrity");
        internal static void Register()
        {
            TestRunner.Add("Home service: authored requests, source origins and admission", Contracts);
            TestRunner.Add("Home service: recurring physical repair, home edits and saved continuation", Physical);
        }
        internal static GameFactory Factory(bool extension = false, Action<JsonValue> edit = null)
            => WorkerServiceTests.Factory(extension, root =>
            {
                root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number("32"));
                Parts(root, extension).Set("recurring_activity", Json.Parse("{\"id\":\"home.service\",\"plan\":\"activity:home.service\",\"template\":null,\"target\":\"worker_home\",\"arguments\":null,\"cadence\":\"submission\",\"trigger\":\"schedule\",\"dispatch_mode\":\"compete\",\"facility_policy\":null,\"reserve_use\":\"none\",\"worker_nodes\":[\"service\"],\"enabled\":false,\"priority\":60,\"group\":\"fleet.service\",\"operation_policy\":null,\"interval\":{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"},\"conditions\":[{\"id\":\"damaged\",\"source\":\"integrity\",\"signal\":\"home.integrity\",\"unit\":\"unit.condition\",\"minimum\":0,\"maximum\":4,\"blocker\":\"home.not-due\"}]}"));
                edit?.Invoke(root);
            }, activityDocument: "home-service.json");
        private static JsonValue Parts(JsonValue root, bool extension = false) => root.Get("entities")[extension ? 3 : 2].Get("components");
        private static JsonValue Source(JsonValue root) => Parts(root).Get("recurring_activity");
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 1006 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id)
        {
            foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity;
            throw new InvalidOperationException("home service entity missing: " + id);
        }
        private static string Actor(bool extension) => extension ? "machine.b" : "machine.a";
        private static WorkView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static RecurringPlanView Read(SimGame game, bool extension = false)
        {
            Code refusal = View(game).ReadRecurringPlan(Find(game, Actor(extension)).Id, Service, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("home service private view refused: " + refusal); return value;
        }
        private static PlanView Plan(SimGame game, long id)
        {
            var plans = new List<PlanView>(); View(game).CopyPlans(plans);
            foreach (var plan in plans) if (plan.Id == id) return plan;
            throw new InvalidOperationException("home service plan missing: " + id);
        }
        private static Fix Condition(SimGame game, string id) => ConditionContracts.Read(game.World, Find(game, id).Get<IConditionProvider>(Integrity)).Value;
        private static Fix Stock(SimGame game, string id) => Find(game, id).Get<IInletPort>().Amount(game.World.Content.ResourceId("supply"));
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("home service game update failed: " + game.Fault + "/" + twin?.Fault);
        }
        private static CommandResult Command(SimGame game, Command command)
        {
            int sequence = game.Seat(0).Submit(command); Step(game);
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            foreach (var result in results) if (result.Sequence == sequence) return result;
            throw new InvalidOperationException("home service command lost its answer");
        }
        private static void Home(Check c, SimGame game, string home, bool extension = false)
            => c.That(Command(game, new SetHomeCommand { Worker = Find(game, Actor(extension)).Id,
                Home = home == null ? EntityId.None : Find(game, home).Id }).Accepted, "ordinary home command controls future destination");
        private static void Configure(Check c, SimGame game, bool enabled, bool extension = false, int minutes = 1)
            => c.That(Command(game, new ConfigureRecurringPlanCommand { Entity = Find(game, Actor(extension)).Id, Source = Service,
                Settings = new RecurringPlanSettings(enabled, 60, GameDuration.Minutes(minutes), Code.None) }).Accepted, "ordinary recurring control owns enabled/cadence");
        private static void Reach(SimGame game, Func<bool> complete, string stage, SimGame twin = null)
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
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "clean request/provenance protocol identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "one new plan payload layout");
            c.That(!default(RecurringPlanRequest).IsWellFormed, "default request cannot invent intent");
            c.Throws<ArgumentNullException>(() => RecurringPlanRequest.Ready((PlanIntent)null), "ready request requires intent");
            c.Throws<ArgumentException>(() => RecurringPlanRequest.Refused(Code.None), "refusal needs a reason");
            c.Throws<ArgumentException>(() => new PlanOrigin(EntityId.None, Service, 1, false), "origin must have an allocated positive source identity");
            c.Throws<ArgumentException>(() => new PlanOrigin(new EntityId(1), Code.None, 1, false), "origin cannot lose named source");
            var nodes = new List<string> { "z", "a" }; var binding = new RecurringPlanBinding(RecurringPlanTarget.WorkerHome, nodes, RecurringReserveUse.None, null); nodes.Clear();
            c.Equal("a", binding.WorkerNodes[0], "authored bindings are ordinal and detached");
            c.Throws<NotSupportedException>(() => ((IList<string>)binding.WorkerNodes).Clear(), "binding is immutable");
            c.Throws<ArgumentException>(() => new RecurringPlanBinding(RecurringPlanTarget.WorkerHome, Array.Empty<string>(), RecurringReserveUse.None, null), "home needs explicit machine nodes");
            c.Throws<ArgumentException>(() => new RecurringPlanBinding(RecurringPlanTarget.Self, new[] { "a", "a" }, RecurringReserveUse.None, null), "duplicate authored nodes reject");
            c.Throws<ArgumentException>(() => new RecurringPlanBinding((RecurringPlanTarget)8, Array.Empty<string>(), RecurringReserveUse.None, null), "unknown target policy rejects");
            foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                root => Source(root).Remove("target"), root => Source(root).Remove("worker_nodes"),
                root => Source(root).Set("target", JsonValue.Of("nearest")), root => Source(root).Set("worker_nodes", JsonValue.NewArray()),
                root => Source(root).Set("target", JsonValue.Null()), root => Source(root).Set("worker_nodes", JsonValue.Null()),
                root => Source(root).Set("unowned_setting", JsonValue.Of(true)),
                root => Source(root).Set("worker_nodes", Json.Parse("[\"service\",\"service\"]")),
                root => Source(root).Set("worker_nodes", Json.Parse("[\"missing\"]")),
                root => Source(root).Set("worker_nodes", Json.Parse("[\"settle\"]")),
                root => Source(root).Set("worker_nodes", Json.Parse("[7]")),
                root => { Parts(root).Remove("worker"); Parts(root).Remove("transporter"); },
                root => { var row = Source(root).Clone(); Parts(root).Remove("recurring_activity"); root.Get("entities")[0].Get("components").Set("recurring_activity", row); }
            }) c.Throws<ContentException>(() => New(Factory(edit: edit)), "invalid authored home binding fails before play");
            foreach (bool extension in new[] { false, true })
            {
                var factory = Factory(extension); var game = New(factory); var initial = Read(game, extension); ulong hash = game.Hash();
                c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "actual board owns origin payload");
                c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "existing work system owns new request semantics");
                c.Equal(RecurringPlanCodes.Disabled, initial.Blocker, "disabled cadence remains explicit");
                c.Equal(Find(game, Actor(extension)).Id, initial.Entity, "detached request belongs to one actual source entity");
                c.Equal(RecurringPlanCodes.NoTarget, initial.RequestBlocker, "missing home is still separately visible");
                c.That(initial.Intent == null, "missing home never fabricates a target");
                c.Equal(hash, game.Hash(), "pure home/request preview changes no authoritative state");
                c.Equal(Codes.NotFound, View(game, 1).ReadRecurringPlan(Find(game, Actor(extension)).Id, Service, out _), "source facts remain company private");
                Configure(c, game, true, extension); Step(game); Step(game);
                c.Equal(RecurringPlanCodes.NoTarget, Read(game, extension).Blocker, "enabled missing-home source waits normally");
                c.Equal(0L, Read(game, extension).Schedule.LastPlan, "refusal earns no posting history");
                c.Equal(0, Board(game).Orders.Count, "refusal creates no job/order");
                c.Equal(Fix.FromInt(32), Stock(game, "source"), "refusal reserves/consumes no resources");
                var missing = factory.Load(game.Save()); c.Equal(game.Hash(), missing.Hash(), "typed unavailable request checkpoints exactly");
                Home(c, game, "source", extension); long id = Read(game, extension).Schedule.LastPlan;
                c.That(id > 0, "actual home enables engine-owned posting"); var plan = Plan(game, id);
                c.Equal(Find(game, Actor(extension)).Id, plan.Origin.Entity, "plan origin retains actual source worker");
                c.Equal(Service, plan.Origin.Source, "plan origin retains actual named capability");
                c.That(!plan.Origin.IsManual, "automatic admission is not manual intent");
                c.Equal(Find(game, "source").Id.Value, new JsonReader(plan.Arguments, "request").Int("target"), "request targets facility, not source worker");
                c.Equal(Find(game, Actor(extension)).Id.Value, new JsonReader(plan.Arguments.Get("workers")[0], "selection").Int("worker"), "request selects source worker explicitly");
                long posted = Read(game, extension).Schedule.LastPostedTick;
                Home(c, game, "destination", extension);
                c.Equal(id, Read(game, extension).Schedule.LastPlan, "changed home cannot create another live service");
                c.Equal(RecurringPlanCodes.Outstanding, Read(game, extension).Blocker, "one actual source posting owns outstanding work");
                c.Equal(posted, Read(game, extension).Schedule.LastPostedTick, "home edit cannot rebase the single-clock cadence");
                c.Equal(Find(game, "source").Id.Value, new JsonReader(Plan(game, id).Arguments, "plan").Int("target"), "active intent does not retarget");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "dynamic home preserves original history/checkpoint");
                c.Equal(RecurringPlanCodes.RequestChanged, RecurringPlanContracts.PreviewAdmission(game.World,
                    Find(game, Actor(extension)).Get<IRecurringPlanSource>(Service), RecurringPlanRequest.Ready(plan.Intent)), "stale captured target cannot be submitted");
                Home(c, game, null, extension); c.Equal(RecurringPlanCodes.NoTarget, Read(game, extension).RequestBlocker, "clearing home yields typed refusal with retained history");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "history is not validated against absent current intent");
                // Explicit ownership-loss diagnostic instrumentation, restored without creating resources.
                Home(c, game, "destination", extension); Entity destination = Find(game, "destination"); destination.Owner = game.Seat(1).Id;
                c.Equal(Codes.NotOwned, Read(game, extension).RequestBlocker, "foreign home does not leak/search another facility");
                destination.Owner = game.Seat(0).Id;
            }
            Origins(c); Arbitration(c); PublicSource(c);
        }
        private static void Origins(Check c)
        {
            var factory = Factory(); var game = New(factory); Home(c, game, "source"); Configure(c, game, true);
            long id = Read(game).Schedule.LastPlan;
            // Explicit codec/acknowledgement fault instrumentation: no substitute execution or resources.
            object entries = typeof(WorkBoard).GetField("_plans", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Board(game));
            object entry = ((System.Collections.IList)entries)[0]; var origin = entry.GetType().GetField("Origin", BindingFlags.NonPublic | BindingFlags.Instance);
            var retained = (PlanOrigin)origin.GetValue(entry); origin.SetValue(entry, default(PlanOrigin));
            c.Throws<CorruptStateException>(() => game.Save(), "a source cannot acknowledge a manual plan with identical intent/time");
            origin.SetValue(entry, new PlanOrigin(new EntityId(int.MaxValue), Service, retained.CycleHead, retained.TemplateCycle));
            c.Throws<CorruptStateException>(() => game.Hash(), "unallocated provenance cannot checkpoint");
            origin.SetValue(entry, new PlanOrigin(retained.Entity, new Code("wrong.source"), retained.CycleHead, retained.TemplateCycle));
            c.Throws<CorruptStateException>(() => game.Hash(), "wrong named source cannot acknowledge actual posting");
            origin.SetValue(entry, retained); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "actual origin restores after explicit fault instrumentation");
            Configure(c, game, false); c.That(Command(game, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { id }) }).Accepted, "normal cancellation ends original service intent");
            var result = Command(game, new RepeatPlanCommand { Plan = id }); c.That(result.Accepted, "normal player repeat uses existing plan board");
            c.That(Plan(game, result.Arg0).Origin.IsManual, "player repeat cannot inherit automatic origin");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "ended source history and manual repeat checkpoint independently");
            byte[] bytes;
            using (var stream = new MemoryStream()) { PlanOrigin.Sync(new StateWriter(stream), retained); bytes = stream.ToArray(); }
            var decoded = PlanOrigin.Sync(new StateReader(bytes, 0, bytes.Length), default);
            c.Equal(retained.Entity, decoded.Entity, "same embedded codec restores source identity");
            c.Equal(retained.Source, decoded.Source, "same embedded codec restores named source");
            c.Throws<CorruptStateException>(() => PlanOrigin.Sync(new StateReader(bytes, 0, bytes.Length - 1), retained), "truncated origin cannot be accepted");
            foreach (var pair in new[] { (EntityId.None, Service.Id), (retained.Entity, (string)null), (new EntityId(-1), Service.Id), (retained.Entity, "") })
            {
                EntityId entity = pair.Item1; string source = pair.Item2;
                using (var stream = new MemoryStream()) { var writer = new StateWriter(stream); writer.Entity(ref entity); writer.String(ref source); bytes = stream.ToArray(); }
                c.Throws<CorruptStateException>(() => PlanOrigin.Sync(new StateReader(bytes, 0, bytes.Length), retained), "malformed embedded origin pair rejects");
            }
            var detached = Plan(game, id); Configure(c, game, false);
            c.Equal(retained.Entity, detached.Origin.Entity, "captured source origin does not mutate with live policy");
        }
        private static void Arbitration(Check c)
        {
            foreach (bool reversed in new[] { false, true })
            {
                var factory = Factory(edit: root =>
                {
                    var first = Source(root); var second = first.Clone(); second.Set("id", JsonValue.Of("alpha"));
                    first.Set("enabled", JsonValue.Of(true)); second.Set("enabled", JsonValue.Of(true));
                    var rows = JsonValue.NewArray(); rows.Add(reversed ? second : first); rows.Add(reversed ? first : second); Parts(root).Set("recurring_activity", rows);
                });
                var game = New(factory); Home(c, game, "source"); var entity = Find(game, "machine.a");
                var winner = RecurringPlanContracts.Read(game.World, entity.Get<IRecurringPlanSource>(new Code("alpha")));
                var loser = Read(game); var plans = new List<PlanView>(); View(game).CopyPlans(plans);
                c.Equal(1, plans.Count, "equal requests share ordinary duplicate admission, not duplicate repair systems");
                c.That(winner.Schedule.LastPlan > 0 && loser.Schedule.LastPlan == 0, "stable source identity wins independently of JSON order");
                c.Equal(new Code("alpha"), plans[0].Origin.Source, "winner alone owns acknowledged origin");
                c.Equal(-1L, loser.Schedule.LastPostedTick, "refused competitor earns no false posting time");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "competing sources restore actual winner and refusal");
            }
            var unsupported = New(Factory(edit: root => { Parts(root).Remove("point_motion"); Parts(root).Remove("transporter"); }));
            // Explicit malformed-binding diagnostic instrumentation; no motion or gameplay fallback.
            Find(unsupported, "machine.a").Get<IWorker>().Home = Find(unsupported, "source").Id;
            c.Equal(WorkerHomeContracts.Unsupported, Read(unsupported).RequestBlocker, "unreachable interaction contract has typed unsupported-home request");
        }
        private static void PublicSource(Check c)
        {
            var factory = RecurringPlanTests.Factory(root => root.Get("entities")[0].Get("components").Set("proof_recurring_activity",
                Json.Parse("{\"id\":\"custom\",\"plan\":\"activity:equipment.inspect\",\"template\":null,\"target\":\"self\",\"arguments\":null,\"reserve_use\":\"none\",\"worker_nodes\":[],\"cadence\":\"submission\",\"trigger\":\"schedule\",\"dispatch_mode\":\"compete\",\"facility_policy\":null,\"group\":null,\"enabled\":true,\"priority\":80,\"interval\":{\"numerator\":30,\"denominator\":1,\"unit\":\"minute\"},\"operation_policy\":null,\"fault\":0,\"idle_only\":false}")));
            var game = RecurringPlanTests.New(factory); Entity entity = RecurringPlanTests.Spawn(game); Step(game);
            var source = entity.Get<IRecurringPlanSource>(new Code("custom")); var value = RecurringPlanContracts.Read(game.World, source);
            c.That(value.Schedule.LastPlan > 0, "unrelated public-only provider uses new request protocol");
            var plan = Plan(game, value.Schedule.LastPlan); c.Equal(entity.Id, plan.Origin.Entity, "public source has the same actual origin");
            c.Equal(new Code("custom"), plan.Origin.Source, "public source uses no native special-case");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "public recurring provider restores through same origin validation");
            for (int fault = 1; fault <= 8; fault++)
            {
                int selected = fault;
                var badFactory = RecurringPlanTests.Factory(root => root.Get("entities")[0].Get("components").Set("proof_recurring_activity",
                    Json.Parse("{\"id\":\"custom\",\"plan\":\"activity:equipment.inspect\",\"template\":null,\"target\":\"self\",\"arguments\":null,\"reserve_use\":\"none\",\"worker_nodes\":[],\"cadence\":\"submission\",\"trigger\":\"schedule\",\"dispatch_mode\":\"compete\",\"facility_policy\":null,\"group\":null,\"enabled\":true,\"priority\":80,\"interval\":{\"numerator\":30,\"denominator\":1,\"unit\":\"minute\"},\"operation_policy\":null,\"fault\":" + selected + ",\"idle_only\":false}")));
                var bad = RecurringPlanTests.New(badFactory);
                if (fault == 7) bad.Update(); // Ensure the actual successful posting is not already at coordinate zero.
                Entity badEntity = RecurringPlanTests.Spawn(bad);
                if (fault < 6 || fault == 8)
                { bad.Update(); c.That(bad.Fault != null, "impure/invalid request or dishonest origin acknowledgement faults update mode=" + fault); }
                else
                {
                    if (fault == 7) { bad.Update(); bad.Update(); }
                    bad.Seat(0).Submit(new ConfigureRecurringPlanCommand { Entity = badEntity.Id, Source = new Code("custom"),
                        Settings = new RecurringPlanSettings(false, 80, GameDuration.Minutes(60), Code.None) });
                    bad.Update(); c.That(bad.Fault != null, "dishonest settings cannot rewrite interval/history mode=" + fault);
                }
            }
        }
        private static void Physical(Check c)
        {
            ServiceRun(c, false); ServiceRun(c, true); CancelAndRepost(c);
        }
        private static void ServiceRun(Check c, bool extension)
        {
            var factory = Factory(extension); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash();
            string actor = Actor(extension); Home(c, game, "source", extension); Configure(c, game, true, extension);
            long id = Read(game, extension).Schedule.LastPlan; Order order = Board(game).Order(Plan(game, id).Order);
            Reach(game, () => order.Node("service").Progress.WholeTicks >= 3, "physical home service starts");
            c.Equal(Find(game, actor).Id, Board(game).Job(order.Node("service").JobId).Worker, "actual source worker is the only selected actor");
            c.Equal(Fix.One, Find(game, actor).Get<PointPresence>().Position, "physical bay arrival earns work");
            c.Equal(Fix.FromInt(29), Stock(game, "source"), "three actual facility spares consumed once");
            c.Equal(Fix.FromInt(4), Condition(game, actor), "partial work is not instant repair");
            c.Equal(EntityId.None, order.Node("service").CompletedBy, "arrival is not completed service");
            Home(c, game, "destination", extension); var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "partial service plus changed home saves exactly");
            var originalIntent = Plan(game, id).Intent; long coordinate = game.Clock.StepIndex; ulong paused = game.Hash();
            game.Clock.Pause(); twin.Clock.Pause(); game.Update(1000000); twin.Update(1000000);
            c.Equal(coordinate, game.Clock.StepIndex, "one game clock pause stops service time"); c.Equal(paused, game.Hash(), "paused host frame earns no service or cadence");
            game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, () => Board(game).Job(order.Node("service").JobId)?.ToilIndex == 2, "actual work awaits bay departure", twin);
            c.Equal(Fix.FromInt(4), Condition(game, actor), "full work cannot repair before departure");
            c.Equal(id, Read(game, extension).Schedule.LastPlan, "new home cannot produce overlapping automatic plan");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "saved departure has no early effect");
            Reach(game, () => order.Node("service").State == NodeState.Done, "physical departure earns repair", twin);
            c.Equal(Fix.FromInt(10), Condition(game, actor), "actual native/public worker repaired through existing effect");
            c.Equal(Find(game, actor).Id, order.Node("service").CompletedBy, "completion identifies selected subject");
            c.Equal(Fix.FromInt(4), Condition(game, "source"), "facility is not repaired as a substitute subject");
            c.Equal(Fix.FromInt(32), Stock(game, "destination"), "edited home is not silently charged for original work");
            c.Equal(Fix.FromInt(29), Stock(game, "source"), "tools released; consumed spares are not refunded");
            c.Equal(Find(game, "source").Id.Value, new JsonReader(originalIntent.Arguments, "plan").Int("target"), "detached original intent stays at original facility");
            Reach(game, () => Plan(game, id).State == PlanState.Done, "ordinary graph settles", twin);
            Reach(game, () => Find(game, actor).Get<PointPresence>().Position == Fix.FromInt(10), "idle worker returns to current home", twin);
            for (int i = 0; i < 8; i++) Step(game, twin);
            c.Equal(new Code("home.not-due"), Read(game, extension).Blocker, "healthy worker does not spend recurring spares");
            c.Equal(id, Read(game, extension).Schedule.LastPlan, "healthy cadence does not fabricate inspections");
            c.Equal(game.Hash(), twin.Hash(), "saved/interleaved physical continuation is deterministic");
            c.Equal(untouched, isolated.Hash(), "session condition/home/cadence/stock are isolated");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "ended repair at old home checkpoints with new home");
        }
        private static void CancelAndRepost(Check c)
        {
            var factory = Factory(); var game = New(factory); Home(c, game, "source"); Configure(c, game, true);
            long first = Read(game).Schedule.LastPlan; Order order = Board(game).Order(Plan(game, first).Order);
            Reach(game, () => order.Node("service").Progress.WholeTicks >= 3, "cancel scenario reaches physical service");
            long posted = Read(game).Schedule.LastPostedTick; Configure(c, game, false, minutes: 60);
            c.Equal(posted, Read(game).Schedule.LastPostedTick, "disabled interval edit retains original earned posting time");
            Home(c, game, "destination"); c.That(Command(game, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { first }) }).Accepted, "normal cancel stops partial repair");
            c.Equal(Fix.FromInt(4), Condition(game, "machine.a"), "cancel cannot apply repair effect");
            c.Equal(Fix.FromInt(29), Stock(game, "source"), "cancel releases tools without refunding consumed spares");
            Configure(c, game, true, minutes: 60); c.Equal(RecurringPlanCodes.Cadence, Read(game).Blocker, "re-enable waits against original single-clock posting");
            c.Equal(first, Read(game).Schedule.LastPlan, "waiting does not fabricate a new admission");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "disabled/cancelled history and edited interval restore");
            Configure(c, game, true); Configure(c, twin, true);
            long second = Read(game).Schedule.LastPlan; c.That(second > first, "shorter eligible cadence posts a new ordinary plan");
            c.Equal(Find(game, "destination").Id.Value, new JsonReader(Plan(game, second).Arguments, "plan").Int("target"), "future admission uses current home");
            c.Equal(Find(game, "machine.a").Id, Plan(game, second).Origin.Entity, "new admission retains same source provenance");
            Reach(game, () => Plan(game, second).State == PlanState.Done, "real second-facility service completes", twin);
            c.Equal(Fix.FromInt(10), Condition(game, "machine.a"), "cancelled worker repaired only through new physical service");
            c.Equal(Fix.FromInt(29), Stock(game, "destination"), "new home supplies its own actual spares");
            c.Equal(Fix.FromInt(29), Stock(game, "source"), "old home has no duplicate consumption/refund");
            c.Equal(PlanState.Cancelled, Plan(game, first).State, "original plan retains actual cancellation disposition");
            c.Equal(game.Hash(), twin.Hash(), "cancel/edit/repost saved continuation is exact");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "both actual origins and dispositions checkpoint");
        }
    }
}
