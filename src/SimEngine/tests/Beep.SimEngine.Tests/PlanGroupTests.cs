using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class PlanGroupTests
    {
        public static void Register()
        {
            TestRunner.Add("Plan groups: whole admission, ordering and private contracts", Contracts);
            TestRunner.Add("Plan groups: physical pause, cancellation, repeat and saved commands", Physical);
        }
        private static GameFactory Factory()
        {
            JsonValue basis=Content.Json.Parse(PointGame.Definitions);basis.Get("scenarios")[0].Set("seats",JsonValue.Number("2"));
            basis.Get("entities")[2].Get("components").Set("work_policy",Content.Json.Parse("{\"profile\":\"shared.machine\"}"));
            var text=new StringBuilder();basis.WriteCanonical(text);
            return PointGame.Factory(text.ToString(),FrameworkProofContent.Read("emergency-preemption.json"),new PlanProofModule(),
                "{\"id\":\"proof.plans\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"plan-groups.json\"]}",FrameworkProofContent.Read("plan-groups.json"));
        }
        private static SimGame New(GameFactory factory)
        {var request=new NewGameRequest{Scenario="points",Seed=917};request.Seats.Add(Controller.Human);request.Seats.Add(Controller.Human);request.Mods.Add("proof.plans");request.Mods.Add("sim.incidents");return factory.NewGame(request);}
        private static WorkView View(SimGame game,int seat=0)=>game.Seat(seat).View.Feature<WorkView>();
        private static List<PlanView> Group(SimGame game,string group,int seat=0)
        {var result=new List<PlanView>();View(game,seat).CopyPlanGroup(group,result);return result;}
        private static List<PlanView> Plans(SimGame game)
        {var result=new List<PlanView>();View(game).CopyPlans(result);return result;}
        private static PlanIntent Intent(SimGame game,string group,string activity="group.travel",string target="source",int priority=40,bool paused=true)
            =>new PlanIntent("activity:"+activity,Content.Json.Parse("{\"target\":"+Find(game,target).Id.Value+"}"),group,priority,paused);
        private static PlanGroupIntent Pair(SimGame game,string group,bool paused=true)
            =>new PlanGroupIntent(group,new[]{Intent(game,group,paused:paused),Intent(game,group,"group.inspection",paused:paused)});
        private static CommandResult Submit(Check c,SimGame game,PlanGroupIntent intent)
        {var result=TestKit.Order(game,0,new SubmitPlanGroupCommand{Intent=intent});c.That(result.Accepted,"ordinary whole group submission accepted");return result;}
        private static void Edit(Check c,SimGame game,string group,bool paused,int priority=40)
            =>c.That(TestKit.Order(game,0,new ConfigurePlanGroupCommand{Group=group,Priority=priority,Paused=paused}).Accepted,"ordinary whole group control accepted");
        private static void Cancel(Check c,SimGame game,string group)
            =>c.That(TestKit.Order(game,0,new CancelPlanGroupCommand{Group=group}).Accepted,"ordinary whole group cancellation accepted");
        private static void Until(SimGame game,Func<bool> done,string stage,SimGame twin=null)
        {
            for(int i=0;i<800&&!done();i++){Tick(game);if(twin!=null)Tick(twin);}
            if(!done())throw new InvalidOperationException(stage+" timeout tick="+game.Tick+" plans="+string.Join(";",Plans(game).ConvertAll(p=>p.Id+"/"+p.Group+"/"+p.State+"/"+p.Blocker)));
        }
        private static void Contracts(Check c)
        {
            GameFactory factory=Factory();SimGame game=New(factory);WorkBoard board=Board(game);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build,"current group command semantics");c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat,"command table breaks saves directly");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, board.StateSchemaVersion,"existing native plan/group records retain their sole codec");
            c.That(ReferenceEquals(board,game.World.Services.Get<IPlanBoard>()),"group contract is same WorkBoard owner");
            c.Throws<ArgumentException>(()=>new PlanGroupIntent("",new[]{Intent(game,"alpha")}),"explicit nonempty group required");
            c.Throws<ArgumentException>(()=>new PlanGroupIntent("alpha",Array.Empty<PlanIntent>()),"empty submission has no inferred entries");
            c.Throws<ArgumentException>(()=>new PlanGroupIntent("alpha",new[]{Intent(game,"beta")}),"entry must explicitly name its group");
            var list=new List<PlanIntent>{Intent(game,"alpha"),Intent(game,"alpha","group.inspection")};var input=new PlanGroupIntent("alpha",list);list.Clear();
            c.Equal(2,input.Entries.Count,"input captures ordered immutable membership");
            ulong hash=game.Hash();var preview=View(game).PreviewPlanGroup(input);
            c.That(preview.Ready&&preview.Entries.Count==2&&preview.Entries[0].Plan==0,"whole preflight reports each unallocated entry");c.Equal(hash,game.Hash(),"group preflight is pure");
            var bad=new PlanGroupIntent("bad",new[]{Intent(game,"bad"),Intent(game,"bad","missing")});
            var refused=View(game).PreviewPlanGroup(bad);c.That(refused.Blocker==Codes.UnknownDef&&refused.Entries[0].Blocker.IsNone&&refused.Entries[1].Blocker==Codes.UnknownDef,"late invalid intent returns per-entry refusal");
            c.Equal(refused.Blocker,TestKit.Order(game,0,new SubmitPlanGroupCommand{Intent=bad}).Refusal,"whole invalid group preview/command parity");c.Equal(0,Plans(game).Count,"refused last entry creates no earlier prefix");
            var duplicate=new PlanGroupIntent("dup",new[]{Intent(game,"dup"),Intent(game,"dup")});
            c.Equal(PlanCodes.Duplicate,View(game).PreviewPlanGroup(duplicate).Blocker,"joint provider identities detect within-set duplicates");
            c.Equal(PlanCodes.Duplicate,TestKit.Order(game,0,new SubmitPlanGroupCommand{Intent=duplicate}).Refusal,"duplicate set command grants no prefix");
            c.Equal(0,Plans(game).Count,"refused duplicate allocates no plans");
            var wrongSite=new PlanGroupIntent("wrong",new[]{Intent(game,"wrong"),Intent(game,"wrong","group.inspection","destination")});
            c.Equal(PlannedInspectionDef.WrongSite,View(game).PreviewPlanGroup(wrongSite).Blocker,"public mod provider owns actual admission rules");
            c.Equal(PlannedInspectionDef.WrongSite,TestKit.Order(game,0,new SubmitPlanGroupCommand{Intent=wrongSite}).Refusal,"provider refusal prevents the entire earlier valid prefix");
            c.Equal(0,Plans(game).Count,"no host reproduces provider site rules or creates ghost plans");
            var submitted=Submit(c,game,input);var alpha=Group(game,"alpha");
            c.That(submitted.Arg0==1&&submitted.Arg1==2&&alpha[0].Id==1&&alpha[1].Id==2,"first success proves previous refusals allocated no IDs");
            c.That(alpha.TrueForAll(p=>p.State==PlanState.Pending&&p.Order==0&&p.Paused),"submitted paused group creates no physical execution");
            c.Equal(PlanCodes.GroupExists,View(game).PreviewPlanGroup(input).Blocker,"same company group history cannot be overwritten");
            c.That(Group(game,"alpha",1).Count==0,"foreign group facts reveal no member IDs");
            var foreign=View(game,1).PreviewPlanGroupCancel("alpha");c.That(foreign.Blocker==Codes.NotFound&&foreign.Entries.Count==0,"foreign label does not reveal existence");
            c.Equal(Codes.NotFound,TestKit.Order(game,1,new CancelPlanGroupCommand{Group="alpha"}).Refusal,"foreign command uses same scoped lookup");
            c.Equal(Codes.InvalidArgument,View(game).PreviewPlanGroupEdit("alpha",-1,false).Blocker,"invalid group priority preflights all entries");
            c.Equal(Codes.NotFound,View(game).PreviewPlanGroupEdit("absent",1,true).Blocker,"unknown group refuses without invented membership");
            c.Equal(Codes.InvalidArgument,View(game).PreviewPlanGroupMove("alpha",alpha[0].Id).Blocker,"group cannot move before its own member");
            Edit(c,game,"alpha",true,int.MaxValue);
            c.That(Group(game,"alpha").TrueForAll(p=>p.Priority==int.MaxValue),"whole priority has no arbitrary ceiling");
            c.That(alpha.TrueForAll(p=>p.Priority==40),"retained plan views stay detached");
            var tail=Submit(c,game,new PlanGroupIntent("tail",new[]{Intent(game,"tail","inspect","destination")}));
            c.That(TestKit.Order(game,0,new MovePlanGroupCommand{Group="alpha",Before=0}).Accepted,"whole pending group can move to tail");
            c.That(Plans(game)[0].Id==tail.Arg0&&Plans(game)[1].Id==alpha[0].Id&&Plans(game)[2].Id==alpha[1].Id,"stable block move preserves member IDs/order and other rows");
            c.That(TestKit.Order(game,0,new MovePlanGroupCommand{Group="alpha",Before=tail.Arg0}).Accepted,"whole group can move before owned pending anchor");
            c.That(Plans(game)[0].Id==alpha[0].Id&&Plans(game)[1].Id==alpha[1].Id,"explicit moved order remains stable");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"group ordering/settings save in existing plan codec");
            c.That(TestKit.Order(game,0,new ConfigurePlanCommand{Plan=alpha[1].Id,Group="split",Priority=5,Paused=true}).Accepted,"individual regroup changes existing membership");
            c.That(Group(game,"alpha").Count==1&&Group(game,"split")[0].Id==alpha[1].Id,"group selection derives current records, not cached old membership");
            Cancel(c,game,"alpha");c.That(Group(game,"split")[0].State==PlanState.Pending,"group cancellation does not touch regrouped member");
            c.Equal(Codes.AlreadyDone,View(game).PreviewPlanGroupCancel("alpha").Blocker,"fully terminal group has no live cancellation");
            c.Equal(Codes.InvalidArgument,View(game).PreviewPlanGroupRepeat("alpha","alpha").Blocker,"repeat requires explicit different generation group");
            c.Equal(PlanCodes.GroupExists,View(game).PreviewPlanGroupRepeat("alpha","tail").Blocker,"repeat cannot overwrite existing destination");
            c.Equal(PlanCodes.NotEnded,View(game).PreviewPlanGroupRepeat("split","next").Blocker,"live members forbid repeating whole group");
            var repeated=TestKit.Order(game,0,new RepeatPlanGroupCommand{Group="alpha",NewGroup="second"});c.That(repeated.Accepted&&repeated.Arg0>tail.Arg0&&repeated.Arg1==1,"terminal repeat creates fresh native intent only");
            c.That(Group(game,"alpha")[0].State==PlanState.Cancelled&&Group(game,"second")[0].State==PlanState.Active,"old history not cloned or mutated by repetition");
            var remaining=Group(game,"split");hash=game.Hash();View(game).CopyPlanGroup("",remaining);c.That(remaining.Count==0&&game.Hash()==hash,"invalid query clears old facts without mutation");
            SimGame limit=New(factory);var field=typeof(WorkBoard).GetField("_nextPlan",BindingFlags.NonPublic|BindingFlags.Instance);field.SetValue(Board(limit),long.MaxValue-1);
            c.Equal(PlanCodes.IdentityRange,View(limit).PreviewPlanGroup(Pair(limit,"overflow")).Blocker,"whole ID range admission refuses before allocation");
            c.Equal(PlanCodes.IdentityRange,TestKit.Order(limit,0,new SubmitPlanGroupCommand{Intent=Pair(limit,"overflow")}).Refusal,"overflow command creates no prefix");c.Equal(0,Plans(limit).Count,"no partially allocated last ID");
        }
        private static void Physical(Check c)
        {
            GameFactory factory=Factory();SimGame game=New(factory);Submit(c,game,Pair(game,"field"));Configure(c,game);
            game.Seat(0).Submit(new ConfigurePlanGroupCommand{Group="field",Paused=false,Priority=100});SimGame twin=factory.Load(game.Save());Tick(game);Tick(twin);
            c.Equal(game.Hash(),twin.Hash(),"saved group command inbox resumes identically");
            c.That(Group(game,"field").TrueForAll(p=>p.State==PlanState.Active&&p.Order>0&&p.Priority==100),"one group promotion uses actual separate native orders");
            c.Equal(PlanCodes.NotPending,View(game).PreviewPlanGroupMove("field",0).Blocker,"active group cannot reorder native execution through intent list");
            Until(game,()=>Assigned(game)!=null&&Assigned(game).ToilIndex>0,"actual grouped worker travel/work",twin);
            Edit(c,game,"field",true,77);Edit(c,twin,"field",true,77);var active=Assigned(game);c.That(active!=null&&active.State==JobState.Paused,"whole pause retains actual assigned machine relation");
            WorkProgress progress=Board(game).Order(active.OrderId).Node(active.NodeId).Progress;EntityId worker=active.Worker;
            for(int i=0;i<7;i++){Tick(game);Tick(twin);}c.That(Board(game).Order(active.OrderId).Node(active.NodeId).Progress.Equals(progress)&&Assigned(game).Worker==worker,"group pause earns no physical work and does not release machine");
            c.Equal(game.Hash(),twin.Hash(),"paused groups retain identical saved native state");
            Edit(c,game,"field",false,77);Edit(c,twin,"field",false,77);
            Until(game,()=>Group(game,"field").Exists(p=>p.State==PlanState.Done),"mixed terminal and active group",twin);
            c.That(Group(game,"field").Exists(p=>p.State==PlanState.Active),"group members retain independent real completion");
            c.Equal(1,View(game).PreviewPlanGroupEdit("field",88,true).Entries.Count,"bulk edit selects only current live membership");
            c.Equal(PlanCodes.NotEnded,View(game).PreviewPlanGroupRepeat("field","premature").Blocker,"mixed terminal/live group cannot clone an unfinished execution");
            Edit(c,game,"field",true,88);Edit(c,twin,"field",true,88);
            c.That(Group(game,"field").Find(p=>p.State==PlanState.Done).Priority==77,"terminal history is never overwritten by group control");
            c.That(Group(game,"field").Find(p=>p.State==PlanState.Active).Priority==88,"active priority remains its actual native order only");
            Edit(c,game,"field",false,88);Edit(c,twin,"field",false,88);
            Until(game,()=>Group(game,"field").TrueForAll(p=>p.State==PlanState.Done),"complete all real grouped work",twin);c.Equal(game.Hash(),twin.Hash(),"grouped native DAGs complete identically");
            c.That(View(game).PreviewPlanGroupRepeat("field","again").Ready,"finished group re-admits existing authored workflows");
            var repeated=TestKit.Order(game,0,new RepeatPlanGroupCommand{Group="field",NewGroup="again"});var repeatedTwin=TestKit.Order(twin,0,new RepeatPlanGroupCommand{Group="field",NewGroup="again"});
            c.That(repeated.Accepted&&repeatedTwin.Accepted&&repeated.Arg0>Group(game,"field")[1].Id,"whole repeat creates fresh intents/executions");
            Until(game,()=>Group(game,"again").TrueForAll(p=>p.State==PlanState.Done),"repeat real DAG work",twin);c.Equal(game.Hash(),twin.Hash(),"fresh repeated execution has no copied prior progress");
            Materials(c);Cargo(c);PendingInbox(c);
        }
        private static void Materials(Check c)
        {
            GameFactory factory=Factory();SimGame game=New(factory);Submit(c,game,new PlanGroupIntent("build",new[]{Intent(game,"build","group.fabricate",paused:false),Intent(game,"build","group.travel","destination",priority:1,paused:false)}));Configure(c,game);
            Until(game,()=>{var order=Board(game).Order(Group(game,"build")[0].Order);return order.Node("work").Progress.WholeTicks>0;},"group real material consumption/tool hold");
            c.Equal(Fix.FromInt(12),Find(game,"source").Get<IInventory>().Amount(Supply(game)),"physical group work consumes authored eight inputs once");
            c.Equal(Fix.FromInt(2),Find(game,"source").Get<IInventory>().ReservedOut(Supply(game)),"real tools remain protected by actual node execution");
            Edit(c,game,"build",true);SimGame twin=factory.Load(game.Save());for(int i=0;i<5;i++){Tick(game);Tick(twin);}c.Equal(game.Hash(),twin.Hash(),"saved grouped physical material pause retains real claims");
            Cancel(c,game,"build");Cancel(c,twin,"build");
            c.That(Group(game,"build").TrueForAll(p=>p.State==PlanState.Cancelled),"whole cancellation settles all native execution links");
            c.That(Find(game,"source").Get<IInventory>().ReservedOut(Supply(game)).IsZero&&Find(game,"machine").Get<IWorker>().Job==0,"cancellation releases only actual group tool/machine commitments");
            c.Equal(Fix.FromInt(12),Find(game,"source").Get<IInventory>().Amount(Supply(game)),"group cancellation never refunds already consumed inputs");c.Equal(game.Hash(),twin.Hash(),"cancelled physical group twins remain identical");
        }
        private static void Cargo(Check c)
        {
            GameFactory factory=Factory();SimGame game=New(factory);Submit(c,game,new PlanGroupIntent("supply",new[]{Intent(game,"supply","group.supplied","destination",paused:false)}));Configure(c,game);
            Until(game,()=>Find(game,"machine").Get<ITransporter>().Cargo.Amount(Supply(game)).IsPositive,"physical group supply loaded cargo");
            Edit(c,game,"supply",true);SimGame twin=factory.Load(game.Save());Fix cargo=Find(game,"machine").Get<ITransporter>().Cargo.Amount(Supply(game));
            c.That(cargo.IsPositive,"loaded supplies remain real despite parent group pause");Cancel(c,game,"supply");Cancel(c,twin,"supply");
            c.That(Group(game,"supply")[0].State==PlanState.Cancelled,"group retires requester, not acquired delivery");
            Until(game,()=>Find(game,"destination").Get<IInventory>().Amount(Supply(game))==Fix.FromInt(8),"acquired cargo still delivers physically after group cancellation",twin);
            c.That(Find(game,"machine").Get<ITransporter>().Cargo.Amount(Supply(game)).IsZero,"cargo leaves actual carrier only on real delivery");
            c.Equal(game.Hash(),twin.Hash(),"saved loaded group cancellation preserves native delivery");
        }
        private static void PendingInbox(Check c)
        {
            GameFactory factory=Factory();SimGame game=New(factory);var input=Pair(game,"pending");game.Seat(0).Submit(new SubmitPlanGroupCommand{Intent=input});SimGame twin=factory.Load(game.Save());Tick(game);Tick(twin);
            c.That(Group(game,"pending").Count==2&&Group(game,"pending").TrueForAll(p=>p.State==PlanState.Pending),"saved immutable group payload admits complete paused set");c.Equal(game.Hash(),twin.Hash(),"group submission uses sole wire codec and saved inbox");
            game.Seat(0).Submit(new CancelPlanGroupCommand{Group="pending"});twin=factory.Load(game.Save());Tick(game);Tick(twin);c.Equal(game.Hash(),twin.Hash(),"saved group cancellation uses ordinary command ordering");
            c.That(Group(game,"pending").TrueForAll(p=>p.Order==0&&p.State==PlanState.Cancelled),"pending cancellation creates no ghost project/work");
        }
    }
}
