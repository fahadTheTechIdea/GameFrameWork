using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Point geometry adapter for the existing engine-owned startup DAG, not another world generator.</summary>
    public sealed class PointLayerSetupDef : Def, IWorldSetupDefinition
    {
        public void ValidateComposition(World world)
        {
            Code refusal = Check(world, null);
            if (!refusal.IsNone) throw new ContentException(Id + ": startup layer declaration refused " + refusal);
        }
        private string _layer;
        private ISpatialLayerDefinition _resolved;
        private Fix _point, _value;
        private PlayerId _owner;
        public IReadOnlyList<string> Dependencies { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            if (reader.Raw.Get("after")?.Kind != JsonKind.Array) throw reader.Error("explicit after array required");
            Dependencies = Array.AsReadOnly(reader.Strings("after"));
            _layer = reader.String("layer"); _point = reader.Fix("point");
            _value = reader.Fix("value"); _owner = new PlayerId(reader.Int("owner"));
        }
        protected override void ResolveCore(ContentResolver content) => _resolved = content.Def<ISpatialLayerDefinition>(_layer);
        protected override void ValidateResolvedCore(ContentResolver content)
        {
            if (!(_resolved.Space is PointSpaceDef space) || _point < Fix.Zero || _point > space.Extent ||
                _owner != PlayerId.None && (!_owner.IsValid || _resolved.Visibility == SpatialLayerVisibility.Public))
                throw content.Fail("invalid point layer setup space/location/owner");
        }
        public Code Check(World world, IWorldSetupContext context)
        {
            if (_owner != PlayerId.None && !world.Players.IsSeat(_owner)) return Codes.NotSeated;
            if (!(world.Space is PointSpace space) || space.Id != _resolved.Space.Id) return Codes.InvalidArgument;
            foreach (ISpatialLayer layer in space.SpatialLayers)
                if (layer.Id == _layer)
                    return SpatialResourceValues.CheckValue(layer, _value) == UnitConversionStatus.Converted ? Code.None : Codes.InvalidArgument;
            return Codes.NotFound;
        }
        public EntityId Generate(World world, IWorldSetupContext context)
        {
            Code refusal = Check(world, context);
            if (!refusal.IsNone) throw new ContentException(Id + ": startup refused " + refusal);
            var space = (PointSpace)world.Space;
            if (_resolved.Visibility == SpatialLayerVisibility.CompanyPrivate) space.AssignOwner(world, _layer, _owner);
            refusal = space.WriteLayer(world, PlayerId.None, _layer, _point, _value);
            if (!refusal.IsNone) throw new ContentException(Id + ": startup write refused " + refusal);
            return EntityId.None;
        }
    }

    /// <summary>Independent non-oil content contracts. All work/stock/processing/project owners remain native.</summary>
    public sealed class PlanetXModule : GameModule
    {
        public override string Id => "planet-x";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[]
        {
            new ModuleRequirement(WorldSetupModule.ModuleId, Version),
            new ModuleRequirement("points", Version), new ModuleRequirement(SpatialResourcesModule.ModuleId, Version)
        };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<PlanetOreDef>("planet_ores");
            types.Section<PointLayerSetupDef>("point_layer_setups");
        }
    }

    /// <summary>Explicit composition only; no host gameplay, generated content defaults or live state repairs.</summary>
    public static class PlanetX
    {
        public static GameFactory Factory(string masterDocument = null, IReadOnlyList<GameModule> additionalModules = null,
            IReadOnlyDictionary<string, string> additionalDocuments = null)
        {
            var modules = new List<GameModule>
            {
                new ContentAccessModule(), new InventoryModule(), new ResourceFlowsModule(), new OperationsModule(),
                new TransformationModule(), new MotionModule(), new WorkModule(), new LogisticsModule(),
                new ConstructionModule(), new PointModule(), new WorldSetupModule(),
                new PointSpatialResourcesModule(), new PlanetXModule()
            };
            if (additionalModules != null) modules.AddRange(additionalModules);
            var definition = new GameDefinition("planet-x", modules);
            var files = new PreloadedFileSource().Add("manifest.json", masterDocument ?? FrameworkProofContent.Read("planet-x-master.json"))
                .Add("framework/access.json", FrameworkProofContent.Access)
                .Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"))
                .Add("planet-x.json", FrameworkProofContent.Read("planet-x.json"));
            if (additionalDocuments != null) foreach (var document in additionalDocuments) files.Add(document.Key, document.Value);
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
    }
}

