using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    // Explicit master-file authoring, public commands/Update/facts/saves and assertions only.
    internal static class SpatialPresenceCompositionTests
    {
        internal const string Case="Spatial presence composition: unused geometry contracts require actual providers before initialization";
        internal static void Register()=>TestRunner.Add(Case,Run);
        internal static GameFactory Factory(SpatialPresenceCompositionModule proof, string mode="valid", bool independent=false,Action<JsonValue> edit=null)
        {
            var world=Json.Parse(FrameworkProofContent.Read("spatial-composition-world.json"));
            var master=Json.Parse(FrameworkProofContent.Read("spatial-composition-master.json"));
            world.Remove("absent_spatial_spaces");world.Remove("spatial_composition_probes");
            var scenario=world.Get("scenarios")[0];
            scenario.Set("modules",Json.Parse("[\"sim.world-setup\",\"sim.tile-navigation\",\"proof.spatial-presence-composition\"]"));
            var declaration=master.Get("modules")[3];declaration.Set("id",JsonValue.Of(proof.Id));declaration.Remove("requires");
            if(independent){world.Set("independent_tile_spaces",world.Get("tile_spaces"));world.Remove("tile_spaces");}
            if(mode!="valid")
            {
                scenario.Set("modules",Json.Parse("[\"sim.world-setup\",\"proof.spatial-presence-composition\"]"));
                scenario.Get("settings").Get(WorldSetupModule.ModuleId).Set("profile",JsonValue.Of(mode=="local"?"spatial.local-only":"spatial.empty"));
                world.Get("entities")[0].Get("components").Remove("mover");
                if(mode=="local")scenario.Set("space",JsonValue.Null());
                else if(mode=="missing-provider"||mode=="wrong-provider-id"||mode=="wrong-geometry"||mode=="missing-layers"||mode.StartsWith("layer-",StringComparison.Ordinal)||mode.StartsWith("revision-",StringComparison.Ordinal))
                {
                    var spaces=world.Get(independent?"independent_tile_spaces":"tile_spaces");
                    spaces[0].Set("mode",JsonValue.Of(mode));world.Set("unavailable_tile_spaces",spaces);
                    world.Remove(independent?"independent_tile_spaces":"tile_spaces");
                    if(mode=="missing-layers"||mode.StartsWith("layer-",StringComparison.Ordinal))
                    {
                        world.Set("units",Json.Parse("[{\"id\":\"presence.unit\",\"dimension\":\"count\",\"scale\":{\"numerator\":1,\"denominator\":1},\"discrete\":false}]"));
                        world.Set("spatial_layers",Json.Parse("[{\"id\":\"presence.layer\",\"space\":\"spatial.tiles\",\"type\":\"terrain\",\"value_type\":\"fixed\",\"visibility\":\"public\",\"unit\":\"presence.unit\"}]"));
                    }
                }
                else if(mode=="clock"||mode=="throw"||mode=="throw-after-clock")
                    world.Get("entities").Add(Json.Parse("{\"id\":\"presence.probe\",\"kind\":\"entity\",\"components\":{\"admission_presence\":{\"space\":\"spatial.tiles\",\"mode\":\""+mode+"\"}}}"));
            }
            edit?.Invoke(world);
            var definition=new GameDefinition("presence-composition",new GameModule[]{new MotionModule(),new TileNavigationModule(),new WorldSetupModule(),proof});
            var files=new PreloadedFileSource().Add("manifest.json",master.ToString()).Add("spatial-composition-world.json",world.ToString())
                .Add("spatial-composition-routes.json",FrameworkProofContent.Read("spatial-composition-routes.json"));
            return new GameFactory(definition,ContentSet.Load(files,definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory)=>factory.NewGame(new NewGameRequest{Scenario="spatial-composition",Seed=622,Seats={Controller.Human}});
        private static TileSpatialSnapshot Position(SimGame game)=>(TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(x=>x.Def=="spatial.cart").Spatial;
        private static void Step(Check c,SimGame game,SimGame twin)
        {game.Update();twin.Update();if(game.Fault!=null||twin.Fault!=null)throw new InvalidOperationException("presence physical update failed "+game.Fault+"/"+twin.Fault);c.Equal(game.Hash(),twin.Hash(),"physical saved twins share authoritative geometry/time");}
        private static void Run(Check c)
        {
            foreach(bool independent in new[]{false,true})
            {
                foreach(string mode in new[]{"missing-provider","wrong-provider-id","wrong-geometry","missing-layers"})
                {
                    var probe=new SpatialPresenceCompositionModule();c.Throws<ContentException>(()=>New(Factory(probe,mode,independent)),"unused native presence rejects "+mode);
                    c.Equal(0,probe.Initialize.Initializations,"missing actual provider fails before Initialize");
                }
                foreach(string mode in new[]{"clock","throw","throw-after-clock"})
                {
                    var probe=new SpatialPresenceCompositionModule();Exception refusal=null;
                    try{New(Factory(probe,mode,independent));}catch(Exception error){refusal=error;}
                    c.That(refusal is InvalidOperationException,"unused direct-contract observation refuses "+mode);
                    if(mode!="clock")c.Equal("proof.presence-first-error",refusal?.Message,"first callback error is not masked by later purity checks");
                    c.Equal(0,probe.Initialize.Initializations,"invalid direct admission never initializes");
                }
                var emptyProof=new SpatialPresenceCompositionModule();var emptyFactory=Factory(emptyProof,"unused-valid",independent);var empty=New(emptyFactory);
                c.That(emptyProof.Initialize.Initializations==1&&empty.Seat(0).Snapshot.Entities.Count==0,"valid unused definitions admit without inventing instances");
                c.Equal(empty.Hash(),emptyFactory.Load(empty.Save()).Hash(),"empty valid spatial session restores through original admission");
                var localFactory=Factory(new SpatialPresenceCompositionModule(),"local",independent);var local=New(localFactory);
                c.That(local.Observer.Space==null&&local.Seat(0).Snapshot.Entities.Single().Spatial==null,"unselected geometry remains optional and creates no map fallback");
                var localTwin=localFactory.Load(local.Save());Step(c,local,localTwin);
                Physical(c,independent);
            }
            // Existing independent point capabilities validate their own geometry, not tile services.
            var pointFactory=PlanetXProductionTests.Factory();var point=PlanetXProductionTests.New(pointFactory);
            c.That(point.Observer.Space is PointWorldSnapshot&&point.Modules.All(x=>x!=TileNavigationModule.ModuleId),"non-tile presence composes with its actual independent provider");
            var limits=new ReplayLimits(4*1024*1024,1000);var replay=point.RecordReplay(limits);var command=PlanetXProductionTests.Queue(point,"smelter","space.smelt.planetx");
            ulong before=point.Hash();c.Equal(Code.None,point.Seat(0).Preview(command),"ordinary point production admission");c.Equal(before,point.Hash(),"point preview changes no state");
            point.Seat(0).Submit(command);var pointTwin=pointFactory.Load(point.Save());
            for(int i=0;i<256&&PlanetXProductionTests.Stock(point,"smelter","alloy")!=Fix.FromInt(6);i++)PlanetXProductionTests.Step(point,pointTwin);
            c.Equal(Fix.FromInt(6),PlanetXProductionTests.Stock(point,"smelter","alloy"),"independent point presence executes actual physical production");
            c.That(new ReplayVerifier().Verify(pointFactory,replay.Finish(),limits).Success,"point presence/physical work replay");
        }
        private static void Physical(Check c,bool independent)
        {
            var proof=new SpatialPresenceCompositionModule();var factory=Factory(proof,independent:independent);var game=New(factory);var isolated=New(factory);ulong isolatedHash=isolated.Hash();
            c.That(proof.Initialize.Initializations==2&&game.Observer.Space!=null,"actual native/independent authored tile provider initializes");
            var actor=game.Seat(0).Snapshot.Entities.Single(x=>x.Def=="spatial.cart").Id;var command=new MoveVehicleCommand{Vehicle=actor,To=new TileCoord(7,2)};
            var limits=new ReplayLimits(2*1024*1024,512);var replay=game.RecordReplay(limits);ulong before=game.Hash();
            c.Equal(Code.None,game.Seat(0).Preview(command),"ordinary move admitted by engine");c.Equal(before,game.Hash(),"move preview pure");game.Seat(0).Submit(command);var twin=factory.Load(game.Save());Step(c,game,twin);
            var retained=Position(game);c.That(retained.Tile!=command.To&&retained.Position!=FixVec2.Centre(new TileCoord(1,2)),"normal update advances physical travel without teleport");
            twin=factory.Load(game.Save());game.Clock.Pause();c.Equal(0,game.Update(),"paused clock earns no travel");game.Clock.Resume();
            int updates=0;bool crossed=false;for(;updates<96&&Position(game).Position!=FixVec2.Centre(command.To);updates++){Step(c,game,twin);crossed|=Position(game).Tile==new TileCoord(3,2);}
            c.That(updates<96&&!crossed,"bounded native travel respects occupied obstacle");c.Equal(FixVec2.Centre(command.To),Position(game).Position,"actual spatial presence reaches exact destination");
            c.That(retained.Position!=Position(game).Position,"retained snapshot is detached");c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"physical terminal state restores");
            c.That(new ReplayVerifier().Verify(factory,replay.Finish(),limits).Success,"normal movement commands replay after presence admission");c.Equal(isolatedHash,isolated.Hash(),"interleaved geometry session remains isolated");
        }
    }
}
