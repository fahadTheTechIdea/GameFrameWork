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
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // JSON intent, normal player input/Update, public detached facts and assertions only.
    internal static class TilePresenceBindingTests
    {
        internal const string Case="Tile presence bindings: independent JSON metadata drives original hauling rollout and construction";
        internal static void Register()=>TestRunner.Add(Case,Run);
        internal static GameFactory Factory(TilePresenceBindingModule proof,bool independent=true,Action<JsonValue> edit=null)
        {
            var root=Json.Parse(Colony.Defs);if(independent)
                for(int i=0;i<root.Get("entities").Count;i++){
                    var parts=root.Get("entities")[i].Get("components");var presence=parts.Get("tile_presence");
                    if(presence!=null){parts.Set("independent_tile_presence",presence);parts.Remove("tile_presence");}
                }
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(proof.Id));edit?.Invoke(root);
            return Colony.Factory(root.ToString(),additionalModules:new GameModule[]{proof},moduleDeclarations:new[]{"{\"id\":\"proof.tile-presence-bindings\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.tile-navigation\",\"version\":\"1.0.0\"}],\"documents\":[]}"});
        }
        private static SimGame New(GameFactory factory)=>factory.NewGame(new NewGameRequest{Scenario="colony",Seed=663,Seats={Controller.Human,Controller.Human}});
        private static EntityView Actor(SimGame game,string prototype)=>game.Seat(0).Snapshot.Entities.Single(x=>x.Def==prototype&&x.Owner==new PlayerId(0));
        private static int Trucks(SimGame game)=>game.Seat(0).Snapshot.Entities.Count(x=>x.Def=="truck"&&x.Owner==new PlayerId(0));
        private static Fix Steel(SimGame game)
        {
            Fix total=Fix.Zero;foreach(var entity in game.Seat(0).Snapshot.Entities){var facts=new List<Facet>();game.Seat(0).View.Inspect(entity.Id,facts);foreach(var fact in facts)if(fact.Key==Storage.FacetStock&&fact.Subject=="steel")total+=fact.Value;}return total;
        }
        private static void Step(Check c,SimGame game,SimGame twin)
        {game.Update();twin.Update();if(game.Fault!=null||twin.Fault!=null)throw new InvalidOperationException("presence work fault "+game.Fault+"/"+twin.Fault);c.Equal(game.Hash(),twin.Hash(),"actual coordinate/resource/work saved twins");}
        private static void Until(Check c,SimGame game,SimGame twin,Func<bool> ready,string stage)
        {
            for(int i=0;i<768&&!ready();i++)Step(c,game,twin);
            if(!ready()){var orders=new List<OrderView>();game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);throw new InvalidOperationException(stage+" exceeded768Updates at="+game.Clock.StepIndex+" entities="+string.Join(";",game.Seat(0).Snapshot.Entities.Select(x=>x.Def+"/"+x.State))+" orders="+string.Join(";",orders.Select(x=>x.Plan+"/"+x.State)));}
        }
        private static SimGame Input(Check c,GameFactory factory,SimGame game,Command command)
        {ulong before=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(command),"normal player command admitted");c.Equal(before,game.Hash(),"engine preview pure");game.Seat(0).Submit(command);return factory.Load(game.Save());}
        private static void Run(Check c)
        {
            c.That(typeof(TilePresence).BaseType==typeof(Component),"original runtime no longer requires native definition class");
            foreach(bool independent in new[]{false,true})
            {
                foreach(string field in new[]{"space","width","height","blocks","movable"})
                    Refuse(c,independent,doc=>Parts(doc,independent).Remove(field),"missing authored "+field);
                foreach(string field in new[]{"width","height"})Refuse(c,independent,doc=>Parts(doc,independent).Set(field,JsonValue.Number("0")),"nonpositive "+field);
                Refuse(c,independent,doc=>Parts(doc,independent).Set("unknown",JsonValue.Of(true)),"strict unknown field");
                Refuse(c,independent,doc=>Parts(doc,independent).Set("space",JsonValue.Of("missing.space")),"missing actual tile space");
                var mobileProof=new TilePresenceBindingModule();
                c.That(New(Factory(mobileProof,independent,doc=>Parts(doc,independent).Set("movable",JsonValue.Of(true)))).Fault==null,
                    "actual native or independent positive mobile dimensions are not restricted to one tile");
                Physical(c,independent);
            }
            foreach(string mode in new[]{"width","height","space","oversized"})Refuse(c,true,doc=>{
                doc.Get("entities").Add(Json.Parse("{\"id\":\"presence.unused\",\"kind\":\"entity\",\"components\":{\"independent_tile_presence\":{\"space\":\"colony.tiles\",\"width\":1,\"height\":1,\"blocks\":true,\"movable\":true,\"proof_fault\":\""+mode+"\"}}}"));
            },"unused malformed direct "+mode);
            foreach(string mode in new[]{"clock","throw","throw-after-clock"})Fault(c,mode);
        }
        private static JsonValue Parts(JsonValue doc,bool independent)=>doc.Get("entities")[0].Get("components").Get(independent?"independent_tile_presence":"tile_presence");
        private static void Refuse(Check c,bool independent,Action<JsonValue> edit,string label)
        {var proof=new TilePresenceBindingModule();c.Throws<ContentException>(()=>New(Factory(proof,independent,edit)),label);c.Equal(0,proof.Initialize.Initializations,"invalid actual metadata rejects before Initialize");}
        internal static void Physical(Check c,bool independent)
        {
            var proof=new TilePresenceBindingModule();var factory=Factory(proof,independent);var game=New(factory);var isolated=New(factory);ulong isolatedHash=isolated.Hash();
            c.Equal(2,proof.Initialize.Initializations,"actual file/master registration initializes");var map=game.Seat(0).View.Feature<TileMapView>();ulong before=game.Hash();
            c.That(map.TryGetFootprint("depot",out var depot)&&depot.W==3&&depot.H==3&&map.TryGetFootprint("tower",out var tower)&&tower.W==2&&tower.H==2,"detached authored dimensions use actual contract");
            c.That(!map.TryGetFootprint("missing",out _),"missing content has no fabricated footprint");c.Equal(before,game.Hash(),"footprint reads pure");
            c.Equal(Fix.FromInt(50),Steel(game),"only authored company resources");var limits=new ReplayLimits(8*1024*1024,4000);var replay=game.RecordReplay(limits);
            int initial=Trucks(game);var command=new EnqueueTransformationCommand{Transformer=Actor(game,"factory").Id,Capability=new Code("transformation"),Transformation="build_truck",Home=Actor(game,"depot").Id};
            var twin=Input(c,factory,game,command);Step(c,game,twin);c.Equal(initial,Trucks(game),"queued manufacturing never instant-spawns vehicle");
            bool moved=false;var original=game.Seat(0).Snapshot.Entities.Where(x=>x.Def=="truck"&&x.Owner==new PlayerId(0)).ToDictionary(x=>x.Id,x=>((TileSpatialSnapshot)x.Spatial).Position);
            for(int i=0;i<768&&Trucks(game)==initial;i++){
                Step(c,game,twin);moved|=game.Seat(0).Snapshot.Entities.Where(x=>original.ContainsKey(x.Id)).Any(x=>((TileSpatialSnapshot)x.Spatial).Position!=original[x.Id]);
                if(i==12){twin=factory.Load(game.Save());ulong paused=game.Hash();game.Clock.Pause();c.Equal(0,game.Update(),"sole clock pauses physical presence");c.Equal(paused,game.Hash(),"paused geometry/resources unchanged");game.Clock.Resume();}
            }
            c.That(moved&&Trucks(game)==initial+1,"ordinary hauling and productive manufacturing roll out one actual vehicle within768Updates");
            var spawned=game.Seat(0).Snapshot.Entities.Single(x=>x.Def=="truck"&&x.Owner==new PlayerId(0)&&!original.ContainsKey(x.Id));var retained=(TileSpatialSnapshot)spawned.Spatial;
            c.That(retained.Footprint.W==1&&retained.Footprint.H==1&&retained.Movable,"manufactured output binds same native coordinate owner");c.Equal(Fix.FromInt(40),Steel(game),"physical manufacture consumes authored ten once");
            var build=new ConstructCommand{Def="tower",Placement=Json.Parse("{\"space\":\"colony.tiles\",\"x\":12,\"y\":16}"),Priority=70};twin=Input(c,factory,game,build);Step(c,game,twin);
            c.That(game.Seat(0).Snapshot.Entities.Any(x=>x.Def=="site:tower")&&!game.Seat(0).Snapshot.Entities.Any(x=>x.Def=="tower"),"construction creates physical site not finished building");
            var site=Actor(game,"site:tower");var siteFacts=(TileSpatialSnapshot)site.Spatial;twin=factory.Load(game.Save());
            Until(c,game,twin,()=>game.Seat(0).Snapshot.Entities.Any(x=>x.Def=="tower"),"physically supplied staged construction");
            var built=Actor(game,"tower");c.That(built.Id!=site.Id&&!game.Seat(0).Snapshot.Entities.Any(x=>x.Id==site.Id),"original construction replaces and retires actual site");
            var builtPosition=(TileSpatialSnapshot)built.Spatial;c.That(builtPosition.Tile==new TileCoord(12,16)&&builtPosition.Footprint.W==2&&siteFacts.Tile==builtPosition.Tile,"construction uses exact authored actual footprint and retained detached site");
            c.Equal(Fix.FromInt(10),Steel(game),"thirty physical construction materials consumed once, no duplicate stock");c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"finished coordinate/occupancy/material state restores without regrant");
            c.That(new ReplayVerifier().Verify(factory,replay.Finish(),limits).Success,"ordinary hauling/manufacture/construction presence commands replay");c.Equal(isolatedHash,isolated.Hash(),"interleaved session has no coordinate/cargo/project leakage");
            c.That(!game.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(Actor(game,"factory").Id,new Code("transformation"),out _),"foreign company receives no private production state");
        }
        private static void Fault(Check c,string mode)
        {
            var proof=new TilePresenceBindingModule();var factory=Factory(proof);var isolated=New(factory);var game=New(factory);ulong other=isolated.Hash();proof.Arm("depot",mode);
            Exception refusal=null;try{game.Seat(0).View.Feature<TileMapView>().TryGetFootprint("depot",out _);}catch(Exception error){refusal=error;}
            c.That(refusal is InvalidOperationException,"actual metadata callback refuses "+mode);if(mode!="clock")c.Equal("proof.tile-presence-first-error",refusal?.Message,"first error preserved");
            c.That(game.Fault!=null,"original world latches footprint observation fault");c.Throws<InvalidOperationException>(()=>game.Save(),"faulted preview cannot save healthy state");
            c.Throws<InvalidOperationException>(()=>game.Seat(0).View.Feature<TileMapView>().TryGetFootprint("missing",out _),"faulted missing preview publishes no success/empty prefix");
            c.Equal(other,isolated.Hash(),"negative callback leaves other session untouched");
        }
    }
}
