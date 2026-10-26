using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Engine-side authored provider. No tick, completion queue, copied progress or private engine access.
    public sealed class CompletionEffectDef : Def, IWorkEffect, IWorkEffectPreview, IWorkEffectPlanValidator, IWorkEffectIntentAdmission
    {
        public IReadOnlyList<IContentDefinition> ContentDependencies { get; private set; }
        private string _mode;
        private BillSpec _spec;
        private Bill _bill;
        public static readonly Code Rejected = new Code("proof.completion-rejected");
        protected override void ReadCore(JsonReader reader)
        {
            _mode = reader.String("mode");
            if (_mode != "wait" && _mode != "fail" && _mode != "post_claim" && _mode != "mutating_pending" && _mode != "admission_wait") throw reader.Error("invalid completion mode");
            _spec = BillSpec.Read(reader, "quantities");
            if (_spec.IsEmpty) throw reader.Error("completion effect needs a positive native bill");
        }
        protected override void ResolveCore(ContentResolver content)
        { _bill = _spec.Resolve(content); ContentDependencies = _bill.ContentReferences(content.Content); }
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
        {
            if (!plan.Graph.HasMachineWork(node?.Id)) throw new ContentException("completion proof requires physical ancestry");
        }
        public Code CheckIntent(World world, PlayerId company, Entity target)
        {
            if (target == null || !ReferenceEquals(world.Entities.Get(target.Id), target)) return Codes.NotFound;
            return target.Owner == company ? Code.None : Codes.NotOwned;
        }
        public Code CheckStart(World world, PlayerId company, Entity target)
        {
            Code check = CheckIntent(world, company, target); if (!check.IsNone) return check;
            return _mode == "admission_wait" && target.Get<IInventory>()?.Reservations.Count > 0 ? ResourceTransactions.ActivePromises : Code.None;
        }
        public Code Check(World world, Order order, IActivityNodeDefinition node)
        {
            Entity target = world.Entities.Get(order.Target);
            if (target == null) return Codes.NotFound;
            if (target.Owner != order.Owner) return Codes.NotOwned;
            if (_mode == "fail") return Rejected;
            if (target.Get<IInventory>()?.Reservations.Count > 0) return ResourceTransactions.ActivePromises;
            return world.Services.Get<IResourceTransaction>().EvaluateInventory(Intent(order)).Ready ? Code.None : WorkEffectCodes.IncompatibleInventory;
        }
        private ResourceInventoryIntent Intent(Order order) => new ResourceInventoryIntent(
            new ReservationOwner("proof.completion", order.Id), order.Owner, order.Target, _bill,
            ResourceFlowKind.Produced, new Code(Id));
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
        {
            if (node == null ? order.State != OrderState.Completing : order.Node(node.Id).State != NodeState.Completing)
                return WorkEffectResult.Failed(WorkEffectCodes.NotComplete);
            Code refusal = Check(world, order, node);
            if (refusal == ResourceTransactions.ActivePromises) return WorkEffectResult.Pending(refusal);
            if (!refusal.IsNone) return WorkEffectResult.Failed(refusal);
            if (!world.Services.Get<IResourceTransaction>().CommitInventory(Intent(order)).Committed)
                throw new InvalidOperationException("completion inventory changed after preflight");
            if (_mode == "mutating_pending") return WorkEffectResult.Pending(ResourceTransactions.ActivePromises);
            if (_mode == "post_claim")
            {
                ResourceAmount line = _bill[0];
                if (!world.Entities.Get(order.Target).Get<IInventory>().ReserveIn(new ReservationOwner("proof.completion", order.Id), line.Resource, line.Amount).IsValid)
                    throw new InvalidOperationException("completion claim changed after production");
            }
            return WorkEffectResult.Applied;
        }
    }

    // A module chooses its current authored outcome; Upgrades alone commits replacement.
    public sealed class OutcomeReplacementEffectDef : Def, IPhysicalWorkEffect, IWorkEffectIntentAdmission, IEntityReplacementEffect
    {
        private string _source, _empty, _stocked, _resource;
        private EntityDef _sourceDefinition, _emptyDefinition, _stockedDefinition;
        private ResourceId _selector;
        public IReadOnlyList<IContentDefinition> ContentDependencies { get; private set; }
        protected override void ReadCore(JsonReader reader)
        { _source = reader.String("source"); _empty = reader.String("empty"); _stocked = reader.String("stocked"); _resource = reader.String("resource"); }
        protected override void ResolveCore(ContentResolver content)
        {
            _sourceDefinition = content.Def<EntityDef>(_source); _emptyDefinition = content.Def<EntityDef>(_empty);
            _stockedDefinition = content.Def<EntityDef>(_stocked); _selector = content.Resource(_resource);
            var references = new List<IContentDefinition> { _sourceDefinition };
            foreach (IContentDefinition definition in new IContentDefinition[] { _emptyDefinition, _stockedDefinition, content.Content.Resource(_selector) })
                if (!references.Contains(definition)) references.Add(definition);
            ContentDependencies = references.AsReadOnly();
        }
        public bool AcceptsSourceDefinition(EntityDef definition) => ReferenceEquals(definition, _sourceDefinition);
        private EntityDef Outcome(Entity source) => source.Get<IInventory>().Amount(_selector).IsPositive ? _stockedDefinition : _emptyDefinition;
        public Code CheckReplacement(World world, Entity source, EntityDef destination)
            => AcceptsSourceDefinition(source.Def) && ReferenceEquals(destination, Outcome(source)) ? Code.None : StandardCodes.WrongTarget;
        public void RequireCapabilities(World world) { }
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
        {
            PhysicalWorkEffectContracts.ValidatePlan(world, this, plan, node);
            if (node != null || plan is IEntityActivityDefinition targeted && !AcceptsSourceDefinition(targeted.TargetDefinition))
                throw new ContentException(Id + ": outcome replacement requires whole-order work on its declared source");
        }
        public Code CheckIntent(World world, PlayerId company, Entity target)
            => target == null || !ReferenceEquals(world.Entities.Get(target.Id), target) ? Codes.NotFound :
                target.Owner != company ? Codes.NotOwned : AcceptsSourceDefinition(target.Def) ? Code.None : StandardCodes.WrongTarget;
        public Code CheckStart(World world, PlayerId company, Entity target) => CheckIntent(world, company, target);
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node) => world.Entities.Get(order.Target);
        public Code CheckSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
        { Code check = CheckIntent(world, order.Owner, target); return check.IsNone ? Upgrades.Check(world, target, Outcome(target)) : check; }
        public Code CommitSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => Upgrades.TryReplace(world, target, Outcome(target), out _);
        public Code Check(World world, Order order, IActivityNodeDefinition node) => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node) => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }

    public sealed class CompletionEffectModule : GameModule
    {
        public override string Id => "proof.completion";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(WorkModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        { types.Section<CompletionEffectDef>("completion_effects"); types.Section<OutcomeReplacementEffectDef>("outcome_replacement_effects"); }
    }
}
