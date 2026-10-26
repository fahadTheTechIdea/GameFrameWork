using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Declared engine-side mod policy; no membership, codec, scheduler or test-owned gameplay.</summary>
    public sealed class RequiredNavigationFleetDef : Def, IFleetPolicy
    {
        public static readonly Code Incompatible = new Code("proof.fleet-missing-navigation");
        private string _required;
        private IReadOnlyList<string> _classes;
        public IReadOnlyList<string> NavigationClasses => _classes;
        public IReadOnlyList<IContentDefinition> ContentDependencies => Array.Empty<IContentDefinition>();
        protected override void ReadCore(JsonReader reader)
        {
            _required = reader.String("required_navigation"); var values = reader.Array("navigation_classes");
            if (string.IsNullOrWhiteSpace(_required)) throw reader.Error("explicit required navigation identity needed");
            if (values == null || values.Count == 0) throw reader.Error("explicit navigation classes required");
            var ids = new List<string>();
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i]?.Kind != JsonKind.String) throw reader.Error("string navigation identities required");
                if (string.IsNullOrWhiteSpace(values[i].Text)) throw reader.Error("explicit navigation identity needed");
                if (ids.Contains(values[i].Text)) throw reader.Error("duplicate navigation identity");
                ids.Add(values[i].Text);
            }
            ids.Sort(StringComparer.Ordinal); _classes = ids.AsReadOnly();
        }
        public void Validate(World world)
        {
            foreach (var profile in NavigationClasses) if (profile == _required) return;
            throw new ContentException(Id + ": required navigation must be declared");
        }
        public Code CheckMember(World world, Entity member)
        {
            var value = MotionNavigationContracts.Read(world, member);
            if (value != null) foreach (var allowed in value.Allowed) if (allowed == _required) return Code.None;
            return Incompatible;
        }
        public Code CheckNavigation(string profile)
        {
            foreach (var allowed in NavigationClasses) if (allowed == profile) return Code.None;
            return MotionNavigationCodes.NotAllowed;
        }
    }
    public sealed class FleetPolicyProofModule : GameModule
    {
        public override string Id => "proof.fleet-policy";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(MachineFleetsModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types) => types.Section<RequiredNavigationFleetDef>("required_navigation_fleet_policies");
    }
}
