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
    // Actual JSON/master and public seat intent/observations/Update only. No direct stock, claims or geometry.
    internal static class ConnectedActivitySourceTests
    {
        internal const string Case="Connected activity sources: original target and worker network claims survive physical refusal and saved release";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(bool point,bool half,bool independent,Action<JsonValue> extra=null)
        {
            var authored=Json.Parse(FrameworkProofContent.Read("network-source-work.json"));
            if(independent){authored.Set("independent_network_resources",authored.Get("target_network_resource_requirements"));authored.Remove("target_network_resource_requirements");}
            if(!point) {
                foreach(string section in new[]{independent?"independent_network_resources":"target_network_resource_requirements","worker_network_resource_requirements"}) {
                    var row=authored.Get(section)[0];row.Set("network",JsonValue.Of("binding.main"));row.Set("quantities",Json.Parse("{\"steel\":2}"));
                }
                authored.Get("activities")[1].Get("nodes")[0].Set("roles",Json.Parse("[\"crane\"]"));
                authored.Get("resource_signals")[0].Set("resource",JsonValue.Of("steel"));authored.Get("resource_signals")[0].Set("unit",JsonValue.Of("unit.steel"));
            }
            extra?.Invoke(authored);
            var declarations="["+FrameworkProofContent.Read("network-source-work-module.json")+"]";
            var documents=new Dictionary<string,string>{{"network-source-work.json",authored.ToString()}};
            var retry=Json.Parse(FrameworkProofContent.Read("work-retry.json"));
            retry.Set("work_retry_profiles",Json.Parse(FrameworkProofContent.Read("completion-retry.json")).Get("work_retry_profiles"));
            if(!point) {
                documents.Add("work-retry.json",retry.ToString());
                return PhysicalLineRemovalTests.Create(false,extraModules:new GameModule[]{new NetworkResourceSourceModule()},
                    extraDeclarations:declarations,extraDocuments:documents,editDefinitions:root=>{
                        if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));
                    },editNetwork:root=>{
                        var band=root.Get("entities")[0].Get("components").Get("signal_network_gate").Get("conditions")[0];
                        band.Set("signal",JsonValue.Of("binding.stored"));band.Set("minimum",JsonValue.Number("0"));band.Set("maximum",JsonValue.Number("3"));
                    });
            }
            return SpatialNetworkGraphTests.Points(half,new PointLineGeometryProofModule(),
                moreDocuments:new Dictionary<string,string>{{"point-owned-network.json",FrameworkProofContent.Read("point-owned-network.json")}},
                extraModules:new GameModule[]{new NetworkResourceSourceModule()},extraDeclarations:declarations,moduleDocuments:documents,
                editCorridors:root=>{root.Get("lines")[0].Set("owned",JsonValue.Of(true));root.Get("lines")[0].Set("owner_layer",JsonValue.Of("corridor.company"));},
                editNetwork:root=>root.Get("networks")[0].Set("public",JsonValue.Of(false)),retryProfiles:retry.ToString(),
                afterPorts:root=>{
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.network-sources"));
                    root.Get("entities")[1].Get("components").Get("ports").Get("list")[0].Set("pull",Json.Parse("[]"));
                    root.Get("entity_setups")[1].Get("placement").Set("position",JsonValue.Number("20"));
                    root.Get("entities")[2].Get("components").Set("ports",Json.Parse("{\"list\":[{\"network\":\"point.network\",\"push\":[],\"pull\":[]}]}"));
                    root.Get("entities")[0].Get("components").Set("network_signal",Json.Parse("{\"id\":\"source.pool\",\"network\":\"point.network\"}"));
                    root.Get("entities")[0].Get("components").Set("signal_network_gate",Json.Parse("{\"id\":\"source.buffer\",\"networks\":[\"point.network\"],\"conditions\":[{\"id\":\"stock\",\"source\":\"source.pool\",\"signal\":\"source.pool-stock\",\"minimum\":0,\"maximum\":3,\"unit\":\"unit.supply\",\"blocker\":\"source.buffer-full\"}]}"));
                });
        }
        private static SimGame New(GameFactory f,bool point)=>point?ConstructionPlanTests.NewPoints(f):PhysicalLineRemovalTests.New(f);
        private static List<NetworkPoolView> Pools(SimGame g,bool point,int seat=0)
        {
            var rows=new List<NetworkPoolView>();Code refusal=g.Seat(seat).View.Feature<InfrastructureView>().CopyPools(point?"point.network":"binding.main",rows);
            if(!refusal.IsNone)throw new InvalidOperationException("actual connected pools "+refusal);return rows;
        }
        private static Fix Total(SimGame g,bool point)=>point?SpatialCorridorExecutionTests.Total(g,true)+Pools(g,true).SelectMany(p=>p.Stock).Aggregate(Fix.Zero,(s,q)=>s+q.Amount):PhysicalLineRemovalTests.Steel(g);
        private static OrderView Order(SimGame g,int id)=>PhysicalLineRemovalTests.Orders(g).Single(x=>x.Id==id);
        private static bool Done(SimGame g,string plan,EntityId target,int order)
            =>g.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion(plan,target,out var fact)&&fact.Order==order;
        private static void Reach(SimGame g,SimGame twin,Func<bool> ready,string stage,bool point)
        {
            for(int i=0;i<800&&!ready();i++)Step(g,twin);
            if(!ready())throw new InvalidOperationException(stage+" exceeded800Updates clock="+g.Clock.StepIndex+
                " orders="+string.Join(";",PhysicalLineRemovalTests.Orders(g).Select(x=>x.Id+"/"+x.State+"/"+x.Blocker))+
                " promises="+string.Join(";",Pools(g,point).Select(p=>p.Endpoint+"/"+p.OutboundPromises.Count)));
        }
        private static void Start(Check c,GameFactory f,SimGame g,bool point)
        {
            if(point) {
                long build=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=SpatialCorridorExecutionTests.Intent(true,false)}).Arg0;
                Reach(g,f.Load(g.Save()),()=>PermanentRemovalTests.Plan(g,build).State==PlanState.Done,"physical connected field",true);
            }
            Reach(g,null,()=>Pools(g,point).Single().Stock.Any(q=>q.Amount>=Fix.FromInt(4)),"real elapsed source pool stock",point);
        }
        private static void Physical(Check c,bool point,bool half,bool independent)
        {
            var f=Factory(point,half,independent);var g=New(f,point);var isolated=New(f,point);ulong untouched=isolated.Hash();
            var limits=new ReplayLimits(4*1024*1024,4000);
            using(var record=g.RecordReplay(limits)) {
                Start(c,f,g,point);Fix initial=Total(g,point);
                var cut=new PostOrderCommand{Plan=point?"corridor.remove":"removal.work",Target=Entity(g,point?"corridor.removal-site":"removal.site"),Priority=100};
                int remove=checked((int)PermanentRemovalTests.Submit(c,g,cut).Arg0);
                int hold=checked((int)PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="source.hold",Target=Entity(g,point?"source":"binding.source"),Priority=20}).Arg0);
                var twin=f.Load(g.Save());
                Reach(g,twin,()=>Pools(g,point).Single().OutboundPromises.Any(q=>q.Amount==Fix.FromInt(2)),"actual connected target tool claim",point);
                var retained=Pools(g,point).Single();
                Reach(g,twin,()=>Order(g,remove).Blocker==ResourceTransactions.ActivePromises,"earned physical removal waits for real tools",point);
                c.Equal(OrderState.Completing,Order(g,remove).State,"original earned removal is pending completion");
                c.Equal(point?Fix.Parse("1.5"):Fix.One,point?SpatialCorridorExecutionTests.Segment(g,true,1):Fix.FromInt(PhysicalLineRemovalTests.Tile(g,11)),"live receipts protect actual bridge geometry");
                c.Equal(retained.Endpoint,Pools(g,point).Single().Endpoint,"same original pool endpoint retained while claimed");
                c.Equal(initial,Total(g,point),"held tools and pending removal never manufacture or destroy stock");
                c.Equal(Codes.AlreadyDone,g.Seat(0).Preview(cut),"existing intent cannot post a duplicate removal order");
                c.Equal(ResourceTransactions.ActivePromises,g.Seat(0).View.Feature<WorkView>().PreviewOrderEffect(remove),"existing earned effect reports actual claim refusal");
                ulong pure=g.Hash();c.Equal(0,Pools(g,point,1).Count,"private shared stock withheld");c.Equal(pure,g.Hash(),"immutable pool views pure");
                twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"physical effect wait and original claims durable");
                if(independent){PermanentRemovalTests.Submit(c,g,new CancelOrderCommand{Order=hold});twin=f.Load(g.Save());}
                Reach(g,twin,()=>Pools(g,point).Count==2,"ordinary release allows original split",point);
                c.That(Pools(g,point).All(p=>p.OutboundPromises.Count==0&&p.InboundPromises.Count==0),"no orphaned native or independent source promises");
                c.Equal(initial,Total(g,point),"actual split conserves source and pool quantities");
                c.Equal(Fix.FromInt(2),retained.OutboundPromises.Single().Amount,"retained pre-release receipt facts detached");
                int disconnected=checked((int)PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="source.hold",Target=Entity(g,point?"destination":"binding.remote"),Priority=20}).Arg0);
                twin=f.Load(g.Save());Reach(g,twin,()=>PermanentRemovalTests.Node(g,disconnected,"hold").Blocker==NetworkService.Disconnected,"typed target connectivity blocker",point);
                c.Equal(WorkProgress.Zero,PermanentRemovalTests.Node(g,disconnected,"hold").Progress,"unconnected target earns no resource-backed work");
                c.That(Pools(g,point).All(p=>p.OutboundPromises.Count==0),"unconnected source acquires no substitute tool");
                PermanentRemovalTests.Submit(c,g,new CancelOrderCommand{Order=disconnected});
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"released and disconnected source cancellation durable");
                c.Equal(untouched,isolated.Hash(),"interleaved source sessions isolated");
                c.That(new ReplayVerifier().Verify(f,record.Finish(),limits).Success,"actual native or point source command replay");
            }
        }
        private static void Worker(Check c,bool half)
        {
            var f=Factory(true,half,false);var g=New(f,true);var limits=new ReplayLimits(4*1024*1024,4000);
            using(var record=g.RecordReplay(limits)) {
                Start(c,f,g,true);Fix initial=Total(g,true);
                int work=checked((int)PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="source.worker-hold",Target=Entity(g,"source"),Priority=80}).Arg0);
                var twin=f.Load(g.Save());Reach(g,twin,()=>Pools(g,true).Single().OutboundPromises.Count>0,"candidate worker's actual connected tool source",true);
                c.Equal(Fix.FromInt(2),Pools(g,true).Single().OutboundPromises.Single().Amount,"worker requirement uses original shared pool book");
                Reach(g,twin,()=>Done(g,"activity:source.worker-hold",Entity(g,"source"),work),"physical worker finishes connected work",true);
                c.That(Pools(g,true).Single().OutboundPromises.Count==0,"worker reusable tool returns without a second inventory");
                int move=checked((int)PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="inspect",Target=Entity(g,"destination"),Priority=80}).Arg0);
                twin=f.Load(g.Save());Reach(g,twin,()=>Done(g,"activity:inspect",Entity(g,"destination"),move),"ordinary work physically moves worker away",true);
                c.That(PermanentRemovalTests.At(g,Entity(g,"machine"))>=Fix.FromInt(17),"worker is genuinely beyond authored contact reach");
                work=checked((int)PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="source.worker-hold",Target=Entity(g,"destination"),Priority=80}).Arg0);
                twin=f.Load(g.Save());for(int i=0;i<4;i++)Step(g,twin);
                c.Equal(WorkProgress.Zero,PermanentRemovalTests.Node(g,work,"work").Progress,"unconnected worker cannot earn resource-backed work");
                c.That(Pools(g,true).Single().OutboundPromises.Count==0,"no imaginary worker network connection");
                PermanentRemovalTests.Submit(c,g,new CancelOrderCommand{Order=work});
                c.Equal(initial,Total(g,true),"worker selection and travel conserve original tool quantities");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"worker connectivity blocker and cancellation durable");
                c.That(new ReplayVerifier().Verify(f,record.Finish(),limits).Success,"actual worker source travel and claim replay");
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current connected source identity");c.Equal(154,EngineInfo.SaveFormat,"original state schemas retained");
            foreach(bool point in new[]{false,true})foreach(bool half in new[]{false,true})Physical(c,point,half,point?half:!half);
            foreach(bool half in new[]{false,true})Worker(c,half);
        }
    }
}

