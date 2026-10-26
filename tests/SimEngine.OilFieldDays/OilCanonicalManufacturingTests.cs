using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Actual domain binary/public contracts only. No source projections or private gameplay calls.
    internal static partial class OilCanonicalManufacturingTests
    {
        internal const string Case="Oil canonical manufacturing: actual public company completes sequential physical haul trucks";
        private static readonly Code Production=new Code("production"), StockPort=new Code("oil.stock");
        internal static void Register()
        { TestRunner.Add(Case,Run); TestRunner.Add(NetworkCase,Network); }
        private sealed class Files:IFileSource
        {
            private readonly DiskFileSource _source;
            internal Files(string root)=>_source=new DiskFileSource(root);
            public bool Exists(string path)=>_source.Exists(path);
            public string ReadText(string path)
            {
                string text=_source.ReadText(path);if(path!="oil/spatial-world.json")return text;
                var root=Json.Parse(text);var rows=root.Get("tile_spaces")[0].Get("scenarios");
                for(int i=0;i<rows.Count;i++)if(rows[i].Get("scenario").Text=="frontier") {
                    rows[i].Set("width",JsonValue.Number("24"));rows[i].Set("height",JsonValue.Number("24"));
                }
                return root.ToString(); // Explicit proof-map choice, not an engine limit or time/resource rewrite.
            }
        }
        private static TransformationSnapshot State(SimGame game,EntityId hq)
        {
            if(!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(hq,Production,out var state))
                throw new InvalidOperationException("missing actual company production view");return state;
        }
        private static Fix Stock(SimGame game,EntityId hq,string resource)
        {
            if(!game.Seat(0).View.TryFindResource(resource,out var id))throw new InvalidOperationException("missing actual material "+resource);
            var refusal=game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(hq,StockPort,id,out var facts);
            if(!refusal.IsNone)throw new InvalidOperationException("actual material view "+resource+":"+refusal);return facts.Stock.AmountOf(id);
        }
        private static List<EntityView> Trucks(SimGame game)=>game.Seat(0).Snapshot.Entities.Where(e=>e.Def=="haul_truck").OrderBy(e=>e.Id.Value).ToList();
        private static string Diagnostic(SimGame game,EntityId hq)
        {
            var state=State(game,hq);
            return "time="+game.Clock.StepIndex+";phase="+state.Phase+";progress="+state.Progress+"/"+state.Required
                +";queue="+state.QueueCount+";blocker="+state.Blocker+";trucks="+string.Join(",",Trucks(game).Select(e=>e.Id+":"+((TileSpatialSnapshot)e.Spatial).Tile));
        }
        private static void Advance(SimGame game,EntityId hq,long steps,string stage)
        {
            var timer=Stopwatch.StartNew();long done=0;
            while(done<steps) {
                int batch=(int)Math.Min(60,steps-done);int ran=game.RunTicks(batch);done+=ran;
                if(game.Fault!=null||game.State!=GameState.Running||ran!=batch)
                    throw new InvalidOperationException("actual Oil production stopped "+stage+";"+Diagnostic(game,hq)+";fault="+game.Fault);
                if(timer.ElapsedMilliseconds>45000)throw new InvalidOperationException("bounded actual Oil production "+stage+";"+Diagnostic(game,hq));
            }
            Console.WriteLine("manufacturing-stage "+stage+";elapsed-ms="+timer.ElapsedMilliseconds+";"+Diagnostic(game,hq));
        }
        private static SimGame Saved(Check c,GameFactory factory,SimGame game)
        {var restored=factory.Load(game.Save());c.Equal(game.Hash(),restored.Hash(),"actual Oil production checkpoint retains original domain and native owners");return restored;}
        private static GameFactory Factory(out Assembly oil)
        {
            oil=ExternalModuleBoundaryTests.LoadOilAssembly();var api=oil.GetType("OilCo.Game.OilCoGame",true);
            var load=(Func<IFileSource,ContentSet>)api.GetMethod("LoadContent").CreateDelegate(typeof(Func<IFileSource,ContentSet>));
            var create=(Func<ContentSet,GameFactory>)api.GetMethod("Factory").CreateDelegate(typeof(Func<ContentSet,GameFactory>));
            string root=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(oil.Location),"../../../../content"));
            return create(load(new Files(root)));
        }
        private static EntityId Found(Check c,SimGame game,Assembly oil)
        {
            var planningType=oil.GetType("OilCo.Game.Views.PlanningView",true);
            var planning=typeof(PlayerView).GetMethod("Feature").MakeGenericMethod(planningType).Invoke(game.Seat(0).View,null);
            var suggest=(Func<IList<TileCoord>,int,int>)planningType.GetMethod("SuggestFoundingSites")
                .CreateDelegate(typeof(Func<IList<TileCoord>,int,int>),planning);
            var sites=new List<TileCoord>();c.Equal(1,suggest(sites,1),"actual engine suggests a valid whole-fleet founding site");
            if(sites.Count!=1)throw new InvalidOperationException("no actual founding site");
            var commandType=oil.GetType("OilCo.Game.Commands.FoundCompanyCommand",true);
            var found=(Command)Activator.CreateInstance(commandType);commandType.GetField("At").SetValue(found,sites[0]);
            ulong before=game.Hash();c.Equal(Code.None,game.Seat(0).Preview(found),"actual suggestion and public founding command agree");
            c.Equal(before,game.Hash(),"public founding preview is pure");c.That(TestKit.Order(game,0,found).Accepted,"normal company founding accepted");
            return game.Seat(0).Snapshot.Entities.Single(e=>e.Def=="company_base").Id;
        }
        private static void Run(Check c)
        {
            var factory=Factory(out var oil);var game=factory.NewGame(new NewGameRequest{Scenario="frontier",Seed=17,Seats={Controller.Human}});
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build,"actual Oil uses current engine behavior");
            c.Equal(60L,game.SimulationTime.ExactTicks(GameDuration.Hours(1)),"unchanged canonical minute quantum");
            var hq=Found(c,game,oil);int original=Trucks(game).Count;
            c.Equal(2,original,"unchanged actual initial fleet contains two haul trucks");
            var opening=new Dictionary<string,Fix>();foreach(string resource in new[]{"structural_steel","machinery","electrical_parts"})opening.Add(resource,Stock(game,hq,resource));
            var production=game.Seat(0).View.Feature<TransformationManagementView>();
            c.Equal(Code.None,production.ReadExecutionPreview(hq,Production,"build_haul_truck",out var preview),"actual named queue execution preview");
            c.That(preview.EntityOutput=="haul_truck"&&preview.AuthoredDuration.Equals(GameDuration.Hours(36)),"actual canonical vehicle output and duration, no proof prototype substitution");
            c.Equal(Fix.FromInt(18000),preview.MonetaryCost.AuthoredAmount,"unchanged original optional manufacturing charge");
            c.That(TestKit.Order(game,0,new EnqueueTransformationCommand{Transformer=hq,Capability=Production,Transformation="build_haul_truck",Count=2}).Accepted,"normal sequential actual manufacture accepted");
            game=Saved(c,factory,game);long previous=-1;
            for(int cycle=0;cycle<2;cycle++) {
                for(int i=0;i<60&&State(game,hq).Progress.WholeTicks==0;i++)Advance(game,hq,1,"cycle"+cycle+" admission");
                var state=State(game,hq);c.Equal(2160L,state.Required,"each canonical vehicle requires its own36hour work interval");
                c.That(state.Progress.Fraction==0&&state.Progress.WholeTicks>0&&state.Progress.WholeTicks<state.Required,"actual native productive progress, not an invented elapsed timer");
                long started=game.Clock.StepIndex-state.Progress.WholeTicks;
                c.That(previous<0||started>=previous,"next vehicle begins only after previous result committed");
                Advance(game,hq,state.Required-state.Progress.WholeTicks-1,"cycle"+cycle+" final incomplete interval");
                c.Equal(state.Required-1,State(game,hq).Progress.WholeTicks,"original work is incomplete one minute before required duration");
                c.Equal(original+cycle,Trucks(game).Count,"no premature actual vehicle output");
                game=Saved(c,factory,game);Advance(game,hq,1,"cycle"+cycle+" completion");
                for(int i=0;i<60&&Trucks(game).Count==original+cycle;i++)Advance(game,hq,1,"cycle"+cycle+" physical release");
                c.Equal(original+cycle+1,Trucks(game).Count,"actual canonical vehicle manufactured and physically placed");
                c.That(game.Clock.StepIndex-started>=2160,"actual result cannot precede original36hours");previous=game.Clock.StepIndex;
                var trucks=Trucks(game);var footprint=((TileSpatialSnapshot)trucks.Last().Spatial).Footprint;
                c.That(trucks.Take(trucks.Count-1).All(e=>!((TileSpatialSnapshot)e.Spatial).Footprint.Overlaps(footprint)),"actual native rollout has no overlapping haul trucks");
                game=Saved(c,factory,game);
            }
            c.That(State(game,hq).QueueCount==0&&State(game,hq).Wip.Stock.IsEmpty,"native queue and original WIP complete without residual copies");
            foreach(var cost in new[]{("structural_steel",20),("machinery",6),("electrical_parts",4)})
                c.Equal(opening[cost.Item1]-Fix.FromInt(cost.Item2),Stock(game,hq,cost.Item1),"actual finite canonical manufacturing material "+cost.Item1);
            c.Equal(Code.None,game.Seat(0).View.Feature<BusinessView>().ReadAccount(new PlayerId(0),out var account),"existing optional account remains readable");
            c.Equal(0,account.BudgetReservations,"original manufacturing obligations retire");
            c.That(game.Fault==null,"actual sequential Oil manufacture remains healthy");
        }
    }
}
