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
    // Master-listed authored geometry and public player inputs only; no direct dock/route/claim/position mutation.
    internal static partial class DockTravelSelectionTests
    {
        internal const string Case="Dock travel selection: actual prospective cost precedes stable identity without displacing physical claims";
        internal const string PruningCase="Dock departure selection: dominated routes lose only after a feasible guarded incumbent";
        internal const string ServicePruningCase="Dock candidate selection: full eligibility precedes pruning losing service journeys";
        internal static void Register(){TestRunner.Add(Case,Run);TestRunner.Add(PruningCase,Pruning);TestRunner.Add(ServicePruningCase,ServicePruning);TestRunner.Add(RankedDepartureCase,RankedDepartures);TestRunner.Add(RankedServiceCase,RankedServices);}
        private static GameFactory Factory(bool half,string mode,bool shuffled=false,string fault=null,string routeCase=null,string serviceCase=null)
        {
            var root=Json.Parse(PointGame.Definitions);root.Get("scenarios")[0].Set("seats",JsonValue.Number("2"));
            var entities=JsonValue.NewArray();entities.Add(root.Get("entities")[0]);entities.Add(root.Get("entities")[2]);root.Set("entities",entities);
            if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));
            var data=Json.Parse(FrameworkProofContent.Read("dock-cost-points.json"));var dock=data.Get("entities")[0].Get("components").Get("point_dock");var slots=dock.Get("slots");
            if(mode=="prospective"||mode=="tie") {
                root.Get("entity_setups")[0].Get("placement").Set("position",JsonValue.Number("10"));
                slots[0].Set("offset",JsonValue.Number("2"));slots[1].Set("offset",JsonValue.Number("-2"));
            }
            if(mode=="tie")root.Get("entity_setups")[2].Get("placement").Set("position",JsonValue.Number("10"));
            if(routeCase!=null) {
                slots[3].Set("id",JsonValue.Of("z.losing-exit"));slots[4].Set("id",JsonValue.Of("a.best-exit"));
                if(routeCase=="tie")slots[3].Set("offset",JsonValue.Number("-1"));
                root.Get("entities")[1].Get("components").Get("point_motion").Set("area_fault",JsonValue.Of("departure-"+routeCase));
                if(routeCase.StartsWith("ranked-",StringComparison.Ordinal)) {
                    slots[3].Set("id",JsonValue.Of("a.expensive-exit"));slots[4].Set("id",JsonValue.Of("z.cheap-exit"));
                    string rankedMode=routeCase.Substring("ranked-".Length);
                    root.Get("entities")[1].Get("components").Get("point_motion").Set("area_fault",JsonValue.Of("departure-"+rankedMode));
                    if(rankedMode=="cost")dock.Set("fault",JsonValue.Of("losing-cost-touch"));
                }
            }
            if(serviceCase!=null) {
                bool ranked=serviceCase.StartsWith("ranked-",StringComparison.Ordinal);
                if(ranked)serviceCase=serviceCase.Substring("ranked-".Length);
                slots[0].Set("id",JsonValue.Of("z.losing-bay"));slots[1].Set("id",JsonValue.Of("a.best-bay"));
                if(ranked){slots[0].Set("id",JsonValue.Of("a.expensive-bay"));slots[1].Set("id",JsonValue.Of("z.cheap-bay"));}
                if(serviceCase=="tie")slots[0].Set("offset",JsonValue.Number("-1"));
                if(serviceCase=="cost")dock.Set("fault",JsonValue.Of("losing-cost-touch"));
                root.Get("entities")[1].Get("components").Get("point_motion").Set("area_fault",JsonValue.Of("service-"+serviceCase));
            }
            if(shuffled){var reverse=JsonValue.NewArray();for(int i=slots.Count-1;i>=0;i--)reverse.Add(slots[i]);dock.Set("slots",reverse);}
            if(fault!=null)dock.Set("fault",JsonValue.Of(fault));
            return PointGame.Factory(definitions:root.ToString(),additionalDocuments:new Dictionary<string,string>{{"dock-cost-points.json",data.ToString()}});
        }
        private static SimGame New(GameFactory f)=>f.NewGame(new NewGameRequest{Scenario="points",Seed=2291,Seats={Controller.Human,Controller.Human}});
        private static HaulResourcesCommand Command(SimGame g)
        {g.Seat(0).View.TryFindResource("supply",out var resource);return new HaulResourcesCommand{Source=Entity(g,"source"),Destination=Entity(g,"destination"),Worker=Entity(g,"machine"),Resource=resource,Ceiling=Fix.FromInt(4),Priority=100};}
        private static WorkJobView Job(SimGame g,int id)=>PermanentRemovalTests.Jobs(g).Single(x=>x.Id==id);
        private static Fix At(SimGame g)=>((PointSnapshot)g.Seat(0).Snapshot.Entities.Single(e=>e.Id==Entity(g,"machine")).Spatial).Position;
        private static bool Live(WorkJobView j)=>j.State==JobState.Open||j.State==JobState.Assigned||j.State==JobState.Paused;
        private static long Physical(Check c,bool half,string mode,bool shuffled)
        {
            var f=Factory(half,mode,shuffled);var g=New(f);var idle=New(f);ulong isolated=idle.Hash();var limits=new ReplayLimits(2*1024*1024,1000);
            using(var recorder=g.RecordReplay(limits)) {
                var cmd=Command(g);ulong pure=g.Hash();c.Equal(Code.None,g.Seat(0).Preview(cmd),"complete slot/journey preview ready");c.Equal(pure,g.Hash(),"cost enumeration allocates no stock/slot/route or clock");
                int id=(int)PermanentRemovalTests.Submit(c,g,cmd).Arg0;var selected=Job(g,id);Code expected=new Code(mode=="near"?"z.near-bay":"a.far-bay");
                c.Equal(expected,selected.DestinationDockSlot,mode=="near"?"closest eligible service beats alphabetically first far bay and closer waiting place":"equal actual source-origin costs use stable slot identity");
                c.Equal(Fix.Zero,Stock(g,"destination"),"selected slot never instantly delivers resources");
                var privateJobs=new List<WorkJobView>();g.Seat(1).View.Feature<WorkView>().CopyJobs(privateJobs);c.Equal(0,privateJobs.Count,"private physical slot commitments withheld");
                var twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"chosen actual slot and stock receipts durable");Step(g,twin);Step(g,twin);
                c.Equal(expected,Job(g,id).DestinationDockSlot,"valid original commitment stays retained through movement and saved continuation");
                bool sawExit=false;int updates=0;
                for(;updates<160&&PermanentRemovalTests.Jobs(g).Any(j=>j.Id==id&&Live(j));updates++) {
                    Step(g,twin);var row=PermanentRemovalTests.Jobs(g).FirstOrDefault(j=>j.Id==id);
                    if(row.Id==id&&row.DestinationDockSlot==new Code("z.near-exit"))sawExit=true;
                }
                c.That(!PermanentRemovalTests.Jobs(g).Any(j=>j.Id==id&&Live(j)),"physical cheapest-slot haul finishes within160Updates");
                c.That(updates>1&&sawExit,"unloading and minimum-cost actual departure occur over game time");
                c.Equal(Fix.FromInt(13),At(g),"actual nearest exit reached, not alphabetically first far exit");
                c.Equal(Fix.FromInt(4),Stock(g,"destination"),"four real supplies delivered");c.Equal(Fix.FromInt(16),Stock(g,"source"),"four supplies actually left source");
                c.Equal(Fix.Zero,Stock(g,"machine"),"carrier has no stranded or duplicated cargo");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"settled chosen geometry/claims restore");c.Equal(isolated,idle.Hash(),"interleaved session unchanged");
                c.That(new ReplayVerifier().Verify(f,recorder.Finish(),limits).Success,"same public inputs replay contract-priced slot choice and physical motion");return g.Clock.StepIndex;
            }
        }
        private static void Negative(Check c,string mode)
        {
            var f=Factory(false,"near",fault:mode);var g=New(f);c.Throws<InvalidOperationException>(()=>g.Seat(0).Preview(Command(g)),"illegal actual cost/reach callback faults before slot acquisition: "+mode);
            c.That(g.Fault!=null,"original first fault retained: "+mode);c.Throws<InvalidOperationException>(()=>g.Save(),"faulted choice cannot save healthy: "+mode);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual current dock selection identity");
            foreach(bool half in new[]{false,true})foreach(string mode in new[]{"near","prospective","tie"})
                c.Equal(Physical(c,half,mode,false),Physical(c,half,mode,true),"shuffled authored slots keep the same physical chronology");
            Negative(c,"negative-cost");Negative(c,"cost-touch");
        }
        private static void Pruning(Check c)
        {
            foreach(string mode in new[]{"far","tie","blocked"})foreach(bool shuffled in new[]{false,true}) {
                var factory=Factory(false,"near",shuffled,routeCase:mode);var game=New(factory);
                ulong before=game.Hash();var twin=factory.Load(game.Save());
                foreach(var session in new[]{game,twin}) {
                    var world=session.World;var destination=world.Entities.Get(Entity(session,"destination"));
                    var carrier=world.Entities.Get(Entity(session,"machine"));
                    session.Seat(0).View.TryFindResource("supply",out var resource);
                    var origin=new PointSnapshot(world.Space.Id,Fix.FromInt(mode=="tie"?11:10),Fix.FromInt(mode=="tie"?11:10));
                    Code result=world.Services.Get<WorkBoard>().PreviewDock(destination,carrier,
                        WorkDockRequest.Haul(resource,ResourceHandlingPhase.Unloading),origin,
                        out Code selected,out _,out var arrival,out var travel);
                    c.Equal(Code.None,result,"complete prospective journey stays feasible: "+mode);
                    c.Equal(new Code("z.near-bay"),selected,"existing cheapest service selection unchanged: "+mode);
                    c.That(arrival is PointSnapshot at && at.Position==Fix.FromInt(mode=="blocked"?17:13),"feasible departure uses original cost and stable tie order: "+mode);
                    c.That(travel.RemainingTicks.HasValue && travel.Blocker.IsNone,"actual winning route retains its travel estimate: "+mode);
                    c.Equal(before,session.Hash(),"pure bounded choice allocates no claims/state/time: "+mode);
                    c.That(session.Fault==null,"losing route is not invoked; necessary fallback remains evaluated: "+mode);
                }
                c.Equal(game.Hash(),twin.Hash(),"restored independent session selects identically: "+mode);
            }
        }
        private static void ServicePruning(Check c)
        {
            foreach(string mode in new[]{"far","tie","blocked","all-blocked","pending","cost"})foreach(bool shuffled in new[]{false,true}) {
                var factory=Factory(false,"near",shuffled,serviceCase:mode);var game=New(factory);
                ulong before=game.Hash();var twin=factory.Load(game.Save());
                foreach(var session in new[]{game,twin}) {
                    var world=session.World;var destination=world.Entities.Get(Entity(session,"destination"));
                    var carrier=world.Entities.Get(Entity(session,"machine"));
                    session.Seat(0).View.TryFindResource("supply",out var resource);
                    var origin=new PointSnapshot(world.Space.Id,Fix.FromInt(10),Fix.FromInt(10));
                    if(mode=="cost") {
                        c.Throws<InvalidOperationException>(()=>world.Services.Get<WorkBoard>().PreviewDock(destination,carrier,
                            WorkDockRequest.Haul(resource,ResourceHandlingPhase.Unloading),origin,out _,out _,out _,out _,productiveOnly:true),
                            "losing candidate cost remains guarded after feasible incumbent");
                        c.That(session.Fault!=null,"losing cost mutation still latches original fault");
                        c.Throws<InvalidOperationException>(()=>session.Save(),"no healthy save from losing-cost mutation");
                        continue;
                    }
                    Code result=world.Services.Get<WorkBoard>().PreviewDock(destination,carrier,
                        WorkDockRequest.Haul(resource,ResourceHandlingPhase.Unloading),origin,
                        out Code selected,out _,out var arrival,out var travel,productiveOnly:true);
                    bool refused=mode=="all-blocked"||mode=="pending";
                    c.Equal(mode=="pending"?MotionAreaCodes.Pending:refused?MotionAreaCodes.Outside:Code.None,result,"necessary journeys and original typed refusal remain: "+mode);
                    c.Equal(refused?Code.None:new Code(mode=="blocked"?"z.losing-bay":"a.best-bay"),selected,"same feasible service rank or no prefix: "+mode);
                    c.That(refused?arrival==null:arrival is PointSnapshot at&&at.Position==Fix.FromInt(13),"actual journey arrival retained: "+mode);
                    c.That(refused?!travel.RemainingTicks.HasValue:travel.RemainingTicks.HasValue&&travel.Blocker.IsNone,"only proven journeys publish travel: "+mode);
                    c.Equal(before,session.Hash(),"candidate selection changes no authoritative state: "+mode);
                    c.That(session.Fault==null,"pure selection and necessary fallback remain healthy: "+mode);
                }
                if(mode!="cost")c.Equal(game.Hash(),twin.Hash(),"restored/shuffled selection keeps actual session identity: "+mode);
            }
        }
    }
}
