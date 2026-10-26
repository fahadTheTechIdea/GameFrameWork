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
    // Thin players only: native JSON, commands, immutable views, Update and durability.
    internal static class ScopedRecoveryMatchingTests
    {
        internal const string Case = "Scoped recovery matching: same-site physical supply, named cargo returns and evacuation";
        private static readonly Code FirstPort=new Code("a-stock"), SecondPort=new Code("other"), FarPort=new Code("return");
        private static readonly ResourceEndpointId First=new ResourceEndpointId("proof.pools",9000000001),
            Second=new ResourceEndpointId("proof.pools",9000000002), Third=new ResourceEndpointId("proof.pools",9000000003),
            Fourth=new ResourceEndpointId("proof.pools",9000000004);
        internal static void Register() => TestRunner.Add(Case,Run);
        private static EntityId Find(SimGame game,string name) => game.Seat(0).Snapshot.Entities.Single(e=>e.Def==name).Id;
        private static ResourceId Steel(SimGame game)
        { if(!game.Seat(0).View.TryFindResource("steel",out var id)) throw new InvalidOperationException("missing steel"); return id; }
        private static LogisticsView Logistics(SimGame game,int seat=0) => game.Seat(seat).View.Feature<LogisticsView>();
        private static List<WorkJobView> Jobs(SimGame game)
        { var list=new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(list); return list; }
        private static bool Live(WorkJobView job) => job.State==JobState.Open || job.State==JobState.Assigned || job.State==JobState.Paused;
        private static WorkJobView Job(SimGame game,int id) => Jobs(game).Single(j=>j.Id==id);
        private static List<LogisticsDispatchOffer> Queue(SimGame game,int seat=0)
        { var list=new List<LogisticsDispatchOffer>(); Logistics(game,seat).CopyQueue(list); return list; }
        private static LogisticsOperationalSnapshot Facts(SimGame game,string site,Code port=default,ResourceEndpointId? original=null)
        {
            Code refusal=Logistics(game).ReadDiagnostics(Find(game,site),Steel(game),out var facts,port,original);
            if(!refusal.IsNone) throw new InvalidOperationException("facts "+site+"/"+port+"/"+refusal);
            return facts;
        }
        private static Fix Stock(SimGame game,string site,Code port=default,ResourceEndpointId? original=null)
            => Facts(game,site,port,original).Stock;
        private static void Step(SimGame game,SimGame twin=null)
        {
            game.Update(); twin?.Update();
            if(game.Fault!=null || twin?.Fault!=null || twin!=null && game.Hash()!=twin.Hash())
                throw new InvalidOperationException("recovery Update "+game.Tick+"/"+game.Fault+"/"+twin?.Fault);
        }
        private static void Until(SimGame game,Func<bool> done,string stage,SimGame twin=null)
        {
            for(int i=0;i<1000&&!done();i++) Step(game,twin);
            if(!done()) throw new InvalidOperationException(stage+" at "+game.Tick+
                " jobs="+string.Join(";",Jobs(game).Select(j=>j.Id+"/"+j.State+"/"+j.ToilIndex+"/"+j.Loaded+"/"+j.DestinationPort+"/"+j.DestinationEndpoint+"/"+j.Blocker))+
                " queue="+string.Join(";",Queue(game).Select(q=>q.Kind+"/"+q.DestinationPort+"/"+q.DestinationEndpoint+"/"+q.Haul.Status+"/"+q.Haul.Blocker)));
        }
        private static long Command(Check c,SimGame game,Command command)
        {
            ulong before=game.Hash(); c.Equal(Code.None,game.Seat(0).Preview(command),"public native command preview");
            c.Equal(before,game.Hash(),"command preview pure"); var reply=TestKit.Order(game,0,command);
            c.That(reply.Accepted,"native command execution "+reply.Refusal);
            if(!reply.Accepted || game.Fault!=null) throw new InvalidOperationException("command "+reply.Refusal+"/"+game.Fault);
            return reply.Arg0;
        }
        private static SimGame Save(Check c,GameFactory factory,SimGame game)
        { var twin=factory.Load(game.Save()); c.Equal(game.Hash(),twin.Hash(),"one native recovery/matching checkpoint"); return twin; }
        private static void Band(Check c,SimGame game,string site,Code port,int amount,int priority,bool enabled=true)
            => Command(c,game,new ConfigureStockPolicyCommand {Store=Find(game,site),Port=port,Resource=Steel(game),
                Change=LogisticsDemandChange.Replace(new LogisticsDemandSettings(enabled,Fix.FromInt(amount),Fix.FromInt(amount),Fix.FromInt(amount),priority,LogisticsReserveAccess.Protected))});
        private static void Select(Check c,SimGame game,string site,Code port,ResourceEndpointId endpoint)
            => Command(c,game,new SelectResourcePortCommand {Entity=Find(game,site),Port=port,Endpoint = new ResourceEndpointId("proof.pools", endpoint.Identity)});
        private static SimGame New(GameFactory factory,string scenario)
            => factory.NewGame(new NewGameRequest {Scenario=scenario,Seed=971,Seats={Controller.Human,Controller.Human}});
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current actual-endpoint recovery semantics");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"no saved layout or compatibility reader");
            var factory=ScopedHaulGame.Factory(scopedRecovery:true);
            SupplyAndReturn(c,factory); Evacuation(c,factory);
        }
        private static void SupplyAndReturn(Check c,GameFactory factory)
        {
            var game=New(factory,"scoped-recovery"); var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,6000));
            c.That(!game.Modules.Contains("sim.tycoon"),"resource recovery requires no economy");
            EntityId site=Find(game,"source.store"), carrier=Find(game,"haul.machine");
            c.That(!game.Seat(0).View.Feature<InventoryView>().TryGetCapacity(site, Code.None,out _),"named-only site has no implicit local stock");
            int funding=checked((int)Command(c,game,new HaulResourcesCommand {Source=Find(game,"source.warehouse"),Destination=site,
                SourcePort=new Code("stock"),DestinationPort=FirstPort,Worker=carrier,Resource=Steel(game),Ceiling=Fix.FromInt(8),Priority=100}));
            Until(game,()=>!Jobs(game).Any(j=>j.Id==funding&&Live(j)),"physical scoped source funding");
            c.Equal(Fix.FromInt(8),Stock(game,"source.store",FirstPort),"physical public supply stock");
            c.Equal(Fix.Zero,Stock(game,"source.store",SecondPort),"distinct same-site store starts empty");
            ulong pure=game.Hash();
            c.Equal(HaulStatus.InvalidRequest,Logistics(game).PreviewHaul(site,site,Steel(game),Fix.One,sourcePort:FirstPort,destinationPort:new Code("z-stock")).Status,
                "actual same-store alias refuses self transfer");
            c.Equal(pure,game.Hash(),"self-transfer preview pure");
            Band(c,game,"source.store",SecondPort,6,100);
            Until(game,()=>Jobs(game).Any(j=>Live(j)&&j.Source==site&&j.Destination==site),"automatic distinct same-site dispatch");
            var transfer=Jobs(game).Single(j=>Live(j)&&j.Source==site&&j.Destination==site);
            c.Equal(First,transfer.SourceEndpoint,"same-site matcher selects actual public source");
            c.Equal(Second,transfer.DestinationEndpoint,"same-site matcher keeps independent actual target");
            c.Equal(FirstPort,transfer.SourcePort,"equivalent actual sources have ordinal authored-name tie break");
            c.Equal(Fix.Zero,transfer.Loaded,"assignment does not move stock instantly");
            var twin=Save(c,factory,game);
            Until(game,()=>Job(game,transfer.Id).Loaded.IsPositive,"same-site native timed pickup",twin);
            c.That(Job(game,transfer.Id).Loaded<Fix.FromInt(6),"same-site loading still consumes game time");
            c.Equal(Fix.FromInt(8),Stock(game,"source.store",FirstPort)+Stock(game,"source.store",SecondPort)+Stock(game,"haul.machine"),
                "same-site partial transfer conserves physical quantities");
            twin=Save(c,factory,game);
            Until(game,()=>!Jobs(game).Any(j=>j.Id==transfer.Id&&Live(j)),"same-site timed delivery and departure",twin);
            c.Equal(Fix.FromInt(2),Stock(game,"source.store",FirstPort),"actual source lost only six");
            c.Equal(Fix.FromInt(6),Stock(game,"source.store",SecondPort),"actual same-site target received six");
            c.Equal(Fix.Zero,Stock(game,"haul.machine"),"carrier empties through unloading only");
            Band(c,game,"source.store",SecondPort,6,100,false);

            int interrupted=checked((int)Command(c,game,new HaulResourcesCommand {Source=Find(game,"source.warehouse"),Destination=Find(game,"source.sink"),
                SourcePort=new Code("stock"),DestinationPort=new Code("tank"),Worker=carrier,Resource=Steel(game),Ceiling=Fix.FromInt(8),Priority=100}));
            Until(game,()=>Job(game,interrupted).Loaded==Fix.FromInt(8)&&Job(game,interrupted).ToilIndex==2,"ordinary complete pickup before player interruption");
            Command(c,game,new SetWorkerHeldCommand {Worker=carrier,Held=true});
            c.That(!Jobs(game).Any(j=>j.Id==interrupted&&Live(j)),"player interruption releases original job");
            c.Equal(Fix.FromInt(8),Stock(game,"haul.machine"),"interruption retains actual cargo");
            c.Equal(Fix.Zero,Stock(game,"source.sink",new Code("tank"),Third),"original cancelled destination receives no cargo");
            Band(c,game,"source.store",SecondPort,9,200);
            Band(c,game,"source.store",FirstPort,5,20);
            Band(c,game,"recovery.far",FarPort,2,100);
            Until(game,()=>Queue(game).Any(q=>q.Kind==LogisticsDispatchKind.CargoReturn),"native residual recovery observation");
            ulong held=game.Hash(); var waiting=Queue(game);
            c.Equal(0,Queue(game,1).Count,"recovery and actual port demand facts company-private");
            c.Equal(held,game.Hash(),"queue observation owns no stock/jobs/age");
            c.That(waiting.Where(q=>q.Kind==LogisticsDispatchKind.CargoReturn).All(q=>q.Haul.Status!=HaulStatus.Ready),"held carrier waits physically");
            twin=Save(c,factory,game);
            Command(c,game,new SetWorkerHeldCommand {Worker=carrier,Held=false});
            twin=Save(c,factory,game);
            Until(game,()=>Jobs(game).Any(j=>Live(j)&&!j.Source.IsValid&&j.DestinationEndpoint==Second),"highest actual named demand return",twin);
            var first=Jobs(game).Single(j=>Live(j)&&!j.Source.IsValid);
            c.Equal(SecondPort,first.DestinationPort,"native return retains selected authored port");
            c.Equal(Second,first.DestinationEndpoint,"native return claims real second store");
            c.Equal(Fix.FromInt(3),first.Amount,"policy caps actual return to three-unit second-store gap");
            c.Equal(Fix.FromInt(3),first.Loaded,"return binds already carried material, no pickup fabrication");
            c.Equal(ResourceEndpointId.ForEntity(carrier),first.SourceEndpoint,"native return retains actual cargo identity");
            c.Equal(Fix.FromInt(3),Facts(game,"source.store",SecondPort).InboundPromised,"one actual replacement delivery promise");
            Select(c,game,"source.store",SecondPort,First);
            c.Equal(First,Facts(game,"source.store",SecondPort).Endpoint,"current selection changed");
            c.Equal(Second,Job(game,first.Id).DestinationEndpoint,"acquired return remains on original actual store");
            twin=Save(c,factory,game);
            Until(game,()=>Jobs(game).Any(j=>Live(j)&&!j.Source.IsValid&&j.DestinationEndpoint==Fourth),"next demand priority selects farther actual store",twin);
            c.Equal(Fix.FromInt(9),Stock(game,"source.store",SecondPort,Second),"first original store physically received its three");
            var second=Jobs(game).Single(j=>Live(j)&&!j.Source.IsValid);
            c.Equal(FarPort,second.DestinationPort,"far named-only endpoint participates in same selector");
            c.Equal(Fix.FromInt(2),second.Amount,"next native gap is exactly two");
            c.Equal(Fix.FromInt(5),Stock(game,"haul.machine"),"return releases only delivered first prefix");
            Select(c,game,"recovery.far",FarPort,Second);
            twin=Save(c,factory,game);
            Until(game,()=>Jobs(game).Any(j=>Live(j)&&!j.Source.IsValid&&j.DestinationEndpoint==First),"remaining lower-priority actual return",twin);
            c.Equal(Fix.FromInt(2),Stock(game,"recovery.far",FarPort,Fourth),"changed far selection did not relocate acquired delivery");
            c.Equal(FirstPort,Jobs(game).Single(j=>Live(j)&&!j.Source.IsValid).DestinationPort,"same-store aliases choose stable first name");
            c.Equal(Fix.FromInt(3),Stock(game,"haul.machine"),"only real remaining three await final return");
            twin=Save(c,factory,game);
            Until(game,()=>Stock(game,"haul.machine").IsZero&&!Jobs(game).Any(Live),"all original cargo physically returned",twin);
            c.Equal(Fix.FromInt(5),Stock(game,"source.store",FirstPort,First),"final original first store receives exact three");
            c.Equal(Fix.FromInt(9),Stock(game,"source.store",SecondPort,Second),"second demand remains exact despite aliases");
            c.Equal(Fix.FromInt(2),Stock(game,"recovery.far",FarPort,Fourth),"far actual demand remains exact");
            c.Equal(Fix.FromInt(30),Stock(game,"source.warehouse")+Stock(game,"source.store",FirstPort,First)+
                Stock(game,"source.store",SecondPort,Second)+Stock(game,"recovery.far",FarPort,Fourth)+Stock(game,"haul.machine"),
                "all physical same-site supply and split recovery conserve original thirty");
            Step(game,twin);
            c.That(!Queue(game).Any(q=>q.Kind==LogisticsDispatchKind.CargoReturn),"resolved native recovery intent retires");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,6000)).Success,"same-site and named recovery replay through native commands");
        }
        private static void Evacuation(Check c,GameFactory factory)
        {
            var game=New(factory,"scoped-evacuation"); var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,3000));
            EntityId origin=Find(game,"recovery.evac");
            Band(c,game,"source.store",SecondPort,6,50);
            Until(game,()=>Jobs(game).Any(j=>Live(j)&&j.Source==origin),"native local evacuation selects named actual destination");
            var haul=Jobs(game).Single(j=>Live(j)&&j.Source==origin);
            c.Equal(SecondPort,haul.DestinationPort,"evacuation retains named target");
            c.Equal(Second,haul.DestinationEndpoint,"evacuation claims actual scoped capacity");
            c.Equal(Fix.FromInt(6),haul.Amount,"native evacuation binds only original six units");
            var twin=Save(c,factory,game);
            Until(game,()=>Job(game,haul.Id).Loaded==Fix.FromInt(6),"evacuation physical pickup",twin);
            Select(c,game,"source.store",SecondPort,First);
            twin=Save(c,factory,game);
            Until(game,()=>Stock(game,"source.store",SecondPort,Second)==Fix.FromInt(6)&&!Jobs(game).Any(Live),"original named evacuation delivery/departure",twin);
            c.Equal(Fix.Zero,Stock(game,"source.store",SecondPort),"newly selected empty first store is not a delivery fallback");
            c.Equal(Fix.Zero,Stock(game,"haul.machine"),"evacuation cargo fully unloaded");
            c.Equal(game.Hash(),twin.Hash(),"original named evacuation saved twins settle identically");
            c.That(game.Fault==null,"same original native evacuation protocol remains fault-free");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,3000)).Success,"named evacuation replay needs no host gameplay");
        }
    }
}

