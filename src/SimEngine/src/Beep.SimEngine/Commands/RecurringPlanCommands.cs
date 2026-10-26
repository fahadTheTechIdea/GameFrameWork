using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    public sealed class ConfigureRecurringTemplateCommand : Command
    {
        public EntityId Entity;
        public Code Source;
        public RecurringTemplateSettings Settings;
        public override void Sync(IStateSync sync)
        {
            sync.Entity(ref Entity); string source = Source.Id; sync.String(ref source); Source = source == null ? Code.None : new Code(source);
            Settings = RecurringTemplateSettings.Sync(sync, Settings);
        }
    }
    internal sealed class ConfigureRecurringTemplateHandler : CommandHandler<ConfigureRecurringTemplateCommand>
    {
        protected override Code Check(World world, ConfigureRecurringTemplateCommand command)
            => RecurringPlanContracts.CheckPlayerConfigureTemplate(world, command.Player, command.Entity, command.Source, command.Settings).Blocker;
        protected override void Apply(World world, ConfigureRecurringTemplateCommand command, CommandContext result)
        {
            RecurringPlanContracts.ConfigureTemplate(world, world.Entities.Get(command.Entity).Get<IRecurringPlanSource>(command.Source), command.Settings);
            result.Entity = command.Entity;
        }
    }
    public sealed class ConfigureRecurringPlanCommand : Command
    {
        public EntityId Entity;
        public Code Source;
        public RecurringPlanSettings Settings;
        public override void Sync(IStateSync sync)
        {
            sync.Entity(ref Entity); string source = Source.Id; sync.String(ref source); Source = source == null ? Code.None : new Code(source);
            Settings = RecurringPlanSettings.Sync(sync, Settings);
        }
    }
    internal sealed class ConfigureRecurringPlanHandler : CommandHandler<ConfigureRecurringPlanCommand>
    {
        protected override Code Check(World world, ConfigureRecurringPlanCommand command)
            => RecurringPlanContracts.CheckPlayerConfigure(world, command.Player, command.Entity, command.Source, command.Settings);
        protected override void Apply(World world, ConfigureRecurringPlanCommand command, CommandContext context)
        {
            RecurringPlanContracts.Configure(world, world.Entities.Get(command.Entity).Get<IRecurringPlanSource>(command.Source), command.Settings);
            context.Entity = command.Entity;
        }
    }
}
