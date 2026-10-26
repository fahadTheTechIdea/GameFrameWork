using System;
using System.Collections.Generic;

namespace Beep.SimEngine.Kernel
{
    /// <summary>Embedded actual cell endpoint. Hooks protect stock; books remain with this endpoint alone.</summary>
    public interface ISpatialResourceCellGuard : IResourceEndpoint
    {
        World World { get; }
        ISpatialLayer Layer { get; }
        Fix Quantity { get; }
        bool AllowsQuantity(Fix nativeAfter);
        void PreflightQuantityChange(Fix nativeAfter);
        void QuantityChanged();
        void PreflightOwnerChange();
        void OwnerChanged();
        /// <summary>Stage an existing book against candidate stock; reading publishes only through the returned action.</summary>
        Action StageSync(IStateSync sync, Fix nativeQuantity);
    }

    /// <summary>One per-layer binding/index/codec owner. Keys belong to the spatial provider, not this class.</summary>
    public sealed class SpatialResourceCellBindings
    {
        private readonly ISpatialLayer _layer;
        private World _world;
        private readonly SortedDictionary<long, ISpatialResourceCellGuard> _cells = new SortedDictionary<long, ISpatialResourceCellGuard>();
        public SpatialResourceCellBindings(ISpatialLayer layer) { _layer = layer ?? throw new ArgumentNullException(nameof(layer)); }
        public void Bind(long key, ISpatialResourceCellGuard cell)
        {
            if (key < 0 || cell == null || cell.World == null || cell.World.Space?.Id != _layer.Space ||
                _world != null && !ReferenceEquals(cell.World, _world) || !ReferenceEquals(cell.Layer, _layer) || !cell.Endpoint.IsValid ||
                _layer.ResourceMeasure != SpatialResourceMeasure.Quantity || _cells.ContainsKey(key))
                throw new Content.ContentException("invalid or duplicate spatial resource cell: " + _layer.Id);
            foreach (var existing in _cells.Values)
                if (existing.Endpoint == cell.Endpoint) throw new Content.ContentException("duplicate layer endpoint identity");
            _world = cell.World; _cells.Add(key, cell);
        }
        private bool Pure(ISpatialResourceCellGuard cell, Func<bool> check)
        {
            return SpatialMutation.Check(_world, () => (Version: cell.Version, Owner: _layer.Owner, Stock: cell.Quantity), check, before => {
                if (before.Version != cell.Version || before.Owner != _layer.Owner || before.Stock != cell.Quantity)
                    throw new InvalidOperationException("spatial resource guard mutated a pure admission");
            });
        }
        public bool AllowsQuantity(long key, Fix after) => !_cells.TryGetValue(key, out var cell) || Pure(cell, () => cell.AllowsQuantity(after));
        public void PreflightChange(long key, Fix after)
        {
            if (_cells.TryGetValue(key, out var cell)) Pure(cell, () => { cell.PreflightQuantityChange(after); return true; });
        }
        public void Changed(long key) { if (_cells.TryGetValue(key, out var cell)) cell.QuantityChanged(); }
        public IReadOnlyList<long> PreflightChanges(Func<long, Fix> candidate)
        {
            var changes = new List<long>();
            foreach (var binding in _cells)
            {
                Fix after = candidate(binding.Key);
                if (after == binding.Value.Quantity) continue;
                PreflightChange(binding.Key, after); changes.Add(binding.Key);
            }
            return changes;
        }
        public void ChangesCommitted(IReadOnlyList<long> changes) { foreach (long key in changes) Changed(key); }
        public void PreflightOwnerChange()
        { foreach (var cell in _cells.Values) Pure(cell, () => { cell.PreflightOwnerChange(); return true; }); }
        public void OwnerChanged() { foreach (var cell in _cells.Values) cell.OwnerChanged(); }
        private static readonly Action EmptyPublication = () => { };
        /// <summary>Stage actual books with a value-context sampler; no captured selector or alternate stock.</summary>
        public Action StageSync<TContext>(IStateSync sync, TContext context, Func<TContext, long, Fix> quantity)
        {
            if (sync.Count(_cells.Count) != _cells.Count) throw new CorruptStateException("spatial resource binding count differs from authored content");
            // Count is still encoded/validated. An actual empty set has no sampler or book callbacks.
            if (_cells.Count == 0) return sync.Reading ? EmptyPublication : null;
            return StageNonEmpty(sync, context, quantity);
        }
        private Action StageNonEmpty<TContext>(IStateSync sync, TContext context, Func<TContext, long, Fix> quantity)
        {
            var commits = sync.Reading ? new List<Action>() : null;
            foreach (var binding in _cells)
            {
                long key = binding.Key; sync.Long(ref key);
                if (key != binding.Key) throw new CorruptStateException("spatial resource binding geometry differs from authored content");
                int version = binding.Value.Version;
                Action commit = binding.Value.StageSync(sync, quantity(context, key));
                if (binding.Value.Version != version) throw new CorruptStateException("spatial resource checkpoint mutated before publication");
                if (sync.Reading)
                {
                    if (commit == null) throw new CorruptStateException("spatial resource owner supplied no staged restore");
                    commits.Add(commit);
                }
                else if (commit != null) throw new CorruptStateException("spatial resource writer supplied a restore action");
            }
            return sync.Reading ? Publication(commits) : null;
        }
        // Keep capture allocation on the read path only; writers/hashers never publish a restore.
        private static Action Publication(List<Action> commits) => () => { foreach (Action commit in commits) commit(); };
    }
}
