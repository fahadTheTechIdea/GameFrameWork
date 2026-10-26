using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable binding to an existing utility consumer; no consumption state.</summary>
    public interface IUtilitySignalDefinition : IAutomationSignalDefinition
    {
        Code Consumer { get; }
    }
    /// <summary>Sole strict authored selector for the original stateless utility signal component.</summary>
    public sealed class UtilitySignalDeclaration
    {
        public Code CapabilityId { get; }
        public Code Consumer { get; }
        public static IReadOnlyList<AutomationMetricSchema> Metrics { get; } = Array.AsReadOnly(new[]
        {
            new AutomationMetricSchema(new Code("utility.ready"), AutomationSignalKind.Boolean, new Code("unit.boolean")),
            AutomationMetricSchema.Quantity(new Code("utility.requested")),
            AutomationMetricSchema.Quantity(new Code("utility.available")),
            AutomationMetricSchema.Quantity(new Code("utility.consumed"))
        });
        private UtilitySignalDeclaration(JsonReader reader)
        {
            string id = reader.String("id"), consumer = reader.String("consumer");
            try
            {
                ContractIdentity.ValidateId(id, "utility signal");
                ContractIdentity.ValidateId(consumer, "utility consumer");
            }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            CapabilityId = new Code(id); Consumer = new Code(consumer);
        }
        public static UtilitySignalDeclaration Read(JsonReader reader)
            => new UtilitySignalDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void ValidateOwner(EntityDef owner) => UtilitySignalBindingContracts.ValidateConsumer(owner, Consumer);
    }
    /// <summary>Actual aliases and original scoped observations only, never a utility calculator/store.</summary>
    public static class UtilitySignalBindingContracts
    {
        internal static void ValidateConsumer(EntityDef owner, Code identity)
        {
            if (identity.IsNone) throw new ContentException(owner.Id + ": utility signal requires an explicit consumer identity");
            var consumer = owner.Component<INamedCapabilityDefinition>(identity) as ComponentDef;
            if (consumer == null || !ReferenceEquals(consumer.Owner, owner) ||
                !typeof(IUtilityConsumer).IsAssignableFrom(consumer.ComponentType))
                throw new ContentException(owner.Id + ": utility signal requires its actual named consumer '" + identity.Id + "'");
        }
        internal static T Observe<T>(World world, IUtilitySignalDefinition definition, Entity entity, Func<T> read)
            => EntityObservation.ReadProvider(world, definition, entity, () => {
                var part = AutomationSignalContracts.ValidateReusable(world, definition, typeof(UtilitySignal), 1, 1,
                    UtilitySignalDeclaration.Metrics, entity);
                ValidateConsumer(part.Owner, definition.Consumer); return read();
            }, observeTargetState: true, faultPhase: "utility.signal-metadata");
        public static void ValidateBinding(World world, IUtilitySignalDefinition definition)
            => Observe(world, definition, null, () => true);
        internal static void ValidateComposition(World world)
        {
            foreach (var owner in world.Content.All<EntityDef>())
                foreach (var part in owner.Components)
                {
                    bool runtime = part.ComponentType == typeof(UtilitySignal);
                    if (runtime != (part is IUtilitySignalDefinition))
                        throw new ContentException(owner.Id + ": original utility signal requires matching metadata contract");
                    if (runtime) ValidateBinding(world, (IUtilitySignalDefinition)part);
                }
        }
    }
}
