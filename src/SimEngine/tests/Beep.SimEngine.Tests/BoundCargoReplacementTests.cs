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
using static Beep.SimEngine.Tests.PermanentRemovalTests;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Content composition and ordinary public player actions only; engine owners execute replacement and hauling.
    internal static class BoundCargoReplacementTests
    {
        internal const string Case = "Bound cargo replacement: authored targets, physical work and saved inventory identity";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(Action<JsonValue> edit = null, Action<JsonValue> worldEdit = null,
            IReadOnlyList<GameModule> extraModules = null, JsonValue extraDeclarations = null)
        {
            var content = Json.Parse(FrameworkProofContent.Read("bound-cargo-replacement.json")); edit?.Invoke(content);
            if ((extraModules == null) != (extraDeclarations == null)) throw new ArgumentException("extra cargo modules and declarations must be authored together");
            var modules = new List<GameModule> { new LogisticsRecoveryPolicyModule(), new LogisticsRecoveryPolicyProofModule() };
            var declarations = Json.Parse("[{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"proof.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\"}],\"documents\":[\"bound-cargo-replacement.json\"]}]");
            if (extraModules != null) modules.AddRange(extraModules);
            if (extraDeclarations != null) for (int i = 0; i < extraDeclarations.Count; i++) declarations.Add(extraDeclarations[i].Clone());
            return EntityReferenceReplacementTests.Factory(edit: root => {
                StartupContentAuthoring.Position(root, "machine", JsonValue.Number("70"));
                StartupContentAuthoring.Position(root, "crew", JsonValue.Number("78"));
                StartupContentAuthoring.Include(root, "cargo.start");
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyProofModule.ModuleId));
                root.Get("scenarios")[0].Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId,
                    Json.Parse("{\"profile\":\"cargo.manual-recovery\"}"));
                worldEdit?.Invoke(root);
            }, extraModules: modules, extraDeclarations: declarations.ToString(),
            extraDocuments: new Dictionary<string,string> { ["bound-cargo-replacement.json"] = content.ToString() });
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "points", Seed = 53, Seats = new List<Controller> { Controller.Human, Controller.Human },
            Mods = { ResourceFlowsModule.ModuleId } });
        private static CargoDispositionSnapshot Cargo(SimGame game, EntityId carrier)
        {
            var refusal = game.Seat(0).View.Feature<LogisticsView>().ReadCargoDisposition(carrier, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("bound cargo facts: " + refusal);
            return value;
        }
        private static JsonValue Binding(JsonValue content) => content.Get("entities")[0].Get("components").Get("proof_bound_cargo");
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recorder = game.RecordReplay(limits);
            EntityId carrier = Entity(game,"cargo.carrier"), old = Entity(game,"cargo.container"), courier = Entity(game,"machine");
            Submit(c,game,new SetWorkerHeldCommand { Worker = courier, Held = true });
            int haul = checked((int)Submit(c,game,new HaulResourcesCommand { Source = Entity(game,"source"), Destination = Entity(game,"destination"),
                Worker = carrier, Resource = Supply(game), Ceiling = Fix.FromInt(8), Priority = 100 }).Arg0);
            var twin = Save(c,factory,game);
            Reach(game,twin,()=>Job(game,haul).Loaded == Fix.FromInt(8),"bound carrier physically loads original stock");
            Submit(c,game,new SetWorkerHeldCommand { Worker = carrier, Held = true });
            c.That(!Jobs(game).Any(x=>x.Id==haul) && Facts(game,old).OutboundPromised.IsZero,"holding releases original cargo claims, not stock");
            var before = Cargo(game,carrier); Fix position = At(game,carrier);
            c.That(before.Endpoint == ResourceEndpointId.ForEntity(old) && before.HasResidualCargo && Facts(game,old).Stock == Fix.FromInt(8),
                "carrier references its actual separately placed inventory");
            c.Equal(Fix.Zero,Facts(game,carrier).Stock,"carrier-local inventory is not a cargo mirror");
            int order = checked((int)Submit(c,game,new PostOrderCommand { Target = old, Plan = "cargo.upgrade" }).Arg0);
            twin = Save(c,factory,game);
            Reach(game,twin,()=>Node(game,order,"upgrade").Progress.WholeTicks > 0,"real inspection crew starts three-minute upgrade");
            c.That(Exists(game,old) && Facts(game,old).Stock == Fix.FromInt(8),"pending work consumes time, not replacement stock");
            twin = Save(c,factory,game); game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash();
            c.Equal(0,game.Update(1000000),"sole paused clock freezes physical upgrade");
            c.Equal(0,twin.Update(1000000),"saved paused clock freezes same work");
            c.That(game.Hash()==paused && twin.Hash()==paused,"pending paused checkpoint identity"); game.Clock.Resume(); twin.Clock.Resume();
            Reach(game,twin,()=>game.Seat(0).Snapshot.Entities.Any(x=>x.Def=="cargo.container.next"),"physical cargo-owner upgrade completes");
            EntityId made = Entity(game,"cargo.container.next"); var after = Cargo(game,carrier);
            c.That(!Exists(game,old) && after.Endpoint == ResourceEndpointId.ForEntity(made),"same carrier rebinds to actual replacement inventory before source retirement");
            c.Equal(Fix.FromInt(8),Facts(game,made).Stock,"original receipts relocate all retained cargo once");
            c.Equal(position,At(game,carrier),"inventory replacement never teleports the held carrier");
            c.Equal(Fix.FromInt(75),At(game,made),"replacement stays at actual cargo-container placement");
            c.Equal(Fix.FromInt(20),Total(game),"physical relocation conserves original total");
            c.That(before.Endpoint==ResourceEndpointId.ForEntity(old) && before.HasResidualCargo,"retained cargo facts stay historical");
            c.Equal(Codes.NotFound,game.Seat(1).View.Feature<LogisticsView>().ReadCargoDisposition(carrier,out _),"cargo binding remains company-private");
            twin = Save(c,factory,game);
            Submit(c,game,new SetWorkerHeldCommand { Worker = courier, Held = false });
            Submit(c,game,new HaulResourcesCommand { Source = made, Destination = Entity(game,"destination"), Worker = courier,
                Resource = Supply(game), Ceiling = Fix.FromInt(8), Priority = 100 });
            twin = Save(c,factory,game);
            Reach(game,twin,()=>Facts(game,Entity(game,"destination")).Stock==Fix.FromInt(8) && !Jobs(game).Any(),"ordinary courier physically delivers replacement-container stock");
            c.That(Facts(game,made).Stock.IsZero && !Cargo(game,carrier).HasResidualCargo,"rebound cargo reads actual emptied inventory");
            c.Equal(position,At(game,carrier),"later delivery does not move the held carrier");
            c.Equal(Fix.FromInt(20),Total(game),"delivery has no creation, refund or duplicate inventory");
            var restored = factory.Load(game.Save());
            c.Equal(game.Hash(),restored.Hash(),"terminal cargo binding and claims save coherently");
            c.That(Cargo(restored,carrier).Endpoint==ResourceEndpointId.ForEntity(made) && !Cargo(restored,carrier).HasResidualCargo,
                "restored provider resolves the actual replacement, not the retired identity");
            c.Equal(isolatedHash,isolated.Hash(),"interleaved independent session unchanged");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"public cargo replacement and delivery replay identically");

            var deniedFactory = Factory(json=>Binding(json).Set("cargo_replacements",JsonValue.NewArray())); var denied = New(deniedFactory);
            var command = new PostOrderCommand { Target = Entity(denied,"cargo.container"), Plan = "cargo.upgrade" };
            var incompatible = new Code("proof.cargo-replacement-incompatible"); ulong unchanged = denied.Hash();
            c.Equal(incompatible,denied.Seat(0).Preview(command),"unlisted destination refuses before work admission");
            c.Equal(unchanged,denied.Hash(),"typed cargo replacement refusal is pure");
            int sequence = denied.Seat(0).Submit(command); PermanentRemovalTests.Step(denied); long cursor = 0; var results = new List<CommandResult>();
            denied.Seat(0).ReadResults(ref cursor,results);
            c.That(results.Any(x=>x.Sequence==sequence && x.Refusal==incompatible),"normal inbox returns same unlisted-target refusal");
            c.That(Exists(denied,command.Target) && !denied.Seat(0).Snapshot.Entities.Any(x=>x.Def=="cargo.container.next"),"refusal grants no replacement");
            c.Equal(denied.Hash(),deniedFactory.Load(denied.Save()).Hash(),"refused state remains saveable");
            foreach (Action<JsonValue> invalid in new Action<JsonValue>[] {
                json=>Binding(json).Remove("cargo_replacements"),
                json=>Binding(json).Set("cargo_replacements",JsonValue.Null()),
                json=>Binding(json).Set("cargo_replacements",Json.Parse("[1]")),
                json=>Binding(json).Set("cargo_replacements",Json.Parse("[\" \" ]")),
                json=>Binding(json).Set("cargo_replacements",Json.Parse("[\"cargo.container\"]")),
                json=>Binding(json).Set("cargo_replacements",Json.Parse("[\"missing\"]")),
                json=>Binding(json).Set("cargo_replacements",Json.Parse("[\"cargo.container.next\",\"cargo.container.next\"]")),
                json=>Binding(json).Set("cargo_replacements",Json.Parse("[\"reference\"]")) })
                c.Throws<ContentException>(()=>New(Factory(invalid)),"invalid explicit cargo metadata fails composition");
        }
    }
}
