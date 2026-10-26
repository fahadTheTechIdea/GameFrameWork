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
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static class ObservationRefreshTests
    {
        public static void Register() => TestRunner.Add("Knowledge refresh: physical observations and durable chronology", Run);
        private static string Document => FrameworkProofContent.Read("knowledge-refresh-work.json");
        private static GameFactory Factory(string work = null) => ConstructionPlanTests.Points(
            additionalModule: new KnowledgeProofTechnologyModule(),
            moduleDeclaration: "{\"id\":\"sim.technology\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"knowledge-refresh-work.json\",\"technology.json\"]}",
            moduleDocument: work ?? Document, knowledgeDefinitions: FrameworkProofContent.Read("knowledge.json"),
            extraDocuments: new Dictionary<string, string> { ["technology.json"] = FrameworkProofContent.Read("technology.json") },
            scenarioEdit: scenario => scenario.Get("modules").Add(JsonValue.Of(TechnologyModule.ModuleId)));
        private static KnowledgeObservation Reading(SimGame game, string subject = StockKnowledgeProvider.SubjectId)
        {
            if (!game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), StockKnowledgeProvider.ProviderId, subject, out var reading))
                throw new InvalidOperationException("missing actual observation " + subject);
            return reading;
        }
        private static long Quantity(KnowledgeObservation reading) => new JsonReader(reading.Data, "reading").Long("quantity_raw");
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " fault=" + game.Fault + " orders=" +
                string.Join(";", Board(game).Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static Order Start(Check c, SimGame game, string plan)
        {
            var command = new PostOrderCommand { Plan = plan, Target = Find(game, "source").Id, Priority = 60 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary authored activity preview " + plan);
            CommandResult result = TestKit.Order(game, 0, command);
            c.That(result.Accepted, "ordinary authored activity accepted " + plan);
            return Board(game).Order((int)result.Arg0);
        }
        private static void Run(Check c)
        {
            GameFactory factory = Factory(); SimGame game = ConstructionPlanTests.NewPoints(factory);
            ICompanyKnowledge ledger = game.World.Services.Get<ICompanyKnowledge>();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "refresh has exact current engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no old observation save reader");
            c.Equal(3, ((CompanyKnowledgeService)ledger).StateSchemaVersion, "one migrated ledger codec");
            c.Equal(1, game.World.Services.Persistent().Count(s => s.Provider.Id == CompanyKnowledgeModule.ModuleId), "one knowledge persistence owner");
            var definition = game.World.Content.Get<StockKnowledgeSubjectDef>(StockKnowledgeProvider.SubjectId);
            c.That(definition.AuthoringPath.Contains("knowledge-refresh-work.json"), "subject comes from actual selected master document");
            c.Equal(KnowledgeRetention.LatestObservation, definition.Retention, "explicit latest retention authored");
            c.Equal(KnowledgeRetention.FirstLearned, game.World.Content.Get<StockKnowledgeSubjectDef>("source.permanent").Retention, "explicit permanent subject authored");
            c.Equal(KnowledgeRetention.FirstLearned, game.World.Services.Get<ITechnologyKnowledge>() is IKnowledgeProvider tech
                ? tech.Retention("research.foundation") : (KnowledgeRetention)0, "technology remains intrinsically permanent");
            Configure(c, game);
            var held = new List<KnowledgeObservation>(); game.Seat(0).View.Feature<KnowledgeView>().CopyKnown(held);
            c.Equal(0, held.Count, "no pre-granted observations");
            Order first = Start(c, game, "field.discover");
            Reach(game, () => first.Node("sample").Progress.WholeTicks > 0, "first partial observation");
            c.That(!ledger.TryRead(new PlayerId(0), new PlayerId(0), StockKnowledgeProvider.ProviderId, StockKnowledgeProvider.SubjectId, out _), "partial physical work publishes nothing");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "unknown partial work saves identically");
            Reach(game, () => first.State == OrderState.Done, "first learned and remembered observations", twin);
            c.Equal(game.Hash(), twin.Hash(), "first physical continuation agrees");
            KnowledgeObservation original = Reading(game), permanent = Reading(game, "source.permanent");
            c.Equal(Fix.FromInt(12).Raw, Quantity(original), "first actual survey consumes eight supplies");
            c.Equal(original.LearnedTick, original.ObservedTick, "first observation initializes both times");
            c.That(original.LearnedTick > 0 && original.ObservedTick <= game.Tick, "times derive from sole game clock");
            c.Equal(Fix.FromInt(12).Raw, Quantity(permanent), "separate permanent subject captures same real stock");
            c.Equal(0, held.Count, "held pre-learning list remains detached");
            c.Throws<InvalidOperationException>(() => original.Data.Set("quantity_raw", JsonValue.Number("0")), "held reading payload is immutable");
            c.That(game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "field.observed", out var fact), "same physical DAG learns native permanent fact");
            long factTime = fact.LearnedTick;

            Order refresh = Start(c, game, "field.refresh");
            Reach(game, () => refresh.Node("sample").Progress.WholeTicks > 0, "refresh partial material-consuming work");
            c.Equal(Fix.FromInt(4), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "second activity already consumed physical inputs");
            c.Equal(Fix.FromInt(12).Raw, Quantity(Reading(game)), "remembered data is stale until actual refresh completes");
            c.Equal(original.ObservedTick, Reading(game).ObservedTick, "reads and partial work do not refresh chronology");
            ulong hash = game.Hash();
            c.Equal(Code.None, ledger.CheckRecord(new PlayerId(0), original.Provider, original.Subject,
                Content.Json.Parse("{\"resource\":\"supply\",\"quantity_raw\":" + Fix.FromInt(4).Raw + "}")), "fresh data preview accepts explicit latest policy");
            c.Equal(hash, game.Hash(), "refresh readiness is pure");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "old observation and partial new work checkpoint together");
            Reach(game, () => refresh.State == OrderState.Done, "changed observation completed", twin);
            c.Equal(game.Hash(), twin.Hash(), "changed remembered reading continues identically");
            KnowledgeObservation changed = Reading(game);
            c.Equal(Fix.FromInt(4).Raw, Quantity(changed), "refresh reads actual remaining stock");
            c.Equal(original.LearnedTick, changed.LearnedTick, "refresh preserves first-learned chronology");
            c.That(changed.ObservedTick > original.ObservedTick, "refresh advances last-observed chronology");
            c.Equal(Fix.FromInt(12).Raw, Quantity(original), "held old observation remains historical");
            c.Equal(permanent.ObservedTick, Reading(game, "source.permanent").ObservedTick, "permanent reading does not follow latest subject");
            c.Equal(Fix.FromInt(12).Raw, Quantity(Reading(game, "source.permanent")), "permanent payload stays first learned");
            c.That(ledger.TryRead(new PlayerId(0), new PlayerId(0), "sim.knowledge.facts", "field.observed", out fact)
                && fact.LearnedTick == factTime && fact.ObservedTick == factTime, "repeat native fixed fact is idempotent");
            hash = game.Hash();
            c.Equal(KnowledgeCodes.Known, ledger.Record(new PlayerId(0), permanent.Provider, permanent.Subject, changed.Data), "changed permanent reading refuses explicitly");
            c.Equal(Code.None, ledger.Record(new PlayerId(0), permanent.Provider, permanent.Subject, permanent.Data.Clone()), "identical permanent reading is no-op even later");
            c.Equal(hash, game.Hash(), "permanent no-op/refusal does not change ledger");

            Order cancelled = Start(c, game, "field.reobserve");
            Reach(game, () => cancelled.Node("sample").Progress.WholeTicks > 0, "cancelled repeat partial work");
            c.Equal(changed.ObservedTick, Reading(game).ObservedTick, "same-value incomplete work does not masquerade as fresh");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = cancelled.Id }).Accepted, "ordinary cancellation accepted");
            c.Equal(changed.ObservedTick, Reading(game).ObservedTick, "cancellation earns no refresh");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "cancelled observation work retains sole learned state");
            Order reaffirm = Start(c, game, "field.reobserve");
            twin = factory.Load(game.Save());
            Reach(game, () => reaffirm.State == OrderState.Done, "same-value actual physical reobservation", twin);
            c.Equal(game.Hash(), twin.Hash(), "same-value reobservation has deterministic saved chronology");
            KnowledgeObservation latest = Reading(game);
            c.Equal(Quantity(changed), Quantity(latest), "actual no-consumption reobservation retains real quantity");
            c.Equal(original.LearnedTick, latest.LearnedTick, "all refreshes preserve original learning");
            c.That(latest.ObservedTick > changed.ObservedTick, "completed same-value work still establishes a new observation time");
            c.Equal(Fix.FromInt(4), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "knowledge creates no balance or money");
            var rows = new List<KnowledgeObservation>(); game.Seat(0).View.Feature<KnowledgeView>().CopyKnown(rows);
            c.Equal(3, rows.Count, "refresh replaces SAME subject instead of appending a duplicate history store");
            game.Seat(1).View.Feature<KnowledgeView>().CopyKnown(rows); c.Equal(0, rows.Count, "foreign company sees no private reading");
            c.That(!ledger.TryRead(new PlayerId(-2), new PlayerId(0), latest.Provider, latest.Subject, out _), "malformed viewer is not an observer");
            c.That(ledger.TryRead(PlayerId.None, new PlayerId(0), latest.Provider, latest.Subject, out _), "explicit observer remains deliberate tooling authority");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled latest/permanent/native records all reload");
            SimGame isolated = ConstructionPlanTests.NewPoints(factory);
            c.That(!isolated.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), latest.Provider, latest.Subject, out _), "interleaved session borrows no observation");
            InvalidDecode(c, game, latest, permanent);
            InvalidAuthoring(c);
        }
        // Engine codec instrumentation, NOT a player observation grant or gameplay path.
        private static void InvalidDecode(Check c, SimGame game, KnowledgeObservation latest, KnowledgeObservation permanent)
        {
            var ledger = (CompanyKnowledgeService)game.World.Services.Get<ICompanyKnowledge>(); ulong hash = game.Hash();
            foreach (string mode in new[] { "negative-learned", "negative-observed", "future-observed", "reversed", "permanent-refresh", "schema", "duplicate", "payload", "truncated" })
            {
                KnowledgeObservation row = mode == "permanent-refresh" ? permanent : latest;
                long learned = mode == "negative-learned" ? -1 : mode == "reversed" ? game.Tick : row.LearnedTick;
                long observed = mode == "negative-observed" ? -1 : mode == "future-observed" ? game.Tick + 1
                    : mode == "reversed" ? 0 : mode == "permanent-refresh" ? game.Tick : row.ObservedTick;
                PlayerId company = row.Company; string provider = row.Provider, subject = row.Subject;
                int schema = mode == "schema" ? row.SchemaVersion + 1 : row.SchemaVersion;
                string data = mode == "payload" ? "{}" : CompanyKnowledgeService.Canonical(row.Data);
                using var stream = new MemoryStream(); var writer = new StateWriter(stream); int count = mode == "duplicate" ? 2 : 1;
                writer.Count(count);
                for (int i = 0; i < count; i++)
                { writer.Player(ref company); writer.String(ref provider); writer.String(ref subject); writer.Int(ref schema);
                    writer.Long(ref learned); writer.Long(ref observed); writer.String(ref data); }
                byte[] bytes = stream.ToArray();
                c.Throws<CorruptStateException>(() => ledger.Sync(new StateReader(bytes, 0, mode == "truncated" ? bytes.Length - 1 : bytes.Length)), "invalid refreshed decode refuses " + mode);
                c.Equal(hash, game.Hash(), "rejected decode preserves entire settled ledger " + mode);
            }
        }
        private static void InvalidAuthoring(Check c)
        {
            foreach (string retention in new[] { "missing", "", "automatic", "LATEST" })
            {
                JsonValue document = Content.Json.Parse(Document); JsonValue subject = document.Get("stock_knowledge_subjects")[0];
                if (retention == "missing") subject.Remove("retention"); else subject.Set("retention", JsonValue.Of(retention));
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Factory(document.ToString())), "no guessed retention policy " + retention);
            }
            c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Factory(Document.Replace("\"subject\":\"source.reading\"", "\"subject\":\"missing\""))), "effect needs actual subject reference");
            c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Factory(Document.Replace("\"resource\":\"supply\"", "\"resource\":\"missing\""))), "subject needs actual resource reference");
        }
    }
}
