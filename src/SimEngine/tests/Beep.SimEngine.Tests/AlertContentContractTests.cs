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
    // Explicit authoring, player commands and immutable views only; all alert/automation/work rules are in SimEngine.
    internal static class AlertContentContractTests
    {
        internal const string Case = "Alert content contracts: independent JSON priorities drive deferred emergency and physical response";
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly Code Leak = new Code("pressure.leak");
        private static readonly ReplayLimits Limits = new ReplayLimits(4*1024*1024,3000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Build(InitializeProbe initialize, out AlertContractModule proof, Action<JsonValue> edit=null,
            Action<JsonValue> scenarioEdit=null, Action<JsonValue> actionEdit=null)
        {
            proof=new AlertContractModule(initialize);
            var content=Json.Parse(FrameworkProofContent.Read("alert-contracts.json"));edit?.Invoke(content);
            return Factory(initialize,out _,extension:proof,contentDocuments:new Dictionary<string,string>{{"alert-contracts.json",content.ToString()}},
                edit:(native,external)=>{
                    var action=native.Get("company_alert_control_actions")[0];
                    action.Get("enabled").Set("profile",JsonValue.Of("contract.alert-response"));
                    action.Get("disabled").Set("profile",JsonValue.Of("contract.alert-response"));actionEdit?.Invoke(action);
                },scenarioEdit:scenario=>{
                    var companies=scenario.Get("settings").Get(CompanyAlertModule.ModuleId).Get("companies");
                    companies[0].Set("profile",JsonValue.Of("contract.alert-response"));companies[1].Set("profile",JsonValue.Of("contract.alert-other"));
                    scenarioEdit?.Invoke(scenario);
                });
        }
        private static CompanyAlertView Alerts(SimGame game,int seat=0)=>game.Seat(seat).View.Feature<CompanyAlertView>();
        private static CompanyAlertSettings At(CompanyAlertLevel level)=>new CompanyAlertSettings("contract.alert-response",level,Array.Empty<WorkerReserveSetting>());
        private static List<WorkerDispatchOffer> Offers(SimGame game)
        {var values=new List<WorkerDispatchOffer>();game.Seat(0).View.Feature<WorkView>().CopyAssignments(values);return values;}
        private static void Run(Check c)
        {
            var initialize=new InitializeProbe();var factory=Build(initialize,out var proof);var game=New(factory);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current alert content protocol");c.Equal(154,EngineInfo.SaveFormat,"unchanged saved owners");
            c.That(proof.IndependentAlert&&proof.IndependentAliases,"actual owned independent profile and automation contracts");c.Equal(1,initialize.Initializations,"valid composition initializes once");
            ulong hash=game.Hash();var initial=Alert(game);
            c.That(initial.Settings.Profile=="contract.alert-response"&&initial.Version==0,"actual file profile admitted by sole settings owner");
            c.Equal(Codes.NotOwned,Alerts(game,1).TryReadAlert(Company,out _),"foreign company policy stays private");
            c.Equal(CompanyAlertCodes.InvalidSettings,Alerts(game).PreviewAlert(Company,new CompanyAlertSettings("contract.alert",CompanyAlertLevel.Normal,Array.Empty<WorkerReserveSetting>())),"wrong content kind explicitly refuses");
            c.Throws<NotSupportedException>(()=>((IList<WorkerReserveSetting>)initial.Settings.WorkerReserves).Add(new WorkerReserveSetting(new WorkerRoleId("inspect"),1)),"settings facts immutable");
            c.Equal(hash,game.Hash(),"policy previews and reads pure");
            var recorder=game.RecordReplay(Limits);
            Order(c,game,new RaiseIncidentCommand{Target=Entity(game,"field.plant").Id,Source=Leak,Kind=Leak});
            Until(game,null,()=>Offers(game).Any(value=>value.Emergency),"actual emergency offers after detection");
            c.That(Offers(game).Where(value=>value.Emergency).All(value=>value.EffectivePriority==value.Priority),"normal nonemergency rule never boosts emergency work");
            Order(c,game,new ConfigureCompanyAlertCommand{Company=Company,Settings=At(CompanyAlertLevel.Urgent)});
            c.That(Alert(game).Settings.Level==CompanyAlertLevel.Urgent&&Alert(game).Version==1,"normal command changes original policy only");
            hash=game.Hash();var urgent=Offers(game);c.That(urgent.Any(value=>value.Emergency&&value.EffectivePriority==value.Priority+17L),"actual urgent work uses authored matching scope and emergency rule");c.Equal(hash,game.Hash(),"derived queue does not commit policy/work state");
            Order(c,game,new ConfigureCompanyAlertCommand{Company=Company,Settings=At(CompanyAlertLevel.Normal)});
            Order(c,game,new ConfigureAutomationCommand{Rule="plant.alert",Settings=Settings(game)});
            c.Equal(2,Alert(game).Version,"configuring rule does not commit its pending policy action");
            Until(game,null,()=>Rule(game).PendingDecision==true,"incident earns authored dwell");var pending=Rule(game);
            c.That(Alert(game).Settings.Level==CompanyAlertLevel.Normal&&Active(game)&&Stock(game)==Fix.FromInt(20),"deferred priority cannot teleport repair or consume inputs");
            var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"pending contract alert saves exactly");
            game.Clock.Pause();twin.Clock.Pause();hash=game.Hash();long at=game.Clock.StepIndex;Step(game,twin);
            c.That(game.Hash()==hash&&game.Clock.StepIndex==at,"sole clock pause retains pending intent");game.Clock.Resume();twin.Clock.Resume();Step(game,twin);
            c.That(Alert(game).Settings.Level==CompanyAlertLevel.Emergency&&Alert(game).Version==3&&Rule(game).Latch,"next Update applies normal command against actual profile contract");
            c.That(pending.PendingDecision==true&&initial.Version==0,"retained facts detached");c.Equal(0,Alert(game,1).Version,"other company unaffected");
            var offers=Offers(game);c.That(offers.Any(value=>value.Emergency&&value.EffectivePriority==value.Priority+500L),"same scheduler combines wildcard and scope JSON emergency rules");
            Order(c,game,new ConfigureWorkPolicyCommand{Worker=Entity(game,"machine").Id,Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("haul"),new WorkerRoleId("inspect")},null,true,true,WorkAssignmentMode.All)});
            twin=factory.Load(game.Save());Until(game,twin,()=>Stock(game)==Fix.FromInt(12)&&Active(game),"physical response reaches productive repair");
            c.Equal(Fix.FromInt(20),((PointSnapshot)Entity(game,"machine").Spatial).Position,"native worker physically travels to response");
            twin=factory.Load(game.Save());Until(game,twin,()=>!Active(game),"real timed response completes");
            c.Equal(CompanyAlertLevel.Emergency,Alert(game).Settings.Level,"completion pass cannot apply same-tick feedback");
            Until(game,twin,()=>Rule(game).PendingDecision==false,"healthy signal earns restore dwell");twin=factory.Load(game.Save());Step(game,twin);
            c.That(Alert(game).Settings.Level==CompanyAlertLevel.Normal&&Alert(game).Version==4&&Stock(game)==Fix.FromInt(12),"false branch restores actual contract profile with no repeated consumption");
            for(int i=0;i<4;i++)Step(game,twin);
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"settled profile chronology and original state codecs");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),Limits).Success,"normal alert/automation/work command stream replays");
            var fresh=New(factory);c.That(Alert(fresh).Version==0&&Stock(fresh)==Fix.FromInt(20)&&!Active(fresh),"new interleaved session isolated");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach(string fault in new[]{"normal","urgent","emergency","duplicate","scope","scope-type","emergency-type","adjustment","unknown-scope","unknown-unused-scope","initial-kind","action-kind","initial-missing","action-missing"})
            {
                var initialize=new InitializeProbe();
                c.Throws<ContentException>(()=>New(Build(initialize,out _,edit:document=>{
                    var profile=document.Get("independent_alert_profiles")[0];var rule=profile.Get("emergency")[1];
                    if(fault=="normal"||fault=="urgent"||fault=="emergency")profile.Remove(fault);
                    if(fault=="duplicate")profile.Get("emergency").Add(rule.Clone());
                    if(fault=="scope")rule.Set("scope",JsonValue.Of("Bad Scope"));if(fault=="scope-type")rule.Set("scope",JsonValue.Number("2"));
                    if(fault=="emergency-type")rule.Set("emergency",JsonValue.Of("yes"));if(fault=="adjustment")rule.Remove("priority_adjustment");
                    if(fault=="unknown-scope")rule.Set("scope",JsonValue.Of("missing.scope"));
                    if(fault=="unknown-unused-scope")document.Get("independent_alert_profiles")[2].Get("urgent").Add(Json.Parse("{\"scope\":\"missing.scope\",\"emergency\":false,\"priority_adjustment\":1}"));
                },scenarioEdit:scenario=>{
                    if(fault=="initial-kind"||fault=="initial-missing")scenario.Get("settings").Get(CompanyAlertModule.ModuleId).Get("companies")[0].Set("profile",JsonValue.Of(fault=="initial-kind"?"contract.alert":"missing"));
                },actionEdit:action=>{
                    if(fault=="action-kind"||fault=="action-missing")action.Get("enabled").Set("profile",JsonValue.Of(fault=="action-kind"?"contract.alert":"missing"));
                })),"early alert refusal "+fault);
                c.Equal(0,initialize.Initializations,"before Initialize "+fault);
            }
        }
    }
}
