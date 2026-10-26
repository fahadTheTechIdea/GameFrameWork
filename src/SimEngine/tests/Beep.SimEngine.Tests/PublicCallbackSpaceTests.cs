using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Tests
{
    // Data authoring, ordinary public views/Update/save/replay only. Bad writes belong to the proof module.
    internal static class PublicCallbackSpaceTests
    {
        internal const string Case = "Public read callbacks: selected space integrity private output and physical continuation";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(ViewObservationModule module, bool half)
        {
            var root = Json.Parse(FrameworkProofContent.Read("view-observation.json"));
            if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            root.Set("spatial_layers", Json.Parse("[{\"id\":\"views.signal\",\"space\":\"views.tiles\",\"type\":\"proof.signal\",\"value_type\":\"fixed\",\"visibility\":\"public\",\"unit\":\"views.count\",\"metadata\":{}}]"));
            return ViewObservationProof.Factory(module, root.ToString());
        }
        private static void Healthy(Check c, bool half)
        {
            var module = new ViewObservationModule(); var f = Factory(module, half);
            var idle = ViewObservationProof.New(f); ulong idleHash = idle.Hash(); var g = ViewObservationProof.New(f);
            var own = g.Seat(0).Snapshot.Entities.Single(); var foreign = g.Seat(1).Snapshot.Entities.Single();
            var retained = g.Seat(0).Snapshot; var view = g.Seat(0).View; ulong hash = g.Hash();
            var model = view.Feature<ViewObservationReadModel>();
            c.That(ReferenceEquals(model, view.Feature<ViewObservationReadModel>()), "pure factory produces one cached viewer read model");
            c.Equal(1, view.EntityCount, "original visibility reveals only own entity");
            c.That(!view.TryGetEntity(foreign.Id, out var absent) && absent.Id == EntityId.None, "foreign lookup cannot disclose private facts");
            var rows = new List<EntityView>(); view.CopyEntities(rows);
            c.That(rows.Count == 1 && rows[0].Id == own.Id, "staged entity query uses same private policy");
            view.CopyEntities(new PlayerId(1), rows); c.Equal(0, rows.Count, "owner filtering does not bypass visibility");
            var facets = new List<Facet>(); c.That(view.Inspect(own.Id, facets), "inspect uses actual own entity");
            c.That(facets.Any(x => x.Key == ViewObservationPart.ValueFacet && x.Value == Fix.FromInt(2)), "authored initial fact is detached");
            c.That(!view.Inspect(foreign.Id, facets) && facets.Count == 0, "hidden inspection produces no rows");
            var map = view.Feature<TileMapView>(); var samples = new List<Fix>();
            c.Equal(SpatialLayerReadStatus.Success, map.ReadLayer("views.signal", new TileRect(0, 0, 1, 1), samples), "authored public scalar layer reads normally");
            c.Equal(Fix.Zero, samples.Single(), "observation does not create map values");
            c.Equal(hash, g.Hash(), "factory visibility and inspection preserve all saved state");
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            var twin = f.Load(g.Save());
            for (int i = 0; i < 5; i++) { g.Update(); twin.Update(); c.Equal(g.Hash(), twin.Hash(), "ordinary Update and saved continuation agree"); }
            c.That(model.Read(own.Id, out var value) && value == Fix.FromInt(7), "only actual game Update earns probe progress");
            c.That(retained.Entities.Single().Version == own.Version && facets.Count == 0, "retained private snapshot and caller rows are not rewritten");
            c.Equal(idleHash, idle.Hash(), "healthy projection leaves interleaved session unchanged");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "healthy projected loop replays");
        }
        private static void Negative(Check c, bool half, string query, string mode)
        {
            var module = new ViewObservationModule(); var f = Factory(module, half);
            var idle = ViewObservationProof.New(f); ulong idleHash = idle.Hash(); var g = ViewObservationProof.New(f);
            var view = query.StartsWith("observer-", StringComparison.Ordinal) ? g.Observer : g.Seat(0).View;
            var own = g.Seat(0).Snapshot; var foreign = g.Seat(1).Snapshot; var observer = g.Snapshot;
            var target = own.Entities.Single(); var hidden = foreign.Entities.Single();
            var map = view.Feature<TileMapView>(); // pure native factory, before arming the bad extension callback
            var entities = new List<EntityView> { target }; var facets = new List<Facet> { new Facet(ViewObservationPart.ValueFacet, Fix.One) };
            EntityView result = target; bool returned = false;
            string surface = query.Contains("factory") ? "factory" : query.Contains("inspect") ? "inspect" : "visibility";
            module.ArmSpatialRead(surface, mode);
            c.Throws<InvalidOperationException>(() =>
            {
                switch (query)
                {
                    case "entity-count": _ = view.EntityCount; break;
                    case "try-own": returned = view.TryGetEntity(target.Id, out result); break;
                    case "try-foreign": returned = view.TryGetEntity(hidden.Id, out result); break;
                    case "copy": view.CopyEntities(entities); break;
                    case "copy-owner": view.CopyEntities(new PlayerId(0), entities); break;
                    case "region": map.CopyEntities(new TileRect(0, 0, map.Width, map.Height), entities); break;
                    case "occupant": returned = map.TryGetOccupantAt(((TileSpatialSnapshot)target.Spatial).Footprint.Origin, out result); break;
                    case "inspect": case "observer-inspect": returned = view.Inspect(target.Id, facets); break;
                    case "factory": case "observer-factory": _ = view.Feature<ViewObservationReadModel>(); break;
                    default: throw new ArgumentException(nameof(query));
                }
            }, "selected-space mutation refuses before public result: " + query + "/" + mode);
            c.That(g.Fault != null && module.SpatialReadCalls == 1 && !returned, "actual bad callback faults once without successful result");
            if (query.StartsWith("copy", StringComparison.Ordinal) || query == "region") c.Equal(0, entities.Count, "caller receives no seeded or partial entity prefix");
            if (query.Contains("inspect")) c.Equal(0, facets.Count, "inspect publishes neither seeded nor staged facet");
            if (query.StartsWith("try-", StringComparison.Ordinal) || query == "occupant") c.Equal(EntityId.None, result.Id, "refused query resets output before callback");
            if (mode == "throw") c.That(g.Fault.Message.Contains("proof.public-read-original-error"), "original callback exception remains first fault");
            else c.That(g.Fault.Message.Contains("mutat"), "actual space codec or invalidation rejects mutation even after value restoration: " + g.Fault.Message);
            var first = g.Fault; long stopped = g.Clock.StepIndex;
            c.Equal(0, g.Update(), "normal Update publishes terminal retained facts without advancing time");
            c.That(ReferenceEquals(own.Space, g.Seat(0).Snapshot.Space) && ReferenceEquals(foreign.Space, g.Seat(1).Snapshot.Space) &&
                ReferenceEquals(observer.Space, g.Snapshot.Space), "fault preserves each authorized last healthy spatial payload");
            c.That(g.Seat(0).Snapshot.Entities.Select(x => x.Id).SequenceEqual(own.Entities.Select(x => x.Id)) &&
                g.Seat(1).Snapshot.Entities.Select(x => x.Id).SequenceEqual(foreign.Entities.Select(x => x.Id)), "fault exposes neither another viewer nor partial publication");
            c.Equal(0, g.Seat(0).Snapshot.Changed.Count + g.Seat(0).Snapshot.Spawned.Count + g.Seat(0).Snapshot.Removed.Count, "fault invents no deltas");
            c.Throws<InvalidOperationException>(() => view.TryGetEntity(EntityId.None, out _), "missing live query still refuses after first fault");
            c.Throws<InvalidOperationException>(() => view.Feature<ViewObservationReadModel>(), "failed or new factory cannot retry after terminal fault");
            g.Update(); c.That(ReferenceEquals(first, g.Fault) && stopped == g.Clock.StepIndex && module.SpatialReadCalls == 1, "first fault stops sole clock and all callback retries");
            c.Throws<InvalidOperationException>(() => g.Save(), "mutated read cannot become healthy save");
            c.Throws<InvalidOperationException>(() => g.Hash(), "mutated read cannot become healthy hash");
            c.Equal(idleHash, idle.Hash(), "bad callback changes no interleaved session");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual public callback guard build");
            foreach (bool half in new[] { false, true })
            {
                Healthy(c, half);
                foreach (string query in new[] { "entity-count", "try-own", "try-foreign", "copy", "copy-owner", "region", "occupant", "inspect", "observer-inspect", "factory", "observer-factory" })
                    foreach (string mode in new[] { "write", "restore", "throw" }) Negative(c, half, query, mode);
            }
            TilePresenceBindingTests.Physical(c, true);
        }
    }
}
