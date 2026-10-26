using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public readonly struct EntityOutputPreview
    {
        internal EntityOutputPreview(string output, Code refusal, SpatialPlacement placement)
        { Output = output; Refusal = refusal; Placement = placement; }
        public string Output { get; }
        public Code Refusal { get; }
        public SpatialPlacement Placement { get; }
    }
    /// <summary>Private detached transformer facts; all provider selection is explicitly named.</summary>
    public sealed class TransformationManagementView : ITransformationExecutionView
    {
        public Code ReadExecutionPreview(EntityId entityId, Code capability, string transformationId, out TransformationExecutionPreview facts)
        {
            Code result = ReadRecipe<TransformationExecutionPreview>(entityId, capability, transformationId, null,
                (entity, provider, definition) => ProjectExecution(provider, definition), out facts);
            return result.IsNone && facts == null ? Codes.UnknownDef : result;
        }

        private TransformationExecutionPreview ProjectExecution(ITransformer provider, ITransformationDefinition definition)
        {
            var control = OperationContracts.ReadControl(_world, provider);
            var current = TransformationContracts.Read(_world, provider);
            if (!TransformationContracts.CanDiscloseExecution(_world, _viewer, current)) return null;
            var gates = TransformationContracts.ReadGates(_world, provider, definition);
            var cost = BusinessModeRules.EvaluateCost(_world, MonetaryCostCategory.Manufacturing, definition.Cash);
            GameDuration? cycle = null, remaining = null; long? completion = null;
            ProductiveEstimateStatus status;
            long required = _world.Time.Ticks(definition.Duration);
            if (definition.Mode == TransformationMode.Batch && control.Target > Fix.Zero
                && WorkProgress.Zero.TryRemainingUpdates(control.Target, required, out long cycleUpdates))
                cycle = GameDuration.FromSteps(cycleUpdates, _world.Time);
            if (current.Transformation != definition.Id) status = ProductiveEstimateStatus.NotHead;
            else if (current.Phase == TransformationPhase.Returning) status = ProductiveEstimateStatus.Returning;
            else if (!control.Enabled || !gates.Blocker.IsNone || !current.Blocker.IsNone) status = ProductiveEstimateStatus.Blocked;
            else if (_world.Clock.Paused) status = ProductiveEstimateStatus.Paused;
            else if (control.Target <= Fix.Zero) status = ProductiveEstimateStatus.ZeroRate;
            else if (current.Phase != TransformationPhase.Working) status = ProductiveEstimateStatus.Waiting;
            else if (definition.Mode == TransformationMode.Continuous) status = ProductiveEstimateStatus.Continuous;
            else if (!current.Progress.TryRemainingUpdates(control.Target, required, out long updates)
                || updates > long.MaxValue - _world.Tick) status = ProductiveEstimateStatus.OutOfRange;
            else
            {
                remaining = GameDuration.FromSteps(updates, _world.Time);
                completion = _world.Tick + updates; status = ProductiveEstimateStatus.Conditional;
            }
            return new TransformationExecutionPreview(definition, control, current, gates, cost, _world.Tick,
                _world.Clock.Paused, cycle, remaining, completion, status);
        }
        public Code ReadOutputPorts(EntityId entityId, Code capability, string transformationId, out IReadOnlyList<ResourcePortSnapshot> ports)
            => ReadPorts(entityId, capability, transformationId, 0, out ports);
        public Code ReadInputPorts(EntityId entityId, Code capability, string transformationId, out IReadOnlyList<ResourcePortSnapshot> ports)
            => ReadPorts(entityId, capability, transformationId, 1, out ports);
        public Code ReadToolPorts(EntityId entityId, Code capability, string transformationId, out IReadOnlyList<ResourcePortSnapshot> ports)
            => ReadPorts(entityId, capability, transformationId, 2, out ports);
        public Code ReadOperatingPorts(EntityId entityId, Code capability, string transformationId, out IReadOnlyList<ResourcePortSnapshot> ports)
            => ReadPorts(entityId, capability, transformationId, 3, out ports);
        public Code ReadToolConsumption(EntityId entityId, Code capability, string transformationId, out Bill consumption)
            => ReadRecipe(entityId, capability, transformationId, Bill.Empty,
                (entity, provider, definition) => definition.ToolConsumption, out consumption);
        public Code ReadToolConsumptionRates(EntityId entityId, Code capability, string transformationId, out IReadOnlyList<ResourceRateInfo> rates)
            => ReadRecipe<IReadOnlyList<ResourceRateInfo>>(entityId, capability, transformationId, Array.Empty<ResourceRateInfo>(),
                (entity, provider, definition) =>
                {
                    var source = definition.ToolConsumptionRates;
                    var result = new ResourceRateInfo[source.Count];
                    for (int i = 0; i < result.Length; i++) result[i] = new ResourceRateInfo(source[i]);
                    return Array.AsReadOnly(result);
                }, out rates);
        private Code ReadPorts(EntityId entityId, Code capability, string transformationId, int direction, out IReadOnlyList<ResourcePortSnapshot> ports)
            => ReadRecipe<IReadOnlyList<ResourcePortSnapshot>>(entityId, capability, transformationId, Array.Empty<ResourcePortSnapshot>(),
                (entity, provider, definition) => ResourcePortContracts.Facts(direction == 1
                    ? TransformationContracts.InputBindings(_world, entity, definition) : direction == 2
                    ? TransformationContracts.ToolBindings(_world, entity, definition) : direction == 3
                    ? TransformationContracts.OperatingBindings(_world, entity, definition) : TransformationContracts.OutputBindings(_world, entity, definition)), out ports);
        // Selection and the entire detached projection are one observation; no out facts escape
            // until the actual processor, whole entity, selected space and clock scopes verify.
        private Code ReadRecipe<T>(EntityId entityId, Code capability, string transformationId, T empty,
            Func<Entity, ITransformer, ITransformationDefinition, T> project, out T facts)
        {
            facts = empty;
            Entity entity = Owned(entityId); ITransformer provider = Provider(entityId, capability);
            if (entity == null || provider == null) return Codes.NotFound;
            var result = EntityObservation.ReadProvider(_world, provider, entity, () =>
            {
                ITransformationDefinition definition = _world.Content.Find<ITransformationDefinition>(transformationId);
                if (!TransformationContracts.CanDisclose(_world, _viewer, definition)) return (Codes.UnknownDef, empty);
                if (!TransformationContracts.Supports(provider, definition)) return (EnqueueTransformationHandler.NotMadeHere, empty);
                return (Code.None, project(entity, provider, definition));
            }, space: _world.Space, observeTargetState: true);
            facts = result.Item2; return result.Item1;
        }
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal TransformationManagementView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        private Entity Owned(EntityId id)
        {
            _world.RequireHealthyUpdate();
            Entity entity = _world.Entities.Get(id);
            return _world.ViewFor(_viewer).CanObserveOwned(entity) ? entity : null;
        }
        private ITransformer Provider(EntityId id, Code capability)
            => capability.IsNone ? null : Owned(id)?.Get<ITransformer>(capability);
        public Code ReadStaffing(EntityId entityId, Code capability, string transformationId, out IReadOnlyList<TransformationStaffingSnapshot> facts)
            => ReadRecipe<IReadOnlyList<TransformationStaffingSnapshot>>(entityId, capability, transformationId, Array.Empty<TransformationStaffingSnapshot>(),
                (entity, provider, definition) => ReadStaffing(entity, provider, definition), out facts);
        private IReadOnlyList<TransformationStaffingSnapshot> ReadStaffing(Entity entity, ITransformer provider, ITransformationDefinition definition)
        {
            var sources = new List<(Code Identity, ITransformationStaffing Provider)>();
            foreach (var component in entity.Components) if (component is ITransformationStaffing source)
                sources.Add((EntityObservation.ReadProvider(_world, source, entity, () => source.CapabilityId), source));
            sources.Sort((a, b) => string.CompareOrdinal(a.Identity.Id, b.Identity.Id));
            var result = new List<TransformationStaffingSnapshot>();
            foreach (var entry in sources)
            {
                ITransformationStaffing source = entry.Provider; Code identity = entry.Identity;
                TransformationStaffingSnapshot value = EntityObservation.ReadProvider(_world, source, entity, () =>
                {
                    if (identity.IsNone || source.CapabilityId != identity || !ReferenceEquals(entity.Get<ITransformationStaffing>(identity), source))
                        throw new InvalidOperationException("staffing source is not the actual named capability");
                    var snapshot = source.ReadStaffing(_world, provider, definition);
                    if (source.CapabilityId != identity || snapshot == null || snapshot.Capability != identity || snapshot.Requirements == null ||
                        !snapshot.Applies && (!snapshot.Blocker.IsNone || snapshot.Requirements.Count != 0))
                        throw new InvalidOperationException("staffing source returned malformed facts");
                    return snapshot;
                });
                result.Add(value);
            }
            return result.AsReadOnly();
        }
        public bool CopyQueue(EntityId entity, Code capability, IList<TransformationQueueEntry> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            if (!TryGetTransformation(entity, capability, out TransformationSnapshot snapshot)) return false;
            foreach (TransformationQueueEntry entry in snapshot.Queue) destination.Add(entry);
            return true;
        }
        public bool TryGetTransformation(EntityId entity, Code capability, out TransformationSnapshot snapshot)
        {
            snapshot = default; ITransformer provider = Provider(entity, capability);
            if (provider == null) return false;
            var result = EntityObservation.ReadProvider(_world, provider, Owned(entity), () =>
            {
                TransformationSnapshot current = TransformationContracts.Read(_world, provider);
                return TransformationContracts.CanDiscloseExecution(_world, _viewer, current)
                    ? (true, current) : (false, default(TransformationSnapshot));
            }, space: _world.Space, observeTargetState: true);
            snapshot = result.Item2; return result.Item1;
        }
        public IReadOnlyList<string> TransformationsOf(EntityId entity, Code capability)
        {
            ITransformer provider = Provider(entity, capability);
            if (provider == null) return Array.Empty<string>();
            return EntityObservation.ReadProvider(_world, provider, Owned(entity), () =>
            {
                var list = new List<string>();
                foreach (ITransformationDefinition definition in TransformationContracts.Definition(provider).Transformations)
                    if (TransformationContracts.CanDisclose(_world, _viewer, definition)) list.Add(definition.Id);
                return list.AsReadOnly();
            }, space: _world.Space, observeTargetState: true);
        }
        public string TransformationEntityOutput(string transformation)
        {
            _world.RequireHealthyUpdate();
            ITransformationDefinition definition = _world.Content.Find<ITransformationDefinition>(transformation);
            if (definition == null) return null;
            return EntityObservation.ReadProvider(_world, definition, null, () =>
            {
                if (!TransformationContracts.CanDisclose(_world, _viewer, definition)) return null;
                EntityDef output = definition.EntityOutput;
                return output != null && _world.ViewFor(_viewer).CanSeeContent(output) ? output.Id : null;
            }, space: _world.Space);
        }
        public Code PreviewEnqueue(EntityId entityId, Code capability, string transformation, int count, bool repeat, EntityId home)
        {
            _world.RequireHealthyUpdate();
            PlayerId company = _viewer == PlayerId.None ? Owned(entityId)?.Owner ?? PlayerId.None : _viewer;
            return _world.Services.Get<ITransformationQueue>().CheckEnqueue(company,
                entityId, capability, transformation, count, repeat, home);
        }
        public Code PreviewCancel(EntityId entityId, Code capability, int index)
        {
            _world.RequireHealthyUpdate();
            PlayerId company = _viewer == PlayerId.None ? Owned(entityId)?.Owner ?? PlayerId.None : _viewer;
            return _world.Services.Get<ITransformationQueue>().CheckCancel(company, entityId, capability, index);
        }
        public Code ReadMonetaryCost(EntityId entityId, Code capability, string transformationId, out MonetaryCostDecision decision)
            => ReadRecipe(entityId, capability, transformationId, default(MonetaryCostDecision),
                (entity, provider, definition) => BusinessModeRules.EvaluateCost(_world, MonetaryCostCategory.Manufacturing, definition.Cash), out decision);
        public Code ReadGates(EntityId entityId, Code capability, string transformationId, out ExecutionGateEvaluation evaluation)
            => ReadRecipe<ExecutionGateEvaluation>(entityId, capability, transformationId, null,
                (entity, provider, definition) => TransformationContracts.ReadGates(_world, provider, definition), out evaluation);
        public EntityOutputPreview PreviewEntityOutput(EntityId entityId, Code capability, string transformationId)
        {
            Code selection = ReadRecipe(entityId, capability, transformationId, default(EntityOutputPreview),
                (entity, provider, transformation) =>
            {
                EntityDef output = transformation.EntityOutput;
                if (output != null && !_world.ViewFor(_viewer).CanSeeContent(output))
                    return new EntityOutputPreview(null, Codes.UnknownDef, null);
                Code refusal = TransformerManager.CheckAccess(_world, entity.Owner, transformation);
                SpatialPlacement placement = null;
                if (refusal.IsNone && output != null)
                {
                    refusal = EntityRollout.Find(_world, entity, output, out placement);
                    if (refusal.IsNone) refusal = _world.PrepareCreation(output, entity.Owner, entity.Id, placement, out _);
                    if (refusal.IsNone) refusal = ResourceTransactions.CheckResultDefinition(output);
                }
                return new EntityOutputPreview(output?.Id, refusal, placement);
            }, out EntityOutputPreview result);
            return selection.IsNone ? result : new EntityOutputPreview(null, selection, null);
        }
    }
}

