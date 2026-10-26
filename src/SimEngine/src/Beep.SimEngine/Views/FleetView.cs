using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class FleetView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal FleetView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        private IMachineFleets Fleets => _world.Services.Get<IMachineFleets>();
        public void CopyPolicies(IList<FleetPolicySnapshot> destination)
            => Fleets.CopyPolicies(_viewer, destination);
        private static FleetSnapshot Private(FleetSnapshot value)
        {
            if (value == null) return null;
            var entries = new List<FleetMemberSnapshot>();
            foreach (var member in value.Members)
                entries.Add(new FleetMemberSnapshot(member.Entity, member.Blocker == Codes.NotOwned ? Codes.NotFound : member.Blocker, member.Navigation));
            return new FleetSnapshot(value.Company, value.Id, value.Settings, value.Blocker == Codes.NotOwned ? Codes.NotFound : value.Blocker, entries);
        }
        public bool TryRead(string id, out FleetSnapshot snapshot)
        { bool found = Fleets.TryRead(_viewer, id, out var value); snapshot = Private(value); return found; }
        public void CopyFleets(IList<FleetSnapshot> destination)
        {
            destination.Clear();
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(_viewer)) return;
            var values = new List<FleetSnapshot>(); Fleets.CopyFleets(_viewer, values);
            destination.Clear(); foreach (var value in values) destination.Add(Private(value));
        }
        public Code PreviewConfigure(string id, FleetSettings settings, out FleetSnapshot snapshot)
        {
            Code refusal = Fleets.PreviewConfigure(_viewer, id, settings, out var value);
            snapshot = Private(value);
            return refusal == Codes.NotOwned ? Codes.NotFound : refusal;
        }
        public Code PreviewRemove(string id) => Fleets.CheckRemove(_viewer, id);
        public MotionNavigationSelectionPreview PreviewNavigation(string id, string profile)
        {
            var value = Fleets.PreviewNavigation(_viewer, id, profile);
            if (value.Blocker != Codes.NotOwned) return value;
            var entries = new MotionNavigationSelectionEntry[value.Entries.Count];
            for (int i = 0; i < entries.Length; i++)
            { var e = value.Entries[i]; entries[i] = new MotionNavigationSelectionEntry(e.Entity, e.Blocker == Codes.NotOwned ? Codes.NotFound : e.Blocker, e.Selection); }
            return new MotionNavigationSelectionPreview(Codes.NotFound, entries);
        }
        public WorkerHomeSelectionPreview PreviewHome(string id, EntityId home)
        {
            var value = Fleets.PreviewHome(_viewer, id, home);
            if (value.Blocker != Codes.NotOwned) return value;
            var entries = new WorkerHomeSelectionEntry[value.Entries.Count];
            for (int i = 0; i < entries.Length; i++)
            { var e = value.Entries[i]; entries[i] = new WorkerHomeSelectionEntry(e.Entity, e.Blocker == Codes.NotOwned ? Codes.NotFound : e.Blocker, e.Current); }
            return new WorkerHomeSelectionPreview(Codes.NotFound, entries);
        }
        public OutletPolicySelectionPreview PreviewOutletPolicy(string id, OutletPolicyChange change)
        {
            var value = Fleets.PreviewOutletPolicy(_viewer, id, change);
            if (value.Blocker != Codes.NotOwned) return value;
            var entries = new OutletPolicySelectionEntry[value.Entries.Count];
            for (int i = 0; i < entries.Length; i++)
            { var e = value.Entries[i]; entries[i] = new OutletPolicySelectionEntry(e.Entity, e.Blocker == Codes.NotOwned ? Codes.NotFound : e.Blocker, e.Current, e.Next); }
            return new OutletPolicySelectionPreview(Codes.NotFound, entries);
        }
        public RecurringPlanSelectionPreview PreviewRecurringPlan(string id, Code source, RecurringPlanSettings settings)
        {
            var value = Fleets.PreviewRecurringPlan(_viewer, id, source, settings);
            if (value.Blocker != Codes.NotOwned) return value;
            var entries = new RecurringPlanSelectionEntry[value.Entries.Count];
            for (int i = 0; i < entries.Length; i++)
            { var e = value.Entries[i]; entries[i] = new RecurringPlanSelectionEntry(e.Entity, e.Source, e.Blocker == Codes.NotOwned ? Codes.NotFound : e.Blocker, e.Schedule); }
            return new RecurringPlanSelectionPreview(Codes.NotFound, entries);
        }
        public HaulTripSelectionPreview PreviewTripPolicy(string id, HaulTripSettings settings)
        {
            var value = Fleets.PreviewTripPolicy(_viewer, id, settings);
            if (value.Blocker != Codes.NotOwned) return value;
            var entries = new HaulTripSelectionEntry[value.Entries.Count];
            for (int i = 0; i < entries.Length; i++)
            { var e = value.Entries[i]; entries[i] = new HaulTripSelectionEntry(e.Entity, e.Blocker == Codes.NotOwned ? Codes.NotFound : e.Blocker, e.Settings); }
            return new HaulTripSelectionPreview(Codes.NotFound, entries);
        }
        public HaulLoadSelectionPreview PreviewLoadPolicy(string id, HaulLoadSettings settings)
        {
            var value = Fleets.PreviewLoadPolicy(_viewer, id, settings);
            if (value.Blocker != Codes.NotOwned) return value;
            var entries = new HaulLoadSelectionEntry[value.Entries.Count];
            for (int i = 0; i < entries.Length; i++)
            { var e = value.Entries[i]; entries[i] = new HaulLoadSelectionEntry(e.Entity, e.Blocker == Codes.NotOwned ? Codes.NotFound : e.Blocker, e.Settings); }
            return new HaulLoadSelectionPreview(Codes.NotFound, entries);
        }
        public WorkPolicySelectionPreview PreviewWorkPolicy(string id, WorkPolicySettings settings)
        {
            var value = Fleets.PreviewWorkPolicy(_viewer, id, settings);
            if (value.Blocker != Codes.NotOwned) return value;
            var entries = new WorkPolicySelectionEntry[value.Entries.Count];
            for (int i = 0; i < entries.Length; i++)
            { var e = value.Entries[i]; entries[i] = new WorkPolicySelectionEntry(e.Entity, e.Blocker == Codes.NotOwned ? Codes.NotFound : e.Blocker, e.Settings); }
            return new WorkPolicySelectionPreview(Codes.NotFound, entries);
        }
    }
}
