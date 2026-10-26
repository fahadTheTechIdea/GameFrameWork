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
    // Player simulation only. Native engine contracts and JSON own all gameplay.
    internal static class ScopedSourceRouteTests
    {
        internal const string Case = "Scoped sources and routes: public alias discovery, original trips and same-site stores";
        private static readonly Code A=new Code("a-stock"), Z=new Code("z-stock"), Other=new Code("other"), Tank=new Code("tank"), Process=new Code("process");
        private const long First=9000000001, Second=9000000002, Third=9000000003;
        internal static void Register() => TestRunner.Add(Case,Run);
        private static EntityId Find(SimGame game,string prototype) => game.Seat(0).Snapshot.Entities.Single(e=>e.Def==prototype).Id;
        private static ResourceId Steel(SimGame game)
        { if(!game.Seat(0).View.TryFindResource("steel",out var resource)) throw new InvalidOperationException("missing steel"); return resource; }
        private static List<WorkJobView> Jobs(SimGame game)
        { var result=new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(result); return result; }
        private static bool Live(WorkJobView job) => job.State==JobState.Open || job.State==JobState.Assigned || job.State==JobState.Paused;
        private static List<RouteView> Routes(SimGame game,int seat=0)
        { var result=new List<RouteView>(); game.Seat(seat).View.Feature<LogisticsView>().CopyRoutes(result); return result; }
        private static List<LogisticsDispatchOffer> Offers(SimGame game)
        { var result=new List<LogisticsDispatchOffer>(); game.Seat(0).View.Feature<LogisticsView>().CopyQueue(result); return result; }
        private static TransformationSnapshot State(SimGame game)
        { if(!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Find(game,"source.factory"),Process,out var state)) throw new InvalidOperationException("missing process"); return state; }
        private static ResourcePortSnapshot Port(SimGame game,string prototype,Code name)
        { var refusal=game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(Find(game,prototype),name,Steel(game),out var result); if(!refusal.IsNone) throw new InvalidOperationException("port "+name+"/"+refusal); return result; }
        private static Fix Stock(SimGame game,string prototype)
        { var facts=new List<Facet>(); game.Seat(0).View.Inspect(Find(game,prototype),facts); return facts.Where(f=>f.Key==Storage.FacetStock && f.Subject=="steel").Aggregate(Fix.Zero,(sum,f)=>sum+f.Value); }
        private static void Step(SimGame game,SimGame twin=null)
        { game.Update(); twin?.Update(); if(game.Fault!=null || twin?.Fault!=null || twin!=null && twin.Hash()!=game.Hash()) throw new InvalidOperationException("route update "+game.Tick+"/"+game.Fault+"/"+twin?.Fault); }
        private static void Until(SimGame game,Func<bool> done,string stage,SimGame twin=null)
        {
            for(int i=0;i<1000 && !done();i++) Step(game,twin);
            if(!done()) throw new InvalidOperationException(stage+" at "+game.Tick+" process="+State(game).Phase+"/"+State(game).Blocker+" offers="+
                string.Join(";",Offers(game).Select(o=>o.Route+"/"+o.Haul.Status+"/"+o.Haul.Blocker))+" jobs="+string.Join(";",Jobs(game).Select(j=>j.Id+"/"+j.State+"/"+j.ToilIndex+"/"+j.Loaded+"/"+j.Blocker)));
        }
        private static long Command(Check c,SimGame game,Command command)
        {
            ulong hash=game.Hash(); c.Equal(Code.None,game.Seat(0).Preview(command),"native command preview"); c.Equal(hash,game.Hash(),"preview changes no state");
            var result=TestKit.Order(game,0,command); c.That(result.Accepted,"native command executes: "+result.Refusal);
            if(!result.Accepted || game.Fault!=null) throw new InvalidOperationException("command "+result.Refusal+"/"+game.Fault); return result.Arg0;
        }
        private static void Select(Check c,SimGame game,string site,Code port,long pool) => Command(c,game,new SelectResourcePortCommand {Entity=Find(game,site),Port=port,Endpoint = new ResourceEndpointId("proof.pools", pool)});
        private static void Queue(Check c,SimGame game) => Command(c,game,new EnqueueTransformationCommand {Transformer=Find(game,"source.factory"),Capability=Process,Transformation="source.batch"});
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {Scenario="scoped-sources",Seed=953,Seats={Controller.Human,Controller.Human}});
        private static SimGame Save(Check c,GameFactory factory,SimGame game)
        { var twin=factory.Load(game.Save()); c.Equal(game.Hash(),twin.Hash(),"route intent and original receipts checkpoint"); return twin; }
        private static void Fund(Check c,SimGame game,string destination,Code port,int amount)
        {
            int id=checked((int)Command(c,game,new HaulResourcesCommand {Source=Find(game,"source.warehouse"),Destination=Find(game,destination),DestinationPort=port,Worker=Find(game,"haul.machine"),Resource=Steel(game),Ceiling=Fix.FromInt(amount),Priority=100}));
            Until(game,()=>!Jobs(game).Any(j=>j.Id==id && Live(j)),"physical source funding");
        }
        private static AddRouteCommand Route(SimGame game,string source,Code sourcePort,string destination,Code destinationPort,int batch=3) => new AddRouteCommand {
            From=Find(game,source),To=Find(game,destination),SourcePort=sourcePort,DestinationPort=destinationPort,Resource=Steel(game),Settings=new LogisticsRouteSettings(true,Fix.FromInt(batch),40)};
        private static WorkJobView Assigned(SimGame game,int route) => Jobs(game).Single(j=>Live(j)&&j.Route==route);
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"named source/route protocol"); c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"original route codec migration");
            var factory=ScopedHaulGame.Factory(scopedSources:true); var game=New(factory); var steel=Steel(game); var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,4000));
            c.That(!game.Modules.Contains("sim.tycoon"),"source exposure and routing require no business");
            c.That(!game.Seat(0).View.Feature<InventoryView>().TryGetCapacity(Find(game,"source.store"), Code.None,out _),"named-only source has no local inventory fallback");
            c.Equal(Fix.FromInt(30),Stock(game,"source.warehouse"),"all material starts in authored private warehouse");
            Fund(c,game,"source.store",Z,8); Fund(c,game,"source.store",Z,4);
            c.Equal(Fix.FromInt(12),Port(game,"source.store",A).Stock.AmountOf(steel),"aliases read one actually funded pool");
            Command(c,game,new SetWorkerHeldCommand {Worker=Find(game,"haul.machine"),Held=true}); Queue(c,game);
            Until(game,()=>Offers(game).Count==1,"one demand offer despite source aliases");
            c.Equal(0L,State(game).Wip.Execution,"blocked carrier cannot start manufacture");
            Command(c,game,new SetWorkerHeldCommand {Worker=Find(game,"haul.machine"),Held=false});
            Until(game,()=>Jobs(game).Any(j=>Live(j)&&j.HaulDemand.IsValid),"automatic scoped source pickup");
            var supply=Jobs(game).Single(j=>Live(j)&&j.HaulDemand.IsValid); c.Equal(A,supply.SourcePort,"ordinal alias choice is independent of authored z-before-a order");
            c.Equal(new ResourceEndpointId("proof.pools",First),supply.SourceEndpoint,"same native job claims real scoped source");
            Select(c,game,"source.store",A,Second); var twin=Save(c,factory,game);
            Until(game,()=>State(game).QueueCount==0,"original automatic source supplies completed process",twin);
            c.Equal(Fix.FromInt(2),Stock(game,"source.factory"),"output comes from physical scoped input");
            c.Equal(Fix.FromInt(10),Port(game,"source.store",Z).Stock.AmountOf(steel),"exact original source lost two units"); c.Equal(Fix.Zero,Port(game,"source.store",A).Stock.AmountOf(steel),"new source selection was not substituted");
            var declaration=Route(game,"source.store",Z,"source.sink",Tank); ulong before=game.Hash();
            c.Equal(Code.None,game.Seat(0).View.Feature<LogisticsView>().PreviewRoute(declaration.From,declaration.To,steel,declaration.Settings,Z,Tank),"engine route management preview matches command"); c.Equal(before,game.Hash(),"route preview acquires nothing");
            c.Equal(Codes.NotFound,game.Seat(1).Preview(declaration),"foreign route refuses");
            c.Equal(Codes.NotFound,game.Seat(1).View.Feature<LogisticsView>().PreviewRoute(declaration.From,declaration.To,steel,declaration.Settings,Z,Tank),"foreign route preview reveals nothing");
            var invalid=Route(game,"source.store",Z,"source.store",Z); c.Equal(Codes.InvalidArgument,game.Seat(0).Preview(invalid),"actual endpoint self-route refuses");
            invalid=Route(game,"source.store",new Code("missing"),"source.sink",Tank); c.Equal(StandardCodes.NoStorage,game.Seat(0).Preview(invalid),"missing named source has no entity fallback");
            invalid=Route(game,"source.store",Z,"source.sink",Tank); invalid.Settings=new LogisticsRouteSettings(true,Fix.Half,40); c.Equal(Codes.InvalidArgument,game.Seat(0).Preview(invalid),"native discrete batch quantum refuses fractional route"); c.Equal(before,game.Hash(),"all refused intents leave IDs and stock unchanged");
            int route=checked((int)Command(c,game,declaration)); var retained=Routes(game).Single(r=>r.Id==route);
            c.Equal(Z,retained.SourcePort,"standing intent saves source name"); c.Equal(Tank,retained.DestinationPort,"standing intent saves destination name"); c.Equal(0,Routes(game,1).Count,"standing routes are company-private");
            Until(game,()=>Jobs(game).Any(j=>Live(j)&&j.Route==route),"standing native scoped route"); var trip=Assigned(game,route);
            twin=Save(c,factory,game); Until(game,()=>Jobs(game).Any(j=>j.Id==trip.Id && j.Loaded.IsPositive),"standing timed pickup",twin);
            Select(c,game,"source.store",Z,Second); Select(c,game,"source.sink",Tank,Second);
            c.Equal(new ResourceEndpointId("proof.pools",First),Jobs(game).Single(j=>j.Id==trip.Id).SourceEndpoint,"old pickup still pins original store");
            c.Equal(new ResourceEndpointId("proof.pools",Third),Jobs(game).Single(j=>j.Id==trip.Id).DestinationEndpoint,"old delivery still pins original store");
            c.Equal(HaulStatus.InvalidRequest,Offers(game).Single(o=>o.Route==route).Haul.Status,"future self-alias trips wait instead of silently copying stock");
            Command(c,game,new ConfigureRouteCommand {Route=route,Settings=new LogisticsRouteSettings(false,Fix.FromInt(3),40)});
            c.Equal(retained.CreatedTick,Routes(game).Single(r=>r.Id==route).CreatedTick,"policy edit retains original age"); c.That(retained.Settings.Enabled,"retained route fact stays detached");
            twin=Save(c,factory,game); Until(game,()=>!Jobs(game).Any(j=>j.Id==trip.Id && Live(j)),"disabled future intent preserves acquired original trip",twin);
            c.Equal(Fix.Zero,Port(game,"source.sink",Tank).Stock.AmountOf(steel),"new destination got no old cargo");
            Select(c,game,"source.sink",Tank,Third); Select(c,game,"source.store",Z,First);
            c.Equal(Fix.FromInt(3),Port(game,"source.sink",Tank).Stock.AmountOf(steel),"actual original destination received trip");
            Command(c,game,new ConfigureRouteCommand {Route=route,Settings=new LogisticsRouteSettings(true,Fix.FromInt(2),40)});
            Until(game,()=>Jobs(game).Any(j=>Live(j)&&j.Route==route),"next named standing trip"); trip=Assigned(game,route);
            Command(c,game,new RemoveRouteCommand {Route=route}); c.Equal(0,Routes(game).Count,"remove retires future intent only");
            twin=Save(c,factory,game); Until(game,()=>!Jobs(game).Any(j=>j.Id==trip.Id && Live(j)),"removed-route acquired delivery",twin);
            c.Equal(Fix.FromInt(5),Port(game,"source.sink",Tank).Stock.AmountOf(steel),"second acquired trip survives route removal");
            int local=checked((int)Command(c,game,Route(game,"source.store",Z,"source.store",Other,2)));
            Until(game,()=>Jobs(game).Any(j=>Live(j)&&j.Route==local),"same-site distinct-store haul"); trip=Assigned(game,local);
            c.That(trip.Source==trip.Destination && trip.SourceEndpoint!=trip.DestinationEndpoint,"same physical site is not same stock endpoint");
            Command(c,game,new RemoveRouteCommand {Route=local}); twin=Save(c,factory,game);
            Until(game,()=>!Jobs(game).Any(j=>j.Id==trip.Id && Live(j)),"same-site timed transfer and departure",twin);
            c.Equal(Fix.FromInt(3),Port(game,"source.store",Z).Stock.AmountOf(steel),"same-site pickup loses actual stock"); c.Equal(Fix.FromInt(2),Port(game,"source.store",Other).Stock.AmountOf(steel),"same-site unload delivers actual stock");
            c.Equal(Fix.FromInt(30),Stock(game,"source.warehouse")+Stock(game,"source.factory")+Stock(game,"haul.machine")+Port(game,"source.store",Z).Stock.AmountOf(steel)+Port(game,"source.store",Other).Stock.AmountOf(steel)+Port(game,"source.sink",Tank).Stock.AmountOf(steel),"all routes and manufacture conserve native material");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,4000)).Success,"ordinary manual/automatic/standing/same-site command replay");
            PrivateSource(c,factory);
        }
        private static void PrivateSource(Check c,GameFactory factory)
        {
            var game=New(factory); var steel=Steel(game); Fund(c,game,"source.sink",Tank,4); Queue(c,game);
            Until(game,()=>Offers(game).Count==1,"private-only supply diagnostic"); for(int i=0;i<20;i++) Step(game);
            c.Equal(HaulStatus.NoSupply,Offers(game).Single().Haul.Status,"private store and private warehouse are not auto-discovered"); c.Equal(0L,State(game).Wip.Execution,"private stock never bypasses supply policy");
            c.Equal(Fix.FromInt(4),Port(game,"source.sink",Tank).Stock.AmountOf(steel),"private scoped stock is real and untouched");
            int route=checked((int)Command(c,game,Route(game,"source.sink",Tank,"source.factory",new Code("local"),2)));
            Until(game,()=>Jobs(game).Any(j=>Live(j)&&j.Route==route),"explicit player route may use owned private stock"); var trip=Assigned(game,route);
            Command(c,game,new RemoveRouteCommand {Route=route}); var twin=Save(c,factory,game);
            Until(game,()=>State(game).QueueCount==0,"explicit private-source physical manufacture",twin);
            Until(game,()=>!Jobs(game).Any(j=>j.Id==trip.Id && Live(j)),"private route departure",twin);
            c.Equal(Fix.FromInt(2),Stock(game,"source.factory"),"only physically funded private route makes output");
            c.Equal(Fix.FromInt(30),Stock(game,"source.warehouse")+Stock(game,"source.factory")+Stock(game,"haul.machine")+Port(game,"source.sink",Tank).Stock.AmountOf(steel),"private route conserves all resources");
        }
    }
}
