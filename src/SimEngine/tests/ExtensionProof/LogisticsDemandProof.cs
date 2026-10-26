using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // An ordinary engine-side mod, compiled using public framework contracts only. No matcher,
    // stock table, reservation book, worker dispatch or haul progression belongs to this module.
    public sealed class DemandProofDef : Def
    {
        private string _destination;
        private BillSpec _quantities;
        public LogisticsDemandId Source { get; private set; }
        public PlayerId Company { get; private set; }
        public EntityDef Destination { get; private set; }
        public Bill Quantities { get; private set; }
        public int Priority { get; private set; }
        public bool InitiallyActive { get; private set; }
        public bool Emergency { get; private set; }
        protected override void ReadCore(JsonReader r)
        {
            try { Source = new LogisticsDemandId(r.String("scope"), r.Long("identity"), Code.None); }
            catch (ArgumentException ex) { throw r.Error(ex.Message); }
            Company = new PlayerId(r.Int("company")); _destination = r.String("destination");
            _quantities = BillSpec.Read(r, "quantities"); Priority = r.Int("priority");
            InitiallyActive = r.Bool("active"); Emergency = r.Bool("emergency");
            if (!Company.IsValid || Priority < 0 || _quantities.IsEmpty) throw r.Error("explicit owner, priority and positive bill required");
        }
        protected override void ResolveCore(ContentResolver content)
        { Destination = content.Def<EntityDef>(_destination); Quantities = _quantities.Resolve(content); }
    }
    public interface IDemandProof { void Retire(); }
    public sealed class DemandProofOwner : ILogisticsDemandOwner, IPersistentService, IDemandProof
    {
        private bool _active;
        public World World { get; }
        public DemandProofDef Definition { get; }
        public string DemandScope => Definition.Source.Scope;
        public string Id { get; }
        public int StateSchemaVersion => 1;
        public DemandProofOwner(World world, DemandProofDef definition, string stateId = "proof.demands.source")
        { World = world; Definition = definition; Id = stateId; _active = definition.InitiallyActive; }
        public Code CheckSourceDisclosure(PlayerId viewer, LogisticsDemandId source, PlayerId company, EntityId destination)
        {
            if (viewer != PlayerId.None && viewer != company) return Codes.NotFound;
            var view = World.ViewFor(viewer);
            if (!source.Equals(Definition.Source) || company != Definition.Company || !view.TryGetEntity(destination, out var entity) || entity.Owner != company) return Codes.NotFound;
            var catalog = new List<Beep.SimEngine.Hosting.CatalogueEntry>(); view.CopyCatalogue(catalog);
            bool prototype = false; foreach (var entry in catalog) if (entry.Id == Definition.Destination.Id) prototype = true;
            if (!prototype) return Codes.NotFound;
            var resources = new List<Beep.SimEngine.Hosting.ResourceInfo>(); view.CopyResources(resources);
            foreach (var line in Definition.Quantities.Lines)
            { bool known = false; foreach (var resource in resources) if (resource.Resource == line.Resource) known = true; if (!known) return Codes.NotFound; }
            return Code.None;
        }
        public LogisticsDemandSourceCheck CheckSource(LogisticsDemandId source, PlayerId company, EntityId destination)
        {
            if (!source.Equals(Definition.Source)) return LogisticsDemandSourceCheck.Invalid(LogisticsDemandCodes.InvalidSource);
            Entity entity = World.Entities.Get(destination);
            if (company != Definition.Company || entity != null && (entity.Owner != company || entity.Def != Definition.Destination))
                return LogisticsDemandSourceCheck.Invalid(Codes.NotOwned);
            return !_active || entity == null ? LogisticsDemandSourceCheck.Expired(LogisticsDemandCodes.Expired) : LogisticsDemandSourceCheck.Active(Definition.Emergency);
        }
        public void Retire() => _active = false;
        public void Sync(IStateSync sync) => sync.Bool(ref _active);
    }
    public sealed class SubmitProofDemandCommand : Command
    { public override void Sync(IStateSync sync) { } }
    public sealed class RetireProofDemandCommand : Command
    {
        public bool Withdraw;
        public override void Sync(IStateSync sync) => sync.Bool(ref Withdraw);
    }
    public sealed class SubmitProofDemandHandler : CommandHandler<SubmitProofDemandCommand>
    {
        public static Entity Destination(World world, DemandProofDef definition)
        {
            foreach (Entity entity in world.Entities.All)
                if (entity.Alive && entity.Owner == definition.Company && entity.Def == definition.Destination) return entity;
            return null;
        }
        protected override Code Check(World world, SubmitProofDemandCommand command)
        {
            DemandProofDef def = world.Services.Get<DemandProofOwner>().Definition;
            if (command.Player != def.Company) return Codes.NotOwned;
            Entity entity = Destination(world, def);
            return entity == null ? Codes.NotFound : world.Services.Get<ILogisticsDemandRequests>().CheckRequests(def.Source, def.Company, entity.Id, new[] { LogisticsSupplyRequest.Entity(entity.Id, def.Quantities) }, def.Priority);
        }
        protected override void Apply(World world, SubmitProofDemandCommand command, CommandContext context)
        {
            DemandProofDef def = world.Services.Get<DemandProofOwner>().Definition;
            Code result = world.Services.Get<ILogisticsDemandRequests>().ReplaceRequests(def.Source, def.Company, Destination(world, def).Id, new[] { LogisticsSupplyRequest.Entity(Destination(world, def).Id, def.Quantities) }, def.Priority);
            if (!result.IsNone) throw new InvalidOperationException("proof demand changed after check: " + result);
        }
    }
    public sealed class RetireProofDemandHandler : CommandHandler<RetireProofDemandCommand>
    {
        protected override Code Check(World world, RetireProofDemandCommand command)
            => command.Player == world.Services.Get<DemandProofOwner>().Definition.Company ? Code.None : Codes.NotOwned;
        protected override void Apply(World world, RetireProofDemandCommand command, CommandContext context)
        {
            DemandProofOwner owner = world.Services.Get<DemandProofOwner>(); owner.Retire();
            if (command.Withdraw) world.Services.Get<ILogisticsDemandRequests>().WithdrawRequests(owner.Definition.Source);
        }
    }
    public sealed class DemandProofModule : GameModule
    {
        private readonly bool _duplicate;
        private readonly World _foreign;
        private readonly bool _incidents;
        public DemandProofModule(bool duplicate = false, World foreign = null, bool incidents = false) { _duplicate = duplicate; _foreign = foreign; _incidents = incidents; }
        public override string Id => "proof.demands";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => _incidents
            ? new[] { new ModuleRequirement(LogisticsModule.ModuleId, Version), new ModuleRequirement(IncidentResponseModule.ModuleId, Version) }
            : new[] { new ModuleRequirement(LogisticsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes content)
        {
            content.Section<DemandProofDef>("proof_demands");
            if (_incidents) content.Component<ContractIncidentDef>("contract_incident", 3);
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Require<ILogisticsDemandRequests>();
            DemandProofDef def = composer.Content.Get<DemandProofDef>("proof.delivery");
            var owner = new DemandProofOwner(_foreign ?? composer.World, def);
            composer.Services.Bind(owner); composer.Services.Bind<IDemandProof>(owner);
            if (_duplicate) composer.Services.Bind<ILogisticsDemandOwner>(new DemandProofOwner(composer.World, def, "proof.demands.other"));
            composer.Commands.Register("proof.demands.submit", new SubmitProofDemandHandler());
            composer.Commands.Register("proof.demands.retire", new RetireProofDemandHandler());
        }
    }
}
