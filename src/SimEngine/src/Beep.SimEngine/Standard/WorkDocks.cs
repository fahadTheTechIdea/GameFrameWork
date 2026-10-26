using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class WorkBoard : IWorkDockAdmission
    {
        public Code CheckDock(WorkRequirementContext context)
        {
            if (!ReferenceEquals(context.World, _world)) return Codes.InvalidArgument;
            Entity target = context.Target, worker = context.Worker;
            if (target == null || !target.Alive || !ReferenceEquals(_world.Entities.Get(target.Id), target)) return Codes.NotFound;
            if (worker == null || !worker.Alive || !ReferenceEquals(_world.Entities.Get(worker.Id), worker)) return WorkSystem.BlockerNoWorker;
            if (target.Owner != context.Owner || worker.Owner != context.Owner) return Codes.NotOwned;
            if (Dock(target) == null) return WorkDockCodes.Missing;
            bool declared = false;
            foreach (WorkerRoleId role in context.Definition.Roles) if (role == context.Role) { declared = true; break; }
            if (!declared || !WorkerExecutionContracts.Can(_world, worker, context.Role)) return WorkDockCodes.Incompatible;
            return PreviewDock(target, worker, WorkDockRequest.Work(context.Role), null, out _, out _, out _, out _, DockJob(context));
        }

        private Job DockJob(WorkRequirementContext context)
        {
            Job job = Job(context.Execution?.JobId ?? 0);
            // Only this node's actual live worker/target may exclude its own existing slot intent.
            if (job?.IsLive != true || job.IsHaul || job.Worker != context.Worker.Id || job.Target != context.Target.Id ||
                job.Owner != context.Owner || job.OrderId != context.Execution.OrderId || job.NodeId != context.Node)
                job = null;
            return job;
        }

        // Temporary search intent, never a live claim, provider clone or saved state.
        internal readonly struct DockProposal
        {
            public IWorkDock Dock { get; }
            public Code Slot { get; }
            public DockProposal(IWorkDock dock, Code slot) { Dock = dock; Slot = slot; }
        }

        private Code CheckDockSet(IReadOnlyList<WorkRequirementContext> contexts, out IReadOnlyList<IncidentPreparednessDock> selected)
        {
            selected = Array.Empty<IncidentPreparednessDock>();
            var workers = new List<WorkRequirementContext>();
            foreach (var context in contexts) if (context.Worker != null) workers.Add(context);
            if (workers.Count == 0) return Code.None;
            var tried = new HashSet<Code>[workers.Count];
            var slots = new Code[workers.Count]; var docks = new IWorkDock[workers.Count];
            for (int i = 0; i < workers.Count; i++) { tried[i] = new HashSet<Code>(); docks[i] = Dock(workers[i].Target); }
            var prefix = new List<DockProposal>(); int depth = 0; Code refusal = Code.None;
            while (depth >= 0)
            {
                var context = workers[depth];
                Code check = PreviewDock(context.Target, context.Worker, WorkDockRequest.Work(context.Role), null,
                    out Code slot, out _, out _, out _, DockJob(context), productiveOnly: true, proposals: prefix, excluded: tried[depth]);
                if (check.IsNone)
                {
                    slots[depth] = slot; tried[depth].Add(slot);
                    if (depth == workers.Count - 1)
                    {
                        var rows = new List<IncidentPreparednessDock>();
                        for (int i = 0; i < workers.Count; i++) if (docks[i] != null)
                            rows.Add(new IncidentPreparednessDock(workers[i].Node, workers[i].Worker.Id, workers[i].Target.Id, slots[i]));
                        selected = rows.AsReadOnly(); return Code.None;
                    }
                    if (docks[depth] != null) prefix.Add(new DockProposal(docks[depth], slot));
                    depth++; tried[depth].Clear();
                }
                else
                {
                    if (refusal.IsNone) refusal = check;
                    depth--;
                    if (depth >= 0 && docks[depth] != null) prefix.RemoveAt(prefix.Count - 1);
                }
            }
            return refusal;
        }

        private IWorkDock Dock(Entity endpoint)
        {
            IWorkDock dock = endpoint?.Get<IWorkDock>();
            if (dock == null) return null;
            return ReadDock(dock, null, () =>
            {
                if (!ReferenceEquals(dock.Entity, endpoint)) throw new InvalidOperationException("dock must alias its actual entity");
                return dock;
            });
        }

        // The actual provider, its whole endpoint, carrier and selected space use the original
        // observation owner. Checks and validation run inside it, including property callbacks.
        private T ReadDock<T>(IWorkDock dock, Entity carrier, Func<T> read)
            => EntityObservation.ReadProvider(_world, dock, null, () =>
            {
                Entity endpoint = dock.Entity;
                if (endpoint == null || !ReferenceEquals(_world.Entities.Get(endpoint.Id), endpoint) ||
                    !ReferenceEquals(endpoint.Get<IWorkDock>(), dock) ||
                    !ReferenceEquals(dock.Slots, endpoint.Def.Component<IWorkDockDefinition>()?.Slots))
                    throw new InvalidOperationException("dock must alias its actual entity and immutable authored slots");
                return carrier == null ? read() : EntityObservation.ReadProvider(_world, carrier, carrier, read, observeTargetState: true);
            }, space: _world.Space, observeTargetState: true);

        private IReadOnlyList<WorkDockSlot> DockSlots(IWorkDock dock) => ReadDock(dock, null, () => dock.Slots);
        private bool SlotMatches(IWorkDock dock, Code id, WorkDockRequest request)
        { foreach (var slot in DockSlots(dock)) if (slot.Id == id) return slot.Matches(request); return false; }

        private WorkDockSlotKind Kind(IWorkDock dock, Code id)
        {
            return ReadDock(dock, null, () =>
            {
                foreach (WorkDockSlot slot in dock.Slots) if (slot.Id == id) return slot.Kind;
                throw new InvalidOperationException("job names a missing dock slot");
            });
        }

        private SpatialOverlapStatus Overlap(IWorkDock dock, Entity carrier, SpatialSnapshot place,
            IWorkDock other, SpatialSnapshot geometry)
        {
            if (!(_world.Space is ISpatialOverlap space)) return SpatialOverlapStatus.Unsupported;
            return ReadDock(dock, carrier, () => ReadDock(other, null, () =>
            {
                SpatialOverlapStatus result = space.CheckOverlap(_world, place, geometry);
                if (!ReferenceEquals(_world.Space, space) || result < SpatialOverlapStatus.Disjoint || result > SpatialOverlapStatus.Unsupported ||
                    ReferenceEquals(place, geometry) && result == SpatialOverlapStatus.Disjoint)
                    throw new InvalidOperationException("spatial provider changed authority or returned invalid overlap facts");
                return result;
            }));
        }

        // Existing jobs alone own claims. Identity inequality is not physical disjointness.
        private Code ClaimBlocker(IWorkDock dock, Entity carrier, Code slot, Job except = null, IReadOnlyList<DockProposal> proposals = null)
        {
            if (!(_world.Space is ISpatialOverlap)) return WorkDockCodes.UnsupportedGeometry;
            SpatialSnapshot place = CaptureDock(dock, carrier, slot);
            SpatialOverlapStatus self = Overlap(dock, carrier, place, dock, place);
            if (self == SpatialOverlapStatus.Unsupported) return WorkDockCodes.UnsupportedGeometry;
            if (self != SpatialOverlapStatus.Overlapping) throw new InvalidOperationException("dock geometry must overlap itself");
            Code Check(IWorkDock other, Code claimed)
            {
                if (claimed.IsNone || other == null) return Code.None; // Existing participant-loss cleanup owns missing endpoints.
                if (other.Entity.Id == dock.Entity.Id && claimed == slot) return WorkDockCodes.Full;
                Kind(other, claimed); // Never substitute geometry for a missing authored slot.
                var overlap = Overlap(dock, carrier, place, other, CaptureDock(other, carrier, claimed));
                return overlap == SpatialOverlapStatus.Overlapping ? WorkDockCodes.Full :
                    overlap == SpatialOverlapStatus.Unsupported ? WorkDockCodes.UnsupportedGeometry : Code.None;
            }
            foreach (Job job in _jobs)
                if (job.IsLive && !ReferenceEquals(job, except))
                {
                    Code result = job.IsHaul ? Check(Dock(_world.Entities.Get(job.From)), job.SourceDockSlot) : Code.None;
                    if (result.IsNone) result = Check(Dock(_world.Entities.Get(job.IsHaul ? job.To : job.Target)), job.DestinationDockSlot);
                    if (result.IsNone) result = Check(Dock(_world.Entities.Get(job.To)), job.RecoveryDockSlot);
                    if (!result.IsNone) return result;
                }
            if (proposals != null) foreach (var proposal in proposals)
            { Code blocker = Check(proposal.Dock, proposal.Slot); if (!blocker.IsNone) return blocker; }
            return Code.None;
        }

        private static bool Before(WorkDockSlot candidate, WorkDockSlot selected)
            => candidate.Kind < selected.Kind || candidate.Kind == selected.Kind && StringComparer.Ordinal.Compare(candidate.Id.Id, selected.Id.Id) < 0;

        private static bool Cheaper(WorkDockSlot candidate, long candidateCost, WorkDockSlot selected, long selectedCost)
            => candidate.Kind < selected.Kind || candidate.Kind == selected.Kind &&
                (candidateCost < selectedCost || candidateCost == selectedCost && Before(candidate, selected));

        private Code DockRequest(IWorkDock dock, Entity carrier, WorkDockRequest request)
        {
            if (!request.IsValid) throw new ArgumentException("invalid physical dock request");
            return ReadDock(dock, carrier, () => dock.CheckRequest(_world, carrier, request));
        }
        private Code Vacancy(IWorkDock dock, Entity carrier, Code slot)
            => ReadDock(dock, carrier, () => dock.CheckVacancy(_world, carrier, slot));
        private SpatialInteractionStatus DockReach(IWorkDock dock, Entity carrier, Code slot)
        {
            return ReadDock(dock, carrier, () =>
            {
                SpatialInteractionStatus result = dock.Check(_world, carrier, slot);
                if (result < SpatialInteractionStatus.WithinReach || result > SpatialInteractionStatus.Unsupported)
                    throw new InvalidOperationException("dock provider returned an undefined reach status");
                return result;
            });
        }
        private bool DockCost(IWorkDock dock, Entity carrier, Code slot, SpatialSnapshot origin, out long cost)
        {
            long estimate = 0;
            bool result = ReadDock(dock, carrier, () =>
            {
                bool ready = dock.TryEstimateCost(_world, carrier, slot, origin, out estimate);
                if (ready && estimate < 0) throw new InvalidOperationException("dock cost must be nonnegative");
                return ready;
            });
            cost = estimate;
            return result;
        }
        private Code DockPolicy(IWorkDock dock, Entity carrier, Code slot)
        {
            if (WorkPolicyRules.Read(carrier)?.Area == null) return Code.None;
            SpatialSnapshot geometry = CaptureDock(dock, carrier, slot);
            return ReadDock(dock, carrier, () => WorkPolicyRules.CheckGeometry(_world, carrier, geometry));
        }
        private SpatialSnapshot CaptureDock(IWorkDock dock, Entity carrier, Code slot)
        {
            return ReadDock(dock, carrier, () =>
            {
                SpatialSnapshot geometry = dock.CaptureSlot(_world, slot);
                if (geometry == null || geometry.Space != _world.Space.Id || geometry.Space != dock.Entity.Get<ISpatialPresence>()?.Space)
                    throw new InvalidOperationException("dock geometry must alias the actual endpoint space");
                return geometry;
            });
        }
        private List<WorkDockSlot> OrderedSlots(IWorkDock dock, WorkDockSlotKind? kind = null, WorkDockRequest? request = null)
        {
            var slots = new List<WorkDockSlot>(); foreach (var slot in DockSlots(dock))
                if ((!kind.HasValue || slot.Kind == kind.Value) && (!request.HasValue || slot.Matches(request.Value))) slots.Add(slot);
            slots.Sort((a, b) => Before(a, b) ? -1 : Before(b, a) ? 1 : 0); return slots;
        }
        private MotionRouteCheck PlanRoute(Entity carrier, SpatialSnapshot origin, MotionDestination destination)
        {
            ISpatialRegion region = WorkPolicyRules.MotionRegion(_world, carrier);
            origin ??= EntityObservation.Read(_world, carrier, () => carrier.Get<ISpatialPresence>()?.Capture());
            if (origin == null || carrier.Get<IMotion>() == null) return new MotionRouteCheck(MotionAreaCodes.Unsupported, null, null);
            return MotionAreaContracts.Plan(_world, carrier, region, origin, destination);
        }
        private Code DockJourney(IWorkDock dock, Entity carrier, WorkDockRequest request, Code selected,
            SpatialSnapshot origin, out SpatialSnapshot arrival, out MotionTravelEstimate duration, Func<SpatialSnapshot, Code> continuation = null)
        {
            arrival = null;
            duration = new MotionTravelEstimate(null, MotionArrivalCodes.Unsupported);
            if (!SlotMatches(dock, selected, request)) return WorkDockCodes.Incompatible;
            if (WorkPolicyRules.MotionRegion(_world, carrier) == null && carrier.Get<IMotionAreaConstraint>() == null)
                return continuation?.Invoke(null) ?? Code.None;
            MotionRouteCheck approach = PlanRoute(carrier, origin, new MotionDestination(CaptureDock(dock, carrier, selected), MotionDestinationKind.Exact));
            if (!approach.Blocker.IsNone) return approach.Blocker;
            Code Finish(SpatialSnapshot service, MotionTravelEstimate prefix, out SpatialSnapshot final, out MotionTravelEstimate travel)
            {
                final = null;
                travel = new MotionTravelEstimate(null, MotionArrivalCodes.Unsupported);
                if (!(request.IsWork || request.Handling == ResourceHandlingPhase.Unloading))
                { Code check = continuation?.Invoke(service) ?? Code.None; if (check.IsNone) { final = service; travel = prefix; } return check; }
                Code refusal = MotionAreaCodes.Outside;
                var exits = new List<(WorkDockSlot Slot, long Cost, int Index)>();
                foreach (var exit in OrderedSlots(dock, WorkDockSlotKind.Departure, request))
                {
                    if (!DockPolicy(dock, carrier, exit.Id).IsNone) continue;
                    if (!DockCost(dock, carrier, exit.Id, service, out long exitCost)) continue;
                    exits.Add((exit, exitCost, exits.Count));
                }
                // All original guarded eligibility/cost reads precede ranking. The
                // first feasible exit in the SAME comparator order is definitive;
                // earlier IDs with worse estimates need no speculative route.
                exits.Sort((a, b) => Cheaper(a.Slot, a.Cost, b.Slot, b.Cost) ? -1 : Cheaper(b.Slot, b.Cost, a.Slot, a.Cost) ? 1 : 0);
                int refusalIndex = -1;
                foreach (var exit in exits)
                {
                    var route = PlanRoute(carrier, service, new MotionDestination(CaptureDock(dock, carrier, exit.Slot.Id), MotionDestinationKind.Exact));
                    Code check = route.Blocker.IsNone ? continuation?.Invoke(route.Arrival) ?? Code.None : route.Blocker;
                    if (check.IsNone)
                    {
                        final = route.Arrival; travel = MotionArrivalContracts.Combine(prefix, route.Travel.Value);
                        return Code.None;
                    }
                    // Total refusal still reports Pending, otherwise the LAST
                    // original-ID failure, not whichever ranked route ran last.
                    if (check == MotionAreaCodes.Pending) refusal = check;
                    else if (refusal != MotionAreaCodes.Pending && exit.Index > refusalIndex)
                    { refusal = check; refusalIndex = exit.Index; }
                }
                return refusal;
            }
            if (Kind(dock, selected) == WorkDockSlotKind.Departure)
            { Code check = continuation?.Invoke(approach.Arrival) ?? Code.None; if (check.IsNone) { arrival = approach.Arrival; duration = approach.Travel.Value; } return check; }
            if (Kind(dock, selected) == WorkDockSlotKind.Service) return Finish(approach.Arrival, approach.Travel.Value, out arrival, out duration);
            Code blocked = MotionAreaCodes.Outside;
            foreach (var bay in OrderedSlots(dock, WorkDockSlotKind.Service, request))
            {
                if (!DockPolicy(dock, carrier, bay.Id).IsNone) continue;
                var route = PlanRoute(carrier, approach.Arrival, new MotionDestination(CaptureDock(dock, carrier, bay.Id), MotionDestinationKind.Exact));
                Code check = route.Blocker.IsNone ? Finish(route.Arrival, MotionArrivalContracts.Combine(approach.Travel.Value, route.Travel.Value), out arrival, out duration) : route.Blocker;
                if (check.IsNone) return Code.None;
                if (blocked != MotionAreaCodes.Pending) blocked = check;
            }
            arrival = null; return blocked;
        }
        private Code KindPolicy(IWorkDock dock, Entity carrier, WorkDockSlotKind kind, WorkDockRequest request)
        {
            Code refusal = WorkDockCodes.Incompatible;
            foreach (WorkDockSlot slot in DockSlots(dock))
                if (slot.Kind == kind && slot.Matches(request))
                { Code result = DockPolicy(dock, carrier, slot.Id); if (result.IsNone) return Code.None; refusal = result; }
            return refusal;
        }
        internal Code CheckAssignedDockPolicy(Job job, Entity carrier)
        {
            Code Check(EntityId endpoint, Code slot, bool departure)
            {
                IWorkDock dock = Dock(_world.Entities.Get(endpoint));
                if (dock == null || slot.IsNone) return Code.None;
                var request = Request(job, job.IsHaul && endpoint == job.From && job.ToilIndex < 2);
                if (!SlotMatches(dock, slot, request)) return WorkDockCodes.Incompatible;
                Code refusal = DockPolicy(dock, carrier, slot); if (!refusal.IsNone) return refusal;
                if (Kind(dock, slot) == WorkDockSlotKind.Waiting)
                { refusal = KindPolicy(dock, carrier, WorkDockSlotKind.Service, request); if (!refusal.IsNone) return refusal; }
                return departure ? KindPolicy(dock, carrier, WorkDockSlotKind.Departure, request) : Code.None;
            }
            if (job.IsHaul && job.ToilIndex < 2)
            { Code refusal = Check(job.From, job.SourceDockSlot, false); if (!refusal.IsNone) return refusal; }
            return Check(job.IsHaul ? job.To : job.Target, job.DestinationDockSlot, true);
        }
        private SpatialInteractionStatus DockApproach(IWorkDock dock, Entity carrier, Code slot)
        {
            return ReadDock(dock, null, () =>
            {
                var facts = EntityObservation.Capture(_world, carrier, allowEntityVersionChange: true);
                IMotion motion = carrier.Get<IMotion>();
                var components = new List<EntityObservation.ProviderScope>();
                foreach (Component component in carrier.Components)
                    if (!ReferenceEquals(component, motion))
                        components.Add(EntityObservation.CaptureProvider(_world, component, component.Sync, allowEntityVersionChange: true));
                var presence = carrier.Get<ISpatialPresence>();
                SpatialSnapshot before = presence == null ? null : EntityObservation.ReadProvider(_world, presence, carrier, () => presence.Capture(), observeTargetState: true);
                SpatialInteractionStatus result = dock.Approach(_world, carrier, slot);
                if (result < SpatialInteractionStatus.WithinReach || result > SpatialInteractionStatus.Unsupported)
                    throw new InvalidOperationException("dock approach returned undefined status");
                facts.Verify();
                foreach (var component in components) component.Verify();
                SpatialSnapshot after = presence == null ? null : EntityObservation.ReadProvider(_world, presence, carrier, () => presence.Capture(), observeTargetState: true);
                if (!ReferenceEquals(carrier.Get<IMotion>(), motion) || !ReferenceEquals(carrier.Get<ISpatialPresence>(), presence) ||
                    before != null && !before.Matches(after))
                    throw new InvalidOperationException("dock approach changed actual carrier geometry or capability binding");
                return result;
            });
        }

        /// <summary>Pure admission, before any job/native promise allocation.</summary>
        internal Code PreviewDock(Entity endpoint, Entity carrier, WorkDockRequest request, SpatialSnapshot origin,
            out Code selected, out long cost, out SpatialSnapshot arrival, out MotionTravelEstimate duration, Job current = null, Func<SpatialSnapshot, Code> continuation = null,
            bool productiveOnly = false, IReadOnlyList<DockProposal> proposals = null, ISet<Code> excluded = null, Code? requiredSlot = null)
        {
            selected = Code.None; cost = 0; arrival = null;
            duration = new MotionTravelEstimate(null, MotionArrivalCodes.Unsupported);
            IWorkDock dock = Dock(endpoint);
            if (dock == null)
            {
                if (requiredSlot.HasValue && !requiredSlot.Value.IsNone) return WorkDockCodes.Missing;
                if (excluded?.Contains(Code.None) == true) return WorkDockCodes.Full;
                // A supplied origin describes a future itinerary leg. Its route is checked
                // below through PlanRoute; the carrier's current-location reach is irrelevant.
                SpatialInteractionStatus? status = origin == null ? SpatialInteraction.Check(_world, carrier, endpoint) : (SpatialInteractionStatus?)null;
                if ((status.HasValue && !SpatialInteraction.CanApproach(status.Value)) || !SpatialInteraction.TryEstimateCost(_world, carrier, endpoint, out cost))
                    return WorkSystem.BlockerUnreachable;
                // Local stationary work requires no fictitious motion provider.
                if (origin == null && status == SpatialInteractionStatus.WithinReach && carrier.Get<IMotion>() == null)
                {
                    SpatialSnapshot local = EntityObservation.Read(_world, carrier, () => carrier.Get<ISpatialPresence>()?.Capture());
                    Code policy = EntityObservation.Read(_world, carrier, () => WorkPolicyRules.CheckGeometry(_world, carrier, local));
                    arrival = local;
                    Code check = policy.IsNone ? continuation?.Invoke(arrival) ?? Code.None : policy;
                    if (check.IsNone) duration = new MotionTravelEstimate(0, Code.None);
                    return check;
                }
                if (WorkPolicyRules.MotionRegion(_world, carrier) == null && carrier.Get<IMotionAreaConstraint>() == null)
                    return continuation?.Invoke(null) ?? Code.None;
                var geometry = EntityObservation.Read(_world, endpoint, () => endpoint.Get<ISpatialPresence>()?.Capture());
                if (geometry == null) return MotionAreaCodes.Unsupported;
                var route = PlanRoute(carrier, origin, new MotionDestination(geometry, MotionDestinationKind.Interaction));
                if (!route.Blocker.IsNone) return route.Blocker;
                Code result = continuation?.Invoke(route.Arrival) ?? Code.None;
                if (result.IsNone) { arrival = route.Arrival; duration = route.Travel.Value; }
                return result;
            }
            bool owned = current?.Worker == carrier.Id && CurrentDockEndpoint(current) == endpoint.Id;
            bool departing = !productiveOnly && owned && current.ToilIndex < current.Toils.Count && _world.Services.Get<IWorkPhaseCatalog>().Purpose(current.Toils[current.ToilIndex].Executor) == WorkPhasePurpose.Departure;
            return SelectDock(dock, carrier, request, origin, out selected, out cost, out arrival, out duration, current,
                owned ? current.IsHaul && current.From == endpoint.Id && current.ToilIndex < 2
                    ? current.SourceDockSlot : current.DestinationDockSlot : Code.None, departing,
                !owned || current.HandlingProgress.Equals(WorkProgress.Zero), continuation, productiveOnly, proposals, excluded, requiredSlot);
        }

        // One pure selector for service admission and physical departure. Departure requires its
        // actual exit journey, not future output capacity; the bay clears only at physical arrival.
        private Code SelectDock(IWorkDock dock, Entity carrier, WorkDockRequest request, SpatialSnapshot origin,
            out Code selected, out long cost, out SpatialSnapshot arrival, out MotionTravelEstimate duration, Job current, Code retained,
            bool departing, bool allowWaiting, Func<SpatialSnapshot, Code> continuation = null,
            bool productiveOnly = false, IReadOnlyList<DockProposal> proposals = null, ISet<Code> excluded = null, Code? requiredSlot = null)
        {
            selected = Code.None; cost = 0; arrival = null;
            duration = new MotionTravelEstimate(null, MotionArrivalCodes.Unsupported);
            // Leaving an already occupied facility is physical movement, not fresh service admission.
            Code refusal = departing ? Code.None : DockRequest(dock, carrier, request);
            if (!refusal.IsNone) return refusal;
            bool Allowed(WorkDockSlot slot) => slot.Matches(request) && (!requiredSlot.HasValue || slot.Id == requiredSlot.Value) && excluded?.Contains(slot.Id) != true && (productiveOnly ? slot.Kind == WorkDockSlotKind.Service : departing ? slot.Kind == WorkDockSlotKind.Departure :
                slot.Kind == WorkDockSlotKind.Service || allowWaiting && slot.Kind == WorkDockSlotKind.Waiting);
            Code Check(WorkDockSlot slot, bool keep, out long travel)
            {
                travel = 0;
                Code policy = DockPolicy(dock, carrier, slot.Id); if (!policy.IsNone) return policy;
                Code claim = ClaimBlocker(dock, carrier, slot.Id, current, proposals); if (!claim.IsNone) return claim;
                if (!keep && slot.Kind == WorkDockSlotKind.Service)
                { Code queue = QueueBlocker(dock, carrier, slot, current); if (!queue.IsNone) return queue; }
                // A valid existing claim can wait for transient occupancy; a replacement cannot
                // steal an occupied slot. Structural route checks still reject static obstructions.
                if (!keep && !Vacancy(dock, carrier, slot.Id).IsNone) return WorkDockCodes.Occupied;
                if (!DockCost(dock, carrier, slot.Id, origin, out travel) || origin == null &&
                    WorkPolicyRules.MotionRegion(_world, carrier) == null && !SpatialInteraction.CanApproach(DockReach(dock, carrier, slot.Id)))
                    return WorkSystem.BlockerUnreachable;
                return Code.None;
            }
            Code retainedBlocker = Code.None;
            if (!retained.IsNone)
                foreach (WorkDockSlot slot in DockSlots(dock))
                    if (slot.Id == retained && Allowed(slot))
                    {
                        retainedBlocker = Check(slot, true, out cost);
                        if (retainedBlocker.IsNone)
                            retainedBlocker = DockJourney(dock, carrier, request, slot.Id, origin, out arrival, out duration, continuation);
                        if (retainedBlocker.IsNone) { selected = retained; return Code.None; }
                        break;
                    }
            if (departing)
                refusal = KindPolicy(dock, carrier, WorkDockSlotKind.Departure, request);
            else
                refusal = KindPolicy(dock, carrier, WorkDockSlotKind.Service, request);
            if (!refusal.IsNone) return refusal;
            if (!departing && (request.IsWork || request.Handling == ResourceHandlingPhase.Unloading))
            { refusal = KindPolicy(dock, carrier, WorkDockSlotKind.Departure, request); if (!refusal.IsNone) return refusal; }
            Code routeBlocker = retainedBlocker;
            int refusalIndex = retainedBlocker.IsNone ? int.MaxValue : -1;
            void Refuse(Code blocker, int index)
            {
                if (blocker == MotionAreaCodes.Pending) routeBlocker = blocker;
                else if (!blocker.IsNone && routeBlocker != MotionAreaCodes.Pending && index < refusalIndex)
                { routeBlocker = blocker; refusalIndex = index; }
            }
            var slots = OrderedSlots(dock);
            var candidates = new List<(WorkDockSlot Slot, long Cost, int Index)>();
            for (int first = 0; first < slots.Count;)
            {
                int end = first + 1;
                while (end < slots.Count && slots[end].Kind == slots[first].Kind) end++;
                candidates.Clear();
                // Read the original guarded eligibility/cost callbacks in ID order for
                // this kind. A feasible service must not probe unused waiting kinds.
                for (int index = first; index < end; index++)
                {
                    WorkDockSlot slot = slots[index];
                    if (!Allowed(slot) || slot.Id == retained || !DockPolicy(dock, carrier, slot.Id).IsNone) continue;
                    Code check = Check(slot, false, out long travel);
                    if (check.IsNone) candidates.Add((slot, travel, index));
                    else Refuse(check, index);
                }
                candidates.Sort((a, b) => Cheaper(a.Slot, a.Cost, b.Slot, b.Cost) ? -1 : Cheaper(b.Slot, b.Cost, a.Slot, a.Cost) ? 1 : 0);
                foreach (var candidate in candidates)
                {
                    Code journey = DockJourney(dock, carrier, request, candidate.Slot.Id, origin, out var finish, out var exposure, continuation);
                    if (!journey.IsNone) { Refuse(journey, candidate.Index); continue; }
                    selected = candidate.Slot.Id; cost = candidate.Cost; arrival = finish; duration = exposure;
                    return Code.None;
                }
                first = end;
            }
            cost = 0; arrival = null; return !routeBlocker.IsNone ? routeBlocker : departing ? WorkDockCodes.DepartureBlocked : WorkDockCodes.Full;
        }

        // One remaining-journey selector serves pure funding and actual dock rebinding.
        // Durations are freshly measured from actual geometry, never decremented estimates.
        private Code SelectRemainingJourney(Job job, Entity carrier, out Code sourceSlot,
            out Code destinationSlot, out MotionTravelEstimate travel)
        {
            bool pickup = job.IsHaul && job.From.IsValid && job.ToilIndex < 2;
            Entity destination = _world.Entities.Get(job.IsHaul ? job.To : job.Target);
            IWorkDock destinationDock = Dock(destination);
            Code nextDestination = job.DestinationDockSlot, nextSource = job.SourceDockSlot;
            MotionTravelEstimate delivery = new MotionTravelEstimate(null, MotionArrivalCodes.Unsupported),
                collection = new MotionTravelEstimate(0, Code.None);
            bool departing = job.ToilIndex < job.Toils.Count && _world.Services.Get<IWorkPhaseCatalog>().Purpose(job.Toils[job.ToilIndex].Executor) == WorkPhasePurpose.Departure;
            Code Destination(SpatialSnapshot origin)
            {
                if (destination == null) return Codes.NotFound;
                // A non-docked endpoint has no physical departure leg.
                if (departing && destinationDock == null)
                { delivery = new MotionTravelEstimate(0, Code.None); return Code.None; }
                if (destinationDock == null)
                    return PreviewDock(destination, carrier, Request(job, false), origin, out _, out _, out _, out delivery, job);
                // Future delivery has not earned handling yet. An active partial service must
                // retain that progress and can move only to another service slot, never a wait.
                return SelectDock(destinationDock, carrier, Request(job, false), origin, out nextDestination,
                    out _, out _, out delivery, job, job.DestinationDockSlot, departing,
                    pickup || job.HandlingProgress.Equals(WorkProgress.Zero));
            }
            Code result;
            if (pickup)
            {
                Entity source = _world.Entities.Get(job.From); IWorkDock sourceDock = Dock(source);
                if (source == null) result = Codes.NotFound;
                else if (sourceDock == null)
                    result = PreviewDock(source, carrier, Request(job, true), null, out _, out _, out _, out collection, job, Destination);
                else
                    result = SelectDock(sourceDock, carrier, Request(job, true), null, out nextSource, out _, out _, out collection,
                        job, job.SourceDockSlot, false, job.HandlingProgress.Equals(WorkProgress.Zero), Destination);
            }
            else result = Destination(null);
            sourceSlot = nextSource; destinationSlot = nextDestination;
            travel = result.IsNone ? MotionArrivalContracts.Combine(collection, delivery) : new MotionTravelEstimate(null, result);
            return result;
        }

        internal MotionTravelEstimate ReadRemainingHaulTravel(Job job, Entity carrier)
        {
            if (!job.IsHaul || !job.IsLive || job.Worker != carrier.Id || carrier.Get<IWorker>()?.Job != job.Id)
                throw new InvalidOperationException("remaining haul travel requires the actual retained job/carrier");
            if (job.ToilIndex >= job.Toils.Count) return new MotionTravelEstimate(0, Code.None);
            if (!job.RecoveryCause.IsNone && job.RecoveryDockSlot.IsNone && RecoveryDepartureReached(job, carrier))
                return new MotionTravelEstimate(0, Code.None); // Future delivery is not admitted while waiting at the actual exit.
            SelectRemainingJourney(job, carrier, out _, out _, out var travel);
            return travel;
        }

        /// <summary>Mutating execution only: atomically replace invalid existing dock intentions.</summary>
        internal Code ReplanDockBindings(Job job, Entity carrier)
        {
            if (!job.RecoveryCause.IsNone && job.RecoveryDockSlot.IsNone)
                return CheckAssignedDockPolicy(job, carrier); // A completed departure waits at its actual exit, not a new speculative exit.
            bool pickup = job.IsHaul && job.From.IsValid && job.ToilIndex < 2;
            if (Dock(_world.Entities.Get(job.IsHaul ? job.To : job.Target)) == null &&
                (!pickup || Dock(_world.Entities.Get(job.From)) == null)) return Code.None;
            Code result = SelectRemainingJourney(job, carrier, out var nextSource, out var nextDestination, out _);
            if (!result.IsNone) return result;
            bool currentChanged = pickup ? nextSource != job.SourceDockSlot : nextDestination != job.DestinationDockSlot;
            if (currentChanged)
            {
                // Stop only obsolete current motion; no coordinates, receipts or earned work change.
                MotionExecutionContracts.Stop(_world, carrier);
                if (job.ToilIndex < job.Toils.Count)
                {
                    WorkPhasePurpose purpose = _world.Services.Get<IWorkPhaseCatalog>().Purpose(job.Toils[job.ToilIndex].Executor);
                    if (purpose == WorkPhasePurpose.Productive || purpose == WorkPhasePurpose.Pickup || purpose == WorkPhasePurpose.Delivery)
                    { Code wait = ReapproachActivity(job); if (!wait.IsNone) return wait; }
                }
            }
            job.SourceDockSlot = nextSource; job.DestinationDockSlot = nextDestination;
            return Code.None;
        }

        internal void ReleaseDepartedDock(Job job, Entity carrier)
        {
            if (job.SourceDockSlot.IsNone || job.ToilIndex < 2) return;
            IWorkDock dock = Dock(_world.Entities.Get(job.From));
            if (dock == null || DockReach(dock, carrier, job.SourceDockSlot) != SpatialInteractionStatus.WithinReach)
                job.SourceDockSlot = Code.None;
        }

        private bool ServiceAvailable(IWorkDock dock, Entity carrier, WorkDockSlot slot, Job job)
            => slot.Kind == WorkDockSlotKind.Service && slot.Matches(Request(job, job.IsHaul && job.ToilIndex < 2)) && DockPolicy(dock, carrier, slot.Id).IsNone &&
                ClaimBlocker(dock, carrier, slot.Id, job).IsNone && Vacancy(dock, carrier, slot.Id).IsNone &&
                SpatialInteraction.CanApproach(DockReach(dock, carrier, slot.Id)) &&
                DockJourney(dock, carrier, Request(job, job.IsHaul && job.ToilIndex < 2), slot.Id, null, out _, out _,
                    continuation: job.IsHaul && job.ToilIndex < 2 ? origin => RemainingDelivery(job, carrier, origin) : null).IsNone;
        private Code RemainingDelivery(Job job, Entity carrier, SpatialSnapshot origin)
        {
            Entity endpoint = _world.Entities.Get(job.To); if (endpoint == null) return Codes.NotFound;
            var dock = Dock(endpoint);
            if (dock != null) return DockJourney(dock, carrier, WorkDockRequest.Haul(job.Resource, ResourceHandlingPhase.Unloading),
                job.DestinationDockSlot, origin, out _, out _);
            if (WorkPolicyRules.MotionRegion(_world, carrier) == null) return Code.None;
            SpatialSnapshot target = EntityObservation.Read(_world, endpoint, () => endpoint.Get<ISpatialPresence>()?.Capture());
            return target == null ? MotionAreaCodes.Unsupported : PlanRoute(carrier, origin, new MotionDestination(target, MotionDestinationKind.Interaction)).Blocker;
        }

        // FIFO over arrived eligible jobs, scoped to the candidate's physical bay, not endpoint IDs.
        // Retained service claims never participate in this pure new-admission/promotion check.
        private Code QueueBlocker(IWorkDock candidateDock, Entity carrier, WorkDockSlot candidate, Job current)
        {
            SpatialSnapshot place = null;
            foreach (Job other in _jobs)
            {
                if (other.State != JobState.Assigned || ReferenceEquals(other, current) || other.RetryAt > _world.Tick ||
                    current != null && (other.CreatedTick > current.CreatedTick || other.CreatedTick == current.CreatedTick && other.Id > current.Id)) continue;
                bool source = other.IsHaul && other.From.IsValid && other.ToilIndex < 2;
                Code slot = source ? other.SourceDockSlot : other.DestinationDockSlot;
                IWorkDock dock = Dock(_world.Entities.Get(source ? other.From : other.IsHaul ? other.To : other.Target));
                Entity worker = _world.Entities.Get(other.Worker);
                if (dock == null || worker == null || slot.IsNone || Kind(dock, slot) != WorkDockSlotKind.Waiting ||
                    DockReach(dock, worker, slot) != SpatialInteractionStatus.WithinReach ||
                    !EntityObservation.Read(_world, worker, () => _world.System<WorkSystem>().CheckJob(_world, worker, other)).IsNone ||
                    !DockRequest(dock, worker, Request(other, source)).IsNone) continue;
                foreach (WorkDockSlot bay in DockSlots(dock))
                {
                    if (!ServiceAvailable(dock, worker, bay, other)) continue;
                    place ??= CaptureDock(candidateDock, carrier, candidate.Id);
                    SpatialSnapshot service = CaptureDock(dock, worker, bay.Id);
                    var overlap = EntityObservation.Read(_world, worker, () => Overlap(candidateDock, carrier, place, dock, service));
                    if (overlap == SpatialOverlapStatus.Unsupported) return WorkDockCodes.UnsupportedGeometry;
                    if (overlap == SpatialOverlapStatus.Overlapping) return WorkDockCodes.Waiting;
                }
            }
            return Code.None;
        }

        private static WorkDockRequest Request(Job job, bool source)
            => job.IsHaul ? WorkDockRequest.Haul(job.Resource, source ? ResourceHandlingPhase.Loading : ResourceHandlingPhase.Unloading) : WorkDockRequest.Work(job.Role);

        internal SpatialInteractionStatus ApproachDock(Job job, Entity carrier, Entity endpoint)
        {
            bool source = job.IsHaul && job.From.IsValid && job.ToilIndex < 2;
            Code slot = source ? job.SourceDockSlot : job.DestinationDockSlot;
            IWorkDock dock = Dock(endpoint);
            if (dock == null) return SpatialInteraction.Approach(_world, carrier, endpoint);
            Code request = DockRequest(dock, carrier, Request(job, source));
            if (!request.IsNone) { job.Blocker = request; return SpatialInteractionStatus.Unreachable; }
            if (!SlotMatches(dock, slot, Request(job, source))) { job.Blocker = WorkDockCodes.Incompatible; return SpatialInteractionStatus.Unreachable; }
            Code policy = DockPolicy(dock, carrier, slot);
            if (!policy.IsNone) { job.Blocker = policy; return SpatialInteractionStatus.Pending; }
            if (Kind(dock, slot) == WorkDockSlotKind.Waiting)
            {
                if (DockReach(dock, carrier, slot) == SpatialInteractionStatus.WithinReach)
                {
                    // Promotion is fresh service admission through the same selector as initial
                    // dispatch, including provider cost, physical FIFO and the remaining delivery.
                    Code promotion = PreviewDock(endpoint, carrier, Request(job, source), null, out Code next,
                        out _, out _, out _, job, productiveOnly: true,
                        continuation: source ? origin => RemainingDelivery(job, carrier, origin) : null);
                    if (promotion.IsNone)
                    {
                        slot = next;
                        if (source) job.SourceDockSlot = slot; else job.DestinationDockSlot = slot;
                    }
                }
                if (Kind(dock, slot) == WorkDockSlotKind.Waiting)
                {
                    job.Blocker = WorkDockCodes.Waiting;
                    SpatialInteractionStatus wait = DockApproach(dock, carrier, slot);
                    if (wait == SpatialInteractionStatus.WithinReach) carrier.Get<IMotion>()?.Stop();
                    return wait == SpatialInteractionStatus.WithinReach ? SpatialInteractionStatus.Pending : wait;
                }
            }
            if (!Vacancy(dock, carrier, slot).IsNone)
            { job.Blocker = WorkDockCodes.Occupied; carrier.Get<IMotion>()?.Stop(); return SpatialInteractionStatus.Pending; }
            return DockApproach(dock, carrier, slot);
        }

        // Pure physical occupancy, independent of changed policy; never releases a bay under a vehicle.
        internal bool OccupiesDeliveryDock(Job job, Entity carrier)
        {
            Entity endpoint = _world.Entities.Get(job.To);
            if (endpoint == null) return false;
            IWorkDock dock = Dock(endpoint);
            return dock == null ? SpatialInteraction.CheckProximity(_world, carrier, endpoint) == SpatialProximityStatus.WithinReach
                : !job.DestinationDockSlot.IsNone && DockReach(dock, carrier, job.DestinationDockSlot) == SpatialInteractionStatus.WithinReach;
        }
        internal SpatialInteractionStatus CheckDockReach(Job job, Entity carrier, Entity endpoint)
        {
            SpatialInteractionStatus status = ReadDockReach(job, carrier, endpoint, out Code blocker);
            if (!blocker.IsNone) job.Blocker = blocker;
            return status;
        }
        private SpatialInteractionStatus ReadDockReach(Job job, Entity carrier, Entity endpoint, out Code blocker)
        {
            blocker = Code.None;
            bool source = job.IsHaul && job.From.IsValid && job.ToilIndex < 2;
            Code slot = source ? job.SourceDockSlot : job.DestinationDockSlot;
            IWorkDock dock = Dock(endpoint);
            if (dock == null) return SpatialInteraction.Check(_world, carrier, endpoint);
            Code policy = DockPolicy(dock, carrier, slot);
            if (!policy.IsNone) { blocker = policy; return SpatialInteractionStatus.Pending; }
            if (Kind(dock, slot) != WorkDockSlotKind.Service) return SpatialInteractionStatus.Pending;
            return SlotMatches(dock, slot, Request(job, source)) && DockRequest(dock, carrier, Request(job, source)).IsNone &&
                Vacancy(dock, carrier, slot).IsNone ? DockReach(dock, carrier, slot) : SpatialInteractionStatus.Pending;
        }

        internal SpatialInteractionStatus DepartDock(Job job, Entity carrier, Entity endpoint)
        {
            IWorkDock dock = Dock(endpoint);
            if (dock == null) return SpatialInteractionStatus.WithinReach;
            Code slot = job.DestinationDockSlot;
            if (Kind(dock, slot) != WorkDockSlotKind.Departure || !SlotMatches(dock, slot, Request(job, false)))
            {
                Code refusal = SelectDock(dock, carrier, Request(job, false), null, out Code selected, out _, out _, out _,
                    job, Code.None, true, false);
                if (!refusal.IsNone)
                { job.Blocker = refusal; carrier.Get<IMotion>()?.Stop(); return SpatialInteractionStatus.Pending; }
                slot = selected; job.DestinationDockSlot = slot;
            }
            if (!Vacancy(dock, carrier, slot).IsNone)
            { job.Blocker = WorkDockCodes.DepartureBlocked; carrier.Get<IMotion>()?.Stop(); return SpatialInteractionStatus.Pending; }
            return DockApproach(dock, carrier, slot);
        }

        internal Code PreviewRecoveryDeparture(Job job, Entity carrier, out Code slot)
        {
            IWorkDock dock = Dock(_world.Entities.Get(job.To));
            if (dock == null) { slot = Code.None; return Codes.NotFound; }
            return SelectDock(dock, carrier, Request(job, false), null, out slot, out _, out _, out _,
                job, Code.None, true, false);
        }
        internal bool RecoveryDepartureReached(Job job, Entity carrier)
        {
            IWorkDock dock = Dock(_world.Entities.Get(job.To));
            return dock != null && !job.DestinationDockSlot.IsNone && Kind(dock, job.DestinationDockSlot) == WorkDockSlotKind.Departure &&
                SlotMatches(dock, job.DestinationDockSlot, Request(job, false)) &&
                DockReach(dock, carrier, job.DestinationDockSlot) == SpatialInteractionStatus.WithinReach;
        }
        internal void CompleteRecoveryDeparture(Job job, Entity carrier)
        {
            if (job.RecoveryCause.IsNone || !RecoveryDepartureReached(job, carrier))
                throw new InvalidOperationException("recovery bay cannot clear before actual physical departure");
            job.RecoveryDockSlot = Code.None;
        }
        internal HaulRecoveryStage ReadRecoveryStage(Job job)
            => job.IsLive && job.IsHaul && job.State == JobState.Paused && job.Blocker == HaulRecoveryCodes.DestinationUnavailable
                ? HaulRecoveryStage.WaitingForDestination : job.RecoveryCause.IsNone ? HaulRecoveryStage.None :
                job.RecoveryDockSlot.IsNone && _world.Entities.Get(job.Worker) is Entity worker && RecoveryDepartureReached(job, worker)
                    ? HaulRecoveryStage.WaitingForDestination : HaulRecoveryStage.Departing;

        private static EntityId CurrentDockEndpoint(Job job)
            => job.IsHaul ? job.From.IsValid && job.ToilIndex < 2 ? job.From : job.To : job.Target;

        private bool WaitingDock(Job job, out IWorkDock dock, out Code slot)
        {
            dock = null; slot = Code.None;
            if (job == null || !job.IsLive) return false;
            bool source = job.IsHaul && job.From.IsValid && job.ToilIndex < 2;
            dock = Dock(_world.Entities.Get(CurrentDockEndpoint(job)));
            slot = source ? job.SourceDockSlot : job.DestinationDockSlot;
            return dock != null && !slot.IsNone && Kind(dock, slot) == WorkDockSlotKind.Waiting;
        }

        public bool IsDockWaiting(Job job) => WaitingDock(job, out _, out _);
        public Code ReadDockWaiting(Job job, out bool waiting, out bool arrived)
        {
            waiting = arrived = false;
            if (job == null || !ReferenceEquals(Job(job.Id), job)) return Codes.NotFound;
            if (!WaitingDock(job, out var dock, out var slot)) return Code.None;
            waiting = true;
            Entity carrier = _world.Entities.Get(job.Worker);
            if (carrier == null || !carrier.Alive) return Codes.NotFound;
            SpatialInteractionStatus status = DockReach(dock, carrier, slot);
            if (status == SpatialInteractionStatus.Unsupported) return WorkDockCodes.UnsupportedGeometry;
            arrived = status == SpatialInteractionStatus.WithinReach;
            return Code.None;
        }

        private void ValidateDockBinding(Job job, bool source, bool restoring)
        {
            Code slot = source ? job.SourceDockSlot : job.DestinationDockSlot;
            EntityId endpointId = source ? job.IsHaul ? job.From : EntityId.None : job.IsHaul ? job.To : job.Target;
            if (!job.IsLive || !endpointId.IsValid || !job.IsHaul && (job.State == JobState.Open || !job.Worker.IsValid))
            { if (!slot.IsNone) throw HaulError(restoring, "ended/open/unassigned job owns dock commitment"); return; }
            Entity endpoint = _world.Entities.Get(endpointId);
            if (endpoint == null) return; // Normal physical participant-loss cleanup on next Update.
            IWorkDock dock = Dock(endpoint);
            if (dock == null)
            { if (!slot.IsNone) throw HaulError(restoring, "undocked endpoint owns a dock commitment"); return; }
            if (source && job.ToilIndex >= 2 && slot.IsNone) return; // Physically departed pickup.
            if (slot.IsNone) throw HaulError(restoring, "docked job has no physical slot");
            WorkDockSlotKind kind;
            try { kind = Kind(dock, slot); }
            catch (InvalidOperationException) { throw HaulError(restoring, "saved job names missing dock slot"); }
            if (!SlotMatches(dock, slot, Request(job, source)))
                throw HaulError(restoring, "dock commitment is incompatible with its actual work/resource request");
            if (!ClaimBlocker(dock, _world.Entities.Get(job.Worker) ?? endpoint, slot, job).IsNone)
                throw HaulError(restoring, "dock geometry is unsupported or claimed by more than one job");
            bool current = source == (job.IsHaul && job.From.IsValid && job.ToilIndex < 2);
            bool departing = job.ToilIndex < job.Toils.Count && _world.Services.Get<IWorkPhaseCatalog>().Purpose(job.Toils[job.ToilIndex].Executor) == WorkPhasePurpose.Departure;
            if (source && job.ToilIndex >= 2 && kind != WorkDockSlotKind.Service ||
                !source && departing && kind == WorkDockSlotKind.Waiting || !source && !departing && kind == WorkDockSlotKind.Departure ||
                source && job.ToilIndex < 2 && kind == WorkDockSlotKind.Departure ||
                current && kind == WorkDockSlotKind.Waiting && (!job.HandlingProgress.Equals(WorkProgress.Zero) ||
                    !job.IsHaul && job.ToilIndex != 0))
                throw HaulError(restoring, "waiting dock owns completed or elapsed service");
        }

        internal void ValidateDocks(Job job, bool restoring)
        {
            ValidateDockBinding(job, true, restoring); ValidateDockBinding(job, false, restoring);
            if (!job.RecoveryDockSlot.IsNone && (!job.IsHaul || !job.IsLive || job.RecoveryCause.IsNone))
                throw HaulError(restoring, "retained recovery bay has no live departure owner");
            if (job.RecoveryCause.IsNone) return;
            Entity endpoint = _world.Entities.Get(job.To); if (endpoint == null) return;
            IWorkDock dock = Dock(endpoint); Entity worker = _world.Entities.Get(job.Worker);
            if (dock == null) throw HaulError(restoring, "recovery departure lost its physical dock");
            if (job.RecoveryDockSlot.IsNone)
            { if (worker != null && !RecoveryDepartureReached(job, worker)) throw HaulError(restoring, "recovery released its old bay before physical departure"); }
            else if (Kind(dock, job.RecoveryDockSlot) == WorkDockSlotKind.Departure ||
                !SlotMatches(dock, job.RecoveryDockSlot, Request(job, false)) ||
                !ClaimBlocker(dock, worker ?? endpoint, job.RecoveryDockSlot, job).IsNone)
                throw HaulError(restoring, "retained recovery bay is invalid or multiply claimed");
        }

        internal void ValidateActivityItinerary(Job job, bool restoring)
        {
            if (job.IsHaul || job.OrderId == 0) return;
            Entity endpoint = _world.Entities.Get(job.Target);
            Order order = Order(job.OrderId); IActivityNodeDefinition definition = order?.Plan.Graph.Find(job.NodeId);
            if (definition == null || !ActivityExecutorContracts.RequiresWorker(definition) || job.Target != order.Target || job.Toils.Count == 0 ||
                job.ToilIndex < 0 || job.ToilIndex > job.Toils.Count) throw HaulError(restoring, "invalid activity executor itinerary");
            var context = ExecutorContext(order, definition, _world.Entities.Get(job.Worker));
            bool matches = ActivityExecutorContracts.Read(_world, definition.Executor, endpoint, context.Worker,
                () => definition.Executor.MatchesItinerary(context, job.Toils.AsReadOnly()));
            if (!matches) throw HaulError(restoring, "activity itinerary disagrees with its authored executor");
            if (job.IsLive && (job.ToilIndex == job.Toils.Count ||
                _world.Services.Get<IWorkPhaseCatalog>().Purpose(job.Toils[job.ToilIndex].Executor) == WorkPhasePurpose.Departure))
            {
                ActivityNodeExecution node = order.Node(job.NodeId);
                if (node == null || !node.Progress.Complete(_world.Time.Ticks(definition.Duration)))
                    throw HaulError(restoring, "activity cannot depart before earning its authored work");
            }
            ValidateDocks(job, restoring);
        }

        private bool HasDockClaim(Entity endpoint)
        {
            foreach (Job job in _jobs)
                if (job.IsLive && (job.IsHaul && job.From == endpoint.Id && !job.SourceDockSlot.IsNone ||
                    (job.IsHaul ? job.To : job.Target) == endpoint.Id && (!job.DestinationDockSlot.IsNone || !job.RecoveryDockSlot.IsNone))) return true;
            return false;
        }
        public Code CheckDockRemoval(Entity endpoint) => HasDockClaim(endpoint) ? ResourceTransactions.ActivePromises : Code.None;
        public Code CheckEndpointMotion(Entity endpoint)
        {
            if (endpoint == null || !endpoint.Alive || !ReferenceEquals(_world.Entities.Get(endpoint.Id), endpoint)) return Codes.NotFound;
            return HasDockClaim(endpoint) ? WorkDockCodes.ServiceHold : Code.None;
        }
    }
}
