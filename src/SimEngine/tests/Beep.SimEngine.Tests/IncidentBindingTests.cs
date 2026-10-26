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
    // JSON authoring and ordinary player commands/views only. No driver-owned incident or work rules.
    internal static class IncidentBindingTests
    {
        internal const string Case = "Incident bindings: native and independent JSON share physical episodes, signals and saved history";
        private static readonly Code Source = new Code("pressure.leak"), SignalSource = new Code("incident-state");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Build(bool independent, InitializeProbe initialize, out IncidentBindingsModule proof,
            Action<JsonValue> edit = null, Action<JsonValue> responseEdit = null)
        {
            proof = new IncidentBindingsModule(initialize);
            return Factory(initialize, out _, extension: proof, responseEdit: responseEdit, conditionsEdit: document => {
                string signalKind = independent ? "independent_incident_signal" : "incident_signal";
                for (int i = 0; i < document.Get("entities").Count; i++)
                {
                    var parts = document.Get("entities")[i].Get("components");
                    var episode = parts.Get("incident").Clone(); var signal = parts.Get("incident_signal").Clone();
                    if (independent) { episode.Set("fault", JsonValue.Of("none")); signal.Set("fault", JsonValue.Of("none")); }
                    signal.Set("phase", JsonValue.Of("detection"));
                    parts.Remove("incident"); parts.Remove("incident_signal");
                    parts.Set(independent ? "independent_incident" : "incident", episode); parts.Set(signalKind, signal);
                }
                var active = document.Get("signals")[0]; active.Get("providers")[0].Set("component", JsonValue.Of(signalKind));
                var held = active.Clone(); held.Set("id", JsonValue.Of("field.detection-held")); held.Set("metric", JsonValue.Of("incident.phase-hold"));
                document.Get("signals").Add(held); edit?.Invoke(document);
            });
        }
        private static EntityId Plant(SimGame game) => Entity(game, "field.plant").Id;
        private static IncidentsView Incidents(SimGame game, int company = 0) => game.Seat(company).View.Feature<IncidentsView>();
        private static IncidentLifecycleView Lifecycle(SimGame game)
        { if (!Incidents(game).TryGetLifecycle(Plant(game), Source, out var facts)) throw new InvalidOperationException("missing lifecycle"); return facts; }
        private static IncidentPhaseView Phase(SimGame game, string phase)
        { var phases = new List<IncidentPhaseView>(); if (!Incidents(game).CopyPhases(Plant(game), Source, phases)) throw new InvalidOperationException("missing phases"); return phases.Single(p => p.Id == phase); }
        private static bool Signal(SimGame game, string id)
        {
            var refusal = View(game).ReadSignal(Plant(game), SignalSource, id, out var facts);
            if (!refusal.IsNone || !facts.Available) throw new InvalidOperationException("missing actual signal " + id + ":" + refusal + "/" + facts.Blocker);
            return facts.Value == Fix.One;
        }
        private static void Raise(Check c, SimGame game)
            => Order(c, game, new RaiseIncidentCommand { Target = Plant(game), Source = Source, Kind = Source });
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current incident contract build");
            c.Equal(154, EngineInfo.SaveFormat, "original episode/work codecs unchanged");
            var native = Physical(c, false); var independent = Physical(c, true);
            c.That(native.SequenceEqual(independent), "native and independent declarations earn identical episode/phase timing and resource outcomes");
            Invalid(c);
        }
        private static long[] Physical(Check c, bool independent)
        {
            var initialize = new InitializeProbe(); var factory = Build(independent, initialize, out var proof); var game = New(factory);
            c.Equal(independent, proof.ActualBindings, "actual independent definitions reuse original runtime aliases");
            c.Equal(1, initialize.Initializations, "Initialize once");
            c.That(!Signal(game, "field.incident-active") && !Signal(game, "field.detection-held") && Lifecycle(game).Snapshot.Episode == 0, "no invented incident or phase work");
            var retained = Phase(game, "detection");
            c.Throws<NotSupportedException>(() => ((IList<string>)retained.NodeIds).Clear(), "phase intent is immutable");
            c.That(!Incidents(game, 1).TryGetLifecycle(Plant(game), Source, out _), "foreign incident history hidden");
            c.Equal(Codes.NotOwned, View(game, 1).ReadSignal(Plant(game), SignalSource, "field.incident-active", out _), "foreign signal hidden");
            ulong hash = game.Hash(); var unsupported = new RaiseIncidentCommand { Target = Plant(game), Source = Source, Kind = new Code("unrelated.incident") };
            c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(unsupported), "wrong incident kind refused"); c.Equal(hash, game.Hash(), "preview/signal/history reads pure");
            var recorder = game.RecordReplay(new ReplayLimits(4 * 1024 * 1024, 3000));
            Order(c, game, new ConfigureAutomationCommand { Rule = "plant.alert", Settings = Settings(game) });
            Until(game, null, () => Rule(game).HasApplied, "initial normal action");
            Raise(c, game); var raised = Lifecycle(game); var twin = factory.Load(game.Save());
            c.That(raised.Snapshot.Active && raised.Snapshot.Episode == 1 && Stock(game) == Fix.FromInt(20) && Signal(game, "field.incident-active"), "raising grants no materials/repair");
            c.Equal(new Code("point.blocked"), Entity(game, "field.plant").State, "Game.Update settles actual incident entity state");
            c.Equal(game.Hash(), twin.Hash(), "raised episode checkpoint");
            Until(game, twin, () => Alert(game).Settings.Level == CompanyAlertLevel.Emergency, "actual deferred signal action");
            Until(game, twin, () => Phase(game, "detection").Complete, "original timed detection");
            c.That(Signal(game, "field.detection-held") && Active(game) && Stock(game) == Fix.FromInt(20), "phase hold derives original completed DAG node, not repair grant");
            long detected = game.Clock.StepIndex;
            Order(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine").Id,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
            twin = factory.Load(game.Save());
            Until(game, twin, () => Stock(game) == Fix.FromInt(12) && Phase(game, "repair").Nodes.Count == 1 && Phase(game, "repair").Nodes[0].Progress.WholeTicks > 0, "real worker travel and partial repair");
            int order = Lifecycle(game).OrderId; var partial = Phase(game, "repair").Nodes[0];
            c.That(partial.Worker.IsValid && partial.JobId > 0 && Active(game), "partial repair has original physical job/worker");
            c.Equal(Fix.FromInt(20), ((PointSnapshot)Entity(game, "machine").Spatial).Position, "worker moves from4 to20 before repair");
            c.That(!retained.Complete && retained.NodesDone == 0, "earlier facts remain detached");
            twin = factory.Load(game.Save()); game.Clock.Pause(); twin.Clock.Pause(); long at = game.Clock.StepIndex; hash = game.Hash(); Step(game, twin);
            c.That(game.Clock.StepIndex == at && game.Hash() == hash, "one paused clock earns no work"); game.Clock.Resume(); twin.Clock.Resume();
            Order(c, game, new CancelOrderCommand { Order = order });
            c.That(Lifecycle(game).OrderId == order && Stock(game) == Fix.FromInt(12) && Active(game), "cancel resumes same original episode without repeated spend");
            c.That(Phase(game, "repair").Nodes[0].Progress.WholeTicks >= partial.Progress.WholeTicks, "earned work retained");
            twin = factory.Load(game.Save()); Until(game, twin, () => !Active(game), "physical final response"); var completed = Lifecycle(game);
            c.That(completed.OrderId == order && completed.OrderState == OrderState.Done && completed.Snapshot.ResolvedTick > raised.Snapshot.RaisedTick, "completion follows elapsed original work");
            c.That(Stock(game) == Fix.FromInt(12) && !Signal(game, "field.incident-active") && !Signal(game, "field.detection-held") && Phase(game, "repair").Complete, "physical response resolves signals exactly once");
            c.That(Entity(game, "field.plant").State != new Code("point.blocked"), "same entity Update clears incident state after completion");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "resolved episode/history checkpoint");
            Until(game, twin, () => Alert(game).Settings.Level == CompanyAlertLevel.Normal, "healthy signal restores policy after dwell");
            Raise(c, game); twin = factory.Load(game.Save()); Until(game, twin, () => Lifecycle(game).OrderId > 0 && Lifecycle(game).OrderId != order, "new physical response episode");
            c.That(Lifecycle(game).Snapshot.Episode == 2 && completed.Snapshot.Episode == 1 && !completed.Snapshot.Active, "new episode does not rewrite retained history");
            Until(game, twin, () => !Active(game), "second episode physical completion");
            c.Equal(Fix.FromInt(4), Stock(game), "each episode consumes its own original eight resources");
            for (int i = 0; i < 6; i++) Step(game, twin);
            c.That(Stock(game) == Fix.FromInt(4) && !Active(game), "settled updates repeat no effects");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), new ReplayLimits(4 * 1024 * 1024, 3000)).Success, "normal commands/signals/physical cancellation replay");
            var fresh = New(factory); c.That(Stock(fresh) == Fix.FromInt(20) && Lifecycle(fresh).Snapshot.Episode == 0 && !Active(fresh), "sessions keep separate episode/work/stock owners");
            return new[] { raised.Snapshot.RaisedTick, detected, completed.Snapshot.ResolvedTick, Lifecycle(game).Snapshot.ResolvedTick };
        }
        private static void Invalid(Check c)
        {
            foreach (string fault in new[] { "id", "workflow", "state", "source", "phase", "extra", "workflow-null", "states-null", "phase-copy", "metrics-empty", "episode.throw", "episode.pause", "episode.reenter", "signal.throw", "signal.pause", "signal.reenter", "unbound" })
            {
                var initialize = new InitializeProbe();
                Action create = () => New(Build(true, initialize, out _, document => {
                    // Unselected metadata must fail before any entity Initialize, not only on later spawning.
                    var unused = document.Get("entities")[0].Clone(); unused.Set("id", JsonValue.Of("field.unused")); document.Get("entities").Add(unused);
                    var parts = unused.Get("components"); var episode = parts.Get("independent_incident"); var signal = parts.Get("independent_incident_signal");
                    if (fault == "id") episode.Set("id", JsonValue.Of("Bad Id"));
                    if (fault == "workflow") episode.Set("workflow", JsonValue.Of("missing"));
                    if (fault == "state") episode.Get("states").Set("active", JsonValue.Of("missing"));
                    if (fault == "source") signal.Set("source", JsonValue.Of("missing"));
                    if (fault == "phase") signal.Set("phase", JsonValue.Of("missing"));
                    if (fault == "extra") episode.Set("unused_legacy", JsonValue.Of(true));
                    if (fault == "workflow-null" || fault == "states-null") episode.Set("fault", JsonValue.Of(fault));
                    if (fault == "phase-copy" || fault == "metrics-empty") signal.Set("fault", JsonValue.Of(fault));
                    if (fault.StartsWith("episode.")) episode.Set("fault", JsonValue.Of("validation-" + fault.Substring(8)));
                    if (fault.StartsWith("signal.")) signal.Set("fault", JsonValue.Of("validation-" + fault.Substring(7)));
                    if (fault == "unbound") { episode.Remove("fault"); parts.Remove("independent_incident"); parts.Set("unbound_incident", episode); }
                }));
                if (fault.Contains(".")) c.Throws<InvalidOperationException>(create, "pure callback refused " + fault);
                else c.Throws<ContentException>(create, "early actual binding refusal " + fault);
                c.Equal(0, initialize.Initializations, "unused malformed binding rejected before Initialize " + fault);
            }
        }
    }
}
