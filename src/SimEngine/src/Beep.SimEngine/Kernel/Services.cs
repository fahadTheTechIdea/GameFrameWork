using System;
using System.Collections.Generic;

namespace Beep.SimEngine.Kernel
{
    /// <summary>A declared engine operation owner, not a saved phase or a second game clock.
    /// The read must be pure. While true, no checkpoint or new game-time update may begin.</summary>
    public interface ISimulationOperationBoundary
    {
        bool OperationInProgress { get; }
    }

    // Reusable transient nesting primitive. The declaring service owns the instance;
    // it is neither a registered service, saved phase, resource ledger nor game clock.
    internal sealed class SimulationOperation
    {
        private int _depth;
        internal bool Active => _depth != 0;
        internal Scope Enter()
        {
            _depth = checked(_depth + 1);
            return new Scope(this);
        }
        internal readonly struct Scope : IDisposable
        {
            private readonly SimulationOperation _owner;
            internal Scope(SimulationOperation owner) { _owner = owner; }
            public void Dispose() { _owner._depth--; }
        }
    }

    /// <summary>Checks registered service/system/generation/command contracts before any system Initialize; aliases validate once.</summary>
    public interface ICompositionValidator
    {
        void ValidateComposition(World world);
    }
    /// <summary>Capture registered service/system/generation/command declarations after freeze, before cross-provider validation.</summary>
    public interface ICompositionPreparation
    {
        void PrepareComposition(World world);
    }

    /// <summary>
    /// The world's services by contract type (XNA's GameServiceContainer): an economy, a research
    /// table, a pathfinder. Each contract has one declared owner; bindings and requirements freeze
    /// before systems initialize. Aliases of a stateful instance share one persistence responsibility.
    /// </summary>
    public sealed class ServiceContainer
    {
        private sealed class Binding
        {
            public object Instance;
            public string Owner;
        }
        private readonly Dictionary<Type, Binding> _services = new Dictionary<Type, Binding>();
        private readonly List<Type> _order = new List<Type>();
        private readonly Dictionary<Type, List<string>> _requirements = new Dictionary<Type, List<string>>();
        private readonly List<PersistentServiceRegistration> _persistent = new List<PersistentServiceRegistration>();
        private bool _frozen;
        internal string ComposingModule { get; set; }

        private void RequireOwner()
        {
            if (_frozen || ComposingModule == null) throw new InvalidOperationException("service providers/requirements are declared by a composing module");
        }

        public void Require<T>() where T : class
        {
            RequireOwner();
            if (!_requirements.TryGetValue(typeof(T), out List<string> owners))
            {
                owners = new List<string>(); _requirements.Add(typeof(T), owners);
            }
            if (!owners.Contains(ComposingModule)) owners.Add(ComposingModule);
        }

        public void Bind<T>(T service) where T : class
        {
            RequireOwner();
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (_services.TryGetValue(typeof(T), out Binding existing))
                throw new InvalidOperationException("service '" + typeof(T).Name + "' already owned by '" + existing.Owner + "'; '" + ComposingModule + "' cannot replace it");
            foreach (Binding binding in _services.Values)
                if (ReferenceEquals(binding.Instance, service) && binding.Owner != ComposingModule)
                    throw new InvalidOperationException("service instance is already owned by '" + binding.Owner + "'");
            if (service is IPersistentService state)
            {
                bool alias = false;
                for (int i = 0; i < _persistent.Count; i++)
                    if (ReferenceEquals(_persistent[i].Provider, state)) alias = true;
                if (!alias)
                {
                    var registration = new PersistentServiceRegistration(state, ComposingModule);
                    for (int i = 0; i < _persistent.Count; i++)
                        if (_persistent[i].Metadata.Id == registration.Metadata.Id)
                            throw new InvalidOperationException("two services share the id '" + registration.Metadata.Id + "'");
                    _persistent.Add(registration);
                }
            }
            _order.Add(typeof(T));
            _services.Add(typeof(T), new Binding { Instance = service, Owner = ComposingModule });
        }

        public T Get<T>() where T : class
            => _services.TryGetValue(typeof(T), out Binding s) ? (T)s.Instance : throw new InvalidOperationException("no service " + typeof(T).Name + " is bound");

        public bool TryGet<T>(out T service) where T : class
        {
            service = _services.TryGetValue(typeof(T), out Binding s) ? (T)s.Instance : null;
            return service != null;
        }

        public bool Has<T>() where T : class => _services.ContainsKey(typeof(T));

        /// <summary>Declared service instances implementing a contract, in composition order; aliases appear once.</summary>
        public IReadOnlyList<T> Providers<T>() where T : class
        {
            var result = new List<T>();
            foreach (Type type in _order)
                if (_services[type].Instance is T provider)
                {
                    bool alias = false;
                    foreach (T previous in result) if (ReferenceEquals(previous, provider)) { alias = true; break; }
                    if (!alias) result.Add(provider);
                }
            return result.AsReadOnly();
        }

        internal string OwnerOf(object provider)
        {
            foreach (Type type in _order)
                if (ReferenceEquals(_services[type].Instance, provider)) return _services[type].Owner;
            throw new InvalidOperationException("provider is not a declared service");
        }

        internal void Freeze()
        {
            foreach (KeyValuePair<Type, List<string>> requirement in _requirements)
                if (!_services.ContainsKey(requirement.Key))
                    throw new InvalidOperationException("missing service '" + requirement.Key.Name + "' required by " + string.Join(",", requirement.Value));
            _persistent.Sort((a, b) => string.CompareOrdinal(a.Metadata.Id, b.Metadata.Id));
            _frozen = true;
        }

        /// <summary>Prepare every provider before cross-provider checks, each aliased instance once.</summary>
        internal void ValidateComposition(World world, IEnumerable<GameSystem> systems, IEnumerable<WorldGenStage> stages,
            IReadOnlyList<CommandRegistry.Entry> commands)
        {
            var seen = new List<object>();
            for (int i = 0; i < _order.Count; i++)
                Add(_services[_order[i]].Instance);
            foreach (GameSystem system in systems) Add(system);
            foreach (WorldGenStage stage in stages) Add(stage);
            foreach (CommandRegistry.Entry command in commands) Add(command.Handler);
            foreach (object provider in seen)
                if (provider is ICompositionPreparation preparation) preparation.PrepareComposition(world);
            foreach (object provider in seen)
                if (provider is ICompositionValidator validator) validator.ValidateComposition(world);

            void Add(object provider)
            {
                for (int i = 0; i < seen.Count; i++) if (ReferenceEquals(seen[i], provider)) return;
                seen.Add(provider);
            }
        }

        /// <summary>Stateful services in captured ID order, each instance once.</summary>
        internal IReadOnlyList<PersistentServiceRegistration> Persistent()
        {
            if (!_frozen) throw new InvalidOperationException("service codecs are settled at composition freeze");
            return _persistent.AsReadOnly();
        }
    }
}
