using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Selection on the ORIGINAL dock/job authority. Presence owns occupancy; motion owns travel.
    // No home-slot binding, reservation, parking queue, route copy or additional elapsed clock.
    public sealed partial class WorkBoard
    {
        internal bool ReadHomeParking(Entity home, Entity worker, out Code selected,
            out SpatialInteractionStatus status, out Code blocker)
        {
            selected = Code.None; status = SpatialInteractionStatus.Unsupported; blocker = Code.None;
            IWorkDock dock = Dock(home); if (dock == null) return false;
            bool compatible = false;
            var slots = new List<WorkDockSlot>(OrderedSlots(dock, WorkDockSlotKind.Parking));
            if (slots.Count == 0) return false;
            var arrived = new HashSet<Code>();
            var candidates = new List<(WorkDockSlot Slot, long Cost, int Order)>();
            Code firstBlocker = Code.None; int firstOrder = int.MaxValue, order = 0;
            void Refuse(Code reason, int index)
            { if (index < firstOrder) { firstOrder = index; firstBlocker = reason; } }
            // An eligible actual arrival wins over every route candidate. Geometric arrival
            // is pure and independent of role/service admission, never inferred from cost.
            foreach (WorkDockSlot slot in slots)
            {
                int index = order++;
                if (!ReadDock(dock, worker, () => dock.IsAtSlot(_world, worker, slot.Id))) continue;
                arrived.Add(slot.Id);
                Code refusal = AdmitHomeParking(dock, worker, slot, out bool role);
                if (!role) continue;
                compatible = true;
                if (!refusal.IsNone) { Refuse(refusal, index); continue; }
                selected = slot.Id; status = SpatialInteractionStatus.WithinReach; return true;
            }
            // Motion already owns this destination. Transient traffic may block its current
            // route without invalidating the parking intent. Re-ranking every update makes
            // a returning worker chase a passing vehicle between alternative parking slots.
            // Keep no new binding: re-observe the real goal, admission and structural route.
            foreach (WorkDockSlot slot in slots)
            {
                if (arrived.Contains(slot.Id) || !ReadDock(dock, worker, () => dock.IsApproachingSlot(_world, worker, slot.Id))) continue;
                Code refusal = AdmitHomeParking(dock, worker, slot, out bool role);
                if (!role || !refusal.IsNone) continue;
                var route = PlanRoute(worker, null, new MotionDestination(CaptureDock(dock, worker, slot.Id), MotionDestinationKind.Exact));
                if (!route.Blocker.IsNone) continue;
                selected = slot.Id; status = SpatialInteractionStatus.Pending; return true;
            }
            order = 0;
            foreach (WorkDockSlot slot in slots)
            {
                int index = order++;
                if (arrived.Contains(slot.Id)) continue; // Already refused by its actual admission, never a route alternative.
                Code refusal = AdmitHomeParking(dock, worker, slot, out bool role);
                if (!role) continue;
                compatible = true;
                if (!refusal.IsNone) { Refuse(refusal, index); continue; }
                if (!DockCost(dock, worker, slot.Id, null, out long cost))
                {
                    // Preserve reach-before-cost refusal semantics even for an estimator that refuses.
                    var reach = DockReach(dock, worker, slot.Id);
                    if (reach == SpatialInteractionStatus.WithinReach) throw new InvalidOperationException("dock arrival and reach disagree");
                    Code reason = reach == SpatialInteractionStatus.Unsupported ? WorkDockCodes.UnsupportedGeometry : WorkSystem.BlockerUnreachable;
                    if (SpatialInteraction.CanApproach(reach))
                    {
                        var route = PlanRoute(worker, null, new MotionDestination(CaptureDock(dock, worker, slot.Id), MotionDestinationKind.Exact));
                        if (!route.Blocker.IsNone) reason = route.Blocker;
                    }
                    Refuse(reason, index);
                    continue;
                }
                candidates.Add((slot, cost, index));
            }
            candidates.Sort((a, b) => Cheaper(a.Slot, a.Cost, b.Slot, b.Cost) ? -1 : Cheaper(b.Slot, b.Cost, a.Slot, a.Cost) ? 1 : 0);
            foreach (var candidate in candidates)
            {
                var reach = DockReach(dock, worker, candidate.Slot.Id);
                if (reach == SpatialInteractionStatus.WithinReach) throw new InvalidOperationException("dock arrival and reach disagree");
                if (!SpatialInteraction.CanApproach(reach))
                { Refuse(reach == SpatialInteractionStatus.Unsupported ? WorkDockCodes.UnsupportedGeometry : WorkSystem.BlockerUnreachable, candidate.Order); continue; }
                var route = PlanRoute(worker, null, new MotionDestination(CaptureDock(dock, worker, candidate.Slot.Id), MotionDestinationKind.Exact));
                if (!route.Blocker.IsNone) { Refuse(route.Blocker, candidate.Order); continue; }
                selected = candidate.Slot.Id; status = reach; return true;
            }
            blocker = firstBlocker;
            if (blocker.IsNone) blocker = compatible ? WorkDockCodes.Full : WorkDockCodes.Incompatible;
            status = blocker == WorkDockCodes.UnsupportedGeometry ? SpatialInteractionStatus.Unsupported : SpatialInteractionStatus.Unreachable;
            return true;
        }
        private Code AdmitHomeParking(IWorkDock dock, Entity worker, WorkDockSlot slot, out bool role)
        {
            role = ReadDock(dock, worker, () => !slot.WorkRole.IsValid || worker.Get<IWorker>()?.Can(slot.WorkRole) == true);
            if (!role) return WorkDockCodes.Incompatible;
            Code refusal = ClaimBlocker(dock, worker, slot.Id);
            if (refusal.IsNone) refusal = Vacancy(dock, worker, slot.Id);
            if (refusal.IsNone) refusal = DockPolicy(dock, worker, slot.Id);
            return refusal;
        }
        internal SpatialInteractionStatus ApproachHomeParking(Entity home, Entity worker, Code slot)
            => DockApproach(Dock(home), worker, slot);
    }
}
