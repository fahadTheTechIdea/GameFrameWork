using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentInventoryWorkEffectDef : Def, IInventoryWorkEffectDefinition
    {
        private InventoryWorkEffectDeclaration _declaration;
        public Bill Quantities => _declaration.Quantities;
        public IReadOnlyList<ResourcePortRoute> Ports => _declaration.Ports;
        public ResourceFlowKind Kind => _declaration.Kind;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        protected override void ReadCore(JsonReader reader) => _declaration = InventoryWorkEffectDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void RequireCapabilities(World world) => InventoryWorkEffectContracts.RequireCapabilities(world, this);
        public Code CheckIntent(World world, PlayerId company, Entity target) => InventoryWorkEffectContracts.CheckIntent(world, this, company, target);
        public Code CheckStart(World world, PlayerId company, Entity target) => InventoryWorkEffectContracts.CheckStart(world, this, company, target);
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node) => world.Entities.Get(order.Target);
        public Code CheckSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => InventoryWorkEffectContracts.Check(world, this, order.Owner, target, new ReservationOwner("sim.work", order.Id));
        public Code CommitSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => InventoryWorkEffectContracts.Commit(world, this, order.Owner, target, new ReservationOwner("sim.work", order.Id));
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node) => PhysicalWorkEffectContracts.ValidatePlan(world, this, plan, node);
        public Code Check(World world, Order order, IActivityNodeDefinition node) => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node) => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }
    public sealed class InventoryWorkProofModule : GameModule
    {
        public override string Id => "proof.inventory-work";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Section<IndependentInventoryWorkEffectDef>("independent_inventory_effects");
    }
}
