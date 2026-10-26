using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    public sealed class ConfigureAutomationCommand : Command
    {
        public string Rule;
        public AutomationSettings Settings;
        public override void Sync(IStateSync sync)
        { sync.String(ref Rule); Settings = AutomationSettings.Sync(sync, Settings); }
    }
    public sealed class RemoveAutomationCommand : Command
    {
        public string Rule;
        public override void Sync(IStateSync sync) => sync.String(ref Rule);
    }
    internal sealed class ConfigureAutomationHandler : CommandHandler<ConfigureAutomationCommand>
    {
        protected override Code Check(World world, ConfigureAutomationCommand command)
            => world.Services.Get<IAutomationRules>().CheckConfigure(command.Player, command.Rule, command.Settings);
        protected override void Apply(World world, ConfigureAutomationCommand command, CommandContext context)
        {
            world.Services.Get<IAutomationRules>().Configure(command.Player, command.Rule, command.Settings);
            context.Entity = command.Settings.Target.Entity;
        }
    }
    internal sealed class RemoveAutomationHandler : CommandHandler<RemoveAutomationCommand>
    {
        protected override Code Check(World world, RemoveAutomationCommand command)
            => world.Services.Get<IAutomationRules>().CheckRemove(command.Player, command.Rule);
        protected override void Apply(World world, RemoveAutomationCommand command, CommandContext context)
            => world.Services.Get<IAutomationRules>().Remove(command.Player, command.Rule);
    }
}
