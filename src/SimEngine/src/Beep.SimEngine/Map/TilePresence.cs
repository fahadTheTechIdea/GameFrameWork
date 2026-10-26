using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Maps
{
    public sealed class TilePlacement : SpatialPlacement
    {
        public TilePlacement(string space, TileCoord origin, FixVec2 position) : base(space)
        { Origin = origin; Position = position; }
        public TileCoord Origin { get; }
        public FixVec2 Position { get; }
        public static JsonValue Arguments(string space, TileCoord origin)
        {
            var value = JsonValue.NewObject(); value.Set("space", JsonValue.Of(space));
            value.Set("x", JsonValue.Number(origin.X.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            value.Set("y", JsonValue.Number(origin.Y.ToString(System.Globalization.CultureInfo.InvariantCulture))); value.Freeze(); return value;
        }
        public static JsonValue PathArguments(string space, IReadOnlyList<TileCoord> path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            var rows = JsonValue.NewArray();
            foreach (TileCoord tile in path) rows.Add(Arguments(space, tile));
            rows.Freeze(); return rows;
        }
    }

    public interface ITilePresence : ISpatialPresence
    {
        TileCoord Tile { get; }
        FixVec2 Position { get; }
        FixVec2 PreviousPosition { get; }
        FixVec2 Heading { get; }
        TileRect Footprint { get; }
        bool Blocks { get; }
        bool Movable { get; }
        bool Placed { get; }
        void Place(TileCoord tile, FixVec2 position);
        void Move(World world, FixVec2 position);
    }
    public interface ITilePresenceDefinition : ISpatialPresenceDefinition
    {
        int Width { get; }
        int Height { get; }
        bool Blocks { get; }
        bool Movable { get; }
    }
    /// <summary>Optional multi-part pure geometry constraint, implemented by component definitions in the tile placement preflight.</summary>
    public interface ITilePlacementConstraint
    {
        Code CheckTilePlacement(World world, EntityDef definition, TilePlacement placement, Entity replacing);
    }
    /// <summary>Authored auxiliary geometry that another permanent blocking footprint must leave clear.</summary>
    public interface ITilePlacementClearance : ITilePlacementConstraint
    {
        Code CheckTileObstruction(World world, EntityDef definition, TilePlacement placement, TileRect obstruction);
    }
    /// <summary>Pure authored auxiliary geometry bounds applied to every prospective and actual route origin.</summary>
    public interface ITileNavigationOriginConstraint
    {
        Code CheckNavigationOrigin(World world, TileCoord origin);
    }
    /// <summary>
    /// Pure clearance of an already-started adjacent tile segment. Derived from the motion owner's
    /// actual route/position, not future route reservations. Null means no segment is in flight.
    /// </summary>
    public interface ITileMotionClearance : IMotion
    {
        TileRect? ReadMotionClearance();
    }
    /// <summary>Sole tile presence authoring and actual-alias admission; no live coordinates.</summary>
    public sealed class TilePresenceDeclaration
    {
        private ResolvedContent _content;
        public string Space { get; }
        public int Width { get; }
        public int Height { get; }
        public bool Blocks { get; }
        public bool Movable { get; }
        private TilePresenceDeclaration(string space, int width, int height, bool blocks, bool movable)
        {
            Space=space; Width=width; Height=height; Blocks=blocks; Movable=movable;
            CheckGeometry(Space, Width, Height, Movable);
        }
        public static TilePresenceDeclaration Read(JsonReader reader)
        {
            if(reader==null)throw new ArgumentNullException(nameof(reader));
            try { return new TilePresenceDeclaration(reader.String("space"),reader.Int("width"),reader.Int("height"),reader.Bool("blocks"),reader.Bool("movable")); }
            catch(ContentException error){throw reader.Error(error.Message);}
        }
        public void Resolve(ContentResolver content)
        { _content=content.Content; content.Def<ITileSpaceDefinition>(Space); }
        private static void CheckGeometry(string space,int width,int height,bool movable)
        {
            if(string.IsNullOrWhiteSpace(space)||width<1||height<1)throw new ContentException("explicit space and positive footprint are required");
        }
        public void ValidateBinding(EntityDef entity)
            =>ValidateMetadata(_content,entity,entity.Component<ITilePresenceDefinition>(),false);
        private static void ValidateMetadata(ResolvedContent content,EntityDef entity,ITilePresenceDefinition presence,bool frozen)
        {
            if(content==null||entity==null||!ReferenceEquals(content.Find<EntityDef>(entity.Id),entity)||
                !(presence is ComponentDef component)||!ReferenceEquals(component.Owner,entity)||
                !ReferenceEquals(entity.Component<ISpatialPresenceDefinition>(),presence)||!entity.HasCapability<ITilePresence>()||
                frozen&&!component.IsFrozen)
                throw new ContentException("tile presence requires its actual owned component contract");
            CheckGeometry(presence.Space,presence.Width,presence.Height,presence.Movable);
            if(content.Find<ITileSpaceDefinition>(presence.Space)==null)throw new ContentException(entity.Id+": tile presence requires its actual authored tile space");
        }
        public static void ValidateComposition(World world,EntityDef entity,ITilePresenceDefinition presence)
        {
            ValidateMetadata(world.Content,entity,presence,true);
            if (!ReferenceEquals(entity.Component<ITilePresenceDefinition>(),presence)||
                !(world.Space is ITileSpatialSpace selected) || selected.Id != presence.Space ||
                selected.Width <= 0 || selected.Height <= 0 || presence.Width > selected.Width || presence.Height > selected.Height ||
                presence.Movable && !(selected is ITileRelocationSpace))
                throw new ContentException(entity.Id + ": tile presence requires its actual selected tile-space provider");
            TileSpaceGeometry geometry = SpatialSpaceContracts.ReadGeometry(world.Content.Get<ITileSpaceDefinition>(presence.Space), world.Settings.Scenario);
            if (selected.Width != geometry.Width || selected.Height != geometry.Height)
                throw new ContentException(entity.Id + ": selected tile provider differs from authored geometry");
        }
    }
    public sealed class TilePresenceDef : ComponentDef<TilePresence>, ITilePresenceDefinition
    {
        private TilePresenceDeclaration _declaration;
        public string Space=>_declaration.Space;
        public int Width=>_declaration.Width;
        public int Height=>_declaration.Height;
        public bool Blocks=>_declaration.Blocks;
        public bool Movable=>_declaration.Movable;
        protected override void ReadCore(JsonReader reader)=>_declaration=TilePresenceDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content)=>_declaration.Resolve(content);
        protected override void ValidateCore(EntityDef entity)=>_declaration.ValidateBinding(entity);
        public void ValidateComposition(World world,EntityDef entity)=>TilePresenceDeclaration.ValidateComposition(world,entity,this);
    }
    /// <summary>Sole tile-coordinate/interpolation owner for an entity.</summary>
    public sealed class TilePresence : Component, ITilePresence
    {
        public new ITilePresenceDefinition Def=>(ITilePresenceDefinition)base.Def;
        public string Space => Def.Space;
        public TileCoord Tile { get; private set; }
        public FixVec2 Position { get; private set; }
        public FixVec2 PreviousPosition { get; private set; }
        public FixVec2 Heading { get; private set; }
        public bool Blocks => Def.Blocks;
        public bool Movable => Def.Movable;
        public bool Placed { get; private set; }
        public TileRect Footprint => new TileRect(Tile, Def.Width, Def.Height);
        public void Place(TileCoord tile, FixVec2 position)
        {
            if (Placed) throw new InvalidOperationException("tile presence is already placed; use physical movement");
            Tile = tile; Position = PreviousPosition = position; Placed = true;
        }
        public void BeginTick() => PreviousPosition = Position;
        public void Move(World world, FixVec2 position)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.RequireHealthyUpdate();
            if (!Movable || !Placed) throw new InvalidOperationException("presence is not a placed mobile entity");
            if (!ReferenceEquals(world.Entities.Get(Entity.Id), Entity)) throw new ArgumentException("entity is not live in this world", nameof(world));
            if (!(world.Space is ITileRelocationSpace map) || map.Id != Space ||
                !map.InBounds(new TileRect(position.Tile, Footprint.W, Footprint.H)))
                throw new ArgumentException("position is outside the entity's selected relocation space", nameof(position));
            if (Position == position) return;
            _ = checked(Entity.Version + 1);
            FixVec2 heading = (position - Position).Normalized();
            var observation = EntityObservation.CaptureProvider(world, this);
            map.Relocate(world, this, position.Tile);
            observation.Verify();
            Heading = heading; Position = position; Tile = position.Tile;
            Entity.Touch();
        }
        public SpatialSnapshot Capture() => new TileSpatialSnapshot(Space, Tile, Footprint, Position, PreviousPosition, Heading, Blocks, Movable);
        public override void Sync(IStateSync s)
        {
            bool placed = Placed; s.Bool(ref placed); Placed = placed;
            TileCoord tile = Tile; s.Tile(ref tile); Tile = tile;
            FixVec2 position = Position, previous = PreviousPosition, heading = Heading;
            s.Vec(ref position); s.Vec(ref previous); s.Vec(ref heading);
            Position = position; PreviousPosition = previous; Heading = heading;
        }
    }
    public interface ITileFootprintSnapshot
    {
        TileRect Footprint { get; }
    }
    public sealed class TileSpatialSnapshot : SpatialSnapshot, ITileFootprintSnapshot
    {
        /// <summary>Detached tile facts for native or independently implemented spatial providers.</summary>
        public TileSpatialSnapshot(string space, TileCoord tile, TileRect footprint, FixVec2 position, FixVec2 previous, FixVec2 heading, bool blocks, bool movable) : base(space)
        { Tile = tile; Footprint = footprint; Position = position; PreviousPosition = previous; Heading = heading; Blocks = blocks; Movable = movable; }
        public TileCoord Tile { get; }
        public TileRect Footprint { get; }
        public FixVec2 Position { get; }
        public FixVec2 PreviousPosition { get; }
        public FixVec2 Heading { get; }
        public bool Blocks { get; }
        public bool Movable { get; }
        public override bool Matches(SpatialSnapshot other)
            => other is TileSpatialSnapshot value && Space == value.Space && Tile.Equals(value.Tile) && Footprint.Equals(value.Footprint) &&
                Position == value.Position && PreviousPosition == value.PreviousPosition && Heading == value.Heading && Blocks == value.Blocks && Movable == value.Movable;
    }
    public sealed class TileWorldSnapshot : SpatialWorldSnapshot
    {
        /// <summary>Detached selected geometry; exposes neither a runtime map nor a renderer.</summary>
        public TileWorldSnapshot(string space, int version, int width, int height) : base(space, version) { Width = width; Height = height; }
        public int Width { get; }
        public int Height { get; }
    }
}
