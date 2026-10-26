using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    public sealed class SetOperatingAreaCommand : Command
    {
        public string Name, Template, ShapeJson;
        public long ExpectedRevision;
        public override void Sync(IStateSync sync)
        { sync.String(ref Name); sync.String(ref Template); sync.String(ref ShapeJson); sync.Long(ref ExpectedRevision); }
    }
    public sealed class RemoveOperatingAreaCommand : Command
    {
        public string Name;
        public long ExpectedRevision;
        public override void Sync(IStateSync sync) { sync.String(ref Name); sync.Long(ref ExpectedRevision); }
    }
    internal sealed class SetOperatingAreaHandler : CommandHandler<SetOperatingAreaCommand>
    {
        protected override Code Check(World world, SetOperatingAreaCommand command)
            => world.Services.Get<IOperatingAreas>().PreviewSet(command.Player, command.Name, command.Template, command.ShapeJson, command.ExpectedRevision);
        protected override void Apply(World world, SetOperatingAreaCommand command, CommandContext result)
            => world.Services.Get<IOperatingAreas>().Set(command.Player, command.Name, command.Template, command.ShapeJson, command.ExpectedRevision);
    }
    internal sealed class RemoveOperatingAreaHandler : CommandHandler<RemoveOperatingAreaCommand>
    {
        protected override Code Check(World world, RemoveOperatingAreaCommand command)
            => world.Services.Get<IOperatingAreas>().PreviewRemove(command.Player, command.Name, command.ExpectedRevision);
        protected override void Apply(World world, RemoveOperatingAreaCommand command, CommandContext result)
            => world.Services.Get<IOperatingAreas>().Remove(command.Player, command.Name, command.ExpectedRevision);
    }
}
