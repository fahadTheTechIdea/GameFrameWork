using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using JsonParser = Beep.SimEngine.Content.Json;

namespace Beep.SimEngine.Tests
{
    public static partial class ResourceDispositionTests
    {
        private const string IncidentContentionNativeCase = "Incident contention: simultaneous local responses preserve scarce physical supply saved episodes and allocation budgets";
        private const string IncidentContentionScopedCase = "Incident contention: simultaneous scoped independent effects preserve half-minute physical supply saved episodes and allocation budgets";
        private static void IncidentContention(Check c, bool scoped, bool custom, bool half)
        {
            var profile = JsonParser.Parse(FrameworkProofContent.Read("work-effect-incident-contention.json"));
            var response = JsonParser.Parse(FrameworkProofContent.Read("work-effect-incident-ports.json"));
            response.Get("activities")[0].Get("nodes")[0].Set("effect", JsonValue.Of(custom ? "port.dispose-independent" : "port.dispose"));
            PortPhysical(c, scoped, custom, "destroyed", (factory, game, abandoned, site) => IncidentContentionContinuation(c, factory, game, abandoned, site, profile, scoped, custom),
                edit: root => {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(IncidentResponseModule.ModuleId));
                    root.Get("scenarios")[0].Get("settings").Get("sim.work").Set("retry", JsonValue.Of("disposition.incident.retry"));
                    root.Get("entities")[1].Get("components").Set("incident", profile.Get("sources").Clone());
                    if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
                }, incidentDocument: response.ToString());
        }
        // Same public physical setup/engine owners. No response scheduler, raw stocks/jobs or provider writes.
        private static void IncidentContentionContinuation(Check c, GameFactory factory, SimGame game, int abandoned, EntityId site,
            JsonValue profile, bool scoped, bool custom)
        {
            var sources = Enumerable.Range(0, profile.Get("sources").Count).Select(i => new Code(profile.Get("sources")[i].Get("id").Text)).ToArray();
            int waves = int.Parse(profile.Get("waves").Text, System.Globalization.CultureInfo.InvariantCulture);
            var budget = new CapacityLogisticsStressTests.Profile();
            var idle = factory.Load(game.Save()); ulong untouched = idle.Hash(); var twin = factory.Load(game.Save());
            var carrier = game.Seat(0).Snapshot.Entities.Single(row => row.Def == "machine").Id;
            var crew = game.Seat(0).Snapshot.Entities.Single(row => row.Def == "crew").Id;
            var donor = game.Seat(0).Snapshot.Entities.Single(row => row.Def == "source").Id;
            if (!game.Seat(0).View.TryFindResource("supply", out var supply) || !game.Seat(0).View.TryFindResource("spare", out var spare))
                throw new InvalidOperationException("actual contention resources absent");
            Code effect = new Code(custom ? "port.dispose-independent" : "port.dispose");
            long peak = 0, allocated = 0; int updates = 0; var trace = new Queue<string>(); var generations = new HashSet<int>();
            WorkView Work(SimGame current) => current.Seat(0).View.Feature<WorkView>();
            IncidentLifecycleView Life(Code source) {
                if (!game.Seat(0).View.Feature<IncidentsView>().TryGetLifecycle(site, source, out var value)) throw new InvalidOperationException("missing named lifecycle " + source);
                return value;
            }
            ActivityNodeView Node(Code source, string id) {
                var rows = new List<ActivityNodeView>(); if (!Work(game).TryCopyNodes(Life(source).OrderId, rows)) throw new InvalidOperationException("missing response nodes " + source);
                return rows.Single(row => row.Id == id);
            }
            List<WorkJobView> Jobs() { var rows = new List<WorkJobView>(); Work(game).CopyJobs(rows); return rows; }
            LogisticsOperationalSnapshot Stock(ResourceId resource, string port) {
                Code result = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(site, resource, out var value, new Code(port));
                if (!result.IsNone) throw new InvalidOperationException("actual named response stock " + result); return value;
            }
            BigInteger Removed(ResourceId resource) => game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates
                .Where(row => row.Resource == resource && row.Kind == ResourceFlowKind.Destroyed && row.Cause == effect).Aggregate(BigInteger.Zero, (sum, row) => sum + row.AmountRaw);
            void Observe() {
                if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("contention response fault " + game.Fault + "/" + twin.Fault);
                c.Equal(game.Hash(), twin.Hash(), "saved simultaneous incident sessions retain actual episode, claims, progress and time after every update");
                foreach (var resource in new[] { supply, spare }) {
                    Fix total = Fix.Zero;
                    foreach (var entity in game.Seat(0).Snapshot.Entities)
                        if (game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, resource, out var stock).IsNone) total += stock.Stock;
                    if (scoped) total += Stock(resource, resource == supply ? "feed" : "parts").Stock;
                    c.Equal(new BigInteger(Fix.FromInt(resource == supply ? 20 : 10).Raw), new BigInteger(total.Raw) + Removed(resource),
                        "physical stock/cargo/opaque buffers plus classified cleanup remains exactly funded");
                }
                var active = Jobs().Where(row => row.State == JobState.Assigned).ToArray();
                c.Equal(active.Length, active.Select(row => row.Worker).Distinct().Count(), "simultaneous responses and supply cannot share an acquired machine");
                c.That(active.Count(row => !row.IsHaul) <= 1 && active.Where(row => !row.IsHaul).All(row => row.Worker == crew), "one authored response machine serves concurrent jobs without duplicate assignment");
                foreach (var endpoint in new[] { donor, site }) {
                    Fix bay = ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(row => row.Id == endpoint).Spatial).Position + Fix.One;
                    c.That(game.Seat(0).Snapshot.Entities.Count(row => (row.Id == carrier || row.Id == crew) &&
                        ((PointSnapshot)row.Spatial).Position == bay) <= 1, "response work and physical replenishment never overlap the actual shared service bay");
                }
                trace.Enqueue(game.Clock.StepIndex + ":" + string.Join(";", active.Select(row => row.Id + "/" + row.State + "/" + row.Blocker + "/" + row.Loaded)));
                if (trace.Count > 8) trace.Dequeue();
            }
            void Update(SimGame current) {
                long before = GC.GetAllocatedBytesForCurrentThread(); current.Update();
                if (ReferenceEquals(current, game)) { long bytes = GC.GetAllocatedBytesForCurrentThread() - before; peak = Math.Max(peak, bytes); allocated = checked(allocated + bytes); updates++; }
            }
            void Step() { Update(game); Update(twin); Observe(); }
            void Reach(Func<bool> done, string stage) {
                for (int i = 0; i < 180 && !done(); i++) Step();
                if (!done()) throw new InvalidOperationException(stage + " exceeded180 updates at " + game.Clock.StepIndex + ":" + string.Join(" | ", trace) + "; incidents=" +
                    string.Join(";", sources.Select(source => source + "/" + Life(source).Snapshot.Active + "/" + Life(source).OrderId + "/" + Life(source).OrderState + "/" + Node(source, "cleanup").State + "/" + Node(source, "cleanup").Blocker)));
            }
            void Input(Command command) {
                void Send(SimGame current) {
                    ulong before = current.Hash(); c.Equal(Code.None, current.Seat(0).Preview(command), "ordinary player response/supply intent admitted");
                    c.Equal(before, current.Hash(), "preview changes no incident, material or machine ownership");
                    int sequence = current.Seat(0).Submit(command); Update(current); long cursor = 0; var answers = new List<CommandResult>(); current.Seat(0).ReadResults(ref cursor, answers);
                    var result = answers.Single(row => row.Sequence == sequence); c.That(result.Accepted, "ordinary intent commits through engine inbox " + result.Refusal);
                    if (!result.Accepted) throw new InvalidOperationException("response contention input refused " + result.Refusal);
                }
                Send(game); Send(twin); Observe();
            }
            void Save() { twin = factory.Load(game.Save()); Observe(); }
            void Haul(ResourceId resource, string port, int amount) => Input(new HaulResourcesCommand { Source = donor, Destination = site,
                SourcePort = new Code("stock"), DestinationPort = new Code(port), Resource = resource, Ceiling = Fix.FromInt(amount), Worker = carrier, Priority = 99 });
            c.That(!game.Modules.Contains("sim.tycoon"), "simultaneous response and scarce physical resources need no business module");
            var limits = new ReplayLimits(16 * 1024 * 1024, 5000);
            ulong initialHash = game.Hash();
            using var recording = game.RecordReplay(limits);
            var plans = new List<PlanView>(); Work(game).CopyPlans(plans);
            Input(new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { plans.Single().Id }) });
            c.Equal(BigInteger.Zero, Removed(supply), "setup cancellation leaves the actual initial bill unspent");
            var previous = new Dictionary<Code, int>();
            for (int wave = 0; wave < waves; wave++) {
                foreach (var current in new[] { game, twin }) {
                    var sequences = new List<int>();
                    foreach (var source in sources) {
                        var command = new RaiseIncidentCommand { Target = site, Source = source, Kind = new Code("proof.disposition") };
                        c.Equal(Code.None, current.Seat(0).Preview(command), "each named source independently admits its next actual episode"); sequences.Add(current.Seat(0).Submit(command));
                    }
                    Update(current); long cursor = 0; var answers = new List<CommandResult>(); current.Seat(0).ReadResults(ref cursor, answers);
                    c.That(sequences.All(sequence => answers.Single(row => row.Sequence == sequence).Accepted), "same-update incidents are actual independent engine episodes");
                }
                Observe();
                c.That(sources.Select(source => Life(source).OrderId).Distinct().Count() == sources.Length && sources.All(source => Life(source).Snapshot.Active && Life(source).Snapshot.Episode == wave + 1),
                    "simultaneous named sources own distinct responses in the same wave");
                foreach (var source in sources) {
                    int order = Life(source).OrderId; c.That(generations.Add(order) && (!previous.TryGetValue(source, out int old) || order > old), "new episode allocates one fresh response, not copied or reset history"); previous[source] = order;
                    c.That(!game.Seat(1).View.Feature<IncidentsView>().TryGetLifecycle(site, source, out _), "foreign company cannot inspect concurrent private episodes");
                }
                Save();
                Reach(() => sources.Count(source => !Life(source).Snapshot.Active) == (wave == 0 ? 1 : 0) &&
                    sources.Where(source => Life(source).Snapshot.Active).All(source => Node(source, "cleanup").State == NodeState.Completing && Node(source, "cleanup").Blocker == WorkEffectCodes.NoStock),
                    "scarce whole bills leave distinct earned responses at typed resource waits");
                int remaining = sources.Count(source => Life(source).Snapshot.Active); var removedBefore = Removed(supply);
                c.That(sources.Where(source => Life(source).Snapshot.Active).All(source => Node(source, "restart").Progress.WholeTicks == 0), "no unsupplied cleanup starts its dependent restart");
                Save();
                Reach(() => Work(game).ReadHome(crew, out var home).IsNone && home.State == WorkerHomeState.AtHome && !Jobs().Any(row => row.IsHaul), "earned waiting work releases the response machine to its real home");
                Haul(supply, "feed", remaining * 2);
                Reach(() => Stock(supply, "feed").Stock == Fix.FromInt(remaining * 2) && !Jobs().Any(row => row.IsHaul) &&
                    Work(game).ReadHome(carrier, out var home).IsNone && home.State == WorkerHomeState.AtHome, "first scarce resource physically unloads and carrier returns");
                c.Equal(removedBefore, Removed(supply), "missing spares prevent partial bill consumption despite competing earned responses");
                c.Equal(Fix.Zero, Stock(supply, "feed").OutboundPromised, "no response monopolizes incomplete bill authority");
                Haul(spare, "parts", remaining);
                Reach(() => sources.Any(source => Life(source).Snapshot.Active && Node(source, "restart").Progress.WholeTicks > 0 &&
                    Node(source, "restart").Progress.WholeTicks < Node(source, "restart").RequiredTicks), "one supplied response machine earns partial restart amid contention");
                var interrupted = sources.First(source => Life(source).Snapshot.Active && Node(source, "restart").Progress.WholeTicks > 0 && Node(source, "restart").Progress.WholeTicks < Node(source, "restart").RequiredTicks);
                int original = Life(interrupted).OrderId; var cleanup = Node(interrupted, "cleanup"); var destroyed = Removed(supply);
                Input(new CancelOrderCommand { Order = original });
                c.That(Life(interrupted).OrderId == original && Node(interrupted, "cleanup").State == NodeState.Done && Node(interrupted, "cleanup").Progress.Equals(cleanup.Progress), "automatic response resumes original episode/order without replaying cleanup");
                c.Equal(destroyed, Removed(supply), "restart cancellation does not refund or duplicate classified cleanup");
                Reach(() => sources.All(source => !Life(source).Snapshot.Active) && !Jobs().Any(row => row.IsHaul), "all physically supplied concurrent responses finish through original owners");
                foreach (var source in sources) {
                    c.That(Life(source).OrderId == previous[source] && Life(source).OrderState == OrderState.Done && Node(source, "cleanup").State == NodeState.Done && Node(source, "restart").State == NodeState.Done,
                        "each source preserves its own completed response identity and earned phases");
                }
                c.Equal(new BigInteger(Fix.FromInt((wave + 1) * sources.Length * 2).Raw), Removed(supply), "every concurrent episode spends exactly one original supply bill");
                c.Equal(new BigInteger(Fix.FromInt((wave + 1) * sources.Length).Raw), Removed(spare), "every concurrent episode spends exactly one original spare bill");
                Save();
            }
            c.Equal(waves * sources.Length, generations.Count, "all waves retain distinct monotonic actual response orders");
            c.That(Stock(supply, "feed").Stock == Fix.Zero && Stock(spare, "parts").Stock == Fix.Zero && Stock(supply, "feed").OutboundPromised == Fix.Zero && Stock(spare, "parts").OutboundPromised == Fix.Zero &&
                Stock(supply, "feed").InboundPromised == Fix.Zero && Stock(spare, "parts").InboundPromised == Fix.Zero,
                "all selected resource stocks and response promises settle without duplication");
            c.That(game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).EvictedThroughTick >= 0,
                "actual bounded history eviction preserves cumulative cleanup across all concurrent episodes");
            c.Equal(untouched, idle.Hash(), "another loaded session retains its original work and materials");
            c.That(peak <= budget.PerUpdateBudget && allocated <= budget.TotalBudget, "existing managed Update allocation budgets remain valid for this finite incident workload");
            byte[] bytes = recording.Finish(); var description = ReplayVerifier.Describe(bytes, limits);
            c.That(description.InitialStep == recording.InitialStep && description.InitialHash == initialHash,
                "incident recording starts before cancellation and every named raise");
            c.That(description.FinalStep == game.Clock.StepIndex && description.FinalHash == game.Hash(),
                "incident recording reaches the actual completed final wave, not an old session prefix");
            c.Equal(1 + waves * (sources.Length + 3), recording.CommandCount,
                "complete incident recording retains initial plan cancellation, every raise, both physical refills and restart cancellation per wave");
            c.That(description.Commands == recording.CommandCount && description.Checkpoints == recording.CheckpointCount,
                "complete incident description retains every original command and checkpoint");
            c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "one checkpoint per earned incident game update");
            c.Equal(updates, recording.CheckpointCount, "recorded primary is retained across every saved-twin restoration");
            var verification = new ReplayVerifier().Verify(factory, bytes, limits);
            c.That(verification.Success && verification.Step == game.Clock.StepIndex && verification.ExpectedHash == game.Hash()
                && verification.ActualHash == game.Hash(), "complete raised/refilled/cancelled incident stream reproduces its final live state: " + verification.Failure + "/" + verification.Detail);
            c.That(verification.Game != null && verification.Game.Hash() == game.Hash(), "verified incident world retains the actual complete final state");
            Console.WriteLine("incident contention replay: scoped=" + scoped + ";custom=" + custom + ";initial=" + description.InitialStep
                + ";final=" + description.FinalStep + ";commands=" + description.Commands + ";checkpoints=" + description.Checkpoints
                + ";live-hash=" + game.Hash().ToString("X16") + ";verified-hash=" + verification.ActualHash.ToString("X16"));
            Console.WriteLine("incident contention metrics: scoped=" + scoped + ";custom=" + custom + ";updates=" + updates + ";episodes=" + generations.Count + ";peak-update-bytes=" + peak + ";total-update-bytes=" + allocated);
        }
    }
}
