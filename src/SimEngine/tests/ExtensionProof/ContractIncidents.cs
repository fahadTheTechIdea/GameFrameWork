using System;
using Beep.SimEngine;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public interface IContractIncidentDefinition : IIncidentDefinition
    {
        Code Kind { get; }
        ActivityDef Response { get; }
        IEntityStateDefinition Stopped { get; }
    }

    public abstract class ContractIncidentDefinition<TIncident> : ComponentDef<TIncident>, IContractIncidentDefinition
        where TIncident : Component, new()
    {
        public Code CapabilityId { get; private set; }
        public Code Kind { get; private set; }
        public bool Supports(Code kind) => kind == Kind;
        public ActivityDef Response { get; private set; }
        public IEntityStateDefinition Stopped { get; private set; }
        private string _response;
        protected override void ReadCore(JsonReader r)
        { CapabilityId = new Code(r.String("id")); Kind = new Code(r.String("kind")); _response = r.String("response"); }
        protected override void ResolveCore(ContentResolver c)
        {
            if (Kind.IsNone) throw c.Fail("incident kind must be authored");
            Response = c.Def<ActivityDef>(_response); Stopped = c.Def<IEntityStateDefinition>("stopped");
            ResponseWork.ValidatePlan(Response);
        }
    }

    public sealed class ContractIncidentDef : ContractIncidentDefinition<ContractIncident> { }
    public sealed class AlarmIncidentDef : ContractIncidentDefinition<AlarmIncident> { }

    public interface IMarkSink : INamedEntityCapability { void AddMarks(int amount); }

    public abstract class ContractIncident<TDef> : Component<TDef>, IIncidentResponse, IMarkSink, IEntityStateSource
        where TDef : ComponentDef, IContractIncidentDefinition
    {
        private bool _active;
        private int _episode, _completed, _marks;
        private long _tick;
        public Code CapabilityId => Def.CapabilityId;
        public int Completed => _completed;
        public int Marks => _marks;
        public IActivityDefinition ResponsePlan => Def.Response;
        public int ResponsePriority => Def.Response.Priority;
        public IncidentSnapshot ReadIncident() => _active ? IncidentSnapshot.Raised(Def.Kind, _tick, _episode) : IncidentSnapshot.Inactive();
        public bool Supports(Code kind) => kind == Def.Kind;
        public bool Raise(World world, Code kind)
        {
            if (_active || !Supports(kind)) return false;
            _active = true; _tick = world.Tick; _episode = checked(_episode + 1); Entity.Touch();
            return true;
        }
        public void Renew(World world)
        {
            if (!_active) throw new InvalidOperationException("only an active incident can renew its episode");
            _tick = world.Tick; _episode = checked(_episode + 1); Entity.Touch();
        }
        public Code CompleteResponse(World world, Order order)
        {
            Code refusal = ResponseWork.CheckCompletion(world, this, Entity, order);
            if (!refusal.IsNone) return refusal;
            _active = false; _completed++; Entity.Touch(); return Code.None;
        }
        public void AddMarks(int amount) { _marks = checked(_marks + amount); Entity.Touch(); }
        public IEntityStateDefinition GetState(EntityUpdateContext context) => _active ? Def.Stopped : null;
        public override void Sync(IStateSync sync)
        {
            sync.Bool(ref _active); sync.Int(ref _episode); sync.Long(ref _tick); sync.Int(ref _completed); sync.Int(ref _marks);
            if (sync.Reading && (_episode < 0 || (_active && (_episode == 0 || _tick < 0)))) throw new CorruptStateException("invalid contract incident episode");
        }
    }

    public sealed class ContractIncident : ContractIncident<ContractIncidentDef> { }
    public sealed class AlarmIncident : ContractIncident<AlarmIncidentDef> { }

    public sealed class MarkEffectDef : Def, IWorkEffect
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.Empty<IContentDefinition>();
        private Code _source;
        private int _amount;
        protected override void ReadCore(JsonReader r)
        { _source = new Code(r.String("source")); _amount = r.Int("amount"); if (_source.IsNone || _amount <= 0) throw r.Error("mark effect needs source and positive amount"); }
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition step)
        {
            IMarkSink sink = world.Entities.Get(order.Target)?.Get<IMarkSink>(_source);
            if (sink == null) return WorkEffectResult.Failed(IncidentCodes.NotResponse);
            sink.AddMarks(_amount); return WorkEffectResult.Applied;
        }
    }

}
