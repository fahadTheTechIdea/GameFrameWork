using System.Collections.Generic;
using Beep.SimEngine;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Read-only native declaration/condition observation, not a second infrastructure interpreter.</summary>
    public sealed class OilInfrastructureProofView
    {
        private readonly World _world;
        public OilInfrastructureProofView(World world) { _world = world; }
        public Code Placement(string line, TileCoord at)
        {
            _world.RequireHealthyUpdate(); var definition = _world.Content.Get<ILineDefinition>(line);
            return LinePlacementContracts.Check(_world, definition.PlacementPolicy, new PlayerId(0),
                new TilePlacement("oil.tiles", at, FixVec2.Centre(at)));
        }
        public GameDuration Duration(string line)
        {
            _world.RequireHealthyUpdate();
            return _world.Content.Get<ILineDefinition>(line).Workflow.Graph.Find("install").Duration;
        }
        public Fix BoundCost(string line)
        {
            _world.RequireHealthyUpdate();
            var node = _world.Content.Get<ILineDefinition>(line).Workflow.Graph.Find("install");
            var binding = node.Requirement as IActivityRequirementBinding;
            if (binding == null) throw new ContentException("no selected cost binding");
            foreach (var entry in binding.Entries) if (entry.Requirement is IWorkCostRequirement cost) return cost.Amount;
            throw new ContentException("binding has no actual cost requirement");
        }
        public IResourceRate Rate(string network, string resource)
        {
            _world.RequireHealthyUpdate();
            return _world.Content.Get<INetworkDefinition>(network).Rate(_world.Content.ResourceId(resource));
        }
        public int CarriedResourceCount(string network)
        { _world.RequireHealthyUpdate(); return _world.Content.Get<INetworkDefinition>(network).Resources.Count; }
        public int LineCount { get { _world.RequireHealthyUpdate(); return _world.Content.All<ILineDefinition>().Count; } }
    }
    public sealed class OilInfrastructureProofModule : GameModule
    {
        public override string Id => "proof.oil-infrastructure";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InfrastructureModule.ModuleId, Version) };
        public override void Compose(WorldComposer composer)
            => composer.AddViewFeature(Id, 1, (world, viewer) => new OilInfrastructureProofView(world));
    }
}
