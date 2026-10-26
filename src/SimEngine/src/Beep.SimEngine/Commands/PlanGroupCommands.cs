using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    public sealed class SubmitPlanGroupCommand : Command
    {
        public PlanGroupIntent Intent;
        public override void Sync(IStateSync sync) => PlanGroupIntent.Sync(sync, ref Intent);
    }
    public sealed class ConfigurePlanGroupCommand : Command
    {
        public string Group;
        public int Priority;
        public bool Paused;
        public override void Sync(IStateSync sync) { sync.String(ref Group); sync.Int(ref Priority); sync.Bool(ref Paused); }
    }
    public sealed class MovePlanGroupCommand : Command
    {
        public string Group;
        public long Before;
        public override void Sync(IStateSync sync) { sync.String(ref Group); sync.Long(ref Before); }
    }
    public sealed class CancelPlanGroupCommand : Command
    {
        public string Group;
        public override void Sync(IStateSync sync) => sync.String(ref Group);
    }
    public sealed class RepeatPlanGroupCommand : Command
    {
        public string Group, NewGroup;
        public override void Sync(IStateSync sync) { sync.String(ref Group); sync.String(ref NewGroup); }
    }
    internal sealed class SubmitPlanGroupHandler : CommandHandler<SubmitPlanGroupCommand>
    {
        protected override Code Check(World world, SubmitPlanGroupCommand command) => world.Services.Get<IPlanBoard>().PreviewPlanGroup(command.Player, command.Intent).Blocker;
        protected override void Apply(World world, SubmitPlanGroupCommand command, CommandContext result)
        { var ids = world.Services.Get<IPlanBoard>().SubmitPlanGroup(command.Player, command.Intent); result.Arg0 = ids[0]; result.Arg1 = ids.Count; }
    }
    internal sealed class ConfigurePlanGroupHandler : CommandHandler<ConfigurePlanGroupCommand>
    {
        protected override Code Check(World world, ConfigurePlanGroupCommand command) => world.Services.Get<IPlanBoard>().PreviewPlanGroupEdit(command.Player, command.Group, command.Priority, command.Paused).Blocker;
        protected override void Apply(World world, ConfigurePlanGroupCommand command, CommandContext result)
            => world.Services.Get<IPlanBoard>().ConfigurePlanGroup(command.Player, command.Group, command.Priority, command.Paused);
    }
    internal sealed class MovePlanGroupHandler : CommandHandler<MovePlanGroupCommand>
    {
        protected override Code Check(World world, MovePlanGroupCommand command) => world.Services.Get<IPlanBoard>().PreviewPlanGroupMove(command.Player, command.Group, command.Before).Blocker;
        protected override void Apply(World world, MovePlanGroupCommand command, CommandContext result)
            => world.Services.Get<IPlanBoard>().MovePlanGroup(command.Player, command.Group, command.Before);
    }
    internal sealed class CancelPlanGroupHandler : CommandHandler<CancelPlanGroupCommand>
    {
        protected override Code Check(World world, CancelPlanGroupCommand command) => world.Services.Get<IPlanBoard>().PreviewPlanGroupCancel(command.Player, command.Group).Blocker;
        protected override void Apply(World world, CancelPlanGroupCommand command, CommandContext result)
            => world.Services.Get<IPlanBoard>().CancelPlanGroup(command.Player, command.Group);
    }
    internal sealed class RepeatPlanGroupHandler : CommandHandler<RepeatPlanGroupCommand>
    {
        protected override Code Check(World world, RepeatPlanGroupCommand command) => world.Services.Get<IPlanBoard>().PreviewPlanGroupRepeat(command.Player, command.Group, command.NewGroup).Blocker;
        protected override void Apply(World world, RepeatPlanGroupCommand command, CommandContext result)
        { var ids = world.Services.Get<IPlanBoard>().RepeatPlanGroup(command.Player, command.Group, command.NewGroup); result.Arg0 = ids[0]; result.Arg1 = ids.Count; }
    }
}
