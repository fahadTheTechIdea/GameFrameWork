using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Pure predicate over the sole entity update's committed state, never a state predictor.</summary>
    public abstract class EntityStateWorkRequirementDef : WorkRequirementDef
    {
        public override System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => new System.Collections.Generic.List<IContentDefinition>(States).AsReadOnly();
        private string[] _states;
        public IReadOnlyList<IEntityStateDefinition> States { get; private set; }
        public Code Blocker { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            if (!reader.Has("states")) throw reader.Error("explicit state references are required");
            _states = reader.Strings("states");
            if (_states.Length == 0) throw reader.Error("state requirement needs at least one state");
            string blocker = reader.String("blocker");
            try { ContractIdentity.ValidateId(blocker, "state requirement blocker"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            Blocker = new Code(blocker);
        }
        protected override void ResolveCore(ContentResolver content)
        {
            var states = new List<IEntityStateDefinition>(); var ids = new HashSet<string>(StringComparer.Ordinal);
            var codes = new HashSet<Code>();
            foreach (string id in _states)
            {
                IEntityStateDefinition state = content.Def<IEntityStateDefinition>(id);
                if (!ids.Add(state.Id) || !codes.Add(state.State))
                    throw content.Fail("state requirement references duplicate published states");
                states.Add(state);
            }
            states.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            States = states.AsReadOnly(); _states = null;
        }
        protected abstract Entity Subject(WorkRequirementContext context);
        public override WorkRequirementResult Evaluate(WorkRequirementContext context)
        {
            Entity actual = Subject(context);
            if (actual == null) return WorkRequirementResult.Pending(Codes.NotFound);
            if (!actual.Alive || !ReferenceEquals(context.World.Entities.Get(actual.Id), actual))
                return WorkRequirementResult.Invalid(Codes.NotFound);
            if (actual.Owner != context.Owner) return WorkRequirementResult.Blocked(Codes.NotOwned);
            IEntity entity = actual;
            foreach (IEntityStateDefinition state in States)
                if (entity.State == state.State) return WorkRequirementResult.Satisfied();
            return WorkRequirementResult.Blocked(Blocker);
        }
    }
    public sealed class TargetStateRequirementDef : EntityStateWorkRequirementDef
    { protected override Entity Subject(WorkRequirementContext context) => context.Target; }
    public sealed class WorkerStateRequirementDef : EntityStateWorkRequirementDef, IWorkerWorkRequirement
    { protected override Entity Subject(WorkRequirementContext context) => context.Worker; }
}
