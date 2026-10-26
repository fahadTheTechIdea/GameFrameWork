using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Maps
{
    /// <summary>One viewer-scoped regional reader over selected tile sample contracts. No map store or renderer.</summary>
    public sealed class TileLayerView : ISpatialLayerQuery<TileRect, Fix>, ISpatialLayerQuery<TileRect, int>,
        ISpatialLayerPagedQuery<TileRect, Fix>, ISpatialLayerPagedQuery<TileRect, int>
    {
        private readonly SpatialChangeScope _pages = new SpatialChangeScope();
        private readonly World _world;
        private readonly ITileSpatialSpace _space;
        private readonly PlayerId _viewer;

        public TileLayerView(World world, PlayerId viewer)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _space = world.Space as ITileSpatialSpace ?? throw new InvalidOperationException("tile queries require the actual selected tile space");
            if (_space is not ISpatialLayerProvider) throw new InvalidOperationException("tile queries require actual layer metadata");
            _viewer = viewer;
        }

        /// <summary>Explicit contracts for one selected provider and one actual read-model instance per viewer.</summary>
        public static void Compose(WorldComposer composer, ITileSpatialSpace space)
        {
            if (composer == null || space == null) throw new ArgumentNullException();
            if (!ReferenceEquals(composer.World.Space, space)) throw new InvalidOperationException("tile queries bind only the actual selected space");
            string primaryId = "tile-layers." + space.Id;
            composer.AddViewFeature(primaryId, 1, (world, viewer) => new TileLayerView(world, viewer));
            composer.AddViewFeatureAlias<ISpatialLayerQuery<TileRect, Fix>, TileLayerView>(primaryId + ".fixed", 1);
            composer.AddViewFeatureAlias<ISpatialLayerQuery<TileRect, int>, TileLayerView>(primaryId + ".integer", 1);
            composer.AddViewFeatureAlias<ISpatialLayerPagedQuery<TileRect, Fix>, TileLayerView>(primaryId + ".fixed-pages", 1);
            composer.AddViewFeatureAlias<ISpatialLayerPagedQuery<TileRect, int>, TileLayerView>(primaryId + ".integer-pages", 1);
            composer.RequireViewFeature<TileLayerView>(primaryId, 1);
            if (space is ISpatialRegionChangeSource<TileRect> changes)
                SpatialRegionChangeView<TileRect>.Compose(composer, changes, "tile-changes." + space.Id);
        }

        public SpatialLayerReadStatus ReadLayer(string layerId, TileRect region, IList<Fix> destination)
            => Query(layerId, region, destination, false, (value, type) => value, null).Status;

        public SpatialLayerReadStatus ReadLayer(string layerId, TileRect region, IList<int> destination)
            => Query(layerId, region, destination, true, ReadInteger, null).Status;

        public SpatialLayerPageResult ReadLayerPage(string layerId, TileRect region,
            SpatialLayerPageRequest request, IList<Fix> destination)
            => Query(layerId, region, destination, false, (value, type) => value, request).Page;

        public SpatialLayerPageResult ReadLayerPage(string layerId, TileRect region,
            SpatialLayerPageRequest request, IList<int> destination)
            => Query(layerId, region, destination, true, ReadInteger, request).Page;

        private static int ReadInteger(Fix value, SpatialLayerValueType type)
        {
            if (value.Raw % Fix.One.Raw != 0)
                throw new InvalidOperationException("integer tile layer returned a fractional sample");
            long integer = value.Raw / Fix.One.Raw;
            if (integer < int.MinValue || integer > int.MaxValue ||
                type == SpatialLayerValueType.Byte && (integer < byte.MinValue || integer > byte.MaxValue))
                throw new InvalidOperationException("integer tile sample is outside its declared representation");
            return (int)integer;
        }

        private (SpatialLayerReadStatus Status, SpatialLayerPageResult Page) Query<T>(string layerId,
            TileRect region, IList<T> destination, bool integer, Func<Fix, SpatialLayerValueType, T> convert,
            SpatialLayerPageRequest? request)
        {
            destination?.Clear();
            _world.RequireHealthyUpdate();
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            // Stage values until the original space/layer/clock observation verifies. Never publish a prefix.
            var result = EntityObservation.ReadProvider(_world, _space, null,
                () => Read(layerId, region, integer, convert, request), space: _space);
            if (result.Status == SpatialLayerReadStatus.Success)
                foreach (T value in result.Values) destination.Add(value);
            return (result.Status, result.Page);
        }

        private static (SpatialLayerReadStatus Status, SpatialLayerPageResult Page, List<T> Values)
            Refused<T>(SpatialLayerReadStatus status) => (status, SpatialLayerPageResult.Refused(status), null);

        private static TileCoord SampleTile(TileRect region, long offset)
            => new TileCoord(checked(region.X + (int)(offset % region.W)),
                checked(region.Y + (int)(offset / region.W)));

        private (SpatialLayerReadStatus Status, SpatialLayerPageResult Page, List<T> Values) Read<T>(string id,
            TileRect region, bool integer, Func<Fix, SpatialLayerValueType, T> convert, SpatialLayerPageRequest? request)
        {
            ISpatialLayer selected = null;
            foreach (ISpatialLayer layer in ((ISpatialLayerProvider)_space).SpatialLayers)
                if (layer.Id == id) { selected = layer; break; }
            if (!SpatialLayerAccess.CanObserve(_world, selected, _viewer)) return Refused<T>(SpatialLayerReadStatus.Unavailable);
            // Check private/missing identity before geometry or type, preserving indistinguishable refusals.
            if (region.X < 0 || region.Y < 0 || region.W < 0 || region.H < 0 ||
                (long)region.X + region.W > _space.Width || (long)region.Y + region.H > _space.Height)
                return Refused<T>(SpatialLayerReadStatus.InvalidRegion);
            SpatialLayerValueType valueType = selected.ValueType;
            if (integer ? valueType != SpatialLayerValueType.Byte && valueType != SpatialLayerValueType.Int32
                : valueType != SpatialLayerValueType.Fixed) return Refused<T>(SpatialLayerReadStatus.ValueTypeMismatch);
            long total = (long)region.W * region.H;
            long offset = request?.Offset ?? 0;
            int count;
            if (request.HasValue) {
                SpatialLayerReadStatus envelope = SpatialLayerPaging.CheckEnvelope(total, request.Value, out count);
                if (envelope != SpatialLayerReadStatus.Success) return Refused<T>(envelope);
            }
            else {
                if (total > int.MaxValue) return Refused<T>(SpatialLayerReadStatus.BufferCapacityExceeded);
                count = (int)total;
            }
            // Every requested sample must be visible before checking cursor freshness or sampling.
            // Pages authorize only their bounded slice, not every cell in a potentially huge region.
            for (int i = 0; i < count; i++)
                if (!SpatialLayerAccess.CanObserveSample(_world, selected, _viewer,
                    TileSpatialGeometry.SampleSnapshot(_space, SampleTile(region, offset + i))))
                    return Refused<T>(SpatialLayerReadStatus.Unavailable);
            SpatialLayerPageResult page = default;
            if (request.HasValue) {
                string binding = FormattableString.Invariant($"tile:{id.Length}:{id}:{region.X}:{region.Y}:{region.W}:{region.H}");
                page = SpatialLayerPaging.Check(total, EntityObservation.ReadSpace(_world, _viewer).Version,
                    _pages, binding, request.Value);
                if (page.Status != SpatialLayerReadStatus.Success) return Refused<T>(page.Status);
            }
            if (count == 0) return (SpatialLayerReadStatus.Success, page, new List<T>());
            var definition = _world.Content.Get<ISpatialLayerDefinition>(id);
            var samples = TileSpatialGeometry.RequireSampleLayer(_world, _space, definition, false);
            return EntityObservation.ReadProvider(_world, samples, null, () => {
                var values = new List<T>();
                for (int i = 0; i < count; i++) values.Add(convert(samples.GetSample(SampleTile(region, offset + i)), valueType));
                return (SpatialLayerReadStatus.Success, page, values);
            }, space: _space);
        }
    }
}
