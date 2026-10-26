using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Actual direct JSON alias; no journal, stock, update, transaction or gameplay implementation.
    public sealed class DirectFlowJournalDef : Def, IResourceFlowJournalDefinition
    {
        private ResourceFlowJournalDeclaration _declaration;
        public int RetainedRecordsPerCompany => _declaration.RetainedRecordsPerCompany;
        protected override void ReadCore(JsonReader reader) => _declaration = ResourceFlowJournalDeclaration.Read(reader);
    }
    // Negative metadata only; never selected in a positive physical session.
    public sealed class BrokenFlowJournalDef : Def, IResourceFlowJournalDefinition
    {
        private ResourceFlowJournalDeclaration _declaration;
        private string _mode;
        string IContentDefinition.Id => _mode == "identity" ? "missing.policy" : Id;
        string IContentDefinition.Module => _mode == "owner" ? "missing.module" : Module;
        public int RetainedRecordsPerCompany => _mode == "zero" ? 0 : _mode == "negative" ? -1
            : _mode == "throw" ? throw new InvalidOperationException("proof.flow-policy-first-error") : _declaration.RetainedRecordsPerCompany;
        protected override void ReadCore(JsonReader reader)
        {
            _declaration = ResourceFlowJournalDeclaration.Read(reader); _mode = reader.String("mode");
            if (_mode != "zero" && _mode != "negative" && _mode != "identity" && _mode != "owner" && _mode != "throw")
                throw reader.Error("explicit negative probe mode required");
        }
    }
    public sealed class ResourceFlowContentModule : GameModule
    {
        public override string Id => "proof.resource-flow-content";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {new ModuleRequirement(ResourceFlowsModule.ModuleId, Version)});
        public readonly InitializeProbe Initialize = new InitializeProbe("proof.flow-policy.initialize");
        public override void DeclareContent(ContentTypes types)
        { types.Section<DirectFlowJournalDef>("direct_flow_journals"); types.Section<BrokenFlowJournalDef>("broken_flow_journals"); }
        public override void Compose(WorldComposer composer) => composer.AddSystem(Initialize, 306);
    }
}
