using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>JSON-authored fixed shipment contributed by an ordinary public-only engine module.</summary>
    public sealed class DispatchProofDef : Def
    {
        private string _from, _to, _resource;
        public EntityDef From { get; private set; }
        public EntityDef To { get; private set; }
        public ResourceId Resource { get; private set; }
        public PlayerId Company { get; private set; }
        public long Identity { get; private set; }
        public int Priority { get; private set; }
        public Fix Desired { get; private set; }
        public GameDuration Interval { get; private set; }
        public bool Enabled { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _from = reader.String("from"); _to = reader.String("to"); _resource = reader.String("resource");
            Company = new PlayerId(reader.Int("company")); Identity = reader.Long("identity");
            Priority = reader.Int("priority"); Desired = reader.Fix("desired");
            JsonValue enabled = reader.Value("enabled");
            if (enabled == null || enabled.Kind != JsonKind.Bool) throw reader.Error("enabled must be an explicit boolean");
            Enabled = enabled.Bool;
            Interval = new JsonReader(reader.Object("interval") ?? throw reader.Error("interval must be explicit"), reader.Where + ".interval").RationalDuration();
            if (!Company.IsValid || Identity <= 0 || Priority < 0 || !Desired.IsPositive || Interval.IsZero)
                throw reader.Error("shipment needs explicit owner/identity/priority/quantity/interval");
        }
        protected override void ResolveCore(ContentResolver content)
        { From = content.Def<EntityDef>(_from); To = content.Def<EntityDef>(_to); Resource = content.Resource(_resource); }
    }
    public interface IDispatchProof { }
    public sealed class DispatchProofSource : IWorkerDispatchSource, IDispatchProof
    {
        private readonly DispatchProofDef _definition;
        public World World { get; }
        public string DispatchScope { get; }
        /// <summary>Explicit contract-corruption injection used only by framework verification.</summary>
        public int CandidateFault { get; set; }
        public DispatchProofSource(World world, DispatchProofDef definition, string scope = "proof.shipments")
        { World = world; _definition = definition; DispatchScope = scope; }
        public Schedule GetDispatchSchedule(ISimulationTime time) => Schedule.Every(time, _definition.Interval);
        public void PrepareDispatch() { }
        private Entity Find(EntityDef definition)
        {
            foreach (Entity entity in World.Entities.All)
                if (entity.Alive && entity.Def == definition && entity.Owner == _definition.Company) return entity;
            return null;
        }
        private HaulPreview Preview(out Fix gap, EntityId worker = default)
        {
            Entity from = Find(_definition.From), to = Find(_definition.To); IInletPort inlet = to?.Get<IInletPort>();
            gap = inlet == null ? Fix.Zero : _definition.Desired - inlet.Amount(_definition.Resource) - inlet.ReservedIn(_definition.Resource);
            return gap.IsPositive ? World.Services.Get<IHaulExecution>().PreviewHaul(_definition.Company, from?.Id ?? EntityId.None,
                to.Id, _definition.Resource, gap, worker: worker) : default; // No offer exists when no native gap remains.
        }
        public void CopyOffers(PlayerId viewer, WorkCandidateMode mode, IList<WorkerDispatchOffer> destination)
        {
            destination.Clear(); if (!_definition.Enabled || viewer.IsValid && viewer != _definition.Company) return;
            Entity to = Find(_definition.To); if (to == null) return;
            HaulPreview haul = Preview(out Fix gap); if (!gap.IsPositive) return;
            bool ready = haul.Status == HaulStatus.Ready;
            destination.Add(new WorkerDispatchOffer(DispatchScope, _definition.Identity, "shipment", _definition.Company,
                to.Id, haul.Carrier, ready, ready ? Code.None : haul.Blocker.IsNone ? new Code("proof.shipment-unavailable") : haul.Blocker,
                false, _definition.Priority, 0, World.Tick, haul.TravelCost, 1, 0, true)); // Definition intent exists from new-game tick zero.
        }
        public void CopyCandidates(long identity, string entry, WorkCandidateMode mode, IList<WorkerDispatchOffer> destination)
        {
            destination.Clear(); var offers = new List<WorkerDispatchOffer>(); CopyOffers(PlayerId.None, mode, offers);
            if (offers.Count == 0 || identity != offers[0].Identity || entry != offers[0].Entry || !offers[0].Ready) return;
            WorkerDispatchOffer best = offers[0]; destination.Add(best);
            if (CandidateFault == 1) { destination.Add(best); return; }
            if (CandidateFault == 2) { destination.Clear(); return; }
            if (CandidateFault == 3)
            {
                destination.Clear(); destination.Add(new WorkerDispatchOffer(DispatchScope, identity, entry, best.Company,
                    best.Target, best.Worker, true, Code.None, false, checked(best.Priority + 1), best.CreatedTick, World.Tick, best.TravelCost, 1, 0, true)); return;
            }
            foreach (Entity entity in World.Entities.All)
            {
                if (!entity.Alive || entity.Id == best.Worker || entity.Get<IWorker>() == null) continue;
                HaulPreview haul = Preview(out Fix gap, entity.Id); if (!gap.IsPositive || haul.Status != HaulStatus.Ready) continue;
                destination.Add(new WorkerDispatchOffer(DispatchScope, identity, entry, best.Company, best.Target, haul.Carrier,
                    true, Code.None, false, best.Priority, best.CreatedTick, World.Tick, haul.TravelCost, 1, 0, true));
            }
        }
        public bool TryAcquire(long identity, string entry, EntityId worker, WorkCandidateMode mode)
        {
            if (!worker.IsValid || !_definition.Enabled || identity != _definition.Identity || entry != "shipment") return false;
            HaulPreview haul = Preview(out Fix gap, worker);
            return haul.Status == HaulStatus.Ready && World.Services.Get<IHaulExecution>().AcquireHaul(_definition.Company,
                haul.Source, haul.Destination, _definition.Resource, gap, _definition.Priority, worker: worker) != 0;
        }
    }
    public sealed class DispatchProofModule : GameModule
    {
        private readonly bool _duplicate;
        private readonly World _foreign;
        public DispatchProofModule(bool duplicate = false, World foreign = null) { _duplicate = duplicate; _foreign = foreign; }
        public override string Id => "proof.shipments";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[]
        {
            new ModuleRequirement(LogisticsModule.ModuleId, Version), new ModuleRequirement(IncidentResponseModule.ModuleId, Version)
        };
        public override void DeclareContent(ContentTypes content)
        { content.Section<DispatchProofDef>("proof_shipments"); content.Section<ShipmentAlertProfileDef>("proof_alert_profiles"); content.Section<ShipmentDispatchProfileDef>("proof_dispatch_profiles"); content.Section<DualInputRequirementDef>("proof_dual_input_requirements"); content.Section<RoleInputRequirementDef>("proof_role_input_requirements"); content.Component<ContractIncidentDef>("contract_incident", 3); content.Component<ProofWorkPolicyDef>("proof_work_policy", 2); }
        public override void Compose(WorldComposer composer)
        {
            var source = new DispatchProofSource(_foreign ?? composer.World, composer.Content.Get<DispatchProofDef>("proof.shipment"));
            composer.Services.Bind(source); composer.Services.Bind<IDispatchProof>(source); // Alias deduplicates through declared services.
            if (_duplicate) composer.Services.Bind<IDuplicateDispatchProof>(new DuplicateDispatchProof(composer.World, source));
        }
    }
    public interface IDuplicateDispatchProof { }
    public sealed class DuplicateDispatchProof : IWorkerDispatchSource, IDuplicateDispatchProof
    {
        private readonly DispatchProofSource _source;
        public World World { get; }
        public string DispatchScope => _source.DispatchScope;
        public DuplicateDispatchProof(World world, DispatchProofSource source) { World = world; _source = source; }
        public Schedule GetDispatchSchedule(ISimulationTime time) => _source.GetDispatchSchedule(time);
        public void PrepareDispatch() => _source.PrepareDispatch();
        public void CopyOffers(PlayerId viewer, WorkCandidateMode mode, IList<WorkerDispatchOffer> destination) => _source.CopyOffers(viewer, mode, destination);
        public void CopyCandidates(long identity, string entry, WorkCandidateMode mode, IList<WorkerDispatchOffer> destination) => _source.CopyCandidates(identity, entry, mode, destination);
        public bool TryAcquire(long identity, string entry, EntityId worker, WorkCandidateMode mode) => _source.TryAcquire(identity, entry, worker, mode);
    }
}
