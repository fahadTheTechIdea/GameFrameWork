using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>One observation boundary for pure plan admission; no plan state or evaluator.</summary>
    internal static class PlanObservation
    {
        internal static Code Intent(IPlayerActivityDefinition activity, World world, PlayerId company, Entity target)
            => EntityObservation.ReadProvider(world, activity, target,
                () => activity.CheckIntent(world, company, target), space: world.Space, observeTargetState: true);

        internal static string Identity(World world, IPlanDefinition plan, JsonValue arguments)
            => Read(world, plan, arguments, references => plan.IntentIdentity(world, arguments));
        internal static Code Submission(World world, IPlanDefinition plan, PlayerId company, JsonValue arguments)
            => Read(world, plan, arguments, references => plan.CheckSubmission(world, company, arguments));
        internal static Code Activation(World world, IPlanDefinition plan, PlayerId company, JsonValue arguments)
            => Read(world, plan, arguments, references => plan.CheckActivation(world, company, arguments));
        internal static IReadOnlyList<EntityId> References(World world, IPlanDefinition plan, JsonValue arguments)
            => Read(world, plan, arguments, references => references);

        internal static IActivityDefinition Binding(World world, IPlanDefinition plan, EntityId target)
            => EntityObservation.ReadProvider(world, plan, world.Entities.Get(target),
                () => plan.BindExecution(world, target), space: world.Space, observeTargetState: true);

        internal static string ExecutionIdentity(World world, IPlanDefinition plan, Order order)
            => EntityObservation.ReadProvider(world, plan, world.Entities.Get(order.Target), () =>
            {
                IActivityDefinition binding = order.Plan;
                var record = EntityObservation.CaptureProvider(world, order,
                    sync => world.Services.Get<WorkBoard>().SyncOrder(sync, order));
                string identity = plan.ExecutionIdentity(world, order);
                record.Verify();
                if (!ReferenceEquals(binding, order.Plan))
                    throw new InvalidOperationException("execution identity changed its actual bound plan");
                return identity;
            }, space: world.Space, observeTargetState: true);

        private static T Read<T>(World world, IPlanDefinition plan, JsonValue arguments, Func<IReadOnlyList<EntityId>, T> read)
        {
            world.RequireHealthyUpdate();
            var facts = EntityObservation.Capture(world);
            var provider = EntityObservation.CaptureProvider(world, plan);
            var spatial = EntityObservation.CaptureSpace(world, world.Space);
            var entities = new List<(EntityObservation.Scope Facts, EntityObservation.ProviderScope State)>();
            void Verify()
            {
                world.RequireHealthyUpdate(); facts.Verify(); provider.Verify(); spatial.Verify();
                foreach (var entity in entities) { entity.Facts.Verify(); entity.State.Verify(); }
            }
            try
            {
                // Reference schema belongs to this provider, never inferred from numeric JSON fields.
                var source = plan.ReadEntityReferences(world, arguments)
                    ?? throw new InvalidOperationException("plan must declare its complete reference set");
                var unique = new SortedSet<EntityId>();
                foreach (var reference in source)
                    if (!reference.IsValid || !unique.Add(reference))
                        throw new InvalidOperationException("plan references must be valid and unique");
                var references = new List<EntityId>(unique).AsReadOnly();
                foreach (var reference in references)
                {
                    Entity entity = world.Entities.Get(reference);
                    // Missing live targets remain the admission provider's typed refusal, not a fallback entity.
                    if (entity != null) entities.Add((EntityObservation.Capture(world, entity),
                        EntityObservation.CaptureProvider(world, entity, entity.Sync)));
                }
                T value = read(references); Verify(); return value;
            }
            catch (ContentException)
            {
                // Malformed user arguments are expected only if every observed scope stayed pure.
                try { Verify(); }
                catch (Exception error) { world.LatchFault("observation.provider", error); throw; }
                throw;
            }
            catch (Exception error) { world.LatchFault("observation.provider", error); throw; }
        }
    }
}
