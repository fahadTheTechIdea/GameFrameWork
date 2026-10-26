using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentEntityUnlockDef : Def, ITechnologyUnlock
    {
        private readonly TechnologyUnlockDeclaration<EntityDef> _declaration = new TechnologyUnlockDeclaration<EntityDef>();
        public EntityDef Target => _declaration.Target;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader, "definition");
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public bool Matches(World world, IContentDefinition definition)
        {
            world.Services.Get<TechnologyUnlockContractModule>().Observe(world);
            return _declaration.Matches(definition);
        }
    }
    public sealed class IndependentRecipeUnlockDef : Def, ITechnologyUnlock
    {
        private readonly TechnologyUnlockDeclaration<ITransformationDefinition> _declaration = new TechnologyUnlockDeclaration<ITransformationDefinition>();
        public ITransformationDefinition Target => _declaration.Target;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader, "definition");
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public bool Matches(World world, IContentDefinition definition)
        {
            world.Services.Get<TechnologyUnlockContractModule>().Observe(world);
            return _declaration.Matches(definition);
        }
    }
    public sealed class ContextActivityUnlockDef : Def, ITechnologyUnlock
    {
        private readonly TechnologyUnlockDeclaration<IAuthoredActivityDefinition> _declaration = new TechnologyUnlockDeclaration<IAuthoredActivityDefinition>();
        public IAuthoredActivityDefinition Target => _declaration.Target;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader, "definition");
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public bool Matches(World world, IContentDefinition definition)
        {
            world.Services.Get<TechnologyUnlockContractModule>().Observe(world);
            return _declaration.Matches(definition);
        }
    }
    // Invalid immutable metadata probe, never registered as a state owner.
    public sealed class StatefulUnlockDef : Def, ITechnologyUnlock, IPersistentService
    {
        private bool _state;
        public int StateSchemaVersion => 1;
        protected override void ReadCore(JsonReader reader) { }
        public bool Matches(World world, IContentDefinition definition) => false;
        public void Sync(IStateSync sync) => sync.Bool(ref _state);
    }
    public enum UnlockProbeFault { Exception, PauseClock, AllocateIdentity }
    public sealed class TechnologyUnlockContractModule : GameModule
    {
        public const string ModuleId = "proof.unlock-contracts";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(TechnologyModule.ModuleId, Version),
            new ModuleRequirement(TransformationModule.ModuleId, Version) };
        public bool ActualAliases, Independent, DirectDefinitions, ExactTargets;
        public InitializeProbe Initialize { get; private set; }
        private UnlockProbeFault? _fault;
        public int Reads;
        public bool MutationCompleted;
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentEntityUnlockDef>("independent_entity_unlocks");
            types.Section<ContextActivityUnlockDef>("context_activity_unlocks");
            types.Section<IndependentRecipeUnlockDef>("independent_recipe_unlocks");
            types.Section<StatefulUnlockDef>("stateful_unlocks");
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Bind(this);
            var content = composer.Content;
            var entity = content.Get<ITechnologyUnlock>("unlock.entity");
            var activity = content.Get<ITechnologyUnlock>("unlock.activity");
            var recipe = content.Get<ITechnologyUnlock>("unlock.recipe");
            var research = content.Get<ITechnologyDefinition>("research.foundation");
            ActualAliases = research.Unlocks.Count == 3 && ReferenceEquals(entity, research.Unlocks[0])
                && ReferenceEquals(activity, research.Unlocks[1]) && ReferenceEquals(recipe, research.Unlocks[2]);
            Independent = recipe is IndependentRecipeUnlockDef;
            DirectDefinitions = typeof(EntityTechnologyUnlockDef).BaseType == typeof(Def) &&
                typeof(ActivityTechnologyUnlockDef).BaseType == typeof(Def) &&
                typeof(TransformationTechnologyUnlockDef).BaseType == typeof(Def);
            ExactTargets = (entity is EntityTechnologyUnlockDef nativeEntity && ReferenceEquals(nativeEntity.Target, content.Get<EntityDef>("point.tower")) ||
                entity is IndependentEntityUnlockDef independentEntity && ReferenceEquals(independentEntity.Target, content.Get<EntityDef>("point.tower"))) &&
                (activity is ActivityTechnologyUnlockDef nativeActivity && ReferenceEquals(nativeActivity.Target, content.Get<IAuthoredActivityDefinition>("inspect")) ||
                activity is ContextActivityUnlockDef independentActivity && ReferenceEquals(independentActivity.Target, content.Get<IAuthoredActivityDefinition>("inspect"))) &&
                (recipe is TransformationTechnologyUnlockDef nativeRecipe && ReferenceEquals(nativeRecipe.Target, content.Get<ITransformationDefinition>("contract.make")) ||
                recipe is IndependentRecipeUnlockDef independentRecipe && ReferenceEquals(independentRecipe.Target, content.Get<ITransformationDefinition>("contract.make")));
            Initialize = new InitializeProbe("proof.unlock-initialize"); composer.AddSystem(Initialize, 12348);
        }
        public void Arm(UnlockProbeFault fault) { _fault = fault; Reads = 0; MutationCompleted = false; }
        internal void Observe(World world)
        {
            if (!_fault.HasValue || Reads != 0) return;
            Reads++;
            if (_fault == UnlockProbeFault.Exception) throw new InvalidOperationException("proof.unlock-callback");
            if (_fault == UnlockProbeFault.PauseClock) ((GameClock)world.Clock).Pause();
            if (_fault == UnlockProbeFault.AllocateIdentity)
            {
                EntityDef prototype = world.Content.Get<EntityDef>("contract.result");
                world.Spawn(prototype, new PlayerId(0));
            }
            MutationCompleted = true;
        }
    }
}
