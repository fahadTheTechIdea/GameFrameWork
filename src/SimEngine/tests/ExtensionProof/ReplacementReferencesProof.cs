using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class ReplacementReferenceDef : ComponentDef<ReplacementReference>
    {
        public string Mode { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Mode = reader.String("mode");
            if (Mode != "normal" && Mode != "deny" && Mode != "mutate-check" && Mode != "throw-commit" && Mode != "despawn-commit" &&
                Mode != "clock-commit" && Mode != "speed-commit" && Mode != "space-commit" && Mode != "random-commit" && Mode != "random-restore-commit" &&
                Mode != "move-source-commit" && Mode != "move-target-commit" && Mode != "stock-commit" && Mode != "claim-commit" &&
                Mode != "other-stock-commit" && Mode != "other-claim-commit" && Mode != "other-claim-cycle-commit")
                throw reader.Error("explicit replacement reference mode required");
        }
    }
    public sealed class ReplacementReference : Component<ReplacementReferenceDef>, IEntityReplacementReferenceOwner
    {
        public EntityId Reference;
        public int Commits;
        public bool CheckpointRejected;
        public Code CheckReferenceReplacement(World world, Entity source, EntityDef destination)
        {
            if (Reference != source.Id) return Code.None;
            if (Def.Mode == "deny") return new Code("proof.reference-denied");
            if (Def.Mode == "mutate-check") { Commits++; Entity.Touch(); }
            return Code.None;
        }
        public void ReplaceEntityReferences(World world, Entity source, Entity destination)
        {
            if (Reference != source.Id) return;
            if (!ReferenceEquals(world.Entities.Get(source.Id), source) || !ReferenceEquals(world.Entities.Get(destination.Id), destination))
                throw new InvalidOperationException("reference commit requires both actual live identities");
            if (Def.Mode == "throw-commit") throw new InvalidOperationException("authored reference commit failure");
            if (Def.Mode == "despawn-commit") { world.Despawn(source); return; }
            // Read-only fault probe: a normal checkpoint writer must not accept the completion transient.
            try { world.Services.Get<Beep.SimEngine.Standard.WorkBoard>().Sync(new StateHasher()); }
            catch (CorruptStateException error)
            {
                if (!error.Message.Contains("completion retry differs")) throw;
                CheckpointRejected = true;
            }
            Reference = destination.Id; Commits++; Entity.Touch();
            // Negative reference-only commit probes. Own reference changes above are
            // legitimate; unrelated clock/geometry/resource changes below are not.
            if (Def.Mode == "clock-commit") ((Beep.SimEngine.Hosting.GameClock)world.Clock).Pause();
            if (Def.Mode == "speed-commit") ((Beep.SimEngine.Hosting.GameClock)world.Clock).Speed = 2;
            if (Def.Mode == "space-commit") { var space=(PointSpace)world.Space;space.SetAccess(!space.AccessOpen); }
            if (Def.Mode == "random-commit") world.Random.NextULong();
            if (Def.Mode == "random-restore-commit") { ulong state=world.Random.State;world.Random.NextULong();world.Random.State=state; }
            if (Def.Mode == "move-source-commit") { var point=source.Get<PointPresence>();point.Move(world,point.Position+Fix.One); }
            if (Def.Mode == "move-target-commit") { var point=destination.Get<PointPresence>();point.Move(world,point.Position+Fix.One); }
            if (Def.Mode == "stock-commit") { var stock=destination.Get<Beep.SimEngine.Standard.IInventory>();stock.Remove(stock.Stock[0].Resource,Fix.One); }
            if (Def.Mode == "claim-commit") { var stock=destination.Get<Beep.SimEngine.Standard.IInventory>();
                stock.ReserveIn(new ReservationOwner("proof.reference-claim",destination.Id.Value),stock.Stock[0].Resource,Fix.One); }
            if (Def.Mode.StartsWith("other-", StringComparison.Ordinal))
                foreach (Entity entity in world.Entities.All) if (entity.Alive && entity.Def.Id == "reference.stock") {
                    var stock = entity.Get<Beep.SimEngine.Standard.IInventory>(); var resource = stock.Stock[0].Resource;
                    if (Def.Mode == "other-stock-commit") stock.Remove(resource, Fix.One);
                    else {
                        var requester = new ReservationOwner("proof.unrelated-reference-claim", entity.Id.Value);
                        ReservationToken token = stock.ReserveIn(requester, resource, Fix.One);
                        if (Def.Mode == "other-claim-cycle-commit") stock.ReleaseIn(requester, token);
                    }
                }
        }
        public override void Sync(IStateSync sync)
        { sync.Entity(ref Reference); sync.Int(ref Commits); sync.Bool(ref CheckpointRejected); }
    }
    public sealed class SetReplacementReferenceCommand : Command
    {
        public EntityId Owner, Target;
        public override void Sync(IStateSync sync) { sync.Entity(ref Owner); sync.Entity(ref Target); }
    }
    internal sealed class SetReplacementReferenceHandler : CommandHandler<SetReplacementReferenceCommand>
    {
        protected override Code Check(World world, SetReplacementReferenceCommand command)
        {
            Entity owner = world.Entities.Get(command.Owner), target = world.Entities.Get(command.Target);
            if (owner == null || target == null || owner.Owner != command.Player || target.Owner != command.Player) return Codes.NotFound;
            return owner.Get<ReplacementReference>() == null ? Codes.InvalidArgument : Code.None;
        }
        protected override void Apply(World world, SetReplacementReferenceCommand command, CommandContext result)
        { var part = world.Entities.Get(command.Owner).Get<ReplacementReference>(); part.Reference = command.Target; part.Entity.Touch(); }
    }
    public readonly struct ReplacementReferenceFacts
    {
        public EntityId Reference { get; }
        public int Commits { get; }
        public bool CheckpointRejected { get; }
        public ReplacementReferenceFacts(EntityId reference, int commits, bool checkpointRejected)
        { Reference = reference; Commits = commits; CheckpointRejected = checkpointRejected; }
    }
    public sealed class ReplacementReferenceView
    {
        private readonly World _world; private readonly PlayerId _viewer;
        internal ReplacementReferenceView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        public bool Read(EntityId id, out ReplacementReferenceFacts facts)
        {
            facts = default; _world.RequireHealthyUpdate();
            Entity entity = _world.Entities.Get(id);
            if (entity == null || entity.Owner != _viewer || entity.Get<ReplacementReference>() == null) return false;
            var owner = entity.Get<ReplacementReference>();
            facts = new ReplacementReferenceFacts(owner.Reference, owner.Commits, owner.CheckpointRejected);
            return true;
        }
    }
    public sealed class ReplacementReferencesModule : GameModule
    {
        public override string Id => "proof.references";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement("sim.work", Version) });
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<ReplacementReferenceDef>("replacement_reference", 2);
            types.Section<ReferenceSchemaPlanDef>("reference_plans");
            types.Component<ProofRecurringSourceDef>("reference_recurring", 6);
            types.Section<SystemReferenceSettingsDef>("system_reference_settings");
            types.Section<IndependentControlActionDef>("reference_control_actions");
            types.Component<ProofNetworkPortsDef>("reference_network_ports", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Bind<Beep.SimEngine.Standard.IActivitySource>(new ReferenceSchemaPlanSource(composer.Content));
            foreach (var settings in composer.Content.All<SystemReferenceSettingsDef>()) SystemReferenceProof.Compose(composer, settings);
            composer.Commands.Register("proof.set-reference", new SetReplacementReferenceHandler());
            composer.AddViewFeature("proof.reference", 2, (world, viewer) => new ReplacementReferenceView(world, viewer));
        }
    }
}

