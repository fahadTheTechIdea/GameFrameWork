using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public readonly struct ObjectiveObservation
    {
        public bool Valid { get; }
        public bool Satisfied { get; }
        public Fix Progress { get; }
        public Code Blocker { get; }
        public ObjectiveObservation(bool valid, bool satisfied, Fix progress, Code blocker)
        {
            if (progress < Fix.Zero || progress > Fix.One || satisfied && (!valid || progress != Fix.One || !blocker.IsNone) ||
                !satisfied && blocker.IsNone) throw new ArgumentException("objective observation needs bounded progress and explicit readiness");
            Valid = valid; Satisfied = satisfied; Progress = progress; Blocker = blocker;
        }
        public static ObjectiveObservation Complete => new ObjectiveObservation(true, true, Fix.One, Code.None);
        public static ObjectiveObservation Wait(Code blocker, Fix progress = default) => new ObjectiveObservation(true, false, progress, blocker);
        public static ObjectiveObservation Invalid(Code blocker) => new ObjectiveObservation(false, false, Fix.Zero, blocker);
    }
    public readonly struct ObjectiveContext
    {
        public World World { get; }
        public PlayerId Company { get; }
        public string Objective { get; }
        public long Tick => World.Tick;
        internal ObjectiveContext(World world, PlayerId company, string objective)
        { World = world; Company = company; Objective = objective; }
    }
    public interface IObjectivePredicate : IContentDefinition, IDependencyNode
    {
        new string Id { get; }
        /// <summary>Actual immutable content whose state this predicate observes; explicit even when empty.</summary>
        IReadOnlyList<IContentDefinition> ObservationContent { get; }
        /// <summary>Validate declared bounds and required providers for every loaded predicate before Initialize, even if unused.</summary>
        void ValidateBinding(World world);
        ObjectiveObservation Observe(ObjectiveContext context, IReadOnlyDictionary<string, ObjectiveObservation> children);
    }
    public abstract class ObjectivePredicateDef : Def, IObjectivePredicate
    {
        public virtual IReadOnlyList<string> Dependencies => Array.Empty<string>();
        public abstract IReadOnlyList<IContentDefinition> ObservationContent { get; }
        public virtual void ValidateBinding(World world) { }
        public abstract ObjectiveObservation Observe(ObjectiveContext context, IReadOnlyDictionary<string, ObjectiveObservation> children);
    }
    public interface IObjectiveEffect : IContentDefinition
    {
        /// <summary>Validate declared bounds and required providers for every loaded effect before Initialize, even if unused.</summary>
        void ValidateBinding(World world);
        Code Check(ObjectiveContext context);
        void Apply(ObjectiveContext context);
    }
    public abstract class ObjectiveEffectDef : Def, IObjectiveEffect
    {
        public virtual void ValidateBinding(World world) { }
        public abstract Code Check(ObjectiveContext context);
        public abstract void Apply(ObjectiveContext context);
    }
    public interface IObjectiveDefinition : IContentDefinition, IDependencyNode
    {
        new string Id { get; }
        IObjectivePredicate Predicate { get; }
        GameDuration? Deadline { get; }
        IObjectiveEffect OnComplete { get; }
        IObjectiveEffect OnFailure { get; }
    }
    /// <summary>Sole objective metadata reader/resolver; ObjectiveSystem owns lifecycle and effects.</summary>
    public sealed class ObjectiveDeclaration
    {
        private string _predicate, _complete, _failure;
        public IReadOnlyList<string> Dependencies { get; private set; }
        public IObjectivePredicate Predicate { get; private set; }
        public GameDuration? Deadline { get; private set; }
        public IObjectiveEffect OnComplete { get; private set; }
        public IObjectiveEffect OnFailure { get; private set; }
        public void Read(JsonReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (reader.Array("prerequisites") == null) throw reader.Error("explicit prerequisite array required");
            string[] dependencies = reader.Strings("prerequisites"); Array.Sort(dependencies, StringComparer.Ordinal);
            Dependencies = Array.AsReadOnly(dependencies); _predicate = reader.String("predicate");
            JsonValue deadline = reader.Value("deadline");
            if (deadline == null) throw reader.Error("explicit deadline duration or null required");
            if (deadline.Kind != JsonKind.Null)
            {
                Deadline = new JsonReader(deadline, reader.Where + ".deadline").RationalDuration();
                if (Deadline.Value.IsZero) throw reader.Error("objective deadline must be positive");
            }
            _complete = OptionalReference(reader, "on_complete"); _failure = OptionalReference(reader, "on_failure");
        }
        internal static string OptionalReference(JsonReader reader, string key)
        {
            JsonValue value = reader.Value(key);
            if (value == null || value.Kind != JsonKind.Null && value.Kind != JsonKind.String) throw reader.Error(key + " needs an explicit identity or null");
            if (value.Kind == JsonKind.Null) return null;
            try { ContractIdentity.ValidateId(value.Text, key); } catch (ArgumentException e) { throw reader.Error(e.Message); }
            return value.Text;
        }
        public void Resolve(ContentResolver resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            if (_predicate == null) throw new InvalidOperationException("objective declaration resolves once");
            foreach (string dependency in Dependencies) resolver.Def<IObjectiveDefinition>(dependency);
            Predicate = resolver.Def<IObjectivePredicate>(_predicate);
            OnComplete = _complete == null ? null : resolver.Def<IObjectiveEffect>(_complete);
            OnFailure = _failure == null ? null : resolver.Def<IObjectiveEffect>(_failure);
            _predicate = _complete = _failure = null;
        }
    }
    public class ObjectiveDef : Def, IObjectiveDefinition
    {
        private readonly ObjectiveDeclaration _declaration = new ObjectiveDeclaration();
        public IReadOnlyList<string> Dependencies => _declaration.Dependencies;
        public IObjectivePredicate Predicate => _declaration.Predicate;
        public GameDuration? Deadline => _declaration.Deadline;
        public IObjectiveEffect OnComplete => _declaration.OnComplete;
        public IObjectiveEffect OnFailure => _declaration.OnFailure;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver resolver) => _declaration.Resolve(resolver);
    }
    public sealed class LogicalObjectivePredicateDef : ObjectivePredicateDef
    {
        public override IReadOnlyList<IContentDefinition> ObservationContent => Array.Empty<IContentDefinition>();
        private bool _all;
        private IReadOnlyList<string> _dependencies;
        public override IReadOnlyList<string> Dependencies => _dependencies;
        protected override void ReadCore(JsonReader reader)
        {
            string kind = reader.String("kind"); if (kind != "all" && kind != "any") throw reader.Error("logical objective kind must be all or any");
            _all = kind == "all";
            if (reader.Array("predicates") == null) throw reader.Error("explicit predicates required");
            string[] children = reader.Strings("predicates"); if (children.Length == 0) throw reader.Error("logical objective predicates cannot be empty");
            Array.Sort(children, StringComparer.Ordinal); _dependencies = Array.AsReadOnly(children);
        }
        protected override void ResolveCore(ContentResolver resolver) { foreach (string child in Dependencies) resolver.Def<IObjectivePredicate>(child); }
        public override ObjectiveObservation Observe(ObjectiveContext context, IReadOnlyDictionary<string, ObjectiveObservation> children)
        {
            bool satisfied = _all; BigInteger sum = BigInteger.Zero; Fix maximum = Fix.Zero; Code blocker = Code.None;
            foreach (string id in Dependencies)
            {
                ObjectiveObservation child = children[id]; if (!child.Valid) return ObjectiveObservation.Invalid(child.Blocker);
                satisfied = _all ? satisfied & child.Satisfied : satisfied | child.Satisfied;
                sum += child.Progress.Raw; if (child.Progress > maximum) maximum = child.Progress;
                if (!child.Satisfied && blocker.IsNone) blocker = child.Blocker;
            }
            return satisfied ? ObjectiveObservation.Complete : ObjectiveObservation.Wait(blocker, _all ? Fix.FromRaw((long)(sum / Dependencies.Count)) : maximum);
        }
    }
    public sealed class TechnologyObjectivePredicateDef : ObjectivePredicateDef
    {
        public override IReadOnlyList<IContentDefinition> ObservationContent => Array.AsReadOnly(new IContentDefinition[] { Technology });
        private string _technology;
        public ITechnologyDefinition Technology { get; private set; }
        protected override void ReadCore(JsonReader reader) => _technology = reader.String("technology");
        protected override void ResolveCore(ContentResolver resolver) => Technology = resolver.Def<ITechnologyDefinition>(_technology);
        public override void ValidateBinding(World world)
        { if (!world.Services.Has<ITechnologyKnowledge>()) throw new ContentException(Id + ": selected technology objective requires ITechnologyKnowledge"); }
        public override ObjectiveObservation Observe(ObjectiveContext context, IReadOnlyDictionary<string, ObjectiveObservation> children)
        {
            Code check = context.World.Services.Get<ITechnologyKnowledge>().CheckKnown(context.Company, Technology.Id);
            return check.IsNone ? ObjectiveObservation.Complete : check == TechnologyCodes.Locked ? ObjectiveObservation.Wait(check) : ObjectiveObservation.Invalid(check);
        }
    }
    public sealed class EntityObjectivePredicateDef : ObjectivePredicateDef
    {
        public override IReadOnlyList<IContentDefinition> ObservationContent => Array.AsReadOnly(new IContentDefinition[] { Prototype });
        private string _prototype;
        public EntityDef Prototype { get; private set; }
        public long Minimum { get; private set; }
        protected override void ReadCore(JsonReader reader)
        { _prototype = reader.String("prototype"); Minimum = reader.Long("minimum"); if (Minimum <= 0) throw reader.Error("positive minimum entity count required"); }
        protected override void ResolveCore(ContentResolver resolver) => Prototype = resolver.Def<EntityDef>(_prototype);
        public override ObjectiveObservation Observe(ObjectiveContext context, IReadOnlyDictionary<string, ObjectiveObservation> children)
        {
            long count = 0;
            foreach (Entity entity in context.World.Entities.All)
                if (entity.Alive && entity.Owner == context.Company && ReferenceEquals(entity.Def, Prototype)) count = checked(count + 1);
            return count >= Minimum ? ObjectiveObservation.Complete : ObjectiveObservation.Wait(ObjectiveCodes.Condition,
                Fix.FromRaw((long)((BigInteger)count * Fix.One.Raw / Minimum)));
        }
    }
    /// <summary>Shared private readiness; the objective owner alone commits lifecycle/effects.</summary>
    public sealed class ConditionObjectivePredicateDef : ObjectivePredicateDef
    {
        public override IReadOnlyList<IContentDefinition> ObservationContent => Array.AsReadOnly(new IContentDefinition[] { Condition });
        private string _condition;
        public IGameCondition Condition { get; private set; }
        protected override void ReadCore(JsonReader reader) => _condition = reader.String("condition");
        protected override void ResolveCore(ContentResolver content) => Condition = content.Def<IGameCondition>(_condition);
        public override void ValidateBinding(World world)
        {
            for (int i = 0; i < world.Players.Count; i++) GameConditionContracts.ValidateBinding(world, new PlayerId(i), Condition);
        }
        public override ObjectiveObservation Observe(ObjectiveContext context, IReadOnlyDictionary<string, ObjectiveObservation> children)
        {
            Code blocker = GameConditionContracts.Read(context.World, context.World.ViewFor(context.Company), Condition);
            return blocker.IsNone ? ObjectiveObservation.Complete : ObjectiveObservation.Wait(blocker);
        }
    }
    public sealed class OutcomeObjectiveEffectDef : ObjectiveEffectDef
    {
        public Code Outcome { get; private set; }
        protected override void ReadCore(JsonReader reader)
        { string id = reader.String("outcome"); try { ContractIdentity.ValidateId(id, "objective outcome"); } catch (ArgumentException e) { throw reader.Error(e.Message); } Outcome = new Code(id); }
        public override Code Check(ObjectiveContext context) => context.World.Outcome.IsNone ? Code.None : ObjectiveCodes.Outcome;
        public override void Apply(ObjectiveContext context) => context.World.End(Outcome);
    }
    public enum ObjectiveState : byte { Locked = 1, Active = 2, Completing = 3, Failing = 4, Completed = 5, Failed = 6 }
    public static class ObjectiveCodes
    {
        public static readonly Code Condition = new Code("objective.condition"), Prerequisite = new Code("objective.prerequisite"),
            Undisclosed = new Code("objective.undisclosed"),
            DependencyFailed = new Code("objective.dependency-failed"), Deadline = new Code("objective.deadline"),
            InvalidObservation = new Code("objective.invalid-observation"), Outcome = new Code("objective.outcome-set");
    }
    public readonly struct ObjectiveSnapshot
    {
        public PlayerId Company { get; }
        public string Objective { get; }
        public ObjectiveState State { get; }
        public long? ActivatedTick { get; }
        public long? SettledTick { get; }
        public long? DeadlineTick { get; }
        public ObjectiveObservation Observation { get; }
        public Code Blocker { get; }
        public Code Failure { get; }
        public IReadOnlyList<string> Prerequisites { get; }
        internal ObjectiveSnapshot(PlayerId company, IObjectiveDefinition definition, ObjectiveState state, long activated, long settled,
            long? deadline, ObjectiveObservation observation, Code blocker, Code failure)
        { Company = company; Objective = definition.Id; State = state; ActivatedTick = activated < 0 ? (long?)null : activated;
            SettledTick = settled < 0 ? (long?)null : settled; DeadlineTick = deadline; Observation = observation; Blocker = blocker; Failure = failure;
            Prerequisites = new List<string>(definition.Dependencies).AsReadOnly(); }
    }
    public interface IObjectives
    {
        void CopyObjectives(PlayerId viewer, IList<ObjectiveSnapshot> destination);
        ObjectiveObservation Observe(PlayerId company, string objective);
    }
}
