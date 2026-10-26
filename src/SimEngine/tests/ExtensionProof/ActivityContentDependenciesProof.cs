using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Reuses existing direct providers; no scheduler, stock, history or state book.
    public sealed class ActivityContentDependenciesModule : GameModule
    {
        public const string ModuleId = "proof.activity-content";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public InitializeProbe Initialize { get; } = new InitializeProbe(ModuleId + ".initialize");
        public bool ActualDirectAliases { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentTargetResourceDef>("dependency_direct_resources");
            types.Section<CompletionEffectDef>("dependency_direct_effects");
            types.Section<InvalidContentDependenciesDef>("dependency_invalid_effects");
        }
        public override void Compose(WorldComposer composer)
        {
            var requirement = composer.Content.Get<IWorkRequirement>("deps.external-materials");
            var effect = composer.Content.Get<IWorkEffect>("deps.external-finish");
            var resource = composer.Content.Get<IResourceContentDefinition>("supply");
            ActualDirectAliases = !(requirement is WorkRequirementDef) && effect is CompletionEffectDef &&
                ReferenceEquals(requirement.ContentDependencies[0], resource) && ReferenceEquals(effect.ContentDependencies[0], resource);
            composer.AddSystem(Initialize, 306);
        }
    }

    // Negative metadata instrumentation only; it must fail before Initialize.
    public sealed class InvalidContentDependenciesDef : Def, IWorkEffect
    {
        private string _mode, _resource;
        private IResourceContentDefinition _resolved;
        private sealed class ForeignAlias : IContentDefinition
        { public string Id => "foreign.alias"; public string Module => "foreign.module"; }
        public IReadOnlyList<IContentDefinition> ContentDependencies
        {
            get
            {
                if (_mode == "null") return null;
                if (_mode == "duplicate") return Array.AsReadOnly(new IContentDefinition[] { _resolved, _resolved });
                return Array.AsReadOnly(new IContentDefinition[] { new ForeignAlias() });
            }
        }
        protected override void ReadCore(JsonReader reader)
        {
            _mode = reader.String("mode"); _resource = reader.String("resource");
            if (_mode != "null" && _mode != "duplicate" && _mode != "foreign") throw reader.Error("explicit metadata fault mode required");
        }
        protected override void ResolveCore(ContentResolver content) => _resolved = content.Def<IResourceContentDefinition>(_resource);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => WorkEffectResult.Failed(new Code("proof.invalid-dependencies-executed"));
    }
}
