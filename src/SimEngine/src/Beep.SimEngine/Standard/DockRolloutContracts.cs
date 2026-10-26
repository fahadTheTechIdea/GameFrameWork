using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional production metadata on the SAME physical layout definition. No coordinates or live claims.</summary>
    public interface IWorkDockRolloutDefinition : IWorkDockDefinition
    { IReadOnlyList<WorkDockRollout> Rollouts { get; } }

    public sealed class WorkDockRollout
    {
        public Code Id { get; }
        public IReadOnlyList<EntityDef> Outputs { get; }
        public IReadOnlyList<Code> Starts { get; }
        public IReadOnlyList<Code> Exits { get; }
        public int MaxExpanded { get; }
        public WorkDockRollout(Code id, IReadOnlyList<EntityDef> outputs, IReadOnlyList<Code> starts, IReadOnlyList<Code> exits, int maxExpanded)
        {
            if (id.IsNone || outputs == null || starts == null || exits == null || outputs.Count == 0 || starts.Count == 0 || exits.Count == 0 || maxExpanded < 1)
                throw new ArgumentException("rollout needs identity, outputs, start/exit sets and positive search budget");
            Id = id; MaxExpanded = maxExpanded;
            var result = new List<EntityDef>(outputs); result.Sort((a,b) => StringComparer.Ordinal.Compare(a?.Id,b?.Id)); Outputs = result.AsReadOnly();
            var from = new List<Code>(starts); from.Sort((a,b) => StringComparer.Ordinal.Compare(a.Id,b.Id)); Starts = from.AsReadOnly();
            var to = new List<Code>(exits); to.Sort((a,b) => StringComparer.Ordinal.Compare(a.Id,b.Id)); Exits = to.AsReadOnly();
        }
    }

    /// <summary>Single strict JSON reader/resolver used by native and independent layout providers.</summary>
    public sealed class WorkDockRolloutDeclaration
    {
        private Code _id;
        private string[] _outputs, _starts, _exits;
        private int _maxExpanded;
        public static IReadOnlyList<WorkDockRolloutDeclaration> Read(JsonReader reader)
        {
            if (!reader.Has("rollouts")) return Array.Empty<WorkDockRolloutDeclaration>(); // No selected launch capability, never a placement fallback.
            var rows = reader.Array("rollouts") ?? throw reader.Error("rollouts must be an explicit array");
            var result = new List<WorkDockRolloutDeclaration>(); var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = new JsonReader(rows[i], reader.Where + ".rollouts[" + i + "]"); string id = row.String("id");
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw row.Error("unique rollout identity required");
                var value = new WorkDockRolloutDeclaration { _id = new Code(id), _outputs = Unique(row, "outputs"), _starts = Unique(row, "starts"), _exits = Unique(row, "exits"), _maxExpanded = row.Int("max_expanded") };
                if (value._maxExpanded < 1) throw row.Error("positive explicit rollout search budget required");
                row.End(); result.Add(value);
            }
            return result.AsReadOnly();
        }
        private static string[] Unique(JsonReader row, string key)
        {
            string[] values = row.Strings(key); var ids = new HashSet<string>(StringComparer.Ordinal);
            if (values.Length == 0) throw row.Error("nonempty " + key + " required");
            foreach (string id in values) if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw row.Error("unique nonempty " + key + " required");
            return values;
        }
        public WorkDockRollout Resolve(ContentResolver content)
        {
            var outputs = new EntityDef[_outputs.Length]; for (int i = 0; i < outputs.Length; i++) outputs[i] = content.Def<EntityDef>(_outputs[i]);
            return new WorkDockRollout(_id, outputs, Array.ConvertAll(_starts, id => new Code(id)), Array.ConvertAll(_exits, id => new Code(id)), _maxExpanded);
        }
    }

    public static partial class WorkDockRules
    {
        public static IEnumerable<WorkDockRollout> RolloutsFor(EntityDef producer, EntityDef output)
        {
            if (producer?.Component<IWorkDockRolloutDefinition>() is not IWorkDockRolloutDefinition layout || layout.Rollouts == null) yield break;
            var lanes = new List<WorkDockRollout>(layout.Rollouts); lanes.Sort((a,b) => StringComparer.Ordinal.Compare(a.Id.Id,b.Id.Id));
            foreach (var lane in lanes) foreach (var result in lane.Outputs) if (ReferenceEquals(result, output)) { yield return lane; break; }
        }
        public static void ValidateRollouts(ResolvedContent content, EntityDef owner)
        {
            var layout = owner.Component<IWorkDockRolloutDefinition>(); if (layout == null) return;
            if (layout.Rollouts == null) throw new ContentException(owner.Id + ": immutable rollout metadata required");
            var slots = new Dictionary<Code, WorkDockSlot>(); foreach (var slot in layout.Slots) slots.Add(slot.Id, slot);
            var ids = new HashSet<Code>();
            foreach (var lane in layout.Rollouts)
            {
                if (lane == null || lane.Id.IsNone || !ids.Add(lane.Id) || lane.MaxExpanded < 1 || lane.Outputs.Count == 0 || lane.Starts.Count == 0 || lane.Exits.Count == 0)
                    throw new ContentException(owner.Id + ": invalid rollout lane");
                var outputs = new HashSet<string>(StringComparer.Ordinal);
                foreach (var output in lane.Outputs)
                    if (output == null || !ReferenceEquals(content.Find<EntityDef>(output.Id), output) || !outputs.Add(output.Id) || !output.HasCapability<IMotion>() ||
                        output.Component<ISpatialPresenceDefinition>()?.Space != owner.Component<ISpatialPresenceDefinition>()?.Space)
                        throw new ContentException(owner.Id + ": rollout outputs require unique actual mobile prototypes in the layout's space");
                var selected = new HashSet<Code>();
                foreach (Code id in lane.Starts)
                    if (!selected.Add(id) || !slots.TryGetValue(id, out var slot) || slot.Kind != WorkDockSlotKind.Service && slot.Kind != WorkDockSlotKind.Rollout)
                        throw new ContentException(owner.Id + ": rollout start must reference a unique service or launch slot");
                foreach (Code id in lane.Exits)
                    if (!selected.Add(id) || !slots.TryGetValue(id, out var slot) || slot.Kind != WorkDockSlotKind.Departure)
                        throw new ContentException(owner.Id + ": rollout exit must reference a distinct authored departure");
            }
        }
    }
}
