using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // New technology content uses ordinary authored activities, not native technology inheritance.
    public sealed class IndependentTechnologyDef : ActivityDef, ITechnologyDefinition
    {
        private readonly TechnologyDeclaration _declaration = new TechnologyDeclaration();
        public IReadOnlyList<string> Dependencies => _declaration.Dependencies;
        public override IReadOnlyList<IContentDefinition> DisclosureContent => _declaration.DisclosureContent(this);
        public IReadOnlyList<ITechnologyUnlock> Unlocks => _declaration.Unlocks;
        protected override void ReadCore(JsonReader reader)
        {
            base.ReadCore(reader);
            _declaration.Read(reader, this);
        }
        protected override void ResolveCore(ContentResolver content)
        {
            base.ResolveCore(content);
            _declaration.Resolve(content);
        }
        public override Code CheckIntent(World world, PlayerId company, Entity target)
        {
            Code ordinary = base.CheckIntent(world, company, target);
            return ordinary.IsNone ? world.Services.Get<ITechnologyKnowledge>().CheckResearch(company, this) : ordinary;
        }
    }
    public sealed class IndependentActivityUnlockDef : Def, ITechnologyUnlock
    {
        private readonly TechnologyUnlockDeclaration<IAuthoredActivityDefinition> _declaration = new TechnologyUnlockDeclaration<IAuthoredActivityDefinition>();
        public IAuthoredActivityDefinition Activity => _declaration.Target;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader, "activity");
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public bool Matches(World world, IContentDefinition definition) => _declaration.Matches(definition);
    }

    /// <summary>Detached metadata admission facts only; no learned/work/clock state.</summary>
    public sealed class TechnologyContractModule : GameModule
    {
        public const string ModuleId = "proof.technology-contracts";
        private readonly InitializeProbe _initialize;
        public TechnologyContractModule(InitializeProbe initialize) { _initialize = initialize; }
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {
            new ModuleRequirement(TechnologyModule.ModuleId, Version), new ModuleRequirement(ObjectiveModule.ModuleId, Version) });
        public bool IndependentTechnology { get; private set; }
        public bool IndependentUnlock { get; private set; }
        public bool NativeLinks { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentTechnologyDef>("independent_technologies");
            types.Section<IndependentActivityUnlockDef>("independent_activity_unlocks");
        }
        public override void Compose(WorldComposer composer)
        {
            var content = composer.Content; var technology = content.Get<ITechnologyDefinition>("research.external");
            IndependentTechnology = !(technology is TechnologyDef) && ReferenceEquals(technology, content.Get<IndependentTechnologyDef>(technology.Id)) && content.Owns(technology);
            var unlock = content.Get<ITechnologyUnlock>("unlock.external");
            IndependentUnlock = unlock is IndependentActivityUnlockDef && ReferenceEquals(unlock, technology.Unlocks[0]) && content.Owns(unlock);
            NativeLinks = content.Get<ITechnologyDefinition>("research.foundation").Dependencies[0] == technology.Id
                && ReferenceEquals(content.Get<TechnologyRequirementDef>("knowledge.external").Technology, technology)
                && ReferenceEquals(content.Get<TechnologyObjectivePredicateDef>("objective.external").Technology, technology);
            composer.AddSystem(_initialize, 301);
        }
    }
}

