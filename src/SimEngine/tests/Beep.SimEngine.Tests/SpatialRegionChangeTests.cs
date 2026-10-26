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
    internal static partial class SpatialRegionChangeTests
    {
        internal const string Case = "Regional change contracts: scoped native and mod cursors with physical saved work";
        internal static void Register()
        { TestRunner.Add(Case, Run); TestRunner.Add(GeometryCase, GeometryRun); TestRunner.Add(GeometryProtocolCase, GeometryProtocol);
            TestRunner.Add(OwnershipCase, OwnershipProtocol); TestRunner.Add(OwnershipWorkflowCase, OwnershipRun); }
        private static EntityView Mine(SimGame game, string prototype)
            => game.Seat(0).Snapshot.Entities.First(e => e.Def == prototype && e.Owner == new PlayerId(0));
        private static CommandResult Submit(Check c, SimGame game, Command command)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary command admission");
            c.Equal(before, game.Hash(), "ordinary preview is pure");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary player command accepted");
            if (!result.Accepted) throw new InvalidOperationException("player refused: " + result.Refusal);
            return result;
        }
        private static bool Productive(SimGame game, int order)
        {
            var nodes = new List<ActivityNodeView>();
            return game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes) && nodes.Any(n => n.ProductiveSteps > 0 && !n.Progress.Equals(WorkProgress.Zero));
        }
        private static bool Done(SimGame game, int order)
        {
            return game.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:field.discover", Mine(game, "source").Id,
                out var completion) && completion.Order == order;
        }
        private static Fix Stock(SimGame game, EntityId entity, string resource)
        {
            var facts = new List<Facet>();
            if (!game.Seat(0).View.Inspect(entity, facts)) throw new InvalidOperationException("missing public stock facts");
            return facts.Where(f => f.Key == Storage.FacetStock && f.Subject == resource).Aggregate(Fix.Zero, (n, f) => n + f.Value);
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update();
            if (game.Fault != null) throw new InvalidOperationException("region change Update fault " + game.Fault);
            if (twin == null) return;
            twin.Update();
            if (twin.Fault != null || game.Hash() != twin.Hash())
                throw new InvalidOperationException("region change saved twins diverged at " + game.Clock.StepIndex + "/" + twin.Fault);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !done(); i++) Update(game, twin);
            if (!done())
            {
                var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
                throw new InvalidOperationException(stage + "/time=" + game.Clock.StepIndex + "/entities=" +
                    string.Join(";", game.Seat(0).Snapshot.Entities.Select(e => e.Id + "/" + e.Def + "/" + e.State)) +
                    "/orders=" + string.Join(";", orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
            }
        }
        private static void Refused<TRegion>(Check c, SpatialRegionChanges<TRegion> result, string label)
            => c.That(result.Status == SpatialRegionChangeStatus.InvalidCursor && result.Cursor == null &&
                !result.FullInvalidation && result.ChangedRegions.Count == 0, label + " exposes no baseline or regional facts");
        private static void Unchanged<TRegion>(Check c, SpatialRegionChanges<TRegion> result, SpatialRegionCursor before, string label)
            => c.That(result.Status == SpatialRegionChangeStatus.Success && result.Cursor.Version == before.Version &&
                !result.FullInvalidation && result.ChangedRegions.Count == 0, label);
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current regional change contract identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "transient cursors add no state/save owner");
            var scope = new SpatialChangeScope(); var baseline = scope.Capture(3);
            c.That(scope.TryBaseline(null, 3, out int initial) && initial == -1, "initial cursor is explicit absence, not guessed version");
            c.That(scope.TryBaseline(baseline, 3, out int before) && before == 3, "scope validates its exact captured baseline");
            c.That(!scope.TryBaseline(new SpatialChangeScope().Capture(3), 3, out _) &&
                !scope.TryBaseline(scope.Capture(4), 3, out _), "foreign and future versions refuse even with equal numeric counters");
            c.Throws<ArgumentOutOfRangeException>(() => scope.Capture(-1), "negative provider cursor refuses");
            c.Throws<ArgumentOutOfRangeException>(() => scope.TryBaseline(null, -1, out _), "invalid current provider facts refuse");
            var region = new TileRect(1, 2, 3, 4); var input = new List<TileRect> { region };
            var retained = SpatialRegionChanges<TileRect>.Incremental(baseline, input); input.Clear();
            c.That(retained.ChangedRegions.Count == 1 && retained.ChangedRegions[0].Equals(region), "result defensively copies provider list");
            c.Throws<NotSupportedException>(() => ((IList<TileRect>)retained.ChangedRegions).Clear(), "retained regional list is readonly");
            c.Throws<ArgumentNullException>(() => SpatialRegionChanges<TileRect>.Full(null), "successful result requires actual cursor");
            c.That(!typeof(IPersistentService).IsAssignableFrom(typeof(SpatialChangeScope)), "scope identity is no persistent counter/store");
            NativeProtocol(c); NativePhysical(c); PointPhysical(c);
        }
        private static void NativeProtocol(Check c)
        {
            var factory = RegionalDiscoveryTests.Tiles();
            var game = factory.NewGame(new NewGameRequest { Scenario = "layers", Seed = 883 });
            var mine = game.Seat(0).View.Feature<TileMapView>(); var other = game.Seat(1).View.Feature<TileMapView>();
            ISpatialRegionChangeQuery<TileRect> query = mine; var map = (TileMap)game.World.Space;
            var first = new TileCoord(69996, 69996); var gap = new TileCoord(69997, 69996);
            var initial = query.ReadChanges(); var baseline = initial.Cursor; var foreign = other.ReadChanges().Cursor;
            c.That(initial.Status == SpatialRegionChangeStatus.Success && initial.FullInvalidation && initial.ChangedRegions.Count == 0,
                "wide native first read signals full without enumerating world");
            c.That(mine.Width == 70000 && mine.Height == 70000, "actual authored space not platform-capped");

            // Explicit native protocol writes/knowledge instrumentation. Not normal-player discovery grants.
            map.AssignOwner(game.World, "observer.private", new PlayerId(0));
            map.Layer<IntLayer>("observer.public").Set(first, 11); map.Layer<IntLayer>("observer.public").Set(gap, 33);
            Unchanged(c, query.ReadChanges(baseline), baseline, "hidden native sample writes reveal no change/timing/location");
            Unchanged(c, other.ReadChanges(foreign), foreign, "other viewer also learns no hidden native activity");
            var fact = game.World.Content.Get<KnowledgeFactDef>("field.observed");
            c.Equal(Code.None, game.World.Services.Get<ICompanyKnowledge>().Record(new PlayerId(0), "sim.knowledge.facts", fact.Id, fact.Value),
                "native knowledge protocol records authored regional fact");
            Update(game);
            var revealed = query.ReadChanges(baseline);
            c.That(revealed.FullInvalidation && revealed.ChangedRegions.Count == 0 && revealed.Cursor.Version > baseline.Version,
                "new authorized scope requests reread, not hidden region enumeration");
            baseline = revealed.Cursor;
            map.Layer<IntLayer>("observer.public").Set(gap, 44);
            Unchanged(c, query.ReadChanges(baseline), baseline, "unknown cell in SAME native chunk leaks no activity");
            map.Layer<IntLayer>("observer.public").Set(first, 12);
            var changed = query.ReadChanges(baseline);
            c.That(changed.Status == SpatialRegionChangeStatus.Success && !changed.FullInvalidation &&
                changed.ChangedRegions.Count == 1 && changed.ChangedRegions[0].Contains(first),
                "existing native exact visible write produces one actual clipped region");
            var values = new List<int>();
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("observer.public", new TileRect(gap, 1, 1), values),
                "regional invalidation does not authorize hidden samples within coarse chunk");
            Unchanged(c, other.ReadChanges(foreign), foreign, "private knowledge and known writes do not invalidate foreign viewer");
            Refused(c, query.ReadChanges(foreign), "same-session other-viewer cursor");
            Refused(c, query.ReadChanges(new SpatialChangeScope().Capture(changed.Cursor.Version)), "fabricated matching integer cursor");
            ulong hash = game.Hash(); query.ReadChanges(changed.Cursor); _ = mine.Layers;
            c.Equal(hash, game.Hash(), "region/cursor queries are pure and absent from authoritative hash");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "native values/knowledge keep original saved owners");
            var loaded = twin.Seat(0).View.Feature<TileMapView>();
            Refused(c, loaded.ReadChanges(changed.Cursor), "old cursor after load");
            c.That(loaded.ReadChanges().FullInvalidation && loaded.ReadChanges().ChangedRegions.Count == 0, "loaded native view explicitly starts new full stream");
            Refused(c, query.ReadChanges(loaded.ReadChanges().Cursor), "loaded cursor in original session");
            var fresh = factory.NewGame(new NewGameRequest { Scenario = "layers", Seed = 883 });
            Refused(c, fresh.Seat(0).View.Feature<TileMapView>().ReadChanges(baseline), "new interleaved native session cursor");
            c.That(changed.ChangedRegions.Count == 1 && revealed.ChangedRegions.Count == 0, "retained native regional facts never mutate after later queries/load");
            Update(game, twin); c.Equal(game.Hash(), twin.Hash(), "native protocol continuation retains deterministic authoritative state");
        }
        private static void NativePhysical(Check c)
        {
            var factory = Colony.Factory(); var game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 887 });
            ISpatialRegionChangeQuery<TileRect> query = game.Seat(0).View.Feature<TileMapView>();
            var baseline = query.ReadChanges().Cursor;
            var built = Submit(c, game, new ConstructCommand { Def = "tower", Priority = 50,
                Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)) });
            c.That(game.Seat(0).Snapshot.Entities.Any(e => e.Id == built.Entity && e.Def == "site:tower") &&
                !game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "native construction starts ordinary timed site");
            bool moving = false, loaded = false;
            Reach(game, () => {
                moving |= game.Seat(0).Snapshot.Entities.Any(e => e.State == new Code("state.moving"));
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                loaded |= jobs.Any(j => j.IsHaul && j.Loaded > Fix.Zero);
                return loaded && moving;
            }, "native loaded delivery");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "native cargo travel saves through original owners");
            Refused(c, twin.Seat(0).View.Feature<TileMapView>().ReadChanges(baseline), "moving saved native query baseline");
            long step = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Update(game, twin);
            c.Equal(step, game.Clock.StepIndex, "sole pause stops actual travel/hauling");
            Unchanged(c, query.ReadChanges(baseline), baseline, "entity travel/work does not invent spatial-layer writes");
            game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, () => game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "native physical build completion", twin);
            c.That(game.Clock.StepIndex > 1 && moving && loaded, "real native travel and delivery precede finished structure");
            Fix remaining = Fix.Zero;
            foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0))) remaining += Stock(game, entity.Id, "steel");
            c.Equal(Fix.FromInt(20), remaining, "native50 opening minus30 constructed resources, no duplicated cargo");
            Unchanged(c, query.ReadChanges(baseline), baseline, "construction does not create fake map-layer delta");
        }
        private static void PointPhysical(Check c,Action<Check,GameFactory,SimGame> after=null)
        {
            var factory = RegionalDiscoveryTests.Points(); var game = ConstructionPlanTests.NewPoints(factory);
            var idle = ConstructionPlanTests.NewPoints(factory); ulong idleHash = idle.Hash();
            var mine = game.Seat(0).View.Feature<PointLayerView>(); var other = game.Seat(1).View.Feature<PointLayerView>();
            ISpatialRegionChangeQuery<PointRegion> query = mine;
            var initial = query.ReadChanges(); var baseline = initial.Cursor; var foreign = other.ReadChanges().Cursor;
            c.That(initial.FullInvalidation && initial.ChangedRegions.Count == 0 && mine.Layers.Count == 0,
                "mod first read has no fabricated precise-region history or unknown layer metadata");
            Refused(c, query.ReadChanges(foreign), "mod other-viewer cursor");
            var machine = Mine(game, "machine").Id; var source = Mine(game, "source").Id;
            Submit(c, game, new ConfigureWorkPolicyCommand { Worker = machine, Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
            var posted = Submit(c, game, new PostOrderCommand { Plan = "field.discover", Target = source, Priority = 60 });
            int order = checked((int)posted.Arg0);
            Reach(game, () => Productive(game, order), "mod productive partial discovery");
            c.That(mine.Layers.Count == 0 && game.Clock.StepIndex > 1, "partial positive work does not prematurely disclose regional knowledge");
            Unchanged(c, query.ReadChanges(baseline), baseline, "partial work grants no region invalidation");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "physical learning saves original work/knowledge/resource state");
            Refused(c, twin.Seat(0).View.Feature<PointLayerView>().ReadChanges(baseline), "mod pre-load work cursor");
            Reach(game, () => Done(game, order), "mod physical discovery completion", twin);
            var learned = query.ReadChanges(baseline);
            c.That(learned.Status == SpatialRegionChangeStatus.Success && learned.FullInvalidation && learned.ChangedRegions.Count == 0 &&
                learned.Cursor.Version > baseline.Version && mine.Layers.Count == 1, "mod original visibility version publishes explicit full after real completion");
            c.Equal(learned.Cursor.Version,game.Seat(0).View.Space.Version,"non-tile world snapshot uses its actual existing viewer revision, never constant zero");
            c.Equal(Fix.FromInt(12), Stock(game, source, "supply"), "first actual activity consumes exactly8 supplies");
            Unchanged(c, other.ReadChanges(foreign), foreign, "player-earned knowledge remains company-private");
            Submit(c, game, new AssignPointLayerCommand { Layer = "point.deposit", Owner = new PlayerId(0) });
            c.That(mine.Layers.Count == 2, "normal engine ownership command reveals known private layer only to owner");
            baseline = query.ReadChanges().Cursor;
            Submit(c, game, new WritePointLayerCommand { Layer = "point.deposit", Position = Fix.One, Value = Fix.FromInt(7) });
            c.That(query.ReadChanges(baseline).FullInvalidation, "mod actual authorized private sample write invalidates owner through same contract");
            Unchanged(c, other.ReadChanges(foreign), foreign, "mod private sample write cannot invalidate another viewer");
            baseline = query.ReadChanges().Cursor;
            Submit(c, game, new WritePointLayerCommand { Layer = "point.terrain", Position = Fix.One, Value = Fix.FromRatio(5, 2) });
            c.That(query.ReadChanges(baseline).FullInvalidation, "known public sample invalidates only authorized mod viewer");
            var values = new List<Fix>();
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("point.terrain", new PointRegion(Fix.One, Fix.One, 1), values),
                "provider-owned non-tile values remain readable through original contract");
            c.Equal(Fix.FromRatio(5, 2), values[0], "exact authored non-tile quantity, no raster/platform conversion");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(new WritePointLayerCommand { Layer = "point.terrain", Position = Fix.FromInt(3), Value = Fix.One }),
                "invalidations never authorize unknown mod gap");
            var own = query.ReadChanges().Cursor; ulong hash = game.Hash(); query.ReadChanges(own); _ = mine.Layers;
            c.Equal(hash, game.Hash(), "mod cursor/result queries remain pure");
            Unchanged(c, query.ReadChanges(own), own, "unchanged mod data yields incremental empty result, not perpetual full");
            Refused(c, mine.ReadChanges(new PointLayerView((PointSpace)game.World.Space, new PlayerId(0)).ReadChanges().Cursor),
                "another query instance cannot substitute its matching numeric baseline");
            var saved = factory.Load(game.Save());
            Refused(c, saved.Seat(0).View.Feature<PointLayerView>().ReadChanges(own), "completed mod save rejects old stream");
            c.Equal(game.Hash(), saved.Hash(), "completed mod save retains no cursor cache in authoritative state");
            c.Equal(idleHash, idle.Hash(), "shared authored mod content retains independent interleaved session");
            var native = Colony.New(889).Seat(0).View.Feature<TileMapView>();
            Refused(c, query.ReadChanges(native.ReadChanges().Cursor), "other provider cursor cannot cross query/geometry contract");
            c.That(initial.ChangedRegions.Count == 0 && learned.FullInvalidation, "retained mod results stay detached after writes/load");
            after?.Invoke(c,factory,game);
        }
    }
}
