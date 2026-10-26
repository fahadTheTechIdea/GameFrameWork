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
    // Authored files/master composition + public player commands/clock/views only.
    internal static class AuthoredLineCursorTests
    {
        internal const string Case="Authored line cursors: contract worksites replace generation with physical durable DAG execution";
        internal static void Register()=>TestRunner.Add(Case,Run);
        internal static GameFactory Compose(bool half,bool independent,out AuthoredLineCursorModule proof,
            Action<JsonValue> invalid=null,Action<JsonValue> lineEdit=null,string cursorDocument="authored-line-cursors.json")
        {
            proof=new AuthoredLineCursorModule();
            var data=Json.Parse(FrameworkProofContent.Read(cursorDocument));
            if(!independent) for(int i=0;i<data.Get("entities").Count;i++) {
                var parts=data.Get("entities")[i].Get("components");
                if(parts.Has("proof_line_work")) {parts.Remove("proof_line_work");parts.Set("line_work",JsonValue.NewObject());}
            }
            invalid?.Invoke(data);
            var infrastructure=Json.Parse(FrameworkProofContent.Read("authored-segment-infrastructure.json"));
            var entities=JsonValue.NewArray();
            for(int i=0;i<infrastructure.Get("entities").Count;i++) {
                var entry=infrastructure.Get("entities")[i]; if(!entry.Get("components").Has("line_work"))entities.Add(entry);
            }
            infrastructure.Set("entities",entities);
            infrastructure.Get("lines")[0].Set("work_marker",JsonValue.Of("cursor.road"));
            infrastructure.Get("lines")[1].Set("work_marker",JsonValue.Of("cursor.pipe"));
            lineEdit?.Invoke(infrastructure);
            return AuthoredSegmentWorkflowTests.Factory(infrastructure:infrastructure.ToString(),extraModule:proof,
                extraDocument:cursorDocument,extraJson:data.ToString(),worldAuthor:world=> {
                    if(half)world.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));
                });
        }
        private static int Lay(Check c,SimGame g,string line,int y)
        {
            var command=new LayLineCommand {Line=line};
            command.Tiles.AddRange(new[]{new TileCoord(16,y),new TileCoord(17,y),new TileCoord(18,y)});
            PermanentRemovalTests.Submit(c,g,command); return Orders(g).Last().Id;
        }
        private static SimGame Saved(Check c,GameFactory f,SimGame g)
        {var twin=f.Load(g.Save());c.Equal(g.Hash(),twin.Hash(),"actual authored cursor/graph/claims checkpoint");return twin;}
        private static void Physical(Check c,bool half,bool independent)
        {
            var f=Compose(half,independent,out var proof);var g=New(f);var idle=New(f);ulong isolated=idle.Hash();
            var limits=new ReplayLimits(4*1024*1024,6000);using var recorder=g.RecordReplay(limits);
            c.That(proof.ActualBinding && proof.Initialization.Initializations>0,"actual JSON worksite aliases initialize through original owners");
            int order=Lay(c,g,"segment.road-line",10);var first=Orders(g).Single(x=>x.Id==order);
            c.That(g.Seat(0).View.TryGetEntity(first.Target,out var site) && site.Def=="cursor.road","explicit work_marker chooses actual prototype, not a generated name");
            c.Equal(9,first.NodeCount,"one original three-stage DAG per authored path cell");
            c.Equal(0,Tile(g,"segment.road",16,10),"normal command creates intent, never instant built topology");
            var twin=Saved(c,f,g);
            Reach(g,twin,()=>Nodes(g,order).Any(x=>x.Id=="segment.0.prepare"&&x.Progress.WholeTicks>0),"real machine preparation at authored cursor");
            var retained=Nodes(g,order).Single(x=>x.Id=="segment.0.prepare");var progress=retained.Progress;
            c.That(Nodes(g,order).Any(x=>x.Worker.IsValid),"original physical worker executes the cursor graph");
            c.Equal(Fix.FromInt(40),Steel(g),"held tools and carried resources remain real stock");
            EntityId worker=retained.Worker;
            PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand {Worker=worker,Held=true});twin=Saved(c,f,g);
            c.That(Nodes(g,order).Single(x=>x.Id=="segment.0.prepare").Progress.RemainingRaw(retained.RequiredTicks)<=progress.RemainingRaw(retained.RequiredTicks),"worker interruption retains earned node progress");
            PermanentRemovalTests.Submit(c,g,new SetWorkerHeldCommand {Worker=worker,Held=false});twin=Saved(c,f,g);
            Reach(g,twin,()=>Tile(g,"segment.road",16,10)==1,"earned first physical segment");
            c.Equal(0,Tile(g,"segment.road",17,10),"future segment remains intent until machine work");
            c.Equal(Fix.FromInt(38),Steel(g),"exact first segment material cost, no duplicated cursor stock");
            twin=Saved(c,f,g);g.Clock.Pause();twin.Clock.Pause();ulong paused=g.Hash();
            c.Equal(0,g.Update(1000000),"sole clock freezes cursor/graph/topology");c.Equal(0,twin.Update(1000000),"saved sole clock freezes");
            c.That(g.Hash()==paused&&g.Hash()==twin.Hash(),"pause checkpoint has no extra cursor clock");g.Clock.Resume();twin.Clock.Resume();
            Reach(g,twin,()=>Plans(g).Single(x=>x.Order==order).State==PlanState.Done,"complete authored corridor");
            c.That(!g.Seat(0).View.TryGetEntity(first.Target,out _),"original final effect removes actual authored cursor once");
            for(int x=16;x<=18;x++)c.Equal(1,Tile(g,"segment.road",x,10),"earned corridor survives cursor removal");
            c.Equal(Fix.FromInt(34),Steel(g),"three real segment costs paid exactly once");
            c.Equal(progress,retained.Progress,"detached progress remains immutable after completion");
            twin=Saved(c,f,g);
            int cancelled=Lay(c,g,"segment.pipe-line",14);var cancel=Orders(g).Single(x=>x.Id==cancelled);twin=Saved(c,f,g);
            Reach(g,twin,()=>Tile(g,"segment.pipe",16,14)==1&&Nodes(g,cancelled).Any(x=>x.Id=="segment.1.prepare"&&x.Progress.WholeTicks>0),"partial second corridor before cancellation");
            c.That(g.Seat(0).View.TryGetEntity(cancel.Target,out var pending)&&pending.Def=="cursor.pipe","second JSON prototype uses same cursor owner");
            PermanentRemovalTests.Submit(c,g,new CancelOrderCommand {Order=cancelled});twin=Saved(c,f,g);
            Reach(g,twin,()=>!g.Seat(0).View.TryGetEntity(cancel.Target,out _),"actual cancelled cursor and claims clear");
            c.Equal(1,Tile(g,"segment.pipe",16,14),"cancel preserves completed geometry prefix");c.Equal(0,Tile(g,"segment.pipe",17,14),"cancel cannot complete future geometry");
            c.Equal(Fix.FromInt(32),Steel(g),"cancel returns real held resources without repeated cost");
            var hidden=new List<ActivityNodeView>();c.That(!Work(g,1).TryCopyNodes(order,hidden)&&hidden.Count==0,"foreign viewer gets no private cursor graph");
            c.Equal(0,Plans(g,1).Count,"persistent cursor plan history remains private");
            Saved(c,f,g);c.Equal(isolated,idle.Hash(),"cursor binding and compiled graphs isolate interleaved session");
            c.That(new ReplayVerifier().Verify(f,recorder.Finish(),limits).Success,"ordinary file-authored worksite input stream replays");
        }
        private static void Admission(Check c,bool independent)
        {
            Action<JsonValue>[] bad={
                x=>x.Get("entities")[0].Set("kind",JsonValue.Of("structure")),
                x=>x.Get("entities")[0].Get("components").Get("tile_presence").Set("blocks",JsonValue.Of(true)),
                x=>x.Get("entities")[0].Get("components").Get("tile_presence").Set("movable",JsonValue.Of(false)),
                x=>x.Get("entities")[0].Get("components").Get("tile_presence").Set("width",JsonValue.Number("2")),
                x=>x.Get("entities")[0].Get("components").Remove("storage"),
                x=>x.Get("entities")[0].Get("components").Remove("tile_presence"),
                x=>x.Get("entities")[0].Get("components").Remove(independent?"proof_line_work":"line_work"),
                x=>x.Get("entities")[0].Get("components").Get("storage").Set("initial_stock",Json.Parse("{\"steel\":1}")),
                x=>x.Get("entities")[0].Get("components").Get("storage").Set("capacity_policy",JsonValue.Of("missing.policy")),
                x=>x.Get("entities")[0].Get("components").Get("tile_presence").Set("space",JsonValue.Of("missing.space"))
            };
            foreach(var edit in bad){AuthoredLineCursorModule proof=null;c.Throws<ContentException>(()=>New(Compose(false,independent,out proof,edit)),"invalid actual worksite fails early");c.Equal(0,proof.Initialization.Initializations,"invalid worksite never initializes systems");}
            foreach(string field in new[]{"missing","unknown","obsolete"}) {
                AuthoredLineCursorModule proof=null;
                c.Throws<ContentException>(()=>New(Compose(false,independent,out proof,lineEdit:x=> {
                    var line=x.Get("lines")[0];if(field=="missing")line.Remove("work_marker");if(field=="unknown")line.Set("work_marker",JsonValue.Of("unknown.marker"));
                    if(field=="obsolete")line.Set("work_capacity",JsonValue.Of("survey.storage"));
                })),"required work_marker/strict no-compatibility content");c.Equal(0,proof.Initialization.Initializations,"invalid reference/old field precedes initialization");
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current authored-cursor engine identity");
            foreach(bool independent in new[]{false,true}) {Admission(c,independent);foreach(bool half in new[]{false,true})Physical(c,half,independent);}
        }
    }
}
