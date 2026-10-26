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
    // Ordinary authored player intent, public views and the single Game.Update only.
    internal static class PortableLinePathTests
    {
        internal const string Case = "Portable corridor paths: provider JSON identity and durable physical cursor";
        internal static void Register() => TestRunner.Add(Case,Run);
        private static GameFactory Factory(bool direct,bool half)
        {
            var infra=Json.Parse(FrameworkProofContent.Read("authored-segment-infrastructure.json"));
            if(direct) {
                infra.Get("lines")[0].Set("work_marker",JsonValue.Of("cursor.road"));
                infra.Get("lines")[1].Set("work_marker",JsonValue.Of("cursor.pipe"));
            }
            return AuthoredSegmentWorkflowTests.Factory(infrastructure:infra.ToString(),
                extraModule:direct?new AuthoredLineCursorModule():null,
                extraDocument:direct?"authored-line-cursors.json":null,
                extraJson:direct?FrameworkProofContent.Read("authored-line-cursors.json"):null,
                worldAuthor:root=>{if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));});
        }
        private static PlanIntent Intent()
        {
            var rows=JsonValue.NewArray();
            for(int x=16;x<=18;x++)rows.Add(TilePlacement.Arguments("survey.tiles",new TileCoord(x,10)));
            var arguments=JsonValue.NewObject();arguments.Set("space",JsonValue.Of("survey.tiles"));arguments.Set("path",rows);
            return new PlanIntent("line:segment.road-line",arguments,"corridor",60,true);
        }
        private static PlanView Plan(SimGame game,long id)=>Plans(game).Single(p=>p.Id==id);
        private static void Physical(Check c,bool direct,bool half)
        {
            var f=Factory(direct,half);var g=New(f);var idle=New(f);ulong isolated=idle.Hash();
            var limits=new ReplayLimits(4*1024*1024,4000);
            using(var recorder=g.RecordReplay(limits)) {
                PlanIntent intent=Intent();ulong hash=g.Hash();
                c.Equal(Code.None,g.Seat(0).Preview(new SubmitPlanCommand{Intent=intent}),"provider path preview before cursor allocation");
                foreach(Action<JsonValue> edit in new Action<JsonValue>[] {
                    j=>j.Remove("path"),j=>j.Set("tiles",j.Get("path")),j=>j.Set("path",JsonValue.NewArray()),
                    j=>j.Set("space",JsonValue.Of("other")),j=>j.Get("path")[0].Set("target",JsonValue.Number("1")),
                    j=>j.Get("path")[0].Remove("x")
                }) {
                    var bad=intent.Arguments.Clone();edit(bad);
                    c.Equal(Codes.InvalidArgument,g.Seat(0).Preview(new SubmitPlanCommand{Intent=new PlanIntent("line:segment.road-line",bad,null,60,true)}),"strict current path schema refuses");
                }
                c.That(g.Fault==null && g.Hash()==hash,"malformed intent is a pure refusal, not a terminal provider fault");
                long id=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=intent}).Arg0;
                var pending=Plan(g,id);
                c.That(pending.Order==0 && pending.State==PlanState.Pending,"paused path owns no marker or stock");
                c.Equal(Fix.FromInt(40),Steel(g),"all stock retained before activation");
                c.Equal(0,Tile(g,"segment.road",16,10),"no instant construction");
                c.Equal(PlanCodes.Duplicate,g.Seat(0).Preview(new SubmitPlanCommand{Intent=intent}),"path identity rejects duplicate ordered intent");
                c.Throws<InvalidOperationException>(()=>pending.Arguments.Get("path")[0].Set("x",JsonValue.Number("1")),"retained path facts frozen");
                c.That(Work(g).CapturePlanTemplate("corridor",TilePlacement.Arguments("survey.tiles",new TileCoord(14,10)),out PlanTemplate template).Ready,"same engine captures relative path intent");
                c.Equal("2",template.Entries[0].Arguments.Get("path")[0].Get("dx").Text,"provider owns template geometry");
                var placed=PermanentRemovalTests.Submit(c,g,new PlacePlanTemplateCommand{
                    Selection=PlanTemplateSelection.Snapshot(template),Anchor=TilePlacement.Arguments("survey.tiles",new TileCoord(14,20)),Group="translated"});
                long translated=Plans(g).Single(p=>p.Group=="translated").Id;
                c.That(placed.Accepted && Plan(g,translated).Order==0,"translated paused path remains intent");
                PermanentRemovalTests.Submit(c,g,new CancelPlanCommand{Selection=new PlanCancellationSelection(false,new[]{translated})});
                var twin=f.Load(g.Save());Step(g,twin);
                PermanentRemovalTests.Submit(c,g,new ConfigurePlanCommand{Plan=id,Group="corridor",Priority=60,Paused=false});
                twin=f.Load(g.Save());int order=Plan(g,id).Order;
                Reach(g,twin,()=>Nodes(g,order).Any(n=>n.Id=="segment.0.prepare"&&n.Progress.WholeTicks>0),"physical first segment travel and work");
                var retained=Nodes(g,order).Single(n=>n.Id=="segment.0.prepare");var progress=retained.Progress;
                c.That(!Work(g,1).TryCopyNodes(order,new List<ActivityNodeView>()) && Plans(g,1).Count==0,"private plan/cursor/node views");
                c.That(g.Seat(0).Snapshot.Entities.Count(e=>e.Def==(direct?"cursor.road":"sim.line-work.segment.road-line"))==1,"one original physical cursor");
                twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"partial provider JSON cursor roundtrip");
                long at=g.Clock.StepIndex;g.Clock.Pause();twin.Clock.Pause();Step(g,twin);c.Equal(at,g.Clock.StepIndex,"sole clock stops corridor");g.Clock.Resume();twin.Clock.Resume();
                Reach(g,twin,()=>Nodes(g,order).Any(n=>n.Id=="segment.0.install"&&n.State==NodeState.Done),"first earned segment");
                twin=f.Load(g.Save());
                c.That(Tile(g,"segment.road",16,10)==1 && Tile(g,"segment.road",18,10)==0,"completed prefix, not future placement");
                Reach(g,twin,()=>Plan(g,id).State==PlanState.Done,"full physical corridor path");
                for(int x=16;x<=18;x++)c.Equal(1,Tile(g,"segment.road",x,10),"earned canonical segment");
                c.Equal(Fix.FromInt(34),Steel(g),"three segments consume exactly six physical units");
                c.Equal(progress,retained.Progress,"detached retained node progress");
                c.That(pending.Order==0 && pending.Arguments.Get("path")[0].Get("x").Text=="16","detached pending path unaffected");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"completed/canceled path persistence");
                c.Equal(isolated,idle.Hash(),"interleaved session isolated");
                c.That(new ReplayVerifier().Verify(f,recorder.Finish(),limits).Success,"ordinary player path inputs replay");
            }
        }
        private static void PointPaths(Check c)
        {
            var root=Json.Parse(PointGame.Definitions);root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.portable-line-paths"));
            var f=PointGame.Factory(definitions:root.ToString(),additionalModule:new PortableLinePathsProofModule(),
                moduleDeclaration:"{\"id\":\"proof.portable-line-paths\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"points\",\"version\":\"1.0.0\"}],\"documents\":[\"portable-line-paths.json\"]}",
                moduleDocument:FrameworkProofContent.Read("portable-line-paths.json"));
            var g=f.NewGame(new NewGameRequest{Scenario="points"});var view=g.Seat(0).View.Feature<PointCorridorPathView>();ulong hash=g.Hash();
            var facts=view.Inspect("path.points");var inverse=view.Inspect("path.points",facts.Path);
            c.Equal(3,facts.Count,"non-tile path keeps authored segment count");
            c.That(facts.Path[0].Get("x")==null && facts.Path[0].Get("position").Text=="1.5","shared path never substitutes tile coordinates");
            c.Equal(facts.Identity,inverse.Identity,"actual point provider inverse and stable path identity");
            c.That(facts.Identity!=view.Inspect("path.reversed").Identity,"ordered path identity distinguishes reversed intent");
            var equivalent=facts.Path.Clone();equivalent[0].Set("position",JsonValue.Number("1.500"));
            c.Equal(facts.Identity,view.Inspect("path.points",equivalent).Identity,"provider identity, not raw input formatting");
            c.Throws<InvalidOperationException>(()=>facts.Path[0].Set("position",JsonValue.Number("2")),"non-tile returned path deeply frozen");
            foreach(Action<JsonValue> edit in new Action<JsonValue>[] {
                j=>j[0].Set("space",JsonValue.Of("other")),j=>j[0].Set("x",JsonValue.Number("1")),j=>j[0].Remove("position")
            }){var bad=facts.Path.Clone();edit(bad);c.Throws<ContentException>(()=>view.Inspect("path.points",bad),"point provider rejects incompatible input without tile cast");}
            c.That(g.Fault==null && hash==g.Hash(),"point observations and malformed inputs pure");
            var twin=f.Load(g.Save());Step(g,twin);
            c.Equal(facts.Identity,twin.Seat(0).View.Feature<PointCorridorPathView>().Inspect("path.points").Identity,"restored selected provider same canonical identity");
            c.Equal("1.5",facts.Path[0].Get("position").Text,"retained point facts detached");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current incompatible cursor build");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"current original save envelope");
            foreach(bool direct in new[]{false,true})foreach(bool half in new[]{false,true})Physical(c,direct,half);
            PointPaths(c);
        }
    }
}
