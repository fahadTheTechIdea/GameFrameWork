using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IPlayerActivityDefinition : IPlanDefinition
    {
        int Priority { get; }
        /// <summary>Pure admission; must not mutate the target, providers, selected space or game clock.</summary>
        Code CheckIntent(World world, PlayerId company, Entity target);
    }

    /// <summary>Optional intent capability on the existing graph definition, not another content catalog.</summary>
    public interface IPlanDefinition : IActivityDefinition
    {
        /// <summary>Pure complete unique entity references in this provider's intent schema; never infer IDs from numbers.</summary>
        IReadOnlyList<EntityId> ReadEntityReferences(World world, JsonValue arguments);
        /// <summary>Pure metadata admission for replacing a referenced entity, without creating the destination.</summary>
        Code CheckEntityReplacement(World world, PlayerId company, JsonValue arguments, Entity source, EntityDef destination);
        /// <summary>Pure argument rewrite; change only the requested live reference, never progress or historical facts.</summary>
        JsonValue ReplaceEntityReference(World world, JsonValue arguments, EntityId source, EntityId destination);
        /// <summary>Purely bind the sole immutable graph to its actual native execution target;
        /// fixed graphs return this definition. Must not mutate the target, providers, space or clock.</summary>
        IActivityDefinition BindExecution(World world, EntityId target);
        /// <summary>Pure provider-owned stable identity; malformed argument schemas throw ContentException.</summary>
        string IntentIdentity(World world, JsonValue arguments);
        /// <summary>Pure identity of the actual native target/placement, checked against intent on
        /// admission/checkpoints. The supplied order is read-only; never change its fields or binding.</summary>
        string ExecutionIdentity(World world, Order order);
        /// <summary>Pure admission to the intent board; may allow a currently blocked physical placement.</summary>
        Code CheckSubmission(World world, PlayerId company, JsonValue arguments);
        /// <summary>Pure current admission to the actual native workflow.</summary>
        Code CheckActivation(World world, PlayerId company, JsonValue arguments);
        /// <summary>Revalidate and create exactly one existing WorkBoard order, never a copied execution.</summary>
        Order Activate(World world, PlayerId company, JsonValue arguments, int priority);
    }
    /// <summary>Target-intent authoring belongs to the same plan provider, including its JSON schema.</summary>
    public interface IEntityTargetPlanDefinition : IPlanDefinition
    {
        /// <summary>Validate ordinary authored targeting; null validates orderability without choosing a live target.</summary>
        void ValidateTargetDefinition(EntityDef target);
        PlanIntent CreateTargetIntent(EntityId target, IActivityWorkerSelection workers, string group, int priority);
    }

    /// <summary>Exact immutable cancellation intent; IDs, never mutable group labels, identify targets.</summary>
    public sealed class PlanCancellationSelection
    {
        public IReadOnlyList<long> Plans { get; }
        public bool PendingOnly { get; }
        public PlanCancellationSelection(bool pendingOnly, IEnumerable<long> plans)
        {
            if (plans == null) throw new ArgumentNullException(nameof(plans));
            Plans = PlanIntent.CaptureDependencies(plans);
            if (Plans.Count == 0) throw new ArgumentException("cancellation requires actual plan identities");
            PendingOnly = pendingOnly;
        }
        internal static void Sync(IStateSync sync, ref PlanCancellationSelection selection)
        {
            bool present = selection != null; sync.Bool(ref present);
            if (!present) { if (sync.Reading) selection = null; return; }
            bool pendingOnly = selection?.PendingOnly ?? false; sync.Bool(ref pendingOnly);
            IReadOnlyList<long> plans = selection?.Plans; PlanIntent.SyncDependencies(sync, ref plans);
            if (sync.Reading)
                try { selection = new PlanCancellationSelection(pendingOnly, plans); }
                catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
        }
    }
    public enum PlanState : byte { Pending = 0, Active = 1, Done = 2, Failed = 3, Cancelled = 4 }

    /// <summary>Immutable provenance on the actual plan. Default denotes explicit player intent.</summary>
    public readonly struct PlanOrigin
    {
        public EntityId Entity { get; }
        public Code Source { get; }
        public long CycleHead { get; }
        public bool TemplateCycle { get; }
        public bool IsManual => Entity == EntityId.None && Source.IsNone && CycleHead == 0 && !TemplateCycle;
        public PlanOrigin(EntityId entity, Code source, long cycleHead, bool templateCycle)
        {
            if (!entity.IsValid || source.IsNone || cycleHead <= 0) throw new ArgumentException("recurring origin requires actual source and cycle-head identities");
            ContractIdentity.ValidateId(source.Id, "plan origin source"); Entity = entity; Source = source;
            CycleHead = cycleHead; TemplateCycle = templateCycle;
        }
        internal static PlanOrigin Sync(IStateSync sync, PlanOrigin current)
        {
            EntityId entity = current.Entity; string source = current.Source.Id;
            long head = current.CycleHead; bool template = current.TemplateCycle;
            sync.Entity(ref entity); sync.String(ref source); sync.Long(ref head); sync.Bool(ref template);
            if (entity == EntityId.None && source == null && head == 0 && !template) return default;
            try { return new PlanOrigin(entity, source == null ? Code.None : new Code(source), head, template); }
            catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
        }
    }

    /// <summary>Immutable intent using a master-authored graph and provider-owned JSON argument schema.</summary>
    public sealed class PlanIntent
    {
        public string Definition { get; }
        public JsonValue Arguments { get; }
        public string Group { get; }
        public int Priority { get; }
        public bool Paused { get; }
        public IReadOnlyList<long> Dependencies { get; }
        public PlanIntent(string definition, JsonValue arguments, string group, int priority, bool paused, IEnumerable<long> dependencies = null)
        {
            if (string.IsNullOrWhiteSpace(definition) || arguments == null || !arguments.IsObject || priority < 0)
                throw new ArgumentException("a plan needs a definition key, object arguments and nonnegative priority");
            if (group != null) ContractIdentity.ValidateId(group, "plan group");
            Definition = definition; Arguments = arguments.Clone(); Arguments.Freeze();
            Group = group; Priority = priority; Paused = paused;
            Dependencies = CaptureDependencies(dependencies);
        }
        internal static IReadOnlyList<long> CaptureDependencies(IEnumerable<long> dependencies)
        {
            var links = dependencies == null ? new List<long>() : new List<long>(dependencies); links.Sort();
            for (int i = 0; i < links.Count; i++)
                if (links[i] <= 0 || i > 0 && links[i] == links[i - 1]) throw new ArgumentException("plan prerequisites must be positive unique IDs");
            return links.AsReadOnly();
        }
        internal string EncodedArguments()
        { var text = new StringBuilder(); Arguments.WriteCanonical(text); return text.ToString(); }
        internal static void Sync(IStateSync sync, ref PlanIntent intent)
        {
            bool present = intent != null; sync.Bool(ref present);
            if (!present) { if (sync.Reading) intent = null; return; }
            string definition = intent?.Definition, group = intent?.Group, arguments = intent?.EncodedArguments();
            int priority = intent?.Priority ?? 0; bool paused = intent?.Paused ?? false;
            sync.String(ref definition); sync.String(ref arguments); sync.String(ref group); sync.Int(ref priority); sync.Bool(ref paused);
            IReadOnlyList<long> dependencies = intent?.Dependencies; SyncDependencies(sync, ref dependencies);
            if (sync.Reading)
                try { intent = new PlanIntent(definition, Json.Parse(arguments), group, priority, paused, dependencies); }
                catch (Exception error) when (error is ArgumentException || error is ContentException)
                { throw new CorruptStateException("invalid saved plan intent: " + error.Message); }
        }
        internal static void SyncDependencies(IStateSync sync, ref IReadOnlyList<long> dependencies)
        {
            int count = sync.Count(dependencies?.Count ?? 0); var decoded = sync.Reading ? new List<long>() : null;
            long previous = 0;
            for (int i = 0; i < count; i++)
            {
                long id = sync.Reading ? 0 : dependencies[i]; sync.Long(ref id);
                if (id <= previous) throw new CorruptStateException("plan prerequisites must be canonical positive unique IDs");
                previous = id; if (sync.Reading) decoded.Add(id);
            }
            if (sync.Reading) dependencies = decoded.AsReadOnly();
        }
    }

    /// <summary>Submission-only link between zero-based group member positions; persisted links use actual plan IDs.</summary>
    public readonly struct PlanGroupDependency
    {
        public int Entry { get; }
        public int Prerequisite { get; }
        public PlanGroupDependency(int entry, int prerequisite)
        {
            if (entry < 0 || prerequisite < 0 || entry == prerequisite) throw new ArgumentException("group links require distinct nonnegative positions");
            Entry = entry; Prerequisite = prerequisite;
        }
    }
    public sealed class PlanGroupIntent
    {
        public string Group { get; }
        public IReadOnlyList<PlanIntent> Entries { get; }
        public IReadOnlyList<PlanGroupDependency> Dependencies { get; }
        public PlanGroupIntent(string group, IEnumerable<PlanIntent> entries, IEnumerable<PlanGroupDependency> dependencies = null)
        {
            ContractIdentity.ValidateId(group, "plan group");
            var copy = entries == null ? throw new ArgumentNullException(nameof(entries)) : new List<PlanIntent>(entries);
            if (copy.Count == 0) throw new ArgumentException("a group requires nonempty explicit intents");
            foreach (PlanIntent entry in copy) if (entry == null || entry.Group != group) throw new ArgumentException("every group entry explicitly names its group");
            var links = dependencies == null ? new List<PlanGroupDependency>() : new List<PlanGroupDependency>(dependencies);
            links.Sort((a, b) => a.Entry != b.Entry ? a.Entry.CompareTo(b.Entry) : a.Prerequisite.CompareTo(b.Prerequisite));
            var nodes = new List<GroupNode>(); for (int i = 0; i < copy.Count; i++) nodes.Add(new GroupNode(i));
            foreach (var link in links)
            {
                if (link.Entry >= copy.Count || link.Prerequisite >= copy.Count || link.Entry < 0 || link.Prerequisite < 0)
                    throw new ArgumentException("group dependency position is outside submitted membership");
                nodes[link.Entry].After.Add(link.Prerequisite.ToString(CultureInfo.InvariantCulture));
            }
            try { _ = new DependencyGraph<GroupNode>(nodes, "plan group " + group); }
            catch (ContentException error) { throw new ArgumentException(error.Message); }
            Group = group; Entries = copy.AsReadOnly();
            Dependencies = links.AsReadOnly();
        }
        private sealed class GroupNode : IDependencyNode
        {
            internal readonly List<string> After = new List<string>();
            internal GroupNode(int position) { Id = position.ToString(CultureInfo.InvariantCulture); }
            public string Id { get; }
            public IReadOnlyList<string> Dependencies => After;
        }
        internal static void Sync(IStateSync sync, ref PlanGroupIntent intent)
        {
            bool present = intent != null; sync.Bool(ref present); if (!present) { if (sync.Reading) intent = null; return; }
            string group = intent?.Group; sync.String(ref group); int count = sync.Count(intent?.Entries.Count ?? 0);
            var entries = sync.Reading ? new List<PlanIntent>() : null;
            for (int i = 0; i < count; i++) { PlanIntent entry = sync.Reading ? null : intent.Entries[i]; PlanIntent.Sync(sync, ref entry); if (sync.Reading) entries.Add(entry); }
            int linkCount = sync.Count(intent?.Dependencies.Count ?? 0); var links = sync.Reading ? new List<PlanGroupDependency>() : null;
            int previousEntry = -1, previousPrerequisite = -1;
            for (int i = 0; i < linkCount; i++)
            {
                int entry = sync.Reading ? 0 : intent.Dependencies[i].Entry, prerequisite = sync.Reading ? 0 : intent.Dependencies[i].Prerequisite;
                sync.Int(ref entry); sync.Int(ref prerequisite);
                if (entry < previousEntry || entry == previousEntry && prerequisite <= previousPrerequisite)
                    throw new CorruptStateException("group prerequisite links must be canonical and unique");
                previousEntry = entry; previousPrerequisite = prerequisite;
                if (sync.Reading)
                    try { links.Add(new PlanGroupDependency(entry, prerequisite)); }
                    catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
            }
            if (sync.Reading)
                try { intent = new PlanGroupIntent(group, entries, links); }
                catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
        }
    }
    public readonly struct PlanGroupEntryCheck
    {
        /// <summary>Actual stable ID, or zero for a not-yet-submitted intent at this array position.</summary>
        public long Plan { get; }
        public Code Blocker { get; }
        public PlanGroupEntryCheck(long plan, Code blocker) { Plan = plan; Blocker = blocker; }
    }
    public sealed class PlanGroupCheck
    {
        public Code Blocker { get; }
        public bool Ready => Blocker.IsNone;
        public IReadOnlyList<PlanGroupEntryCheck> Entries { get; }
        public PlanGroupCheck(Code blocker, IEnumerable<PlanGroupEntryCheck> entries = null)
        { Blocker = blocker; Entries = entries == null ? Array.Empty<PlanGroupEntryCheck>() : new List<PlanGroupEntryCheck>(entries).AsReadOnly(); }
    }

    /// <summary>Detached intent facts; Target/Order identify the sole actual execution, not a ghost site.</summary>
    public readonly struct PlanView
    {
        public long Id { get; }
        public PlanIntent Intent { get; }
        public EntityId Target { get; }
        public PlanState State { get; }
        public int Order { get; }
        public long CreatedTick { get; }
        /// <summary>Timestamp from the sole game clock; -1 while live.</summary>
        public long EndedTick { get; }
        public PlanOrigin Origin { get; }
        /// <summary>Current recurring source binding, distinct from historical Origin.Entity; None for manual intent.</summary>
        public EntityId CurrentSource { get; }
        public Code Blocker { get; }
        public string Definition => Intent.Definition;
        public JsonValue Arguments => Intent.Arguments;
        public string Group => Intent.Group;
        public int Priority => Intent.Priority;
        public bool Paused => Intent.Paused;
        public IReadOnlyList<long> Dependencies => Intent.Dependencies;
        internal PlanView(long id, PlanIntent intent, EntityId target, PlanState state, int order,
            long createdTick, long endedTick, Code blocker, PlanOrigin origin, EntityId currentSource)
        { Id = id; Intent = intent; Target = target; State = state; Order = order; CreatedTick = createdTick; EndedTick = endedTick; Blocker = blocker; Origin = origin; CurrentSource = currentSource; }
    }

    public interface IPlanBoard
    {
        PlanHistoryPolicyView? ReadPlanHistoryPolicy();
        void CopyPlanHistory(PlayerId viewer, IList<PlanHistoryCohortView> destination);
        PlanGroupCheck PreviewRetirePlanHistory(PlayerId company, PlanHistorySelection selection);
        void RetirePlanHistory(PlayerId company, PlanHistorySelection selection);
        PlanGroupCheck PreviewStorePlanTemplate(PlayerId company, long id, long revision, string name, PlanTemplate template);
        StoredPlanTemplateView StorePlanTemplate(PlayerId company, long id, long revision, string name, PlanTemplate template);
        Code PreviewRemovePlanTemplate(PlayerId company, long id, long revision);
        void RemovePlanTemplate(PlayerId company, long id, long revision);
        Code ReadStoredPlanTemplate(PlayerId company, long id, out StoredPlanTemplateView value);
        void CopyStoredPlanTemplates(PlayerId company, IList<StoredPlanTemplateView> destination);
        Code ResolvePlanTemplate(PlayerId company, PlanTemplateSelection selection, out PlanTemplate template);
        bool CanDisclosePlanTemplate(PlayerId viewer, PlanTemplate template);
        PlanGroupCheck PreviewPlanTemplateStructure(PlanTemplate template, JsonValue anchor);
        PlanGroupCheck CapturePlanTemplate(PlayerId company, string group, JsonValue origin, out PlanTemplate template);
        PlanGroupCheck PreviewPlanTemplate(PlayerId company, PlanTemplate template, JsonValue anchor, string group);
        /// <summary>Pure current provider activation facts, not a placement or resource reservation.</summary>
        PlanGroupCheck PreviewPlanTemplateActivation(PlayerId company, PlanTemplate template, JsonValue anchor, string group);
        /// <summary>Pure whole admission; immutable intent is returned only on success, never an execution grant.</summary>
        PlanGroupCheck ExpandPlanTemplate(PlayerId company, PlanTemplate template, JsonValue anchor, string group, out PlanGroupIntent intent);
        /// <summary>Creates the whole intent in authored entry order with contiguous positive stable plan IDs.</summary>
        IReadOnlyList<long> PlacePlanTemplate(PlayerId company, PlanTemplate template, JsonValue anchor, string group);
        PlanGroupCheck PreviewPlanGroup(PlayerId company, PlanGroupIntent intent);
        PlanGroupCheck PreviewPlanGroupEdit(PlayerId company, string group, int priority, bool paused);
        PlanGroupCheck PreviewPlanGroupMove(PlayerId company, string group, long before);
        PlanGroupCheck PreviewPlanGroupCancel(PlayerId company, string group);
        PlanGroupCheck PreviewPlanGroupRepeat(PlayerId company, string group, string newGroup);
        IReadOnlyList<long> SubmitPlanGroup(PlayerId company, PlanGroupIntent intent);
        void ConfigurePlanGroup(PlayerId company, string group, int priority, bool paused);
        void MovePlanGroup(PlayerId company, string group, long before);
        void CancelPlanGroup(PlayerId company, string group);
        IReadOnlyList<long> RepeatPlanGroup(PlayerId company, string group, string newGroup);
        void CopyPlanGroup(PlayerId viewer, string group, IList<PlanView> destination);
        Code PreviewPlan(PlayerId company, PlanIntent intent);
        Code PreviewPlanEdit(PlayerId company, long plan, string group, int priority, bool paused);
        PlanGroupCheck PreviewPlanDependencies(PlayerId company, PlanDependencyChanges changes);
        Code PreviewPlanMove(PlayerId company, long plan, long before);
        PlanGroupCheck PreviewPlanCancel(PlayerId company, PlanCancellationSelection selection);
        Code PreviewPlanRepeat(PlayerId company, long plan);
        long SubmitPlan(PlayerId company, PlanIntent intent);
        void ConfigurePlan(PlayerId company, long plan, string group, int priority, bool paused);
        void ConfigurePlanDependencies(PlayerId company, PlanDependencyChanges changes);
        void MovePlan(PlayerId company, long plan, long before);
        void CancelPlan(PlayerId company, PlanCancellationSelection selection);
        long RepeatPlan(PlayerId company, long plan);
        void CopyPlans(PlayerId viewer, IList<PlanView> destination);
    }

    public static class PlanCodes
    {
        public static readonly Code HistoryUnselected = new Code("work.plan-history-unselected");
        public static readonly Code HistoryYoung = new Code("work.plan-history-young");
        public static readonly Code HistoryMinimum = new Code("work.plan-history-minimum");
        public static readonly Code HistoryDependency = new Code("work.plan-history-dependency");
        public static readonly Code HistoryCycle = new Code("work.plan-history-cycle");
        public static readonly Code HistorySource = new Code("work.plan-history-source");
        public static readonly Code HistoryExecution = new Code("work.plan-history-execution");
        public static readonly Code HistoryProvider = new Code("work.plan-history-provider");
        public static readonly Code TemplateRevisionConflict = new Code("work.plan-template-revision-conflict");
        public static readonly Code TemplateNameConflict = new Code("work.plan-template-name-conflict");
        public static readonly Code Paused = new Code("work.plan-paused");
        public static readonly Code Duplicate = new Code("work.plan-duplicate");
        public static readonly Code NotPending = new Code("work.plan-not-pending");
        public static readonly Code NotEnded = new Code("work.plan-not-ended");
        public static readonly Code GroupExists = new Code("work.plan-group-exists");
        public static readonly Code IdentityRange = new Code("work.plan-identity-range");
        public static readonly Code TemplateUnsupported = new Code("work.plan-template-unsupported");
        public static readonly Code LiveBindings = new Code("work.plan-template-live-bindings");
        public static readonly Code ExternalDependencies = new Code("work.plan-template-external-dependencies");
        public static readonly Code DependencyPending = new Code("work.plan-dependency-pending");
        public static readonly Code DependencyCancelled = new Code("work.plan-dependency-cancelled");
        public static readonly Code DependencyFailed = new Code("work.plan-dependency-failed");
        public static readonly Code DependencyCycle = new Code("work.plan-dependency-cycle");
        public static readonly Code SourceOwnedDependencies = new Code("work.plan-source-owned-dependencies");
        public static bool IsWait(Code blocker) => blocker == Paused || blocker == DependencyPending ||
            blocker == DependencyCancelled || blocker == DependencyFailed;
    }
}
