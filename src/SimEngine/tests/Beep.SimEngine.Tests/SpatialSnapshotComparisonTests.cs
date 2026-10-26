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
    // Actual point content and public player input/publications only; comparer lives in engine/mod.
    internal static class SpatialSnapshotComparisonTests
    {
        internal const string Case="Spatial snapshot comparison: observed symmetric stable delta matching and first-fault retention";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(string mode,bool half) {
            var root=Json.Parse(PointGame.Definitions);root.Get("scenarios")[0].Set("seats",JsonValue.Number("2"));
            if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));
            var presence=root.Get("entities")[2].Get("components").Get("point_presence");
            presence.Set("comparison_fault",JsonValue.Of(mode));presence.Set("comparison_fault_at",JsonValue.Number("2"));
            return PointGame.Factory(definitions:root.ToString());
        }
        private static SimGame New(GameFactory f,string scenario="points")=>f.NewGame(new NewGameRequest{Scenario=scenario,Seed=7531,Seats={Controller.Human,Controller.Human}});
        private static EntityView Fact(GameSnapshot snapshot,EntityId id)=>snapshot.Entities.Single(x=>x.Id==id);
        private static Fix Position(EntityView fact)=>fact.Spatial is PointComparisonSnapshot first?first.Value.Position:
            fact.Spatial is PointOtherComparisonSnapshot second?second.Value.Position:((PointSnapshot)fact.Spatial).Position;
        private static bool Complete(SimGame g,EntityId target)=>g.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:inspect",target,out _);
        private static void Healthy(Check c,string mode,bool half) {
            var f=Factory(mode,half);var g=New(f);var idle=New(f);ulong untouched=idle.Hash();EntityId machine=Entity(g,"machine"),target=Entity(g,"destination");
            var initial=g.Seat(0).Snapshot;var limits=new ReplayLimits(4*1024*1024,6000);using var recording=g.RecordReplay(limits);
            PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="inspect",Target=target,Priority=30});
            var twin=f.Load(g.Save());bool transition=false;int moved=0;
            var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<1000&&watch.ElapsedMilliseconds<20000&&!Complete(g,target);i++) {
                var before=g.Seat(0).Snapshot;Step(g,twin);var after=g.Seat(0).Snapshot;
                c.Equal(before.Revision,after.PreviousRevision,"original delta baseline retained");
                c.That(after.Revision>before.Revision&&after.Viewer==before.Viewer,"original private publication stream ordered");
                EntityView a=Fact(before,machine),b=Fact(after,machine);
                if(Position(a)!=Position(b)) {c.That(after.Changed.Any(x=>x.Id==machine),"real physical spatial change appears in original Changed delta");moved++;}
                if(a.Spatial.GetType()!=b.Spatial.GetType()) {transition=true;c.That(after.Changed.Any(x=>x.Id==machine),"shape-type transition does not invoke cross-type callback");}
            }
            if(!Complete(g,target))throw new InvalidOperationException("healthy snapshot comparison exceeds1000Updates/20s clock="+g.Clock.StepIndex+" mode="+mode+" fault="+g.Fault);
            c.That(moved>0,"normal same-shape comparison retains actual movement");
            if(mode=="shape-transition")c.That(transition,"actual authored alternate shape was published");
            c.Equal(Fix.Zero,Position(Fact(initial,machine)),"retained initial payload stays detached");
            c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"normal delta comparison publication durable");c.Equal(untouched,idle.Hash(),"normal comparison sessions isolated");
            c.That(new ReplayVerifier().Verify(f,recording.Finish(),limits).Success,"public physical movement and comparison deltas replay");
        }
        private static void Negative(Check c,string mode,bool half) {
            var f=Factory(mode,half);var g=New(f);var idle=New(f);ulong untouched=idle.Hash();var target=Entity(g,"destination");
            PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="inspect",Target=target,Priority=30});
            GameSnapshot before=null,foreign=null;
            var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<1000&&watch.ElapsedMilliseconds<20000&&g.Fault==null;i++){before=g.Seat(0).Snapshot;foreign=g.Seat(1).Snapshot;g.Update();}
            var fault=g.Fault;c.That(fault!=null,"invalid spatial comparison faults "+mode);
            if(fault==null)throw new InvalidOperationException("comparison probe exceeds1000Updates/20s clock="+g.Clock.StepIndex+"/"+mode);
            if(mode=="throw")c.That(fault.Message.Contains("proof.original-snapshot-comparison-error"),"original comparison exception retained");
            var after=g.Seat(0).Snapshot;c.Equal(GameState.Faulted,after.State,"faulted delta publication explicit");
            c.Equal(before.Tick,after.Tick,"fault epoch retains last healthy time");
            c.Equal(0,after.Changed.Count+after.Spawned.Count+after.Removed.Count,"invalid comparison publishes no partial deltas");
            c.That(ReferenceEquals(before.Space,after.Space)&&ReferenceEquals(foreign.Space,g.Seat(1).Snapshot.Space),"each viewer retains its own healthy map");
            foreach(var old in before.Entities)c.That(ReferenceEquals(old.Spatial,Fact(after,old.Id).Spatial),"last healthy spatial payload retained");
            long stopped=g.Clock.StepIndex;g.Update();c.Equal(stopped,g.Clock.StepIndex,"faulted comparison cannot advance clock");
            c.Throws<InvalidOperationException>(()=>g.Hash(),"invalid comparison cannot hash healthy");c.Throws<InvalidOperationException>(()=>g.Save(),"invalid comparison cannot save healthy");
            var facts=new List<EntityView>();c.Throws<InvalidOperationException>(()=>g.Seat(0).View.CopyEntities(facts),"faulted live comparison projection refuses");
            c.That(ReferenceEquals(fault,g.Fault),"comparison first fault retained");c.Equal(untouched,idle.Hash(),"invalid comparison session isolated");
        }
        private static void Run(Check c) {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current spatial comparison identity");c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"current authored protocol, no historical comparison save pin");
            foreach(bool half in new[]{false,true}){Healthy(c,"normal",half);Healthy(c,"shape-transition",half);}
            foreach(string mode in new[]{"nonreflexive","asymmetric","stateful","throw","mutate"})foreach(bool half in new[]{false,true})Negative(c,mode,half);
            var native=New(ScopedHaulGame.Factory(restoredDockDelivery:true),"scoped-dock-restoration");Step(native);c.That(native.Fault==null&&native.Snapshot.Space!=null,"native spatial matching remains valid");
            var plain=New(TransformerTests.Factory(),"process");Step(plain);c.That(plain.Fault==null&&plain.Snapshot.Space==null,"nonspatial primitive matching remains valid");
        }
    }
}
