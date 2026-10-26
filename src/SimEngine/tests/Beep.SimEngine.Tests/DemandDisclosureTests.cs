using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Authored JSON and normal players only. Physical requests/materials/time remain engine-owned.
    internal static class DemandDisclosureTests
    {
        internal const string Case = "Demand disclosure: hidden workflow owners withhold combined logistics facts while physical supply continues";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Process = new Code("contract.process");
        private static GameFactory Factory(bool half, bool direct)
            => RecipeDisclosureTests.Factory(direct, "result", half, knowledgeExtraDocument: "logistics-disclosure.json",
                knowledgeEdit: root => root.Get("knowledge_visibility_policies")[0].Get("rules").Add(JsonValue.Of("demand.hide-work")));
        private static LogisticsView View(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<LogisticsView>();
        private static List<LogisticsDemandView> Demands(SimGame g, bool observer = false)
        { var rows = new List<LogisticsDemandView>(); (observer ? g.Observer.Feature<LogisticsView>() : View(g)).CopyDemands(rows); return rows; }
        private static Fix Stock(SimGame g, EntityId target, ResourceId resource)
        { var code = g.Observer.Feature<LogisticsView>().ReadDiagnostics(target, resource, out var value); if (!code.IsNone) throw new InvalidOperationException("actual demand stock/" + code); return value.Stock; }
        private static bool Known(SimGame g) => g.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "recipe.discovered", out _);
        private static bool WorkDone(SimGame g, EntityId target)
        {
            return g.Observer.Feature<WorkView>().TryReadCompanyActivityCompletion(new PlayerId(0), "activity:demand.hidden-work", target, out _);
        }
        private static TransformationSnapshot ActualProcess(SimGame g, EntityId target)
        { if (!g.Observer.Feature<TransformationManagementView>().TryGetTransformation(target, Process, out var value)) throw new InvalidOperationException("actual process absent"); return value; }
        private static void Reach(SimGame g, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 512 && !done(); i++) TransformationContentContractTests.Step(g, twin);
            if (!done())
            {
                var orders = new List<OrderView>(); g.Observer.Feature<WorkView>().CopyOrders(orders);
                var jobs = new List<WorkJobView>(); g.Observer.Feature<WorkView>().CopyJobs(jobs);
                var offers = new List<LogisticsDispatchOffer>(); g.Observer.Feature<LogisticsView>().CopyQueue(offers);
                var factory = TransformationContentContractTests.Entity(g, "contract.factory"); var process = ActualProcess(g, factory);
                if (!g.Seat(0).View.TryFindResource("supply", out var supply)) throw new InvalidOperationException(stage + "/supply metadata unavailable");
                throw new InvalidOperationException(stage + "/clock=" + g.Clock.StepIndex + "/known=" + Known(g) +
                    "/demands=" + string.Join(";", Demands(g, true).Select(x => x.Source.Scope + "/" + x.Quantity + "/" + x.Check.Status)) +
                    "/orders=" + string.Join(";", orders.Select(x => x.Id + "/" + x.Plan + "/" + x.State + "/" + x.Blocker + "/" + x.NodesDone + "/" + x.NodeCount)) +
                    "/jobs=" + string.Join(";", jobs.Select(x => x.Id + "/" + x.State + "/" + x.Blocker)) +
                    "/offers=" + string.Join(";", offers.Select(x => x.Destination + "/" + x.Requested + "/" + x.Haul.Status + "/" + x.Haul.Blocker)) +
                    "/process=" + process.Phase + "/" + process.Blocker + "/wip=" + process.Wip.Stock + "/stock=" +
                    Stock(g, factory, supply));
            }
        }
        private static void Hidden(Check c, SimGame g, EntityId target, ResourceId resource, string scope)
        {
            ulong pure = g.Hash(); c.That(g.Seat(0).View.TryGetEntity(target, out _) && g.Seat(0).View.TryFindResource("supply", out _), "destination and resource remain genuinely visible");
            var actual = Demands(g, true).Where(x => x.Destination == target).ToArray();
            c.That(actual.Any(x => x.Source.Scope == scope && x.Quantity.IsPositive) && actual.Any(x => x.Source.Scope == LogisticsService.StockDemandScope), "actual hidden source and visible stock co-demand coexist");
            var projected = Demands(g).Where(x => x.Destination == target).ToArray();
            c.That(projected.All(x => x.Source.Scope != scope) && projected.Any(x => x.Source.Scope == LogisticsService.StockDemandScope), "owner hides only private individual source, not unrelated visible intent");
            c.Equal(Codes.NotFound, View(g).ReadStockTarget(target, resource, out _), "combined target unavailable, never partial visible sum");
            c.Equal(Codes.NotFound, View(g).ReadDiagnostics(target, resource, out var diagnostics), "combined diagnostic unavailable"); c.That(diagnostics == null, "no invented reduced diagnostic");
            var offers = new List<LogisticsDispatchOffer>(); View(g).CopyQueue(offers);
            c.That(offers.All(x => x.Destination != target), "whole aggregated offer withheld even visible stock source wins ordering");
            c.Equal(Code.None, View(g).ReadStockPolicy(target, resource, out var band), "unrelated actual player stock policy remains visible"); c.Equal(Fix.FromInt(2), band.Desired, "stock policy not replaced with hidden sum");
            View(g, 1).CopyDemands(new List<LogisticsDemandView>());
            c.Equal(Codes.NotFound, View(g, 1).ReadStockTarget(target, resource, out _), "foreign aggregate remains private");
            c.Equal(pure, g.Hash(), "owner disclosure and combined previews pure");
        }
        private static void Healthy(Check c, bool half, string kind)
        {
            bool work = kind == "work"; var f = Factory(half, kind == "external"); var g = ConstructionPlanTests.NewPoints(f); var idle = ConstructionPlanTests.NewPoints(f); ulong isolated = idle.Hash();
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "physical supply/work/manufacturing needs no business module");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current source disclosure identity");
            var source = TransformationContentContractTests.Entity(g, "source"); var machine = TransformationContentContractTests.Entity(g, "machine");
            var target = TransformationContentContractTests.Entity(g, work ? "destination" : "contract.factory");
            c.That(g.Seat(0).View.TryFindResource("supply", out var resource), "actual supply identity");
            var limits = new ReplayLimits(6 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            PermanentRemovalTests.Submit(c, g, new ConfigureWorkPolicyCommand { Worker = machine,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
            PermanentRemovalTests.Submit(c, g, new PostOrderCommand { Plan = "recipe.discover", Target = source });
            Reach(g, null, () => Known(g), "physical initial discovery");
            PermanentRemovalTests.Submit(c, g, new SetWorkerHeldCommand { Worker = machine, Held = true });
            PermanentRemovalTests.Submit(c, g, new ConfigureStockPolicyCommand { Store = target, Resource = resource,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(2), Fix.FromInt(2), Fix.FromInt(10), 100, LogisticsReserveAccess.Protected)) });
            if (work) PermanentRemovalTests.Submit(c, g, new PostOrderCommand { Plan = "demand.hidden-work", Target = target, Priority = 1 });
            else PermanentRemovalTests.Submit(c, g, new EnqueueTransformationCommand { Transformer = target, Capability = Process, Transformation = "contract.make" });
            string scope = work ? WorkModule.ModuleId : TransformationModule.ModuleId;
            Reach(g, null, () => Demands(g, true).Any(x => x.Destination == target && x.Source.Scope == scope), "actual workflow supply request");
            var retained = Demands(g).Single(x => x.Destination == target && x.Source.Scope == scope);
            c.Equal(Code.None, View(g).ReadStockTarget(target, resource, out var combined), "known whole combined target");
            c.Equal(Fix.FromInt(work ? 8 : 6), combined, "actual workflow plus stock demand total");
            PermanentRemovalTests.Submit(c, g, new ForgetKnowledgeCommand { Policy = "recipe.forget" });
            c.That(!Known(g), "normal forgetting revokes metadata while actual unmet source remains");
            Command newRequest = work ? new PostOrderCommand { Plan = "demand.hidden-work", Target = target }
                : new EnqueueTransformationCommand { Transformer = target, Capability = Process, Transformation = "contract.make" };
            c.Equal(Codes.UnknownDef, g.Seat(0).Preview(newRequest), "accepted physical operation survives revocation but new player intent remains restricted");
            Hidden(c, g, target, resource, scope);
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "private requests and stock co-demand saved without copied state");
            TransformationContentContractTests.Step(g, twin); Hidden(c, g, target, resource, scope);
            c.Equal(Fix.FromInt(20), Stock(g, source, resource), "held machine prevented artificial or instant supply");
            PermanentRemovalTests.Submit(c, g, new SetWorkerHeldCommand { Worker = machine, Held = false }); twin = f.Load(g.Save());
            if (work)
            {
                Reach(g, twin, () => WorkDone(g, target), "hidden workflow physically supplied and completed");
                c.That(g.Observer.Feature<WorkView>().TryReadCompanyActivityCompletion(new PlayerId(0), "activity:demand.hidden-work", target, out var completion) &&
                    completion.Order > 0 && completion.Company == new PlayerId(0) && completion.Target == target && completion.CompletedTick > completion.CreatedTick,
                    "observer obtains actual completed identity and earned positive duration, not absence inference");
                c.That(!g.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:demand.hidden-work", target, out _), "earned hidden activity completion still private");
                c.That(!g.Seat(0).View.Feature<WorkView>().TryReadCompanyActivityCompletion(new PlayerId(0), "activity:demand.hidden-work", target, out _) &&
                    !g.Seat(1).View.Feature<WorkView>().TryReadCompanyActivityCompletion(new PlayerId(0), "activity:demand.hidden-work", target, out _), "company-qualified completion cannot bypass hidden or foreign player authority");
                Reach(g, twin, () => Stock(g, target, resource) == Fix.FromInt(2), "original stock policy physically replenishes after consumed work");
                c.Equal(Fix.FromInt(12), Stock(g, source, resource), "source supplied six consumed work materials and two retained stock units once");
                c.Equal(Fix.FromInt(2), Stock(g, target, resource), "actual stock band survives material consumption");
            }
            else
            {
                Reach(g, twin, () => ActualProcess(g, target).QueueCount == 0, "hidden recipe physically supplied and manufactured");
                c.Equal(1, g.Snapshot.Entities.Count(x => x.Def == "contract.result"), "one actual native or independent manufactured result");
                c.Equal(Fix.FromInt(14), Stock(g, source, resource), "source supplied three consumed recipe units plus reusable tool and retained stock once");
                c.Equal(Fix.FromInt(3), Stock(g, target, resource), "actual reusable tool and stock share sole buffer");
            }
            c.Equal(Fix.Zero, Stock(g, machine, resource), "timed delivery leaves no duplicate cargo");
            c.That(retained.Quantity == Fix.FromInt(work ? 6 : 4) && retained.Check.Status == LogisticsDemandStatus.Active, "retained source facts remain old immutable observations");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "completed hidden state durable");
            PermanentRemovalTests.Submit(c, g, new PostOrderCommand { Plan = "recipe.discover", Target = source });
            Reach(g, null, () => Known(g), "physical rediscovery");
            if (work) c.That(g.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:demand.hidden-work", target, out _), "rediscovery restores same earned work completion");
            else c.That(g.Seat(0).View.Feature<TransformationManagementView>().TransformationsOf(target, Process).Contains("contract.make"), "rediscovery restores same recipe metadata");
            c.Equal(Code.None, View(g).ReadStockTarget(target, resource, out _), "completed rediscovered whole target available");
            c.Equal(isolated, idle.Hash(), "separate undiscovered session isolated");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "discovery requests forgetting hidden physical supply and rediscovery replay");
        }
        private static void Run(Check c) { foreach (bool half in new[] { false, true }) foreach (string kind in new[] { "work", "native", "external" }) Healthy(c, half, kind); }
    }
}

