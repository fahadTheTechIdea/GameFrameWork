namespace Beep.SimEngine.Standard
{
    /// <summary>
    /// The standard library's named settings: how urgent each kind of work is and how often planners run.
    /// Retry rules are master-authored work_retry_profiles, not constants here.
    /// A game that needs different planner numbers changes them here or, per order,
    /// in the order's own content ("priority"); nothing in the library uses a bare number for these.
    /// </summary>
    public static class Tuning
    {
        // Priorities of the work the library posts by itself (an order carries its own from content).
        public const int TransformationSupplyPriority = 55;

        /// <summary>Elapsed game duration between logistics planning passes.</summary>
        public static readonly GameDuration LogisticsPlanInterval = GameDuration.Minutes(10);
        /// <summary>Elapsed duration between assignment passes when nothing on the work board changed.</summary>
        public static readonly GameDuration AssignFallbackInterval = GameDuration.Minutes(10);
        /// <summary>Loads planned for one demand in one pass.</summary>
        public const int LoadsPerDemandPass = 4;

    }
}
