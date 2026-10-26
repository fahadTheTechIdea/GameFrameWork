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
    // Pure file assembly and public player/observer inputs only. The engine owns every exposure/work rule.
    internal static class PropagationContentContractTests
    {
        internal const string Case = "Propagation contracts: direct JSON rules, delayed physical recovery and private saved exposure";
        private static readonly Code Source = new Code("pressure.leak"), Spread = new Code("spread");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Build(InitializeProbe initialize, out PropagationContentModule proof, bool independent = true,
            Action<JsonValue> edit = null, Action<JsonValue> conditionsEdit = null, Action<JsonValue> responseEdit = null, int budget = 20)
        {
            var authored = Json.Parse(FrameworkProofContent.Read("propagation-contracts.json")); edit?.Invoke(authored);
            proof = new PropagationContentModule(initialize);
            return Factory(initialize, out _, extension: proof,
                contentDocuments: new Dictionary<string,string> { ["propagation-contracts.json"] = authored.ToString() },
                additionalModules: new GameModule[] { new IncidentPropagationModule() },
                additionalDeclarations: Json.Parse("[{\"id\":\"sim.incident-propagation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.incidents\",\"version\":\"1.0.0\"}],\"documents\":[]}]"),
                responseEdit: responseEdit,
                scenarioEdit: s => { s.Get("modules").Add(JsonValue.Of(IncidentPropagationModule.ModuleId)); s.Get("settings").Set(IncidentPropagationModule.ModuleId,Json.Parse("{\"budget\":"+budget+"}")); },
                conditionsEdit: document => {
                    var entities = document.Get("entities"); var setups = document.Get("entity_setups");
                    foreach (var row in new[] { (Id: "field.receiver", At: 22), (Id: "field.far", At: 80) })
                    {
                        var entity = entities[0].Clone(); entity.Set("id",JsonValue.Of(row.Id)); entity.Get("components").Get("point_presence").Set("position",JsonValue.Number(row.At.ToString())); entities.Add(entity);
                        var setup = setups[0].Clone(); setup.Set("id",JsonValue.Of(row.Id+".start")); setup.Set("entity",JsonValue.Of(row.Id)); setup.Get("placement").Set("position",JsonValue.Number(row.At.ToString())); setups.Add(setup);
                        document.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of(row.Id+".start"));
                    }
                    entities[1].Get("components").Get("point_presence").Set("position",JsonValue.Number("23")); setups[1].Get("placement").Set("position",JsonValue.Number("23"));
                    for(int i=0;i<entities.Count;i++)
                    { entities[i].Get("components").Set("exposure_private_probe",Json.Parse("{}")); entities[i].Get("components").Get("incident_signal").Set("phase",JsonValue.Of("detection")); }
                    var binding=Json.Parse("{\"id\":\"spread\",\"source\":\"pressure.leak\",\"rule\":\"contract.field-spread\"}");
                    if(independent)binding.Set("metadata_fault",JsonValue.Of("none"));
                    entities[0].Get("components").Set(independent?"independent_propagation":"incident_propagation",binding);
                    var held=document.Get("signals")[0].Clone();held.Set("id",JsonValue.Of("field.detection-held"));held.Set("metric",JsonValue.Of("incident.phase-hold"));document.Get("signals").Add(held);
                    conditionsEdit?.Invoke(document);
                });
        }
        private static JsonValue RuleJson(JsonValue document) => document.Get("independent_incident_propagation_rules")[0];
        private static EntityId Id(SimGame game,string def="field.plant",int company=0)=>Entity(game,def,company).Id;
        private static IncidentLifecycleView Life(SimGame game,string def="field.plant",int company=0)
        { if(!game.Seat(company).View.Feature<IncidentsView>().TryGetLifecycle(Id(game,def,company),Source,out var value))throw new InvalidOperationException("missing owned incident "+def);return value; }
        private static IncidentPropagationSnapshot Exposure(SimGame game,bool observer=false)
        { var view=(observer?game.Observer:game.Seat(0).View).Feature<IncidentPropagationView>();if(!view.TryRead(Id(game),Spread,out var value))throw new InvalidOperationException("missing owned exposure");return value; }
        private static bool Detected(SimGame game)
        { var phases=new List<IncidentPhaseView>();game.Seat(0).View.Feature<IncidentsView>().CopyPhases(Id(game),Source,phases);return phases.Single(p=>p.Id=="detection").Complete; }
        private static Fix StockAt(SimGame game,string def="field.receiver")
        {var facets=new List<Facet>();game.Seat(0).View.Inspect(Id(game,def),facets);return facets.Where(f=>f.Key==Storage.FacetStock&&f.Subject=="supply").Aggregate(Fix.Zero,(n,f)=>n+f.Value);}
        private static void Raise(Check c,SimGame game,string def="field.plant")=>Order(c,game,new RaiseIncidentCommand{Target=Id(game,def),Source=Source,Kind=Source});
        private static void EnableWorker(Check c,SimGame game)=>Order(c,game,new ConfigureWorkPolicyCommand{Worker=Id(game,"machine"),Settings=new WorkPolicySettings(true,0,new[]{new WorkerRoleId("haul"),new WorkerRoleId("inspect")},null,true,true,WorkAssignmentMode.All)});
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current propagation contract identity");c.Equal(154,EngineInfo.SaveFormat,"original exposure1/work/episode2 schemas");
            c.That(Wave(c,false).SequenceEqual(Wave(c,true)),"native and independent emitter metadata use identical earned timing");
            Isolation(c);Stale(c);Companies(c);Bounded(c);Invalid(c);Faults(c);
        }
        private static long[] Wave(Check c,bool independent)
        {
            var initialize=new InitializeProbe();var factory=Build(initialize,out var proof,independent);var game=New(factory);
            c.That(proof.ActualRule&&proof.IndependentBinding==independent,"actual direct rule and original reusable emitter aliases");c.Equal(1,initialize.Initializations,"Initialize once");
            Step(game);var inactive=Exposure(game);c.That(inactive.ObservedEpisode==0&&inactive.Pending.Count==0&&inactive.NextCycleTick==-1,"no invented exposure schedule");
            var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,3000));Raise(c,game);
            c.That(Active(game)&&!Life(game,"field.receiver").Snapshot.Active&&StockAt(game)==Fix.FromInt(20),"ordinary raise cannot instantly damage target or spend stock");
            Until(game,null,()=>Exposure(game).Pending.Any(p=>p.Target==Id(game,"field.receiver")),"actual delayed target admission");
            var before=Exposure(game);var pending=before.Pending.Single(p=>p.Target==Id(game,"field.receiver"));
            c.That(pending.ExpectedEpisode==0&&pending.DueTick-pending.ScheduledTick==6&&pending.ScheduledTick>Life(game).Snapshot.RaisedTick,"exact interval then six authored game minutes");
            c.Throws<NotSupportedException>(()=>((IList<PendingExposureSnapshot>)before.Pending).Clear(),"pending facts immutable");
            c.That(before.ScanAfter==EntityId.None&&before.ScanThrough==EntityId.None,"private view hides global scan identities");
            var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"original pending exposure checkpoint");
            game.Clock.Pause();twin.Clock.Pause();long at=game.Clock.StepIndex;ulong hash=game.Hash();Step(game,twin);c.That(game.Clock.StepIndex==at&&game.Hash()==hash,"one paused clock freezes delayed exposure");game.Clock.Resume();twin.Clock.Resume();
            Until(game,twin,()=>Life(game,"field.receiver").Snapshot.Active,"delayed authoritative target Raise");var received=Life(game,"field.receiver");
            c.That(received.Snapshot.Episode==1&&received.Snapshot.RaisedTick==pending.DueTick&&received.OrderId>0,"due receipt enrolls target in original physical response");
            c.That(!Life(game,"field.far").Snapshot.Active&&!Life(game,"field.foreign-plant",1).Snapshot.Active,"radius and same-company policy refuse distant/foreign targets");
            c.That(before.Pending.Count==1&&StockAt(game)==Fix.FromInt(20),"retained exposure detached; arrival grants no repair/material mutation");
            EnableWorker(c,game);twin=factory.Load(game.Save());Until(game,twin,()=>!Active(game)&&!Life(game,"field.receiver").Snapshot.Active,"ordinary physical source and target recovery");
            c.That(Stock(game)==Fix.FromInt(12)&&StockAt(game)==Fix.FromInt(12)&&((PointSnapshot)Entity(game,"machine").Spatial).Position==Fix.FromInt(22),"same real machine travels4 to20 to22 and consumes each actual bill");
            Until(game,twin,()=>Exposure(game).Blocker==IncidentPropagationCodes.SourceEnded,"source completion stops its exposure cause");
            c.That(Exposure(game).Pending.Count==0&&Exposure(game).NextCycleTick==-1&&Life(game,"field.receiver").Snapshot.Episode==1,"no duplicate exposure after resolved source");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"resolved original exposure/incident/work history checkpoint");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,3000)).Success,"normal incident/work and delayed propagation replay");
            var fresh=New(factory);Step(fresh);c.That(!Active(fresh)&&Exposure(fresh).ObservedEpisode==0&&StockAt(fresh)==Fix.FromInt(20),"interleaved session owns distinct exposure/stock/episodes");
            return new[]{pending.ScheduledTick,pending.DueTick,received.Snapshot.RaisedTick,Life(game).Snapshot.ResolvedTick,Life(game,"field.receiver").Snapshot.ResolvedTick};
        }
        private static void Isolation(Check c)
        {
            var factory=Build(new InitializeProbe(),out _,edit:j=>{RuleJson(j).Get("delay").Set("numerator",JsonValue.Number("60"));RuleJson(j).Set("source_conditions",Json.Parse("[{\"id\":\"isolation\",\"source\":\"incident-state\",\"signal\":\"field.detection-held\",\"expected\":false,\"blocker\":\"field.isolated\"}]"));},
                responseEdit:j=>{var detect=j.Get("activities")[0].Get("nodes")[0];detect.Set("executor",JsonValue.Of("work.staffed"));detect.Set("roles",Json.Parse("[\"inspect\"]"));});
            var game=New(factory);EnableWorker(c,game);Raise(c,game);
            Until(game,null,()=>Exposure(game).Pending.Any(p=>p.Target==Id(game,"field.receiver")),"physical isolation pending exposure");long due=Exposure(game).Pending.First(p=>p.Target==Id(game,"field.receiver")).DueTick;
            Until(game,null,()=>Detected(game),"machine reaches source and earns isolation phase");
            c.That(Active(game)&&game.Clock.StepIndex<due&&((PointSnapshot)Entity(game,"machine").Spatial).Position==Fix.FromInt(20),"physical machine isolation precedes delayed delivery");
            Order(c,game,new SetWorkerHeldCommand{Worker=Id(game,"machine"),Held=true});int order=Life(game).OrderId;Order(c,game,new CancelOrderCommand{Order=order});
            var twin=factory.Load(game.Save());Until(game,twin,()=>game.Clock.StepIndex>due+2,"saved isolated due-time recheck");
            c.That(Active(game)&&!Life(game,"field.receiver").Snapshot.Active&&Life(game).OrderId==order&&StockAt(game)==Fix.FromInt(20),"current earned phase suppresses stale exposure despite cancelled/resumed active episode");
            Order(c,game,new SetWorkerHeldCommand{Worker=Id(game,"machine"),Held=false});twin=factory.Load(game.Save());Until(game,twin,()=>!Active(game),"held response physically resumes and resolves");
            c.That(Stock(game)==Fix.FromInt(12)&&!Life(game,"field.receiver").Snapshot.Active,"isolation/recovery never fabricates target damage or repair stock");
        }
        private static void Stale(Check c)
        {
            var factory=Build(new InitializeProbe(),out _,edit:j=>RuleJson(j).Get("delay").Set("numerator",JsonValue.Number("60")),conditionsEdit:j=>j.Get("entities")[0].Get("components").Get("storage").Set("initial_stock",Json.Parse("{}")));
            var game=New(factory);Raise(c,game);Until(game,null,()=>Exposure(game).Pending.Any(p=>p.Target==Id(game,"field.receiver")),"preexisting target episode receipt");
            var pending=Exposure(game).Pending.First(p=>p.Target==Id(game,"field.receiver"));Raise(c,game,"field.receiver");EnableWorker(c,game);var twin=factory.Load(game.Save());
            Until(game,twin,()=>!Life(game,"field.receiver").Snapshot.Active,"target independently physically recovers");
            c.That(Active(game)&&Life(game,"field.receiver").Snapshot.Episode==1&&game.Clock.StepIndex<pending.DueTick&&StockAt(game)==Fix.FromInt(12),"source shortage retains active cause while target earns a different completed episode");
            twin=factory.Load(game.Save());Until(game,twin,()=>game.Clock.StepIndex>pending.DueTick,"stale expected-episode delivery");
            c.That(!Life(game,"field.receiver").Snapshot.Active&&Life(game,"field.receiver").Snapshot.Episode==1&&StockAt(game)==Fix.FromInt(12),"old expected episode cannot damage a recovered target or spend twice");
        }
        private static void Companies(Check c)
        {
            var factory=Build(new InitializeProbe(),out _,edit:j=>RuleJson(j).Set("companies",JsonValue.Of("any")));var game=New(factory);Raise(c,game);
            Until(game,null,()=>Exposure(game,true).Pending.Any(p=>p.Target==Id(game,"field.foreign-plant",1)),"authored foreign pending exposure");
            c.That(!Exposure(game).Pending.Any(p=>p.Target==Id(game,"field.foreign-plant",1)),"company-private pending diagnostics hide foreign identity");
            c.That(!game.Seat(1).View.Feature<IncidentPropagationView>().TryRead(Id(game),Spread,out _),"foreign emitter diagnostics denied");
            var twin=factory.Load(game.Save());Until(game,twin,()=>Life(game,"field.foreign-plant",1).Snapshot.Active,"explicit any-company delayed pressure");
            c.Equal(1,Life(game,"field.foreign-plant",1).Snapshot.Episode,"same ordinary target owner, no privileged duplicate state");
        }
        private static void Bounded(Check c)
        {
            var factory=Build(new InitializeProbe(),out _,budget:1);var game=New(factory);Raise(c,game);Until(game,null,()=>Exposure(game,true).Scanning,"bounded captured scan");
            var captured=Exposure(game,true);c.That(captured.ScanThrough.IsValid&&captured.ScanAfter.IsValid,"original budget owner retains captured source membership cursor");
            var twin=factory.Load(game.Save());Until(game,twin,()=>Life(game,"field.receiver").Snapshot.Active,"saved bounded budget fair progress");
            c.That(Life(game,"field.receiver").Snapshot.Episode==1&&captured.Pending.Count==0,"bounded continuation neither duplicates receipt nor mutates retained scan facts");
        }
        private static void Invalid(Check c)
        {
            foreach(var fault in new[]{"interval","delay","inexact","companies","target","radius","source-condition","target-condition","interval-zero","target-none","validation-throw","validation-pause","validation-reenter","binding-rule","binding-source","binding-copy","binding-null","binding-unbound"})
            {
                var initialize=new InitializeProbe();Action create=()=>New(Build(initialize,out _,edit:j=>{
                    var rule=RuleJson(j).Clone();rule.Set("id",JsonValue.Of("contract.unused"));j.Get("independent_incident_propagation_rules").Add(rule);
                    if(fault=="interval")rule.Get("interval").Set("numerator",JsonValue.Number("0"));if(fault=="delay")rule.Remove("delay");if(fault=="inexact")rule.Get("interval").Set("denominator",JsonValue.Number("2"));
                    if(fault=="companies")rule.Set("companies",JsonValue.Of("implicit"));if(fault=="target")rule.Set("target_source",JsonValue.Of("missing"));if(fault=="radius")rule.Set("radius",JsonValue.Number("-1"));
                    if(fault=="source-condition")rule.Set("source_conditions",Json.Parse("[{\"id\":\"bad\",\"source\":\"missing\",\"signal\":\"missing\",\"expected\":false,\"blocker\":\"field.bad\"}]"));
                    if(fault=="target-condition")rule.Set("target_conditions",Json.Parse("[{\"id\":\"bad\",\"source\":\"missing\",\"signal\":\"field.incident-active\",\"expected\":false,\"blocker\":\"field.bad\"}]"));
                    if(fault=="interval-zero"||fault=="target-none"||fault.StartsWith("validation-"))rule.Set("fault",JsonValue.Of(fault));
                },conditionsEdit:j=>{
                    var unused=j.Get("entities")[0].Clone();unused.Set("id",JsonValue.Of("field.unused"));j.Get("entities").Add(unused);var parts=unused.Get("components");var binding=parts.Get("independent_propagation");
                    binding.Set("rule",JsonValue.Of("contract.unused"));
                    if(fault=="binding-rule")binding.Set("rule",JsonValue.Of("pressure.leak"));if(fault=="binding-source")binding.Set("source",JsonValue.Of("missing"));
                    if(fault=="binding-copy")binding.Set("metadata_fault",JsonValue.Of("rule-copy"));if(fault=="binding-null")binding.Set("metadata_fault",JsonValue.Of("rule-null"));
                    if(fault=="binding-unbound"){binding.Remove("metadata_fault");parts.Remove("independent_propagation");parts.Set("unbound_propagation",binding);}
                }));
                if(fault.StartsWith("validation-"))c.Throws<InvalidOperationException>(create,"pure rule validation refusal "+fault);else c.Throws<ContentException>(create,"early unused propagation refusal "+fault);
                c.Equal(0,initialize.Initializations,"rejected before Initialize "+fault);
            }
        }
        private static void Faults(Check c)
        {
            foreach(var fault in new[]{"observe-default","observe-throw","observe-pause","observe-touch","observe-source-private","observe-target-private","observe-reenter"})
            {
                var factory=Build(new InitializeProbe(),out _,edit:j=>RuleJson(j).Set("fault",JsonValue.Of(fault)));var game=New(factory);Raise(c,game);
                for(int i=0;i<30&&game.Fault==null;i++)game.Update();
                c.That(game.Fault?.Phase=="observation.provider","actual eligibility/provider codec/clock boundary faults "+fault);
                c.Throws<InvalidOperationException>(()=>Life(game,"field.receiver"),"faulted exposure cannot advertise a healthy live incident view "+fault);
                c.Throws<InvalidOperationException>(()=>game.Save(),"faulted exposure cannot checkpoint "+fault);c.Throws<InvalidOperationException>(()=>game.Hash(),"faulted exposure cannot advertise healthy hash "+fault);
            }
        }
    }
}
