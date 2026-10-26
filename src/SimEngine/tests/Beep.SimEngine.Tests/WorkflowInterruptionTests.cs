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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Pure master/JSON composition and normal player intent; no writable world or gameplay in driver.
    internal static class WorkflowInterruptionTests
    {
        internal const string Case = "Workflow interruption: earned graph progress and partial cargo survive shutdown pause reselection and cancellation";
        internal static void Register() => TestRunner.Add(Case,Run);
        private static WorkView Work(SimGame g,int seat=0) => g.Seat(seat).View.Feature<WorkView>();
        private static List<ActivityNodeView> Nodes(SimGame g,int order)
        { var rows=new List<ActivityNodeView>();Work(g).TryCopyNodes(order,rows);return rows; }
        private static ActivityNodeView Node(SimGame g,int order,string node) => Nodes(g,order).Single(x=>x.Id==node);
        private static List<WorkJobView> Jobs(SimGame g)
        { var rows=new List<WorkJobView>();Work(g).CopyJobs(rows);return rows; }
        private static bool Live(WorkJobView j) => j.State==JobState.Open||j.State==JobState.Assigned||j.State==JobState.Paused;
        private static WorkJobView Job(SimGame g,int id) => Jobs(g).Single(j=>j.Id==id);
        private static PlanView Plan(SimGame g,long id)
        { var rows=new List<PlanView>();Work(g).CopyPlans(rows);return rows.Single(x=>x.Id==id); }
        private static bool Exists(SimGame g,string def) => g.Seat(0).Snapshot.Entities.Any(x=>x.Def==def);
        private static bool Completed(SimGame g,string plan) => Work(g).TryReadActivityCompletion(plan,EntityId.None,out _);
        private static Fix Position(SimGame g,string name)
            => ((PointSnapshot)g.Seat(0).Snapshot.Entities.Single(x=>x.Id==Entity(g,name)).Spatial).Position;
        private static ResourceId Resource(SimGame g)
        { if(!g.Seat(0).View.TryFindResource("supply",out var r))throw new InvalidOperationException("missing supply");return r; }
        private static LogisticsOperationalSnapshot Facts(SimGame g,string name)
        {
            var refusal=g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(Entity(g,name),Resource(g),out var value);
            if(!refusal.IsNone)throw new InvalidOperationException("stock facts "+name+"/"+refusal);return value;
        }
        private static Fix Total(SimGame g) => Stock(g,"source")+Stock(g,"destination")+Stock(g,"machine");
        private static CommandResult Result(SimGame g,int seq)
        { long cursor=0;var rows=new List<CommandResult>();g.Seat(0).ReadResults(ref cursor,rows);return rows.Single(x=>x.Sequence==seq); }
        private static int Queue(Check c,SimGame g,Command command)
        { ulong before=g.Hash();c.Equal(Code.None,g.Seat(0).Preview(command),"normal command preview");c.Equal(before,g.Hash(),"preview pure");return g.Seat(0).Submit(command); }
        private static CommandResult Submit(Check c,SimGame g,Command command)
        { int sequence=Queue(c,g,command);Step(g);var result=Result(g,sequence);c.That(result.Accepted,"normal engine command accepted "+result.Refusal);return result; }
        private static SimGame Save(Check c,GameFactory f,SimGame g)
        { var twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"actual graph/job/stock/clock save");return twin; }
        private static void Reach(SimGame g,SimGame twin,Func<bool> done,string stage)
        {
            var trail=new Queue<string>();
            for(int i=0;i<512&&!done();i++)
            {
                trail.Enqueue(g.Clock.StepIndex+":stock="+Total(g)+":jobs="+string.Join(";",Jobs(g).Select(j=>j.Id+"/"+j.State+"/"+j.Worker+"/"+j.ToilIndex+"/"+j.Loaded+"/"+j.Blocker)));
                if(trail.Count>16)trail.Dequeue();Step(g,twin);
            }
            if(!done())throw new InvalidOperationException(stage+" exceeded512Updates "+string.Join(" | ",trail));
        }
        private static void Policy(Check c,SimGame g,string worker,bool enabled,bool builder=false)
            => Submit(c,g,new ConfigureWorkPolicyCommand {Worker=Entity(g,worker),Settings=new WorkPolicySettings(enabled,0,
                new[]{new WorkerRoleId(builder?"extension.builder":"haul")},null,true,true,WorkAssignmentMode.All)});
        private static void Band(Check c,SimGame g,Fix desired,bool enabled)
            => Submit(c,g,new ConfigureStockPolicyCommand {Store=Entity(g,"destination"),Resource=Resource(g),
                Change=LogisticsDemandChange.Replace(new LogisticsDemandSettings(enabled,desired,desired,enabled?desired:Fix.FromInt(40),30,LogisticsReserveAccess.Protected))});
        private static GameFactory Factory(bool direct,bool half)
        {
            var add=Json.Parse(FrameworkProofContent.Read("workflow-interruption.json"));
            if(direct)for(int i=0;i<2;i++)
            {
                var parts=add.Get("entities")[i].Get("components");var spec=parts.Get("worker");
                spec.Set("role",spec.Get("roles")[0]);spec.Remove("roles");
                spec.Set("rate",spec.Get("work_rate"));spec.Remove("work_rate");spec.Get("states").Remove("loading");
                parts.Remove("worker");parts.Set("contract_worker",spec);
            }
            var declarations=Json.Parse("[{\"id\":\"proof.workflow-extension\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.construction\",\"version\":\"1.0.0\"},{\"id\":\"points\",\"version\":\"1.0.0\"},{\"id\":\"sim.world-setup\",\"version\":\"1.0.0\"}],\"documents\":[\"workflow-extension.json\",\"workflow-extension-inputs.json\",\"workflow-interruption.json\"]},{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
            var documents=new Dictionary<string,string> {
                {"workflow-extension.json",FrameworkProofContent.Read("workflow-extension.json")},
                {"workflow-extension-inputs.json",FrameworkProofContent.Read("workflow-extension-inputs.json")},
                {"workflow-interruption.json",add.ToString()} };
            return ConstructionPlanTests.Points(extraModules:new GameModule[]{new WorkflowExtensionModule(),new LogisticsRecoveryPolicyModule()},
                extraDeclarations:declarations.ToString(),extraDocuments:documents,startupProfiles:new[]{"lifecycle.start"},
                scenarioEdit:s=>{
                    s.Get("modules").Add(JsonValue.Of(WorkflowExtensionModule.ModuleId));s.Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                    s.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId,Json.Parse("{\"profile\":\"lifecycle.recovery\"}"));
                },worldEdit:world=>{
                    StartupContentAuthoring.Position(world,"machine",JsonValue.Number("4"));
                    if(half)world.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));
                });
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current sole engine identity");
            foreach(bool direct in new[]{false,true})foreach(bool half in new[]{false,true})foreach(bool cancel in new[]{false,true})Scenario(c,direct,half,cancel);
        }
        private static void Scenario(Check c,bool direct,bool half,bool cancel)
        {
            var f=Factory(direct,half);var g=ConstructionPlanTests.NewPoints(f);var idle=ConstructionPlanTests.NewPoints(f);ulong idleHash=idle.Hash();
            var limits=new ReplayLimits(4*1024*1024,6000);var recording=g.RecordReplay(limits);
            c.That(!g.Modules.Contains(TycoonModule.ModuleId),"work/manufacturing/recovery independent of business");
            Policy(c,g,"machine",true);Submit(c,g,new SetHomeCommand{Worker=Entity(g,"machine"),Home=Entity(g,"source")});
            Policy(c,g,"lifecycle.a",true,true);
            long plan=Submit(c,g,new SubmitPlanCommand{Intent=new PlanIntent("build:extension.facility",
                Json.Parse("{\"placement\":{\"space\":\"field.points\",\"position\":30}}"),"interruption",70,false)}).Arg0;
            var twin=Save(c,f,g);Reach(g,twin,()=>Plan(g,plan).Order>0,"actual planned site");int order=Plan(g,plan).Order;
            Reach(g,twin,()=>Node(g,order,"materials").State==NodeState.Done,"actual eight hauled and consumed");
            Submit(c,g,new PostOrderCommand{Plan="extension.calibrate",Target=Entity(g,"lifecycle.a")});twin=Save(c,f,g);
            Reach(g,twin,()=>Node(g,order,"assemble").Progress.WholeTicks>=2,"actual first builder assembly");
            var retained=Node(g,order,"assemble");c.Equal(Entity(g,"lifecycle.a"),retained.Worker,"first actual actor earns work");
            c.Equal(Fix.FromInt(30),Position(g,"lifecycle.a"),"first builder reached physical site");
            c.That(Node(g,order,"cure").Progress.WholeTicks>0&&Node(g,order,"finish").Progress.Equals(WorkProgress.Zero),"parallel cure is earned, finish not early");
            Submit(c,g,new ConfigureOperationCommand{Entity=Entity(g,"lifecycle.a"),Operation=new Code("availability"),Enabled=false,Target=Fix.One});
            c.Equal(retained.Progress,Node(g,order,"assemble").Progress,"shutdown does not rewind or earn assembly");
            c.That(!Node(g,order,"assemble").Worker.IsValid&&Node(g,order,"assemble").Blocker!=Code.None,"unavailable actor releases actual assignment with typed wait");
            Submit(c,g,new ConfigurePlanCommand{Plan=plan,Group="interruption",Priority=70,Paused=true});
            Submit(c,g,new ConfigureWorkPolicyCommand{Worker=Entity(g,"lifecycle.a"),Settings=new WorkPolicySettings(true,0,
                new[]{new WorkerRoleId("extension.builder")},null,true,true,WorkAssignmentMode.EmergencyOnly)});
            Submit(c,g,new ConfigureOperationCommand{Entity=Entity(g,"lifecycle.a"),Operation=new Code("availability"),Enabled=true,Target=Fix.One});
            Submit(c,g,new SetHomeCommand{Worker=Entity(g,"lifecycle.a"),Home=Entity(g,"lifecycle.home")});twin=Save(c,f,g);
            Reach(g,twin,()=>Position(g,"lifecycle.a")==Fix.FromInt(20),"unavailable worker physically clears site");
            for(int i=0;i<3;i++)Step(g,twin);
            c.Equal(retained.Progress,Node(g,order,"assemble").Progress,"saved lost-capacity plan pause retains earned progress");
            c.That(Node(g,order,"materials").Resources.Single().Spent&&Stock(g,"source")==Fix.FromInt(12),"loss cannot refund or repeat spent eight");
            Policy(c,g,"lifecycle.b",true,true);
            if(cancel)
            {
                int sequence=Queue(c,g,new CancelPlanCommand{Selection=new PlanCancellationSelection(false,new[]{plan})});twin=Save(c,f,g);Step(g,twin);
                c.That(Result(g,sequence).Accepted,"saved partial plan cancellation accepted");
                Reach(g,twin,()=>Plan(g,plan).State==PlanState.Cancelled&&!Exists(g,"extension.site"),"original abandonment");
                c.That(!Exists(g,"extension.facility")&&!Completed(g,"build:extension.facility"),"cancelled earned prefix creates no completion effect");
            }
            else
            {
                Submit(c,g,new ConfigurePlanCommand{Plan=plan,Group="interruption",Priority=70,Paused=false});twin=Save(c,f,g);
                Reach(g,twin,()=>Node(g,order,"assemble").Worker==Entity(g,"lifecycle.b")&&Node(g,order,"assemble").Progress.WholeTicks>retained.Progress.WholeTicks,"backup physically earns remaining assembly");
                c.Equal(Fix.FromInt(30),Position(g,"lifecycle.b"),"replacement travels, no remote work");
                c.Equal(retained.JobId,Node(g,order,"assemble").JobId,"same original job retains node progress across worker reselection");
                Reach(g,twin,()=>Plan(g,plan).State==PlanState.Done,"same graph original once-only final effect");
                for(int i=0;i<4;i++)Step(g,twin);
                c.That(g.Seat(0).Snapshot.Entities.Count(x=>x.Def=="extension.facility")==1&&Completed(g,"build:extension.facility"),"one earned construction effect/history");
            }
            c.That(retained.Worker==Entity(g,"lifecycle.a")&&retained.Progress.WholeTicks==2,"old assembly observation detached from later actor/progress");
            c.Equal(Fix.FromInt(12),Total(g),"actual spent material never returned/repeated");
            var hidden=new List<PlanView>();Work(g,1).CopyPlans(hidden);c.Equal(0,hidden.Count,"private interrupted plan history");
            Carrier(c,f,g,false);Carrier(c,f,g,true);
            c.Equal(Fix.FromInt(12),Total(g),"both interrupted physical shipments conserve remaining twelve");
            c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"terminal interrupted graph/cargo state durable");
            c.Equal(idleHash,idle.Hash(),"interleaved untouched session");
            c.That(new ReplayVerifier().Verify(f,recording.Finish(),limits).Success,"ordinary shutdown/reselect/cancel/partial cargo replay");
        }
        private static void Carrier(Check c,GameFactory f,SimGame g,bool delivery)
        {
            Band(c,g,Fix.Zero,false);
            int id=checked((int)Submit(c,g,new HaulResourcesCommand{Source=Entity(g,"source"),Destination=Entity(g,"destination"),
                Worker=Entity(g,"machine"),Resource=Resource(g),Ceiling=Fix.FromInt(8),Priority=100}).Arg0);
            var twin=Save(c,f,g);
            Reach(g,twin,()=>Job(g,id).Loaded.IsPositive&&Job(g,id).Loaded<Job(g,id).Amount&&Job(g,id).ToilIndex==(delivery?3:1),
                delivery?"actual partial delivery":"actual partial pickup");
            Policy(c,g,"machine",false);var retained=Job(g,id);var progress=retained.HandlingProgress;
            Fix cargo=Stock(g,"machine"),at=Position(g,"machine"),prefix=Stock(g,"destination"),source=Stock(g,"source");
            c.That(cargo.IsPositive&&progress.WholeTicks>0,"real partial cargo and handling earned");
            twin=Save(c,f,g);for(int i=0;i<3;i++)Step(g,twin);
            c.That(Job(g,id).State==JobState.Paused&&Job(g,id).HandlingProgress.Equals(progress)&&Stock(g,"machine")==cargo&&Position(g,"machine")==at,
                "policy pause retains actual handling/cargo/position");
            int sequence=Queue(c,g,new SetWorkerHeldCommand{Worker=Entity(g,"machine"),Held=true});twin=Save(c,f,g);
            g.Clock.Pause();twin.Clock.Pause();ulong paused=g.Hash();long time=g.Clock.StepIndex;Step(g,twin);
            c.That(g.Hash()==paused&&g.Clock.StepIndex==time&&Jobs(g).Any(j=>j.Id==id&&Live(j)),"saved held inbox waits under sole clock pause");
            g.Clock.Resume();twin.Clock.Resume();Step(g,twin);c.That(Result(g,sequence).Accepted,"ordinary saved held interruption");
            c.That(!Jobs(g).Any(j=>j.Id==id&&Live(j))&&Stock(g,"machine")==cargo&&Position(g,"machine")==at,"old commitment ends, physical cargo/position survive cancellation");
            c.That(Stock(g,"source")==source&&Stock(g,"destination")==prefix,"interruption cannot rewind pickup/delivery prefix");
            c.That(Facts(g,"source").OutboundPromised.IsZero&&Facts(g,"destination").InboundPromised.IsZero,"actual old promises released without changing stock");
            Band(c,g,prefix+cargo,true);Policy(c,g,"machine",true);
            Submit(c,g,new SetWorkerHeldCommand{Worker=Entity(g,"machine"),Held=false});twin=Save(c,f,g);
            Reach(g,twin,()=>Jobs(g).Any(j=>Live(j)&&j.RecoveryKind==LogisticsRecoveryKind.CargoReturn),"original residual recovery reacquisition");
            var recovery=Jobs(g).Single(j=>Live(j)&&j.RecoveryKind==LogisticsRecoveryKind.CargoReturn);
            c.That(recovery.Id!=id&&recovery.Amount==cargo&&recovery.Loaded==cargo,"fresh original recovery carries only actual residual");
            Reach(g,twin,()=>Stock(g,"machine").IsZero&&!Jobs(g).Any(j=>j.IsHaul&&Live(j)),"physical residual delivery and release");
            c.Equal(prefix+cargo,Stock(g,"destination"),"exact actual prefix plus residual delivered once");
            c.Equal(source,Stock(g,"source"),"recovery never loads original unpicked promise");
            c.That(Facts(g,"destination").InboundPromised.IsZero&&Total(g)==Fix.FromInt(12),"recovery releases actual claim without duplication");
            c.That(retained.Loaded==cargo&&retained.HandlingProgress.Equals(progress),"retained old partial job facts immutable");
        }
    }
}
