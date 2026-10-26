using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    internal static class HaulLoadPolicyTests
    {
        internal static void Register()
        {
            TestRunner.Add("Haul load policies: authored contracts, deadlines and canonical admission", Admission);
            TestRunner.Add("Haul load policies: physical batches, dispatch history and saved continuation", Physical);
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static GameFactory Factory(bool extension = false, int fault = 0, Action<JsonValue> edit = null, bool extraCarrier = false)
        {
            var root = Content.Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            var parts = root.Get("entities")[2].Get("components");
            parts.Set(extension ? "contract_haul_load_policy" : "haul_load_policy", Content.Json.Parse(extension
                ? "{\"profile\":\"load.batch\",\"fault\":" + fault + "}" : "{\"profile\":\"load.batch\",\"states\":{\"disabled\":\"blocked\"}}"));
            if (extraCarrier)
            {
                var second = root.Get("entities")[2].Clone(); second.Set("id", JsonValue.Of("standby"));
                second.Get("components").Remove(extension ? "contract_haul_load_policy" : "haul_load_policy");
                root.Get("entities").Add(second); StartupContentAuthoring.Add(root, "standby", "standby", 0, second.Get("components").Get("point_presence"));
            }
            var load = Content.Json.Parse(FrameworkProofContent.Read("haul-load-policies.json")); edit?.Invoke(load);
            return PointGame.Factory(Text(root), retryProfiles: FrameworkProofContent.Read("haul-retry.json"),
                additionalDocuments: new Dictionary<string, string> { ["haul-load-policies.json"] = Text(load) });
        }
        private static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "points", Seed = 677 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request); }
        private static Entity Find(SimGame game, string id)
        { foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException(id); }
        private static ResourceId Resource(SimGame game) => game.World.Content.ResourceId("supply");
        private static LogisticsView View(SimGame game) => game.Seat(0).View.Feature<LogisticsView>();
        private static HaulLoadSettings Initial(SimGame game, string profile)
            => game.World.Content.Get<IHaulLoadProfile>(profile).Initial;
        private static CommandResult Select(SimGame game, string profile, int seat = 0)
            => TestKit.Order(game, seat, new ConfigureHaulLoadPolicyCommand { Worker = Find(game, "machine").Id, Settings = Initial(game, profile) });
        private static int Route(SimGame game, int batch)
        {
            var result = TestKit.Order(game, 0, new AddRouteCommand { From = Find(game, "source").Id, To = Find(game, "destination").Id,
                Resource = Resource(game), Settings = new LogisticsRouteSettings(true, Fix.FromInt(batch), 1) });
            if (!result.Accepted) throw new InvalidOperationException(result.Refusal.ToString()); return checked((int)result.Arg0);
        }
        private static LogisticsDispatchOffer Offer(SimGame game, int route)
        { var offers = new List<LogisticsDispatchOffer>(); View(game).CopyQueue(offers); foreach (var offer in offers) if (offer.Route == route) return offer; throw new InvalidOperationException("missing route offer"); }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 250 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done())
            {
                var machine = Find(game, "machine"); var job = game.World.Services.Get<WorkBoard>().Job(machine.Get<IWorker>().Job);
                throw new InvalidOperationException(stage + " timeout tick=" + game.Tick + " position=" + machine.Get<PointPresence>().Position +
                    " worker=" + machine.Id + " job=" + job?.Id + "/" + job?.State + "/" + job?.Blocker +
                    " source=" + Find(game, "source").Get<IOutletPort>().Amount(Resource(game)) + " destination=" + Find(game, "destination").Get<IInletPort>().Amount(Resource(game)));
            }
        }
        private static void Admission(Check c)
        {
            EditedAdmission(c);
            var factory = Factory(); var game = New(factory); var machine = Find(game, "machine"); var view = View(game); ulong hash = game.Hash();
            c.Equal(Code.None, view.ReadHaulLoadPolicy(machine.Id, out var snapshot), "owned load policy reads through engine");
            c.Equal("load.batch", snapshot.Profile, "actual authored profile selected"); c.Equal(100L, snapshot.MaximumWaitTicks, "wait duration uses session time");
            c.Equal(Fix.FromInt(4), snapshot.MinimumLoad.AmountOf(Resource(game)), "minimum uses native resource quantity");
            c.Throws<NotSupportedException>(() => ((IList<ResourceAmount>)snapshot.MinimumLoad.Lines).Clear(), "minimum is immutable");
            c.Equal(hash, game.Hash(), "private read changes no owner state");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<LogisticsView>().ReadHaulLoadPolicy(machine.Id, out var hidden), "foreign read refuses"); c.That(hidden == null, "foreign policy facts do not leak");
            c.Equal(HaulLoadPolicyCodes.NotPolicy, view.ReadHaulLoadPolicy(Find(game, "source").Id, out _), "no policy is not a fabricated default");
            int route = Route(game, 2); var waiting = Offer(game, route);
            c.Equal(HaulStatus.PolicyBlocked, waiting.Haul.Status, "short automatic load waits"); c.Equal(HaulLoadPolicyCodes.Minimum, waiting.Haul.Blocker, "typed minimum blocker");
            c.Equal(0, machine.Get<IWorker>().Job, "waiting creates no native job"); c.Equal(Fix.Zero, Find(game, "source").Get<IOutletPort>().ReservedOut(Resource(game)), "waiting reserves no material");
            long original = waiting.CreatedTick; hash = game.Hash(); Offer(game, route); c.Equal(hash, game.Hash(), "queue read is pure");
            c.That(view.PreviewHaul(Find(game, "source").Id, Find(game, "destination").Id, Resource(game), Fix.FromInt(2)).Status == HaulStatus.Ready, "explicit haul preview is not automatic standing batching");
            c.Equal(Code.None, view.PreviewHaulLoadPolicy(machine.Id, Initial(game, "load.immediate")), "canonical configure preview accepts");
            c.That(Select(game, "load.immediate").Accepted, "normal command selects authored immediate profile");
            Reach(game, () => machine.Get<IWorker>().Job != 0, "immediate profile assignment");
            c.That(machine.Get<IWorker>().Job != 0, "ordinary assignment reacts without host scheduler");
            c.Equal("load.batch", snapshot.Profile, "captured snapshot does not track later selection");
            c.Equal(original, Offer(game, route).CreatedTick, "priority age is not reset by policy or admission");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "current profile/history save restores exactly");
            foreach (var invalid in new[] { new HaulLoadSettings("missing.load", true, Bill.Empty, GameDuration.Zero, false), Initial(game, "load.too-large") })
            { Code refusal = view.PreviewHaulLoadPolicy(machine.Id, invalid); c.That(!refusal.IsNone, "invalid profile refuses " + invalid.Profile); c.Equal(refusal,
                TestKit.Order(game, 0, new ConfigureHaulLoadPolicyCommand { Worker = machine.Id, Settings = invalid }).Refusal, "preview/command refusal parity " + invalid.Profile); }
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<LogisticsView>().PreviewHaulLoadPolicy(machine.Id, Initial(game, "load.off")), "foreign preview refuses");
            c.That(!Select(game, "load.off", 1).Accepted, "foreign command cannot switch another carrier");
            var native = game.World.Content.Get<IHaulLoadProfile>("load.batch");
            c.Equal(HaulLoadPolicyCodes.Minimum, native.CheckLoad(new HaulLoadContext(game.World.Time, Resource(game), Fix.One, 99, false), native.Initial), "just before deadline waits");
            c.Equal(Code.None, native.CheckLoad(new HaulLoadContext(game.World.Time, Resource(game), Fix.One, 100, false), native.Initial), "exact deadline admits positive smaller load");
            c.Equal(HaulLoadPolicyCodes.Minimum, native.CheckLoad(new HaulLoadContext(game.World.Time, Resource(game), Fix.One, 0, true), native.Initial), "emergency needs explicit bypass");
            c.Equal(Code.None, game.World.Content.Get<IHaulLoadProfile>("load.emergency").CheckLoad(new HaulLoadContext(game.World.Time, Resource(game), Fix.One, 0, true), Initial(game, "load.emergency")), "authored emergency bypass applies");
            c.Equal(HaulLoadPolicyCodes.Disabled, game.World.Content.Get<IHaulLoadProfile>("load.off").CheckLoad(new HaulLoadContext(game.World.Time, Resource(game), Fix.FromInt(4), 100, true), Initial(game, "load.off")), "disabled profile is not bypassed by emergency/deadline");
            c.Equal(StateCodecTests.ExpectedLogisticsSchema, game.World.Services.Get<LogisticsService>().StateSchemaVersion, "history has one current codec"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no old command/history save reader");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "load rules belong to engine build");
            Strict(c); Providers(c); SavedAdmission(c);
        }
        private static void Strict(Check c)
        {
            foreach (string field in new[] { "enabled", "minimum_load", "maximum_wait", "emergency_bypass" })
                c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("haul_load_profiles")[0].Remove(field))), "required JSON field " + field);
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("haul_load_profiles")[0].Set("minimum_load", JsonValue.Null()))), "null minimum does not invent an empty policy");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("haul_load_profiles")[0].Set("minimum_load", Content.Json.Parse("{\"supply\":0.5}")))), "fractional discrete native minimum refuses");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("haul_load_profiles")[0].Set("minimum_load", Content.Json.Parse("{\"supply\":9}")))), "initial minimum beyond actual cargo refuses before gameplay");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("haul_load_profiles")[0].Get("maximum_wait").Set("numerator", JsonValue.Number("92233720368547758070")))), "unrepresentable session deadline refuses without clamping");
            var valid = New(Factory(edit: root => root.Get("haul_load_profiles")[0].Get("maximum_wait").Set("numerator", JsonValue.Number("3000000000"))));
            View(valid).ReadHaulLoadPolicy(Find(valid, "machine").Id, out var snapshot); c.Equal(3000000000L, snapshot.MaximumWaitTicks, "wide game time has no artificial 32-bit cap");
        }
        private static void Providers(Check c)
        {
            var factory = Factory(extension: true); var game = New(factory); var machine = Find(game, "machine");
            c.Equal(Code.None, View(game).ReadHaulLoadPolicy(machine.Id, out _), "independent public policy uses canonical read");
            c.That(Select(game, "load.odd").Accepted, "normal command selects independent authored profile");
            int route = Route(game, 4); c.Equal(new Code("proof.odd-load"), Offer(game, route).Haul.Blocker, "new profile rule affects same dispatcher without scheduler edits");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "public selection/current wait survive save");
            c.That(TestKit.Order(game, 0, new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(true, Fix.FromInt(3), 1) }).Accepted, "ordinary route settings change packed load");
            Reach(game, () => machine.Get<IWorker>().Job != 0, "public odd-load assignment");
            c.That(machine.Get<IWorker>().Job != 0, "independent odd-load rule admits actual three units");
            foreach (int fault in new[] { 1, 2 })
            {
                var broken = New(Factory(extension: true, fault: fault));
                c.Throws<InvalidOperationException>(() => View(broken).ReadHaulLoadPolicy(Find(broken, "machine").Id, out _), "mutating/missing public profile faults " + fault);
            }
            foreach (int fault in new[] { 3, 4 })
            {
                var broken = New(Factory(extension: true, fault: fault));
                c.Throws<InvalidOperationException>(() => HaulLoadPolicyContracts.Configure(broken.World, Find(broken, "machine"), Initial(broken, "load.immediate")), "no-op commit/mutating admission faults " + fault);
            }
            var other = New(Factory(extraCarrier: true)); int choice = Route(other, 2);
            c.Equal(Find(other, "standby").Id, OfferCarrier(other, choice), "ready standby carrier is considered instead of blocking all candidates");
        }
        private static EntityId OfferCarrier(SimGame game, int route)
        { foreach (var job in game.World.Services.Get<WorkBoard>().Jobs) if (job.IsLive && job.Route == route) return job.Worker; return Offer(game, route).Haul.Carrier; }
        private static void SavedAdmission(Check c)
        {
            var factory = Factory(); var game = New(factory); var machine = Find(game, "machine"); var part = machine.Get<HaulLoadPolicy>(); byte[] bytes;
            using (var stream = new MemoryStream()) { var writer = new StateWriter(stream); HaulLoadSettings.Sync(writer,
                new HaulLoadSettings("missing.load", true, Bill.Empty, GameDuration.Zero, false)); writer.Flush(); bytes = stream.ToArray(); }
            c.Throws<CorruptStateException>(() => part.Sync(new StateReader(bytes, 0, bytes.Length)), "unknown saved profile rejects before assignment");
            c.Equal("load.batch", part.ReadLoadPolicy().Profile, "rejected native profile decode leaves old selection intact");
            c.Throws<CorruptStateException>(() => part.Sync(new StateReader(bytes, 0, bytes.Length - 1)), "truncated profile rejects before assignment");
            c.Equal("load.batch", part.ReadLoadPolicy().Profile, "truncated decode does not mutate selection");
            Route(game, 2); game.Seat(0).Submit(new ConfigureHaulLoadPolicyCommand { Worker = machine.Id, Settings = Initial(game, "load.immediate") });
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending normal policy command is in current save");
            for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); }
            c.Equal("load.immediate", machine.Get<IHaulLoadPolicy>().ReadLoadPolicy().Profile, "pending command uses normal engine commit on continuation");
            c.Equal(game.Hash(), twin.Hash(), "pending selection and admission continue deterministically");
            var malformed = New(factory); Route(malformed, 2); var logistics = malformed.World.Services.Get<LogisticsService>();
            using (var stream = new MemoryStream()) { var writer = new StateWriter(stream); logistics.Sync(writer); writer.Flush(); bytes = stream.ToArray(); }
            Array.Copy(BitConverter.GetBytes(malformed.Tick + 1), 0, bytes, bytes.Length - 8, 8);
            c.Throws<CorruptStateException>(() => logistics.Sync(new StateReader(bytes, 0, bytes.Length)), "future native dispatch history rejects without old-reader repair");
        }
        private static void Physical(Check c)
        { EditedPhysical(c, false); EditedPhysical(c, true); Batches(c, false); Batches(c, true); Demand(c); }
        private static HaulLoadSettings Settings(SimGame game, int minimum, GameDuration wait, bool enabled = true, bool emergency = false)
            => new HaulLoadSettings("load.batch", enabled, new Bill(new[] { new ResourceAmount(Resource(game), Fix.FromInt(minimum)) }), wait, emergency);
        private static void EditedAdmission(Check c)
        {
            var factory = Factory(); var game = New(factory); var machine = Find(game, "machine"); var view = View(game);
            var edited = Settings(game, 2, GameDuration.Decimal("1.5", DurationUnit.Minute), emergency: true);
            view.ReadHaulLoadPolicy(machine.Id, out var retained); ulong before = game.Hash(); long step = game.Clock.StepIndex;
            c.Equal(Code.None, view.PreviewHaulLoadPolicy(machine.Id, edited), "engine preview admits player-authored minimum/wait/bypass");
            c.Equal(Code.None, game.Seat(0).Preview(new ConfigureHaulLoadPolicyCommand { Worker = machine.Id, Settings = edited }), "command and management edit admission agree");
            c.Equal(before, game.Hash(), "edit previews create no policy/job/resource promises");
            c.Equal(step, game.Clock.StepIndex, "policy preview advances no game clock");
            c.That(TestKit.Order(game, 0, new ConfigureHaulLoadPolicyCommand { Worker = machine.Id, Settings = edited }).Accepted, "ordinary player command applies full editable settings");
            view.ReadHaulLoadPolicy(machine.Id, out var actual);
            c.Equal(Fix.FromInt(2), actual.MinimumLoad.AmountOf(Resource(game)), "actual native minimum changes without another JSON profile");
            c.Equal(2L, actual.MaximumWaitTicks, "fractional game duration rounds once against the existing session quantum");
            c.That(actual.EmergencyBypass && actual.Settings.MaximumWait.Equals(edited.MaximumWait), "exact duration and bypass remain exposed, not rounded stored values");
            c.Equal(Fix.FromInt(4), retained.MinimumLoad.AmountOf(Resource(game)), "retained policy snapshot is detached from later edits");
            c.Equal(Fix.FromInt(4), Initial(game, "load.batch").MinimumLoad.AmountOf(Resource(game)), "runtime edits never mutate shared authored initial policy");
            c.Throws<NotSupportedException>(() => ((IList<ResourceAmount>)actual.MinimumLoad.Lines).Clear(), "edited native minimum bill is immutable");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "exact decimal wait and complete edited settings save/hash through one codec");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<LogisticsView>().PreviewHaulLoadPolicy(machine.Id, edited), "foreign edit cannot inspect provider settings");
            c.Equal(Codes.NotFound, TestKit.Order(game, 1, new ConfigureHaulLoadPolicyCommand { Worker = machine.Id, Settings = edited }).Refusal, "foreign edit command matches preview");
            foreach (var invalid in new[] {
                (HaulLoadSettings)null,
                Settings(game, 9, GameDuration.Minutes(1)),
                new HaulLoadSettings("load.batch", true, new Bill(new[] { new ResourceAmount(Resource(game), Fix.FromRatio(1, 2)) }), GameDuration.Zero, false),
                new HaulLoadSettings("load.batch", false, new Bill(new[] { new ResourceAmount(new ResourceId(99999), Fix.One) }), GameDuration.Zero, true),
                Settings(game, 4, GameDuration.Days(long.MaxValue))
            })
            {
                var refusal = view.PreviewHaulLoadPolicy(machine.Id, invalid);
                c.Equal(HaulLoadPolicyCodes.Invalid, refusal, "invalid missing/native/capacity/time settings refuse explicitly");
                c.Equal(refusal, TestKit.Order(game, 0, new ConfigureHaulLoadPolicyCommand { Worker = machine.Id, Settings = invalid }).Refusal, "invalid edit command matches pure preview");
            }
            var profile = game.World.Content.Get<IHaulLoadProfile>("load.batch");
            c.Equal(Code.None, profile.CheckLoad(new HaulLoadContext(game.World.Time, Resource(game), Fix.One, 0, true), edited), "current bypass is honored by the same authored rule");
            c.Equal(HaulLoadPolicyCodes.Disabled, profile.CheckLoad(new HaulLoadContext(game.World.Time, Resource(game), Fix.FromInt(8), 100, true), Settings(game, 2, GameDuration.Zero, false, true)), "disabled runtime policy cannot be bypassed by emergency/age/large load");
            foreach (GameDuration duration in new[] { GameDuration.Zero, GameDuration.Minutes(0), GameDuration.Decimal("0.125e2", DurationUnit.Minute), new GameDuration(3, 2, DurationUnit.Minute), GameDuration.Hours(3000000000) })
            {
                byte[] bytes;
                using (var stream = new MemoryStream()) { var writer = new StateWriter(stream); GameDuration.Sync(writer, duration); writer.Flush(); bytes = stream.ToArray(); }
                var restored = GameDuration.Sync(new StateReader(bytes, 0, bytes.Length), default);
                c.That(duration.Equals(restored), "duration codec preserves exact authored literal/rational/units/wide values");
            }
            foreach (var bad in new[] { (Text: (string)null, Scale: 1L, Divisor: 0L), (Text: "-1", Scale: 1L, Divisor: 1L),
                (Text: "NaN", Scale: 1L, Divisor: 1L), (Text: "1", Scale: 0L, Divisor: 1L), (Text: "1", Scale: 1L, Divisor: 0L) })
            {
                byte[] bytes; using (var stream = new MemoryStream())
                { var writer = new StateWriter(stream); string text = bad.Text; long scale = bad.Scale, divisor = bad.Divisor; writer.String(ref text); writer.Long(ref scale); writer.Long(ref divisor); writer.Flush(); bytes = stream.ToArray(); }
                c.Throws<CorruptStateException>(() => GameDuration.Sync(new StateReader(bytes, 0, bytes.Length), default), "corrupt exact duration refuses without another time conversion/default");
            }
        }
        private static void EditedPhysical(Check c, bool extension)
        {
            var factory = Factory(extension: extension); var game = New(factory); var isolated = New(factory); var machine = Find(game, "machine");
            int route = Route(game, 2); long created = Offer(game, route).CreatedTick;
            c.Equal(HaulLoadPolicyCodes.Minimum, Offer(game, route).Haul.Blocker, "initial JSON batch policy waits without cargo or jobs");
            var edited = Settings(game, 4, GameDuration.Decimal("6.5", DurationUnit.Minute), emergency: true);
            game.Seat(0).Submit(new ConfigureHaulLoadPolicyCommand { Worker = machine.Id, Settings = edited });
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending full settings command has the same self-described save/command stream");
            game.Clock.Pause(); long step = game.Clock.StepIndex; ulong paused = game.Hash(); c.Equal(0, game.Update(), "sole-clock pause cannot commit pending policy or fund a wait deadline");
            c.Equal(step, game.Clock.StepIndex, "policy has no independently advancing timer"); c.Equal(paused, game.Hash(), "paused pending command/progress/resources remain unchanged"); game.Clock.Resume();
            Tick(game); Tick(twin); View(game).ReadHaulLoadPolicy(machine.Id, out var changed);
            c.Equal(7L, changed.MaximumWaitTicks, "current fractional wait uses existing request-age timing");
            c.Equal(created, Offer(game, route).CreatedTick, "settings edit preserves original request fairness age");
            Reach(game, () => machine.Get<IWorker>().Job != 0, "edited deadline assignment", twin);
            long admitted = Offer(game, route).LastDispatchTick;
            c.That(admitted - created >= 7 && admitted - created < 20, "edited maximum wait admits the real small load at bounded request age, not original hundred-minute delay");
            var job = game.World.Services.Get<WorkBoard>().Job(machine.Get<IWorker>().Job);
            Reach(game, () => machine.Get<ITransporter>().Cargo.Amount(Resource(game)).IsPositive, "actual edited-policy pickup", twin);
            foreach (var session in new[] { game, twin })
                c.That(TestKit.Order(session, 0, new ConfigureHaulLoadPolicyCommand { Worker = Find(session, "machine").Id,
                    Settings = Settings(session, 6, GameDuration.Minutes(50), enabled: false, emergency: true) }).Accepted, "player disables future batching through complete current settings");
            c.Equal(job.Id, machine.Get<IWorker>().Job, "minimum/disable edits do not cancel accepted shipment or erase cargo");
            c.Equal(admitted, Offer(game, route).LastDispatchTick, "editing batching cannot reset successful dispatch history");
            if (!extension) c.That(machine.State != new Code("point.blocked"), "disabled future intake does not mislabel the accepted unblocked physical job");
            Reach(game, () => !job.IsLive, "accepted edited-policy physical delivery", twin);
            c.Equal(JobState.Done, job.State, "accepted trip completes physically even while future automatic dispatch is disabled");
            c.Equal(Fix.FromInt(2), Find(game, "destination").Get<IInletPort>().Amount(Resource(game)), "exact real partial batch is delivered once");
            c.Equal(Fix.FromInt(18), Find(game, "source").Get<IOutletPort>().Amount(Resource(game)), "no policy edit grants, duplicates or destroys supply");
            c.Equal(HaulLoadPolicyCodes.Disabled, Offer(game, route).Haul.Blocker, "next automatic matching reads edited disabled settings");
            c.Equal(game.Hash(), twin.Hash(), "edited settings/physical claims/native stock/age save continuation is deterministic");
            View(isolated).ReadHaulLoadPolicy(Find(isolated, "machine").Id, out var untouched);
            c.Equal(100L, untouched.MaximumWaitTicks, "other session retains its own initial policy, not the edited game value");
            c.Equal(Fix.FromInt(4), untouched.MinimumLoad.AmountOf(Resource(isolated)), "shared JSON does not share mutable runtime policy state");
            foreach (var session in new[] { game, twin })
                c.That(TestKit.Order(session, 0, new ConfigureHaulLoadPolicyCommand { Worker = Find(session, "machine").Id,
                    Settings = Settings(session, 2, GameDuration.Minutes(50)) }).Accepted, "lowering minimum enables next small batch without waiting or adding a profile");
            Reach(game, () => machine.Get<IWorker>().Job != 0 && machine.Get<IWorker>().Job != job.Id, "edited minimum normal scheduled dispatch", twin);
            c.That(machine.Get<IWorker>().Job != 0 && machine.Get<IWorker>().Job != job.Id, "existing dispatcher starts a new physically funded batch from edited threshold");
            c.Equal(created, Offer(game, route).CreatedTick, "second edited dispatch still preserves original fairness age");
            c.Equal(game.Hash(), twin.Hash(), "native/public edited threshold dispatch shares existing deterministic owner");
        }
        private static void Batches(Check c, bool extension)
        {
            var factory = Factory(extension: extension); var game = New(factory); var isolated = New(factory); var machine = Find(game, "machine");
            var board = game.World.Services.Get<WorkBoard>(); int route = Route(game, 4); var first = board.Job(machine.Get<IWorker>().Job);
            c.That(first?.IsHaul == true && first.Amount == Fix.FromInt(4), "full minimum batch starts immediately through native job");
            long firstDispatch = Offer(game, route).LastDispatchTick, created = Offer(game, route).CreatedTick;
            c.That(TestKit.Order(game, 0, new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(true, Fix.FromInt(2), 1) }).Accepted, "player reduces future route load without changing acquired batch");
            Reach(game, () => !first.IsLive, "first physical batch");
            c.Equal(Fix.FromInt(4), Find(game, "destination").Get<IInletPort>().Amount(Resource(game)), "first batch actually loads/travels/unloads");
            c.Equal(0, machine.Get<IWorker>().Job, "next small batch waits after recent actual dispatch");
            var wait = Offer(game, route); c.Equal(HaulLoadPolicyCodes.Minimum, wait.Haul.Blocker, "second short load does not reuse original route deadline");
            c.Equal(firstDispatch, wait.LastDispatchTick, "failed preview does not advance dispatch clock"); c.Equal(created, wait.CreatedTick, "fairness age remains original");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "waiting second batch restores exact history");
            Reach(game, () => machine.Get<IWorker>().Job != 0, "short batch deadline", twin); var second = board.Job(machine.Get<IWorker>().Job);
            c.That(second.Amount == Fix.FromInt(2), "deadline releases actual short amount, not fabricated minimum");
            c.That(Offer(game, route).LastDispatchTick - firstDispatch >= 100, "request waited the authored maximum interval");
            c.Equal(created, Offer(game, route).CreatedTick, "second admission preserves fairness age");
            c.That(Select(game, "load.off").Accepted && Select(twin, "load.off").Accepted, "disable future dispatch in both saved continuations");
            if (!extension) c.That(machine.State != new Code("point.blocked"), "Game.Update keeps accepted unblocked work state despite disabled future intake");
            c.Equal(second.Id, machine.Get<IWorker>().Job, "profile change cannot revoke acquired shipment");
            Reach(game, () => !second.IsLive, "grandfathered disabled shipment", twin);
            c.Equal(JobState.Done, second.State, "acquired short batch completes while automatic loading is disabled");
            c.Equal(Fix.FromInt(6), Find(game, "destination").Get<IInletPort>().Amount(Resource(game)), "only two real batches arrive exactly once");
            c.Equal(Fix.FromInt(14), Find(game, "source").Get<IOutletPort>().Amount(Resource(game)), "source supplied exact quantities");
            c.Equal(HaulLoadPolicyCodes.Disabled, Offer(game, route).Haul.Blocker, "next automatic shipment reports disabled policy");
            c.That(Select(game, "load.batch").Accepted && Select(twin, "load.batch").Accepted, "normal player command restores batching");
            c.Equal(HaulLoadPolicyCodes.Minimum, Offer(game, route).Haul.Blocker, "new small load waits from second admission, not ancient route creation");
            c.Equal(0, machine.Get<IWorker>().Job, "old route age cannot permanently bypass per-dispatch maximum wait");
            c.That(Select(game, "load.off").Accepted && Select(twin, "load.off").Accepted, "player pauses future dispatch without deleting history");
            for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); Tick(isolated); }
            c.Equal(game.Hash(), twin.Hash(), "history/profile/loaded continuation stays deterministic");
            c.Equal(Fix.Zero, Find(isolated, "destination").Get<IInletPort>().Amount(Resource(isolated)), "interleaved session has no request leakage");
        }
        private static void Demand(Check c)
        {
            var factory = Factory(); var game = New(factory); var machine = Find(game, "machine"); var destination = Find(game, "destination");
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = destination.Id, Resource = Resource(game),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(2), Fix.FromInt(2), Fix.FromInt(2), 1, LogisticsReserveAccess.Protected)) }).Accepted, "ordinary stock demand selects same automatic loading policy");
            var demands = new List<LogisticsDemandView>(); View(game).CopyDemands(demands); long created = demands[0].CreatedTick;
            c.Equal(0, machine.Get<IWorker>().Job, "short native demand waits without reserving remote stock");
            var twin = factory.Load(game.Save()); Reach(game, () => machine.Get<IWorker>().Job != 0, "native demand deadline", twin);
            View(game).CopyDemands(demands); c.That(demands[0].LastDispatchTick - created >= 100, "demand row owns wait history, not a carrier timer");
            c.Equal(created, demands[0].CreatedTick, "demand fairness age survives actual dispatch");
            var job = game.World.Services.Get<WorkBoard>().Job(machine.Get<IWorker>().Job);
            Reach(game, () => !job.IsLive, "native demand short delivery", twin);
            c.Equal(Fix.FromInt(2), destination.Get<IInletPort>().Amount(Resource(game)), "actual small demand is physically satisfied");
            c.Equal(game.Hash(), twin.Hash(), "native demand deadline/load/delivery save twins match");
            View(game).CopyDemands(demands); long admitted = demands[0].LastDispatchTick;
            foreach (var session in new[] { game, twin })
                c.That(TestKit.Order(session, 0, new ConfigureStockPolicyCommand { Store = Find(session, "destination").Id, Resource = Resource(session),
                    Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(4), Fix.FromInt(4), Fix.FromInt(4), 1, LogisticsReserveAccess.Protected)) }).Accepted, "stock edit uses same existing demand row");
            View(game).CopyDemands(demands); c.Equal(admitted, demands[0].LastDispatchTick, "surviving demand update preserves successful admission history");
            c.Equal(created, demands[0].CreatedTick, "demand edit does not reset original fairness age");
            var offers = new List<LogisticsDispatchOffer>(); View(game).CopyQueue(offers);
            c.Equal(HaulLoadPolicyCodes.Minimum, offers[0].Haul.Blocker, "updated short gap waits from recent dispatch rather than original demand age");
            Reach(game, () => destination.Get<IInletPort>().Amount(Resource(game)) == Fix.FromInt(4), "updated native demand delivery", twin);
            c.Equal(game.Hash(), twin.Hash(), "updated demand clock and second physical delivery remain deterministic");
        }
    }
}
