using System;
using System.Collections.Generic;
using System.IO;
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

namespace Beep.SimEngine.Tests
{
    internal static partial class CapacityLogisticsStressTests
    {
        internal const string Case = "Capacity logistics stress: authored concurrent fleet budgets and physical conservation";
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add("Navigation diagonal costs: rounded authored edges retain optimal deterministic routes", RoundedRouteCosts);
            TestRunner.Add("Navigation nested queries: original router isolates active profile scratch and saved sessions", NestedRouteQueries);
            TestRunner.Add("Navigation goal ties: equal cost frontier remains optimal deterministic and bounded", GoalDirectedTies);
            TestRunner.Add("Guarded byte layers: actual supplied hauling update costs saved twins and replay", ByteLayerSupply);
            TestRunner.Add(DockNativeCase, c => DockContention(c, true, false));
            TestRunner.Add(DockIndependentCase, c => DockContention(c, false, true));
        }
        internal sealed class Profile
        {
            internal readonly int[] Carriers;
            internal readonly int Destinations, Rounds, Quantity, MaxUpdates, SampleEvery, CarrierSpeed;
            internal readonly long PerUpdateBudget, TotalBudget;
            internal Profile()
            {
                using var stream = typeof(Program).Assembly.GetManifestResourceStream("tests.capacity-logistics-stress-profile.json")
                    ?? throw new InvalidOperationException("missing explicit stress profile");
                using var text = new StreamReader(stream); var r = new JsonReader(Json.Parse(text.ReadToEnd()), "stress.profile");
                if (r.Int("schema") != 1 || r.String("id") != "capacity.logistics.concurrent") throw r.Error("stress profile identity");
                var carriers = r.Value("carriers"); Carriers = new int[carriers.Count];
                for (int i = 0; i < Carriers.Length; i++) Carriers[i] = int.Parse(carriers[i].Text, System.Globalization.CultureInfo.InvariantCulture);
                Destinations = r.Int("destinations"); Rounds = r.Int("rounds"); Quantity = r.Int("haul_quantity");
                MaxUpdates = r.Int("max_updates_per_round"); SampleEvery = r.Int("sample_every_updates");
                CarrierSpeed = r.Int("carrier_tiles_per_hour");
                PerUpdateBudget = r.Long("max_allocated_bytes_per_update"); TotalBudget = r.Long("max_total_update_allocated_bytes"); r.End();
                if (Carriers.Length == 0 || Carriers.Any(x => x <= 0) || Destinations <= 0 || Rounds <= 0 || Quantity <= 0 || MaxUpdates <= 0 ||
                    SampleEvery <= 0 || CarrierSpeed <= 0 || PerUpdateBudget <= 0 || TotalBudget <= 0) throw new InvalidOperationException("positive authored stress bounds required");
            }
        }
        private static GameFactory Factory(Profile profile, int carriers, bool native, bool half, JsonValue observation = null, Action<JsonValue> configure = null)
        {
            var root = Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            var source = root.Get("entities")[0].Clone();
            var destination = root.Get("entities")[1].Clone(); var worker = root.Get("entities")[2].Clone();
            root.Remove("point_spaces");
            root.Set("tile_spaces", Json.Parse("[{\"id\":\"stress.tiles\",\"width\":72,\"height\":" + (carriers + 18) + "}]"));
            root.Set("uniform_tile_navigation", Json.Parse("[{\"id\":\"stress.route\",\"space\":\"stress.tiles\",\"cost\":10}]"));
            root.Get("scenarios")[0].Set("space", JsonValue.Of("stress.tiles"));
            var scenarioModules = root.Get("scenarios")[0].Get("modules"); scenarioModules.Add(JsonValue.Of(TileNavigationModule.ModuleId));
            Presence(source, false); Presence(destination, false); Presence(worker, true);
            worker.Get("components").Remove("point_motion");
            worker.Get("components").Set("mover", Json.Parse("{\"classes\":[\"stress.route\"],\"class\":\"stress.route\",\"speed\":" + profile.CarrierSpeed + ",\"states\":{\"moving\":\"travel\",\"stopped\":\"blocked\"}}"));
            int opening = checked(carriers * profile.Rounds * profile.Quantity);
            source.Get("components").Get("point_stock").Set("capacity_policy", JsonValue.Of("fixture.count.1000"));
            source.Get("components").Get("point_stock").Set("initial", JsonValue.Number(opening.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            destination.Get("components").Get("point_stock").Set("capacity_policy", JsonValue.Of("fixture.count.1000"));
            if (native) { Storage(source, opening); Storage(destination, 0); }
            var entities = JsonValue.NewArray(); entities.Add(source);
            var setups = JsonValue.NewArray(); var entries = JsonValue.NewArray();
            AddSetup("source", 8, 16);
            for (int i = 0; i < profile.Destinations; i++)
            {
                var def = destination.Clone(); def.Set("id", JsonValue.Of("stress.destination." + i));
                entities.Add(def); AddSetup("stress.destination." + i, 28 + 12 * i, 16);
            }
            for (int i = 0; i < carriers; i++)
            {
                var def = worker.Clone(); def.Set("id", JsonValue.Of("stress.carrier." + i)); entities.Add(def); AddSetup("stress.carrier." + i, 2, 16 + i);
            }
            root.Set("entities", entities); root.Set("entity_setups", setups); root.Get("world_setup_profiles")[0].Set("entries", entries);
            configure?.Invoke(root);
            if (observation != null)
            {
                scenarioModules.Add(JsonValue.Of("proof.navigation-observation"));
                return PointGame.Factory(definitions: root.ToString(), additionalModule: new NavigationObservationModule(),
                    moduleDeclaration: observation.Get("module").ToString(), moduleDocument: observation.Get("content").ToString());
            }
            return PointGame.Factory(definitions: root.ToString());

            void Storage(JsonValue entity, int stock)
            {
                var components = entity.Get("components"); components.Remove("point_stock");
                components.Set("storage", Json.Parse("{\"capacity_policy\":\"fixture.count.1000\",\"initial_stock\":" + (stock == 0 ? "{}" : "{\"supply\":" + stock + "}") + ",\"public\":false}"));
            }
            void Presence(JsonValue entity, bool mobile)
            {
                var parts = entity.Get("components"); parts.Remove("point_presence");
                parts.Set("tile_presence", Json.Parse("{\"space\":\"stress.tiles\",\"width\":1,\"height\":" + (mobile ? 1 : carriers) + ",\"blocks\":true,\"movable\":" + (mobile ? "true" : "false") + "}"));
            }
            void AddSetup(string name, int x, int y)
            {
                setups.Add(Json.Parse("{\"id\":\"" + name + "\",\"after\":[],\"entity\":\"" + name + "\",\"company\":0,\"placement\":{\"space\":\"stress.tiles\",\"x\":" + x + ",\"y\":" + y + "},\"home\":null}"));
                entries.Add(JsonValue.Of(name));
            }
        }
        private static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest
        { Scenario = "points", Seed = 9621, Seats = { Controller.Human, Controller.Human } });
        private static List<WorkJobView> Jobs(SimGame g)
        { var jobs = new List<WorkJobView>(); g.Seat(0).View.Feature<WorkView>().CopyJobs(jobs); return jobs; }
        private static void Accounting(Check c, SimGame g, Fix opening, ResourceId resource)
        {
            Fix total = Fix.Zero;
            foreach (var entity in g.Seat(0).Snapshot.Entities)
            {
                c.Equal(Code.None, g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, resource, out var facts), "actual native/external endpoint diagnostic admitted");
                total += facts.Stock;
                c.That(facts.Stock >= Fix.Zero && facts.OutboundPromised >= Fix.Zero && facts.InboundPromised >= Fix.Zero && facts.OutboundPromised <= facts.Stock,
                    "concurrent stock and outbound promises stay physically owned/nonnegative");
            }
            c.Equal(opening, total, "source/cargo/destination conservation under concurrent physical work");
            var active = Jobs(g).Where(x => x.State == JobState.Assigned).ToArray();
            c.Equal(active.Length, active.Select(x => x.Worker).Distinct().Count(), "active physical hauls never share a reserved carrier");
            var hidden = new List<WorkJobView>(); g.Seat(1).View.Feature<WorkView>().CopyJobs(hidden); c.Equal(0, hidden.Count, "foreign company cannot see stress activity");
        }
        private static void Stress(Check c, Profile p, int carriers, bool native, bool half)
        {
            Console.WriteLine("stress start: carriers=" + carriers + ";native=" + native + ";half=" + half);
            var f = Factory(p, carriers, native, half); var g = New(f); var idle = New(f); ulong idleHash = idle.Hash();
            var source = Entity(g, "source"); var workerIds = Enumerable.Range(0, carriers).Select(i => Entity(g, "stress.carrier." + i)).ToArray();
            var targets = Enumerable.Range(0, p.Destinations).Select(i => Entity(g, "stress.destination." + i)).ToArray();
            if (!g.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("explicit stress resource unavailable");
            Fix opening = Fix.FromInt(checked(carriers * p.Rounds * p.Quantity));
            long peak = 0, totalAllocated = 0; int updates = 0;
            var seenJobs = new HashSet<int>();
            var limits = new ReplayLimits(32 * 1024 * 1024, p.MaxUpdates * p.Rounds); using var recording = g.RecordReplay(limits);
            for (int round = 0; round < p.Rounds; round++)
            {
                var accepted = new List<int>(); var competing = new List<int>(); long before = g.Clock.StepIndex;
                Fix beforeStock = PermanentRemovalTests.Facts(g, source).Stock;
                foreach (int i in Enumerable.Range(0, carriers))
                {
                    var command = Intent(i, round); c.Equal(Code.None, g.Seat(0).Preview(command), "burst preflight uses actual unreserved carrier");
                    accepted.Add(g.Seat(0).Submit(command)); competing.Add(g.Seat(0).Submit(Intent(i, round)));
                }
                c.Equal(before, g.Clock.StepIndex, "queued player intentions do not advance physical time before update");
                c.Equal(beforeStock, PermanentRemovalTests.Facts(g, source).Stock, "queued player intentions consume no stock before update");
                Update();
                long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results);
                foreach (int sequence in accepted) c.That(results.Single(x => x.Sequence == sequence).Accepted, "first ordered intent owns carrier/resources");
                foreach (int sequence in competing) c.That(!results.Single(x => x.Sequence == sequence).Accepted, "same-carrier competing intent refuses without duplicate ownership");
                var roundJobs = Jobs(g);
                c.Equal(carriers, roundJobs.Count, "current generation has one job per carrier after prior completed jobs retire");
                foreach (var job in roundJobs) c.That(seenJobs.Add(job.Id), "each new generation owns a fresh stable job identity");
                Accounting(c, g, opening, resource);
                var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "concurrent reserved/pickup state saves exactly");
                bool done = false;
                for (int i = 0; i < p.MaxUpdates; i++)
                {
                    if (Jobs(g).All(x => x.State == JobState.Done)) { done = true; break; }
                    Update(); twin.Update();
                    if (twin.Fault != null || g.Hash() != twin.Hash()) throw new InvalidOperationException("stress twin differs at=" + g.Clock.StepIndex + " fault=" + twin.Fault?.Message);
                    if (i % p.SampleEvery == 0) Accounting(c, g, opening, resource);
                }
                if (!done) throw new InvalidOperationException("stress round exceeded authored update bound: carriers=" + carriers + ";native=" + native + ";half=" + half +
                    ";round=" + round + ";clock=" + g.Clock.StepIndex + ";source=" + PermanentRemovalTests.Facts(g, source).Stock +
                    ";jobs=" + string.Join(",", Jobs(g).Select(x => x.Id + ":" + x.State + ":" + x.Blocker + ":toil=" + x.ToilIndex + ":loaded=" + x.Loaded +
                        ":handled=" + x.HandledAmount + ":remaining=" + x.HandlingRemaining + ":route=" + x.Route + ":worker=" + x.Worker +
                        ":position=" + ((TileSpatialSnapshot)g.Seat(0).Snapshot.Entities.Single(e => e.Id == x.Worker).Spatial).Footprint.X)));
                Accounting(c, g, opening, resource);
                c.That(workerIds.All(id => ((TileSpatialSnapshot)g.Seat(0).Snapshot.Entities.Single(x => x.Id == id).Spatial).Footprint.X > 2), "every actual carrier moved physically");
                Console.WriteLine("stress round: carriers=" + carriers + ";native=" + native + ";half=" + half + ";round=" + round + ";clock=" + g.Clock.StepIndex);
            }
            c.Equal(Fix.Zero, PermanentRemovalTests.Facts(g, source).Stock, "finite source consumed exactly once across repeated bursts");
            c.That(Jobs(g).All(x => x.State == JobState.Done && x.Loaded == Fix.Zero), "all cargo physically delivered without duplicate jobs or stranded load");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "completed concurrent stress restores original accounting");
            c.Equal(idleHash, idle.Hash(), "interleaved independent stress session unchanged");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "concurrent intent/refusal/physical round stream replays");
            c.That(peak <= p.PerUpdateBudget, "declared per-update managed allocation budget");
            c.That(totalAllocated <= p.TotalBudget, "declared accumulated update allocation budget");
            c.Equal(carriers * p.Rounds, seenJobs.Count, "all accepted generations counted without relying on retired history");
            Console.WriteLine("stress metrics: carriers=" + carriers + ";native=" + native + ";half=" + half + ";updates=" + updates + ";hauls=" + seenJobs.Count +
                ";peak-update-bytes=" + peak + ";total-update-bytes=" + totalAllocated);

            HaulResourcesCommand Intent(int i, int round) => new HaulResourcesCommand { Source = source, Destination = targets[(i + round) % targets.Length],
                Worker = workerIds[i], Resource = resource, Ceiling = Fix.FromInt(p.Quantity), Priority = 10 };
            void Update()
            {
                long start = GC.GetAllocatedBytesForCurrentThread(); g.Update(); long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
                peak = Math.Max(peak, allocated); totalAllocated = checked(totalAllocated + allocated); updates++;
                if (g.Fault != null) throw new InvalidOperationException("stress engine fault at=" + g.Clock.StepIndex + ":" + g.Fault.Message);
            }
        }
        internal static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current actual stress engine identity");
            SearchScratch(c);
            var profile = new Profile();
            foreach (int carriers in profile.Carriers) foreach (bool native in new[] { false, true }) foreach (bool half in new[] { false, true })
                Stress(c, profile, carriers, native, half);
        }
        internal static GameFactory ObservationFactory(string fault, bool raw)
        {
            var json = Json.Parse(FrameworkProofContent.Read("navigation-observation.json"));
            var probe = json.Get("content").Get("navigation_observation_probes")[0];
            probe.Set("fault", JsonValue.Of(fault)); probe.Set("raw", JsonValue.Of(raw));
            return Factory(new Profile(), 8, false, false, json);
        }
        // Pure algorithm proof, not a player/session model: no World, stock, clock or gameplay writes.
        private static void SearchScratch(Check c)
        {
            var map = new TileMap("stress.search-probe", 8, 8);
            var search = new PathSearch(); var path = new List<TileCoord>();
            var calls = new Dictionary<TileCoord, int>(); var from = new TileCoord(0, 0); var goal = new TileRect(7, 0, 1, 1);
            c.Equal(PathSearchResult.Found, search.Find(map, from, goal, Corridor, 10, true, 1000, path), "search keeps original unique corridor route");
            c.That(path.SequenceEqual(Enumerable.Range(1, 7).Select(x => new TileCoord(x, 0))), "cost scratch preserves exact canonical path");
            c.That(calls.Count > 0 && calls.Values.All(x => x == 1), "guarded entry cost evaluated once per visited tile within one search");
            calls.Clear();
            c.Equal(PathSearchResult.Unreachable, search.Find(map, from, goal, Barrier, 10, true, 1000, path), "next search observes changed actual cost callback, not old scratch");
            c.Equal(0, path.Count, "failed search exposes no previous path prefix");
            c.That(calls.Count > 0 && calls.Values.All(x => x == 1), "failed search also uses only current-search costs");
            c.Throws<InvalidOperationException>(() => search.Find(map, from, goal, _ => throw new InvalidOperationException("explicit cost failure"), 10, true, 1000, path), "provider failure is not cached or swallowed");
            calls.Clear();
            c.Equal(PathSearchResult.Found, search.Find(map, from, goal, Corridor, 10, true, 1000, path), "search after failure starts fresh");
            c.That(calls.Values.All(x => x == 1), "scratch retained for allocation reuse never leaks a previous cost");
            c.Equal(PathSearchResult.BudgetExceeded, search.Find(map, from, goal, Corridor, 10, true, 0, path), "existing explicit expansion bound remains a typed pending result");

            int Count(TileCoord tile) { calls.TryGetValue(tile, out int value); calls[tile] = value + 1; return value; }
            int Corridor(TileCoord tile) { Count(tile); return tile.Y == 0 ? 10 : 0; }
            int Barrier(TileCoord tile) { Count(tile); return tile.X == 4 ? 0 : 10; }
        }
    }
}
