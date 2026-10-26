using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IPlanHistoryPolicy : IContentDefinition
    {
        GameDuration MinimumAge { get; }
        int MinimumRetainedCohorts { get; }
        int CohortsPerUpdate { get; }
    }
    public abstract class PlanHistoryPolicyDef : Def, IPlanHistoryPolicy
    {
        public abstract GameDuration MinimumAge { get; }
        public abstract int MinimumRetainedCohorts { get; }
        public abstract int CohortsPerUpdate { get; }
    }
    public sealed class AuthoredPlanHistoryPolicyDef : PlanHistoryPolicyDef
    {
        private PlanHistoryPolicyDeclaration _declaration;
        public override GameDuration MinimumAge => _declaration.MinimumAge;
        public override int MinimumRetainedCohorts => _declaration.MinimumRetainedCohorts;
        public override int CohortsPerUpdate => _declaration.CohortsPerUpdate;
        protected override void ReadCore(JsonReader reader) => _declaration = PlanHistoryPolicyDeclaration.Read(reader);
    }
    /// <summary>One explicit immutable reader shared by native and independent authored policies.</summary>
    public sealed class PlanHistoryPolicyDeclaration
    {
        public GameDuration MinimumAge { get; }
        public int MinimumRetainedCohorts { get; }
        public int CohortsPerUpdate { get; }
        private PlanHistoryPolicyDeclaration(GameDuration age, int retained, int budget)
        { MinimumAge = age; MinimumRetainedCohorts = retained; CohortsPerUpdate = budget; }
        public static PlanHistoryPolicyDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ContentException("explicit plan history policy required");
            GameDuration age = (reader.Child("minimum_age") ?? throw reader.Error("minimum_age required")).RationalDuration();
            int retained = reader.Int("minimum_retained_cohorts"), budget = reader.Int("cohorts_per_update");
            if (retained < 0 || budget <= 0) throw reader.Error("nonnegative retained cohorts and positive update budget required");
            return new PlanHistoryPolicyDeclaration(age, retained, budget);
        }
    }
    internal static class PlanHistoryPolicyContracts
    {
        internal static PlanHistoryPolicyView Read(World world, IPlanHistoryPolicy policy, long cursor)
        {
            if (policy == null || !world.Content.Owns(policy) || !ReferenceEquals(world.Content.Find<IPlanHistoryPolicy>(policy.Id), policy))
                throw new ContentException("history policy must alias immutable selected content");
            return EntityObservation.ReadProvider(world, policy, null, () =>
            {
                GameDuration age = policy.MinimumAge; int retained = policy.MinimumRetainedCohorts, budget = policy.CohortsPerUpdate;
                if (retained < 0 || budget <= 0) throw new ContentException("history policy requires nonnegative retention and positive update budget");
                try { if (world.Time.Ticks(age) < 0) throw new ArgumentException("history age must be nonnegative"); }
                catch (Exception error) when (error is OverflowException || error is ArgumentException)
                { throw new ContentException("history age cannot resolve against selected game time: " + error.Message); }
                return new PlanHistoryPolicyView(policy.Id, age, retained, budget, cursor);
            });
        }
    }
    public readonly struct PlanHistoryPolicyView
    {
        public string Policy { get; }
        public GameDuration MinimumAge { get; }
        public int MinimumRetainedCohorts { get; }
        public int CohortsPerUpdate { get; }
        /// <summary>Last examined actual plan head ID, not another time coordinate.</summary>
        public long Cursor { get; }
        internal PlanHistoryPolicyView(string policy, GameDuration age, int retained, int budget, long cursor)
        { Policy = policy; MinimumAge = age; MinimumRetainedCohorts = retained;
            CohortsPerUpdate = budget; Cursor = cursor; }
    }
    public readonly struct PlanHistoryCohortView
    {
        public long Head { get; }
        public IReadOnlyList<long> Plans { get; }
        public long EndedTick { get; }
        public Code Blocker { get; }
        public bool Eligible => Blocker.IsNone;
        internal PlanHistoryCohortView(long head, IEnumerable<long> plans, long ended, Code blocker)
        { Head = head; Plans = new List<long>(plans).AsReadOnly(); EndedTick = ended; Blocker = blocker; }
    }
}
