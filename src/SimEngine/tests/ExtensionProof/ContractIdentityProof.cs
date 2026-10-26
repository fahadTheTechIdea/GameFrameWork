using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent assembly compilation of the original authoring validator, without copying its grammar.</summary>
    public static class ContractIdentityProof
    {
        public static void Validate(string identity, int schema, string kind)
            => ContractIdentity.Validate(identity, schema, kind);
        public static void ValidateId(string identity, string kind)
            => ContractIdentity.ValidateId(identity, kind);
    }
}
