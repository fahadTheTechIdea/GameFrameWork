using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
    // Only actual master/JSON and public player commands/views/Update. No direct transaction, stock or placement writes.
    internal static class MixedProductionAcceptanceTests
    {
        internal const string Case="Mixed production acceptance: physical solid liquid catalyst utility staffing and whole resource entity results";
        private static readonly Code Process=new Code("process");
        // Independent fixture oracles, never engine stock or a running/replayed balance.
        private static readonly (string Name, Fix Opening)[] Materials = {
            ("mix.ore",Fix.FromInt(4)), ("mix.feed",Fix.FromInt(8)), ("mix.flux",Fix.One),
            ("mix.fuel",Fix.FromInt(8)), ("mix.product",Fix.Zero), ("mix.waste",Fix.Zero) };
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(bool half)
        {
            var root=Json.Parse(PointGame.Definitions);root.Get("scenarios")[0].Set("seats",JsonValue.Number("2"));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.output-ports"));
            if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));
            root.Get("entities")[2].Get("components").Get("storage").Set("capacity_policy",JsonValue.Of("mix.cargo"));
            root.Get("entities")[2].Get("components").Get("transporter").Set("handling",JsonValue.Of("mix.handling"));
            root.Get("entities")[2].Get("components").Set("work_policy",Json.Parse("{\"profile\":\"mix.cargo-policy\"}"));
            foreach(string id in new[]{"mix.source","mix.receiver","mix.factory","mix.operator"})root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of(id+"-start"));
            return PointGame.Factory(definitions:root.ToString(),extraModules:new GameModule[]{new EndpointProofModule(),new OutputPortProofModule()},
                extraDeclarations:FrameworkProofContent.Read("mixed-production-modules.json"),
                extraDocuments:new Dictionary<string,string>{{"mixed-production.json",FrameworkProofContent.Read("mixed-production.json")},{"mixed-production-pools.json",FrameworkProofContent.Read("mixed-production-pools.json")}});
        }
        private static SimGame New(GameFactory f)=>f.NewGame(new NewGameRequest{Scenario="points",Seed=2183,Seats={Controller.Human,Controller.Human}});
        private static EntityId Site(SimGame g)=>Entity(g,"mix.factory");
        private static ResourceId Resource(SimGame g,string name)
        {if(!g.Seat(0).View.TryFindResource(name,out var r))throw new InvalidOperationException("actual mixed resource missing "+name);return r;}
        private static TransformationManagementView View(SimGame g)=>g.Seat(0).View.Feature<TransformationManagementView>();
        private static TransformationSnapshot Current(SimGame g)
        {if(!View(g).TryGetTransformation(Site(g),Process,out var row))throw new InvalidOperationException("actual mixed processor missing");return row;}
        private static ResourcePortSnapshot Port(SimGame g,string port,string resource)
        {Code refusal=g.Seat(0).View.Feature<InventoryView>().ReadResourcePort(Site(g),new Code(port),Resource(g,resource),out var row);if(!refusal.IsNone)throw new InvalidOperationException("actual mixed port "+refusal);return row;}
        private static int Results(SimGame g)=>g.Seat(0).Snapshot.Entities.Count(e=>e.Def=="mix.result"&&e.Owner==new PlayerId(0));
        private static bool Live(WorkJobView j)=>j.State==JobState.Open||j.State==JobState.Assigned||j.State==JobState.Paused;
        private static bool CarrierFree(SimGame g)=>!PermanentRemovalTests.Jobs(g).Any(j=>j.Worker==Entity(g,"machine")&&Live(j));
        private static void Accounting(Check c,SimGame g)
        {
            var facts=g.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            c.Equal(ResourceFlowReadStatus.Ready,facts.Status,"actual mixed resource journal is readable");
            foreach(var material in Materials) {
                var resource=Resource(g,material.Name);var rows=facts.Aggregates.Where(row=>row.Resource==resource).ToArray();
                BigInteger Amount(ResourceFlowKind kind)=>rows.Where(row=>row.Kind==kind).Aggregate(BigInteger.Zero,(sum,row)=>sum+row.AmountRaw);
                c.Equal(new BigInteger(material.Opening.Raw),Amount(ResourceFlowKind.Initialized),"authored opening is never re-initialized: "+material.Name);
                c.That(rows.All(row=>row.Kind==ResourceFlowKind.Initialized||row.Kind==ResourceFlowKind.StockTransfer||row.Kind==ResourceFlowKind.Produced||row.Kind==ResourceFlowKind.Consumed),
                    "physical manufacturing needs no purchase, creation, destruction, loss or sale compensation: "+material.Name);
                bool output=material.Name=="mix.product"||material.Name=="mix.waste";
                c.Equal(BigInteger.Zero,Amount(output?ResourceFlowKind.Consumed:ResourceFlowKind.Produced),"only the actual authored side of the recipe changes material: "+material.Name);
                c.Equal(new BigInteger(material.Opening.Raw)+Amount(ResourceFlowKind.Produced)-Amount(ResourceFlowKind.Consumed),new BigInteger(Total(g,material.Name).Raw),
                    "every settled update conserves actual stock/cargo/WIP/scoped vessels and classified flow: "+material.Name);
            }
        }
        private static void Advance(Check c,SimGame g,SimGame twin=null)
        { Step(g,twin);Accounting(c,g); }
        private static CommandResult Send(Check c,SimGame g,Command command)
        { var result=PermanentRemovalTests.Submit(c,g,command);Accounting(c,g);return result; }
        private static void Reach(Check c,SimGame g,SimGame twin,Func<bool> ready,string stage)
        {
            for(int i=0;i<600&&!ready();i++)Advance(c,g,twin);
            if(!ready())throw new InvalidOperationException(stage+" exceeded600Updates step="+g.Clock.StepIndex+" phase="+Current(g).Phase+"/"+Current(g).Blocker+
                " progress="+Current(g).Progress+" results="+Results(g)+" positions="+string.Join(";",g.Seat(0).Snapshot.Entities.Where(e=>e.Def=="machine"||e.Def=="mix.operator").Select(e=>e.Def+"/"+(e.Spatial as PointSnapshot)?.Position))+
                " jobs="+string.Join(";",PermanentRemovalTests.Jobs(g).Select(j=>j.Worker+"/"+j.State+"/"+j.Blocker+"/toil"+j.ToilIndex+"/dock"+j.SourceDockSlot+","+j.DestinationDockSlot)));
        }
        private static Fix Total(SimGame g,string name)
        {
            Fix total=Current(g).Wip.Stock.AmountOf(Resource(g,name));
            foreach(var entity in g.Seat(0).Snapshot.Entities){var facts=new List<Facet>();g.Seat(0).View.Inspect(entity.Id,facts);foreach(var fact in facts)if(fact.Key==Storage.FacetStock&&fact.Subject==name)total+=fact.Value;}
            if(name=="mix.flux")total+=Port(g,"tool",name).Stock.AmountOf(Resource(g,name));
            if(name=="mix.fuel")total+=Port(g,"fuel",name).Stock.AmountOf(Resource(g,name));
            if(name=="mix.product")total+=Port(g,"product",name).Stock.AmountOf(Resource(g,name));
            if(name=="mix.waste")total+=Port(g,"waste",name).Stock.AmountOf(Resource(g,name));return total;
        }
        private static void Haul(Check c,GameFactory f,SimGame g,string source,string target,string resource,Fix quantity,string sourcePort,string destinationPort)
        {
            Reach(c,g,null,()=>CarrierFree(g),"previous physical carrier departure");long before=g.Clock.StepIndex;
            Send(c,g,new HaulResourcesCommand{Source=Entity(g,source),Destination=Entity(g,target),Worker=Entity(g,"machine"),Resource=Resource(g,resource),Ceiling=quantity,
                SourcePort=new Code(sourcePort),DestinationPort=new Code(destinationPort),Priority=100});
            var twin=f.Load(g.Save());Reach(c,g,twin,()=>CarrierFree(g),"physical mixed "+resource+" delivery");
            c.That(g.Clock.StepIndex>before+1,"mixed loading/travel/unloading takes actual game time: "+resource);
        }
        private static BigInteger Recorded(SimGame g,string resource,ResourceFlowKind kind)
        {
            var facts=g.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            if(facts.Status!=ResourceFlowReadStatus.Ready)throw new InvalidOperationException("actual mixed journal "+facts.Status);
            return facts.Aggregates.Where(x=>x.Resource==Resource(g,resource)&&x.Kind==kind).Aggregate(BigInteger.Zero,(s,x)=>s+x.AmountRaw);
        }
        private static void Physical(Check c,bool half)
        {
            var f=Factory(half);var g=New(f);var idle=New(f);ulong isolated=idle.Hash();var limits=new ReplayLimits(4*1024*1024,5000);
            using(var recorder=g.RecordReplay(limits)) {
                Accounting(c,g);ulong initialHash=g.Hash();
                c.That(!g.Modules.Contains(TycoonModule.ModuleId),"mixed industrial work needs no business module");
                c.Equal(0,Results(g),"no instant entity result");
                Send(c,g,new EnqueueTransformationCommand{Transformer=Site(g),Capability=Process,Transformation="mix.batch",Count=2});
                c.Equal(0L,Current(g).Wip.Execution,"disabled queued work does not acquire or create completed state");
                Haul(c,f,g,"mix.source","mix.factory","mix.ore",Fix.FromInt(2),"stock","raw");
                Haul(c,f,g,"mix.source","mix.factory","mix.feed",Fix.FromInt(4),"stock","raw");
                Haul(c,f,g,"mix.source","mix.factory","mix.flux",Fix.One,"stock","tool");
                Haul(c,f,g,"mix.source","mix.factory","mix.fuel",Fix.FromInt(6),"stock","fuel");
                c.Equal(Fix.FromInt(4),Total(g,"mix.ore"),"solid hauling conserves finite mass");c.Equal(Fix.FromInt(8),Total(g,"mix.feed"),"liquid hauling conserves finite volume");
                int support=(int)Send(c,g,new PostOrderCommand{Plan="mix.support",Target=Site(g),Priority=80}).Arg0;
                Send(c,g,new ConfigureOperationCommand{Entity=Site(g),Operation=Process,Enabled=true,Target=Fix.One});
                var twin=f.Load(g.Save());Reach(c,g,twin,()=>Current(g).Phase==TransformationPhase.Working&&Current(g).Progress.WholeTicks>0,"staffed mixed batch acquires whole WIP");
                var active=Current(g);long execution=active.Wip.Execution;
                c.Equal(Fix.FromInt(2),active.Wip.Stock.AmountOf(Resource(g,"mix.ore")),"actual mass input in original WIP");
                c.Equal(Fix.FromInt(4),active.Wip.Stock.AmountOf(Resource(g,"mix.feed")),"actual volume input in same original WIP");
                c.Equal(new ResourceEndpointId("proof.pools",101),active.Tools.Receipts.Single().Token.Endpoint,"catalyst held at actual remote endpoint, not local copy");
                c.Equal(Fix.Parse("0.5"),active.Tools.Receipts.Single().Amount,"whole remote catalyst held before working");
                ulong pure=g.Hash();c.Equal(Code.None,View(g).ReadStaffing(Site(g),Process,"mix.batch",out var staffing),"engine staffing facts");
                c.That(staffing.Any(x=>x.Applies&&x.Requirements.All(n=>n.Ready)),"real machine activity supports productive operation");
                c.That(g.Seat(0).View.Feature<OperationsView>().TryGetUtilityDemand(Site(g),Process,out var utility)&&utility.Drawing&&utility.Consumed.AmountOf(Resource(g,"mix.fuel")).IsPositive,"liquid utility comes from actual timed original draw");
                c.Equal(Code.None,View(g).ReadExecutionPreview(Site(g),Process,"mix.batch",out var preview),"mixed engine-calculated preview");
                c.Equal(2,preview.Outputs.Count,"preview describes both native resource yields");c.Equal(pure,g.Hash(),"staffing/utility/preview observations pure");
                c.That(!g.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(Site(g),Process,out _),"foreign private production withheld");
                twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"mixed WIP/catalyst/utility/staffing state durable");
                Reach(c,g,twin,()=>Results(g)==1,"first complete resource and entity conversion");
                c.Equal(Fix.One,Port(g,"product","mix.product").Stock.AmountOf(Resource(g,"mix.product")),"first solid whole result");
                c.Equal(Fix.FromInt(2),Port(g,"waste","mix.waste").Stock.AmountOf(Resource(g,"mix.waste")),"first liquid whole result");
                c.Equal(Fix.Parse("0.75"),Total(g,"mix.flux"),"remote catalyst wear occurs once at successful result");
                c.Equal(Fix.FromInt(5),Total(g,"mix.fuel"),"six productive minutes consume three real liquid fuel units");
                Haul(c,f,g,"mix.source","mix.factory","mix.ore",Fix.FromInt(2),"stock","raw");
                Haul(c,f,g,"mix.source","mix.factory","mix.feed",Fix.FromInt(4),"stock","raw");
                twin=f.Load(g.Save());Reach(c,g,twin,()=>Current(g).Phase==TransformationPhase.Blocked&&Current(g).Blocker==TransformerManager.BlockerOutput,"whole second output backpressure");
                var blocked=Current(g);c.That(blocked.Wip.Execution!=execution,"second physical execution has stable distinct identity");
                c.Equal(Fix.FromInt(2),blocked.Wip.Stock.AmountOf(Resource(g,"mix.ore")),"full solid WIP retained at completion wait");c.Equal(Fix.FromInt(4),blocked.Wip.Stock.AmountOf(Resource(g,"mix.feed")),"full liquid WIP retained at completion wait");
                c.Equal(Fix.Parse("0.5"),blocked.Tools.Held.AmountOf(Resource(g,"mix.flux")),"second catalyst remains protected before conversion");
                var token=blocked.Tools.Receipts.Single().Token;twin=f.Load(g.Save());for(int i=0;i<4;i++)Advance(c,g,twin);
                c.Equal(blocked.Progress,Current(g).Progress,"full output wait preserves earned work");c.Equal(blocked.Operating.ElapsedTicks,Current(g).Operating.ElapsedTicks,"completed wait burns no extra utility fuel");
                c.Equal(token,Current(g).Tools.Receipts.Single().Token,"same original remote catalyst receipt survives saved wait");c.Equal(Fix.Parse("0.75"),Total(g,"mix.flux"),"waiting does not repeat catalyst wear");
                Haul(c,f,g,"mix.factory","mix.receiver","mix.product",Fix.One,"product","stock");
                c.Equal(1,Results(g),"one drained vessel cannot create a partial entity result");c.Equal(Fix.Zero,Port(g,"product","mix.product").Stock.AmountOf(Resource(g,"mix.product")),"one ready output remains empty until all outputs admit");
                c.Equal(Fix.Parse("0.75"),Total(g,"mix.flux"),"no partial catalyst consumption after one-output release");
                Haul(c,f,g,"mix.factory","mix.receiver","mix.waste",Fix.FromInt(2),"waste","stock");twin=f.Load(g.Save());
                Reach(c,g,twin,()=>Results(g)==2&&Current(g).QueueCount==0,"whole mixed final conversion and legal entity rollout");
                c.Equal(Fix.FromInt(2),Total(g,"mix.product"),"two solid results across real vessels and receiver");c.Equal(Fix.FromInt(4),Total(g,"mix.waste"),"two liquid results across real vessels and receiver");
                c.Equal(Fix.Zero,Total(g,"mix.ore"),"all four solid raw units consumed exactly once");c.Equal(Fix.Zero,Total(g,"mix.feed"),"all eight fluid raw units consumed exactly once");
                c.Equal(Fix.Parse("0.5"),Total(g,"mix.flux"),"two authored quarter-unit catalyst charges, no duplicate wear");c.Equal(Fix.FromInt(2),Total(g,"mix.fuel"),"six operating fuel units spent, no completed-wait burns");
                c.Equal(new BigInteger(Fix.Parse("0.5").Raw),Recorded(g,"mix.flux",ResourceFlowKind.Consumed),"journal verifies actual remote catalyst consumption");
                c.Equal(new BigInteger(Fix.FromInt(6).Raw),Recorded(g,"mix.fuel",ResourceFlowKind.Consumed),"journal verifies actual utility consumption");
                c.Equal(new BigInteger(Fix.FromInt(2).Raw),Recorded(g,"mix.product",ResourceFlowKind.Produced),"solid production journal complete");c.Equal(new BigInteger(Fix.FromInt(4).Raw),Recorded(g,"mix.waste",ResourceFlowKind.Produced),"fluid production journal complete");
                c.Equal(0,Current(g).Tools.Receipts.Count,"successful complete batches release original remaining tools");
                Send(c,g,new CancelOrderCommand{Order=support});
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"mixed settled resources/results/staff state durable");c.Equal(isolated,idle.Hash(),"interleaved full composition isolated");
                byte[] bytes=recorder.Finish();var description=ReplayVerifier.Describe(bytes,limits);
                c.That(description.InitialHash==initialHash&&description.InitialStep==recorder.InitialStep,"mixed replay starts at the actual unsupplied primary");
                c.That(description.FinalHash==g.Hash()&&description.FinalStep==g.Clock.StepIndex&&description.Commands==recorder.CommandCount&&description.Checkpoints==recorder.CheckpointCount,
                    "mixed replay contains every original command/update through both completed live results");
                c.Equal(g.Clock.StepIndex-recorder.InitialStep,(long)recorder.CheckpointCount,"mixed recorder covers every earned game update");
                var replay=new ReplayVerifier().Verify(f,bytes,limits);
                c.That(replay.Success&&replay.ActualHash==g.Hash(),"complete public physical mixed workflow replays to the final live hash");
                Console.WriteLine("mixed manufacturing replay: half="+half+";initial="+recorder.InitialStep+";final="+g.Clock.StepIndex+";commands="+recorder.CommandCount+
                    ";checkpoints="+recorder.CheckpointCount+";expected-hash="+g.Hash().ToString("X16")+";actual-hash="+replay.ActualHash.ToString("X16"));
            }
        }
        private static void Run(Check c)
        {c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current actual production baseline");Physical(c,false);Physical(c,true);}
    }
}
