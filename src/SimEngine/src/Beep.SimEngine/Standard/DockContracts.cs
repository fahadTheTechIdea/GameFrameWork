using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum WorkDockSlotKind : byte { Service = 1, Waiting = 2, Departure = 3, Parking = 4, Rollout = 5 }

    /// <summary>Immutable authored physical place; commitments belong to existing jobs, occupancy to geometry.</summary>
    public readonly struct WorkDockSlot
    {
        public Code Id { get; }
        public WorkDockSlotKind Kind { get; }
        public WorkerRoleId WorkRole { get; }
        public ResourceId Resource { get; }
        public WorkDockSlot(Code id, WorkDockSlotKind kind, WorkerRoleId workRole, ResourceId resource)
        {
            if (id.IsNone || kind < WorkDockSlotKind.Service || kind > WorkDockSlotKind.Rollout)
                throw new ArgumentException("dock slot requires stable identity and declared kind");
            if (workRole != WorkerRoleId.None && !workRole.IsValid || resource != ResourceId.None && !resource.IsValid || workRole.IsValid && resource.IsValid)
                throw new ArgumentException("dock slot admits one role, one resource, or its endpoint's authored policy");
            if (kind == WorkDockSlotKind.Parking && resource.IsValid) throw new ArgumentException("home parking may filter worker roles, not handled resources");
            if (kind == WorkDockSlotKind.Rollout && (resource.IsValid || workRole.IsValid)) throw new ArgumentException("launch eligibility belongs to authored outputs, not service filters");
            Id = id; Kind = kind; WorkRole = workRole; Resource = resource;
        }
        public bool Matches(WorkDockRequest request) => !Id.IsNone && Kind >= WorkDockSlotKind.Service && Kind <= WorkDockSlotKind.Departure &&
            request.IsValid && (WorkRole.IsValid ? request.IsWork && request.Role == WorkRole :
            Resource.IsValid ? !request.IsWork && request.Resource == Resource : true);
    }

    /// <summary>Single strict slot metadata reader/resolver; geometry remains with the selected provider.</summary>
    public sealed class WorkDockSlotDeclaration
    {
        public Code Id { get; private set; }
        private WorkDockSlotKind _kind;
        private string _role, _resource;
        public static WorkDockSlotDeclaration Read(JsonReader reader)
        {
            string id = reader.String("id"), kind = reader.String("kind");
            string role = reader.String("work_role", null), resource = reader.String("resource", null);
            if (string.IsNullOrWhiteSpace(id) || kind != "service" && kind != "waiting" && kind != "departure" && kind != "parking" && kind != "rollout" ||
                role != null && string.IsNullOrWhiteSpace(role) || resource != null && string.IsNullOrWhiteSpace(resource) || role != null && resource != null ||
                kind == "parking" && resource != null || kind == "rollout" && (role != null || resource != null))
                throw reader.Error("slot needs identity/kind and at most one nonempty work_role or resource filter");
            return new WorkDockSlotDeclaration { Id = new Code(id), _role = role, _resource = resource,
                _kind = kind == "service" ? WorkDockSlotKind.Service : kind == "waiting" ? WorkDockSlotKind.Waiting : kind == "departure" ? WorkDockSlotKind.Departure : kind == "parking" ? WorkDockSlotKind.Parking : WorkDockSlotKind.Rollout };
        }
        public WorkDockSlot Resolve(ContentResolver content) => new WorkDockSlot(Id, _kind,
            _role == null ? WorkerRoleId.None : content.Def<IWorkerRoleDefinition>(_role).Role,
            _resource == null ? ResourceId.None : content.Resource(_resource));
    }

    public interface IWorkDockDefinition : IComponentDefinition
    {
        IReadOnlyList<WorkDockSlot> Slots { get; }
    }

    /// <summary>One explicit request for ordinary machine work or native resource handling.</summary>
    public readonly struct WorkDockRequest
    {
        public WorkerRoleId Role { get; }
        public ResourceId Resource { get; }
        public ResourceHandlingPhase? Handling { get; }
        public bool IsWork => Role.IsValid;
        public bool IsValid => IsWork ? !Resource.IsValid && !Handling.HasValue :
            Resource.IsValid && (Handling == ResourceHandlingPhase.Loading || Handling == ResourceHandlingPhase.Unloading);
        private WorkDockRequest(WorkerRoleId role, ResourceId resource, ResourceHandlingPhase? handling)
        {
            Role = role; Resource = resource; Handling = handling;
            if (!IsValid) throw new ArgumentException("dock request needs one explicit work role or resource handling phase");
        }
        public static WorkDockRequest Work(WorkerRoleId role) => new WorkDockRequest(role, ResourceId.None, null);
        public static WorkDockRequest Haul(ResourceId resource, ResourceHandlingPhase phase)
            => new WorkDockRequest(WorkerRoleId.None, resource, phase);
    }

    /// <summary>Common content admission, reused by every geometry provider through existing definitions.</summary>
    public static partial class WorkDockRules
    {
        public static void ValidateRequests(IWorkDockDefinition definition, IReadOnlyList<WorkerRoleId> roles, IReadOnlyList<ResourceId> resources)
        {
            bool serves = false;
            foreach (var slot in definition.Slots) serves |= slot.Kind == WorkDockSlotKind.Service || slot.Kind == WorkDockSlotKind.Waiting;
            if (serves && roles.Count + resources.Count == 0)
                throw new ContentException("service docks require explicit work roles or resources");
            bool Role(WorkerRoleId value) { foreach (var role in roles) if (role == value) return true; return false; }
            bool Resource(ResourceId value) { foreach (var resource in resources) if (resource == value) return true; return false; }
            foreach (var slot in definition.Slots)
                if (slot.Kind != WorkDockSlotKind.Parking && (slot.WorkRole.IsValid && !Role(slot.WorkRole) || slot.Resource.IsValid && !Resource(slot.Resource)))
                    throw new ContentException("slot filter must belong to its endpoint's authored role/resource policy");
            void Journey(WorkDockRequest request) {
                bool service = false, exit = false;
                foreach (var slot in definition.Slots) if (slot.Matches(request)) {
                    service |= slot.Kind == WorkDockSlotKind.Service; exit |= slot.Kind == WorkDockSlotKind.Departure;
                }
                if (!service || !exit) throw new ContentException("each authored dock request needs compatible service and departure slots");
            }
            foreach (var role in roles) Journey(WorkDockRequest.Work(role));
            foreach (var resource in resources) Journey(WorkDockRequest.Haul(resource, ResourceHandlingPhase.Loading));
        }
        /// <summary>Admit selected physical geometry before initialization, including unused docks.
        /// Foreign prototypes retain metadata validation without requiring this session's map.</summary>
        public static void ValidateComposition(World world, EntityDef owner)
        {
            if (world == null || owner == null) throw new ArgumentNullException("actual dock composition");
            IWorkDockDefinition definition = owner.Component<IWorkDockDefinition>();
            if (definition == null) throw new ContentException(owner.Id + ": dock requires its actual definition");
            bool Validate()
            {
                ValidateDefinition(owner);
                ValidateRollouts(world.Content, owner);
                ISpatialPresenceDefinition presence = owner.Component<ISpatialPresenceDefinition>();
                if (presence == null || !owner.HasCapability<ISpatialPresence>() || string.IsNullOrWhiteSpace(presence.Space))
                    throw new ContentException(owner.Id + ": physical dock requires its actual spatial presence definition and capability");
                if (presence.Space != world.Settings.Space) return true;
                if (!(world.Space is ISpatialOverlap) || world.Space.Id != presence.Space)
                    throw new ContentException(owner.Id + ": selected physical dock requires an actual matching spatial overlap provider");
                return true;
            }
            EntityObservation.ReadProvider(world, definition, null, () => world.Space == null ? Validate()
                : EntityObservation.ReadProvider(world, world.Space, null, Validate));
        }

        /// <summary>Resource service needs authored compatible access, not an unnamed inventory facade.</summary>
        public static void ValidateResourceService(EntityDef owner, ResolvedContent content, IReadOnlyList<ResourceId> resources)
        {
            if (owner == null || content == null || resources == null) throw new ArgumentNullException("explicit resource service definition");
            foreach (ResourceId resource in resources)
            {
                bool accepted = owner.HasCapability<IInletPort>();
                foreach (ComponentDef component in owner.Components)
                    if (component is IResourcePortDefinition port && port.Accepts(content, resource))
                    { accepted = true; break; }
                if (!accepted) throw new ContentException(owner.Id + ": dock resource service requires compatible authored inlet or named port access");
            }
        }

        public static void ValidateDefinition(EntityDef owner)
        {
            IWorkDockDefinition definition = owner.Component<IWorkDockDefinition>();
            if (definition?.Slots == null || definition.Slots.Count == 0 || !owner.HasCapability<IWorkDock>())
                throw new ContentException(owner.Id + ": dock requires immutable authored slots and matching runtime capability");
            bool service = false, departure = false, waiting = false, rollout = false;
            var unique = new HashSet<Code>();
            foreach (WorkDockSlot slot in definition.Slots)
            {
                if (string.IsNullOrWhiteSpace(slot.Id.Id) || slot.Kind < WorkDockSlotKind.Service || slot.Kind > WorkDockSlotKind.Rollout || !unique.Add(slot.Id) ||
                    slot.Kind == WorkDockSlotKind.Parking && slot.Resource.IsValid)
                    throw new ContentException(owner.Id + ": invalid or duplicate dock slot");
                service |= slot.Kind == WorkDockSlotKind.Service; departure |= slot.Kind == WorkDockSlotKind.Departure;
                waiting |= slot.Kind == WorkDockSlotKind.Waiting;
                rollout |= slot.Kind == WorkDockSlotKind.Rollout;
            }
            bool launch = definition is IWorkDockRolloutDefinition authored && authored.Rollouts?.Count > 0;
            if ((service || waiting || departure && !launch) && (!service || !departure))
                throw new ContentException(owner.Id + ": service dock requires service and departure slots");
            if (rollout && !launch || launch && !departure) throw new ContentException(owner.Id + ": launch dock requires authored rollout lanes and departures");
        }
    }

    /// <summary>
    /// Engine-side geometry capability. Slots are immutable JSON intent; checks are pure. Vacancy
    /// checks actual physical occupants, not jobs. Approach requests existing motion, never teleports.
    /// Providers own no queue, worker claim, stock receipt or handling progress.
    /// </summary>
    public interface IWorkDock : IEntityCapability, IEntityRemovalGuard
    {
        /// <summary>Pure detached geometry of an actual authored slot; never a live coordinate owner.</summary>
        SpatialSnapshot CaptureSlot(World world, Code slot);
        IReadOnlyList<WorkDockSlot> Slots { get; }
        /// <summary>Admission to new loading/unloading/work service. Departure still obeys vacancy, geometry and motion gates, not new service admission.</summary>
        Code CheckRequest(World world, Entity carrier, WorkDockRequest request);
        Code CheckVacancy(World world, Entity carrier, Code slot);
        /// <summary>Pure cost from an explicit prospective origin, or current carrier position when null. Never substitute a foreign origin.</summary>
        bool TryEstimateCost(World world, Entity carrier, Code slot, SpatialSnapshot origin, out long cost);
        /// <summary>Pure actual arrival only, without route search or motion. Agrees with Check returning
        /// WithinReach for the same unchanged participants; cost zero alone never implies arrival.</summary>
        bool IsAtSlot(World world, Entity carrier, Code slot);
        /// <summary>Pure comparison with the carrier's actual active motion destination. Does not
        /// infer intent from proximity or create a slot reservation, route copy or parking binding.</summary>
        bool IsApproachingSlot(World world, Entity carrier, Code slot);
        SpatialInteractionStatus Check(World world, Entity carrier, Code slot);
        /// <summary>May change only actual motion intent and carrier invalidation version. Endpoint, space,
        /// carrier identity/lifecycle/geometry and non-motion component state remain unchanged until Update.</summary>
        SpatialInteractionStatus Approach(World world, Entity carrier, Code slot);
    }

    public static class WorkDockCodes
    {
        public static readonly Code Missing = new Code("dock.missing");
        public static readonly Code Full = new Code("work.dock-full");
        public static readonly Code Waiting = new Code("work.dock-waiting");
        public static readonly Code Occupied = new Code("work.dock-occupied");
        public static readonly Code Incompatible = new Code("work.dock-incompatible");
        public static readonly Code DepartureBlocked = new Code("work.dock-departure-blocked");
        public static readonly Code UnsupportedGeometry = new Code("work.dock-unsupported-geometry");
        public static readonly Code ServiceHold = new Code("motion.dock-service-hold");
        public static bool IsWait(Code code) => code == Full || code == Waiting || code == Occupied || code == DepartureBlocked || code == UnsupportedGeometry || code == ServiceHold;
    }
}
