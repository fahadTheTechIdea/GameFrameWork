using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional periodic money cost, independent of physical work or operation capability.</summary>
    public interface IEntityOperatingCostDefinition : IContentDependencyDefinition
    {
        EntityDef TargetDefinition { get; }
        Fix Amount { get; }
        MonetaryCostCategory Category { get; }
        GameDuration Period { get; }
        GameDuration Offset { get; }
        Code Reason { get; }
        IOperatingCostCondition Condition { get; }
    }
    public sealed class EntityOperatingCostDef : Def, IEntityOperatingCostDefinition
    {
        private string _target, _condition;
        public EntityDef TargetDefinition { get; private set; }
        public Fix Amount { get; private set; }
        public MonetaryCostCategory Category { get; private set; }
        public GameDuration Period { get; private set; }
        public GameDuration Offset { get; private set; }
        public Code Reason { get; private set; }
        public IOperatingCostCondition Condition { get; private set; }
        public IReadOnlyList<IContentDefinition> ContentDependencies { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _target = reader.String("target"); Amount = reader.Fix("amount");
            if (!reader.Has("condition")) throw reader.Error("condition must name a provider or be explicitly null");
            _condition = reader.NullableString("condition");
            string category = reader.String("category");
            Category = category == "construction" ? MonetaryCostCategory.Construction
                : category == "manufacturing" ? MonetaryCostCategory.Manufacturing
                : category == "running" ? MonetaryCostCategory.Running
                : throw reader.Error("operating cost category must be construction, manufacturing or running");
            Period = new JsonReader(reader.Object("period") ?? throw reader.Error("period is required"), reader.Where + ".period").RationalDuration();
            Offset = new JsonReader(reader.Object("offset") ?? throw reader.Error("offset is required"), reader.Where + ".offset").RationalDuration();
            string reason = reader.String("reason");
            try { ContractIdentity.ValidateId(_target, "target"); ContractIdentity.ValidateId(reason, "cost reason"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            Reason = new Code(reason);
            if (!Amount.IsPositive || Period.IsZero) throw reader.Error("periodic cost requires positive amount and period");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            TargetDefinition = content.Def<EntityDef>(_target);
            Condition = content.OptionalDef<IOperatingCostCondition>(_condition);
            var dependencies = new List<IContentDefinition> { TargetDefinition };
            if (Condition != null) dependencies.Add(Condition);
            ContentDependencies = dependencies.AsReadOnly();
        }
    }
    public readonly struct EntityOperatingCostView
    {
        public string Id { get; }
        public MonetaryCostDecision Cost { get; }
        public GameDuration Period { get; }
        public GameDuration Offset { get; }
        public Code Reason { get; }
        public Code EligibilityBlocker { get; }
        internal EntityOperatingCostView(IEntityOperatingCostDefinition definition, MonetaryCostDecision cost, Code blocker)
        { Id = definition.Id; Cost = cost; Period = definition.Period; Offset = definition.Offset; Reason = definition.Reason; EligibilityBlocker = blocker; }
    }
    public interface IBusinessOperatingCosts
    {
        Code CopyCosts(EntityId target, PlayerId viewer, IList<EntityOperatingCostView> destination);
    }
    /// <summary>One deterministic schedule interpreter. Balances/budgets remain in the original account owner.</summary>
    internal sealed class EntityOperatingCostSystem : GameSystem, IBusinessOperatingCosts, ICompositionValidator
    {
        private readonly World _world;
        private readonly IReadOnlyList<IEntityOperatingCostDefinition> _definitions;
        private readonly Dictionary<string, Schedule> _schedules = new Dictionary<string, Schedule>(StringComparer.Ordinal);
        public override string Id => "sim.tycoon-operating-costs";
        public override int StateSchemaVersion => 1;
        internal EntityOperatingCostSystem(World world)
        {
            _world = world;
            var definitions = new List<IEntityOperatingCostDefinition>(world.Content.All<IEntityOperatingCostDefinition>());
            definitions.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); _definitions = definitions.AsReadOnly();
        }
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ContentException("operating costs belong to another session");
            foreach (var definition in _definitions)
                EntityObservation.ReadProvider(world, definition, null, () => {
                    if (!world.Content.Owns(definition) || !ReferenceEquals(world.Content.Find<IEntityOperatingCostDefinition>(definition.Id), definition)
                        || definition.TargetDefinition == null
                        || !ReferenceEquals(world.Content.Find<EntityDef>(definition.TargetDefinition.Id), definition.TargetDefinition)
                        || !definition.Amount.IsPositive || definition.Reason.IsNone)
                        throw new ContentException("operating cost requires actual immutable target metadata and positive amount");
                    long period = world.Time.ExactTicks(definition.Period), offset = world.Time.ExactTicks(definition.Offset);
                    if (period <= 0 || offset < 0 || offset >= period)
                        throw new ContentException(definition.Id + ": offset must be within its positive exact period");
                    _schedules[definition.Id] = Schedule.Every(world.Time, definition.Period, offset);
                    if (definition.Condition != null)
                    {
                        if (!world.Content.Owns(definition.Condition) || !ReferenceEquals(world.Content.Find<IOperatingCostCondition>(definition.Condition.Id), definition.Condition))
                            throw new ContentException(definition.Id + ": actual cost condition required");
                        definition.Condition.ValidateTarget(definition.TargetDefinition);
                    }
                    if (BusinessModeRules.EvaluateCost(world, definition.Category, definition.Amount).Applicability == MonetaryCostApplicability.UnavailableCapability)
                        throw new ContentException(definition.Id + ": enabled periodic cost requires its monetary capability");
                    return true;
                });
        }
        public override void Update(World world, TickInfo tick)
        {
            foreach (var definition in _definitions)
            {
                if (!_schedules[definition.Id].IsDue(tick.Tick)) continue;
                var facts = EntityObservation.ReadProvider(world, definition, null, () =>
                    (world.Time.ExactTicks(definition.Period), world.Time.ExactTicks(definition.Offset), definition.TargetDefinition,
                        BusinessModeRules.EvaluateCost(world, definition.Category, definition.Amount), definition.Reason, definition.Condition));
                if (tick.Tick < facts.Item2 || (tick.Tick - facts.Item2) % facts.Item1 != 0) continue;
                if (facts.Item4.Applicability != MonetaryCostApplicability.Enabled) continue;
                foreach (Entity entity in world.Entities.All)
                    if (entity.Alive && entity.Owner.IsValid && ReferenceEquals(entity.Def, facts.Item3))
                    {
                        if (!CheckCondition(facts.Item6, entity).IsNone) continue;
                        world.Services.Get<IEconomy>().Charge(entity.Owner, facts.Item4.AuthoredAmount, facts.Item5);
                    }
            }
        }
        public Code CopyCosts(EntityId target, PlayerId viewer, IList<EntityOperatingCostView> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear(); _world.RequireHealthyUpdate();
            Entity entity = _world.Entities.Get(target);
            if (!_world.ViewFor(viewer).CanObserveOwned(entity)) return Codes.NotFound;
            foreach (var definition in _definitions)
            {
                var facts = EntityObservation.ReadProvider(_world, definition, entity, () =>
                    (definition.TargetDefinition, new EntityOperatingCostView(definition,
                        BusinessModeRules.EvaluateCost(_world, definition.Category, definition.Amount),
                        ReferenceEquals(definition.TargetDefinition, entity.Def) ? CheckCondition(definition.Condition, entity) : Code.None)));
                if (ReferenceEquals(facts.Item1, entity.Def)) destination.Add(facts.Item2);
            }
            return Code.None;
        }
        private Code CheckCondition(IOperatingCostCondition condition, Entity entity)
            => condition == null ? Code.None : EntityObservation.ReadProvider(_world, condition, entity,
                () => condition.Check(_world, entity), observeTargetState: true);
    }
}
