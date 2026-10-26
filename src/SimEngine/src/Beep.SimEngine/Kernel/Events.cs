using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Kernel
{
    /// <summary>Optional immutable, authored dispatch safety policy; null means no event-volume cap.</summary>
    public interface IEventDispatchPolicy : IContentDefinition
    {
        long? MaximumEventsPerDispatch { get; }
    }
    public static class EventDispatchPolicyContracts
    {
        public static long? Read(World world, IEventDispatchPolicy policy)
            => EntityObservation.ReadProvider(world, policy, null, () => {
                if (policy == null || !world.Content.Owns(policy) ||
                    !ReferenceEquals(world.Content.Find<IEventDispatchPolicy>(policy.Id), policy))
                    throw new ContentException("event dispatch policy requires its actual immutable catalog alias");
                long? limit = policy.MaximumEventsPerDispatch;
                if (limit.HasValue && limit.Value <= 0)
                    throw new ContentException(policy.Id + ": event dispatch limit must be positive or explicit null");
                return limit;
            });
    }
    /// <summary>Immutable declaration of one module-owned event payload contract.</summary>
    public sealed class EventRegistration
    {
        internal EventRegistration(string id, int schemaVersion, string owner, Type contract)
        {
            Id = id; SchemaVersion = schemaVersion; Owner = owner; Contract = contract;
        }
        public string Id { get; }
        public int SchemaVersion { get; }
        public string Owner { get; }
        public Type Contract { get; }
    }

    /// <summary>Detached subscription identity. No executable delegate is exposed.</summary>
    public readonly struct EventSubscription
    {
        internal EventSubscription(string id, string owner, string eventId)
        {
            Id = id; Owner = owner; EventId = eventId;
        }
        public string Id { get; }
        public string Owner { get; }
        public string EventId { get; }
    }

    /// <summary>
    /// Module-declared in-world events. Struct payloads dispatch without boxing at the fixed event
    /// phase, in global publication order, including events published by handlers. Declarations and
    /// owned subscriptions freeze before system initialization. Bound to its one World; a terminal
    /// failure stops before the next listener. No event remains pending after dispatch, even on failure.
    /// </summary>
    public sealed class EventBus
    {
        private readonly struct Requirement
        {
            public readonly string Id, Owner;
            public readonly int Version;
            public Requirement(string id, int version, string owner) { Id = id; Version = version; Owner = owner; }
        }

        private interface IQueue
        {
            EventRegistration Registration { get; set; }
            List<Requirement> Requirements { get; }
            bool HasSubscription(string id);
            void CopySubscriptions(IList<EventSubscription> into);
            void DispatchNext(World world);
            void Reset();
        }

        private sealed class Queue<T> : IQueue where T : struct
        {
            private readonly struct Handler
            {
                public readonly string Id, Owner;
                public readonly Action<T> Callback;
                public Handler(string id, string owner, Action<T> callback) { Id = id; Owner = owner; Callback = callback; }
            }
            private readonly List<Handler> _handlers = new List<Handler>();
            private T[] _items = new T[8];
            private int _count, _read;
            public EventRegistration Registration { get; set; }
            public List<Requirement> Requirements { get; } = new List<Requirement>();
            public bool HasHandlers => _handlers.Count > 0;
            public bool Empty => _count == 0;
            public void Subscribe(string id, string owner, Action<T> handler) => _handlers.Add(new Handler(id, owner, handler));
            public bool HasSubscription(string id)
            {
                foreach (Handler handler in _handlers) if (handler.Id == id) return true;
                return false;
            }
            public void CopySubscriptions(IList<EventSubscription> into)
            {
                foreach (Handler handler in _handlers)
                    into.Add(new EventSubscription(handler.Id, handler.Owner, Registration.Id));
            }
            public void Push(in T e)
            {
                if (_count == _items.Length) Array.Resize(ref _items, _items.Length * 2);
                _items[_count++] = e;
            }
            public void DispatchNext(World world)
            {
                T e = _items[_read++];
                for (int i = 0; i < _handlers.Count; i++)
                {
                    world.RequireHealthyUpdate();
                    try { _handlers[i].Callback(e); }
                    catch (Exception error)
                    {
                        world.LatchFault("events." + _handlers[i].Id, error);
                        throw;
                    }
                    world.RequireHealthyUpdate();
                }
            }
            public void Reset()
            {
                Array.Clear(_items, 0, _count);
                _count = 0; _read = 0;
            }
        }

        private long? _maximumEventsPerDispatch;
        public long? MaximumEventsPerDispatch => _maximumEventsPerDispatch;
        private readonly World _world;
        private readonly Dictionary<Type, IQueue> _queues = new Dictionary<Type, IQueue>();
        private readonly List<IQueue> _order = new List<IQueue>();
        private readonly List<IQueue> _touched = new List<IQueue>();
        private bool _dispatching, _frozen;
        internal string ComposingModule { get; set; }
        public bool HasPending => _order.Count != 0;

        internal EventBus(World world) { _world = world ?? throw new ArgumentNullException(nameof(world)); }

        private void RequireComposition()
        {
            if (_frozen || ComposingModule == null)
                throw new InvalidOperationException("event declarations/subscriptions belong to a composing module");
        }

        // A forward subscription/requirement uses the same entry later declared by its provider.
        // It acquires no implicit identity and cannot publish until the declaration passes freeze.
        private Queue<T> CompositionQueue<T>() where T : struct
        {
            if (!_queues.TryGetValue(typeof(T), out IQueue queue))
            {
                queue = new Queue<T>(); _queues.Add(typeof(T), queue);
            }
            return (Queue<T>)queue;
        }

        public void Register<T>(string id, int schemaVersion) where T : struct
        {
            RequireComposition();
            ContractIdentity.Validate(id, schemaVersion, "event");
            foreach (IQueue queue in _queues.Values)
                if (queue.Registration != null && (queue.Registration.Id == id || queue.Registration.Contract == typeof(T)))
                    throw new InvalidOperationException("event '" + id + "' or payload " + typeof(T).Name
                        + " already declared by '" + queue.Registration.Owner + "'");
            CompositionQueue<T>().Registration = new EventRegistration(id, schemaVersion, ComposingModule, typeof(T));
        }

        public void Require<T>(string id, int schemaVersion) where T : struct
        {
            RequireComposition();
            ContractIdentity.Validate(id, schemaVersion, "event");
            CompositionQueue<T>().Requirements.Add(new Requirement(id, schemaVersion, ComposingModule));
        }

        public void Subscribe<T>(string id, Action<T> handler) where T : struct
        {
            RequireComposition();
            ContractIdentity.ValidateId(id, "event subscription");
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            foreach (IQueue queue in _queues.Values)
                if (queue.HasSubscription(id)) throw new InvalidOperationException("event subscription id '" + id + "' used twice");
            CompositionQueue<T>().Subscribe(id, ComposingModule, handler);
        }

        internal void Freeze()
        {
            if (_world.Services.TryGet<IEventDispatchPolicy>(out var policy))
                _maximumEventsPerDispatch = EventDispatchPolicyContracts.Read(_world, policy);
            foreach (KeyValuePair<Type, IQueue> entry in _queues)
            {
                EventRegistration registration = entry.Value.Registration;
                if (registration == null)
                    throw new InvalidOperationException("event payload '" + entry.Key.Name + "' has subscribers/requirements but no module declaration");
                foreach (Requirement requirement in entry.Value.Requirements)
                    if (registration.Id != requirement.Id || registration.SchemaVersion != requirement.Version)
                        throw new InvalidOperationException("module '" + requirement.Owner + "' requires event '"
                            + requirement.Id + "' schema " + requirement.Version + "; provider identity/schema differs");
            }
            _frozen = true;
        }

        public IReadOnlyList<EventRegistration> Registrations
        {
            get
            {
                if (!_frozen) throw new InvalidOperationException("event composition is not settled");
                var entries = new List<EventRegistration>(_queues.Count);
                foreach (IQueue queue in _queues.Values) entries.Add(queue.Registration);
                entries.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return entries.AsReadOnly();
            }
        }

        public IReadOnlyList<EventSubscription> Subscriptions
        {
            get
            {
                if (!_frozen) throw new InvalidOperationException("event composition is not settled");
                var entries = new List<EventSubscription>();
                foreach (IQueue queue in _queues.Values) queue.CopySubscriptions(entries);
                entries.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                return entries.AsReadOnly();
            }
        }

        public void Publish<T>(in T e) where T : struct
        {
            _world.RequireHealthyUpdate();
            if (!_frozen) throw new InvalidOperationException("events publish only after composition freeze");
            if (!_queues.TryGetValue(typeof(T), out IQueue entry))
                throw new InvalidOperationException("undeclared event payload '" + typeof(T).Name + "'");
            var queue = (Queue<T>)entry;
            if (!queue.HasHandlers) return;
            if (_maximumEventsPerDispatch.HasValue && _order.Count >= _maximumEventsPerDispatch.Value)
            {
                var error = new InvalidOperationException("authored event dispatch budget exceeded: " + _maximumEventsPerDispatch.Value);
                _world.LatchFault("events.budget", error);
                DiscardPending();
                throw error;
            }
            if (queue.Empty) _touched.Add(queue);
            queue.Push(e);
            _order.Add(queue);
        }

        internal void Dispatch()
        {
            if (_dispatching) return;
            _dispatching = true;
            try
            {
                _world.RequireHealthyUpdate();
                for (int i = 0; i < _order.Count; i++) _order[i].DispatchNext(_world);
            }
            finally
            {
                DiscardPending();
                _dispatching = false;
            }
        }
        private void DiscardPending()
        {
            _order.Clear();
            for (int i = 0; i < _touched.Count; i++) _touched[i].Reset();
            _touched.Clear();
        }
    }
}
