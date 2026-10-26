using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>An ordinary authored graph on one exact prototype, with portable target intent.</summary>
    public interface IEntityActivityDefinition : IEntityTargetPlanDefinition, IPlayerActivityDefinition, IPlanTemplateDefinition, IContentDefinition
    {
        EntityDef TargetDefinition { get; }
    }

    /// <summary>No execution store: the existing ActivityDef/WorkBoard owns this graph and its work.</summary>
    public sealed class EntityActivityDef : ActivityDef, IEntityActivityDefinition
    {
        private string _target;
        private System.Collections.Generic.IReadOnlyList<IContentDefinition> _disclosure;
        public EntityDef TargetDefinition { get; private set; }
        public override System.Collections.Generic.IReadOnlyList<IContentDefinition> DisclosureContent
            => _disclosure;
        protected override void ReadCore(JsonReader reader)
        { base.ReadCore(reader); _target = reader.String("target"); }
        protected override void ResolveCore(ContentResolver content)
        { base.ResolveCore(content); TargetDefinition = content.Def<EntityDef>(_target); }
        protected override void ValidateResolvedCore(ContentResolver content)
        {
            base.ValidateResolvedCore(content); ValidateTargetDefinition(TargetDefinition);
            var effect = content.OptionalDef<IWorkEffect>(OnComplete);
            if (effect is IEntityReplacementEffect replacement &&
                !replacement.AcceptsSourceDefinition(TargetDefinition))
                throw content.Fail(PlanKey + ": replacement source must match the authored activity target");
            var references = new System.Collections.Generic.List<IContentDefinition> { this, TargetDefinition };
            _disclosure = references.AsReadOnly();
        }
        public override void ValidateTargetDefinition(EntityDef target)
        {
            base.ValidateTargetDefinition(target ?? TargetDefinition);
            if (target != null && !ReferenceEquals(target, TargetDefinition))
                throw new ContentException(PlanKey + ": target must match the authored entity prototype");
        }
        public override Code CheckIntent(World world, PlayerId company, Entity target)
        {
            if (!ReferenceEquals(target.Def, TargetDefinition)) return StandardCodes.WrongTarget;
            return base.CheckIntent(world, company, target);
        }
        public void ValidateTemplate(World world, JsonValue arguments)
        { ValidateTargetDefinition(null); EntityTargetPlanTemplates.Validate(world, arguments, PlanKey); }
        public void ValidateTemplateAnchor(World world, JsonValue arguments, JsonValue anchor)
        { ValidateTemplate(world, arguments); EntityTargetPlanTemplates.ValidateAnchor(world, arguments, anchor, PlanKey); }
        public Code CaptureTemplate(World world, PlayerId company, JsonValue arguments, JsonValue origin, out JsonValue template)
            => EntityTargetPlanTemplates.Capture(world, company, TargetDefinition, Graph, PlanKey, arguments, origin, out template);
        public Code PlaceTemplate(World world, PlayerId company, JsonValue arguments, JsonValue anchor, out JsonValue intent)
        { ValidateTemplate(world, arguments); return EntityTargetPlanTemplates.Place(world, company, TargetDefinition, PlanKey, arguments, anchor, out intent); }
    }

    /// <summary>Stateless existing-target template protocol; selected providers own all geometry.</summary>
    internal static class EntityTargetPlanTemplates
    {
        internal static JsonValue PlacementArguments(JsonValue arguments, string key)
        {
            var reader = new JsonReader(arguments, key + ".intent");
            JsonReader placement = reader.Child("placement") ?? throw reader.Error("placement is required");
            reader.End(); return placement.Raw;
        }
        private static ISpatialPlacementTemplates Space(World world)
            => world.Space as ISpatialPlacementTemplates ?? throw new ContentException("selected space does not support portable placement templates");
        internal static void Validate(World world, JsonValue arguments, string key)
            => Space(world).ValidateTemplatePlacement(PlacementArguments(arguments, key));
        internal static void ValidateAnchor(World world, JsonValue arguments, JsonValue anchor, string key)
        {
            var provider = Space(world);
            provider.ReadPlacement(provider.PlaceTemplatePlacement(PlacementArguments(arguments, key), anchor));
        }
        internal static Code Capture(World world, PlayerId company, EntityDef prototype, IActivityGraph graph,
            string key, JsonValue arguments, JsonValue origin, out JsonValue template)
        {
            template = null;
            var selection = ActivityWorkerSelection.Read(arguments, graph, key + ".intent", out EntityId target);
            if (selection.Bindings.Count != 0) return PlanCodes.LiveBindings;
            Entity entity = world.Entities.Get(target); if (entity == null) return Codes.NotFound;
            if (entity.Owner != company) return Codes.NotOwned;
            if (!ReferenceEquals(entity.Def, prototype)) return StandardCodes.WrongTarget;
            var provider = Space(world); var scope = EntityObservation.Capture(world, entity);
            try
            {
                SpatialPlacement placement = provider.CapturePlacement(entity);
                if (placement == null || placement.Space != world.Space.Id) return Codes.InvalidArgument;
                JsonValue absolute = provider.WritePlacement(placement);
                if (provider.PlacementIdentity(provider.ReadPlacement(absolute)) != provider.PlacementIdentity(placement))
                    throw new InvalidOperationException("placement serialization changed the actual activity target geometry");
                template = JsonValue.NewObject(); template.Set("placement", provider.CaptureTemplatePlacement(absolute, origin));
                return Code.None;
            }
            finally { scope.Verify(); }
        }
        internal static Code Place(World world, PlayerId company, EntityDef prototype, string key,
            JsonValue arguments, JsonValue anchor, out JsonValue intent)
        {
            intent = null; var provider = Space(world);
            JsonValue placement = provider.PlaceTemplatePlacement(PlacementArguments(arguments, key), anchor);
            Code check = EntityPlacementTargets.Find(world, company, prototype, placement, out EntityId target);
            if (!check.IsNone) return check;
            intent = ActivityWorkerSelection.CreateArguments(target, ActivityWorkerSelection.Unbound); return Code.None;
        }
    }
}
