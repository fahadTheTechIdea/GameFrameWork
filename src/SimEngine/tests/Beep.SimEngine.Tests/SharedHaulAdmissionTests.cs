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
    // Player commands, immutable facts and bounded Update calls only; original engine owns admission and execution.
    internal static class SharedHaulAdmissionTests
    {
        internal const string Case = "Shared haul admission: public stock carrier dock claims and amount-scaled saved handling";
        private static readonly string[] Machines = { "machine.a", "machine.b", "machine.c", "machine.d" };
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(PriorActivityProofModule proof = null, LogisticsProjectionFaultModule projection = null)
        {
            var modules = new List<GameModule> {
                new ContentAccessModule(), new InventoryModule(), new OperationsModule(), new MotionModule(),
                new WorkModule(), new LogisticsModule(), new TransformationModule(), new WorldSetupModule(), new PointModule(), new TileNavigationModule() };
            if (proof != null) modules.Add(proof);
            if (projection != null) modules.Add(projection);
            var definition = new GameDefinition("shared-haul", modules);
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read(proof == null ? "shared-haul-master.json" : "logistics-view-fault-master.json"))
                .Add("open-access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"))
                .Add("shared-haul.json", FrameworkProofContent.Read("shared-haul.json"));
            if (proof != null) files.Add("logistics-view-fault.json", FrameworkProofContent.Read("logistics-view-fault.json"));
            foreach (string name in new[] { "world", "work", "recipes" })
                files.Add("process-only-" + name + ".json", FrameworkProofContent.Read("process-only-" + name + ".json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory, string scenario = "shared-haul")
        {
            var request = new NewGameRequest { Scenario = scenario, Seed = 912 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static EntityId Entity(SimGame game, string name)
            => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "queue." + name || e.Def == "queue." + name + ".scarce").Id;
        private static TileCoord Position(SimGame game, string name)
            => ((TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == Entity(game, name)).Spatial).Tile;
        private static Fix Stock(SimGame game, string name)
        {
            var facts = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, name), facts);
            return facts.Where(f => f.Key == Storage.FacetStock && f.Subject == "process.input")
                .Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        private static List<WorkJobView> Jobs(SimGame game, int seat = 0)
        {
            var rows = new List<WorkJobView>(); game.Seat(seat).View.Feature<WorkView>().CopyJobs(rows); return rows;
        }
        private static WorkJobView Job(SimGame game, string machine)
            => Jobs(game).Single(j => j.Worker == Entity(game, machine));
        private static IReadOnlyList<ResourceReservation> Reservations(SimGame game, string name)
        {
            if (!game.Seat(0).View.Feature<InventoryView>().TryGetReservations(Entity(game, name), Code.None, out var rows))
                throw new InvalidOperationException("missing actual reservations " + name);
            return rows;
        }
        private static Fix Claims(SimGame game, string name, ReservationDirection direction)
            => Reservations(game, name).Where(r => r.Direction == direction).Aggregate(Fix.Zero, (sum, r) => sum + r.Amount);
        private static HaulResourcesCommand Haul(SimGame game, string machine, int amount)
            => new HaulResourcesCommand { Source = Entity(game, "source"), Destination = Entity(game, "destination"),
                Worker = Entity(game, machine), Resource = Resource(game),
                Ceiling = Fix.FromInt(amount), Priority = 60 };
        private static ResourceId Resource(SimGame game)
        {
            if (!game.Seat(0).View.TryFindResource("process.input", out var resource)) throw new InvalidOperationException("missing visible authored input");
            return resource;
        }
        private static void Integrity(SimGame game, int opening = 32)
        {
            var jobs = Jobs(game);
            if (jobs.Any(j => !j.IsHaul) || jobs.Select(j => j.Worker).Distinct().Count() != jobs.Count)
                throw new InvalidOperationException("carrier double-booking");
            var names = new[] { "source", "destination" }.Concat(Machines);
            Fix total = Fix.Zero;
            foreach (string name in names)
            {
                Fix stock = Stock(game, name); total += stock;
                var rows = Reservations(game, name);
                Fix outgoing = Claims(game, name, ReservationDirection.Outbound), incoming = Claims(game, name, ReservationDirection.Inbound);
                int limit = name == "source" || name == "destination" && opening == 12 ? 40 : name == "destination" ? 24 : 8;
                if (outgoing > stock || stock + incoming > Fix.FromInt(limit) || rows.Any(r => !r.Token.IsValid || !r.Owner.IsValid || !r.Amount.IsPositive)
                    || rows.Select(r => r.Token).Distinct().Count() != rows.Count)
                    throw new InvalidOperationException("actual endpoint overbooked " + name);
            }
            if (total != Fix.FromInt(opening)) throw new InvalidOperationException("physical material conservation " + total);
            foreach (var claims in new[] { jobs.Where(j => j.ToilIndex < 2).Select(j => j.SourceDockSlot),
                jobs.Select(j => j.DestinationDockSlot) })
            {
                var nonempty = claims.Where(s => !s.IsNone).ToList();
                if (nonempty.Distinct().Count() != nonempty.Count) throw new InvalidOperationException("live dock identity double-booked");
            }
            if (Machines.Select(name => Position(game, name)).Distinct().Count() != Machines.Length)
                throw new InvalidOperationException("physical mobile tile overlap");
        }
        private static void Step(SimGame game, SimGame twin = null, int opening = 32)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("saved haul diverged at " + game.Clock.StepIndex + ": " + game.Fault + "/" + twin?.Fault);
            Integrity(game, opening);
        }
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage, int opening = 32)
        {
            for (int i = 0; i < 256 && !ready(); i++) Step(game, twin, opening);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " source=" + Stock(game, "source")
                + " destination=" + Stock(game, "destination") + " jobs=" + string.Join(";", Jobs(game).Select(j =>
                    j.Id + "/" + j.Worker + "/" + j.ToilIndex + "/" + j.Blocker + "/" + j.SourceDockSlot + "/" + j.DestinationDockSlot
                    + "/" + j.Loaded + "/" + j.HandlingProgress.WholeTicks)));
        }
        private static void Policy(Check c, SimGame game, string machine, bool enabled)
        {
            var command = new ConfigureWorkPolicyCommand { Worker = Entity(game, machine),
                Settings = new WorkPolicySettings(enabled, 1, new[] { new WorkerRoleId("haul") }, null, true, false, WorkAssignmentMode.All) };
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal worker policy admission");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary policy command");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "unchanged engine runtime identity");
            var factory = Factory(); var game = New(factory);
            c.That(game.Seat(0).Snapshot.Entities.Count == 6 && Stock(game, "source") == Fix.FromInt(32), "explicit queue profile initializes only real queue actors");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000); var replay = game.RecordReplay(limits);
            ulong hash = game.Hash();
            foreach (var item in new[] { (Name: "machine.a", Amount: 4), (Name: "machine.b", Amount: 8), (Name: "machine.c", Amount: 8) })
            {
                var command = Haul(game, item.Name, item.Amount);
                c.Equal(Code.None, game.Seat(0).Preview(command), "simultaneous player intent has no hidden reservations before commit");
                game.Seat(0).Submit(command);
            }
            c.That(Stock(game, "source") == Fix.FromInt(32) && Jobs(game).Count == 0
                && Reservations(game, "source").Count == 0 && Reservations(game, "destination").Count == 0, "pending inbox changes recorded input state but creates no stock/job/claim prefix before Update");
            var twin = factory.Load(game.Save()); Step(game, twin);
            c.That(Jobs(game).Count == 3 && Jobs(game).Select(j => j.Worker).Distinct().Count() == 3, "same update acquires three distinct exact carriers");
            c.Equal(Fix.FromInt(20), Claims(game, "destination", ReservationDirection.Inbound), "one native destination book holds exact total promised shipment");
            c.That(Jobs(game).Where(j => j.ToilIndex < 2).Select(j => j.SourceDockSlot).Distinct().Count() == 3, "one bay and two waiting claims, no dock double-booking");
            hash = game.Hash(); Code assigned = game.Seat(0).Preview(Haul(game, "machine.a", 4));
            c.That(!assigned.IsNone && game.Hash() == hash, "already acquired carrier cannot take a second shipment");
            var duplicate = TestKit.Order(game, 0, Haul(game, "machine.a", 4));
            c.That(!duplicate.Accepted && duplicate.Refusal == assigned && Jobs(game).Count == 3, "carrier contention refuses with no job/reservation prefix");
            twin = factory.Load(game.Save());
            var retained = Jobs(game); var retainedReceipts = Reservations(game, "destination");
            hash = game.Hash(); var extra = Haul(game, "machine.d", 4);
            Code blocked = game.Seat(0).Preview(extra);
            c.That(!blocked.IsNone, "fourth carrier cannot overbook finite authored dock places");
            c.Equal(hash, game.Hash(), "blocked preview leaves every original stock/job/claim untouched");
            var refusal = TestKit.Order(game, 0, extra);
            c.That(!refusal.Accepted && refusal.Refusal == blocked && Jobs(game).Count == 3, "refused command agrees with pure preview and creates no haul prefix");
            twin = factory.Load(game.Save());
            Reach(game, twin, () => Job(game, "machine.a").Loaded > Fix.Zero && Job(game, "machine.a").Loaded < Fix.FromInt(4), "partial loading");
            c.Equal(4L, Job(game, "machine.a").HandlingRequiredTicks.Value, "four units require four authored loading steps");
            c.Equal(8L, Job(game, "machine.b").HandlingRequiredTicks.Value, "eight units require eight authored loading steps");
            c.That(Job(game, "machine.b").Loaded == Fix.Zero && Job(game, "machine.c").Loaded == Fix.Zero
                && Job(game, "machine.b").DockWaiting && Job(game, "machine.c").DockWaiting, "waiting carriers earn no load while bay is physically held");
            Policy(c, game, "machine.a", false); twin = factory.Load(game.Save());
            var paused = Job(game, "machine.a"); Fix partial = Stock(game, "machine.a");
            for (int i = 0; i < 4; i++) Step(game, twin);
            c.That(Job(game, "machine.a").HandlingProgress.Equals(paused.HandlingProgress) && Stock(game, "machine.a") == partial
                && Job(game, "machine.b").Loaded == Fix.Zero, "disabled acquired service retains partial cargo and bay, no waiting worker steals it");
            hash = game.Hash(); long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(game.Clock.StepIndex == at && game.Hash() == hash, "sole game clock pauses all loaded/queued physical work");
            game.Clock.Resume(); twin.Clock.Resume(); Policy(c, game, "machine.a", true); twin = factory.Load(game.Save());
            Reach(game, twin, () => Jobs(game).Any(j => j.ToilIndex == 3 && j.Amount == Fix.FromInt(4)
                && j.HandlingProgress.WholeTicks > 0 && j.Loaded > Fix.Zero), "partial unloading");
            var shortUnload = Jobs(game).First(j => j.ToilIndex == 3 && j.Amount == Fix.FromInt(4));
            c.Equal(8L, shortUnload.HandlingRequiredTicks.Value, "four units require eight authored unloading steps");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial delivery restores original remaining cargo and destination receipt");
            Reach(game, twin, () => game.Seat(0).Preview(Haul(game, "machine.d", 4)).IsNone, "actual source and destination dock place release");
            extra = Haul(game, "machine.d", 4);
            c.Equal(Code.None, game.Seat(0).Preview(extra), "fourth carrier becomes admissible through physical dock release, not instant fallback");
            c.That(TestKit.Order(game, 0, extra).Accepted, "released shared source accepts next normal haul");
            twin = factory.Load(game.Save());
            c.Equal(Fix.FromInt(24), Claims(game, "destination", ReservationDirection.Inbound) + Stock(game, "destination"), "actual shared receive capacity fully promised, never oversubscribed");
            hash = game.Hash();
            Code full = game.Seat(0).Preview(Haul(game, "machine.a", 8));
            c.That(!full.IsNone && game.Hash() == hash, "full shared destination refuses another load without creating claims");
            Reach(game, twin, () => Jobs(game).Any(j => j.ToilIndex == 3 && j.Amount == Fix.FromInt(8)
                && j.HandlingProgress.WholeTicks > 0 && j.Loaded > Fix.Zero), "larger partial unloading");
            c.Equal(16L, Jobs(game).First(j => j.ToilIndex == 3 && j.Amount == Fix.FromInt(8)).HandlingRequiredTicks.Value, "eight units require sixteen authored unloading steps");
            twin = factory.Load(game.Save());
            Reach(game, twin, () => Jobs(game).Count == 0, "all physical departures complete");
            c.That(Stock(game, "source") == Fix.FromInt(8) && Stock(game, "destination") == Fix.FromInt(24)
                && Machines.All(m => Stock(game, m) == Fix.Zero), "four physical shipments deliver exactly24 once, no lost or created stock");
            c.That(new[] { "source", "destination" }.Concat(Machines).All(m => Reservations(game, m).Count == 0), "terminal endpoint books release all actual promises");
            c.That(retained.Count == 3 && retainedReceipts.Count == 3 && retainedReceipts.Sum(r => r.Amount.Raw) == Fix.FromInt(20).Raw, "retained jobs/receipts are detached, no borrowed live queue state");
            c.Equal(0, Jobs(game, 1).Count, "other company never sees private hauling jobs");
            c.That(!game.Seat(1).View.Feature<InventoryView>().TryGetReservations(Entity(game, "source"), Code.None, out var foreign)
                && foreign.Count == 0, "other company never sees private source reservations");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal native queue state saves");
            c.That(new ReplayVerifier().Verify(factory, replay.Finish(), limits).Success, "ordinary concurrent haul/refusal/policy/clock inputs replay deterministically");
            var isolated = New(factory); for (int i = 0; i < 3; i++) { Step(game, twin); Step(isolated); }
            c.That(Stock(isolated, "source") == Fix.FromInt(32) && Stock(isolated, "destination") == Fix.Zero && Jobs(isolated).Count == 0,
                "interleaved new session has no borrowed stock/reservations/carriers/dock queue");
            var scarce = New(factory, "shared-haul-scarce");
            var scarceReplay = scarce.RecordReplay(limits);
            foreach (string machine in new[] { "machine.a", "machine.b", "machine.c" })
            {
                var intent = Haul(scarce, machine, 8);
                c.Equal(Code.None, scarce.Seat(0).Preview(intent), "uncommitted player inputs share the real12-unit source without speculative booking");
                scarce.Seat(0).Submit(intent);
            }
            var scarceTwin = factory.Load(scarce.Save()); Step(scarce, scarceTwin, 12);
            long cursor = 0; var answers = new List<CommandResult>(); scarce.Seat(0).ReadResults(ref cursor, answers);
            c.That(answers.Count == 3 && answers.Count(r => r.Accepted) == 2 && answers.Count(r => !r.Accepted) == 1,
                "authoritative saved inbox grants first8, then remaining4, refuses third instead of overbooking shared stock");
            c.That(Jobs(scarce).Select(j => j.Amount).OrderBy(n => n).SequenceEqual(new[] { Fix.FromInt(4), Fix.FromInt(8) })
                && Claims(scarce, "destination", ReservationDirection.Inbound) == Fix.FromInt(12), "actual partial admission and endpoint promises total exact available12");
            c.That(Jobs(scarce).Count == 2 && Jobs(scarce).All(j => j.Worker != Entity(scarce, "machine.c"))
                && Reservations(scarce, "machine.c").Count == 0, "refused claimant leaves carrier/dock/resource ownership untouched");
            scarceTwin = factory.Load(scarce.Save());
            Reach(scarce, scarceTwin, () => Jobs(scarce).Count == 0, "scarce physical completion", 12);
            c.That(Stock(scarce, "source") == Fix.Zero && Stock(scarce, "destination") == Fix.FromInt(12)
                && Machines.All(m => Stock(scarce, m) == Fix.Zero && Reservations(scarce, m).Count == 0), "scarce shared source depletes only through earned physical pickup/delivery");
            c.That(new ReplayVerifier().Verify(factory, scarceReplay.Finish(), limits).Success, "same-step stock contention and exact partial shipment replay deterministically");
        }
    }
}
