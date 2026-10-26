using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    public sealed class ConfigureOperationPolicyCommand : Command
    {
        public EntityId Entity;
        public Code Policy;
        public OperationPolicySettings Settings;
        public override void Sync(IStateSync sync)
        {
            sync.Entity(ref Entity); string id = Policy.Id; sync.String(ref id); Policy = id == null ? Code.None : new Code(id);
            bool present = Settings != null; sync.Bool(ref present);
            if (!present) { if (sync.Reading) Settings = null; return; }
            var settings = Settings; OperationPolicySettings.Sync(sync, ref settings); if (sync.Reading) Settings = settings;
        }
    }
    internal sealed class ConfigureOperationPolicyHandler : CommandHandler<ConfigureOperationPolicyCommand>
    {
        protected override Code Check(World world, ConfigureOperationPolicyCommand command)
            => OperationPolicyContracts.CheckConfigure(world, command.Player, world.Entities.Get(command.Entity), command.Policy, command.Settings);
        protected override void Apply(World world, ConfigureOperationPolicyCommand command, CommandContext context)
        {
            OperationPolicyContracts.Configure(world, world.Entities.Get(command.Entity).Get<IOperationPolicy>(command.Policy), command.Settings);
            context.Entity = command.Entity;
        }
    }
}
