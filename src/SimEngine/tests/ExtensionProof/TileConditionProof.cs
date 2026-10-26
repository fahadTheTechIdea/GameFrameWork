using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Extension-owned predicate, not gameplay in a player driver or demo.
    public sealed class TileColumnsConditionDef : Def, ITileConditionDefinition
    {
        private string _space;
        public ITileSpaceDefinition Space { get; private set; }
        public int MinimumX { get; private set; }
        public int MaximumX { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _space = reader.String("space");
            MinimumX = reader.Int("minimum_x"); MaximumX = reader.Int("maximum_x");
            if (MinimumX < 0 || MaximumX <= MinimumX) throw reader.Error("explicit nonempty column interval required");
        }
        protected override void ResolveCore(ContentResolver content) => Space = content.Def<ITileSpaceDefinition>(_space);
        protected override void ValidateResolvedCore(ContentResolver content)
        {
            if (!SpatialSpaceContracts.ContainsAny(Space, new TileRect(MinimumX, 0, MaximumX - MinimumX, 1)))
                throw content.Fail("column condition exceeds authored tile space");
        }
        public bool Matches(World world, ITileSpatialSpace map, TileCoord tile)
        {
            TileConditionContracts.RequireContext(this, world, map);
            return map.InBounds(tile) && tile.X >= MinimumX && tile.X < MaximumX;
        }
    }
    public sealed class TileConditionProofModule : GameModule
    {
        public const string ModuleId = "proof.tile-conditions";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types) => types.Section<TileColumnsConditionDef>("proof_tile_columns");
    }
}
