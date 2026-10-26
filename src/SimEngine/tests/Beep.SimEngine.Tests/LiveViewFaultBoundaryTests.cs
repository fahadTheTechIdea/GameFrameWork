using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class LiveViewFaultBoundaryTests
    {
        internal const string Case = "Live views: terminal faults refuse empty missing private observer and spatial shortcuts";
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        internal static void Register() => TestRunner.Add(Case, Run);

        private static void ListRefuses<T>(Check c, Action<IList<T>> query, string name)
        {
            var rows = new List<T> { default };
            c.Throws<InvalidOperationException>(() => query(rows), name + " refuses terminal live read");
            c.Equal(0, rows.Count, name + " clears stale destination on refusal");
        }

        private static void Refuses(Check c, PlayerView view, EntityId existing, EntityId foreign, ResourceId visible)
        {
            c.Throws<InvalidOperationException>(() => { _ = view.EntityCount; }, "entity count cannot report partial/empty success");
            c.Throws<InvalidOperationException>(() => { _ = view.Space; }, "space refuses even when no space selected");
            ListRefuses<EntityView>(c, view.CopyEntities, "all entities");
            ListRefuses<EntityView>(c, dst => view.CopyEntities(new PlayerId(99), dst), "empty owner filter");
            foreach (EntityId id in new[] { EntityId.None, existing, foreign })
            {
                EntityView result = new EntityView { Id = existing };
                c.Throws<InvalidOperationException>(() => view.TryGetEntity(id, out result), "missing/own/private entity cannot bypass fault");
                c.Equal(EntityId.None, result.Id, "entity out value cleared before refusal");
                ListRefuses<Facet>(c, dst => view.Inspect(id, dst), "missing/own/private inspect");
            }
            ListRefuses<CatalogueEntry>(c, view.CopyCatalogue, "catalogue");
            ListRefuses<ResourceInfo>(c, view.CopyResources, "resources");
            ListRefuses<UnitInfo>(c, view.CopyUnits, "units");
            ListRefuses<CapacityPolicyInfo>(c, view.CopyCapacityPolicies, "empty capacity policies");
            foreach (string id in new[] { "missing", "visible", "secret" })
            {
                ResourceId resource = visible;
                c.Throws<InvalidOperationException>(() => view.TryFindResource(id, out resource), "resource lookup refuses before missing/private shortcut");
                c.Equal(default(ResourceId), resource, "resource out value cleared before refusal");
            }
            c.Throws<InvalidOperationException>(() => view.ResourceName(visible), "resource name cannot return authored/private fallback after fault");
        }

        private static void SpatialRefuses(Check c, TileMapView map, TileRect area, SpatialRegionCursor retained)
        {
            c.Throws<InvalidOperationException>(() => { _ = map.Version; }, "live tile revision refuses");
            c.Throws<InvalidOperationException>(() => { _ = map.Layers; }, "empty layer catalogue refuses");
            c.Throws<InvalidOperationException>(() => { _ = map.LayerIds; }, "empty layer IDs refuse");
            c.Throws<InvalidOperationException>(() => map.ReadChanges(), "initial region query cannot mint a healthy cursor");
            c.Throws<InvalidOperationException>(() => map.ReadChanges(retained), "retained region cursor cannot advance after fault");
            foreach (TileRect region in new[] { area, new TileRect(0, 0, 0, 0), new TileRect(100, 100, 1, 1) })
            {
                ListRefuses<EntityView>(c, dst => map.CopyEntities(region, dst), "tile entity region");
                ListRefuses<int>(c, dst => map.ReadLayer("missing", region, dst), "integer layer");
                ListRefuses<Fix>(c, dst => map.ReadLayer("missing", region, dst), "fixed layer");
                ListRefuses<int>(c, dst => map.ReadLayerPage("missing", region, new SpatialLayerPageRequest(0, 1), dst), "integer page");
                ListRefuses<Fix>(c, dst => map.ReadLayerPage("missing", region, new SpatialLayerPageRequest(0, 1), dst), "fixed page");
            }
            foreach (TileCoord tile in new[] { new TileCoord(2, 2), new TileCoord(5, 2), new TileCoord(0, 0) })
            {
                EntityView value = new EntityView { Def = "stale" };
                c.Throws<InvalidOperationException>(() => map.TryGetOccupantAt(tile, out value), "own/private/empty occupancy refuses");
                c.That(value.Def == null && value.Id == EntityId.None, "occupancy output is cleared before refusal");
            }
            c.Throws<InvalidOperationException>(() => map.TryGetFootprint("missing", out _), "missing footprint refuses");
        }

        private static void Empty(Check c)
        {
            int calls = 0;
            var module = new ProbeModule("probe", new ModuleVersion(1, 0, 0), compose: composer =>
                composer.AddViewFeature<ProbeView>("probe.terminal-view", 1, (world, viewer) =>
                { calls++; throw new InvalidOperationException("proof.empty.view-failure"); }));
            var factory = CompositionProbe.Factory(module);
            var game = factory.NewGame(new NewGameRequest { Scenario = "probe", Seed = 1 });
            var independent = factory.NewGame(new NewGameRequest { Scenario = "probe", Seed = 1 });
            ulong isolated = independent.Hash();
            c.That(game.Seat(0).View.EntityCount == 0 && game.Observer.EntityCount == 0 && game.Observer.Space == null,
                "authored empty mapless composition is healthy before fault");
            var entities = new List<EntityView>(); game.Seat(0).View.CopyEntities(entities);
            c.That(entities.Count == 0 && !game.Seat(0).View.TryGetEntity(EntityId.None, out _), "healthy missing/empty results are still ordinary refusal facts");
            var twin = factory.Load(game.Save()); game.Update(); twin.Update();
            c.That(game.Hash() == twin.Hash() && game.Fault == null && twin.Fault == null, "mapless saved twin uses actual loop");
            var retained = game.Seat(0).Snapshot;
            c.Throws<InvalidOperationException>(() => game.Seat(0).View.Feature<ProbeView>(), "real declared factory failure latches terminal owner");
            var fault = game.Fault;
            Refuses(c, game.Seat(0).View, EntityId.None, EntityId.None, default);
            Refuses(c, game.Observer, EntityId.None, EntityId.None, default);
            c.That(game.Observer.Feature<OtherProbeView>() == null && game.Observer.FeatureRegistrations.Count == 1,
                "absent feature and captured registration metadata remain diagnostic, not live facts");
            c.Throws<InvalidOperationException>(() => game.Observer.Feature<ProbeView>(), "failed registered factory cannot retry");
            for (int i = 0; i < 2; i++) c.Equal(0, game.Update(), "faulted empty loop cannot advance");
            c.That(ReferenceEquals(fault, game.Fault) && calls == 1 && fault.Phase == "observation.provider",
                "empty shortcuts preserve original first fault without another factory call");
            c.That(game.Seat(0).Snapshot.State == GameState.Faulted && game.Seat(0).Snapshot.Tick == retained.Tick &&
                game.Seat(0).Snapshot.Entities.Count == 0 && retained.State != GameState.Faulted,
                "fault publication retains detached empty baseline and never mutates retained snapshot");
            c.Throws<InvalidOperationException>(() => game.Save(), "partial/faulted mapless state cannot save");
            c.Equal(isolated, independent.Hash(), "empty failure is isolated from untouched session");
        }

        private static void Populated(Check c)
        {
            int factoryCalls = 0; ViewObservationVisibility visibility = null;
            var module = new ViewObservationModule
            {
                ConfigureVisibility = value => visibility = value,
                OnFactory = (world, viewer) => { factoryCalls++; throw new InvalidOperationException("proof.populated.view-failure"); }
            };
            var factory = ViewObservationProof.Factory(module); var game = ViewObservationProof.New(factory);
            // Existing module instrumentation is session-local; capture before creating another session.
            var currentVisibility = visibility;
            var independent = ViewObservationProof.New(factory); ulong isolated = independent.Hash();
            var view = game.Seat(0).View; var observer = game.Observer;
            EntityId own = game.Seat(0).Snapshot.Entities.Single().Id;
            EntityId foreign = game.Seat(1).Snapshot.Entities.Single().Id;
            c.That(view.EntityCount == 1 && observer.EntityCount == 2 && !view.TryGetEntity(foreign, out _), "healthy privacy and observer facts use original projection");
            c.That(observer.TryFindResource("visible", out var resource) && view.ResourceName(resource) == "visible", "healthy content lookup uses authored resource");
            var map = view.Feature<TileMapView>(); var observerMap = observer.Feature<TileMapView>();
            var area = new TileRect(0, 0, 16, 16); var rows = new List<EntityView>();
            c.That(map.CopyEntities(area, rows).IsNone && rows.Count == 1 && !map.TryGetOccupantAt(new TileCoord(0, 0), out _), "healthy spatial and empty occupancy are unchanged");
            var cursor = map.ReadChanges().Cursor;
            c.That(cursor != null && map.LayerIds.Count == 0 && map.Layers.Count == 0, "healthy empty layers/first cursor are authored facts");
            using (var recorder = game.RecordReplay(Limits))
            {
                var twin = factory.Load(game.Save());
                for (int i = 0; i < 3; i++)
                { game.Update(); twin.Update(); c.That(game.Hash() == twin.Hash() && game.Fault == null, "healthy saved interleaved updates remain deterministic"); }
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game.Hash() == game.Hash(), "healthy live queries preserve current replay identity");
            }
            var first = game.Seat(0).Snapshot; var second = game.Seat(1).Snapshot; var all = game.Snapshot;
            c.Throws<InvalidOperationException>(() => view.Feature<ViewObservationReadModel>(), "actual selected factory fails without host gameplay");
            var fault = game.Fault; int reads = currentVisibility.EntityReads + currentVisibility.ContentReads;
            Refuses(c, view, own, foreign, resource); Refuses(c, observer, own, foreign, resource);
            SpatialRefuses(c, map, area, cursor); SpatialRefuses(c, observerMap, area, null);
            c.That(ReferenceEquals(map, view.Feature<TileMapView>()), "cached model handle may remain diagnostic but live methods refuse");
            c.That(view.CommandIds.Count > 0 && view.StateCodecs.Count > 0 && view.FeatureRegistrations.Count > 0,
                "captured identities remain available to explain failed composition");
            c.Throws<InvalidOperationException>(() => view.Feature<ViewObservationReadModel>(), "failed factory has no cached retry path");
            for (int i = 0; i < 2; i++)
            {
                c.Equal(0, game.Update(), "terminal game cannot earn more time");
                foreach (var pair in new[] { (first, game.Seat(0).Snapshot), (second, game.Seat(1).Snapshot), (all, game.Snapshot) })
                    c.That(pair.Item2.State == GameState.Faulted && pair.Item2.Tick == pair.Item1.Tick &&
                        pair.Item2.Entities.Select(e => e.Id).SequenceEqual(pair.Item1.Entities.Select(e => e.Id)) &&
                        pair.Item2.Entities.Select(e => e.Version).SequenceEqual(pair.Item1.Entities.Select(e => e.Version)) &&
                        pair.Item2.Spawned.Count == 0 && pair.Item2.Changed.Count == 0 && pair.Item2.Removed.Count == 0,
                        "each faulted snapshot retains only its last safe private facts with empty deltas");
            }
            c.That(ReferenceEquals(fault, game.Fault) && factoryCalls == 1 && currentVisibility.EntityReads + currentVisibility.ContentReads == reads,
                "all shortcut refusals preserve original first fault and invoke no provider again");
            c.That(first.Entities.Count == 1 && second.Entities.Count == 1 && all.Entities.Count == 2, "retained private snapshots never borrow observer data");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted populated state refuses save");
            c.Equal(isolated, independent.Hash(), "live-view failure leaves independent session untouched");
        }

        private static void Run(Check c) { Empty(c); Populated(c); }
    }
}
