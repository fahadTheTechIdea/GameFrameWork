using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Registrations only; all normal eligibility is in the shared engine declarations.
    public sealed class IndependentTilePropagationRuleDef : Def, IIncidentExposureEffectRule, IContentDependencyDefinition
    {
        private TileIncidentPropagationDeclaration _declaration;
        public int Resolutions { get; private set; }
        public ITileSpaceDefinition Space => _declaration.Space;
        public long Radius => _declaration.Radius;
        public GameDuration Interval => _declaration.Interval;
        public GameDuration Delay => _declaration.Delay;
        public Code TargetSource => _declaration.TargetSource;
        public Code TargetKind => _declaration.TargetKind;
        public bool CrossCompany => _declaration.CrossCompany;
        public IReadOnlyList<SignalGateCondition> SourceConditions => _declaration.SourceConditions;
        public IReadOnlyList<SignalGateCondition> TargetConditions => _declaration.TargetConditions;
        public TileFootprintConditionDeclaration SourceTiles => _declaration.SourceTiles;
        public TileFootprintConditionDeclaration TargetTiles => _declaration.TargetTiles;
        public IIncidentExposureEffect ExposureEffect=>_declaration.ExposureEffect;
        public IReadOnlyList<IContentDefinition> ContentDependencies=>ExposureEffect==null?Array.Empty<IContentDefinition>():Array.AsReadOnly(new IContentDefinition[]{ExposureEffect});
        protected override void ReadCore(JsonReader reader) => _declaration = TileIncidentPropagationDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) { _declaration.Resolve(content); Resolutions++; }
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.Validate(content);
        public void ValidateSource(EntityDef source) => _declaration.ValidateSource(source);
        public void ValidateBinding(World world) => _declaration.ValidateBinding(world, Id);
        public IncidentExposureObservation Observe(IncidentPropagationContext context, Entity target)
            => _declaration.Observe(context, target, Id);
    }

    public sealed class IndependentTerrainConditionDef : Def, ITileConditionDefinition
    {
        private readonly TileLayerBandDeclaration _declaration = new TileLayerBandDeclaration();
        internal Action<World, ITileSpatialSpace> Observe;
        public ITileSpaceDefinition Space => _declaration.Space;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.Validate(content);
        public bool Matches(World world, ITileSpatialSpace map, TileCoord tile)
        {
            TileConditionContracts.RequireContext(this, world, map); Observe?.Invoke(world, map);
            return _declaration.Matches(world, map, tile);
        }
    }
    public sealed class TileIncidentPropagationProofModule : GameModule
    {
        private readonly InitializeProbe _initialize;
        private World _world;
        public TileIncidentPropagationProofModule(InitializeProbe initialize) { _initialize = initialize; }
        public override string Id => "proof.terrain-incidents";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(IncidentPropagationModule.ModuleId, Version),
            new ModuleRequirement(WorldSetupModule.ModuleId, Version),
            new ModuleRequirement(TileConditionsModule.ModuleId, Version) };
        public bool ActualAliases { get; private set; }
        public bool OneExposureOwner { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentTilePropagationRuleDef>("independent_tile_propagation_rules");
            types.Section<IndependentTerrainConditionDef>("independent_terrain_conditions");
            types.Section<TileTerrainWorkEffectDef>("prepared_terrain_effects");
            types.Section<ExposurePreparationProbeDef>("exposure_preparation_probes");
        }
        public override void Compose(WorldComposer composer)
        {
            _world = composer.World;
            var binding = composer.Content.Get<EntityDef>("terrain.source").Component<IIncidentPropagationDefinition>(new Code("spread"));
            var rule = binding.Rule;
            var sources = rule is IndependentTilePropagationRuleDef direct ? direct.SourceTiles : ((TileIncidentPropagationRuleDef)rule).SourceTiles;
            var targets = rule is IndependentTilePropagationRuleDef independent ? independent.TargetTiles : ((TileIncidentPropagationRuleDef)rule).TargetTiles;
            ActualAliases = ReferenceEquals(composer.Content.Get<IIncidentPropagationRule>("terrain.spread"), rule)
                && (sources.Conditions.Count == 0 || ReferenceEquals(sources.Conditions[0], composer.Content.Get<ITileConditionDefinition>("terrain.connected")))
                && ReferenceEquals(targets.Conditions[0], composer.Content.Get<ITileConditionDefinition>("terrain.receiver"));
            if (rule is IndependentTilePropagationRuleDef selected) ActualAliases &= selected.Resolutions == 1;
            OneExposureOwner = composer.Services.Providers<IIncidentPropagationState>().Count() == 1;
            composer.AddSystem(_initialize, 12345);
        }
        // Armed only by the negative callback test, never a normal game update or rule.
        public void Arm(string fault)
        {
            var condition = (IndependentTerrainConditionDef)_world.Content.Get<ITileConditionDefinition>("terrain.connected");
            condition.Observe = (world, map) => {
                if (fault == "throw") throw new InvalidOperationException("proof.terrain.predicate");
                if (fault == "pause") ((GameClock)world.Clock).Pause();
                if (fault == "layer") ((ITileWritableSampleLayer)TileSpatialGeometry.RequireSampleLayer(world, map,
                    world.Content.Get<ISpatialLayerDefinition>("network.line"), false)).SetSample(new TileCoord(11,28), Fix.Zero);
                if (fault == "touch") foreach (var entity in world.Entities.All)
                    if (entity.Def.Id == "terrain.source") { entity.Touch(); break; }
            };
        }
    }
}
