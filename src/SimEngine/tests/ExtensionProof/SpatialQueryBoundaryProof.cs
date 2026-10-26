using Beep.SimEngine;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class SpatialQueryBoundaryProofModule : GameModule
    {
        public const string ModuleId = "proof.spatial-query-boundary";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void Compose(WorldComposer composer)
            => composer.AddViewFeature("proof.spatial-query-boundary", 1, (world, viewer) => new SpatialQueryBoundaryProofView(world, viewer));
    }
    /// <summary>An independent assembly uses only the existing public read-only engine boundary.</summary>
    public sealed class SpatialQueryBoundaryProofView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal SpatialQueryBoundaryProofView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        public bool KnowsEntityPrototype(string identity)
        {
            _world.RequireHealthyUpdate();
            return _world.ViewFor(_viewer).CanSeeContent(_world.Content.Find<EntityDef>(identity));
        }
    }
}
