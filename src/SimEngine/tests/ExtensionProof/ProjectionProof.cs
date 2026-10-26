using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Proves that an ordinary provider retaining authored JSON cannot mutate shared content.</summary>
    public sealed class RawContentProbeDef : Def
    {
        public JsonValue Payload { get; private set; }
        protected override void ReadCore(JsonReader reader) => Payload = reader.Object("payload");
    }

    /// <summary>External engine-side visibility rule; hosts never decide which simulation facts are public.</summary>
    public sealed class OwnerVisibility : IVisibility
    {
        public bool CanSee(World world, PlayerId viewer, Entity entity) => entity.Owner == viewer;
        public bool CanSeeContent(World world, PlayerId viewer, IContentDefinition definition) => true;
    }

    public sealed class ProjectionModule : GameModule
    {
        public const string SpatialDefinitions = @"
          ""units"": [{ ""id"": ""unit.classification"", ""dimension"": ""classification"", ""scale"": { ""numerator"": 1, ""denominator"": 1 }, ""discrete"": true }],
          ""tile_spaces"": [{ ""id"": ""projection.tiles"", ""width"": 32, ""height"": 32 }],
          ""spatial_layers"": [
            { ""id"": ""public"", ""space"": ""projection.tiles"", ""type"": ""terrain.classification"", ""value_type"": ""byte"", ""visibility"": ""public"", ""unit"": ""unit.classification"" },
            { ""id"": ""knowledge.0"", ""space"": ""projection.tiles"", ""type"": ""knowledge.survey"", ""value_type"": ""byte"", ""visibility"": ""company_private"", ""unit"": ""unit.classification"" },
            { ""id"": ""knowledge.1"", ""space"": ""projection.tiles"", ""type"": ""knowledge.survey"", ""value_type"": ""byte"", ""visibility"": ""company_private"", ""unit"": ""unit.classification"" }
          ],";
        private readonly bool _fog;
        public ProjectionModule(bool fog) { _fog = fog; }
        public override string Id => "projection";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types) => types.Section<RawContentProbeDef>("raw_probes");
        public override void Compose(WorldComposer composer)
        {
            TileMap map = composer.Services.Get<TileMap>();
            map.AssignOwner(composer.World, "knowledge.0", new PlayerId(0));
            map.AssignOwner(composer.World, "knowledge.1", new PlayerId(1));
            if (_fog) composer.Services.Bind<IVisibility>(new OwnerVisibility());
            composer.AddWorldGen(new ProjectionStart(), 10);
        }
    }

    public sealed class ProjectionStart : WorldGenStage
    {
        public override string Id => "projection.start";
        public override void Generate(World world, ScenarioDef scenario)
        {
            TileMap map = (TileMap)world.Space;
            map.Layer<ByteLayer>("public").Fill((x, y) => 1);
            for (int seat = 0; seat < world.Players.Count; seat++)
                map.SpawnAt(world, world.Content.Get<EntityDef>("marker"), new PlayerId(seat), new TileCoord(1 + seat * 3, 1));
        }
    }
}
