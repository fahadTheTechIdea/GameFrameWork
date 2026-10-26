using System;
using System.Globalization;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    public interface ISpatialSensorDefinition : INamedCapabilityDefinition
    {
        string Space { get; }
    }
    public interface ISpatialSensor : INamedEntityCapability
    {
        /// <summary>Pure current coverage; null explicitly means inactive. Never grants remembered knowledge.</summary>
        SensorCoverage ReadCoverage(World world);
        /// <summary>Pure current admission, including provider-owned geometry and obstruction rules.</summary>
        bool CanObserve(World world, SpatialSnapshot sample);
    }
    /// <summary>Detached provider-owned geometry and opaque scope identity, not saved visibility state.</summary>
    public abstract class SensorCoverage
    {
        protected SensorCoverage(string space, string key)
        {
            if (string.IsNullOrWhiteSpace(space) || string.IsNullOrEmpty(key)) throw new ArgumentException("sensor coverage needs explicit space/key");
            Space = space; Key = key;
        }
        public string Space { get; }
        public string Key { get; }
        /// <summary>Detached geometry only, never authority to observe a changed world.</summary>
        public abstract bool Covers(SpatialSnapshot sample);
    }
    /// <summary>Native square tile coverage; controls/gating/state remain in the existing execution owner.</summary>
    public sealed partial class TileSensorDef : ControlledExecutionDef<TileSensor>, ISpatialSensorDefinition
    {
        public string Space { get; private set; }
        protected override Code MultiplierUnit => new Code("unit.tile-distance");
        protected override bool HasTarget(EntityDef owner) => owner.HasCapability<ITilePresence>();
        protected override void ReadCore(JsonReader reader)
        {
            base.ReadCore(reader); Space = reader.String("space");
            ReadOcclusion(reader);
            var settings = ControlSettings.Initial;
            if (!Integral(settings.Minimum) || !Integral(settings.Maximum) || !Integral(settings.Target))
                throw reader.Error("tile sensor distance bands and target must be nonnegative integral tile distances");
        }
        private static bool Integral(Fix value) => value >= Fix.Zero && value == Fix.FromInt(value.ToIntFloor());
        protected override void ResolveCore(ContentResolver content) { base.ResolveCore(content); content.Def<ITileSpaceDefinition>(Space); ResolveOcclusion(content); }
        protected override void ValidateCore(EntityDef owner)
        {
            base.ValidateCore(owner);
            ValidateOcclusion(owner);
            if (owner.Component<ITilePresenceDefinition>()?.Space != Space)
                throw new ContentException(owner.Id + ": sensor must use its actual tile presence space");
        }
        internal Code CheckDistance(Fix value) => Integral(value) ? ControlSettings.Check(value) : OperationCodes.TargetBounds;
    }
    public sealed partial class TileSensor : ControlledExecution<TileSensorDef>, ISpatialSensor
    {
        public SensorCoverage ReadCoverage(World world)
        {
            if (world == null || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity)) throw new ArgumentException("sensor is not live in this world");
            var presence = Entity.Get<ITilePresence>();
            var execution = ReadExecution(world);
            if (!execution.Gate.Blocker.IsNone || !execution.Multiplier.IsPositive || !presence.Placed) return null;
            return new TileSensorCoverage(presence.Space, presence.Footprint, execution.Multiplier.ToIntFloor(), ReadOcclusionKey(world));
        }
        // The generic control owner validates bounds. Tile distance also requires its native quantum.
        public override Code CheckControl(bool enabled, Fix target) => Def.CheckDistance(target);
        public override void Sync(IStateSync sync)
        {
            base.Sync(sync);
            if (!Def.CheckDistance(ReadControl().Target).IsNone) throw new CorruptStateException("invalid saved sensor distance");
        }
    }
    public sealed class TileSensorCoverage : SensorCoverage
    {
        public TileRect Footprint { get; }
        public int Distance { get; }
        public TileSensorCoverage(string space, TileRect footprint, int distance, string obstructionKey)
            : base(space, string.Join(":", footprint.X.ToString(CultureInfo.InvariantCulture), footprint.Y.ToString(CultureInfo.InvariantCulture),
                footprint.W.ToString(CultureInfo.InvariantCulture), footprint.H.ToString(CultureInfo.InvariantCulture), distance.ToString(CultureInfo.InvariantCulture)) + ":" +
                (obstructionKey ?? throw new ArgumentNullException(nameof(obstructionKey))))
        {
            if (footprint.W <= 0 || footprint.H <= 0 || distance < 0) throw new ArgumentException("invalid tile coverage");
            Footprint = footprint; Distance = distance;
        }
        public override bool Covers(SpatialSnapshot sample)
        {
            if (!(sample is ITileFootprintSnapshot tile) || sample.Space != Space || tile.Footprint.W <= 0 || tile.Footprint.H <= 0) return false;
            return tile.Footprint.X >= (long)Footprint.X - Distance && tile.Footprint.Y >= (long)Footprint.Y - Distance &&
                (long)tile.Footprint.X + tile.Footprint.W <= (long)Footprint.X + Footprint.W + Distance &&
                (long)tile.Footprint.Y + tile.Footprint.H <= (long)Footprint.Y + Footprint.H + Distance;
        }
    }
}
