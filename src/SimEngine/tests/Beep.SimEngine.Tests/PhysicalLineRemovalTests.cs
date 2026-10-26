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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Ordinary player commands, master-listed content and immutable observations only.
    internal static class PhysicalLineRemovalTests
    {
        internal const string Case = "Physical line removal: authored machine work conserves split pools and saved cancellation";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Create(bool independent, Action<JsonValue> edit = null, bool lastTile = false, GameModule additionalModule = null,
            Action<JsonValue> editNetwork = null, Action<JsonValue> editDefinitions = null,
            IReadOnlyList<GameModule> extraModules=null,string extraDeclarations=null,IReadOnlyDictionary<string,string> extraDocuments=null, Action<JsonValue> editTopology=null)
        {
            var content = Json.Parse(FrameworkProofContent.Read("physical-line-removal.json"));
            if (independent) { content.Set("independent_line_effects", content.Get("line_effects")); content.Remove("line_effects"); }
            if (lastTile) content.Get("entity_setups")[0].Get("placement").Set("x", JsonValue.Number("10"));
            edit?.Invoke(content);
            var modules = new List<GameModule> { new PhysicalLineRemovalProofModule() };
            var declarations = Json.Parse("[{\"id\":\"proof.physical-line-removal\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"}],\"documents\":[\"physical-line-removal.json\"]}]");
            if (additionalModule != null)
            {
                modules.Add(additionalModule);
                declarations.Add(Json.Parse("{\"id\":\"proof.infrastructure-services\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
            }
            if(extraModules!=null)foreach(var module in extraModules)modules.Add(module);
            if(extraDeclarations!=null){var rows=Json.Parse(extraDeclarations);for(int i=0;i<rows.Count;i++)declarations.Add(rows[i]);}
            var documents=new Dictionary<string,string>{{"physical-line-removal.json",content.ToString()}};
            if(extraDocuments!=null)foreach(var row in extraDocuments)documents.Add(row.Key,row.Value);
            return NetworkSignalBindingTests.Create(new InitializeProbe(), out _, independent: false, edit: root => {
                if (!lastTile) {
                    root.Get("tile_layer_setups")[0].Set("width", JsonValue.Number("3"));
                    root.Get("tile_layer_setups")[1].Set("width", JsonValue.Number("3"));
                    root.Get("entity_setups")[1].Get("placement").Set("x", JsonValue.Number("13"));
                }
                editNetwork?.Invoke(root);
                root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("removal.site.start"));
                root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("removal.crane.start"));
            }, extraModules: modules,
            extraDeclarations: declarations.ToString(),
            extraDocuments:documents,editDefinitions:editDefinitions,editTopology:editTopology);
        }
        internal static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "colony", Seed = 1462 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request); }
        internal static WorkView Work(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        internal static PostOrderCommand Cut(SimGame game) => new PostOrderCommand { Plan = "removal.work", Target = Entity(game,"removal.site"), Priority = 100 };
        internal static List<OrderView> Orders(SimGame game)
        { var rows = new List<OrderView>(); Work(game).CopyOrders(rows); return rows; }
        internal static List<ActivityNodeView> Nodes(SimGame game, int id)
        { var rows = new List<ActivityNodeView>(); if (!Work(game).TryCopyNodes(id, rows)) throw new InvalidOperationException("missing removal graph"); return rows; }
        internal static int Tile(SimGame game, int x)
        {
            var rows = new List<int>();
            if (game.Seat(0).View.Feature<TileMapView>().ReadLayer("network.line", new TileRect(x,28,1,1), rows) != SpatialLayerReadStatus.Success)
                throw new InvalidOperationException("missing actual removal geometry");
            return rows.Single();
        }
        internal static List<NetworkPoolView> Pools(SimGame game)
        {
            var rows = new List<NetworkPoolView>();
            if (!game.Seat(0).View.Feature<InfrastructureView>().CopyPools("binding.main", rows).IsNone) throw new InvalidOperationException("missing pool facts");
            return rows;
        }
        internal static Fix Steel(SimGame game)
        {
            game.Seat(0).View.TryFindResource("steel", out var steel);
            Fix sum = Pools(game).SelectMany(p=>p.Stock).Where(p=>p.Resource==steel).Aggregate(Fix.Zero,(s,p)=>s+p.Amount);
            var facets = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game,"binding.source"), facets);
            return sum + facets.Where(f=>f.Key==Storage.FacetStock&&f.Subject=="steel").Aggregate(Fix.Zero,(s,f)=>s+f.Value);
        }
        internal static void Reach(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            for (int i=0;i<160&&!done();i++) Step(game,twin);
            if (!done()) throw new InvalidOperationException(stage+" step="+game.Clock.StepIndex+" tile="+Tile(game,11)+
                " orders="+string.Join(";",Orders(game).Select(o=>o.Id+"/"+o.State+"/"+o.Blocker)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current exact engine identity");
            c.Equal(154,EngineInfo.SaveFormat,"original topology/work/claim codecs retained");
            c.Equal(Physical(c,false),Physical(c,true),"native/direct removal physical chronology");
            foreach (bool direct in new[]{false,true}) {
                foreach (string fault in new[]{"missing-line","empty-line","unknown-line","extra","workerless"}) {
                    c.Throws<ContentException>(()=>New(Create(direct,edit:root=>{
                        var effects=root.Get(direct?"independent_line_effects":"line_effects");
                        var unused=effects[0].Clone();unused.Set("id",JsonValue.Of("removal.unused"));effects.Add(unused);
                        if(fault=="missing-line") unused.Remove("line");
                        if(fault=="empty-line") unused.Set("line",JsonValue.Of(""));
                        if(fault=="unknown-line") unused.Set("line",JsonValue.Of("absent.line"));
                        if(fault=="extra") unused.Set("instant",JsonValue.Of(true));
                        if(fault=="workerless") {var node=root.Get("activities")[0].Get("nodes")[0];node.Set("executor",JsonValue.Of("work.timed"));node.Set("roles",Json.Parse("[]"));}
                    })),"strict authored removal "+direct+"/"+fault);
                }
                var factory=Create(direct,lastTile:true);var game=New(factory);
                for(int i=0;i<4;i++)Step(game);
                ulong hash=game.Hash();var command=Cut(game);
                c.Equal(NetworkService.OccupiedNetwork,game.Seat(0).Preview(command),"stocked last tile refuses before work");
                c.Equal(hash,game.Hash(),"refused topology preview pure");
                c.That(!TestKit.Order(game,0,command).Accepted,"ordinary command same stock refusal");
                c.Equal(1,Tile(game,10),"last pool geometry survives refused command");
                c.Equal(Fix.FromInt(30),Steel(game),"no stock loss from refused removal");
            }
        }
        private static long Physical(Check c,bool independent)
        {
            var factory=Create(independent);var game=New(factory);var isolated=New(factory);ulong isolatedHash=isolated.Hash();
            var limits=new ReplayLimits(4*1024*1024,2000);var recorder=game.RecordReplay(limits);
            c.That(!game.Modules.Contains(TycoonModule.ModuleId),"work does not require business");
            var origin=((TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e=>e.Def=="crane").Spatial).Position;
            c.Equal(Codes.NotOwned,game.Seat(1).Preview(Cut(game)),"foreign target cannot order removal");
            Order(c,game,Cut(game));int id=Orders(game).Single().Id;
            c.Equal(1,Tile(game,11),"post never removes instantly");
            c.Equal(WorkEffectCodes.NotComplete,Work(game).PreviewOrderEffect(id),"unearned removal preview");
            Reach(game,null,()=>Nodes(game,id).Single().Progress.WholeTicks>0,"machine reaches physical work");
            c.That(((TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e=>e.Def=="crane").Spatial).Position!=origin,"actual machine travels");
            c.Equal(1,Tile(game,11),"partial earned work retains bridge");
            var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"partial travel/work saves");
            game.Clock.Pause();twin.Clock.Pause();ulong hash=game.Hash();long at=game.Clock.StepIndex;Step(game,twin);
            c.That(hash==game.Hash()&&at==game.Clock.StepIndex,"single paused clock freezes removal");game.Clock.Resume();twin.Clock.Resume();
            // Cancel the first attempt through ordinary input, retaining actual geometry and pool stock.
            Order(c,game,new CancelOrderCommand {Order=id});twin=factory.Load(game.Save());
            for(int i=0;i<4;i++)Step(game,twin);
            c.Equal(1,Tile(game,11),"cancelled partial work cannot cut bridge");
            c.Equal(Fix.FromInt(30),Steel(game),"cancelled work never grants salvage or destroys fluid");
            Order(c,game,Cut(game));id=Orders(game).Single().Id;twin=factory.Load(game.Save());
            Reach(game,twin,()=>Tile(game,11)==0,"earned physical removal");
            c.That(Tile(game,10)==1&&Tile(game,12)==1,"removal is target footprint only");
            c.Equal(2,Pools(game).Count,"original shared pool physically splits");
            c.Equal(Fix.FromInt(30),Steel(game),"native redistribution conserves source and pools");
            var view=game.Seat(0).View.Feature<InfrastructureView>();
            c.Equal(Code.None,view.ReadConnection(Entity(game,"binding.source"),"binding.main",out var source),"source connection remains");
            c.Equal(Code.None,view.ReadConnection(Entity(game,"binding.watcher"),"binding.main",out var watcher),"target connection remains");
            c.That(source.Endpoint!=watcher.Endpoint,"actual split endpoint identities");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"completed removal restores original topology");
            // Reconnect using the existing corridor workflow and real material delivery, never a stamp call.
            game.Seat(0).View.TryFindResource("steel",out var steel);
            var lay=new LayLineCommand {Line="network.pipe"};lay.Tiles.Add(new TileCoord(11,28));
            Order(c,game,lay);
            var marker=game.Seat(0).Snapshot.Entities.Single(e=>e.Def=="sim.line-work.network.pipe").Id;
            c.Equal(0,Tile(game,11),"rebuild intent does not reconnect instantly");
            Order(c,game,new HaulResourcesCommand {Source=Entity(game,"depot"),Destination=marker,
                Worker=Entity(game,"truck"),Resource=steel,Ceiling=Fix.One,Priority=100});
            twin=factory.Load(game.Save());
            Reach(game,twin,()=>Tile(game,11)==1,"hauled material and earned reconnect");
            c.Equal(1,Pools(game).Count,"original topology joins after physical reconstruction");
            c.Equal(Fix.FromInt(30),Steel(game),"reconnection conserves original connected stock");
            c.Equal(Code.None,view.ReadConnection(Entity(game,"binding.source"),"binding.main",out source),"reconnected source facts");
            c.Equal(Code.None,view.ReadConnection(Entity(game,"binding.watcher"),"binding.main",out watcher),"reconnected target facts");
            c.Equal(source.Endpoint,watcher.Endpoint,"physical rebuilding restores shared connection");
            var depot=new List<Facet>();game.Seat(0).View.Inspect(Entity(game,"depot"),depot);
            c.Equal(Fix.FromInt(49),depot.Where(f=>f.Key==Storage.FacetStock&&f.Subject=="steel").Aggregate(Fix.Zero,(s,f)=>s+f.Value),
                "rebuild consumes one actual hauled material, no resource grant");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"physical reconnected checkpoint");
            c.Equal(isolatedHash,isolated.Hash(),"interleaved session remains isolated");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"ordinary removal/cancellation replay");
            return game.Clock.StepIndex;
        }
    }
}
