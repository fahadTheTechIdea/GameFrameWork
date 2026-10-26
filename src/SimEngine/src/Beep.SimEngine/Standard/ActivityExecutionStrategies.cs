using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Sole authored fixed-work strategy; execution state remains with the original job and node.</summary>
    public sealed class FixedActivityExecution
    {
        public static readonly Code UnsupportedDock = new Code("work.fixed-unsupported-dock");
        public Code Work { get; }
        public Code AccessBlocker { get; }
        private FixedActivityExecution(JsonReader reader)
        {
            Work = ActivityExecutorContracts.ReadCode(reader, "work");
            AccessBlocker = ActivityExecutorContracts.ReadCode(reader, "access_blocker");
        }
        public static FixedActivityExecution Read(JsonReader reader)
            => new FixedActivityExecution(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void ValidateDefinition(IActivityNodeDefinition definition) => ActivityExecutorContracts.ValidateRoles(definition, true);
        public void ValidateComposition(World world)
        {
            var phases = world.Services.Get<IWorkPhaseCatalog>();
            if (!phases.Contains(Work) || phases.Purpose(Work) != WorkPhasePurpose.Productive)
                throw new ContentException("fixed executor requires a composed productive phase");
        }
        public Code CheckWorker(WorkRequirementContext context)
        {
            if (context.Target.Get<IWorkDock>() != null) return UnsupportedDock;
            return SpatialInteraction.CheckProximity(context.World, context.Worker, context.Target) == SpatialProximityStatus.WithinReach ? Code.None : AccessBlocker;
        }
        public IReadOnlyList<Toil> CreateItinerary(ActivityExecutorContext context)
        {
            if (context.Target == null) throw new InvalidOperationException("fixed activity target is absent");
            return Array.AsReadOnly(new[] { new Toil { Executor = Work, At = context.Order.Target } });
        }
        public bool MatchesItinerary(ActivityExecutorContext context, IReadOnlyList<Toil> itinerary)
            => itinerary.Count == 1 && itinerary[0].Executor == Work && itinerary[0].At == context.Order.Target &&
                !itinerary[0].Resource.IsValid && itinerary[0].Amount.IsZero;
        public ActivityApproachResult Reapproach(ActivityExecutorContext context, int current) => ActivityApproachResult.Wait(AccessBlocker);
        public Fix ReadRate(ActivityExecutorContext context)
            => context.World.Services.Get<IWorkerDispatch>().EffectiveWorkRate(context.World, context.Worker);
    }

    /// <summary>Sole typed timed-work strategy; no fabricated worker, itinerary, progress or extra clock.</summary>
    public sealed class TimedActivityExecution
    {
        public Fix Rate { get; }
        private TimedActivityExecution(JsonReader reader)
        { Rate = reader.Fix("rate"); if (!Rate.IsPositive) throw reader.Error("timed executor rate must be positive"); }
        public static TimedActivityExecution Read(JsonReader reader)
            => new TimedActivityExecution(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void ValidateDefinition(IActivityNodeDefinition definition) => ActivityExecutorContracts.ValidateRoles(definition, false);
        public void ValidateComposition(World world) { }
        public IReadOnlyList<Toil> CreateItinerary(ActivityExecutorContext context) => Array.Empty<Toil>();
        public bool MatchesItinerary(ActivityExecutorContext context, IReadOnlyList<Toil> itinerary) => itinerary.Count == 0;
        public Code CheckWorker(WorkRequirementContext context) => Code.None;
        public ActivityApproachResult Reapproach(ActivityExecutorContext context, int current) => throw new InvalidOperationException("timed node has no worker approach");
        public Fix ReadRate(ActivityExecutorContext context) => Rate;
    }
}
