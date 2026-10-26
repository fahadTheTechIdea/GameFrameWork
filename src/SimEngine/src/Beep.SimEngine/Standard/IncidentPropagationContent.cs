using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Sole explicit propagation policy declaration; no live scans, episodes or timing state.</summary>
    public sealed class IncidentPropagationPolicyDeclaration
    {
        public GameDuration Interval { get; }
        public GameDuration Delay { get; }
        public Code TargetSource { get; }
        public Code TargetKind { get; }
        public bool CrossCompany { get; }
        public IReadOnlyList<SignalGateCondition> SourceConditions { get; }
        public IReadOnlyList<SignalGateCondition> TargetConditions { get; }
        public IncidentWeatherSelectionDeclaration Weather { get; }
        private bool _resolved;
        private IncidentPropagationPolicyDeclaration(JsonReader reader)
        {
            Interval = reader.Child("interval")?.RationalDuration() ?? throw reader.Error("interval required");
            Delay = reader.Child("delay")?.RationalDuration() ?? throw reader.Error("delay required");
            if (Interval.IsZero || Delay.IsZero) throw reader.Error("propagation interval/delay must be positive");
            string targetSource = reader.String("target_source"), targetKind = reader.String("target_kind");
            try { ContractIdentity.ValidateId(targetSource, "propagation target source"); ContractIdentity.ValidateId(targetKind, "propagation target kind"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            TargetSource = new Code(targetSource); TargetKind = new Code(targetKind);
            string scope = reader.String("companies");
            if (scope != "same" && scope != "any") throw reader.Error("explicit companies must be same or any");
            CrossCompany = scope == "any";
            SourceConditions = SignalGateCondition.ReadAll(reader, "source_conditions", true);
            TargetConditions = SignalGateCondition.ReadAll(reader, "target_conditions", true);
            if (reader.Has("weather")) Weather = IncidentWeatherSelectionDeclaration.Read(reader.Child("weather")
                ?? throw reader.Error("weather selection must be an explicit object"));
        }
        public static IncidentPropagationPolicyDeclaration Read(JsonReader reader) => new IncidentPropagationPolicyDeclaration(reader);
        public void Resolve(ContentResolver content)
        {
            if (_resolved) throw new InvalidOperationException("propagation policy already resolved");
            foreach (var condition in SourceConditions) condition.Resolve(content);
            foreach (var condition in TargetConditions) condition.Resolve(content);
            Weather?.Resolve(content);
            _resolved = true;
        }
        public void ValidateSource(EntityDef source)
        { foreach (var condition in SourceConditions) condition.Validate(source); }
        public void ValidateBinding(World world, string id)
        {
            IncidentPropagationContracts.ValidateDurations(world, Interval, Delay, id);
            Weather?.ValidateBinding(world);
            bool found = false;
            foreach (EntityDef target in world.Content.All<EntityDef>())
            {
                var incident = target.Component<IPhasedIncidentDefinition>(TargetSource);
                if (incident != null && incident.Workflow.Kind == TargetKind)
                {
                    if (!(incident is ComponentDef part) || !typeof(IPhasedIncidentResponse).IsAssignableFrom(part.ComponentType))
                        throw new ContentException(id + ": target metadata requires an actual phased incident capability");
                    found = true;
                    foreach (var condition in TargetConditions) condition.Validate(target);
                }
            }
            if (!found) throw new ContentException(id + ": no declared target supports the exact source/kind binding");
        }
        public Code CheckConditions(IncidentPropagationContext context, Entity target, string id)
        {
            var source = SignalGateCondition.ReadGate(context.World, context.Source, new Code(id), true, SourceConditions);
            if (!source.Blocker.IsNone) return source.Blocker;
            Code blocker = SignalGateCondition.ReadGate(context.World, target, new Code(id), true, TargetConditions).Blocker;
            return !blocker.IsNone ? blocker : Weather?.Check(context) ?? Code.None;
        }
    }

    /// <summary>Selected actual weather policy shared by tile/network rules; no weather/exposure state.</summary>
    public sealed class IncidentWeatherSelectionDeclaration
    {
        private readonly string _provider;
        public IWeatherDefinition Provider { get; private set; }
        public IReadOnlyList<Code> Conditions { get; }
        public Code Blocker { get; }
        private IncidentWeatherSelectionDeclaration(JsonReader reader)
        {
            _provider = reader.String("provider"); string blocker = reader.String("blocker");
            if (reader.Array("conditions") == null) throw reader.Error("explicit weather conditions required");
            string[] ids = reader.Strings("conditions");
            var seen = new HashSet<string>(StringComparer.Ordinal); var conditions = new List<Code>();
            try
            {
                ContractIdentity.ValidateId(_provider, "weather provider"); ContractIdentity.ValidateId(blocker, "weather blocker");
                foreach (string id in ids)
                {
                    ContractIdentity.ValidateId(id, "selected weather condition");
                    if (!seen.Add(id)) throw reader.Error("duplicate selected weather condition");
                    conditions.Add(new Code(id));
                }
            }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            if (conditions.Count == 0) throw reader.Error("weather selection must be nonempty");
            Conditions = conditions.AsReadOnly(); Blocker = new Code(blocker); reader.End();
        }
        public static IncidentWeatherSelectionDeclaration Read(JsonReader reader) => new IncidentWeatherSelectionDeclaration(reader);
        public void Resolve(ContentResolver content)
        {
            if (Provider != null) throw new InvalidOperationException("weather selection resolves once");
            Provider = content.Def<IWeatherDefinition>(_provider);
        }
        public void ValidateBinding(World world)
        {
            if (!world.Services.TryGet<IWeatherReader>(out _)) throw new ContentException("selected incident weather requires its actual reader");
            WeatherContracts.ValidateDefinition(world, Provider);
            EntityObservation.ReadProvider(world, Provider, null, () => {
                foreach (var selected in Conditions)
                {
                    bool found = false; foreach (var declared in Provider.ConditionIds) if (selected == declared) found = true;
                    if (!found) throw new ContentException("incident weather condition is undeclared: " + selected);
                }
                return true;
            });
        }
        public Code Check(IncidentPropagationContext context)
        {
            Code refusal = context.World.Services.Get<IWeatherReader>().Read(Provider.Id, context.Source.Owner, out var snapshot);
            if (!refusal.IsNone) return refusal;
            if (snapshot == null || snapshot.Provider != Provider.Id || snapshot.ObservedUpdate != context.World.Tick)
                throw new InvalidOperationException("weather reader returned stale or foreign facts");
            var observation = snapshot.Observation;
            if (!observation.Blocker.IsNone) return observation.Blocker;
            foreach (var condition in Conditions) if (condition == observation.Condition) return Code.None;
            return Blocker;
        }
    }

    public sealed class IncidentPropagationBindingDeclaration
    {
        private readonly string _rule;
        public Code CapabilityId { get; }
        public Code Source { get; }
        public IIncidentPropagationRule Rule { get; private set; }
        private IncidentPropagationBindingDeclaration(Code id, Code source, string rule)
        { CapabilityId = id; Source = source; _rule = rule; }
        public static IncidentPropagationBindingDeclaration Read(JsonReader reader)
        {
            string id = reader.String("id"), source = reader.String("source"), rule = reader.String("rule");
            try { foreach (var value in new[] { id, source, rule }) ContractIdentity.ValidateId(value, "propagation binding"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            return new IncidentPropagationBindingDeclaration(new Code(id), new Code(source), rule);
        }
        public void Resolve(ContentResolver content)
        {
            if (Rule != null) throw new InvalidOperationException("propagation binding already resolved");
            Rule = content.Def<IIncidentPropagationRule>(_rule);
        }
        public void ValidateOwner(EntityDef owner)
        {
            var source = owner.Component<IPhasedIncidentDefinition>(Source) as ComponentDef;
            if (source == null || !typeof(IPhasedIncidentResponse).IsAssignableFrom(source.ComponentType))
                throw new ContentException(owner.Id + ": propagation requires its exact named phased incident source");
            Rule.ValidateSource(owner);
        }
    }

    /// <summary>Actual contract aliases and scoped observations, never a second propagation catalog.</summary>
    public static class IncidentPropagationContracts
    {
        internal static void ValidateDurations(World world, GameDuration interval, GameDuration delay, string id)
        {
            try
            {
                if (world.Time.ExactTicks(interval) <= 0 || world.Time.ExactTicks(delay) <= 0)
                    throw new ContentException(id + ": positive exact session durations required");
            }
            catch (Exception error) when (error is ArgumentException || error is OverflowException)
            { throw new ContentException(id + ": durations do not fit the exact session quantum: " + error.Message); }
        }
        public static void ValidateRule(World world, IIncidentPropagationRule rule)
        {
            if (rule == null) throw new ContentException("propagation requires an explicit actual rule");
            EntityObservation.ReadProvider(world, rule, null, () => {
                if (!(rule is Def actual) || !actual.IsFrozen || !world.Content.Owns(rule) ||
                    !ReferenceEquals(world.Content.Find<IIncidentPropagationRule>(rule.Id), rule))
                    throw new ContentException("propagation requires its actual immutable rule alias");
                ValidateDurations(world, rule.Interval, rule.Delay, rule.Id);
                if (rule.TargetSource.IsNone || rule.TargetKind.IsNone) throw new ContentException("propagation requires explicit target source/kind");
                _=ReadExposureEffect(world,rule);
                return true;
            });
        }
        public static IIncidentExposureEffect ReadExposureEffect(World world,IIncidentPropagationRule rule)
        {
            return EntityObservation.ReadProvider(world,rule,null,()=>{
                var effect=(rule as IIncidentExposureEffectRule)?.ExposureEffect;if(effect==null)return null;
                if(effect is not Def actual||!actual.IsFrozen||!world.Content.Owns(effect)||
                    !ReferenceEquals(world.Content.Find<IIncidentExposureEffect>(effect.Id),effect))
                    throw new ContentException("exposure effect must alias actual immutable content");
                effect.RequireCapabilities(world);return effect;
            },world.Space);
        }
        public static void ValidateBinding(World world, IIncidentPropagationDefinition definition)
            => ReadBinding(world, definition, null);
        private static IIncidentPropagationRule ReadBinding(World world, IIncidentPropagationDefinition definition, Entity entity)
            => EntityObservation.ReadProvider(world, definition, entity, () => {
                if (!(definition is ComponentDef part) || !part.IsFrozen || !world.Content.Owns(part.Owner) ||
                    !ReferenceEquals(part.Owner.Component<IIncidentPropagationDefinition>(definition.CapabilityId), definition) ||
                    !typeof(IIncidentPropagationEmitter).IsAssignableFrom(part.ComponentType) || entity != null && !ReferenceEquals(entity.Def, part.Owner))
                    throw new ContentException("propagation requires its actual immutable emitter binding");
                var source = part.Owner.Component<IPhasedIncidentDefinition>(definition.Source);
                if (!(source is ComponentDef sourcePart) || !typeof(IPhasedIncidentResponse).IsAssignableFrom(sourcePart.ComponentType))
                    throw new ContentException("propagation requires its actual named phased source");
                ValidateRule(world, definition.Rule); return definition.Rule;
            });
        internal static IIncidentPropagationRule ReadEmitter(World world, IIncidentPropagationEmitter emitter, Entity entity)
            => EntityObservation.ReadProvider(world, emitter, entity, () => {
                var definition = entity.Def.Component<IIncidentPropagationDefinition>(emitter.CapabilityId);
                if (definition == null || !ReferenceEquals(emitter.Entity, entity) ||
                    !ReferenceEquals(entity.Get<IIncidentPropagationEmitter>(emitter.CapabilityId), emitter) ||
                    emitter.Source != definition.Source || !ReferenceEquals(emitter.Rule, definition.Rule))
                    throw new InvalidOperationException("propagation emitter differs from actual named authored binding");
                var sourceScope = EntityObservation.CaptureProvider(world, entity, entity.Sync, "incident.propagation.metadata");
                try { return ReadBinding(world, definition, entity); } finally { sourceScope.Verify(); }
            });
        internal static void ValidateComposition(World world)
        {
            foreach (var rule in world.Content.All<IIncidentPropagationRule>())
                EntityObservation.ReadProvider(world, rule, null, () => { ValidateRule(world, rule); rule.ValidateBinding(world); ValidateRule(world, rule); return true; });
            foreach (var owner in world.Content.All<EntityDef>())
                foreach (var part in owner.Components)
                {
                    if (part.ComponentType == typeof(IncidentPropagationEmitter) && !(part is IIncidentPropagationDefinition))
                        throw new ContentException(owner.Id + ": reusable propagation emitter requires its metadata contract");
                    if (part is IIncidentPropagationDefinition definition)
                        EntityObservation.ReadProvider(world, part, null, () => {
                            var rule = ReadBinding(world, definition, null);
                            rule.ValidateSource(owner); definition.ValidateBinding(world);
                            ReadBinding(world, definition, null); return true;
                        });
                }
        }
    }
}
