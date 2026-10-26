using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    public sealed class ConfigureFleetCommand : Command
    {
        public string Fleet;
        public FleetSettings Settings;
        public override void Sync(IStateSync sync)
        {
            sync.String(ref Fleet); bool present = Settings != null; sync.Bool(ref present);
            if (present) Settings = FleetSettings.Sync(sync, Settings); else if (sync.Reading) Settings = null;
        }
    }
    public sealed class RemoveFleetCommand : Command
    {
        public string Fleet;
        public override void Sync(IStateSync sync) => sync.String(ref Fleet);
    }
    public sealed class ConfigureFleetNavigationCommand : Command
    {
        public string Fleet, Profile;
        public override void Sync(IStateSync sync) { sync.String(ref Fleet); sync.String(ref Profile); }
    }
    public sealed class ConfigureFleetWorkPolicyCommand : Command
    {
        public string Fleet;
        public WorkPolicySettings Settings;
        public override void Sync(IStateSync sync)
        {
            sync.String(ref Fleet); bool present = Settings != null; sync.Bool(ref present);
            if (present) Settings = WorkPolicySettings.Sync(sync, Settings); else if (sync.Reading) Settings = null;
        }
    }
    public sealed class ConfigureFleetHomeCommand : Command
    {
        public string Fleet;
        public EntityId Home;
        public override void Sync(IStateSync sync) { sync.String(ref Fleet); sync.Entity(ref Home); }
    }
    internal sealed class ConfigureFleetHomeHandler : CommandHandler<ConfigureFleetHomeCommand>
    {
        protected override Code Check(World world, ConfigureFleetHomeCommand command)
            => world.Services.Get<IMachineFleets>().PreviewHome(command.Player, command.Fleet, command.Home).Blocker;
        protected override void Apply(World world, ConfigureFleetHomeCommand command, CommandContext context)
        {
            var fleets = world.Services.Get<IMachineFleets>();
            context.Arg1 = fleets.ConfigureHome(command.Player, command.Fleet, command.Home);
            fleets.TryRead(command.Player, command.Fleet, out var fleet); context.Arg0 = fleet.Settings.Members.Count;
        }
    }
    public sealed class ConfigureFleetRecurringPlanCommand : Command
    {
        public string Fleet;
        public Code Source;
        public RecurringPlanSettings Settings;
        public override void Sync(IStateSync sync)
        {
            sync.String(ref Fleet); string source = Source.Id; sync.String(ref source); Source = source == null ? Code.None : new Code(source);
            Settings = RecurringPlanSettings.Sync(sync, Settings);
        }
    }
    internal sealed class ConfigureFleetRecurringPlanHandler : CommandHandler<ConfigureFleetRecurringPlanCommand>
    {
        protected override Code Check(World world, ConfigureFleetRecurringPlanCommand command)
            => world.Services.Get<IMachineFleets>().PreviewRecurringPlan(command.Player, command.Fleet, command.Source, command.Settings).Blocker;
        protected override void Apply(World world, ConfigureFleetRecurringPlanCommand command, CommandContext context)
        {
            var fleets = world.Services.Get<IMachineFleets>();
            context.Arg1 = fleets.ConfigureRecurringPlan(command.Player, command.Fleet, command.Source, command.Settings);
            fleets.TryRead(command.Player, command.Fleet, out var fleet); context.Arg0 = fleet.Settings.Members.Count;
        }
    }
    public sealed class ConfigureFleetOutletPolicyCommand : Command
    {
        public string Fleet;
        public OutletPolicyChange Change;
        public override void Sync(IStateSync sync)
        {
            sync.String(ref Fleet); bool present = Change != null; sync.Bool(ref present);
            if (present) Change = OutletPolicyChange.Sync(sync, Change); else if (sync.Reading) Change = null;
        }
    }
    internal sealed class ConfigureFleetOutletPolicyHandler : CommandHandler<ConfigureFleetOutletPolicyCommand>
    {
        protected override Code Check(World world, ConfigureFleetOutletPolicyCommand command)
            => world.Services.Get<IMachineFleets>().PreviewOutletPolicy(command.Player, command.Fleet, command.Change).Blocker;
        protected override void Apply(World world, ConfigureFleetOutletPolicyCommand command, CommandContext context)
        {
            var fleets = world.Services.Get<IMachineFleets>();
            context.Arg1 = fleets.ConfigureOutletPolicy(command.Player, command.Fleet, command.Change);
            fleets.TryRead(command.Player, command.Fleet, out var fleet); context.Arg0 = fleet.Settings.Members.Count;
        }
    }
    public sealed class ConfigureFleetTripPolicyCommand : Command
    {
        public string Fleet;
        public HaulTripSettings Settings;
        public override void Sync(IStateSync sync) { sync.String(ref Fleet); Settings = HaulTripSettings.Sync(sync, Settings); }
    }
    internal sealed class ConfigureFleetTripPolicyHandler : CommandHandler<ConfigureFleetTripPolicyCommand>
    {
        protected override Code Check(World world, ConfigureFleetTripPolicyCommand command)
            => world.Services.Get<IMachineFleets>().PreviewTripPolicy(command.Player, command.Fleet, command.Settings).Blocker;
        protected override void Apply(World world, ConfigureFleetTripPolicyCommand command, CommandContext context)
        {
            var fleets = world.Services.Get<IMachineFleets>();
            context.Arg1 = fleets.ConfigureTripPolicy(command.Player, command.Fleet, command.Settings);
            fleets.TryRead(command.Player, command.Fleet, out var fleet); context.Arg0 = fleet.Settings.Members.Count;
        }
    }
    public sealed class ConfigureFleetLoadPolicyCommand : Command
    {
        public string Fleet;
        public HaulLoadSettings Settings;
        public override void Sync(IStateSync sync) { sync.String(ref Fleet); Settings = HaulLoadSettings.Sync(sync, Settings); }
    }
    internal sealed class ConfigureFleetLoadPolicyHandler : CommandHandler<ConfigureFleetLoadPolicyCommand>
    {
        protected override Code Check(World world, ConfigureFleetLoadPolicyCommand command)
            => world.Services.Get<IMachineFleets>().PreviewLoadPolicy(command.Player, command.Fleet, command.Settings).Blocker;
        protected override void Apply(World world, ConfigureFleetLoadPolicyCommand command, CommandContext context)
        {
            var fleets = world.Services.Get<IMachineFleets>();
            context.Arg1 = fleets.ConfigureLoadPolicy(command.Player, command.Fleet, command.Settings);
            fleets.TryRead(command.Player, command.Fleet, out var fleet); context.Arg0 = fleet.Settings.Members.Count;
        }
    }
    internal sealed class ConfigureFleetHandler : CommandHandler<ConfigureFleetCommand>
    {
        protected override Code Check(World world, ConfigureFleetCommand command)
            => world.Services.Get<IMachineFleets>().PreviewConfigure(command.Player, command.Fleet, command.Settings, out _);
        protected override void Apply(World world, ConfigureFleetCommand command, CommandContext context)
        { world.Services.Get<IMachineFleets>().Configure(command.Player, command.Fleet, command.Settings); context.Arg0 = command.Settings.Members.Count; }
    }
    internal sealed class RemoveFleetHandler : CommandHandler<RemoveFleetCommand>
    {
        protected override Code Check(World world, RemoveFleetCommand command) => world.Services.Get<IMachineFleets>().CheckRemove(command.Player, command.Fleet);
        protected override void Apply(World world, RemoveFleetCommand command, CommandContext context) => world.Services.Get<IMachineFleets>().Remove(command.Player, command.Fleet);
    }
    internal sealed class ConfigureFleetNavigationHandler : CommandHandler<ConfigureFleetNavigationCommand>
    {
        protected override Code Check(World world, ConfigureFleetNavigationCommand command)
            => world.Services.Get<IMachineFleets>().PreviewNavigation(command.Player, command.Fleet, command.Profile).Blocker;
        protected override void Apply(World world, ConfigureFleetNavigationCommand command, CommandContext context)
        {
            var fleets = world.Services.Get<IMachineFleets>();
            context.Arg1 = fleets.ConfigureNavigation(command.Player, command.Fleet, command.Profile);
            fleets.TryRead(command.Player, command.Fleet, out var fleet); context.Arg0 = fleet.Settings.Members.Count;
        }
    }
    internal sealed class ConfigureFleetWorkPolicyHandler : CommandHandler<ConfigureFleetWorkPolicyCommand>
    {
        protected override Code Check(World world, ConfigureFleetWorkPolicyCommand command)
            => world.Services.Get<IMachineFleets>().PreviewWorkPolicy(command.Player, command.Fleet, command.Settings).Blocker;
        protected override void Apply(World world, ConfigureFleetWorkPolicyCommand command, CommandContext context)
        {
            var fleets = world.Services.Get<IMachineFleets>();
            context.Arg1 = fleets.ConfigureWorkPolicy(command.Player, command.Fleet, command.Settings);
            fleets.TryRead(command.Player, command.Fleet, out var fleet); context.Arg0 = fleet.Settings.Members.Count;
        }
    }
}
