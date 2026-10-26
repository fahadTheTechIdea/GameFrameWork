using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class ReserveTransporterDef : ComponentDef<ReserveTransporter>, ITransporterDefinition
    {
        private TransporterDeclaration _declaration;
        public IResourceHandlingProfile Handling => _declaration.Handling;
        public int Fault { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _declaration = TransporterDeclaration.Read(reader); Fault = reader.Int("fault");
            if (Fault < 0 || Fault > 2) throw reader.Error("unknown reserve transporter fault");
        }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateBinding(EntityDef owner) => TransporterDeclaration.ValidateBinding(owner);
        protected override void ValidateCore(EntityDef owner) => ValidateBinding(owner);
    }
    // Public-only engine-side capability; it owns no stock, handling timer, receipt or fuel balance.
    public sealed class ReserveTransporter : Component<ReserveTransporterDef>, ITransporter
    {
        public IInventory Cargo
        {
            get { if (Def.Fault == 1) Entity.Touch(); return Def.Fault == 2 ? null : Entity.Get<IInventory>(); }
        }
        public IResourceHandlingProfile Handling => Def.Handling;
        public Code CheckRemoval(World world) => TransporterContracts.CheckRemoval(world, this);
    }
}
