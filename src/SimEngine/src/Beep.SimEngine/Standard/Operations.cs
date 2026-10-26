using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Commands;

namespace Beep.SimEngine.Standard
{
    public interface IOperationDefinition : INamedCapabilityDefinition
    {
        /// <summary>Actual immutable schema bound by the original content declaration owner.</summary>
        OperationProviderSchema OperationSchema { get; }
        int OperationSchemaVersion { get; }
        /// <summary>Authored controls for a controllable part; null explicitly means read-only.</summary>
        OperationSettings ControlSettings { get; }
    }

    public interface IAutomationSignalDefinition : INamedCapabilityDefinition
    {
        /// <summary>Actual immutable schema bound by the original content declaration owner.</summary>
        AutomationSignalProviderSchema SignalSchema { get; }
        int SignalSchemaVersion { get; }
        IReadOnlyList<AutomationMetricSchema> SignalBindings { get; }
    }

    public interface IOperation : INamedEntityCapability
    {
        /// <summary>Deterministic nonmutating projection of this provider's authoritative state.</summary>
        OperationSnapshot ReadOperation(World world);
    }

    public interface IControllableOperation : IOperation
    {
        OperationControlSnapshot ReadControl();
        Code CheckControl(bool enabled, Fix target);
        void Configure(bool enabled, Fix target);
    }

    public interface IAutomationSignalSource : INamedEntityCapability
    {
        /// <summary>Reads the authored binding; unsupported bindings return typed unavailability.</summary>
        AutomationSignalSnapshot ReadSignal(World world, IAutomationSignalContent signal);
    }

    public static class OperationCodes
    {
        public static readonly Code NotOperation = new Code("operation.not-operation");
        public static readonly Code NotControllable = new Code("operation.not-controllable");
        public static readonly Code Disabled = new Code("operation.disabled");
        public static readonly Code TargetBounds = new Code("operation.target-bounds");
        public static readonly Code SignalUnavailable = new Code("operation.signal-unavailable");
        public static readonly Code SignalNumericRange = new Code("signal.numeric-range");
    }

    /// <summary>One authored parser reused by providers; live settings remain in their existing codecs.</summary>
    public sealed class OperationSettings
    {
        public Code Id { get; }
        public OperationControlSnapshot Initial { get; }
        private OperationSettings(Code id, OperationControlSnapshot initial) { Id = id; Initial = initial; }
        public static OperationSettings Read(JsonReader r)
        {
            if (r == null) throw new ContentException("operation settings must be authored");
            Code id = new Code(r.String("id"));
            if (id.IsNone) throw r.Error("operation identity must be authored");
            try { ContractIdentity.ValidateId(id.Id, "operation"); }
            catch (ArgumentException e) { throw r.Error(e.Message); }
            JsonValue enabled = r.Value("enabled");
            if (enabled == null || enabled.Kind != JsonKind.Bool) throw r.Error("enabled must be an authored boolean");
            Fix target = r.Fix("target"), minimum = r.Fix("minimum"), maximum = r.Fix("maximum");
            Code unit = new Code(r.String("unit"));
            r.End();
            try { return new OperationSettings(id, new OperationControlSnapshot(enabled.Bool, target, minimum, maximum, unit)); }
            catch (ArgumentException e) { throw r.Error(e.Message); }
        }
        public Code Check(Fix target) => target < Initial.Minimum || target > Initial.Maximum ? OperationCodes.TargetBounds : Code.None;
        public OperationControlSnapshot Snapshot(bool enabled, Fix target)
            => new OperationControlSnapshot(enabled, target, Initial.Minimum, Initial.Maximum, Initial.Unit);
    }

    /// <summary>Open file-owned descriptor; distinct from the entity signal capability declaration.</summary>
    public interface IAutomationSignalContent : IContentDefinition
    {
        AutomationSignalKind Kind { get; }
        Code Metric { get; }
        Code Unit { get; }
        IReadOnlyList<AutomationSignalProviderSchema> Providers { get; }
    }

    /// <summary>Native JSON implementation; metadata uses the same parser as independent definitions.</summary>
    public class AutomationSignalDef : Def, IAutomationSignalContent
    {
        private AutomationSignalDeclaration _declaration;
        public AutomationSignalKind Kind => _declaration.Kind;
        public Code Metric => _declaration.Metric;
        public Code Unit => _declaration.Unit;
        public IReadOnlyList<AutomationSignalProviderSchema> Providers => _declaration.Providers;
        protected override void ReadCore(JsonReader reader) => _declaration = AutomationSignalDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content, this);
    }

    public interface IResourceAutomationSignalDefinition : IAutomationSignalContent, IAutomationQuantitySignalDefinition
    {
        IResourceDefinition Resource { get; }
    }

    /// <summary>Explicit resource/unit parameters for the existing typed reader and provider contract.</summary>
    public class ResourceAutomationSignalDef : AutomationSignalDef, IResourceAutomationSignalDefinition
    {
        private string _resource;
        public IResourceDefinition Resource { get; private set; }
        public IUnitDefinition QuantityUnit { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            base.ReadCore(reader); _resource = reader.String("resource");
            if (Kind != AutomationSignalKind.Number) throw reader.Error("resource signals require numeric quantities");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            Resource = content.Def<IResourceContentDefinition>(_resource); QuantityUnit = content.Def<IUnitContentDefinition>(Unit.Id);
            if (Resource.QuantityUnit.Dimension != QuantityUnit.Dimension)
                throw content.Fail("resource signal output unit has an incompatible resource dimension");
            base.ResolveCore(content);
        }
    }

    internal sealed class ConfigureOperationHandler : CommandHandler<ConfigureOperationCommand>
    {
        internal static Code Check(World world, PlayerId owner, EntityId id, Code operationId, bool enabled, Fix target)
        {
            world.RequireHealthyUpdate();
            Entity entity = world.Entities.Get(id);
            if (entity == null || !entity.Alive || entity.Owner != owner || !world.ViewFor(owner).CanObserveOwned(entity)) return Codes.NotFound;
            return EntityObservation.ReadProvider(world, entity, entity, () => {
            if (operationId.IsNone) return Codes.InvalidArgument;
            IControllableOperation operation = entity.Get<IControllableOperation>(operationId);
            if (operation == null) return OperationCodes.NotControllable;
            Code safety = OperationPolicyContracts.CheckControl(world, operation, enabled);
            return safety.IsNone ? OperationContracts.Check(world, operation, enabled, target) : safety;
            }, observeTargetState: true);
        }
        protected override Code Check(World world, ConfigureOperationCommand command)
            => Check(world, command.Player, command.Entity, command.Operation, command.Enabled, command.Target);
        protected override void Apply(World world, ConfigureOperationCommand command, CommandContext result)
        {
            IControllableOperation operation = world.Entities.Get(command.Entity).Get<IControllableOperation>(command.Operation);
            Code refusal = OperationContracts.Commit(world, operation, command.Enabled, command.Target);
            if (!refusal.IsNone) throw new InvalidOperationException("admitted operation control refused: " + refusal);
            OperationPolicyContracts.AcknowledgeControl(world, operation);
            result.Entity = command.Entity;
        }
    }

    internal static class OperationContracts
    {
        private static IOperationDefinition Definition(World world, IOperation operation, out Code identity)
        {
            Entity entity = operation.Entity; identity = operation.CapabilityId;
            if (entity == null || !ReferenceEquals(world.Entities.Get(entity.Id), entity) || identity.IsNone ||
                !ReferenceEquals(entity.Get<IOperation>(identity), operation))
                throw new InvalidOperationException("operation must be the actual live named capability of the executing world");
            return entity.Def.Component<IOperationDefinition>(identity)
                ?? throw new InvalidOperationException("operation has no actual named metadata");
        }
        internal static OperationProviderSchema ReadSchema(World world, IOperation operation)
            => EntityObservation.ReadProvider(world, operation, null, () => Definition(world, operation, out _).OperationSchema
                ?? throw new InvalidOperationException("operation has no actual registered schema"));
        internal static Code ReadIdentity(World world, IOperation operation)
            => EntityObservation.ReadProvider(world, operation, null, () => { Definition(world, operation, out Code identity); return identity; });
        internal static OperationSnapshot Read(World world, IOperation operation)
            => EntityObservation.ReadProvider(world, operation, null, () =>
            {
                Definition(world, operation, out _);
                var value = operation.ReadOperation(world);
                if (!value.IsValid) throw new InvalidOperationException("operation returned malformed facts");
                return value;
            });
        internal static Code Check(World world, IControllableOperation operation, bool enabled, Fix target)
            => EntityObservation.ReadProvider(world, operation, null, () =>
            {
                OperationControlSnapshot control = ReadControl(world, operation);
                if (target < control.Minimum || target > control.Maximum) return OperationCodes.TargetBounds;
                return operation.CheckControl(enabled, target);
            });
        internal static Code Commit(World world, IControllableOperation operation, bool enabled, Fix target)
        {
            Code refusal = Check(world, operation, enabled, target); if (!refusal.IsNone) return refusal;
            Entity entity = operation.Entity; PlayerId owner = entity.Owner; long tick = world.Tick; EntityId next = world.Entities.NextIdentity;
            operation.Configure(enabled, target);
            OperationControlSnapshot applied = ReadControl(world, operation);
            if (applied.Enabled != enabled || applied.Target != target || entity.Owner != owner || world.Tick != tick ||
                world.Entities.NextIdentity != next || !ReferenceEquals(world.Entities.Get(entity.Id), entity))
                throw new InvalidOperationException("operation did not commit the admitted controls");
            return Code.None;
        }
        internal static OperationControlSnapshot ReadControl(World world, IControllableOperation operation)
            => EntityObservation.ReadProvider(world, operation, null, () =>
            {
                var definition = Definition(world, operation, out Code identity);
                var schema = definition.OperationSchema; var settings = definition.ControlSettings;
                if (schema == null || !schema.Controllable || settings == null)
                    throw new InvalidOperationException("controllable operation has no actual declared control schema");
                OperationControlSnapshot value = operation.ReadControl(), declared = settings.Initial;
                if (settings.Id != identity || value.Unit != declared.Unit || value.Minimum != declared.Minimum ||
                    value.Maximum != declared.Maximum || value.Target < declared.Minimum || value.Target > declared.Maximum)
                    throw new InvalidOperationException("operation control facts disagree with their authored schema");
                return value;
            });
    }
}
