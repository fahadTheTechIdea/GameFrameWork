using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Public-only engine-side geometry provider. No job scheduler, queue, stock or timer.</summary>
    public interface IPointDockDefinition : IWorkDockDefinition { Fix Offset(Code slot); }
    public sealed class PointDockDef : ComponentDef<PointDock>, IPointDockDefinition, IWorkDockRolloutDefinition
    {
        private readonly Dictionary<Code, Fix> _offsets = new Dictionary<Code, Fix>();
        private readonly List<WorkDockSlotDeclaration> _slots = new List<WorkDockSlotDeclaration>();
        private string[] _resources, _roles;
        private ResolvedContent _content;
        private IReadOnlyList<WorkDockRolloutDeclaration> _rollouts;
        public IReadOnlyList<WorkDockRollout> Rollouts { get; private set; }
        public IReadOnlyList<WorkerRoleId> WorkRoles { get; private set; }
        public string Fault { get; private set; }
        public int FaultAt { get; private set; }
        public IReadOnlyList<WorkDockSlot> Slots { get; private set; }
        public IReadOnlyList<ResourceId> Resources { get; private set; }
        public Fix Offset(Code slot) => _offsets.TryGetValue(slot, out Fix offset) ? offset : throw new ArgumentException("unknown point dock slot");
        protected override void ReadCore(JsonReader reader)
        {
            if (!reader.Has("resources") || !reader.Has("work_roles")) throw reader.Error("dock requires explicit resources and work_roles arrays");
            _resources = reader.Strings("resources"); _roles = reader.Strings("work_roles");
            _rollouts = WorkDockRolloutDeclaration.Read(reader);
            Fault = reader.String("fault", null);
            FaultAt = reader.Int("fault_at", 0);
            if (FaultAt < 0 || Fault == null && FaultAt != 0) throw reader.Error("dock fault activation needs a probe and nonnegative game-time coordinate");
            if (Fault != null && Fault != "request-touch" && Fault != "vacancy-touch" && Fault != "negative-cost" && Fault != "cost-touch" &&
                Fault != "invalid-reach" && Fault != "endpoint-approach-touch" && Fault != "geometry-touch" &&
                Fault != "geometry-null" && Fault != "geometry-space" && Fault != "slots-touch" && Fault != "slots-foreign" &&
                Fault != "approach-invalid" && Fault != "approach-position" && Fault != "losing-cost-touch" &&
                Fault != "zero-cost" && Fault != "refuse-cost" && !CallbackFault(Fault)) throw reader.Error("unknown dock fault probe");
            JsonValue rows = reader.Array("slots");
            if (rows == null) throw reader.Error("dock requires explicit slot geometry");
            for (int i = 0; i < rows.Count; i++)
            {
                var row = new JsonReader(rows[i], "point_dock.slots[" + i + "]");
                var declaration = WorkDockSlotDeclaration.Read(row); Fix offset = row.Fix("offset"); row.End();
                var id = declaration.Id;
                if (_offsets.ContainsKey(id) || _offsets.ContainsValue(offset)) throw row.Error("dock slot identities and positions must be distinct");
                _offsets.Add(id, offset); _slots.Add(declaration);
            }
        }
        private static bool CallbackFault(string fault)
        {
            foreach (string phase in new[] { "request", "vacancy", "cost", "geometry", "arrival", "intent", "reach", "approach" })
                foreach (string action in new[] { "touch", "payload", "carrier-touch", "carrier-payload", "space", "clock", "throw" })
                    if (fault == phase + "-" + action && (phase != "geometry" || !action.StartsWith("carrier-", StringComparison.Ordinal))) return true;
            return false;
        }
        protected override void ResolveCore(ContentResolver content)
        {
            _content = content.Content;
            var resources = new ResourceId[_resources.Length]; var unique = new HashSet<ResourceId>();
            for (int i = 0; i < resources.Length; i++)
            { resources[i] = content.Resource(_resources[i]); if (!unique.Add(resources[i])) throw content.Fail("duplicate dock resource"); }
            Resources = Array.AsReadOnly(resources);
            var roles = new WorkerRoleId[_roles.Length]; var uniqueRoles = new HashSet<WorkerRoleId>();
            for (int i = 0; i < roles.Length; i++)
            { roles[i] = content.Def<IWorkerRoleDefinition>(_roles[i]).Role; if (!uniqueRoles.Add(roles[i])) throw content.Fail("duplicate dock work role"); }
            WorkRoles = Array.AsReadOnly(roles);
            Slots = Array.AsReadOnly(_slots.ConvertAll(slot => slot.Resolve(content)).ToArray());
            var rollouts = new List<WorkDockRollout>(); foreach (var lane in _rollouts) rollouts.Add(lane.Resolve(content)); Rollouts = rollouts.AsReadOnly();
        }
        protected override void ValidateCore(EntityDef owner)
        {
            WorkDockRules.ValidateDefinition(owner);
            WorkDockRules.ValidateRollouts(_content, owner);
            PointPresenceDef presence = owner.Component<PointPresenceDef>();
            if (presence == null) throw new ContentException(owner.Id + ": point dock needs presence");
            WorkDockRules.ValidateResourceService(owner, _content, Resources);
            WorkDockRules.ValidateRequests(this, WorkRoles, Resources);
            foreach (Fix offset in _offsets.Values)
            {
                Fix point;
                try { point = presence.InitialPosition + offset; }
                catch (OverflowException) { throw new ContentException(owner.Id + ": dock position overflows"); }
                if (point < Fix.Zero || point > presence.SpaceDefinition.Extent) throw new ContentException(owner.Id + ": dock position is outside its space");
            }
        }
    }

    public sealed class PointDock : Component<PointDockDef>, IWorkDock
    {
        // Test diagnostics only, excluded from persistence and never read by gameplay decisions.
        public int ReachChecks { get; private set; }
        public int VacancyChecks { get; private set; }
        public int CostChecks { get; private set; }
        public IReadOnlyList<WorkDockSlot> Slots
        {
            get {
                if (Def.Fault == "slots-touch") Entity.Touch();
                return Def.Fault == "slots-foreign" ? new List<WorkDockSlot>(Def.Slots).AsReadOnly() : Def.Slots;
            }
        }
        private bool Active(World world) => world.Clock.StepIndex >= Def.FaultAt;
        // Declared malicious engine-side callbacks only. No driver controls these live objects.
        private void Probe(World world, Entity carrier, string phase)
        {
            if (!Active(world)) return;
            string fault = Def.Fault;
            if (fault == phase + "-touch") Entity.Touch();
            if (fault == phase + "-payload") Entity.Get<SingleResourcePort>().FaultServicePayload();
            if (fault == phase + "-carrier-touch") carrier.Touch();
            if (fault == phase + "-carrier-payload") carrier.Get<IWorker>().Job = checked(carrier.Get<IWorker>().Job + 1);
            if (fault == phase + "-space") { var space = (PointSpace)world.Space; space.SetAccess(!space.AccessOpen); }
            if (fault == phase + "-clock") ((Hosting.GameClock)world.Clock).Pause();
            if (fault == phase + "-throw") throw new InvalidOperationException("dock.probe." + phase + ".throw");
        }
        public SpatialSnapshot CaptureSlot(World world, Code slot)
        {
            if (world == null || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) || Entity.Get<PointPresence>()?.Space != world.Space.Id)
                throw new InvalidOperationException("invalid actual point dock geometry");
            Probe(world, null, "geometry");
            if (Active(world) && Def.Fault == "geometry-null") return null;
            Fix position = Position(slot);
            return new PointSnapshot(Active(world) && Def.Fault == "geometry-space" ? "foreign.space" : world.Space.Id, position, position);
        }
        private bool Live(World world, Entity actor)
            => ReferenceEquals(world.Entities.Get(Entity.Id), Entity) && ReferenceEquals(world.Entities.Get(actor.Id), actor) &&
                world.Space is PointSpace space && Entity.Get<PointPresence>()?.Space == space.Id && actor.Get<PointPresence>()?.Space == space.Id;
        private Fix Position(Code slot) => Entity.Get<PointPresence>().Position + Def.Offset(slot);
        public Code CheckRequest(World world, Entity carrier, WorkDockRequest request)
        {
            Probe(world, carrier, "request");
            if (carrier == null || !Live(world, carrier) || !carrier.Has<PointMotion>() || !request.IsValid) return WorkDockCodes.Incompatible;
            if (request.IsWork)
            { foreach (var role in Def.WorkRoles) if (role == request.Role) return Code.None; }
            else foreach (ResourceId candidate in Def.Resources) if (candidate == request.Resource) return Code.None;
            return WorkDockCodes.Incompatible;
        }
        public Code CheckVacancy(World world, Entity carrier, Code slot)
        {
            VacancyChecks++;
            Probe(world, carrier, "vacancy");
            if (!Live(world, carrier)) return WorkDockCodes.Incompatible;
            Fix place = Position(slot);
            return ((PointSpace)world.Space).CheckMobileVacancy(world, carrier, place, place);
        }
        public bool TryEstimateCost(World world, Entity carrier, Code slot, SpatialSnapshot origin, out long cost)
        {
            CostChecks++;
            Probe(world, carrier, "cost");
            if (Active(world) && Def.Fault == "losing-cost-touch" &&
                (slot == new Code("z.losing-bay") || slot == new Code("a.expensive-exit") || slot == new Code("a.expensive-bay"))) Entity.Touch();
            cost = 0; if (!Live(world, carrier) || origin != null && (origin is not PointSnapshot || origin.Space != world.Space.Id)) return false;
            if (Active(world) && Def.Fault == "zero-cost") return true;
            if (Active(world) && Def.Fault == "refuse-cost") return false;
            Fix from = origin is PointSnapshot point ? point.Position : carrier.Get<PointPresence>().Position;
            cost = Active(world) && Def.Fault == "negative-cost" ? -1 : Fix.Abs(Position(slot) - from).Raw; return true;
        }
        public bool IsAtSlot(World world, Entity carrier, Code slot)
        {
            Probe(world, carrier, "arrival");
            return Live(world, carrier) && carrier.Get<PointPresence>().Position == Position(slot);
        }
        public bool IsApproachingSlot(World world, Entity carrier, Code slot)
        {
            Probe(world, carrier, "intent");
            return Live(world, carrier) && carrier.Get<PointMotion>() is PointMotion motion &&
                motion.Status == MoveStatus.Moving && motion.Goal == Position(slot);
        }
        public SpatialInteractionStatus Check(World world, Entity carrier, Code slot)
        {
            ReachChecks++;
            Probe(world, carrier, "reach");
            if (Active(world) && Def.Fault == "invalid-reach") return (SpatialInteractionStatus)255;
            if (!Live(world, carrier)) return SpatialInteractionStatus.Unsupported;
            Fix target = Position(slot);
            if (target < Fix.Zero || target > Entity.Get<PointPresence>().Def.SpaceDefinition.Extent) return SpatialInteractionStatus.Unreachable;
            if (IsAtSlot(world, carrier, slot)) return SpatialInteractionStatus.WithinReach;
            return ((PointSpace)world.Space).AccessOpen && carrier.Has<PointMotion>() ? SpatialInteractionStatus.Reachable : SpatialInteractionStatus.Unreachable;
        }
        public SpatialInteractionStatus Approach(World world, Entity carrier, Code slot)
        {
            Probe(world, carrier, "approach");
            if (Active(world) && Def.Fault == "endpoint-approach-touch") Entity.Touch();
            if (Active(world) && Def.Fault == "approach-invalid") return (SpatialInteractionStatus)255;
            if (Active(world) && Def.Fault == "approach-position") carrier.Get<PointPresence>().Move(world, carrier.Get<PointPresence>().Position + Fix.One);
            SpatialInteractionStatus status = Check(world, carrier, slot);
            if (status == SpatialInteractionStatus.Reachable) carrier.Get<PointMotion>().GoTo(Position(slot));
            return status;
        }
        public Code CheckRemoval(World world) => world.Services.Get<WorkBoard>().CheckDockRemoval(Entity);
    }
}
