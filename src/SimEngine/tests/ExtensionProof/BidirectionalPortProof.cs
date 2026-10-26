using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Registration and immutable alias checks only; native inventory/hauling owners execute gameplay.
    public sealed class BidirectionalPortModule : GameModule
    {
        public override string Id => "proof.bidirectional-ports";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InventoryModule.ModuleId, Version) };
        public readonly InitializeProbe Initialize = new InitializeProbe();
        public bool ActualAliases { get; private set; }
        public override void DeclareContent(ContentTypes types) => types.Component<IndependentStorageDef>("independent_storage", 4);
        public override void Compose(WorldComposer composer)
        {
            ActualAliases = true;
            foreach (string name in new[] { "source", "receiver", "smelter", "assembler", "carrier", "cargo2" })
            {
                EntityDef entity = composer.Content.Get<EntityDef>("space." + name);
                var inventory = entity.Component<IInventoryDefinition>();
                ActualAliases &= inventory is IStorageDefinition storage && ReferenceEquals(storage.Content, composer.Content)
                    && inventory.ComponentType == typeof(Storage) && entity.HasCapability<IOutletPort>() && entity.HasCapability<IInletPort>()
                    && ReferenceEquals(inventory, entity.Component<IInletPortDefinition>());
            }
            if (!ActualAliases) throw new ContentException("bidirectional fixture requires actual shared inventory aliases");
            composer.AddSystem(Initialize, 301);
        }
    }
}
