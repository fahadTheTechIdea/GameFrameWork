using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>External contract implementation deliberately changing metadata after graph capture.</summary>
    public sealed class MutableDependencyProof : IDependencyNode
    {
        private string _id;
        private IReadOnlyList<string> _dependencies;
        public int IdentityReads { get; private set; }
        public int DependencyReads { get; private set; }
        public bool RejectReads { get; set; }
        public MutableDependencyProof(string id, params string[] dependencies)
        { _id = id; _dependencies = dependencies; }
        public void Replace(string id, params string[] dependencies)
        { _id = id; _dependencies = dependencies; }
        public string Id
        {
            get
            {
                IdentityReads++;
                if (RejectReads) throw new System.InvalidOperationException("provider identity was read after capture");
                return _id;
            }
        }
        public IReadOnlyList<string> Dependencies
        {
            get
            {
                DependencyReads++;
                if (RejectReads) throw new System.InvalidOperationException("provider edges were read after capture");
                return _dependencies;
            }
        }
    }
}
