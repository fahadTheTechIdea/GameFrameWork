using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent profile authoring; topology/execution stay with the existing engine owners.
    public sealed class ContractStartupProfileDef : Def, IWorldSetupProfileDefinition
    {
        private string _include;
        public IReadOnlyList<string> Dependencies { get; private set; }
        public IReadOnlyList<IWorldSetupDefinition> Entries { get; } = Array.AsReadOnly(Array.Empty<IWorldSetupDefinition>());
        public IReadOnlyList<IWorldSetupProfileDefinition> Includes { get; private set; }
        public DependencyGraph<IWorldSetupDefinition> Graph { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _include = reader.String("include");
            Dependencies = Array.AsReadOnly(new[] { _include });
        }
        protected override void ResolveCore(ContentResolver content)
            => Includes = Array.AsReadOnly(new[] { content.Def<IWorldSetupProfileDefinition>(_include) });
        protected override void ValidateResolvedCore(ContentResolver content) => Graph = WorldSetupProfileDef.Compile(this);
    }

    /// <summary>Detached module instrumentation facts, never gameplay state or another catalog.</summary>
    public sealed class StartupContentContractFacts
    {
        public int Entries { get; }
        public int Profiles { get; }
        public bool NativeAlias { get; }
        public bool ExternalNodeAlias { get; }
        public bool ExternalRootAlias { get; }
        public bool OwnedAndFrozen { get; }
        public bool MissingFind { get; }
        public bool MissingGetRefused { get; }
        public bool WrongContractRefused { get; }
        public bool AmbiguousFindRefused { get; }
        public bool AmbiguousGetRefused { get; }
        public bool TableReadDetached { get; }
        public bool ProfileListsImmutable { get; }
        public StartupContentContractFacts(ResolvedContent content)
        {
            Entries = content.Count<IWorldSetupDefinition>(); Profiles = content.Count<IWorldSetupProfileDefinition>();
            var native = content.Get<IWorldSetupDefinition>("source");
            var external = content.Get<IWorldSetupDefinition>("admission.check");
            var root = content.Get<IWorldSetupProfileDefinition>("admission.contract-root");
            NativeAlias = ReferenceEquals(native, content.Get<EntitySetupDef>("source"));
            ExternalNodeAlias = !(external is WorldSetupDef) && ReferenceEquals(external, content.Get<StartupAdmissionProbeDef>("admission.check"))
                && ReferenceEquals(root.Graph.Find("admission.check"), external);
            ExternalRootAlias = !(root is WorldSetupProfileDef) && ReferenceEquals(root, content.Get<ContractStartupProfileDef>("admission.contract-root"))
                && ReferenceEquals(root.Includes[0], content.Get<IWorldSetupProfileDefinition>("points.start"));
            OwnedAndFrozen = true;
            var all = content.All<IWorldSetupDefinition>();
            foreach (var entry in all) OwnedAndFrozen &= content.Owns(entry) && ((Def)entry).IsFrozen
                && ReferenceEquals(entry, content.Get<IWorldSetupDefinition>(entry.Id));
            MissingFind = content.Find<IWorldSetupDefinition>("not-listed") == null;
            MissingGetRefused = Refused(() => content.Get<IWorldSetupDefinition>("not-listed"));
            WrongContractRefused = Refused(() => content.Get<IWorldSetupProfileDefinition>("machine"));
            AmbiguousFindRefused = Refused(() => content.Find<IContentDefinition>("source"));
            AmbiguousGetRefused = Refused(() => content.Get<IContentDefinition>("source"));
            ((IWorldSetupDefinition[])all)[0] = null;
            TableReadDetached = content.All<IWorldSetupDefinition>()[0] != null;
            try { ((IList<IWorldSetupProfileDefinition>)root.Includes)[0] = null; }
            catch (NotSupportedException) { ProfileListsImmutable = true; }
        }
        private static bool Refused(Action read)
        { try { read(); return false; } catch (ContentException) { return true; } }
    }
}

