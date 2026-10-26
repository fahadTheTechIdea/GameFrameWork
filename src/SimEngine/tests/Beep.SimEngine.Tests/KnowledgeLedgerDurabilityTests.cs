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
    // Pure authored composition and public player commands/views; no host gameplay or state writes.
    internal static class KnowledgeLedgerDurabilityTests
    {
        internal const string Case = "Knowledge ledger durability: physical learning and policy faults preserve private facts";
        private const string Provider = "sim.knowledge.facts", Reading = "field.stock-reading", Method = "field.method";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool independent, bool half, string phase = null, string mode = null, KnowledgeContentContractsModule module = null)
        {
            var doc = Json.Parse(FrameworkProofContent.Read("knowledge-ledger-durability.json"));
            if (phase != null) { doc.Get("ledger_failures")[0].Set("phase", JsonValue.Of(phase)); doc.Get("ledger_failures")[0].Set("mode", JsonValue.Of(mode)); }
            if (phase == "observation") doc.Get("ledger_subject_probes")[0].Set("id", JsonValue.Of(Method));
            return KnowledgeContentContractTests.Factory(module ?? new KnowledgeContentContractsModule(), independent,
                edit: original => { if (phase == "observation") original.Remove("knowledge_facts"); },
                additionalModules: phase == null ? null : new GameModule[] { new KnowledgeLedgerDurabilityModule() },
                additionalDeclarations: phase == null ? null : new[] { Json.Parse(FrameworkProofContent.Read("knowledge-ledger-durability-module.json")) },
                additionalFiles: phase == null ? null : new Dictionary<string,string> { ["knowledge-ledger-durability.json"] = doc.ToString() },
                scenarioEdit: scenario => { if (phase != null) scenario.Get("modules").Add(JsonValue.Of("proof.knowledge-ledger-durability")); },
                worldEdit: root => { if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30")); });
        }
        private static SimGame New(GameFactory f) => ConstructionPlanTests.NewPoints(f);
        private static EntityId Entity(SimGame g, string name) => g.Seat(0).Snapshot.Entities.Single(e => e.Def == name).Id;
        private static KnowledgeView View(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<KnowledgeView>();
        private static bool Known(SimGame g, string subject, int seat = 0) => View(g, seat).TryRead(new PlayerId(seat), Provider, subject, out _);
        private static KnowledgeObservation Fact(SimGame g, string subject, int seat = 0)
        { if (!View(g, seat).TryRead(new PlayerId(seat), Provider, subject, out var fact)) throw new InvalidOperationException("missing earned fact"); return fact; }
        private static void Step(SimGame g, SimGame twin = null) => PermanentRemovalTests.Step(g, twin);
        private static void Until(SimGame g, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 480 && !done(); i++) Step(g, twin);
            if (!done()) throw new InvalidOperationException(stage + " step=" + g.Clock.StepIndex + " jobs="
                + string.Join(";", PermanentRemovalTests.Jobs(g).Select(x => x.Id + "/" + x.State + "/" + x.Blocker)));
        }
        private static CommandResult Submit(Check c, SimGame g, Command command) => PermanentRemovalTests.Submit(c, g, command);
        private static int Begin(Check c, SimGame g) => checked((int)Submit(c, g, new PostOrderCommand
            { Plan = "field.observe", Target = Entity(g, "source"), Priority = 70 }).Arg0);
        private static List<ActivityNodeView> Nodes(SimGame g, int order)
        { var rows = new List<ActivityNodeView>(); g.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, rows); return rows; }
        private static SimGame Saved(Check c, GameFactory f, SimGame g)
        { var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "original private ledger checkpoint"); return twin; }
        private static void Enable(Check c, SimGame g) => Submit(c, g, new ConfigureWorkPolicyCommand { Worker = Entity(g, "machine"),
            Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
        private static void Learn(Check c, GameFactory f, SimGame g)
        {
            Enable(c, g); int order = Begin(c, g);
            Until(g, null, () => Nodes(g, order).Any(n => n.Progress.WholeTicks > 0), "actual partial material-consuming work");
            var partial = Nodes(g, order).Single(); c.That(partial.Progress.WholeTicks < partial.RequiredTicks, "unfinished timed work");
            c.That(!Known(g, Reading) && !Known(g, Method), "partial work grants no knowledge");
            c.Equal(Fix.FromInt(18), TransformationContentContractTests.Stock(g, "source"), "two actual sample inputs consumed once");
            var twin = Saved(c, f, g); long at = g.Clock.StepIndex; g.Clock.Pause(); twin.Clock.Pause(); Step(g, twin);
            c.Equal(at, g.Clock.StepIndex, "sole paused clock"); g.Clock.Resume(); twin.Clock.Resume();
            Until(g, twin, () => Known(g, Reading) && Known(g, Method), "earned complete knowledge");
            c.That(partial.Progress.WholeTicks < partial.RequiredTicks, "old work facts retained immutable");
        }
        private static void Physical(Check c, bool independent, bool half)
        {
            var f = Factory(independent, half); var g = New(f); var idle = New(f); ulong isolated = idle.Hash();
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "knowledge work needs no Tycoon");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = g.RecordReplay(limits))
            {
                Learn(c, f, g); var fact = Fact(g, Reading); var old = new List<KnowledgeObservation>(); View(g).CopyKnown(old);
                c.That(old.Count == 2 && old.All(x => x.Company == new PlayerId(0) && x.Data.Frozen), "sole immutable own knowledge table");
                c.Equal(18L * Fix.One.Raw, new JsonReader(fact.Data, "actual reading").Long("quantity_raw"), "actual remaining stock reading");
                c.Throws<InvalidOperationException>(() => fact.Data.Set("quantity_raw", JsonValue.Number("999")), "frozen returned payload rejects mutation");
                c.That(!View(g, 1).TryRead(new PlayerId(0), Provider, Reading, out _), "foreign ledger private");
                c.Equal(Code.None, View(g).ReadFreshness(new PlayerId(0), Provider, Reading, out var fresh), "original freshness owner");
                c.That(fresh.IsFresh, "new actual reading fresh"); ulong hash = g.Hash();
                c.Equal(Code.None, View(g).PreviewShare(new PlayerId(1), "field.share", out var outgoing), "shared evaluator admits own earned facts");
                c.Equal(hash, g.Hash(), "shared evaluator pure");
                c.Throws<NotSupportedException>(() => ((IList<KnowledgeSubjectSnapshot>)outgoing.Entries).Clear(), "authored subject projection immutable");
                Submit(c, g, new ShareKnowledgeCommand { Recipient = new PlayerId(1), Policy = "field.share" });
                var received = Fact(g, Reading, 1); c.Equal(fact.ObservedTick, received.ObservedTick, "sharing preserves observation time");
                c.Equal(fact.Data.ToString(), received.Data.ToString(), "sharing preserves actual history");
                var twin = Saved(c, f, g); Until(g, twin, () => {
                    View(g).ReadFreshness(new PlayerId(0), Provider, Reading, out var value); return !value.IsFresh;
                }, "actual freshness expiry");
                c.That(fresh.IsFresh, "retained freshness immutable");
                hash = g.Hash(); c.Equal(Code.None, View(g).PreviewForget("field.forget", out var forgetting), "same owner previews forgetting");
                c.Equal(hash, g.Hash(), "forget evaluation pure");
                Submit(c, g, new ForgetKnowledgeCommand { Policy = "field.forget" });
                c.That(!Known(g, Reading) && Known(g, Method) && Known(g, Reading, 1), "forget own selective reading only");
                c.That(forgetting.Entries.Any(x => x.Observation.HasValue) && old.All(x => x.Data.Frozen), "earlier knowledge projection retained");
                int refresh = Begin(c, g); Until(g, null, () => Nodes(g, refresh).Any(n => n.Progress.WholeTicks > 0), "physical re-observation partial");
                Submit(c, g, new CancelOrderCommand { Order = refresh });
                c.That(!Known(g, Reading), "cancelled unfinished work cannot record");
                c.Equal(Fix.FromInt(16), TransformationContentContractTests.Stock(g, "source"), "spent cancelled inputs not refunded");
                Begin(c, g); twin = Saved(c, f, g); Until(g, twin, () => Known(g, Reading), "physical rediscovery");
                c.That(Fact(g, Reading).ObservedTick > fact.ObservedTick, "new earned observation uses current game time");
                c.Equal(Fix.FromInt(14), TransformationContentContractTests.Stock(g, "source"), "all three physical sample costs once");
                c.Equal(received.ObservedTick, Fact(g, Reading, 1).ObservedTick, "recipient history not remotely refreshed");
                Saved(c, f, g); c.Equal(isolated, idle.Hash(), "interleaved ledger isolated");
                c.That(new ReplayVerifier().Verify(f, recorder.Finish(), limits).Success, "ordinary work/share/forget/cancel stream replays");
                Console.WriteLine("ledger physical independent=" + independent + " half=" + half + " final=" + g.Clock.StepIndex);
            }
        }
        private static void FaultedQueries(Check c, KnowledgeView view, KnowledgeObservation old)
        {
            foreach (var company in new[] { new PlayerId(0), new PlayerId(1), new PlayerId(int.MaxValue) })
                foreach (var subject in new[] { Reading, Method, "missing", "" })
                {
                    KnowledgeObservation observed = old; KnowledgeFreshnessSnapshot freshness = null;
                    c.Throws<InvalidOperationException>(() => view.TryRead(company, Provider, subject, out observed), "cached missing/private subject rejects fault");
                    c.That(observed.Data == null, "faulted fact output empty");
                    c.Throws<InvalidOperationException>(() => view.ReadFreshness(company, Provider, subject, out freshness), "cached freshness rejects before shortcut");
                    c.That(freshness == null, "faulted freshness output empty");
                }
            foreach (var policy in new[] { "field.share", "ledger.share", "missing", "" })
                foreach (var recipient in new[] { new PlayerId(0), new PlayerId(1), new PlayerId(int.MaxValue) })
                {
                    KnowledgeShareSnapshot shared = null;
                    c.Throws<InvalidOperationException>(() => view.PreviewShare(recipient, policy, out shared), "cached share preview rejects before shortcut");
                    c.That(shared == null, "no partial sharing projection");
                }
            foreach (var policy in new[] { "field.forget", "ledger.forget", "missing", "" })
            {
                KnowledgeForgetSnapshot removed = null;
                c.Throws<InvalidOperationException>(() => view.PreviewForget(policy, out removed), "cached forgetting preview rejects before shortcut");
                c.That(removed == null, "no partial forgetting projection");
            }
            var rows = new List<KnowledgeObservation> { old }; c.Throws<InvalidOperationException>(() => view.CopyKnown(rows), "cached seeded table rejects");
            c.Equal(0, rows.Count, "faulted table clears destination");
        }
        private static void Failure(Check c, string phase, string mode, bool save)
        {
            var f = Factory(true, false, phase, mode); var g = New(f); var idle = New(f);
            Learn(c, f, g); var old = Fact(g, Reading); var cached = View(g); var foreign = View(g, 1); ulong isolated = idle.Hash(); Saved(c, f, g);
            while (g.Clock.StepIndex < 100) { g.Update(); if (g.Fault != null) throw new InvalidOperationException("premature fault " + g.Fault); }
            Exception error = null; KnowledgeShareSnapshot shared = null; KnowledgeForgetSnapshot forgotten = null;
            try {
                if (save) g.Save();
                else if (phase == "forget") cached.PreviewForget("ledger.forget", out forgotten);
                else cached.PreviewShare(new PlayerId(1), "ledger.share", out shared);
            } catch (Exception actual) { error = actual; }
            c.That(error is InvalidOperationException && g.Fault != null, "actual " + phase + "/" + mode + " callback terminal");
            if (error == null || g.Fault == null) throw new InvalidOperationException("missing callback fault " + phase + "/" + mode);
            if (mode != "clock") c.Equal("proof.knowledge-ledger-first-error", error.Message, "first provider callback not masked");
            c.That(shared == null && forgotten == null, "failed preview publishes no prefix");
            var first = g.Fault; FaultedQueries(c, cached, old); FaultedQueries(c, foreign, old);
            c.Throws<InvalidOperationException>(() => g.Save(), "faulted ledger cannot save");
            c.Throws<InvalidOperationException>(() => g.Hash(), "faulted ledger cannot hash healthy state");
            c.Equal(0, g.Update(), "original terminal update"); c.That(ReferenceEquals(first, g.Fault), "first fault remains authoritative");
            c.Equal(isolated, idle.Hash(), "failing ledger session isolated"); Step(idle);
            c.That(old.Data.Frozen && old.Data.Get("quantity_raw").Text == (18L * Fix.One.Raw).ToString(), "old private data stays immutable");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current ledger identity"); c.Equal(154, EngineInfo.SaveFormat, "original saved state schema");
            foreach (bool independent in new[] { false, true }) foreach (bool half in new[] { false, true }) Physical(c, independent, half);
            foreach (string phase in new[] { "share", "forget", "observation" })
                foreach (string mode in new[] { "throw", "clock", "clock-throw" }) Failure(c, phase, mode, false);
            Failure(c, "observation", "clock-throw", true);
            var module = new KnowledgeContentContractsModule(); Exception error = null;
            try { New(Factory(true, false, "composition", "clock-throw", module)); } catch (Exception actual) { error = actual; }
            c.That(error is InvalidOperationException && error.Message == "proof.knowledge-ledger-first-error", "composition first callback preserved");
            c.Equal(0, module.Initialize.Initializations, "invalid policy no Initialize");
        }
    }
}
