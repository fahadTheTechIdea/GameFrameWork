using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Normal player commands and detached facts only. Native contracts/JSON own all dock/game rules.
    internal static class NamedDockRecoveryTests
    {
        internal const string Case = "Named dock recovery: partial unloading, retained bay, physical queue, exit wait and saved replacement";
        internal const string RestorationCase = "Named dock restoration: original receipt, partial handling, physical egress and busy service";
        internal const string PointCase = "External point dock: named original restoration, physical service and saved continuation";
        internal const string AlternateCase = "Named dock alternate access: retained receipt physical egress and busy destination queue";
        private static readonly Code Alias=new Code("alias"),Far=new Code("return"),Bay=new Code("bay"),Wait=new Code("wait"),Exit=new Code("exit");
        private static readonly ResourceEndpointId First=new ResourceEndpointId("proof.pools",9000000001),
            Second=new ResourceEndpointId("proof.pools",9000000002),Fourth=new ResourceEndpointId("proof.pools",9000000004);
        private static readonly TileCoord BayTile=new TileCoord(21,10),WaitTile=new TileCoord(20,12),ExitTile=new TileCoord(14,10);
        internal static void Register()
        { TestRunner.Add(Case,Run); TestRunner.Add(RestorationCase,RunRestoration); TestRunner.Add(PointCase,c=>RunRestoration(c,true)); TestRunner.Add(AlternateCase,RunAlternate); }
        private static EntityView Entity(SimGame g,string id)=>g.Seat(0).Snapshot.Entities.Single(e=>e.Def==id||e.Def=="point."+id);
        private static EntityId Find(SimGame g,string id)=>Entity(g,id).Id;
        private static TileCoord Tile(SimGame g,string id)=>((TileSpatialSnapshot)Entity(g,id).Spatial).Tile;
        private static object Position(SimGame g,string id)
        {
            var spatial=Entity(g,id).Spatial;
            if(spatial is PointSnapshot point)return point.Position;
            if(spatial is TileSpatialSnapshot tile)return tile.Tile;
            throw new InvalidOperationException("unsupported proof geometry "+id);
        }
        private static ResourceId Steel(SimGame g)
        {if(!g.Seat(0).View.TryFindResource("steel",out var r))throw new InvalidOperationException("missing steel");return r;}
        private static LogisticsView Logistics(SimGame g)=>g.Seat(0).View.Feature<LogisticsView>();
        private static List<WorkJobView> Jobs(SimGame g)
        {var list=new List<WorkJobView>();g.Seat(0).View.Feature<WorkView>().CopyJobs(list);return list;}
        private static bool Live(WorkJobView j)=>j.State==JobState.Assigned||j.State==JobState.Open||j.State==JobState.Paused;
        private static WorkJobView Job(SimGame g,int id)=>Jobs(g).Single(j=>j.Id==id);
        private static LogisticsOperationalSnapshot Facts(SimGame g,string site,Code port=default,ResourceEndpointId? original=null)
        {
            Code refusal=Logistics(g).ReadDiagnostics(Find(g,site),Steel(g),out var facts,port,original);
            if(!refusal.IsNone)throw new InvalidOperationException("named dock facts "+site+"/"+refusal);return facts;
        }
        private static LogisticsOperationalSnapshot Original(SimGame g)=>Facts(g,"recovery.far",Far,First);
        private static LogisticsOperationalSnapshot Other(SimGame g)=>Facts(g,"recovery.far",Far,Second);
        private static Fix Cargo(SimGame g,string machine="dock.machine")=>Facts(g,machine).Stock;
        private static void Step(SimGame g,SimGame twin=null)
        {
            g.Update();twin?.Update();
            if(g.Fault!=null||twin?.Fault!=null||twin!=null&&g.Hash()!=twin.Hash())
                throw new InvalidOperationException("named dock Update "+g.Tick+"/"+g.Fault+"/"+twin?.Fault);
            int count=g.Seat(0).Snapshot.Entities.Count(e=>e.Kind==EntityKind.Vehicle&&(e.Spatial is TileSpatialSnapshot t&&t.Tile==BayTile||e.Spatial is PointSnapshot p&&p.Position==Fix.FromInt(21)));
            if(count>1)throw new InvalidOperationException("physical service bay overlaps at "+g.Tick);
        }
        private static void Until(SimGame g,Func<bool> done,string stage,SimGame twin=null)
        {
            for(int i=0;i<1000&&!done();i++)Step(g,twin);
            if(!done())throw new InvalidOperationException(stage+" at "+g.Tick+" main="+Position(g,"dock.machine")+" queue="+Position(g,"dock.queue")+
                " jobs="+string.Join(";",Jobs(g).Select(j=>j.Id+"/"+j.State+"/"+j.ToilIndex+"/"+j.Loaded+"/"+j.DestinationDockSlot+"/"+j.RecoveryDockSlot+"/"+j.RecoveryCause+"/"+j.Blocker)));
        }
        private static long Command(Check c,SimGame g,Command command)
        {
            ulong before=g.Hash();c.Equal(Code.None,g.Seat(0).Preview(command),"ordinary dock command preview");
            c.Equal(before,g.Hash(),"command preview pure");var result=TestKit.Order(g,0,command);
            c.That(result.Accepted,"native dock command "+result.Refusal);
            if(!result.Accepted||g.Fault!=null)throw new InvalidOperationException("named dock command "+result.Refusal+"/"+g.Fault);return result.Arg0;
        }
        private static void Held(Check c,SimGame g,string machine,bool held)
            =>Command(c,g,new SetWorkerHeldCommand{Worker=Find(g,machine),Held=held});
        private static void Band(Check c,SimGame g,string site,Code port,Fix amount,int priority)
            =>Command(c,g,new ConfigureStockPolicyCommand{Store=Find(g,site),Port=port,Resource=Steel(g),
                Change=LogisticsDemandChange.Replace(new LogisticsDemandSettings(true,amount,amount,
                    amount,priority,LogisticsReserveAccess.Protected))});
        private static void Policy(Check c,SimGame g,bool enabled)
            =>Command(c,g,new ConfigureWorkPolicyCommand{Worker=Find(g,"dock.machine"),
                Settings=new WorkPolicySettings(enabled,0,new[]{new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
        private static SimGame Save(Check c,GameFactory f,SimGame g)
        {var twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"actual native bay/exit/receipts/progress checkpoint");return twin;}
        private static void RunAlternate(Check c)
        {
            var factory=ScopedHaulGame.Factory(alternateDockAccess:true);
            var request=new NewGameRequest{Scenario="scoped-dock-restoration",Seed=1027,Seats={Controller.Human,Controller.Human}};
            var game=factory.NewGame(request);var idle=factory.NewGame(request);ulong isolated=idle.Hash();
            var limits=new ReplayLimits(4*1024*1024,6000);using var recording=game.RecordReplay(limits);
            var nextBay=new TileCoord(35,24);var nextWait=new TileCoord(34,26);
            bool Verify(Func<bool> ready)
            {
                int count=game.Seat(0).Snapshot.Entities.Count(e=>e.Kind==EntityKind.Vehicle&&e.Spatial is TileSpatialSnapshot t&&t.Tile==nextBay);
                if(count>1)throw new InvalidOperationException("alternate physical service bay overlaps");
                Fix total=Facts(game,"source.warehouse",new Code("stock")).Stock+Original(game).Stock+Other(game).Stock+
                    Facts(game,"recovery.far",Far,Fourth).Stock+Cargo(game)+Cargo(game,"dock.queue");
                if(total!=Fix.FromInt(30))throw new InvalidOperationException("alternate dock lost authored material "+total);
                return ready();
            }
            c.That(!game.Modules.Contains("sim.tycoon"),"queued retained delivery is resource-only");
            Held(c,game,"dock.queue",true);
            int funding=checked((int)Command(c,game,new HaulResourcesCommand{Source=Find(game,"source.warehouse"),Destination=Find(game,"source.sink"),
                SourcePort=new Code("stock"),DestinationPort=new Code("tank"),Resource=Steel(game),Ceiling=Fix.FromInt(8),Worker=Find(game,"dock.machine"),Priority=100}));
            Until(game,()=>Verify(()=>Job(game,funding).Loaded==Fix.FromInt(8)&&Job(game,funding).ToilIndex==2),"real retained-delivery pickup");
            Held(c,game,"dock.machine",true);Band(c,game,"restore.target",Alias,Fix.FromInt(8),200);
            Band(c,game,"recovery.far",Far,Fix.Zero,100);Held(c,game,"dock.machine",false);
            Until(game,()=>Verify(()=>Jobs(game).Any(j=>Live(j)&&j.Worker==Find(game,"dock.machine")&&j.Destination==Find(game,"restore.target"))),"original dock return acquired");
            int id=Jobs(game).Single(j=>Live(j)&&j.Worker==Find(game,"dock.machine")).Id;
            Until(game,()=>Verify(()=>Job(game,id).Loaded.IsPositive&&Job(game,id).Loaded<Fix.FromInt(8)),"earned original partial unloading");
            var original=Job(game,id);Fix prefix=Original(game).Stock,remaining=original.Loaded;
            c.Equal(BayTile,Tile(game,"dock.machine"),"original material prefix requires actual old service bay");
            c.That(!original.HandlingProgress.Equals(WorkProgress.Zero),"actual handling credit exists before changing access");
            Policy(c,game,false);
            Held(c,game,"dock.queue",false);
            int queued=checked((int)Command(c,game,new HaulResourcesCommand{Source=Find(game,"source.warehouse"),Destination=Find(game,"dock.alternate"),
                SourcePort=new Code("stock"),DestinationPort=new Code("queue"),Resource=Steel(game),Ceiling=Fix.FromInt(2),Worker=Find(game,"dock.queue"),Priority=100}));
            Until(game,()=>Verify(()=>Tile(game,"dock.queue")==nextBay&&Job(game,queued).Loaded==Fix.FromInt(2)&&Job(game,queued).ToilIndex==2),"second carrier physically occupies new service bay");
            Command(c,game,new ConfigureWorkPolicyCommand{Worker=Find(game,"dock.queue"),
                Settings=new WorkPolicySettings(false,0,new[]{new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
            Command(c,game,new SelectResourcePortCommand{Entity=Find(game,"restore.target"),Port=Alias,Endpoint=Second});
            c.Equal(Bay,Job(game,id).RecoveryDockSlot,"old bay retained for physical egress");
            c.Equal(remaining,Original(game).InboundPromised,"one actual original capacity receipt survives physical access loss");
            c.Equal(BayTile,Tile(game,"dock.machine"),"no teleport to replacement queue");
            var twin=Save(c,factory,game);for(int i=0;i<3;i++)Step(game,twin);
            c.Equal(BayTile,Tile(game,"dock.machine"),"disabled carrier still occupies old bay");
            c.Equal(original.HandlingProgress,Job(game,id).HandlingProgress,"old handling stays retained during paused egress");
            Policy(c,game,true);twin=Save(c,factory,game);bool reachedExit=false;
            Until(game,()=>Verify(()=>{reachedExit|=Tile(game,"dock.machine")==ExitTile;return Job(game,id).Destination==Find(game,"dock.alternate");}),
                "physical departure then alternate same-store waiting admission",twin);
            c.That(reachedExit,"actual original exit visited before rebinding");
            var rebound=Job(game,id);
            c.Equal(First,rebound.DestinationEndpoint,"new physical port retains same backing store");
            c.Equal(remaining,rebound.Loaded,"same job and carrier retain exact residual cargo");
            c.Equal(Wait,rebound.DestinationDockSlot,"busy new service permits ordinary waiting slot");
            c.Equal(Code.None,rebound.RecoveryDockSlot,"old bay released only after earned departure");
            c.Equal(WorkProgress.Zero,rebound.HandlingProgress,"service credit cannot move to a different physical bay");
            c.Equal(prefix,rebound.UnloadingBaseline,"physically delivered original prefix is retained");
            c.Equal(remaining,Original(game).InboundPromised,"queued access reacquires no duplicate capacity");
            twin=Save(c,factory,game);
            Until(game,()=>Verify(()=>Tile(game,"dock.machine")==nextWait),"physical travel into alternate waiting slot",twin);
            c.Equal(nextBay,Tile(game,"dock.queue"),"incumbent keeps exclusive new service bay");
            c.Equal(prefix,Original(game).Stock,"waiting carrier unloads nothing remotely");
            c.Equal(Fix.Zero,Other(game).Stock,"disabled busy carrier unloads nothing");
            game.Clock.Pause();twin.Clock.Pause();ulong paused=game.Hash();Step(game,twin);
            c.Equal(paused,game.Hash(),"one paused game clock freezes bay/queue/receipts/cargo");game.Clock.Resume();twin.Clock.Resume();
            twin=Save(c,factory,game);for(int i=0;i<3;i++)Step(game,twin);
            c.Equal(nextWait,Tile(game,"dock.machine"),"saved busy queue remains a physical wait");
            c.Equal(WorkProgress.Zero,Job(game,id).HandlingProgress,"waiting earns no unloading credit");
            Command(c,game,new ConfigureWorkPolicyCommand{Worker=Find(game,"dock.queue"),
                Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
            twin=Save(c,factory,game);
            Until(game,()=>Verify(()=>Cargo(game).IsZero&&Cargo(game,"dock.queue").IsZero&&!Jobs(game).Any(Live)),"real queued service and both physical departures",twin);
            c.Equal(Fix.FromInt(8),Original(game).Stock,"original full delivery reaches same store once");
            c.Equal(Fix.FromInt(2),Other(game).Stock,"separate carrier delivers only its own two units");
            c.Equal(Fix.FromInt(20),Facts(game,"source.warehouse",new Code("stock")).Stock,"neither delivery repeats pickup");
            c.Equal(Fix.Zero,Original(game).InboundPromised,"original completed receipt released");
            c.Equal(Fix.Zero,Other(game).InboundPromised,"separate completed receipt released");
            var foreign=new List<LogisticsDispatchOffer>();game.Seat(1).View.Feature<LogisticsView>().CopyQueue(foreign);
            c.Equal(0,foreign.Count,"private dock/recovery queue withheld");
            c.Equal(isolated,idle.Hash(),"interleaved session receives no queue or access changes");Save(c,factory,game);
            var replay=new ReplayVerifier().Verify(factory,recording.Finish(),limits);
            c.That(replay.Success&&replay.Game?.Hash()==game.Hash(),"ordinary retained dock queue replay "+replay.Failure+"/"+replay.Detail);
        }
        private static void RunRestoration(Check c)=>RunRestoration(c,false);
        private static void RunRestoration(Check c,bool point)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"restored original native dock delivery semantics");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"existing embedded receipt/progress layout");
            var f=ScopedHaulGame.Factory(restoredDockDelivery:true,pointDockRecovery:point);
            object bay=point?(object)Fix.FromInt(21):BayTile,exit=point?(object)Fix.FromInt(25):ExitTile;
            for(int episode=0;episode<3;episode++)
            {
                var g=f.NewGame(new NewGameRequest{Scenario=point?"scoped-point-dock-restoration":"scoped-dock-restoration",Seed=1019UL+(ulong)episode,Seats={Controller.Human,Controller.Human}});
                var recording=g.RecordReplay(new ReplayLimits(4*1024*1024,6000));
                c.That(!g.Modules.Contains("sim.tycoon"),"restoration needs no business or money");
                if(point)
                {
                    c.That(g.Modules.Contains("points")&&!g.Modules.Contains("sim.tile-navigation"),"authored external space replaces active tile routing");
                    c.That(g.Seat(0).Snapshot.Entities.All(e=>e.Spatial is PointSnapshot),"every actual initialized entity uses external point presence");
                    c.Equal(6,g.Seat(0).Snapshot.Entities.Count,"native world setup creates each point entity once");
                    c.That(!g.Seat(0).View.Feature<InventoryView>().TryGetCapacity(Find(g,"restore.target"), Code.None,out _),"external named-only dock invents no local stock");
                }
                Held(c,g,"dock.queue",true);
                int funding=checked((int)Command(c,g,new HaulResourcesCommand{Source=Find(g,"source.warehouse"),Destination=Find(g,"source.sink"),
                    SourcePort=new Code("stock"),DestinationPort=new Code("tank"),Resource=Steel(g),Ceiling=Fix.FromInt(8),Worker=Find(g,"dock.machine"),Priority=100}));
                Until(g,()=>Job(g,funding).Loaded==Fix.FromInt(8)&&Job(g,funding).ToilIndex==2,"restoration physical pickup");
                Held(c,g,"dock.machine",true);
                Band(c,g,"restore.target",Alias,Fix.FromInt(8),200);Band(c,g,"recovery.far",Far,Fix.Zero,100);Held(c,g,"dock.machine",false);
                Until(g,()=>Jobs(g).Any(j=>Live(j)&&j.Worker==Find(g,"dock.machine")&&j.DestinationEndpoint==First),"original acquired dock return");
                int id=Jobs(g).Single(j=>Live(j)&&j.Worker==Find(g,"dock.machine")).Id;
                Until(g,()=>Job(g,id).Loaded.IsPositive&&Job(g,id).Loaded<Fix.FromInt(8),"original productive partial unloading");
                var partial=Job(g,id);Fix remaining=partial.Loaded,prefix=Original(g).Stock;
                c.That(!partial.HandlingProgress.Equals(WorkProgress.Zero),"real original handling already earned");
                c.Equal(bay,Position(g,"dock.machine"),"original partial delivery requires physical bay");
                c.Equal(Fix.FromInt(8),prefix+remaining,"physical prefix and residual conserve eight");
                Command(c,g,new SelectResourcePortCommand{Entity=Find(g,"restore.target"),Port=Alias,Endpoint = new ResourceEndpointId("proof.pools", Second.Identity)});
                c.Equal(HaulRecoveryCodes.DestinationBindingLost,Job(g,id).RecoveryCause,"actual lost original access");
                c.Equal(Bay,Job(g,id).RecoveryDockSlot,"old bay retained during required egress");
                c.Equal(remaining,Original(g).InboundPromised,"one original residual promise during access loss");
                int queued=0;bool sawExit=false,sawAlternate=false;
                SimGame twin;
                if(episode==0)
                {
                    Policy(c,g,false);
                    Command(c,g,new SelectResourcePortCommand{Entity=Find(g,"restore.target"),Port=Alias,Endpoint = new ResourceEndpointId("proof.pools", First.Identity)});
                    c.Equal(bay,Position(g,"dock.machine"),"restoring access never teleports from old bay");
                    c.Equal(HaulRecoveryStage.Departing,Job(g,id).RecoveryStage,"restoration cannot skip admitted departure");
                    c.Equal(partial.HandlingProgress,Job(g,id).HandlingProgress,"restored paused egress keeps earned handling");
                    twin=Save(c,f,g);for(int i=0;i<3;i++)Step(g,twin);
                    c.Equal(Bay,Job(g,id).RecoveryDockSlot,"paused restored access cannot release bay");
                    Policy(c,g,true);twin=Save(c,f,g);
                    Until(g,()=>{sawExit|=Position(g,"dock.machine").Equals(exit);return Job(g,id).RecoveryCause.IsNone;},
                        "restored original still reaches actual exit before resuming",twin);
                }
                else
                {
                    twin=Save(c,f,g);
                    Until(g,()=>Job(g,id).RecoveryStage==HaulRecoveryStage.WaitingForDestination&&Position(g,"dock.machine").Equals(exit),
                        "settled physical departure retains unavailable original",twin);
                    sawExit=true;
                    c.Equal(Code.None,Job(g,id).RecoveryDockSlot,"real exit frees original bay");
                    c.Equal(remaining,Original(g).InboundPromised,"exit wait retains one original receipt");
                    if(episode==2)
                    {
                        Held(c,g,"dock.queue",false);
                        queued=checked((int)Command(c,g,new HaulResourcesCommand{Source=Find(g,"source.warehouse"),Destination=Find(g,"restore.target"),
                            SourcePort=new Code("stock"),DestinationPort=new Code("queue"),Resource=Steel(g),Ceiling=Fix.FromInt(2),Worker=Find(g,"dock.queue"),Priority=100}));
                        c.Equal(Second,Job(g,queued).DestinationEndpoint,"separate authored stable queue port");
                        Until(g,()=>Position(g,"dock.queue").Equals(bay)&&Job(g,queued).Loaded==Fix.FromInt(2)&&Job(g,queued).ToilIndex==2,
                            "second vehicle physically occupies original service before unloading");
                        Command(c,g,new ConfigureWorkPolicyCommand{Worker=Find(g,"dock.queue"),
                            Settings=new WorkPolicySettings(false,0,new[]{new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
                    }
                    Command(c,g,new SelectResourcePortCommand{Entity=Find(g,"restore.target"),Port=Alias,Endpoint = new ResourceEndpointId("proof.pools", First.Identity)});
                    if(episode==2)
                    {
                        // This is a normal later stock-band edit, not withdrawal of an acquired receipt.
                        Band(c,g,"restore.target",Alias,Fix.Zero,200);
                        twin=Save(c,f,g);for(int i=0;i<3;i++)Step(g,twin);
                        c.Equal(exit,Position(g,"dock.machine"),"restored original waits physically for busy service");
                        c.Equal(HaulRecoveryStage.WaitingForDestination,Job(g,id).RecoveryStage,"no false restored service admission");
                        c.Equal(Exit,Job(g,id).DestinationDockSlot,"partial handling never enters incompatible wait slot");
                        c.Equal(partial.HandlingProgress,Job(g,id).HandlingProgress,"busy service earns no unloading time");
                        c.Equal(remaining,Cargo(g),"busy bay preserves actual residual cargo");
                        c.Equal(remaining,Original(g).InboundPromised,"later zero stock target preserves acquired original promise");
                        c.Equal(Fix.Zero,Other(g).Stock,"paused second carrier cannot transfer through observation");
                        Command(c,g,new ConfigureWorkPolicyCommand{Worker=Find(g,"dock.queue"),
                            Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
                    }
                    twin=Save(c,f,g);
                    Until(g,()=>{sawAlternate|=Jobs(g).Any(j=>j.Id==queued&&Live(j)&&j.DestinationDockSlot==new Code("exit-alt"));
                        return Job(g,id).RecoveryCause.IsNone;},"original owned receipt resumes only when real service available",twin);
                }
                c.That(sawExit,"original access restoration never skips physical departure");
                c.Equal(First,Job(g,id).DestinationEndpoint,"restoration reuses actual original destination");
                c.Equal(Alias,Job(g,id).DestinationPort,"restoration keeps original authored port");
                c.Equal(Find(g,"restore.target"),Job(g,id).Destination,"restoration keeps original physical facility");
                c.Equal(Find(g,"dock.machine"),Job(g,id).Worker,"same job and physical carrier survive restoration");
                c.Equal(Bay,Job(g,id).DestinationDockSlot,"restored delivery reacquires actual productive service");
                c.Equal(Code.None,Job(g,id).RecoveryDockSlot,"completed original egress has no retained old bay");
                c.Equal(partial.HandlingProgress,Job(g,id).HandlingProgress,"restoring same delivery does not reset earned time");
                c.Equal(partial.UnloadingBaseline,Job(g,id).UnloadingBaseline,"same original leg preserves original unload baseline");
                c.Equal(remaining,Job(g,id).Loaded,"restoration admission never instantly unloads cargo");
                c.Equal(prefix,Original(g).Stock,"prefix stays unchanged until physical return");
                c.Equal(remaining,Original(g).InboundPromised,"owned capacity reused once, not requested twice");
                c.Equal(Fix.Zero,Facts(g,"recovery.far",Far).InboundPromised,"no invented replacement claim");
                twin=Save(c,f,g);
                Until(g,()=>{sawAlternate|=Jobs(g).Any(j=>j.Id==queued&&Live(j)&&j.DestinationDockSlot==new Code("exit-alt"));
                    return !Jobs(g).Any(Live)&&Cargo(g).IsZero&&Cargo(g,"dock.queue").IsZero;},
                    "physical restored delivery and independent departure complete",twin);
                c.Equal(Fix.FromInt(8),Original(g).Stock,"original receives exactly its real complete eight");
                c.Equal(episode==2?Fix.FromInt(2):Fix.Zero,Other(g).Stock,"independent second receipt has exact physical delivery");
                c.Equal(Fix.Zero,Facts(g,"recovery.far",Far).Stock,"restored original does not fabricate other output");
                c.Equal(Fix.Zero,Original(g).InboundPromised,"original inbound receipt consumed without leak");
                c.Equal(Fix.Zero,Other(g).InboundPromised,"second inbound receipt consumed without leak");
                c.Equal(Fix.Zero,Facts(g,"recovery.far",Far).InboundPromised,"no speculative future receipt");
                c.Equal(Fix.FromInt(30),Facts(g,"source.warehouse").Stock+Original(g).Stock+Other(g).Stock,"actual local thirty conserved");
                if(episode==2)c.That(sawAlternate,"busy second carrier uses real authored alternative exit, no overlapping shortcut");
                c.Equal(g.Hash(),twin.Hash(),"restored saved stages continue identically");
                c.That(new ReplayVerifier().Verify(f,recording.Finish(),new ReplayLimits(4*1024*1024,6000)).Success,"native restoration commands replay");
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"named resource dock recovery semantics");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"same embedded job layout, no compatibility reader");
            var f=ScopedHaulGame.Factory(originalDockRecovery:true);
            var g=f.NewGame(new NewGameRequest{Scenario="scoped-dock-original-loss",Seed=1009,Seats={Controller.Human,Controller.Human}});
            var recording=g.RecordReplay(new ReplayLimits(4*1024*1024,6000));
            c.That(!g.Modules.Contains("sim.tycoon"),"native named dock gameplay needs no business/money");
            c.That(!g.Seat(0).View.Feature<InventoryView>().TryGetCapacity(Find(g,"dock.target"), Code.None,out _),"named-only dock has no invented local storage");
            Held(c,g,"dock.queue",true);
            int funding=checked((int)Command(c,g,new HaulResourcesCommand{Source=Find(g,"source.warehouse"),Destination=Find(g,"source.sink"),
                SourcePort=new Code("stock"),DestinationPort=new Code("tank"),Resource=Steel(g),Ceiling=Fix.FromInt(8),Worker=Find(g,"dock.machine"),Priority=100}));
            Until(g,()=>Job(g,funding).Loaded==Fix.FromInt(8)&&Job(g,funding).ToilIndex==2,"physical original complete pickup");
            Held(c,g,"dock.machine",true);
            c.Equal(Fix.FromInt(8),Cargo(g),"player interruption retains actual loaded cargo");
            Band(c,g,"dock.target",Alias,Fix.FromInt(8),200);Band(c,g,"recovery.far",Far,Fix.Zero,100);Held(c,g,"dock.machine",false);
            Until(g,()=>Jobs(g).Any(j=>Live(j)&&!j.Source.IsValid&&j.DestinationEndpoint==First),"native named dock return");
            int id=Jobs(g).Single(j=>Live(j)&&j.Worker==Find(g,"dock.machine")).Id;
            c.Equal(Bay,Job(g,id).DestinationDockSlot,"native named-only dock claims real service bay");
            Until(g,()=>Job(g,id).Loaded.IsPositive&&Job(g,id).Loaded<Fix.FromInt(8),"physical original partial unloading");
            c.Equal(BayTile,Tile(g,"dock.machine"),"partial transfer occurs only at actual service tile");
            Fix prefix=Original(g).Stock,remaining=Cargo(g);
            c.That(prefix.IsPositive&&remaining.IsPositive,"actual committed prefix and residual both positive");
            c.Equal(Fix.FromInt(8),prefix+remaining,"original partial unloading conserves eight");
            var twin=Save(c,f,g);
            Command(c,g,new SelectResourcePortCommand{Entity=Find(g,"dock.target"),Port=Alias,Endpoint = new ResourceEndpointId("proof.pools", Second.Identity)});
            c.Equal(HaulRecoveryCodes.DestinationBindingLost,Job(g,id).RecoveryCause,"same native job admits explicitly authored access-loss cause");
            c.Equal(HaulRecoveryStage.Departing,Job(g,id).RecoveryStage,"lost access begins actual egress, not instant retarget");
            c.Equal(Bay,Job(g,id).RecoveryDockSlot,"old bay retained alongside selected departure");
            c.Equal(Exit,Job(g,id).DestinationDockSlot,"authored physical exit selected");
            c.Equal(BayTile,Tile(g,"dock.machine"),"departure admission never teleports carrier");
            c.Equal(remaining,Original(g).InboundPromised,"old actual residual receipt remains protected during departure");
            c.Equal(Fix.Zero,Facts(g,"recovery.far",Far).InboundPromised,"no speculative future replacement capacity");
            Policy(c,g,false);
            c.Equal(JobState.Paused,Job(g,id).State,"work policy pauses admitted egress without ending job");
            c.Equal(BayTile,Tile(g,"dock.machine"),"disabled carrier remains physically at old bay");
            Held(c,g,"dock.queue",false);
            int queued=checked((int)Command(c,g,new HaulResourcesCommand{Source=Find(g,"source.warehouse"),Destination=Find(g,"dock.target"),
                SourcePort=new Code("stock"),DestinationPort=Alias,Resource=Steel(g),Ceiling=Fix.FromInt(2),Worker=Find(g,"dock.queue"),Priority=100}));
            c.Equal(Second,Job(g,queued).DestinationEndpoint,"new normal haul binds unrelated current named store");
            c.Equal(Wait,Job(g,queued).DestinationDockSlot,"retained old bay sends ordinary carrier to authored wait slot");
            twin=Save(c,f,g);
            Until(g,()=>Job(g,queued).DockWaiting&&Tile(g,"dock.queue")==WaitTile,"native physically arrived dock queue",twin);
            c.Equal(Fix.Zero,Other(g).Stock,"queued carrier never silently transfers through blocked bay");
            c.Equal(Fix.FromInt(2),Cargo(g,"dock.queue"),"queued delivery has physically picked up its own cargo");
            c.Equal(BayTile,Tile(g,"dock.machine"),"paused recovery still physically occupies bay");
            c.Equal(remaining,Cargo(g),"paused departure earns no unload or cargo loss");
            c.Equal(prefix,Original(g).Stock,"completed original prefix is never retracted");
            c.Equal(Bay,Job(g,id).RecoveryDockSlot,"queue waiting cannot steal original bay claim");
            Policy(c,g,true);twin=Save(c,f,g);
            Until(g,()=>Tile(g,"dock.machine")!=BayTile&&Tile(g,"dock.machine")!=ExitTile,"physical intermediate egress",twin);
            c.Equal(new Code("moving"),Entity(g,"dock.machine").State,"entity state follows real engine movement update");
            c.Equal(Bay,Job(g,id).RecoveryDockSlot,"empty physical bay stays claimed until actual exit");
            c.Equal(Wait,Job(g,queued).DestinationDockSlot,"queue cannot claim bay before recovery reaches exit");
            c.Equal(remaining,Job(g,id).Loaded,"intermediate egress retains actual residual");
            twin=Save(c,f,g);
            Until(g,()=>Job(g,id).RecoveryStage==HaulRecoveryStage.WaitingForDestination&&Tile(g,"dock.machine")==ExitTile,
                "actual departure and complete-capacity wait",twin);
            c.Equal(Code.None,Job(g,id).RecoveryDockSlot,"old bay releases only at actual exit");
            c.Equal(HaulRecoveryCodes.DestinationUnavailable,Job(g,id).Blocker,"future capacity absence is a typed exit wait");
            c.Equal(First,Job(g,id).DestinationEndpoint,"waiting keeps original actual receipt identity");
            c.Equal(remaining,Original(g).InboundPromised,"original receipt retained while no whole replacement exists");
            c.Equal(Fix.Zero,Facts(g,"recovery.far",Far).InboundPromised,"exit wait still holds no speculative replacement");
            twin=Save(c,f,g);
            Until(g,()=>Other(g).Stock==Fix.FromInt(2),"queued carrier promotes and physically unloads current named store",twin);
            c.Equal(prefix,Original(g).Stock,"queued current delivery cannot alter original completed prefix");
            c.Equal(remaining,Cargo(g),"original exit wait retains all remaining cargo");
            c.Equal(ExitTile,Tile(g,"dock.machine"),"wait occurs at actual authored exit, not home/depot");
            c.Equal(Fix.Zero,Other(g).InboundPromised,"queued unload retires exact current receipt");
            c.That(Jobs(g).Any(j=>j.Id==queued&&Live(j)),"queued departure stays physical while original carrier holds shared exit");
            c.Equal(Fix.FromInt(30),Facts(g,"source.warehouse").Stock+Original(g).Stock+Other(g).Stock+Cargo(g)+Cargo(g,"dock.queue"),
                "queue and exit wait preserve actual local thirty");
            Band(c,g,"recovery.far",Far,remaining,100);
            Until(g,()=>Job(g,id).DestinationEndpoint==Fourth,"normal stock policy admits complete remaining replacement");
            c.Equal(Find(g,"dock.machine"),Job(g,id).Worker,"same actual carrier and job survive entire native dock workflow");
            c.Equal(prefix,Job(g,id).UnloadingBaseline,"replacement retains original physically delivered prefix");
            c.Equal(remaining,Job(g,id).Loaded,"replacement admits only real residual, not original full eight");
            c.Equal(Fix.Zero,Original(g).InboundPromised,"replacement releases exact surviving original receipt");
            c.Equal(remaining,Facts(g,"recovery.far",Far).InboundPromised,"complete residual replacement claimed once");
            c.Equal(Fix.Zero,Facts(g,"recovery.far",Far).Stock,"replacement admission is not delivery");
            twin=Save(c,f,g);
            Until(g,()=>!Jobs(g).Any(Live)&&Cargo(g).IsZero&&Cargo(g,"dock.queue").IsZero,
                "native residual delivery and both physical departures",twin);
            c.Equal(prefix,Original(g).Stock,"original retains only actually delivered prefix");
            c.Equal(remaining,Facts(g,"recovery.far",Far).Stock,"replacement receives exactly remaining original cargo");
            c.Equal(Fix.FromInt(2),Other(g).Stock,"queued carrier delivered only its independent two");
            c.Equal(Fix.Zero,Original(g).InboundPromised,"no original promise leak");
            c.Equal(Fix.Zero,Other(g).InboundPromised,"no current named promise leak");
            c.Equal(Fix.Zero,Facts(g,"recovery.far",Far).InboundPromised,"no replacement promise leak");
            c.Equal(Fix.FromInt(30),Facts(g,"source.warehouse").Stock+Original(g).Stock+Other(g).Stock+Facts(g,"recovery.far",Far).Stock,
                "complete physical workflow local thirty-unit conservation");
            c.Equal(g.Hash(),twin.Hash(),"all saved physical stages settle identically");
            c.That(new ReplayVerifier().Verify(f,recording.Finish(),new ReplayLimits(4*1024*1024,6000)).Success,"normal native dock command replay");
        }
    }
}
