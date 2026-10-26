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
    // Authored content and ordinary player commands/views/Update only; never simulation in the driver.
    internal static class SpatialCorridorExecutionTests
    {
        internal const string Case = "Spatial corridor execution: one physical owner across tile and point geometry";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Native(bool half,InfrastructureServiceContractsProofModule probe)
            =>AuthoredSegmentWorkflowTests.Factory(extraModule:probe,extraDocument:"corridor-owner-probe.json",
                extraJson:FrameworkProofContent.Read("corridor-owner-probe.json"),
                worldAuthor:root=>{if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));});
        internal static GameFactory Points(bool half,PointLineGeometryProofModule probe,Action<JsonValue> edit=null,
            IReadOnlyDictionary<string,string> extraDocuments=null,Action<JsonValue> editWorld=null,
            IReadOnlyList<GameModule> extraModules=null,string extraDeclarations=null,IReadOnlyDictionary<string,string> moduleDocuments=null,string retryProfiles=null)
        {
            var root=Json.Parse(PointGame.Definitions);root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(InfrastructureModule.ModuleId));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(probe.Id));root.Get("scenarios")[0].Set("seats",JsonValue.Number("2"));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ConstructionModule.ModuleId));
            root.Get("scenarios")[0].Get("settings").Set(ConstructionModule.ModuleId,Json.Parse("{\"recovery\":\"corridor.recovery\"}"));
            if(half)root.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));
            var data=Json.Parse(FrameworkProofContent.Read("point-corridors.json"));edit?.Invoke(data);
            editWorld?.Invoke(root);
            return PointGame.Factory(definitions:root.ToString(),additionalModule:probe,
                moduleDeclaration:"{\"id\":\"proof.point-lines\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"},{\"id\":\"points\",\"version\":\"1.0.0\"}],\"documents\":[\"point-corridors.json\"]}",
                moduleDocument:data.ToString(),additionalDocuments:extraDocuments,
                extraModules:extraModules,extraDeclarations:extraDeclarations,extraDocuments:moduleDocuments,retryProfiles:retryProfiles);
        }
        internal static PlanIntent Intent(bool point,bool paused,int offset=0)
        {
            string space=point?"field.points":"survey.tiles";var rows=JsonValue.NewArray();
            for(int i=0;i<3;i++) {
                if(point){var row=JsonValue.NewObject();row.Set("space",JsonValue.Of(space));row.Set("position",JsonValue.Number((3+i*2+offset).ToString(System.Globalization.CultureInfo.InvariantCulture)));rows.Add(row);}
                else rows.Add(TilePlacement.Arguments(space,new TileCoord(16+i,10+offset)));
            }
            var args=JsonValue.NewObject();args.Set("space",JsonValue.Of(space));args.Set("path",rows);
            return new PlanIntent(point?"line:corridor.point-line":"line:segment.road-line",args,"physical",60,paused);
        }
        private static PlanView Plan(SimGame g,long id)=>Plans(g).Single(p=>p.Id==id);
        internal static Fix Total(SimGame g,bool point)
        {
            if(!point)return Steel(g);
            Fix sum=Fix.Zero;
            foreach(var e in g.Seat(0).Snapshot.Entities){var facts=new List<Facet>();g.Seat(0).View.Inspect(e.Id,facts);foreach(var fact in facts)if(fact.Key==Storage.FacetStock&&fact.Subject=="supply")sum+=fact.Value;}
            return sum;
        }
        internal static Fix Segment(SimGame g,bool point,int index,int offset=0)
        {
            if(!point)return Fix.FromInt(Tile(g,"segment.road",16+index,10+offset));
            var values=new List<Fix>();var status=g.Seat(0).View.Feature<PointLayerView>().ReadLayer("corridor.depth",new PointRegion(Fix.FromInt(3+index*2+offset),Fix.One,1),values);
            if(status!=SpatialLayerReadStatus.Success||values.Count!=1)throw new InvalidOperationException("point corridor sample "+status);
            return values[0];
        }
        private static void Physical(Check c,bool point,bool half)
        {
            var native=new InfrastructureServiceContractsProofModule();var independent=new PointLineGeometryProofModule();
            var f=point?Points(half,independent):Native(half,native);
            var g=point?ConstructionPlanTests.NewPoints(f):New(f);var idle=point?ConstructionPlanTests.NewPoints(f):New(f);ulong isolated=idle.Hash();
            if(point)c.That(independent.SharedOwner&&independent.NativeOwnerAbsent,"point provider uses original shared construction and no native routing/topology");
            else c.That(native.OneOwner&&!native.ConcreteBound&&native.PersistentOwners==1&&native.EndpointOwners==1,"one shared execution owner and one separate native topology/endpoint owner");
            var limits=new ReplayLimits(4*1024*1024,4000);
            using(var recorder=g.RecordReplay(limits)) {
                ulong hash=g.Hash();var intent=Intent(point,true);
                c.Equal(Code.None,g.Seat(0).Preview(new SubmitPlanCommand{Intent=intent}),"same shared admission for selected path");
                c.Equal(hash,g.Hash(),"preview pure before resources/entities");
                long id=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=intent}).Arg0;
                var pending=Plan(g,id);c.That(pending.Order==0&&pending.State==PlanState.Pending,"planned path not completed construction");
                c.Equal(Fix.FromInt(point?20:40),Total(g,point),"paused path consumes nothing");
                c.Equal(Fix.Zero,Segment(g,point,0),"no instant geometry");
                var twin=f.Load(g.Save());Step(g,twin);
                PermanentRemovalTests.Submit(c,g,new ConfigurePlanCommand{Plan=id,Group="physical",Priority=60,Paused=false});twin=f.Load(g.Save());
                int order=Plan(g,id).Order;
                Reach(g,twin,()=>Nodes(g,order).Any(n=>n.Id==(point?"segment.0.install":"segment.0.prepare")&&n.Progress.WholeTicks>0),"physical travel/material/work in selected geometry");
                var retained=Nodes(g,order).Single(n=>n.Id==(point?"segment.0.install":"segment.0.prepare"));var progress=retained.Progress;
                c.Equal(Fix.Zero,Segment(g,point,0),"partial work does not stamp");
                c.That(!Work(g,1).TryCopyNodes(order,new List<ActivityNodeView>())&&Plans(g,1).Count==0,"private work and plan intent");
                c.Equal(point?3:9,Nodes(g,order).Count,"same compiler binds authored nodes once per segment");
                if(point)c.That(g.Seat(0).Snapshot.Entities.Single(e=>e.Def=="machine").Spatial is PointSnapshot s&&s.Position>Fix.Zero,"actual point worker travels, no tile proxy");
                twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"partly worked actual cursor saves");
                long at=g.Clock.StepIndex;g.Clock.Pause();twin.Clock.Pause();Step(g,twin);c.Equal(at,g.Clock.StepIndex,"sole clock pauses corridor");g.Clock.Resume();twin.Clock.Resume();
                Reach(g,twin,()=>Segment(g,point,0).IsPositive,"first earned segment");
                c.Equal(Fix.Zero,Segment(g,point,2),"future segment not fabricated");
                PermanentRemovalTests.Submit(c,g,new ConfigurePlanCommand{Plan=id,Group="physical",Priority=60,Paused=true});twin=f.Load(g.Save());
                var before=Nodes(g,order).Select(n=>n.Progress).ToArray();for(int i=0;i<3;i++)Step(g,twin);
                c.That(before.SequenceEqual(Nodes(g,order).Select(n=>n.Progress)),"policy pause retains prefix/progress");
                PermanentRemovalTests.Submit(c,g,new ConfigurePlanCommand{Plan=id,Group="physical",Priority=60,Paused=false});twin=f.Load(g.Save());
                Reach(g,twin,()=>Plan(g,id).State==PlanState.Done,"full physical path");
                for(int i=0;i<3;i++)c.Equal(point?Fix.Parse("1.5"):Fix.One,Segment(g,point,i),"provider's authored layer value earned");
                c.Equal(Fix.FromInt(point?14:34),Total(g,point),"exactly six material units consumed");
                c.That(g.Seat(0).Snapshot.Entities.All(e=>e.Def!=(point?"corridor.cursor":"sim.line-work.segment.road-line")),"same shared owner retires completed empty cursor");
                c.Equal(progress,retained.Progress,"retained progress detached");c.That(pending.Order==0,"retained pending facts detached");
                long canceled=PermanentRemovalTests.Submit(c,g,new SubmitPlanCommand{Intent=Intent(point,false,point?20:10)}).Arg0;
                int canceledOrder=Plan(g,canceled).Order;
                Reach(g,null,()=>Nodes(g,canceledOrder).Any(n=>n.Progress.WholeTicks>0),"cancellation after physical work begins");
                var remaining=Total(g,point);
                PermanentRemovalTests.Submit(c,g,new CancelPlanCommand{Selection=new PlanCancellationSelection(false,new[]{canceled})});
                Step(g);c.Equal(PlanState.Cancelled,Plan(g,canceled).State,"shared cancellation settles actual intent");
                for(int i=0;i<3;i++)c.Equal(Fix.Zero,Segment(g,point,i,point?20:10),"canceled unearned segment never stamped");
                c.Equal(remaining,Total(g,point),"cancellation conserves physically retained supplies");
                c.Equal(g.Hash(),f.Load(g.Save()).Hash(),"completed/canceled owners durable");
                c.Equal(isolated,idle.Hash(),"interleaved selected geometry isolated");
                c.That(new ReplayVerifier().Verify(f,recorder.Finish(),limits).Success,"same public player input stream replays");
            }
        }
        private static void Invalid(Check c)
        {
            foreach(string fault in new[]{"missing-value","zero-value","unknown-layer","wrong-marker","owned-without-owner"}) {
                var probe=new PointLineGeometryProofModule();
                c.Throws<ContentException>(()=>ConstructionPlanTests.NewPoints(Points(false,probe,data=>{
                    var row=data.Get("lines")[0].Clone();row.Set("id",JsonValue.Of("corridor.unused"));data.Get("lines").Add(row);
                    if(fault=="missing-value")row.Remove("value");if(fault=="zero-value")row.Set("value",JsonValue.Number("0"));
                    if(fault=="unknown-layer")row.Set("layer",JsonValue.Of("missing.layer"));
                    if(fault=="wrong-marker")row.Set("work_marker",JsonValue.Of("machine"));
                    if(fault=="owned-without-owner")row.Set("owned",JsonValue.Of(true));
                })),"invalid loaded unused point corridor refuses "+fault);
                c.Equal(0,probe.Initialization.Initializations,"refusal before Initialize "+fault);
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current extracted owner build");c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat,"current original envelope");
            foreach(bool point in new[]{false,true})foreach(bool half in new[]{false,true})Physical(c,point,half);
            Invalid(c);
        }
    }
}
