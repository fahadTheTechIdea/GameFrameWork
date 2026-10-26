using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Only public player commands/views, Update and durability; JSON/native modules own all rules.
    internal static class ScopedActivityResourceTests
    {
        internal const string Case = "Scoped activity resources: parallel actual supply, running sources and physical tool lifetime";
        private static readonly Code Feed=new Code("feed"), FeedAlias=new Code("feed-alias"), Fluid=new Code("fluid"),
            Tools=new Code("tools"), Toolbox=new Code("toolbox");
        private static readonly ResourceEndpointId First=new ResourceEndpointId("proof.pools",5000000001),
            Second=new ResourceEndpointId("proof.pools",5000000002), Third=new ResourceEndpointId("proof.pools",5000000003);
        internal static void Register() => TestRunner.Add(Case,Run);
        private static EntityId Find(SimGame game,string name) => game.Seat(0).Snapshot.Entities.Single(e=>e.Def==name).Id;
        private static ResourceId Steel(SimGame game)
        { if(!game.Seat(0).View.TryFindResource("steel",out var id)) throw new InvalidOperationException("missing steel"); return id; }
        private static WorkView Work(SimGame game,int seat=0) => game.Seat(seat).View.Feature<WorkView>();
        private static LogisticsView Logistics(SimGame game,int seat=0) => game.Seat(seat).View.Feature<LogisticsView>();
        private static List<ActivityNodeView> Nodes(SimGame game,int order,int seat=0)
        { var list=new List<ActivityNodeView>(); Work(game,seat).TryCopyNodes(order,list); return list; }
        private static List<WorkJobView> Jobs(SimGame game)
        { var list=new List<WorkJobView>(); Work(game).CopyJobs(list); return list; }
        private static List<LogisticsDemandView> Demands(SimGame game,int seat=0)
        { var list=new List<LogisticsDemandView>(); Logistics(game,seat).CopyDemands(list); return list; }
        private static bool Live(WorkJobView job) => job.State==JobState.Open || job.State==JobState.Assigned || job.State==JobState.Paused;
        private static bool Earned(ActivityNodeView node) => node.Progress.WholeTicks>0 || node.Progress.Fraction>0;
        private static bool Complete(SimGame game) => Work(game).TryReadActivityCompletion("activity:dag.scoped",Find(game,"dag.facility"),out _);
        private static void Step(SimGame game,SimGame twin=null)
        {
            game.Update(); twin?.Update();
            if(game.Fault!=null || twin?.Fault!=null || twin!=null && game.Hash()!=twin.Hash())
                throw new InvalidOperationException("scoped DAG update "+game.Tick+"/"+game.Fault+"/"+twin?.Fault);
        }
        private static void Until(SimGame game,int order,Func<bool> done,string stage,SimGame twin=null)
        {
            for(int i=0;i<1000&&!done();i++) Step(game,twin);
            if(!done())
            {
                long cursor=0; var news=new List<Notification>(); game.Seat(0).ReadNotifications(ref cursor,news);
                throw new InvalidOperationException(stage+" at "+game.Tick+" news="+string.Join(";",news.Select(n=>n.Tick+"/"+n.Kind))+
                " nodes="+string.Join(";",Nodes(game,order).Select(n=>n.Id+"/"+n.State+"/"+n.Progress.WholeTicks+"/"+n.Blocker))+
                " demands="+string.Join(";",Demands(game).Select(d=>d.Port+"/"+d.Endpoint+"/"+d.Quantity))+
                " jobs="+string.Join(";",Jobs(game).Select(j=>j.Id+"/"+j.State+"/"+j.Loaded+"/"+j.Blocker)));
            }
        }
        private static long Order(Check c,SimGame game,Command command)
        {
            ulong hash=game.Hash(); c.Equal(Code.None,game.Seat(0).Preview(command),"ordinary command preview");
            c.Equal(hash,game.Hash(),"preview pure"); var reply=TestKit.Order(game,0,command);
            c.That(reply.Accepted,"normal engine execution "+reply.Refusal);
            if(!reply.Accepted||game.Fault!=null) throw new InvalidOperationException("command "+reply.Refusal+"/"+game.Fault);
            return reply.Arg0;
        }
        private static SimGame Save(Check c,GameFactory factory,SimGame game)
        { var twin=factory.Load(game.Save()); c.Equal(game.Hash(),twin.Hash(),"native actual DAG/claim/demand checkpoint"); return twin; }
        private static void Select(Check c,SimGame game,string entity,Code port,ResourceEndpointId endpoint)
            => Order(c,game,new SelectResourcePortCommand {Entity=Find(game,entity),Port=port,Endpoint = new ResourceEndpointId("proof.pools", endpoint.Identity)});
        private static LogisticsOperationalSnapshot Read(Check c,SimGame game,string entity,Code port=default,ResourceEndpointId? endpoint=null)
        {
            ulong hash=game.Hash();
            c.Equal(Code.None,Logistics(game).ReadDiagnostics(Find(game,entity),Steel(game),out var value,port,endpoint),"native endpoint diagnostics");
            c.Equal(hash,game.Hash(),"native facts pure");
            if(value==null) throw new InvalidOperationException("no actual diagnostic "+entity+"/"+port); return value;
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"scoped activity protocol");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"existing native saved layouts");
            var factory=ScopedHaulGame.Factory(scopedActivities:true);
            var game=factory.NewGame(new NewGameRequest {Scenario="scoped-activity",Seed=963,Seats={Controller.Human,Controller.Human}});
            var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,6000));
            EntityId facility=Find(game,"dag.facility"), hauler=Find(game,"haul.machine");
            c.That(!game.Modules.Contains("sim.tycoon"),"DAG work/manufacturing resources are economy-independent");
            c.That(!game.Seat(0).View.Feature<InventoryView>().TryGetCapacity(facility, Code.None,out _),"actual target owns no local inventory");
            var proposed=new List<WorkRequirementView>(); ulong hash=game.Hash();
            c.Equal(Code.None,Work(game).PreviewOrderRequirements("dag.scoped",facility,proposed),"normal resource-blocked plan diagnostics");
            c.Equal(hash,game.Hash(),"all proposed native resource checks pure");
            c.That(proposed.SelectMany(r=>r.Result.Resources).Any(r=>r.Endpoint==First),"first scoped source exposed by same evaluator");
            c.That(proposed.SelectMany(r=>r.Result.Resources).Any(r=>r.Endpoint==Second),"second same-resource source exposed independently");
            // The worker's actual held tool is physically supplied, never an implicit remote fallback.
            int funding=checked((int)Order(c,game,new HaulResourcesCommand {Source=Find(game,"dag.warehouse"),Destination=facility,DestinationPort=Tools,
                Worker=hauler,Resource=Steel(game),Ceiling=Fix.FromInt(2),Priority=100}));
            Until(game,0,()=>!Jobs(game).Any(j=>j.Id==funding&&Live(j)),"physical tool funding");
            c.Equal(Fix.FromInt(2),Read(c,game,"dag.facility",Tools).Stock,"actual tools physically stored");
            int order=checked((int)Order(c,game,new PostOrderCommand {Plan="dag.scoped",Target=facility}));
            var demands=Demands(game).Where(d=>d.Source.Scope==WorkModule.ModuleId&&d.Source.Identity==order).ToList();
            c.Equal(2,demands.Count,"one envelope keeps independent actual input stores");
            c.Equal(Fix.FromInt(5),demands.Single(d=>d.Endpoint==First).Quantity,"two alias occurrences aggregate one five-unit actual target request");
            c.Equal(Fix.One,demands.Single(d=>d.Endpoint==Second).Quantity,"running input uses native next-input quantum, not total budget");
            c.Equal(0,Demands(game,1).Count,"actual scoped work demands company-private");
            c.Equal(0,Nodes(game,order,1).Count,"graph execution company-private");
            var twin=Save(c,factory,game);
            Until(game,order,()=>Nodes(game,order).Count(n=>(n.Id=="material-a"||n.Id=="material-b")&&n.State==NodeState.Working&&Earned(n))==2,
                "parallel native consume nodes after physical alias delivery",twin);
            var parallel=Nodes(game,order);
            c.That(parallel.Where(n=>n.Id=="material-a"||n.Id=="material-b").All(n=>n.Resources.Single().Choice.Endpoint==First),"parallel nodes bind same actual store, not separate inventories");
            c.That(parallel.Where(n=>n.Id=="material-a"||n.Id=="material-b").All(n=>n.Resources.Single().Spent),"both startup consumptions earned physical work");
            c.Equal(Fix.FromInt(5),parallel.Where(n=>n.Id=="material-a"||n.Id=="material-b").Aggregate(Fix.Zero,(sum,n)=>sum+n.Resources.Single().Choice.Quantities.AmountOf(Steel(game))),"native original occurrence bills retained");
            Until(game,order,()=>Nodes(game,order).Any(n=>n.Id=="fluid"&&Earned(n)&&n.State!=NodeState.Done),"native running input started",twin);
            var running=Nodes(game,order).Single(n=>n.Id=="fluid");
            c.Equal(Second,running.Resources.Single().Choice.Endpoint,"running binding acquired actual fluid store");
            Select(c,game,"dag.facility",Fluid,First);
            twin=Save(c,factory,game);
            c.Equal(Second,Nodes(game,order).Single(n=>n.Id=="fluid").Resources.Single().Choice.Endpoint,"selection edit cannot relocate running source");
            Until(game,order,()=>Nodes(game,order).Any(n=>n.Id=="assembly"&&n.Worker.IsValid),"dependent complete machine/tool acquisition",twin);
            var assembly=Nodes(game,order).Single(n=>n.Id=="assembly");
            c.Equal(Find(game,"dag.builder"),assembly.Worker,"real ordinary machine executes dependent node");
            c.Equal(2,assembly.Resources.Count(r=>r.Choice.Use==WorkResourceUse.Hold),"native target/worker held tools are distinct occurrences");
            c.That(assembly.Resources.Where(r=>r.Choice.Use==WorkResourceUse.Hold).All(r=>r.Choice.Endpoint==Third&&r.Receipts.Count==1&&!r.Spent),
                "complete held set uses actual shared provider receipts");
            c.Equal(Fix.FromInt(2),Read(c,game,"dag.facility",Tools).Stock,"holds protect real stock, not consume it");
            c.Equal(HaulStatus.NoSupply,Logistics(game).PreviewHaul(facility,Find(game,"dag.warehouse"),Steel(game),Fix.FromInt(2),sourcePort:Tools).Status,
                "real tool holds block conflicting export");
            Select(c,game,"dag.facility",Tools,First);
            Select(c,game,"dag.builder",Toolbox,Second);
            twin=Save(c,factory,game);
            Until(game,order,()=>Nodes(game,order).Any(n=>n.Id=="assembly"&&Earned(n)),"held tools survive both current selection edits",twin);
            var retained=Nodes(game,order).Single(n=>n.Id=="assembly");
            c.That(retained.Resources.Where(r=>r.Choice.Use==WorkResourceUse.Hold).All(r=>r.Choice.Endpoint==Third),"target and worker checks retain admitted original endpoints");
            Until(game,order,()=>Complete(game),"full authored graph physically completes",twin);
            c.That(Complete(twin),"saved twin earned same full graph completion");
            c.Equal(Fix.FromInt(20),Read(c,game,"dag.warehouse").Stock+Read(c,game,"haul.machine").Stock+
                Read(c,game,"dag.facility",Feed,First).Stock+Read(c,game,"dag.facility",Fluid,Second).Stock+
                Read(c,game,"dag.facility",Tools,Third).Stock,"first workflow physically consumes five startup, three running and two dependent material units");
            c.Equal(Fix.FromInt(2),Read(c,game,"dag.facility",Tools,Third).Stock,"completed work releases, never consumes tools");
            c.Equal(Fix.Zero,Read(c,game,"dag.facility",Fluid,Second).Stock,"running total consumed exactly three original native units");
            c.Equal(0,Demands(game).Count(d=>d.Source.Scope==WorkModule.ModuleId&&d.Source.Identity==order),"completed workflow withdraws original scoped envelope");
            c.Equal(HaulStatus.NoSupply,Logistics(game).PreviewHaul(facility,Find(game,"dag.warehouse"),Steel(game),Fix.FromInt(2),sourcePort:Tools,destinationPort:new Code("stock")).Status,
                "current selected empty feed may differ from released original tool store");
            // Restore authoring selections for a second physical order; cancel live holds through the native command.
            Select(c,game,"dag.facility",Fluid,Second); Select(c,game,"dag.facility",Tools,Third); Select(c,game,"dag.builder",Toolbox,Third);
            int cancelled=checked((int)Order(c,game,new PostOrderCommand {Plan="dag.scoped",Target=facility}));
            Until(game,cancelled,()=>Nodes(game,cancelled).Any(n=>n.Id=="assembly"&&n.Worker.IsValid),"second complete native held set");
            Select(c,game,"dag.facility",Tools,First); Select(c,game,"dag.builder",Toolbox,Second);
            twin=Save(c,factory,game);
            Order(c,game,new CancelOrderCommand {Order=cancelled});
            Order(c,twin,new CancelOrderCommand {Order=cancelled});
            c.Equal(game.Hash(),twin.Hash(),"cancel releases original holds identically after restore");
            Until(game,cancelled,()=>!Jobs(game).Any(Live),"physical cancellation/departure settles",twin);
            c.Equal(0,Demands(game).Count(d=>d.Source.Scope==WorkModule.ModuleId&&d.Source.Identity==cancelled),"cancel withdraws every original scoped request");
            Select(c,game,"dag.facility",Tools,Third);
            c.Equal(Fix.FromInt(2),Logistics(game).PreviewHaul(facility,Find(game,"dag.warehouse"),Steel(game),Fix.FromInt(2),sourcePort:Tools).Amount,
                "normal cancelled-original stock can be hauled again");
            c.That(retained.Resources.Where(r=>r.Choice.Use==WorkResourceUse.Hold).All(r=>r.Receipts.Count==1),"retained detached node facts not rewritten by cancellation");
            c.That(game.Fault==null,"normal work/supply/selection/cancel never faults");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,6000)).Success,
                "native scoped activity commands replay without host gameplay or extra resource owner");
        }
    }
}
