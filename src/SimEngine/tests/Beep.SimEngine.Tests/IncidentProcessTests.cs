using System;
using System.Collections.Generic;
using System.IO;
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
    internal static class IncidentProcessTests
    {
        internal static void Register()
        {
            TestRunner.Add("Incident pressure: authored metadata and bounded full-cell waits", Admission);
            TestRunner.Add("Incident pressure: native finite pulses recovery phase stop and episode reset", c => Physical(c, true));
            TestRunner.Add("Incident pressure: independent half-minute finite pulses recovery phase stop and episode reset", c => Physical(c, false));
            TestRunner.Add("Incident pressure: authored reset and retain partial phase-stop policies", ProgressPolicies);
        }
        internal static GameFactory Factory(bool native, bool half = false, Action<JsonValue> edit = null, bool full = false, bool environment = false)
            => IndependentTileWorkTests.Factory(nativeTiles: native, incidentPressure: true, independentPressure: !native, halfMinute: half,
                authorIncidentPressure: edit, spillEnvironment: environment,
                authorSpillResources: full ? json => json.Get("tile_layer_setups")[0].Set("value", JsonValue.Number("99")) : null);
        internal static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "independent-sensor", Seed = 809, Seats = { Controller.Human, Controller.Human } });
        internal static EntityId Id(SimGame game, string definition) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == definition).Id;
        internal static Fix Stock(SimGame game, EntityId entity, string resource = "oil")
        { var rows = new List<Facet>(); game.Seat(0).View.Inspect(entity, rows); return rows.Where(f => f.Key == Storage.FacetStock && f.Subject == resource).Aggregate(Fix.Zero, (sum, f) => sum + f.Value); }
        private static Fix Spill(SimGame game)
        {
            var definition = game.World.Content.Get<ISpatialLayerDefinition>("sensor.spilled-oil");
            return TileSpatialGeometry.RequireSampleLayer(game.World, (ITileSpatialSpace)game.World.Space, definition, false).GetSample(new TileCoord(14, 20));
        }
        internal static IncidentProcessSnapshot Process(SimGame game)
        {
            if (!game.Seat(0).View.Feature<IncidentsView>().TryGetProcess(Id(game, "sensor.leaking-site"), new Code("leak-flow"), out var result))
                throw new InvalidOperationException("missing original incident-process view");
            return result;
        }
        internal static IncidentLifecycleView Incident(SimGame game)
        {
            if (!game.Seat(0).View.Feature<IncidentsView>().TryGetLifecycle(Id(game, "sensor.leaking-site"), new Code("leak"), out var result))
                throw new InvalidOperationException("missing original phased incident");
            return result;
        }
        internal static bool RepairComplete(SimGame game)
        {
            var nodes = new List<ActivityNodeView>();
            return game.Seat(0).View.Feature<WorkView>().TryCopyNodes(Incident(game).OrderId, nodes) && nodes.Single(n => n.Id == "repair").State == NodeState.Done;
        }
        internal static List<WorkJobView> Jobs(SimGame game)
        { var rows = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(rows); return rows; }
        private static void Step(Check c, SimGame game, SimGame twin = null, int expected = 5)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("incident process fault " + game.Fault + "/" + twin?.Fault);
            Fix total = Spill(game); foreach (var entity in game.Seat(0).Snapshot.Entities) total += Stock(game, entity.Id);
            c.Equal(Fix.FromInt(expected), total, "finite current tank + cell + cargo conserves actual authored material");
            if (twin != null) c.Equal(game.Hash(), twin.Hash(), "saved original interval/incident/work/material owners continue identically");
        }
        private static void Until(Check c, SimGame game, SimGame twin, Func<bool> ready, string stage, int expected = 5)
        {
            for (int i = 0; i < 480 && !ready(); i++) Step(c, game, twin, expected);
            if (!ready()) { var p = Process(game); throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " interval=" + p.ElapsedTicks + "/" + p.RequiredTicks + " blocker=" + p.Blocker + " spill=" + Spill(game)); }
        }
        private static SimGame Save(Check c, GameFactory factory, SimGame game)
        { var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "original interval, episode and resource checkpoint"); return twin; }
        private static CommandResult Send(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary pressure/physical-response input admission");
            c.Equal(hash, game.Hash(), "command preview is pure"); var result = TestKit.Order(game, 0, command);
            c.That(result.Accepted, "engine command " + result.Refusal); if (!result.Accepted) throw new InvalidOperationException("pressure command " + result.Refusal); return result;
        }
        private static void Raise(Check c, SimGame game) => Send(c, game, new RaiseIncidentCommand { Target = Id(game, "sensor.leaking-site"), Source = new Code("leak"), Kind = new Code("sensor.fire") });
        private static void Admission(Check c)
        {
            foreach (string invalid in new[] { "source", "workflow", "phase", "empty", "duplicate", "interval", "unaligned", "effect", "policy" })
                c.Throws<ContentException>(() => New(Factory(false, edit: json => {
                    var profile = json.Get("incident_process_profiles")[0];
                    if (invalid == "source") profile.Set("source", JsonValue.Of("absent"));
                    else if (invalid == "workflow") profile.Set("workflow", JsonValue.Of("sensor.fire"));
                    else if (invalid == "phase") profile.Set("until_phases_complete", Json.Parse("[\"missing.phase\"]"));
                    else if (invalid == "empty") profile.Set("until_phases_complete", Json.Parse("[]"));
                    else if (invalid == "duplicate") profile.Set("until_phases_complete", Json.Parse("[\"repair\",\"repair\"]"));
                    else if (invalid == "interval") profile.Set("interval", Json.Parse("{\"numerator\":0,\"denominator\":1,\"unit\":\"minute\"}"));
                    else if (invalid == "unaligned") profile.Set("interval", Json.Parse("{\"numerator\":1,\"denominator\":2,\"unit\":\"minute\"}"));
                    else if (invalid == "effect") profile.Set("effect", JsonValue.Of("missing.effect"));
                    else profile.Set("ineligible_progress", JsonValue.Of("invent"));
                })), "invalid process metadata refuses before play");
            foreach (bool native in new[] { false, true })
            {
                var factory = Factory(native, full: true); var game = New(factory); Raise(c, game); var twin = Save(c, factory, game);
                Until(c, game, twin, () => Spill(game) == Fix.FromInt(100), "actual finite cell fills", 104);
                Until(c, game, twin, () => Process(game).Due && Process(game).Blocker == WorkEffectCodes.IncompatibleInventory, "one saturated due pulse waits for actual room", 104);
                var held = Process(game); twin = Save(c, factory, game); for (int i = 0; i < 8; i++) Step(c, game, twin, 104);
                c.Equal(held.ElapsedTicks, Process(game).ElapsedTicks, "backpressure retains one due interval, not accumulated missed pulses");
                c.Equal(Fix.FromInt(4), Stock(game, Id(game, "sensor.leaking-site")), "full cell cannot delete or create local tank material");
                c.That(game.Fault == null, "capacity wait remains healthy and loadable");
            }
        }
        private static void Physical(Check c, bool native)
        {
            var factory = Factory(native, half: !native); var game = New(factory); var idle = New(factory); var idleReference = New(factory);
            var source = Id(game, "sensor.leaking-site"); var worker = Id(game, "sensor.cart");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "persistent incident needs no business module");
            c.Equal(IncidentProcessCodes.Inactive, Process(game).Blocker, "inactive source has no pressure");
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = game.RecordReplay(limits);
            Raise(c, game); var first = Process(game); var twin = Save(c, factory, game);
            c.Equal(native ? 2L : 4L, first.RequiredTicks, "same two-minute interval uses the selected session quantum");
            c.That(first.ElapsedTicks > 0 && first.ElapsedTicks < first.RequiredTicks && Spill(game).IsZero, "new episode earns a partial interval without instant material movement");
            game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash(); Step(c, game, twin); c.Equal(paused, game.Hash(), "one paused clock freezes process pressure"); game.Clock.Resume(); twin.Clock.Resume();
            ulong hash = game.Hash(); var view = game.Seat(0).View.Feature<IncidentsView>();
            c.That(!game.Seat(1).View.Feature<IncidentsView>().TryGetProcess(source, new Code("leak-flow"), out var foreign) && foreign.RequiredTicks == 0, "foreign process view withholds cadence and episode state");
            c.Equal(hash, game.Hash(), "owned/private process diagnostics are pure");
            var runtime = game.World.Entities.Get(source).Get<IIncidentProcess>(new Code("leak-flow"));
            c.Throws<InvalidOperationException>(() => runtime.UpdateProcess(game.World, new TickInfo(game.Clock.StepIndex, 1, 0, game.World.Time)), "a caller cannot advance the process outside the original game update");
            c.Equal(hash, game.Hash(), "rejected outside-loop visit changes no interval or material");
            foreach (var invalid in new[] { (Episode: -1, Elapsed: 0L), (Episode: 0, Elapsed: 1L) })
            {
                using var stream = new MemoryStream(); var writer = new StateWriter(stream); int episode = invalid.Episode; long elapsed = invalid.Elapsed; writer.Int(ref episode); writer.Long(ref elapsed); writer.Flush();
                c.Throws<CorruptStateException>(() => ((Component)runtime).Sync(new StateReader(stream.ToArray(), 0, checked((int)stream.Length))), "invalid original interval codec prefix refuses before publication");
                c.Equal(hash, game.Hash(), "bad interval/provenance restore retains actual state");
            }
            Until(c, game, twin, () => Spill(game) == Fix.FromInt(5), "repeated real material pulses exhaust finite tank");
            Until(c, game, twin, () => Process(game).Due && Process(game).Blocker == WorkEffectCodes.NoStock, "finite source waits without fabricated spill stock");
            twin = Save(c, factory, game); for (int i = 0; i < 6; i++) Step(c, game, twin);
            Step(c, idle, idleReference); c.Equal(IncidentProcessCodes.Inactive, Process(idle).Blocker, "interleaved live session keeps its own inactive incident");
            c.Equal(first.RequiredTicks, Process(game).ElapsedTicks, "empty-source backlog is one due interval"); c.Equal(1L, first.ElapsedTicks, "held early snapshot remains detached");
            Send(c, game, new PostOrderCommand { Plan = "sensor.recover-spill-work", Target = source, Priority = 100 }); twin = Save(c, factory, game);
            Until(c, game, twin, () => Spill(game) == Fix.FromInt(2), "positive-time machine recovery physically refills the tank");
            Step(c, game, twin); c.Equal(Fix.FromInt(3), Spill(game), "one pending pulse resumes once after supply, not a catch-up burst");
            Step(c, game, twin); c.Equal(Fix.FromInt(3), Spill(game), "next pulse must earn its positive interval again"); twin = Save(c, factory, game);
            Until(c, game, twin, () => Jobs(game).All(j => j.Worker != worker), "recovery worker physically departs");
            game.Seat(0).View.TryFindResource("steel", out var steel);
            Send(c, game, new HaulResourcesCommand { Source = Id(game, "sensor.depot"), Destination = source, Resource = steel, Ceiling = Fix.One, Worker = worker, Priority = 100 }); twin = Save(c, factory, game);
            Until(c, game, twin, () => RepairComplete(game), "physically supplied repair completes stop phase");
            c.That(Incident(game).Snapshot.Active && Process(game).Blocker == IncidentProcessCodes.PhaseComplete, "repair completion stops pressure before the episode's unsupplied cleanup ends");
            Until(c, game, twin, () => Jobs(game).All(j => j.Worker != worker), "repair departure preserves physical occupancy");
            Send(c, game, new PostOrderCommand { Plan = "sensor.pressure-recover-one", Target = source, Priority = 100 }); twin = Save(c, factory, game);
            Until(c, game, twin, () => Stock(game, source) == Fix.One, "physical recovery leaves real material after isolation");
            Fix isolatedSpill = Spill(game); for (int i = 0; i < first.RequiredTicks * 2 + 1; i++) Step(c, game, twin);
            c.Equal(isolatedSpill, Spill(game), "completed stop phase prevents leakage despite available local material");
            c.Equal(0L, Process(game).ElapsedTicks, "authored reset policy clears ineligible interval progress");
            Step(c, idle, idleReference); c.Equal(Fix.Zero, Spill(idle), "independently advanced session receives no spill pressure");
            Until(c, game, twin, () => Jobs(game).All(j => j.Worker != worker), "recovered machine departs before supply");
            Send(c, game, new HaulResourcesCommand { Source = Id(game, "sensor.depot"), Destination = source, Resource = steel, Ceiling = Fix.One, Worker = worker, Priority = 100 }); twin = Save(c, factory, game);
            Until(c, game, twin, () => !Incident(game).Snapshot.Active, "physical cleanup resolves original episode");
            c.Equal(IncidentProcessCodes.Inactive, Process(game).Blocker, "resolved episode cannot retain active pressure");
            Raise(c, game); twin = Save(c, factory, game); var second = Process(game);
            c.That(second.Episode == 2 && second.ElapsedTicks == 1 && !second.Due, "new episode starts its own interval provenance without prior carry or another clock");
            Until(c, game, twin, () => Spill(game) == isolatedSpill + Fix.One, "new episode earns a fresh real material pulse");
            Step(c, idle, idleReference); c.That(idle.Clock.StepIndex == 3 && Process(idle).Episode == 0 && Spill(idle).IsZero,
                "interleaved live sessions retain separate clock, episode, interval and material");
            c.Equal(idleReference.Hash(), idle.Hash(), "interleaved untouched-input session matches its independently advanced reference"); Save(c, factory, game);
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits); c.That(replay.Success && replay.Game?.Hash() == game.Hash(), "normal process/physical-response replay " + replay.Failure + "/" + replay.Detail);
        }
        private static void ProgressPolicies(Check c)
        {
            foreach (string policy in new[] { "reset", "retain" })
            {
                var factory = Factory(false, half: true, edit: json => {
                    var profile = json.Get("incident_process_profiles")[0]; profile.Set("ineligible_progress", JsonValue.Of(policy));
                    profile.Set("interval", Json.Parse("{\"numerator\":100,\"denominator\":1,\"unit\":\"minute\"}"));
                });
                var game = New(factory); var source = Id(game, "sensor.leaking-site"); var worker = Id(game, "sensor.cart");
                Raise(c, game); game.Seat(0).View.TryFindResource("steel", out var steel);
                Send(c, game, new HaulResourcesCommand { Source = Id(game, "sensor.depot"), Destination = source, Resource = steel, Ceiling = Fix.One, Worker = worker, Priority = 100 });
                var twin = Save(c, factory, game); Until(c, game, twin, () => RepairComplete(game), "physical phase-stop policy preparation");
                var stopped = Process(game); c.That(stopped.ElapsedTicks > 0 && stopped.ElapsedTicks < stopped.RequiredTicks && !stopped.PhaseEligible,
                    "repair finishes during a real partial process interval");
                twin = Save(c, factory, game); for (int i = 0; i < 3; i++) Step(c, game, twin);
                c.Equal(policy == "reset" ? 0L : stopped.ElapsedTicks, Process(game).ElapsedTicks, "authored ineligible-progress policy is applied by original interval owner");
                c.That(Incident(game).Snapshot.Active && Spill(game).IsZero && Stock(game, source) == Fix.FromInt(5), "policy wait neither resolves incident nor invents or consumes material");
                Save(c, factory, game);
            }
        }
    }
}
