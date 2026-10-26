using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Actual immutable weather content, not a second clock or propagation owner.</summary>
    public interface IWeatherDefinition : IContentDefinition
    {
        IReadOnlyList<Code> ConditionIds { get; }
        void ValidateBinding(World world);
        WeatherObservation Observe(WeatherContext context);
    }
    public readonly struct WeatherContext
    {
        public World World { get; }
        public ISimulationTime Time => World.Time;
        public long Update { get; }
        internal WeatherContext(World world) { World = world; Update = world.Tick; }
    }
    public readonly struct WeatherObservation
    {
        public Code Phase { get; }
        public Code Condition { get; }
        public long ElapsedSteps { get; }
        public long RemainingSteps { get; }
        public Code Blocker { get; }
        public WeatherObservation(Code phase, Code condition, long elapsedSteps, long remainingSteps, Code blocker)
        { Phase = phase; Condition = condition; ElapsedSteps = elapsedSteps; RemainingSteps = remainingSteps; Blocker = blocker; }
    }
    public sealed class WeatherSnapshot
    {
        public string Provider { get; }
        public long ObservedUpdate { get; }
        public WeatherObservation Observation { get; }
        public GameDuration PhaseElapsed { get; }
        public GameDuration PhaseRemaining { get; }
        internal WeatherSnapshot(string provider, long update, WeatherObservation observation, ISimulationTime time)
        { Provider = provider; ObservedUpdate = update; Observation = observation;
            PhaseElapsed = GameDuration.FromSteps(observation.ElapsedSteps, time);
            PhaseRemaining = GameDuration.FromSteps(observation.RemainingSteps, time); }
    }
    public interface IWeatherReader
    {
        Code Read(string provider, PlayerId viewer, out WeatherSnapshot snapshot);
    }
    /// <summary>Sole ordered phase authoring/interpretation, reusable by independent metadata.</summary>
    public sealed class WeatherTimelineDeclaration
    {
        private readonly struct Phase
        {
            internal readonly Code Id, Condition;
            internal readonly GameDuration Duration;
            internal Phase(Code id, Code condition, GameDuration duration) { Id = id; Condition = condition; Duration = duration; }
        }
        private readonly IReadOnlyList<Phase> _phases;
        public IReadOnlyList<Code> ConditionIds { get; }
        public bool Repeat { get; }
        public static readonly Code Exhausted = new Code("weather.exhausted");
        private WeatherTimelineDeclaration(JsonReader reader)
        {
            Repeat = reader.Bool("repeat");
            var rows = reader.Array("phases") ?? throw reader.Error("explicit weather phases required");
            if (rows.Count == 0) throw reader.Error("weather timeline must be nonempty");
            var phases = new List<Phase>(); var ids = new HashSet<string>(StringComparer.Ordinal);
            var conditions = new SortedSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = new JsonReader(rows[i], "weather.phases[" + i + "]");
                string id = row.String("id"), condition = row.String("condition");
                try { ContractIdentity.ValidateId(id, "weather phase"); ContractIdentity.ValidateId(condition, "weather condition"); }
                catch (ArgumentException error) { throw row.Error(error.Message); }
                if (!ids.Add(id)) throw row.Error("duplicate weather phase");
                GameDuration duration = row.Child("duration")?.RationalDuration() ?? throw row.Error("weather phase duration required");
                if (duration.IsZero) throw row.Error("weather duration must be positive");
                row.End(); conditions.Add(condition); phases.Add(new Phase(new Code(id), new Code(condition), duration));
            }
            _phases = phases.AsReadOnly(); var codes = new List<Code>();
            foreach (string id in conditions) codes.Add(new Code(id)); ConditionIds = codes.AsReadOnly();
        }
        public static WeatherTimelineDeclaration Read(JsonReader reader)
            => new WeatherTimelineDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void ValidateBinding(World world)
        {
            try { _ = Total(world.Time); }
            catch (Exception error) when (error is ArgumentException || error is OverflowException)
            { throw new ContentException("weather durations must fit exact session time: " + error.Message); }
        }
        private long Total(ISimulationTime time)
        {
            long total = 0;
            foreach (var phase in _phases)
            {
                long steps = time.ExactTicks(phase.Duration);
                if (steps <= 0) throw new ArgumentException("positive exact weather duration required");
                total = checked(total + steps);
            }
            return total;
        }
        public WeatherObservation Observe(WeatherContext context)
        {
            long total = Total(context.Time), at = context.Update;
            if (at < 0) throw new InvalidOperationException("weather requires actual nonnegative game time");
            if (Repeat) at %= total;
            else if (at >= total) return new WeatherObservation(Code.None, Code.None, 0, 0, Exhausted);
            foreach (var phase in _phases)
            {
                long steps = context.Time.ExactTicks(phase.Duration);
                if (at < steps) return new WeatherObservation(phase.Id, phase.Condition, at, steps - at, Code.None);
                at -= steps;
            }
            throw new InvalidOperationException("weather timeline has no current phase");
        }
    }
    public sealed class WeatherTimelineDef : Def, IWeatherDefinition
    {
        private WeatherTimelineDeclaration _declaration;
        public IReadOnlyList<Code> ConditionIds => _declaration.ConditionIds;
        protected override void ReadCore(JsonReader reader) => _declaration = WeatherTimelineDeclaration.Read(reader);
        public void ValidateBinding(World world) => _declaration.ValidateBinding(world);
        public WeatherObservation Observe(WeatherContext context) => _declaration.Observe(context);
    }
    public static class WeatherContracts
    {
        public static void ValidateDefinition(World world, IWeatherDefinition definition)
        {
            EntityObservation.ReadProvider(world, definition, null, () => {
                RequireDefinition(world, definition); definition.ValidateBinding(world); RequireDefinition(world, definition); return true;
            }, world.Space);
        }
        private static void RequireDefinition(World world, IWeatherDefinition definition)
        {
            if (!(definition is Def actual) || !actual.IsFrozen || !world.Content.Owns(definition) ||
                !ReferenceEquals(world.Content.Find<IWeatherDefinition>(definition.Id), definition))
                throw new ContentException("weather requires its actual immutable content alias");
            var ids = definition.ConditionIds;
            if (ids == null || ids.Count == 0) throw new ContentException(definition.Id + ": declared weather conditions required");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids)
            {
                try { ContractIdentity.ValidateId(id.Id, "weather condition"); }
                catch (ArgumentException error) { throw new ContentException(definition.Id + ": " + error.Message); }
                if (!seen.Add(id.Id)) throw new ContentException(definition.Id + ": duplicate weather condition");
            }
        }
        internal static WeatherSnapshot Observe(World world, IWeatherDefinition definition)
            => EntityObservation.ReadProvider(world, definition, null, () => {
                RequireDefinition(world, definition);
                var context = new WeatherContext(world); var result = definition.Observe(context);
                if (result.Blocker.IsNone)
                {
                    bool known = false; foreach (var condition in definition.ConditionIds) if (condition == result.Condition) known = true;
                    if (!known || result.Phase.IsNone || result.ElapsedSteps < 0 || result.RemainingSteps <= 0)
                        throw new InvalidOperationException("weather provider returned invalid current phase facts");
                    ContractIdentity.ValidateId(result.Phase.Id, "weather observed phase");
                }
                else
                {
                    ContractIdentity.ValidateId(result.Blocker.Id, "weather blocker");
                    if (!result.Phase.IsNone || !result.Condition.IsNone || result.ElapsedSteps != 0 || result.RemainingSteps != 0)
                        throw new InvalidOperationException("refused weather cannot publish substituted phase facts");
                }
                RequireDefinition(world, definition);
                return new WeatherSnapshot(definition.Id, context.Update, result, world.Time);
            }, world.Space);
    }
    /// <summary>One pure observed read boundary; no weather state, cursor, codec or game clock.</summary>
    internal sealed class WeatherReader : IWeatherReader, ICompositionValidator
    {
        private readonly World _world;
        private bool _reading;
        internal WeatherReader(World world) { _world = world; }
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ContentException("foreign weather composition");
            foreach (var definition in world.Content.All<IWeatherDefinition>()) WeatherContracts.ValidateDefinition(world, definition);
        }
        public Code Read(string provider, PlayerId viewer, out WeatherSnapshot snapshot)
        {
            snapshot = null; _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(viewer)) return Codes.NotSeated;
            var definition = _world.Content.Find<IWeatherDefinition>(provider);
            if (definition == null || !_world.ViewFor(viewer).CanSeeContent(definition)) return Codes.UnknownDef;
            try
            {
                if (_reading) throw new InvalidOperationException("recursive weather observation");
                _reading = true;
                var value = WeatherContracts.Observe(_world, definition); _world.RequireHealthyUpdate();
                snapshot = value; return Code.None;
            }
            catch (Exception error) { _world.LatchFault("weather.observation", error); throw; }
            finally { _reading = false; }
        }
    }
}
