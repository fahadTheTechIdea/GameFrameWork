using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static partial class CapacityLogisticsStressTests
    {
        // Independent algorithm oracle only: never used by a host or simulation decision.
        private static long? ShortestRoundedCost(int[,] costs, TileCoord from, TileRect goal,
            bool corners, Func<TileCoord, TileCoord, bool> permits)
        {
            int width=costs.GetLength(1),height=costs.GetLength(0);
            var distance=new long[height,width];var visited=new bool[height,width];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)distance[y,x]=long.MaxValue;
            if(costs[from.Y,from.X]<=0)return null;
            distance[from.Y,from.X]=0;
            while(true)
            {
                long best=long.MaxValue;int bx=-1,by=-1;
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                    if(!visited[y,x]&&distance[y,x]<best){best=distance[y,x];bx=x;by=y;}
                if(bx<0)return null;
                var current=new TileCoord(bx,by);if(goal.Contains(current))return best;
                visited[by,bx]=true;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    int x=bx+dx,y=by+dy;
                    if(dx==0&&dy==0||x<0||y<0||x>=width||y>=height||visited[y,x]||costs[y,x]<=0)continue;
                    var next=new TileCoord(x,y);if(permits!=null&&!permits(current,next))continue;
                    if(corners&&dx!=0&&dy!=0&&(costs[by,x]<=0||costs[y,bx]<=0))continue;
                    long step=dx!=0&&dy!=0?(long)costs[y,x]*14/10:costs[y,x];
                    if(best+step<distance[y,x])distance[y,x]=best+step;
                }
            }
        }
        private static void RoundedRouteCosts(Check c)
        {
            using var stream=typeof(Program).Assembly.GetManifestResourceStream("tests.diagonal-route-costs.json")
                ??throw new InvalidOperationException("missing authored diagonal route proof");
            using var text=new StreamReader(stream);var r=new JsonReader(Json.Parse(text.ReadToEnd()),"diagonal.route.proof");
            if(r.Int("schema")!=1||r.String("id")!="navigation.rounded-diagonal")throw r.Error("proof identity");
            int expected=r.Int("expected_cost"),cases=r.Int("weighted_cases"),seed=r.Int("seed"),budget=r.Int("maximum_expanded");
            var rows=r.Value("rows");r.End();int height=rows.Count,width=rows[0].Count;
            if(cases<1||width<1||height<1||budget<width*height)throw new InvalidOperationException("invalid explicit proof bounds");
            var fixedCosts=new int[height,width];
            for(int y=0;y<height;y++){
                if(rows[y].Count!=width)throw new InvalidOperationException("nonrectangular proof map");
                for(int x=0;x<width;x++)fixedCosts[y,x]=int.Parse(rows[y][x].Text,System.Globalization.CultureInfo.InvariantCulture);
            }
            var from=new TileCoord(0,0);var exact=new TileRect(width-1,height-1,1,1);
            c.Equal((long?)expected,ShortestRoundedCost(fixedCosts,from,exact,true,null),"independent fixed shortest path is the authored19");
            var map=new TileMap("rounded.route.proof",width,height);var search=new PathSearch();
            CheckMap(fixedCosts,exact,true,null,"fixed counterexample");
            uint state=(uint)seed;
            int Next(int maximum){state=unchecked(state*1664525+1013904223);return (int)(state%(uint)maximum);}
            for(int n=0;n<cases;n++)
            {
                var values=new int[height,width];int minimum=n%19+1;
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)values[y,x]=Next(11)==0?0:minimum+Next(5);
                values[0,0]=minimum;values[height-1,width-1]=minimum;
                bool directed=n%3==0;
                CheckMap(values,n%4==0?new TileRect(width-2,height-2,2,2):exact,n%2==0,
                    directed?(a,b)=>b.X>=a.X:null,"weighted "+n);
            }
            var large=new int[height,width];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)large[y,x]=int.MaxValue;
            CheckMap(large,exact,true,null,"maximum authored edge");
            var path=new List<TileCoord>();
            c.Equal(PathSearchResult.BudgetExceeded,search.Find(map,from,exact,t=>fixedCosts[t.Y,t.X],1,true,0,path),"explicit zero budget remains pending");
            c.Equal(0,path.Count,"budget refusal exposes no old path");
            var original=new InvalidOperationException("rounded route original callback");Exception actual=null;
            try{search.Find(map,from,exact,_=>throw original,1,true,budget,path);}catch(Exception error){actual=error;}
            c.That(ReferenceEquals(original,actual),"cost exception is not swallowed or replaced");
            CheckMap(fixedCosts,exact,true,null,"fresh scratch after failure");

            void CheckMap(int[,] values,TileRect target,bool corners,Func<TileCoord,TileCoord,bool> permits,string label)
            {
                long? optimum=ShortestRoundedCost(values,from,target,corners,permits);
                int minimum=int.MaxValue;foreach(int value in values)if(value>0&&value<minimum)minimum=value;
                var reads=new Dictionary<TileCoord,int>();var actualPath=new List<TileCoord>();
                int Cost(TileCoord tile){reads[tile]=reads.TryGetValue(tile,out int count)?count+1:1;return values[tile.Y,tile.X];}
                var status=search.Find(map,from,target,Cost,minimum,corners,budget,actualPath,permits);
                c.Equal(optimum.HasValue?PathSearchResult.Found:PathSearchResult.Unreachable,status,label+" existence agrees with independent oracle");
                c.That(reads.Values.All(count=>count==1),label+" original query-local callback owner reads each tile once");
                long total=0;TileCoord previous=from;bool legal=true;
                foreach(var tile in actualPath){
                    int dx=Math.Abs(tile.X-previous.X),dy=Math.Abs(tile.Y-previous.Y);
                    legal&=dx<=1&&dy<=1&&dx+dy>0&&values[tile.Y,tile.X]>0&&(permits==null||permits(previous,tile));
                    if(corners&&dx>0&&dy>0)legal&=values[previous.Y,tile.X]>0&&values[tile.Y,previous.X]>0;
                    total+=dx>0&&dy>0?(long)values[tile.Y,tile.X]*14/10:values[tile.Y,tile.X];previous=tile;
                }
                c.That(legal,label+" every actual path edge obeys original costs/corner/directed rules");
                if(optimum.HasValue){c.That(target.Contains(previous),label+" reaches actual target member");c.Equal(optimum.Value,total,label+" route cost is optimal");}
                else c.Equal(0,actualPath.Count,label+" no successful prefix on refusal");
                var repeat=new List<TileCoord>();
                c.Equal(status,search.Find(map,from,target,t=>values[t.Y,t.X],minimum,corners,budget,repeat,permits),label+" repeated query status stable");
                c.That(actualPath.SequenceEqual(repeat),label+" coordinate tie breaks are deterministic");
            }
        }
    }
}
