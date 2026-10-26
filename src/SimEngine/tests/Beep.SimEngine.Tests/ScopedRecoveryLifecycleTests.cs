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
    // Player commands and immutable facts only; native JSON/modules own removal and recovery.
    internal static class ScopedRecoveryLifecycleTests
    {
        internal const string Case = "Scoped recovery lifecycle: original claim reroute, physical site loss, waiting and cancellation";
        internal const string RetainedAccessCase = "Scoped retained endpoint: physical site loss rebinds access without replacing capacity";
        private static readonly Code Tank=new Code("tank"), Alias=new Code("alias"), Far=new Code("return");
        private static readonly ResourceEndpointId First=new ResourceEndpointId("proof.pools",9000000001),
            Second=new ResourceEndpointId("proof.pools",9000000002), Fourth=new ResourceEndpointId("proof.pools",9000000004);
        internal static void Register()
        {
            TestRunner.Add(Case,Run);
            TestRunner.Add(RetainedAccessCase,RetainedAccess);
        }
        private static EntityId Find(SimGame game,string prototype) => game.Seat(0).Snapshot.Entities.Single(e=>e.Def==prototype).Id;
        private static ResourceId Steel(SimGame game)
        { if(!game.Seat(0).View.TryFindResource("steel",out var id)) throw new InvalidOperationException("missing steel"); return id; }
        private static LogisticsView Logistics(SimGame game) => game.Seat(0).View.Feature<LogisticsView>();
        private static List<WorkJobView> Jobs(SimGame game)
        { var list=new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(list); return list; }
        private static bool Live(WorkJobView job) => job.State==JobState.Open || job.State==JobState.Assigned || job.State==JobState.Paused;
        private static WorkJobView Job(SimGame game,int id) => Jobs(game).Single(j=>j.Id==id);
        private static void Step(SimGame game,SimGame twin=null)
        {
            game.Update(); twin?.Update();
            if(game.Fault!=null || twin?.Fault!=null || twin!=null && game.Hash()!=twin.Hash())
                throw new InvalidOperationException("recovery lifecycle Update "+game.Tick+"/"+game.Fault+"/"+twin?.Fault);
        }
        private static void Until(SimGame game,Func<bool> done,string stage,SimGame twin=null)
        {
            for(int i=0;i<1000&&!done();i++) Step(game,twin);
            if(!done()) throw new InvalidOperationException(stage+" at "+game.Tick+" jobs="+
                string.Join(";",Jobs(game).Select(j=>j.Id+"/"+j.State+"/"+j.ToilIndex+"/"+j.Loaded+"/"+j.DestinationEndpoint+"/"+j.Blocker)));
        }
        private static long Command(Check c,SimGame game,Command command)
        {
            ulong hash=game.Hash(); c.Equal(Code.None,game.Seat(0).Preview(command),"normal command preview");
            c.Equal(hash,game.Hash(),"preview pure"); var reply=TestKit.Order(game,0,command);
            c.That(reply.Accepted,"native command execution "+reply.Refusal);
            if(!reply.Accepted||game.Fault!=null) throw new InvalidOperationException("lifecycle command "+reply.Refusal+"/"+game.Fault);
            return reply.Arg0;
        }
        private static LogisticsOperationalSnapshot Facts(SimGame game,string entity,Code port=default,ResourceEndpointId? original=null)
        {
            Code refusal=Logistics(game).ReadDiagnostics(Find(game,entity),Steel(game),out var facts,port,original);
            if(!refusal.IsNone) throw new InvalidOperationException("lifecycle facts "+entity+"/"+refusal); return facts;
        }
        private static LogisticsOperationalSnapshot Original(SimGame game)
            => Facts(game,"recovery.far",Far,First); // An ordinary authored alias can observe the surviving actual provider.
        private static Fix Cargo(SimGame game) => Facts(game,"reroute.machine").Stock;
        private static SimGame Save(Check c,GameFactory factory,SimGame game)
        { var twin=factory.Load(game.Save()); c.Equal(game.Hash(),twin.Hash(),"surviving original claim and cargo checkpoint"); return twin; }
        private static void Band(Check c,SimGame game,string entity,Code port,int amount,int priority)
            => Command(c,game,new ConfigureStockPolicyCommand {Store=Find(game,entity),Port=port,Resource=Steel(game),
                Change=LogisticsDemandChange.Replace(new LogisticsDemandSettings(true,Fix.FromInt(amount),Fix.FromInt(amount),Fix.FromInt(amount),priority,LogisticsReserveAccess.Protected))});
        private static void Policy(Check c,SimGame game,bool enabled,string area=null)
            => Command(c,game,new ConfigureWorkPolicyCommand {Worker=Find(game,"reroute.machine"),
                Settings=new WorkPolicySettings(enabled,0,new[] {new WorkerRoleId("haul")},area,true,true,WorkAssignmentMode.All)});
        private static void Select(Check c,SimGame game,ResourceEndpointId endpoint)
            => Command(c,game,new SelectResourcePortCommand {Entity=Find(game,"reroute.target"),Port=Alias,Endpoint = new ResourceEndpointId("proof.pools", endpoint.Identity)});
        private static void Held(Check c,SimGame game,bool held)
            => Command(c,game,new SetWorkerHeldCommand {Worker=Find(game,"reroute.machine"),Held=held});
        private static SimGame New(GameFactory factory)
            => factory.NewGame(new NewGameRequest {Scenario="scoped-reroute",Seed=977,Seats={Controller.Human,Controller.Human}});
        private static void LoadAndInterrupt(Check c,SimGame game)
        {
            int id=checked((int)Command(c,game,new HaulResourcesCommand {Source=Find(game,"source.warehouse"),Destination=Find(game,"source.sink"),
                SourcePort=new Code("stock"),DestinationPort=new Code("tank"),Resource=Steel(game),Ceiling=Fix.FromInt(8),Worker=Find(game,"reroute.machine"),Priority=100}));
            Until(game,()=>Job(game,id).Loaded==Fix.FromInt(8)&&Job(game,id).ToilIndex==2,"physical original full pickup");
            Held(c,game,true);
            c.That(!Jobs(game).Any(j=>j.Id==id&&Live(j)),"player interruption ends only original job");
            c.Equal(Fix.FromInt(8),Cargo(game),"all actual cargo retained after interruption");
            c.Equal(Fix.FromInt(22),Facts(game,"source.warehouse").Stock,"only actual pickup removes warehouse stock");
        }
        private static int StartReturn(Check c,SimGame game,int farDemand)
        {
            Band(c,game,"reroute.target",Tank,8,200);
            Band(c,game,"recovery.far",Far,farDemand,100);
            Held(c,game,false);
            Until(game,()=>Jobs(game).Any(j=>Live(j)&&!j.Source.IsValid&&j.DestinationEndpoint==First),"actual named original cargo return");
            var job=Jobs(game).Single(j=>Live(j)&&!j.Source.IsValid);
            c.Equal(Fix.FromInt(8),job.Amount,"original highest-priority return binds complete actual eight");
            c.Equal(Fix.FromInt(8),Original(game).InboundPromised,"original independent store holds one native eight-unit receipt");
            c.Equal(Alias,job.DestinationPort,"ordinal authored alias retained independently of demand name");
            return job.Id;
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual original claim lifecycle protocol");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"existing native layouts preserved without old reader");
            var factory=ScopedHaulGame.Factory(scopedReroutes:true);
            AreaReroute(c,factory); SiteLoss(c,factory,false); SiteLoss(c,factory,true);
        }
        private static void AreaReroute(Check c,GameFactory factory)
        {
            var game=New(factory); var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,6000));
            c.That(!game.Modules.Contains("sim.tycoon"),"removal/recovery are resource-only gameplay");
            LoadAndInterrupt(c,game); int id=StartReturn(c,game,8);
            Select(c,game,Second);
            c.Equal(First,Job(game,id).DestinationEndpoint,"current selection edit cannot relocate original claim");
            var twin=Save(c,factory,game);
            Policy(c,game,true,"reroute.without-target");
            Until(game,()=>Job(game,id).DestinationEndpoint==Fourth,"normal area change reroutes same native job");
            c.Equal(id,Job(game,id).Id,"no replacement job identity or duplicate dispatch");
            c.Equal(Find(game,"reroute.machine"),Job(game,id).Worker,"same actual carrier retains cargo");
            c.Equal(Far,Job(game,id).DestinationPort,"replacement named store admitted");
            c.Equal(Fix.FromInt(8),Job(game,id).Loaded,"replacement never changes physical loaded amount");
            c.Equal(Fix.Zero,Original(game).InboundPromised,"original First receipt released despite current selection pointing to Second");
            c.Equal(Fix.Zero,Facts(game,"reroute.target",Alias,Second).InboundPromised,"unrelated current Second store is never promised or released");
            c.Equal(Fix.FromInt(8),Facts(game,"recovery.far",Far).InboundPromised,"complete actual replacement capacity claimed once");
            c.Equal(Fix.Zero,Facts(game,"recovery.far",Far).Stock,"reroute acquires no instant delivery");
            twin=Save(c,factory,game);
            Until(game,()=>Cargo(game).IsZero&&!Jobs(game).Any(Live),"native area-constrained replacement movement and unloading",twin);
            c.Equal(Fix.FromInt(8),Facts(game,"recovery.far",Far).Stock,"actual replacement physically receives all eight");
            c.Equal(Fix.Zero,Original(game).Stock,"original store receives no rerouted stock");
            c.Equal(Fix.FromInt(30),Facts(game,"source.warehouse").Stock+Facts(game,"recovery.far",Far).Stock,"area reroute preserves original thirty");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,6000)).Success,"native area reroute replay");
        }
        private static void RetainedAccess(Check c)
        {
            var factory=ScopedHaulGame.Factory(scopedReroutes:true);
            foreach(bool partial in new[]{false,true}) RetainedAccess(c,factory,partial);
        }
        private static void RetainedAccess(Check c,GameFactory factory,bool partial)
        {
            var game=New(factory);var idle=New(factory);ulong isolated=idle.Hash();
            var limits=new ReplayLimits(4*1024*1024,6000);using var recording=game.RecordReplay(limits);
            c.That(!game.Modules.Contains("sim.tycoon"),"alternate access recovery needs no business module");
            EntityId target=Find(game,"reroute.target"),replacement=Find(game,"recovery.far");
            var actual=Logistics(game).PreviewHaul(Find(game,"source.warehouse"),Find(game,"source.sink"),Steel(game),Fix.FromInt(8),
                sourcePort:new Code("stock"),destinationPort:Tank,worker:Find(game,"reroute.machine"));
            c.Equal(HaulStatus.Ready,actual.Status,"actual preview supplies native feasible quantum");
            RecoveryDestinationFacts Commitment(Fix amount) => new RecoveryDestinationFacts(new PlayerId(0),actual.Source,actual.Destination,
                actual.Carrier,Steel(game),actual.Amount,actual.QuantityQuantum,Fix.Zero,0,0,actual.SourceEndpoint,
                actual.DestinationPort,actual.DestinationEndpoint,amount);
            ulong before=game.Hash();
            c.Throws<ArgumentException>(()=>Commitment(-Fix.One),"negative committed capacity refused");
            c.Throws<ArgumentException>(()=>Commitment(actual.Amount+Fix.One),"commitment cannot exceed actual feasible quantity");
            c.Throws<ArgumentException>(()=>Commitment(Fix.FromRatio(1,2)),"committed capacity requires actual native quantum");
            var facts=Commitment(actual.Amount);
            c.That(!facts.HasDemand&&facts.CommittedAmount==actual.Amount,"retained delivery facts distinguish commitment from new demand");
            c.Equal(actual.Amount,game.World.Content.Get<IRecoveryDestinationPolicy>("reroute.demand-only").Evaluate(facts).Ceiling,
                "authored policy honors valid commitment without fabricating demand");
            c.Equal(before,game.Hash(),"detached commitment validation/decision changes no session state");
            LoadAndInterrupt(c,game);int id=StartReturn(c,game,3);
            if(partial) Until(game,()=>Original(game).Stock.IsPositive&&Job(game,id).Loaded.IsPositive,"physically earned original delivery prefix");
            Fix prefix=Original(game).Stock,remaining=Cargo(game);
            c.Equal(Fix.FromInt(8),prefix+remaining,"actual original prefix plus actual carrier stock");
            if(partial)c.That(prefix.IsPositive&&remaining.IsPositive,"real partial unload, not synthetic progress");
            Policy(c,game,false);
            c.That(Job(game,id).State==JobState.Paused,"disabled worker retains original loaded job and receipt");
            Command(c,game,new DemolishCommand{Target=target});var twin=Save(c,factory,game);
            Until(game,()=>!game.Seat(0).Snapshot.Entities.Any(e=>e.Id==target),"ordinary machine completes five-minute physical demolition",twin);
            Until(game,()=>Job(game,id).Blocker==HaulRecoveryCodes.DestinationUnavailable,"original access missing and replacement room insufficient",twin);
            c.Equal(remaining,Original(game).InboundPromised,"original backing store keeps exact remaining delivery promise");
            c.Equal(prefix,Original(game).Stock,"site removal never destroys independent original material");
            c.Equal(remaining,Cargo(game),"loaded material waits physically after site removal");
            twin=Save(c,factory,game);for(int i=0;i<3;i++)Step(game,twin);
            game.Clock.Pause();twin.Clock.Pause();ulong paused=game.Hash();Step(game,twin);
            c.Equal(paused,game.Hash(),"one paused clock freezes retained receipt and stranded carrier");game.Clock.Resume();twin.Clock.Resume();
            Band(c,game,"recovery.far",Far,0,100); // Withdraw the separate Fourth request before changing physical selection.
            Command(c,game,new SelectResourcePortCommand{Entity=replacement,Port=Far,Endpoint=First});
            // No fresh demand is authored for First. Its old acquired promise must not be
            // mistaken for a new demand gap, nor allocated to another backing store.
            Policy(c,game,true);
            Until(game,()=>Job(game,id).Destination==replacement,"same job selects surviving physical access to original endpoint");
            var rebound=Job(game,id);
            c.Equal(First,rebound.DestinationEndpoint,"same actual backing store, not a substitute");
            c.Equal(Far,rebound.DestinationPort,"new physical port binding");
            c.Equal(Find(game,"reroute.machine"),rebound.Worker,"same actual carrier and execution identity");
            c.Equal(remaining,rebound.Loaded,"rebind does not load, refund or unload material");
            c.Equal(prefix,rebound.UnloadingBaseline,"already delivered prefix stays authoritative");
            c.Equal(WorkProgress.Zero,rebound.HandlingProgress,"unfinished service credit cannot move to another physical site");
            c.Equal(remaining,Facts(game,"recovery.far",Far).InboundPromised,"existing exact capacity promise retained once");
            c.Equal(Fix.Zero,Facts(game,"recovery.far",Far).UnpromisedGap,"same-store capacity receipt is not fabricated fresh demand");
            c.Equal(prefix,Facts(game,"recovery.far",Far).Stock,"alternate access is not instant completion");
            twin=Save(c,factory,game);
            Until(game,()=>Cargo(game).IsZero&&!Jobs(game).Any(Live),"saved real travel and timed unloading through surviving alias",twin);
            c.Equal(Fix.FromInt(8),Facts(game,"recovery.far",Far).Stock,"complete original delivery arrives exactly once in same store");
            c.Equal(Fix.Zero,Facts(game,"recovery.far",Far).InboundPromised,"completed retained receipt has no leak");
            c.Equal(Fix.FromInt(22),Facts(game,"source.warehouse").Stock,"alternate access never repeats pickup");
            c.Equal(Fix.Zero,Facts(game,"recovery.far",Far,Fourth).Stock,"cancelled separate stock request receives no material");
            c.Equal(Fix.FromInt(30),Facts(game,"source.warehouse").Stock+Facts(game,"recovery.far",Far).Stock,"physical loss and alias delivery conserve original material");
            var foreign=new List<LogisticsDispatchOffer>();game.Seat(1).View.Feature<LogisticsView>().CopyQueue(foreign);
            c.Equal(0,foreign.Count,"foreign company gets no private recovery queue");
            c.Equal(isolated,idle.Hash(),"another session receives no port selection or demolition");
            Save(c,factory,game);
            var replay=new ReplayVerifier().Verify(factory,recording.Finish(),limits);
            c.That(replay.Success&&replay.Game?.Hash()==game.Hash(),"ordinary physical loss/retained access replay "+replay.Failure+"/"+replay.Detail);
        }
        private static void SiteLoss(Check c,GameFactory factory,bool cancel)
        {
            var game=New(factory); var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,6000));
            EntityId target=Find(game,"reroute.target");
            LoadAndInterrupt(c,game); int id=StartReturn(c,game,3);
            Select(c,game,Second); Policy(c,game,false);
            c.That(Live(Job(game,id))&&Job(game,id).State==JobState.Paused,"disabled policy retains original job/claim without cancelling cargo");
            long removal=Command(c,game,new DemolishCommand {Target=target});
            c.That(removal>0&&game.Seat(0).Snapshot.Entities.Any(e=>e.Id==target),"demolition creates physical intent, no instant site loss");
            var twin=Save(c,factory,game);
            Until(game,()=>!game.Seat(0).Snapshot.Entities.Any(e=>e.Id==target),"ordinary demolition machine earns site removal",twin);
            Until(game,()=>Job(game,id).Blocker==HaulRecoveryCodes.DestinationUnavailable,"removed site enters native full-cargo wait",twin);
            c.Equal(Fix.FromInt(8),Cargo(game),"site removal does not delete real carried material");
            c.Equal(Fix.FromInt(8),Original(game).InboundPromised,"independent original store/receipt survive removed geometry");
            c.Equal(Fix.Zero,Facts(game,"recovery.far",Far).InboundPromised,"three-unit alternative cannot acquire partial eight-unit replacement");
            twin=Save(c,factory,game);
            for(int i=0;i<4;i++) Step(game,twin);
            c.Equal(Fix.FromInt(8),Original(game).InboundPromised,"saved no-capacity wait retains original actual promise");
            c.Equal(Fix.FromInt(8),Cargo(game),"saved no-capacity wait neither unloads nor duplicates");
            if(cancel)
            {
                Held(c,game,true);
                c.That(!Jobs(game).Any(j=>j.Id==id&&Live(j)),"ordinary held command cancels the stranded actual job");
                c.Equal(Fix.Zero,Original(game).InboundPromised,"cancellation releases original surviving scoped receipt after site loss");
                c.Equal(Fix.FromInt(8),Cargo(game),"cancellation retains all physical residual cargo");
                Band(c,game,"recovery.far",Far,8,100); Policy(c,game,true); Held(c,game,false);
                Until(game,()=>Jobs(game).Any(j=>Live(j)&&!j.Source.IsValid&&j.DestinationEndpoint==Fourth),"ordinary remaining cargo reacquires actual return");
                c.That(Jobs(game).Single(j=>Live(j)&&!j.Source.IsValid).Id!=id,"cancelled execution is never silently resumed");
            }
            else
            {
                Policy(c,game,true);
                c.Equal(HaulRecoveryCodes.DestinationUnavailable,Job(game,id).Blocker,"too-small demand still preserves complete loaded job");
                Band(c,game,"recovery.far",Far,8,100);
                Until(game,()=>Job(game,id).DestinationEndpoint==Fourth,"full replacement becomes feasible through native stock policy");
                c.Equal(id,Job(game,id).Id,"lost-site reroute preserves same native job");
                c.Equal(Fix.Zero,Original(game).InboundPromised,"successful replacement releases original surviving receipt even though its site is gone");
                c.Equal(Fix.FromInt(8),Job(game,id).Loaded,"lost-site replacement preserves complete real cargo");
            }
            c.Equal(Fix.FromInt(8),Facts(game,"recovery.far",Far).InboundPromised,"one full actual replacement receipt");
            c.Equal(Fix.Zero,Facts(game,"recovery.far",Far).Stock,"replacement admission is not delivery");
            twin=Save(c,factory,game);
            Until(game,()=>Cargo(game).IsZero&&!Jobs(game).Any(Live),"physical post-loss replacement delivery and departure",twin);
            c.Equal(Fix.FromInt(8),Facts(game,"recovery.far",Far).Stock,"all original material arrives at surviving replacement");
            c.Equal(Fix.Zero,Original(game).InboundPromised,"no original receipt leak after completion");
            c.Equal(Fix.Zero,Facts(game,"recovery.far",Far).InboundPromised,"completed replacement releases its actual capacity promise");
            c.Equal(Fix.FromInt(30),Facts(game,"source.warehouse").Stock+Facts(game,"recovery.far",Far).Stock+Cargo(game),"physical removal/wait/recovery conserves original thirty");
            c.Equal(game.Hash(),twin.Hash(),"post-loss native claims and physical state settle identically");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,6000)).Success,
                cancel?"native lost-site cancellation/reassignment replay":"native lost-site wait/reroute replay");
        }
    }
}

