using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Hosting
{
    /// <summary>Seat-scoped decision access. No lifecycle, observer or mutable world/provider handles.</summary>
    public sealed class PlayerDecisionContext
    {
        private readonly World _world;
        private readonly PlayerView _view;
        private readonly Func<Command, int> _submit;
        private readonly Func<IReadOnlyList<CommandResult>> _answers;
        private readonly Func<int, CommandResult?> _findAnswer;
        private int? _remaining;
        private bool _closed;
        internal PlayerDecisionContext(World world, PlayerId company, PlayerView view,
            Func<Command, int> submit, Func<IReadOnlyList<CommandResult>> answers, int? budget,
            Func<int, CommandResult?> findAnswer = null)
        { _world = world; Id = company; _view = view; _submit = submit; _answers = answers; _remaining = budget; _findAnswer = findAnswer; }
        public PlayerId Id { get; }
        public PlayerView View { get { RequireOpen(); return _view; } }
        public long Tick { get { RequireOpen(); return _world.Tick; } }
        public ISimulationTime Time { get { RequireOpen(); return _world.Time; } }
        public CalendarReading Date { get { RequireOpen(); return _world.Date; } }
        public IReadOnlyList<CommandResult> Answers { get { RequireOpen(); return _answers(); } }
        /// <summary>Actual remaining authored input budget; null means this context is not budgeted.</summary>
        public int? RemainingCommands { get { RequireOpen(); return _remaining; } }
        /// <summary>Actual owned reply, when retained; false never fabricates acceptance or refusal.</summary>
        public bool TryGetAnswer(int sequence, out CommandResult answer)
        {
            RequireOpen();
            if (_findAnswer != null)
            {
                CommandResult? found = _findAnswer(sequence); answer = found.GetValueOrDefault(); return found.HasValue;
            }
            foreach (CommandResult candidate in Answers)
                if (candidate.Sequence == sequence) { answer = candidate; return true; }
            answer = default; return false;
        }
        internal Code Blocker { get; private set; }
        public Code Preview(Command command)
        { RequireOpen(); return _world.Check(Id, command); }
        public int Submit(Command command)
        {
            RequireOpen();
            if (_remaining == 0) throw new InvalidOperationException("controller exceeded its authored command budget");
            int sequence = _submit(command);
            if (_remaining.HasValue) _remaining--;
            return sequence;
        }
        public Code ObserveCondition(IGameCondition condition)
        { RequireOpen(); return GameConditionContracts.Read(_world, View, condition); }
        public PlanSiteObservation ObservePlanSites(IPlanSiteSource source)
        { RequireOpen(); return PlanSiteContracts.Read(_world, View, source); }
        public PlanRecoveryCheck ObservePlanRecovery(IPlanRecoveryPolicy policy, PlanRecoveryContext context)
        {
            RequireOpen();
            if (context == null || !ReferenceEquals(context.View, View)) throw new ArgumentException("recovery must use the actual decision view");
            return PlanRecoveryContracts.Read(_world, policy, context);
        }
        public void ReportBlocker(Code blocker) { RequireOpen(); Blocker = blocker; }
        internal void Invoke(IPlayerController controller)
        {
            try
            {
                RequireOpen();
                controller.Decide(this);
                _world.RequireHealthyUpdate();
            }
            catch (Exception error)
            {
                _world.LatchFault("player.decide." + Id.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), error);
                throw;
            }
            finally { _closed = true; }
        }
        private void RequireOpen()
        {
            if (_closed) throw new InvalidOperationException("controller decision context has expired");
            _world.RequireHealthyUpdate();
        }
    }
    /// <summary>Optional persistence capability required on engine-authored decision providers.</summary>
    public interface IPlayerControllerState
    {
        int StateSchemaVersion { get; }
        void Sync(IStateSync sync);
    }
}
