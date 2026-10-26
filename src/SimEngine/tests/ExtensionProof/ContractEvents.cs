using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.ExtensionProof
{
    public readonly struct PulseEvent
    {
        public PulseEvent(long value) { Value = value; }
        public long Value { get; }
    }
    public readonly struct EchoEvent
    {
        public EchoEvent(long value) { Value = value; }
        public long Value { get; }
    }

    /// <summary>External engine-side state; listeners persist through the ordinary service codec.</summary>
    public sealed class EventProbeState : IPersistentService
    {
        public int StateSchemaVersion => 2;
        private readonly List<long> _values = new List<long>();
        public string Id => "eventprobe.state";
        public IReadOnlyList<long> Values => _values.AsReadOnly();
        internal void Record(long value) => _values.Add(value);
        public void Sync(IStateSync sync)
        {
            int count = sync.Count(_values.Count);
            if (sync.Reading) _values.Clear();
            for (int i = 0; i < count; i++)
            {
                long value = sync.Reading ? 0 : _values[i];
                sync.Long(ref value);
                if (sync.Reading) _values.Add(value);
            }
        }
    }

    public sealed class EventProbeSystem : GameSystem
    {
        public override int StateSchemaVersion => 1;
        public override string Id => "eventprobe.publish";
        public override void Update(World world, TickInfo tick)
        {
            world.Events.Publish(new PulseEvent(checked(tick.Tick + 1)));
            world.Events.Publish(new EchoEvent(checked(10000 + tick.Tick)));
        }
    }

    /// <summary>Proves unrelated engine-side event behavior without friendship or scheduler changes.</summary>
    public sealed class EventProbeModule : GameModule
    {
        public override string Id => "eventprobe";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires
            => new[] { new ModuleRequirement("probe", new ModuleVersion(1, 0, 0)) };
        public override void Compose(WorldComposer composer)
        {
            var state = new EventProbeState();
            composer.Services.Bind(state);
            composer.Events.Register<PulseEvent>("eventprobe.pulse", 2);
            composer.Events.Register<EchoEvent>("eventprobe.echo", 3);
            composer.Events.Subscribe<PulseEvent>("eventprobe.z-first", e =>
            {
                state.Record(e.Value);
                composer.World.Events.Publish(new EchoEvent(checked(-e.Value)));
            });
            composer.Events.Subscribe<PulseEvent>("eventprobe.a-second", e => state.Record(checked(e.Value * 100)));
            composer.Events.Subscribe<EchoEvent>("eventprobe.echo-listener", e => state.Record(e.Value));
            composer.AddSystem(new EventProbeSystem(), 1);
        }
    }
}
