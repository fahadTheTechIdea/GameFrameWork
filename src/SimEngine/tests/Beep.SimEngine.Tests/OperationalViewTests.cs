using System;
using System.Collections.Generic;
using System.Linq;
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
    // Authored composition, player inputs and immutable facts only. No simulation rules here.
    internal static class OperationalViewTests
    {
        internal const string Case = "Operational views: private utility flow and deferred controls publish only healthy complete facts";
        private static readonly Code ProcessId = new Code("contract.process"), Probe = new Code("probe"), Second = new Code("probe.second");
        private static readonly PlayerId Company = new PlayerId(0);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static OperationsView View(SimGame game, int seat=0) => game.Seat(seat).View.Feature<OperationsView>();
        private static void Input(Check c, SimGame game, Command command)
        {
            ulong hash=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(command),"ordinary public preview");
            c.Equal(hash,game.Hash(),"preview does not advance or mutate");game.Seat(0).Submit(command);Step(game);
        }
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for(int i=0;i<256&&!ready();i++)Step(game,twin);
            if(!ready())throw new InvalidOperationException(stage+" step="+game.Clock.StepIndex+" fault="+game.Fault);
        }
        private static void Pause(Check c, SimGame game, SimGame twin)
        {
            ulong hash=game.Hash();long at=game.Clock.StepIndex;game.Clock.Pause();twin.Clock.Pause();Step(game,twin);
            c.That(hash==game.Hash()&&at==game.Clock.StepIndex,"one paused clock freezes authoritative state");game.Clock.Resume();twin.Clock.Resume();
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current diagnostics protocol");c.Equal(154,EngineInfo.SaveFormat,"same saved ownership");
            Utility(c,false);Utility(c,true);Network(c,false);Network(c,true);Controls(c);Faults(c);NetworkFaults(c);
        }
        private static UtilityDemandSnapshot Demand(SimGame game)
        {
            if(!View(game).TryGetUtilityDemand(Entity(game,"contract.factory"),ProcessId,out var value))throw new InvalidOperationException("missing original utility consumer");return value;
        }
        private static void UtilityFacts(Check c, SimGame game)
        {
            ulong hash=game.Hash();var demand=Demand(game);game.Seat(0).View.TryFindResource("supply",out var supply);
            var ids=new List<Code>();c.That(View(game).CopyUtilityConsumerIds(Entity(game,"contract.factory"),ids)&&ids.SequenceEqual(new[]{ProcessId}),"original named utility owner");
            foreach(string metric in new[]{"ready","requested","available","consumed"}) {
                c.Equal(Code.None,View(game).ReadSignal(Entity(game,"contract.factory"),new Code("utility"),"binding.utility-"+metric,out var signal),"authored utility signal");
                if(!demand.Drawing)c.That(!signal.Available&&signal.Blocker==UtilityCodes.Inactive,"inactive utility is typed, not invented stock");
                else if(!demand.NextBlocker.IsNone)c.That(!signal.Available&&signal.Blocker==demand.NextBlocker,"next-demand refusal remains actual");
                else {Fix expected=metric=="ready"?(demand.Satisfied?Fix.One:Fix.Zero):metric=="requested"?demand.NextInputs.AmountOf(supply):metric=="available"?demand.Available.AmountOf(supply):demand.Consumed.AmountOf(supply);
                    c.That(signal.Available&&signal.Value==expected,"utility signal derives original consumption/readiness "+metric);}
            }
            c.Equal(hash,game.Hash(),"complete utility reads pure");
        }
        private static void Utility(Check c,bool direct)
        {
            var factory=UtilitySignalBindingTests.Create(new InitializeProbe(),out var proof,direct);var game=ConstructionPlanTests.NewPoints(factory);
            var isolated=ConstructionPlanTests.NewPoints(factory);ulong other=isolated.Hash();
            c.That(proof.ActualBinding==direct&&!game.Modules.Contains(TycoonModule.ModuleId),"authored original utility binding without business");
            var limits=new ReplayLimits(4*1024*1024,3000);var recorder=game.RecordReplay(limits);UtilityFacts(c,game);
            c.That(!View(game,1).TryGetUtilityDemand(Entity(game,"contract.factory"),ProcessId,out _),"foreign utility consumer private");
            Fix opening=Stock(game,"source")+Stock(game,"machine")+Stock(game,"contract.factory");
            Input(c,game,new ConfigureWorkPolicyCommand{Worker=Entity(game,"machine"),Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("inspect"),new WorkerRoleId("haul")},null,true,true,WorkAssignmentMode.All)});
            Input(c,game,new EnqueueTransformationCommand{Transformer=Entity(game,"contract.factory"),Capability=ProcessId,Transformation="contract.make"});
            Reach(game,null,()=>Demand(game).Drawing&&Demand(game).ElapsedTicks>=4,"earned utility exposure");UtilityFacts(c,game);var retained=Demand(game);
            Input(c,game,new ConfigureOperationCommand{Entity=Entity(game,"contract.factory"),Operation=ProcessId,Enabled=false,Target=Fix.One});
            var stopped=Demand(game);var twin=factory.Load(game.Save());for(int i=0;i<4;i++)Step(game,twin);
            c.That(stopped.ElapsedTicks==Demand(game).ElapsedTicks&&Process(game).QueueCount==1,"shutdown retains work without free output");Pause(c,game,twin);
            Input(c,game,new ConfigureOperationCommand{Entity=Entity(game,"contract.factory"),Operation=ProcessId,Enabled=true,Target=Fix.One});
            Reach(game,null,()=>Demand(game).Blocker==UtilityCodes.Shortage,"actual finite utility shortage");UtilityFacts(c,game);
            game.Seat(0).View.TryFindResource("supply",out var supply);c.Equal(Fix.FromInt(2),Demand(game).Consumed.AmountOf(supply),"exact initial fuel consumed");
            twin=factory.Load(game.Save());long earned=Demand(game).ElapsedTicks;for(int i=0;i<3;i++)Step(game,twin);
            c.Equal(earned,Demand(game).ElapsedTicks,"saved shortage stalls productive time");
            Fix source=Stock(game,"source");Input(c,game,new HaulResourcesCommand{Source=Entity(game,"source"),Destination=Entity(game,"contract.factory"),DestinationPort=new Code("operating.input"),Worker=Entity(game,"machine"),Resource=supply,Ceiling=Fix.FromInt(8),Priority=90});
            c.Equal(Fix.Zero,Stock(game,"contract.factory"),"order is not instant refuel");twin=factory.Load(game.Save());
            Reach(game,twin,()=>Demand(game).Consumed.AmountOf(supply)>Fix.FromInt(2),"physical supplied utility resumes");UtilityFacts(c,game);
            c.Equal(source-Fix.FromInt(8),Stock(game,"source"),"physical source debit");
            Reach(game,twin,()=>Process(game).QueueCount==0,"original32-minute manufacture");UtilityFacts(c,game);
            c.Equal(Fix.FromInt(8),opening+Fix.One-Stock(game,"source")-Stock(game,"machine")-Stock(game,"contract.factory"),"one earned output, eight units consumed");
            c.That(retained.Drawing&&retained.ElapsedTicks>=4,"retained utility facts detached");c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"settled utility saves");
            c.Equal(other,isolated.Hash(),"utility sessions isolated");c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"utility public controls/haul replay");
        }
        private static AutomationSignalSnapshot Pool(SimGame game)
        {
            if(!View(game).ReadSignal(Entity(game,"binding.watcher"),new Code("pool"),"binding.stored",out var value).IsNone)throw new InvalidOperationException("pool refusal");return value;
        }
        private static void Network(Check c,bool direct)
        {
            var factory=NetworkSignalBindingTests.Create(new InitializeProbe(),out var proof,direct);var game=NetworkIncidentPropagationTests.New(factory);
            var isolated=NetworkIncidentPropagationTests.New(factory);ulong other=isolated.Hash();var infra=game.Seat(0).View.Feature<InfrastructureView>();
            var foreign=game.Seat(1).View.Feature<InfrastructureView>();var journal=game.Seat(0).View.Feature<ResourceFlowView>();
            var limits=new ReplayLimits(4*1024*1024,3000);var recorder=game.RecordReplay(limits);game.Seat(0).View.TryFindResource("steel",out var steel);
            c.That(proof.ActualBinding==direct&&!game.Modules.Contains(TycoonModule.ModuleId),"network uses authored original binding only");
            c.That(Pool(game).Available&&Pool(game).Value==Fix.Zero,"no pre-Update flow");var twin=factory.Load(game.Save());
            Reach(game,twin,()=>Pool(game).Value==Fix.FromInt(8),"two actual four-unit injections");
            ulong hash=game.Hash();var pools=new List<NetworkPoolView>();var flows=new List<NetworkFlowSnapshot>();
            c.Equal(Code.None,infra.CopyPools("binding.main",pools),"pure pool view");c.Equal(Code.None,infra.CopyFlow("binding.main",flows),"pure earned-flow view");
            c.That(pools.Count==1&&pools[0].Stock.Single(x=>x.Resource==steel).Amount==Fix.FromInt(8),"actual connected stock");
            c.That(flows.Count>0&&flows.All(x=>x.ElapsedTicks>=0&&x.LastTick<=game.Clock.StepIndex&&x.NextAllowance>=Fix.Zero),"earned service is not delivered stock");
            c.Equal(Code.None,infra.ReadConnection(Entity(game,"binding.source"),"binding.main",out var connection),"settled endpoint identity");
            c.That(connection.Endpoint==pools[0].Endpoint,"connection and stock share original pool");
            c.Equal(Code.None,infra.ReadGates(Entity(game,"binding.source"),"binding.main",out var gates),"actual readiness");
            c.Equal(new Code("binding.backpressure"),gates.Blocker,"authored backpressure from real pool");
            c.Equal(Code.None,infra.ReadDemand("binding.main",Entity(game,"binding.source"),steel,false,out var demand),"same native flow admission");
            c.That(demand.Maximum==Fix.Zero&&demand.Blocker==gates.Blocker,"blocked admission promises no throughput");
            var summary=journal.ReadSummary(Company);c.Equal(ResourceFlowReadStatus.Ready,summary.Status,"actual committed flow history");
            c.That(summary.Aggregates.Any(x=>x.Resource==steel&&x.Kind==ResourceFlowKind.StockTransfer&&x.AmountRaw==Fix.FromInt(8).Raw),"journal records eight delivered units, not potential allowance");
            c.Equal(ResourceFlowReadStatus.NotOwned,game.Seat(1).View.Feature<ResourceFlowView>().ReadSummary(Company).Status,"committed history private");
            c.Equal(Code.None,foreign.CopyPools("binding.main",new List<NetworkPoolView>()),"foreign diagnostic safely returns own subset");
            c.That(foreign.ReadConnection(Entity(game,"binding.source"),"binding.main",out _)!=Code.None,"foreign endpoint refused");
            c.Equal(hash,game.Hash(),"all network facts observational");var retained=pools[0];twin=factory.Load(game.Save());
            for(int i=0;i<4;i++)Step(game,twin);c.Equal(Fix.FromInt(8),Pool(game).Value.Value,"backpressure stalls original flow");Pause(c,game,twin);
            c.That(retained.Stock.Single(x=>x.Resource==steel).Amount==Fix.FromInt(8),"retained pool detached");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"flow/backpressure saved");c.Equal(other,isolated.Hash(),"network sessions isolated");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"actual flow and clock replay");
        }
        private static GameFactory ControlFactory(out OperationalViewProofModule proof,bool multiple=false)
        {
            var initialize=new InitializeProbe();proof=new OperationalViewProofModule(initialize);
            return ControlContentContractTests.Factory(initialize,out _,extension:proof,edit:(native,external)=>{
                if(!multiple)return;
                var parts=external.Get("entities")[0].Get("components");var original=parts.Get("automation_probe");var second=original.Clone();
                second.Get("operation").Set("id",JsonValue.Of("probe.second"));var values=JsonValue.NewArray();values.Add(original);values.Add(second);parts.Set("automation_probe",values);
            });
        }
        private static AutomationRuleSnapshot Rule(SimGame game)=>ControlContentContractTests.Rule(game,"sensor.rule");
        private static void Controls(Check c)
        {
            var factory=ControlFactory(out var proof);var game=ControlContentContractTests.New(factory);var isolated=ControlContentContractTests.New(factory);ulong other=isolated.Hash();
            var view=View(game);var id=Entity(game,"contract.sensor");ulong hash=game.Hash();var ids=new List<Code>();
            c.That(view.CopyOperationIds(id,ids)&&ids.SequenceEqual(new[]{Probe}),"authored operation identity");
            c.That(view.CopySignalSourceIds(id,ids)&&ids.SequenceEqual(new[]{Probe}),"signal identity retains original named provider");
            c.That(view.TryGetOperation(id,Probe,out _)&&view.TryGetOperationSchema(id,Probe,out _)&&view.TryGetSignalSchema(id,Probe,out _),"actual provider/schema discovery");
            c.That(view.CopyControlTargetIds(id,"contract.sensor-rule",ids)&&ids.SequenceEqual(new[]{Probe}),"action owns compatible target discovery");
            c.That(view.TryGetAutomationProfile("contract.sensor-rule",out var retained),"complete detached profile");
            c.Throws<NotSupportedException>(()=>((IDictionary<string,bool>)retained.InitialState).Clear(),"profile immutable");
            var profiles=new List<AutomationProfileSnapshot>();view.CopyAutomationProfiles(profiles);c.That(profiles.Any(x=>x.Id==retained.Id),"whole profile list");
            c.That(view.CopyCompanyControlTargetIds(Company,"contract.incident-alert",ids)&&ids.Count==1,"company action uses same policy");
            c.That(!View(game,1).CopyCompanyControlTargetIds(Company,"contract.incident-alert",ids)&&ids.Count==0,"private company target refused");c.Equal(hash,game.Hash(),"control discovery pure");
            var limits=new ReplayLimits(4*1024*1024,3000);var recorder=game.RecordReplay(limits);
            Input(c,game,new ConfigureAutomationCommand{Rule="sensor.rule",Settings=ControlContentContractTests.Settings(game,true)});
            Reach(game,null,()=>Rule(game).HasApplied,"initial deferred branch");
            Input(c,game,new SetAutomationProbeCommand{Entity=id,Level=Fix.FromInt(2),Available=true});
            Reach(game,null,()=>Rule(game).PendingDecision==true,"threshold earns authored dwell");var pending=Rule(game);
            c.That(view.TryGetControl(id,Probe,out var control)&&control.Enabled,"pending decision has not changed operation");
            var twin=factory.Load(game.Save());Pause(c,game,twin);Step(game,twin);
            c.That(Rule(game).Latch&&view.TryGetControl(id,Probe,out control)&&!control.Enabled,"next Update commits ordinary operation disable");
            Input(c,game,new SetAutomationProbeCommand{Entity=id,Level=Fix.FromInt(4),Available=true});twin=factory.Load(game.Save());for(int i=0;i<6;i++)Step(game,twin);
            c.That(Rule(game).Latch&&Rule(game).PendingDecision==null,"hysteresis retains actual disabled state");
            Input(c,game,new SetAutomationProbeCommand{Entity=id,Level=Fix.FromInt(6),Available=true});
            Reach(game,null,()=>Rule(game).PendingDecision==false,"upper threshold earns deferred restart");twin=factory.Load(game.Save());Step(game,twin);
            c.That(!Rule(game).Latch&&view.TryGetControl(id,Probe,out control)&&control.Enabled,"ordinary next-step restart");
            c.That(pending.PendingDecision==true&&retained.Id=="contract.sensor-rule","retained rule/profile detached");
            var rows=new List<AutomationRuleSnapshot>();View(game,1).CopyAutomationRules(rows);c.Equal(0,rows.Count,"runtime rules private");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"settled controls save");c.Equal(other,isolated.Hash(),"control sessions isolated");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"normal control inputs replay");
        }
        private static void Faults(Check c)
        {
            foreach(string point in new[]{"profile","targets","sources"})foreach(string fault in new[]{"throw","pause","throw-pause","neighbor"}) {
                if(point=="profile"&&fault=="neighbor")continue; // No target entity in company metadata projection.
                var factory=ControlFactory(out var proof,true);var game=ControlContentContractTests.New(factory);var view=View(game);var foreign=View(game,1);var id=Entity(game,"contract.sensor");
                var ids=new List<Code>{new Code("stale")};var profiles=new List<AutomationProfileSnapshot>();var missing=new EntityId(int.MaxValue);
                c.That(view.CopySignalSourceIds(id,ids)&&ids.SequenceEqual(new[]{Probe,Second}),"healthy complete two-provider discovery before late fault");
                proof.Arm(point,fault);
                c.Throws<InvalidOperationException>(()=>{if(point=="profile")view.CopyAutomationProfiles(profiles);else if(point=="targets")view.CopyControlTargetIds(id,"contract.sensor-rule",ids);else view.CopySignalSourceIds(id,ids);},"late actual callback refused "+point+"/"+fault);
                c.That(game.Fault!=null&&(point=="profile"?profiles.Count==0:ids.Count==0),"no healthy prefix after first fault");
                if(fault=="throw"||fault=="throw-pause")c.That(game.Fault.Message.Contains("proof.operational.original"),"original throw survives clock mutation");
                c.Throws<InvalidOperationException>(()=>game.Save(),"faulted state cannot save");
                var refused=new Action[]{
                    ()=>view.CopyUtilityConsumerIds(missing,new List<Code>()),()=>view.TryGetUtilityDemand(missing,Code.None,out _),
                    ()=>view.CopyOperationIds(missing,new List<Code>()),()=>view.CopySignalSourceIds(missing,new List<Code>()),
                    ()=>view.TryGetOperation(missing,Code.None,out _),()=>view.CopyControlTargetIds(missing,"",new List<Code>()),
                    ()=>view.CopyCompanyControlTargetIds(new PlayerId(999),"",new List<Code>()),()=>view.TryGetControl(missing,Code.None,out _),
                    ()=>view.TryGetOperationSchema(missing,Code.None,out _),()=>view.TryGetSignalSchema(missing,Code.None,out _),
                    ()=>view.PreviewConfigure(missing,Code.None,false,Fix.Zero),()=>view.ReadSignal(missing,Code.None,"",out _),
                    ()=>view.CopyAutomationRules(new List<AutomationRuleSnapshot>()),()=>view.TryGetAutomationProfile("",out _),
                    ()=>view.CopyAutomationProfiles(new List<AutomationProfileSnapshot>()),()=>view.TryGetAutomationRule(new PlayerId(999),"",out _),
                    ()=>view.PreviewAutomation("",null),()=>view.PreviewRemoveAutomation(""),()=>foreign.TryGetControl(id,Probe,out _)};
                foreach(var read in refused)c.Throws<InvalidOperationException>(read,"cached missing/private/none facts reject after fault");
            }
        }
        private static void NetworkFaults(Check c)
        {
            foreach(string fault in new[]{"live-throw","live-pause","live-touch","live-private"}) {
                var factory=NetworkSignalBindingTests.Create(new InitializeProbe(),out _,edit:doc=>{
                    for(int i=0;i<doc.Get("entities").Count;i++)doc.Get("entities")[i].Get("components").Get("independent_network_signal").Set("metadata_fault",JsonValue.Of(fault));
                });
                var game=NetworkIncidentPropagationTests.New(factory);var infra=game.Seat(0).View.Feature<InfrastructureView>();var journal=game.Seat(0).View.Feature<ResourceFlowView>();
                var view=View(game);var id=Entity(game,"binding.source");c.That(infra!=null&&journal!=null,"actual selected network diagnostics cached before fault");
                game.Update();if(game.Fault==null)c.Throws<InvalidOperationException>(()=>view.ReadSignal(id,new Code("pool"),"binding.stored",out _),"late actual network metadata callback");
                c.That(game.Fault!=null,"network callback first fault latched");var missing=new EntityId(int.MaxValue);
                var refused=new Action[]{()=>infra.CopyPools("",new List<NetworkPoolView>()),()=>infra.CopyFlow("",new List<NetworkFlowSnapshot>()),
                    ()=>infra.ReadConnection(missing,"",out _),()=>infra.ReadDemand("",missing,ResourceId.None,false,out _),()=>infra.ReadGates(missing,"",out _),
                    ()=>journal.Read(new PlayerId(999),0),()=>journal.ReadSummary(new PlayerId(1)),()=>journal.ReadWindow(new PlayerId(999),0,0),()=>game.Save()};
                foreach(var read in refused)c.Throws<InvalidOperationException>(read,"cached missing/private network/history facts reject after fault");
            }
        }
    }
}
