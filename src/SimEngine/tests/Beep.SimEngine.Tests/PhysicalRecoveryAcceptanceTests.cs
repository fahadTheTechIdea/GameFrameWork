using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.LogisticsPolicyContentTests;

namespace Beep.SimEngine.Tests
{
    // Public player commands, immutable observations and sole-clock Update; no simulation implementation.
    internal static class PhysicalRecoveryAcceptanceTests
    {
        internal const string Case = "Physical recovery acceptance: loaded cargo survives road removal and finite fuel rescue";
        private static readonly ReplayLimits Limits = new ReplayLimits(8 * 1024 * 1024, 5000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static ResourceId Resource(SimGame game, string name)
        { if (!game.Seat(0).View.TryFindResource(name, out var id)) throw new InvalidOperationException("missing actual recovery resource " + name); return id; }
        private static LogisticsOperationalSnapshot Facts(SimGame game, string entity, string resource)
        {
            var refusal = View(game).ReadDiagnostics(Id(game, entity), Resource(game, resource), out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual recovery diagnostics " + entity + "/" + refusal);
            return value;
        }
        private static TileSpatialSnapshot Position(SimGame game, string entity) =>
            (TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == Id(game, entity)).Spatial;
        private static WorkJobView Haul(SimGame game, int id)
        {
            var jobs = Jobs(game); foreach (var job in jobs) if (job.Id == id) return job;
            throw new InvalidOperationException("original physical haul disappeared id=" + id + "/at=" + game.Clock.StepIndex +
                "/cargo=" + Facts(game, "recovery.carrier", "steel").Stock +
                "/jobs=" + string.Join(";", jobs.Select(j => j.Id + "/" + j.State + "/" + j.Blocker)));
        }
        private static Code FuelBlocker(SimGame game)
        {
            var refusal = game.Seat(0).View.Feature<MotionView>().ReadOperatingInputs(Id(game, "recovery.carrier"), 1, out var inputs);
            if (!refusal.IsNone) throw new InvalidOperationException("actual movement fuel diagnostics " + refusal);
            return inputs.Blocker;
        }
        private static int Road(Check c, SimGame game)
        {
            var samples = new List<int>();
            c.Equal(SpatialLayerReadStatus.Success, game.Seat(0).View.Feature<TileMapView>().ReadLayer("road", new TileRect(18, 12, 1, 1), samples), "actual authorized road cell");
            return samples.Single();
        }
        private static void Conserved(Check c, SimGame game, bool fuelShortage)
        {
            Fix steel = Fix.Zero, fuel = Fix.Zero;
            foreach (string entity in new[] { "recovery.source", "recovery.destination", "recovery.carrier", "recovery.service" }) {
                steel += Facts(game, entity, "steel").Stock;
                fuel += Facts(game, entity, "recovery.fuel").Stock;
            }
            c.Equal(Fix.FromInt(20), steel, "finite steel conserved during interrupted physical work");
            var summary = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            BigInteger consumed = summary.Aggregates.Where(a => a.Resource == Resource(game, "recovery.fuel") && a.Kind == ResourceFlowKind.Consumed)
                .Aggregate(BigInteger.Zero, (sum, a) => sum + a.AmountRaw);
            c.Equal(new BigInteger(Fix.FromInt(fuelShortage ? 42 : 72).Raw), new BigInteger(fuel.Raw) + consumed, "finite fuel stock plus actual journal consumption conserved");
            c.That(!summary.Aggregates.Any(a => a.Kind == ResourceFlowKind.StockPurchase || a.Kind == ResourceFlowKind.AuthoredCreation || a.Kind == ResourceFlowKind.Produced), "no money, purchases or rescue grants");
        }
        private static void Run(Check c)
        {
            foreach (bool fuelShortage in new[] { false, true }) Scenario(c, fuelShortage);
            foreach (bool partial in new[] { false, true }) EndpointScenario(c, partial);
            BreakdownScenario(c);
            OutputScenario(c);
        }
        private static TransformationSnapshot Process(SimGame game)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Id(game, "recovery.processor"), new Code("process"), out var value))
                throw new InvalidOperationException("missing actual recovery process facts");
            return value;
        }
        private static LogisticsOperationalSnapshot OutputFacts(SimGame game, string resource, string port)
        {
            var refusal = View(game).ReadDiagnostics(Id(game, "recovery.processor"), Resource(game, resource), out var value, new Code(port));
            if (!refusal.IsNone) throw new InvalidOperationException("actual recovery output diagnostics " + refusal);
            return value;
        }
        private static void OutputConserved(Check c, SimGame game)
        {
            var journal = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            BigInteger Flow(string name, ResourceFlowKind kind) => journal.Aggregates.Where(a => a.Resource == Resource(game, name) && a.Kind == kind)
                .Aggregate(BigInteger.Zero, (sum, a) => sum + a.AmountRaw);
            Fix steel = Process(game).Wip.Stock.AmountOf(Resource(game, "steel"));
            foreach (string entity in new[] { "recovery.source", "recovery.destination", "recovery.carrier", "recovery.service", "recovery.processor" }) steel += Facts(game, entity, "steel").Stock;
            c.Equal(new BigInteger(Fix.FromInt(25).Raw), new BigInteger(steel.Raw) + Flow("steel", ResourceFlowKind.Consumed), "raw stock plus protected WIP plus consumed input conserve finite steel");
            foreach (var item in new[] { (resource: "recovery.product", port: "product"), (resource: "recovery.scrap", port: "scrap") }) {
                Fix stock = OutputFacts(game, item.resource, item.port).Stock;
                foreach (string entity in new[] { "recovery.destination", "recovery.carrier", "recovery.service" }) stock += Facts(game, entity, item.resource).Stock;
                c.Equal(Flow(item.resource, ResourceFlowKind.Produced), new BigInteger(stock.Raw), "actual authored output journal equals vessel, cargo and delivered stock");
            }
            c.That(!journal.Aggregates.Any(a => a.Kind == ResourceFlowKind.StockPurchase || a.Kind == ResourceFlowKind.AuthoredCreation), "output clearance uses no money or resource grants");
        }
        private static SimGame DrainOutput(Check c, GameFactory factory, SimGame game, string resource, string port, int quantity, int delivered)
        {
            var twin = Input(c, factory, game, new HaulResourcesCommand { Source = Id(game, "recovery.processor"), SourcePort = new Code(port), Destination = Id(game, "recovery.destination"),
                Worker = Id(game, "recovery.service"), Resource = Resource(game, resource), Ceiling = Fix.FromInt(quantity), Priority = 100 });
            int id = Jobs(game).Single(j => j.IsHaul && j.Worker == Id(game, "recovery.service")).Id;
            Until(c, game, twin, () => Facts(game, "recovery.service", resource).Stock.IsPositive && Facts(game, "recovery.destination", resource).Stock < Fix.FromInt(delivered), "finished stock really enters transporter before delivery");
            c.Equal(Id(game, "recovery.processor"), Haul(game, id).Source, "actual output haul retains physical processor endpoint");
            c.Equal(new Code(port), Haul(game, id).SourcePort, "named output port is not silently substituted");
            twin = Partial(c, factory, game); OutputConserved(c, game);
            Until(c, game, twin, () => Facts(game, "recovery.destination", resource).Stock == Fix.FromInt(delivered) && !Jobs(game).Any(j => j.Id == id && j.Worker.IsValid), "transporter completes timed finished-output delivery and departure");
            OutputConserved(c, game); return twin;
        }
        private static void OutputScenario(Check c)
        {
            var factory = PhysicalRecoveryScenario.Create(false, blockedOutput: true);
            SimGame NewSession() => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 919, Mods = { ResourceFlowsModule.ModuleId }, Seats = { Controller.Human, Controller.Human } });
            var game = NewSession(); var isolated = NewSession(); ulong untouched = isolated.Hash();
            using var recording = game.RecordReplay(Limits);
            c.That(!game.Modules.Contains("sim.tycoon"), "physical output recovery is resource-only");
            var management = game.Seat(0).View.Feature<TransformationManagementView>();
            c.Equal(Code.None, management.ReadOutputPorts(Id(game, "recovery.processor"), new Code("process"), "recovery.batch", out var published), "actual authored output routes");
            c.Equal(2, published.Count, "whole result uses two distinct output books");
            c.That(published[0].Endpoint != published[1].Endpoint, "capacity is independently owned per output book");
            ulong before = game.Hash();
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<TransformationManagementView>().ReadOutputPorts(Id(game, "recovery.processor"), new Code("process"), "recovery.batch", out var foreign), "foreign output metadata withheld");
            c.Equal(0, foreign.Count, "foreign output read leaks no stock or endpoint"); c.Equal(before, game.Hash(), "private output refusal is pure");
            var twin = Input(c, factory, game, new SetWorkerHeldCommand { Worker = Id(game, "recovery.carrier"), Held = true },
                new EnqueueTransformationCommand { Transformer = Id(game, "recovery.processor"), Capability = new Code("process"), Transformation = "recovery.batch", Count = 2 });
            c.Equal(Fix.Zero, OutputFacts(game, "recovery.product", "product").Stock, "enqueue does not instantly produce finished output");
            c.Equal(6L, Process(game).Required, "authored batch takes six game minutes");
            twin = Partial(c, factory, game);
            Until(c, game, twin, () => Process(game).Phase == TransformationPhase.Blocked && Process(game).Blocker == TransformerManager.BlockerOutput, "first batch fills both vessels; completed second batch waits physically");
            var retained = Process(game);
            c.Equal(1, retained.QueueCount, "only first complete result committed"); c.Equal(6L, retained.Progress.WholeTicks, "full output retains completed second-batch work");
            c.Equal(Fix.FromInt(2), retained.Wip.Stock.AmountOf(Resource(game, "steel")), "blocked batch preserves protected actual raw inputs");
            c.Equal(Fix.One, retained.Tools.Held.AmountOf(Resource(game, "steel")), "blocked batch preserves real reusable tool claim");
            c.Equal(Fix.FromInt(2), OutputFacts(game, "recovery.product", "product").Stock, "first product fills authored two-unit vessel");
            c.Equal(Fix.FromInt(2), OutputFacts(game, "recovery.scrap", "scrap").Stock, "first byproduct independently fills its vessel");
            twin = Partial(c, factory, game);
            for (int i = 0; i < 8; i++) {
                Step(c, game, twin); OutputConserved(c, game); c.Equal(retained.Progress, Process(game).Progress, "blocked result gains no repeated productive work");
                c.Equal(retained.Wip.Execution, Process(game).Wip.Execution, "output wait retains same production execution");
                c.Equal(retained.Tools.Receipts.Single().Token, Process(game).Tools.Receipts.Single().Token, "same reusable tool receipt retained while output is full");
            }
            twin = DrainOutput(c, factory, game, "recovery.product", "product", 1, 1);
            c.Equal(Fix.One, OutputFacts(game, "recovery.product", "product").Stock, "partial physical drain frees insufficient capacity for whole output");
            c.Equal(TransformerManager.BlockerOutput, Process(game).Blocker, "no fractional second result despite partial space");
            c.Equal(retained.Progress, Process(game).Progress, "partial drain keeps completed work");
            twin = Partial(c, factory, game);
            twin = DrainOutput(c, factory, game, "recovery.product", "product", 1, 2);
            c.Equal(Fix.Zero, OutputFacts(game, "recovery.product", "product").Stock, "first product completely hauled out");
            c.Equal(Fix.FromInt(2), OutputFacts(game, "recovery.scrap", "scrap").Stock, "still-full byproduct blocks atomic result");
            c.Equal(1, Process(game).QueueCount, "freeing one complete port never commits half the second result");
            c.Equal(retained.Wip.Execution, Process(game).Wip.Execution, "same protected batch survives independent port clearance");
            twin = Partial(c, factory, game);
            twin = DrainOutput(c, factory, game, "recovery.scrap", "scrap", 2, 2);
            Until(c, game, twin, () => Process(game).QueueCount == 0, "second result atomically commits after physical removal frees every output vessel");
            c.Equal(Fix.FromInt(2), OutputFacts(game, "recovery.product", "product").Stock, "second complete product committed exactly once");
            c.Equal(Fix.FromInt(2), OutputFacts(game, "recovery.scrap", "scrap").Stock, "second complete byproduct committed in same whole-result completion");
            c.That(Process(game).Wip.Stock.IsEmpty && Process(game).Tools.Held.IsEmpty, "completion consumes WIP and releases reusable tools");
            foreach (var item in new[] { (resource: "recovery.product", port: "product"), (resource: "recovery.scrap", port: "scrap") }) {
                c.Equal(Fix.Zero, OutputFacts(game, item.resource, item.port).OutboundPromised, "finished drain releases source export promise");
                foreach (string entity in new[] { "recovery.destination", "recovery.service" }) {
                    c.Equal(Fix.Zero, Facts(game, entity, item.resource).InboundPromised, "no orphan output inlet promises");
                    c.Equal(Fix.Zero, Facts(game, entity, item.resource).OutboundPromised, "no orphan loaded output promises");
                }
            }
            c.That(published.All(p => p.Stock.IsEmpty), "retained initial output publication remains detached");
            twin = Partial(c, factory, game);
            for (int i = 0; i < 12; i++) { Step(c, game, twin); OutputConserved(c, game); }
            c.Equal(Fix.FromInt(2), OutputFacts(game, "recovery.product", "product").Stock, "later updates do not repeat product completion");
            c.Equal(Fix.FromInt(2), OutputFacts(game, "recovery.scrap", "scrap").Stock, "later updates do not repeat byproduct completion");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled process and output receipts restore exactly"); c.Equal(untouched, isolated.Hash(), "output recovery leaves another session untouched");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), Limits);
            c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "physical multi-port output recovery replay " + replay.Failure + "/" + replay.Detail);
        }
        private static bool DamageActive(SimGame game)
        {
            if (!game.Seat(0).View.Feature<IncidentsView>().TryGetIncident(Id(game, "recovery.carrier"), new Code("damage"), out var value))
                throw new InvalidOperationException("missing actual carrier incident facts");
            return value.Active;
        }
        private static ActivityNodeView RepairNode(SimGame game)
        {
            var phases = new List<IncidentPhaseView>();
            if (!game.Seat(0).View.Feature<IncidentsView>().CopyPhases(Id(game, "recovery.carrier"), new Code("damage"), phases))
                throw new InvalidOperationException("missing actual carrier response phases");
            return phases.Single(p => p.Id == "repair").Nodes.Single();
        }
        private static void PartsConserved(Check c, SimGame game)
        {
            var summary = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0));
            var consumed = summary.Aggregates.Where(a => a.Resource == Resource(game, "recovery.parts") && a.Kind == ResourceFlowKind.Consumed)
                .Aggregate(BigInteger.Zero, (sum, a) => sum + a.AmountRaw);
            c.Equal(new BigInteger(Fix.One.Raw), new BigInteger(Facts(game, "recovery.carrier", "recovery.parts").Stock.Raw) + consumed,
                "one finite onboard spare plus native consumption conserved");
        }
        private static void BreakdownScenario(Check c)
        {
            var factory = PhysicalRecoveryScenario.Create(false);
            SimGame NewSession() => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 918, Mods = { ResourceFlowsModule.ModuleId }, Seats = { Controller.Human, Controller.Human } });
            var game = NewSession(); var isolated = NewSession(); ulong untouched = isolated.Hash();
            using var recording = game.RecordReplay(Limits);
            c.That(!game.Modules.Contains("sim.tycoon"), "breakdown response has no business module");
            var twin = Input(c, factory, game, new SetWorkerHeldCommand { Worker = Id(game, "crane"), Held = true },
                new HaulResourcesCommand { Source = Id(game, "recovery.source"), Destination = Id(game, "recovery.destination"), Worker = Id(game, "recovery.carrier"), Resource = Resource(game, "steel"), Ceiling = Fix.FromInt(8), Priority = 100 });
            int id = Jobs(game).Single(j => j.IsHaul).Id;
            Until(c, game, twin, () => Haul(game, id).Loaded == Fix.FromInt(8) && Haul(game, id).ToilIndex == 2, "breakdown starts after full physical pickup");
            var pickupPosition = Position(game, "recovery.carrier").Position;
            Until(c, game, twin, () => Position(game, "recovery.carrier").Position != pickupPosition, "carrier actually travels before breakdown");
            var incident = new RaiseIncidentCommand { Target = Id(game, "recovery.carrier"), Source = new Code("damage"), Kind = new Code("recovery.breakdown") };
            ulong before = game.Hash(); c.Equal(Codes.NotOwned, game.Seat(1).Preview(incident), "foreign breakdown admission refused"); c.Equal(before, game.Hash(), "foreign incident preview is pure");
            twin = Input(c, factory, game, incident);
            c.That(DamageActive(game), "ordinary command raises native persistent breakdown");
            var stopped = Position(game, "recovery.carrier").Position; var retained = Haul(game, id);
            var motion = game.Seat(0).View.Feature<MotionView>();
            c.Equal(Code.None, motion.ReadExecution(Id(game, "recovery.carrier"), out var blocked), "actual broken movement diagnostics");
            c.Equal(new Code("recovery.broken"), blocked.Blocker, "native incident signal gates actual travel");
            c.That(!game.Seat(1).View.Feature<IncidentsView>().TryGetIncident(Id(game, "recovery.carrier"), new Code("damage"), out _), "foreign incident facts remain private");
            twin = Partial(c, factory, game);
            for (int i = 0; i < 12; i++) {
                Step(c, game, twin); Conserved(c, game, false); PartsConserved(c, game);
                c.Equal(stopped, Position(game, "recovery.carrier").Position, "active damage grants no free movement");
                c.Equal(Fix.FromInt(8), Haul(game, id).Loaded, "waiting repair preserves actual loaded cargo");
                c.Equal(retained.DestinationEndpoint, Haul(game, id).DestinationEndpoint, "breakdown retains original receiving claim");
                c.Equal(0L, RepairNode(game).ProductiveSteps, "held mechanic grants no productive repair");
            }
            c.Equal(Fix.One, Facts(game, "recovery.carrier", "recovery.parts").Stock, "waiting response does not invent or consume an unavailable mechanic's work");
            var mechanicStart = Position(game, "crane").Position;
            twin = Input(c, factory, game, new SetWorkerHeldCommand { Worker = Id(game, "crane"), Held = false });
            Until(c, game, twin, () => RepairNode(game).ProductiveSteps > 0 && DamageActive(game), "mechanic travels to carrier and begins timed repair");
            c.That(Position(game, "crane").Position != mechanicStart, "repair machine physically travels from its original site");
            c.Equal(Id(game, "crane"), RepairNode(game).Worker, "native response uses the real compatible machine");
            c.Equal(6L, RepairNode(game).RequiredTicks, "six authored game minutes required, not instantaneous repair");
            c.Equal(Code.None, motion.ReadExecution(Id(game, "recovery.carrier"), out var servicing), "repair service hold diagnostics");
            c.Equal(WorkDockCodes.ServiceHold, servicing.DockBlocker, "actual repair dock claim also prevents drive-off");
            twin = Partial(c, factory, game);
            c.That(DamageActive(game), "partial repair save keeps damage active");
            Until(c, game, twin, () => !DamageActive(game), "native response completes repair and resolves episode");
            c.Equal(Fix.Zero, Facts(game, "recovery.carrier", "recovery.parts").Stock, "physical repair consumes one actual onboard spare");
            c.Equal(6L, RepairNode(game).ProductiveSteps, "response earned every authored minute");
            twin = Partial(c, factory, game);
            Until(c, game, twin, () => Facts(game, "recovery.destination", "steel").Stock == Fix.FromInt(8) && !Jobs(game).Any(j => j.Id == id && j.Worker.IsValid), "same original haul resumes after timed response and dock departure");
            c.Equal(Fix.FromInt(12), Facts(game, "recovery.source", "steel").Stock, "breakdown never repeats pickup");
            c.Equal(Fix.Zero, Facts(game, "recovery.carrier", "steel").Stock, "all original cargo delivered once");
            c.Equal(Fix.Zero, Facts(game, "recovery.destination", "steel").InboundPromised, "repaired delivery releases inlet receipt");
            c.Equal(Fix.Zero, Facts(game, "recovery.carrier", "steel").OutboundPromised, "repaired delivery releases cargo receipt");
            for (int i = 0; i < 12; i++) { Step(c, game, twin); Conserved(c, game, false); PartsConserved(c, game); }
            c.Equal(Fix.FromInt(8), Facts(game, "recovery.destination", "steel").Stock, "later updates never repeat repaired delivery");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled incident and haul restore exactly"); c.Equal(untouched, isolated.Hash(), "repair leaves interleaved session untouched");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), Limits);
            c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "physical breakdown response replay " + replay.Failure + "/" + replay.Detail);
        }
        private static readonly ResourceEndpointId OriginalBook = new ResourceEndpointId("colony.pools", 9100000001);
        private static readonly ResourceEndpointId AlternateBook = new ResourceEndpointId("colony.pools", 9100000002);
        private static LogisticsOperationalSnapshot EndpointFacts(SimGame game, bool original)
        {
            var refusal = View(game).ReadDiagnostics(Id(game, "recovery.alternate"), Resource(game, "steel"), out var value,
                new Code("alternate"), original ? OriginalBook : AlternateBook);
            if (!refusal.IsNone) throw new InvalidOperationException("actual surviving endpoint book " + refusal);
            return value;
        }
        private static ConfigureStockPolicyCommand Band(SimGame game, string entity, Code port, int quantity) => new ConfigureStockPolicyCommand {
            Store = Id(game, entity), Port = port, Resource = Resource(game, "steel"),
            Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(quantity), Fix.FromInt(quantity), Fix.FromInt(quantity), 100, LogisticsReserveAccess.Protected)) };
        private static ConfigureWorkPolicyCommand CarrierEnabled(SimGame game, bool enabled) => new ConfigureWorkPolicyCommand {
            Worker = Id(game, "recovery.carrier"), Settings = new WorkPolicySettings(enabled, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) };
        private static void EndpointConserved(Check c, SimGame game)
        {
            Fix steel = EndpointFacts(game, true).Stock + EndpointFacts(game, false).Stock;
            foreach (string entity in new[] { "recovery.source", "recovery.funding-sink", "recovery.carrier", "recovery.service" }) steel += Facts(game, entity, "steel").Stock;
            c.Equal(Fix.FromInt(20), steel, "independent original and replacement books plus physical cargo conserve steel");
        }
        private static void EndpointScenario(Check c, bool partial)
        {
            var factory = PhysicalRecoveryScenario.Create(false, endpointLoss: true);
            SimGame NewSession() => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 917, Mods = { ResourceFlowsModule.ModuleId }, Seats = { Controller.Human, Controller.Human } });
            var game = NewSession(); var isolated = NewSession(); ulong untouched = isolated.Hash();
            using var recording = game.RecordReplay(Limits);
            c.That(!game.Modules.Contains("sim.tycoon"), "physical endpoint recovery remains resource-only");
            var twin = Input(c, factory, game, new SetWorkerHeldCommand { Worker = Id(game, "recovery.service"), Held = true },
                new HaulResourcesCommand { Source = Id(game, "recovery.source"), Destination = Id(game, "recovery.funding-sink"), Worker = Id(game, "recovery.carrier"), Resource = Resource(game, "steel"), Ceiling = Fix.FromInt(8), Priority = 100 });
            int funded = Jobs(game).Single(j => j.IsHaul).Id;
            Until(c, game, twin, () => Haul(game, funded).Loaded == Fix.FromInt(8) && Haul(game, funded).ToilIndex == 2, "endpoint loss begins with physical full pickup");
            twin = Input(c, factory, game, new SetWorkerHeldCommand { Worker = Id(game, "recovery.carrier"), Held = true });
            c.That(!Jobs(game).Any(j => j.Id == funded && j.Worker.IsValid), "player ends funding execution, not its actual cargo");
            c.Equal(Fix.FromInt(8), Facts(game, "recovery.carrier", "steel").Stock, "initial cancellation protects all loaded cargo");
            twin = Input(c, factory, game, Band(game, "recovery.funding-sink", Code.None, 0), Band(game, "recovery.destination", new Code("target"), 8), Band(game, "recovery.alternate", new Code("alternate"), 3));
            twin = Input(c, factory, game, new SetWorkerHeldCommand { Worker = Id(game, "recovery.carrier"), Held = false });
            Until(c, game, twin, () => Jobs(game).Any(j => j.IsHaul && j.RecoveryKind == LogisticsRecoveryKind.CargoReturn && j.DestinationEndpoint == OriginalBook), "ordinary engine cargo-return assignment selects declared original endpoint");
            int id = Jobs(game).Single(j => j.IsHaul && j.Worker == Id(game, "recovery.carrier")).Id;
            var initial = Haul(game, id);
            if (partial) Until(c, game, twin, () => EndpointFacts(game, true).Stock.IsPositive && Haul(game, id).Loaded.IsPositive, "genuine partial original delivery before site demolition");
            twin = Input(c, factory, game, CarrierEnabled(game, false));
            var retained = Haul(game, id); Fix prefix = EndpointFacts(game, true).Stock, remaining = retained.Loaded;
            c.Equal(Fix.FromInt(8), prefix + remaining, "committed prefix plus real remaining cargo");
            if (partial) c.That(prefix.IsPositive && remaining.IsPositive, "partial endpoint loss is a physical split, not fabricated state");
            c.Equal(remaining, EndpointFacts(game, true).InboundPromised, "exact surviving original receipt before demolition");
            EntityId removed = Id(game, "recovery.destination");
            twin = Partial(c, factory, game);
            twin = Input(c, factory, game, new DemolishCommand { Target = removed });
            c.That(game.Seat(0).Snapshot.Entities.Any(e => e.Id == removed), "demolition command does not instantly remove the loading site");
            Until(c, game, twin, () => !game.Seat(0).Snapshot.Entities.Any(e => e.Id == removed), "crane physically demolishes original endpoint site");
            c.Equal(remaining, EndpointFacts(game, true).InboundPromised, "independent original book survives actual site removal");
            c.Equal(prefix, EndpointFacts(game, true).Stock, "site demolition never rewinds the committed delivery prefix");
            c.Equal(initial.Worker, Haul(game, id).Worker, "same physical carrier survives geometry removal");
            c.Equal(OriginalBook, Haul(game, id).DestinationEndpoint, "too-small alternate has not stolen original binding");
            c.Equal(Fix.Zero, EndpointFacts(game, false).InboundPromised, "three-unit alternate never takes a partial whole-cargo replacement");
            twin = Input(c, factory, game, CarrierEnabled(game, true));
            Until(c, game, twin, () => Haul(game, id).Blocker == HaulRecoveryCodes.DestinationUnavailable, "removed physical endpoint produces typed whole-load wait");
            c.Equal(HaulRecoveryStage.WaitingForDestination, Haul(game, id).RecoveryStage, "public diagnostics identify destination loss");
            twin = Partial(c, factory, game); var stopped = Position(game, "recovery.carrier").Position;
            for (int i = 0; i < 6; i++) {
                Step(c, game, twin); EndpointConserved(c, game); c.Equal(remaining, Haul(game, id).Loaded, "saved unavailable recovery retains actual cargo");
                c.Equal(stopped, Position(game, "recovery.carrier").Position, "unavailable destination grants no movement");
                c.Equal(remaining, EndpointFacts(game, true).InboundPromised, "original receiving promise remains exact while waiting");
                c.Equal(Fix.Zero, EndpointFacts(game, false).InboundPromised, "insufficient replacement never reserves a fragment");
            }
            var foreign = game.Seat(1).View.Feature<LogisticsView>();
            c.Equal(Codes.NotOwned, foreign.ReadDiagnostics(Id(game, "recovery.alternate"), Resource(game, "steel"), out var hidden, new Code("alternate"), OriginalBook), "foreign original-book diagnostics refused");
            c.That(hidden == null, "foreign endpoint refusal reveals no surviving receipt facts");
            twin = Input(c, factory, game, Band(game, "recovery.alternate", new Code("alternate"), 8));
            // Demolition leaves its machine physically in the narrow road. Player relocation must clear it.
            twin = Input(c, factory, game, new MoveVehicleCommand { Vehicle = Id(game, "crane"), To = new TileCoord(18, 15) });
            Until(c, game, twin, () => Haul(game, id).DestinationEndpoint == AlternateBook, "complete remaining replacement is admitted atomically");
            c.Equal(remaining, Haul(game, id).Loaded, "replanning keeps actual remaining cargo only");
            c.Equal(prefix, Haul(game, id).UnloadingBaseline, "committed original prefix remains authoritative");
            c.Equal(Fix.Zero, EndpointFacts(game, true).InboundPromised, "replacement releases original surviving book exactly");
            c.Equal(remaining, EndpointFacts(game, false).InboundPromised, "complete residual replacement owns one exact inlet claim");
            c.Equal(Fix.Zero, EndpointFacts(game, false).Stock, "replanning is not instant delivery");
            twin = Partial(c, factory, game);
            Until(c, game, twin, () => EndpointFacts(game, false).Stock == remaining && !Jobs(game).Any(j => j.Id == id && j.Worker.IsValid), "physical remaining delivery and dock departure complete");
            c.Equal(prefix, EndpointFacts(game, true).Stock, "later recovery never repeats original delivery prefix");
            c.Equal(Fix.Zero, EndpointFacts(game, true).InboundPromised, "no original receipt leak"); c.Equal(Fix.Zero, EndpointFacts(game, false).InboundPromised, "no replacement receipt leak");
            c.Equal(Fix.Zero, Facts(game, "recovery.carrier", "steel").OutboundPromised, "no retained cargo receipt leak");
            for (int i = 0; i < 12; i++) { Step(c, game, twin); EndpointConserved(c, game); }
            c.Equal(remaining, EndpointFacts(game, false).Stock, "terminal updates do not duplicate recovered delivery");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "removed endpoint and settled book restore exactly"); c.Equal(untouched, isolated.Hash(), "endpoint recovery does not touch another session");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), Limits);
            c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "physical site demolition recovery replay " + replay.Failure + "/" + replay.Detail);
        }
        private static void Scenario(Check c, bool fuelShortage)
        {
            var factory = PhysicalRecoveryScenario.Create(fuelShortage);
            SimGame NewSession() => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 916, Mods = { ResourceFlowsModule.ModuleId }, Seats = { Controller.Human, Controller.Human } });
            var game = NewSession(); var isolated = NewSession(); ulong untouched = isolated.Hash();
            using var recording = game.RecordReplay(Limits);
            c.That(!game.Modules.Contains("sim.tycoon"), "resource recovery has no optional business module");
            c.Equal(1, Road(c, game), "authored physical road initially present");
            var edge = new List<int>();
            c.Equal(SpatialLayerReadStatus.Success, game.Seat(0).View.Feature<TileMapView>().ReadLayer("road", new TileRect(47, 12, 1, 1), edge), "boundary road is actually present");
            c.Equal(1, edge.Single(), "boundary test is not a missing-road refusal");
            ulong edgeHash = game.Hash(); c.Equal(new Code("core.unreachable"), game.Seat(0).Preview(new MoveVehicleCommand { Vehicle = Id(game, "recovery.carrier"), To = new TileCoord(47, 12) }), "mobile dock cannot navigate beyond its declared slot geometry bounds");
            c.Equal(edgeHash, game.Hash(), "mobile geometry-bound refusal is pure");
            var command = new HaulResourcesCommand { Source = Id(game, "recovery.source"), Destination = Id(game, "recovery.destination"),
                Worker = Id(game, "recovery.carrier"), Resource = Resource(game, "steel"), Ceiling = Fix.FromInt(8), Priority = 100 };
            ulong before = game.Hash(); c.Equal(Codes.NotFound, game.Seat(1).Preview(command), "foreign loaded-haul admission refused"); c.Equal(before, game.Hash(), "foreign preview is pure");
            var twin = Input(c, factory, game, command);
            int id = Jobs(game).Single(j => j.IsHaul).Id;
            Until(c, game, twin, () => Haul(game, id).Loaded == Fix.FromInt(8) && Haul(game, id).ToilIndex == 2, "full physical loading before interruption");
            twin = Partial(c, factory, game);
            if (fuelShortage) {
                Until(c, game, twin, () => FuelBlocker(game) == OperatingInputCodes.Inputs, "actual movement exhausts finite paid movement exposure");
                c.Equal(Fix.Zero, Facts(game, "recovery.carrier", "recovery.fuel").Stock, "actual shortage follows finite stock consumption");
                var motion = game.Seat(0).View.Feature<MotionView>();
                c.Equal(Code.None, motion.ReadOperatingInputs(Id(game, "recovery.carrier"), 1, out var inputs), "public movement operating-input diagnostic");
                c.Equal(OperatingInputCodes.Inputs, inputs.Blocker, "native fuel shortage blocks actual next movement interval");
                var stopped = Position(game, "recovery.carrier").Position; var retained = Haul(game, id);
                for (int i = 0; i < 12; i++) { Step(c, game, twin); Conserved(c, game, true); }
                c.Equal(stopped, Position(game, "recovery.carrier").Position, "no fuel means no free travel");
                c.Equal(Fix.FromInt(8), Facts(game, "recovery.carrier", "steel").Stock, "stranded cargo remains physically aboard");
                c.Equal(retained.DestinationEndpoint, Haul(game, id).DestinationEndpoint, "fuel wait keeps original destination reservation");
                twin = Partial(c, factory, game);
                twin = Input(c, factory, game, new HaulResourcesCommand { Source = Id(game, "recovery.source"), Destination = Id(game, "recovery.carrier"),
                    Worker = Id(game, "recovery.service"), Resource = Resource(game, "recovery.fuel"), Ceiling = Fix.FromInt(30), Priority = 200 });
                Until(c, game, twin, () => Facts(game, "recovery.service", "recovery.fuel").Stock.IsPositive, "second machine physically loads finite rescue fuel");
                c.Equal(Fix.Zero, Facts(game, "recovery.carrier", "recovery.fuel").Stock, "rescue dispatch is not instant refueling");
                twin = Partial(c, factory, game);
                Until(c, game, twin, () => Facts(game, "recovery.carrier", "recovery.fuel").Stock.IsPositive, "fuel arrives physically at stranded vehicle dock");
                c.Equal(Fix.FromInt(8), Haul(game, id).Loaded, "fuel rescue does not duplicate or replace original loaded job");
                c.Equal(Code.None, motion.ReadExecution(Id(game, "recovery.carrier"), out var servicing), "actual service hold is visible");
                c.Equal(WorkDockCodes.ServiceHold, servicing.DockBlocker, "original receiving dock claim prevents drive-off");
                twin = Partial(c, factory, game);
                for (int i = 0; i < 3; i++) { Step(c, game, twin); Conserved(c, game, true); c.Equal(stopped, Position(game, "recovery.carrier").Position, "partial fuel delivery keeps receiving vehicle still"); }
            } else {
                twin = Input(c, factory, game, new PostOrderCommand { Target = Id(game, "recovery.cut-site"), Plan = "recovery.cut-road" });
                c.Equal(1, Road(c, game), "road-removal command does not instantly edit the map");
                Until(c, game, twin, () => Road(c, game) == 0, "crane physically removes authored road segment");
                Until(c, game, twin, () => Haul(game, id).State == JobState.Paused && Haul(game, id).Blocker == WorkSystem.BlockerUnreachable, "removed road makes actual route unreachable");
                var retained = Haul(game, id); var stopped = Position(game, "recovery.carrier").Position;
                for (int i = 0; i < 12; i++) { Step(c, game, twin); Conserved(c, game, false); }
                c.Equal(stopped, Position(game, "recovery.carrier").Position, "carrier cannot cross removed road");
                c.Equal(retained.Loaded, Haul(game, id).Loaded, "road loss retains actual original cargo");
                c.Equal(retained.DestinationEndpoint, Haul(game, id).DestinationEndpoint, "road loss retains original receiving claim");
                twin = Partial(c, factory, game);
                twin = Input(c, factory, game, new LayLineCommand { Line = "road", Tiles = { new TileCoord(18, 12) } });
                c.Equal(0, Road(c, game), "rebuild command does not instantly restore road");
                Until(c, game, twin, () => Road(c, game) == 1, "native line construction physically restores same segment");
                twin = Input(c, factory, game, new MoveVehicleCommand { Vehicle = Id(game, "crane"), To = new TileCoord(18, 15) });
            }
            twin = Partial(c, factory, game);
            Until(c, game, twin, () => Facts(game, "recovery.destination", "steel").Stock == Fix.FromInt(8) && !Jobs(game).Any(j => j.Id == id && j.Worker.IsValid), "original loaded job completes after physical recovery");
            Until(c, game, twin, () => !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), "all actual recovery hauling and departure settle");
            c.Equal(Fix.FromInt(12), Facts(game, "recovery.source", "steel").Stock, "original eight units removed once");
            c.Equal(Fix.Zero, Facts(game, "recovery.carrier", "steel").Stock, "original cargo delivered once");
            foreach (string entity in new[] { "recovery.source", "recovery.destination", "recovery.carrier", "recovery.service" })
                foreach (string resource in new[] { "steel", "recovery.fuel" }) {
                    var facts = Facts(game, entity, resource); c.Equal(Fix.Zero, facts.InboundPromised, "no remaining inlet claims"); c.Equal(Fix.Zero, facts.OutboundPromised, "no remaining export/cargo claims");
                }
            for (int i = 0; i < 12; i++) { Step(c, game, twin); Conserved(c, game, fuelShortage); }
            c.Equal(Fix.FromInt(8), Facts(game, "recovery.destination", "steel").Stock, "later updates do not repeat completed delivery");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal recovery restores exact native state"); c.Equal(untouched, isolated.Hash(), "interleaved session untouched");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), Limits);
            c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "actual physical recovery replay " + replay.Failure + "/" + replay.Detail);
        }
    }
}
