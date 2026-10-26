using System;
using System.Collections.Generic;
using System.Globalization;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class LayerAccessProbeDef : Def
    {
        public string Stage { get; private set; }
        public string Fault { get; private set; }
        public string HiddenResource { get; private set; }
        public string HiddenUnit { get; private set; }
        public int HiddenUnitViewer { get; private set; }
        public Fix FirstStart { get; private set; }
        public Fix FirstEnd { get; private set; }
        public Fix SecondStart { get; private set; }
        public Fix SecondEnd { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Stage = reader.String("stage"); Fault = reader.String("fault");
            var hidden = reader.Value("hidden_resource") ?? throw reader.Error("explicit hidden_resource or null required");
            HiddenResource = hidden.Kind == JsonKind.Null ? null : reader.String("hidden_resource");
            var unit = reader.Value("hidden_unit") ?? throw reader.Error("explicit hidden_unit or null required");
            HiddenUnit = unit.Kind == JsonKind.Null ? null : reader.String("hidden_unit");
            HiddenUnitViewer = reader.Int("hidden_unit_viewer");
            if (HiddenUnitViewer < 0 || HiddenUnitViewer > 1) throw reader.Error("unit visibility probe requires explicit first/second seat");
            FirstStart = reader.Fix("first_start"); FirstEnd = reader.Fix("first_end");
            SecondStart = reader.Fix("second_start"); SecondEnd = reader.Fix("second_end");
            if (Stage != "none" && Stage != "content-layer" && Stage != "content-unit" && Stage != "content-resource" &&
                Stage != "content-vertical" && Stage != "sample" && Stage != "keys") throw reader.Error("explicit layer callback stage required");
            if (Fault != "none" && Fault != "throw" && Fault != "state" && Fault != "clock" && Fault != "space" && Fault != "invalid-key")
                throw reader.Error("explicit layer callback fault required");
            if ((Stage == "none") != (Fault == "none") || Fault == "invalid-key" && Stage != "keys") throw reader.Error("layer probe stage/fault mismatch");
            if (FirstStart < Fix.Zero || FirstEnd < FirstStart || SecondStart < Fix.Zero || SecondEnd < SecondStart)
                throw reader.Error("explicit ordered coverage ranges required");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            if (HiddenResource != null) content.Def<IResourceContentDefinition>(HiddenResource);
            if (HiddenUnit != null) content.Def<IUnitContentDefinition>(HiddenUnit);
        }
    }
    public sealed class LayerAccessReadModel
    {
        private readonly World _world; private readonly PlayerId _viewer;
        public LayerAccessReadModel(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        private ISpatialLayer Find(string id)
        {
            _ = _world.ViewFor(_viewer).Space;
            foreach (var layer in ((ISpatialLayerProvider)_world.Space).SpatialLayers) if (layer.Id == id) return layer;
            return null;
        }
        public void AppendKeys(string id, IList<string> destination)
            => SpatialLayerAccess.CopyObservationKeys(_world, Find(id), _viewer, destination);
        public bool ObserveUnowned(string id)
            => SpatialLayerAccess.CanObserve(_world, new UnownedLayer(Find(id)), _viewer);
        // Negative-only capability proxy. It must never receive a CanRead callback.
        private sealed class UnownedLayer : ISpatialLayer
        {
            private readonly ISpatialLayer _source;
            public UnownedLayer(ISpatialLayer source) { _source = source ?? throw new ArgumentNullException(nameof(source)); }
            public bool CanRead(PlayerId viewer) => throw new InvalidOperationException("proof.unowned-layer-called");
            public string Id => _source.Id; public string Space => _source.Space; public Code Type => _source.Type;
            public SpatialLayerValueType ValueType => _source.ValueType; public SpatialLayerVisibility Visibility => _source.Visibility;
            public PlayerId Owner => _source.Owner; public IUnitDefinition Unit => _source.Unit;
            public IResourceDefinition Resource => _source.Resource; public SpatialResourceMeasure ResourceMeasure => _source.ResourceMeasure;
            public Code VerticalDomain => _source.VerticalDomain; public IUnitDefinition VerticalUnit => _source.VerticalUnit;
            public string VerticalReference => _source.VerticalReference; public Fix VerticalOffset => _source.VerticalOffset;
            public JsonValue Metadata => _source.Metadata;
        }
    }
    public sealed class LayerAccessProofModule : GameModule
    {
        public override string Id => "proof.layer-access";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement("points", Version) };
        public override void DeclareContent(ContentTypes content) => content.Section<LayerAccessProbeDef>("layer_access_probes");
        public override void Compose(WorldComposer composer)
        {
            var config = composer.Content.Get<LayerAccessProbeDef>("layer.access");
            var visibility = new ViewObservationVisibility(); // Same existing no-self-guard proof owner/codec.
            int unitCalls = 0;
            void Attack(World world, string stage)
            {
                // Negative-only public query injection, after real physical updates and explicit host pause.
                if (!world.Clock.Paused || world.Clock.StepIndex == 0 || config.Stage != stage) return;
                if (config.Fault == "throw") throw new InvalidOperationException("proof.layer-access-original");
                if (config.Fault == "state") visibility.Value++;
                if (config.Fault == "clock") ((GameClock)world.Clock).Resume();
                if (config.Fault == "space") ((PointSpace)world.Space).SetAccess(false);
            }
            visibility.ContentAllowed = (world, viewer, definition) => viewer == PlayerId.None ||
                (!(definition is IResourceDefinition) || definition.Id != config.HiddenResource) &&
                (!(definition is IUnitDefinition) || viewer.Value != config.HiddenUnitViewer || definition.Id != config.HiddenUnit);
            visibility.OnContent = (world, viewer, definition) =>
            {
                if (!world.Clock.Paused || world.Clock.StepIndex == 0) return;
                if (definition is ISpatialLayerDefinition) { unitCalls = 0; Attack(world, "content-layer"); }
                // Resource closure may visit other units between these actual repeated metre links.
                if (definition is IUnitDefinition && definition.Id == "point.metre")
                { unitCalls++; Attack(world, unitCalls == 1 ? "content-unit" : "content-vertical"); }
                if (definition is IResourceDefinition) Attack(world, "content-resource");
            };
            visibility.SampleAllowed = (viewer, sample) => viewer == PlayerId.None || sample is PointSnapshot point &&
                (viewer.Value == 0 ? point.Position >= config.FirstStart && point.Position <= config.FirstEnd
                    : viewer.Value == 1 && point.Position >= config.SecondStart && point.Position <= config.SecondEnd);
            visibility.OnSample = (world, viewer, layer, sample) => Attack(world, "sample");
            visibility.OnKeys = (world, viewer, layer, destination) =>
            {
                destination.Add("coverage:" + viewer.Value.ToString(CultureInfo.InvariantCulture));
                Attack(world, "keys");
                if (world.Clock.Paused && world.Clock.StepIndex > 0 && config.Stage == "keys" && config.Fault == "invalid-key")
                    destination.Add(" ");
            };
            composer.Services.Bind<IVisibility>(visibility);
            composer.Services.Bind<ISpatialSampleVisibility>(visibility);
            composer.AddViewFeature("proof.layer-access-keys", 1, (world, viewer) => new LayerAccessReadModel(world, viewer));
        }
    }
}
