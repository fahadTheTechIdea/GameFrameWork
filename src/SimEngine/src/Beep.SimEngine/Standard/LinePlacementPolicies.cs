using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    /// <summary>Authored pure corridor placement; no construction, geometry or stock owner.</summary>
    public interface ILinePlacementPolicy : IContentDefinition
    {
        ISpatialSpaceDefinition Space { get; }
        void ValidateBinding(World world);
        Code Check(World world, PlayerId company, SpatialPlacement placement);
    }
    /// <summary>Sole native reader, sharing existing footprint condition interpretation.</summary>
    public sealed class TileLinePlacementDeclaration
    {
        private readonly string _space;
        private readonly TileFootprintConditionDeclaration _conditions;
        public ITileSpaceDefinition Space { get; private set; }
        public Code Blocker { get; }
        private TileLinePlacementDeclaration(JsonReader reader)
        {
            _space = reader.String("space"); string blocker = reader.String("blocker");
            try { ContractIdentity.ValidateId(_space, "line placement space"); ContractIdentity.ValidateId(blocker, "line placement blocker"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            Blocker = new Code(blocker); _conditions = TileFootprintConditionDeclaration.Read(reader.Child("footprint"));
        }
        public static TileLinePlacementDeclaration Read(JsonReader reader)
            => new TileLinePlacementDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        { Space = content.Def<ITileSpaceDefinition>(_space); _conditions.Resolve(content); }
        public void Validate(ContentResolver content) => _conditions.Validate(content, Space);
        public void ValidateBinding(World world) => _conditions.ValidateBinding(world, Space);
        public Code Check(World world, PlayerId company, SpatialPlacement placement)
        {
            ValidateBinding(world);
            if (!world.Players.IsSeat(company)) return Codes.NotSeated;
            if (!(placement is TilePlacement tile) || tile.Space != Space.Id) return LineCodes.WrongSpace;
            return _conditions.Matches(world, (ITileSpatialSpace)world.Space, new TileRect(tile.Origin, 1, 1)) ? Code.None : Blocker;
        }
    }
    public sealed class TileLinePlacementPolicyDef : Def, ILinePlacementPolicy
    {
        private TileLinePlacementDeclaration _declaration;
        public ISpatialSpaceDefinition Space => _declaration.Space;
        protected override void ReadCore(JsonReader reader) => _declaration = TileLinePlacementDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.Validate(content);
        public void ValidateBinding(World world) => _declaration.ValidateBinding(world);
        public Code Check(World world, PlayerId company, SpatialPlacement placement) => _declaration.Check(world, company, placement);
    }
    public static class LinePlacementContracts
    {
        internal static void ValidateMetadata(ILinePlacementPolicy policy, ISpatialSpaceDefinition space, ContentResolver content)
        {
            if (!(policy is Def actual) || !content.Content.Owns(policy) || policy.Id != actual.Id || policy.Module != actual.Module
                || !ReferenceEquals(content.Content.Find<ILinePlacementPolicy>(actual.Id), policy)
                || !ReferenceEquals(policy.Space, space) || !content.Content.Owns(space)
                || !ReferenceEquals(content.Content.Find<ISpatialSpaceDefinition>(space.Id), space))
                throw content.Fail("line placement policy requires its actual catalog and spatial aliases");
        }
        public static Code Check(World world, ILinePlacementPolicy policy, PlayerId company, SpatialPlacement placement)
            => EntityObservation.ReadProvider(world, policy, null, () => {
                if (!(policy is Def actual) || !actual.IsFrozen || !world.Content.Owns(policy)
                    || policy.Id != actual.Id || policy.Module != actual.Module
                    || !ReferenceEquals(world.Content.Find<ILinePlacementPolicy>(actual.Id), policy))
                    throw new ContentException("actual frozen line placement policy required");
                if (policy.Space == null || !world.Content.Owns(policy.Space) || world.Space?.Id != policy.Space.Id
                    || placement == null || placement.Space != policy.Space.Id) return LineCodes.WrongSpace;
                return policy.Check(world, company, placement);
            }, world.Space);
        internal static void ValidateComposition(World world)
        {
            foreach (var policy in world.Content.All<ILinePlacementPolicy>())
                NetworkPolicyContracts.Read(world, policy, null, () => {
                    var space = policy.Space;
                    if (space == null || !world.Content.Owns(space) || !ReferenceEquals(world.Content.Find<ISpatialSpaceDefinition>(space.Id), space))
                        throw new ContentException("line placement policy requires its actual spatial alias");
                    if (space.Id == world.Space?.Id) policy.ValidateBinding(world); return true;
                });
        }
    }
}
