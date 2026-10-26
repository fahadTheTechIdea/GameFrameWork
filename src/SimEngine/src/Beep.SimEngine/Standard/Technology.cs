using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface ITechnologyUnlock : IContentDefinition
    {
        bool Matches(World world, IContentDefinition definition);
    }
    /// <summary>Declared final work effect for earned technology completion, never an instant grant.</summary>
    public interface ITechnologyCompletionEffect : IContentDefinition, IWorkEffect
    { new string Id { get; } }
    public interface ITechnologyDefinition : IPlayerActivityDefinition, IContentDefinition, IDependencyNode
    {
        new string Id { get; }
        IReadOnlyList<ITechnologyUnlock> Unlocks { get; }
    }
    /// <summary>Sole typed reference authoring/matching; definitions never capture a session.</summary>
    public sealed class TechnologyUnlockDeclaration<T> where T : class, IContentDefinition
    {
        private string _target;
        public T Target { get; private set; }
        public void Read(JsonReader reader, string property) => _target = reader.String(property);
        public void Resolve(ContentResolver resolver) { Target = resolver.Def<T>(_target); _target = null; }
        public bool Matches(IContentDefinition definition) => ReferenceEquals(Target, definition);
    }
    public sealed class EntityTechnologyUnlockDef : Def, ITechnologyUnlock
    {
        private readonly TechnologyUnlockDeclaration<EntityDef> _declaration = new TechnologyUnlockDeclaration<EntityDef>();
        public EntityDef Target => _declaration.Target;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader, "definition");
        protected override void ResolveCore(ContentResolver resolver) => _declaration.Resolve(resolver);
        public bool Matches(World world, IContentDefinition definition) => _declaration.Matches(definition);
    }
    public sealed class ActivityTechnologyUnlockDef : Def, ITechnologyUnlock
    {
        private readonly TechnologyUnlockDeclaration<IAuthoredActivityDefinition> _declaration = new TechnologyUnlockDeclaration<IAuthoredActivityDefinition>();
        public IAuthoredActivityDefinition Target => _declaration.Target;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader, "definition");
        protected override void ResolveCore(ContentResolver resolver) => _declaration.Resolve(resolver);
        public bool Matches(World world, IContentDefinition definition) => _declaration.Matches(definition);
    }
    public sealed class TransformationTechnologyUnlockDef : Def, ITechnologyUnlock
    {
        private readonly TechnologyUnlockDeclaration<ITransformationDefinition> _declaration = new TechnologyUnlockDeclaration<ITransformationDefinition>();
        public ITransformationDefinition Target => _declaration.Target;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader, "definition");
        protected override void ResolveCore(ContentResolver resolver) => _declaration.Resolve(resolver);
        public bool Matches(World world, IContentDefinition definition) => _declaration.Matches(definition);
    }
    public sealed class LineTechnologyUnlockDef : Def, ITechnologyUnlock
    {
        private readonly TechnologyUnlockDeclaration<ILineDefinition> _declaration = new TechnologyUnlockDeclaration<ILineDefinition>();
        public ILineDefinition Target => _declaration.Target;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader, "definition");
        protected override void ResolveCore(ContentResolver resolver) => _declaration.Resolve(resolver);
        public bool Matches(World world, IContentDefinition definition) => _declaration.Matches(definition);
    }
    public static class TechnologyUnlockContracts
    {
        private static bool IsActual(ITechnologyUnlock unlock, World world)
            => unlock is Def actual && actual.IsFrozen && !(unlock is IPersistentService) &&
                unlock.Id == actual.Id && world.Content.Owns(unlock) &&
                ReferenceEquals(world.Content.Find<ITechnologyUnlock>(unlock.Id), unlock);
        internal static void Validate(ITechnologyUnlock unlock, World world)
            => EntityObservation.ReadProvider(world, unlock, null, () => {
                if (!IsActual(unlock, world)) throw new ContentException("technology unlock requires its actual immutable content alias");
                return true;
            }, world.Space);
        public static bool Matches(ITechnologyUnlock unlock, World world, IContentDefinition definition)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            return EntityObservation.ReadProvider(world, unlock, null, () => {
                if (!IsActual(unlock, world) || !world.Content.Owns(definition))
                    throw new InvalidOperationException("technology matching requires actual session content");
                return unlock.Matches(world, definition);
            }, world.Space);
        }
    }

    /// <summary>Sole prerequisite/unlock declaration for native and independent research content.
    /// ActivityDeclaration still owns the work graph; TechnologyService alone owns learned semantics.</summary>
    public sealed class TechnologyDeclaration
    {
        public IReadOnlyList<string> Dependencies { get; private set; }
        public IReadOnlyList<ITechnologyUnlock> Unlocks { get; private set; }
        private string[] _unlocks;
        private IReadOnlyList<ITechnologyDefinition> _prerequisites;
        public IReadOnlyList<IContentDefinition> DisclosureContent(IContentDefinition owner)
        {
            var references = new List<IContentDefinition> { owner };
            foreach (ITechnologyDefinition prerequisite in _prerequisites) references.Add(prerequisite);
            return references.AsReadOnly();
        }
        public void Read(JsonReader reader, IAuthoredActivityDefinition activity)
        {
            if (reader == null || activity == null) throw new ArgumentNullException();
            if (reader.Raw.Get("prerequisites")?.Kind != JsonKind.Array || reader.Raw.Get("unlocks")?.Kind != JsonKind.Array) throw reader.Error("technology needs explicit prerequisite and unlock arrays");
            var prerequisites = reader.Strings("prerequisites"); Array.Sort(prerequisites, StringComparer.Ordinal);
            Dependencies = Array.AsReadOnly(prerequisites); _unlocks = reader.Strings("unlocks");
            if (!activity.PlayerOrderable || activity.OnComplete == null)
                throw reader.Error("technology is player-orderable and requires a final completion effect, never entity replacement");
        }
        public void Resolve(ContentResolver resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            if (_unlocks == null) throw new InvalidOperationException("technology declaration resolves once");
            var prerequisites = new List<ITechnologyDefinition>();
            foreach (string prerequisite in Dependencies) prerequisites.Add(resolver.Def<ITechnologyDefinition>(prerequisite));
            _prerequisites = prerequisites.AsReadOnly();
            var unlocks = new List<ITechnologyUnlock>(); var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in _unlocks) { if (!ids.Add(id)) throw resolver.Fail("repeated technology unlock"); unlocks.Add(resolver.Def<ITechnologyUnlock>(id)); }
            Unlocks = unlocks.AsReadOnly(); _unlocks = null;
        }
    }
    /// <summary>Research is an ordinary authored graph; no alternate execution model.</summary>
    public class TechnologyDef : ActivityDef, ITechnologyDefinition
    {
        private readonly TechnologyDeclaration _technology = new TechnologyDeclaration();
        public IReadOnlyList<string> Dependencies => _technology.Dependencies;
        public override IReadOnlyList<IContentDefinition> DisclosureContent => _technology.DisclosureContent(this);
        public IReadOnlyList<ITechnologyUnlock> Unlocks => _technology.Unlocks;
        protected override void ReadCore(JsonReader reader)
        { base.ReadCore(reader); _technology.Read(reader, this); }
        protected override void ResolveCore(ContentResolver resolver)
        { base.ResolveCore(resolver); _technology.Resolve(resolver); }
        public override Code CheckIntent(World world, PlayerId company, Entity target)
        {
            Code check = base.CheckIntent(world, company, target);
            return !check.IsNone ? check : world.Services.Get<ITechnologyKnowledge>().CheckResearch(company, this);
        }
    }
    public interface ITechnologyKnowledge
    {
        Code CheckKnown(PlayerId company, string technology);
        Code CheckResearch(PlayerId company, ITechnologyDefinition technology);
        Code CheckCompletion(Order order);
        Code Complete(Order order);
        /// <summary>Private detached facts; missing, hidden and foreign technologies all return NotFound.</summary>
        Code ReadTechnology(PlayerId viewer, PlayerId company, string technology, out TechnologySnapshot snapshot);
        void CopyTechnologies(PlayerId viewer, IList<TechnologySnapshot> destination);
        Code CopyRoute(PlayerId company, string technology, IList<string> destination);
    }
    public static class TechnologyCodes
    {
        public static readonly Code MissingPrerequisite = new Code("technology.prerequisite");
        public static readonly Code Locked = new Code("technology.locked");
        public static readonly Code Known = new Code("technology.known");
        public static readonly Code Active = new Code("technology.active");
    }
    public readonly struct TechnologySnapshot
    {
        public PlayerId Company { get; }
        public string Technology { get; }
        public bool Known { get; }
        public long? CompletedTick { get; }
        public int ActiveOrder { get; }
        public Code Blocker { get; }
        public IReadOnlyList<string> Prerequisites { get; }
        internal TechnologySnapshot(PlayerId company, ITechnologyDefinition technology, bool known, long? tick, int order, Code blocker)
        { Company = company; Technology = technology.Id; Known = known; CompletedTick = tick; ActiveOrder = order; Blocker = blocker;
            Prerequisites = new List<string>(technology.Dependencies).AsReadOnly(); }
    }
    public sealed class TechnologyService : ITechnologyKnowledge, IKnowledgeProvider, ICompositionValidator, IContentAccessRule
    {
        private readonly World _world;
        private DependencyGraph<ITechnologyDefinition> _graph;
        public TechnologyService(World world) { _world = world ?? throw new ArgumentNullException(nameof(world)); }
        public string Id => TechnologyModule.ModuleId;
        public World World => _world;
        public int SchemaVersion => 1;
        public KnowledgeRetention Retention(string subject) => KnowledgeRetention.FirstLearned;
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ContentException("technology belongs to another session");
            foreach (ITechnologyUnlock unlock in world.Content.All<ITechnologyUnlock>()) TechnologyUnlockContracts.Validate(unlock, world);
            _graph = new DependencyGraph<ITechnologyDefinition>(world.Content.All<ITechnologyDefinition>(), Id);
            var owners = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (ITechnologyDefinition definition in world.Content.All<ITechnologyDefinition>())
            {
                ValidateDefinition(world, definition);
                foreach (ITechnologyUnlock unlock in definition.Unlocks)
                {
                    if (owners.ContainsKey(unlock.Id)) throw new ContentException("duplicate technology unlock owner for " + unlock.Id);
                    owners.Add(unlock.Id, definition.Id);
                }
            }
        }
        private static void ValidateDefinition(World world, ITechnologyDefinition definition)
        {
            if (!world.Content.Owns(definition) || !ReferenceEquals(world.Content.Find<ITechnologyDefinition>(definition.Id), definition))
                throw new ContentException(definition.Id + ": technology must be an unambiguous actual content alias");
            if (definition.Graph == null || definition.Graph.Nodes.Count == 0 || !HasDuration(definition))
                throw new ContentException(definition.Id + ": research requires positive elapsed work");
            if (world.Content.Find<ITechnologyCompletionEffect>(definition.OnComplete) == null)
                throw new ContentException(definition.Id + ": research requires a technology completion effect");
            foreach (IActivityNodeDefinition node in definition.Graph.Nodes)
                if (world.Content.Find<IWorkEffect>(node.Effect) is IEntityReplacementEffect)
                    throw new ContentException(definition.Id + ": technology cannot replace its research owner");
            if (definition.Unlocks == null) throw new ContentException(definition.Id + ": explicit unlock declarations required");
            var unlocks = new HashSet<string>(StringComparer.Ordinal);
            foreach (ITechnologyUnlock unlock in definition.Unlocks)
                if (unlock == null || !world.Content.Owns(unlock) || !unlocks.Add(unlock.Id)
                    || !ReferenceEquals(world.Content.Find<ITechnologyUnlock>(unlock.Id), unlock))
                    throw new ContentException(definition.Id + ": unlocks require unique actual owned content aliases");
        }
        private static bool HasDuration(ITechnologyDefinition definition)
        { foreach (IActivityNodeDefinition node in definition.Graph.Nodes) if (!node.Duration.IsZero) return true; return false; }
        private KnowledgeObservation? Find(PlayerId company, string id)
            => _world.Services.Get<ICompanyKnowledge>().TryRead(company, company, Id, id, out var fact) ? fact : (KnowledgeObservation?)null;
        private Order Active(PlayerId company, ITechnologyDefinition technology)
        {
            foreach (Order order in _world.Services.Get<WorkBoard>().Orders)
                if (order.IsActive && order.Owner == company && ReferenceEquals(order.Plan, technology)) return order;
            return null;
        }
        public Code CopyRoute(PlayerId company, string technology, IList<string> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear(); _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            ITechnologyDefinition root = _graph.Find(technology);
            if (root == null || !CanDisclose(company, root)) return Codes.UnknownDef;
            if (Find(company, technology) != null) return Code.None;
            var facts = new List<string>();
            foreach (string id in _graph.PrerequisiteOrder(technology)) if (Find(company, id) == null) facts.Add(id);
            foreach (string id in facts) destination.Add(id);
            return Code.None;
        }

        public Code CheckKnown(PlayerId company, string technology)
        {
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            if (_world.Content.Find<ITechnologyDefinition>(technology) == null) return Codes.UnknownDef;
            return Find(company, technology) != null ? Code.None : TechnologyCodes.Locked;
        }
        public Code CheckResearch(PlayerId company, ITechnologyDefinition technology)
        {
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            if (!_world.Content.Owns(technology)) return Codes.UnknownDef;
            if (!CanDisclose(company, technology)) return Codes.UnknownDef;
            return EntityObservation.ReadProvider(_world, technology, null, () => {
                if (!ReferenceEquals(_world.Content.Find<ITechnologyDefinition>(technology.Id), technology)) return Codes.UnknownDef;
                if (Find(company, technology.Id) != null) return TechnologyCodes.Known;
                foreach (string prerequisite in technology.Dependencies) if (Find(company, prerequisite) == null) return TechnologyCodes.MissingPrerequisite;
                return Active(company, technology) == null ? Code.None : TechnologyCodes.Active;
            });
        }
        Code IContentAccessRule.Check(PlayerId player, IContentDefinition content)
        {
            _world.RequireHealthyUpdate();
            foreach (ITechnologyDefinition technology in _world.Content.All<ITechnologyDefinition>())
                foreach (ITechnologyUnlock unlock in technology.Unlocks)
                    if (TechnologyUnlockContracts.Matches(unlock, _world, content) && Find(player, technology.Id) == null) return TechnologyCodes.Locked;
            return Code.None;
        }
        // Disclosure is original visibility over actual prerequisites, not another learned ledger.
        private bool CanDisclose(PlayerId viewer, ITechnologyDefinition root)
        {
            _world.RequireHealthyUpdate();
            var pending = new Stack<ITechnologyDefinition>(); var visited = new HashSet<string>(StringComparer.Ordinal);
            var view = _world.ViewFor(viewer); pending.Push(root);
            while (pending.Count > 0)
            {
                ITechnologyDefinition definition = pending.Pop();
                bool visible = EntityObservation.ReadProvider(_world, definition, null, () => {
                    if (!visited.Add(definition.Id)) return true;
                    if (!view.CanSeeContent(definition)) return false;
                    foreach (string prerequisite in definition.Dependencies) pending.Push(_world.Content.Get<ITechnologyDefinition>(prerequisite));
                    return true;
                });
                if (!visible) return false;
            }
            return true;
        }
        public Code CheckCompletion(Order order)
        {
            _world.RequireHealthyUpdate();
            if (order == null || !ReferenceEquals(_world.Services.Get<WorkBoard>().Order(order.Id), order)) return Codes.NotFound;
            if (!(order.Plan is ITechnologyDefinition technology) || !_world.Content.Owns(technology) || !ReferenceEquals(_world.Content.Find<ITechnologyDefinition>(technology.Id), technology) ||
                order.State != OrderState.Completing || order.Plan.OnComplete == null) return WorkEffectCodes.NotComplete;
            Entity target = _world.Entities.Get(order.Target); if (target == null) return Codes.NotFound;
            if (target.Owner != order.Owner) return Codes.NotOwned;
            foreach (ActivityNodeExecution node in order.Nodes) if (node.State != NodeState.Done) return WorkEffectCodes.NotComplete;
            if (Find(order.Owner, technology.Id) != null) return TechnologyCodes.Known;
            foreach (string prerequisite in technology.Dependencies) if (Find(order.Owner, prerequisite) == null) return TechnologyCodes.MissingPrerequisite;
            return Code.None;
        }
        public Code Complete(Order order)
        {
            Code check = CheckCompletion(order); if (!check.IsNone) return check;
            var technology = (ITechnologyDefinition)order.Plan;
            Code recorded = _world.Services.Get<ICompanyKnowledge>().Record(order.Owner, Id, technology.Id, JsonValue.NewObject());
            if (!recorded.IsNone) return recorded;
            _world.Services.Get<WorkBoard>().Dirty = true; return Code.None;
        }
        public Code ReadTechnology(PlayerId viewer, PlayerId company, string technology, out TechnologySnapshot snapshot)
        {
            snapshot = default;
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company) || viewer != PlayerId.None &&
                (!_world.Players.IsSeat(viewer) || viewer != company)) return Codes.NotFound;
            ITechnologyDefinition definition = _world.Content.Find<ITechnologyDefinition>(technology);
            if (definition == null) return Codes.NotFound;
            return ReadTechnologyFact(viewer, company, definition, out snapshot);
        }
        private Code ReadTechnologyFact(PlayerId viewer, PlayerId company, ITechnologyDefinition definition, out TechnologySnapshot snapshot)
        {
            snapshot = default;
            TechnologySnapshot fact = default;
            bool disclosed = EntityObservation.ReadProvider(_world, definition, null, () => {
                if (!CanDisclose(viewer, definition)) return false;
                KnowledgeObservation? learned = Find(company, definition.Id); Order order = Active(company, definition);
                fact = new TechnologySnapshot(company, definition, learned != null, learned?.LearnedTick,
                    order?.Id ?? 0, CheckResearch(company, definition));
                return true;
            });
            _world.RequireHealthyUpdate();
            if (!disclosed) return Codes.NotFound;
            snapshot = fact;
            return Code.None;
        }
        public void CopyTechnologies(PlayerId viewer, IList<TechnologySnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear(); _world.RequireHealthyUpdate();
            var facts = new List<TechnologySnapshot>();
            if (viewer != PlayerId.None && !_world.Players.IsSeat(viewer)) return;
            for (int company = 0; company < _world.Players.Count; company++)
            {
                var owner = new PlayerId(company); if (viewer != PlayerId.None && owner != viewer) continue;
                foreach (ITechnologyDefinition definition in _world.Content.All<ITechnologyDefinition>())
                    if (ReadTechnologyFact(viewer, owner, definition, out var fact).IsNone) facts.Add(fact);
            }
            _world.RequireHealthyUpdate();
            foreach (TechnologySnapshot fact in facts) destination.Add(fact);
        }
        public Code CheckObservation(string subject, JsonValue data)
            => _world.Content.Find<ITechnologyDefinition>(subject) == null ? Codes.UnknownDef
                : data.Kind == JsonKind.Object && data.Count == 0 ? Code.None : Codes.InvalidArgument;
        public void ValidateKnowledge(IReadOnlyList<KnowledgeObservation> observations)
        {
            foreach (KnowledgeObservation learned in observations)
                foreach (string prerequisite in _world.Content.Get<ITechnologyDefinition>(learned.Subject).Dependencies)
                {
                    bool known = false;
                    foreach (KnowledgeObservation previous in observations)
                        if (previous.Company == learned.Company && previous.Subject == prerequisite && previous.LearnedTick <= learned.LearnedTick) { known = true; break; }
                    if (!known) throw new CorruptStateException("learned technology precedes required knowledge");
                }
        }
    }
    /// <summary>One earned-completion delegation for native and independent research effects.</summary>
    public static class TechnologyCompletionContracts
    {
        public static void ValidatePlan(IWorkEffect effect, IActivityDefinition plan, IActivityNodeDefinition node)
        {
            if (!(plan is ITechnologyDefinition) || node != null || plan.OnComplete != effect.Id)
                throw new ContentException(effect.Id + ": technology completion is a research final effect only");
        }
        public static Code Check(World world, Order order, IActivityNodeDefinition node)
            => node != null ? WorkEffectCodes.NotComplete : world.Services.Get<ITechnologyKnowledge>().CheckCompletion(order);
        public static WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => WorkEffectContracts.Outcome(world, node != null ? WorkEffectCodes.NotComplete : world.Services.Get<ITechnologyKnowledge>().Complete(order));
    }
    public sealed class TechnologyCompletionDef : Def, ITechnologyCompletionEffect, IWorkEffectPlanValidator, IWorkEffectPreview
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        protected override void ReadCore(JsonReader reader) { }
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
            => TechnologyCompletionContracts.ValidatePlan(this, plan, node);
        public Code Check(World world, Order order, IActivityNodeDefinition node)
            => TechnologyCompletionContracts.Check(world, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => TechnologyCompletionContracts.Apply(world, order, node);
    }
    public sealed class TechnologyRequirementDef : WorkRequirementDef
    {
        public override System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.AsReadOnly(new IContentDefinition[] { Technology });
        private string _technology;
        public ITechnologyDefinition Technology { get; private set; }
        protected override void ReadCore(JsonReader reader) => _technology = reader.String("technology");
        protected override void ResolveCore(ContentResolver resolver) => Technology = resolver.Def<ITechnologyDefinition>(_technology);
        public override WorkRequirementResult Evaluate(WorkRequirementContext context)
        { Code check = context.World.Services.Get<ITechnologyKnowledge>().CheckKnown(context.Owner, Technology.Id);
            return check.IsNone ? WorkRequirementResult.Satisfied() : WorkRequirementResult.Pending(check); }
    }
}
