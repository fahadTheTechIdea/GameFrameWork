using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Transient callback instrumentation only; metadata is authored through the shared unit reader.
    public sealed class ResourceSnapshotUnitDef : Def, IUnitContentDefinition
    {
        private UnitDeclaration _declaration;
        internal Action OnRead;
        public UnitId Unit { get { OnRead?.Invoke(); return _declaration.Unit; } }
        public string Dimension => _declaration.Dimension;
        public long ScaleNumerator => _declaration.ScaleNumerator;
        public long ScaleDenominator => _declaration.ScaleDenominator;
        public bool Discrete => _declaration.Discrete;
        protected override void ReadCore(JsonReader reader) => _declaration = UnitDeclaration.Read(Id, reader);
    }
    public enum ResourceSnapshotFault { Exception, PauseClock, AllocateIdentity }
    public sealed class ResourceSnapshotModule : GameModule
    {
        private World _world;
        private ResourceSnapshotUnitDef _quantity, _measure;
        public int Reads { get; private set; }
        public override string Id => "proof.resource-snapshots";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires
            => new[] { new ModuleRequirement(InventoryModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Section<ResourceSnapshotUnitDef>("snapshot_units");
        public override void Compose(WorldComposer composer)
        {
            _world = composer.World;
            _quantity = composer.Content.Get<ResourceSnapshotUnitDef>("unit.snapshot.quantity");
            _measure = composer.Content.Get<ResourceSnapshotUnitDef>("unit.snapshot.measure");
            composer.Services.Bind<IVisibility>(new SnapshotVisibility());
        }
        public void Arm(bool measure, ResourceSnapshotFault mode)
        {
            (measure ? _measure : _quantity).OnRead = () =>
            {
                Reads++;
                if (mode == ResourceSnapshotFault.Exception) throw new InvalidOperationException("proof.resource.metadata.failure");
                if (mode == ResourceSnapshotFault.PauseClock) ((GameClock)_world.Clock).Pause();
                if (mode == ResourceSnapshotFault.AllocateIdentity) _world.Spawn(_world.Content.Get<EntityDef>("source"), new PlayerId(0));
            };
        }
        private sealed class SnapshotVisibility : IVisibility
        {
            public bool CanSee(World world, PlayerId viewer, Entity entity) => entity.Owner == viewer;
            public bool CanSeeContent(World world, PlayerId viewer, IContentDefinition definition)
                => viewer.Value == 1 || definition.Id != "snapshot.hidden";
        }
    }
}
