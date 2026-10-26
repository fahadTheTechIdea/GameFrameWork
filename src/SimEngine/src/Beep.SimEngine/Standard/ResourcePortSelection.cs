using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IResourcePortSelectionDefinition : IResourcePortDefinition
    {
        IReadOnlyList<ResourceEndpointId> Choices { get; }
        /// <summary>Explicit authored selection for a newly created capability, independent of choice ordering. Retained state is not reset.</summary>
        ResourceEndpointId InitialEndpoint { get; }
        /// <summary>Declared original endpoint access survives a different current selection; temporary unavailability remains explicit.</summary>
        bool RetainsOriginalAccess { get; }
    }
    /// <summary>Selected reference only; normal routing admission and commands belong to the framework.</summary>
    public interface IResourcePortSelection : IResourcePort
    {
        ResourceEndpointId SelectedEndpoint { get; }
        IResourceOutlet ResolveChoice(World world,ResourceEndpointId endpoint);
        void Select(World world,ResourceEndpointId endpoint);
    }
    public readonly struct ResourcePortChoiceSnapshot
    {
        public ResourceEndpointId Endpoint { get; }
        public bool Selected { get; }
        public Code Blocker { get; }
        public ResourcePortChoiceSnapshot(ResourceEndpointId endpoint,bool selected,Code blocker)
        { Endpoint=endpoint;Selected=selected;Blocker=blocker; }
    }
    internal static class ResourcePortSelectionContracts
    {
        internal static void ValidateDefinitions(World world)
        {
            foreach(EntityDef owner in world.Content.All<EntityDef>())
                foreach(ComponentDef part in owner.Components)
                    if(part is IResourcePortSelectionDefinition definition)ReadChoices(world,owner,definition);
        }
        internal static IReadOnlyList<ResourceEndpointId> ReadChoices(World world,EntityDef owner,IResourcePortSelectionDefinition definition)
        {
            if(definition==null||!(definition is ComponentDef part)||!world.Content.Owns(owner)||
                !ReferenceEquals(part.Owner,owner)||definition.CapabilityId.IsNone||
                !ReferenceEquals(owner.Component<IResourcePortDefinition>(definition.CapabilityId),definition)||
                !ReferenceEquals(owner.Component<IResourcePortSelectionDefinition>(definition.CapabilityId),definition)||
                !typeof(IResourcePortSelection).IsAssignableFrom(part.ComponentType))
                throw new ContentException("resource-port selection requires actual named metadata/runtime contracts");
            return EntityObservation.ReadProvider(world,definition,null,()=>{
                var choices=definition.Choices;
                if(choices==null||choices.Count==0)throw new ContentException(owner.Id+": explicit nonempty resource-port choices required");
                var result=new List<ResourceEndpointId>();var seen=new HashSet<ResourceEndpointId>();
                foreach(var endpoint in choices) {
                    if(!endpoint.IsValid||!seen.Add(endpoint))throw new ContentException(owner.Id+": invalid or duplicate resource-port choice");
                    bool declared=endpoint.Scope==ResourceEndpointId.EntityScope;
                    foreach(var provider in world.Services.Providers<IResourceEndpointProvider>())
                        if(provider.EndpointScope==endpoint.Scope&&ReferenceEquals(provider.World,world))declared=true;
                    if(!declared)throw new ContentException(owner.Id+": resource-port choice requires its actual selected endpoint provider");
                    result.Add(endpoint);
                }
                if(!definition.InitialEndpoint.IsValid||!seen.Contains(definition.InitialEndpoint))
                    throw new ContentException(owner.Id+": initial resource-port endpoint must be an explicit declared choice");
                result.Sort((a,b)=>{int scope=string.CompareOrdinal(a.Scope,b.Scope);return scope!=0?scope:a.Identity.CompareTo(b.Identity);});
                return result.AsReadOnly();
            },world.Space);
        }
        internal static void ValidateReplacement(World world,Entity source,Entity prepared)
        {
            foreach(var component in prepared.Components)
            {
                if(component is not IResourcePortSelection port)continue;
                var definition=prepared.Def.Component<IResourcePortSelectionDefinition>(port.CapabilityId);
                ReadChoices(world,prepared.Def,definition);
                var previous=source.Get<IResourcePortSelection>(port.CapabilityId) as Component;
                var expected=previous!=null&&previous.Def.Key==component.Def.Key&&Upgrades.CanRetainState(previous.Def,component.Def)
                    ?EntityObservation.ReadProvider(world,previous,source,()=>((IResourcePortSelection)previous).SelectedEndpoint,world.Space,observeTargetState:true)
                    :EntityObservation.ReadProvider(world,definition,null,()=>definition.InitialEndpoint,world.Space);
                // The outer replacement owner observes this detached candidate's actual codec.
                if(port.SelectedEndpoint!=expected)
                    throw new InvalidOperationException("prepared resource-port selection differs from authored initial or retained endpoint");
            }
        }
        private static IResourcePortSelectionDefinition Definition(World world,IResourcePortSelection port)
        {
            if(port?.Entity?.Alive!=true||!ReferenceEquals(world.Entities.Get(port.Entity.Id),port.Entity)||
                !ReferenceEquals(port.Entity.Get<IResourcePortSelection>(port.CapabilityId),port))
                throw new InvalidOperationException("resource-port selection requires its actual named capability");
            return port.Entity.Def.Component<IResourcePortSelectionDefinition>(port.CapabilityId)
                ??throw new InvalidOperationException("resource-port selection lost actual metadata");
        }
        private static Code Candidate(World world,IResourcePortSelection port,IResourcePortSelectionDefinition definition,
            ResourceEndpointId endpoint,out IResourceOutlet store)
        {
            var scopes=new List<EntityObservation.ProviderScope>();
            foreach(var provider in world.Services.Providers<IResourceEndpointProvider>())
                if(provider.EndpointScope==endpoint.Scope&&provider is IPersistentService saved)
                    scopes.Add(EntityObservation.CaptureProvider(world,provider,saved.Sync,"resource-port.choice"));
            store=EntityObservation.ReadProvider(world,port,port.Entity,()=>port.ResolveChoice(world,endpoint),world.Space,observeTargetState:true);
            foreach(var scope in scopes)scope.Verify();
            if(store==null)return Codes.NotFound;
            if(store.Company!=port.Entity.Owner)return Codes.NotOwned;
            ResourcePortContracts.ValidateStore(port.Entity,definition,store,endpoint);
            var probe=new ResourceClaimIntent(ReservationDirection.Outbound,
                new ReservationOwner("sim.inventory.port-selection",port.Entity.Id.Value),endpoint,Bill.Empty);
            var admission=world.Services.Get<IResourceTransaction>().EvaluateClaims(port.Entity.Owner,new[]{probe}, System.Array.Empty<ResourceReservation>());
            return admission.Ready?Code.None:admission.Status==ResourceTransferStatus.MissingEndpoint?Codes.NotFound:
                admission.Status==ResourceTransferStatus.InvalidRequest?Codes.NotOwned:StandardCodes.NotAccepted;
        }
        internal static Code Check(World world,IResourcePortSelection port,ResourceEndpointId endpoint)
        {
            if(port==null||!endpoint.IsValid)return Codes.InvalidArgument;
            var definition=Definition(world,port);bool selected=false;
            foreach(var choice in ReadChoices(world,port.Entity.Def,definition))if(choice==endpoint)selected=true;
            if(!selected)return Codes.InvalidArgument;
            return Candidate(world,port,definition,endpoint,out _);
        }
        internal static Code CopyChoices(World world,IResourcePortSelection port,IList<ResourcePortChoiceSnapshot> destination)
        {
            destination.Clear();if(port==null)return Codes.InvalidArgument;
            var definition=Definition(world,port);
            var values=EntityObservation.ReadProvider(world,port,port.Entity,()=>{
                var rows=new List<ResourcePortChoiceSnapshot>();var choices=ReadChoices(world,port.Entity.Def,definition);
                ResourceEndpointId selected=port.SelectedEndpoint;bool found=false;
                foreach(var endpoint in choices) {
                    if(endpoint==selected)found=true;
                    Code blocker=Candidate(world,port,definition,endpoint,out _);
                    if(blocker!=Codes.NotOwned)rows.Add(new ResourcePortChoiceSnapshot(endpoint,endpoint==selected,blocker));
                }
                if(!found)throw new InvalidOperationException("port selected an undeclared endpoint");
                return rows;
            },world.Space,observeTargetState:true);
            foreach(var value in values)destination.Add(value);return Code.None;
        }
        internal static void Select(World world,IResourcePortSelection port,ResourceEndpointId endpoint)
        {
            Code refusal=Check(world,port,endpoint);
            if(!refusal.IsNone)throw new InvalidOperationException("resource-port selection changed after admission: "+refusal);
            var definition=Definition(world,port);var stocks=new List<StockScope>();
            var scopes=new List<EntityObservation.ProviderScope>();
            foreach(var provider in world.Services.Providers<IResourceEndpointProvider>())
                if(provider is IPersistentService saved)scopes.Add(EntityObservation.CaptureProvider(world,null,saved.Sync,"resource-port.selection"));
            foreach(var choice in ReadChoices(world,port.Entity.Def,definition)) {
                Candidate(world,port,definition,choice,out var store);
                if(store!=null)stocks.Add(new StockScope(world,store));
            }
            var facts=EntityObservation.Capture(world);var spatial=EntityObservation.CaptureSpace(world,world.Space);
            Entity entity=port.Entity;PlayerId company=entity.Owner;
            try {
                if(port.SelectedEndpoint!=endpoint)port.Select(world,endpoint);
                facts.Verify();spatial.Verify();foreach(var scope in scopes)scope.Verify();foreach(var stock in stocks)stock.Verify(world);
                if(!ReferenceEquals(world.Entities.Get(entity.Id),entity)||!entity.Alive||entity.Owner!=company||port.SelectedEndpoint!=endpoint)
                    throw new InvalidOperationException("selection changed its actual entity/owner or failed to select endpoint");
                var selected=ResourcePortContracts.Store(world,entity,port,endpoint);
                // A declared selected reference can be temporarily unavailable; selection itself never grants stock access.
                facts.Verify();spatial.Verify();foreach(var scope in scopes)scope.Verify();foreach(var stock in stocks)stock.Verify(world);
            }
            catch(Exception error){world.LatchFault("resource-port.selection",error);throw;}
        }
        // Transient verification only. Same entity version may change when its selected reference changes.
        private sealed class StockScope
        {
            private readonly IResourceOutlet _store;
            private readonly ResourceEndpointId _endpoint;
            private readonly PlayerId _company;
            private readonly ICapacityPolicy _policy;
            private readonly bool _public,_evacuate;
            private readonly List<ResourceReservation> _receipts;
            private readonly List<(ResourceId Resource,Fix Amount,Fix Available,Fix In,Fix Out,bool Accepts)> _stock;
            internal StockScope(World world,IResourceOutlet store)
            {
                _store=store;_endpoint=store.Endpoint;_company=store.Company;_policy=store.CapacityPolicy;
                _public=store.Public;_evacuate=store.Evacuate;_receipts=new List<ResourceReservation>(store.Reservations);
                _stock=new List<(ResourceId,Fix,Fix,Fix,Fix,bool)>();
                foreach(var resource in ((IResourceCatalog)world.Content).Definitions)
                    _stock.Add((resource.Resource,store.Amount(resource.Resource),store.Available(resource.Resource),
                        store.ReservedIn(resource.Resource),store.ReservedOut(resource.Resource),store.Accepts(world.Content,resource.Resource)));
            }
            internal void Verify(World world)
            {
                if(_store.Endpoint!=_endpoint||_store.Company!=_company||!ReferenceEquals(_store.CapacityPolicy,_policy)||
                    _store.Public!=_public||_store.Evacuate!=_evacuate||_store.Reservations.Count!=_receipts.Count)
                    throw new InvalidOperationException("selection changed actual stock/receipt ownership");
                for(int i=0;i<_receipts.Count;i++)if(!_receipts[i].Equals(_store.Reservations[i]))
                    throw new InvalidOperationException("selection changed actual resource promises");
                foreach(var value in _stock)
                    if(_store.Amount(value.Resource)!=value.Amount||_store.Available(value.Resource)!=value.Available||
                        _store.ReservedIn(value.Resource)!=value.In||_store.ReservedOut(value.Resource)!=value.Out||
                        _store.Accepts(world.Content,value.Resource)!=value.Accepts)
                        throw new InvalidOperationException("selection changed actual quantities or compatibility");
            }
        }
    }
}

