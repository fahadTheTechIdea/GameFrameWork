using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class IndependentTileLineTests
    {
        internal const string Case = "Independent tile lines: physical underlay activates owned corridor and original network";
        internal const string NativeCase = "Native tile lines: physical underlay activates owned corridor and original network";
        internal const string AdmissionCase = "Tile line geometry: authored references and path admission remain authoritative";
        internal static void Register()
        {
            TestRunner.Add(Case, c => Physical(c, false));
            TestRunner.Add(NativeCase, c => Physical(c, true));
            TestRunner.Add(AdmissionCase, Admission);
        }
        private static GameFactory Factory(bool native, Action<JsonValue> author = null)
            => IndependentTileWorkTests.Factory(nativeTiles: native, lines: true, authorLines: author);
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "independent-sensor", Seed = 723, Seats = { Controller.Human, Controller.Human } });
        private static List<PlanView> Plans(SimGame game, int viewer = 0)
        { var rows = new List<PlanView>(); game.Seat(viewer).View.Feature<WorkView>().CopyPlans(rows); return rows; }
        private static Fix Stock(SimGame game)
        {
            Fix total = Fix.Zero; var facts = new List<Facet>();
            foreach (var entity in game.Seat(0).Snapshot.Entities) {
                game.Seat(0).View.Inspect(entity.Id, facts);
                foreach (var fact in facts) if (fact.Key == Storage.FacetStock && fact.Subject == "steel") total += fact.Value;
            }
            return total;
        }
        private static LayLineCommand Lay(string line)
        { var command = new LayLineCommand { Line = line }; for (int x = 14; x <= 16; x++) command.Tiles.Add(new TileCoord(x, 16)); return command; }
        private static long Submit(Check c, SimGame game, string line)
        {
            var command = Lay(line); ulong hash = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(command), "engine accepts persistent authored line intent");
            c.Equal(hash, game.Hash(), "line intent preview changes neither clock, stock nor topology");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary corridor command: " + result.Refusal);
            if (!result.Accepted) throw new InvalidOperationException("line intent refused " + result.Refusal);
            return result.Arg0;
        }
        private static void Update(Check c, SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("line fault " + game.Fault + "/" + twin?.Fault);
            if (twin != null) c.Equal(game.Hash(), twin.Hash(), "original line/material/work/layer/network saves continue identically");
        }
        private static EntityId Entity(SimGame game, string name) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == name).Id;
        private static void Physical(Check c, bool native)
        {
            var factory = Factory(native); var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
            c.That(native ? game.World.Space is TileMap : game.World.Space is SensorTileSpace && !game.World.Services.Has<TileMap>(), "actual authored provider executes shared tile geometry");
            c.That(!game.World.Services.Has<IEconomy>(), "physical infrastructure has no mandatory business");
            var view = game.Seat(0).View.Feature<InfrastructureView>(); var path = new List<TileCoord>();
            c.Equal(Fix.FromInt(12), Stock(game), "only original finite authored materials exist");
            c.Equal(PathSearchResult.Unreachable, view.SuggestLinePath("sensor.pipe", new TileCoord(14,16), new TileCoord(16,16), path), "engine route obeys missing underlay policy");
            c.Equal(0, path.Count, "refused route exposes no prefix");
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = game.RecordReplay(limits);
            long pipe = Submit(c, game, "sensor.pipe"); var pending = Plans(game).Single(p => p.Id == pipe);
            c.That(pending.State == PlanState.Pending && pending.Order == 0 && pending.Blocker == new Code("sensor.requires-underlay"), "original plan waits for actual terrain before creating a work marker");
            c.Equal(Fix.FromInt(12), Stock(game), "blocked line consumes no material");
            long underlay = Submit(c, game, "sensor.underlay-line"); var twin = factory.Load(game.Save());
            bool loadedSaved = false, workSaved = false, pipeSaved = false; int updates = 0;
            var jobs = new List<WorkJobView>(); var nodes = new List<ActivityNodeView>();
            bool Done() => Plans(game).Single(p => p.Id == pipe).State == PlanState.Done;
            while (!Done() && updates++ < 400)
            {
                Update(c, game, twin);
                game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                if (!loadedSaved && jobs.Any(j => j.IsHaul && j.Loaded.IsPositive)) {
                    loadedSaved = true; twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual loaded construction cargo restores during travel");
                }
                var plans = Plans(game);
                foreach (var plan in plans.Where(p => p.Order > 0))
                    if (!workSaved && game.Seat(0).View.Feature<WorkView>().TryCopyNodes(plan.Order, nodes) &&
                        nodes.Any(n => n.Progress.WholeTicks > 0 && n.Progress.WholeTicks < n.RequiredTicks)) {
                        workSaved = true; twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "productive segment and cursor checkpoint restores");
                        long step = game.Clock.StepIndex; ulong hash = game.Hash(); game.Clock.Pause(); twin.Clock.Pause();
                        // Pause requests are part of authoritative replay/clock state; compare after pausing.
                        hash = game.Hash(); Update(c, game, twin); c.Equal(step, game.Clock.StepIndex, "one paused clock freezes line work and hauling");
                        c.Equal(hash, game.Hash(), "paused line creates no topology or materials"); game.Clock.Resume(); twin.Clock.Resume();
                    }
                var current = plans.Single(p => p.Id == pipe);
                if (!pipeSaved && current.Order > 0) {
                    pipeSaved = true; c.Equal(PlanState.Done, plans.Single(p => p.Id == underlay).State, "earned complete underlay activates the existing corridor plan");
                    twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "activated owned corridor restores through original execution owners");
                }
            }
            if (!Done()) throw new InvalidOperationException("corridor stalled clock=" + game.Clock.StepIndex + " plans=" + string.Join(";", Plans(game).Select(p => p.State + "/" + p.Blocker + "/" + p.Order)));
            c.That(loadedSaved && workSaved && pipeSaved && updates < 400, "physical corridor includes hauling, productive work and pending-to-active saves");
            c.Equal(Fix.FromInt(6), Stock(game), "two three-segment corridors consume exactly six original materials");
            c.Equal(PathSearchResult.Found, view.SuggestLinePath("sensor.pipe", new TileCoord(14,16), new TileCoord(16,16), path), "engine route sees earned complete corridor underlay");
            c.Equal(3, path.Count, "complete engine route includes both endpoints");
            c.Equal(PathSearchResult.Found, view.SuggestLinkPath("sensor.pipe", Entity(game,"sensor.line-start"), Entity(game,"sensor.line-end"), path), "engine calculates structure endpoints on native and independent geometry");
            c.Equal(3, path.Count, "structure route uses same engine corridor rules");
            var pools = new List<NetworkPoolView>(); c.Equal(Code.None, view.CopyPools("sensor.network", pools), "original network publishes immutable pool facts");
            c.That(pools.Count == 1 && pools[0].MemberCount == 3 && pools[0].Company == new PlayerId(0) && pools[0].Stock.Count == 0,
                "original topology owns one three-cell private empty network, not another graph or invented contents");
            c.Equal(Code.None, game.Seat(1).View.Feature<InfrastructureView>().CopyPools("sensor.network", pools), "foreign network query is safely filtered");
            c.Equal(0, pools.Count, "foreign company sees no private pool or retained prefix");
            c.Equal(0, Plans(game, 1).Count, "private line intent and progress stay in engine views");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed corridor and original topology checkpoint");
            c.Equal(isolated, idle.Hash(), "interleaved session receives no corridors, network or stock changes");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(replay.Success && replay.Game?.Hash() == game.Hash(), "ordinary corridor/underlay construction replay: " + replay.Failure + "/" + replay.Detail);
        }
        private static void Admission(Check c)
        {
            foreach (bool native in new[] { false, true }) {
                foreach (Action<JsonValue> invalid in new Action<JsonValue>[] {
                    json => json.Get("lines")[0].Set("layer", JsonValue.Of("absent.layer")),
                    json => json.Get("lines")[1].Set("owner_layer", JsonValue.Of("sensor.underlay")),
                    json => json.Get("lines")[0].Set("value", JsonValue.Number("256")),
                    json => json.Get("tile_line_placement_policies")[1].Get("footprint").Set("conditions", Json.Parse("[\"absent.condition\"]"))
                }) c.Throws<ContentException>(() => New(Factory(native, invalid)), "malformed authored corridor refuses before simulation");
                var game = New(Factory(native)); var command = Lay("sensor.pipe"); command.Tiles.Add(new TileCoord(14,16)); ulong hash = game.Hash();
                c.Equal(LineCodes.NotContiguous, game.Seat(0).Preview(command), "engine rejects duplicate/noncontiguous line intent");
                c.Equal(hash, game.Hash(), "path refusal changes no authoritative state");
            }
        }
    }
}
