using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent metadata only. The shared engine declaration and existing layer provider own rules.
    public sealed class DirectSpatialLayerDef : Def, ISpatialLayerDefinition
    {
        private readonly SpatialLayerDeclaration _declaration = new SpatialLayerDeclaration();
        public string SpaceId => _declaration.SpaceId;
        public ISpatialSpaceDefinition Space => _declaration.Space;
        public Code Type => _declaration.Type;
        public SpatialLayerValueType ValueType => _declaration.ValueType;
        public SpatialLayerVisibility Visibility => _declaration.Visibility;
        public IUnitDefinition Unit => _declaration.Unit;
        public IResourceDefinition Resource => _declaration.Resource;
        public SpatialResourceMeasure ResourceMeasure => _declaration.ResourceMeasure;
        public Code VerticalDomain => _declaration.VerticalDomain;
        public IUnitDefinition VerticalUnit => _declaration.VerticalUnit;
        public string VerticalReference => _declaration.VerticalReference;
        public Fix VerticalOffset => _declaration.VerticalOffset;
        public JsonValue Metadata => _declaration.Metadata;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }

    // Explicit negative metadata, never selected in a positive scenario.
    public sealed class BrokenSpatialLayerDef : Def, ISpatialLayerDefinition
    {
        private readonly SpatialLayerDeclaration _declaration = new SpatialLayerDeclaration();
        private string _mode;
        private readonly JsonValue _mutable = JsonValue.NewObject();
        private readonly JsonValue _array = JsonValue.NewArray();
        string IContentDefinition.Id => _mode == "identity" ? "missing.alias" : Id;
        string IContentDefinition.Module => _mode == "owner" ? "missing.module" : Module;
        public string SpaceId => _mode == "space-id" ? "missing.space" : _declaration.SpaceId;
        public ISpatialSpaceDefinition Space => _mode == "space" ? null : _declaration.Space;
        public Code Type => _mode == "type" ? Code.None : _declaration.Type;
        public SpatialLayerValueType ValueType => _mode == "value-zero" ? 0 : _mode == "value-invalid" ? (SpatialLayerValueType)255 : _declaration.ValueType;
        public SpatialLayerVisibility Visibility => _mode == "visibility-zero" ? 0 : _mode == "visibility-invalid" ? (SpatialLayerVisibility)255 : _declaration.Visibility;
        public IUnitDefinition Unit => _mode == "unit" ? null : _declaration.Unit;
        public IResourceDefinition Resource => _mode == "resource" ? null : _mode == "resource-foreign" ? new BasicResourceDef() : _declaration.Resource;
        public SpatialResourceMeasure ResourceMeasure => _mode == "measure" ? SpatialResourceMeasure.None : _mode == "measure-invalid" ? (SpatialResourceMeasure)255 : _declaration.ResourceMeasure;
        public Code VerticalDomain => _mode == "vertical" ? Code.None : _declaration.VerticalDomain;
        public IUnitDefinition VerticalUnit => _mode == "vertical-unit" ? null : _declaration.VerticalUnit;
        public string VerticalReference => _mode == "reference" ? " " : _declaration.VerticalReference;
        public Fix VerticalOffset => _declaration.VerticalOffset;
        public JsonValue Metadata => _mode == "metadata-null" ? null : _mode == "metadata-mutable" ? _mutable : _mode == "metadata-array" ? _array
            : _mode == "throw" ? throw new InvalidOperationException("proof.layer-first-metadata-error") : _declaration.Metadata;
        protected override void ReadCore(JsonReader reader)
        {
            _declaration.Read(reader); _mode = reader.String("mode");
            if (Array.IndexOf(new[]{"identity","owner","space-id","space","type","value-zero","value-invalid","visibility-zero","visibility-invalid","unit","resource","resource-foreign","measure","measure-invalid","vertical","vertical-unit","reference","metadata-null","metadata-mutable","metadata-array","throw"},_mode)<0)
                throw reader.Error("explicit negative layer probe required");
        }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }

    public sealed class SpatialLayerContentModule : GameModule
    {
        public override string Id => "proof.spatial-layer-content";
        public override ModuleVersion Version => new ModuleVersion(1,0,0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[]{new ModuleRequirement(SpatialResourcesModule.ModuleId,Version)});
        public readonly InitializeProbe Initialize = new InitializeProbe("proof.layer.initialize");
        public override void DeclareContent(ContentTypes types)
        { types.Section<DirectSpatialLayerDef>("direct_spatial_layers"); types.Section<BrokenSpatialLayerDef>("broken_spatial_layers"); }
        public override void Compose(WorldComposer composer) => composer.AddSystem(Initialize,306);
    }
}
