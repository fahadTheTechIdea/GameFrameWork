using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>One authored connectivity/eligibility algorithm; existing topology and exposure owners keep all state.</summary>
    public sealed class NetworkIncidentPropagationDeclaration
    {
        private readonly IncidentPropagationPolicyDeclaration _policy;
        private readonly string _network;
        public INetworkDefinition Network { get; private set; }
        public GameDuration Interval => _policy.Interval;
        public GameDuration Delay => _policy.Delay;
        public Code TargetSource => _policy.TargetSource;
        public Code TargetKind => _policy.TargetKind;
        public bool CrossCompany => _policy.CrossCompany;
        private NetworkIncidentPropagationDeclaration(JsonReader reader)
        {
            _policy = IncidentPropagationPolicyDeclaration.Read(reader);
            _network = reader.String("network");
            try { ContractIdentity.ValidateId(_network, "incident propagation network"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
        }
        public static NetworkIncidentPropagationDeclaration Read(JsonReader reader)
            => new NetworkIncidentPropagationDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (Network != null) throw new InvalidOperationException("network propagation resolves once");
            Network = content.Def<INetworkDefinition>(_network); _policy.Resolve(content);
        }
        public void ValidateSource(EntityDef source)
        {
            _policy.ValidateSource(source);
            NetworkConnectionContracts.Validate(source, Network);
        }
        public void ValidateBinding(World world, string id)
        {
            _policy.ValidateBinding(world, id);
            if (Network == null || !world.Content.Owns(Network) ||
                !ReferenceEquals(world.Content.Find<INetworkDefinition>(Network.Id), Network))
                throw new ContentException(id + ": propagation requires its actual immutable network");
            if (!world.Services.TryGet<INetworkDiagnostics>(out _))
                throw new ContentException(id + ": network propagation requires explicit network diagnostics");
        }
        public IncidentExposureObservation Observe(IncidentPropagationContext context, Entity target, string id)
        {
            var diagnostics = context.World.Services.Get<INetworkDiagnostics>();
            // Each endpoint is authorized as its own owner; the existing propagation owner enforces company scope.
            Code blocker = diagnostics.ReadConnection(context.Source.Id, Network.Id, context.Source.Owner, out var source);
            if (!blocker.IsNone) return IncidentExposureObservation.Refused(blocker);
            blocker = diagnostics.ReadConnection(target.Id, Network.Id, target.Owner, out var destination);
            if (!blocker.IsNone) return IncidentExposureObservation.Refused(blocker);
            if (source.Endpoint != destination.Endpoint)
                return IncidentExposureObservation.Refused(NetworkService.Disconnected);
            blocker = NetworkGateContracts.Read(context.World, context.Source, Network).Blocker;
            if (blocker.IsNone) blocker = NetworkGateContracts.Read(context.World, target, Network).Blocker;
            if (blocker.IsNone) blocker = _policy.CheckConditions(context, target, id);
            return blocker.IsNone ? IncidentExposureObservation.Ready : IncidentExposureObservation.Refused(blocker);
        }
    }

    public sealed class NetworkIncidentPropagationRuleDef : Def, IIncidentPropagationRule
    {
        private NetworkIncidentPropagationDeclaration _declaration;
        public GameDuration Interval => _declaration.Interval;
        public GameDuration Delay => _declaration.Delay;
        public Code TargetSource => _declaration.TargetSource;
        public Code TargetKind => _declaration.TargetKind;
        public bool CrossCompany => _declaration.CrossCompany;
        protected override void ReadCore(JsonReader reader) => _declaration = NetworkIncidentPropagationDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateSource(EntityDef source) => _declaration.ValidateSource(source);
        public void ValidateBinding(World world) => _declaration.ValidateBinding(world, Id);
        public IncidentExposureObservation Observe(IncidentPropagationContext context, Entity target)
            => _declaration.Observe(context, target, Id);
    }
}
