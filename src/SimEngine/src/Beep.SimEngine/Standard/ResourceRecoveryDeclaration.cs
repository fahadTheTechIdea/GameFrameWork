using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Strict immutable authoring only; actual stock disposition stays in ConstructionService.</summary>
    public sealed class ResourceRecoveryDeclaration
    {
        private readonly string _space, _container;
        public string Space { get; private set; }
        public EntityDef Container { get; private set; }
        private ResourceRecoveryDeclaration(JsonReader reader)
        { _space = reader.String("space"); _container = reader.String("container"); }
        public static ResourceRecoveryDeclaration Read(JsonReader reader)
            => new ResourceRecoveryDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver resolver)
        { Space = resolver.Def<ISpatialSpaceDefinition>(_space).Id; Container = resolver.Def<EntityDef>(_container); }
        public void Validate(ContentResolver resolver)
            => ValidateMetadata(resolver.Content, Space, Container);

        // The same admission for authored declarations and all actual contract bindings at composition.
        internal static void ValidateMetadata(ResolvedContent content, string space, EntityDef container)
        {
            ISpatialSpaceDefinition geometry = content.Find<ISpatialSpaceDefinition>(space);
            if (geometry == null || !content.Owns(geometry) || container == null || !content.Owns(container) ||
                !ReferenceEquals(content.Find<EntityDef>(container.Id), container) ||
                container.Component<ISpatialPresenceDefinition>()?.Space != space)
                throw new ContentException("recovery requires its actual authored space and spatial container");
            IInventoryDefinition inventory = container.Component<IInventoryDefinition>();
            if (inventory == null || !container.HasCapability<IInventory>() || inventory.InitialStock == null ||
                !inventory.InitialStock.IsEmpty || inventory.CapacityPolicy == null ||
                !CapacityPolicyContracts.Check(inventory.CapacityPolicy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>()).Fits)
                throw new ContentException(container.Id + ": recovery requires compatible empty authored inventory");
        }
    }
}
