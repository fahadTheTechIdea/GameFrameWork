using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Exact typed content match and pure visibility predicate; no visibility state owner.</summary>
    public interface IVisibilityTarget : IContentDefinition
    {
        IReadOnlyList<IContentDefinition> Targets { get; }
        bool Allows(World world, PlayerId company);
        void ValidateBinding(World world);
    }
    public interface IVisibilitySampleTarget : IVisibilityTarget
    {
        bool AllowsSample(World world, PlayerId company, SpatialSnapshot sample);
        void CopyObservationKeys(World world, PlayerId company, IList<string> destination);
    }
    public interface IVisibilityEntityTarget : IVisibilityTarget
    {
        bool AllowsEntity(World world, PlayerId company, Entity entity);
    }
    public interface IKnowledgeVisibilityDefinition : IContentDefinition
    {
        bool OwnEntities { get; }
        bool UnlistedEntities { get; }
        bool UnlistedContent { get; }
        IReadOnlyList<IVisibilityTarget> Rules { get; }
    }
    /// <summary>Actual immutable aliases; no independent visibility registry.</summary>
    public static class KnowledgeVisibilityContracts
    {
        public static IReadOnlyList<IContentDefinition> SingleTarget(IContentDefinition target)
            => Array.AsReadOnly(new[] { target ?? throw new ArgumentNullException(nameof(target)) });
        public static void ValidateAlias(World world, IContentDefinition definition)
        {
            if (!(definition is Def actual) || !actual.IsFrozen || !world.Content.Owns(definition) || definition is IPersistentService)
                throw new ContentException("visibility content requires actual frozen immutable metadata");
            bool alias = definition is IVisibilityTarget rule
                ? ReferenceEquals(world.Content.Find<IVisibilityTarget>(definition.Id), rule)
                : definition is IKnowledgeVisibilityDefinition policy &&
                    ReferenceEquals(world.Content.Find<IKnowledgeVisibilityDefinition>(definition.Id), policy);
            if (!alias) throw new ContentException(definition.Id + ": invalid visibility contract alias");
        }
        public static bool Known(World world, PlayerId company, IEnumerable<IKnowledgeSubjectDefinition> subjects, bool all)
        {
            ICompanyKnowledge knowledge = world.Services.Get<ICompanyKnowledge>();
            foreach (IKnowledgeSubjectDefinition subject in subjects)
            {
                bool known = knowledge.TryRead(company, company, AuthoredKnowledgeProvider.ProviderId, subject.Id, out _);
                if (all && !known) return false; if (!all && known) return true;
            }
            return all;
        }
    }
    /// <summary>The sole strict JSON definition/knowledge-target declaration and matching algorithm.</summary>
    public sealed class KnowledgeVisibilityTargetDeclaration<T> where T : class, IContentDefinition
    {
        private string _target;
        private string[] _subjects;
        private bool _all;
        private IReadOnlyList<IKnowledgeSubjectDefinition> _resolvedSubjects;
        public T Target { get; private set; }
        public IReadOnlyList<IContentDefinition> Targets { get; private set; }
        private KnowledgeVisibilityTargetDeclaration() { }
        public static KnowledgeVisibilityTargetDeclaration<T> Read(JsonReader reader)
        {
            var value = new KnowledgeVisibilityTargetDeclaration<T>();
            value._target = reader.String("definition"); string mode = reader.String("mode");
            if (mode != "all" && mode != "any") throw reader.Error("visibility mode must be all or any"); value._all = mode == "all";
            if (!reader.Has("facts")) throw reader.Error("explicit visibility facts required");
            value._subjects = reader.Strings("facts");
            if (value._subjects.Length == 0) throw reader.Error("visibility facts cannot be empty");
            Array.Sort(value._subjects, StringComparer.Ordinal);
            for (int i = 1; i < value._subjects.Length; i++)
                if (value._subjects[i] == value._subjects[i - 1]) throw reader.Error("duplicate visibility fact");
            return value;
        }
        public void Resolve(ContentResolver content)
        {
            if (Target != null) throw new InvalidOperationException("visibility target resolves once");
            Target = content.Def<T>(_target); var subjects = new List<IKnowledgeSubjectDefinition>();
            Targets = KnowledgeVisibilityContracts.SingleTarget(Target);
            foreach (string id in _subjects) subjects.Add(content.Def<IKnowledgeSubjectDefinition>(id));
            _resolvedSubjects = subjects.AsReadOnly(); _subjects = null;
        }
        public void ValidateBinding(World world, string context)
        {
            if (!world.Services.Has<ICompanyKnowledge>() || !world.Content.Owns(Target))
                throw new ContentException(context + ": visibility needs actual knowledge/content owner");
            foreach (var subject in _resolvedSubjects) KnowledgeContentContracts.ValidateSubject(world, subject);
        }
        public bool Allows(World world, PlayerId company)
            => KnowledgeVisibilityContracts.Known(world, company, _resolvedSubjects, _all);
    }
    public abstract class DefinitionKnowledgeVisibilityDef<T> : Def, IVisibilityTarget where T : class, IContentDefinition
    {
        private KnowledgeVisibilityTargetDeclaration<T> _declaration;
        public T Target => _declaration.Target;
        protected override void ReadCore(JsonReader reader) => _declaration = KnowledgeVisibilityTargetDeclaration<T>.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public IReadOnlyList<IContentDefinition> Targets => _declaration.Targets;
        public void ValidateBinding(World world) => _declaration.ValidateBinding(world, Id);
        public bool Allows(World world, PlayerId company) => _declaration.Allows(world, company);
    }
    public sealed class EntityKnowledgeVisibilityDef : DefinitionKnowledgeVisibilityDef<EntityDef> { }
    public sealed class ActivityKnowledgeVisibilityDef : DefinitionKnowledgeVisibilityDef<IAuthoredActivityDefinition> { }
    public sealed class TransformationKnowledgeVisibilityDef : DefinitionKnowledgeVisibilityDef<ITransformationDefinition> { }
    public sealed class LayerKnowledgeVisibilityDef : DefinitionKnowledgeVisibilityDef<ISpatialLayerDefinition> { }
    public sealed class ObjectiveKnowledgeVisibilityDef : DefinitionKnowledgeVisibilityDef<IObjectiveDefinition> { }
    public sealed class ObjectivePredicateKnowledgeVisibilityDef : DefinitionKnowledgeVisibilityDef<IObjectivePredicate> { }

    /// <summary>The sole explicit immutable authored visibility policy declaration.</summary>
    public sealed class KnowledgeVisibilityDeclaration
    {
        private string[] _rules;
        public bool OwnEntities { get; private set; }
        public bool UnlistedEntities { get; private set; }
        public bool UnlistedContent { get; private set; }
        public IReadOnlyList<IVisibilityTarget> Rules { get; private set; }
        private KnowledgeVisibilityDeclaration() { }
        public static KnowledgeVisibilityDeclaration Read(JsonReader reader)
        {
            var value = new KnowledgeVisibilityDeclaration {
                OwnEntities = reader.Bool("own_entities"), UnlistedEntities = reader.Bool("unlisted_entities"), UnlistedContent = reader.Bool("unlisted_content") };
            if (!reader.Has("rules")) throw reader.Error("visibility rules required"); value._rules = reader.Strings("rules");
            Array.Sort(value._rules, StringComparer.Ordinal);
            for (int i = 1; i < value._rules.Length; i++)
                if (value._rules[i] == value._rules[i - 1]) throw reader.Error("duplicate visibility rule");
            return value;
        }
        public void Resolve(ContentResolver content)
        {
            if (Rules != null) throw new InvalidOperationException("visibility policy resolves once");
            var rules = new List<IVisibilityTarget>(); foreach (string id in _rules) rules.Add(content.Def<IVisibilityTarget>(id));
            Rules = rules.AsReadOnly(); _rules = null;
        }
    }
    /// <summary>Explicit globally public prototype metadata. Other matching rules still restrict observation.</summary>
    public sealed class EntityPublicVisibilityDef : Def, IVisibilityTarget
    {
        private string _definition;
        public EntityDef Target { get; private set; }
        protected override void ReadCore(JsonReader reader) => _definition = reader.String("definition");
        protected override void ResolveCore(ContentResolver content) => Target = content.Def<EntityDef>(_definition);
        public IReadOnlyList<IContentDefinition> Targets => KnowledgeVisibilityContracts.SingleTarget(Target);
        public bool Allows(World world, PlayerId company) => world.Players.IsSeat(company) && world.Content.Owns(Target);
        public void ValidateBinding(World world)
        {
            if (!world.Content.Owns(Target) || !ReferenceEquals(world.Content.Find<EntityDef>(Target.Id), Target))
                throw new ContentException(Id + ": public visibility needs its actual immutable entity prototype");
        }
    }

    public sealed class KnowledgeVisibilityPolicyDef : Def, IKnowledgeVisibilityDefinition
    {
        private KnowledgeVisibilityDeclaration _declaration;
        public bool OwnEntities => _declaration.OwnEntities;
        public bool UnlistedEntities => _declaration.UnlistedEntities;
        public bool UnlistedContent => _declaration.UnlistedContent;
        public IReadOnlyList<IVisibilityTarget> Rules => _declaration.Rules;
        protected override void ReadCore(JsonReader reader) => _declaration = KnowledgeVisibilityDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }

    /// <summary>The sole selected visibility policy; learned facts remain CompanyKnowledgeService state.</summary>
    public sealed class KnowledgeVisibility : IVisibility, ISpatialSampleVisibility, ICompositionValidator
    {
        private readonly World _world;
        private readonly IKnowledgeVisibilityDefinition _policy;
        private bool _evaluating;
        public KnowledgeVisibility(World world, IKnowledgeVisibilityDefinition policy)
        { _world = world ?? throw new ArgumentNullException(nameof(world)); _policy = policy ?? throw new ArgumentNullException(nameof(policy)); }
        private void CheckEntry()
        {
            _world.RequireHealthyUpdate();
            if (!_evaluating) return;
            var error = new InvalidOperationException("visibility rule cannot recursively enter its visibility owner");
            _world.LatchFault("visibility.rule", error); throw error;
        }
        private T Observe<T>(object provider, Entity entity, Func<T> read)
        {
            CheckEntry(); _evaluating = true;
            try
            {
                var codec = EntityObservation.CaptureProvider(_world, entity,
                    entity == null ? (Action<IStateSync>)null : entity.Sync, "visibility.rule");
                T result = EntityObservation.ReadProvider(_world, provider, entity, read, _world.Space);
                codec.Verify(); return result;
            }
            finally { _evaluating = false; }
        }
        private readonly struct PolicyFacts
        {
            internal readonly bool Own, Entities, Content;
            internal readonly IReadOnlyList<RuleFacts> Rules;
            internal PolicyFacts(bool own, bool entities, bool content, IReadOnlyList<RuleFacts> rules)
            { Own = own; Entities = entities; Content = content; Rules = rules; }
        }
        private sealed class TargetIdentity : IEqualityComparer<IContentDefinition>
        {
            internal static readonly TargetIdentity Instance = new TargetIdentity();
            public bool Equals(IContentDefinition left, IContentDefinition right) => ReferenceEquals(left, right);
            public int GetHashCode(IContentDefinition value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
        }
        private readonly struct RuleFacts
        {
            internal readonly IVisibilityTarget Rule;
            private readonly IContentDefinition _first;
            private readonly IReadOnlyList<IContentDefinition> _remaining;
            internal RuleFacts(IVisibilityTarget rule, IContentDefinition first, IReadOnlyList<IContentDefinition> remaining)
            { Rule = rule; _first = first; _remaining = remaining; }
            internal bool Matches(IContentDefinition definition)
            {
                if (_first != null && ReferenceEquals(_first, definition)) return true;
                if (_remaining != null) foreach (var target in _remaining) if (ReferenceEquals(target, definition)) return true;
                return false;
            }
        }
        private RuleFacts ReadRule(IVisibilityTarget rule)
        {
            KnowledgeVisibilityContracts.ValidateAlias(_world, rule);
            var targets = rule.Targets;
            if (targets == null || targets.Count == 0) throw new ContentException(rule.Id + ": explicit visibility targets required");
            // Most rules name one target. Keep that detached reference without allocating
            // a list/hash set; still enumerate and validate every current provider target.
            IContentDefinition first = null;
            List<IContentDefinition> remaining = null;
            HashSet<IContentDefinition> seen = null;
            foreach (var target in targets)
            {
                if (!(target is Def definition) || !definition.IsFrozen || !_world.Content.Owns(target))
                    throw new ContentException(rule.Id + ": visibility targets must be unique actual frozen content");
                if (first == null) { first = target; continue; }
                if (remaining == null)
                {
                    remaining = new List<IContentDefinition>();
                    seen = new HashSet<IContentDefinition>(TargetIdentity.Instance) { first };
                }
                if (!seen.Add(target)) throw new ContentException(rule.Id + ": visibility targets must be unique actual frozen content");
                remaining.Add(target);
            }
            return new RuleFacts(rule, first, remaining?.AsReadOnly());
        }
        private PolicyFacts ReadPolicy(IKnowledgeVisibilityDefinition definition)
            => Observe(definition, null, () => {
                KnowledgeVisibilityContracts.ValidateAlias(_world, definition);
                bool own = definition.OwnEntities, entities = definition.UnlistedEntities, content = definition.UnlistedContent;
                var rules = definition.Rules;
                if (rules == null) throw new ContentException(definition.Id + ": explicit visibility rules required");
                string previous = null; var actual = new List<IVisibilityTarget>();
                foreach (var rule in rules)
                {
                    KnowledgeVisibilityContracts.ValidateAlias(_world, rule);
                    if (previous != null && string.CompareOrdinal(previous, rule.Id) >= 0)
                        throw new ContentException(definition.Id + ": visibility rules must be canonical and unique");
                    previous = rule.Id; actual.Add(rule);
                }
                foreach (IKnowledgeVisibilityRuleBinding binding in _world.Content.All<IKnowledgeVisibilityRuleBinding>())
                {
                    var attached = EntityObservation.ReadProvider(_world, binding, null, () => {
                        if (!(binding is Def def) || !def.IsFrozen || !_world.Content.Owns(binding)
                            || !ReferenceEquals(_world.Content.Find<IKnowledgeVisibilityRuleBinding>(binding.Id), binding))
                            throw new ContentException("visibility binding requires actual immutable content");
                        KnowledgeVisibilityContracts.ValidateAlias(_world, binding.Policy);
                        if (binding.Rules == null || binding.Rules.Count == 0)
                            throw new ContentException(binding.Id + ": explicit visibility binding rules required");
                        string last = null; var result = new List<IVisibilityTarget>();
                        foreach (IVisibilityTarget rule in binding.Rules)
                        {
                            KnowledgeVisibilityContracts.ValidateAlias(_world, rule);
                            if (last != null && string.CompareOrdinal(last, rule.Id) >= 0)
                                throw new ContentException(binding.Id + ": visibility binding rules must be canonical and unique");
                            last = rule.Id; result.Add(rule);
                        }
                        return (binding.Policy, result);
                    });
                    if (ReferenceEquals(attached.Item1, definition)) actual.AddRange(attached.Item2);
                }
                actual.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                for (int i = 1; i < actual.Count; i++)
                    if (actual[i].Id == actual[i - 1].Id)
                        throw new ContentException(definition.Id + ": duplicate composed visibility rule");
                // This count belongs to our already validated local list, not a provider
                // capacity promise. No mutable metadata or completed observation is cached.
                var facts = new RuleFacts[actual.Count];
                for (int i = 0; i < facts.Length; i++) facts[i] = ReadRule(actual[i]);
                return new PolicyFacts(own, entities, content, Array.AsReadOnly(facts));
            });
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ContentException("foreign visibility world");
            foreach (IVisibilityTarget rule in world.Content.All<IVisibilityTarget>()) Observe(rule, null, () => ReadRule(rule));
            foreach (IKnowledgeVisibilityDefinition definition in world.Content.All<IKnowledgeVisibilityDefinition>()) _ = ReadPolicy(definition);
            foreach (var facts in ReadPolicy(_policy).Rules)
                Observe(facts.Rule, null, () => { facts.Rule.ValidateBinding(world); return true; });
        }
        private bool Allows(PlayerId viewer, IContentDefinition definition, bool unlisted, IReadOnlyList<RuleFacts> rules)
        {
            bool matched = false;
            foreach (var facts in rules)
                if (facts.Matches(definition))
                { matched = true; if (!Observe(facts.Rule, null, () => facts.Rule.Allows(_world, viewer))) return false; }
            return matched || unlisted;
        }
        public bool CanSee(World world, PlayerId viewer, Entity entity)
        {
            CheckEntry();
            if (!ReferenceEquals(world, _world) || entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity)) return false;
            if (viewer == PlayerId.None) return true; if (!world.Players.IsSeat(viewer)) return false;
            var policy = ReadPolicy(_policy);
            if (policy.Own && entity.Owner == viewer) return true;
            bool matched = false;
            foreach (var facts in policy.Rules)
                if (facts.Matches(entity.Def))
                {
                    IVisibilityTarget rule = facts.Rule;
                    matched = true;
                    if (!Observe(rule, entity, () => rule.Allows(world, viewer))
                        || rule is IVisibilityEntityTarget scoped && !Observe(rule, entity, () => scoped.AllowsEntity(world, viewer, entity))) return false;
                }
            return matched || policy.Entities;
        }
        public bool CanSeeContent(World world, PlayerId viewer, IContentDefinition definition)
        {
            CheckEntry();
            if (!ReferenceEquals(world, _world) || !world.Content.Owns(definition)) return false;
            if (viewer == PlayerId.None) return true; if (!world.Players.IsSeat(viewer)) return false;
            var policy = ReadPolicy(_policy); return Allows(viewer, definition, policy.Content, policy.Rules);
        }
        public bool CanSeeSample(World world, PlayerId viewer, ISpatialLayerDefinition layer, SpatialSnapshot sample)
        {
            CheckEntry();
            if (!CanSeeContent(world, viewer, layer) || sample == null || sample.Space != layer.Space.Id) return false;
            if (viewer == PlayerId.None) return true;
            foreach (var facts in ReadPolicy(_policy).Rules)
                if (facts.Matches(layer) && facts.Rule is IVisibilitySampleTarget scoped
                    && !Observe(facts.Rule, null, () => scoped.AllowsSample(world, viewer, sample))) return false;
            return true;
        }
        public void CopyObservationKeys(World world, PlayerId viewer, ISpatialLayerDefinition layer, IList<string> destination)
        {
            CheckEntry();
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            if (!CanSeeContent(world, viewer, layer) || viewer == PlayerId.None) return;
            var values = new List<string>();
            foreach (var facts in ReadPolicy(_policy).Rules)
                if (facts.Matches(layer) && facts.Rule is IVisibilitySampleTarget scoped)
                {
                    IVisibilityTarget rule = facts.Rule;
                    var keys = Observe(rule, null, () => { var items = new List<string>(); scoped.CopyObservationKeys(world, viewer, items); return items; });
                    foreach (string key in keys)
                    {
                        if (key == null) throw new InvalidOperationException("visibility observation key cannot be null");
                        values.Add(rule.Id.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + rule.Id + key);
                    }
                }
            foreach (string value in values) destination.Add(value);
        }
    }
}
