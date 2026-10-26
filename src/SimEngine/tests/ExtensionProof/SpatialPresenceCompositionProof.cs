using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Negative composition instrumentation only; never a geometry or gameplay fallback.
    public sealed class UnavailableTileSpaceDef : Def, ITileSpaceDefinition
    {
        private TileSpaceDeclaration _declaration;
        public IReadOnlyList<TileSpaceGeometry> Geometries => _declaration.Geometries;
        public TileSpaceGeometry ReadGeometry(string scenario) => _declaration.ReadGeometry(scenario);
        private string _mode;
        protected override void ReadCore(JsonReader reader)
        { _declaration=TileSpaceDeclaration.Read(reader); _mode=reader.String("mode"); }
        public void Compose(WorldComposer composer)
        {
            if (_mode == "missing-provider") return;
            var space = new UnavailableSpace(_mode == "wrong-provider-id" ? Id+".wrong" : Id, composer.World, _mode);
            foreach (var layer in composer.Content.All<ISpatialLayerDefinition>())
                if (layer.Space.Id == Id) space.RegisterLayer(layer);
            composer.Services.Bind<ISpatialSpace>(space);
        }
        private sealed class UnavailableSpace : ISpatialSpace, ISpatialLayerProvider
        {
            private readonly World _world;
            private readonly string _mode;
            private readonly List<ISpatialLayer> _layers = new List<ISpatialLayer>();
            private int _reads;
            internal UnavailableSpace(string id, World world, string mode) { Id=id; _world=world; _mode=mode; }
            public string Id { get; }
            public int ObservationVersion
            {
                get
                {
                    if(_mode=="revision-negative")return -1;
                    if(_mode=="revision-state"||_mode=="revision-state-throw")_reads++;
                    if(_mode=="revision-clock"||_mode=="revision-clock-throw")((GameClock)_world.Clock).Speed=2;
                    if(_mode=="revision-throw"||_mode=="revision-state-throw"||_mode=="revision-clock-throw")
                        throw new InvalidOperationException("proof.space-revision-first-error");
                    return _reads;
                }
            }
            public int StateSchemaVersion=>1;
            public void Sync(IStateSync sync) => sync.Int(ref _reads);
            public IReadOnlyList<ISpatialLayer> SpatialLayers
            {
                get {
                    if (_mode=="layer-null") return null;
                    if (_mode=="layer-missing") return Array.Empty<ISpatialLayer>();
                    if (_mode=="layer-clock"||_mode=="layer-clock-throw") ((GameClock)_world.Clock).Speed=2;
                    if (_mode=="layer-state"||_mode=="layer-state-throw") _reads++;
                    if (_mode=="layer-throw"||_mode=="layer-clock-throw"||_mode=="layer-state-throw")
                        throw new InvalidOperationException("proof.layer-admission-first-error");
                    return _layers.AsReadOnly();
                }
            }
            private sealed class MetadataLayer : SpatialLayer
            { internal MetadataLayer(ISpatialLayerDefinition definition) : base(definition) { } }
            public void RegisterLayer(ISpatialLayerDefinition definition) => _layers.Add(new MetadataLayer(definition));
            public void AssignOwner(World world, string id, PlayerId owner) => throw new InvalidOperationException("negative provider must not assign");
            public void ValidateComposition(World world) { }
            public void OnSpawn(Entity entity)=>throw new InvalidOperationException("negative provider must not spawn");
            public void OnDespawn(Entity entity)=>throw new InvalidOperationException("negative provider must not despawn");
            public void OnLoaded(World world)=>throw new InvalidOperationException("negative provider must not load");
            public SpatialWorldSnapshot Capture(PlayerId viewer)=>throw new InvalidOperationException("negative provider must not publish");
        }
    }
    // Direct contract declaration, unused runtime capability. Exercises admission, not simulation.
    public sealed class AdmissionPresenceDef : ComponentDef<AdmissionPresence>, ISpatialPresenceDefinition
    {
        public string Space { get; private set; }
        private string _mode;
        protected override void ReadCore(JsonReader reader) { Space=reader.String("space"); _mode=reader.String("mode"); }
        protected override void ResolveCore(ContentResolver content)=>content.Def<ISpatialSpaceDefinition>(Space);
        public void ValidateComposition(World world, EntityDef entity)
        {
            if (!ReferenceEquals(entity.Component<ISpatialPresenceDefinition>(),this) || !(world.Space is TileMap selected) || selected.Id!=Space)
                throw new ContentException(entity.Id+": admission probe requires the actual tile provider");
            if (_mode == "clock" || _mode == "throw-after-clock") ((GameClock)world.Clock).Speed=2;
            if (_mode == "throw" || _mode == "throw-after-clock") throw new InvalidOperationException("proof.presence-first-error");
        }
    }
    public sealed class AdmissionPresence : Component<AdmissionPresenceDef>, ISpatialPresence
    {
        public string Space=>Def.Space;
        public void BeginTick()=>throw new InvalidOperationException("unused admission probe must not update");
        public SpatialSnapshot Capture()=>throw new InvalidOperationException("unused admission probe must not publish");
    }
    public sealed class SpatialPresenceCompositionModule : GameModule
    {
        public override string Id=>"proof.spatial-presence-composition";
        public override ModuleVersion Version=>new ModuleVersion(1,0,0);
        public readonly InitializeProbe Initialize=new InitializeProbe();
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentTileSpaceDef>("independent_tile_spaces");
            types.Section<UnavailableTileSpaceDef>("unavailable_tile_spaces");
            types.Component<AdmissionPresenceDef>("admission_presence",1);
        }
        public override void Compose(WorldComposer composer)=>composer.AddSystem(Initialize,301);
    }
}
