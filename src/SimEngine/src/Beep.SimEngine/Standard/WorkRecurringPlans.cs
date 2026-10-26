using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class WorkBoard
    {
        /// <summary>Read actual recurring provenance, never a player priority or copied service flag.</summary>
        internal bool IsSelfService(Entity worker, IActivityNodeDefinition node, ActivityNodeExecution execution, IRecurringPlanSource prospective)
            => IsOwnRecurringWork(worker, node, execution, prospective, out var source) &&
                worker.Def.Component<IRecurringPlanBindingDefinition>(source.CapabilityId).Binding.ReserveUse == RecurringReserveUse.SelfService;
        private bool IsOwnRecurringWork(Entity worker, IActivityNodeDefinition node, ActivityNodeExecution execution, IRecurringPlanSource prospective, out IRecurringPlanSource source)
        {
            source = prospective;
            if (worker == null || node == null || !worker.Alive || !ReferenceEquals(_world.Entities.Get(worker.Id), worker)) return false;
            Order order = null;
            long originPlan = 0;
            if (execution != null)
            {
                order = Order(execution.OrderId);
                if (order?.IsActive != true || !ReferenceEquals(order.Node(execution.Id), execution) ||
                    !ReferenceEquals(order.Plan.Graph.Find(execution.Id), node) || order.WorkerSelection.WorkerFor(node.Id) != worker.Id ||
                    !_activePlanByOrder.TryGetValue(order.Id, out var entry) || entry.Origin.IsManual || PlanSourceOwner(entry) != worker.Id) return false;
                source = worker.Get<IRecurringPlanSource>(entry.Origin.Source);
                originPlan = entry.Id;
            }
            if (source == null || !ReferenceEquals(source.Entity, worker) ||
                !ReferenceEquals(worker.Get<IRecurringPlanSource>(source.CapabilityId), source)) return false;
            if (originPlan != 0 && RecurringPlanContracts.ReadSchedule(_world, source).LastPlan != originPlan) return false;
            var definition = worker.Def.Component<IRecurringPlanBindingDefinition>(source.CapabilityId);
            if (definition?.Binding == null || definition.Template != null) return false;
            var plan = RecurringPlanContracts.ResolvePlan(_world, definition);
            if (!ReferenceEquals(plan.Graph.Find(node.Id), node) || order != null && !ReferenceEquals(order.Plan, plan)) return false;
            foreach (string id in definition.Binding.WorkerNodes) if (id == node.Id) return true;
            return false;
        }
        internal Code CheckRecurringDispatch(Entity worker, IActivityNodeDefinition node, ActivityNodeExecution execution, IRecurringPlanSource prospective)
        {
            if (IsOwnRecurringWork(worker, node, execution, prospective, out _)) return Code.None;
            foreach (Component component in worker.Components)
                if (component is IRecurringPlanSource source &&
                    worker.Def.Component<IRecurringPlanDefinition>(source.CapabilityId).WorkerDispatch == RecurringWorkerDispatchMode.ServiceFirst)
                {
                    var schedule = RecurringPlanContracts.ReadSchedule(_world, source);
                    ValidateRecurringLink(source, schedule);
                    foreach (var member in RecurringMembers(source, schedule))
                        if (member.Live && !member.Paused) return RecurringPlanCodes.ServicePending;
                }
            return Code.None;
        }

        private bool _validatingRecurringDefinitions; // Transient callback guard, never saved chronology.
        private void ValidateRecurringDefinitions()
        {
            if (_validatingRecurringDefinitions) throw new InvalidOperationException("recurring metadata cannot recursively compose");
            _validatingRecurringDefinitions = true;
            try { ValidateRecurringDefinitionsCore(); }
            finally { _validatingRecurringDefinitions = false; }
        }
        private void ValidateRecurringDefinitionsCore()
        {
            foreach (EntityDef entity in _world.Content.All<EntityDef>())
                foreach (ComponentDef component in entity.Components)
                {
                    if (component.ComponentType == typeof(RecurringActivitySource) && !(component is IRecurringActivitySourceDefinition))
                        throw new ContentException(entity.Id + ": reusable recurring runtime requires its definition contract");
                    if (typeof(IRecurringPlanSource).IsAssignableFrom(component.ComponentType) && !(component is IRecurringPlanDefinition))
                        throw new ContentException(entity.Id + ": recurring source requires actual definition metadata");
                    if (component is IRecurringPlanDefinition definition)
                    {
                        EntityObservation.ReadProvider(_world, component, null, () => {
                        if (definition is IRecurringActivitySourceDefinition reusable)
                        {
                            RecurringActivityBindingContracts.ValidateBinding(_world, reusable);
                            reusable.ValidateBinding(_world);
                            RecurringActivityBindingContracts.ValidateBinding(_world, reusable);
                        }
                        if (!typeof(IRecurringPlanSource).IsAssignableFrom(component.ComponentType) || definition.Initial == null ||
                            (byte)definition.Cadence > (byte)RecurringCadenceBasis.SuccessfulCompletion ||
                            (byte)definition.TriggerMode > (byte)RecurringTriggerMode.ScheduleOrMaintenance ||
                            (byte)definition.WorkerDispatch > (byte)RecurringWorkerDispatchMode.ServiceFirst)
                            throw new ContentException(entity.Id + ": invalid recurring provider/activity binding");
                        if (definition.WorkerDispatch == RecurringWorkerDispatchMode.ServiceFirst &&
                            (!(definition is IRecurringPlanBindingDefinition dispatchBinding) || dispatchBinding.Binding == null ||
                            dispatchBinding.Binding.WorkerNodes.Count == 0 || definition.Template != null || !entity.HasCapability<IWorker>()))
                            throw new ContentException(entity.Id + ": service_first requires actual source-owned worker nodes on a single activity");
                        IPlanDefinition plan = definition.Template == null ? RecurringPlanContracts.ResolvePlan(_world, definition) : null;
                        if (plan != null) ValidatePlan(plan);
                        else
                        {
                            if (definition.PlanKey != null || !ReferenceEquals(_world.Content.Find<IPlanTemplateContent>(definition.Template.Id), definition.Template) || definition.Template.Template == null ||
                                !typeof(IRecurringTemplateSource).IsAssignableFrom(component.ComponentType) ||
                                definition.Template.Template.Space != _world.Settings.Space || !(definition is IRecurringPlanBindingDefinition templateBinding) ||
                                templateBinding.Binding?.Target != RecurringPlanTarget.Template || !ValidGroup(templateBinding.Group))
                                throw new ContentException(entity.Id + ": recurring template must alias selected immutable content and explicit binding/group");
                            foreach (var entry in definition.Template.Template.Entries)
                                ValidatePlan(ResolvePlan(entry.Definition) as IPlanDefinition);
                            // Structural anchor validation precedes players/targets; actual actor admission runs only in Update.
                            ValidateTemplateAnchor(definition.Template.Template, templateBinding.Binding.Arguments);
                        }
                        if (definition.TriggerMode != RecurringTriggerMode.Schedule &&
                            (!(definition is IRecurringOperationPolicyDefinition triggerLink) || triggerLink.OperationPolicy.IsNone))
                            throw new ContentException(entity.Id + ": maintenance trigger requires its actual named operation policy");
                        if (definition is IRecurringOperationPolicyDefinition link && !link.OperationPolicy.IsNone)
                        {
                            var policy = entity.Component<IOperationPolicyDefinition>(link.OperationPolicy);
                            bool coordinator = false;
                            foreach (var system in _world.Systems) if (system.Id == EquipmentConditionModule.ModuleId) coordinator = true;
                            if (!coordinator || policy?.InitialSettings == null ||
                                !RecurringPlanContracts.CheckPriority(definition, definition.Initial.Priority, policy.InitialSettings.Criticality).IsNone)
                                throw new ContentException(entity.Id + ": recurring source requires selected policy coordinator, actual named policy and representable priority");
                        }
                        if (!RecurringPlanContracts.CheckPriority(definition, definition.Initial.Priority, 0).IsNone)
                            throw new ContentException(entity.Id + ": recurring template priority is not representable");
                        Code refusal = RecurringPlanContracts.CheckInterval(_world, definition.Initial.Interval);
                        if (!refusal.IsNone) throw new ContentException(entity.Id + ": invalid recurring interval: " + refusal);
                        refusal = RecurringPlanContracts.CheckFacilityPolicy(_world, definition, definition.Initial.FacilityPolicy);
                        if (!refusal.IsNone) throw new ContentException(entity.Id + ": invalid recurring facility policy: " + refusal);
                        if (definition is IRecurringPlanBindingDefinition binding)
                        {
                            if (binding.Binding == null) throw new ContentException(entity.Id + ": recurring binding metadata is missing");
                            binding.Binding.Validate(entity, plan?.Graph);
                            if (binding.Binding.Target == RecurringPlanTarget.AuthoredIntent)
                            {
                                string identity = PlanObservation.Identity(_world, plan, binding.Binding.Arguments);
                                if (string.IsNullOrWhiteSpace(identity)) throw new ContentException(entity.Id + ": authored recurring intent has no stable identity");
                            }
                            else if (binding.Binding.Target != RecurringPlanTarget.Template)
                                ((IEntityTargetPlanDefinition)plan).ValidateTargetDefinition(binding.Binding.Target == RecurringPlanTarget.Self ? entity : null);
                        }
                        return true;
                        });
                    }
                }
        }
        internal void ValidateRecurringLink(IRecurringPlanSource source, RecurringPlanSchedule schedule)
        {
            if (schedule.LastPlan == 0) return;
            var members = RecurringMembers(source, schedule);
            if (members.Count == 0 || members[0].CreatedTick != schedule.LastPostedTick)
                throw new CorruptStateException("recurring cycle head differs from actual posting chronology");
            var definition = source.Entity.Def.Component<IRecurringPlanDefinition>(source.CapabilityId);
            PlanTemplate template = PlanEntryById(schedule.LastPlan)?.CycleTemplate;
            if ((definition.Template != null) != (template != null)) throw new CorruptStateException("recurring cycle lost its frozen admitted template shape");
            ValidateRecurringCycle(members, schedule.LastPlan, template, definition.PlanKey, source.Entity.Owner);
            if (schedule.LastCompletedPlan != 0 && !SuccessfulRecurringCompletion(source, schedule.LastCompletedPlan).HasValue)
                throw new CorruptStateException("recurring completion reference does not identify successful whole-cycle work");
        }
        internal long? SuccessfulRecurringCompletion(IRecurringPlanSource source, long plan)
        {
            if (plan == 0) return null;
            var members = RecurringMembers(source, plan);
            var definition = source.Entity.Def.Component<IRecurringPlanDefinition>(source.CapabilityId);
            var head = PlanEntryById(plan);
            if (head == null || members.Count == 0 || (definition.Template != null) != (head.CycleTemplate != null))
                throw new CorruptStateException("recurring success lost its actual source cycle");
            ValidateRecurringCycle(members, plan, head.CycleTemplate, definition.PlanKey, source.Entity.Owner);
            long completed = -1;
            foreach (var member in members)
            {
                if (member.State != PlanState.Done) return null;
                if (member.EndedTick < member.CreatedTick || member.EndedTick > _world.Clock.StepIndex)
                    throw new CorruptStateException("recurring completion is outside actual game chronology");
                completed = Math.Max(completed, member.EndedTick);
            }
            return completed;
        }
        private void ValidateRecurringCycle(List<PlanEntry> members, long first, PlanTemplate template, string planKey, PlayerId company)
        {
            int count = template?.Entries.Count ?? 1;
            if (members.Count != count || members[0].Id != first)
                throw new CorruptStateException("recurring source history does not alias its actual admitted plan");
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].Id - first != i || members[i].Company != company ||
                    members[i].CreatedTick != members[0].CreatedTick || members[i].Origin.CycleHead != first ||
                    members[i].Origin.TemplateCycle != (template != null) || members[i].Origin.Entity != members[0].Origin.Entity ||
                    members[i].Origin.Source != members[0].Origin.Source ||
                    members[i].Definition != (template == null ? planKey : template.Entries[i].Definition) ||
                    i > 0 && members[i].CycleTemplate != null)
                    throw new CorruptStateException("recurring source cycle has missing, duplicate or different template members");
                var expected = new List<long>();
                if (template != null)
                    foreach (string dependency in template.Entries[i].Dependencies)
                    {
                        int at = 0; while (template.Entries[at].Id != dependency) at++;
                        expected.Add(checked(first + at));
                    }
                expected.Sort();
                if (expected.Count != members[i].Dependencies.Count) throw new CorruptStateException("recurring cycle lost authored prerequisite links");
                for (int j = 0; j < expected.Count; j++) if (expected[j] != members[i].Dependencies[j])
                    throw new CorruptStateException("recurring cycle has different authored prerequisite links");
            }
        }
        private List<PlanEntry> RecurringMembers(IRecurringPlanSource source, RecurringPlanSchedule schedule)
            => RecurringMembers(source, schedule.LastPlan);
        private List<PlanEntry> RecurringMembers(IRecurringPlanSource source, long cycle)
        {
            var members = new List<PlanEntry>();
            if (cycle != 0 && (PlanEntryById(cycle) is not PlanEntry head || PlanSourceOwner(head) != source.Entity.Id))
                return members;
            if (cycle != 0) foreach (var entry in _plans)
                if (entry.Origin.CycleHead == cycle && entry.Origin.Source == source.CapabilityId)
                    members.Add(entry);
            members.Sort((a, b) => a.Id.CompareTo(b.Id)); return members;
        }
        internal bool HasOutstandingRecurringPlan(IRecurringPlanSource source, RecurringPlanSchedule schedule)
        {
            foreach (var entry in RecurringMembers(source, schedule)) if (entry.Live) return true;
            return false;
        }
        private long SubmitRecurringPlan(IRecurringPlanSource source, RecurringPlanView view)
        {
            IReadOnlyList<long> plans = view.GroupIntent == null ? new[] { SubmitPlan(source.Entity.Owner, view.Intent) }
                : SubmitPlanGroup(source.Entity.Owner, view.GroupIntent);
            foreach (long plan in plans) PlanEntryById(plan).Origin = new PlanOrigin(source.Entity.Id, source.CapabilityId, plans[0], view.TemplateSettings != null);
            PlanEntryById(plans[0]).SourceOwner = source.Entity.Id;
            if (view.TemplateSettings != null)
            {
                Code check = ResolvePlanTemplate(source.Entity.Owner, view.TemplateSettings.Selection, out PlanTemplate template);
                RequirePlanCheck(check); PlanEntryById(plans[0]).CycleTemplate = template;
            }
            return plans[0];
        }
        private void ValidateRecurringSources()
        {
            foreach (Entity entity in _world.Entities.All)
                if (entity.Alive) foreach (Component component in entity.Components)
                    if (component is IRecurringPlanSource source) RecurringPlanContracts.Read(_world, source);
        }
        internal void ObserveRecurringPlans()
        {
            // Acknowledge successful whole cycles before capturing the next admission pass.
            // Actual board history owns completion time; source state retains only its stable ID.
            foreach (Entity entity in _world.Entities.All)
                if (entity.Alive) foreach (Component component in entity.Components)
                    if (component is IRecurringPlanSource source &&
                        entity.Def.Component<IRecurringPlanDefinition>(source.CapabilityId).Cadence == RecurringCadenceBasis.SuccessfulCompletion)
                    {
                        var schedule = RecurringPlanContracts.ReadSchedule(_world, source);
                        ValidateRecurringLink(source, schedule);
                        if (schedule.LastPlan > schedule.LastCompletedPlan && SuccessfulRecurringCompletion(source, schedule.LastPlan).HasValue)
                            RecurringPlanContracts.RecordCompletion(_world, source, schedule.LastPlan);
                    }
            // Capture every trigger first. Subsequent submissions only recheck ordinary board admission.
            var pass = new List<(IRecurringPlanSource Source, RecurringPlanView View, int Priority)>();
            foreach (Entity entity in _world.Entities.All)
                if (entity.Alive) foreach (Component component in entity.Components)
                    if (component is IRecurringPlanSource source)
                    {
                        var value = RecurringPlanContracts.Read(_world, source);
                        if (value.Blocker.IsNone) pass.Add((source, value, RecurringPlanContracts.EffectivePriority(_world, source, value.Schedule.Priority)));
                    }
            pass.Sort((a, b) => a.Priority != b.Priority
                ? b.Priority.CompareTo(a.Priority)
                : a.Source.Entity.Id != b.Source.Entity.Id ? a.Source.Entity.Id.Value.CompareTo(b.Source.Entity.Id.Value)
                : string.CompareOrdinal(a.Source.CapabilityId.Id, b.Source.CapabilityId.Id));
            foreach (var entry in pass)
            {
                var offered = entry.View.GroupIntent == null ? RecurringPlanRequest.Ready(entry.View.Intent) : RecurringPlanRequest.Ready(entry.View.GroupIntent);
                if (!RecurringPlanContracts.PreviewAdmission(_world, entry.Source, offered).IsNone) continue;
                long plan = SubmitRecurringPlan(entry.Source, entry.View);
                RecurringPlanContracts.Record(_world, entry.Source, entry.View, plan);
            }
        }
    }
}
