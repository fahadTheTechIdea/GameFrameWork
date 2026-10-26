using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IConditionDefinition : INamedCapabilityDefinition
    {
        IUnitDefinition Unit { get; }
        Fix Maximum { get; }
        Fix Initial { get; }
    }
    public interface IConditionProvider : INamedEntityCapability
    {
        ConditionSnapshot ReadCondition();
    }
    public interface IConditionChange : IConditionProvider
    {
        Code CheckChange(World world, ConditionChange change);
        void ApplyChange(World world, ConditionChange change);
    }
    public enum ConditionChangeKind : byte { Damage = 0, Repair = 1, Restore = 2 }
    public readonly struct ConditionChange
    {
        public ConditionChangeKind Kind { get; }
        public UnitQuantity Quantity { get; }
        public ConditionChange(ConditionChangeKind kind, UnitQuantity quantity) { Kind = kind; Quantity = quantity; }
    }
    public readonly struct ConditionSnapshot
    {
        public Code Capability { get; }
        public UnitId Unit { get; }
        public Fix Value { get; }
        public Fix Maximum { get; }
        public long ChangedTick { get; }
        public ConditionSnapshot(Code capability, UnitId unit, Fix value, Fix maximum, long changedTick)
        {
            if (capability.IsNone || !unit.IsValid || !maximum.IsPositive || value < Fix.Zero || value > maximum || changedTick < -1)
                throw new ArgumentException("malformed condition facts");
            Capability = capability; Unit = unit; Value = value; Maximum = maximum; ChangedTick = changedTick;
        }
    }
    public static class ConditionCodes
    {
        public static readonly Code NotCondition = new Code("condition.not-condition"), ReadOnly = new Code("condition.read-only"),
            InvalidChange = new Code("condition.invalid-change"), Bounds = new Code("condition.bounds");
    }
    /// <summary>One live binding/read/change protocol; providers alone own condition and its codec.</summary>
    public static class ConditionContracts
    {
        public static ConditionSnapshot Read(World world, IConditionProvider provider)
            => EntityObservation.ReadProvider(world, provider, null, () =>
        {
            Entity entity = provider?.Entity; Code identity = provider?.CapabilityId ?? Code.None;
            if (world == null || entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity) ||
                identity.IsNone || !ReferenceEquals(entity.Get<IConditionProvider>(identity), provider))
                throw new ArgumentException("condition must be the actual named provider in this session");
            IConditionDefinition definition = entity.Def.Component<IConditionDefinition>(identity);
            if (definition == null || definition.Unit == null ||
                !ReferenceEquals(world.Content.Find<IUnitContentDefinition>(definition.Unit.Unit.Id), definition.Unit))
                throw new InvalidOperationException("condition has no actual catalog-owned unit definition");
            ConditionSnapshot value = provider.ReadCondition();
            if (value.Capability != identity || value.Unit != definition.Unit.Unit || value.Maximum != definition.Maximum ||
                !value.Maximum.IsPositive || value.Value < Fix.Zero || value.Value > value.Maximum || value.ChangedTick < -1 ||
                value.ChangedTick > world.Tick || value.ChangedTick == -1 && value.Value != definition.Initial ||
                UnitConversion.ValidateAmount(definition.Unit, value.Value) != UnitConversionStatus.Converted ||
                UnitConversion.ValidateAmount(definition.Unit, value.Maximum) != UnitConversionStatus.Converted)
                throw new InvalidOperationException("condition facts disagree with authored metadata or game time");
            return value;
        });
        public static Code Check(World world, IConditionProvider provider, ConditionChange change)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (provider == null) return ConditionCodes.NotCondition;
            return EntityObservation.ReadProvider(world, provider, null, () =>
            {
                ConditionSnapshot before = Read(world, provider);
                IConditionDefinition definition = provider.Entity.Def.Component<IConditionDefinition>(before.Capability);
                Code admission = CheckQuantity(definition, before, change); if (!admission.IsNone) return admission;
                if (!(provider is IConditionChange mutable)) return ConditionCodes.ReadOnly;
                Code result = mutable.CheckChange(world, change);
                if (!Same(before, Read(world, provider))) throw new InvalidOperationException("condition changed during pure admission");
                return result;
            });
        }
        public static Code CheckQuantity(IConditionDefinition definition, ConditionSnapshot before, ConditionChange change)
        {
            if (definition?.Unit == null || before.Unit != definition.Unit.Unit || before.Maximum != definition.Maximum ||
                (byte)change.Kind > (byte)ConditionChangeKind.Restore || !change.Quantity.Amount.IsPositive || change.Quantity.Unit != before.Unit ||
                UnitConversion.ValidateAmount(definition.Unit, change.Quantity.Amount) != UnitConversionStatus.Converted) return ConditionCodes.InvalidChange;
            if (change.Kind == ConditionChangeKind.Restore)
                return change.Quantity.Amount <= before.Maximum ? Code.None : ConditionCodes.Bounds;
            return (change.Kind == ConditionChangeKind.Damage ? change.Quantity.Amount > before.Value : change.Quantity.Amount > before.Maximum - before.Value)
                ? ConditionCodes.Bounds : Code.None;
        }
        /// <summary>The admitted native value calculation, shared by provider mutation and exact verification.</summary>
        public static Fix ValueAfter(ConditionSnapshot before, ConditionChange change)
            => change.Kind == ConditionChangeKind.Damage ? before.Value - change.Quantity.Amount :
                change.Kind == ConditionChangeKind.Repair ? before.Value + change.Quantity.Amount :
                change.Kind == ConditionChangeKind.Restore ? Fix.Max(before.Value, change.Quantity.Amount) :
                throw new ArgumentException("unknown condition change kind");
        public static Code Commit(World world, IConditionProvider provider, ConditionChange change)
        {
            Code refusal = Check(world, provider, change); if (!refusal.IsNone) return refusal;
            ConditionSnapshot before = Read(world, provider); Entity entity = provider.Entity;
            int version = entity.Version; EntityId next = world.Entities.NextIdentity; long tick = world.Tick; PlayerId owner = entity.Owner;
            ((IConditionChange)provider).ApplyChange(world, change);
            ConditionSnapshot after = Read(world, provider);
            Fix expected = ValueAfter(before, change);
            if (after.Value != expected || after.ChangedTick != tick || after.Maximum != before.Maximum || after.Unit != before.Unit ||
                after.Capability != before.Capability || entity.Version == version || entity.Owner != owner || world.Tick != tick || world.Entities.NextIdentity != next)
                throw new InvalidOperationException("condition provider did not commit the admitted exact change");
            return Code.None;
        }
        private static bool Same(ConditionSnapshot a, ConditionSnapshot b) => a.Capability == b.Capability && a.Unit == b.Unit &&
            a.Value == b.Value && a.Maximum == b.Maximum && a.ChangedTick == b.ChangedTick;
    }
    public interface IEquipmentConditionDefinition : IConditionWearDefinition, IAutomationSignalDefinition
    {
        EntityStateBindings States { get; }
    }
    /// <summary>Sole authored condition identity/unit/band reader; no live state.</summary>
    public sealed class ConditionDeclaration
    {
        private readonly string _unit;
        public Code CapabilityId { get; }
        public IUnitDefinition Unit { get; private set; }
        public Fix Maximum { get; }
        public Fix Initial { get; }
        private ConditionDeclaration(JsonReader reader)
        {
            string id = reader.String("id");
            try { ContractIdentity.ValidateId(id, "condition"); } catch (ArgumentException error) { throw reader.Error(error.Message); }
            CapabilityId = new Code(id); _unit = reader.String("unit"); Maximum = reader.Fix("maximum"); Initial = reader.Fix("initial");
            if (!Maximum.IsPositive || Initial < Fix.Zero || Initial > Maximum)
                throw reader.Error("condition requires positive maximum and bounded explicit initial value");
        }
        public static ConditionDeclaration Read(JsonReader reader) => new ConditionDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            Unit = content.Def<IUnitContentDefinition>(_unit);
            if (UnitConversion.ValidateAmount(Unit, Initial) != UnitConversionStatus.Converted ||
                UnitConversion.ValidateAmount(Unit, Maximum) != UnitConversionStatus.Converted)
                throw content.Fail("condition quantities must obey their native unit quantum");
        }
    }
    /// <summary>Sole equipment metadata declaration; original runtime owns value and exposure.</summary>
    public sealed class EquipmentConditionDeclaration
    {
        private readonly ConditionDeclaration _condition;
        private readonly string _wear;
        public Code CapabilityId => _condition.CapabilityId;
        public IUnitDefinition Unit => _condition.Unit;
        public Fix Maximum => _condition.Maximum;
        public Fix Initial => _condition.Initial;
        public static IReadOnlyList<AutomationMetricSchema> SignalBindings { get; } = Array.AsReadOnly(new[] {
            new AutomationMetricSchema(new Code("condition.value"), AutomationSignalKind.Number, new Code("unit.condition")),
            new AutomationMetricSchema(new Code("condition.maximum"), AutomationSignalKind.Number, new Code("unit.condition")) });
        public IConditionWearRule Wear { get; private set; }
        public EntityStateBindings States { get; private set; }
        private EquipmentConditionDeclaration(JsonReader reader)
        {
            _condition = ConditionDeclaration.Read(reader);
            JsonValue wear = reader.Value("wear");
            if (wear == null || wear.Kind != JsonKind.Null && wear.Kind != JsonKind.String ||
                wear.Kind == JsonKind.String && string.IsNullOrWhiteSpace(wear.Text))
                throw reader.Error("explicit wear:null or wear profile identity required");
            _wear = wear.Kind == JsonKind.Null ? null : wear.Text;
            States = EntityStateBindings.Read(reader, "damaged", "failed");
        }
        public static EquipmentConditionDeclaration Read(JsonReader reader)
            => new EquipmentConditionDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            _condition.Resolve(content); States = States.Resolve(content);
            Wear = _wear == null ? null : content.Def<IConditionWearRule>(_wear);
            if (Unit.Unit != new UnitId("unit.condition"))
                throw content.Fail("equipment signals require explicit unit.condition");
        }
    }
    public sealed class EquipmentConditionDef : ComponentDef<EquipmentCondition>, IEquipmentConditionDefinition
    {
        private EquipmentConditionDeclaration _declaration;
        public IConditionWearRule Wear => _declaration.Wear;
        public Code CapabilityId => _declaration.CapabilityId;
        public IUnitDefinition Unit => _declaration.Unit;
        public Fix Maximum => _declaration.Maximum;
        public Fix Initial => _declaration.Initial;
        public EntityStateBindings States => _declaration.States;
        public int SignalSchemaVersion => 1;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => EquipmentConditionDeclaration.SignalBindings;
        protected override void ReadCore(JsonReader reader) => _declaration = EquipmentConditionDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    /// <summary>One condition value with its change time and cumulative eligible exposure. No mirrors.</summary>
    public sealed class EquipmentCondition : Component, IConditionWearProvider, IAutomationSignalSource, IEntityStateSource
    {
        public new IEquipmentConditionDefinition Def => (IEquipmentConditionDefinition)base.Def;
        private Fix _value;
        private long _changed = -1;
        private long _exposure, _exposureAt = -1;
        public Code CapabilityId => Def.CapabilityId;
        public override void OnCreate() { _value = Def.Initial; _changed = -1; _exposure = 0; _exposureAt = -1; }
        public ConditionExposureSnapshot ReadExposure() => new ConditionExposureSnapshot(_exposure, _exposureAt);
        public void ApplyWear(ConditionWearContext context, ConditionWearDecision decision)
        {
            ConditionWearContracts.CheckContext(context, this);
            if (Def.Wear == null || !decision.Advance || !decision.Blocker.IsNone || decision.Damage < Fix.Zero || decision.Damage > _value ||
                _exposure == long.MaxValue || UnitConversion.ValidateAmount(Def.Unit, decision.Damage) != UnitConversionStatus.Converted)
                throw new InvalidOperationException("wear requires exact eligible condition/exposure");
            if (decision.Damage.IsPositive)
            {
                var refusal = ConditionContracts.Commit(context.World, this,
                    new ConditionChange(ConditionChangeKind.Damage, new UnitQuantity(Def.Unit.Unit, decision.Damage)));
                if (!refusal.IsNone) throw new InvalidOperationException("admitted wear change refused: " + refusal);
            }
            _exposure++; _exposureAt = context.Tick; Entity.Touch();
        }
        public ConditionSnapshot ReadCondition() => new ConditionSnapshot(CapabilityId, Def.Unit.Unit, _value, Def.Maximum, _changed);
        public Code CheckChange(World world, ConditionChange change)
        {
            if (!ReferenceEquals(world.Entities.Get(Entity.Id), Entity) || !Entity.Alive) return Codes.NotFound;
            return ConditionContracts.CheckQuantity(Def, ReadCondition(), change);
        }
        public void ApplyChange(World world, ConditionChange change)
        {
            Code refusal = CheckChange(world, change); if (!refusal.IsNone) throw new InvalidOperationException("condition change refused: " + refusal);
            _value = ConditionContracts.ValueAfter(ReadCondition(), change);
            _changed = world.Tick; Entity.Touch();
        }
        public AutomationSignalSnapshot ReadSignal(World world, IAutomationSignalContent signal)
        {
            ConditionSnapshot value = ConditionContracts.Read(world, this);
            return signal.Metric.Id == "condition.value" ? signal.Number(value.Value, new Code(value.Unit.Id)) :
                signal.Metric.Id == "condition.maximum" ? signal.Number(value.Maximum, new Code(value.Unit.Id)) : signal.Unavailable(OperationCodes.SignalUnavailable);
        }
        public IEntityStateDefinition GetState(EntityUpdateContext context)
            => _value == Fix.Zero ? Def.States["failed"] : _value < Def.Maximum ? Def.States["damaged"] : null;
        public override void Sync(IStateSync sync)
        {
            Fix value = _value; long changed = _changed; sync.Fix(ref value); sync.Long(ref changed);
            long exposure = _exposure, exposureAt = _exposureAt; sync.Long(ref exposure); sync.Long(ref exposureAt);
            if (value < Fix.Zero || value > Def.Maximum || changed < -1 || changed == -1 && value != Def.Initial ||
                UnitConversion.ValidateAmount(Def.Unit, value) != UnitConversionStatus.Converted ||
                !ConditionWearContracts.IsStructurallyValid(Def, new ConditionExposureSnapshot(exposure, exposureAt)))
                throw new CorruptStateException("invalid saved equipment condition");
            if (sync.Reading) { _value = value; _changed = changed; _exposure = exposure; _exposureAt = exposureAt; }
        }
    }
    public interface IConditionEffect : IPhysicalWorkEffect
    {
        Code Source { get; }
        IUnitDefinition Unit { get; }
        ConditionChange Change { get; }
        bool WorkerSubject { get; }
    }
    /// <summary>One strict immutable authored condition-change declaration.</summary>
    public sealed class ConditionEffectDeclaration
    {
        private readonly string _unit;
        public Code Source { get; }
        public ConditionChange Change { get; }
        public IUnitDefinition Unit { get; private set; }
        private ConditionEffectDeclaration(Code source, string unit, ConditionChange change)
        { Source = source; _unit = unit; Change = change; }
        public static ConditionEffectDeclaration Read(JsonReader reader)
        {
            string source = reader.String("source"), kind = reader.String("kind"), unit = reader.String("unit"); Fix amount = reader.Fix("amount");
            try { ContractIdentity.ValidateId(source, "condition effect source"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            if (!amount.IsPositive || kind != "damage" && kind != "repair" && kind != "restore")
                throw reader.Error("condition effect requires damage/repair/restore and positive explicit amount/target");
            try { return new ConditionEffectDeclaration(new Code(source), unit, new ConditionChange(
                kind == "damage" ? ConditionChangeKind.Damage : kind == "repair" ? ConditionChangeKind.Repair : ConditionChangeKind.Restore,
                new UnitQuantity(new UnitId(unit), amount))); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
        }
        public void Resolve(ContentResolver content)
        {
            Unit = content.Def<IUnitContentDefinition>(_unit);
            if (UnitConversion.ValidateAmount(Unit, Change.Quantity.Amount) != UnitConversionStatus.Converted)
                throw content.Fail("condition effect amount violates native unit quantum");
        }
    }
    /// <summary>Actual native condition owner and existing CompletedBy are the only execution authorities.</summary>
    public static class ConditionEffectContracts
    {
        public static void ValidateDefinition(World world, IConditionEffect effect)
        {
            if (!world.Services.Has<WorkBoard>() || effect.Source.IsNone || effect.Unit == null ||
                !ReferenceEquals(world.Content.Find<IUnitContentDefinition>(effect.Unit.Unit.Id), effect.Unit) ||
                effect.Change.Quantity.Unit != effect.Unit.Unit || !effect.Change.Quantity.Amount.IsPositive ||
                (byte)effect.Change.Kind > (byte)ConditionChangeKind.Restore ||
                UnitConversion.ValidateAmount(effect.Unit, effect.Change.Quantity.Amount) != UnitConversionStatus.Converted)
                throw new ContentException(effect.Id + ": invalid selected condition effect metadata");
        }
        public static void ValidatePlan(World world, IConditionEffect effect, IActivityDefinition plan, IActivityNodeDefinition node)
        {
            PhysicalWorkEffectContracts.ValidatePlan(world, effect, plan, node);
            if (effect.WorkerSubject && (node == null || node.Duration.IsZero || node.Roles.Count == 0))
                throw new ContentException(plan.PlanKey + ": worker condition effect requires its own positive-time machine node");
        }
        public static Entity ResolveSubject(World world, IConditionEffect effect, Order order, IActivityNodeDefinition node)
            => world.Entities.Get(effect.WorkerSubject ? node == null ? EntityId.None : order.Node(node.Id)?.CompletedBy ?? EntityId.None : order.Target);
        public static Code Check(World world, IConditionEffect effect, Entity subject)
            => ConditionContracts.Check(world, subject.Get<IConditionProvider>(effect.Source), effect.Change);
        public static Code Commit(World world, IConditionEffect effect, Entity subject)
            => ConditionContracts.Commit(world, subject.Get<IConditionProvider>(effect.Source), effect.Change);
    }
    public class ConditionEffectDef : Def, IConditionEffect
    {
        public IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.AsReadOnly(new IContentDefinition[] { (IUnitContentDefinition)Unit });
        private ConditionEffectDeclaration _declaration;
        public Code Source => _declaration.Source;
        public IUnitDefinition Unit => _declaration.Unit;
        public ConditionChange Change => _declaration.Change;
        public virtual bool WorkerSubject => false;
        protected override void ReadCore(JsonReader reader) => _declaration = ConditionEffectDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void RequireCapabilities(World world) => ConditionEffectContracts.ValidateDefinition(world, this);
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node)
            => ConditionEffectContracts.ResolveSubject(world, this, order, node);
        public Code CheckSubject(World world, Entity subject, Order order, IActivityNodeDefinition node)
            => ConditionEffectContracts.Check(world, this, subject);
        public Code CommitSubject(World world, Entity subject, Order order, IActivityNodeDefinition node)
            => ConditionEffectContracts.Commit(world, this, subject);
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
            => ConditionEffectContracts.ValidatePlan(world, this, plan, node);
        public Code Check(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }
    public sealed class WorkerConditionEffectDef : ConditionEffectDef
    {
        public override bool WorkerSubject => true;
    }
    public class ConditionRequirementDef : WorkRequirementDef
    {
        public override System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.AsReadOnly(new IContentDefinition[] { (IUnitContentDefinition)_resolvedUnit });
        private string _unit;
        private IUnitDefinition _resolvedUnit;
        public Code Source { get; private set; }
        public Fix Minimum { get; private set; }
        public Fix Maximum { get; private set; }
        public Code Blocker { get; private set; }
        protected virtual Entity Subject(WorkRequirementContext context) => context.Target;
        protected override void ReadCore(JsonReader reader)
        {
            string source = reader.String("source"), blocker = reader.String("blocker"); _unit = reader.String("unit");
            try { ContractIdentity.ValidateId(source, "condition requirement source"); ContractIdentity.ValidateId(blocker, "condition requirement blocker"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            Source = new Code(source); Blocker = new Code(blocker); Minimum = reader.Fix("minimum"); Maximum = reader.Fix("maximum");
            if (Minimum < Fix.Zero || Maximum < Minimum) throw reader.Error("condition requirement needs a nonnegative ordered range");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            _resolvedUnit = content.Def<IUnitContentDefinition>(_unit);
            if (UnitConversion.ValidateAmount(_resolvedUnit, Minimum) != UnitConversionStatus.Converted ||
                UnitConversion.ValidateAmount(_resolvedUnit, Maximum) != UnitConversionStatus.Converted) throw content.Fail("condition requirement violates native unit quantum");
        }
        public override WorkRequirementResult Evaluate(WorkRequirementContext context)
        {
            IConditionProvider provider = Subject(context)?.Get<IConditionProvider>(Source);
            if (provider == null) return WorkRequirementResult.Invalid(ConditionCodes.NotCondition);
            ConditionSnapshot value = ConditionContracts.Read(context.World, provider);
            if (value.Unit != _resolvedUnit.Unit) return WorkRequirementResult.Invalid(ConditionCodes.InvalidChange);
            return value.Value >= Minimum && value.Value <= Maximum ? WorkRequirementResult.Satisfied() : WorkRequirementResult.Pending(Blocker);
        }
    }
    public sealed class WorkerConditionRequirementDef : ConditionRequirementDef, IWorkerWorkRequirement
    {
        protected override Entity Subject(WorkRequirementContext context) => context.Worker;
    }
    /// <summary>One condition pass: capture every eligibility decision before committing any wear.</summary>
    public sealed class EquipmentConditionSystem : GameSystem, ICompositionValidator
    {
        private World _world;
        public override string Id => "sim.equipment-condition";
        public override int StateSchemaVersion => 3;
        public void ValidateComposition(World world)
        {
            foreach (EntityDef entity in world.Content.All<EntityDef>())
                foreach (ComponentDef part in entity.Components)
                {
                    if (typeof(IConditionProvider).IsAssignableFrom(part.ComponentType) && !(part is IConditionDefinition))
                        throw new ContentException(entity.Id + ": condition provider requires declared metadata");
                    if (part is IConditionDefinition condition)
                    {
                        if (!typeof(IConditionProvider).IsAssignableFrom(part.ComponentType) || condition.Unit == null ||
                            !ReferenceEquals(entity.Component<IConditionDefinition>(condition.CapabilityId), condition) ||
                            !ReferenceEquals(world.Content.Find<IUnitContentDefinition>(condition.Unit.Unit.Id), condition.Unit) ||
                            !condition.Maximum.IsPositive || condition.Initial < Fix.Zero || condition.Initial > condition.Maximum ||
                            UnitConversion.ValidateAmount(condition.Unit, condition.Maximum) != UnitConversionStatus.Converted ||
                            UnitConversion.ValidateAmount(condition.Unit, condition.Initial) != UnitConversionStatus.Converted)
                            throw new ContentException(entity.Id + ": condition needs actual named metadata, unit and exact bounded initial value");
                        if (part is IEquipmentConditionDefinition equipment)
                        {
                            if (!typeof(IConditionWearProvider).IsAssignableFrom(part.ComponentType) ||
                                !typeof(IEntityStateSource).IsAssignableFrom(part.ComponentType) ||
                                !typeof(IAutomationSignalSource).IsAssignableFrom(part.ComponentType) || equipment.States == null ||
                                condition.Unit.Unit != new UnitId("unit.condition") || equipment.States.Definitions.Count != 2 ||
                                !equipment.States.Definitions.ContainsKey("damaged") || !equipment.States.Definitions.ContainsKey("failed"))
                                throw new ContentException(entity.Id + ": equipment requires original condition runtime and explicit state modes");
                            foreach (var state in equipment.States.Definitions.Values)
                                if (state == null || !ReferenceEquals(world.Content.Find<IEntityStateDefinition>(state.Id), state))
                                    throw new ContentException(entity.Id + ": equipment states must alias the actual catalog");
                        }
                    }
                    if (typeof(IOperationPolicy).IsAssignableFrom(part.ComponentType) && !(part is IOperationPolicyDefinition))
                        throw new ContentException(entity.Id + ": operation policy provider requires declared metadata");
                    if (part is IOperationPolicyDefinition policy) OperationPolicyContracts.ValidateDefinition(world, entity, part, policy);
                    if (part is IConditionWearDefinition definition)
                    {
                        if (!typeof(IConditionWearProvider).IsAssignableFrom(part.ComponentType))
                            throw new ContentException(entity.Id + ": wear definition requires a wear provider");
                        if (definition.Wear == null) continue;
                        if (!ReferenceEquals(world.Content.Find<IConditionWearRule>(definition.Wear.Id), definition.Wear) ||
                            !ReferenceEquals(definition.Unit, definition.Wear.Unit))
                            throw new ContentException(entity.Id + ": wear profile must alias the actual catalog and condition unit");
                        definition.Wear.ValidateBinding(world, entity);
                    }
                }
        }
        public override void Initialize(World world) { _world = world; }
        private static void Validate(World world)
        {
            foreach (Entity entity in world.Entities.All)
                if (entity.Alive) foreach (Component component in entity.Components)
                {
                    if (component is IConditionWearProvider wear) ConditionWearContracts.ReadExposure(world, wear);
                    else if (component is IConditionProvider provider) ConditionContracts.Read(world, provider);
                    if (component is IOperationPolicy policy) OperationPolicyContracts.Read(world, policy);
                }
        }
        public override void Update(World world, TickInfo tick)
        {
            Validate(world);
            var pass = new List<(IConditionWearProvider Provider, ConditionWearContext Context, ConditionWearDecision Decision)>();
            foreach (Entity entity in world.Entities.All)
                if (entity.Alive) foreach (Component component in entity.Components)
                    if (component is IConditionWearProvider provider)
                    {
                        var snapshot = ConditionWearContracts.Read(world, provider, out var context);
                        if (snapshot.Decision.Advance) pass.Add((provider, context, snapshot.Decision));
                    }
            foreach (var entry in pass) ConditionWearContracts.Commit(entry.Provider, entry.Context, entry.Decision);
            var policies = new List<(IOperationPolicy Provider, OperationPolicyView View)>();
            foreach (Entity entity in world.Entities.All)
                if (entity.Alive) foreach (Component component in entity.Components)
                    if (component is IOperationPolicy policy) policies.Add((policy, OperationPolicyContracts.Read(world, policy)));
            foreach (var entry in policies) OperationPolicyContracts.Observe(world, entry.Provider, entry.View);
            Validate(world);
        }
        public override void Sync(IStateSync sync) { if (!sync.Reading) Validate(_world); }
        public override void OnLoaded(World world)
        {
            try { Validate(world); }
            catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
            catch (InvalidOperationException error) { throw new CorruptStateException(error.Message); }
        }
    }
}
