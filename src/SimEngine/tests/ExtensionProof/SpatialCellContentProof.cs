using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Direct metadata/delegation only: location, binding and physical work stay engine-owned.
    public sealed class DirectTileCellDef : Def, ISpatialResourceCellDefinition
    {
        private TileResourceCellDeclaration _declaration;
        public long Identity => _declaration.Cell.Identity;
        public bool Public => _declaration.Cell.Public;
        public ISpatialLayerDefinition Layer => _declaration.Cell.Layer;
        public ICapacityPolicy Capacity => _declaration.Cell.Capacity;
        protected override void ReadCore(JsonReader reader) => _declaration = TileResourceCellDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.ValidateResolved(content);
        public void ValidateComposition(World world) => _declaration.ValidateComposition(world);
        public IResourceOutlet Bind(World world, ResourceEndpointId endpoint) => _declaration.Bind(world, endpoint);
        public Code CheckSite(World world, Entity target) => _declaration.CheckSite(world, target);
    }
    // Explicit negative metadata modes; none is selected by the positive proof.
    public sealed class BrokenTileCellDef : Def, ISpatialResourceCellDefinition
    {
        private TileResourceCellDeclaration _declaration;
        private string _mode;
        private World _faultWorld;
        private string _siteFault;
        private bool _afterRaise;
        public int SiteCalls { get; private set; }
        public int VersionAtCall { get; private set; }
        // Negative callback instrumentation only, scoped to the exact armed session.
        public void ArmSite(World world, string fault, bool afterRaise)
        {
            if(fault!="none"&&fault!="refuse"&&fault!="target"&&fault!="target-refuse"&&fault!="target-throw"&&fault!="clock"&&fault!="throw")
                throw new ArgumentException("explicit spill-site probe fault required");
            _faultWorld=world;_siteFault=fault;_afterRaise=afterRaise;SiteCalls=0;
        }
        public long Identity => _mode == "identity" ? 0 : _declaration.Cell.Identity;
        public bool Public => _mode == "access" ? !_declaration.Cell.Public : _declaration.Cell.Public;
        public ISpatialLayerDefinition Layer => _mode == "layer" ? null : _declaration.Cell.Layer;
        public ICapacityPolicy Capacity => _mode == "capacity" ? null : _declaration.Cell.Capacity;
        protected override void ReadCore(JsonReader reader)
        {
            _declaration = TileResourceCellDeclaration.Read(reader); _mode = reader.String("mode");
            if (_mode != "identity" && _mode != "access" && _mode != "layer" && _mode != "capacity" && _mode != "throw"
                && _mode != "clock" && _mode != "bind" && _mode != "site-clock" && _mode != "site-probe") throw reader.Error("explicit negative probe mode required");
        }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.ValidateResolved(content);
        public void ValidateComposition(World world)
        {
            if (_mode == "throw") throw new InvalidOperationException("proof.cell-first-composition-error");
            if (_mode == "clock") ((GameClock)world.Clock).Speed = 2;
            _declaration.ValidateComposition(world);
        }
        public IResourceOutlet Bind(World world, ResourceEndpointId endpoint) => _mode == "bind" ? null : _declaration.Bind(world, endpoint);
        public Code CheckSite(World world, Entity target)
        {
            if (_mode == "site-clock") ((GameClock)world.Clock).Speed = 2;
            if(_mode=="site-probe"&&ReferenceEquals(world,_faultWorld)&&
                (!_afterRaise||target.Get<IPhasedIncidentResponse>(new Code("leak"))?.ReadLifecycle().Active==true))
            {
                SiteCalls++;VersionAtCall=target.Version;
                if(_siteFault.StartsWith("target",StringComparison.Ordinal))target.Get<ExposurePrivateProbe>().Corrupt();
                if(_siteFault=="clock")((GameClock)world.Clock).Pause();
                if(_siteFault=="throw"||_siteFault=="target-throw")throw new InvalidOperationException("proof.spill-site-first-error");
                if(_siteFault=="refuse"||_siteFault=="target-refuse")return new Code("proof.spill-site-refused");
            }
            return _declaration.CheckSite(world, target);
        }
    }
    public sealed class SpatialCellContentModule : GameModule
    {
        public override string Id => "proof.spatial-cell-content";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {new ModuleRequirement(SpatialResourcesModule.ModuleId,Version)});
        public readonly InitializeProbe Initialize = new InitializeProbe("proof.cell.initialize");
        public override void DeclareContent(ContentTypes types)
        { types.Section<DirectTileCellDef>("direct_tile_cells"); types.Section<BrokenTileCellDef>("broken_tile_cells"); types.Component<ExposurePrivateProbeDef>("spill_site_private_probe",1); }
        public override void Compose(WorldComposer composer) => composer.AddSystem(Initialize, 306);
    }
}
