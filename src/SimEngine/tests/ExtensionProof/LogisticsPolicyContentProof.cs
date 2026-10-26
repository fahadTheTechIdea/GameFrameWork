using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent immutable metadata, sharing the engine declaration and physical owners.</summary>
    public sealed class IndependentLogisticsPolicyDef : Def, ILogisticsPolicyDefinition
    {
        private LogisticsPolicyDeclaration _declaration;
        private string _fault;
        internal Action ObserveStates;
        public IReadOnlyList<StockPolicyEntry> Stock => _fault == "null-stock" ? null :
            _fault == "duplicate-stock" ? Array.AsReadOnly(new[] { _declaration.Stock[0], _declaration.Stock[0] }) : _declaration.Stock;
        public OutletPolicySettings Outlet => _fault == "null-outlet" ? null : _declaration.Outlet;
        public EntityStateBindings States { get { ObserveStates?.Invoke(); return _fault == "null-states" ? null : _declaration.States; } }
        protected override void ReadCore(JsonReader reader)
        {
            _fault = reader.String("proof_fault");
            if (_fault != "none" && _fault != "null-stock" && _fault != "duplicate-stock" && _fault != "null-outlet" && _fault != "null-states")
                throw reader.Error("unknown metadata proof fault");
            _declaration = LogisticsPolicyDeclaration.Read(reader);
        }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content) => LogisticsPolicyDeclaration.Validate(content.Content, this);
    }
    public sealed class LogisticsPolicyContentModule : GameModule
    {
        private World _world;
        public InitializeProbe Initialize { get; } = new InitializeProbe();
        public override string Id => "proof.logistics-policy-content";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(LogisticsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes content) => content.Section<IndependentLogisticsPolicyDef>("independent_logistics_policies");
        public override void Compose(WorldComposer composer) { _world = composer.World; composer.AddSystem(Initialize, 306); }
        // Negative observation injection only; no stock, dispatch, game loop or gameplay owner.
        public void Arm(string profile, string mode)
        {
            World actual = _world;
            actual.Content.Get<IndependentLogisticsPolicyDef>(profile).ObserveStates = () => {
                if (mode == "clock" || mode == "throw-after-clock") ((GameClock)actual.Clock).Speed = 2;
                if (mode == "throw" || mode == "throw-after-clock") throw new InvalidOperationException("proof.logistics-policy-first-error");
            };
        }
    }
}
