using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent registered metadata reuses the sole native band declaration, never a native Def base.
    public sealed class IndependentTileBandDef : Def, ITileConditionDefinition
    {
        private readonly TileLayerBandDeclaration _declaration = new TileLayerBandDeclaration();
        private string _fault;
        private ITileSpaceDefinition _other;
        public ITileSpaceDefinition Space => _fault == "null-space" ? null : _fault == "foreign-alias" ? _other : _declaration.Space;
        protected override void ReadCore(JsonReader reader)
        { _declaration.Read(reader); _fault = reader.String("proof_fault", null); }
        protected override void ResolveCore(ContentResolver content)
        { _declaration.Resolve(content); if (_fault == "foreign-alias") _other = new TileSpaceDef(); }
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.Validate(content);
        public bool Matches(World world, ITileSpatialSpace map, TileCoord tile)
        {
            TileConditionContracts.RequireContext(this, world, map);
            world.Services.Get<TileConditionContractModule>().Observe(world, map);
            return _declaration.Matches(world, map, tile);
        }
    }
    public enum TilePredicateFault { Exception, LayerWrite, PauseClock, AllocateIdentity }
    public sealed class TileConditionContractModule : GameModule
    {
        private TilePredicateFault? _fault;
        public int Reads;
        public bool MutationCompleted;
        public bool SharedAlias, Independent, DirectDefinitions;
        public InitializeProbe Initialize { get; private set; }
        public override string Id => "proof.tile-contracts";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types) => types.Section<IndependentTileBandDef>("independent_tile_bands");
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Bind(this);
            var navigation = composer.Content.Get<LayeredTileNavigationDef>("land");
            var placement = composer.Content.Get<TileConstructionPlacementDef>("colony.placement");
            var predicate = navigation.Rules[0].Conditions[0];
            SharedAlias = ReferenceEquals(predicate, placement.Rules[0].Conditions[0]) &&
                ReferenceEquals(predicate, composer.Content.Get<ITileConditionDefinition>(predicate.Id)) &&
                ReferenceEquals(predicate.Space, composer.Content.Get<ITileSpaceDefinition>("colony.tiles"));
            Independent = predicate is IndependentTileBandDef || predicate is TileColumnsConditionDef;
            DirectDefinitions = typeof(TileLayerBandConditionDef).BaseType == typeof(Def) &&
                typeof(IndependentTileBandDef).BaseType == typeof(Def) && typeof(TileColumnsConditionDef).BaseType == typeof(Def);
            Initialize = new InitializeProbe(); composer.AddSystem(Initialize, 12345);
        }
        public void Arm(TilePredicateFault fault) { _fault = fault; Reads = 0; MutationCompleted = false; }
        internal void Observe(World world, ITileSpatialSpace map)
        {
            if (!_fault.HasValue || Reads != 0) return;
            Reads++;
            if (_fault == TilePredicateFault.Exception) throw new InvalidOperationException("proof.tile-predicate");
            if (_fault == TilePredicateFault.LayerWrite)
                ((TileMap)map).Layer<ByteLayer>("water").Set(new TileCoord(1, 1), 1);
            if (_fault == TilePredicateFault.PauseClock) ((GameClock)world.Clock).Pause();
            if (_fault == TilePredicateFault.AllocateIdentity) world.Spawn(world.Content.Get<EntityDef>("tower"), new PlayerId(0), entity =>
                    entity.Get<ITilePresence>().Place(new TileCoord(44, 28), FixVec2.Centre(new TileCoord(44, 28))));
            MutationCompleted = true;
        }
    }
}
