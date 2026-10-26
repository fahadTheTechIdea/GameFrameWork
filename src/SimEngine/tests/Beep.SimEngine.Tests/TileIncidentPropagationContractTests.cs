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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Master-listed content and ordinary player commands/views/clock/save only, never map or damage writes.
    internal static partial class TileIncidentPropagationContractTests
    {
        internal const string Case = "Tile incidents: authored footprint conditions reobserve physical changes and supplied response";
        private static readonly Code Leak = new Code("leak"), Spread = new Code("spread");
        internal static void Register() { TestRunner.Add(Case, Run); TestRunner.Add(PreparationCase, Preparation); }
        private static GameFactory Factory(InitializeProbe initialize, out TileIncidentPropagationProofModule proof,
            bool direct, bool any = false, Action<JsonValue> edit = null, Action<JsonValue> editBindings = null,
            string exposureDocument = null)
        {
            var terrain = Json.Parse(FrameworkProofContent.Read("terrain-incidents.json"));
            var removal = Json.Parse(FrameworkProofContent.Read("physical-line-removal.json"));
            var rule = terrain.Get("tile_incident_propagation_rules")[0];
            rule.Get("source_tiles").Set("match", JsonValue.Of(any ? "any" : "all"));
            if (direct) {
                terrain.Set("independent_tile_propagation_rules", terrain.Get("tile_incident_propagation_rules")); terrain.Remove("tile_incident_propagation_rules");
                terrain.Set("independent_terrain_conditions", terrain.Get("tile_layer_band_conditions")); terrain.Remove("tile_layer_band_conditions");
            }
            edit?.Invoke(terrain); proof = new TileIncidentPropagationProofModule(initialize);
            var declarations = Json.Parse("[{\"id\":\"proof.physical-line-removal\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"}],\"documents\":[\"physical-line-removal.json\"]},{\"id\":\"sim.tile-conditions\",\"version\":\"1.0.0\",\"documents\":[]},{\"id\":\"proof.terrain-incidents\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.incident-propagation\",\"version\":\"1.0.0\"},{\"id\":\"sim.world-setup\",\"version\":\"1.0.0\"},{\"id\":\"sim.tile-conditions\",\"version\":\"1.0.0\"}],\"documents\":[\"terrain-incidents.json\"]}]");
            var documents = new Dictionary<string,string> { { "physical-line-removal.json", removal.ToString() }, { "terrain-incidents.json", terrain.ToString() } };
            if (exposureDocument != null) {
                declarations[2].Get("documents").Add(JsonValue.Of("exposure-preparation.json"));
                documents.Add("exposure-preparation.json", exposureDocument);
            }
            return NetworkIncidentPropagationTests.Create(new InitializeProbe("probe.terrain-base"), edit: content => {
                content.Get("activities")[0].Get("nodes")[0].Set("requirement", JsonValue.Of("terrain.response-water"));
            }, editBindings: root => {
                root.Get("tile_layer_setups")[0].Set("width", JsonValue.Number("3")); root.Get("tile_layer_setups")[1].Set("width", JsonValue.Number("3"));
                root.Get("entity_setups")[1].Get("placement").Set("x", JsonValue.Number("13"));
                root.Get("entities")[1].Get("components").Get("tile_presence").Set("width", JsonValue.Number("2"));
                var parts = root.Get("entities")[0].Get("components"); parts.Remove("incident_propagation");
                parts.Get("storage").Set("initial_stock", Json.Parse("{\"steel\":8}"));
                var entries = root.Get("world_setup_profiles")[0].Get("entries");
                foreach (string entry in new[] { "removal.site.start", "removal.crane.start", "terrain.source.start", "terrain.receiver.start" }) entries.Add(JsonValue.Of(entry));
                editBindings?.Invoke(root);
            }, additionalModules: new GameModule[] { new PhysicalLineRemovalProofModule(), new TileConditionsModule(), proof },
                additionalDeclarations: declarations.ToString(), additionalDocuments: documents);
        }
        private static JsonValue Rule(JsonValue doc, bool direct) => doc.Get(direct ? "independent_tile_propagation_rules" : "tile_incident_propagation_rules")[0];
        private static SimGame New(GameFactory factory) => NetworkIncidentPropagationTests.New(factory);
        private static IncidentPropagationView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<IncidentPropagationView>();
        private static IncidentPropagationSnapshot Exposure(SimGame game)
        {
            if (!View(game).TryRead(Entity(game,"terrain.source"), Spread, out var facts)) throw new InvalidOperationException("missing terrain exposure");
            return facts;
        }
        private static IncidentEligibilitySnapshot Eligibility(SimGame game, string target = "binding.watcher")
        {
            Code refusal = View(game).ReadEligibility(Entity(game,"terrain.source"), Spread, Entity(game,target), out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("terrain eligibility " + refusal);
            return facts;
        }
        private static IncidentLifecycleView Incident(SimGame game, string target)
            => NetworkIncidentPropagationTests.Incident(game,target,Leak);
        private static Fix Amount(SimGame game, string entity, string resource)
        {
            var rows = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game,entity),rows);
            return rows.Where(f=>f.Key==Storage.FacetStock && f.Subject==resource).Aggregate(Fix.Zero,(s,f)=>s+f.Value);
        }
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i=0;i<256&&!ready();i++) Step(game,twin);
            if (!ready()) throw new InvalidOperationException(stage+" update="+game.Clock.StepIndex+" layer11="+PhysicalLineRemovalTests.Tile(game,11)
                +" pending="+string.Join(",",Exposure(game).Pending.Select(p=>p.Target+"/"+p.DueTick))
                +" eligibility="+Eligibility(game).Observation.Blocker+" source="+Incident(game,"terrain.source").Blocker);
        }
        private static void Supply(Check c, SimGame game, EntityId target, string resource)
        {
            game.Seat(0).View.TryFindResource(resource,out var id);
            Order(c,game,new HaulResourcesCommand {Source=Entity(game,"depot"),Destination=target,Worker=Entity(game,"truck"),Resource=id,Ceiling=Fix.One,Priority=100});
        }
        private static void Raise(Check c, SimGame game) => Order(c,game,new RaiseIncidentCommand {
            Target=Entity(game,"terrain.source"),Source=Leak,Kind=new Code("network.fire")});
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual terrain engine build");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"current save protocol; original work/map/episode/exposure owners retained");
            foreach (bool any in new[] { false,true })
                c.Equal(Physical(c,false,any),Physical(c,true,any),"native/direct identical physical chronology "+any);
            Selection(c); Invalid(c); Faults(c);
        }
        private static long Physical(Check c, bool direct, bool any)
        {
            var initialize = new InitializeProbe(); var factory=Factory(initialize,out var proof,direct,any);
            var game=New(factory); var isolated=New(factory); ulong isolatedHash=isolated.Hash();
            c.That(proof.ActualAliases&&proof.OneExposureOwner,"actual typed aliases and one original exposure owner");
            c.Equal(2,initialize.Initializations,"each selected session initializes once");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId),"physical response independent of optional business");
            c.Equal(IncidentPropagationCodes.Inactive,Eligibility(game).Observation.Blocker,"inactive source cannot expose a target");
            var limits=new ReplayLimits(4*1024*1024,4000);var recorder=game.RecordReplay(limits);
            Raise(c,game); Reach(game,null,()=>Exposure(game).Pending.Count==1,"initial authored eligible target");
            var pending=Exposure(game).Pending.Single();var retained=Eligibility(game);ulong hash=game.Hash();
            c.That(pending.Target==Entity(game,"binding.watcher")&&pending.DueTick-pending.ScheduledTick==60,"mask and actual distance select exactly one delayed receipt");
            c.That(retained.Observation.Eligible&&retained.Rule=="terrain.spread"&&retained.AuthoredDelaySteps==60
                &&retained.SourceEpisode==1&&retained.ObservedUpdate==game.Clock.StepIndex,"pure current eligibility and authored clock facts");
            c.Equal(IncidentPropagationCodes.TargetTerrain,Eligibility(game,"binding.source").Observation.Blocker,"nearby unmasked target is not silently exposed");
            c.Equal(IncidentPropagationCodes.Distance,Eligibility(game,"binding.remote").Observation.Blocker,"remote distance refuses before terrain");
            c.Equal(hash,game.Hash(),"eligibility reads neither advance scans nor change map/episode/work state");
            c.Equal(Codes.NotFound,View(game,1).ReadEligibility(Entity(game,"terrain.source"),Spread,Entity(game,"binding.watcher"),out var hidden),"private source and target query refuses");
            c.That(hidden==null,"foreign query returns no episode/condition/clock facts");
            c.Equal(Codes.InvalidArgument,View(game).ReadEligibility(Entity(game,"terrain.source"),Code.None,Entity(game,"binding.watcher"),out var unnamed),"named emitter required");
            c.That(unnamed==null,"invalid emitter has no substituted facts");
            Order(c,game,PhysicalLineRemovalTests.Cut(game));
            int cut=PhysicalLineRemovalTests.Orders(game).Single(o=>o.Plan=="activity:removal.work").Id;
            Reach(game,null,()=>PhysicalLineRemovalTests.Nodes(game,cut).Single().ProductiveSteps>0,"machine physically reaches cut");
            c.Equal(1,PhysicalLineRemovalTests.Tile(game,11),"partial work cannot change terrain");
            var twin=factory.Load(game.Save()); Reach(game,twin,()=>PhysicalLineRemovalTests.Tile(game,11)==0,"earned layer change");
            c.That(game.Clock.StepIndex<pending.DueTick&&PhysicalLineRemovalTests.Tile(game,12)==1,"one footprint cell cut before due, second physically retained");
            var changed=Eligibility(game);
            c.That(changed.Observation.Eligible==any&&(any||changed.Observation.Blocker==IncidentPropagationCodes.SourceTerrain),"explicit all/any reobserves two-cell footprint");
            c.That(retained.Observation.Eligible,"retained eligibility is detached, not a live predicate");
            c.Equal(Fix.FromInt(8),PhysicalLineRemovalTests.Steel(game),"terrain selection does not destroy network stock");
            twin=factory.Load(game.Save());long at=game.Clock.StepIndex;hash=game.Hash();game.Clock.Pause();twin.Clock.Pause();Step(game,twin);
            c.That(at==game.Clock.StepIndex&&hash==game.Hash(),"sole clock freezes pending receipt");game.Clock.Resume();twin.Clock.Resume();
            Reach(game,twin,()=>game.Clock.StepIndex>pending.DueTick,"saved delayed delivery coordinate");
            c.That(Incident(game,"binding.watcher").Snapshot.Active==any,"due-time selection distinguishes partial all/any, no cached initial terrain");
            if (!any)
            {
                c.Equal(0,Exposure(game).Pending.Count,"blocked delayed receipt removed without damage");
                var line=new LayLineCommand {Line="network.pipe"};line.Tiles.Add(new TileCoord(11,28));Order(c,game,line);
                var marker=game.Seat(0).Snapshot.Entities.Single(e=>e.Def=="sim.line-work.network.pipe").Id;
                Supply(c,game,marker,"steel");twin=factory.Load(game.Save());
                Reach(game,twin,()=>PhysicalLineRemovalTests.Tile(game,11)==1,"hauled material earns restored source layer");
                c.That(Eligibility(game).Observation.Eligible&&Amount(game,"depot","steel")==Fix.FromInt(49),"one actual reconstruction material, no instant eligible terrain");
                Reach(game,twin,()=>Exposure(game).Pending.Count==1,"fresh footprint receipt after reconstruction");
                var fresh=Exposure(game).Pending.Single();
                c.That(fresh.ScheduledTick>pending.DueTick&&fresh.DueTick-fresh.ScheduledTick==60,"restored layer earns fresh full delay");
                Reach(game,twin,()=>Incident(game,"binding.watcher").Snapshot.Active,"fresh eligible target episode");
            }
            c.That(Incident(game,"binding.watcher").Snapshot.Episode==1
                &&Incident(game,"terrain.source").Snapshot.Active&&!Incident(game,"terrain.source").Blocker.IsNone,"exact target episode; source response waits for actual water");
            Supply(c,game,Entity(game,"terrain.source"),"water");twin=factory.Load(game.Save());
            Reach(game,twin,()=>!Incident(game,"terrain.source").Snapshot.Active,"physical source response");
            Supply(c,game,Entity(game,"binding.watcher"),"water");twin=factory.Load(game.Save());
            Reach(game,twin,()=>!Incident(game,"binding.watcher").Snapshot.Active,"physical target response");
            c.That(Amount(game,"depot","water")==Fix.FromInt(8)&&Amount(game,"terrain.source","water")+Amount(game,"binding.watcher","water")+Amount(game,"truck","water")==Fix.Zero,"two real hauled response units consumed once");
            c.That(!Incident(game,"binding.remote").Snapshot.Active&&!Incident(game,"binding.source").Snapshot.Active,"unselected targets remain undamaged");
            c.Equal(IncidentPropagationCodes.SourceEnded,Eligibility(game).Observation.Blocker,"real repair stops original cause");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"completed physical selection and response checkpoint");
            c.Equal(isolatedHash,isolated.Hash(),"interleaved unrelated session unchanged");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"ordinary commands/layer work/delayed episodes and supplied repair replay");
            return game.Clock.StepIndex;
        }
        private static void Selection(Check c)
        {
            foreach (string mode in new[] { "all-target","same-cell","empty-all" })
            {
                var factory=Factory(new InitializeProbe(),out _,true,edit:doc=>{
                    var rule=Rule(doc,true);
                    if(mode=="all-target")rule.Get("target_tiles").Set("match",JsonValue.Of("all"));
                    if(mode=="empty-all")rule.Get("source_tiles").Set("conditions",Json.Parse("[]"));
                    if(mode=="same-cell") {
                        doc.Get("spatial_layers").Add(Json.Parse("{\"id\":\"terrain.other-mask\",\"space\":\"recovery.tiles\",\"type\":\"terrain.exposure\",\"value_type\":\"byte\",\"visibility\":\"public\",\"unit\":\"unit.classification\"}"));
                        doc.Get("independent_terrain_conditions").Add(Json.Parse("{\"id\":\"terrain.other\",\"space\":\"recovery.tiles\",\"layer\":\"terrain.other-mask\",\"minimum\":1,\"maximum\":2}"));
                        doc.Get("tile_layer_setups").Add(Json.Parse("{\"id\":\"terrain.other.start\",\"after\":[\"terrain.receiver.start\"],\"layer\":\"terrain.other-mask\",\"x\":14,\"y\":28,\"width\":1,\"height\":1,\"value\":1,\"owner\":-1}"));
                        rule.Get("target_tiles").Get("conditions").Add(JsonValue.Of("terrain.other"));
                    }
                },editBindings:root=>{if(mode=="same-cell")root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("terrain.other.start"));});
                var game=New(factory);Raise(c,game);var value=Eligibility(game);ulong hash=game.Hash();
                c.That(value.Observation.Eligible==(mode=="empty-all"),"whole-cell conjunction and explicit empty-all semantics "+mode);
                if(mode!="empty-all")c.Equal(IncidentPropagationCodes.TargetTerrain,value.Observation.Blocker,"typed target footprint blocker "+mode);
                c.Equal(hash,game.Hash(),"selection facts pure "+mode);
            }
        }
        private static void Invalid(Check c)
        {
            foreach(bool direct in new[]{false,true})
                foreach(string fault in new[]{"source","target","match","conditions","any-empty","duplicate","unknown","foreign-space","radius","space","extra"})
                {
                    var initialize=new InitializeProbe();
                    c.Throws<ContentException>(()=>New(Factory(initialize,out _,direct,edit:doc=>{
                        var row=Rule(doc,direct).Clone();row.Set("id",JsonValue.Of("terrain.unused"));
                        doc.Get(direct?"independent_tile_propagation_rules":"tile_incident_propagation_rules").Add(row);
                        if(fault=="source")row.Remove("source_tiles");if(fault=="target")row.Remove("target_tiles");
                        if(fault=="match")row.Get("source_tiles").Set("match",JsonValue.Of("implicit"));
                        if(fault=="conditions")row.Get("source_tiles").Remove("conditions");
                        if(fault=="any-empty"){row.Get("source_tiles").Set("match",JsonValue.Of("any"));row.Get("source_tiles").Set("conditions",Json.Parse("[]"));}
                        if(fault=="duplicate")row.Get("source_tiles").Get("conditions").Add(JsonValue.Of("terrain.connected"));
                        if(fault=="unknown")row.Get("source_tiles").Get("conditions").Add(JsonValue.Of("missing"));
                        if(fault=="foreign-space"){doc.Set("tile_spaces",Json.Parse("[{\"id\":\"terrain.other-space\",\"width\":48,\"height\":32}]"));doc.Get(direct?"independent_terrain_conditions":"tile_layer_band_conditions")[0].Set("space",JsonValue.Of("terrain.other-space"));}
                        if(fault=="radius")row.Set("radius",JsonValue.Number("-1"));if(fault=="space")row.Set("space",JsonValue.Of("missing"));
                        if(fault=="extra")row.Get("source_tiles").Set("host_sample",JsonValue.Number("1"));
                    })),"invalid unused native/direct tile rule "+direct+"/"+fault);
                    c.Equal(0,initialize.Initializations,"malformed loaded authoring never initializes "+direct+"/"+fault);
                }
        }
        private static void Faults(Check c)
        {
            foreach(string fault in new[]{"throw","pause","layer","touch"})
            {
                var factory=Factory(new InitializeProbe(),out var proof,true);var game=New(factory);Raise(c,game);
                var view=View(game);var source=Entity(game,"terrain.source");var target=Entity(game,"binding.watcher");
                proof.Arm(fault);
                c.Throws<InvalidOperationException>(()=>view.ReadEligibility(source,Spread,target,out _),"selected original predicate observation refuses "+fault);
                c.That(game.Fault!=null,"actual eligibility callback latches first fault "+fault);
                if(fault=="throw")c.That(game.Fault.Message.Contains("proof.terrain.predicate"),"first callback message preserved");
                c.Throws<InvalidOperationException>(()=>game.Save(),"faulted observation cannot save "+fault);
                c.Throws<InvalidOperationException>(()=>game.Hash(),"faulted observation cannot hash "+fault);
                c.Throws<InvalidOperationException>(()=>view.ReadEligibility(source,Spread,EntityId.None,out _),"faulted query refuses even missing target "+fault);
            }
        }
    }
}
