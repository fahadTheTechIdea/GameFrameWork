using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed class WorkerHomeSelectionEntry
    {
        public EntityId Entity { get; }
        public Code Blocker { get; }
        public WorkerHomeView Current { get; }
        internal WorkerHomeSelectionEntry(EntityId entity, Code blocker, WorkerHomeView current)
        { Entity = entity; Blocker = blocker; Current = current; }
    }
    public sealed class WorkerHomeSelectionPreview
    {
        public Code Blocker { get; }
        public IReadOnlyList<WorkerHomeSelectionEntry> Entries { get; }
        internal WorkerHomeSelectionPreview(Code blocker, WorkerHomeSelectionEntry[] entries)
        { Blocker = blocker; Entries = Array.AsReadOnly((WorkerHomeSelectionEntry[])entries.Clone()); }
    }
    public static partial class WorkerHomeContracts
    {
        public static WorkerHomeSelectionPreview PreviewGroup(World world, PlayerId player, IReadOnlyList<EntityId> targets, EntityId home)
        {
            if (world == null) return new WorkerHomeSelectionPreview(Codes.InvalidArgument, Array.Empty<WorkerHomeSelectionEntry>());
            world.RequireHealthyUpdate();
            if (targets == null || targets.Count == 0)
                return new WorkerHomeSelectionPreview(Codes.InvalidArgument, Array.Empty<WorkerHomeSelectionEntry>());
            if (!world.Players.IsSeat(player)) return new WorkerHomeSelectionPreview(Codes.NotSeated, Array.Empty<WorkerHomeSelectionEntry>());
            var ids = new EntityId[targets.Count]; for (int i = 0; i < ids.Length; i++) ids[i] = targets[i]; Array.Sort(ids);
            for (int i = 0; i < ids.Length; i++)
                if (!ids[i].IsValid || i > 0 && ids[i] == ids[i - 1])
                    return new WorkerHomeSelectionPreview(Codes.InvalidArgument, Array.Empty<WorkerHomeSelectionEntry>());
            var entries = new WorkerHomeSelectionEntry[ids.Length]; Code first = Code.None;
            for (int i = 0; i < ids.Length; i++)
            {
                var entity = world.Entities.Get(ids[i]); WorkerHomeView current = null;
                Code refusal = CheckHome(world, player, ids[i], home);
                if (refusal != Codes.NotFound && entity?.Get<IWorker>() != null) ReadCurrent(world, player, entity, out current);
                entries[i] = new WorkerHomeSelectionEntry(ids[i], refusal, current);
                if (first.IsNone && !refusal.IsNone) first = refusal;
            }
            return new WorkerHomeSelectionPreview(first, entries);
        }
        /// <summary>Whole admission; actual workers own home intent. A violated commit faults, never rolls back.</summary>
        public static int ConfigureGroup(World world, PlayerId player, IReadOnlyList<EntityId> targets, EntityId home)
        {
            var preview = PreviewGroup(world, player, targets, home);
            if (!preview.Blocker.IsNone) throw new InvalidOperationException("worker home selection refused: " + preview.Blocker);
            int changed = 0;
            try
            {
                foreach (var entry in preview.Entries)
                {
                    if (ReadHome(world, world.Entities.Get(entry.Entity)) != home) changed++;
                    Configure(world, player, entry.Entity, home);
                }
                return changed;
            }
            catch (Exception error) { world.LatchFault("work.configure-home-selection", error); throw; }
        }
    }
}
