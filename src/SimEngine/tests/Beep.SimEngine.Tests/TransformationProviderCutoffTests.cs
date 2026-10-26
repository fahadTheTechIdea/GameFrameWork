using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static partial class TransformationProviderCutoffTests
    {
        internal const string Case = "Production provider cutoff: staffing and advance failures stop later dispatch with saved WIP intact";
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add(ReadCase, PureParticipants);
        }
        private static GameFactory Factory()
        {
            var authored = Json.Parse(FrameworkProofContent.Read("transformer-proof.json"));
            // Setup uses dependency IDs, not the order a host happens to list entries.
            authored.Get("entity_setups")[0].Get("after").Add(JsonValue.Of("start"));
            // Existing sibling-part authoring pattern: one native processor, two independent processors.
            return TransformerTests.Factory(root => root.Get("entities")[0].Get("components").Set("pulse_transformer",
                authored.Get("entities")[0].Get("components").Get("pulse_transformer").Clone()), transformerDocument: authored.ToString());
        }
        private static PulseTransformer Source(SimGame game) => TransformerTests.Find(game, "assembler").Get<PulseTransformer>();
        private static PulseTransformer Later(SimGame game) => TransformerTests.Find(game, "emitter").Get<PulseTransformer>();
        private static Transformer Native(SimGame game) => TransformerTests.Find(game, "assembler").Get<Transformer>();
        private static ulong Codec(Component part) { var hash = new StateHasher(); part.Sync(hash); return hash.Value; }
        private static int Results(SimGame game) => game.World.Entities.All.Count(entity => entity.Alive && entity.Def.Id == "token");
        private static Fix Wip(SimGame game) => Native(game).WorkInProgress.Amount(game.World.Content.ResourceId("supply"));
        private static Fix Stock(SimGame game) => Native(game).Entity.Get<IInventory>().Amount(game.World.Content.ResourceId("supply"));
        private static SimGame Prepared(Check c, GameFactory factory, bool loaded)
        {
            var game = TransformerTests.New(factory);
            c.That(Native(game).Entity.Id.Value < Later(game).Entity.Id.Value, "authored setup dependency proves source precedes later dispatch");
            var intents = new[] {
                new EnqueueTransformationCommand { Transformer = Native(game).Entity.Id, Capability = new Code("transformation"), Transformation = "build", Count = 1 },
                new EnqueueTransformationCommand { Transformer = Source(game).Entity.Id, Capability = new Code("pulse"), Transformation = "pulse", Count = 1 },
                new EnqueueTransformationCommand { Transformer = Later(game).Entity.Id, Capability = new Code("pulse"), Transformation = "pulse", Count = 1 }
            };
            foreach (var command in intents) {
                ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary native/external queue preview");
                c.Equal(before, game.Hash(), "mixed production preview stays pure"); game.Seat(0).Submit(command);
            }
            game.Update(); var answers = new List<CommandResult>(); long cursor = 0; game.Seat(0).ReadResults(ref cursor, answers);
            c.That(answers.Count == 3 && answers.All(answer => answer.Accepted), "all three ordinary intents accepted by their original owners");
            c.That(Wip(game) == Fix.FromInt(3) && Stock(game) == Fix.FromInt(17) && Native(game).Progress.WholeTicks == 1,
                "actual native raw input is protected in WIP after one game minute");
            c.That(Results(game) == 0 && Source(game).ReadTransformation(game.World).Progress.WholeTicks == 1
                && Later(game).ReadTransformation(game.World).Progress.WholeTicks == 1, "independent results require real elapsed work");
            return loaded ? factory.Load(game.Save()) : game;
        }
        private static void Run(Check c)
        {
            foreach (bool loaded in new[] { false, true })
            {
                var factory = Factory(); var healthy = Prepared(c, factory, loaded); var twin = factory.Load(healthy.Save());
                for (int i = 0; i < 8 && Results(healthy) < 3; i++) {
                    healthy.Update(); twin.Update();
                    c.That(healthy.Fault == null && healthy.Hash() == twin.Hash(), "actual mixed providers retain saved deterministic continuation");
                }
                c.That(Results(healthy) == 3 && Wip(healthy).IsZero && Stock(healthy) == Fix.FromInt(18),
                    "one native conversion plus two earned external outputs, exact original material accounting");
                foreach (string stage in new[] { "staff-native", "staff-independent", "advance-independent" })
                foreach (string mode in new[] { "caught-native", "propagated-native", "direct-throw" })
                {
                    string context = stage + "/" + mode + "/loaded=" + loaded;
                    factory = Factory(); var game = Prepared(c, factory, loaded);
                    var independent = factory.Load(game.Save()); ulong isolated = independent.Hash();
                    var world = game.World; var first = Source(game); var later = Later(game);
                    ulong originalFirst = Codec(first), originalLater = Codec(later); long time = game.Tick;
                    var published = game.Seat(0).Snapshot; int outputBefore = Results(game);
                    int faults = 0, firstEntries = 0, laterEntries = 0, laterStaffing = 0;
                    EntityId preparedOutput = default;
                    var primary = new InvalidOperationException("proof.original-production-callback");
                    Action fail = () => {
                        faults++;
                        if (mode == "direct-throw") throw primary;
                        try { world.Spawn(world.Content.Get<EntityDef>("token"), first.Entity.Owner, _ => throw primary); }
                        catch (InvalidOperationException) { if (mode != "caught-native") throw; }
                    };
                    first.BeforeAdvanceProbe = _ => { firstEntries++; preparedOutput = world.Entities.NextIdentity; };
                    later.BeforeAdvanceProbe = _ => laterEntries++;
                    later.StaffingProbe = (_, _) => laterStaffing++;
                    if (stage == "advance-independent") first.AfterAdvanceProbe = _ => fail();
                    else first.StaffingProbe = (_, processor) => {
                        if (stage == "staff-native" ? ReferenceEquals(processor, Native(game)) : ReferenceEquals(processor, first)) fail();
                    };
                    game.Update();
                    c.Equal(1, faults, "one actual provider failure before first-fault cutoff: " + context);
                    c.Equal(stage == "advance-independent" ? 1 : 0, firstEntries, "staffing failure cannot enter processor Advance: " + context);
                    c.That(laterEntries == 0 && laterStaffing == 0, "no later processor or staffing callback after first failure: " + context);
                    c.Equal(originalLater, Codec(later), "later provider queue/progress/control codec unchanged: " + context);
                    c.Equal(mode == "direct-throw" ? TransformationModule.ModuleId : "entity.create", world.Fault?.Phase,
                        "original native/system phase retained, no wrapper masks first cause: " + context);
                    c.Equal("InvalidOperationException: proof.original-production-callback", world.Fault?.Message, "original production cause retained: " + context);
                    Entity earned = world.Entities.Get(preparedOutput);
                    c.That((stage == "advance-independent" ? originalFirst != Codec(first) && earned?.Def.Id == "token" && earned.Owner == first.Entity.Owner
                        : originalFirst == Codec(first)) && Results(game) == outputBefore,
                        "retain actual earlier creation in original pending store, never publish or fake rollback: " + context);
                    c.That(Wip(game) == Fix.FromInt(3) && Stock(game) == Fix.FromInt(17), "original native WIP/material ownership remains exact: " + context);
                    c.Equal(time, game.Tick, "failed production pass cannot complete time: " + context);
                    c.Equal(published.Tick, game.Seat(0).Snapshot.Tick, "failed pass retains last healthy publication: " + context);
                    c.That(!world.Events.HasPending, "no queued success prefix after terminal failure: " + context);
                    if (world.Fault != null) {
                        c.Throws<InvalidOperationException>(() => game.Save(), "failed production cannot save as healthy: " + context);
                        c.Throws<InvalidOperationException>(() => game.Hash(), "failed production cannot certify healthy hash: " + context);
                    }
                    c.Equal(isolated, independent.Hash(), "saved independent session unchanged by provider fault: " + context);
                    independent.Update(); c.That(independent.Tick > time && independent.Fault == null && Results(independent) == 2,
                        "independent original providers really continue timed production: " + context);
                }
            }
        }
    }
}
