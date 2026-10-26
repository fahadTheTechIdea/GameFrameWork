using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.ExtensionProof
{
    // Deliberately mutable getters prove that registration, not the provider, owns declarations.
    public sealed class MutableCodecState : IProbeFirst, IProbeSecond, IPersistentService
    {
        public string SuppliedId = "probe.state";
        public int SuppliedVersion = 1;
        public int IdReads, VersionReads, ReadCalls, Value;
        // One-shot negative codec instrumentation; never persistent simulation state.
        public Action BeforeWrite;
        public string Id { get { IdReads++; return SuppliedId; } }
        public int StateSchemaVersion { get { VersionReads++; return SuppliedVersion; } }
        public void Sync(IStateSync s) { if (s.Reading) ReadCalls++; else BeforeWrite?.Invoke(); s.Int(ref Value); }
    }

    public sealed class CodecPartDef : ComponentDef<CodecPart>
    {
        protected override void ReadCore(JsonReader r) { }
    }
    public sealed class CodecPart : Component<CodecPartDef>
    {
        public int Value = 5;
        public override void Sync(IStateSync s) => s.Int(ref Value);
    }

    public sealed class MutableCodecSystem : GameSystem
    {
        public string SuppliedId = "probe.system";
        public int SuppliedVersion = 1;
        public Schedule SuppliedSchedule = Schedule.Every(10, 3);
        public int IdReads, VersionReads, ScheduleReads, ReadCalls, Updates, Initializations;
        public bool Throw;
        public override string Id { get { IdReads++; return SuppliedId; } }
        public override int StateSchemaVersion { get { VersionReads++; return SuppliedVersion; } }
        public override Schedule GetSchedule(ISimulationTime time) { ScheduleReads++; return SuppliedSchedule; }
        public override void Initialize(World world) => Initializations++;
        public override void OnNewGame(World world)
        {
            EntityDef def = world.Content.Find<EntityDef>("probe.authored");
            if (def != null) world.Spawn(def, new PlayerId(0));
        }
        public override void Update(World world, TickInfo tick)
        {
            if (Throw) throw new InvalidOperationException("probe failure");
            Updates++;
            if (world.Services.TryGet<IProbeFirst>(out IProbeFirst provider) && provider is MutableCodecState state)
                state.Value = checked(state.Value + (int)tick.Span);
            ComponentList<CodecPart> parts = world.Entities.Components<CodecPart>();
            for (int i = 0; i < parts.Count; i++) parts[i].Value++;
        }
        public override void Sync(IStateSync s) { if (s.Reading) ReadCalls++; s.Int(ref Updates); }
    }
}
