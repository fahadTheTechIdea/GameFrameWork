using System;
using System.Collections.Generic;
using System.Globalization;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Provider state stays in the same Transformer/Sync; this is a physical buffer, not a balance copy.
    public sealed partial class Transformer
    {
        private ResourceInventoryState _wip;
        private int _wipVersion;
        private long _execution, _nextExecution = 1;
        private readonly List<TransformationPortLink> _inputSources = new List<TransformationPortLink>();
        private readonly List<TransformationPortLink> _toolSources = new List<TransformationPortLink>();
        private ResourceInventoryState Wip => _wip ?? (_wip = new ResourceInventoryState(TransformerManager.WipEndpoint(this),
            () => Entity.Owner, () => _wipVersion, () => _wipVersion = checked(_wipVersion + 1), Def.Content,
            () => Def.WipCapacity, AcceptsWip, false, false));
        public IResourceOutlet WorkInProgress => Wip;
        public ReservationOwner WorkInProgressOwner => _execution == 0 ? default : OwnerFor(_execution);
        private ReservationOwner OwnerFor(long execution)
            => new ReservationOwner(TransformerManager.WipOwnerScope(Wip.Endpoint), execution);
        private bool AcceptsWip(ResourceId resource)
        {
            foreach (ITransformationDefinition definition in Def.Transformations)
                if (definition.Inputs.AmountOf(resource).IsPositive) return true;
            return false;
        }
        private void SyncWip(IStateSync sync)
        {
            sync.Int(ref _wipVersion); sync.Long(ref _execution); sync.Long(ref _nextExecution);
            if (_wipVersion < 0 || _execution < 0 || _nextExecution <= 0 || _execution >= _nextExecution)
                throw new CorruptStateException("invalid WIP version/execution sequence");
            Wip.Sync(sync);
            SyncPortSources(sync, _inputSources);
            SyncPortSources(sync, _toolSources);
        }
        private void SyncPortSources(IStateSync sync, List<TransformationPortLink> sources)
        {
            int count = sync.Count(sources.Count);
            if (sync.Reading) sources.Clear();
            ResourceId previous = default;
            for (int i = 0; i < count; i++)
            {
                var source = sync.Reading ? default : sources[i];
                ResourceId resource = source.Resource; string port = source.Port.Id; ResourceEndpointId endpoint = source.Endpoint;
                sync.Resource(ref resource); sync.String(ref port); ResourceEndpointId.Sync(sync, ref endpoint);
                if (_execution == 0 || !resource.IsValid || !endpoint.IsValid || endpoint == Wip.Endpoint ||
                    i > 0 && resource.CompareTo(previous) <= 0) throw new CorruptStateException("invalid original WIP source identity/order");
                try { ContractIdentity.ValidateId(port, "WIP input port"); }
                catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
                if (sync.Reading) sources.Add(new TransformationPortLink(resource, new Code(port), endpoint));
                previous = resource;
            }
        }
        public Code CheckRemoval(World world)
            => TransformationContracts.CheckRemoval(world, this);

        private Code StartWip(World world, ITransformationDefinition definition)
        {
            ReservationOwner owner = OwnerFor(Queue[0].Execution);
            IResourceTransaction transactions = world.Services.Get<IResourceTransaction>();
            var ports = TransformationContracts.InputBindings(world, Entity, definition);
            var tools = TransformationContracts.ToolBindings(world, Entity, definition);
            Code unavailable=ResourcePortContracts.Blocker(ports,tools);if(!unavailable.IsNone)return unavailable;
            var startup = new List<ResourceClaimIntent>(ResourcePortContracts.OutboundClaims(owner,
                ResourcePortContracts.QuantitiesByEndpoint(ports, definition.Inputs),
                ResourcePortContracts.QuantitiesByEndpoint(tools, definition.Tools)));
            if (!definition.Inputs.IsEmpty)
                startup.Add(new ResourceClaimIntent(ReservationDirection.Inbound, owner, Wip.Endpoint, definition.Inputs));
            if (startup.Count != 0)
            {
                var check = transactions.EvaluateClaims(Entity.Owner, startup, System.Array.Empty<ResourceReservation>());
                if (!check.Ready) return check.Status == ResourceTransferStatus.NoCapacity ? TransformerManager.BlockerWipFull :
                    definition.Tools.IsEmpty ? WorkSystem.BlockerMaterials : TransformerManager.BlockerTools;
            }
            var movements = new List<ResourceMovementIntent>();
            foreach (var input in ResourcePortContracts.QuantitiesByEndpoint(ports, definition.Inputs))
                movements.Add(new ResourceMovementIntent(owner, Entity.Owner, input.Endpoint, Wip.Endpoint, input.Quantities, Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain));
            if (movements.Count != 0)
            {
                var check = transactions.EvaluateMovements(movements);
                if (!check.Ready) return check.Status == ResourceTransferStatus.NoCapacity ? TransformerManager.BlockerWipFull : WorkSystem.BlockerMaterials;
            }
            IReadOnlyList<ResourceReservation> acquired = Array.Empty<ResourceReservation>();
            if (startup.Count != 0)
            {
                var claims = transactions.AcquireClaims(Entity.Owner, startup, System.Array.Empty<ResourceReservation>());
                if (!claims.Committed) throw new InvalidOperationException("whole selected input/tool admission changed before acquisition");
                acquired = claims.Receipts;
            }
            if (movements.Count != 0)
            {
                var heldMovements = new List<ResourceMovementIntent>();
                foreach (var movement in movements)
                {
                    var records = new List<ResourceReservation>();
                    var incoming = new List<ResourceReservation>();
                    foreach (var record in acquired)
                        if (movement.Quantities.AmountOf(record.Resource).IsPositive)
                        {
                            if (record.Direction == ReservationDirection.Outbound && record.Token.Endpoint == movement.Source) records.Add(record);
                            if (record.Direction == ReservationDirection.Inbound && record.Token.Endpoint == Wip.Endpoint) incoming.Add(record);
                        }
                    heldMovements.Add(new ResourceMovementIntent(owner, Entity.Owner, movement.Source, Wip.Endpoint, movement.Quantities, records, incoming, acquired, ResourceMovementParticipantDisposition.Retain));
                }
                var result = transactions.CommitMovements(heldMovements);
                if (!result.Committed) throw new InvalidOperationException("complete input-port movement changed after acquisition: " + result.Status);
                var protectedInputs = transactions.AcquireClaims(Entity.Owner,
                    new[] { new ResourceClaimIntent(ReservationDirection.Outbound, owner, Wip.Endpoint, definition.Inputs) }, System.Array.Empty<ResourceReservation>());
                if (!protectedInputs.Committed) throw new InvalidOperationException("received WIP could not protect its actual input bill");
            }
            _inputSources.Clear();
            _toolSources.Clear();
            foreach (var tool in tools) _toolSources.Add(new TransformationPortLink(tool.Route.Resource, tool.Route.Port, tool.Endpoint));
            if (definition.Mode == TransformationMode.Batch)
                foreach (var port in ports) _inputSources.Add(new TransformationPortLink(port.Route.Resource, port.Route.Port, port.Endpoint));
            _execution = Queue[0].Execution;
            return Code.None;
        }
        private void ReturnWip(World world)
        {
            ITransformationDefinition definition = world.Content.Get<ITransformationDefinition>(Queue[0].Transformation);
            foreach(var source in _inputSources) {
                var binding=ResourcePortContracts.Bind(world,Entity,source.Resource,Entity.Get<IResourcePort>(source.Port),source.Endpoint);
                if(!binding.Ready){Blocker=binding.Blocker;return;}
            }
            if (!definition.Inputs.IsEmpty)
            {
                var grouped = new Dictionary<ResourceEndpointId, List<ResourceAmount>>();
                foreach (var source in _inputSources)
                {
                    if (!grouped.TryGetValue(source.Endpoint, out var amounts)) grouped.Add(source.Endpoint, amounts = new List<ResourceAmount>());
                    amounts.Add(new ResourceAmount(source.Resource, definition.Inputs.AmountOf(source.Resource)));
                }
                var movements = new List<ResourceMovementIntent>();
                var participants = ToolClaims(world);
                foreach (var group in grouped)
                {
                    Bill quantities = new Bill(group.Value); var receipts = new List<ResourceReservation>();
                    foreach (var record in Wip.Reservations) if (quantities.AmountOf(record.Resource).IsPositive) receipts.Add(record);
                    movements.Add(new ResourceMovementIntent(WorkInProgressOwner, Entity.Owner, Wip.Endpoint, group.Key, quantities, receipts, Array.Empty<ResourceReservation>(), participants, ResourceMovementParticipantDisposition.Release));
                }
                ResourceTransferBatchResult result = world.Services.Get<IResourceTransaction>().CommitMovements(movements);
                if (!result.Committed)
                {
                    if (result.Status == ResourceTransferStatus.NoCapacity) { Blocker = TransformerManager.BlockerReturnFull; return; }
                    if (result.Status == ResourceTransferStatus.Incompatible) { Blocker = TransformerManager.BlockerReturnCompatibility; return; }
                    if (result.Status == ResourceTransferStatus.MissingEndpoint) { Blocker = TransformerManager.BlockerReturnMissing; return; }
                    if (result.Status == ResourceTransferStatus.InvalidRequest) { Blocker = TransformerManager.BlockerReturnAccess; return; }
                    throw new InvalidOperationException("cancellation lost its actual protected WIP: " + result.Status);
                }
            }
            else ReleaseTools(world);
            Queue.RemoveAt(0); _execution = 0; _inputSources.Clear(); _toolSources.Clear(); Progress = WorkProgress.Zero; _runningTicks = 0; Blocker = Code.None;
            Phase = Queue.Count == 0 ? TransformationPhase.Idle : TransformationPhase.Supplying;
        }
    }

    // Endpoint resolution/checkpoint inspection are aliases of existing entity/provider ownership.
    public sealed partial class TransformerManager
    {
        public const string WipScope = TransformationModule.ModuleId + ".wip";
        public string EndpointScope => WipScope;
        public static readonly Code BlockerActive = new Code("blocker.active-transformation");
        public static readonly Code BlockerReturning = new Code("blocker.transformation-returning");
        public static readonly Code BlockerReturnFull = new Code("blocker.transformation-return-full");
        public static readonly Code BlockerReturnCompatibility = new Code("blocker.transformation-return-incompatible");
        public static readonly Code BlockerWipFull = new Code("blocker.transformation-wip-full");
        public static readonly Code BlockerIdentityRange = new Code("blocker.transformation-identity-range");
        public static readonly Code BlockerTools = new Code("blocker.transformation-tools");
        public static readonly Code BlockerInputCompatibility = new Code("blocker.transformation-input-incompatible");
        public static readonly Code BlockerInputEndpoint = new Code("blocker.transformation-input-endpoint");

        public static readonly Code BlockerReturnMissing = new Code("blocker.transformation-return-missing");
        public static readonly Code BlockerReturnAccess = new Code("blocker.transformation-return-access");

        public static ResourceEndpointId WipEndpoint(ITransformerWip provider)
        {
            if (provider?.Entity == null || !provider.Entity.Id.IsValid) throw new InvalidOperationException("WIP requires an actual entity capability");
            for (int i = 0; i < provider.Entity.Components.Count; i++)
                if (ReferenceEquals(provider.Entity.Components[i], provider))
                    return new ResourceEndpointId(WipScope, checked(((long)provider.Entity.Id.Value << 32) + i + 1L));
            throw new InvalidOperationException("foreign WIP capability");
        }
        public static string WipOwnerScope(ResourceEndpointId endpoint)
        {
            if (!endpoint.IsValid || endpoint.Scope != WipScope) throw new ArgumentException("invalid WIP identity");
            return WipScope + "." + endpoint.Identity.ToString(CultureInfo.InvariantCulture);
        }
        public IResourceEndpoint Resolve(ResourceEndpointId endpoint)
        {
            if (!endpoint.IsValid || endpoint.Scope != WipScope) return null;
            long entityId = endpoint.Identity >> 32, ordinal = endpoint.Identity & uint.MaxValue;
            if (entityId <= 0 || entityId > int.MaxValue || ordinal <= 0 || ordinal > int.MaxValue) return null;
            Entity entity = World.Entities.Get(new EntityId((int)entityId));
            if (entity == null || ordinal > entity.Components.Count || !(entity.Components[(int)ordinal - 1] is ITransformerWip provider)) return null;
            int version = entity.Version; IResourceOutlet stock = provider.WorkInProgress;
            if (entity.Version != version || stock == null || stock.Endpoint != endpoint || stock.Company != entity.Owner)
                throw new InvalidOperationException("WIP endpoint provider changed its actual identity during resolution");
            return stock;
        }
        public void InspectReservations(Action<ResourceEndpointId, IReadOnlyList<ResourceReservation>> inspect)
        {
            foreach (Entity entity in World.Entities.All)
                if (entity.Alive)
                    foreach (Component component in entity.Components)
                        if (component is ITransformerWip provider)
                        {
                            IResourceOutlet stock = provider.WorkInProgress;
                            inspect(stock.Endpoint, stock.Reservations);
                        }
        }
        private void ValidateWipClaims(Dictionary<ReservationOwner, (ResourceEndpointId Wip, IReadOnlyList<TransformationPortLink> Tools)> active)
        {
            Action<ResourceEndpointId, IReadOnlyList<ResourceReservation>> inspect = (endpoint, records) =>
            {
                foreach (ResourceReservation record in records)
                    if (record.Owner.Scope != null && record.Owner.Scope.StartsWith(WipScope + ".", StringComparison.Ordinal) &&
                        (!active.TryGetValue(record.Owner, out var expected) ||
                            endpoint != expected.Wip && !ToolEndpoint(expected.Tools, endpoint, record.Resource) ||
                            record.Direction != ReservationDirection.Outbound))
                        throw new CorruptStateException("orphan or foreign execution-owned WIP promise");
            };
            foreach (Entity entity in World.Entities.All)
                if (entity.Alive && entity.Get<IInletPort>() is IInletPort inlet) inspect(inlet.Endpoint, inlet.Reservations);
            foreach (IResourceEndpointProvider provider in World.Services.Providers<IResourceEndpointProvider>()) provider.InspectReservations(inspect);
        }
        private static bool ToolEndpoint(IReadOnlyList<TransformationPortLink> sources, ResourceEndpointId endpoint, ResourceId resource)
        {
            foreach (var source in sources) if (source.Endpoint == endpoint && source.Resource == resource) return true;
            return false;
        }
    }
}
