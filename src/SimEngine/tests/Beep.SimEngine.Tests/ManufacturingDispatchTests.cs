using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class ManufacturingDispatchTests
    {
        public const string Case = "Manufacturing dispatch: authored physical preparation, sole queue and timed saved production";
        private static readonly Code Capability = new Code("dispatch.production");
        public static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(Action<JsonValue> manufacturing = null, Action<JsonValue> work = null,
            string workDocument = "manufacturing-dispatch-work.json", string operationsDocument = null,
            string startupDocument = "manufacturing-dispatch-startup.json", GameModule extension = null,
            string extensionDocument = null, string extensionText = null)
        {
            var modules = new List<GameModule> {
                new ContentAccessModule(), new InventoryModule(), new OperationsModule(), new TransformationModule(),
                new MotionModule(), new TileNavigationModule(), new WorkModule(), new CompanyKnowledgeModule(),
                new WorldSetupModule(), new InfrastructureModule() };
            if (extension != null) modules.Add(extension);
            var definition = new GameDefinition("manufacturing-dispatch", modules.ToArray());
            JsonValue factory = Json.Parse(FrameworkProofContent.Read("manufacturing-dispatch.json")); manufacturing?.Invoke(factory);
            JsonValue preparation = Json.Parse(FrameworkProofContent.Read(workDocument)); work?.Invoke(preparation);
            JsonValue master = Json.Parse(FrameworkProofContent.Read("manufacturing-dispatch-master.json"));
            foreach (JsonValue module in Rows(master.Get("modules")))
            {
                if (module.Get("id").Text == OperationsModule.ModuleId && operationsDocument != null)
                    module.Get("documents").Add(JsonValue.Of(operationsDocument));
                var documents = JsonValue.NewArray();
                foreach (JsonValue document in Rows(module.Get("documents")))
                    documents.Add(JsonValue.Of(document.Text == "manufacturing-dispatch-work.json" ? workDocument :
                        document.Text == "manufacturing-dispatch-startup.json" ? startupDocument : document.Text));
                module.Set("documents", documents);
            }
            if (extension != null)
            {
                var declared = JsonValue.NewObject(); declared.Set("id", JsonValue.Of(extension.Id));
                declared.Set("version", JsonValue.Of(extension.Version.ToString()));
                var required = JsonValue.NewArray();
                foreach (var requirement in extension.Requires)
                {
                    var row = JsonValue.NewObject(); row.Set("id", JsonValue.Of(requirement.Id));
                    row.Set("version", JsonValue.Of(requirement.Version.ToString())); required.Add(row);
                }
                declared.Set("requires", required); var documents = JsonValue.NewArray();
                documents.Add(JsonValue.Of(extensionDocument)); declared.Set("documents", documents);
                master.Get("modules").Add(declared);
            }
            var files = new PreloadedFileSource().Add("manifest.json", master.ToString())
                .Add("open-access.json", FrameworkProofContent.Access).Add("manufacturing-dispatch.json", factory.ToString())
                .Add(workDocument, preparation.ToString());
            JsonValue world = Json.Parse(FrameworkProofContent.Read("manufacturing-dispatch-world.json"));
            if (extension != null) world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(extension.Id));
            files.Add("manufacturing-dispatch-world.json", world.ToString());
            if (operationsDocument != null) files.Add(operationsDocument, FrameworkProofContent.Read(operationsDocument));
            if (extension != null) files.Add(extensionDocument, extensionText ?? FrameworkProofContent.Read(extensionDocument));
            foreach (string name in new[] { startupDocument,
                "native-survey-navigation.json", "native-survey-work.json", "native-survey-knowledge.json", "work-retry.json",
                "authored-segment-work.json", "authored-segment-infrastructure.json" }) files.Add(name, FrameworkProofContent.Read(name));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static IEnumerable<JsonValue> Rows(JsonValue array)
        { for (int i = 0; i < array.Count; i++) yield return array[i]; }
        internal static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "native-survey", Seed = 757 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity FactoryEntity(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "dispatch.factory");
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static WorkView Work(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static TransformationSnapshot Production(SimGame game)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(FactoryEntity(game).Id, Capability, out var result))
                throw new InvalidOperationException("missing actual named production");
            return result;
        }
        private static List<PlanView> Plans(SimGame game)
        { var result = new List<PlanView>(); Work(game).CopyPlans(result); return result; }
        private static Fix Stock(SimGame game)
        {
            ResourceId steel = game.World.Content.ResourceId("steel"); Fix total = Fix.Zero;
            foreach (Entity entity in game.World.Entities.All) if (entity.Get<IInventory>() is IInventory inventory) total += inventory.Amount(steel);
            total += Production(game).Wip.Stock.AmountOf(steel); return total;
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("manufacturing time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " production=" + Production(game).Phase + "/" + Production(game).Progress + "/" + Production(game).Blocker +
                " nodes=" + string.Join(";", Board(game).Orders.SelectMany(o => o.Nodes.Select(n => n.Id + "/" + n.State + "/" + n.Progress + "/" + n.Blocker))));
        }
        private static long Begin(Check c, SimGame game)
        {
            IEntityTargetPlanDefinition definition = game.World.Content.Get<EntityActivityDef>("dispatch.prepare");
            var command = new SubmitPlanCommand { Intent = definition.CreateTargetIntent(FactoryEntity(game).Id, ActivityWorkerSelection.Unbound, "manufacturing", 50) };
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary engine preview admits authored preparation");
            c.Equal(hash, game.Hash(), "plan preview changes no work or production");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "player submits ordinary production preparation plan");
            return result.Arg0;
        }
        private static void Control(Check c, SimGame game, bool enabled)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = FactoryEntity(game).Id, Operation = Capability, Enabled = enabled, Target = Fix.One }).Accepted,
                "player changes actual factory operation through engine command");
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "strict authored dispatch identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no duplicated saved dispatch or queue state");
            var factory = Factory(); var game = New(factory);
            var coordinator = game.World.Services.Get<ITransformationQueue>();
            c.That(ReferenceEquals(coordinator, game.World.Services.Get<TransformerManager>()), "queue contract aliases SAME execution manager");
            ITransformationDispatchEffect effect = game.World.Content.Get<TransformationDispatchEffectDef>("dispatch.enqueue");
            c.That(ReferenceEquals(effect.Transformation, game.World.Content.Get<TransformationDef>("dispatch.batch")), "JSON effect aliases actual selected transformation");
            c.Equal(Capability, effect.Capability, "named provider not inferred");
            c.That(effect.Count == 2 && !effect.Repeat && !effect.HomeIsTarget, "JSON dispatch intent exact");
            c.Equal(Fix.FromInt(60), Stock(game), "authored factory and workers own real starting materials");
            c.That(!game.World.Services.Has<IEconomy>(), "work and manufacturing independent of optional business");
            ulong hash = game.Hash();
            c.Equal(Code.None, coordinator.CheckEnqueue(new PlayerId(0), FactoryEntity(game).Id, Capability, "dispatch.batch", 2, false, EntityId.None), "same current queue admission as effect");
            c.Equal(Codes.NotFound, coordinator.CheckEnqueue(new PlayerId(1), FactoryEntity(game).Id, Capability, "dispatch.batch", 2, false, EntityId.None), "foreign queue admission shares private preview refusal");
            c.Equal(Codes.NotFound, coordinator.CheckEnqueue(new PlayerId(0), FactoryEntity(game).Id, new Code("missing"), "dispatch.batch", 2, false, EntityId.None), "unknown named provider no fallback");
            c.Equal(hash, game.Hash(), "queue admissions pure");
            long plan = Begin(c, game);
            Reach(game, () => Plans(game).Single(p => p.Id == plan).State == PlanState.Active, "actual authored activation");
            Order order = Board(game).Order(Plans(game).Single(p => p.Id == plan).Order);
            c.Equal("inspect|prepare|finish", string.Join("|", order.Plan.Graph.Nodes.Select(n => n.Id)), "sole graph compiler preserves parallel preparation");
            Reach(game, () => order.Node("inspect").Progress.WholeTicks > 0 && order.Node("prepare").Progress.WholeTicks > 0, "parallel physical preparation");
            c.Equal(0, Production(game).QueueCount, "unearned physical plan dispatches nothing");
            c.Equal(Fix.FromInt(60), Stock(game), "held tool not consumed");
            hash = game.Hash();
            c.Equal(WorkEffectStatus.Failed, game.World.Content.Get<TransformationDispatchEffectDef>("dispatch.enqueue").Apply(game.World, order, null).Status, "unearned effect cannot enqueue");
            c.Equal(hash, game.Hash(), "refused dispatch no mutation");
            c.That(!Work(game, 1).TryCopyNodes(order.Id, new List<ActivityNodeView>()), "preparation facts private");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial preparation saves actual jobs/tool promises");
            Reach(game, () => Plans(game).Single(p => p.Id == plan).State == PlanState.Done, "earned manufacturing dispatch", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved preparation dispatches identically");
            var queued = Production(game);
            c.Equal(2, queued.QueueCount, "one earned effect appends exact two actual batches");
            c.That(queued.Queue.All(q => q.Transformation == "dispatch.batch" && !q.Repeat && !q.Home.IsValid), "actual queue matches authored request");
            c.Equal(TransformationPhase.Idle, queued.Phase, "disabled factory does not instantly manufacture");
            c.Equal(WorkProgress.Zero, queued.Progress, "preparation progress is not copied into production");
            c.Equal(Fix.FromInt(58), Stock(game), "physical final preparation consumes exactly two worker materials");
            for (int i = 0; i < 8; i++) Update(game, twin);
            c.Equal(2, Production(game).QueueCount, "settled effect is never dispatched twice");
            c.Equal(Fix.FromInt(58), Stock(game), "disabled production consumes no manufacturing inputs");
            c.Equal(2, queued.QueueCount, "detached saved queue remains immutable");
            c.Throws<NotSupportedException>(() => ((IList<TransformationQueueEntry>)queued.Queue).Clear(), "host cannot mutate actual queue via snapshot");
            c.That(!game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(FactoryEntity(game).Id, Capability, out _), "queued production private");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "queued earned dispatch checkpoints with existing owners");
            Control(c, game, true); Control(c, twin, true);
            Reach(game, () => Production(game).Phase == TransformationPhase.Working && Production(game).Progress.WholeTicks > 0, "real timed factory work", twin);
            var working = Production(game); ResourceId steel = game.World.Content.ResourceId("steel");
            c.Equal(Fix.FromInt(3), working.Wip.Stock.AmountOf(steel), "actual input WIP moves physically from factory store");
            c.Equal(Fix.One, working.Tools.Held.AmountOf(steel), "actual factory tool reservation held separately from consumables");
            c.Equal(Fix.FromInt(58), Stock(game), "moving inputs to WIP creates or destroys no material");
            c.That(working.Progress.WholeTicks < working.Required && working.Required == 10, "authored production duration not instant");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "active WIP/tool/time restores identically");
            Reach(game, () => Production(game).QueueCount == 1, "first real batch delivery", twin);
            c.Equal(Fix.FromInt(56), Stock(game), "three consumed and one generated only after earned production time");
            c.Equal(game.Hash(), twin.Hash(), "saved first batch matches");
            Reach(game, () => Production(game).QueueCount == 0, "second sequential physical batch", twin);
            c.Equal(Fix.FromInt(54), Stock(game), "second actual batch exact native resource accounting");
            c.Equal(game.Hash(), twin.Hash(), "saved sequential manufacturing identical");
            c.That(Production(game).Wip.Stock.IsEmpty && Production(game).Tools.Held.IsEmpty, "completed production clears only actual WIP/tool commitments");
            c.Equal(0, FactoryEntity(game).Get<IInventory>().Reservations.Count, "all actual production promises released");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed physical pipeline checkpoints");

            game = New(factory); plan = Begin(c, game);
            Reach(game, () => Plans(game).Single(p => p.Id == plan).State == PlanState.Active, "cancel preparation activation");
            order = Board(game).Order(Plans(game).Single(p => p.Id == plan).Order);
            Reach(game, () => order.Node("prepare").Progress.WholeTicks > 0, "cancel held-tool preparation");
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { plan }) }).Accepted, "player cancels preparation before dispatch");
            for (int i = 0; i < 8; i++) Update(game);
            c.Equal(0, Production(game).QueueCount, "cancel cannot dispatch unearned production");
            c.Equal(Fix.FromInt(60), Stock(game), "cancelled held tool consumes/refunds no material");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled preparation saves normally");

            var direct = new EnqueueTransformationCommand { Transformer = FactoryEntity(game).Id, Capability = Capability, Transformation = "dispatch.batch", Count = 1 };
            c.That(TestKit.Order(game, 0, direct).Accepted, "ordinary player enqueue uses SAME coordinator");
            c.Equal(1, Production(game).QueueCount, "ordinary command appends once");
            direct = new EnqueueTransformationCommand { Transformer = FactoryEntity(game).Id, Capability = Capability, Transformation = "dispatch.batch", Count = 2 };
            c.That(TestKit.Order(game, 0, direct).Accepted, "second normal enqueue preserves real existing prefix");
            c.Equal(3, Production(game).QueueCount, "same verified append path handles existing queue");
            foreach (string invalid in new[] { "missing-count", "zero-count", "missing-repeat", "repeat-count", "missing-home", "unknown-home", "missing-transformation", "wrong-capability" })
                c.Throws<ContentException>(() => New(Factory(root => {
                    JsonValue row = root.Get("transformation_dispatch_effects")[0];
                    switch (invalid) {
                        case "missing-count": row.Remove("count"); break;
                        case "zero-count": row.Set("count", JsonValue.Number("0")); break;
                        case "missing-repeat": row.Remove("repeat"); break;
                        case "repeat-count": row.Set("repeat", JsonValue.Of(true)); break;
                        case "missing-home": row.Remove("home"); break;
                        case "unknown-home": row.Set("home", JsonValue.Of("infer")); break;
                        case "missing-transformation": row.Set("transformation", JsonValue.Of("missing")); break;
                        case "wrong-capability": row.Set("capability", JsonValue.Of("missing")); break;
                    }
                })), "strict authored dispatch refuses " + invalid);
            c.Throws<ContentException>(() => New(Factory(work: root => {
                JsonValue nodes = root.Get("entity_activities")[0].Get("nodes");
                for (int i = 0; i < nodes.Count; i++) { nodes[i].Set("executor", JsonValue.Of("work.timed")); nodes[i].Remove("roles"); nodes[i].Remove("requirement"); }
            })), "timer-only graph cannot authorize physical manufacturing dispatch");
        }
    }
}
