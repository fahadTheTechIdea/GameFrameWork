using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Authored inputs, ordinary commands and public facts only; the engine owns all execution.
    internal static class ResourceFlowContentTests
    {
        internal const string Case = "Resource-flow content: direct and native JSON policies share physical private saved journal ownership";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(ResourceFlowContentModule module, bool native = false, Action<JsonValue> edit = null, string nativeFlows = null)
        {
            var addon = Json.Parse(FrameworkProofContent.Read("resource-flow-contracts.json"));
            var flows = Json.Parse("{\"resource_flow_journals\":[]}");
            if (native) { flows.Set("resource_flow_journals", addon.Get("direct_flow_journals").Clone()); addon.Remove("direct_flow_journals"); }
            edit?.Invoke(addon);
            var root = Json.Parse(PointGame.Definitions);
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module.Id));
            return PointGame.Factory(root.ToString(), additionalModule: module,
                moduleDeclaration: "{\"id\":\"proof.resource-flow-content\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\"}],\"documents\":[\"resource-flow-contracts.json\"]}",
                moduleDocument: addon.ToString(), flowDefinitions: nativeFlows ?? flows.ToString());
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static EntityId Actor(SimGame game, string id) => game.Seat(0).Snapshot.Entities.Single(x => x.Def == id).Id;
        private static Fix Stock(SimGame game, string id)
        {
            var rows = new List<Facet>(); game.Seat(0).View.Inspect(Actor(game, id), rows);
            return rows.Where(x => x.Key == Storage.FacetStock && x.Subject == "supply").Aggregate(Fix.Zero, (sum, x) => sum + x.Value);
        }
        private static ResourceFlowView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<ResourceFlowView>();
        private static bool CarrierReleased(SimGame game)
        {
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
            return !jobs.Any(x => x.Worker == Actor(game, "machine"));
        }
        private static void Step(Check c, SimGame game, SimGame twin = null)
        {
            c.Equal(1, game.Update(), "one ordinary authored minute update");
            if (game.Fault != null) throw new InvalidOperationException("flow physical fault " + game.Fault);
            c.Equal(Fix.FromInt(20), Stock(game, "source") + Stock(game, "destination") + Stock(game, "machine"), "actual material conserved in source/cargo/destination each update");
            if (twin != null) { twin.Update(); c.Equal(game.Hash(), twin.Hash(), "saved journal and physical execution twin"); }
        }
        private static void Reach(Check c, SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 256 && !ready(); i++) Step(c, game, twin);
            if (!ready()) {
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                throw new InvalidOperationException(stage + " at=" + game.Clock.StepIndex + " source=" + Stock(game,"source") + " cargo=" + Stock(game,"machine") + " destination=" + Stock(game,"destination")
                    + " jobs=" + string.Join(";", jobs.Select(x => x.Id + "/" + x.State + "/" + x.Blocker)));
            }
        }
        private static void Refuse(Check c, Action<JsonValue> edit, string label, string nativeFlows = null, bool native = false)
        {
            var module = new ResourceFlowContentModule();
            c.Throws<ContentException>(() => New(Factory(module, native, edit, nativeFlows)), label);
            c.Equal(0, module.Initialize.Initializations, "invalid policy refuses before Initialize/stock grants: " + label);
        }
        private static void Run(Check c)
        {
            foreach (bool native in new[] { false, true })
            {
                foreach (string quantity in new[] { "0", "-1" })
                    Refuse(c, native ? (Action<JsonValue>)null : doc => doc.Get("direct_flow_journals")[0].Set("retained_records_per_company", JsonValue.Number(quantity)),
                        "strict native/direct positive retention " + native + "/" + quantity,
                        native ? "{\"resource_flow_journals\":[{\"id\":\"journal.actual\",\"retained_records_per_company\":" + quantity + "}]}" : null, native);
            }
            Refuse(c, doc => doc.Get("direct_flow_journals")[0].Remove("retained_records_per_company"), "no missing retention default");
            Refuse(c, doc => doc.Get("direct_flow_journals")[0].Set("unknown", JsonValue.Of("value")), "unknown metadata refuses");
            Refuse(c, doc => doc.Remove("direct_flow_journals"), "missing sole policy refuses");
            Refuse(c, null, "mixed native/direct policies cannot silently select native",
                "{\"resource_flow_journals\":[{\"id\":\"journal.native\",\"retained_records_per_company\":2}]}");
            Refuse(c, doc => { var row=doc.Get("direct_flow_journals")[0].Clone(); row.Set("id",JsonValue.Of("journal.second")); doc.Get("direct_flow_journals").Add(row); }, "duplicate direct policy count refuses");
            foreach (string mode in new[] { "zero", "negative", "identity", "owner" })
                Refuse(c, doc => { doc.Remove("direct_flow_journals"); doc.Set("broken_flow_journals",Json.Parse("[{\"id\":\"journal.broken\",\"retained_records_per_company\":2,\"mode\":\"" + mode + "\"}]")); }, "actual direct contract admission " + mode);
            var throwing = new ResourceFlowContentModule();
            c.Throws<InvalidOperationException>(() => New(Factory(throwing, edit: doc => { doc.Remove("direct_flow_journals"); doc.Set("broken_flow_journals",Json.Parse("[{\"id\":\"journal.broken\",\"retained_records_per_company\":2,\"mode\":\"throw\"}]")); })), "first metadata callback error remains visible");
            c.Equal(0, throwing.Initialize.Initializations, "throwing policy runs no Initialize");
            Physical(c, false); Physical(c, true);
        }
        private static void Physical(Check c, bool native)
        {
            var module = new ResourceFlowContentModule(); var factory = Factory(module, native); var game = New(factory);
            c.That(game.Initialized && module.Initialize.Initializations == 1 && game.Clock.StepIndex == 0, "actual native/direct policy composes before normal initialized session " + native);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current behavior identity");
            var company = new PlayerId(0);
            var retained = View(game).Read(company, 0); var cumulative = View(game).ReadSummary(company);
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var recorder = game.RecordReplay(limits);
            if (!game.Seat(0).View.TryFindResource("supply",out var resource)) throw new InvalidOperationException("no actual supply");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "initial journal restore does not grant stock again");
            for (int trip = 1; trip <= 3; trip++)
            {
                Reach(c,game,null,() => CarrierReleased(game),"carrier released");
                var command = new HaulResourcesCommand { Source=Actor(game,"source"), Destination=Actor(game,"destination"), Worker=Actor(game,"machine"), Resource=resource, Ceiling=Fix.FromInt(2), Priority=70 };
                ulong before=game.Hash(); c.Equal(Code.None,game.Seat(0).Preview(command),"ordinary physical haul admits"); c.Equal(before,game.Hash(),"preview changes neither journal nor time");
                game.Seat(0).Submit(command); var twin=factory.Load(game.Save());
                Step(c,game,twin); c.That(Stock(game,"destination") < Fix.FromInt(trip*2),"pending acquisition is not instant delivery");
                Reach(c,game,twin,() => Stock(game,"machine").IsPositive,"real loaded cargo");
                twin=factory.Load(game.Save()); ulong paused=game.Hash(); game.Clock.Pause(); c.Equal(0,game.Update(),"pause earns no transfer fact"); c.Equal(paused,game.Hash(),"paused stock/work/journal unchanged"); game.Clock.Resume();
                Reach(c,game,twin,() => Stock(game,"destination")==Fix.FromInt(trip*2),"real physical unload");
                c.Equal(game.Hash(),factory.Load(game.Save()).Hash(),"delivered history checkpoint remains identical");
            }
            var page=View(game).Read(company,0); var summary=View(game).ReadSummary(company);
            c.That(page.Status==ResourceFlowReadStatus.Expired && page.Entries.Count==2 && page.Oldest>1,"both JSON registrations use exact authored retention");
            c.That(summary.Status==ResourceFlowReadStatus.Ready && summary.Latest==page.Latest && summary.Latest>page.Entries.Count && summary.EvictedThroughTick>=0,"sole journal retains cumulative chronology beyond window eviction");
            BigInteger moved=0; foreach(var row in summary.Aggregates) if(row.Resource==resource && row.Cause.IsNone) moved+=row.AmountRaw;
            c.Equal((BigInteger)Fix.FromInt(12).Raw,moved,"cumulative two physical transfer legs for three actual two-unit deliveries");
            c.Equal(ResourceFlowReadStatus.Expired,View(game).ReadWindow(company,0,game.Clock.StepIndex).Status,"expired window cannot invent old facts");
            var denied=View(game,1).Read(company,0); c.That(denied.Status==ResourceFlowReadStatus.NotOwned && denied.Entries.Count==0 && denied.Latest==0,"private journal leaks no facts or watermarks");
            c.Equal(ResourceFlowReadStatus.NotOwned,View(game,1).ReadSummary(company).Status,"cumulative history is company private too");
            c.Throws<NotSupportedException>(() => ((IList<FeedEntry<ResourceFlowFact>>)page.Entries).Clear(),"published history collection is immutable");
            c.That(retained.Latest==cumulative.Latest && retained.Latest<page.Latest,"initial retained publication never grows with live journal");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"physical journal retention/saves/hauls replay");
            var other=New(factory); other.Update(); c.That(Stock(other,"destination")==Fix.Zero && View(other).ReadSummary(company).Latest<summary.Latest,"interleaved session owns distinct history and stock");
        }
    }
}

