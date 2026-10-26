using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Public content providers; no second game loop, objective store or codec.</summary>
    public class ObjectiveProofModule : ObjectiveModule
    {
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Section<StockObjectiveProofDef>("proof_stock_objective_predicates");
            types.Section<GatedObjectiveProofEffectDef>("proof_gated_objective_effects");
            types.Section<MutatingObjectiveProofDef>("proof_mutating_objective_predicates");
            types.Section<MutatingObjectiveProofEffectDef>("proof_mutating_objective_effects");
        }
    }
    /// <summary>Deliberate observation fault injection, never a player gameplay implementation.</summary>
    public sealed class MutatingObjectiveProofDef : ObjectivePredicateDef
    {
        public override IReadOnlyList<IContentDefinition> ObservationContent => Array.Empty<IContentDefinition>();
        protected override void ReadCore(JsonReader reader) { }
        public override ObjectiveObservation Observe(ObjectiveContext context, IReadOnlyDictionary<string, ObjectiveObservation> children)
        { ((Beep.SimEngine.Hosting.GameClock)context.World.Clock).Pause(); return ObjectiveObservation.Complete; }
    }
    /// <summary>Check must not change clock authority; Apply must never be reached after this fault.</summary>
    public sealed class MutatingObjectiveProofEffectDef : ObjectiveEffectDef
    {
        protected override void ReadCore(JsonReader reader) { }
        public override Code Check(ObjectiveContext context)
        { ((Beep.SimEngine.Hosting.GameClock)context.World.Clock).Pause(); return Code.None; }
        public override void Apply(ObjectiveContext context) => context.World.End(new Code("proof.bad-effect"));
    }
    public sealed class StockObjectiveProofDef : ObjectivePredicateDef
    {
        public override IReadOnlyList<IContentDefinition> ObservationContent => Array.AsReadOnly(new IContentDefinition[] { Prototype });
        private string _prototype; private BillSpec _spec;
        public EntityDef Prototype { get; private set; }
        public Bill Quantities { get; private set; }
        protected override void ReadCore(JsonReader reader) { _prototype = reader.String("prototype"); _spec = BillSpec.Read(reader, "quantities"); }
        protected override void ResolveCore(ContentResolver resolver)
        { Prototype = resolver.Def<EntityDef>(_prototype); Quantities = _spec.Resolve(resolver); _spec = null;
            if (Quantities.IsEmpty) throw resolver.Fail("proof stock objective requires actual quantities"); }
        public override ObjectiveObservation Observe(ObjectiveContext context, IReadOnlyDictionary<string, ObjectiveObservation> children)
        {
            foreach (Entity entity in context.World.Entities.All)
            {
                if (!entity.Alive || entity.Owner != context.Company || !ReferenceEquals(entity.Def, Prototype)) continue;
                IInventory inventory = entity.Get<IInventory>(); if (inventory == null) continue;
                bool ready = true;
                foreach (ResourceAmount line in Quantities.Lines) ready &= inventory.Amount(line.Resource) >= line.Amount;
                if (ready) return ObjectiveObservation.Complete;
            }
            return ObjectiveObservation.Wait(ObjectiveCodes.Condition);
        }
    }
    public sealed class GatedObjectiveProofEffectDef : ObjectiveEffectDef
    {
        private string _gate; private IObjectivePredicate _predicate;
        protected override void ReadCore(JsonReader reader) => _gate = reader.String("gate");
        protected override void ResolveCore(ContentResolver resolver)
        { _predicate = resolver.Def<IObjectivePredicate>(_gate); if (_predicate.Dependencies.Count != 0) throw resolver.Fail("proof effect gate requires a leaf"); }
        public override void ValidateBinding(World world) => _predicate.ValidateBinding(world);
        public override Code Check(ObjectiveContext context)
        {
            ObjectiveObservation observation = _predicate.Observe(context, new Dictionary<string, ObjectiveObservation>());
            return observation.Satisfied ? Code.None : observation.Blocker;
        }
        public override void Apply(ObjectiveContext context)
            => context.World.Notify(new Code("proof.objective-effect"), context.Company, subject: context.Objective);
    }
}
