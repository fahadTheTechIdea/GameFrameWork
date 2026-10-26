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
    // Normal player intents/observations only: no World/live provider access or state repair.
    internal static partial class GenericColonyCompositionTests
    {
        internal const string Case = "Generic colony: resource-only staged work, sequential manufacturing and recovered supply";
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add("Generic colony acceptance: authored pressure, physical industry and objective recovery", EmergencyChain);
        }
        private static readonly Code Process = new Code("transformation");
        private static IEnumerable<EntityView> Mine(SimGame game, string prototype = null)
            => game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0) && (prototype == null || e.Def == prototype));
        private static EntityView One(SimGame game, string prototype) => Mine(game, prototype).Single();
        private static int Trucks(SimGame game) => Mine(game, "truck").Count();
        private static TransformationSnapshot Production(SimGame game, EntityId factory)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(factory, Process, out var snapshot))
                throw new InvalidOperationException("missing public production facts");
            return snapshot;
        }
        private static Fix Stock(SimGame game, EntityId id, int seat = 0)
        {
            var facets = new List<Facet>();
            if (!game.Seat(seat).View.Inspect(id, facets)) throw new InvalidOperationException("missing visible inventory facts " + id);
            Fix sum = Fix.Zero;
            foreach (var fact in facets) if (fact.Key == Storage.FacetStock && fact.Subject == "steel") sum += fact.Value;
            return sum;
        }
        private static Fix Steel(SimGame game) => Mine(game).Aggregate(Fix.Zero, (sum, entity) => sum + Stock(game, entity.Id));
        private static string Diagnostics(SimGame game, EntityId factory)
        {
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
            var production = Production(game, factory);
            return "clock=" + game.Clock.StepIndex + "/production=" + production.Phase + "/" + production.Progress +
                "/" + production.Blocker + "/queue=" + production.QueueCount + "/entities=" +
                string.Join(";", Mine(game).Select(e => e.Id + "/" + e.Def + "/" + e.State)) + "/orders=" +
                string.Join(";", orders.Select(o => o.Id + "/" + o.Plan + "/" + o.State + "/" + o.Blocker)) + "/jobs=" +
                string.Join(";", jobs.Select(j => j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.Blocker));
        }
        private sealed class Observations
        {
            internal bool Travel, LoadedHaul, Work;
            internal void Read(SimGame game)
            {
                Travel |= Mine(game).Any(e => e.State == new Code("state.moving"));
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                LoadedHaul |= jobs.Any(j => j.IsHaul && j.Loaded > Fix.Zero);
                var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
                foreach (var order in orders)
                {
                    var nodes = new List<ActivityNodeView>();
                    if (game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, nodes))
                        Work |= nodes.Any(n => n.ProductiveSteps > 0 && !n.Progress.Equals(WorkProgress.Zero));
                }
            }
        }
        private static void Update(SimGame game, EntityId factory, Observations observations, SimGame twin = null)
        {
            game.Update();
            if (game.Fault != null) throw new InvalidOperationException(game.Fault + "/" + Diagnostics(game, factory));
            observations.Read(game);
            if (twin != null)
            {
                twin.Update(); if (twin.Fault != null) throw new InvalidOperationException("saved twin fault " + twin.Fault);
                if (game.Hash() != twin.Hash()) throw new InvalidOperationException("saved physical sessions diverged/" + Diagnostics(game, factory));
            }
        }
        private static void Reach(SimGame game, EntityId factory, Observations observations, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !ready(); i++) Update(game, factory, observations, twin);
            if (!ready()) throw new InvalidOperationException(stage + "/" + Diagnostics(game, factory));
        }
        private static CommandResult Submit(Check c, SimGame game, Command command)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal player preview");
            c.Equal(before, game.Hash(), "preview does not supply materials or advance time");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "normal player command");
            if (game.Fault != null) throw new InvalidOperationException("command fault " + game.Fault);
            return result;
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 811 });
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "direct generic composition identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no new money/physical save layout");
            var factory = Colony.Factory(); var game = New(factory); var observations = new Observations();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId) && game.Seat(0).View.Feature<BusinessView>() == null,
                "generic colony selects no Tycoon or business feature");
            c.That(!game.Snapshot.Entities.Any(e => e.Def == "market"), "no priced vendor startup or substitute free market");
            c.Equal(10, game.Snapshot.Entities.Count, "exact physical startup: two companies, no extra economic entity");
            var depot = One(game, "depot").Id; var plant = One(game, "factory").Id;
            var crane = One(game, "crane").Id;
            c.Equal(Fix.FromInt(50), Steel(game), "real authored opening materials");
            var otherDepot = game.Seat(1).Snapshot.Entities.Single(e => e.Owner == new PlayerId(1) && e.Def == "depot").Id;
            c.Equal(Fix.FromInt(50), Stock(game, otherDepot, 1), "other company owns independent authored materials");
            c.Equal(Code.None, game.Seat(0).View.Feature<TransformationManagementView>().ReadMonetaryCost(plant, Process, "build_truck", out var decision),
                "same engine cost diagnostics remain optional");
            c.That(decision.AuthoredAmount == Fix.Zero && decision.Applicability == MonetaryCostApplicability.NotAuthored && decision.EffectiveAmount == null,
                "native recipe does not acquire an invented price or account obligation");
            var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            Submit(c, game, new EnqueueTransformationCommand { Capability = Process, Transformer = plant, Transformation = "build_truck", Count = 2, Home = depot });
            c.Equal(2, Trucks(game), "two machine outputs cannot appear immediately");
            var build = Submit(c, game, new ConstructCommand { Def = "tower", Priority = 50, Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)) });
            c.That(Mine(game, "site:tower").Any(e => e.Id == build.Entity) && !Mine(game, "tower").Any(), "build command creates physical project, not finished tower");
            c.Equal(Codes.NotOwned, game.Seat(1).Preview(new CancelConstructionCommand { Site = build.Entity }), "foreign player cannot cancel another company's project");
            Reach(game, plant, observations, () => Production(game, plant).Progress.WholeTicks > 0 && Trucks(game) == 2, "first partial timed machine");
            var partial = Production(game, plant);
            c.That(partial.Progress.WholeTicks < partial.Required && partial.QueueCount == 2, "sequential first batch still has two authored intents");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "save during actual haul/work/manufacturing");
            Reach(game, plant, observations, () => Trucks(game) == 3 && Production(game, plant).QueueCount == 1, "first machine completes alone", twin);
            c.That(Production(game, plant).Progress.WholeTicks < Production(game, plant).Required, "second batch has independent elapsed work");
            twin = factory.Load(game.Save());
            Reach(game, plant, observations, () => Trucks(game) == 4 && Production(game, plant).QueueCount == 0 && Mine(game, "tower").Count() == 1,
                "two physical batches and completed staged tower", twin);
            c.Equal(Fix.Zero, Steel(game), "50 opening steel consumed by two real machines and tower");
            c.That(observations.Travel && observations.LoadedHaul && observations.Work, "normal engine moved loaded trucks and executed positive machine work");
            c.Equal(Fix.FromInt(50), Stock(game, otherDepot, 1), "completion never steals another company's stock");
            c.Equal(isolatedHash, isolated.Hash(), "interleaved idle session remains independent");
            var tower = One(game, "tower").Id;
            c.Equal(Codes.NotOwned, game.Seat(1).Preview(new DemolishCommand { Target = tower }), "foreign demolition refuses through normal command");
            Submit(c, game, new EnqueueTransformationCommand { Capability = Process, Transformer = plant, Transformation = "build_truck", Home = depot });
            twin = factory.Load(game.Save());
            for (int i = 0; i < 20; i++) Update(game, plant, observations, twin);
            var waiting = Production(game, plant);
            c.That(waiting.QueueCount == 1 && waiting.Progress.Equals(WorkProgress.Zero) && !waiting.Blocker.IsNone,
                "missing physical resources retain typed waiting intent, no instant fallback");
            c.Equal(4, Trucks(game), "shortage cannot create third manufactured machine");
            c.Equal(Fix.Zero, Steel(game), "shortage cannot fabricate supplier stock");
            var shortageSave = game.Save(); twin = factory.Load(shortageSave); c.Equal(game.Hash(), twin.Hash(), "shortage state roundtrip");
            long pausedAt = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause();
            for (int i = 0; i < 3; i++) Update(game, plant, observations, twin);
            c.Equal(pausedAt, game.Clock.StepIndex, "sole clock pause stops production and recovery");
            game.Clock.Resume(); twin.Clock.Resume();
            Submit(c, game, new DemolishCommand { Target = tower });
            c.That(Mine(game, "tower").Any(e => e.Id == tower), "demolition is physical work, not immediate salvage");
            twin = factory.Load(game.Save());
            Reach(game, plant, observations, () => !Mine(game, "tower").Any(), "physical demolition and real salvage", twin);
            c.That(Trucks(game) == 4, "recovered stock still waits for actual delivery and manufacturing");
            twin = factory.Load(game.Save());
            Reach(game, plant, observations, () => Trucks(game) == 5 && Production(game, plant).QueueCount == 0,
                "recovered resource hauling and final timed machine", twin);
            c.Equal(Fix.FromInt(5), Steel(game), "explicit15 salvage minus10 final recipe, no monetary stock grant");
            c.Equal(Fix.FromInt(50), Stock(game, otherDepot, 1), "recovery retains company ownership");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "final physical roundtrip");
            c.That(game.Clock.StepIndex > 500 && game.Fault == null && twin.Fault == null, "positive real game time, healthy deterministic continuations");
            c.Equal(isolatedHash, isolated.Hash(), "recovery cannot mutate independent session");
            var bad = Json.Parse(FrameworkProofContent.Read("colony-work.json"));
            bad.Set("work_cost_requirements", Json.Parse("[{\"id\":\"forbidden.cash\",\"amount\":1,\"credit\":false,\"category\":\"construction\"}]"));
            c.Throws<ContentException>(() => New(Colony.Factory(moduleDocuments: new Dictionary<string, string> { ["colony-work.json"] = bad.ToString() })),
                "native monetary authoring still requires declared Tycoon ownership, never free fallback");
        }
    }
}

