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
    // Thin player: public commands, detached facts and engine Update only.
    internal static class ScopedHaulTests
    {
        internal const string Case = "Scoped physical hauling: named original stores, timed handling, saved travel and interruption";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Tank = new Code("tank"), StockPort = new Code("stock");
        private const long First = 5000000001, Second = 5000000002;
        private static EntityId Find(SimGame game, string prototype) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == prototype).Id;
        private static ResourceId Steel(SimGame game)
        { if (!game.Seat(0).View.TryFindResource("steel", out var resource)) throw new InvalidOperationException("missing resource"); return resource; }
        private static List<WorkJobView> Jobs(SimGame game)
        { var facts = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(facts); return facts; }
        private static WorkJobView Job(SimGame game, int id) => Jobs(game).Single(j => j.Id == id);
        private static bool Live(WorkJobView job) => job.State == JobState.Assigned || job.State == JobState.Paused || job.State == JobState.Open;
        private static Fix Stock(SimGame game, string entity)
        {
            var facts = new List<Facet>(); game.Seat(0).View.Inspect(Find(game,entity), facts);
            return facts.Where(f=>f.Key==Storage.FacetStock && f.Subject=="steel").Aggregate(Fix.Zero,(sum,f)=>sum+f.Value);
        }
        private static ResourcePortSnapshot Port(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(Find(game,"haul.destination"), Tank, Steel(game), out var facts);
            if (!code.IsNone) throw new InvalidOperationException("port refusal " + code); return facts;
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && twin.Hash()!=game.Hash())
                throw new InvalidOperationException("haul update " + game.Tick + "/" + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i=0;i<600 && !done();i++) Step(game,twin);
            if (!done()) throw new InvalidOperationException(stage+" time="+game.Tick+" jobs="+string.Join(";",Jobs(game).Select(j=>j.Id+"/"+j.State+"/"+j.ToilIndex+"/"+j.Loaded+"/"+j.Blocker)));
        }
        private static long Command(Check c, SimGame game, Command command)
        {
            ulong hash=game.Hash(); c.Equal(Code.None,game.Seat(0).Preview(command),"native command admission");
            c.Equal(hash,game.Hash(),"native preview changes no state");
            var result=TestKit.Order(game,0,command); c.That(result.Accepted,"native command executes: "+result.Refusal);
            if (!result.Accepted || game.Fault!=null) throw new InvalidOperationException("haul command "+result.Refusal+"/"+game.Fault);
            return result.Arg0;
        }
        private static HaulResourcesCommand Haul(SimGame game, bool exporting = false) => new HaulResourcesCommand {
            Source=Find(game,exporting?"haul.destination":"haul.source"), Destination=Find(game,exporting?"haul.source":"haul.destination"),
            Worker=Find(game,"haul.machine"), SourcePort=exporting?Tank:StockPort, DestinationPort=exporting?StockPort:Tank,
            Resource=Steel(game), Ceiling=Fix.FromInt(8), Priority=100 };
        private static void Select(Check c, SimGame game, long pool) => Command(c,game,new SelectResourcePortCommand {Entity=Find(game,"haul.destination"),Port=Tank,Endpoint = new ResourceEndpointId("proof.pools", pool)});
        private static SimGame Saved(Check c, GameFactory factory, SimGame game)
        { var twin=factory.Load(game.Save()); c.Equal(game.Hash(),twin.Hash(),"original port receipt checkpoint"); return twin; }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"native physical port protocol"); c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"original job codec changed without old reader");
            var factory=ScopedHaulGame.Factory(); var game=factory.NewGame(new NewGameRequest {Scenario="scoped-haul",Seed=937,Seats={Controller.Human,Controller.Human}});
            var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,1600));
            c.Equal(Fix.FromInt(30),Stock(game,"haul.source"),"only JSON opening stock");
            c.Equal(new ResourceEndpointId("proof.pools",First),Port(game).Endpoint,"actual 64-bit service identity");
            c.Equal(Fix.Zero,Port(game).Stock.AmountOf(Steel(game)),"scoped endpoint starts empty");
            ulong before=game.Hash(); var preview=game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(Find(game,"haul.source"),Find(game,"haul.destination"),Steel(game),Fix.FromInt(8),sourcePort:StockPort,destinationPort:Tank);
            c.Equal(HaulStatus.Ready,preview.Status,"actual scoped delivery preview"); c.Equal(new ResourceEndpointId("proof.pools",First),preview.DestinationEndpoint,"preview reports actual store, not local entity");
            c.Equal(Fix.FromInt(8),preview.Amount,"native cargo capacity limits amount"); c.Equal(before,game.Hash(),"port facts and haul selection are pure");
            c.Equal(Codes.NotFound,game.Seat(1).Preview(Haul(game)),"foreign player cannot order private haul");
            c.Equal(Codes.NotOwned,game.Seat(1).View.Feature<InventoryView>().ReadResourcePort(Find(game,"haul.destination"),Tank,Steel(game),out _),"scoped quantities remain company-private");
            var bad=Haul(game); bad.DestinationPort=new Code("missing"); c.That(!game.Seat(0).Preview(bad).IsNone,"specified absent port refuses without local fallback"); c.Equal(before,game.Hash(),"private/missing refusal changes no stock or ID");
            int id=checked((int)Command(c,game,Haul(game)));
            c.Equal(Tank,Job(game,id).DestinationPort,"actual Job retains named destination"); c.Equal(new ResourceEndpointId("proof.pools",First),Job(game,id).DestinationEndpoint,"actual receipt pins original pool");
            c.Equal(Fix.Zero,Port(game).Stock.AmountOf(Steel(game)),"admission never delivers cargo"); c.Equal(Fix.FromInt(30),Stock(game,"haul.source"),"travel never takes stock before pickup");
            var twin=Saved(c,factory,game); Until(game,()=>Job(game,id).Loaded.IsPositive,"first physical loading",twin);
            c.That(Job(game,id).Loaded<Fix.FromInt(8),"authored loading takes elapsed game time");
            c.Equal(Fix.FromInt(30),Stock(game,"haul.source")+Stock(game,"haul.machine"),"partial pickup conserves native resources");
            twin=Saved(c,factory,game); Until(game,()=>Job(game,id).ToilIndex==3 && Job(game,id).Loaded==Fix.FromInt(8),"loaded physical arrival",twin);
            c.Equal(Fix.Zero,Port(game).Stock.AmountOf(Steel(game)),"loaded travel does not silently unload");
            Select(c,game,Second); c.Equal(new ResourceEndpointId("proof.pools",Second),Port(game).Endpoint,"current selection can change independently");
            c.Equal(new ResourceEndpointId("proof.pools",First),Job(game,id).DestinationEndpoint,"in-flight delivery keeps original receipt");
            twin=Saved(c,factory,game); Until(game,()=>!Jobs(game).Any(j=>j.Id==id && Live(j)),"original destination delivery and departure",twin);
            c.Equal(Fix.Zero,Port(game).Stock.AmountOf(Steel(game)),"new selected pool receives none of old cargo"); c.Equal(Fix.Zero,Stock(game,"haul.destination"),"local inventory is never a scoped fallback");
            Select(c,game,First); c.Equal(Fix.FromInt(8),Port(game).Stock.AmountOf(Steel(game)),"all real cargo reached original service stock");
            c.Equal(Fix.FromInt(22),Stock(game,"haul.source"),"source loses only actual delivery"); c.Equal(Fix.Zero,Stock(game,"haul.machine"),"cargo empties only through physical unloading");
            int export=checked((int)Command(c,game,Haul(game,true))); c.Equal(new ResourceEndpointId("proof.pools",First),Job(game,export).SourceEndpoint,"export claims actual scoped stock");
            Select(c,game,Second); twin=Saved(c,factory,game); Until(game,()=>Job(game,export).Loaded.IsPositive,"original scoped pickup despite selection change",twin);
            c.That(Job(game,export).Loaded<Fix.FromInt(8),"scoped loading is timed too");
            twin=Saved(c,factory,game); Until(game,()=>!Jobs(game).Any(j=>j.Id==export && Live(j)),"scoped export physical completion",twin);
            c.Equal(Fix.FromInt(30),Stock(game,"haul.source"),"scoped export returns exact stock to real entity"); Select(c,game,First); c.Equal(Fix.Zero,Port(game).Stock.AmountOf(Steel(game)),"original scoped store was emptied, not copied");
            int interrupted=checked((int)Command(c,game,Haul(game))); Until(game,()=>Job(game,interrupted).Loaded.IsPositive,"interrupted partial pickup");
            Fix carried=Stock(game,"haul.machine"); Select(c,game,Second);
            Command(c,game,new SetWorkerHeldCommand {Worker=Find(game,"haul.machine"),Held=true});
            c.That(!Jobs(game).Any(j=>j.Id==interrupted && Live(j)),"ordinary worker interruption retires original haul"); c.That(Stock(game,"haul.machine")>=carried,"interruption retains already transferred cargo");
            c.That(game.Seat(0).View.Feature<InventoryView>().TryGetReservations(Find(game,"haul.machine"), Code.None,out var cargoClaims) && cargoClaims.Count==0,"interrupted cargo promises released");
            Select(c,game,First); c.Equal(Fix.Zero,Port(game).Stock.AmountOf(Steel(game)),"cancelled destination was not manufactured");
            c.Equal(Fix.FromInt(30),Stock(game,"haul.source")+Stock(game,"haul.machine"),"interrupted workflow conserves all stock");
            Saved(c,factory,game); c.Equal(HaulStatus.NoCarrier,game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(Find(game,"haul.source"),Find(game,"haul.destination"),Steel(game),Fix.One,sourcePort:StockPort,destinationPort:Tank).Status,"held carrier cannot be promised");
            c.Equal(Fix.Zero,game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(Find(game,"haul.destination"),Find(game,"haul.source"),Steel(game),Fix.One,sourcePort:Tank,destinationPort:StockPort).Amount,"spent scoped stock creates no export");
            var facts=game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0);
            c.That(facts.Entries.Any(f=>f.Item.IsTransfer && f.Item.Source.Endpoint.Scope=="proof.pools") && facts.Entries.Any(f=>f.Item.IsTransfer && f.Item.Destination.Endpoint.Scope=="proof.pools"),"same historical transfer journal records real scoped legs");
            c.That(facts.Entries.Where(f=>f.Item.IsTransfer).All(f=>f.Item.Kind==ResourceFlowKind.StockTransfer),"resource-only hauling never purchases or creates resources");
            var replay=new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,1600)); c.That(replay.Success,"native command replay retains selected and original stores");
        }
    }
}
