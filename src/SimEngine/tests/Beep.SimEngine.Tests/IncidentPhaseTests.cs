using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class IncidentPhaseTests
    {
        private static readonly Code Source = new Code("leak"), Kind = new Code("machine.leak");
        public static void Register()
        {
            TestRunner.Add("Incidents: authored workflow contracts and strict phase admission", Contracts);
            TestRunner.Add("Incidents: phased physical response, cancellation and saved continuation", Physical);
        }
        private static string Document => FrameworkProofContent.Read("incident-phases.json");
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static GameFactory Factory(string document = null) => Colony.Factory(additionalDocuments:
            new Dictionary<string, string> { { "incident-phases.json", document ?? Document } });
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 811 });
        // Explicit scenario fixture installation, not a client-side repair/spawn fallback.
        private static Entity Target(SimGame game, int seat = 0)
        {
            Entity target = ((global::Beep.SimEngine.Maps.TileMap)game.World.Space).SpawnAt(game.World,
                game.World.Content.Get<EntityDef>("leaking-machine"), new PlayerId(seat), new TileCoord(16 + seat * 6, 18));
            game.World.Entities.Flush(); return target;
        }
        private static IncidentPhaseView Phase(SimGame game, EntityId target, string id)
        {
            var phases = new List<IncidentPhaseView>(); game.Seat(0).View.Feature<IncidentsView>().CopyPhases(target, Source, phases);
            foreach (IncidentPhaseView phase in phases) if (phase.Id == id) return phase;
            throw new InvalidOperationException("missing authored phase " + id);
        }
        private static CommandResult Raise(SimGame game, EntityId target, int seat = 0)
            => TestKit.Order(game, seat, new RaiseIncidentCommand { Target = target, Source = Source, Kind = Kind });
        private static void Invalid(Check c, Action<JsonValue> edit, string reason)
        {
            JsonValue document = Json.Parse(Document); edit(document);
            c.Throws<ContentException>(() => New(Factory(Text(document))), reason);
        }
        private static JsonValue Workflow(JsonValue json) => json.Get("incident_workflows")[0];
        private static JsonValue Activity(JsonValue json) => json.Get("activities")[0];
        private static void Contracts(Check c)
        {
            SimGame game = New(Factory()); Entity target = Target(game), foreign = Target(game, 1);
            Incident incident = target.Get<Incident>(); IIncidentWorkflowDefinition workflow = incident.Workflow;
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "existing native record layouts remain unchanged");
            c.That(incident is IPhasedIncidentResponse && target.Get<IWorkOrderRetention>() == null, "named incident history does not collide with singleton disposition ownership");
            c.Equal(5, workflow.Phases.Count, "authored phase labels have no mandatory oil enum");
            c.Equal(5, workflow.Response.Graph.Nodes.Count, "phases share the existing graph");
            c.Equal(1, workflow.Response.Graph.Find("cleanup").Dependencies.Count, "cleanup branches after isolation, not repair");
            c.Equal(2, workflow.Response.Graph.Find("restart").Dependencies.Count, "restart requires both parallel branches");
            c.Throws<NotSupportedException>(() => ((IList<IIncidentPhaseDefinition>)workflow.Phases).Clear(), "workflow phase array is immutable");
            c.Throws<NotSupportedException>(() => ((IList<string>)workflow.Phases[0].Nodes).Clear(), "phase mappings are immutable");
            IncidentsView view = game.Seat(0).View.Feature<IncidentsView>();
            c.That(view.TryGetLifecycle(target.Id, Source, out IncidentLifecycleView before) && before.Snapshot.Episode == 0 &&
                before.Snapshot.RaisedTick == -1 && before.Snapshot.ResolvedTick == -1 && before.OrderId == 0 && !before.OrderState.HasValue,
                "never-raised is explicit, without invented work progress");
            var phases = new List<IncidentPhaseView>();
            c.That(view.CopyPhases(target.Id, Source, phases) && phases.Count == 5 && phases[0].Nodes.Count == 0 && !phases[0].Complete,
                "pending authored metadata is not fabricated execution");
            c.That(!view.TryGetLifecycle(foreign.Id, Source, out _), "foreign lifecycle stays private");
            c.That(!view.CopyPhases(foreign.Id, Source, phases) && phases.Count == 0, "foreign phase read clears caller output");
            c.Throws<ArgumentOutOfRangeException>(() => game.World.ViewFor(new PlayerId(-2)), "arbitrary negative viewers are not observers");
            c.That(game.World.ViewFor(PlayerId.None).Feature<IncidentsView>().TryGetLifecycle(target.Id, Source, out _), "explicit observer may inspect engine facts");
            c.That(!view.TryGetLifecycle(target.Id, Code.None, out _) && !view.TryGetLifecycle(target.Id, new Code("absent"), out _), "source is explicit and exact");
            c.Equal(Codes.NotOwned, Raise(game, foreign.Id).Refusal, "ordinary command cannot affect another company");
            SimGame other = New(Factory()); Entity otherTarget = Target(other);
            ulong hash = game.Hash();
            c.That(!incident.Raise(other.World, Kind), "cross-session calls cannot mutate an identically numbered entity");
            c.Equal(hash, game.Hash(), "foreign-session refusal is nonmutating");
            Invalid(c, j => Workflow(j).Set("phases", JsonValue.NewArray()), "empty phase set fails content admission");
            Invalid(c, j => Workflow(j).Get("phases")[1].Set("id", JsonValue.Of("detection")), "duplicate phase identity fails");
            Invalid(c, j => Workflow(j).Get("phases")[0].Set("id", JsonValue.Of("Bad Phase")), "invalid phase identity fails");
            Invalid(c, j => Workflow(j).Get("phases")[0].Set("nodes", JsonValue.NewArray()), "empty phase node mapping fails");
            Invalid(c, j => Workflow(j).Get("phases")[0].Set("nodes", Json.Parse("[\"absent\"]")), "missing activity node fails");
            Invalid(c, j => Workflow(j).Get("phases")[1].Set("nodes", Json.Parse("[\"detect\"]")), "multiply mapped activity node fails");
            Invalid(c, j => { var phases = JsonValue.NewArray(); for (int i = 0; i < 4; i++) phases.Add(Workflow(j).Get("phases")[i]); Workflow(j).Set("phases", phases); }, "uncovered activity node fails");
            Invalid(c, j => Workflow(j).Set("response", JsonValue.Of("absent")), "missing response activity fails");
            Invalid(c, j => Workflow(j).Set("kind", JsonValue.Of("")), "missing incident kind fails");
            Invalid(c, j => Activity(j).Remove("on_complete"), "final physical response completion is mandatory");
            Invalid(c, j => j.Get("response_effects")[0].Set("bound_response", JsonValue.Of(false)), "response source is not implicit");
            Invalid(c, j => { j.Get("response_effects")[0].Remove("bound_response"); j.Get("response_effects")[0].Set("source", JsonValue.Of("leak")); }, "workflow recovery binds exact executing episode");
            Invalid(c, j => Activity(j).Set("replace_with", JsonValue.Of("tank")), "response cannot replace its episode owner");
            Invalid(c, j => Activity(j).Set("player_orderable", JsonValue.Of(true)), "unbound player orders cannot bypass episode admission");
            Invalid(c, j => Activity(j).Get("nodes")[1].Set("effect", JsonValue.Of("leak.resolve")), "whole response completion cannot run as a node effect");
            Invalid(c, j => Activity(j).Get("nodes")[0].Set("after", Json.Parse("[\"restart\"]")), "cycles use native DAG validation");
            Invalid(c, j => { var nodes = Activity(j).Get("nodes"); for (int i = 0; i < nodes.Count; i++) nodes[i].Remove("roles"); }, "timers cannot replace physical responders");
            Invalid(c, j => { var nodes = Activity(j).Get("nodes"); for (int i = 0; i < nodes.Count; i++) nodes[i].Set("duration", Content.Json.Parse("{\"numerator\":0,\"denominator\":1,\"unit\":\"hour\"}")); }, "zero time cannot replace physical response");
            Invalid(c, j => j.Get("entities")[0].Get("components").Get("incident").Set("workflow", JsonValue.Of("absent")), "component workflow reference must resolve");
            Invalid(c, j => j.Get("entities")[0].Get("components").Get("incident").Get("states").Set("active", JsonValue.Of("absent")), "active state is explicitly authored");
            Invalid(c, j => Workflow(j).Set("legacy_phase", JsonValue.Of("repair")), "unknown keys are not compatibility aliases");
            c.Throws<ArgumentException>(() => new IncidentLifecycleSnapshot(Kind, 0, 0, -1), "unraised cannot contain an invented raised tick");
            c.Throws<ArgumentException>(() => new IncidentLifecycleSnapshot(Kind, 1, 10, 9), "resolution cannot precede the incident");
            c.Throws<ArgumentException>(() => new IncidentLifecycleSnapshot(Kind, 1, 10, -2), "only minus one means unresolved");
            // Staged codec fault injection; a rejected decode preserves the actual episode owner.
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); int episode = 1; long raised = -1, resolved = -1;
                writer.Int(ref episode); writer.Long(ref raised); writer.Long(ref resolved); byte[] bytes = stream.ToArray();
                c.Throws<CorruptStateException>(() => incident.Sync(new StateReader(bytes, 0, bytes.Length)), "malformed episode codec is rejected");
            }
            c.Equal(hash, game.Hash(), "failed staged codec does not partially overwrite prior state");
            c.That(otherTarget.Get<Incident>().ReadLifecycle().Episode == 0, "other session's incident remains isolated");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity target = Target(game), foreign = Target(game, 1);
            WideTimeTests.SetTick(game, 1L << 40);
            Incident incident = target.Get<Incident>(); ResourceId steel = game.World.Content.ResourceId("steel");
            IInventory inventory = target.Get<IInventory>(); WorkBoard board = game.World.Services.Get<WorkBoard>();
            IncidentsView view = game.Seat(0).View.Feature<IncidentsView>();
            Entity crane = game.World.Entities.Get(Colony.Mine(game, 0, "crane")[0].Id);
            FixVec2 initialPosition = crane.Get<global::Beep.SimEngine.Maps.ITilePresence>().Position;
            c.That(Raise(game, target.Id).Accepted, "episode starts through ordinary public player command");
            c.That(view.TryGetResponseOrder(target.Id, Source, out int id), "engine posts the existing authoritative work order");
            Order order = board.Order(id);
            c.Equal((1L << 40), incident.ReadLifecycle().RaisedTick, "incident timestamps retain wide game time");
            c.That(incident.ReadLifecycle().Active && target.State == new Code("state.stopped"), "entity Update settles active state; no instant repair");
            c.Equal(IncidentCodes.StaleResponse, incident.CompleteResponse(game.World, order), "unfinished native graph cannot clear incident");
            IncidentPhaseView captured = Phase(game, target.Id, "detection"); ulong raisedHash = game.Hash();
            view.TryGetLifecycle(target.Id, Source, out _); Phase(game, target.Id, "repair");
            c.Equal(raisedHash, game.Hash(), "phase reads cannot advance work or mutate state");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "initial episode and graph restore together");
            int waited = 0;
            while (order.Node("repair").Progress.WholeTicks == 0 && waited++ < 200) { game.RunTicks(1); twin.RunTicks(1); }
            c.That(order.Node("repair").Progress.WholeTicks > 0, "bounded diagnostic: repair starts physically; state=" + order.State + ", blocker=" + order.Blocker);
            c.Equal(game.Hash(), twin.Hash(), "travel, detection and physical isolation preserve twin identity");
            c.That(crane.Get<global::Beep.SimEngine.Maps.ITilePresence>().Position != initialPosition, "response machine really travels");
            c.That(Phase(game, target.Id, "detection").Complete && Phase(game, target.Id, "isolation").Complete, "completed native nodes derive phase completion");
            c.That(!captured.Complete && captured.Nodes[0].Progress.WholeTicks == 0, "captured early phase is detached from later progress");
            IncidentPhaseView repair = Phase(game, target.Id, "repair"), cleanup = Phase(game, target.Id, "cleanup");
            c.That(repair.Nodes[0].JobId > 0 && repair.Nodes[0].Worker.IsValid && repair.Nodes[0].Progress.WholeTicks > 0, "phase exposes actual assigned native worker/progress");
            c.That(cleanup.Nodes[0].Blocker == new Code("leak.cleanup-disabled") && !cleanup.Complete, "typed requirement blocks only its parallel branch");
            c.That(order.Node("restart").Progress.WholeTicks == 0, "restart waits for both branches");
            c.Equal(Fix.FromInt(26), inventory.Amount(steel), "isolation consumes one and repair consumes three actual units");
            c.Equal(Fix.FromInt(9993), ((TestLedger)game.World.Services.Get<IEconomy>()).Cash(target.Owner), "repair bill charges exactly once");
            c.Equal(Fix.FromInt(2), inventory.ReservedOut(steel), "physical tool reserve remains a native promise");
            long progress = order.Node("repair").Progress.WholeTicks;
            // Stop at an actual cancellation checkpoint before the automatic response phase resumes.
            game.World.System<WorkSystem>().Cancel(game.World, order);
            c.That(order.State == OrderState.Cancelled && incident.ReadLifecycle().Active && incident.ReadLifecycle().ResolvedTick == -1, "cancelled scheduling does not resolve incident");
            c.Equal(Fix.Zero, inventory.ReservedOut(steel), "cancellation releases unspent tool promise");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "cancelled partial response is a valid saved checkpoint");
            game.RunTicks(1); twin.RunTicks(1);
            c.That(view.TryGetResponseOrder(target.Id, Source, out int resumed) && resumed == id, "resumption uses the same work authority and identity");
            c.That(order.Node("repair").Progress.WholeTicks >= progress && order.Node("isolate").State == NodeState.Done, "partial elapsed work and completed stages are preserved");
            c.Equal(Fix.FromInt(26), inventory.Amount(steel), "resumption cannot repeat effects or material consumption");
            c.Equal(Fix.FromInt(9993), ((TestLedger)game.World.Services.Get<IEconomy>()).Cash(target.Owner), "resumption cannot double-charge spent cost");
            c.Equal(game.Hash(), twin.Hash(), "native promise reacquisition and fresh jobs are deterministic");
            c.Equal(Fix.FromInt(2), inventory.ReservedOut(steel), "resumption reacquires released tools through native commitments");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "re-enrolled checkpoint also restores");
            c.That(TestKit.Order(game, 0, new SetWorkPermitCommand { Target = target.Id, Enabled = true }).Accepted, "player enables authored cleanup requirement through engine command");
            c.That(TestKit.Order(twin, 0, new SetWorkPermitCommand { Target = target.Id, Enabled = true }).Accepted, "same player input replays on restored twin");
            bool parallel = false; waited = 0;
            while (incident.ReadLifecycle().Active && waited++ < 240)
            {
                game.RunTicks(1); twin.RunTicks(1);
                parallel |= order.Node("repair").State == NodeState.Working && order.Node("cleanup").State == NodeState.Working;
            }
            c.That(!incident.ReadLifecycle().Active, "bounded diagnostic: whole response completes; state=" + order.State + ", blocker=" + order.Blocker);
            c.That(parallel, "native scheduler works repair and cleanup in parallel");
            c.Equal(game.Hash(), twin.Hash(), "saved interrupted workflow completes identically in interleaved sessions");
            c.That(view.TryGetLifecycle(target.Id, Source, out IncidentLifecycleView completed) && completed.OrderId == id &&
                completed.OrderState == OrderState.Done && completed.Snapshot.ResolvedTick >= completed.Snapshot.RaisedTick,
                "completed phase history remains in its sole native board");
            foreach (var phase in incident.Workflow.Phases) c.That(Phase(game, target.Id, phase.Id).Complete, "authored phase finishes: " + phase.Id);
            c.That(!view.TryGetResponseOrder(target.Id, Source, out _), "completed incident has no active order");
            c.Equal(target.Def.InitialState.State, target.State, "entity Update settles restored state only after completion");
            c.Equal(Fix.Zero, inventory.ReservedOut(steel), "completion releases actual held tools");
            c.Equal(Fix.FromInt(26), inventory.Amount(steel), "complete workflow spends no duplicate materials");
            SimGame completedSave = factory.Load(game.Save()); c.Equal(game.Hash(), completedSave.Hash(), "resolved phase history survives save/load");
            c.That(completedSave.Seat(0).View.Feature<IncidentsView>().TryGetLifecycle(target.Id, Source, out var restored) && restored.Snapshot.ResolvedTick == completed.Snapshot.ResolvedTick,
                "resolved timestamp is persisted, not guessed from order age");
            c.That(!foreign.Get<Incident>().ReadLifecycle().Active && foreign.Get<IInventory>().Amount(steel) == Fix.FromInt(30), "response cannot touch another company");
            c.That(Raise(game, target.Id).Accepted, "new incident gets a new explicit episode");
            c.That(view.TryGetResponseOrder(target.Id, Source, out int next) && next != id && incident.ReadLifecycle().Episode == 2, "new episode starts a fresh graph rather than reuse completed work");
            ulong nextHash = game.Hash();
            c.Equal(IncidentCodes.StaleResponse, incident.CompleteResponse(game.World, order), "previous episode's completed record cannot clear new damage");
            c.Equal(IncidentCodes.StaleResponse, incident.CompleteResponse(game.World, new Order { Id = next }), "forged record cannot bypass native ownership");
            c.Equal(nextHash, game.Hash(), "stale and forged completion refusals leave current state unchanged");
            c.That(!completed.Snapshot.Active && completed.Snapshot.Episode == 1, "captured completed facts do not change during a new episode");
            for (int i = 0; i < 4; i++) ResponseWork.EnsurePosted(game.World, target, incident);
            int live = 0; foreach (Order item in board.Orders) if (item.IsActive && item.Target == target.Id && item.ResponseSource == Source) live++;
            c.Equal(1, live, "repeated dispatch does not duplicate live incident work");
            FailureAndHistory(c);
        }

        private static void FailureAndHistory(Check c)
        {
            JsonValue bad = Json.Parse(Document);
            Activity(bad).Get("nodes")[1].Set("requirement", JsonValue.Of("leak.cleanup-permit"));
            bad.Get("entities")[0].Get("components").Remove("work_permit");
            GameFactory factory = Factory(Text(bad)); SimGame game = New(factory); Entity target = Target(game);
            c.That(Raise(game, target.Id).Accepted, "failed provider fixture still raises through public command");
            WorkBoard board = game.World.Services.Get<WorkBoard>();
            Order order = board.ActiveResponseOn(target.Id, Source);
            TestKit.RunUntil(game, () => order.State == OrderState.Failed, 40, 1);
            c.That(order.State == OrderState.Failed && order.Blocker == new Code("leak.missing-permit"), "invalid requirement produces the real permanent blocker");
            SimGame restored = factory.Load(game.Save()); int count = board.Orders.Count;
            game.RunTicks(30); restored.RunTicks(30);
            c.Equal(count, board.Orders.Count, "permanent failure cannot repeatedly create new response orders");
            c.That(board.ActiveResponseOn(target.Id, Source) == null && target.Get<Incident>().ReadLifecycle().Active, "failed work leaves actual unresolved incident intact");
            c.That(game.Seat(0).View.Feature<IncidentsView>().TryGetLifecycle(target.Id, Source, out var failure) && failure.OrderId == order.Id &&
                failure.OrderState == OrderState.Failed && failure.Blocker == new Code("leak.missing-permit"), "failed episode remains inspectable without an active order");
            c.Equal(game.Hash(), restored.Hash(), "saved permanent failure remains deterministic and idle");
            // Semantic checkpoint faults, deliberately injected through the component codec.
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); int episode = 1; long raised = game.World.Tick + 1, resolved = -1;
                writer.Int(ref episode); writer.Long(ref raised); writer.Long(ref resolved); byte[] bytes = stream.ToArray();
                target.Get<Incident>().Sync(new StateReader(bytes, 0, bytes.Length));
                c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "future incident timestamp cannot restore as a valid game episode");
            }
            JsonValue paired = Json.Parse(Document); JsonValue component = paired.Get("entities")[0].Get("components").Get("incident");
            var sources = JsonValue.NewArray(); sources.Add(component); JsonValue second = component.Clone(); second.Set("id", JsonValue.Of("secondary")); sources.Add(second);
            paired.Get("entities")[0].Get("components").Set("incident", sources);
            factory = Factory(Text(paired)); game = New(factory); target = Target(game);
            c.That(TestKit.Order(game, 0, new RaiseIncidentCommand { Target = target.Id, Source = new Code("secondary"), Kind = Kind }).Accepted,
                "multiple named incidents can share the same component type and workflow");
            c.That(!target.Get<Incident>(Source).ReadLifecycle().Active && target.Get<Incident>(new Code("secondary")).ReadLifecycle().Active,
                "one named source does not raise its neighbor");
            // The board selects the exact named incident episode, not the first component.
            Order secondary = game.World.Services.Get<WorkBoard>().ActiveResponseOn(target.Id, new Code("secondary"));
            c.That(game.World.Services.Get<WorkBoard>().RetainsOrder(secondary), "later named response owner retains its own history");
            restored = factory.Load(game.Save()); c.Equal(game.Hash(), restored.Hash(), "multiple named incident state owners restore independently");
        }
    }
}
