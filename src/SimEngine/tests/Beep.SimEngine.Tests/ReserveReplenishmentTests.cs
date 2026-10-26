using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;

namespace Beep.SimEngine.Tests
{
    internal static class ReserveReplenishmentTests
    {
        internal static void Register()
        {
            TestRunner.Add("Reserve replenishment: strict stock access, shared controls and protected obligations", Contracts);
            TestRunner.Add("Reserve replenishment: physical reserve use, refill and saved continuation", Physical);
        }
        private static GameFactory Fixture(bool publicOutlet = false, bool publicPolicy = false, bool publicWorker = false,
            bool donor = false, Action<JsonValue> author = null, Action<JsonValue> workAuthor = null)
            => EmergencyMaterialReserveTests.Fixture(publicOutlet, publicPolicy, publicWorker, author: profiles =>
            {
                profiles.Get("logistics_policies")[0].Set("stock", Json.Parse(FrameworkProofContent.Read("reserve-replenishment.json")).Get("stock"));
                author?.Invoke(profiles);
            }, edit: root =>
            {
                if (!donor) return;
                var extra = root.Get("entities")[0].Clone(); extra.Set("id", JsonValue.Of("donor"));
                var parts = extra.Get("components"); parts.Remove("logistics_policy"); parts.Remove("contract_outlet_policy");
                parts.Get("point_presence").Set("position", JsonValue.Number("30"));
                root.Get("entities").Add(extra); StartupContentAuthoring.Add(root, "donor", "donor", 0, extra.Get("components").Get("point_presence"));
            }, workAuthor: workAuthor);
        private static JsonValue StockJson(JsonValue root) => root.Get("logistics_policies")[0].Get("stock").Get("supply");
        private static LogisticsDemandSettings Settings(LogisticsReserveAccess access, bool enabled = true)
            => new LogisticsDemandSettings(enabled, Fix.FromInt(17), Fix.FromInt(20), Fix.FromInt(40), 5, access);
        private static LogisticsDemandView Band(SimGame game)
        {
            var facts = new List<LogisticsDemandView>(); game.Seat(0).View.Feature<LogisticsView>().CopyDemands(facts);
            foreach (var fact in facts) if (fact.Destination == Find(game, "source").Id && fact.Source.Scope == LogisticsService.StockDemandScope) return fact;
            throw new InvalidOperationException("missing authored reserve band");
        }
        private static List<LogisticsDispatchOffer> Offers(SimGame game)
        { var facts = new List<LogisticsDispatchOffer>(); game.Seat(0).View.Feature<LogisticsView>().CopyQueue(facts); return facts; }
        private static Fix Stock(SimGame game, string prototype) => Find(game, prototype).Get<IInletPort>().Amount(Supply(game));
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null)
                throw new InvalidOperationException("reserve replenishment update fault: " + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 400 && !done(); i++) Step(game, twin);
            if (!done())
            {
                var facts = new List<string>(); foreach (Job job in Board(game).Jobs) facts.Add(job.Id + "/" + job.State + "/" + job.Blocker + "/" + job.ToilIndex + "/" + job.Loaded + "/from=" + job.From + "/to=" + job.To);
                throw new InvalidOperationException(stage + " timeout at game update " + game.Clock.StepIndex + ": " + string.Join(";", facts) +
                    "; source=" + Stock(game, "source") + "; donor=" + Stock(game, "donor") + "; destination=" + Stock(game, "destination"));
            }
        }
        private static ConfigureStockPolicyCommand Change(SimGame game, LogisticsDemandChange change)
            => new ConfigureStockPolicyCommand { Store = Find(game, "source").Id, Resource = Supply(game), Change = change };
        private static byte[] Bytes(LogisticsDemandSettings settings)
        { using var stream = new MemoryStream(); LogisticsDemandSettings.Sync(new StateWriter(stream), settings); return stream.ToArray(); }
        private static void Contracts(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "explicit stock access changes current game semantics");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "stock setting codec replaced without a legacy reader");
            foreach (bool publicOutlet in new[] { false, true }) foreach (bool publicPolicy in new[] { false, true })
            {
                var factory = Fixture(publicOutlet, publicPolicy); var game = Start(factory); var retained = Band(game); var logistics = game.World.Services.Get<LogisticsService>();
                c.Equal(LogisticsReserveAccess.EmergencyOnly, retained.Settings.ReserveAccess, "native/public initializer seeds same actual demand owner");
                c.That(!retained.Replenishing, "authored full reserve has no fake refill request");
                c.Equal(Fix.FromInt(20), logistics.DemandOf(Find(game, "source").Id, Supply(game)), "public total intent still includes emergency-accessible band");
                c.Equal(Fix.Zero, logistics.Spare(Find(game, "source").Get<IOutletPort>(), Supply(game)), "ordinary export protects reserve refill target");
                var view = game.Seat(0).View.Feature<LogisticsView>(); ulong before = game.Hash();
                c.Equal(LogisticsPolicyCodes.Reserve, view.PreviewHaul(Find(game, "source").Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(8)).Blocker,
                    "manual hauling cannot inherit stock emergency access");
                var hidden = new List<LogisticsDemandView>(); PlayerView.Create(game.World, new PlayerId(1)).Feature<LogisticsView>().CopyDemands(hidden);
                c.Equal(0, hidden.Count, "refill settings stay company private");
                c.Equal(before, game.Hash(), "stock observations and previews mutate no demands/receipts");
                var level = LogisticsDemandChange.Level(LogisticsStockLevel.Minimum, Fix.FromInt(16), true);
                c.That(TestKit.Order(game, 0, Change(game, level)).Accepted, "existing band control changes selected native level");
                c.Equal(LogisticsReserveAccess.EmergencyOnly, Band(game).Settings.ReserveAccess, "band-only change preserves access");
                c.Equal(Fix.FromInt(20), Band(game).Settings.Desired, "band-only change preserves desired reserve target");
                c.Equal(Fix.FromInt(17), retained.Settings.Minimum, "retained private band is detached");
                c.Throws<ArgumentException>(() => Settings((LogisticsReserveAccess)0), "absent access enum is not an implicit default");
                c.Throws<ArgumentException>(() => Settings((LogisticsReserveAccess)9), "unknown access enum refuses");
                var unknown = Bytes(Settings(LogisticsReserveAccess.EmergencyOnly)); unknown[unknown.Length - 1] = 9;
                c.Throws<CorruptStateException>(() => LogisticsDemandSettings.Sync(new StateReader(unknown, 0, unknown.Length), null), "unknown saved stock access refuses");
                var shortBytes = Bytes(Settings(LogisticsReserveAccess.EmergencyOnly)); Array.Resize(ref shortBytes, shortBytes.Length - 1);
                c.Throws<CorruptStateException>(() => LogisticsDemandSettings.Sync(new StateReader(shortBytes, 0, shortBytes.Length), null), "truncated stock setting refuses");
                c.Equal(LogisticsPolicyCodes.NoPolicy, view.PreviewStockPolicy(Find(game, "destination").Id, Supply(game), LogisticsDemandChange.Replace(Settings(LogisticsReserveAccess.EmergencyOnly))),
                    "destination without outlet policy cannot pretend to expose borrowable reserve");
                var invalid = Change(game, LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.Half, Fix.One, Fix.FromInt(20), 0, LogisticsReserveAccess.EmergencyOnly)));
                c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(invalid), "non-native resource quantity refuses preview");
                c.Equal(Codes.InvalidArgument, TestKit.Order(game, 0, invalid).Refusal, "non-native command/preview refusal parity");
                game.Seat(0).Submit(Change(game, LogisticsDemandChange.Replace(Settings(LogisticsReserveAccess.Protected))));
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending stock access edit saves through one owner");
                Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "pending shared command continues identically");
                c.Equal(LogisticsReserveAccess.Protected, Band(game).Settings.ReserveAccess, "restored settings replace authored initial policy");
                c.Equal(StateCodecTests.ExpectedLogisticsSchema, logistics.StateSchemaVersion, "same demand owner owns new stock access codec");
            }
            foreach (Action<JsonValue> invalid in new Action<JsonValue>[] {
                root => StockJson(root).Remove("reserve_access"), root => StockJson(root).Set("reserve_access", JsonValue.Of("unknown")),
                root => StockJson(root).Set("reserve_access", JsonValue.Of(true)), root => StockJson(root).Set("reserve_access", JsonValue.Null()) })
                c.Throws<ContentException>(() => Start(Fixture(author: invalid)), "stock JSON requires typed explicit access");
            foreach (bool publicOutlet in new[] { false, true })
            {
                var protectedStock = Start(Fixture(publicOutlet, author: root => StockJson(root).Set("reserve_access", JsonValue.Of("protected"))));
                Raise(c, protectedStock); c.That(Assigned(protectedStock) == null, "outlet access does not override protected stock target");
                var protectedOutlet = Start(Fixture(publicOutlet, author: root => root.Get("logistics_policies")[0].Get("outlet").Set("reserve_access", JsonValue.Of("protected"))));
                Raise(c, protectedOutlet); c.That(Assigned(protectedOutlet) == null, "stock access does not override protected outlet floor");
                var workflow = Start(Fixture(publicOutlet, workAuthor: work =>
                {
                    work.Get("activities")[0].Set("automatic_supply", JsonValue.Of(true));
                    work.Get("target_resource_requirements")[0].Get("quantities").Set("supply", JsonValue.Number("20"));
                }));
                Post(c, workflow); Raise(c, workflow);
                c.That(workflow.World.Services.Get<LogisticsService>().DemandOf(Find(workflow, "source").Id, Supply(workflow)) > Fix.FromInt(20),
                    "actual ordinary workflow adds its own obligation, not another stock balance");
                c.That(Assigned(workflow) == null && Stock(workflow, "source") == Fix.FromInt(20), "emergency-only stock access never releases ordinary workflow obligation");
            }
        }
        private static void Physical(Check c)
        {
            foreach (bool publicOutlet in new[] { false, true }) foreach (bool publicPolicy in new[] { false, true })
            {
                var factory = Fixture(publicOutlet, publicPolicy, donor: true); var game = Start(factory); var isolated = Start(factory); ulong unrelated = isolated.Hash();
                Configure(c, game); var response = Raise(c, game); var haul = Assigned(game);
                c.That(haul?.IsHaul == true && haul.From == Find(game, "source").Id && haul.Amount == Fix.FromInt(8), "actual emergency borrows preferred reserved depot stock");
                c.Equal(Fix.FromInt(20), Stock(game, "source"), "reserve not teleported at acquisition");
                c.Equal(Fix.FromInt(20), Stock(game, "donor"), "donor not fabricated as incident stock");
                Until(game, () => haul.Loaded.IsPositive, "partial reserve loading");
                c.That(haul.Loaded < haul.Amount && Stock(game, "destination") == Fix.Zero, "loading takes game time and is not delivery");
                c.That(Band(game).Replenishing, "existing refill latch detects actual reserved-stock shortage");
                var offers = Offers(game); bool ordinaryRefill = false;
                foreach (var offer in offers) if (offer.Destination == Find(game, "source").Id) ordinaryRefill |= !offer.Emergency;
                c.That(ordinaryRefill, "reserve recovery remains ordinary demand, not emergency authority");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial emergency cargo and refill latch save");
                Until(game, () => response.State == OrderState.Done, "physical incident repair", twin);
                c.Equal(game.Hash(), twin.Hash(), "partial reserve use saves through actual timed repair");
                Until(game, () => Assigned(game)?.IsHaul == true && Assigned(game).To == Find(game, "source").Id, "ordinary refill dispatch", twin);
                var refill = Assigned(game); c.Equal(Find(game, "donor").Id, refill.From, "real external donor supplies reserve recovery");
                c.Equal(Fix.FromInt(8), refill.Amount, "existing desired target requests only consumed quantity");
                Until(game, () => refill.Loaded.IsPositive, "partial ordinary reserve refill", twin);
                var saved = factory.Load(game.Save()); c.Equal(game.Hash(), saved.Hash(), "ordinary refill partial cargo saves");
                long time = game.Clock.StepIndex; game.Clock.Pause(); saved.Clock.Pause(); Step(game, saved);
                c.Equal(time, game.Clock.StepIndex, "sole game clock pause stops physical reserve refill");
                game.Clock.Resume(); saved.Clock.Resume();
                Until(game, () => !refill.IsLive, "physical refill unloading/departure", saved);
                c.Equal(game.Hash(), saved.Hash(), "saved ordinary refill finishes identically");
                c.Equal(Fix.FromInt(20), Stock(game, "source"), "depot physically restored to authored desired stock");
                c.Equal(Fix.FromInt(12), Stock(game, "donor"), "real donor paid exactly eight native units");
                c.Equal(Fix.Zero, Stock(game, "destination"), "incident consumed physically delivered inputs exactly once");
                c.Equal(Fix.Zero, Find(game, "machine").Get<ITransporter>().Cargo.Amount(Supply(game)), "no duplicated or stranded cargo after refill");
                c.That(!Band(game).Replenishing, "same refill latch settles at target");
                var second = Raise(c, game); Raise(c, saved);
                Until(game, () => second.State == OrderState.Done, "second incident from restored reserve", saved);
                Until(game, () => Stock(game, "source") == Fix.FromInt(20) && Assigned(game) == null, "second physical reserve refill", saved);
                c.Equal(game.Hash(), saved.Hash(), "repeated incident/refill remains identical after checkpoint");
                c.Equal(Fix.FromInt(4), Stock(game, "donor"), "second refill uses actual finite donor balance");
                var third = Raise(c, game); Raise(c, saved);
                Until(game, () => third.State == OrderState.Done, "third incident consumes real reserve", saved);
                Until(game, () => Stock(game, "source") == Fix.FromInt(16) && Assigned(game) == null, "finite donor partial replenishment", saved);
                c.Equal(game.Hash(), saved.Hash(), "depleted donor/partial reserve shortage saves deterministically");
                c.Equal(Fix.Zero, Stock(game, "donor"), "third refill cannot manufacture missing donor stock");
                c.That(Band(game).Replenishing, "existing latch reports unresolved reserve shortage");
                bool shortage = false; foreach (var offer in Offers(game))
                    if (offer.Destination == Find(game, "source").Id) shortage |= !offer.Emergency && offer.Requested == Fix.FromInt(4) && offer.Haul.Status != HaulStatus.Ready;
                c.That(shortage, "finite stock depletion exposes ordinary unmet demand instead of instant fallback");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "unresolved reserve readiness persists through existing owners");
                c.Equal(unrelated, isolated.Hash(), "interleaved sessions remain isolated");
                c.That(ReferenceEquals(game.Clock, game.World.Clock), "all reserve gameplay uses the same game clock");
            }
            StandbyRefill(c); PublicCarrier(c);
        }
        private static void StandbyRefill(Check c)
        {
            var factory = Fixture(donor: true); var game = Start(factory); var response = Raise(c, game);
            Until(game, () => response.State == OrderState.Done, "standby worker emergency delivery/repair");
            for (int i = 0; i < 12; i++) Step(game);
            c.That(Assigned(game) == null && Stock(game, "source") == Fix.FromInt(12), "emergency-only worker cannot borrow reserve status for ordinary replenishment");
            Configure(c, game); Until(game, () => Stock(game, "source") == Fix.FromInt(20) && Assigned(game) == null, "player explicitly permits ordinary refill");
            c.Equal(Fix.FromInt(12), Stock(game, "donor"), "standby release permits real physical refill, not free stock");
        }
        private static void PublicCarrier(Check c)
        {
            foreach (bool publicOutlet in new[] { false, true })
            {
                var factory = Fixture(publicOutlet, publicPolicy: true, publicWorker: true, donor: true); var game = Start(factory);
                // Public provider authors only haul role; repair correctly waits for a compatible inspector.
                c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = Find(game, "machine").Id,
                    Settings = new WorkPolicySettings(true, 100, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted, "player permits ordinary public-carrier logistics");
                var response = Raise(c, game); var supply = Assigned(game); Until(game, () => !supply.IsLive, "public carrier reserve supply");
                if (supply.State != JobState.Done) throw new InvalidOperationException("public supply ended without delivery: " + supply.State + "/" + supply.Blocker + "/" + supply.ToilIndex + "/loaded=" + supply.Loaded);
                var twin = factory.Load(game.Save());
                Until(game, () => Stock(game, "source") == Fix.FromInt(20) && Assigned(game) == null, "public carrier physical refill", twin);
                c.Equal(game.Hash(), twin.Hash(), "public-only worker/outlet/policy restore and replenish through same engine");
                c.Equal(Fix.FromInt(12), Stock(game, "donor"), "public carrier takes only actual donor stock");
                c.Equal(Fix.FromInt(8), Stock(game, "destination"), "haul-only worker cannot consume response inputs as fake inspection");
                c.Equal(Fix.FromInt(8), game.World.Services.Get<LogisticsService>().DemandOf(Find(game, "destination").Id, Supply(game)),
                    "fulfilled unclaimed workflow inputs remain protected while missing repair role");
                c.Equal(Fix.Zero, game.World.Services.Get<LogisticsService>().Spare(Find(game, "destination").Get<IOutletPort>(), Supply(game)),
                    "ordinary refill cannot steal supplied-but-unclaimed response inputs");
                c.That(response.State != OrderState.Done, "missing repair role remains actual blocker");
            }
        }
    }
}
