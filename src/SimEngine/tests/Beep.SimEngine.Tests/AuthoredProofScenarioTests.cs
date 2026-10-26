using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Only authored content assembly, player intents, immutable facts and bounded Update.
    internal static class AuthoredProofScenarioTests
    {
        internal const string Case = "Authored proof scenarios: native metadata and portable timed player workflows";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Process = new Code("transformation");
        private static NewGameRequest Request(string scenario) => new NewGameRequest { Scenario = scenario, Seed = 853 };
        private static EntityView Entity(SimGame game, string prototype)
            => game.Seat(0).Snapshot.Entities.Single(e => e.Def == prototype && e.Owner == new PlayerId(0));
        private static Fix Stock(SimGame game, EntityId id, string resource)
        {
            var facts = new List<Facet>();
            if (!game.Seat(0).View.Inspect(id, facts)) throw new InvalidOperationException("missing owned inventory " + id);
            return facts.Where(f => f.Key == Storage.FacetStock && f.Subject == resource).Aggregate(Fix.Zero, (n, f) => n + f.Value);
        }
        private static bool Completed(SimGame game, EntityId target)
            => game.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:inspect", target, out _);
        private static List<WorkJobView> Jobs(SimGame game)
        {
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs); return jobs;
        }
        private static string Diagnostics(SimGame game)
        {
            var work = game.Seat(0).View.Feature<WorkView>();
            var orders = new List<OrderView>(); if (work != null) work.CopyOrders(orders);
            var jobs = new List<WorkJobView>(); if (work != null) work.CopyJobs(jobs);
            return "mode=" + game.GameMode.ModeId + "/time=" + game.Clock.StepIndex + "/entities=" +
                string.Join(";", game.Seat(0).Snapshot.Entities.Select(e => e.Id + "/" + e.Def + "/" + e.State)) +
                "/orders=" + string.Join(";", orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)) +
                "/jobs=" + string.Join(";", jobs.Select(j => j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.Blocker));
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update();
            if (game.Fault != null) throw new InvalidOperationException(game.Fault + "/" + Diagnostics(game));
            if (twin == null) return;
            twin.Update();
            if (twin.Fault != null || game.Hash() != twin.Hash())
                throw new InvalidOperationException("saved authored sessions differ/" + twin.Fault + "/" + Diagnostics(game));
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1000 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + "/" + Diagnostics(game));
        }
        private static CommandResult Submit(Check c, SimGame game, Command command)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary authored workflow preview");
            c.Equal(before, game.Hash(), "preview does not consume time/materials or mutate modes");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "normal player intent accepted");
            if (game.Fault != null) throw new InvalidOperationException("player command fault " + game.Fault);
            return result;
        }
        private static void Configuration(Check c, GameFactory factory, SimGame game, string scenario, string profile)
        {
            c.Equal(profile, factory.Scenarios().Single().GameMode, "metadata names actual file-authored mode");
            c.Equal(profile, game.GameMode.ModeId, "Initialize uses the same authored identity");
            c.That(ReferenceEquals(game.GameMode, factory.ScenarioGameMode(scenario)) &&
                ReferenceEquals(game.GameMode, factory.PreviewGameMode(Request(scenario))), "one frozen configuration owner");
            c.Equal(0, game.GameMode.Properties.Count, "physical proof has no fabricated business schema");
            c.That(!game.GameMode.TryGet(new Code("business.money.enabled"), out var absent) &&
                absent.Kind == GameModeValueKind.Unavailable, "no money property means unavailable, never an invented false/default");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId) && game.Seat(0).View.Feature<BusinessView>() == null,
                "resource-only authored game has no Tycoon business feature");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "data-only migration does not invent a simulation build change");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "original checkpoint codecs unchanged");
            c.Equal(FrameworkProofContent.Read("test/defs.json"), TestGame.Defs, "canonical test world comes from actual JSON file");
            c.Equal(FrameworkProofContent.Read("extra/defs.json"), TestGame.ExtraDefs, "canonical extra prototype comes from actual JSON file");
            c.Equal(FrameworkProofContent.Read("test-master.json"), TestGame.Manifest, "canonical test master comes from actual JSON file");
            c.Equal(FrameworkProofContent.Read("points.json"), PointGame.Definitions, "canonical point world comes from actual JSON file");
            c.Equal(FrameworkProofContent.Read("probe.json"), CompositionProbe.Definitions, "canonical composition world comes from actual JSON file");
            var master = Json.Parse(FrameworkProofContent.Read("point-master.json"));
            JsonValue owner = null;
            for (int i = 0; i < master.Get("modules").Count; i++)
                if (master.Get("modules")[i].Get("id").Text == "points") owner = master.Get("modules")[i];
            c.That(owner != null && owner.Get("documents").Count == 1 && owner.Get("documents")[0].Text == "points.json",
                "point master owns the actual world document explicitly");

            var tileFactory = TestGame.Factory(); var tile = tileFactory.NewGame(Request("flat"));
            Configuration(c, tileFactory, tile, "flat", "test.resource-only");
            c.Equal(0L, tile.Clock.StepIndex, "authored tile Initialize does not advance clock");
            ulong tileHash = tile.Hash(); var tileTwin = tileFactory.Load(tile.Save());
            for (int i = 0; i < 3; i++) Update(tile, tileTwin);
            c.That(tile.Hash() != tileHash && tile.Clock.StepIndex == 3, "ordinary Update owns tile entity progression");
            var edited = Json.Parse(TestGame.Defs);
            edited.Get("game_modes")[0].Set("id", JsonValue.Of("test.authored"));
            edited.Get("scenarios")[0].Set("game_mode", JsonValue.Of("test.authored"));
            var editedFactory = new GameFactory(TestGame.Definition(), TestGame.Content(edited.ToString()));
            c.Equal("test.authored", editedFactory.PreviewGameMode(Request("flat")).ModeId, "JSON edit changes native engine preview identity");
            c.Equal("test.authored", editedFactory.NewGame(Request("flat")).GameMode.ModeId, "JSON edit changes actual initialized game");
            c.Equal("test.resource-only", tile.GameMode.ModeId, "editing detached authoring never mutates retained session");
            edited.Get("scenarios")[0].Remove("game_mode");
            c.Throws<ContentException>(() => new GameFactory(TestGame.Definition(), TestGame.Content(edited.ToString())).NewGame(Request("flat")),
                "explicit content override still cannot invoke a legacy/inferred mode fallback");

            int compositions = 0; IGameModeConfiguration composed = null;
            var probeFactory = CompositionProbe.Factory(new ProbeModule("probe", new ModuleVersion(1, 0, 0),
                compose: composer => { compositions++; composed = composer.World.GameMode; }));
            c.Equal("probe.resource-only", probeFactory.PreviewGameMode(Request("probe")).ModeId, "actual probe JSON mode preview");
            c.Equal(0, compositions, "file-owned mode preview performs no gameplay composition");
            var probe = probeFactory.NewGame(Request("probe"));
            Configuration(c, probeFactory, probe, "probe", "probe.resource-only");
            c.That(compositions == 1 && ReferenceEquals(composed, probe.GameMode), "one required immutable owner exists before module composition");

            var factory = PointGame.Factory(); var game = factory.NewGame(Request("points"));
            Configuration(c, factory, game, "points", "point.resource-only");
            var idle = factory.NewGame(Request("points")); ulong idleHash = idle.Hash();
            EntityId machine = Entity(game, "machine").Id, source = Entity(game, "source").Id, destination = Entity(game, "destination").Id;
            var retained = game.Seat(0).Snapshot;
            c.Equal(Fix.Zero, ((PointSnapshot)Entity(game, "machine").Spatial).Position, "point JSON authors real initial machine position");
            var result = Submit(c, game, new PostOrderCommand { Plan = "inspect", Target = destination, Priority = 30 });
            int order = checked((int)result.Arg0);
            c.That(!Completed(game, destination), "physical inspection does not complete at posting");
            Reach(game, () => ((PointSnapshot)Entity(game, "machine").Spatial).Position > Fix.Zero && !Completed(game, destination),
                "actual point travel before work");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "saved moving point workflow identity");
            long paused = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause();
            for (int i = 0; i < 3; i++) Update(game, twin);
            c.Equal(paused, game.Clock.StepIndex, "one pause freezes actual point travel and work");
            game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, () => {
                var nodes = new List<ActivityNodeView>();
                return game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, nodes) &&
                    nodes.Any(n => n.ProductiveSteps > 0 && !n.Progress.Equals(WorkProgress.Zero)) && !Completed(game, destination);
            }, "positive point machine work", twin);
            twin = factory.Load(game.Save());
            Reach(game, () => Completed(game, destination), "real timed inspection completion", twin);
            c.Equal(Fix.Zero, ((PointSnapshot)retained.Entities.Single(e => e.Id == machine).Spatial).Position, "retained publication stays immutable after physical Update");
            c.That(game.Clock.StepIndex > 1, "inspection requires real game time");
            c.That(game.Seat(0).View.TryFindResource("supply", out var resource), "player resolves actual authored resource identity");
            Submit(c, game, new AddRouteCommand { From = source, To = destination, Resource = resource,
                Settings = new LogisticsRouteSettings(true, Fix.FromInt(8), 1) });
            Reach(game, () => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero), "actual loaded point shipment");
            c.That(Stock(game, destination, "supply") < Fix.FromInt(20), "loaded stock still needs timed travel/unloading");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "save during real cargo movement");
            Reach(game, () => Stock(game, destination, "supply") == Fix.FromInt(20) && Stock(game, machine, "supply") == Fix.Zero,
                "complete finite physical shipments", twin);
            c.Equal(Fix.Zero, Stock(game, source, "supply"), "finite source actually depleted");
            c.Equal(Fix.FromInt(20), Stock(game, destination, "supply"), "exact native resources delivered once, no duplicates");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed physical point world roundtrip");
            c.Equal(idleHash, idle.Hash(), "shared file definitions never share live session state");

            Mapless(c);
        }
        private static void Mapless(Check c)
        {
            var factory = OptionalProcessTests.Factory(new ProcessProofModule());
            var game = factory.NewGame(Request("process")); Configuration(c, factory, game, "process", "process.resource-only");
            c.That(game.Snapshot.Space == null, "explicit authored game needs no platform/map shape");
            EntityId assembler = Entity(game, "assembler").Id;
            c.Equal(Fix.FromInt(20), Stock(game, assembler, "supply"), "mapless authoring initializes actual materials");
            Submit(c, game, new EnqueueTransformationCommand { Transformer = assembler, Capability = Process, Transformation = "build", Count = 1 });
            c.That(!game.Seat(0).Snapshot.Entities.Any(e => e.Def == "token"), "manufacturing never creates immediate output");
            Update(game);
            c.That(!game.Seat(0).Snapshot.Entities.Any(e => e.Def == "token"), "positive work still needs remaining elapsed duration");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial mapless authored workflow checkpoint");
            Reach(game, () => game.Seat(0).Snapshot.Entities.Count(e => e.Def == "token") == 1, "timed mapless output", twin);
            c.Equal(Fix.FromInt(18), Stock(game, assembler, "supply"), "authored consumed/produced resources conserve native recipe quantities");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "mapless completion preserves mandatory configuration identity");
        }
    }
}
