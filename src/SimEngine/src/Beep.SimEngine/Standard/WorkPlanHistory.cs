using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class WorkBoard
    {
        private bool _readingPlanHistoryReference;
        /// <summary>Read existing reference owners once per admission; no persistent pin ledger.</summary>
        private List<IPlanHistoryRetention> PlanHistoryProviders()
        {
            var result = new List<IPlanHistoryRetention>();
            foreach (var provider in _world.Services.Providers<IPlanHistoryRetention>()) Add(provider);
            foreach (var system in _world.Systems) if (system is IPlanHistoryRetention provider) Add(provider);
            foreach (Entity entity in _world.Entities.All)
                if (entity.Alive) foreach (Component component in entity.Components)
                    if (component is IPlanHistoryRetention provider) Add(provider);
            return result;

            void Add(IPlanHistoryRetention provider)
            {
                foreach (var previous in result) if (ReferenceEquals(previous, provider)) return;
                result.Add(provider);
            }
        }
        private bool ProviderRetainsPlan(IPlanHistoryRetention provider, PlanEntry entry)
        {
            if (_readingPlanHistoryReference)
            {
                var error = new InvalidOperationException("history references cannot recursively request reference admission");
                _world.LatchFault("observation.provider", error);
                throw error;
            }
            _readingPlanHistoryReference = true;
            try
            {
                return EntityObservation.ReadProvider(_world, provider, null, () => {
                    bool retained = provider.RetainsPlan(_world, entry.Company, entry.Id);
                    _world.RequireHealthyUpdate();
                    return retained;
                }, observeTargetState: true);
            }
            finally { _readingPlanHistoryReference = false; }
        }
        public PlanGroupCheck PreviewRetirePlanHistory(PlayerId company, PlanHistorySelection selection)
        {
            if (!_world.Players.IsSeat(company)) return new PlanGroupCheck(Codes.NotSeated);
            if (selection == null) return new PlanGroupCheck(Codes.InvalidArgument);
            var selected = new HashSet<long>(selection.Plans);
            var checks = new List<PlanGroupEntryCheck>();
            // Establish whole private membership before observing any reference provider.
            foreach (long id in selection.Plans)
            {
                Code check = CheckPlanOwner(company, id, out PlanEntry entry);
                if (check.IsNone && entry.Live) check = PlanCodes.NotEnded;
                checks.Add(new PlanGroupEntryCheck(id, check));
            }
            var result = GroupResult(checks); if (!result.Ready) return result;
            var dependencies = new HashSet<long>(); var incompleteCycles = new HashSet<long>();
            foreach (PlanEntry entry in _plans)
            {
                if (!selected.Contains(entry.Id))
                {
                    foreach (long id in entry.Dependencies) if (selected.Contains(id)) dependencies.Add(id);
                    if (!entry.Origin.IsManual) incompleteCycles.Add(entry.Origin.CycleHead);
                }
            }
            var sources = new HashSet<long>();
            foreach (Entity entity in _world.Entities.All)
                if (entity.Alive) foreach (Component component in entity.Components)
                    if (component is IRecurringPlanSource source)
                    {
                        var schedule = RecurringPlanContracts.ReadSchedule(_world, source);
                        if (schedule.LastPlan != 0) sources.Add(schedule.LastPlan);
                        if (schedule.LastCompletedPlan != 0) sources.Add(schedule.LastCompletedPlan);
                    }
            List<IPlanHistoryRetention> providers = PlanHistoryProviders(); checks.Clear();
            foreach (long id in selection.Plans)
            {
                PlanEntry entry = PlanEntryById(id); Code check = Code.None;
                if (dependencies.Contains(id)) check = PlanCodes.HistoryDependency;
                else if (!entry.Origin.IsManual && incompleteCycles.Contains(entry.Origin.CycleHead)) check = PlanCodes.HistoryCycle;
                else if (!entry.Origin.IsManual && sources.Contains(entry.Origin.CycleHead)) check = PlanCodes.HistorySource;
                else if (entry.Order != 0 && RetainsOrder(Order(entry.Order))) check = PlanCodes.HistoryExecution;
                else foreach (var provider in providers)
                    if (ProviderRetainsPlan(provider, entry)) { check = PlanCodes.HistoryProvider; break; }
                checks.Add(new PlanGroupEntryCheck(id, check));
            }
            return GroupResult(checks);
        }
        public void RetirePlanHistory(PlayerId company, PlanHistorySelection selection)
        {
            RequirePlanCheck(PreviewRetirePlanHistory(company, selection).Blocker);
            var selected = new HashSet<long>(selection.Plans);
            for (int i = _plans.Count - 1; i >= 0; i--)
                if (selected.Contains(_plans[i].Id))
                { _planById.Remove(_plans[i].Id); _plans.RemoveAt(i); }
            // IDs and native work are untouched. Actual terminal list/lookup are the only history.
            Dirty = true;
        }
    }
}
