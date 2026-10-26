using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IIncidentDefinition : INamedCapabilityDefinition
    {
        bool Supports(Code kind);
    }

    public interface IIncident : INamedEntityCapability
    {
        IncidentSnapshot ReadIncident();
        bool Supports(Code kind);
        bool Raise(World world, Code kind);
    }

    public interface IIncidentResponse : IIncident
    {
        /// <summary>The actual authored response, even before an incident. Episode activity is separate.</summary>
        IActivityDefinition ResponsePlan { get; }
        int ResponsePriority { get; }
        /// <summary>Validate owner, plan and episode before mutation. Refusals change nothing.</summary>
        Code CompleteResponse(World world, Order order);
    }

    public static class IncidentCodes
    {
        public static readonly Code NotResponse = new Code("incident.not-response");
        public static readonly Code NoIncident = new Code("incident.no-incident");
        public static readonly Code StaleResponse = new Code("incident.stale-response");
    }

    /// <summary>Posting uses the sole work board; providers retain all incident state and codecs.</summary>
    public static class ResponseWork
    {
        /// <summary>One completion implementation; owned effects delegate, actual response owns episode mutation.</summary>
        public static WorkEffectResult ApplyCompletion(World world, IIncidentCompletionEffect effect, Order order, IActivityNodeDefinition node)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (effect == null || !world.Content.Owns(effect) || !ReferenceEquals(world.Content.Find<IWorkEffect>(effect.Id), effect))
                throw new ArgumentException("completion effect must alias actual selected content", nameof(effect));
            if (order == null || !ReferenceEquals(world.Services.Get<WorkBoard>().Order(order.Id), order))
                return WorkEffectResult.Failed(IncidentCodes.StaleResponse);
            if (node != null || order.Plan.OnComplete != effect.Id || order.CompletionEffectApplied)
                return WorkEffectResult.Failed(WorkEffectCodes.NotComplete);
            Entity target = world.Entities.Get(order.Target);
            if (target == null) return WorkEffectResult.Failed(Codes.NotFound);
            Code source = effect.BoundResponse ? order.ResponseSource : effect.Source;
            if (source.IsNone) return WorkEffectResult.Failed(IncidentCodes.NotResponse);
            IIncidentResponse response = target.Get<IIncidentResponse>(source);
            if (response == null) return WorkEffectResult.Failed(IncidentCodes.NotResponse);
            Code check = CheckCompletion(world, response, target, order);
            return WorkEffectContracts.Outcome(world, check.IsNone ? response.CompleteResponse(world, order) : check);
        }

        internal static void ValidateComposition(World world)
        {
            foreach (var effect in world.Content.All<IIncidentCompletionEffect>())
                EntityObservation.ReadProvider(world, effect, null, () => { IncidentCompletionDeclaration.Validate(effect); return true; });
            foreach (var workflow in world.Content.All<IIncidentWorkflowDefinition>())
                EntityObservation.ReadProvider(world, workflow, null, () =>
                {
                    if (!world.Content.Owns(workflow) || !ReferenceEquals(world.Content.Find<IIncidentWorkflowDefinition>(workflow.Id), workflow))
                        throw new ContentException("incident workflow must alias actual selected content");
                    IncidentPhases.Validate(workflow, world.Content); return true;
                });
        }

        public static void ValidatePlan(IActivityDefinition plan)
        {
            if (plan == null) throw new ContentException("active incident requires an authored response plan");
            for (int i = 0; i < plan.Graph.Nodes.Count; i++)
                if (!plan.Graph.Nodes[i].Duration.IsZero && plan.Graph.Nodes[i].Roles.Count > 0) return;
            throw new ContentException("response requires positive-time machine work");
        }
        public static IncidentSnapshot Read(IIncident incident)
        {
            IncidentSnapshot snapshot = incident.ReadIncident();
            if (!snapshot.IsValid) throw new InvalidOperationException("incident returned malformed facts");
            return snapshot;
        }
        public static bool AnyActive(Entity entity)
        {
            for (int i = 0; i < entity.Components.Count; i++)
                if (entity.Components[i] is IIncident incident && Read(incident).Active) return true;
            return false;
        }
        public static Code EnsurePosted(World world, Entity entity, IIncidentResponse response)
        {
            if (!ReferenceEquals(entity.Get<IIncidentResponse>(response.CapabilityId), response))
                throw new InvalidOperationException("response provider is not owned by the target entity");
            var facts = EntityObservation.ReadProvider(world, response, entity,
                () => (Snapshot: Read(response), Plan: response.ResponsePlan, Priority: response.ResponsePriority));
            IncidentSnapshot snapshot = facts.Snapshot;
            if (!snapshot.Active) return IncidentCodes.NoIncident;
            IActivityDefinition plan = facts.Plan ?? throw new InvalidOperationException("active incident has no response plan");
            ValidatePlan(plan);
            WorkBoard board = world.Services.Get<WorkBoard>();
            Order existing = board.ActiveResponseOn(entity.Id, response.CapabilityId);
            if (existing != null && (!ReferenceEquals(existing.Plan, plan) || existing.ResponseEpisode != snapshot.Episode))
            { world.System<WorkSystem>().Cancel(world, existing); existing = null; }
            if (existing == null)
            {
                Order previous = board.ResponseOn(entity.Id, response.CapabilityId, snapshot.Episode);
                if (previous != null && ReferenceEquals(previous.Plan, plan))
                {
                    Code binding = CheckActiveOrder(world, entity, previous);
                    if (!binding.IsNone) return binding;
                    if (previous.State == OrderState.Cancelled) { board.ResumeResponse(previous); return Code.None; }
                    // Permanent failures remain visible, not a source of repeated jobs or consumption.
                    if (previous.State == OrderState.Failed) return previous.Blocker.IsNone ? IncidentCodes.StaleResponse : previous.Blocker;
                    if (previous.State == OrderState.Done) return IncidentCodes.StaleResponse;
                }
                Order order = board.Post(plan, entity.Owner, entity.Id, facts.Priority);
                order.ResponseSource = response.CapabilityId; order.ResponseEpisode = snapshot.Episode;
            }
            return Code.None;
        }
        public static Code CheckCompletion(World world, IIncidentResponse response, Entity entity, Order order)
        {
            Code binding = CheckBinding(world, response, entity, order);
            if (!binding.IsNone) return binding;
            IActivityDefinition plan = response.ResponsePlan;
            if (order.State != OrderState.Completing ||
                order.Nodes.Count != plan.Graph.Nodes.Count)
                return IncidentCodes.StaleResponse;
            for (int i = 0; i < order.Nodes.Count; i++) if (order.Nodes[i].State != NodeState.Done) return IncidentCodes.StaleResponse;
            return Code.None;
        }

        /// <summary>Pure binding guard for every work/supply/effect phase, not only final recovery.</summary>
        public static Code CheckActiveOrder(World world, Entity entity, Order order)
        {
            if (order == null) return IncidentCodes.StaleResponse;
            if (order.ResponseSource.IsNone) return order.ResponseEpisode == 0 ? Code.None : IncidentCodes.StaleResponse;
            IIncidentResponse response = entity?.Get<IIncidentResponse>(order.ResponseSource);
            return response == null ? IncidentCodes.NotResponse : CheckBinding(world, response, entity, order);
        }

        private static Code CheckBinding(World world, IIncidentResponse response, Entity entity, Order order)
        {
            if (entity == null || !ReferenceEquals(world.Entities.Get(entity.Id), entity) ||
                !ReferenceEquals(entity.Get<IIncidentResponse>(response.CapabilityId), response)) return IncidentCodes.NotResponse;
            var facts = EntityObservation.ReadProvider(world, response, entity,
                () => (Snapshot: Read(response), Plan: response.ResponsePlan));
            IncidentSnapshot snapshot = facts.Snapshot; IActivityDefinition plan = facts.Plan;
            if (!snapshot.Active) return IncidentCodes.NoIncident;
            if (order == null || !ReferenceEquals(world.Services.Get<WorkBoard>().Order(order.Id), order) ||
                order.Owner != entity.Owner || order.Target != entity.Id || plan == null ||
                order.ResponseSource != response.CapabilityId || order.ResponseEpisode != snapshot.Episode ||
                !ReferenceEquals(order.Plan, plan) || order.CreatedTick < snapshot.RaisedTick)
                return IncidentCodes.StaleResponse;
            return Code.None;
        }
    }

    /// <summary>One optional engine posting phase. Providers own incident state; the existing board owns work.</summary>
    public sealed class IncidentResponseSystem : GameSystem, ICompositionValidator
    {
        private bool _validating; // Transient recursion guard, never an incident or clock owner.
        public override string Id => "sim.incident-responses";
        public override int StateSchemaVersion => 1;
        public void ValidateComposition(World world)
        {
            if (_validating) throw new InvalidOperationException("incident binding validation cannot recursively compose");
            _validating = true;
            try { IncidentBindingContracts.ValidateComposition(world); IncidentProcessContracts.ValidateComposition(world); }
            finally { _validating = false; }
        }
        public override void OnLoaded(World world)
        {
            try
            {
                foreach (Entity entity in world.Entities.All)
                    foreach (Component component in entity.Components)
                        if (component is IPhasedIncidentResponse response) IncidentPhases.ReadLifecycle(world, response, out _);
                foreach (Entity entity in world.Entities.All)
                    foreach (Component component in entity.Components)
                        if (component is IIncidentProcess process)
                            EntityObservation.ReadProvider(world, process, entity, () => { process.ValidateProcessCheckpoint(world); return true; }, space: world.Space, observeTargetState: true);
            }
            catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
            catch (InvalidOperationException error) { throw new CorruptStateException(error.Message); }
        }
        public override void Update(World world, TickInfo tick)
        {
            foreach (Entity entity in world.Entities.All)
                if (entity.Alive)
                    foreach (Component component in entity.Components)
                        if (component is IIncidentResponse response && ResponseWork.Read(response).Active)
                            ResponseWork.EnsurePosted(world, entity, response);
            foreach (Entity entity in world.Entities.All)
                if (entity.Alive)
                    foreach (Component component in entity.Components)
                        if (component is IIncidentProcess process) process.UpdateProcess(world, tick);
        }
    }
}
