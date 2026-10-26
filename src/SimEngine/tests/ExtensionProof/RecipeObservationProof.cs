using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Transient observation probes, armed only AFTER original content validation/Initialize.
    // All healthy recipe metadata uses the original declaration and actual resource aliases.
    public sealed class ObservedRecipeDef : IndependentTransformationDef
    {
        internal Action<string> Observe;
        public override EntityDef EntityOutput { get { Observe?.Invoke("output"); return base.EntityOutput; } }
        public override Fix Cash { get { Observe?.Invoke("cost"); return base.Cash; } }
        public override Bill ToolConsumption { get { Observe?.Invoke("bill"); return base.ToolConsumption; } }
        public override IReadOnlyList<ResourcePortRoute> InputPorts { get { Observe?.Invoke("ports"); return base.InputPorts; } }
        public override IReadOnlyList<IResourceRate> ToolConsumptionRates
        {
            get
            {
                Observe?.Invoke("recipe");
                var original = base.ToolConsumptionRates;
                if (Observe == null) return original;
                var probes = new IResourceRate[original.Count];
                for (int i = 0; i < probes.Length; i++) probes[i] = new ObservedRate(original[i], Observe);
                return Array.AsReadOnly(probes);
            }
        }
        private sealed class ObservedRate : IResourceRate
        {
            private readonly IResourceRate _source; private readonly Action<string> _read;
            internal ObservedRate(IResourceRate source, Action<string> read) { _source = source; _read = read; }
            public IResourceDefinition Resource { get { _read("rate"); return new ObservedResource(_source.Resource, _read); } }
            public Fix Quantity => _source.Quantity;
            public long PeriodNumerator => _source.PeriodNumerator;
            public long PeriodDenominator => _source.PeriodDenominator;
            public DurationUnit PeriodUnit { get { _read("period"); return _source.PeriodUnit; } }
        }
        private sealed class ObservedResource : IResourceDefinition
        {
            private readonly IResourceDefinition _source; private readonly Action<string> _read;
            internal ObservedResource(IResourceDefinition source, Action<string> read) { _source = source; _read = read; }
            public string Id => _source.Id;
            public ResourceId Resource => _source.Resource;
            public string Category { get { _read("resource"); return _source.Category; } }
            public IUnitDefinition QuantityUnit => new ObservedUnit(_source.QuantityUnit, _read);
            public Code Phase => _source.Phase;
            public IReadOnlyList<string> Traits => _source.Traits;
            public IReadOnlyList<ResourceCapacityMeasure> CapacityMeasures => _source.CapacityMeasures;
            public bool HasTrait(string trait) => _source.HasTrait(trait);
        }
        private sealed class ObservedUnit : IUnitDefinition
        {
            private readonly IUnitDefinition _source; private readonly Action<string> _read;
            internal ObservedUnit(IUnitDefinition source, Action<string> read) { _source = source; _read = read; }
            public UnitId Unit { get { _read("unit"); return _source.Unit; } }
            public string Dimension => _source.Dimension;
            public long ScaleNumerator => _source.ScaleNumerator;
            public long ScaleDenominator => _source.ScaleDenominator;
            public bool Discrete => _source.Discrete;
        }
    }
    public sealed class RecipeObservationModule : GameModule
    {
        private World _world; private ObservedRecipeDef _recipe; private Action<string> _observe;
        public int Reads { get; private set; }
        public override string Id => "proof.recipe-observations";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires
            => new[] { new ModuleRequirement(TransformationModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<ObservedRecipeDef>("observed_recipes");
            types.Component<ObservedProcessorDef>("observed_processor", 9);
            types.Component<ManagementGateDef>("management_gate", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            _world = composer.World; _recipe = composer.Content.Get<ObservedRecipeDef>("contract.make");
            composer.Services.Bind<IVisibility>(new RecipeVisibility(this));
        }
        private ManagementGate Gate()
        {
            foreach (Entity entity in _world.Entities.All)
                if (entity.Def.Id == "contract.factory") return entity.Get<ManagementGate>();
            throw new InvalidOperationException("missing actual management gate probe");
        }
        public void ArmHiddenGate(string site) => Gate().HiddenRead = site;
        public void ArmMalformed(int mode) => Gate().Malformed = mode;
        public void Arm(string site, ResourceSnapshotFault mode)
        {
            ArmProbe(site, () =>
            {
                if (mode == ResourceSnapshotFault.Exception) throw new InvalidOperationException("proof.recipe.observation.failure");
                if (mode == ResourceSnapshotFault.PauseClock) ((GameClock)_world.Clock).Pause();
                if (mode == ResourceSnapshotFault.AllocateIdentity) _world.Spawn(_world.Content.Get<EntityDef>("source"), new PlayerId(0));
            });
        }
        // Explicit transient negative instrumentation; no authored or persistent gameplay behavior.
        public void ArmProbe(string site, Action probe)
        {
            if (probe == null) throw new ArgumentNullException(nameof(probe));
            SetObserver(at => { if (at == site && Reads == 0) { Reads++; probe(); } });
        }
        public void Disarm() => SetObserver(null);
        private void SetObserver(Action<string> observe)
        {
            _recipe.Observe = _observe = observe;
            var processor = _world.Content.Get<EntityDef>("contract.factory").Component<ITransformerDefinition>(new Code("contract.process"));
            if (processor is ObservedProcessorDef observed) observed.Observe = _observe;
            foreach (Entity entity in _world.Entities.All)
                if (entity.Def.Id == "contract.factory" && entity.Get<ManagementGate>() is ManagementGate gate) gate.Observe = _observe;
        }
        private sealed class RecipeVisibility : IVisibility
        {
            private readonly RecipeObservationModule _module;
            internal RecipeVisibility(RecipeObservationModule module) => _module = module;
            public bool CanSee(World world, PlayerId viewer, Entity entity) => entity.Owner == viewer;
            public bool CanSeeContent(World world, PlayerId viewer, IContentDefinition definition)
            {
                _module._observe?.Invoke("visibility");
                return definition.Id != "observation.hidden";
            }
        }
    }
}
