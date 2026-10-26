using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    public sealed class ConfigureWorkPolicyCommand : Command
    {
        public EntityId Worker;
        public WorkPolicySettings Settings;
        public override void Sync(IStateSync sync)
        {
            sync.Entity(ref Worker); bool present = Settings != null; sync.Bool(ref present);
            if (present) Settings = WorkPolicySettings.Sync(sync, Settings); else if (sync.Reading) Settings = null;
        }
    }
    internal sealed class ConfigureWorkPolicyHandler : CommandHandler<ConfigureWorkPolicyCommand>
    {
        protected override Code Check(World world, ConfigureWorkPolicyCommand command)
            => WorkPolicyRules.CheckConfigure(world, command.Player, world.Entities.Get(command.Worker), command.Settings);
        protected override void Apply(World world, ConfigureWorkPolicyCommand command, CommandContext result)
        { WorkPolicyRules.Configure(world, world.Entities.Get(command.Worker), command.Settings); result.Entity = command.Worker; }
    }
    public sealed class ConfigureWorkPolicyGroupCommand : Command
    {
        public EntityId[] Workers;
        public WorkPolicySettings Settings;
        public override void Sync(IStateSync sync)
        {
            int count = sync.Count(Workers?.Length ?? 0);
            var targets = sync.Reading ? new EntityId[count] : Workers == null ? System.Array.Empty<EntityId>() : (EntityId[])Workers.Clone();
            if (!sync.Reading) System.Array.Sort(targets);
            for (int i = 0; i < targets.Length; i++) sync.Entity(ref targets[i]);
            bool present = Settings != null; sync.Bool(ref present);
            if (present) Settings = WorkPolicySettings.Sync(sync, Settings); else if (sync.Reading) Settings = null;
            if (sync.Reading) Workers = targets;
        }
    }
    internal sealed class ConfigureWorkPolicyGroupHandler : CommandHandler<ConfigureWorkPolicyGroupCommand>
    {
        protected override Code Check(World world, ConfigureWorkPolicyGroupCommand command)
            => WorkPolicyRules.PreviewGroup(world, command.Player, command.Workers, command.Settings).Blocker;
        protected override void Apply(World world, ConfigureWorkPolicyGroupCommand command, CommandContext result)
        {
            result.Arg1 = WorkPolicyRules.ConfigureGroup(world, command.Player, command.Workers, command.Settings);
            result.Arg0 = command.Workers.Length;
        }
    }
}
