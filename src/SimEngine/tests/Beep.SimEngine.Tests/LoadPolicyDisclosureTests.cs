using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    // Only original JSON, commands and views; native policy/logistics/work/motion owners execute everything.
    internal static class LoadPolicyDisclosureTests
    {
        internal const string Case = "Load policy disclosure: private profiles and minimum resources preserve known replacements and saved delivery";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(bool resource, string hiddenResource = null, Action<JsonValue> edit = null)
        {
            var rules = Json.Parse(FrameworkProofContent.Read("haul-trip-policy.json"));
            var profiles = Json.Parse(FrameworkProofContent.Read("haul-load-policies.json")).Get("haul_load_profiles").Clone();
            var visible = profiles[1].Clone(); visible.Set("id", JsonValue.Of("load.visible")); visible.Set("minimum_load", JsonValue.NewObject()); profiles.Add(visible);
            rules.Set("haul_load_profiles", profiles);
            return HaulTripPolicyTests.Factory(rules: rules.ToString(), edit: document => {
                document.Get("entities")[1].Get("components").Set("haul_load_policy", Json.Parse("{\"profile\":\"load.batch\",\"states\":{\"disabled\":\"blocked\"}}"));
                document.Get("entities")[2].Get("components").Set("haul_load_policy", Json.Parse("{\"profile\":\"load.visible\",\"states\":{\"disabled\":\"blocked\"}}"));
                document.Get("entity_setups")[1].Get("after").Add(JsonValue.Of("reserve.courier"));
                edit?.Invoke(document);
            }, editPointScenario: scenario => {
                scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                var sight = Json.Parse(resource
                    ? "{\"target_kind\":\"resource\",\"target\":\"supply\",\"initial_hidden\":false}"
                    : "{\"target_kind\":\"load-profile\",\"target\":\"load.batch\",\"initial_hidden\":false}");
                if (resource && hiddenResource != null) sight.Set("target", JsonValue.Of(hiddenResource));
                scenario.Get("settings").Set(InventorySightModule.ModuleId, sight);
            }, pointModules: new GameModule[] { new InventorySightModule() },
                pointDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
        }
        private static LogisticsView View(SimGame game) => game.Seat(0).View.Feature<LogisticsView>();
        private static HaulLoadSettings Known() => new HaulLoadSettings("load.visible", true, Bill.Empty, GameDuration.Minutes(0), false);
        private static HaulLoadSettings Private(bool resource, ResourceId supply) => new HaulLoadSettings(resource ? "load.visible" : "load.batch", true,
            resource ? new Bill(new[] { new ResourceAmount(supply, Fix.One) }) : Bill.Empty, GameDuration.Zero, false);
        private static HaulLoadSettings Actual(SimGame game, EntityId entity)
        {
            var refusal = game.Observer.Feature<LogisticsView>().ReadHaulLoadPolicy(entity, out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("actual load settings/" + refusal); return facts.Settings;
        }
        private static bool Same(HaulLoadSettings left, HaulLoadSettings right, ResourceId supply)
            => left.Profile == right.Profile && left.Enabled == right.Enabled && left.MaximumWait.Equals(right.MaximumWait)
                && left.EmergencyBypass == right.EmergencyBypass && left.MinimumLoad.AmountOf(supply) == right.MinimumLoad.AmountOf(supply);
        internal static Fix Stock(SimGame game, EntityId entity, ResourceId supply)
        {
            var refusal = game.Observer.Feature<LogisticsView>().ReadDiagnostics(entity, supply, out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("actual load disclosure stock/" + refusal); return facts.Stock;
        }
        private static ConfigureHaulLoadPolicyGroupCommand Group(EntityId[] entities, HaulLoadSettings settings)
            => new ConfigureHaulLoadPolicyGroupCommand { Workers = entities, Settings = settings };
        private static void Hidden(Check c, SimGame game, EntityId worker, EntityId visible, ResourceId supply, bool resource)
        {
            var view = View(game); ulong hash = game.Hash(); long at = game.Clock.StepIndex; var ids = new[] { worker, visible };
            c.That(game.Seat(0).View.TryGetEntity(worker, out _) && game.Seat(0).View.TryGetEntity(visible, out _), "both cached owned workers remain visible");
            c.Equal(!resource, game.Seat(0).View.TryFindResource("supply", out _), "only selected profile or actual minimum resource metadata is hidden");
            c.Equal(Codes.NotFound, view.ReadHaulLoadPolicy(worker, out var hidden), "management withholds the whole private profile/minimum bill");
            c.That(hidden == null, "no private settings snapshot or substituted empty minimum");
            c.Equal(Code.None, view.ReadHaulLoadPolicy(visible, out var known), "unrelated known policy stays visible");
            c.That(known.Profile == "load.visible" && known.MinimumLoad.IsEmpty, "authored known no-minimum policy remains exact");
            foreach (var id in ids)
            {
                c.Equal(Codes.NotFound, view.PreviewHaulLoadPolicy(id, Private(resource, supply)), "private requested profile/minimum refuses typed configure");
                c.Equal(Codes.NotFound, game.Seat(0).Preview(new ConfigureHaulLoadPolicyCommand { Worker = id, Settings = Private(resource, supply) }), "ordinary single preview agrees");
            }
            var denied = view.PreviewHaulLoadPolicyGroup(ids, Private(resource, supply));
            c.Equal(Codes.NotFound, denied.Blocker, "whole private grouped request refuses");
            c.That(denied.Entries.All(row => row.Settings == null), "private request yields no visible member settings prefix");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(Group(ids, Private(resource, supply))), "ordinary grouped preview agrees");
            var allowed = view.PreviewHaulLoadPolicyGroup(ids, Known());
            c.Equal(Code.None, allowed.Blocker, "known replacement remains allowed despite hidden old intent");
            c.That(allowed.Entries.Single(row => row.Entity == visible).Settings?.Profile == "load.visible"
                && allowed.Entries.Single(row => row.Entity == worker).Settings == null, "ready replacement withholds only hidden current settings");
            c.Equal(Code.None, game.Seat(0).Preview(Group(ids, Known())), "normal group admission does not require reading hidden old settings");
            c.Equal(HaulLoadPolicyCodes.Invalid, view.PreviewHaulLoadPolicy(worker, new HaulLoadSettings("missing.load", true, Bill.Empty, GameDuration.Zero, false)), "unknown profile preserves native invalid validation");
            c.Equal(HaulLoadPolicyCodes.Invalid, view.PreviewHaulLoadPolicy(worker, new HaulLoadSettings("load.visible", true,
                new Bill(new[] { new ResourceAmount(new ResourceId(99999), Fix.One) }), GameDuration.Zero, false)), "unknown resource is not replaced or defaulted by disclosure checks");
            c.Equal(hash, game.Hash(), "all policy/request/current projections remain pure"); c.Equal(at, game.Clock.StepIndex, "disclosure never advances game time");
        }
        private static void Run(Check c)
        {
            foreach (bool resource in new[] { false, true })
            {
                var factory = Factory(resource); var request = new NewGameRequest { Scenario = "points", Seed = 12323, Seats = { Controller.Human, Controller.Human } };
                var game = factory.NewGame(request); var idle = factory.NewGame(request); ulong isolated = idle.Hash();
                var worker = TransformationContentContractTests.Entity(game, "reserve.truck"); var visible = TransformationContentContractTests.Entity(game, "reserve.courier");
                var source = TransformationContentContractTests.Entity(game, "source"); var target = TransformationContentContractTests.Entity(game, "destination"); var supply = PermanentRemovalTests.Supply(game);
                c.That(visible.Value < worker.Value, "authored visible-first member order exercises real whole admission");
                var oldWorker = Actual(game, worker); var oldVisible = Actual(game, visible); var ids = new[] { worker, visible };
                c.That(Same(oldVisible, Known(), supply), "visible member already has the exact file-authored requested value, including duration unit");
                var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recording = game.RecordReplay(limits);
                PermanentRemovalTests.Submit(c, game, new SetInventorySightCommand { Hidden = true }); Hidden(c, game, worker, visible, supply, resource);
                var refused = factory.Load(game.Save()); var refusedTwin = factory.Load(game.Save()); using (var rejected = refused.RecordReplay(limits))
                {
                    var sequences = new[] { refused.Seat(0).Submit(Group(ids, Private(resource, supply))),
                        refused.Seat(0).Submit(new ConfigureHaulLoadPolicyCommand { Worker = visible, Settings = Private(resource, supply) }) };
                    refusedTwin.Seat(0).Submit(Group(ids, Private(resource, supply))); refusedTwin.Seat(0).Submit(new ConfigureHaulLoadPolicyCommand { Worker = visible, Settings = Private(resource, supply) });
                    PermanentRemovalTests.Step(refused, refusedTwin);
                    foreach (var current in new[] { refused, refusedTwin })
                    {
                        long cursor = 0; var results = new List<CommandResult>(); current.Seat(0).ReadResults(ref cursor, results);
                        foreach (int sequence in sequences) c.That(results.Single(row => row.Sequence == sequence).Refusal == Codes.NotFound
                            && !results.Single(row => row.Sequence == sequence).Accepted, "executed private group/single refuse without success");
                        c.That(Same(oldWorker, Actual(current, worker), supply) && Same(oldVisible, Actual(current, visible), supply), "refused request commits neither visible nor hidden member");
                    }
                    c.That(new ReplayVerifier().Verify(factory, rejected.Finish(), limits).Success, "actual private grouped/single prefix replay");
                }
                var twin = factory.Load(game.Save()); Hidden(c, twin, worker, visible, supply, resource);
                game.Clock.Pause(); twin.Clock.Pause(); long stopped = game.Clock.StepIndex; PermanentRemovalTests.Step(game, twin);
                c.Equal(stopped, game.Clock.StepIndex, "paused private policies preserve the sole stopped clock"); game.Clock.Resume(); twin.Clock.Resume();
                foreach (var current in new[] { game, twin })
                {
                    var changed = PermanentRemovalTests.Submit(c, current, Group(ids, Known()));
                    c.That(changed.Arg0 == 2 && changed.Arg1 == 1, "selected2/changed1 comes from actual settings, not hidden previews");
                    c.Equal(Code.None, View(current).ReadHaulLoadPolicy(worker, out var settings), "known replacement becomes visible");
                    c.That(settings.Profile == "load.visible" && settings.MinimumLoad.IsEmpty && settings.MaximumWaitTicks == 0, "known explicit file-authored settings committed exactly");
                    PermanentRemovalTests.Submit(c, current, new SetInventorySightCommand { Hidden = false });
                }
                c.Equal(game.Hash(), twin.Hash(), "hidden-to-known replacement and reveal agree in saved twins"); twin = factory.Load(game.Save());
                foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new HaulResourcesCommand {
                    Worker = worker, Source = source, Destination = target, Resource = supply, Ceiling = Fix.FromInt(4), Priority = 100 });
                var timer = Stopwatch.StartNew(); long began = game.Clock.StepIndex;
                bool Done() => Stock(game, target, supply) == Fix.FromInt(4) && !PermanentRemovalTests.Jobs(game).Any(job => job.IsHaul && job.Worker == worker);
                while (!Done() && game.Clock.StepIndex - began < 128 && timer.Elapsed.TotalSeconds < 25)
                {
                    PermanentRemovalTests.Step(game, twin);
                    c.Equal(Fix.FromInt(20), Stock(game, source, supply) + Stock(game, target, supply) + Stock(game, worker, supply), "finite supply conserved through known load policy");
                }
                if (!Done()) throw new InvalidOperationException("load policy disclosure physical/128updates25seconds/" + game.Clock.StepIndex);
                c.That(Stock(game, source, supply) == Fix.FromInt(16) && Stock(game, worker, supply) == Fix.Zero, "normal replacement physically delivers four finite units once");
                c.That(oldWorker.Profile == "load.batch" && oldWorker.MinimumLoad.AmountOf(supply) == Fix.FromInt(4), "old authorized minimum/profile remains detached");
                c.Equal(game.Hash(), twin.Hash(), "saved replacement and physical completion converge"); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed native stock/claims/load state restores");
                c.Equal(isolated, idle.Hash(), "private metadata and policy state remain session isolated");
                c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "main replay captures every advancing update");
                var verified = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
                c.That(verified.Success && verified.ActualHash == game.Hash(), "complete private metadata/replacement/physical delivery replay");
                Console.WriteLine("load-disclosure/" + (resource ? "resource" : "profile") + "/full/" + recording.InitialStep + "->" + game.Clock.StepIndex
                    + "/inputs=" + recording.CommandCount + "/checkpoints=" + recording.CheckpointCount + "/hash=" + game.Hash().ToString("X16"));
            }
        }
    }
}
