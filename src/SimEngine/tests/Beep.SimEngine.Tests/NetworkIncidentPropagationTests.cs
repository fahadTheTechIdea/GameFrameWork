using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Master-listed declarations, ordinary player input and immutable observations only.
    internal static class NetworkIncidentPropagationTests
    {
        internal const string Case = "Network incidents: connected topology and isolation govern delayed saved exposures";
        private static readonly Code Leak = new Code("leak"), Spread = new Code("spread"), Valve = new Code("valve");
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Create(InitializeProbe initialize, bool independent = false,
            Action<JsonValue> edit = null, bool missingProvider = false, Action<JsonValue> editBindings = null,
            IReadOnlyList<GameModule> additionalModules = null, string additionalDeclarations = null,
            IReadOnlyDictionary<string,string> additionalDocuments = null)
        {
            var content = Json.Parse(FrameworkProofContent.Read("network-incidents.json"));
            if (independent) {
                content.Set("independent_network_incident_rules", content.Get("network_incident_propagation_rules"));
                content.Remove("network_incident_propagation_rules");
            }
            edit?.Invoke(content);
            var modules = new List<GameModule> { new IncidentResponseModule(), new IncidentPropagationModule(), new NetworkIncidentProofModule(missingProvider ? initialize : null) };
            if (additionalModules != null) modules.AddRange(additionalModules);
            var declarations = Json.Parse("[{\"id\":\"sim.incidents\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"sim.incident-propagation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.incidents\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"proof.network-incidents\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.incident-propagation\",\"version\":\"1.0.0\"}],\"documents\":[\"network-incidents.json\"]}]");
            if (additionalDeclarations != null) { var entries = Json.Parse(additionalDeclarations); for (int i=0;i<entries.Count;i++) declarations.Add(entries[i]); }
            var documents = new Dictionary<string,string> { { "network-incidents.json", content.ToString() } };
            if (additionalDocuments != null) foreach (var pair in additionalDocuments) documents.Add(pair.Key,pair.Value);
            return NetworkSignalBindingTests.Create(initialize, out _, independent: false, edit: root => {
                foreach (var index in new[] { 0, 1, 2 }) {
                    var parts = root.Get("entities")[index].Get("components");
                    parts.Set("incident", Json.Parse("[{\"id\":\"leak\",\"workflow\":\"network.fire\",\"states\":{\"active\":\"stopped\"}},{\"id\":\"valve\",\"workflow\":\"network.valve\",\"states\":{\"active\":\"stopped\"}}]"));
                    parts.Set("incident_signal", Json.Parse("{\"id\":\"valve-state\",\"source\":\"valve\",\"workflow\":\"network.valve\",\"phase\":\"hold\"}"));
                    parts.Set("signal_network_gate", Json.Parse("{\"id\":\"isolation\",\"networks\":[\"binding.main\"],\"conditions\":[{\"id\":\"open\",\"source\":\"valve-state\",\"signal\":\"network.valve-active\",\"expected\":false,\"blocker\":\"network.incident.isolated\"}]}"));
                }
                root.Get("entities")[0].Get("components").Set("incident_propagation", Json.Parse("{\"id\":\"spread\",\"source\":\"leak\",\"rule\":\"network.fire-spread\"}"));
                root.Get("tile_layer_setups").Add(Json.Parse("{\"id\":\"remote.geometry\",\"after\":[\"binding.owner\"],\"layer\":\"network.line\",\"x\":19,\"y\":28,\"width\":1,\"height\":1,\"value\":1,\"owner\":-1}"));
                root.Get("tile_layer_setups").Add(Json.Parse("{\"id\":\"remote.owner\",\"after\":[\"remote.geometry\"],\"layer\":\"network.owner\",\"x\":19,\"y\":28,\"width\":1,\"height\":1,\"value\":1,\"owner\":0}"));
                root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("remote.geometry"));
                root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("remote.owner"));
                editBindings?.Invoke(root);
            }, extraModules: modules, extraDeclarations: declarations.ToString(), extraDocuments: documents,
            editScenario: scenario => {
                scenario.Get("settings").Set(IncidentPropagationModule.ModuleId, Json.Parse("{\"budget\":64}"));
                if (missingProvider)
                    scenario.Set("modules", Json.Parse("[\"sim.access\",\"sim.inventory\",\"sim.operations\",\"sim.motion\",\"sim.tile-navigation\",\"sim.work\",\"sim.world-setup\",\"sim.incidents\",\"sim.incident-propagation\",\"proof.network-incidents\"]"));
            }, editMaster: master => {
                if (!missingProvider) return;
                // Explicit alternative document ownership: no module with an infrastructure dependency is selected.
                JsonValue original = null, selected = null;
                for (int i=0;i<master.Get("modules").Count;i++) {
                    var module=master.Get("modules")[i];
                    if (module.Get("id").Text=="proof.network-signal-bindings") original=module;
                    if (module.Get("id").Text=="proof.network-incidents") selected=module;
                }
                for (int i=0;i<original.Get("documents").Count;i++) selected.Get("documents").Add(original.Get("documents")[i]);
                original.Set("documents",Json.Parse("[]"));
            });
        }
        internal static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "colony", Seed = 1401 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        internal static InfrastructureView Network(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<InfrastructureView>();
        internal static NetworkConnectionSnapshot Connection(SimGame game, string id)
        {
            Code refusal = Network(game).ReadConnection(Entity(game,id), "binding.main", out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("connection " + id + "/" + refusal);
            return facts;
        }
        internal static IncidentLifecycleView Incident(SimGame game, string id, Code source)
        {
            if (!game.Seat(0).View.Feature<IncidentsView>().TryGetLifecycle(Entity(game,id),source,out var facts))
                throw new InvalidOperationException("missing incident " + id + "/" + source);
            return facts;
        }
        internal static IncidentPropagationSnapshot Exposure(SimGame game)
        {
            if (!game.Seat(0).View.Feature<IncidentPropagationView>().TryRead(Entity(game,"binding.source"),Spread,out var facts))
                throw new InvalidOperationException("missing authored emitter facts");
            return facts;
        }
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i=0;i<32&&!ready();i++) Step(game,twin);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex +
                " pending=" + Exposure(game).Pending.Count + " blocker=" + Exposure(game).Blocker +
                " target=" + Incident(game,"binding.watcher",Leak).Snapshot.Active + "/" +
                Incident(game,"binding.watcher",Leak).Blocker);
        }
        private static void Raise(Check c, SimGame game, string entity, Code source, string kind)
            => Order(c,game,new RaiseIncidentCommand {Target=Entity(game,entity),Source=source,Kind=new Code(kind)});
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current network propagation protocol");
            c.Equal(154,EngineInfo.SaveFormat,"original topology/exposure/work/incident codecs");
            c.That(typeof(IndependentNetworkPropagationRuleDef).BaseType==typeof(Def),"direct rule has no native mandatory base");
            foreach (bool independent in new[] {false,true}) {
                Physical(c,independent);
                Isolation(c,independent,"binding.source"); Isolation(c,independent,"binding.watcher");
            }
            Invalid(c);
        }
        private static void Physical(Check c,bool independent)
        {
            var initialize=new InitializeProbe();var factory=Create(initialize,independent);var game=New(factory);var isolated=New(factory);
            ulong isolatedHash=isolated.Hash(); var recorder=game.RecordReplay(new ReplayLimits(4*1024*1024,1000));
            c.Equal(2,initialize.Initializations,"each valid session initialized once");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId),"hazards/work/networks require no business");
            var source=Connection(game,"binding.source");var target=Connection(game,"binding.watcher");var remote=Connection(game,"binding.remote");
            c.That(source.Endpoint==target.Endpoint&&source.Endpoint!=remote.Endpoint,"same actual pool, remote has distinct settled topology");
            ulong hash=game.Hash();Connection(game,"binding.source");Connection(game,"binding.remote");
            c.Equal(hash,game.Hash(),"connection identity reads pure");
            c.Equal(Codes.NotFound,Network(game,1).ReadConnection(Entity(game,"binding.source"),"binding.main",out var foreign),"foreign connection facts remain private");
            c.That(!foreign.Endpoint.IsValid,"refusal publishes no pool identity");
            Raise(c,game,"binding.source",Leak,"network.fire");
            Reach(game,null,()=>Exposure(game).Pending.Count==1,"schedule connected endpoint");
            var pending=Exposure(game).Pending.Single();
            c.Equal(Entity(game,"binding.watcher"),pending.Target,"only same component target selected");
            c.That(!Incident(game,"binding.watcher",Leak).Snapshot.Active&&pending.DueTick>game.Clock.StepIndex,"exposure does not raise immediately");
            var retained=Exposure(game);var twin=factory.Load(game.Save());
            game.Clock.Pause();twin.Clock.Pause();hash=game.Hash();long at=game.Clock.StepIndex;Step(game,twin);
            c.That(at==game.Clock.StepIndex&&hash==game.Hash(),"paused sole clock freezes delayed exposures");
            game.Clock.Resume();twin.Clock.Resume();
            Reach(game,twin,()=>Incident(game,"binding.watcher",Leak).Snapshot.Active,"due exposure");
            var raised=Incident(game,"binding.watcher",Leak);
            c.That(raised.Snapshot.RaisedTick>=pending.DueTick&&raised.Snapshot.Episode==1,"delayed raise commits exactly one episode");
            for(int i=0;i<5;i++)Step(game,twin);
            c.Equal(1,Incident(game,"binding.watcher",Leak).Snapshot.Episode,"repeat scans never duplicate active episode");
            c.That(!Incident(game,"binding.remote",Leak).Snapshot.Active,"nearby same-network different pool cannot spread");
            c.That(Incident(game,"binding.source",Leak).Snapshot.Active&&Incident(game,"binding.source",Leak).OrderId>0,"repair waits for ordinary physical machine, no instant response");
            c.Equal(1,retained.Pending.Count,"retained immutable exposure facts");
            c.Throws<NotSupportedException>(()=>((IList<PendingExposureSnapshot>)retained.Pending).Clear(),"pending facts immutable");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"completed wave checkpoint");
            c.Equal(Code.None,game.Seat(0).View.Feature<OperationsView>().ReadSignal(Entity(game,"binding.source"),new Code("pool"),"binding.stored",out var quantity),"quantity uses same original connection admission");
            var facets=new List<Facet>();game.Seat(0).View.Inspect(Entity(game,"binding.source"),facets);
            Fix local=facets.Where(f=>f.Key==Storage.FacetStock&&f.Subject=="steel").Aggregate(Fix.Zero,(sum,f)=>sum+f.Value);
            c.That(quantity.Available&&quantity.Value.Value+local==Fix.FromInt(30),"hazard scans conserve original source/pool stock");
            c.Equal(isolatedHash,isolated.Hash(),"interleaved idle session isolated");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),new ReplayLimits(4*1024*1024,1000)).Success,"ordinary commands and delayed wave replay");
        }
        private static void Isolation(Check c,bool independent,string closed)
        {
            var factory=Create(new InitializeProbe(),independent);var game=New(factory);
            Raise(c,game,"binding.source",Leak,"network.fire");
            Reach(game,null,()=>Exposure(game).Pending.Count==1,"scheduled before isolation");
            long due=Exposure(game).Pending.Single().DueTick;
            Raise(c,game,closed,Valve,"network.valve");
            c.That(game.Clock.StepIndex<due,"player isolates before queued exposure becomes due");
            c.Equal(Code.None,Network(game).ReadGates(Entity(game,closed),"binding.main",out var gate),"ordinary gate diagnostic");
            c.Equal(new Code("network.incident.isolated"),gate.Blocker,"actual endpoint isolation controls network");
            var twin=factory.Load(game.Save());
            for(int i=0;i<10;i++)Step(game,twin);
            c.That(!Incident(game,"binding.watcher",Leak).Snapshot.Active,"due-time recheck honors "+closed+" isolation");
            c.Equal(0,Exposure(game).Pending.Count,"blocked exposures removed, not delivered stale");
            c.That(Incident(game,closed,Valve).Snapshot.Active,"physical valve recovery cannot silently reopen isolation");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"isolated pending wave restores");
        }
        private static void Invalid(Check c)
        {
            foreach(bool independent in new[]{false,true})
                foreach(string fault in new[]{"missing-network","empty-network","unknown-network","zero-delay","extra"})
                {
                    var initialize=new InitializeProbe();
                    c.Throws<ContentException>(()=>New(Create(initialize,independent,edit:document=>{
                        var rows=document.Get(independent?"independent_network_incident_rules":"network_incident_propagation_rules");
                        var row=rows[0].Clone();row.Set("id",JsonValue.Of("network.unused-spread"));rows.Add(row);
                        if(fault=="missing-network")row.Remove("network");
                        if(fault=="empty-network")row.Set("network",JsonValue.Of(""));
                        if(fault=="unknown-network")row.Set("network",JsonValue.Of("absent.network"));
                        if(fault=="zero-delay")row.Get("delay").Set("numerator",JsonValue.Number("0"));
                        if(fault=="extra")row.Set("instant_spread",JsonValue.Of(true));
                    })),"strict network propagation content "+independent+"/"+fault);
                    c.Equal(0,initialize.Initializations,"invalid rule before Initialize");
                }
            var missing=new InitializeProbe();
            ContentException error=null;
            try { New(Create(missing,missingProvider:true)); }
            catch(ContentException caught) { error=caught; }
            c.That(error!=null&&error.Message.Contains("requires active provider 'sim.infrastructure'"),"native topology content requires actual selected provider: "+error?.Message);
            c.Equal(0,missing.Initializations,"missing provider fails before selected Initialize probe");
            var unconnected=new InitializeProbe();
            c.Throws<ContentException>(()=>New(Create(unconnected,editBindings:root=>{
                var parts=root.Get("entities")[0].Get("components");
                parts.Remove("ports");parts.Remove("network_signal");parts.Remove("signal_network_gate");
            })),"emitter requires its actual authored source connection");
            c.Equal(0,unconnected.Initializations,"unconnected source rejected before Initialize");
        }
    }
}
