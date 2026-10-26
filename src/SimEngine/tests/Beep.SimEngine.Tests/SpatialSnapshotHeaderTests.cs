using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Public player intent and retained publications only; actual space/presence providers own capture.
    internal static class SpatialSnapshotHeaderTests
    {
        internal const string Case = "Spatial snapshot headers: actual space identity and retained private publications reject malformed captures";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(string worldFault=null,string entityFault=null,bool half=false) {
            var root=Json.Parse(PointGame.Definitions);root.Get("scenarios")[0].Set("seats",JsonValue.Number("2"));
            if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));
            if(worldFault!=null)root.Get("point_spaces")[0].Set("snapshot_fault",JsonValue.Of(worldFault));
            if(entityFault!=null) {
                var presence=root.Get("entities")[2].Get("components").Get("point_presence");
                presence.Set("snapshot_fault",JsonValue.Of(entityFault));presence.Set("snapshot_fault_at",JsonValue.Number("2"));
            }
            return PointGame.Factory(definitions:root.ToString());
        }
        private static SimGame New(GameFactory f,string scenario="points")=>f.NewGame(new NewGameRequest{
            Scenario=scenario,Seed=7331,Seats={Controller.Human,Controller.Human}});
        private static void Headers(Check c,GameSnapshot snapshot,string space) {
            c.Equal(space,snapshot.Space?.Space,"publication identifies actual selected space");
            if(space!=null)c.That(snapshot.Space.Version>=0,"viewer-local world revision is nonnegative");
            foreach(var e in snapshot.Entities)if(e.Spatial!=null)c.Equal(space,e.Spatial.Space,"entity snapshot identifies actual selected space");
        }
        private static void Healthy(Check c,bool half) {
            var f=Factory(half:half);var g=New(f);var idle=New(f);ulong untouched=idle.Hash();var limits=new ReplayLimits(4*1024*1024,6000);using var recording=g.RecordReplay(limits);
            var initial=g.Seat(0).Snapshot;Headers(c,initial,"field.points");c.Equal(0,initial.Space.Version,"independent viewer revision is not forced to global observation counter");
            var machine=Entity(g,"machine");var target=Entity(g,"destination");
            PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="inspect",Target=target,Priority=30});
            var twin=f.Load(g.Save());var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<1000&&watch.ElapsedMilliseconds<20000&&!g.Seat(0).View.Feature<Beep.SimEngine.Views.WorkView>().TryReadActivityCompletion("activity:inspect",target,out _);i++)Step(g,twin);
            if(!g.Seat(0).View.Feature<Beep.SimEngine.Views.WorkView>().TryReadActivityCompletion("activity:inspect",target,out _))
                throw new InvalidOperationException("physical snapshot header proof exceeds1000Updates/20s clock="+g.Clock.StepIndex+" fault="+g.Fault);
            Headers(c,g.Snapshot,"field.points");Headers(c,g.Seat(0).Snapshot,"field.points");Headers(c,g.Seat(1).Snapshot,"field.points");
            c.That(((PointSnapshot)g.Seat(0).Snapshot.Entities.Single(x=>x.Id==machine).Spatial).Position>Fix.Zero,"actual point movement remains published");
            c.Equal(Fix.Zero,((PointSnapshot)initial.Entities.Single(x=>x.Id==machine).Spatial).Position,"retained point payload remains detached");
            c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"normal spatial header publication survives save/load");c.Equal(untouched,idle.Hash(),"normal spatial sessions isolated");
            c.That(new ReplayVerifier().Verify(f,recording.Finish(),limits).Success,"normal public point work and snapshot headers replay");
        }
        private static void Cutoff(Check c,SimGame g,SimGame idle,ulong untouched,GameSnapshot before,GameSnapshot beforeForeign,string error) {
            var fault=g.Fault;c.That(fault!=null,"malformed capture records first engine fault");
            if(fault==null)throw new InvalidOperationException("missing malformed capture fault "+error);
            if(error!=null)c.That(fault.Message.Contains(error),"original capture exception retained");
            var after=g.Seat(0).Snapshot;var foreign=g.Seat(1).Snapshot;
            c.Equal(GameState.Faulted,after.State,"actual snapshot publication marks faulted state");
            c.Equal(before.Tick,after.Tick,"faulted snapshot retains last healthy publication time");
            c.Equal(before.Entities.Count,after.Entities.Count,"no malformed new entity prefix published");
            c.That(ReferenceEquals(before.Space,after.Space),"last healthy world snapshot retained, not malformed provider result");
            c.That(ReferenceEquals(beforeForeign.Space,foreign.Space),"each viewer retains its own healthy space projection");
            c.That(before.Entities.Select(x=>x.Id).SequenceEqual(after.Entities.Select(x=>x.Id)),"retained visible identities unchanged");
            c.Equal(0,after.Changed.Count+after.Spawned.Count+after.Removed.Count,"fault epoch invents no private deltas");
            foreach(var old in before.Entities) {
                var current=after.Entities.Single(x=>x.Id==old.Id);
                c.That(ReferenceEquals(old.Spatial,current.Spatial),"last healthy entity payload retained");
            }
            long stopped=g.Clock.StepIndex;g.Update();c.Equal(stopped,g.Clock.StepIndex,"faulted header session cannot advance clock");
            c.Throws<InvalidOperationException>(()=>g.Hash(),"faulted header cannot hash healthy");c.Throws<InvalidOperationException>(()=>g.Save(),"faulted header cannot save healthy");
            var facts=new List<EntityView>();c.Throws<InvalidOperationException>(()=>g.Seat(0).View.CopyEntities(facts),"faulted live snapshot query refuses");
            c.That(ReferenceEquals(fault,g.Fault),"first header fault not replaced");c.Equal(untouched,idle.Hash(),"faulted snapshot session isolated");
        }
        private static void WorldFailure(Check c,string mode) {
            var f=Factory(worldFault:mode);var g=New(f);var idle=New(f);ulong untouched=idle.Hash();
            var before=g.Seat(0).Snapshot;var foreign=g.Seat(1).Snapshot;ulong pure=g.Hash();
            var command=new SetPointAccessCommand{Open=false};c.Equal(Code.None,g.Seat(0).Preview(command),"normal authored access control preview");c.Equal(pure,g.Hash(),"access preview does not trigger malformed capture");
            g.Seat(0).Submit(command);g.Update();Cutoff(c,g,idle,untouched,before,foreign,mode=="throw"?"proof.original-world-snapshot-error":null);
        }
        private static void EntityFailure(Check c,string mode) {
            var f=Factory(entityFault:mode);var g=New(f);var idle=New(f);ulong untouched=idle.Hash();var target=Entity(g,"destination");
            PermanentRemovalTests.Submit(c,g,new PostOrderCommand{Plan="inspect",Target=target,Priority=30});
            GameSnapshot before=null,foreign=null;
            var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<1000&&watch.ElapsedMilliseconds<20000&&g.Fault==null;i++){before=g.Seat(0).Snapshot;foreign=g.Seat(1).Snapshot;g.Update();}
            if(g.Fault==null)throw new InvalidOperationException("entity header probe exceeds1000Updates/20s clock="+g.Clock.StepIndex+" mode="+mode);
            Cutoff(c,g,idle,untouched,before,foreign,mode=="throw"?"proof.original-entity-snapshot-error":null);
        }
        private static void Run(Check c) {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current snapshot contract identity");c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"current authored protocol, no historical snapshot save pin");
            Healthy(c,false);Healthy(c,true);
            var native=New(ScopedHaulGame.Factory(restoredDockDelivery:true),"scoped-dock-restoration");Headers(c,native.Snapshot,"haul.tiles");
            var plain=New(TransformerTests.Factory(),"process");Headers(c,plain.Snapshot,null);c.That(plain.Snapshot.Entities.All(x=>x.Spatial==null),"nonspatial engine keeps explicit absent spatial facts");
            foreach(string mode in new[]{"null","foreign","blank","negative","throw","mutate"})WorldFailure(c,mode);
            foreach(string mode in new[]{"null","foreign","blank","throw","mutate"})EntityFailure(c,mode);
        }
    }
}
