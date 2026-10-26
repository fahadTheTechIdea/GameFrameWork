using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using TileProof = Beep.SimEngine.Tests.TileConditionContractTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string NativeTileConditionCase = "Networked tile conditions: native wet gates partial cargo and full physical construction";
        internal const string IndependentTileConditionCase = "Networked tile condition contracts: independent wet gates partial cargo and full physical construction";
        private static string TilePredicateDiagnostics(SimGame game) => "at=" + game.Clock.StepIndex + "/steel=" + TileProof.Steel(game) +
            "/jobs=" + string.Join(";", TileProof.Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Worker + "/" + j.Loaded + "/" + j.Blocker));
        private static Pair TilePredicatePair(SimGame a, SimGame b)
        {
            void Observe(SimGame game)
            {
                ulong before = game.Hash(); Fix stock = TileProof.Steel(game);
                if (stock < Fix.FromInt(20) || stock > Fix.FromInt(50)) throw new InvalidOperationException("tile work escaped original fifty minus thirty material balance");
                if (game.Seat(0).Preview(TileProof.Build(35, 5)) != new Code("colony.on-water") ||
                    game.Seat(0).Preview(TileProof.Move(game, 35, 5)) != new Code("core.unreachable"))
                    throw new InvalidOperationException("same original wet predicate no longer gates placement and routing");
                if (game.Hash() != before) throw new InvalidOperationException("tile gate observations mutated state");
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, TilePredicateDiagnostics, 0);
        }
        private static Pair TilePredicateRestore(Check c, GameFactory factory, Pair source)
        {
            var saved = TilePredicatePair(factory.Load(source.A.Save()), factory.Load(source.B.Save()));
            c.Equal(source.A.Hash(), saved.A.Hash(), "original map samples claims cargo work and navigation restore exactly");
            c.Equal(source.B.Hash(), saved.B.Hash(), "peer physical tile checkpoint retains original identity"); return saved;
        }
        private static void TilePredicateRun(Check c, string kind)
        {
            var factory = TileProof.Create(new TileConditionContractModule(), kind);
            SimGame New() => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 877, Seats = { Controller.Human, Controller.Human } });
            var pair = TilePredicatePair(New(), New());
            Console.WriteLine("MODULES tile-predicate-" + kind + "=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(pair.A.Modules.Contains(TileConditionsModule.ModuleId) && pair.A.Modules.Contains(TileConstructionRulesModule.ModuleId) &&
                !pair.A.Modules.Contains(TycoonModule.ModuleId), "original shared tile predicates and physical construction need no business");
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            int blocked = Submit(c, pair, TileProof.Build(35, 5), new Code("colony.on-water"));
            int route = Submit(c, pair, TileProof.Move(pair.A, 35, 5), new Code("core.unreachable"));
            int built = Submit(c, pair, TileProof.Build(10, 16));
            bool Loaded(SimGame game) => TileProof.Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero) && !TileProof.Mine(game, "tower").Any();
            Reach(pair, Loaded, "real accepted dry project starts physical loading and travel");
            c.That(Result(pair.A, 0, blocked)?.Refusal == new Code("colony.on-water") && Result(pair.A, 0, route)?.Refusal == new Code("core.unreachable") &&
                Result(pair.A, 0, built)?.Accepted == true && TileProof.Mine(pair.A, "site:tower").Length == 1,
                "same immutable predicate refusals reach canonical answers without extra sites or instant structures");
            var site = Result(pair.A, 0, built).Value.Entity;
            c.That(pair.A.Seat(0).View.Feature<ConstructionView>().TryReadSite(site, out var facts) && facts.Order > 0, "public physical project owns sole activity order");
            int order = facts.Order;
            var restored = TilePredicateRestore(c, factory, pair);
            var cargoLeft = restored.A.RecordReplay(limits); var cargoRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "native clock pause during actual tile cargo");
            long at = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, g => g.Clock.StepIndex >= at + 4, "saved tile peers independently advance actual physical cargo"); pair.Round();
            c.That(pair.A.Clock.StepIndex == at && pair.A.Hash() == frozen && restored.A.Clock.StepIndex > at, "no map cargo reservation or clock borrowed across sessions");
            pair.A.Net.RequestResume();
            bool Erecting(SimGame game) => LoadNodes(game, order).Any(n => n.Id == "erect" && n.Progress.WholeTicks > 0 && n.Progress.WholeTicks < n.RequiredTicks);
            Both(pair, restored, Erecting, "full original two-hour prep and delivery lead to staffed three-hour erection prefix");
            var erect = LoadNodes(pair.A, order).Single(n => n.Id == "erect");
            c.That(erect.RequiredTicks == 180 && LoadNodes(pair.A, order).Single(n => n.Id == "prep").RequiredTicks == 120 &&
                erect.Worker == TileProof.Mine(pair.A, "crane").Single().Id && !TileProof.Mine(pair.A, "tower").Any(),
                "authored hours stay actual game minutes and required original crane does work at site");
            ReplayPair(c, factory, cargoLeft, cargoRight, restored, limits, 0, "saved physical tile cargo through full preparation to staffed prefix replay");
            restored = TilePredicateRestore(c, factory, pair);
            var workLeft = restored.A.RecordReplay(limits); var workRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new SetWorkerHeldCommand { Worker = TileProof.Mine(current.A, "crane").Single().Id, Held = true });
            Both(pair, restored, g => Result(g, 0, 4)?.Accepted == true, "ordinary crane hold admitted on four worlds");
            var held = LoadNodes(pair.A, order).Single(n => n.Id == "erect").Progress;
            c.That(held.WholeTicks >= erect.Progress.WholeTicks && held.WholeTicks < erect.RequiredTicks, "hold preserves only genuinely earned work");
            ReplayPair(c, factory, workLeft, workRight, restored, limits, 1, "saved earned tile work through ordinary crane hold replay");
            restored = TilePredicateRestore(c, factory, pair);
            var heldLeft = restored.A.RecordReplay(limits); var heldRight = restored.B.RecordReplay(limits);
            long until = pair.A.Clock.StepIndex + 5;
            Both(pair, restored, g => g.Clock.StepIndex >= until, "saved held crane leaves productive progress stopped while sole clock advances");
            c.That(LoadNodes(pair.A, order).Single(n => n.Id == "erect").Progress.Equals(held) && !TileProof.Mine(pair.A, "tower").Any(), "held machine neither works remotely nor completes early");
            foreach (var current in new[] { pair, restored }) Submit(c, current, new SetWorkerHeldCommand { Worker = TileProof.Mine(current.A, "crane").Single().Id, Held = false });
            Both(pair, restored, g => TileProof.Mine(g, "tower").Length == 1, "same original project resumes and finishes full physical construction");
            c.That(TileProof.Steel(pair.A) == Fix.FromInt(20) && TileProof.Mine(pair.A, "site:tower").Length == 0,
                "one original replacement consumes exactly thirty material units with no orphaned construction site");
            ReplayPair(c, factory, heldLeft, heldRight, restored, limits, 1, "saved held crane to resumed completed physical construction replay");
            ReplayPair(c, factory, left, right, pair, limits, 5, "full shared predicate physical tile command stream replay");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && pair.A.Hash() == restored.B.Hash(), "native shuffled transport converges across four actually advanced tile worlds");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "completed map and original work state checkpoint");
        }
    }
}
