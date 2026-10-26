using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Public-contract engine-side fault instrumentation, not host gameplay or another stock owner.
    internal sealed class ClaimInlet : IResourceInlet
    {
        private readonly ResourceInventoryState _stock;
        public ClaimInlet(ResourceInventoryState stock) { _stock = stock; }
        public ResourceEndpointId Endpoint => _stock.Endpoint;
        public void WriteResourceState(IStateSync writer) => _stock.WriteResourceState(writer);
        public PlayerId Company => _stock.Company;
        public int Version => _stock.Version;
        public bool Public => _stock.Public;
        public ICapacityPolicy CapacityPolicy => _stock.CapacityPolicy;
        public IReadOnlyList<ResourceAmount> InboundPromises => _stock.InboundPromises;
        public IReadOnlyList<ResourceAmount> OutboundPromises => _stock.OutboundPromises;
        public IReadOnlyList<ResourceReservation> Reservations => _stock.Reservations;
        public Fix Amount(ResourceId resource) => _stock.Amount(resource);
        public Fix ReservedIn(ResourceId resource) => _stock.ReservedIn(resource);
        public bool Accepts(ResolvedContent content, ResourceId resource) => _stock.Accepts(content, resource);
        public Fix Offer(ResourceId resource, Fix ceiling) => _stock.Offer(resource, ceiling);
        public CapacityCheck CheckIncoming(IReadOnlyList<ResourceAmount> incoming) => _stock.CheckIncoming(incoming);
        public bool CanReserveIn(ReservationOwner owner, ResourceId resource, Fix amount) => _stock.CanReserveIn(owner, resource, amount);
        public ReservationToken ReserveIn(ReservationOwner owner, ResourceId resource, Fix amount) => _stock.ReserveIn(owner, resource, amount);
        public void ReleaseIn(ReservationOwner owner, ReservationToken token) => _stock.ReleaseIn(owner, token);
        public ResourceReceiptCheck CheckReservedIn(ResolvedContent content, ReservationOwner owner, ReservationToken token)
            => _stock.CheckReservedIn(content, owner, token);
        public Fix ReceiveReserved(ResolvedContent content, ReservationOwner owner, ReservationToken token, Fix amount)
            => _stock.ReceiveReserved(content, owner, token, amount);
        public Fix Receive(ResolvedContent content, ResourceId resource, Fix amount) => _stock.Receive(content, resource, amount);
    }
    public enum ClaimFaultMode { None, MutatingCheck, ShortTake, ExtraAcquire, IgnoreRelease, EraseRemainder, OtherStock, OtherReceipt, ShortReceive, EraseInletRemainder, SilentVersion, RefusingOut, RefusingIn, OtherEndpoint, MutatingOtherEndpointCheck, MutatingInCheck, ExtraInAcquire, IgnoreInRelease, WrongInDirection, OtherEndpointAcquire, OtherEndpointReceive, OtherEndpointRelease, CapacityReadPeer, ReservationReadPeer }
    internal class ClaimFaultOutlet : IResourceOutlet
    {
        protected readonly ResourceInventoryState _stock;
        private readonly ResourceInventoryState _peer;
        private readonly ClaimFaultMode _mode;
        private readonly int _initialVersion;
        public ClaimFaultOutlet(ResourceInventoryState stock, ClaimFaultMode mode, ResourceInventoryState peer = null)
        { _stock = stock; _mode = mode; _peer = peer; _initialVersion = stock.Version; }
        public ResourceEndpointId Endpoint => _stock.Endpoint;
        public void WriteResourceState(IStateSync writer) => _stock.WriteResourceState(writer);
        public PlayerId Company => _stock.Company;
        public int Version => _mode == ClaimFaultMode.SilentVersion ? _initialVersion : _stock.Version;
        public bool Public => _stock.Public;
        public bool Evacuate => _stock.Evacuate;
        public ICapacityPolicy CapacityPolicy => _stock.CapacityPolicy;
        public IReadOnlyList<ResourceAmount> InboundPromises => _stock.InboundPromises;
        public IReadOnlyList<ResourceAmount> OutboundPromises => _stock.OutboundPromises;
        public IReadOnlyList<ResourceReservation> Reservations
        { get { if (_mode == ClaimFaultMode.ReservationReadPeer) MutatePeer(); return _stock.Reservations; } }
        public CapacityCheck CheckIncoming(IReadOnlyList<ResourceAmount> incoming)
        { if (_mode == ClaimFaultMode.CapacityReadPeer) MutatePeer(); return _stock.CheckIncoming(incoming); }
        public Fix Offer(ResourceId resource, Fix ceiling) => _stock.Offer(resource, ceiling);
        public Fix Amount(ResourceId resource) => _stock.Amount(resource);
        public Fix Available(ResourceId resource) => _stock.Available(resource);
        public bool Accepts(ResolvedContent content, ResourceId resource) => _stock.Accepts(content, resource);
        public Fix Receive(ResolvedContent content, ResourceId resource, Fix amount) => _stock.Receive(content, resource, amount);
        public Fix Take(ResourceId resource, Fix amount) => _stock.Take(resource, amount);
        public Fix ReservedIn(ResourceId resource) => _stock.ReservedIn(resource);
        public Fix ReservedOut(ResourceId resource) => _stock.ReservedOut(resource);
        public bool CanReserveIn(ReservationOwner owner, ResourceId resource, Fix amount)
        {
            if (_mode == ClaimFaultMode.MutatingInCheck)
            { _stock.ReserveIn(new ReservationOwner("proof.fault", 3), resource, Fix.One); return false; }
            return _stock.CanReserveIn(owner, resource, amount);
        }
        public ReservationToken ReserveIn(ReservationOwner owner, ResourceId resource, Fix amount)
        {
            if (_mode == ClaimFaultMode.WrongInDirection) return _stock.ReserveOut(owner, resource, amount);
            ReservationToken token = _stock.ReserveIn(owner, resource, amount);
            if (_mode == ClaimFaultMode.ExtraInAcquire) _stock.ReserveIn(new ReservationOwner("proof.fault", 4), resource, Fix.One);
            return token;
        }
        public void ReleaseIn(ReservationOwner owner, ReservationToken token)
        { if (_mode != ClaimFaultMode.IgnoreInRelease) _stock.ReleaseIn(owner, token); }
        public ResourceReceiptCheck CheckReservedIn(ResolvedContent content, ReservationOwner owner, ReservationToken token)
            => _mode == ClaimFaultMode.RefusingIn ? new ResourceReceiptCheck(ResourceTransferStatus.NoCapacity)
                : _stock.CheckReservedIn(content, owner, token);
        public Fix ReceiveReserved(ResolvedContent content, ReservationOwner owner, ReservationToken token, Fix amount)
        {
            Fix actual = _stock.ReceiveReserved(content, owner, token, amount);
            if (_mode == ClaimFaultMode.OtherEndpointReceive) MutatePeer();
            if (_mode == ClaimFaultMode.EraseInletRemainder) _stock.ReleaseIn(owner, token);
            return _mode == ClaimFaultMode.ShortReceive ? actual - Fix.One : actual;
        }
        public bool CanReserveOut(ReservationOwner owner, ResourceId resource, Fix amount)
        {
            if (_mode == ClaimFaultMode.MutatingCheck)
            {
                _stock.ReserveOut(new ReservationOwner("proof.fault", 1), resource, Fix.One);
                return false; // Even a refusing pure callback must fault when it mutates.
            }
            return _stock.CanReserveOut(owner, resource, amount);
        }
        public virtual ReservationToken ReserveOut(ReservationOwner owner, ResourceId resource, Fix amount)
        {
            ReservationToken token = _stock.ReserveOut(owner, resource, amount);
            if (_mode == ClaimFaultMode.OtherEndpointAcquire) MutatePeer();
            if (_mode == ClaimFaultMode.ExtraAcquire) _stock.ReserveIn(new ReservationOwner("proof.fault", 2), resource, Fix.One);
            return token;
        }
        public void ReleaseOut(ReservationOwner owner, ReservationToken token)
        { if (_mode != ClaimFaultMode.IgnoreRelease) _stock.ReleaseOut(owner, token); if (_mode == ClaimFaultMode.OtherEndpointRelease) MutatePeer(); }
        public void IncreaseReservedOut(ReservationOwner owner, ReservationToken token, Fix amount) => _stock.IncreaseReservedOut(owner, token, amount);
        public ResourceReceiptCheck CheckReservedOut(ReservationOwner owner, ReservationToken token)
        {
            if (_mode == ClaimFaultMode.MutatingCheck) _stock.Take(_stock.Reservations[0].Resource, Fix.One);
            if (_mode == ClaimFaultMode.MutatingOtherEndpointCheck)
            { MutatePeer(); return new ResourceReceiptCheck(ResourceTransferStatus.NoStock); }
            if (_mode == ClaimFaultMode.RefusingOut) return new ResourceReceiptCheck(ResourceTransferStatus.NoStock);
            return _stock.CheckReservedOut(owner, token);
        }
        public Fix TakeReserved(ReservationOwner owner, ReservationToken token, Fix amount)
        {
            ResourceId resource = _stock.CheckReservedOut(owner, token).Record.Resource;
            Fix actual = _stock.TakeReserved(owner, token, amount);
            if (_mode == ClaimFaultMode.OtherEndpoint) MutatePeer();
            if (_mode == ClaimFaultMode.EraseRemainder) _stock.ReleaseOut(owner, token);
            if (_mode == ClaimFaultMode.OtherStock)
                foreach (ResourceAmount line in _stock.Stock)
                    if (line.Resource != resource) { _stock.Remove(line.Resource, Fix.One); break; }
            if (_mode == ClaimFaultMode.OtherReceipt)
                foreach (ResourceReservation record in _stock.Reservations)
                    if (!record.Token.Equals(token))
                    {
                        if (record.Direction == ReservationDirection.Outbound) _stock.ReleaseOut(record.Owner, record.Token);
                        else _stock.ReleaseIn(record.Owner, record.Token);
                        break;
                    }
            return _mode == ClaimFaultMode.ShortTake ? actual - Fix.One : actual;
        }
        private void MutatePeer()
        {
            if (_peer == null) throw new InvalidOperationException("peer fault requires an actual endpoint");
            _peer.Take(_peer.Stock[0].Resource, Fix.One);
        }
    }

    // Distinct capability shape; all primitive stock/receipt delegation remains in the original base.
    internal sealed class CoupledClaimOutlet : ClaimFaultOutlet, IResourceConversionEndpoint
    {
        private readonly EndpointPools _provider;
        public CoupledClaimOutlet(ResourceInventoryState stock, EndpointPools provider)
            : base(stock, provider.ClaimFault) { _provider = provider; }
        public override ReservationToken ReserveOut(ReservationOwner owner, ResourceId resource, Fix amount)
        {
            ReservationToken token = base.ReserveOut(owner, resource, amount);
            _provider.AfterReserveOut?.Invoke(_stock, owner, resource, amount, token);
            return token;
        }
        public CapacityCheck CheckConversion(IReadOnlyList<ResourceAmount> consumed, IReadOnlyList<ResourceAmount> produced)
            => _provider.ConversionCheck == null ? _stock.CheckConversion(consumed, produced) : _provider.ConversionCheck(_stock, consumed, produced);
        public void ConvertReserved(ReservationOwner owner, IReadOnlyList<ResourceConversionInput> inputs, Bill produced, IReadOnlyList<ResourceLotAmount> lots = null)
        {
            if (_provider.ConversionCommit == null) _stock.ConvertReserved(owner, inputs, produced, lots);
            else _provider.ConversionCommit(_stock, owner, inputs, produced);
        }
    }

    public sealed class ConversionProbeCommand : Command
    {
        public ResourceConversionIntent Intent;
        public string Result;
        public EntityId Source;
        public override void Sync(IStateSync sync)
        {
            sync.String(ref Result); sync.Entity(ref Source);
            string scope = Intent.Owner.Scope, cause = Intent.Cause.Id; long identity = Intent.Owner.Identity;
            PlayerId company = Intent.Company;
            sync.String(ref scope); sync.Long(ref identity); sync.Player(ref company); sync.String(ref cause);
            ResourceConversionInput[] inputs = SyncInputs(sync, Intent.Inputs);
            bool present = Intent.Outputs != null; sync.Bool(ref present);
            int count = sync.Count(Intent.Outputs?.Count ?? 0); var outputs = new ResourceProductionIntent[count];
            for (int i = 0; i < count; i++)
            {
                ResourceProductionIntent output = sync.Reading ? default : Intent.Outputs[i]; ResourceEndpointId endpoint = output.Endpoint;
                ResourceEndpointId.Sync(sync, ref endpoint); bool hasBill = output.Quantities != null; sync.Bool(ref hasBill);
                int lines = sync.Count(output.Quantities?.Count ?? 0); var bill = new List<ResourceAmount>();
                for (int j = 0; j < lines; j++)
                {
                    ResourceId resource = sync.Reading ? default : output.Quantities[j].Resource; Fix amount = sync.Reading ? default : output.Quantities[j].Amount;
                    sync.Resource(ref resource); sync.Fix(ref amount); bill.Add(new ResourceAmount(resource, amount));
                }
                if (sync.Reading) outputs[i] = new ResourceProductionIntent(endpoint, hasBill ? new Bill(bill) : null);
            }
            if (sync.Reading) Intent = new ResourceConversionIntent(new ReservationOwner(scope, identity), company,
                inputs, present ? outputs : null, new Code(cause));
        }
        private static ResourceConversionInput[] SyncInputs(IStateSync sync, IReadOnlyList<ResourceConversionInput> inputs)
        {
            bool present = inputs != null; sync.Bool(ref present); int count = sync.Count(inputs?.Count ?? 0);
            var copy = new ResourceConversionInput[count];
            for (int i = 0; i < count; i++)
            {
                ResourceConversionInput input = sync.Reading ? default : inputs[i];
                ResourceReservation record = input.Receipt; ReservationToken token = record.Token;
                string scope = record.Owner.Scope; long identity = record.Owner.Identity; ResourceId resource = record.Resource;
                Fix amount = record.Amount; byte direction = (byte)record.Direction;
                ReservationToken.Sync(sync, ref token); sync.String(ref scope); sync.Long(ref identity);
                sync.Resource(ref resource); sync.Fix(ref amount); sync.Byte(ref direction);
                Fix consumed = input.Consumed; bool release = input.ReleaseRemainder; byte kind = (byte)input.Kind;
                sync.Fix(ref consumed); sync.Bool(ref release); sync.Byte(ref kind);
                if (sync.Reading) copy[i] = new ResourceConversionInput((ResourceFlowKind)kind,
                    new ResourceReservation(token, new ReservationOwner(scope, identity), resource, amount, (ReservationDirection)direction), consumed, release);
            }
            return present ? copy : null;
        }
    }
    internal sealed class ConversionProbeHandler : CommandHandler<ConversionProbeCommand>
    {
        protected override Code Check(World world, ConversionProbeCommand command)
        {
            if (command.Intent.Company != command.Player) return Codes.NotOwned;
            Code refusal = Prepare(world, command, out IEntityCreation creation);
            if (!refusal.IsNone) return refusal;
            var result = world.Services.Get<IResourceTransaction>().EvaluateConversion(command.Intent, creation);
            return result.Ready ? Code.None : result.Blocker.IsNone ? Codes.Blocked : result.Blocker;
        }
        private static Code Prepare(World world, ConversionProbeCommand command, out IEntityCreation creation)
        {
            creation = null;
            return command.Result == null ? Code.None : world.PrepareCreation(world.Content.Find<Beep.SimEngine.Content.EntityDef>(command.Result),
                command.Player, command.Source, null, out creation);
        }
        protected override void Apply(World world, ConversionProbeCommand command, CommandContext context)
        {
            Code refusal = Prepare(world, command, out IEntityCreation creation);
            if (!refusal.IsNone || !world.Services.Get<IResourceTransaction>().CommitConversion(command.Intent, creation).Committed)
                throw new InvalidOperationException("conversion changed after current-state admission");
        }
    }

    // Commands exercise the normal authoritative Update/fault boundary using the actual engine service.
    public sealed class ClaimProbeCommand : Command
    {
        public bool Consume, Release;
        public ResourceClaimIntent[] Intents = Array.Empty<ResourceClaimIntent>();
        public ResourceReservation[] Receipts = Array.Empty<ResourceReservation>();
        public ResourceReservation[] Participants = Array.Empty<ResourceReservation>();
        public ResourceEndpointId[] ReleaseEndpoints = Array.Empty<ResourceEndpointId>();
        public ResourceClaimReleasePolicy ReleasePolicy = ResourceClaimReleasePolicy.Exact;
        public Code Cause = new Code("proof.claim-use");
        public override void Sync(IStateSync sync)
        {
            sync.Bool(ref Consume); sync.Bool(ref Release);
            string cause = Cause.Id; sync.String(ref cause); if (sync.Reading) Cause = new Code(cause);
            int count = sync.Count(Intents.Length); if (sync.Reading) Intents = new ResourceClaimIntent[count];
            for (int i = 0; i < count; i++)
            {
                ResourceClaimIntent intent = Intents[i]; ResourceEndpointId endpoint = intent.Endpoint;
                string scope = intent.Owner.Scope; long identity = intent.Owner.Identity;
                byte kind = (byte)intent.Kind; sync.Byte(ref kind);
                ResourceReservation original = intent.OriginalReceipt;
                if ((ResourceClaimKind)kind == ResourceClaimKind.Increase) SyncReceipt(sync, ref original);
                else if ((ResourceClaimKind)kind != ResourceClaimKind.Fresh) throw new CorruptStateException("invalid resource claim kind");
                ResourceEndpointId.Sync(sync, ref endpoint); sync.String(ref scope); sync.Long(ref identity);
                byte claimDirection = (byte)intent.Direction; sync.Byte(ref claimDirection);
                bool present = intent.Quantities != null; sync.Bool(ref present);
                int lines = sync.Count(intent.Quantities?.Count ?? 0); var bill = new List<ResourceAmount>();
                for (int j = 0; j < lines; j++)
                {
                    ResourceId resource = sync.Reading ? default : intent.Quantities[j].Resource;
                    Fix amount = sync.Reading ? default : intent.Quantities[j].Amount;
                    sync.Resource(ref resource); sync.Fix(ref amount); bill.Add(new ResourceAmount(resource, amount));
                }
                if (sync.Reading)
                {
                    Bill quantities = present ? new Bill(bill) : null;
                    Intents[i] = (ResourceClaimKind)kind == ResourceClaimKind.Increase
                        ? new ResourceClaimIntent(original, quantities)
                        : new ResourceClaimIntent((ReservationDirection)claimDirection, new ReservationOwner(scope, identity), endpoint, quantities);
                    if (Intents[i].Endpoint != endpoint || !Intents[i].Owner.Equals(new ReservationOwner(scope, identity)) ||
                        Intents[i].Direction != (ReservationDirection)claimDirection)
                        throw new CorruptStateException("claim intent differs from original receipt");
                }
            }
            count = sync.Count(Receipts.Length); if (sync.Reading) Receipts = new ResourceReservation[count];
            for (int i = 0; i < count; i++)
            {
                ResourceReservation record = Receipts[i]; SyncReceipt(sync, ref record);
                if (sync.Reading) Receipts[i] = record;
            }
            bool presentParticipants = Participants != null; sync.Bool(ref presentParticipants);
            count = sync.Count(Participants?.Length ?? 0); if (sync.Reading) Participants = presentParticipants ? new ResourceReservation[count] : null;
            for (int i = 0; i < count; i++)
            {
                ResourceReservation record = sync.Reading ? default : Participants[i]; SyncReceipt(sync, ref record);
                if (sync.Reading) Participants[i] = record;
            }
            bool hasEndpoints = ReleaseEndpoints != null; sync.Bool(ref hasEndpoints);
            count = sync.Count(ReleaseEndpoints?.Length ?? 0); if (sync.Reading) ReleaseEndpoints = hasEndpoints ? new ResourceEndpointId[count] : null;
            for (int i = 0; i < count; i++)
            {
                var identity = sync.Reading ? default : ReleaseEndpoints[i]; ResourceEndpointId.Sync(sync, ref identity);
                if (sync.Reading) ReleaseEndpoints[i] = identity;
            }
            byte releasePolicy = (byte)ReleasePolicy; sync.Byte(ref releasePolicy); if (sync.Reading) ReleasePolicy = (ResourceClaimReleasePolicy)releasePolicy;
        }
        private static void SyncReceipt(IStateSync sync, ref ResourceReservation record)
        {
            ReservationToken token = record.Token; string scope = record.Owner.Scope; long identity = record.Owner.Identity;
            ResourceId resource = record.Resource; Fix amount = record.Amount; byte direction = (byte)record.Direction;
            ReservationToken.Sync(sync, ref token); sync.String(ref scope); sync.Long(ref identity);
            sync.Resource(ref resource); sync.Fix(ref amount); sync.Byte(ref direction);
            if (sync.Reading) record = new ResourceReservation(token, new ReservationOwner(scope, identity), resource, amount, (ReservationDirection)direction);
        }
    }
    internal sealed class ClaimProbeHandler : CommandHandler<ClaimProbeCommand>
    {
        protected override Code Check(World world, ClaimProbeCommand command)
        {
            if (command.Consume)
                foreach (ResourceReservation receipt in command.Receipts)
                    if (receipt.Direction != ReservationDirection.Outbound) return Codes.InvalidArgument;
            IResourceTransaction transaction = world.Services.Get<IResourceTransaction>();
            bool ready = command.Consume ? transaction.EvaluateClaimConsumption(command.Player, command.Receipts, command.Cause, command.Participants).Ready
                : command.Release ? transaction.EvaluateClaimRelease(new ResourceClaimReleaseIntent(command.Player, command.Receipts, command.Participants, command.ReleaseEndpoints, command.ReleasePolicy)).Ready
                : transaction.EvaluateClaims(command.Player, command.Intents, command.Participants).Ready;
            return ready ? Code.None : Codes.Blocked;
        }
        protected override void Apply(World world, ClaimProbeCommand command, CommandContext context)
        {
            IResourceTransaction transaction = world.Services.Get<IResourceTransaction>();
            ResourceClaimResult result = command.Consume ? transaction.ConsumeClaims(command.Player, command.Receipts, command.Cause, command.Participants)
                : command.Release ? transaction.ReleaseClaims(new ResourceClaimReleaseIntent(command.Player, command.Receipts, command.Participants, command.ReleaseEndpoints, command.ReleasePolicy)) : transaction.AcquireClaims(command.Player, command.Intents, command.Participants);
            if (!result.Committed) throw new InvalidOperationException("claim probe unexpectedly refused after current-state admission: " + result.Status);
            context.Arg0 = result.Receipts.Count;
        }
    }

    // Calls the single engine transaction protocol through the ordinary command/Update boundary.
    public sealed class TransferProbeCommand : Command
    {
        public bool Move;
        // Trusted engine-module protocol instrumentation only, not an ordinary player transfer capability.
        public bool SystemEnvelope;
        public ResourceMovementIntent[] Movements = Array.Empty<ResourceMovementIntent>();
        public ResourceTransferIntent[] Intents = Array.Empty<ResourceTransferIntent>();
        public override void Sync(IStateSync sync)
        {
            sync.Bool(ref Move);
            sync.Bool(ref SystemEnvelope);
            if (Move)
            {
                bool present = Movements != null; sync.Bool(ref present);
                int movements = sync.Count(Movements?.Length ?? 0);
                if (sync.Reading) Movements = present ? new ResourceMovementIntent[movements] : null;
                for (int i = 0; i < movements; i++)
                {
                    ResourceMovementIntent movement = sync.Reading ? default : Movements[i];
                    SyncMovement(sync, ref movement);
                    if (sync.Reading) Movements[i] = movement;
                }
                return;
            }
            int count = sync.Count(Intents.Length);
            if (sync.Reading) Intents = new ResourceTransferIntent[count];
            for (int i = 0; i < count; i++)
            {
                ResourceTransferIntent intent = Intents[i];
                string scope = intent.Owner.Scope; long identity = intent.Owner.Identity; byte kind = (byte)intent.Kind;
                ReservationToken source = intent.Source, destination = intent.Destination;
                ResourceId resource = intent.Resource; Fix amount = intent.Amount; PlayerId company = intent.Company;
                sync.String(ref scope); sync.Long(ref identity); sync.Player(ref company);
                ReservationToken.Sync(sync, ref source); ReservationToken.Sync(sync, ref destination);
                sync.Resource(ref resource); sync.Fix(ref amount); sync.Byte(ref kind);
                if (sync.Reading) Intents[i] = new ResourceTransferIntent(new ReservationOwner(scope, identity), company,
                    source, destination, resource, amount, (ResourceTransferKind)kind);
            }
        }
        private static void SyncMovement(IStateSync sync, ref ResourceMovementIntent movement)
        {
            string scope = movement.Owner.Scope; long identity = movement.Owner.Identity; PlayerId company = movement.Company;
            ResourceEndpointId source = movement.Source, destination = movement.Destination;
            sync.String(ref scope); sync.Long(ref identity); sync.Player(ref company);
            ResourceEndpointId.Sync(sync, ref source); ResourceEndpointId.Sync(sync, ref destination);
            bool hasQuantities = movement.Quantities != null; sync.Bool(ref hasQuantities);
            int count = sync.Count(movement.Quantities?.Count ?? 0); var bill = new List<ResourceAmount>();
            for (int i = 0; i < count; i++)
            {
                ResourceId resource = sync.Reading ? default : movement.Quantities[i].Resource;
                Fix amount = sync.Reading ? default : movement.Quantities[i].Amount;
                sync.Resource(ref resource); sync.Fix(ref amount); bill.Add(new ResourceAmount(resource, amount));
            }
            var claims = SyncMovementClaims(sync, movement.SourceClaims);
            var destinationClaims = SyncMovementClaims(sync, movement.DestinationClaims);
            var participantClaims = SyncMovementClaims(sync, movement.ParticipantClaims);
            byte participantDisposition = (byte)movement.ParticipantDisposition; sync.Byte(ref participantDisposition);
            if (sync.Reading) movement = new ResourceMovementIntent(new ReservationOwner(scope, identity), company, source, destination,
                hasQuantities ? new Bill(bill) : null, claims, destinationClaims, participantClaims, (ResourceMovementParticipantDisposition)participantDisposition);
        }
        private static IReadOnlyList<ResourceReservation> SyncMovementClaims(IStateSync sync, IReadOnlyList<ResourceReservation> current)
        {
            bool present = current != null; sync.Bool(ref present);
            int count = sync.Count(current?.Count ?? 0); var claims = new List<ResourceReservation>();
            for (int i = 0; i < count; i++)
            {
                ResourceReservation record = sync.Reading ? default : current[i];
                ReservationToken token = record.Token; string ownerScope = record.Owner.Scope; long ownerId = record.Owner.Identity;
                ResourceId resource = record.Resource; Fix amount = record.Amount; byte direction = (byte)record.Direction;
                ReservationToken.Sync(sync, ref token); sync.String(ref ownerScope); sync.Long(ref ownerId);
                sync.Resource(ref resource); sync.Fix(ref amount); sync.Byte(ref direction);
                if (sync.Reading) claims.Add(new ResourceReservation(token, new ReservationOwner(ownerScope, ownerId), resource, amount, (ReservationDirection)direction));
            }
            return sync.Reading ? present ? claims.AsReadOnly() : null : current;
        }
    }
    internal sealed class TransferProbeHandler : CommandHandler<TransferProbeCommand>
    {
        protected override Code Check(World world, TransferProbeCommand command)
        {
            if (command.Move)
            {
                if (command.Movements == null) return Codes.InvalidArgument;
                foreach (ResourceMovementIntent movement in command.Movements)
                    if (!command.SystemEnvelope && movement.Company != command.Player) return Codes.NotOwned;
                return world.Services.Get<IResourceTransaction>().EvaluateMovements(command.Movements).Ready ? Code.None : Codes.Blocked;
            }
            foreach (ResourceTransferIntent intent in command.Intents)
                if (!command.SystemEnvelope && intent.Company != command.Player) return Codes.NotOwned;
            return world.Services.Get<IResourceTransaction>().EvaluateTransfers(command.Intents).Ready ? Code.None : Codes.Blocked;
        }
        protected override void Apply(World world, TransferProbeCommand command, CommandContext context)
        {
            IResourceTransaction transaction = world.Services.Get<IResourceTransaction>();
            if (!(command.Move ? transaction.CommitMovements(command.Movements) : transaction.CommitTransfers(command.Intents)).Committed)
                throw new InvalidOperationException("native transfer refused after admission");
        }
    }
}
