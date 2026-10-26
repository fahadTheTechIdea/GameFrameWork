using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>One pacing/reservation/chronology owner. Gameplay, commands and responses keep their actual authorities.</summary>
    internal sealed class ScenarioPressureSystem : GameSystem, IScenarioPressure, ICompositionValidator
    {
        private static readonly Code CommandStream = new Code(ScenarioPressureModule.ModuleId);
        private sealed class Binding
        {
            public PlayerId Company;
            public PressureProfileSettings Profile;
            public Fix Available, Reserved, Spent;
            public long Decisions, Accepted, Refused, Last = -1, Next, Refill, CooldownUntil, Settled = -1;
            public int Pending;
            public long Issued = -1;
            public string Source, Command;
            public Code Blocker;
        }
        private readonly struct Decision
        {
            public Decision(Binding binding, PressureSourceSettings source, Command command, Code blocker)
            { Binding = binding; Source = source; Command = command; Blocker = blocker; }
            public Binding Binding { get; }
            public PressureSourceSettings Source { get; }
            public Command Command { get; }
            public Code Blocker { get; }
        }
        private readonly World _world;
        private readonly List<Binding> _bindings = new List<Binding>();
        public override string Id => ScenarioPressureModule.ModuleId;
        public override int StateSchemaVersion => 2;
        public ScenarioPressureSystem(World world, IReadOnlyDictionary<int, IScenarioPressureProfile> profiles)
        {
            _world = world;
            foreach (var profile in profiles)
            {
                PressureProfileSettings settings = PressureContentContracts.Read(world, profile.Value);
                _bindings.Add(new Binding { Company = new PlayerId(profile.Key), Profile = settings,
                    Available = settings.Initial });
            }
            _bindings.Sort((a, b) => a.Company.Value.CompareTo(b.Company.Value));
        }
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ContentException("foreign pressure session");
            foreach (IPressureSource source in world.Content.All<IPressureSource>()) PressureContentContracts.Read(world, source);
            foreach (IScenarioPressureProfile profile in world.Content.All<IScenarioPressureProfile>()) PressureContentContracts.Read(world, profile);
            foreach (Binding binding in _bindings)
            {
                if (!world.Players.IsSeat(binding.Company) || !world.Content.Owns(binding.Profile.Definition) || binding.Profile.Cadence <= 0 || binding.Profile.Period <= 0)
                    throw new ContentException("pressure binding needs actual company/profile and positive session durations");
                foreach (PressureSourceSettings source in binding.Profile.Sources)
                {
                    EntityObservation.ReadProvider(world, source.Definition, null, () => {
                        source.Definition.ValidateBinding(world, binding.Company); return true;
                    }, space: world.Space);
                }
            }
        }
        public void CopyPressure(PlayerId viewer, IList<ScenarioPressureSnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination)); destination.Clear();
            _world.RequireHealthyUpdate();
            if (viewer != PlayerId.None && !_world.Players.IsSeat(viewer)) return;
            foreach (Binding b in _bindings)
                if (viewer == PlayerId.None || viewer == b.Company)
                    destination.Add(new ScenarioPressureSnapshot(b.Company, b.Profile.Definition.Id, b.Available, b.Reserved, b.Spent,
                        b.Decisions, b.Accepted, b.Refused, b.Last, b.Next, b.Refill, b.CooldownUntil, b.Pending, b.Source, b.Blocker));
        }
        public bool CopyCandidates(PlayerId viewer, PlayerId company, string source, IList<PressureCandidateSnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination)); destination.Clear();
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company) || viewer != PlayerId.None && (viewer != company || !_world.Players.IsSeat(viewer))) return false;
            foreach (Binding binding in _bindings)
                if (binding.Company == company)
                    foreach (PressureSourceSettings settings in binding.Profile.Sources)
                        if (settings.Definition.Id == source && settings.Definition is IPressureSourceCandidates provider)
                        {
                            var facts = EntityObservation.ReadProvider(_world, settings.Definition, null, () => {
                                var observed = new List<PressureCandidateSnapshot>();
                                provider.CopyCandidates(new PressureContext(_world, company), observed);
                                EntityId previous = EntityId.None;
                                foreach (var fact in observed)
                                {
                                    if (fact == null || !fact.Entity.IsValid || previous.IsValid && previous.CompareTo(fact.Entity) >= 0 ||
                                        !_world.ViewFor(company).TryGetEntity(fact.Entity, out var entity) || entity.Owner != company)
                                        throw new InvalidOperationException("pressure source returned unordered/hidden/foreign candidates");
                                    previous = fact.Entity;
                                }
                                return observed;
                            }, space: _world.Space);
                            foreach (var fact in facts) destination.Add(fact);
                            return true;
                        }
            return false;
        }
        public override void Update(World world, TickInfo tick)
        {
            // Settle all actual replies/refills before observing any private source; actions remain queued for NEXT Update.
            foreach (Binding binding in _bindings) { SettleAnswer(binding, tick.Tick); Replenish(binding, tick.Tick); }
            if (!world.Outcome.IsNone) return;
            var decisions = new List<Decision>();
            foreach (Binding binding in _bindings)
            {
                if (binding.Pending != 0) { binding.Blocker = PressureCodes.Awaiting; continue; }
                if (tick.Tick < binding.CooldownUntil) { binding.Blocker = PressureCodes.Cooldown; continue; }
                if (tick.Tick < binding.Next) continue;
                decisions.Add(Evaluate(binding));
            }
            foreach (Decision decision in decisions)
            {
                Binding binding = decision.Binding;
                long count = checked(binding.Decisions + 1), next = checked(tick.Tick + binding.Profile.Cadence);
                if (decision.Command != null)
                {
                    Fix available = binding.Available - decision.Source.Cost;
                    _world.Commands.TryGet(decision.Command.GetType(), out CommandRegistry.Entry entry);
                    int sequence = world.Submit(binding.Company, decision.Command, CommandStream);
                    binding.Available = available; binding.Reserved = decision.Source.Cost;
                    binding.Pending = sequence; binding.Source = decision.Source.Definition.Id; binding.Command = entry.Id;
                    binding.Issued = tick.Tick; binding.Blocker = PressureCodes.Awaiting;
                }
                else binding.Blocker = decision.Blocker;
                binding.Decisions = count; binding.Last = tick.Tick; binding.Next = next;
            }
        }
        private Decision Evaluate(Binding binding)
        {
            var context = new PressureContext(_world, binding.Company); Code blocker = Code.None;
            foreach (PressureSourceSettings settings in binding.Profile.Sources)
            {
                IPressureSource source = settings.Definition;
                PressureObservation observation = EntityObservation.ReadProvider(_world, source, null,
                    () => source.Evaluate(context), space: _world.Space);
                if (observation.Command == null)
                {
                    if (observation.Blocker.IsNone) throw new InvalidOperationException(source.Id + ": malformed pressure wait");
                    if (blocker.IsNone) blocker = observation.Blocker;
                    continue;
                }
                Command command = observation.Command;
                if (!observation.Blocker.IsNone || !command.Stream.IsNone || command.Sequence != 0 || command.IssuedTick != 0 || command.Player != PlayerId.None)
                    throw new InvalidOperationException(source.Id + ": pressure must propose a fresh unstamped command");
                Code refusal = _world.Check(binding.Company, command);
                if (refusal.IsNone && settings.Cost > binding.Available) refusal = PressureCodes.Budget;
                if (!refusal.IsNone) { if (blocker.IsNone) blocker = refusal; continue; }
                return new Decision(binding, settings, command, Code.None);
            }
            return new Decision(binding, default, null, blocker);
        }
        private void SettleAnswer(Binding binding, long now)
        {
            if (binding.Pending == 0) return;
            foreach (CommandResult result in _world.LastTickResults)
            {
                if (result.Player != binding.Company || result.Stream != CommandStream || result.Sequence != binding.Pending) continue;
                if (result.Command != binding.Command || result.Tick != now || result.Accepted != result.Refusal.IsNone)
                    throw new InvalidOperationException("pressure correlation received mismatched actual result");
                if (result.Accepted)
                {
                    Fix spent = binding.Spent + binding.Reserved;
                    long accepted = checked(binding.Accepted + 1), cooldown = checked(now + binding.Profile.Cooldown);
                    binding.Spent = spent; binding.Accepted = accepted; binding.CooldownUntil = cooldown;
                    binding.Blocker = binding.Profile.Cooldown == 0 ? Code.None : PressureCodes.Cooldown;
                }
                else
                {
                    Fix available = binding.Available + binding.Reserved; long refused = checked(binding.Refused + 1);
                    binding.Available = available; binding.Refused = refused; binding.Blocker = result.Refusal;
                }
                binding.Settled = now; binding.Pending = 0; binding.Reserved = Fix.Zero;
                binding.Source = binding.Command = null; binding.Issued = -1;
                return;
            }
        }
        private static void Replenish(Binding binding, long now)
        {
            long periods = (now - binding.Refill) / binding.Profile.Period;
            if (periods <= 0) return;
            long room = binding.Profile.Capacity.Raw - binding.Reserved.Raw - binding.Available.Raw;
            long gain = (long)BigInteger.Min(new BigInteger(room), new BigInteger(periods) * binding.Profile.Refill.Raw);
            binding.Available = Fix.FromRaw(checked(binding.Available.Raw + gain));
            binding.Refill = checked(binding.Refill + checked(periods * binding.Profile.Period));
        }
        private void ValidateState(Binding binding)
        {
            if (binding.Available < Fix.Zero || binding.Reserved < Fix.Zero || binding.Spent < Fix.Zero ||
                (BigInteger)binding.Available.Raw + binding.Reserved.Raw > binding.Profile.Capacity.Raw ||
                binding.Decisions < 0 || binding.Accepted < 0 || binding.Refused < 0 || binding.Pending < 0 ||
                (BigInteger)binding.Accepted + binding.Refused + (binding.Pending == 0 ? 0 : 1) > binding.Decisions ||
                binding.Last < -1 || binding.Next < 0 || binding.Refill < 0 || binding.Refill > _world.Tick || binding.Refill % binding.Profile.Period != 0 ||
                binding.Settled < -1 || binding.Settled >= _world.Tick || binding.CooldownUntil < 0 ||
                binding.Decisions == 0 && (binding.Last != -1 || binding.Next != 0 || binding.Available != binding.Profile.Initial ||
                    binding.Pending != 0 || binding.Spent != Fix.Zero || binding.Settled != -1 || !binding.Blocker.IsNone) ||
                binding.Decisions > 0 && (binding.Last < 0 || binding.Last >= _world.Tick || binding.Decisions > binding.Last + 1 ||
                    binding.Decisions > binding.Last / binding.Profile.Cadence + 1 ||
                    binding.Last > long.MaxValue - binding.Profile.Cadence || binding.Next != binding.Last + binding.Profile.Cadence) ||
                binding.Accepted == 0 && (binding.Spent != Fix.Zero || binding.CooldownUntil != 0) ||
                binding.Accepted > 0 && binding.Spent <= Fix.Zero ||
                (BigInteger)binding.Spent.Raw > (BigInteger)binding.Accepted * binding.Profile.Capacity.Raw ||
                (binding.Accepted + (BigInteger)binding.Refused == 0) != (binding.Settled == -1))
                throw new CorruptStateException("invalid pressure budget/cadence chronology");
            // Necessary history invariants derive from the actual authored sources, not a second ledger.
            long firstCost = binding.Profile.Sources[0].Cost.Raw, minimumCost = firstCost, maximumCost = firstCost;
            BigInteger stride = BigInteger.Zero;
            foreach (PressureSourceSettings costSource in binding.Profile.Sources)
            {
                minimumCost = Math.Min(minimumCost, costSource.Cost.Raw); maximumCost = Math.Max(maximumCost, costSource.Cost.Raw);
                stride = BigInteger.GreatestCommonDivisor(stride, (BigInteger)costSource.Cost.Raw - firstCost);
            }
            BigInteger spent = binding.Spent.Raw, accepted = binding.Accepted;
            if (spent < accepted * minimumCost || spent > accepted * maximumCost ||
                !stride.IsZero && (spent - accepted * firstCost) % stride != 0 ||
                (BigInteger)binding.Available.Raw + binding.Reserved.Raw + spent > binding.Profile.Initial.Raw +
                    (BigInteger)(binding.Refill / binding.Profile.Period) * binding.Profile.Refill.Raw)
                throw new CorruptStateException("invalid pressure funding/source cost history");
            long lastAccepted = binding.CooldownUntil - binding.Profile.Cooldown;
            if (binding.Settled == 0 || binding.Accepted > 0 && (lastAccepted <= 0 || lastAccepted > binding.Settled ||
                binding.Refused == 0 && lastAccepted != binding.Settled))
                throw new CorruptStateException("invalid pressure acceptance/cooldown chronology");
            // Completed checkpoints have processed every elapsed refill period. In-flight observation
            // remains governed by the existing phase guards, not an invented second notion of time.
            if (!_world.Stepping && binding.Refill != ((_world.Tick == 0 ? 0 : _world.Tick - 1) / binding.Profile.Period) * binding.Profile.Period)
                throw new CorruptStateException("invalid pressure completed refill chronology");
            if (binding.Pending == 0)
            {
                if (binding.Reserved != Fix.Zero || binding.Source != null || binding.Command != null || binding.Issued != -1)
                    throw new CorruptStateException("pressure reservation lacks a pending command");
                return;
            }
            PressureSourceSettings source = default;
            foreach (PressureSourceSettings candidate in binding.Profile.Sources) if (candidate.Definition.Id == binding.Source) source = candidate;
            if (source.Definition == null || binding.Reserved != source.Cost || binding.Issued != binding.Last ||
                !_world.WasCommandIssued(binding.Company, CommandStream, binding.Pending) ||
                !_world.PendingCommandMatches(binding.Company, CommandStream, binding.Pending, binding.Command, binding.Issued))
                throw new CorruptStateException("invalid/orphan pressure command reservation");
        }
        public override void Sync(IStateSync sync)
        {
            if (sync.Count(_bindings.Count) != _bindings.Count) throw new CorruptStateException("pressure company binding count differs");
            foreach (Binding binding in _bindings)
            {
                PlayerId company = binding.Company; string profile = binding.Profile.Definition.Id;
                sync.Player(ref company); sync.String(ref profile);
                if (company != binding.Company || profile != binding.Profile.Definition.Id) throw new CorruptStateException("pressure binding identity differs");
                sync.Fix(ref binding.Available); sync.Fix(ref binding.Reserved); sync.Fix(ref binding.Spent);
                sync.Long(ref binding.Decisions); sync.Long(ref binding.Accepted); sync.Long(ref binding.Refused);
                sync.Long(ref binding.Last); sync.Long(ref binding.Next); sync.Long(ref binding.Refill);
                sync.Long(ref binding.CooldownUntil); sync.Long(ref binding.Settled);
                sync.Int(ref binding.Pending); sync.Long(ref binding.Issued); sync.String(ref binding.Source); sync.String(ref binding.Command);
                string blocker = binding.Blocker.Id; sync.String(ref blocker);
                if (blocker != null)
                    try { ContractIdentity.ValidateId(blocker, "pressure blocker"); }
                    catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
                if (sync.Reading) binding.Blocker = blocker == null ? Code.None : new Code(blocker);
                ValidateState(binding);
            }
        }
        public override void OnLoaded(World world) { foreach (Binding binding in _bindings) ValidateState(binding); }
    }
}
