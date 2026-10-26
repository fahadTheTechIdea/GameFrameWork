using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class LogisticsModule : GameModule
    {
        public const string ModuleId = "sim.logistics";
        public const int SystemOrder = 450;
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[]
        {
            new ModuleRequirement(WorkModule.ModuleId, Version),
            new ModuleRequirement(InventoryModule.ModuleId, Version)
        });
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<TransporterDef>("transporter", 2);
            types.Section<AuthoredResourceHandlingDef>("handling_profiles");
            types.Section<LogisticsPolicyDef>("logistics_policies");
            types.Component<LogisticsPolicyComponentDef>("logistics_policy", 3);
            types.Section<AuthoredHaulLoadProfileDef>("haul_load_profiles");
            types.Component<HaulLoadPolicyDef>("haul_load_policy", 2);
            types.Section<AuthoredHaulTripProfileDef>("haul_trip_profiles");
            types.Component<HaulTripPolicyDef>("haul_trip_policy", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Require<WorkBoard>();
            composer.Services.Require<IWorkerDispatch>();
            composer.Services.Require<IResourceTransaction>();
            var logistics = new LogisticsService(composer.World);
            composer.Services.Bind(logistics);
            composer.Services.Bind<ITransformationSupplyRequests>(logistics);
            composer.Services.Bind<IWorkSupplyRequests>(logistics);
            composer.Services.Bind<IHaulExecution>(logistics);
            composer.Services.Bind<IHaulPlayerControls>(logistics);
            composer.Services.Bind<ILogisticsDemandRequests>(logistics);
            composer.Services.Bind<ILogisticsStockPolicies>(logistics);
            composer.Services.Bind<ILogisticsRoutes>(logistics);
            composer.Services.Bind<ILogisticsDispatchQueue>(logistics);
            composer.Services.Bind<ILogisticsDiagnostics>(logistics);
            composer.Services.Bind<ILogisticsDemandSources>(logistics);
            composer.Services.Bind<IWorkerDispatchSource>(logistics);
            composer.AddSystem(new LogisticsMaintenanceSystem(), SystemOrder);
            composer.Commands.Register("sim.configure-stock-policy", new Beep.SimEngine.Commands.ConfigureStockPolicyHandler());
            composer.Commands.Register("sim.configure-outlet-policy", new Beep.SimEngine.Commands.ConfigureOutletPolicyHandler());
            composer.Commands.Register("sim.configure-outlet-policy-group", new Beep.SimEngine.Commands.ConfigureOutletPolicyGroupHandler());
            composer.Commands.Register("sim.configure-haul-load-policy", new Beep.SimEngine.Commands.ConfigureHaulLoadPolicyHandler());
            composer.Commands.Register("sim.configure-haul-load-policy-group", new Beep.SimEngine.Commands.ConfigureHaulLoadPolicyGroupHandler());
            composer.Commands.Register("sim.configure-haul-trip-policy", new Beep.SimEngine.Commands.ConfigureHaulTripPolicyHandler());
            composer.Commands.Register("sim.configure-haul-trip-policy-group", new Beep.SimEngine.Commands.ConfigureHaulTripPolicyGroupHandler());
            composer.Commands.Register("sim.haul-resources", new Beep.SimEngine.Commands.HaulResourcesHandler());
            composer.Commands.Register("sim.add-route", new AddRouteHandler());
            composer.Commands.Register("sim.configure-route", new ConfigureRouteHandler());
            composer.Commands.Register("sim.remove-route", new RemoveRouteHandler());
            composer.AddViewFeature("sim.logistics", 58, (world, viewer) => new LogisticsView(world, viewer));
        }
    }
}
