using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Commands
{
    public sealed class RetirePlanHistoryCommand : Command
    {
        public PlanHistorySelection Selection;
        public override void Sync(IStateSync sync) => PlanHistorySelection.Sync(sync, ref Selection);
    }
    internal sealed class RetirePlanHistoryHandler : CommandHandler<RetirePlanHistoryCommand>
    {
        protected override Code Check(World world, RetirePlanHistoryCommand command)
            => world.Services.Get<IPlanBoard>().PreviewRetirePlanHistory(command.Player, command.Selection).Blocker;
        protected override void Apply(World world, RetirePlanHistoryCommand command, CommandContext result)
        {
            world.Services.Get<IPlanBoard>().RetirePlanHistory(command.Player, command.Selection);
            result.Arg0 = command.Selection.Plans[0]; result.Arg1 = command.Selection.Plans.Count;
        }
    }
    public sealed class SubmitPlanCommand : Command
    {
        public PlanIntent Intent;
        public override void Sync(IStateSync sync) => PlanIntent.Sync(sync, ref Intent);
    }
    public sealed class ConfigurePlanCommand : Command
    {
        public long Plan;
        public string Group;
        public int Priority;
        public bool Paused;
        public override void Sync(IStateSync sync)
        { sync.Long(ref Plan); sync.String(ref Group); sync.Int(ref Priority); sync.Bool(ref Paused); }
    }
    public sealed class MovePlanCommand : Command
    {
        public long Plan, Before;
        public override void Sync(IStateSync sync) { sync.Long(ref Plan); sync.Long(ref Before); }
    }
    public sealed class ConfigurePlanDependenciesCommand : Command
    {
        public PlanDependencyChanges Changes;
        public override void Sync(IStateSync sync) => PlanDependencyChanges.Sync(sync, ref Changes);
    }
    public sealed class CancelPlanCommand : Command
    {
        public PlanCancellationSelection Selection;
        public override void Sync(IStateSync sync) => PlanCancellationSelection.Sync(sync, ref Selection);
    }
    public sealed class RepeatPlanCommand : Command
    {
        public long Plan;
        public override void Sync(IStateSync sync) => sync.Long(ref Plan);
    }
    internal sealed class SubmitPlanHandler : CommandHandler<SubmitPlanCommand>
    {
        protected override Code Check(World world, SubmitPlanCommand command)
            => world.Services.Get<IPlanBoard>().PreviewPlan(command.Player, command.Intent);
        protected override void Apply(World world, SubmitPlanCommand command, CommandContext result)
        { result.Arg0 = world.Services.Get<IPlanBoard>().SubmitPlan(command.Player, command.Intent); }
    }
    internal sealed class ConfigurePlanHandler : CommandHandler<ConfigurePlanCommand>
    {
        protected override Code Check(World world, ConfigurePlanCommand command)
            => world.Services.Get<IPlanBoard>().PreviewPlanEdit(command.Player, command.Plan, command.Group, command.Priority, command.Paused);
        protected override void Apply(World world, ConfigurePlanCommand command, CommandContext result)
        { world.Services.Get<IPlanBoard>().ConfigurePlan(command.Player, command.Plan, command.Group, command.Priority, command.Paused); result.Arg0 = command.Plan; }
    }
    internal sealed class MovePlanHandler : CommandHandler<MovePlanCommand>
    {
        protected override Code Check(World world, MovePlanCommand command)
            => world.Services.Get<IPlanBoard>().PreviewPlanMove(command.Player, command.Plan, command.Before);
        protected override void Apply(World world, MovePlanCommand command, CommandContext result)
        { world.Services.Get<IPlanBoard>().MovePlan(command.Player, command.Plan, command.Before); result.Arg0 = command.Plan; }
    }
    internal sealed class ConfigurePlanDependenciesHandler : CommandHandler<ConfigurePlanDependenciesCommand>
    {
        protected override Code Check(World world, ConfigurePlanDependenciesCommand command)
            => world.Services.Get<IPlanBoard>().PreviewPlanDependencies(command.Player, command.Changes).Blocker;
        protected override void Apply(World world, ConfigurePlanDependenciesCommand command, CommandContext result)
        {
            world.Services.Get<IPlanBoard>().ConfigurePlanDependencies(command.Player, command.Changes);
            result.Arg0 = command.Changes.Entries[0].Plan; result.Arg1 = command.Changes.Entries.Count;
        }
    }
    internal sealed class CancelPlanHandler : CommandHandler<CancelPlanCommand>
    {
        protected override Code Check(World world, CancelPlanCommand command)
            => world.Services.Get<IPlanBoard>().PreviewPlanCancel(command.Player, command.Selection).Blocker;
        protected override void Apply(World world, CancelPlanCommand command, CommandContext result)
        {
            world.Services.Get<IPlanBoard>().CancelPlan(command.Player, command.Selection);
            result.Arg0 = command.Selection.Plans[0]; result.Arg1 = command.Selection.Plans.Count;
        }
    }
    internal sealed class RepeatPlanHandler : CommandHandler<RepeatPlanCommand>
    {
        protected override Code Check(World world, RepeatPlanCommand command)
            => world.Services.Get<IPlanBoard>().PreviewPlanRepeat(command.Player, command.Plan);
        protected override void Apply(World world, RepeatPlanCommand command, CommandContext result)
            => result.Arg0 = world.Services.Get<IPlanBoard>().RepeatPlan(command.Player, command.Plan);
    }
}
