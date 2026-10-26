using System;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class LogisticsService
    {
        public HaulTripEvaluation ReadHaulTripBudget(EntityId worker)
        {
            Entity carrier = _world.Entities.Get(worker);
            if (carrier == null || !carrier.Alive) throw new ArgumentException("trip budget requires an actual live carrier");
            if (carrier.Get<IHaulTripPolicy>() == null) return null;
            WorkBoard board = _world.Services.Get<WorkBoard>();
            Job job = board.Job(carrier.Get<IWorker>()?.Job ?? 0);
            if (job?.IsLive != true || !job.IsHaul || job.Worker != worker || job.ToilIndex >= job.Toils.Count) return null;
            // Acquired cargo is already excluded by its actual outbound receipts. Adding the
            // fresh return commitment here would count it twice and strand a funded return.
            return EntityObservation.Read(_world, carrier, () => HaulTripPolicyContracts.CheckTrip(_world, carrier,
                board.ReadRemainingHaulTravel(job, carrier), emergency: board.IsEmergency(job)));
        }
    }
}
