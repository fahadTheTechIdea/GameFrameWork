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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Master-listed metadata and public simulated players only; no resource/gameplay owner in this driver.
    internal static class ResourceContentContractTests
    {
        internal const string Case = "Resource content contracts: independent JSON catalog, physical hauling and manufacturing";
        internal const string OptionalMetadataCase = "Resource optional metadata: unselected quality and lot fields refuse before composition";
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add(OptionalMetadataCase, OptionalMetadata);
        }
        private static readonly Code Process = new Code("process");
        internal static GameFactory Factory(bool half, DirectResourceContentModule module, Action<JsonValue> edit = null, bool duplicate = false,
            Action<JsonValue> worldEdit = null, IReadOnlyList<GameModule> extraModules = null, string extraDeclarations = null, IReadOnlyDictionary<string,string> extraDocuments = null)
        {
            var root = Json.Parse(PointGame.Definitions); root.Set("resources", JsonValue.NewArray());
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(DirectResourceContentModule.ModuleId));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
            if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            if (duplicate) root.Get("resources").Add(Json.Parse("{\"id\":\"supply\",\"unit\":\"unit.supply\",\"category\":\"general\",\"phase\":\"solid\",\"traits\":[],\"capacity\":{\"unit.supply\":1}}"));
            var destination = root.Get("entities")[1].Get("components");
            destination.Set("inventory_port", Json.Parse("{\"id\":\"stock\"}"));
            destination.Set("transformer", Json.Parse("{\"automatic_supply\":false,\"wip_capacity\":\"fixture.count.8\",\"transformations\":[\"direct.batch\"],\"states\":{\"working\":\"processing\",\"stopped\":\"blocked\"},\"operation\":{\"id\":\"process\",\"enabled\":true,\"target\":1,\"minimum\":1,\"maximum\":1,\"unit\":\"unit.work-rate\"}}"));
            var units = root.Get("units");
            units.Add(Json.Parse("{\"id\":\"unit.work-rate\",\"dimension\":\"work-rate\",\"scale\":{\"numerator\":1,\"denominator\":1},\"discrete\":false}"));
            var document = Json.Parse(FrameworkProofContent.Read("resources-contracts.json")); edit?.Invoke(document);
            worldEdit?.Invoke(root);
            return PointGame.Factory(definitions: root.ToString(), additionalModule: module,
                moduleDeclaration: "{\"id\":\"proof.direct-resources\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"resources-contracts.json\"]}",
                moduleDocument: document.ToString(), extraModules: extraModules, extraDeclarations: extraDeclarations, extraDocuments: extraDocuments);
        }
        private static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest { Scenario = "points", Seed = 6217, Seats = { Controller.Human, Controller.Human } });
        private static ResourceId Supply(SimGame g)
        { if (!g.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("actual direct resource absent"); return resource; }
        private static LogisticsOperationalSnapshot Facts(SimGame g, EntityId site, ResourceId resource)
        {
            var code = g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(site, resource, out var value);
            if (!code.IsNone) throw new InvalidOperationException("actual direct resource diagnostics " + code);
            return value;
        }
        private static TransformationSnapshot Current(SimGame g, EntityId site)
        { if (!g.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(site, Process, out var value)) throw new InvalidOperationException("actual direct-resource process absent"); return value; }
        private static void Until(SimGame g, SimGame twin, Func<bool> ready, string stage, EntityId site)
        {
            for (int i = 0; i < 512 && !ready(); i++) Step(g, twin);
            if (!ready()) throw new InvalidOperationException(stage + " exceeded512Updates time=" + g.Clock.StepIndex + "/process=" + Current(g, site).Phase + "/" + Current(g, site).Blocker +
                "/jobs=" + string.Join(";", PermanentRemovalTests.Jobs(g).Select(x => x.Id + "/" + x.State + "/" + x.Blocker)));
        }
        internal static void Healthy(Check c, bool half, bool cancel, GameFactory supplied = null, DirectResourceContentModule suppliedModule = null,
            int expectedResourceCount = 2, Action<Check, GameFactory, SimGame, EntityId, ResourceId> partialCheckpoint = null)
        {
            var module = suppliedModule ?? new DirectResourceContentModule(); var f = supplied ?? Factory(half, module); var g = New(f); var idle = New(f); ulong isolated = idle.Hash();
            c.That(module.Composed && module.ActualAliases, "independent resource resolves once and aliases sole actual catalog");
            var r = Supply(g); EntityId source = Entity(g, "source"), target = Entity(g, "destination"), carrier = Entity(g, "machine");
            var rows = new List<ResourceInfo>(); ulong pure = g.Hash(); g.Seat(0).View.CopyResources(rows);
            c.Equal(pure, g.Hash(), "detached direct metadata read pure"); c.Equal(expectedResourceCount, rows.Count, "no duplicate native resource entry");
            var metadata = rows.Single(x => x.Id == "supply"); var extra = rows.Single(x => x.Id == "direct.unused");
            c.Equal(r, metadata.Resource, "public metadata uses same compact resource identity");
            c.Equal(new UnitId("unit.supply"), metadata.QuantityUnit.Unit, "direct metadata retains actual quantity unit");
            c.Equal(Fix.FromRatio(1, 2), extra.CapacityMeasures[0].PerQuantity, "fractional count-capacity coefficient remains explicit");
            c.Throws<NotSupportedException>(() => ((IList<string>)metadata.Traits).Clear(), "direct traits detached readonly");
            c.Throws<NotSupportedException>(() => ((IList<ResourceCapacityMeasure>)metadata.CapacityMeasures).Clear(), "direct capacity measures detached readonly");
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            PermanentRemovalTests.Submit(c, g, new HaulResourcesCommand { Source = source, Destination = target, Worker = carrier, Resource = r, Ceiling = Fix.FromInt(8), Priority = 100 });
            var twin = f.Load(g.Save()); Until(g, twin, () => Facts(g, target, r).Stock == Fix.FromInt(8) && Facts(g, carrier, r).Stock.IsZero, "real direct resource delivery", target);
            c.Equal(Fix.FromInt(12), Facts(g, source, r).Stock, "actual source supplied eight units physically");
            c.Equal(Fix.FromInt(8), Facts(g, target, r).Stock, "actual receiver gets eight, not a new resource balance");
            PermanentRemovalTests.Submit(c, g, new EnqueueTransformationCommand { Transformer = target, Capability = Process, Transformation = "direct.batch" });
            Until(g, null, () => Current(g, target).Phase == TransformationPhase.Working && Current(g, target).Progress.WholeTicks > 0, "earned direct resource WIP", target);
            partialCheckpoint?.Invoke(c, f, g, target, r);
            var partial = Current(g, target); c.Equal(Fix.FromInt(2), partial.Wip.Stock.AmountOf(r), "actual input moves once into original WIP");
            c.Equal(Fix.One, partial.Tools.Held.AmountOf(r), "same actual resource protects reusable tool");
            c.Equal(Fix.FromInt(6), Facts(g, target, r).Stock, "no copied material or repeated input charge");
            c.That(!g.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(target, Process, out _), "company-private direct-resource WIP withheld");
            twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "direct identity original receipts WIP/progress durable");
            g.Clock.Pause(); twin.Clock.Pause(); long time = g.Clock.StepIndex; Step(g, twin);
            c.Equal(time, g.Clock.StepIndex, "sole paused clock consumes no resource"); c.Equal(partial.Progress, Current(g, target).Progress, "paused direct WIP earns no progress");
            g.Clock.Resume(); twin.Clock.Resume();
            if (cancel) {
                PermanentRemovalTests.Submit(c, g, new CancelTransformationCommand { Transformer = target, Capability = Process, Index = 0 }); twin = f.Load(g.Save());
            }
            Until(g, twin, () => Current(g, target).QueueCount == 0, "settled direct-resource manufacturing/cancellation", target);
            var final = Current(g, target); c.That(final.Wip.Stock.IsEmpty && final.Tools.Receipts.Count == 0, "actual WIP and reusable claims settle");
            c.Equal(Fix.FromInt(cancel ? 8 : 7), Facts(g, target, r).Stock, "cancel returns actual input or completion consumes two and yields one once");
            c.Equal(Fix.FromInt(12), Facts(g, source, r).Stock, "manufacturing fabricates no source stock");
            c.Equal(Fix.FromInt(2), partial.Wip.Stock.AmountOf(r), "retained partial metadata remains detached");
            c.Equal("proof.direct", metadata.Traits.Single(), "retained resource metadata remains authored and detached");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "settled direct resource lifecycle durable"); c.Equal(isolated, idle.Hash(), "independent resource sessions isolated");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "public direct-resource physical hauling/manufacturing replay");
        }
        // This is strict authoring/ordinary quantity coverage, NOT proof of a lot/quality provider.
        // Reuse the original factory, declaration readers, inventory and detached player views.
        private static GameFactory OptionalMetadataFactory(DirectResourceContentModule module, string target = null, string key = null)
            => Factory(false, module, document => {
                document.Get("contract_resources")[0].Get("traits").Add(JsonValue.Of("quality.grade-a"));
                document.Get("contract_resources")[0].Get("traits").Add(JsonValue.Of("lot.example"));
                if (target == "direct.used" || target == "direct.unused")
                    document.Get("contract_resources")[target == "direct.used" ? 0 : 1].Set(key, JsonValue.Of("unselected"));
            }, worldEdit: root => {
                var resource = Json.Parse("{\"id\":\"native.unused\",\"unit\":\"unit.supply\",\"category\":\"general\",\"phase\":\"solid\",\"traits\":[],\"capacity\":{\"unit.supply\":1}}");
                if (target == "native.unused") resource.Set(key, JsonValue.Of("unselected"));
                root.Get("resources").Add(resource);
            });
        private static void OptionalMetadata(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual resource-contract engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "ordinary resource payload identity");
            var module = new DirectResourceContentModule(); var factory = OptionalMetadataFactory(module); var game = New(factory);
            c.That(module.Composed && module.ActualAliases, "same native/independent declarations compose without optional fields");
            var rows = new List<ResourceInfo>(); ulong hash = game.Hash(); game.Seat(0).View.CopyResources(rows);
            c.Equal(3, rows.Count, "native unused and two independent kinds share sole resource catalog");
            var supply = rows.Single(row => row.Id == "supply");
            c.That(supply.Traits.SequenceEqual(new[] { "proof.direct", "quality.grade-a", "lot.example" }),
                "trait labels are retained metadata, not an invented grade or lot inventory selector");
            c.Equal(hash, game.Hash(), "metadata labels do not mutate stock or world hash");
            var source = Entity(game, "source"); var destination = Entity(game, "destination"); var carrier = Entity(game, "machine");
            c.Equal(Fix.FromInt(20), Facts(game, source, supply.Resource).Stock, "source retains one ordinary quantity");
            c.Equal(Fix.Zero, Facts(game, destination, supply.Resource).Stock + Facts(game, carrier, supply.Resource).Stock,
                "labels create no second buffer or extra stock");
            var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "ordinary metadata and sole inventory survive native save/load");
            rows.Clear(); twin.Seat(0).View.CopyResources(rows);
            c.That(rows.Single(row => row.Id == "supply").Traits.SequenceEqual(supply.Traits), "saved catalog retains authored labels");
            c.Equal(supply.Resource, rows.Single(row => row.Id == "supply").Resource, "saved ordinary resource identity is stable");
            game.Update(); twin.Update(); c.Equal(game.Hash(), twin.Hash(), "ordinary saved twins continue through native Update");
            c.Equal(Fix.FromInt(20), Facts(twin, source, supply.Resource).Stock, "no implicit label-driven split or creation");

            ulong runningHash = game.Hash();
            foreach (string target in new[] { "native.unused", "direct.used", "direct.unused" })
                foreach (string key in new[] { "quality", "quality_grade", "quality_policy", "lot", "lot_id", "lot_tracking" }) {
                    var invalidModule = new DirectResourceContentModule(); ContentException refusal = null;
                    try { New(OptionalMetadataFactory(invalidModule, target, key)); }
                    catch (ContentException error) { refusal = error; }
                    c.That(refusal != null && refusal.Message.Contains("unknown key '" + key + "'"),
                        "unsupported optional field is explicitly rejected, not silently ignored: " + target + "/" + key);
                    c.That(!invalidModule.Composed, "unsupported optional field refuses before module composition: " + target + "/" + key);
                }
            c.That(game.Hash() == runningHash && twin.Hash() == runningHash,
                "rejected content cannot disturb either already running independent session");
        }
        private static void Refusal(Check c, string mode)
        {
            var module = new DirectResourceContentModule(); Exception error = null;
            try {
                var f = Factory(false, module, document => {
                    if (mode == "duplicate") document.Get("contract_resources").Add(document.Get("contract_resources")[0].Clone());
                    else if (mode == "wrong-kind") {
                        var wrong = document.Get("contract_resources")[1].Clone(); wrong.Remove("fault");
                        document.Set("wrong_resource_contracts", JsonValue.NewArray()); document.Get("wrong_resource_contracts").Add(wrong);
                        var actual = JsonValue.NewArray(); actual.Add(document.Get("contract_resources")[0].Clone());
                        document.Set("contract_resources", actual);
                    } else if (mode != "cross-kind") document.Get("contract_resources")[1].Set("fault", JsonValue.Of(mode));
                }, duplicate: mode == "cross-kind"); New(f);
            } catch (Exception caught) { error = caught; }
            c.That(error is ContentException && !module.Composed, "invalid unused/duplicate/wrong-kind direct metadata refuses before Compose " + mode);
            if (error == null) throw new InvalidOperationException("missing expected early direct resource refusal " + mode);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current resource-contract engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "authoritative resource payload identity");
            foreach (bool half in new[] { false, true }) { Healthy(c, half, false); Healthy(c, half, true); }
            foreach (string mode in new[] { "identity", "category", "unit", "phase", "traits-null", "traits-duplicate", "trait-lookup", "measures-null", "measures-empty", "measure-default", "ambiguous", "duplicate", "cross-kind", "wrong-kind" }) Refusal(c, mode);
        }
    }
}
