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

namespace Beep.SimEngine.Tests
{
    // Player intent and bounded observations only; all matching, travel, reservations and controls run in the engine.
    internal static class OutletReserveActionTests
    {
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        internal static GameFactory Factory(bool independent = false, Action<JsonValue> edit = null, string hiddenDependency = null,
            string controlsFile = "outlet-reserve-automation.json", bool nativeDestination = false, string workFile = null,
            ResourceControlDeclarationProofModule controlDeclarations = null, Action<JsonValue> extraControlEdit = null)
        {
            JsonValue definitions = Json.Parse(PointGame.Definitions);
            var source = definitions.Get("entities")[0].Get("components"); source.Remove("point_stock");
            source.Set("storage", Json.Parse("{\"capacity_policy\":\"fixture.count.40\",\"initial_stock\":{\"supply\":20,\"spare\":5},\"public\":true,\"evacuate\":false,\"phases\":[],\"accepts\":[\"supply\",\"spare\"]}"));
            source.Set(independent ? "contract_outlet_policy" : "logistics_policy", Json.Parse(independent ? "{\"profile\":\"reserve.source\"}" : "{\"profiles\":[{\"port\":null,\"profile\":\"reserve.source\"}]}"));
            source.Set("inventory_signal", Json.Parse("{\"id\":\"feed\",\"inventory\":\"storage\",\"quantities\":null}"));
            definitions.Get("entities")[1].Get("components").Set("logistics_policy", Json.Parse("{\"profiles\":[{\"port\":null,\"profile\":\"reserve.destination\"}]}"));
            if (nativeDestination)
            {
                var destination = definitions.Get("entities")[1].Get("components"); destination.Remove("point_stock");
                destination.Set("storage", Json.Parse("{\"capacity_policy\":\"fixture.count.40\",\"initial_stock\":{},\"public\":true,\"evacuate\":false,\"phases\":[],\"accepts\":[\"supply\",\"spare\"]}"));
            }
            StartupContentAuthoring.Add(definitions, "source.foreign", "source", 1, definitions.Get("entities")[0].Get("components").Get("point_presence"));
            StartupContentAuthoring.Add(definitions, "destination.foreign", "destination", 1, definitions.Get("entities")[1].Get("components").Get("point_presence"));
            var scenario = definitions.Get("scenarios")[0]; scenario.Set("seats", JsonValue.Number("2")); scenario.Set("default_seats", JsonValue.Number("2"));
            scenario.Get("modules").Add(JsonValue.Of(AutomationModule.ModuleId)); scenario.Get("modules").Add(JsonValue.Of(InventorySignalsModule.ModuleId));
            var content = Json.Parse(FrameworkProofContent.Read(controlsFile)); edit?.Invoke(content);
            var modules = new List<GameModule> { new AutomationModule(), new InventorySignalsModule() };
            var declarations = Json.Parse("[{\"id\":\"sim.automation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[\"outlet-reserve-automation.json\"]},{\"id\":\"sim.inventory-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
            var selectedFiles = JsonValue.NewArray(); selectedFiles.Add(JsonValue.Of(controlsFile)); declarations[0].Set("documents", selectedFiles);
            var documents = new Dictionary<string, string> { [controlsFile] = Text(content) };
            if (controlDeclarations != null)
            {
                modules.Add(controlDeclarations); scenario.Get("modules").Add(JsonValue.Of(controlDeclarations.Id));
                declarations.Add(Json.Parse("{\"id\":\"proof.resource-control-declarations\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.automation\",\"version\":\"1.0.0\"}],\"documents\":[\"resource-control-declarations.json\"]}"));
                var extra = Json.Parse(FrameworkProofContent.Read(ResourceControlDeclarationProofModule.Document));
                extraControlEdit?.Invoke(extra);
                if (controlDeclarations.UseDeclarations)
                    foreach (JsonValue document in new[] { content, extra })
                        foreach (string section in new[] { "stock_control_actions", "outlet_reserve_control_actions" })
                            if (document.Get(section) != null)
                            { document.Set("declared_" + section, document.Get(section)); document.Remove(section); }
                documents[controlsFile] = Text(content);
                documents.Add(ResourceControlDeclarationProofModule.Document, Text(extra));
            }
            if (hiddenDependency != null)
            {
                modules.Add(new AutomationKnowledgeVisibilityModule()); scenario.Get("modules").Add(JsonValue.Of(KnowledgeVisibilityModule.ModuleId));
                scenario.Get("settings").Set(KnowledgeVisibilityModule.ModuleId, Json.Parse("{\"policy\":\"reserve.profile.visibility\"}"));
                declarations.Add(Json.Parse("{\"id\":\"sim.knowledge-visibility\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"outlet-reserve-visibility.json\"]}"));
                var visibility = Json.Parse(FrameworkProofContent.Read("outlet-reserve-visibility.json"));
                var rules = JsonValue.NewArray(); rules.Add(JsonValue.Of("reserve.visibility." + hiddenDependency));
                visibility.Get("knowledge_visibility_policies")[0].Set("rules", rules);
                documents.Add("outlet-reserve-visibility.json", Text(visibility));
            }
            var workDocuments = workFile == null ? null : new Dictionary<string, string> { [workFile] = FrameworkProofContent.Read(workFile) };
            return PointGame.Factory(Text(definitions), extraModules: modules, extraDeclarations: Text(declarations), extraDocuments: documents, additionalDocuments: workDocuments);
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 397 });
        internal static Entity Find(SimGame game, string prototype, int company = 0)
        {
            foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == prototype && entity.Owner == new PlayerId(company)) return entity;
            throw new InvalidOperationException("missing reserve fixture " + prototype);
        }
        private static ResourceId Resource(SimGame game, string resource = "supply") => game.World.Content.ResourceId(resource);
        private static LogisticsView Logistics(SimGame game, int company = 0) => game.Seat(company).View.Feature<LogisticsView>();
        private static OperationsView Operations(SimGame game, int company = 0) => game.Seat(company).View.Feature<OperationsView>();
        internal static OutletPolicySettings Outlet(SimGame game)
        {
            Code refusal = Logistics(game).ReadOutletPolicy(Find(game, "source").Id, out var settings);
            if (!refusal.IsNone) throw new InvalidOperationException("reserve settings unavailable " + refusal); return settings;
        }
        internal static AutomationSettings Settings(SimGame game, bool release = false)
        {
            if (!Operations(game).TryGetAutomationProfile("reserve.band", out var profile)) throw new InvalidOperationException("reserve profile unavailable");
            var targets = new List<Code>();
            if (!Operations(game).CopyControlTargetIds(Find(game, "source").Id, profile.Id, targets) || targets.Count != 1)
                throw new InvalidOperationException("reserve target discovery unavailable");
            var thresholds = new Dictionary<string, ControlThresholdBand>();
            foreach (var band in profile.Thresholds) thresholds.Add(band.Key, release
                ? new ControlThresholdBand(band.Value.Initial.Unit, Fix.FromInt(15), Fix.FromInt(10)) : band.Value.Initial);
            return new AutomationSettings(profile.Id, true, new ControlBinding(Find(game, "source").Id, targets[0]),
                new Dictionary<string, ControlBinding> { ["stock"] = new ControlBinding(Find(game, "source").Id, new Code("feed")) }, thresholds, profile.WhenTrue, profile.WhenFalse);
        }
        private static AutomationRuleSnapshot Rule(SimGame game)
            => Operations(game).TryGetAutomationRule(new PlayerId(0), "reserve", out var rule) ? rule : null;
        private static CommandResult Configure(SimGame game, bool release = false)
            => TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "reserve", Settings = Settings(game, release) });
        private static void Update(SimGame game) { game.Update(); if (game.Fault != null) throw new InvalidOperationException(game.Fault.ToString()); }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 180 && !done(); i++) { Update(game); if (twin != null) Update(twin); }
            if (!done())
            {
                var jobs = new List<string>(); foreach (var job in game.World.Services.Get<WorkBoard>().Jobs) jobs.Add(job.Id + "/" + job.State + "/" + job.Blocker);
                throw new InvalidOperationException(stage + " timed out at game step " + game.Clock.StepIndex
                    + "; reserve=" + Outlet(game).Reserve.AmountOf(Resource(game)) + "; rule=" + Rule(game)?.Blocker + "; " + string.Join(";", jobs));
            }
        }
        private static CommandResult Export(SimGame game, OutletPolicyChange change)
            => TestKit.Order(game, 0, new ConfigureOutletPolicyCommand { Source = Find(game, "source").Id, Change = change });

        internal static void Contracts(Check c)
        {
            foreach (var fault in new[] { "resource", "unit", "command", "owner", "missing-resource", "missing-unit" })
                c.Throws<ContentException>(() => New(Factory(edit: root => {
                    var policy = root.Get("outlet_reserve_control_actions")[0];
                    if (fault.StartsWith("missing-", StringComparison.Ordinal)) policy.Remove(fault.Substring(8));
                    else policy.Set(fault, JsonValue.Of(fault == "unit" ? "unit.capacity.volume" : "missing"));
                })), "native reserve action requires exact content and normal command: " + fault);
            c.Throws<ContentException>(() => AutomationTests.New(AutomationTests.Factory(root => {
                root.Set("outlet_reserve_control_actions", Json.Parse("[{\"id\":\"reserve.missing-module\",\"command\":\"sim.configure-outlet-policy\",\"owner\":\"sim.logistics\",\"resource\":\"supply\",\"unit\":\"unit.kit\"}]"));
            })), "selected reserve definition cannot invent an absent logistics module/handler");
            foreach (bool independent in new[] { false, true })
            {
                var game = New(Factory(independent)); var source = Find(game, "source"); var settings = Settings(game); ulong hash = game.Hash();
                c.That(Operations(game).TryGetAutomationProfile("reserve.band", out var profile) && profile.ActionCommand == "sim.configure-outlet-policy"
                    && profile.ActionCommandOwner == LogisticsModule.ModuleId, "reserve profile selects ordinary logistics command, native/mod=" + independent);
                c.Equal(Code.None, Operations(game).PreviewAutomation("reserve", settings), "reserve rule normal-handler preview succeeds");
                c.Equal(hash, game.Hash(), "reserve rule/profile/target discovery leaves state unchanged");
                var invalidViewer = new LogisticsView(game.World, new PlayerId(-2));
                c.Equal(Codes.NotFound, invalidViewer.ReadOutletPolicy(source.Id, out _), "invalid logistics viewer is not an observer");
                c.Equal(Codes.NotFound, invalidViewer.PreviewOutletPolicy(source.Id, OutletPolicyChange.Reserve(Resource(game), Fix.One, true)), "invalid logistics viewer cannot inspect reserve admission");
                c.That(source.Get<IOutletPolicy>() is ContractOutletPolicy == independent, "same contract dispatch uses actual native/public provider");
                var ids = new List<Code> { new Code("stale") };
                c.That(Operations(game).CopyControlTargetIds(Find(game, "machine").Id, "reserve.band", ids) && ids.Count == 0, "unsupported entity supplies no fabricated outlet target");
                c.That(!Operations(game).CopyControlTargetIds(Find(game, "source", 1).Id, "reserve.band", ids) && ids.Count == 0, "foreign targets stay private");
                c.That(!Operations(game).CopyControlTargetIds(source.Id, "missing", ids) && ids.Count == 0, "unknown profile cannot substitute another action");
                c.Throws<ArgumentNullException>(() => Operations(game).CopyControlTargetIds(source.Id, "reserve.band", null), "target discovery rejects missing destination");
                var invalid = new AutomationSettings(settings.Profile, true, new ControlBinding(source.Id, new Code("storage")), settings.Inputs, settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
                c.Equal(LogisticsPolicyCodes.NoPolicy, Operations(game).PreviewAutomation("wrong", invalid), "reserve binding is the public outlet contract slot, not a guessed storage key");
                var foreign = new AutomationSettings(settings.Profile, true, new ControlBinding(Find(game, "source", 1).Id, settings.Target.Capability), settings.Inputs, settings.Thresholds, settings.WhenTrue, settings.WhenFalse);
                c.Equal(Codes.NotOwned, Operations(game).PreviewAutomation("foreign", foreign), "reserve action never acquires a foreign provider");
                foreach (var change in new[] { OutletPolicyChange.Reserve(Resource(game), Fix.Parse("1.5"), true),
                    OutletPolicyChange.Reserve(Resource(game), Fix.FromInt(38), true), OutletPolicyChange.Reserve(new ResourceId(999), Fix.Zero, true) })
                {
                    hash = game.Hash();
                    c.Equal(Codes.InvalidArgument, Logistics(game).PreviewOutletPolicy(source.Id, change), "normal reserve preview validates exact native quantity/combined capacity/zero resource");
                    c.Equal(hash, game.Hash(), "invalid normal reserve preview preserves settings and reservations");
                    c.Equal(Codes.InvalidArgument, Export(game, change).Refusal, "normal command agrees with reserve preview");
                }
                var copy = (ConfigureOutletPolicyCommand)game.World.Commands.Copy(new ConfigureOutletPolicyCommand { Source = source.Id,
                    Change = OutletPolicyChange.Reserve(Resource(game), Fix.Zero, false) });
                c.That(copy.Change.Kind == OutletPolicyChange.ChangeKind.Reserve && copy.Change.Resource == Resource(game)
                    && copy.Change.Amount.IsZero && !copy.Change.Enabled, "normal command codec retains exact zero/enabled reserve intent");
                c.That(Export(game, copy.Change).Accepted, "zero reserve and disabled export use ordinary command");
                c.That(Outlet(game).Reserve.AmountOf(Resource(game)).IsZero && Outlet(game).Reserve.AmountOf(Resource(game, "spare")) == Fix.FromInt(3)
                    && !Outlet(game).Enabled && Outlet(game).ExportPriority == 3 && Outlet(game).AllowedDestinations[0] == "destination",
                    "one reserve removal preserves all unrelated actual policy facts");
            }
            foreach (var target in new[] { "1.5", "38", "-1" })
            {
                var game = New(Factory(edit: root => root.Get("control_rules")[0].Get("when_true").Set("target", JsonValue.Number(target))));
                c.Equal(Codes.InvalidArgument, Operations(game).PreviewAutomation("reserve", Settings(game)), "automation preserves normal quantity admission: " + target);
                c.Equal(Codes.InvalidArgument, Configure(game).Refusal, "automation configure agrees without a partial rule: " + target);
                c.That(Rule(game) == null, "invalid reserve action publishes no policy prefix");
            }
            var wrongUnit = New(Factory(edit: root => {
                root.Get("control_rules")[0].Get("when_true").Set("unit", JsonValue.Of("unit.capacity.count"));
                root.Get("control_rules")[0].Get("when_false").Set("unit", JsonValue.Of("unit.capacity.count"));
            }));
            c.Equal(AutomationCodes.UnitMismatch, Operations(wrongUnit).PreviewAutomation("reserve", Settings(wrongUnit)), "action unit is exact native resource unit, not another dimension-compatible label");
            foreach (string dependency in new[] { "resource", "unit" })
            {
                var hidden = New(Factory(hiddenDependency: dependency)); var observer = hidden.Observer.Feature<OperationsView>();
                c.That(observer.TryGetAutomationProfile("reserve.band", out var profile), "explicit observer can read complete action profile");
                var settings = new AutomationSettings(profile.Id, true, new ControlBinding(Find(hidden, "source").Id, LogisticsPolicyRules.OutletPolicyBinding),
                    new Dictionary<string, ControlBinding> { ["stock"] = new ControlBinding(Find(hidden, "source").Id, new Code("feed")) },
                    new Dictionary<string, ControlThresholdBand> { ["high"] = profile.Thresholds["high"].Initial }, profile.WhenTrue, profile.WhenFalse);
                ulong hash = hidden.Hash(); var ids = new List<Code>();
                c.That(!Operations(hidden).TryGetAutomationProfile(profile.Id, out _), "hidden native action dependency withholds entire profile: " + dependency);
                c.That(!Operations(hidden).CopyControlTargetIds(settings.Target.Entity, profile.Id, ids) && ids.Count == 0, "hidden action dependency withholds target discovery: " + dependency);
                c.Equal(Codes.UnknownDef, Operations(hidden).PreviewAutomation("reserve", settings), "hidden action dependency uses same configure admission gate: " + dependency);
                c.Equal(hash, hidden.Hash(), "hidden reserve diagnostics remain pure: " + dependency);
            }
            Corrupt(c);
        }
        private static void Corrupt(Check c)
        {
            foreach (byte kind in new byte[] { 0, 3, 255 })
            {
                using var stream = new MemoryStream(); var writer = new StateWriter(stream); byte value = kind; writer.Byte(ref value); writer.Flush(); var data = stream.ToArray();
                c.Throws<CorruptStateException>(() => OutletPolicyChange.Sync(new StateReader(data, 0, data.Length), null), "unknown saved outlet intent does not load as replacement/reserve: " + kind);
            }
            c.Throws<ArgumentException>(() => OutletPolicyChange.Reserve(new ResourceId(1), -Fix.One, true), "negative reserve has no clamped intent");
            c.Throws<ArgumentNullException>(() => OutletPolicyChange.Replace(null), "replacement has no inferred policy defaults");
        }

        internal static void Execution(Check c)
        {
            foreach (bool independent in new[] { false, true }) Physical(c, independent);
            var game = New(Factory()); Configure(game); Update(game); Update(game);
            c.That(Rule(game).PendingDecision == false, "reserve action earns dwell before normal commit");
            c.That(TestKit.Order(game, 0, new RemoveAutomationCommand { Rule = "reserve" }).Accepted && Rule(game) == null,
                "ordinary cancellation removes pending reserve decision");
            c.Equal(Fix.FromInt(20), Outlet(game).Reserve.AmountOf(Resource(game)), "cancelled reserve action cannot apply later");
            var disabled = New(Factory(edit: root => root.Get("control_rules")[0].Get("when_false").Set("enabled", JsonValue.Of(false))));
            c.That(Configure(disabled).Accepted, "native reserve rule can author disabled exports, not just a quantity");
            Update(disabled); Update(disabled); c.That(Outlet(disabled).Enabled, "disabled export action also waits for normal deferred commit");
            Update(disabled);
            c.That(!Outlet(disabled).Enabled && Rule(disabled).HasApplied && Outlet(disabled).Reserve.AmountOf(Resource(disabled)) == Fix.FromInt(20),
                "native action interprets enabled state through the ordinary outlet owner");
            CommitRefusal(c); SavedCommand(c);
        }
        private static void CommitRefusal(Check c)
        {
            foreach (bool independent in new[] { false, true })
            {
                var factory = Factory(independent); var game = New(factory); Configure(game); Update(game); Update(game);
                var next = new OutletPolicySettings(true, new Bill(new[] { new ResourceAmount(Resource(game), Fix.FromInt(4)),
                    new ResourceAmount(Resource(game, "spare"), Fix.FromInt(25)) }), 7, new[] { "destination" }, LogisticsReserveAccess.Protected);
                c.That(Export(game, OutletPolicyChange.Replace(next)).Accepted, "player can change complete valid policy before due reserve commit");
                c.That(Rule(game).Suspended && Rule(game).Blocker == Codes.InvalidArgument && !Rule(game).HasApplied,
                    "due reserve action rechecks combined current capacity and suspends without resetting other reserves");
                c.That(Outlet(game).Reserve.AmountOf(Resource(game)) == Fix.FromInt(4)
                    && Outlet(game).Reserve.AmountOf(Resource(game, "spare")) == Fix.FromInt(25) && Outlet(game).ExportPriority == 7,
                    "refused due action leaves actual player settings intact");
                var twin = factory.Load(game.Save()); for (int i = 0; i < 5; i++) { Update(game); Update(twin); }
                c.Equal(game.Hash(), twin.Hash(), "saved native capacity refusal and its existing physical jobs continue identically");
            }
        }
        private static void SavedCommand(Check c)
        {
            var factory = Factory(); var game = New(factory); var source = Find(game, "source").Id; var supply = Resource(game);
            var command = new ConfigureOutletPolicyCommand { Source = source, Change = OutletPolicyChange.Reserve(supply, Fix.FromInt(6), false) };
            game.Seat(0).Submit(command); command.Change = OutletPolicyChange.Reserve(supply, Fix.FromInt(39), true);
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending ordinary reserve command persists through existing inbox codec");
            Update(game); Update(twin);
            c.That(Outlet(game).Reserve.AmountOf(supply) == Fix.FromInt(6) && !Outlet(game).Enabled,
                "saved normal reserve command uses submitted detached intent, not later caller mutation");
            c.Equal(game.Hash(), twin.Hash(), "pending normal reserve command commits identically after save/load");
        }
        private static void Physical(Check c, bool independent)
        {
            var factory = Factory(independent); var game = New(factory); var supply = Resource(game); var spare = Resource(game, "spare");
            c.That(Configure(game).Accepted, "player installs authored stock-driven reserve rule, mod=" + independent);
            Update(game); Update(game);
            c.That(Rule(game).PendingDecision == false && Outlet(game).Reserve.AmountOf(supply) == Fix.FromInt(20), "pending decision does not move stock or silently release reserve");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending native reserve rule saves through existing owners");
            game.Clock.Pause(); ulong paused = game.Hash(); c.Equal(0, game.Update(), "single clock pause holds outlet automation"); c.Equal(paused, game.Hash(), "pause changes no reserve/work state"); game.Clock.Resume();
            Update(game); Update(twin); c.Equal(game.Hash(), twin.Hash(), "deferred reserve commit survives saved continuation");
            c.That(Rule(game).HasApplied && !Rule(game).Latch && Find(game, "destination").Get<IInventory>().Amount(supply).IsZero,
                "stock-demand matching honors held reserve without instant deliveries");
            c.That(Configure(game, release: true).Accepted, "player changes typed thresholds, not scheduler decisions");
            Reach(game, () => Rule(game).PendingDecision == true, "reserve release dwell/cooldown");
            var updated = new OutletPolicySettings(true, new Bill(new[] { new ResourceAmount(supply, Fix.FromInt(20)), new ResourceAmount(spare, Fix.FromInt(4)) }), 17, new[] { "destination" }, LogisticsReserveAccess.Protected);
            c.That(Export(game, OutletPolicyChange.Replace(updated)).Accepted, "player edits unrelated live settings while automation intent is pending");
            c.That(Outlet(game).ExportPriority == 17 && Outlet(game).Reserve.AmountOf(spare) == Fix.FromInt(4), "deferred action resolves current unrelated settings, not initial profile defaults");
            var cargo = Find(game, "machine").Get<ITransporter>().Cargo; var dest = Find(game, "destination").Get<IInventory>(); var source = Find(game, "source").Get<IInventory>();
            Reach(game, () => cargo.Amount(supply) == Fix.FromInt(8), "physical reserve-enabled pickup");
            c.That(dest.Amount(supply).IsZero && dest.ReservedIn(supply).IsPositive, "truck carries acquired supply and actual receiving reservation before delivery");
            Fix promised = dest.ReservedIn(supply), acquired = cargo.Amount(supply), origin = source.Amount(supply);
            c.That(Export(game, OutletPolicyChange.Reserve(supply, Fix.FromInt(20), false)).Accepted, "ordinary emergency player edit stops new exports during physical travel");
            c.That(dest.ReservedIn(supply) == promised && cargo.Amount(supply) == acquired && source.Amount(supply) == origin,
                "reserve edit never rewrites acquired cargo, source stock or the live receiving receipt");
            c.That(TestKit.Order(game, 0, new RemoveAutomationCommand { Rule = "reserve" }).Accepted, "player cancels further automatic changes without cancelling physical haul");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "in-flight cargo/reserve/current settings checkpoint restores");
            long start = game.Clock.StepIndex;
            Reach(game, () => cargo.Amount(supply).IsZero && dest.Amount(supply).IsPositive, "disabled exporter committed physical delivery", twin);
            c.That(game.Clock.StepIndex > start && dest.Amount(supply) == acquired, "hauling completes over actual game time, not reserve-command teleportation");
            c.Equal(game.Hash(), twin.Hash(), "saved physical delivery uses identical reservations, cargo and single-clock timing");
            c.That(!Outlet(game).Enabled && Outlet(game).Reserve.AmountOf(supply) == Fix.FromInt(20)
                && Outlet(game).Reserve.AmountOf(spare) == Fix.FromInt(4) && Outlet(game).ExportPriority == 17
                && Outlet(game).AllowedDestinations.Count == 1 && Outlet(game).AllowedDestinations[0] == "destination",
                "current edited policy survives automation cancellation and actual delivery");
            c.Equal(Fix.FromInt(20), source.Amount(supply) + cargo.Amount(supply) + dest.Amount(supply), "reserve automation and physical delivery conserve native material");
        }
    }
}
