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

namespace Beep.SimEngine.Tests
{
    // Same manifest fixture and engine owners as reserve actions; tests submit player intent and observe bounded results.
    internal static class StockTargetActionTests
    {
        internal static GameFactory Factory(bool independent = false, Action<JsonValue> edit = null, string hiddenDependency = null, string workFile = null)
            => OutletReserveActionTests.Factory(independent, edit, hiddenDependency, "stock-target-automation.json", !independent, workFile);
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 401 });
        internal static Entity Find(SimGame game, string prototype, int company = 0)
        {
            foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == prototype && entity.Owner == new PlayerId(company)) return entity;
            throw new InvalidOperationException("missing stock fixture " + prototype);
        }
        private static ResourceId Resource(SimGame game, string resource = "supply") => game.World.Content.ResourceId(resource);
        private static LogisticsView Logistics(SimGame game, int company = 0) => game.Seat(company).View.Feature<LogisticsView>();
        private static OperationsView Operations(SimGame game, int company = 0) => game.Seat(company).View.Feature<OperationsView>();
        internal static LogisticsDemandSettings Stock(SimGame game)
        {
            Code refusal = Logistics(game).ReadStockPolicy(Find(game, "destination").Id, Resource(game), out var settings);
            if (!refusal.IsNone) throw new InvalidOperationException("stock settings unavailable " + refusal); return settings;
        }
        private static LogisticsDemandView Demand(SimGame game)
        {
            var rows = new List<LogisticsDemandView>(); Logistics(game).CopyDemands(rows);
            foreach (var row in rows) if (row.Destination == Find(game, "destination").Id && row.Resource == Resource(game) && row.Source.Scope == LogisticsService.StockDemandScope) return row;
            throw new InvalidOperationException("missing actual stock demand row");
        }
        internal static AutomationSettings Settings(SimGame game, string profile = "stock.band", string target = "destination")
        {
            if (!Operations(game).TryGetAutomationProfile(profile, out var metadata)) throw new InvalidOperationException("stock profile unavailable");
            var ids = new List<Code>(); Operations(game).CopyControlTargetIds(Find(game, target).Id, profile, ids);
            if (ids.Count != 1) throw new InvalidOperationException("missing actual stock target");
            var bands = new Dictionary<string, ControlThresholdBand>(); foreach (var band in metadata.Thresholds) bands.Add(band.Key, band.Value.Initial);
            return new AutomationSettings(profile, true, new ControlBinding(Find(game, target).Id, ids[0]),
                new Dictionary<string, ControlBinding> { ["stock"] = new ControlBinding(Find(game, "source").Id, new Code("feed")) }, bands, metadata.WhenTrue, metadata.WhenFalse);
        }
        internal static AutomationRuleSnapshot Rule(SimGame game)
            => Operations(game).TryGetAutomationRule(new PlayerId(0), "stock", out var rule) ? rule : null;
        private static CommandResult Configure(SimGame game)
            => TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "stock", Settings = Settings(game) });
        private static CommandResult Change(SimGame game, LogisticsDemandChange change, string target = "destination", string resource = "supply")
            => TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Find(game, target).Id, Resource = Resource(game, resource), Change = change });
        private static void Update(SimGame game) { game.Update(); if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString()); }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 220 && !done(); i++) { Update(game); if (twin != null) Update(twin); }
            if (!done())
            {
                var jobs = new List<string>(); foreach (var job in game.World.Services.Get<WorkBoard>().Jobs) jobs.Add(job.Id + "/" + job.State + "/" + job.Blocker);
                throw new InvalidOperationException(stage + " timeout at game step " + game.Clock.StepIndex + "; desired=" + Stock(game).Desired
                    + "; enabled=" + Stock(game).Enabled + "; rule=" + Rule(game)?.Blocker + "; " + string.Join(";", jobs));
            }
        }
        internal static void Contracts(Check c)
        {
            foreach (var fault in new[] { "resource", "unit", "command", "owner", "missing-resource", "missing-unit" })
                c.Throws<ContentException>(() => New(Factory(edit: root => {
                    var action = root.Get("stock_control_actions")[0];
                    if (fault.StartsWith("missing-", StringComparison.Ordinal)) action.Remove(fault.Substring(8));
                    else action.Set(fault, JsonValue.Of(fault == "unit" ? "unit.capacity.volume" : "missing"));
                })), "stock action shares exact native resource/command linking: " + fault);
            foreach (bool independent in new[] { false, true })
            {
                var game = New(Factory(independent)); var target = Find(game, "destination").Id; var supply = Resource(game); var settings = Settings(game); ulong hash = game.Hash();
                c.That(Operations(game).TryGetAutomationProfile("stock.band", out var profile) && profile.ActionCommand == "sim.configure-stock-policy"
                    && profile.ActionCommandOwner == LogisticsModule.ModuleId, "stock profile selects actual normal demand command");
                c.That(Find(game, "destination").Get<IInventory>() is Storage != independent, "actual stock target can be native or public-only inventory");
                c.Equal(Code.None, Operations(game).PreviewAutomation("stock", settings), "stock action admits actual existing disabled row");
                c.Equal(hash, game.Hash(), "stock profile/target/normal preview are pure");
                c.That(!Stock(game).Enabled && Stock(game).Minimum == Fix.FromInt(2) && Stock(game).Desired == Fix.FromInt(4) && Stock(game).Maximum == Fix.FromInt(12), "authored stock bands remain sole initial state");
                var owner = game.World.Services.Get<LogisticsService>();
                var stockSource = new LogisticsDemandId(LogisticsService.StockDemandScope, target.Value, Code.None);
                c.Equal(LogisticsPolicyCodes.StockPolicyOwner, owner.SetRequest(stockSource, new PlayerId(0), target, supply, Fix.One, 1), "generic line edits cannot bypass stock bands");
                c.Equal(LogisticsPolicyCodes.StockPolicyOwner, owner.ReplaceRequests(stockSource, new PlayerId(0), target, Array.Empty<LogisticsSupplyRequest>(), 1), "generic replacement cannot delete authoritative stock intent");
                c.Throws<ArgumentException>(() => owner.WithdrawRequests(stockSource), "generic withdrawal cannot remove stock policy state");
                c.Equal(hash, game.Hash(), "alternate stock mutation paths refuse without changing engine state");
                foreach (var amount in new[] { Fix.One, Fix.FromInt(13), Fix.Parse("2.5") })
                {
                    var change = LogisticsDemandChange.Level(LogisticsStockLevel.Desired, amount, true); hash = game.Hash();
                    c.Equal(Codes.InvalidArgument, Logistics(game).PreviewStockPolicy(target, supply, change), "stock preview refuses below minimum/above maximum/fractional native target");
                    c.Equal(hash, game.Hash(), "invalid desired preview preserves current stock row");
                    c.Equal(Codes.InvalidArgument, Change(game, change).Refusal, "stock command refusal matches same preview");
                }
                c.Equal(LogisticsPolicyCodes.NoStockPolicy, Logistics(game).PreviewStockPolicy(Find(game, "machine").Id, supply, LogisticsDemandChange.Level(LogisticsStockLevel.Desired, Fix.One, true)), "desired action cannot invent an absent machine stock row");
                c.Equal(LogisticsPolicyCodes.NoStockPolicy, Logistics(game).ReadStockPolicy(Find(game, "machine").Id, supply, out _), "absent stock view returns a typed missing policy");
                var ids = new List<Code>();
                c.That(Operations(game).CopyControlTargetIds(Find(game, "machine").Id, profile.Id, ids) && ids.Count == 0, "target discovery never invents stock intent for an inventory");
                c.That(!Operations(game).CopyControlTargetIds(Find(game, "destination", 1).Id, profile.Id, ids), "foreign stock bindings stay private");
                c.Equal(Codes.NotFound, Logistics(game, 1).ReadStockPolicy(target, supply, out _), "stock settings are company-private");
                c.Equal(Codes.NotFound, new LogisticsView(game.World, new PlayerId(-2)).ReadStockPolicy(target, supply, out _), "invalid stock viewer is no observer");
                var wrong = new AutomationSettings(settings.Profile, true, new ControlBinding(target, new Code("logistics.stock.spare")), settings.Inputs, settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
                c.Equal(LogisticsPolicyCodes.NoStockPolicy, Operations(game).PreviewAutomation("wrong", wrong), "binding must name the authored actual resource row");
                long created = Demand(game).CreatedTick;
                c.That(Change(game, LogisticsDemandChange.Level(LogisticsStockLevel.Desired, Fix.FromInt(6), false)).Accepted, "ordinary desired edit accepts disabled existing stock row");
                c.That(Stock(game).Desired == Fix.FromInt(6) && Stock(game).Minimum == Fix.FromInt(2) && Stock(game).Maximum == Fix.FromInt(12)
                    && Stock(game).Priority == 65 && Demand(game).CreatedTick == created, "desired edit retains current limits, priority and original row age");
                var copy = (ConfigureStockPolicyCommand)game.World.Commands.Copy(new ConfigureStockPolicyCommand { Store = target, Resource = supply,
                    Change = LogisticsDemandChange.Level(LogisticsStockLevel.Desired, Fix.FromInt(6), false) });
                c.That(copy.Change.Kind == LogisticsDemandChange.ChangeKind.Level && copy.Change.StockLevel == LogisticsStockLevel.Desired && copy.Change.Target == Fix.FromInt(6) && !copy.Change.Enabled
                    && copy.Resource == supply, "normal command codec retains resource and explicit desired/enabled intent");
            }
            foreach (var amount in new[] { "1", "13", "2.5", "-1" })
            {
                var game = New(Factory(edit: root => root.Get("control_rules")[0].Get("when_true").Set("target", JsonValue.Number(amount))));
                c.Equal(Codes.InvalidArgument, Operations(game).PreviewAutomation("stock", Settings(game)), "stock automation uses normal target admission: " + amount);
                c.Equal(Codes.InvalidArgument, Configure(game).Refusal, "stock configure matches preview without partial policy: " + amount);
                c.That(Rule(game) == null, "invalid stock action publishes no rule prefix");
            }
            Resources(c); Visibility(c); Corrupt(c);
        }
        private static void Resources(Check c)
        {
            var game = New(Factory());
            c.That(Change(game, LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.Zero, Fix.FromInt(4), Fix.FromInt(12), 1, LogisticsReserveAccess.Protected)), "source").Accepted, "player authors actual source supply policy");
            c.That(Change(game, LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.Zero, Fix.One, Fix.FromInt(3), 2, LogisticsReserveAccess.Protected)), "source", "spare").Accepted, "player authors distinct actual spare policy");
            var supply = Settings(game, target: "source"); var spare = Settings(game, "stock.spares", "source");
            c.That(supply.Target.Entity == spare.Target.Entity && supply.Target.Capability != spare.Target.Capability, "stable stock bindings distinguish resources on the same entity");
            c.That(TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "supply", Settings = supply }).Accepted, "normal rule claims actual source supply row");
            c.Equal(AutomationCodes.Conflict, Operations(game).PreviewAutomation("supply.other", supply), "competing profiles for the same stock resource conflict");
            c.Equal(Code.None, Operations(game).PreviewAutomation("spare", spare), "another resource on the same entity is not a duplicate target");
            c.That(TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "spare", Settings = spare }).Accepted, "existing arbitration can independently control distinct resources");
            c.That(Change(game, LogisticsDemandChange.Level(LogisticsStockLevel.Desired, Fix.Zero, false), "source", "spare").Accepted, "zero desired target with minimum zero is explicit valid intent");
            c.Equal(Code.None, Logistics(game).ReadStockPolicy(Find(game, "source").Id, Resource(game, "spare"), out var row), "zero desired retains one authoritative stock row");
            c.That(row.Desired.IsZero && row.Maximum == Fix.FromInt(3) && row.Priority == 2, "zero desired is not an implicit policy deletion or defaults reset");
        }
        private static void Visibility(Check c)
        {
            foreach (string dependency in new[] { "resource", "unit" })
            {
                var game = New(Factory(hiddenDependency: dependency)); var target = Find(game, "destination").Id; var observer = game.Observer.Feature<OperationsView>();
                c.That(observer.TryGetAutomationProfile("stock.band", out var profile), "observer receives complete stock action profile");
                var settings = new AutomationSettings(profile.Id, true, new ControlBinding(target, new Code("logistics.stock.supply")),
                    new Dictionary<string, ControlBinding> { ["stock"] = new ControlBinding(Find(game, "source").Id, new Code("feed")) },
                    new Dictionary<string, ControlThresholdBand> { ["high"] = profile.Thresholds["high"].Initial }, profile.WhenTrue, profile.WhenFalse);
                ulong hash = game.Hash(); var ids = new List<Code>();
                c.That(!Operations(game).TryGetAutomationProfile(profile.Id, out _), "hidden native dependency withholds complete stock profile: " + dependency);
                c.That(!Operations(game).CopyControlTargetIds(target, profile.Id, ids) && ids.Count == 0, "hidden stock dependency withholds binding discovery: " + dependency);
                c.Equal(Codes.UnknownDef, Operations(game).PreviewAutomation("stock", settings), "stock configure shares complete dependency admission: " + dependency);
                c.Equal(hash, game.Hash(), "hidden stock metadata/admission are pure: " + dependency);
            }
        }
        private static void Corrupt(Check c)
        {
            foreach (byte kind in new byte[] { 0, 3, 255 })
            {
                using var stream = new MemoryStream(); var writer = new StateWriter(stream); byte value = kind; writer.Byte(ref value); writer.Flush(); var data = stream.ToArray();
                c.Throws<CorruptStateException>(() => LogisticsDemandChange.Sync(new StateReader(data, 0, data.Length), null), "invalid saved stock intent kind rejects: " + kind);
            }
            c.Throws<ArgumentException>(() => LogisticsDemandChange.Level(LogisticsStockLevel.Desired, -Fix.One, true), "negative desired target has no clamped intent");
            c.Throws<ArgumentNullException>(() => LogisticsDemandChange.Replace(null), "stock replacement needs explicit settings");
        }
        internal static void Execution(Check c)
        {
            foreach (bool independent in new[] { false, true }) { Physical(c, independent); Refusal(c, independent); }
            var factory = Factory(); var game = New(factory); var supply = Resource(game);
            var command = new ConfigureStockPolicyCommand { Store = Find(game, "destination").Id, Resource = supply, Change = LogisticsDemandChange.Level(LogisticsStockLevel.Desired, Fix.FromInt(6), false) };
            game.Seat(0).Submit(command); command.Change = LogisticsDemandChange.Level(LogisticsStockLevel.Desired, Fix.FromInt(39), true);
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending normal desired-stock command saves in existing inbox");
            Update(game); Update(twin);
            c.That(Stock(game).Desired == Fix.FromInt(6) && !Stock(game).Enabled, "normal stock command uses submitted detached intent after caller mutation");
            c.Equal(game.Hash(), twin.Hash(), "pending normal stock command commits identically after save/load");
        }
        private static void Refusal(Check c, bool independent)
        {
            var factory = Factory(independent); var game = New(factory); Configure(game); Update(game); Update(game);
            c.That(Change(game, LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(3), Fix.FromInt(4), Fix.FromInt(6), 91, LogisticsReserveAccess.Protected))).Accepted, "later player bands are valid before due action");
            c.That(Rule(game).Suspended && Rule(game).Blocker == Codes.InvalidArgument && !Rule(game).HasApplied, "due desired target rechecks later limits and refuses without clamping");
            c.That(Stock(game).Minimum == Fix.FromInt(3) && Stock(game).Desired == Fix.FromInt(4) && Stock(game).Maximum == Fix.FromInt(6)
                && Stock(game).Priority == 91, "refused stock action preserves all actual later player bands");
            var twin = factory.Load(game.Save()); for (int i = 0; i < 5; i++) { Update(game); Update(twin); }
            c.Equal(game.Hash(), twin.Hash(), "saved stock refusal and ordinary physical request continue identically");
        }
        private static void Physical(Check c, bool independent)
        {
            var factory = Factory(independent); var game = New(factory); var supply = Resource(game); long created = Demand(game).CreatedTick;
            c.That(Configure(game).Accepted, "player installs stock-driven desired-target rule, public inventory=" + independent);
            Update(game); Update(game);
            c.That(Rule(game).PendingDecision == true && !Stock(game).Enabled, "stock action earns dwell and does not enable same-update supply");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending desired rule saves through existing owners");
            game.Clock.Pause(); ulong paused = game.Hash(); c.Equal(0, game.Update(), "sole clock pause holds desired-stock rule"); c.Equal(paused, game.Hash(), "paused stock rule changes no stock or demand"); game.Clock.Resume();
            var later = new LogisticsDemandSettings(false, Fix.FromInt(3), Fix.FromInt(4), Fix.FromInt(14), 91, LogisticsReserveAccess.Protected);
            c.That(Change(game, LogisticsDemandChange.Replace(later)).Accepted, "player edits limits/priority before due desired action");
            c.That(Change(twin, LogisticsDemandChange.Replace(later)).Accepted, "saved pending rule receives the same player limits/priority edit");
            c.Equal(game.Hash(), twin.Hash(), "saved pending desired action resolves identical current settings at actual commit");
            c.That(Stock(game).Enabled && Stock(game).Desired == Fix.FromInt(12) && Stock(game).Minimum == Fix.FromInt(3)
                && Stock(game).Maximum == Fix.FromInt(14) && Stock(game).Priority == 91 && Demand(game).CreatedTick == created,
                "deferred stock action preserves actual current bounds/priority/row age, not initial JSON values");
            var source = Find(game, "source").Get<IInventory>(); var dest = Find(game, "destination").Get<IInventory>(); var cargo = Find(game, "machine").Get<ITransporter>().Cargo;
            Reach(game, () => cargo.Amount(supply) == Fix.FromInt(8) && Rule(game).PendingDecision == false, "actual pickup and stock feedback cooldown");
            c.That(dest.Amount(supply).IsZero && dest.ReservedIn(supply) == Fix.FromInt(8) && Stock(game).Desired == Fix.FromInt(12), "feedback decision is pending while real acquired cargo travels");
            twin = factory.Load(game.Save()); long before = game.Clock.StepIndex; Update(game); Update(twin);
            c.That(!Stock(game).Enabled && Stock(game).Desired == Fix.FromInt(4) && !Rule(game).Latch && Rule(game).HasApplied, "actual source signal disables refill through normal deferred stock command");
            c.That(dest.ReservedIn(supply) == Fix.FromInt(8) && cargo.Amount(supply) == Fix.FromInt(8) && source.Amount(supply) == Fix.FromInt(12), "lower disabled target leaves physical cargo, source balances and live receiving claims intact");
            c.Equal(game.Hash(), twin.Hash(), "saved pending opposite stock action commits identically");
            c.That(TestKit.Order(game, 0, new RemoveAutomationCommand { Rule = "stock" }).Accepted, "ordinary cancellation stops future stock decisions, not the acquired haul");
            twin = factory.Load(game.Save());
            Reach(game, () => cargo.Amount(supply).IsZero && dest.Amount(supply) == Fix.FromInt(8), "disabled requester physical delivery", twin);
            c.That(game.Clock.StepIndex > before && !Stock(game).Enabled && Stock(game).Desired == Fix.FromInt(4), "real delivery takes game time and may finish above a later lowered desired level");
            c.Equal(game.Hash(), twin.Hash(), "cancelled rule/disabled demand/current limits and physical delivery survive saved continuation");
            c.Equal(Fix.FromInt(20), source.Amount(supply) + cargo.Amount(supply) + dest.Amount(supply), "stock controls conserve actual transported material");
            c.That(Change(game, LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(9), Fix.FromInt(12), Fix.FromInt(14), 91, LogisticsReserveAccess.Protected))).Accepted,
                "player raises current minimum to request the remaining physical refill through the same owner");
            twin = factory.Load(game.Save()); Reach(game, () => dest.Amount(supply) == Fix.FromInt(12) && cargo.Amount(supply).IsZero
                && !Demand(game).Replenishing, "second physical refill to desired and settled logistics phase", twin);
            c.That(!Demand(game).Replenishing && source.Amount(supply) == Fix.FromInt(8), "existing refill latch clears only when actual desired stock arrives");
            c.Equal(game.Hash(), twin.Hash(), "saved second refill reaches current desired stock deterministically");
        }
    }
}
