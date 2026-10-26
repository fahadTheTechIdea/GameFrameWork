using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Pure authored eligibility. Progress, interruption and discovery stay in AuthoredExploration.</summary>
    public interface IExplorationRequirementDefinition : IContentDefinition
    {
        void ValidateTarget(World world, ISpatialRegionDefinition region);
        Code CheckTarget(World world, Entity explorer, ISpatialSensor sensor, ISpatialRegionDefinition region);
    }

    public sealed class ContactExplorationRequirementDef : Def, IExplorationRequirementDefinition
    {
        protected override void ReadCore(JsonReader reader) { }
        public void ValidateTarget(World world, ISpatialRegionDefinition region) { }
        public Code CheckTarget(World world, Entity explorer, ISpatialSensor sensor, ISpatialRegionDefinition region)
        {
            var sample = EntityObservation.ReadSpatial(world, explorer);
            SpatialRegionStatus inside = EntityObservation.ReadProvider(world, region, explorer, () => region.Check(sample),
                space: world.Space, observeTargetState: true);
            if (inside == SpatialRegionStatus.Outside) return ExplorationCodes.Outside;
            if (inside == SpatialRegionStatus.Unsupported) return ExplorationCodes.UnsupportedRegion;
            if (inside != SpatialRegionStatus.Inside) throw new InvalidOperationException("invalid exploration region status");
            return EntityObservation.ReadProvider(world, sensor, explorer, () => sensor.CanObserve(world, sample),
                space: world.Space, observeTargetState: true) ? Code.None : ExplorationCodes.NoSensor;
        }
    }

    public sealed class SensorRegionExplorationRequirementDef : Def, IExplorationRequirementDefinition
    {
        protected override void ReadCore(JsonReader reader) { }
        private static IReadOnlyList<SpatialSnapshot> Footprints(World world, ISpatialRegionDefinition region, Entity explorer)
            => EntityObservation.ReadProvider(world, region, explorer, () => {
                if (region is not ISpatialObservationRegion observable)
                    throw new ContentException(region.Id + ": sensor exploration requires complete observation footprints");
                var shapes = observable.CaptureObservationFootprints();
                if (shapes == null || shapes.Count == 0) throw new ContentException(region.Id + ": observation footprints cannot be empty");
                var captured = new List<SpatialSnapshot>();
                foreach (var shape in shapes)
                {
                    if (shape == null || shape.Space != region.Space || !shape.Matches(shape)
                        || region.Check(shape) != SpatialRegionStatus.Inside)
                        throw new ContentException(region.Id + ": observation footprint must be a complete contained same-space shape");
                    captured.Add(shape);
                }
                return captured.AsReadOnly();
            }, space: world.Space, observeTargetState: true);
        public void ValidateTarget(World world, ISpatialRegionDefinition region) => Footprints(world, region, null);
        public Code CheckTarget(World world, Entity explorer, ISpatialSensor sensor, ISpatialRegionDefinition region)
        {
            var shapes = Footprints(world, region, explorer);
            foreach (var shape in shapes)
                if (!EntityObservation.ReadProvider(world, sensor, explorer, () => sensor.CanObserve(world, shape),
                    space: world.Space, observeTargetState: true)) return ExplorationCodes.NoSensor;
            return Code.None;
        }
    }
}
