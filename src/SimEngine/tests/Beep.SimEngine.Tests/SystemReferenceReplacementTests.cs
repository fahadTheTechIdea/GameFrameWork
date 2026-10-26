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
    internal static class SystemReferenceReplacementTests
    {
        internal const string Case = "System replacement references: declared order aliases pure refusal and saved physical continuation";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool alias, string mode = "normal")
        {
            var file = Json.Parse(FrameworkProofContent.Read("system-replacement-references.json"));
            file.Get("system_reference_settings")[0].Set("alias", JsonValue.Of(alias));
            file.Get("system_reference_settings")[0].Set("mode", JsonValue.Of(mode));
            return EntityReferenceReplacementTests.Factory(moduleDocument: file.ToString());
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest
        { Scenario = "points", Seed = 31, Seats = new List<Controller> { Controller.Human, Controller.Human }, Mods = { ResourceFlowsModule.ModuleId } });
        private static EntityId Find(SimGame game, string def) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == def).Id;
        private static List<SystemReferenceFact> Facts(SimGame game, int seat = 0)
        { var facts = new List<SystemReferenceFact>(); game.Seat(seat).View.Feature<SystemReferenceView>().Copy(facts); return facts; }
        private static void Send(Check c, SimGame game, Command command)
        {
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary public command admission");
            int sequence = game.Seat(0).Submit(command); game.Update(); long cursor = 0;
            var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Any(a => a.Sequence == sequence && a.Accepted), "engine inbox commits player intent");
        }
        private static void Run(Check c)
        {
            foreach (bool alias in new[] { false, true })
            {
                var factory = Factory(alias); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
                var old = Find(game, "source");
                Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true });
                Send(c, game, new SetSystemReferencesCommand { Target = old });
                var beforeFacts = Facts(game); c.Equal(3, beforeFacts.Count, "three actual mod systems own references");
                c.That(beforeFacts.All(f => f.Reference == old && f.Commits == 0), "public configuration grants no replacement");
                var codecs = game.Seat(0).View.StateCodecs.Where(codec => codec.Kind == StateCodecKind.System && codec.Owner == "proof.references" && codec.Id.StartsWith("proof.reference.")).ToArray();
                c.Equal(3, codecs.Length, "systems retain one declared persistence owner each, including service alias");
                var command = new PostOrderCommand { Plan = "upgrade", Target = old }; ulong before = game.Hash();
                c.Equal(Code.None, game.Seat(0).Preview(command), "system reference admission is pure"); c.Equal(before, game.Hash(), "no checks mutate reference system state");
                Send(c, game, command); var twin = factory.Load(game.Save());
                c.That(game.Seat(0).Snapshot.Entities.Any(e => e.Id == old), "player posting does not instantly replace");
                int steps = 0;
                while (!game.Seat(0).Snapshot.Entities.Any(e => e.Def == "source.next") && steps++ < 25)
                { game.Update(); twin.Update(); if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("system reference fault: " + game.Fault + "/" + twin.Fault); }
                var made = Find(game, "source.next"); var facts = Facts(game);
                c.That(made != old && facts.All(f => f.Reference == made && f.Commits == 1), "every system-only owner commits once; service alias never doubles");
                c.Equal(alias ? 2 : 1, facts.Single(f => f.Id == "proof.reference.first").Ordinal, "remaining systems follow actual declared order");
                c.Equal(alias ? 1 : 2, facts.Single(f => f.Id == "proof.reference.alias").Ordinal, "existing service alias position takes precedence exactly once");
                c.Equal(3, facts.Single(f => f.Id == "proof.reference.last").Ordinal, "scrambled registration order does not reorder callbacks");
                c.That(beforeFacts.All(f => f.Reference == old && f.Commits == 0), "prior detached facts do not change");
                c.Equal(0, Facts(game, 1).Count, "other company cannot inspect system references");
                c.Equal(game.Hash(), twin.Hash(), "pending saved upgrade continues identically with system owners");
                game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
                c.That(Facts(game).All(f => f.Reference == made && f.Commits == 1), "new references persist through their own system codecs");
                for (int i = 0; i < 5; i++) { game.Update(); twin.Update(); }
                c.Equal(game.Hash(), twin.Hash(), "normal subsequent updates retain system-owned state");
                c.Equal(isolatedHash, isolated.Hash(), "another interleaved session stays unchanged");
                var flows = game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0);
                c.That(flows.Entries.Count(e => e.Item.IsTransfer && e.Item.Source.Endpoint == ResourceEndpointId.ForEntity(old) &&
                    e.Item.Destination.Endpoint == ResourceEndpointId.ForEntity(made) && e.Item.Amount == Fix.FromInt(20)) == 1,
                    "system participation does not duplicate actual replacement resource transfer");
            }
            foreach (string mode in new[] { "deny", "mutate-check", "throw-commit" })
            {
                var factory = Factory(true, mode); var game = New(factory); var old = Find(game, "source");
                Send(c, game, new SetWorkerHeldCommand { Worker = Find(game, "machine"), Held = true });
                Send(c, game, new SetSystemReferencesCommand { Target = old });
                var command = new PostOrderCommand { Plan = "upgrade", Target = old };
                if (mode == "deny")
                {
                    ulong before = game.Hash(); Code refusal = new Code("proof.system-reference-denied");
                    c.Equal(refusal, game.Seat(0).Preview(command), "system-only owner refuses before mutation"); c.Equal(before, game.Hash(), "typed refusal preserves state");
                    int sequence = game.Seat(0).Submit(command); game.Update(); long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
                    c.That(answers.Any(a => a.Sequence == sequence && a.Refusal == refusal), "preview and normal inbox report identical system refusal");
                    c.That(game.Fault == null && Facts(game).All(f => f.Reference == old && f.Commits == 0), "refused owners commit nothing");
                    c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused reference state stays coherent");
                }
                else
                {
                    if (mode == "mutate-check") c.Throws<InvalidOperationException>(() => game.Seat(0).Preview(command), "pure system check mutation is observed");
                    else
                    {
                        Send(c, game, command);
                        for (int i = 0; i < 25 && game.Fault == null; i++) game.Update();
                    }
                    c.That(game.Fault != null, "bad system callback faults original engine boundary");
                    c.Throws<InvalidOperationException>(() => game.Save(), "no healthy save follows bad system callback");
                    c.Throws<InvalidOperationException>(() => game.Hash(), "no healthy hash follows bad system callback");
                }
            }
        }
    }
}
