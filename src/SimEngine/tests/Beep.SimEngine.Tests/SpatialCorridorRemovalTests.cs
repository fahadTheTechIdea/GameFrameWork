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
    // Actual JSON/master authoring and normal player commands/views/Update only.
    internal static class SpatialCorridorRemovalTests
    {
        internal const string Case="Spatial corridor removal: shared earned execution with selected geometry and native conservation";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static WorkView Work(SimGame g,int seat=0)=>g.Seat(seat).View.Feature<WorkView>();
        private static List<ActivityNodeView> Nodes(SimGame g,int order)
        {var rows=new List<ActivityNodeView>();if(!Work(g).TryCopyNodes(order,rows))throw new InvalidOperationException("missing actual removal nodes");return rows;}
        private static List<OrderView> Orders(SimGame g)
        {var rows=new List<OrderView>();Work(g).CopyOrders(rows);return rows;}
        private static GameFactory Native(bool half,bool last,InfrastructureServiceContractsProofModule probe)
            =>PhysicalLineRemovalTests.Create(false,lastTile:last,additionalModule:probe,
                editDefinitions:root=>{if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));});
        private static GameFactory Points(bool half,PointLineGeometryProofModule probe)
            =>SpatialCorridorExecutionTests.Points(half,probe,
                extraDocuments:new Dictionary<string,string>{{"point-corridor-removal.json",FrameworkProofContent.Read("point-corridor-removal.json")}},
                editWorld:root=>root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("corridor.removal-site.start")));
        private static Fix Value(SimGame g,bool point,int index=1)
            =>point?SpatialCorridorExecutionTests.Segment(g,true,index):Fix.FromInt(PhysicalLineRemovalTests.Tile(g,10+index));
        private static Fix Total(SimGame g,bool point)
            =>point?SpatialCorridorExecutionTests.Total(g,true):PhysicalLineRemovalTests.Steel(g);
        private static PostOrderCommand Cut(SimGame g,bool point)
            =>new PostOrderCommand{Plan=point?"corridor.remove":"removal.work",Target=Entity(g,point?"corridor.removal-site":"removal.site"),Priority=100};
        private static void Reach(SimGame g,SimGame twin,Func<bool> ready,string stage)
        {
            for(int i=0;i<900&&!ready();i++)Step(g,twin);
            if(!ready())throw new InvalidOperationException(stage+" clock="+g.Clock.StepIndex+" orders="+string.Join(";",Orders(g).Select(o=>o.Id+"/"+o.State+"/"+o.Blocker)));
        }
        private static void Flow(Check c,bool point,bool half)
        {
            var native=new InfrastructureServiceContractsProofModule();var independent=new PointLineGeometryProofModule();
            var f=point?Points(half,independent):Native(half,false,native);
            var g=point?ConstructionPlanTests.NewPoints(f):PhysicalLineRemovalTests.New(f);
            var isolated=point?ConstructionPlanTests.NewPoints(f):PhysicalLineRemovalTests.New(f);ulong untouched=isolated.Hash();
            c.That(point?independent.SharedOwner&&independent.NativeOwnerAbsent:native.OneOwner&&native.PersistentOwners==1&&native.EndpointOwners==1,"same execution/removal alias and actual selected geometry owner");
            var limits=new ReplayLimits(4*1024*1024,4000);
            using(var recorder=g.RecordReplay(limits)) {
                if(point) {
                    long plan=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=SpatialCorridorExecutionTests.Intent(true,false)}).Arg0;
                    Reach(g,null,()=>PermanentRemovalTests.Plan(g,plan).State==PlanState.Done,"physical point corridor build");
                    c.Equal(Fix.FromInt(14),Total(g,true),"build consumes actual six supplies");
                    var mobile=new PostOrderCommand{Plan="corridor.remove",Target=Entity(g,"machine"),Priority=100};ulong pure=g.Hash();
                    c.Equal(Codes.InvalidArgument,g.Seat(0).Preview(mobile),"mobile provider subject cannot remove corridor");c.Equal(pure,g.Hash(),"mobile refusal pure");
                    var missing=new PostOrderCommand{Plan="corridor.remove",Target=Entity(g,"source"),Priority=100};
                    c.Equal(Codes.NotFound,g.Seat(0).Preview(missing),"actual unlaid fixed subject refuses");c.Equal(pure,g.Hash(),"unlaid refusal pure");
                }
                else for(int i=0;i<4;i++)Step(g);
                Fix stock=Total(g,point);var command=Cut(g,point);ulong before=g.Hash();
                c.Equal(Codes.NotOwned,g.Seat(1).Preview(command),"foreign company cannot order physical removal");
                c.Equal(Code.None,g.Seat(0).Preview(command),"selected geometry admits actual fixed subject");c.Equal(before,g.Hash(),"removal preview pure");
                int order=(int)PermanentRemovalTests.Submit(c,g,command).Arg0;
                Fix original=point?Fix.Parse("1.5"):Fix.One;
                c.Equal(original,Value(g,point),"post never cuts instantly");
                c.Equal(WorkEffectCodes.NotComplete,Work(g).PreviewOrderEffect(order),"same original progress gate");
                Reach(g,null,()=>Nodes(g,order).Single().Progress.WholeTicks>0,"actual machine travel and earned removal work");
                var detached=Nodes(g,order).Single();var earned=detached.Progress;
                c.Equal(original,Value(g,point),"partial work retains corridor");
                c.That(!Work(g,1).TryCopyNodes(order,new List<ActivityNodeView>()),"foreign work progress withheld");
                var twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"saved partial work agrees");
                long clock=g.Clock.StepIndex;g.Clock.Pause();twin.Clock.Pause();Step(g,twin);c.Equal(clock,g.Clock.StepIndex,"sole clock freezes removal");g.Clock.Resume();twin.Clock.Resume();
                PermanentRemovalTests.Submit(c,g,new CancelOrderCommand{Order=order});twin=f.Load(g.Save());
                for(int i=0;i<3;i++)Step(g,twin);
                c.Equal(original,Value(g,point),"cancel cannot remove unearned geometry");c.Equal(stock,Total(g,point),"cancel keeps original resources");
                order=(int)PermanentRemovalTests.Submit(c,g,Cut(g,point)).Arg0;
                string completionPlan=Orders(g).Single(o=>o.Id==order).Plan;twin=f.Load(g.Save());
                Reach(g,twin,()=>Value(g,point)==Fix.Zero,"earned selected geometry cut");
                c.That(Work(g).TryReadActivityCompletion(completionPlan,command.Target,out var completion)&&completion.Order==order,
                    "original compact earned history proves completion after order retirement");
                c.That(!Work(g,1).TryReadActivityCompletion(completionPlan,command.Target,out _),"foreign compact history withheld");
                c.Equal(original,Value(g,point,0),"left geometry retained");c.Equal(original,Value(g,point,2),"right geometry retained");
                c.Equal(stock,Total(g,point),"remove grants no salvage or stock loss");
                if(!point)c.Equal(2,PhysicalLineRemovalTests.Pools(g).Count,"native whole-set pool split conserves stock");
                c.Equal(earned,detached.Progress,"old partial progress remains detached");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"completed removal checkpoint durable");
                c.That(Work(f.Load(g.Save())).TryReadActivityCompletion(completionPlan,command.Target,out var restored)&&restored.Order==completion.Order,
                    "saved original completion history retains actual earned identity");
                ulong cut=g.Hash();c.Equal(Codes.NotFound,g.Seat(0).Preview(Cut(g,point)),"already removed real geometry refuses");c.Equal(cut,g.Hash(),"missing geometry refusal pure");
                if(point) {
                    var args=Json.Parse("{\"space\":\"field.points\",\"path\":[{\"space\":\"field.points\",\"position\":5}]}");
                    long plan=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=new PlanIntent("line:corridor.point-line",args,"repair",60,false)}).Arg0;
                    twin=f.Load(g.Save());Reach(g,twin,()=>PermanentRemovalTests.Plan(g,plan).State==PlanState.Done,"physical point corridor rebuild");
                    c.Equal(original,Value(g,true),"rebuild earned authored fixed layer value");c.Equal(Fix.FromInt(12),Total(g,true),"rebuild consumes two actually hauled supplies");
                }
                c.Equal(untouched,isolated.Hash(),"interleaved original selected-space session isolated");
                c.That(new ReplayVerifier().Verify(f,recorder.Finish(),limits).Success,"same public input replays physical construction/removal");
            }
        }
        private static void LastPool(Check c)
        {
            var f=Native(false,true,new InfrastructureServiceContractsProofModule());var g=PhysicalLineRemovalTests.New(f);
            for(int i=0;i<4;i++)Step(g);ulong before=g.Hash();
            c.Equal(NetworkService.OccupiedNetwork,g.Seat(0).Preview(Cut(g,false)),"native stocked last pool still refuses through shared removal");
            c.Equal(before,g.Hash(),"last pool refusal pure");c.Equal(Fix.FromInt(30),Total(g,false),"last pool stock survives");
            c.Equal(Fix.One,Value(g,false,0),"last pool geometry survives");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current shared removal identity");c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"current work/layer/topology codecs");
            foreach(bool point in new[]{false,true})foreach(bool half in new[]{false,true})Flow(c,point,half);
            LastPool(c);
        }
    }
}
