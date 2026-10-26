using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.ExtensionProof
{
    // Deliberate fault injection through public contracts, not another navigation implementation.
    public sealed class TrafficClearanceProbeDef : ComponentDef<TrafficClearanceProbe>
    {
        public TileRect Cells { get; private set; }
        protected override void ReadCore(JsonReader reader)
            => Cells = new TileRect(reader.Int("x"), reader.Int("y"), reader.Int("width"), reader.Int("height"));
    }
    public sealed class TrafficClearanceProbe : Component<TrafficClearanceProbeDef>, ITileMotionClearance
    {
        private World _world;
        private int _fault, _hidden;
        private MoveStatus _status = MoveStatus.Moving;
        // Test instrumentation, never authored gameplay or a saved reservation owner.
        public void Arm(World world, int fault) { _world = world; _fault = fault; }
        public MoveStatus Status => _status;
        public void Stop() => _status = MoveStatus.Idle;
        public void Advance(World world, TickInfo tick, Fix speedModifier) { }
        public TileRect? ReadMotionClearance()
        {
            if (_fault == 0) return null;
            if (_fault == 2) throw new InvalidOperationException("proof.traffic-clearance");
            if (_fault == 3) Entity.Touch();
            if (_fault == 4) _hidden++;
            if (_fault == 5) ((GameClock)_world.Clock).Pause();
            if (_fault == 6) return new TileRect(0, 0, 3, 3);
            if (_fault == 7) _status = MoveStatus.Idle;
            return Def.Cells;
        }
        public override void Sync(IStateSync sync) { sync.Int(ref _hidden); sync.Enum(ref _status); }
    }
    public sealed class TrafficClearanceProbeModule : GameModule
    {
        public override string Id => "proof.traffic-clearance";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types) => types.Component<TrafficClearanceProbeDef>("traffic_clearance_probe", 1);
        public override void Compose(WorldComposer composer) { }
    }
}
