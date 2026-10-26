using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Registered negative metadata/factories only; no accepted runtime store or gameplay authority.
    public sealed class InvalidRegionContentDef : Def, ISpatialRegionDefinition
    {
        private SpatialRegionDeclaration _declaration;
        private string _mode;
        public ISpatialSpaceDefinition SpaceDefinition => _declaration.SpaceDefinition;
        public string Space => _mode == "space" ? "wrong.space" : _declaration.Space;
        public string ShapeJson => _mode == "shape" ? null : _declaration.ShapeJson;
        protected override void ReadCore(JsonReader reader)
            => _declaration = SpatialRegionDeclaration.Read(reader, r => _mode = r.String("mode"));
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public SpatialRegionStatus Check(SpatialSnapshot presence) => SpatialRegionStatus.Unsupported;
        public ISpatialRegionGeometry CreateGeometry(string id, JsonReader shape)
        {
            for (int i = 0; i < shape.Raw.Count; i++) shape.Skip(shape.Raw.KeyAt(i));
            if (_mode == "factory-null") return null;
            if (_mode == "factory-self") return this;
            if (_mode == "factory-identity") return new InvalidVersion(id + ".wrong", Space, ShapeJson);
            if (_mode == "factory-shape") return new InvalidVersion(id, Space, "{");
            if (_mode == "factory-space") return new InvalidVersion(id, Space, "{\"space\":\"other\"}");
            throw new InvalidOperationException("proof.region.factory-refused");
        }
        private sealed class InvalidVersion : ISpatialRegionGeometry
        {
            public string Id { get; }
            public string Space { get; }
            public string ShapeJson { get; }
            internal InvalidVersion(string id, string space, string shape) { Id = id; Space = space; ShapeJson = shape; }
            public SpatialRegionStatus Check(SpatialSnapshot presence) => SpatialRegionStatus.Unsupported;
        }
    }
    public sealed class RegionContentModule : GameModule
    {
        public const string ModuleId = "proof.region-content";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement("proof.exploration-bindings", Version) };
        public InitializeProbe Initialize { get; private set; }
        public bool ActualAliases { get; private set; }
        public bool OriginalAreaOwner { get; private set; }
        public override void DeclareContent(ContentTypes types) => types.Section<InvalidRegionContentDef>("invalid_region_content");
        public override void Compose(WorldComposer composer)
        {
            var region = composer.Content.Get<ISpatialRegionDefinition>("binding.region");
            var travel = composer.Content.Get<ISpatialRegionDefinition>("region.travel");
            var profile = composer.Content.Get<IAuthoredExplorationProfileDefinition>("binding.profile");
            var requirement = composer.Content.Get<TargetRegionRequirementDef>("region.inside");
            ActualAliases = region.GetType().BaseType == typeof(Def) && composer.Content.Owns(region) &&
                ReferenceEquals(profile.Entries[0].Region, region) && ReferenceEquals(requirement.Region, region) &&
                travel.GetType().BaseType == typeof(Def) && composer.Content.Owns(travel);
            OriginalAreaOwner = composer.Services.Get<IOperatingAreas>() is IPersistentService state && state.Id == "sim.operating-areas";
            Initialize = new InitializeProbe("proof.region-content-init"); composer.AddSystem(Initialize, 12355);
        }
    }
}
