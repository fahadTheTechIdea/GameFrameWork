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
using static Beep.SimEngine.Tests.ControlContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Uses the one shared thin player workflow, never raw World/provider/stock/progress writes.
    internal static class SignalContentContractTests
    {
        internal const string Case="Signal content contracts: independent descriptors drive shared automation, physical work and guarded saved facts";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static readonly PlayerId Company=new PlayerId(0);
        private static GameFactory Build(InitializeProbe init,out SignalContractModule module,Action<JsonValue> edit=null)
        {
            var json=Json.Parse(FrameworkProofContent.Read("signal-contracts.json"));edit?.Invoke(json);
            module=new SignalContractModule(init);
            return Factory(init,out _,(native,external)=>{
                external.Remove("signals");
                native.Get("control_rules")[0].Get("inputs")[0].Set("signal",JsonValue.Of("contract.incident-active"));
            },extension:module,contentDocuments:new Dictionary<string,string>{{"signal-contracts.json",json.ToString()}},
                responseEdit:response=>response.Get("activities")[0].Get("nodes")[1].Set("requirement",JsonValue.Of("contract.signal-repair")));
        }
        private static AutomationSignalSnapshot Read(SimGame game,string signal="contract.plant-stock",int company=0,Code source=default)
        {
            string def=signal=="contract.sensor-level"?"contract.sensor":company==0?"field.plant":"field.foreign-plant";
            if(source.IsNone)source=new Code(signal=="contract.sensor-level"?"probe":signal=="contract.incident-active"?"incident-state":"feed");
            Code refusal=View(game,company).ReadSignal(Entity(game,def,company).Id,source,signal,out var facts);
            if(!refusal.IsNone)throw new InvalidOperationException("signal refused "+refusal);return facts;
        }
        private static void Run(Check c)
        {
            var init=new InitializeProbe();var factory=Build(init,out var proof);var game=New(factory);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"open descriptor protocol");c.Equal(154,EngineInfo.SaveFormat,"same existing codecs");
            c.That(proof.IndependentDescriptors&&proof.IndependentAliases,"direct owned descriptor/rule/action/recovery contracts without required native bases");c.Equal(1,init.Initializations,"initialization once");
            ulong hash=game.Hash();var frozen=Read(game);c.That(frozen.Signal=="contract.plant-stock"&&frozen.Kind==AutomationSignalKind.Number&&frozen.Unit==new Code("unit.supply")&&frozen.Value==Fix.FromInt(20),"original inventory provider reads independent exact resource descriptor");
            c.Equal(Fix.Zero,Read(game,"contract.incident-active").Value.Value,"native incident provider reads independent Boolean descriptor");c.Equal(Fix.FromInt(6),Read(game,"contract.sensor-level").Value.Value,"external provider reads independent numeric descriptor");
            c.That(!Read(game,source:new Code("missing")).Available&&Read(game,source:new Code("missing")).Blocker==OperationCodes.SignalUnavailable,"missing source stays typed unavailable, no substitution");
            c.That(!Read(game,"contract.sensor-level",source:new Code("incident-state")).Available,"incompatible source stays typed unavailable");
            c.That(View(game).TryGetAutomationProfile("contract.incident-alert",out var profile)&&profile.Inputs.Single().Signal=="contract.incident-active","native rule aliases direct descriptor");
            c.That(View(game).TryGetAutomationProfile("contract.sensor-rule",out var sensor)&&sensor.Inputs.Single().Signal=="contract.sensor-level","independent wrapper and native rule share direct descriptor");
            c.Throws<NotSupportedException>(()=>((IList<AutomationProviderSnapshot>)sensor.Inputs[0].Providers).Clear(),"provider metadata snapshot immutable");c.Equal(hash,game.Hash(),"typed reads/profile observations pure");
            c.Equal(Code.None,View(game).ReadSignal(Entity(game,"field.plant").Id,new Code("incident-state"),"field.incident-active",out var native),"native metadata shares same reader");c.Equal(Fix.Zero,native.Value.Value,"native actual incident state agrees");
            var requirements=new List<WorkRequirementView>();
            c.Equal(StandardCodes.NotOrderable,game.Seat(0).View.Feature<WorkView>().PreviewOrderRequirements("pressure.response",Entity(game,"field.plant").Id,requirements),"incident response is not a separate player-order shortcut");c.That(requirements.Count==0,"refused preview discloses no fabricated requirements");
            var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,3000));
            Order(c,game,new ConfigureAutomationCommand{Rule="plant.alert",Settings=Settings(game)});Until(game,null,()=>Rule(game).HasApplied,"initial ordinary normal decision");
            Order(c,game,new RaiseIncidentCommand{Target=Entity(game,"field.plant").Id,Source=new Code("pressure.leak"),Kind=new Code("pressure.leak")});
            Until(game,null,()=>Rule(game).PendingDecision==true,"descriptor-driven alert dwell");c.That(Read(game,"contract.incident-active").Value==Fix.One&&Stock(game)==Fix.FromInt(20)&&Alert(game).Settings.Level==CompanyAlertLevel.Normal,"pending signal intent cannot repair or consume");
            var twin=factory.Load(game.Save());Step(game,twin);c.Equal(CompanyAlertLevel.Emergency,Alert(game).Settings.Level,"next Update applies same normal alert command");
            Order(c,game,new ConfigureWorkPolicyCommand{Worker=Entity(game,"machine").Id,Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("haul"),new WorkerRoleId("inspect")},null,true,true,WorkAssignmentMode.All)});
            twin=factory.Load(game.Save());Until(game,twin,()=>Stock(game)==Fix.FromInt(12)&&Active(game),"actual resource/signal guarded response starts");
            var orders=new List<OrderView>();var work=game.Seat(0).View.Feature<WorkView>();work.CopyOrders(orders);
            c.That(work.TryCopyOrderRequirements(orders.Single(order=>order.Plan=="activity:pressure.response").Id,requirements)&&requirements.Count>0,"actual incident order projects shared independent signal requirements");
            c.Equal(Fix.FromInt(20),((PointSnapshot)Entity(game,"machine").Spatial).Position,"real machine reaches the incident");c.Equal(Fix.FromInt(12),Read(game).Value.Value,"same descriptor projects exact consumed stock, no cached readiness");c.Equal(Fix.FromInt(20),frozen.Value.Value,"retained quantity facts detached");
            game.Clock.Pause();twin.Clock.Pause();hash=game.Hash();long time=game.Clock.StepIndex;Step(game,twin);c.That(game.Clock.StepIndex==time&&game.Hash()==hash,"paused sole clock earns no repair time");game.Clock.Resume();twin.Clock.Resume();
            twin=factory.Load(game.Save());Until(game,twin,()=>!Active(game),"signal-gated physical response completes");Until(game,twin,()=>Rule(game).PendingDecision==false,"independent healthy signal earns restoration dwell");Step(game,twin);
            c.That(Alert(game).Version==2&&Alert(game).Settings.Level==CompanyAlertLevel.Normal&&Read(game,"contract.incident-active").Value==Fix.Zero,"same Boolean descriptor restores actual policy after work");
            Order(c,game,new ConfigureAutomationCommand{Rule="sensor.rule",Settings=Settings(game,true)});Until(game,null,()=>Rule(game,"sensor.rule").HasApplied,"native numeric initial state");Sensor(c,game,2,true);Until(game,null,()=>Rule(game,"sensor.rule").PendingDecision==true,"independent numeric threshold");
            Sensor(c,game,2,false);c.That(Read(game,"contract.sensor-level").Blocker==new Code("probe.sensor-offline")&&Rule(game,"sensor.rule").WaitingForInputs&&Rule(game,"sensor.rule").PendingDecision==null,"shared descriptor preserves unavailable reason and cancels stale intent");
            twin=factory.Load(game.Save());for(int i=0;i<4;i++)Step(game,twin);c.That(Rule(game,"sensor.rule").WaitingForInputs,"saved wait unchanged");Sensor(c,game,2,true);twin=factory.Load(game.Save());Until(game,twin,()=>Rule(game,"sensor.rule").Latch,"restored current reading earns native dwell and deferred control");
            Sensor(c,game,4,true);twin=factory.Load(game.Save());for(int i=0;i<6;i++)Step(game,twin);c.That(Rule(game,"sensor.rule").Latch&&Rule(game,"sensor.rule").PendingDecision==null,"native hysteresis over independent descriptor");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"settled provider/work/control codecs");c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,3000)).Success,"normal physical/automation command history replays");
            var fresh=New(factory);c.That(Read(fresh).Value==Fix.FromInt(20)&&Read(fresh,"contract.incident-active").Value==Fix.Zero&&!Active(fresh),"fresh session keeps original state independent");
            Faults(c,factory);Invalid(c);
        }
        private static void Faults(Check c,GameFactory factory)
        {
            foreach(int mode in new[]{1,5,6,7})
            {
                var game=New(factory);Order(c,game,new InjectSignalContractFaultCommand{Entity=Entity(game,"contract.sensor").Id,Mode=mode});
                Code foreign=View(game,1).ReadSignal(Entity(game,"contract.sensor").Id,new Code("probe"),"contract.sensor-level",out _);
                c.That((foreign==Codes.NotOwned||foreign==Codes.NotFound)&&game.Fault==null,"foreign request refuses before fault callback "+mode);
                c.Throws<InvalidOperationException>(()=>Read(game,"contract.sensor-level"),"same guarded reader catches unit/hidden-state/callback/identity fault "+mode);
                var fault=game.Fault;long time=game.Clock.StepIndex;c.That(fault!=null,"existing fault owner latched "+mode);game.Update();c.That(game.Clock.StepIndex==time&&ReferenceEquals(fault,game.Fault),"faulted update freezes sole clock "+mode);
                c.Throws<InvalidOperationException>(()=>game.Save(),"fault cannot save "+mode);c.Throws<InvalidOperationException>(()=>game.Hash(),"fault cannot hash "+mode);c.Throws<InvalidOperationException>(()=>Read(game,"contract.sensor-level"),"fault cannot appear healthy "+mode);
            }
            c.That(New(factory).Fault==null,"other sessions remain healthy");
        }
        private static void Invalid(Check c)
        {
            foreach(var fault in new[]{"kind","metric","unit","provider","version","empty","missing","duplicate","resource","quantity-unit","resource-kind","wrong-kind"})
            {
                var init=new InitializeProbe();c.Throws<ContentException>(()=>New(Build(init,out _,json=>{
                    var scalar=json.Get("independent_signals")[1];var quantity=json.Get("independent_resource_signals")[0];
                    if(fault=="kind")scalar.Set("kind",JsonValue.Of("opaque"));if(fault=="metric")scalar.Set("metric",JsonValue.Of("probe.unknown"));if(fault=="unit")scalar.Set("unit",JsonValue.Of("unit.wrong"));
                    if(fault=="provider")scalar.Get("providers")[0].Set("component",JsonValue.Of("missing"));if(fault=="version")scalar.Get("providers")[0].Set("schema",JsonValue.Number("2"));
                    if(fault=="empty")scalar.Set("providers",Json.Parse("[]"));if(fault=="missing")scalar.Remove("providers");if(fault=="duplicate")scalar.Set("providers",Json.Parse("[{\"component\":\"automation_probe\",\"schema\":1},{\"component\":\"automation_probe\",\"schema\":1}]"));
                    if(fault=="resource")quantity.Set("resource",JsonValue.Of("missing"));if(fault=="quantity-unit")quantity.Set("unit",JsonValue.Of("unit.level"));if(fault=="resource-kind")quantity.Set("kind",JsonValue.Of("boolean"));if(fault=="wrong-kind")scalar.Set("kind",JsonValue.Of("boolean"));
                })),"early independent metadata refusal "+fault);c.Equal(0,init.Initializations,"rejected before Initialize "+fault);
            }
        }
    }
}
