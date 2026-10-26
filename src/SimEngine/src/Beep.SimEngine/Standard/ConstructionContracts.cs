using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IRemovalPlanDefinition : IEntityTargetPlanDefinition, IPlayerActivityDefinition, IActivityWorkflowBinding
    { }
    public interface IConstructionDefinition : IComponentDefinition, IPlanDefinition, IActivitySource, IActivityWorkflowBinding
    {
        EntityDef TargetDefinition { get; }
        EntityDef SiteDefinition { get; }
        IEntityStateDefinition SiteState { get; }
        Bill SupplyCapacity { get; }
        Bill Salvage { get; }
        IRemovalPlanDefinition Demolition { get; }
    }
    public interface IConstructionSiteDefinition : IComponentDefinition
    {
        EntityDef Target { get; }
        IEntityStateDefinition ConstructionState { get; }
    }
    public interface IConstructionSite : IEntityCapability
    {
        EntityDef Target { get; }
        int Order { get; set; }
    }
    public interface IConstructionProjects
    {
        Code CheckIntent(World world, PlayerId company, EntityDef definition, JsonValue arguments);
        Code CheckPlacement(World world, PlayerId company, EntityDef definition, JsonValue arguments);
        Entity PlaceSite(World world, PlayerId company, EntityDef definition, JsonValue arguments, int priority);
        Code Abandon(World world, Entity site);
        Code Finish(World world, Order order);
        Code CheckDemolition(World world, Entity entity);
        Code PreviewDemolition(World world, PlayerId company, EntityId target, out PlanIntent intent);
        Code Demolish(World world, Order order);
    }
    /// <summary>Declared engine-side rule; shared by actual placement, commands and previews.</summary>
    public interface IConstructionPlacementRule
    {
        Code CheckPlacement(World world, PlayerId company, EntityDef definition, SpatialPlacement placement);
    }
}
