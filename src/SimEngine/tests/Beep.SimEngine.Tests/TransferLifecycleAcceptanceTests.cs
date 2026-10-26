using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.PermanentRemovalTests;

namespace Beep.SimEngine.Tests
{
    // Ordinary players only. Original engine work, inventory, receipts, removal and recovery own all rules.
    internal static class TransferLifecycleAcceptanceTests
    {
        internal const string Case = "Transfer lifecycle acceptance: saved pickup delivery cancellation and physical removal conserve stock and position";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static EntityId Entity(SimGame g, string name) => TransformationContentContractTests.Entity(g, name);
        private static void Balance(Check c, SimGame g)
        {
            c.Equal(Fix.FromInt(20), Total(g), "actual finite balance at every observed lifecycle Update");
            foreach (var entity in g.Seat(0).Snapshot.Entities)
                if (g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, Supply(g), out var facts).IsNone)
                    c.That(facts.Stock >= Fix.Zero && facts.InboundPromised >= Fix.Zero && facts.OutboundPromised >= Fix.Zero,
                        "actual inventory/promise facts never negative");
        }
        private static void Tick(Check c, SimGame g, SimGame twin = null)
        { Step(g, twin); Balance(c, g); }
        private static void Until(Check c, SimGame g, SimGame twin, Func<bool> done, string stage)
        {
            var trail = new Queue<string>();
            for (int i = 0; i < 512 && !done(); i++)
            {
                var positions = g.Seat(0).Snapshot.Entities.Where(x => x.Def == "machine" || x.Def == "cargo.container" || x.Def == "lifecycle.b")
                    .Select(x => x.Def + "@" + ((PointSnapshot)x.Spatial).Position);
                trail.Enqueue(g.Clock.StepIndex + ":total=" + Total(g) + ":positions=" + string.Join(",", positions) + ":jobs=" +
                    string.Join(";", Jobs(g).Select(x => x.Id + "/" + x.State + "/" + x.Worker + "/" + x.ToilIndex + "/" + x.Loaded + "/" + x.Blocker)));
                if (trail.Count > 12) trail.Dequeue(); Tick(c, g, twin);
                if ((i + 1) % 128 == 0) Console.WriteLine("stage wait: " + stage + " Updates=" + (i + 1) + " " + trail.Last());
            }
            if (!done()) throw new InvalidOperationException(stage + " exceeds512Updates " + string.Join(" | ", trail));
            Console.WriteLine("stage complete: " + stage + " at=" + g.Clock.StepIndex);
        }
        private static CargoDispositionSnapshot Cargo(Check c, SimGame g, EntityId worker)
        {
            ulong before = g.Hash(); c.Equal(Code.None, g.Seat(0).View.Feature<LogisticsView>().ReadCargoDisposition(worker, out var facts), "actual cargo owner view");
            c.Equal(before, g.Hash(), "cargo-owner observation pure"); return facts;
        }
        private static long Activity(Check c, SimGame g, string activity, EntityId target, params string[] nodes)
            => TransporterRetirementTests.Activity(c, g, activity, target, nodes);
        private static int Haul(Check c, SimGame g, EntityId worker, int amount)
            => checked((int)Submit(c, g, new HaulResourcesCommand { Source = Entity(g, "source"), Destination = Entity(g, "destination"),
                Worker = worker, Resource = Supply(g), Ceiling = Fix.FromInt(amount), Priority = 100 }).Arg0);
        private static void Scenario(Check c, bool half, bool bound, bool delivery)
        {
            Console.WriteLine("transfer variant: half=" + half + " bound=" + bound + " delivery=" + delivery);
            var f = TransporterRetirementTests.Compose(half, bound); var g = ConstructionPlanTests.NewPoints(f);
            var idle = ConstructionPlanTests.NewPoints(f); ulong isolation = idle.Hash();
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "transfer/removal is physical engine work, not mandatory business");
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            EntityId carrier = Entity(g, "machine"), cargoOwner = Entity(g, bound ? "cargo.container" : "machine"),
                source = Entity(g, "source"), destination = Entity(g, "destination"), actor = Entity(g, "lifecycle.b");
            Policy(c, g, "machine", true); Policy(c, g, "lifecycle.b", true, true);
            Submit(c, g, new SetHomeCommand { Worker = actor, Home = Entity(g, "lifecycle.home") }); Balance(c, g);
            int first = Haul(c, g, carrier, 4); c.That(Facts(g, cargoOwner).Stock.IsZero && Facts(g, destination).Stock.IsZero, "normal haul does not instantly load/deliver");
            var twin = Save(c, f, g);
            Until(c, g, twin, () => Job(g, first).Loaded.IsPositive && Job(g, first).ToilIndex == 1, "normal actual pickup");
            c.That(At(g, carrier) == Fix.One && Job(g, first).SourceDockSlot == new Code("retire.service.a"), "normal pickup reaches actual authored source service slot at1"); twin = Save(c, f, g);
            Until(c, g, twin, () => Facts(g, destination).Stock.IsPositive, "normal physical delivery prefix");
            c.That(At(g, carrier) == Fix.FromInt(11) && Job(g, first).DestinationDockSlot == new Code("retire.service.a"), "normal delivery reaches actual authored destination service slot at11"); twin = Save(c, f, g);
            Until(c, g, twin, () => Facts(g, destination).Stock == Fix.FromInt(4) && Facts(g, cargoOwner).Stock.IsZero && !Jobs(g).Any(x => x.Id == first), "normal whole transfer");
            c.Equal(Fix.FromInt(16), Facts(g, source).Stock, "normal exact finite source debit");
            c.That(Facts(g, source).OutboundPromised.IsZero && Facts(g, destination).InboundPromised.IsZero, "normal receipt release");

            int haul = Haul(c, g, carrier, 8); twin = Save(c, f, g);
            Until(c, g, twin, () => Job(g, haul).Loaded.IsPositive && Job(g, haul).Loaded < Job(g, haul).Amount && Job(g, haul).ToilIndex == (delivery ? 3 : 1), "second actual partial pickup/delivery");
            Policy(c, g, "machine", false); var prefix = Job(g, haul); var retained = Cargo(c, g, carrier);
            EntityId endpoint = delivery ? destination : source;
            Fix cargo = Facts(g, cargoOwner).Stock, endpointStock = Facts(g, endpoint).Stock, endpointPosition = At(g, endpoint),
                position = At(g, carrier), ownerPosition = At(g, cargoOwner), sourceStock = Facts(g, source).Stock, deliveredStock = Facts(g, destination).Stock;
            c.That(cargo.IsPositive && prefix.Loaded == cargo && retained.Endpoint == ResourceEndpointId.ForEntity(cargoOwner), "partial stock is actual bound/native inventory, not carrier shadow");
            c.That(position == Fix.FromInt(delivery ? 11 : 1) && prefix.HandlingProgress.WholeTicks > 0 &&
                (delivery ? prefix.DestinationDockSlot : prefix.SourceDockSlot) == new Code("retire.service.a"), "partial handling earned at actual selected endpoint service slot");
            if (bound) c.That(Facts(g, carrier).Stock.IsZero && ownerPosition == Fix.FromInt(75), "independent cargo stays with actual separate inventory binding");
            long retirement = Activity(c, g, delivery ? "retire.destination-plan" : "retire.source-plan", endpoint, "pack", "retire"); twin = Save(c, f, g);
            Until(c, g, twin, () => Plan(g, retirement).Order > 0 && Node(g, Plan(g, retirement).Order, "pack").Blocker == ResourceTransactions.ActivePromises, "live endpoint promise prevents packing/despawn");
            c.That(Exists(g, endpoint) && Facts(g, endpoint).Stock == endpointStock && Facts(g, cargoOwner).Stock == cargo && At(g, carrier) == position, "removal wait retains actual endpoint/cargo/physical position");
            c.Equal(prefix.HandlingProgress, Job(g, haul).HandlingProgress, "blocked endpoint removal retains original partial handling");
            // Pause the independently earned packing effect before releasing its blocking haul claim.
            // Otherwise that legitimate other activity may commit stock disposition in the same Update.
            Submit(c, g, new ConfigurePlanCommand { Plan = retirement, Group = "carrier.retirement", Priority = 100, Paused = true }); twin = Save(c, f, g);
            var cancel = new SetWorkerHeldCommand { Worker = carrier, Held = true }; c.Equal(Code.None, g.Seat(0).Preview(cancel), "ordinary interruption preview");
            int sequence = g.Seat(0).Submit(cancel); twin = Save(c, f, g); g.Clock.Pause(); twin.Clock.Pause();
            ulong paused = g.Hash(); long clock = g.Clock.StepIndex; Tick(c, g, twin);
            c.That(g.Hash() == paused && g.Clock.StepIndex == clock && Job(g, haul).Loaded == cargo && Exists(g, endpoint), "saved pending cancellation waits under one paused game clock");
            g.Clock.Resume(); twin.Clock.Resume(); Tick(c, g, twin);
            long cursor = 0; var answers = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, answers);
            c.That(answers.Single(x => x.Sequence == sequence).Accepted && !Jobs(g).Any(x => x.Id == haul), "actual saved cancellation clears only old job");
            c.That(Facts(g, source).Stock == sourceStock && Facts(g, destination).Stock == deliveredStock && Facts(g, cargoOwner).Stock == cargo && At(g, carrier) == position,
                "cancellation preserves committed stock/position source=" + Facts(g, source).Stock + "/" + sourceStock +
                " destination=" + Facts(g, destination).Stock + "/" + deliveredStock + " cargo=" + Facts(g, cargoOwner).Stock + "/" + cargo + " position=" + At(g, carrier) + "/" + position);
            c.That(Facts(g, source).OutboundPromised.IsZero && Facts(g, destination).InboundPromised.IsZero && Facts(g, cargoOwner).InboundPromised.IsZero && Facts(g, cargoOwner).OutboundPromised.IsZero,
                "exact old source/destination/cargo promises released, not stock");
            Submit(c, g, new ConfigurePlanCommand { Plan = retirement, Group = "carrier.retirement", Priority = 100, Paused = false }); twin = Save(c, f, g);
            Until(c, g, twin, () => Node(g, Plan(g, retirement).Order, "pack").Progress.WholeTicks > 0, "earned physical endpoint packing prefix"); twin = Save(c, f, g);
            Until(c, g, twin, () => Plan(g, retirement).State == PlanState.Done, "physical endpoint stock packing and removal");
            c.That(!Exists(g, endpoint) && At(g, carrier) == position && At(g, cargoOwner) == ownerPosition && Facts(g, cargoOwner).Stock == cargo, "endpoint removed without cargo loss/position rebinding");
            Fix packed = Fix.Zero; int containers = 0;
            foreach (var entity in g.Seat(0).Snapshot.Entities)
            {
                if (entity.Id == cargoOwner || entity.Def == "source" || entity.Def == "destination" || entity.Def == "retire.receiver") continue;
                if (g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, Supply(g), out var facts).IsNone && facts.Stock.IsPositive)
                { packed += facts.Stock; containers++; c.Equal(endpointPosition, ((PointSnapshot)entity.Spatial).Position, "endpoint disposition uses actual removed inventory placement"); }
            }
            c.That(containers > 0 && packed == endpointStock, "removed endpoint stock packed once, not gifted or discarded");
            c.Equal(Codes.NotFound, g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(endpoint, Supply(g), out _), "removed stable endpoint stays missing");

            long loaded = Activity(c, g, "carrier.retire", carrier, "retire"); twin = Save(c, f, g);
            Until(c, g, twin, () => Plan(g, loaded).Order > 0 && Work(g).PreviewOrderEffect(Plan(g, loaded).Order) == ResourceTransactions.UndisposedStock, "actual loaded-carrier removal refusal");
            c.That(Exists(g, carrier) && Facts(g, cargoOwner).Stock == cargo && At(g, carrier) == position, "carrier cannot despawn retained native/separate cargo");
            twin = Save(c, f, g); Submit(c, g, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { loaded }) }); twin = Save(c, f, g);
            Until(c, g, twin, () => At(g, actor) == Fix.FromInt(70), "service actor clears real route to authored home");
            EntityId receiver = Entity(g, "retire.receiver");
            Submit(c, g, new ConfigureStockPolicyCommand { Store = receiver, Resource = Supply(g),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, cargo, cargo, cargo, 90, LogisticsReserveAccess.Protected)) });
            Policy(c, g, "machine", true); Submit(c, g, new SetWorkerHeldCommand { Worker = carrier, Held = false }); twin = Save(c, f, g);
            Until(c, g, twin, () => Jobs(g).Any(x => x.RecoveryKind == LogisticsRecoveryKind.CargoReturn && x.Worker == carrier), "original residual recovery binds actual carrier");
            var returning = Jobs(g).Single(x => x.RecoveryKind == LogisticsRecoveryKind.CargoReturn && x.Worker == carrier);
            c.That(returning.Amount == cargo && returning.Loaded == cargo && returning.SourceEndpoint == ResourceEndpointId.ForEntity(cargoOwner), "actual inventory source differs from carrier origin only when authored");
            twin = Save(c, f, g);
            Until(c, g, twin, () => Facts(g, receiver).Stock == cargo && Facts(g, cargoOwner).Stock.IsZero && !Jobs(g).Any(x => x.IsHaul), "once-only physical residual return after endpoint loss");
            c.Equal(Fix.FromInt(20), At(g, carrier), "actual return reaches receiver geometry without a host fallback");
            c.That(Facts(g, receiver).InboundPromised.IsZero && Facts(g, cargoOwner).OutboundPromised.IsZero, "residual delivery releases actual receipts");
            long empty = Activity(c, g, "carrier.retire", carrier, "retire"); twin = Save(c, f, g);
            Until(c, g, twin, () => Plan(g, empty).Order > 0 && Node(g, Plan(g, empty).Order, "retire").Progress.WholeTicks > 0, "earned empty-carrier physical retirement prefix");
            c.That(Exists(g, carrier) && At(g, actor) == At(g, carrier) + Fix.One, "removal worker actually reaches carrier service before final effect"); twin = Save(c, f, g);
            Until(c, g, twin, () => Plan(g, empty).State == PlanState.Done, "original once-only empty-carrier removal");
            c.That(!Exists(g, carrier) && !Exists(g, endpoint), "actual stable carrier/endpoint both removed after accounted disposition");
            if (bound) c.That(Exists(g, cargoOwner) && Facts(g, cargoOwner).Stock.IsZero && At(g, cargoOwner) == ownerPosition, "separate empty inventory remains its original entity/placement");
            for (int i = 0; i < 3; i++) Tick(c, g, twin);
            c.That(prefix.Loaded == cargo && prefix.HandlingProgress.WholeTicks > 0 && retained.HasResidualCargo, "retained partial job/cargo facts immutable after real removal");
            var hidden = new List<PlanView>(); Work(g, 1).CopyPlans(hidden); c.Equal(0, hidden.Count, "all cancellation/removal history company private");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "terminal accounting/removed references/position save identity"); c.Equal(isolation, idle.Hash(), "interleaved transfer/removal session isolation");
            var replay = new ReplayVerifier().Verify(f, recording.Finish(), limits);
            c.That(replay.Success && replay.Game.Hash() == g.Hash(), "public transfer/cancel/physical removal replay: " + replay.Failure + "/" + replay.Detail);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current literal transfer lifecycle identity");
            foreach (bool half in new[] { false, true }) foreach (bool bound in new[] { false, true })
                foreach (bool delivery in new[] { false, true }) Scenario(c, half, bound, delivery);
        }
    }
}
