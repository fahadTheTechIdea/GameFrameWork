using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Tests
{
    // Authored content, public views/one clock/Update/save only. Negative writes occur inside the mod.
    internal static class RegionalEntityQueryTests
    {
        internal const string Case = "Regional entity queries: selected space purity private geometry and atomic fault output";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(TilePresenceBindingModule proof) => TilePresenceBindingTests.Factory(proof, edit: root =>
        {
            var scenario = root.Get("scenarios")[0];
            if (scenario.Get("settings") == null) scenario.Set("settings", JsonValue.NewObject());
            scenario.Get("settings").Set(proof.Id, Json.Parse("{\"owner_visibility\":true}"));
        });
        private static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest
        { Scenario = "colony", Seed = 618, Seats = { Controller.Human, Controller.Human } });
        private static TileMapView View(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<TileMapView>();
        private static TileRect Whole(TileMapView view) => new TileRect(0, 0, view.Width, view.Height);
        private static void Healthy(Check c)
        {
            var proof = new TilePresenceBindingModule(); var f = Factory(proof); var idle = New(f); ulong untouched = idle.Hash(); var g = New(f);
            var own = View(g); var foreign = View(g, 1); var observer = g.Observer.Feature<TileMapView>();
            ulong hash = g.Hash(); var rows = new List<EntityView>();
            c.Equal(Code.None, own.CopyEntities(Whole(own), rows), "healthy original regional query");
            c.That(rows.Count > 0 && rows.All(x => x.Owner == new PlayerId(0)), "owner visibility comes from engine contract, not host filtering");
            c.That(rows.Select(x => x.Id).SequenceEqual(g.Seat(0).Snapshot.Entities.Select(x => x.Id)), "regional rows agree with authorized immutable publication");
            var retained = rows.ToArray(); c.Equal(Code.None, foreign.CopyEntities(Whole(foreign), rows), "foreign own regional stream");
            c.That(rows.Count > 0 && rows.All(x => x.Owner == new PlayerId(1)) && !rows.Any(x => retained.Any(a => a.Id == x.Id)), "foreign rows never disclose owner's geometry");
            c.Equal(Code.None, observer.CopyEntities(Whole(observer), rows), "explicit observer regional stream");
            c.That(rows.Count == g.Snapshot.Entities.Count && rows.Any(x => x.Owner == new PlayerId(0)) && rows.Any(x => x.Owner == new PlayerId(1)), "observer authorization covers both actual companies");
            c.Equal(Codes.OutOfBounds, own.CopyEntities(new TileRect(-1, 0, 1, 1), rows), "authored world bounds refuse query without geometry default");
            c.Equal(0, rows.Count, "geometry refusal publishes no prefix");
            c.Equal(Code.None, own.CopyEntities(new TileRect(0, 0, 0, own.Height), rows), "empty valid region has explicit success"); c.Equal(0, rows.Count, "empty region returns no invented entity");
            c.Equal(hash, g.Hash(), "regional/private/observer/refused reads mutate no space state RNG or time");
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "query adds no saved state");
            g.Update(); twin.Update(); c.That(g.Fault == null && twin.Fault == null && g.Hash() == twin.Hash(), "healthy native query/clock saved continuation");
            c.That(retained.All(x => x.Spatial is TileSpatialSnapshot), "retained spatial facts remain detached native shapes");
            c.Equal(untouched, idle.Hash(), "regional queries do not affect another session");
        }
        private static void Negative(Check c, string mode, bool overlap)
        {
            var proof = new TilePresenceBindingModule(); var f = Factory(proof); var idle = New(f); ulong untouched = idle.Hash(); var g = New(f);
            var view = View(g); var own = g.Seat(0).Snapshot; var foreign = g.Seat(1).Snapshot;
            var actor = own.Entities.Single(x => x.Def == "factory"); var footprint = ((TileSpatialSnapshot)actor.Spatial).Footprint;
            var region = overlap ? Whole(view) : new TileRect(view.Width - 1, view.Height - 1, 1, 1);
            c.That(overlap || !region.Overlaps(footprint), "negative nonoverlapping query is genuinely disjoint from callback owner");
            var rows = new List<EntityView> { actor }; proof.ArmRegion("factory", mode);
            c.Throws<InvalidOperationException>(() => view.CopyEntities(region, rows), "footprint callback cannot mutate actual selected space: " + mode);
            c.That(g.Fault != null && rows.Count == 0 && proof.RegionProbeReads == 1, "invalid query publishes no seed/partial prefix and invokes bad callback once");
            if (mode == "throw-after-space") c.That(g.Fault.Message.Contains("proof.regional-query-original-error"), "original callback exception retained");
            var first = g.Fault; long at = g.Clock.StepIndex;
            c.That(ReferenceEquals(own.Space, g.Seat(0).Snapshot.Space) && ReferenceEquals(foreign.Space, g.Seat(1).Snapshot.Space), "each viewer keeps its own last healthy spatial publication");
            c.Throws<InvalidOperationException>(() => view.CopyEntities(Whole(view), rows), "faulted later queries cannot retry bad callback");
            c.Equal(0, rows.Count, "later refusal cannot disclose map mutation");
            g.Update(); c.That(ReferenceEquals(first, g.Fault) && at == g.Clock.StepIndex && proof.RegionProbeReads == 1, "original first fault stops all time and callback retries");
            c.Throws<InvalidOperationException>(() => g.Save(), "query mutation cannot become healthy checkpoint");
            c.Throws<InvalidOperationException>(() => g.Hash(), "query mutation cannot become healthy hash");
            c.Equal(untouched, idle.Hash(), "bad regional getter cannot mutate another session");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual selected-space query guard build");
            Healthy(c);
            foreach (string mode in new[] { "space", "space-restore", "throw-after-space" }) foreach (bool overlap in new[] { false, true }) Negative(c, mode, overlap);
            TilePresenceBindingTests.Physical(c, true);
        }
    }
}
