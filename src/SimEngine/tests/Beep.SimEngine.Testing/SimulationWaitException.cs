using System;
using System.Collections.Generic;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Testing
{
    public enum SimulationWaitFailure { StepLimitReached, EngineStopped }
    public enum SimulationWaitBudgetUnit { SimulationSteps, LifecycleUpdates }

    /// <summary>Detached public developer diagnostics, not another gameplay state/progress authority.</summary>
    public sealed class SimulationWaitException : InvalidOperationException
    {
        internal SimulationWaitException(SimGame game, SimulationWaitFailure reason, int completed, int allowed, SimulationWaitBudgetUnit budgetUnit)
            : base("simulation wait failed: " + reason + "; scenario=" + game.Scenario + "; seed=" + game.Seed +
                "; completed=" + completed + "/" + allowed + "; budget-unit=" + budgetUnit + "; clock=" + game.Clock.StepIndex +
                "; state=" + game.State + "; outcome=" + game.Outcome + "; fault=" + game.Fault)
        {
            Reason = reason; CompletedSteps = completed; AllowedSteps = allowed; BudgetUnit = budgetUnit;
            Scenario = game.Scenario; Seed = game.Seed; ClockStep = game.Clock.StepIndex; State = game.State;
            Outcome = game.Outcome; Fault = game.Fault; Snapshot = game.Snapshot;
            Modules = new List<string>(game.Modules).AsReadOnly();
            if (Fault != null) HashUnavailable = "faulted partial update: " + Fault;
            else
            {
                try { StateHash = game.Hash(); }
                catch (Exception failure) { HashUnavailable = failure.GetType().Name + ": " + failure.Message; }
            }
        }
        public SimulationWaitFailure Reason { get; }
        public SimulationWaitBudgetUnit BudgetUnit { get; }
        public int CompletedSteps { get; }
        public int AllowedSteps { get; }
        public string Scenario { get; }
        public ulong Seed { get; }
        public long ClockStep { get; }
        public GameState State { get; }
        public Code Outcome { get; }
        public WorldFault Fault { get; }
        /// <summary>Observer-level retained publication, not a company-private transport feed.</summary>
        public GameSnapshot Snapshot { get; }
        public IReadOnlyList<string> Modules { get; }
        public string EngineBuild => EngineInfo.Build;
        public int SaveFormat => EngineInfo.SaveFormat;
        public ulong? StateHash { get; }
        public string HashUnavailable { get; }
    }
}

