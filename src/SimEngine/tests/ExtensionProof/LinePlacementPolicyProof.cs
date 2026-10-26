using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent content registration; the original declaration and work/topology owners do the work.
    public sealed class IndependentLinePlacementPolicyDef : Def, ILinePlacementPolicy
    {
        private TileLinePlacementDeclaration _declaration;
        private string _fault;
        private GameDuration _after;
        public ISpatialSpaceDefinition Space => _fault == "space-null" ? null : _declaration.Space;
        protected override void ReadCore(JsonReader r)
        {
            _fault = r.String("fault"); _after = r.Child("read_after").RationalDuration();
            if (_after.IsZero || _fault != "none" && _fault != "space-null" && _fault != "clock" && _fault != "throw"
                && _fault != "clock-throw" && _fault != "map") throw r.Error("explicit negative mode/time required");
            _declaration = TileLinePlacementDeclaration.Read(r);
        }
        protected override void ResolveCore(ContentResolver r) => _declaration.Resolve(r);
        protected override void ValidateResolvedCore(ContentResolver r) => _declaration.Validate(r);
        public void ValidateBinding(World world) { _declaration.ValidateBinding(world); _ = world.Time.Ticks(_after); }
        public Code Check(World world, PlayerId company, SpatialPlacement placement)
        {
            if (world.Clock.StepIndex >= world.Time.Ticks(_after))
            {
                if (_fault == "clock" || _fault == "clock-throw") ((GameClock)world.Clock).Speed = 2;
                if (_fault == "map") ((TileMap)world.Space).Layer<ByteLayer>("segment.pipe").Set(new TileCoord(0,0), 1);
                if (_fault == "throw" || _fault == "clock-throw") throw new InvalidOperationException("proof.line-placement-first-error");
            }
            return _declaration.Check(world, company, placement);
        }
    }
    public sealed class LinePlacementPolicyProofModule : GameModule
    {
        public override string Id => "proof.line-placement-policies";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InfrastructureModule.ModuleId, Version) };
        public readonly InitializeProbe Initialize = new InitializeProbe("proof.line-placement.initialize");
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentLinePlacementPolicyDef>("independent_line_placement_policies");
            types.Section<TileLayerBandConditionDef>("tile_layer_band_conditions");
        }
        public override void Compose(WorldComposer composer) => composer.AddSystem(Initialize, 8000);
    }
}
