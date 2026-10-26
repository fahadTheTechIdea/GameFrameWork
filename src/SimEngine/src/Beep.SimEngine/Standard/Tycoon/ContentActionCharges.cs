using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IContentActionChargeDefinition : IContentDependencyDefinition
    {
        Code Action { get; }
        IContentDefinition Target { get; }
        Fix Amount { get; }
        MonetaryCostCategory Category { get; }
        Code Reason { get; }
        bool OncePerCompany { get; }
    }
    /// <summary>Optional Tycoon-owned annotation; physical content declares no monetary fields.</summary>
    public sealed class ContentActionChargeDef : Def, IContentActionChargeDefinition
    {
        private string _target;
        public Code Action { get; private set; }
        public IContentDefinition Target { get; private set; }
        public Fix Amount { get; private set; }
        public MonetaryCostCategory Category { get; private set; }
        public Code Reason { get; private set; }
        public bool OncePerCompany { get; private set; }
        public IReadOnlyList<IContentDefinition> ContentDependencies { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Action = new Code(reader.String("action")); _target = reader.String("target");
            Amount = reader.Fix("amount"); Reason = new Code(reader.String("reason")); OncePerCompany = reader.Bool("once_per_company");
            string category = reader.String("category");
            Category = category == "construction" ? MonetaryCostCategory.Construction : category == "manufacturing" ? MonetaryCostCategory.Manufacturing
                : category == "running" ? MonetaryCostCategory.Running : throw reader.Error("explicit supported monetary category required");
            try { ContractIdentity.ValidateId(Action.Id, "action"); ContractIdentity.ValidateId(_target, "target"); ContractIdentity.ValidateId(Reason.Id, "reason"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            if (!Amount.IsPositive) throw reader.Error("content action charge requires a positive amount");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            Target = content.Def<IContentDefinition>(_target);
            ContentDependencies = Array.AsReadOnly(new[] { Target });
        }
    }
    public readonly struct ContentActionChargeView
    {
        public string Definition { get; }
        public Code Action { get; }
        public string Target { get; }
        public MonetaryCostDecision Cost { get; }
        public bool AlreadyCharged { get; }
        internal ContentActionChargeView(string definition, Code action, string target, MonetaryCostDecision cost, bool charged)
        { Definition = definition; Action = action; Target = target; Cost = cost; AlreadyCharged = charged; }
    }
    public interface IBusinessContentCharges
    {
        Code Read(PlayerId company, Code action, string target, out ContentActionChargeView facts);
        Code Charge(PlayerId company, Code action, string target);
    }
    /// <summary>Sole action annotation selection/admission; mutable money and once receipts stay in BusinessAccounts.</summary>
    internal sealed class ContentActionCharges : IBusinessContentCharges
    {
        private readonly World _world;
        private readonly BusinessAccounts _accounts;
        private readonly Dictionary<(Code, string), IContentActionChargeDefinition> _definitions = new Dictionary<(Code, string), IContentActionChargeDefinition>();
        internal ContentActionCharges(World world, BusinessAccounts accounts)
        {
            _world = world; _accounts = accounts;
            var once = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in world.Content.All<IContentActionChargeDefinition>())
                EntityObservation.ReadProvider(world, definition, null, () => {
                    if (!world.Content.Owns(definition) || !ReferenceEquals(world.Content.Find<IContentActionChargeDefinition>(definition.Id), definition)
                        || definition.Target == null || !world.Content.Owns(definition.Target)
                        || !ReferenceEquals(world.Content.Find<IContentDefinition>(definition.Target.Id), definition.Target)
                        || !definition.Amount.IsPositive || definition.Action.IsNone || definition.Reason.IsNone)
                        throw new ContentException("content action charge requires actual immutable content and positive authored metadata");
                    var key = (definition.Action, definition.Target.Id);
                    if (_definitions.ContainsKey(key)) throw new ContentException("duplicate content action charge for " + definition.Target.Id);
                    _definitions.Add(key, definition);
                    if (definition.OncePerCompany) once.Add(definition.Id);
                    if (BusinessModeRules.EvaluateCost(world, definition.Category, definition.Amount).Applicability == MonetaryCostApplicability.UnavailableCapability)
                        throw new ContentException("enabled content action charge requires its account capability");
                    return true;
                });
            accounts?.ConfigureActionCharges(once);
        }
        public Code Read(PlayerId company, Code action, string target, out ContentActionChargeView facts)
        {
            facts = default; _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            if (action.IsNone || target == null || _world.Content.Find<IContentDefinition>(target) == null) return Codes.UnknownDef;
            if (!_definitions.TryGetValue((action, target), out var definition)) return Code.None; // Explicitly no authored monetary intent.
            var value = EntityObservation.ReadProvider(_world, definition, null, () =>
                (definition.Id, definition.Category, definition.Amount, definition.OncePerCompany));
            MonetaryCostDecision cost = BusinessModeRules.EvaluateCost(_world, value.Item2, value.Item3);
            bool charged = value.Item4 && _accounts != null && _accounts.IsActionCharged(company, value.Id);
            facts = new ContentActionChargeView(value.Id, action, target, cost, charged);
            if (charged || cost.Applicability != MonetaryCostApplicability.Enabled)
                return cost.Applicability == MonetaryCostApplicability.UnavailableCapability ? BusinessModeRules.UnavailableCostCapability : Code.None;
            return _accounts.CanAfford(company, cost.AuthoredAmount) ? Code.None : Codes.CannotAfford;
        }
        public Code Charge(PlayerId company, Code action, string target)
        {
            Code refusal = Read(company, action, target, out var facts);
            if (!refusal.IsNone || facts.Definition == null || facts.AlreadyCharged || facts.Cost.Applicability != MonetaryCostApplicability.Enabled) return refusal;
            var definition = _definitions[(action, target)];
            return _accounts.ChargeAction(company, definition.Id, facts.Cost.AuthoredAmount, definition.Reason, definition.OncePerCompany);
        }
    }
}
