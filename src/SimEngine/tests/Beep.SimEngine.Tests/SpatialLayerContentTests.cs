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
    // Authored documents, player input, ordinary Update, public detached facts and assertions only.
    internal static class SpatialLayerContentTests
    {
        internal const string Case="Spatial layer content: direct and native layered metadata drives physical tile and point extraction";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private const string LayerModule="{\"id\":\"proof.spatial-layer-content\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.spatial-resources\",\"version\":\"1.0.0\"}],\"documents\":[\"layer-contracts.json\",\"layer-startup.json\"]}";
        private const string CellsModule="{\"id\":\"sim.spatial-resources\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"cell-work.json\"]}";
        private static void MoveLayers(JsonValue source,JsonValue destination)
        {
            var rows=source.Get("spatial_layers");if(rows==null)return;
            for(int i=0;i<rows.Count;i++)destination.Get("direct_spatial_layers").Add(rows[i].Clone());source.Remove("spatial_layers");
        }
        private static GameFactory Tiles(SpatialLayerContentModule proof,bool native=false,Action<JsonValue> edit=null)
        {
            var root=Json.Parse(Colony.Defs);var work=Json.Parse(FrameworkProofContent.Read("spatial-extraction.json"));
            var layer=Json.Parse(FrameworkProofContent.Read("spatial-layer-contracts.json"));
            var startup=Json.Parse(FrameworkProofContent.Read("spatial-cell-contracts.json"));startup.Remove("direct_tile_cells");
            if(native){layer.Set("spatial_layers",layer.Get("direct_spatial_layers").Clone());layer.Remove("direct_spatial_layers");}
            else {MoveLayers(root,layer);MoveLayers(work,layer);}
            edit?.Invoke(layer);
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(proof.Id));root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
            root.Get("scenarios")[0].Get("settings").Get(WorldSetupModule.ModuleId).Set("profile",JsonValue.Of("layer.start"));
            return Colony.Factory(root.ToString(),flowDefinitions:"{\"resource_flow_journals\":[{\"id\":\"layer.journal\",\"retained_records_per_company\":128}]}",
                additionalModules:new GameModule[]{new SpatialResourcesModule(),proof},moduleDeclarations:new[]{CellsModule,LayerModule},
                moduleDocuments:new Dictionary<string,string>{{"cell-work.json",work.ToString()},{"layer-contracts.json",layer.ToString()},{"layer-startup.json",startup.ToString()}});
        }
        private static GameFactory Points(SpatialLayerContentModule proof,bool native)
        {
            var root=Json.Parse(FrameworkProofContent.Read("planet-x.json"));var layer=Json.Parse("{\"direct_spatial_layers\":[]}");
            if(!native)MoveLayers(root,layer);root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(proof.Id));
            var master=Json.Parse(FrameworkProofContent.Read("planet-x-master.json"));master.Get("modules").Add(Json.Parse(LayerModule));
            return PlanetX.Factory(master.ToString(),new GameModule[]{proof},new Dictionary<string,string>{{"planet-x.json",root.ToString()},{"layer-contracts.json",layer.ToString()},{"layer-startup.json","{}"}});
        }
        private static SimGame New(GameFactory factory,bool point=false)=>factory.NewGame(new NewGameRequest{Scenario=point?"planet-x":"colony",Seed=818,Seats={Controller.Human,Controller.Human}});
        private static EntityId Actor(SimGame game,string prototype)=>game.Seat(0).Snapshot.Entities.Single(x=>x.Def==prototype).Id;
        private static IReadOnlyList<SpatialLayerSnapshot> Layers(SimGame game,bool point,int seat=0)=>point?game.Seat(seat).View.Feature<PointLayerView>().Layers:game.Seat(seat).View.Feature<TileMapView>().Layers;
        private static Fix Stock(SimGame game,EntityId target,string resource)
        {var facts=new List<Facet>();game.Seat(0).View.Inspect(target,facts);return facts.Where(x=>x.Key==Storage.FacetStock&&x.Subject==resource).Aggregate(Fix.Zero,(s,f)=>s+f.Value);}
        private static Fix Deposit(SimGame game,bool point)
        {
            if(point){var rows=new List<Fix>();if(game.Seat(0).View.Feature<PointLayerView>().ReadLayer("planet.deposit",new PointRegion(Fix.FromInt(20),Fix.One,1),rows)!=SpatialLayerReadStatus.Success||rows.Count!=1)throw new InvalidOperationException("point deposit unavailable");return rows[0];}
            var tiles=new List<int>();if(game.Seat(0).View.Feature<TileMapView>().ReadLayer("extract.deposit",new TileRect(20,20,1,1),tiles)!=SpatialLayerReadStatus.Success||tiles.Count!=1)throw new InvalidOperationException("tile deposit unavailable");return Fix.FromRatio(tiles[0],2);
        }
        private static void Refuse(Check c,Action<JsonValue> edit,string label,bool thrown=false)
        {
            var proof=new SpatialLayerContentModule();
            if(thrown)c.Throws<InvalidOperationException>(()=>New(Tiles(proof,edit:edit)),label);else c.Throws<ContentException>(()=>New(Tiles(proof,edit:edit)),label);
            c.Equal(0,proof.Initialize.Initializations,"invalid loaded/unused layer precedes Initialize "+label);
        }
        private static void Run(Check c)
        {
            foreach(string field in new[]{"space","type","value_type","visibility","unit"})Refuse(c,doc=>doc.Get("direct_spatial_layers")[0].Remove(field),"required authored "+field);
            Refuse(c,doc=>doc.Get("direct_spatial_layers")[0].Set("unknown",JsonValue.Of("ignored")),"strict unknown metadata");
            foreach(string field in new[]{"space","unit","resource","vertical_unit"})Refuse(c,doc=>DepositRow(doc).Set(field,JsonValue.Of("missing.alias")),"missing actual reference "+field);
            Refuse(c,doc=>DepositRow(doc).Set("unit",JsonValue.Of("unit.water")),"quantity dimension matches actual native resource");
            Refuse(c,doc=>DepositRow(doc).Set("resource_measure",JsonValue.Of("implicit")),"no implicit resource interpretation");
            Refuse(c,doc=>DepositRow(doc).Remove("vertical_domain"),"vertical metadata needs domain");
            Refuse(c,doc=>{var row=DepositRow(doc).Clone();row.Set("id",JsonValue.Of("unused.layer"));row.Set("unit",JsonValue.Of("missing.unit"));doc.Get("direct_spatial_layers").Add(row);},"invalid unused layer refuses");
            Refuse(c,doc=>{var rows=JsonValue.NewArray();rows.Add(DepositRow(doc).Clone());doc.Set("spatial_layers",rows);},"mixed native/direct alias cannot silently select a preferred definition");
            foreach(string mode in new[]{"identity","owner","space-id","space","type","value-zero","value-invalid","visibility-zero","visibility-invalid","unit","resource","resource-foreign","measure","measure-invalid","vertical","vertical-unit","reference","metadata-null","metadata-mutable","metadata-array","throw"})
                Refuse(c,doc=>{var row=DepositRow(doc).Clone();row.Set("id",JsonValue.Of("unused.broken"));row.Set("mode",JsonValue.Of(mode));var rows=JsonValue.NewArray();rows.Add(row);doc.Set("broken_spatial_layers",rows);},"all loaded direct metadata admitted "+mode,mode=="throw");
            foreach(bool native in new[]{false,true}){Physical(c,Tiles(new SpatialLayerContentModule(),native),false);Physical(c,Points(new SpatialLayerContentModule(),native),true);}
        }
        private static JsonValue DepositRow(JsonValue doc)
        {var rows=doc.Get("direct_spatial_layers");for(int i=0;i<rows.Count;i++)if(rows[i].Get("id").Text=="extract.deposit")return rows[i];throw new InvalidOperationException("authored deposit row missing");}
        private static void Physical(Check c,GameFactory factory,bool point)
        {
            var game=New(factory,point);var isolated=New(factory,point);ulong other=isolated.Hash();
            var target=Actor(game,point?"planet.mine":"extract.site");string resource=point?"planet.ore":"steel",plan=point?"planet.extract":"extract.activity",deposit=point?"planet.deposit":"extract.deposit";
            Fix opening=point?Fix.FromInt(12):Fix.FromInt(6);c.Equal(opening,Deposit(game,point),"authored actual finite material, not copied inventory");
            var resources=new List<ResourceInfo>();game.Seat(0).View.CopyResources(resources);
            var initialFlows=game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0);
            c.Equal(ResourceFlowReadStatus.Ready,initialFlows.Status,"opening accounting uses actual available flow history");
            c.That(initialFlows.Entries.All(row=>row.Item.Source.Endpoint.Scope!=SpatialResourcesModule.ModuleId&&row.Item.Destination.Endpoint.Scope!=SpatialResourcesModule.ModuleId),
                "world-authored finite layer quantities are opening stock, not initialized inventory or produced transfer facts");
            Fix Material(SimGame session,string id)=>session.Seat(0).Snapshot.Entities.Where(entity=>entity.Owner==new PlayerId(0))
                .Aggregate(id==resource?Deposit(session,point):Fix.Zero,(sum,entity)=>sum+Stock(session,entity.Id,id));
            var initialMaterial=resources.ToDictionary(row=>row.Id,row=>Material(game,row.Id));
            var initialTotals=resources.ToDictionary(row=>row.Id,row=>initialFlows.Totals(row.Resource));
            void Account(SimGame session)
            {
                var page=session.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0);
                c.Equal(ResourceFlowReadStatus.Ready,page.Status,"finite accounting never silently uses expired flow history");
                foreach(var row in resources){
                    c.Equal(initialMaterial[row.Id],Material(session,row.Id),"every owned inventory/cargo plus finite layer balances without treating lake level as stock: "+row.Id);
                    var current=page.Totals(row.Resource);var before=initialTotals[row.Id];
                    c.That(current.InitializedRaw==before.InitializedRaw&&current.CreatedRaw==before.CreatedRaw&&current.ConsumedRaw==before.ConsumedRaw&&
                        current.DestroyedRaw==before.DestroyedRaw&&current.LostRaw==before.LostRaw&&current.SoldRaw==before.SoldRaw&&current.StockPurchasedRaw==before.StockPurchasedRaw,
                        "finite extraction/load grants no lifecycle production, purchase, destruction or second initialization: "+row.Id);
                }
                if(!point){
                    var map=session.Seat(0).View.Feature<TileMapView>();var integers=new List<int>();var heights=new List<Fix>();
                    c.Equal(SpatialLayerReadStatus.Success,map.ReadLayer("layer.classification",new TileRect(20,20,1,1),integers),"actual non-material classification remains readable");
                    c.Equal(3,integers.Single(),"classification is unchanged by finite material work");
                    c.Equal(SpatialLayerReadStatus.Success,map.ReadLayer("layer.control",new TileRect(20,20,1,1),integers),"actual non-material control remains readable");
                    c.Equal(7,integers.Single(),"control value is not spent as a resource");
                    c.Equal(SpatialLayerReadStatus.Success,map.ReadLayer("layer.lake",new TileRect(20,20,1,1),heights),"actual associated water-level measurement remains readable");
                    c.Equal(Fix.FromRatio(3,2),heights.Single(),"associated lake level stays descriptive, never an inferred water quantity");
                }
            }
            Account(game);Account(factory.Load(game.Save()));
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current behavior identity");
            var layers=Layers(game,point);var retained=layers.Single(x=>x.Id==deposit);
            c.That(retained.Unit!=null&&retained.Resource.Id==resource&&retained.ResourceMeasure==SpatialResourceMeasure.Quantity&&retained.VerticalDomain==new Code("underground")&&retained.VerticalUnit!=null&&retained.VerticalReference=="surface"&&retained.VerticalOffset==Fix.FromInt(-5),"same actual units/resource/vertical aliases survive either declaration/provider");
            c.Throws<InvalidOperationException>(()=>retained.Metadata.Set("mutate",JsonValue.Of(true)),"retained layer metadata is immutable");
            c.Throws<NotSupportedException>(()=>((IList<SpatialLayerSnapshot>)layers).Clear(),"retained layer collection is detached read-only");
            c.That(!Layers(game,point,1).Any(x=>x.Id==deposit),"private definition/data withheld from another company");
            if(!point){
                var map=game.Seat(0).View.Feature<TileMapView>();var ints=new List<int>();var fixedValues=new List<Fix>();
                c.Equal(SpatialLayerReadStatus.Success,map.ReadLayer("layer.classification",new TileRect(20,20,1,1),ints),"byte representation shares actual metadata");c.Equal(3,ints.Single(),"authored byte sample");
                c.Equal(SpatialLayerReadStatus.Success,map.ReadLayer("layer.control",new TileRect(20,20,1,1),ints),"integer representation shares actual metadata");c.Equal(7,ints.Single(),"authored integer sample");
                c.Equal(SpatialLayerReadStatus.Success,map.ReadLayer("layer.lake",new TileRect(20,20,1,1),fixedValues),"fixed surface-water representation");c.Equal(Fix.FromRatio(3,2),fixedValues.Single(),"authored lake level");
                var lake=layers.Single(x=>x.Id=="layer.lake");c.That(lake.ResourceMeasure==SpatialResourceMeasure.Associated&&lake.Resource.Id=="water"&&lake.VerticalDomain==new Code("lake.surface")&&lake.VerticalOffset==Fix.Half&&lake.Metadata.Get("water_body").Text=="lake","water level is associated metadata, never a fabricated material volume");
                c.Equal(SpatialLayerReadStatus.ValueTypeMismatch,map.ReadLayer("layer.control",new TileRect(20,20,1,1),fixedValues),"typed query refuses another representation");c.Equal(0,fixedValues.Count,"refused typed query clears output");
            }
            var replayLimits=new ReplayLimits(8*1024*1024,2000);var replay=game.RecordReplay(replayLimits);
            var command=new PostOrderCommand{Plan=plan,Target=target};ulong hash=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(command),"normal activity admission uses layer contract");c.Equal(hash,game.Hash(),"preview has no mutation");
            game.Seat(0).Submit(command);var twin=factory.Load(game.Save());int order=0;bool productive=false;
            for(int i=0;i<256&&Stock(game,target,resource)!=Fix.FromInt(2);i++){
                c.Equal(1,game.Update(),"ordinary sole-clock update");if(game.Fault!=null)throw new InvalidOperationException("layer update fault "+game.Fault);
                twin.Update();c.Equal(game.Hash(),twin.Hash(),"saved actual values/books/work twin");c.Equal(opening,Deposit(game,point)+Stock(game,target,resource),"finite quantity conserved each update");
                Account(game);Account(twin);
                if(order==0){var orders=new List<OrderView>();game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);order=orders.Single(x=>x.Plan=="activity:"+plan&&x.Target==target).Id;c.Equal(Fix.Zero,Stock(game,target,resource),"queued work cannot instantly extract");}
                var nodes=new List<ActivityNodeView>();game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order,nodes);
                if(!productive&&nodes.Any(x=>x.ProductiveSteps>0&&x.Progress.WholeTicks>0&&x.State!=NodeState.Done)){
                    productive=true;twin=factory.Load(game.Save());ulong paused=game.Hash();game.Clock.Pause();c.Equal(0,game.Update(),"pause earns no layer depletion");c.Equal(paused,game.Hash(),"paused saved values unchanged");game.Clock.Resume();
                }
            }
            c.That(productive&&Stock(game,target,resource)==Fix.FromInt(2),"bounded real productive work delivers exact atomic extraction; step="+game.Clock.StepIndex+" deposit="+Deposit(game,point)+" stock="+Stock(game,target,resource));
            c.Equal(opening-Fix.FromInt(2),Deposit(game,point),"only actual finite layer debited");c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"completed layer/material state restores without regrant");
            Account(factory.Load(game.Save()));
            var materialFacts=game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0),initialFlows.Latest).Entries.Select(row=>row.Item)
                .Where(row=>row.Source.Endpoint.Scope==SpatialResourcesModule.ModuleId||row.Destination.Endpoint.Scope==SpatialResourcesModule.ModuleId).ToArray();
            c.That(materialFacts.Length==1&&materialFacts[0].Kind==ResourceFlowKind.StockTransfer&&materialFacts[0].Amount==Fix.FromInt(2)&&
                materialFacts[0].Source.Endpoint.Identity==1&&materialFacts[0].Destination.Endpoint==ResourceEndpointId.ForEntity(target)&&
                materialFacts[0].SourceBefore-materialFacts[0].SourceAfter==Fix.FromInt(2)&&materialFacts[0].DestinationAfter-materialFacts[0].DestinationBefore==Fix.FromInt(2),
                "one exact native movement debits the actual authored cell and credits the receiving inventory, not Produced or Initialized");
            c.That(retained.Owner==new PlayerId(0)&&retained.VerticalOffset==Fix.FromInt(-5),"retained metadata does not become live mutable provider state");
            c.Equal(1,replay.CommandCount,"recording includes the actual one player extraction command");
            c.Equal(game.Clock.StepIndex-replay.InitialStep,(long)replay.CheckpointCount,"recording includes every committed update through completed extraction");
            var verified=new ReplayVerifier().Verify(factory,replay.Finish(),replayLimits);
            c.That(verified.Success&&verified.ActualHash==game.Hash(),"ordinary layered finite work replays to the complete final live state, not only a valid prefix");
            Console.WriteLine("spatial opening accounting: point="+point+";metadata-view="+retained.GetType().Name+";initial-step="+replay.InitialStep+";final-step="+game.Clock.StepIndex+
                ";commands="+replay.CommandCount+";checkpoints="+replay.CheckpointCount+";expected-hash="+game.Hash().ToString("X16")+";actual-hash="+verified.ActualHash.ToString("X16"));
            c.Equal(other,isolated.Hash(),"interleaved layer ownership isolated");
            if(point){var hidden=new List<Fix>{Fix.One};c.That(game.Seat(1).View.Feature<PointLayerView>().ReadLayer(deposit,new PointRegion(Fix.FromInt(20),Fix.One,1),hidden)!=SpatialLayerReadStatus.Success&&hidden.Count==0,"non-tile private sample clears output");}
            else{var hidden=new List<int>{1};c.That(game.Seat(1).View.Feature<TileMapView>().ReadLayer(deposit,new TileRect(20,20,1,1),hidden)!=SpatialLayerReadStatus.Success&&hidden.Count==0,"tile private sample clears output");}
        }
    }
}
