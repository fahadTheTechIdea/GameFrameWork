using System;
using System.Diagnostics;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    internal static partial class VisibilityContentContractTests
    {
        // Authoring and observation only. The original visibility owner decides every query;
        // these counters are diagnostics, not a visibility cache or another knowledge ledger.
        private static void Allocation(Check c)
        {
            const string targetId = "field.visibility-declared";
            foreach (bool independent in new[] { false, true })
            foreach (int count in new[] { 1, 2, 3 })
            {
                var document = Json.Parse(FrameworkProofContent.Read("visibility-targets.json"));
                var targets = JsonValue.NewArray();
                foreach (string id in new[] { "source", "machine", "destination" })
                    if (targets.Count < count) targets.Add(JsonValue.Of(id));
                document.Get("declared_visibility_targets")[0].Set("targets", targets);
                var module = new VisibilityContentContractsModule();
                var factory = Factory(module, independent, edit: doc =>
                    doc.Get("independent_visibility_policies")[0].Get("rules").Add(JsonValue.Of(targetId)),
                    targetDocument: document.ToString());
                var game = New(factory);
                var rule = game.World.Content.Get<DeclaredVisibilityTargetsDef>(targetId);
                var source = game.World.Content.Get<EntityDef>("source");
                var machine = game.World.Content.Get<EntityDef>("machine");
                ulong before = game.Hash(); int reads = 0;
                rule.TargetsProbe = () => reads++;
                c.That(!game.Seat(0).View.CanSeeContent(source), "first actual declared target remains hidden/" + independent + "/" + count);
                c.Equal(count < 2, game.Seat(0).View.CanSeeContent(machine), "remaining-target membership is exact/" + independent + "/" + count);
                c.That(game.Observer.CanSeeContent(source), "observer keeps its original bypass/" + count);
                c.Equal(before, game.Hash(), "target observations preserve the actual world/" + count);

                var warming = Stopwatch.StartNew();
                do { _ = game.Seat(0).View.CanSeeContent(source); } while (warming.ElapsedMilliseconds < 150);
                const int rounds = 256;
                int priorReads = reads, allowed = 0;
                long allocated = GC.GetAllocatedBytesForCurrentThread(), started = Stopwatch.GetTimestamp();
                for (int i = 0; i < rounds; i++) if (game.Seat(0).View.CanSeeContent(source)) allowed++;
                long bytes = GC.GetAllocatedBytesForCurrentThread() - allocated, ticks = Stopwatch.GetTimestamp() - started;
                c.Equal(0, allowed, "every measured query uses actual refusal/" + count);
                c.Equal(rounds, reads - priorReads, "target metadata is freshly observed on every query/" + count);
                c.Equal(before, game.Hash(), "measurement changes no authoritative state/" + count);
                Console.WriteLine(FormattableString.Invariant($"visibility facts;build={EngineInfo.Build};independent={independent};targets={count};reads={rounds};elapsed-ms={(double)ticks * 1000 / Stopwatch.Frequency:F3};allocated={bytes};hash={before:X16}"));
                var twin = factory.Load(game.Save());
                c.Equal(game.Hash(), twin.Hash(), "transient facts introduce no saved state/" + count);
                c.Equal(game.Seat(0).View.CanSeeContent(source), twin.Seat(0).View.CanSeeContent(source), "saved twin observes same native policy/" + count);

                var primary = new InvalidOperationException("visibility-allocation-original");
                rule.TargetsProbe = () => throw primary;
                Exception observed = null;
                try { _ = game.Seat(0).View.CanSeeContent(source); } catch (Exception error) { observed = error; }
                c.That(ReferenceEquals(primary, observed), "later metadata callback is not replaced by cached facts/" + count);
                c.That(game.Fault?.Message.Contains(primary.Message) == true, "actual target callback retains original first fault/" + count);
                c.Throws<InvalidOperationException>(() => game.Save(), "failed metadata observation cannot save/" + count);
                rule.TargetsProbe = null;
                c.That(!twin.Seat(0).View.CanSeeContent(source) && twin.Fault == null, "interleaved saved session remains healthy/" + count);
            }
            foreach (string list in new[] { "[\"source\",\"machine\",\"source\"]", "[\"source\",\"machine\",\"machine\"]" })
            {
                var document = Json.Parse(FrameworkProofContent.Read("visibility-targets.json"));
                document.Get("declared_visibility_targets")[0].Set("targets", Json.Parse(list));
                c.Throws<ContentException>(() => New(Factory(new VisibilityContentContractsModule(), targetDocument: document.ToString())),
                    "late duplicate first or remaining target cannot bypass actual alias validation");
            }
        }
    }
}
