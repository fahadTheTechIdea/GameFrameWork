using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;

namespace Beep.SimEngine.Tests
{
    public static class TechnologyTests
    {
        private const string Foundation = "research.foundation", Advanced = "research.advanced";
        public static void Register()
        {
            TestRunner.Add("Technology: authored contracts, access identities and private knowledge", Contracts);
            TestRunner.Add("Technology: physical research, unlocks and saved continuation", Physical);
            TestRunner.Add("Technology: scoped single reads share native catalog facts and private refusal", SingleReads);
        }
        private static GameFactory Factory(string json = null)
            => ConstructionPlanTests.Points(additionalModule: new TechnologyModule(),
                moduleDeclaration: "{\"id\":\"sim.technology\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"technology.json\"]}",
                moduleDocument: json ?? FrameworkProofContent.Read("technology.json"), scenarioEdit: scenario => scenario.Get("modules").Add(JsonValue.Of(TechnologyModule.ModuleId)));
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        internal static GameFactory DiscoveryFactory(string facts = null, string work = null, string visibility = null, Action<JsonValue> scenarioEdit = null, Action<JsonValue> constructionEdit = null,
            IReadOnlyDictionary<string, string> additionalDocuments = null, Action<JsonValue> worldEdit = null)
        {
            JsonValue root = Content.Json.Parse(FrameworkProofContent.Read("technology.json"));
            JsonValue extra = Content.Json.Parse(work ?? FrameworkProofContent.Read("knowledge-work.json"));
            root.Set("stock_knowledge_effects", extra.Get("stock_knowledge_effects"));
            root.Set("stock_knowledge_subjects", extra.Get("stock_knowledge_subjects"));
            JsonValue activities = extra.Get("activities");
            for (int i = 0; i < activities.Count; i++) root.Get("activities").Add(activities[i]);
            return ConstructionPlanTests.Points(edit: constructionEdit, worldEdit: worldEdit, additionalModule: new KnowledgeProofTechnologyModule(),
                moduleDeclaration: "{\"id\":\"sim.technology\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"technology.json\"]}",
                moduleDocument: root.ToString(), knowledgeDefinitions: facts ?? FrameworkProofContent.Read("knowledge.json"), additionalDocuments: additionalDocuments,
                extraModules: visibility == null ? null : new GameModule[] { new StockKnowledgeVisibilityModule() },
                extraDeclarations: visibility == null ? null : "[{\"id\":\"sim.knowledge-visibility\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"visibility.json\"]}]",
                extraDocuments: visibility == null ? null : new Dictionary<string, string> { ["visibility.json"] = visibility },
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(TechnologyModule.ModuleId));
                    if (visibility != null) { scenario.Get("modules").Add(JsonValue.Of(KnowledgeVisibilityModule.ModuleId)); scenario.Get("settings").Set(KnowledgeVisibilityModule.ModuleId, Content.Json.Parse("{\"policy\":\"discovery.policy\"}")); }
                    scenarioEdit?.Invoke(scenario);
                });
        }
        private static List<KnowledgeObservation> Observations(SimGame game, int seat = 0)
        { var facts = new List<KnowledgeObservation>(); game.Seat(seat).View.Feature<KnowledgeView>().CopyKnown(facts); return facts; }
        private static TechnologyView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<TechnologyView>();
        private static List<TechnologySnapshot> Facts(SimGame game, int seat = 0)
        { var list = new List<TechnologySnapshot>(); View(game, seat).CopyTechnologies(list); return list; }
        private static bool Known(SimGame game, string id) => Facts(game).Find(f => f.Technology == id).Known;
        private static CommandResult Research(SimGame game, string id, string target = "source", int seat = 0)
            => TestKit.Order(game, seat, new PostOrderCommand { Plan = id, Target = Find(game, target).Id, Priority = 70 });
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " tick=" + game.Tick + " technology=" + string.Join(";", Facts(game).ConvertAll(f => f.Technology + "/" + f.Known + "/" + f.ActiveOrder + "/" + f.Blocker)));
        }
        private static void SingleReads(Check c)
        {
            var factory = Factory(); var game = New(factory); var twin = factory.Load(game.Save());
            ulong before = game.Hash();
            foreach (var expected in Facts(game))
            {
                c.Equal(Code.None, View(game).ReadTechnology(expected.Technology, out var fact), "own scoped read admitted");
                c.Equal(expected.Company, fact.Company, "actual company identity");
                c.Equal(expected.Known, fact.Known, "same learned state as catalog");
                c.Equal(expected.CompletedTick, fact.CompletedTick, "same completion chronology");
                c.Equal(expected.ActiveOrder, fact.ActiveOrder, "same physical execution identity");
                c.Equal(expected.Blocker, fact.Blocker, "same native blocker");
                c.That(expected.Prerequisites.SequenceEqual(fact.Prerequisites), "same detached prerequisite facts");
                c.Equal(Code.None, twin.Seat(0).View.Feature<TechnologyView>().ReadTechnology(expected.Technology, out var saved), "saved native read admitted");
                c.Equal(fact.Technology, saved.Technology, "same saved definition identity");
            }
            foreach (var company in new[] { new PlayerId(1), new PlayerId(999), PlayerId.None })
            {
                c.Equal(Codes.NotFound, View(game).ReadTechnology(company, Foundation, out var withheld), "foreign/invalid owner indistinguishable from missing");
                c.Equal<string>(null, withheld.Technology, "refusal exposes no definition or state");
            }
            c.Equal(Codes.NotFound, View(game).ReadTechnology("missing", out var missing), "missing definition withheld");
            c.Equal<string>(null, missing.Technology, "missing output cleared");
            c.Equal(Code.None, game.Observer.Feature<TechnologyView>().ReadTechnology(new PlayerId(1), Foundation, out var observed), "explicit observer may inspect actual foreign enrollment");
            c.Equal(new PlayerId(1), observed.Company, "observer snapshot retains real owner");
            c.Equal(Codes.NotFound, game.Observer.Feature<TechnologyView>().ReadTechnology(Foundation, out _), "observer has no invented company");
            c.Equal(before, game.Hash(), "reads preserve original knowledge/work/clock");
            c.Equal(before, twin.Hash(), "saved session remains isolated and identical");
        }
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); var service = game.World.Services.Get<ITechnologyKnowledge>();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "typed access/progression has current identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no legacy API/save reader");
            c.That(ReferenceEquals(service, game.World.Services.Get<TechnologyService>()), "knowledge contract aliases one native service");
            c.Equal(1, game.World.Services.Providers<IContentAccessRule>().Count, "technology access discovered through declared same-owner rules");
            c.That(game.World.Content.Get<TechnologyDef>(Foundation).AuthoringPath.Contains("technology.json"), "technology definitions come from explicit master document");
            c.That(ReferenceEquals(game.World.Content.Get<TechnologyDef>(Foundation), game.World.Content.Get<ActivityDef>(Foundation)), "technology graph aliases existing activity catalog");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "research uses unchanged native work codec");
            c.That(!typeof(IPersistentService).IsAssignableFrom(typeof(TechnologyService)), "technology rules own no duplicate learned table/codec");
            c.Equal(3, ((CompanyKnowledgeService)game.World.Services.Get<ICompanyKnowledge>()).StateSchemaVersion, "one generic knowledge codec");
            ulong hash = game.Hash(); var privateFacts = Facts(game);
            c.That(privateFacts.Count == 2 && privateFacts.TrueForAll(f => f.Company == new PlayerId(0) && !f.Known && f.ActiveOrder == 0 && f.CompletedTick == null), "private immutable facts contain own unlearned knowledge only");
            c.That(Facts(game, 1).TrueForAll(f => f.Company == new PlayerId(1)), "another company never reads prior company's knowledge");
            c.Equal(Code.None, View(game).PreviewResearch(Foundation, Find(game, "source").Id, 70), "root research uses ordinary native admission");
            c.Equal(TechnologyCodes.MissingPrerequisite, View(game).PreviewResearch(Advanced, Find(game, "source").Id, 70), "authored prerequisite cannot be skipped");
            c.Equal(TechnologyCodes.Locked, service.CheckKnown(new PlayerId(0), Foundation), "unlearned is explicit, not assumed available");
            c.Equal(Codes.UnknownDef, service.CheckKnown(new PlayerId(0), "missing"), "unknown technology refuses");
            c.Equal(Codes.NotSeated, service.CheckKnown(new PlayerId(9), Foundation), "unknown company refuses");
            c.Equal(hash, game.Hash(), "private graph and readiness reads are pure");
            c.Equal(TechnologyCodes.MissingPrerequisite, Research(game, Advanced).Refusal, "ordinary research preview/command parity");
            c.Equal(Codes.NotOwned, Research(game, Foundation, seat: 1).Refusal, "foreign target retains ordinary ownership rules");
            c.Equal(0, Board(game).Orders.Count, "refused research allocates no execution");
            var access = game.World.Services.Get<IContentAccess>(); EntityDef tower = game.World.Content.Get<EntityDef>("point.tower"); ActivityDef homonym = game.World.Content.Get<ActivityDef>("point.tower");
            c.Equal(TechnologyCodes.Locked, access.Check(game.World, new PlayerId(0), tower), "entity unlock gates exact resolved prototype");
            c.Equal(Code.None, access.Check(game.World, new PlayerId(0), homonym), "same-text activity identity is not the entity prototype");
            c.Equal(Codes.UnknownDef, access.Check(game.World, new PlayerId(0), null), "null content identity never opens access");
            SimGame foreign = New(Factory()); c.Equal(Codes.UnknownDef, access.Check(game.World, new PlayerId(0), foreign.World.Content.Get<EntityDef>("point.tower")), "foreign catalog identity cannot substitute same-text content");
            c.Equal(Codes.UnknownDef, access.Check(foreign.World, new PlayerId(0), tower), "foreign session cannot use rule owner");
            c.Equal(TechnologyCodes.Locked, game.Seat(0).View.Feature<ConstructionView>().CheckPlacement("point.tower", Content.Json.Parse("{\"space\":\"field.points\",\"position\":12}")), "normal construction previews use technology gate");
            c.Equal(TechnologyCodes.Locked, View(game).PreviewResearch("inspect", Find(game, "source").Id, 1), "typed activity unlock uses same access boundary");
            c.Equal(Code.None, View(game).PreviewResearch("point.tower", Find(game, "source").Id, 1), "same-name ordinary activity remains usable");
            c.Equal(Codes.NotFound, service.Complete(null), "no instant grant API without actual order");
            foreach (string mode in new[] { "missing", "cycle", "duplicate", "missing-unlock", "bad-target", "no-final", "zero-time", "node-effect", "missing-array" })
            {
                JsonValue root = Content.Json.Parse(FrameworkProofContent.Read("technology.json")); JsonValue advanced = root.Get("technologies")[0], foundation = root.Get("technologies")[1];
                if (mode == "missing") advanced.Set("prerequisites", Content.Json.Parse("[\"missing\"]"));
                if (mode == "cycle") foundation.Set("prerequisites", Content.Json.Parse("[\"research.advanced\"]"));
                if (mode == "duplicate") advanced.Set("prerequisites", Content.Json.Parse("[\"research.foundation\",\"research.foundation\"]"));
                if (mode == "missing-unlock") foundation.Set("unlocks", Content.Json.Parse("[\"missing\"]"));
                if (mode == "bad-target") root.Get("technology_entity_unlocks")[0].Set("definition", JsonValue.Of("missing"));
                if (mode == "no-final") foundation.Remove("on_complete");
                if (mode == "zero-time") { foundation.Get("nodes")[0].Set("duration", Content.Json.Parse("{\"numerator\":0,\"denominator\":1,\"unit\":\"hour\"}")); foundation.Get("nodes")[1].Set("duration", Content.Json.Parse("{\"numerator\":0,\"denominator\":1,\"unit\":\"hour\"}")); }
                if (mode == "node-effect") foundation.Get("nodes")[0].Set("effect", JsonValue.Of("research.complete"));
                if (mode == "missing-array") foundation.Remove("prerequisites");
                c.Throws<ContentException>(() => New(Factory(root.ToString())), "invalid authored " + mode + " refuses before Initialize");
            }
            SimGame ordinary = ConstructionPlanTests.NewPoints(ConstructionPlanTests.Points());
            c.That(!ordinary.World.Services.Has<ITechnologyKnowledge>(), "work-only game needs no progression state/module");
            c.Equal(Code.None, ordinary.Seat(0).View.Feature<ConstructionView>().CheckPlacement("point.tower", Content.Json.Parse("{\"space\":\"field.points\",\"position\":12}")), "optional omission does not install a hidden tech lock");
            PhysicalCancellation(c);
            KnowledgeContracts(c);
        }
        private static void PhysicalCancellation(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Configure(c, game);
            CommandResult command = Research(game, Foundation); c.That(command.Accepted, "research posts one ordinary native graph");
            Order order = Board(game).Order((int)command.Arg0); var knowledge = game.World.Services.Get<ITechnologyKnowledge>();
            ulong hash = game.Hash(); c.Equal(WorkEffectCodes.NotComplete, knowledge.Complete(order), "active graph cannot instantly learn"); c.Equal(hash, game.Hash(), "refused early completion changes no state");
            c.Equal(TechnologyCodes.Active, View(game).PreviewResearch(Foundation, Find(game, "destination").Id, 1), "company-exclusive active research spans actual targets");
            c.Equal(TechnologyCodes.Active, Research(game, Foundation, "destination").Refusal, "another target cannot duplicate the active research");
            Until(game, () => order.Node("research").Progress.WholeTicks > 0, "actual research resources/travel");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "real native inputs consumed once");
            c.Equal(Fix.FromInt(2), Find(game, "source").Get<IInventory>().ReservedOut(Supply(game)), "real native reusable tools retained");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "ordinary cancellation ends research work");
            c.That(!Known(game, Foundation), "cancelled research never learns or unlocks");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "spent research materials never refunded");
            c.Equal(Fix.Zero, Find(game, "source").Get<IInventory>().ReservedOut(Supply(game)), "unspent tools release through native cancellation");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled work and unlearned knowledge checkpoint");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Configure(c, game);
            var arguments = Content.Json.Parse("{\"target\":" + Find(game, "source").Id.Value + "}");
            game.Seat(0).Submit(new SubmitPlanCommand { Intent = new PlanIntent("activity:" + Foundation, arguments, "research", 70, false) });
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "saved ordinary research plan inbox is identical");
            Until(game, () => Facts(game).Find(f => f.Technology == Foundation).ActiveOrder > 0 &&
                Board(game).Order(Facts(game).Find(f => f.Technology == Foundation).ActiveOrder).Node("research").Progress.WholeTicks > 0, "native research partial progress", twin);
            var plans = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlanGroup("research", plans); long id = plans[0].Id;
            var savedFacts = Facts(game); c.That(!Known(game, Foundation), "partial timed work has no premature knowledge");
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = id, Group = "research", Priority = 70, Paused = true }).Accepted, "research uses native plan pause");
            c.That(TestKit.Order(twin, 0, new ConfigurePlanCommand { Plan = id, Group = "research", Priority = 70, Paused = true }).Accepted, "twin pause accepted");
            twin = factory.Load(game.Save()); var order = Board(game).Order(Facts(game).Find(f => f.Technology == Foundation).ActiveOrder); WorkProgress progress = order.Node("research").Progress;
            for (int i = 0; i < 8; i++) { Tick(game); Tick(twin); }
            c.Equal(progress, order.Node("research").Progress, "paused research earns no work/material burn"); c.Equal(game.Hash(), twin.Hash(), "paused research and real tools remain identical after load");
            TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = id, Group = "research", Priority = 70, Paused = false }); TestKit.Order(twin, 0, new ConfigurePlanCommand { Plan = id, Group = "research", Priority = 70, Paused = false });
            Until(game, () => Known(game, Foundation), "real completed technology unlock", twin); c.Equal(game.Hash(), twin.Hash(), "native completion/knowledge records are twin-identical");
            c.That(savedFacts.TrueForAll(f => !f.Known), "retained knowledge facts remain detached");
            c.That(Facts(game, 1).TrueForAll(f => !f.Known), "learned technology never leaks/grants to another company");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "earned knowledge consumes eight physical inputs");
            c.Equal(Fix.Zero, Find(game, "source").Get<IInventory>().ReservedOut(Supply(game)), "real research tools release on completion");
            c.Equal(TechnologyCodes.Known, Research(game, Foundation).Refusal, "completed technology cannot execute/grant twice");
            c.Equal(TechnologyCodes.Known, Research(twin, Foundation).Refusal, "twin receives the identical refused player command");
            c.Equal(Code.None, game.Seat(0).View.Feature<ConstructionView>().CheckPlacement("point.tower", Content.Json.Parse("{\"space\":\"field.points\",\"position\":12}")), "earned entity unlock reaches actual normal construction admission");
            c.Equal(TechnologyCodes.Locked, game.World.Services.Get<IContentAccess>().Check(game.World, new PlayerId(1), game.World.Content.Get<EntityDef>("point.tower")), "other company remains locked despite player progress");
            c.That(Research(game, Advanced).Accepted && Research(twin, Advanced).Accepted, "prerequisite opens ordinary advanced graph");
            Until(game, () => Known(game, Advanced), "advanced technology native requirements", twin);
            c.Equal(game.Hash(), twin.Hash(), "entire two-step graph knowledge survives isolated continuation");
            c.Equal(Fix.FromInt(4), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "each earned technology consumes real materials once");
            c.Equal(Code.None, View(game).PreviewResearch("inspect", Find(game, "source").Id, 1), "advanced typed activity unlock affects normal command preview");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed technology history saves without retained native orders");
            c.That(Facts(game).TrueForAll(f => f.Known && f.CompletedTick.HasValue && f.ActiveOrder == 0), "knowledge does not retain parallel active work/progress");
            SimGame corrupt = factory.Load(game.Save()); var service = corrupt.World.Services.Get<ICompanyKnowledge>();
            var records = (IList)typeof(CompanyKnowledgeService).GetField("_known", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(service);
            var record = (KnowledgeObservation)records[0];
            records[0] = new KnowledgeObservation(record.Company, record.Provider, record.Subject, record.SchemaVersion, corrupt.Tick + 1, corrupt.Tick + 1, record.Data);
            c.Throws<CorruptStateException>(() => corrupt.Save(), "future learned record refuses checkpoint"); c.Throws<CorruptStateException>(() => corrupt.Hash(), "future learned record cannot hash as settled");
            KnowledgePhysical(c);
        }

        private static void KnowledgeContracts(Check c)
        {
            SimGame game = New(DiscoveryFactory()); ICompanyKnowledge ledger = game.World.Services.Get<ICompanyKnowledge>();
            c.Equal(3, game.World.Services.Providers<IKnowledgeProvider>().Count, "technology, authored facts and public stock provider discover once, including aliases");
            c.Equal(1, game.World.Services.Persistent().Count(s => s.Provider.Id == CompanyKnowledgeModule.ModuleId), "one learned-state registration");
            c.Equal(0, game.World.Services.Persistent().Count(s => s.Provider.Id == TechnologyModule.ModuleId), "no old technology learned codec");
            c.Equal(0, Observations(game).Count, "authored definitions do not pre-grant knowledge");
            c.Equal(0, Observations(game, 1).Count, "both companies start independently unknown");
            c.That(game.World.Content.Get<KnowledgeFactDef>("field.observed").AuthoringPath.Contains("knowledge.json"), "master-listed knowledge document is actual origin");
            ulong hash = game.Hash(); JsonValue data = Content.Json.Parse("{\"resource\":\"supply\",\"quantity_raw\":0}");
            c.Equal(KnowledgeCodes.MissingProvider, ledger.CheckRecord(new PlayerId(0), "missing", "source.reading", data), "missing provider refuses explicitly");
            c.Equal(Codes.NotSeated, ledger.CheckRecord(PlayerId.None, StockKnowledgeProvider.ProviderId, StockKnowledgeProvider.SubjectId, data), "observer cannot own learned state");
            c.Equal(Codes.InvalidArgument, ledger.CheckRecord(new PlayerId(0), StockKnowledgeProvider.ProviderId, "bad subject", data), "noncanonical subject refuses");
            c.Equal(Codes.InvalidArgument, ledger.CheckRecord(new PlayerId(0), StockKnowledgeProvider.ProviderId, StockKnowledgeProvider.SubjectId, null), "no fabricated payload");
            c.Equal(Codes.UnknownDef, ledger.CheckRecord(new PlayerId(0), StockKnowledgeProvider.ProviderId, "missing", data), "provider rejects unknown subject");
            foreach (string invalid in new[] { "{}", "{\"resource\":\"missing\",\"quantity_raw\":0}", "{\"resource\":\"supply\",\"quantity_raw\":-1}", "{\"resource\":\"supply\",\"quantity_raw\":0,\"extra\":1}" })
                c.Equal(Codes.InvalidArgument, ledger.CheckRecord(new PlayerId(0), StockKnowledgeProvider.ProviderId, StockKnowledgeProvider.SubjectId, Content.Json.Parse(invalid)), "typed public provider rejects malformed data");
            c.That(!game.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(1), StockKnowledgeProvider.ProviderId, StockKnowledgeProvider.SubjectId, out _), "private lookup returns no foreign fact");
            var invalidViewer = new List<TechnologySnapshot>(); game.World.Services.Get<ITechnologyKnowledge>().CopyTechnologies(new PlayerId(-2), invalidViewer);
            c.Equal(0, invalidViewer.Count, "invalid negative viewer is not an observer");
            c.Equal(hash, game.Hash(), "all knowledge readiness and private reads are pure");
            c.Throws<ContentException>(() => New(DiscoveryFactory(facts: "{\"knowledge_facts\":[{\"id\":\"field.observed\",\"fresh_for\":null}]}")), "missing explicit fact value refuses");
            c.Throws<ContentException>(() => New(DiscoveryFactory(facts: FrameworkProofContent.Read("knowledge.json").Replace("\"subject\":\"field.observed\"", "\"subject\":\"missing\""))), "missing effect reference refuses");
            JsonValue work = Content.Json.Parse(FrameworkProofContent.Read("knowledge-work.json")); work.Get("activities")[0].Get("nodes")[0].Remove("roles");
            c.Throws<ContentException>(() => New(DiscoveryFactory(work: work.ToString())), "timer-only learning cannot replace positive physical machine work");
            Configure(c, game); CommandResult command = Research(game, "field.discover"); c.That(command.Accepted, "discovery uses ordinary authored work admission");
            Order order = Board(game).Order((int)command.Arg0);
            Until(game, () => order.Node("sample").Progress.WholeTicks > 0, "partial discovery");
            c.Equal(0, Observations(game).Count, "partial machine work earns no early knowledge");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "player cancels discovery normally");
            c.Equal(0, Observations(game).Count, "cancelled discovery has no known facts");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "cancellation does not refund spent sample materials");
        }

        private static void KnowledgePhysical(Check c)
        {
            GameFactory factory = DiscoveryFactory(); SimGame game = New(factory); Configure(c, game);
            var empty = Observations(game); CommandResult result = Research(game, "field.discover"); c.That(result.Accepted, "normal player orders physical discovery");
            Order order = Board(game).Order((int)result.Arg0);
            Until(game, () => order.Node("sample").Progress.WholeTicks > 0, "physical observation travel/material/work");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial discovery checkpoint includes native machine and requirements");
            Until(game, () => Observations(game).Count == 2, "physical public and native observations", twin);
            c.Equal(game.Hash(), twin.Hash(), "native facts and public observation continue identically");
            c.Equal(0, empty.Count, "held pre-discovery snapshot remains detached");
            c.Equal(0, Observations(game, 1).Count, "other seat sees no discovered private observations");
            var view = game.Seat(0).View.Feature<KnowledgeView>();
            c.That(view.TryRead(new PlayerId(0), StockKnowledgeProvider.ProviderId, StockKnowledgeProvider.SubjectId, out KnowledgeObservation stock), "private public-provider observation is readable");
            c.Equal(Fix.FromInt(12).Raw, new JsonReader(stock.Data, "stock").Long("quantity_raw"), "observation captures actual physical remaining stock, not authored balance");
            c.Equal("supply", stock.Data.Get("resource").Text, "observation retains stable resource identity");
            c.That(view.TryRead(new PlayerId(0), "sim.knowledge.facts", "field.observed", out KnowledgeObservation fact), "final native effect earns authored fact");
            c.Equal("sampled", fact.Data.Get("classification").Text, "authored structured payload retained");
            c.That(stock.LearnedTick > 0 && fact.LearnedTick >= stock.LearnedTick && fact.LearnedTick <= game.Tick, "facts have actual native completion chronology");
            c.Throws<InvalidOperationException>(() => fact.Data.Set("classification", JsonValue.Of("changed")), "view payload is immutable");
            c.Throws<InvalidOperationException>(() => fact.Data.Get("layers").Add(JsonValue.Of("changed")), "nested view payload is immutable");
            ICompanyKnowledge ledger = game.World.Services.Get<ICompanyKnowledge>(); ulong hash = game.Hash();
            JsonValue copy = stock.Data.Clone();
            c.Equal(Code.None, ledger.Record(new PlayerId(0), stock.Provider, stock.Subject, copy), "identical record is idempotent");
            c.Equal(hash, game.Hash(), "identical record changes neither hash nor first learned time");
            copy.Set("quantity_raw", JsonValue.Number("0"));
            c.Equal(KnowledgeCodes.Known, ledger.Record(new PlayerId(0), stock.Provider, stock.Subject, copy), "changed observation cannot silently overwrite known history");
            c.Equal(hash, game.Hash(), "refused replacement changes no state");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "knowledge never becomes another resource balance");
            c.That(!ledger.TryRead(new PlayerId(-2), new PlayerId(0), stock.Provider, stock.Subject, out _), "invalid negative viewer cannot leak learned fact");
            c.That(!game.Seat(1).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), stock.Provider, stock.Subject, out _), "foreign known fact stays private");
            c.That(ledger.TryRead(PlayerId.None, new PlayerId(0), stock.Provider, stock.Subject, out _), "explicit observer reads intentional observer view");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "both provider payloads survive full save/load");
            c.Equal(0, Observations(New(factory)).Count, "another interleaved session starts with no borrowed knowledge");
            KnowledgeDecode(c, game);
            c.That(Research(game, Foundation).Accepted, "technology and discovery share one ledger with ordinary native research");
            Until(game, () => Known(game, Foundation), "shared-ledger research after discovery");
            c.Equal(3, Observations(game).Count, "research adds to same authority alongside typed discovered facts");
            c.Equal(Fix.FromInt(4), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "discovery and research spend native materials separately once");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "mixed company knowledge saves with technology rules, no duplicate codec");
        }

        private static void KnowledgeDecode(Check c, SimGame game)
        {
            var ledger = (CompanyKnowledgeService)game.World.Services.Get<ICompanyKnowledge>();
            var facts = Observations(game); ulong hash = game.Hash();
            foreach (string mode in new[] { "future", "schema", "duplicate", "missing-provider", "bad-key", "invalid-data", "truncated" })
            {
                using var stream = new MemoryStream(); var writer = new StateWriter(stream);
                writer.Count(mode == "duplicate" ? 2 : 1);
                KnowledgeObservation row = facts[0]; PlayerId company = row.Company;
                string provider = mode == "missing-provider" ? "missing" : row.Provider, subject = mode == "bad-key" ? "bad subject" : row.Subject;
                int schema = mode == "schema" ? row.SchemaVersion + 1 : row.SchemaVersion; long tick = mode == "future" ? game.Tick + 1 : row.LearnedTick;
                long observed = mode == "future" ? game.Tick + 1 : row.ObservedTick;
                string data = mode == "invalid-data" ? "{}" : CompanyKnowledgeService.Canonical(row.Data);
                for (int i = 0; i < (mode == "duplicate" ? 2 : 1); i++)
                { writer.Player(ref company); writer.String(ref provider); writer.String(ref subject); writer.Int(ref schema); writer.Long(ref tick); writer.Long(ref observed); writer.String(ref data); }
                byte[] bytes = stream.ToArray();
                c.Throws<CorruptStateException>(() => ledger.Sync(new StateReader(bytes, 0, mode == "truncated" ? bytes.Length - 1 : bytes.Length)), "invalid learned checkpoint refuses: " + mode);
                c.Equal(hash, game.Hash(), "failed learned decode retains entire prior ledger: " + mode);
            }
        }
    }
}
