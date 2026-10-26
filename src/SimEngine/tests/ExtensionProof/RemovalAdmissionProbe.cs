using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Engine-side adversarial capability only; existing world/removal/recovery owners execute gameplay.
    public sealed class RemovalAdmissionProbeDef : ComponentDef<RemovalAdmissionProbe>
    {
        public string Mode { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Mode = reader.String("mode");
            if (Mode != "normal" && Mode != "refuse" && Mode != "state" && Mode != "sibling" &&
                Mode != "clock" && Mode != "space" && Mode != "throw")
                throw reader.Error("explicit supported removal admission probe mode required");
        }
    }
    public sealed class RemovalAdmissionProbe : Component<RemovalAdmissionProbeDef>, IEntityRemovalGuard
    {
        public static readonly Code Refused = new Code("proof.removal-refused");
        private int _reads;
        public Code CheckRemoval(World world)
        {
            if (!world.Clock.Paused || Def.Mode == "normal") return Code.None;
            if (Def.Mode == "refuse") return Refused;
            if (Def.Mode == "throw") throw new InvalidOperationException("proof.original-removal-guard-error");
            if (Def.Mode == "state") _reads = checked(_reads + 1);
            if (Def.Mode == "sibling") {
                var inventory = Entity.Get<IInventory>();
                inventory.Take(inventory.Stock[0].Resource, Fix.One);
            }
            if (Def.Mode == "clock") ((GameClock)world.Clock).Speed = 2;
            if (Def.Mode == "space") ((PointSpace)world.Space).SetAccess(false);
            return Code.None;
        }
        public override void Sync(IStateSync sync)
        {
            sync.Int(ref _reads);
            if (_reads < 0) throw new CorruptStateException("negative removal probe observation counter");
        }
    }
}
