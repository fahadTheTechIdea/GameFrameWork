using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Ordinary player decisions and detached facts; all matching, reservations, travel and controls are engine-owned.
    internal static class ResourceControlDeclarationTests
    {
        internal const string Case = "Resource control declarations: JSON stock bands and outlet reserves share physical delivery and saved intent";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool direct, bool independent, out ResourceControlDeclarationProofModule module,
            InitializeProbe init = null, Action<JsonValue> edit = null, Action<JsonValue> extraEdit = null)
        {
            module = new ResourceControlDeclarationProofModule(direct, init);
            return OutletReserveActionTests.Factory(independent, edit, controlsFile: "stock-target-automation.json",
                nativeDestination: !independent, controlDeclarations: module, extraControlEdit: extraEdit);
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 401 });
        private static EntityId Entity(SimGame game, string definition, int seat = 0)
            => game.Seat(seat).Snapshot.Entities.Single(e => e.Def == definition && e.Owner == new PlayerId(seat)).Id;
        private static ResourceId Resource(SimGame game, string id = "supply")
            => game.Seat(0).View.TryFindResource(id, out var resource) ? resource : throw new InvalidOperationException("missing authored resource " + id);
        private static Fix Amount(SimGame game, string definition, string resource = "supply")
            => game.Seat(0).View.Feature<InventoryView>().TryGetAmount(Entity(game, definition), Resource(game, resource), out var value)
                ? value : throw new InvalidOperationException("actual private inventory unavailable: " + definition + "/" + resource);
        private static Fix Loaded(SimGame game, string resource = "supply")
        {
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
            return jobs.Where(j => j.IsHaul && j.Resource == Resource(game, resource)).Aggregate(Fix.Zero, (sum, j) => sum + j.Loaded);
        }
        private static LogisticsDemandSettings Stock(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<LogisticsView>().ReadStockPolicy(Entity(game, "destination"), Resource(game), out var value);
            if (!code.IsNone) throw new InvalidOperationException("actual stock policy unavailable " + code); return value;
        }
        private static OutletPolicySettings Outlet(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<LogisticsView>().ReadOutletPolicy(Entity(game, "source"), out var value);
            if (!code.IsNone) throw new InvalidOperationException("actual outlet policy unavailable " + code); return value;
        }
        private static AutomationRuleSnapshot Rule(SimGame game, string id)
            => game.Seat(0).View.Feature<OperationsView>().TryGetAutomationRule(new PlayerId(0), id, out var value) ? value : null;
        private static void Observe(SimGame game)
        {
            if (game.Fault != null) throw new InvalidOperationException("resource control fault: " + game.Fault);
            if (Amount(game, "source") + Amount(game, "destination") + Loaded(game) != Fix.FromInt(20)
                || Amount(game, "source", "spare") + Amount(game, "destination", "spare") + Loaded(game, "spare") != Fix.FromInt(5))
                throw new InvalidOperationException("actual stock/cargo conservation at " + game.Clock.StepIndex);
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); Observe(game);
            if (twin != null) { twin.Update(); Observe(twin); if (game.Hash() != twin.Hash()) throw new InvalidOperationException("saved physical control divergence"); }
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 220 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + ";step=" + game.Clock.StepIndex + ";source=" + Amount(game, "source")
                + ";destination=" + Amount(game, "destination") + ";cargo=" + Loaded(game) + ";desired=" + Stock(game).Desired + ";reserve=" + Outlet(game).Reserve.AmountOf(Resource(game)));
        }
        private static void Order(Check c, SimGame game, Command command, SimGame twin = null)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary command preview");
            c.Equal(hash, game.Hash(), "preview remains pure"); c.That(TestKit.Order(game, 0, command).Accepted, "ordinary command accepted"); Observe(game);
            if (twin != null) { c.That(TestKit.Order(twin, 0, command).Accepted, "saved twin accepts same intent"); Observe(twin); c.Equal(game.Hash(), twin.Hash(), "same intent and engine update agree"); }
        }
        private static AutomationSettings Settings(SimGame game, string profile, bool release = false)
        {
            var view = game.Seat(0).View.Feature<OperationsView>();
            if (!view.TryGetAutomationProfile(profile, out var metadata)) throw new InvalidOperationException("missing control profile");
            EntityId target = Entity(game, profile == "reserve.combined" ? "source" : "destination");
            var ids = new List<Code>();
            if (!view.CopyControlTargetIds(target, profile, ids) || ids.Count != 1) throw new InvalidOperationException("actual target not available");
            var bands = new Dictionary<string, ControlThresholdBand>();
            foreach (var row in metadata.Thresholds) bands.Add(row.Key, release ? new ControlThresholdBand(row.Value.Initial.Unit, Fix.FromInt(25), Fix.FromInt(21)) : row.Value.Initial);
            return new AutomationSettings(profile, true, new ControlBinding(target, ids[0]),
                new Dictionary<string, ControlBinding> { ["stock"] = new ControlBinding(Entity(game, "source"), new Code("feed")) }, bands, metadata.WhenTrue, metadata.WhenFalse);
        }
        private static ConfigureAutomationCommand Configure(SimGame game, string rule, string profile, bool release = false)
            => new ConfigureAutomationCommand { Rule = rule, Settings = Settings(game, profile, release) };
        private static void Run(Check c)
        {
            var nativeHashes = new Dictionary<bool, ulong>();
            foreach (bool direct in new[] { false, true }) foreach (bool independent in new[] { false, true })
            {
                var factory = Factory(direct, independent, out var module); var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
                c.Equal(direct ? 5 : 0, module.DeclaredCount, "four stock-level declarations and one reserve declaration");
                c.Equal(direct ? 0 : 5, module.NativeCount, "direct content has no hidden native resource-action definitions");
                var quantities = game.Seat(0).View.Feature<InventoryView>(); ulong pure = game.Hash();
                c.That(quantities.TryGetAmount(Entity(game, "destination"), Resource(game), out var empty) && empty.IsZero, "available empty stock is distinct from unavailable");
                c.That(!quantities.TryGetAmount(Entity(game, "source", 1), Resource(game), out var hidden) && hidden.IsZero, "foreign quantities are unavailable and clear output");
                c.That(!quantities.TryGetAmount(new EntityId(int.MaxValue), Resource(game), out hidden) && hidden.IsZero, "missing entity cannot fabricate inventory");
                c.That(!quantities.TryGetAmount(Entity(game, "source"), new ResourceId(int.MaxValue), out hidden) && hidden.IsZero, "unknown resources cannot fabricate available quantities");
                c.Equal(pure, game.Hash(), "all detached inventory reads are pure"); Observe(game);
                var limits = new ReplayLimits(4 * 1024 * 1024, 3000); ulong initialHash = game.Hash(); using var recording = game.RecordReplay(limits);
                Order(c, game, Configure(game, "reserve", "reserve.combined"));
                Reach(game, () => Rule(game, "reserve").PendingDecision == true, "earned source hold");
                var twin = factory.Load(game.Save()); game.Clock.Pause(); twin.Clock.Pause(); long pausedAt = game.Clock.StepIndex;
                Step(game, twin); c.Equal(pausedAt, game.Clock.StepIndex, "sole clock pause cannot apply pending control"); game.Clock.Resume(); twin.Clock.Resume();
                Reach(game, () => Rule(game, "reserve").HasApplied, "deferred source reserve", twin);
                c.Equal(Fix.FromInt(20), Outlet(game).Reserve.AmountOf(Resource(game)), "held supply remains real source stock");
                Order(c, game, Configure(game, "minimum", "stock.minimum")); Reach(game, () => Rule(game, "minimum").HasApplied, "minimum control");
                c.Equal(Fix.FromInt(3), Stock(game).Minimum, "minimum action uses original stock owner"); Order(c, game, new RemoveAutomationCommand { Rule = "minimum" });
                Order(c, game, Configure(game, "maximum", "stock.maximum")); Reach(game, () => Rule(game, "maximum").HasApplied, "maximum control");
                c.Equal(Fix.FromInt(16), Stock(game).Maximum, "maximum action uses original stock owner"); Order(c, game, new RemoveAutomationCommand { Rule = "maximum" });
                Order(c, game, new ConfigureStockPolicyCommand { Store = Entity(game, "destination"), Resource = Resource(game),
                    Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(3), Fix.FromInt(4), Fix.FromInt(16), 91, LogisticsReserveAccess.Protected)) });
                Order(c, game, Configure(game, "stock", "stock.band")); Reach(game, () => Rule(game, "stock").PendingDecision == true, "desired control dwell");
                twin = factory.Load(game.Save()); Reach(game, () => Rule(game, "stock").HasApplied, "desired control deferred commit", twin);
                c.That(Stock(game).Desired == Fix.FromInt(12) && Stock(game).Minimum == Fix.FromInt(3) && Stock(game).Maximum == Fix.FromInt(16) && Stock(game).Priority == 91,
                    "desired intent preserves current independently edited bands and priority");
                for (int i = 0; i < 4; i++) Step(game, twin);
                c.That(Amount(game, "source") == Fix.FromInt(20) && Amount(game, "destination").IsZero && Loaded(game).IsZero, "held exports prevent delivery without a teleport fallback");
                Order(c, game, Configure(game, "reserve", "reserve.combined", true), twin);
                Reach(game, () => Rule(game, "reserve").PendingDecision == false, "reserve release dwell", twin);
                Order(c, game, new ConfigureOutletPolicyCommand { Source = Entity(game, "source"), Change = OutletPolicyChange.Replace(
                    new OutletPolicySettings(true, new Bill(new[] { new ResourceAmount(Resource(game), Fix.FromInt(20)), new ResourceAmount(Resource(game, "spare"), Fix.FromInt(4)) }),
                    17, new[] { "destination" }, LogisticsReserveAccess.Protected)) }, twin);
                Reach(game, () => Loaded(game) == Fix.FromInt(8) && Rule(game, "stock").PendingDecision == false, "physical pickup and source feedback", twin);
                c.That(Amount(game, "destination").IsZero && Amount(game, "source") == Fix.FromInt(12), "eight acquired units travel before delivery");
                Step(game, twin);
                c.That(!Stock(game).Enabled && Stock(game).Desired == Fix.FromInt(4) && Loaded(game) == Fix.FromInt(8), "lower target cannot erase acquired cargo");
                Order(c, game, new ConfigureOutletPolicyCommand { Source = Entity(game, "source"), Change = OutletPolicyChange.Reserve(Resource(game), Fix.FromInt(20), false) }, twin);
                Order(c, game, new RemoveAutomationCommand { Rule = "stock" }, twin); Order(c, game, new RemoveAutomationCommand { Rule = "reserve" }, twin);
                twin = factory.Load(game.Save()); long departing = game.Clock.StepIndex;
                Reach(game, () => Loaded(game).IsZero && Amount(game, "destination") == Fix.FromInt(8), "committed delivery after controls cancelled", twin);
                c.That(game.Clock.StepIndex > departing && !Stock(game).Enabled && !Outlet(game).Enabled, "actual delivery earns time despite disabled future work");
                c.That(Outlet(game).Reserve.AmountOf(Resource(game, "spare")) == Fix.FromInt(4) && Outlet(game).ExportPriority == 17
                    && Outlet(game).AllowedDestinations.SequenceEqual(new[] { "destination" }), "reserve control preserves unrelated current policy fields");
                Order(c, game, new ConfigureOutletPolicyCommand { Source = Entity(game, "source"), Change = OutletPolicyChange.Reserve(Resource(game), Fix.FromInt(4), true) }, twin);
                Order(c, game, new ConfigureStockPolicyCommand { Store = Entity(game, "destination"), Resource = Resource(game),
                    Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(9), Fix.FromInt(12), Fix.FromInt(16), 91, LogisticsReserveAccess.Protected)) }, twin);
                Reach(game, () => Amount(game, "destination") == Fix.FromInt(12) && Loaded(game).IsZero, "second physical refill", twin);
                c.Equal(game.Hash(), twin.Hash(), "saved twins agree through both deliveries"); c.Equal(isolated, idle.Hash(), "independent session borrows no policy or stock state");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled physical control saves");
                byte[] bytes = recording.Finish(); var description = ReplayVerifier.Describe(bytes, limits); var replay = new ReplayVerifier().Verify(factory, bytes, limits);
                c.That(description.InitialHash == initialHash && description.InitialStep == recording.InitialStep && description.FinalStep == game.Clock.StepIndex
                    && description.FinalHash == game.Hash() && description.Commands == recording.CommandCount && description.Checkpoints == recording.CheckpointCount,
                    "complete recording spans every player input and live final state");
                c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "all earned updates recorded, no paused checkpoint");
                c.That(replay.Success && replay.Step == game.Clock.StepIndex && replay.ActualHash == game.Hash() && replay.ExpectedHash == game.Hash(), "complete physical replay: " + replay.Detail);
                if (direct) c.Equal(nativeHashes[independent], game.Hash(), "native/direct declarations produce identical final gameplay state"); else nativeHashes.Add(independent, game.Hash());
                Console.WriteLine("resource control replay: direct=" + direct + ";independent=" + independent + ";initial=" + description.InitialStep + ";final=" + description.FinalStep
                    + ";commands=" + description.Commands + ";checkpoints=" + description.Checkpoints + ";live-hash=" + game.Hash().ToString("X16") + ";verified-hash=" + replay.ActualHash.ToString("X16"));
            }
            foreach (bool direct in new[] { false, true }) foreach (string fault in new[] { "resource", "unit", "command", "owner", "level", "missing-unit", "outlet-command", "outlet-unit" })
            {
                var init = new InitializeProbe();
                c.Throws<ContentException>(() => New(Factory(direct, false, out _, init, root => {
                    var action = root.Get("stock_control_actions")[0];
                    if (fault == "missing-unit") action.Remove("unit");
                    else if (!fault.StartsWith("outlet-", StringComparison.Ordinal)) action.Set(fault, JsonValue.Of(fault == "unit" ? "unit.capacity.volume" : "missing"));
                }, extra => {
                    var action = extra.Get("outlet_reserve_control_actions")[0];
                    if (fault == "outlet-command") action.Set("command", JsonValue.Of("sim.configure-stock-policy"));
                    if (fault == "outlet-unit") action.Set("unit", JsonValue.Of("unit.capacity.volume"));
                })), "resource action refuses malformed native/direct authoring " + fault);
                c.Equal(0, init.Initializations, "invalid resource action refuses before Initialize");
            }
        }
    }
}
