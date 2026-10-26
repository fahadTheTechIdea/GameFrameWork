using System;

namespace Beep.SimEngine.Kernel
{
    public enum StateCodecKind : byte { Component, System, Service }

    /// <summary>Captured declaration of the existing state owner; contains no executable provider.</summary>
    public sealed class StateCodecRegistration
    {
        internal StateCodecRegistration(StateCodecKind kind, string id, int schemaVersion, string owner, Type contract)
        {
            ContractIdentity.Validate(id, schemaVersion, "state codec");
            ContractIdentity.ValidateId(owner, "codec owner");
            Kind = kind; Id = id; SchemaVersion = schemaVersion; Owner = owner;
            ContractType = contract ?? throw new ArgumentNullException(nameof(contract));
        }
        public StateCodecKind Kind { get; }
        public string Id { get; }
        public int SchemaVersion { get; }
        public string Owner { get; }
        public Type ContractType { get; }

        internal void SyncIdentity(IStateSync s)
        {
            int kind = (int)Kind, version = SchemaVersion;
            string id = Id, owner = Owner;
            s.Int(ref kind); s.String(ref id); s.String(ref owner); s.Int(ref version);
            if (kind != (int)Kind || id != Id || owner != Owner || version != SchemaVersion)
                throw new CorruptStateException("state codec declaration differs: " + Kind + ":" + Id);
        }
    }

    internal sealed class PersistentServiceRegistration
    {
        internal PersistentServiceRegistration(IPersistentService provider, string owner)
        {
            Provider = provider;
            Metadata = new StateCodecRegistration(StateCodecKind.Service, provider.Id, provider.StateSchemaVersion, owner, provider.GetType());
        }
        internal StateCodecRegistration Metadata { get; }
        internal IPersistentService Provider { get; }
    }
}
