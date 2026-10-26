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
    // JSON, public player inputs and detached facts only. Negative callbacks live in ExtensionProof.
    internal static class ExplorationObservationDurabilityTests
    {
        internal const string Case = "Exploration observation durability: physical contact and first faults retain private immutable facts";
        private static readonly Code Source = new Code("binding.contact"), Failure = new Code("zz.failure"), Sight = new Code("sensor.sight");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool independent, bool half, string fault = null, ExplorationBindingsModule binding = null)
        {
            var probe = Json.Parse(FrameworkProofContent.Read("exploration-observation.json"));
            if (fault != null) probe.Set("mode", JsonValue.Of(fault));
            return ExplorationBindingTests.Factory(binding ?? new ExplorationBindingsModule(), independent,
                sensorEdit: sensor => { if (fault != null) sensor.Get("entities")[0].Get("components").Set("exploration_observation_probe", probe); },
                extraModules: fault == null ? null : new GameModule[] { new ExplorationObservationModule() },
                extraDeclarations: fault == null ? null : new[] { Json.Parse(FrameworkProofContent.Read("exploration-observation-module.json")) },
                definitionEdit: root => { if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30")); });
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 827 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static EntityId Entity(SimGame g, string name) => g.Seat(0).Snapshot.Entities.Single(e => e.Def == name).Id;
        private static Fix Position(SimGame g) => ((PointSnapshot)g.Seat(0).Snapshot.Entities.Single(e => e.Def == "sensor.machine").Spatial).Position;
        private static ExplorationSnapshot Read(SimGame g)
        {
            var refusal = g.Seat(0).View.Feature<ExplorationView>().Read(Entity(g, "sensor.machine"), Source, out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("exploration refusal " + refusal); return facts;
        }
        private static ExplorationEntrySnapshot Entry(SimGame g, string name) => Read(g).Entries.Single(e => e.Id == name);
        private static bool Known(SimGame g, string name, int seat = 0) => g.Seat(seat).View.Feature<KnowledgeView>()
            .TryRead(new PlayerId(seat), "sim.knowledge.facts", "binding." + name + "-known", out _);
        private static Fix Stock(SimGame g, string name) => TransformationContentContractTests.Stock(g, name);
        private static void Step(SimGame g, SimGame twin = null) => PermanentRemovalTests.Step(g, twin);
        private static void Until(SimGame g, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 480 && !done(); i++) Step(g, twin);
            if (!done()) throw new InvalidOperationException(stage + " step=" + g.Clock.StepIndex + " position=" + Position(g)
                + " entries=" + string.Join(";", Read(g).Entries.Select(e => e.Id + "/" + e.Progress + "/" + e.Blocker)));
        }
        private static CommandResult Submit(Check c, SimGame g, Command command) => PermanentRemovalTests.Submit(c, g, command);
        private static void Control(Check c, SimGame g, bool enabled) => Submit(c, g, new ConfigureOperationCommand
            { Entity = Entity(g, "sensor.machine"), Operation = Sight, Enabled = enabled, Target = Fix.FromInt(3) });
        private static void Travel(Check c, SimGame g, string target) => Submit(c, g, new PostOrderCommand
            { Plan = "binding.travel", Target = Entity(g, target), Priority = 70 });
        private static SimGame Saved(Check c, GameFactory f, SimGame g)
        { var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "original exploration checkpoint"); return twin; }
        private static void Physical(Check c, bool independent, bool half)
        {
            var f = Factory(independent, half); var g = New(f); var idle = New(f); ulong isolated = idle.Hash();
            var old = Read(g); c.Equal(ExplorationCodes.Outside, old.Entries[0].Blocker, "actual region initial blocker");
            c.Throws<NotSupportedException>(() => ((IList<ExplorationEntrySnapshot>)old.Entries).Clear(), "entries immutable");
            c.Equal(Codes.NotFound, g.Seat(1).View.Feature<ExplorationView>().Read(Entity(g, "sensor.machine"), Source, out var hidden), "private source refusal");
            c.That(hidden == null, "no private profile facts");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = g.RecordReplay(limits))
            {
                Submit(c, g, new SetWorkerHeldCommand { Worker = Entity(g, "sensor.machine"), Held = true });
                g.Seat(0).View.TryFindResource("supply", out var resource);
                int route = checked((int)Submit(c, g, new AddRouteCommand { From = Entity(g, "source"), To = Entity(g, "sensor.machine"),
                    Resource = resource, Settings = new LogisticsRouteSettings(true, Fix.One, 70) }).Arg0);
                c.Equal(Fix.Zero, Stock(g, "sensor.machine"), "supply not instant");
                var twin = Saved(c, f, g); Until(g, twin, () => Stock(g, "sensor.machine") == Fix.One, "physical sensor supply");
                Submit(c, g, new RemoveRouteCommand { Route = route });
                Until(g, null, () => PermanentRemovalTests.Jobs(g).All(j => j.State == JobState.Done || j.State == JobState.Cancelled), "carrier departure");
                Submit(c, g, new SetWorkerHeldCommand { Worker = Entity(g, "machine"), Held = true });
                Control(c, g, false); Submit(c, g, new SetWorkerHeldCommand { Worker = Entity(g, "sensor.machine"), Held = false });
                Travel(c, g, "destination"); twin = Saved(c, f, g);
                Until(g, twin, () => Position(g) == Fix.FromInt(10), "physical contact arrival");
                c.Equal(ExplorationCodes.NoSensor, Entry(g, "a.reset").Blocker, "active sensor required");
                c.That(!Known(g, "reset") && !Known(g, "retain"), "no arrival discovery");
                Control(c, g, true); Until(g, null, () => Entry(g, "a.reset").Progress.WholeTicks >= 3, "partial contact dwell");
                var partial = Read(g); c.That(partial.Entries.All(e => !e.Known && e.Progress.WholeTicks < e.RequiredSteps), "parallel unfinished timed contact");
                c.Equal(new Code("binding.exploring"), g.Seat(0).Snapshot.Entities.Single(e => e.Def == "sensor.machine").State, "entity Update sampling state");
                Control(c, g, false); long retained = Entry(g, "b.retain").Progress.WholeTicks;
                c.Equal(0L, Entry(g, "a.reset").Progress.WholeTicks, "authored reset interruption");
                c.That(retained > 0, "retained actual unfinished intervals");
                twin = Saved(c, f, g); long at = g.Clock.StepIndex; g.Clock.Pause(); twin.Clock.Pause(); Step(g, twin);
                c.Equal(at, g.Clock.StepIndex, "one paused game clock"); g.Clock.Resume(); twin.Clock.Resume(); Step(g, twin);
                c.Equal(retained, Entry(g, "b.retain").Progress.WholeTicks, "saved disabled retain stays unfinished");
                Travel(c, g, "sensor.contact"); twin = Saved(c, f, g);
                Until(g, twin, () => Position(g) == Fix.Parse("12.5"), "physical contact departure");
                c.Equal(ExplorationCodes.Outside, Entry(g, "b.retain").Blocker, "actual outside interruption");
                c.Equal(retained, Entry(g, "b.retain").Progress.WholeTicks, "outside retain preserved");
                Control(c, g, true); Travel(c, g, "destination"); twin = Saved(c, f, g);
                Until(g, twin, () => Position(g) == Fix.FromInt(10), "physical return");
                Until(g, twin, () => Known(g, "reset") && Known(g, "retain"), "earned complete contact");
                c.That(Read(g).Entries.All(e => e.Known && e.Progress.Equals(WorkProgress.Zero)), "original ledger owns completed membership");
                c.That(!Known(g, "reset", 1) && !Known(g, "retain", 1), "private earned knowledge");
                c.Equal(Fix.One, Stock(g, "sensor.machine"), "single actual delivered unit");
                c.Equal(Fix.FromInt(19), Stock(g, "source"), "single actual source debit");
                c.That(old.Entries.All(e => !e.Known && e.Progress.Equals(WorkProgress.Zero)), "retained initial snapshot unchanged");
                c.That(partial.Entries.All(e => !e.Known), "retained unfinished snapshot unchanged");
                Saved(c, f, g); c.Equal(isolated, idle.Hash(), "interleaved session isolated");
                c.That(new ReplayVerifier().Verify(f, recorder.Finish(), limits).Success, "actual public player stream replays");
                Console.WriteLine("exploration physical independent=" + independent + " half=" + half + " final=" + g.Clock.StepIndex);
            }
        }
        private static void ReadFailure(Check c, string mode)
        {
            var f = Factory(true, false, mode); var g = New(f); var idle = New(f); ulong isolated = idle.Hash();
            var cached = g.Seat(0).View.Feature<ExplorationView>(); var foreign = g.Seat(1).View.Feature<ExplorationView>();
            var id = Entity(g, "sensor.machine"); var old = Read(g); var rows = new List<ExplorationSnapshot>();
            cached.CopySources(rows); c.Equal(2, rows.Count, "actual contact and explicit negative source initially healthy");
            var saved = g.Save(); c.Equal(g.Hash(), f.Load(saved).Hash(), "healthy negative source checkpoint");
            for (int i = 0; i < 3; i++) Step(g);
            rows.Add(old);
            Exception error = null; try { cached.CopySources(rows); } catch (Exception actual) { error = actual; }
            c.That(error is InvalidOperationException, "actual failing observation rejects " + mode);
            if (error == null) throw new InvalidOperationException("negative source did not fail " + mode);
            if (mode == "throw" || mode == "clock-throw") c.Equal("proof.exploration-first-error", error.Message, "original callback not masked");
            c.Equal(0, rows.Count, "later failing source publishes no healthy prefix");
            c.That(g.Fault != null, "original session fault latched " + mode); var first = g.Fault;
            foreach (var view in new[] { cached, foreign })
                foreach (var target in new[] { id, new EntityId(int.MaxValue) })
                    foreach (var source in new[] { Source, Failure, Code.None, new Code("missing.source") })
                    {
                        ExplorationSnapshot rejected = old;
                        c.Throws<InvalidOperationException>(() => view.Read(target, source, out rejected), "cached missing/private/source query rejects fault");
                        c.That(rejected == null, "faulted query cannot leak retained prefix");
                    }
            rows.Add(old); c.Throws<InvalidOperationException>(() => cached.CopySources(rows), "cached collection rejects fault");
            c.Equal(0, rows.Count, "cached seeded collection cleared");
            c.Throws<InvalidOperationException>(() => g.Save(), "failed observation cannot checkpoint");
            c.Throws<InvalidOperationException>(() => g.Hash(), "failed observation cannot hash healthy facts");
            c.Equal(0, g.Update(), "terminal original update"); c.That(ReferenceEquals(first, g.Fault), "first fault remains authoritative");
            c.Equal(isolated, idle.Hash(), "failed session isolated"); Step(idle);
            c.That(old.Entries.All(e => !e.Known), "retained facts detached after actual error");
        }
        private static void ValidationFailure(Check c)
        {
            var binding = new ExplorationBindingsModule(); Exception error = null;
            try { New(Factory(true, false, "composition-clock-throw", binding)); } catch (Exception actual) { error = actual; }
            c.That(error is InvalidOperationException && error.Message == "proof.exploration-first-error", "composition preserves first callback error");
            c.Equal(0, binding.Initialize.Initializations, "invalid composition cannot Initialize");
            var f = Factory(true, false, "load-clock-throw"); var g = New(f); var old = Read(g); ulong hash = g.Hash(); error = null;
            try { f.Load(g.Save()); } catch (Exception actual) { error = actual; }
            c.That(error is InvalidOperationException && error.Message == "proof.exploration-first-error", "loaded validation preserves first callback error");
            c.Equal(hash, g.Hash(), "failed loaded session cannot affect original"); Step(g);
            c.That(old.Entries.All(e => !e.Known), "retained original facts survive failed load");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current engine identity");
            c.Equal(154, EngineInfo.SaveFormat, "original contact state schema");
            foreach (bool independent in new[] { false, true }) foreach (bool half in new[] { false, true }) Physical(c, independent, half);
            foreach (string mode in new[] { "throw", "clock", "clock-throw", "malformed" }) ReadFailure(c, mode);
            ValidationFailure(c);
        }
    }
}
