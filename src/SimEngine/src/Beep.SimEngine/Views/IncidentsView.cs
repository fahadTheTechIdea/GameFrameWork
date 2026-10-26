using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public readonly struct IncidentLifecycleView
    {
        public string Workflow { get; }
        public IncidentLifecycleSnapshot Snapshot { get; }
        public int OrderId { get; }
        public OrderState? OrderState { get; }
        public Code Blocker { get; }
        internal IncidentLifecycleView(IPhasedIncidentResponse response, IncidentLifecycleSnapshot snapshot, Order order)
        { Workflow = response.Workflow.Id; Snapshot = snapshot; OrderId = order?.Id ?? 0;
            OrderState = order?.State; Blocker = order?.Blocker ?? Code.None; }
    }
    /// <summary>Detached observations of the sole native node records, not phase execution state.</summary>
    public readonly struct IncidentPhaseView
    {
        public string Id { get; }
        public IReadOnlyList<string> NodeIds { get; }
        public IReadOnlyList<ActivityNodeView> Nodes { get; }
        public int NodesDone { get; }
        public bool Complete => NodesDone == NodeIds.Count;
        internal IncidentPhaseView(IIncidentPhaseDefinition phase, Order order, WorkBoard board, World world)
        {
            Id = phase.Id; NodeIds = new List<string>(phase.Nodes).AsReadOnly();
            var nodes = new List<ActivityNodeView>();
            if (order != null) foreach (string id in NodeIds)
            {
                ActivityNodeExecution node = order.Node(id);
                nodes.Add(new ActivityNodeView(order.Plan.Graph.Find(id), node, world.Time, board.Job(node.JobId), board));
            }
            Nodes = nodes.AsReadOnly(); NodesDone = IncidentPhases.CompletedNodes(phase, order);
        }
    }

    public sealed class IncidentsView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal IncidentsView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        public Code ReadPreparedness(EntityId id, Code source, out IncidentPreparednessSnapshot snapshot)
        {
            snapshot = null;
            if (Owned(id) == null) return Codes.NotFound;
            return _world.Services.Get<IIncidentPreparedness>().ReadPreparedness(id, source, out snapshot);
        }
        private Entity Owned(EntityId id)
        {
            _world.RequireHealthyUpdate();
            Entity entity = _world.Entities.Get(id);
            return entity != null && (_viewer == PlayerId.None || entity.Owner == _viewer) ? entity : null;
        }
        private T Observe<T>(Entity entity, IIncident provider, Func<T> read)
            => EntityObservation.ReadProvider(_world, provider, entity, read, observeTargetState: true);
        public bool CopyIncidentIds(EntityId id, IList<Code> into)
        {
            if (into == null) throw new ArgumentNullException(nameof(into));
            into.Clear(); Entity entity = Owned(id);
            if (entity == null) return false;
            var values = new List<Code>();
            for (int i = 0; i < entity.Components.Count; i++)
                if (entity.Components[i] is IIncident incident) values.Add(Observe(entity, incident, () => {
                    Code identity = incident.CapabilityId;
                    if (identity.IsNone || !ReferenceEquals(entity.Get<IIncident>(identity), incident))
                        throw new InvalidOperationException("incident identity must name its actual entity owner");
                    return identity;
                }));
            foreach (Code identity in values) into.Add(identity);
            return true;
        }
        public bool TryGetIncident(EntityId id, Code source, out IncidentSnapshot snapshot)
        {
            snapshot = default; _world.RequireHealthyUpdate();
            if (source.IsNone) return false;
            IIncident incident = Owned(id)?.Get<IIncident>(source);
            if (incident == null) return false;
            snapshot = Observe(incident.Entity, incident, () => ResponseWork.Read(incident)); return true;
        }
        public bool TryGetProcess(EntityId id, Code capability, out IncidentProcessSnapshot snapshot)
        {
            snapshot = default; Entity entity = Owned(id);
            if (entity == null || capability.IsNone) return false;
            var process = entity.Get<IIncidentProcess>(capability); if (process == null) return false;
            snapshot = EntityObservation.ReadProvider(_world, process, entity, () => process.ReadProcess(_world), space: _world.Space, observeTargetState: true);
            return true;
        }
        /// <summary>The existing work-order identity; its progress/blockers remain in WorkView.</summary>
        public bool TryGetResponseOrder(EntityId id, Code source, out int orderId)
        {
            orderId = 0; _world.RequireHealthyUpdate();
            if (source.IsNone) return false;
            IIncidentResponse response = Owned(id)?.Get<IIncidentResponse>(source);
            if (response == null) return false;
            int result = Observe(response.Entity, response, () => {
                if (!ResponseWork.Read(response).Active || response.ResponsePlan == null) return 0;
                Order order = _world.Services.Get<WorkBoard>().ActiveResponseOn(id, source);
                return order == null || !ResponseWork.CheckActiveOrder(_world, response.Entity, order).IsNone ? 0 : order.Id;
            });
            orderId = result; return result != 0;
        }

        private bool ReadPhased(EntityId id, Code source, out IPhasedIncidentResponse response,
            out IncidentLifecycleSnapshot snapshot, out Order order)
        {
            response = null; snapshot = default; order = null;
            Entity entity = Owned(id);
            if (source.IsNone || entity == null) return false;
            response = entity.Get<IPhasedIncidentResponse>(source);
            if (response == null) return false;
            Order observed = null;
            IPhasedIncidentResponse selected = response;
            snapshot = Observe(entity, selected, () => IncidentPhases.ReadLifecycle(_world, selected, out observed));
            order = observed;
            return true;
        }
        public bool TryGetLifecycle(EntityId id, Code source, out IncidentLifecycleView view)
        {
            view = default;
            if (!ReadPhased(id, source, out IPhasedIncidentResponse response, out IncidentLifecycleSnapshot snapshot, out Order order)) return false;
            view = Observe(response.Entity, response, () => new IncidentLifecycleView(response, snapshot, order)); return true;
        }
        public bool CopyPhases(EntityId id, Code source, IList<IncidentPhaseView> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            if (!ReadPhased(id, source, out IPhasedIncidentResponse response, out _, out Order order)) return false;
            WorkBoard board = _world.Services.Get<WorkBoard>();
            var values = Observe(response.Entity, response, () => {
                var phases = new List<IncidentPhaseView>();
                foreach (IIncidentPhaseDefinition phase in response.Workflow.Phases)
                    phases.Add(new IncidentPhaseView(phase, order, board, _world));
                return phases;
            });
            foreach (IncidentPhaseView phase in values) destination.Add(phase);
            return true;
        }
    }
}
