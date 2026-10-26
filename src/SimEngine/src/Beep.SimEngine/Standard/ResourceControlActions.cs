using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>One reusable resource/unit linker and binding admission, independent of native content types.</summary>
    public abstract class ResourceControlActionDeclaration
    {
        public ControlTargetKind TargetKind => ControlTargetKind.Entity;
        private string _command, _owner, _resource, _unit;
        private IResourceContentDefinition _definition;
        private IUnitContentDefinition _quantityUnit;
        private IReadOnlyList<IContentDefinition> _dependencies;
        public string CommandId => _command;
        public string CommandOwner => _owner;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _dependencies;
        public abstract void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination);
        public abstract Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action);
        public abstract ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination);
        public abstract Command CreateCommand(ControlActionRequest request);
        protected IResourceContentDefinition ResourceDefinition => _definition;
        protected abstract Type CommandType { get; }
        protected abstract string ExpectedCommandOwner { get; }
        protected abstract void ValidateTargets(World world, string id);
        protected abstract Code CheckTarget(World world, ControlBinding target);
        public virtual void Read(JsonReader reader)
        {
            _command = reader.String("command"); _owner = reader.String("owner");
            _resource = reader.String("resource"); _unit = reader.String("unit");
            try
            {
                foreach (var id in new[] { _command, _owner, _resource, _unit }) ContractIdentity.ValidateId(id, "resource action identity");
            }
            catch (ArgumentException ex) { throw reader.Error(ex.Message); }
        }
        public void Resolve(ContentResolver content)
        {
            _definition = content.Def<IResourceContentDefinition>(_resource); _quantityUnit = content.Def<IUnitContentDefinition>(_unit);
            if (!ReferenceEquals(_definition.QuantityUnit, _quantityUnit)) throw content.Fail("resource action requires its resource's exact native unit");
            _dependencies = Array.AsReadOnly<IContentDefinition>(new IContentDefinition[] { _definition, _quantityUnit });
        }
        public void ValidateComposition(World world, string id)
        {
            if (!world.Content.Owns(_definition) || !world.Content.Owns(_quantityUnit)
                || !world.Commands.TryGet(CommandId, out var command) || command.Type != CommandType
                || command.Owner != ExpectedCommandOwner || CommandOwner != command.Owner)
                throw new ContentException(id + ": resource action requires actual native resource/unit and normal command owner");
            ValidateTargets(world, id);
        }
        public Code CheckBinding(World world, PlayerId company, ControlBinding target, ControlAction action)
        {
            if (world.Entities.Get(target.Entity) == null) return Codes.NotFound;
            Code refusal = CheckTarget(world, target); if (!refusal.IsNone) return refusal;
            if (action.Unit != new Code(_quantityUnit.Unit.Id)) return AutomationCodes.UnitMismatch;
            return action.Target < Fix.Zero ? Codes.InvalidArgument : Code.None;
        }
        protected Code CheckReplacementAction(ControlAction action)
        {
            if (action.Unit != new Code(_quantityUnit.Unit.Id)) return AutomationCodes.UnitMismatch;
            return action.Target < Fix.Zero ? Codes.InvalidArgument : Code.None;
        }
        protected Code CheckReplacementResource(World world, EntityDef destination, Code port, ControlAction action, bool outlet)
        {
            Code refusal = CheckReplacementAction(action);
            return refusal.IsNone ? LogisticsPolicyRules.CheckReplacementPort(world, destination, port, ResourceDefinition.Resource, action.Target, outlet, out _) : refusal;
        }
    }
    /// <summary>One-resource reserve intent into the existing outlet policy owner.</summary>
    public sealed class OutletReserveControlActionDeclaration : ResourceControlActionDeclaration
    {
        public override Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action)
        {
            if (!LogisticsPolicyRules.ParsePolicyTarget(target.Capability, LogisticsPolicyRules.OutletPolicyBinding.Id, false, out var port, out _))
                return LogisticsPolicyCodes.NoPolicy;
            var current = source.Get<IOutletPolicy>() as Component;
            ComponentDef retained = null;
            foreach (var part in destination.Components) if (current != null && part.Key == current.Def.Key) retained = part;
            if (current == null || retained == null || !destination.HasCapability<IOutletPolicy>() || !Upgrades.CanRetainState(current.Def, retained))
                return LogisticsPolicyCodes.NoPolicy;
            return CheckReplacementResource(world, destination, port, action, true);
        }
        public override ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination)
            => new ControlBinding(destination.Id, target.Capability);
        protected override Type CommandType => typeof(ConfigureOutletPolicyCommand);
        protected override string ExpectedCommandOwner => LogisticsModule.ModuleId;
        protected override void ValidateTargets(World world, string id) { } // Singular entity contract; no additional service.
        public override void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination)
        {
            destination.Clear();
            Entity target = world.Entities.Get(entity); IOutletPolicy policy = target.Get<IOutletPolicy>();
            if (policy == null) return;
            var names = new List<Code>(); var guard = EntityObservation.CaptureProvider(world, policy);
            try
            {
                policy.CopyOutletPolicyPorts(names); var seen = new HashSet<Code>();
                foreach (Code name in names)
                {
                    if (!seen.Add(name) || LogisticsPolicyRules.Read(world,target,name) == null)
                        throw new InvalidOperationException("outlet policy returned duplicate or missing port intent");
                    IResourceInlet port = LogisticsPolicyRules.PolicyPort(world,target,name);
                    int version = port.Version; var endpoint = port.Endpoint;
                    if (port.Accepts(world.Content,ResourceDefinition.Resource)) destination.Add(LogisticsPolicyRules.OutletPolicyTarget(name));
                    if (port.Version != version || port.Endpoint != endpoint) throw new InvalidOperationException("outlet target observation mutated stock");
                }
            }
            finally { guard.Verify(); }
        }
        protected override Code CheckTarget(World world, ControlBinding target)
        {
            Entity entity = world.Entities.Get(target.Entity);
            var ids = new List<Code>(); CopyTargetIds(world,entity.Owner,entity.Id,ids);
            if (!ids.Contains(target.Capability)) return LogisticsPolicyCodes.NoPolicy;
            return Code.None; // Full current policy, capacity and native quantum belong to the normal handler.
        }
        public override Command CreateCommand(ControlActionRequest request)
        {
            if (!LogisticsPolicyRules.ParsePolicyTarget(request.Target.Capability,LogisticsPolicyRules.OutletPolicyBinding.Id,false,out var port,out _))
                throw new InvalidOperationException("invalid admitted outlet target");
            return new ConfigureOutletPolicyCommand { Source = request.Target.Entity, Port = port,
                Change = OutletPolicyChange.Reserve(ResourceDefinition.Resource,request.Action.Target,request.Action.Enabled) };
        }
    }
    /// <summary>Explicit single stock-level intent into the sole demand owner; no new refill controller.</summary>
    public sealed class StockControlActionDeclaration : ResourceControlActionDeclaration
    {
        public override Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action)
        {
            if (!LogisticsPolicyRules.ParsePolicyTarget(target.Capability, LogisticsPolicyRules.StockPolicyBinding(ResourceDefinition).Id,
                true, out var port, out var endpoint)) return LogisticsPolicyCodes.NoStockPolicy;
            if (port.IsNone) endpoint = ResourceEndpointId.ForEntity(source.Id);
            Code refusal = CheckReplacementAction(action);
            return refusal.IsNone ? world.Services.Get<ILogisticsStockPolicies>().CheckStockTargetReplacement(source, destination,
                ResourceDefinition.Resource, port, endpoint, action.Target) : refusal;
        }
        public override ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination)
        {
            if (!LogisticsPolicyRules.ParsePolicyTarget(target.Capability, LogisticsPolicyRules.StockPolicyBinding(ResourceDefinition).Id,
                true, out var port, out var original)) throw new InvalidOperationException("replacement lost stock target schema");
            if (port.IsNone) original = ResourceEndpointId.ForEntity(source.Id);
            Code refusal = world.Services.Get<ILogisticsStockPolicies>().RebindStockTarget(source, destination,
                ResourceDefinition.Resource, port, original, out var endpoint);
            if (!refusal.IsNone) throw new InvalidOperationException("replacement stock target binding violated admission: " + refusal.Id);
            return new ControlBinding(destination.Id, LogisticsPolicyRules.StockPolicyBinding(ResourceDefinition, port, endpoint));
        }
        public LogisticsStockLevel StockLevel { get; private set; }
        public override void Read(JsonReader reader)
        {
            base.Read(reader);
            string level = reader.String("level");
            switch (level)
            {
                case "minimum": StockLevel = LogisticsStockLevel.Minimum; break;
                case "desired": StockLevel = LogisticsStockLevel.Desired; break;
                case "maximum": StockLevel = LogisticsStockLevel.Maximum; break;
                default: throw reader.Error("stock action requires explicit minimum, desired or maximum level");
            }
        }
        protected override Type CommandType => typeof(ConfigureStockPolicyCommand);
        protected override string ExpectedCommandOwner => LogisticsModule.ModuleId;
        protected override void ValidateTargets(World world, string id)
        {
            if (!world.Services.Has<ILogisticsStockPolicies>()) throw new ContentException(id + ": stock action requires the normal stock-policy service");
        }
        public override void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination)
        {
            destination.Clear();
            var targets = new List<LogisticsStockPolicyTarget>();
            world.Services.Get<ILogisticsStockPolicies>().CopyStockPolicyTargets(entity,ResourceDefinition.Resource,targets);
            foreach (var target in targets) destination.Add(LogisticsPolicyRules.StockPolicyBinding(ResourceDefinition,target.Port,target.Endpoint));
        }
        protected override Code CheckTarget(World world, ControlBinding target)
        {
            var ids = new List<Code>(); CopyTargetIds(world,world.Entities.Get(target.Entity).Owner,target.Entity,ids);
            if (!ids.Contains(target.Capability) ||
                !LogisticsPolicyRules.ParsePolicyTarget(target.Capability,LogisticsPolicyRules.StockPolicyBinding(ResourceDefinition).Id,true,out var port,out var endpoint))
                return LogisticsPolicyCodes.NoStockPolicy;
            if (port.IsNone) endpoint = ResourceEndpointId.ForEntity(target.Entity);
            return world.Services.Get<ILogisticsStockPolicies>().TryReadStockPolicy(target.Entity,ResourceDefinition.Resource,out _,port,endpoint)
                ? Code.None : LogisticsPolicyCodes.NoStockPolicy;
        }
        public override Command CreateCommand(ControlActionRequest request)
        {
            if (!LogisticsPolicyRules.ParsePolicyTarget(request.Target.Capability,LogisticsPolicyRules.StockPolicyBinding(ResourceDefinition).Id,true,out var port,out var endpoint))
                throw new InvalidOperationException("invalid admitted stock target");
            if (port.IsNone) endpoint = ResourceEndpointId.ForEntity(request.Target.Entity);
            return new ConfigureStockPolicyCommand { Store = request.Target.Entity,Port = port,Endpoint = endpoint,Resource = ResourceDefinition.Resource,
                Change = LogisticsDemandChange.Level(StockLevel,request.Action.Target,request.Action.Enabled) };
        }
    }

    public sealed class OutletReserveControlActionPolicyDef : Def, IControlActionPolicy
    {
        private readonly OutletReserveControlActionDeclaration _declaration = new OutletReserveControlActionDeclaration();
        public string CommandId => _declaration.CommandId;
        public string CommandOwner => _declaration.CommandOwner;
        public ControlTargetKind TargetKind => _declaration.TargetKind;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateComposition(World world) => _declaration.ValidateComposition(world, Id);
        public void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination)
            => _declaration.CopyTargetIds(world, company, entity, destination);
        public Code CheckBinding(World world, PlayerId company, ControlBinding target, ControlAction action)
            => _declaration.CheckBinding(world, company, target, action);
        public Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action)
            => _declaration.CheckReplacement(world, company, target, source, destination, action);
        public ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination)
            => _declaration.RebindTarget(world, company, target, source, destination);
        public Command CreateCommand(ControlActionRequest request) => _declaration.CreateCommand(request);
    }

    public sealed class StockControlActionPolicyDef : Def, IControlActionPolicy
    {
        private readonly StockControlActionDeclaration _declaration = new StockControlActionDeclaration();
        public LogisticsStockLevel StockLevel => _declaration.StockLevel;
        public string CommandId => _declaration.CommandId;
        public string CommandOwner => _declaration.CommandOwner;
        public ControlTargetKind TargetKind => _declaration.TargetKind;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateComposition(World world) => _declaration.ValidateComposition(world, Id);
        public void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination)
            => _declaration.CopyTargetIds(world, company, entity, destination);
        public Code CheckBinding(World world, PlayerId company, ControlBinding target, ControlAction action)
            => _declaration.CheckBinding(world, company, target, action);
        public Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action)
            => _declaration.CheckReplacement(world, company, target, source, destination, action);
        public ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination)
            => _declaration.RebindTarget(world, company, target, source, destination);
        public Command CreateCommand(ControlActionRequest request) => _declaration.CreateCommand(request);
    }
}
