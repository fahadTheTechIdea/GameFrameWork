using System;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    public sealed class ConfigureMotionNavigationCommand : Command
    {
        public EntityId Entity;
        public string Profile;
        public override void Sync(IStateSync state) { state.Entity(ref Entity); state.String(ref Profile); }
    }
    internal sealed class ConfigureMotionNavigationHandler : CommandHandler<ConfigureMotionNavigationCommand>
    {
        protected override Code Check(World world, ConfigureMotionNavigationCommand command)
            => MotionNavigationContracts.CheckConfigure(world, command.Player, world.Entities.Get(command.Entity), command.Profile);
        protected override void Apply(World world, ConfigureMotionNavigationCommand command, CommandContext context)
        { MotionNavigationContracts.Configure(world, world.Entities.Get(command.Entity), command.Profile); context.Entity = command.Entity; }
    }
    public sealed class ConfigureMotionNavigationGroupCommand : Command
    {
        public EntityId[] Entities;
        public string Profile;
        public override void Sync(IStateSync state)
        {
            int count = state.Count(Entities?.Length ?? 0);
            var targets = state.Reading ? new EntityId[count] : Entities == null ? Array.Empty<EntityId>() : (EntityId[])Entities.Clone();
            if (!state.Reading) Array.Sort(targets);
            for (int i = 0; i < targets.Length; i++) state.Entity(ref targets[i]);
            state.String(ref Profile); if (state.Reading) Entities = targets;
        }
    }
    internal sealed class ConfigureMotionNavigationGroupHandler : CommandHandler<ConfigureMotionNavigationGroupCommand>
    {
        protected override Code Check(World world, ConfigureMotionNavigationGroupCommand command)
            => MotionNavigationContracts.PreviewGroup(world, command.Player, command.Entities, command.Profile).Blocker;
        protected override void Apply(World world, ConfigureMotionNavigationGroupCommand command, CommandContext context)
        {
            context.Arg1 = MotionNavigationContracts.ConfigureGroup(world, command.Player, command.Entities, command.Profile);
            context.Arg0 = command.Entities.Length;
        }
    }
}
