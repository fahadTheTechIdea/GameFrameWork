using System;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum WorkLoadScope : byte { Company = 1, Endpoint = 2, Worker = 3 }

    /// <summary>Detached distinct machine counts. Eligibility is current and target-independent, not an acquired team.</summary>
    public readonly struct WorkerCapacitySnapshot
    {
        public PlayerId Company { get; }
        public WorkerRoleId Role { get; }
        public EntityId Subject { get; }
        public int TotalWorkers { get; }
        public int BusyWorkers { get; }
        public int HeldWorkers { get; }
        /// <summary>Unclaimed and unheld, not necessarily stationary or eligible for a particular task.</summary>
        public int IdleWorkers => TotalWorkers - BusyWorkers - HeldWorkers;
        public int OrdinaryReady { get; }
        public int EmergencyReady { get; }
        internal WorkerCapacitySnapshot(PlayerId company, WorkerRoleId role, EntityId subject, int total, int busy, int held, int ordinary, int emergency)
        { Company = company; Role = role; Subject = subject; TotalWorkers = total; BusyWorkers = busy; HeldWorkers = held; OrdinaryReady = ordinary; EmergencyReady = emergency; }
    }

    public readonly struct WorkLoadSnapshot
    {
        public PlayerId Company { get; }
        public WorkLoadScope Scope { get; }
        public EntityId Subject { get; }
        public int LiveJobs { get; }
        public int ActivityJobs { get; }
        public int HaulJobs { get; }
        public int OpenJobs { get; }
        public int AssignedJobs { get; }
        public int PausedJobs { get; }
        public int DockWaiting { get; }
        public int DockArrivedWaiting { get; }
        internal WorkLoadSnapshot(PlayerId company, WorkLoadScope scope, EntityId subject,
            int activity, int haul, int open, int assigned, int paused, int waiting, int arrived)
        {
            Company = company; Scope = scope; Subject = subject;
            ActivityJobs = activity; HaulJobs = haul; LiveJobs = checked(activity + haul);
            OpenJobs = open; AssignedJobs = assigned; PausedJobs = paused;
            DockWaiting = waiting; DockArrivedWaiting = arrived;
        }
    }

    /// <summary>Pure counts of the existing board, never another job registry or queue.</summary>
    public interface IWorkLoad
    {
        Code ReadWorkLoad(PlayerId company, WorkLoadScope scope, EntityId subject, out WorkLoadSnapshot value);
        Code ReadWorkerCapacity(PlayerId company, WorkerRoleId role, EntityId subject, out WorkerCapacitySnapshot value);
    }

    public sealed partial class WorkBoard : IWorkLoad
    {
        public Code ReadWorkerCapacity(PlayerId company, WorkerRoleId role, EntityId subject, out WorkerCapacitySnapshot value)
        {
            value = default;
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            if (role != WorkerRoleId.None && _world.Content.Find<IWorkerRoleDefinition>(role.Value)?.Role != role) return Codes.InvalidArgument;
            if (subject != EntityId.None)
            {
                if (!subject.IsValid) return Codes.InvalidArgument;
                Entity actual = _world.Entities.Get(subject);
                if (actual == null || !actual.Alive) return Codes.NotFound;
                if (actual.Owner != company) return Codes.NotOwned;
                if (!actual.Has<IWorker>()) return StandardCodes.NotWorker;
            }
            var dispatch = _world.Services.Get<IWorkerDispatch>();
            int total = 0, busy = 0, held = 0, ordinary = 0, emergency = 0;
            foreach (Entity entity in _world.Entities.All)
            {
                if (!entity.Alive || entity.Owner != company || !entity.Has<IWorker>() || subject != EntityId.None && entity.Id != subject) continue;
                EntityObservation.Read(_world, entity, () =>
                {
                    var actor = entity.Get<IWorker>(); var execution = WorkerExecutionContracts.Read(_world, entity);
                    bool matches = role == WorkerRoleId.None;
                    foreach (var declared in execution.Roles) if (declared == role) matches = true;
                    if (!matches) return false;
                    total = checked(total + 1);
                    if (actor.Job != 0) { busy = checked(busy + 1); return true; }
                    if (actor.Held) { held = checked(held + 1); return true; }
                    bool readyOrdinary = false, readyEmergency = false;
                    foreach (var declared in execution.Roles)
                    {
                        if (role != WorkerRoleId.None && role != declared) continue;
                        readyOrdinary |= dispatch.CheckIdleWorker(_world, entity, company, declared, false).IsNone;
                        readyEmergency |= dispatch.CheckIdleWorker(_world, entity, company, declared, true).IsNone;
                    }
                    if (readyOrdinary) ordinary = checked(ordinary + 1);
                    if (readyEmergency) emergency = checked(emergency + 1);
                    return true;
                });
                if (!ReferenceEquals(_world.Services.Get<IWorkerDispatch>(), dispatch)) throw new InvalidOperationException("worker capacity lost its dispatch authority");
            }
            value = new WorkerCapacitySnapshot(company, role, subject, total, busy, held, ordinary, emergency);
            return Code.None;
        }
        public Code ReadWorkLoad(PlayerId company, WorkLoadScope scope, EntityId subject, out WorkLoadSnapshot value)
        {
            value = default;
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            if (scope < WorkLoadScope.Company || scope > WorkLoadScope.Worker ||
                scope == WorkLoadScope.Company && subject != EntityId.None ||
                scope != WorkLoadScope.Company && !subject.IsValid) return Codes.InvalidArgument;
            if (scope != WorkLoadScope.Company)
            {
                Entity entity = _world.Entities.Get(subject);
                if (entity == null || !entity.Alive) return Codes.NotFound;
                if (entity.Owner != company) return Codes.NotOwned;
                if (scope == WorkLoadScope.Worker && entity.Get<IWorker>() == null) return StandardCodes.NotWorker;
            }
            int activity = 0, haul = 0, open = 0, assigned = 0, paused = 0, waiting = 0, arrived = 0;
            foreach (Job job in _jobs)
            {
                if (!job.IsLive || job.Owner != company ||
                    scope == WorkLoadScope.Worker && job.Worker != subject ||
                    scope == WorkLoadScope.Endpoint && (job.IsHaul ? job.From != subject && job.To != subject : job.Target != subject)) continue;
                if (job.IsHaul) haul++; else activity++;
                if (job.State == JobState.Open) open++;
                else if (job.State == JobState.Assigned) assigned++;
                else if (job.State == JobState.Paused) paused++;
                EntityId dockEndpoint = CurrentDockEndpoint(job);
                if (scope != WorkLoadScope.Endpoint || dockEndpoint == subject)
                {
                    Code refusal = ReadDockWaiting(job, out bool isWaiting, out bool isArrived);
                    if (!refusal.IsNone) return refusal;
                    if (isWaiting) waiting++; if (isArrived) arrived++;
                }
            }
            value = new WorkLoadSnapshot(company, scope, subject, activity, haul, open, assigned, paused, waiting, arrived);
            return Code.None;
        }
    }
}
