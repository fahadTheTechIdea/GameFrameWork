using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static partial class KnowledgeVisibilityTests
    {
        private const string ProviderIdentityCase = "Knowledge provider identity: actual lookup callbacks reject mutation and preserve public first faults";
        // Explicit extension-protocol setup/fault injection, not an instant player discovery path.
        private static void ProviderIdentityObservation(Check c)
        {
            var factory = Factory(); var company = new PlayerId(0);
            foreach (bool loaded in new[] { false, true })
            foreach (string entry in new[] { "freshness", "check", "record", "missing" })
            foreach (string mode in new[] { "clock", "throw", "clock-throw" }) {
                var game = New(factory); var ledger = game.World.Services.Get<ICompanyKnowledge>();
                var subject = game.World.Content.Get<StockKnowledgeSubjectDef>(StockKnowledgeProvider.SubjectId);
                var data = JsonValue.NewObject(); data.Set("resource", JsonValue.Of(subject.Resource.Id));
                data.Set("quantity_raw", JsonValue.Number(Fix.FromInt(20).Raw.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                c.Equal(Code.None, ledger.Record(company, StockKnowledgeProvider.ProviderId, subject.Id, data), "valid raw ledger protocol setup uses the original owner");
                if (loaded) { game = factory.Load(game.Save()); ledger = game.World.Services.Get<ICompanyKnowledge>(); }
                var independent = factory.Load(game.Save()); ulong untouched = independent.Hash();
                var view = game.Seat(0).View.Feature<KnowledgeView>();
                c.Equal(KnowledgeCodes.UnsupportedFreshness, view.ReadFreshness(company, StockKnowledgeProvider.ProviderId, subject.Id, out _), "provider without freshness capability is explicitly unsupported while healthy");
                var actual = game.World.Services.Get<StockKnowledgeProvider>(); int calls = 0;
                var original = new InvalidOperationException("proof.original-knowledge-identity-error");
                actual.ObserveIdentity = () => {
                    calls++; if (calls != 1) return;
                    if (mode != "throw") game.Clock.Pause();
                    if (mode != "clock") throw original;
                };
                string context = entry + "/" + mode + "/loaded=" + loaded;
                c.Equal(Codes.NotFound, game.Seat(1).View.Feature<KnowledgeView>().ReadFreshness(company, StockKnowledgeProvider.ProviderId, subject.Id, out var hidden), "foreign freshness rejects before invoking a private provider: " + context);
                c.That(hidden == null && calls == 0, "private refusal leaks neither provider callbacks nor a snapshot: " + context);
                Exception caught = null; KnowledgeFreshnessSnapshot result = null;
                try {
                    if (entry == "freshness") view.ReadFreshness(company, StockKnowledgeProvider.ProviderId, subject.Id, out result);
                    else if (entry == "check") ledger.CheckRecord(company, StockKnowledgeProvider.ProviderId, subject.Id, data);
                    else if (entry == "record") ledger.Record(company, StockKnowledgeProvider.ProviderId, subject.Id, data);
                    else ledger.CheckRecord(company, "proof.missing-provider", subject.Id, data);
                } catch (Exception error) { caught = error; }
                actual.ObserveIdentity = null;
                c.That(calls > 0, "actual selected or refusing provider lookup executes: " + context);
                c.That(caught is InvalidOperationException && game.Fault != null, "identity callback cannot mutate or throw and return a healthy lookup: " + context);
                c.That(result == null, "failed public freshness returns no partial snapshot: " + context);
                if (mode != "clock") c.That(ReferenceEquals(original, caught), "identity callback retains its exact original exception: " + context);
                if (game.Fault != null) {
                    var first = game.Fault; long step = game.Clock.StepIndex;
                    c.Throws<InvalidOperationException>(() => view.TryRead(company, StockKnowledgeProvider.ProviderId, subject.Id, out _), "cached known reading cannot escape terminal fault: " + context);
                    c.Throws<InvalidOperationException>(() => game.Save(), "impure lookup cannot save a healthy state: " + context);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "impure lookup cannot publish a healthy hash: " + context);
                    c.Equal(0, game.Update(), "faulted identity lookup advances no game step: " + context);
                    c.Equal(step, game.Clock.StepIndex, "sole clock remains at failed lookup epoch: " + context);
                    c.That(ReferenceEquals(first, game.Fault), "original first fault remains authoritative: " + context);
                }
                c.Equal(untouched, independent.Hash(), "a separate saved session retains its original knowledge: " + context);
            }
        }
    }
}
