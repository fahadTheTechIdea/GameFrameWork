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
    public static class StateCodecTests
    {
        private static readonly ModuleVersion V1 = new ModuleVersion(1, 0, 0);
        // Independently authored verification expectations, not values read back from the engine.
        private sealed class ExpectedProtocol
        {
            internal readonly string Build;
            internal readonly int Save, Replay;
            internal readonly IReadOnlyList<(StateCodecKind Kind, string Id, string Owner, int Schema)> Codecs;
            internal ExpectedProtocol(JsonValue value)
            {
                var reader = new JsonReader(value, "current-engine-protocol");
                Build = reader.String("engine_build"); Save = reader.Int("save_format"); Replay = reader.Int("replay_version");
                var rows = reader.Array("state_codecs"); reader.End();
                if (string.IsNullOrWhiteSpace(Build) || Save <= 0 || Replay <= 0 || rows == null || rows.Count == 0)
                    throw reader.Error("explicit positive protocol and nonempty codec expectations required");
                var codecs = new List<(StateCodecKind Kind, string Id, string Owner, int Schema)>();
                var identities = new HashSet<(StateCodecKind,string)>();
                for (int i=0; i<rows.Count; i++)
                {
                    var row = new JsonReader(rows[i], reader.Where + ".state_codecs[" + i + "]");
                    string kind = row.String("kind"), id = row.String("id"), owner = row.String("owner");
                    int schema = row.Int("schema"); row.End();
                    StateCodecKind selected = kind == "component" ? StateCodecKind.Component :
                        kind == "system" ? StateCodecKind.System : kind == "service" ? StateCodecKind.Service :
                        throw row.Error("unknown codec expectation kind");
                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(owner) || schema <= 0 || !identities.Add((selected,id)))
                        throw row.Error("positive schemas, explicit identities and unique kind/id pairs required");
                    codecs.Add((selected,id,owner,schema));
                }
                Codecs = codecs.AsReadOnly();
            }
            internal int Schema(StateCodecKind kind,string id)
                => Codecs.Single(row => row.Kind==kind && row.Id==id).Schema;
        }
        private static string ExpectedDocument()
        {
            using var stream = typeof(StateCodecTests).Assembly.GetManifestResourceStream("tests.current-engine-protocol.json")
                ?? throw new InvalidOperationException("missing independently authored protocol baseline");
            using var reader = new StreamReader(stream); return reader.ReadToEnd();
        }
        private static readonly Lazy<ExpectedProtocol> Current = new Lazy<ExpectedProtocol>(() => new ExpectedProtocol(Json.Parse(ExpectedDocument())));
        internal static string ExpectedBuild => Current.Value.Build;
        internal static int ExpectedSaveFormat => Current.Value.Save;
        internal static int ExpectedWorkBoardSchema => Current.Value.Schema(StateCodecKind.Service,"sim.work");
        internal static int ExpectedWorkSystemSchema => Current.Value.Schema(StateCodecKind.System,"sim.work");
        internal static int ExpectedLogisticsSchema => Current.Value.Schema(StateCodecKind.Service,"sim.logistics");
        internal static int ExpectedAutomationSchema => Current.Value.Schema(StateCodecKind.System,"sim.automation");
        private const string NativeProtocolCase = "Current native protocol: authored expectations and physical selected-role checkpoints";
        private const string ServiceWriteCase = "State codecs: knowledge contract writes cannot mutate read-only encoding";
        private const string PlanWriteCase = "State codecs: plan board contracts cannot mutate read-only encoding";

        public static void Register()
        {
            TestRunner.Add(NativeProtocolCase, CurrentNativeProtocol);
            TestRunner.Add(ServiceWriteCase, ServiceWrites);
            TestRunner.Add(PlanWriteCase, PlanWrites);
            TestRunner.Add("State codecs: explicit declarations refuse before initialization", Invalid);
            TestRunner.Add("State codecs: captured identity, aliases and scheduling", Captured);
            TestRunner.Add("State codecs: authored parts use active declared owners", Authored);
            TestRunner.Add("State codecs: declaration drift refuses before payload restoration", Drift);
            TestRunner.Add("State codecs: immutable facts, isolated sessions and saved twins", Durability);
        }
        private static string RawPlanFacts(WorkBoard board)
        {
            // Deliberate raw negative-protocol lens, not another engine codec or checkpoint.
            object Field(object value, string name) => value.GetType().GetField(name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).GetValue(value);
            var facts = new List<string>();
            foreach (string name in new[] { "_nextPlan", "_nextTemplate", "_nextOrder", "_nextJob" }) facts.Add(name + ":" + Field(board, name));
            foreach (string name in new[] { "_plans", "_storedPlanTemplates" })
            {
                facts.Add(name);
                foreach (object row in (System.Collections.IEnumerable)Field(board, name))
                {
                    facts.Add("row");
                    foreach (var field in row.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).OrderBy(field => field.Name))
                    {
                        object value = field.GetValue(row);
                        string text = value is IEnumerable<long> ids ? string.Join(",", ids)
                            : value is PlanTemplate template ? template.ToJson().ToString() : value?.ToString();
                        facts.Add(field.Name + ":" + text);
                    }
                }
            }
            foreach (string name in new[] { "_planById", "_activePlanByOrder", "_orders", "_orderById", "_jobs" })
                facts.Add(name + ":" + ((System.Collections.ICollection)Field(board, name)).Count);
            return string.Join("|", facts);
        }
        private static void PlanWrites(Check c)
        {
            // Explicit codec fault injection; normal physical player acceptance remains elsewhere.
            string[] writes = { "store", "revise", "remove", "submit", "configure", "move", "dependencies", "cancel", "repeat", "place", "submit-group", "configure-group", "move-group", "cancel-group", "repeat-group", "retire", "post", "restore" };
            foreach (string entry in new[] { "save", "hash", "breakdown", "observation" })
                foreach (string mode in writes.Concat(writes.Select(write => write + "-throw")).Concat(new[] { "read", "healthy" }))
                {
                    string action = mode.EndsWith("-throw", StringComparison.Ordinal) ? mode.Substring(0, mode.Length - 6) : mode;
                    var probe = new ProbeModule("probe.encoding", V1, compose: composer => composer.Services.Bind<IProbeFirst>(new MutableCodecState()));
                    var factory = ConstructionPlanTests.Points(templateDefinitions: FrameworkProofContent.Read("plan-templates.json"), extraModules: new[] { probe },
                        extraDeclarations: "[{\"id\":\"probe.encoding\",\"version\":\"1.0.0\",\"documents\":[]}]",
                        scenarioEdit: scenario => scenario.Get("modules").Add(JsonValue.Of(probe.Id)));
                    var game = ConstructionPlanTests.NewPoints(factory); var idle = ConstructionPlanTests.NewPoints(factory);
                    var board = game.World.Services.Get<WorkBoard>(); IPlanBoard plans = board; var company = new PlayerId(0);
                    var codec = (MutableCodecState)game.World.Services.Get<IProbeFirst>();
                    byte[] empty; using (var bytes = new MemoryStream()) { var writer = new StateWriter(bytes); board.Sync(writer); writer.Flush(); empty = bytes.ToArray(); }
                    var template = game.World.Content.Get<PlanTemplateDef>("point.pair").Template;
                    JsonValue At(int value) => Json.Parse("{\"space\":\"field.points\",\"position\":" + value + "}");
                    var stored = plans.StorePlanTemplate(company, 0, 0, "original", template);
                    var first = plans.PlacePlanTemplate(company, template, At(12), "first");
                    var other = plans.PlacePlanTemplate(company, template, At(32), "other");
                    var expanded = plans.ExpandPlanTemplate(company, template, At(72), "fresh", out var intent);
                    if (!expanded.Ready) throw new InvalidOperationException("fixture expansion refused: " + expanded.Blocker);
                    if (action == "repeat" || action == "retire") plans.CancelPlan(company, new PlanCancellationSelection(false, new[] { first[0] }));
                    if (action == "repeat-group") plans.CancelPlanGroup(company, "first");
                    string beforeFacts = RawPlanFacts(board); ulong before = game.Hash(), isolated = idle.Hash(); int calls = 0;
                    var original = new InvalidOperationException("proof.plan-encoding-original-error");
                    codec.BeforeWrite = () => {
                        codec.BeforeWrite = null; calls++;
                        try
                        {
                            switch (action)
                            {
                                case "store": plans.StorePlanTemplate(company, 0, 0, "extra", template); break;
                                case "revise": plans.StorePlanTemplate(company, stored.Id, stored.Revision, "renamed", template); break;
                                case "remove": plans.RemovePlanTemplate(company, stored.Id, stored.Revision); break;
                                case "submit": plans.SubmitPlan(company, intent.Entries[0]); break;
                                case "configure": plans.ConfigurePlan(company, first[0], "edited", 99, true); break;
                                case "move": plans.MovePlan(company, first[0], 0); break;
                                case "dependencies": plans.ConfigurePlanDependencies(company, new PlanDependencyChanges(new[] { new PlanDependencyChange(first[1], new[] { first[0] }) })); break;
                                case "cancel": plans.CancelPlan(company, new PlanCancellationSelection(false, new[] { first[0] })); break;
                                case "repeat": plans.RepeatPlan(company, first[0]); break;
                                case "place": plans.PlacePlanTemplate(company, template, At(72), "fresh"); break;
                                case "submit-group": plans.SubmitPlanGroup(company, intent); break;
                                case "configure-group": plans.ConfigurePlanGroup(company, "first", 99, true); break;
                                case "move-group": plans.MovePlanGroup(company, "first", 0); break;
                                case "cancel-group": plans.CancelPlanGroup(company, "first"); break;
                                case "repeat-group": plans.RepeatPlanGroup(company, "first", "repeated"); break;
                                case "retire": plans.RetirePlanHistory(company, new PlanHistorySelection(new[] { first[0] })); break;
                                case "post": ((IActivityExecutionOrders)board).PostActivity(((IActivityCatalog)board).ResolvePlan("activity:point.tower.build-work"), company,
                                    game.Seat(0).Snapshot.Entities.Single(entity => entity.Def == "source").Id, 70); break;
                                case "restore": board.Sync(new StateReader(empty, 0, empty.Length)); break;
                                case "read": var rows = new List<StoredPlanTemplateView>(); plans.CopyStoredPlanTemplates(company, rows); c.That(rows.Count == 1, "nested private template read remains legal"); break;
                            }
                        }
                        catch { if (mode.EndsWith("-throw", StringComparison.Ordinal)) throw original; throw; }
                        if (mode.EndsWith("-throw", StringComparison.Ordinal)) throw original;
                    };
                    Exception observed = null; bool returned = false;
                    try {
                        if (entry == "save") game.Save(); else if (entry == "hash") game.Hash(); else if (entry == "breakdown") game.HashBreakdown();
                        else EntityObservation.ReadProvider(game.World, codec, null, () => true);
                        returned = true;
                    } catch (Exception error) { observed = error; }
                    string context = entry + "/" + mode;
                    c.That(calls == 1 && game.World.ObservationCaptureDepth == 0, "plan codec once, capture depth released " + context);
                    c.That(beforeFacts == RawPlanFacts(board), "encoding publishes no plan/template/history/identity prefix " + context);
                    if (mode == "read" || mode == "healthy")
                    {
                        c.That(returned && observed == null && game.Fault == null, "healthy plan encoding allowed " + context);
                        c.Equal(before, game.Hash(), "healthy plan encoding pure " + context);
                        var restored = factory.Load(game.Save()); c.Equal(before, restored.Hash(), "normal restore remains writable " + context);
                        var restoredBoard = restored.World.Services.Get<IPlanBoard>(); restoredBoard.RemovePlanTemplate(company, stored.Id, stored.Revision);
                        c.That(restoredBoard.StorePlanTemplate(company, 0, 0, "replacement", template).Id > stored.Id, "normal restored library writes retain monotonic identity");
                    }
                    else
                    {
                        c.That(!returned && observed != null && game.Fault?.Phase == (entry == "observation" ? "observation.provider" : "checkpoint.encode"), "plan mutation cannot return healthy state " + context);
                        if (mode.EndsWith("-throw", StringComparison.Ordinal)) c.That(ReferenceEquals(original, observed) && game.Fault?.Message == original.GetType().Name + ": " + original.Message, "original plan codec cause retained " + context);
                        var firstFault = game.Fault;
                        c.Throws<InvalidOperationException>(() => game.Save(), "plan encoder fault cannot save healthy " + context);
                        c.Throws<InvalidOperationException>(() => game.Hash(), "plan encoder fault cannot hash healthy " + context);
                        c.That(game.Update() == 0 && ReferenceEquals(firstFault, game.Fault) && calls == 1, "plan encoder fault stops retries/update " + context);
                    }
                    c.Equal(isolated, idle.Hash(), "plan mutation remains session-local " + context);
                    idle.Update(); c.That(idle.Fault == null, "unrelated plan session remains playable " + context);
                }
        }
        private static void ServiceWrites(Check c)
        {
            // Explicit corrupt-codec protocol, not a player workflow or host-side learning rule.
            foreach (string entry in new[] { "save", "hash", "breakdown", "observation" })
                foreach (string mode in new[] { "record", "share", "forget", "restore", "record-throw", "share-throw", "forget-throw", "restore-throw", "read", "healthy" })
                {
                    var authored = Json.Parse(FrameworkProofContent.Read("exploration-bindings.json"));
                    var knowledge = JsonValue.NewObject();
                    foreach (string section in new[] { "knowledge_facts", "knowledge_sharing_policies", "knowledge_forgetting_policies" })
                        knowledge.Set(section, authored.Get(section));
                    var probe = new ProbeModule("probe.encoding", V1, compose: composer => composer.Services.Bind<IProbeFirst>(new MutableCodecState()));
                    var factory = ConstructionPlanTests.Points(knowledgeDefinitions: knowledge.ToString(),
                        extraModules: new[] { probe }, extraDeclarations: "[{\"id\":\"probe.encoding\",\"version\":\"1.0.0\",\"documents\":[]}]",
                        scenarioEdit: scenario => { scenario.Get("modules").Add(JsonValue.Of(CompanyKnowledgeModule.ModuleId)); scenario.Get("modules").Add(JsonValue.Of(probe.Id)); });
                    var game = ConstructionPlanTests.NewPoints(factory); var idle = ConstructionPlanTests.NewPoints(factory);
                    var owner = (CompanyKnowledgeService)game.World.Services.Get<ICompanyKnowledge>();
                    var codec = (MutableCodecState)game.World.Services.Get<IProbeFirst>();
                    byte[] empty;
                    using (var bytes = new MemoryStream()) { var writer = new StateWriter(bytes); owner.Sync(writer); writer.Flush(); empty = bytes.ToArray(); }
                    foreach (string subject in new[] { "binding.reset-known", "binding.retain-known" })
                    {
                        var definition = game.World.Content.Get<IKnowledgeSubjectDefinition>(subject);
                        c.Equal(Code.None, owner.Record(new PlayerId(0), "sim.knowledge.facts", subject, definition.Observe(game.World, null)), "normal record before encoding");
                    }
                    var ledgerField = typeof(CompanyKnowledgeService).GetField("_known", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    object ledger = ledgerField.GetValue(owner); ulong before = game.Hash(), isolated = idle.Hash(); int calls = 0;
                    var original = new InvalidOperationException("proof.knowledge-encoding-original-error");
                    codec.BeforeWrite = () => {
                        codec.BeforeWrite = null; calls++;
                        try
                        {
                            Code result = Code.None;
                            if (mode.StartsWith("record", StringComparison.Ordinal)) result = owner.Record(new PlayerId(1), "sim.knowledge.facts", "binding.reset-known", game.World.Content.Get<IKnowledgeSubjectDefinition>("binding.reset-known").Observe(game.World, null));
                            if (mode.StartsWith("share", StringComparison.Ordinal)) result = game.World.Services.Get<IKnowledgeSharing>().Share(new PlayerId(0), new PlayerId(1), "binding.share");
                            if (mode.StartsWith("forget", StringComparison.Ordinal)) result = game.World.Services.Get<IKnowledgeForgetting>().Forget(new PlayerId(0), "binding.forget");
                            if (mode.StartsWith("restore", StringComparison.Ordinal)) owner.Sync(new StateReader(empty, 0, empty.Length));
                            if (!result.IsNone) throw new InvalidOperationException("fixture refused before mutation: " + result);
                            if (mode == "read") c.That(owner.TryRead(new PlayerId(0), new PlayerId(0), "sim.knowledge.facts", "binding.reset-known", out _), "nested immutable service read allowed");
                        }
                        catch { if (mode.EndsWith("-throw", StringComparison.Ordinal)) throw original; throw; }
                        if (mode.EndsWith("-throw", StringComparison.Ordinal)) throw original;
                    };
                    Exception observed = null; bool returned = false;
                    try {
                        if (entry == "save") game.Save(); else if (entry == "hash") game.Hash();
                        else if (entry == "breakdown") game.HashBreakdown();
                        else EntityObservation.ReadProvider(game.World, codec, null, () => true);
                        returned = true;
                    }
                    catch (Exception error) { observed = error; }
                    string context = entry + "/" + mode;
                    c.That(calls == 1 && game.World.ObservationCaptureDepth == 0, "single actual codec and released capture depth " + context);
                    c.That(ReferenceEquals(ledger, ledgerField.GetValue(owner)), "read-only encoding publishes no ledger prefix " + context);
                    if (mode == "read" || mode == "healthy")
                    {
                        c.That(returned && observed == null && game.Fault == null, "healthy original encoding remains supported " + context);
                        c.Equal(before, game.Hash(), "healthy encoding pure " + context);
                        var restored = factory.Load(game.Save()); c.Equal(before, restored.Hash(), "normal restore retains actual learned facts " + context);
                        c.Equal(Code.None, restored.World.Services.Get<IKnowledgeSharing>().Share(new PlayerId(0), new PlayerId(1), "binding.share"), "normal sharing after restore writable");
                        c.Equal(Code.None, restored.World.Services.Get<IKnowledgeForgetting>().Forget(new PlayerId(0), "binding.forget"), "normal forgetting after restore writable");
                        c.That(restored.World.Services.Get<ICompanyKnowledge>().TryRead(new PlayerId(1), new PlayerId(1), "sim.knowledge.facts", "binding.reset-known", out _), "recipient retains shared history after sender forgets");
                    }
                    else
                    {
                        c.That(!returned && observed != null && game.Fault?.Phase == (entry == "observation" ? "observation.provider" : "checkpoint.encode"), "mutating service codec cannot publish a healthy result " + context);
                        if (mode.EndsWith("-throw", StringComparison.Ordinal)) c.That(ReferenceEquals(original, observed) && game.Fault?.Message == original.GetType().Name + ": " + original.Message, "original codec cause retained " + context);
                        var first = game.Fault;
                        c.Throws<InvalidOperationException>(() => game.Save(), "failed service encoder cannot save healthy " + context);
                        c.Throws<InvalidOperationException>(() => game.Hash(), "failed service encoder cannot hash healthy " + context);
                        c.That(game.Update() == 0 && ReferenceEquals(first, game.Fault) && calls == 1, "faulted encoder cannot retry or advance " + context);
                    }
                    c.Equal(isolated, idle.Hash(), "service encoding is session-local " + context);
                    idle.Update(); c.That(idle.Fault == null, "unrelated service session remains playable " + context);
                }
        }
        private static void CurrentNativeProtocol(Check c)
        {
            var expected = Current.Value;
            c.Equal(expected.Build, EngineInfo.Build, "independent authored build matches engine");
            c.Equal(expected.Save, EngineInfo.SaveFormat, "independent authored save protocol matches engine");
            c.Equal(expected.Replay, ReplayFormat.Version, "independent authored replay protocol matches engine");
            foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                value => value.Remove("engine_build"),
                value => value.Set("save_format", JsonValue.Number("0")),
                value => value.Set("replay_version", JsonValue.Number("-1")),
                value => value.Get("state_codecs")[0].Set("kind", JsonValue.Of("unknown")),
                value => value.Get("state_codecs")[0].Set("schema", JsonValue.Number("0")),
                value => value.Get("state_codecs")[0].Remove("owner"),
                value => value.Get("state_codecs").Add(value.Get("state_codecs")[0].Clone()),
                value => value.Set("inferred_defaults", JsonValue.Of(true))
            })
            {
                var value = Json.Parse(ExpectedDocument()); edit(value);
                c.Throws<ContentException>(() => new ExpectedProtocol(value), "invalid verification authoring has no defaults");
            }
            c.Throws<NotSupportedException>(() => ((IList<(StateCodecKind Kind,string Id,string Owner,int Schema)>)expected.Codecs).Clear(),
                "expected codec rows are immutable");
            var factory = FacilitySelectionTests.Factory(edit: root =>
            {
                // This protocol fixture uses only machine.a; other actors must not block its physical egress.
                StartupContentAuthoring.Position(root, "machine.b", JsonValue.Number("80"));
                StartupContentAuthoring.Position(root, "machine.c", JsonValue.Number("82"));
                StartupContentAuthoring.Position(root, "machine.d", JsonValue.Number("84"));
            });
            SimGame NewNative() => factory.NewGame(new NewGameRequest { Scenario="points", Seed=6302,
                Seats={Controller.Human,Controller.Human} });
            var native = NewNative();
            var signals = InventorySignalTests.New(InventorySignalTests.Factory());
            ulong nativeHash=native.Hash(), signalsHash=signals.Hash();
            foreach(var row in expected.Codecs)
            {
                var matches = new[] { native, signals }.SelectMany(game => game.Seat(0).View.StateCodecs)
                    .Where(codec => codec.Kind==row.Kind && codec.Id==row.Id).ToArray();
                c.That(matches.Length>0, "expected native codec is actually composed: "+row.Kind+":"+row.Id);
                foreach(var codec in matches)
                {
                    c.Equal(row.Schema,codec.SchemaVersion,"captured native schema follows independent expectation "+row.Id);
                    c.Equal(row.Owner,codec.Owner,"captured composing owner follows independent expectation "+row.Id);
                }
            }
            c.Equal(nativeHash,native.Hash(),"native declaration reads are pure");
            c.Equal(signalsHash,signals.Hash(),"optional automation metadata reads are pure");
            c.Equal(nativeHash,factory.Load(native.Save()).Hash(),"native protocol restores before player work");
            c.Equal(expected.Save,SaveGame.Describe(native.Save()).Format,"saved envelope declares authored current format");
            var retired = native.Save().Take(native.Save().Length-8).ToArray();
            using(var stream=new MemoryStream())
            {
                var writer=new StateWriter(stream); int version=expected.Save-1; writer.Int(ref version); writer.Flush();
                Buffer.BlockCopy(stream.ToArray(),0,retired,8,4);
            }
            var artifact=new byte[retired.Length+8]; Buffer.BlockCopy(retired,0,artifact,0,retired.Length);
            // Independently reseal this adversarial SAVE fixture: its raw-byte checksum
            // is deliberately not the replay protocol's length-prefixed checksum.
            ulong checksum=14695981039346656037UL;
            unchecked { foreach(byte value in retired) { checksum^=value; checksum*=1099511628211UL; } }
            for(int i=0;i<8;i++) artifact[retired.Length+i]=(byte)(checksum>>(i*8));
            string refusal=null;
            try { factory.Load(artifact); } catch(CorruptStateException error) { refusal=error.Message; }
            c.That(refusal=="unsupported engine build/save format","checksum-valid retired protocol is rejected, not migrated: "+refusal);
            c.Equal(nativeHash,native.Hash(),"retired layout refusal changes no existing session");
            foreach(bool exact in new[]{false,true})
            {
                var game=NewNative(); var isolated=NewNative(); ulong untouched=isolated.Hash();
                EntityId Id(string prototype) => game.Seat(0).Snapshot.Entities.Single(e=>e.Def==prototype).Id;
                WorkView Work(SimGame session) => session.Seat(0).View.Feature<WorkView>();
                PlanView Plan(SimGame session,long id)
                {
                    var rows=new List<PlanView>(); Work(session).CopyPlans(rows); return rows.Single(p=>p.Id==id);
                }
                ActivityNodeView Node(SimGame session,long id)
                {
                    var rows=new List<ActivityNodeView>();
                    if(!Work(session).TryCopyNodes(Plan(session,id).Order,rows))throw new InvalidOperationException("missing current-codec work");
                    return rows.Single(n=>n.Id=="service");
                }
                Fix Stock(SimGame session)
                {
                    var rows=new List<Facet>(); session.Seat(0).View.Inspect(Id("source"),rows);
                    return rows.Where(f=>f.Key==Storage.FacetStock && f.Subject=="supply").Aggregate(Fix.Zero,(a,b)=>a+b.Value);
                }
                void Update(SimGame twin=null)
                {
                    game.Update();twin?.Update();
                    if(game.Fault!=null || twin?.Fault!=null || twin!=null && game.Hash()!=twin.Hash())
                        throw new InvalidOperationException("protocol clock="+game.Tick+"/fault="+game.Fault+"/twin="+twin?.Fault);
                }
                void Wait(Func<bool> done,string stage,SimGame twin=null)
                {
                    for(int i=0;i<1200 && !done();i++)Update(twin);
                    if(!done())
                    {
                        var jobs=new List<WorkJobView>();Work(game).CopyJobs(jobs);
                        throw new InvalidOperationException(stage+"/clock="+game.Tick+"/jobs="+string.Join(";",
                            jobs.Select(j=>j.Id+"/"+j.State+"/"+j.Blocker+"/"+j.Worker)));
                    }
                }
                using(var recorder=game.RecordReplay(new ReplayLimits(8*1024*1024,5000)))
                {
                    var role=exact ? new WorkerRoleId("inspect") : WorkerRoleId.None;
                    var selection=new ActivityWorkerSelection(new[]{new ActivityWorkerBinding("service",Id("machine.a"),role)});
                    var intent=new PlanIntent("activity:facility.service",ActivityWorkerSelection.CreateArguments(Id("source"),selection),null,60,false);
                    c.Equal(exact ? "inspect" : null,intent.Arguments.Get("workers")[0].Get("role").Text,
                        "one JSON selection explicitly authors exact or unrestricted role");
                    var command=new SubmitPlanCommand {Intent=intent};
                    ulong before=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(command),"ordinary selected-role plan preview");
                    c.Equal(before,game.Hash(),"preview changes no current-codec state");
                    int sequence=game.Seat(0).Submit(command);
                    var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"pending selected-role command checkpoint");
                    Update(twin);long cursor=0;var answers=new List<CommandResult>();game.Seat(0).ReadResults(ref cursor,answers);
                    var answer=answers.Single(a=>a.Sequence==sequence);c.That(answer.Accepted,"normal saved pending intent admitted: "+answer.Refusal);
                    if(!answer.Accepted)throw new InvalidOperationException("selected-role admission "+answer.Refusal);
                    long plan=answer.Arg0;Wait(()=>Plan(game,plan).Order>0,"ordinary plan activation",twin);
                    if(exact)
                    {
                        ulong observed=game.Hash();
                        c.Equal(Code.None,Work(game).ReadRecurringFacilities(Id("machine.a"),new Code("fleet.service"),out var facilities),
                            "exact pending player intent produces a normal facility query, not an update fault");
                        var same=facilities.Candidates.Single(candidate=>candidate.Target==Id("source"));
                        c.That(!same.Ready && !same.Readiness.Blocker.IsNone,"existing selected-role work is a typed facility blocker");
                        c.Equal(observed,game.Hash(),"blocked selected-role facility query remains pure");
                    }
                    Wait(()=>Node(game,plan).Worker.IsValid,"ordinary actual machine acquisition",twin);
                    c.Equal(role,Node(game,plan).RequestedRole,"current selection codec retains explicit role intent");
                    c.Equal(new WorkerRoleId("inspect"),Node(game,plan).SelectedRole,"actual compatible physical job role");
                    c.Equal(Id("machine.a"),Node(game,plan).RequestedWorker,"same selected machine survives codec");
                    c.Equal(Fix.FromInt(32),Stock(game),"claims do not spend physical stock");
                    c.Equal(Codes.NotFound,game.Seat(1).View.Feature<WorkView>().ReadRecurringFacilities(Id("machine.a"),new Code("fleet.service"),out _),
                        "metadata does not expose private gameplay selections");
                    twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"claimed role saves through board codec");
                    var detached=Node(game,plan);
                    Wait(()=>Node(game,plan).Progress.WholeTicks>=3,"actual service progress",twin);
                    c.Equal(Fix.FromInt(29),Stock(game),"physical work spends exactly three native spares");
                    c.Equal(0L,detached.Progress.WholeTicks,"retained role/progress view is detached");
                    game.Clock.Pause();twin.Clock.Pause();ulong paused=game.Hash();long step=game.Clock.StepIndex;
                    Update(twin);c.Equal(paused,game.Hash(),"sole-clock pause retains role/progress");
                    c.Equal(step,game.Clock.StepIndex,"no verification-owned clock");
                    twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"paused working role checkpoint");
                    game.Clock.Resume();twin.Clock.Resume();
                    Wait(()=>Plan(game,plan).State==PlanState.Done,"physical work/departure/settling",twin);
                    c.Equal(Fix.FromInt(29),Stock(game),"completion cannot duplicate material spending");
                    var condition=game.Seat(0).View.Feature<EquipmentConditionView>();
                    c.Equal(Code.None,condition.Read(Id("machine.a"),new Code("integrity"),out var value),"native condition read");
                    c.Equal(Fix.FromInt(10),value.Value,"physical engine work alone repairs");
                    c.Equal(exact ? "inspect" : null,Plan(game,plan).Arguments.Get("workers")[0].Get("role").Text,
                        "completed plan retains exact or explicitly unrestricted intent after live order archival");
                    c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"ended role receipts restore");
                    c.Equal(untouched,isolated.Hash(),"protocol expectations carry no mutable game state");
                    var bytes=recorder.Finish();var description=ReplayVerifier.Describe(bytes,new ReplayLimits(8*1024*1024,5000));
                    c.Equal(expected.Build,description.Build,"replay embeds authored current engine identity");
                    c.Equal(expected.Save,description.SaveFormat,"replay embeds authored current save layout");
                    var replay=new ReplayVerifier().Verify(factory,bytes,new ReplayLimits(8*1024*1024,5000));
                    c.That(replay.Success && replay.Game!=null && replay.Game.Hash()==game.Hash(),"canonical selected-role replay: "+replay.Failure+"/"+replay.Detail);
                }
            }
        }

        private static ProbeModule Module(Action<WorldComposer> compose = null, Action<ContentTypes> content = null)
            => new ProbeModule("probe", V1, content: content, compose: compose);
        private static GameFactory Factory(MutableCodecSystem system, MutableCodecState state, int componentSchema = 1, int order = 10)
            => CompositionProbe.Factory(Module(composer =>
            {
                composer.Services.Bind<IProbeFirst>(state);
                composer.Services.Bind<IProbeSecond>(state);
                composer.AddSystem(system, order);
            }, types =>
            {
                types.Component<CodecPartDef>("probe.part", componentSchema);
            }), AuthoredCodecContent("probe.part"));
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "probe", Seed = 1 });

        private static void Invalid(Check c)
        {
            var spy = new InitializeProbe();
            foreach (string id in new[] { "", "Upper", "path/codec", " " })
            {
                c.Throws<ArgumentException>(() => CompositionProbe.New(Module(composer =>
                {
                    composer.AddSystem(spy, 1);
                    composer.Services.Bind<IProbeFirst>(new MutableCodecState { SuppliedId = id });
                })), "invalid service identity refuses: " + id);
                c.Throws<ArgumentException>(() => CompositionProbe.New(Module(composer =>
                    composer.AddSystem(new MutableCodecSystem { SuppliedId = id }, 10))), "invalid system identity refuses: " + id);
                c.Throws<ArgumentException>(() => CompositionProbe.Factory(Module(content: types =>
                    types.Component<CodecPartDef>(id, 1))), "invalid component identity refuses: " + id);
            }
            foreach (int version in new[] { 0, -1 })
            {
                c.Throws<ArgumentOutOfRangeException>(() => CompositionProbe.New(Module(composer =>
                    composer.Services.Bind(new MutableCodecState { SuppliedVersion = version }))), "service schema must be positive");
                c.Throws<ArgumentOutOfRangeException>(() => CompositionProbe.New(Module(composer =>
                    composer.AddSystem(new MutableCodecSystem { SuppliedVersion = version }, 10))), "system schema must be positive");
                c.Throws<ArgumentOutOfRangeException>(() => CompositionProbe.Factory(Module(content: types =>
                    types.Component<CodecPartDef>("probe.part", version))), "component schema must be positive");
            }
            c.Throws<ArgumentOutOfRangeException>(() => Schedule.Every(5, -1), "negative schedule phase refuses before arithmetic");
            c.Throws<ArgumentException>(() => CompositionProbe.New(Module(composer =>
                composer.AddSystem(new MutableCodecSystem { SuppliedSchedule = default }, 10))), "default schedule refuses at composition");
            c.Equal(0, spy.Initializations, "invalid codecs never initialize any system");
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(Module(composer =>
            {
                composer.Services.Bind<IProbeFirst>(new MutableCodecState());
                composer.Services.Bind<IProbeSecond>(new MutableCodecState());
            })), "distinct service instances cannot share a codec ID");
        }

        private static void Captured(Check c)
        {
            var state = new MutableCodecState();
            var system = new MutableCodecSystem();
            var factory = CompositionProbe.Factory(Module(composer =>
            {
                composer.Services.Bind<IProbeFirst>(state);
                state.SuppliedId = "invalid/service"; state.SuppliedVersion = 0;
                composer.Services.Bind<IProbeSecond>(state);
                composer.AddSystem(system, 10);
                system.SuppliedId = "invalid/system"; system.SuppliedVersion = 0; system.SuppliedSchedule = default;
            }));
            SimGame game = New(factory);
            game.RunTicks(24);
            c.Equal(3, system.Updates, "captured periodic schedule executes exactly at 3, 13 and 23");
            c.Equal(30, state.Value, "captured span reaches the ordinary service provider");
            var codecs = game.Seat(0).View.StateCodecs;
            var components=codecs.Where(codec=>codec.Kind==StateCodecKind.Component).ToList();
            c.Equal(game.World.Content.ComponentCodecs.Count,components.Count,"captured catalog retains every original declared component codec");
            foreach(var declared in game.World.Content.ComponentCodecs)
                c.That(components.Any(codec=>ReferenceEquals(codec,declared)),"component codec is the actual declaration, not another registry");
            c.That(components.Any(codec=>codec.Id=="tile_presence"),"current fixture explicitly retains its declared physical presence codec");
            var owners=codecs.Where(codec=>codec.Kind!=StateCodecKind.Component).ToList();
            c.Equal(2, owners.Count, "two aliases have exactly one persistent codec");
            c.Equal("probe.system", owners[0].Id, "system identity is captured before getter mutation");
            c.Equal("probe.state", owners[1].Id, "service identity is captured at first binding");
            c.Equal(1, owners[1].SchemaVersion, "alias reuses captured schema without rereading");
            c.Equal("probe", owners[1].Owner, "captured composing owner remains authoritative");
            ulong before = game.Hash();
            game.Save(); game.World.HashBreakdown();
            c.Equal(before, game.Hash(), "save and hash do not reread or change mutable declarations");
            c.Equal(1, system.IdReads, "system identity getter read once");
            c.Equal(1, system.VersionReads, "system schema getter read once");
            c.Equal(1, system.ScheduleReads, "system schedule getter read once");
            c.Equal(1, state.IdReads, "service identity getter read once including aliases");
            c.Equal(1, state.VersionReads, "service schema getter read once including aliases");
            system.Throw = true; game.RunTicks(10);
            c.Equal("probe.system", game.World.Fault.Phase, "fault phase uses captured identity");
        }

        internal static string AuthoredCodecContent(string key)
        {
            var root = Json.Parse(CompositionProbe.Definitions);
            root.Set("entities", Json.Parse("[{\"id\":\"probe.authored\",\"kind\":\"marker\",\"components\":{\"" + key + "\":{}}}]"));
            return root.ToString();
        }
        private static void Authored(Check c)
        {
            GameFactory factory = Factory(new MutableCodecSystem(), new MutableCodecState(), 7);
            SimGame game = New(factory);
            EntityDef entity = game.World.Content.Get<EntityDef>("probe.authored");
            ComponentDef part = entity.Components[0];
            c.Equal("probe.part", part.Key, "authored parts receive the declared stable component identity");
            c.Equal(7, part.StateCodec.SchemaVersion, "authored parts use declared schema, not a fabricated default");
            c.Equal("probe", part.StateCodec.Owner, "authored part retains its codec owner");
            c.That(ReferenceEquals(part.StateCodec, game.Seat(0).View.StateCodecs[0]), "diagnostics project the same declaration, not another registry");
            c.Throws<ContentException>(() => New(CompositionProbe.Factory(Module(), AuthoredCodecContent("unknown.part"))), "authored parts cannot bypass declaration");
            var definition = new GameDefinition("probe", new GameModule[]
            {
                Module(),
                new ProbeModule("provider", V1, content: types => types.Component<CodecPartDef>("provider.part", 1))
            });
            const string manifest = "{ \"schema\": 1, \"modules\": [{ \"id\": \"probe\", \"version\": \"1.0.0\", \"documents\": [\"probe.json\"] }, { \"id\": \"provider\", \"version\": \"1.0.0\", \"documents\": [] }] }";
            var content = ContentSet.Load(new PreloadedFileSource().Add("manifest.json", manifest)
                .Add("probe.json", AuthoredCodecContent("provider.part")), definition.ContentTypes);
            c.Throws<ContentException>(() => content.Resolve(new[] { "probe" }), "inactive codec provider cannot supply authored parts");
            c.Equal("provider", content.Resolve(new[] { "probe", "provider" }).Get<EntityDef>("probe.authored").Components[0].StateCodec.Owner,
                "explicit active provider owns authored component codec");
        }

        private static void Drift(Check c)
        {
            SimGame original = New(Factory(new MutableCodecSystem(), new MutableCodecState()));
            original.RunTicks(24);
            byte[] saved = original.Save();
            for (int change = 0; change < 7; change++)
            {
                var state = new MutableCodecState(); var system = new MutableCodecSystem();
                if (change == 0) state.SuppliedVersion = 2;
                if (change == 1) system.SuppliedVersion = 2;
                if (change == 3) state.SuppliedId = "probe.other";
                if (change == 4) system.SuppliedSchedule = Schedule.Every(10, 4);
                if (change == 6) system.SuppliedId = "probe.other";
                GameFactory changed = Factory(system, state, change == 2 ? 2 : 1, change == 5 ? 11 : 10);
                c.Throws<CorruptStateException>(() => changed.Load(saved), "declaration drift refuses: " + change);
                c.Equal(0, system.Initializations, "no initialization before declaration match: " + change);
                c.Equal(0, state.ReadCalls, "no service payload restored before declaration match: " + change);
                c.Equal(0, system.ReadCalls, "no system payload restored before declaration match: " + change);
            }
            GameFactory missing = CompositionProbe.Factory(Module());
            c.Throws<CorruptStateException>(() => missing.Load(saved), "missing codec declarations refuse before payload restore");
            c.Equal(EngineInfo.SaveFormat, SaveGame.Describe(saved).Format, "only the current breaking layout is written");
        }

        private static void Durability(Check c)
        {
            var aSystem = new MutableCodecSystem(); var aState = new MutableCodecState();
            var bSystem = new MutableCodecSystem(); var bState = new MutableCodecState();
            SimGame a = New(Factory(aSystem, aState));
            GameFactory bf = Factory(bSystem, bState);
            a.RunTicks(24);
            SimGame b = bf.Load(a.Save());
            c.Equal(a.Hash(), b.Hash(), "external system/service/authored part restore through their sole Sync paths");
            c.Equal(aState.Value, bState.Value, "aliased provider restores its state once");
            c.Equal(1, bState.ReadCalls, "service aliases restore once");
            c.Equal(1, bSystem.ReadCalls, "system restores once");
            c.Equal(1, bSystem.Initializations, "valid restoration initializes once after declaration validation");
            c.Equal(8, b.World.Entities.Components<CodecPart>()[0].Value, "authored component state is restored, not reset");
            var codecs = a.Seat(0).View.StateCodecs;
            c.Equal(3, codecs.Count, "component/system/service declarations share one manifest");
            c.Equal(StateCodecKind.Component, codecs[0].Kind, "metadata uses deterministic kind then ID order");
            c.Equal(typeof(CodecPartDef), codecs[0].ContractType, "component contract is its declared definition type");
            c.Throws<NotSupportedException>(() => ((IList<StateCodecRegistration>)codecs).Clear(), "metadata collection is read-only");
            foreach (var property in typeof(StateCodecRegistration).GetProperties())
                c.That(property.SetMethod == null, "metadata property is immutable: " + property.Name);
            ulong unchanged = a.Hash();
            a.Seat(0).View.StateCodecs.ToString();
            c.Equal(unchanged, a.Hash(), "diagnostic reads change no simulation state");
            for (int i = 0; i < 40; i++) { a.RunTicks(1); b.RunTicks(1); }
            c.Equal(a.Hash(), b.Hash(), "interleaved saved sessions continue identically");
            b.RunTicks(10);
            c.That(a.Hash() != b.Hash(), "sessions have no shared mutable simulation state");
            c.Equal(7, aSystem.Updates, "one session's extra ticks do not update the other");
            c.That(a.World.HashBreakdown()[0].Key == "codecs", "desync breakdown includes declaration identity");
            c.That(typeof(MutableCodecSystem).Assembly != typeof(World).Assembly, "proof providers are unrelated assembly contracts");
        }
    }
}
