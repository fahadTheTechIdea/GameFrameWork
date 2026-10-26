using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public enum StartRuleFault { None, BindingClock, CheckClock, CheckTarget, CheckSwap, CommitThrow, CommitRefuse, CommitSwap }

    // Negative callback instrumentation only; no new gameplay/account/work owner.
    public sealed class ActivityStartRuleProofModule : GameModule
    {
        public override string Id => "proof.activity-start";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public StartRuleFault Fault { get; private set; }
        public int Commits { get; internal set; }
        private World _world;
        public override void DeclareContent(ContentTypes types) => types.Section<IndependentStartRuleDef>("independent_start_rules");
        public override void Compose(WorldComposer composer) { _world = composer.World; composer.Services.Bind(this); }
        public void Arm(StartRuleFault fault)
        {
            Fault = fault;
            if (fault == StartRuleFault.BindingClock) _world.Content.Get<IndependentStartRuleDef>("start.settle").ArmBinding(this);
        }
        internal void BindingRead() { if (Fault == StartRuleFault.BindingClock) ((GameClock)_world.Clock).Pause(); }
        public void InjectDirectSettlementFault()
        {
            var target = _world.Entities.All[0];
            _world.Services.Get<WorkBoard>().Post(_world.Content.Get<IAuthoredActivityDefinition>("settle"), target.Owner, target.Id, 0);
        }
    }

    public sealed class IndependentStartRuleDef : Def, IActivityStartRule
    {
        private string _activityId, _alternateId;
        private IAuthoredActivityDefinition _activity, _alternate;
        private ActivityStartRuleProofModule _bindingProbe;
        private bool _swapped;
        public IAuthoredActivityDefinition Activity { get { _bindingProbe?.BindingRead(); return _swapped ? _alternate : _activity; } }
        internal void ArmBinding(ActivityStartRuleProofModule probe) => _bindingProbe = probe;
        public IReadOnlyList<IContentDefinition> ContentDependencies { get; private set; }
        protected override void ReadCore(JsonReader reader) { _activityId = reader.String("activity"); _alternateId = reader.String("alternate"); }
        protected override void ResolveCore(ContentResolver content)
        {
            _activity = content.Def<IAuthoredActivityDefinition>(_activityId);
            _alternate = content.Def<IAuthoredActivityDefinition>(_alternateId);
            ContentDependencies = Array.AsReadOnly(new IContentDefinition[] { _activity, _alternate });
        }
        public Code Check(World world, PlayerId company, Entity target)
        {
            var probe = world.Services.Get<ActivityStartRuleProofModule>();
            switch (probe.Fault)
            {
                case StartRuleFault.CheckClock: ((GameClock)world.Clock).Pause(); break;
                case StartRuleFault.CheckTarget: target.Get<IInventory>().Take(world.Content.ResourceId("kit"), Fix.One); break;
                case StartRuleFault.CheckSwap: _swapped = true; break;
            }
            return Code.None;
        }
        public Code Commit(World world, PlayerId company, Entity target)
        {
            var probe = world.Services.Get<ActivityStartRuleProofModule>();
            if (probe.Fault == StartRuleFault.CommitThrow) throw new InvalidOperationException("proof.start-first-error");
            if (probe.Fault == StartRuleFault.CommitRefuse) return new Code("proof.start-refused");
            if (probe.Fault == StartRuleFault.CommitSwap) _swapped = true;
            probe.Commits++;
            return Code.None;
        }
    }
}
