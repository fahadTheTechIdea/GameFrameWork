using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class PersistentTerrainCleanupTests
    {
        internal const string Case="Persistent terrain cleanup: independent tile repair supply and earned footprint publication";
        internal const string NativeCase="Persistent terrain cleanup: native tile repair supply and earned footprint publication";
        internal const string DepositionCase="Incident terrain deposition: independent delayed once-only addition and physical cleanup cycles";
        internal const string NativeDepositionCase="Incident terrain deposition: native delayed once-only addition and physical cleanup cycles";
        internal static void Register(){TestRunner.Add(Case,c=>Run(c,false));TestRunner.Add(NativeCase,c=>Run(c,true));
            TestRunner.Add(DepositionCase,c=>Deposition(c,false));TestRunner.Add(NativeDepositionCase,c=>Deposition(c,true));}
        private static SimGame New(GameFactory factory)=>factory.NewGame(new NewGameRequest{
            Scenario="independent-sensor",Seed=733,Seats={Controller.Human,Controller.Human}});
        private static EntityId Id(SimGame game,string name)=>game.Seat(0).Snapshot.Entities.Single(e=>e.Def==name).Id;
        private static IncidentLifecycleView Incident(SimGame game,EntityId id)
        {
            if(!game.Seat(0).View.Feature<IncidentsView>().TryGetLifecycle(id,new Code("leak"),out var facts))
                throw new InvalidOperationException("missing actual incident");return facts;
        }
        // Read-only protocol assertion over actual engine-owned samples; no driver writes or cleanup rules.
        private static Fix Contamination(SimGame game,EntityId id)
        {
            var bounds=((TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e=>e.Id==id).Spatial).Footprint;
            var metadata=game.World.Content.Get<ISpatialLayerDefinition>("sensor.contamination");
            var layer=TileSpatialGeometry.RequireSampleLayer(game.World,(ITileSpatialSpace)game.World.Space,metadata,true);
            Fix total=Fix.Zero;for(int y=bounds.Y;y<bounds.Bottom;y++)for(int x=bounds.X;x<bounds.Right;x++)total+=layer.GetSample(new TileCoord(x,y));
            return total;
        }
        private static Fix Stock(SimGame game)
        {
            Fix total=Fix.Zero;var facts=new List<Facet>();foreach(var e in game.Seat(0).Snapshot.Entities){
                game.Seat(0).View.Inspect(e.Id,facts);foreach(var f in facts)if(f.Key==Storage.FacetStock&&f.Subject=="steel")total+=f.Value;
            }return total;
        }
        private static void Step(Check c,SimGame game,SimGame twin=null)
        {
            game.Update();twin?.Update();if(game.Fault!=null||twin?.Fault!=null)throw new InvalidOperationException("cleanup fault "+game.Fault+"/"+twin?.Fault);
            if(twin!=null)c.Equal(game.Hash(),twin.Hash(),"original terrain/response/material/clock codecs continue identically");
        }
        private static void Until(Check c,SimGame game,SimGame twin,Func<bool> done,string stage)
        {
            for(int i=0;i<240&&!done();i++)Step(c,game,twin);
            if(!done())throw new InvalidOperationException(stage+" clock="+game.Clock.StepIndex);
        }
        private static SimGame Save(Check c,GameFactory factory,SimGame game)
        {var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"persistent contamination and actual response phase restore");return twin;}
        private static void Send(Check c,SimGame game,Command command)
        {
            ulong hash=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(command),"ordinary physical cleanup command admission");
            c.Equal(hash,game.Hash(),"preview changes no terrain/material/work");var result=TestKit.Order(game,0,command);
            c.That(result.Accepted,"ordinary engine command "+result.Refusal);if(!result.Accepted)throw new InvalidOperationException("cleanup command refused "+result.Refusal);
        }
        private static ActivityNodeView Cleanup(SimGame game,EntityId target)
        {
            var nodes=new List<ActivityNodeView>();var response=Incident(game,target);
            if(!game.Seat(0).View.Feature<WorkView>().TryCopyNodes(response.OrderId,nodes))throw new InvalidOperationException("missing actual cleanup graph");
            return nodes.Single(n=>n.Id=="cleanup");
        }
        internal static void Clean(Check c,GameFactory factory,SimGame game,ref SimGame twin,EntityId target)
        {
            Fix dirty=Contamination(game,target);int episode=Incident(game,target).Snapshot.Episode;
            game.Seat(0).View.TryFindResource("steel",out var steel);
            Send(c,game,new HaulResourcesCommand{Source=Id(game,"sensor.depot"),Destination=target,Worker=Id(game,"sensor.cart"),
                Resource=steel,Ceiling=Fix.FromInt(2),Priority=100});twin=Save(c,factory,game);
            Until(c,game,twin,()=>{var n=Cleanup(game,target);return n.Progress.WholeTicks>0&&n.Progress.WholeTicks<n.RequiredTicks;},"supplied positive-time cleanup after repair");
            var nodes=new List<ActivityNodeView>();game.Seat(0).View.Feature<WorkView>().TryCopyNodes(Incident(game,target).OrderId,nodes);
            c.Equal(NodeState.Done,nodes.Single(n=>n.Id=="repair").State,"authored repair dependency completes before cleanup");
            c.Equal(dirty,Contamination(game,target),"partial cleanup cannot clear either terrain cell early");
            var partial=Cleanup(game,target).Progress;twin=Save(c,factory,game);
            game.Clock.Pause();twin.Clock.Pause();ulong hash=game.Hash();Step(c,game,twin);
            c.Equal(hash,game.Hash(),"one paused clock freezes cleanup and sample values");game.Clock.Resume();twin.Clock.Resume();
            Send(c,game,new ConfigureWorkPolicyCommand{Worker=Id(game,"sensor.cart"),Settings=new WorkPolicySettings(false,1,
                new[]{new WorkerRoleId("sensor.inspect"),new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
            twin=Save(c,factory,game);for(int i=0;i<3;i++)Step(c,game,twin);
            c.Equal(partial,Cleanup(game,target).Progress,"disabled machine earns no extra cleanup work");
            c.Equal(dirty,Contamination(game,target),"disabled response retains persistent dirty state");
            Send(c,game,new ConfigureWorkPolicyCommand{Worker=Id(game,"sensor.cart"),Settings=new WorkPolicySettings(true,1,
                new[]{new WorkerRoleId("sensor.inspect"),new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
            twin=Save(c,factory,game);Until(c,game,twin,()=>!Incident(game,target).Snapshot.Active,"earned cleanup and original episode completion");
            c.Equal(Fix.Zero,Contamination(game,target),"complete footprint clears through original atomic layer owner");
            c.Equal(episode,Incident(game,target).Snapshot.Episode,"one original incident closes once");
        }
        private static void Deposition(Check c,bool native)
        {
            c.Throws<ContentException>(()=>New(IndependentTileWorkTests.Factory(nativeTiles:native,terrainDeposition:true,
                authorIncidents:json=>json.Get("tile_incident_propagation_rules")[0].Set("exposure_effect",JsonValue.Of("absent.effect")))),"missing exposure effect refuses before play");
            c.Throws<ContentException>(()=>New(IndependentTileWorkTests.Factory(nativeTiles:native,terrainDeposition:true,
                authorTerrainCleanup:json=>json.Get("tile_terrain_work_effects")[0].Set("operation",JsonValue.Of("multiply")))),"unknown terrain operation refuses before play");
            var blockedFactory=IndependentTileWorkTests.Factory(nativeTiles:native,terrainDeposition:true,
                authorTerrainCleanup:json=>json.Get("spatial_layers")[0].Set("value_type",JsonValue.Of("byte")),
                authorTerrainDeposition:json=>json.Get("tile_terrain_work_effects")[0].Set("value",JsonValue.Number("255")));
            var blocked=New(blockedFactory);var blockedSource=Id(blocked,"sensor.incident-source");var blockedTarget=Id(blocked,"sensor.worksite");
            Send(c,blocked,new RaiseIncidentCommand{Target=blockedSource,Source=new Code("leak"),Kind=new Code("sensor.fire")});
            for(int i=0;i<8;i++)Step(c,blocked);
            c.That(!Incident(blocked,blockedTarget).Snapshot.Active&&Incident(blocked,blockedTarget).Snapshot.Episode==0,"unrepresentable addition raises no target episode");
            c.Equal(Fix.FromInt(2),Contamination(blocked,blockedTarget),"failed whole-footprint addition publishes no prefix");
            c.Equal(Fix.FromInt(12),Stock(blocked),"blocked exposure consumes no response material");
            Code refusal=blocked.Seat(0).View.Feature<IncidentPropagationView>().ReadEligibility(blockedSource,new Code("spread"),blockedTarget,out var eligibility);
            c.Equal(Code.None,refusal,"engine calculates blocked exposure diagnostics");
            c.Equal(Codes.InvalidArgument,eligibility.Observation.Blocker,"native value overflow is a typed eligibility wait, not a fault");Save(c,blockedFactory,blocked);
            var factory=IndependentTileWorkTests.Factory(nativeTiles:native,terrainDeposition:true);var game=New(factory);var idle=New(factory);ulong isolated=idle.Hash();
            var source=Id(game,"sensor.incident-source");var target=Id(game,"sensor.worksite");
            c.That(!game.Modules.Contains("sim.tycoon"),"terrain exposure/cleanup requires no business module");
            var limits=new ReplayLimits(4*1024*1024,6000);using var recording=game.RecordReplay(limits);
            for(int episode=1;episode<=2;episode++){
                Fix previous=episode==1?Fix.FromInt(2):Fix.Zero;
                c.Equal(previous,Contamination(game,target),"original terrain state before new delayed exposure");
                Send(c,game,new RaiseIncidentCommand{Target=source,Source=new Code("leak"),Kind=new Code("sensor.fire")});
                var twin=Save(c,factory,game);var pending=new List<PendingExposureSnapshot>();
                Until(c,game,twin,()=>{
                    game.Seat(0).View.Feature<IncidentPropagationView>().TryRead(source,new Code("spread"),out var exposure);
                    return exposure.Pending.Count>0;
                },"original exposure owner schedules target");
                c.Equal(previous,Contamination(game,target),"scheduling does not deposit terrain early");
                game.Clock.Pause();twin.Clock.Pause();ulong paused=game.Hash();Step(c,game,twin);c.Equal(paused,game.Hash(),"one paused clock freezes pending deposition");
                game.Clock.Resume();twin.Clock.Resume();twin=Save(c,factory,game);
                Until(c,game,twin,()=>Incident(game,target).Snapshot.Active,"actual due exposure raises episode and publishes prepared terrain");
                c.Equal(episode,Incident(game,target).Snapshot.Episode,"one target episode per delivered original exposure");
                c.Equal(previous+Fix.FromInt(6),Contamination(game,target),"authored add3 at two cells retains prior terrain rather than replacing it");
                c.Equal(Fix.FromInt(12-4*(episode-1)),Stock(game),"deposition is terrain state, not generated or consumed material");
                ulong hash=game.Hash();c.Equal(Codes.InvalidArgument,game.Seat(0).Preview(new RaiseIncidentCommand{Target=target,Source=new Code("leak"),Kind=new Code("sensor.fire")}),"active episode cannot be raised twice");
                c.Equal(hash,game.Hash(),"duplicate raise preview is pure");twin=Save(c,factory,game);for(int i=0;i<5;i++)Step(c,game,twin);
                c.Equal(previous+Fix.FromInt(6),Contamination(game,target),"active/waiting episode cannot deposit repeatedly");
                Clean(c,factory,game,ref twin,source);Clean(c,factory,game,ref twin,target);
                c.Equal(Fix.Zero,Contamination(game,source)+Contamination(game,target),"physical work clears actual deposited terrain");
                c.Equal(Fix.FromInt(12-4*episode),Stock(game),"actual supplied repair/cleanup consumes authored amounts only");
            }
            Save(c,factory,game);c.Equal(isolated,idle.Hash(),"interleaved session receives no deposition or episode state");
            var replay=new ReplayVerifier().Verify(factory,recording.Finish(),limits);
            c.That(replay.Success&&replay.Game?.Hash()==game.Hash(),"ordinary two-cycle delayed deposition/cleanup replay "+replay.Failure+"/"+replay.Detail);
        }
        private static void Run(Check c,bool native)
        {
            foreach(Action<JsonValue> invalid in new Action<JsonValue>[] {
                json=>json.Get("tile_terrain_work_effects")[0].Set("layer",JsonValue.Of("missing.layer")),
                json=>{json.Get("spatial_layers")[0].Set("value_type",JsonValue.Of("byte"));json.Get("tile_terrain_work_effects")[0].Set("value",JsonValue.Number("0.5"));},
                json=>{json.Get("spatial_layers")[0].Set("resource",JsonValue.Of("steel"));json.Get("spatial_layers")[0].Set("resource_measure",JsonValue.Of("quantity"));}
            })c.Throws<ContentException>(()=>New(IndependentTileWorkTests.Factory(nativeTiles:native,terrainCleanup:true,authorTerrainCleanup:invalid)),
                "invalid/missing/resource-stock terrain effects refuse before play");
            var factory=IndependentTileWorkTests.Factory(nativeTiles:native,terrainCleanup:true);var game=New(factory);var idle=New(factory);ulong isolated=idle.Hash();
            c.That(!game.Modules.Contains("sim.tycoon"),"persistent cleanup needs no business module");
            var source=Id(game,"sensor.incident-source");var target=Id(game,"sensor.worksite");
            c.Equal(Fix.FromInt(24),Contamination(game,source),"authored source contamination belongs to original terrain layer");
            c.Equal(Fix.FromInt(24),Contamination(game,target),"authored target contamination persists independently of episode");
            c.Equal(Fix.FromInt(12),Stock(game),"finite authored response materials");
            var limits=new ReplayLimits(4*1024*1024,6000);using var recording=game.RecordReplay(limits);
            Send(c,game,new RaiseIncidentCommand{Target=source,Source=new Code("leak"),Kind=new Code("sensor.fire")});
            var twin=Save(c,factory,game);Until(c,game,twin,()=>Incident(game,target).Snapshot.Active,"normal delayed target incident");
            c.Equal(Fix.FromInt(48),Contamination(game,source)+Contamination(game,target),"incident enrollment does not silently clean terrain");
            Clean(c,factory,game,ref twin,source);c.Equal(Fix.FromInt(24),Contamination(game,target),"source cleanup does not clear another target");
            Clean(c,factory,game,ref twin,target);c.Equal(Fix.FromInt(8),Stock(game),"two repairs and two cleanups consume four actual supplied units");
            twin=Save(c,factory,game);for(int i=0;i<3;i++)Step(c,game,twin);
            c.Equal(Fix.Zero,Contamination(game,source)+Contamination(game,target),"clean state remains durable after response completion");
            c.Equal(isolated,idle.Hash(),"interleaved session has its own unchanged terrain/materials/incidents");
            var replay=new ReplayVerifier().Verify(factory,recording.Finish(),limits);
            c.That(replay.Success&&replay.Game?.Hash()==game.Hash(),"ordinary repair/cleanup/disabled-work/pause replay "+replay.Failure+"/"+replay.Detail);
        }
    }
}
