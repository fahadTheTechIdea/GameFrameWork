using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface ITransformationDispatchEffect : IPhysicalWorkEffect
    {
        ITransformationDefinition Transformation { get; }
        Code Capability { get; }
        int Count { get; }
        bool Repeat { get; }
        bool HomeIsTarget { get; }
    }

    /// <summary>One strict authored dispatch declaration for native and independent definitions.</summary>
    public sealed class TransformationDispatchDeclaration
    {
        private ResolvedContent _content;
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies
            => TransformationContracts.ContentReferences(_content, Transformation);
        private readonly string _transformation;
        public ITransformationDefinition Transformation { get; private set; }
        public Code Capability { get; }
        public int Count { get; }
        public bool Repeat { get; }
        public bool HomeIsTarget { get; }
        private TransformationDispatchDeclaration(string transformation, Code capability, int count, bool repeat, bool home)
        { _transformation = transformation; Capability = capability; Count = count; Repeat = repeat; HomeIsTarget = home; }
        public static TransformationDispatchDeclaration Read(JsonReader reader)
        {
            string transformation = reader.String("transformation"); Code capability = new Code(reader.String("capability"));
            if (string.IsNullOrWhiteSpace(transformation) || capability.IsNone) throw reader.Error("dispatch requires explicit transformation and named capability");
            int count = reader.Int("count"); bool repeat = reader.Bool("repeat"); string home = reader.String("home");
            if (count < 1 || repeat && count != 1) throw reader.Error("dispatch count must be positive; repeat represents one recurring queue entry");
            if (home != "none" && home != "target") throw reader.Error("dispatch home must explicitly be none or target");
            return new TransformationDispatchDeclaration(transformation, capability, count, repeat, home == "target");
        }
        public void Resolve(ContentResolver resolver)
        {
            _content = resolver.Content;
            Transformation = resolver.Def<ITransformationDefinition>(_transformation);
            if (Transformation.Mode == TransformationMode.Continuous && Repeat)
                throw resolver.Fail("continuous dispatch cannot repeat");
        }
    }

    /// <summary>Dispatch owns no queue, time or output; it delegates to the selected native queue contract.</summary>
    public static class TransformationDispatchContracts
    {
        public static void ValidateDefinition(World world, ITransformationDispatchEffect effect)
        {
            if (!world.Services.Has<ITransformationQueue>()) throw new ContentException(effect.Id + ": dispatch requires declared ITransformationQueue");
            if (!world.Services.Has<ITransformationOperationQueue>()) throw new ContentException(effect.Id + ": dispatch requires declared ITransformationOperationQueue");
            var recipe = effect.Transformation;
            if (recipe == null || !world.Content.Owns(recipe) || !ReferenceEquals(world.Content.Find<ITransformationDefinition>(recipe.Id), recipe) ||
                effect.Capability.IsNone || effect.Count < 1 || effect.Repeat && (effect.Count != 1 || recipe.Mode == TransformationMode.Continuous))
                throw new ContentException(effect.Id + ": invalid actual transformation dispatch metadata");
        }
        public static void ValidatePlan(World world, ITransformationDispatchEffect effect, IActivityDefinition plan, IActivityNodeDefinition node)
        {
            ValidateDefinition(world, effect);
            PhysicalWorkEffectContracts.ValidatePlan(world, effect, plan, node);
            if (!(plan is IEntityActivityDefinition targeted)) return;
            ITransformerDefinition provider = targeted.TargetDefinition.Component<ITransformerDefinition>(effect.Capability);
            if (provider != null)
                foreach (ITransformationDefinition definition in provider.Transformations)
                    if (ReferenceEquals(definition, effect.Transformation)) return;
            throw new ContentException(plan.PlanKey + ": dispatch target must declare the named supported transformation");
        }
        private static EntityId Home(ITransformationDispatchEffect effect, Entity target) => effect.HomeIsTarget ? target.Id : EntityId.None;
        public static Code CheckIntent(World world, ITransformationDispatchEffect effect, PlayerId company, Entity target)
            => CheckTarget(world, effect, company, target, true);
        public static Code CheckStart(World world, ITransformationDispatchEffect effect, PlayerId company, Entity target)
            => CheckTarget(world, effect, company, target, false);
        private static Code CheckTarget(World world, ITransformationDispatchEffect effect, PlayerId company, Entity target, bool playerIntent)
        {
            if (target == null || !ReferenceEquals(world.Entities.Get(target.Id), target)) return Codes.NotFound;
            if (target.Owner != company) return Codes.NotOwned;
            return playerIntent ? world.Services.Get<ITransformationQueue>().CheckEnqueue(company, target.Id, effect.Capability,
                effect.Transformation.Id, effect.Count, effect.Repeat, Home(effect, target)) :
                world.Services.Get<ITransformationOperationQueue>().CheckEnqueueOperation(company, target.Id, effect.Capability,
                effect.Transformation.Id, effect.Count, effect.Repeat, Home(effect, target));
        }
        public static Code Commit(World world, ITransformationDispatchEffect effect, PlayerId company, Entity target)
            => world.Services.Get<ITransformationOperationQueue>().EnqueueOperation(company, target.Id, effect.Capability,
                effect.Transformation.Id, effect.Count, effect.Repeat, Home(effect, target));
    }

    /// <summary>Earned activity dispatches ordinary physical production, never creates its outputs.</summary>
    public sealed class TransformationDispatchEffectDef : Def, ITransformationDispatchEffect, IWorkEffectIntentAdmission
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        private TransformationDispatchDeclaration _declaration;
        public ITransformationDefinition Transformation => _declaration.Transformation;
        public Code Capability => _declaration.Capability;
        public int Count => _declaration.Count;
        public bool Repeat => _declaration.Repeat;
        public bool HomeIsTarget => _declaration.HomeIsTarget;
        protected override void ReadCore(JsonReader reader) => _declaration = TransformationDispatchDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver resolver) => _declaration.Resolve(resolver);
        public void RequireCapabilities(World world) => TransformationDispatchContracts.ValidateDefinition(world, this);
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node) => world.Entities.Get(order.Target);
        public Code CheckSubject(World world, Entity target, Order order, IActivityNodeDefinition node) => CheckStart(world, order.Owner, target);
        public Code CommitSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => TransformationDispatchContracts.Commit(world, this, order.Owner, target);
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
            => TransformationDispatchContracts.ValidatePlan(world, this, plan, node);
        public Code CheckIntent(World world, PlayerId company, Entity target)
            => TransformationDispatchContracts.CheckIntent(world, this, company, target);
        public Code CheckStart(World world, PlayerId company, Entity target) => TransformationDispatchContracts.CheckStart(world, this, company, target);
        public Code Check(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }
}
