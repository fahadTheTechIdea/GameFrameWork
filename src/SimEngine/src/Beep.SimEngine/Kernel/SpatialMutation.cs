using System;
using System.Collections.Generic;

namespace Beep.SimEngine.Kernel
{
    /// <summary>The intrinsic publication owner used by spatial providers after pure admission.</summary>
    public interface ISpatialMutation : ISimulationOperationBoundary
    {
        void Commit(Action publish);
    }
    /// <summary>Publication boundary for selected spatial owners; World retains the only fault state.
    /// Pure admission happens before this call. Once publication starts, a failure is terminal, not rollback.</summary>
    public sealed class SpatialMutation : ISpatialMutation
    {
        private readonly World _world;
        private readonly SimulationOperation _operation = new SimulationOperation();
        internal SpatialMutation(World world) { _world = world ?? throw new ArgumentNullException(nameof(world)); }
        public bool OperationInProgress { get { _world.RequireHealthyUpdate(); return _operation.Active; } }
        internal sealed class Facts
        {
            private readonly EntityObservation.Scope _world;
            private readonly List<EntityObservation.Scope> _entities = new List<EntityObservation.Scope>();
            private readonly EntityObservation.SpatialScope _space;
            internal Facts(World world, bool observeSpace = false)
            {
                _world = EntityObservation.Capture(world);
                foreach (var entity in world.Entities.All) _entities.Add(EntityObservation.Capture(world, entity));
                _space = observeSpace ? EntityObservation.CaptureSpace(world, world.Space) : default;
            }
            internal void Verify()
            { _world.Verify(); foreach (var entity in _entities) entity.Verify(); _space.CheckUnchanged(); }
        }
        internal static T Check<TCapture,T>(World world, Func<TCapture> captureOwner, Func<T> read, Action<TCapture> verifyOwner)
            where TCapture : struct
        {
            world.RequireHealthyUpdate(); var facts = new Facts(world, observeSpace: true);
            TCapture? captured = null;
            try {
                captured = captureOwner();
                facts.Verify(); // A corrupt fact getter must not dispatch the guard method.
                T result = read();
                verifyOwner(captured.Value);
                facts.Verify(); // Verification getters belong to the SAME pure interval.
                world.RequireHealthyUpdate();
                return result;
            }
            catch (Exception error) {
                // An ordinary refusal is not a fault. Mutation plus refusal preserves the first error.
                try { if (captured.HasValue) verifyOwner(captured.Value); facts.Verify(); }
                catch { world.LatchFault("spatial.guard", error); }
                throw;
            }
        }
        public void Commit(Action publish)
        {
            if (publish == null) throw new ArgumentNullException(nameof(publish));
            World world = _world;
            world.RequireStateMutation();
            using var operation = _operation.Enter();
            var facts = new Facts(world);
            try {
                publish();
                facts.Verify();
                world.RequireHealthyUpdate();
            }
            catch (Exception error) { world.LatchFault("spatial.mutation", error); throw; }
        }
    }
}
