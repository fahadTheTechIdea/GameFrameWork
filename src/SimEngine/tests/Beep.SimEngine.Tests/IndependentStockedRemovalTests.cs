using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class IndependentStockedRemovalTests
    {
        internal const string Case = "Independent stocked removal: physical cut evacuation and empty-segment dismantling";
        internal const string NativeCase = "Native stocked removal: physical cut evacuation and empty-segment dismantling";
        internal static void Register() { TestRunner.Add(Case,c=>Run(c,false)); TestRunner.Add(NativeCase,c=>Run(c,true)); }
        private static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest {
            Scenario="independent-sensor",Seed=723,Seats={Controller.Human,Controller.Human} });
        private static EntityId Id(SimGame g,string name) => g.Seat(0).Snapshot.Entities.Single(e=>e.Def==name).Id;
        private static List<NetworkPoolView> Pools(SimGame g)
        { var rows=new List<NetworkPoolView>(); if(!g.Seat(0).View.Feature<InfrastructureView>().CopyPools("sensor.network",rows).IsNone)throw new InvalidOperationException("missing actual pool facts");return rows; }
        private static Fix NetworkStock(SimGame g)
        {
            if(!g.Seat(0).View.TryFindResource("steel",out var steel))throw new InvalidOperationException("missing actual steel");
            return Pools(g).SelectMany(p=>p.Stock).Where(s=>s.Resource==steel).Aggregate(Fix.Zero,(sum,s)=>sum+s.Amount);
        }
        private static Fix Total(SimGame g)
        {
            Fix total=NetworkStock(g);var facts=new List<Facet>();
            foreach(var entity in g.Seat(0).Snapshot.Entities){g.Seat(0).View.Inspect(entity.Id,facts);foreach(var f in facts)if(f.Key==Storage.FacetStock&&f.Subject=="steel")total+=f.Value;}
            return total;
        }
        private static void Update(Check c,SimGame g,SimGame twin=null)
        { g.Update();twin?.Update();if(g.Fault!=null||twin?.Fault!=null)throw new InvalidOperationException("stocked removal fault "+g.Fault+"/"+twin?.Fault);
          if(twin!=null)c.Equal(g.Hash(),twin.Hash(),"saved actual pool/work/port/cargo state continues identically"); }
        private static void Until(Check c,SimGame g,SimGame twin,Func<bool> ready,string stage)
        {
            for(int i=0;i<240&&!ready();i++)Update(c,g,twin);
            if(!ready()){var jobs=new List<WorkJobView>();g.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);throw new InvalidOperationException(stage+" clock="+g.Clock.StepIndex+" pools="+Pools(g).Count+" stock="+NetworkStock(g)+" jobs="+string.Join(";",jobs.Select(j=>j.State+"/"+j.Blocker)));}
        }
        private static PostOrderCommand Cut(SimGame g,string site) => new PostOrderCommand {Plan="sensor.dismantle",Target=Id(g,site),Priority=100};
        private static int Post(Check c,SimGame g,string site)
        { var command=Cut(g,site);c.Equal(Code.None,g.Seat(0).Preview(command),"ordinary physical dismantling preview");var result=TestKit.Order(g,0,command);c.That(result.Accepted,"ordinary machine work command: "+result.Refusal);if(!result.Accepted)throw new InvalidOperationException("cut refused "+result.Refusal);return checked((int)result.Arg0); }
        private static bool Partial(SimGame g,int order)
        {var rows=new List<ActivityNodeView>();return g.Seat(0).View.Feature<WorkView>().TryCopyNodes(order,rows)&&rows.Any(n=>n.Progress.WholeTicks>0&&n.Progress.WholeTicks<n.RequiredTicks);}
        private static bool Done(SimGame g,string site) => g.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:sensor.dismantle",Id(g,site),out _);
        private static void Dismantle(Check c,GameFactory factory,SimGame game,ref SimGame twin,string site)
        {
            int order=Post(c,game,site);twin=factory.Load(game.Save());
            Until(c,game,twin,()=>Partial(game,order),"physical productive dismantling");
            twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"partially productive removal restores through original graph");
            game.Clock.Pause();twin.Clock.Pause();ulong hash=game.Hash();long step=game.Clock.StepIndex;
            Update(c,game,twin);c.Equal(step,game.Clock.StepIndex,"one paused clock freezes removal and transfer");c.Equal(hash,game.Hash(),"paused dismantling changes no material or geometry");game.Clock.Resume();twin.Clock.Resume();
            Until(c,game,twin,()=>Done(game,site),"earned actual line removal");
        }
        private static void Evacuate(Check c,GameFactory factory,SimGame game,ref SimGame twin,string connection)
        {
            game.Seat(0).View.TryFindResource("steel",out var steel);
            var command=new HaulResourcesCommand {Source=Id(game,connection),SourcePort=new Code("network-stock"),Destination=Id(game,"sensor.depot"),Worker=Id(game,"sensor.cart"),Resource=steel,Ceiling=Fix.FromInt(3),Priority=100};
            c.Equal(Code.None,game.Seat(0).Preview(command),"native connected pool supports ordinary physical evacuation");
            var result=TestKit.Order(game,0,command);c.That(result.Accepted,"connected material evacuation command: "+result.Refusal);if(!result.Accepted)throw new InvalidOperationException("evacuation refused "+result.Refusal);
            int job=checked((int)result.Arg0);twin=factory.Load(game.Save());var jobs=new List<WorkJobView>();
            bool Loaded(){game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);return jobs.Any(j=>j.Id==job&&j.Loaded.IsPositive);}
            Until(c,game,twin,Loaded,"loaded recovered material physically travels");
            twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"interrupted connected-pool cargo preserves original selected endpoint and receipts");
            bool Finished(){game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);return jobs.All(j=>j.Id!=job);}
            Until(c,game,twin,Finished,"actual recovered cargo unload and departure");
        }
        private static void Run(Check c,bool native)
        {
            var factory=IndependentTileWorkTests.Factory(nativeTiles:native,stockedRemoval:true);var game=New(factory);var idle=New(factory);ulong isolated=idle.Hash();
            c.That(!game.World.Services.Has<IEconomy>()&&(native?game.World.Space is TileMap:game.World.Space is not TileMap),"resource-only selected geometry, no business or native fallback");
            c.Equal(Fix.FromInt(18),Total(game),"only authored depot and feed material exist");
            var limits=new ReplayLimits(4*1024*1024,6000);using var recording=game.RecordReplay(limits);
            Until(c,game,null,()=>NetworkStock(game)==Fix.FromInt(6),"rate-limited source ports fill actual original network");
            c.That(Pools(game).Count==1&&Pools(game)[0].MemberCount==3,"authored initial pipe has one actual three-member pool");
            int cancelled=Post(c,game,"sensor.pipe-middle");Until(c,game,null,()=>Partial(game,cancelled),"interruptible partial cutting");
            c.That(TestKit.Order(game,0,new CancelOrderCommand {Order=cancelled}).Accepted,"ordinary cancellation stops unearned removal");
            var twin=factory.Load(game.Save());for(int i=0;i<3;i++)Update(c,game,twin);
            c.That(Pools(game).Count==1&&Pools(game)[0].MemberCount==3&&NetworkStock(game)==Fix.FromInt(6),"cancelled work retains actual stocked geometry");
            Dismantle(c,factory,game,ref twin,"sensor.pipe-middle");
            c.That(Pools(game).Count==2&&Pools(game).All(p=>p.MemberCount==1),"earned machine work splits the stocked pipe into original endpoint pools");
            c.Equal(Fix.FromInt(6),NetworkStock(game),"stocked physical split conserves every stored material");
            foreach(string site in new[]{"sensor.pipe-left","sensor.pipe-right"}) {
                ulong hash=game.Hash();c.Equal(NetworkService.OccupiedNetwork,game.Seat(0).Preview(Cut(game,site)),"stocked final segment requires physical evacuation before removal");c.Equal(hash,game.Hash(),"occupied refusal allocates no work or material prefix");
            }
            Evacuate(c,factory,game,ref twin,"sensor.line-start");
            c.Equal(Fix.FromInt(3),NetworkStock(game),"real transporter recovers exactly one split pool's three materials");
            Dismantle(c,factory,game,ref twin,"sensor.pipe-left");c.Equal(1,Pools(game).Count,"empty left endpoint pool retires only after earned work");
            Evacuate(c,factory,game,ref twin,"sensor.line-end");c.Equal(Fix.Zero,NetworkStock(game),"second physical delivery empties final original pool");
            Dismantle(c,factory,game,ref twin,"sensor.pipe-right");c.Equal(0,Pools(game).Count,"empty final pipe disappears after recovery and machine work");
            c.Equal(Fix.FromInt(18),Total(game),"full dismantling and recovery conserves all original material without invented salvage");
            var hidden=new List<NetworkPoolView>();game.Seat(1).View.Feature<InfrastructureView>().CopyPools("sensor.network",hidden);c.Equal(0,hidden.Count,"foreign company receives no retained private pools");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"finished recovery and dismantling saves through original owners");c.Equal(isolated,idle.Hash(),"interleaved session receives no cuts, recovered cargo or topology changes");
            var replay=new ReplayVerifier().Verify(factory,recording.Finish(),limits);c.That(replay.Success&&replay.Game?.Hash()==game.Hash(),"ordinary physical cut/haul/cancel/pause replay: "+replay.Failure+"/"+replay.Detail);
        }
    }
}
