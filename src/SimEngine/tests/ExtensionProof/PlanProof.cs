using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed partial class PointSpace : ISpatialConstructionPlacement, ISpatialRecoveryPlacement, ISpatialPlacementTemplates
    {
        // Fault instrumentation only; never authored gameplay, current time or serialized state.
        public Action PlacementReadFault, PlacementCaptureFault;
        public SpatialPlacement ReadPlacement(JsonValue arguments)
        {
            PlacementReadFault?.Invoke();
            var reader = new JsonReader(arguments, Id + ".placement");
            string space = reader.String("space"); Fix position = reader.Fix("position"); reader.End();
            if (space != Id) throw reader.Error("placement names a different selected space");
            return new PointPlacement(Id, position);
        }
        public JsonValue WritePlacement(SpatialPlacement placement)
        {
            if (!(placement is PointPlacement point) || point.Space != Id) throw new ContentException("incompatible point placement serialization");
            return TemplatePosition("position", point.Position);
        }
        public string PlacementIdentity(SpatialPlacement placement)
        {
            if (!(placement is PointPlacement point) || point.Space != Id) throw new ContentException("incompatible point placement identity");
            return Id + ":" + point.Position.Raw.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        private Fix RelativePlacement(JsonValue relative)
        {
            var reader = new JsonReader(relative, Id + ".relative-placement"); string space = reader.String("space");
            Fix offset = reader.Fix("offset"); reader.End(); if (space != Id) throw reader.Error("template names another space"); return offset;
        }
        public void ValidateTemplatePlacement(JsonValue relative) => RelativePlacement(relative);
        private JsonValue TemplatePosition(string key, Fix value)
        { var result = JsonValue.NewObject(); result.Set("space", JsonValue.Of(Id)); result.Set(key, JsonValue.Number(value.ToExactString())); return result; }
        public JsonValue CaptureTemplatePlacement(JsonValue placement, JsonValue origin)
            => TemplatePosition("offset", ((PointPlacement)ReadPlacement(placement)).Position - ((PointPlacement)ReadPlacement(origin)).Position);
        public JsonValue PlaceTemplatePlacement(JsonValue relative, JsonValue anchor)
            => TemplatePosition("position", ((PointPlacement)ReadPlacement(anchor)).Position + RelativePlacement(relative));
        public Code CheckConstructionDefinition(EntityDef target, EntityDef site)
        {
            GeometryObservationFault?.Invoke("construction-definition", null);
            return target?.Component<ISpatialPresenceDefinition>()?.Space == Id && site?.Component<ISpatialPresenceDefinition>()?.Space == Id ? Code.None : Codes.InvalidArgument;
        }
        public Code CheckRecoveryDefinition(EntityDef container)
        {
            GeometryObservationFault?.Invoke("recovery-definition", null);
            return container?.Component<ISpatialPresenceDefinition>()?.Space == Id && !container.HasCapability<IMotion>() ? Code.None : Codes.InvalidArgument;
        }
    }
    /// <summary>A normal authored provider, compiled only against public framework contracts.</summary>
    public sealed class PlannedInspectionDef : ActivityDef
    {
        private string _target;
        private string _activationFault;
        private EntityDef _definition;
        public static readonly Code WrongSite = new Code("proof.plan-wrong-site");
        protected override void ReadCore(JsonReader reader)
        {
            base.ReadCore(reader); _target = reader.String("admission_target"); _activationFault = reader.String("activation_fault", null);
            if (_activationFault != null && _activationFault != "wrong-target" && _activationFault != "extra-order")
                throw reader.Error("unknown activation fault probe");
        }
        protected override void ResolveCore(ContentResolver resolver)
        { base.ResolveCore(resolver); _definition = resolver.Def<EntityDef>(_target); }
        public override Code CheckIntent(World world, PlayerId company, Entity target)
        {
            Code check = base.CheckIntent(world, company, target);
            return !check.IsNone ? check : ReferenceEquals(target.Def, _definition) ? Code.None : WrongSite;
        }
        public override void ValidateTargetDefinition(EntityDef target)
        {
            base.ValidateTargetDefinition(target);
            if (target != null && !ReferenceEquals(target, _definition)) throw new ContentException("planned inspection needs its authored target prototype");
        }
        public override Order Activate(World world, PlayerId company, JsonValue arguments, int priority)
        {
            Order order = base.Activate(world, company, arguments, priority);
            if (_activationFault == "extra-order") world.Services.Get<WorkBoard>().Post(this, company, order.Target, priority);
            if (_activationFault == "wrong-target")
                foreach (Entity entity in world.Entities.All)
                    if (entity.Alive && entity.Owner == company && entity.Id != order.Target)
                    { order.Target = entity.Id; break; }
            return order;
        }
    }
    public sealed class PlanProofModule : GameModule
    {
        public override string Id => "proof.plans";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(WorkModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<PlannedInspectionDef>("planned_inspections");
            types.Component<ProofRecurringSourceDef>("proof_plan_recurring", 6);
            types.Component<ProofPlanHistoryRetentionDef>("proof_plan_history", 2);
        }
    }
}
