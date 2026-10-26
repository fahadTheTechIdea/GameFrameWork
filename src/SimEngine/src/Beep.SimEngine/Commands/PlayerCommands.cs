using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Commands
{
    /// <summary>Hand the current session seat to its authored controller, or take it back.</summary>
    public sealed class SetControllerCommand : Command
    {
        public Controller Controller;
        public override void Sync(IStateSync sync) => sync.Enum(ref Controller);
    }
    internal sealed class SetControllerHandler : CommandHandler<SetControllerCommand>
    {
        protected override Code Check(World world, SetControllerCommand command)
            => world.CheckController(command.Player, command.Controller);
        protected override void Apply(World world, SetControllerCommand command, CommandContext result)
            => world.SetController(command.Player, command.Controller);
    }
}
