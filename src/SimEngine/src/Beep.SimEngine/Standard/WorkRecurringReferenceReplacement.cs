using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class WorkBoard
    {
        private EntityId PlanSourceOwner(PlanEntry entry)
        {
            if (entry.Origin.IsManual) return EntityId.None;
            var head = PlanEntryById(entry.Origin.CycleHead);
            if (head == null || head.Origin.CycleHead != head.Id || !head.SourceOwner.IsValid)
                throw new CorruptStateException("recurring plan lost its actual current source binding");
            return head.SourceOwner;
        }
        private Code CheckRecurringReferenceReplacement(Entity source, EntityDef destination)
        {
            foreach (Component component in source.Components)
                if (component is IRecurringPlanSource provider)
                {
                    var oldDefinition = source.Def.Component<IRecurringPlanDefinition>(provider.CapabilityId);
                    var next = destination.Component<IRecurringPlanDefinition>(provider.CapabilityId);
                    if (next is not ComponentDef target || oldDefinition == null || target.Key != component.Def.Key ||
                        !Upgrades.CanRetainState(component.Def, target) ||
                        next.Cadence != oldDefinition.Cadence || next.TriggerMode != oldDefinition.TriggerMode ||
                        next.WorkerDispatch != oldDefinition.WorkerDispatch || next.PlanKey != oldDefinition.PlanKey ||
                        (next.Template != null) != (oldDefinition.Template != null))
                        return ReplacementRecurringSource;
                    if (oldDefinition is IRecurringPlanBindingDefinition oldBinding &&
                        (next is not IRecurringPlanBindingDefinition binding || binding.Binding.Target != oldBinding.Binding.Target))
                        return ReplacementRecurringSource;
                    var schedule = RecurringPlanContracts.ReadSchedule(_world, provider);
                    ValidateRecurringLink(provider, schedule);
                }
            foreach (var entry in _plans)
                if (entry.SourceOwner == source.Id &&
                    (entry.Company != source.Owner || source.Get<IRecurringPlanSource>(entry.Origin.Source) == null ||
                    destination.Component<IRecurringPlanDefinition>(entry.Origin.Source) == null))
                    return ReplacementRecurringSource;
            return Code.None;
        }
        private void ReplaceRecurringReferences(Entity source, Entity destination)
        {
            var sources = new List<IRecurringPlanSource>();
            foreach (Component component in source.Components)
                if (component is IRecurringPlanSource provider)
                {
                    var retained = destination.Get<IRecurringPlanSource>(provider.CapabilityId);
                    if (retained == null || !RecurringPlanContracts.Same(
                        RecurringPlanContracts.ReadSchedule(_world, provider), RecurringPlanContracts.ReadSchedule(_world, retained)) ||
                        !RecurringPlanContracts.SameTemplateSettings(
                            RecurringPlanContracts.ReadTemplateSettings(_world, provider), RecurringPlanContracts.ReadTemplateSettings(_world, retained)))
                        throw new InvalidOperationException("replacement did not retain the actual recurring source schedule");
                    sources.Add(retained);
                }
            // One mutable live binding per existing head; never change historical Origin/Target/Arguments.
            bool changed = false;
            foreach (var entry in _plans)
                if (entry.SourceOwner == source.Id) { entry.SourceOwner = destination.Id; changed = true; }
            if (changed) Dirty = true;
            foreach (var retained in sources) ValidateRecurringLink(retained, RecurringPlanContracts.ReadSchedule(_world, retained));
        }
    }
}
