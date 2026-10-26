using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class EnvironmentAccountingTests
    {
        private static readonly Code Source = new Code("leak");
        public static void Register()
        {
            TestRunner.Add("Environment accounting: authored metrics and durable journal admission", Contracts);
            TestRunner.Add("Environment accounting: physical cleanup, rates and saved objectives", Physical);
        }
        private static string Text(JsonValue json) { var text = new StringBuilder(); json.WriteCanonical(text); return text.ToString(); }
        private static JsonValue Metric(JsonValue json) => json.Get("environmental_metrics")[0];
        private static GameFactory Factory(Action<JsonValue> edit = null, Action<JsonValue> incidentEdit = null,
            Action<JsonValue> baseEdit = null, int retention = 2, bool objectives = false)
        {
            JsonValue document = Json.Parse(FrameworkProofContent.Read("environment-accounting.json")); edit?.Invoke(document);
            if (!objectives) document.Remove("objectives");
            JsonValue incident = Json.Parse(FrameworkProofContent.Read("incident-phases.json"));
            // This resource-only accounting profile does not select optional monetary requirements.
            incident.Remove("work_cost_requirements");
            incident.Get("work_requirement_groups")[0].Set("entries", Json.Parse("[{\"id\":\"materials\",\"requirement\":\"leak.materials\"},{\"id\":\"tools\",\"requirement\":\"leak.tools\"}]"));
            incident.Get("activities")[0].Get("nodes")[1].Set("effect", JsonValue.Of("leak.spill"));
            incident.Get("activities")[0].Get("nodes")[3].Set("effect", JsonValue.Of("leak.cleanup")); incidentEdit?.Invoke(incident);
            JsonValue definitions = Json.Parse(Colony.Defs); baseEdit?.Invoke(definitions);
            definitions.Get("scenarios")[0].Get("settings").Set(ObjectiveModule.ModuleId,
                Json.Parse("{\"companies\":[{\"company\":0,\"objectives\":[\"environment.safe-response\",\"environment.cleanup-goal\"]}]}"));
            return Colony.Factory(Text(definitions), "{\"resource_flow_journals\":[{\"id\":\"diagnostics.resource-flows\",\"retained_records_per_company\":" + retention + "}]}",
                additionalDocuments: new Dictionary<string, string> { { "incident-phases.json", Text(incident) }, { "environment-accounting.json", Text(document) } });
        }
        private static SimGame New(GameFactory factory, bool objectives = false)
        {
            var request = new NewGameRequest { Scenario = "colony", Seed = 919 }; request.Mods.Add(EnvironmentalMetricsModule.ModuleId);
            if (objectives) request.Mods.Add(ObjectiveModule.ModuleId);
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Spawn(SimGame game, int seat = 0)
        {
            var entity = ((TileMap)game.World.Space).SpawnAt(game.World, game.World.Content.Get<EntityDef>("leaking-machine"), new PlayerId(seat), new TileCoord(12 + seat * 8, 28));
            game.World.Entities.Flush(); return entity;
        }
        private static void Tick(SimGame game)
        { if (game.RunTicks(1) != 1) throw new InvalidOperationException("environment tick=" + game.Tick + "; " + game.Fault); }
        private static void Advance(SimGame game, int ticks, SimGame twin = null)
        { for (int i = 0; i < ticks; i++) { Tick(game); if (twin != null) Tick(twin); } }
        private static EnvironmentalObservation Read(SimGame game, string metric = "environment.spilled")
            => game.Seat(0).View.Feature<EnvironmentalMetricsView>().Read(new PlayerId(0), metric);
        private static void Reach(SimGame game, EntityId target, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 500 && !ready(); i++) Advance(game, 1, twin);
            if (!ready())
            {
                var order = game.World.Services.Get<WorkBoard>().ActiveResponseOn(target, Source); var metric = Read(game);
                throw new InvalidOperationException(stage + " timeout; tick=" + game.Tick + "; response=" + order?.State + "/" + order?.Blocker +
                    "; spill=" + metric.AmountRaw + "; blocker=" + metric.Blocker);
            }
        }
        // Resource protocol instrumentation only; gameplay proof below uses commands and physical work.
        private static ResourceInventoryResult Commit(SimGame game, Entity entity, int amount, ResourceFlowKind kind, string cause, long identity)
            => game.World.Services.Get<IResourceTransaction>().CommitInventory(new ResourceInventoryIntent(new ReservationOwner("proof.environment", identity),
                entity.Owner, entity.Id, new Bill(new[] { new ResourceAmount(game.World.Content.ResourceId("steel"), Fix.FromInt(amount)) }), kind, new Code(cause)));
        private static void Invalid(Check c, Action<JsonValue> edit, string why)
            => c.Throws<ContentException>(() => New(Factory(edit)), why);
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity a = Spawn(game), foreign = Spawn(game, 1);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current engine-owned metric contracts");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "cumulative journal directly replaces prior codec");
            c.Equal(7, ((IPersistentService)game.World.Services.Get<IResourceFlowJournal>()).StateSchemaVersion, "sole journal owns attributed cumulative history and retained feed");
            var empty = Read(game); c.That(empty.Available && empty.AmountRaw == BigInteger.Zero && empty.Facts == 0, "no matching physical facts is a real zero, not unavailable input");
            c.That(!empty.Rate.HasValue && empty.RateBlocker == EnvironmentalCodes.NoElapsedTime, "zero elapsed game time never fabricates a rate");
            Tick(game); ulong hash = game.Hash(); var view = game.Seat(0).View.Feature<EnvironmentalMetricsView>();
            var refused = view.Read(new PlayerId(1), "environment.spilled");
            c.That(!refused.Available && refused.Blocker == Codes.NotOwned && !refused.Unit.IsValid && refused.ThroughTick == 0, "foreign refusal withholds unit, quantities and time watermarks");
            c.Equal(Codes.NotSeated, game.Observer.Feature<EnvironmentalMetricsView>().Read(new PlayerId(99), "environment.spilled").Blocker, "observer cannot invent a company");
            c.Equal(Codes.UnknownDef, Read(game, "absent").Blocker, "absent metric never substitutes default emissions");
            c.Equal(hash, game.Hash(), "empty/private/unknown reads are pure");
            c.That(Commit(game, a, 2, ResourceFlowKind.Lost, "leak.spill", 1).Committed && Commit(game, a, 3, ResourceFlowKind.Lost, "leak.spill", 2).Committed,
                "exact physical losses commit through existing authoritative lifecycle protocol");
            c.That(game.World.Events.HasPending && Read(game).AmountRaw == BigInteger.Zero, "pending physical effects are not a synchronous second journal path");
            c.Throws<InvalidOperationException>(() => game.Save(), "save cannot lose pending physical history"); Tick(game);
            c.Equal((BigInteger)Fix.FromInt(5).Raw, Read(game).AmountRaw.Value, "native cumulative metric classifies both actual loss facts");
            c.Equal(2L, Read(game).Facts, "one subscriber counts each committed fact once");
            var recent = Read(game, "environment.recent-spill");
            c.That(recent.Available && recent.AmountRaw == (BigInteger)Fix.FromInt(5).Raw && recent.Facts == 2,
                "complete nonempty time-window uses actual retained physical facts");
            c.Equal((BigInteger)5, recent.Rate.Value.Numerator, "five actual units in one game minute gives exact window rate");
            c.Equal(BigInteger.One, recent.Rate.Value.Denominator, "window rates never round individual per-frame quantities");
            c.Equal(Read(game).AmountRaw, Read(game, "environment.proof-spill").AmountRaw, "independent public provider reuses actual history without scheduler changes");
            var captured = Read(game); var summary = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            c.Throws<NotSupportedException>(() => ((IList<ResourceFlowAggregate>)summary.Aggregates).Clear(), "cumulative summary is immutable detached data");
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(1)).Status, "raw cumulative journal remains company private");
            c.That(Commit(game, a, 1, ResourceFlowKind.Lost, "leak.spill", 3).Committed && Commit(game, a, 1, ResourceFlowKind.Lost, "unrelated.loss", 4).Committed &&
                Commit(game, a, 1, ResourceFlowKind.Destroyed, "leak.spill", 5).Committed, "explicit kinds/causes remain distinct native historical classifications");
            Tick(game);
            c.Equal((BigInteger)Fix.FromInt(6).Raw, Read(game).AmountRaw.Value, "evicted matching facts remain in cumulative total, other kind/cause excluded");
            c.Equal(ResourceFlowReadStatus.Expired, game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Status, "individual retained history expires by authored policy");
            c.Equal(EnvironmentalCodes.HistoryExpired, Read(game, "environment.recent-spill").Blocker, "overlapping eviction refuses a window instead of truncating rate to zero");
            c.Equal((BigInteger)Fix.FromInt(5).Raw, captured.AmountRaw.Value, "captured amount stays detached after new physical facts");
            c.Equal((BigInteger)120, Read(game).Rate.Value.Numerator, "six native units over three minutes yields exactly120 units/hour");
            c.Equal(BigInteger.One, Read(game).Rate.Value.Denominator, "rational rate reduces exactly without fixed-point rounding");
            Tick(game);
            var quiet = Read(game, "environment.recent-spill"); c.That(quiet.Available && quiet.AmountRaw == BigInteger.Zero && quiet.Rate.Value.Numerator.IsZero,
                "newer complete empty window certifies actual no new loss");
            c.Equal(ResourceFlowReadStatus.CursorAhead, game.Observer.Feature<ResourceFlowView>().ReadWindow(new PlayerId(0), -1, game.Tick).Status, "negative time-window refuses");
            c.Equal(ResourceFlowReadStatus.CursorAhead, game.Observer.Feature<ResourceFlowView>().ReadWindow(new PlayerId(0), 0, game.Tick + 1).Status, "future window cannot infer facts");
            c.That(foreign.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")) == Fix.FromInt(30), "own losses never spend another company's inventory");
            SimGame twin = factory.Load(game.Save()); Advance(game, 3, twin); c.Equal(game.Hash(), twin.Hash(), "evicted history, summaries and rates restore through one codec");
            Invalid(c, j => Metric(j).Remove("resources"), "explicit resource selection required");
            Invalid(c, j => Metric(j).Set("resources", JsonValue.NewArray()), "empty resource filters refuse");
            Invalid(c, j => Metric(j).Get("resources").Add(JsonValue.Of("steel")), "duplicate resources refuse");
            Invalid(c, j => Metric(j).Set("resources", Json.Parse("[\"absent\"]")), "missing resource refuses");
            Invalid(c, j => Metric(j).Set("resources", Json.Parse("[\"steel\",\"water\"]")), "mixed units cannot become an environmental scalar");
            Invalid(c, j => Metric(j).Set("unit", JsonValue.Of("unit.capacity.count")), "same dimension is not inferred native unit conversion");
            Invalid(c, j => Metric(j).Set("unit", JsonValue.Of("absent")), "missing unit refuses");
            Invalid(c, j => Metric(j).Set("kinds", Json.Parse("[\"stock-transfer\"]")), "movement never silently becomes loss or emissions");
            Invalid(c, j => Metric(j).Get("kinds").Add(JsonValue.Of("lost")), "duplicate kinds refuse");
            Invalid(c, j => Metric(j).Get("causes").Add(JsonValue.Of("leak.spill")), "duplicate causes refuse");
            Invalid(c, j => Metric(j).Set("causes", JsonValue.NewArray()), "all-causes wildcard is not a fabricated default");
            Invalid(c, j => Metric(j).Set("causes", Json.Parse("[\"\"]")), "cause selection requires actual nonempty Code identities");
            Invalid(c, j => Metric(j).Remove("window"), "window null or explicit duration is mandatory");
            Invalid(c, j => Metric(j).Set("window", Json.Parse("{\"numerator\":0,\"denominator\":1,\"unit\":\"minute\"}")), "zero window refuses");
            Invalid(c, j => Metric(j).Set("window", Json.Parse("{\"numerator\":1,\"denominator\":2,\"unit\":\"minute\"}")), "window must align exactly to session time");
            Invalid(c, j => Metric(j).Set("rate_unit", JsonValue.Of("month")), "no assumed calendar rate unit");
            Invalid(c, j => Metric(j).Set("legacy", JsonValue.Of(true)), "unknown content keys refuse cleanly");
            Invalid(c, j => j.Get("environmental_objective_predicates")[0].Set("metric", JsonValue.Of("absent")), "objective binding resolves actual metric");
            Invalid(c, j => j.Get("environmental_objective_predicates")[0].Set("amount", JsonValue.Number("0.5")), "discrete native objective threshold cannot round silently");
            Invalid(c, j => j.Get("environmental_objective_predicates")[0].Remove("unit"), "objective threshold unit is explicit, not a resolution-order default");
            c.Throws<ContentException>(() => New(Factory(j => j.Get("environmental_objective_predicates")[0].Set("unit", JsonValue.Of("unit.water")), objectives: true), true),
                "selected objective checks its exact metric quantity unit before ticking");
            Codec(c, game); LargeHistory(c); ProviderFaults(c); NoSpaceOrWork(c);
        }
        private sealed class StringFaultReader : IStateSync
        {
            private readonly IStateSync _inner; private readonly string _match, _replacement; private bool _changed; private int _skip;
            internal StringFaultReader(byte[] bytes, string match, string replacement, int skip) { _inner = new StateReader(bytes, 0, bytes.Length); _match = match; _replacement = replacement; _skip = skip; }
            public SyncMode Mode => SyncMode.Read; public bool Reading => true;
            public void String(ref string value) { _inner.String(ref value); if (!_changed && value == _match) { if (_skip > 0) _skip--; else { value = _replacement; _changed = true; } } }
            public void Int(ref int v) => _inner.Int(ref v); public void Long(ref long v) => _inner.Long(ref v); public void ULong(ref ulong v) => _inner.ULong(ref v);
            public void Bool(ref bool v) => _inner.Bool(ref v); public void Byte(ref byte v) => _inner.Byte(ref v); public void Fix(ref Fix v) => _inner.Fix(ref v);
            public void Entity(ref EntityId v) => _inner.Entity(ref v); public void Player(ref PlayerId v) => _inner.Player(ref v); public void Resource(ref ResourceId v) => _inner.Resource(ref v);
            public void Tile(ref TileCoord v) => _inner.Tile(ref v); public void Vec(ref FixVec2 v) => _inner.Vec(ref v); public void Bytes(byte[] v) => _inner.Bytes(v);
            public int Count(int v) => _inner.Count(v); public void Section(string v) => _inner.Section(v);
        }
        private static void Codec(Check c, SimGame game)
        {
            var journal = game.World.Services.Get<IResourceFlowJournal>(); var codec = (IPersistentService)journal; byte[] bytes;
            using (var stream = new MemoryStream()) { codec.Sync(new StateWriter(stream)); bytes = stream.ToArray(); }
            string amount = journal.ReadSummary(new PlayerId(0)).Aggregates[0].AmountRaw.ToString(); ulong before = game.Hash();
            foreach (string bad in new[] { "-1", "01", "0", "9999999999999999999999999999999999999999999" })
            { c.Throws<CorruptStateException>(() => codec.Sync(new StringFaultReader(bytes, amount, bad, 0)), "staged cumulative decode rejects malformed/impossible quantity " + bad);
                c.Equal(before, game.Hash(), "rejected cumulative restore preserves all original company partitions " + bad); }
            string scope = null;
            foreach (var row in journal.ReadSummary(new PlayerId(0)).Aggregates)
                if (row.Kind == ResourceFlowKind.Lost) { scope = row.Attribution.OwnerScope; break; }
            if (scope == null) throw new InvalidOperationException("missing actual loss attribution for codec probe");
            int retained = 0;
            foreach (var entry in journal.Read(new PlayerId(0), 0).Entries) if (entry.Item.Owner.Scope == scope) retained++;
            c.Throws<CorruptStateException>(() => codec.Sync(new StringFaultReader(bytes, scope, " ", retained)), "malformed cumulative owner attribution refuses after retained facts decode");
            c.Equal(before, game.Hash(), "failed attributed summary restore publishes no company partition");
        }
        private static void LargeHistory(Check c)
        {
            GameFactory factory = Factory(incidentEdit: j => {
                var inventory = j.Get("entities")[0].Get("components").Get("single_resource_port");
                inventory.Set("capacity_policy", JsonValue.Of("fixture.count.huge")); inventory.Set("initial", JsonValue.Number("2000000000"));
            }, baseEdit: j => j.Get("capacity_policies").Add(Json.Parse("{\"id\":\"fixture.count.huge\",\"limits\":{\"unit.capacity.count\":2000000000}}")));
            SimGame game = New(factory); Entity target = Spawn(game); Tick(game);
            for (int i = 0; i < 6; i++)
            {
                c.That(Commit(game, target, 2000000000, ResourceFlowKind.Lost, "leak.spill", i * 2 + 1).Committed, "real large native loss commits " + i); Tick(game);
                if (i < 5) { c.That(Commit(game, target, 2000000000, ResourceFlowKind.Produced, "proof.restock", i * 2 + 2).Committed, "authored protocol fixture replenishes actual stock " + i); Tick(game); }
            }
            BigInteger expected = (BigInteger)2000000000 * 6 * Fix.One.Raw;
            c.That(expected > long.MaxValue && Read(game).AmountRaw == expected, "historical quantity exceeds Fix without overflow or world cap");
            c.Equal(6L, Read(game).Facts, "large quantities do not create duplicate individual events");
            c.Equal(expected, factory.Load(game.Save()).Seat(0).View.Feature<EnvironmentalMetricsView>().Read(new PlayerId(0), "environment.spilled").AmountRaw.Value,
                "arbitrary-precision cumulative amount is canonical in the sole journal codec");
        }
        private static void ProviderFaults(Check c)
        {
            for (int mode = 1; mode <= 2; mode++)
            {
                int fault = mode; SimGame game = New(Factory(j => {
                    j.Get("proof_environment_metrics")[0].Set("fault_mode", JsonValue.Number(fault.ToString()));
                    j.Get("environmental_objective_predicates")[0].Set("metric", JsonValue.Of("environment.proof-spill"));
                }, objectives: true), true);
                for (int i = 0; i < 10 && game.Fault == null; i++) game.Update();
                c.That(game.Fault?.Phase == "observation.provider", "actual metric observation guard owns the first provider fault " + mode);
                c.Throws<InvalidOperationException>(() => game.Save(), "dishonest provider cannot save a healthy objective/history checkpoint " + mode);
            }
        }
        private static void NoSpaceOrWork(Check c)
        {
            var definition = new GameDefinition("environment-only", new GameModule[] { new InventoryModule(), new ResourceFlowsModule(), new EnvironmentalMetricsModule() });
            JsonValue original = Json.Parse(Colony.Defs); var content = JsonValue.NewObject();
            foreach (string section in new[] { "units", "resources", "time_definitions", "calendars", "scenarios" }) content.Set(section, original.Get(section).Clone());
            var scenario = content.Get("scenarios")[0]; scenario.Set("space", Json.Parse("null")); scenario.Set("modules", Json.Parse("[\"sim.environment-metrics\"]")); scenario.Set("settings", Json.Parse("{}"));
            scenario.Set("game_mode", JsonValue.Of("environment.resource-only"));
            var metrics = Json.Parse(FrameworkProofContent.Read("environment-accounting.json"));
            metrics.Remove("proof_environment_metrics"); metrics.Remove("objectives"); metrics.Remove("inventory_effects"); metrics.Remove("environmental_objective_predicates");
            string master = "{\"schema\":1,\"modules\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\",\"documents\":[\"framework.json\",\"environment-only-game-mode.json\"]}," +
                "{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[\"flows.json\"]}," +
                "{\"id\":\"sim.environment-metrics\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\"}],\"documents\":[\"metrics.json\"]}]}";
            var files = new PreloadedFileSource().Add("manifest.json", master).Add("framework.json", Text(content))
                .Add("environment-only-game-mode.json", FrameworkProofContent.Read("environment-only-game-mode.json"))
                .Add("flows.json", FrameworkProofContent.Read("resource-flows.json")).Add("metrics.json", Text(metrics));
            var factory = new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 81 });
            c.That(game.World.Space == null && !game.World.Services.Has<WorkBoard>(), "environment module composes without any map, worker, objective or operation module");
            c.That(Read(game).Available && Read(game).AmountRaw.Value.IsZero && !Read(game).Rate.HasValue, "empty mapless historical source reports real zero and explicit unavailable zero-age rate");
            c.Equal(0, game.World.Entities.All.Count, "metric selection creates no ghost entities or fabricated providers");
            Tick(game); c.That(Read(game).Rate.Value.Numerator.IsZero, "actual game Update permits a complete elapsed zero-loss rate");
            SimGame twin = factory.Load(game.Save()); Advance(game, 5, twin);
            c.Equal(game.Hash(), twin.Hash(), "no-work/no-space metrics preserve isolated native saved continuation");
        }
        private static ObjectiveSnapshot Objective(SimGame game)
        {
            var facts = new List<ObjectiveSnapshot>(); game.Seat(0).View.Feature<ObjectiveView>().CopyObjectives(facts);
            foreach (var fact in facts) if (fact.Objective == "environment.cleanup-goal") return fact;
            throw new InvalidOperationException("missing enrolled cleanup objective");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(retention: 2, objectives: true); SimGame game = New(factory, true); Entity target = Spawn(game), foreign = Spawn(game, 1);
            WideTimeTests.SetTick(game, 1L << 40);
            Entity crane = game.World.Entities.Get(Colony.Mine(game, 0, "crane")[0].Id); var initial = crane.Get<ITilePresence>().Position;
            c.That(TestKit.Order(game, 0, new RaiseIncidentCommand { Target = target.Id, Source = Source, Kind = new Code("machine.leak") }).Accepted,
                "ordinary incident trigger starts real native DAG, not instant environmental side effects");
            c.Equal(BigInteger.Zero, Read(game).AmountRaw.Value, "spill effect waits for positive-time response work");
            Reach(game, target.Id, () => Read(game).AmountRaw > BigInteger.Zero, "actual physical spill");
            c.That(crane.Get<ITilePresence>().Position != initial, "machine physically travels before effect records environmental loss");
            c.Equal((BigInteger)Fix.FromInt(6).Raw, Read(game).AmountRaw.Value, "actual stock loss is the only spill amount");
            c.That(Read(game).ThroughTick > (1L << 40), "environment coverage timestamps retain wide signed game ages");
            var rate = Read(game).Rate.Value;
            c.Equal((BigInteger)6 * 60 * rate.Denominator, rate.Numerator * Read(game).ThroughTick,
                "wide-age cumulative rate uses exact rational session time without narrowing into fixed point");
            c.Equal(BigInteger.Zero, Read(game, "environment.cleaned").AmountRaw.Value, "cleanup waits for its own blocked machine branch");
            var before = Read(game); var stock = target.Get<IInventory>().Amount(game.World.Content.ResourceId("steel"));
            SimGame twin = factory.Load(game.Save()); Advance(game, 30, twin);
            c.Equal(before.AmountRaw, Read(game).AmountRaw, "persistent incident does not repeatedly spend or record once-only spill effects");
            c.Equal(BigInteger.Zero, Read(game, "environment.cleaned").AmountRaw.Value, "blocked cleanup cannot fabricate progress/accounting");
            c.That(Objective(game).State == ObjectiveState.Active && !Objective(game).Observation.Satisfied, "native objective waits on actual cleanup history");
            c.Equal(game.Hash(), twin.Hash(), "blocked physical response and cumulative history continue identically");
            var order = game.World.Services.Get<WorkBoard>().ActiveResponseOn(target.Id, Source);
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted && TestKit.Order(twin, 0, new CancelOrderCommand { Order = order.Id }).Accepted,
                "ordinary cancellation preserves existing completed physical effect state");
            Advance(game, 1, twin); c.Equal(order.Id, game.World.Services.Get<WorkBoard>().ActiveResponseOn(target.Id, Source).Id, "native response resumes same order/history");
            c.That(TestKit.Order(game, 0, new SetWorkPermitCommand { Target = target.Id, Enabled = true }).Accepted &&
                TestKit.Order(twin, 0, new SetWorkPermitCommand { Target = target.Id, Enabled = true }).Accepted, "ordinary readiness allows physical cleanup");
            Reach(game, target.Id, () => Read(game, "environment.cleaned").AmountRaw > 0, "timed cleanup disposal", twin);
            c.Equal((BigInteger)Fix.FromInt(5).Raw, Read(game, "environment.cleaned").AmountRaw.Value, "cleanup records actual verified disposal, never restores fabricated stock");
            c.That(Objective(game).State != ObjectiveState.Completed, "same-Update event publication cannot recursively complete environmental objective");
            Reach(game, target.Id, () => Objective(game).State == ObjectiveState.Completed, "next-step environmental objective", twin);
            Reach(game, target.Id, () => !target.Get<IPhasedIncidentResponse>(Source).ReadLifecycle().Active, "true physical restart", twin);
            Advance(game, 5, twin);
            c.Equal(1L, Read(game).Facts, "cancel/resume/restart never duplicates spill history");
            c.Equal(1L, Read(game, "environment.cleaned").Facts, "cleanup commits exactly once after actual required work");
            c.Equal(Fix.FromInt(16), target.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "stock conserves6 spill+3 repair+5 cleanup units, tools release normally");
            c.Equal(Fix.FromInt(30), foreign.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "physical accounting cannot consume foreign inventory");
            var summary = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            BigInteger count = 0; foreach (var row in summary.Aggregates) count += row.Facts;
            c.Equal((BigInteger)summary.Latest, count, "whole cumulative counts match the sole event-feed sequence after multiple evictions");
            c.Equal((BigInteger)Fix.FromInt(6).Raw, before.AmountRaw.Value, "captured spill history remains a detached measurement");
            c.Equal(game.Hash(), twin.Hash(), "saved physical cleanup/objective/history/rates share one deterministic continuation");
            SimGame restored = factory.Load(game.Save()); c.Equal(game.Hash(), restored.Hash(), "completed objective and physical histories restore together");
            c.Equal((BigInteger)Fix.FromInt(5).Raw, Read(restored, "environment.cleaned").AmountRaw.Value, "postcleanup cumulative metric survives dropped individual feed facts");
        }
    }
}
