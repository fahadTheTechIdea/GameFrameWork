using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class WorkBoard
    {
        private IPlanHistoryPolicy _historyPolicy;
        private long _historyAge, _historyCursor;
        internal void SelectPlanHistoryPolicy(IPlanHistoryPolicy policy)
        {
            if (_historyPolicy != null) throw new ContentException("history policy must be selected once from actual authored content");
            var facts = PlanHistoryPolicyContracts.Read(_world, policy, 0);
            _historyAge = _world.Time.Ticks(facts.MinimumAge);
            _historyPolicy = policy;
        }
        private void ValidatePlanHistoryPolicy()
        {
            if (_historyPolicy == null) return;
            var facts = PlanHistoryPolicyContracts.Read(_world, _historyPolicy, _historyCursor);
            if (_historyAge != _world.Time.Ticks(facts.MinimumAge))
                throw new ContentException("history policy must alias immutable selected content");
        }
        private void SyncPlanHistoryPolicy(IStateSync sync)
        {
            string id = _historyPolicy?.Id; sync.String(ref id);
            if (id != _historyPolicy?.Id) throw new CorruptStateException("saved history policy differs from selected content");
            sync.Long(ref _historyCursor);
            if (_historyCursor < 0 || _historyCursor >= _nextPlan || _historyPolicy == null && _historyCursor != 0)
                throw new CorruptStateException("invalid actual plan history scan cursor");
        }
        public PlanHistoryPolicyView? ReadPlanHistoryPolicy()
            => _historyPolicy == null ? (PlanHistoryPolicyView?)null : PlanHistoryPolicyContracts.Read(_world, _historyPolicy, _historyCursor);

        // Derived scoped references only. Never a persistent second cohort/history ledger.
        private sealed class HistoryCohort
        {
            internal readonly List<PlanEntry> Members = new List<PlanEntry>();
            internal long Head => Members[0].Id;
            internal PlayerId Company => Members[0].Company;
            internal bool Terminal
            { get { foreach (PlanEntry entry in Members) if (entry.Live) return false; return true; } }
            internal long Ended
            { get { if (!Terminal) return -1; long last = 0; foreach (PlanEntry entry in Members) last = Math.Max(last, entry.EndedTick); return last; } }
            internal IEnumerable<long> Ids
            { get { foreach (PlanEntry entry in Members) yield return entry.Id; } }
        }
        private List<HistoryCohort> HistoryCohorts()
        {
            var result = new List<HistoryCohort>();
            var recurring = new Dictionary<long, HistoryCohort>();
            var manual = new Dictionary<(int, string), HistoryCohort>();
            foreach (PlanEntry entry in _plans)
            {
                HistoryCohort cohort = null;
                if (!entry.Origin.IsManual) recurring.TryGetValue(entry.Origin.CycleHead, out cohort);
                else if (entry.Group != null) manual.TryGetValue((entry.Company.Value, entry.Group), out cohort);
                if (cohort == null)
                {
                    cohort = new HistoryCohort(); result.Add(cohort);
                    if (!entry.Origin.IsManual) recurring.Add(entry.Origin.CycleHead, cohort);
                    else if (entry.Group != null) manual.Add((entry.Company.Value, entry.Group), cohort);
                }
                cohort.Members.Add(entry);
            }
            foreach (HistoryCohort cohort in result) cohort.Members.Sort((a, b) => a.Id.CompareTo(b.Id));
            result.Sort((a, b) => a.Head.CompareTo(b.Head)); return result;
        }
        private Dictionary<HistoryCohort, Code> HistoryPolicyChecks(List<HistoryCohort> cohorts)
        {
            var protectedHeads = new HashSet<long>();
            var policy = ReadPlanHistoryPolicy();
            if (policy.HasValue && policy.Value.MinimumRetainedCohorts > 0)
            {
                var companies = new SortedDictionary<int, List<HistoryCohort>>();
                foreach (var cohort in cohorts) if (cohort.Terminal)
                {
                    if (!companies.TryGetValue(cohort.Company.Value, out var rows))
                    { rows = new List<HistoryCohort>(); companies.Add(cohort.Company.Value, rows); }
                    rows.Add(cohort);
                }
                foreach (var rows in companies.Values)
                {
                    rows.Sort((a, b) => a.Ended != b.Ended ? b.Ended.CompareTo(a.Ended) : b.Head.CompareTo(a.Head));
                    for (int i = 0; i < Math.Min(rows.Count, policy.Value.MinimumRetainedCohorts); i++) protectedHeads.Add(rows[i].Head);
                }
            }
            var result = new Dictionary<HistoryCohort, Code>();
            foreach (var cohort in cohorts)
                result.Add(cohort, !cohort.Terminal ? PlanCodes.NotEnded : _historyPolicy == null ? PlanCodes.HistoryUnselected
                    : protectedHeads.Contains(cohort.Head) ? PlanCodes.HistoryMinimum
                    : _world.Clock.StepIndex - cohort.Ended < _historyAge ? PlanCodes.HistoryYoung : Code.None);
            return result;
        }
        private List<HistoryCohort> SafeHistoryCohorts(List<HistoryCohort> candidates, Dictionary<HistoryCohort, Code> blockers)
        {
            var safe = new List<HistoryCohort>();
            var companies = new SortedDictionary<int, List<HistoryCohort>>();
            foreach (var cohort in candidates)
            {
                if (!companies.TryGetValue(cohort.Company.Value, out var rows))
                { rows = new List<HistoryCohort>(); companies.Add(cohort.Company.Value, rows); }
                rows.Add(cohort);
            }
            foreach (var rows in companies.Values)
            {
                var ids = new List<long>(); var owners = new Dictionary<long, HistoryCohort>();
                foreach (var cohort in rows) foreach (long id in cohort.Ids) { ids.Add(id); owners.Add(id, cohort); }
                PlanGroupCheck check = PreviewRetirePlanHistory(rows[0].Company, new PlanHistorySelection(ids));
                if (check.Ready) { safe.AddRange(rows); continue; }
                var rejected = new HashSet<HistoryCohort>(); var pending = new Queue<HistoryCohort>();
                foreach (var entry in check.Entries) if (!entry.Blocker.IsNone)
                {
                    var cohort = owners[entry.Plan];
                    if (rejected.Add(cohort)) { blockers[cohort] = entry.Blocker; pending.Enqueue(cohort); }
                }
                if (rejected.Count == 0) throw new InvalidOperationException("history reference admission lost selected diagnostics");
                // A retained dependent retains its actual prerequisites. Each whole cohort enters once.
                while (pending.Count > 0)
                    foreach (var member in pending.Dequeue().Members)
                        foreach (long prerequisite in member.Dependencies)
                            if (owners.TryGetValue(prerequisite, out var ancestor) && rejected.Add(ancestor))
                            { blockers[ancestor] = PlanCodes.HistoryDependency; pending.Enqueue(ancestor); }
                foreach (var cohort in rows) if (!rejected.Contains(cohort)) safe.Add(cohort);
            }
            return safe;
        }
        public void CopyPlanHistory(PlayerId viewer, IList<PlanHistoryCohortView> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination)); _world.RequireHealthyUpdate();
            var facts = EntityObservation.ReadProvider(_world, this, null, () => {
                var all = HistoryCohorts(); var visible = new HashSet<long>();
                foreach (var cohort in all)
                {
                    bool disclose = true;
                    foreach (PlanEntry member in cohort.Members)
                        if (!CanDisclosePlan(viewer, member)) { disclose = false; break; }
                    if (disclose) visible.Add(cohort.Head);
                }
                // Policy references/retirement safety remain derived from the entire original board.
                var checks = HistoryPolicyChecks(all); var candidates = new List<HistoryCohort>();
                var rows = new List<PlanHistoryCohortView>();
                foreach (var cohort in all) if (checks[cohort].IsNone) candidates.Add(cohort);
                _ = SafeHistoryCohorts(candidates, checks);
                foreach (var cohort in all)
                    if (visible.Contains(cohort.Head))
                        rows.Add(new PlanHistoryCohortView(cohort.Head, cohort.Ids, cohort.Ended, checks[cohort]));
                return rows;
            });
            _world.RequireHealthyUpdate(); destination.Clear(); foreach (var fact in facts) destination.Add(fact);
        }
        internal void RetireAutomaticPlanHistory()
        {
            if (_historyPolicy == null) return;
            var cohorts = HistoryCohorts(); if (cohorts.Count == 0) return;
            var checks = HistoryPolicyChecks(cohorts); var candidates = new List<HistoryCohort>();
            int start = 0; while (start < cohorts.Count && cohorts[start].Head <= _historyCursor) start++;
            if (start == cohorts.Count) start = 0;
            int count = Math.Min(cohorts.Count, PlanHistoryPolicyContracts.Read(_world, _historyPolicy, _historyCursor).CohortsPerUpdate);
            for (int i = 0; i < count; i++)
            {
                var cohort = cohorts[(int)(((long)start + i) % cohorts.Count)];
                _historyCursor = cohort.Head;
                if (checks[cohort].IsNone) candidates.Add(cohort);
            }
            var safe = SafeHistoryCohorts(candidates, checks);
            var companies = new SortedDictionary<int, List<long>>();
            foreach (var cohort in safe)
            {
                if (!companies.TryGetValue(cohort.Company.Value, out var ids)) { ids = new List<long>(); companies.Add(cohort.Company.Value, ids); }
                ids.AddRange(cohort.Ids);
            }
            foreach (var pair in companies) RetirePlanHistory(new PlayerId(pair.Key), new PlanHistorySelection(pair.Value));
        }
    }
}
