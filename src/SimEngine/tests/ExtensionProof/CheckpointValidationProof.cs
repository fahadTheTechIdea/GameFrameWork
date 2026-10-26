using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.ExtensionProof
{
    // Public-only lifecycle/placement fault probes. No gameplay state, queue or timing implementation.
    public sealed class CheckpointProbeDef : ComponentDef<CheckpointProbe>
    {
        public string Fault { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Fault = reader.String("fault"); if (Fault != "none" && Fault != "touch") throw reader.Error("unknown checkpoint probe");
        }
    }
    public sealed class CheckpointProbe : Component<CheckpointProbeDef>, IEntityCheckpointValidation
    {
        public void ValidateCheckpoint()
        {
            if (Entity.Get<ITilePresence>()?.Placed != true) throw new CorruptStateException("checkpoint ran before presence finished decoding");
            if (Def.Fault == "touch") Entity.Touch();
        }
    }
    public sealed class TileConstraintProbeDef : ComponentDef<TileConstraintProbe>, ITilePlacementConstraint
    {
        public string Fault { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Fault = reader.String("fault"); if (Fault != "none" && Fault != "touch-replacing") throw reader.Error("unknown tile placement probe");
        }
        public Code CheckTilePlacement(World world, EntityDef definition, TilePlacement placement, Entity replacing)
        {
            if (Fault == "touch-replacing") replacing?.Touch(); return Code.None;
        }
    }
    public sealed class TileConstraintProbe : Component<TileConstraintProbeDef> { }
}
