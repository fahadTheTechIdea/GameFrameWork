using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class ProofOperatingInputsDef : ComponentDef<ProofOperatingInputs>, IOperatingInputsDefinition
    {
        public bool BindsEntityInventory => true;
        public ICapacityPolicy CapacityPolicy => Owner.Component<IInventoryDefinition>().CapacityPolicy;
        public bool Accepts(ResolvedContent content, ResourceId resource)
        {
            foreach (var rate in Inputs) if (rate.Resource.Resource == resource) return Owner.Component<IInventoryDefinition>().Accepts(content, resource);
            return false;
        }
        private ResourceRateSpec _inputs;
        public Code CapabilityId { get; private set; }
        public OperatingPhase Phase { get; private set; }
        public IReadOnlyList<IResourceRate> Inputs { get; private set; }
        public int Fault { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            CapabilityId = new Code(reader.String("id")); _inputs = ResourceRateSpec.Read(reader, "inputs"); Fault = reader.Int("fault");
            string phase = reader.String("phase");
            Phase = phase == "movement" ? OperatingPhase.Movement : phase == "idle" ? OperatingPhase.Idle : phase == "standby" ? OperatingPhase.Standby : throw reader.Error("invalid proof operating phase");
            if (Fault < 0 || Fault > 9) throw reader.Error("unknown operating proof fault");
        }
        protected override void ResolveCore(ContentResolver content) => Inputs = _inputs.Resolve(content);
        protected override void ValidateCore(EntityDef owner)
        { if (!owner.HasCapability<IInventory>() || (Phase == OperatingPhase.Movement ? !owner.HasCapability<IMotion>() : !owner.HasCapability<IWorker>())) throw new ContentException(owner.Id + ": proof needs actual phase target/inventory contracts"); }
    }
    // Public-only phase provider. The engine calculates resource expenditure; this provider owns exposure only.
    public sealed class ProofOperatingInputs : Component<ProofOperatingInputsDef>, IOperatingInputs
    {
        private long _elapsed, _updated = -1;
        public Code CapabilityId => Def.CapabilityId;
        public ResourcePortResolution ResolveStore(World world, ResourceEndpointId? original = null)
            => original.HasValue && original.Value != ResourceEndpointId.ForEntity(Entity.Id)
                ? ResourcePortResolution.Unavailable(ResourcePortCodes.OriginalUnavailable):ResourcePortResolution.ReadyStore(Entity.Get<IInventory>());
        public IResourcePort ResolveUtilityPort(World world, ResourceId resource)
        {
            foreach (var rate in Def.Inputs) if (rate.Resource.Resource == resource) return this;
            return null;
        }
        public UtilityDemandSnapshot ReadUtilityDemand(World world)
        {
            var facts = OperatingInputsContracts.ReadDemand(world, this);
            if (Def.Fault < 6) return facts;
            return new UtilityDemandSnapshot(facts.Consumer, Def.Fault == 9 ? checked(facts.ElapsedTicks + 1) : facts.ElapsedTicks,
                facts.Ports, Def.Fault == 7 ? false : facts.Drawing,
                Def.Fault == 8 ? new Bill(new[] { new ResourceAmount(Def.Inputs[0].Resource.Resource, Fix.One) }) : facts.Consumed,
                Def.Fault == 6 ? Bill.Empty : facts.NextInputs, Def.Fault == 6 ? Bill.Empty : facts.Available, facts.NextBlocker);
        }
        public OperatingExposureSnapshot ReadExposure()
        {
            if (Def.Fault == 1) Entity.Touch();
            return Def.Fault == 2 ? new OperatingExposureSnapshot(-1, -1) : Def.Fault == 3 ? new OperatingExposureSnapshot(1, long.MaxValue) : new OperatingExposureSnapshot(_elapsed, _updated);
        }
        public void AdvanceExposure(OperatingInputsContext context)
        {
            if (Def.Fault == 4) return;
            if (!ReferenceEquals(context.Entity, Entity) || context.Source != CapabilityId || context.Phase != Def.Phase || context.Before.ElapsedTicks != _elapsed || context.Before.UpdatedTick != _updated)
                throw new InvalidOperationException("proof rejected stale/foreign context");
            _elapsed = context.After.ElapsedTicks; _updated = context.After.UpdatedTick; Entity.Touch();
            if (Def.Fault == 5) Entity.Get<IInventory>().Remove(Def.Inputs[0].Resource.Resource, Fix.One);
        }
        public override void Sync(IStateSync sync)
        {
            long elapsed = _elapsed, updated = _updated; sync.Long(ref elapsed); sync.Long(ref updated);
            if (!OperatingInputsContracts.IsStructurallyValid(new OperatingExposureSnapshot(elapsed, updated))) throw new CorruptStateException("invalid proof exposure");
            if (sync.Reading) { _elapsed = elapsed; _updated = updated; }
        }
    }
}
