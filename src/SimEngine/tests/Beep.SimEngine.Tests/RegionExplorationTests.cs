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

namespace Beep.SimEngine.Tests
{
    internal static class RegionExplorationTests
    {
        internal const string Case = "Region exploration: authored requirements earn whole remote shapes with private saved dwell";
        private static readonly Code Source = new Code("binding.contact"), Sight = new Code("sensor.sight");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool independent, bool half, Action<JsonValue> edit = null)
        {
            var version = new ModuleVersion(1, 0, 0);
            return ExplorationBindingTests.Factory(new ExplorationBindingsModule(), independent,
                edit: doc => {
                    string profiles = independent ? "independent_contact_profiles" : "exploration_profiles";
                    foreach (var row in Rows(doc.Get(profiles)[0].Get("entries")))
                        row.Set("requirement", JsonValue.Of("exploration.sensor-region"));
                    edit?.Invoke(doc);
                },
                sensorEdit: doc => doc.Get("entities")[0].Get("components").Get("point_sensor").Get("operation").Set("target", JsonValue.Number("5")),
                extraModules: new GameModule[] { new ProbeModule("proof.region-exploration", version,
                    new[] { new ModuleRequirement(ExplorationModule.ModuleId, version) }) },
                extraDeclarations: new[] { Json.Parse("{\"id\":\"proof.region-exploration\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.exploration\",\"version\":\"1.0.0\"}],\"documents\":[\"region-exploration.json\"]}") },
                extraFiles: new Dictionary<string,string> { ["region-exploration.json"] = FrameworkProofContent.Read("region-exploration.json") },
                definitionEdit: doc => { if (half) doc.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30")); });
        }
        private static IEnumerable<JsonValue> Rows(JsonValue array)
        { for (int i = 0; i < array.Count; i++) yield return array[i]; }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "points", Seed = 42457, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Find(SimGame game, string definition) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == definition).Id;
        private static ExplorationSnapshot Read(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<ExplorationView>().Read(Find(game, "sensor.machine"), Source, out var value);
            if (!code.IsNone) throw new InvalidOperationException("region exploration query: " + code); return value;
        }
        private static bool Known(SimGame game, string branch, int seat = 0) => game.Seat(seat).View.Feature<KnowledgeView>()
            .TryRead(new PlayerId(seat), "sim.knowledge.facts", "binding." + branch + "-known", out _);
        private static void Step(SimGame game, SimGame twin = null)
        { PermanentRemovalTests.Step(game, twin); }
        private static void Until(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 480 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " entries=" +
                string.Join(";", Read(game).Entries.Select(e => e.Id + "/" + e.Progress + "/" + e.Blocker)));
        }
        private static CommandResult Order(Check c, SimGame game, Command command)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary remote exploration command preview");
            c.Equal(before, game.Hash(), "remote exploration preview is pure");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary remote exploration command accepted"); return result;
        }
        private static void Control(Check c, SimGame game, bool enabled, int range)
            => Order(c, game, new ConfigureOperationCommand { Entity = Find(game, "sensor.machine"), Operation = Sight, Enabled = enabled, Target = Fix.FromInt(range) });
        private static void Physical(Check c, bool independent, bool half)
        {
            var factory = Factory(independent, half); var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "remote discovery has no money or business requirement");
            var initial = Read(game); c.That(initial.Entries.All(e => e.Requirement == "exploration.sensor-region" && e.Blocker == ExplorationCodes.NoSensor),
                "authored whole-region requirement is reported and initially blocked");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<ExplorationView>().Read(Find(game,"sensor.machine"), Source, out var hidden), "foreign exploration details withheld");
            c.That(hidden == null, "foreign query has no entry prefix");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                Order(c, game, new SetWorkerHeldCommand { Worker = Find(game,"sensor.machine"), Held = true });
                game.Seat(0).View.TryFindResource("supply", out var supply);
                int route = checked((int)Order(c,game,new AddRouteCommand { From = Find(game,"source"), To = Find(game,"sensor.machine"), Resource = supply,
                    Settings = new LogisticsRouteSettings(true, Fix.One,70) }).Arg0);
                var twin = factory.Load(game.Save());
                Until(game,twin,()=>TransformationContentContractTests.Stock(game,"sensor.machine")==Fix.One,"physical remote sensor supply");
                c.Equal(game.Hash(),twin.Hash(),"physical delivery and requirement progress share original checkpoint");
                Order(c,game,new RemoveRouteCommand { Route=route });
                Until(game,null,()=>PermanentRemovalTests.Jobs(game).All(j=>j.State==JobState.Done||j.State==JobState.Cancelled),"remote supplier departure");
                Order(c,game,new SetWorkerHeldCommand { Worker=Find(game,"sensor.machine"),Held=false });
                c.That(Read(game).Entries.All(e=>e.Progress.Equals(WorkProgress.Zero)&&e.Blocker==ExplorationCodes.NoSensor),
                    "range five sees region center but not full interval; no discovery progress");
                c.Equal(Fix.FromInt(5),((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(e=>e.Def=="sensor.machine").Spatial).Position,
                    "explorer remains physically outside the nine-to-eleven target interval");
                Control(c,game,true,6);
                twin=factory.Load(game.Save()); Until(game,twin,()=>Read(game).Entries[0].Progress.WholeTicks>=3,"partial whole-region dwell");
                var partial=Read(game); c.That(partial.Entries.All(e=>!e.Known&&e.Progress.WholeTicks>0),"parallel remote entries earn real unfinished time");
                c.Equal(new Code("binding.exploring"),game.Seat(0).Snapshot.Entities.Single(e=>e.Def=="sensor.machine").State,"entity Update settles authored sampling state");
                Control(c,game,false,6); long retained=Read(game).Entries[1].Progress.WholeTicks;
                c.That(Read(game).Entries[0].Progress.Equals(WorkProgress.Zero)&&retained>0,"shutdown obeys authored reset and retain interruption");
                twin=factory.Load(game.Save()); long time=game.Clock.StepIndex; game.Clock.Pause();twin.Clock.Pause();Step(game,twin);
                c.Equal(time,game.Clock.StepIndex,"one paused game clock earns no remote dwell");game.Clock.Resume();twin.Clock.Resume();
                Step(game,twin);c.Equal(retained,Read(game).Entries[1].Progress.WholeTicks,"saved shutdown earns no exposure");
                Control(c,game,true,6);twin=factory.Load(game.Save());
                Until(game,twin,()=>Read(game).Entries[0].Progress.WholeTicks==Read(game).Entries[0].RequiredSteps-1,"last undiscovered remote interval");
                c.That(!Known(game,"reset"),"whole shape cannot reveal before full authored duration");
                twin=factory.Load(game.Save());Until(game,twin,()=>Known(game,"reset")&&Known(game,"retain"),"remote discoveries");
                c.That(Read(game).Entries.All(e=>e.Known&&e.Progress.Equals(WorkProgress.Zero)),"sole knowledge ledger owns completed discovery");
                c.That(!Known(game,"reset",1),"earned remote knowledge stays private");
                c.Equal(game.Hash(),twin.Hash(),"saved partial-to-complete remote continuation identical");
                Order(c,game,new ShareKnowledgeCommand { Recipient=new PlayerId(1),Policy="binding.share" });
                c.That(Known(game,"reset",1)&&Known(game,"retain",1),"ordinary sharing transfers only earned facts");
                Control(c,game,false,6);Order(c,game,new ForgetKnowledgeCommand { Policy="binding.forget" });
                c.That(!Known(game,"reset")&&Known(game,"reset",1),"forgetting affects own ledger, no recipient mutation");
                Control(c,game,true,6);twin=factory.Load(game.Save());Until(game,twin,()=>Known(game,"reset")&&Known(game,"retain"),"remote rediscovery");
                c.Equal(Fix.One,TransformationContentContractTests.Stock(game,"sensor.machine"),"dwell invents no sensor supply");
                c.Equal(Fix.FromInt(19),TransformationContentContractTests.Stock(game,"source"),"exact one unit physically delivered");
                c.That(initial.Entries.All(e=>!e.Known&&e.Progress.Equals(WorkProgress.Zero)),"retained initial facts detached");
                c.Equal(isolated,idle.Hash(),"interleaved world borrows no remote dwell or knowledge");
                c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"public remote control supply share and forget replay identically");
            }
        }
        private static void Regions(Check c)
        {
            var game=LiveSensorTests.New(ContactExplorationTests.Factory());
            foreach(string id in new[]{"contact.north","contact.east"}) {
                var region=game.World.Content.Get<ISpatialRegionDefinition>(id);var shapes=((ISpatialObservationRegion)region).CaptureObservationFootprints();
                c.That(shapes.Count==(id=="contact.north"?1:2)&&shapes.All(s=>region.Check(s)==SpatialRegionStatus.Inside),"native full rectangle or exact painted cells captured");
                c.Throws<NotSupportedException>(()=>((IList<SpatialSnapshot>)shapes).Clear(),"region footprints detached and immutable");
                foreach(var shape in shapes)c.That(shape is TileRegionSnapshot&&shape.Matches(shape),"region geometry carries no fabricated entity state");
                if(id=="contact.north") {
                    var whole=new TileSensorCoverage(region.Space,new TileRect(8,10,1,1),1,string.Empty);
                    var centerOnly=new TileSensorCoverage(region.Space,new TileRect(8,10,1,1),0,string.Empty);
                    c.That(whole.Covers(shapes[0])&&!centerOnly.Covers(shapes[0]),"tile sensor coverage admits full rectangle, never center-only sight");
                }
            }
            var world=Json.Parse(FrameworkProofContent.Read("native-contact-exploration-regions.json"));
            var painted=world.Get("painted_tile_regions")[0].Get("tiles");painted[1].Set("y",JsonValue.Number("13"));
            game=LiveSensorTests.New(ContactExplorationTests.Factory(additionalDocuments:new Dictionary<string,string>{["native-contact-exploration-regions.json"]=world.ToString()}));
            var disjoint=game.World.Content.Get<ISpatialRegionDefinition>("contact.east");var cells=((ISpatialObservationRegion)disjoint).CaptureObservationFootprints();
            c.That(cells.Count==2&&((ITileFootprintSnapshot)cells[1]).Footprint.Y==13
                &&disjoint.Check(new TileRegionSnapshot(disjoint.Space,new TileRect(14,10,1,4)))==SpatialRegionStatus.Outside,
                "disconnected painted geometry is never substituted by bounding rectangle");
        }
        private static void Invalid(Check c)
        {
            foreach(bool independent in new[]{false,true})foreach(string mode in new[]{"missing","unknown","unsupported"})
                c.Throws<ContentException>(()=>New(Factory(independent,false,doc=>{
                    var entry=doc.Get(independent?"independent_contact_profiles":"exploration_profiles")[0].Get("entries")[0];
                    if(mode=="missing")entry.Remove("requirement");
                    if(mode=="unknown")entry.Set("requirement",JsonValue.Of("missing.requirement"));
                    if(mode=="unsupported") {
                        doc.Set("point_station_regions",Json.Parse("[{\"id\":\"binding.unsupported\",\"space\":\"field.points\",\"a\":9,\"b\":11}]"));
                        entry.Set("region",JsonValue.Of("binding.unsupported"));
                    }
                })),"missing unknown or unsupported requirement refuses before gameplay: "+mode);
        }
        private static void Run(Check c)
        {
            foreach(bool independent in new[]{false,true})foreach(bool half in new[]{false,true})Physical(c,independent,half);
            Regions(c);Invalid(c);
        }
    }
}
