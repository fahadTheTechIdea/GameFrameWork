using System;
using System.Collections.Generic;
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
    public static partial class ConstructionPlanTests
    {
        private static GameFactory RemovalFactory(Action<JsonValue> edit = null)
        {
            JsonValue authored = Content.Json.Parse(FrameworkProofContent.Read("removal-workflows.json")); edit?.Invoke(authored);
            // Removal is a resource-only fixture. It has no purchase/sale workflow or active Tycoon.
            var physical = Content.Json.Parse(FrameworkProofContent.Read("resource-recovery.json"));
            for (int i = 0; i < physical.Get("entities").Count; i++)
                if (physical.Get("entities")[i].Get("id").Text == "market")
                    physical.Get("entities")[i].Get("components").Remove("supplier");
            var documents = new Dictionary<string, string>();
            foreach (var pair in new[] { ("colony-navigation.json", "layered_tile_navigation"), ("colony-placement.json", "tile_construction_rules"),
                ("colony-conditions.json", "tile_layer_band_conditions"), ("colony-startup.json", "tile_layer_setups") })
            {
                var document = Content.Json.Parse(FrameworkProofContent.Read(pair.Item1));
                var rows = document.Get(pair.Item2);
                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i].Has("space")) rows[i].Set("space", JsonValue.Of("recovery.tiles"));
                    if (rows[i].Has("layer") && rows[i].Get("layer").Text == "colony.lanes") rows[i].Set("layer", JsonValue.Of("road"));
                }
                if (pair.Item1 == "colony-startup.json")
                    for (int i = 0; i < document.Get("entity_setups").Count; i++)
                        document.Get("entity_setups")[i].Get("placement").Set("space", JsonValue.Of("recovery.tiles"));
                documents.Add(pair.Item1, Text(document));
            }
            physical.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(WorldSetupModule.ModuleId));
            physical.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(TileConditionsModule.ModuleId));
            physical.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(TileConstructionRulesModule.ModuleId));
            physical.Get("scenarios")[0].Get("settings").Set(WorldSetupModule.ModuleId, Content.Json.Parse("{\"profile\":\"colony.start\"}"));
            physical.Get("scenarios")[0].Get("settings").Set(TileConstructionRulesModule.ModuleId, Content.Json.Parse("{\"profile\":\"colony.placement\"}"));
            return Colony.Factory(Text(physical), FrameworkProofContent.Read("endpoints-flows.json"),
                additionalDocuments: new Dictionary<string, string> { ["removal-workflows.json"] = Text(authored) }, moduleDocuments: documents);
        }
        private static IEntityTargetPlanDefinition Removal(SimGame game, string prototype)
            => game.World.Content.Get<EntityDef>(prototype).Component<IConstructionDefinition>().Demolition;
        private static PlanIntent Remove(SimGame game, Entity target, bool paused = true, IActivityWorkerSelection workers = null)
        {
            PlanIntent intent = Removal(game, target.Def.Id).CreateTargetIntent(target.Id, workers ?? ActivityWorkerSelection.Unbound, "field.construction", 70);
            return new PlanIntent(intent.Definition, intent.Arguments, intent.Group, intent.Priority, paused);
        }
        private static Fix RemovalStock(SimGame game, ResourceId resource)
        {
            Fix total = Fix.Zero; foreach (Entity entity in game.World.Entities.All)
                if (entity.Owner == new PlayerId(0) || entity.Def.Id == "recovery.container")
                { IInletPort port = entity.Get<IInletPort>(); if (port != null) total += port.Amount(resource); }
            return total;
        }
        private static void RemovalAdmission(Check c)
        {
            GameFactory factory = RemovalFactory(); SimGame game = New(factory);
            // Initial-world fixtures only; every timed action below is a public player command.
            Entity target = Spawn(game, "removal.tower", 20, 16), foreign = Spawn(game, "removal.tower", 28, 16, 1);
            IEntityTargetPlanDefinition provider = Removal(game, target.Def.Id); WorkBoard board = game.World.Services.Get<WorkBoard>();
            c.That(ReferenceEquals(provider, board.ResolvePlan("demolish:removal.tower")), "removal provider is the existing exact authored graph alias");
            c.That(ReferenceEquals(provider, provider.BindExecution(game.World, target.Id)), "fixed removal graph has no copied execution model");
            c.Equal(2, provider.Graph.Nodes.Count, "master-listed JSON authors both removal stages");
            c.Equal("isolate", provider.Graph.Find("remove").Dependencies[0], "same activity compiler resolves removal dependencies");
            c.That(game.World.Content.Get<EntityDef>(target.Def.Id).AuthoringPath.Contains("removal-workflows.json"), "master owns the removal prototype");
            c.Throws<ContentException>(() => provider.ValidateTargetDefinition(game.World.Content.Get<EntityDef>("factory")), "foreign prototype is not a compatible removal target");
            PlanIntent intent = Remove(game, target); ulong hash = game.Hash();
            c.Equal(Code.None, View(game).PreviewPlan(intent), "pending native removal is valid");
            c.Equal(Code.None, game.Seat(0).View.Feature<ConstructionView>().CheckDemolition(target.Id), "matching immediate removal preview is engine-owned");
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<ConstructionView>().CheckDemolition(target.Id), "foreign company cannot preview another target's removal");
            c.Equal(Codes.NotOwned, View(game).PreviewPlan(Remove(game, foreign)), "foreign target intent refuses before execution");
            foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                j => j.Remove("target"), j => j.Set("target", JsonValue.Number("0")),
                j => j.Set("target", JsonValue.Of("1")), j => j.Set("placement", JsonValue.NewObject()),
                j => j.Set("workers", JsonValue.NewArray()) })
            { JsonValue bad = intent.Arguments.Clone(); edit(bad); c.Equal(Codes.InvalidArgument, View(game).PreviewPlan(new PlanIntent(intent.Definition, bad, null, 1, true)), "strict target/selection schema refuses missing or extra data"); }
            Entity crane = FindDefinition(game, "crane"), truck = FindDefinition(game, "truck");
            var selected = new ActivityWorkerSelection(new[] { new ActivityWorkerBinding("remove", crane.Id) });
            c.Equal(Code.None, View(game).PreviewPlan(Remove(game, target, true, selected)), "normal explicit compatible machine selection applies to removal");
            c.Equal(WorkSystem.BlockerNoWorker, View(game).PreviewPlan(Remove(game, target, true,
                new ActivityWorkerSelection(new[] { new ActivityWorkerBinding("remove", truck.Id) }))), "incompatible machine is never replaced silently");
            c.Throws<ContentException>(() => Remove(game, target, true, new ActivityWorkerSelection(new[] { new ActivityWorkerBinding("missing", crane.Id) })), "selection must name an authored removal stage");
            c.Equal(hash, game.Hash(), "all removal previews preserve plans, stock, time and worker claims");
            long id = Submit(c, game, intent); c.Equal(PlanState.Pending, Read(game, id).State, "paused intent creates no work execution");
            c.Equal(0, Read(game, id).Order, "there is no phantom removal order");
            c.Equal(PlanCodes.Duplicate, View(game).PreviewPlan(intent), "same live removal intent cannot be posted twice");
            var hidden = new List<PlanView>(); game.Seat(1).View.Feature<WorkView>().CopyPlans(hidden); c.Equal(0, hidden.Count, "foreign immutable plan view has no removal details");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "pending removal has the ordinary durable plan schema");
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { id }) }).Accepted && target.Alive, "pending cancel earns no removal or salvage");
            foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                j => j.Set("priority", JsonValue.Number("-1")), j => j.Remove("automatic_supply"), j => j.Set("nodes", JsonValue.NewArray()),
                j => j.Get("nodes")[0].Remove("duration"), j => j.Get("nodes")[0].Set("roles", Content.Json.Parse("[\"missing\"]")),
                j => j.Get("nodes")[0].Remove("roles"),
                j => j.Get("nodes")[0].Set("after", Content.Json.Parse("[\"remove\"]")),
                j => j.Get("nodes")[1].Set("after", Content.Json.Parse("[\"missing\"]")),
                j => j.Set("role", JsonValue.Of("crane")) })
                c.Throws<ContentException>(() => New(RemovalFactory(root => edit(root.Get("activities")[1]))), "missing contracts, cycles, unknown references and removed shorthand fail before update");
            c.Throws<ContentException>(() => New(RemovalFactory(root => root.Get("entities")[0].Get("components").Get("construction").Get("demolish").Remove("activity"))), "removal must reference an authored workflow");
            c.Throws<ContentException>(() => New(RemovalFactory(root => root.Get("entities")[0].Get("components").Get("construction").Get("demolish").Remove("salvage"))), "removal salvage declaration remains explicit");
            c.Throws<ContentException>(() => New(RemovalFactory(root => {
                JsonValue demolition = root.Get("activities")[1];
                foreach (JsonValue node in new[] { demolition.Get("nodes")[0], demolition.Get("nodes")[1] }) node.Set("duration", Content.Json.Parse("{\"numerator\":0,\"denominator\":1,\"unit\":\"hour\"}"));
            })), "authoring cannot turn removal into an instant machine-free fallback");
            SimGame unavailable = New(RemovalFactory(root => root.Get("entities")[0].Get("components").Get("construction").Remove("demolish")));
            Entity noRemoval = Spawn(unavailable, "removal.tower", 20, 16);
            c.Equal(ConstructionService.NotConstructible, unavailable.Seat(0).Preview(new DemolishCommand { Target = noRemoval.Id }), "absent removal authoring creates no default activity");
        }
        private static void RemovalPhysical(Check c)
        {
            foreach (bool point in new[] { false, true })
            {
                GameFactory factory = point ? Points() : RemovalFactory(); SimGame game = point ? NewPoints(factory) : New(factory);
                if (point) EmergencyPreemptionTests.Configure(c, game);
                string prototype = point ? "point.tower" : "removal.tower";
                long build = Submit(c, game, Build(game, prototype, point ? 12 : 10, false));
                Reach(game, () => Read(game, build).State == PlanState.Done, "player physically builds removal target");
                Entity target = FindDefinition(game, prototype); c.That(target != null, "target is a real player-constructed facility");
                ResourceId steel = point ? game.World.Content.ResourceId("supply") : game.World.Content.ResourceId("steel");
                IInventory inventory = target.Get<IInventory>(); var owner = new ReservationOwner("proof.removal.external", 1);
                ReservationToken promise = inventory.ReserveIn(owner, steel, Fix.One);
                var direct = new DemolishCommand { Target = target.Id }; ulong hash = game.Hash();
                c.Equal(ResourceTransactions.ActivePromises, game.Seat(0).Preview(direct), "immediate command retains actual evacuation preflight");
                c.Equal(ResourceTransactions.ActivePromises, game.Seat(0).View.Feature<ConstructionView>().CheckDemolition(target.Id), "management preview matches that same immediate preflight");
                c.Equal(hash, game.Hash(), "physical preflight cannot erase another owner's promise");
                IActivityWorkerSelection chosen = point ? ActivityWorkerSelection.Unbound :
                    new ActivityWorkerSelection(new[] { new ActivityWorkerBinding("remove", FindDefinition(game, "crane").Id) });
                long removal = Submit(c, game, Remove(game, target, false, chosen));
                c.Equal(PlanState.Pending, Read(game, removal).State, "planned removal waits honestly for a committed inbound receipt");
                c.Equal(ResourceTransactions.ActivePromises, Read(game, removal).Blocker, "pending blocker is calculated by actual removal owner");
                c.Equal(1, inventory.Reservations.Count, "intent cannot release a foreign promise");
                SimGame twin = factory.Load(game.Save()); game.Clock.Pause(); ulong stopped = game.Hash();
                c.Equal(0, game.Update(), "one paused game clock stops every removal stage"); c.Equal(stopped, game.Hash(), "paused update earns no independent removal time"); game.Clock.Resume();
                inventory.ReleaseIn(owner, promise); twin.World.Entities.Get(target.Id).Get<IInventory>().ReleaseIn(owner, promise);
                Reach(game, () => Read(game, removal).State == PlanState.Active, "released receipt allows normal plan promotion", twin);
                WorkBoard board = game.World.Services.Get<WorkBoard>(); Order order = board.Order(Read(game, removal).Order);
                c.That(ReferenceEquals(order.Plan, Removal(game, prototype)) && order.Target == target.Id, "native order aliases exact existing target and authored DAG");
                c.Equal(chosen.WorkerFor(point ? "demolish" : "remove"), order.WorkerSelection.WorkerFor(point ? "demolish" : "remove"), "physical execution retains the actual selected machine policy");
                Reach(game, () => order.Nodes[0].Progress.WholeTicks > 0, "machine travels and earns actual removal progress", twin);
                Pause(c, game, removal, true); Pause(c, twin, removal, true); WorkProgress progress = order.Nodes[0].Progress;
                for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); }
                c.Equal(progress, order.Nodes[0].Progress, "policy pause retains earned physical work without advancing it"); c.That(target.Alive, "paused machine cannot instantly demolish target");
                c.Equal(game.Hash(), twin.Hash(), "paused target/work/material state continues identically");
                twin = factory.Load(game.Save()); Pause(c, game, removal, false); Pause(c, twin, removal, false);
                if (!point)
                {
                    Reach(game, () => order.Node("remove").Progress.WholeTicks > 0, "selected machine reaches dependent removal stage", twin);
                    c.Equal(chosen.WorkerFor("remove"), board.Job(order.Node("remove").JobId).Worker, "dependent physical stage is executed by the selected machine");
                    c.Equal(NodeState.Done, order.Node("isolate").State, "authored dependency finishes before removal earns work");
                }
                Reach(game, () => Read(game, removal).State == PlanState.Done, "physical removal and normal salvage finish", twin);
                c.That(!target.Alive && game.World.Entities.Get(target.Id) == null, "actual facility disappears only after authored timed work");
                c.Equal(game.Hash(), twin.Hash(), "removed target and terminal history survive saved continuation");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal removal can save with no live target");
                c.Equal(Codes.NotFound, View(game).PreviewPlanRepeat(removal), "repeat never substitutes a different target for a removed entity");
                if (!point) c.Equal(Fix.FromInt(33), RemovalStock(game, steel), "fifty real units minus thirty build minus two removal plus fifteen authored salvage");
            }
            GameFactory cancelFactory = RemovalFactory(); SimGame cancel = New(cancelFactory);
            long construction = Submit(c, cancel, Build(cancel, "removal.tower", 10, false)); Reach(cancel, () => Read(cancel, construction).State == PlanState.Done, "cancellation target built normally");
            Entity preserved = FindDefinition(cancel, "removal.tower");
            CommandResult command = TestKit.Order(cancel, 0, new DemolishCommand { Target = preserved.Id }); c.That(command.Accepted, "ordinary removal command creates a persistent plan");
            c.That(command.Arg0 > 0 && Read(cancel, command.Arg0).Target == preserved.Id && command.Entity == preserved.Id, "command result names plan plus actual target, never a legacy order ID");
            c.Equal(67, Read(cancel, command.Arg0).Priority, "ordinary command uses the actual JSON-authored removal priority, not an engine constant");
            Order execution = cancel.World.Services.Get<WorkBoard>().Order(Read(cancel, command.Arg0).Order);
            Reach(cancel, () => execution.Nodes[0].Progress.WholeTicks > 0, "cancellation follows some real earned work");
            c.That(TestKit.Order(cancel, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { command.Arg0 }) }).Accepted, "shared cancellation controls active removal");
            Reach(cancel, () => Read(cancel, command.Arg0).State == PlanState.Cancelled, "native cancellation settles board");
            c.That(preserved.Alive, "cancelled removal cannot destroy facility");
            c.Equal(Fix.FromInt(18), RemovalStock(cancel, cancel.World.Content.ResourceId("steel")), "cancellation preserves actual two spent inputs and awards no salvage");
            c.Equal(cancel.Hash(), cancelFactory.Load(cancel.Save()).Hash(), "cancelled physical removal is durable");
        }
    }
}
