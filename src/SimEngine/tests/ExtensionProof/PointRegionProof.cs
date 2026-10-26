using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>A whole interval, not two point samples that could miss unseen interior geometry.</summary>
    public sealed class PointIntervalSnapshot : SpatialSnapshot
    {
        public Fix Minimum { get; }
        public Fix Maximum { get; }
        public PointIntervalSnapshot(string space, Fix minimum, Fix maximum) : base(space)
        { if (minimum < Fix.Zero || maximum < minimum) throw new ArgumentException("ordered interval required"); Minimum = minimum; Maximum = maximum; }
        public override bool Matches(SpatialSnapshot other) => other is PointIntervalSnapshot interval
            && Space == interval.Space && Minimum == interval.Minimum && Maximum == interval.Maximum;
    }
    /// <summary>Disconnected stations intentionally declare no continuous travel containment capability.</summary>
    public sealed class PointStationsRegionDef : SpatialRegionDef
    {
        public override ISpatialRegionGeometry CreateGeometry(string id, JsonReader shape) => BuildGeometry<PointStationsRegionDef>(id, shape);
        private Fix _a, _b;
        protected override void ReadShape(JsonReader reader) { _a = reader.Fix("a"); _b = reader.Fix("b"); }
        protected override void ResolveShape(ContentResolver content)
        {
            if (SpaceDefinition is not PointSpaceDef points || _a < Fix.Zero || _b < Fix.Zero || _a > points.Extent || _b > points.Extent)
                throw content.Fail("stations need actual point-space coordinates");
        }
        protected override SpatialRegionStatus CheckShape(SpatialSnapshot presence)
            => presence is PointSnapshot point ? point.Position == _a || point.Position == _b ? SpatialRegionStatus.Inside : SpatialRegionStatus.Outside
                : SpatialRegionStatus.Unsupported;
    }
    /// <summary>An ordinary mod region; the work scheduler has no knowledge of point coordinates.</summary>
    public sealed class PointIntervalRegionDef : Def, ISpatialRegionDefinition, ISpatialTravelRegion, ISpatialObservationRegion
    {
        private SpatialRegionDeclaration _declaration;
        public string Space => _declaration.Space;
        public ISpatialSpaceDefinition SpaceDefinition => _declaration.SpaceDefinition;
        public string ShapeJson => _declaration.ShapeJson;
        public ISpatialRegionGeometry CreateGeometry(string id, JsonReader shape)
            => _declaration.CreateGeometry<PointIntervalRegionDef>(this, id, shape);
        public Fix Minimum { get; private set; }
        public Fix Maximum { get; private set; }
        // Transient negative callback injection only; no authored or saved region state.
        public Action<SpatialSnapshot> ContainmentProbe;
        public IReadOnlyList<SpatialSnapshot> CaptureObservationFootprints()
            => Array.AsReadOnly<SpatialSnapshot>(new SpatialSnapshot[] { new PointIntervalSnapshot(Space, Minimum, Maximum) });
        public SpatialRegionStatus CheckTravel(SpatialSnapshot from, SpatialSnapshot to)
        {
            if (from is not PointSnapshot a || to is not PointSnapshot b || a.Space != Space || b.Space != Space)
                return SpatialRegionStatus.Unsupported;
            // This region is a convex interval: its provider proves the entire continuous sweep.
            return a.Position >= Minimum && a.Position <= Maximum && b.Position >= Minimum && b.Position <= Maximum
                ? SpatialRegionStatus.Inside : SpatialRegionStatus.Outside;
        }
        protected override void ReadCore(JsonReader reader)
            => _declaration = SpatialRegionDeclaration.Read(reader, ReadShape);
        private void ReadShape(JsonReader reader)
        {
            Minimum = reader.Fix("minimum"); Maximum = reader.Fix("maximum");
            if (Minimum < Fix.Zero || Maximum < Minimum) throw reader.Error("interval needs an ordered nonnegative extent");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            _declaration.Resolve(content);
            if (!(SpaceDefinition is PointSpaceDef points) || Maximum > points.Extent)
                throw content.Fail("point interval requires compatible space and extent");
        }
        public SpatialRegionStatus Check(SpatialSnapshot presence)
        { ContainmentProbe?.Invoke(presence); return _declaration.Check(presence, CheckPoint); }
        private SpatialRegionStatus CheckPoint(SpatialSnapshot presence)
            => presence is PointSnapshot point ? point.Position >= Minimum && point.Position <= Maximum
                ? SpatialRegionStatus.Inside : SpatialRegionStatus.Outside
                : presence is PointIntervalSnapshot interval ? interval.Minimum >= Minimum && interval.Maximum <= Maximum
                    ? SpatialRegionStatus.Inside : SpatialRegionStatus.Outside : SpatialRegionStatus.Unsupported;
    }
}
