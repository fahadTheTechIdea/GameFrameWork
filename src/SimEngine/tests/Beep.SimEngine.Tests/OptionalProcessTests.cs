using System;
using System.Collections.Generic;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class OptionalProcessTests
    {
        public static void Register()
        {
            TestRunner.Add("Optional process: mapless timed inventory processing and saved sessions", Pure);
            TestRunner.Add("Optional process: explicit provider refusals and physical colony composition", Composition);
        }
        internal static GameFactory Factory(ProcessProofModule module, string json = null, string access = null)
        {
            var definition = new GameDefinition("pure-process", new GameModule[]
            {
                new InventoryModule(), new OperationsModule(), new ContentAccessModule(), new TransformationModule(), new WorldSetupModule(), module
            });
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("process-manifest.json"))
                .Add("framework/access.json", access ?? FrameworkProofContent.Access)
                .Add("process.json", json ?? FrameworkProofContent.Read("process.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "process", Seed = 13 });
        private static EntityId Entity(SimGame game, string definition)
        {
            var entities = new List<EntityView>(); game.Observer.CopyEntities(entities);
            foreach (EntityView entity in entities) if (entity.Def == definition) return entity.Id;
            throw new InvalidOperationException("missing " + definition);
        }
        private static int Count(SimGame game, string definition)
        {
            var entities = new List<EntityView>(); game.Observer.CopyEntities(entities);
            int count = 0; foreach (EntityView entity in entities) if (entity.Def == definition) count++; return count;
        }
        private static TransformationSnapshot State(SimGame game, EntityId transformer)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(transformer, new Code("transformation"), out TransformationSnapshot result))
                throw new InvalidOperationException("missing transformation facts");
            return result;
        }

        private static void Pure(Check c)
        {
            var module = new ProcessProofModule(); GameFactory factory = Factory(module); SimGame game = New(factory);
            c.Equal(1, module.Initializations, "the actual selected process session initializes normally");
            c.That(game.World.Space == null, "no spatial provider or invented geometry");
            c.That(!game.World.Services.Has<IEconomy>(), "zero-priced transformations require no economy policy");
            c.That(!game.World.Services.Has<NavService>() && !game.World.Services.Has<WorkBoard>(), "no nav or work coordinator");
            c.That(!game.World.Services.Has<LogisticsService>() && !game.World.Services.Has<IConstructionProjects>(), "no logistics/construction provider");
            c.That(!game.World.Services.Has<ITransformationSupplyRequests>(), "local supply does not silently fabricate a request provider");
            c.That(!game.World.Content.HasModule(WorkModule.ModuleId), "physical module is not even registered/selected");
            c.That(typeof(SimGame).Assembly.GetType("Beep.SimEngine.Standard.StandardModule") == null, "old bundle class is removed rather than wrapped");
            EntityId assembler = Entity(game, "assembler");
            IInventory stock = game.World.Entities.Get(assembler).Get<IInventory>();
            c.That(stock is SingleResourcePort && stock is IInletPort && stock is IOutletPort, "ordinary independently compiled inventory satisfies inlet/outlet contracts");
            c.Equal(Fix.FromInt(20), stock.Amount(game.World.Content.ResourceId("supply")), "initial supply is authored in the endpoint definition");
            var view = game.Seat(0).View.Feature<TransformationManagementView>();
            ulong before = game.Hash(); EntityOutputPreview preview = view.PreviewEntityOutput(assembler, new Code("transformation"), "build");
            c.That(preview.Refusal.IsNone && preview.Placement == null && preview.Output == "token", "non-spatial result preview has no fabricated coordinate");
            c.Equal(before, game.Hash(), "preview reads the same engine rules without mutating");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = assembler, Transformation = "build", Count = 2 }).Accepted, "player enqueues two ordinary batches");
            c.Equal(TransformationPhase.Working, State(game, assembler).Phase, "first batch starts through the normal update phase");
            c.Equal(1L, State(game, assembler).Progress.WholeTicks, "first tick applies actual work, not instant output");
            c.Equal(6L, State(game, assembler).Required, "authored duration resolves against scenario quantum");
            c.Equal(Fix.FromInt(17), stock.Amount(game.World.Content.ResourceId("supply")), "first inputs consumed once before working");
            c.Equal(0, Count(game, "token"), "no entity output before duration completes");
            SimGame saved = factory.Load(game.Save()); c.Equal(game.Hash(), saved.Hash(), "active process saves without optional coordinators");
            game.RunTicks(5); saved.RunTicks(2); saved.RunTicks(3);
            c.Equal(game.Hash(), saved.Hash(), "split update batches preserve saved progress");
            c.Equal(1, Count(game, "token"), "first entity releases exactly at its sixth tick");
            c.Equal(Fix.FromInt(18), stock.Amount(game.World.Content.ResourceId("supply")), "first resource result is published once with the entity");
            c.Equal(1, State(game, assembler).QueueCount, "second batch remains sequential");
            game.RunTicks(6); saved.RunTicks(6);
            c.Equal(2, Count(game, "token"), "second batch takes its own six ticks");
            c.Equal(Fix.FromInt(16), stock.Amount(game.World.Content.ResourceId("supply")), "two batches have exact input/output accounting");
            c.Equal(game.Hash(), saved.Hash(), "saved twins retain exact output identity/accounting");
            c.That(factory.Describe(game.Save()).Compatible, "module manifests and codecs identify saved process sessions");
            var queue = new List<TransformationQueueEntry>(); c.That(view.CopyQueue(assembler, new Code("transformation"), queue) && queue.Count == 0, "process view reports authoritative completed queue");
            c.That(game.Fault == null, "absent optional services are never accessed on the local zero-price path");

            string poor = FrameworkProofContent.Read("process.json").Replace("\"initial\": 20", "\"initial\": 2");
            GameFactory poorFactory = Factory(new ProcessProofModule(), poor); SimGame waiting = New(poorFactory);
            EntityId poorAssembler = Entity(waiting, "assembler");
            c.That(TestKit.Order(waiting, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = poorAssembler, Transformation = "build", Count = 1 }).Accepted, "local shortage can retain player intent");
            c.Equal(WorkSystem.BlockerMaterials, State(waiting, poorAssembler).Blocker, "local shortage is explicit, not instant invented delivery");
            waiting.RunTicks(20); c.Equal(0, Count(waiting, "token"), "starved local work does not finish");
            SimGame blockedSave = poorFactory.Load(waiting.Save());
            c.Equal(waiting.Hash(), blockedSave.Hash(), "local blocked queue survives save/load");
            c.That(TestKit.Order(waiting, 0, new CancelTransformationCommand { Capability = new Code("transformation"), Transformer = poorAssembler, Index = 0 }).Accepted, "ordinary cancellation needs no logistics service");
            c.Equal(0, State(waiting, poorAssembler).QueueCount, "cancellation removes only requested intent");
        }

        private static void Composition(Check c)
        {
            string basis = FrameworkProofContent.Read("process.json");
            foreach (string invalid in new[] { basis.Replace("\"automatic_supply\": false", "\"automatic_supply\": true"), basis.Replace("\"duration\": { \"numerator\": 1, \"denominator\": 10, \"unit\": \"hour\" }", "\"duration\": { \"numerator\": 1, \"denominator\": 10, \"unit\": \"hour\" }, \"cash\": 2") })
            {
                var module = new ProcessProofModule(); GameFactory factory = Factory(module, invalid);
                c.Throws<ContentException>(() => New(factory), "declared automatic supply/price requires an actual selected provider");
                c.Equal(0, module.Initializations, "missing selected services fail before Initialize");
            }
            var absent = new ProcessProofModule();
            c.Throws<ContentException>(() => New(Factory(absent, access: "{}")), "no authored access policy is not silently treated as open");
            c.Equal(0, absent.Initializations, "missing policy refuses before initialization");
            c.Throws<ContentException>(() => New(Factory(new ProcessProofModule(), access: "{\"open_content_access\":[{\"id\":\"one\"},{\"id\":\"two\"}]}")), "two policy owners do not compete by registration order");
            c.Throws<ContentException>(() => New(Factory(new ProcessProofModule(), basis.Replace("\"automatic_supply\": false, ", ""))), "old transformer schema without authored supply selection fails");

            string restricted = basis.Replace("\"entity_setups\":", "\"process_access\": [{\"id\":\"restriction\",\"deny\":\"build\"}], \"entity_setups\":");
            SimGame denied = New(Factory(new ProcessProofModule(), restricted, "{}")); EntityId transformer = Entity(denied, "assembler");
            Code refusal = new Code("proof.content-denied");
            c.Equal(refusal, denied.Seat(0).Preview(new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = transformer, Transformation = "build", Count = 1 }), "new JSON-authored contract policy controls queue admission without construction");
            c.Equal(refusal, denied.Seat(0).View.Feature<TransformationManagementView>().PreviewEntityOutput(transformer, new Code("transformation"), "build").Refusal, "result preview shares the same policy");
            c.Equal(refusal, TestKit.Order(denied, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = transformer, Transformation = "build", Count = 1 }).Refusal, "command result agrees with preview");

            GameFactory colony = Colony.Factory(); SimGame physical = colony.NewGame(new NewGameRequest { Scenario = "colony", Seed = 7 });
            EntityId factoryEntity = Colony.Mine(physical, 0, "factory")[0].Id;
            Entity crane = physical.World.Entities.Get(Colony.Mine(physical, 0, "crane")[0].Id);
            Entity depot = physical.World.Entities.Get(Colony.Mine(physical, 0, "depot")[0].Id);
            NavGrid grid = physical.World.Services.Get<NavService>().Grid("land");
            ITilePresence cranePresence = crane.Get<ITilePresence>();
            TileCoord occupied = depot.Get<ITilePresence>().Tile;
            c.That(grid.CostFor(crane, cranePresence.Tile) > 0, "routing permits the mover's own occupied tile");
            c.Equal(0, grid.CostFor(crane, occupied), "routing refuses another entity's blocking footprint");
            c.Equal(PathSearchResult.Unreachable, grid.CanReach(cranePresence.Tile, new TileRect(occupied, 1, 1)), "reachability does not promise entry into occupied destinations");
            ulong routingHash = physical.Hash(); var path = new List<TileCoord>();
            c.Equal(PathSearchResult.Found, grid.FindPath(cranePresence.Tile, new TileRect(new TileCoord(12, 5), 1, 1), path), "reachable routes remain available around physical occupants");
            bool clear = true; foreach (TileCoord tile in path) clear &= grid.CostFor(crane, tile) > 0;
            c.That(clear, "every planned entry respects current occupancy");
            c.Equal(routingHash, physical.Hash(), "occupancy-aware route queries do not mutate session state");
            c.That(physical.World.Services.Get<ITransformationSupplyRequests>() == physical.World.Services.Get<LogisticsService>(), "request interface aliases the sole existing demand coordinator");
            c.That(TestKit.Order(physical, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = factoryEntity, Transformation = "build_truck", Count = 1 }).Accepted, "ordinary physical manufacturing command still admits");
            CommandResult build = TestKit.Order(physical, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(physical.World.Space.Id, new TileCoord(10, 16)), Priority = 50 });
            c.That(build.Accepted, "ordinary construction uses independent access policy");
            physical.RunTicks(31);
            if (physical.Fault != null) throw new InvalidOperationException("physical checkpoint fault: " + physical.Fault.Detail);
            SimGame twin = colony.Load(physical.Save());
            c.Equal(physical.Hash(), twin.Hash(), "physical work/transformation and shared request provider save once");
            bool moved = false; int ticks = 0;
            for (; ticks < 4320 && (Colony.Mine(physical, 0, "tower").Count == 0 || Colony.Mine(physical, 0, "truck").Count < 3); ticks++)
            {
                physical.RunTicks(1); twin.RunTicks(1);
                foreach (EntityView worker in Colony.Mine(physical, 0, "crane")) moved |= worker.State == physical.World.Content.Get<IEntityStateDefinition>("moving").State;
            }
            c.That(ticks < 4320, "bounded physical acceptance finishes; tick=" + physical.Tick + ", fault=" + physical.Fault);
            c.That(moved, "construction machine moves over game time, not an instant fallback");
            c.Equal(1, Colony.Mine(physical, 0, "tower").Count, "materials and work build the structure");
            c.Equal(3, Colony.Mine(physical, 0, "truck").Count, "automatic demand hauls transformation inputs and timed manufacturing releases a machine");
            c.Equal(physical.Hash(), twin.Hash(), "both physical workflows continue identically after restore");
            c.That(physical.Fault == null, "separate registrations retain normal physical execution");
        }
    }
}
