using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Maps
{
    public sealed partial class TileMap : ILineGeometryComposition
    {
        public void ComposeLineGeometry(WorldComposer composer)
            => TileLineGeometry.Compose(composer, this);
    }
    /// <summary>Stateless shared tile geometry. Selected providers own samples; NetworkService owns pools.</summary>
    public sealed class TileLineGeometry : ILineRemovalGeometry, ITileLineRouting, INetworkGraphGeometry
    {
        public static void Compose(WorldComposer composer, ITileNavigationSpace space)
        {
            if(!ReferenceEquals(composer.World.Space,space))throw new ContentException("tile corridor requires actual selected geometry");
            composer.Services.Require<NavService>();
            var geometry=new TileLineGeometry(composer.World);
            composer.Services.Bind<ILineGeometry>(geometry);
            composer.Services.Bind<ILineRemovalGeometry>(geometry);
            composer.Services.Bind<INetworkGraphGeometry>(geometry);
            composer.Services.Bind<ITileLineRouting>(geometry);
            composer.Commands.Register("sim.lay-line",new LayLineHandler());
        }
        private readonly World _world;
        private readonly ITileNavigationSpace _map;
        public TileLineGeometry(World world)
        {
            _world=world??throw new ArgumentNullException(nameof(world));
            _map = world.Space as ITileNavigationSpace ?? throw new ContentException("tile corridors require navigation geometry");
            if (_map is not ISpatialPlacementIntent) throw new ContentException("tile corridors require canonical placement intent");
            if (_map is not ITileSampleMutationSpace) throw new ContentException("tile corridors require whole-set sample mutation");
        }
        private ITileNavigationSpace Map { get { _world.RequireHealthyUpdate(); return _map; } }
        private ITileWritableSampleLayer Layer(ISpatialLayerDefinition definition)
        {
            var layer = TileSpatialGeometry.RequireLayer<ITileWritableSampleLayer>(_world, Map, definition, false);
            if (layer.Version < 0) throw new InvalidOperationException("negative actual tile corridor revision");
            return layer;
        }
        private static int Value(ITileSampleLayer layer, TileCoord tile)
        {
            Fix value = layer.GetSample(tile);
            if (value.Raw % Fix.One.Raw != 0 || value < Fix.FromInt(layer.ValueType == SpatialLayerValueType.Byte ? 0 : int.MinValue) ||
                value > Fix.FromInt(layer.ValueType == SpatialLayerValueType.Byte ? byte.MaxValue : int.MaxValue))
                throw new InvalidOperationException("invalid actual tile corridor sample");
            return value.ToIntFloor();
        }
        private static List<TileCoord> TilePath(ILineDefinition line, IReadOnlyList<SpatialPlacement> path)
        {
            if (path == null || path.Count == 0) return null;
            var tiles = new List<TileCoord>();
            foreach (SpatialPlacement placement in path) {
                if (!(placement is TilePlacement tile) || tile.Space != line.Layer.Space.Id ||
                    tile.Position != FixVec2.Centre(tile.Origin)) return null;
                tiles.Add(tile.Origin);
            }
            return tiles;
        }
        public string Space => _world.Space.Id;
        private static byte TileValue(ILineDefinition line)
        {
            if (!line.Value.IsPositive || line.Value.Raw % Fix.One.Raw != 0 || line.Value.Raw / Fix.One.Raw > byte.MaxValue)
                throw new ContentException(line.Id + ": native line requires exact positive byte value");
            return (byte)(line.Value.Raw / Fix.One.Raw);
        }
        public void ValidateLine(World world, ILineDefinition line)
        {
            if (!ReferenceEquals(world,_world) || !ReferenceEquals(world.Space, Map) || line.Layer.Space.Id != Map.Id)
                throw new ContentException("tile line geometry requires its actual selected space");
            var map = Map;
            ITilePresenceDefinition cursor = line.WorkMarker.Component<ITilePresenceDefinition>();
            if (cursor == null || cursor.Space != map.Id || cursor.Width != 1 || cursor.Height != 1 || cursor.Blocks || !cursor.Movable)
                throw new ContentException(line.Id + ": native work marker requires a nonblocking movable unit tile footprint");
            TileValue(line);
            try {
                if (line.Layer.ValueType != SpatialLayerValueType.Byte || Layer(line.Layer).ValueType != SpatialLayerValueType.Byte ||
                    Layer(line.Layer) is not ITileSparseSampleLayer)
                    throw new ContentException(line.Id + ": native line requires its actual byte layer");
                if (line.Owned && Layer(line.OwnerLayer).ValueType != SpatialLayerValueType.Int32)
                    throw new ContentException(line.Id + ": native owner layer requires integer values");
            }
            catch (InvalidOperationException error) { throw new ContentException(line.Id + ": unregistered native corridor layer: " + error.Message); }
        }
        public Code CheckPath(World world, ILineDefinition line, IReadOnlyList<SpatialPlacement> path)
        {
            if (!ReferenceEquals(world,_world) || line.Layer.Space.Id != Space) return LineCodes.WrongSpace;
            var tiles = TilePath(line,path); if (tiles == null) return Codes.InvalidArgument;
            var seen = new HashSet<TileCoord>(); var map = Map;
            for (int i=0;i<tiles.Count;i++) {
                if (!map.InBounds(tiles[i])) return Codes.OutOfBounds;
                if (!seen.Add(tiles[i]) || i>0 && TileCoord.Steps(tiles[i],tiles[i-1]) != 1) return LineCodes.NotContiguous;
            }
            return Code.None;
        }
        public Code CheckSegment(World world, PlayerId company, ILineDefinition line, SpatialPlacement placement, out bool alreadyLaid)
        {
            alreadyLaid = false;
            if (!ReferenceEquals(world,_world) || !(placement is TilePlacement tile) || tile.Space != Space) return LineCodes.WrongSpace;
            var map = Map; if (!map.InBounds(tile.Origin)) return Codes.OutOfBounds;
            var layer = Layer(line.Layer);
            if (line.Owned && Value(layer, tile.Origin) != 0 && Value(Layer(line.OwnerLayer), tile.Origin) != company.Value+1)
                return LineCodes.ForeignLine;
            alreadyLaid = Value(layer, tile.Origin) >= TileValue(line); return Code.None;
        }
        public bool IsCursorAt(World world, Entity cursor, SpatialPlacement placement)
            => ReferenceEquals(world,_world) && placement is TilePlacement tile && tile.Space == Space &&
                cursor?.Get<ITilePresence>() is ITilePresence presence && presence.Space == Space && presence.Tile == tile.Origin;
        public void MoveCursor(World world, Entity cursor, SpatialPlacement placement)
        {
            if (!ReferenceEquals(world,_world) || !(placement is TilePlacement tile) || tile.Space != Space)
                throw new InvalidOperationException("native cursor requires actual selected tile placement");
            cursor.Get<ITilePresence>().Move(world,FixVec2.Centre(tile.Origin));
        }
        Code ILineGeometry.Stamp(World world, PlayerId company, ILineDefinition line, SpatialPlacement placement)
        {
            Code refusal = CheckSegment(world,company,line,placement,out _);
            return !refusal.IsNone ? refusal : Stamp(line,((TilePlacement)placement).Origin,company) ? Code.None : LineCodes.ForeignLine;
        }
        private Code CheckPlacement(PlayerId player, ILineDefinition line, TileCoord tile)
            => LinePlacementContracts.Check(_world, line.PlacementPolicy, player,
                ((ISpatialPlacementIntent)Map).ReadPlacement(TilePlacement.Arguments(line.Layer.Space.Id, tile)));

        /// <summary>
    /// The path a line would take between two tiles: the shortest 8-way route over tiles the line may
    /// use (the game's tile rules, not through another owner's line), deterministic. Clears
    /// <paramref name="path"/> and distinguishes unreachable from an exhausted search budget.
        /// </summary>
        public PathSearchResult PlanPath(PlayerId player, ILineDefinition line, TileCoord from, TileCoord to,
            List<TileCoord> path, int maxExpanded = NavGrid.DefaultExpansionBudget)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            path.Clear();
            _world.RequireHealthyUpdate();
            return line == null ? PathSearchResult.InvalidRequest : LineDefinitionContracts.Read(_world, line, null,
                () => PlanPathCore(player, line, from, to, path, maxExpanded));
        }
        private PathSearchResult PlanPathCore(PlayerId player, ILineDefinition line, TileCoord from, TileCoord to,
            List<TileCoord> path, int maxExpanded)
        {
            if (!_world.ViewFor(player).CanSeeContent(line)) return PathSearchResult.InvalidRequest;
            if (line == null || _world.Space == null || line.Layer.Space.Id != _world.Space.Id ||
                !Map.InBounds(from) || !Map.InBounds(to))
                return PathSearchResult.InvalidRequest;
            var layer = Layer(line.Layer);
            var owner = line.Owned ? Layer(line.OwnerLayer) : null;
            bool Usable(TileCoord t)
            {
                if (!Map.InBounds(t)) return false;
                if (owner != null && Value(layer,t) != 0 && Value(owner,t) != player.Value + 1) return false;
                if (!CheckPlacement(player, line, t).IsNone) return false;
                return true;
            }
            if (!Usable(from) || !Usable(to)) return PathSearchResult.Unreachable;
            // The shared router, with the line's own tile rules as the cost (ordinary or blocked).
            PathSearchResult result = _world.Services.Get<NavService>().Search.Find(Map, from,
                new TileRect(to, 1, 1), t => Usable(t) ? 10 : 0, 10, false, maxExpanded, path);
            if (result != PathSearchResult.Found) return result;
            path.Insert(0, from);
            return PathSearchResult.Found;
        }

        /// <summary>
        /// Lays one tile (raising, never lowering, the layer's value) and claims it for an owned line. An owned
        /// tile another company laid since the path was planned is never overwritten: it returns false.
        /// </summary>
        public bool Stamp(ILineDefinition line, TileCoord t, PlayerId owner)
        {
            if (line == null || _world.Space == null || line.Layer.Space.Id != _world.Space.Id)
                throw new ArgumentException("line must belong to the selected spatial space", nameof(line));
            var layer = Layer(line.Layer);
            var changes = new List<TileSampleMutation>();
            if (line.Owned)
            {
                var owners = Layer(line.OwnerLayer);
                if (Value(layer,t) != 0 && Value(owners,t) != owner.Value + 1) return false;
                changes.Add(new TileSampleMutation(line.OwnerLayer, t, Fix.FromInt(owner.Value + 1)));
            }
            byte value = TileValue(line); if (Value(layer,t) < value) changes.Add(new TileSampleMutation(line.Layer, t, Fix.FromInt(value)));
            if (changes.Count > 0) ((ITileSampleMutationSpace)Map).ApplySamples(changes);
            return true;
        }

        public Code ReadRemovalPath(World world, ILineDefinition line, Entity target, out IReadOnlyList<SpatialPlacement> path)
        {
            path = null;
            if (!ReferenceEquals(world,_world) || line.Layer.Space.Id != Space) return LineCodes.WrongSpace;
            ITilePresence presence = target?.Get<ITilePresence>();
            if (presence == null || !presence.Placed || presence.Movable) return Codes.InvalidArgument;
            if (presence.Space != Space) return LineCodes.WrongSpace;
            TileRect footprint = presence.Footprint; var placements = new List<SpatialPlacement>();
            for (int y=footprint.Y;y<footprint.Bottom;y++)
                for (int x=footprint.X;x<footprint.Right;x++)
                    placements.Add(((ISpatialPlacementIntent)Map).ReadPlacement(TilePlacement.Arguments(Space,new TileCoord(x,y))));
            path = placements.AsReadOnly(); return Code.None;
        }
        Code ILineRemovalGeometry.CheckRemoval(World world, PlayerId company, ILineDefinition line, IReadOnlyList<SpatialPlacement> path)
        {
            if (!ReferenceEquals(world,_world)) return LineCodes.WrongSpace;
            var tiles = TilePath(line,path);
            return tiles == null ? Codes.InvalidArgument : _world.Services.Get<INetworkTopology>().CheckRemoval(company,line,path);
        }
        Code ILineRemovalGeometry.Remove(World world, PlayerId company, ILineDefinition line, IReadOnlyList<SpatialPlacement> path)
        {
            if (!ReferenceEquals(world,_world)) return LineCodes.WrongSpace;
            var tiles = TilePath(line,path);
            return tiles == null ? Codes.InvalidArgument : _world.Services.Get<INetworkTopology>().Remove(company,line,path);
        }

        private static string Key(TileCoord at)=>at.Y.ToString("D10",System.Globalization.CultureInfo.InvariantCulture)+":"+at.X.ToString("D10",System.Globalization.CultureInfo.InvariantCulture);
        private bool TryKey(string identity,out TileCoord at)
        {
            at=default;
            if(identity==null||identity.Length!=21||identity[10]!=':'||
                !int.TryParse(identity.Substring(0,10),out int y)||!int.TryParse(identity.Substring(11,10),out int x))return false;
            at=new TileCoord(x,y);return Map.InBounds(at)&&Key(at)==identity;
        }
        public void ValidateNetwork(World world,INetworkDefinition network)=>ValidateLine(world,network.Line);
        public NetworkGeometryVersion ReadVersion(World world,ILineDefinition line)
        {
            if(!ReferenceEquals(world,_world)||line.Layer.Space.Id!=Space)throw new InvalidOperationException("native graph requires actual selected space");
            return new NetworkGeometryVersion(Layer(line.Layer).Version,line.Owned?Layer(line.OwnerLayer).Version:0);
        }
        public void CopyVertices(World world,ILineDefinition line,IList<NetworkVertex> destination)
        {
            destination.Clear();var cells=new List<TileCoord>();var layer=Layer(line.Layer);
            ((ITileSparseSampleLayer)layer).CopyNonZeroTiles(cells);
            var owners=line.Owned?Layer(line.OwnerLayer):null; TileCoord? previous=null;
            foreach(var cell in cells){
                if(!Map.InBounds(cell)||Value(layer,cell)<=0||previous.HasValue&&(cell.Y<previous.Value.Y||cell.Y==previous.Value.Y&&cell.X<=previous.Value.X))
                    throw new InvalidOperationException("noncanonical actual tile graph enumeration");
                int owner=owners==null?0:Value(owners,cell);destination.Add(new NetworkVertex(Key(cell),owner==0?PlayerId.None:new PlayerId(owner-1)));previous=cell;
            }
        }
        public void CopyNeighbours(World world,ILineDefinition line,string identity,IList<string> destination)
        {
            destination.Clear();if(!TryKey(identity,out var at))throw new InvalidOperationException("invalid native graph identity");
            foreach(var delta in TileSpatialGeometry.Neighbours){var next=new TileCoord(checked(at.X+delta.X),checked(at.Y+delta.Y));if(Map.InBounds(next))destination.Add(Key(next));}
        }
        public bool ValidateIdentity(World world,ILineDefinition line,string identity)=>ReferenceEquals(world,_world)&&line.Layer.Space.Id==Space&&TryKey(identity,out _);
        public bool Touches(World world,ILineDefinition line,Entity anchor,EntityDef footprint,string identity)
        {
            if(!ValidateIdentity(world,line,identity)||!TryKey(identity,out var at)||
                anchor?.Get<ITilePresence>() is not ITilePresence presence||!presence.Placed||presence.Space!=Space||
                footprint?.Component<ITilePresenceDefinition>() is not ITilePresenceDefinition definition||definition.Space!=Space)return false;
            var shape=ReferenceEquals(footprint,anchor.Def)?presence.Footprint:new TileRect(presence.Tile,definition.Width,definition.Height);
            return (long)at.X>=(long)shape.X-1&&(long)at.X<(long)shape.X+shape.W+1&&
                (long)at.Y>=(long)shape.Y-1&&(long)at.Y<(long)shape.Y+shape.H+1;
        }
        public string VertexIdentity(World world,ILineDefinition line,SpatialPlacement placement)
        {
            if(!(placement is TilePlacement tile)||tile.Space!=Space||!Map.InBounds(tile.Origin)||tile.Position!=FixVec2.Centre(tile.Origin))
                throw new InvalidOperationException("native graph requires actual canonical placement");
            return Key(tile.Origin);
        }
        public Code CheckErase(World world,PlayerId company,ILineDefinition line,IReadOnlyList<SpatialPlacement> path)
        {
            if(!ReferenceEquals(world,_world)||line.Layer.Space.Id!=Space)return LineCodes.WrongSpace;
            var cells=TilePath(line,path);if(cells==null)return Codes.InvalidArgument;var map=Map;
            var layer=Layer(line.Layer);var owners=line.Owned?Layer(line.OwnerLayer):null;var seen=new HashSet<TileCoord>();
            foreach(var cell in cells){if(!map.InBounds(cell))return Codes.OutOfBounds;if(!seen.Add(cell))return Codes.InvalidArgument;
                if(Value(layer,cell)==0)return Codes.NotFound;if(owners!=null&&Value(owners,cell)!=company.Value+1)return LineCodes.ForeignLine;}
            return Code.None;
        }
        public void Erase(World world,PlayerId company,ILineDefinition line,IReadOnlyList<SpatialPlacement> path)
        {
            if(!CheckErase(world,company,line,path).IsNone)throw new InvalidOperationException("native erase changed after graph admission");
            var changes=new List<TileSampleMutation>();
            foreach(var cell in TilePath(line,path)) {
                changes.Add(new TileSampleMutation(line.Layer,cell,Fix.Zero));
                if(line.Owned)changes.Add(new TileSampleMutation(line.OwnerLayer,cell,Fix.Zero));
            }
            ((ITileSampleMutationSpace)Map).ApplySamples(changes);
        }

    }
}
