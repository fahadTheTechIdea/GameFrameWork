using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum WorkEffectStatus : byte { Applied = 0, Pending = 1, Failed = 2 }

    /// <summary>Pending/failed providers leave gameplay state unchanged; Applied commits exactly once.</summary>
    public readonly struct WorkEffectResult
    {
        public WorkEffectStatus Status { get; }
        public Code Blocker { get; }
        private WorkEffectResult(WorkEffectStatus status, Code blocker)
        {
            if (blocker.IsNone) throw new ArgumentException("a refused effect requires an explicit blocker", nameof(blocker));
            Status = status; Blocker = blocker;
        }
        public static WorkEffectResult Applied => default;
        public static WorkEffectResult Pending(Code blocker) => new WorkEffectResult(WorkEffectStatus.Pending, blocker);
        public static WorkEffectResult Failed(Code blocker) => new WorkEffectResult(WorkEffectStatus.Failed, blocker);
    }

    /// <summary>An authored, engine-side completion effect. Refusals must leave state unchanged.</summary>
    public interface IWorkEffect : IContentDependencyDefinition
    {
        WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node);
    }

    /// <summary>Optional pure effect diagnostics. No mutation or guessed success for other providers.</summary>
    public interface IWorkEffectPreview : IWorkEffect
    {
        Code Check(World world, Order order, IActivityNodeDefinition node);
    }

    /// <summary>Optional effect-specific admission within the sole work-plan validator.</summary>
    public interface IWorkEffectPlanValidator : IWorkEffect
    {
        void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node);
    }

    /// <summary>Pure structural intent and current start admission for the same declared final effect.</summary>
    public interface IWorkEffectIntentAdmission : IWorkEffect
    {
        Code CheckIntent(World world, PlayerId company, Entity target);
        Code CheckStart(World world, PlayerId company, Entity target);
    }

    /// <summary>Pure declared-source and current-outcome admission; execution remains ordinary whole-order physical work.</summary>
    public interface IEntityReplacementEffect : IWorkEffect
    {
        bool AcceptsSourceDefinition(EntityDef definition);
        Code CheckReplacement(World world, Entity source, EntityDef destination);
    }

    public static class WorkEffectCodes
    {
        public static readonly Code NoPreview = new Code("work.effect-no-preview");
        public static readonly Code NoStock = new Code("work.effect-no-stock");
        public static readonly Code IncompatibleInventory = new Code("work.effect-incompatible-inventory");
        public static readonly Code NotComplete = new Code("work.effect-not-complete");
        public static readonly Code MovingSource = new Code("work.recovery-moving-source");
        public static readonly Code BusySource = new Code("work.recovery-busy-source");
    }

    /// <summary>Physical effect callbacks; no definition inheritance is required.</summary>
    public interface IPhysicalWorkEffect : IWorkEffectPreview, IWorkEffectPlanValidator
    {
        void RequireCapabilities(World world);
        Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node);
        Code CheckSubject(World world, Entity subject, Order order, IActivityNodeDefinition node);
        Code CommitSubject(World world, Entity subject, Order order, IActivityNodeDefinition node);
    }

    /// <summary>The sole physical admission/preview/earned completion algorithm.</summary>
    public static class PhysicalWorkEffectContracts
    {
        public static void ValidatePlan(World world, IPhysicalWorkEffect effect, IActivityDefinition plan, IActivityNodeDefinition node)
        {
            effect.RequireCapabilities(world);
            if (!plan.Graph.HasMachineWork(node?.Id))
                throw new ContentException(plan.PlanKey + ": effect '" + effect.Id + "' requires positive-time machine work in its dependency ancestry");
        }
        private static T Observe<T>(World world, object provider, Entity target, Func<T> read)
        {
            var codec = EntityObservation.CaptureProvider(world, target, target.Sync, "work.effect");
            T result = EntityObservation.ReadProvider(world, provider, target, read, world.Space);
            codec.Verify(); return result;
        }
        private static Code CheckSubject(World world, IPhysicalWorkEffect effect, Order order, IActivityNodeDefinition node, out Entity subject)
        {
            subject = null;
            if (order == null || !ReferenceEquals(world.Services.Get<WorkBoard>().Order(order.Id), order)) return Codes.NotFound;
            Entity target = world.Entities.Get(order.Target);
            if (target == null) return Codes.NotFound;
            if (target.Owner != order.Owner) return Codes.NotOwned;
            Code binding = ResponseWork.CheckActiveOrder(world, target, order);
            if (!binding.IsNone) return binding;
            if (node == null ? order.Plan.OnComplete != effect.Id : !ReferenceEquals(order.Plan.Graph.Find(node.Id), node) || node.Effect != effect.Id)
                return WorkEffectCodes.NotComplete;
            Entity completed = node == null ? null : world.Entities.Get(order.Node(node.Id)?.CompletedBy ?? EntityId.None);
            var completedFacts = completed == null ? EntityObservation.Capture(world) : EntityObservation.Capture(world, completed);
            var completedCodec = EntityObservation.CaptureProvider(world, completed,
                completed == null ? (Action<IStateSync>)null : completed.Sync, "work.effect");
            target = Observe(world, effect, target, () => effect.ResolveSubject(world, order, node));
            completedFacts.Verify(); completedCodec.Verify();
            if (target == null || !target.Alive) return Codes.NotFound;
            if (!ReferenceEquals(world.Entities.Get(target.Id), target)) throw new InvalidOperationException("effect subject must be an actual session entity");
            if (target.Owner != order.Owner) return Codes.NotOwned;
            Entity actual = target;
            Code result = Observe(world, effect, actual, () => effect.CheckSubject(world, actual, order, node));
            if (result.IsNone) subject = actual;
            return result;
        }
        public static Code Check(World world, IPhysicalWorkEffect effect, Order order, IActivityNodeDefinition node)
            => CheckSubject(world, effect, order, node, out _);
        public static WorkEffectResult Apply(World world, IPhysicalWorkEffect effect, Order order, IActivityNodeDefinition node)
        {
            Code refusal = CheckSubject(world, effect, order, node, out var subject);
            if (!refusal.IsNone) return WorkEffectContracts.Outcome(world, refusal);
            if (node == null)
            {
                if (order.State != OrderState.Completing || order.NodesDone != order.Nodes.Count) return WorkEffectResult.Failed(WorkEffectCodes.NotComplete);
            }
            else if (!order.IsActive || order.Node(node.Id).State != NodeState.Completing) return WorkEffectResult.Failed(WorkEffectCodes.NotComplete);
            var facts = EntityObservation.Capture(world, subject);
            var codec = EntityObservation.CaptureProvider(world, subject, subject.Sync, "work.effect");
            var spatial = EntityObservation.CaptureSpace(world, world.Space);
            Code result = effect.CommitSubject(world, subject, order, node);
            if (!result.IsNone)
            {
                facts.Verify(); codec.Verify();
                spatial.Verify();
            }
            return WorkEffectContracts.Outcome(world, result);
        }
    }

    /// <summary>Authored physical retirement delegates every removal guard to the sole kernel owner.</summary>
    public sealed class EntityRemovalEffectDef : Def, IPhysicalWorkEffect, IWorkEffectIntentAdmission
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.AsReadOnly(new IContentDefinition[] { SourceDefinition });
        private string _source;
        public EntityDef SourceDefinition { get; private set; }
        protected override void ReadCore(JsonReader reader) => _source = reader.String("source");
        protected override void ResolveCore(ContentResolver content) => SourceDefinition = content.Def<EntityDef>(_source);
        public void RequireCapabilities(World world) { }
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
        {
            PhysicalWorkEffectContracts.ValidatePlan(world, this, plan, node);
            if (node != null) throw new ContentException(plan.PlanKey + ": removal is a whole-order effect, never a node effect");
            if (plan is IEntityActivityDefinition targeted && !ReferenceEquals(targeted.TargetDefinition, SourceDefinition))
                throw new ContentException(plan.PlanKey + ": removal source must match the authored activity target");
        }
        public Code CheckIntent(World world, PlayerId company, Entity target)
        {
            if (target == null || !target.Alive || !ReferenceEquals(world.Entities.Get(target.Id), target)) return Codes.NotFound;
            if (target.Owner != company) return Codes.NotOwned;
            return ReferenceEquals(target.Def, SourceDefinition) ? Code.None : StandardCodes.WrongTarget;
        }
        // Preparation may release promises/evacuate stock through explicit prior nodes. Their
        // completion prerequisites belong to the final guard, never an implicit instant fallback.
        public Code CheckStart(World world, PlayerId company, Entity target) => CheckIntent(world, company, target);
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node) => world.Entities.Get(order.Target);
        public Code CheckSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
        {
            Code identity = CheckIntent(world, order.Owner, target);
            return identity.IsNone ? world.CheckDespawn(target) : identity;
        }
        public Code CommitSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
        { world.Despawn(target); return Code.None; }
        public Code Check(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }

    /// <summary>Authored upgrade delegates state/geometry/stock preservation to the sole coordinator.</summary>
    public sealed class EntityReplacementEffectDef : Def, IPhysicalWorkEffect, IEntityReplacementEffect, IWorkEffectIntentAdmission
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.AsReadOnly(ReferenceEquals(SourceDefinition, TargetDefinition) ? new IContentDefinition[] { SourceDefinition } : new IContentDefinition[] { SourceDefinition, TargetDefinition });
        private string _source, _target;
        public EntityDef SourceDefinition { get; private set; }
        public EntityDef TargetDefinition { get; private set; }
        public bool AcceptsSourceDefinition(EntityDef definition) => ReferenceEquals(definition, SourceDefinition);
        public Code CheckReplacement(World world, Entity source, EntityDef destination)
            => AcceptsSourceDefinition(source.Def) && ReferenceEquals(TargetDefinition, destination) ? Code.None : StandardCodes.WrongTarget;
        protected override void ReadCore(JsonReader reader)
        { _source = reader.String("source"); _target = reader.String("target"); }
        protected override void ResolveCore(ContentResolver content)
        { SourceDefinition = content.Def<EntityDef>(_source); TargetDefinition = content.Def<EntityDef>(_target); }
        public void RequireCapabilities(World world)
        {
            if (!world.Services.Has<IContentAccess>()) throw new ContentException(Id + ": replacement requires IContentAccess");
            if (!world.Services.Has<IContentOperationAccess>()) throw new ContentException(Id + ": replacement requires IContentOperationAccess");
        }
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
        {
            PhysicalWorkEffectContracts.ValidatePlan(world, this, plan, node);
            if (node != null) throw new ContentException(plan.PlanKey + ": replacement is a whole-order effect, never a node effect");
            if (plan is IEntityActivityDefinition targeted && !ReferenceEquals(targeted.TargetDefinition, SourceDefinition))
                throw new ContentException(plan.PlanKey + ": replacement source must match the authored activity target");
        }
        public Code CheckIntent(World world, PlayerId company, Entity target)
            => CheckTarget(world, company, target, true);
        private Code CheckTarget(World world, PlayerId company, Entity target, bool playerIntent)
        {
            if (target == null || !ReferenceEquals(world.Entities.Get(target.Id), target)) return Codes.NotFound;
            if (target.Owner != company) return Codes.NotOwned;
            if (!ReferenceEquals(target.Def, SourceDefinition)) return StandardCodes.WrongTarget;
            return playerIntent ? world.Services.Get<IContentAccess>().Check(world, company, TargetDefinition)
                : world.Services.Get<IContentOperationAccess>().CheckOperation(world, company, TargetDefinition);
        }
        public Code CheckStart(World world, PlayerId company, Entity target)
        {
            Code check = CheckTarget(world, company, target, false);
            return check.IsNone ? Upgrades.Check(world, target, TargetDefinition) : check;
        }
        public Code CheckSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => CheckStart(world, order.Owner, target);
        public Code CommitSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => Upgrades.TryReplace(world, target, TargetDefinition, out _);
    
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node) => world.Entities.Get(order.Target);
        public Code Check(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }

    /// <summary>Authored disposition over real inventory. Uses the sole lifecycle coordinator and facts.</summary>
    public sealed class InventoryEffectDef : Def, IInventoryWorkEffectDefinition
    {
        private InventoryWorkEffectDeclaration _declaration;
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        public Bill Quantities => _declaration.Quantities;
        public System.Collections.Generic.IReadOnlyList<ResourcePortRoute> Ports => _declaration.Ports;
        public ResourceFlowKind Kind => _declaration.Kind;
        protected override void ReadCore(JsonReader reader) => _declaration = InventoryWorkEffectDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content)
            => _declaration.Resolve(content);
        public void RequireCapabilities(World world) => InventoryWorkEffectContracts.RequireCapabilities(world, this);
        public Code CheckIntent(World world, PlayerId company, Entity target) => InventoryWorkEffectContracts.CheckIntent(world, this, company, target);
        public Code CheckStart(World world, PlayerId company, Entity target) => InventoryWorkEffectContracts.CheckStart(world, this, company, target);
        public Code CheckSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => InventoryWorkEffectContracts.Check(world, this, order.Owner, target, new ReservationOwner("sim.work", order.Id));
        public Code CommitSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => InventoryWorkEffectContracts.Commit(world, this, order.Owner, target, new ReservationOwner("sim.work", order.Id));
    
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node) => world.Entities.Get(order.Target);
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.ValidatePlan(world, this, plan, node);
        public Code Check(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }

    /// <summary>JSON-authored timed recovery delegates to the selected sole stock evacuation owner.</summary>
    public sealed class ResourceRecoveryEffectDef : Def, IPhysicalWorkEffect
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        protected override void ReadCore(JsonReader reader) { }
        public void RequireCapabilities(World world)
        {
            if (!world.Services.Has<IResourceRecovery>()) throw new ContentException(Id + ": recovery effect requires IResourceRecovery");
        }
        public Code CheckSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
        {
            if (target.Get<IMotion>()?.Status == MoveStatus.Moving) return WorkEffectCodes.MovingSource;
            IWorker worker = target.Get<IWorker>();
            if (worker != null && worker.Job != 0)
            {
                Job job = world.Services.Get<WorkBoard>().Job(worker.Job);
                if (job == null || !job.IsLive || job.OrderId != order.Id || node == null || job.NodeId != node.Id ||
                    order.Node(node.Id).State != NodeState.Completing) return WorkEffectCodes.BusySource;
            }
            return world.Services.Get<IResourceRecovery>().CheckEvacuation(world, target);
        }
        public Code CommitSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => world.Services.Get<IResourceRecovery>().Evacuate(world, target);
    
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node) => world.Entities.Get(order.Target);
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.ValidatePlan(world, this, plan, node);
        public Code Check(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }

    /// <summary>One refusal/retry classifier for all completion content, not a provider inheritance requirement.</summary>
    public static class WorkEffectContracts
    {
        private static void RequireActual(World world, IWorkEffect effect)
        {
            if (!(effect is Def actual) || !actual.IsFrozen || effect is IPersistentService ||
                effect.Id != actual.Id || !world.Content.Owns(effect) ||
                !ReferenceEquals(world.Content.Find<IWorkEffect>(effect.Id), effect))
                throw new ContentException("work effect requires its actual immutable content alias");
        }
        public static void ValidateDefinition(World world, IWorkEffect effect)
            => EntityObservation.ReadProvider(world, effect, null, () => {
                RequireActual(world, effect);
                if (effect is IPhysicalWorkEffect physical) physical.RequireCapabilities(world);
                if (effect is ILineEffectDefinition line) LineEffectContracts.ValidateMetadata(line, new ContentResolver(world.Content, (Def)line));
                if (effect is ITransformationDispatchEffect dispatch) TransformationDispatchContracts.ValidateDefinition(world, dispatch);
                if (effect is IConditionEffect condition) ConditionEffectContracts.ValidateDefinition(world, condition);
                return true;
            }, world.Space);
        public static void ValidatePlan(World world, IWorkEffect effect, IActivityDefinition plan, IActivityNodeDefinition node)
            => EntityObservation.ReadProvider(world, effect, null, () => {
                RequireActual(world, effect);
                if (effect is IWorkEffectPlanValidator validator) validator.ValidatePlan(world, plan, node);
                return true;
            }, world.Space);
        public static Code Preview(World world, IWorkEffect effect, Order order, IActivityNodeDefinition node)
        {
            Entity target = world.Entities.Get(order.Target);
            var scope = EntityObservation.CaptureProvider(world, target,
                target == null ? (Action<IStateSync>)null : target.Sync, "work.effect");
            Code result = EntityObservation.ReadProvider(world, effect, target, () => {
                RequireActual(world, effect);
                return effect is IWorkEffectPreview preview ? preview.Check(world, order, node) : WorkEffectCodes.NoPreview;
            }, world.Space);
            scope.Verify(); return result;
        }
        /// <summary>Refusals verify the actual target codec, not another persisted model or world hash.</summary>
        public static WorkEffectResult Apply(World world, IWorkEffect effect, Order order, IActivityNodeDefinition node)
        {
            ValidateDefinition(world, effect);
            if (order == null || !ReferenceEquals(world.Services.Get<WorkBoard>().Order(order.Id), order))
                return WorkEffectResult.Failed(Codes.NotFound);
            if (node == null ? order.Plan.OnComplete != effect.Id || order.State != OrderState.Completing || order.NodesDone != order.Nodes.Count :
                !ReferenceEquals(order.Plan.Graph.Find(node.Id), node) || node.Effect != effect.Id ||
                !order.IsActive || order.Node(node.Id)?.State != NodeState.Completing)
                return WorkEffectResult.Failed(WorkEffectCodes.NotComplete);
            Entity target = world.Entities.Get(order.Target);
            var facts = target == null ? EntityObservation.Capture(world) : EntityObservation.Capture(world, target);
            var scope = EntityObservation.CaptureProvider(world, target,
                target == null ? (Action<IStateSync>)null : target.Sync, "work.effect");
            var spatial = EntityObservation.CaptureSpace(world, world.Space);
            var clock = world.Clock; long at = clock.StepIndex; int speed = clock.Speed; bool paused = clock.Paused;
            try
            {
                WorkEffectResult result = effect.Apply(world, order, node);
                if (!ReferenceEquals(world.Clock, clock) || clock.StepIndex != at || clock.Speed != speed || clock.Paused != paused)
                    throw new InvalidOperationException("work effects cannot mutate the game clock");
                if (result.Status < WorkEffectStatus.Applied || result.Status > WorkEffectStatus.Failed ||
                    (result.Status == WorkEffectStatus.Applied) != result.Blocker.IsNone)
                    throw new InvalidOperationException("work effect returned malformed completion facts");
                if (result.Status != WorkEffectStatus.Applied)
                {
                    facts.Verify(); scope.Verify();
                    spatial.Verify();
                }
                world.RequireHealthyUpdate();
                return result;
            }
            catch (Exception error) { world.LatchFault("work.effect", error); throw; }
        }
        /// <summary>Native refusals use the same selected authored retry policy as physical jobs.</summary>
        public static WorkEffectResult Outcome(World world, Code code)
        {
            if (code.IsNone) return WorkEffectResult.Applied;
            if (code == Codes.NotFound || code == Codes.NotOwned || code == Codes.InvalidArgument ||
                code == WorkEffectCodes.NotComplete || code == IncidentCodes.NotResponse || code == IncidentCodes.StaleResponse)
                return WorkEffectResult.Failed(code);
            return world.Services.Get<WorkBoard>().RetryPolicy.CanRetry(code)
                ? WorkEffectResult.Pending(code) : WorkEffectResult.Failed(code);
        }
    }

    public sealed class ConstructionEffectDef : Def, IWorkEffect
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        private bool _demolish;
        protected override void ReadCore(JsonReader r)
        {
            string operation = r.String("operation");
            if (operation != "finish" && operation != "demolish") throw r.Error("construction effect operation must be finish or demolish");
            _demolish = operation == "demolish";
        }
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
        {
            IConstructionProjects projects = world.Services.Get<IConstructionProjects>();
            return WorkEffectContracts.Outcome(world, _demolish ? projects.Demolish(world, order) : projects.Finish(world, order));
        }
    }

    public enum LineEffectOperation : byte { Segment, Finish, Remove }

    public sealed class LineEffectDef : Def, ILineEffectDefinition, IWorkEffectIntentAdmission
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => LineEffectContracts.ContentReferences(this);
        private LineEffectDeclaration _declaration;
        public LineEffectOperation Operation => _declaration.Operation;
        public ILineDefinition Line => _declaration.Line;
        protected override void ReadCore(JsonReader r) => _declaration = LineEffectDeclaration.Read(r);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public Code CheckIntent(World world, PlayerId company, Entity target)
            => LineEffectContracts.CheckIntent(world, this, company, target);
        public Code CheckStart(World world, PlayerId company, Entity target)
            => LineEffectContracts.CheckStart(world, this, company, target);
        public void RequireCapabilities(World world)
        => LineEffectContracts.RequireCapabilities(world, this);
        public Code CheckSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => LineEffectContracts.CheckSubject(world, this, target, order, node);
        public Code CommitSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
            => LineEffectContracts.CommitSubject(world, this, target, order, node);
    
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node) => LineEffectContracts.ResolveSubject(world, order, node);
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
            => LineEffectContracts.ValidatePlan(world, this, plan, node);
        public Code Check(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }

    /// <summary>JSON names the incident owner; normal work completion performs recovery.</summary>
    public sealed class IncidentResponseEffectDef : Def, IWorkEffect, IIncidentCompletionEffect
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        private IncidentCompletionDeclaration _declaration;
        public Code Source => _declaration.Source;
        public bool BoundResponse => _declaration.BoundResponse;
        protected override void ReadCore(JsonReader r) => _declaration = IncidentCompletionDeclaration.Read(r);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => ResponseWork.ApplyCompletion(world, this, order, node);
    }
}
