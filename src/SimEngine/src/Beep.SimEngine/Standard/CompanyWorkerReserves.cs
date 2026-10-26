using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public readonly struct WorkerReserveSetting
    {
        public WorkerRoleId Role { get; }
        public int MinimumReady { get; }
        public WorkerReserveSetting(WorkerRoleId role, int minimumReady)
        {
            if (!role.IsValid || minimumReady < 0) throw new ArgumentException("explicit reserve role and nonnegative count required");
            Role = role; MinimumReady = minimumReady;
        }
    }
    public readonly struct CompanyWorkerReserveSnapshot
    {
        public PlayerId Company { get; }
        public WorkerRoleId Role { get; }
        public int MinimumReady { get; }
        public int ReadyWorkers { get; }
        public int CoveredWorkers { get; }
        public int Shortfall => MinimumReady - CoveredWorkers;
        /// <summary>Raw compatible-role headroom only, never simultaneous coverage or assignment eligibility.</summary>
        public int Headroom => Math.Max(0, ReadyWorkers - MinimumReady);
        public CompanyWorkerReserveSnapshot(PlayerId company, WorkerRoleId role, int minimum, int ready, int covered)
        {
            if (!company.IsValid || !role.IsValid || minimum < 0 || ready < 0 || covered < 0 || covered > minimum || covered > ready)
                throw new ArgumentException("explicit company/role and valid compatible/distinct readiness facts required");
            Company = company; Role = role; MinimumReady = minimum; ReadyWorkers = ready; CoveredWorkers = covered;
        }
    }
    /// <summary>Illustrative distinct coverage, not an acquired worker or reservation.</summary>
    public readonly struct WorkerReserveAssignment
    {
        public EntityId Worker { get; }
        public WorkerRoleId Role { get; }
        public WorkerReserveAssignment(EntityId worker, WorkerRoleId role)
        {
            if (!worker.IsValid || !role.IsValid) throw new ArgumentException("coverage needs a real worker identity and role");
            Worker = worker; Role = role;
        }
    }
    public sealed class CompanyWorkerReserveCoverage
    {
        public PlayerId Company { get; }
        public IReadOnlyList<CompanyWorkerReserveSnapshot> Roles { get; }
        public IReadOnlyList<WorkerReserveAssignment> Assignments { get; }
        public long RequiredWorkers { get; }
        public int CoveredWorkers => Assignments.Count;
        public long Shortfall => RequiredWorkers - CoveredWorkers;
        public bool Complete => Shortfall == 0;
        public CompanyWorkerReserveCoverage(PlayerId company, IEnumerable<CompanyWorkerReserveSnapshot> roles, IEnumerable<WorkerReserveAssignment> assignments)
        {
            if (!company.IsValid || roles == null || assignments == null) throw new ArgumentException("explicit company and detached coverage facts required");
            var rows = new List<CompanyWorkerReserveSnapshot>(roles); var matches = new List<WorkerReserveAssignment>(assignments);
            rows.Sort((a, b) => string.CompareOrdinal(a.Role.Value, b.Role.Value)); matches.Sort((a, b) => a.Worker.Value.CompareTo(b.Worker.Value));
            var byRole = new Dictionary<WorkerRoleId, int>(); long required = 0;
            foreach (var row in rows)
            {
                if (row.Company != company || !row.Role.IsValid || byRole.ContainsKey(row.Role)) throw new ArgumentException("coverage roles must be unique and belong to the same company");
                byRole.Add(row.Role, 0); required = checked(required + row.MinimumReady);
            }
            EntityId previous = EntityId.None;
            foreach (var match in matches)
            {
                if (!match.Worker.IsValid || !match.Role.IsValid || match.Worker == previous || !byRole.ContainsKey(match.Role))
                    throw new ArgumentException("coverage requires distinct workers and declared roles");
                previous = match.Worker; byRole[match.Role] = checked(byRole[match.Role] + 1);
            }
            foreach (var row in rows) if (row.CoveredWorkers != byRole[row.Role]) throw new ArgumentException("role coverage must equal its distinct assignments");
            Company = company; RequiredWorkers = required; Roles = rows.AsReadOnly(); Assignments = matches.AsReadOnly();
        }
    }
    public static class CompanyWorkerReserveCodes
    {
        public static readonly Code MinimumReady = new Code("work.company-minimum-ready");
    }
    public interface ICompanyWorkerReservePolicy
    {
        World World { get; }
        Code ReadReserve(PlayerId company, WorkerRoleId role, out CompanyWorkerReserveSnapshot value);
        Code ReadCoverage(PlayerId company, out CompanyWorkerReserveCoverage value);
        /// <summary>Fresh ordinary acquisition only; WorkSystem retains incumbent/emergency authority.</summary>
        Code CheckOrdinaryAssignment(Entity worker);
    }
    /// <summary>Stateless actual readiness; no extra worker membership, claim, route or resource promise.</summary>
    public static class CompanyWorkerReserveRules
    {
        public static IReadOnlyList<WorkerRoleId> ReadyRoles(World world, Entity entity)
        {
            if (entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity)) throw new ArgumentException("reserve needs actual live worker");
            var actor = entity.Get<IWorker>(); if (actor == null) return Array.Empty<WorkerRoleId>();
            return EntityObservation.Read(world, entity, () =>
            {
                if (actor.Job != 0 || actor.Held) return (IReadOnlyList<WorkerRoleId>)Array.Empty<WorkerRoleId>();
                var execution = WorkerExecutionContracts.Read(world, entity);
                var dispatch = world.Services.Get<IWorkerDispatch>();
                var roles = new List<WorkerRoleId>();
                foreach (WorkerRoleId role in execution.Roles)
                    if (dispatch.CheckIdleWorker(world, entity, entity.Owner, role, true).IsNone) roles.Add(role);
                return roles.AsReadOnly();
            });
        }
        private static bool Allows(IReadOnlyList<WorkerRoleId> roles, WorkerRoleId role)
        { foreach (var value in roles) if (value == role) return true; return false; }
        internal static CompanyWorkerReserveCoverage Capture(World world, PlayerId company, IReadOnlyList<WorkerReserveSetting> settings, EntityId excluded)
        {
            // A transient matching over current facts, never another worker roster or claim owner.
            var workers = new List<(EntityId Id, IReadOnlyList<WorkerRoleId> Roles)>();
            foreach (var entity in world.Entities.All)
                if (entity.Alive && entity.Owner == company && entity.Id != excluded && entity.Has<IWorker>())
                {
                    var roles = ReadyRoles(world, entity); if (roles.Count != 0) workers.Add((entity.Id, roles));
                }
            workers.Sort((a, b) => a.Id.Value.CompareTo(b.Id.Value));
            var floors = new List<WorkerReserveSetting>(settings); floors.Sort((a, b) => string.CompareOrdinal(a.Role.Value, b.Role.Value));
            var capacities = new List<int>(); var compatible = new List<IReadOnlyList<EntityId>>();
            foreach (var floor in floors)
            {
                capacities.Add(floor.MinimumReady); var ids = new List<EntityId>();
                foreach (var worker in workers) if (Allows(worker.Roles, floor.Role)) ids.Add(worker.Id);
                compatible.Add(ids.AsReadOnly());
            }
            var allocations = DistinctWorkerMatching.Solve(capacities, compatible);
            var matches = new List<WorkerReserveAssignment>(); var values = new List<CompanyWorkerReserveSnapshot>();
            var counts = new int[floors.Count];
            foreach (var match in allocations) { counts[match.Demand]++; matches.Add(new WorkerReserveAssignment(match.Worker, floors[match.Demand].Role)); }
            for (int i = 0; i < floors.Count; i++)
                values.Add(new CompanyWorkerReserveSnapshot(company, floors[i].Role, floors[i].MinimumReady, compatible[i].Count, counts[i]));
            return new CompanyWorkerReserveCoverage(company, values, matches);
        }
    }
    internal sealed partial class CompanyAlertPolicies
    {
        public Code ReadReserve(PlayerId company, WorkerRoleId role, out CompanyWorkerReserveSnapshot value)
        {
            value = default;
            if (!ValidCompany(company)) return CompanyAlertCodes.NotCompany;
            if (!role.IsValid || World.Content.Find<IWorkerRoleDefinition>(role.Value)?.Role != role) return Codes.InvalidArgument;
            var floors = new List<WorkerReserveSetting>(ReadAlert(company).Settings.WorkerReserves); bool configured = false;
            foreach (var floor in floors) if (floor.Role == role) { configured = true; break; }
            // An absent role is explicitly unprotected, as before; this temporary zero row is not policy state.
            if (!configured) floors.Add(new WorkerReserveSetting(role, 0));
            foreach (var row in CompanyWorkerReserveRules.Capture(World, company, floors, EntityId.None).Roles)
                if (row.Role == role) { value = row; return Code.None; }
            throw new InvalidOperationException("reserve query lost its requested role");
        }
        public Code ReadCoverage(PlayerId company, out CompanyWorkerReserveCoverage value)
        {
            value = null; if (!ValidCompany(company)) return CompanyAlertCodes.NotCompany;
            value = CompanyWorkerReserveRules.Capture(World, company, ReadAlert(company).Settings.WorkerReserves, EntityId.None); return Code.None;
        }
        public Code CheckOrdinaryAssignment(Entity worker)
        {
            if (worker == null || !worker.Alive || !ReferenceEquals(World.Entities.Get(worker.Id), worker)) return Codes.NotFound;
            if (!ValidCompany(worker.Owner)) return CompanyAlertCodes.NotCompany;
            var floors = ReadAlert(worker.Owner).Settings.WorkerReserves;
            var before = CompanyWorkerReserveRules.Capture(World, worker.Owner, floors, EntityId.None);
            var after = CompanyWorkerReserveRules.Capture(World, worker.Owner, floors, worker.Id);
            if (after.CoveredWorkers < before.CoveredWorkers) return CompanyWorkerReserveCodes.MinimumReady;
            for (int i = 0; i < before.Roles.Count; i++)
                if (after.Roles[i].ReadyWorkers < before.Roles[i].ReadyWorkers && after.Roles[i].ReadyWorkers < before.Roles[i].MinimumReady)
                    return CompanyWorkerReserveCodes.MinimumReady;
            return Code.None;
        }
    }
}
