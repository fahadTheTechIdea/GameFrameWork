using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IIncidentPhaseDefinition
    {
        string Id { get; }
        IReadOnlyList<string> Nodes { get; }
    }
    public interface IIncidentWorkflowDefinition : IContentDefinition
    {
        Code Kind { get; }
        IActivityDefinition Response { get; }
        int Priority { get; }
        IReadOnlyList<IIncidentPhaseDefinition> Phases { get; }
    }
    public interface IPhasedIncidentResponse : IIncidentResponse
    {
        IIncidentWorkflowDefinition Workflow { get; }
        IncidentLifecycleSnapshot ReadLifecycle();
    }
    public interface IPhasedIncidentDefinition : IIncidentDefinition
    {
        IIncidentWorkflowDefinition Workflow { get; }
    }

    /// <summary>Immutable labels over the existing DAG, not another dependency compiler.</summary>
    public sealed class IncidentPhaseDefinition : IIncidentPhaseDefinition
    {
        public string Id { get; }
        public IReadOnlyList<string> Nodes { get; }
        public IncidentPhaseDefinition(string id, IEnumerable<string> nodes)
        {
            ContractIdentity.Validate(id, 1, "incident phase");
            Id = id; Nodes = new List<string>(nodes ?? throw new ArgumentNullException(nameof(nodes))).AsReadOnly();
        }
    }

    public sealed class IncidentWorkflowDef : Def, IIncidentWorkflowDefinition
    {
        private IncidentWorkflowDeclaration _declaration;
        public Code Kind => _declaration.Kind;
        public IActivityDefinition Response => _declaration.Response;
        public int Priority => _declaration.Priority;
        public IReadOnlyList<IIncidentPhaseDefinition> Phases => _declaration.Phases;
        protected override void ReadCore(JsonReader r) => _declaration = IncidentWorkflowDeclaration.Read(r);
        protected override void ResolveCore(ContentResolver c) => _declaration.Resolve(c, this);
    }

    public static class IncidentPhases
    {
        public static IIncidentPhaseDefinition Find(IIncidentWorkflowDefinition workflow, string id)
        {
            if (workflow == null || id == null) return null;
            foreach (IIncidentPhaseDefinition phase in workflow.Phases) if (phase.Id == id) return phase;
            return null;
        }
        internal static int CompletedNodes(IIncidentPhaseDefinition phase, Order order)
        {
            int done = 0;
            if (order != null) foreach (string id in phase.Nodes) if (order.Node(id).State == NodeState.Done) done++;
            return done;
        }
        /// <summary>One pure lifecycle/history check shared by views and restored-state admission.</summary>
        public static IncidentLifecycleSnapshot ReadLifecycle(World world, IPhasedIncidentResponse response, out Order order)
        {
            Order observed = null;
            var snapshot = EntityObservation.ReadProvider(world, response, response.Entity,
                () => ReadLifecycleCore(world, response, out observed));
            order = observed; return snapshot;
        }
        private static IncidentLifecycleSnapshot ReadLifecycleCore(World world, IPhasedIncidentResponse response, out Order order)
        {
            Entity entity = response.Entity;
            if (!ReferenceEquals(world.Entities.Get(entity.Id), entity) || !ReferenceEquals(entity.Get<IPhasedIncidentResponse>(response.CapabilityId), response))
                throw new InvalidOperationException("incident belongs to another entity/session");
            int version = entity.Version;
            IncidentLifecycleSnapshot snapshot = response.ReadLifecycle(); IncidentSnapshot active = ResponseWork.Read(response);
            _ = new IncidentLifecycleSnapshot(snapshot.Kind, snapshot.Episode, snapshot.RaisedTick, snapshot.ResolvedTick);
            if (response.Workflow == null || response.Workflow.Kind != snapshot.Kind || snapshot.Active != active.Active ||
                snapshot.RaisedTick > world.Tick || snapshot.ResolvedTick > world.Tick ||
                active.Active && (active.Episode != snapshot.Episode || active.RaisedTick != snapshot.RaisedTick || active.Kind != snapshot.Kind ||
                    !ReferenceEquals(response.ResponsePlan, response.Workflow.Response)))
                throw new InvalidOperationException("incident lifecycle differs from its authoritative response episode");
            order = world.Services.Get<WorkBoard>().ResponseOn(entity.Id, response.CapabilityId, snapshot.Episode);
            if (order != null && (order.Owner != entity.Owner || !ReferenceEquals(order.Plan, response.Workflow.Response) || order.CreatedTick < snapshot.RaisedTick ||
                snapshot.Active && order.State == OrderState.Done || !snapshot.Active && (order.State != OrderState.Done ||
                    !order.CompletionEffectApplied || snapshot.ResolvedTick != order.CompletedTick)))
                throw new InvalidOperationException("incident history differs from its authoritative workflow");
            if (snapshot.Episode > 0 && !snapshot.Active && order == null)
                throw new InvalidOperationException("resolved incident is missing its physical completion history");
            if (version != entity.Version) throw new InvalidOperationException("incident mutated a lifecycle read");
            return snapshot;
        }
        /// <summary>Open providers share this admission rule; all execution stays in WorkBoard.</summary>
        public static void Validate(IIncidentWorkflowDefinition workflow, ResolvedContent content)
        {
            if (workflow == null || workflow.Kind.IsNone || workflow.Priority < 0 || workflow.Phases == null || workflow.Phases.Count == 0)
                throw new ContentException("incident workflow needs kind, response priority and phases");
            ResponseWork.ValidatePlan(workflow.Response);
            var phases = new HashSet<string>(StringComparer.Ordinal); var nodes = new HashSet<string>(StringComparer.Ordinal);
            foreach (IIncidentPhaseDefinition phase in workflow.Phases)
            {
                if (phase == null) throw new ContentException("incident workflow has a missing phase");
                try { ContractIdentity.Validate(phase.Id, 1, "incident phase"); }
                catch (ArgumentException error) { throw new ContentException(error.Message); }
                if (!phases.Add(phase.Id) || phase.Nodes == null || phase.Nodes.Count == 0)
                    throw new ContentException("incident phases must have unique identities and nonempty node mappings");
                foreach (string node in phase.Nodes)
                    if (string.IsNullOrWhiteSpace(node) || workflow.Response.Graph.Find(node) == null || !nodes.Add(node))
                        throw new ContentException("incident phase names an absent or multiply mapped activity node");
            }
            if (nodes.Count != workflow.Response.Graph.Nodes.Count) throw new ContentException("every incident activity node must belong to one phase");
            if (!(workflow.Response is IContentDefinition definition) || !content.Owns(definition))
                throw new ContentException("incident workflow response must alias actual authored content");
            if (workflow.Response is IAuthoredActivityDefinition activity && activity.PlayerOrderable)
                throw new ContentException("incident response is enrolled by its episode, not an unbound player order");
            var final = content.Find<IWorkEffect>(workflow.Response.OnComplete) as IIncidentCompletionEffect;
            if (final == null || !final.BoundResponse || !final.Source.IsNone)
                throw new ContentException("incident workflow requires an explicit bound_response final effect");
            foreach (IActivityNodeDefinition node in workflow.Response.Graph.Nodes)
            {
                IWorkEffect effect = content.Find<IWorkEffect>(node.Effect);
                if (effect is IEntityReplacementEffect) throw new ContentException("incident recovery cannot replace its state owner");
                if (effect is IIncidentCompletionEffect)
                    throw new ContentException("incident recovery is a whole-order final effect, not a node effect");
            }
        }
    }

    public sealed class IncidentDef : ComponentDef<Incident>, IIncidentEpisodeDefinition
    {
        private IncidentEpisodeDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public IIncidentWorkflowDefinition Workflow => _declaration.Workflow;
        public bool Supports(Code kind) => kind == Workflow.Kind;
        public EntityStateBindings States => _declaration.States;
        protected override void ReadCore(JsonReader r) => _declaration = IncidentEpisodeDeclaration.Read(r);
        protected override void ResolveCore(ContentResolver c) => _declaration.Resolve(c);
        public void ValidateBinding(World world) => IncidentBindingContracts.ValidateEpisode(world, this);
    }

    /// <summary>Only episode facts are owned here. Work, claims, elapsed time and phase progress are not.</summary>
    public class Incident : Component, IPhasedIncidentResponse, IEntityStateSource
    {
        public new IIncidentEpisodeDefinition Def => (IIncidentEpisodeDefinition)base.Def;
        private int _episode;
        private long _raised = -1, _resolved = -1;
        private bool Active => _episode > 0 && _resolved < 0;
        public Code CapabilityId => Def.CapabilityId;
        public IIncidentWorkflowDefinition Workflow => Def.Workflow;
        public IActivityDefinition ResponsePlan => Workflow.Response;
        public int ResponsePriority => Workflow.Priority;
        public IncidentSnapshot ReadIncident() => Active ? IncidentSnapshot.Raised(Workflow.Kind, _raised, _episode) : IncidentSnapshot.Inactive();
        public IncidentLifecycleSnapshot ReadLifecycle() => new IncidentLifecycleSnapshot(Workflow.Kind, _episode, _raised, _resolved);
        public bool Supports(Code kind) => kind == Workflow.Kind;
        public bool Raise(World world, Code kind)
        {
            IncidentBindingContracts.ReadEpisode(world, Def, Entity);
            if (!ReferenceEquals(world.Entities.Get(Entity.Id), Entity) || !Entity.Alive || Active || !Supports(kind)) return false;
            int episode = checked(_episode + 1);
            _episode = episode; _raised = world.Tick; _resolved = -1; Entity.Touch(); return true;
        }
        public Code CompleteResponse(World world, Order order)
        {
            IncidentBindingContracts.ReadEpisode(world, Def, Entity);
            Code refusal = ResponseWork.CheckCompletion(world, this, Entity, order);
            if (!refusal.IsNone) return refusal;
            _resolved = world.Tick; Entity.Touch(); return Code.None;
        }
        public IEntityStateDefinition GetState(EntityUpdateContext context)
            => Active ? IncidentBindingContracts.ReadEpisode(context.World, Def, Entity).States["active"] : null;
        public override void Sync(IStateSync s)
        {
            int episode = _episode; long raised = _raised, resolved = _resolved;
            s.Int(ref episode); s.Long(ref raised); s.Long(ref resolved);
            if (s.Reading)
            {
                try { _ = new IncidentLifecycleSnapshot(Workflow.Kind, episode, raised, resolved); }
                catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
                _episode = episode; _raised = raised; _resolved = resolved;
            }
        }
    }
}
