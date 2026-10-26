using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed partial class LogisticsService
    {
        // One intent chronology. Inventories own amounts; jobs own carriers, cargo and receipts.
        private struct RecoveryIntent
        {
            internal LogisticsRecoveryKind Kind;
            internal EntityId Origin;
            internal Code SourcePort;
            internal ResourceEndpointId SourceEndpoint;
            internal ResourceId Resource;
            internal PlayerId Company;
            internal long Created, LastDispatch;
        }
        private readonly List<RecoveryIntent> _recoveries = new List<RecoveryIntent>();
        public static readonly Code EvacuationDisabled = new Code("logistics.evacuation-disabled");
        public static readonly Code CargoReturnDisabled = new Code("logistics.cargo-return-disabled");
        private static string RecoveryEntry(LogisticsRecoveryKind kind, ResourceId resource, ResourceEndpointId endpoint)
            => (kind == LogisticsRecoveryKind.Evacuation ? "evacuation." : "return.") + DemandEntry(resource, Code.None, endpoint);
        private ILogisticsRecoveryPolicy RecoveryPolicy => _world.Services.TryGet<ILogisticsRecoveryPolicy>(out var policy) ? policy : null;
        private void ValidateRecoveryPolicy()
        {
            var policy = RecoveryPolicy;
            if (policy == null) return;
            foreach (LogisticsRecoveryKind kind in new[] { LogisticsRecoveryKind.Evacuation, LogisticsRecoveryKind.CargoReturn })
                LogisticsRecoveryContracts.Read(_world, policy, kind);
        }
        private IResourceOutlet RecoveryStock(RecoveryIntent intent)
        {
            Entity origin = _world.Entities.Get(intent.Origin);
            if (origin?.Alive != true || origin.Owner != intent.Company) return null;
            if (intent.Kind == LogisticsRecoveryKind.Evacuation)
            {
                // Future evacuation uses the current authored port; acquired jobs retain their original receipts.
                var stock = PhysicalPort(origin, intent.SourcePort, intent.Resource).Store as IResourceOutlet;
                return stock?.Public == true && stock.Evacuate && stock.Endpoint == intent.SourceEndpoint ? stock : null;
            }
            if (origin.Get<ITransporter>() == null) return null;
            IInventory cargo = TransporterContracts.ReadCargo(_world, origin);
            return cargo.Endpoint == intent.SourceEndpoint ? cargo : null;
        }
        private bool ActiveRecovery(RecoveryIntent intent)
        {
            IResourceOutlet stock = RecoveryStock(intent);
            if (stock == null) return false;
            if (intent.Kind == LogisticsRecoveryKind.Evacuation) return stock.Amount(intent.Resource).IsPositive;
            var disposition = ReadCargoDisposition(intent.Origin);
            // Actual commitments keep the original age during transport; operating reserves do not become return cargo.
            return (disposition.Unassigned.AmountOf(intent.Resource) + disposition.Committed.AmountOf(intent.Resource)).IsPositive;
        }
        private void ObserveRecovery(LogisticsRecoveryKind kind, EntityId origin, ResourceId resource, PlayerId company,
            Code port, ResourceEndpointId endpoint)
        {
            // Aliases and shared physical sites never create duplicate ages or dispatches for one actual store.
            for (int i = 0; i < _recoveries.Count; i++)
                if (_recoveries[i].Kind == kind && _recoveries[i].SourceEndpoint == endpoint &&
                    _recoveries[i].Resource == resource && _recoveries[i].Company == company)
                {
                    // A current alternate public alias retains the same actual store's age, not an acquired job's binding.
                    if (!ActiveRecovery(_recoveries[i]))
                    {
                        var current = _recoveries[i]; current.Origin = origin; current.SourcePort = port;
                        _recoveries[i] = current;
                    }
                    return;
                }
            _recoveries.Add(new RecoveryIntent { Kind = kind, Origin = origin, SourcePort = port, SourceEndpoint = endpoint,
                Resource = resource, Company = company,
                Created = _world.Clock.StepIndex, LastDispatch = _world.Clock.StepIndex });
        }
        private void RefreshRecoveries()
        {
            ValidateRecoveryPolicy(); if (RecoveryPolicy == null) return;
            foreach (Entity site in _world.Entities.All)
            {
                if (!site.Alive || !_world.Players.IsSeat(site.Owner) || site.Has<ITransporter>()) continue;
                foreach (IResourceContentDefinition resource in _world.Content.All<IResourceContentDefinition>())
                    VisitPorts(site, resource.Resource, true, (name, inlet) =>
                    {
                        if (inlet is IResourceOutlet outlet && outlet.Evacuate && outlet.Amount(resource.Resource).IsPositive)
                            ObserveRecovery(LogisticsRecoveryKind.Evacuation, site.Id, resource.Resource, site.Owner, name, outlet.Endpoint);
                    });
            }
            var carriers = _world.Entities.Components<ITransporter>();
            for (int index = 0; index < carriers.Count; index++)
            {
                Entity carrier = carriers[index].Entity;
                if (!carrier.Alive || !_world.Players.IsSeat(carrier.Owner) || carrier.Get<IWorker>() == null) continue;
                foreach (ResourceAmount line in ReadCargoDisposition(carrier.Id).Unassigned.Lines)
                    ObserveRecovery(LogisticsRecoveryKind.CargoReturn, carrier.Id, line.Resource, carrier.Owner,
                        Code.None, TransporterContracts.ReadCargo(_world, carrier).Endpoint);
            }
            // Rediscover current aliases first so unchanged actual stores keep chronology across selection edits.
            _recoveries.RemoveAll(intent => !ActiveRecovery(intent));
        }
        private HaulPreview PreviewRecovery(RecoveryIntent intent, WorkCandidateMode mode, EntityId selectedWorker)
        {
            var settings = LogisticsRecoveryContracts.Read(_world, RecoveryPolicy, intent.Kind);
            if (settings == null || !settings.Enabled) return new HaulPreview(HaulStatus.PolicyBlocked,
                blocker: intent.Kind == LogisticsRecoveryKind.Evacuation ? EvacuationDisabled : CargoReturnDisabled);
            IResourceOutlet stock = RecoveryStock(intent); if (stock == null) return new HaulPreview(HaulStatus.NotFound);
            if (intent.Kind != LogisticsRecoveryKind.CargoReturn)
                return PreviewRecoveryCandidate(intent, settings, stock, null, mode, selectedWorker);
            if (!ReadCargoDisposition(intent.Origin).Unassigned.AmountOf(intent.Resource).IsPositive)
                return new HaulPreview(HaulStatus.NoSupply);
            HaulPreview Candidate(Entity carrier)
            {
                if (carrier?.Alive != true || carrier.Owner != intent.Company || carrier.Get<IWorker>() == null || carrier.Get<ITransporter>() == null ||
                    TransporterContracts.ReadCargo(_world, carrier).Endpoint != intent.SourceEndpoint)
                    return new HaulPreview(HaulStatus.NoCarrier);
                return PreviewRecoveryCandidate(intent, settings, stock, carrier, mode, carrier.Id);
            }
            if (selectedWorker.IsValid) return Candidate(_world.Entities.Get(selectedWorker));
            // The chronology names one actual buffer, not an exclusive worker. Selection
            // is derived through the same full physical preview and comparator as dispatch;
            // never mutate Origin, age, claims or last-attempt stamps while observing it.
            HaulPreview original = Candidate(_world.Entities.Get(intent.Origin)), best = original;
            List<HaulLoadSettings> loadInputs = null;
            List<(EntityId Entity, Code Source)> serviceInputs = null;
            original.CopyLoadPolicyInputs(ref loadInputs);
            original.CopyServiceInputs(ref serviceInputs);
            var carriers = _world.Entities.Components<ITransporter>();
            for (int i = 0; i < carriers.Count; i++)
            {
                Entity carrier = carriers[i].Entity;
                if (carrier.Id == intent.Origin) continue;
                HaulPreview candidate = Candidate(carrier);
                candidate.CopyLoadPolicyInputs(ref loadInputs);
                candidate.CopyServiceInputs(ref serviceInputs);
                if (candidate.Status == HaulStatus.Ready && (best.Status != HaulStatus.Ready ||
                    CompareCandidates(_world, candidate.Carrier, candidate.InterruptedJob, SelectedCost(candidate),
                        best.Carrier, best.InterruptedJob, SelectedCost(best)) < 0)) best = candidate;
            }
            return (best.Status == HaulStatus.Ready ? best : original).WithLoadPolicyInputs(loadInputs).WithServiceInputs(serviceInputs);
        }
        private HaulPreview PreviewRecoveryCandidate(RecoveryIntent intent, LogisticsRecoverySettings settings, IResourceOutlet stock,
            Entity returning, WorkCandidateMode mode, EntityId selectedWorker)
        {
            var assignment = new WorkerDispatchOffer(DispatchScope, intent.SourceEndpoint.Identity, RecoveryEntry(intent.Kind, intent.Resource, intent.SourceEndpoint), intent.Company,
                intent.Origin, EntityId.None, false, DispatchUnavailable, false, settings.Priority, intent.Created,
                _world.Clock.StepIndex, null, settings.AttemptsPerPass, 0, true);
            // The carrier drives the return, but its actual inventory owns the source endpoint.
            // Independent transporters may bind cargo on a separate entity; never substitute carrier-local stock.
            Entity source = returning == null ? _world.Entities.Get(intent.Origin) : ((IInventory)stock).Entity;
            return PreviewDestination(_world, source, intent.Resource, intent.Company, returning, assignment, mode,
                checked(_world.Clock.StepIndex - intent.LastDispatch), selectedWorker, destinationSettings: settings, sourcePort: intent.SourcePort, sourceEndpoint: intent.SourceEndpoint,
                evacuatingEndpoint: intent.Kind == LogisticsRecoveryKind.Evacuation ? intent.SourceEndpoint : (ResourceEndpointId?)null);
        }
        private void AddRecoveries(PlayerId viewer, WorkCandidateMode mode, IList<LogisticsDispatchOffer> offers,
            long? identity, string entry, EntityId worker)
        {
            var policy = RecoveryPolicy; if (policy == null) return;
            foreach (var intent in _recoveries)
            {
                if (!ActiveRecovery(intent) || viewer.IsValid && viewer != intent.Company || identity.HasValue &&
                    (identity != intent.SourceEndpoint.Identity || entry != RecoveryEntry(intent.Kind, intent.Resource, intent.SourceEndpoint))) continue;
                var settings = LogisticsRecoveryContracts.Read(_world, policy, intent.Kind);
                HaulPreview haul = PreviewRecovery(intent, mode, worker);
                EntityId recoveryOrigin = intent.Kind == LogisticsRecoveryKind.CargoReturn
                    ? intent.Origin : haul.Status == HaulStatus.Ready ? haul.Source : intent.Origin;
                Fix requested = intent.Kind == LogisticsRecoveryKind.CargoReturn
                    ? ReadCargoDisposition(intent.Origin).Unassigned.AmountOf(intent.Resource) : RecoveryStock(intent).Available(intent.Resource);
                bool emergency = haul.HaulDemand.IsValid && InspectSource(haul.HaulDemand, intent.Company, haul.Destination).Emergency;
                offers.Add(new LogisticsDispatchOffer(haul.HaulDemand, 0, intent.Company, haul.Destination, intent.Resource,
                    requested, settings.Priority, intent.Created, _world.Clock.StepIndex, haul, emergency, intent.LastDispatch,
                    intent.Kind == LogisticsRecoveryKind.Evacuation ? LogisticsDispatchKind.Evacuation : LogisticsDispatchKind.CargoReturn, recoveryOrigin,
                    destinationPort: haul.DestinationPort, destinationEndpoint: haul.DestinationEndpoint,
                    sourcePort: haul.Status == HaulStatus.Ready ? haul.SourcePort : intent.SourcePort, sourceEndpoint: intent.SourceEndpoint));
            }
        }
        private void RecordRecovery(WorkerDispatchOffer intent, ResourceId resource, LogisticsRecoveryKind kind)
        {
            for (int i = 0; i < _recoveries.Count; i++)
                if (_recoveries[i].Kind == kind && _recoveries[i].SourceEndpoint.Identity == intent.Identity && _recoveries[i].Resource == resource &&
                    RecoveryEntry(kind, resource, _recoveries[i].SourceEndpoint) == intent.Entry)
                { var current = _recoveries[i]; current.LastDispatch = _world.Clock.StepIndex; _recoveries[i] = current; return; }
            throw new InvalidOperationException("admitted recovery intent disappeared");
        }
        public HaulPreview PreviewReturnCargo(EntityId workerId, ResourceId resource)
        {
            Entity worker = _world.Entities.Get(workerId);
            if (worker == null) return new HaulPreview(HaulStatus.NotFound);
            if (worker.Get<ITransporter>() == null || _world.Content.Resource(resource) == null) return new HaulPreview(HaulStatus.InvalidRequest);
            IInventory cargo = TransporterContracts.ReadCargo(_world, worker);
            foreach (var intent in _recoveries)
                if (intent.Kind == LogisticsRecoveryKind.CargoReturn && intent.Company == worker.Owner && intent.SourceEndpoint == cargo.Endpoint &&
                    intent.Resource == resource && ActiveRecovery(intent))
                    return PreviewRecovery(intent, WorkCandidateMode.IdleOnly, workerId);
            // Observation never creates or rebases an intent. The normal assignment preparation observes residual stock.
            if (LogisticsRecoveryContracts.Read(_world, RecoveryPolicy, LogisticsRecoveryKind.CargoReturn)?.Enabled != true)
                return new HaulPreview(HaulStatus.PolicyBlocked, blocker: CargoReturnDisabled);
            return new HaulPreview(HaulStatus.NoSupply, blocker: DispatchUnavailable);
        }
        public bool TryReplanDestination(World world, Job job, Code blocker)
        {
            if (!ReferenceEquals(world, _world) || job == null || !ReferenceEquals(world.Services.Get<WorkBoard>().Job(job.Id), job))
                throw new ArgumentException("replanning requires the actual session haul");
            using var operation = BeginHaulOperation();
            try
            {
                int delivery = job.From.IsValid ? 2 : 0;
                bool recovering = !job.RecoveryCause.IsNone;
                bool destinationMissing = world.Entities.Get(job.To) == null;
                Code binding = CheckDestinationBinding(job);
                // Loss needs its own explicitly authored permission, even during an earlier dock recovery.
                if (!binding.IsNone) blocker = binding;
                else if (recovering) blocker = job.RecoveryCause;
                var settings = job.RecoveryKind.HasValue ? LogisticsRecoveryContracts.Read(_world, RecoveryPolicy, job.RecoveryKind.Value) : null;
                if (settings?.Enabled != true || blocker.IsNone || !System.Linq.Enumerable.Contains(settings.RerouteBlockers, blocker) ||
                    !job.IsLive || !job.IsHaul || (job.ToilIndex != delivery && job.ToilIndex != delivery + 1 &&
                        !(recovering && job.ToilIndex == job.Toils.Count - 1)) || !job.Loaded.IsPositive ||
                    job.OutReservation.IsValid || job.CargoInReservation.IsValid) return false;
                Entity worker = world.Entities.Get(job.Worker);
                if (worker?.Get<IWorker>()?.Job != job.Id) return false;
                WorkBoard board = world.Services.Get<WorkBoard>();
                bool docked = world.Entities.Get(job.To)?.Get<IWorkDock>() != null;
                // A removed dock has no remaining geometry to traverse. Continue from the actual carrier,
                // never teleport to its old exit or fabricate a completed departure.
                if (recovering && !destinationMissing && !board.RecoveryDepartureReached(job, worker)) return false;
                bool leaveDock = !recovering && docked &&
                    (!job.HandlingProgress.Equals(WorkProgress.Zero) || board.OccupiesDeliveryDock(job, worker));
                board.ValidateHaul(job, false); // Validate itinerary/progress/claims before any replacement commitment.
                IInventory cargo = TransporterContracts.ReadCargo(_world, worker);
                ResourceReceiptCheck held = cargo.CheckReservedOut(job.ReservationOwner, job.CargoOutReservation);
                if (!held.Ready || held.Record.Resource != job.Resource || held.Record.Amount != job.Loaded)
                    throw new InvalidOperationException("recovery lost its actual loaded cargo promise");
                IResourceTransaction transactions = world.Services.Get<IResourceTransaction>();
                ResourceClaimSnapshot previousClaims = transactions.ReadClaims(job.Owner, job.InReservation.Endpoint, job.ReservationOwner);
                if (previousClaims.Status != ResourceTransferStatus.MissingEndpoint &&
                    (previousClaims.Status != ResourceTransferStatus.Ready ||
                    !ResourceReservationBook.Matches(previousClaims.Records, job.DestinationEndpoint, job.ReservationOwner,
                        job.InReservation, job.Resource, job.Loaded, ReservationDirection.Inbound)))
                    throw new InvalidOperationException("recovery lost its actual destination promise");
                if (leaveDock)
                {
                    // Admit the actual exit only. Replacement demand/capacity is re-evaluated after
                    // physical departure; an unavailable future output cannot imprison the old bay.
                    Code departure = board.PreviewRecoveryDeparture(job, worker, out Code exit);
                    if (!departure.IsNone) return false;
                    // This actual job retains both bay and selected exit. No future capacity is promised yet.
                    job.RecoveryCause = blocker; job.RecoveryDockSlot = job.DestinationDockSlot;
                    job.DestinationDockSlot = exit; job.ToilIndex = job.Toils.Count - 1;
                    job.RetryAt = 0; job.Failures = 0; job.Blocker = Code.None; job.State = JobState.Assigned; job.TripHeld = false;
                    MotionExecutionContracts.Stop(world, worker);
                    return true;
                }
                // Restored original access resumes the same acquired delivery, after actual egress.
                // It uses the common preview and its existing receipt, never a second capacity claim.
                HaulPreview? restored = recovering && binding.IsNone
                    ? Preview(job.Owner, cargo.Entity.Id, job.To, job.Resource, job.Loaded, false, worker,
                        demand: job.HaulDemand, selectedWorker: job.Worker, continuing: job, destinationPort: job.DestinationPort,
                        destinationEndpoint: job.DestinationEndpoint, selection: job.HaulSelection)
                    : (HaulPreview?)null;
                HaulPreview next = restored.HasValue && restored.Value.Status == HaulStatus.Ready && restored.Value.Amount == job.Loaded
                    ? restored.Value : PreviewDestination(world, cargo.Entity, job.Resource, job.Owner, worker,
                        selectedWorker: job.Worker, continuing: job, destinationSettings: settings);
                if (next.Status != HaulStatus.Ready || next.Amount != job.Loaded)
                {
                    if (binding.IsNone) return false;
                    // Lost physical access never becomes available stock/room. The actual original
                    // endpoint may still own its receipt; retain it while waiting for whole replacement.
                    job.RetryAt = 0; job.Failures = 0; job.TripHeld = false;
                    job.Blocker = HaulRecoveryCodes.DestinationUnavailable; job.State = JobState.Paused;
                    MotionExecutionContracts.Stop(world, worker);
                    return true;
                }
                IResourceInlet output = PhysicalPort(world.Entities.Get(next.Destination), next.DestinationPort, job.Resource, next.DestinationEndpoint).Store;
                if (output == null) throw new InvalidOperationException("admitted recovery replacement disappeared");
                bool retainedDestination = next.DestinationEndpoint == job.DestinationEndpoint;
                bool changedAccess = next.Destination != job.To || next.DestinationPort != job.DestinationPort;
                ReservationToken replacement = job.InReservation;
                if (retainedDestination)
                {
                    if (previousClaims.Status != ResourceTransferStatus.Ready)
                        throw new InvalidOperationException("retained delivery lost its original endpoint receipt");
                }
                else
                {
                    // Promise the complete remaining delivery before withdrawing the old promise. Cargo never changes.
                    ResourceClaimResult claimed = transactions.AcquireClaims(job.Owner, new[] {
                        new ResourceClaimIntent(ReservationDirection.Inbound, job.ReservationOwner, next.DestinationEndpoint,
                            new Bill(new[] { new ResourceAmount(job.Resource, job.Loaded) })) }, System.Array.Empty<ResourceReservation>());
                    if (!claimed.Committed || claimed.Receipts.Count != 1)
                        throw new InvalidOperationException("destination violated successful replanning preflight");
                    replacement = claimed.Receipts[0].Token;
                }
                ResourceReceiptCheck replacementCheck = output.CheckReservedIn(world.Content, job.ReservationOwner, replacement);
                if (!replacementCheck.Ready || replacementCheck.Record.Resource != job.Resource || replacementCheck.Record.Amount != job.Loaded)
                    throw new InvalidOperationException("destination returned a mismatched replanning promise");
                if (!retainedDestination && previousClaims.Status == ResourceTransferStatus.Ready)
                {
                    // Physical site loss does not destroy an independent endpoint's actual receipt.
                    var previous = new ResourceReservation(job.InReservation, job.ReservationOwner, job.Resource, job.Loaded, ReservationDirection.Inbound);
                    if (!transactions.ReleaseClaims(new ResourceClaimReleaseIntent(job.Owner, new[] { previous }, System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceEndpointId>(), ResourceClaimReleasePolicy.Exact)).Committed)
                        throw new InvalidOperationException("original recovery destination claim changed after replacement");
                }
                EntityId previousTarget = job.To;
                for (int i = delivery; i < job.Toils.Count; i++)
                { Toil toil = job.Toils[i]; if (toil.At != previousTarget) throw new InvalidOperationException("invalid recovery delivery itinerary"); toil.At = next.Destination; job.Toils[i] = toil; }
                job.To = next.Destination; job.Target = next.Destination; job.InReservation = replacement;
                job.HaulDemand = next.HaulDemand;
                job.DestinationPort = next.DestinationPort; job.DestinationEndpoint = next.DestinationEndpoint;
                job.DestinationDockSlot = next.DestinationDockSlot;
                if (!retainedDestination || changedAccess)
                { job.UnloadingBaseline = job.Amount - job.Loaded; job.HandlingProgress = WorkProgress.Zero; }
                job.ToilIndex = delivery;
                job.RecoveryCause = Code.None; job.RecoveryDockSlot = Code.None;
                job.RetryAt = 0; job.Failures = 0; job.Blocker = Code.None; job.State = JobState.Assigned; job.TripHeld = false;
                MotionExecutionContracts.Stop(world, worker);
                return true;
            }
            catch (Exception error) { _world.LatchFault("logistics.replan", error); throw; }
        }
        private void SyncRecoveries(IStateSync state)
        {
            ValidateRecoveryPolicy(); string profile = RecoveryPolicy?.Id; state.String(ref profile);
            if (profile != RecoveryPolicy?.Id) throw new CorruptStateException("saved recovery policy differs from selected content");
            int count = state.Count(_recoveries.Count); if (state.Reading) _recoveries.Clear();
            var seen = new HashSet<(LogisticsRecoveryKind, ResourceEndpointId, ResourceId, PlayerId)>();
            for (int i = 0; i < count; i++)
            {
                var intent = state.Reading ? default : _recoveries[i]; byte kind = (byte)intent.Kind; state.Byte(ref kind);
                intent.Kind = (LogisticsRecoveryKind)kind;
                state.Entity(ref intent.Origin); state.Resource(ref intent.Resource); state.Player(ref intent.Company);
                string port = intent.SourcePort.Id; state.String(ref port);
                if (port != null && string.IsNullOrWhiteSpace(port)) throw new CorruptStateException("invalid saved recovery source port");
                if (state.Reading) intent.SourcePort = port == null ? Code.None : new Code(port);
                ResourceEndpointId.Sync(state, ref intent.SourceEndpoint);
                state.Long(ref intent.Created); state.Long(ref intent.LastDispatch);
                if (profile == null || kind > (byte)LogisticsRecoveryKind.CargoReturn || !intent.Origin.IsValid || intent.Origin.Value >= _world.Entities.NextIdentity.Value ||
                    !intent.SourceEndpoint.IsValid || intent.Kind == LogisticsRecoveryKind.CargoReturn && !intent.SourcePort.IsNone ||
                    intent.Kind == LogisticsRecoveryKind.Evacuation && intent.SourcePort.IsNone &&
                        intent.SourceEndpoint != ResourceEndpointId.ForEntity(intent.Origin) ||
                    !_world.Players.IsSeat(intent.Company) || _world.Content.Resource(intent.Resource) == null ||
                    intent.Created < 0 || intent.LastDispatch < intent.Created || intent.LastDispatch > _world.Clock.StepIndex ||
                    !seen.Add((intent.Kind, intent.SourceEndpoint, intent.Resource, intent.Company))) throw new CorruptStateException("invalid saved recovery intent chronology");
                // Expired rows await normal preparation; observing a checkpoint never prunes state.
                if (state.Reading) _recoveries.Add(intent);
            }
        }
    }
}
