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
    // A simulated player only. Normal content/provider contracts and the engine own all gameplay.
    internal static class OriginalPortRecoveryTests
    {
        internal const string Case = "Original port recovery: partial unload, saved unavailable wait, restoration and cancellation";
        private static readonly Code Alias = new Code("alias"), Far = new Code("return");
        private static readonly ResourceEndpointId First = new ResourceEndpointId("proof.pools",9000000001),
            Second = new ResourceEndpointId("proof.pools",9000000002), Fourth = new ResourceEndpointId("proof.pools",9000000004);
        internal static void Register() => TestRunner.Add(Case,Run);
        private static EntityId Find(SimGame g,string prototype) => g.Seat(0).Snapshot.Entities.Single(e=>e.Def==prototype).Id;
        private static ResourceId Steel(SimGame g)
        { if(!g.Seat(0).View.TryFindResource("steel",out var r))throw new InvalidOperationException("missing steel");return r; }
        private static LogisticsView Logistics(SimGame g,int seat=0) => g.Seat(seat).View.Feature<LogisticsView>();
        private static List<WorkJobView> Jobs(SimGame g)
        { var rows=new List<WorkJobView>();g.Seat(0).View.Feature<WorkView>().CopyJobs(rows);return rows; }
        private static bool Live(WorkJobView j) => j.State==JobState.Open||j.State==JobState.Assigned||j.State==JobState.Paused;
        private static WorkJobView Job(SimGame g,int id) => Jobs(g).Single(j=>j.Id==id);
        private static LogisticsOperationalSnapshot Facts(SimGame g,string site,Code port=default,ResourceEndpointId? original=null)
        {
            Code refusal=Logistics(g).ReadDiagnostics(Find(g,site),Steel(g),out var f,port,original);
            if(!refusal.IsNone)throw new InvalidOperationException("original port facts "+site+"/"+refusal);return f;
        }
        private static LogisticsOperationalSnapshot Original(SimGame g) => Facts(g,"recovery.far",Far,First);
        private static Fix Cargo(SimGame g) => Facts(g,"haul.machine").Stock;
        private static void Step(SimGame g,SimGame twin=null)
        {
            g.Update();twin?.Update();
            if(g.Fault!=null||twin?.Fault!=null||twin!=null&&g.Hash()!=twin.Hash())
                throw new InvalidOperationException("original port Update "+g.Tick+"/"+g.Fault+"/"+twin?.Fault);
        }
        private static void Until(SimGame g,Func<bool> done,string stage,SimGame twin=null)
        {
            for(int i=0;i<1000&&!done();i++)Step(g,twin);
            if(!done())throw new InvalidOperationException(stage+" at "+g.Tick+" jobs="+
                string.Join(";",Jobs(g).Select(j=>j.Id+"/"+j.State+"/"+j.ToilIndex+"/"+j.Loaded+"/"+j.DestinationEndpoint+"/"+j.Blocker)));
        }
        private static long Command(Check c,SimGame g,Command command)
        {
            ulong before=g.Hash();c.Equal(Code.None,g.Seat(0).Preview(command),"ordinary player command preview");
            c.Equal(before,g.Hash(),"preview pure");var result=TestKit.Order(g,0,command);
            c.That(result.Accepted,"native command "+result.Refusal);
            if(!result.Accepted||g.Fault!=null)throw new InvalidOperationException("original port command "+result.Refusal+"/"+g.Fault);
            return result.Arg0;
        }
        private static void Band(Check c,SimGame g,string site,Code port,int amount,int priority)
            =>Command(c,g,new ConfigureStockPolicyCommand{Store=Find(g,site),Port=port,Resource=Steel(g),
                Change=LogisticsDemandChange.Replace(new LogisticsDemandSettings(true,Fix.FromInt(amount),Fix.FromInt(amount),
                    Fix.FromInt(amount),priority,LogisticsReserveAccess.Protected))});
        private static void Held(Check c,SimGame g,bool held)
            =>Command(c,g,new SetWorkerHeldCommand{Worker=Find(g,"haul.machine"),Held=held});
        private static void Select(Check c,SimGame g,ResourceEndpointId endpoint)
            =>Command(c,g,new SelectResourcePortCommand{Entity=Find(g,"binding.target"),Port=Alias,Endpoint = new ResourceEndpointId("proof.pools", endpoint.Identity)});
        private static SimGame Save(Check c,GameFactory f,SimGame g)
        { var twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"original receipt/access/cargo checkpoint");return twin; }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"original access recovery protocol");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"current authored state layout, no compatibility path");
            var f=ScopedHaulGame.Factory(originalPortLoss:true);
            Scenario(c,f,true,false);Scenario(c,f,false,false);Scenario(c,f,false,true);
        }
        private static void Scenario(Check c,GameFactory f,bool partial,bool cancel)
        {
            var g=f.NewGame(new NewGameRequest{Scenario="scoped-original-port-loss",Seed=997,Seats={Controller.Human,Controller.Human}});
            var recording=g.RecordReplay(new ReplayLimits(4*1024*1024,6000));
            c.That(!g.Modules.Contains("sim.tycoon"),"lost access recovery is resource-only game logic");
            c.That(!g.Seat(0).View.Feature<InventoryView>().TryGetCapacity(Find(g,"binding.target"), Code.None,out _),"no local inventory fallback");
            int funding=checked((int)Command(c,g,new HaulResourcesCommand{Source=Find(g,"source.warehouse"),Destination=Find(g,"source.sink"),
                SourcePort=new Code("stock"),DestinationPort=new Code("tank"),Resource=Steel(g),Ceiling=Fix.FromInt(8),Worker=Find(g,"haul.machine"),Priority=100}));
            Until(g,()=>Job(g,funding).Loaded==Fix.FromInt(8)&&Job(g,funding).ToilIndex==2,"physical complete pickup");
            Held(c,g,true);
            c.That(!Jobs(g).Any(j=>j.Id==funding&&Live(j)),"player interruption ends original execution");
            c.Equal(Fix.FromInt(8),Cargo(g),"all physically loaded material retained");
            Band(c,g,"binding.target",Alias,8,200);Band(c,g,"recovery.far",Far,partial?8:3,100);Held(c,g,false);
            Until(g,()=>Jobs(g).Any(j=>Live(j)&&!j.Source.IsValid&&j.DestinationEndpoint==First),"native named return");
            int id=Jobs(g).Single(Live).Id;
            c.Equal(Fix.FromInt(8),Original(g).InboundPromised,"one exact original full receipt");
            c.Equal(Alias,Job(g,id).DestinationPort,"actual named delivery binding");
            if(partial)Until(g,()=>Job(g,id).Loaded.IsPositive&&Job(g,id).Loaded<Fix.FromInt(8),"physical partial original unload");
            Fix prefix=Original(g).Stock,remaining=Cargo(g);
            c.Equal(Fix.FromInt(8),prefix+remaining,"only physical unloading moves original stock");
            if(partial)c.That(prefix.IsPositive&&remaining.IsPositive,"partial leg genuinely committed before access loss");
            var twin=Save(c,f,g);
            Select(c,g,Second);
            c.Equal(Second,Facts(g,"binding.target",Alias).Endpoint,"ordinary routing selection now accesses another actual store");
            c.Equal(Fix.Zero,Facts(g,"binding.target",Alias).Stock,"current unrelated store receives no old material");
            c.Equal(Fix.Zero,Facts(g,"binding.target",Alias).InboundPromised,"current unrelated store inherits no original claim");
            c.That(g.Seat(0).Snapshot.Entities.Any(e=>e.Id==Find(g,"binding.target")),"physical site survives access loss");
            Code refusal=Logistics(g).ReadDiagnostics(Find(g,"binding.target"),Steel(g),out _,Alias,First);
            c.That(!refusal.IsNone,"explicit lost original access is unavailable rather than current substitution");
            if(partial)
            {
                Until(g,()=>Job(g,id).DestinationEndpoint==Fourth,"complete remaining cargo replacement");
                c.Equal(remaining,Job(g,id).Loaded,"same job keeps only real remaining cargo");
                c.Equal(Find(g,"haul.machine"),Job(g,id).Worker,"same actual carrier survives partial original access loss");
                c.Equal(prefix,Original(g).Stock,"replanning never rewinds or repeats the committed original prefix");
                c.Equal(prefix,Job(g,id).UnloadingBaseline,"already delivered original prefix remains authoritative");
                c.Equal(Fix.Zero,Original(g).InboundPromised,"replacement releases surviving original book without physical access");
                c.Equal(remaining,Facts(g,"recovery.far",Far).InboundPromised,"exact residual capacity claimed, not original full eight");
                c.Equal(Fix.Zero,Facts(g,"recovery.far",Far).Stock,"replacement is not instant delivery");
            }
            else
            {
                Until(g,()=>Job(g,id).Blocker==HaulRecoveryCodes.DestinationUnavailable,"native lost-access whole-cargo wait");
                c.Equal(JobState.Paused,Job(g,id).State,"lost access is a gameplay wait, not a provider fault");
                c.Equal(HaulRecoveryStage.WaitingForDestination,Job(g,id).RecoveryStage,"immutable recovery stage");
                c.Equal(Fix.FromInt(8),Original(g).InboundPromised,"independent original receipt survives inaccessible physical port");
                c.Equal(Fix.Zero,Facts(g,"recovery.far",Far).InboundPromised,"too-small destination never steals partial promise");
                var progress=Job(g,id).HandlingProgress;twin=Save(c,f,g);
                for(int i=0;i<4;i++)Step(g,twin);
                c.Equal(progress,Job(g,id).HandlingProgress,"saved unavailable wait earns no handling time");
                c.Equal(Fix.FromInt(8),Cargo(g),"saved wait preserves actual cargo");
                if(cancel)
                {
                    Held(c,g,true);c.That(!Jobs(g).Any(j=>j.Id==id&&Live(j)),"ordinary player cancellation ends stranded job");
                    c.Equal(Fix.Zero,Original(g).InboundPromised,"cancellation releases exact inaccessible original receipt");
                    c.Equal(Fix.FromInt(8),Cargo(g),"cancellation never discards cargo");
                    Band(c,g,"recovery.far",Far,8,100);Held(c,g,false);
                    Until(g,()=>Jobs(g).Any(j=>Live(j)&&!j.Source.IsValid&&j.DestinationEndpoint==Fourth),"new ordinary residual return");
                    c.That(Jobs(g).Single(Live).Id!=id,"cancelled job never silently resumes");
                }
                else
                {
                    Select(c,g,First);
                    c.Equal(id,Jobs(g).Single(Live).Id,"restored original access resumes same native job");
                    c.Equal(First,Job(g,id).DestinationEndpoint,"restoration retains original claim and target");
                    c.Equal(Fix.FromInt(8),Original(g).InboundPromised,"restoration does not reacquire or duplicate receipt");
                }
            }
            twin=Save(c,f,g);
            Until(g,()=>Cargo(g).IsZero&&!Jobs(g).Any(Live),"normal movement, timed unloading and departure",twin);
            c.Equal(Fix.Zero,Original(g).InboundPromised,"no original claim leak");
            c.Equal(Fix.Zero,Facts(g,"recovery.far",Far).InboundPromised,"no replacement claim leak");
            c.Equal(Fix.FromInt(8),Original(g).Stock+Facts(g,"recovery.far",Far).Stock,"eight actual cargo units arrive exactly once");
            c.Equal(partial?prefix:cancel?Fix.Zero:Fix.FromInt(8),Original(g).Stock,"original receives only its actually completed leg");
            c.Equal(partial?remaining:cancel?Fix.FromInt(8):Fix.Zero,Facts(g,"recovery.far",Far).Stock,"replacement receives only actual rerouted residual");
            c.Equal(Fix.Zero,Facts(g,"recovery.far",Far,Second).Stock,"unrelated selected store stays physically empty");
            c.Equal(Fix.Zero,Facts(g,"recovery.far",Far,Second).InboundPromised,"unrelated selected store has no leaked promises");
            c.Equal(Fix.FromInt(30),Facts(g,"source.warehouse").Stock+Original(g).Stock+Facts(g,"recovery.far",Far).Stock+Cargo(g),"local thirty-unit conservation");
            var privateRows=new List<LogisticsDispatchOffer>();Logistics(g,1).CopyQueue(privateRows);
            c.Equal(0,privateRows.Count,"operational queue remains company-private");
            c.Equal(g.Hash(),twin.Hash(),"saved physical continuation identity");
            c.That(new ReplayVerifier().Verify(f,recording.Finish(),new ReplayLimits(4*1024*1024,6000)).Success,"normal command replay");
        }
    }
}
