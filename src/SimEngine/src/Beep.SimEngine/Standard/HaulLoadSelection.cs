using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed class HaulLoadSelectionEntry
    {
        public EntityId Entity { get; }
        public Code Blocker { get; }
        public HaulLoadSettings Settings { get; }
        internal HaulLoadSelectionEntry(EntityId entity, Code blocker, HaulLoadSettings settings)
        { Entity = entity; Blocker = blocker; Settings = settings; }
    }
    public sealed class HaulLoadSelectionPreview
    {
        public Code Blocker { get; }
        public IReadOnlyList<HaulLoadSelectionEntry> Entries { get; }
        internal HaulLoadSelectionPreview(Code blocker, HaulLoadSelectionEntry[] entries)
        { Blocker = blocker; Entries = Array.AsReadOnly((HaulLoadSelectionEntry[])entries.Clone()); }
    }
    public static partial class HaulLoadPolicyContracts
    {
        public static HaulLoadSelectionPreview PreviewGroup(World world, PlayerId player, IReadOnlyList<EntityId> targets, HaulLoadSettings settings)
        {
            if (world == null) return new HaulLoadSelectionPreview(Codes.InvalidArgument, Array.Empty<HaulLoadSelectionEntry>());
            world.RequireHealthyUpdate();
            if (targets == null || targets.Count == 0)
                return new HaulLoadSelectionPreview(Codes.InvalidArgument, Array.Empty<HaulLoadSelectionEntry>());
            if (!world.Players.IsSeat(player)) return new HaulLoadSelectionPreview(Codes.NotSeated, Array.Empty<HaulLoadSelectionEntry>());
            var ids = new EntityId[targets.Count]; for (int i = 0; i < ids.Length; i++) ids[i] = targets[i]; Array.Sort(ids);
            for (int i = 0; i < ids.Length; i++)
                if (!ids[i].IsValid || i > 0 && ids[i] == ids[i - 1])
                    return new HaulLoadSelectionPreview(Codes.InvalidArgument, Array.Empty<HaulLoadSelectionEntry>());
            var entries = new HaulLoadSelectionEntry[ids.Length]; Code first = Code.None;
            for (int i = 0; i < ids.Length; i++)
            {
                var entity = world.Entities.Get(ids[i]); HaulLoadSettings current = null;
                Code refusal = CheckConfigure(world, player, entity, settings);
                if (refusal != Codes.NotFound) current = Read(world, entity);
                if (!CanDisclose(world, player, current)) current = null;
                entries[i] = new HaulLoadSelectionEntry(ids[i], refusal, current);
                if (first.IsNone && !refusal.IsNone) first = refusal;
            }
            return new HaulLoadSelectionPreview(first, entries);
        }
        /// <summary>Whole fresh admission before original provider commits; violation faults, never rolls back.</summary>
        public static int ConfigureGroup(World world, PlayerId player, IReadOnlyList<EntityId> targets, HaulLoadSettings settings)
        {
            var preview = PreviewGroup(world, player, targets, settings);
            if (!preview.Blocker.IsNone) throw new InvalidOperationException("haul load selection refused: " + preview.Blocker);
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
            catch (Exception error) { world.LatchFault("logistics.configure-load-selection", error); throw; }
        }
    }
}
