using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Unused malformed metadata instrumentation; healthy execution stays in original engine owners.
    public sealed class MalformedActivityDisclosureDef : ActivityDef
    {
        private string _mode;
        private string[] _references;
        private IReadOnlyList<IContentDefinition> _content;
        public override IReadOnlyList<IContentDefinition> DisclosureContent => _mode == "self" ? base.DisclosureContent : _mode == "null" ? null : _content;
        protected override void ReadCore(JsonReader reader)
        {
            base.ReadCore(reader); _mode = reader.String("mode");
            if (reader.Array("references") == null) throw reader.Error("explicit references required");
            _references = reader.Strings("references");
        }
        protected override void ResolveCore(ContentResolver resolver)
        {
            base.ResolveCore(resolver);
            var references = new List<IContentDefinition>();
            foreach (string id in _references) references.Add(resolver.Def<IContentDefinition>(id));
            if (_mode == "null-entry") references.Add(null);
            if (_mode == "foreign") references.Add(new ForeignContent());
            _content = references.AsReadOnly(); _references = null;
        }
        private sealed class ForeignContent : IContentDefinition
        { public string Id => "decl.native"; public string Module => TechnologyModule.ModuleId; }
    }
    public sealed class ActivityDisclosureModule : GameModule
    {
        public override string Id => "proof.activity-disclosure";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<MalformedActivityDisclosureDef>("malformed_activity_disclosure");
            types.Section<DeclaredPlanDef>("disclosure_plan_bindings");
        }
        public override void Compose(WorldComposer composer)
        {
            var source = new DeclaredPlanSource(composer.Content);
            composer.Services.Bind(source); composer.Services.Bind<IDeclaredPlanSource>(source);
        }
    }
}
