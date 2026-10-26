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
    // Authored startup, ordinary activity inputs, public facts and assertions only.
    internal static partial class SpatialCellContentTests
    {
        internal const string Case = "Spatial cell content: native direct tile and independent point aliases own finite timed saved extraction";
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add("Spatial cell codec: explicit empty bindings retain exact counts and staged publication", EmptyBindingCodec);
        }
        private const string Declaration = "{\"id\":\"sim.spatial-resources\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"cell-work.json\"]}";
        private const string ProofDeclaration = "{\"id\":\"proof.spatial-cell-content\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.spatial-resources\",\"version\":\"1.0.0\"}],\"documents\":[\"spatial-cell-contracts.json\"]}";
        private static GameFactory Tiles(SpatialCellContentModule proof, bool native = false, Action<JsonValue,JsonValue> edit = null)
        {
            var work=Json.Parse(FrameworkProofContent.Read("spatial-extraction.json"));
            var addon=Json.Parse(FrameworkProofContent.Read("spatial-cell-contracts.json"));
            if(native) addon.Remove("direct_tile_cells");
            else {work.Remove("tile_resource_cells"); work.Get("spatial_resource_transfer_effects")[0].Set("cell",JsonValue.Of("extract.direct"));}
            edit?.Invoke(work,addon);
            var root=Json.Parse(Colony.Defs);
            root.Get("scenarios")[0].Get("settings").Get(WorldSetupModule.ModuleId).Set("profile",JsonValue.Of("extract.start"));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(proof.Id)); root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
            var retry=Json.Parse(FrameworkProofContent.Read("work-retry.json"));
            retry.Get("work_retry_profiles")[0].Get("retry_blockers").Add(JsonValue.Of(WorkEffectCodes.NoStock.Id));
            return Colony.Factory(root.ToString(),flowDefinitions:"{\"resource_flow_journals\":[{\"id\":\"extract.journal\",\"retained_records_per_company\":128}]}",retryProfiles:retry.ToString(),
                additionalModules:new GameModule[]{new SpatialResourcesModule(),proof}, moduleDeclarations:new[]{Declaration,ProofDeclaration},
                moduleDocuments:new Dictionary<string,string>{{"cell-work.json",work.ToString()},{"spatial-cell-contracts.json",addon.ToString()}});
        }
        private static GameFactory Points(SpatialCellContentModule proof)
        {
            var master=Json.Parse(FrameworkProofContent.Read("planet-x-master.json")); master.Get("modules").Add(Json.Parse(ProofDeclaration));
            var world=Json.Parse(FrameworkProofContent.Read("planet-x.json")); world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(proof.Id));
            // Authored two-sided service geometry: one-dimensional machines cannot pass
            // the stationary extractor; the carrier starts on the delivery side.
            var entities=world.Get("entities");for(int i=0;i<entities.Count;i++)if(entities[i].Get("id").Text=="planet.mine")
                entities[i].Get("components").Get("point_dock").Get("slots").Add(Json.Parse("{\"id\":\"haul.service\",\"kind\":\"service\",\"offset\":3}"));
            var startup=world.Get("entity_setups");for(int i=0;i<startup.Count;i++)if(startup[i].Get("id").Text=="planet.start.carrier")
                startup[i].Get("placement").Set("position",JsonValue.Number("24"));
            var retry=Json.Parse(FrameworkProofContent.Read("work-retry.json"));retry.Get("work_retry_profiles")[0].Get("retry_blockers").Add(JsonValue.Of(WorkEffectCodes.NoStock.Id));
            return PlanetX.Factory(master.ToString(),new GameModule[]{proof},
                new Dictionary<string,string>{{"spatial-cell-contracts.json","{}"},{"planet-x.json",world.ToString()},{"work-retry.json",retry.ToString()}});
        }
        private static SimGame New(GameFactory factory,bool point=false)
            => factory.NewGame(new NewGameRequest{Scenario=point?"planet-x":"colony",Seed=817,Seats={Controller.Human,Controller.Human}});
        private static EntityId Actor(SimGame game,string id) => game.Seat(0).Snapshot.Entities.Single(x=>x.Def==id).Id;
        private static Fix Stock(SimGame game,EntityId target,string resource)
        {var rows=new List<Facet>(); game.Seat(0).View.Inspect(target,rows); return rows.Where(x=>x.Key==Storage.FacetStock&&x.Subject==resource).Aggregate(Fix.Zero,(sum,x)=>sum+x.Value);}
        private static Fix Deposit(SimGame game,bool point)
        {
            if(point){
                var rows=new List<Fix>(); var status=game.Seat(0).View.Feature<PointLayerView>().ReadLayer("planet.deposit",new PointRegion(Fix.FromInt(20),Fix.One,1),rows);
                if(status!=SpatialLayerReadStatus.Success||rows.Count!=1)throw new InvalidOperationException("point deposit "+status); return rows[0];
            }
            var tileRows=new List<int>(); var tileStatus=game.Seat(0).View.Feature<TileMapView>().ReadLayer("extract.deposit",new TileRect(20,20,1,1),tileRows);
            if(tileStatus!=SpatialLayerReadStatus.Success||tileRows.Count!=1)throw new InvalidOperationException("tile deposit "+tileStatus);
            return Fix.FromRatio(tileRows[0],2); // Authored half-native storage unit, only an assertion-side unit expectation.
        }
        private static List<ActivityNodeView> Nodes(SimGame game,int order)
        {var rows=new List<ActivityNodeView>(); game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order,rows);return rows;}
        private static void Step(Check c,SimGame game,EntityId target,string resource,Fix original,bool point,SimGame twin=null)
        {
            c.Equal(1,game.Update(),"one ordinary game update");
            if(game.Fault!=null)throw new InvalidOperationException("cell update fault "+game.Fault);
            c.Equal(original,Deposit(game,point)+Stock(game,target,resource)+Stock(game,Actor(game,point?"planet.refinery":"extract.receiver"),resource)
                +Stock(game,Actor(game,point?"planet.carrier":"extract.worker"),resource),"finite actual layer + receiving inventory + physical cargo conserved each update");
            if(twin!=null){twin.Update(); c.Equal(game.Hash(),twin.Hash(),"saved layer/book/order/worker twin");}
        }
        private static void Reach(Check c,SimGame game,EntityId target,string resource,Fix original,bool point,Func<bool> ready,string stage,SimGame twin=null)
        {
            for(int i=0;i<256&&!ready();i++)Step(c,game,target,resource,original,point,twin);
            if(!ready()){
                var orders=new List<OrderView>();game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
                var jobs=new List<WorkJobView>();game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                throw new InvalidOperationException(stage+" at="+game.Clock.StepIndex+" layer="+Deposit(game,point)+" stock="+Stock(game,target,resource)+" orders="+string.Join(";",orders.Select(x=>x.Id+"/"+x.State+"/"+x.Blocker))
                    +" jobs="+string.Join(";",jobs.Select(x=>x.Id+"/"+x.State+"/"+x.Blocker+" worker="+x.Worker)));
            }
        }
        private static void Refuse(Check c,Action<JsonValue,JsonValue> edit,string label,bool argument=false)
        {
            var proof=new SpatialCellContentModule();
            if(argument)c.Throws<InvalidOperationException>(()=>New(Tiles(proof,edit:edit)),label);
            else c.Throws<ContentException>(()=>New(Tiles(proof,edit:edit)),label);
            c.Equal(0,proof.Initialize.Initializations,"no Initialize/stock grants after invalid cell "+label);
        }
        private static void Run(Check c)
        {
            foreach(string field in new[]{"identity","layer","capacity_policy","public","x","y"})
                Refuse(c,(work,addon)=>addon.Get("direct_tile_cells")[0].Remove(field),"explicit cell key required "+field);
            Refuse(c,(work,addon)=>addon.Get("direct_tile_cells")[0].Set("unknown",JsonValue.Number("1")),"no ignored cell metadata");
            foreach(string identity in new[]{"0","-1"})Refuse(c,(work,addon)=>addon.Get("direct_tile_cells")[0].Set("identity",JsonValue.Number(identity)),"positive stable identity "+identity);
            Refuse(c,(work,addon)=>addon.Get("direct_tile_cells")[0].Set("layer",JsonValue.Of("missing.layer")),"missing actual layer refuses");
            Refuse(c,(work,addon)=>addon.Get("direct_tile_cells")[0].Set("capacity_policy",JsonValue.Of("missing.capacity")),"missing capacity refuses");
            Refuse(c,(work,addon)=>addon.Get("direct_tile_cells")[0].Set("public",JsonValue.Of(true)),"access cannot bypass private layer");
            Refuse(c,(work,addon)=>addon.Get("direct_tile_cells")[0].Set("x",JsonValue.Number("10000")),"location respects selected authored extent");
            Refuse(c,(work,addon)=>{var row=addon.Get("direct_tile_cells")[0].Clone();row.Set("id",JsonValue.Of("extract.duplicate")); addon.Get("direct_tile_cells").Add(row);},"duplicate scoped identity refuses before bindings");
            Refuse(c,(work,addon)=>work.Get("spatial_resource_transfer_effects")[0].Set("cell",JsonValue.Of("missing.cell")),"effect resolves actual contract alias");
            foreach(string mode in new[]{"identity","access","layer","capacity","throw","clock","bind"})
                Refuse(c,(work,addon)=>Broken(work,addon,mode),"direct provider "+mode,mode=="throw"||mode=="clock");
            Physical(c,Tiles(new SpatialCellContentModule()),false);
            Physical(c,Tiles(new SpatialCellContentModule(),native:true),false);
            Physical(c,Points(new SpatialCellContentModule()),true);
            var badFactory=Tiles(new SpatialCellContentModule(),edit:(work,addon)=>Broken(work,addon,"site-clock"));var bad=New(badFactory);
            var badOrder=new PostOrderCommand{Plan="extract.activity",Target=Actor(bad,"extract.site")};
            ulong clean=bad.Hash();c.Equal(Code.None,bad.Seat(0).Preview(badOrder),"admission defers physical site work");c.Equal(clean,bad.Hash(),"admission remains pure");bad.Seat(0).Submit(badOrder);
            for(int i=0;i<256&&bad.Fault==null;i++)bad.Update();
            c.That(bad.Fault!=null,"physical site purity latches first mutation fault");
            long stopped=bad.Clock.StepIndex;c.Equal(0,bad.Update(),"site fault cuts off further physical steps");c.Equal(stopped,bad.Clock.StepIndex,"fault does not grant another clock step");
            c.Throws<InvalidOperationException>(()=>bad.Save(),"site-corrupted state cannot checkpoint");
        }
        private static void Broken(JsonValue work,JsonValue addon,string mode)
        {
            var row=addon.Get("direct_tile_cells")[0].Clone();row.Set("id",JsonValue.Of("extract.broken"));row.Set("mode",JsonValue.Of(mode));
            var rows=JsonValue.NewArray();rows.Add(row);addon.Remove("direct_tile_cells");addon.Set("broken_tile_cells",rows);
            work.Get("spatial_resource_transfer_effects")[0].Set("cell",JsonValue.Of("extract.broken"));
        }
        private static void Physical(Check c,GameFactory factory,bool point)
        {
            var game=New(factory,point);var other=New(factory,point);ulong untouched=other.Hash();
            string prototype=point?"planet.mine":"extract.site",resource=point?"planet.ore":"steel",plan=point?"planet.extract":"extract.activity";
            var target=Actor(game,prototype);Fix original=point?Fix.FromInt(12):Fix.FromInt(6);
            c.Equal(original,Deposit(game,point),"authored startup supplies real finite deposit, not inventory substitute");
            c.Equal(Fix.Zero,Stock(game,target,resource),"receiving entity begins empty");
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current behavior identity");
            var retained=game.Seat(0).Snapshot;var limits=new ReplayLimits(8*1024*1024,4000);var replay=game.RecordReplay(limits);
            int rounds=point?6:3;
            for(int round=1;round<=rounds;round++){
                var command=new PostOrderCommand{Plan=plan,Target=target};ulong hash=game.Hash();
                c.Equal(Code.None,game.Seat(0).Preview(command),"ordinary extraction command preview");c.Equal(hash,game.Hash(),"preview grants no material or time");
                game.Seat(0).Submit(command);var twin=factory.Load(game.Save());Step(c,game,target,resource,original,point,twin);
                c.Equal(Fix.FromInt((round-1)*2),Stock(game,target,resource),"queued activity cannot instantly extract");
                var orders=new List<OrderView>();game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
                int order=orders.Single(x=>x.Plan=="activity:"+plan&&x.Target==target&&x.State!=OrderState.Done).Id;
                Reach(c,game,target,resource,original,point,()=>Nodes(game,order).Any(x=>x.ProductiveSteps>0&&x.Progress.WholeTicks>0),"actual productive machine work",twin);
                twin=factory.Load(game.Save());ulong paused=game.Hash();game.Clock.Pause();c.Equal(0,game.Update(),"paused work earns no depletion");c.Equal(paused,game.Hash(),"paused material and orders remain");game.Clock.Resume();
                Reach(c,game,target,resource,original,point,()=>Stock(game,target,resource)==Fix.FromInt(round*2),"earned atomic two-unit extraction",twin);
                c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"completed withdrawal/book versions restore without regrant");
            }
            var history=game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0);
            var facts=history.Entries.Where(x=>x.Item.Source.Endpoint.Scope==SpatialResourcesModule.ModuleId).Select(x=>x.Item).ToArray();
            c.That(facts.Length==rounds&&facts.All(x=>x.Kind==ResourceFlowKind.StockTransfer&&x.Amount==Fix.FromInt(2)&&x.SourceBefore.Value-x.SourceAfter.Value==x.Amount&&x.DestinationAfter-x.DestinationBefore==x.Amount),
                "all exact debits/credits retain actual scoped provenance, never Produced or fabricated stock");
            c.That(facts.All(x=>x.Source.Endpoint.Identity==1&&x.SourceVersionAfter>x.SourceVersionBefore),"same sole embedded endpoint/version identity survives all activity rounds");
            c.Equal(ResourceFlowReadStatus.NotOwned,game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0).Status,"private extraction provenance");
            if(point){var hidden=new List<Fix>();c.That(game.Seat(1).View.Feature<PointLayerView>().ReadLayer("planet.deposit",new PointRegion(Fix.FromInt(20),Fix.One,1),hidden)!=SpatialLayerReadStatus.Success&&hidden.Count==0,"point material remains company private");}
            else{var hidden=new List<int>();c.That(game.Seat(1).View.Feature<TileMapView>().ReadLayer("extract.deposit",new TileRect(20,20,1,1),hidden)!=SpatialLayerReadStatus.Success&&hidden.Count==0,"tile material remains company private");}
            c.That(retained.Tick==0&&retained.Entities.Any(x=>x.Id==target),"retained initial publication survives physical work");
            c.Equal(Fix.Zero,Deposit(game,point),"finite authored deposit is exhausted");
            game.Seat(0).Submit(new PostOrderCommand{Plan=plan,Target=target});Step(c,game,target,resource,original,point);
            var pending=new List<OrderView>();game.Seat(0).View.Feature<WorkView>().CopyOrders(pending);
            int blocked=pending.Single(x=>x.Plan=="activity:"+plan&&x.Target==target&&x.State!=OrderState.Done).Id;
            Reach(c,game,target,resource,original,point,()=>Nodes(game,blocked).Any(x=>x.Blocker==WorkEffectCodes.NoStock),"real finite deposit blocker");
            var blockedTwin=factory.Load(game.Save());Step(c,game,target,resource,original,point,blockedTwin);
            c.Equal(original,Stock(game,target,resource),"blocked extraction fabricates neither yield nor refill");
            game.Seat(0).Submit(new CancelOrderCommand{Order=blocked});Step(c,game,target,resource,original,point);
            Haul(c,game,factory,target,resource,original,point);
            c.That(new ReplayVerifier().Verify(factory,replay.Finish(),limits).Success,"actual layered cell extraction replays");
            c.Equal(untouched,other.Hash(),"interleaved session has distinct deposit/book/work ownership");
        }
        private static void Haul(Check c,SimGame game,GameFactory factory,EntityId source,string resource,Fix original,bool point)
        {
            EntityId receiver=Actor(game,point?"planet.refinery":"extract.receiver"),carrier=Actor(game,point?"planet.carrier":"extract.worker");
            var jobs=new List<WorkJobView>();
            Reach(c,game,source,resource,original,point,()=>{jobs.Clear();game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);return !jobs.Any(x=>x.Worker==carrier);},"explicit carrier release");
            var resources=new List<ResourceInfo>();game.Seat(0).View.CopyResources(resources);
            var command=new HaulResourcesCommand{Source=source,Destination=receiver,Worker=carrier,Resource=resources.Single(x=>x.Id==resource).Resource,Ceiling=Fix.FromInt(2)};
            ulong before=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(command),"ordinary finite output haul admitted");c.Equal(before,game.Hash(),"haul preview transfers no material");
            game.Seat(0).Submit(command);var twin=factory.Load(game.Save());Step(c,game,source,resource,original,point,twin);
            c.Equal(Fix.Zero,Stock(game,receiver,resource),"haul requires physical loading/travel/unloading");
            Reach(c,game,source,resource,original,point,()=>Stock(game,carrier,resource).IsPositive,"loaded finite resource cargo",twin);
            twin=factory.Load(game.Save());Reach(c,game,source,resource,original,point,()=>Stock(game,receiver,resource)==Fix.FromInt(2),"finite resource physical delivery",twin);
            c.Equal(Fix.Zero,Stock(game,carrier,resource),"delivered cargo settles empty");c.Equal(original-Fix.FromInt(2),Stock(game,source,resource),"hauling moves existing extracted stock only");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"delivered finite stock checkpoint restores exactly");
        }
    }
}

