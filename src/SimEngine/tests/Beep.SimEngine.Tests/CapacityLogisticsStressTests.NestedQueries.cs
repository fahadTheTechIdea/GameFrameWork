using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static partial class CapacityLogisticsStressTests
    {
        private static void NestedRouteQueries(Check c)
        {
            foreach(bool raw in new[]{false,true})
            {
                var json=Json.Parse(FrameworkProofContent.Read("navigation-nested-observation.json"));
                json.Get("content").Get("navigation_observation_probes")[0].Set("raw",JsonValue.Of(raw));
                var factory=Factory(new Profile(),1,false,false,json);var game=New(factory);var idle=New(factory);ulong isolated=idle.Hash();
                game.Update();game.Clock.Pause();ulong before=game.Hash();long time=game.Tick;
                string name=raw?"probe.navigation.raw":"probe.navigation.cost";
                var router=game.World.Services.Get<NavService>();var grid=router.Grid(name);
                var from=new TileCoord(2,2);var goal=new TileRect(8,5,1,1);var path=new List<TileCoord>();
                Exception actual=null;PathSearchResult result=PathSearchResult.InvalidRequest;
                try{result=grid.FindPath(from,goal,path,256);}catch(Exception error){actual=error;}
                c.That(actual==null,"pure nested "+name+" query cannot corrupt outer scratch: "+actual?.Message);
                c.That(game.Fault==null,"pure nested "+name+" query remains a healthy observation");
                if(actual!=null||game.Fault!=null)continue; // Retain baseline failures without reusing a faulted world.
                c.Equal(PathSearchResult.Found,result,"outer profile remains reachable after every nested cost query");
                c.That(game.World.Content.Get<NavigationObservationDef>("probe.navigation.cost").NestedQueries>0,"actual authored inner profile was queried");
                var reference=new List<TileCoord>();
                c.Equal(PathSearchResult.Found,router.Grid("stress.route").FindPath(from,goal,reference,256),"same ten-cost flat control route exists");
                c.That(path.SequenceEqual(reference),"nested one-cost profile cannot contaminate outer ten-cost route or deterministic ties");
                c.Equal(before,game.Hash(),"nested profile queries mutate no gameplay state");
                c.Equal(time,game.Tick,"queries drive no second clock or elapsed time");
                var restored=factory.Load(game.Save());c.Equal(before,restored.Hash(),"pure nested route leaves original save durable");
                for(int repeat=0;repeat<3;repeat++)
                {
                    var next=new List<TileCoord>();var twin=new List<TileCoord>();
                    c.Equal(PathSearchResult.Found,grid.FindPath(from,goal,next,256),"reused root and nested scratch remain valid");
                    c.Equal(PathSearchResult.Found,restored.World.Services.Get<NavService>().Grid(name).FindPath(from,goal,twin,256),"loaded session has its own router scratch");
                    c.That(next.SequenceEqual(reference)&&next.SequenceEqual(twin),"interleaved saved nested queries retain identical paths");
                    c.Equal(game.Hash(),restored.Hash(),"interleaved nested query hashes agree");
                }
                c.Equal(isolated,idle.Hash(),"another real session is untouched by nested route queries");
            }
            NestedScratchCallbacks(c);
        }
        private static void NestedScratchCallbacks(Check c)
        {
            var map=new TileMap("nested.algorithm",12,12);var search=new PathSearch();
            var from=new TileCoord(1,1);var goal=new TileRect(10,10,1,1);
            var expected=new List<TileCoord>();c.Equal(PathSearchResult.Found,search.Find(map,from,goal,_=>10,10,true,256,expected),"flat algorithm reference");
            foreach(string callback in new[]{"cost","step","reached","caught-child-error"})
            {
                bool invoked=false;int children=0;var actual=new List<TileCoord>();Exception error=null;
                void Nested()
                {
                    if(invoked)return;invoked=true;
                    var inner=new List<TileCoord>();var grandchild=new List<TileCoord>();
                    int Cost(TileCoord tile)
                    {
                        children++;
                        c.Equal(PathSearchResult.Found,search.Find(map,new TileCoord(2,2),new TileRect(3,3,1,1),_=>1,1,true,32,grandchild),"third active invocation retains its own costs");
                        return 7;
                    }
                    if(callback=="caught-child-error")
                    {
                        var original=new ApplicationException("nested algorithm original");Exception caught=null;
                        try{search.Find(map,from,goal,_=>throw original,1,true,256,inner);}catch(Exception failure){caught=failure;}
                        c.That(ReferenceEquals(original,caught),"nested callback original exception preserved");
                    }
                    c.Equal(PathSearchResult.Found,search.Find(map,new TileCoord(2,2),new TileRect(4,4,1,1),Cost,7,true,64,inner),"nested algorithm route remains reachable");
                }
                try
                {
                    var status=search.Find(map,from,goal,tile=>{if(callback=="cost"||callback=="caught-child-error")Nested();return 10;},10,true,256,actual,
                        callback=="step"?(a,b)=>{Nested();return true;}:null,callback=="reached"?_=>Nested():null);
                    c.Equal(PathSearchResult.Found,status,callback+" outer route status");
                }
                catch(Exception failure){error=failure;}
                c.That(error==null,callback+" callback cannot overwrite active scratch: "+error?.Message);
                c.That(invoked&&children>0,callback+" really executes nested and third-level queries");
                c.That(actual.SequenceEqual(expected),callback+" outer parent chain and route ordering remain exact");
                var clean=new List<TileCoord>();
                c.Equal(PathSearchResult.Found,search.Find(map,from,goal,_=>10,10,true,256,clean),callback+" scratch returns even after errors");
                c.That(clean.SequenceEqual(expected),callback+" subsequent non-nested route stays exact");
            }
        }
    }
}
