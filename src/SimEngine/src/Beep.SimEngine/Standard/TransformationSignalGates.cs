using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface ISignalTransformationGateDefinition : INamedCapabilityDefinition
    {
        IReadOnlyList<ITransformationDefinition> Transformations { get; }
        IReadOnlyList<SignalGateCondition> Conditions { get; }
    }

    /// <summary>Sole JSON declaration. Signal providers, not the gate, own physical utility state.</summary>
    public sealed class SignalTransformationGateDeclaration
    {
        private readonly string[] _transformations;
        public Code CapabilityId { get; private set; }
        public IReadOnlyList<ITransformationDefinition> Transformations { get; private set; }
        public IReadOnlyList<SignalGateCondition> Conditions { get; private set; }
        private SignalTransformationGateDeclaration(JsonReader reader)
        {
            CapabilityId = new Code(reader.String("id"));
            try { ContractIdentity.ValidateId(CapabilityId.Id, "transformation gate"); }
            catch (ArgumentException ex) { throw reader.Error(ex.Message); }
            _transformations = reader.Strings("transformations");
            if (_transformations.Length == 0) throw reader.Error("gate requires explicit transformations");
            var transforms = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in _transformations)
            {
                try { ContractIdentity.ValidateId(id, "gated transformation"); }
                catch (ArgumentException ex) { throw reader.Error(ex.Message); }
                if (!transforms.Add(id)) throw reader.Error("duplicate gated transformation");
            }
            Conditions = SignalGateCondition.ReadAll(reader);
        }
        public static SignalTransformationGateDeclaration Read(JsonReader reader)
            => new SignalTransformationGateDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (Transformations != null) throw new InvalidOperationException("transformation gate metadata resolves once");
            var transformations = new List<ITransformationDefinition>();
            foreach (string id in _transformations) transformations.Add(content.Def<ITransformationDefinition>(id));
            transformations.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); Transformations = transformations.AsReadOnly();
            foreach (var condition in Conditions) condition.Resolve(content);
        }
        public void ValidateOwner(EntityDef owner)
            => TransformationGateBindingContracts.ValidateOwner(owner, Transformations, Conditions);
    }

    public sealed class SignalTransformationGateDef : ComponentDef<SignalTransformationGate>, ISignalTransformationGateDefinition
    {
        private SignalTransformationGateDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public IReadOnlyList<ITransformationDefinition> Transformations => _declaration.Transformations;
        public IReadOnlyList<SignalGateCondition> Conditions => _declaration.Conditions;
        protected override void ReadCore(JsonReader reader) => _declaration = SignalTransformationGateDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
    }

    /// <summary>Admission for the original stateless runtime; no second gate evaluator or state owner.</summary>
    public static class TransformationGateBindingContracts
    {
        internal static void ValidateOwner(EntityDef owner, IReadOnlyList<ITransformationDefinition> transformations,
            IReadOnlyList<SignalGateCondition> conditions)
        {
            if (transformations == null || transformations.Count == 0 || conditions == null || conditions.Count == 0)
                throw new ContentException(owner.Id + ": gate requires explicit transformations and conditions");
            foreach (var transformation in transformations)
            {
                bool supported = false;
                foreach (var component in owner.Components)
                    if (component is ITransformerDefinition transformer)
                        foreach (var actual in transformer.Transformations) if (ReferenceEquals(actual, transformation)) supported = true;
                if (!supported) throw new ContentException(owner.Id + ": signal gate names an unsupported transformation '" + transformation.Id + "'");
            }
            foreach (var condition in conditions) condition.Validate(owner);
        }
        internal static T Observe<T>(World world, ISignalTransformationGateDefinition definition, Entity entity,
            Func<IReadOnlyList<ITransformationDefinition>, IReadOnlyList<SignalGateCondition>, T> read)
            => EntityObservation.ReadProvider(world, definition, entity, () => {
                var scope = entity == null ? default : EntityObservation.CaptureProvider(world, entity, entity.Sync, "transformation.gate-metadata");
                try
                {
                    var part = ExecutionGateContracts.ValidateReusable(world, definition, typeof(SignalTransformationGate), 1, entity);
                    var transformations = definition.Transformations; var conditions = definition.Conditions;
                    if (transformations == null || transformations.Count == 0 || conditions == null || conditions.Count == 0)
                        throw new ContentException(part.Owner.Id + ": gate requires explicit transformations and conditions");
                    string previous = null;
                    foreach (var transformation in transformations)
                    {
                        if (transformation == null || !world.Content.Owns(transformation) ||
                            !ReferenceEquals(world.Content.Find<ITransformationDefinition>(transformation.Id), transformation) ||
                            previous != null && string.CompareOrdinal(previous, transformation.Id) >= 0)
                            throw new ContentException(part.Owner.Id + ": gate requires actual unique ordered catalog transformations");
                        previous = transformation.Id;
                    }
                    previous = null;
                    foreach (var condition in conditions)
                    {
                        if (condition == null || condition.Signal == null || !world.Content.Owns(condition.Signal) ||
                            !ReferenceEquals(world.Content.Find<IAutomationSignalContent>(condition.Signal.Id), condition.Signal) ||
                            previous != null && string.CompareOrdinal(previous, condition.Id) >= 0)
                            throw new ContentException(part.Owner.Id + ": gate requires actual unique ordered signal conditions");
                        previous = condition.Id;
                    }
                    ValidateOwner(part.Owner, transformations, conditions);
                    return read(transformations, conditions);
                }
                finally { if (entity != null) scope.Verify(); }
            });
        public static void ValidateBinding(World world, ISignalTransformationGateDefinition definition)
            => Observe(world, definition, null, (transformations, conditions) => true);
        internal static void ValidateComposition(World world)
        {
            foreach (var owner in world.Content.All<EntityDef>()) foreach (var part in owner.Components)
            {
                bool runtime = part.ComponentType == typeof(SignalTransformationGate);
                if (runtime != (part is ISignalTransformationGateDefinition))
                    throw new ContentException(owner.Id + ": original transformation gate requires matching metadata contract");
                if (runtime) ValidateBinding(world, (ISignalTransformationGateDefinition)part);
            }
        }
    }
    public sealed class SignalTransformationGate : Component, ITransformationGate
    {
        private ISignalTransformationGateDefinition Binding => (ISignalTransformationGateDefinition)Def;
        public Code CapabilityId => Binding.CapabilityId;
        public ExecutionGateSnapshot ReadGate(World world, ITransformer processor, ITransformationDefinition transformation)
        {
            if (world == null || Entity == null || !Entity.Alive || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) ||
                !ReferenceEquals(Entity.Get<ITransformationGate>(CapabilityId), this) || processor == null ||
                !ReferenceEquals(processor.Entity, Entity) || transformation == null ||
                !ReferenceEquals(world.Content.Find<ITransformationDefinition>(transformation.Id), transformation))
                throw new ArgumentException("transformation gate requires its actual entity, processor and catalog transformation");
            return TransformationGateBindingContracts.Observe(world, Binding, Entity, (transformations, conditions) => {
                bool applies = false;
                foreach (var definition in transformations) if (ReferenceEquals(definition, transformation)) applies = true;
                return SignalGateCondition.ReadGate(world, Entity, CapabilityId, applies, conditions);
            });
        }
    }
}
