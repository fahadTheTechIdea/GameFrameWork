using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class ScenarioPressureTests
    {
        internal static readonly Code Leak = new Code("pressure.leak");
        internal const string RefundCase = "Scenario pressure refund: queued native refusal refunds and re-reserves actual cost";
        internal const string RetainedFaultCase = "Scenario pressure views: retained faults reject private missing candidates and clear outputs";
        public static void Register()
        {
            TestRunner.Add("Scenario pressure: authored budgets, ordinary incidents and saved physical response", Run);
            TestRunner.Add(RefundCase, RefundRace);
            TestRunner.Add(RetainedFaultCase, RetainedFaults);
        }
        internal static GameFactory Factory(string profile = "pressure.incidents", string content = null, Action<JsonValue> edit = null, GameModule viewProbe = null, string response = null)
            => ConstructionPlanTests.Points(extraModules: viewProbe == null ? new GameModule[] { new ScenarioPressureProofModule() }
                    : new GameModule[] { new ScenarioPressureProofModule(), viewProbe },
                extraDeclarations: "[{\"id\":\"sim.scenario-pressure\",\"version\":\"1.0.0\",\"documents\":[\"scenario-pressure.json\"]}"
                    + (viewProbe == null ? "" : ",{\"id\":\"" + viewProbe.Id + "\",\"version\":\"1.0.0\",\"documents\":[]}") + "]",
                extraDocuments: new Dictionary<string, string> { ["scenario-pressure.json"] = content ?? FrameworkProofContent.Read("scenario-pressure.json") },
                additionalDocuments: new Dictionary<string, string> { ["pressure-response.json"] = response ?? FrameworkProofContent.Read("pressure-response.json") },
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(ScenarioPressureModule.ModuleId));
                    scenario.Get("modules").Add(JsonValue.Of(IncidentResponseModule.ModuleId));
                    if (viewProbe != null) scenario.Get("modules").Add(JsonValue.Of(viewProbe.Id));
                    scenario.Get("settings").Set(ScenarioPressureModule.ModuleId, Content.Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"" + profile + "\"}]}"));
                    edit?.Invoke(scenario);
                }, startupProfiles: new[] { "pressure-response.start" });
        internal static SimGame New(GameFactory factory, params string[] mods) => ConstructionPlanTests.NewPoints(factory, mods);
        internal static ScenarioPressureSnapshot Facts(SimGame game)
        { var facts = new List<ScenarioPressureSnapshot>(); game.Seat(0).View.Feature<ScenarioPressureView>().CopyPressure(facts); return facts.Single(); }
        private static Entity Target(SimGame game)
            => game.World.Entities.All.Where(e => e.Alive && e.Owner == new PlayerId(0) && e.Def.Id == "pressure.machine").OrderBy(e => e.Id).First();
        internal static bool Active(SimGame game, EntityId entity, int seat = 0)
        { game.Seat(seat).View.Feature<IncidentsView>().TryGetIncident(entity, Leak, out IncidentSnapshot snapshot); return snapshot.Active; }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 400 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " pressure=" + Facts(game).Blocker + "/" + Facts(game).PendingSequence +
                " orders=" + string.Join(";", Board(game).Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "native pacing/command ownership has exact current identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no former proof command/save reader");
            Physical(c); RefundRace(c); PublicSource(c); InvalidContent(c);
            SimGame omitted = New(ConstructionPlanTests.Points(scenarioEdit: scenario => scenario.Get("modules").Add(JsonValue.Of(IncidentResponseModule.ModuleId))));
            c.That(!omitted.World.Services.Has<IScenarioPressure>(), "omitted optional module installs no pacing policy/state");
            c.That(omitted.World.Commands.TryGet(typeof(RaiseIncidentCommand), out CommandRegistry.Entry command) && command.Id == "sim.raise-incident" && command.Owner == IncidentResponseModule.ModuleId,
                "incident command is natively owned even without pressure");
            c.That(!omitted.World.Commands.Entries.Any(e => e.Id.Contains("proof.shipments.raise") || e.Id.Contains("colony.raise")), "no old proof incident registration/alias remains");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity target = Target(game);
            c.Equal(Controller.Human, game.World.Players.ControllerOf(new PlayerId(0)), "pressure does not replace human controller");
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IBusinessAccounts>(), "pacing/work need neither economy nor Tycoon");
            c.That(game.World.Content.Get<ScenarioPressureProfileDef>("pressure.incidents").AuthoringPath.Contains("scenario-pressure.json"), "profile comes from explicit master document");
            var retained = Facts(game); ulong hash = game.Hash(); var privateFacts = new List<ScenarioPressureSnapshot> { retained };
            game.Seat(1).View.Feature<ScenarioPressureView>().CopyPressure(privateFacts);
            c.Equal(0, privateFacts.Count, "other company reads no private pacing state and clears prior output");
            game.World.Services.Get<IScenarioPressure>().CopyPressure(new PlayerId(-2), privateFacts); c.Equal(0, privateFacts.Count, "invalid negative is not observer");
            game.World.Services.Get<IScenarioPressure>().CopyPressure(new PlayerId(8), privateFacts); c.Equal(0, privateFacts.Count, "nonseat receives no pacing state");
            game.Observer.Feature<ScenarioPressureView>().CopyPressure(privateFacts); c.Equal(1, privateFacts.Count, "exact observer deliberately sees enrollment");
            c.Equal(hash, game.Hash(), "all pacing diagnostics are pure");
            Tick(game);
            var pending = Facts(game);
            c.That(pending.PendingSequence > 0 && pending.PendingSource == "pressure.a-leak" && pending.Reserved == Fix.One && pending.Available == Fix.Zero,
                "stable source tie-break reserves actual cost in normal command queue");
            c.That(!Active(game, target.Id) && pending.Accepted == 0 && pending.Spent == Fix.Zero && pending.Decisions == 1,
                "decision submits for next Update, not instant pressure mutation/charge");
            c.That(retained.PendingSequence == 0 && retained.Available == Fix.One, "retained pacing facts stay detached");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending native command/reservation restores from actual inbox and one pacing codec");
            Tick(game); Tick(twin);
            var charged = Facts(game);
            c.That(Active(game, target.Id) && charged.Accepted == 1 && charged.PendingSequence == 0 && charged.Reserved == Fix.Zero && charged.Spent == Fix.One,
                "accepted ordinary native incident spends reservation exactly once");
            c.That(charged.CooldownUntil == 61 && charged.Blocker == PressureCodes.Cooldown, "cooldown uses existing game time from actual acceptance");
            var answers = new List<CommandResult>(); long cursor = 0; game.Seat(0).ReadResults(ref cursor, answers);
            c.That(answers.Any(a => a.Sequence == pending.PendingSequence && a.Accepted && a.Command == "sim.raise-incident" && a.Entity == target.Id),
                "pressure has ordinary native result/actual target identity");
            c.Equal(game.Hash(), twin.Hash(), "accepted incident and native response creation are twin-identical");
            c.That(game.World.Entities.All.Where(e => e.Owner == new PlayerId(1) && e.Def.Id == "pressure.machine").All(e => !ResponseWork.Read(e.Get<IIncident>(Leak)).Active),
                "private source never targets another company");
            Configure(c, game); twin = factory.Load(game.Save());
            Reach(game, () => Board(game).Orders.Any(o => o.Target == target.Id && o.Node("repair").Progress.WholeTicks > 0), "physical repair work", twin);
            c.That(Active(game, target.Id) && Find(game, "machine").Get<PointPresence>().Position == Fix.FromInt(20), "response worker travels physically before repairing");
            c.Equal(Fix.FromInt(12), target.Get<IInventory>().Amount(Supply(game)), "response consumes eight actual materials without replenishment bonus");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "active partially repaired incident saves with pacing chronology");
            Reach(game, () => !Active(game, target.Id), "actual emergency completion", twin);
            c.That(game.Hash() == twin.Hash() && Facts(game).Accepted == 1, "physical recovery and saved pacing continuation agree without extra incident grant");
            c.Equal(Fix.FromInt(12), target.Get<IInventory>().Amount(Supply(game)), "completion does not mint replacement resources");
            while (game.Tick < 62) Tick(game);
            c.That(Facts(game).Available == Fix.FromRatio(3, 4) && Facts(game).PendingSequence == 0 && Facts(game).Blocker == PressureCodes.Budget,
                "three elapsed refill periods replenish exactly; unpayable pressure is an explicit wait");
            while (game.Tick < 81) Tick(game);
            c.That(Facts(game).Reserved == Fix.One && Facts(game).PendingSequence > 0 && Facts(game).Spent == Fix.One,
                "fourth elapsed refill makes one real next-update incident affordable, not an accepted charge");
            twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(Facts(game).Accepted == 2 && Facts(game).Spent == Fix.FromInt(2) && game.Hash() == twin.Hash(), "replenished dispatch/acceptance is saved and charged once");
        }
        private static void RefundRace(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity target = Target(game);
            Tick(game); var pending = Facts(game);
            // Explicit engine protocol race instrumentation, NOT normal player workflow or pressure shortcut.
            c.That(target.Get<IIncident>(Leak).Raise(game.World, Leak), "fixture changes actual incident after queued preview, before command application");
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            var refunded = Facts(game);
            c.That(refunded.Refused == 1 && refunded.Accepted == 0 && refunded.Spent == Fix.Zero && refunded.Available + refunded.Reserved == Fix.One,
                "actual command refusal refunds cost exactly; ordinary next-cadence reservation is still unspent");
            c.That(refunded.PendingSequence > pending.PendingSequence && refunded.Reserved == Fix.One && refunded.Decisions == 2,
                "refunded points may reserve a fresh eligible target, never reuse the refused correlation ticket");
            var answers = new List<CommandResult>(); long cursor = 0; game.Seat(0).ReadResults(ref cursor, answers);
            c.That(answers.Any(a => a.Sequence == pending.PendingSequence && !a.Accepted && a.Refusal == Codes.InvalidArgument), "refunded attempt has ordinary authoritative refusal");
            c.Equal(game.Hash(), twin.Hash(), "refusal/correlation cleanup restores and continues identically");
            Tick(game); Tick(twin);
            c.That(Facts(game).Accepted == 1 && Facts(game).Refused == 1 && Facts(game).Spent == Fix.One && game.Hash() == twin.Hash(),
                "fresh replacement request spends refunded points only after its own actual acceptance");
        }
        private static void PublicSource(Check c)
        {
            GameFactory factory = Factory("pressure.policy"); SimGame game = New(factory);
            c.That(game.World.Content.Get<WorkerPolicyPressureDef>("pressure.worker") is IPressureSource, "public source extends sole contract/content registry");
            for (int i = 0; i < 5; i++) Tick(game);
            c.That(Facts(game).PendingSequence == 0 && Facts(game).Spent == Fix.Zero && Facts(game).Blocker.Id == "proof.pressure.grace", "authored grace reads actual game time without a new clock");
            Tick(game); SimGame twin = factory.Load(game.Save());
            c.That(Facts(game).Reserved == Fix.FromRatio(1, 2) && Facts(game).Available == Fix.FromRatio(1, 2), "unrelated policy action uses same pressure reservation and ordinary command");
            Tick(game); Tick(twin);
            game.Seat(0).View.Feature<WorkView>().TryReadPolicy(Find(game, "machine").Id, out WorkerPolicyView policy);
            c.That(policy.Settings.Enabled && Facts(game).Accepted == 1 && Facts(game).Spent == Fix.FromRatio(1, 2) && game.Hash() == twin.Hash(),
                "public source executes native policy with no scheduler/host gameplay change");
            for (int i = 0; i < 24; i++) Tick(game);
            c.That(Facts(game).Available == Fix.FromRatio(1, 2) && Facts(game).Accepted == 1 && Facts(game).PendingSequence == 0,
                "explicit zero refill and no eligible worker cannot spend or fabricate more actions");
            c.That(Facts(game).Decisions <= game.Tick && Facts(game).NextDecision > Facts(game).LastDecision, "decision cadence remains bounded and visible");
        }
        private static void RetainedFaults(Check c)
        {
            int calls = 0;
            const string marker = "proof.pressure.cached-view.failure";
            var probe = new ProbeModule("proof.pressure-view-fault", new ModuleVersion(1, 0, 0), compose: composer =>
                composer.AddViewFeature<ProbeView>("proof.pressure-view-fault", 1, (world, viewer) =>
                { calls++; throw new InvalidOperationException(marker); }));
            var factory = Factory(viewProbe: probe); var healthy = New(factory); var idle = New(factory);
            ulong untouched = idle.Hash(); var limits = new ReplayLimits(4 * 1024 * 1024, 1000);
            using (var recording = healthy.RecordReplay(limits))
            {
                var twin = factory.Load(healthy.Save());
                for (int i = 0; i < 3; i++)
                {
                    Tick(healthy); Tick(twin);
                    c.Equal(healthy.Hash(), twin.Hash(), "healthy pending/accepted pressure views preserve actual saved chronology");
                }
                var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
                c.That(replay.Success && replay.ActualHash == healthy.Hash(), "complete initial native pressure decisions replay to final live hash");
                Console.WriteLine("pressure cached-view healthy replay: initial=" + recording.InitialStep + ";final=" + healthy.Tick
                    + ";commands=" + recording.CommandCount + ";checkpoints=" + recording.CheckpointCount
                    + ";expected-hash=" + healthy.Hash().ToString("X16") + ";actual-hash=" + replay.ActualHash.ToString("X16"));
            }
            foreach (int age in new[] { 0, 1, 2 })
            foreach (bool restored in new[] { false, true })
            {
                var game = New(factory);
                for (int i = 0; i < age; i++) Tick(game);
                if (restored) game = factory.Load(game.Save());
                var own = game.Seat(0).View.Feature<ScenarioPressureView>();
                var foreign = game.Seat(1).View.Feature<ScenarioPressureView>();
                var observer = game.Observer.Feature<ScenarioPressureView>();
                var retained = Facts(game); var candidates = new List<PressureCandidateSnapshot>();
                ulong before = game.Hash();
                c.That(own.CopyCandidates(new PlayerId(0), "pressure.a-leak", candidates) && candidates.Count == (age < 2 ? 2 : 1),
                    "healthy native candidate facts reflect actual initial, pending or accepted incident");
                var priorCandidate = candidates[0];
                c.That(!foreign.CopyCandidates(new PlayerId(0), "pressure.a-leak", candidates) && candidates.Count == 0,
                    "healthy foreign candidates refuse and clear retained private rows");
                c.That(observer.CopyCandidates(new PlayerId(0), "pressure.a-leak", candidates), "exact observer retains deliberate native candidate access");
                c.Equal(before, game.Hash(), "healthy cached pressure/candidate reads remain pure");
                int previousCalls = calls; long at = game.Tick;
                // Negative extension-protocol probe: a real registered view factory throws through the original engine boundary.
                c.Throws<InvalidOperationException>(() => game.Seat(0).View.Feature<ProbeView>(), "actual declared callback latches the first session fault");
                var first = game.Fault;
                c.That(first != null && first.Phase == "observation.provider" && first.Message.Contains(marker), "original callback cause and phase remain observable");
                foreach (var view in new[] { own, foreign, observer, new ScenarioPressureView(game.World, new PlayerId(-2)),
                    new ScenarioPressureView(game.World, new PlayerId(99)) })
                {
                    var rows = new List<ScenarioPressureSnapshot> { retained };
                    c.Throws<InvalidOperationException>(() => view.CopyPressure(rows), "cached owned/foreign/observer/invalid pressure facts reject terminal state");
                    c.Equal(0, rows.Count, "terminal pressure copy clears caller's retained rows");
                    foreach (var company in new[] { new PlayerId(0), new PlayerId(1), new PlayerId(99), PlayerId.None })
                    foreach (string source in new[] { "pressure.a-leak", "pressure.worker", "missing", null })
                    {
                        candidates.Clear(); candidates.Add(priorCandidate);
                        c.Throws<InvalidOperationException>(() => view.CopyCandidates(company, source, candidates),
                            "terminal cutoff precedes missing, private, unenrolled or incompatible candidate shortcuts");
                        c.Equal(0, candidates.Count, "terminal candidate query publishes no retained private prefix");
                    }
                }
                c.Throws<InvalidOperationException>(() => game.Seat(0).View.Feature<ProbeView>(), "failed factory cannot run again through another request");
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted pressure cannot checkpoint as healthy");
                c.Throws<InvalidOperationException>(() => game.Hash(), "faulted pressure cannot publish healthy hash");
                c.Equal(0, game.Update(), "faulted native pressure cannot advance, refill or dispatch");
                c.That(ReferenceEquals(first, game.Fault) && game.Tick == at && calls == previousCalls + 1,
                    "cached queries preserve original fault/time and invoke no failing callback again");
                c.Equal(untouched, idle.Hash(), "faulted pressure views leave an interleaved idle session unchanged");
                c.That(Facts(idle).PendingSequence == 0 && Facts(healthy).Accepted == 1, "independent live native pressure views remain healthy");
            }
        }
        private static void InvalidContent(Check c)
        {
            foreach (string mode in new[] { "capacity", "initial", "cost", "source", "duplicate", "cadence", "target", "company", "profile" })
            {
                JsonValue json = Content.Json.Parse(FrameworkProofContent.Read("scenario-pressure.json"));
                JsonValue profile = json.Get("scenario_pressure_profiles")[0];
                if (mode == "capacity") profile.Set("capacity", JsonValue.Number("0"));
                if (mode == "initial") profile.Set("initial", JsonValue.Number("2"));
                if (mode == "cost") json.Get("incident_pressure_sources")[0].Set("cost", JsonValue.Number("2"));
                if (mode == "source") profile.Set("sources", Content.Json.Parse("[\"missing\"]"));
                if (mode == "duplicate") profile.Set("sources", Content.Json.Parse("[\"pressure.a-leak\",\"pressure.a-leak\"]"));
                if (mode == "cadence") profile.Get("cadence").Set("numerator", JsonValue.Number("0"));
                if (mode == "target") json.Get("incident_pressure_sources")[0].Set("entity", JsonValue.Of("source"));
                c.Throws<ContentException>(() => New(Factory(content: json.ToString(), edit: scenario => {
                    var row = scenario.Get("settings").Get(ScenarioPressureModule.ModuleId).Get("companies")[0];
                    if (mode == "company") row.Set("company", JsonValue.Number("8"));
                    if (mode == "profile") row.Set("profile", JsonValue.Of("missing"));
                })), "invalid authored pressure refuses before Initialize: " + mode);
            }
        }
    }
}
