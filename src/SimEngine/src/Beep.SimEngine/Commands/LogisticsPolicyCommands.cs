using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Commands
{
    public sealed class ConfigureStockPolicyCommand : Command, IEntityControlCommand
    {
        public EntityId Store;
        public Code Port;
        public ResourceEndpointId? Endpoint;
        public ResourceId Resource;
        public EntityId TargetEntity => Store;
        public LogisticsDemandChange Change;
        public override void Sync(IStateSync sync)
        {
            sync.Entity(ref Store); string port = Port.Id; sync.String(ref port); if (sync.Reading) Port = port == null ? Code.None : new Code(port);
            bool bound = Endpoint.HasValue; sync.Bool(ref bound); var endpoint = Endpoint ?? default;
            if (bound) ResourceEndpointId.Sync(sync, ref endpoint); if (sync.Reading) Endpoint = bound ? endpoint : (ResourceEndpointId?)null;
            sync.Resource(ref Resource); bool present = Change != null; sync.Bool(ref present);
            if (present) Change = LogisticsDemandChange.Sync(sync, Change); else if (sync.Reading) Change = null;
        }
    }
    internal sealed class ConfigureStockPolicyHandler : CommandHandler<ConfigureStockPolicyCommand>
    {
        internal static Code Check(World world, PlayerId player, EntityId store, ResourceId resource, LogisticsDemandChange change, Code port, ResourceEndpointId? endpoint)
        {
            world.RequireHealthyUpdate(); Entity entity = world.Entities.Get(store);
            if (entity == null || !entity.Alive || entity.Owner != player || !world.ViewFor(player).CanObserveOwned(entity)) return Codes.NotFound;
            return world.Services.Get<ILogisticsStockPolicies>().CheckStockPolicy(player, store, resource, change, port, endpoint);
        }
        protected override Code Check(World world, ConfigureStockPolicyCommand command)
            => Check(world, command.Player, command.Store, command.Resource, command.Change, command.Port, command.Endpoint);
        protected override void Apply(World world, ConfigureStockPolicyCommand command, CommandContext context)
        {
            Code refusal = world.Services.Get<ILogisticsStockPolicies>().ConfigureStockPolicy(command.Player, command.Store, command.Resource, command.Change, command.Port, command.Endpoint);
            if (!refusal.IsNone) throw new System.InvalidOperationException("stock policy changed after admission: " + refusal); context.Entity = command.Store;
        }
    }
    public sealed class ConfigureOutletPolicyCommand : Command, IEntityControlCommand
    {
        public EntityId Source;
        public Code Port;
        public EntityId TargetEntity => Source;
        public OutletPolicyChange Change;
        public override void Sync(IStateSync sync)
        {
            sync.Entity(ref Source); string port = Port.Id; sync.String(ref port); if (sync.Reading) Port = port == null ? Code.None : new Code(port);
            bool present = Change != null; sync.Bool(ref present);
            if (present) Change = OutletPolicyChange.Sync(sync, Change); else if (sync.Reading) Change = null;
        }
    }
    internal sealed class ConfigureOutletPolicyHandler : CommandHandler<ConfigureOutletPolicyCommand>
    {
        protected override Code Check(World world, ConfigureOutletPolicyCommand command)
            => LogisticsPolicyRules.CheckChange(world, command.Player, world.Entities.Get(command.Source), command.Change, out _, command.Port);
        protected override void Apply(World world, ConfigureOutletPolicyCommand command, CommandContext context)
        {
            Entity entity = world.Entities.Get(command.Source);
            LogisticsPolicyRules.Configure(world, entity, command.Change, command.Port); context.Entity = command.Source;
        }
    }
    public sealed class ConfigureOutletPolicyGroupCommand : Command
    {
        public EntityId[] Sources;
        public Code Port;
        public OutletPolicyChange Change;
        public override void Sync(IStateSync sync)
        {
            int count = sync.Count(Sources?.Length ?? 0);
            var ids = sync.Reading ? new EntityId[count] : Sources == null ? System.Array.Empty<EntityId>() : (EntityId[])Sources.Clone();
            if (!sync.Reading) System.Array.Sort(ids);
            for (int i = 0; i < ids.Length; i++) sync.Entity(ref ids[i]);
            string port = Port.Id; sync.String(ref port); if (sync.Reading) Port = port == null ? Code.None : new Code(port);
            bool present = Change != null; sync.Bool(ref present);
            if (present) Change = OutletPolicyChange.Sync(sync, Change); else if (sync.Reading) Change = null;
            if (sync.Reading) Sources = ids;
        }
    }
    internal sealed class ConfigureOutletPolicyGroupHandler : CommandHandler<ConfigureOutletPolicyGroupCommand>
    {
        protected override Code Check(World world, ConfigureOutletPolicyGroupCommand command)
            => LogisticsPolicyRules.PreviewGroup(world, command.Player, command.Sources, command.Change, command.Port).Blocker;
        protected override void Apply(World world, ConfigureOutletPolicyGroupCommand command, CommandContext context)
        { context.Arg1 = LogisticsPolicyRules.ConfigureGroup(world, command.Player, command.Sources, command.Change, command.Port); context.Arg0 = command.Sources.Length; }
    }
}
