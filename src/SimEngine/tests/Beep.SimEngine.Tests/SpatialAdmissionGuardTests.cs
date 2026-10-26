using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Composed JSON, public player commands/time/facts and early-refusal assertions only.
    internal static class SpatialAdmissionGuardTests
    {
        internal const string Case="Spatial admission: selected providers layers and callbacks refuse before initialization";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static SimGame New(GameFactory factory)=>factory.NewGame(new NewGameRequest{Scenario="spatial-composition",Seed=629,Seats={Controller.Human}});
        private static TileSpatialSnapshot Position(SimGame game)=>(TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(x=>x.Def=="spatial.cart").Spatial;
        private static void Input(Check c,SimGame game,Command command)
        {
            ulong hash=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(command),"public command admission");
            c.Equal(hash,game.Hash(),"pure preview changes no geometry");game.Seat(0).Submit(command);Step(game);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current observed provider build");c.Equal(154,EngineInfo.SaveFormat,"same original spatial/game state codecs");
            foreach(bool direct in new[]{false,true}){Refusals(c,direct);Physical(c,direct);Nonspatial(c,direct);}
            Point(c);
        }
        private static void Refusals(Check c,bool direct)
        {
            foreach(string mode in new[]{"missing-provider","wrong-provider-id","wrong-geometry","missing-layers","layer-null","layer-missing",
                "layer-clock","layer-state","layer-throw","layer-clock-throw","layer-state-throw"})
            {
                var proof=new SpatialPresenceCompositionModule();Exception refused=null;
                try{New(SpatialPresenceCompositionTests.Factory(proof,mode,direct));}catch(Exception error){refused=error;}
                bool callback=mode=="layer-clock"||mode=="layer-state"||mode.EndsWith("-throw",StringComparison.Ordinal);
                c.That(callback?refused is InvalidOperationException:refused is Beep.SimEngine.Content.ContentException,"actual selected provider/layer refuses "+mode);
                c.Equal(0,proof.Initialize.Initializations,"refusal before any Initialize "+mode);
                if(mode.EndsWith("-throw",StringComparison.Ordinal))
                    c.Equal("proof.layer-admission-first-error",refused?.Message,"original layer callback error is not finally-masked "+mode);
            }
        }
        private static void Physical(Check c,bool direct)
        {
            var proof=new SpatialPresenceCompositionModule();var factory=SpatialPresenceCompositionTests.Factory(proof,independent:direct);
            var game=New(factory);var idle=New(factory);ulong isolated=idle.Hash();
            c.That(proof.Initialize.Initializations==2&&game.Observer.Space!=null,"actual native/direct geometry initializes once per new session");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId),"geometry and movement have no business requirement");
            var limits=new ReplayLimits(2*1024*1024,1000);var replay=game.RecordReplay(limits);
            var command=new MoveVehicleCommand{Vehicle=Entity(game,"spatial.cart"),To=new TileCoord(7,2)};
            Input(c,game,command);var first=Position(game);c.That(first.Position!=FixVec2.Centre(command.To),"real travel is not an admission-side teleport");
            var twin=factory.Load(game.Save());long at=game.Clock.StepIndex;ulong hash=game.Hash();
            game.Clock.Pause();twin.Clock.Pause();Step(game,twin);c.That(at==game.Clock.StepIndex&&hash==game.Hash(),"sole paused clock earns no travel or metadata state");
            game.Clock.Resume();twin.Clock.Resume();
            int updates=0;for(;updates<96&&Position(game).Position!=FixVec2.Centre(command.To);updates++)Step(game,twin);
            if(Position(game).Position!=FixVec2.Centre(command.To))throw new InvalidOperationException("native/direct travel stalled "+game.Clock.StepIndex+" "+game.Fault);
            c.That(updates>0&&first.Position!=Position(game).Position,"actual later arrival and detached opening pose");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"same geometry/route/clock codecs retain terminal state");
            c.Equal(isolated,idle.Hash(),"interleaved spatial session is unchanged");
            c.That(new ReplayVerifier().Verify(factory,replay.Finish(),limits).Success,"normal native/direct physical input replay after admission");
        }
        private static void Nonspatial(Check c,bool direct)
        {
            var proof=new SpatialPresenceCompositionModule();var factory=SpatialPresenceCompositionTests.Factory(proof,"local",direct);var game=New(factory);
            c.That(proof.Initialize.Initializations==1&&game.Observer.Space==null&&game.Seat(0).Snapshot.Entities.Single().Spatial==null,
                "unselected spatial authoring imposes no provider or default map");
            var twin=factory.Load(game.Save());Step(game,twin);c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"nonspatial work/time persists without geometry");
        }
        private static void Point(Check c)
        {
            var factory=PlanetXProductionTests.Factory();var game=PlanetXProductionTests.New(factory);var idle=PlanetXProductionTests.New(factory);ulong isolated=idle.Hash();
            c.That(game.Observer.Space is PointWorldSnapshot&&!game.Modules.Contains(TileNavigationModule.ModuleId),"independent point admission selects no tile fallback");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId),"point manufacture remains resource-only");
            var limits=new ReplayLimits(4*1024*1024,2000);var replay=game.RecordReplay(limits);
            Input(c,game,PlanetXProductionTests.Queue(game,"smelter","space.smelt.planetx"));
            c.Equal(Fix.Zero,PlanetXProductionTests.Stock(game,"smelter","alloy"),"ordinary enqueue grants no immediate product");
            var twin=factory.Load(game.Save());long at=game.Clock.StepIndex;ulong hash=game.Hash();
            game.Clock.Pause();twin.Clock.Pause();Step(game,twin);c.That(at==game.Clock.StepIndex&&hash==game.Hash(),"point provider shares the same paused clock");
            game.Clock.Resume();twin.Clock.Resume();
            int updates=0;for(;updates<256&&PlanetXProductionTests.Stock(game,"smelter","alloy")!=Fix.FromInt(6);updates++)Step(game,twin);
            if(PlanetXProductionTests.Stock(game,"smelter","alloy")!=Fix.FromInt(6))throw new InvalidOperationException("point physical manufacture stalled "+game.Clock.StepIndex+" "+game.Fault);
            c.That(updates>1,"original worker hauling and timed manufacture required updates");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"point provider and physical work save through existing codecs");
            c.Equal(isolated,idle.Hash(),"interleaved point session remains isolated");
            c.That(new ReplayVerifier().Verify(factory,replay.Finish(),limits).Success,"independent point physical manufacture replay after guarded admission");
        }
    }
}
