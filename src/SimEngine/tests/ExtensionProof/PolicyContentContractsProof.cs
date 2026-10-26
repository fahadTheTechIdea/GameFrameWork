using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentWorkPolicyDef : Def, IWorkPolicyDefinition
    {
        private WorkPolicyDeclaration _declaration;
        public WorkPolicySettings Initial => _declaration.Initial;
        public EntityStateBindings States => _declaration.States;
        protected override void ReadCore(JsonReader reader) => _declaration=WorkPolicyDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class IndependentRetryPolicyDef : Def, IWorkRetryPolicy
    {
        private WorkRetryDeclaration _declaration;
        protected override void ReadCore(JsonReader reader) => _declaration=WorkRetryDeclaration.Read(reader);
        public bool CanRetry(Code blocker) => _declaration.CanRetry(blocker);
        public long Delay(ISimulationTime time,int failures) => _declaration.Delay(time,failures);
    }
    public sealed class IndependentDispatchPolicyDef : Def, IWorkerDispatchPolicy, IWorkerDispatchPreemptionPolicy
    {
        private DispatchPolicyDeclaration _declaration;
        public WorkerDispatchOrdering Ordering => _declaration.Ordering;
        public IReadOnlyList<string> DispatchScopes => _declaration.DispatchScopes;
        protected override void ReadCore(JsonReader reader) => _declaration=DispatchPolicyDeclaration.Read(reader);
        public void ValidateTime(ISimulationTime time) => _declaration.ValidateTime(time);
        public long? StarvationAfter(ISimulationTime time,WorkerDispatchOffer offer) => _declaration.StarvationAfter(time,offer);
        public bool CanPreempt(WorkerDispatchOffer challenger,WorkExecutionFacts incumbent) => _declaration.CanPreempt(challenger,incumbent);
    }
    // Public engine extension: declares content and detached composition facts, never a test-side scheduler.
    public sealed class PolicyContentContractModule : WorkContentContractModule
    {
        public PolicyContentContractModule(InitializeProbe initialize) : base(initialize) { }
        public bool IndependentPolicies { get; private set; }
        public IReadOnlyList<long> RetryDelays { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Section<IndependentWorkPolicyDef>("independent_work_policies");
            types.Section<IndependentRetryPolicyDef>("independent_retry_policies");
            types.Section<IndependentDispatchPolicyDef>("independent_dispatch_policies");
        }
        public override void Compose(WorldComposer composer)
        {
            base.Compose(composer);
            var worker=composer.Content.Get<IWorkPolicyDefinition>("contract.worker");
            var retry=composer.Content.Get<IWorkRetryPolicy>("contract.retry");
            var dispatch=composer.Content.Get<IWorkerDispatchPolicy>("contract.dispatch");
            IndependentPolicies=!(worker is WorkPolicyDef)&&!(retry is WorkRetryProfileDef)&&!(dispatch is DispatchProfileDef)
                &&composer.Content.Owns(worker)&&composer.Content.Owns(retry)&&composer.Content.Owns(dispatch)
                &&ReferenceEquals(composer.Content.Get<EntityDef>("machine").Component<WorkPolicyComponentDef>().Profile,worker);
            RetryDelays=Array.AsReadOnly(new[]{retry.Delay(composer.World.Time,1),retry.Delay(composer.World.Time,2),
                retry.Delay(composer.World.Time,3),retry.Delay(composer.World.Time,int.MaxValue)});
        }
    }
}
