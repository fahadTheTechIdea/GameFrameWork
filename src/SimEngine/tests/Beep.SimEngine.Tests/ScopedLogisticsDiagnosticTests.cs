using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Thin player scenario: native commands, detached views, Update, save/load and replay only.
    internal static class ScopedLogisticsDiagnosticTests
    {
        internal const string Case = "Scoped logistics diagnostics: actual stores, committed trips and incoming history";
        private static readonly Code A = new Code("a-stock"), Z = new Code("z-stock"), Other = new Code("other"),
            Tank = new Code("tank"), Alias = new Code("alias");
        private static readonly ResourceEndpointId First = new ResourceEndpointId("proof.pools",9000000001),
            Second = new ResourceEndpointId("proof.pools",9000000002), Third = new ResourceEndpointId("proof.pools",9000000003);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static EntityId Find(SimGame game,string prototype)
            => game.Seat(0).Snapshot.Entities.Single(e=>e.Def==prototype).Id;
        private static ResourceId Steel(SimGame game)
        { if(!game.Seat(0).View.TryFindResource("steel",out var id)) throw new InvalidOperationException("missing authored steel"); return id; }
        private static LogisticsView View(SimGame game,int seat=0) => game.Seat(seat).View.Feature<LogisticsView>();
        private static List<WorkJobView> Jobs(SimGame game)
        { var rows=new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(rows); return rows; }
        private static bool Live(WorkJobView job) => job.State==JobState.Open || job.State==JobState.Assigned || job.State==JobState.Paused;
        private static void Step(SimGame game,SimGame twin=null)
        {
            game.Update(); twin?.Update();
            if(game.Fault!=null || twin?.Fault!=null || twin!=null && game.Hash()!=twin.Hash())
                throw new InvalidOperationException("diagnostic update "+game.Tick+"/"+game.Fault+"/"+twin?.Fault);
        }
        private static void Until(SimGame game,Func<bool> done,string stage,SimGame twin=null)
        {
            for(int i=0;i<1000 && !done();i++) Step(game,twin);
            if(!done())
            {
                var offers=new List<LogisticsDispatchOffer>(); View(game).CopyQueue(offers);
                throw new InvalidOperationException(stage+" at "+game.Tick+" offers="+
                    string.Join(";",offers.Select(o=>o.DestinationEndpoint+"/"+o.Haul.Status+"/"+o.Haul.Blocker))+
                    " jobs="+string.Join(";",Jobs(game).Select(j=>j.Id+"/"+j.State+"/"+j.Loaded+"/"+j.Blocker)));
            }
        }
        private static long Order(Check c,SimGame game,Command command)
        {
            ulong hash=game.Hash();
            c.Equal(Code.None,game.Seat(0).Preview(command),"native command preview");
            c.Equal(hash,game.Hash(),"command preview is pure");
            var result=TestKit.Order(game,0,command);
            c.That(result.Accepted,"normal command execution "+result.Refusal);
            if(!result.Accepted || game.Fault!=null) throw new InvalidOperationException("command "+result.Refusal+"/"+game.Fault);
            return result.Arg0;
        }
        private static void Fund(Check c,SimGame game,Code port,int quantity)
        {
            int job=checked((int)Order(c,game,new HaulResourcesCommand {
                Source=Find(game,"source.warehouse"),Destination=Find(game,"source.store"),DestinationPort=port,
                Worker=Find(game,"haul.machine"),Resource=Steel(game),Ceiling=Fix.FromInt(quantity),Priority=100 }));
            Until(game,()=>!Jobs(game).Any(j=>j.Id==job&&Live(j)),"physical funding "+port);
        }
        private static void Select(Check c,SimGame game,string site,Code port,ResourceEndpointId endpoint)
            => Order(c,game,new SelectResourcePortCommand {Entity=Find(game,site),Port=port,Endpoint = new ResourceEndpointId("proof.pools", endpoint.Identity)});
        private static void Band(Check c,SimGame game,Code port,bool enabled,int minimum,int desired,int priority)
            => Order(c,game,new ConfigureStockPolicyCommand {Store=Find(game,"source.sink"),Port=port,Resource=Steel(game),
                Change=LogisticsDemandChange.Replace(new LogisticsDemandSettings(enabled,Fix.FromInt(minimum),Fix.FromInt(desired),Fix.FromInt(10),priority,LogisticsReserveAccess.Protected))});
        private static LogisticsOperationalSnapshot Read(Check c,SimGame game,string site,Code port=default,ResourceEndpointId? endpoint=null)
        {
            ulong hash=game.Hash();
            c.Equal(Code.None,View(game).ReadDiagnostics(Find(game,site),Steel(game),out var value,port,endpoint),"actual diagnostic read");
            c.Equal(hash,game.Hash(),"diagnostic read is pure");
            if(value==null) throw new InvalidOperationException("missing diagnostics "+site+"/"+port);
            return value;
        }
        private static LogisticsThroughputSnapshot History(Check c,SimGame game,string site,Code port=default,ResourceEndpointId? endpoint=null)
        {
            ulong hash=game.Hash();
            c.Equal(Code.None,View(game).ReadIncomingThroughput(Find(game,site),Steel(game),0,game.Tick,out var value,port,endpoint),"actual incoming history");
            c.Equal(hash,game.Hash(),"history read is pure");
            if(value==null) throw new InvalidOperationException("missing throughput "+site+"/"+port);
            return value;
        }
        private static void Incoming(Check c,LogisticsThroughputSnapshot value,ResourceEndpointId endpoint,int amount,long legs)
        {
            c.Equal(endpoint,value.Endpoint,"incoming exact endpoint");
            c.Equal(new BigInteger(Fix.FromInt(amount).Raw),value.IncomingRaw,"only native incoming committed quantity");
            c.Equal(legs,value.TransferLegs,"native incremental unload transfer legs, not completed trip count");
            c.Equal(value.IncomingRaw*value.RateSecondsDenominator,
                value.RateRawNumerator*(BigInteger)value.ThroughStep*60,"native exact rate uses sole authored minute clock");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"scoped diagnostic build");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"no new persisted diagnostic state");
            var factory=ScopedHaulGame.Factory(scopedPolicies:true);
            var game=factory.NewGame(new NewGameRequest {Scenario="scoped-policies",Seed=962,Seats={Controller.Human,Controller.Human}});
            var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,4000));
            EntityId sink=Find(game,"source.sink"); ResourceId steel=Steel(game);
            ulong hash=game.Hash();
            c.That(!game.Modules.Contains("sim.tycoon"),"real logistics and diagnostics require no business module");
            c.Equal(StandardCodes.NoStorage,View(game).ReadDiagnostics(sink,steel,out var absent),"named-only site has no fabricated local inventory");
            c.That(absent==null,"refused operational result is empty");
            c.Equal(StandardCodes.NoStorage,View(game).ReadDiagnostics(sink,steel,out _,new Code("missing")),"unknown named port refuses");
            c.Equal(StandardCodes.NoStorage,View(game).ReadDiagnostics(sink,steel,out _,Tank,new ResourceEndpointId("proof.pools",99)),"unknown original has no current-selection fallback");
            c.Equal(Codes.InvalidArgument,View(game).ReadDiagnostics(sink,steel,out _,Tank,default(ResourceEndpointId)),"invalid explicit endpoint refuses");
            c.Equal(Codes.InvalidArgument,View(game).ReadDiagnostics(sink,default(ResourceId),out _,Tank),"invalid resource refuses");
            c.Equal(Codes.NotOwned,View(game,1).ReadDiagnostics(sink,steel,out _,Tank),"foreign operational facts private");
            c.Equal(Codes.NotOwned,View(game,1).ReadIncomingThroughput(sink,steel,0,1,out _,Tank),"foreign history private");
            c.Equal(StandardCodes.NoStorage,View(game).ReadIncomingThroughput(sink,steel,0,1,out _,Tank,new ResourceEndpointId("proof.pools",99)),"history original resolution has no fallback");
            c.Equal(hash,game.Hash(),"all refusal reads preserve engine state");
            c.That(game.Fault==null,"typed refusals never fault session");
            var initial=Read(c,game,"source.sink",Tank);
            c.Equal(Third,initial.Endpoint,"authored actual scoped endpoint");
            c.Equal(Tank,initial.Port,"requested named binding recorded");
            c.Equal(Fix.Zero,initial.Stock,"pools begin physically empty");
            c.Equal(0,initial.Shipments.Count,"no artificial shipments");
            Fund(c,game,Z,8); Fund(c,game,Z,4); Fund(c,game,Other,4);
            var first=Read(c,game,"source.store",Z); var alias=Read(c,game,"source.store",A); var second=Read(c,game,"source.store",Other);
            c.Equal(First,first.Endpoint,"first source store identity");
            c.Equal(first.Endpoint,alias.Endpoint,"aliases share actual endpoint");
            c.Equal(Fix.FromInt(12),first.Stock,"first store physically received two trips");
            c.Equal(first.Stock,alias.Stock,"aliases never copy or duplicate stock");
            c.Equal(Second,second.Endpoint,"second actual same-site store independent");
            c.Equal(Fix.FromInt(4),second.Stock,"second store quantity independent");
            c.Equal(Fix.FromInt(14),Read(c,game,"source.warehouse").Stock,"local read remains actual endpoint contract");
            Incoming(c,History(c,game,"source.store",Z),First,12,12);
            Incoming(c,History(c,game,"source.store",A),First,12,12);
            Incoming(c,History(c,game,"source.store",Other),Second,4,4);
            Incoming(c,History(c,game,"source.warehouse"),ResourceEndpointId.ForEntity(Find(game,"source.warehouse")),0,0);
            hash=game.Hash();
            c.Equal(Codes.InvalidArgument,View(game).ReadIncomingThroughput(sink,steel,game.Tick,game.Tick,out _,Tank),"empty history window refuses");
            c.Equal(Codes.InvalidArgument,View(game).ReadIncomingThroughput(sink,steel,0,game.Tick+1,out _,Tank),"future history window refuses");
            c.Equal(hash,game.Hash(),"invalid history windows preserve state");
            Order(c,game,new SetWorkerHeldCommand {Worker=Find(game,"haul.machine"),Held=true});
            Band(c,game,Alias,true,3,6,70);
            Select(c,game,"source.sink",Tank,Second);
            Band(c,game,Tank,true,5,8,80);
            var thirdWaiting=Read(c,game,"source.sink",Alias); var secondWaiting=Read(c,game,"source.sink",Tank);
            c.Equal(1,thirdWaiting.QueueLength,"third-store queue excludes other same-site demand");
            c.Equal(1,secondWaiting.QueueLength,"second-store queue excludes third-store demand");
            c.That(thirdWaiting.Offers.All(o=>o.DestinationEndpoint==Third),"third offers are exact scoped destination");
            c.That(secondWaiting.Offers.All(o=>o.DestinationEndpoint==Second),"second offers are exact scoped destination");
            c.Equal(Fix.FromInt(6),thirdWaiting.UnmetDesired,"third desired deficit from actual stock");
            c.Equal(Fix.FromInt(6),thirdWaiting.UnpromisedGap,"third gap independent");
            c.Equal(Fix.FromInt(4),secondWaiting.UnmetDesired,"second desired deficit uses its own stock");
            c.Equal(Fix.FromInt(4),secondWaiting.UnpromisedGap,"second gap independent");
            c.Equal(0,thirdWaiting.Shipments.Count,"held machine has not acquired third trip");
            c.Equal(0,secondWaiting.Shipments.Count,"held machine has not acquired second trip");
            c.That(secondWaiting.Carriers.Count==1&&secondWaiting.Carriers[0].Held,"carrier facts remain original companywide facts");
            // The same actual store is reachable from another physical site; facts follow its endpoint.
            var sameActual=Read(c,game,"source.store",Other);
            c.Equal(1,sameActual.QueueLength,"actual endpoint diagnostics see an alias site's demand without another queue");
            Order(c,game,new SetWorkerHeldCommand {Worker=Find(game,"haul.machine"),Held=false});
            Until(game,()=>Jobs(game).Any(j=>Live(j)&&j.HaulDemand.IsValid),"actual automatic trip");
            var trip=Jobs(game).Single(j=>Live(j)&&j.HaulDemand.IsValid);
            c.Equal(Second,trip.DestinationEndpoint,"native priority chose second demand");
            c.Equal(First,trip.SourceEndpoint,"native public source is actual first store");
            c.Equal(Fix.FromInt(4),trip.Amount,"actual deficit is physical trip quantity");
            var acquired=Read(c,game,"source.sink",Tank);
            c.Equal(Fix.FromInt(4),acquired.Stock,"stock not teleported by reservation");
            c.Equal(Fix.FromInt(4),acquired.InboundPromised,"native incoming receipt reported once");
            c.Equal(Fix.FromInt(4),acquired.UnmetDesired,"desired deficit is stock-based");
            c.Equal(Fix.Zero,acquired.UnpromisedGap,"actual incoming promise removes additional demand gap");
            c.Equal(0,acquired.QueueLength,"fully promised demand not queued again");
            c.Equal(1,acquired.Shipments.Count,"only exact destination live trip reported");
            c.Equal(0,Read(c,game,"source.sink",Alias).Shipments.Count,"other same-site endpoint never inherits live trip");
            c.Equal(trip.SourcePort,acquired.Shipments[0].SourcePort,"shipment exposes original source port");
            c.Equal(trip.DestinationPort,acquired.Shipments[0].DestinationPort,"shipment exposes original destination port");
            c.Equal(First,acquired.Shipments[0].SourceEndpoint,"shipment exposes admitted source ID");
            c.Equal(Second,acquired.Shipments[0].DestinationEndpoint,"shipment exposes admitted destination ID");
            Band(c,game,Tank,false,5,8,80); Band(c,game,Alias,false,3,6,70);
            Select(c,game,"source.sink",Tank,First);
            Select(c,game,"source.store",Z,Second);
            var current=Read(c,game,"source.sink",Tank);
            c.Equal(First,current.Endpoint,"current read resolves new selection");
            c.Equal(0,current.Shipments.Count,"new selection cannot steal original shipment diagnostic");
            var pinned=Read(c,game,"source.sink",Tank,Second);
            c.Equal(Second,pinned.Endpoint,"explicit original still resolves");
            c.Equal(1,pinned.Shipments.Count,"explicit original keeps admitted shipment");
            c.Equal(First,pinned.Shipments[0].SourceEndpoint,"changed source selection cannot rewrite original trip");
            c.Equal(Fix.FromInt(4),pinned.InboundPromised,"original capacity claim survives edits");
            c.Equal(Fix.Zero,pinned.UnmetDesired,"disabled original stock policy has no active desired deficit");
            c.Equal(1,Read(c,game,"source.store",Other).Shipments.Count,"cross-site endpoint alias shares original live trip facts");
            var twin=factory.Load(game.Save());
            c.Equal(game.Hash(),twin.Hash(),"original trips/claims checkpoint without diagnostic state");
            c.Equal(pinned.Endpoint,Read(c,twin,"source.sink",Tank,Second).Endpoint,"restored original diagnostic endpoint");
            Until(game,()=>!Jobs(game).Any(j=>j.Id==trip.Id&&Live(j)),"original physical delivery after selection edits",twin);
            var delivered=Read(c,game,"source.sink",Tank,Second);
            c.Equal(Fix.FromInt(8),delivered.Stock,"old cargo physically reaches admitted original endpoint");
            c.Equal(Fix.Zero,delivered.InboundPromised,"native incoming receipt spent");
            c.Equal(0,delivered.Shipments.Count,"completed job absent from live facts");
            c.Equal(Fix.FromInt(4),pinned.Stock,"retained operational snapshot detached");
            c.Equal(Fix.FromInt(4),pinned.InboundPromised,"retained promise snapshot detached");
            Incoming(c,History(c,game,"source.sink",Tank,Second),Second,8,8);
            Incoming(c,History(c,game,"source.store",Other),Second,8,8);
            Incoming(c,History(c,game,"source.sink",Tank),First,12,12);
            Incoming(c,History(c,game,"source.sink",Alias),Third,0,0);
            c.Equal(History(c,game,"source.sink",Tank,Second).IncomingRaw,
                History(c,twin,"source.sink",Tank,Second).IncomingRaw,"restored journal has original actual throughput");
            c.Equal(Fix.FromInt(30),Read(c,game,"source.warehouse").Stock+
                Read(c,game,"source.store",A).Stock+Read(c,game,"source.store",Other).Stock+
                Read(c,game,"source.sink",Alias).Stock+Read(c,game,"haul.machine").Stock,"physical conservation through observations");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,4000)).Success,
                "same normal player commands replay without diagnostic state or host logic");
        }
    }
}
