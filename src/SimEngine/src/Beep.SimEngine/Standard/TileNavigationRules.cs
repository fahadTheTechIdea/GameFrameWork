using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    public interface ITileNavigationRule : IContentDefinition
    {
        string Space { get; }
        int MinCost { get; }
        int Cost(World world, TileCoord tile);
    }
    /// <summary>Actual authored profile admission and current cost checks, not a router/catalog.</summary>
    public static class TileNavigationContracts
    {
        public static void Validate(ITileNavigationRule rule, ContentResolver content)
        {
            if (rule == null || !ReferenceEquals(content.Def<ITileNavigationRule>(rule.Id), rule) || rule.MinCost < 1)
                throw content.Fail("tile navigation requires its actual profile alias and positive declared minimum");
            content.Def<ITileSpaceDefinition>(rule.Space);
        }
        public static void RequireContext(ITileNavigationRule rule, World world)
        {
            if (!(rule is Def authored) || !authored.IsFrozen || world == null ||
                !world.Content.Owns(rule) || !ReferenceEquals(world.Content.Get<ITileNavigationRule>(rule.Id), rule) ||
                !(world.Space is ITileSpatialSpace map) || map.Id != rule.Space || rule.MinCost < 1)
                throw new InvalidOperationException("navigation profile requires its frozen actual session content and tile space");
        }
        private static readonly Func<(World World, ITileNavigationRule Rule, int Minimum, TileCoord Tile), int> CostRead = ReadCostCore;
        internal static int ReadCost(World world, ITileNavigationRule rule, int minimum, TileCoord tile)
            => EntityObservation.ReadProvider(world, rule, null, (world, rule, minimum, tile), CostRead, world.Space);
        internal static int ReadCostCore((World World, ITileNavigationRule Rule, int Minimum, TileCoord Tile) context)
        {
            RequireContext(context.Rule, context.World);
            if (context.Rule.MinCost != context.Minimum) throw new InvalidOperationException("navigation minimum changed after registration");
            int value = context.Rule.Cost(context.World, context.Tile);
            if (value < 0 || value > 0 && value < context.Minimum)
                throw new InvalidOperationException("navigation cost violates its declared nonnegative minimum");
            return value;
        }
    }
    public sealed class UniformTileNavigationDeclaration
    {
        private string _space;
        public ITileSpaceDefinition Space { get; private set; }
        public int Cost { get; private set; }
        public void Read(JsonReader reader)
        {
            _space = reader.String("space"); Cost = reader.Int("cost");
            if (string.IsNullOrWhiteSpace(_space) || Cost <= 0)
                throw reader.Error("uniform navigation requires explicit space and positive cost");
        }
        public void Resolve(ContentResolver content) => Space = content.Def<ITileSpaceDefinition>(_space);
    }
    /// <summary>Explicit authored uniform terrain; occupancy/path search remain NavService-owned.</summary>
    public sealed class UniformTileNavigationDef : Def, ITileNavigationRule
    {
        private readonly UniformTileNavigationDeclaration _declaration = new UniformTileNavigationDeclaration();
        public string Space => _declaration.Space.Id;
        public int MinCost => _declaration.Cost;
        public int Cost(World world, TileCoord tile)
        {
            TileNavigationContracts.RequireContext(this, world);
            return ((ITileSpatialSpace)world.Space).InBounds(tile) ? _declaration.Cost : 0;
        }
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
}
