using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Read-only observation of native profiles, not a second cost interpreter.</summary>
    public sealed class OilNavigationProofView
    {
        private readonly World _world;
        public OilNavigationProofView(World world) { _world = world; }
        public int EntryCost(string profile, TileCoord tile) =>
            _world.Services.Get<NavService>().Grid(profile).Cost(tile);
        public int MinimumCost(string profile) =>
            _world.Services.Get<NavService>().Grid(profile).MinCost;
    }
    public sealed class OilNavigationProofModule : GameModule
    {
        public override string Id => "proof.oil-navigation";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires =>
            new[] { new ModuleRequirement(TileNavigationModule.ModuleId, Version) };
        public override void Compose(WorldComposer composer) =>
            composer.AddViewFeature(Id, 1, (world, viewer) => new OilNavigationProofView(world));
    }
}
