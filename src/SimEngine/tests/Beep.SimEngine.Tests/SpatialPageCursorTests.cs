using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class SpatialPageCursorTests
    {
        internal const string Case = "Spatial page cursors: bound native and mod reads with physical saved discovery";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static EntityView Mine(SimGame game, string prototype)
            => game.Seat(0).Snapshot.Entities.First(e => e.Def == prototype && e.Owner == new PlayerId(0));
        private static Fix Stock(SimGame game, EntityId id, string resource)
        {
            var facts = new List<Facet>();
            if (!game.Seat(0).View.Inspect(id, facts)) throw new InvalidOperationException("missing observed inventory");
            return facts.Where(f => f.Key == Storage.FacetStock && f.Subject == resource).Aggregate(Fix.Zero, (a, f) => a + f.Value);
        }
        private static CommandResult Submit(Check c, SimGame game, Command command)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary player preview");
            c.Equal(before, game.Hash(), "preview does not mutate engine");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary player command admitted");
            if (!result.Accepted) throw new InvalidOperationException("command refused: " + result.Refusal);
            return result;
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update();
            if (game.Fault != null) throw new InvalidOperationException("page Update fault: " + game.Fault);
            if (twin == null) return;
            twin.Update();
            if (twin.Fault != null || game.Hash() != twin.Hash())
                throw new InvalidOperationException("saved page twins diverged at " + game.Clock.StepIndex + "/" + twin.Fault);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !done(); i++) Update(game, twin);
            if (done()) return;
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            throw new InvalidOperationException(stage + "/time=" + game.Clock.StepIndex + "/entities=" +
                string.Join(";", game.Seat(0).Snapshot.Entities.Select(e => e.Id + "/" + e.Def + "/" + e.State)) +
                "/orders=" + string.Join(";", orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static void Refused(Check c, SpatialLayerPageResult page, int count, SpatialLayerReadStatus status, string label)
            => c.That(page.Status == status && count == 0 && page.Cursor == null && page.Count == 0 &&
                page.TotalCount == 0 && page.Offset == 0 && page.NextOffset == 0 && !page.HasMore, label + " returns no cursor/count/values");
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "exact page protocol identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "query binding adds no persistent authority");
            var scope = new SpatialChangeScope(); const string key = "layer:region";
            var request = new SpatialLayerPageRequest(long.MaxValue - 2, int.MaxValue, scope.Capture(7, key));
            var page = SpatialLayerPaging.Check(long.MaxValue, 7, scope, key, request);
            c.That(page.Count == 2 && page.NextOffset == long.MaxValue && !page.HasMore && page.Cursor.Version == 7,
                "pure shared envelope supports long counts without overflow/platform cap");
            Refused(c, SpatialLayerPaging.Check(long.MaxValue, 8, scope, key, request), 0, SpatialLayerReadStatus.Invalidated, "owned stale version");
            Refused(c, SpatialLayerPaging.Check(long.MaxValue, 6, scope, key, request), 0, SpatialLayerReadStatus.Invalidated, "owned future version");
            Refused(c, SpatialLayerPaging.Check(long.MaxValue, 7, scope, "other", request), 0, SpatialLayerReadStatus.InvalidCursor, "other binding");
            Refused(c, SpatialLayerPaging.Check(long.MaxValue, 7, new SpatialChangeScope(), key, request), 0, SpatialLayerReadStatus.InvalidCursor, "equal foreign version");
            c.That(!scope.TryBaseline(page.Cursor, 7, out _), "page cannot substitute regional-change baseline");
            Refused(c, SpatialLayerPaging.Check(1, 7, scope, key, new SpatialLayerPageRequest(0, 1, scope.Capture(7))), 0,
                SpatialLayerReadStatus.InvalidCursor, "regional-change cursor cannot substitute page baseline");
            c.Throws<ArgumentNullException>(() => SpatialLayerPaging.Check(1, 7, null, key, new SpatialLayerPageRequest(0, 1)), "null scope is provider error");
            c.Throws<ArgumentNullException>(() => SpatialLayerPaging.Check(1, 7, scope, null, new SpatialLayerPageRequest(0, 1)), "null binding is provider error");
            c.Throws<ArgumentOutOfRangeException>(() => SpatialLayerPaging.Check(-1, 7, scope, key, new SpatialLayerPageRequest(0, 1)), "negative count is provider error");
            c.Throws<ArgumentOutOfRangeException>(() => SpatialLayerPaging.Check(1, -1, scope, key, new SpatialLayerPageRequest(0, 1)), "negative version is provider error");
            Refused(c, SpatialLayerPaging.Check(1, 7, scope, key, default), 0, SpatialLayerReadStatus.InvalidPage, "invalid envelope");
            NativeProtocol(c); NativePhysical(c); PointPhysical(c);
        }
        private static void NativeProtocol(Check c)
        {
            var factory = RegionalDiscoveryTests.Tiles(); var game = factory.NewGame(new NewGameRequest { Scenario = "layers", Seed = 891 });
            var mine = game.Seat(0).View.Feature<TileMapView>(); var other = game.Seat(1).View.Feature<TileMapView>();
            var first = new TileCoord(69996, 69996); var exact = new TileRect(first, 1, 1); var whole = new TileRect(0, 0, 70000, 70000);
            var ints = new List<int> { 99 }; var fixedValues = new List<Fix>();
            Refused(c, mine.ReadLayerPage("observer.public", exact, new SpatialLayerPageRequest(0, 1), ints), ints.Count,
                SpatialLayerReadStatus.Unavailable, "unknown actual native sample");
            // Explicit native protocol setup, not a normal-player knowledge grant or gameplay driver.
            var map = (TileMap)game.World.Space; map.AssignOwner(game.World, "observer.private", new PlayerId(0));
            map.Layer<IntLayer>("observer.public").Set(first, 17); map.Layer<FixedLayer>("observer.private").Set(first, Fix.FromInt(9));
            var fact = game.World.Content.Get<KnowledgeFactDef>("field.observed");
            foreach (int seat in new[] { 0, 1 })
                c.Equal(Code.None, game.World.Services.Get<ICompanyKnowledge>().Record(new PlayerId(seat), "sim.knowledge.facts", fact.Id, fact.Value), "native authorization protocol setup");
            Update(game);
            long offset = (long)first.Y * 70000 + first.X;
            var retained = mine.ReadLayerPage("observer.public", whole, new SpatialLayerPageRequest(offset, 1), ints);
            c.That(retained.Status == SpatialLayerReadStatus.Success && retained.TotalCount == 4900000000L && ints.Single() == 17,
                "actual huge native region reads one authorized sample without world-size restriction");
            ulong hash = game.Hash();
            var repeated = mine.ReadLayerPage("observer.public", whole, new SpatialLayerPageRequest(offset, 2, retained.Cursor), ints);
            Refused(c, repeated, ints.Count, SpatialLayerReadStatus.Unavailable, "known page plus unknown gap gives no partial values");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayerPage("observer.public", whole, new SpatialLayerPageRequest(offset, 1, retained.Cursor), ints).Status,
                "authorized continuation keeps exact region binding");
            Refused(c, mine.ReadLayerPage("observer.public", exact, new SpatialLayerPageRequest(0, 1, retained.Cursor), ints), ints.Count,
                SpatialLayerReadStatus.InvalidCursor, "same sample different rectangle");
            Refused(c, mine.ReadLayerPage("observer.private", whole, new SpatialLayerPageRequest(offset, 1, retained.Cursor), fixedValues), fixedValues.Count,
                SpatialLayerReadStatus.InvalidCursor, "same geometry different actual layer/value type");
            Refused(c, other.ReadLayerPage("observer.public", whole, new SpatialLayerPageRequest(offset, 1, retained.Cursor), ints), ints.Count,
                SpatialLayerReadStatus.InvalidCursor, "authorized other viewer cannot reuse native cursor");
            Refused(c, mine.ReadLayerPage("observer.public", whole, new SpatialLayerPageRequest(offset, 1, mine.ReadChanges().Cursor), ints), ints.Count,
                SpatialLayerReadStatus.InvalidCursor, "native regional baseline is not page baseline");
            c.Equal(SpatialRegionChangeStatus.InvalidCursor, mine.ReadChanges(retained.Cursor).Status, "native page baseline is not regional baseline");
            Refused(c, mine.ReadLayerPage("missing", exact, new SpatialLayerPageRequest(0, 1, retained.Cursor), ints), ints.Count,
                SpatialLayerReadStatus.Unavailable, "missing layer remains indistinguishable from hidden layer");
            c.Equal(hash, game.Hash(), "all native accepted/refused queries are pure");
            map.Layer<IntLayer>("observer.public").Set(new TileCoord(first.X + 1, first.Y), 88);
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayerPage("observer.public", whole, new SpatialLayerPageRequest(offset, 1, retained.Cursor), ints).Status,
                "unknown same-chunk write never leaks through retained page");
            map.Layer<IntLayer>("observer.public").Set(first, 18);
            Refused(c, mine.ReadLayerPage("observer.public", whole, new SpatialLayerPageRequest(offset, 1, retained.Cursor), ints), ints.Count,
                SpatialLayerReadStatus.Invalidated, "owned actual visible native write explicitly invalidates");
            retained = mine.ReadLayerPage("observer.public", whole, new SpatialLayerPageRequest(offset, 1), ints);
            int retainedVersion = retained.Cursor.Version;
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "native cursor/binding not saved or hashed");
            var loaded = twin.Seat(0).View.Feature<TileMapView>();
            Refused(c, loaded.ReadLayerPage("observer.public", whole, new SpatialLayerPageRequest(offset, 1, retained.Cursor), ints), ints.Count,
                SpatialLayerReadStatus.InvalidCursor, "saved native view rejects old cursor even if counters match");
            c.Equal(SpatialLayerReadStatus.Success, loaded.ReadLayerPage("observer.public", whole, new SpatialLayerPageRequest(offset, 1), ints).Status,
                "explicit fresh native read after restore");
            c.That(ints.Single() == 18 && retained.Cursor.Version == retainedVersion && retained.TotalCount == 4900000000L,
                "retained detached page facts remain unchanged after mutation/load");
            Update(game, twin);
        }
        private static void NativePhysical(Check c)
        {
            var factory = Colony.Factory(); var game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 893 });
            var built = Submit(c, game, new ConstructCommand { Def = "tower", Priority = 50,
                Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)) });
            c.That(game.Seat(0).Snapshot.Entities.Any(e => e.Id == built.Entity && e.Def == "site:tower"), "engine creates timed construction site, not finished building");
            bool moved = false, loaded = false;
            Reach(game, () => {
                moved |= game.Seat(0).Snapshot.Entities.Any(e => e.State == new Code("state.moving"));
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                loaded |= jobs.Any(j => j.IsHaul && j.Loaded > Fix.Zero); return moved && loaded;
            }, "native physical loaded haul");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "native loaded cargo saves original physical owners");
            long before = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Update(game, twin);
            c.Equal(before, game.Clock.StepIndex, "one clock pauses loaded physical travel");
            game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, () => game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "native delivered construction", twin);
            c.That(moved && loaded && game.Clock.StepIndex > before, "actual travel/delivery/time precedes completed construction");
            Fix remaining = Fix.Zero;
            foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0))) remaining += Stock(game, entity.Id, "steel");
            c.Equal(Fix.FromInt(20), remaining, "physical construction consumes exactly30 of50 steel with no cargo duplication");
        }
        private static void PointPhysical(Check c)
        {
            var factory = RegionalDiscoveryTests.Points(); var game = ConstructionPlanTests.NewPoints(factory);
            var idle = ConstructionPlanTests.NewPoints(factory); ulong idleHash = idle.Hash();
            var mine = game.Seat(0).View.Feature<PointLayerView>(); var other = game.Seat(1).View.Feature<PointLayerView>();
            var region = new PointRegion(Fix.Zero, Fix.One, 3); var values = new List<Fix>();
            Refused(c, mine.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1), values), values.Count,
                SpatialLayerReadStatus.Unavailable, "mod knowledge not yet earned");
            var machine = Mine(game, "machine").Id; var source = Mine(game, "source").Id;
            Submit(c, game, new ConfigureWorkPolicyCommand { Worker = machine, Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
            int order = checked((int)Submit(c, game, new PostOrderCommand { Plan = "field.discover", Target = source, Priority = 60 }).Arg0);
            Reach(game, () => {
                var nodes = new List<ActivityNodeView>();
                return game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes) && nodes.Any(n => n.ProductiveSteps > 0 && !n.Progress.Equals(WorkProgress.Zero));
            }, "mod partial productive discovery");
            Refused(c, mine.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1), values), values.Count,
                SpatialLayerReadStatus.Unavailable, "partial real work cannot reveal samples");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "mod saves partial physical discovery");
            Reach(game, () => game.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:field.discover", source, out var completion) && completion.Order == order,
                "mod completed real discovery", twin);
            c.Equal(Fix.FromInt(12), Stock(game, source, "supply"), "player-earned mod visibility costs8 actual supplies");
            Submit(c, game, new AssignPointLayerCommand { Layer = "point.deposit", Owner = new PlayerId(0) });
            var first = mine.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1), values);
            c.That(first.Status == SpatialLayerReadStatus.Success && first.Count == 1 && first.TotalCount == 3 && first.HasMore, "public mod provider implements same bound page contract");
            ulong hash = game.Hash();
            var second = mine.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(first.NextOffset, 2, first.Cursor), values);
            c.That(second.Status == SpatialLayerReadStatus.Success && second.Count == 2 && second.NextOffset == 3 && !second.HasMore,
                "mod can change capacity while continuing the SAME exact region");
            Refused(c, mine.ReadLayerPage("point.terrain", new PointRegion(Fix.One, Fix.One, 2), new SpatialLayerPageRequest(0, 1, first.Cursor), values), values.Count,
                SpatialLayerReadStatus.InvalidCursor, "mod changed start/count");
            Refused(c, mine.ReadLayerPage("point.terrain", new PointRegion(Fix.Zero, Fix.FromRatio(1, 2), 3), new SpatialLayerPageRequest(0, 1, first.Cursor), values), values.Count,
                SpatialLayerReadStatus.InvalidCursor, "mod changed exact fractional spacing");
            Refused(c, mine.ReadLayerPage("point.deposit", region, new SpatialLayerPageRequest(0, 1, first.Cursor), values), values.Count,
                SpatialLayerReadStatus.InvalidCursor, "mod different authorized layer");
            Refused(c, other.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1, first.Cursor), values), values.Count,
                SpatialLayerReadStatus.Unavailable, "foreign company never learns private earned knowledge or binding");
            Refused(c, mine.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1, mine.ReadChanges().Cursor), values), values.Count,
                SpatialLayerReadStatus.InvalidCursor, "mod change stream cannot replace page stream");
            c.Equal(SpatialRegionChangeStatus.InvalidCursor, mine.ReadChanges(first.Cursor).Status, "mod page cannot replace change stream");
            c.Equal(hash, game.Hash(), "all mod page reads/refusals leave sole state unchanged");
            Submit(c, game, new WritePointLayerCommand { Layer = "point.terrain", Position = Fix.One, Value = Fix.FromRatio(5, 2) });
            Refused(c, mine.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(1, 1, first.Cursor), values), values.Count,
                SpatialLayerReadStatus.Invalidated, "mod owned visible write explicitly invalidates");
            first = mine.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1), values);
            var saved = factory.Load(game.Save()); c.Equal(game.Hash(), saved.Hash(), "mod cursor state/binding omitted from saved authority");
            var restored = saved.Seat(0).View.Feature<PointLayerView>();
            Refused(c, restored.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(1, 1, first.Cursor), values), values.Count,
                SpatialLayerReadStatus.InvalidCursor, "restored mod query rejects pre-save page identity");
            var fresh = restored.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(1, 1), values);
            c.That(fresh.Status == SpatialLayerReadStatus.Success && values.Single() == Fix.FromRatio(5, 2), "fresh mod page returns saved exact quantity");
            Refused(c, mine.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(1, 1, fresh.Cursor), values), values.Count,
                SpatialLayerReadStatus.InvalidCursor, "restored cursor cannot enter original session");
            var independent = new PointLayerView((PointSpace)game.World.Space, new PlayerId(0));
            Refused(c, independent.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(1, 1, first.Cursor), values), values.Count,
                SpatialLayerReadStatus.InvalidCursor, "same provider/viewer but another actual query identity");
            var native = Colony.New(895).Seat(0).View.Feature<TileMapView>().ReadChanges().Cursor;
            Refused(c, mine.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(1, 1, native), values), values.Count,
                SpatialLayerReadStatus.InvalidCursor, "foreign geometry/provider cannot substitute cursor");
            c.Equal(idleHash, idle.Hash(), "interleaved session remains untouched by physical work and cursor reads");
            Update(game, saved);
        }
    }
}
