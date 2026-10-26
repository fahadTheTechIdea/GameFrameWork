using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class WorkModule : GameModule
    {
        public const string ModuleId = "sim.work";
        public const int SystemOrder = 400;
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[]
        {
            new ModuleRequirement(ContentAccessModule.ModuleId, Version),
            new ModuleRequirement(InventoryModule.ModuleId, Version),
            new ModuleRequirement(MotionModule.ModuleId, Version)
        });
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<WorkerRoleDef>("worker_roles");
            types.Component<WorkerDef>("worker", 1);
            types.Component<TileWorkDockDef>("tile_work_dock", 5);
            types.Component<WorkerExecutionDef>("worker_execution", 1);
            types.Section<WorkPolicyDef>("work_policies");
            types.Section<AuthoredWorkRetryProfileDef>("work_retry_profiles");
            types.Component<WorkPolicyComponentDef>("work_policy", 4);
            types.Component<RecurringActivitySourceDef>("recurring_activity", 8);
            types.Section<AuthoredActivityFacilityPolicyDef>("activity_facility_policies");
            types.Section<ActivityDef>("activities");
            types.Section<StaffedActivityExecutorDef>("staffed_activity_executors");
            types.Section<TimedActivityExecutorDef>("timed_activity_executors");
            types.Section<CompanyActivityExecutorDef>("company_activity_executors");
            types.Component<CompanyActivityRateDef>("company_activity_rate", 1);
            types.Section<FixedActivityExecutorDef>("fixed_activity_executors");
            types.Section<EntityActivityDef>("entity_activities");
            types.Section<EntityReplacementEffectDef>("entity_replacement_effects");
            types.Section<EntityRemovalEffectDef>("entity_removal_effects");
            types.Section<PlanTemplateDef>("plan_templates");
            types.Section<WorkRequirementGroupDef>("work_requirement_groups");
            types.Section<TargetResourceRequirementDef>("target_resource_requirements");
            types.Section<WorkerResourceRequirementDef>("worker_resource_requirements");
            types.Section<TargetUtilityRequirementDef>("target_utility_requirements");
            types.Section<WorkerUtilityRequirementDef>("worker_utility_requirements");
            types.Section<TargetRegionRequirementDef>("target_region_requirements");
            types.Section<DockWorkRequirementDef>("dock_work_requirements");
            types.Section<TargetStateRequirementDef>("target_state_requirements");
            types.Section<WorkerStateRequirementDef>("worker_state_requirements");
            types.Section<TargetSignalRequirementDef>("target_signal_requirements");
            types.Section<WorkerSignalRequirementDef>("worker_signal_requirements");
            types.Section<PriorActivityRequirementDef>("prior_activity_requirements");
            types.Section<InventoryEffectDef>("inventory_effects");
            types.Section<ResourceRecoveryEffectDef>("resource_recovery_effects");
        }
        public override void Compose(WorldComposer composer)
        {
            ActivityDefinitionContracts.ValidateComposition(composer.World);
            ActivityStartRules.Validate(composer.World);
            WorkerRoleContracts.ValidateComposition(composer.World);
            WorkPolicyDeclaration.ValidateComposition(composer.World);
            ActivityFacilityPolicyDeclaration.ValidateComposition(composer.World);
            composer.Services.Require<IContentAccess>();
            JsonReader settings = composer.Scenario.SettingsFor(ModuleId) ?? throw new ContentException("selected sim.work needs an explicit retry policy");
            string retry = settings.String("retry"); settings.End();
            var board = new WorkBoard(composer.World, composer.Content.Get<IWorkRetryPolicy>(retry));
            composer.Services.Bind(board);
            composer.Services.Bind<IWorkAssignments>(board);
            composer.Services.Bind<IActivityExecutionOrders>(board);
            composer.Services.Bind<IWorkDockAdmission>(board);
            composer.Services.Bind<IActivityCompletionHistory>(board);
            composer.Services.Bind<IPlanBoard>(board);
            composer.Services.Bind<IActivityCatalog>(board);
            composer.Services.Bind<IActivityFacilitySelection>(board);
            composer.Services.Bind<IIncidentPreparedness>(board);
            composer.Services.Bind<IWorkLoad>(board);
            composer.Services.Bind<IOperatingAreas>(new OperatingAreas(composer.World));
            composer.Events.Register<EntityReplaced>("sim.entity-replaced", 1);
            var work = new WorkSystem();
            composer.Services.Bind(new TravelWorkPhaseExecutor());
            composer.Services.Bind(new ProductiveWorkPhaseExecutor());
            composer.Services.Bind(new PickupWorkPhaseExecutor());
            composer.Services.Bind(new DeliveryWorkPhaseExecutor());
            composer.Services.Bind(new DepartureWorkPhaseExecutor());
            composer.Services.Bind<IWorkPhaseCatalog>(work);
            composer.Services.Bind<IWorkerDispatch>(work);
            composer.Services.Bind<IWorkerControl>(work);
            composer.Services.Bind<IWorkDispatchQueue>(work);
            composer.AddSystem(work, SystemOrder);
            composer.Commands.Register("sim.post-order", new PostOrderHandler());
            composer.Commands.Register("sim.cancel-order", new CancelOrderHandler());
            composer.Commands.Register("sim.set-order-priority", new SetOrderPriorityHandler());
            composer.Commands.Register("sim.set-home", new SetHomeHandler());
            composer.Commands.Register("sim.set-worker-held", new Commands.SetWorkerHeldHandler());
            composer.Commands.Register("sim.set-worker-home-group", new Commands.SetWorkerHomeGroupHandler());
            composer.Commands.Register("sim.configure-work-policy", new Commands.ConfigureWorkPolicyHandler());
            composer.Commands.Register("sim.configure-work-policy-group", new Commands.ConfigureWorkPolicyGroupHandler());
            composer.Commands.Register("sim.set-operating-area", new Commands.SetOperatingAreaHandler());
            composer.Commands.Register("sim.remove-operating-area", new Commands.RemoveOperatingAreaHandler());
            composer.Commands.Register("sim.configure-recurring-plan", new Commands.ConfigureRecurringPlanHandler());
            composer.Commands.Register("sim.configure-recurring-template", new Commands.ConfigureRecurringTemplateHandler());
            composer.Commands.Register("sim.submit-plan", new Commands.SubmitPlanHandler());
            composer.Commands.Register("sim.configure-plan", new Commands.ConfigurePlanHandler());
            composer.Commands.Register("sim.configure-plan-dependencies", new Commands.ConfigurePlanDependenciesHandler());
            composer.Commands.Register("sim.move-plan", new Commands.MovePlanHandler());
            composer.Commands.Register("sim.cancel-plan", new Commands.CancelPlanHandler());
            composer.Commands.Register("sim.repeat-plan", new Commands.RepeatPlanHandler());
            composer.Commands.Register("sim.retire-plan-history", new Commands.RetirePlanHistoryHandler());
            composer.Commands.Register("sim.submit-plan-group", new Commands.SubmitPlanGroupHandler());
            composer.Commands.Register("sim.configure-plan-group", new Commands.ConfigurePlanGroupHandler());
            composer.Commands.Register("sim.move-plan-group", new Commands.MovePlanGroupHandler());
            composer.Commands.Register("sim.cancel-plan-group", new Commands.CancelPlanGroupHandler());
            composer.Commands.Register("sim.repeat-plan-group", new Commands.RepeatPlanGroupHandler());
            composer.Commands.Register("sim.place-plan-template", new Commands.PlacePlanTemplateHandler());
            composer.Commands.Register("sim.store-plan-template", new Commands.StorePlanTemplateHandler());
            composer.Commands.Register("sim.remove-plan-template", new Commands.RemovePlanTemplateHandler());
            composer.AddViewFeature("sim.work", 112, (world, viewer) => new WorkView(world, viewer));
        }
    }
}
