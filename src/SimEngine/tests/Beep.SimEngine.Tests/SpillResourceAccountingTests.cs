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
    internal static partial class SpillResourceAccountingTests
    {
        internal const string Case="Spill accounting: independent physical deposition recovery hauling and treatment";
        internal const string NativeCase="Spill accounting: native physical deposition recovery hauling and treatment";
        internal static void Register(){TestRunner.Add(Case,c=>Run(c,false));TestRunner.Add(NativeCase,c=>Run(c,true));
            TestRunner.Add(InterruptedCargoCase,c=>{Run(c,false,true,interruptedCargo:true);Run(c,true,true,interruptedCargo:true);});
            TestRunner.Add("Spill accounting: finite stock and bounded room refuse exposure before episode",Admission);
            TestRunner.Add("Spill environment: authored current stock privacy and malformed inputs",EnvironmentAdmission);
            TestRunner.Add("Spill environment: independent physical backlog recovery and treatment objectives",c=>Run(c,false,true));
            TestRunner.Add("Spill environment: native physical backlog recovery and treatment objectives",c=>Run(c,true,true));
            TestRunner.Add("Spill history: independent cumulative deposition and recovery survive eviction",c=>Run(c,false,true,true));
            TestRunner.Add("Spill history: native cumulative deposition and recovery survive eviction",c=>Run(c,true,true,true));
            TestRunner.Add("Spill disposal: native recovery hauling cancellation and earned destruction",c=>Run(c,true,true,false,true));
            TestRunner.Add("Spill disposal: independent contracts recovery hauling cancellation and earned destruction",c=>Run(c,false,true,false,true));
            TestRunner.Add("Spill disposal recovery: native interrupted cargo completes physical timed disposal",c=>Run(c,true,true,disposal:true,interruptedCargo:true));
            TestRunner.Add("Spill disposal recovery: independent interrupted cargo completes physical timed disposal",c=>Run(c,false,true,disposal:true,interruptedCargo:true));
            TestRunner.Add(SiteObservationCase,SiteObservation);
            TestRunner.Add(PublicSamplesCase,PublicSamples);
            TestRunner.Add(PublicSampleFaultCase,PublicSampleFaults);
            TestRunner.Add(IntegerSamplesCase,IntegerSamples);
            TestRunner.Add(IntegerFaultCase,IntegerFaults);
            TestRunner.Add(AliasDeclarationCase,AliasDeclarations);
            TestRunner.Add("Spatial revisions: native physical private spill and recovery do not notify foreign viewers",c=>Run(c,true,privateRevision:true));
            TestRunner.Add("Spatial revisions: independent physical private spill and recovery do not notify foreign viewers",c=>Run(c,false,privateRevision:true));
            TestRunner.Add(RevisionProtocolCase,RevisionProtocol);
            TestRunner.Add(RegionRevisionCase,RegionRevisions);
            TestRunner.Add(SharedPageCase,SharedPages);
            TestRunner.Add(PageFaultCase,PageFaults);
            TestRunner.Add(RegionChangeCase,RegionChangeProtocol);
            TestRunner.Add(RegionChangeFaultCase,RegionChangeFaults);
            TestRunner.Add(NativeUnfinishedCase,c=>Run(c,true,true,false,true,UnfinishedDisposal));
            TestRunner.Add(IndependentUnfinishedCase,c=>Run(c,false,true,false,true,UnfinishedDisposal));}
        private static SimGame New(GameFactory factory)=>factory.NewGame(new NewGameRequest{
            Scenario="independent-sensor",Seed=743,Seats={Controller.Human,Controller.Human}});
        private static EntityId Id(SimGame game,string name)=>game.Seat(0).Snapshot.Entities.Single(e=>e.Def==name).Id;
        private static ResourceId Resource(SimGame game,string name)
        {if(!game.Seat(0).View.TryFindResource(name,out var value))throw new InvalidOperationException("missing resource "+name);return value;}
        private static Fix Stock(SimGame game,EntityId id,string resource)
        {var rows=new List<Facet>();game.Seat(0).View.Inspect(id,rows);return rows.Where(f=>f.Key==Storage.FacetStock&&f.Subject==resource).Aggregate(Fix.Zero,(sum,f)=>sum+f.Value);}
        // Read-only assertion over original layer stock, not a driver-owned spill balance.
        private static Fix Spill(SimGame game)
        {
            var query=game.Seat(0).View.Feature<ISpatialLayerQuery<TileRect,Fix>>()
                ??throw new InvalidOperationException("selected spill space has no public fixed sample query contract");
            var values=new List<Fix>();var status=query.ReadLayer("sensor.spilled-oil",new TileRect(14,20,1,1),values);
            if(status!=SpatialLayerReadStatus.Success||values.Count!=1)throw new InvalidOperationException("authorized spill sample unavailable "+status);
            return values[0];
        }
        private static TransformationSnapshot Process(SimGame game)
        {if(!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Id(game,"sensor.treatment"),new Code("treat"),out var value))throw new InvalidOperationException("missing actual treatment state");return value;}
        private static Fix Total(SimGame game)
        {
            Fix total=Spill(game)+Process(game).Wip.Stock.AmountOf(Resource(game,"oil"))+Process(game).Wip.Stock.AmountOf(Resource(game,"treated-oil"));
            foreach(var e in game.Seat(0).Snapshot.Entities)total+=Stock(game,e.Id,"oil")+Stock(game,e.Id,"treated-oil");
            var destroyed=game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates
                .Where(e=>e.Kind==ResourceFlowKind.Destroyed&&(e.Resource==Resource(game,"oil")||e.Resource==Resource(game,"treated-oil")))
                .Aggregate(System.Numerics.BigInteger.Zero,(sum,e)=>sum+e.AmountRaw);
            return total+Fix.FromRaw(checked((long)destroyed));
        }
        private static IncidentLifecycleView Incident(SimGame game,EntityId target)
        {if(!game.Seat(0).View.Feature<IncidentsView>().TryGetLifecycle(target,new Code("leak"),out var value))throw new InvalidOperationException("missing actual spill episode");return value;}
        private static List<WorkJobView> Jobs(SimGame game)
        {var rows=new List<WorkJobView>();game.Seat(0).View.Feature<WorkView>().CopyJobs(rows);return rows;}
        private static void Step(Check c,SimGame game,SimGame twin=null)
        {
            game.Update();twin?.Update();if(game.Fault!=null||twin?.Fault!=null)throw new InvalidOperationException("spill fault "+game.Fault+"/"+twin?.Fault);
            c.Equal(Fix.FromInt(5),Total(game),"original raw + spill + cargo + WIP + treated material + explicit disposal accounted");
            if(twin!=null)c.Equal(game.Hash(),twin.Hash(),"saved original resource/incident/motion/processor owners continue identically");
        }
        private static void Until(Check c,SimGame game,SimGame twin,Func<bool> ready,string stage)
        {
            var elapsed=System.Diagnostics.Stopwatch.StartNew();int updates=0;
            while(updates<240&&!ready()&&elapsed.Elapsed<TimeSpan.FromSeconds(20)){Step(c,game,twin);updates++;}
            if(!ready()){
                var process=Process(game);
                throw new InvalidOperationException(stage+" clock="+game.Clock.StepIndex+" updates="+updates+" elapsed-ms="+elapsed.ElapsedMilliseconds+
                    " spill="+Spill(game)+" jobs="+string.Join(",",Jobs(game).Select(job=>job.Id))+" process="+process.Phase+"/"+process.Blocker+" progress="+process.Progress.WholeTicks);
            }
        }
        private static SimGame Save(Check c,GameFactory factory,SimGame game)
        {var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"original spill/material/claim checkpoint");return twin;}
        private static CommandResult Send(Check c,SimGame game,Command command)
        {ulong hash=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(command),"ordinary physical spill command admission");c.Equal(hash,game.Hash(),"preview changes no resources or episode");var result=TestKit.Order(game,0,command);c.That(result.Accepted,"engine command "+result.Refusal);if(!result.Accepted)throw new InvalidOperationException("spill command "+result.Refusal);return result;}
        private static void Admission(Check c)
        {
            foreach(bool native in new[]{false,true})foreach(bool full in new[]{false,true})
            {
                var factory=IndependentTileWorkTests.Factory(nativeTiles:native,spillResources:true,authorSpillResources:json=>{
                    if(full)json.Get("tile_layer_setups")[0].Set("value",JsonValue.Number("100"));
                    else json.Get("entities")[1].Get("components").Get("storage").Set("initial_stock",Json.Parse("{\"oil\":2}"));
                });
                var game=New(factory);var site=Id(game,"sensor.spill-site");var source=Id(game,"sensor.incident-source");
                Send(c,game,new RaiseIncidentCommand{Target=source,Source=new Code("leak"),Kind=new Code("sensor.fire")});
                for(int i=0;i<8;i++){game.Update();c.That(game.Fault==null,"blocked exposure is a normal wait, not an engine fault");}
                c.That(!Incident(game,site).Snapshot.Active&&Incident(game,site).Snapshot.Episode==0,"unavailable physical spill raises no target episode");
                c.Equal(Fix.FromInt(full?100:0),Spill(game),"refusal leaves actual spill layer unchanged");
                c.Equal(Fix.FromInt(full?5:2),Stock(game,site,"oil"),"refusal consumes no local material");
                ulong hash=game.Hash();
                c.Equal(Code.None,game.Seat(0).View.Feature<IncidentPropagationView>().ReadEligibility(source,new Code("spread"),site,out var eligibility),"engine exposes exact blocked exposure facts");
                c.Equal(full?WorkEffectCodes.IncompatibleInventory:WorkEffectCodes.NoStock,eligibility.Observation.Blocker,"typed blocker distinguishes full cell from missing stock");
                c.Equal(hash,game.Hash(),"blocked exposure diagnostics do not mutate authoritative state");
                Save(c,factory,game);
            }
        }
        private static ObjectiveSnapshot Objective(SimGame game,string name)
        {var rows=new List<ObjectiveSnapshot>();game.Seat(0).View.Feature<ObjectiveView>().CopyObjectives(rows);return rows.Single(row=>row.Objective==name);}
        private static void Backlog(Check c,SimGame game,int amount)
        {
            ulong hash=game.Hash();var view=game.Seat(0).View.Feature<EnvironmentalMetricsView>();
            var value=view.Read(new PlayerId(0),"sensor.spill-backlog");
            c.That(value.Available&&value.Measure==EnvironmentalMeasure.Stock,"current material backlog is an explicit stock measurement");
            c.Equal(new System.Numerics.BigInteger(Fix.FromInt(amount).Raw),value.AmountRaw.Value,"public engine metric reads actual spill material");
            c.Equal(game.Clock.StepIndex,value.FromTick,"current stock has current game-clock coverage");
            c.Equal(value.FromTick,value.ThroughTick,"stock is not a cumulative flow window");
            c.That(!value.Rate.HasValue&&value.RateBlocker==EnvironmentalCodes.StockHasNoRate,"current stock never fabricates a throughput");
            var external=view.Read(new PlayerId(0),"sensor.extension-backlog");
            c.That(external.Available&&external.Measure==value.Measure&&external.AmountRaw==value.AmountRaw&&external.Facts==value.Facts&&external.RateBlocker==value.RateBlocker,
                "direct external definition shares actual cell/metric algorithms without native inheritance");
            foreach(var refused in new[]{view.Read(new PlayerId(1),"sensor.spill-backlog"),
                game.Seat(1).View.Feature<EnvironmentalMetricsView>().Read(new PlayerId(1),"sensor.spill-backlog")})
                c.That(!refused.Available&&!refused.Unit.IsValid&&!refused.Measure.HasValue&&refused.FromTick==0&&refused.ThroughTick==0,
                    "foreign/private refusal withholds material, type, unit and timestamps");
            c.Equal(hash,game.Hash(),"private current-stock observations do not mutate gameplay");
        }
        private static void EnvironmentAdmission(Check c)
        {
            c.Equal(typeof(Def),typeof(Beep.SimEngine.ExtensionProof.IndependentSpatialEnvironmentalMetricDef).BaseType,"independent metric has no native metric base");
            foreach(string invalid in new[]{"missing","empty","duplicate","unit","rate","window"})
                c.Throws<ContentException>(()=>New(IndependentTileWorkTests.Factory(spillEnvironment:true,authorSpillEnvironment:json=>{
                    var metric=json.Get("spatial_environmental_metrics")[0];
                    if(invalid=="missing")metric.Set("cells",Json.Parse("[\"missing.cell\"]"));
                    else if(invalid=="empty")metric.Set("cells",Json.Parse("[]"));
                    else if(invalid=="duplicate")metric.Set("cells",Json.Parse("[\"sensor.spill-cell\",\"sensor.spill-cell\"]"));
                    else if(invalid=="unit")metric.Set("unit",JsonValue.Of("unit.work-rate"));
                    else if(invalid=="rate")metric.Set("rate_unit",JsonValue.Of("minute"));
                    else metric.Set("window",Json.Parse("{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"}"));
                })),"malformed actual current-stock metadata refuses before play");
            foreach(bool native in new[]{false,true}){
                var factory=IndependentTileWorkTests.Factory(nativeTiles:native,spillEnvironment:true);var game=New(factory);
                Backlog(c,game,0);c.That(Objective(game,"sensor.spill-recovered").State!=ObjectiveState.Completed,"empty initial cell cannot skip prerequisite spill detection");
                Step(c,game);Backlog(c,game,0);Save(c,factory,game);
                c.That(Objective(game,"sensor.spill-treated").State==ObjectiveState.Locked,"treatment objective waits for physical recovery prerequisite");
            }
        }
        private static void History(Check c,SimGame game,int deposited,int recovered)
        {
            ulong hash=game.Hash();var view=game.Seat(0).View.Feature<EnvironmentalMetricsView>();
            foreach(var expected in new[]{(Metric:"sensor.spilled-total",Amount:deposited),(Metric:"sensor.recovered-total",Amount:recovered)}){
                var value=view.Read(new PlayerId(0),expected.Metric);
                c.That(value.Available&&value.Measure==EnvironmentalMeasure.Flow&&value.FromTick==0,"scoped cumulative transfer is an available lifetime flow");
                c.Equal(new System.Numerics.BigInteger(Fix.FromInt(expected.Amount).Raw),value.AmountRaw.Value,"original attributed journal keeps exact directed quantity");
                var empty=game.Seat(1).View.Feature<EnvironmentalMetricsView>().Read(new PlayerId(1),expected.Metric);
                c.That(empty.Available&&empty.AmountRaw.Value.IsZero&&empty.Facts==0,"another company cannot acquire this company's historical transfer facts");
            }
            var foreign=game.Seat(1).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            c.That(foreign.Status==ResourceFlowReadStatus.NotOwned&&foreign.Aggregates.Count==0&&foreign.Latest==0,"attributed summaries withhold foreign endpoint history and watermarks");
            c.Equal(hash,game.Hash(),"scoped cumulative/history reads remain pure");
        }
        private static ActivityNodeView DisposalNode(SimGame game,int order)
        {var rows=new List<ActivityNodeView>();if(!game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order,rows))throw new InvalidOperationException("missing actual disposal graph");return rows.Single(n=>n.Id=="dispose");}
        private static void Dispose(Check c,GameFactory factory,SimGame game,ref SimGame twin,EntityId site,EntityId worker,bool native,
            Action<Check,SimGame,int,bool> partialProbe=null,bool interruptedCargo=false)
        {
            var destination=Id(game,"sensor.disposal");var command=new PostOrderCommand{Plan="sensor.dispose-work",Target=destination,Priority=100};
            ulong hash=game.Hash();c.Equal(WorkEffectCodes.NoStock,game.Seat(0).Preview(command),"empty local disposal facility cannot begin productive work");
            c.Equal(StandardCodes.WrongTarget,game.Seat(0).Preview(new PostOrderCommand{Plan="sensor.dispose-work",Target=site}),"authored facility targeting prohibits remote disposal at arbitrary storage");
            c.Equal(hash,game.Hash(),"refused disposal previews change no stock, jobs or time");
            int haul=checked((int)Send(c,game,new HaulResourcesCommand{Source=site,Destination=destination,DestinationPort=new Code("material"),
                Resource=Resource(game,"oil"),Ceiling=Fix.FromInt(3),Worker=worker,Priority=100}).Arg0);
            twin=Save(c,factory,game);Until(c,game,twin,()=>Jobs(game).Any(j=>j.Id==haul&&j.Loaded.IsPositive),"recovered waste physically loads into actual carrier");twin=Save(c,factory,game);
            if(interruptedCargo){
                Until(c,game,twin,()=>Jobs(game).Any(j=>j.Id==haul&&j.Loaded==Fix.FromInt(3)&&j.ToilIndex==2),"full recovered waste pickup before player interruption");
                InterruptedCargo(c,factory,game,ref twin,worker,destination,haul);
            }
            Until(c,game,twin,()=>Stock(game,destination,"oil")==Fix.FromInt(3)&&Jobs(game).All(j=>j.Id!=haul),"physical travel unloading and departure at disposal facility");
            int order=checked((int)Send(c,game,command).Arg0);twin=Save(c,factory,game);
            Until(c,game,twin,()=>{var node=DisposalNode(game,order);return node.Progress.WholeTicks>0&&node.Progress.WholeTicks<node.RequiredTicks;},"earned partial disposal machine work");
            c.Equal(Fix.FromInt(3),Stock(game,destination,"oil"),"partial work cannot delete local material");
            string effectId=native?"sensor.dispose-oil":"sensor.dispose-oil-independent";
            partialProbe?.Invoke(c,game,order,native);
            twin=Save(c,factory,game);game.Clock.Pause();twin.Clock.Pause();hash=game.Hash();Step(c,game,twin);
            c.Equal(hash,game.Hash(),"one paused clock freezes pending disposal");game.Clock.Resume();twin.Clock.Resume();
            var progress=DisposalNode(game,order).Progress;
            Send(c,game,new ConfigureWorkPolicyCommand{Worker=worker,Settings=new WorkPolicySettings(false,1,new[]{new WorkerRoleId("sensor.inspect"),new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
            twin=Save(c,factory,game);for(int i=0;i<3;i++)Step(c,game,twin);
            c.Equal(progress,DisposalNode(game,order).Progress,"disabled machine earns no disposal progress");
            Send(c,game,new CancelOrderCommand{Order=order});twin=Save(c,factory,game);
            c.Equal(Fix.FromInt(3),Stock(game,destination,"oil"),"cancelled partial disposal retains every local unit");
            var metric=game.Seat(0).View.Feature<EnvironmentalMetricsView>();
            c.That(metric.Read(new PlayerId(0),"sensor.disposed-total").AmountRaw.Value.IsZero,"cancelled work records no disposal fact");
            Send(c,game,new ConfigureWorkPolicyCommand{Worker=worker,Settings=new WorkPolicySettings(true,1,new[]{new WorkerRoleId("sensor.inspect"),new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
            order=checked((int)Send(c,game,new PostOrderCommand{Plan="sensor.dispose-work",Target=destination,Priority=100}).Arg0);twin=Save(c,factory,game);
            Until(c,game,twin,()=>Stock(game,destination,"oil").IsZero,"complete four-minute disposal commits actual lifecycle sink");
            Until(c,game,twin,()=>Objective(game,"sensor.spill-disposed").State==ObjectiveState.Completed,"engine disposal objective consumes committed history");
            var disposed=metric.Read(new PlayerId(0),"sensor.disposed-total");
            c.That(disposed.Available&&disposed.Measure==EnvironmentalMeasure.Flow&&disposed.AmountRaw==new System.Numerics.BigInteger(Fix.FromInt(3).Raw)&&disposed.Facts==1,"exactly one real three-unit destruction is classified as disposal");
            c.Equal(Fix.FromInt(2),Stock(game,site,"oil"),"unspilled material remains outside the disposal facility");
            Backlog(c,game,0);for(int i=0;i<4;i++)Step(c,game,twin);
            c.Equal(disposed.AmountRaw,metric.Read(new PlayerId(0),"sensor.disposed-total").AmountRaw,"settled disposal cannot repeatedly consume or report material");
            var facts=game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates;
            c.That(facts.Any(f=>f.Resource==Resource(game,"oil")&&f.Kind==ResourceFlowKind.Destroyed&&f.Cause==new Code(effectId)&&
                f.Attribution.Endpoint==ResourceEndpointId.ForEntity(destination)&&f.Facts==1&&f.AmountRaw==Fix.FromInt(3).Raw),"historical sink identifies the actual facility and authored effect");
            var foreign=game.Seat(1).View.Feature<EnvironmentalMetricsView>().Read(new PlayerId(0),"sensor.disposed-total");
            c.That(!foreign.Available&&!foreign.Unit.IsValid,"foreign seat cannot read this company's disposal amount");Save(c,factory,game);
        }
        private static void Run(Check c,bool native,bool environment=false,bool history=false,bool disposal=false,
            Action<Check,SimGame,int,bool> partialDisposalProbe=null,bool privateRevision=false,bool interruptedCargo=false)
        {
            if(disposal)foreach(string invalid in new[]{"kind","empty","instant"})
                c.Throws<ContentException>(()=>New(IndependentTileWorkTests.Factory(nativeTiles:native,spillDisposal:true,authorSpillDisposal:json=>{
                    if(invalid=="kind")json.Get("inventory_effects")[0].Set("kind",JsonValue.Of("teleport"));
                    else if(invalid=="empty")json.Get("inventory_effects")[0].Set("quantities",Json.Parse("{}"));
                    else json.Get("entity_activities")[0].Get("nodes")[0].Set("duration",Json.Parse("{\"numerator\":0,\"denominator\":1,\"unit\":\"minute\"}"));
                })),"invalid disposal metadata or instant ancestry refuses before play");
            if(history)foreach(string invalid in new[]{"cell","direction","missing-selector","resource","cause","mixed-kinds"})
                c.Throws<ContentException>(()=>New(IndependentTileWorkTests.Factory(nativeTiles:native,spillHistory:true,authorSpillHistory:json=>{
                    var metric=json.Get("environmental_metrics")[0];
                    if(invalid=="cell")metric.Get("transfer").Set("cell",JsonValue.Of("missing.cell"));
                    else if(invalid=="direction")metric.Get("transfer").Set("direction",JsonValue.Of("invent"));
                    else if(invalid=="missing-selector")metric.Remove("transfer");
                    else if(invalid=="resource")metric.Set("resources",Json.Parse("[\"steel\"]"));
                    else if(invalid=="cause")metric.Set("causes",Json.Parse("[\"sensor.treat-oil\"]"));
                    else metric.Set("kinds",Json.Parse("[\"lost\",\"stock_transfer\"]"));
                })),"invalid scoped historical selection refuses before play");
            foreach(string reason in new[]{"operation","cell","resource"})c.Throws<ContentException>(()=>New(IndependentTileWorkTests.Factory(nativeTiles:native,spillResources:true,
                authorSpillResources:json=>{
                    var effect=json.Get("spatial_resource_transfer_effects")[0];
                    if(reason=="operation")effect.Set("operation",JsonValue.Of("invent"));
                    else if(reason=="cell")effect.Set("cell",JsonValue.Of("missing.cell"));
                    else effect.Set("quantities",Json.Parse("{\"steel\":3}"));
                })),"invalid actual spill transfer metadata refuses before play");
            var factory=IndependentTileWorkTests.Factory(nativeTiles:native,spillResources:true,spillEnvironment:environment,spillHistory:history,spillDisposal:disposal,independentDisposal:disposal&&!native,
                authorSpillResources:interruptedCargo&&!disposal?json=>json.Get("entities")[0].Get("components").Get("storage").Set("public",JsonValue.Of(true)):null,
                authorSpillDisposal:interruptedCargo&&disposal?json=>json.Get("entities")[0].Get("components").Get("storage").Set("public",JsonValue.Of(true)):null,
                additionalModule:interruptedCargo?new LogisticsRecoveryPolicyModule():null,
                additionalDeclaration:interruptedCargo?"""{"id":"sim.logistics-recovery","version":"1.0.0","requires":[{"id":"sim.logistics","version":"1.0.0"}],"documents":["spill-cargo-recovery.json"]}""":null,
                additionalDocument:interruptedCargo?Beep.SimEngine.ExtensionProof.FrameworkProofContent.Read("spill-cargo-recovery.json"):null,
                authorScenario:interruptedCargo?scenario=>scenario.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId,Json.Parse("{\"profile\":\"spill.cargo-recovery\"}")):null);
            var game=New(factory);var idle=New(factory);ulong isolated=idle.Hash();
            c.That(!game.Modules.Contains("sim.tycoon"),"spill/recovery/processing requires no business mode");
            c.Equal(Fix.FromInt(5),Total(game),"only file-authored finite liquid material exists");c.Equal(Fix.Zero,Spill(game),"resource-quantity spill cell starts empty");
            if(environment)Backlog(c,game,0);
            if(history)History(c,game,0,0);
            var site=Id(game,"sensor.spill-site");var source=Id(game,"sensor.incident-source");var worker=Id(game,"sensor.cart");var treatment=Id(game,"sensor.treatment");
            var limits=new ReplayLimits(4*1024*1024,6000);using var recording=game.RecordReplay(limits);
            Send(c,game,new RaiseIncidentCommand{Target=source,Source=new Code("leak"),Kind=new Code("sensor.fire")});var twin=Save(c,factory,game);
            c.Equal(Fix.Zero,Spill(game),"source incident cannot spill target material before delayed exposure");
            int ownRevision=privateRevision?Revision(game,0):0;
            int foreignRevision=privateRevision?Revision(game,1):0;
            int ownTwinRevision=privateRevision?Revision(twin,0):0;
            int foreignTwinRevision=privateRevision?Revision(twin,1):0;
            var ownChanges=privateRevision?Changes(game).ReadChanges().Cursor:null;
            var foreignChanges=privateRevision?Changes(game,1).ReadChanges().Cursor:null;
            var twinOwnChanges=privateRevision?Changes(twin).ReadChanges().Cursor:null;
            var twinForeignChanges=privateRevision?Changes(twin,1).ReadChanges().Cursor:null;
            Until(c,game,twin,()=>Incident(game,site).Snapshot.Active,"delayed target spill");
            if(privateRevision){
                c.That(Revision(game,0)>ownRevision,"owner observes actual private deposit invalidation");
                c.Equal(foreignRevision,Revision(game,1),"another company learns no private deposit through world revision");
                c.That(Revision(twin,0)>ownTwinRevision,"restored owner observes actual private deposit invalidation");
                c.Equal(foreignTwinRevision,Revision(twin,1),"restored foreign view learns no private deposit through world revision");
                ChangedAt(c,game,ownChanges,new TileCoord(14,20));UnchangedRegions(c,game,1,foreignChanges);
                ChangedAt(c,twin,twinOwnChanges,new TileCoord(14,20));UnchangedRegions(c,twin,1,twinForeignChanges);
            }
            c.Equal(Fix.FromInt(3),Spill(game),"three existing units enter original quantity cell");c.Equal(Fix.FromInt(2),Stock(game,site,"oil"),"same transfer debits actual local tank");
            if(history)History(c,game,3,0);
            if(environment){Backlog(c,game,3);Until(c,game,twin,()=>Objective(game,"sensor.spill-detected").State==ObjectiveState.Completed,"engine objective detects actual deposited stock");
                c.That(Objective(game,"sensor.spill-recovered").State!=ObjectiveState.Completed,"spill detection cannot finish recovery while material remains");}
            twin=Save(c,factory,game);for(int i=0;i<4;i++)Step(c,game,twin);c.Equal(Fix.FromInt(3),Spill(game),"active episode cannot duplicate spill transfer");
            PersistentTerrainCleanupTests.Clean(c,factory,game,ref twin,source);PersistentTerrainCleanupTests.Clean(c,factory,game,ref twin,site);
            c.Equal(Fix.FromInt(3),Spill(game),"logical terrain cleanup cannot delete resource-quantity spill stock");c.Equal(Fix.FromInt(5),Total(game),"response materials do not replace or erase spilled liquid");
            if(environment)Backlog(c,game,3);
            if(history)History(c,game,3,0);
            int recovery=checked((int)Send(c,game,new PostOrderCommand{Plan="sensor.recover-spill-work",Target=site,Priority=100}).Arg0);twin=Save(c,factory,game);
            ownRevision=privateRevision?Revision(game,0):0;
            foreignRevision=privateRevision?Revision(game,1):0;
            var nodes=new List<ActivityNodeView>();
            Until(c,game,twin,()=>game.Seat(0).View.Feature<WorkView>().TryCopyNodes(recovery,nodes)&&nodes.Any(n=>n.Progress.WholeTicks>0&&n.Progress.WholeTicks<n.RequiredTicks),"positive-time machine recovery");
            c.Equal(Fix.FromInt(3),Spill(game),"partial recovery cannot instantly extract stock");twin=Save(c,factory,game);
            game.Clock.Pause();twin.Clock.Pause();ulong paused=game.Hash();Step(c,game,twin);c.Equal(paused,game.Hash(),"one paused clock freezes actual spill recovery");game.Clock.Resume();twin.Clock.Resume();
            ownTwinRevision=privateRevision?Revision(twin,0):0;
            foreignTwinRevision=privateRevision?Revision(twin,1):0;
            ownChanges=privateRevision?Changes(game).ReadChanges().Cursor:null;
            foreignChanges=privateRevision?Changes(game,1).ReadChanges().Cursor:null;
            twinOwnChanges=privateRevision?Changes(twin).ReadChanges().Cursor:null;
            twinForeignChanges=privateRevision?Changes(twin,1).ReadChanges().Cursor:null;
            Until(c,game,twin,()=>Spill(game).IsZero,"earned physical cell withdrawal");c.Equal(Fix.FromInt(5),Stock(game,site,"oil"),"recovery returns original material exactly once to actual site");
            if(history)History(c,game,3,3);
            if(privateRevision){
                c.That(Revision(game,0)>ownRevision,"owner observes actual earned private withdrawal invalidation");
                c.Equal(foreignRevision,Revision(game,1),"another company learns no earned private withdrawal through world revision");
                c.That(Revision(twin,0)>ownTwinRevision,"partly completed restored work invalidates its actual owner at withdrawal");
                c.Equal(foreignTwinRevision,Revision(twin,1),"partly completed restored work never notifies the foreign viewer");
                ChangedAt(c,game,ownChanges,new TileCoord(14,20));UnchangedRegions(c,game,1,foreignChanges);
                ChangedAt(c,twin,twinOwnChanges,new TileCoord(14,20));UnchangedRegions(c,twin,1,twinForeignChanges);
            }
            if(environment){Backlog(c,game,0);Until(c,game,twin,()=>Objective(game,"sensor.spill-recovered").State==ObjectiveState.Completed,"engine objective observes earned resource recovery");
                c.That(Objective(game,disposal?"sensor.spill-disposed":"sensor.spill-treated").State!=ObjectiveState.Completed,"recovered material cannot finish its final industrial objective early");}
            var recoveredFlows=game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            c.Equal(new System.Numerics.BigInteger(Fix.FromInt(3).Raw),recoveredFlows.Aggregates.Where(e=>e.Resource==Resource(game,"oil")&&e.Kind==ResourceFlowKind.StockTransfer&&
                e.Attribution.Destination==new ResourceEndpointId(SpatialResourcesModule.ModuleId,2)).Aggregate(System.Numerics.BigInteger.Zero,(sum,e)=>sum+e.AmountRaw),"one actual spill leg is journaled, not generation or loss");
            c.Equal(new System.Numerics.BigInteger(Fix.FromInt(3).Raw),recoveredFlows.Aggregates.Where(e=>e.Resource==Resource(game,"oil")&&e.Kind==ResourceFlowKind.StockTransfer&&
                e.Attribution.Source==new ResourceEndpointId(SpatialResourcesModule.ModuleId,2)).Aggregate(System.Numerics.BigInteger.Zero,(sum,e)=>sum+e.AmountRaw),"one actual recovered withdrawal is journaled");
            if(disposal){
                Dispose(c,factory,game,ref twin,site,worker,native,partialDisposalProbe,interruptedCargo);c.Equal(Fix.FromInt(5),Total(game),"remaining material plus original journaled disposal closes the finite balance");
                c.Equal(isolated,idle.Hash(),"interleaved session receives no disposal or recovered cargo");Save(c,factory,game);
                if(interruptedCargo){
                    var facts=game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates;
                    c.That(!facts.Any(row=>row.Resource==Resource(game,"oil")&&(row.Kind==ResourceFlowKind.Lost||row.Kind==ResourceFlowKind.Consumed||row.Kind==ResourceFlowKind.Produced)),
                        "recovered cargo reaches only its authored destruction sink, never fabricated loss, treatment or production");
                    c.Equal(17,recording.CommandCount,"initial incident, two cleanup deliveries with work controls, recovery, haul, cargo hold/release/demand and disposal cancellation/control inputs all recorded");
                    c.Equal(game.Clock.StepIndex-recording.InitialStep,(long)recording.CheckpointCount,"complete physical spill/cargo/disposal recording spans every advancing update");
                }
                var verification=new ReplayVerifier().Verify(factory,recording.Finish(),limits);c.That(verification.Success&&verification.ActualHash==game.Hash(),"normal physical disposal replay "+verification.Failure+"/"+verification.Detail);
                if(interruptedCargo)Console.WriteLine("interrupted spill disposal replay: native="+native+";initial="+recording.InitialStep+";final="+game.Clock.StepIndex
                    +";commands="+recording.CommandCount+";checkpoints="+recording.CheckpointCount+";expected-hash="+game.Hash().ToString("X16")+";actual-hash="+verification.ActualHash.ToString("X16"));
                return;
            }
            int haul=checked((int)Send(c,game,new HaulResourcesCommand{Source=site,Destination=treatment,DestinationPort=new Code("material"),Resource=Resource(game,"oil"),Ceiling=Fix.FromInt(3),Worker=worker,Priority=100}).Arg0);
            twin=Save(c,factory,game);Until(c,game,twin,()=>Jobs(game).Any(j=>j.Id==haul&&j.Loaded.IsPositive),"recovered liquid physically loaded");twin=Save(c,factory,game);
            if(interruptedCargo){
                Until(c,game,twin,()=>Jobs(game).Any(j=>j.Id==haul&&j.Loaded==Fix.FromInt(3)&&j.ToilIndex==2),"full original pickup before player interruption");
                InterruptedCargo(c,factory,game,ref twin,worker,treatment,haul);
            }
            Until(c,game,twin,()=>Stock(game,treatment,"oil")==Fix.FromInt(3)&&Jobs(game).All(j=>j.Id!=haul),"real travel/unloading/departure to treatment");
            Send(c,game,new EnqueueTransformationCommand{Transformer=treatment,Capability=new Code("treat"),Transformation="sensor.treat-oil",Count=1});twin=Save(c,factory,game);
            Until(c,game,twin,()=>Process(game).Phase==TransformationPhase.Working&&Process(game).Progress.WholeTicks>0&&Process(game).Progress.WholeTicks<Process(game).Required,"actual five-minute treatment WIP");
            c.Equal(Fix.FromInt(3),Process(game).Wip.Stock.AmountOf(Resource(game,"oil")),"protected original WIP holds actual recovered material");
            c.Equal(Fix.Zero,Stock(game,treatment,"treated-oil"),"partial treatment produces no instant result");twin=Save(c,factory,game);
            Until(c,game,twin,()=>Stock(game,treatment,"treated-oil")==Fix.FromInt(3)&&Process(game).Queue.Count==0,"earned complete treatment conversion");
            if(environment){Until(c,game,twin,()=>Objective(game,"sensor.spill-treated").State==ObjectiveState.Completed,"original objective system consumes committed treatment fact");
                Backlog(c,game,0);var treated=game.Seat(0).View.Feature<EnvironmentalMetricsView>().Read(new PlayerId(0),"sensor.treated-total");
                c.That(treated.Available&&treated.Measure==EnvironmentalMeasure.Flow&&treated.AmountRaw==new System.Numerics.BigInteger(Fix.FromInt(3).Raw)&&treated.Facts==1&&treated.Rate.HasValue,
                    "existing journal metric reports one actual treatment flow, not remaining spill stock");}
            c.Equal(Fix.FromInt(2),Stock(game,site,"oil"),"unspilled liquid remains in original site");c.Equal(Fix.FromInt(5),Total(game),"final raw plus treated total is conserved");
            var flows=game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            var consumed=flows.Aggregates.Where(e=>e.Resource==Resource(game,"oil")&&e.Kind==ResourceFlowKind.Consumed).ToArray();
            var produced=flows.Aggregates.Where(e=>e.Resource==Resource(game,"treated-oil")&&e.Kind==ResourceFlowKind.Produced).ToArray();
            c.That(consumed.Length==1&&produced.Length==1,"one original conversion classifies actual raw consumption and treated production");
            if(consumed.Length!=1||produced.Length!=1)throw new InvalidOperationException("expected one original treatment conversion fact pair");
            c.That(consumed[0].Facts==1&&produced[0].Facts==1,"saved WIP completion never repeats either conversion fact");
            c.That(consumed[0].AmountRaw==Fix.FromInt(3).Raw&&produced[0].AmountRaw==consumed[0].AmountRaw,"three actual recovered units become three authored treated units");
            c.That(consumed[0].Cause==new Code("sensor.treat-oil")&&produced[0].Cause==consumed[0].Cause,"both material dispositions retain the actual recipe identity");
            c.Equal(Process(game).Wip.Endpoint,consumed[0].Attribution.Endpoint,"consumption belongs to the actual original WIP endpoint, not a fictional site loss");
            c.Equal(ResourceEndpointId.ForEntity(treatment),produced[0].Attribution.Endpoint,"produced material belongs to the actual receiver inventory");
            c.That(!flows.Aggregates.Any(e=>(e.Resource==Resource(game,"oil")||e.Resource==Resource(game,"treated-oil"))&&
                (e.Kind==ResourceFlowKind.Lost||e.Kind==ResourceFlowKind.Destroyed)),"recovery and treatment add no silent loss or destruction sink");
            if(history){
                Until(c,game,twin,()=>Objective(game,"sensor.spill-accounted").State==ObjectiveState.Completed,"original objective consumes cumulative directed totals after eviction");
                History(c,game,3,3);Backlog(c,game,0);
                var page=game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0),0);
                c.That(page.Status==ResourceFlowReadStatus.Expired&&page.Entries.Count==2,"authored two-record journal really evicts early deposit/recovery facts");
                var metrics=game.Seat(0).View.Feature<EnvironmentalMetricsView>();
                c.Equal(EnvironmentalCodes.HistoryExpired,metrics.Read(new PlayerId(0),"sensor.recent-spilled").Blocker,"incomplete transfer window refuses rather than borrowing lifetime totals");
                var quiet=metrics.Read(new PlayerId(0),"sensor.quiet-spilled");
                c.That(quiet.Available&&quiet.AmountRaw.Value.IsZero,"complete recent quiet window is true zero despite nonzero lifetime spill");
                var restored=factory.Load(game.Save());History(c,restored,3,3);
                c.That(Objective(restored,"sensor.spill-accounted").State==ObjectiveState.Completed,"cumulative-history objective survives saved continuation");
            }
            c.Equal(isolated,idle.Hash(),"interleaved session receives no spill, cargo or treatment");Save(c,factory,game);
            var replay=new ReplayVerifier().Verify(factory,recording.Finish(),limits);c.That(replay.Success&&replay.Game?.Hash()==game.Hash(),"ordinary spill/recovery/treatment replay "+replay.Failure+"/"+replay.Detail);
        }
    }
}
