using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Detached movement allowance derived from actual service and the one game clock.
    /// Not a handling/queue forecast, reservation or mutable timing authority.</summary>
    public sealed class ServiceMovementWindow
    {
        public Code Source { get; }
        public bool Unrestricted { get; }
        public long? DueUpdateStep { get; }
        public GameDuration? DueGameTime { get; }
        public long? RemainingUpdateSteps { get; }
        public GameDuration? RemainingGameTime { get; }
        public Code Blocker { get; }
        internal IReadOnlyList<(EntityId Entity, Code Source)> SourceInputs { get; }

        internal ServiceMovementWindow(World world, Code source, bool unrestricted, long? due, Code blocker,
            IReadOnlyList<(EntityId Entity, Code Source)> sourceInputs = null)
        {
            SourceInputs = sourceInputs == null ? Array.Empty<(EntityId, Code)>()
                : new List<(EntityId Entity, Code Source)>(sourceInputs).AsReadOnly();
            Source = source; Unrestricted = unrestricted; DueUpdateStep = due;
            DueGameTime = due.HasValue ? GameDuration.FromSteps(due.Value, world.Time) : (GameDuration?)null;
            if (!unrestricted && blocker.IsNone)
            {
                if (!due.HasValue) throw new InvalidOperationException("ready service window requires an actual earned deadline");
                long remaining = checked(due.Value - world.Clock.StepIndex);
                if (remaining < 0) blocker = RecurringPlanCodes.MovementWindow;
                else
                {
                    RemainingUpdateSteps = remaining;
                    RemainingGameTime = GameDuration.FromSteps(remaining, world.Time);
                }
            }
            Blocker = blocker;
        }

        /// <summary>Checks this observed allowance; fresh engine dispatch always reads current facts.</summary>
        public Code CheckMovement(long updateSteps)
        {
            if (updateSteps < 0) throw new ArgumentOutOfRangeException(nameof(updateSteps));
            if (!Blocker.IsNone) return Blocker;
            return !Unrestricted && updateSteps > RemainingUpdateSteps.Value ? RecurringPlanCodes.MovementWindow : Code.None;
        }
    }
}
