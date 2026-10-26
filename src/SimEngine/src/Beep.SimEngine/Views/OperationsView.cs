using System;
using System.Collections.Generic;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class OperationsView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal OperationsView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        private Entity Owned(EntityId id)
        {
            _world.RequireHealthyUpdate();
            if (_viewer != PlayerId.None && !_world.Players.IsSeat(_viewer)) return null;
            Entity entity = _world.Entities.Get(id);
            return _world.ViewFor(_viewer).CanObserveOwned(entity) ? entity : null;
        }
        private T Observe<T>(object provider, Entity entity, Func<T> read)
            => EntityObservation.ReadProvider(_world, provider, entity, read, observeTargetState: true);
        public bool CopyUtilityConsumerIds(EntityId id, IList<Code> into)
        {
            if (into == null) throw new ArgumentNullException(nameof(into));
            into.Clear(); Entity entity = Owned(id); if (entity == null) return false;
            var ids = new List<Code>();
            foreach (Component component in entity.Components) if (component is IUtilityConsumer consumer) ids.Add(Observe(consumer, entity, () => {
                Code identity = consumer.CapabilityId;
                if (identity.IsNone || !ReferenceEquals(entity.Get<IUtilityConsumer>(identity), consumer))
                    throw new InvalidOperationException("utility identity must name its actual entity owner");
                return identity;
            }));
            ids.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); foreach (Code identity in ids) into.Add(identity);
            return true;
        }
        public bool TryGetUtilityDemand(EntityId id, Code capability, out UtilityDemandSnapshot snapshot)
        {
            snapshot = default; _world.RequireHealthyUpdate(); Entity entity = capability.IsNone ? null : Owned(id);
            IUtilityConsumer provider = entity?.Get<IUtilityConsumer>(capability);
            if (provider == null) return false;
            snapshot = Observe(provider, entity, () => UtilityContracts.Read(_world, provider)); return true;
        }
        public bool CopyOperationIds(EntityId id, IList<Code> into)
        {
            if (into == null) throw new ArgumentNullException(nameof(into));
            into.Clear(); Entity entity = Owned(id);
            if (entity == null) return false;
            var ids = new List<Code>();
            foreach (Component part in entity.Components)
                if (part is IOperation operation) ids.Add(Observe(operation, entity, () => OperationContracts.ReadIdentity(_world, operation)));
            foreach (Code identity in ids) into.Add(identity);
            return true;
        }
        /// <summary>Includes signal-only capabilities; operation membership is not required.</summary>
        public bool CopySignalSourceIds(EntityId id, IList<Code> into)
        {
            if (into == null) throw new ArgumentNullException(nameof(into));
            into.Clear();
            Entity entity = Owned(id);
            if (entity == null) return false;
            var sources = new List<Code>();
            for (int i = 0; i < entity.Components.Count; i++)
                if (entity.Components[i] is IAutomationSignalSource source) sources.Add(Observe(source, entity, () => {
                    Code identity = source.CapabilityId;
                    if (identity.IsNone || !ReferenceEquals(entity.Get<IAutomationSignalSource>(identity), source))
                        throw new InvalidOperationException("signal identity must name its actual entity owner");
                    return identity;
                }));
            sources.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            foreach (var source in sources) into.Add(source);
            return true;
        }
        public bool TryGetOperation(EntityId id, Code operationId, out OperationSnapshot snapshot)
        {
            snapshot = default; _world.RequireHealthyUpdate();
            if (operationId.IsNone) return false;
            Entity entity = Owned(id); IOperation operation = entity?.Get<IOperation>(operationId);
            if (operation == null) return false;
            snapshot = Observe(operation, entity, () => OperationContracts.Read(_world, operation));
            return true;
        }
        /// <summary>Actual target bindings interpreted by the selected authored action, not host type checks.</summary>
        public bool CopyControlTargetIds(EntityId id, string profile, IList<Code> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear(); Entity entity = Owned(id);
            IControlRule rule = string.IsNullOrEmpty(profile) ? null : _world.Content.Find<IControlRule>(profile);
            if (entity == null || !AutomationProfileSnapshot.VisibleTo(_world, _viewer, rule)) return false;
            var targets = Observe(rule, entity, () => {
                if (rule.ActionPolicy.TargetKind != ControlTargetKind.Entity) return null;
                var values = new List<Code>(); ControlActionDispatch.CopyTargets(_world, entity.Owner, entity, rule.ActionPolicy, values); return values;
            });
            if (targets == null) return false;
            foreach (Code target in targets) destination.Add(target); return true;
        }
        /// <summary>Explicit company capability discovery; signal sources remain real entity bindings.</summary>
        public bool CopyCompanyControlTargetIds(PlayerId company, string profile, IList<Code> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination)); destination.Clear();
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company) || _viewer != PlayerId.None && (!_world.Players.IsSeat(_viewer) || _viewer != company)) return false;
            IControlRule rule = string.IsNullOrEmpty(profile) ? null : _world.Content.Find<IControlRule>(profile);
            if (!AutomationProfileSnapshot.VisibleTo(_world, _viewer, rule)) return false;
            var targets = Observe(rule, null, () => {
                if (rule.ActionPolicy.TargetKind != ControlTargetKind.Company) return null;
                var values = new List<Code>(); ControlActionDispatch.CopyTargets(_world, company, null, rule.ActionPolicy, values); return values;
            });
            if (targets == null) return false;
            foreach (Code target in targets) destination.Add(target); return true;
        }
        public bool TryGetControl(EntityId id, Code operationId, out OperationControlSnapshot snapshot)
        {
            snapshot = default; _world.RequireHealthyUpdate();
            if (operationId.IsNone) return false;
            Entity entity = Owned(id); IControllableOperation operation = entity?.Get<IControllableOperation>(operationId);
            if (operation == null) return false;
            snapshot = Observe(operation, entity, () => OperationContracts.ReadControl(_world, operation));
            return true;
        }
        public bool TryGetOperationSchema(EntityId id, Code operationId, out OperationProviderSchema schema)
        {
            schema = null; _world.RequireHealthyUpdate();
            if (operationId.IsNone) return false;
            Entity entity = Owned(id); IOperation operation = entity?.Get<IOperation>(operationId);
            if (operation == null) return false;
            schema = Observe(operation, entity, () => OperationContracts.ReadSchema(_world, operation));
            return true;
        }
        public bool TryGetSignalSchema(EntityId id, Code sourceId, out AutomationSignalProviderSchema schema)
        {
            schema = null; _world.RequireHealthyUpdate();
            if (sourceId.IsNone) return false;
            Entity entity = Owned(id); IAutomationSignalSource source = entity?.Get<IAutomationSignalSource>(sourceId);
            if (source == null) return false;
            schema = Observe(source, entity, () => AutomationSignalContracts.Schema(entity.Def, sourceId));
            return schema != null;
        }
        public Code PreviewConfigure(EntityId id, Code operationId, bool enabled, Fix target)
        {
            _world.RequireHealthyUpdate();
            Entity entity = _world.Entities.Get(id);
            return ConfigureOperationHandler.Check(_world, _viewer == PlayerId.None ? entity?.Owner ?? _viewer : _viewer, id, operationId, enabled, target);
        }
        public Code ReadSignal(EntityId id, Code sourceId, string signalId, out AutomationSignalSnapshot snapshot)
        {
            snapshot = default; _world.RequireHealthyUpdate();
            if (_viewer != PlayerId.None && !_world.Players.IsSeat(_viewer)) return Codes.NotSeated;
            PlayerView view = _world.ViewFor(_viewer);
            if (!view.TryGetEntity(id, out _)) return Codes.NotFound;
            Entity entity = _world.Entities.Get(id);
            if (entity == null) return Codes.NotFound;
            if (_viewer.IsValid && entity.Owner != _viewer) return Codes.NotOwned;
            if (sourceId.IsNone) return Codes.InvalidArgument;
            IAutomationSignalContent signal = _world.Content.Find<IAutomationSignalContent>(signalId);
            if (signal == null || !view.CanSeeContent(signal)) return Codes.UnknownDef;
            IAutomationSignalSource source = entity.Get<IAutomationSignalSource>(sourceId);
            snapshot = Observe(signal, entity, () => signal.ReadFrom(_world, source));
            return Code.None;
        }

        public void CopyAutomationRules(IList<AutomationRuleSnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            _world.RequireHealthyUpdate();
            if (_world.Services.TryGet<IAutomationRules>(out var rules)) {
                var values = Observe(rules, null, () => { var list = new List<AutomationRuleSnapshot>(); rules.CopyRules(_viewer, list); return list; });
                foreach (var value in values) destination.Add(value);
            }
        }
        /// <summary>Detached complete authored profile; no executable rule/provider handles.</summary>
        public bool TryGetAutomationProfile(string profile, out AutomationProfileSnapshot snapshot)
        {
            snapshot = null; _world.RequireHealthyUpdate();
            if (string.IsNullOrEmpty(profile) || _viewer != PlayerId.None && !_world.Players.IsSeat(_viewer)) return false;
            IControlRule definition = _world.Content.Find<IControlRule>(profile);
            if (!AutomationProfileSnapshot.VisibleTo(_world, _viewer, definition)) return false;
            snapshot = Observe(definition, null, () => new AutomationProfileSnapshot(definition));
            return true;
        }
        public void CopyAutomationProfiles(IList<AutomationProfileSnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            _world.RequireHealthyUpdate();
            var profiles = new List<AutomationProfileSnapshot>();
            foreach (var definition in _world.Content.All<IControlRule>())
                if (AutomationProfileSnapshot.VisibleTo(_world, _viewer, definition))
                    profiles.Add(Observe(definition, null, () => new AutomationProfileSnapshot(definition)));
            profiles.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            foreach (var profile in profiles) destination.Add(profile);
        }
        public bool TryGetAutomationRule(PlayerId company, string id, out AutomationRuleSnapshot snapshot)
        {
            snapshot = null; _world.RequireHealthyUpdate();
            if (!(_viewer == PlayerId.None || _world.Players.IsSeat(_viewer) && _viewer == company) || !_world.Services.TryGet<IAutomationRules>(out var rules)) return false;
            AutomationRuleSnapshot result = null;
            bool found = Observe(rules, null, () => rules.TryGet(company, id, out result)); snapshot = result; return found;
        }
        public Code PreviewAutomation(string id, AutomationSettings settings)
        {
            _world.RequireHealthyUpdate();
            return !_viewer.IsValid ? Codes.NotSeated : _world.Services.TryGet<IAutomationRules>(out var rules)
                ? Observe(rules, null, () => rules.CheckConfigure(_viewer, id, settings)) : Codes.NotAllowed;
        }
        public Code PreviewRemoveAutomation(string id)
        {
            _world.RequireHealthyUpdate();
            return !_viewer.IsValid ? Codes.NotSeated : _world.Services.TryGet<IAutomationRules>(out var rules)
                ? Observe(rules, null, () => rules.CheckRemove(_viewer, id)) : Codes.NotAllowed;
        }
    }
}
