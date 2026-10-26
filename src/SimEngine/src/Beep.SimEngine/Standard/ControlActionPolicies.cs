using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Normal single-entity controls expose their actual target for generic dispatch validation.</summary>
    public interface IEntityControlCommand { EntityId TargetEntity { get; } }
    public interface ICompanyControlCommand { PlayerId TargetCompany { get; } }

    /// <summary>Detached requested intent, not an execution context or independent clock.</summary>
    public readonly struct ControlActionRequest
    {
        internal ControlActionRequest(PlayerId company, ControlBinding target, ControlAction action, long stepIndex)
        { Company = company; Target = target; Action = action; StepIndex = stepIndex; }
        public PlayerId Company { get; }
        public ControlBinding Target { get; }
        public ControlAction Action { get; }
        public long StepIndex { get; }
    }

    public interface IControlActionPolicy : IContentDefinition
    {

        string CommandId { get; }
        string CommandOwner { get; }
        ControlTargetKind TargetKind { get; }
        IReadOnlyList<IContentDefinition> ContentDependencies { get; }
        void ValidateComposition(World world);
        void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination);
        Code CheckBinding(World world, PlayerId company, ControlBinding target, ControlAction action);
        /// <summary>Pure admission for this policy's exact target during physical replacement; no command or state reset.</summary>
        Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action);
        /// <summary>Pure typed target rewrite. Preserve capability intent; remap embedded endpoint identities through this policy's schema.</summary>
        ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination);
        Command CreateCommand(ControlActionRequest request);
    }

    /// <summary>Shared operation intent; the normal command owner still owns safety and mutation.</summary>
    public sealed class OperationControlActionDeclaration
    {
        private string _command, _owner;
        public string CommandId => _command;
        public string CommandOwner => _owner;
        public ControlTargetKind TargetKind => ControlTargetKind.Entity;
        public IReadOnlyList<IContentDefinition> ContentDependencies => Array.Empty<IContentDefinition>();
        public void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination)
        {
            destination.Clear();
            foreach (var component in world.Entities.Get(entity).Components)
                if (component is IControllableOperation operation) destination.Add(operation.CapabilityId);
        }
        public void Read(JsonReader reader)
        {
            _command = reader.String("command"); _owner = reader.String("owner");
            try { ContractIdentity.ValidateId(_command, "action command"); ContractIdentity.ValidateId(_owner, "action command owner"); }
            catch (ArgumentException ex) { throw reader.Error(ex.Message); }
        }
        public void ValidateComposition(World world, string id)
        {
            if (!world.Commands.TryGet(CommandId, out var command) || command.Type != typeof(ConfigureOperationCommand)
                || command.Owner != OperationsModule.ModuleId || CommandOwner != command.Owner)
                throw new ContentException(id + ": operation action requires the actual normal operation command owner");
        }
        public Code CheckBinding(World world, PlayerId company, ControlBinding target, ControlAction action)
        {
            var operation = world.Entities.Get(target.Entity)?.Get<IControllableOperation>(target.Capability);
            if (operation == null) return OperationCodes.NotControllable;
            return OperationContracts.ReadControl(world, operation).Unit == action.Unit ? Code.None : AutomationCodes.UnitMismatch;
        }
        public Command CreateCommand(ControlActionRequest request) => new ConfigureOperationCommand
        {
            Entity = request.Target.Entity, Operation = request.Target.Capability,
            Enabled = request.Action.Enabled, Target = request.Action.Target
        };
        public Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action)
        {
            var operation = destination.Component<IOperationDefinition>(target.Capability);
            var current = source.Get<IControllableOperation>(target.Capability) as Component;
            if (current == null || operation is not ComponentDef component || operation.ControlSettings == null ||
                !destination.HasCapability<IControllableOperation>(target.Capability) || current.Def.Key != component.Key ||
                !Upgrades.CanRetainState(current.Def, component)) return OperationCodes.NotControllable;
            if (operation.ControlSettings.Initial.Unit != action.Unit) return AutomationCodes.UnitMismatch;
            return operation.ControlSettings.Check(action.Target);
        }
        public ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination)
            => new ControlBinding(destination.Id, target.Capability);
    }

    public sealed class OperationControlActionPolicyDef : Def, IControlActionPolicy
    {
        private readonly OperationControlActionDeclaration _declaration = new OperationControlActionDeclaration();
        public string CommandId => _declaration.CommandId;
        public string CommandOwner => _declaration.CommandOwner;
        public ControlTargetKind TargetKind => _declaration.TargetKind;
        public IReadOnlyList<IContentDefinition> ContentDependencies => _declaration.ContentDependencies;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        public void ValidateComposition(World world) => _declaration.ValidateComposition(world, Id);
        public void CopyTargetIds(World world, PlayerId company, EntityId entity, IList<Code> destination)
            => _declaration.CopyTargetIds(world, company, entity, destination);
        public Code CheckBinding(World world, PlayerId company, ControlBinding target, ControlAction action)
            => _declaration.CheckBinding(world, company, target, action);
        public Code CheckReplacement(World world, PlayerId company, ControlBinding target, Entity source, EntityDef destination, ControlAction action)
            => _declaration.CheckReplacement(world, company, target, source, destination, action);
        public ControlBinding RebindTarget(World world, PlayerId company, ControlBinding target, Entity source, Entity destination)
            => _declaration.RebindTarget(world, company, target, source, destination);
        public Command CreateCommand(ControlActionRequest request) => _declaration.CreateCommand(request);
    }

    /// <summary>Stateless dispatch into the existing registry; no queue, handler or policy state owner.</summary>
    internal static class ControlActionDispatch
    {
        internal static void Validate(World world, IControlActionPolicy policy)
        {
            if (policy == null || !world.Content.Owns(policy) || !ReferenceEquals(world.Content.Find<IControlActionPolicy>(policy.Id), policy))
                throw new ContentException("explicit actual catalog-owned action policy required");
            try { ContractIdentity.ValidateId(policy.CommandId, "action command"); ContractIdentity.ValidateId(policy.CommandOwner, "action command owner"); }
            catch (ArgumentException ex) { throw new ContentException(policy.Id + ": " + ex.Message); }
            Type targetContract = policy.TargetKind == ControlTargetKind.Entity ? typeof(IEntityControlCommand)
                : policy.TargetKind == ControlTargetKind.Company ? typeof(ICompanyControlCommand) : null;
            if (targetContract == null || !world.Commands.TryGet(policy.CommandId, out var entry) || entry.Owner != policy.CommandOwner
                || !targetContract.IsAssignableFrom(entry.Type))
                throw new ContentException(policy.Id + ": missing/foreign normal typed control command");
            policy.ValidateComposition(world);
            if (policy.ContentDependencies == null) throw new ContentException(policy.Id + ": explicit action content dependencies required");
            foreach (var dependency in policy.ContentDependencies)
                if (!world.Content.Owns(dependency)) throw new ContentException(policy.Id + ": foreign action content dependency");
        }

        internal static void CopyTargets(World world, PlayerId company, Entity entity, IControlActionPolicy policy, IList<Code> destination)
        {
            var targets = EntityObservation.ReadProvider(world, policy, entity, () => {
                var values = new List<Code>();
                policy.CopyTargetIds(world, company, entity?.Id ?? EntityId.None, values);
                return values;
            }, observeTargetState: true);
            targets.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            for (int i = 0; i < targets.Count; i++)
            {
                ContractIdentity.ValidateId(targets[i].Id, "control target");
                if (i > 0 && targets[i] == targets[i - 1]) throw new InvalidOperationException("action provider returned duplicate target identities");
            }
            foreach (var target in targets) destination.Add(target);
        }

        private static T Pure<T>(World world, AutomationSettings settings, object provider, Func<T> read)
        {
            return EntityObservation.ReadParticipants(world, provider, settings.ResolveParticipants(world), read, space: world.Space);
        }

        internal static Code CheckBinding(World world, PlayerId company, AutomationSettings settings, ControlAction action)
        {
            if (!world.Players.IsSeat(company)) return Codes.NotSeated;
            var policy = world.Content.Get<IControlRule>(settings.Profile).ActionPolicy;
            if (policy.TargetKind != settings.Target.Kind) return AutomationCodes.InvalidSettings;
            if (settings.Target.Kind == ControlTargetKind.Entity)
            {
                Entity target = world.Entities.Get(settings.Target.Entity);
                if (target == null) return Codes.NotFound;
                if (target.Owner != company) return Codes.NotOwned;
            }
            return Pure(world, settings, policy, () => policy.CheckBinding(world, company, settings.Target, action));
        }

        internal static Code Prepare(World world, PlayerId company, AutomationSettings settings, ControlAction action,
            out CommandRegistry.Entry entry, out Command command)
        {
            entry = null; command = null;
            Code refusal = CheckBinding(world, company, settings, action); if (!refusal.IsNone) return refusal;
            var policy = world.Content.Get<IControlRule>(settings.Profile).ActionPolicy;
            if (!world.Commands.TryGet(policy.CommandId, out entry) || entry.Owner != policy.CommandOwner)
                throw new InvalidOperationException("control action lost its declared normal command owner");
            var request = new ControlActionRequest(company, settings.Target, action, world.Clock.StepIndex);
            Command authored = Pure(world, settings, policy, () => policy.CreateCommand(request));
            Type declared = entry.Type;
            if (!Pure(world, settings, (object)authored ?? policy, () => authored != null && authored.GetType() == declared && MatchesTarget(authored, company, settings.Target)
                && authored.Player == PlayerId.None && authored.Stream.IsNone && authored.IssuedTick == 0 && authored.Sequence == 0))
                throw new InvalidOperationException("action provider returned a null, foreign, redirected or stamped control command");
            Command detached = Pure(world, settings, authored, () => world.Commands.Copy(authored));
            if (!Pure(world, settings, detached, () => MatchesTarget(detached, company, settings.Target)))
                throw new InvalidOperationException("control command codec changed its actual target");
            detached.Player = company; detached.IssuedTick = request.StepIndex;
            var handler = entry.Handler;
            ulong before = Pure(world, settings, detached, () => Fingerprint(detached));
            refusal = Pure(world, settings, handler, () => handler.Check(world, detached));
            if (!Pure(world, settings, detached, () => Fingerprint(detached) == before && detached.Player == company
                && detached.Stream.IsNone && detached.IssuedTick == request.StepIndex && detached.Sequence == 0
                && MatchesTarget(detached, company, settings.Target)))
                throw new InvalidOperationException("normal control admission changed its command payload or sender");
            command = detached; return refusal;
        }
        private static ulong Fingerprint(Command command)
        { var hash = new StateHasher(); command.Sync(hash); return hash.Value; }
        private static bool MatchesTarget(Command command, PlayerId company, ControlBinding target)
            => target.Kind == ControlTargetKind.Entity ? command is IEntityControlCommand entity && entity.TargetEntity == target.Entity
                : target.Kind == ControlTargetKind.Company && command is ICompanyControlCommand seat && seat.TargetCompany == company;
    }
}
