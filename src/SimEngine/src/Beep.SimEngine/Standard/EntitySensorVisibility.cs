using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Actual live geometry restriction on the same prototype/content visibility target.</summary>
    public sealed class EntitySensorVisibilityDef : SensorVisibilityDef<EntityDef>, IVisibilityEntityTarget
    {
        protected override string TargetSpace => Target.Component<ISpatialPresenceDefinition>()?.Space;
        protected override void ValidateResolvedCore(ContentResolver content)
        {
            if (string.IsNullOrEmpty(TargetSpace) || !Target.HasCapability<ISpatialPresence>())
                throw content.Fail("entity sensor visibility requires actual spatial prototype metadata/runtime");
        }
        public bool AllowsEntity(World world, PlayerId company, Entity entity)
        {
            if (entity == null || !entity.Alive || !ReferenceEquals(entity.Def, Target) || !ReferenceEquals(world.Entities.Get(entity.Id), entity)) return false;
            var presence = entity.Get<ISpatialPresence>();
            if (presence == null || presence.Space != TargetSpace) return false;
            return Covers(world, company, EntityObservation.ReadSpatial(world, entity));
        }
    }
}
