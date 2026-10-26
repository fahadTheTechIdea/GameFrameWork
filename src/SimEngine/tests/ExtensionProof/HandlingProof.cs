using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent slot-based authored handling, without job/progress/inventory ownership.</summary>
    public sealed class SlotHandlingDef : Def, IResourceHandlingProfile
    {
        private string _resourceName;
        private ResourceId _resource;
        private Fix _batch;
        private GameDuration _loading, _unloading;
        protected override void ReadCore(JsonReader reader)
        {
            _resourceName = reader.String("resource"); _batch = reader.Fix("batch");
            _loading = reader.Child("loading").RationalDuration(); _unloading = reader.Child("unloading").RationalDuration();
            if (!_batch.IsPositive || _loading.IsZero || _unloading.IsZero) throw reader.Error("explicit positive slots and durations required");
        }
        protected override void ResolveCore(ContentResolver content) => _resource = content.Resource(_resourceName);
        public ResourceHandlingCheck CheckHandling(ISimulationTime time, ResourceId resource, Fix amount, ResourceHandlingPhase phase)
        {
            if (time == null || !amount.IsPositive || phase < ResourceHandlingPhase.Loading || phase > ResourceHandlingPhase.Unloading)
                return new ResourceHandlingCheck(ResourceHandlingStatus.InvalidRequest);
            if (resource != _resource) return new ResourceHandlingCheck(ResourceHandlingStatus.UnsupportedResource);
            long slots = amount.Raw / _batch.Raw + (amount.Raw % _batch.Raw == 0 ? 0 : 1);
            try { return new ResourceHandlingCheck(ResourceHandlingStatus.Ready, time.Ticks(phase == ResourceHandlingPhase.Loading ? _loading : _unloading, slots, 1)); }
            catch (OverflowException) { return new ResourceHandlingCheck(ResourceHandlingStatus.OutOfRange); }
        }
    }
    public sealed class HandlingProofModule : GameModule
    {
        public override string Id => "proof.handling";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(LogisticsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes content) => content.Section<SlotHandlingDef>("slot_handling_profiles");
    }
}
