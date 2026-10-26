using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.LineDefinitionContractTests;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Authored JSON and ordinary public player intent only; no host rules or map/stock/progress writes.
    internal static class LinePlacementPolicyTests
    {
        internal const string Case = "Line placement policies: physical underlay controls authored corridors and route admission";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(bool independent, bool half, LinePlacementPolicyProofModule module = null,
            Action<JsonValue> edit = null, Action<JsonValue> editLine = null)
        {
            var data = Json.Parse(FrameworkProofContent.Read("line-placement-policies.json"));
            if (!independent) {
                var row = data.Get("independent_line_placement_policies")[0]; row.Remove("fault"); row.Remove("read_after");
                data.Set("tile_line_placement_policies", data.Get("independent_line_placement_policies")); data.Remove("independent_line_placement_policies");
            }
            edit?.Invoke(data);
            var infra = Json.Parse(FrameworkProofContent.Read("authored-segment-infrastructure.json"));
            infra.Get("lines")[0].Set("placement_policy", JsonValue.Of("segment.underlay-policy")); editLine?.Invoke(infra);
            return AuthoredSegmentWorkflowTests.Factory(infrastructure: infra.ToString(), extraModule: module ?? new LinePlacementPolicyProofModule(),
                extraDocument: "line-placement-policies.json", extraJson: data.ToString(),
                worldAuthor: root => { if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30")); });
        }
        private static LayLineCommand Lay(string id)
        { var command = new LayLineCommand { Line = id }; for (int x = 16; x <= 18; x++) command.Tiles.Add(new TileCoord(x,10)); return command; }
        private static PlanIntent Intent(int y = 10)
        {
            var rows = JsonValue.NewArray(); for (int x = 16; x <= 18; x++) rows.Add(TilePlacement.Arguments("survey.tiles", new TileCoord(x,y)));
            var arguments = JsonValue.NewObject(); arguments.Set("space", JsonValue.Of("survey.tiles")); arguments.Set("path", rows);
            return new PlanIntent("line:segment.road-line", arguments, "corridor", 60, false);
        }
        private static PathSearchResult Route(SimGame g, List<TileCoord> path)
            => g.Seat(0).View.Feature<InfrastructureView>().SuggestLinePath("segment.road-line", new TileCoord(16,10), new TileCoord(18,10), path);
        private static void Physical(Check c, bool independent, bool half)
        {
            var f = Factory(independent, half); var g = New(f); var idle = New(f); ulong isolated = idle.Hash();
            c.Equal(Fix.FromInt(40), Steel(g), "authored physical stock only");
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000);
            using (var recorder = g.RecordReplay(limits))
            {
                ulong before = g.Hash(); c.Equal(Code.None, g.Seat(0).Preview(Lay("segment.road-line")), "ordinary command submits intent; activation may wait");
                var path = new List<TileCoord> { new TileCoord(0,0) }; c.Equal(PathSearchResult.Unreachable, Route(g, path), "router uses same actual predicate");
                c.Equal(0, path.Count, "refused route publishes no prefix"); c.Equal(before, g.Hash(), "placement observations pure");
                long road = PermanentRemovalTests.Submit(c, g, new SubmitPlanCommand { Intent = Intent() }).Arg0;
                var pending = Plans(g).Single(p => p.Id == road); c.That(pending.Order == 0 && pending.State == PlanState.Pending, "persistent intent owns no completed entity/work");
                var twin = f.Load(g.Save()); for (int i = 0; i < 4; i++) Step(g, twin);
                c.Equal(Fix.FromInt(40), Steel(g), "blocked intent consumes no materials"); c.Equal(0, Tile(g, "segment.road",16,10), "no instant construction");
                c.That(Plans(g).Single(p => p.Id == road).Blocker == new Code("line.requires-underlay"), "engine plan reports exact policy blocker");
                PermanentRemovalTests.Submit(c, g, Lay("segment.pipe-line")); twin = f.Load(g.Save());
                Reach(g, twin, () => Tile(g,"segment.pipe",18,10) == 1 && Plans(g).Single(p => p.Id == road).Order > 0, "physical underlay activates same pending plan");
                c.Equal(PathSearchResult.Found, Route(g, path), "earned underlay permits actual route"); c.Equal(3, path.Count, "engine returns complete route");
                int order = Plans(g).Single(p => p.Id == road).Order;
                Reach(g, twin, () => Nodes(g, order).Any(n => n.Id == "segment.0.prepare" && n.Progress.WholeTicks > 0), "real machine travel and authored work");
                var old = Nodes(g, order).Single(n => n.Id == "segment.0.prepare"); var prefix = old.Progress;
                c.That(!Work(g,1).TryCopyNodes(order, new List<ActivityNodeView>()) && Plans(g,1).Count == 0, "execution and plan intent private");
                twin = f.Load(g.Save()); long at = g.Clock.StepIndex; g.Clock.Pause(); twin.Clock.Pause(); Step(g,twin);
                c.Equal(at,g.Clock.StepIndex,"one game clock pauses original graph"); g.Clock.Resume(); twin.Clock.Resume();
                Reach(g,twin,()=>Plans(g).Single(p=>p.Id==road).State==PlanState.Done,"full physically earned corridor");
                for(int x=16;x<=18;x++){c.Equal(1,Tile(g,"segment.pipe",x,10),"earned underlay");c.Equal(1,Tile(g,"segment.road",x,10),"earned corridor");}
                c.Equal(Fix.FromInt(28),Steel(g),"six physical segments consume exactly twelve, held tools retained");
                c.Equal(prefix,old.Progress,"retained facts detached");c.That(pending.Order==0,"retained pending intent unchanged");
                long canceled = PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand { Intent=Intent(20) }).Arg0;
                PermanentRemovalTests.Submit(c,g,new CancelPlanCommand { Selection=new PlanCancellationSelection(false,new[]{canceled}) });
                c.Equal(Fix.FromInt(28),Steel(g),"blocked cancellation spends or refunds nothing");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"completed/canceled original owners durable");c.Equal(isolated,idle.Hash(),"interleaved session isolated");
                c.That(new ReplayVerifier().Verify(f,recorder.Finish(),limits).Success,"ordinary plan/material/travel/clock input replay");
            }
        }
        private static void Failure(Check c,string mode)
        {
            var f=Factory(true,false,edit:data=>data.Get("independent_line_placement_policies")[0].Set("fault",JsonValue.Of(mode)));
            var g=New(f);var idle=New(Factory(false,false));ulong isolated=idle.Hash();var cached=g.Seat(0).View.Feature<InfrastructureView>();var foreign=g.Seat(1).View.Feature<InfrastructureView>();
            while(g.Clock.StepIndex<3)Step(g);var path=new List<TileCoord>{new TileCoord(0,0)};Exception error=null;
            try{cached.SuggestLinePath("segment.road-line",new TileCoord(16,10),new TileCoord(18,10),path);}catch(Exception actual){error=actual;}
            c.That(error is InvalidOperationException && g.Fault!=null && path.Count==0,"actual policy fault withholds path "+mode);
            if(error==null||g.Fault==null)throw new InvalidOperationException("missing policy fault "+mode);
            if(mode=="throw"||mode=="clock-throw")c.Equal("proof.line-placement-first-error",error.Message,"first callback preserved");
            var first=g.Fault;
            foreach(var view in new[]{cached,foreign})foreach(string id in new[]{"missing","segment.road-line"})
            {path.Add(new TileCoord(0,0));c.Throws<InvalidOperationException>(()=>view.SuggestLinePath(id,new TileCoord(16,10),new TileCoord(18,10),path),"faulted private/missing route rejects");c.Equal(0,path.Count,"no seeded stale route");}
            c.Throws<InvalidOperationException>(()=>g.Save(),"faulted policy cannot save");c.Throws<InvalidOperationException>(()=>g.Hash(),"faulted policy cannot hash healthy state");
            c.Equal(0,g.Update(),"terminal Update");c.That(ReferenceEquals(first,g.Fault),"first fault stable");c.Equal(isolated,idle.Hash(),"policy fault session isolated");Step(idle);
        }
        private static void Invalid(Check c)
        {
            foreach(string fault in new[]{"unknown-condition","any-empty","unknown-space","space-null","missing-policy","unknown-policy"})
            {
                var module=new LinePlacementPolicyProofModule();
                c.Throws<ContentException>(()=>New(Factory(true,false,module,edit:data=>{
                    var row=data.Get("independent_line_placement_policies")[0].Clone();row.Set("id",JsonValue.Of("segment.unused-policy"));data.Get("independent_line_placement_policies").Add(row);
                    if(fault=="unknown-condition")row.Get("footprint").Get("conditions").Add(JsonValue.Of("missing.condition"));
                    if(fault=="any-empty"){row.Get("footprint").Set("match",JsonValue.Of("any"));row.Get("footprint").Set("conditions",JsonValue.NewArray());}
                    if(fault=="unknown-space")row.Set("space",JsonValue.Of("missing.space"));if(fault=="space-null")row.Set("fault",JsonValue.Of("space-null"));
                },editLine:infra=>{if(fault=="missing-policy")infra.Get("lines")[0].Remove("placement_policy");if(fault=="unknown-policy")infra.Get("lines")[0].Set("placement_policy",JsonValue.Of("missing.policy"));})),"malformed actual policy refused "+fault);
                c.Equal(0,module.Initialize.Initializations,"refusal before Initialize "+fault);
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current engine identity");c.Equal(154,EngineInfo.SaveFormat,"no duplicate state/schema");
            foreach(bool independent in new[]{false,true})foreach(bool half in new[]{false,true})Physical(c,independent,half);
            Invalid(c);foreach(string mode in new[]{"throw","clock","clock-throw","map"})Failure(c,mode);
        }
    }
}
