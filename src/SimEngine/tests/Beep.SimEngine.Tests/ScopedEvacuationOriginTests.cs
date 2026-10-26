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
    // Normal JSON and public player commands; no endpoint stock/progress writes or host gameplay.
    internal static class ScopedEvacuationOriginTests
    {
        internal const string Case="Scoped evacuation origins: actual named stores, aliases, selection and physical durability";
        private static readonly Code FirstPort=new Code("a-first"), Alias=new Code("z-alias"), Other=new Code("other"),
            Private=new Code("private"), Far=new Code("return");
        private static readonly ResourceEndpointId First=new ResourceEndpointId("proof.pools",9000000005),
            Second=new ResourceEndpointId("proof.pools",9000000006), Hidden=new ResourceEndpointId("proof.pools",9000000007);
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static EntityId Find(SimGame g,string id)=>g.Seat(0).Snapshot.Entities.Single(e=>e.Def==id).Id;
        private static ResourceId Steel(SimGame g)
        { if(!g.Seat(0).View.TryFindResource("steel",out var r))throw new InvalidOperationException("missing steel");return r; }
        private static LogisticsView Logistics(SimGame g,int seat=0)=>g.Seat(seat).View.Feature<LogisticsView>();
        private static List<WorkJobView> Jobs(SimGame g)
        {var jobs=new List<WorkJobView>();g.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);return jobs;}
        private static bool Live(WorkJobView j)=>j.State==JobState.Open||j.State==JobState.Assigned||j.State==JobState.Paused;
        private static WorkJobView Job(SimGame g,int id)=>Jobs(g).Single(j=>j.Id==id);
        private static List<LogisticsDispatchOffer> Queue(SimGame g,int seat=0)
        {var q=new List<LogisticsDispatchOffer>();Logistics(g,seat).CopyQueue(q);return q;}
        private static LogisticsOperationalSnapshot Facts(SimGame g,string site,Code port=default,ResourceEndpointId? original=null)
        {
            var refusal=Logistics(g).ReadDiagnostics(Find(g,site),Steel(g),out var f,port,original);
            if(!refusal.IsNone)throw new InvalidOperationException("origin facts "+site+"/"+refusal);return f;
        }
        private static void Step(SimGame g,SimGame twin=null)
        {
            g.Update();twin?.Update();
            if(g.Fault!=null||twin?.Fault!=null||twin!=null&&g.Hash()!=twin.Hash())
                throw new InvalidOperationException("origin Update "+g.Tick+"/"+g.Fault+"/"+twin?.Fault);
        }
        private static void Until(SimGame g,Func<bool> done,string stage,SimGame twin=null)
        {
            for(int i=0;i<1000&&!done();i++)Step(g,twin);
            if(!done())throw new InvalidOperationException(stage+" at "+g.Tick+" jobs="+
                string.Join(";",Jobs(g).Select(j=>j.Id+"/"+j.State+"/"+j.SourcePort+"/"+j.SourceEndpoint+"/"+j.Loaded+"/"+j.Blocker))+
                " queue="+string.Join(";",Queue(g).Select(q=>q.Kind+"/"+q.SourcePort+"/"+q.SourceEndpoint+"/"+q.Requested+"/"+q.Haul.Status)));
        }
        private static long Command(Check c,SimGame g,Command command)
        {
            ulong h=g.Hash();c.Equal(Code.None,g.Seat(0).Preview(command),"ordinary preview");
            c.Equal(h,g.Hash(),"preview pure");var reply=TestKit.Order(g,0,command);
            c.That(reply.Accepted,"ordinary native command "+reply.Refusal);
            if(!reply.Accepted||g.Fault!=null)throw new InvalidOperationException("origin command "+reply.Refusal+"/"+g.Fault);
            return reply.Arg0;
        }
        private static SimGame Save(Check c,GameFactory factory,SimGame g)
        {var twin=factory.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"exact origin chronology/claims checkpoint");return twin;}
        private static void Fund(Check c,SimGame g,Code port,int amount)
        {
            int id=checked((int)Command(c,g,new HaulResourcesCommand{Source=Find(g,"source.warehouse"),
                Destination=Find(g,"evac.origins"),SourcePort=new Code("stock"),DestinationPort=port,
                Worker=Find(g,"haul.machine"),Resource=Steel(g),Ceiling=Fix.FromInt(amount),Priority=100}));
            Until(g,()=>!Jobs(g).Any(j=>j.Id==id&&Live(j)),"normal physical funding "+port);
            c.Equal(Fix.FromInt(amount),Facts(g,"evac.origins",port).Stock,"only delivered funding becomes actual origin stock");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"exact named-origin native identity");
            c.Equal(154,EngineInfo.SaveFormat,"source binding serialized without old reader");
            var factory=ScopedHaulGame.Factory(scopedEvacuation:true);
            var g=factory.NewGame(new NewGameRequest{Scenario="scoped-evacuation-origins",Seed=983,Seats={Controller.Human,Controller.Human}});
            var recorder=g.RecordReplay(new ReplayLimits(4*1024*1024,6000));
            c.That(!g.Modules.Contains("sim.tycoon"),"evacuation resource-only");
            c.That(!g.Seat(0).View.Feature<InventoryView>().TryGetCapacity(Find(g,"evac.origins"), Code.None,out _),"no local inventory fallback");
            Fund(c,g,FirstPort,6);Fund(c,g,Other,4);Fund(c,g,Private,3);
            Command(c,g,new SetWorkerHeldCommand{Worker=Find(g,"haul.machine"),Held=true});
            Until(g,()=>Queue(g).Count(q=>q.Kind==LogisticsDispatchKind.Evacuation)==2,"two exact original evacuation intents");
            ulong h=g.Hash();var waiting=Queue(g).Where(q=>q.Kind==LogisticsDispatchKind.Evacuation).ToList();
            c.Equal(2,waiting.Count,"distinct stores independent, aliases deduplicated");
            c.Equal(1,waiting.Count(q=>q.SourceEndpoint==First),"first actual store owns one chronology");
            c.Equal(1,waiting.Count(q=>q.SourceEndpoint==Second),"second actual store owns one chronology");
            c.That(waiting.All(q=>q.SourceEndpoint!=Hidden),"private origin not discovered");
            c.Equal(FirstPort,waiting.Single(q=>q.SourceEndpoint==First).SourcePort,"ordinal alias owns original physical binding");
            c.Equal(Fix.FromInt(6),waiting.Single(q=>q.SourceEndpoint==First).Requested,"first actual request");
            c.Equal(Fix.FromInt(4),waiting.Single(q=>q.SourceEndpoint==Second).Requested,"second actual request");
            c.Equal(0,Queue(g,1).Count,"company-private origins");
            c.Equal(h,g.Hash(),"queue reads never mutate chronology");
            var twin=Save(c,factory,g);
            for(int i=0;i<3;i++)Step(g,twin);
            c.Equal(waiting.Single(q=>q.SourceEndpoint==First).CreatedTick,
                Queue(g).Single(q=>q.Kind==LogisticsDispatchKind.Evacuation&&q.SourceEndpoint==First).CreatedTick,
                "blocked original age not rebased by reads or saves");
            Command(c,g,new ConfigureStockPolicyCommand{Store=Find(g,"recovery.far"),Port=Far,Resource=Steel(g),
                Change=LogisticsDemandChange.Replace(new LogisticsDemandSettings(true,Fix.FromInt(10),Fix.FromInt(10),Fix.FromInt(10),100,LogisticsReserveAccess.Protected))});
            Command(c,g,new SetWorkerHeldCommand{Worker=Find(g,"haul.machine"),Held=false});
            Until(g,()=>Jobs(g).Any(j=>Live(j)&&j.SourceEndpoint==First&&j.Source==Find(g,"evac.origins")),"normal named evacuation admission");
            int first=Jobs(g).Single(j=>Live(j)).Id;
            c.Equal(FirstPort,Job(g,first).SourcePort,"job retains admitted source name");
            c.Equal(Fix.FromInt(6),Job(g,first).Amount,"actual first store evacuation quantity");
            c.Equal(Fix.Zero,Job(g,first).Loaded,"admission never teleports pickup");
            c.Equal(Fix.FromInt(6),Facts(g,"evac.origins",FirstPort).OutboundPromised,"original actual outbound receipt");
            c.Equal(Fix.Zero,Facts(g,"evac.origins",Other).OutboundPromised,"independent second has no first-job receipt");
            Command(c,g,new SelectResourcePortCommand{Entity=Find(g,"evac.origins"),Port=FirstPort,Endpoint = new ResourceEndpointId("proof.pools", Second.Identity)});
            c.Equal(Second,Facts(g,"evac.origins",FirstPort).Endpoint,"current selection switches future origin only");
            c.Equal(First,Job(g,first).SourceEndpoint,"acquired source receipt stays original");
            c.Equal(FirstPort,Job(g,first).SourcePort,"acquired source name is not rewritten");
            var currentOrigins=Queue(g).Where(q=>q.Kind==LogisticsDispatchKind.Evacuation).ToList();
            c.Equal(2,currentOrigins.Count,"selection rediscovery still has one intent per actual store");
            c.Equal(Alias,currentOrigins.Single(q=>q.SourceEndpoint==First).SourcePort,"future origin uses surviving current alias");
            c.Equal(waiting.Single(q=>q.SourceEndpoint==First).CreatedTick,
                currentOrigins.Single(q=>q.SourceEndpoint==First).CreatedTick,"actual original age survives alias reselection");
            twin=Save(c,factory,g);
            Until(g,()=>Job(g,first).Loaded.IsPositive,"real named partial pickup",twin);
            c.That(Job(g,first).Loaded<Fix.FromInt(6),"loading requires elapsed game time");
            c.Equal(Fix.FromInt(6),Facts(g,"evac.origins",Alias).Stock+Facts(g,"haul.machine").Stock,"original partial pickup conserved");
            twin=Save(c,factory,g);
            Until(g,()=>Jobs(g).Any(j=>Live(j)&&j.SourceEndpoint==Second&&j.Source==Find(g,"evac.origins")),"second original named store evacuation",twin);
            c.Equal(Fix.FromInt(6),Facts(g,"recovery.far",Far).Stock,"first exact delivery earned physically");
            c.Equal(Fix.Zero,Facts(g,"evac.origins",Alias).Stock,"first original exhausted, no current-selection substitute");
            int second=Jobs(g).Single(j=>Live(j)).Id;
            c.Equal(Second,Job(g,second).SourceEndpoint,"second actual origin independent");
            c.Equal(Fix.FromInt(4),Job(g,second).Amount,"second actual four only");
            twin=Save(c,factory,g);
            Until(g,()=>!Jobs(g).Any(Live)&&Facts(g,"haul.machine").Stock.IsZero,"native complete delivery/departure",twin);
            c.Equal(Fix.FromInt(10),Facts(g,"recovery.far",Far).Stock,"both actual named origins physically delivered");
            c.Equal(Fix.FromInt(3),Facts(g,"evac.origins",Private).Stock,"private evacuating store untouched");
            c.Equal(Fix.Zero,Facts(g,"evac.origins",Alias).OutboundPromised,"original First claims settled");
            c.Equal(Fix.Zero,Facts(g,"evac.origins",Other).OutboundPromised,"original Second claims settled");
            c.Equal(Fix.Zero,Facts(g,"recovery.far",Far).InboundPromised,"delivery promises settled");
            c.Equal(Fix.FromInt(30),Facts(g,"source.warehouse").Stock+Facts(g,"recovery.far",Far).Stock+
                Facts(g,"evac.origins",Private).Stock+Facts(g,"haul.machine").Stock,"original thirty conserved");
            c.That(g.Seat(0).Snapshot.Entities.Any(e=>e.Id==Find(g,"evac.origins")),"empty named store does not despawn unrelated physical facility");
            c.Equal(g.Hash(),twin.Hash(),"saved exact origin continuation identical");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,6000)).Success,"native named-origin player replay");
        }
    }
}
