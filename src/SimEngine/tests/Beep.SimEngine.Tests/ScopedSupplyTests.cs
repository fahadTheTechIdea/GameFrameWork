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
    // Player driver only: native commands, detached views, one clock and Update.
    internal static class ScopedSupplyTests
    {
        internal const string Case = "Scoped automatic supply: independent named demands, physical production and saved replacement";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Process = new Code("process"), Feed = new Code("feed"), Tools = new Code("tools"), Power = new Code("power");
        private const long First = 5000000001, Third = 5000000003;
        private static EntityId Find(SimGame game, string prototype) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == prototype).Id;
        private static ResourceId Steel(SimGame game)
        { if (!game.Seat(0).View.TryFindResource("steel", out var resource)) throw new InvalidOperationException("missing steel"); return resource; }
        private static List<LogisticsDemandView> Demands(SimGame game, int seat = 0)
        { var result = new List<LogisticsDemandView>(); game.Seat(seat).View.Feature<LogisticsView>().CopyDemands(result); return result; }
        private static List<WorkJobView> Jobs(SimGame game)
        { var result = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(result); return result; }
        private static bool Live(WorkJobView job) => job.State == JobState.Assigned || job.State == JobState.Open || job.State == JobState.Paused;
        private static TransformationSnapshot State(SimGame game)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Find(game,"supply.factory"),Process,out var state))
                throw new InvalidOperationException("missing process"); return state;
        }
        private static ResourcePortSnapshot Port(SimGame game, Code port)
        {
            var refusal=game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(Find(game,"supply.factory"),port,Steel(game),out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("port "+port+"/"+refusal); return facts;
        }
        private static Fix Stock(SimGame game, string prototype)
        {
            var facts = new List<Facet>(); game.Seat(0).View.Inspect(Find(game,prototype),facts);
            return facts.Where(f=>f.Key==Storage.FacetStock && f.Subject=="steel").Aggregate(Fix.Zero,(sum,f)=>sum+f.Value);
        }
        private static void Step(SimGame game, SimGame twin=null)
        {
            game.Update(); twin?.Update();
            if (game.Fault!=null || twin?.Fault!=null || twin!=null && twin.Hash()!=game.Hash())
                throw new InvalidOperationException("supply update "+game.Tick+"/"+game.Fault+"/"+twin?.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin=null)
        {
            for(int i=0;i<1200 && !done();i++) Step(game,twin);
            if(!done()) throw new InvalidOperationException(stage+" timed out at "+game.Tick+" process="+State(game).Phase+"/"+State(game).Blocker+
                " demands="+string.Join(";",Demands(game).Select(d=>d.Port+"/"+d.Endpoint+"/"+d.Quantity))+
                " jobs="+string.Join(";",Jobs(game).Select(j=>j.Id+"/"+j.State+"/"+j.ToilIndex+"/"+j.Loaded+"/"+j.Blocker)));
        }
        private static void Command(Check c, SimGame game, Command command)
        {
            ulong hash=game.Hash(); c.Equal(Code.None,game.Seat(0).Preview(command),"native preview accepts"); c.Equal(hash,game.Hash(),"preview is pure");
            var result=TestKit.Order(game,0,command); c.That(result.Accepted,"native command executes: "+result.Refusal);
            if (!result.Accepted || game.Fault!=null) throw new InvalidOperationException("command "+result.Refusal+"/"+game.Fault);
        }
        private static void Queue(Check c,SimGame game,string recipe) => Command(c,game,new EnqueueTransformationCommand {Transformer=Find(game,"supply.factory"),Capability=Process,Transformation=recipe});
        private static SimGame Save(Check c,GameFactory factory,SimGame game)
        { var twin=factory.Load(game.Save()); c.Equal(game.Hash(),twin.Hash(),"exact demand and original physical receipt checkpoint"); return twin; }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {Scenario="scoped-supply",Seed=947,Seats={Controller.Human,Controller.Human}});
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"named supply protocol"); c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"explicit endpoint demand codec");
            var factory=ScopedHaulGame.Factory(automaticSupply:true); var game=New(factory); var steel=Steel(game);
            var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,4000));
            c.That(!game.Modules.Contains("sim.tycoon"),"manufacturing supply does not require business");
            c.Equal(Fix.FromInt(30),Stock(game,"supply.warehouse"),"opening stock is authored JSON only");
            Command(c,game,new SetWorkerHeldCommand {Worker=Find(game,"haul.machine"),Held=true}); Queue(c,game,"supply.batch");
            Until(game,()=>Demands(game).Count==3,"three actual scoped demands");
            var retained=Demands(game); c.Equal(3,retained.Select(d=>d.Endpoint).Distinct().Count(),"same resource remains independent across actual stores");
            c.Equal(Fix.FromInt(8),retained.Single(d=>d.Port==Feed).Quantity,"authored feed requirement");
            c.Equal(Fix.One,retained.Single(d=>d.Port==Tools).Quantity,"authored reusable tool requirement"); c.Equal(Fix.One,retained.Single(d=>d.Port==Power).Quantity,"authored first operating quantum");
            c.That(retained.All(d=>d.Endpoint.Scope=="proof.pools" && d.Resource==steel),"no local-inventory substitution");
            c.Equal(0L,State(game).Wip.Execution,"no manufacturing prefix before whole funding");
            c.Equal(0,Demands(game,1).Count,"scoped request facts are private");
            var offers=new List<LogisticsDispatchOffer>(); ulong before=game.Hash(); game.Seat(0).View.Feature<LogisticsView>().CopyQueue(offers);
            c.Equal(3,offers.Count,"single matcher exposes three stores, not one entity/resource group"); c.Equal(before,game.Hash(),"offer preview creates no claims or stock");
            c.That(offers.All(o=>o.DestinationEndpoint.IsValid && !o.DestinationPort.IsNone),"even refused offers retain exact destinations");
            Save(c,factory,game);
            Command(c,game,new SetWorkerHeldCommand {Worker=Find(game,"haul.machine"),Held=false});
            Until(game,()=>Jobs(game).Any(j=>Live(j)&&j.DestinationPort==Feed),"automatic native feed haul");
            var feed=Jobs(game).Single(j=>Live(j)&&j.DestinationPort==Feed); c.Equal(new ResourceEndpointId("proof.pools",First),feed.DestinationEndpoint,"real haul receipt pins original feed store");
            var twin=Save(c,factory,game); Until(game,()=>Jobs(game).Any(j=>j.Id==feed.Id && j.Loaded.IsPositive),"timed native feed loading",twin);
            c.Equal(Fix.Zero,Port(game,Feed).Stock.AmountOf(steel),"loaded cargo has not arrived"); c.Equal(Fix.Zero,Stock(game,"supply.factory"),"no instant local fallback");
            c.That(State(game).Progress.Equals(WorkProgress.Zero),"delivery waits earn no work");
            Command(c,game,new SelectResourcePortCommand {Entity=Find(game,"supply.factory"),Port=Feed,Endpoint = new ResourceEndpointId("proof.pools", Third)});
            c.Equal(new ResourceEndpointId("proof.pools",First),Jobs(game).Single(j=>j.Id==feed.Id).DestinationEndpoint,"replacement cannot redirect in-flight cargo");
            Until(game,()=>Demands(game).All(d=>d.Endpoint.Identity!=First),"whole replacement retires old intent");
            c.Equal(Fix.FromInt(8),retained.Single(d=>d.Port==Feed).Quantity,"retained detached demand does not mutate");
            c.Equal(2,Demands(game).Count,"feed and operating aliases share one actual endpoint vector");
            c.Equal(retained.Single(d=>d.Port==Tools).CreatedTick,Demands(game).Single(d=>d.Port==Tools).CreatedTick,"unchanged actual endpoint retains original starvation age");
            c.Equal(Fix.FromInt(9),Demands(game).Single(d=>d.Endpoint.Identity==Third).Quantity,"whole physical vector sums same-store requirements once");
            twin=Save(c,factory,game); Until(game,()=>State(game).Phase==TransformationPhase.Working,"actual funded manufacturing",twin);
            c.That(State(game).Wip.Execution>0 && State(game).Tools.Held.AmountOf(steel)==Fix.One,"native WIP and original reusable catalyst acquired");
            twin=Save(c,factory,game); Until(game,()=>State(game).QueueCount==0,"batch output and physical tool release",twin);
            c.Equal(Fix.FromInt(8),Stock(game,"supply.factory"),"only native completed output reaches local store");
            c.Equal(Fix.One,Port(game,Tools).Stock.AmountOf(steel),"reusable tool is released, not consumed");
            c.Equal(0,Demands(game).Count,"completed process retires only its supply source");
            Command(c,game,new SelectResourcePortCommand {Entity=Find(game,"supply.factory"),Port=Feed,Endpoint = new ResourceEndpointId("proof.pools", First)});
            c.Equal(Fix.FromInt(8),Port(game,Feed).Stock.AmountOf(steel),"old acquired cargo still arrived at its original store");
            c.Equal(Fix.FromInt(29),Stock(game,"supply.warehouse")+Stock(game,"haul.machine")+Stock(game,"supply.factory")+
                Port(game,Feed).Stock.AmountOf(steel)+Port(game,Tools).Stock.AmountOf(steel)+Port(game,Power).Stock.AmountOf(steel),"batch accounting loses exactly the operating consumable");
            var replay=new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,4000)); c.That(replay.Success,"native demand/selection/receipt replay");
            Flow(c,factory);
        }
        private static void Flow(Check c,GameFactory factory)
        {
            var game=New(factory); var steel=Steel(game); Queue(c,game,"supply.flow");
            Until(game,()=>State(game).Phase==TransformationPhase.Working,"continuous physical startup");
            var twin=Save(c,factory,game);
            Until(game,()=>State(game).Flow.ToolConsumed.AmountOf(steel)>=Fix.FromInt(3) && State(game).Operating.Consumed.AmountOf(steel)>=Fix.FromInt(2)
                && State(game).Flow.Produced.AmountOf(steel)>=Fix.FromInt(2),"continuous feed, catalyst and operating replenishment",twin);
            var state=State(game); c.That(state.Tools.Held.AmountOf(steel)==Fix.One,"replenishment preserves original catalyst charge");
            var history=game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0);
            c.That(history.Entries.Any(e=>e.Item.IsTransfer && e.Item.Destination.Endpoint==new ResourceEndpointId("proof.pools",5000000002)) &&
                history.Entries.Any(e=>e.Item.IsTransfer && e.Item.Destination.Endpoint==new ResourceEndpointId("proof.pools",Third)),
                "ordinary truck journal proves physical delivery to independent tools and operating stores");
            c.Equal(state.Flow.Produced.AmountOf(steel),Stock(game,"supply.factory"),"elapsed native output, no driver production");
            Fix total=Stock(game,"supply.warehouse")+Stock(game,"haul.machine")+Stock(game,"supply.factory")+
                Port(game,Feed).Stock.AmountOf(steel)+Port(game,Tools).Stock.AmountOf(steel)+Port(game,Power).Stock.AmountOf(steel);
            c.Equal(Fix.FromInt(30)-state.Flow.Consumed.AmountOf(steel)-state.Flow.ToolConsumed.AmountOf(steel)-state.Operating.Consumed.AmountOf(steel)+state.Flow.Produced.AmountOf(steel),total,"all physical resources reconcile with native elapsed consumption");
            Command(c,game,new CancelTransformationCommand {Transformer=Find(game,"supply.factory"),Capability=Process,Index=0});
            c.Equal(0,State(game).QueueCount,"ordinary stop ends continuous execution"); c.Equal(0,Demands(game).Count,"stop retires demand without deleting stock");
            Save(c,factory,game);
        }
    }
}
