using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class StockBandControlTests
    {
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 419 });
        private static LogisticsView Logistics(SimGame game) => game.Seat(0).View.Feature<LogisticsView>();
        private static OperationsView Operations(SimGame game) => game.Seat(0).View.Feature<OperationsView>();
        private static Entity Destination(SimGame game) => StockTargetActionTests.Find(game, "destination");
        private static ResourceId Supply(SimGame game) => game.World.Content.ResourceId("supply");
        private static CommandResult Change(SimGame game, LogisticsStockLevel level, int amount, bool enabled = false)
            => TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Destination(game).Id, Resource = Supply(game), Change = LogisticsDemandChange.Level(level, Fix.FromInt(amount), enabled) });
        private static AutomationSettings Values(AutomationSettings settings, int amount, bool enabled = true)
        {
            var action = new ControlAction(enabled, Fix.FromInt(amount), settings.WhenTrue.Unit);
            return new AutomationSettings(settings.Profile, settings.Enabled, settings.Target, settings.Inputs, settings.Thresholds, action, action);
        }
        private static CommandResult Configure(SimGame game, AutomationSettings settings)
            => TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "stock", Settings = settings });
        private static void Update(SimGame game) { game.Update(); if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString()); }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 220 && !done(); i++) { Update(game); if (twin != null) Update(twin); }
            if (!done()) throw new InvalidOperationException(stage + " timeout at game clock " + game.Clock.StepIndex + "; blocker=" + StockTargetActionTests.Rule(game)?.Blocker);
        }
        private static long AgeIdentity(SimGame game)
        {
            var rows = new List<LogisticsDemandView>(); Logistics(game).CopyDemands(rows);
            foreach (var row in rows) if (row.Destination == Destination(game).Id && row.Resource == Supply(game) && row.Source.Scope == LogisticsService.StockDemandScope) return row.CreatedTick;
            throw new InvalidOperationException("missing actual stock row");
        }
        internal static void Contracts(Check c)
        {
            foreach (string level in new[] { null, "", "stock", "Desired", "minimum_typo" })
                c.Throws<ContentException>(() => New(StockTargetActionTests.Factory(edit: root => {
                    var action = root.Get("stock_control_actions")[0]; if (level == null) action.Remove("level"); else action.Set("level", JsonValue.Of(level));
                })), "JSON requires exact explicit stock level: " + level);
            c.Throws<ContentException>(() => New(StockTargetActionTests.Factory(edit: root => {
                var actions = root.Get("stock_control_actions"); root.Remove("stock_control_actions"); root.Set("stock_target_control_actions", actions);
            })), "old desired-only section is not a compatibility loader");
            foreach (bool independent in new[] { false, true })
            {
                var factory = StockTargetActionTests.Factory(independent); var game = New(factory); long created = AgeIdentity(game);
                foreach (var level in new[] { LogisticsStockLevel.Minimum, LogisticsStockLevel.Desired, LogisticsStockLevel.Maximum })
                {
                    int amount = level == LogisticsStockLevel.Minimum ? 3 : level == LogisticsStockLevel.Desired ? 6 : 16;
                    var intent = LogisticsDemandChange.Level(level, Fix.FromInt(amount), false); ulong hash = game.Hash();
                    c.Equal(Code.None, Logistics(game).PreviewStockPolicy(Destination(game).Id, Supply(game), intent), "each level uses canonical stock admission");
                    c.Equal(hash, game.Hash(), "level preview is pure");
                    c.That(Change(game, level, amount).Accepted, "ordinary player command changes selected stock level");
                    var row = StockTargetActionTests.Stock(game);
                    c.That(row.Minimum == Fix.FromInt(3) && row.Desired == Fix.FromInt(level == LogisticsStockLevel.Minimum ? 4 : 6)
                        && row.Maximum == Fix.FromInt(level == LogisticsStockLevel.Maximum ? 16 : 12) && !row.Enabled && row.Priority == 65
                        && AgeIdentity(game) == created, "single-level edit preserves unrelated levels, priority and row age");
                    var copy = (ConfigureStockPolicyCommand)game.World.Commands.Copy(new ConfigureStockPolicyCommand { Store = Destination(game).Id, Resource = Supply(game), Change = intent });
                    c.That(copy.Change.Kind == LogisticsDemandChange.ChangeKind.Level && copy.Change.StockLevel == level && copy.Change.Target == Fix.FromInt(amount), "one normal codec retains selected level identity");
                    var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "each normal stock level saves through sole demand owner");
                }
                foreach (var intent in new[] { LogisticsDemandChange.Level(LogisticsStockLevel.Minimum, Fix.FromInt(7), false),
                    LogisticsDemandChange.Level(LogisticsStockLevel.Desired, Fix.One, false), LogisticsDemandChange.Level(LogisticsStockLevel.Maximum, Fix.FromInt(5), false),
                    LogisticsDemandChange.Level(LogisticsStockLevel.Minimum, Fix.Parse("2.5"), false), LogisticsDemandChange.Level(LogisticsStockLevel.Maximum, Fix.FromInt(41), false) })
                {
                    ulong hash = game.Hash(); c.Equal(Codes.InvalidArgument, Logistics(game).PreviewStockPolicy(Destination(game).Id, Supply(game), intent), "inversion/nonnative quantity/actual capacity refuse");
                    c.Equal(hash, game.Hash(), "invalid stock level preview does not change bands");
                    c.Equal(Codes.InvalidArgument, TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Destination(game).Id, Resource = Supply(game), Change = intent }).Refusal, "normal level refusal matches preview");
                }
                var multiResource = StockTargetActionTests.Find(game, "source").Id;
                c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = multiResource, Resource = Supply(game),
                    Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.FromInt(3), Fix.FromInt(6), Fix.FromInt(16), 65, LogisticsReserveAccess.Protected)) }).Accepted, "multi-resource source authors actual supply bands");
                c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = multiResource, Resource = game.World.Content.ResourceId("spare"),
                    Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(false, Fix.Zero, Fix.One, Fix.FromInt(3), 7, LogisticsReserveAccess.Protected)) }).Accepted, "another actual resource has its own maximum");
                c.Equal(Codes.InvalidArgument, TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = multiResource, Resource = Supply(game),
                    Change = LogisticsDemandChange.Level(LogisticsStockLevel.Maximum, Fix.FromInt(38), false) }).Refusal, "maximum level obeys actual combined resource capacity");
                c.That(Logistics(game).ReadStockPolicy(multiResource, Supply(game), out var unchanged) == Code.None && unchanged.Maximum == Fix.FromInt(16), "combined-capacity refusal retains existing maximum");
                c.Equal(LogisticsPolicyCodes.NoStockPolicy, Logistics(game).PreviewStockPolicy(StockTargetActionTests.Find(game, "machine").Id, Supply(game), LogisticsDemandChange.Level(LogisticsStockLevel.Maximum, Fix.One, false)), "level edits never invent a missing stock row");
                game = New(factory); var min = StockTargetActionTests.Settings(game, "stock.minimum"); var max = StockTargetActionTests.Settings(game, "stock.maximum");
                c.That(min.Target.Equals(max.Target) && min.Target.Equals(StockTargetActionTests.Settings(game).Target), "all levels share the one resource policy binding");
                c.That(Configure(game, min).Accepted, "minimum rule discovers actual native/public stock target");
                c.Equal(AutomationCodes.Conflict, Operations(game).PreviewAutomation("max", max), "different stock fields cannot acquire conflicting writers for one resource");
                var ids = new List<Code>(); c.That(!Operations(game).CopyControlTargetIds(StockTargetActionTests.Find(game, "destination", 1).Id, max.Profile, ids), "maximum target remains company-private");
            }
            foreach (string hidden in new[] { "resource", "unit" })
            {
                var game = New(StockTargetActionTests.Factory(hiddenDependency: hidden));
                foreach (string profile in new[] { "stock.minimum", "stock.maximum" }) c.That(!Operations(game).TryGetAutomationProfile(profile, out _), "hidden dependency withholds complete band profile");
            }
            Corruption(c);
        }
        private static void Corruption(Check c)
        {
            foreach (byte level in new byte[] { 0, 4, 255 })
            {
                c.Throws<ArgumentException>(() => LogisticsDemandChange.Level((LogisticsStockLevel)level, Fix.One, false), "invalid typed level has no default");
                using var stream = new MemoryStream(); var writer = new StateWriter(stream); byte kind = 2, field = level; Fix target = Fix.One; bool enabled = false;
                writer.Byte(ref kind); writer.Byte(ref field); writer.Fix(ref target); writer.Bool(ref enabled); writer.Flush(); var bytes = stream.ToArray();
                c.Throws<CorruptStateException>(() => LogisticsDemandChange.Sync(new StateReader(bytes, 0, bytes.Length), null), "unknown saved level identity rejects before continuation");
            }
        }
        internal static void Execution(Check c)
        {
            foreach (bool independent in new[] { false, true }) { MinimumPhysical(c, independent); MaximumPhysical(c, independent); LaterRefusal(c, independent); Inbox(c, independent); }
        }
        private static void MinimumPhysical(Check c, bool independent)
        {
            var factory = StockTargetActionTests.Factory(independent); var game = New(factory); var initial = StockTargetActionTests.Settings(game, "stock.minimum");
            c.That(Configure(game, initial).Accepted, "JSON minimum action installs without immediate supply");
            Update(game); Update(game); c.That(StockTargetActionTests.Rule(game).PendingDecision == true && !StockTargetActionTests.Stock(game).Enabled, "minimum action earns dwell and defers");
            var twin = factory.Load(game.Save()); game.Clock.Pause(); ulong paused = game.Hash(); c.Equal(0, game.Update(), "one clock pause holds minimum intent"); c.Equal(paused, game.Hash(), "paused minimum changes no state"); game.Clock.Resume(); Update(game); Update(twin);
            c.That(StockTargetActionTests.Stock(game).Minimum == Fix.FromInt(3) && StockTargetActionTests.Stock(game).Desired == Fix.FromInt(4)
                && StockTargetActionTests.Stock(game).Maximum == Fix.FromInt(12) && StockTargetActionTests.Stock(game).Priority == 65, "minimum action preserves actual other bands/priority");
            c.Equal(game.Hash(), twin.Hash(), "saved pending minimum action commits identically");
            var cargo = StockTargetActionTests.Find(game, "machine").Get<ITransporter>().Cargo; var dest = Destination(game).Get<IInventory>(); var supply = Supply(game);
            Reach(game, () => cargo.Amount(supply) == Fix.FromInt(4), "minimum-driven physical pickup");
            c.That(dest.Amount(supply).IsZero && dest.ReservedIn(supply) == Fix.FromInt(4), "minimum refill uses actual carrier and receiving claim");
            Configure(game, Values(initial, 0)); twin = factory.Load(game.Save()); Reach(game, () => StockTargetActionTests.Stock(game).Minimum.IsZero, "minimum zero edit", twin);
            c.That(cargo.Amount(supply) == Fix.FromInt(4) && dest.ReservedIn(supply) == Fix.FromInt(4), "minimum zero does not revoke acquired cargo/claims");
            Reach(game, () => dest.Amount(supply) == Fix.FromInt(4) && cargo.Amount(supply).IsZero, "minimum-zero physical delivery", twin);
            c.Equal(game.Hash(), twin.Hash(), "native/public minimum edit and physical delivery save identically");
        }
        private static void MaximumPhysical(Check c, bool independent)
        {
            var factory = StockTargetActionTests.Factory(independent, workFile: "stock-band-work.json"); var game = New(factory);
            TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Destination(game).Id, Resource = Supply(game),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(2), Fix.FromInt(12), Fix.FromInt(12), 91, LogisticsReserveAccess.Protected)) });
            var cargo = StockTargetActionTests.Find(game, "machine").Get<ITransporter>().Cargo; var dest = Destination(game).Get<IInventory>(); var source = StockTargetActionTests.Find(game, "source").Get<IInventory>(); var supply = Supply(game);
            Reach(game, () => cargo.Amount(supply) == Fix.FromInt(8), "maximum edit acquired physical cargo");
            c.That(Change(game, LogisticsStockLevel.Desired, 6, true).Accepted, "current desired changes before lowering maximum");
            var settings = Values(StockTargetActionTests.Settings(game, "stock.maximum"), 6); Configure(game, settings);
            var twin = factory.Load(game.Save()); Reach(game, () => StockTargetActionTests.Stock(game).Maximum == Fix.FromInt(6), "lower maximum deferred commit", twin);
            c.That(StockTargetActionTests.Stock(game).Desired == Fix.FromInt(6) && StockTargetActionTests.Stock(game).Minimum == Fix.FromInt(2)
                && StockTargetActionTests.Stock(game).Priority == 91 && dest.ReservedIn(supply) == Fix.FromInt(8) && cargo.Amount(supply) == Fix.FromInt(8), "maximum edit preserves actual player bands/priority and oversized acquired receipt");
            c.Equal(game.Hash(), twin.Hash(), "saved maximum edit preserves incoming cargo above new ceiling");
            Reach(game, () => dest.Amount(supply) == Fix.FromInt(8) && cargo.Amount(supply).IsZero, "already acquired delivery above new maximum", twin);
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "band.waiting-work", Target = Destination(game).Id }).Accepted,
                "player posts actual JSON work with material demand beyond current stock ceiling");
            c.Equal(Code.None, Logistics(game).ReadStockTarget(Destination(game).Id, supply, out var stockTarget), "actual work stock target available");
            c.That(stockTarget > dest.Amount(supply), "existing work owner publishes real unmet demand beyond new maximum");
            twin = factory.Load(game.Save());
            for (int i = 0; i < 8; i++) { Update(game); Update(twin); }
            c.That(dest.ReservedIn(supply).IsZero && dest.Amount(supply) == Fix.FromInt(8) && source.Amount(supply) == Fix.FromInt(12), "lower maximum creates no new claims or fabricated cargo disposal");
            c.Equal(game.Hash(), twin.Hash(), "lowered ceiling and settled physical jobs continue identically");
            Configure(game, Values(settings, 12)); Reach(game, () => StockTargetActionTests.Stock(game).Maximum == Fix.FromInt(12), "maximum raise edited action");
            c.That(dest.Amount(supply) == Fix.FromInt(8) && StockTargetActionTests.Stock(game).Desired == Fix.FromInt(6), "raising maximum does not invent stock or reset desired intent");
            twin = factory.Load(game.Save()); Reach(game, () => dest.Amount(supply) == Fix.FromInt(12) && cargo.Amount(supply).IsZero, "raised maximum bounds actual work refill", twin);
            c.That(source.Amount(supply) == Fix.FromInt(8) && dest.ReservedIn(supply).IsZero, "raised maximum admits only four more units despite higher aggregate demand");
            c.Equal(game.Hash(), twin.Hash(), "saved raised ceiling bounds native/public physical refill identically");
            c.Equal(Fix.FromInt(20), source.Amount(supply) + dest.Amount(supply) + cargo.Amount(supply), "maximum control conserves actual material");
        }
        private static void LaterRefusal(Check c, bool independent)
        {
            foreach (string profile in new[] { "stock.minimum", "stock.maximum" })
            {
                var factory = StockTargetActionTests.Factory(independent); var game = New(factory); Configure(game, StockTargetActionTests.Settings(game, profile)); Update(game); Update(game);
                var settings = profile == "stock.minimum" ? new LogisticsDemandSettings(false, Fix.One, Fix.FromInt(2), Fix.FromInt(12), 87, LogisticsReserveAccess.Protected)
                    : new LogisticsDemandSettings(false, Fix.FromInt(2), Fix.FromInt(18), Fix.FromInt(20), 87, LogisticsReserveAccess.Protected);
                c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Destination(game).Id, Resource = Supply(game), Change = LogisticsDemandChange.Replace(settings) }).Accepted, "later player bands admit before pending level commit");
                c.That(StockTargetActionTests.Rule(game).Suspended && StockTargetActionTests.Rule(game).Blocker == Codes.InvalidArgument && !StockTargetActionTests.Rule(game).HasApplied,
                    "later current desired refuses incompatible min/max without clamping");
                c.That(StockTargetActionTests.Stock(game).Desired == settings.Desired && StockTargetActionTests.Stock(game).Priority == 87 && !StockTargetActionTests.Stock(game).Enabled, "refused level retains all actual later player intent");
                var twin = factory.Load(game.Save()); for (int i = 0; i < 5; i++) { Update(game); Update(twin); } c.Equal(game.Hash(), twin.Hash(), "saved typed level refusal continues identically");
            }
        }
        private static void Inbox(Check c, bool independent)
        {
            var factory = StockTargetActionTests.Factory(independent); var game = New(factory);
            var command = new ConfigureStockPolicyCommand { Store = Destination(game).Id, Resource = Supply(game), Change = LogisticsDemandChange.Level(LogisticsStockLevel.Maximum, Fix.FromInt(16), false) };
            game.Seat(0).Submit(command); command.Change = LogisticsDemandChange.Level(LogisticsStockLevel.Minimum, Fix.FromInt(99), true);
            var twin = factory.Load(game.Save()); Update(game); Update(twin);
            c.That(StockTargetActionTests.Stock(game).Maximum == Fix.FromInt(16) && StockTargetActionTests.Stock(game).Minimum == Fix.FromInt(2), "saved inbox retains explicit selected level and detached quantity");
            c.Equal(game.Hash(), twin.Hash(), "normal level inbox save uses existing command codec only");
        }
    }
}
