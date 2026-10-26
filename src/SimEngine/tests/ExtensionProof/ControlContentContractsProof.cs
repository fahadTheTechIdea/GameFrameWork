using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class DeclaredOutletReserveActionDef : Def, IControlActionPolicy
    {
        private readonly OutletReserveControlActionDeclaration _declaration = new OutletReserveControlActionDeclaration();
        public string CommandId => _declaration.CommandId;
        public string CommandOwner => _declaration.CommandOwner;
        public ControlTargetKind TargetKind => _declaration.TargetKind;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateComposition(World world) => _declaration.ValidateComposition(world, Id);
        public void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination)
            => _declaration.CopyTargetIds(world, company, entity, destination);
        public Code CheckBinding(World world, PlayerId company, ControlBinding target, ControlAction action)
            => _declaration.CheckBinding(world, company, target, action);
        public Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action)
            => _declaration.CheckReplacement(world, company, target, source, destination, action);
        public ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination)
            => _declaration.RebindTarget(world, company, target, source, destination);
        public Command CreateCommand(ControlActionRequest request) => _declaration.CreateCommand(request);
    }

    public sealed class DeclaredStockActionDef : Def, IControlActionPolicy
    {
        private readonly StockControlActionDeclaration _declaration = new StockControlActionDeclaration();
        public string CommandId => _declaration.CommandId;
        public string CommandOwner => _declaration.CommandOwner;
        public ControlTargetKind TargetKind => _declaration.TargetKind;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateComposition(World world) => _declaration.ValidateComposition(world, Id);
        public void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination)
            => _declaration.CopyTargetIds(world, company, entity, destination);
        public Code CheckBinding(World world, PlayerId company, ControlBinding target, ControlAction action)
            => _declaration.CheckBinding(world, company, target, action);
        public Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action)
            => _declaration.CheckReplacement(world, company, target, source, destination, action);
        public ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination)
            => _declaration.RebindTarget(world, company, target, source, destination);
        public Command CreateCommand(ControlActionRequest request) => _declaration.CreateCommand(request);
    }

    public sealed class ResourceControlDeclarationProofModule : GameModule
    {
        public const string Document = "resource-control-declarations.json";
        private readonly InitializeProbe _initialize;
        public ResourceControlDeclarationProofModule(bool declarations, InitializeProbe initialize = null)
        { UseDeclarations = declarations; _initialize = initialize ?? new InitializeProbe(); }
        public bool UseDeclarations { get; }
        public int DeclaredCount { get; private set; }
        public int NativeCount { get; private set; }
        public override string Id => "proof.resource-control-declarations";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(AutomationModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<DeclaredOutletReserveActionDef>("declared_outlet_reserve_control_actions");
            types.Section<DeclaredStockActionDef>("declared_stock_control_actions");
        }
        public override void Compose(WorldComposer composer)
        {
            DeclaredCount = composer.Content.All<DeclaredOutletReserveActionDef>().Count + composer.Content.All<DeclaredStockActionDef>().Count;
            NativeCount = composer.Content.All<OutletReserveControlActionPolicyDef>().Count + composer.Content.All<StockControlActionPolicyDef>().Count;
            composer.AddSystem(_initialize, 301);
        }
    }

    public sealed class DeclaredOperationControlActionDef : Def, IControlActionPolicy
    {
        private readonly OperationControlActionDeclaration _declaration = new OperationControlActionDeclaration();
        public string CommandId => _declaration.CommandId;
        public string CommandOwner => _declaration.CommandOwner;
        public ControlTargetKind TargetKind => _declaration.TargetKind;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        public void ValidateComposition(World world) => _declaration.ValidateComposition(world, Id);
        public void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination)
            => _declaration.CopyTargetIds(world, company, entity, destination);
        public Code CheckBinding(World world, PlayerId company, ControlBinding target, ControlAction action)
            => _declaration.CheckBinding(world, company, target, action);
        public Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action)
            => _declaration.CheckReplacement(world, company, target, source, destination, action);
        public ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination)
            => _declaration.RebindTarget(world, company, target, source, destination);
        public Command CreateCommand(ControlActionRequest request) => _declaration.CreateCommand(request);
    }

    public sealed class DeclaredAlertControlActionDef : Def, IControlActionPolicy
    {
        private readonly CompanyAlertControlActionDeclaration _declaration = new CompanyAlertControlActionDeclaration();
        public string CommandId => _declaration.CommandId;
        public string CommandOwner => _declaration.CommandOwner;
        public ControlTargetKind TargetKind => _declaration.TargetKind;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateComposition(World world) => _declaration.ValidateComposition(world, Id);
        public void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination)
            => _declaration.CopyTargetIds(world, company, entity, destination);
        public Code CheckBinding(World world, PlayerId company, ControlBinding target, ControlAction action)
            => _declaration.CheckBinding(world, company, target, action);
        public Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action)
            => _declaration.CheckReplacement(world, company, target, source, destination, action);
        public ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination)
            => _declaration.RebindTarget(world, company, target, source, destination);
        public Command CreateCommand(ControlActionRequest request) => _declaration.CreateCommand(request);
    }

    public sealed class DeclaredControlInputRecoveryDef : Def, IControlInputRecoveryPolicy
    {
        private readonly ControlInputRecoveryDeclaration _declaration = new ControlInputRecoveryDeclaration();
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        public ControlInputRecoveryAction Decide(ControlInputFailure failure) => _declaration.Decide(failure);
    }

    // Direct file-authored conditions, sharing the engine parser/compiler/evaluator, not another rule runtime.
    public sealed class DeclaredControlRuleDef : Def, IControlRule
    {
        private readonly ControlRuleDeclaration _declaration = new ControlRuleDeclaration();
        public IControlInputRecoveryPolicy InputRecovery => _declaration.InputRecovery;
        public IControlActionPolicy ActionPolicy => _declaration.ActionPolicy;
        public IReadOnlyList<ControlSignalInput> Inputs => _declaration.Inputs;
        public IReadOnlyDictionary<string, IControlThresholdDefinition> Thresholds => _declaration.Thresholds;
        public IReadOnlyDictionary<string, bool> InitialState => _declaration.InitialState;
        public bool InitialLatch => _declaration.InitialLatch;
        public GameDuration Dwell => _declaration.Dwell;
        public GameDuration Cooldown => _declaration.Cooldown;
        public ControlAction WhenTrue => _declaration.WhenTrue;
        public ControlAction WhenFalse => _declaration.WhenFalse;
        public ControlRuleArbitration Arbitration => _declaration.Arbitration;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public ControlEvaluation Evaluate(ControlEvaluationContext context) => _declaration.Evaluate(context);
    }

    // Independent metadata only. Native evaluation, commands and recovery retain all gameplay rules.
    public sealed class IndependentControlRuleDef : Def, IControlRule, IDependencyNode
    {
        internal Action ObserveProjection; // Explicitly armed negative metadata read only.
        private string _rule, _action, _recovery;
        public IControlRule Delegate { get; private set; }
        public IControlActionPolicy ActionPolicy { get; private set; }
        public IControlInputRecoveryPolicy InputRecovery { get; private set; }
        public IReadOnlyList<string> Dependencies { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _rule = reader.String("rule"); _action = reader.String("action_policy"); _recovery = reader.String("input_recovery");
            Dependencies = Array.AsReadOnly(new[] { _rule });
        }
        protected override void ResolveCore(ContentResolver content)
        {
            Delegate = content.Def<IControlRule>(_rule);
            ActionPolicy = content.Def<IControlActionPolicy>(_action);
            InputRecovery = content.Def<IControlInputRecoveryPolicy>(_recovery);
        }
        public IReadOnlyList<ControlSignalInput> Inputs => Delegate.Inputs;
        public IReadOnlyDictionary<string, IControlThresholdDefinition> Thresholds => Delegate.Thresholds;
        public IReadOnlyDictionary<string, bool> InitialState => Delegate.InitialState;
        public bool InitialLatch => Delegate.InitialLatch;
        public GameDuration Dwell => Delegate.Dwell;
        public GameDuration Cooldown { get { ObserveProjection?.Invoke(); return Delegate.Cooldown; } }
        public ControlAction WhenTrue => Delegate.WhenTrue;
        public ControlAction WhenFalse => Delegate.WhenFalse;
        public ControlRuleArbitration Arbitration => Delegate.Arbitration;
        public ControlEvaluation Evaluate(ControlEvaluationContext context) => Delegate.Evaluate(context);
    }
    public sealed class IndependentControlRecoveryDef : Def, IControlInputRecoveryPolicy, IDependencyNode
    {
        private string _policy;
        private IControlInputRecoveryPolicy _delegate;
        public IReadOnlyList<string> Dependencies { get; private set; }
        protected override void ReadCore(JsonReader reader)
        { _policy = reader.String("policy"); Dependencies = Array.AsReadOnly(new[] { _policy }); }
        protected override void ResolveCore(ContentResolver content) => _delegate = content.Def<IControlInputRecoveryPolicy>(_policy);
        public ControlInputRecoveryAction Decide(ControlInputFailure failure) => _delegate.Decide(failure);
    }
    public sealed class IndependentControlActionDef : Def, IControlActionPolicy, IDependencyNode
    {
        internal Action ObserveTargets; // Explicitly armed negative callback only.
        private string _policy, _replacementMode;
        private IControlActionPolicy _delegate;
        public IReadOnlyList<string> Dependencies { get; private set; }
        public IReadOnlyList<IContentDefinition> ContentDependencies { get; private set; }
        public string CommandId => _delegate.CommandId;
        public string CommandOwner => _delegate.CommandOwner;
        public ControlTargetKind TargetKind => _delegate.TargetKind;
        protected override void ReadCore(JsonReader reader)
        {
            _policy = reader.String("policy"); Dependencies = Array.AsReadOnly(new[] { _policy });
            _replacementMode = reader.String("replacement_mode", "normal");
            if (_replacementMode != "normal" && _replacementMode != "deny" && _replacementMode != "mutate-check" && _replacementMode != "redirect")
                throw reader.Error("invalid replacement proof mode");
        }
        protected override void ResolveCore(ContentResolver content) => _delegate = content.Def<IControlActionPolicy>(_policy);
        // Capturing a dependency's metadata belongs after all content resolution, not resolve order.
        public void ValidateComposition(World world)
        {
            _delegate.ValidateComposition(world);
            var dependencies = new List<IContentDefinition> { _delegate };
            foreach (var dependency in _delegate.ContentDependencies) if (!dependencies.Contains(dependency)) dependencies.Add(dependency);
            dependencies.Sort((a,b) => string.CompareOrdinal(a.Id,b.Id)); ContentDependencies = dependencies.AsReadOnly();
        }
        public void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination)
        { _delegate.CopyTargetIds(world, company, entity, destination); ObserveTargets?.Invoke(); }
        public Code CheckBinding(World world, PlayerId company, ControlBinding target, ControlAction action)
            => _delegate.CheckBinding(world, company, target, action);
        public Command CreateCommand(ControlActionRequest request) => _delegate.CreateCommand(request);
        public Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action)
        {
            if (_replacementMode == "mutate-check") source.Touch();
            return _replacementMode == "deny" ? new Code("proof.control-replacement-denied") :
                _delegate.CheckReplacement(world, company, target, source, destination, action);
        }
        public ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination)
            => _replacementMode == "redirect" ? new ControlBinding(source.Id, target.Capability) :
                _delegate.RebindTarget(world, company, target, source, destination);
    }
    public class ControlContractModule : GameModule
    {
        private readonly InitializeProbe _initialize;
        public ControlContractModule(InitializeProbe initialize) { _initialize = initialize; }
        public override string Id => "proof.control-contracts";
        public override ModuleVersion Version => new ModuleVersion(1,0,0);
        public bool IndependentAliases { get; private set; }
        public int DeclaredRuleCount { get; private set; }
        public int NativeRuleCount { get; private set; }
        public int DeclaredActionCount { get; private set; }
        public int NativeOperationAndAlertCount { get; private set; }
        public int DeclaredRecoveryCount { get; private set; }
        public int NativeRecoveryCount { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentControlRuleDef>("independent_control_rules");
            types.Section<DeclaredControlRuleDef>("declared_control_rules");
            types.Section<DeclaredOperationControlActionDef>("declared_operation_control_actions");
            types.Section<DeclaredAlertControlActionDef>("declared_alert_control_actions");
            types.Section<DeclaredControlInputRecoveryDef>("declared_input_recovery");
            types.Section<IndependentControlActionDef>("independent_control_actions");
            types.Section<IndependentControlRecoveryDef>("independent_control_recovery");
        }
        public override void Compose(WorldComposer composer)
        {
            var content = composer.Content;
            var rule = content.Get<IControlRule>("contract.incident-alert");
            var action = content.Get<IControlActionPolicy>("contract.alert");
            var recovery = content.Get<IControlInputRecoveryPolicy>("contract.inputs");
            IndependentAliases = rule.GetType().BaseType == typeof(Def) && action.GetType().BaseType == typeof(Def) && recovery.GetType().BaseType == typeof(Def)
                && content.Owns(rule) && content.Owns(action) && content.Owns(recovery);
            DeclaredRuleCount = content.All<DeclaredControlRuleDef>().Count;
            NativeRuleCount = content.All<AuthoredControlRuleDef>().Count;
            DeclaredActionCount = content.All<DeclaredOperationControlActionDef>().Count + content.All<DeclaredAlertControlActionDef>().Count;
            NativeOperationAndAlertCount = content.All<OperationControlActionPolicyDef>().Count + content.All<CompanyAlertControlActionPolicyDef>().Count;
            DeclaredRecoveryCount = content.All<DeclaredControlInputRecoveryDef>().Count;
            NativeRecoveryCount = content.All<AuthoredControlInputRecoveryPolicyDef>().Count;
            composer.AddSystem(_initialize,301);
        }
    }
}
