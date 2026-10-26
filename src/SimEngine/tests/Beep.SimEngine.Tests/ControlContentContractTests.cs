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

namespace Beep.SimEngine.Tests
{
    // Thin player/environment input and public facts; every decision/work/stock transition is engine-owned.
    internal static class ControlContentContractTests
    {
        internal const string Case = "Control content contracts: independent JSON policies drive deferred alerts, physical response and saved sensor recovery";
        internal const string DeclarationCase = "Control rule declarations: direct JSON condition DAG drives native deferred response and saved hysteresis";
        internal const string PolicyDeclarationCase = "Control policy declarations: direct JSON actions and recovery retain native physical response and guarded commands";
        internal static void Register()
        {
            TestRunner.Add(Case, c => Run(c, false)); TestRunner.Add(DeclarationCase, c => Run(c, true));
            TestRunner.Add(PolicyDeclarationCase, c => Run(c, true, true));
        }
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly Code Leak = new Code("pressure.leak");
        internal static GameFactory Factory(InitializeProbe init, out ControlContractModule proof, Action<JsonValue,JsonValue> edit = null,
            ControlContractModule extension = null, IReadOnlyDictionary<string,string> contentDocuments = null,
            Action<JsonValue> responseEdit = null, Action<JsonValue> constructionEdit = null, Action<JsonValue> worldEdit = null,
            IReadOnlyList<GameModule> additionalModules = null, JsonValue additionalDeclarations = null, Action<JsonValue> scenarioEdit = null,
            Action<JsonValue> conditionsEdit = null, bool declaredRules = false, bool sharedLeafDag = false, bool declaredPolicies = false)
        {
            var native = Json.Parse(FrameworkProofContent.Read("company-alert-automation.json"));
            var external = Json.Parse(FrameworkProofContent.Read("control-contracts.json"));
            native.Get("control_rules")[0].Set("action_policy",JsonValue.Of("contract.alert"));
            native.Get("control_rules")[0].Set("input_recovery",JsonValue.Of("contract.inputs"));
            native.Get("control_input_recovery_policies")[0].Set("retry_blockers",Json.Parse("[\"probe.sensor-offline\"]"));
            edit?.Invoke(native,external); proof = extension ?? new ControlContractModule(init);
            if (declaredPolicies)
            {
                // Select independently registered content kinds, never a native policy alias/fallback.
                native.Set("declared_alert_control_actions", native.Get("company_alert_control_actions"));
                native.Remove("company_alert_control_actions");
                native.Set("declared_input_recovery", native.Get("control_input_recovery_policies"));
                native.Remove("control_input_recovery_policies");
                external.Set("declared_operation_control_actions", external.Get("operation_control_actions"));
                external.Remove("operation_control_actions");
            }
            if (declaredRules)
            {
                // Content-kind selection only. All rule fields come from the existing master-listed files.
                foreach (JsonValue document in new[] { native, external })
                {
                    document.Set("declared_control_rules", document.Get("control_rules"));
                    document.Remove("control_rules");
                }
                if (sharedLeafDag)
                {
                    // Positive fixture only; never overwrite an intentionally malformed authored root.
                    JsonValue rule = external.Get("declared_control_rules")[0];
                    rule.Get("conditions").Add(Json.Parse("{\"id\":\"either.low\",\"kind\":\"any\",\"conditions\":[\"low\"],\"initial\":false}"));
                    rule.Get("conditions").Add(Json.Parse("{\"id\":\"both.low\",\"kind\":\"all\",\"conditions\":[\"low\",\"either.low\"],\"initial\":false}"));
                    rule.Set("root", JsonValue.Of("both.low"));
                }
            }
            var response = Json.Parse(FrameworkProofContent.Read("objective-response.json")); responseEdit?.Invoke(response);
            var declarations = Json.Parse("[{\"id\":\"sim.conditions\",\"version\":\"1.0.0\",\"documents\":[\"objective-conditions.json\"]},{\"id\":\"sim.inventory-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"sim.automation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[\"company-alert-automation.json\"]},{\"id\":\"sim.company-alerts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"company-alert-automation-profiles.json\"]},{\"id\":\"proof.automation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.automation\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"proof.control-contracts\",\"version\":\"1.0.0\",\"documents\":[\"control-contracts.json\"]}]");
            var requires = JsonValue.NewArray();
            foreach (var required in proof.Requires)
            { var declared = JsonValue.NewObject(); declared.Set("id",JsonValue.Of(required.Id)); declared.Set("version",JsonValue.Of(required.Version.ToString())); requires.Add(declared); }
            declarations[declarations.Count-1].Set("requires",requires);
            var conditions = Json.Parse(FrameworkProofContent.Read("objective-conditions.json")); conditionsEdit?.Invoke(conditions);
            var documents = new Dictionary<string,string> { ["objective-conditions.json"]=conditions.ToString(),["company-alert-automation.json"]=native.ToString(),["company-alert-automation-profiles.json"]=FrameworkProofContent.Read("company-alert-automation-profiles.json"),["control-contracts.json"]=external.ToString() };
            if (contentDocuments != null)
            {
                foreach (var document in contentDocuments.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    declarations[declarations.Count-1].Get("documents").Add(JsonValue.Of(document.Key));
                    documents.Add(document.Key,document.Value);
                }
            }
            var modules = new List<GameModule> { new GameConditionProofModule(),new InventorySignalsModule(),new AutomationModule(),new CompanyAlertModule(),new AutomationProofModule(),proof };
            if(additionalModules!=null)modules.AddRange(additionalModules);
            if(additionalDeclarations!=null)for(int i=0;i<additionalDeclarations.Count;i++)declarations.Add(additionalDeclarations[i]);
            return ConstructionPlanTests.Points(edit:constructionEdit,extraModules:modules,
                extraDeclarations:declarations.ToString(),
                extraDocuments:documents,
                additionalDocuments:new Dictionary<string,string> { ["objective-response.json"]=response.ToString() },
                scenarioEdit:s => {
                    foreach(var id in new[] { GameConditionsModule.ModuleId,InventorySignalsModule.ModuleId,AutomationModule.ModuleId,IncidentResponseModule.ModuleId,CompanyAlertModule.ModuleId,"proof.automation","proof.control-contracts" }) s.Get("modules").Add(JsonValue.Of(id));
                    s.Get("settings").Set(CompanyAlertModule.ModuleId,Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"alerts.response\",\"level\":\"normal\",\"worker_reserves\":[]},{\"company\":1,\"profile\":\"alerts.other\",\"level\":\"normal\",\"worker_reserves\":[]}]}"));
                    scenarioEdit?.Invoke(s);
                },startupProfiles:new[] { "objective-conditions.start","contract.control-start" },worldEdit:w=>{StartupContentAuthoring.Position(w,"machine",JsonValue.Number("4"));worldEdit?.Invoke(w);});
        }
        internal static SimGame New(GameFactory factory)=>ConstructionPlanTests.NewPoints(factory);
        internal static OperationsView View(SimGame game,int company=0)=>game.Seat(company).View.Feature<OperationsView>();
        internal static EntityView Entity(SimGame game,string def,int company=0)=>game.Seat(company).Snapshot.Entities.Single(e=>e.Def==def&&e.Owner==new PlayerId(company));
        internal static CompanyAlertSnapshot Alert(SimGame game,int company=0)
        { var code=game.Seat(company).View.Feature<CompanyAlertView>().TryReadAlert(new PlayerId(company),out var value); if(!code.IsNone)throw new InvalidOperationException("alert "+code);return value; }
        internal static bool Active(SimGame game)
        { game.Seat(0).View.Feature<IncidentsView>().TryGetIncident(Entity(game,"field.plant").Id,Leak,out var value);return value.Active; }
        internal static Fix Stock(SimGame game)
        { var facets=new List<Facet>();game.Seat(0).View.Inspect(Entity(game,"field.plant").Id,facets);return facets.Where(f=>f.Key==Storage.FacetStock&&f.Subject=="supply").Aggregate(Fix.Zero,(n,f)=>n+f.Value); }
        internal static AutomationRuleSnapshot Rule(SimGame game,string id="plant.alert")
        { if(!View(game).TryGetAutomationRule(Company,id,out var value))throw new InvalidOperationException("missing rule "+id);return value; }
        internal static void Step(SimGame game,SimGame twin=null)
        { game.Update();twin?.Update();if(game.Fault!=null||twin!=null&&(twin.Fault!=null||game.Hash()!=twin.Hash()))throw new InvalidOperationException("control step="+game.Clock.StepIndex+" fault="+game.Fault+"/"+twin?.Fault); }
        internal static void Until(SimGame game,SimGame twin,Func<bool> done,string stage)
        { int steps=0;while(!done()&&steps++<700)Step(game,twin);if(!done()){var jobs=new List<WorkJobView>();game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);throw new InvalidOperationException(stage+" step="+game.Clock.StepIndex+" stock="+Stock(game)+" machine="+((PointSnapshot)Entity(game,"machine").Spatial).Position+" jobs="+string.Join(";",jobs.Select(j=>j.Id+"/"+j.State+"/"+j.Blocker)));} }
        internal static void Order(Check c,SimGame game,Command command)
        { ulong hash=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(command),"normal public preview");c.Equal(hash,game.Hash(),"preview pure");c.That(TestKit.Order(game,0,command).Accepted,"normal command accepted"); }
        internal static AutomationSettings Settings(SimGame game,bool sensor=false,EntityId foreign=default)
        {
            string profile=sensor?"contract.sensor-rule":"contract.incident-alert";
            if(!View(game).TryGetAutomationProfile(profile,out var facts))throw new InvalidOperationException("missing profile");
            var thresholds=new Dictionary<string,ControlThresholdBand>();foreach(var row in facts.Thresholds)thresholds.Add(row.Key,row.Value.Initial);
            var id=sensor?Entity(game,"contract.sensor").Id:foreign.IsValid?foreign:Entity(game,"field.plant").Id;
            return new AutomationSettings(profile,true,sensor?new ControlBinding(id,new Code("probe")):ControlBinding.ForCompany(CompanyAlertControlActionDeclaration.Capability),
                new Dictionary<string,ControlBinding>{{sensor?"level":"incident",new ControlBinding(id,new Code(sensor?"probe":"incident-state"))}},thresholds,facts.WhenTrue,facts.WhenFalse);
        }
        internal static void Sensor(Check c,SimGame game,int level,bool available)
            =>Order(c,game,new SetAutomationProbeCommand { Entity=Entity(game,"contract.sensor").Id,Level=Fix.FromInt(level),Available=available });
        private static void Run(Check c, bool declaredRules, bool declaredPolicies = false)
        {
            var init=new InitializeProbe();var factory=Factory(init,out var proof, declaredRules: declaredRules, sharedLeafDag: declaredRules, declaredPolicies: declaredPolicies);var game=New(factory);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current contract protocol");c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"canonical current save protocol");
            if (declaredRules)
            {
                c.Equal(2, proof.DeclaredRuleCount, "both file-authored rules use the public shared declaration");
                c.Equal(0, proof.NativeRuleCount, "no hidden native rule definition backs the direct JSON rules");
                c.That(View(game).TryGetAutomationProfile("contract.native-sensor", out var direct)
                    && direct.InitialState.Count == 3 && direct.Thresholds.Count == 1,
                    "one shared threshold leaf contributes through both logical DAG branches");
            }
            if (declaredPolicies)
            {
                c.Equal(2, proof.DeclaredActionCount, "operation and company actions are direct independent definitions");
                c.Equal(0, proof.NativeOperationAndAlertCount, "no hidden native operation/company policy definitions");
                c.Equal(1, proof.DeclaredRecoveryCount, "input recovery uses the engine's shared declaration through a direct contract");
                c.Equal(0, proof.NativeRecoveryCount, "no native recovery definition substitutes for independent JSON");
            }
            c.That(proof.IndependentAliases,"independent actual rule/action/recovery aliases, no native bases");c.Equal(1,init.Initializations,"valid initialization once");
            ulong hash=game.Hash();c.That(View(game).TryGetAutomationProfile("contract.incident-alert",out var retained)&&retained.ActionPolicy=="contract.alert"&&retained.InputRecovery=="contract.inputs","detached independent profile");
            c.Throws<NotSupportedException>(()=>((IDictionary<string,bool>)retained.InitialState).Clear(),"named state immutable");c.Equal(hash,game.Hash(),"contract discovery pure");
            var ids=new List<Code>();c.That(View(game).CopyCompanyControlTargetIds(Company,retained.Id,ids)&&ids.SequenceEqual(new[]{CompanyAlertControlActionDeclaration.Capability}),"contract company target discovery");
            c.That(!View(game,1).CopyCompanyControlTargetIds(Company,retained.Id,ids)&&ids.Count==0,"foreign company discovery denied");
            c.Equal(Codes.NotOwned,View(game).PreviewAutomation("foreign",Settings(game,foreign:Entity(game,"field.foreign-plant",1).Id)),"foreign input cannot drive own policy");
            var limits=new ReplayLimits(4*1024*1024,3000); ulong initialHash=game.Hash();
            using var recorder=game.RecordReplay(limits);
            Order(c,game,new ConfigureAutomationCommand { Rule="plant.alert",Settings=Settings(game) });
            Until(game,null,()=>Rule(game).HasApplied,"initial normal deferred action");c.Equal(0,Alert(game).Version,"matching normal action grants no revision");
            Order(c,game,new RaiseIncidentCommand { Target=Entity(game,"field.plant").Id,Source=Leak,Kind=Leak });
            Until(game,null,()=>Rule(game).PendingDecision==true,"incident earns dwell");var pending=Rule(game);
            c.That(Alert(game).Settings.Level==CompanyAlertLevel.Normal&&Active(game)&&Stock(game)==Fix.FromInt(20),"pending alert grants no repair/material change");
            var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"pending contract action saves");
            game.Clock.Pause();twin.Clock.Pause();hash=game.Hash();long at=game.Clock.StepIndex;Step(game,twin);
            c.That(game.Clock.StepIndex==at&&game.Hash()==hash,"paused sole clock cannot commit pending intent");game.Clock.Resume();twin.Clock.Resume();Step(game,twin);
            c.That(Alert(game).Settings.Level==CompanyAlertLevel.Emergency&&Alert(game).Version==1&&Rule(game).Latch,"next Update uses ordinary alert owner");
            c.That(pending.PendingDecision==true&&retained.ActionPolicy=="contract.alert","retained snapshots detached");c.Equal(0,Alert(game,1).Version,"other company untouched");
            Order(c,game,new ConfigureWorkPolicyCommand { Worker=Entity(game,"machine").Id,Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("haul"),new WorkerRoleId("inspect")},null,true,true,WorkAssignmentMode.All) });
            twin=factory.Load(game.Save());Until(game,twin,()=>Stock(game)==Fix.FromInt(12)&&Active(game),"physical response starts and consumes eight");
            c.Equal(Fix.FromInt(20),((PointSnapshot)Entity(game,"machine").Spatial).Position,"real response worker reaches plant");
            twin=factory.Load(game.Save());Until(game,twin,()=>!Active(game),"native physical repair completes");
            c.Equal(CompanyAlertLevel.Emergency,Alert(game).Settings.Level,"repair pass does not apply new decision");
            Until(game,twin,()=>Rule(game).PendingDecision==false,"restoration earns dwell");twin=factory.Load(game.Save());Step(game,twin);
            c.That(Alert(game).Version==2&&Alert(game).Settings.Level==CompanyAlertLevel.Normal&&Stock(game)==Fix.FromInt(12),"original false branch restores policy without duplicate inputs");
            Order(c,game,new ConfigureAutomationCommand { Rule="sensor.rule",Settings=Settings(game,true) });
            Until(game,null,()=>Rule(game,"sensor.rule").HasApplied,"native initial numeric branch");Sensor(c,game,2,true);
            Until(game,null,()=>Rule(game,"sensor.rule").PendingDecision==true,"native threshold dwell");
            Sensor(c,game,2,false);c.That(Rule(game,"sensor.rule").WaitingForInputs&&Rule(game,"sensor.rule").PendingDecision==null,"direct recovery cancels stale pending command and waits");
            c.That(View(game).TryGetControl(Entity(game,"contract.sensor").Id,new Code("probe"),out var control)&&control.Enabled,"offline sensor cannot apply stale disable");
            twin=factory.Load(game.Save());for(int i=0;i<5;i++)Step(game,twin);c.That(Rule(game,"sensor.rule").WaitingForInputs,"saved direct recovery remains waiting");
            Sensor(c,game,2,true);twin=factory.Load(game.Save());Until(game,twin,()=>Rule(game,"sensor.rule").Latch&&Rule(game,"sensor.rule").HasApplied,"fresh restored reading earns dwell and next-step disable");
            Sensor(c,game,4,true);twin=factory.Load(game.Save());for(int i=0;i<6;i++)Step(game,twin);c.That(Rule(game,"sensor.rule").Latch&&Rule(game,"sensor.rule").PendingDecision==null,"native hysteresis retains low state in band");
            Sensor(c,game,6,true);Until(game,null,()=>Rule(game,"sensor.rule").PendingDecision==false,"hysteresis exits after cooldown/dwell");
            Order(c,game,new RemoveAutomationCommand { Rule="sensor.rule" });for(int i=0;i<4;i++)Step(game);
            c.That(!View(game).TryGetAutomationRule(Company,"sensor.rule",out _)&&View(game).TryGetControl(Entity(game,"contract.sensor").Id,new Code("probe"),out control)&&!control.Enabled,"ordinary removal cancels queued restore");
            var privateRules=new List<AutomationRuleSnapshot>();View(game,1).CopyAutomationRules(privateRules);c.Equal(0,privateRules.Count,"rule execution state remains company-private");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"settled native state saves");
            byte[] recording=recorder.Finish();var description=ReplayVerifier.Describe(recording,limits);
            c.That(description.InitialStep==recorder.InitialStep&&description.InitialHash==initialHash,
                "whole control stream starts before the first configuration");
            c.That(description.FinalStep==game.Clock.StepIndex&&description.FinalHash==game.Hash()
                &&description.Commands==recorder.CommandCount&&description.Checkpoints==recorder.CheckpointCount,
                "whole control stream includes every admitted command and final checkpoint");
            c.Equal(game.Clock.StepIndex-recorder.InitialStep,(long)recorder.CheckpointCount,"one checkpoint per earned update, none for paused updates");
            var replay=new ReplayVerifier().Verify(factory,recording,limits);
            c.That(replay.Success&&replay.Step==game.Clock.StepIndex&&replay.ExpectedHash==game.Hash()&&replay.ActualHash==game.Hash(),
                "normal command streams and native contract decisions replay: "+replay.Detail);
            Console.WriteLine("control declaration replay: direct="+declaredRules+";policies="+declaredPolicies+";initial="+description.InitialStep+";final="+description.FinalStep
                +";commands="+description.Commands+";checkpoints="+description.Checkpoints+";live-hash="+game.Hash().ToString("X16")+";verified-hash="+replay.ActualHash.ToString("X16"));
            var fresh=New(factory);c.That(Stock(fresh)==Fix.FromInt(20)&&!Active(fresh)&&Alert(fresh).Version==0,"fresh session does not inherit incidents/material/policy state");
            Invalid(c, declaredRules, declaredPolicies);
            if (declaredRules) InvalidDeclaration(c, declaredPolicies);
            if (declaredPolicies) InvalidPolicies(c);
        }
        private static void Invalid(Check c, bool declaredRules, bool declaredPolicies)
        {
            foreach(var fault in new[]{"rule.missing","rule.wrong","rule.self","rule.mutual","action.missing","action.wrong","action.self","recovery.missing","recovery.wrong","recovery.self","native.action","native.recovery","unit","duration"})
            {
                var init=new InitializeProbe();c.Throws<ContentException>(()=>New(Factory(init,out _,(native,external)=>{
                    var rule=external.Get("independent_control_rules")[0];var action=external.Get("independent_control_actions")[0];var recovery=external.Get("independent_control_recovery")[0];
                    if(fault=="rule.missing")rule.Set("rule",JsonValue.Of("missing"));if(fault=="rule.wrong")rule.Set("rule",JsonValue.Of("automation.alert"));if(fault=="rule.self")rule.Set("rule",JsonValue.Of("contract.incident-alert"));
                    if(fault=="rule.mutual"){rule.Set("rule",JsonValue.Of("contract.sensor-rule"));external.Get("independent_control_rules")[1].Set("rule",JsonValue.Of("contract.incident-alert"));}
                    if(fault=="action.missing")action.Set("policy",JsonValue.Of("missing"));if(fault=="action.wrong")action.Set("policy",JsonValue.Of("automation.alert-inputs"));if(fault=="action.self")action.Set("policy",JsonValue.Of("contract.alert"));
                    if(fault=="recovery.missing")recovery.Set("policy",JsonValue.Of("missing"));if(fault=="recovery.wrong")recovery.Set("policy",JsonValue.Of("automation.alert"));if(fault=="recovery.self")recovery.Set("policy",JsonValue.Of("contract.inputs"));
                    if(fault=="native.action")native.Get("control_rules")[0].Set("action_policy",JsonValue.Of("contract.inputs"));if(fault=="native.recovery")native.Get("control_rules")[0].Set("input_recovery",JsonValue.Of("contract.alert"));
                    if(fault=="unit")native.Get("control_rules")[0].Get("when_false").Set("unit",JsonValue.Of("unit.wrong"));
                    if(fault=="duration")native.Get("control_rules")[0].Get("dwell").Set("numerator",JsonValue.Number("-1"));
                }, declaredRules: declaredRules, declaredPolicies: declaredPolicies)),"early control declaration refusal "+fault);c.Equal(0,init.Initializations,"refusal before Initialize "+fault);
            }
        }
        private static void InvalidDeclaration(Check c, bool declaredPolicies)
        {
            foreach (string fault in new[] { "missing-input", "missing-root", "cycle", "unused-condition", "kind", "reversed-band", "missing-signal", "signal-unit" })
            {
                var init = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(init, out _, (native, external) => {
                    JsonValue rule = external.Get("control_rules")[0];
                    JsonValue condition = rule.Get("conditions")[0];
                    if (fault == "missing-input") condition.Set("input", JsonValue.Of("absent"));
                    if (fault == "missing-root") rule.Set("root", JsonValue.Of("absent"));
                    if (fault == "cycle")
                    {
                        rule.Set("conditions", Json.Parse("[{\"id\":\"low\",\"kind\":\"all\",\"conditions\":[\"low\"],\"initial\":false}]"));
                    }
                    if (fault == "unused-condition") rule.Get("conditions").Add(Json.Parse("{\"id\":\"unused\",\"kind\":\"boolean\",\"input\":\"level\",\"expected\":true,\"initial\":false}"));
                    if (fault == "kind") condition.Set("kind", JsonValue.Of("invented"));
                    if (fault == "reversed-band") condition.Set("on", JsonValue.Number("6"));
                    if (fault == "missing-signal") rule.Get("inputs")[0].Set("signal", JsonValue.Of("absent"));
                    if (fault == "signal-unit") condition.Set("unit", JsonValue.Of("unit.boolean"));
                }, declaredRules: true, declaredPolicies: declaredPolicies)), "direct shared declaration refuses " + fault);
                c.Equal(0, init.Initializations, "invalid direct DAG refuses before Initialize " + fault);
            }
        }
        private static void InvalidPolicies(Check c)
        {
            foreach (string fault in new[] { "alert-command", "alert-owner", "operation-command", "operation-owner", "missing-profile", "missing-role", "duplicate-retry", "invalid-retry", "missing-retry" })
            {
                var init = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(init, out _, (native, external) => {
                    JsonValue alert = native.Get("company_alert_control_actions")[0];
                    JsonValue operation = external.Get("operation_control_actions")[0];
                    JsonValue recovery = native.Get("control_input_recovery_policies")[0];
                    if (fault == "alert-command") alert.Set("command", JsonValue.Of("missing.command"));
                    if (fault == "alert-owner") alert.Set("owner", JsonValue.Of(OperationsModule.ModuleId));
                    if (fault == "operation-command") operation.Set("command", JsonValue.Of("sim.configure-company-alert"));
                    if (fault == "operation-owner") operation.Set("owner", JsonValue.Of(CompanyAlertModule.ModuleId));
                    if (fault == "missing-profile") alert.Get("enabled").Set("profile", JsonValue.Of("missing.profile"));
                    if (fault == "missing-role") alert.Get("enabled").Get("worker_reserves")[0].Set("role", JsonValue.Of("missing.role"));
                    if (fault == "duplicate-retry") recovery.Set("retry_blockers", Json.Parse("[\"probe.sensor-offline\",\"probe.sensor-offline\"]"));
                    if (fault == "invalid-retry") recovery.Set("retry_blockers", Json.Parse("[1]"));
                    if (fault == "missing-retry") recovery.Remove("retry_blockers");
                }, declaredRules: true, declaredPolicies: true)), "direct policy declaration refuses " + fault);
                c.Equal(0, init.Initializations, "direct policy refusal precedes Initialize " + fault);
            }
        }
    }
}
