using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentUniformRuleDef : Def, ITileNavigationRule
    {
        private readonly UniformTileNavigationDeclaration _declaration = new UniformTileNavigationDeclaration();
        private string _fault;
        public string Space => _fault == "space" ? null : _declaration.Space.Id;
        public int MinCost => _fault == "minimum" ? 0 : _declaration.Cost;
        protected override void ReadCore(JsonReader reader) { _declaration.Read(reader); _fault = reader.String("proof_fault", null); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public int Cost(World world, TileCoord tile)
        { TileNavigationContracts.RequireContext(this, world); return _declaration.Cost; }
    }
    public sealed class IndependentLayeredRuleDef : Def, ITileNavigationRule
    {
        private readonly LayeredTileNavigationDeclaration _declaration = new LayeredTileNavigationDeclaration();
        private string _fault;
        public string Space => _fault == "space" ? null : _declaration.Space.Id;
        public int MinCost => _fault == "minimum" ? 0 : _declaration.MinCost;
        public IReadOnlyList<LayeredTileNavigationEntry> Rules => _declaration.Rules;
        protected override void ReadCore(JsonReader reader) { _declaration.Read(reader); _fault = reader.String("proof_fault", null); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.Validate(content);
        public int Cost(World world, TileCoord tile)
        {
            TileNavigationContracts.RequireContext(this, world);
            int? invalid = world.Services.Get<SpatialRuleContractModule>().Observe(SpatialRuleTarget.Navigation, world);
            return invalid ?? _declaration.Cost(world, tile);
        }
    }
    public sealed class IndependentPlacementRuleDef : Def, IConstructionPlacementDefinition
    {
        private readonly TileConstructionPlacementDeclaration _declaration = new TileConstructionPlacementDeclaration();
        private string _fault;
        public string Space => _fault == "space" ? null : _declaration.Space.Id;
        public IReadOnlyList<TileConstructionPlacementEntry> Rules => _declaration.Rules;
        protected override void ReadCore(JsonReader reader) { _declaration.Read(reader); _fault = reader.String("proof_fault", null); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.Validate(content);
        public Code CheckPlacement(World world, PlayerId company, EntityDef definition, SpatialPlacement placement)
        {
            if (!ConstructionPlacementContracts.IsActual(this, world)) return Codes.InvalidArgument;
            world.Services.Get<SpatialRuleContractModule>().Observe(SpatialRuleTarget.Placement, world);
            return _declaration.CheckPlacement(world, company, definition, placement);
        }
    }
    // Different authored terrain semantics, not a copy of the engine's layered/footprint rules.
    public sealed class GradientNavigationRuleDef : Def, ITileNavigationRule
    {
        private string _space, _condition;
        private ITileSpaceDefinition _resolved;
        private ITileConditionDefinition _blocked;
        private int _minimum, _preferredY;
        public string Space => _resolved.Id;
        public int MinCost => _minimum;
        protected override void ReadCore(JsonReader reader)
        {
            _space = reader.String("space"); _condition = reader.String("blocked_condition");
            _minimum = reader.Int("minimum_cost"); _preferredY = reader.Int("preferred_y");
            if (_minimum < 1 || _preferredY < 0) throw reader.Error("positive minimum and nonnegative preferred row required");
        }
        protected override void ResolveCore(ContentResolver content)
        { _resolved = content.Def<ITileSpaceDefinition>(_space); _blocked = content.Def<ITileConditionDefinition>(_condition); }
        protected override void ValidateResolvedCore(ContentResolver content)
        {
            if (!ReferenceEquals(_blocked.Space, _resolved) || !SpatialSpaceContracts.ContainsAny(_resolved, new TileRect(0, _preferredY, 1, 1)))
                throw content.Fail("gradient needs actual shared condition space and in-bounds preferred row");
        }
        public int Cost(World world, TileCoord tile)
        {
            TileNavigationContracts.RequireContext(this, world);
            if (TileConditionContracts.Matches(_blocked, world, (TileMap)world.Space, tile)) return 0;
            return checked(_minimum + Math.Abs(tile.Y - _preferredY));
        }
    }
    public enum SpatialRuleTarget { Navigation, Placement }
    public enum SpatialRuleFault { Exception, LayerWrite, PauseClock, AllocateIdentity, NegativeCost, BelowMinimum }
    public sealed class SpatialRuleContractModule : GameModule
    {
        private SpatialRuleFault? _fault;
        private SpatialRuleTarget _target;
        public int Reads;
        public bool MutationCompleted, Aliases, IndependentNavigation, IndependentPlacement, DirectDefinitions;
        public InitializeProbe Initialize { get; private set; }
        public override string Id => "proof.spatial-rule-contracts";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(TileNavigationModule.ModuleId, Version),
            new ModuleRequirement(TileConstructionRulesModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentUniformRuleDef>("independent_uniform_navigation");
            types.Section<IndependentLayeredRuleDef>("independent_layered_navigation");
            types.Section<IndependentPlacementRuleDef>("independent_placement_rules");
            types.Section<GradientNavigationRuleDef>("gradient_navigation");
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Bind(this);
            var navigation = composer.Content.Get<ITileNavigationRule>("land");
            var placement = composer.Content.Get<IConstructionPlacementDefinition>("colony.placement");
            IndependentNavigation = navigation is IndependentLayeredRuleDef || navigation is GradientNavigationRuleDef;
            IndependentPlacement = placement is IndependentPlacementRuleDef;
            Aliases = ReferenceEquals(placement, composer.Services.Get<IConstructionPlacementRule>()) &&
                navigation.Space == composer.World.Space.Id && placement.Space == composer.World.Space.Id &&
                composer.Services.Get<NavService>().Find(navigation.Id)?.MinCost == navigation.MinCost;
            DirectDefinitions = typeof(UniformTileNavigationDef).BaseType == typeof(Def) &&
                typeof(LayeredTileNavigationDef).BaseType == typeof(Def) &&
                typeof(TileConstructionPlacementDef).BaseType == typeof(Def);
            Initialize = new InitializeProbe(); composer.AddSystem(Initialize, 12346);
        }
        public void Arm(SpatialRuleTarget target, SpatialRuleFault fault)
        { _target = target; _fault = fault; Reads = 0; MutationCompleted = false; }
        internal int? Observe(SpatialRuleTarget target, World world)
        {
            if (!_fault.HasValue || _target != target || Reads != 0) return null;
            Reads++;
            if (_fault == SpatialRuleFault.Exception) throw new InvalidOperationException("proof.spatial-profile");
            if (_fault == SpatialRuleFault.LayerWrite) ((TileMap)world.Space).Layer<ByteLayer>("water").Set(new TileCoord(1, 1), 1);
            if (_fault == SpatialRuleFault.PauseClock) ((GameClock)world.Clock).Pause();
            if (_fault == SpatialRuleFault.AllocateIdentity) world.Spawn(world.Content.Get<EntityDef>("tower"), new PlayerId(0),
                entity => entity.Get<ITilePresence>().Place(new TileCoord(44, 28), FixVec2.Centre(new TileCoord(44, 28))));
            MutationCompleted = true;
            if (_fault == SpatialRuleFault.NegativeCost) return -1;
            if (_fault == SpatialRuleFault.BelowMinimum) return 1;
            return null;
        }
    }
}
