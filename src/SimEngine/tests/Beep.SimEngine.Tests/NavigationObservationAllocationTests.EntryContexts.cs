using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static partial class NavigationObservationAllocationTests
    {
        private static void EntryContexts(Check c)
        {
            using var stream=typeof(Program).Assembly.GetManifestResourceStream("tests.navigation-entry-read-profile.json")
                ??throw new InvalidOperationException("missing explicit entry-read measurement profile");
            using var text=new StreamReader(stream);var profile=new JsonReader(Json.Parse(text.ReadToEnd()),"entry.measurement");
            if(profile.Int("schema")!=1||profile.String("id")!="navigation.entry-contexts")throw profile.Error("explicit profile identity required");
            int iterations=profile.Int("iterations"),warmup=profile.Int("warmup"),repeats=profile.Int("repeats");
            var contexts=profile.Value("contexts");profile.End();
            if(iterations<1||warmup<1||repeats<1||contexts==null||contexts.Count==0)throw new InvalidOperationException("positive authored measurement bounds required");
            Console.WriteLine("  entry measurement build="+EngineInfo.Build+" iterations="+iterations+" warmup="+warmup+" repeats="+repeats);
            for(int i=0;i<contexts.Count;i++)
            {
                var r=new JsonReader(contexts[i],"entry.measurement.contexts["+i+"]");
                string fixture=r.String("fixture"),scenario=r.String("scenario"),prototype=r.String("actor"),navigation=r.String("navigation"),condition=r.String("condition");
                var tile=new TileCoord(r.Int("x"),r.Int("y"));bool matches=r.Bool("condition_matches");int expected=r.Int("entry_cost");r.End();
                var factory=fixture=="native-layered"?LayeredNavigationTests.Factory():fixture=="independent-work"?IndependentTileWorkTests.Factory():
                    throw new InvalidOperationException("unknown explicit measurement fixture: "+fixture);
                NewGameRequest Request()=>new NewGameRequest {Scenario=scenario,Seed=723,Seats={Controller.Human,Controller.Human}};
                var game=factory.NewGame(Request());var idle=factory.NewGame(Request());ulong isolated=idle.Hash();
                c.That((game.World.Space is TileMap)==(fixture=="native-layered"),"measurement uses actual selected native/independent space");
                c.That(!game.World.Services.Has<IEconomy>(),"entry/condition queries need no business module");
                game.Clock.Pause();ulong before=game.Hash();long time=game.Clock.StepIndex;
                var actor=game.World.Entities.All.Single(e=>e.Def.Id==prototype&&e.Owner==new PlayerId(0));
                var map=game.World.Space as ITileSpatialSpace??throw new InvalidOperationException("actual tile-space capability missing");
                var predicate=game.World.Content.Get<ITileConditionDefinition>(condition);var grid=game.World.Services.Get<NavService>().Grid(navigation);
                c.That(map.InBounds(tile),"authored measurement coordinate belongs to selected space");
                var reads=new (string Name,Func<int> Read,int Expected)[]{
                    ("condition",()=>TileConditionContracts.Matches(predicate,game.World,map,tile)?1:0,matches?1:0),
                    ("structural",()=>grid.StructuralCostFor(actor,tile),expected),
                    ("live-entry",()=>grid.CostFor(actor,tile),expected)
                };
                foreach(var read in reads)
                {
                    int warm=0;for(int n=0;n<warmup;n++)warm+=read.Read();
                    c.Equal(read.Expected*warmup,warm,fixture+" "+read.Name+" warmup returns authored values");
                    for(int sample=0;sample<repeats;sample++)
                    {
                        long allocated=GC.GetAllocatedBytesForCurrentThread(),started=Stopwatch.GetTimestamp();int checksum=0;
                        for(int n=0;n<iterations;n++)checksum+=read.Read();
                        long ticks=Stopwatch.GetTimestamp()-started;allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
                        c.Equal(read.Expected*iterations,checksum,fixture+" "+read.Name+" measured actual callback values");
                        c.That(game.Fault==null,fixture+" "+read.Name+" full observation remains healthy");
                        Console.WriteLine(FormattableString.Invariant($"  allocation fixture={fixture} operation={read.Name} round={sample+1} calls={iterations} bytes={allocated} bytes-per-call={(double)allocated/iterations:0.000} elapsed-ms={(double)ticks*1000/Stopwatch.Frequency:0.000}"));
                    }
                }
                c.Equal(before,game.Hash(),"all measured original callbacks preserve actual gameplay state");
                c.Equal(time,game.Clock.StepIndex,"measurements advance no game time");
                var twin=factory.Load(game.Save());c.Equal(before,twin.Hash(),"measured state saves through original codecs");
                var twinActor=twin.World.Entities.Get(actor.Id);var twinGrid=twin.World.Services.Get<NavService>().Grid(navigation);
                c.Equal(expected,twinGrid.CostFor(twinActor,tile),"loaded original provider returns identical live entry cost");
                c.Equal(game.Hash(),twin.Hash(),"interleaved original/loaded queries retain twin identity");
                c.Equal(isolated,idle.Hash(),"another session receives no cached state or read side effects");
            }
        }
    }
}
