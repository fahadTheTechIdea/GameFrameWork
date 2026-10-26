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
    internal static class EmergencyMaterialReserveTests
    {
        internal static void Register()
        {
            TestRunner.Add("Emergency material reserves: strict policies, private controls and bounded demand", Contracts);
            TestRunner.Add("Emergency material reserves: physical supply, retained commitments and saved continuation", Physical);
        }
        internal static GameFactory Fixture(bool publicOutlet = false, bool publicPolicy = false, bool publicWorker = false,
            Action<JsonValue> author = null, Action<JsonValue> edit = null, Action<JsonValue> workAuthor = null)
        {
            var profiles = Json.Parse(FrameworkProofContent.Read("emergency-material-reserves.json")); author?.Invoke(profiles);
            return WorkerStandbyTests.Fixture(publicPolicy, publicWorker, supplied: true, haulWorker: publicWorker,
                edit: root =>
                {
                    root.Get("entities")[0].Get("components").Set(publicOutlet ? "contract_outlet_policy" : "logistics_policy",
                        Json.Parse(publicOutlet ? "{\"profile\":\"reserve.materials\"}" : "{\"profiles\":[{\"port\":null,\"profile\":\"reserve.materials\"}]}"));
                    edit?.Invoke(root);
                }, workAuthor: work => { work.Set("logistics_policies", profiles.Get("logistics_policies")); workAuthor?.Invoke(work); });
        }
        private static JsonValue Outlet(JsonValue document) => document.Get("logistics_policies")[0].Get("outlet");
        private static OutletPolicySettings Read(SimGame game) => LogisticsPolicyRules.Read(game.World, Find(game, "source"));
        private static OutletPolicySettings Settings(SimGame game, LogisticsReserveAccess access, bool enabled = true,
            string[] destinations = null, Fix? reserve = null)
            => new OutletPolicySettings(enabled, new Bill(new[] { new ResourceAmount(Supply(game), reserve ?? Fix.FromInt(20)) }),
                3, destinations ?? new[] { "destination" }, access);
        private static void Ordinary(Check c, SimGame game)
            => c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Find(game, "destination").Id, Resource = Supply(game),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.FromInt(12), Fix.FromInt(40), int.MaxValue, LogisticsReserveAccess.Protected)) }).Accepted,
                "player authors ordinary maximum-priority stock intent");
        private static List<LogisticsDispatchOffer> Offers(SimGame game)
        { var facts = new List<LogisticsDispatchOffer>(); game.Seat(0).View.Feature<LogisticsView>().CopyQueue(facts); return facts; }
        private static Fix Stock(SimGame game, string prototype) => Find(game, prototype).Get<IInletPort>().Amount(Supply(game));
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("material reserve update fault: " + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) Step(game, twin);
            if (!done())
            {
                var facts = new List<string>(); foreach (Job job in Board(game).Jobs) facts.Add(job.Id + "/" + job.State + "/" + job.Blocker + "/" + job.ToilIndex + "/" + job.Loaded);
                throw new InvalidOperationException(stage + " timeout at game update " + game.Clock.StepIndex + ": " + string.Join(";", facts));
            }
        }
        private static byte[] Bytes(OutletPolicySettings settings)
        { using var stream = new MemoryStream(); OutletPolicySettings.Sync(new StateWriter(stream), settings); return stream.ToArray(); }
        private static void Contracts(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "reserve access changes actual simulation semantics");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "clean protocol replacement, no old layout reader");
            foreach (bool publicOutlet in new[] { false, true }) foreach (bool publicPolicy in new[] { false, true })
            {
                var factory = Fixture(publicOutlet, publicPolicy); var game = Start(factory); var source = Find(game, "source");
                var view = game.Seat(0).View.Feature<LogisticsView>(); var logistics = game.World.Services.Get<LogisticsService>();
                var foreign = Start(factory);
                c.Throws<ArgumentException>(() => LogisticsPolicyRules.Read(game.World, Find(foreign, "source")), "public observation refuses foreign provider entity");
                c.Equal(LogisticsReserveAccess.EmergencyOnly, Read(game).ReserveAccess, "explicit JSON seeds native/public outlet owner");
                c.That(source.Get<IOutletPort>() is IInletPort, "same outlet also has inlet contract");
                c.Equal(Fix.Zero, logistics.Spare(source.Get<IOutletPort>(), Supply(game)), "ordinary spare never spends incident reserve");
                c.Equal(Code.None, view.ReadOutletPolicy(source.Id, out var retained), "private immutable policy facts available");
                c.Equal(Codes.NotOwned, PlayerView.Create(game.World, new PlayerId(1)).Feature<LogisticsView>().ReadOutletPolicy(source.Id, out _), "foreign viewer cannot read reserve settings");
                c.Equal(Codes.NotOwned, PlayerView.Create(game.World, new PlayerId(1)).Feature<LogisticsView>().PreviewOutletPolicy(source.Id, OutletPolicyChange.Replace(retained)), "foreign preview cannot gain policy authority");
                c.Equal(LogisticsPolicyCodes.NoPolicy, view.ReadOutletPolicy(Find(game, "destination").Id, out _), "absent outlet policy has no invented reserve");
                Ordinary(c, game);
                c.That(TestKit.Order(game, 0, new ConfigureCompanyAlertCommand { Company = new PlayerId(0),
                    Settings = new CompanyAlertSettings("alerts.standard", CompanyAlertLevel.Emergency, Array.Empty<WorkerReserveSetting>()) }).Accepted,
                    "player can raise existing company alert without fabricating emergency demand");
                ulong before = game.Hash(); int next = Board(game).NextJobId;
                c.That(Offers(game).Count == 1 && !Offers(game)[0].Emergency && Offers(game)[0].Haul.Blocker == LogisticsPolicyCodes.Reserve,
                    "ordinary maximum priority/alert cannot borrow reserve");
                c.Equal(LogisticsPolicyCodes.Reserve, view.PreviewHaul(source.Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(8)).Blocker,
                    "manual preview has no emergency authority");
                c.Equal(0, logistics.AcquireHaul(source.Owner, source.Id, Find(game, "destination").Id, Supply(game), Fix.FromInt(8), int.MaxValue),
                    "manual acquire cannot supply itself emergency provenance");
                c.Equal(before, game.Hash(), "all policy/admission reads preserve actual stock and claims");
                c.Equal(next, Board(game).NextJobId, "refused ordinary acquisition allocates no job");
                c.That(TestKit.Order(game, 0, new ConfigureOutletPolicyCommand { Source = source.Id,
                    Change = OutletPolicyChange.Reserve(Supply(game), Fix.FromInt(19), true) }).Accepted, "existing resource control edits reserve only");
                c.Equal(LogisticsReserveAccess.EmergencyOnly, Read(game).ReserveAccess, "reserve-only change preserves emergency access");
                c.Equal(3, Read(game).ExportPriority, "reserve-only change preserves export priority");
                c.That(Read(game).Allows("destination") && !Read(game).Allows("source"), "reserve-only change preserves actual destination filter");
                c.Equal(Fix.FromInt(20), retained.Reserve.AmountOf(Supply(game)), "retained settings do not follow later command");
                c.Throws<ArgumentException>(() => Settings(game, (LogisticsReserveAccess)0), "missing enum is not a compatibility default");
                c.Throws<ArgumentException>(() => Settings(game, (LogisticsReserveAccess)9), "unknown enum refuses");
                var invalid = Settings(game, LogisticsReserveAccess.EmergencyOnly, reserve: Fix.FromRatio(1, 2)); before = game.Hash();
                c.Equal(Codes.InvalidArgument, view.PreviewOutletPolicy(source.Id, OutletPolicyChange.Replace(invalid)), "native resource quantum refuses fractional reserve");
                c.Equal(Codes.InvalidArgument, TestKit.Order(game, 0, new ConfigureOutletPolicyCommand { Source = source.Id, Change = OutletPolicyChange.Replace(invalid) }).Refusal,
                    "preview/command invalid-quantity parity");
                // The command advances game time; corrupt decode below must preserve its current state.
                before = game.Hash(); var component = (Component)source.Get<IOutletPolicy>(); var unknown = Bytes(Read(game)); unknown[5] = 9;
                c.Throws<CorruptStateException>(() => component.Sync(new StateReader(unknown, 0, unknown.Length)), "unknown saved access refuses before publishing");
                c.Equal(before, game.Hash(), "unknown access leaves actual native/public owner intact");
                var shortBytes = Bytes(Read(game)); Array.Resize(ref shortBytes, shortBytes.Length - 1);
                c.Throws<CorruptStateException>(() => component.Sync(new StateReader(shortBytes, 0, shortBytes.Length)), "truncated access settings refuse");
                c.Equal(before, game.Hash(), "truncated settings leave actual owner intact");
                var badNative = Bytes(invalid);
                c.Throws<CorruptStateException>(() => component.Sync(new StateReader(badNative, 0, badNative.Length)), "valid codec with invalid native envelope refuses");
                c.Equal(before, game.Hash(), "late native validation stages settings before publish");
                game.Seat(0).Submit(new ConfigureOutletPolicyCommand { Source = source.Id, Change = OutletPolicyChange.Replace(Settings(game, LogisticsReserveAccess.Protected)) });
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending access command uses shared save/hash codec");
                Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "saved pending access edit continues identically");
                c.Equal(LogisticsReserveAccess.Protected, Read(game).ReserveAccess, "existing command applies requested access exactly");
                c.Equal(LogisticsReserveAccess.EmergencyOnly, retained.ReserveAccess, "retained access fact stays detached");
                c.Equal(StateCodecTests.ExpectedLogisticsSchema, logistics.StateSchemaVersion, "existing logistics owner declares new demand admission semantics");
            }
            foreach (Action<JsonValue> invalid in new Action<JsonValue>[] {
                p => Outlet(p).Remove("reserve_access"), p => Outlet(p).Set("reserve_access", JsonValue.Of("unknown")),
                p => Outlet(p).Set("reserve_access", JsonValue.Of(true)), p => Outlet(p).Set("reserve_access", JsonValue.Null()) })
                c.Throws<ContentException>(() => Start(Fixture(author: invalid)), "strict JSON reserve access is explicit and typed");
            Gates(c); ModDemand(c, false);
        }
        private static void Gates(Check c)
        {
            foreach (bool publicOutlet in new[] { false, true }) foreach (string gate in new[] { "protected", "disabled", "destination", "own-demand", "route", "emergency-worker", "area", "role" })
            {
                var game = Start(Fixture(publicOutlet, author: p =>
                {
                    if (gate == "protected") Outlet(p).Set("reserve_access", JsonValue.Of("protected"));
                    if (gate == "disabled") Outlet(p).Set("enabled", JsonValue.Of(false));
                    if (gate == "destination") Outlet(p).Set("allowed_destinations", JsonValue.NewArray());
                }));
                if (gate == "own-demand") c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Find(game, "source").Id, Resource = Supply(game),
                    Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.Zero, Fix.FromInt(20), Fix.FromInt(40), 0, LogisticsReserveAccess.Protected)) }).Accepted, "source own stock obligation remains protected");
                if (gate == "route") c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = false }).Accepted, "player closes actual route");
                if (gate == "emergency-worker") c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = Find(game, "machine").Id,
                    Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, false, WorkAssignmentMode.EmergencyOnly) }).Accepted,
                    "player makes machine ineligible for emergencies");
                if (gate == "area" || gate == "role") c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = Find(game, "machine").Id,
                    Settings = new WorkPolicySettings(true, 0, gate == "role" ? new[] { new WorkerRoleId("inspect") } : new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") },
                        gate == "area" ? "reserve.home" : null, true, true, WorkAssignmentMode.EmergencyOnly) }).Accepted,
                    "player restricts actual operating area or role");
                Raise(c, game); var facts = Offers(game);
                c.That(facts.Count == 1 && facts[0].Emergency && facts[0].Haul.Status != HaulStatus.Ready, "actual emergency cannot bypass current gate: " + gate);
                c.Equal(Fix.FromInt(20), Stock(game, "source"), "no instant reserve consumption under gate: " + gate);
                c.Equal(Fix.Zero, Find(game, "source").Get<IOutletPort>().ReservedOut(Supply(game)), "no source receipts acquired under gate: " + gate);
                c.Equal(0, Find(game, "machine").Get<IWorker>().Job, "machine remains unassigned under gate: " + gate);
            }
        }
        private static void Physical(Check c)
        {
            foreach (bool publicOutlet in new[] { false, true }) foreach (bool publicPolicy in new[] { false, true })
            {
                var factory = Fixture(publicOutlet, publicPolicy); var game = Start(factory); var isolated = Start(factory); ulong separate = isolated.Hash();
                Ordinary(c, game); Order response = Raise(c, game); Job haul = Assigned(game);
                c.That(haul?.IsHaul == true, "same engine assigns actual incident supply carrier");
                c.Equal(Fix.FromInt(8), haul.Amount, "mixed ordinary target cannot inflate emergency allocation");
                c.Equal(Fix.FromInt(20), Stock(game, "source"), "acquisition promises stock but does not teleport pickup");
                c.Equal(Fix.Zero, Stock(game, "destination"), "incident does not invent delivered materials");
                c.Equal(Fix.FromInt(8), Find(game, "source").Get<IOutletPort>().ReservedOut(Supply(game)), "actual receipt protects exactly emergency load");
                var offers = Offers(game); c.That(offers.Count == 1 && !offers[0].Emergency && offers[0].Requested == Fix.FromInt(12),
                    "promised emergency load leaves only ordinary shortage, not emergency-laundered refill");
                Until(game, () => haul.Loaded.IsPositive, "partial physical pickup");
                c.That(haul.Loaded < haul.Amount, "native handling progresses over game time");
                c.Equal(Fix.Zero, Stock(game, "destination"), "partial cargo has not reached destination");
                var saved = factory.Load(game.Save()); c.Equal(game.Hash(), saved.Hash(), "partial reserve load/receipts save exactly");
                var requested = Settings(game, LogisticsReserveAccess.Protected, enabled: false);
                game.Seat(0).Submit(new ConfigureOutletPolicyCommand { Source = Find(game, "source").Id, Change = OutletPolicyChange.Replace(requested) });
                saved.Seat(0).Submit(new ConfigureOutletPolicyCommand { Source = Find(saved, "source").Id, Change = OutletPolicyChange.Replace(Settings(saved, LogisticsReserveAccess.Protected, enabled: false)) });
                Step(game, saved); c.Equal(game.Hash(), saved.Hash(), "source policy edit cannot rewrite acquired receipt history");
                Until(game, () => response.State == OrderState.Done, "timed delivery then real repair", saved);
                c.Equal(game.Hash(), saved.Hash(), "saved loading/travel/unloading/repair identity");
                c.Equal(Fix.FromInt(12), Stock(game, "source"), "only real emergency inputs removed from original reserve");
                c.Equal(Fix.Zero, Stock(game, "destination"), "response consumes delivered inputs exactly once");
                c.That(!Find(game, "destination").Get<ContractIncident>().ReadIncident().Active, "physical repair resolves actual incident");
                c.Equal(separate, isolated.Hash(), "interleaved unrelated session remains isolated");
                long time = game.Clock.StepIndex; game.Clock.Pause(); game.Update(); c.Equal(time, game.Clock.StepIndex, "sole game clock pause stops reserve gameplay");
                c.That(ReferenceEquals(game.Clock, game.World.Clock), "session and entity world expose same game clock instance");
            }
            OwnDemand(c); PublicCarrier(c); Expired(c); ModDemand(c, true);
        }
        private static void OwnDemand(Check c)
        {
            var factory = Fixture(); var game = Start(factory);
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Find(game, "source").Id, Resource = Supply(game),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.Zero, Fix.FromInt(15), Fix.FromInt(40), 0, LogisticsReserveAccess.Protected)) }).Accepted, "source protects its actual own demand");
            var response = Raise(c, game); var haul = Assigned(game); c.Equal(Fix.FromInt(5), haul.Amount, "emergency releases outlet floor but not source own obligation");
            Until(game, () => !haul.IsLive, "first bounded delivery");
            c.Equal(Fix.FromInt(15), Stock(game, "source"), "source own stock remains intact after first physical load");
            c.Equal(Fix.FromInt(5), Stock(game, "destination"), "short supply waits physically for remaining inputs");
            c.That(response.State != OrderState.Done, "shortage does not fabricate completed repair");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial response shortage saves");
            var command = new ConfigureStockPolicyCommand { Store = Find(game, "source").Id, Resource = Supply(game),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.Zero, Fix.FromInt(15), Fix.FromInt(40), 0, LogisticsReserveAccess.Protected)) };
            game.Seat(0).Submit(command); twin.Seat(0).Submit(new ConfigureStockPolicyCommand { Store = Find(twin, "source").Id, Resource = Supply(twin), Change = command.Change });
            Step(game, twin); Until(game, () => response.State == OrderState.Done, "remaining three hauled after explicit own-demand edit", twin);
            c.Equal(game.Hash(), twin.Hash(), "partial demand then edited continuation stays deterministic");
            c.Equal(Fix.FromInt(12), Stock(game, "source"), "two physical loads consume only eight total");
        }
        private static void PublicCarrier(Check c)
        {
            foreach (bool publicOutlet in new[] { false, true })
            {
                var factory = Fixture(publicOutlet, publicPolicy: true, publicWorker: true); var game = Start(factory); var response = Raise(c, game); var haul = Assigned(game);
                c.That(haul?.IsHaul == true, "public-only worker uses same reserve dispatch, no native worker lookup");
                var twin = factory.Load(game.Save()); Until(game, () => !haul.IsLive, "public carrier timed reserve delivery", twin);
                c.Equal(game.Hash(), twin.Hash(), "public worker/port/policy physical continuation saves");
                c.Equal(Fix.FromInt(8), Stock(game, "destination"), "public haul-only worker physically delivers response inputs");
                c.Equal(Fix.FromInt(12), Stock(game, "source"), "public carrier consumes no extra reserve");
                c.That(response.State != OrderState.Done, "haul-only provider does not fabricate inspection role to repair");
            }
        }
        private static void Expired(Check c)
        {
            var factory = Fixture(); var game = Start(factory); Raise(c, game); var haul = Assigned(game);
            Until(game, () => haul.Loaded.IsPositive, "acquired incident supply prefix");
            // Mod-owned incident execution retires the bound episode. No host stock/haul logic.
            Find(game, "destination").Get<ContractIncident>().Renew(game.World);
            var twin = factory.Load(game.Save()); Until(game, () => !haul.IsLive, "retired episode committed cargo finishes", twin);
            c.Equal(game.Hash(), twin.Hash(), "retired requester does not rewrite acquired cargo on restore");
            c.Equal(Fix.FromInt(12), Stock(game, "source"), "new response uses prior promised material, not duplicate reserve shipment");
        }
        private static void ModDemand(Check c, bool physical)
        {
            foreach (bool publicOutlet in new[] { false, true }) foreach (bool emergency in new[] { false, true })
            {
                var factory = EmergencyHaulTests.Factory(emergency: emergency, edit: root =>
                {
                    root.Set("logistics_policies", Json.Parse(FrameworkProofContent.Read("emergency-material-reserves.json")).Get("logistics_policies"));
                    root.Get("entities")[0].Get("components").Set(publicOutlet ? "contract_outlet_policy" : "logistics_policy", Json.Parse(publicOutlet ? "{\"profile\":\"reserve.materials\"}" : "{\"profiles\":[{\"port\":null,\"profile\":\"reserve.materials\"}]}"));
                });
                var request = new NewGameRequest { Scenario = "points", Seed = 719 }; request.Mods.Add("proof.demands"); var game = factory.NewGame(request);
                c.That(Offers(game).Count == 0, "declared emergency owner alone supplies no fabricated quantity");
                Ordinary(c, game);
                c.That(TestKit.Order(game, 0, new SubmitProofDemandCommand()).Accepted, "public-only module registers actual authored demand through ordinary command");
                var facts = Offers(game); c.That(facts.Count == 1 && facts[0].Emergency == emergency, "actual public demand owner supplies classification");
                c.Equal(Fix.FromInt(emergency ? 8 : 20), facts[0].Requested, "public demand aggregation respects explicit emergency boundary");
                c.Equal(LogisticsPolicyCodes.Reserve, game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(Find(game, "source").Id,
                    Find(game, "destination").Id, Supply(game), Fix.FromInt(8)).Blocker, "public emergency identity cannot be inherited by unrelated manual haul");
                if (!physical) continue;
                Configure(c, game, haulOnly: true); var haul = Assigned(game);
                if (!emergency)
                {
                    c.That(haul == null, "ordinary public mod request cannot borrow emergency-only stock");
                    c.Equal(Fix.FromInt(20), Stock(game, "source"), "ordinary module request does not alter stock"); continue;
                }
                c.That(haul?.IsHaul == true && haul.Amount == Fix.FromInt(8), "public emergency source acquires only its real current shortage");
                Until(game, () => haul.Loaded.IsPositive, "public demand physical partial load");
                c.That(TestKit.Order(game, 0, new RetireProofDemandCommand { Withdraw = true }).Accepted, "module retires actual demand without cancelling committed cargo");
                var twin = factory.Load(game.Save()); Until(game, () => !haul.IsLive, "retired public demand delivery", twin);
                c.Equal(game.Hash(), twin.Hash(), "public source retirement/physical receipts restore through declared owner");
                c.Equal(Fix.FromInt(12), Stock(game, "source"), "retired public demand cannot issue extra reserve shipments");
                c.Equal(Fix.FromInt(8), Stock(game, "destination"), "existing carrier delivered real retired-demand cargo");
                c.That(Offers(game).Count == 1 && !Offers(game)[0].Emergency, "ordinary deficit never retains expired mod emergency authority");
            }
        }
    }
}
