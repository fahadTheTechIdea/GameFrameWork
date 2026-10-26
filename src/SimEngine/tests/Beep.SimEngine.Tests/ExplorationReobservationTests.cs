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

namespace Beep.SimEngine.Tests
{
    // Public player inputs and immutable engine facts. No host freshness, sampling or progress logic.
    internal static class ExplorationReobservationTests
    {
        internal const string Case = "Exploration reobservation: expired history earns replacement dwell without premature timestamps";
        private const string Provider="sim.knowledge.facts", Reset="binding.reset-known", Retain="binding.retain-known";
        private static readonly Code Source=new Code("binding.contact"), Sight=new Code("sensor.sight");
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(bool independent,bool half,bool single=false)
        {
            var version=new ModuleVersion(1,0,0);
            return ExplorationBindingTests.Factory(new ExplorationBindingsModule(),independent,
                edit:doc=>{
                    doc.Remove("knowledge_facts");
                    if(single) {
                        var entries=doc.Get(independent?"independent_contact_profiles":"exploration_profiles")[0].Get("entries");
                        doc.Get(independent?"independent_contact_profiles":"exploration_profiles")[0].Set("entries",Json.Parse("["+entries[1].ToString()+"]"));
                    }
                },
                extraModules:new GameModule[]{new ProbeModule("proof.reobservation",version,new[]{new ModuleRequirement(ExplorationModule.ModuleId,version)})},
                extraDeclarations:new[]{Json.Parse("{\"id\":\"proof.reobservation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.exploration\",\"version\":\"1.0.0\"}],\"documents\":[\"exploration-reobservation.json\"]}")},
                extraFiles:new Dictionary<string,string>{["exploration-reobservation.json"]=FrameworkProofContent.Read("exploration-reobservation.json")},
                definitionEdit:doc=>{if(half)doc.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));});
        }
        private static SimGame New(GameFactory f)=>f.NewGame(new NewGameRequest{Scenario="points",Seed=9257,Seats={Controller.Human,Controller.Human}});
        private static EntityId Find(SimGame g,string id)=>g.Seat(0).Snapshot.Entities.Single(e=>e.Def==id).Id;
        private static ExplorationSnapshot Read(SimGame g)
        {
            var code=g.Seat(0).View.Feature<ExplorationView>().Read(Find(g,"sensor.machine"),Source,out var value);
            if(!code.IsNone)throw new InvalidOperationException("reobservation query "+code);return value;
        }
        private static ExplorationEntrySnapshot Entry(SimGame g,string id="a.reset")=>Read(g).Entries.Single(e=>e.Id==id);
        private static KnowledgeFreshnessSnapshot Knowledge(SimGame g,string subject,int seat=0)
        {
            var code=g.Seat(seat).View.Feature<KnowledgeView>().ReadFreshness(new PlayerId(seat),Provider,subject,out var value);
            if(!code.IsNone)throw new InvalidOperationException("reobservation knowledge "+code);return value;
        }
        private static long Value(KnowledgeFreshnessSnapshot reading)=>new JsonReader(reading.Observation.Data,"reading").Long("value_raw");
        private static void Step(SimGame g,SimGame twin=null)=>PermanentRemovalTests.Step(g,twin);
        private static void Until(SimGame g,SimGame twin,Func<bool> done,string stage)
        {
            for(int i=0;i<480&&!done();i++)Step(g,twin);
            if(!done())throw new InvalidOperationException(stage+" step="+g.Clock.StepIndex+" entries="+
                string.Join(";",Read(g).Entries.Select(e=>e.Id+"/"+e.Progress+"/"+e.Known+"/"+e.NeedsObservation+"/"+e.Blocker)));
        }
        private static CommandResult Order(Check c,SimGame g,Command command)
        {
            ulong before=g.Hash();c.Equal(Code.None,g.Seat(0).Preview(command),"ordinary reobservation preview");c.Equal(before,g.Hash(),"preview earns no observation");
            var result=TestKit.Order(g,0,command);c.That(result.Accepted,"ordinary reobservation command accepted");return result;
        }
        private static void Control(Check c,SimGame g,bool enabled)=>Order(c,g,new ConfigureOperationCommand{
            Entity=Find(g,"sensor.machine"),Operation=Sight,Enabled=enabled,Target=Fix.FromInt(3)});
        private static void Initial(Check c,GameFactory f,SimGame g)
        {
            c.That(Read(g).Entries.All(e=>!e.Known&&e.NeedsObservation&&e.Knowledge==null),"unknown history is explicit, not fabricated freshness");
            Order(c,g,new SetWorkerHeldCommand{Worker=Find(g,"sensor.machine"),Held=true});
            g.Seat(0).View.TryFindResource("supply",out var supply);
            int route=checked((int)Order(c,g,new AddRouteCommand{From=Find(g,"source"),To=Find(g,"sensor.machine"),Resource=supply,
                Settings=new LogisticsRouteSettings(true,Fix.One,70)}).Arg0);
            var twin=f.Load(g.Save());Until(g,twin,()=>TransformationContentContractTests.Stock(g,"sensor.machine")==Fix.One,"physical sensing supply");
            Order(c,g,new RemoveRouteCommand{Route=route});Until(g,null,()=>PermanentRemovalTests.Jobs(g).All(j=>j.State==JobState.Done||j.State==JobState.Cancelled),"supplier departure");
            Order(c,g,new SetWorkerHeldCommand{Worker=Find(g,"machine"),Held=true});
            Order(c,g,new SetWorkerHeldCommand{Worker=Find(g,"sensor.machine"),Held=false});
            Order(c,g,new PostOrderCommand{Plan="binding.travel",Target=Find(g,"destination"),Priority=70});
            twin=f.Load(g.Save());Until(g,twin,()=>((PointSnapshot)g.Seat(0).Snapshot.Entities.Single(e=>e.Def=="sensor.machine").Spatial).Position==Fix.FromInt(10),"physical reading contact");
            Until(g,twin,()=>Read(g).Entries.All(e=>e.Known),"initial earned readings");c.Equal(g.Hash(),twin.Hash(),"initial reading dwell saves deterministically");
        }
        private static void Physical(Check c,bool independent,bool half)
        {
            var f=Factory(independent,half);var g=New(f);var idle=New(f);ulong isolated=idle.Hash();
            var limits=new ReplayLimits(4*1024*1024,4000);
            using(var recorder=g.RecordReplay(limits)) {
                Initial(c,f,g);var initial=Read(g);var first=Knowledge(g,Reset);var firstRetain=Knowledge(g,Retain);
                c.That(initial.Entries.All(e=>e.Known&&!e.NeedsObservation&&e.Knowledge.IsFresh),"fresh history waits without scheduling another scan");
                c.That(!g.Modules.Contains(TycoonModule.ModuleId),"reobservation requires no budget or business");
                c.That(ReferenceEquals(g.World.Services.Get<ICompanyKnowledge>(),g.World.Services.Get<IKnowledgeFreshness>()),"one original ledger owns membership and freshness");
                c.Equal(first.Observation.ObservedTick,Entry(g).Knowledge.Observation.ObservedTick,"exploration embeds original immutable freshness fact");
                ulong before=g.Hash();Read(g);Read(g);c.Equal(before,g.Hash(),"freshness previews never advance progress or timestamps");
                Order(c,g,new WritePointLayerCommand{Layer="point.terrain",Position=Fix.FromInt(10),Value=Fix.FromInt(9)});
                c.Equal(first.Observation.ObservedTick,Knowledge(g,Reset).Observation.ObservedTick,"changed actual terrain does not refresh historical timestamp");
                c.Equal(Value(first),Value(Knowledge(g,Reset)),"old reading is not silently replaced by current world value");
                c.That(Value(first)!=Fix.FromInt(9).Raw,"authored world change genuinely differs from first sample");
                Control(c,g,false);var twin=f.Load(g.Save());
                Until(g,twin,()=>Read(g).Entries.All(e=>e.NeedsObservation),"authored lifetime expiry");
                var stale=Read(g);c.That(stale.Entries.All(e=>e.Known&&e.NeedsObservation&&!e.Knowledge.IsFresh&&e.Blocker==ExplorationCodes.NoSensor),
                    "expiry retains known history but demands new coverage and work");
                c.That(stale.Entries.All(e=>e.Progress.Equals(WorkProgress.Zero)),"disabled expiry earns no replacement prefix");
                long at=g.Clock.StepIndex,age=Entry(g).Knowledge.AgeSteps;g.Clock.Pause();twin.Clock.Pause();Step(g,twin);
                c.That(g.Clock.StepIndex==at&&Entry(g).Knowledge.AgeSteps==age,"one paused clock freezes both age and replacement");g.Clock.Resume();twin.Clock.Resume();
                Control(c,g,true);twin=f.Load(g.Save());Until(g,twin,()=>Entry(g).Progress.WholeTicks>=3,"partial replacement dwell");
                c.That(Entry(g).Known&&Entry(g).NeedsObservation,"known stale data and unfinished replacement coexist");
                c.Equal(first.Observation.ObservedTick,Knowledge(g,Reset).Observation.ObservedTick,"partial replacement earns no new observation time");
                Control(c,g,false);long retained=Entry(g,"b.retain").Progress.WholeTicks;
                c.That(Entry(g).Progress.Equals(WorkProgress.Zero)&&retained>0,"replacement uses original reset and retain interruption");
                twin=f.Load(g.Save());Step(g,twin);c.Equal(retained,Entry(g,"b.retain").Progress.WholeTicks,"saved disabled replacement retains earned work only");
                Control(c,g,true);twin=f.Load(g.Save());Until(g,twin,()=>Entry(g).Progress.WholeTicks==Entry(g).RequiredSteps-1,"final unearned replacement interval");
                c.Equal(first.Observation.ObservedTick,Knowledge(g,Reset).Observation.ObservedTick,"last partial interval still exposes only old reading");
                twin=f.Load(g.Save());Until(g,twin,()=>Knowledge(g,Reset).Observation.ObservedTick>first.Observation.ObservedTick
                    &&Knowledge(g,Retain).Observation.ObservedTick>firstRetain.Observation.ObservedTick,"completed replacement readings");
                var replaced=Knowledge(g,Reset);c.Equal(Fix.FromInt(9).Raw,Value(replaced),"full replacement samples actual changed layer value");
                c.Equal(first.Observation.LearnedTick,replaced.Observation.LearnedTick,"latest replacement preserves original learned coordinate");
                c.Equal(g.Hash(),twin.Hash(),"partial-to-refreshed saved continuation identical");
                Control(c,g,false);Order(c,g,new ShareKnowledgeCommand{Recipient=new PlayerId(1),Policy="binding.share"});
                var shared=Knowledge(g,Reset,1);c.Equal(replaced.Observation.ObservedTick,shared.Observation.ObservedTick,"sharing retains actual observation time, not receipt time");
                c.Equal(Fix.FromInt(9).Raw,Value(shared),"shared latest reading has actual committed data");
                Order(c,g,new ForgetKnowledgeCommand{Policy="binding.forget"});
                c.That(!Entry(g).Known&&Knowledge(g,Reset,1).Observation.ObservedTick==shared.Observation.ObservedTick,"forgetting is company-local and preserves recipient history");
                Control(c,g,true);twin=f.Load(g.Save());Until(g,twin,()=>Read(g).Entries.All(e=>e.Known),"earned rediscovery after forgetting");
                c.That(Knowledge(g,Reset).Observation.LearnedTick>first.Observation.LearnedTick,"forget and re-earn establishes a new learned coordinate");
                c.Equal(Fix.One,TransformationContentContractTests.Stock(g,"sensor.machine"),"reobservation invents no sensor stock");
                c.Equal(Fix.FromInt(19),TransformationContentContractTests.Stock(g,"source"),"single actual delivery remains conserved");
                c.That(initial.Entries.All(e=>e.Knowledge.IsFresh)&&stale.Entries.All(e=>!e.Knowledge.IsFresh),"retained snapshots keep their original age/current facts");
                c.Equal(isolated,idle.Hash(),"interleaved session borrows no expiry or observation state");
                c.That(new ReplayVerifier().Verify(f,recorder.Finish(),limits).Success,"physical contact changes refresh controls and sharing replay identically");
            }
        }
        private static void Failure(Check c,bool loaded,bool throws)
        {
            var f=Factory(true,false,single:true);var g=New(f);Initial(c,f,g);var first=Knowledge(g,Reset);
            Control(c,g,false);Until(g,null,()=>Entry(g).NeedsObservation,"negative fixture expiry");
            Control(c,g,true);Until(g,null,()=>Entry(g).Progress.WholeTicks==Entry(g).RequiredSteps-1,"negative final replacement interval");
            if(loaded)g=f.Load(g.Save());var twin=f.Load(g.Save());var retained=Entry(g);long time=g.Clock.StepIndex;
            var published=g.Seat(0).Snapshot;var primary=new InvalidOperationException("proof.reobservation-original");int calls=0;
            var subject=g.World.Content.Get<PointReadingDef>(Reset);var target=g.World;
            subject.ObservationProbe=world=>{if(!ReferenceEquals(world,target))return;calls++;if(throws)throw primary;((GameClock)world.Clock).Pause();};
            try{g.Update();}finally{subject.ObservationProbe=null;}
            c.That(calls==1&&g.Fault!=null,"actual final subject callback faults before publishing replacement");
            c.Equal("observation.provider",g.Fault?.Phase,"same original subject observation boundary owns fault");
            if(throws)c.Equal("InvalidOperationException: proof.reobservation-original",g.Fault?.Message,"original callback cause is not masked");
            c.That(g.Clock.StepIndex==time&&g.Seat(0).Snapshot.Tick==published.Tick,"failed replacement advances no time or healthy snapshot");
            c.That(retained.Known&&retained.NeedsObservation&&retained.Knowledge.Observation.ObservedTick==first.Observation.ObservedTick,
                "last safe immutable history has no refreshed prefix");
            c.Throws<InvalidOperationException>(()=>g.Save(),"faulted replacement cannot checkpoint healthy state");
            c.Throws<InvalidOperationException>(()=>g.Hash(),"faulted replacement cannot certify healthy hash");
            Step(twin);c.That(Knowledge(twin,Reset).Observation.ObservedTick>first.Observation.ObservedTick&&twin.Fault==null,"separate saved session completes original replacement");
        }
        private static void Run(Check c)
        {
            foreach(bool independent in new[]{false,true})foreach(bool half in new[]{false,true})Physical(c,independent,half);
            foreach(bool loaded in new[]{false,true})foreach(bool throws in new[]{false,true})Failure(c,loaded,throws);
        }
    }
}
