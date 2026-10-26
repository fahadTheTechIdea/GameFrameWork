using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.AutomationTests;

namespace Beep.SimEngine.Tests
{
    // Player settings and bounded observations only; engine owns evaluation, timing and physical work.
    internal static class RuntimeActionSettingsTests
    {
        private static AutomationSettings Actions(AutomationSettings current, ControlAction yes, ControlAction no)
            => new AutomationSettings(current.Profile, current.Enabled, current.Target, current.Inputs, current.Thresholds, yes, no);
        private static ControlAction Value(ControlAction current, int target, bool? enabled = null)
            => new ControlAction(enabled ?? current.Enabled, Fix.FromInt(target), current.Unit);
        private static void Update(SimGame game) { game.Update(); if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString()); }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null, int bound = 100)
        {
            for (int i = 0; i < bound && !done(); i++) { Update(game); if (twin != null) Update(twin); }
            if (!done()) throw new InvalidOperationException(stage + " timed out at sole game clock " + game.Clock.StepIndex);
        }
        private static Fix Target(SimGame game)
            => View(game).TryGetControl(Find(game, "actuator").Id, new Code("probe"), out var value) ? value.Target : throw new InvalidOperationException("missing actual operation");
        private static Fix Level(SimGame game)
        {
            Code code = View(game).ReadSignal(Find(game, "actuator").Id, new Code("probe"), "probe.level", out var value);
            return code.IsNone && value.Available ? value.Value.Value : throw new InvalidOperationException("missing actual independent level");
        }
        internal static void Contracts(Check c)
        {
            var game = New(Factory()); var initial = Settings(game);
            var edited = Actions(initial, Value(initial.WhenTrue, 3), Value(initial.WhenFalse, 5)); ulong hash = game.Hash();
            c.Equal(Code.None, View(game).PreviewAutomation("field", edited), "runtime branches use actual normal command admission");
            c.Equal(hash, game.Hash(), "runtime action preview is pure");
            foreach (bool yes in new[] { false, true })
            {
                var wrong = new ControlAction(true, Fix.One, new Code("unit.level"));
                var badUnit = Actions(initial, yes ? wrong : initial.WhenTrue, yes ? initial.WhenFalse : wrong);
                c.Equal(AutomationCodes.UnitMismatch, View(game).PreviewAutomation("field", badUnit), "each edited branch requires declared exact unit");
                c.Equal(AutomationCodes.UnitMismatch, Configure(game, badUnit).Refusal, "configure agrees on action unit refusal");
                var badBounds = Actions(initial, yes ? Value(initial.WhenTrue, 11) : initial.WhenTrue, yes ? initial.WhenFalse : Value(initial.WhenFalse, -1));
                c.Equal(OperationCodes.TargetBounds, View(game).PreviewAutomation("field", badBounds), "both branches are preflighted through normal operation bounds");
                c.Equal(OperationCodes.TargetBounds, Configure(game, badBounds).Refusal, "normal configure validates both targets");
                c.That(Rule(game) == null && Target(game) == Fix.One, "refused action installs no partial rule/control");
            }
            c.Throws<ArgumentException>(() => Actions(initial, default, initial.WhenFalse), "no omitted true branch is inferred");
            c.Throws<ArgumentException>(() => Actions(initial, initial.WhenTrue, default), "no omitted false branch is inferred");
            c.That(Configure(game, edited).Accepted, "ordinary configure installs player action values");
            var retained = Rule(game); c.That(!retained.HasApplied && !retained.LastAppliedAction.HasValue, "candidate has no fabricated applied action");
            c.That(retained.Settings.WhenTrue.Equals(edited.WhenTrue) && retained.Settings.WhenFalse.Equals(edited.WhenFalse), "immutable view exposes runtime branches");
            c.That(View(game).TryGetAutomationProfile(initial.Profile, out var profile) && profile.WhenTrue.Target == Fix.One, "runtime edit never mutates JSON profile intent");
            var foreign = new List<AutomationRuleSnapshot>(); View(game, 1).CopyAutomationRules(foreign); c.Equal(0, foreign.Count, "runtime actions remain private");
            var copy = (ConfigureAutomationCommand)game.World.Commands.Copy(new ConfigureAutomationCommand { Rule = "field", Settings = edited });
            c.That(copy.Settings.WhenTrue.Equals(edited.WhenTrue) && copy.Settings.WhenFalse.Equals(edited.WhenFalse), "existing inbox codec detaches both branches");
            foreach (string unit in new[] { (string)null, "bad unit" })
            {
                using var stream = new MemoryStream(); var writer = new StateWriter(stream); bool enabled = true; Fix amount = Fix.One; string id = unit;
                writer.Bool(ref enabled); writer.Fix(ref amount); writer.String(ref id); writer.Flush(); byte[] bytes = stream.ToArray();
                c.Throws<CorruptStateException>(() => ControlAction.Sync(new StateReader(bytes, 0, bytes.Length), default), "embedded action codec rejects missing/malformed unit");
            }
            foreach (bool independent in new[] { false, true })
            {
                var stockGame = StockTargetActionTests.Factory(independent).NewGame(new NewGameRequest { Scenario = "points", Seed = 407 });
                var stock = StockTargetActionTests.Settings(stockGame); var bad = Actions(stock, Value(stock.WhenTrue, 13), stock.WhenFalse);
                c.Equal(Codes.InvalidArgument, View(stockGame).PreviewAutomation("stock", bad), "runtime stock obeys actual maximum for either inventory");
                var reserveGame = OutletReserveActionTests.Factory(independent).NewGame(new NewGameRequest { Scenario = "points", Seed = 409 });
                var reserve = OutletReserveActionTests.Settings(reserveGame); bad = Actions(reserve, Value(reserve.WhenTrue, 38), reserve.WhenFalse);
                c.Equal(Codes.InvalidArgument, View(reserveGame).PreviewAutomation("reserve", bad), "runtime reserve obeys combined native capacity");
            }
        }
        internal static void Execution(Check c)
        {
            PendingEdit(c); AppliedEdits(c); Independent(c);
            foreach (bool independent in new[] { false, true }) { StockPhysical(c, independent); StockRefusal(c, independent); ReservePhysical(c, independent); }
        }
        private static void PendingEdit(Check c)
        {
            var factory = Factory(); var game = New(factory); var initial = Settings(game); Configure(game, initial); Update(game); Update(game);
            c.That(Rule(game).PendingDecision == true && Target(game) == Fix.One, "old action is pending before edit");
            var changed = Actions(initial, Value(initial.WhenTrue, 3), Value(initial.WhenFalse, 5));
            var command = new ConfigureAutomationCommand { Rule = "field", Settings = changed }; game.Seat(0).Submit(command);
            command.Settings = Actions(initial, Value(initial.WhenTrue, 9), initial.WhenFalse);
            var twin = factory.Load(game.Save()); Update(game); Update(twin);
            c.That(!Rule(game).PendingDecision.HasValue && Rule(game).Candidate == true && !Rule(game).HasApplied && Target(game) == Fix.One,
                "inbox edit clears stale due action and earns fresh dwell without same-update commit");
            c.Equal(changed.WhenTrue, Rule(game).Settings.WhenTrue, "submitted action is detached from caller mutation");
            c.Equal(game.Hash(), twin.Hash(), "saved pending edit continues identically");
            var retained = Rule(game); game.Clock.Pause(); ulong paused = game.Hash(); c.Equal(0, game.Update(), "one clock pause holds action dwell"); c.Equal(paused, game.Hash(), "paused action changes no state"); game.Clock.Resume();
            Reach(game, () => Rule(game).HasApplied, "edited pending action", twin);
            c.That(Target(game) == Fix.FromInt(3) && !Enabled(game) && Rule(game).LastAppliedAction.Value.Equals(changed.WhenTrue), "only edited action commits after fresh dwell");
            c.That(!retained.HasApplied, "retained snapshot never follows later applied state");
            c.Equal(game.Hash(), twin.Hash(), "fresh dwell/pending/commit use sole saved rule owner");
        }
        private static void AppliedEdits(Check c)
        {
            var factory = Factory(); var game = New(factory); Configure(game, Settings(game)); Reach(game, () => Rule(game).HasApplied, "initial action");
            var previous = Rule(game); var changed = Actions(previous.Settings, Value(previous.Settings.WhenTrue, 4), previous.Settings.WhenFalse);
            c.That(Configure(game, changed).Accepted, "selected edit with unchanged latch admits");
            c.That(Rule(game).Latch && Rule(game).LastAppliedAction.Value.Equals(previous.LastAppliedAction.Value)
                && Rule(game).LastAppliedAt == previous.LastAppliedAt && Rule(game).CooldownUntil == previous.CooldownUntil && Target(game) == Fix.One,
                "edit preserves applied history and cooldown without fabricated reset");
            var twin = factory.Load(game.Save()); Reach(game, () => Target(game) == Fix.FromInt(4), "stable condition edited action", twin);
            c.That(Rule(game).Latch && Rule(game).LastAppliedAt >= previous.CooldownUntil && Rule(game).LastAppliedAction.Value.Equals(changed.WhenTrue), "same true condition re-applies changed payload after dwell/cooldown");
            c.Equal(game.Hash(), twin.Hash(), "saved actual action can differ from current configured action");
            previous = Rule(game); var opposite = Actions(changed, changed.WhenTrue, Value(changed.WhenFalse, 6)); Configure(game, opposite);
            for (int i = 0; i < 8; i++) Update(game);
            c.That(!Rule(game).Candidate.HasValue && !Rule(game).PendingDecision.HasValue && Rule(game).LastAppliedAt == previous.LastAppliedAt && Target(game) == Fix.FromInt(4), "unselected edit causes no redundant commit");
            TestKit.Order(game, 0, new SetAutomationProbeCommand { Entity = Find(game, "sensor").Id, Level = Fix.FromInt(2), Available = true });
            twin = factory.Load(game.Save()); Reach(game, () => !Rule(game).Latch, "edited opposite action", twin);
            c.That(Enabled(game) && Target(game) == Fix.FromInt(6) && Rule(game).LastAppliedAction.Value.Equals(opposite.WhenFalse), "edited false branch uses ordinary operation handler");
            c.Equal(game.Hash(), twin.Hash(), "opposite action uses settings not JSON initial values");
        }
        private static void Independent(Check c)
        {
            var factory = ControlActionPolicyTests.Independent(); var game = New(factory); var initial = Settings(game);
            var changed = Actions(initial, Value(initial.WhenTrue, 7), Value(initial.WhenFalse, 3)); Configure(game, changed);
            var twin = factory.Load(game.Save()); Reach(game, () => Rule(game).HasApplied, "independent action", twin);
            c.That(Level(game) == Fix.FromInt(7) && Enabled(game) && Rule(game).LastAppliedAction.Value.Unit == new Code("unit.level"), "public-only different-unit action dispatches its own command without coordinator switch");
            c.Equal(game.Hash(), twin.Hash(), "independent edited action saves before commit");
            changed = Actions(changed, Value(changed.WhenTrue, 8), changed.WhenFalse); Configure(game, changed);
            twin = factory.Load(game.Save()); Reach(game, () => Level(game) == Fix.FromInt(8), "independent stable latch edit", twin);
            c.Equal(game.Hash(), twin.Hash(), "independent edit preserves native timing/saved identity");
        }
        private static void StockPhysical(Check c, bool independent)
        {
            var factory = StockTargetActionTests.Factory(independent); var game = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 411 });
            var settings = StockTargetActionTests.Settings(game); var changed = Actions(settings, Value(settings.WhenTrue, 6), Value(settings.WhenFalse, 4, false));
            TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "stock", Settings = changed });
            var source = StockTargetActionTests.Find(game, "source").Get<IInventory>(); var dest = StockTargetActionTests.Find(game, "destination").Get<IInventory>();
            var cargo = StockTargetActionTests.Find(game, "machine").Get<ITransporter>().Cargo; var supply = game.World.Content.ResourceId("supply");
            Reach(game, () => cargo.Amount(supply) == Fix.FromInt(6), "edited stock pickup");
            c.That(dest.Amount(supply).IsZero && dest.ReservedIn(supply) == Fix.FromInt(6), "edited stock makes actual claim/timed cargo");
            changed = Actions(changed, Value(changed.WhenTrue, 4, false), changed.WhenFalse);
            var before = StockTargetActionTests.Rule(game); c.That(TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "stock", Settings = changed }).Accepted, "player edits stock action while delivery travels");
            c.Equal(before.LastAppliedAt, StockTargetActionTests.Rule(game).LastAppliedAt, "in-flight edit preserves actual timestamp");
            var twin = factory.Load(game.Save()); Reach(game, () => !StockTargetActionTests.Stock(game).Enabled, "edited stock disable", twin);
            c.That(StockTargetActionTests.Stock(game).Desired == Fix.FromInt(4) && cargo.Amount(supply) == Fix.FromInt(6)
                && dest.ReservedIn(supply) == Fix.FromInt(6), "deferred disable retains cargo/receiving claims");
            c.Equal(game.Hash(), twin.Hash(), "saved stock edit commits while cargo remains in transit");
            TestKit.Order(game, 0, new RemoveAutomationCommand { Rule = "stock" }); twin = factory.Load(game.Save());
            Reach(game, () => dest.Amount(supply) == Fix.FromInt(6) && cargo.Amount(supply).IsZero, "disabled stock physical arrival", twin, 220);
            c.Equal(Fix.FromInt(20), source.Amount(supply) + dest.Amount(supply) + cargo.Amount(supply), "runtime edits conserve actual transported material");
            c.Equal(game.Hash(), twin.Hash(), "native/public physical arrival survives edit/cancellation");
        }
        private static void ReservePhysical(Check c, bool independent)
        {
            var factory = OutletReserveActionTests.Factory(independent); var game = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 413 });
            var settings = OutletReserveActionTests.Settings(game); var changed = Actions(settings, settings.WhenTrue, Value(settings.WhenFalse, 10));
            TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "reserve", Settings = changed });
            var supply = game.World.Content.ResourceId("supply"); var spare = game.World.Content.ResourceId("spare");
            var cargo = OutletReserveActionTests.Find(game, "machine").Get<ITransporter>().Cargo;
            Reach(game, () => cargo.Amount(supply) == Fix.FromInt(8), "edited reserve pickup");
            c.That(OutletReserveActionTests.Outlet(game).Reserve.AmountOf(supply) == Fix.FromInt(10)
                && OutletReserveActionTests.Outlet(game).Reserve.AmountOf(spare) == Fix.FromInt(3), "edited reserve preserves unrelated resource reserve");
            var twin = factory.Load(game.Save()); var dest = OutletReserveActionTests.Find(game, "destination").Get<IInventory>();
            Reach(game, () => dest.Amount(supply) == Fix.FromInt(10) && cargo.Amount(supply).IsZero, "edited reserve timed two-trip delivery", twin, 220);
            c.Equal(game.Hash(), twin.Hash(), "native/public edited reserve and physical hauling continue from save");
        }
        private static void StockRefusal(Check c, bool independent)
        {
            var factory = StockTargetActionTests.Factory(independent); var game = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 415 });
            var initial = StockTargetActionTests.Settings(game); var changed = Actions(initial, Value(initial.WhenTrue, 6), initial.WhenFalse);
            TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "stock", Settings = changed });
            Reach(game, () => StockTargetActionTests.Rule(game).HasApplied, "initial runtime stock action");
            var previous = StockTargetActionTests.Rule(game);
            changed = Actions(changed, Value(changed.WhenTrue, 8), changed.WhenFalse);
            TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "stock", Settings = changed });
            Reach(game, () => StockTargetActionTests.Rule(game).PendingDecision == true, "edited stock pending decision");
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = changed.Target.Entity, Resource = game.World.Content.ResourceId("supply"),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(2), Fix.FromInt(6), Fix.FromInt(7), 83, LogisticsReserveAccess.Protected)) }).Accepted, "later player limits admit through normal stock owner");
            var refused = StockTargetActionTests.Rule(game);
            c.That(refused.Suspended && refused.Blocker == Codes.InvalidArgument && refused.Settings.WhenTrue.Equals(changed.WhenTrue)
                && refused.LastAppliedAction.Value.Equals(previous.LastAppliedAction.Value) && refused.LastAppliedAt == previous.LastAppliedAt,
                "later native limit refusal preserves edited intent and actual applied history");
            c.That(StockTargetActionTests.Stock(game).Desired == Fix.FromInt(6) && StockTargetActionTests.Stock(game).Maximum == Fix.FromInt(7)
                && StockTargetActionTests.Stock(game).Priority == 83, "due runtime action cannot overwrite later player bounds/priority");
            var twin = factory.Load(game.Save()); for (int i = 0; i < 8; i++) { Update(game); Update(twin); }
            c.Equal(game.Hash(), twin.Hash(), "saved refused runtime stock action retains actual history and current physical jobs");
        }
    }
}
