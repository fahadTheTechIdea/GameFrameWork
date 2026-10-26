using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Stable identities of native physical algorithms, not a closed phase enumeration.</summary>
    public static class WorkPhases
    {
        public static readonly Code Travel = new Code("sim.work.travel");
        public static readonly Code Productive = new Code("sim.work.productive");
        public static readonly Code Pickup = new Code("sim.work.pickup");
        public static readonly Code Delivery = new Code("sim.work.delivery");
        public static readonly Code Departure = new Code("sim.work.departure");
    }

    /// <summary>A module-declared physical job phase. State belongs to existing declared owners.</summary>
    public interface IWorkPhaseExecutor
    {
        Code Id { get; }
        WorkPhasePurpose Purpose { get; }
        void Execute(WorkPhaseContext context);
    }

    /// <summary>Derived immutable composition facts; aliases the actual WorkSystem, not another owner.</summary>
    public interface IWorkPhaseCatalog
    {
        IReadOnlyList<Code> Ids { get; }
        bool Contains(Code id);
        WorkPhasePurpose Purpose(Code id);
    }

    public enum WorkPhasePurpose : byte { Other = 0, Travel = 1, Productive = 2, Pickup = 3, Delivery = 4, Departure = 5 }

    /// <summary>Engine-issued single-operation invocation, valid only during this actual phase call.</summary>
    public sealed class WorkPhaseContext
    {
        private readonly WorkSystem _system;
        private readonly int _index;
        private bool _active = true, _used;
        internal WorkPhaseContext(WorkSystem system, World world, Job job, Entity worker, Entity target, bool transferWait)
        {
            _system = system; World = world; Job = job; Worker = worker; Target = target;
            TransferWait = transferWait; _index = job.ToilIndex; Phase = job.Toils[_index];
        }
        public World World { get; }
        public Job Job { get; }
        public Entity Worker { get; }
        public Entity Target { get; }
        public Toil Phase { get; }
        public bool TransferWait { get; }
        internal void Expire() => _active = false;
        private void Invoke(Action operation)
        {
            if (!_active || _used || !ReferenceEquals(World.Services.Get<WorkBoard>().Job(Job.Id), Job) ||
                !ReferenceEquals(World.Entities.Get(Worker.Id), Worker) || !ReferenceEquals(World.Entities.Get(Target.Id), Target) ||
                Job.Worker != Worker.Id || Job.ToilIndex != _index || _index >= Job.Toils.Count ||
                Job.State != JobState.Assigned && Job.State != JobState.Paused ||
                !Job.Toils[_index].Equals(Phase))
                throw new InvalidOperationException("expired, reused or changed physical work phase invocation");
            _used = true; operation();
        }
        public void Approach() => Invoke(() => _system.ExecuteTravel(this));
        public void AdvanceProductiveWork() => Invoke(() => _system.ExecuteProductive(this));
        public void PickUp() => Invoke(() => _system.ExecutePickup(this));
        public void Deliver() => Invoke(() => _system.ExecuteDelivery(this));
        public void Depart() => Invoke(() => _system.ExecuteDeparture(this));
        public void Pause(Code blocker) => Invoke(() => _system.ExecutePhasePause(this, blocker));
    }

    internal sealed class TravelWorkPhaseExecutor : IWorkPhaseExecutor
    { public Code Id => WorkPhases.Travel; public WorkPhasePurpose Purpose => WorkPhasePurpose.Travel; public void Execute(WorkPhaseContext context) => context.Approach(); }
    internal sealed class ProductiveWorkPhaseExecutor : IWorkPhaseExecutor
    { public Code Id => WorkPhases.Productive; public WorkPhasePurpose Purpose => WorkPhasePurpose.Productive; public void Execute(WorkPhaseContext context) => context.AdvanceProductiveWork(); }
    internal sealed class PickupWorkPhaseExecutor : IWorkPhaseExecutor
    { public Code Id => WorkPhases.Pickup; public WorkPhasePurpose Purpose => WorkPhasePurpose.Pickup; public void Execute(WorkPhaseContext context) => context.PickUp(); }
    internal sealed class DeliveryWorkPhaseExecutor : IWorkPhaseExecutor
    { public Code Id => WorkPhases.Delivery; public WorkPhasePurpose Purpose => WorkPhasePurpose.Delivery; public void Execute(WorkPhaseContext context) => context.Deliver(); }
    internal sealed class DepartureWorkPhaseExecutor : IWorkPhaseExecutor
    { public Code Id => WorkPhases.Departure; public WorkPhasePurpose Purpose => WorkPhasePurpose.Departure; public void Execute(WorkPhaseContext context) => context.Depart(); }

    public sealed partial class WorkSystem : IWorkPhaseCatalog
    {
        private readonly Dictionary<Code, (IWorkPhaseExecutor Provider, WorkPhasePurpose Purpose)> _phaseExecutors = new Dictionary<Code, (IWorkPhaseExecutor, WorkPhasePurpose)>();
        private IReadOnlyList<Code> _phaseIds = Array.AsReadOnly(Array.Empty<Code>());
        public IReadOnlyList<Code> Ids => _phaseIds;
        public bool Contains(Code id)
        {
            if (!_phaseExecutors.TryGetValue(id, out var executor)) return false;
            if (executor.Provider.Id != id || executor.Provider.Purpose != executor.Purpose) throw new InvalidOperationException("physical phase provider changed its declared metadata");
            return true;
        }
        public WorkPhasePurpose Purpose(Code id)
        {
            if (!Contains(id)) throw new InvalidOperationException("unknown physical phase purpose '" + id + "'");
            return _phaseExecutors[id].Purpose;
        }
        private void ValidateWorkPhases(World world)
        {
            _phaseExecutors.Clear(); var ids = new List<Code>();
            foreach (IWorkPhaseExecutor executor in world.Services.Providers<IWorkPhaseExecutor>())
            {
                Code id = executor.Id;
                try { ContractIdentity.ValidateId(id.Id, "physical work phase executor"); }
                catch (ArgumentException error) { throw new ContentException(error.Message); }
                if (_phaseExecutors.ContainsKey(id)) throw new ContentException("duplicate physical work phase executor '" + id + "'");
                WorkPhasePurpose purpose = executor.Purpose;
                if (purpose < WorkPhasePurpose.Other || purpose > WorkPhasePurpose.Departure) throw new ContentException("invalid physical phase purpose");
                _phaseExecutors.Add(id, (executor, purpose)); ids.Add(id);
            }
            foreach (Code native in new[] { WorkPhases.Travel, WorkPhases.Productive, WorkPhases.Pickup, WorkPhases.Delivery, WorkPhases.Departure })
                if (!Contains(native)) throw new ContentException("missing native physical phase executor '" + native + "'");
            ids.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); _phaseIds = ids.AsReadOnly();
        }
        private void ExecutePhase(World world, Job job, Entity worker, Entity target, bool transferWait)
        {
            Code id = job.Toils[job.ToilIndex].Executor;
            if (!Contains(id)) throw new InvalidOperationException("unknown physical work phase executor '" + id + "'");
            var context = new WorkPhaseContext(this, world, job, worker, target, transferWait);
            try { _phaseExecutors[id].Provider.Execute(context); }
            finally { context.Expire(); }
        }
        internal void ExecutePhasePause(WorkPhaseContext context, Code blocker)
        {
            if (blocker.IsNone) throw new InvalidOperationException("phase pause requires an explicit blocker");
            PausePolicy(context.World, context.Worker, context.Job, blocker);
        }
    }
}
