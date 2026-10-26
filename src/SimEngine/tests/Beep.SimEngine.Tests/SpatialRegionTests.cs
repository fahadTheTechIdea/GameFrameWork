using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;
using static Beep.SimEngine.Tests.PhysicalRecoveryTestContent;

namespace Beep.SimEngine.Tests
{
    public static class SpatialRegionTests
    {
        private static string Definitions => FrameworkProofContent.Read("work-regions.json");
        public static void Register()
        {
            TestRunner.Add("Work regions: authored shapes, provider contracts and pure private requirements", Admission);
            TestRunner.Add("Work regions: physical target guards, paused ownership and saved continuation", Physical);
        }
        private static WorkRequirementResult Result(Check c, SimGame game, Entity target, string plan)
        {
            var facts = new List<WorkRequirementView>();
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewOrderRequirements(plan, target.Id, facts), "public engine view evaluates the authored requirement");
            return facts[0].Result;
        }
        private static void Admission(Check c)
        {
            SimGame game = New(Factory(Definitions));
            Entity inside = Spawn(game, "area.target", 20, 20), border = Spawn(game, "area.target", 25, 24), outside = Spawn(game, "area.target", 28, 20);
            Entity foreign = Spawn(game, "area.target", 32, 24, 1); Tick(game);
            var rectangle = game.World.Content.Get<RectangularTileRegionDef>("area.field");
            var painted = game.World.Content.Get<PaintedTileRegionDef>("area.pad");
            c.That(ReferenceEquals(rectangle, game.World.Content.Get<ISpatialRegionDefinition>("area.field")), "base registry aliases the same immutable region");
            c.That(ReferenceEquals(rectangle, game.World.Content.Get<TargetRegionRequirementDef>("area.in-field").Region), "work uses the same contract provider, not a second selector");
            c.Equal(SpatialRegionStatus.Inside, rectangle.Check(inside.Get<ISpatialPresence>().Capture()), "full contained footprint is inside");
            c.Equal(SpatialRegionStatus.Outside, rectangle.Check(border.Get<ISpatialPresence>().Capture()), "origin inside does not admit a footprint across the boundary");
            c.Equal(SpatialRegionStatus.Outside, rectangle.Check(outside.Get<ISpatialPresence>().Capture()), "outside footprint refuses");
            c.Equal(SpatialRegionStatus.Inside, painted.Check(inside.Get<ISpatialPresence>().Capture()), "unordered painted tiles cover the entire footprint");
            c.Equal(new TileCoord(20, 20), painted.Tiles[0], "painted read view has canonical coordinate order");
            c.Throws<NotSupportedException>(() => ((IList<TileCoord>)painted.Tiles).Clear(), "painted intent cannot mutate through its view");
            c.Equal(SpatialRegionStatus.Unsupported, rectangle.Check(null), "missing geometry is not a fabricated position");
            c.Equal(SpatialRegionStatus.Unsupported, rectangle.Check(new PointSnapshot(rectangle.Space, Fix.Zero, Fix.Zero)), "incompatible coordinate representation refuses even with same space ID");
            c.Equal(SpatialRegionStatus.Unsupported, rectangle.Check(new PointSnapshot("foreign.space", Fix.Zero, Fix.Zero)), "foreign space is explicitly unsupported");
            Entity nonspatial = game.World.Spawn(game.World.Content.Get<EntityDef>("area.nonspatial"), new PlayerId(0));
            game.World.Settle(); Tick(game);
            ulong hash = game.Hash();
            c.Equal(WorkRequirementStatus.Invalid, Result(c, game, nonspatial, "area.paint").Status, "missing target presence is invalid, never an outside wait or default location");
            c.Equal(SpatialWorkCodes.UnsupportedRegion, Result(c, game, nonspatial, "area.paint").Blocker, "incompatible target has typed diagnostics");
            c.Equal(WorkRequirementStatus.Satisfied, Result(c, game, inside, "area.paint").Status, "painted intent works through the sole evaluator");
            c.Equal(SpatialWorkCodes.OutsideRegion, Result(c, game, outside, "area.paint").Blocker, "pure preview has typed outside blocker");
            c.Equal(WorkRequirementStatus.Satisfied, Result(c, game, inside, "area.timed").Status, "all/any composition shares region guards");
            var facts = new List<WorkRequirementView>();
            game.Seat(0).View.Feature<WorkView>().PreviewOrderRequirements("area.paint", inside.Id, facts);
            c.Equal(Codes.NotOwned, game.Seat(0).View.Feature<WorkView>().PreviewOrderRequirements("area.paint", foreign.Id, facts), "foreign geometry diagnostics are private");
            c.Equal(0, facts.Count, "refusal clears retained private facts");
            c.Equal(hash, game.Hash(), "shape checks and requirement previews change no stock, jobs or state");
            var retained = inside.Get<ISpatialPresence>().Capture();
            c.Equal(SpatialRegionStatus.Inside, rectangle.Check(retained), "region accepts detached geometry without a live execution handle");
            Refuse(c, "\"region\":\"area.field\"", "\"region\":\"missing\"", "unknown region references refuse");
            Refuse(c, "\"width\":8", "\"width\":0", "empty rectangle refuses");
            Refuse(c, "\"width\":8", "\"width\":2147483647", "wide boundary validation refuses without integer overflow");
            Refuse(c, "\"x\":18", "\"x\":-1", "negative origin refuses");
            Refuse(c, "{\"x\":21,\"y\":21}", "{\"x\":20,\"y\":20}", "duplicate painted tile refuses");
            Refuse(c, "{\"x\":21,\"y\":21}", "{\"x\":48,\"y\":21}", "painted out-of-space tile refuses");
            Refuse(c, "\"space\":\"recovery.tiles\",\"x\":18", "\"space\":\"missing\",\"x\":18", "unknown space references refuse");
            SimGame hole = New(Factory(Definitions.Replace(",{\"x\":21,\"y\":20}", "")));
            Entity pad = Spawn(hole, "area.target", 20, 20); Tick(hole);
            c.Equal(SpatialWorkCodes.OutsideRegion, Result(c, hole, pad, "area.paint").Blocker, "one missing painted footprint tile refuses");
            PointAdmission(c);
        }
        private static void Refuse(Check c, string oldValue, string newValue, string message)
            => c.Throws<ContentException>(() => New(Factory(Definitions.Replace(oldValue, newValue))), message);
        private static Entity Find(SimGame game, string id)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == id) return entity;
            throw new InvalidOperationException("missing region proof entity " + id);
        }
        private static GameFactory PointFactory(string json = null)
            => PointGame.Factory(layerDefinitions: json ?? FrameworkProofContent.Read("point-regions.json"));
        private static SimGame NewPoint(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 17 });
        private static void PointAdmission(Check c)
        {
            SimGame game = NewPoint(PointFactory()); Entity target = Find(game, "destination"), outside = Find(game, "source");
            c.That(!game.World.Services.Has<TileMap>(), "custom region needs no tile service");
            c.That(game.World.Content.Get<ISpatialRegionDefinition>("area.interval") is PointIntervalRegionDef, "external JSON definition aliases the same region family");
            ulong hash = game.Hash();
            c.Equal(WorkRequirementStatus.Satisfied, Result(c, game, target, "area.point-work").Status, "generic evaluator accepts an external non-tile region");
            c.Equal(SpatialWorkCodes.OutsideRegion, Result(c, game, outside, "area.point-work").Blocker, "external shape exposes same typed outside blocker");
            c.Equal(hash, game.Hash(), "point preview is pure");
            c.Throws<ContentException>(() => NewPoint(PointFactory(FrameworkProofContent.Read("point-regions.json").Replace("\"maximum\":12", "\"maximum\":101"))), "external shape validation runs before Initialize");
        }
        private static Order Post(Check c, SimGame game, Entity target, string plan)
        {
            CommandResult result = TestKit.Order(game, 0, new PostOrderCommand { Target = target.Id, Plan = plan });
            c.That(result.Accepted, "normal command queues intent even while a region guard is blocked");
            return game.World.Services.Get<WorkBoard>().Order(checked((int)result.Arg0));
        }
        private static void Reach(SimGame game, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 200 && !ready(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!ready())
            {
                var facts = new List<string>();
                foreach (Order order in game.World.Services.Get<WorkBoard>().Orders)
                    foreach (var node in order.Nodes) facts.Add(order.Id + ":" + node.Id + "/" + node.State + "/" + node.Progress + "/" + node.Blocker);
                throw new InvalidOperationException(stage + " timed out at " + game.Tick + ": " + string.Join("; ", facts));
            }
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(Definitions); SimGame game = New(factory);
            Entity target = Spawn(game, "area.mobile", 30, 20), machine = Spawn(game, "area.machine", 16, 20); Tick(game);
            Order order = Post(c, game, target, "area.work"); var node = order.Node("work");
            ResourceId steel = game.World.Content.ResourceId("steel");
            for (int i = 0; i < 5; i++) Tick(game);
            c.Equal(SpatialWorkCodes.OutsideRegion, node.Blocker, "execution uses preview's actual region refusal");
            c.Equal(WorkProgress.Zero, node.Progress, "blocked target advances no work");
            c.Equal(0, machine.Get<IWorker>().Job, "blocked target claims no worker");
            c.Equal(Fix.FromInt(10), target.Get<IInventory>().Amount(steel), "blocked target spends no startup material");
            target.Get<ITilePresence>().Move(game.World, FixVec2.Centre(new TileCoord(20, 20)));
            Tick(game);
            c.That(node.JobId != 0 && machine.Get<IWorker>().Job == node.JobId, "one job owner assigns after the actual target enters its region");
            c.Equal(WorkProgress.Zero, node.Progress, "assignment outside worker's starting location still requires physical travel");
            Reach(game, () => node.Progress.WholeTicks >= 1, "region work arrival");
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Amount(steel), "actual reached work consumes startup once");
            WorkProgress progress = node.Progress; int job = node.JobId;
            target.Get<ITilePresence>().Move(game.World, FixVec2.Centre(new TileCoord(30, 20))); Tick(game);
            c.Equal(NodeState.Paused, node.State, "leaving region pauses ongoing work");
            c.Equal(job, machine.Get<IWorker>().Job, "pause retains the actual exclusive worker relation");
            c.Equal(progress, node.Progress, "outside step commits no progress");
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "paused region work restores through existing owners without another area store");
            for (int i = 0; i < 3; i++) { Tick(game); Tick(twin); }
            c.Equal(progress, node.Progress, "paused region work never creeps forward");
            target.Get<ITilePresence>().Move(game.World, FixVec2.Centre(new TileCoord(24, 22)));
            twin.World.Entities.Get(target.Id).Get<ITilePresence>().Move(twin.World, FixVec2.Centre(new TileCoord(24, 22)));
            Reach(game, () => order.State == OrderState.Done, "region work resume", twin);
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Amount(steel), "region resume never recharges startup");
            c.Equal(game.Hash(), twin.Hash(), "saved region twins finish identically");
            c.Equal(0, machine.Get<IWorker>().Job, "completion releases the actual job");
            Timed(c); Cancel(c); PointPhysical(c);
        }
        private static void Cancel(Check c)
        {
            GameFactory factory = Factory(Definitions); SimGame game = New(factory);
            Entity target = Spawn(game, "area.mobile", 20, 20), machine = Spawn(game, "area.machine", 16, 20); Tick(game);
            Order order = Post(c, game, target, "area.work");
            Reach(game, () => order.Node("work").Progress.WholeTicks > 0, "region cancellation arrival");
            target.Get<ITilePresence>().Move(game.World, FixVec2.Centre(new TileCoord(30, 20))); Tick(game);
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "player cancels ordinary area-paused work");
            c.Equal(OrderState.Cancelled, order.State, "same order owner records cancellation");
            c.Equal(0, machine.Get<IWorker>().Job, "cancellation releases only the actual worker relation");
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "cancelled region work retains actual prior consumption");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled area work restores without phantom region state");
        }
        private static void Timed(Check c)
        {
            GameFactory factory = Factory(Definitions); SimGame game = New(factory);
            Entity target = Spawn(game, "area.mobile", 30, 20); Tick(game);
            Order order = Post(c, game, target, "area.timed");
            c.Equal(WorkProgress.Zero, order.Node("work").Progress, "timed node obeys same target guard");
            target.Get<ITilePresence>().Move(game.World, FixVec2.Centre(new TileCoord(20, 20))); Tick(game); Tick(game);
            c.That(order.Node("work").Progress.WholeTicks > 0, "timed node advances after target enters valid region");
            SimGame twin = factory.Load(game.Save()); Reach(game, () => order.State == OrderState.Done, "timed region completion", twin);
            c.Equal(game.Hash(), twin.Hash(), "timed region intent and branch restore identically");
        }
        private static void PointPhysical(Check c)
        {
            GameFactory factory = PointFactory(); SimGame game = NewPoint(factory);
            Entity target = Find(game, "destination"); Order order = Post(c, game, target, "area.point-work");
            Reach(game, () => order.Node("work").Progress.WholeTicks >= 1, "external region arrival");
            WorkProgress progress = order.Node("work").Progress;
            target.Get<PointPresence>().Move(game.World, Fix.FromInt(20)); Tick(game);
            c.Equal(SpatialWorkCodes.OutsideRegion, order.Node("work").Blocker, "external region stops physical work without scheduler edits");
            c.Equal(progress, order.Node("work").Progress, "external guard advances no blocked progress");
            SimGame twin = factory.Load(game.Save());
            target.Get<PointPresence>().Move(game.World, Fix.FromInt(10));
            twin.World.Entities.Get(target.Id).Get<PointPresence>().Move(twin.World, Fix.FromInt(10));
            Reach(game, () => order.State == OrderState.Done, "external region resume", twin);
            c.Equal(game.Hash(), twin.Hash(), "external provider and region saved work resume deterministically");
            c.That(!game.World.Services.Has<TileMap>(), "external physical completion remains tile independent");
        }
    }
}
