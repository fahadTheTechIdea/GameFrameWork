using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Another file of the sole plan owner. Group membership is derived from existing intent records.
    public sealed partial class WorkBoard
    {
        private static bool ValidGroup(string group)
        { try { ContractIdentity.ValidateId(group, "plan group"); return true; } catch (ArgumentException) { return false; } }
        private List<PlanEntry> GroupEntries(PlayerId company, string group)
        {
            var result = new List<PlanEntry>();
            foreach (PlanEntry entry in _plans) if (entry.Company == company && entry.Group == group) result.Add(entry);
            return result;
        }
        private Code CheckGroup(PlayerId company, string group, out List<PlanEntry> entries)
        {
            entries = null; _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            if (!ValidGroup(group)) return Codes.InvalidArgument;
            entries = GroupEntries(company, group);
            return entries.Count == 0 ? Codes.NotFound : Code.None;
        }
        private static PlanGroupCheck GroupResult(IEnumerable<PlanGroupEntryCheck> entries)
        {
            var copy = new List<PlanGroupEntryCheck>(entries); Code blocker = Code.None;
            foreach (PlanGroupEntryCheck entry in copy) if (!entry.Blocker.IsNone) { blocker = entry.Blocker; break; }
            return new PlanGroupCheck(blocker, copy);
        }
        public PlanGroupCheck PreviewPlanGroup(PlayerId company, PlanGroupIntent intent)
        {
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company)) return new PlanGroupCheck(Codes.NotSeated);
            if (intent == null) return new PlanGroupCheck(Codes.InvalidArgument);
            if (GroupEntries(company, intent.Group).Count > 0) return new PlanGroupCheck(PlanCodes.GroupExists);
            if (_nextPlan > long.MaxValue - intent.Entries.Count) return new PlanGroupCheck(PlanCodes.IdentityRange);
            var entries = new List<PlanGroupEntryCheck>(); var identities = new HashSet<(string Definition, string Identity)>();
            foreach (PlanIntent entry in intent.Entries)
            {
                Code blocker = PreviewPlan(company, entry);
                if (blocker.IsNone)
                {
                    IPlanDefinition definition = (IPlanDefinition)ResolvePlan(entry.Definition);
                    string identity = PlanObservation.Identity(_world, definition, entry.Arguments);
                    if (!identities.Add((entry.Definition, identity))) blocker = PlanCodes.Duplicate;
                }
                entries.Add(new PlanGroupEntryCheck(0, blocker));
            }
            return GroupResult(entries);
        }
        private PlanGroupCheck CheckLiveGroup(PlayerId company, string group, Func<PlanEntry, Code> check)
        {
            Code admission = CheckGroup(company, group, out List<PlanEntry> members);
            if (!admission.IsNone) return new PlanGroupCheck(admission);
            var entries = new List<PlanGroupEntryCheck>();
            foreach (PlanEntry entry in members) if (entry.Live) entries.Add(new PlanGroupEntryCheck(entry.Id, check(entry)));
            return entries.Count == 0 ? new PlanGroupCheck(Codes.AlreadyDone) : GroupResult(entries);
        }
        public PlanGroupCheck PreviewPlanGroupEdit(PlayerId company, string group, int priority, bool paused)
            => CheckLiveGroup(company, group, entry => PreviewPlanEdit(company, entry.Id, group, priority, paused));
        public PlanGroupCheck PreviewPlanGroupCancel(PlayerId company, string group)
        {
            Code check = CheckGroup(company, group, out List<PlanEntry> members);
            if (!check.IsNone) return new PlanGroupCheck(check);
            var plans = new List<long>(); foreach (PlanEntry member in members) if (member.Live) plans.Add(member.Id);
            return plans.Count == 0 ? new PlanGroupCheck(Codes.AlreadyDone)
                : PreviewPlanCancel(company, new PlanCancellationSelection(false, plans));
        }
        public PlanGroupCheck PreviewPlanGroupMove(PlayerId company, string group, long before)
        {
            PlanGroupCheck result = CheckLiveGroup(company, group, entry => PreviewPlanMove(company, entry.Id, before));
            if (!result.Ready) return result;
            foreach (PlanGroupEntryCheck entry in result.Entries) if (entry.Plan == before) return new PlanGroupCheck(Codes.InvalidArgument, result.Entries);
            return result;
        }
        private PlanGroupIntent RepeatIntent(List<PlanEntry> entries, string group)
        {
            var positions = new Dictionary<long, int>(); for (int i = 0; i < entries.Count; i++) positions.Add(entries[i].Id, i);
            var intents = new List<PlanIntent>(); var links = new List<PlanGroupDependency>();
            for (int i = 0; i < entries.Count; i++)
            {
                PlanEntry entry = entries[i]; var external = new List<long>();
                foreach (long prerequisite in entry.Dependencies)
                    if (positions.TryGetValue(prerequisite, out int at)) links.Add(new PlanGroupDependency(i, at)); else external.Add(prerequisite);
                intents.Add(new PlanIntent(entry.Definition, entry.Arguments, group, PlanPriority(entry), false, external));
            }
            return new PlanGroupIntent(group, intents, links);
        }
        public PlanGroupCheck PreviewPlanGroupRepeat(PlayerId company, string group, string newGroup)
        {
            Code admission = CheckGroup(company, group, out List<PlanEntry> members);
            if (!admission.IsNone) return new PlanGroupCheck(admission);
            if (!ValidGroup(newGroup) || group == newGroup) return new PlanGroupCheck(Codes.InvalidArgument);
            var entries = new List<PlanGroupEntryCheck>(); bool live = false;
            foreach (PlanEntry entry in members) { live |= entry.Live; entries.Add(new PlanGroupEntryCheck(entry.Id, entry.Live ? PlanCodes.NotEnded : Code.None)); }
            if (live) return GroupResult(entries);
            PlanGroupCheck submission = PreviewPlanGroup(company, RepeatIntent(members, newGroup));
            if (submission.Entries.Count == members.Count)
            {
                entries.Clear(); for (int i = 0; i < members.Count; i++) entries.Add(new PlanGroupEntryCheck(members[i].Id, submission.Entries[i].Blocker));
            }
            return new PlanGroupCheck(submission.Blocker, entries);
        }
        public IReadOnlyList<long> SubmitPlanGroup(PlayerId company, PlanGroupIntent intent)
        {
            RequirePlanCheck(PreviewPlanGroup(company, intent).Blocker);
            long first = _nextPlan; var admitted = new List<PlanIntent>();
            for (int i = 0; i < intent.Entries.Count; i++)
            {
                PlanIntent entry = intent.Entries[i]; var dependencies = new List<long>(entry.Dependencies);
                foreach (var link in intent.Dependencies) if (link.Entry == i) dependencies.Add(checked(first + link.Prerequisite));
                admitted.Add(new PlanIntent(entry.Definition, entry.Arguments, entry.Group, entry.Priority, entry.Paused, dependencies));
            }
            var ids = new List<long>(); foreach (PlanIntent entry in admitted) ids.Add(AddPlan(company, entry));
            return ids.AsReadOnly();
        }
        public void ConfigurePlanGroup(PlayerId company, string group, int priority, bool paused)
        {
            PlanGroupCheck check = PreviewPlanGroupEdit(company, group, priority, paused); RequirePlanCheck(check.Blocker);
            foreach (PlanGroupEntryCheck entry in check.Entries) ConfigurePlan(company, entry.Plan, group, priority, paused);
        }
        public void CancelPlanGroup(PlayerId company, string group)
        {
            PlanGroupCheck check = PreviewPlanGroupCancel(company, group); RequirePlanCheck(check.Blocker);
            var plans = new List<long>(); foreach (PlanGroupEntryCheck entry in check.Entries) plans.Add(entry.Plan);
            CancelPlan(company, new PlanCancellationSelection(false, plans));
        }
        public void MovePlanGroup(PlayerId company, string group, long before)
        {
            PlanGroupCheck check = PreviewPlanGroupMove(company, group, before); RequirePlanCheck(check.Blocker);
            var entries = new List<PlanEntry>(); foreach (PlanGroupEntryCheck entry in check.Entries) entries.Add(PlanEntryById(entry.Plan));
            foreach (PlanEntry entry in entries) _plans.Remove(entry);
            int index = before == 0 ? _plans.Count : _plans.IndexOf(PlanEntryById(before));
            _plans.InsertRange(index, entries);
        }
        public IReadOnlyList<long> RepeatPlanGroup(PlayerId company, string group, string newGroup)
        {
            RequirePlanCheck(PreviewPlanGroupRepeat(company, group, newGroup).Blocker);
            return SubmitPlanGroup(company, RepeatIntent(GroupEntries(company, group), newGroup));
        }
        public void CopyPlanGroup(PlayerId viewer, string group, IList<PlanView> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination)); _world.RequireHealthyUpdate();
            var facts = EntityObservation.ReadProvider(_world, this, null, () => {
                var rows = new List<PlanView>();
                if (ValidGroup(group)) foreach (PlanEntry entry in _plans)
                {
                    if (entry.Group != group || !CanDisclosePlan(viewer, entry)) continue;
                    bool visible = true;
                    foreach (PlanEntry member in GroupEntries(entry.Company, group))
                        if (!CanDisclosePlan(viewer, member)) { visible = false; break; }
                    if (visible) rows.Add(ProjectPlan(entry));
                }
                return rows;
            });
            _world.RequireHealthyUpdate(); destination.Clear(); foreach (var fact in facts) destination.Add(fact);
        }
    }
}
