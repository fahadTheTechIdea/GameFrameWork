using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    public sealed class ConfigureHaulTripPolicyCommand : Command
    {
        public EntityId Worker;
        public HaulTripSettings Settings;
        public override void Sync(IStateSync state) { state.Entity(ref Worker); Settings = HaulTripSettings.Sync(state, Settings); }
    }
    internal sealed class ConfigureHaulTripPolicyHandler : CommandHandler<ConfigureHaulTripPolicyCommand>
    {
        protected override Code Check(World world, ConfigureHaulTripPolicyCommand command)
            => HaulTripPolicyContracts.CheckConfigure(world, command.Player, world.Entities.Get(command.Worker), command.Settings);
        protected override void Apply(World world, ConfigureHaulTripPolicyCommand command, CommandContext context)
        { HaulTripPolicyContracts.Configure(world, world.Entities.Get(command.Worker), command.Settings); context.Entity = command.Worker; }
    }
    public sealed class ConfigureHaulTripPolicyGroupCommand : Command
    {
        public EntityId[] Workers;
        public HaulTripSettings Settings;
        public override void Sync(IStateSync state)
        {
            int count = state.Count(Workers?.Length ?? 0);
            var ids = state.Reading ? new EntityId[count] : Workers == null ? System.Array.Empty<EntityId>() : (EntityId[])Workers.Clone();
            if (!state.Reading) System.Array.Sort(ids);
            for (int i = 0; i < ids.Length; i++) state.Entity(ref ids[i]);
            Settings = HaulTripSettings.Sync(state, Settings); if (state.Reading) Workers = ids;
        }
    }
    internal sealed class ConfigureHaulTripPolicyGroupHandler : CommandHandler<ConfigureHaulTripPolicyGroupCommand>
    {
        protected override Code Check(World world, ConfigureHaulTripPolicyGroupCommand command)
            => HaulTripPolicyContracts.PreviewGroup(world, command.Player, command.Workers, command.Settings).Blocker;
        protected override void Apply(World world, ConfigureHaulTripPolicyGroupCommand command, CommandContext context)
        { context.Arg1 = HaulTripPolicyContracts.ConfigureGroup(world, command.Player, command.Workers, command.Settings); context.Arg0 = command.Workers.Length; }
    }
}
