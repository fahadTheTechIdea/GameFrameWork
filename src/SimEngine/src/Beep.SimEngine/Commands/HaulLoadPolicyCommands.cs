using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    public sealed class ConfigureHaulLoadPolicyCommand : Command
    {
        public EntityId Worker;
        public HaulLoadSettings Settings;
        public override void Sync(IStateSync sync) { sync.Entity(ref Worker); Settings = HaulLoadSettings.Sync(sync, Settings); }
    }
    internal sealed class ConfigureHaulLoadPolicyHandler : CommandHandler<ConfigureHaulLoadPolicyCommand>
    {
        protected override Code Check(World world, ConfigureHaulLoadPolicyCommand command)
            => HaulLoadPolicyContracts.CheckConfigure(world, command.Player, world.Entities.Get(command.Worker), command.Settings);
        protected override void Apply(World world, ConfigureHaulLoadPolicyCommand command, CommandContext context)
        { HaulLoadPolicyContracts.Configure(world, world.Entities.Get(command.Worker), command.Settings); context.Entity = command.Worker; }
    }
    public sealed class ConfigureHaulLoadPolicyGroupCommand : Command
    {
        public EntityId[] Workers;
        public HaulLoadSettings Settings;
        public override void Sync(IStateSync state)
        {
            int count = state.Count(Workers?.Length ?? 0);
            var ids = state.Reading ? new EntityId[count] : Workers == null ? System.Array.Empty<EntityId>() : (EntityId[])Workers.Clone();
            if (!state.Reading) System.Array.Sort(ids);
            for (int i = 0; i < ids.Length; i++) state.Entity(ref ids[i]);
            Settings = HaulLoadSettings.Sync(state, Settings); if (state.Reading) Workers = ids;
        }
    }
    internal sealed class ConfigureHaulLoadPolicyGroupHandler : CommandHandler<ConfigureHaulLoadPolicyGroupCommand>
    {
        protected override Code Check(World world, ConfigureHaulLoadPolicyGroupCommand command)
            => HaulLoadPolicyContracts.PreviewGroup(world, command.Player, command.Workers, command.Settings).Blocker;
        protected override void Apply(World world, ConfigureHaulLoadPolicyGroupCommand command, CommandContext context)
        { context.Arg1 = HaulLoadPolicyContracts.ConfigureGroup(world, command.Player, command.Workers, command.Settings); context.Arg0 = command.Workers.Length; }
    }
}
