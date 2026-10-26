using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable authored node execution strategy; actual state stays with WorkBoard/jobs.</summary>
    public interface IActivityExecutor : IContentDefinition
    {
        void ValidateDefinition(IActivityNodeDefinition definition);
        void ValidateComposition(World world);
        IReadOnlyList<Toil> CreateItinerary(ActivityExecutorContext context);
        bool MatchesItinerary(ActivityExecutorContext context, IReadOnlyList<Toil> itinerary);
        Code CheckWorker(WorkRequirementContext context);
        ActivityApproachResult Reapproach(ActivityExecutorContext context, int current);
        Fix ReadRate(ActivityExecutorContext context);
    }

    /// <summary>A pure executor decision; it cannot move a worker or change earned progress.</summary>
    public readonly struct ActivityApproachResult
    {
        private readonly byte _kind;
        public int Index { get; }
        public Code Blocker { get; }
        public bool IsWait => _kind == 2;
        public bool IsValid => _kind == 1 && Index >= 0 && Blocker.IsNone || _kind == 2 && Index == -1 && !Blocker.IsNone;
        private ActivityApproachResult(byte kind, int index, Code blocker) { _kind = kind; Index = index; Blocker = blocker; }
        public static ActivityApproachResult ResumeAt(int index)
        { if (index < 0) throw new ArgumentOutOfRangeException(nameof(index)); return new ActivityApproachResult(1, index, Code.None); }
        public static ActivityApproachResult Wait(Code blocker)
        { if (blocker.IsNone) throw new ArgumentException("physical access wait requires a blocker", nameof(blocker)); return new ActivityApproachResult(2, -1, blocker); }
    }

    public readonly struct ActivityExecutorContext
    {
        internal ActivityExecutorContext(World world, Order order, IActivityNodeDefinition definition, Entity worker)
        { World = world; Order = order; Definition = definition; Worker = worker; }
        public World World { get; }
        public Order Order { get; }
        public IActivityNodeDefinition Definition { get; }
        public ActivityNodeExecution Node => Order.Node(Definition.Id);
        public Entity Target => World.Entities.Get(Order.Target);
        public Entity Worker { get; }
    }

    public static class ActivityExecutorContracts
    {
        internal static Code ReadCode(JsonReader reader, string key)
        {
            string id = reader.String(key);
            try { ContractIdentity.ValidateId(id, "activity executor " + key); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            return new Code(id);
        }
        private static void RequireActual(World world, IActivityExecutor executor)
        {
            if (!(executor is Def actual) || !actual.IsFrozen || executor is IPersistentService ||
                string.IsNullOrWhiteSpace(actual.Id) || string.IsNullOrWhiteSpace(actual.Module) ||
                executor.Id != actual.Id || executor.Module != actual.Module || !world.Content.Owns(executor) ||
                !ReferenceEquals(world.Content.Find<IActivityExecutor>(actual.Id), executor))
                throw new ContentException("activity executor requires its actual immutable catalog alias");
        }
        internal static T Read<T>(World world, IActivityExecutor executor, Entity target, Entity worker, Func<T> read)
        {
            var targetCodec = EntityObservation.CaptureProvider(world, target,
                target == null ? (Action<IStateSync>)null : target.Sync, "work.executor-read");
            var workerFacts = worker == null ? EntityObservation.Capture(world) : EntityObservation.Capture(world, worker);
            var workerCodec = EntityObservation.CaptureProvider(world, worker,
                worker == null || ReferenceEquals(target, worker) ? (Action<IStateSync>)null : worker.Sync, "work.executor-read");
            // ReadProvider already latches a thrown callback. Preserve its original typed error;
            // no facts are returned from that path and a faulted session cannot checkpoint.
            T value = EntityObservation.ReadProvider(world, executor, target, () =>
                { RequireActual(world, executor); return read(); }, world.Space);
            try { workerFacts.Verify(); workerCodec.Verify(); targetCodec.Verify(); }
            catch (Exception error) { world.LatchFault("work.executor-read", error); throw; }
            return value;
        }
        /// <summary>The captured authored role alternatives solely determine machine participation.</summary>
        public static bool RequiresWorker(IActivityNodeDefinition definition)
        {
            if (definition?.Roles == null) throw new ContentException("activity node requires explicit role metadata");
            return definition.Roles.Count > 0;
        }
        public static void ValidateRoles(IActivityNodeDefinition definition, bool requiresWorker)
        {
            if (RequiresWorker(definition) != requiresWorker)
                throw new ContentException("authored executor and node worker roles disagree");
        }
    }
    /// <summary>Authored physical approach/work/optional departure; phases may be supplied by modules.</summary>
    public sealed class StaffedActivityExecution
    {
        public Code Travel { get; private set; }
        public Code Work { get; private set; }
        public Code Departure { get; private set; }
        private StaffedActivityExecution(JsonReader reader)
        {
            Travel = ActivityExecutorContracts.ReadCode(reader, "travel"); Work = ActivityExecutorContracts.ReadCode(reader, "work"); Departure = ActivityExecutorContracts.ReadCode(reader, "departure");
            if (Travel == Work || Travel == Departure || Work == Departure) throw reader.Error("staffed phases must be distinct");
        }
        public static StaffedActivityExecution Read(JsonReader reader)
            => new StaffedActivityExecution(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void ValidateDefinition(IActivityNodeDefinition definition)
            => ActivityExecutorContracts.ValidateRoles(definition, true);
        public void ValidateComposition(World world)
        {
            var phases = world.Services.Get<IWorkPhaseCatalog>();
            if (!phases.Contains(Travel) || !phases.Contains(Work) || !phases.Contains(Departure) ||
                phases.Purpose(Travel) != WorkPhasePurpose.Travel || phases.Purpose(Work) != WorkPhasePurpose.Productive ||
                phases.Purpose(Departure) != WorkPhasePurpose.Departure)
                throw new ContentException("staffed executor requires composed travel/productive/departure phase contracts");
        }
        public IReadOnlyList<Toil> CreateItinerary(ActivityExecutorContext context)
        {
            Entity target = context.Target ?? throw new InvalidOperationException("activity itinerary target is absent");
            var phases = new List<Toil> { new Toil { Executor = Travel, At = target.Id }, new Toil { Executor = Work, At = target.Id } };
            if (target.Get<IWorkDock>() != null) phases.Add(new Toil { Executor = Departure, At = target.Id });
            return phases.AsReadOnly();
        }
        public bool MatchesItinerary(ActivityExecutorContext context, IReadOnlyList<Toil> itinerary)
        {
            int count = context.Target == null ? itinerary.Count : context.Target.Get<IWorkDock>() == null ? 2 : 3;
            if (count != 2 && count != 3 || itinerary.Count != count) return false;
            for (int i = 0; i < count; i++)
                if (itinerary[i].At != context.Order.Target || itinerary[i].Executor != (i == 0 ? Travel : i == 1 ? Work : Departure) ||
                    itinerary[i].Resource.IsValid || !itinerary[i].Amount.IsZero) return false;
            return true;
        }
        public Code CheckWorker(WorkRequirementContext context) => Code.None;
        public ActivityApproachResult Reapproach(ActivityExecutorContext context, int current) => ActivityApproachResult.ResumeAt(0);
        public Fix ReadRate(ActivityExecutorContext context)
            => context.World.Services.Get<IWorkerDispatch>().EffectiveWorkRate(context.World, context.Worker);
    }

    public sealed class StaffedActivityExecutorDef : Def, IActivityExecutor
    {
        private StaffedActivityExecution _execution;
        public Code Travel => _execution.Travel;
        public Code Work => _execution.Work;
        public Code Departure => _execution.Departure;
        protected override void ReadCore(JsonReader reader) => _execution = StaffedActivityExecution.Read(reader);
        public void ValidateDefinition(IActivityNodeDefinition definition) => _execution.ValidateDefinition(definition);
        public void ValidateComposition(World world) => _execution.ValidateComposition(world);
        public IReadOnlyList<Toil> CreateItinerary(ActivityExecutorContext context) => _execution.CreateItinerary(context);
        public bool MatchesItinerary(ActivityExecutorContext context, IReadOnlyList<Toil> itinerary) => _execution.MatchesItinerary(context,itinerary);
        public Code CheckWorker(WorkRequirementContext context) => _execution.CheckWorker(context);
        public ActivityApproachResult Reapproach(ActivityExecutorContext context, int current) => _execution.Reapproach(context,current);
        public Fix ReadRate(ActivityExecutorContext context) => _execution.ReadRate(context);
    }

    /// <summary>Normal fixed-work JSON registration; no required native executor base.</summary>
    public sealed class FixedActivityExecutorDef : Def, IActivityExecutor
    {
        private FixedActivityExecution _execution;
        public Code Work => _execution.Work;
        public Code AccessBlocker => _execution.AccessBlocker;
        protected override void ReadCore(JsonReader reader) => _execution = FixedActivityExecution.Read(reader);
        public void ValidateDefinition(IActivityNodeDefinition definition) => _execution.ValidateDefinition(definition);
        public void ValidateComposition(World world) => _execution.ValidateComposition(world);
        public Code CheckWorker(WorkRequirementContext context) => _execution.CheckWorker(context);
        public IReadOnlyList<Toil> CreateItinerary(ActivityExecutorContext context) => _execution.CreateItinerary(context);
        public bool MatchesItinerary(ActivityExecutorContext context, IReadOnlyList<Toil> itinerary) => _execution.MatchesItinerary(context, itinerary);
        public ActivityApproachResult Reapproach(ActivityExecutorContext context, int current) => _execution.Reapproach(context, current);
        public Fix ReadRate(ActivityExecutorContext context) => _execution.ReadRate(context);
    }

    public sealed class TimedActivityExecutorDef : Def, IActivityExecutor
    {
        private TimedActivityExecution _execution;
        public Fix Rate => _execution.Rate;
        protected override void ReadCore(JsonReader reader) => _execution = TimedActivityExecution.Read(reader);
        public void ValidateDefinition(IActivityNodeDefinition definition) => _execution.ValidateDefinition(definition);
        public void ValidateComposition(World world) => _execution.ValidateComposition(world);
        public IReadOnlyList<Toil> CreateItinerary(ActivityExecutorContext context) => _execution.CreateItinerary(context);
        public bool MatchesItinerary(ActivityExecutorContext context, IReadOnlyList<Toil> itinerary) => _execution.MatchesItinerary(context, itinerary);
        public Code CheckWorker(WorkRequirementContext context) => _execution.CheckWorker(context);
        public ActivityApproachResult Reapproach(ActivityExecutorContext context, int current) => _execution.Reapproach(context, current);
        public Fix ReadRate(ActivityExecutorContext context) => _execution.ReadRate(context);
    }

    public sealed partial class WorkBoard
    {
        internal ActivityExecutorContext ExecutorContext(Order order, IActivityNodeDefinition definition, Entity worker)
            => new ActivityExecutorContext(_world, order, definition, worker);
        internal Code CheckExecutorWorker(WorkRequirementContext context)
            => ActivityExecutorContracts.Read(_world, context.Definition.Executor, context.Target, context.Worker, () => context.Definition.Executor.CheckWorker(context));
        internal Code ReapproachActivity(Job job)
        {
            if (job.IsHaul) { job.ToilIndex--; return Code.None; }
            Order order = Order(job.OrderId); IActivityNodeDefinition definition = order?.Plan.Graph.Find(job.NodeId);
            if (definition == null) throw new InvalidOperationException("activity re-approach lost its authored executor");
            var context = ExecutorContext(order, definition, _world.Entities.Get(job.Worker));
            ActivityApproachResult approach = ActivityExecutorContracts.Read(_world, definition.Executor, context.Target, context.Worker,
                () => definition.Executor.Reapproach(context, job.ToilIndex));
            if (!approach.IsValid) throw new InvalidOperationException("activity executor returned an invalid physical approach result");
            if (approach.IsWait) return approach.Blocker;
            int index = approach.Index;
            if (index < 0 || index >= job.Toils.Count || _world.Services.Get<IWorkPhaseCatalog>().Purpose(job.Toils[index].Executor) != WorkPhasePurpose.Travel)
                throw new InvalidOperationException("activity executor returned an invalid physical approach phase");
            job.ToilIndex = index;
            return Code.None;
        }
        internal Fix ExecutorRate(Order order, IActivityNodeDefinition definition, Entity worker)
        {
            var context = ExecutorContext(order, definition, worker);
            Fix rate = ActivityExecutorContracts.Read(_world, definition.Executor, context.Target, context.Worker, () => definition.Executor.ReadRate(context));
            if (rate < Fix.Zero) throw new InvalidOperationException("activity executor returned negative work rate");
            return rate;
        }
    }
}
