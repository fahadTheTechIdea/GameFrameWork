using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    public static class WorkPhaseExecutorTests
    {
        public const string Case = "Work phase executors: stable composition, physical work and saved identity";
        public static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(PhaseIdentityProbeModule probe = null)
        {
            var modules = new List<GameModule> { new ContentAccessModule(), new InventoryModule(), new MotionModule(),
                new TileNavigationModule(), new WorkModule(), new CompanyKnowledgeModule(), new WorldSetupModule() };
            var master = Content.Json.Parse(FrameworkProofContent.Read("native-survey-master.json"));
            var world = Content.Json.Parse(FrameworkProofContent.Read("native-survey-world.json"));
            if (probe != null)
            {
                modules.Add(probe);
                master.Get("modules").Add(Content.Json.Parse("{\"id\":\"proof.phase-identity\",\"version\":\"1.0.0\",\"documents\":[]}"));
                world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(probe.Id));
            }
            var definition = new GameDefinition("native-phase", modules);
            var files = new PreloadedFileSource().Add("manifest.json", master.ToString()).Add("native-survey-world.json", world.ToString())
                .Add("native-survey-startup.json", FrameworkProofContent.Read("native-survey-startup.json"))
                .Add("native-survey-navigation.json", FrameworkProofContent.Read("native-survey-navigation.json"))
                .Add("native-survey-work.json", FrameworkProofContent.Read("native-survey-work.json"))
                .Add("native-survey-knowledge.json", FrameworkProofContent.Read("native-survey-knowledge.json"))
                .Add("open-access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "native-survey", Seed = 743 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Def.Id == id);
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("phase time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " jobs=" + string.Join(";",
                Board(game).Jobs.Select(j => j.Id + "/" + j.State + "/" + j.ToilIndex + "/" + j.Blocker)));
        }
        private static Order Start(Check c, SimGame game)
        {
            var command = new PostOrderCommand { Plan = "survey.sample", Target = Find(game, "survey.site").Id, Priority = 60 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary authored intent preview");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary player activity accepted");
            return Board(game).Order((int)result.Arg0);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "contract phase identity build");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "stable string phase payload, no enum reader");
            var factory = Factory(); var game = New(factory); var board = Board(game);
            var catalog = game.World.Services.Get<IWorkPhaseCatalog>();
            ulong before = game.Hash();
            c.That(ReferenceEquals(catalog, game.World.Services.Get<IWorkerDispatch>()), "catalog aliases actual WorkSystem");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, board.StateSchemaVersion, "same board owns new phase payload");
            c.Equal(5, catalog.Ids.Count, "five native contract providers");
            c.Equal(string.Join("|", catalog.Ids.OrderBy(id => id.Id, StringComparer.Ordinal)), string.Join("|", catalog.Ids), "stable ordinal phase identity order");
            foreach (Code phase in new[] { WorkPhases.Travel, WorkPhases.Productive, WorkPhases.Pickup, WorkPhases.Delivery, WorkPhases.Departure })
                c.That(catalog.Contains(phase), "native phase is explicitly composed " + phase);
            c.That(!catalog.Contains(Code.None) && !catalog.Contains(new Code("missing.phase")), "no unknown phase fallback");
            c.Throws<NotSupportedException>(() => ((IList<Code>)catalog.Ids).Clear(), "phase catalog is immutable");
            c.That(!game.World.Services.Has<IEconomy>(), "physical phases need no business module");
            c.Equal(before, game.Hash(), "derived phase catalog reads do not alter authoritative state");
            ResourceId steel = game.World.Content.ResourceId("steel"); Entity site = Find(game, "survey.site"), worker = Find(game, "survey.machine");
            Order order = Start(c, game); ActivityNodeExecution node = order.Node("sample");
            Reach(game, () => board.Job(node.JobId)?.State == JobState.Assigned, "actual travel assignment");
            Job job = board.Job(node.JobId);
            c.Equal(WorkPhases.Travel, job.Toils[job.ToilIndex].Executor, "actual journey uses stable phase identity");
            c.Equal(0L, node.Progress.WholeTicks, "travel grants no productive work");
            c.Equal(Fix.FromInt(20), site.Get<IInventory>().Amount(steel), "travel consumes no startup input");
            c.That(worker.Get<ITilePresence>().Tile.X < 15, "worker actually begins away from site");
            using (var stream = new MemoryStream())
            {
                board.Sync(new StateWriter(stream)); byte[] raw = stream.ToArray(), needle = Encoding.UTF8.GetBytes(WorkPhases.Travel.Id);
                int offset = -1;
                for (int i = 0; i <= raw.Length - needle.Length && offset < 0; i++)
                    if (raw.Skip(i).Take(needle.Length).SequenceEqual(needle)) offset = i;
                c.That(offset >= 0, "actual sole-owner payload contains stable phase identity");
                if (offset < 0) throw new InvalidOperationException("no saved travel identity");
                raw[offset] = (byte)'x';
                try { new WorkBoard(game.World, board.RetryPolicy).Sync(new StateReader(raw, 0, raw.Length)); c.That(false, "unknown saved executor refused"); }
                catch (CorruptStateException error) { c.That(error.Message.Contains("unknown physical work phase executor"), "sole codec refuses missing phase contract, not legacy translation"); }
            }
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "save during actual travel");
            Reach(game, () => node.Progress.WholeTicks > 0, "real arrival and productive phase", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved travel progresses identically");
            c.Equal(WorkPhases.Productive, board.Job(node.JobId).Toils[board.Job(node.JobId).ToilIndex].Executor, "same job enters productive contract");
            c.Equal(Fix.FromInt(12), site.Get<IInventory>().Amount(steel), "actual native material transaction consumes eight once");
            c.That(node.Progress.WholeTicks < game.World.Time.Ticks(order.Plan.Graph.Find(node.Id).Duration), "positive duration not instant completion");
            var partial = factory.Load(game.Save()); c.Equal(game.Hash(), partial.Hash(), "save during actual partial work");
            Reach(game, () => order.State == OrderState.Done, "earned completion", partial);
            c.Equal(game.Hash(), partial.Hash(), "partial productive work resumes identically");
            c.Equal(Fix.FromInt(12), site.Get<IInventory>().Amount(steel), "completion never recharges startup materials");
            Order cancelled = Start(c, game); Reach(game, () => cancelled.Node("sample").Progress.WholeTicks > 0, "second ordinary physical execution");
            c.Equal(Fix.FromInt(4), site.Get<IInventory>().Amount(steel), "repeat owns its own actual inputs");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = cancelled.Id }).Accepted, "normal cancellation accepted");
            c.Equal(Fix.FromInt(4), site.Get<IInventory>().Amount(steel), "cancellation fabricates no refund");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled actual jobs checkpoint canonically");
            c.Equal(Fix.FromInt(20), Find(New(factory), "survey.site").Get<IInventory>().Amount(steel), "other session stock is isolated");
            var extra = New(Factory(new PhaseIdentityProbeModule(new Code("proof.phase.unused"))));
            c.Equal(6, extra.World.Services.Get<IWorkPhaseCatalog>().Ids.Count, "new stable contract composes without enum edit and aliases once");
            c.That(extra.World.Services.Get<IWorkPhaseCatalog>().Contains(new Code("proof.phase.unused")), "declared extension identity discovered");
            c.Throws<ContentException>(() => New(Factory(new PhaseIdentityProbeModule(WorkPhases.Productive))), "duplicate phase identity refuses before Initialize");
            c.Throws<ContentException>(() => New(Factory(new PhaseIdentityProbeModule(Code.None))), "empty phase identity refuses before Initialize");
        }
    }
}
