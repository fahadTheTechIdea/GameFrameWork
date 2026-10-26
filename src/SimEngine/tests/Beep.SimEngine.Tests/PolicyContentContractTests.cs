using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ControlContentContractTests;
using static Beep.SimEngine.Tests.ActivityContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Thin authoring/player input and public observations; no World access or execution decisions.
    internal static class PolicyContentContractTests
    {
        internal const string Case="Policy content contracts: independent JSON policies control physical work and capped haul retry";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(InitializeProbe initialize,out PolicyContentContractModule proof,
            Action<JsonValue> edit=null,Action<JsonValue> scenarioEdit=null,Action<JsonValue> worldEdit=null)
        {
            var document=Json.Parse(FrameworkProofContent.Read("policy-contracts.json"));edit?.Invoke(document);
            proof=new PolicyContentContractModule(initialize);
            return WorkContentContractTests.Factory(initialize,out _,extension:proof,
                contentDocuments:new Dictionary<string,string>{{"policy-contracts.json",document.ToString()}},
                additionalModules:new GameModule[]{new DispatchPolicyModule()},
                additionalDeclarations:Json.Parse("[{\"id\":\"sim.dispatch-policy\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[]}]"),
                scenarioEdit:scenario=>{
                    scenario.Get("modules").Add(JsonValue.Of(DispatchPolicyModule.ModuleId));
                    scenario.Get("settings").Get(WorkModule.ModuleId).Set("retry",JsonValue.Of("contract.retry"));
                    scenario.Get("settings").Set(DispatchPolicyModule.ModuleId,Json.Parse("{\"profile\":\"contract.dispatch\"}"));
                    scenarioEdit?.Invoke(scenario);
                },worldEdit:world=>{
                    world.Get("entities")[2].Get("components").Get("work_policy").Set("profile",JsonValue.Of("contract.worker"));
                    worldEdit?.Invoke(world);
                });
        }
        private static void Enabled(Check c,SimGame game,bool enabled)
            =>Order(c,game,new ConfigureWorkPolicyCommand{Worker=Entity(game,"machine").Id,
                Settings=new WorkPolicySettings(enabled,7,new[]{new WorkerRoleId("haul"),new WorkerRoleId("inspect"),new WorkerRoleId("contract.inspect")},null,true,true,WorkAssignmentMode.All)});
        private static WorkJobView Job(SimGame game,int id)
        {var values=new List<WorkJobView>();Work(game).CopyJobs(values);return values.Single(value=>value.Id==id);}
        private static Fix Amount(SimGame game,string definition)
        {
            var values=new List<Facet>();game.Seat(0).View.Inspect(Entity(game,definition).Id,values);
            return values.Where(value=>value.Key==Storage.FacetStock&&value.Subject=="supply").Aggregate(Fix.Zero,(n,value)=>n+value.Value);
        }
        private static void Run(Check c)
        {
            var initialize=new InitializeProbe();var factory=Factory(initialize,out var proof);var game=New(factory);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current contract composition identity");
            c.Equal(154,EngineInfo.SaveFormat,"same physical saved execution codecs");
            c.That(proof.IndependentPolicies&&proof.IndependentWorkContent,"actual owned independent policy/work content, no native policy bases");
            c.That(proof.RetryDelays.SequenceEqual(new long[]{2,4,8,8}),"shared authored capped delay in actual session");
            c.Equal(1,initialize.Initializations,"one Initialize after admission");
            var machine=Entity(game,"machine").Id;ulong hash=game.Hash();
            c.Equal(Code.None,Work(game).TryReadPolicy(machine,out var initial),"public immutable policy view");
            c.That(!initial.Settings.Enabled&&initial.Settings.Priority==7&&initial.Settings.Allows(new WorkerRoleId("contract.inspect")),"direct JSON initial settings reach real worker");
            c.Equal(Codes.NotFound,Work(game,1).TryReadPolicy(machine,out _),"foreign company policy stays private");
            c.Throws<NotSupportedException>(()=>((IList<WorkerRoleId>)initial.Settings.AllowedRoles).Clear(),"authored role list immutable");
            c.Equal(hash,game.Hash(),"policy observations pure");
            var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,3000));
            var intent=new PlanIntent("activity:contract.inspection",ActivityWorkerSelection.CreateArguments(Plant(game),ActivityWorkerSelection.Unbound),"contract.policy",60,false);
            c.Equal(Code.None,Work(game).PreviewPlan(intent),"normal independent workflow preview");
            var accepted=TestKit.Order(game,0,new SubmitPlanCommand{Intent=intent});c.That(accepted.Accepted,"normal player submits plan");
            long plan=accepted.Arg0;var twin=factory.Load(game.Save());
            Until(game,twin,()=>Plan(game,plan).Order>0,"native execution begins");int id=Plan(game,plan).Order;
            for(int i=0;i<9;i++)Step(game,twin);
            c.That(Node(game,id,"inspect").Progress.WholeTicks==0&&Stock(game)==Fix.FromInt(20),"disabled file policy blocks physical work and input consumption");
            var queue=new List<WorkerDispatchOffer>();hash=game.Hash();Work(game).CopyAssignments(queue);
            c.That(Work(game).TryReadDispatchPolicy(out var dispatch)&&dispatch.Id=="contract.dispatch"&&dispatch.Ordering==WorkerDispatchOrdering.StarvationFirst,"selected independent policy visible through engine facts");
            c.That(dispatch.Scopes.SequenceEqual(new[]{"sim.logistics","sim.work"}),"canonical declared dispatch scopes");
            c.Throws<NotSupportedException>(()=>((IList<string>)dispatch.Scopes).Clear(),"dispatch profile facts immutable");
            c.Equal(0,queue.Count,"disabled machine has no eligible assignment, not a fabricated offer");
            c.Equal(hash,game.Hash(),"dispatch diagnostics pure");
            Enabled(c,game,true);twin=factory.Load(game.Save());
            Until(game,twin,()=>Node(game,id,"inspect").Progress.WholeTicks>0,"machine physically reaches selected work");
            var partial=Node(game,id,"inspect");
            c.That(partial.Worker==machine&&partial.SelectedRole==new WorkerRoleId("contract.inspect")&&((PointSnapshot)Entity(game,"machine").Spatial).Position==Fix.FromInt(20),"same original travel and productive execution");
            c.Equal(Fix.FromInt(18),Stock(game),"exact two physical materials consumed");
            c.Equal(Code.None,View(game).ReadSignal(Plant(game),new Code("feed"),"field.stock",out var tools),"actual inventory signal");
            c.Equal(Fix.FromInt(16),tools.Value,"real tool reservation, no duplicate inventory");
            Enabled(c,game,false);twin=factory.Load(game.Save());long progress=Node(game,id,"inspect").Progress.WholeTicks;
            for(int i=0;i<3;i++)Step(game,twin);
            c.Equal(progress,Node(game,id,"inspect").Progress.WholeTicks,"ordinary disable preserves acquired progress");
            Enabled(c,game,true);twin=factory.Load(game.Save());
            Until(game,twin,()=>Plan(game,plan).State==PlanState.Done,"same parallel graph completes after re-enable");
            c.Equal(Fix.FromInt(18),Stock(game),"no repeated consume on resume");
            c.Equal(Code.None,View(game).ReadSignal(Plant(game),new Code("feed"),"field.stock",out tools),"completion inventory signal");
            c.Equal(Fix.FromInt(18),tools.Value,"tools released by original receipt owner");
            c.That(!initial.Settings.Enabled&&partial.State!=NodeState.Done,"retained policy/progress facts detached");

            c.Equal(Fix.FromInt(20),((PointSnapshot)Entity(game,"machine").Spatial).Position,"no implicit home destination or instant return");
            c.That(game.Seat(0).View.TryFindResource("supply",out var resource),"public resource identity");
            var haul=new HaulResourcesCommand{Source=Entity(game,"source").Id,Destination=Entity(game,"destination").Id,Worker=machine,Resource=resource,Ceiling=Fix.FromInt(8),Priority=50};
            c.Equal(Code.None,game.Seat(0).Preview(haul),"normal physical haul preview");
            var hauling=TestKit.Order(game,0,haul);c.That(hauling.Accepted,"normal haul command accepted");int job=checked((int)hauling.Arg0);
            twin=factory.Load(game.Save());Until(game,twin,()=>Amount(game,"machine")==Fix.FromInt(8),"real cargo loaded");
            Fix total=Amount(game,"source")+Amount(game,"machine")+Amount(game,"destination");
            Order(c,game,new SetPointAccessCommand{Open=false});twin=factory.Load(game.Save());
            Until(game,twin,()=>Job(game,job).FailureCount==1,"actual blocked route enters retry");
            var failed=Job(game,job);
            c.That(failed.Blocker==WorkSystem.BlockerUnreachable&&failed.State==JobState.Paused&&failed.RetryRemainingTicks>0&&failed.RetryRemainingTicks<=2,"direct retry profile drives saved original job deadline");
            c.Equal(Fix.FromInt(8),Amount(game,"machine"),"route wait retains physical cargo");
            Until(game,twin,()=>Job(game,job).FailureCount==2,"second physical route failure");
            var second=Job(game,job);
            c.That(second.RetryRemainingTicks>0&&second.RetryRemainingTicks<=4,"second authored delay grows without a new clock");
            var foreign=new List<WorkJobView>();Work(game,1).CopyJobs(foreign);c.That(foreign.Count==0,"saved cargo/retry facts private");
            game.Clock.Pause();twin.Clock.Pause();hash=game.Hash();long at=game.Clock.StepIndex;Step(game,twin);
            c.That(game.Hash()==hash&&game.Clock.StepIndex==at,"paused sole clock cannot consume retry");
            game.Clock.Resume();twin.Clock.Resume();
            Order(c,game,new SetPointAccessCommand{Open=true});twin=factory.Load(game.Save());
            Until(game,twin,()=>Amount(game,"destination")==Fix.FromInt(8)&&Amount(game,"machine")==Fix.Zero,"physical route recovery unloads exactly once");
            c.Equal(total,Amount(game,"source")+Amount(game,"machine")+Amount(game,"destination"),"cargo conservation through retries and saves");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"unchanged native codecs restore policy/job/progress");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,3000)).Success,"ordinary policy/plan/haul stream replays");
            var fresh=New(factory);c.That(Stock(fresh)==Fix.FromInt(20)&&Amount(fresh,"destination")==Fix.Zero,"interleaved session state isolated");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach(string fault in new[]{"role","duplicate-role","area","state","missing-states","assignment","negative-priority","missing-enabled","retry-missing","retry-zero","retry-maximum","multiplier","empty-blockers","duplicate-blocker","dispatch-ordering","dispatch-zero","dispatch-scopes","duplicate-scope","unknown-scope","wrong-worker-profile","wrong-retry-profile","wrong-dispatch-profile"})
            {
                var initialize=new InitializeProbe();
                c.Throws<ContentException>(()=>New(Factory(initialize,out _,document=>{
                    var worker=document.Get("independent_work_policies")[0];var retry=document.Get("independent_retry_policies")[0];var dispatch=document.Get("independent_dispatch_policies")[0];
                    if(fault=="role")worker.Set("allowed_roles",Json.Parse("[\"missing\"]"));
                    if(fault=="duplicate-role")worker.Set("allowed_roles",Json.Parse("[\"haul\",\"haul\"]"));
                    if(fault=="area")worker.Set("area",JsonValue.Of("missing"));
                    if(fault=="state")worker.Get("states").Set("disabled",JsonValue.Of("missing"));
                    if(fault=="missing-states")worker.Remove("states");
                    if(fault=="assignment")worker.Set("assignment_mode",JsonValue.Of("invent"));
                    if(fault=="negative-priority")worker.Set("priority",JsonValue.Number("-1"));
                    if(fault=="missing-enabled")worker.Remove("enabled");
                    if(fault=="retry-missing")retry.Remove("initial_delay");
                    if(fault=="retry-zero")retry.Get("initial_delay").Set("numerator",JsonValue.Number("0"));
                    if(fault=="retry-maximum")retry.Get("maximum_delay").Set("numerator",JsonValue.Number("1"));
                    if(fault=="multiplier")retry.Set("multiplier",JsonValue.Number("0"));
                    if(fault=="empty-blockers")retry.Set("retry_blockers",Json.Parse("[]"));
                    if(fault=="duplicate-blocker")retry.Set("retry_blockers",Json.Parse("[\"blocker.unreachable\",\"blocker.unreachable\"]"));
                    if(fault=="dispatch-ordering")dispatch.Set("ordering",JsonValue.Of("invent"));
                    if(fault=="dispatch-zero")dispatch.Get("starvation_after").Set("numerator",JsonValue.Number("0"));
                    if(fault=="dispatch-scopes")dispatch.Remove("scopes");
                    if(fault=="duplicate-scope")dispatch.Set("scopes",Json.Parse("[\"sim.work\",\"sim.work\"]"));
                    if(fault=="unknown-scope")dispatch.Set("scopes",Json.Parse("[\"missing\"]"));
                },scenarioEdit:scenario=>{
                    if(fault=="wrong-retry-profile")scenario.Get("settings").Get(WorkModule.ModuleId).Set("retry",JsonValue.Of("contract.worker"));
                    if(fault=="wrong-dispatch-profile")scenario.Get("settings").Get(DispatchPolicyModule.ModuleId).Set("profile",JsonValue.Of("contract.retry"));
                },worldEdit:world=>{
                    if(fault=="wrong-worker-profile")world.Get("entities")[2].Get("components").Get("work_policy").Set("profile",JsonValue.Of("contract.retry"));
                })), "early direct policy refusal "+fault);
                c.Equal(0,initialize.Initializations,"refused before Initialize "+fault);
            }
        }
    }
}
