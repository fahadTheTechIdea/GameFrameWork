using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional module-owned requirements attached while compiling the one authored graph.</summary>
    public interface IActivityRequirementBinding : IWorkRequirementGroup
    {
        string ActivityId { get; }
        string NodeId { get; }
        /// <summary>Capture the actual physical root once, before content publication.</summary>
        void BindPhysicalRequirement(IWorkRequirement physical);
    }

    /// <summary>Standalone file-owned activity; bound execution plans still use IActivityDefinition.</summary>
    public interface IAuthoredActivityDefinition : IContentDefinition, IPlayerActivityDefinition, IEntityTargetPlanDefinition
    {
        bool PlayerOrderable { get; }
        IReadOnlyList<string> TargetTags { get; }
        bool Targets(EntityDef definition);
    }

    /// <summary>Sole activity JSON parser and graph compiler over the existing authored node implementation.</summary>
    public sealed class ActivityDeclaration
    {
        private IReadOnlyList<ActivityNodeDef> _nodes;
        public bool AutomaticSupply { get; }
        public bool PlayerOrderable { get; }
        public IReadOnlyList<string> TargetTags { get; }
        public int Priority { get; }
        public string OnComplete { get; }
        public IActivityGraph Graph { get; private set; }
        private ActivityDeclaration(JsonReader reader)
        {
            AutomaticSupply = reader.Bool("automatic_supply");
            JsonValue nodes = reader.Array("nodes") ?? throw reader.Error("an activity has nodes");
            if (nodes.Count == 0) throw reader.Error("an activity requires at least one node");
            var definitions = new ActivityNodeDef[nodes.Count];
            for (int i = 0; i < definitions.Length; i++)
                definitions[i] = ActivityNodeDef.Read(new JsonReader(nodes[i], reader.Where + ".nodes[" + i + "]"));
            _nodes = Array.AsReadOnly(definitions);
            OnComplete = reader.NullableString("on_complete"); PlayerOrderable = reader.Bool("player_orderable", false);
            TargetTags = Array.AsReadOnly(reader.Strings("target_tags")); Priority = reader.Int("priority", 50);
            if (Priority < 0) throw reader.Error("activity priority must be nonnegative");
        }
        public static ActivityDeclaration Read(JsonReader reader)
            => new ActivityDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content, IAuthoredActivityDefinition definition)
        {
            if (_nodes == null) throw new InvalidOperationException("activity resolves once");
            if (definition == null || definition.AutomaticSupply != AutomaticSupply || definition.PlayerOrderable != PlayerOrderable ||
                definition.Priority != Priority || definition.OnComplete != OnComplete || !ReferenceEquals(definition.TargetTags, TargetTags))
                throw content.Fail("activity metadata does not belong to its declaration");
            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i]; node.Resolve(content, "activity " + definition.Id);
                IActivityRequirementBinding selected = null;
                foreach (var binding in content.Content.All<IActivityRequirementBinding>())
                    if (binding.ActivityId == definition.Id && binding.NodeId == node.Id)
                    {
                        if (selected != null) throw content.Fail("multiple requirement bindings for node '" + node.Id + "'");
                        selected = binding;
                    }
                if (selected != null)
                {
                    selected.BindPhysicalRequirement(node.Requirement);
                    node.BindRequirement(selected);
                }
            }
            Graph = new ActivityGraph(_nodes, definition.PlanKey); _nodes = null;
        }
    }

    /// <summary>One authored metadata/target/player-intent owner; never another plan or work scheduler.</summary>
    public static class ActivityDefinitionContracts
    {
        internal static void ValidateComposition(World world)
        {
            foreach (var definition in world.Content.All<IAuthoredActivityDefinition>())
                EntityObservation.ReadProvider(world, definition, null, () =>
                {
                    if (!world.Content.Owns(definition) || !ReferenceEquals(world.Content.Find<IAuthoredActivityDefinition>(definition.Id), definition) ||
                        definition.PlanKey != "activity:" + definition.Id || definition.Graph == null ||
                        definition.Priority < 0 || definition.TargetTags == null)
                        throw new ContentException("authored activity requires actual owned content, canonical plan identity, graph and target metadata");
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (string tag in definition.TargetTags)
                    {
                        try { ContractIdentity.ValidateId(tag, "activity target tag"); }
                        catch (ArgumentException error) { throw new ContentException(definition.Id + ": " + error.Message); }
                        if (!seen.Add(tag)) throw new ContentException(definition.Id + ": duplicate activity target tag");
                    }
                    return true;
                });
        }
        public static bool Targets(IAuthoredActivityDefinition activity, EntityDef definition)
        {
            if (activity.TargetTags.Count == 0) return true;
            for (int i = 0; i < activity.TargetTags.Count; i++) if (definition.HasTag(activity.TargetTags[i])) return true;
            return false;
        }
        public static Code CheckIntent(IAuthoredActivityDefinition activity, World world, PlayerId company, Entity target)
        {
            if (!activity.PlayerOrderable) return StandardCodes.NotOrderable;
            Code access = world.Services.Get<IContentAccess>().Check(world, company, activity);
            if (!access.IsNone) return access;
            if (!activity.Targets(target.Def)) return StandardCodes.WrongTarget;
            var admission = world.Content.Find<IWorkEffect>(activity.OnComplete) as IWorkEffectIntentAdmission;
            return admission == null ? Code.None : EntityObservation.ReadProvider(world, admission, target, () => admission.CheckIntent(world, company, target));
        }
        public static string IntentIdentity(IAuthoredActivityDefinition activity, JsonValue arguments)
        {
            var selection = ActivityWorkerSelection.Read(arguments, activity.Graph, activity.PlanKey + ".intent", out EntityId target);
            return selection.Identity(target);
        }
        public static void ValidateTargetDefinition(IAuthoredActivityDefinition activity, EntityDef target)
        {
            if (!activity.PlayerOrderable || target != null && !activity.Targets(target))
                throw new ContentException(activity.PlanKey + ": recurring targeting requires ordinary orderable compatible work");
        }
        public static PlanIntent CreateTargetIntent(IAuthoredActivityDefinition activity, EntityId target, IActivityWorkerSelection workers, string group, int priority)
        {
            var selection = new ActivityWorkerSelection(workers?.Bindings ?? throw new ArgumentNullException(nameof(workers)));
            selection.ValidateGraph(activity.Graph);
            return new PlanIntent(activity.PlanKey, ActivityWorkerSelection.CreateArguments(target, selection), group, priority, false);
        }
        public static Code CheckSubmission(IAuthoredActivityDefinition activity, World world, PlayerId company, JsonValue arguments)
        {
            var selection = ActivityWorkerSelection.Read(arguments, activity.Graph, activity.PlanKey + ".intent", out EntityId target);
            Code check = world.Services.Get<WorkBoard>().CheckPlayerActivity(company, activity.PlanKey, target, 0);
            return check.IsNone ? selection.Check(world, company, activity.Graph) : check;
        }
        public static Code CheckActivation(IAuthoredActivityDefinition activity, World world, PlayerId company, JsonValue arguments)
        {
            Code check = PlanObservation.Submission(world, activity, company, arguments); if (!check.IsNone) return check;
            var admission = world.Content.Find<IWorkEffect>(activity.OnComplete) as IWorkEffectIntentAdmission;
            if (admission == null) return Code.None;
            ActivityWorkerSelection.Read(arguments, activity.Graph, activity.PlanKey + ".intent", out EntityId identity);
            Entity target = world.Entities.Get(identity);
            return EntityObservation.ReadProvider(world, admission, target, () => admission.CheckStart(world, company, target));
        }
        public static Order Activate(IAuthoredActivityDefinition activity, World world, PlayerId company, JsonValue arguments, int priority)
        {
            Code check = PlanObservation.Activation(world, activity, company, arguments);
            if (!check.IsNone) throw new InvalidOperationException("activity plan admission changed: " + check);
            var selection = ActivityWorkerSelection.Read(arguments, activity.Graph, activity.PlanKey + ".intent", out EntityId target);
            return world.Services.Get<WorkBoard>().Post(activity, company, target, priority, selection);
        }
    }
}
