using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Open authored strategy factory in the existing content catalog.</summary>
    public interface IPlayerControllerDefinition : IContentDefinition
    {
        GameDuration Cadence { get; }
        int CommandsPerDecision { get; }
        IPlayerController Create();
        void ValidateBinding(World world, PlayerId company);
    }
    public abstract class PlayerControllerDef : Def, IPlayerControllerDefinition
    {
        public GameDuration Cadence { get; private set; }
        public int CommandsPerDecision { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Cadence = (reader.Child("cadence") ?? throw reader.Error("cadence required")).RationalDuration();
            CommandsPerDecision = reader.Int("commands_per_decision");
            if (Cadence.IsZero || CommandsPerDecision <= 0) throw reader.Error("positive cadence and command budget required");
        }
        public abstract IPlayerController Create();
        public virtual void ValidateBinding(World world, PlayerId company) { }
    }
    public readonly struct PlayerControllerFacts
    {
        public PlayerId Company { get; }
        public string Profile { get; }
        public Controller Selection { get; }
        public long Decisions { get; }
        public long LastDecision { get; }
        public long NextDecision { get; }
        public Code Blocker { get; }
        public int PendingAnswers { get; }
        public int AwaitingCommands { get; }
        internal PlayerControllerFacts(PlayerId company, string profile, Controller selection, long decisions,
            long last, long next, Code blocker, int answers, int awaiting)
        { Company = company; Profile = profile; Selection = selection; Decisions = decisions; LastDecision = last;
            NextDecision = next; Blocker = blocker; PendingAnswers = answers; AwaitingCommands = awaiting; }
    }
    public interface IPlayerControllers
    {
        Code CheckSelection(PlayerId company, Controller selection);
        void CopyControllers(PlayerId viewer, IList<PlayerControllerFacts> destination);
        bool CopyPlaybook(PlayerId viewer, PlayerId company, IList<PlaybookStepSnapshot> destination);
    }
    /// <summary>One scheduling/reply/codec owner. The service alias never adds another persistent store.</summary>
    internal sealed class PlayerControllersSystem : GameSystem, IPlayerControllers, ICompositionValidator, IPlanHistoryRetention,
        IEntityReplacementReferenceOwner
    {
        private static readonly Code CommandStream = new Code(PlayerControllersModule.ModuleId);
        private readonly World _world;
        private readonly SortedDictionary<int, Binding> _bindings = new SortedDictionary<int, Binding>();
        private sealed class Binding
        {
            public PlayerId Company;
            public IPlayerControllerDefinition Profile;
            public IPlayerController Controller;
            public IPlayerControllerState State;
            public int Schema;
            public long Cadence, Decisions, Last = -1, Next;
            public Code Blocker;
            public readonly List<int> Awaiting = new List<int>();
            public readonly List<CommandResult> Answers = new List<CommandResult>();
        }
        private sealed class ControllerNode : IDependencyNode
        {
            public string Id { get; }
            public IReadOnlyList<string> Dependencies { get; }
            public ControllerNode(IPlayerControllerDefinition definition)
            {
                Id = definition.Id;
                var dependencies = definition is IDependencyNode declared ? declared.Dependencies : Array.Empty<string>();
                if (dependencies == null) throw new ContentException(Id + ": explicit controller dependencies required");
                // Enumerate in the same provider observation as the getter. The existing
                // DependencyGraph remains the only topology validator/compiler.
                var captured = new List<string>();
                foreach (string dependency in dependencies) captured.Add(dependency);
                Dependencies = captured.AsReadOnly();
            }
        }
        public PlayerControllersSystem(World world, IReadOnlyDictionary<int, IPlayerControllerDefinition> profiles)
        {
            _world = world;
            var nodes = new List<ControllerNode>();
            foreach (IPlayerControllerDefinition profile in world.Content.All<IPlayerControllerDefinition>())
            {
                nodes.Add(EntityObservation.ReadProvider(world, profile, null, () => {
                    if (!world.Content.Owns(profile) || !ReferenceEquals(world.Content.Find<IPlayerControllerDefinition>(profile.Id), profile))
                        throw new ContentException("controller needs an actual unambiguous profile");
                    _ = ResolveCadence(profile); _ = CommandBudget(profile);
                    return new ControllerNode(profile);
                }));
            }
            _ = new DependencyGraph<ControllerNode>(nodes, PlayerControllersModule.ModuleId);
            var instances = new List<IPlayerController>();
            foreach (KeyValuePair<int, IPlayerControllerDefinition> pair in profiles)
            {
                IPlayerController controller = EntityObservation.ReadProvider(world, pair.Value, null, pair.Value.Create);
                if (controller == null || !(controller is IPlayerControllerState state))
                    throw new ContentException("authored controllers need distinct stateful instances: " + pair.Value.Id);
                int schema = ControllerSchema(controller, state);
                if (schema <= 0 || instances.Exists(previous => ReferenceEquals(previous, controller)))
                    throw new ContentException("authored controllers need distinct stateful instances: " + pair.Value.Id);
                instances.Add(controller);
                long cadence = EntityObservation.ReadProvider(world, pair.Value, null, () => ResolveCadence(pair.Value));
                _bindings.Add(pair.Key, new Binding { Company = new PlayerId(pair.Key), Profile = pair.Value,
                    Controller = controller, State = state, Schema = schema, Cadence = cadence });
            }
        }
        public override string Id => PlayerControllersModule.ModuleId;
        public override int StateSchemaVersion => 2;
        private int ControllerSchema(IPlayerController controller, IPlayerControllerState state)
            => EntityObservation.ReadProvider(_world, controller, null, () => state.StateSchemaVersion);
        // Call only inside the original profile observation; no cached metadata authority.
        private long ResolveCadence(IPlayerControllerDefinition profile)
        {
            GameDuration duration = profile.Cadence;
            try
            {
                long cadence = _world.Time.Ticks(duration);
                if (cadence <= 0) throw new ContentException("controller cadence must advance game time: " + profile.Id);
                return cadence;
            }
            catch (OverflowException) { throw new ContentException("controller cadence exceeds its selected session representation: " + profile.Id); }
        }
        private static int CommandBudget(IPlayerControllerDefinition profile)
        {
            int budget = profile.CommandsPerDecision;
            if (budget <= 0) throw new ContentException("controller command budget must be positive: " + profile.Id);
            return budget;
        }
        public Code CheckReferenceReplacement(World world, Entity source, EntityDef destination)
        {
            if (!ReferenceEquals(world, _world)) throw new InvalidOperationException("foreign controller reference world");
            // A controller owns its company's live intent, not another company's private replacement events.
            // Historical sightings and command replies are not live intent and are never rewritten here.
            foreach (Binding binding in _bindings.Values)
                if (binding.Company == source.Owner && binding.Controller is IEntityReplacementReferenceOwner owner)
                {
                    Code refusal = EntityObservation.ReadProvider(world, binding.Controller, source,
                        () => owner.CheckReferenceReplacement(world, source, destination), observeTargetState: true);
                    if (!refusal.IsNone) return refusal;
                }
            return Code.None;
        }
        public void ReplaceEntityReferences(World world, Entity source, Entity destination)
        {
            if (!ReferenceEquals(world, _world)) throw new InvalidOperationException("foreign controller reference world");
            foreach (Binding binding in _bindings.Values)
                if (binding.Company == source.Owner && binding.Controller is IEntityReplacementReferenceOwner owner)
                    owner.ReplaceEntityReferences(world, source, destination);
        }
        public bool RetainsPlan(World world, PlayerId company, long plan)
        {
            if (!_bindings.TryGetValue(company.Value, out var binding)) return false;
            if (!ReferenceEquals(world, _world)) throw new InvalidOperationException("foreign plan reference world");
            if (binding.Controller is IPlanHistoryRetention reference)
            {
                bool retained = EntityObservation.ReadProvider(world, binding.Controller, null,
                    () => reference.RetainsPlan(world, company, plan));
                if (retained) return true;
            }
            foreach (CommandResult answer in binding.Answers) if (Retains(answer)) return true;
            // Commands can commit before this slower controller collects/consumes their reply.
            foreach (CommandResult answer in _world.LastTickResults)
                if (answer.Stream == CommandStream && binding.Awaiting.Contains(answer.Sequence) && Retains(answer)) return true;
            return false;

            bool Retains(CommandResult answer)
                => answer.Player == company && answer.Stream == CommandStream && answer.Accepted && answer.Command == "sim.place-plan-template"
                    && answer.Arg0 > 0 && answer.Arg1 > 0 && plan >= answer.Arg0 && plan - answer.Arg0 < answer.Arg1;
        }
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ContentException("foreign controller world");
            foreach (Binding binding in _bindings.Values)
            {
                if (!world.Players.IsSeat(binding.Company)) throw new ContentException("controller binding needs an actual session seat");
                if (binding.Schema <= 0 || ControllerSchema(binding.Controller, binding.State) != binding.Schema)
                    throw new ContentException("controller schema must remain stable during composition");
                EntityObservation.ReadProvider(world, binding.Profile, null,
                    () => { binding.Profile.ValidateBinding(world, binding.Company); return 0; });
            }
            for (int i = 0; i < world.Players.Count; i++)
                if (!CheckSelection(new PlayerId(i), world.Players.ControllerOf(new PlayerId(i))).IsNone)
                    throw new ContentException("active autopilot needs an explicitly authored controller profile");
        }
        public Code CheckSelection(PlayerId company, Controller selection)
        {
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            if (selection != Controller.Human && selection != Controller.Autopilot) return Codes.InvalidArgument;
            return selection == Controller.Autopilot && !_bindings.ContainsKey(company.Value) ? Codes.NotFound : Code.None;
        }
        public void CopyControllers(PlayerId viewer, IList<PlayerControllerFacts> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            _world.RequireHealthyUpdate();
            if (viewer != PlayerId.None && !_world.Players.IsSeat(viewer)) return;
            var facts = new List<PlayerControllerFacts>();
            foreach (Binding binding in _bindings.Values)
                if (viewer == PlayerId.None || binding.Company == viewer)
                    facts.Add(EntityObservation.ReadProvider(_world, binding.Controller, null, () =>
                        EntityObservation.ReadProvider(_world, binding.Profile, null, () =>
                            new PlayerControllerFacts(binding.Company, binding.Profile.Id,
                                _world.Players.ControllerOf(binding.Company), binding.Decisions, binding.Last, binding.Next,
                                binding.Blocker, binding.Answers.Count, binding.Awaiting.Count))));
            _world.RequireHealthyUpdate();
            foreach (var fact in facts) destination.Add(fact);
        }
        public bool CopyPlaybook(PlayerId viewer, PlayerId company, IList<PlaybookStepSnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination)); destination.Clear();
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company) || viewer != PlayerId.None && (viewer != company || !_world.Players.IsSeat(viewer)) ||
                !_bindings.TryGetValue(company.Value, out var binding) || !(binding.Controller is IPlanPlaybookDiagnostics provider)) return false;
            var view = _world.ViewFor(company);
            var facts = new List<PlaybookStepSnapshot>();
            EntityObservation.ReadProvider(_world, binding.Controller, null, () => {
                provider.CopySteps(view, condition => GameConditionContracts.Read(_world, view, condition),
                    source => PlanSiteContracts.Read(_world, view, source),
                    (policy, context) => PlanRecoveryContracts.Read(_world, policy, context), facts);
                return 0;
            });
            string previous = null;
            foreach (var fact in facts)
            {
                if (fact == null || previous != null && string.CompareOrdinal(previous, fact.Id) >= 0)
                    throw new InvalidOperationException("controller diagnostics require canonical distinct step identities");
                previous = fact.Id;
            }
            foreach (var fact in facts) destination.Add(fact);
            return true;
        }
        private void ValidateProviderState(Binding binding)
        {
            if (!(binding.Controller is IPlayerControllerStateValidation provider)) return;
            EntityObservation.ReadProvider(_world, binding.Controller, null, () => {
                provider.ValidateState(_world.ViewFor(binding.Company), Array.AsReadOnly(binding.Awaiting.ToArray()), Array.AsReadOnly(binding.Answers.ToArray()));
                return 0;
            });
        }
        public override void Update(World world, TickInfo tick)
        {
            foreach (Binding binding in _bindings.Values)
            {
                foreach (CommandResult answer in world.LastTickResults)
                    if (answer.Player == binding.Company && answer.Stream == CommandStream && binding.Awaiting.Remove(answer.Sequence)) binding.Answers.Add(answer);
                if (!world.Outcome.IsNone || world.Players.ControllerOf(binding.Company) != Controller.Autopilot || tick.Tick < binding.Next) continue;
                long next = checked(tick.Tick + binding.Cadence), decisions = checked(binding.Decisions + 1);
                IReadOnlyList<CommandResult> answers = Array.AsReadOnly(binding.Answers.ToArray());
                var context = new PlayerDecisionContext(world, binding.Company, PlayerView.Create(world, binding.Company),
                    command => { int sequence = world.Submit(binding.Company, command, CommandStream); binding.Awaiting.Add(sequence); return sequence; },
                    () => answers, EntityObservation.ReadProvider(world, binding.Profile, null, () => CommandBudget(binding.Profile)));
                context.Invoke(binding.Controller);
                binding.Answers.Clear(); binding.Blocker = context.Blocker;
                binding.Last = tick.Tick; binding.Next = next; binding.Decisions = decisions;
            }
        }
        public override void Sync(IStateSync sync)
        {
            if (sync.Count(_bindings.Count) != _bindings.Count) throw new CorruptStateException("controller binding count differs from content");
            foreach (Binding binding in _bindings.Values)
            {
                int company = binding.Company.Value, schema = binding.Schema; string profile = binding.Profile.Id;
                sync.Int(ref company); sync.String(ref profile); sync.Int(ref schema);
                if (company != binding.Company.Value || profile != binding.Profile.Id || schema != binding.Schema
                    || ControllerSchema(binding.Controller, binding.State) != schema) throw new CorruptStateException("controller identity/schema differs from content");
                sync.Long(ref binding.Decisions); sync.Long(ref binding.Last); sync.Long(ref binding.Next);
                string blocker = binding.Blocker.Id; sync.String(ref blocker);
                if (blocker != null && blocker.Length == 0) throw new CorruptStateException("empty controller blocker identity");
                if (sync.Reading) binding.Blocker = blocker == null ? Code.None : new Code(blocker);
                sync.Ints(binding.Awaiting);
                int count = sync.Count(binding.Answers.Count);
                var answers = sync.Reading ? new List<CommandResult>() : null;
                for (int i = 0; i < count; i++)
                {
                    CommandResult answer = sync.Reading ? default : binding.Answers[i]; CommandResult.Sync(sync, ref answer);
                    if (sync.Reading) answers.Add(answer);
                }
                if (sync.Reading) { binding.Answers.Clear(); binding.Answers.AddRange(answers); }
                ValidateState(binding); binding.State.Sync(sync);
                if (!sync.Reading) ValidateProviderState(binding);
            }
        }
        public override void OnLoaded(World world)
        {
            foreach (Binding binding in _bindings.Values) { ValidateState(binding); ValidateProviderState(binding); }
            for (int i = 0; i < world.Players.Count; i++)
                if (!CheckSelection(new PlayerId(i), world.Players.ControllerOf(new PlayerId(i))).IsNone)
                    throw new CorruptStateException("restored autopilot lacks its authored controller");
        }
        private void ValidateState(Binding binding)
        {
            if (binding.Decisions < 0 || binding.Decisions == 0 && (binding.Last != -1 || binding.Next != 0
                    || binding.Awaiting.Count != 0 || binding.Answers.Count != 0 || !binding.Blocker.IsNone)
                || binding.Decisions > 0 && (binding.Last < 0 || binding.Last >= _world.Tick || binding.Last > long.MaxValue - binding.Cadence
                    || binding.Decisions > binding.Last + 1 || binding.Next != binding.Last + binding.Cadence)) throw new CorruptStateException("invalid controller cadence state");
            var sequences = new HashSet<int>();
            foreach (int sequence in binding.Awaiting)
                if (sequence <= 0 || !sequences.Add(sequence) || !_world.IsPendingCommand(binding.Company, CommandStream, sequence))
                    throw new CorruptStateException("invalid/orphan controller correlation ticket");
            foreach (CommandResult answer in binding.Answers)
                if (answer.Player != binding.Company || answer.Stream != CommandStream || answer.Sequence <= 0 || !sequences.Add(answer.Sequence)
                    || answer.Tick < 0 || answer.Tick >= _world.Tick || answer.Accepted != answer.Refusal.IsNone
                    || answer.Command == null || !_world.Commands.TryGet(answer.Command, out _)
                    || !_world.WasCommandIssued(binding.Company, CommandStream, answer.Sequence) || _world.IsPendingCommand(binding.Company, CommandStream, answer.Sequence))
                    throw new CorruptStateException("invalid controller command answer");
        }
    }
}
