using System;
using System.Collections.Generic;
using System.Globalization;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Engine-side content extension. The original controlled execution owns all mutable state/Sync.
    public sealed class PointSensorDef : ControlledExecutionDef<PointSensor>, ISpatialSensorDefinition
    {
        public string Space { get; private set; }
        protected override Code MultiplierUnit => new Code("point.metre");
        protected override bool HasTarget(EntityDef owner) => owner.HasCapability<ISpatialPresence>();
        protected override void ReadCore(JsonReader reader)
        { base.ReadCore(reader); Space = reader.String("space"); }
        protected override void ResolveCore(ContentResolver content)
        { base.ResolveCore(content); content.Def<PointSpaceDef>(Space); }
        protected override void ValidateCore(EntityDef owner)
        {
            base.ValidateCore(owner);
            if (owner.Component<ISpatialPresenceDefinition>()?.Space != Space)
                throw new ContentException(owner.Id + ": sensor requires actual point-space presence");
        }
    }

    public sealed partial class PointSensor : ControlledExecution<PointSensorDef>, ISpatialSensor
    {
        public SensorCoverage ReadCoverage(World world)
        {
            ProbeObservation(world, SensorObservationStage.Coverage);
            if (world == null || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity))
                throw new ArgumentException("point sensor requires its actual live session");
            var rate = ReadExecution(world);
            if (!rate.Gate.Blocker.IsNone || !rate.Multiplier.IsPositive) return null;
            if (!(Entity.Get<ISpatialPresence>().Capture() is PointSnapshot position) || position.Space != Def.Space)
                throw new InvalidOperationException("point sensor geometry disagrees with declared space");
            return new PointCoverage(position.Space, position.Position, rate.Multiplier);
        }
        public bool CanObserve(World world, SpatialSnapshot sample)
        {
            ProbeObservation(world, SensorObservationStage.Sight);
            return ReadCoverage(world)?.Covers(sample) == true;
        }
        private sealed class PointCoverage : SensorCoverage
        {
            private readonly Fix _origin, _radius;
            public PointCoverage(string space, Fix origin, Fix radius)
                : base(space, origin.Raw.ToString(CultureInfo.InvariantCulture) + ":" + radius.Raw.ToString(CultureInfo.InvariantCulture))
            { _origin = origin; _radius = radius; }
            public override bool Covers(SpatialSnapshot sample)
                => sample != null && sample.Space == Space && (sample is PointSnapshot point && Fix.Abs(point.Position - _origin) <= _radius
                    || sample is PointIntervalSnapshot interval && Fix.Abs(interval.Minimum - _origin) <= _radius
                        && Fix.Abs(interval.Maximum - _origin) <= _radius);
        }
    }

    public sealed class PointSensorModule : GameModule
    {
        public const string ModuleId = "proof.point-sensors";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {
            new ModuleRequirement("points", Version), new ModuleRequirement(OperationsModule.ModuleId, Version),
            new ModuleRequirement(KnowledgeVisibilityModule.ModuleId, Version),
            new ModuleRequirement(InventorySignalsModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        { types.Component<PointSensorDef>("point_sensor", 1); types.Section<PointReadingDef>("point_readings"); }
    }
}
