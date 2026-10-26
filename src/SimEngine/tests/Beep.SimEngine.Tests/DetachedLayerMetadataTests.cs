using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    internal static class DetachedLayerMetadataTests
    {
        internal const string Case = "Spatial layer snapshots: detached contract facts and authoritative capture";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static SimGame New(GameFactory f, string scenario = "points") => f.NewGame(new NewGameRequest
        { Scenario = scenario, Seed = 5519, Seats = { Controller.Human, Controller.Human } });
        private static void Contracts(Check c, ISpatialLayerSnapshot snapshot)
        {
            c.That(snapshot is SpatialLayerSnapshot && !(snapshot is ISpatialLayer), "retained facts cannot act as runtime layer authority");
            c.That(snapshot.Unit is UnitInfo && !(snapshot.Unit is IContentDefinition), "layer unit contains scalar value copy, no live content provider");
            if (snapshot.VerticalUnit != null)
                c.That(snapshot.VerticalUnit is UnitInfo && !(snapshot.VerticalUnit is IContentDefinition), "optional vertical unit is detached, not original definition");
            if (snapshot.Resource != null)
            {
                var resource = snapshot.Resource;
                c.That(resource is ResourceInfo && !(resource is IContentDefinition), "resource is existing detached facts, not catalog/provider capability");
                c.That(resource.QuantityUnit is UnitInfo && !(resource.QuantityUnit is IContentDefinition), "nested quantity unit is detached");
                foreach (var measure in resource.CapacityMeasures)
                    c.That(measure.Unit is UnitInfo && !(measure.Unit is IContentDefinition), "each capacity measure retains only detached unit facts");
                foreach (string trait in resource.Traits) c.That(resource.HasTrait(trait), "one shared trait lookup recognizes copied facts");
                c.That(!resource.HasTrait("not.an.authored.trait"), "trait lookup does not fabricate a capability");
                c.Throws<NotSupportedException>(() => ((IList<string>)resource.Traits).Clear(), "resource traits remain detached read-only");
                c.Throws<NotSupportedException>(() => ((IList<ResourceCapacityMeasure>)resource.CapacityMeasures).Clear(), "one canonical measure collection is read-only");
            }
            c.Throws<InvalidOperationException>(() => snapshot.Metadata.Set("unauthorized", JsonValue.Of(true)), "nested JSON facts remain deeply frozen");
        }
        private static void Healthy(Check c, bool half)
        {
            var f = SpatialLayerAccessGuardTests.Factory(half); var g = New(f); var idle = New(f); ulong idleHash = idle.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            var target = Entity(g, "destination");
            PermanentRemovalTests.Submit(c, g, new PostOrderCommand { Plan = "inspect", Target = target, Priority = 30 });
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "physical activity saves before completion");
            bool completed = false;
            for (int i = 0; i < 1000; i++)
            {
                ulong before = g.Hash(); long time = g.Clock.StepIndex;
                foreach (var snapshot in g.Seat(0).View.Feature<PointLayerView>().Layers) Contracts(c, snapshot);
                c.Equal(before, g.Hash(), "authoritative metadata capture is pure"); c.Equal(time, g.Clock.StepIndex, "metadata capture does not advance time");
                if (g.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:inspect", target, out _)) { completed = true; break; }
                Step(g, twin);
            }
            if (!completed) throw new InvalidOperationException("detached metadata physical flow exceeds1000Updates clock=" + g.Clock.StepIndex);
            c.That(((PointSnapshot)g.Seat(0).Snapshot.Entities.Single(x => x.Id == Entity(g, "machine")).Spatial).Position > Fix.Zero,
                "real engine worker movement precedes activity completion");
            PermanentRemovalTests.Submit(c, g, new AssignPointLayerCommand { Layer = "point.deposit", Owner = new PlayerId(0) });
            PermanentRemovalTests.Submit(c, g, new WritePointLayerCommand { Layer = "point.deposit", Position = Fix.FromInt(3), Value = Fix.FromInt(9) });
            var layers = g.Seat(0).View.Feature<PointLayerView>().Layers;
            var held = layers.Single(x => x.Id == "point.deposit"); Contracts(c, held);
            c.Equal(new UnitId("unit.supply"), held.Unit.Unit, "actual authored layer unit identity copied");
            c.Equal("supply", held.Resource.Id, "actual authored resource identity copied");
            c.Equal(Fix.FromInt(-5), held.VerticalOffset, "actual subsurface offset is copied, not renderer geometry");
            var resources = new List<ResourceInfo>(); g.Seat(0).View.CopyResources(resources);
            var supply = resources.Single(x => x.Id == "supply");
            c.Equal(supply.Resource, held.Resource.Resource, "copy keeps same canonical compact resource identity");
            c.Equal(supply.QuantityUnit.Unit, held.Resource.QuantityUnit.Unit, "layer/catalog projections share same original authored facts");
            c.That(!g.Seat(1).View.Feature<PointLayerView>().Layers.Any(x => x.Id == "point.deposit"), "foreign company receives no private metadata projection");
            c.That(layers.All(x => x.Id != "point.water"), "hidden linked resource never appears through detached metadata");
            PermanentRemovalTests.Submit(c, g, new AssignPointLayerCommand { Layer = "point.deposit", Owner = new PlayerId(1) });
            c.Equal(new PlayerId(0), held.Owner, "retained projection keeps original owner facts after transfer");
            c.That(!g.Seat(0).View.Feature<PointLayerView>().Layers.Any(x => x.Id == held.Id), "original company cannot recapture transferred private layer");
            var transferred = g.Seat(1).View.Feature<PointLayerView>().Layers.Single(x => x.Id == held.Id); Contracts(c, transferred);
            c.Equal(new PlayerId(1), transferred.Owner, "new authorized projection comes from actual owner");
            c.Equal(held.Resource.Resource, transferred.Resource.Resource, "ownership change never creates another resource identity");
            var restored = f.Load(g.Save()); c.Equal(g.Hash(), restored.Hash(), "same original layer/visibility/clock owners restore");
            Contracts(c, restored.Seat(1).View.Feature<PointLayerView>().Layers.Single(x => x.Id == held.Id));
            for (int i = 0; i < 4; i++) Step(g, restored);
            c.Equal(idleHash, idle.Hash(), "metadata copies/ownership changes do not affect interleaved session");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "physical work and metadata ownership commands replay");
            c.Equal(4, g.Seat(0).View.FeatureRegistrations.Single(x => x.Id == "points.layers.field.points").SchemaVersion, "point read contract declares new metadata schema");
            c.Throws<NotSupportedException>(() => ((IList<SpatialLayerSnapshot>)layers).Clear(), "captured layer collection is detached read-only");
        }
        private static void DetachedResourceCopy(Check c)
        {
            // Standalone negative metadata-copy instrumentation, never a registered resource or game balance.
            var source = new ResourceSnapshotTests.MutableMetadata(); IResourceDefinition held = new ResourceInfo(source);
            source.SourceTraits.Clear(); source.SourceMeasures.Clear(); source.Unit.Dimension = "changed";
            source.Unit.ScaleNumerator = 9; source.Unit.Unit = new UnitId("unit.changed-source");
            c.That(held.HasTrait("original.trait") && !held.HasTrait("changed"), "shared trait lookup reads copied list, not source callback");
            c.That(held.QuantityUnit.Unit == new UnitId("unit.copy-proof") && held.QuantityUnit.Dimension == "mass" && held.QuantityUnit.ScaleNumerator == 1,
                "metadata interface never retains mutable source unit");
            c.That(held.CapacityMeasures.Count == 1 && held.CapacityMeasures[0].PerQuantity == Fix.FromInt(2) &&
                held.CapacityMeasures[0].Unit.Unit == new UnitId("unit.copy-proof"), "canonical measure shape deeply copies unit/collection facts");
            c.That(held.QuantityUnit is UnitInfo && held.CapacityMeasures[0].Unit is UnitInfo, "nested interfaces contain only existing scalar DTO values");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current detached metadata build identity");
            c.Equal(154, EngineInfo.SaveFormat, "no duplicate metadata state/codec");
            c.That(!typeof(ISpatialLayer).IsAssignableFrom(typeof(SpatialLayerSnapshot)), "snapshot no longer declares runtime authority contract");
            c.Equal(0, typeof(SpatialLayerSnapshot).GetConstructors().Length, "host cannot bypass authoritative capture using public constructor");
            c.That(typeof(SpatialLayerSnapshot).GetMethod("CanRead") == null, "retained facts expose no stale authorization method");
            c.That(typeof(UnitInfo).IsValueType && typeof(ResourceInfo).IsValueType, "existing value projections remain responsible for detached content");
            c.That(typeof(ResourceInfo).Assembly.GetType("Beep.SimEngine.Hosting.ResourceCapacityInfo") == null, "duplicate capacity projection removed, not shimmed");
            DetachedResourceCopy(c); Healthy(c, false); Healthy(c, true);
            var native = New(SpatialLayerTests.Factory(), "layers"); var map = native.Seat(0).View.Feature<TileMapView>();
            foreach (var snapshot in map.Layers) Contracts(c, snapshot);
            foreach (var snapshot in native.Observer.Feature<TileMapView>().Layers) Contracts(c, snapshot);
            c.Equal(7, native.Seat(0).View.FeatureRegistrations.Single(x => x.Id == "tiles.layers.tiles").SchemaVersion, "native read contract declares new metadata schema");
            c.That(!map.Layers.Any(x => x.Id == "deposit"), "native unassigned private resource metadata stays hidden");
            var values = new List<Fix>(); c.Equal(SpatialLayerReadStatus.Success, map.ReadLayer("elevation", new TileRect(0, 0, 1, 1), values), "native reads retain same shared authorization");
            c.Equal(Fix.Zero, values.Single(), "native sparse empty fact remains canonical zero");
        }
    }
}
