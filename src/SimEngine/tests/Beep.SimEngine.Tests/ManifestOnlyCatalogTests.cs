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
    // Master-authored JSON composition and ordinary public player commands/observations only.
    internal static class ManifestOnlyCatalogTests
    {
        internal const string Case="Manifest-only content: authored prototypes own physical durable corridor execution";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Compose(bool half,bool independent,out AuthoredLineCursorModule probe,
            Action<JsonValue> edit=null)
            =>AuthoredLineCursorTests.Compose(half,independent,out probe,edit,cursorDocument:"manifest-only-catalog.json");
        private static List<CatalogueEntry> Catalog(SimGame game)
        {var rows=new List<CatalogueEntry>();game.Observer.CopyCatalogue(rows);return rows;}
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual authored-only content engine build");
            c.Equal(154,EngineInfo.SaveFormat,"original physical codecs unchanged");
            c.That(typeof(ContentTypes).GetMethod("Generator")==null,"public module catalog generator deleted");
            c.That(typeof(EntityDef).GetMethod("Generated")==null,"programmatic prototype factory deleted");
            c.That(typeof(StorageDef).GetMethod("Make")==null&&typeof(StorageDeclaration).GetMethod("Make")==null,
                "native inventory authoring has no constructor-generated alternative");
            c.That(typeof(TilePresenceDef).GetMethod("Make")==null,"native presence has no constructor-generated alternative");
            CodecContinuation(c);
            foreach(bool independent in new[]{false,true}) {
                foreach(bool half in new[]{false,true})Physical(c,half,independent);
                Admission(c,independent);
            }
        }
        private static void CodecContinuation(Check c)
        {
            var module=new ProbeModule("probe",new ModuleVersion(1,0,0),content:types=>types.Component<CodecPartDef>("probe.part",7),
                compose:composer=>{var state=new MutableCodecState();composer.Services.Bind<IProbeFirst>(state);composer.Services.Bind<IProbeSecond>(state);composer.AddSystem(new MutableCodecSystem(),10);});
            var factory=CompositionProbe.Factory(module,StateCodecTests.AuthoredCodecContent("probe.part"));
            var game=factory.NewGame(new NewGameRequest{Scenario="probe",Seed=1});
            var idle=factory.NewGame(new NewGameRequest{Scenario="probe",Seed=1});ulong isolated=idle.Hash();
            c.Equal("probe.authored",game.Seat(0).Snapshot.Entities.Single().Def,"original codec instrumentation creates its actual authored nonspatial entity");
            var codecs=game.Seat(0).View.StateCodecs;
            c.That(codecs.Any(x=>x.Id=="probe.part"&&x.SchemaVersion==7&&x.Owner=="probe"),"authored component keeps its captured declared codec");
            for(int i=0;i<7;i++)game.Update();
            var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"authored nonspatial codec checkpoint retains original entity/system/service state");
            for(int i=0;i<13;i++)Step(game,twin);
            c.Equal(game.Hash(),twin.Hash(),"original authored codec continuation agrees through ordinary updates");
            c.Equal(isolated,idle.Hash(),"original codec entity/system/service owners are session-local, only metadata is shared");
        }
        private static void Physical(Check c,bool half,bool independent)
        {
            var factory=Compose(half,independent,out var probe);var game=New(factory);var idle=New(factory);ulong isolated=idle.Hash();
            var entries=Catalog(game);c.Equal(3,entries.Count(x=>x.Tags.Contains("manifest-authored")),"three actual master-authored prototype declarations");
            c.That(game.Seat(0).Snapshot.Entities.All(x=>x.Def!="manifest.unselected-prototype"),"authored unused prototype grants no instant live entity");
            c.That(probe.Initialization.Initializations==2&&game.Modules.Contains("proof.authored-line-cursors"),"actual selected module initializes once per live session");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId),"ordinary resource work needs no business module");
            var retained=entries.Single(x=>x.Id=="cursor.road");retained.Tags[0]="caller-only";
            c.That(Catalog(game).Single(x=>x.Id=="cursor.road").Tags.Contains("line-work"),"catalog intent arrays are detached from authored definition");
            var limits=new ReplayLimits(4*1024*1024,6000);using var recorder=game.RecordReplay(limits);
            var lay=new LayLineCommand{Line="segment.road-line"};lay.Tiles.AddRange(new[]{new TileCoord(16,10),new TileCoord(17,10),new TileCoord(18,10)});
            PermanentRemovalTests.Submit(c,game,lay);var order=Orders(game).Last();
            c.That(game.Seat(0).View.TryGetEntity(order.Target,out var cursor)&&cursor.Def=="cursor.road","physical work chooses the actual file-owned prototype");
            c.Equal(9,order.NodeCount,"original compiler binds three authored stages per actual cell");
            c.Equal(0,Tile(game,"segment.road",16,10),"posting intent creates no completed topology");
            var twin=factory.Load(game.Save());c.Equal(game.Hash(),twin.Hash(),"actual cursor and graph save before productive work");
            Reach(game,twin,()=>Nodes(game,order.Id).Any(x=>x.Progress.WholeTicks>0),"manifest-authored physical machine preparation");
            var partial=Nodes(game,order.Id).First(x=>x.Progress.WholeTicks>0);var progress=partial.Progress;
            c.That(Nodes(game,order.Id).Any(x=>x.Worker.IsValid),"existing workers earn elapsed physical work");
            c.Equal(Fix.FromInt(40),Steel(game),"tools remain actual held stock before install costs");
            twin=factory.Load(game.Save());game.Clock.Pause();twin.Clock.Pause();ulong paused=game.Hash();
            Step(game,twin);c.Equal(paused,game.Hash(),"one paused game clock freezes original cursor/graph/material owners");game.Clock.Resume();twin.Clock.Resume();
            Reach(game,twin,()=>Plans(game).Single(x=>x.Order==order.Id).State==PlanState.Done,"manifest-owned physical corridor completion");
            c.That(!game.Seat(0).View.TryGetEntity(order.Target,out _),"original once-only finish removes live cursor, not authored prototype");
            for(int x=16;x<=18;x++)c.Equal(1,Tile(game,"segment.road",x,10),"only earned physical cells become infrastructure");
            c.Equal(Fix.FromInt(34),Steel(game),"three actual segment material costs, no generated worksite stock");
            c.Equal(progress,partial.Progress,"retained progress is detached from later engine updates");
            c.Equal(3,Catalog(game).Count(x=>x.Tags.Contains("manifest-authored")),"live finalization never changes frozen authored catalog");
            var foreign=new List<ActivityNodeView>();c.That(!Work(game,1).TryCopyNodes(order.Id,foreign)&&foreign.Count==0,"private activity is not catalog intent or foreign progress");
            var save=game.Save();c.That(factory.Describe(save).Compatible&&factory.Load(save).Hash()==game.Hash(),"actual selected manifest/content identity restores completed work");
            var changed=Compose(half,independent,out _,x=>x.Get("entities")[2].Get("tags").Add(JsonValue.Of("changed-authored-identity")));
            c.That(!changed.Describe(save).Compatible,"changing an unused actual file prototype changes selected content identity");
            c.Throws<Exception>(()=>changed.Load(save),"changed master-owned document cannot silently restore old content identity");
            var verified=new ReplayVerifier().Verify(factory,recorder.Finish(),limits);
            c.That(verified.Success&&verified.Game.Hash()==game.Hash(),"sole authored catalog and physical public-player stream replay identically");
            c.Equal(isolated,idle.Hash(),"interleaved idle session shares metadata, never graph/material/clock state");
        }
        private static void Admission(Check c,bool independent)
        {
            Action<JsonValue>[] invalid={
                x=>x.Set("generators",JsonValue.NewArray()),
                x=>x.Set("patches",JsonValue.NewArray()),
                x=>x.Get("entities").Add(x.Get("entities")[0].Clone()),
                x=>x.Get("entities")[0].Get("components").Set("unregistered_generated_part",JsonValue.NewObject()),
                x=>x.Get("entities")[0].Set("unknown_generated_field",JsonValue.Number("1")),
                x=>x.Get("entities")[0].Get("components").Get("storage").Set("capacity_policy",JsonValue.Of("missing.policy"))
            };
            foreach(var edit in invalid) {
                AuthoredLineCursorModule probe=null;
                c.Throws<ContentException>(()=>New(Compose(false,independent,out probe,edit)),"invalid actual document cannot use generator/patch fallback");
                c.Equal(0,probe.Initialization.Initializations,"invalid master-owned content refuses before Initialize");
            }
        }
    }
}
