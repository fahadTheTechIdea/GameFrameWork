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
using static Beep.SimEngine.Tests.TransformationContentContractTests;
using static Beep.SimEngine.Tests.PhysicalLineRemovalTests;

namespace Beep.SimEngine.Tests
{
    // The existing JSON fixture, ordinary commands and public immutable facts only.
    internal static class InfrastructureServiceContractTests
    {
        internal const string Case = "Infrastructure contracts: one owner earns physical corridor work and private flow facts";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual contract build");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current authored protocol retains original infrastructure codec");
            c.Equal(Physical(c, false), Physical(c, true), "native/direct effects use the same contract-owned chronology");
            foreach (string operation in new[] { "segment", "finish" })
            {
                var initialize = new InitializeProbe();
                var module = new MissingLineEffectsProofModule(initialize);
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(ConstructionPlanTests.Points(
                    extraModules: new[] { module },
                    extraDeclarations: "[{\"id\":\"proof.missing-line-effects\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"missing-line-effects.json\"]}]",
                    extraDocuments: new Dictionary<string, string> { { "missing-line-effects.json", "{\"independent_line_effects\":[{\"id\":\"unused.line-effect\",\"operation\":\"" + operation + "\"}]}" } },
                    scenarioEdit: scenario => scenario.Get("modules").Add(JsonValue.Of(module.Id)))),
                    "unused " + operation + " requires the actual effects contract");
                c.Equal(0, initialize.Initializations, "missing capability refuses before Initialize");
            }
        }
        private static long Physical(Check c, bool direct)
        {
            var aliases = new InfrastructureServiceContractsProofModule();
            var factory = Create(direct, additionalModule: aliases, editNetwork: root => {
                // Explicit three-tile band earns eight units while retaining split capacity.
                var band = root.Get("entities")[0].Get("components").Get("signal_network_gate").Get("conditions")[0];
                band.Set("minimum", JsonValue.Number("26")); band.Set("maximum", JsonValue.Number("30"));
            });
            var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            c.That(aliases.OneOwner && !aliases.ConcreteBound, "five service contracts name one owner without concrete registration");
            c.Equal(1, aliases.PersistentOwners, "aliased service codec registered once");
            c.Equal(1, aliases.EndpointOwners, "aliased endpoint provider discovered once");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); var recorder = game.RecordReplay(limits);
            Step(game);
            var view = game.Seat(0).View.Feature<InfrastructureView>();
            var flows = new List<NetworkFlowSnapshot>();
            Reach(game, null, () => view.CopyFlow("binding.main", flows).IsNone && flows.Count > 0, "actual startup flow cursor");
            ulong hash = game.Hash();
            c.Equal(Code.None, view.CopyFlow("binding.main", flows), "contract flow facts admitted");
            c.That(flows.Count > 0, "actual elapsed transfer cursors observed");
            var retained = flows.ToArray();
            long retainedAt = retained[0].LastTick, retainedElapsed = retained[0].ElapsedTicks;
            c.Equal(Code.None, game.Seat(1).View.Feature<InfrastructureView>().CopyFlow("binding.main", flows), "foreign flow query has no quantity leakage");
            c.Equal(0, flows.Count, "foreign company sees no private cursors");
            c.Equal(Codes.UnknownDef, view.CopyFlow("missing.network", flows), "unknown content typed refusal");
            c.Equal(0, flows.Count, "refused diagnostics leaves no old facts");
            var path = new List<TileCoord>();
            c.Equal(PathSearchResult.Found, view.SuggestLinePath("network.pipe", new TileCoord(11,28), new TileCoord(11,29), path), "path view selects construction contract");
            c.That(path.First() == new TileCoord(11,28) && path.Last() == new TileCoord(11,29), "actual endpoint path facts");
            c.Equal(PathSearchResult.Found, view.SuggestLinkPath("network.pipe", Entity(game,"binding.source"), Entity(game,"binding.watcher"), path), "link path remains engine calculated");
            c.Equal(hash, game.Hash(), "healthy path and flow reads pure");
            var flowTwin = factory.Load(game.Save());
            c.Equal(game.Hash(), flowTwin.Hash(), "elapsed flow codec restores through diagnostic aliases");
            c.Equal(Code.None, flowTwin.Seat(0).View.Feature<InfrastructureView>().CopyFlow("binding.main", flows), "saved flow contract query");
            c.Equal(retained.Length, flows.Count, "actual saved flow cursor count");
            c.Equal(retainedElapsed, flows[0].ElapsedTicks, "unfinished elapsed interval preserved");
            Step(game, flowTwin);
            Order(c, game, Cut(game)); int cut = Orders(game).Single().Id;
            Reach(game, null, () => Nodes(game,cut).Single().Progress.WholeTicks > 0, "contract-owned physical cut");
            c.Equal(1, Tile(game,11), "uncompleted machine work cannot cut instantly");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "original work/topology saved through aliases");
            game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); long at = game.Clock.StepIndex; Step(game,twin);
            c.That(hash == game.Hash() && at == game.Clock.StepIndex, "one clock freezes contract-owned work");
            game.Clock.Resume(); twin.Clock.Resume();
            Reach(game,twin,() => Tile(game,11) == 0,"earned topology-contract cut");
            c.Equal(2, Pools(game).Count, "same pool owner splits actual geometry");
            game.Seat(0).View.TryFindResource("steel",out var connectedSteel);
            c.Equal(Fix.FromInt(8),Pools(game).SelectMany(p=>p.Stock).Where(p=>p.Resource==connectedSteel).Aggregate(Fix.Zero,(s,p)=>s+p.Amount),
                "actual earned network stock survives split");
            c.Equal(Fix.FromInt(30), Steel(game), "split conserves actual original stock");
            var lay = new LayLineCommand { Line = "network.pipe" }; lay.Tiles.Add(new TileCoord(11,28));
            Order(c,game,lay);
            var marker = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "sim.line-work.network.pipe").Id;
            int id = Orders(game).Single(o => o.Target == marker).Id;
            c.Equal(0,Tile(game,11),"construction contract posts work, not completed geometry");
            c.Equal(WorkEffectCodes.NotComplete,Work(game).PreviewOrderEffect(id),"whole-order finish cannot be applied early");
            game.Seat(0).View.TryFindResource("steel",out var steel);
            Order(c,game,new HaulResourcesCommand { Source=Entity(game,"depot"),Destination=marker,
                Worker=Entity(game,"truck"),Resource=steel,Ceiling=Fix.One,Priority=100 });
            Reach(game,null,() => Nodes(game,id).Any(n => n.Progress.WholeTicks > 0),"hauled steel earns segment work");
            c.Equal(0,Tile(game,11),"partial segment retains cut");
            twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"bound execution identity restores through construction contract");
            Reach(game,twin,() => Tile(game,11)==1 && game.Seat(0).Snapshot.Entities.All(e=>e.Id!=marker),"earned segment and final cursor cleanup");
            c.Equal(1,Pools(game).Count,"same topology rejoins after physical work");
            c.Equal(Fix.FromInt(30),Steel(game),"joining cannot grant or lose connected stock");
            var depot=new List<Facet>();game.Seat(0).View.Inspect(Entity(game,"depot"),depot);
            c.Equal(Fix.FromInt(49),depot.Where(f=>f.Key==Storage.FacetStock&&f.Subject=="steel").Aggregate(Fix.Zero,(s,f)=>s+f.Value),"one physical material consumed");
            c.That(retained[0].LastTick==retainedAt && retained[0].ElapsedTicks==retainedElapsed && retainedAt<game.Clock.StepIndex,
                "retained elapsed facts remain detached from later physical work");
            c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"completed aliased codecs restore once");
            c.Equal(isolatedHash,isolated.Hash(),"separate session owner remains isolated");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"ordinary commands replay through unchanged loop");
            c.That(aliases.OneOwner && !aliases.ConcreteBound && aliases.PersistentOwners==1 && aliases.EndpointOwners==1 && aliases.Compositions>2,
                "load/replay compositions preserve sole service owner");
            return game.Clock.StepIndex;
        }
    }
}
