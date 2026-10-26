using System;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Kernel
{
    /// <summary>Ephemeral current-state intent, never a live entity or a saved creation queue.</summary>
    public interface IEntityCreation
    {
        EntityDef Definition { get; }
        PlayerId Owner { get; }
        EntityId Source { get; }
        SpatialPlacement Placement { get; }
        EntityId Identity { get; }
        long Tick { get; }
    }

    internal sealed class EntityCreation : IEntityCreation
    {
        internal readonly World World;
        public EntityDef Definition { get; }
        public PlayerId Owner { get; }
        public EntityId Source { get; }
        public SpatialPlacement Placement { get; }
        public EntityId Identity { get; }
        public long Tick { get; }
        internal EntityCreation(World world, EntityDef definition, PlayerId owner, EntityId source, SpatialPlacement placement)
        { World = world; Definition = definition; Owner = owner; Source = source; Placement = placement; Identity = world.Entities.NextIdentity; Tick = world.Tick; }
    }

    public sealed partial class World
    {
        public static readonly Code StaleCreation = new Code("core.stale-creation");
        public static readonly Code CreationIdentityExhausted = new Code("core.creation-identity-exhausted");

        public Code PrepareCreation(EntityDef definition, PlayerId owner, EntityId source, SpatialPlacement placement, out IEntityCreation creation)
        {
            creation = null;
            var candidate = new EntityCreation(this, definition, owner, source, placement);
            Code refusal = CheckCreation(candidate);
            if (refusal.IsNone) creation = candidate;
            return refusal;
        }

        public Code CheckCreation(IEntityCreation creation)
        {
            if (Fault != null) throw new InvalidOperationException("faulted world cannot admit creation");
            if (!(creation is EntityCreation intent) || !ReferenceEquals(intent.World, this)) return Codes.InvalidArgument;
            if (intent.Tick != Tick || intent.Identity != Entities.NextIdentity) return StaleCreation;
            if (intent.Identity.Value == int.MaxValue) return CreationIdentityExhausted;
            if (intent.Definition == null || !ReferenceEquals(Content.Find<EntityDef>(intent.Definition.Id), intent.Definition)) return Codes.UnknownDef;
            if (intent.Owner != PlayerId.None && !Players.IsSeat(intent.Owner)) return Codes.NotSeated;
            if (intent.Source.Value < 0) return Codes.InvalidArgument;
            Entity source = intent.Source.IsValid ? Entities.Get(intent.Source) : null;
            if (intent.Source.IsValid && source == null) return Codes.NotFound;
            if (source != null && source.Owner != intent.Owner) return Codes.NotOwned;
            ISpatialPresenceDefinition presence = intent.Definition.Component<ISpatialPresenceDefinition>();
            if (presence == null) return intent.Placement == null && !intent.Definition.HasCapability<ISpatialPresence>() ? Code.None : Codes.InvalidArgument;
            if (Space == null || presence.Space != Space.Id || !(Space is ISpatialPlacement provider)) return EntityRollout.Unsupported;
            if (intent.Placement == null || intent.Placement.Space != Space.Id) return Codes.InvalidArgument;
            return EntityObservation.ReadProvider(this, provider, source,
                () => provider.CheckPlacement(this, intent.Definition, intent.Placement, null),
                space: Space, observeTargetState: true);
        }

        /// <summary>Only the existing Spawn/EntityStore lifecycle allocates and initializes the real result.</summary>
        public Entity CommitCreation(IEntityCreation creation)
        {
            Code refusal = CheckCreation(creation);
            if (!refusal.IsNone) throw new InvalidOperationException("creation changed after complete admission: " + refusal);
            Entity result = Spawn(creation.Definition, creation.Owner,
                entity => { if (creation.Placement != null) ((ISpatialPlacement)Space).ConfigurePlacement(entity, creation.Placement); });
            if (result.Id != creation.Identity || !ReferenceEquals(Entities.Get(result.Id), result) ||
                result.Def != creation.Definition || result.Owner != creation.Owner)
                throw new InvalidOperationException("creation violated its actual identity/definition/company");
            return result;
        }
    }
}
