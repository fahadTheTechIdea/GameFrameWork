using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable composition lookup aliasing WorkBoard; never another authored or execution store.</summary>
    public interface IActivityCatalog
    {
        IReadOnlyList<string> PlanKeys { get; }
        IActivityDefinition ResolvePlan(string key);
        bool CanDiscloseActivity(PlayerId viewer, string key);
    }

    public sealed partial class WorkBoard : IActivityCatalog, ICompositionPreparation
    {
        private Dictionary<string, IActivityDefinition> _definitions;
        private IReadOnlyList<string> _definitionKeys;
        public IReadOnlyList<string> PlanKeys => _definitionKeys ??
            throw new InvalidOperationException("activity declarations are captured during composition");
        public void PrepareComposition(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ContentException("foreign activity composition world");
            CapturePlans();
        }

        private void CapturePlans()
        {
            if (_definitions != null) throw new InvalidOperationException("activity declarations are already captured");
            var definitions = new Dictionary<string, IActivityDefinition>(StringComparer.Ordinal);
            var owners = new Dictionary<string, string>(StringComparer.Ordinal);
            var sources = new List<IActivitySource>();
            void Add(IActivityDefinition plan, string owner)
            {
                if (plan == null || string.IsNullOrWhiteSpace(plan.PlanKey) || plan.Graph == null)
                    throw new ContentException(owner + ": activity declaration requires an identity and compiled graph");
                if (definitions.TryGetValue(plan.PlanKey, out var previous))
                {
                    if (!ReferenceEquals(previous, plan))
                        throw new ContentException("activity key '" + plan.PlanKey + "' is declared by different definitions in '" + owners[plan.PlanKey] + "' and '" + owner + "'");
                    return;
                }
                definitions.Add(plan.PlanKey, plan); owners.Add(plan.PlanKey, owner);
            }
            void Source(IActivitySource source, string owner)
            {
                foreach (IActivitySource previous in sources) if (ReferenceEquals(previous, source)) return;
                sources.Add(source);
                IReadOnlyList<IActivityDefinition> plans = source.ActivityDefinitions ??
                    throw new ContentException(owner + ": activity source returned no declaration collection");
                foreach (IActivityDefinition plan in plans) Add(plan, owner);
            }
            foreach (IAuthoredActivityDefinition definition in _world.Content.All<IAuthoredActivityDefinition>()) Add(definition, definition.Module);
            foreach (EntityDef entity in _world.Content.All<EntityDef>())
            {
                ISpatialPresenceDefinition presence = entity.Component<ISpatialPresenceDefinition>();
                if (presence != null && presence.Space != _world.Settings.Space) continue;
                foreach (ComponentDef component in entity.Components)
                    if (component is IActivitySource source) Source(source, entity.Module + "/" + entity.Id);
                    else if (component is IActivityDefinition plan) Add(plan, entity.Module + "/" + entity.Id);
            }
            foreach (IActivitySource source in _world.Services.Providers<IActivitySource>())
                Source(source, _world.Services.OwnerOf(source));
            var keys = new List<string>(definitions.Keys); keys.Sort(StringComparer.Ordinal);
            foreach (string key in keys) WalkDisclosure(definitions[key], null, definitions);
            _definitions = definitions; _definitionKeys = keys.AsReadOnly();
        }

        // Traverse actual aliases only; no second catalog, learned state or cached authorization.
        private Def RequireDisclosureAlias(string plan, IContentDefinition reference)
        {
            if (!(reference is Def actual) || !actual.IsFrozen || !_world.Content.Owns(reference) ||
                reference is IPersistentService || reference.Id != actual.Id || reference.Module != actual.Module)
                throw new ContentException(plan + ": disclosure content requires unique actual immutable aliases");
            return actual;
        }

        private bool WalkDisclosure(IActivityDefinition root, Func<IContentDefinition, bool> visible,
            IReadOnlyDictionary<string, IActivityDefinition> definitions)
        {
            var pending = new Stack<IActivityDefinition>(); var visited = new HashSet<string>(StringComparer.Ordinal);
            var contentVisited = new HashSet<(Type Kind, string Id)>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                IActivityDefinition plan = pending.Pop();
                bool admitted = EntityObservation.ReadProvider(_world, plan, null, () => {
                    if (!visited.Add(plan.PlanKey)) return true;
                    IReadOnlyList<IContentDefinition> references = plan.DisclosureContent;
                    if (references == null || references.Count == 0)
                        throw new ContentException(plan.PlanKey + ": nonempty explicit activity disclosure content required");
                    // Catalog identities are scoped by definition kind: a line and its layer
                    // may share an authored ID, but the same actual alias must not repeat.
                    var identities = new HashSet<(Type Kind, string Id)>();
                    var content = new Stack<IContentDefinition>();
                    foreach (IContentDefinition reference in references)
                    {
                        Def actual = RequireDisclosureAlias(plan.PlanKey, reference);
                        if (!identities.Add((actual.GetType(), actual.Id)))
                            throw new ContentException(plan.PlanKey + ": disclosure content requires unique actual immutable aliases");
                        content.Push(reference);
                    }
                    // The compiled graph is authoritative. Authors do not repeat its
                    // selected contract aliases in a parallel disclosure declaration.
                    foreach (IActivityNodeDefinition node in plan.Graph.Nodes)
                    {
                        content.Push(node.Executor);
                        foreach (WorkerRoleId role in node.Roles)
                            content.Push(_world.Content.Get<IWorkerRoleDefinition>(role.Value));
                        if (node.Requirement != null) content.Push(node.Requirement);
                        if (node.Effect != null) content.Push(_world.Content.Get<IWorkEffect>(node.Effect));
                    }
                    if (plan.OnComplete != null) content.Push(_world.Content.Get<IWorkEffect>(plan.OnComplete));
                    while (content.Count > 0)
                    {
                        IContentDefinition reference = content.Pop();
                        Def actual = RequireDisclosureAlias(plan.PlanKey, reference);
                        if (!contentVisited.Add((actual.GetType(), actual.Id))) continue;
                        if (visible != null && !visible(reference)) return false;
                        if (reference is IActivityDefinition activity) pending.Push(activity);
                        if (reference is IContentDependencyDefinition provider)
                            EntityObservation.ReadProvider(_world, provider, null, () => {
                                IReadOnlyList<IContentDefinition> dependencies = provider.ContentDependencies;
                                if (dependencies == null)
                                    throw new ContentException(provider.Id + ": explicit content dependencies required");
                                var aliases = new HashSet<(Type Kind, string Id)>();
                                foreach (IContentDefinition dependency in dependencies)
                                {
                                    Def linked = RequireDisclosureAlias(plan.PlanKey, dependency);
                                    if (!aliases.Add((linked.GetType(), linked.Id)))
                                        throw new ContentException(provider.Id + ": content dependencies require unique actual aliases");
                                    content.Push(dependency);
                                }
                                return true;
                            });
                        if (reference is IPriorActivityWorkRequirement prior)
                        {
                            if (prior.Plan == null || !definitions.TryGetValue(prior.Plan, out var prerequisite))
                                throw new ContentException(prior.Id + ": prior activity requires an actual captured plan");
                            pending.Push(prerequisite);
                        }
                        if (reference is IWorkRequirementGroup group)
                            EntityObservation.ReadProvider(_world, group, null, () => {
                                if (group.Entries == null || group.Entries.Count == 0)
                                    throw new ContentException(plan.PlanKey + ": requirement group disclosure requires entries");
                                foreach (WorkRequirementEntry entry in group.Entries)
                                {
                                    if (entry?.Requirement == null)
                                        throw new ContentException(plan.PlanKey + ": requirement group disclosure requires actual references");
                                    content.Push(entry.Requirement);
                                }
                                return true;
                            });
                    }
                    return true;
                });
                if (!admitted) return false;
            }
            return true;
        }

        public bool CanDiscloseActivity(PlayerId viewer, string key)
        {
            _world.RequireHealthyUpdate();
            if (viewer != PlayerId.None && !_world.Players.IsSeat(viewer)) return false;
            IActivityDefinition definition = ResolvePlan(key); if (definition == null) return false;
            var view = _world.ViewFor(viewer);
            return WalkDisclosure(definition, view.CanSeeContent, _definitions);
        }

        /// <summary>Reads captured definitions only; providers never run during a lookup or saved restore.</summary>
        public IActivityDefinition ResolvePlan(string key)
        {
            if (_definitions == null) throw new InvalidOperationException("activity declarations are not captured");
            if (string.IsNullOrEmpty(key) || !_definitions.TryGetValue(key, out var plan)) return null;
            if (plan.PlanKey != key) throw new InvalidOperationException("captured activity identity changed");
            return plan;
        }
    }
}

