using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Maps
{
    /// <summary>Viewer-scoped tile queries, separate from the nonspatial player projection.</summary>
    public sealed class TileMapView : ISpatialLayerQuery<TileRect, int>, ISpatialLayerQuery<TileRect, Fix>,
        ISpatialLayerPagedQuery<TileRect, int>, ISpatialLayerPagedQuery<TileRect, Fix>, ISpatialRegionChangeQuery<TileRect>
    {
        private readonly ISpatialRegionChangeQuery<TileRect> _changes;
        private readonly World _world;
        private readonly TileMap _map;
        private readonly PlayerView _view;
        private readonly ISpatialLayerQuery<TileRect, Fix> _fixedSamples;
        private readonly ISpatialLayerQuery<TileRect, int> _integerSamples;
        private readonly ISpatialLayerPagedQuery<TileRect, Fix> _fixedPages;
        private readonly ISpatialLayerPagedQuery<TileRect, int> _integerPages;
        internal TileMapView(World world, PlayerId viewer)
        {
            _world = world; _map = world.Services.Get<TileMap>(); _view = world.ViewFor(viewer); Viewer = viewer;
            _fixedSamples = _view.Feature<ISpatialLayerQuery<TileRect, Fix>>()
                ?? throw new InvalidOperationException("selected native tile space requires its fixed sample query contract");
            _integerSamples = _view.Feature<ISpatialLayerQuery<TileRect, int>>()
                ?? throw new InvalidOperationException("selected native tile space requires its integer sample query contract");
            _fixedPages = _view.Feature<ISpatialLayerPagedQuery<TileRect, Fix>>()
                ?? throw new InvalidOperationException("selected native tile space requires its fixed page query contract");
            _integerPages = _view.Feature<ISpatialLayerPagedQuery<TileRect, int>>()
                ?? throw new InvalidOperationException("selected native tile space requires its integer page query contract");
            _changes = _view.Feature<ISpatialRegionChangeQuery<TileRect>>()
                ?? throw new InvalidOperationException("selected native tile space requires its regional change query contract");
        }
        public PlayerId Viewer { get; }
        public string Space => _map.Id;
        public int Width => _map.Width;
        public int Height => _map.Height;
        public bool TryGetFootprint(string definition, out TileRect footprint)
        {
            footprint=default;
            _world.RequireHealthyUpdate();
            EntityDef def = _world.Content.Find<EntityDef>(definition);
            ITilePresenceDefinition presence = _view.CanSeeContent(def) ? def.Component<ITilePresenceDefinition>() : null;
            if(presence==null)return false;
            TileRect? candidate=EntityObservation.ReadProvider(_world,presence,null,()=>
                presence.Space==Space?(TileRect?)new TileRect(0,0,presence.Width,presence.Height):null,_map);
            if(!candidate.HasValue)return false;
            footprint=candidate.Value;
            return true;
        }
        // ------------------------------------------------------------------ map

        /// <summary>Bumped by visible tile changes only: compare to know whether to re-read the map.</summary>
        public int Version
        {
            get { _world.RequireHealthyUpdate(); return _map.VersionFor(Viewer); }
        }

        /// <summary>
        /// Reads an authorized byte/integer layer over an in-bounds rectangle, row by row. Clears
        /// <paramref name="dst"/> first, including on refusal. Unallocated in-bounds cells read zero.
        /// </summary>
        public SpatialLayerReadStatus ReadLayer(string layer, TileRect area, IList<int> dst)
            => _integerSamples.ReadLayer(layer, area, dst);

        /// <summary>Reads exact fixed-point values through the same authorization and region rules.</summary>
        public SpatialLayerReadStatus ReadLayer(string layer, TileRect area, IList<Fix> dst)
            => _fixedSamples.ReadLayer(layer, area, dst);

        /// <summary>Reads only the requested row-major page, including regions larger than one IList.</summary>
        public SpatialLayerPageResult ReadLayerPage(string layer, TileRect area,
            SpatialLayerPageRequest request, IList<int> destination)
            => _integerPages.ReadLayerPage(layer, area, request, destination);

        /// <summary>Exact fixed-point pages obey the same visibility, geometry and version rules.</summary>
        public SpatialLayerPageResult ReadLayerPage(string layer, TileRect area,
            SpatialLayerPageRequest request, IList<Fix> destination)
            => _fixedPages.ReadLayerPage(layer, area, request, destination);

        /// <summary>Visible authored layer contracts, including meaning, units, resource links and vertical metadata.</summary>
        public IReadOnlyList<SpatialLayerSnapshot> Layers
        {
            get
            {
                _world.RequireHealthyUpdate();
                var layers = new List<SpatialLayerSnapshot>();
                for (int i = 0; i < _map.Layers.Count; i++)
                {
                    MapLayer layer = _map.Layers[i];
                    SpatialLayerSnapshot snapshot = SpatialLayerAccess.Capture(_world, layer, Viewer);
                    if (snapshot != null) layers.Add(snapshot);
                }
                return layers.AsReadOnly();
            }
        }

        public IReadOnlyList<string> LayerIds
        {
            get
            {
                _world.RequireHealthyUpdate();
                var ids = new List<string>();
                for (int i = 0; i < _map.Layers.Count; i++)
                    if (SpatialLayerAccess.CanObserve(_world, _map.Layers[i], Viewer))
                        ids.Add(_map.Layers[i].Id);
                return ids.AsReadOnly();
            }
        }

        /// <summary>
        /// Returns exact changed native regions, or a full-invalidation signal. Retain the returned
        /// Cursor only for this exact viewer/session query; old or foreign streams refuse.
        /// </summary>
        public SpatialRegionChanges<TileRect> ReadChanges(SpatialRegionCursor previous = null)
            => _changes.ReadChanges(previous);

        /// <summary>Visible entities overlapping an in-bounds area. Refused geometry never publishes a prefix.</summary>
        public Code CopyEntities(TileRect area, IList<EntityView> dst)
        {
            if (dst == null) throw new ArgumentNullException(nameof(dst));
            dst.Clear();
            _world.RequireHealthyUpdate();
            if (area.X < 0 || area.Y < 0 || area.W < 0 || area.H < 0 ||
                (long)area.X + area.W > _map.Width || (long)area.Y + area.H > _map.Height)
                return Codes.OutOfBounds;
            if (area.W == 0 || area.H == 0) return Code.None;
            var values = new List<EntityView>();
            IReadOnlyList<Entity> all = _world.Entities.All;
            for (int i = 0; i < all.Count; i++)
            {
                Entity e = all[i];
                if (!e.Alive || !_view.CanSee(e)) continue;
                var presence = e.Get<ITilePresence>();
                if (presence != null && EntityObservation.ReadProvider(_world, presence, e, () => area.Overlaps(presence.Footprint), space: _map, observeTargetState: true))
                    values.Add(PlayerView.ViewOf(_world, e));
            }
            foreach (EntityView value in values) dst.Add(value);
            return Code.None;
        }

        /// <summary>The structure standing on a tile, if any.</summary>
        public bool TryGetOccupantAt(TileCoord tile, out EntityView view)
        {
            view = default;
            _world.RequireHealthyUpdate();
            Entity e = _map.OccupantAt(tile);
            if (e != null && !_view.CanSee(e)) e = null;
            view = e == null ? default : PlayerView.ViewOf(_world, e);
            return e != null;
        }

    }
}
