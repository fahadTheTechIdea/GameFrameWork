using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Player commands and detached facts only; no stock, receipt, topology or recovery writes.
    internal static class TileCargoRecoveryContractTests
    {
        internal const string Case = "Tile cargo recovery contracts: independent geometry and authored residual return";
        internal const string NativeCase = "Tile cargo recovery contracts: native geometry and authored residual return";
        internal static void Register()
        {
            TestRunner.Add(Case,c => Run(c,false));
            TestRunner.Add(NativeCase,c => Run(c,true));
        }
        private static EntityId Id(SimGame game,string def)
            => game.Seat(0).Snapshot.Entities.Single(e => e.Def == def).Id;
        private static Fix Stock(SimGame game,EntityId id)
        {
            var rows = new List<Facet>(); game.Seat(0).View.Inspect(id,rows);
            return rows.Where(f => f.Key == Storage.FacetStock && f.Subject == "steel")
                .Aggregate(Fix.Zero,(sum,f) => sum + f.Value);
        }
        private static Fix PoolStock(SimGame game)
        {
            var rows = new List<NetworkPoolView>();
            Code refusal = game.Seat(0).View.Feature<InfrastructureView>().CopyPools("sensor.network",rows);
            if (!refusal.IsNone) throw new InvalidOperationException("pool facts refused " + refusal);
            return rows.SelectMany(p => p.Stock).Aggregate(Fix.Zero,(sum,r) => sum + r.Amount);
        }
        private static List<WorkJobView> Jobs(SimGame game)
        {
            var rows = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(rows); return rows;
        }
        private static void Step(Check c,SimGame game,SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("cargo recovery fault " + game.Fault + "/" + twin?.Fault);
            Fix total = PoolStock(game);
            foreach (var e in game.Seat(0).Snapshot.Entities) total += Stock(game,e.Id);
            c.Equal(Fix.FromInt(18),total,"physical flow/load/cancel/return conserves all authored material");
            if (twin != null) c.Equal(game.Hash(),twin.Hash(),"saved original cargo/claims/policies continue identically");
        }
        private static void Until(Check c,SimGame game,SimGame twin,Func<bool> ready,string stage)
        {
            for (int i=0; i<240 && !ready(); i++) Step(c,game,twin);
            if (!ready()) throw new InvalidOperationException(stage + " clock=" + game.Clock.StepIndex + " jobs=" +
                string.Join(";",Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker)));
        }
        private static CommandResult Send(Check c,SimGame game,Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None,game.Seat(0).Preview(command),"ordinary command admission");
            c.Equal(hash,game.Hash(),"preview owns no recovery or physical state");
            var result = TestKit.Order(game,0,command); c.That(result.Accepted,"ordinary command commits " + result.Refusal);
            if (!result.Accepted) throw new InvalidOperationException("command refused " + result.Refusal);
            return result;
        }
        private static SimGame Save(Check c,GameFactory factory,SimGame game)
        {
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(),twin.Hash(),"existing state codecs restore exact recovery state"); return twin;
        }
        private static void Run(Check c,bool native)
        {
            foreach (bool independent in new[] { false,true }) Physical(c,native,independent);
        }
        private static void Physical(Check c,bool native,bool independent)
        {
            var factory = IndependentTileWorkTests.Factory(nativeTiles:native,recoverCargo:true,independentRecovery:independent);
            var request = new NewGameRequest { Scenario="independent-sensor",Seed=725,Seats={Controller.Human,Controller.Human} };
            var game = factory.NewGame(request); var isolated = factory.NewGame(request); ulong idle = isolated.Hash();
            c.That(!game.Modules.Contains("sim.tycoon"),"physical recovery needs no business module");
            var policy = game.World.Services.Get<ILogisticsRecoveryPolicy>();
            c.That((policy is IndependentRecoveryProfileDef) == independent &&
                ReferenceEquals(policy,game.World.Content.Find<ILogisticsRecoveryPolicy>(policy.Id)),"selected native/direct policy is actual contract metadata");
            var limits = new ReplayLimits(4*1024*1024,6000); using var recording = game.RecordReplay(limits);
            Until(c,game,null,() => PoolStock(game) == Fix.FromInt(6),"rate-limited authored feed");
            var worker = Id(game,"sensor.cart"); var depot = Id(game,"sensor.depot");
            if (!game.Seat(0).View.TryFindResource("steel",out var steel)) throw new InvalidOperationException("missing steel");
            int haul = checked((int)Send(c,game,new HaulResourcesCommand {
                Source=Id(game,"sensor.line-start"),SourcePort=new Code("network-stock"),Destination=depot,
                Worker=worker,Resource=steel,Ceiling=Fix.FromInt(3),Priority=100 }).Arg0);
            var twin = Save(c,factory,game);
            Until(c,game,twin,() => Jobs(game).Any(j => j.Id == haul && j.Loaded == Fix.FromInt(3) && j.ToilIndex == 2),"real complete network pickup");
            c.Equal(Fix.FromInt(12),Stock(game,depot),"loaded material is not instant delivery");
            Send(c,game,new SetWorkerHeldCommand { Worker=worker,Held=true });
            c.That(!Jobs(game).Any(j => j.Id == haul && (j.State == JobState.Open || j.State == JobState.Assigned || j.State == JobState.Paused)),"interruption retires original assigned job");
            c.Equal(Fix.FromInt(3),Stock(game,worker),"interruption retains actual loaded cargo");
            c.Equal(Fix.FromInt(3),PoolStock(game),"cargo is never refunded into its original network");
            Send(c,game,new SetWorkerHeldCommand { Worker=worker,Held=false }); twin = Save(c,factory,game);
            for (int i=0; i<4; i++) Step(c,game,twin);
            var logistics = game.Seat(0).View.Feature<LogisticsView>(); var queue = new List<LogisticsDispatchOffer>();
            logistics.CopyQueue(queue);
            c.That(queue.Any(q => q.Kind == LogisticsDispatchKind.CargoReturn && q.Haul.Blocker == RecoveryDestinationDeclaration.NoDemand),"explicit demand-only policy waits without invented demand");
            c.Equal(Fix.FromInt(3),Stock(game,worker),"idle recovery wait keeps cargo physically loaded");
            twin = Save(c,factory,game); game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash();
            Step(c,game,twin); c.Equal(paused,game.Hash(),"one paused game clock freezes recovery"); game.Clock.Resume(); twin.Clock.Resume();
            Send(c,game,new ConfigureStockPolicyCommand { Store=depot,Resource=steel,Change=LogisticsDemandChange.Replace(
                new LogisticsDemandSettings(true,Fix.FromInt(15),Fix.FromInt(15),Fix.FromInt(15),50,LogisticsReserveAccess.Protected)) });
            twin = Save(c,factory,game);
            Until(c,game,twin,() => Jobs(game).Any(j => j.RecoveryKind == LogisticsRecoveryKind.CargoReturn && j.Worker == worker),"normal residual return acquisition");
            var returning = Jobs(game).Single(j => j.RecoveryKind == LogisticsRecoveryKind.CargoReturn && j.Worker == worker);
            c.That(returning.Id != haul && !returning.Source.IsValid && !returning.Purchase && returning.Destination == depot,
                "engine selects one new physical return without pickup, purchase or a host destination decision");
            c.Equal(Fix.FromInt(3),returning.Loaded,"return acquires only actual residual cargo");
            twin = Save(c,factory,game);
            Until(c,game,twin,() => Stock(game,depot) == Fix.FromInt(13) && Stock(game,worker) == Fix.FromInt(2),"timed partial return unloading");
            c.Equal(Fix.FromInt(3),PoolStock(game),"partial return cannot debit original network again");
            twin = Save(c,factory,game);
            Until(c,game,twin,() => Stock(game,depot) == Fix.FromInt(15) && Stock(game,worker).IsZero && !Jobs(game).Any(j => j.Id == returning.Id),"saved physical unloading and departure");
            var hidden = new List<LogisticsDispatchOffer>(); game.Seat(1).View.Feature<LogisticsView>().CopyQueue(hidden);
            c.Equal(0,hidden.Count,"foreign seat receives no private recovery queue");
            c.Equal(idle,isolated.Hash(),"interleaved session retains its own original stock/policies");
            Save(c,factory,game);
            var replay = new ReplayVerifier().Verify(factory,recording.Finish(),limits);
            c.That(replay.Success && replay.Game?.Hash() == game.Hash(),"normal interruption/demand/return replay " + replay.Failure + "/" + replay.Detail);
        }
    }
}
