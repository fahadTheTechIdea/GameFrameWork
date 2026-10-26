using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static class CommandStreamTests
    {
        internal const string Case = "Command streams: selected producer identities isolate counters, pending saves and actual answers";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static SetControllerCommand Input() => new SetControllerCommand { Controller = Controller.Human };
        private static byte[] Counters(params (string Stream, int Sequence)[] rows)
        {
            using var output = new MemoryStream(); var writer = new StateWriter(output);
            writer.Count(1); writer.Count(rows.Length);
            foreach (var row in rows) { string id = row.Stream; int value = row.Sequence; writer.String(ref id); writer.Int(ref value); }
            writer.Flush(); return output.ToArray();
        }
        private static void Run(Check c)
        {
            var factory = ConstructionPlanTests.Points(); var game = ConstructionPlanTests.NewPoints(factory);
            var autonomous = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 719,
                Seats = { Controller.Autopilot, Controller.Human } });
            ulong autonomousBefore = autonomous.Hash();
            c.Throws<ArgumentException>(() => autonomous.StartLockstep(new LockstepOptions { LocalSeats = { 0 } }),
                "an initially autonomous seat cannot fabricate undeclared transport ownership");
            c.That(autonomous.Net == null && !autonomous.Clock.Sequenced && autonomous.Hash() == autonomousBefore,
                "invalid transport setup leaves authoritative state and attachment unchanged");
            autonomous.StartLockstep(new LockstepOptions { LocalSeats = { 1 }, InputDelay = 0 });
            c.Equal(Code.None, autonomous.Seat(0).Preview(Input()), "transport attachment does not change deterministic gameplay admission");
            c.Equal(Code.None, autonomous.World.Check(new PlayerId(0), Input()), "ordinary command check and preview agree independently of transport ownership");
            c.Equal(autonomousBefore, autonomous.Hash(), "transport-independent preview is pure and allocates no command identity");
            var company = new PlayerId(0); var work = new Code(WorkModule.ModuleId); var motion = new Code(MotionModule.ModuleId);
            ulong initial = game.Hash();
            c.Throws<ArgumentException>(() => game.World.Submit(company, Input(), Code.None), "unstated producer never aliases player input");
            c.Throws<ArgumentException>(() => game.World.Submit(company, Input(), new Code("mod.not-selected")), "unselected producer is refused before allocation");
            c.Equal(initial, game.Hash(), "invalid producer submission changes neither counter nor pending state");
            c.Equal(1, game.Seat(0).Submit(Input()), "external player owns first player-stream ticket");
            c.Equal(1, game.World.Submit(company, Input(), work), "selected work producer independently owns ticket one");
            c.Equal(1, game.World.Submit(company, Input(), motion), "selected motion producer independently owns ticket one");
            foreach (var stream in new[] { CommandStreams.Player, work, motion }) {
                c.That(game.World.IsPendingCommand(company, stream, 1), "exact producer pending identity exists");
                c.Equal(1, game.World.LastCommandSequence(company, stream), "each producer has only its actual allocation");
            }
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "same-number different-producer pending commands save without ambiguity");
            game.Update(); twin.Update(); c.Equal(game.Hash(), twin.Hash(), "restored producer-scoped commands execute identically");
            var answers = new List<CommandResult>(); long cursor = 0; game.Seat(0).ReadResults(ref cursor, answers);
            c.That(answers.Count == 3 && answers.All(a => a.Accepted && a.Sequence == 1) && answers.Select(a => a.Stream).Distinct().Count() == 3,
                "actual result stream preserves all three identities, not three copies of a shared ticket");
            c.That(answers.Select(a => a.Stream.Id).SequenceEqual(answers.Select(a => a.Stream.Id).OrderBy(id => id, StringComparer.Ordinal)),
                "same-time same-company tie order uses stable ordinal producer identity");
            var stamped = Input(); stamped.Player = company; stamped.Stream = work; stamped.Sequence = 1; stamped.IssuedTick = game.Clock.StepIndex;
            ulong before = game.Hash(); c.Throws<ArgumentException>(() => game.World.SubmitStamped(stamped), "duplicate exact producer ticket cannot be applied again");
            c.Equal(before, game.Hash(), "duplicate refusal changes no authoritative state");
            c.That(game.World.Commands.Copy(stamped).Stream == work, "native command snapshot retains explicit producer identity");
            foreach (var stream in new[] { CommandStreams.Player, work, motion }) {
                c.Equal(2, game.World.Submit(company, Input(), stream), "each restored stream continues its own monotonic identity");
                c.Equal(2, twin.World.Submit(company, Input(), stream), "saved twin allocates the same native next identity");
            }
            c.Equal(game.Hash(), twin.Hash(), "all pending producer counters remain twin-identical");
            var inbox = new CommandInbox(game.World.Commands, 1, game.Modules);
            Parallel.For(0, 64, _ => inbox.Submit(company, work, Input(), 0));
            var pending = inbox.Drain();
            c.That(pending.Length == 64 && pending.Select(command => command.Sequence).OrderBy(value => value).SequenceEqual(Enumerable.Range(1, 64)),
                "sole inbox lock allocates unique native producer tickets under concurrent callers");
            c.Equal(1, inbox.Submit(company, motion, Input(), 0), "independent selected stream is not advanced by concurrent work allocations");
            foreach (var rows in new[] {
                new[] { ("", 1) }, new[] { ("mod.not-selected", 1) }, new[] { (work.Id, 0) }, new[] { (work.Id, -1) },
                new[] { (work.Id, 1), (work.Id, 2) }, new[] { (work.Id, 1), (CommandStreams.Player.Id, 1) } }) {
                var invalid = new CommandInbox(game.World.Commands, 1, game.Modules); byte[] data = Counters(rows);
                c.Throws<CorruptStateException>(() => invalid.SyncSequences(new StateReader(data, 0, data.Length)),
                    "restoration rejects missing/unknown/nonpositive/duplicate/noncanonical producer identity");
            }
            var result = answers[0]; using var bytes = new MemoryStream(); var codec = new StateWriter(bytes);
            CommandResult.Sync(codec, ref result); codec.Flush(); var image = bytes.ToArray(); var restored = new CommandResult();
            CommandResult.Sync(new StateReader(image, 0, image.Length), ref restored);
            c.Equal(result, restored, "actual command-result codec retains producer along with actor and sequence");
        }
    }
}
