using System;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class WorkSystem : IWorkerControl
    {
        public Code CheckHeld(World world, PlayerId company, EntityId identity, bool held)
        {
            world.RequireHealthyUpdate();
            if (!ReferenceEquals(world.Services.Get<IWorkerControl>(), this)) throw new InvalidOperationException("worker control belongs to another session");
            Entity entity = world.Entities.Get(identity);
            if (entity == null || !entity.Alive || entity.Owner != company || !world.ViewFor(company).CanObserveOwned(entity)) return Codes.NotFound;
            if (entity.Get<IWorker>() == null) return StandardCodes.NotWorker;
            return EntityObservation.Read(world, entity, () =>
            {
                IWorker worker = entity.Get<IWorker>();
                Job job = world.Services.Get<WorkBoard>().Job(worker.Job);
                if (!ReferenceEquals(worker.Entity, entity) || !worker.Idle && (job?.IsLive != true || job.Worker != identity || job.Owner != company) ||
                    worker.Held && worker.Job != 0) throw new InvalidOperationException("worker control found invalid actual assignment facts");
                return held ? CheckInterruption(world, entity) : worker.Held ? Code.None : Codes.AlreadyDone;
            });
        }
        public void SetHeld(World world, PlayerId company, EntityId identity, bool held)
        {
            Code refusal = CheckHeld(world, company, identity, held);
            if (!refusal.IsNone) throw new InvalidOperationException("worker control refused at execution: " + refusal);
            Entity entity = world.Entities.Get(identity); IWorker worker = entity.Get<IWorker>();
            EntityId home = worker.Home, next = world.Entities.NextIdentity; long update = world.Clock.StepIndex;
            if (held && worker.Job != 0)
            {
                Job job = world.Services.Get<WorkBoard>().Job(worker.Job);
                if (!Interrupt(world, job).IsNone) throw new InvalidOperationException("admitted worker control lost actual interruption");
            }
            MotionExecutionContracts.Stop(world, entity);
            bool changed = worker.Held != held || worker.HeldArrived;
            worker.Held = held; worker.HeldArrived = false;
            if (worker.Held != held || worker.HeldArrived || worker.Job != 0 || !worker.Idle || worker.Home != home || entity.Owner != company ||
                world.Clock.StepIndex != update || world.Entities.NextIdentity != next || !ReferenceEquals(world.Entities.Get(identity), entity))
                throw new InvalidOperationException("worker control did not commit exact held state without changing its identity/home/session");
            if (changed) entity.Touch();
            _board.Dirty = true;
        }
    }
}
