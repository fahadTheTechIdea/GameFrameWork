using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public static class SpatialWorkCodes
    {
        public static readonly Code OutsideRegion = new Code("work.target-outside-region");
        public static readonly Code UnsupportedRegion = new Code("work.target-region-unsupported");
    }

    /// <summary>Full target geometry must belong to the authored region. Not a route/travel restriction.</summary>
    public sealed class TargetRegionRequirementDef : WorkRequirementDef
    {
        public override System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.AsReadOnly(new IContentDefinition[] { (ISpatialRegionDefinition)Region });
        private string _region;
        public ISpatialRegion Region { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _region = reader.String("region");
            if (string.IsNullOrWhiteSpace(_region)) throw reader.Error("target region reference is required");
        }
        protected override void ResolveCore(ContentResolver content) => Region = content.Def<ISpatialRegionDefinition>(_region);
        public override WorkRequirementResult Evaluate(WorkRequirementContext context)
        {
            SpatialRegionStatus status = Region.Check(context.Target?.Get<ISpatialPresence>()?.Capture());
            if (status == SpatialRegionStatus.Inside) return WorkRequirementResult.Satisfied();
            if (status == SpatialRegionStatus.Outside) return WorkRequirementResult.Blocked(SpatialWorkCodes.OutsideRegion);
            return WorkRequirementResult.Invalid(SpatialWorkCodes.UnsupportedRegion);
        }
    }
}
