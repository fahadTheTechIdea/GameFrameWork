using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed class WorkPolicySelectionEntry
    {
        public EntityId Entity { get; }
        public Code Blocker { get; }
        public WorkPolicySettings Settings { get; }
        internal WorkPolicySelectionEntry(EntityId entity, Code blocker, WorkPolicySettings settings)
        { Entity = entity; Blocker = blocker; Settings = settings; }
    }
    public sealed class WorkPolicySelectionPreview
    {
        public Code Blocker { get; }
        public IReadOnlyList<WorkPolicySelectionEntry> Entries { get; }
        internal WorkPolicySelectionPreview(Code blocker, WorkPolicySelectionEntry[] entries)
        { Blocker = blocker; Entries = Array.AsReadOnly((WorkPolicySelectionEntry[])entries.Clone()); }
    }
    public static partial class WorkPolicyRules
    {
        internal static WorkPolicySettings ReadObserved(World world, Entity entity)
        {
            var policy = entity.Get<IWorkPolicy>(); if (policy == null) return null;
            if (!ReferenceEquals(policy.Entity, entity)) throw new InvalidOperationException("work policy must belong to its actual worker");
            return EntityObservation.ReadProvider(world, policy, entity, () => Read(entity));
        }
        public static WorkPolicySelectionPreview PreviewGroup(World world, PlayerId player, IReadOnlyList<EntityId> targets, WorkPolicySettings settings)
        {
            if (world == null) return new WorkPolicySelectionPreview(Codes.InvalidArgument, Array.Empty<WorkPolicySelectionEntry>());
            world.RequireHealthyUpdate();
            if (targets == null || targets.Count == 0)
                return new WorkPolicySelectionPreview(Codes.InvalidArgument, Array.Empty<WorkPolicySelectionEntry>());
            if (!world.Players.IsSeat(player)) return new WorkPolicySelectionPreview(Codes.NotSeated, Array.Empty<WorkPolicySelectionEntry>());
            var ids = new EntityId[targets.Count]; for (int i = 0; i < ids.Length; i++) ids[i] = targets[i]; Array.Sort(ids);
            for (int i = 0; i < ids.Length; i++)
                if (!ids[i].IsValid || i > 0 && ids[i] == ids[i - 1])
                    return new WorkPolicySelectionPreview(Codes.InvalidArgument, Array.Empty<WorkPolicySelectionEntry>());
            var entries = new WorkPolicySelectionEntry[ids.Length]; Code first = Code.None;
            for (int i = 0; i < ids.Length; i++)
            {
                var entity = world.Entities.Get(ids[i]); WorkPolicySettings current = null;
                Code refusal = CheckConfigure(world, player, entity, settings);
                if (refusal != Codes.NotFound) current = ReadObserved(world, entity);
                entries[i] = new WorkPolicySelectionEntry(ids[i], refusal, current);
                if (first.IsNone && !refusal.IsNone) first = refusal;
            }
            return new WorkPolicySelectionPreview(first, entries);
        }
        /// <summary>Whole admission before mutation; actual providers commit, violations fail-stop rather than roll back.</summary>
        public static int ConfigureGroup(World world, PlayerId player, IReadOnlyList<EntityId> targets, WorkPolicySettings settings)
        {
            var preview = PreviewGroup(world, player, targets, settings);
            if (!preview.Blocker.IsNone) throw new InvalidOperationException("work policy selection refused: " + preview.Blocker);
            int changed = 0;
            try
            {
                foreach (var entry in preview.Entries)
                {
                    if (!settings.Matches(entry.Settings)) changed++;
                    Configure(world, world.Entities.Get(entry.Entity), settings);
                }
                return changed;
            }
            catch (Exception error) { world.LatchFault("work.configure-selection", error); throw; }
        }
    }
}
