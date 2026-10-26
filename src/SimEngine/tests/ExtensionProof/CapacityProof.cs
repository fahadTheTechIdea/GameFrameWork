using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Ordinary engine-side content policy: the common evaluator remains the only measure algorithm.</summary>
    public sealed class TraitCapacityPolicyDef : CapacityPolicyDef
    {
        public string RequiredTrait { get; private set; }
        protected override void ReadProperties(JsonReader reader)
        {
            RequiredTrait = reader.String("required_trait");
            if (string.IsNullOrWhiteSpace(RequiredTrait)) throw reader.Error("required_trait must be authored");
        }
        protected override CapacityStatus ValidateResource(IResourceDefinition resource)
            => resource.HasTrait(RequiredTrait) ? CapacityStatus.Fits : CapacityStatus.ResourceRejected;
    }

    /// <summary>An authored consumer resolves through the actual authored contract index, even for a later-declared policy kind.</summary>
    public sealed class CapacityBindingDef : Def
    {
        private string _policy;
        public ICapacityPolicy Policy { get; private set; }
        protected override void ReadCore(JsonReader reader) { _policy = reader.String("policy"); }
        protected override void ResolveCore(ContentResolver content)
        {
            Policy = content.Def<ICapacityPolicyDefinition>(_policy);
            if (Policy.Limits.Count == 0) throw content.Fail("resolved policy needs authored limits");
            if (!Policy.Check(Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>()).Fits)
                throw content.Fail("empty capacity binding is invalid");
        }
    }

    public sealed class CapacityProofModule : GameModule
    {
        public override string Id => "proof.capacity";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires
            => new[] { new ModuleRequirement("proof.resources", Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<CapacityBindingDef>("capacity_bindings");
            types.Section<TraitCapacityPolicyDef>("trait_capacity_policies");
        }
    }
}
