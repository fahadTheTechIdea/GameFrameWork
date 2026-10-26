using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentAlertProfileDef : Def, ICompanyAlertProfileDefinition
    {
        private CompanyAlertProfileDeclaration _declaration;
        public IReadOnlyList<string> DispatchScopes => _declaration.DispatchScopes;
        protected override void ReadCore(JsonReader reader) => _declaration = CompanyAlertProfileDeclaration.Read(reader);
        public long PriorityAdjustment(CompanyAlertLevel level, WorkerDispatchOffer offer) => _declaration.PriorityAdjustment(level, offer);
    }
    // Content-only extension; the existing base contributes one diagnostic Initialize probe, no gameplay.
    public sealed class AlertContractModule : ControlContractModule
    {
        public AlertContractModule(InitializeProbe initialize) : base(initialize) { }
        public bool IndependentAlert { get; private set; }
        public override void DeclareContent(ContentTypes types)
        { base.DeclareContent(types); types.Section<IndependentAlertProfileDef>("independent_alert_profiles"); }
        public override void Compose(WorldComposer composer)
        {
            base.Compose(composer);
            var profile = composer.Content.Get<ICompanyAlertProfileDefinition>("contract.alert-response");
            IndependentAlert = !(profile is CompanyAlertProfileDef) && composer.Content.Owns(profile);
        }
    }
}
