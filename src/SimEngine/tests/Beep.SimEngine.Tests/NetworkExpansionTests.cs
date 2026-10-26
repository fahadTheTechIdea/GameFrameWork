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
    // JSON/master, ordinary player commands/views and Update; the driver never manufactures a promise.
    internal static class NetworkExpansionTests
    {
        internal const string Case="Network expansion: physical construction waits for actual pool promises without changing geometry";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(bool half,Action<JsonValue> edit=null)
        {
            var authored=Json.Parse(FrameworkProofContent.Read("network-expansion.json"));edit?.Invoke(authored);
            var retry=Json.Parse(FrameworkProofContent.Read("work-retry.json"));
            retry.Set("work_retry_profiles",Json.Parse(FrameworkProofContent.Read("completion-retry.json")).Get("work_retry_profiles"));
            return SpatialNetworkGraphTests.Points(half,new PointLineGeometryProofModule(),
                moreDocuments:new Dictionary<string,string>{
                    ["point-owned-network.json"]=FrameworkProofContent.Read("point-owned-network.json"),
                    ["network-expansion.json"]=authored.ToString()},
                editCorridors:root=>{
                    root.Get("lines")[0].Set("owned",JsonValue.Of(true));
                    root.Get("lines")[0].Set("owner_layer",JsonValue.Of("corridor.company"));
                },editNetwork:root=>root.Get("networks")[0].Set("public",JsonValue.Of(false)),
                afterPorts:root=>{
                    root.Get("entities")[0].Get("components").Get("point_stock").Set("initial",JsonValue.Number("40"));
                    root.Get("entities")[1].Get("components").Remove("ports");
                },retryProfiles:retry.ToString());
        }
        private static List<NetworkPoolView> Pools(SimGame g,int seat=0)
        {
            var pools=new List<NetworkPoolView>();
            Code code=g.Seat(seat).View.Feature<InfrastructureView>().CopyPools("point.network",pools);
            if(!code.IsNone)throw new InvalidOperationException("expansion pools "+code);return pools;
        }
        private static Fix Total(SimGame g)=>SpatialCorridorExecutionTests.Total(g,true)+
            Pools(g).SelectMany(p=>p.Stock).Aggregate(Fix.Zero,(sum,q)=>sum+q.Amount);
        private static Fix At(SimGame g,int position)
        {
            var values=new List<Fix>();
            var code=g.Seat(0).View.Feature<PointLayerView>().ReadLayer("corridor.depth",new PointRegion(Fix.FromInt(position),Fix.One,1),values);
            if(code!=SpatialLayerReadStatus.Success||values.Count!=1)throw new InvalidOperationException("expansion layer "+code);return values[0];
        }
        private static PlanIntent Intent(int position)=>new PlanIntent("line:corridor.point-line",
            Json.Parse("{\"space\":\"field.points\",\"path\":[{\"space\":\"field.points\",\"position\":"+position+"}]}"),"expansion",80,false);
        private static bool Waiting(SimGame g,long plan)
        {
            var facts=PermanentRemovalTests.Plan(g,plan);var nodes=new List<ActivityNodeView>();
            return facts.Order>0&&g.Seat(0).View.Feature<WorkView>().TryCopyNodes(facts.Order,nodes)&&
                nodes.Any(n=>n.Id=="segment.0.install"&&n.Blocker==ResourceTransactions.ActivePromises);
        }
        private static void Reach(SimGame g,SimGame twin,Func<bool> ready,string stage)
        {
            for(int i=0;i<800&&!ready();i++)Step(g,twin);
            if(!ready())throw new InvalidOperationException(stage+" exceeded800Updates clock="+g.Clock.StepIndex+
                " pools="+string.Join(";",Pools(g).Select(p=>p.Endpoint+"/"+p.MemberCount))+
                " jobs="+string.Join(";",PermanentRemovalTests.Jobs(g).Select(j=>j.State+"/"+j.Blocker)));
        }
        private static void Physical(Check c,bool half,bool cancel)
        {
            var f=Factory(half);var g=ConstructionPlanTests.NewPoints(f);var isolated=ConstructionPlanTests.NewPoints(f);ulong untouched=isolated.Hash();
            var limits=new ReplayLimits(4*1024*1024,4000);
            using(var record=g.RecordReplay(limits)) {
                long first=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=SpatialCorridorExecutionTests.Intent(true,false)}).Arg0;
                var twin=f.Load(g.Save());Reach(g,twin,()=>PermanentRemovalTests.Plan(g,first).State==PlanState.Done,"initial physical corridor");
                Reach(g,twin,()=>Pools(g).Single().Stock.Sum(q=>q.Amount.Raw)>=Fix.FromInt(4).Raw,"actual elapsed pool supply");
                int hold=checked((int)PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="expansion.hold",Target=Entity(g,"source"),Priority=50}).Arg0);
                twin=f.Load(g.Save());Reach(g,twin,()=>PermanentRemovalTests.Node(g,hold,"retain").Progress.WholeTicks>0,"ordinary authored tool acquisition");
                var endpoint=Pools(g).Single().Endpoint;var progress=PermanentRemovalTests.Node(g,hold,"retain");
                c.That(progress.Progress.WholeTicks>0,"actual timed activity holds original pool resources");
                c.Equal(Fix.FromInt(2),Pools(g).Single().OutboundPromises.Aggregate(Fix.Zero,(sum,q)=>sum+q.Amount),"original pool owns the actual tool receipt");
                long disconnected=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=Intent(15)}).Arg0;
                twin=f.Load(g.Save());Reach(g,twin,()=>PermanentRemovalTests.Plan(g,disconnected).State==PlanState.Done,"unrelated physical expansion");
                c.That(Pools(g).Any(p=>p.Endpoint==endpoint&&p.MemberCount==3),"unaffected claimed endpoint retained exactly");
                long expansion=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=Intent(9)}).Arg0;
                twin=f.Load(g.Save());
                Reach(g,twin,()=>Waiting(g,expansion),"earned expansion promise wait");
                var waiting=PermanentRemovalTests.Node(g,PermanentRemovalTests.Plan(g,expansion).Order,"segment.0.install");
                c.Equal(NodeState.Completing,waiting.State,"earned construction retained, not failed or restarted");
                c.Equal(Fix.Zero,At(g,9),"refused expansion has no committed geometry prefix");
                c.That(Pools(g).Any(p=>p.Endpoint==endpoint&&p.MemberCount==3),"active actual endpoint and receipts never replaced");
                c.Equal(Fix.FromInt(30),Total(g),"initial six plus unrelated two plus earned adjacent two paid once");
                ulong pure=g.Hash();c.Equal(0,Pools(g,1).Count,"owned pool withheld from other seat");At(g,9);c.Equal(pure,g.Hash(),"waiting facts pure");
                twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"pending completion and pool promises checkpoint through original owners");
                for(int i=0;i<6;i++)Step(g,twin);
                c.Equal(waiting.Progress,PermanentRemovalTests.Node(g,PermanentRemovalTests.Plan(g,expansion).Order,"segment.0.install").Progress,"retry earns no duplicate work");
                c.Equal(Fix.Zero,At(g,9),"retry cannot bypass live receipts");
                if(cancel) { PermanentRemovalTests.Submit(c,g,new CancelOrderCommand{Order=hold});twin=f.Load(g.Save()); }
                Reach(g,twin,()=>PermanentRemovalTests.Plan(g,expansion).State==PlanState.Done,"receipt settlement resumes physical topology");
                c.Equal(Fix.Parse("1.5"),At(g,9),"earned segment installed after actual release");
                c.Equal(5,Pools(g).Sum(p=>p.MemberCount),"original graph grows four connected plus one separate vertex");
                c.That(!Pools(g).Any(p=>p.Endpoint==endpoint),"changed pool receives actual new endpoint only after settlement");
                c.That(Pools(g).All(p=>p.OutboundPromises.Count==0&&p.InboundPromises.Count==0),"settled work leaves no orphaned pool promises");
                c.Equal(Fix.FromInt(30),Total(g),"stock conserved, held tool returned and no repeated material payment");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"settled expansion durable");c.Equal(untouched,isolated.Hash(),"interleaved sessions isolated");
                c.That(new ReplayVerifier().Verify(f,record.Finish(),limits).Success,"real command/time/work/network replay");
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current expansion identity");c.Equal(154,EngineInfo.SaveFormat,"no new authoritative store or schema");
            c.Throws<ContentException>(()=>ConstructionPlanTests.NewPoints(Factory(false,root=>root.Get("target_network_resource_requirements")[0].Set("network",JsonValue.Of("missing.network")))),"unknown authored network refused before Initialize");
            foreach(bool half in new[]{false,true})foreach(bool cancel in new[]{false,true})Physical(c,half,cancel);
        }
    }
}
