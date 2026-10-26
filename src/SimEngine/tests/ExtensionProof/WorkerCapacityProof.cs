using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class ProofWorkLoadSignalDef : ComponentDef<WorkLoadSignal>, IWorkLoadSignalDefinition
    {
        private WorkLoadSignalDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public WorkLoadScope Scope => _declaration.Scope;
        public bool IsCapacity => _declaration.IsCapacity;
        public WorkerRoleId Role => _declaration.Role;
        public int SignalSchemaVersion => 2;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => WorkLoadSignalDeclaration.Metrics;
        protected override void ReadCore(JsonReader reader) => _declaration = WorkLoadSignalDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        // The engine, not an optional mod callback, must admit the actual binding.
    }

    /// <summary>Data-only fixture projection to an explicitly registered independent kind.</summary>
    public static class WorkSignalMetadataProof
    {
        public static void SelectIndependent(JsonValue document)
        {
            JsonValue entities = document.Get("entities");
            if (entities != null) for (int i = 0; i < entities.Count; i++)
            {
                var components = entities[i].Get("components"); var source = components.Get("work_load_signal");
                if (source == null) continue;
                components.Remove("work_load_signal"); components.Set("proof_work_load_signal", source);
            }
            JsonValue signals = document.Get("signals");
            if (signals != null) for (int i = 0; i < signals.Count; i++)
            {
                var providers = signals[i].Get("providers");
                for (int j = 0; j < providers.Count; j++)
                    if (providers[j].Get("component").Text == "work_load_signal")
                        providers[j].Set("component", JsonValue.Of("proof_work_load_signal"));
            }
        }
    }

    // Registration only: reuse the actual independent policy provider, never implement fleet rules.
    public sealed class WorkerCapacityProofModule : GameModule
    {
        public override string Id => "proof.worker-capacity";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(WorkModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<ProofWorkPolicyDef>("capacity_work_policy", 2);
            types.Component<ProofWorkLoadSignalDef>("proof_work_load_signal", 1);
        }
    }
}
