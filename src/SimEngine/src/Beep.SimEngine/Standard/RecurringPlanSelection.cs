using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed class RecurringPlanSelectionEntry
    {
        public EntityId Entity { get; }
        public Code Source { get; }
        public Code Blocker { get; }
        public RecurringPlanSchedule? Schedule { get; }
        internal RecurringPlanSelectionEntry(EntityId entity, Code source, Code blocker, RecurringPlanSchedule? schedule)
        { Entity = entity; Source = source; Blocker = blocker; Schedule = schedule; }
    }
    public sealed class RecurringPlanSelectionPreview
    {
        public Code Blocker { get; }
        public IReadOnlyList<RecurringPlanSelectionEntry> Entries { get; }
        internal RecurringPlanSelectionPreview(Code blocker, RecurringPlanSelectionEntry[] entries)
        { Blocker = blocker; Entries = Array.AsReadOnly((RecurringPlanSelectionEntry[])entries.Clone()); }
    }
    public static partial class RecurringPlanContracts
    {
        public static RecurringPlanSelectionPreview PreviewGroup(World world, PlayerId player, IReadOnlyList<EntityId> targets, Code source, RecurringPlanSettings settings)
        {
            if (world == null || targets == null || targets.Count == 0 || source.IsNone)
                return new RecurringPlanSelectionPreview(Codes.InvalidArgument, Array.Empty<RecurringPlanSelectionEntry>());
            if (!world.Players.IsSeat(player)) return new RecurringPlanSelectionPreview(Codes.NotSeated, Array.Empty<RecurringPlanSelectionEntry>());
            var ids = new EntityId[targets.Count]; for (int i = 0; i < ids.Length; i++) ids[i] = targets[i]; Array.Sort(ids);
            for (int i = 0; i < ids.Length; i++)
                if (!ids[i].IsValid || i > 0 && ids[i] == ids[i - 1])
                    return new RecurringPlanSelectionPreview(Codes.InvalidArgument, Array.Empty<RecurringPlanSelectionEntry>());
            var entries = new RecurringPlanSelectionEntry[ids.Length]; Code first = Code.None;
            for (int i = 0; i < ids.Length; i++)
            {
                var entity = world.Entities.Get(ids[i]); RecurringPlanSchedule? current = null;
                var provider = entity?.Alive == true && entity.Owner == player ? entity.Get<IRecurringPlanSource>(source) : null;
                Code refusal = entity?.Alive != true ? Codes.NotFound : entity.Owner != player ? Codes.NotOwned : CheckConfigure(world, provider, settings);
                if (provider != null) current = ReadSchedule(world, provider);
                entries[i] = new RecurringPlanSelectionEntry(ids[i], source, refusal, current);
                if (first.IsNone && !refusal.IsNone) first = refusal;
            }
            return new RecurringPlanSelectionPreview(first, entries);
        }
        /// <summary>Whole fresh admission; original providers own cadence. A violated commit faults, never rolls back.</summary>
        public static int ConfigureGroup(World world, PlayerId player, IReadOnlyList<EntityId> targets, Code source, RecurringPlanSettings settings)
        {
            var preview = PreviewGroup(world, player, targets, source, settings);
            if (!preview.Blocker.IsNone) throw new InvalidOperationException("recurring selection refused: " + preview.Blocker);
            int changed = 0;
            try
            {
                foreach (var entry in preview.Entries)
                {
                    if (!settings.Matches(entry.Schedule.Value.Settings)) changed++;
                    Configure(world, world.Entities.Get(entry.Entity).Get<IRecurringPlanSource>(source), settings);
                }
                return changed;
            }
            catch (Exception error) { world.LatchFault("work.configure-recurring-selection", error); throw; }
        }
    }
}
