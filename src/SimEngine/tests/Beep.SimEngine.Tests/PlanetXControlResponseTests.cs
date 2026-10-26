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
    // Only player input, public observations and bounded waits. Engine modules own every game rule.
    internal static class PlanetXControlResponseTests
    {
        internal const string Case = "Planet X control acceptance: authored research, deferred safety control and physical incident recovery";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Run(Check c)
        {
            var factory = PlanetX.Factory(FrameworkProofContent.Read("planet-x-control-master.json"),
                new GameModule[] { new CompanyKnowledgeModule(), new TechnologyModule(), new AutomationModule(),
                    new IncidentResponseModule(), new ScenarioPressureModule(), new PlanetXControlModule() },
                new Dictionary<string, string> { ["planet-x-control.json"] = FrameworkProofContent.Read("planet-x-control.json") });
            SimGame New() => factory.NewGame(new NewGameRequest { Scenario = "planet-x-control", Seed = 829,
                Seats = { Controller.Human, Controller.Human } });
            var game = New(); var isolated = New(); ulong isolatedHash = isolated.Hash();
            var limits = new ReplayLimits(16 * 1024 * 1024, 12000); var recorder = game.RecordReplay(limits);
            IEnumerable<EntityView> Mine(string prototype = null) => game.Seat(0).Snapshot.Entities
                .Where(e => e.Owner == new PlayerId(0) && (prototype == null || e.Def == prototype));
            var mine = Mine("planet.mine").Single().Id; var plant = Mine("planet.refinery").Single().Id;
            var lab = Mine("planet.lab").Single().Id; var watch = Mine("planet.watch-station").Single().Id;
            var retainedDigger = Mine("planet.digger").Single();
            var carrier = Mine("planet.carrier").Single().Id;
            OperationsView Operations(SimGame session) => session.Seat(0).View.Feature<OperationsView>();
            WorkView Work(SimGame session) => session.Seat(0).View.Feature<WorkView>();
            IncidentsView Incidents(SimGame session) => session.Seat(0).View.Feature<IncidentsView>();
            List<OrderView> Orders()
            { var rows = new List<OrderView>(); Work(game).CopyOrders(rows); return rows; }
            List<WorkJobView> Jobs()
            { var rows = new List<WorkJobView>(); Work(game).CopyJobs(rows); return rows; }
            List<ActivityNodeView> Nodes(int order)
            {
                var rows = new List<ActivityNodeView>();
                if (!Work(game).TryCopyNodes(order, rows)) throw new InvalidOperationException("missing public order " + order);
                return rows;
            }
            TechnologySnapshot Technology(int seat = 0)
            {
                var rows = new List<TechnologySnapshot>(); game.Seat(seat).View.Feature<TechnologyView>().CopyTechnologies(rows);
                return rows.Single(t => t.Technology == "planet.safety");
            }
            IncidentSnapshot Incident()
            {
                if (!Incidents(game).TryGetIncident(watch, new Code("planet.fault"), out var result))
                    throw new InvalidOperationException("missing actual incident");
                return result;
            }
            IncidentLifecycleView Lifecycle()
            {
                if (!Incidents(game).TryGetLifecycle(watch, new Code("planet.fault"), out var result))
                    throw new InvalidOperationException("missing actual lifecycle");
                return result;
            }
            ScenarioPressureSnapshot Pressure()
            {
                var rows = new List<ScenarioPressureSnapshot>();
                game.Seat(0).View.Feature<ScenarioPressureView>().CopyPressure(rows); return rows.Single();
            }
            AutomationRuleSnapshot Rule(SimGame session = null)
            {
                if (!Operations(session ?? game).TryGetAutomationRule(new PlayerId(0), "safety", out var result))
                    throw new InvalidOperationException("missing configured rule");
                return result;
            }
            bool Enabled(SimGame session = null)
            {
                if (!Operations(session ?? game).TryGetControl(plant, new Code("transformation"), out var result))
                    throw new InvalidOperationException("missing native operation"); return result.Enabled;
            }
            AutomationSignalSnapshot Safety()
            {
                Code refusal = Operations(game).ReadSignal(watch, new Code("safety"), "planet.safety-ready", out var result);
                if (!refusal.IsNone) throw new InvalidOperationException("signal read refused " + refusal); return result;
            }
            TransformationSnapshot Production()
            {
                if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(
                    plant, new Code("transformation"), out var result)) throw new InvalidOperationException("missing production facts");
                return result;
            }
            Fix Deposit()
            {
                var rows = new List<Fix>(); var status = game.Seat(0).View.Feature<PointLayerView>().ReadLayer("planet.deposit",
                    new PointRegion(Fix.FromInt(20), Fix.One, 1), rows);
                if (status != SpatialLayerReadStatus.Success || rows.Count != 1) throw new InvalidOperationException("missing deposit " + status);
                return rows[0];
            }
            Fix Stock(string resource, EntityId? endpoint = null)
            {
                Fix total = Fix.Zero;
                foreach (var entity in Mine().Where(e => !endpoint.HasValue || e.Id == endpoint.Value))
                {
                    var facets = new List<Facet>();
                    if (!game.Seat(0).View.Inspect(entity.Id, facets)) throw new InvalidOperationException("missing private inventory");
                    foreach (var facet in facets) if (facet.Key == Storage.FacetStock && facet.Subject == resource) total += facet.Value;
                }
                return total;
            }
            string Diagnostics() => "step=" + game.Clock.StepIndex + "/deposit=" + Deposit() +
                "/stock=" + Stock("planet.ore") + "/" + Stock("planet.alloy") +
                "/technology=" + Technology().Known + "/incident=" + Incident().Active + "/" + Lifecycle().Blocker +
                "/process=" + Production().Phase + "/" + Production().Progress + "/" + Production().Blocker +
                "/orders=" + string.Join(";", Orders().Select(o => o.Id + "/" + o.Plan + "/" + o.State + "/" + o.Blocker)) +
                "/jobs=" + string.Join(";", Jobs().Select(j => j.Id + "/" + j.State + "/" + j.Blocker)) +
                "/actors=" + string.Join(";", Mine().Where(e => e.Spatial is PointSnapshot).Select(e => e.Def + "/" + ((PointSnapshot)e.Spatial).Position + "/" + e.State));
            bool AtHome(EntityId worker) => Work(game).ReadHome(worker, out var home).IsNone && home.State == WorkerHomeState.AtHome;
            bool researchPartial = false, repairPartial = false, travelled = false, hauled = false, processed = false;
            bool extractorBay = false, oreBay = false, builderBay = false, alloyBay = false;
            bool carrierStayedParked = true;
            void Observe()
            {
                var actors = Mine().Where(e => e.Def == "planet.digger" || e.Def == "planet.carrier" || e.Def == "planet.rover").ToArray();
                Fix Position(string prototype) => ((PointSnapshot)actors.Single(e => e.Def == prototype).Spatial).Position;
                Fix left = Position("planet.digger"), right = Position("planet.carrier");
                bool ordered = left < right;
                if (actors.Any(e => e.Def == "planet.rover")) {
                    Fix middle = Position("planet.rover"); ordered &= left < middle && middle < right;
                }
                if (!ordered) throw new InvalidOperationException("machines overlapped/crossed/" + Diagnostics());
                if (!Technology().Known || Incident().Active) carrierStayedParked &= right == Fix.FromInt(100);
                var jobs = Jobs();
                extractorBay |= jobs.Any(j => !j.IsHaul && j.Target == mine && j.DestinationDockSlot == new Code("extract"));
                oreBay |= jobs.Any(j => j.IsHaul && j.Source == mine && j.SourceDockSlot == new Code("load") && j.Loaded > Fix.Zero);
                builderBay |= jobs.Any(j => !j.IsHaul && j.DestinationDockSlot == new Code("build"));
                alloyBay |= jobs.Any(j => j.IsHaul && j.DestinationDockSlot == new Code("unload") && j.Loaded > Fix.Zero);
                travelled |= Mine().Any(e => e.State == new Code("planet.travel"));
                hauled |= jobs.Any(j => j.IsHaul && j.Loaded > Fix.Zero);
                processed |= Production().Progress.WholeTicks > 0 && Production().Progress.WholeTicks < Production().Required;
                foreach (var order in Orders())
                {
                    var nodes = Nodes(order.Id);
                    bool partial = nodes.Any(n => n.ProductiveSteps > 0 && n.Progress.WholeTicks > 0 && n.Progress.WholeTicks < n.RequiredTicks);
                    if (order.Plan == "activity:planet.safety" && partial && !researchPartial)
                    { c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "partial physical research persists"); researchPartial = true; }
                    if (order.Plan == "activity:planet.control-response" &&
                        nodes.Any(n => n.Id == "repair" && n.ProductiveSteps > 0 && n.Progress.WholeTicks < n.RequiredTicks) && !repairPartial)
                    { c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "partial physical response persists"); repairPartial = true; }
                }
            }
            void Update(SimGame twin = null)
            {
                game.Update(); if (game.Fault != null) throw new InvalidOperationException(game.Fault + "/" + Diagnostics());
                Observe();
                if (twin != null)
                {
                    twin.Update();
                    if (twin.Fault != null || game.Hash() != twin.Hash()) throw new InvalidOperationException("saved twin diverged/" + Diagnostics());
                }
            }
            void Until(Func<bool> ready, string stage, SimGame twin = null)
            {
                var watchdog = Stopwatch.StartNew(); // Diagnostics only; not gameplay time.
                for (int i = 0; i < 2000 && watchdog.Elapsed.TotalSeconds < 30 && !ready(); i++) Update(twin);
                if (!ready()) throw new InvalidOperationException(stage + "/" + Diagnostics());
            }
            CommandResult Submit(Command command, Code expected = default)
            {
                ulong before = game.Hash(); c.Equal(expected, game.Seat(0).Preview(command), "normal preview parity");
                c.Equal(before, game.Hash(), "preview changes no state");
                var result = TestKit.Order(game, 0, command);
                c.Equal(expected, result.Refusal, "normal command matches admission");
                if (expected.IsNone) c.That(result.Accepted, "normal player command accepted");
                if (game.Fault != null) throw new InvalidOperationException(game.Fault + "/" + Diagnostics());
                Observe(); return result;
            }

            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "external game content changes no native build protocol");
            c.That(game.Snapshot.Space is PointWorldSnapshot && !game.Modules.Contains("sim.tile-navigation"), "non-tile game");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId) && game.Seat(0).View.Feature<BusinessView>() == null, "no business dependency");
            c.Equal(7, Mine().Count(), "four original entities, two reserve stations and one empty parking endpoint");
            c.Equal(Fix.FromInt(12), Deposit(), "original finite underground ore unchanged");
            c.Equal(Fix.FromInt(2), Stock("planet.alloy"), "exactly two disclosed starting research/repair reserves");
            c.Equal(Fix.One, Stock("planet.alloy", lab), "one authored research input");
            c.Equal(Fix.One, Stock("planet.alloy", watch), "one authored repair input");
            c.That(!Technology().Known && !Technology(1).Known, "each company starts with its own unlearned knowledge");
            var openingSafety = Safety();
            c.That(openingSafety.Value.HasValue && openingSafety.Value.Value == Fix.Zero, "unknown technology gives a valid false signal");
            c.That(Operations(game).TryGetSignalSchema(watch, new Code("safety"), out var signalSchema) && signalSchema != null,
                "independent signal provider publishes declared schema");
            ulong opening = game.Hash(); Safety(); Technology(); Incident(); Pressure();
            c.Equal(opening, game.Hash(), "knowledge/signal/incident/pressure reads are pure");
            c.That(!Incidents(isolated).TryGetIncident(EntityId.None, new Code("planet.fault"), out _), "invalid identity has no substitute incident");
            c.That(!game.Seat(1).View.Feature<IncidentsView>().TryGetIncident(watch, new Code("planet.fault"), out _), "foreign incident facts private");
            c.That(!game.Seat(1).View.Feature<OperationsView>().TryGetSignalSchema(watch, new Code("safety"), out _), "foreign provider schema private");

            c.That(Operations(game).TryGetAutomationProfile("planet.safety-control", out var profile), "authored profile visible");
            var settings = new AutomationSettings(profile.Id, true, new ControlBinding(plant, new Code("transformation")),
                new Dictionary<string, ControlBinding> { ["ready"] = new ControlBinding(watch, new Code("safety")) },
                new Dictionary<string, ControlThresholdBand>(), profile.WhenTrue, profile.WhenFalse);
            Submit(new ConfigureAutomationCommand { Rule = "safety", Settings = settings });
            c.That(Enabled() && Rule().PendingDecision == false && Rule().PendingAt == game.Clock.StepIndex,
                "unsafe decision queues next-update shutdown without same-update feedback");
            var retainedRule = Rule();
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending safety action persists");
            c.That(Rule(twin).PendingDecision == false && Rule(twin).PendingAt == retainedRule.PendingAt &&
                !Rule(twin).HasApplied && Enabled(twin), "load preserves due shutdown without applying it or changing actual target");
            game.Clock.Pause(); twin.Clock.Pause(); long paused = game.Clock.StepIndex;
            Update(twin); c.That(game.Clock.StepIndex == paused && Enabled() && Rule().PendingDecision == false,
                "one GameClock pause freezes pending action");
            game.Clock.Resume(); twin.Clock.Resume(); Update(twin);
            c.That(!Enabled() && Rule().HasApplied && retainedRule.PendingDecision == false && !retainedRule.HasApplied,
                "normal engine action disables refinery and retained rule stays detached");
            c.Equal(retainedRule.PendingAt, Rule().LastAppliedAt, "restored unsafe action applies at its original due game-clock step");

            Submit(new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = plant,
                Transformation = "planet.assemble", Home = plant }, TechnologyCodes.Locked);
            Submit(new ConstructCommand { Def = "planet.beacon", Priority = 50,
                Placement = Json.Parse("{\"space\":\"planet.space\",\"position\":30}") }, TechnologyCodes.Locked);
            c.That(!Mine("planet.beacon-site").Any() && !Mine("planet.rover").Any(), "locked commands create no physical execution");
            twin = factory.Load(game.Save());
            Until(() => Incident().Active && Lifecycle().OrderId > 0 &&
                Nodes(Lifecycle().OrderId).Any(n => n.Id == "detect" && n.State == NodeState.Done) &&
                Nodes(Lifecycle().OrderId).Any(n => n.Id == "repair" && n.Blocker == TechnologyCodes.Locked),
                "physical detection reaches authored technology blocker", twin);
            var blocked = Nodes(Lifecycle().OrderId).Single(n => n.Id == "repair");
            c.That(blocked.ProductiveSteps == 0 && Stock("planet.alloy", watch) == Fix.One, "unknown research prevents consuming or earning repair");
            c.That(!Enabled() && Production().Progress.Equals(WorkProgress.Zero), "unsafe refinery earns no productive work");
            c.Equal(1L, Pressure().Accepted, "one normal incident pressure command accepted");
            c.Equal(Fix.One, Pressure().Spent, "finite explicit pressure cost consumed");
            c.Equal(Fix.Zero, Pressure().Available, "no hidden pressure refill");
            c.Equal(1, Incident().Episode, "exactly one real episode");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "technology-blocked episode save");

            c.Equal(Code.None, game.Seat(0).View.Feature<TechnologyView>().PreviewResearch("planet.safety", lab, 80), "native research preview");
            var research = Submit(new PostOrderCommand { Plan = "planet.safety", Target = lab, Priority = 80 });
            c.That(!Technology().Known && Stock("planet.alloy", lab) == Fix.One, "research posting does not grant knowledge or consume before travel");
            twin = factory.Load(game.Save());
            Until(() => Technology().Known, "staffed research completion", twin);
            c.That(!Technology(1).Known && Stock("planet.alloy", lab) == Fix.Zero, "earned private knowledge consumes exact authored research material");
            c.That(Incident().Active && Safety().Value == Fix.Zero && !Enabled(), "knowledge alone cannot restart unresolved equipment");
            c.That(researchPartial && Technology().CompletedTick.HasValue && Technology().CompletedTick.Value > 0,
                "observed productive research precedes the native knowledge completion record");
            twin = factory.Load(game.Save());
            Until(() => !Incident().Active && Rule().PendingDecision == true, "physical repair and restart defer safe enable", twin);
            c.That(!Enabled() && Stock("planet.alloy", watch) == Fix.Zero && Safety().Value == Fix.One,
                "safe signal follows real episode completion, target still unchanged");
            c.That(Lifecycle().Snapshot.Episode == 1 && Lifecycle().Snapshot.ResolvedTick > Lifecycle().Snapshot.RaisedTick,
                "incident records positive game-time recovery");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "deferred safe restart save");
            var pendingRestart = Rule();
            twin = factory.Load(game.Save());
            c.That(Rule(twin).PendingDecision == true && Rule(twin).PendingAt == pendingRestart.PendingAt && !Enabled(twin),
                "load preserves pending safe enable without same-load target mutation");
            Update(twin);
            c.That(Enabled() && Rule().Latch && !Rule().PendingDecision.HasValue, "next update applies normal restart");
            c.Equal(pendingRestart.PendingAt, Rule().LastAppliedAt, "restored safe action applies at its original due step");
            c.That(researchPartial && repairPartial, "partial productive research and repair observed and saved");

            twin = factory.Load(game.Save());
            Until(() => AtHome(retainedDigger.Id), "response machine physically returns before haul traffic starts", twin);
            c.That(AtHome(carrier), "carrier remained at authored parking during research and response");
            Submit(new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = plant,
                Transformation = "planet.refine", Count = 6 });
            for (int extraction = 1; extraction <= 6; extraction++)
            {
                Fix before = Deposit(); Submit(new PostOrderCommand { Plan = "planet.extract", Target = mine });
                c.Equal(before, Deposit(), "player posting does not mine");
                Fix remaining = Fix.FromInt(12 - extraction * 2); twin = factory.Load(game.Save());
                Until(() => Deposit() == remaining, "finite extraction " + extraction, twin);
                c.Equal(remaining, Deposit(), "exact earned spatial debit");
            }
            twin = factory.Load(game.Save());
            Until(() => Production().QueueCount == 0 && Stock("planet.alloy", plant) == Fix.FromInt(6) && AtHome(carrier),
                "finite refining and physical carrier parking before deployment", twin);
            Submit(new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = plant,
                Transformation = "planet.assemble", Home = plant });
            twin = factory.Load(game.Save());
            Until(() => Mine("planet.rover").Count() == 1, "earned builder manufacture", twin);
            var builder = Mine("planet.rover").Single().Id;
            Submit(new SetHomeCommand { Worker = builder, Home = EntityId.None });
            var build = Submit(new ConstructCommand { Def = "planet.beacon", Priority = 50,
                Placement = Json.Parse("{\"space\":\"planet.space\",\"position\":30}") });
            c.That(Mine("planet.beacon-site").Any(e => e.Id == build.Entity) && !Mine("planet.beacon").Any(), "unlocked build still creates only a physical project");
            twin = factory.Load(game.Save());
            Until(() => Mine("planet.rover").Count() == 1 && Production().QueueCount == 0 &&
                Mine("planet.beacon").Count() == 1 && !Mine("planet.beacon-site").Any(),
                "native physical manufacture and deployment after recovery", twin);
            c.That(travelled && hauled && processed, "real travel, loaded hauling and timed native processing observed");
            c.That(extractorBay && oreBay && builderBay && alloyBay, "all four authored work/resource bays used through actual jobs");
            c.That(carrierStayedParked, "carrier never obstructed research or incident response traffic");
            c.That(((PointSnapshot)retainedDigger.Spatial).Position == Fix.One && retainedDigger.State == new Code("planet.idle"),
                "retained opening worker facts detached");
            c.Equal(Fix.Zero, Deposit(), "no underground refill");
            c.Equal(Fix.Zero, Stock("planet.ore"), "twelve ore consumed");
            c.Equal(Fix.Zero, Stock("planet.alloy"), "six produced and two initialized alloy all earned/consumed");
            c.That(Technology().Known && !Technology(1).Known && !Incident().Active && Enabled(), "final private research/recovery policy state");
            c.Equal(pendingRestart.PendingAt, Rule().LastAppliedAt, "later physical play does not repeat already-applied safe action");
            c.Equal(1L, Pressure().Accepted, "finite pressure cannot create later hidden episodes");
            var page = game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0);
            c.Equal(ResourceFlowReadStatus.Ready, page.Status, "complete journal window");
            var facts = page.Entries.Select(e => e.Item).ToArray();
            Fix Sum(Func<ResourceFlowFact, bool> predicate) => facts.Where(predicate).Aggregate(Fix.Zero, (sum, fact) => sum + fact.Amount);
            c.Equal(Fix.FromInt(2), Sum(f => f.Kind == ResourceFlowKind.Initialized), "explicit authored bootstrap provenance");
            c.Equal(Fix.FromInt(12), Sum(f => f.Source.Endpoint.Scope == SpatialResourcesModule.ModuleId && f.Kind == ResourceFlowKind.StockTransfer), "real deposit transfer provenance");
            c.Equal(Fix.FromInt(6), Sum(f => f.Kind == ResourceFlowKind.Produced), "only native refinery outputs");
            c.Equal(Fix.FromInt(20), Sum(f => f.Kind == ResourceFlowKind.Consumed), "twelve ore and eight alloy consumed by existing owners");
            c.That(!facts.Any(f => f.Kind == ResourceFlowKind.AuthoredCreation || f.Kind == ResourceFlowKind.StockPurchase), "no fabricated production or buying");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "finished cross-game save/load");
            c.Equal(isolatedHash, isolated.Hash(), "isolated session unchanged");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success && replay.Game != null, "canonical player replay/" + replay.Failure + "/" + replay.Detail);
            if (!replay.Success) throw new InvalidOperationException("replay refused/" + replay.Detail);
            c.Equal(game.Hash(), replay.ActualHash, "research/control/response/production replay final hash");
            c.Equal(isolatedHash, isolated.Hash(), "replay does not mutate another game");
        }
    }
}

