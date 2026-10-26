using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class CanonicalReplayTests
    {
        internal const string Case = "Canonical replay: physical native and mod checkpoints with shuffled lockstep inputs";
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        private static readonly IReplayVerifier Verifier = new ReplayVerifier();
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "exact canonical replay identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "replay observation changes no persistent save payload");
            c.Throws<ArgumentOutOfRangeException>(() => new ReplayLimits(0, 1), "explicit positive byte budget");
            c.Throws<ArgumentOutOfRangeException>(() => new ReplayLimits(1, 0), "explicit positive record budget");
            Protocol(c); Native(c); Points(c); Network(c);
        }
        private static void Failure(Check c, ReplayVerification result, string code, string label)
            => c.That(!result.Success && result.Failure == new Code(code) && result.Game == null, label + " exposes no unverified live game");
        private static SimGame Verify(Check c, GameFactory factory, SimGame original, byte[] bytes, string label)
        {
            ulong before = original.Hash(); var result = Verifier.Verify(factory, bytes, Limits);
            c.That(result.Success && result.Game != null, label + "/" + result.Failure + "/" + result.Detail);
            if (!result.Success) throw new InvalidOperationException("replay refused " + label + "/" + result.Failure + "/" + result.Step + "/" + result.Detail);
            c.That(result.ActualHash == before && result.ExpectedHash == before && result.Step == original.Tick, label + " matches actual final state/time/pending inbox");
            c.Equal(before, original.Hash(), "verification creates an independent game, not a mutation of recording source");
            return result.Game;
        }
        private sealed class InputScript : PlayerController
        {
            private bool _sent;
            public override void Decide(PlayerDecisionContext context)
            { if (!_sent) { _sent = true; context.Submit(new PlaceBin { At = new TileCoord(-1, 5) }); } }
        }
        private static byte[] Reseal(byte[] prefix)
        {
            var artifact = new byte[prefix.Length + 8]; Buffer.BlockCopy(prefix, 0, artifact, 0, prefix.Length);
            ulong checksum = ReplayFormat.Checksum(prefix, prefix.Length);
            for (int i = 0; i < 8; i++) artifact[prefix.Length + i] = (byte)(checksum >> (i * 8));
            return artifact;
        }
        private static byte[] Change(byte[] source, int offset, ulong value, int size)
        {
            byte[] prefix = source.Take(source.Length - 8).ToArray();
            for (int i = 0; i < size; i++) prefix[offset + i] = (byte)(value >> (i * 8));
            return Reseal(prefix);
        }
        private static void Protocol(Check c)
        {
            // Diagnostic codec fixture only. Normal physical gameplay is in Native/Points below.
            var factory = TestGame.Factory(); var game = TestGame.New(901);
            var pending = new PlaceBin { At = new TileCoord(10, 10) }; game.Seat(0).Submit(pending); pending.At = new TileCoord(-9, -9);
            ulong initial = game.Hash(); var recorder = game.RecordReplay(Limits);
            c.Equal(initial, game.Hash(), "starting recorder does not change authoritative state/pending input");
            c.Throws<InvalidOperationException>(() => game.RecordReplay(Limits), "one recorder per session");
            game.Seat(0).Attach(new InputScript());
            bool callback = false; game.Updated += _ => {
                if (callback) return; callback = true; game.Seat(0).Submit(new PlaceBin { At = new TileCoord(-1, 9) });
            };
            game.RunTicks(2);
            c.That(callback && recorder.CommandCount == 2 && recorder.CheckpointCount == 2, "controller and Updated inputs recorded once at their actual boundaries");
            game.Clock.Pause(); long step = game.Tick; game.Update(); c.Equal(step, game.Tick, "pause produces no fake step checkpoint");
            game.Seat(0).Submit(new PlaceBin { At = new TileCoord(-1, 12) });
            byte[] artifact = recorder.Finish(); ulong final = game.Hash();
            c.That(!recorder.Active && recorder.Failure.IsNone, "successful recorder closes explicitly");
            c.Throws<InvalidOperationException>(() => recorder.Finish(), "closed recorder cannot publish twice");
            var description = ReplayVerifier.Describe(artifact, Limits);
            c.That(description.InitialHash == initial && description.FinalHash == final && description.InitialStep == 0 && description.FinalStep == 2 &&
                description.Commands == 3 && description.Checkpoints == 2 && description.Modules.Count > 0 && description.Build == EngineInfo.Build,
                "self-description retains actual build/content/modules/time/step identity and counts");
            c.That(description.Time == game.SimulationTime.Id && description.ContentFingerprint != 0, "authored time and content provenance retained");
            c.Throws<NotSupportedException>(() => ((IList<ReplayModuleInfo>)description.Modules).Clear(), "retained module identity collection is readonly");
            var replay = Verify(c, factory, game, artifact, "initial saved pending input plus final paused pending inputs");
            game.Clock.Resume(); game.Update(); replay.Update(); c.Equal(game.Hash(), replay.Hash(), "verified final pending inputs execute through original command phase");
            byte[] corrupted = (byte[])artifact.Clone(); corrupted[corrupted.Length / 2] ^= 1;
            Failure(c, Verifier.Verify(factory, corrupted, Limits), "replay.invalid", "checksum corruption");
            Failure(c, Verifier.Verify(factory, artifact.Take(artifact.Length - 1).ToArray(), Limits), "replay.invalid", "truncation");
            Failure(c, Verifier.Verify(factory, Change(artifact, 4 + ReplayFormat.Magic.Length, 99, 4), Limits), "replay.invalid", "unsupported replay schema");
            byte[] extra = artifact.Take(artifact.Length - 8).Concat(new byte[] { 7 }).ToArray();
            Failure(c, Verifier.Verify(factory, Reseal(extra), Limits), "replay.invalid", "correct-checksum trailing record");
            var parsed = ReplayFormat.Read(artifact, Limits); var checkpoint = parsed.Records.First(r => r.Kind == ReplayFormat.Checkpoint);
            var mismatch = Verifier.Verify(factory, Change(artifact, checkpoint.Offset + 9, checkpoint.Hash ^ 1, 8), Limits);
            Failure(c, mismatch, "replay.state", "correct-checksum mismatched state checkpoint");
            c.That(mismatch.Step == 1 && mismatch.Record >= 0 && mismatch.ExpectedHash != mismatch.ActualHash, "mismatch provides actual record/step/hash context");
            var answerReader = new StateReader(artifact, checkpoint.Offset, artifact.Length - checkpoint.Offset);
            byte answerKind = 0; long answerStep = 0; ulong answerHash = 0; PlayerId answerPlayer = PlayerId.None; string answerStream = null;
            answerReader.Byte(ref answerKind); answerReader.Long(ref answerStep); answerReader.ULong(ref answerHash); answerReader.Count(0);
            answerReader.Player(ref answerPlayer); answerReader.String(ref answerStream);
            Failure(c, Verifier.Verify(factory, Change(artifact, answerReader.Position, 999, 4), Limits), "replay.results", "correct-checksum changed command answer");
            Failure(c, Verifier.Verify(factory, Change(artifact, checkpoint.Offset + 1, 10, 8), Limits), "replay.invalid", "noncontiguous checkpoint sequence");
            var input = parsed.Records.First(r => r.Kind == ReplayFormat.Input);
            int nextOffset = parsed.Records.First(r => r.Offset > input.Offset).Offset;
            byte[] duplicate = artifact.Take(nextOffset).Concat(artifact.Skip(input.Offset).Take(nextOffset - input.Offset))
                .Concat(artifact.Skip(nextOffset).Take(artifact.Length - 8 - nextOffset)).ToArray();
            Failure(c, Verifier.Verify(factory, Reseal(duplicate), Limits), "replay.invalid", "duplicate canonical input despite valid checksum");
            Failure(c, Verifier.Verify(Colony.Factory(), artifact, Limits), "replay.identity", "another game's factory");
            Failure(c, Verifier.Verify(factory, artifact, new ReplayLimits(artifact.Length - 1, 12000)), "replay.invalid", "explicit artifact byte limit");
            var limitedGame = TestGame.New(903); var limited = limitedGame.RecordReplay(new ReplayLimits(Limits.MaxBytes, 1));
            limitedGame.RunTicks(2);
            c.That(!limited.Active && limited.Failure == new Code("replay.recording-limit") && limitedGame.Fault == null, "observation limit never caps or faults ordinary game progression");
            c.Throws<InvalidOperationException>(() => limited.Finish(), "incomplete recording cannot silently publish truncated replay");
            limitedGame.Update(); using (var next = limitedGame.RecordReplay(Limits)) { limitedGame.Update(); }
            c.That(limitedGame.Tick == 4 && limitedGame.Fault == null, "failed/disposed recorder releases ownership and ordinary game still runs");
            c.Throws<ArgumentException>(() => limitedGame.RecordReplay(new ReplayLimits(1, 1)), "initial save cannot exceed declared recording budget");
            using (var concurrent = limitedGame.RecordReplay(Limits))
            {
                Task producer = Task.Run(() => { for (int i = 0; i < 24; i++) limitedGame.Seat(0).Submit(new PlaceBin { At = new TileCoord(-1, i) }); });
                for (int i = 0; i < 200 && !producer.IsCompleted; i++) limitedGame.Update();
                c.That(producer.Wait(10000), "concurrent input producer finishes within explicit wall bound");
                limitedGame.Update(); c.Equal(24, concurrent.CommandCount, "threaded submissions observed exactly once through original inbox gate");
                Verify(c, factory, limitedGame, concurrent.Finish(), "concurrent admitted inputs");
            }
        }
        private static EntityView Mine(SimGame game, string prototype)
            => game.Seat(0).Snapshot.Entities.First(e => e.Def == prototype && e.Owner == new PlayerId(0));
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("recorded Update fault: " + game.Fault);
            if (twin == null) return;
            twin.Update(); if (twin.Fault != null || twin.Hash() != game.Hash()) throw new InvalidOperationException("recorded saved twins diverged at " + game.Tick);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !done(); i++) Update(game, twin);
            if (done()) return;
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            throw new InvalidOperationException(stage + "/time=" + game.Tick + "/fault=" + game.Fault + "/orders=" +
                string.Join(";", orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static Fix Stock(SimGame game, EntityId entity, string resource)
        {
            var facts = new List<Facet>(); if (!game.Seat(0).View.Inspect(entity, facts)) throw new InvalidOperationException("missing observed stock");
            return facts.Where(f => f.Key == Storage.FacetStock && f.Subject == resource).Aggregate(Fix.Zero, (a, f) => a + f.Value);
        }
        private static CommandResult Submit(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary recorded player preview");
            c.Equal(hash, game.Hash(), "recording cannot authorize preview mutation");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary recorded player command accepted");
            if (!result.Accepted) throw new InvalidOperationException("recorded player refused: " + result.Refusal);
            return result;
        }
        private static void Native(Check c)
        {
            var factory = Colony.Factory(); var game = Colony.New(907); var idle = Colony.New(907); ulong idleHash = idle.Hash();
            using (var recorder = game.RecordReplay(Limits))
            {
                var site = Submit(c, game, new ConstructCommand { Def = "tower", Priority = 60,
                    Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)) });
                c.That(game.Seat(0).Snapshot.Entities.Any(e => e.Id == site.Entity && e.Def == "site:tower"), "recording sees real unfinished site, never instant structure");
                bool moving = false, loaded = false;
                Reach(game, () => { moving |= game.Seat(0).Snapshot.Entities.Any(e => e.State == new Code("state.moving"));
                    var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs); loaded |= jobs.Any(j => j.IsHaul && j.Loaded > Fix.Zero);
                    return loaded && moving; }, "native recorded cargo travel");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "recording state is absent from physical cargo save");
                long step = game.Tick; game.Clock.Pause(); twin.Clock.Pause(); Update(game, twin); c.Equal(step, game.Tick, "sole pause does not create replay work/time");
                game.Clock.Resume(); twin.Clock.Resume(); game.Clock.Speed = 2; twin.Clock.Speed = 2;
                Reach(game, () => game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "native recorded delivered construction", twin);
                Fix remaining = Fix.Zero; foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0))) remaining += Stock(game, entity.Id, "steel");
                c.Equal(Fix.FromInt(20), remaining, "real50−30 construction conservation under recorded multi-step frames");
                byte[] artifact = recorder.Finish(); var verified = Verify(c, factory, game, artifact, "native movement/loaded hauling/construction");
                c.That(verified.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower") && ReplayVerifier.Describe(artifact, Limits).Checkpoints == game.Tick,
                    "every completed step and finished structure reproduced, not only final hash claimed");
            }
            c.Equal(idleHash, idle.Hash(), "native recording/replay leaves interleaved session independent");
        }
        private static void Points(Check c)
        {
            var factory = RegionalDiscoveryTests.Points(); var game = ConstructionPlanTests.NewPoints(factory);
            var machine = Mine(game, "machine").Id; var source = Mine(game, "source").Id; int order;
            using (var recorder = game.RecordReplay(Limits))
            {
                Submit(c, game, new ConfigureWorkPolicyCommand { Worker = machine, Settings = new WorkPolicySettings(true, 0,
                    new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
                order = checked((int)Submit(c, game, new PostOrderCommand { Plan = "field.discover", Target = source, Priority = 60 }).Arg0);
                Reach(game, () => { var nodes = new List<ActivityNodeView>(); return game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes)
                    && nodes.Any(n => n.ProductiveSteps > 0 && !n.Progress.Equals(WorkProgress.Zero)); }, "mod recorded partial discovery");
                c.That(game.Seat(0).View.Feature<PointLayerView>().Layers.Count == 0, "recording never grants early/private knowledge");
                Verify(c, factory, game, recorder.Finish(), "public mod partial physical work");
            }
            var saved = factory.Load(game.Save());
            using (var recorder = saved.RecordReplay(Limits))
            {
                Reach(saved, () => saved.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:field.discover", source, out var fact) && fact.Order == order,
                    "mod saved replay learning completion");
                c.Equal(Fix.FromInt(12), Stock(saved, source, "supply"), "actual saved discovery consumes8 supplies");
                c.That(saved.Seat(0).View.Feature<PointLayerView>().Layers.Count == 1 && saved.Seat(1).View.Feature<PointLayerView>().Layers.Count == 0,
                    "replayed learning is physically earned and company-private");
                var verified = Verify(c, factory, saved, recorder.Finish(), "public mod save-origin completion without resubmitted plan");
                c.Equal(Fix.FromInt(12), Stock(verified, source, "supply"), "verifier reconstructs actual saved resource ledger");
            }
        }
        private static void Exchange(SimGame from, SimGame to)
        {
            var messages = new List<byte[]>(); from.Net.DrainOutgoing(messages); messages.Reverse();
            foreach (byte[] message in messages) to.Net.Receive(message);
        }
        private static void Network(Check c)
        {
            var factory = TestGame.Factory(); var request = new NewGameRequest { Scenario = "flat", Seed = 911, Seats = { Controller.Human, Controller.Human } };
            var a = factory.NewGame(request); var b = factory.NewGame(request);
            a.StartLockstep(new LockstepOptions { LocalSeats = { 0 }, InputDelay = 2, ChecksumEvery = 1 });
            b.StartLockstep(new LockstepOptions { LocalSeats = { 1 }, InputDelay = 2, ChecksumEvery = 1 });
            using (var left = a.RecordReplay(Limits)) using (var right = b.RecordReplay(Limits))
            {
                a.Seat(0).Submit(new PlaceBin { At = new TileCoord(-1, 8) }); b.Seat(1).Submit(new PlaceBin { At = new TileCoord(-1, 11) });
                c.That(left.CommandCount == 0 && right.CommandCount == 0, "network buffered proposals are not recorded as admitted gameplay");
                for (int i = 0; i < 32; i++) { a.RunTicks(1); b.RunTicks(1); Exchange(a, b); Exchange(b, a); }
                c.That(a.Tick == b.Tick && a.Hash() == b.Hash() && a.Net.State != NetState.Broken && b.Net.State != NetState.Desynced,
                    "original lockstep reaches identical state despite reversed packet delivery");
                c.That(left.CommandCount == 2 && right.CommandCount == 2, "both peers record exactly the two actual logical admissions");
                byte[] l = left.Finish(), r = right.Finish(); c.That(l.SequenceEqual(r), "same canonical engine recording despite different local seats and transport chronology");
                Verify(c, factory, a, l, "shuffled lockstep logical stream"); Verify(c, factory, b, r, "other peer logical stream");
            }
        }
    }
}
