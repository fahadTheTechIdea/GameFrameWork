using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent JSON metadata reuses the original engine cursor/codec, not a scheduler or stock store.
    public sealed class IndependentLineWorkDef : ComponentDef<LineWork>, ILineWorkDefinition
    {
        protected override void ReadCore(JsonReader reader) { }
        public void ValidateBinding(EntityDef owner) => LineWorkContracts.ValidateBinding(owner);
        protected override void ValidateCore(EntityDef owner) => ValidateBinding(owner);
    }
    public sealed class AuthoredLineCursorModule : GameModule
    {
        public override string Id => "proof.authored-line-cursors";
        public override ModuleVersion Version => new ModuleVersion(1,0,0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {new ModuleRequirement(InfrastructureModule.ModuleId,Version)};
        public readonly InitializeProbe Initialization = new InitializeProbe();
        public bool ActualBinding { get; private set; }
        public override void DeclareContent(ContentTypes types) => types.Component<IndependentLineWorkDef>("proof_line_work",3);
        public override void Compose(WorldComposer composer)
        {
            var line=composer.Content.Get<ILineDefinition>("segment.road-line");
            ActualBinding=line.WorkMarker.Id=="cursor.road" && line.WorkMarker.HasCapability<ILineWork>() &&
                line.WorkMarker.Component<ILineWorkDefinition>()!=null;
            composer.AddSystem(Initialization,306);
        }
    }
}
