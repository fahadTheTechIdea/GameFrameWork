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
    // Pure content authoring and public commands/clock/views. No normal gameplay decisions or writes.
    internal static class OwnerInvariantTests
    {
        internal const string Case = "Owner invariants: physical condition incident and bidirectional stock share authoritative state";
        private static readonly Code Integrity = new Code("integrity"), Reserve = new Code("reserve"), Leak = new Code("leak"), Valve = new Code("valve");
        private static readonly Code First = new Code("owner.first"), Second = new Code("owner.second");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(out OwnerObservationModule proof, bool direct, Action<JsonValue> edit = null)
        {
            proof = new OwnerObservationModule();
            return NetworkIncidentPropagationTests.Create(new InitializeProbe("probe.owner-base"), edit: content => {
                // No propagation in this ownership scenario; both original incident identities remain independent.
                content.Set("network_incident_propagation_rules", Json.Parse("[]"));
                var node = content.Get("activities")[0].Get("nodes")[0];
                node.Set("effect", JsonValue.Of("owner.repair")); node.Set("requirement", JsonValue.Of("owner.water"));
            }, editBindings: root => {
                for (int i = 0; i < root.Get("entities").Count; i++) {
                    var parts = root.Get("entities")[i].Get("components"); parts.Remove("incident_propagation");
                    parts.Set("inventory_port", Json.Parse("[{\"id\":\"owner.first\"},{\"id\":\"owner.second\"}]"));
                    parts.Set("equipment_condition", Json.Parse("[{\"id\":\"integrity\",\"unit\":\"unit.condition\",\"maximum\":10,\"initial\":10,\"wear\":null,\"states\":{\"damaged\":\"stopped\",\"failed\":\"stopped\"}},{\"id\":\"reserve\",\"unit\":\"unit.condition\",\"maximum\":20,\"initial\":20,\"wear\":null,\"states\":{\"damaged\":\"stopped\",\"failed\":\"stopped\"}}]"));
                    if (direct) {
                        parts.Set("independent_condition", parts.Get("equipment_condition")); parts.Remove("equipment_condition");
                        parts.Set("independent_storage", parts.Get("storage")); parts.Remove("storage");
                        parts.Set("owner_incident", parts.Get("incident")); parts.Remove("incident");
                    }
                }
                root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("removal.site.start"));
                root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("removal.crane.start"));
                edit?.Invoke(root);
            }, additionalModules: new GameModule[] {new EquipmentConditionModule(), proof, new PhysicalLineRemovalProofModule()},
            additionalDeclarations: "[{\"id\":\"sim.equipment-condition\",\"version\":\"1.0.0\",\"documents\":[]},{\"id\":\"proof.owner-observation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.equipment-condition\",\"version\":\"1.0.0\"},{\"id\":\"sim.incidents\",\"version\":\"1.0.0\"}],\"documents\":[\"owner-invariants.json\"]},{\"id\":\"proof.physical-line-removal\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"}],\"documents\":[\"physical-line-removal.json\"]}]",
            additionalDocuments: new Dictionary<string,string> {
                {"owner-invariants.json", FrameworkProofContent.Read("owner-invariants.json")},
                {"physical-line-removal.json", FrameworkProofContent.Read("physical-line-removal.json")} });
        }
        private static SimGame New(GameFactory factory) => NetworkIncidentPropagationTests.New(factory);
        private static EntityId Source(SimGame game) => Entity(game,"binding.source");
        private static EquipmentConditionView Conditions(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<EquipmentConditionView>();
        private static IncidentsView Incidents(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<IncidentsView>();
        private static ConditionSnapshot Condition(SimGame game, Code source)
        { if (!Conditions(game).Read(Source(game), source, out var facts).IsNone) throw new InvalidOperationException("missing actual condition"); return facts; }
        private static IncidentLifecycleView Incident(SimGame game, Code source)
        { if (!Incidents(game).TryGetLifecycle(Source(game),source,out var facts)) throw new InvalidOperationException("missing actual episode"); return facts; }
        private static Fix Stock(SimGame game, string name)
        { var rows = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game,name),rows); return rows.Where(f=>f.Key==Storage.FacetStock&&f.Subject=="water").Aggregate(Fix.Zero,(sum,f)=>sum+f.Value); }
        private static bool NoHaul(SimGame game)
        { var rows = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(rows); return !rows.Any(j=>j.IsHaul); }
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i=0;i<256&&!ready();i++) Step(game,twin);
            if (!ready()) throw new InvalidOperationException(stage+" step="+game.Clock.StepIndex+" condition="+Condition(game,Integrity).Value
                +" incident="+Incident(game,Leak).Snapshot.Active+"/"+Incident(game,Leak).Blocker+" water="+Stock(game,"depot")+"/"+Stock(game,"binding.source")+"/"+Stock(game,"truck"));
        }
        private static ResourcePortSnapshot Port(Check c, SimGame game, Code port)
        {
            game.Seat(0).View.TryFindResource("water",out var water);
            c.Equal(Code.None,game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(Source(game),port,water,out var facts),"actual named store");return facts;
        }
        private static void Input(Check c, SimGame game, Command command)
        {
            ulong before=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(command),"ordinary player preview");c.Equal(before,game.Hash(),"pure preview");
            game.Seat(0).Submit(command);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current view-boundary build"); c.Equal(154,EngineInfo.SaveFormat,"unchanged owner payloads");
            c.That(typeof(IResourceInlet).IsAssignableFrom(typeof(IResourceOutlet))&&typeof(IInletPort).IsAssignableFrom(typeof(IOutletPort)),"universal generic/entity outlet inherits inlet");
            c.That(typeof(IInletPort).IsAssignableFrom(typeof(ITransporter).GetProperty("Cargo").PropertyType),"cargo has both directions through same inventory contract");
            foreach(var assembly in new[]{typeof(IOutletPort).Assembly,typeof(OwnerObservationModule).Assembly})
                foreach(var type in assembly.GetTypes().Where(t=>typeof(IResourceOutlet).IsAssignableFrom(t)))
                    c.That(typeof(IResourceInlet).IsAssignableFrom(type),"compiled outlet invariant "+type.FullName);
            c.Equal(Physical(c,false),Physical(c,true),"native/direct metadata chronology identical");
            Invalid(c); Faults(c);
        }
        private static long Physical(Check c, bool direct)
        {
            var factory=Factory(out var proof,direct);var game=New(factory);var isolated=New(factory);ulong isolatedHash=isolated.Hash();
            c.That(proof.ActualAliases&&proof.Initialize.Initializations==2,"actual native owners and one Initialize per session");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId),"no business gameplay selected");
            var conditionIds=new List<Code>();var incidentIds=new List<Code>();
            c.That(Conditions(game).CopyIds(Source(game),conditionIds)&&conditionIds.SequenceEqual(new[]{Integrity,Reserve}),"two independent named condition owners");
            c.That(Incidents(game).CopyIncidentIds(Source(game),incidentIds)&&incidentIds.SequenceEqual(new[]{Leak,Valve}),"two independent named episode owners");
            var initial=Condition(game,Integrity);var first=Port(c,game,First);var second=Port(c,game,Second);
            c.That(first.Endpoint==ResourceEndpointId.ForEntity(Source(game))&&first.Endpoint==second.Endpoint&&first.Version==second.Version,"named inlet/outlets alias ONE actual inventory");
            var inventory=game.Seat(0).View.Feature<InventoryView>();
            foreach(string name in new[]{"depot","truck","binding.source"}) c.That(inventory.TryGetCapacity(Entity(game,name), Code.None,out _)&&inventory.TryGetReservations(Entity(game,name), Code.None,out var receipts)&&receipts.Count==0,"structure/cargo share actual receiving promises "+name);
            ulong before=game.Hash();Conditions(game).Read(Source(game),Integrity,out _);Incidents(game).TryGetIncident(Source(game),Leak,out _);
            var phases=new List<IncidentPhaseView>();c.That(Incidents(game).CopyPhases(Source(game),Leak,phases)&&phases.Count==1,"phase labels derive existing response graph");
            c.Equal(Codes.NotFound,Conditions(game,1).Read(Source(game),Integrity,out _),"foreign condition stays private");
            c.That(!Incidents(game,1).TryGetIncident(Source(game),Leak,out _)&&!Incidents(game,1).CopyIncidentIds(Source(game),incidentIds)&&incidentIds.Count==0,"foreign episodes stay private");
            c.Equal(before,game.Hash(),"complete owner views pure");
            var limits=new ReplayLimits(4*1024*1024,3000);var recorder=game.RecordReplay(limits);
            Input(c,game,new PostOrderCommand {Target=Source(game),Plan="owner.damage-work"});var twin=factory.Load(game.Save());
            Reach(game,twin,()=>Condition(game,Integrity).Value==Fix.FromInt(4),"physical twelve-minute damage");
            c.That(game.Clock.StepIndex>=12&&Condition(game,Reserve).Value==Fix.FromInt(20)&&!Incident(game,Leak).Snapshot.Active&&!Incident(game,Valve).Snapshot.Active,"condition change touches only original named scalar, no fabricated incident");
            game.Seat(0).View.TryFindResource("water",out var water);
            Input(c,game,new HaulResourcesCommand {Source=Entity(game,"depot"),Destination=Source(game),DestinationPort=First,Worker=Entity(game,"truck"),Resource=water,Ceiling=Fix.FromInt(2),Priority=100});
            twin=factory.Load(game.Save());Reach(game,twin,()=>Stock(game,"binding.source")==Fix.FromInt(2)&&Stock(game,"truck")==Fix.Zero&&NoHaul(game),"physical inlet delivery and departure");
            Input(c,game,new HaulResourcesCommand {Source=Source(game),SourcePort=Second,Destination=Entity(game,"depot"),Worker=Entity(game,"truck"),Resource=water,Ceiling=Fix.One,Priority=100});
            twin=factory.Load(game.Save());Reach(game,twin,()=>Stock(game,"depot")==Fix.FromInt(9)&&Stock(game,"binding.source")==Fix.One&&Stock(game,"truck")==Fix.Zero&&NoHaul(game),"same inventory exports physical return and departs");
            Input(c,game,new RaiseIncidentCommand {Target=Source(game),Source=Leak,Kind=new Code("network.fire")});Step(game);
            c.That(Incident(game,Leak).Snapshot.Active&&Incident(game,Leak).Snapshot.Episode==1&&!Incident(game,Valve).Snapshot.Active,"raise mutates only selected original episode owner");
            c.Equal(Fix.FromInt(4),Condition(game,Integrity).Value,"raise is not instant repair");
            twin=factory.Load(game.Save());long at=game.Clock.StepIndex;game.Clock.Pause();twin.Clock.Pause();before=game.Hash();Step(game,twin);
            c.That(game.Clock.StepIndex==at&&game.Hash()==before,"one clock pauses all three owners");game.Clock.Resume();twin.Clock.Resume();
            Reach(game,twin,()=>!Incident(game,Leak).Snapshot.Active,"supplied timed incident repair");
            c.That(game.Clock.StepIndex>=at+7&&Condition(game,Integrity).Value==Fix.FromInt(10)&&Condition(game,Reserve).Value==Fix.FromInt(20),"eight earned repair minutes restore original scalar only");
            c.That(Stock(game,"depot")==Fix.FromInt(9)&&Stock(game,"binding.source")==Fix.Zero&&Stock(game,"truck")==Fix.Zero,"one water consumed once through original stock owner");
            c.That(initial.Value==Fix.FromInt(10)&&first.Stock.AmountOf(water)==Fix.Zero,"retained facts detached");
            c.That(Port(c,game,First).Endpoint==Port(c,game,Second).Endpoint,"same outlet/inlet authority after damage and recovery");
            for(int i=0;i<4;i++) Step(game,twin);
            c.That(Condition(game,Integrity).Value==Fix.FromInt(10)&&Stock(game,"depot")==Fix.FromInt(9),"completion cannot repeat effect or consumption");
            foreach(string name in new[]{"depot","truck","binding.source"}) c.That(inventory.TryGetReservations(Entity(game,name), Code.None,out var receipts)&&receipts.Count==0,"terminal promises settle "+name);
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"all original owner codecs restore");c.Equal(isolatedHash,isolated.Hash(),"interleaved owners stay isolated");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"public command stream and sole clock replay");return game.Clock.StepIndex;
        }
        private static void Invalid(Check c)
        {
            foreach(string fault in new[]{"stock","condition","incident","cross-kind"}) {
                OwnerObservationModule proof=null;
                c.Throws<ContentException>(()=>New(Factory(out proof,true,root=>{
                    var parts=root.Get("entities")[0].Get("components");
                    if(fault=="stock")parts.Set("storage",parts.Get("independent_storage"));
                    if(fault=="condition")parts.Get("independent_condition")[1].Set("id",JsonValue.Of("integrity"));
                    if(fault=="incident")parts.Get("owner_incident")[1].Set("id",JsonValue.Of("leak"));
                    if(fault=="cross-kind")parts.Get("owner_incident")[1].Set("id",JsonValue.Of("reserve"));
                })),"duplicate authoritative binding refuses "+fault);
                c.That(proof!=null&&proof.Initialize.Initializations==0,"duplicate refuses before Initialize "+fault);
            }
        }
        private static void Faults(Check c)
        {
            foreach(string kind in new[]{"condition","incident"}) foreach(string fault in new[]{"throw","pause","neighbor"}) {
                var factory=Factory(out var proof,true);var game=New(factory);var values=new List<Code>{new Code("stale")};
                var conditions=Conditions(game);var incidents=Incidents(game);
                var foreignConditions=Conditions(game,1);var foreignIncidents=Incidents(game,1);
                proof.Arm(kind,kind=="condition"?"condition.identity":"identity",fault);
                c.Throws<InvalidOperationException>(()=>{if(kind=="condition")Conditions(game).CopyIds(Source(game),values);else Incidents(game).CopyIncidentIds(Source(game),values);},"actual late owner callback refuses "+kind+"/"+fault);
                c.That(game.Fault!=null&&values.Count==0,"first fault and no healthy prefix "+kind+"/"+fault);
                if(fault=="throw")c.That(game.Fault.Message.Contains("proof.owner.original"),"original callback exception retained");
                c.Throws<InvalidOperationException>(()=>game.Save(),"faulted owners cannot save");
                var missing=new EntityId(int.MaxValue);
                c.Throws<InvalidOperationException>(()=>conditions.Read(missing,Code.None,out _),"faulted missing condition");
                c.Throws<InvalidOperationException>(()=>conditions.ReadWear(missing,Code.None,out _),"faulted missing wear");
                c.Throws<InvalidOperationException>(()=>conditions.ReadPolicy(missing,Code.None,out _),"faulted missing policy");
                c.Throws<InvalidOperationException>(()=>conditions.CopyIds(missing,new List<Code>()),"faulted empty condition ids");
                c.Throws<InvalidOperationException>(()=>conditions.CopyPolicyIds(missing,new List<Code>()),"faulted empty policy ids");
                c.Throws<InvalidOperationException>(()=>conditions.PreviewPolicy(missing,Code.None,default),"faulted missing policy preview");
                c.Throws<InvalidOperationException>(()=>foreignConditions.Read(Source(game),Integrity,out _),"faulted private condition");
                c.Throws<InvalidOperationException>(()=>incidents.TryGetIncident(missing,Code.None,out _),"faulted missing incident");
                c.Throws<InvalidOperationException>(()=>incidents.TryGetResponseOrder(missing,Code.None,out _),"faulted missing response");
                c.Throws<InvalidOperationException>(()=>incidents.TryGetLifecycle(missing,Code.None,out _),"faulted missing lifecycle");
                c.Throws<InvalidOperationException>(()=>incidents.CopyIncidentIds(missing,new List<Code>()),"faulted empty incident ids");
                c.Throws<InvalidOperationException>(()=>incidents.CopyPhases(missing,Code.None,new List<IncidentPhaseView>()),"faulted empty phases");
                c.Throws<InvalidOperationException>(()=>incidents.ReadPreparedness(missing,Code.None,out _),"faulted missing preparedness");
                c.Throws<InvalidOperationException>(()=>foreignIncidents.TryGetIncident(Source(game),Leak,out _),"faulted private incident");
            }
        }
    }
}
