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
    // Pure JSON composition and ordinary player commands/clock/immutable views only.
    internal static class WeatherIncidentPropagationTests
    {
        internal const string Case = "Weather incidents: authored clock phases govern tile and connected delayed exposures";
        private static readonly Code Leak = new Code("leak"), Spread = new Code("spread");
        internal static void Register() => TestRunner.Add(Case,Run);
        internal static GameFactory Factory(InitializeProbe initialize, out WeatherProofModule proof, bool direct, bool tile,
            Action<JsonValue> edit = null, Action<JsonValue> editRule = null, bool missingReader = false)
        {
            var weather = Json.Parse(FrameworkProofContent.Read("weather-incidents.json"));
            if (direct) { weather.Set("independent_weather_timelines",weather.Get("weather_timelines")); weather.Remove("weather_timelines"); }
            edit?.Invoke(weather); proof = new WeatherProofModule(initialize);
            var modules = new List<GameModule> {new PhysicalLineRemovalProofModule(),new TileConditionsModule(),proof};
            if (!missingReader) modules.Add(new WeatherModule());
            var declarations = Json.Parse("[{\"id\":\"proof.physical-line-removal\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"}],\"documents\":[\"physical-line-removal.json\"]},{\"id\":\"sim.tile-conditions\",\"version\":\"1.0.0\",\"documents\":[]},{\"id\":\"proof.weather\",\"version\":\"1.0.0\",\"documents\":[\"weather-incidents.json\"]}]");
            if (!missingReader) declarations.Add(Json.Parse("{\"id\":\"sim.weather\",\"version\":\"1.0.0\",\"documents\":[]}"));
            return NetworkIncidentPropagationTests.Create(new InitializeProbe("probe.weather-base"),edit: content => {
                var rule = content.Get("network_incident_propagation_rules")[0];
                rule.Get("delay").Set("numerator",JsonValue.Number("8"));
                rule.Set("weather",Json.Parse("{\"provider\":\"weather.local\",\"conditions\":[\"weather.dry\"],\"blocker\":\"weather.rain-stops-spread\"}"));
                if (tile) {
                    rule.Remove("network"); rule.Set("space",JsonValue.Of("recovery.tiles")); rule.Set("radius",JsonValue.Number("2"));
                    rule.Set("source_tiles",Json.Parse("{\"match\":\"all\",\"conditions\":[]}"));
                    rule.Set("target_tiles",Json.Parse("{\"match\":\"all\",\"conditions\":[\"weather.receiver\"]}"));
                    content.Set("tile_incident_propagation_rules",content.Get("network_incident_propagation_rules")); content.Remove("network_incident_propagation_rules");
                }
                editRule?.Invoke(rule);
                content.Get("activities")[0].Get("nodes")[0].Set("requirement",JsonValue.Of("weather.response-water"));
            },editBindings: root => {
                foreach (string entry in new[]{"removal.site.start","removal.crane.start","weather.receiver.start"})
                    root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of(entry));
            },additionalModules: modules,additionalDeclarations: declarations.ToString(),additionalDocuments: new Dictionary<string,string> {
                {"weather-incidents.json",weather.ToString()}, {"physical-line-removal.json",FrameworkProofContent.Read("physical-line-removal.json")} });
        }
        private static SimGame New(GameFactory factory) => NetworkIncidentPropagationTests.New(factory);
        private static WeatherSnapshot Weather(SimGame game, int seat = 0)
        {
            Code refusal = game.Seat(seat).View.Feature<WeatherView>().Read("weather.local",out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("weather read "+refusal); return value;
        }
        private static IncidentEligibilitySnapshot Eligibility(SimGame game, string target = "binding.watcher", int seat = 0)
        {
            Code refusal = game.Seat(seat).View.Feature<IncidentPropagationView>().ReadEligibility(Entity(game,"binding.source"),Spread,Entity(game,target),out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("weather eligibility "+refusal); return value;
        }
        private static IncidentPropagationSnapshot Exposure(SimGame game) => NetworkIncidentPropagationTests.Exposure(game);
        private static IncidentLifecycleView Incident(SimGame game, string entity) => NetworkIncidentPropagationTests.Incident(game,entity,Leak);
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i=0;i<256&&!ready();i++) Step(game,twin);
            if (!ready()) throw new InvalidOperationException(stage+" step="+game.Clock.StepIndex+" weather="+Weather(game).Observation.Condition+
                " pending="+string.Join(",",Exposure(game).Pending.Select(p=>p.DueTick))+" blocker="+Eligibility(game).Observation.Blocker+
                " source="+Incident(game,"binding.source").Blocker);
        }
        private static Fix Stock(SimGame game,string entity,string resource)
        { var rows=new List<Facet>();game.Seat(0).View.Inspect(Entity(game,entity),rows);return rows.Where(f=>f.Key==Storage.FacetStock&&f.Subject==resource).Aggregate(Fix.Zero,(sum,f)=>sum+f.Value); }
        private static void Supply(Check c,SimGame game,string entity)
        {
            game.Seat(0).View.TryFindResource("water",out var water);
            Order(c,game,new HaulResourcesCommand {Source=Entity(game,"depot"),Destination=Entity(game,entity),Worker=Entity(game,"truck"),Resource=water,Ceiling=Fix.One,Priority=100});
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current weather build"); c.Equal(154,EngineInfo.SaveFormat,"no new clock/weather state codec");
            c.That(typeof(IndependentWeatherDef).BaseType==typeof(Def),"independent weather needs no native metadata base");
            foreach (bool tile in new[]{false,true}) c.Equal(Physical(c,false,tile),Physical(c,true,tile),"native/direct weather chronology "+tile);
            Timeline(c); Invalid(c); Faults(c);
        }
        private static long Physical(Check c,bool direct,bool tile)
        {
            var initialize=new InitializeProbe(); var factory=Factory(initialize,out _,direct,tile);var game=New(factory);var isolated=New(factory);ulong isolatedHash=isolated.Hash();
            c.Equal(2,initialize.Initializations,"each explicit master initializes once"); c.That(game.Modules.Contains(WeatherModule.ModuleId)&&!game.Modules.Contains(TycoonModule.ModuleId),"explicit weather without business");
            var limits=new ReplayLimits(4*1024*1024,3000);var recorder=game.RecordReplay(limits);
            c.Equal(new Code("weather.dry"),Weather(game).Observation.Condition,"authored initial weather");
            Order(c,game,new RaiseIncidentCommand {Target=Entity(game,"binding.source"),Source=Leak,Kind=new Code("network.fire")});
            Reach(game,null,()=>Exposure(game).Pending.Count==1,"dry weather schedules original receipt");var pending=Exposure(game).Pending.Single();
            c.That(pending.Target==Entity(game,"binding.watcher")&&pending.DueTick-pending.ScheduledTick==8,"existing topology/terrain/distance and exact authored delay");
            c.That(!Eligibility(game,"binding.remote").Observation.Eligible,"remote target fails existing rule, no global weather damage");
            var retained=Weather(game);var eligibility=Eligibility(game);ulong hash=game.Hash();Weather(game);Eligibility(game);
            c.Equal(hash,game.Hash(),"weather/eligibility read pure");c.Equal(retained.Observation.Condition,Weather(game,1).Observation.Condition,"public authored world weather is shared, private incidents are not");
            c.Equal(Codes.NotFound,game.Seat(1).View.Feature<IncidentPropagationView>().ReadEligibility(Entity(game,"binding.source"),Spread,Entity(game,"binding.watcher"),out var hidden),"foreign incident context hidden");c.That(hidden==null,"no foreign weather-linked episode facts");
            var twin=factory.Load(game.Save());long at=game.Clock.StepIndex;game.Clock.Pause();twin.Clock.Pause();hash=game.Hash();Step(game,twin);
            c.That(at==game.Clock.StepIndex&&hash==game.Hash()&&Weather(game).Observation.ElapsedSteps==retained.Observation.ElapsedSteps,"sole clock pauses weather and exposure");game.Clock.Resume();twin.Clock.Resume();
            Reach(game,twin,()=>Weather(game).Observation.Condition==new Code("weather.wet"),"authored rain boundary");
            c.Equal(new Code("weather.rain-stops-spread"),Eligibility(game).Observation.Blocker,"weather selection returns authored typed blocker");
            c.That(retained.Observation.Condition==new Code("weather.dry")&&eligibility.Observation.Eligible,"retained weather/eligibility detached");
            twin=factory.Load(game.Save());Reach(game,twin,()=>game.Clock.StepIndex>pending.DueTick,"due-time weather re-observation");
            c.That(!Incident(game,"binding.watcher").Snapshot.Active&&Exposure(game).Pending.Count==0,"rain prevents queued damage, no stale exposure");
            Reach(game,twin,()=>Exposure(game).Pending.Count==1,"later dry phase earns fresh receipt");var fresh=Exposure(game).Pending.Single();
            c.That(fresh.ScheduledTick>pending.DueTick&&fresh.DueTick-fresh.ScheduledTick==8&&Weather(game).Observation.Condition==new Code("weather.dry"),"fresh full delay, not original deadline or elapsed guess");
            twin=factory.Load(game.Save());Reach(game,twin,()=>Incident(game,"binding.watcher").Snapshot.Active,"fresh favorable due exposure");
            c.Equal(1,Incident(game,"binding.watcher").Snapshot.Episode,"exact one target episode");
            c.That(Incident(game,"binding.source").Snapshot.Active&&!Incident(game,"binding.source").Blocker.IsNone,"machine response still waits for physical water");
            Supply(c,game,"binding.source");twin=factory.Load(game.Save());Reach(game,twin,()=>!Incident(game,"binding.source").Snapshot.Active,"real supplied timed source repair");
            Supply(c,game,"binding.watcher");twin=factory.Load(game.Save());Reach(game,twin,()=>!Incident(game,"binding.watcher").Snapshot.Active,"real supplied timed target repair");
            c.That(Stock(game,"depot","water")==Fix.FromInt(8)&&Stock(game,"binding.source","water")+Stock(game,"binding.watcher","water")+Stock(game,"truck","water")==Fix.Zero,"two finite hauled water consumed once");
            c.That(!Incident(game,"binding.remote").Snapshot.Active,"unselected target unaffected");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"weather and original gameplay save/load checkpoint");c.Equal(isolatedHash,isolated.Hash(),"interleaved weather session unchanged");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"ordinary player inputs/one clock weather/full physical continuation replay");return game.Clock.StepIndex;
        }
        private static void Timeline(Check c)
        {
            foreach(bool repeat in new[]{false,true})
            {
                var factory=Factory(new InitializeProbe(),out _,true,false,edit:doc=>doc.Get("independent_weather_timelines")[0].Set("repeat",JsonValue.Of(repeat)));
                var game=New(factory);ulong before=game.Hash();var view=game.Seat(0).View.Feature<WeatherView>();
                c.Equal(Codes.UnknownDef,view.Read("missing",out var absent),"missing weather is not invented");c.That(absent==null,"no missing weather facts");c.Equal(before,game.Hash(),"unknown read pure");
                Reach(game,null,()=>game.Clock.StepIndex>=54,"authored cycle end");var value=Weather(game).Observation;
                c.That(repeat?value.Condition==new Code("weather.dry")&&value.ElapsedSteps==0:value.Blocker==WeatherTimelineDeclaration.Exhausted&&value.Condition.IsNone,"explicit repeat/exhaustion without fabricated held-last state");
                var twin=factory.Load(game.Save());Step(game,twin);c.Equal(Weather(game).Observation.Condition,Weather(twin).Observation.Condition,"saved phase projection identical");
            }
        }
        private static void Invalid(Check c)
        {
            foreach(bool direct in new[]{false,true}) foreach(string fault in new[]{"repeat","empty","duplicate","zero","nonexact","unknown-key","missing-provider","unknown-condition","empty-selection","duplicate-selection","selection-key"})
            {
                var initialize=new InitializeProbe();c.Throws<ContentException>(()=>New(Factory(initialize,out _,direct,false,edit:doc=>{
                    var row=doc.Get(direct?"independent_weather_timelines":"weather_timelines")[0];
                    if(fault=="repeat")row.Remove("repeat");if(fault=="empty")row.Set("phases",Json.Parse("[]"));
                    if(fault=="duplicate")row.Get("phases")[1].Set("id",row.Get("phases")[0].Get("id"));
                    if(fault=="zero")row.Get("phases")[0].Get("duration").Set("numerator",JsonValue.Number("0"));
                    if(fault=="nonexact")row.Get("phases")[0].Get("duration").Set("denominator",JsonValue.Number("7"));
                    if(fault=="unknown-key")row.Get("phases")[0].Set("host_weather",JsonValue.Number("1"));
                },editRule:rule=>{
                    var selection=rule.Get("weather");
                    if(fault=="missing-provider")selection.Set("provider",JsonValue.Of("missing"));
                    if(fault=="unknown-condition")selection.Set("conditions",Json.Parse("[\"weather.missing\"]"));
                    if(fault=="empty-selection")selection.Set("conditions",Json.Parse("[]"));
                    if(fault=="duplicate-selection")selection.Get("conditions").Add(JsonValue.Of("weather.dry"));
                    if(fault=="selection-key")selection.Set("fallback",JsonValue.Of(true));
                })),"malformed loaded weather "+direct+"/"+fault);c.Equal(0,initialize.Initializations,"invalid weather never initializes "+direct+"/"+fault);
            }
            var missing=new InitializeProbe();c.Throws<ContentException>(()=>New(Factory(missing,out _,true,false,missingReader:true)),"selected independent weather requires actual reader");c.Equal(0,missing.Initializations,"missing reader refuses before Initialize");
        }
        private static void Faults(Check c)
        {
            foreach(string fault in new[]{"throw","pause","map","recursive","malformed","unknown"})
            {
                var factory=Factory(new InitializeProbe(),out var proof,true,false);var game=New(factory);var view=game.Seat(0).View.Feature<WeatherView>();proof.Arm(fault);
                c.Throws<InvalidOperationException>(()=>view.Read("weather.local",out _),"actual selected weather observation rejects "+fault);
                c.That(game.Fault!=null,"first weather fault latched "+fault);if(fault=="throw")c.That(game.Fault.Message.Contains("proof.weather.original"),"original weather exception preserved");
                c.Throws<InvalidOperationException>(()=>game.Save(),"faulted weather cannot save "+fault);c.Throws<InvalidOperationException>(()=>view.Read("missing",out _),"faulted missing weather read cannot hide fault "+fault);
            }
        }
    }
}
