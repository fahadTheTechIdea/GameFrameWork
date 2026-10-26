using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    internal static class ResourceSnapshotTests
    {
        internal const string Case = "Resource snapshots: detached metadata and guarded atomic publication";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(ResourceSnapshotModule module)
            => ConstructionPlanTests.Points(additionalModule: module,
                moduleDeclaration: "{\"id\":\"proof.resource-snapshots\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"resource-snapshots.json\"]}",
                moduleDocument: FrameworkProofContent.Read("resource-snapshots.json"),
                scenarioEdit: scenario => scenario.Get("modules").Add(JsonValue.Of(module.Id)));
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        // Boundary-copy instrumentation only, not a registered catalog, resource store or gameplay provider.
        internal sealed class MutableUnit : IUnitDefinition
        {
            public UnitId Unit { get; set; } = new UnitId("unit.copy-proof");
            public string Dimension { get; set; } = "mass";
            public long ScaleNumerator { get; set; } = 1;
            public long ScaleDenominator { get; set; } = 1;
            public bool Discrete { get; set; }
        }
        internal sealed class MutableMetadata : IResourceDefinition
        {
            internal readonly MutableUnit Unit = new MutableUnit();
            internal readonly List<string> SourceTraits = new List<string> { "original.trait" };
            internal readonly List<ResourceCapacityMeasure> SourceMeasures;
            internal MutableMetadata() => SourceMeasures = new List<ResourceCapacityMeasure> { new ResourceCapacityMeasure(Unit, Fix.FromInt(2)) };
            public string Id => "copy-proof";
            public ResourceId Resource => new ResourceId(7);
            public string Category => "material";
            public IUnitDefinition QuantityUnit => Unit;
            public Code Phase => new Code("solid");
            public IReadOnlyList<string> Traits => SourceTraits;
            public IReadOnlyList<ResourceCapacityMeasure> CapacityMeasures => SourceMeasures;
            public bool HasTrait(string trait) => SourceTraits.Contains(trait);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "resource observation build identity");
            c.Equal(154, EngineInfo.SaveFormat, "no saved state or view schema change");
            // Exercise the engine's actual projection constructor; never author a gameplay balance.
            var metadata = new MutableMetadata(); var retained = new ResourceInfo(metadata);
            metadata.SourceTraits[0] = "changed.trait"; metadata.SourceTraits.Add("later.trait");
            metadata.SourceMeasures.Clear(); metadata.Unit.Unit = new UnitId("unit.changed-proof");
            metadata.Unit.Dimension = "volume"; metadata.Unit.ScaleNumerator = 3; metadata.Unit.Discrete = true;
            c.That(retained.Traits.Count == 1 && retained.Traits[0] == "original.trait", "resource snapshot does not retain mutable trait collection");
            c.That(retained.CapacityMeasures.Count == 1 && retained.CapacityMeasures[0].PerQuantity == Fix.FromInt(2), "resource snapshot does not retain mutable measure collection");
            c.That(retained.QuantityUnit.Unit == new UnitId("unit.copy-proof") && retained.QuantityUnit.Dimension == "mass"
                && retained.QuantityUnit.ScaleNumerator == 1 && !retained.QuantityUnit.Discrete, "quantity unit copied into detached scalar facts");
            c.That(retained.CapacityMeasures[0].Unit.Unit == new UnitId("unit.copy-proof") && retained.CapacityMeasures[0].Unit.Dimension == "mass", "measure unit also detached");
            c.Throws<NotSupportedException>(() => ((IList<string>)retained.Traits).Clear(), "copied traits cannot be modified");
            c.Throws<NotSupportedException>(() => ((IList<ResourceCapacityMeasure>)retained.CapacityMeasures).Clear(), "copied measures cannot be modified");
            Healthy(c); Faults(c);
        }
        private static void Healthy(Check c)
        {
            var module = new ResourceSnapshotModule(); var factory = Factory(module); var game = New(factory); var other = New(factory);
            ulong before = game.Hash(), untouched = other.Hash();
            var rows = new List<ResourceInfo>(); game.Seat(0).View.CopyResources(rows);
            c.That(rows.Any(r => r.Id == "supply") && rows.Any(r => r.Id == "snapshot.resource") && rows.All(r => r.Id != "snapshot.hidden"), "engine visibility filters resource metadata");
            var retained = rows.Single(r => r.Id == "snapshot.resource");
            c.That(retained.QuantityUnit.Unit == new UnitId("unit.snapshot.quantity")
                && retained.CapacityMeasures.Single().Unit.Unit == new UnitId("unit.snapshot.measure"), "nested authored unit contracts appear as facts, not providers");
            game.Seat(1).View.CopyResources(rows);
            c.That(rows.Any(r => r.Id == "snapshot.hidden"), "second viewer uses its own visibility decision");
            c.That(!game.Seat(0).View.TryFindResource("snapshot.hidden", out _) && game.Seat(1).View.TryFindResource("snapshot.hidden", out _), "resource lookup respects same viewer boundary");
            c.Equal(before, game.Hash(), "all healthy metadata reads pure");
            c.Equal(0L, game.Clock.StepIndex, "metadata earns no game time");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                var twin = factory.Load(game.Save());
                game.Clock.Pause(); twin.Clock.Pause(); game.Update(); twin.Update();
                c.That(game.Hash() == twin.Hash() && game.Clock.StepIndex == 0, "saved one-clock pause unchanged by resource guards");
                game.Clock.Resume(); twin.Clock.Resume();
                for (int i = 0; i < 12; i++)
                {
                    game.Update(); twin.Update(); game.Seat(0).View.CopyResources(rows);
                    c.That(game.Fault == null && twin.Fault == null && game.Hash() == twin.Hash(), "saved continuation matches after actual Update and guarded reads");
                }
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "clock/Update replay unchanged by metadata observation");
            }
            c.That(retained.Traits.SequenceEqual(new[] { "snapshot.trait" }) && retained.CapacityMeasures.Single().PerQuantity == Fix.FromInt(2), "retained authored facts stable after session updates");
            c.Equal(untouched, other.Hash(), "interleaved independent session unchanged");
        }
        private static void Faults(Check c)
        {
            foreach (bool measure in new[] { false, true })
                foreach (ResourceSnapshotFault mode in Enum.GetValues(typeof(ResourceSnapshotFault)))
                {
                    var module = new ResourceSnapshotModule(); var game = New(Factory(module));
                    var rows = new List<ResourceInfo>(); game.Seat(0).View.CopyResources(rows);
                    c.That(rows.Count > 1, "healthy metadata available before arming callback instrumentation");
                    module.Arm(measure, mode);
                    c.Throws<InvalidOperationException>(() => game.Seat(0).View.CopyResources(rows), "nested metadata callback mutation/exception refuses: " + measure + "/" + mode);
                    c.Equal(0, rows.Count, "late provider failure publishes no earlier or stale resource facts");
                    c.That(game.Fault != null && game.Fault.Phase == "observation.provider", "original observation owner latches first fault");
                    int reads = module.Reads; var fault = game.Fault;
                    c.That(reads == 1, "callback instrumentation fires once at actual nested metadata read");
                    c.Equal(0, game.Update(), "faulted metadata cannot continue game update");
                    c.Throws<InvalidOperationException>(() => game.Save(), "faulted observation cannot save partial state");
                    c.Throws<InvalidOperationException>(() => game.Hash(), "faulted observation has no healthy hash");
                    c.Throws<InvalidOperationException>(() => game.Seat(0).View.CopyResources(rows), "poisoned metadata cannot be retried");
                    c.That(module.Reads == reads && ReferenceEquals(fault, game.Fault), "fault identity retained and no callback rerun");
                }
        }
    }
}
