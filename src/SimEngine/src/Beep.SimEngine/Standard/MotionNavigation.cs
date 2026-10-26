using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    public interface IMotionNavigationDefinition : IComponentDefinition
    {
        string InitialNavigation { get; }
        IReadOnlyList<string> NavigationClasses { get; }
    }
    /// <summary>Optional selection must alias the actual motion owner. Never a second path or position.</summary>
    public interface IMotionNavigation : IMotion
    {
        string ReadNavigation();
        Code CheckNavigation(World world, string profile);
        void ConfigureNavigation(World world, string profile);
    }
    public sealed class MotionNavigationSnapshot
    {
        public string Profile { get; }
        public IReadOnlyList<string> Allowed { get; }
        internal MotionNavigationSnapshot(string profile, IReadOnlyList<string> allowed)
        {
            Profile = profile; var copy = new string[allowed.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = allowed[i];
            Array.Sort(copy, StringComparer.Ordinal); Allowed = Array.AsReadOnly(copy);
        }
    }
    public static class MotionNavigationCodes
    {
        public static readonly Code Unsupported = new Code("motion.no-navigation-selection"),
            NotAllowed = new Code("motion.navigation-not-allowed"), Missing = new Code("motion.navigation-missing");
    }
    /// <summary>Detached selection facts; never a second fleet/member/policy owner.</summary>
    public sealed class MotionNavigationSelectionEntry
    {
        public EntityId Entity { get; }
        public Code Blocker { get; }
        public MotionNavigationSnapshot Selection { get; }
        internal MotionNavigationSelectionEntry(EntityId entity, Code blocker, MotionNavigationSnapshot selection)
        { Entity = entity; Blocker = blocker; Selection = selection; }
    }
    public sealed class MotionNavigationSelectionPreview
    {
        public Code Blocker { get; }
        public IReadOnlyList<MotionNavigationSelectionEntry> Entries { get; }
        internal MotionNavigationSelectionPreview(Code blocker, MotionNavigationSelectionEntry[] entries)
        { Blocker = blocker; Entries = Array.AsReadOnly((MotionNavigationSelectionEntry[])entries.Clone()); }
    }
    public static class MotionNavigationContracts
    {
        private static IMotionNavigation Provider(World world, Entity entity)
        {
            if (world == null || entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity))
                throw new ArgumentException("navigation selection needs actual live session motion");
            var provider = entity.Get<IMotionNavigation>();
            if (provider == null) return null;
            if (!ReferenceEquals(provider.Entity, entity) || !ReferenceEquals(provider, entity.Get<IMotion>()) ||
                entity.Def.Component<IMotionNavigationDefinition>() == null) throw new InvalidOperationException("navigation selection must alias actual declared motion owner");
            return provider;
        }
        private static bool Contains(IReadOnlyList<string> values, string profile)
        { for (int i = 0; i < values.Count; i++) if (values[i] == profile) return true; return false; }
        internal static void ValidateDefinition(IMotionNavigationDefinition definition)
            => ValidateChoices(definition.NavigationClasses, definition.InitialNavigation);
        internal static void ValidateChoices(IReadOnlyList<string> classes, string initial)
        {
            if (classes == null || classes.Count == 0)
                throw new ContentException("navigation selection requires explicit nonempty classes");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in classes)
            {
                try { ContractIdentity.ValidateId(id, "navigation class"); }
                catch (ArgumentException error) { throw new ContentException(error.Message); }
                if (!seen.Add(id)) throw new ContentException("duplicate navigation class: " + id);
            }
            if (!Contains(classes, initial))
                throw new ContentException("initial navigation must be explicitly listed");
        }
        internal static void ValidateDefinitions(World world)
        {
            foreach (EntityDef entity in world.Content.All<EntityDef>())
            {
                foreach (ComponentDef part in entity.Components)
                {
                    if (typeof(IMotionNavigation).IsAssignableFrom(part.ComponentType) != (part is IMotionNavigationDefinition))
                        throw new ContentException(entity.Id + ": navigation metadata must belong to its actual motion provider");
                    if (typeof(ITileMover).IsAssignableFrom(part.ComponentType) != (part is ITileMoverDefinition))
                        throw new ContentException(entity.Id + ": reusable tile mover metadata/provider must be paired");
                    if (part is ITileMoverDefinition tiles)
                        EntityObservation.ReadProvider(world, part, null, () => {
                            var presence = entity.Component<ITilePresenceDefinition>();
                            if (!tiles.TilesPerHour.IsPositive || presence == null || !presence.Movable ||
                                !entity.HasCapability<ITilePresence>() || tiles.SpaceDefinition == null ||
                                !world.Content.Owns(tiles.SpaceDefinition) ||
                                !ReferenceEquals(world.Content.Get<ITileSpaceDefinition>(presence.Space), tiles.SpaceDefinition) ||
                                tiles.States?.Definitions.Count != 2)
                                throw new ContentException(entity.Id + ": reusable tile mover requires actual speed, mobile space and state modes");
                            foreach (string mode in new[] { "moving", "stopped" })
                                if (!tiles.States.Definitions.TryGetValue(mode, out var state) || state == null ||
                                    !world.Content.Owns(state) || !ReferenceEquals(world.Content.Get<IEntityStateDefinition>(state.Id), state))
                                    throw new ContentException(entity.Id + ": reusable tile mover requires actual owned state " + mode);
                            return true;
                        });
                }
                var definition = entity.Component<IMotionNavigationDefinition>();
                if (entity.HasCapability<IMotionNavigation>() != (definition != null))
                    throw new ContentException(entity.Id + ": navigation definition/provider must be declared together");
                if (definition != null)
                {
                    if (!entity.HasCapability<IMotion>()) throw new ContentException(entity.Id + ": navigation selection requires actual motion");
                    EntityObservation.ReadProvider(world, definition, null, () => { ValidateDefinition(definition); return true; });
                }
                IMobilityDefinition mobility = entity.Component<IMobilityDefinition>();
                if (mobility != null)
                    EntityObservation.ReadProvider(world, mobility, null, () => {
                        ITilePresenceDefinition presence = entity.Component<ITilePresenceDefinition>();
                        if (presence == null) throw new ContentException(entity.Id + ": tile mobility requires authored tile presence");
                        if (presence.Space != world.Settings.Space) return true;
                        if (!(world.Space is ITileNavigationSpace) || !world.Services.TryGet<NavService>(out var navigation))
                            throw new ContentException(entity.Id + ": selected tile mobility requires its actual tile space and navigation provider");
                        if (navigation.Find(mobility.MovementClass) == null)
                            throw new ContentException(entity.Id + ": unregistered movement class '" + mobility.MovementClass + "'");
                        if (mobility is IMotionNavigationDefinition choices)
                            foreach (string id in choices.NavigationClasses)
                                if (navigation.Find(id) == null) throw new ContentException(entity.Id + ": unregistered allowed navigation class '" + id + "'");
                        return true;
                    });
            }
        }
        public static MotionNavigationSnapshot Read(World world, Entity entity)
        {
            var provider = Provider(world, entity); if (provider == null) return null;
            return EntityObservation.ReadProvider(world, provider, entity, () => {
                var definition = entity.Def.Component<IMotionNavigationDefinition>(); ValidateDefinition(definition);
                string selected = provider.ReadNavigation();
                if (!Contains(definition.NavigationClasses, selected)) throw new InvalidOperationException("motion selected an undeclared navigation class");
                return new MotionNavigationSnapshot(selected, definition.NavigationClasses);
            });
        }
        public static Code CheckConfigure(World world, PlayerId player, Entity entity, string profile)
        {
            if (world == null) return Codes.NotFound;
            world.RequireHealthyUpdate();
            if (entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity) ||
                entity.Owner != player || !world.ViewFor(player).CanObserveOwned(entity)) return Codes.NotFound;
            return CheckSelection(world, entity, profile);
        }
        private static Code CheckSelection(World world, Entity entity, string profile)
        {
            if (world == null || entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity)) return Codes.NotFound;
            var provider = Provider(world, entity); if (provider == null) return MotionNavigationCodes.Unsupported;
            var current = Read(world, entity);
            if (string.IsNullOrWhiteSpace(profile) || !Contains(current.Allowed, profile)) return MotionNavigationCodes.NotAllowed;
            return EntityObservation.ReadProvider(world, provider, entity, () => provider.CheckNavigation(world, profile));
        }
        public static void Configure(World world, Entity entity, string profile)
        {
            Code refusal = CheckSelection(world, entity, profile); if (!refusal.IsNone) throw new InvalidOperationException("navigation selection refused: " + refusal);
            var before = Read(world, entity); if (before.Profile == profile) return;
            int version = entity.Version; var owner = entity.Owner; long step = world.Clock.StepIndex; var next = world.Entities.NextIdentity;
            MoveStatus status = MotionExecutionContracts.ReadStatus(world, entity);
            entity.Get<IMotionNavigation>().ConfigureNavigation(world, profile);
            MoveStatus after = MotionExecutionContracts.ReadStatus(world, entity);
            if (Read(world, entity).Profile != profile || entity.Version == version || entity.Owner != owner || world.Clock.StepIndex != step ||
                world.Entities.NextIdentity != next || after != status && !(status == MoveStatus.Unreachable && after == MoveStatus.Moving))
                throw new InvalidOperationException("navigation provider failed exact selection/intent commit");
        }
        public static MotionNavigationSelectionPreview PreviewGroup(World world, PlayerId player, IReadOnlyList<EntityId> targets, string profile)
        {
            if (world == null) return new MotionNavigationSelectionPreview(Codes.InvalidArgument, Array.Empty<MotionNavigationSelectionEntry>());
            world.RequireHealthyUpdate();
            if (!player.IsValid || targets == null || targets.Count == 0)
                return new MotionNavigationSelectionPreview(Codes.InvalidArgument, Array.Empty<MotionNavigationSelectionEntry>());
            if (!world.Players.IsSeat(player)) return new MotionNavigationSelectionPreview(Codes.NotSeated, Array.Empty<MotionNavigationSelectionEntry>());
            var ids = new EntityId[targets.Count]; for (int i = 0; i < ids.Length; i++) ids[i] = targets[i]; Array.Sort(ids);
            for (int i = 0; i < ids.Length; i++)
                if (!ids[i].IsValid || i > 0 && ids[i] == ids[i - 1])
                    return new MotionNavigationSelectionPreview(Codes.InvalidArgument, Array.Empty<MotionNavigationSelectionEntry>());
            var entries = new MotionNavigationSelectionEntry[ids.Length]; Code first = Code.None;
            for (int i = 0; i < ids.Length; i++)
            {
                Entity entity = world.Entities.Get(ids[i]); MotionNavigationSnapshot selection = null;
                Code blocker = CheckConfigure(world, player, entity, profile);
                if (blocker != Codes.NotFound) selection = Read(world, entity);
                entries[i] = new MotionNavigationSelectionEntry(ids[i], blocker, selection);
                if (first.IsNone && !blocker.IsNone) first = blocker;
            }
            return new MotionNavigationSelectionPreview(first, entries);
        }
        /// <summary>Preflight every member before ordinary refusal; provider commit violations fail-stop, not rollback.</summary>
        public static int ConfigureGroup(World world, PlayerId player, IReadOnlyList<EntityId> targets, string profile)
        {
            var preview = PreviewGroup(world, player, targets, profile);
            if (!preview.Blocker.IsNone) throw new InvalidOperationException("navigation selection refused: " + preview.Blocker);
            int changed = 0;
            try
            {
                foreach (var entry in preview.Entries)
                {
                    if (entry.Selection.Profile != profile) changed++;
                    Configure(world, world.Entities.Get(entry.Entity), profile);
                }
                return changed;
            }
            catch (Exception error) { world.LatchFault("motion.configure-selection", error); throw; }
        }
    }
}
