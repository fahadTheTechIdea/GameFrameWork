using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed class HaulTripSelectionEntry
    {
        public EntityId Entity { get; }
        public Code Blocker { get; }
        public HaulTripSettings Settings { get; }
        internal HaulTripSelectionEntry(EntityId entity, Code blocker, HaulTripSettings settings)
        { Entity = entity; Blocker = blocker; Settings = settings; }
    }
    public sealed class HaulTripSelectionPreview
    {
        public Code Blocker { get; }
        public IReadOnlyList<HaulTripSelectionEntry> Entries { get; }
        internal HaulTripSelectionPreview(Code blocker, HaulTripSelectionEntry[] entries)
        { Blocker = blocker; Entries = Array.AsReadOnly((HaulTripSelectionEntry[])entries.Clone()); }
    }
    public static partial class HaulTripPolicyContracts
    {
        public static HaulTripSelectionPreview PreviewGroup(World world, PlayerId player, IReadOnlyList<EntityId> targets, HaulTripSettings settings)
        {
            if (world == null) return new HaulTripSelectionPreview(Codes.InvalidArgument, Array.Empty<HaulTripSelectionEntry>());
            world.RequireHealthyUpdate();
            if (targets == null || targets.Count == 0)
                return new HaulTripSelectionPreview(Codes.InvalidArgument, Array.Empty<HaulTripSelectionEntry>());
            if (!world.Players.IsSeat(player)) return new HaulTripSelectionPreview(Codes.NotSeated, Array.Empty<HaulTripSelectionEntry>());
            var ids = new EntityId[targets.Count]; for (int i = 0; i < ids.Length; i++) ids[i] = targets[i]; Array.Sort(ids);
            for (int i = 0; i < ids.Length; i++)
                if (!ids[i].IsValid || i > 0 && ids[i] == ids[i - 1])
                    return new HaulTripSelectionPreview(Codes.InvalidArgument, Array.Empty<HaulTripSelectionEntry>());
            var entries = new HaulTripSelectionEntry[ids.Length]; Code first = Code.None;
            for (int i = 0; i < ids.Length; i++)
            {
                var entity = world.Entities.Get(ids[i]); HaulTripSettings current = null;
                Code refusal = CheckConfigure(world, player, entity, settings);
                if (refusal != Codes.NotFound) current = Read(world, entity);
                if (!CanDisclose(world, player, current)) current = null;
                entries[i] = new HaulTripSelectionEntry(ids[i], refusal, current);
                if (first.IsNone && !refusal.IsNone) first = refusal;
            }
            return new HaulTripSelectionPreview(first, entries);
        }
        /// <summary>Whole fresh admission before original provider commits; violation faults, never rolls back.</summary>
        public static int ConfigureGroup(World world, PlayerId player, IReadOnlyList<EntityId> targets, HaulTripSettings settings)
        {
            var preview = PreviewGroup(world, player, targets, settings);
            if (!preview.Blocker.IsNone) throw new InvalidOperationException("haul trip selection refused: " + preview.Blocker);
            int changed = 0;
            try
            {
                foreach (var entry in preview.Entries)
                {
                    Entity entity = world.Entities.Get(entry.Entity);
                    if (!settings.Matches(Read(world, entity))) changed++;
                    Configure(world, entity, settings);
                }
                return changed;
            }
            catch (Exception error) { world.LatchFault("logistics.configure-trip-selection", error); throw; }
        }
    }
}
