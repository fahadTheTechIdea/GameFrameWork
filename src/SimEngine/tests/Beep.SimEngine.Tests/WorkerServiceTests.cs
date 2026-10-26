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
    internal static class WorkerServiceTests
    {
        private static readonly Code Integrity = new Code("integrity");
        internal static void Register()
        {
            TestRunner.Add("Worker service: authored subjects, candidate requirements and completion ownership", Contracts);
            TestRunner.Add("Worker service: physical bays, materials, repair and saved continuation", Physical);
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        internal static GameFactory Factory(bool extension = false, Action<JsonValue> edit = null, int fault = 0, string activityDocument = null, string alertProfiles = null, string completionDocument = null, string activityDefinitions = null,
            IReadOnlyList<GameModule> additionalModules = null, JsonValue additionalDeclarations = null, IReadOnlyDictionary<string,string> additionalDocuments = null)
        {
            var documents = new Dictionary<string, string> { ["worker-service.json"] = FrameworkProofContent.Read("worker-service.json") };
            if (activityDocument != null) documents.Add(activityDocument, activityDefinitions ?? FrameworkProofContent.Read(activityDocument));
            var modules = new List<GameModule> { new EquipmentConditionModule(), new WorkerServiceProofModule() };
            if (completionDocument != null)
            {
                modules.Add(new CompletionEffectModule());
                documents.Add(completionDocument, FrameworkProofContent.Read(completionDocument));
            }
            if(additionalModules!=null)modules.AddRange(additionalModules);
            if(additionalDocuments!=null)foreach(var entry in additionalDocuments)documents.Add(entry.Key,entry.Value);
            var declarations=Json.Parse("[{\"id\":\"sim.equipment-condition\",\"version\":\"1.0.0\",\"documents\":[\"worker-service.json\"" + (activityDocument == null ? "" : ",\"" + activityDocument + "\"") + "]},{\"id\":\"proof.worker-service\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.equipment-condition\",\"version\":\"1.0.0\"}],\"documents\":[]}" +
                    (completionDocument == null ? "" : ",{\"id\":\"proof.completion\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"" + completionDocument + "\"]}") + "]");
            if(additionalDeclarations!=null)for(int i=0;i<additionalDeclarations.Count;i++)declarations.Add(additionalDeclarations[i]);
            return ServiceDockTests.Factory(root =>
            {
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(EquipmentConditionModule.ModuleId));
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.worker-service"));
                if (completionDocument != null) root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.completion"));
                root.Get("activities")[0].Set("nodes", Json.Parse("[{\"id\":\"service\",\"executor\":\"work.staffed\",\"roles\":[\"inspect\"],\"duration\":{\"numerator\":1,\"denominator\":5,\"unit\":\"hour\"},\"requirement\":\"service.ready\",\"effect\":\"service.repair\"}]"));
                for (int i = 0; i < root.Get("entities").Count; i++)
                {
                    bool injured = i == 0 || i == (extension ? 3 : 2);
                    var parts = root.Get("entities")[i].Get("components");
                    if (extension && i == 3) parts.Set("proof_service_condition", Json.Parse("{\"id\":\"integrity\",\"unit\":\"unit.condition\",\"maximum\":10,\"initial\":4,\"fault\":" + fault + "}"));
                    else parts.Set("equipment_condition", Json.Parse("{\"id\":\"integrity\",\"unit\":\"unit.condition\",\"maximum\":10,\"initial\":" + (injured ? "4" : "10") + ",\"wear\":null,\"states\":{\"damaged\":\"blocked\",\"failed\":\"blocked\"}}"));
                }
                edit?.Invoke(root);
            }, extraModules: modules,
                extraDeclarations: declarations.ToString(),
                extraDocuments: documents, alertProfiles: alertProfiles);
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 965 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id)
        { foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException("missing worker service " + id); }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static Fix Value(SimGame game, string id) => ConditionContracts.Read(game.World, Find(game, id).Get<IConditionProvider>(Integrity)).Value;
        private static Fix Stock(SimGame game) => Find(game, "source").Get<IInventory>().Amount(game.World.Content.ResourceId("supply"));
        private static Order Post(SimGame game)
        {
            var result = TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = Find(game, "source").Id });
            if (!result.Accepted || game.Fault != null) throw new InvalidOperationException("worker service refused: " + result + "/" + game.Fault);
            return Board(game).Order(checked((int)result.Arg0));
        }
        private static void Step(SimGame game, SimGame twin = null)
        { game.Update(); if (twin != null) twin.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("service update failed at " + game.Clock.StepIndex + ": " + game.Fault + "/" + twin?.Fault); }
        private static void Reach(SimGame game, Order order, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 200 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " timeout at " + game.Clock.StepIndex + ": " + order.State + "/" + order.Node("service").State + "/" + order.Node("service").Blocker);
        }
        private static ActivityNodeView Read(SimGame game, int order, int seat = 0)
        {
            var nodes = new List<ActivityNodeView>(); bool result = game.Seat(seat).View.Feature<WorkView>().TryCopyNodes(order, nodes);
            if (!result) throw new InvalidOperationException("node view refused");
            foreach (var node in nodes) if (node.Id == "service") return node;
            throw new InvalidOperationException("missing service node view");
        }
        private static void Pause(Check c, SimGame game, EntityId actor, bool enabled, SimGame twin = null)
        {
            foreach (var current in twin == null ? new[] { game } : new[] { game, twin })
            {
                var policy = WorkPolicyRules.Read(current.World.Entities.Get(actor));
                c.That(TestKit.Order(current, 0, new ConfigureWorkPolicyCommand { Worker = actor,
                    Settings = new WorkPolicySettings(enabled, policy.Priority, policy.AllowedRoles, policy.Area, policy.Interruptible, policy.EmergencyEligible, WorkAssignmentMode.All) }).Accepted, "ordinary policy controls actual servicing actor");
            }
        }
        private static void Contracts(Check c)
        {
            foreach (bool extension in new[] { false, true })
            {
                var factory = Factory(extension); var game = New(factory);
                c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current worker completion identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "sole node codec changes directly");
                c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "one existing board owns completion actor");
                var candidates = new List<WorkWorkerCandidate>(); ulong hash = game.Hash();
                c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("inspect", "service", Find(game, "source").Id, candidates), "worker scoped requirement participates in ordinary candidates");
                int accepted = 0; foreach (var candidate in candidates) if (candidate.Result.Blocker.IsNone) accepted++;
                c.Equal(1, accepted, "only actual damaged worker may spend facility spares");
                c.Equal(hash, game.Hash(), "worker requirements and candidate previews remain pure");
                var order = Post(game); var node = order.Node("service");
                c.Equal(EntityId.None, node.CompletedBy, "admission/travel cannot fabricate completed actor");
                var view = Read(game, order.Id); c.Equal(EntityId.None, view.CompletedBy, "private view reports no early completion identity");
                var hidden = new List<ActivityNodeView>(); c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyNodes(order.Id, hidden) && hidden.Count == 0, "foreign node receipt is private");
                node.CompletedBy = Find(game, extension ? "machine.b" : "machine.a").Id;
                c.Throws<CorruptStateException>(() => game.Hash(), "unearned working-node actor cannot checkpoint"); node.CompletedBy = EntityId.None;
                c.That(game.Save().Length > 0, "restored fault instrumentation retains actual job state");
                c.Throws<ContentException>(() => New(Factory(extension, root => root.Get("activities")[0].Get("nodes")[0].Set("roles", JsonValue.NewArray()))), "worker repair cannot be a timer with no actor");
                c.Throws<ContentException>(() => New(Factory(extension, root => root.Get("activities")[0].Get("nodes")[0].Set("duration", Content.Json.Parse("{\"numerator\":0,\"denominator\":1,\"unit\":\"hour\"}")))), "zero-time worker repair cannot earn receipt");
                c.Throws<ContentException>(() => New(Factory(extension, root => { root.Get("activities")[0].Get("nodes")[0].Remove("effect"); root.Get("activities")[0].Set("on_complete", JsonValue.Of("service.repair")); })), "aggregate completion cannot guess one servicing actor");
            }
        }
        private static void Physical(Check c)
        { Service(c, false); Service(c, true); Cancel(c); Dishonest(c); }
        private static void Service(Check c, bool extension)
        {
            var factory = Factory(extension, root => root.Get("activities")[0].Get("nodes").Add(Json.Parse("{\"id\":\"settle\",\"executor\":\"work.timed\",\"roles\":[],\"duration\":{\"numerator\":1,\"denominator\":10,\"unit\":\"hour\"},\"after\":[\"service\"]}")));
            var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            string prototype = extension ? "machine.b" : "machine.a"; EntityId actor = Find(game, prototype).Id;
            var order = Post(game); var node = order.Node("service");
            Reach(game, order, () => node.Progress.WholeTicks >= 3, "real service work starts");
            c.Equal(actor, Board(game).Job(node.JobId).Worker, "actual injured actor selected through generic worker requirement");
            c.Equal(Fix.One, Find(game, prototype).Get<PointPresence>().Position, "worker earns service time only at actual bay");
            c.Equal(Fix.FromInt(4), Value(game, prototype), "partial work is not instant repair");
            c.Equal(Fix.FromInt(29), Stock(game), "actual startup consumes three native spares exactly once");
            c.Equal(EntityId.None, node.CompletedBy, "partial service has no completion receipt");
            var retained = Read(game, order.Id); var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial service saves real jobs/resources/condition together");
            long coordinate = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); ulong pausedHash = game.Hash(); game.Update(0); twin.Update(0);
            c.Equal(coordinate, game.Clock.StepIndex, "sole game clock pause stops physical repair time"); c.Equal(pausedHash, game.Hash(), "paused zero-elapsed update changes no service state");
            game.Clock.Resume(); twin.Clock.Resume(); Pause(c, game, actor, false, twin); WorkProgress paused = node.Progress;
            Step(game, twin); Step(game, twin); c.Equal(paused, node.Progress, "normal policy pause earns no repair work");
            c.Equal(Fix.FromInt(4), Value(game, prototype), "paused service cannot apply worker effect");
            Pause(c, game, actor, true, twin);
            Reach(game, order, () => Board(game).Job(node.JobId)?.ToilIndex == 2, "earned service awaits real bay departure", twin);
            c.Equal(Fix.FromInt(4), Value(game, prototype), "fully earned work still cannot repair before physical departure");
            c.Equal(EntityId.None, node.CompletedBy, "departure has not yet earned completion actor");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "completed work/departure stage restores without early repair");
            Reach(game, order, () => node.State == NodeState.Done, "actual exit then worker repair", twin);
            c.That(order.IsActive, "authored settling stage retains completed physical node, not a new receipt ledger");
            c.Equal(actor, node.CompletedBy, "actual final actor retained after original job/machine release");
            c.Equal(Fix.FromInt(10), Value(game, prototype), "one verified worker condition change repairs correct machine");
            c.Equal(Fix.FromInt(4), Value(game, "source"), "facility condition unchanged by worker service");
            c.That(Find(game, prototype).Get<PointPresence>().Position != Fix.One, "repair receipt follows physical bay clearance");
            c.Equal(EntityId.None, retained.CompletedBy, "retained private view remains detached");
            c.Equal(actor, Read(game, order.Id).CompletedBy, "current immutable view exposes real completed identity");
            c.Equal(Fix.FromInt(29), Stock(game), "holding tools release without extra consumption");
            Step(game, twin); Step(game, twin); c.Equal(Fix.FromInt(10), Value(game, prototype), "completed worker effect never repeats");
            c.Equal(game.Hash(), twin.Hash(), "native/public saved continuation is deterministic");
            c.Equal(isolatedHash, isolated.Hash(), "second session has no shared service progress");
            EntityId receipt = node.CompletedBy; node.CompletedBy = EntityId.None;
            c.Throws<CorruptStateException>(() => game.Hash(), "settled positive-time machine node requires receipt");
            node.CompletedBy = game.World.Entities.NextIdentity; c.Throws<CorruptStateException>(() => game.Hash(), "future completion actor rejects");
            node.CompletedBy = Find(game, "source").Id; c.Throws<CorruptStateException>(() => game.Hash(), "live nonworker cannot impersonate actor");
            node.CompletedBy = receipt; c.That(factory.Load(game.Save()).Hash() == game.Hash(), "restored real receipt saves and restores exactly");
            Reach(game, order, () => !order.IsActive, "authored settling stage finishes", twin);
            c.Equal(OrderState.Done, order.State, "physical service and authored settling complete");
            var ended = new List<ActivityNodeView>();
            c.That(!game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, ended), "ended native orders prune through existing owner, no shadow receipt history");
        }
        private static void Cancel(Check c)
        {
            var factory = Factory(); var game = New(factory); var order = Post(game); var node = order.Node("service");
            Reach(game, order, () => node.Progress.WholeTicks > 0, "cancelled service startup");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "ordinary cancellation ends service");
            c.Equal(EntityId.None, node.CompletedBy, "cancelled unfinished service has no actor receipt");
            c.Equal(Fix.FromInt(4), Value(game, "machine.a"), "cancellation cannot repair damaged worker");
            c.Equal(Fix.FromInt(29), Stock(game), "spent spares are not refunded by cancellation");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled progress remains durable");
        }
        private static void Dishonest(Check c)
        {
            foreach (int fault in new[] { 3, 4 })
            {
                var factory = Factory(true, fault: fault); var game = New(factory); var order = Post(game);
                for (int i = 0; i < 200 && game.Fault == null && order.IsActive; i++) game.Update();
                c.That(game.Fault != null, "ignored/future worker condition commit faults ordinary engine update " + fault);
                c.Throws<InvalidOperationException>(() => game.Save(), "dishonest worker completion cannot checkpoint partial state " + fault);
            }
        }
    }
}
