using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    // Player-only driver. Every quantity, recipe, worker, dependency and duration is engine content.
    internal static class PlanetXAcceptanceTests
    {
        internal const string Case = "Planet X acceptance: finite extraction, timed refinery, manufactured worker and physical deployment";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Run(Check c)
        {
            var factory = PlanetX.Factory();
            SimGame New() => factory.NewGame(new NewGameRequest { Scenario = "planet-x", Seed = 817,
                Seats = { Controller.Human, Controller.Human } });
            var game = New(); var isolated = New(); ulong untouched = isolated.Hash();
            var limits = new ReplayLimits(16 * 1024 * 1024, 12000);
            var recorder = game.RecordReplay(limits);
            IEnumerable<EntityView> Mine(string prototype = null) => game.Seat(0).Snapshot.Entities
                .Where(e => e.Owner == new PlayerId(0) && (prototype == null || e.Def == prototype));
            var mine = Mine("planet.mine").Single().Id; var plant = Mine("planet.refinery").Single().Id;
            var retainedMine = Mine("planet.digger").Single();
            WorkView Work(SimGame session) => session.Seat(0).View.Feature<WorkView>();
            List<OrderView> Orders(SimGame session)
            { var rows = new List<OrderView>(); Work(session).CopyOrders(rows); return rows; }
            List<WorkJobView> Jobs(SimGame session)
            { var rows = new List<WorkJobView>(); Work(session).CopyJobs(rows); return rows; }
            string DemandDiagnostics()
            {
                var rows = new List<LogisticsDemandView>(); var logistics = game.Seat(0).View.Feature<LogisticsView>();
                logistics.CopyDemands(rows);
                return string.Join(";", rows.Select(d => d.Destination + "/" + d.Quantity + "/" + d.Check.Status + "/" +
                    logistics.PreviewHaul(plant, d.Destination, d.Resource, d.Quantity).Status + "/" +
                    logistics.PreviewHaul(plant, d.Destination, d.Resource, d.Quantity).Blocker));
            }
            TransformationSnapshot Production(SimGame session)
            {
                if (!session.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(
                    plant, new Code("transformation"), out var result))
                    throw new InvalidOperationException("missing public refinery facts");
                return result;
            }
            Fix Deposit()
            {
                var values = new List<Fix>();
                var status = game.Seat(0).View.Feature<PointLayerView>().ReadLayer("planet.deposit",
                    new PointRegion(Fix.FromInt(20), Fix.One, 1), values);
                if (status != SpatialLayerReadStatus.Success || values.Count != 1)
                    throw new InvalidOperationException("private deposit unavailable: " + status);
                return values[0];
            }
            Fix Stock(string resource)
            {
                Fix total = Fix.Zero;
                foreach (var entity in Mine())
                {
                    var facets = new List<Facet>();
                    if (!game.Seat(0).View.Inspect(entity.Id, facets)) throw new InvalidOperationException("missing owned inventory facts");
                    foreach (var facet in facets)
                        if (facet.Key == Storage.FacetStock && facet.Subject == resource) total += facet.Value;
                }
                return total;
            }
            string Diagnostics() => "step=" + game.Clock.StepIndex + "/deposit=" + Deposit() +
                "/ore=" + Stock("planet.ore") + "/alloy=" + Stock("planet.alloy") +
                "/process=" + Production(game).Phase + "/" + Production(game).Progress + "/" + Production(game).Blocker +
                "/entities=" + string.Join(";", Mine().Select(e => e.Id + "/" + e.Def + "/" + e.State + "/" + ((PointSnapshot)e.Spatial).Position)) +
                "/demands=" + DemandDiagnostics() +
                "/orders=" + string.Join(";", Orders(game).Select(o => o.Id + "/" + o.Plan + "/" + o.State + "/" + o.Blocker)) +
                "/jobs=" + string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.Blocker));
            bool travel = false, haul = false, work = false, process = false, processSave = false;
            void Observe()
            {
                travel |= Mine().Any(e => e.State == new Code("planet.travel"));
                haul |= Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero);
                foreach (var order in Orders(game))
                {
                    var nodes = new List<ActivityNodeView>();
                    if (Work(game).TryCopyNodes(order.Id, nodes))
                        work |= nodes.Any(n => n.ProductiveSteps > 0 && !n.Progress.Equals(WorkProgress.Zero));
                }
                var production = Production(game);
                process |= production.Progress.WholeTicks > 0 && production.Progress.WholeTicks < production.Required;
                if (process && !processSave)
                {
                    c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "partial native conversion and physical stock save");
                    processSave = true;
                }
            }
            void Update(SimGame twin = null)
            {
                game.Update();
                if (game.Fault != null) throw new InvalidOperationException(game.Fault + "/" + Diagnostics());
                Observe();
                if (twin != null)
                {
                    twin.Update();
                    if (twin.Fault != null || twin.Hash() != game.Hash())
                        throw new InvalidOperationException("saved physical twin diverged/" + Diagnostics());
                }
            }
            void Until(Func<bool> ready, string stage, SimGame twin = null)
            {
                var watchdog = Stopwatch.StartNew(); // Observational timeout, never gameplay time.
                for (int i = 0; i < 2000 && watchdog.Elapsed.TotalSeconds < 30 && !ready(); i++) Update(twin);
                if (!ready()) throw new InvalidOperationException(stage + "/" + Diagnostics());
            }
            CommandResult Submit(Command command)
            {
                ulong before = game.Hash();
                c.Equal(Code.None, game.Seat(0).Preview(command), "normal player preview");
                c.Equal(before, game.Hash(), "preview never grants stock or advances time");
                var result = TestKit.Order(game, 0, command);
                c.That(result.Accepted, "normal command accepted/" + result.Refusal);
                if (game.Fault != null) throw new InvalidOperationException(game.Fault + "/" + Diagnostics());
                Observe(); return result;
            }
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current native protocol unchanged by external content");
            c.That(game.Snapshot.Space is PointWorldSnapshot && !game.Modules.Contains("sim.tile-navigation"),
                "non-tile spatial provider, no platform map dependency");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId) && game.Seat(0).View.Feature<BusinessView>() == null,
                "resource-only game composes no business module");
            c.Equal(4, Mine().Count(), "explicit startup creates only two facilities and two bootstrap machines");
            c.Equal(Fix.FromInt(12), Deposit(), "real company-private finite underground ore");
            c.Equal(Fix.Zero, Stock("planet.ore"), "no opening ore inventory substitute");
            c.Equal(Fix.Zero, Stock("planet.alloy"), "no opening manufactured material substitute");
            var foreign = new List<Fix>();
            c.That(game.Seat(1).View.Feature<PointLayerView>().ReadLayer("planet.deposit",
                new PointRegion(Fix.FromInt(20), Fix.One, 1), foreign) != SpatialLayerReadStatus.Success && foreign.Count == 0,
                "foreign player cannot read actual private ore quantity");
            Submit(new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = plant,
                Transformation = "planet.refine", Count = 6 });
            Submit(new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = plant,
                Transformation = "planet.assemble", Home = plant });
            var build = Submit(new ConstructCommand { Def = "planet.beacon", Priority = 50,
                Placement = Json.Parse("{\"space\":\"planet.space\",\"position\":80}") });
            c.That(Mine("planet.beacon-site").Any(e => e.Id == build.Entity) && !Mine("planet.beacon").Any(),
                "normal construction creates a physical site, never a finished building");
            c.That(!Mine("planet.rover").Any(), "manufactured builder does not spawn on queue admission");
            var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "pending recipes and blocked project persist");
            for (int i = 0; i < 8; i++) Update(twin);
            c.That(Production(game).Progress.Equals(WorkProgress.Zero) && !Production(game).Blocker.IsNone,
                "resource shortage is engine-calculated and prevents process work");
            c.Equal(Fix.FromInt(12), Deposit(), "idle refinery never mines or fabricates ore");
            c.That(!Mine("planet.rover").Any() && !Mine("planet.beacon").Any(), "shortage cannot finish machine or building");
            for (int extraction = 1; extraction <= 6; extraction++)
            {
                Fix opening = Deposit();
                var posted = Submit(new PostOrderCommand { Plan = "planet.extract", Target = mine });
                c.Equal(opening, Deposit(), "posting work cannot immediately remove underground material");
                var nodes = new List<ActivityNodeView>();
                c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyNodes(checked((int)posted.Arg0), nodes),
                    "foreign player cannot inspect extraction activity");
                twin = factory.Load(game.Save());
                c.Equal(game.Hash(), twin.Hash(), "pending extraction physical checkpoint");
                Fix remaining = Fix.FromInt(12 - extraction * 2);
                Until(() => Deposit() == remaining, "finite extraction " + extraction, twin);
                c.Equal(remaining, Deposit(), "one earned extraction debits exactly two physical ore");
            }
            c.That(((PointSnapshot)retainedMine.Spatial).Position == Fix.One && retainedMine.State == new Code("planet.idle"),
                "retained opening entity view stays detached after real movement");
            twin = factory.Load(game.Save());
            long paused = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause();
            for (int i = 0; i < 3; i++) Update(twin);
            c.Equal(paused, game.Clock.StepIndex, "one clock pause stops mining/haul/process/construction");
            game.Clock.Resume(); twin.Clock.Resume();
            Until(() => Mine("planet.rover").Count() == 1 && Production(game).QueueCount == 0,
                "physical refining and single manufactured builder rollout", twin);
            c.That(Mine("planet.rover").Single().Spatial is PointSnapshot, "manufactured worker uses actual non-tile presence");
            twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "manufactured worker and running project save");
            Until(() => Mine("planet.beacon").Count() == 1 && !Mine("planet.beacon-site").Any(),
                "manufactured builder travels and finishes physical deployment", twin);
            c.Equal(Fix.Zero, Deposit(), "underground stock exhausted without a hidden refill");
            c.Equal(Fix.Zero, Stock("planet.ore"), "all twelve ore used by six native recipes");
            c.Equal(Fix.Zero, Stock("planet.alloy"), "six alloy consumed by actual machine and building");
            c.That(travel && haul && work && process, "actual travel, loaded cargo, productive machine work and timed processing observed");
            c.Equal(1, Mine("planet.rover").Count(), "exactly one earned worker output");
            var page = game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0);
            c.Equal(ResourceFlowReadStatus.Ready, page.Status, "complete authored resource-flow window retained");
            var facts = page.Entries.Select(e => e.Item).ToArray();
            Fix Sum(Func<ResourceFlowFact, bool> predicate) => facts.Where(predicate).Aggregate(Fix.Zero, (sum, fact) => sum + fact.Amount);
            c.Equal(Fix.FromInt(12), Sum(f => f.Source.Endpoint.Scope == SpatialResourcesModule.ModuleId && f.Kind == ResourceFlowKind.StockTransfer),
                "deposit debit is native stock transfer provenance, not produced inventory");
            c.Equal(Fix.FromInt(6), Sum(f => f.Kind == ResourceFlowKind.Produced), "six native material outputs, no free resources");
            c.Equal(Fix.FromInt(18), Sum(f => f.Kind == ResourceFlowKind.Consumed), "twelve ore plus six alloy consumed by native owners");
            c.That(!facts.Any(f => f.Kind == ResourceFlowKind.Initialized || f.Kind == ResourceFlowKind.AuthoredCreation || f.Kind == ResourceFlowKind.StockPurchase),
                "no initialization grant, fabricated supply or purchase path");
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Status,
                "resource-flow facts remain company-private");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "finished finite chain save/load");
            c.Equal(untouched, isolated.Hash(), "interleaved untouched session retains exact original state");
            var verified = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(verified.Success && verified.Game != null, "canonical player replay accepted/" + verified.Failure + "/" + verified.Detail);
            if (!verified.Success) throw new InvalidOperationException("Planet X replay refused/" + verified.Detail);
            c.Equal(game.Hash(), verified.ActualHash, "finite extraction/process/deployment replay final hash");
            c.Equal(untouched, isolated.Hash(), "replay verification does not mutate another session");
        }
    }
}

