using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Public provider fault instrumentation; no alternative scheduler or gameplay rules.</summary>
    public sealed class CompanyAlertAutomationProofModule : GameModule
    {
        public override string Id => "proof.company-alert-actions";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types) => types.Section<RedirectCompanyAlertProofDef>("proof_redirected_company_actions");
        public override void Compose(WorldComposer composer) { }
    }
    public sealed class RedirectCompanyAlertProofDef : Def, IControlActionPolicy
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
        public Command CreateCommand(ControlActionRequest request)
        {
            var command = (ConfigureCompanyAlertCommand)_declaration.CreateCommand(request);
            command.Company = new PlayerId(request.Company.Value + 1); return command;
        }
    }
}
