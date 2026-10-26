using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Master-file configuration, public player inputs/time/views and numerical assertions only.
    internal static class WideGeometryAcceptanceTests
    {
        internal const string Case="Wide geometry: sparse worlds physical motion pages and checked durations";
        internal static void Register()=>TestRunner.Add(Case,Run);
        private static GameFactory Factory(bool direct,int size,int offset,bool half)
            =>SpaceContentContractTests.Create(new SpaceContentContractsModule(),direct,size,
                edit:(world,authored,master)=>{if(half)world.Get("time_definitions")[0].Get("quantum").Set("numerator",JsonValue.Number("30"));},
                editStartup:root=>{
                    foreach(string section in new[]{"tile_layer_setups","entity_setups"})
                        for(int i=0;i<root.Get(section).Count;i++){
                            var entry=root.Get(section)[i];var position=section=="entity_setups"?entry.Get("placement"):entry;
                            foreach(string axis in new[]{"x","y"})position.Set(axis,JsonValue.Number(checked(offset+int.Parse(position.Get(axis).Text,CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture)));
                        }
                });
        private static TileSpatialSnapshot Position(SimGame game)=>(TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(x=>x.Def=="survey.machine").Spatial;
        private static TileMapView Map(SimGame game,int seat=0)=>game.Seat(seat).View.Feature<TileMapView>();
        private static void Input(Check c,SimGame game,Command command)
        {
            ulong hash=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(command),"pure public physical command preview");
            c.Equal(hash,game.Hash(),"preview changes no position or resources");game.Seat(0).Submit(command);Step(game);
        }
        private static void Reach(SimGame game,SimGame twin,Func<bool> ready)
        {
            for(int i=0;i<256&&!ready();i++)Step(game,twin);
            if(!ready())throw new InvalidOperationException("wide movement stalled at="+game.Clock.StepIndex+" pose="+Position(game).Tile+" fault="+game.Fault);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"current wide-world contract");c.Equal(154,EngineInfo.SaveFormat,"original sparse/motion/clock codecs");
            long allocationStart=GC.GetAllocatedBytesForCurrentThread();
            foreach(bool direct in new[]{false,true})foreach(bool half in new[]{false,true})
                foreach(var world in new[]{(Size:100000,Offset:65500),(Size:int.MaxValue,Offset:int.MaxValue-100)})
                    Physical(c,direct,half,world.Size,world.Offset);
            Numeric(c);
            Console.WriteLine("wide proof observed allocations-current-thread="+(GC.GetAllocatedBytesForCurrentThread()-allocationStart)+" bytes; sparse fixtures and replay, not a general allocation budget");
        }
        private static void Physical(Check c,bool direct,bool half,int size,int offset)
        {
            var factory=Factory(direct,size,offset,half);var game=LiveSensorTests.New(factory);var idle=LiveSensorTests.New(factory);ulong other=idle.Hash();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId),"world/motion/queries do not need business");
            c.That(Map(game).Width==size&&Map(game).Height==size,"authored native/direct extent, no platform-sized cap");
            var limits=new ReplayLimits(4*1024*1024,3000);var recorder=game.RecordReplay(limits);
            Input(c,game,new SetWorkerHeldCommand{Worker=Entity(game,"survey.machine"),Held=true});
            var first=Position(game);c.Equal(new TileCoord(offset+2,offset+10),first.Tile,"actual high-coordinate startup, not presentation offset");
            var values=new List<int>();ulong hash=game.Hash();
            long row=(long)first.Tile.Y*size+first.Tile.X;
            var page=Map(game).ReadLayerPage("survey.terrain",new TileRect(0,0,size,size),new SpatialLayerPageRequest(row,1),values);
            c.That(page.Status==SpatialLayerReadStatus.Success&&page.TotalCount==(long)size*size&&page.Offset==row&&values.Count==1,"small exact page over full-width logical region");
            c.Equal(0,values[0],"real unallocated visible sparse cell is zero");
            var entities=new List<EntityView>();
            c.Equal(Code.None,Map(game).CopyEntities(new TileRect(first.Tile,1,1),entities),"wide entity region typed admission");
            c.That(entities.Any(x=>x.Id==Entity(game,"survey.machine")),"actual spatial query returns high-coordinate worker");
            var held=entities.First(x=>x.Id==Entity(game,"survey.machine"));
            foreach(var invalid in new[]{new TileRect(-1,0,1,1),new TileRect(0,0,-1,1),new TileRect(size-1,0,2,1),new TileRect(int.MaxValue,0,int.MaxValue,1)}){
                entities.Add(held);c.Equal(Codes.OutOfBounds,Map(game).CopyEntities(invalid,entities),"wide bounds refuse without checked-endpoint overflow");
                c.Equal(0,entities.Count,"refusal publishes no stale entity prefix");
            }
            c.Equal(Code.None,Map(game).CopyEntities(new TileRect(size,size,0,0),entities),"empty exact world edge is valid");
            c.Equal(0,entities.Count,"empty region has no entities");
            c.Equal(SpatialLayerReadStatus.InvalidRegion,Map(game).ReadLayer("survey.terrain",new TileRect(size-1,0,2,1),values),"same existing layer bounds semantics");
            var privateValues=new List<Fix>();
            c.Equal(SpatialLayerReadStatus.Unavailable,Map(game,1).ReadLayer("survey.deposit",new TileRect(offset+16,offset+10,1,1),privateValues),"large coordinates grant no private deposit");
            c.Equal(hash,game.Hash(),"all large queries and refusals pure");
            var move=new MoveVehicleCommand{Vehicle=Entity(game,"survey.machine"),To=new TileCoord(offset+10,offset+10)};
            Input(c,game,move);
            c.That(Position(game).Tile!=move.To,"large position does not make physical travel instant");
            var twin=factory.Load(game.Save());long at=game.Clock.StepIndex;hash=game.Hash();
            game.Clock.Pause();twin.Clock.Pause();Step(game,twin);c.That(at==game.Clock.StepIndex&&hash==game.Hash(),"sole paused clock freezes original high-coordinate movement");
            game.Clock.Resume();twin.Clock.Resume();Reach(game,twin,()=>Position(game).Tile==move.To);
            c.That(game.Clock.StepIndex>at&&first.Tile==new TileCoord(offset+2,offset+10),"actual later arrival and detached original pose");
            var arrived=Map(game).ReadLayerPage("survey.terrain",new TileRect(move.To,1,1),new SpatialLayerPageRequest(0,1),values);
            c.Equal(SpatialLayerReadStatus.Success,arrived.Status,"arrived sensor uses actual high-coordinate sparse layer");
            byte[] save=game.Save();c.That(save.Length<512*1024,"tiny populated sparse fixture has bounded checkpoint, not dense world bytes");
            c.Equal(game.Hash(),factory.Load(save).Hash(),"large geometry and actual sparse cells save through original codecs");
            c.Equal(other,idle.Hash(),"interleaved independent world remains unchanged");
            c.That(new ReplayVerifier().Verify(factory,recorder.Finish(),limits).Success,"same ordinary high-coordinate commands/clock replay");
            long hours=1L<<40;var duration=GameDuration.Hours(hours);
            c.Equal(checked(hours*(half?120L:60L)),game.SimulationTime.ExactTicks(duration),"actual minute/half-minute converter handles wide exact duration");
            c.Throws<OverflowException>(()=>game.SimulationTime.ExactTicks(GameDuration.Days(long.MaxValue)),"unrepresentable authored duration fails explicitly");
            c.Throws<ArgumentException>(()=>game.SimulationTime.ExactTicks(new GameDuration(1,3,DurationUnit.Second)),"inexact sub-step schedule refuses rather than rounds");
        }
        private static void Numeric(Check c)
        {
            var high=new TileCoord(int.MaxValue-10,int.MaxValue-10);var near=new TileCoord(int.MaxValue-2,int.MaxValue-2);
            c.Equal(8,TileCoord.Steps(high,near),"wide nearby steps remain exact");c.Equal(112,TileCoord.Octile10(high,near),"wide nearby travel metric exact");
            c.Equal(near,FixVec2.Centre(near).Tile,"native fixed-point centre retains high coordinate");
            c.Throws<OverflowException>(()=>TileCoord.Steps(new TileCoord(int.MinValue+1,0),new TileCoord(int.MaxValue,0)),"unrepresentable distance never wraps");
            c.Throws<OverflowException>(()=>TileCoord.Octile10(new TileCoord(0,0),new TileCoord(int.MaxValue,0)),"cost exhaustion fails explicitly");
            c.Throws<OverflowException>(()=>{_ =new TileRect(int.MaxValue-1,0,2,1).Right;},"rectangle endpoint exhaustion checked");
            c.Throws<OverflowException>(()=>{_ =new TileRect(0,0,int.MaxValue,int.MaxValue).Area;},"dense area count cannot silently wrap");
            c.Throws<OverflowException>(()=>new TileRect(0,0,1,1).Inflate(int.MaxValue),"inflation exhaustion checked");
            c.Throws<OverflowException>(()=>{_ =Fix.FromRaw(long.MaxValue)+Fix.One;},"fixed-point addition checked");
            c.Throws<OverflowException>(()=>{_ =new FixVec2(Fix.FromInt(100000),Fix.Zero).LengthSquared;},"unrepresentable squared distance explicit");
            c.Throws<OverflowException>(()=>GameDuration.Days(1).Times(long.MaxValue),"duration scaling exhaustion explicit");
        }
    }
}
