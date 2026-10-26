using System;
using System.Collections.Generic;
using System.IO;
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
    public static class EquipmentConditionTests
    {
        internal static readonly Code Source = new Code("integrity"), Process = new Code("process");
        public static void Register()
        {
            TestRunner.Add("Equipment condition: authored ownership, providers and checkpoint admission", Contracts);
            TestRunner.Add("Equipment condition: physical service, shutdown and saved continuation", Physical);
        }
        private static string Text(JsonValue value) { var result = new StringBuilder(); value.WriteCanonical(result); return result.ToString(); }
        private static JsonValue Parts(JsonValue value) => value.Get("entities")[0].Get("components");
        internal static GameFactory Factory(Action<JsonValue> edit = null, bool session = false)
        {
            var value = Json.Parse(FrameworkProofContent.Read("equipment-condition.json")); edit?.Invoke(value);
            var documents = new Dictionary<string, string> { { "equipment-condition.json", Text(value) } };
            if (session) {
                documents.Add("condition-wear.json", FrameworkProofContent.Read("condition-wear.json"));
                documents.Add("equipment-session.json", FrameworkProofContent.Read("equipment-session.json"));
            }
            return Colony.Factory(additionalDocuments: documents);
        }
        internal static SimGame New(GameFactory factory, string scenario = "colony")
        {
            var request = new NewGameRequest { Scenario = scenario, Seed = 992 }; request.Mods.Add(EquipmentConditionModule.ModuleId);
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Spawn(SimGame game, bool proof = false, int seat = 0)
        {
            Entity entity = ((TileMap)game.World.Space).SpawnAt(game.World, game.World.Content.Get<EntityDef>(proof ? "proof-condition-machine" : "condition-machine"),
                new PlayerId(seat), new TileCoord(12 + seat * 8, proof ? 26 : 28)); game.World.Entities.Flush(); return entity;
        }
        private static void Tick(SimGame game)
        { if (game.RunTicks(1) != 1) throw new InvalidOperationException("equipment tick=" + game.Tick + "; " + game.Fault); }
        private static void Advance(SimGame game, int count, SimGame twin = null)
        { for (int i = 0; i < count; i++) { Tick(game); if (twin != null) Tick(twin); } }
        internal static ConditionSnapshot Read(SimGame game, EntityId id)
        {
            Code refusal = game.Seat(0).View.Feature<EquipmentConditionView>().Read(id, Source, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("equipment read=" + refusal); return value;
        }
        internal static TransformationSnapshot State(SimGame game, EntityId id)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(id, Process, out var value))
                throw new InvalidOperationException("equipment process missing"); return value;
        }
        private static CommandResult Post(SimGame game, EntityId id, string plan)
            => TestKit.Order(game, 0, new PostOrderCommand { Target = id, Plan = plan });
        private static void Reach(SimGame game, EntityId id, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Advance(game, 1, twin);
            if (!done()) throw new InvalidOperationException(stage + " timeout; tick=" + game.Tick + "; condition=" + Read(game, id).Value + "; process=" + State(game, id).Blocker);
        }
        private static ConditionChange Change(int amount, ConditionChangeKind kind = ConditionChangeKind.Damage, string unit = "unit.condition")
            => new ConditionChange(kind, new UnitQuantity(new UnitId(unit), Fix.FromInt(amount)));
        private static void Invalid(Check c, Action<JsonValue> edit, string why)
            => c.Throws<ContentException>(() => New(Factory(edit)), why);
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity target = Spawn(game), foreign = Spawn(game, seat: 1);
            IConditionProvider provider = target.Get<IConditionProvider>(Source); var captured = Read(game, target.Id);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current condition contracts are engine-owned");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "new component codec leaves existing native payload layouts unchanged");
            c.That(provider is EquipmentCondition && target.Def.Component<IConditionDefinition>(Source) is EquipmentConditionDef, "native owner uses public named contracts");
            c.That(captured.Value == Fix.FromInt(10) && captured.Maximum == Fix.FromInt(10) && captured.ChangedTick == -1 && captured.Unit == new UnitId("unit.condition"),
                "healthy value and bounds come only from explicit JSON");
            c.That(ReferenceEquals(target.Def.Component<IConditionDefinition>(Source).Unit, game.World.Content.Get<UnitDef>("unit.condition")), "unit aliases existing catalog, not another registry");
            var view = game.Seat(0).View.Feature<EquipmentConditionView>(); ulong before = game.Hash();
            c.Equal(Codes.NotFound, view.Read(foreign.Id, Source, out var withheld), "foreign identity reveals no condition");
            c.That(!withheld.Unit.IsValid && withheld.Capability.IsNone, "foreign metadata/time/value is withheld");
            var ids = new List<Code>(); c.That(view.CopyIds(target.Id, ids) && ids.Count == 1 && ids[0] == Source, "named condition IDs derive actual providers");
            c.That(!view.CopyIds(foreign.Id, ids) && ids.Count == 0, "foreign enumeration clears output");
            c.Equal(ConditionCodes.NotCondition, view.Read(target.Id, new Code("absent"), out _), "unknown source never selects another condition");
            c.Equal(Codes.InvalidArgument, view.Read(target.Id, Code.None, out _), "condition source is explicit");
            c.Equal(Code.None, game.Observer.Feature<EquipmentConditionView>().Read(foreign.Id, Source, out _), "explicit observer can diagnose foreign equipment");
            c.Equal(ConditionCodes.Bounds, ConditionContracts.Check(game.World, provider, Change(11)), "excess damage refuses rather than clamp");
            c.Equal(ConditionCodes.Bounds, ConditionContracts.Commit(game.World, provider, Change(1, ConditionChangeKind.Repair)), "excess repair refuses without mutation");
            c.Equal(ConditionCodes.InvalidChange, ConditionContracts.Check(game.World, provider, Change(0)), "zero change is not a silent success");
            c.Equal(ConditionCodes.InvalidChange, ConditionContracts.Check(game.World, provider, Change(-1)), "negative damage cannot secretly repair");
            c.Equal(ConditionCodes.InvalidChange, ConditionContracts.Check(game.World, provider, Change(1, unit: "unit.steel")), "native unit match is explicit");
            c.Equal(ConditionCodes.InvalidChange, ConditionContracts.Check(game.World, provider, Change(1, (ConditionChangeKind)9)), "unknown change tag refuses");
            c.Equal(ConditionCodes.NotCondition, ConditionContracts.Check(game.World, null, Change(1)), "absent provider is a typed blocker");
            c.Throws<ArgumentException>(() => ConditionContracts.Read(New(factory).World, provider), "cross-session provider refuses");
            c.Equal(before, game.Hash(), "reads and ordinary refusals allocate/reserve/spend nothing");
            c.Equal(Code.None, ConditionContracts.Commit(game.World, provider, Change(10)), "engine-side protocol can commit exact full damage");
            c.That(Read(game, target.Id).Value == Fix.Zero && Read(game, target.Id).ChangedTick == game.Tick, "full damage records actual value and wide game-time boundary");
            c.Equal(Fix.FromInt(10), captured.Value, "captured facts stay detached after mutation");
            Tick(game); c.Equal(new Code("state.stopped"), target.State, "entity Update settles authored failed state");
            c.Equal(Code.None, ConditionContracts.Commit(game.World, provider, Change(10, ConditionChangeKind.Repair)), "exact maximum restoration is bounded native arithmetic");
            Tick(game); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "condition and entity state restore through native codec");
            c.That(game.Seat(0).View.Feature<OperationsView>().ReadSignal(target.Id, Source, "equipment.integrity", out var signal).IsNone && signal.Value == Fix.FromInt(10),
                "existing numeric signal reads the same condition value");
            c.That(game.Seat(0).View.Feature<OperationsView>().ReadSignal(target.Id, Source, "equipment.maximum", out var maximum).IsNone && maximum.Value == Fix.FromInt(10),
                "maximum signal derives immutable definition, not a state mirror");
            Invalid(c, j => Parts(j).Get("equipment_condition").Remove("initial"), "no fabricated opening condition");
            Invalid(c, j => Parts(j).Get("equipment_condition").Remove("maximum"), "no genre health ceiling default");
            Invalid(c, j => Parts(j).Get("equipment_condition").Remove("unit"), "native unit must be authored");
            Invalid(c, j => Parts(j).Get("equipment_condition").Set("initial", JsonValue.Number("11")), "initial bounds validated before tick one");
            Invalid(c, j => Parts(j).Get("equipment_condition").Set("maximum", JsonValue.Number("0")), "zero maximum refuses");
            Invalid(c, j => Parts(j).Get("equipment_condition").Set("id", JsonValue.Of("")), "named identity is mandatory");
            Invalid(c, j => Parts(j).Get("equipment_condition").Set("unit", JsonValue.Of("absent")), "missing unit refuses");
            Invalid(c, j => Parts(j).Get("equipment_condition").Set("unit", JsonValue.Of("unit.steel")), "native signal's authored unit cannot change silently");
            Invalid(c, j => Parts(j).Get("equipment_condition").Get("states").Remove("damaged"), "damaged state explicit");
            Invalid(c, j => Parts(j).Get("equipment_condition").Get("states").Set("failed", JsonValue.Of("absent")), "failed state resolves real catalogue");
            Invalid(c, j => Parts(j).Get("equipment_condition").Set("health", JsonValue.Number("100")), "no duplicate legacy health field");
            Invalid(c, j => j.Get("condition_effects")[0].Set("kind", JsonValue.Of("restore")), "change direction never inferred");
            Invalid(c, j => j.Get("condition_effects")[0].Set("amount", JsonValue.Number("0")), "positive physical effect quantity mandatory");
            Invalid(c, j => j.Get("condition_effects")[0].Set("unit", JsonValue.Of("absent")), "effect unit resolves real content");
            Invalid(c, j => j.Get("condition_effects")[0].Set("source", JsonValue.Of("")), "effect source explicit");
            Invalid(c, j => j.Get("condition_requirements")[0].Set("maximum", JsonValue.Number("-1")), "requirement bounds ordered and nonnegative");
            Invalid(c, j => j.Get("condition_requirements")[0].Remove("blocker"), "condition wait has a typed authored blocker");
            Invalid(c, j => j.Get("signals")[0].Set("unit", JsonValue.Of("unit.steel")), "declared signal schema exact units");
            Invalid(c, j => j.Get("activities")[0].Get("nodes")[0].Set("roles", JsonValue.NewArray()), "timer-only node cannot perform physical damage");
            Invalid(c, j => j.Get("activities")[1].Get("nodes")[0].Set("duration", Content.Json.Parse("{\"numerator\":0,\"denominator\":1,\"unit\":\"hour\"}")), "instant node cannot repair");
            Discrete(c); Codec(c, game, target); NamedOwners(c); Mapless(c); Faults(c); Independent(c);
        }
        private static void Discrete(Check c)
        {
            GameFactory factory = Factory(j => j.Get("units")[0].Set("discrete", JsonValue.Of(true)));
            SimGame game = New(factory); Entity target = Spawn(game); ulong hash = game.Hash();
            var fractional = new ConditionChange(ConditionChangeKind.Damage, new UnitQuantity(new UnitId("unit.condition"), Fix.FromRatio(1, 2)));
            c.Equal(ConditionCodes.InvalidChange, ConditionContracts.Check(game.World, target.Get<IConditionProvider>(Source), fractional), "discrete native quantity never rounds a partial damage");
            c.Equal(hash, game.Hash(), "discrete refusal preserves actual value");
            Invalid(c, j => { j.Get("units")[0].Set("discrete", JsonValue.Of(true)); j.Get("condition_effects")[0].Set("amount", JsonValue.Number("0.5")); }, "authored fractional discrete effect refuses");
        }
        private static void Codec(Check c, SimGame game, Entity target)
        {
            var part = target.Get<EquipmentCondition>(Source); ulong before = game.Hash();
            foreach (var pair in new[] { (Fix.FromInt(-1), 0L), (Fix.FromInt(11), 0L), (Fix.FromInt(10), -2L), (Fix.FromInt(4), -1L) })
            {
                byte[] bytes; using (var stream = new MemoryStream())
                { var writer = new StateWriter(stream); Fix value = pair.Item1; long tick = pair.Item2, exposure = 0, exposureAt = -1;
                  writer.Fix(ref value); writer.Long(ref tick); writer.Long(ref exposure); writer.Long(ref exposureAt); bytes = stream.ToArray(); }
                c.Throws<CorruptStateException>(() => part.Sync(new StateReader(bytes, 0, bytes.Length)), "staged condition decode rejects invalid quantity/change chronology");
                c.Equal(before, game.Hash(), "failed component decode preserves original value/time");
            }
            byte[] original, future;
            using (var stream = new MemoryStream()) { part.Sync(new StateWriter(stream)); original = stream.ToArray(); }
            using (var stream = new MemoryStream())
            { var writer = new StateWriter(stream); Fix value = Read(game, target.Id).Value; long tick = checked(game.Tick + 1), exposure = 0, exposureAt = -1;
              writer.Fix(ref value); writer.Long(ref tick); writer.Long(ref exposure); writer.Long(ref exposureAt); future = stream.ToArray(); }
            part.Sync(new StateReader(future, 0, future.Length));
            c.Throws<InvalidOperationException>(() => game.Hash(), "sole checkpoint validator refuses future condition chronology");
            c.Throws<CorruptStateException>(() => game.World.System<EquipmentConditionSystem>().OnLoaded(game.World), "post-restore semantic validator refuses future condition chronology");
            part.Sync(new StateReader(original, 0, original.Length)); c.Equal(before, game.Hash(), "test instrumentation restores exact prior owner payload");
        }
        private static void NamedOwners(Check c)
        {
            GameFactory factory = Factory(j => {
                JsonValue first = Parts(j).Get("equipment_condition").Clone(), second = first.Clone();
                second.Set("id", JsonValue.Of("secondary")); second.Set("initial", JsonValue.Number("8"));
                var entries = JsonValue.NewArray(); entries.Add(first); entries.Add(second); Parts(j).Set("equipment_condition", entries);
            });
            SimGame game = New(factory); Entity target = Spawn(game); var view = game.Seat(0).View.Feature<EquipmentConditionView>();
            var ids = new List<Code>(); c.That(view.CopyIds(target.Id, ids) && ids.Count == 2, "two distinct named owners share one entity without a condition ledger");
            c.Throws<ArgumentException>(() => target.Get<IConditionProvider>(), "unnamed provider lookup cannot choose a condition by accident");
            c.Equal(Code.None, ConditionContracts.Commit(game.World, target.Get<IConditionProvider>(Source), Change(6)), "change targets exact first provider");
            c.That(view.Read(target.Id, new Code("secondary"), out var secondary).IsNone && secondary.Value == Fix.FromInt(8), "first provider damage cannot alter sibling condition");
            Tick(game); SimGame twin = factory.Load(game.Save()); Advance(game, 3, twin);
            c.Equal(game.Hash(), twin.Hash(), "multiple named conditions retain their own sole codecs across restore");
            Invalid(c, j => { var entries = JsonValue.NewArray(); entries.Add(Parts(j).Get("equipment_condition").Clone()); entries.Add(Parts(j).Get("equipment_condition").Clone());
                Parts(j).Set("equipment_condition", entries); }, "duplicate named condition ownership refuses before tick one");
            GameFactory wide = Factory(j => { Parts(j).Get("equipment_condition").Set("maximum", JsonValue.Number("2147483647"));
                Parts(j).Get("equipment_condition").Set("initial", JsonValue.Number("2147483647")); });
            SimGame wideGame = New(wide); Entity wideTarget = Spawn(wideGame);
            c.Equal(Fix.FromInt(int.MaxValue), Read(wideGame, wideTarget.Id).Maximum, "native representation has no invented100-point gameplay maximum");
            c.Equal(Code.None, ConditionContracts.Commit(wideGame.World, wideTarget.Get<IConditionProvider>(Source), Change(int.MaxValue)), "full representable amount damages exactly without overflow or clamp");
            Tick(wideGame); c.Equal(wideGame.Hash(), wide.Load(wideGame.Save()).Hash(), "full-width condition saves exactly");
        }
        private static void Mapless(Check c)
        {
            var definition = new GameDefinition("condition-only", new GameModule[] { new EquipmentConditionModule() });
            JsonValue original = Json.Parse(Colony.Defs), content = JsonValue.NewObject();
            foreach (string section in new[] { "entity_states", "time_definitions", "calendars", "scenarios" }) content.Set(section, original.Get(section).Clone());
            var authored = Json.Parse(FrameworkProofContent.Read("equipment-condition.json")); content.Set("units", authored.Get("units").Clone());
            content.Set("entities", Json.Parse("[{\"id\":\"bare-equipment\",\"kind\":\"structure\",\"components\":{\"equipment_condition\":{\"id\":\"integrity\",\"unit\":\"unit.condition\",\"maximum\":10,\"initial\":4,\"wear\":null,\"states\":{\"damaged\":\"stopped\",\"failed\":\"stopped\"}}}}]"));
            var scenario = content.Get("scenarios")[0]; scenario.Set("space", Json.Parse("null")); scenario.Set("modules", Json.Parse("[\"sim.equipment-condition\"]")); scenario.Set("settings", Json.Parse("{}"));
            var files = new PreloadedFileSource().Add("manifest.json", "{\"schema\":1,\"modules\":[{\"id\":\"sim.equipment-condition\",\"version\":\"1.0.0\",\"documents\":[\"condition.json\"]}]}").Add("condition.json", Text(content));
            var factory = new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes)); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 7 });
            Entity target = game.World.Spawn(game.World.Content.Get<EntityDef>("bare-equipment"), new PlayerId(0)); game.World.Entities.Flush();
            c.That(game.World.Space == null && !game.World.Services.Has<WorkBoard>() && !game.World.Services.Has<IResourceTransaction>(), "condition has no compulsory map, work, inventory or economy");
            c.Equal(Fix.FromInt(4), Read(game, target.Id).Value, "bare equipment owns real explicit condition");
            Tick(game); SimGame twin = factory.Load(game.Save()); Advance(game, 4, twin);
            c.Equal(game.Hash(), twin.Hash(), "mapless/no-work condition persists without a hidden service scheduler");
        }
        private static void Faults(Check c)
        {
            for (int mode = 1; mode <= 5; mode++)
            {
                int fault = mode; GameFactory factory = Factory(j => j.Get("entities")[1].Get("components").Get("proof_condition").Set("fault", JsonValue.Number(fault.ToString())));
                SimGame game = New(factory); Entity target = Spawn(game, proof: true);
                if (mode == 1 || mode == 5) game.RunTicks(1);
                else
                {
                    game.Seat(0).Submit(new PostOrderCommand { Target = target.Id, Plan = "equipment.service" });
                    for (int i = 0; i < 300 && game.Fault == null; i++) game.RunTicks(1);
                }
                c.That(game.Fault != null, "dishonest condition provider faults normal Update mode=" + mode);
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted condition cannot checkpoint mode=" + mode);
            }
        }
        private static void Independent(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity target = Spawn(game, proof: true);
            c.That(target.Get<IConditionProvider>(Source) is ProofCondition, "independent public-contract owner substitutes without scheduler branches");
            c.That(Post(game, target.Id, "equipment.service").Accepted, "same authored service targets public proof provider");
            TestKit.RunUntil(game, () => Read(game, target.Id).Value == Fix.FromInt(10), 500, 1);
            c.Equal(Fix.FromInt(10), Read(game, target.Id).Value, "native physical completion changes independent condition once");
            c.Equal(Fix.FromInt(27), target.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "same DAG consumes actual proof service materials");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "independent owner codec participates in same saved session");
            GameFactory otherUnits = Factory(j => {
                j.Get("entities")[1].Get("components").Get("proof_condition").Set("unit", JsonValue.Of("unit.steel"));
                j.Get("condition_requirements")[0].Set("unit", JsonValue.Of("unit.steel"));
                j.Get("condition_effects")[1].Set("unit", JsonValue.Of("unit.steel"));
            });
            SimGame otherGame = New(otherUnits); Entity otherTarget = Spawn(otherGame, proof: true);
            c.That(Read(otherGame, otherTarget.Id).Unit == new UnitId("unit.steel"), "public condition contract admits a game's actual alternative authored unit");
            c.That(Post(otherGame, otherTarget.Id, "equipment.service").Accepted, "same native effect/requirement consumers need no unit-specific scheduler branch");
            TestKit.RunUntil(otherGame, () => Read(otherGame, otherTarget.Id).Value == Fix.FromInt(10), 500, 1);
            c.Equal(Fix.FromInt(10), Read(otherGame, otherTarget.Id).Value, "typed alternative-unit provider completes actual physical service");
            c.Equal(otherGame.Hash(), otherUnits.Load(otherGame.Save()).Hash(), "alternative-unit owner restores with exact catalog identity");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity target = Spawn(game), foreign = Spawn(game, seat: 1);
            WideTimeTests.SetTick(game, 1L << 40); ResourceId steel = game.World.Content.ResourceId("steel"); IInventory store = target.Get<IInventory>();
            Entity crane = game.World.Entities.Get(Colony.Mine(game, 0, "crane")[0].Id); var start = crane.Get<ITilePresence>().Position;
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = target.Id, Capability = Process, Transformation = "equipment.batch", Count = 1 }).Accepted,
                "ordinary queue starts actual native WIP");
            c.That(Post(game, target.Id, "equipment.damage-work").Accepted, "normal activity commands stress actual equipment through physical work");
            c.Equal(Fix.FromInt(10), Read(game, target.Id).Value, "posting cannot instantly damage the target");
            Reach(game, target.Id, () => Read(game, target.Id).Value == Fix.FromInt(4), "physical damage"); Tick(game);
            c.That(crane.Get<ITilePresence>().Position != start, "machine moves before condition effect");
            c.That(Read(game, target.Id).ChangedTick >= (1L << 40), "condition chronology remains signed64 game time");
            c.Equal(new Code("equipment.unsafe"), State(game, target.Id).Blocker, "existing typed gate stops unsafe in-flight production");
            c.Equal(new Code("state.stopped"), target.State, "entity Update settles its authored damage state");
            var process = State(game, target.Id); SimGame twin = factory.Load(game.Save()); Advance(game, 20, twin);
            c.Equal(process.Progress, State(game, target.Id).Progress, "unsafe operation preserves WIP/progress without advancing");
            c.Equal(Fix.FromInt(4), Read(game, target.Id).Value, "no inferred per-tick wear or free repair");
            c.Equal(game.Hash(), twin.Hash(), "damaged equipment and blocked real process restore together");
            c.That(TestKit.Order(game, 0, new SetWorkPermitCommand { Target = target.Id, Enabled = false }).Accepted &&
                TestKit.Order(twin, 0, new SetWorkPermitCommand { Target = target.Id, Enabled = false }).Accepted, "players disable service readiness through normal command");
            CommandResult posted = Post(game, target.Id, "equipment.service"), other = Post(twin, target.Id, "equipment.service");
            c.That(posted.Accepted && other.Accepted && posted.Arg0 == other.Arg0, "service is native queued intent in both saved worlds");
            Order order = game.World.Services.Get<WorkBoard>().Order(checked((int)posted.Arg0)); Fix stock = store.Amount(steel); Advance(game, 15, twin);
            c.That(order.Node("repair").Progress.Equals(WorkProgress.Zero) && Read(game, target.Id).Value == Fix.FromInt(4), "blocked service earns no condition or work");
            c.Equal(stock, store.Amount(steel), "blocked service cannot spend spares");
            c.That(TestKit.Order(game, 0, new SetWorkPermitCommand { Target = target.Id, Enabled = true }).Accepted &&
                TestKit.Order(twin, 0, new SetWorkPermitCommand { Target = target.Id, Enabled = true }).Accepted, "player allows actual service machine work");
            Reach(game, target.Id, () => !order.Node("repair").Progress.Equals(WorkProgress.Zero), "service startup", twin);
            c.Equal(Fix.FromInt(4), Read(game, target.Id).Value, "partial positive service still cannot repair early");
            c.Equal(stock - Fix.FromInt(3), store.Amount(steel), "startup consumes native spares exactly once");
            c.Equal(Fix.FromInt(2), store.ReservedOut(steel), "service protects actual held tools");
            c.Equal(game.Hash(), twin.Hash(), "actual startup and partial service match saved twin");
            twin = factory.Load(game.Save()); Advance(game, 2, twin);
            c.Equal(game.Hash(), twin.Hash(), "save inside damaged service restores progress/spent inputs/tools");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted &&
                TestKit.Order(twin, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "ordinary cancellation interrupts incomplete service");
            c.Equal(Fix.FromInt(4), Read(game, target.Id).Value, "cancellation cannot apply completion repair");
            c.Equal(stock - Fix.FromInt(3), store.Amount(steel), "spent spares never fabricate cancellation refund");
            c.Equal(Fix.Zero, store.ReservedOut(steel), "cancellation releases only actual unspent tool claims");
            c.That(Post(game, target.Id, "equipment.service").Accepted && Post(twin, target.Id, "equipment.service").Accepted, "new service requires a fresh physical execution");
            Reach(game, target.Id, () => Read(game, target.Id).Value == Fix.FromInt(10), "completed physical repair", twin);
            c.Equal(stock - Fix.FromInt(6), store.Amount(steel), "cancelled and replacement service each pay only their actual startup inputs");
            c.Equal(Fix.Zero, store.ReservedOut(steel), "completed repair releases native tools");
            var repaired = Read(game, target.Id); Advance(game, 3, twin);
            c.That(Read(game, target.Id).Value == repaired.Value && Read(game, target.Id).ChangedTick == repaired.ChangedTick, "once-only effect cannot rerun on later updates");
            c.That(State(game, target.Id).Progress.WholeTicks > process.Progress.WholeTicks && State(game, target.Id).Blocker.IsNone,
                "same real process resumes only after condition is physically restored");
            c.Equal(Fix.FromInt(10), game.Observer.Feature<EquipmentConditionView>().Read(foreign.Id, Source, out var foreignValue).IsNone ? foreignValue.Value : Fix.Zero,
                "foreign equipment condition remains untouched");
            c.Equal(Fix.FromInt(30), foreign.Get<IInventory>().Amount(steel), "foreign supplies remain untouched");
            c.Equal(game.Hash(), twin.Hash(), "repaired equipment and resumed WIP remain deterministic across interleaved worlds");
            twin = factory.Load(game.Save()); Reach(game, target.Id, () => State(game, target.Id).QueueCount == 0, "resumed batch finish", twin);
            c.Equal(Fix.FromInt(23), store.Amount(steel), "physical conservation:30 minus2 batch minus6 actual service plus1 output");
            c.Equal(game.Hash(), twin.Hash(), "completed service and production round-trip exactly");
        }
    }
}
