using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Kernel
{
    /// <summary>Ephemeral provider-specific placement intent; never authoritative entity state.</summary>
    public abstract class SpatialPlacement
    {
        protected SpatialPlacement(string space) { Space = space; }
        public string Space { get; }
    }

    /// <summary>Optional placement decisions owned by the selected spatial authority.</summary>
    public interface ISpatialPlacement
    {
        SpatialPlacement CapturePlacement(Entity source);
        /// <summary>Pure validation. Only replacing's occupied geometry may be ignored.</summary>
        Code CheckPlacement(World world, EntityDef target, SpatialPlacement placement, Entity replacing);
        /// <summary>Configure presence before the existing spawn/initialization callbacks.</summary>
        void ConfigurePlacement(Entity entity, SpatialPlacement placement);
    }

    /// <summary>JSON intent decoding/identity belongs to the selected geometry provider, never a host.</summary>
    public interface ISpatialPlacementIntent : ISpatialPlacement
    {
        SpatialPlacement ReadPlacement(JsonValue arguments);
        /// <summary>Exact provider-owned inverse of ReadPlacement; no host geometry conversion.</summary>
        JsonValue WritePlacement(SpatialPlacement placement);
        string PlacementIdentity(SpatialPlacement placement);
    }

    public interface ISpatialConstructionPlacement : ISpatialPlacementIntent
    {
        /// <summary>Pure geometry definition parity; target and authored site must occupy compatible geometry.</summary>
        Code CheckConstructionDefinition(EntityDef target, EntityDef site);
    }

    /// <summary>Optional pure relative geometry authoring on the same selected spatial authority.</summary>
    public interface ISpatialPlacementTemplates : ISpatialPlacementIntent
    {
        void ValidateTemplatePlacement(JsonValue relative);
        JsonValue CaptureTemplatePlacement(JsonValue placement, JsonValue origin);
        JsonValue PlaceTemplatePlacement(JsonValue relative, JsonValue anchor);
    }

    public interface ISpatialRecoveryPlacement : ISpatialPlacementIntent
    {
        /// <summary>Pure compatibility of an authored recovery container with this selected space.</summary>
        Code CheckRecoveryDefinition(EntityDef container);
    }

    /// <summary>Optional pure result placement, owned by the selected spatial authority.</summary>
    public interface ISpatialRollout : ISpatialPlacement
    {
        Code CheckRolloutDefinition(World world, EntityDef producer, EntityDef output);
        Code FindRollout(World world, EntityRolloutRequest request, out SpatialPlacement placement);
        /// <summary>Validate live/planned geometry and, for mobile outputs, actual authored launch-to-exit connectivity.</summary>
        Code CheckRolloutPlacement(World world, EntityRolloutRequest request, SpatialPlacement placement);
    }

    /// <summary>Ephemeral geometric intent, not an entity, reservation receipt or saved state owner.</summary>
    public sealed class PlannedEntityPlacement
    {
        public EntityDef Definition { get; }
        public SpatialPlacement Placement { get; }
        public PlannedEntityPlacement(EntityDef definition, SpatialPlacement placement)
        { Definition = definition; Placement = placement; }
    }

    /// <summary>One current-session rollout query; source may be planned rather than already spawned.</summary>
    public sealed class EntityRolloutRequest
    {
        public Entity Source { get; }
        public EntityDef Producer { get; }
        public SpatialPlacement Origin { get; }
        public EntityDef Output { get; }
        public IReadOnlyList<PlannedEntityPlacement> Unavailable { get; }
        internal EntityRolloutRequest(Entity source, EntityDef producer, SpatialPlacement origin,
            EntityDef output, IReadOnlyList<PlannedEntityPlacement> unavailable)
        {
            Source = source; Producer = producer; Origin = origin; Output = output;
            var prefix = new List<PlannedEntityPlacement> { new PlannedEntityPlacement(producer, origin) };
            prefix.AddRange(unavailable); Unavailable = prefix.AsReadOnly();
        }
    }

    /// <summary>Current-session boundary for provider-owned rollout. No queue, reservation or position store.</summary>
    public static class EntityRollout
    {
        public static readonly Code Unsupported = new Code("core.rollout-unsupported");
        public static readonly Code Pending = new Code("core.rollout-pending");

        public static Code CheckDefinition(World world, EntityDef producer, EntityDef output)
        {
            if (world == null) return Codes.UnknownDef;
            world.RequireHealthyUpdate();
            if (producer == null || output == null
                || !ReferenceEquals(world.Content.Find<EntityDef>(producer.Id), producer)
                || !ReferenceEquals(world.Content.Find<EntityDef>(output.Id), output)) return Codes.UnknownDef;
            return EntityObservation.ReadProvider(world, (object)world.Space ?? producer, null, () => {
                ISpatialPresenceDefinition destination = output.Component<ISpatialPresenceDefinition>();
                if (destination == null) return Code.None;
                ISpatialPresenceDefinition source = producer.Component<ISpatialPresenceDefinition>();
                if (source == null || world.Space == null || source.Space != world.Space.Id || destination.Space != source.Space
                    || !(world.Space is ISpatialRollout provider)) return Unsupported;
                return provider.CheckRolloutDefinition(world, producer, output);
            });
        }

        public static Code Find(World world, Entity producer, EntityDef output, out SpatialPlacement placement)
        {
            placement = null;
            if (world == null) return Codes.NotFound;
            world.RequireHealthyUpdate();
            if (producer == null || !ReferenceEquals(world.Entities.Get(producer.Id), producer)) return Codes.NotFound;
            var result = EntityObservation.ReadProvider(world, (object)world.Space ?? producer, producer, () => {
                Code refusal = CheckDefinition(world, producer.Def, output);
                if (!refusal.IsNone || output.Component<ISpatialPresenceDefinition>() == null)
                    return (refusal, (SpatialPlacement)null);
                var provider = (ISpatialRollout)world.Space;
                return FindCore(world, new EntityRolloutRequest(producer, producer.Def, provider.CapturePlacement(producer),
                    output, Array.Empty<PlannedEntityPlacement>()));
            }, observeTargetState: true);
            placement = result.Item2;
            return result.Item1;
        }

        /// <summary>Pure planning through the same spatial authority, without a fake or temporary entity.</summary>
        public static Code Find(World world, EntityDef producer, SpatialPlacement origin, EntityDef output,
            IReadOnlyList<PlannedEntityPlacement> unavailable, out SpatialPlacement placement)
        {
            placement = null;
            if (world == null) return Codes.NotFound;
            world.RequireHealthyUpdate();
            if (unavailable == null) return Codes.InvalidArgument;
            var request = new EntityRolloutRequest(null, producer, origin, output, unavailable);
            var result = EntityObservation.ReadProvider(world, (object)world.Space ?? (object)producer ?? world, null,
                () => FindCore(world, request));
            placement = result.Item2; return result.Item1;
        }

        private static (Code, SpatialPlacement) FindCore(World world, EntityRolloutRequest request)
        {
            Code refusal = CheckDefinition(world, request.Producer, request.Output);
            if (!refusal.IsNone || request.Output.Component<ISpatialPresenceDefinition>() == null)
                return (refusal, null);
            var provider = (ISpatialRollout)world.Space;
            if (request.Origin == null || request.Origin.Space != world.Space.Id) return (Codes.InvalidArgument, null);
            foreach (var occupied in request.Unavailable)
                if (occupied == null || occupied.Definition == null || occupied.Placement == null
                    || !ReferenceEquals(world.Content.Find<EntityDef>(occupied.Definition.Id), occupied.Definition)
                    || occupied.Definition.Component<ISpatialPresenceDefinition>()?.Space != world.Space.Id
                    || occupied.Placement.Space != world.Space.Id) return (Codes.InvalidArgument, null);
            // A planned producer must be placeable now. An existing producer's operating gates,
            // not a repeated founding/construction placement decision, govern its continued work.
            if (request.Source == null)
            {
                refusal = provider.CheckPlacement(world, request.Producer, request.Origin, null);
                if (!refusal.IsNone) return (refusal, null);
            }
            for (int i = 1; i < request.Unavailable.Count; i++)
            {
                var occupied = request.Unavailable[i];
                refusal = provider.CheckPlacement(world, occupied.Definition, occupied.Placement, null);
                if (!refusal.IsNone) return (refusal, null);
            }
            refusal = provider.FindRollout(world, request, out SpatialPlacement selected);
            if (!refusal.IsNone) return (refusal, null);
            if (selected == null || selected.Space != world.Space.Id) return (Codes.InvalidArgument, null);
            refusal = provider.CheckRolloutPlacement(world, request, selected);
            return (refusal, refusal.IsNone ? selected : null);
        }
    }

    /// <summary>Pure capability policy for carrying state into a replacement definition. For this
    /// capability it replaces removal admission: retained state is not retired. Other capabilities,
    /// resource/promise checks and the guarded replacement commit remain independently required.</summary>
    public interface IReplacementGuard : IEntityCapability
    {
        Code CheckReplacement(World world, EntityDef target);
    }
}
