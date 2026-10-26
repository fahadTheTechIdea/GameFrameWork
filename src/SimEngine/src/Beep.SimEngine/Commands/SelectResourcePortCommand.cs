using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    public sealed class SelectResourcePortCommand : Command
    {
        public EntityId Entity;
        public Code Port;
        public ResourceEndpointId Endpoint;
        public override void Sync(IStateSync sync)
        {
            sync.Entity(ref Entity);string port=Port.Id;sync.String(ref port);
            if(sync.Reading)Port=port==null?Code.None:new Code(port);
            ResourceEndpointId.Sync(sync,ref Endpoint);
        }
    }
    internal sealed class SelectResourcePortHandler : CommandHandler<SelectResourcePortCommand>
    {
        protected override Code Check(World world,SelectResourcePortCommand command)
        {
            Entity entity=world.Entities.Get(command.Entity);
            if(entity==null)return Codes.NotFound;
            if(entity.Owner!=command.Player)return Codes.NotOwned;
            if(command.Port.IsNone)return Codes.InvalidArgument;
            return ResourcePortSelectionContracts.Check(world,entity.Get<IResourcePortSelection>(command.Port),command.Endpoint);
        }
        protected override void Apply(World world,SelectResourcePortCommand command,CommandContext context)
        {
            ResourcePortSelectionContracts.Select(world,world.Entities.Get(command.Entity).Get<IResourcePortSelection>(command.Port),command.Endpoint);
            context.Entity=command.Entity;
        }
    }
}

