using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Standard
{
    public sealed partial class LogisticsService
    {
        private ResourcePortResolution PhysicalPort(Entity site, Code name, ResourceId resource, ResourceEndpointId? original = null)
        {
            _world.RequireHealthyUpdate();
            if (site?.Alive != true) return ResourcePortResolution.Unavailable(Codes.NotFound);
            return EntityObservation.ReadProvider(_world,site,site,()=>
            {
                if (name.IsNone)
                {
                    IInletPort inlet = site.Get<IInletPort>();
                    if(inlet==null)return ResourcePortResolution.Unavailable(StandardCodes.NoStorage);
                    return original.HasValue && inlet.Endpoint != original.Value
                        ? ResourcePortResolution.Unavailable(ResourcePortCodes.OriginalUnavailable):ResourcePortResolution.ReadyStore(inlet);
                }
                IResourcePort port = site.Get<IResourcePort>(name);
                var definition=site.Def.Component<IResourcePortDefinition>(name);
                if(port==null||definition==null)return ResourcePortResolution.Unavailable(Codes.NotFound); // No inventory fallback.
                if(!definition.Accepts(_world.Content,resource))return ResourcePortResolution.Unavailable(StandardCodes.NotAccepted);
                var binding=ResourcePortContracts.Bind(_world,site,resource,port,original);
                return binding.Ready?ResourcePortResolution.ReadyStore(binding.Store):ResourcePortResolution.Unavailable(binding.Blocker);
            },observeTargetState:true);
        }

        // One deterministic discovery path for source matching and recovery destinations.
        // The callback observes actual stores only; names never create another stock owner.
        private void VisitPorts(Entity site, ResourceId resource, bool publicOnly, Action<Code, IResourceInlet> visit)
        {
            EntityObservation.Read(_world, site, () =>
            {
                void Observe(Code name, IResourceInlet inlet)
                {
                    if (inlet == null) return;
                    int version = inlet.Version; var endpoint = inlet.Endpoint; var company = inlet.Company;
                    var capacity = inlet.CapacityPolicy;
                    try
                    {
                        if ((!publicOnly || inlet.Public) && inlet.Accepts(_world.Content, resource)) visit(name, inlet);
                    }
                    finally
                    {
                        if (inlet.Version != version || inlet.Endpoint != endpoint || inlet.Company != company ||
                            !ReferenceEquals(inlet.CapacityPolicy, capacity))
                            throw new InvalidOperationException("port discovery mutated its actual endpoint");
                    }
                }
                Observe(Code.None, site.Get<IInletPort>());
                var names = new SortedDictionary<string, IResourcePortDefinition>(StringComparer.Ordinal);
                foreach (ComponentDef part in site.Def.Components)
                    if (part is IResourcePortDefinition definition && definition.Accepts(_world.Content, resource))
                        names.Add(definition.CapabilityId.Id, definition);
                foreach (var entry in names)
                {
                    ResourcePortBinding binding = ResourcePortContracts.Bind(_world, site, resource,
                        site.Get<IResourcePort>(entry.Value.CapabilityId));
                    if(!binding.Ready)continue;
                    Observe(entry.Value.CapabilityId, binding.Store);
                    ResourcePortContracts.Verify(new[] { binding });
                }
                return true;
            });
        }

        public ResourcePortResolution ReadHaulEndpoint(Job job, bool pickup)
        {
            _world.RequireHealthyUpdate();
            if (job == null || !job.IsHaul || !ReferenceEquals(_world.Services.Get<WorkBoard>().Job(job.Id), job))
                throw new InvalidOperationException("endpoint read requires the actual haul job");
            ResourceEndpointId endpoint = pickup ? job.SourceEndpoint : job.DestinationEndpoint;
            return PhysicalPort(_world.Entities.Get(pickup ? job.From : job.To),
                pickup ? job.SourcePort : job.DestinationPort, job.Resource, endpoint);
        }

        public Code CheckDestinationBinding(Job job)
        {
            // ReadHaulEndpoint owns actual session/job validation and explicit-original resolution.
            var endpoint = ReadHaulEndpoint(job, false);
            return _world.Entities.Get(job.To) == null ? Codes.NotFound :
                !endpoint.Ready ? HaulRecoveryCodes.DestinationBindingLost : Code.None;
        }

        private Fix PortSpare(Entity site, IResourceOutlet store, ResourceId resource, bool emergency, Code port)
        {
            OutletPolicySettings policy = LogisticsPolicyRules.Read(_world, site, port);
            if (policy?.Enabled == false) return Fix.Zero;
            Fix reserve = policy == null || emergency && policy.ReserveAccess == LogisticsReserveAccess.EmergencyOnly
                ? Fix.Zero : policy.Reserve.AmountOf(resource);
            // Every stock band protects only its actual endpoint, regardless of physical site/name aliases.
            return Fix.Max(Fix.Zero, store.Available(resource) - Fix.Max(reserve, DemandOf(store.Endpoint, resource, emergency)));
        }

        private Fix DeliveryGap(Demand demand, bool emergencyOnly)
        {
            IResourceInlet inlet = PhysicalPort(_world.Entities.Get(demand.Store), demand.Port, demand.Resource, demand.Endpoint).Store;
            if (inlet == null) return Fix.Zero;
            if (HasQualityDemand(inlet.Endpoint, demand.Resource))
            { var gaps = QualityGaps(inlet, demand.Resource, emergencyOnly); return gaps.TryGetValue(demand.Settings.Selection, out Fix gap) ? gap : Fix.Zero; }
            return DeliveryGap(inlet, demand.Resource, emergencyOnly);
        }

        private void ReleaseHaulClaims(Job job)
        {
            IResourceTransaction transactions = _world.Services.Get<IResourceTransaction>();
            var records = new List<ResourceReservation>();
            foreach (ReservationToken token in new[] { job.OutReservation, job.InReservation, job.CargoInReservation, job.CargoOutReservation })
            {
                if (!token.IsValid || job.Purchase && token.Equals(job.OutReservation)) continue;
                ResourceClaimSnapshot snapshot = transactions.ReadClaims(job.Owner, token.Endpoint, job.ReservationOwner);
                if (snapshot.Status == ResourceTransferStatus.MissingEndpoint) continue;
                if (snapshot.Status != ResourceTransferStatus.Ready) throw new InvalidOperationException("surviving haul claim is inaccessible");
                bool found = false;
                foreach (ResourceReservation record in snapshot.Records)
                    if (record.Token.Equals(token)) { records.Add(record); found = true; }
                if (!found) throw new InvalidOperationException("surviving haul receipt disappeared");
            }
            if (records.Count != 0 && !transactions.ReleaseClaims(new ResourceClaimReleaseIntent(job.Owner, records, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed)
                throw new InvalidOperationException("whole surviving haul release changed after admission");
        }
    }
}
