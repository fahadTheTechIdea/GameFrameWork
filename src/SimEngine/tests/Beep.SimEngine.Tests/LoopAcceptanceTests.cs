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

namespace Beep.SimEngine.Tests
{
    // Explicit authored input, ordinary player commands/public views/clock and lifecycle assertions only.
    internal static class LoopAcceptanceTests
    {
        internal const string Case = "Engine loop acceptance: physical births removals once-only entities reentry pause backlog outcome fault and pure snapshots";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Probe(JsonValue entities, string mode = "healthy")
        {
            for (int i = 0; i < entities.Count; i++)
                entities[i].Get("components").Set("loop_probe", Json.Parse("{\"mode\":\"healthy\",\"at\":null}"));
            if (mode != "healthy")
                for (int i = 0; i < entities.Count; i++)
                    if (entities[i].Get("id").Text == "destination")
                        entities[i].Get("components").Set("loop_probe", Json.Parse("{\"mode\":\"" + mode + "\",\"at\":2}"));
        }
        private static GameFactory Factory(LoopObservationModule module, string source = "healthy", string snapshot = "none", long snapshotAt = 3, bool horizon = false)
        {
            var document = Json.Parse(FrameworkProofContent.Read("loop-observation.json"));
            document.Get("loop_observations")[0].Set("snapshot_fault", JsonValue.Of(snapshot));
            document.Get("loop_observations")[0].Set("snapshot_at", snapshot == "none" ? JsonValue.Null() : JsonValue.Number(snapshotAt.ToString()));
            return ConstructionPlanTests.Points(edit: construction => Probe(construction.Get("entities")), additionalModule: module,
                moduleDeclaration: "{\"id\":\"proof.loop-observation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"loop-observation.json\"]}",
                moduleDocument: document.ToString(), worldEdit: world => {
                    Probe(world.Get("entities"), source);
                    world.Get("entities")[2].Get("components").Get("work_policy").Set("profile", JsonValue.Of("loop.worker"));
                    world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module.Id));
                    if (horizon) world.Get("time_definitions")[0].Set("horizon", Json.Parse("{\"numerator\":3,\"denominator\":1,\"unit\":\"minute\"}"));
                });
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static EntityView? Find(SimGame game, string prototype)
        { foreach (var entity in game.Seat(0).Snapshot.Entities) if (entity.Def == prototype) return entity; return null; }
        private static List<PlanView> Plans(SimGame game)
        { var rows = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(rows); return rows; }
        private static void Once(Check c, SimGame game, LoopObservation trace, long from, int count)
        {
            for (long at = from; at < from + count; at++)
            {
                var rows = trace.Reads.Where(x => x.At == at).ToArray();
                c.That(rows.Select(x => x.Entity).SequenceEqual(game.Snapshot.Entities.Select(x => x.Id)),
                    "each live entity updates once in stable ID order at sole clock coordinate " + at);
            }
        }
        private static void Step(Check c, SimGame game, LoopObservation trace, SimGame twin = null)
        {
            long at = game.Clock.StepIndex;
            c.Equal(1, game.Update(), "normal Update completes its one authored step");
            if (game.Fault != null) throw new InvalidOperationException("lifecycle Update at=" + at + " fault=" + game.Fault);
            Once(c, game, trace, at, 1);
            if (twin != null) { twin.Update(); c.Equal(game.Hash(), twin.Hash(), "saved physical lifecycle twin"); }
        }
        private static void Until(Check c, SimGame game, LoopObservation trace, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 256 && !ready(); i++) Step(c, game, trace, twin);
            if (!ready()) throw new InvalidOperationException(stage + " exceeded256Updates at=" + game.Clock.StepIndex + " entities="
                + string.Join(";", game.Snapshot.Entities.Select(x => x.Id + "/" + x.Def + "/" + x.State))
                + " plans=" + string.Join(";", Plans(game).Select(x => x.Id + "/" + x.State + "/" + x.Blocker)));
        }
        private static SimGame Input(Check c, GameFactory factory, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary lifecycle input admits");
            c.Equal(hash, game.Hash(), "command preview changes no state/RNG/time");
            game.Seat(0).Submit(command); return factory.Load(game.Save());
        }
        private static void Run(Check c)
        {
            BirthRemoval(c); Pacing(c); Reentry(c); Outcome(c);
            foreach (string mode in new[] { "throw", "recursive", "swallow-recursive", "step", "swallow-step" }) SourceFault(c, mode);
            foreach (string mode in new[] { "throw", "mutate" }) { SnapshotFault(c, mode, 0); SnapshotFault(c, mode, 3); }
        }
        private static void BirthRemoval(Check c)
        {
            var module = new LoopObservationModule(); var factory = Factory(module); var game = New(factory); var trace = module.Sessions[0];
            c.That(game.Initialized && game.Clock.StepIndex == 0 && trace.Reads.Count == 0 && module.Initialize.Initializations == 1, "initial publication is not a hidden entity Update");
            var initial = game.Seat(0).Snapshot; ulong original = game.Hash(); long revision = initial.Revision; game.Initialize();
            c.That(game.Hash() == original && game.Snapshot.Revision == revision && trace.Reads.Count == 0, "Initialize is idempotent with no gameplay/source callback");
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var replay = game.RecordReplay(limits);
            var command = new ConstructCommand { Def = "point.tower", Placement = Json.Parse("{\"space\":\"field.points\",\"position\":12}"), Priority = 70 };
            var twin = Input(c, factory, game, command); c.That(!Find(game, "point.tower-site").HasValue && !Find(game, "point.tower").HasValue, "queued command creates neither instant site nor finished building");
            Step(c, game, trace, twin); var site = Find(game, "point.tower-site").Value; var siteFacts = game.Seat(0).Snapshot;
            c.That(site.State == new Code("point.building") && site.StateSince == 0 && siteFacts.Spawned.Contains(site.Id),
                "command-phase newborn participates in same entity phase at real birth coordinate");
            c.That(trace.Reads.Count(x => x.Entity == site.Id && x.At == 0) == 1, "newborn has one issued Update, never zero/twice");
            twin = factory.Load(game.Save()); Until(c, game, trace, () => Find(game, "point.tower").HasValue, "actual supplied construction", twin);
            var building = Find(game, "point.tower").Value; long builtAt = game.Clock.StepIndex - 1;
            c.That(game.Clock.StepIndex > 2 && building.Id != site.Id && !Find(game, "point.tower-site").HasValue, "physical machine/material/work completion replaces and retires actual site");
            c.That(game.Snapshot.Spawned.Contains(building.Id) && game.Snapshot.Removed.Contains(site.Id)
                && trace.Reads.Count(x => x.Entity == building.Id && x.At == builtAt) == 1 && !trace.Reads.Any(x => x.Entity == site.Id && x.At == builtAt),
                "system-phase target birth precedes entity phase, old site removal precedes any later callback");
            long siteReads = trace.Reads.Count(x => x.Entity == site.Id);
            var retained = game.Seat(0).Snapshot; var retainedBuilding = building;
            twin = Input(c, factory, game, new DemolishCommand { Target = building.Id });
            Step(c, game, trace, twin); c.That(Find(game, "point.tower").HasValue, "demolition is timed work, not immediate destruction");
            twin = factory.Load(game.Save()); Until(c, game, trace, () => !Find(game, "point.tower").HasValue, "physical demolition", twin);
            long removedAt = game.Clock.StepIndex - 1;
            c.That(game.Snapshot.Removed.Contains(building.Id) && !trace.Reads.Any(x => x.Entity == building.Id && x.At == removedAt), "removed target never updates after demolition's effect checkpoint");
            Step(c, game, trace, twin);
            c.That(trace.Reads.Count(x => x.Entity == site.Id) == siteReads && retained.Entities.Single(x => x.Id == building.Id).Version == retainedBuilding.Version,
                "retired source never runs again and retained built entity facts stay detached");
            c.That(initial.Entities.Count == 3 && siteFacts.Entities.Any(x => x.Id == site.Id) && retained.Entities.Any(x => x.Id == building.Id), "three retained publication epochs survive physical births/removals");
            ulong settled = game.Hash(); int readCount = trace.Reads.Count;
            for (int i = 0; i < 3; i++) { var snapshot = game.Snapshot; var seat = game.Seat(0).Snapshot; var rows = new List<EntityView>(); game.Seat(0).View.CopyEntities(rows); }
            c.That(game.Hash() == settled && trace.Reads.Count == readCount && game.Seat(1).Snapshot.Entities.Count == 0, "snapshot/view reads never update state or leak private entities");
            c.Throws<NotSupportedException>(() => ((IList<EntityView>)retained.Entities).Clear(), "snapshot collection is immutable even through collection cast");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "removed identities/birth-state ages restore without startup grants");
            c.That(new ReplayVerifier().Verify(factory, replay.Finish(), limits).Success, "ordinary physical construction/removal lifecycle replays");
            var separate = New(factory); separate.Update(); c.That(Find(separate, "point.tower") == null && separate.Snapshot.Entities.Count == 3, "interleaved session has no birth/removal leakage");
        }
        private static void Pacing(Check c)
        {
            var module = new LoopObservationModule(); var factory = Factory(module); var game = New(factory); var trace = module.Sessions[0];
            game.Clock.TicksPerSecond = 10; game.Clock.CatchUpCap = 1;
            c.Equal(1, game.Update(350000), "elapsed frame spends exactly one capped tick"); Once(c, game, trace, 0, 1);
            c.That(game.Clock.PendingTicks == 2 && game.Clock.Alpha > Fix.Zero && game.Clock.IsRunningSlowly, "whole backlog and fractional interpolation retained");
            Fix alpha = game.Clock.Alpha; ulong hash = game.Hash(); int reads = trace.Reads.Count;
            game.Clock.Pause(); c.Equal(0, game.Update(9000000), "pause earns no hidden game work");
            c.That(game.Hash() == hash && trace.Reads.Count == reads && game.Clock.PendingTicks == 2 && game.Clock.Alpha == Fix.Zero, "pause preserves prior pacing but no entities/time advance");
            game.Clock.Resume(); c.That(game.Clock.Alpha == alpha, "resume reveals retained fractional work");
            c.Equal(1, game.Update(0), "zero elapsed spends original backlog"); Once(c, game, trace, 1, 1);
            c.Equal(1L, game.Clock.PendingTicks, "only actual completed tick debits backlog");
            var twin = factory.Load(game.Save()); game.Clock.Pause(); c.Equal(2, game.RunTicks(2), "explicit diagnostic batch uses same kernel and documented pause bypass");
            twin.RunTicks(2); c.Equal(game.Hash(), twin.Hash(), "local pause/backlog is not another saved game clock"); Once(c, game, trace, 2, 2);
            c.Equal(1L, game.Clock.PendingTicks, "diagnostic batch retains previously earned platform time");
            game.Clock.Resume(); c.Equal(1, game.Update(0), "original pending paced step remains spendable"); twin.Update(); c.Equal(game.Hash(), twin.Hash(), "elapsed and normal paths settle identical simulation");
            c.Equal(0L, game.Clock.PendingTicks, "all earned whole pacing settles exactly");
        }
        private static void Reentry(Check c)
        {
            var module = new LoopObservationModule(); var factory = Factory(module); var game = New(factory); int callbacks = 0;
            game.Updated += _ => {
                callbacks++; c.Throws<InvalidOperationException>(() => game.Update(), "Updated cannot recursively Update");
                c.Throws<InvalidOperationException>(() => game.Initialize(), "Updated cannot enter Initialize");
                c.Throws<InvalidOperationException>(() => game.RunTicks(1), "Updated cannot enter batch");
            };
            c.Equal(2, game.RunTicks(2), "one batch uses original engine pipeline"); c.Equal(1, callbacks, "one Updated event per public lifecycle call, not per underlying tick");
            Action<GameUpdate> failed = _ => throw new InvalidOperationException("loop.probe.host-observer");
            game.Updated += failed; c.Throws<InvalidOperationException>(() => game.Update(), "host observer exception reports after simulation settlement"); game.Updated -= failed;
            c.That(game.Fault == null && game.Clock.StepIndex == 3 && game.Snapshot.Tick == 3, "host listener cannot poison completed engine state");
            c.Equal(1, game.Update(), "listener exception releases lifecycle guard"); c.Equal(3, callbacks, "successful and failed calls each publish one update");
        }
        private static void Outcome(Check c)
        {
            var module = new LoopObservationModule(); var factory = Factory(module, horizon: true); var game = New(factory); var trace = module.Sessions[0];
            game.Clock.CatchUpCap = 64; game.Clock.TicksPerSecond = 10;
            c.Equal(3, game.Update(2000000), "horizon cuts off earned batch at exact authored boundary"); Once(c, game, trace, 0, 3);
            c.That(game.State == GameState.Ended && game.Outcome == World.HorizonOutcome && game.Clock.StepIndex == 3 && game.Clock.PendingTicks == 0,
                "one clock completes boundary step, discards terminal pacing and runs no fourth step");
            ulong hash = game.Hash(); int reads = trace.Reads.Count;
            c.That(game.Update() == 0 && game.RunTicks(50) == 0 && game.Hash() == hash && trace.Reads.Count == reads, "all later lifecycle paths stop at outcome without entity callbacks");
            var loaded = factory.Load(game.Save()); c.That(loaded.State == GameState.Ended && loaded.Hash() == hash && loaded.Update() == 0, "ended session restores as ended, never restarts work");
        }
        private static void SourceFault(Check c, string mode)
        {
            var module = new LoopObservationModule(); var game = New(Factory(module, source: mode)); var trace = module.Sessions[0];
            c.That(game.Snapshot.Entities[0].Def == "destination", "authored fault target is first actual entity, with later peers available: " + mode);
            Step(c, game, trace); Step(c, game, trace); var retained = game.Seat(0).Snapshot;
            c.Equal(0, game.Update(), "failed source completes no step: " + mode);
            c.That(game.Fault != null && game.Clock.StepIndex == 2 && trace.Reads.Count(x => x.At == 2) == 1,
                "first source failure cuts off later entity callbacks and sole time advancement: " + mode + " clock=" + game.Clock.StepIndex + " fault=" + game.Fault
                + " reads=" + string.Join(",", trace.Reads.Where(x => x.At == 2).Select(x => x.Entity.ToString())));
            c.That(game.Snapshot.State == GameState.Faulted && game.Seat(0).Snapshot.Tick == retained.Tick && game.Seat(0).Snapshot.Spawned.Count == 0
                && game.Seat(0).Snapshot.Changed.Count == 0 && game.Seat(0).Snapshot.Removed.Count == 0, "fault publishes only last-safe immutable facts: " + mode);
            Stopped(c, game, trace, mode);
        }
        private static void SnapshotFault(Check c, string mode, long at)
        {
            var module = new LoopObservationModule(); var game = New(Factory(module, snapshot: mode, snapshotAt: at)); var trace = module.Sessions[0];
            if (at != 0)
            {
                Step(c, game, trace); Step(c, game, trace); game.Clock.CatchUpCap = 1; game.Clock.TicksPerSecond = 10;
                c.Equal(1, game.Update(1000000), "snapshot fault occurs after actual completed update, not a forged tick: " + mode);
                c.That(game.Clock.StepIndex == 3 && game.Snapshot.Tick == 2 && game.Seat(0).Snapshot.Tick == 2 && game.Seat(1).Snapshot.Tick == 2,
                    "later private snapshot throw/mutation withholds all newly captured prefixes: " + mode);
            }
            else c.That(game.Initialized && game.Snapshot.Entities.Count == 0 && game.Seat(0).Snapshot.Entities.Count == 0 && game.Seat(1).Snapshot.Entities.Count == 0
                && trace.Reads.Count == 0 && game.Clock.StepIndex == 0, "initial private capture failure publishes no observer/company prefix: " + mode);
            c.That(game.Fault != null && game.State == GameState.Faulted && game.Clock.PendingTicks == 0 && game.Seat(1).Snapshot.Viewer == new PlayerId(1),
                "snapshot purity failure latches first cause and keeps private publication identity: " + mode);
            Stopped(c, game, trace, mode);
        }
        private static void Stopped(Check c, SimGame game, LoopObservation trace, string label)
        {
            var fault = game.Fault; int reads = trace.Reads.Count, visibility = trace.Visibility.EntityReads;
            c.That(game.Update(1000000) == 0 && game.RunTicks(50) == 0 && trace.Reads.Count == reads && trace.Visibility.EntityReads == visibility && ReferenceEquals(fault, game.Fault),
                "first fault prevents entity/view retries and later batches: " + label);
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted partial state cannot be saved: " + label);
            c.Throws<InvalidOperationException>(() => game.Hash(), "faulted partial state cannot be certified: " + label);
        }
    }
}
