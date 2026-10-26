using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Metadata delegates the sole engine declaration; the runtime remains the original Incident.
    public sealed class OwnerIncidentDef : ComponentDef<Incident>, IIncidentEpisodeDefinition
    {
        private IncidentEpisodeDeclaration _declaration;
        internal Action<string> Observe;
        public Code CapabilityId { get { Observe?.Invoke("identity"); return _declaration.CapabilityId; } }
        public IIncidentWorkflowDefinition Workflow { get { Observe?.Invoke("workflow"); return _declaration.Workflow; } }
        public EntityStateBindings States => _declaration.States;
        public bool Supports(Code kind) => kind == _declaration.Workflow.Kind;
        protected override void ReadCore(JsonReader reader) => _declaration = IncidentEpisodeDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateBinding(World world) => IncidentBindingContracts.ValidateEpisode(world, this);
    }
    public sealed class OwnerObservationModule : GameModule
    {
        public const string ModuleId = "proof.owner-observation";
        private World _world;
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(InventoryModule.ModuleId, Version),
            new ModuleRequirement(EquipmentConditionModule.ModuleId, Version),
            new ModuleRequirement(IncidentResponseModule.ModuleId, Version) };
        public readonly InitializeProbe Initialize = new InitializeProbe();
        public bool ActualAliases { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<IndependentStorageDef>("independent_storage", 4);
            types.Component<IndependentConditionDef>("independent_condition", 2);
            types.Component<OwnerIncidentDef>("owner_incident", 2);
        }
        public override void Compose(WorldComposer composer)
        {
            _world = composer.World; ActualAliases = true;
            foreach (string name in new[] { "binding.source", "binding.watcher", "binding.remote" })
            {
                var entity = composer.Content.Get<EntityDef>(name);
                var inventory = entity.Component<IInventoryDefinition>();
                ActualAliases &= inventory != null && inventory.ComponentType == typeof(Storage)
                    && ReferenceEquals(inventory, entity.Component<IInletPortDefinition>())
                    && entity.HasCapability<IOutletPort>() && entity.HasCapability<IInletPort>();
                foreach (var id in new[] { new Code("integrity"), new Code("reserve") })
                    ActualAliases &= entity.Component<IConditionDefinition>(id).ComponentType == typeof(EquipmentCondition);
                foreach (var id in new[] { new Code("leak"), new Code("valve") })
                    ActualAliases &= entity.Component<IIncidentEpisodeDefinition>(id).ComponentType == typeof(Incident);
            }
            if (!ActualAliases) throw new ContentException("ownership proof requires original runtime owners and actual named aliases");
            composer.AddSystem(Initialize, 12346);
        }
        // Negative-only callback injection. No normal repair, stock, scheduling or clock algorithm here.
        public void Arm(string kind, string point, string fault)
        {
            Entity entity = null;
            foreach (var current in _world.Entities.All) if (current.Def.Id == "binding.source") entity = current;
            if (entity == null) throw new InvalidOperationException("missing actual ownership probe");
            var actual = entity; bool fired = false;
            Action<string> callback = name => {
                if (name != point || fired) return; fired = true;
                if (fault == "throw") throw new InvalidOperationException("proof.owner.original");
                if (fault == "pause") ((GameClock)_world.Clock).Pause();
                if (fault == "neighbor") {
                    var condition = actual.Get<EquipmentCondition>(new Code("integrity"));
                    var value = condition.ReadCondition(); var exposure = condition.ReadExposure();
                    using (var stream = new MemoryStream()) {
                        var writer = new StateWriter(stream); Fix amount = value.Value - Fix.One;
                        long changed = value.ChangedTick == -1 ? 0 : value.ChangedTick, elapsed = exposure.ElapsedTicks, updated = exposure.UpdatedTick;
                        writer.Fix(ref amount); writer.Long(ref changed); writer.Long(ref elapsed); writer.Long(ref updated);
                        var bytes = stream.ToArray(); condition.Sync(new StateReader(bytes, 0, bytes.Length));
                    }
                }
            };
            if (kind == "condition") ((IndependentConditionDef)actual.Def.Component<IConditionDefinition>(new Code("reserve"))).Observe = callback;
            else ((OwnerIncidentDef)actual.Def.Component<IIncidentEpisodeDefinition>(new Code("valve"))).Observe = callback;
        }
    }
}
