using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent authored metadata delegates the sole reader. No runtime entity state or update logic.
    public sealed class IndependentEntityStateDef : Def, IEntityStateDefinition
    {
        private readonly EntityStateDeclaration _declaration = new EntityStateDeclaration();
        private string _fault;
        // Negative metadata callback instrumentation only; no authored/runtime state owner.
        public Action OnPriorityRead;
        public Code State => _fault == "empty-state" ? Code.None : _declaration.State;
        public int Priority
        {
            get { OnPriorityRead?.Invoke(); return _fault == "throw-priority" ? throw new InvalidOperationException("proof.state.priority") : _declaration.Priority; }
        }
        public bool Default => _declaration.Default;
        protected override void ReadCore(JsonReader reader)
        { _declaration.Read(reader); _fault = reader.String("fault", "none"); }
    }
    public sealed class EntityStateContentModule : GameModule
    {
        public override string Id => "proof.entity-state-content";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        // Non-authoritative admission instrumentation, not another system or saved gameplay owner.
        public int Compositions;
        public bool PauseOnStoppedMetadata;
        public override void DeclareContent(ContentTypes content) => content.Section<IndependentEntityStateDef>("independent_entity_states");
        public override void Compose(WorldComposer composer)
        {
            Compositions++;
            if (composer.Content.All<EntityStateDef>().Count != 0 || composer.Content.All<IEntityStateDefinition>().Count != 5)
                throw new ContentException("proof requires only the five independently authored state aliases");
            if (PauseOnStoppedMetadata)
                composer.Content.Get<IndependentEntityStateDef>("space.stopped").OnPriorityRead = () => ((Beep.SimEngine.Hosting.GameClock)composer.World.Clock).Pause();
        }
    }
}
