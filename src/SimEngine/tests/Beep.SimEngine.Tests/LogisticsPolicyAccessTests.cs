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
    // JSON policy intent and public normal players only; no stock/receipt/route state writers.
    internal static class LogisticsPolicyAccessTests
    {
        internal const string Case = "Logistics policy access: private stock export and grouped reserves preserve physical delivery";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half, bool sourceHidden)
            => ResourceContentContractTests.Factory(half, new DirectResourceContentModule(), worldEdit: root => {
                var policies = Json.Parse(FrameworkProofContent.Read("logistics-policies.json"));
                policies.Get("logistics_policies")[1].Get("stock").Get("supply").Set("enabled", JsonValue.Of(false));
                root.Set("logistics_policies", policies.Get("logistics_policies").Clone());
                root.Get("entities")[0].Get("components").Set("logistics_policy", Json.Parse("{\"profiles\":[{\"port\":null,\"profile\":\"policy.source\"}]}"));
                root.Get("entities")[1].Get("components").Set("logistics_policy", Json.Parse("{\"profiles\":[{\"port\":null,\"profile\":\"policy.destination\"}]}"));
                var scenario = root.Get("scenarios")[0]; scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                if (scenario.Get("settings") == null) scenario.Set("settings", JsonValue.NewObject());
                var settings = Json.Parse("{\"target_kind\":\"entity\",\"target\":\"source\",\"initial_hidden\":false}"); settings.Set("target", JsonValue.Of(sourceHidden ? "source" : "destination"));
                scenario.Get("settings").Set(InventorySightModule.ModuleId, settings);
            }, extraModules: new GameModule[] { new InventorySightModule() },
            extraDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
        private static LogisticsView View(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<LogisticsView>();
        private static ConfigureStockPolicyCommand Band(EntityId id, ResourceId resource, bool enabled = true, int desired = 8)
            => new ConfigureStockPolicyCommand { Store = id, Resource = resource, Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(enabled, Fix.FromInt(2), Fix.FromInt(desired), Fix.FromInt(desired), 65, LogisticsReserveAccess.Protected)) };
        private static ConfigureOutletPolicyCommand Reserve(EntityId id, ResourceId resource, int amount)
            => new ConfigureOutletPolicyCommand { Source = id, Change = OutletPolicyChange.Reserve(resource, Fix.FromInt(amount), true) };
        private static ConfigureOutletPolicyGroupCommand Group(EntityId[] ids, ResourceId resource, int amount)
            => new ConfigureOutletPolicyGroupCommand { Sources = ids, Change = OutletPolicyChange.Reserve(resource, Fix.FromInt(amount), true) };
        private static Code Parity(Check c, SimGame g, int seat, Command command)
        {
            var view = View(g, seat);
            Code typed = command is ConfigureStockPolicyCommand band ? view.PreviewStockPolicy(band.Store, band.Resource, band.Change, band.Port, band.Endpoint) :
                command is ConfigureOutletPolicyCommand outlet ? view.PreviewOutletPolicy(outlet.Source, outlet.Change, outlet.Port) :
                view.PreviewOutletPolicyGroup(((ConfigureOutletPolicyGroupCommand)command).Sources, ((ConfigureOutletPolicyGroupCommand)command).Change, ((ConfigureOutletPolicyGroupCommand)command).Port).Blocker;
            c.Equal(typed, g.Seat(seat).Preview(command), "typed/ordinary stock/outlet/group admission share original evaluator"); return typed;
        }
        private static OutletPolicySettings ActualOutlet(SimGame g, EntityId id)
        { var code = g.Observer.Feature<LogisticsView>().ReadOutletPolicy(id, out var value); if (!code.IsNone) throw new InvalidOperationException("actual outlet/" + code); return value; }
        private static LogisticsDemandSettings ActualBand(SimGame g, EntityId id, ResourceId resource)
        { var code = g.Observer.Feature<LogisticsView>().ReadStockPolicy(id, resource, out var value); if (!code.IsNone) throw new InvalidOperationException("actual band/" + code); return value; }
        private static Fix Stock(SimGame g, EntityId id, ResourceId resource)
        { var code = g.Observer.Feature<LogisticsView>().ReadDiagnostics(id, resource, out var value); if (!code.IsNone) throw new InvalidOperationException("actual stock/" + code); return value.Stock; }
        private static void Reach(Check c, SimGame g, SimGame twin, Func<bool> done, EntityId[] ids, ResourceId resource, string stage)
        {
            for (int i = 0; i < 512 && !done(); i++)
            {
                TransformationContentContractTests.Step(g, twin); Fix total = Fix.Zero; foreach (var id in ids) total += Stock(g, id, resource);
                c.Equal(Fix.FromInt(20), total, "physical policy conservation/" + stage);
            }
            if (!done()) throw new InvalidOperationException(stage + "/clock=" + g.Clock.StepIndex + "/stock=" + string.Join(";", ids.Select(x => x + "/" + Stock(g, x, resource))) +
                "/jobs=" + string.Join(";", PermanentRemovalTests.Jobs(g).Select(x => x.Id + "/" + x.State + "/" + x.Blocker)));
        }
        private static void Hidden(Check c, SimGame g, EntityId masked, EntityId[] ids, ResourceId resource)
        {
            ulong pure = g.Hash();
            c.Equal(Codes.NotFound, View(g).ReadOutletPolicy(masked, out var outlet), "hidden outlet facts unavailable"); c.That(outlet == null, "no substituted empty outlet");
            c.Equal(Codes.NotFound, View(g).ReadStockPolicy(masked, resource, out var stock), "hidden stock facts unavailable before physical store lookup"); c.That(stock == null, "no substituted stock bands");
            foreach (int seat in new[] { 0, 1 }) foreach (Command command in new Command[] { Reserve(masked, resource, 20),
                new ConfigureOutletPolicyCommand { Source = masked, Change = null, Port = new Code("missing") },
                Band(masked, resource), new ConfigureStockPolicyCommand { Store = masked, Resource = default, Change = null, Port = new Code("missing") },
                Group(ids, resource, 20) })
                c.Equal(Codes.NotFound, Parity(c, g, seat, command), "private access precedes resource/port/settings details");
            var group = View(g).PreviewOutletPolicyGroup(ids, Group(ids, resource, 20).Change);
            var hidden = group.Entries.Single(x => x.Entity == masked);
            c.That(hidden.Current == null && hidden.Next == null, "hidden grouped member exposes neither current nor candidate policy");
            c.That(group.Entries.Single(x => x.Entity != masked).Current != null, "visible owned group member remains actual detached policy");
            c.Equal(Codes.NotFound, g.Observer.Feature<LogisticsView>().PreviewOutletPolicy(masked, Reserve(masked, resource, 20).Change), "observer diagnosis preserves owner refusal");
            c.Equal(pure, g.Hash(), "private reads/previews are pure");
        }
        private static void Healthy(Check c, bool half, bool sourceHidden)
        {
            var f = Factory(half, sourceHidden); var request = new NewGameRequest { Scenario = "points", Seed = 33777, Seats = { Controller.Human, Controller.Human } };
            var g = f.NewGame(request); var idle = f.NewGame(request); ulong isolated = idle.Hash();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current policy authority identity");
            var source = TransformationContentContractTests.Entity(g, "source"); var target = TransformationContentContractTests.Entity(g, "destination"); var machine = TransformationContentContractTests.Entity(g, "machine");
            c.That(g.Seat(0).View.TryFindResource("supply", out var resource), "actual resource identity"); var ids = new[] { source, target }; var stores = new[] { source, target, machine }; var masked = sourceHidden ? source : target;
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            c.That(!ActualBand(g, target, resource).Enabled, "authored physical initial stock config applies without player command");
            c.Equal(Code.None, Parity(c, g, 0, Reserve(source, resource, 4)), "visible export admission");
            c.Equal(Code.None, Parity(c, g, 0, Band(target, resource)), "visible demand admission");
            c.Equal(Codes.InvalidArgument, Parity(c, g, 0, new ConfigureStockPolicyCommand { Store = target, Resource = resource, Change = null }), "visible malformed demand remains typed");
            foreach (var invalid in new[] { Array.Empty<EntityId>(), new[] { source, source }, new[] { EntityId.None } })
                c.Equal(Codes.InvalidArgument, Parity(c, g, 0, Group(invalid, resource, 4)), "malformed grouped policy shape refuses");
            c.Equal(Codes.NotFound, Parity(c, g, 1, Reserve(source, resource, 4)), "foreign export authority private");
            c.Equal(Codes.NotFound, Parity(c, g, 1, Band(target, resource)), "foreign stock authority private");
            PermanentRemovalTests.Submit(c, g, Band(target, resource));
            Reach(c, g, null, () => Stock(g, machine, resource).IsPositive, stores, resource, "physical automatic stock pickup");
            var beforeSource = ActualOutlet(g, source); var beforeTarget = ActualOutlet(g, target); var beforeBand = ActualBand(g, target, resource);
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = true }); Hidden(c, g, masked, ids, resource);
            var pending = new List<(int Seat, int Sequence)>();
            foreach (int seat in new[] { 0, 1 }) foreach (Command command in new Command[] { Reserve(masked, resource, 20), Band(masked, resource, false, 9), Group(ids, resource, 20) })
                pending.Add((seat, g.Seat(seat).Submit(command)));
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "hidden policies/acquired physical stock and pending actual commands durable");
            TransformationContentContractTests.Step(g, twin);
            foreach (var issue in pending)
            {
                long cursor = 0; var results = new List<CommandResult>(); g.Seat(issue.Seat).ReadResults(ref cursor, results);
                var result = results.Single(x => x.Sequence == issue.Sequence); c.That(!result.Accepted && result.Refusal == Codes.NotFound, "executed private stock/export/group controls refuse");
            }
            c.That(ActualOutlet(g, source).Matches(beforeSource) && ActualOutlet(g, target).Matches(beforeTarget) &&
                ActualBand(g, target, resource).Desired == beforeBand.Desired && ActualBand(g, target, resource).Enabled, "refused controls change no visible prefix or actual hidden policy");
            Reach(c, g, twin, () => Stock(g, target, resource) == Fix.FromInt(8) && Stock(g, machine, resource).IsZero, stores, resource, "hidden physical automatic delivery");
            PermanentRemovalTests.Submit(c, g, new SetInventorySightCommand { Hidden = false });
            c.Equal(Code.None, Parity(c, g, 0, Group(ids.Reverse().ToArray(), resource, 4)), "public grouped export control needs no named fleet");
            PermanentRemovalTests.Submit(c, g, Group(ids.Reverse().ToArray(), resource, 4));
            c.That(ActualOutlet(g, source).Reserve.AmountOf(resource) == Fix.FromInt(4) && ActualOutlet(g, target).Reserve.AmountOf(resource) == Fix.FromInt(4), "actual sorted group reserve commits original owners");
            c.Equal(Fix.FromInt(12), Stock(g, source, resource), "finite source charged eight once");
            c.Equal(Fix.FromInt(4), beforeSource.Reserve.AmountOf(resource), "retained immutable reserve remains old fact");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "settled policy/demand/stock state durable"); c.Equal(isolated, idle.Hash(), "interleaved sessions isolated");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "normal policy/group/private refusals and physical delivery replay");
        }
        private static void Run(Check c) { foreach (bool half in new[] { false, true }) foreach (bool sourceHidden in new[] { false, true }) Healthy(c, half, sourceHidden); }
    }
}

