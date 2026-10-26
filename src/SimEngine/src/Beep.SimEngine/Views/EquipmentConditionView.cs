using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class EquipmentConditionView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal EquipmentConditionView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        private Entity Owned(EntityId id)
        {
            _world.RequireHealthyUpdate();
            Entity entity = _world.Entities.Get(id);
            return _world.ViewFor(_viewer).CanObserveOwned(entity) ? entity : null;
        }
        private T Observe<T>(Entity entity, System.Func<T> read)
            => EntityObservation.ReadProvider(_world, entity, entity, read, observeTargetState: true);
        public bool CopyIds(EntityId id, IList<Code> destination)
        {
            if (destination == null) throw new System.ArgumentNullException(nameof(destination));
            destination.Clear(); Entity entity = Owned(id); if (entity == null) return false;
            var ids = Observe(entity, () => {
                var values = new List<Code>();
                foreach (Component part in entity.Components) if (part is IConditionProvider condition)
                    values.Add(ConditionContracts.Read(_world, condition).Capability);
                return values;
            });
            foreach (Code identity in ids) destination.Add(identity);
            return true;
        }
        public Code Read(EntityId id, Code source, out ConditionSnapshot value)
        {
            value = default; Entity entity = Owned(id); if (entity == null) return Codes.NotFound;
            if (source.IsNone) return Codes.InvalidArgument;
            IConditionProvider provider = entity.Get<IConditionProvider>(source); if (provider == null) return ConditionCodes.NotCondition;
            value = Observe(entity, () => ConditionContracts.Read(_world, provider)); return Code.None;
        }
        public Code ReadWear(EntityId id, Code source, out ConditionWearSnapshot value)
        {
            value = default; Entity entity = Owned(id); if (entity == null) return Codes.NotFound;
            if (source.IsNone) return Codes.InvalidArgument;
            var provider = entity.Get<IConditionWearProvider>(source); if (provider == null) return ConditionWearCodes.NotWear;
            value = Observe(entity, () => ConditionWearContracts.Read(_world, provider, out _)); return Code.None;
        }
        public Code ReadPolicy(EntityId id, Code source, out OperationPolicyView value)
        {
            value = default; Entity entity = Owned(id); if (entity == null) return Codes.NotFound;
            if (source.IsNone) return Codes.InvalidArgument;
            var policy = entity.Get<IOperationPolicy>(source); if (policy == null) return OperationPolicyCodes.NotPolicy;
            value = Observe(entity, () => OperationPolicyContracts.Read(_world, policy)); return Code.None;
        }
        public Code PreviewPolicy(EntityId id, Code source, OperationPolicySettings settings)
        {
            _world.RequireHealthyUpdate();
            Entity entity = _world.Entities.Get(id);
            return OperationPolicyContracts.CheckConfigure(_world, _viewer == PlayerId.None ? entity?.Owner ?? _viewer : _viewer, entity, source, settings);
        }
        public bool CopyPolicyIds(EntityId id, IList<Code> destination)
        {
            if (destination == null) throw new System.ArgumentNullException(nameof(destination));
            destination.Clear(); Entity entity = Owned(id); if (entity == null) return false;
            var ids = Observe(entity, () => {
                var values = new List<Code>();
                foreach (Component part in entity.Components) if (part is IOperationPolicy policy) values.Add(OperationPolicyContracts.ReadIdentity(_world, policy));
                return values;
            });
            foreach (Code identity in ids) destination.Add(identity);
            return true;
        }
    }
}
