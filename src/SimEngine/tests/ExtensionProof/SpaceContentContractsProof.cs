using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent content authoring; delegates the framework's sole native geometry/composition.
    public sealed class IndependentTileSpaceDef : Def, ITileSpaceDefinition
    {
        private TileSpaceDeclaration _declaration;
        private string _fault;
        public IReadOnlyList<TileSpaceGeometry> Geometries => _fault == "width" || _fault == "height"
            ? Array.AsReadOnly(new[] { default(TileSpaceGeometry) }) : _declaration.Geometries;
        public TileSpaceGeometry ReadGeometry(string scenario)
        {
            TileSpaceGeometry geometry = _declaration.ReadGeometry(scenario);
            return _fault == "mismatch" ? new TileSpaceGeometry(geometry.Scenario, checked(geometry.Width + 1), geometry.Height) : geometry;
        }
        protected override void ReadCore(JsonReader reader)
        { _declaration = TileSpaceDeclaration.Read(reader); _fault = reader.String("proof_fault", null); }
        public void Compose(WorldComposer composer) => _declaration.Compose(composer, this);
    }
    public sealed class SpaceContentContractsModule : GameModule
    {
        public override string Id => "proof.space-contracts";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(TileNavigationModule.ModuleId, new ModuleVersion(1, 0, 0)) };
        public readonly InitializeProbe Initialize = new InitializeProbe();
        // Transient verification of composition, never authoritative map/game state.
        public bool Independent, Aliases;
        public override void DeclareContent(ContentTypes types) => types.Section<IndependentTileSpaceDef>("independent_tile_spaces");
        public override void Compose(WorldComposer composer)
        {
            var content = composer.Content;
            var space = content.Get<ITileSpaceDefinition>("survey.tiles");
            Independent = space is IndependentTileSpaceDef;
            Aliases = ReferenceEquals(content.Get<ISpatialSpaceDefinition>(space.Id), space)
                && ReferenceEquals(content.Get<ScenarioDef>("native-survey").Space, space)
                && ReferenceEquals(content.Get<ISpatialLayerDefinition>("survey.deposit").Space, space)
                && ReferenceEquals(content.Get<ISpatialLayerDefinition>("survey.terrain").Space, space)
                && ReferenceEquals(content.Get<RectangularTileRegionDef>("space.rectangle").SpaceDefinition, space)
                && ReferenceEquals(content.Get<PaintedTileRegionDef>("space.paint").SpaceDefinition, space)
                && ReferenceEquals(content.Get<TileLayerBandConditionDef>("space.band").Space, space)
                && ReferenceEquals(content.Get<EntityDef>("survey.machine").Component<ITileMoverDefinition>()?.SpaceDefinition, space)
                && composer.World.Space is TileMap map && map.Width == space.ReadGeometry(composer.Scenario.Id).Width
                && map.Height == space.ReadGeometry(composer.Scenario.Id).Height;
            if (!Aliases) throw new ContentException("independent space does not alias actual engine composition");
            composer.AddSystem(Initialize, 10);
        }
    }
}
