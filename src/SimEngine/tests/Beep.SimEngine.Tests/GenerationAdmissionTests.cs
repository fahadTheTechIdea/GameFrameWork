using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static class GenerationAdmissionTests
    {
        internal const string Case = "Generation admission: original participant codecs guard inventory conversion and requested claim preflight";
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ReservationOwner Owner = new ReservationOwner("proof.generation", 1);
        private static readonly Code Cause = new Code("proof.generate");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool refused = false, bool nativeStorage = false)
        {
            var document = Json.Parse(FrameworkProofContent.Read("generation-admission.json"));
            if (refused) document.Get("generation_admission_probes")[0].Set("refusal", JsonValue.Of("proof.generation-refused"));
            return TransformerTests.Factory(root => {
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.generation-admission"));
                if (nativeStorage) {
                    var parts = root.Get("entities")[0].Get("components"); parts.Remove("process_stock");
                    parts.Set("storage", Json.Parse("{\"capacity_policy\":\"fixture.count.40\",\"initial_stock\":{\"supply\":20},\"public\":false,\"evacuate\":false,\"accepts\":[\"supply\"],\"phases\":[]}"));
                }
            },
                additionalModule: new GenerationAdmissionProofModule(),
                additionalDeclaration: "{\"id\":\"proof.generation-admission\",\"version\":\"1.0.0\",\"requires\":[],\"documents\":[\"generation-admission.json\"]}",
                additionalDocument: document.ToString());
        }
        private static SimGame New(GameFactory factory, bool loaded)
        { var game = TransformerTests.New(factory); return loaded ? factory.Load(game.Save()) : game; }
        private static GenerationAdmissionProbe Probe(SimGame game) => game.World.Services.Get<GenerationAdmissionProbe>();
        private static IInventory Stock(SimGame game) => TransformerTests.Find(game, "assembler").Get<IInventory>();
        private static ResourceId Resource(SimGame game) => game.World.Content.ResourceId("supply");
        private static Bill One(SimGame game) => new Bill(new[] { new ResourceAmount(Resource(game), Fix.One) });
        private static ResourceInventoryResult Invoke(SimGame game, string entry)
        {
            var transaction = game.World.Services.Get<IResourceTransaction>(); var bill = One(game);
            var output = new[] { new ResourceProductionIntent(Stock(game).Endpoint, bill) };
            if (entry == "claims") return transaction.EvaluateConversionClaims(Company, Owner, Array.Empty<ResourceClaimIntent>(), output, Cause);
            if (entry.StartsWith("inventory", StringComparison.Ordinal)) {
                var intent = new ResourceInventoryIntent(Owner, Company, TransformerTests.Find(game, "assembler").Id, bill, ResourceFlowKind.Produced, Cause);
                if (entry == "inventory-commit") return transaction.CommitInventory(intent);
                var result = transaction.EvaluateInventory(intent); return new ResourceInventoryResult(result.Status, blocker: result.Blocker);
            }
            var conversion = new ResourceConversionIntent(Owner, Company, Array.Empty<ResourceConversionInput>(), output, Cause);
            return entry == "conversion-commit" ? transaction.CommitConversion(conversion, null) : transaction.EvaluateConversion(conversion, null);
        }
        private static void Run(Check c)
        {
            foreach (bool loaded in new[] { false, true })
            foreach (string entry in new[] { "inventory-preview", "inventory-commit", "conversion-preview", "conversion-commit", "claims" })
            {
                bool committing = entry.EndsWith("commit", StringComparison.Ordinal);
                var factory = Factory(); var healthy = New(factory, loaded); var twin = factory.Load(healthy.Save());
                ulong before = healthy.Hash(); var result = Invoke(healthy, entry); var other = Invoke(twin, entry);
                c.That(committing ? result.Committed && other.Committed : result.Ready && other.Ready, "actual generation admission succeeds: " + entry);
                c.That(Probe(healthy).Calls == 1 && Probe(healthy).Marker == 7 && Probe(healthy).Commits == (committing ? 1 : 0),
                    "one pure callback and only an actual commit changes participant state: " + entry);
                var admitted = Probe(healthy).Last;
                c.That(admitted.Owner.Equals(Owner) && admitted.Company == Company && admitted.Cause == Cause
                    && admitted.Consumed.IsEmpty && admitted.Produced.AmountOf(Resource(healthy)) == Fix.One,
                    "same actual owner company cause and generation quantities reach participant: " + entry);
                c.Equal(Fix.FromInt(committing ? 21 : 20), Stock(healthy).Amount(Resource(healthy)), "exact original generated stock: " + entry);
                c.That(committing || before == healthy.Hash(), "preview never publishes participant or physical state: " + entry);
                c.Equal(healthy.Hash(), twin.Hash(), "generation participant and physical state continue saved identically: " + entry);
                c.Equal(healthy.Hash(), factory.Load(healthy.Save()).Hash(), "committed or preview-only checkpoint roundtrip: " + entry);

                var refusal = New(Factory(true), loaded); before = refusal.Hash();
                result = Invoke(refusal, entry);
                c.That(result.Status == ResourceTransferStatus.SettlementRefused && result.Blocker == new Code("proof.generation-refused"),
                    "authored optional participant refusal has original typed meaning: " + entry);
                c.That(before == refusal.Hash() && Probe(refusal).Commits == 0, "ordinary refusal has no state or stock prefix: " + entry);

                foreach (string mode in new[] { "silent", "payload-throw", "clock-throw" }) {
                    string context = entry + "/" + mode + "/loaded=" + loaded;
                    factory = Factory(); var game = New(factory, loaded); var independent = factory.Load(game.Save()); ulong isolated = independent.Hash();
                    var probe = Probe(game); var world = game.World; long time = game.Tick; var published = game.Seat(0).Snapshot;
                    var primary = new InvalidOperationException("proof.original-generation-admission");
                    probe.AdmissionProbe = owner => {
                        if (mode == "clock-throw") ((GameClock)owner.Clock).Pause(); else probe.MutatePersistedProbe();
                        if (mode != "silent") throw primary;
                    };
                    Exception thrown = null; try { Invoke(game, entry); } catch (Exception error) { thrown = error; }
                    probe.AdmissionProbe = null;
                    c.That(probe.Calls == 1 && (mode == "clock-throw" || probe.Marker == 8), "actual callback mutates original persistent participant: " + context);
                    c.That(thrown is InvalidOperationException && (mode == "silent" || ReferenceEquals(primary, thrown)), "provider fault propagates original cause: " + context);
                    c.Equal("observation.provider", world.Fault?.Phase, "same observation owner guards generation admission: " + context);
                    if (mode != "silent") c.Equal("InvalidOperationException: proof.original-generation-admission", world.Fault?.Message, "first callback cause is never masked: " + context);
                    c.That(Stock(game).Amount(Resource(game)) == Fix.FromInt(20) && probe.Commits == 0 && Stock(game).Reservations.Count == 0,
                        "invalid admission publishes no stock capacity claim or participant commit: " + context);
                    c.That(game.Tick == time && game.Seat(0).Snapshot.Tick == published.Tick && !world.Events.HasPending,
                        "invalid admission publishes no time snapshot or success event: " + context);
                    if (world.Fault != null) {
                        c.Throws<InvalidOperationException>(() => game.Save(), "invalid admission cannot save healthy state: " + context);
                        c.Throws<InvalidOperationException>(() => game.Hash(), "invalid admission cannot certify healthy hash: " + context);
                    }
                    c.Equal(isolated, independent.Hash(), "separate saved generation session is unchanged: " + context);
                    c.That(Invoke(independent, entry).Status == (committing ? ResourceTransferStatus.Committed : ResourceTransferStatus.Ready)
                        && independent.Fault == null, "independent original transaction still operates: " + context);
                }
            }
            ClaimDeltas(c);
        }
        private static void ClaimDeltas(Check c)
        {
            foreach (bool loaded in new[] { false, true })
            foreach (bool held in new[] { false, true })
            {
                var factory = Factory(nativeStorage: true); var game = New(factory, loaded); var stock = Stock(game);
                var transaction = game.World.Services.Get<IResourceTransaction>();
                var one = One(game); ResourceClaimIntent claim;
                if (held) {
                    var three = new Bill(new[] { new ResourceAmount(Resource(game), Fix.FromInt(3)) });
                    var acquired = transaction.AcquireClaims(Company, new[] {
                        new ResourceClaimIntent(ReservationDirection.Outbound, Owner, stock.Endpoint, three)
                    }, Array.Empty<ResourceReservation>());
                    c.That(acquired.Committed && acquired.Receipts.Count == 1 && acquired.Receipts[0].Amount == Fix.FromInt(3),
                        "actual existing endpoint holds three input units before requested delta preflight");
                    claim = new ResourceClaimIntent(acquired.Receipts[0], one);
                }
                else claim = new ResourceClaimIntent(ReservationDirection.Outbound, Owner, stock.Endpoint, one);
                var output = new[] { new ResourceProductionIntent(stock.Endpoint,
                    new Bill(new[] { new ResourceAmount(Resource(game), Fix.FromInt(2)) })) };
                ulong before = game.Hash(); int calls = Probe(game).Calls;
                c.That(transaction.EvaluateConversionClaims(Company, Owner, new[] { claim }, output, Cause).Ready,
                    "mixed held or prospective consumption and same-endpoint production has whole admission");
                var context = Probe(game).Last;
                c.That(Probe(game).Calls == calls + 1 && context.Consumed.AmountOf(Resource(game)) == Fix.One
                    && context.Produced.AmountOf(Resource(game)) == Fix.FromInt(2),
                    "generation participant sees actual one-unit draw, never full three-unit held receipt");
                c.That(before == game.Hash() && Probe(game).Commits == 0 && stock.Amount(Resource(game)) == Fix.FromInt(20),
                    "claim admission changes no original stock promises generation state or history");
                calls = Probe(game).Calls;
                c.Equal(ResourceTransferStatus.InvalidRequest,
                    transaction.EvaluateConversionClaims(Company, Owner, new[] { claim }, output, Code.None).Status,
                    "missing required cause refuses before participant callback");
                var excessive = new[] { new ResourceProductionIntent(stock.Endpoint,
                    new Bill(new[] { new ResourceAmount(Resource(game), Fix.FromInt(100)) })) };
                c.Equal(ResourceTransferStatus.NoCapacity,
                    transaction.EvaluateConversionClaims(Company, Owner, new[] { claim }, excessive, Cause).Status,
                    "physical whole-output refusal precedes optional generation admission");
                c.That(Probe(game).Calls == calls && before == game.Hash() && game.Fault == null,
                    "structural and physical refusals are pure healthy states without optional callbacks");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "original held claim and admission participant save together");
            }
            var plain = TransformerTests.New(TransformerTests.Factory());
            c.That(!plain.World.Services.Has<IResourceProductionSettlement>() && plain.World.Services.Get<IResourceTransaction>()
                .EvaluateConversionClaims(Company, Owner, Array.Empty<ResourceClaimIntent>(),
                    new[] { new ResourceProductionIntent(Stock(plain).Endpoint, One(plain)) }, Cause).Ready,
                "physical generation requires no optional settlement or business module");
        }
    }
}
