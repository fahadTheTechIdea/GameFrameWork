using Beep.SimEngine;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent geometry binding only. Resource stock, promises, transaction and work rules are framework-owned.
    public sealed class PointResourceCellDef : Def, ISpatialResourceCellDefinition
    {
        private SpatialResourceCellDeclaration _declaration;
        public long Identity => _declaration.Identity;
        public bool Public => _declaration.Public;
        public ISpatialLayerDefinition Layer => _declaration.Layer;
        public ICapacityPolicy Capacity => _declaration.Capacity;
        public Fix Point { get; private set; }
        protected override void ReadCore(JsonReader reader) { _declaration = SpatialResourceCellDeclaration.Read(reader); Point = reader.Fix("point"); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content)
        {
            _declaration.ValidateResolved(content);
            if (!(Layer.Space is PointSpaceDef space) || Point < Fix.Zero || Point > space.Extent) throw content.Fail("point cell is outside its authored space");
        }
        private ISpatialResourceQuantity<Fix> Quantity(World world)
        {
            if (!(world.Space is ISpatialLayerProvider provider) || world.Space.Id != Layer.Space.Id)
                throw new ContentException("point cell requires its actual selected spatial layer provider");
            ISpatialResourceQuantity<Fix> quantity = null;
            foreach (var layer in provider.SpatialLayers)
                if (layer.Id == Layer.Id) quantity = layer as ISpatialResourceQuantity<Fix>;
            if (quantity == null) throw new ContentException("selected point space has no actual quantity owner");
            return quantity;
        }
        public void ValidateComposition(World world) { _ = Quantity(world).ReadQuantity(Point); }
        public IResourceOutlet Bind(World world, ResourceEndpointId endpoint) => _declaration.Bind(world, endpoint, Quantity(world), Point);
        public Code CheckSite(World world, Entity target)
        {
            return target.Get<ISpatialPresence>()?.Capture() is PointSnapshot point && point.Space == Layer.Space.Id && point.Position == Point && !target.Has<IMobility>()
                ? Code.None : Codes.InvalidArgument;
        }
    }
    public sealed class PointSpatialResourcesModule : SpatialResourcesModule
    {
        public override void DeclareContent(ContentTypes types)
        { base.DeclareContent(types); types.Section<PointResourceCellDef>("point_resource_cells"); }
    }
}
