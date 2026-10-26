using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Actual authored bindings and original observations; never another project owner.</summary>
    public static class ConstructionBindingContracts
    {
        public static void ValidateComposition(World world)
        {
            foreach (EntityDef owner in world.Content.All<EntityDef>())
                foreach (ComponentDef part in owner.Components)
                {
                    if (part.ComponentType == typeof(Constructible) && !(part is IConstructionDefinition))
                        throw new ContentException(owner.Id + ": reusable construction requires IConstructionDefinition");
                    if (part is IConstructionDefinition construction)
                        EntityObservation.ReadProvider(world, part, null, () => { ValidateConstruction(world, part, construction); return true; });
                    bool site = typeof(IConstructionSite).IsAssignableFrom(part.ComponentType);
                    if (site != (part is IConstructionSiteDefinition))
                        throw new ContentException(owner.Id + ": construction site runtime and metadata must bind together");
                    if (site) ReadSite(world, (IConstructionSiteDefinition)part, null, _ => true);
                }
        }
        private static void ValidateConstruction(World world, ComponentDef part, IConstructionDefinition construction)
        {
            if (!part.IsFrozen || !world.Content.Owns(part.Owner) ||
                !ReferenceEquals(part.Owner.Component<IConstructionDefinition>(), construction) ||
                part.Owner.Kind != EntityKind.Structure || !ReferenceEquals(construction.TargetDefinition, part.Owner) ||
                part.ComponentType == typeof(Constructible) && part.StateCodec.SchemaVersion != 1)
                throw new ContentException("construction requires actual immutable structure metadata and its registered codec");
            var workflow = ValidateWorkflow(world, construction.Workflow);
            if (!ReferenceEquals(construction.Graph, workflow.Graph) || construction.AutomaticSupply != workflow.AutomaticSupply ||
                construction.PlanKey != "build:" + part.Owner.Id || construction.OnComplete != ConstructionService.FinishEffect ||
                construction.SupplyCapacity == null || construction.Salvage == null)
                throw new ContentException(part.Owner.Id + ": construction must bind the actual authored workflow and explicit bills");
            IEntityStateDefinition state = construction.SiteState;
            if (state == null || !world.Content.Owns(state) || !ReferenceEquals(world.Content.Find<IEntityStateDefinition>(state.Id), state))
                throw new ContentException(part.Owner.Id + ": construction requires its actual authored site state");
            Bill expected = WorkBoard.SupplyCapacity(workflow.Graph, world.Content);
            var supply = construction.SupplyCapacity.Lines;
            if (supply.Count != expected.Lines.Count) throw new ContentException(part.Owner.Id + ": construction supply envelope differs from its workflow");
            for (int i = 0; i < supply.Count; i++)
                if (supply[i].Resource != expected.Lines[i].Resource || supply[i].Amount != expected.Lines[i].Amount)
                    throw new ContentException(part.Owner.Id + ": construction supply envelope differs from its workflow");
            foreach (ResourceAmount yield in construction.Salvage.Lines)
                if (world.Content.Resource(yield.Resource) == null ||
                    UnitConversion.ValidateAmount(world.Content.Resource(yield.Resource).QuantityUnit, yield.Amount) != UnitConversionStatus.Converted)
                    throw new ContentException(part.Owner.Id + ": salvage requires actual resource units");
            IRemovalPlanDefinition removal = construction.Demolition;
            if (removal != null)
            {
                var removalWorkflow = ValidateWorkflow(world, removal.Workflow);
                if (removal.PlanKey != "demolish:" + part.Owner.Id || removal.OnComplete != ConstructionService.DemolishEffect ||
                    !ReferenceEquals(removal.Graph, removalWorkflow.Graph) || removal.AutomaticSupply != removalWorkflow.AutomaticSupply)
                    throw new ContentException(part.Owner.Id + ": removal must bind its actual authored workflow");
                removal.ValidateTargetDefinition(part.Owner);
            }
        }
        private static IAuthoredActivityDefinition ValidateWorkflow(World world, IActivityDefinition selected)
        {
            if (!(selected is IAuthoredActivityDefinition workflow) || !world.Content.Owns(workflow) ||
                !ReferenceEquals(world.Content.Find<IAuthoredActivityDefinition>(workflow.Id), workflow) ||
                workflow.PlayerOrderable || workflow.TargetTags.Count != 0 || workflow.OnComplete != null ||
                workflow.Graph == null || !workflow.Graph.HasMachineWork(null))
                throw new ContentException("construction requires an actual unrestricted non-orderable physical activity");
            return workflow;
        }
        public static T ReadSite<T>(World world, IConstructionSiteDefinition definition, Entity entity, Func<IConstructionSiteDefinition,T> read)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            return EntityObservation.ReadProvider(world, definition, entity, () => {
                var scope = entity == null ? default : EntityObservation.CaptureProvider(world, entity, entity.Sync, "construction.site-metadata");
                try
                {
                    if (!(definition is ComponentDef part) || !part.IsFrozen || !world.Content.Owns(part.Owner) ||
                        !ReferenceEquals(part.Owner.Component<IConstructionSiteDefinition>(), definition) ||
                        !typeof(IConstructionSite).IsAssignableFrom(part.ComponentType) ||
                        part.ComponentType == typeof(Site) && part.StateCodec.SchemaVersion != 1 ||
                        entity != null && !ReferenceEquals(entity.Def, part.Owner))
                        throw new ContentException("site requires its actual immutable owner/runtime/codec binding");
                    EntityDef target = definition.Target;
                    if (target == null || !world.Content.Owns(target) || !ReferenceEquals(world.Content.Find<EntityDef>(target.Id), target) ||
                        !ReferenceEquals(target.Component<IConstructionDefinition>()?.SiteDefinition, part.Owner) ||
                        !ReferenceEquals(definition.ConstructionState, target.Component<IConstructionDefinition>()?.SiteState))
                        throw new ContentException(part.Owner.Id + ": site requires its exact authored target and construction state");
                    T result = read(definition);
                    if (entity != null) scope.Verify();
                    return result;
                }
                catch (Exception error) { world.LatchFault("construction.site-metadata", error); throw; }
            });
        }
    }
}
