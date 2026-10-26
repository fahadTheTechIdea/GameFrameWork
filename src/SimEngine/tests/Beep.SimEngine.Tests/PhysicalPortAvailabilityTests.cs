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
    // Actual JSON/master and ordinary public player inputs only; provider probes execute inside the engine.
    internal static class PhysicalPortAvailabilityTests
    {
        internal const string Case = "Physical port availability: typed refusals original cargo restoration receiving-only endpoints and production";
        private static readonly ResourceEndpointId First = new ResourceEndpointId("proof.pools",9000000001), Second = new ResourceEndpointId("proof.pools",9000000002);
        private static readonly Code Alias = new Code("alias"), Far = new Code("return");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory PointFactory(bool half = false, string mode = "unavailable")
        {
            var root = Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Set("seats",JsonValue.Number("2"));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.output-ports"));
            root.Get("scenarios")[0].Get("settings").Get("sim.world-setup").Set("profile",JsonValue.Of("physical.port.start"));
            if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));
            var doc = Json.Parse(FrameworkProofContent.Read("physical-port-availability.json"));
            doc.Get("entities")[1].Get("components").Get("physical_availability_probe").Set("mode",JsonValue.Of(mode));
            return PointGame.Factory(definitions:root.ToString(),extraModules:new GameModule[]{new EndpointProofModule(),new OutputPortProofModule()},
                extraDeclarations:"[{\"id\":\"proof.pools\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\"}],\"documents\":[\"physical-port-availability.json\"]},{\"id\":\"proof.output-ports\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"proof.pools\",\"version\":\"1.0.0\"},{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"},{\"id\":\"sim.world-setup\",\"version\":\"1.0.0\"}],\"documents\":[]}]",
                extraDocuments:new Dictionary<string,string>{{"physical-port-availability.json",doc.ToString()}});
        }
        private static SimGame New(GameFactory f,string scenario="points") => f.NewGame(new NewGameRequest{Scenario=scenario,Seed=4733,Seats={Controller.Human,Controller.Human}});
        private static ResourceId Resource(SimGame g,string name="steel") { if(!g.Seat(0).View.TryFindResource(name,out var value))throw new InvalidOperationException("missing authored resource");return value; }
        private static EntityId Find(SimGame g,string id,bool point=false) => Entity(g,point?"point."+id:id);
        private static LogisticsView View(SimGame g,int seat=0)=>g.Seat(seat).View.Feature<LogisticsView>();
        private static List<WorkJobView> Jobs(SimGame g)=>PermanentRemovalTests.Jobs(g);
        private static bool Live(WorkJobView j)=>j.State==JobState.Open||j.State==JobState.Assigned||j.State==JobState.Paused;
        private static WorkJobView Job(SimGame g,int id)=>Jobs(g).Single(x=>x.Id==id);
        private static LogisticsOperationalSnapshot Facts(SimGame g,EntityId site,ResourceId resource,Code port=default,ResourceEndpointId? original=null)
        { Code code=View(g).ReadDiagnostics(site,resource,out var facts,port,original);if(!code.IsNone)throw new InvalidOperationException("actual diagnostic refused "+code);return facts; }
        private static void Until(SimGame g,SimGame twin,Func<bool> ready,string stage)
        {
            for(int i=0;i<1000&&!ready();i++)Step(g,twin);
            if(!ready())throw new InvalidOperationException(stage+" exceeds1000Updates clock="+g.Clock.StepIndex+" jobs="+string.Join(";",Jobs(g).Select(x=>x.Id+"/"+x.State+"/"+x.ToilIndex+"/"+x.Loaded+"/"+x.Blocker)));
        }
        private static void Refused(Check c,SimGame g,Command command,Code expected)
        {
            ulong hash=g.Hash();c.Equal(expected,g.Seat(0).Preview(command),"preview retains exact physical port blocker");c.Equal(hash,g.Hash(),"unavailable preview is pure");
            int sequence=g.Seat(0).Submit(command);Step(g);long cursor=0;var results=new List<CommandResult>();g.Seat(0).ReadResults(ref cursor,results);
            var result=results.Single(x=>x.Sequence==sequence);c.That(!result.Accepted&&result.Refusal==expected,"ordinary command refuses with identical typed blocker");
            c.Equal(0,Jobs(g).Count,"unavailable port allocates no physical job or claim");
        }
        private static void Receiving(Check c,bool half)
        {
            var f=PointFactory(half);var g=New(f);var idle=New(f);ulong isolated=idle.Hash();var r=Resource(g,"supply");var limits=new ReplayLimits(4*1024*1024,5000);using var recording=g.RecordReplay(limits);
            EntityId receiver=Entity(g,"receiving.site"),closed=Entity(g,"unavailable.site"),source=Entity(g,"source"),worker=Entity(g,"machine");
            foreach(bool incoming in new[]{true,false}) {
                var cmd=new HaulResourcesCommand{Source=incoming?source:closed,Destination=incoming?closed:receiver,SourcePort=incoming?Code.None:new Code("stock"),DestinationPort=incoming?new Code("stock"):Code.None,Worker=worker,Resource=r,Ceiling=Fix.FromInt(8),Priority=100};
                c.Equal(PhysicalAvailabilityProbe.Closed,View(g).PreviewHaul(cmd.Source,cmd.Destination,r,cmd.Ceiling,sourcePort:cmd.SourcePort,destinationPort:cmd.DestinationPort).Blocker,"actual source/destination preview exposes provider cause");
                Refused(c,g,cmd,PhysicalAvailabilityProbe.Closed);
            }
            c.Equal(PhysicalAvailabilityProbe.Closed,View(g).ReadDiagnostics(closed,r,out var absent,new Code("stock")),"private diagnostic retains named unavailable cause");
            c.That(absent==null,"unavailable store never becomes fabricated zero stock");
            c.Equal(Codes.NotOwned,View(g,1).ReadDiagnostics(closed,r,out absent,new Code("stock")),"foreign private query withholds actual unavailable cause");
            var command=new HaulResourcesCommand{Source=source,Destination=receiver,Worker=worker,Resource=r,Ceiling=Fix.FromInt(8),Priority=100};
            int id=checked((int)PermanentRemovalTests.Submit(c,g,command).Arg0);c.Equal(Fix.Zero,Facts(g,receiver,r).Stock,"incoming-only receiver is not instant delivery");
            var twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"incoming-only original receipt restores");
            Until(g,twin,()=>!Jobs(g).Any(x=>x.Id==id&&Live(x)),"receiving-only physical haul");
            c.Equal(Fix.FromInt(8),Facts(g,receiver,r).Stock,"incoming-only actual stock receives eight once");c.Equal(Fix.FromInt(12),Facts(g,source,r).Stock,"real source debit");
            c.Equal(Fix.Zero,Facts(g,worker,r).Stock,"actual cargo settles");
            c.Equal(HaulStatus.NoSupply,View(g).PreviewHaul(receiver,source,r,Fix.One).Status,"receiving-only port does not fabricate outgoing capability");
            c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"incoming-only settled state durable");
            c.Equal(isolated,idle.Hash(),"receiving-only sessions isolated");c.That(new ReplayVerifier().Verify(f,recording.Finish(),limits).Success,"typed refusals and receiving-only physical replay");
        }
        private static void Restoration(Check c,bool point,bool half)
        {
            var f=ScopedHaulGame.Factory(restoredDockDelivery:true,pointDockRecovery:point,editCommon:root=>{if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));});
            var g=New(f,point?"scoped-point-dock-restoration":"scoped-dock-restoration");var idle=New(f,point?"scoped-point-dock-restoration":"scoped-dock-restoration");ulong isolated=idle.Hash();
            var r=Resource(g);EntityId machine=Find(g,"dock.machine",point),target=Find(g,"restore.target",point),far=Find(g,"recovery.far",point);var limits=new ReplayLimits(4*1024*1024,6000);using var recording=g.RecordReplay(limits);
            PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand{Worker=Find(g,"dock.queue",point),Held=true});
            int funding=checked((int)PermanentRemovalTests.Submit(c,g,new HaulResourcesCommand{Source=Find(g,"source.warehouse",point),Destination=Find(g,"source.sink",point),
                SourcePort=new Code("stock"),DestinationPort=new Code("tank"),Worker=machine,Resource=r,Ceiling=Fix.FromInt(8),Priority=100}).Arg0);
            var twin=f.Load(g.Save());Until(g,twin,()=>Job(g,funding).Loaded==Fix.FromInt(8)&&Job(g,funding).ToilIndex==2,"physical cargo funding");
            PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand{Worker=machine,Held=true});
            PermanentRemovalTests.Submit(c,g,new ConfigureStockPolicyCommand{Store=target,Port=Alias,Resource=r,Change=LogisticsDemandChange.Replace(new LogisticsDemandSettings(true,Fix.FromInt(8),Fix.FromInt(8),Fix.FromInt(8),200,LogisticsReserveAccess.Protected))});
            PermanentRemovalTests.Submit(c,g,new ConfigureStockPolicyCommand{Store=far,Port=Far,Resource=r,Change=LogisticsDemandChange.Replace(new LogisticsDemandSettings(true,Fix.Zero,Fix.Zero,Fix.Zero,100,LogisticsReserveAccess.Protected))});
            PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand{Worker=machine,Held=false});twin=f.Load(g.Save());
            Until(g,twin,()=>Jobs(g).Any(x=>Live(x)&&x.Worker==machine&&x.DestinationEndpoint==First),"original named return");
            int id=Jobs(g).Single(x=>Live(x)&&x.Worker==machine).Id;
            if(point)Until(g,twin,()=>Job(g,id).Loaded.IsPositive&&Job(g,id).Loaded<Fix.FromInt(8),"real partial named delivery");
            Fix prefix=Facts(g,far,r,Far,First).Stock,remaining=Facts(g,machine,r).Stock;
            PermanentRemovalTests.Submit(c,g,new SelectResourcePortCommand{Entity=target,Port=Alias,Endpoint=Second});twin=f.Load(g.Save());
            Until(g,twin,()=>Job(g,id).State==JobState.Paused,"original physical binding unavailable");
            c.Equal(ResourcePortCodes.OriginalUnavailable,View(g).ReadDiagnostics(target,r,out var unavailable,Alias,First),"original physical diagnostic exposes exact typed loss");
            c.That(unavailable==null,"unknown original stock is not fabricated as empty");
            c.Equal(remaining,Facts(g,machine,r).Stock,"original real cargo retained");c.Equal(prefix,Facts(g,far,r,Far,First).Stock,"committed original prefix retained");
            c.Equal(remaining,Facts(g,far,r,Far,First).InboundPromised,"exact actual independent receipt retained");
            c.Equal(Fix.Zero,Facts(g,target,r,Alias).Stock,"current unrelated store gets no original material");c.Equal(Fix.Zero,Facts(g,target,r,Alias).InboundPromised,"current unrelated store gets no original receipt");
            var paused=Job(g,id);twin=f.Load(g.Save());for(int i=0;i<3;i++)Step(g,twin);c.Equal(paused.HandlingProgress,Job(g,id).HandlingProgress,"saved unavailable wait earns no handling");
            PermanentRemovalTests.Submit(c,g,new SelectResourcePortCommand{Entity=target,Port=Alias,Endpoint=First});twin=f.Load(g.Save());
            Until(g,twin,()=>Facts(g,machine,r).Stock.IsZero&&!Jobs(g).Any(Live),"same original physical restoration and departure");
            c.Equal(Fix.FromInt(8),Facts(g,far,r,Far,First).Stock,"restoration physically delivers eight once");c.Equal(Fix.Zero,Facts(g,far,r,Far,First).InboundPromised,"original receipts settle");
            c.Equal(Fix.Zero,Facts(g,far,r,Far,Second).Stock,"unrelated retained second store stays empty");
            c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"typed original recovery settled save");c.Equal(isolated,idle.Hash(),"named recovery sessions isolated");
            c.That(new ReplayVerifier().Verify(f,recording.Finish(),limits).Success,"typed original physical loss and restoration replay");
        }
        private static void Manufacturing(Check c,bool point)
        {
            var f=ResourcePortAvailabilityTests.Factory(point,false);var g=New(f,point?"scoped-point-dock-restoration":"scoped-dock-restoration");var r=Resource(g);var factory=Entity(g,"availability.factory");var limits=new ReplayLimits(4*1024*1024,6000);using var recording=g.RecordReplay(limits);
            PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand{Worker=Find(g,"dock.queue",point),Held=true});
            PermanentRemovalTests.Submit(c,g,new HaulResourcesCommand{Source=Find(g,"source.warehouse",point),Destination=factory,SourcePort=new Code("stock"),DestinationPort=new Code("raw"),Worker=Find(g,"dock.machine",point),Resource=r,Ceiling=Fix.FromInt(8),Priority=100});
            var twin=f.Load(g.Save());Until(g,twin,()=>Facts(g,factory,r,new Code("raw")).Stock==Fix.FromInt(8)&&!Jobs(g).Any(Live),"actual manufacturing feed");
            PermanentRemovalTests.Submit(c,g,new EnqueueTransformationCommand{Transformer=factory,Capability=new Code("process"),Transformation="availability.batch"});twin=f.Load(g.Save());
            TransformationSnapshot Current(){if(!g.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(factory,new Code("process"),out var p))throw new InvalidOperationException("actual process absent");return p;}
            Until(g,twin,()=>Current().QueueCount==0,"whole guarded production");
            c.Equal(Fix.FromInt(4),Facts(g,factory,r,new Code("raw")).Stock,"one input and three timed utility consumed from original owner");c.Equal(Fix.One,Facts(g,factory,r,new Code("output")).Stock,"one complete real output");
            c.Equal(0,Current().Tools.Receipts.Count,"actual reusable tool releases");c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"whole production with broader inlet resolution durable");
            c.That(new ReplayVerifier().Verify(f,recording.Finish(),limits).Success,"physical feed and whole named-port manufacturing replay");
        }
        private static void Negative(Check c,string mode)
        {
            var f=PointFactory(mode:mode);var g=New(f);var idle=New(f);ulong isolated=idle.Hash();var resource=Resource(g,"supply");var command=new HaulResourcesCommand{Source=Entity(g,"source"),Destination=Entity(g,"unavailable.site"),DestinationPort=new Code("stock"),Worker=Entity(g,"machine"),Resource=resource,Ceiling=Fix.One,Priority=100};
            c.Throws<InvalidOperationException>(()=>g.Seat(0).Preview(command),"malformed or mutating actual named binding faults "+mode);
            var fault=g.Fault;c.That(fault!=null,"first actual provider fault retained "+mode);if(mode=="throw")c.That(fault.Message.Contains("proof.physical-port-original-error"),"original provider exception retained");
            c.Throws<InvalidOperationException>(()=>g.Save(),"faulted physical resolution cannot save healthy "+mode);c.Throws<InvalidOperationException>(()=>g.Hash(),"faulted physical resolution cannot hash healthy "+mode);
            c.Throws<InvalidOperationException>(()=>View(g).ReadDiagnostics(EntityId.None,resource,out _),"faulted missing diagnostic refuses "+mode);
            c.That(ReferenceEquals(fault,g.Fault),"subsequent refusals do not replace first error "+mode);c.Equal(isolated,idle.Hash(),"faulted named provider session isolated "+mode);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual current physical availability identity");c.Equal(154,EngineInfo.SaveFormat,"no parallel availability stock/save owner");
            Receiving(c,false);Receiving(c,true);
            foreach(bool point in new[]{false,true})foreach(bool half in new[]{false,true})Restoration(c,point,half);
            Manufacturing(c,false);Manufacturing(c,true);
            foreach(string mode in new[]{"malformed","inlet-only","mutate","clock","throw"})Negative(c,mode);
        }
    }
}

