using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public interface IRemoteSignalDefinition : IAutomationSignalDefinition
    {
        IReadOnlyList<EntityDef> ProviderDefinitions { get; }
        Code Source { get; }
    }
    public interface IRemoteSignalBinding : INamedEntityCapability
    {
        EntityId Target { get; }
        Code CheckTarget(World world, EntityId target);
        void BindTarget(World world, EntityId target);
    }
    public static class RemoteSignalCodes
    {
        public static readonly Code Unbound = new Code("signal.remote-unbound"),
            Incompatible = new Code("signal.remote-incompatible");
    }
    /// <summary>One strict JSON declaration shared by native and independent metadata implementations.</summary>
    public sealed class RemoteSignalDeclaration
    {
        private readonly string[] _providers;
        public Code CapabilityId { get; }
        public IReadOnlyList<EntityDef> ProviderDefinitions { get; private set; }
        public Code Source { get; }
        private RemoteSignalDeclaration(JsonReader reader)
        {
            string id = reader.String("id"), source = reader.String("source");
            if (!reader.Has("providers")) throw reader.Error("explicit compatible providers required");
            _providers = reader.Strings("providers");
            if (_providers.Length == 0) throw reader.Error("remote providers cannot be empty");
            Array.Sort(_providers, StringComparer.Ordinal);
            try
            {
                ContractIdentity.ValidateId(id, "remote signal"); ContractIdentity.ValidateId(source, "remote source");
                for (int i = 0; i < _providers.Length; i++)
                {
                    ContractIdentity.ValidateId(_providers[i], "remote provider definition");
                    if (i > 0 && _providers[i - 1] == _providers[i]) throw reader.Error("duplicate remote provider");
                }
            }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            CapabilityId = new Code(id); Source = new Code(source);
            JsonValue target = reader.Value("target");
            if (target == null || target.Kind != JsonKind.Null) throw reader.Error("remote signals require target:null; live binding uses player commands");
        }
        public static RemoteSignalDeclaration Read(JsonReader reader) => new RemoteSignalDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            var providers = new List<EntityDef>(); foreach (string id in _providers) providers.Add(content.Def<EntityDef>(id));
            ProviderDefinitions = providers.AsReadOnly();
        }
    }
    /// <summary>Authored utility metrics forwarded by the same generic runtime; consumes nothing itself.</summary>
    public sealed class RemoteUtilitySignalDef : ComponentDef<RemoteSignal>, IRemoteSignalDefinition
    {
        private RemoteSignalDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public IReadOnlyList<EntityDef> ProviderDefinitions => _declaration.ProviderDefinitions;
        public Code Source => _declaration.Source;
        public int SignalSchemaVersion => 1;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => UtilitySignalDeclaration.Metrics;
        protected override void ReadCore(JsonReader reader) => _declaration = RemoteSignalDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => RemoteSignalContracts.ValidateProviders(this);
    }
    public static class RemoteSignalContracts
    {
        internal static void ValidateProviders(IRemoteSignalDefinition definition)
        {
            if (definition.Source.IsNone || definition.ProviderDefinitions == null || definition.ProviderDefinitions.Count == 0)
                throw new ContentException("remote signal requires explicit source/providers");
            var seen = new HashSet<EntityDef>();
            foreach (EntityDef provider in definition.ProviderDefinitions)
            {
                if (provider == null || !seen.Add(provider)) throw new ContentException("remote signal requires unique actual providers");
                var source = provider.Component<IAutomationSignalDefinition>(definition.Source);
                if (source == null || typeof(IRemoteSignalBinding).IsAssignableFrom(((ComponentDef)source).ComponentType))
                    throw new ContentException("remote signal requires a declared non-remote source");
                foreach (var expected in definition.SignalBindings)
                    if (!source.SignalSchema.TryGet(expected.Metric, out var actual) || actual.Kind != expected.Kind ||
                        actual.Unit != expected.Unit || actual.UsesAuthoredUnit != expected.UsesAuthoredUnit)
                        throw new ContentException("incompatible remote metric declaration");
            }
        }
        public static bool AcceptsProvider(IRemoteSignalDefinition definition, EntityDef provider)
        { foreach (EntityDef allowed in definition.ProviderDefinitions) if (ReferenceEquals(allowed, provider)) return true; return false; }
        internal static T Observe<T>(World world, IRemoteSignalDefinition definition, Entity entity, Func<T> read)
            => EntityObservation.ReadProvider(world, definition, entity, () => {
                AutomationSignalContracts.ValidateReusable(world, definition, typeof(RemoteSignal), 1, 1, definition.SignalBindings, entity);
                ValidateProviders(definition);
                foreach (var provider in definition.ProviderDefinitions)
                    if (!world.Content.Owns(provider) || !ReferenceEquals(world.Content.Find<EntityDef>(provider.Id), provider))
                        throw new ContentException("remote provider must be its actual frozen content alias");
                return read();
            });
        public static void ValidateBinding(World world, IRemoteSignalDefinition definition) => Observe(world, definition, null, () => true);
        internal static void ValidateComposition(World world)
        {
            foreach (var owner in world.Content.All<EntityDef>()) foreach (var part in owner.Components)
            {
                bool runtime = part.ComponentType == typeof(RemoteSignal);
                if (runtime != (part is IRemoteSignalDefinition)) throw new ContentException(owner.Id + ": remote runtime/metadata contract mismatch");
                if (runtime) ValidateBinding(world, (IRemoteSignalDefinition)part);
            }
        }
    }
    /// <summary>Sole remote reference owner. All values come from the actual source's existing signal reader.</summary>
    public sealed class RemoteSignal : Component, IAutomationSignalSource, IRemoteSignalBinding, IEntityReplacementReferenceOwner
    {
        private EntityId _target = EntityId.None;
        private IRemoteSignalDefinition Binding => Def as IRemoteSignalDefinition
            ?? throw new InvalidOperationException("remote signal requires its actual definition contract");
        public Code CapabilityId => Binding.CapabilityId;
        public EntityId Target => _target;
        public Code CheckTarget(World world, EntityId target)
        {
            RequireActual(world);
            return RemoteSignalContracts.Observe(world, Binding, Entity, () => CheckTargetCore(world, target));
        }
        private void RequireActual(World world)
        {
            world.RequireHealthyUpdate();
            if (Entity == null || !Entity.Alive || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) ||
                !ReferenceEquals(Entity.Get<IRemoteSignalBinding>(CapabilityId), this)) throw new InvalidOperationException("remote signal is not its actual live binding");
        }
        private Code CheckTargetCore(World world, EntityId target)
        {
            if (target == EntityId.None) return Code.None;
            if (!world.Players.IsSeat(Entity.Owner)) return Codes.NotSeated;
            Entity provider = world.Entities.Get(target);
            if (provider == null || !provider.Alive || provider.Owner != Entity.Owner) return Codes.NotFound;
            return RemoteSignalContracts.AcceptsProvider(Binding, provider.Def) && provider.Get<IAutomationSignalSource>(Binding.Source) != null
                ? Code.None : RemoteSignalCodes.Incompatible;
        }
        public void BindTarget(World world, EntityId target)
        {
            Code refusal = CheckTarget(world, target);
            if (!refusal.IsNone) throw new InvalidOperationException("remote signal binding refused: " + refusal);
            if (_target == target) return;
            _target = target; Entity.Touch();
        }
        public AutomationSignalSnapshot ReadSignal(World world, IAutomationSignalContent signal)
        {
            if (world == null || signal == null || !ReferenceEquals(world.Content.Find<IAutomationSignalContent>(signal.Id), signal))
                throw new ArgumentException("remote read requires actual session/content");
            RequireActual(world);
            // Live gates also run inside visibility evaluation. Do not recursively enter that visibility owner.
            // Link admission checks player disclosure; ongoing telemetry is scoped by the unchanged actual owner.
            // The original typed signal reader still validates both schemas and observes the source callback.
            return RemoteSignalContracts.Observe(world, Binding, Entity, () => {
                if (_target == EntityId.None) return signal.Unavailable(RemoteSignalCodes.Unbound);
                Code refusal = CheckTargetCore(world, _target);
                return refusal.IsNone ? signal.ReadFrom(world, world.Entities.Get(_target).Get<IAutomationSignalSource>(Binding.Source)) : signal.Unavailable(refusal);
            });
        }
        public override void Sync(IStateSync sync)
        {
            sync.Entity(ref _target);
            if (_target != EntityId.None && !_target.IsValid) throw new CorruptStateException("invalid remote signal target identity");
        }
        public Code CheckReferenceReplacement(World world, Entity source, EntityDef destination)
        {
            world.RequireHealthyUpdate();
            if (_target != source.Id) return Code.None;
            return RemoteSignalContracts.Observe(world, Binding, Entity,
                () => RemoteSignalContracts.AcceptsProvider(Binding, destination) ? Code.None : RemoteSignalCodes.Incompatible);
        }
        public void ReplaceEntityReferences(World world, Entity source, Entity destination)
        { if (_target == source.Id) { _target = destination.Id; Entity.Touch(); } }
    }
    public sealed class BindRemoteSignalCommand : Command, IEntityControlCommand
    {
        public EntityId Entity, Provider;
        public Code SignalSource;
        public EntityId TargetEntity => Entity;
        public override void Sync(IStateSync sync)
        { sync.Entity(ref Entity); sync.Entity(ref Provider); string id = SignalSource.Id; sync.String(ref id); SignalSource = id == null ? Code.None : new Code(id); }
    }
    internal sealed class BindRemoteSignalHandler : CommandHandler<BindRemoteSignalCommand>
    {
        protected override Code Check(World world, BindRemoteSignalCommand command)
        {
            world.RequireHealthyUpdate(); Entity entity = world.Entities.Get(command.Entity);
            if (entity == null || !entity.Alive || entity.Owner != command.Player || !world.ViewFor(command.Player).CanObserveOwned(entity)) return Codes.NotFound;
            if (command.SignalSource.IsNone) return Codes.InvalidArgument;
            if (command.Provider != EntityId.None && !world.ViewFor(command.Player).TryGetEntity(command.Provider, out _)) return Codes.NotFound;
            var binding = entity.Get<IRemoteSignalBinding>(command.SignalSource);
            return binding == null ? RemoteSignalCodes.Incompatible : EntityObservation.ReadProvider(world, binding, entity,
                () => binding.CheckTarget(world, command.Provider), observeTargetState: true);
        }
        protected override void Apply(World world, BindRemoteSignalCommand command, CommandContext result)
        { world.Entities.Get(command.Entity).Get<IRemoteSignalBinding>(command.SignalSource).BindTarget(world, command.Provider); result.Entity = command.Entity; }
    }
    public readonly struct RemoteSignalBindingSnapshot
    {
        public EntityId Entity { get; }
        public Code Source { get; }
        public EntityId Provider { get; }
        public Code Blocker { get; }
        public RemoteSignalBindingSnapshot(EntityId entity, Code source, EntityId provider, Code blocker)
        { Entity = entity; Source = source; Provider = provider; Blocker = blocker; }
    }
    public sealed class RemoteSignalsView
    {
        private readonly World _world; private readonly PlayerId _viewer;
        internal RemoteSignalsView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        public Code ReadBinding(EntityId entityId, Code source, out RemoteSignalBindingSnapshot snapshot)
        {
            snapshot = default; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(entityId);
            if (entity == null || !entity.Alive || _viewer != PlayerId.None &&
                (entity.Owner != _viewer || !_world.ViewFor(_viewer).TryGetEntity(entityId, out _))) return Codes.NotFound;
            var binding = entity.Get<IRemoteSignalBinding>(source); if (binding == null) return Codes.NotFound;
            snapshot = EntityObservation.ReadProvider(_world, binding, entity, () => new RemoteSignalBindingSnapshot(entityId, source,
                binding.Target, binding.Target == EntityId.None ? RemoteSignalCodes.Unbound : binding.CheckTarget(_world, binding.Target)), observeTargetState: true);
            return Code.None;
        }
    }
    public sealed class RemoteSignalsModule : GameModule
    {
        public const string ModuleId = "sim.remote-signals";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(OperationsModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types) => types.Component<RemoteUtilitySignalDef>("remote_utility_signal", 1);
        public override void Compose(WorldComposer composer)
        {
            RemoteSignalContracts.ValidateComposition(composer.World);
            composer.Commands.Register("sim.bind-remote-signal", new BindRemoteSignalHandler());
            composer.AddViewFeature("sim.remote-signals", 1, (world, viewer) => new RemoteSignalsView(world, viewer));
        }
    }
}
