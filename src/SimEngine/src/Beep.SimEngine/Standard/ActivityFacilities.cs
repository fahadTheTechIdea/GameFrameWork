using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Detached safe offer for preference only; policy cannot override engine admission.</summary>
    public readonly struct ActivityFacilityFacts
    {
        public EntityId Worker { get; }
        public EntityId Target { get; }
        public EntityId Home { get; }
        public WorkerRoleId Role { get; }
        public long TravelCost { get; }
        internal ActivityFacilityFacts(EntityId worker, EntityId target, EntityId home, WorkerRoleId role, long cost)
        { Worker = worker; Target = target; Home = home; Role = role; TravelCost = cost; }
    }
    public interface IActivityFacilityPolicy : IContentDefinition
    {
        /// <summary>Explicit positive machine node whose current readiness probes candidate facilities.</summary>
        string Node { get; }
        /// <summary>Nonnegative rank, lower first; null deliberately excludes a safe offer.</summary>
        long? Rank(ActivityFacilityFacts facts);
    }
    public sealed class AuthoredActivityFacilityPolicyDef : Def, IActivityFacilityPolicy
    {
        private ActivityFacilityPolicyDeclaration _declaration;
        public string Node => _declaration.Node;
        public bool PreferHome => _declaration.PreferHome;
        protected override void ReadCore(JsonReader reader) => _declaration = ActivityFacilityPolicyDeclaration.Read(reader);
        public long? Rank(ActivityFacilityFacts facts) => _declaration.Rank(facts);
    }
    /// <summary>Sole native facility preference reader and pure willingness decision; admission stays in WorkBoard.</summary>
    public sealed class ActivityFacilityPolicyDeclaration
    {
        private ActivityFacilityPolicyDeclaration() { }
        private string _node;
        public string Node => _node;
        public bool PreferHome { get; private set; }
        public static ActivityFacilityPolicyDeclaration Read(JsonReader reader)
        {
            var declaration = new ActivityFacilityPolicyDeclaration(); declaration.ReadCore(reader); return declaration;
        }
        private void ReadCore(JsonReader reader)
        {
            _node = reader.String("node"); PreferHome = reader.Bool("prefer_home");
            try { ContractIdentity.ValidateId(_node, "facility probe node"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
        }
        public long? Rank(ActivityFacilityFacts facts) => PreferHome && facts.Target != facts.Home ? 1 : 0;
        internal static void ValidateComposition(World world)
        {
            foreach (var policy in world.Content.All<IActivityFacilityPolicy>())
                ActivityFacilityPolicyContracts.Read(world, policy, null, null, () => {
                    try { ContractIdentity.ValidateId(policy.Node, "facility probe node"); }
                    catch (ArgumentException error) { throw new ContentException(error.Message); }
                    return true;
                });
        }
    }
    /// <summary>Observe selected content and whole actual entities; never own selection or work state.</summary>
    internal static class ActivityFacilityPolicyContracts
    {
        internal static T Read<T>(World world, IActivityFacilityPolicy policy, Entity target, Entity worker, Func<T> read)
        {
            var targetCodec = EntityObservation.CaptureProvider(world, target,
                target == null ? (Action<IStateSync>)null : target.Sync, "work.facility-policy");
            var workerFacts = worker == null ? EntityObservation.Capture(world) : EntityObservation.Capture(world, worker);
            var workerCodec = EntityObservation.CaptureProvider(world, worker,
                worker == null || ReferenceEquals(target, worker) ? (Action<IStateSync>)null : worker.Sync, "work.facility-policy");
            T value = EntityObservation.ReadProvider(world, policy, target, () => {
                if (!(policy is Def actual) || !actual.IsFrozen || policy is IPersistentService ||
                    string.IsNullOrWhiteSpace(actual.Id) || string.IsNullOrWhiteSpace(actual.Module) ||
                    policy.Id != actual.Id || policy.Module != actual.Module || !world.Content.Owns(policy) ||
                    !ReferenceEquals(world.Content.Find<IActivityFacilityPolicy>(actual.Id), policy))
                    throw new ContentException("facility policy requires its actual immutable catalog alias");
                return read();
            }, world.Space);
            try { workerFacts.Verify(); workerCodec.Verify(); targetCodec.Verify(); }
            catch (Exception error) { world.LatchFault("work.facility-policy", error); throw; }
            return value;
        }
    }
    public readonly struct ActivityFacilityCandidate
    {
        public EntityId Target { get; }
        public WorkerRoleId Role { get; }
        public long? TravelCost { get; }
        public WorkRequirementResult Readiness { get; }
        public long? Preference { get; }
        public bool Ready => Readiness.Status == WorkRequirementStatus.Satisfied && TravelCost.HasValue && Preference.HasValue;
        internal ActivityFacilityCandidate(EntityId target, WorkerRoleId role, long? cost, WorkRequirementResult readiness, long? preference)
        { Target = target; Role = role; TravelCost = cost; Readiness = readiness; Preference = preference; }
    }
    public sealed class ActivityFacilitySelectionView
    {
        public Code Policy { get; }
        public EntityId Worker { get; }
        public EntityId Target { get; }
        public RecurringPlanRequest Request { get; }
        public IReadOnlyList<ActivityFacilityCandidate> Candidates { get; }
        internal ActivityFacilitySelectionView(Code policy, EntityId worker, EntityId target, RecurringPlanRequest request, IEnumerable<ActivityFacilityCandidate> candidates)
        { Policy = policy; Worker = worker; Target = target; Request = request; Candidates = new List<ActivityFacilityCandidate>(candidates).AsReadOnly(); }
    }
    /// <summary>Pure query on the existing board, never another dispatcher or plan authority.</summary>
    public interface IActivityFacilitySelection
    {
        ActivityFacilitySelectionView ReadFacilities(IRecurringPlanSource source);
    }
    public static class ActivityFacilityCodes
    {
        public static readonly Code NoFacility = new Code("work.recurring-no-facility"),
            NotSelected = new Code("work.facility-policy-not-selected"), Excluded = new Code("work.facility-policy-excluded"),
            Unknown = new Code("work.facility-policy-unknown"), Incompatible = new Code("work.facility-policy-incompatible");
    }
}
