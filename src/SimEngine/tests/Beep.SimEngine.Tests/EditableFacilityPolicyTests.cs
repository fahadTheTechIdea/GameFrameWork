using System;
using System.IO;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.FacilitySelectionTests;

namespace Beep.SimEngine.Tests
{
    // Explicit authored fixtures, player commands and bounded game updates; no service rules.
    internal static class EditableFacilityPolicyTests
    {
        private static readonly Code Service = new Code("fleet.service"), Nearest = new Code("service.nearest"),
            HomeFirst = new Code("service.home-first"), Later = new Code("service.later");
        internal static void Register()
        {
            TestRunner.Add("Facility policies: editable settings, private preview and protocol guards", Contracts);
            TestRunner.Add("Facility policies: active physical service, changed preferences and saved continuation", Physical);
        }
        private static GameFactory Fixture(bool publicSource, bool extension, int fault = 0, Action<JsonValue> edit = null)
            => Factory(extension, edit: root =>
            {
                root.Set("proof_facility_policies", Json.Parse("[{\"id\":\"service.later\",\"node\":\"service\",\"prefer_later\":true,\"fault\":0}]"));
                root.Set("activity_facility_policies", Json.Parse("[{\"id\":\"service.bad-node\",\"node\":\"settle\",\"prefer_home\":false}]"));
                var parts = Parts(root, extension); var row = parts.Get("recurring_activity");
                if (publicSource)
                {
                    parts.Remove("recurring_activity"); row.Remove("conditions");
                    row.Set("fault", JsonValue.Number(fault.ToString())); row.Set("idle_only", JsonValue.Of(false));
                    parts.Set("proof_service_recurring", row);
                }
                edit?.Invoke(root);
            });
        private static ConfigureRecurringPlanCommand Edit(SimGame game, bool extension, Code policy, bool enabled = false, long minutes = 1000)
            => new ConfigureRecurringPlanCommand { Entity = Find(game, Actor(extension)).Id, Source = Service,
                Settings = new RecurringPlanSettings(enabled, 60, GameDuration.Minutes(minutes), policy) };
        private static void Configure(Check c, SimGame game, bool extension, Code policy, bool enabled = false, long minutes = 1000)
        {
            var command = Edit(game, extension, policy, enabled, minutes);
            c.Equal(Code.None, View(game).PreviewRecurringPlan(command.Entity, Service, command.Settings), "same preview admits actual profile/settings");
            c.That(Command(game, command).Accepted, "normal player command configures sole source settings");
            c.Equal(policy, SourceView(game, extension).Schedule.Settings.FacilityPolicy, "exact current policy is visible privately");
        }
        private static byte[] Encode(IRecurringPlanSource source)
        { using var stream = new MemoryStream(); ((Component)source).Sync(new StateWriter(stream)); return stream.ToArray(); }
        private static void Decode(IRecurringPlanSource source, byte[] bytes)
            => ((Component)source).Sync(new StateReader(bytes, 0, bytes.Length));
        private static byte[] Payload(string policy)
        {
            using var stream = new MemoryStream(); var writer = new StateWriter(stream);
            bool present = true, enabled = false; int priority = 60;
            writer.Bool(ref present); writer.Bool(ref enabled); writer.Int(ref priority);
            GameDuration.Sync(writer, GameDuration.Minutes(1)); writer.String(ref policy);
            long posted = -1, plan = 0; writer.Long(ref posted); writer.Long(ref plan); return stream.ToArray();
        }
        private static void Contracts(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "single editable policy protocol");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "source and command settings codec replaces old payload");
            foreach (bool publicSource in new[] { false, true }) foreach (bool extension in new[] { false, true })
            {
                var factory = Fixture(publicSource, extension); var game = New(factory);
                var actor = Find(game, Actor(extension)); var source = actor.Get<IRecurringPlanSource>(Service);
                var original = SourceView(game, extension); var offers = Read(game, extension); ulong hash = game.Hash();
                c.That(ReferenceEquals(original.Schedule.Settings, actor.Def.Component<IRecurringPlanDefinition>(Service).Initial), "JSON initial value becomes sole source settings");
                c.Equal(Nearest, offers.Policy, "detached facility facts capture actual current profile");
                c.Equal(hash, game.Hash(), "settings/selection reads remain pure");
                c.Equal(Codes.NotOwned, View(game, 1).PreviewRecurringPlan(actor.Id, Service, Edit(game, extension, HomeFirst).Settings), "foreign edit refuses authority");
                c.Equal(Codes.NotFound, View(game, 1).ReadRecurringPlan(actor.Id, Service, out _), "current preference stays private");
                foreach (var pair in new[] { (Code.None, ActivityFacilityCodes.NotSelected), (new Code("missing"), ActivityFacilityCodes.Unknown),
                    (new Code("service.bad-node"), ActivityFacilityCodes.Incompatible) })
                {
                    var command = Edit(game, extension, pair.Item1);
                    c.Equal(pair.Item2, View(game).PreviewRecurringPlan(actor.Id, Service, command.Settings), "invalid current preference has typed preview");
                    c.Equal(pair.Item2, Command(game, command).Refusal, "command refuses identically without fallback");
                    c.Equal(Nearest, SourceView(game, extension).Schedule.Settings.FacilityPolicy, "refused preference leaves actual settings intact");
                    c.Equal(0L, SourceView(game, extension).Schedule.LastPlan, "refusal creates no service history");
                }
                Configure(c, game, extension, Later);
                c.Equal(Find(game, "destination").Id, Read(game, extension).Target, "public ranked policy can replace authored nearest policy");
                c.Equal(Nearest, offers.Policy, "retained selection does not mutate with edit");
                c.Equal(Nearest, original.Schedule.Settings.FacilityPolicy, "retained settings do not mutate with edit");
                c.Equal(-1L, SourceView(game, extension).Schedule.LastPostedTick, "disabled edits never fabricate or rebase posting time");
                Configure(c, game, extension, Nearest);
                c.Equal(Find(game, "source").Id, Read(game, extension).Target, "switching back uses engine route cost");
                c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "plan/jobs/origin payload unchanged");
                c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "same work owner declares mutable policy semantics");
                var commandPending = Edit(game, extension, Later); game.Seat(0).Submit(commandPending);
                var saved = factory.Load(game.Save()); c.Equal(game.Hash(), saved.Hash(), "pending profile command saves through same command codec");
                Step(game, saved); c.Equal(game.Hash(), saved.Hash(), "pending command applies identically after restore");
                c.Equal(Later, Read(saved, extension).Policy, "restored pending policy becomes authoritative selection");
                var clean = Encode(source); hash = game.Hash();
                c.Throws<CorruptStateException>(() => Decode(source, Payload("bad policy")), "malformed profile identity refuses staged decode");
                c.Equal(hash, game.Hash(), "failed structural decode leaves settings/history unchanged");
                Decode(source, Payload("missing"));
                c.Throws<CorruptStateException>(() => RecurringPlanContracts.ReadSchedule(game.World, source), "unresolved restored profile cannot become usable schedule");
                c.Throws<CorruptStateException>(() => game.Save(), "whole-session checkpoint refuses unresolved actual profile");
                Decode(source, clean); c.Equal(hash, game.Hash(), "diagnostic fixture restores actual owner through same codec");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "edited disabled schedule restores exactly");
            }
            c.Throws<ArgumentException>(() => new RecurringPlanSettings(false, 1, GameDuration.Minutes(1), new Code("bad policy")), "invalid identity cannot enter settings");
            foreach (bool publicSource in new[] { false, true })
            {
                c.Throws<ContentException>(() => New(Fixture(publicSource, false, edit: root => Parts(root).Get(publicSource ? "proof_service_recurring" : "recurring_activity").Remove("facility_policy"))), "explicit profile field is required for either provider");
                foreach (var initial in new[] { JsonValue.Null(), JsonValue.Of("missing"), JsonValue.Of("service.bad-node"), JsonValue.Number("1") })
                    c.Throws<ContentException>(() => New(Fixture(publicSource, false, edit: root =>
                        Parts(root).Get(publicSource ? "proof_service_recurring" : "recurring_activity").Set("facility_policy", initial))), "invalid initial profile refuses before first game update");
                c.Throws<ContentException>(() => New(Fixture(publicSource, false, edit: root =>
                    Parts(root).Get(publicSource ? "proof_service_recurring" : "recurring_activity").Set("target", JsonValue.Of("worker_home")))), "initial alternate facility profile cannot attach to fixed-home source");
            }
            var homeFactory = HomeServiceTests.Factory(); var home = New(homeFactory); var homeActor = Find(home, "machine.a");
            c.Equal(ActivityFacilityCodes.NotSelected, View(home).PreviewRecurringPlan(homeActor.Id, new Code("home.service"),
                new RecurringPlanSettings(false, 60, GameDuration.Minutes(1), HomeFirst)), "fixed-home source never gains an implicit alternate selector");
            var bad = New(Fixture(true, false, fault: 9)); var badActor = Find(bad, "machine.a");
            c.Equal(Code.None, View(bad).PreviewRecurringPlan(badActor.Id, Service, Edit(bad, false, Later).Settings), "dishonest public source has otherwise valid requested profile");
            bad.Seat(0).Submit(Edit(bad, false, Later)); bad.Update();
            c.That(bad.Fault != null, "ignored profile acknowledgement faults normal game update");
            c.Throws<InvalidOperationException>(() => bad.Save(), "dishonest partial commit cannot checkpoint");
        }
        private static void Physical(Check c)
        {
            foreach (bool publicSource in new[] { false, true }) foreach (bool extension in new[] { false, true })
                Run(c, publicSource, extension);
        }
        private static void Run(Check c, bool publicSource, bool extension)
        {
            var factory = Fixture(publicSource, extension); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            var game = New(factory);
            c.That(Command(game, new SetHomeCommand { Worker = Find(game, Actor(extension)).Id, Home = Find(game, "source").Id }).Accepted,
                "player assigns actual home through engine command before service");
            Configure(c, game, extension, Later, true);
            long first = SourceView(game, extension).Schedule.LastPlan;
            c.That(first > 0, "configured actual source posts ordinary physical plan");
            var order = Board(game).Order(Plan(game, first).Order); string actor = Actor(extension);
            c.Equal(Find(game, "destination").Id, order.Target, "public preference earns farther actual service facility");
            Reach(game, () => order.Node("service").Progress.WholeTicks >= 3, "physical preferred facility work begins");
            c.Equal(Fix.FromInt(29), Stock(game, "destination"), "existing stock owner consumes real spares");
            c.Equal(Fix.FromInt(4), Condition(game, actor), "partial timed work cannot repair early");
            long posted = SourceView(game, extension).Schedule.LastPostedTick;
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "mid-work preference saves with actual origin and claims");
            Configure(c, game, extension, HomeFirst, true); Configure(c, twin, extension, HomeFirst, true);
            c.Equal(first, SourceView(game, extension).Schedule.LastPlan, "active preference change cannot post another service");
            c.Equal(posted, SourceView(game, extension).Schedule.LastPostedTick, "preference change never rebases original posting");
            c.Equal(Find(game, "destination").Id, order.Target, "active trip/order never retargets to new preference");
            c.Equal(Service, Plan(game, first).Origin.Source, "source origin remains ordinary bound service");
            game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash(); long update = game.Clock.StepIndex;
            Step(game, twin); c.Equal(paused, game.Hash(), "pause freezes sole game-time service progress");
            c.Equal(update, game.Clock.StepIndex, "no independent service clock advances");
            game.Clock.Resume(); twin.Clock.Resume();
            c.That(Command(game, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { first }) }).Accepted, "player cancels actual partial service");
            c.That(Command(twin, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { first }) }).Accepted, "saved twin cancels through same command");
            c.Equal(Fix.FromInt(4), Condition(game, actor), "cancel never repairs worker or refunds spent stock");
            Configure(c, game, extension, HomeFirst, true, 1); Configure(c, twin, extension, HomeFirst, true, 1);
            Reach(game, () => SourceView(game, extension).Schedule.LastPlan > first, "changed policy becomes new future posting", twin);
            long second = SourceView(game, extension).Schedule.LastPlan;
            c.Equal(Find(game, "source").Id, Plan(game, second).Target, "new actual plan uses preferred supplied home despite current location");
            Configure(c, game, extension, Nearest, false); Configure(c, twin, extension, Nearest, false);
            c.Equal(Find(game, "source").Id, Plan(game, second).Target, "disabled future preference leaves ongoing physical repair intact");
            twin = factory.Load(game.Save()); Reach(game, () => Plan(game, second).State == PlanState.Done, "new ordinary physical service and settling completes", twin);
            c.Equal(Fix.FromInt(10), Condition(game, actor), "native/public condition repairs only after real work and departure");
            c.Equal(Fix.FromInt(29), Stock(game, "source"), "new facility actually supplies three spares and releases tools");
            c.Equal(Fix.FromInt(29), Stock(game, "destination"), "cancelled facility retains already consumed stock without compensation");
            c.Equal(PlanState.Cancelled, Plan(game, first).State, "old target origin/history survives preference changes");
            c.Equal(Find(game, actor).Id, Plan(game, first).Origin.Entity, "old source origin remains actual worker");
            c.Equal(game.Hash(), twin.Hash(), "physical profile-change/cancel/repost continuation is deterministic");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "ended plans restore despite different current preference");
            c.Equal(isolatedHash, isolated.Hash(), "interleaved untouched session has no process-global policy state");
        }
    }
}
