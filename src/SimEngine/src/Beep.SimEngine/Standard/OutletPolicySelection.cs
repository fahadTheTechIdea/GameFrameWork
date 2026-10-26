using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed class OutletPolicySelectionEntry
    {
        public EntityId Entity { get; }
        public Code Blocker { get; }
        public OutletPolicySettings Current { get; }
        public OutletPolicySettings Next { get; }
        internal OutletPolicySelectionEntry(EntityId entity, Code blocker, OutletPolicySettings current, OutletPolicySettings next)
        { Entity = entity; Blocker = blocker; Current = current; Next = next; }
    }
    public sealed class OutletPolicySelectionPreview
    {
        public Code Blocker { get; }
        public IReadOnlyList<OutletPolicySelectionEntry> Entries { get; }
        internal OutletPolicySelectionPreview(Code blocker, OutletPolicySelectionEntry[] entries)
        { Blocker = blocker; Entries = Array.AsReadOnly((OutletPolicySelectionEntry[])entries.Clone()); }
    }
    public static partial class LogisticsPolicyRules
    {
        public static OutletPolicySelectionPreview PreviewGroup(World world, PlayerId player, IReadOnlyList<EntityId> targets, OutletPolicyChange change, Code port = default)
        {
            if (world == null) return new OutletPolicySelectionPreview(Codes.InvalidArgument, Array.Empty<OutletPolicySelectionEntry>());
            world.RequireHealthyUpdate();
            if (targets == null || targets.Count == 0)
                return new OutletPolicySelectionPreview(Codes.InvalidArgument, Array.Empty<OutletPolicySelectionEntry>());
            if (!world.Players.IsSeat(player)) return new OutletPolicySelectionPreview(Codes.NotSeated, Array.Empty<OutletPolicySelectionEntry>());
            var ids = new EntityId[targets.Count]; for (int i = 0; i < ids.Length; i++) ids[i] = targets[i]; Array.Sort(ids);
            for (int i = 0; i < ids.Length; i++)
                if (!ids[i].IsValid || i > 0 && ids[i] == ids[i - 1])
                    return new OutletPolicySelectionPreview(Codes.InvalidArgument, Array.Empty<OutletPolicySelectionEntry>());
            var entries = new OutletPolicySelectionEntry[ids.Length]; Code first = Code.None;
            for (int i = 0; i < ids.Length; i++)
            {
                var entity = world.Entities.Get(ids[i]); OutletPolicySettings current = null, next = null;
                Code refusal = CheckChange(world, player, entity, change, out next, port);
                if (refusal != Codes.NotFound) current = Read(world, entity, port);
                entries[i] = new OutletPolicySelectionEntry(ids[i], refusal, current, next);
                if (first.IsNone && !refusal.IsNone) first = refusal;
            }
            return new OutletPolicySelectionPreview(first, entries);
        }
        /// <summary>Fresh whole admission, then original exact commits. Provider violation faults, not rollback.</summary>
        public static int ConfigureGroup(World world, PlayerId player, IReadOnlyList<EntityId> targets, OutletPolicyChange change, Code port = default)
        {
            var preview = PreviewGroup(world, player, targets, change, port);
            if (!preview.Blocker.IsNone) throw new InvalidOperationException("outlet selection refused: " + preview.Blocker);
            int changed = 0;
            try
            {
                foreach (var entry in preview.Entries)
                {
                    if (!entry.Next.Matches(entry.Current)) changed++;
                    Configure(world, world.Entities.Get(entry.Entity), change, port);
                }
                return changed;
            }
            catch (Exception error) { world.LatchFault("logistics.configure-outlet-selection", error); throw; }
        }
    }
}
