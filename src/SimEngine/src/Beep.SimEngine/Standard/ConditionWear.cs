using System;
using System.Numerics;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IConditionWearDefinition : IConditionDefinition
    {
        IConditionWearRule Wear { get; }
    }
    public interface IConditionWearProvider : IConditionChange
    {
        ConditionExposureSnapshot ReadExposure();
        void ApplyWear(ConditionWearContext context, ConditionWearDecision decision);
    }
    public interface IConditionWearRule : IContentDefinition
    {
        IUnitDefinition Unit { get; }
        void ValidateBinding(World world, EntityDef owner);
        ConditionWearDecision Observe(ConditionWearContext context);
    }
    public readonly struct ConditionExposureSnapshot
    {
        public long ElapsedTicks { get; }
        public long UpdatedTick { get; }
        public ConditionExposureSnapshot(long elapsedTicks, long updatedTick) { ElapsedTicks = elapsedTicks; UpdatedTick = updatedTick; }
    }
    public readonly struct ConditionWearDecision
    {
        public bool Advance { get; }
        public Fix Damage { get; }
        public Code Blocker { get; }
        public ConditionWearDecision(bool advance, Fix damage, Code blocker)
        {
            if (advance != blocker.IsNone || damage < Fix.Zero || !advance && damage != Fix.Zero)
                throw new ArgumentException("wear needs nonnegative exact damage and explicit eligibility/blocker");
            Advance = advance; Damage = damage; Blocker = blocker;
        }
        public static ConditionWearDecision Refused(Code blocker) => new ConditionWearDecision(false, Fix.Zero, blocker);
    }
    /// <summary>Engine-issued admission over one actual provider before the complete pass mutates anything.</summary>
    public readonly struct ConditionWearContext
    {
        public World World { get; }
        public Entity Entity { get; }
        public Code Source { get; }
        public PlayerId Company { get; }
        public long Tick { get; }
        public ConditionSnapshot Condition { get; }
        public ConditionExposureSnapshot Exposure { get; }
        internal ConditionWearContext(World world, IConditionWearProvider provider, ConditionSnapshot condition, ConditionExposureSnapshot exposure)
        { World = world; Entity = provider.Entity; Source = condition.Capability; Company = Entity.Owner; Tick = world.Tick; Condition = condition; Exposure = exposure; }
    }
    public readonly struct ConditionWearSnapshot
    {
        public string Rule { get; }
        public UnitId Unit { get; }
        public ConditionExposureSnapshot Exposure { get; }
        public ConditionWearDecision Decision { get; }
        internal ConditionWearSnapshot(string rule, UnitId unit, ConditionExposureSnapshot exposure, ConditionWearDecision decision)
        { Rule = rule; Unit = unit; Exposure = exposure; Decision = decision; }
    }
    public static class ConditionWearCodes
    {
        public static readonly Code None = new Code("condition.no-wear"), Exhausted = new Code("condition.exhausted"),
            Updated = new Code("condition.wear-already-updated"), TimeRange = new Code("condition.wear-time-range"),
            NotWear = new Code("condition.not-wear-provider");
    }
    /// <summary>One authored exposure-rate declaration and evaluator shared by rule definitions.</summary>
    public sealed class ConditionWearDeclaration
    {
        private string _unit;
        private IUnitDefinition _resolvedUnit;
        public IUnitDefinition Unit => _resolvedUnit;
        public Fix Quantity { get; private set; }
        public long PeriodNumerator { get; private set; }
        public long PeriodDenominator { get; private set; }
        public DurationUnit PeriodUnit { get; private set; }
        public IReadOnlyList<SignalGateCondition> Conditions { get; private set; }
        private ConditionWearDeclaration(JsonReader reader)
        {
            _unit = reader.String("unit"); Quantity = reader.Fix("quantity");
            var period = reader.Child("period") ?? throw reader.Error("explicit wear period required");
            PeriodNumerator = period.Long("numerator"); PeriodDenominator = period.Long("denominator");
            PeriodUnit = JsonReader.TimeUnit(period.String("unit"), period); period.End();
            if (!Quantity.IsPositive || PeriodNumerator <= 0 || PeriodDenominator <= 0) throw reader.Error("wear rate quantity/period must be positive");
            Conditions = SignalGateCondition.ReadAll(reader, "conditions", true);
        }
        public static ConditionWearDeclaration Read(JsonReader reader) => new ConditionWearDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            _resolvedUnit = content.Def<IUnitContentDefinition>(_unit);
            if (UnitConversion.ValidateAmount(Unit, Quantity) != UnitConversionStatus.Converted) throw content.Fail("wear rate violates native unit quantum");
            foreach (var condition in Conditions) condition.Resolve(content);
        }
        public void ValidateBinding(World world, EntityDef owner)
        { foreach (var condition in Conditions) condition.Validate(owner); }
        public ConditionWearDecision Observe(ConditionWearContext context, string identity)
        {
            Code blocker = SignalGateCondition.ReadGate(context.World, context.Entity, new Code(identity), true, Conditions).Blocker;
            if (!blocker.IsNone) return ConditionWearDecision.Refused(blocker);
            long next;
            try { next = checked(context.Exposure.ElapsedTicks + 1); }
            catch (OverflowException) { return ConditionWearDecision.Refused(ConditionWearCodes.TimeRange); }
            long quantum = Unit.Discrete ? Fix.One.Raw : 1;
            BigInteger before = QuantizedGameRates.AccumulatedRaw(Quantity, PeriodNumerator, PeriodDenominator, PeriodUnit,
                context.World.Time, new WorkProgress(context.Exposure.ElapsedTicks), quantum, false);
            BigInteger after = QuantizedGameRates.AccumulatedRaw(Quantity, PeriodNumerator, PeriodDenominator, PeriodUnit,
                context.World.Time, new WorkProgress(next), quantum, false);
            // Authored degradation saturates at real remaining condition; explicit change commands never clamp.
            Fix damage = Fix.FromRaw((long)BigInteger.Min(after - before, context.Condition.Value.Raw));
            return new ConditionWearDecision(true, damage, Code.None);
        }
    }
    public sealed class AuthoredConditionWearDef : Def, IConditionWearRule
    {
        private ConditionWearDeclaration _declaration;
        public IUnitDefinition Unit => _declaration.Unit;
        public Fix Quantity => _declaration.Quantity;
        public long PeriodNumerator => _declaration.PeriodNumerator;
        public long PeriodDenominator => _declaration.PeriodDenominator;
        public DurationUnit PeriodUnit => _declaration.PeriodUnit;
        public IReadOnlyList<SignalGateCondition> Conditions => _declaration.Conditions;
        protected override void ReadCore(JsonReader reader) => _declaration = ConditionWearDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateBinding(World world, EntityDef owner) => _declaration.ValidateBinding(world, owner);
        public ConditionWearDecision Observe(ConditionWearContext context) => _declaration.Observe(context, Id);
    }
    /// <summary>Pure admission, pass capture and exact provider commit; no exposure/condition state here.</summary>
    public static class ConditionWearContracts
    {
        public static bool IsStructurallyValid(IConditionWearDefinition definition, ConditionExposureSnapshot value)
            => definition != null && value.ElapsedTicks >= 0 && value.UpdatedTick >= -1 &&
                (value.ElapsedTicks == 0) == (value.UpdatedTick == -1) &&
                (value.ElapsedTicks <= value.UpdatedTick || value.ElapsedTicks - value.UpdatedTick <= 1) &&
                (definition.Wear != null || value.ElapsedTicks == 0 && value.UpdatedTick == -1);
        public static ConditionExposureSnapshot ReadExposure(World world, IConditionWearProvider provider)
            => EntityObservation.ReadProvider(world, provider, null, () =>
            {
                ConditionSnapshot condition = ConditionContracts.Read(world, provider);
                var definition = provider.Entity.Def.Component<IConditionWearDefinition>(condition.Capability);
                var value = provider.ReadExposure();
                if (!IsStructurallyValid(definition, value) || value.UpdatedTick > world.Tick)
                    throw new InvalidOperationException("condition exposure disagrees with authored provider/time");
                return value;
            });
        public static ConditionWearSnapshot Read(World world, IConditionWearProvider provider, out ConditionWearContext context)
        {
            context = default;
            var observed = EntityObservation.ReadProvider(world, provider, null, () =>
            {
                ConditionSnapshot condition = ConditionContracts.Read(world, provider); var exposure = ReadExposure(world, provider);
                var definition = provider.Entity.Def.Component<IConditionWearDefinition>(condition.Capability); var rule = definition.Wear;
                var admitted = new ConditionWearContext(world, provider, condition, exposure);
                // Validate the real binding even when exposure is already updated or condition exhausted.
                if (rule != null && (!ReferenceEquals(world.Content.Find<IConditionWearRule>(rule.Id), rule) ||
                    !ReferenceEquals(rule.Unit, definition.Unit)))
                    throw new InvalidOperationException("wear rule differs from its catalog/unit binding");
                ConditionWearDecision decision;
                if (rule == null) decision = ConditionWearDecision.Refused(ConditionWearCodes.None);
                else if (exposure.UpdatedTick == world.Tick) decision = ConditionWearDecision.Refused(ConditionWearCodes.Updated);
                else if (condition.Value == Fix.Zero) decision = ConditionWearDecision.Refused(ConditionWearCodes.Exhausted);
                else decision = rule.Observe(admitted);
                if (decision.Advance != decision.Blocker.IsNone || decision.Damage < Fix.Zero || decision.Damage > condition.Value ||
                    !decision.Advance && decision.Damage != Fix.Zero ||
                    UnitConversion.ValidateAmount(definition.Unit, decision.Damage) != UnitConversionStatus.Converted ||
                    decision.Advance && exposure.ElapsedTicks == long.MaxValue)
                    throw new InvalidOperationException("wear rule returned malformed or impossible damage");
                return (Snapshot: new ConditionWearSnapshot(rule?.Id, condition.Unit, exposure, decision), Context: admitted);
            });
            context = observed.Context; return observed.Snapshot;
        }
        public static void CheckContext(ConditionWearContext context, IConditionWearProvider provider)
        {
            if (context.World == null || context.Entity == null || !ReferenceEquals(provider.Entity, context.Entity) || provider.CapabilityId != context.Source ||
                context.World.Tick != context.Tick || context.Entity.Owner != context.Company || context.Exposure.UpdatedTick == context.Tick)
                throw new InvalidOperationException("wear context is stale or belongs to another provider/session");
            ConditionSnapshot current = ConditionContracts.Read(context.World, provider); var exposure = ReadExposure(context.World, provider);
            if (current.Value != context.Condition.Value || current.ChangedTick != context.Condition.ChangedTick || current.Maximum != context.Condition.Maximum ||
                current.Unit != context.Condition.Unit || exposure.ElapsedTicks != context.Exposure.ElapsedTicks || exposure.UpdatedTick != context.Exposure.UpdatedTick)
                throw new InvalidOperationException("wear admission no longer names the actual condition/exposure");
        }
        public static void Commit(IConditionWearProvider provider, ConditionWearContext context, ConditionWearDecision decision)
        {
            if (!decision.Advance) return;
            CheckContext(context, provider); Entity entity = provider.Entity; int version = entity.Version; EntityId next = context.World.Entities.NextIdentity;
            var definition = entity.Def.Component<IConditionWearDefinition>(provider.CapabilityId);
            if (!decision.Blocker.IsNone || decision.Damage < Fix.Zero || decision.Damage > context.Condition.Value ||
                UnitConversion.ValidateAmount(definition.Unit, decision.Damage) != UnitConversionStatus.Converted || context.Exposure.ElapsedTicks == long.MaxValue)
                throw new InvalidOperationException("wear commit requires bounded exact admitted damage/exposure");
            provider.ApplyWear(context, decision);
            ConditionSnapshot after = ConditionContracts.Read(context.World, provider); var exposure = ReadExposure(context.World, provider);
            if (after.Value != context.Condition.Value - decision.Damage || after.ChangedTick != (decision.Damage.IsPositive ? context.Tick : context.Condition.ChangedTick) ||
                exposure.ElapsedTicks != checked(context.Exposure.ElapsedTicks + 1) || exposure.UpdatedTick != context.Tick || entity.Version == version ||
                entity.Owner != context.Company || context.World.Tick != context.Tick || context.World.Entities.NextIdentity != next)
                throw new InvalidOperationException("wear provider did not commit exact admitted condition/exposure");
        }
    }
}
