using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    /// <summary>Hold any owned worker at its actual position, or return it to ordinary engine dispatch.</summary>
    public sealed class SetWorkerHeldCommand : Command
    {
        public EntityId Worker;
        public bool Held;
        public override void Sync(IStateSync sync) { sync.Entity(ref Worker); sync.Bool(ref Held); }
    }
    internal sealed class SetWorkerHeldHandler : CommandHandler<SetWorkerHeldCommand>
    {
        protected override Code Check(World world, SetWorkerHeldCommand command)
            => world.Services.Get<IWorkerControl>().CheckHeld(world, command.Player, command.Worker, command.Held);
        protected override void Apply(World world, SetWorkerHeldCommand command, CommandContext result)
        { world.Services.Get<IWorkerControl>().SetHeld(world, command.Player, command.Worker, command.Held); result.Entity = command.Worker; }
    }
    public sealed class SetWorkerHomeGroupCommand : Command
    {
        public EntityId[] Workers;
        public EntityId Home;
        public override void Sync(IStateSync sync)
        {
            int count = sync.Count(Workers?.Length ?? 0);
            var ids = sync.Reading ? new EntityId[count] : Workers == null ? System.Array.Empty<EntityId>() : (EntityId[])Workers.Clone();
            if (!sync.Reading) System.Array.Sort(ids);
            for (int i = 0; i < ids.Length; i++) sync.Entity(ref ids[i]);
            sync.Entity(ref Home); if (sync.Reading) Workers = ids;
        }
    }
    internal sealed class SetWorkerHomeGroupHandler : CommandHandler<SetWorkerHomeGroupCommand>
    {
        protected override Code Check(World world, SetWorkerHomeGroupCommand command)
            => WorkerHomeContracts.PreviewGroup(world, command.Player, command.Workers, command.Home).Blocker;
        protected override void Apply(World world, SetWorkerHomeGroupCommand command, CommandContext result)
        { result.Arg1 = WorkerHomeContracts.ConfigureGroup(world, command.Player, command.Workers, command.Home); result.Arg0 = command.Workers.Length; }
    }
}
