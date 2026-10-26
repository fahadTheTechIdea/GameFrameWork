using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Engine-side content extension; no copied samples/knowledge/clock/work executor.
    public sealed class PointReadingDef : Def, ISpatialKnowledgeSubjectDefinition
    {
        private SpatialReadingDeclaration _declaration;
        public ISpatialLayerDefinition Layer => _declaration.Layer;
        public KnowledgeRetention Retention => _declaration.Retention;
        public GameDuration? FreshFor => _declaration.FreshFor;
        public Fix Position { get; private set; }
        // Transient negative diagnostic only; all positive state and readings stay with their original owners.
        public Action<World> ObservationProbe;
        public Action<SpatialSnapshot> CoverageProbe;
        public bool CoversSample(SpatialSnapshot sample)
        { CoverageProbe?.Invoke(sample); return CheckLocation(sample).IsNone; }
        protected override void ReadCore(JsonReader reader)
        { _declaration = SpatialReadingDeclaration.Read(reader); Position = reader.Fix("position"); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content)
        {
            if (!(Layer.Space is PointSpaceDef space) || Position < Fix.Zero || Position > space.Extent)
                throw content.Fail("point reading outside actual point space");
        }
        public void ValidateLayer(World world, ISpatialLayer layer)
        {
            if (!(world.Space is PointSpace point) || point.Id != Layer.Space.Id || !(layer is PointLayer)
                || layer.Id != Layer.Id || layer.Space != point.Id)
                throw new ContentException(Id + ": point reading requires its actual point space and readable point layer");
        }
        public Code CheckTarget(World world, Entity target) => _declaration.CheckTarget(world, target, CheckLocation);
        public JsonValue Observe(World world, Entity target)
        { ObservationProbe?.Invoke(world); return _declaration.Observe(world, target, CheckLocation, ReadValue); }
        public Code CheckObservation(World world, JsonValue data) => _declaration.CheckObservation(world, data);
        private Code CheckLocation(SpatialSnapshot target)
            => target is PointSnapshot point && point.Space == Layer.Space.Id && point.Position == Position ? Code.None : Codes.InvalidArgument;
        private Fix ReadValue(World world, ISpatialLayer layer) => ((PointLayer)layer).Read(Position);
    }
}
