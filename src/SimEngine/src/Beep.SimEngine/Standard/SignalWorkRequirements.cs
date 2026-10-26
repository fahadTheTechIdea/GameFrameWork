using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface ISignalWorkRequirement : IWorkRequirement
    {
        IReadOnlyList<SignalGateCondition> Conditions { get; }
    }
    /// <summary>Pure current operational facts; signals/thresholds/state remain with their existing owners.</summary>
    public abstract class SignalWorkRequirementDef : WorkRequirementDef, ISignalWorkRequirement, IWorkRequirementBindingValidator
    {
        public override IReadOnlyList<IContentDefinition> ContentDependencies
        {
            get
            {
                var references = new List<IContentDefinition>();
                foreach (SignalGateCondition condition in Conditions)
                    if (!references.Contains(condition.Signal)) references.Add(condition.Signal);
                return references.AsReadOnly();
            }
        }
        public IReadOnlyList<SignalGateCondition> Conditions { get; private set; }
        protected override void ReadCore(JsonReader reader) => Conditions = SignalGateCondition.ReadAll(reader);
        protected override void ResolveCore(ContentResolver content)
        { foreach (SignalGateCondition condition in Conditions) condition.Resolve(content); }
        public void ValidateBinding(World world)
        {
            foreach (SignalGateCondition condition in Conditions)
            {
                bool found = false;
                foreach (EntityDef definition in world.Content.All<EntityDef>())
                    if (condition.Accepts(definition)) { found = true; break; }
                if (!found) throw new ContentException(Id + ": condition '" + condition.Id + "' has no selected compatible named source");
            }
        }
        protected abstract Entity Subject(WorkRequirementContext context);
        public override WorkRequirementResult Evaluate(WorkRequirementContext context)
        {
            Entity entity = Subject(context);
            if (entity == null) return WorkRequirementResult.Pending(Codes.NotFound);
            if (!entity.Alive || !ReferenceEquals(context.World.Entities.Get(entity.Id), entity))
                return WorkRequirementResult.Invalid(Codes.NotFound);
            if (entity.Owner != context.Owner) return WorkRequirementResult.Blocked(Codes.NotOwned);
            foreach (SignalGateCondition condition in Conditions)
            {
                IAutomationSignalSource source = entity.Get<IAutomationSignalSource>(condition.Source);
                AutomationSignalSnapshot value = condition.Signal.ReadFrom(context.World, source);
                ExecutionGateConditionSnapshot fact = condition.Evaluate(value);
                if (!fact.Ready) return WorkRequirementResult.Pending(fact.Blocker);
            }
            return WorkRequirementResult.Satisfied();
        }
    }
    public sealed class TargetSignalRequirementDef : SignalWorkRequirementDef
    { protected override Entity Subject(WorkRequirementContext context) => context.Target; }
    public sealed class WorkerSignalRequirementDef : SignalWorkRequirementDef, IWorkerWorkRequirement
    { protected override Entity Subject(WorkRequirementContext context) => context.Worker; }
}
