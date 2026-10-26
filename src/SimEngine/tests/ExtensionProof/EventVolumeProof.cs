using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentEventDispatchPolicyDef : Def, IEventDispatchPolicy
    {
        private EventDispatchPolicyDeclaration _declaration;
        public long? MaximumEventsPerDispatch => _declaration.MaximumEventsPerDispatch;
        protected override void ReadCore(JsonReader reader) => _declaration = EventDispatchPolicyDeclaration.Read(reader);
    }
    public readonly struct VolumeEvent
    {
        public readonly PlayerId Company;
        public readonly int Order, Sequence, Count;
        public readonly bool Chain;
        public VolumeEvent(PlayerId company, int order, int sequence, int count, bool chain)
        { Company = company; Order = order; Sequence = sequence; Count = count; Chain = chain; }
    }
    /// <summary>Authored engine-side completion extension, not driver logic or another work scheduler.</summary>
    public sealed class EventBurstEffectDef : Def, IWorkEffect, IWorkEffectPlanValidator
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        private int _count; private bool _chain;
        protected override void ReadCore(JsonReader reader)
        {
            _count = reader.Int("count"); string mode = reader.String("mode");
            if (_count <= 0 || mode != "bulk" && mode != "chain") throw reader.Error("positive count and explicit bulk/chain mode required");
            _chain = mode == "chain";
        }
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
        {
            if (node != null || !plan.Graph.HasMachineWork(null)) throw new ContentException("event burst requires an earned whole physical order");
        }
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
        {
            int count = _chain ? 1 : _count;
            for (int i = 0; i < count; i++) world.Events.Publish(new VolumeEvent(order.Owner, order.Id, checked(i + 1), _count, _chain));
            return WorkEffectResult.Applied;
        }
    }
    /// <summary>One proof module's actual downstream event state, not an engine queue/progress duplicate.</summary>
    public sealed class EventVolumeState : IPersistentService
    {
        private readonly long[] _counts;
        private readonly int[] _orders, _sequences, _late;
        public EventVolumeState(int seats)
        { _counts = new long[seats]; _orders = new int[seats]; _sequences = new int[seats]; _late = new int[seats]; }
        public string Id => "proof.event-volume.receipts";
        public int StateSchemaVersion => 1;
        internal long Count(PlayerId company) => _counts[company.Value];
        internal int Late(PlayerId company) => _late[company.Value];
        internal void Record(VolumeEvent e)
        {
            int company = e.Company.Value;
            if (company < 0 || company >= _counts.Length || e.Order <= 0 || e.Sequence <= 0 || e.Sequence > e.Count)
                throw new InvalidOperationException("invalid actual event receipt");
            if (e.Order != _orders[company])
            { if (e.Order <= _orders[company] || e.Sequence != 1) throw new InvalidOperationException("noncanonical earned event order"); _orders[company] = e.Order; _sequences[company] = 0; _late[company] = 0; }
            if (e.Sequence != checked(_sequences[company] + 1)) throw new InvalidOperationException("missing/duplicate event sequence");
            _counts[company] = checked(_counts[company] + 1); _sequences[company] = e.Sequence;
        }
        internal void RecordLate(VolumeEvent e) => _late[e.Company.Value] = e.Sequence;
        public void Sync(IStateSync sync)
        {
            if (sync.Count(_counts.Length) != _counts.Length) throw new CorruptStateException("event receipt seat identity differs");
            for (int i = 0; i < _counts.Length; i++)
            {
                sync.Long(ref _counts[i]); sync.Int(ref _orders[i]); sync.Int(ref _sequences[i]); sync.Int(ref _late[i]);
                if (_counts[i] < 0 || _orders[i] < 0 || _sequences[i] < 0 || _late[i] < 0 || _late[i] > _sequences[i])
                    throw new CorruptStateException("invalid saved event receipts");
            }
        }
    }
    public sealed class EventVolumeView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        private readonly EventVolumeState _state;
        public EventVolumeView(World world, PlayerId viewer, EventVolumeState state)
        { _world = world; _viewer = viewer; _state = state; }
        private bool Visible(PlayerId company)
        {
            if (_world.Fault != null) throw new InvalidOperationException("event receipts unavailable after engine fault: " + _world.Fault);
            return _world.Players.IsSeat(company) && (!_viewer.IsValid || _viewer == company);
        }
        public long ReadCount(PlayerId company)
            => Visible(company) ? _state.Count(company) : 0;
        public int ReadLate(PlayerId company)
            => Visible(company) ? _state.Late(company) : 0;
    }
    public sealed class EventVolumeModule : GameModule
    {
        public override string Id => "proof.event-volume";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public InitializeProbe Initialize { get; } = new InitializeProbe("proof.event-volume.initialize");
        // Negative terminal instrumentation; never selected positive game rules.
        public EventVolumeState ActualState;
        private EventBus _events;
        public bool PendingAfterFailure;
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<EventBurstEffectDef>("event_burst_effects");
            types.Section<IndependentEventDispatchPolicyDef>("independent_event_dispatch_policies");
        }
        public override void Compose(WorldComposer composer)
        {
            var state = new EventVolumeState(composer.World.Players.Count); ActualState = state;
            _events = composer.Events;
            composer.Services.Bind(state);
            composer.Events.Register<VolumeEvent>("proof.event-volume.event", 1);
            composer.Events.Subscribe<VolumeEvent>("proof.event-volume.first", e => {
                state.Record(e);
                if (e.Chain && e.Sequence < e.Count)
                    try { composer.Events.Publish(new VolumeEvent(e.Company, e.Order, checked(e.Sequence + 1), e.Count, true)); }
                    catch { PendingAfterFailure = composer.Events.HasPending; throw; }
            });
            composer.Events.Subscribe<VolumeEvent>("proof.event-volume.last", state.RecordLate);
            composer.AddSystem(Initialize, 303);
            composer.AddViewFeature("proof.event-volume.view", 1, (world, viewer) => new EventVolumeView(world, viewer, state));
        }
        public long RefusedCount => ActualState.Count(new PlayerId(0));
        public int RefusedLate => ActualState.Late(new PlayerId(0));
        public bool HasPendingAfterFailure => _events.HasPending;
    }
}
