using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class ExplorationModule : GameModule
    {
        public const string ModuleId = "sim.exploration";
        public const int SystemOrder = 420;
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(CompanyKnowledgeModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<AuthoredExplorationProfileDef>("exploration_profiles"); types.Component<AuthoredExplorationDef>("authored_exploration", 1);
            types.Section<ContactExplorationRequirementDef>("contact_exploration_requirements");
            types.Section<SensorRegionExplorationRequirementDef>("sensor_region_exploration_requirements");
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Require<ICompanyKnowledge>(); composer.Services.Require<IKnowledgeFreshness>();
            composer.AddSystem(new ExplorationSystem(), SystemOrder);
            composer.AddViewFeature(Id, 2, (world, viewer) => new ExplorationView(world, viewer));
        }
    }
    public sealed class ExplorationSystem : GameSystem, ICompositionValidator
    {
        public override string Id => ExplorationModule.ModuleId;
        public override int StateSchemaVersion => 1;
        public void ValidateComposition(World world)
        {
            foreach (var profile in world.Content.All<IAuthoredExplorationProfileDefinition>())
                AuthoredExplorationContracts.ValidateProfile(world, profile);
            foreach (EntityDef entity in world.Content.All<EntityDef>()) foreach (ComponentDef part in entity.Components)
                if (part is IExplorationDefinition definition)
                {
                    if (part.ComponentType == typeof(AuthoredExploration) && !(part is IAuthoredExplorationDefinition))
                        throw new ContentException("reusable authored exploration requires its actual binding contract");
                    if (!typeof(IExplorationSource).IsAssignableFrom(part.ComponentType)) throw new ContentException("exploration definition requires its actual provider contract");
                    EntityObservation.ReadProvider(world, definition, null, () => { definition.ValidateBinding(world); return true; });
                }
        }
        public override void Update(World world, TickInfo tick)
        {
            foreach (Entity entity in world.Entities.All) if (entity.Alive)
                foreach (Component part in entity.Components) if (part is IExplorationSource source)
                {
                    if (!(part.Def is IExplorationDefinition definition) || source.CapabilityId != definition.CapabilityId
                        || !ReferenceEquals(source.Entity, entity)) throw new InvalidOperationException("exploration source differs from its actual declaration");
                    source.AdvanceExploration(new ExplorationUpdateContext(world, source)); world.RequireHealthyUpdate();
                }
        }
        public override void OnLoaded(World world)
        {
            foreach (Entity entity in world.Entities.All) if (entity.Alive)
                foreach (Component part in entity.Components) if (part is IExplorationSource source)
                { EntityObservation.ReadProvider(world, source, entity, () => { source.ValidateExploration(world); return true; }, observeTargetState: true); }
        }
    }
}
