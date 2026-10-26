using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class MachineFleetsModule : GameModule
    {
        public const string ModuleId = "sim.machine-fleets";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {
            new ModuleRequirement(MotionModule.ModuleId, Version), new ModuleRequirement(WorkModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types) => types.Section<AuthoredFleetPolicyDef>("fleet_policies");
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Bind<IMachineFleets>(new MachineFleetService(composer.World));
            composer.Commands.Register("sim.configure-fleet", new ConfigureFleetHandler());
            composer.Commands.Register("sim.remove-fleet", new RemoveFleetHandler());
            composer.Commands.Register("sim.configure-fleet-navigation", new ConfigureFleetNavigationHandler());
            composer.Commands.Register("sim.configure-fleet-work-policy", new ConfigureFleetWorkPolicyHandler());
            composer.Commands.Register("sim.configure-fleet-home", new ConfigureFleetHomeHandler());
            composer.Commands.Register("sim.configure-fleet-load-policy", new ConfigureFleetLoadPolicyHandler());
            composer.Commands.Register("sim.configure-fleet-trip-policy", new ConfigureFleetTripPolicyHandler());
            composer.Commands.Register("sim.configure-fleet-recurring-plan", new ConfigureFleetRecurringPlanHandler());
            composer.Commands.Register("sim.configure-fleet-outlet-policy", new ConfigureFleetOutletPolicyHandler());
            composer.AddViewFeature(Id, 7, (world, viewer) => new FleetView(world, viewer));
        }
    }
}
