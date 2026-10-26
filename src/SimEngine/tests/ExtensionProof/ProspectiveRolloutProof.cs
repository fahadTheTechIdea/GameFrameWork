using System;
using System.Collections.Generic;
using Beep.SimEngine;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Read-only parameter binding to the real engine rollout and resource admission owners.</summary>
    public sealed class ProspectiveRolloutView
    {
        private readonly World _world;
        public ProspectiveRolloutView(World world) { _world = world; }
        public (Code, SpatialPlacement) Preview(string producer, string output, SpatialPlacement origin,
            IReadOnlyList<SpatialPlacement> occupied)
        {
            var prefix = new List<PlannedEntityPlacement>();
            if (occupied == null) return (Codes.InvalidArgument, null);
            foreach (var placement in occupied)
                prefix.Add(new PlannedEntityPlacement(_world.Content.Find<EntityDef>(output), placement));
            Code refusal = EntityRollout.Find(_world, _world.Content.Find<EntityDef>(producer), origin,
                _world.Content.Find<EntityDef>(output), prefix, out SpatialPlacement selected);
            return (refusal, selected);
        }
        public (Code, SpatialPlacement) PreviewLive(EntityId producer, string output)
        {
            Code refusal = EntityRollout.Find(_world, _world.Entities.Get(producer),
                _world.Content.Find<EntityDef>(output), out SpatialPlacement selected);
            return (refusal, selected);
        }
        public ResourceTransferStatus InitialStock(string definition, string resource, Fix amount)
            => _world.Services.Get<IResourceTransaction>().EvaluateInitialInventory(
                _world.Content.Find<EntityDef>(definition), new Bill(new[]
                { new ResourceAmount(_world.Content.ResourceId(resource), amount) })).Status;
        public ResourceTransferStatus EmptyInitialStock(string definition)
            => _world.Services.Get<IResourceTransaction>().EvaluateInitialInventory(
                _world.Content.Find<EntityDef>(definition), Bill.Empty).Status;
    }
    public sealed class ProspectiveRolloutModule : GameModule
    {
        public override string Id => "proof.prospective-rollout";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires =>
            new[] { new ModuleRequirement(InventoryModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        { types.Section<PointSpaceDef>("point_spaces"); types.Component<PointPresenceDef>("point_presence", 1); }
        public override void Compose(WorldComposer composer)
            => composer.AddViewFeature(Id, 1, (world, viewer) => new ProspectiveRolloutView(world));
    }
}
