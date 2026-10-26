using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // A game-module eligibility declaration, not a seller, account, transfer or scheduler.
    public sealed class InventorySaleEligibilityDef : Def, IResourceSaleEligibility
    {
        public IReadOnlyList<IContentDefinition> ContentDependencies => Array.Empty<IContentDefinition>();
        protected override void ReadCore(JsonReader reader) { }
        public void ValidateTarget(EntityDef target)
        {
            if (target == null || !target.HasCapability<IInventory>())
                throw new ContentException("inventory sale eligibility requires its actual inventory target");
        }
        public Code Check(World world, Entity entity)
            => entity != null && entity.Alive && ReferenceEquals(world.Entities.Get(entity.Id), entity) && entity.Get<IInventory>() != null
                ? Code.None : Codes.NotFound;
    }
    public sealed class InventorySaleEligibilityModule : GameModule
    {
        public override string Id => "proof.resource-commerce";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {
            new ModuleRequirement(TycoonModule.ModuleId, Version), new ModuleRequirement(ResourceFlowsModule.ModuleId, Version)
        });
        public override void DeclareContent(ContentTypes types)
            => types.Section<InventorySaleEligibilityDef>("inventory_sale_eligibilities");
    }
}
