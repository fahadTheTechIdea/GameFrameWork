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
    internal static class IndependentTileIncidentTests
    {
        private static readonly Code Leak = new Code("leak"), Spread = new Code("spread");
        internal const string Case = "Independent tile incidents: delayed footprint exposure and physically supplied response";
        internal const string NativeCase = "Native tile incidents: delayed footprint exposure and physically supplied response";
        internal const string AdmissionCase = "Tile incident geometry: actual layer admission and all-any footprint selection";
        internal static void Register()
        {
            TestRunner.Add(Case, c => Physical(c, false));
            TestRunner.Add(NativeCase, c => Physical(c, true));
            TestRunner.Add(AdmissionCase, Admission);
        }
        private static GameFactory Factory(bool native, Action<JsonValue> author = null)
            => IndependentTileWorkTests.Factory(nativeTiles: native, incidents: true, authorIncidents: author);
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "independent-sensor", Seed = 723, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Id(SimGame game, string name) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == name).Id;
        private static IncidentLifecycleView Incident(SimGame game, EntityId target)
        {
            if (!game.Seat(0).View.Feature<IncidentsView>().TryGetLifecycle(target, Leak, out var result)) throw new InvalidOperationException("missing actual incident " + target);
            return result;
        }
        private static IncidentPropagationSnapshot Exposure(SimGame game)
        {
            if (!game.Seat(0).View.Feature<IncidentPropagationView>().TryRead(Id(game, "sensor.incident-source"), Spread, out var result))
                throw new InvalidOperationException("missing actual exposure");
            return result;
        }
        private static IncidentEligibilitySnapshot Eligibility(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<IncidentPropagationView>().ReadEligibility(Id(game, "sensor.incident-source"), Spread, Id(game, "sensor.worksite"), out var result);
            if (!code.IsNone) throw new InvalidOperationException("eligibility refused " + code);
            return result;
        }
        private static void Update(Check c, SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("incident fault " + game.Fault + "/" + twin?.Fault);
            if (twin != null) c.Equal(game.Hash(), twin.Hash(), "original exposure/episode/work/material/motion saves continue identically");
        }
        private static void Until(Check c, SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 240 && !ready(); i++) Update(c, game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " clock=" + game.Clock.StepIndex + " source=" +
                Incident(game, Id(game, "sensor.incident-source")).Blocker + " target=" + Incident(game, Id(game, "sensor.worksite")).Blocker +
                " eligibility=" + Eligibility(game).Observation.Blocker + " pending=" + Exposure(game).Pending.Count);
        }
        private static void Raise(Check c, SimGame game)
        {
            var command = new RaiseIncidentCommand { Target = Id(game, "sensor.incident-source"), Source = Leak, Kind = new Code("sensor.fire") };
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary engine incident preview");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary engine incident command, no driver damage write");
        }
        private static Fix Stock(SimGame game)
        {
            Fix total = Fix.Zero; var facts = new List<Facet>();
            foreach (var entity in game.Seat(0).Snapshot.Entities) {
                game.Seat(0).View.Inspect(entity.Id, facts);
                foreach (var fact in facts) if (fact.Key == Storage.FacetStock && fact.Subject == "steel") total += fact.Value;
            }
            return total;
        }
        private static void SupplyAndRepair(Check c, GameFactory factory, SimGame game, ref SimGame twin, EntityId target)
        {
            c.That(game.Seat(0).View.TryFindResource("steel", out var steel), "actual engine resource identity available");
            var command = new HaulResourcesCommand { Source = Id(game, "sensor.depot"), Destination = target,
                Worker = Id(game, "sensor.cart"), Resource = steel, Ceiling = Fix.One, Priority = 100 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal physical response supply preview");
            var accepted = TestKit.Order(game, 0, command); c.That(accepted.Accepted, "normal hauling command: " + accepted.Refusal);
            twin = factory.Load(game.Save()); bool loadedSaved = false, partialSaved = false; int updates = 0;
            var jobs = new List<WorkJobView>(); var nodes = new List<ActivityNodeView>();
            while (Incident(game, target).Snapshot.Active && updates++ < 240)
            {
                Update(c, game, twin);
                game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                if (!loadedSaved && jobs.Any(j => j.IsHaul && j.Loaded.IsPositive)) {
                    loadedSaved = true; twin = factory.Load(game.Save());
                    c.Equal(game.Hash(), twin.Hash(), "real loaded response cargo restores during travel");
                }
                var response = Incident(game, target);
                if (response.OrderId > 0 && game.Seat(0).View.Feature<WorkView>().TryCopyNodes(response.OrderId, nodes) &&
                    nodes.Any(n => n.Progress.WholeTicks > 0 && n.Progress.WholeTicks < n.RequiredTicks) && !partialSaved) {
                    partialSaved = true; twin = factory.Load(game.Save());
                    c.Equal(game.Hash(), twin.Hash(), "actual supplied partial repair restores without instant restart");
                    game.Clock.Pause(); twin.Clock.Pause(); long time = game.Clock.StepIndex; ulong hash = game.Hash();
                    Update(c, game, twin); c.Equal(time, game.Clock.StepIndex, "one paused clock freezes incident response and motion");
                    c.Equal(hash, game.Hash(), "paused response has no stock, damage or progress write"); game.Clock.Resume(); twin.Clock.Resume();
                }
            }
            if (Incident(game, target).Snapshot.Active) throw new InvalidOperationException("physical response stalled at " + game.Clock.StepIndex + "/" + Incident(game, target).Blocker);
            c.That(loadedSaved && partialSaved && updates < 240, "response requires physically transported material and earned machine work");
            c.Equal(1, Incident(game, target).Snapshot.Episode, "single supplied response closes one actual episode");
        }
        private static void Physical(Check c, bool native)
        {
            var factory = Factory(native); var game = New(factory); var fresh = New(factory); ulong isolated = fresh.Hash();
            c.That(native ? game.World.Space is TileMap : game.World.Space is SensorTileSpace && !game.World.Services.Has<TileMap>(), "JSON selects actual native/independent tile geometry");
            c.That(!game.World.Services.Has<IEconomy>(), "incident response requires resources, not business");
            var source = Id(game, "sensor.incident-source"); var target = Id(game, "sensor.worksite");
            c.Equal(Fix.FromInt(12), Stock(game), "authored finite response materials only");
            c.Equal(IncidentPropagationCodes.Inactive, Eligibility(game).Observation.Blocker, "inactive emitter exposes no target");
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = game.RecordReplay(limits);
            Raise(c, game); Until(c, game, null, () => Exposure(game).Pending.Count == 1, "scheduled actual footprint exposure");
            var pending = Exposure(game).Pending.Single(); var retained = Eligibility(game); ulong hash = game.Hash();
            c.That(pending.Target == target && pending.DueTick - pending.ScheduledTick == 4, "same-company partial target mask selects exactly one four-minute exposure");
            c.That(retained.Observation.Eligible && retained.AuthoredDelaySteps == 4, "engine exposes authored eligibility and timing, not host rules");
            c.Equal(hash, game.Hash(), "eligibility and exposure observations are pure");
            c.That(!Incident(game, target).Snapshot.Active, "scheduled exposure is not instant target damage");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<IncidentPropagationView>().ReadEligibility(source, Spread, target, out var hidden), "foreign seat cannot inspect private incident eligibility");
            c.That(hidden == null, "foreign refusal publishes no geometry or episode prefix");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending delayed exposure saves through original owner");
            Until(c, game, twin, () => Incident(game, target).Snapshot.Active, "actual delayed target episode");
            c.That(game.Clock.StepIndex >= pending.DueTick, "target episode begins only after actual exposure delay");
            c.Equal(1, Incident(game, target).Snapshot.Episode, "one target exposure creates exactly one episode");
            c.Equal(Fix.FromInt(12), Stock(game), "unsupplied incidents cannot consume or invent response materials");
            SupplyAndRepair(c, factory, game, ref twin, source); SupplyAndRepair(c, factory, game, ref twin, target);
            c.Equal(Fix.FromInt(10), Stock(game), "two supplied repairs consume exactly two original materials");
            c.Equal(IncidentPropagationCodes.SourceEnded, Eligibility(game).Observation.Blocker, "physically resolved source no longer propagates");
            c.That(retained.Observation.Eligible && retained.AuthoredDelaySteps == 4, "retained detached eligibility remains unchanged");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "finished physical repairs and exposure ledger restore");
            c.Equal(isolated, fresh.Hash(), "interleaved session receives no incidents, cargo or damage");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(replay.Success && replay.Game?.Hash() == game.Hash(), "ordinary incident/haul/repair command replay: " + replay.Failure + "/" + replay.Detail);
        }
        private static void Admission(Check c)
        {
            foreach (bool native in new[] { false, true }) {
                foreach (Action<JsonValue> invalid in new Action<JsonValue>[] {
                    json => json.Get("tile_incident_propagation_rules")[0].Set("radius", JsonValue.Number("-1")),
                    json => json.Get("tile_incident_propagation_rules")[0].Set("space", JsonValue.Of("absent.space")),
                    json => json.Get("tile_layer_band_conditions")[0].Set("layer", JsonValue.Of("absent.layer")),
                    json => json.Get("tile_incident_propagation_rules")[0].Get("target_tiles").Set("conditions", Json.Parse("[]"))
                }) c.Throws<ContentException>(() => New(Factory(native, invalid)), "invalid actual content refuses before simulation");
                var all = New(Factory(native, json => json.Get("tile_incident_propagation_rules")[0].Get("target_tiles").Set("match", JsonValue.Of("all"))));
                Raise(c, all); for (int i = 0; i < 6; i++) Update(c, all);
                c.Equal(IncidentPropagationCodes.TargetTerrain, Eligibility(all).Observation.Blocker, "all requires every target footprint tile, not one masked tile");
                c.Equal(0, Exposure(all).Pending.Count, "failed all-footprint predicate schedules no damage");
                c.That(!Incident(all, Id(all, "sensor.worksite")).Snapshot.Active, "ineligible target remains unchanged");
            }
        }
    }
}
