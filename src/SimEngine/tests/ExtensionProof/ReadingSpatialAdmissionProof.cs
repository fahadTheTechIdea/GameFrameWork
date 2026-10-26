using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Unused negative contract admission only. No live sample, knowledge, work or clock owner.
    public sealed class ReadingAdmissionDef : Def, ISpatialKnowledgeSubjectDefinition
    {
        private SpatialReadingDeclaration _declaration;
        private string _mode;
        public ISpatialLayerDefinition Layer => _declaration.Layer;
        public KnowledgeRetention Retention => _declaration.Retention;
        public GameDuration? FreshFor => _declaration.FreshFor;
        protected override void ReadCore(JsonReader reader)
        { _declaration = SpatialReadingDeclaration.Read(reader); _mode = reader.String("mode"); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateLayer(World world, ISpatialLayer layer)
        {
            if (layer == null || layer.Id != Layer.Id || layer.Space != Layer.Space.Id)
                throw new ContentException("negative reading received foreign layer");
            if (_mode == "clock" || _mode == "clock-throw") ((GameClock)world.Clock).Speed = 2;
            if (_mode == "throw" || _mode == "clock-throw") throw new InvalidOperationException("proof.reading-admission-first-error");
        }
        public bool CoversSample(SpatialSnapshot sample) => throw new InvalidOperationException("unused admission reading must not cover");
        public Code CheckTarget(World world, Entity target) => throw new InvalidOperationException("unused admission reading must not check");
        public JsonValue Observe(World world, Entity target) => throw new InvalidOperationException("unused admission reading must not observe");
        public Code CheckObservation(World world, JsonValue data) => throw new InvalidOperationException("unused admission reading must not validate history");
    }
    public sealed class ReadingSpatialAdmissionModule : GameModule
    {
        public override string Id => "proof.reading-spatial-admission";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public readonly InitializeProbe Initialize = new InitializeProbe("proof.reading.initialize");
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<UnavailableTileSpaceDef>("unavailable_tile_spaces");
            types.Section<ReadingAdmissionDef>("reading_admission_probes");
            types.Section<PointReadingDef>("point_readings");
        }
        public override void Compose(WorldComposer composer) => composer.AddSystem(Initialize, 301);
    }
}
