using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Negative malformed-contract instrumentation only, never a normal objective predicate.
    public sealed class MalformedDisclosurePredicate : Def, IObjectivePredicate
    {
        private string _mode;
        private IReadOnlyList<IContentDefinition> _content;
        private string[] _references;
        public IReadOnlyList<string> Dependencies => Array.Empty<string>();
        public IReadOnlyList<IContentDefinition> ObservationContent => _mode == "null" ? null : _content;
        protected override void ReadCore(JsonReader reader)
        { _mode = reader.String("mode"); if (reader.Array("references") == null) throw reader.Error("explicit references required"); _references = reader.Strings("references"); }
        protected override void ResolveCore(ContentResolver resolver)
        {
            var content = new List<IContentDefinition>();
            foreach (string id in _references) content.Add(resolver.Def<IContentDefinition>(id));
            if (_mode == "null-entry") content.Add(null);
            if (_mode == "foreign") content.Add(new ForeignContent());
            _content = content.AsReadOnly(); _references = null;
        }
        public void ValidateBinding(World world) { }
        public ObjectiveObservation Observe(ObjectiveContext context, IReadOnlyDictionary<string, ObjectiveObservation> children)
            => throw new InvalidOperationException("unused malformed declaration must never execute");
        private sealed class ForeignContent : IContentDefinition
        { public string Id => "decl.native"; public string Module => TechnologyModule.ModuleId; }
    }
    public sealed class ProgressionDisclosureModule : GameModule
    {
        public override string Id => "proof.progression-disclosure";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(ObjectiveModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<ForwardObjectivePredicateDef>("progression_forward_predicates");
            types.Section<MalformedDisclosurePredicate>("malformed_disclosure_predicates");
        }
    }
}
