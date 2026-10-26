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
    internal static class WorkContentContractTests
    {
        internal const string Case="Work content contracts: independent JSON roles executors and nested requirements drive physical player plans";
        internal static void Register()=>TestRunner.Add(Case,Run);
        internal static GameFactory Factory(InitializeProbe initialize,out WorkContentContractModule proof,Action<JsonValue> edit=null,
            WorkContentContractModule extension=null, IReadOnlyDictionary<string,string> contentDocuments=null, Action<JsonValue> worldEdit=null,
            IReadOnlyList<GameModule> additionalModules=null, JsonValue additionalDeclarations=null, Action<JsonValue> scenarioEdit=null)
        {
            var document=Json.Parse(FrameworkProofContent.Read("work-contracts.json"));edit?.Invoke(document);proof=extension??new WorkContentContractModule(initialize);
            var documents=new Dictionary<string,string>{{"work-contracts.json",document.ToString()}};
            if(contentDocuments!=null)foreach(var entry in contentDocuments)documents.Add(entry.Key,entry.Value);
            return Build(initialize,out _,activity=>{
                var node=activity.Get("independent_activities")[0].Get("nodes")[1];
                node.Set("executor",JsonValue.Of("contract.staffed"));node.Set("roles",Json.Parse("[\"contract.inspect\"]"));node.Set("requirement",JsonValue.Of("contract.native-root"));
            },extension:proof,contentDocuments:documents,additionalModules:additionalModules,additionalDeclarations:additionalDeclarations,scenarioEdit:scenarioEdit,worldEdit:world=>{
                world.Get("entities")[2].Get("components").Get("worker").Get("roles").Add(JsonValue.Of("contract.inspect"));
                worldEdit?.Invoke(world);
            });
        }
        private static void Policy(Check c,SimGame game,bool enabled)
            =>Order(c,game,new ConfigureWorkPolicyCommand {Worker=Entity(game,"machine").Id,Settings=new WorkPolicySettings(true,0,
                enabled?new[]{new WorkerRoleId("haul"),new WorkerRoleId("inspect"),new WorkerRoleId("contract.inspect")}:new[]{new WorkerRoleId("haul"),new WorkerRoleId("inspect")},null,true,true,WorkAssignmentMode.All)});
        private static void Run(Check c)
        {
            var initialize=new InitializeProbe();var factory=Factory(initialize,out var proof);var game=New(factory);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current open work protocol");c.Equal(154,EngineInfo.SaveFormat,"existing execution codecs");
            c.That(proof.IndependentWorkContent&&proof.IndependentActivityContent,"direct actual role/executor/group/resource contracts with native group references; no native bases");c.Equal(1,initialize.Initializations,"Initialize once");
            var requirements=new List<WorkRequirementView>();ulong hash=game.Hash();
            c.Equal(Code.None,Work(game).PreviewOrderRequirements("contract.inspection",Plant(game),requirements),"same direct graph requirement preview");
            c.That(requirements.Any(value=>value.Requirement=="contract.inputs")&&requirements.Any(value=>value.Requirement=="contract.tools"),"native and independent groups expose actual per-entry diagnostics");
            c.Equal(hash,game.Hash(),"open contract observations pure");
            var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,3000));Policy(c,game,false);
            var intent=new PlanIntent("activity:contract.inspection",ActivityWorkerSelection.CreateArguments(Plant(game),ActivityWorkerSelection.Unbound),"contract.work",60,true);
            c.Equal(Code.None,Work(game).PreviewPlan(intent),"ordinary plan references direct executor/role/requirements");
            var accepted=TestKit.Order(game,0,new SubmitPlanCommand{Intent=intent});c.That(accepted.Accepted,"player intent accepted");long plan=accepted.Arg0;
            var twin=factory.Load(game.Save());for(int i=0;i<4;i++)Step(game,twin);c.That(Plan(game,plan).Order==0&&Stock(game)==Fix.FromInt(20),"paused intent never acquires inputs");
            Order(c,game,new ConfigurePlanCommand{Plan=plan,Group="contract.work",Priority=60,Paused=false});twin=factory.Load(game.Save());
            Until(game,twin,()=>Plan(game,plan).Order>0,"plan has native order");int id=Plan(game,plan).Order;
            for(int i=0;i<8;i++)Step(game,twin);
            c.That(Node(game,id,"inspect").Progress.WholeTicks==0&&!Node(game,id,"inspect").Worker.IsValid&&Stock(game)==Fix.FromInt(20),"declared role disabled by ordinary player policy cannot work or spend");
            Policy(c,game,true);twin=factory.Load(game.Save());
            Until(game,twin,()=>Node(game,id,"inspect").Progress.WholeTicks>0,"independent executor physically reaches target and works");var partial=Node(game,id,"inspect");
            c.That(partial.Worker.IsValid&&partial.JobId>0&&partial.SelectedRole==new WorkerRoleId("contract.inspect")&&((PointSnapshot)Entity(game,"machine").Spatial).Position==Fix.FromInt(20),"native worker resolves direct role and staffed itinerary");
            c.That(partial.Choices.Any(choice=>choice.Entry=="b.compatible")&&!partial.Choices.Any(choice=>choice.Entry=="a.unavailable"),"nested ANY chooses compatible physical input, not unavailable thirty");
            c.Equal(Fix.FromInt(18),Stock(game),"independent consume declaration spends exactly two");
            c.Equal(Code.None,View(game).ReadSignal(Plant(game),new Code("feed"),"field.stock",out var held),"same inventory source reads independent held tool receipt");c.Equal(Fix.FromInt(16),held.Value,"two real tools unavailable during work");
            var privateNodes=new List<ActivityNodeView>();c.That(!Work(game,1).TryCopyNodes(id,privateNodes)&&privateNodes.Count==0,"direct execution facts remain private");
            c.Throws<NotSupportedException>(()=>((IList<WorkRequirementChoice>)partial.Choices).Clear(),"committed alternative facts immutable");
            game.Clock.Pause();twin.Clock.Pause();hash=game.Hash();long at=game.Clock.StepIndex;Step(game,twin);c.That(game.Clock.StepIndex==at&&game.Hash()==hash,"sole paused clock advances no custom execution");game.Clock.Resume();twin.Clock.Resume();
            twin=factory.Load(game.Save());Until(game,twin,()=>Node(game,id,"finish").Progress.WholeTicks>0,"same original parallel join");
            c.That(Node(game,id,"inspect").State==NodeState.Done&&Node(game,id,"calibrate").State==NodeState.Done,"contract work satisfies authored join only after branches finish");
            Until(game,twin,()=>Plan(game,plan).State==PlanState.Done,"completed history settled by original board");
            c.Equal(Code.None,View(game).ReadSignal(Plant(game),new Code("feed"),"field.stock",out var released),"same source reads released tools");c.Equal(Fix.FromInt(18),released.Value,"holds release without duplicate stock");
            c.That(Stock(game)==Fix.FromInt(18)&&partial.State!=NodeState.Done,"no repeated consumption; old facts detached");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"contract choices and progress use unchanged codecs");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,3000)).Success,"normal policy/plan stream and physical extension execution replay");
            var fresh=New(factory);c.That(Stock(fresh)==Fix.FromInt(20)&&((PointSnapshot)Entity(fresh,"machine").Spatial).Position==Fix.FromInt(4),"sessions share immutable content, not physical execution state");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach(string fault in new[]{"role-id","role-mismatch","guard-role","mode","empty","duplicate-entry","missing","wrong","self-cycle","mutual-cycle","false-topology","executor-phase","executor-purpose","executor-duplicate","resource","empty-bill","negative-bill","use","port","missing-port"})
            {
                var initialize=new InitializeProbe();
                c.Throws<ContentException>(()=>New(Factory(initialize,out _,document=>{
                    var role=document.Get("independent_worker_roles")[0];var group=document.Get("independent_requirement_groups")[0];var executor=document.Get("independent_staffed_executors")[0];var resource=document.Get("independent_target_resources")[0];
                    if(fault=="role-id")role.Set("role",JsonValue.Of("Bad Role"));if(fault=="role-mismatch")role.Set("role",JsonValue.Of("inspect"));
                    if(fault=="guard-role")document.Get("independent_worker_guards")[0].Set("role",JsonValue.Of("missing"));
                    if(fault=="mode")group.Set("mode",JsonValue.Of("invalid"));if(fault=="empty")group.Set("entries",Json.Parse("[]"));
                    if(fault=="duplicate-entry")group.Get("entries")[1].Set("id",JsonValue.Of("a.unavailable"));
                    if(fault=="missing")group.Get("entries")[0].Set("requirement",JsonValue.Of("missing"));
                    if(fault=="wrong")group.Get("entries")[0].Set("requirement",JsonValue.Of("contract.staffed"));
                    if(fault=="self-cycle")group.Get("entries")[0].Set("requirement",JsonValue.Of("contract.any"));
                    if(fault=="mutual-cycle")group.Get("entries")[0].Set("requirement",JsonValue.Of("contract.inputs"));
                    if(fault=="false-topology")group.Set("dependencies",Json.Parse("[]"));
                    if(fault=="executor-phase")executor.Set("travel",JsonValue.Of("missing"));if(fault=="executor-purpose")executor.Set("travel",JsonValue.Of("sim.work.pickup"));if(fault=="executor-duplicate")executor.Set("travel",JsonValue.Of("sim.work.productive"));
                    if(fault=="resource")resource.Set("quantities",Json.Parse("{\"missing\":2}"));if(fault=="empty-bill")resource.Set("quantities",Json.Parse("{}"));if(fault=="negative-bill")resource.Set("quantities",Json.Parse("{\"supply\":-1}"));
                    if(fault=="use")resource.Set("use",JsonValue.Of("invent"));if(fault=="port")resource.Set("port",JsonValue.Of("Bad Port"));if(fault=="missing-port")resource.Set("port",JsonValue.Of("missing"));
                })),"early direct work-content refusal "+fault);c.Equal(0,initialize.Initializations,"before Initialize "+fault);
            }
        }
    }
}
