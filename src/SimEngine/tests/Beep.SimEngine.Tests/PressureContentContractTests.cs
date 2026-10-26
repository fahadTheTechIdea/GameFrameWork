using System;
using System.Collections.Generic;
using System.IO;
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
    internal static class PressureContentContractTests
    {
        internal const string Case = "Pressure content contracts: independent JSON pacing and saved physical incident response";
        internal const string CallbackCase = "Pressure callbacks: original errors malformed candidate cutoff and complete physical replay";
        internal const string SaveIntegrityCase = "Pressure saves: checksum-valid corruption rejects impossible budgets chronology and pending references";
        private static readonly Code Leak = new Code("pressure.leak");
        private static readonly ReplayLimits Limits = new ReplayLimits(4 * 1024 * 1024, 3000);
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add(CallbackCase, c => { Run(c); CallbackFaults(c); });
            TestRunner.Add(SaveIntegrityCase, c => { Run(c); SaveIntegrity(c); });
        }
        private static GameFactory Factory(InitializeProbe initialize, out PressureContractModule proof,
            string selected = "pressure.incidents", Action<JsonValue> edit = null, bool enabled = true, Action<World> composed = null)
        {
            proof = new PressureContractModule(initialize, composed);
            var content = Json.Parse(FrameworkProofContent.Read("pressure-contracts.json")); edit?.Invoke(content);
            return ConstructionPlanTests.Points(extraModules: new GameModule[] { proof },
                extraDeclarations: "[{\"id\":\"sim.scenario-pressure\",\"version\":\"1.0.0\",\"documents\":[\"pressure-contracts.json\"]}]",
                extraDocuments: new Dictionary<string, string> { { "pressure-contracts.json", content.ToString() } },
                additionalDocuments: new Dictionary<string, string> { { "pressure-response.json", FrameworkProofContent.Read("pressure-response.json") } },
                startupProfiles: new[] { "pressure-response.start" }, scenarioEdit: scenario =>
                {
                    if (enabled) scenario.Get("modules").Add(JsonValue.Of(ScenarioPressureModule.ModuleId));
                    scenario.Get("modules").Add(JsonValue.Of(IncidentResponseModule.ModuleId));
                    scenario.Get("settings").Set(ScenarioPressureModule.ModuleId,
                        Json.Parse("{\"companies\":[{\"company\":0,\"profile\":" + (selected == null ? "null" : "\"" + selected + "\"") + "}]}"));
                });
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static ScenarioPressureSnapshot Facts(SimGame game, int seat = 0)
        {
            var result = new List<ScenarioPressureSnapshot>(); game.Seat(seat).View.Feature<ScenarioPressureView>().CopyPressure(result);
            var own = result.Single(); if (own.Company != new PlayerId(seat)) throw new InvalidOperationException("foreign pressure fact"); return own;
        }
        private static EntityId Worker(SimGame game) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "machine").Id;
        private static EntityId Target(SimGame game) => game.Seat(0).Snapshot.Entities.Where(e => e.Def == "pressure.machine").OrderBy(e => e.Id).First().Id;
        private static bool Active(SimGame game, EntityId target)
        { game.Seat(0).View.Feature<IncidentsView>().TryGetIncident(target, Leak, out IncidentSnapshot fact); return fact.Active; }
        private static Fix Stock(SimGame game, EntityId target)
        { var result = new List<Facet>(); game.Seat(0).View.Inspect(target, result); return result.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (a, b) => a + b.Value); }
        private static List<OrderView> Orders(SimGame game)
        { var result = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(result); return result; }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("pressure step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 400 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " pressure=" + Facts(game).Blocker +
                " orders=" + string.Join(";", Orders(game).Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static void Run(Check c)
        {
            var initialize = new InitializeProbe(); var factory = Factory(initialize, out var proof); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current build identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current original pressure/physical codecs");
            c.That(proof.IndependentAlias, "actual profile/source contracts without either native base");
            c.Equal(1, initialize.Initializations, "validated Initialize once");
            var recorder = game.RecordReplay(Limits); var target = Target(game); var retained = Facts(game);
            c.That(retained.Profile == "pressure.incidents" && retained.Available == Fix.One && retained.PendingSequence == 0, "explicit independent profile initial points");
            var facts = new List<ScenarioPressureSnapshot>(); ulong hash = game.Hash();
            game.Seat(1).View.Feature<ScenarioPressureView>().CopyPressure(facts);
            c.Equal(0, facts.Count, "company-private pressure configuration");
            var candidates = new List<PressureCandidateSnapshot>();
            c.That(game.Seat(0).View.Feature<ScenarioPressureView>().CopyCandidates(new PlayerId(0), "pressure.a-leak", candidates) && candidates.Count == 2, "original native candidates through independent profile");
            c.That(candidates.All(p => p.Ready) && candidates[0].Entity == target, "stable actual owned target ordering");
            c.Throws<NotSupportedException>(() => ((IList<ExecutionGateConditionSnapshot>)candidates[0].Conditions).Clear(), "detached candidate facts immutable");
            c.That(!game.Seat(1).View.Feature<ScenarioPressureView>().CopyCandidates(new PlayerId(0), "pressure.a-leak", candidates) && candidates.Count == 0, "no foreign candidates");
            c.Equal(hash, game.Hash(), "contract views pure");
            Step(game); var pending = Facts(game);
            c.That(pending.PendingSequence > 0 && pending.PendingSource == "pressure.a-leak" && pending.Reserved == Fix.One && pending.Available == Fix.Zero, "stable priority/identity reservation");
            c.That(!Active(game, target) && pending.Spent == Fix.Zero && Orders(game).Count == 0, "next-update command not instant incident or work");
            c.That(retained.PendingSequence == 0 && retained.Available == Fix.One, "retained initial facts unchanged");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending source/cost/inbox restored");
            game.Clock.Pause(); twin.Clock.Pause(); long at = game.Clock.StepIndex; hash = game.Hash();
            for (int i = 0; i < 3; i++) Step(game, twin);
            c.That(game.Clock.StepIndex == at && game.Hash() == hash && Facts(game).PendingSequence == pending.PendingSequence, "one clock freezes pending pressure");
            game.Clock.Resume(); twin.Clock.Resume(); Step(game, twin);
            c.That(Active(game, target) && Facts(game).Accepted == 1 && Facts(game).Spent == Fix.One && Facts(game).Reserved == Fix.Zero, "actual native accepted command charges once");
            c.That(Facts(game).CooldownUntil == 61 && Facts(game).Blocker == PressureCodes.Cooldown, "authored cooldown from actual acceptance");
            c.That(Orders(game).Any(o => o.Target == target && o.Plan == "activity:pressure.response"), "original authored response owner");
            var settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") },
                null, true, true, WorkAssignmentMode.All);
            c.Equal(Code.None, game.Seat(0).Preview(new ConfigureWorkPolicyCommand { Worker = Worker(game), Settings = settings }), "normal worker preview");
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = Worker(game), Settings = settings }).Accepted, "normal worker policy command");
            twin = factory.Load(game.Save());
            Until(game, twin, () => Orders(game).Any(o => o.Target == target && o.CurrentProgress.WholeTicks > 0 && o.NodesDone == 1), "physical repair");
            c.Equal(Fix.FromInt(20), ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == Worker(game)).Spatial).Position, "worker physically reaches incident");
            c.Equal(Fix.FromInt(12), Stock(game, target), "repair consumes eight actual authored materials");
            c.That(Active(game, target) && game.Clock.StepIndex > 6, "elapsed travel and work before completion");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "active response save");
            Until(game, twin, () => !Active(game, target), "physical completion");
            c.That(Facts(game).Accepted == 1 && Stock(game, target) == Fix.FromInt(12), "response completion grants no free resources");
            while (game.Clock.StepIndex < 62) Step(game, twin);
            c.That(Facts(game).Available == Fix.FromRatio(3, 4) && Facts(game).Blocker == PressureCodes.Budget && Facts(game).PendingSequence == 0, "original exact-period refill and unaffordable wait");
            while (game.Clock.StepIndex < 81) Step(game, twin);
            c.That(Facts(game).Reserved == Fix.One && Facts(game).Spent == Fix.One, "replenished second request is reserved not spent");
            twin = factory.Load(game.Save()); Step(game, twin);
            c.That(Facts(game).Accepted == 2 && Facts(game).Spent == Fix.FromInt(2), "saved second normal command charged exactly once");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "complete initial policy/pressure/physical replay matches final live hash");
            c.Equal(game.Tick - recorder.InitialStep, (long)recorder.CheckpointCount, "all advancing native pressure/physical updates recorded");
            Console.WriteLine("pressure contract replay: initial=" + recorder.InitialStep + ";final=" + game.Tick
                + ";commands=" + recorder.CommandCount + ";checkpoints=" + recorder.CheckpointCount
                + ";expected-hash=" + game.Hash().ToString("X16") + ";actual-hash=" + replay.ActualHash.ToString("X16"));
            var fresh = New(factory); c.That(Facts(fresh).Decisions == 0 && !Active(fresh, Target(fresh)) && Stock(fresh, Target(fresh)) == Fix.FromInt(20), "interleaved sessions isolated");
            PublicSource(c); Invalid(c);
            var absent = New(Factory(new InitializeProbe(), out _, enabled: false));
            c.That(absent.Seat(0).View.Feature<ScenarioPressureView>() == null, "unselected pressure module creates no owner");
        }
        private static void PublicSource(Check c)
        {
            var factory = Factory(new InitializeProbe(), out _, "pressure.policy"); var game = New(factory);
            for (int i = 0; i < 5; i++) Step(game);
            c.That(Facts(game).PendingSequence == 0 && Facts(game).Blocker.Id == "proof.pressure.grace", "native profile resolves independent source with authored clock grace");
            Step(game); var twin = factory.Load(game.Save());
            c.That(Facts(game).Reserved == Fix.FromRatio(1, 2) && Facts(game).Available == Fix.FromRatio(1, 2), "independent source actual configured cost");
            Step(game, twin);
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().TryReadPolicy(Worker(game), out var policy), "native worker policy facts");
            c.That(policy.Settings.Enabled && Facts(game).Spent == Fix.FromRatio(1, 2) && Facts(game).Accepted == 1, "independent source normal policy command");
            for (int i = 0; i < 25; i++) Step(game, twin);
            c.That(Facts(game).Accepted == 1 && Facts(game).Available == Fix.FromRatio(1, 2) && Facts(game).PendingSequence == 0, "explicit zero refill and no target do not invent actions");
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "capacity", "initial", "refill", "cadence", "period", "cooldown", "sources", "duplicate", "missing-source", "wrong-source", "source-cost", "source-priority", "over-capacity", "overflow", "unused-overflow", "source-id" })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(initialize, out _, edit: json =>
                {
                    var profile = json.Get("independent_pressure_profiles")[0]; var source = json.Get("worker_policy_pressure_sources")[0];
                    if (mode == "capacity") profile.Set("capacity", JsonValue.Number("0"));
                    if (mode == "initial") profile.Set("initial", JsonValue.Number("2"));
                    if (mode == "refill") profile.Remove("refill");
                    if (mode == "cadence") profile.Get("cadence").Set("numerator", JsonValue.Number("0"));
                    if (mode == "period") profile.Remove("refill_period");
                    if (mode == "cooldown") profile.Get("cooldown").Set("denominator", JsonValue.Number("0"));
                    if (mode == "sources") profile.Remove("sources");
                    if (mode == "duplicate") profile.Set("sources", Json.Parse("[\"pressure.a-leak\",\"pressure.a-leak\"]"));
                    if (mode == "missing-source") profile.Set("sources", Json.Parse("[\"missing.source\"]"));
                    if (mode == "wrong-source") profile.Set("sources", Json.Parse("[\"pressure.policy\"]"));
                    if (mode == "source-cost") source.Set("cost", JsonValue.Number("0"));
                    if (mode == "source-priority") source.Remove("priority");
                    if (mode == "over-capacity") json.Get("incident_pressure_sources")[0].Set("cost", JsonValue.Number("2"));
                    if (mode == "overflow" || mode == "unused-overflow")
                    {
                        var duration = json.Get("independent_pressure_profiles")[mode == "overflow" ? 0 : 1].Get("cadence");
                        duration.Set("numerator", JsonValue.Number("9223372036854775807")); duration.Set("unit", JsonValue.Of("hour"));
                    }
                    if (mode == "source-id") profile.Set("sources", Json.Parse("[\" bad.source\"]"));
                })), "invalid actual authored pressure: " + mode);
                c.Equal(0, initialize.Initializations, "invalid pressure refuses before Initialize: " + mode);
            }
            foreach (string id in new[] { "missing.profile", "pressure.worker", null })
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(initialize, out _, id)), "missing/null/wrong-kind selected profile");
                c.Equal(0, initialize.Initializations, "bad binding before Initialize");
            }
        }
        private static GameFactory CallbackFactory(InitializeProbe initialize, string phase, string fault, Action<World> composed = null)
            => Factory(initialize, out _, edit: json => {
                var source = Json.Parse("{\"id\":\"pressure.callback\",\"cost\":1,\"priority\":100,\"entity\":\"pressure.machine\"}");
                source.Set("phase", JsonValue.Of(phase)); source.Set("fault", JsonValue.Of(fault));
                var probes = JsonValue.NewArray(); probes.Add(source); json.Set("pressure_callback_probes", probes);
                json.Get("independent_pressure_profiles")[0].Set("sources", Json.Parse("[\"pressure.callback\"]"));
            }, composed: composed);
        private static void CallbackFaults(Check c)
        {
            foreach (string fault in new[] { "throw", "pause-throw", "pause" })
            {
                World failed = null; Exception thrown = null; var initialize = new InitializeProbe();
                var factory = CallbackFactory(initialize, "validate", fault, world => failed = world);
                c.Throws<InvalidOperationException>(() => { try { New(factory); } catch (InvalidOperationException error) { thrown = error; throw; } },
                    "actual selected ValidateBinding callback rejects before Initialize");
                c.Equal(0, initialize.Initializations, "failed pressure binding cannot initialize gameplay");
                var first = failed?.Fault;
                c.That(first != null && first.Phase == "observation.provider", "validation uses original observation first-fault owner");
                if (fault != "pause")
                    c.That(first != null && first.Message.Contains("proof.pressure.callback.validate." + fault)
                        && thrown.Message.Contains("proof.pressure.callback.validate." + fault), "original thrown cause wins over later mutation verification");
                c.Throws<InvalidOperationException>(() => failed.RequireHealthyUpdate(), "failed binding world cannot appear healthy after exception");
                c.That(ReferenceEquals(first, failed.Fault) && failed.Clock.StepIndex == 0, "validation failure retains first cause and earns no time");
            }
            foreach (string phase in new[] { "evaluate", "candidates" })
            foreach (string fault in phase == "evaluate" ? new[] { "throw" } : new[] { "throw", "prefix-throw", "null", "duplicate", "reverse" })
            foreach (bool restored in new[] { false, true })
            {
                var factory = CallbackFactory(new InitializeProbe(), phase, fault); var game = New(factory);
                if (restored) game = factory.Load(game.Save());
                var idle = New(factory); ulong untouched = idle.Hash(); var prior = Facts(game);
                var own = game.Seat(0).View.Feature<ScenarioPressureView>(); var foreign = game.Seat(1).View.Feature<ScenarioPressureView>();
                var observer = game.Observer.Feature<ScenarioPressureView>();
                var retained = new PressureCandidateSnapshot(Target(game), Array.Empty<ExecutionGateConditionSnapshot>());
                var rows = new List<PressureCandidateSnapshot> { retained }; ulong before = game.Hash();
                c.That(!foreign.CopyCandidates(new PlayerId(0), "pressure.callback", rows) && rows.Count == 0, "private denial invokes no armed candidate provider");
                c.Equal(before, game.Hash(), "healthy private refusal preserves actual world");
                if (phase == "candidates")
                {
                    rows.Add(retained);
                    c.Throws<InvalidOperationException>(() => own.CopyCandidates(new PlayerId(0), "pressure.callback", rows), "actual selected candidate callback/result must fail");
                    c.Equal(0, rows.Count, "failed callback or malformed local facts publish no private prefix");
                }
                else c.Equal(0, game.Update(), "real Update stops when selected Evaluate throws before dispatch");
                var first = game.Fault;
                c.That(first != null && first.Phase == "observation.provider", "callback or result validation latches the original observation fault");
                string cause = fault.Contains("throw") ? "proof.pressure.callback." + phase + "." + fault : "unordered/hidden/foreign";
                c.That(first != null && first.Message.Contains(cause), "actual first callback or malformed-result cause remains diagnostic");
                foreach (var view in new[] { own, foreign, observer })
                {
                    var pressure = new List<ScenarioPressureSnapshot> { prior };
                    c.Throws<InvalidOperationException>(() => view.CopyPressure(pressure), "cached pacing diagnostics reject after provider failure");
                    c.Equal(0, pressure.Count, "failed pacing query clears retained facts");
                    foreach (string source in new[] { "pressure.callback", "missing" })
                    {
                        rows.Clear(); rows.Add(retained);
                        c.Throws<InvalidOperationException>(() => view.CopyCandidates(new PlayerId(0), source, rows), "cached private/missing candidate shortcuts cannot escape callback failure");
                        c.Equal(0, rows.Count, "failed cached query retains no candidate prefix");
                    }
                }
                c.Throws<InvalidOperationException>(() => game.Save(), "failed provider session cannot save as healthy");
                c.Throws<InvalidOperationException>(() => game.Hash(), "failed provider session cannot publish healthy hash");
                c.Equal(0, game.Update(), "failed candidate observation cannot earn time or make pacing decisions");
                c.That(first != null && ReferenceEquals(first, game.Fault) && game.Tick == 0, "first fault/time survive every subsequent query and Update");
                c.That(prior.Decisions == 0 && prior.Available == Fix.One && retained.Entity == Target(idle), "held detached facts remain unchanged");
                c.Equal(untouched, idle.Hash(), "callback failure leaves independent session unchanged");
            }
        }
        // Privileged negative SAVE-byte instrumentation only, not a second gameplay codec or host state model.
        private static Dictionary<string, (int Offset, int Length)> PressureWire(Check c, SimGame game, byte[] save, int seat = 0)
        {
            var owner = (GameSystem)game.World.Services.Get<IScenarioPressure>(); byte[] payload;
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); writer.Section(owner.Id); owner.Sync(writer); writer.Flush(); payload = stream.ToArray();
            }
            SaveGame.ReadHeader(save, out var body); int start = -1, matches = 0;
            for (int i = body.Position; i <= save.Length - 8 - payload.Length; i++)
            {
                int n = 0; while (n < payload.Length && save[i + n] == payload[n]) n++;
                if (n == payload.Length) { start = i; matches++; }
            }
            c.Equal(1, matches, "actual original pressure codec plus section has exactly one full-save occurrence");
            if (matches != 1) throw new InvalidOperationException("ambiguous pressure payload offset");
            Dictionary<string, (int Offset, int Length)> fields = null, selected = null;
            var reader = new StateReader(save, start, payload.Length); reader.Section(owner.Id);
            int countOffset = reader.Position, count = reader.Count(0), countLength = reader.Position - countOffset;
            for (int row = 0; row < count; row++)
            {
                fields = new Dictionary<string, (int Offset, int Length)>();
                var company = PlayerId.None; Mark("company", () => reader.Player(ref company));
                string text = null; Mark("profile", () => reader.String(ref text));
                Fix amount = Fix.Zero;
                foreach (string name in new[] { "available", "reserved", "spent" }) Mark(name, () => reader.Fix(ref amount));
                long time = 0;
                foreach (string name in new[] { "decisions", "accepted", "refused", "last", "next", "refill", "cooldown", "settled" }) Mark(name, () => reader.Long(ref time));
                int ticket = 0; Mark("pending", () => reader.Int(ref ticket)); Mark("issued", () => reader.Long(ref time));
                foreach (string name in new[] { "source", "command", "blocker" }) Mark(name, () => reader.String(ref text));
                if (company == new PlayerId(seat))
                {
                    if (selected != null) throw new InvalidOperationException("duplicate selected pressure binding in actual native payload");
                    selected = fields;
                }
            }
            c.That(reader.AtEnd, "negative field offsets cover exactly the native payload, not guessed whole-save offsets");
            if (!reader.AtEnd) throw new InvalidOperationException("pressure fixture schema differs");
            if (selected == null) throw new InvalidOperationException("selected company is absent from actual pressure payload");
            selected.Add("count", (countOffset, countLength)); return selected;
            void Mark(string name, Action read) { int offset = reader.Position; read(); fields.Add(name, (offset, reader.Position - offset)); }
        }
        private static byte[] ChangePressure(byte[] save, (int Offset, int Length) field, Action<StateWriter> change)
        {
            using (var stream = new MemoryStream())
            {
                stream.Write(save, 0, field.Offset); var writer = new StateWriter(stream); change(writer); writer.Flush();
                int tail = field.Offset + field.Length; stream.Write(save, tail, save.Length - 8 - tail);
                // Independently reseal the adversarial fixture so the native reader must validate pressure semantics.
                ulong checksum = 14695981039346656037UL;
                unchecked { for (int i = 0; i < stream.Length; i++) { checksum ^= stream.GetBuffer()[i]; checksum *= 1099511628211UL; } }
                writer.ULong(ref checksum); writer.Flush(); return stream.ToArray();
            }
        }
        private static void SaveIntegrity(Check c)
        {
            int mutations = 0, rejected = 0;
            foreach (var sample in new[] { ("initial", 0), ("pending", 1), ("accepted", 2), ("refilled", 21), ("cadenced", 16), ("mixed-cost", 2) })
            {
                var factory = Factory(new InitializeProbe(), out _, edit: json => {
                    var profile = json.Get("independent_pressure_profiles")[0];
                    if (sample.Item1 == "cadenced") { profile.Set("initial", JsonValue.Number("0")); profile.Get("cadence").Set("numerator", JsonValue.Number("5")); }
                    if (sample.Item1 == "mixed-cost")
                        foreach (var source in Enumerable.Range(0, json.Get("incident_pressure_sources").Count).Select(i => json.Get("incident_pressure_sources")[i]))
                            source.Set("cost", JsonValue.Number(source.Get("id").Text == "pressure.a-leak" ? "0.25" : "0.75"));
                });
                var game = New(factory); var idle = New(factory); ulong untouched = idle.Hash();
                for (int i = 0; i < sample.Item2; i++) Step(game);
                var facts = Facts(game); var save = game.Save(); ulong original = game.Hash(); var fields = PressureWire(c, game, save);
                c.Equal(original, factory.Load(save).Hash(), "actual initial/pending/accepted/refilled/cadenced/mixed-cost save restores");
                if (sample.Item1 == "cadenced") c.That(facts.Decisions == 4 && facts.LastDecision == 15, "real five-minute cadence earns exactly four decisions by minute16");
                if (sample.Item1 == "mixed-cost") c.Equal(Fix.FromRatio(1, 4), facts.Spent, "real stable source choice spends authored quarter cost, not capacity");
                Refuse("count", w => { int value = 2; w.Int(ref value); });
                Refuse("company", w => { var value = new PlayerId(1); w.Player(ref value); });
                Refuse("profile", w => { string value = "pressure.policy"; w.String(ref value); });
                foreach (string field in new[] { "available", "reserved", "spent" }) Refuse(field, w => { var value = Fix.FromInt(-1); w.Fix(ref value); });
                foreach (string field in new[] { "decisions", "accepted", "refused", "next", "refill", "cooldown" }) Refuse(field, w => { long value = -1; w.Long(ref value); });
                Refuse("pending", w => { int value = -1; w.Int(ref value); });
                Refuse("blocker", w => { string value = "invalid blocker"; w.String(ref value); });
                if (sample.Item1 == "pending")
                {
                    Refuse("pending", w => { int value = facts.PendingSequence + 1; w.Int(ref value); });
                    Refuse("source", w => { string value = "missing.source"; w.String(ref value); });
                    Refuse("command", w => { string value = "sim.configure-work-policy"; w.String(ref value); });
                    Refuse("issued", w => { long value = 1; w.Long(ref value); });
                    Refuse("reserved", w => { var value = Fix.FromRatio(1, 2); w.Fix(ref value); });
                }
                if (sample.Item1 == "accepted")
                {
                    Refuse("cooldown", w => { long value = long.MaxValue; w.Long(ref value); });
                    Refuse("cooldown", w => { long value = 60; w.Long(ref value); });
                    Refuse("settled", w => { long value = 0; w.Long(ref value); });
                    Refuse("available", w => { var value = Fix.One; w.Fix(ref value); });
                    Refuse("spent", w => { var value = Fix.FromRatio(1, 2); w.Fix(ref value); });
                }
                if (sample.Item1 == "refilled") Refuse("refill", w => { long value = 0; w.Long(ref value); });
                if (sample.Item1 == "cadenced") Refuse("decisions", w => { long value = 5; w.Long(ref value); });
                if (sample.Item1 == "mixed-cost")
                {
                    // Coupled valid-range edits preserve total funding: only authored cost reachability can reject them.
                    foreach (var spent in new[] { Fix.FromRatio(1, 8), Fix.FromRatio(1, 2), Fix.FromRatio(7, 8) })
                    {
                        var altered = ChangePressure(save, fields["spent"], w => { var value = spent; w.Fix(ref value); });
                        altered = ChangePressure(altered, fields["available"], w => { var value = Fix.One - spent; w.Fix(ref value); });
                        Reject(altered, "mixed-cost coupled spend/funding");
                    }
                }
                c.Equal(original, game.Hash(), "all failed load attempts leave actual source game unchanged");
                c.Equal(untouched, idle.Hash(), "all malformed loads leave independent idle session unchanged");
                var twin = factory.Load(save); Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "valid save still continues through the actual native pending/settled state");
                void Refuse(string field, Action<StateWriter> change) => Reject(ChangePressure(save, fields[field], change), sample.Item1 + "/" + field);
                void Reject(byte[] altered, string name)
                {
                    mutations++; var header = factory.Describe(altered);
                    c.That(header.Build == EngineInfo.Build && header.Tick == sample.Item2, "modified save passes real checksum/current header before pressure validation: " + name);
                    CorruptStateException refusal = null;
                    try { factory.Load(altered); } catch (CorruptStateException error) { refusal = error; }
                    c.That(refusal != null, "checksum-valid impossible pressure state refuses: " + name);
                    c.That(refusal != null && refusal.Message.IndexOf("pressure", StringComparison.OrdinalIgnoreCase) >= 0,
                        "actual pressure owner, not unrelated file damage, refuses: " + name);
                    if (refusal != null) rejected++;
                }
            }
            Console.WriteLine("pressure save integrity: valid-stages=6;checksum-valid-mutations=" + mutations + ";pressure-refusals=" + rejected);
        }
        internal static void MultiCompanySaveIntegrity(Check c, GameFactory factory)
        {
            // The generalized original locator must still qualify its existing single-company fixtures.
            SaveIntegrity(c);
            int mutations = 0, rejected = 0, orphanCases = 0;
            foreach (int age in new[] { 0, 1, 2, 21, 81 })
            {
                var game = ConstructionPlanTests.NewPoints(factory, ResourceFlowsModule.ModuleId);
                var idle = ConstructionPlanTests.NewPoints(factory, ResourceFlowsModule.ModuleId); ulong untouched = idle.Hash();
                for (int i = 0; i < age; i++) Step(game);
                var save = game.Save(); ulong original = game.Hash(); var first = Facts(game); var second = Facts(game, 1);
                var firstFields = PressureWire(c, game, save); var fields = PressureWire(c, game, save, 1);
                c.That(firstFields["company"].Offset < fields["company"].Offset, "negative edits target the later actual company, after an untouched valid first record");
                c.Equal(original, factory.Load(save).Hash(), "actual two-company pending/settled/refilled save remains valid");
                Refuse("company", w => { var value = new PlayerId(0); w.Player(ref value); });
                Refuse("profile", w => { string value = first.Profile; w.String(ref value); });
                Refuse("available", w => { var value = Fix.FromInt(2); w.Fix(ref value); });
                Refuse("spent", w => { var value = Fix.FromInt(-1); w.Fix(ref value); });
                foreach (string field in new[] { "decisions", "next" }) Refuse(field, w => { long value = -1; w.Long(ref value); });
                Refuse("blocker", w => { string value = "invalid late blocker"; w.String(ref value); });
                if (age == 1)
                {
                    c.That(first.PendingSequence == second.PendingSequence && first.PendingSequence > 0, "real numeric tickets overlap while native company/stream identities remain distinct");
                    Refuse("source", w => { string value = first.PendingSource; w.String(ref value); });
                    Refuse("pending", w => { int value = second.PendingSequence + 1; w.Int(ref value); });
                    Refuse("command", w => { string value = "sim.configure-work-policy"; w.String(ref value); });
                }
                if (age == 2)
                {
                    Refuse("cooldown", w => { long value = first.CooldownUntil; w.Long(ref value); });
                    Refuse("spent", w => { var value = Fix.Zero; w.Fix(ref value); });
                    var stream = new Code(ScenarioPressureModule.ModuleId);
                    c.That(game.World.WasCommandIssued(new PlayerId(1), stream, 1), "read-only negative diagnostics confirm actual company1 ticket1 was issued and consumed");
                    ForgePending(1, second.LastDecision, "consumed own-company ticket cannot reattach", true);
                }
                if (age == 21)
                {
                    c.That(first.Available > second.Available && second.Available.IsZero, "actual zero-refill company has no first-company replenishment");
                    Refuse("available", w => { var value = first.Available; w.Fix(ref value); });
                    Refuse("refill", w => { long value = 0; w.Long(ref value); });
                }
                if (age == 81)
                {
                    var stream = new Code(ScenarioPressureModule.ModuleId);
                    c.That(first.PendingSequence == 2 && second.PendingSequence == 0 &&
                        game.World.WasCommandIssued(new PlayerId(0), stream, first.PendingSequence) &&
                        !game.World.WasCommandIssued(new PlayerId(1), stream, first.PendingSequence),
                        "actual later ticket belongs only to company0, not a guessed unissued global number");
                    ForgePending(first.PendingSequence, first.LastDecision, "other company's issued ticket cannot supply a local reservation", true);
                }
                c.Equal(original, game.Hash(), "all late-row/coupled load refusals preserve actual source state");
                c.Equal(untouched, idle.Hash(), "all failed multi-company loads leave independent session unchanged");
                var twin = factory.Load(save); Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "original valid two-company save still advances identically after rejected corruptions");
                void Refuse(string field, Action<StateWriter> change) => Reject(ChangePressure(save, fields[field], change), "company1/" + age + "/" + field);
                void ForgePending(int ticket, long issued, string name, bool orphan)
                {
                    long cadence = second.NextDecision - second.LastDecision;
                    var changes = new Dictionary<string, Action<StateWriter>> {
                        ["available"] = w => { var value = Fix.Zero; w.Fix(ref value); }, ["reserved"] = w => { var value = Fix.One; w.Fix(ref value); },
                        ["spent"] = w => { var value = Fix.Zero; w.Fix(ref value); }, ["accepted"] = w => { long value = 0; w.Long(ref value); },
                        ["refused"] = w => { long value = 0; w.Long(ref value); }, ["settled"] = w => { long value = -1; w.Long(ref value); },
                        ["cooldown"] = w => { long value = 0; w.Long(ref value); }, ["last"] = w => { long value = issued; w.Long(ref value); },
                        ["next"] = w => { long value = issued + cadence; w.Long(ref value); }, ["issued"] = w => { long value = issued; w.Long(ref value); },
                        ["pending"] = w => { int value = ticket; w.Int(ref value); },
                        ["source"] = w => { string value = "pressure.z-leak"; w.String(ref value); },
                        ["command"] = w => { string value = "sim.raise-incident"; w.String(ref value); },
                        ["blocker"] = w => { string value = PressureCodes.Awaiting.Id; w.String(ref value); }
                    };
                    byte[] altered = save;
                    // Descending original spans keep earlier offsets correct when replacing variable-length strings.
                    foreach (var change in changes.OrderByDescending(change => fields[change.Key].Offset)) altered = ChangePressure(altered, fields[change.Key], change.Value);
                    Reject(altered, name, orphan);
                }
                void Reject(byte[] altered, string name, bool orphan = false)
                {
                    mutations++; var header = factory.Describe(altered);
                    c.That(header.Build == EngineInfo.Build && header.Tick == age, "late-row fixture passes actual current checksum/header: " + name);
                    c.That(altered.Take(fields["company"].Offset).SequenceEqual(save.Take(fields["company"].Offset)), "entire first company/header prefix remains unchanged: " + name);
                    CorruptStateException refusal = null;
                    try { factory.Load(altered); } catch (CorruptStateException error) { refusal = error; }
                    c.That(refusal != null && refusal.Message.IndexOf("pressure", StringComparison.OrdinalIgnoreCase) >= 0, "actual pressure owner rejects later binding: " + name);
                    if (orphan)
                    {
                        orphanCases++; c.That(refusal?.Message == "invalid/orphan pressure command reservation", "locally balanced forged state reaches actual company-scoped reservation correlation: " + name);
                    }
                    if (refusal != null) rejected++;
                }
            }
            Console.WriteLine("multi-company pressure save integrity: valid-stages=5;checksum-valid-late-row-mutations=" + mutations
                + ";pressure-refusals=" + rejected + ";scoped-orphan-cases=" + orphanCases);
        }
    }
}
