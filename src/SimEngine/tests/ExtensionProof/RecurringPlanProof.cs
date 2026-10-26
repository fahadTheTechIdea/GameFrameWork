using System;
using System.Collections.Generic;
using System.Globalization;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class ProofRecurringSourceDef : ComponentDef<ProofRecurringSource>, IRecurringPlanBindingDefinition, IRecurringOperationPolicyDefinition
    {
        public Code CapabilityId { get; private set; }
        public RecurringCadenceBasis Cadence { get; private set; }
        public RecurringTriggerMode TriggerMode { get; private set; }
        public RecurringWorkerDispatchMode WorkerDispatch { get; private set; }
        public Code OperationPolicy { get; private set; }
        public string PlanKey { get; private set; }
        private string _template;
        public IPlanTemplateContent Template { get; private set; }
        public RecurringPlanSettings Initial { get; private set; }
        public RecurringPlanBinding Binding { get; private set; }
        public string Group { get; private set; }
        public int Fault { get; private set; }
        public bool IdleOnly { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            CapabilityId = new Code(reader.String("id")); RecurringPlanBinding.ReadSelection(reader, out var plan, out _template); PlanKey = plan;
            Initial = RecurringPlanSettings.Read(reader); Fault = reader.Int("fault");
            Cadence = RecurringPlanContracts.ReadCadence(reader);
            TriggerMode = RecurringPlanContracts.ReadTriggerMode(reader);
            WorkerDispatch = RecurringPlanContracts.ReadWorkerDispatch(reader);
            OperationPolicy = RecurringPlanContracts.ReadOperationPolicy(reader);
            Binding = RecurringPlanBinding.Read(reader);
            Group = RecurringPlanBinding.ReadGroup(reader);
            IdleOnly = reader.Bool("idle_only");
            if (Fault < 0 || Fault > 18) throw reader.Error("invalid explicit recurring proof policy");
        }
        protected override void ResolveCore(ContentResolver content) => Template = content.OptionalDef<IPlanTemplateContent>(_template);
    }
    // Independent engine-side provider: actual permit readiness, no native source/gate implementation.
    public sealed class ProofRecurringSource : Component<ProofRecurringSourceDef>, IRecurringTemplateSource
    {
        private RecurringPlanSettings _settings;
        private RecurringTemplateSettings _templateSettings;
        private long _lastPosted = -1, _plan, _completed;
        public Code CapabilityId => Def.CapabilityId;
        public override void OnCreate()
        {
            _settings = Def.Initial; _lastPosted = -1; _plan = 0; _completed = 0;
            _templateSettings = Def.Template == null ? null : new RecurringTemplateSettings(PlanTemplateSelection.Snapshot(Def.Template.Template), Def.Binding.Arguments, Def.Group);
        }
        public RecurringTemplateSettings ReadTemplateSettings() => _templateSettings;
        public void ConfigureTemplate(World world, RecurringTemplateSettings settings)
        { if (Def.Fault != 14) _templateSettings = settings; Entity.Touch(); }
        public RecurringPlanSchedule ReadSchedule()
        { if (Def.Fault == 1) Entity.Touch(); return new RecurringPlanSchedule(_settings, _lastPosted, _plan, _completed); }
        public RecurringPlanRequest ReadRequest(World world)
        {
            if (Def.Fault == 15)
            {
                var grouped = Def.Binding.ReadRequest(world, this, Def.Group, _settings.Priority).GroupIntent;
                return RecurringPlanRequest.Ready(new PlanGroupIntent(grouped.Group, grouped.Entries));
            }
            if (Def.Fault == 12)
            {
                var grouped = Def.Binding.ReadRequest(world, this, Def.Group, _settings.Priority).GroupIntent;
                return RecurringPlanRequest.Ready(new PlanGroupIntent(grouped.Group, new[] { grouped.Entries[0] }));
            }
            if (Def.Fault == 10)
            {
                var arguments = Def.Binding.Arguments.Clone(); arguments.Get("placement").Set("position", JsonValue.Number("13"));
                return RecurringPlanRequest.Ready(new PlanIntent(Def.PlanKey, arguments, Def.Group, _settings.Priority, false));
            }
            int priority = RecurringPlanContracts.EffectivePriority(world, this, _settings.Priority);
            return Def.Binding.Target == RecurringPlanTarget.Self ? RecurringPlanRequest.Ready(new PlanIntent(Def.PlanKey,
                Json.Parse("{\"target\":" + (Def.Fault == 4 ? "-1" : Entity.Id.Value.ToString(CultureInfo.InvariantCulture)) + "}"), Def.Group, priority, false))
            : Def.Binding.ReadRequest(world, this, Def.Group, priority);
        }
        public Code CheckTrigger(World world)
        {
            if (Def.Fault == 2) Entity.Touch();
            if (Def.Fault == 11) ((GameClock)world.Clock).Pause();
            if (Def.Binding.Target == RecurringPlanTarget.Self && Entity.Get<IWorkPermit>()?.Enabled != true) return new Code("proof.recurring-permit");
            if (Def.IdleOnly)
            {
                var plans = new List<PlanView>(); world.Services.Get<IPlanBoard>().CopyPlans(Entity.Owner, plans);
                foreach (var plan in plans) if (plan.State == PlanState.Pending || plan.State == PlanState.Active) return new Code("proof.recurring-plan-busy");
            }
            return Code.None;
        }
        public void Configure(World world, RecurringPlanSettings settings)
        {
            _settings = Def.Fault == 6 ? new RecurringPlanSettings(settings.Enabled, settings.Priority, _settings.Interval, settings.FacilityPolicy) : settings;
            if (Def.Fault == 9) _settings = new RecurringPlanSettings(settings.Enabled, settings.Priority, settings.Interval, Def.Initial.FacilityPolicy);
            if (Def.Fault == 7) _lastPosted = 0;
            Entity.Touch();
        }
        public void RecordSubmission(World world, long plan)
        {
            if (Def.Fault == 3) return;
            _lastPosted = Def.Fault == 5 ? checked(world.Clock.StepIndex + 1) : world.Clock.StepIndex; _plan = Def.Fault == 13 ? checked(plan + 1) : plan;
            if (Def.Fault == 8) _settings = new RecurringPlanSettings(_settings.Enabled, _settings.Priority, GameDuration.Minutes(90), _settings.FacilityPolicy);
            Entity.Touch();
        }
        public override void Sync(IStateSync sync)
        {
            RecurringPlanSettings settings = RecurringPlanSettings.Sync(sync, _settings); long posted = _lastPosted, plan = _plan, completed = _completed;
            sync.Long(ref posted); sync.Long(ref plan); sync.Long(ref completed);
            RecurringTemplateSettings template = RecurringTemplateSettings.Sync(sync, _templateSettings);
            if ((template != null) != (Def.Template != null)) throw new CorruptStateException("invalid proof template binding state");
            if (!RecurringPlanContracts.IsStructurallyValid(new RecurringPlanSchedule(settings, posted, plan, completed))) throw new CorruptStateException("invalid proof source history");
            if (sync.Reading) { _settings = settings; _lastPosted = posted; _plan = plan; _completed = completed; _templateSettings = template; }
        }
        public void RecordCompletion(World world, long plan)
        {
            if (Def.Fault == 16) return;
            _completed = Def.Fault == 18 ? checked(plan + 1) : plan;
            if (Def.Fault == 17) _settings = new RecurringPlanSettings(_settings.Enabled, _settings.Priority, GameDuration.Minutes(90), _settings.FacilityPolicy);
            Entity.Touch();
        }
    }
}
