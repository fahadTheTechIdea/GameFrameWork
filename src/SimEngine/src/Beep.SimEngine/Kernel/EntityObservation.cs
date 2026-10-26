using System;
using System.Collections.Generic;

namespace Beep.SimEngine.Kernel
{
    /// <summary>One scoped observation guard; not a world hash, state copy or provider sandbox.</summary>
    internal static class EntityObservation
    {
        /// <summary>Reject cycles in an actual provider operation, not valid calls to another operation.</summary>
        internal static ProviderCallScope EnterProviderCall(World world, object provider, string operation)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("explicit provider operation required", nameof(operation));
            world.RequireHealthyUpdate();
            foreach (var active in world.ActiveProviderCalls)
                if (ReferenceEquals(active.Provider, provider) && StringComparer.Ordinal.Equals(active.Operation, operation))
                {
                    var error = new InvalidOperationException("recursive provider callback: " + operation);
                    world.LatchFault(operation, error); throw error;
                }
            world.ActiveProviderCalls.Add((provider, operation));
            return new ProviderCallScope(world);
        }
        internal readonly struct ProviderCallScope : IDisposable
        {
            private readonly World _world;
            internal ProviderCallScope(World world) { _world = world; }
            // Only lexical internal using scopes own these entries. Cleanup cannot verify
            // or replace the original callback failure with another observation error.
            public void Dispose() => _world.ActiveProviderCalls.RemoveAt(_world.ActiveProviderCalls.Count - 1);
        }
        /// <summary>One capture boundary shared by entity projections and physical invariants.</summary>
        internal static SpatialSnapshot ReadSpatial(World world, Entity entity)
        {
            world.RequireHealthyUpdate();
            var presence = entity.Get<ISpatialPresence>();
            return presence == null ? null : ReadProvider(world, presence, entity, () => {
                SpatialSnapshot value = presence.Capture();
                if (value == null || world.Space == null || presence.Space != world.Space.Id || value.Space != presence.Space)
                    throw new InvalidOperationException("entity spatial snapshot must identify its actual selected space");
                return value;
            }, space: world.Space, observeTargetState: true);
        }
        /// <summary>One original symmetric detached-shape comparison, observed at its actual presence owner.</summary>
        internal static bool MatchesSpatial(World world, Entity entity, SpatialSnapshot current, SpatialSnapshot previous)
        {
            if (current == null || previous == null) return current == null && previous == null;
            if (current.Space != previous.Space || current.GetType() != previous.GetType()) return false;
            ISpatialPresence presence = entity?.Get<ISpatialPresence>();
            if (presence == null) throw new InvalidOperationException("spatial comparison requires its actual presence owner");
            return ReadProvider(world, presence, entity, () => {
                if (!current.Matches(current) || !previous.Matches(previous))
                    throw new InvalidOperationException("spatial snapshot comparison must be reflexive");
                bool forward = current.Matches(previous), reverse = previous.Matches(current);
                if (forward != reverse || current.Matches(previous) != forward || previous.Matches(current) != reverse)
                    throw new InvalidOperationException("spatial snapshot comparison must be symmetric and stable");
                return forward;
            }, space: world.Space, observeTargetState: true);
        }
        /// <summary>One admission boundary for live player reads and published world projections.</summary>
        internal static SpatialWorldSnapshot ReadSpace(World world, PlayerId viewer)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.RequireHealthyUpdate();
            ISpatialSpace space = world.Space;
            if (space == null) return null;
            return ReadProvider(world, space, null, () =>
            {
                SpatialWorldSnapshot value = space.Capture(viewer);
                if (value == null || value.Space != space.Id || value.Version < 0)
                    throw new InvalidOperationException("world spatial snapshot must identify its actual selected space and viewer revision");
                return value;
            }, space: space);
        }

        /// <summary>One pure provider interval over original codecs of its actual bound participants.</summary>
        internal static T ReadParticipants<T>(World world, object provider, IEnumerable<Entity> participants, Func<T> read, ISpatialSpace space = null)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (read == null) throw new ArgumentNullException(nameof(read));
            return ReadProvider(world, provider, null, () => {
                var captured = new List<(Scope Facts, ProviderScope State)>();
                var seen = new HashSet<EntityId>();
                foreach (Entity entity in participants)
                {
                    if (entity == null || !seen.Add(entity.Id)) continue;
                    captured.Add((Capture(world, entity), CaptureProvider(world, entity, entity.Sync)));
                }
                T result = read();
                // Original callback errors are retained by ReadProvider; never mask them in finally.
                foreach (var participant in captured) { participant.Facts.Verify(); participant.State.Verify(); }
                return result;
            }, space: space);
        }

        /// <summary>Return facts only after the actual provider and target scopes verify.</summary>
        internal static T ReadProvider<T>(World world, object provider, Entity target, Func<T> read, ISpatialSpace space = null, bool observeTargetState = false,
            string faultPhase = "observation.provider")
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (read == null) throw new ArgumentNullException(nameof(read));
            return ReadProvider(world, provider, target, read, ReadInvocation<T>.Invoke, space, observeTargetState, faultPhase);
        }
        // One cached stateless invocation per result type, not another observation owner.
        private static class ReadInvocation<T>
        {
            internal static readonly Func<Func<T>, T> Invoke = read => read();
        }
        /// <summary>The same boundary with value context, so hot callers need no captured closure.</summary>
        internal static T ReadProvider<TContext, T>(World world, object provider, Entity target, TContext context,
            Func<TContext, T> read, ISpatialSpace space = null, bool observeTargetState = false, string faultPhase = "observation.provider")
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (read == null) throw new ArgumentNullException(nameof(read));
            if (string.IsNullOrWhiteSpace(faultPhase)) throw new ArgumentException("observation fault phase required", nameof(faultPhase));
            var previousSpatial = world.ActiveSpatialObservation;
            try
            {
                // A null caller target still has an actual codec owner. Use the engine binding,
                // not the provider's interface getter, before observing the whole entity.
                if (observeTargetState && target == null && provider is Component owner) target = owner.Entity;
                var facts = target == null ? Capture(world) : Capture(world, target);
                var spatial = space == null ? default : CaptureSpace(world, space);
                if (space != null) world.ActiveSpatialObservation = spatial;
                // The selected space's original codec/revision is already captured and verified by spatial.
                // Do not hash the same provider again through its inherited persistent-service alias.
                bool separateProvider = space == null || !ReferenceEquals(provider, space);
                var scope = separateProvider ? CaptureProvider(world, provider, faultPhase) : default;
                var targetState = observeTargetState && target != null ? CaptureProvider(world, target, target.Sync, faultPhase) : default;
                facts.Verify();
                world.RequireHealthyUpdate();
                T value = read(context);
                facts.Verify();
                if (separateProvider) scope.Verify();
                if (observeTargetState && target != null) targetState.Verify();
                spatial.Verify();
                return value;
            }
            catch (Exception error) { world.LatchFault(faultPhase, error); throw; }
            finally { world.ActiveSpatialObservation = previousSpatial; }
        }
        internal static T Read<T>(World world, Entity entity, Func<T> read)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            var scope = Capture(world, entity);
            T value = read();
            scope.Verify();
            return value;
        }
        internal static Scope Capture(World world, Entity entity, bool allowEntityVersionChange = false)
        {
            if (world == null || entity == null || !ReferenceEquals(world.Entities.Get(entity.Id), entity))
                throw new ArgumentException("observation requires its actual session entity");
            return new Scope(world, entity, allowEntityVersionChange);
        }
        internal static Scope Capture(World world)
            => new Scope(world ?? throw new ArgumentNullException(nameof(world)), null);
        /// <summary>Observe the actual selected space through its original codec and invalidation revision.</summary>
        internal static SpatialScope CaptureSpace(World world, ISpatialSpace space)
            => new SpatialScope(world, space);
        internal readonly struct SpatialScope
        {
            private readonly World _world;
            private readonly ISpatialSpace _space;
            private readonly ProviderScope _state;
            private readonly int _version;
            internal SpatialScope(World world, ISpatialSpace space)
            {
                _world = world ?? throw new ArgumentNullException(nameof(world)); _space = space;
                int previousCapture = world.ObservationCaptureDepth;
                var entrance = Capture(world);
                try
                {
                    world.ObservationCaptureDepth = checked(previousCapture + 1);
                    if (!ReferenceEquals(world.Space, space))
                        throw new ArgumentException("spatial observation requires its actual selected session space");
                    _version = space?.ObservationVersion ?? 0;
                    if (_version < 0) throw new InvalidOperationException("spatial observation requires a nonnegative original revision");
                    // Preserve the selected space's explicit interface codec even if that
                    // provider also implements another persistent protocol. No bound delegate.
                    var enclosing = world.ActiveSpatialObservation;
                    bool inherited = enclosing.HasValue && ReferenceEquals(enclosing.Value._world, world) &&
                        ReferenceEquals(enclosing.Value._space, space);
                    // Reuse ONLY a currently enclosing pure interval's original baseline.
                    // Getter and callback verification still hash the complete actual space.
                    // A corrupt parent prefix must not become a child's healthy baseline.
                    _state = inherited ? enclosing.Value._state : new ProviderScope(world, space, null, "observation.provider", false,
                        space == null ? CodecSource.None : CodecSource.Space);
                    _state.Verify();
                    entrance.Verify();
                    if (!ReferenceEquals(world.Space, space) || (space?.ObservationVersion ?? 0) != _version)
                        throw new InvalidOperationException("spatial observation changed during baseline capture");
                    if (inherited && _version != enclosing.Value._version)
                        throw new InvalidOperationException("nested spatial observation changed its enclosing revision");
                }
                catch (Exception error) { world.LatchFault("observation.space", error); throw; }
                finally { world.ObservationCaptureDepth = previousCapture; }
            }
            internal void Verify()
            {
                if (_world == null) return;
                try { VerifyCore(latchProvider: true); }
                catch (Exception error) { _world.LatchFault("observation.space", error); throw; }
            }
            // A pure-admission boundary may need to retain a callback's ORIGINAL refusal as
            // the first fault after detecting mutation, rather than this verification error.
            internal void CheckUnchanged()
                => VerifyCore(latchProvider: false);
            private void VerifyCore(bool latchProvider)
            {
                if (_world == null) return;
                if (!ReferenceEquals(_world.Space, _space))
                    throw new InvalidOperationException("spatial observation changed its actual selected session space");
                int version = _space?.ObservationVersion ?? 0;
                if (latchProvider) _state.Verify(); else _state.CheckUnchanged();
                if (version < 0 || version != _version)
                    throw new InvalidOperationException("spatial observation mutated its original revision");
            }
        }
        /// <summary>Observe the provider's actual existing codec, never another persistence model.</summary>
        internal static ProviderScope CaptureProvider(World world, object provider, string faultPhase = "observation.provider")
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            CodecSource source = provider is Component ? CodecSource.Component
                : provider is GameSystem ? CodecSource.System
                : provider is IPersistentService ? CodecSource.Service
                : provider is ISpatialSpace ? CodecSource.Space
                : provider is Hosting.IPlayerControllerState ? CodecSource.Controller : CodecSource.None;
            return new ProviderScope(world, provider, null, faultPhase, false, source);
        }
        internal static ProviderScope CaptureProvider(World world, object provider, Action<IStateSync> codec, string faultPhase = "observation.provider", bool allowEntityVersionChange = false)
            => new ProviderScope(world, provider, codec, faultPhase, allowEntityVersionChange, codec == null ? CodecSource.None : CodecSource.Explicit);
        internal enum CodecSource : byte { None, Explicit, Component, System, Service, Space, Controller }
        internal readonly struct ProviderScope
        {
            private readonly World _world;
            private readonly Scope _facts;
            private readonly Action<IStateSync> _codec;
            private readonly object _provider;
            private readonly CodecSource _source;
            private readonly ulong _state;
            private readonly string _faultPhase;
            internal ProviderScope(World world, object provider, Action<IStateSync> codec, string faultPhase, bool allowEntityVersionChange, CodecSource source)
            {
                _world = world ?? throw new ArgumentNullException(nameof(world));
                _faultPhase = string.IsNullOrWhiteSpace(faultPhase) ? throw new ArgumentException("observation owner fault phase required", nameof(faultPhase)) : faultPhase;
                Entity entity = provider is Component component ? component.Entity : null;
                _facts = entity == null ? Capture(world) : Capture(world, entity, allowEntityVersionChange);
                _codec = codec;
                _provider = provider; _source = source;
                try { world.RequireHealthyUpdate(); _state = Hash(world, provider, codec, source); _facts.Verify(); }
                catch (Exception error) { world.LatchFault(_faultPhase, error); throw; }
            }
            private static ulong Hash(World world, object provider, Action<IStateSync> codec, CodecSource source)
            {
                if (source == CodecSource.None) return 0;
                int previousCapture = world.ObservationCaptureDepth;
                world.ObservationCaptureDepth = checked(previousCapture + 1);
                try {
                    var hash = new StateHasher(observation: true);
                    // Same original codec dispatch/projection and fresh, non-escaping hash ownership.
                    // Explicit callbacks remain supported for callers selecting a particular codec.
                    switch (source)
                    {
                        case CodecSource.Explicit: codec(hash); break;
                        case CodecSource.Component: ((Component)provider).Sync(hash); break;
                        case CodecSource.System: ((GameSystem)provider).Sync(hash); break;
                        case CodecSource.Service: ((IPersistentService)provider).Sync(hash); break;
                        case CodecSource.Space: ((ISpatialSpace)provider).Sync(hash); break;
                        case CodecSource.Controller: ((Hosting.IPlayerControllerState)provider).Sync(hash); break;
                        default: throw new InvalidOperationException("unknown observation codec source");
                    }
                    return hash.Value;
                }
                finally { world.ObservationCaptureDepth = previousCapture; }
            }
            internal void Verify()
            {
                try { CheckUnchanged(); }
                catch (Exception error)
                { _world.LatchFault(_faultPhase, error); throw; }
            }
            internal void CheckUnchanged()
            {
                _facts.Verify();
                if (_state != Hash(_world, _provider, _codec, _source)) throw new InvalidOperationException("provider observation mutated its persisted state");
                _facts.Verify();
                _world.RequireHealthyUpdate();
            }
        }
        // Transient scalar facts, not serialized state; direct callback users need no delegate allocation.
        internal readonly struct Scope
        {
            private readonly World _world;
            private readonly Entity _entity;
            private readonly IGameClock _clock;
            private readonly int _version, _speed;
            private readonly PlayerId _owner;
            private readonly bool _alive, _paused;
            private readonly bool _allowEntityVersionChange;
            private readonly Code _state;
            private readonly long _since, _update;
            private readonly EntityId _next;
            private readonly ulong _randomState, _randomVersion;
            internal Scope(World world, Entity entity, bool allowEntityVersionChange = false)
            {
                _world = world; _entity = entity; _clock = world.Clock;
                _allowEntityVersionChange = allowEntityVersionChange;
                _version = entity?.Version ?? 0; _owner = entity?.Owner ?? PlayerId.None; _alive = entity?.Alive ?? false;
                _state = entity?.State ?? Code.None; _since = entity?.StateSince ?? 0; _update = _clock.StepIndex;
                _paused = _clock.Paused; _speed = _clock.Speed; _next = world.Entities.NextIdentity;
                _randomState = world.Random.State; _randomVersion = world.Random.ObservationVersion;
            }
            internal void Verify()
            {
                if (_entity != null && (!ReferenceEquals(_world.Entities.Get(_entity.Id), _entity) || !_allowEntityVersionChange && _entity.Version != _version || _entity.Owner != _owner ||
                    _entity.Alive != _alive || _entity.State != _state || _entity.StateSince != _since) ||
                    !ReferenceEquals(_world.Clock, _clock) || _clock.StepIndex != _update || _clock.Paused != _paused || _clock.Speed != _speed ||
                    _world.Entities.NextIdentity != _next || _world.Random.State != _randomState || _world.Random.ObservationVersion != _randomVersion)
                    throw new InvalidOperationException("entity observation mutated scoped state, identity, random-stream or game-clock facts");
            }
        }
    }
}
