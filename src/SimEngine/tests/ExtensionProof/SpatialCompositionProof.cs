using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Negative missing-provider declaration only. Never creates fallback geometry.
    public sealed class AbsentSpatialSpaceDef : Def, ISpatialSpaceDefinition
    {
        protected override void ReadCore(JsonReader reader) { }
        public void Compose(WorldComposer composer) { }
    }
    public sealed class SpatialCompositionProbeDef : Def
    {
        public string Mode { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Mode = reader.String("mode");
            if (Mode != "valid" && Mode != "foreign-navigation") throw reader.Error("explicit spatial composition probe mode required");
        }
    }
    // Registration and negative composition instrumentation only. Native engine owners run gameplay.
    public sealed class SpatialCompositionModule : GameModule
    {
        public override string Id => "proof.spatial-composition";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(MotionModule.ModuleId, Version) };
        public readonly InitializeProbe Initialize = new InitializeProbe();
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<AbsentSpatialSpaceDef>("absent_spatial_spaces");
            types.Section<IndependentTileSpaceDef>("independent_tile_spaces");
            types.Component<IndependentMoverDef>("independent_mover", 4);
            types.Section<SpatialCompositionProbeDef>("spatial_composition_probes");
        }
        public override void Compose(WorldComposer composer)
        {
            if (composer.Content.Get<SpatialCompositionProbeDef>("spatial.probe").Mode == "foreign-navigation")
                composer.Services.Bind(new NavService(null));
            composer.AddSystem(Initialize, 301);
        }
    }
}
