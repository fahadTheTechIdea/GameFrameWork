using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
    // Explicit file composition, public player inputs and immutable engine facts. No flow/rate rules here.
    internal static class CommittedThroughputTests
    {
        internal const string Case="Committed throughput: physical flow utilities and controls report private durable measured rates";
        private static readonly PlayerId Company=new PlayerId(0);
        private static readonly Code ProcessId=new Code("contract.process"), Probe=new Code("probe"), PoolId=new Code("pool");
        private static readonly ReplayLimits Limits=new ReplayLimits(4*1024*1024,3000);
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static OperationsView Operations(SimGame game,int seat=0)=>game.Seat(seat).View.Feature<OperationsView>();
        private static ResourceFlowView History(SimGame game)=>game.Seat(0).View.Feature<ResourceFlowView>();
        private static void Input(Check c,SimGame game,Command command)
        {
            ulong hash=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(command),"normal public command preview");c.Equal(hash,game.Hash(),"pure preview");
            game.Seat(0).Submit(command);Step(game);
        }
        private static void Reach(SimGame game,SimGame twin,Func<bool> ready,string stage)
        {
            for(int i=0;i<256&&!ready();i++)Step(game,twin);
            if(!ready())throw new InvalidOperationException(stage+" step="+game.Clock.StepIndex+" fault="+game.Fault);
        }
        private static void Pause(Check c,SimGame game,SimGame twin)
        {
            long at=game.Clock.StepIndex;ulong hash=game.Hash();game.Clock.Pause();twin.Clock.Pause();Step(game,twin);
            c.That(game.Clock.StepIndex==at&&game.Hash()==hash,"one paused game clock advances no throughput/control/work");game.Clock.Resume();twin.Clock.Resume();
        }
        private static ResourceFlowQuery Query(ResourceId resource,long from,long through,DurationUnit per=DurationUnit.Minute,
            ResourceEndpointId? source=null,ResourceEndpointId? destination=null,ResourceEndpointId? endpoint=null,
            Code? cause=null,string owner=null,ResourceFlowKind kind=ResourceFlowKind.StockTransfer,PlayerId? company=null)
            =>new ResourceFlowQuery(company??Company,resource,new[]{kind},from,through,per,source,destination,endpoint,cause,owner);
        private static void Measured(Check c,ResourceFlowObservation value,int amount,long facts,int numerator,int denominator,DurationUnit per)
        {
            c.That(value.Available&&value.Status==ResourceFlowReadStatus.Ready,"complete actual measured facts");
            c.Equal(new BigInteger(Fix.FromInt(amount).Raw),value.AmountRaw.Value,"committed amount not nominal allowance");
            c.Equal(facts,value.Facts,"exact selected committed fact count");c.That(value.Rate.HasValue&&value.RateBlocker.IsNone,"positive-age measured rate available");
            c.Equal(new MeasuredRate(numerator,denominator,per),value.Rate.Value,"exact reduced native-unit/game-time fraction");
        }
        private static Fix Pool(SimGame game)
        {
            var code=Operations(game).ReadSignal(Entity(game,"binding.watcher"),PoolId,"binding.stored",out var value);
            if(!code.IsNone||!value.Available)throw new InvalidOperationException("actual pool signal refused "+code+"/"+value.Blocker);return value.Value.Value;
        }
        private static Fix Steel(SimGame game,string id)
        {
            var rows=new List<Facet>();game.Seat(0).View.Inspect(Entity(game,id),rows);
            return rows.Where(x=>x.Key==Storage.FacetStock&&x.Subject=="steel").Aggregate(Fix.Zero,(sum,x)=>sum+x.Value);
        }
        private static GameFactory NetworkFactory(bool direct,bool halfMinute,int retained=64,string fault=null)
            =>NetworkSignalBindingTests.Create(new InitializeProbe(),out _,direct,
                edit:root=>{if(fault!=null)for(int i=0;i<root.Get("entities").Count;i++)root.Get("entities")[i].Get("components").Get("independent_network_signal").Set("metadata_fault",JsonValue.Of(fault));},
                editDefinitions:root=>{if(halfMinute)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));},
                editFlows:root=>root.Get("resource_flow_journals")[0].Set("retained_records_per_company",JsonValue.Number(retained.ToString(System.Globalization.CultureInfo.InvariantCulture))),
                extraModules:new GameModule[]{new EnvironmentalMetricsModule()},
                extraDeclarations:"[{\"id\":\"sim.environment-metrics\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\"}],\"documents\":[\"committed-throughput-metrics.json\"]}]",
                extraDocuments:new Dictionary<string,string>{{"committed-throughput-metrics.json",FrameworkProofContent.Read("committed-throughput-metrics.json")}});
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current measured-throughput build");c.Equal(154,EngineInfo.SaveFormat,"same sole journal/stock/work/control codecs");
            foreach(bool half in new[]{false,true})foreach(bool direct in new[]{false,true})Network(c,direct,half);
            foreach(bool direct in new[]{false,true})Utility(c,direct);
            Controls(c);Refusals(c);Faults(c);
        }
        private static void Network(Check c,bool direct,bool half)
        {
            var factory=NetworkFactory(direct,half);var game=NetworkIncidentPropagationTests.New(factory);var isolated=NetworkIncidentPropagationTests.New(factory);ulong other=isolated.Hash();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId),"physical flow/manufacturing have no business requirement");
            var journal=History(game);game.Seat(0).View.TryFindResource("steel",out var steel);var recorder=game.RecordReplay(Limits);
            var opening=journal.ReadThroughput(Query(steel,0,0));c.That(opening.Available&&opening.AmountRaw==BigInteger.Zero&&!opening.Rate.HasValue&&opening.RateBlocker==ResourceFlowObservation.NoElapsedTime,"no invented rate before elapsed game time");
            var twin=factory.Load(game.Save());Reach(game,twin,()=>Pool(game)==Fix.FromInt(8),"rate-limited physical network injections");
            long stopped=game.Clock.StepIndex;ulong hash=game.Hash();var infra=game.Seat(0).View.Feature<InfrastructureView>();
            var pools=new List<NetworkPoolView>();c.Equal(Code.None,infra.CopyPools("binding.main",pools),"actual connected pool identity");
            var transfers=journal.Read(Company,0).Entries.Where(x=>x.Item.Resource==steel&&x.Item.Kind==ResourceFlowKind.StockTransfer).ToArray();
            c.Equal(half?4:2,transfers.Length,"native quantum determines committed legs without another clock");
            var source=transfers[0].Item.Source.Endpoint;var endpoint=pools.Single().Endpoint;
            var query=Query(steel,0,stopped,source:source,destination:endpoint,cause:Code.None,owner:transfers[0].Item.Owner.Scope);
            var retained=journal.ReadThroughput(query);Measured(c,retained,8,half?4:2,4,1,DurationUnit.Minute);
            c.Equal(new UnitId("unit.steel"),retained.Unit,"actual native resource unit");
            Measured(c,journal.ReadThroughput(Query(steel,0,stopped,DurationUnit.Hour,destination:endpoint)),8,half?4:2,240,1,DurationUnit.Hour);
            Measured(c,journal.ReadThroughput(Query(steel,0,stopped,destination:source)),0,0,0,1,DurationUnit.Minute);
            Measured(c,journal.ReadThroughput(Query(steel,0,stopped,cause:new Code("not.this.flow"))),0,0,0,1,DurationUnit.Minute);
            Measured(c,journal.ReadThroughput(Query(steel,0,stopped,owner:"not.this.owner")),0,0,0,1,DurationUnit.Minute);
            var initialized=journal.ReadThroughput(Query(steel,0,stopped,endpoint:source,kind:ResourceFlowKind.Initialized));
            c.That(initialized.Available&&initialized.AmountRaw==new BigInteger(Fix.FromInt(30).Raw)&&initialized.Facts==1,"lifecycle endpoint filter never counts transfer as creation");
            c.Throws<NotSupportedException>(()=>((IList<ResourceFlowKind>)query.Kinds).Clear(),"query kinds immutable");
            c.Equal(Code.None,infra.ReadGates(Entity(game,"binding.source"),"binding.main",out var gates),"actual network readiness");
            c.Equal(new Code("binding.backpressure"),gates.Blocker,"real stock backpressure");
            c.Equal(Code.None,infra.ReadDemand("binding.main",Entity(game,"binding.source"),steel,false,out var demand),"same native flow admission");
            c.That(demand.Blocker==gates.Blocker&&demand.Maximum==Fix.Zero,"blocked readiness offers zero allowance, not measured historical zero");
            c.Equal(Fix.FromInt(30),Steel(game,"binding.source")+Pool(game),"actual finite source-to-pool stock conservation");
            var environment=game.Seat(0).View.Feature<EnvironmentalMetricsView>().Read(Company,"throughput.quiet-environment");
            c.That(environment.Available&&environment.Rate.HasValue&&environment.Rate.Value.Equals(new MeasuredRate(0,1,DurationUnit.Hour)),"environmental output uses same shared exact calculator");
            var foreign=game.Seat(1).View.Feature<ResourceFlowView>().ReadThroughput(query);
            c.That(foreign.Status==ResourceFlowReadStatus.NotOwned&&!foreign.AmountRaw.HasValue&&!foreign.Unit.IsValid&&!foreign.Rate.HasValue&&foreign.Facts==0,"no foreign throughput/unit/history leak");
            c.Equal(hash,game.Hash(),"complete rate/readiness/history observations pure");twin=factory.Load(game.Save());Pause(c,game,twin);
            for(int i=0;i<3;i++)Step(game,twin);
            Measured(c,journal.ReadThroughput(Query(steel,stopped,game.Clock.StepIndex,destination:endpoint)),0,0,0,1,DurationUnit.Minute);
            c.Equal(retained.Rate,journal.ReadThroughput(query).Rate,"fixed historical window stable after quiet time and pause");
            c.Equal(retained.Rate,History(twin).ReadThroughput(query).Rate,"saved network cursors/history produce identical measured rate");
            long begin=game.Clock.StepIndex;
            Input(c,game,new HaulResourcesCommand{Source=Entity(game,"depot"),Destination=Entity(game,"binding.watcher"),Worker=Entity(game,"truck"),Resource=steel,Ceiling=Fix.FromInt(5),Priority=90});
            c.Equal(Fix.Zero,Steel(game,"binding.watcher"),"hauling order grants no instant stock/throughput");
            twin=factory.Load(game.Save());Reach(game,twin,()=>Steel(game,"binding.watcher")==Fix.FromInt(5),"physically earned incoming delivery");
            var logistics=game.Seat(0).View.Feature<LogisticsView>();c.Equal(Code.None,logistics.ReadIncomingThroughput(Entity(game,"binding.watcher"),steel,begin,game.Clock.StepIndex,out var hauled),"same original physical incoming diagnostics");
            c.Equal(new BigInteger(Fix.FromInt(5).Raw),hauled.IncomingRaw,"five physically delivered units, no promise counted");
            // Use actual persisted owner scope from committed delivery, not a hardcoded worker classification.
            var delivered=journal.Read(Company,0).Entries.First(x=>x.Item.Resource==steel&&x.Item.IsTransfer&&x.Item.Destination.Endpoint==hauled.Endpoint).Item;
            var incoming=journal.ReadThroughput(Query(steel,begin,game.Clock.StepIndex,DurationUnit.Second,destination:hauled.Endpoint,owner:delivered.Owner.Scope));
            c.That(incoming.Available&&incoming.AmountRaw==hauled.IncomingRaw&&incoming.Facts==hauled.TransferLegs,"haul/native journal share one exact aggregation");
            c.Equal(hauled.RateRawNumerator*incoming.Rate.Value.Denominator,incoming.Rate.Value.Numerator*Fix.One.Raw*hauled.RateSecondsDenominator,"raw-Q32 hauling and native-unit generic rates agree exactly");
            c.Equal(Fix.FromInt(8),Pool(game),"local hauling does not fabricate connected pool stock");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"settled original state restores");c.Equal(other,isolated.Hash(),"interleaved session history isolated");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),Limits).Success,"normal inputs/physical rates replay");
            Retention(c,direct,half);
        }
        private static void Retention(Check c,bool direct,bool half)
        {
            var factory=NetworkFactory(direct,half,1);var game=NetworkIncidentPropagationTests.New(factory);game.Seat(0).View.TryFindResource("steel",out var steel);
            Reach(game,null,()=>Pool(game)==Fix.FromInt(8),"one-record authored retention flow");long at=game.Clock.StepIndex;var journal=History(game);
            var expired=journal.ReadThroughput(Query(steel,0,at));c.That(expired.Status==ResourceFlowReadStatus.Expired&&!expired.AmountRaw.HasValue&&!expired.Rate.HasValue,"eviction never reports partial quantity or false zero");
            Measured(c,journal.ReadThroughput(Query(steel,at-1,at)),half?2:4,1,4,1,DurationUnit.Minute);
            var twin=factory.Load(game.Save());Step(game,twin);Measured(c,journal.ReadThroughput(Query(steel,at,game.Clock.StepIndex)),0,0,0,1,DurationUnit.Minute);
            c.That(History(twin).ReadThroughput(Query(steel,0,at)).Status==ResourceFlowReadStatus.Expired,"saved retention watermark remains authoritative");
        }
        private static UtilityDemandSnapshot Demand(SimGame game)
        {
            if(!Operations(game).TryGetUtilityDemand(Entity(game,"contract.factory"),ProcessId,out var value))throw new InvalidOperationException("actual utility consumer absent");return value;
        }
        private static void Utility(Check c,bool direct)
        {
            var factory=UtilitySignalBindingTests.Create(new InitializeProbe(),out _,direct);var game=ConstructionPlanTests.NewPoints(factory);var recorder=game.RecordReplay(Limits);
            Input(c,game,new EnqueueTransformationCommand{Transformer=Entity(game,"contract.factory"),Capability=ProcessId,Transformation="contract.make"});
            Reach(game,null,()=>Demand(game).Drawing&&Demand(game).ElapsedTicks>=4,"timed actual utility use");var view=Operations(game);ulong hash=game.Hash();game.Seat(0).View.TryFindResource("supply",out var supply);
            var demand=Demand(game);c.That(demand.Satisfied&&demand.Consumed.AmountOf(supply)==Fix.One,"actual satisfied consumer burns one timed native unit");
            c.Equal(Code.None,view.ReadSignal(Entity(game,"contract.factory"),new Code("utility"),"binding.utility-ready",out var ready),"utility readiness signal");
            c.That(ready.Available&&ready.Value==Fix.One,"utility readiness reflects actual demand not platform logic");c.Equal(hash,game.Hash(),"utility reads pure");
            Input(c,game,new ConfigureOperationCommand{Entity=Entity(game,"contract.factory"),Operation=ProcessId,Enabled=false,Target=Fix.One});
            c.That(view.TryGetControl(Entity(game,"contract.factory"),ProcessId,out var control)&&!control.Enabled,"actual operation shutdown fact");
            var stopped=Demand(game);var twin=factory.Load(game.Save());Pause(c,game,twin);for(int i=0;i<3;i++)Step(game,twin);
            c.Equal(stopped.ElapsedTicks,Demand(game).ElapsedTicks,"saved disabled operation cannot earn productive utility exposure");
            Input(c,game,new ConfigureOperationCommand{Entity=Entity(game,"contract.factory"),Operation=ProcessId,Enabled=true,Target=Fix.One});
            Reach(game,null,()=>Demand(game).Blocker==UtilityCodes.Shortage,"finite utility fuel exhaustion");
            c.That(!Demand(game).Satisfied&&Demand(game).Consumed.AmountOf(supply)==Fix.FromInt(2),"unsatisfied utility reflects two actual burned units");
            c.Equal(Code.None,view.ReadSignal(Entity(game,"contract.factory"),new Code("utility"),"binding.utility-ready",out ready),"shortage signal read");
            c.That(ready.Available&&ready.Value==Fix.Zero,"shortage publishes actual false readiness");
            c.That(!Operations(game,1).TryGetUtilityDemand(Entity(game,"contract.factory"),ProcessId,out _),"utility facts private");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"utility shortage saves");c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),Limits).Success,"actual utility control input replay");
        }
        private static void Controls(Check c)
        {
            var factory=ControlContentContractTests.Factory(new InitializeProbe(),out _);var game=ControlContentContractTests.New(factory);var recorder=game.RecordReplay(Limits);
            var id=Entity(game,"contract.sensor");var view=Operations(game);
            Input(c,game,new ConfigureAutomationCommand{Rule="measured.control",Settings=ControlContentContractTests.Settings(game,true)});
            Func<AutomationRuleSnapshot> rule=()=>{if(!view.TryGetAutomationRule(Company,"measured.control",out var value))throw new InvalidOperationException("actual rule missing");return value;};
            Reach(game,null,()=>rule().HasApplied,"original deferred initial branch");
            Input(c,game,new SetAutomationProbeCommand{Entity=id,Level=Fix.FromInt(2),Available=true});Reach(game,null,()=>rule().PendingDecision==true,"threshold earns declared dwell");
            c.That(view.TryGetControl(id,Probe,out var control)&&control.Enabled,"pending is not applied control");
            var pending=rule();var twin=factory.Load(game.Save());Pause(c,game,twin);Step(game,twin);
            c.That(rule().Latch&&view.TryGetControl(id,Probe,out control)&&!control.Enabled,"next Update applies ordinary operation state");
            c.That(pending.PendingDecision==true,"retained decision detached");c.That(!Operations(game,1).TryGetControl(id,Probe,out _),"actual control private");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"applied control saves");c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),Limits).Success,"normal deferred controls replay");
        }
        private static void Refusals(Check c)
        {
            var game=NetworkIncidentPropagationTests.New(NetworkFactory(true,false));game.Seat(0).View.TryFindResource("steel",out var steel);var journal=History(game);
            c.Equal(ResourceFlowReadStatus.CursorAhead,journal.ReadThroughput(Query(steel,0,1)).Status,"future window refuses");
            var unknown=journal.ReadThroughput(Query(new ResourceId(int.MaxValue),0,0));c.That(unknown.Status==ResourceFlowReadStatus.UnknownResource&&!unknown.Unit.IsValid&&!unknown.AmountRaw.HasValue,"unknown resource does not invent native unit");
            c.Equal(ResourceFlowReadStatus.NoSuchCompany,game.Observer.Feature<ResourceFlowView>().ReadThroughput(Query(steel,0,0,company:new PlayerId(999))).Status,"observer unknown company typed refusal");
            c.Throws<ArgumentException>(()=>new ResourceFlowQuery(Company,steel,new ResourceFlowKind[0],0,0,DurationUnit.Minute),"empty kinds refuse");
            c.Throws<ArgumentException>(()=>new ResourceFlowQuery(Company,steel,new[]{ResourceFlowKind.StockTransfer,ResourceFlowKind.StockTransfer},0,0,DurationUnit.Minute),"duplicate kinds refuse");
            c.Throws<ArgumentException>(()=>Query(steel,2,1),"reverse window refuse");
            c.Throws<ArgumentException>(()=>Query(steel,0,0,(DurationUnit)255),"unknown rate unit refuse");
            c.Throws<ArgumentException>(()=>Query(steel,0,0,source:default(ResourceEndpointId)),"invalid endpoint filter refuse");
            c.Throws<ArgumentException>(()=>Query(steel,0,0,owner:""),"empty owner scope refuses");
            c.Equal(new MeasuredRate(2,3,DurationUnit.Second),new MeasuredRate(4,6,DurationUnit.Second),"general fraction reduction");
        }
        private static void Faults(Check c)
        {
            foreach(string fault in new[]{"live-throw","live-pause","live-touch","live-private"}) {
                var game=NetworkIncidentPropagationTests.New(NetworkFactory(true,false,fault:fault));game.Seat(0).View.TryFindResource("steel",out var steel);
                var journal=History(game);var foreign=game.Seat(1).View.Feature<ResourceFlowView>();var view=Operations(game);var id=Entity(game,"binding.source");
                game.Update();if(game.Fault==null)c.Throws<InvalidOperationException>(()=>view.ReadSignal(id,PoolId,"binding.stored",out _),"actual selected callback faults");
                c.That(game.Fault!=null,"original first fault latched");
                c.Throws<InvalidOperationException>(()=>journal.ReadThroughput(Query(steel,0,0)),"cached zero-age throughput rejects fault");
                c.Throws<InvalidOperationException>(()=>foreign.ReadThroughput(Query(steel,0,0)),"cached private throughput rejects fault");
                c.Throws<InvalidOperationException>(()=>journal.ReadThroughput(Query(new ResourceId(int.MaxValue),0,0)),"cached unknown resource rejects fault");
                c.Throws<InvalidOperationException>(()=>journal.ReadThroughput(null),"cached empty query rejects first fault before argument branch");
                c.Throws<InvalidOperationException>(()=>game.Save(),"faulted rates cannot checkpoint");
            }
        }
    }
}
